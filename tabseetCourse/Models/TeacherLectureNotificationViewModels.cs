using System;
using System.Collections.Generic;

namespace tabseetCourse.Models
{
    public class LectureStatusDto
    {
        public int LectureId { get; set; }
        public string CurrentStatus { get; set; } = "Scheduled"; // Scheduled, Completed, Postponed
        public string StatusArabic { get; set; } = "مجدولة";
        public string TeacherId { get; set; }
        public string TeacherName { get; set; }
        public DateTime? ActualCompletionDate { get; set; }
        public string ActualCompletionDateFormatted { get; set; }
        public string Notes { get; set; }
        public string PostponeReason { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    public class UpdateLectureStatusRequest
    {
        public int LectureId { get; set; }
        public string Status { get; set; } // Scheduled, Completed, Postponed
        public string Notes { get; set; }
        public string PostponeReason { get; set; }
    }

    public class AdminNotificationItemViewModel
    {
        public int NotificationId { get; set; }
        public int EventId { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public string CreatedAtFormatted { get; set; }
        public string TimeAgo { get; set; }
        public string TeacherId { get; set; }
        public string TeacherName { get; set; }
        public int? LectureId { get; set; }
        public string LectureTitle { get; set; }
        public int? CategoryId { get; set; }
        public string CourseName { get; set; }
        public string ActionType { get; set; }
        public string ActionTypeArabic { get; set; }
        public string ActionBadgeClass { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public string Notes { get; set; }
        public string Details { get; set; }
        public bool LectureExists { get; set; }
    }

    public class NotificationFilterViewModel
    {
        public string TeacherName { get; set; }
        public string CourseName { get; set; }
        public string ActionType { get; set; }
        public string Status { get; set; } = "all"; // all, unread, read
        public string DateFrom { get; set; }
        public string DateTo { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 15;
    }

    public class NotificationPagedListViewModel
    {
        public List<AdminNotificationItemViewModel> Items { get; set; } = new List<AdminNotificationItemViewModel>();
        public NotificationFilterViewModel Filter { get; set; } = new NotificationFilterViewModel();
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalUnreadCount { get; set; }
        public List<string> AvailableTeachers { get; set; } = new List<string>();
        public List<string> AvailableCourses { get; set; } = new List<string>();
        public Dictionary<string, string> AvailableActionTypes { get; set; } = new Dictionary<string, string>();
    }

    public class NotificationOperationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }

        public static NotificationOperationResult Ok(string message = null, object data = null)
        {
            return new NotificationOperationResult { Success = true, Message = message, Data = data };
        }

        public static NotificationOperationResult Fail(string message, object data = null)
        {
            return new NotificationOperationResult { Success = false, Message = message, Data = data };
        }
    }
}

