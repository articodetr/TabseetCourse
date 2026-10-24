using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using tabseetCourse.Data;
using tabseetCourse.Models;

namespace tabseetCourse.Services
{
    public interface ITeacherLectureNotificationService
    {
        Task<LectureStatusDto> GetLectureStatusAsync(int lectureId);
        Task<Dictionary<int, LectureStatusDto>> GetLectureStatusesForCategoryAsync(int categoryId);
        Task<NotificationOperationResult> UpdateLectureStatusAsync(int lectureId, string newStatus, string teacherId, string teacherName, string notes, string postponeReason);
        Task<NotificationOperationResult> RecordLectureCreatedAsync(int lectureId, string lectureTitle, int categoryId, string courseName, string teacherId, string teacherName, string lectureType, string dayAndTime, string link);
        Task<NotificationOperationResult> RecordLectureUpdatedAsync(int lectureId, string oldTitle, string newTitle, int categoryId, string courseName, string teacherId, string teacherName, string oldTime, string newTime, string oldLink, string newLink, string oldType, string newType);
        Task<NotificationOperationResult> RecordLectureDeletedAsync(int lectureId, string lectureTitle, int categoryId, string courseName, string teacherId, string teacherName);
        Task<int> GetUnreadCountAsync(string adminUserId);
        Task<List<AdminNotificationItemViewModel>> GetLatestNotificationsAsync(string adminUserId, int count = 5);
        Task<NotificationPagedListViewModel> GetNotificationsPagedAsync(string adminUserId, NotificationFilterViewModel filter);
        Task<bool> MarkAsReadAsync(int notificationId, string adminUserId);
        Task<int> MarkAllAsReadAsync(string adminUserId);
        Task<AdminNotificationItemViewModel> GetNotificationDetailsAsync(int notificationId, string adminUserId);
    }

    public class TeacherLectureNotificationService : ITeacherLectureNotificationService
    {
        private static readonly Dictionary<string, (string Arabic, string BadgeClass)> ActionMeta = new Dictionary<string, (string, string)>
        {
            { "LectureCompleted", ("إكمال درس", "badge badge-success") },
            { "LecturePostponed", ("تأجيل درس", "badge badge-warning") },
            { "LectureReopened", ("إعادة فتح درس", "badge badge-secondary") },
            { "LectureCreated", ("إضافة محاضرة", "badge badge-primary") },
            { "LectureTimeChanged", ("تغيير موعد محاضرة", "badge badge-info") },
            { "LectureLinkChanged", ("تعديل رابط المحاضرة", "badge badge-info") },
            { "LectureUpdated", ("تعديل بيانات المحاضرة", "badge badge-info") },
            { "LectureDeleted", ("حذف محاضرة", "badge badge-danger") }
        };

        public static string GetActionArabic(string actionType)
        {
            if (string.IsNullOrEmpty(actionType)) return "إجراء غير محدد";
            return ActionMeta.ContainsKey(actionType) ? ActionMeta[actionType].Arabic : actionType;
        }

        public static string GetActionBadgeClass(string actionType)
        {
            if (string.IsNullOrEmpty(actionType)) return "badge badge-secondary";
            return ActionMeta.ContainsKey(actionType) ? ActionMeta[actionType].BadgeClass : "badge badge-secondary";
        }

        public static string FormatLocalTime(DateTime utcTime)
        {
            try
            {
                TimeZoneInfo tzi = TimeZoneInfo.FindSystemTimeZoneById("Arab Standard Time");
                DateTime local = TimeZoneInfo.ConvertTimeFromUtc(utcTime, tzi);
                return local.ToString("yyyy-MM-dd hh:mm tt", new CultureInfo("ar-SA"));
            }
            catch
            {
                DateTime local = utcTime.AddHours(3);
                return local.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            }
        }

        public static string GetTimeAgo(DateTime utcTime)
        {
            TimeSpan diff = DateTime.UtcNow - utcTime;
            if (diff.TotalSeconds < 60) return "الآن";
            if (diff.TotalMinutes < 60) return $"منذ {(int)diff.TotalMinutes} دقيقة";
            if (diff.TotalHours < 24) return $"منذ {(int)diff.TotalHours} ساعة";
            if (diff.TotalDays < 30) return $"منذ {(int)diff.TotalDays} يوم";
            return FormatLocalTime(utcTime);
        }

        public async Task<LectureStatusDto> GetLectureStatusAsync(int lectureId)
        {
            using (var db = new NotificationDbContext())
            {
                var status = await db.TeacherLectureStatuses.AsNoTracking().FirstOrDefaultAsync(s => s.LectureId == lectureId);
                if (status == null)
                {
                    return new LectureStatusDto
                    {
                        LectureId = lectureId,
                        CurrentStatus = "Scheduled",
                        StatusArabic = "مجدولة"
                    };
                }

                return new LectureStatusDto
                {
                    LectureId = status.LectureId,
                    CurrentStatus = status.CurrentStatus,
                    StatusArabic = TranslateStatus(status.CurrentStatus),
                    TeacherId = status.TeacherId,
                    TeacherName = status.TeacherName,
                    ActualCompletionDate = status.ActualCompletionDate,
                    ActualCompletionDateFormatted = status.ActualCompletionDate.HasValue ? FormatLocalTime(status.ActualCompletionDate.Value) : null,
                    Notes = status.Notes,
                    PostponeReason = status.PostponeReason,
                    UpdatedDate = status.UpdatedDate
                };
            }
        }

        public async Task<Dictionary<int, LectureStatusDto>> GetLectureStatusesForCategoryAsync(int categoryId)
        {
            var result = new Dictionary<int, LectureStatusDto>();
            using (var efDb = new tbseedDBEntities())
            using (var notifDb = new NotificationDbContext())
            {
                var lectureIds = await efDb.Lectures
                    .Where(l => l.Category_ID == categoryId)
                    .Select(l => l.ID)
                    .ToListAsync();

                if (!lectureIds.Any()) return result;

                var statuses = await notifDb.TeacherLectureStatuses
                    .AsNoTracking()
                    .Where(s => lectureIds.Contains(s.LectureId))
                    .ToDictionaryAsync(s => s.LectureId);

                foreach (var id in lectureIds)
                {
                    if (statuses.TryGetValue(id, out var status))
                    {
                        result[id] = new LectureStatusDto
                        {
                            LectureId = status.LectureId,
                            CurrentStatus = status.CurrentStatus,
                            StatusArabic = TranslateStatus(status.CurrentStatus),
                            TeacherId = status.TeacherId,
                            TeacherName = status.TeacherName,
                            ActualCompletionDate = status.ActualCompletionDate,
                            ActualCompletionDateFormatted = status.ActualCompletionDate.HasValue ? FormatLocalTime(status.ActualCompletionDate.Value) : null,
                            Notes = status.Notes,
                            PostponeReason = status.PostponeReason,
                            UpdatedDate = status.UpdatedDate
                        };
                    }
                    else
                    {
                        result[id] = new LectureStatusDto
                        {
                            LectureId = id,
                            CurrentStatus = "Scheduled",
                            StatusArabic = "مجدولة"
                        };
                    }
                }
            }
            return result;
        }

        public async Task<NotificationOperationResult> UpdateLectureStatusAsync(int lectureId, string newStatus, string teacherId, string teacherName, string notes, string postponeReason)
        {
            newStatus = (newStatus ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(newStatus))
            {
                return NotificationOperationResult.Fail("حالة المحاضرة مطلوبة.");
            }

            // Normalization
            if (newStatus.Equals("Completed", StringComparison.OrdinalIgnoreCase) || newStatus == "مكتملة")
                newStatus = "Completed";
            else if (newStatus.Equals("Postponed", StringComparison.OrdinalIgnoreCase) || newStatus == "مؤجلة")
                newStatus = "Postponed";
            else if (newStatus.Equals("Scheduled", StringComparison.OrdinalIgnoreCase) || newStatus == "مجدولة")
                newStatus = "Scheduled";
            else
                return NotificationOperationResult.Fail("حالة المحاضرة غير صالحة.");

            if (newStatus == "Postponed" && string.IsNullOrWhiteSpace(postponeReason))
            {
                return NotificationOperationResult.Fail("سبب التأجيل مطلوب عند تأجيل المحاضرة.");
            }

            using (var efDb = new tbseedDBEntities())
            using (var notifDb = new NotificationDbContext())
            {
                var lecture = await efDb.Lectures.Include(l => l.Category).FirstOrDefaultAsync(l => l.ID == lectureId);
                if (lecture == null)
                {
                    return NotificationOperationResult.Fail("المحاضرة غير موجودة.");
                }

                var existingStatus = await notifDb.TeacherLectureStatuses.FirstOrDefaultAsync(s => s.LectureId == lectureId);
                string oldStatus = existingStatus?.CurrentStatus ?? "Scheduled";

                // Double click / Idempotency check:
                if (existingStatus != null && existingStatus.CurrentStatus == newStatus)
                {
                    if (newStatus == "Completed")
                    {
                        return NotificationOperationResult.Fail("المحاضرة مسجلة كمكتملة بالفعل مسبقاً.");
                    }
                    if (newStatus == "Postponed" && (existingStatus.PostponeReason ?? string.Empty).Trim() == (postponeReason ?? string.Empty).Trim())
                    {
                        return NotificationOperationResult.Fail("المحاضرة مؤجلة بالفعل مسبقاً بنفس السبب.");
                    }
                    if (newStatus == "Scheduled")
                    {
                        return NotificationOperationResult.Fail("المحاضرة مجدولة بالفعل.");
                    }
                }

                DateTime nowUtc = DateTime.UtcNow;
                string actionType;
                string title;
                string details;

                if (newStatus == "Completed")
                {
                    actionType = "LectureCompleted";
                    title = $"إكمال درس: {lecture.Lecture_Name}";
                    details = $"قام المدرس {teacherName} بتأكيد إكمال الدرس في دورة {lecture.Category?.NameAr}";
                    if (!string.IsNullOrWhiteSpace(notes))
                    {
                        details += $" | ملاحظات: {notes.Trim()}";
                    }
                }
                else if (newStatus == "Postponed")
                {
                    actionType = "LecturePostponed";
                    title = $"تأجيل درس: {lecture.Lecture_Name}";
                    details = $"قام المدرس {teacherName} بتأجيل الدرس في دورة {lecture.Category?.NameAr} | سبب التأجيل: {postponeReason.Trim()}";
                    if (!string.IsNullOrWhiteSpace(notes))
                    {
                        details += $" | ملاحظات إضافية: {notes.Trim()}";
                    }
                }
                else // Scheduled (Correction / Reopened)
                {
                    actionType = "LectureReopened";
                    title = $"إعادة فتح درس: {lecture.Lecture_Name}";
                    details = $"قام المدرس {teacherName} بتصحيح حالة الدرس وإعادته إلى مجدول في دورة {lecture.Category?.NameAr}";
                    if (!string.IsNullOrWhiteSpace(notes))
                    {
                        details += $" | سبب التصحيح: {notes.Trim()}";
                    }
                }

                using (var transaction = notifDb.Database.BeginTransaction())
                {
                    try
                    {
                        // 1. Update or Insert TeacherLectureStatus
                        if (existingStatus == null)
                        {
                            existingStatus = new TeacherLectureStatusEntity
                            {
                                LectureId = lectureId,
                                CurrentStatus = newStatus,
                                TeacherId = teacherId,
                                TeacherName = teacherName,
                                ActualCompletionDate = newStatus == "Completed" ? (DateTime?)nowUtc : null,
                                Notes = notes,
                                PostponeReason = newStatus == "Postponed" ? postponeReason : null,
                                CreatedDate = nowUtc,
                                UpdatedDate = nowUtc
                            };
                            notifDb.TeacherLectureStatuses.Add(existingStatus);
                        }
                        else
                        {
                            existingStatus.CurrentStatus = newStatus;
                            existingStatus.TeacherId = teacherId;
                            existingStatus.TeacherName = teacherName;
                            if (newStatus == "Completed")
                            {
                                existingStatus.ActualCompletionDate = nowUtc;
                            }
                            else if (oldStatus == "Completed")
                            {
                                existingStatus.ActualCompletionDate = null;
                            }
                            existingStatus.Notes = notes;
                            existingStatus.PostponeReason = newStatus == "Postponed" ? postponeReason : null;
                            existingStatus.UpdatedDate = nowUtc;
                        }

                        await notifDb.SaveChangesAsync();

                        // 2. Insert Event
                        var eventEntity = new TeacherLectureEventEntity
                        {
                            LectureId = lectureId,
                            CategoryId = lecture.Category_ID,
                            CourseName = lecture.Category?.NameAr ?? "دورة تعليمية",
                            LectureTitle = lecture.Lecture_Name,
                            TeacherId = teacherId,
                            TeacherName = teacherName,
                            ActionType = actionType,
                            OldValue = TranslateStatus(oldStatus),
                            NewValue = TranslateStatus(newStatus),
                            Notes = notes,
                            Details = details,
                            CreatedAt = nowUtc
                        };
                        notifDb.TeacherLectureEvents.Add(eventEntity);
                        await notifDb.SaveChangesAsync();

                        // 3. Distribute to Admin Users
                        await DistributeNotificationToAdminsAsync(notifDb, eventEntity.EventId, title, details, nowUtc);

                        transaction.Commit();
                        return NotificationOperationResult.Ok("تم تحديث حالة المحاضرة بنجاح وإرسال الإشعار للإدارة.");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return NotificationOperationResult.Fail($"حدث خطأ أثناء حفظ حالة المحاضرة: {ex.Message}");
                    }
                }
            }
        }

        public async Task<NotificationOperationResult> RecordLectureCreatedAsync(int lectureId, string lectureTitle, int categoryId, string courseName, string teacherId, string teacherName, string lectureType, string dayAndTime, string link)
        {
            using (var notifDb = new NotificationDbContext())
            using (var transaction = notifDb.Database.BeginTransaction())
            {
                try
                {
                    DateTime nowUtc = DateTime.UtcNow;
                    string typeArabic = lectureType == "1" ? "مباشرة" : "مسجلة";
                    string title = $"إضافة محاضرة جديدة: {lectureTitle}";
                    string details = $"قام المدرس {teacherName} بإضافة محاضرة جديدة ({typeArabic}) في دورة {courseName}";
                    if (lectureType == "1" && !string.IsNullOrWhiteSpace(dayAndTime))
                    {
                        details += $" بموعد: {dayAndTime}";
                    }

                    // Initial status
                    var status = new TeacherLectureStatusEntity
                    {
                        LectureId = lectureId,
                        CurrentStatus = "Scheduled",
                        TeacherId = teacherId,
                        TeacherName = teacherName,
                        CreatedDate = nowUtc,
                        UpdatedDate = nowUtc
                    };
                    notifDb.TeacherLectureStatuses.Add(status);
                    await notifDb.SaveChangesAsync();

                    var ev = new TeacherLectureEventEntity
                    {
                        LectureId = lectureId,
                        CategoryId = categoryId,
                        CourseName = courseName,
                        LectureTitle = lectureTitle,
                        TeacherId = teacherId,
                        TeacherName = teacherName,
                        ActionType = "LectureCreated",
                        OldValue = null,
                        NewValue = $"محاضرة {typeArabic} - {dayAndTime}",
                        Details = details,
                        CreatedAt = nowUtc
                    };
                    notifDb.TeacherLectureEvents.Add(ev);
                    await notifDb.SaveChangesAsync();

                    await DistributeNotificationToAdminsAsync(notifDb, ev.EventId, title, details, nowUtc);

                    transaction.Commit();
                    return NotificationOperationResult.Ok("تم تسجيل إضافة المحاضرة وإشعار الإدارة.");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return NotificationOperationResult.Fail($"فشل تسجيل إشعار إضافة المحاضرة: {ex.Message}");
                }
            }
        }

        public async Task<NotificationOperationResult> RecordLectureUpdatedAsync(int lectureId, string oldTitle, string newTitle, int categoryId, string courseName, string teacherId, string teacherName, string oldTime, string newTime, string oldLink, string newLink, string oldType, string newType)
        {
            bool titleChanged = (oldTitle ?? "").Trim() != (newTitle ?? "").Trim();
            bool timeChanged = (oldTime ?? "").Trim() != (newTime ?? "").Trim();
            bool linkChanged = (oldLink ?? "").Trim() != (newLink ?? "").Trim();
            bool typeChanged = (oldType ?? "").Trim() != (newType ?? "").Trim();

            // If nothing meaningful changed, do not create duplicate event or notification
            if (!titleChanged && !timeChanged && !linkChanged && !typeChanged)
            {
                return NotificationOperationResult.Ok("لم يطرأ أي تغيير مهم يتطلب إشعاراً.");
            }

            using (var notifDb = new NotificationDbContext())
            using (var transaction = notifDb.Database.BeginTransaction())
            {
                try
                {
                    DateTime nowUtc = DateTime.UtcNow;
                    string actionType;
                    string title;
                    List<string> changes = new List<string>();

                    if (timeChanged && !linkChanged)
                    {
                        actionType = "LectureTimeChanged";
                        title = $"تعديل موعد محاضرة: {newTitle}";
                        changes.Add($"تغيير الموعد من ({oldTime}) إلى ({newTime})");
                    }
                    else if (linkChanged && !timeChanged)
                    {
                        actionType = "LectureLinkChanged";
                        title = $"تعديل رابط محاضرة: {newTitle}";
                        changes.Add("تحديث رابط المحاضرة المباشرة / المسجلة");
                    }
                    else
                    {
                        actionType = "LectureUpdated";
                        title = $"تعديل بيانات محاضرة: {newTitle}";
                        if (titleChanged) changes.Add($"تعديل الاسم من '{oldTitle}' إلى '{newTitle}'");
                        if (timeChanged) changes.Add($"تعديل الموعد إلى '{newTime}'");
                        if (linkChanged) changes.Add("تعديل رابط المحاضرة");
                        if (typeChanged) changes.Add($"تغيير النوع من {(oldType == "1" ? "مباشرة" : "مسجلة")} إلى {(newType == "1" ? "مباشرة" : "مسجلة")}");
                    }

                    string changeSummary = string.Join("، ", changes);
                    string details = $"قام المدرس {teacherName} بتعديل المحاضرة '{newTitle}' في دورة '{courseName}': {changeSummary}";

                    var ev = new TeacherLectureEventEntity
                    {
                        LectureId = lectureId,
                        CategoryId = categoryId,
                        CourseName = courseName,
                        LectureTitle = newTitle,
                        TeacherId = teacherId,
                        TeacherName = teacherName,
                        ActionType = actionType,
                        OldValue = $"الموعد: {oldTime} | الرابط: {oldLink}",
                        NewValue = $"الموعد: {newTime} | الرابط: {newLink}",
                        Details = details,
                        CreatedAt = nowUtc
                    };
                    notifDb.TeacherLectureEvents.Add(ev);
                    await notifDb.SaveChangesAsync();

                    await DistributeNotificationToAdminsAsync(notifDb, ev.EventId, title, details, nowUtc);

                    transaction.Commit();
                    return NotificationOperationResult.Ok("تم تسجيل التعديل وإرسال الإشعار للإدارة.");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return NotificationOperationResult.Fail($"فشل تسجيل إشعار التعديل: {ex.Message}");
                }
            }
        }

        public async Task<NotificationOperationResult> RecordLectureDeletedAsync(int lectureId, string lectureTitle, int categoryId, string courseName, string teacherId, string teacherName)
        {
            using (var notifDb = new NotificationDbContext())
            using (var transaction = notifDb.Database.BeginTransaction())
            {
                try
                {
                    DateTime nowUtc = DateTime.UtcNow;
                    string title = $"حذف محاضرة: {lectureTitle}";
                    string details = $"قام المدرس {teacherName} بحذف المحاضرة '{lectureTitle}' من دورة '{courseName}'";

                    var ev = new TeacherLectureEventEntity
                    {
                        LectureId = lectureId,
                        CategoryId = categoryId,
                        CourseName = courseName,
                        LectureTitle = lectureTitle,
                        TeacherId = teacherId,
                        TeacherName = teacherName,
                        ActionType = "LectureDeleted",
                        OldValue = "محاضرة موجودة",
                        NewValue = "تم الحذف",
                        Details = details,
                        CreatedAt = nowUtc
                    };
                    notifDb.TeacherLectureEvents.Add(ev);
                    await notifDb.SaveChangesAsync();

                    await DistributeNotificationToAdminsAsync(notifDb, ev.EventId, title, details, nowUtc);

                    transaction.Commit();
                    return NotificationOperationResult.Ok("تم تسجيل حذف المحاضرة وإرسال الإشعار للإدارة.");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return NotificationOperationResult.Fail($"فشل تسجيل إشعار الحذف: {ex.Message}");
                }
            }
        }

        public async Task<int> GetUnreadCountAsync(string adminUserId)
        {
            if (string.IsNullOrEmpty(adminUserId)) return 0;
            using (var db = new NotificationDbContext())
            {
                return await db.AdminNotifications
                    .AsNoTracking()
                    .CountAsync(n => n.AdminUserId == adminUserId && !n.IsRead);
            }
        }

        public async Task<List<AdminNotificationItemViewModel>> GetLatestNotificationsAsync(string adminUserId, int count = 5)
        {
            if (string.IsNullOrEmpty(adminUserId)) return new List<AdminNotificationItemViewModel>();

            using (var db = new NotificationDbContext())
            {
                var items = await db.AdminNotifications
                    .Include(n => n.Event)
                    .AsNoTracking()
                    .Where(n => n.AdminUserId == adminUserId)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(count)
                    .ToListAsync();

                return items.Select(n => MapToViewModel(n)).ToList();
            }
        }

        public async Task<NotificationPagedListViewModel> GetNotificationsPagedAsync(string adminUserId, NotificationFilterViewModel filter)
        {
            var result = new NotificationPagedListViewModel
            {
                Filter = filter ?? new NotificationFilterViewModel()
            };

            if (result.Filter.Page < 1) result.Filter.Page = 1;
            if (result.Filter.PageSize < 1) result.Filter.PageSize = 15;

            using (var db = new NotificationDbContext())
            using (var efDb = new tbseedDBEntities())
            {
                var query = db.AdminNotifications
                    .Include(n => n.Event)
                    .AsNoTracking()
                    .Where(n => n.AdminUserId == adminUserId);

                // Unread Count for this admin
                result.TotalUnreadCount = await query.CountAsync(n => !n.IsRead);

                // Apply Filters
                if (!string.IsNullOrWhiteSpace(result.Filter.TeacherName))
                {
                    string tName = result.Filter.TeacherName.Trim();
                    query = query.Where(n => n.Event.TeacherName.Contains(tName));
                }

                if (!string.IsNullOrWhiteSpace(result.Filter.CourseName))
                {
                    string cName = result.Filter.CourseName.Trim();
                    query = query.Where(n => n.Event.CourseName.Contains(cName));
                }

                if (!string.IsNullOrWhiteSpace(result.Filter.ActionType) && result.Filter.ActionType != "all")
                {
                    string aType = result.Filter.ActionType.Trim();
                    query = query.Where(n => n.Event.ActionType == aType);
                }

                if (!string.IsNullOrWhiteSpace(result.Filter.Status))
                {
                    if (result.Filter.Status == "unread")
                    {
                        query = query.Where(n => !n.IsRead);
                    }
                    else if (result.Filter.Status == "read")
                    {
                        query = query.Where(n => n.IsRead);
                    }
                }

                if (!string.IsNullOrWhiteSpace(result.Filter.DateFrom) && DateTime.TryParse(result.Filter.DateFrom, out DateTime dtFrom))
                {
                    // Convert dateFrom to UTC start of day
                    DateTime utcFrom = dtFrom.Date.ToUniversalTime();
                    query = query.Where(n => n.CreatedAt >= utcFrom);
                }

                if (!string.IsNullOrWhiteSpace(result.Filter.DateTo) && DateTime.TryParse(result.Filter.DateTo, out DateTime dtTo))
                {
                    // Convert dateTo to UTC end of day
                    DateTime utcTo = dtTo.Date.AddDays(1).AddTicks(-1).ToUniversalTime();
                    query = query.Where(n => n.CreatedAt <= utcTo);
                }

                result.TotalItems = await query.CountAsync();
                result.PageSize = result.Filter.PageSize;
                result.CurrentPage = result.Filter.Page;
                result.TotalPages = (int)Math.Ceiling((double)result.TotalItems / result.PageSize);
                if (result.TotalPages == 0) result.TotalPages = 1;

                var pagedEntities = await query
                    .OrderByDescending(n => n.CreatedAt)
                    .Skip((result.CurrentPage - 1) * result.PageSize)
                    .Take(result.PageSize)
                    .ToListAsync();

                // Check which lectures still exist in database
                var activeLectureIds = new HashSet<int>(
                    await efDb.Lectures.Select(l => l.ID).ToListAsync()
                );

                result.Items = pagedEntities.Select(n =>
                {
                    var vm = MapToViewModel(n);
                    vm.LectureExists = vm.LectureId.HasValue && activeLectureIds.Contains(vm.LectureId.Value);
                    return vm;
                }).ToList();

                // Dropdown filter population
                result.AvailableTeachers = await db.TeacherLectureEvents
                    .AsNoTracking()
                    .Select(e => e.TeacherName)
                    .Distinct()
                    .OrderBy(t => t)
                    .ToListAsync();

                result.AvailableCourses = await db.TeacherLectureEvents
                    .AsNoTracking()
                    .Select(e => e.CourseName)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                result.AvailableActionTypes = ActionMeta.ToDictionary(k => k.Key, v => v.Value.Arabic);
            }

            return result;
        }

        public async Task<bool> MarkAsReadAsync(int notificationId, string adminUserId)
        {
            if (string.IsNullOrEmpty(adminUserId)) return false;

            using (var db = new NotificationDbContext())
            {
                var notif = await db.AdminNotifications
                    .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.AdminUserId == adminUserId);

                if (notif == null) return false;

                if (!notif.IsRead)
                {
                    notif.IsRead = true;
                    notif.ReadAt = DateTime.UtcNow;
                    await db.SaveChangesAsync();
                }
                return true;
            }
        }

        public async Task<int> MarkAllAsReadAsync(string adminUserId)
        {
            if (string.IsNullOrEmpty(adminUserId)) return 0;

            using (var db = new NotificationDbContext())
            {
                var unreadList = await db.AdminNotifications
                    .Where(n => n.AdminUserId == adminUserId && !n.IsRead)
                    .ToListAsync();

                if (!unreadList.Any()) return 0;

                DateTime nowUtc = DateTime.UtcNow;
                foreach (var item in unreadList)
                {
                    item.IsRead = true;
                    item.ReadAt = nowUtc;
                }

                await db.SaveChangesAsync();
                return unreadList.Count;
            }
        }

        public async Task<AdminNotificationItemViewModel> GetNotificationDetailsAsync(int notificationId, string adminUserId)
        {
            if (string.IsNullOrEmpty(adminUserId)) return null;

            using (var db = new NotificationDbContext())
            using (var efDb = new tbseedDBEntities())
            {
                var notif = await db.AdminNotifications
                    .Include(n => n.Event)
                    .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.AdminUserId == adminUserId);

                if (notif == null) return null;

                // Auto-mark as read when details are viewed
                if (!notif.IsRead)
                {
                    notif.IsRead = true;
                    notif.ReadAt = DateTime.UtcNow;
                    await db.SaveChangesAsync();
                }

                var vm = MapToViewModel(notif);
                vm.LectureExists = vm.LectureId.HasValue && await efDb.Lectures.AnyAsync(l => l.ID == vm.LectureId.Value);
                return vm;
            }
        }

        private async Task DistributeNotificationToAdminsAsync(NotificationDbContext db, int eventId, string title, string message, DateTime createdAtUtc)
        {
            // Find active Admin users
            var sql = @"SELECT u.Id 
                        FROM AspNetUsers u 
                        JOIN AspNetUserRoles ur ON u.Id = ur.UserId 
                        JOIN AspNetRoles r ON ur.RoleId = r.Id 
                        WHERE r.Name = 'Admin' AND u.IsActive = 1";

            var adminIds = await db.Database.SqlQuery<string>(sql).ToListAsync();

            if (!adminIds.Any())
            {
                // Fallback to active staff users (userOrTetcherOrStudent == 0)
                adminIds = await db.Database.SqlQuery<string>("SELECT Id FROM AspNetUsers WHERE userOrTetcherOrStudent = 0 AND IsActive = 1").ToListAsync();
            }

            foreach (var adminId in adminIds.Distinct())
            {
                db.AdminNotifications.Add(new AdminNotificationEntity
                {
                    EventId = eventId,
                    AdminUserId = adminId,
                    Title = title,
                    Message = message,
                    IsRead = false,
                    CreatedAt = createdAtUtc
                });
            }

            await db.SaveChangesAsync();
        }

        private static AdminNotificationItemViewModel MapToViewModel(AdminNotificationEntity entity)
        {
            var ev = entity.Event;
            return new AdminNotificationItemViewModel
            {
                NotificationId = entity.NotificationId,
                EventId = entity.EventId,
                Title = entity.Title,
                Message = entity.Message,
                IsRead = entity.IsRead,
                CreatedAtUtc = entity.CreatedAt,
                CreatedAtFormatted = FormatLocalTime(entity.CreatedAt),
                TimeAgo = GetTimeAgo(entity.CreatedAt),
                TeacherId = ev?.TeacherId,
                TeacherName = ev?.TeacherName ?? "مدرس",
                LectureId = ev?.LectureId,
                LectureTitle = ev?.LectureTitle ?? "محاضرة",
                CategoryId = ev?.CategoryId,
                CourseName = ev?.CourseName ?? "دورة تعليمية",
                ActionType = ev?.ActionType,
                ActionTypeArabic = GetActionArabic(ev?.ActionType),
                ActionBadgeClass = GetActionBadgeClass(ev?.ActionType),
                OldValue = ev?.OldValue,
                NewValue = ev?.NewValue,
                Notes = ev?.Notes,
                Details = ev?.Details ?? entity.Message
            };
        }

        private static string TranslateStatus(string status)
        {
            switch (status?.Trim())
            {
                case "Completed": return "مكتملة";
                case "Postponed": return "مؤجلة";
                case "Scheduled":
                default:
                    return "مجدولة";
            }
        }
    }
}

