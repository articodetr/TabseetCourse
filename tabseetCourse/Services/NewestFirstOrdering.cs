using System;
using System.Collections.Generic;
using System.Linq;
using tabseetCourse.Data;

namespace tabseetCourse.Services
{
    /// <summary>
    /// قاعدة «الأحدث أولاً» (Newest First) لحسابات AspNetUsers (الطلاب، المدرسون، مستخدمو الإدارة).
    /// بقية الجداول تملك معرّفاً تسلسلياً IDENTITY يُرتَّب به تنازلياً مباشرة في الاستعلام، أما هذا الجدول
    /// فمفتاحه GUID عشوائي، لذا يُقرأ عمود CreatedAt (ترحيل 20261027_AspNetUsers_CreatedAt.sql) بطلب SQL
    /// مستقل حتى لا يُمس نموذج Identity ولا ملف EDMX. إن لم يُطبَّق الترحيل بعد تبقى القائمة بترتيبها
    /// السابق بدل أن تفشل الصفحة. الحسابات بلا تاريخ (الأقدم من الترحيل) تأتي بعد الموسومة، والترتيب
    /// الثانوي الثابت هو المعرّف حتى لا يتغير ترتيب النتائج بين طلبين متتاليين.
    /// </summary>
    public static class NewestFirstOrdering
    {
        public class UserCreatedAtRow
        {
            public string Id { get; set; }
            public DateTime? CreatedAt { get; set; }
        }

        /// <summary>خريطة (معرّف الحساب → تاريخ الإنشاء UTC) للحسابات الموسومة فقط؛ قاموس فارغ إن لم يوجد العمود.</summary>
        public static Dictionary<string, DateTime> GetUserCreatedAt(tbseedDBEntities db)
        {
            var map = new Dictionary<string, DateTime>(StringComparer.Ordinal);
            if (db == null) return map;
            try
            {
                var rows = db.Database
                    .SqlQuery<UserCreatedAtRow>("SELECT Id, CreatedAt FROM dbo.AspNetUsers WHERE CreatedAt IS NOT NULL")
                    .ToList();
                foreach (var r in rows)
                {
                    if (!string.IsNullOrEmpty(r.Id) && r.CreatedAt.HasValue) map[r.Id] = r.CreatedAt.Value;
                }
            }
            catch (Exception)
            {
                // العمود غير موجود بعد (الترحيل لم يُطبَّق): نُبقي الترتيب كما كان بدل كسر الصفحة.
            }
            return map;
        }

        /// <summary>يرتّب قائمة حسابات من الأحدث إنشاءً إلى الأقدم (CreatedAt تنازلياً، ثم المعرّف كترتيب ثانوي ثابت).</summary>
        public static List<T> Users<T>(IEnumerable<T> users, Func<T, string> userId, tbseedDBEntities db)
        {
            if (users == null) return new List<T>();
            var created = GetUserCreatedAt(db);
            Func<T, string> key = u => userId(u) ?? string.Empty;
            return users
                .OrderByDescending(u => created.ContainsKey(key(u)))
                .ThenByDescending(u => { DateTime d; return created.TryGetValue(key(u), out d) ? d : DateTime.MinValue; })
                .ThenBy(u => key(u), StringComparer.Ordinal)
                .ToList();
        }
    }
}
