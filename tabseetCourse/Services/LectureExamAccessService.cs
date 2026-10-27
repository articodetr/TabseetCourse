using System;
using System.Linq;
using tabseetCourse.Data;
using tabseetCourse.Models;

namespace tabseetCourse.Services
{
    /// <summary>نتيجة فحص أهلية الطالب لدخول اختبار (وظيفة) محاضرة، مع سبب الرفض بالعربية.</summary>
    public class LectureExamAccessResult
    {
        public bool Allowed { get; set; }
        /// <summary>Allowed | NotLoggedIn | LectureNotFound | NotRegistered | SubscriptionPending | NoAssignment | NoQuestions | AlreadyPassed</summary>
        public string Code { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        /// <summary>أيقونة SweetAlert المناسبة للرسالة: success | info | warning | error</summary>
        public string Icon { get; set; }
        public int LectureId { get; set; }
        public int AssignmentId { get; set; }
    }

    /// <summary>
    /// فحص أهلية الطالب لدخول اختبار المحاضرة من بيانات قاعدة البيانات فقط (لا من المتصفح).
    /// الاختبار مستقل عن حضور الدرس (قرار المالك 2026-10-27): يكفي اشتراك فعّال في الدورة (CourseAccessService)،
    /// ووجود اختبار مرتبط بالمحاضرة وله أسئلة، وألا يكون الطالب قد اجتازه سابقاً. لا تُشترط مشاهدة الدرس المسجّل
    /// ولا تسجيل الدخول إلى الدرس المباشر ولا موعده. اكتمال الدرس نفسه (std_lecture.IsFinish) يبقى بقاعدته المعتادة
    /// في StudentAttendanceService: حضور + اجتياز الاختبار إن وُجد.
    /// تُستخدم من زر «الاختبار» في صفحة الطالب ومن صفحة الحل وجلب الأسئلة وتسليم الإجابات على حد سواء.
    /// لا يوجد في قاعدة البيانات موعد انتهاء مستقل للاختبار (Assignments: Name/Description/Lecture_ID/الدرجات فقط) فلا يُفترض وجوده.
    /// </summary>
    public class LectureExamAccessService
    {
        private readonly CourseAccessService _access;
        private readonly StudentAttendanceService _attendance;

        public LectureExamAccessService(tbseedDBEntities db)
            : this(new CourseAccessService(db), new StudentAttendanceService(db))
        {
        }

        public LectureExamAccessService(CourseAccessService access, StudentAttendanceService attendance)
        {
            _access = access;
            _attendance = attendance;
        }

        /// <summary>
        /// اختبار المحاضرة المعتمد كما تعرضه صفحة «برامجي التعليمية»: الأقدم (الأصغر معرّفاً) من الاختبارات التي لها أسئلة،
        /// وإلا الأقدم مطلقاً (فتكون النتيجة «بلا أسئلة» بدل «بلا اختبار»).
        /// </summary>
        public static Assignment GetLectureAssignment(Lecture lecture)
        {
            if (lecture == null || lecture.Assignments == null)
                return null;
            var ordered = lecture.Assignments.OrderBy(a => a.ID).ToList();
            return ordered.FirstOrDefault(a => a.Assignment_Q != null && a.Assignment_Q.Count > 0) ?? ordered.FirstOrDefault();
        }

        /// <summary>الفحص انطلاقاً من المحاضرة (زر «الاختبار»): يختار اختبار المحاضرة بنفسه.</summary>
        public LectureExamAccessResult EvaluateForLecture(string studentId, Lecture lecture)
        {
            return Evaluate(studentId, lecture, GetLectureAssignment(lecture), false);
        }

        /// <summary>الفحص انطلاقاً من الاختبار (صفحة الحل، جلب الأسئلة، التسليم): يتحقق من ارتباطه بمحاضرة فعلية.</summary>
        public LectureExamAccessResult EvaluateForAssignment(string studentId, Assignment assignment)
        {
            return Evaluate(studentId, assignment != null ? assignment.Lecture : null, assignment, true);
        }

        private LectureExamAccessResult Evaluate(string studentId, Lecture lecture, Assignment assignment, bool byAssignment)
        {
            int lectureId = lecture != null ? lecture.ID : 0;
            int assignmentId = assignment != null ? assignment.ID : 0;

            if (string.IsNullOrEmpty(studentId))
                return Deny("NotLoggedIn", "يجب تسجيل الدخول", "سجّل دخولك أولاً للوصول إلى اختبار الدرس.", "warning", lectureId, assignmentId);

            if (lecture == null)
            {
                // اختبار غير مرتبط بأي درس (أو معرّف غير صحيح): يُعامل كعدم توفر الاختبار دون كشف أي بيانات
                return byAssignment
                    ? Deny("NoAssignment", "الاختبار غير متاح بعد", "لم يقم المدرس بإضافة اختبار لهذا الدرس حتى الآن.", "info", lectureId, 0)
                    : Deny("LectureNotFound", "الدرس غير موجود", "تعذر العثور على هذا الدرس.", "error", lectureId, 0);
            }

            // 1) الاشتراك أولاً، قبل كشف أي معلومة عن الاختبار (نظام الاشتراكات والموافقة الحالي)
            var state = _access.GetState(studentId, lecture.Category);
            if (state == SubscriptionState.PendingApproval)
                return Deny("SubscriptionPending", "الاشتراك بانتظار الموافقة", "يجب موافقة الإدارة على اشتراكك في الدورة قبل الدخول إلى الاختبار.", "info", lectureId, 0);
            if (state != SubscriptionState.Active)
                return Deny("NotRegistered", "غير مشترك في الدورة", "يجب الاشتراك في هذه الدورة أولاً للوصول إلى اختباراتها.", "warning", lectureId, 0);

            // 2) وجود الاختبار وارتباطه بالدرس الصحيح
            if (assignment == null || assignment.Lecture_ID != lecture.ID)
                return Deny("NoAssignment", "الاختبار غير متاح بعد", "لم يقم المدرس بإضافة اختبار لهذا الدرس حتى الآن.", "info", lectureId, 0);
            if (assignment.Assignment_Q == null || assignment.Assignment_Q.Count == 0)
                return Deny("NoQuestions", "الاختبار غير متاح بعد", "لم يقم المدرس بإضافة أسئلة لهذا الاختبار حتى الآن.", "info", lectureId, assignmentId);

            // 3) اجتياز سابق من std_lecture (IsAssingmetFinish). الحضور/المشاهدة لا يُشترطان: الاختبار مستقل عن الدرس
            var status = _attendance.GetStatus(studentId, lecture);
            if (status.IsAssignmentFinish)
                return Deny("AlreadyPassed", "تم اجتياز الاختبار بنجاح", "لقد اجتزت اختبار هذا الدرس مسبقًا.", "success", lectureId, assignmentId);

            return Allow(lectureId, assignmentId);
        }

        /// <summary>
        /// تحقق قبل حفظ وظيفة من لوحة الإدارة/المدرس: المحاضرة المرتبطة موجودة فعلاً ويوجد سؤال واحد على الأقل،
        /// وإلا تُحفظ وظيفة بلا درس أو بلا أسئلة فلا يصل إليها الطالب. يعيد رسالة الخطأ أو null عند السلامة.
        /// </summary>
        public static string ValidateAssignmentForSave(tbseedDBEntities db, AssignmentModel am)
        {
            if (am == null)
                return "بيانات الوظيفة غير مكتملة.";
            if (string.IsNullOrWhiteSpace(am.Name))
                return "يجب إدخال اسم الوظيفة.";
            int lectureId = am.Lecture_ID;
            if (lectureId <= 0 || !db.Lectures.Any(l => l.ID == lectureId))
                return "المحاضرة المرتبطة بالوظيفة غير موجودة. أعد فتح صفحة إضافة الوظيفة من قائمة المحاضرات.";
            if (am.L_Assignment_Q == null || !am.L_Assignment_Q.Any(q => q != null && q.Q_ID > 0))
                return "يجب إضافة سؤال واحد على الأقل إلى الوظيفة قبل الحفظ.";
            return null;
        }

        private static LectureExamAccessResult Allow(int lectureId, int assignmentId)
        {
            return new LectureExamAccessResult { Allowed = true, Code = "Allowed", Icon = "success", LectureId = lectureId, AssignmentId = assignmentId };
        }

        private static LectureExamAccessResult Deny(string code, string title, string message, string icon, int lectureId, int assignmentId)
        {
            return new LectureExamAccessResult { Allowed = false, Code = code, Title = title, Message = message, Icon = icon, LectureId = lectureId, AssignmentId = assignmentId };
        }
    }
}
