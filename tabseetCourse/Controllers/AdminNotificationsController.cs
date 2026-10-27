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
                bool isPage = string.Equals(filterContext.ActionDescriptor.ActionName, "Index", StringComparison.OrdinalIgnoreCase);
                if (isPage)
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

        // GET: AdminNotifications
        [HttpGet]
        public async Task<ActionResult> Index(NotificationFilterViewModel filter)
        {
            string adminUserId = User.Identity.GetUserId();
            var model = await _notificationService.GetNotificationsPagedAsync(adminUserId, filter);
            return View(model);
        }

        // GET: AdminNotifications/GetLatest?count=5
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

        // POST: AdminNotifications/MarkAsRead
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> MarkAsRead(int id)
        {
            try
            {
                string adminUserId = User.Identity.GetUserId();
                bool result = await _notificationService.MarkAsReadAsync(id, adminUserId);
                int unreadCount = await _notificationService.GetUnreadCountAsync(adminUserId);

                return Json(new { success = result, unreadCount });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: AdminNotifications/MarkAllAsRead
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> MarkAllAsRead()
        {
            try
            {
                string adminUserId = User.Identity.GetUserId();
                int count = await _notificationService.MarkAllAsReadAsync(adminUserId);

                return Json(new { success = true, markedCount = count, unreadCount = 0 });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: AdminNotifications/GetDetails/5
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
    }
}

