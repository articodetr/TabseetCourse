using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using tabseetCourse.Classes;
using tabseetCourse.Models;
using tabseetCourse.Services;

namespace tabseetCourse.Controllers
{
    /// <summary>
    /// إشعارات المدرسين للإدارة. لا تُستخدم هنا السمة [Authorize(Roles = "Admin")]: فحص الأدوار القياسي يمر عبر
    /// مدير الأدوار القديم فيحاول إنشاء aspnetdb.mdf ويعطي "Failed to generate a user instance of SQL Server".
    /// التحقق يتم في OnActionExecuting بنفس آلية بقية لوحة التحكم (أدوار المستخدم الحالي من ASP.NET Identity).
    ///
    /// صفحة Index تعمل بنماذج HTML عادية بدون JavaScript: "تحديد كمقروء" يرسل POST ثم يعود لنفس الصفحة والتصفية.
    /// جرس الإشعارات في الـ layout يستدعي GetLatest عبر AJAX، لذا تعيد إجراءات POST نتيجة JSON عندما يكون الطلب AJAX.
    /// </summary>
    public class AdminNotificationsController : SharedCalss
    {
        private static readonly List<string> AllowedRoles = new List<string> { "Admin" };
        private readonly ITeacherLectureNotificationService _notificationService;

        public AdminNotificationsController()
        {
            _notificationService = new TeacherLectureNotificationService();
            if (CurrentUser != null)
            {
                ViewData["_UsermodelModel"] = CurrentUser;
                ViewBag._UsermodelModel = CurrentUser;
            }
        }

        public AdminNotificationsController(ITeacherLectureNotificationService notificationService)
        {
            _notificationService = notificationService;
            if (CurrentUser != null)
            {
                ViewData["_UsermodelModel"] = CurrentUser;
                ViewBag._UsermodelModel = CurrentUser;
            }
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);

            // المسموح: مستخدم مسجّل الدخول يحمل دور Admin (الأدوار من AspNetUserRoles عبر UserManager)
            if (CurrentUser == null || !IsTrustedUser(CurrentUser, AllowedRoles))
            {
                // صفحة Index ونماذج POST العادية (بدون AJAX) تُحوَّل إلى تسجيل الدخول؛ واجهات JSON (الجرس) تأخذ 403
                var request = filterContext.HttpContext.Request;
                bool isPage = string.Equals(filterContext.ActionDescriptor.ActionName, "Index", StringComparison.OrdinalIgnoreCase);
                bool isPlainForm = request.HttpMethod == "POST" && !request.IsAjaxRequest();
                if (isPage || isPlainForm)
                {
                    filterContext.Result = RedirectToAction("LogIn", "Home");
                }
                else
                {
                    filterContext.HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    filterContext.Result = Json(new { success = false, count = 0, unreadCount = 0, message = "غير مصرح" }, JsonRequestBehavior.AllowGet);
                }
                return;
            }

            ViewData["_UsermodelModel"] = CurrentUser;
            ViewBag._UsermodelModel = CurrentUser;
        }

        // GET: AdminNotifications?Status=unread&Search=...&Page=2
        [HttpGet]
        public async Task<ActionResult> Index(NotificationFilterViewModel filter)
        {
            string adminUserId = User.Identity.GetUserId();
            var model = await _notificationService.GetNotificationsPagedAsync(adminUserId, filter);
            return View(model);
        }

        // GET: AdminNotifications/GetLatest?count=5  (جرس الإشعارات في الـ layout)
        [HttpGet]
        public async Task<JsonResult> GetLatest(int count = 5)
        {
            try
            {
                string adminUserId = User.Identity.GetUserId();
                var items = await _notificationService.GetLatestNotificationsAsync(adminUserId, count);
                int unreadCount = await _notificationService.GetUnreadCountAsync(adminUserId);

                return Json(new { success = true, items, unreadCount }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // GET: AdminNotifications/GetUnreadCount
        [HttpGet]
        public async Task<JsonResult> GetUnreadCount()
        {
            try
            {
                string adminUserId = User.Identity.GetUserId();
                int count = await _notificationService.GetUnreadCountAsync(adminUserId);
                return Json(new { success = true, count }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, count = 0, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // POST: AdminNotifications/MarkAsRead  (id, returnUrl)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> MarkAsRead(int id, string returnUrl = null)
        {
            string adminUserId = User.Identity.GetUserId();
            try
            {
                bool result = await _notificationService.MarkAsReadAsync(id, adminUserId);

                if (Request.IsAjaxRequest())
                {
                    int unreadCount = await _notificationService.GetUnreadCountAsync(adminUserId);
                    return Json(new { success = result, unreadCount });
                }

                if (!result)
                {
                    TempData["NotificationError"] = "الإشعار غير موجود.";
                }
                return RedirectBackToList(returnUrl);
            }
            catch (Exception ex)
            {
                return Failure(ex, returnUrl);
            }
        }

        // POST: AdminNotifications/MarkAllAsRead  (returnUrl)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> MarkAllAsRead(string returnUrl = null)
        {
            string adminUserId = User.Identity.GetUserId();
            try
            {
                int count = await _notificationService.MarkAllAsReadAsync(adminUserId);

                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = true, markedCount = count, unreadCount = 0 });
                }
                return RedirectBackToList(returnUrl);
            }
            catch (Exception ex)
            {
                return Failure(ex, returnUrl);
            }
        }

        // GET: AdminNotifications/GetDetails?id=5  (واجهة JSON؛ عرض التفاصيل يحدد الإشعار كمقروء)
        [HttpGet]
        public async Task<JsonResult> GetDetails(int id)
        {
            try
            {
                string adminUserId = User.Identity.GetUserId();
                var details = await _notificationService.GetNotificationDetailsAsync(id, adminUserId);
                if (details == null)
                {
                    return Json(new { success = false, message = "الإشعار غير موجود" }, JsonRequestBehavior.AllowGet);
                }

                int unreadCount = await _notificationService.GetUnreadCountAsync(adminUserId);
                return Json(new { success = true, details, unreadCount }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>العودة إلى نفس الصفحة والتصفية التي أتى منها المستخدم؛ تُقبل الروابط المحلية فقط.</summary>
        private ActionResult RedirectBackToList(string returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index");
        }

        private ActionResult Failure(Exception ex, string returnUrl)
        {
            if (Request.IsAjaxRequest())
            {
                return Json(new { success = false, message = ex.Message });
            }
            TempData["NotificationError"] = "تعذر تحديث حالة الإشعار: " + ex.Message;
            return RedirectBackToList(returnUrl);
        }
    }
}
