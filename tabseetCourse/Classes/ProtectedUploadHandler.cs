using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Caching;
using Microsoft.AspNet.Identity;
using tabseetCourse.Data;
using tabseetCourse.Services;

namespace tabseetCourse.Classes
{
    /// <summary>
    /// حماية خادمية لملفات الدورات المخزّنة في المجلد العام ~/Uploade/Images دون نقلها ودون تعطيل الملفات العامة.
    ///
    /// يُسجَّل في Web.config كمعالج لكل طلبات المجلد (location path="Uploade/Images")، فتمر الطلبات عبر
    /// خط أنابيب ASP.NET الإداري (مصادقة Identity تعمل) ثم:
    ///   - الملفات غير المرتبطة بدورات (شعارات، أيقونات، صور الشرائح، قوالب الشهادات...) تُقدَّم كما هي.
    ///   - مرفقات المحاضرات (Lectures.DocumenFile) وكتب الدورات (Categories.DocumentBook) تُقدَّم فقط لـ:
    ///       * حسابات الإدارة (أي دور غير Student/Teacher)،
    ///       * المدرس المسند إلى الدورة (أو دورتها الأم)،
    ///       * الطالب الذي اشتراكه في الدورة مفعّل (CourseAccessService) — المعلّق وغير المشترك يُرفضان.
    ///   - إيصالات الدفع القديمة (Std_Reg_Category.TransactionFile) تُقدَّم لحسابات الإدارة فقط.
    /// قائمة الملفات المحمية تُخزَّن مؤقتاً لمدة قصيرة لتخفيف الاستعلامات على كل صورة.
    /// </summary>
    public class ProtectedUploadHandler : IHttpHandler
    {
        private const string CacheKey = "Tabseet.ProtectedUploadFiles";
        private const int CacheSeconds = 60;
        private const string UploadVirtualRoot = "~/Uploade/Images";

        private sealed class ProtectedFileInfo
        {
            public readonly HashSet<int> CategoryIds = new HashSet<int>();
            public bool AdminOnly;
        }

        public bool IsReusable
        {
            get { return true; }
        }

        public void ProcessRequest(HttpContext context)
        {
            var request = context.Request;
            var response = context.Response;

            string root;
            string physical;
            try
            {
                root = Path.GetFullPath(context.Server.MapPath(UploadVirtualRoot)).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                physical = Path.GetFullPath(context.Server.MapPath(request.FilePath));
            }
            catch (Exception)
            {
                WriteStatus(response, 400, "طلب غير صالح.");
                return;
            }

            if (!physical.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                WriteStatus(response, 403, "غير مسموح.");
                return;
            }

            if (!File.Exists(physical))
            {
                WriteStatus(response, 404, "الملف غير موجود.");
                return;
            }

            string fileName = Path.GetFileName(physical);
            ProtectedFileInfo info;
            bool isProtected = GetProtectedFiles().TryGetValue(fileName, out info);

            if (isProtected)
            {
                bool authenticated = context.User != null && context.User.Identity != null && context.User.Identity.IsAuthenticated;
                if (!authenticated)
                {
                    string returnUrl = HttpUtility.UrlEncode(request.RawUrl);
                    response.Redirect(VirtualPathUtility.ToAbsolute("~/Home/LogIn") + "?ReturnUrl=" + returnUrl, false);
                    context.ApplicationInstance.CompleteRequest();
                    return;
                }
                if (!IsAllowed(context.User.Identity.GetUserId(), info))
                {
                    WriteStatus(response, 403, "هذا الملف خاص بالمشتركين في الدورة. ستتمكن من تحميله بعد تفعيل اشتراكك.");
                    return;
                }
            }

            ServeFile(context, physical, isProtected);
        }

        #region Authorization

        private static bool IsAllowed(string userId, ProtectedFileInfo info)
        {
            if (string.IsNullOrEmpty(userId) || info == null)
                return false;

            using (var db = new tbseedDBEntities())
            {
                var user = db.AspNetUsers.FirstOrDefault(u => u.Id == userId);
                if (user == null)
                    return false;

                var roles = user.AspNetRoles.Select(r => r.Name).ToList();
                bool isStudent = roles.Contains("Student");
                bool isTeacher = roles.Contains("Teacher");
                bool isStaff = roles.Any(r => r != "Student" && r != "Teacher");

                if (isStaff)
                    return true;
                if (info.AdminOnly)
                    return false;

                if (isTeacher)
                {
                    var teacherCategories = ParseTeacherCategories(user.Categories_ID);
                    if (teacherCategories.Count == 0)
                        return false;
                    var ids = info.CategoryIds.ToList();
                    var parents = db.Categories
                        .Where(c => ids.Contains(c.ID) && c.ParentCateogry_ID != null)
                        .Select(c => c.ParentCateogry_ID.Value)
                        .ToList();
                    return ids.Any(teacherCategories.Contains) || parents.Any(teacherCategories.Contains);
                }

                if (isStudent)
                {
                    var access = new CourseAccessService(db);
                    return info.CategoryIds.Any(categoryId => access.CanAccess(userId, categoryId));
                }
            }
            return false;
        }

        /// <summary>تنسيق Categories_ID للمدرس: "6/الرياضيات,9/أساسيات الحاسوب,..."</summary>
        private static HashSet<int> ParseTeacherCategories(string categoriesString)
        {
            var result = new HashSet<int>();
            if (string.IsNullOrWhiteSpace(categoriesString))
                return result;
            foreach (var item in categoriesString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = item.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                int id;
                if (parts.Length > 0 && int.TryParse(parts[0].Trim(), out id))
                    result.Add(id);
            }
            return result;
        }

        #endregion

        #region Protected file map

        private static Dictionary<string, ProtectedFileInfo> GetProtectedFiles()
        {
            var cached = HttpRuntime.Cache.Get(CacheKey) as Dictionary<string, ProtectedFileInfo>;
            if (cached != null)
                return cached;

            var map = new Dictionary<string, ProtectedFileInfo>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using (var db = new tbseedDBEntities())
                {
                    var lectureFiles = db.Lectures
                        .Where(l => l.DocumenFile != null && l.DocumenFile != "")
                        .Select(l => new { l.DocumenFile, l.Category_ID })
                        .ToList();
                    foreach (var f in lectureFiles)
                        Add(map, f.DocumenFile, f.Category_ID, false);

                    var books = db.Categories
                        .Where(c => c.DocumentBook != null && c.DocumentBook != "")
                        .Select(c => new { c.DocumentBook, c.ID })
                        .ToList();
                    foreach (var b in books)
                        Add(map, b.DocumentBook, b.ID, false);

                    var receipts = db.Std_Reg_Category
                        .Where(r => r.TransactionFile != null && r.TransactionFile != "")
                        .Select(r => r.TransactionFile)
                        .ToList();
                    foreach (var r in receipts)
                        Add(map, r, null, true);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("ProtectedUploadHandler.GetProtectedFiles: " + ex.Message);
            }

            HttpRuntime.Cache.Insert(CacheKey, map, null, DateTime.UtcNow.AddSeconds(CacheSeconds), Cache.NoSlidingExpiration);
            return map;
        }

        /// <summary>إبطال القائمة المخزّنة (يُستدعى بعد رفع مرفق أو كتاب جديد إن لزم؛ وإلا تتجدد خلال دقيقة).</summary>
        public static void InvalidateCache()
        {
            HttpRuntime.Cache.Remove(CacheKey);
        }

        private static void Add(Dictionary<string, ProtectedFileInfo> map, string storedName, int? categoryId, bool adminOnly)
        {
            if (string.IsNullOrWhiteSpace(storedName))
                return;
            string key = Path.GetFileName(storedName.Trim());
            if (string.IsNullOrEmpty(key))
                return;
            ProtectedFileInfo info;
            if (!map.TryGetValue(key, out info))
            {
                info = new ProtectedFileInfo();
                map[key] = info;
            }
            if (categoryId.HasValue)
                info.CategoryIds.Add(categoryId.Value);
            if (adminOnly && info.CategoryIds.Count == 0)
                info.AdminOnly = true;
        }

        #endregion

        #region Serving

        private static void ServeFile(HttpContext context, string physical, bool isProtected)
        {
            var response = context.Response;
            var request = context.Request;
            DateTime lastModified = File.GetLastWriteTimeUtc(physical);
            lastModified = new DateTime(lastModified.Year, lastModified.Month, lastModified.Day, lastModified.Hour, lastModified.Minute, lastModified.Second, DateTimeKind.Utc);

            DateTime since;
            string ifModifiedSince = request.Headers["If-Modified-Since"];
            if (!string.IsNullOrEmpty(ifModifiedSince)
                && DateTime.TryParse(ifModifiedSince, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out since)
                && since >= lastModified)
            {
                response.StatusCode = 304;
                response.SuppressContent = true;
                context.ApplicationInstance.CompleteRequest();
                return;
            }

            response.Clear();
            response.StatusCode = 200;
            response.ContentType = MimeMapping.GetMimeMapping(physical);
            response.Cache.SetLastModified(lastModified);
            if (isProtected)
            {
                response.Cache.SetCacheability(HttpCacheability.Private);
                response.Cache.SetNoStore();
                response.AddHeader("X-Content-Type-Options", "nosniff");
            }
            else
            {
                response.Cache.SetCacheability(HttpCacheability.Public);
                response.Cache.SetMaxAge(TimeSpan.FromHours(1));
            }
            response.TransmitFile(physical);
            context.ApplicationInstance.CompleteRequest();
        }

        private static void WriteStatus(HttpResponse response, int statusCode, string message)
        {
            response.Clear();
            response.StatusCode = statusCode;
            response.TrySkipIisCustomErrors = true;
            response.ContentType = "text/plain; charset=utf-8";
            response.Write(message);
        }

        #endregion
    }
}
