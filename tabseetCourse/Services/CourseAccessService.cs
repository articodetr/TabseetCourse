using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using tabseetCourse.Data;

namespace tabseetCourse.Services
{
    /// <summary>حالة اشتراك الطالب في دورة معينة.</summary>
    public enum SubscriptionState
    {
        /// <summary>لا يوجد تسجيل: يُطلب من الطالب الاشتراك أولاً.</summary>
        NotRegistered = 0,
        /// <summary>دورة مدفوعة بانتظار تأكيد الإدارة لاستلام القيمة خارج الموقع.</summary>
        PendingApproval = 1,
        /// <summary>اشتراك مفعّل (دورة مجانية، أو مدفوعة اعتمدتها الإدارة).</summary>
        Active = 2
    }

    /// <summary>نتيجة طلب الاشتراك في دورة.</summary>
    public class SubscriptionRequestResult
    {
        public bool Success { get; set; }
        public bool AlreadyRegistered { get; set; }
        public SubscriptionState State { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string Message { get; set; }
    }

    /// <summary>نتيجة اعتماد طلب اشتراك من الإدارة.</summary>
    public enum ApprovalOutcome
    {
        Activated,
        AlreadyActive,
        NotFound,
        NotPaidCourse
    }

    /// <summary>معلومات التوثيق المقروءة من أعمدة الترحيل (RequestedAt / ApprovedAt / ApprovedBy).</summary>
    public class SubscriptionAuditInfo
    {
        public DateTime? RequestedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string ApprovedBy { get; set; }
    }

    /// <summary>
    /// الآلية الموحدة للتحقق من صلاحية الطالب في الوصول إلى دورة، ولإنشاء طلبات الاشتراك واعتمادها.
    ///
    /// القواعد (تُقرأ من بيانات Categories و Std_Reg_Category على الخادم فقط):
    ///   - الدورة مجانية (Categories.IsPayed = 0) والطالب مسجّل           → Active (تُفتح مباشرة، بغض النظر عن std_category_Ispayed)
    ///   - الدورة مدفوعة (IsPayed != 0) وسجلها category_Ispayed=1 و std_category_Ispayed=0 → PendingApproval (يُمنع المحتوى المحمي)
    ///   - الدورة مدفوعة و std_category_Ispayed=1                         → Active (اعتمدتها الإدارة)
    ///   - لا يوجد سجل للطالب                                              → NotRegistered
    /// الدورات الفرعية: إذا كان للطالب سجل في الدورة الأم (الدبلوم) فإن حالة الأم تحكم الفرع أيضاً،
    /// فلا يُفتح فرع مجاني داخل دبلوم مدفوع قبل اعتماد اشتراك الدبلوم.
    /// </summary>
    public class CourseAccessService
    {
        public const string AdminRole = "Admin";
        public const string ApproverRole = "PaymentActive";

        private readonly tbseedDBEntities _db;

        public CourseAccessService(tbseedDBEntities db)
        {
            _db = db;
        }

        #region Category helpers (server-side truth)

        /// <summary>الدورة مدفوعة وفق بيانات Categories على الخادم (وليس ما يرسله المتصفح).</summary>
        public static bool IsPaidCategory(Category category)
        {
            return category != null && category.IsPayed.HasValue && category.IsPayed.Value != 0;
        }

        /// <summary>سعر الدورة إن كان محدداً (PayedValue رقمية وأكبر من صفر)، وإلا null.</summary>
        public static decimal? GetPrice(Category category)
        {
            if (category == null || string.IsNullOrWhiteSpace(category.PayedValue))
                return null;
            decimal price;
            if (decimal.TryParse(category.PayedValue.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out price) && price > 0)
                return price;
            return null;
        }

        /// <summary>حالة صف تسجيل واحد (بدون النظر إلى الدورة الأم).</summary>
        public static SubscriptionState GetRowState(int? categoryIsPayed, int? stdCategoryIsPayed)
        {
            if ((categoryIsPayed ?? 0) == 0)
                return SubscriptionState.Active;
            return (stdCategoryIsPayed ?? 0) == 1 ? SubscriptionState.Active : SubscriptionState.PendingApproval;
        }

        public static SubscriptionState GetRowState(Std_Reg_Category registration)
        {
            if (registration == null)
                return SubscriptionState.NotRegistered;
            return GetRowState(registration.category_Ispayed, registration.std_category_Ispayed);
        }

        /// <summary>نص عربي قصير للحالة (للواجهات).</summary>
        public static string Describe(SubscriptionState state)
        {
            switch (state)
            {
                case SubscriptionState.Active: return "الاشتراك مفعّل";
                case SubscriptionState.PendingApproval: return "بانتظار موافقة الإدارة";
                default: return "غير مشترك";
            }
        }

        #endregion

        #region Access checks

        /// <summary>حالة اشتراك الطالب في الدورة (مع مراعاة الدورة الأم إن وُجدت).</summary>
        public SubscriptionState GetState(string studentId, int categoryId)
        {
            if (string.IsNullOrEmpty(studentId))
                return SubscriptionState.NotRegistered;
            var category = _db.Categories.Find(categoryId);
            return GetState(studentId, category);
        }

        public SubscriptionState GetState(string studentId, Category category)
        {
            if (string.IsNullOrEmpty(studentId) || category == null)
                return SubscriptionState.NotRegistered;

            int categoryId = category.ID;
            int parentId = category.ParentCateogry_ID ?? -1;

            var rows = _db.Std_Reg_Category
                .Where(r => r.Std_ID == studentId && (r.Category_ID == categoryId || r.Category_ID == parentId))
                .Select(r => new { r.Category_ID, r.category_Ispayed, r.std_category_Ispayed })
                .ToList();

            SubscriptionState? own = null;
            SubscriptionState? parent = null;
            foreach (var row in rows)
            {
                var state = GetRowState(row.category_Ispayed, row.std_category_Ispayed);
                if (row.Category_ID == categoryId)
                    own = own.HasValue && own.Value > state ? own : state;      // عند وجود صفوف مكررة قديمة تُؤخذ الحالة الأفضل
                else
                    parent = parent.HasValue && parent.Value > state ? parent : state;
            }

            if (!own.HasValue && !parent.HasValue)
                return SubscriptionState.NotRegistered;
            if (own == SubscriptionState.PendingApproval || parent == SubscriptionState.PendingApproval)
                return SubscriptionState.PendingApproval;
            return SubscriptionState.Active;
        }

        public bool CanAccess(string studentId, int categoryId)
        {
            return GetState(studentId, categoryId) == SubscriptionState.Active;
        }

        public bool CanAccess(string studentId, Category category)
        {
            return GetState(studentId, category) == SubscriptionState.Active;
        }

        public bool CanAccessLecture(string studentId, Lecture lecture)
        {
            return lecture != null && CanAccess(studentId, lecture.Category_ID);
        }

        public bool CanAccessExam(string studentId, Exam exam)
        {
            return exam != null && exam.Category_ID.HasValue && CanAccess(studentId, exam.Category_ID.Value);
        }

        public bool CanAccessAssignment(string studentId, Assignment assignment)
        {
            return assignment != null && assignment.Lecture != null && CanAccess(studentId, assignment.Lecture.Category_ID);
        }

        /// <summary>
        /// معرّفات الدورات التي يحق للطالب الوصول إلى محتواها (المفعّلة فقط، مع الدورات الفرعية للدبلومات المفعّلة)،
        /// دون الدورات المعلّقة بانتظار الإدارة.
        /// </summary>
        public List<int> GetAccessibleCategoryIds(string studentId)
        {
            var result = new List<int>();
            if (string.IsNullOrEmpty(studentId))
                return result;

            var rows = _db.Std_Reg_Category
                .Where(r => r.Std_ID == studentId && r.Category_ID != null)
                .Select(r => new { CategoryId = r.Category_ID.Value, r.category_Ispayed, r.std_category_Ispayed, ParentId = r.Category.ParentCateogry_ID })
                .ToList();

            var stateByCategory = new Dictionary<int, SubscriptionState>();
            var parentByCategory = new Dictionary<int, int?>();
            foreach (var row in rows)
            {
                var state = GetRowState(row.category_Ispayed, row.std_category_Ispayed);
                SubscriptionState existing;
                if (!stateByCategory.TryGetValue(row.CategoryId, out existing) || state > existing)
                    stateByCategory[row.CategoryId] = state;
                parentByCategory[row.CategoryId] = row.ParentId;
            }

            foreach (var pair in stateByCategory)
            {
                if (pair.Value != SubscriptionState.Active)
                    continue;
                int? parentId = parentByCategory[pair.Key];
                SubscriptionState parentState;
                if (parentId.HasValue && stateByCategory.TryGetValue(parentId.Value, out parentState) && parentState != SubscriptionState.Active)
                    continue;
                result.Add(pair.Key);
            }

            // الدورات الفرعية لدبلوم مفعّل التي لا يملك الطالب صفاً خاصاً بها
            var activeParents = result.Where(id => !parentByCategory[id].HasValue).ToList();
            if (activeParents.Count > 0)
            {
                var children = _db.Categories
                    .Where(c => c.ParentCateogry_ID != null && activeParents.Contains(c.ParentCateogry_ID.Value))
                    .Select(c => c.ID)
                    .ToList();
                foreach (var childId in children)
                {
                    if (!stateByCategory.ContainsKey(childId) && !result.Contains(childId))
                        result.Add(childId);
                }
            }

            return result;
        }

        #endregion

        #region Subscription requests (student)

        /// <summary>
        /// إنشاء طلب اشتراك للطالب في دورة: المجانية تُفعَّل فوراً، والمدفوعة تبقى بانتظار موافقة الإدارة.
        /// لا تُطلب أي معلومات دفع. تُمنع الطلبات المكررة. تُسجَّل الدورة وفروعها كما في السلوك السابق.
        /// </summary>
        public SubscriptionRequestResult Register(string studentId, int categoryId)
        {
            var result = new SubscriptionRequestResult { CategoryId = categoryId };
            if (string.IsNullOrEmpty(studentId))
            {
                result.Message = "يرجى تسجيل الدخول أولاً.";
                return result;
            }

            var category = _db.Categories.Find(categoryId);
            if (category == null)
            {
                result.Message = "الدورة المطلوبة غير موجودة.";
                return result;
            }
            result.CategoryName = category.NameAr;

            var currentState = GetState(studentId, category);
            if (currentState != SubscriptionState.NotRegistered)
            {
                result.AlreadyRegistered = true;
                result.State = currentState;
                result.Message = currentState == SubscriptionState.PendingApproval
                    ? "لديك طلب اشتراك سابق في هذه الدورة بانتظار موافقة الإدارة."
                    : "أنت مشترك بالفعل في هذه الدورة.";
                return result;
            }

            if ((category.CanRegister ?? 0) != 1)
            {
                result.Message = "التسجيل في هذه الدورة غير متاح حالياً.";
                return result;
            }
            DateTime endTime;
            if (!string.IsNullOrWhiteSpace(category.EndTime)
                && DateTime.TryParse(category.EndTime.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out endTime)
                && endTime < DateTime.Now)
            {
                result.Message = "انتهت فترة التسجيل في هذه الدورة.";
                return result;
            }

            bool paid = IsPaidCategory(category);
            var toRegister = _db.Categories.Where(e => e.ID == categoryId || e.ParentCateogry_ID == categoryId).ToList();
            var existingIds = _db.Std_Reg_Category
                .Where(r => r.Std_ID == studentId && r.Category_ID != null)
                .Select(r => r.Category_ID.Value)
                .ToList();

            var created = new List<Std_Reg_Category>();
            foreach (var item in toRegister)
            {
                if (existingIds.Contains(item.ID))
                    continue;
                var registration = new Std_Reg_Category
                {
                    Category_ID = item.ID,
                    Std_ID = studentId,
                    category_Ispayed = IsPaidCategory(item) ? 1 : 0,
                    std_category_Ispayed = 0
                };
                _db.Std_Reg_Category.Add(registration);
                created.Add(registration);
            }
            _db.SaveChanges();

            StampRequestedAt(created.Select(c => c.ID).ToList());

            result.Success = true;
            result.State = paid ? SubscriptionState.PendingApproval : SubscriptionState.Active;
            result.Message = paid
                ? "تم استلام طلب اشتراكك في «" + category.NameAr + "». ستتمكن من دخول الدورة بعد تأكيد الإدارة."
                : "تم تفعيل اشتراكك في «" + category.NameAr + "» ويمكنك بدء التعلم الآن.";
            return result;
        }

        /// <summary>تسجيل تاريخ الطلب في عمود التوثيق؛ لا يُفشل الاشتراك إن لم يُطبَّق الترحيل بعد.</summary>
        private static void StampRequestedAt(List<int> registrationIds)
        {
            if (registrationIds == null || registrationIds.Count == 0)
                return;
            try
            {
                using (var ctx = new SubscriptionDbContext())
                {
                    var now = DateTime.UtcNow;
                    foreach (var row in ctx.Subscriptions.Where(s => registrationIds.Contains(s.ID)))
                    {
                        if (row.RequestedAt == null)
                            row.RequestedAt = now;
                    }
                    ctx.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("CourseAccessService.StampRequestedAt: " + ex.Message);
            }
        }

        #endregion

        #region Admin approval

        /// <summary>
        /// الطلبات المعلّقة (دورة مدفوعة ولم تعتمدها الإدارة بعد). يُستبعد صف الدورة الفرعية عندما يكون
        /// صف الدبلوم الأم لنفس الطالب معلّقاً أيضاً، لأن اعتماد الدبلوم يفعّل فروعه معاً.
        /// </summary>
        public List<Std_Reg_Category> GetPendingRegistrations()
        {
            var pending = _db.Std_Reg_Category
                .Where(r => r.category_Ispayed == 1 && (r.std_category_Ispayed == null || r.std_category_Ispayed == 0))
                .ToList();

            var pendingKeys = new HashSet<string>(pending.Select(p => p.Std_ID + "|" + p.Category_ID));
            return pending
                .Where(p => p.Category == null
                            || !p.Category.ParentCateogry_ID.HasValue
                            || !pendingKeys.Contains(p.Std_ID + "|" + p.Category.ParentCateogry_ID.Value))
                .OrderByDescending(p => p.ID)
                .ToList();
        }

        public int CountPendingRegistrations()
        {
            return GetPendingRegistrations().Count;
        }

        /// <summary>قراءة أعمدة التوثيق لمجموعة من الطلبات (قاموس فارغ إن لم يُطبَّق الترحيل).</summary>
        public static Dictionary<int, SubscriptionAuditInfo> GetAuditInfo(List<int> registrationIds)
        {
            var map = new Dictionary<int, SubscriptionAuditInfo>();
            if (registrationIds == null || registrationIds.Count == 0)
                return map;
            try
            {
                using (var ctx = new SubscriptionDbContext())
                {
                    var rows = ctx.Subscriptions
                        .Where(s => registrationIds.Contains(s.ID))
                        .Select(s => new { s.ID, s.RequestedAt, s.ApprovedAt, s.ApprovedBy })
                        .ToList();
                    foreach (var row in rows)
                        map[row.ID] = new SubscriptionAuditInfo { RequestedAt = row.RequestedAt, ApprovedAt = row.ApprovedAt, ApprovedBy = row.ApprovedBy };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("CourseAccessService.GetAuditInfo: " + ex.Message);
            }
            return map;
        }

        /// <summary>
        /// تأكيد الدفع وتفعيل الاشتراك (تنفّذه الإدارة فقط بعد التحقق من الصلاحية في المتحكم).
        /// آمنة عند التكرار: الطلب المفعّل مسبقاً يعيد AlreadyActive دون أي تغيير.
        /// تُفعَّل معه صفوف الدورات الفرعية المعلّقة لنفس الطالب (ضمن نفس الاشتراك) فقط.
        /// </summary>
        public ApprovalOutcome Approve(int registrationId, string adminUserId, out Std_Reg_Category registration)
        {
            registration = _db.Std_Reg_Category.Find(registrationId);
            if (registration == null)
                return ApprovalOutcome.NotFound;
            if ((registration.category_Ispayed ?? 0) != 1)
                return ApprovalOutcome.NotPaidCourse;
            if ((registration.std_category_Ispayed ?? 0) == 1)
                return ApprovalOutcome.AlreadyActive;

            string studentId = registration.Std_ID;
            int categoryId = registration.Category_ID ?? -1;
            var childIds = _db.Categories
                .Where(c => c.ParentCateogry_ID == categoryId)
                .Select(c => c.ID)
                .ToList();

            using (var ctx = new SubscriptionDbContext())
            {
                var now = DateTime.UtcNow;
                var rows = ctx.Subscriptions
                    .Where(s => s.ID == registrationId
                                || (s.Std_ID == studentId && s.Category_ID != null && childIds.Contains(s.Category_ID.Value)
                                    && s.category_Ispayed == 1 && (s.std_category_Ispayed == null || s.std_category_Ispayed == 0)))
                    .ToList();
                foreach (var row in rows)
                {
                    row.std_category_Ispayed = 1;
                    row.ApprovedAt = now;
                    row.ApprovedBy = adminUserId;
                }
                ctx.SaveChanges();
            }

            _db.Entry(registration).Reload();
            return ApprovalOutcome.Activated;
        }

        #endregion
    }
}
