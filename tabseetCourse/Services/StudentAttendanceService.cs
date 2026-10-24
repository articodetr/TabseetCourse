using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using tabseetCourse.Data;

namespace tabseetCourse.Services
{
    /// <summary>حالة حضور طالب في محاضرة (تُعاد للمتصفح ولصفحات المدرس).</summary>
    public class AttendanceStatusModel
    {
        public int LectureId { get; set; }
        public int WatchedSeconds { get; set; }
        public int RequiredSeconds { get; set; }
        public bool IsTimeEnd { get; set; }
        public bool HasAssignment { get; set; }
        public bool IsAssignmentFinish { get; set; }
        public bool IsFinish { get; set; }
        public string Source { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public int ProgressPercent
        {
            get
            {
                if (IsTimeEnd) return 100;
                if (RequiredSeconds <= 0) return 0;
                return Math.Min(100, (int)Math.Floor(WatchedSeconds * 100.0 / RequiredSeconds));
            }
        }
    }

    /// <summary>صف في قائمة حضور محاضرة (لصفحة المدرس) أو تقرير الإدارة.</summary>
    public class AttendanceReportRowModel
    {
        public string StudentId { get; set; }
        public string StudentName { get; set; }
        public string Email { get; set; }
        public int LectureId { get; set; }
        public string LectureName { get; set; }
        public string LectureType { get; set; }
        public int WatchedSeconds { get; set; }
        public int RequiredSeconds { get; set; }
        public bool IsTimeEnd { get; set; }
        public bool HasAssignment { get; set; }
        public bool IsAssignmentFinish { get; set; }
        public bool IsFinish { get; set; }
        public string Source { get; set; }
        public string MarkedBy { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        /// <summary>مكتملة / جزئية / لم تبدأ</summary>
        public string StatusKey
        {
            get
            {
                if (IsFinish) return "Finished";
                if (IsTimeEnd || WatchedSeconds > 0 || IsAssignmentFinish) return "Partial";
                return "NotStarted";
            }
        }
    }

    /// <summary>
    /// خدمة احتساب حضور الطلاب: البدء والنبضات للمحاضرات المسجلة، دخول المحاضرات المباشرة، اجتياز الواجب،
    /// التسجيل اليدوي من المدرس، وقوائم الحضور. القواعد في <see cref="AttendancePolicy"/>.
    /// الأعلام القديمة (IsTimeEnd / IsAssingmetFinish / IsFinish) تبقى هي مرجع الامتحانات والشهادات كما كانت.
    /// </summary>
    public class StudentAttendanceService
    {
        public const string SourceRecorded = "Recorded";
        public const string SourceLive = "Live";
        public const string SourceAssignment = "Assignment";
        public const string SourceManual = "Manual";

        private static readonly string[] KnownDateFormats =
        {
            "yyyy-MM-dd HH:mm:ss", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm", "yyyy-MM-dd HH:mm", "yyyy-MM-dd",
            "MM/dd/yyyy HH:mm:ss", "MM/dd/yyyy HH:mm", "MM/dd/yyyy h:mm tt", "MM/dd/yyyy h:mm", "MM/dd/yyyy",
            "dd/MM/yyyy HH:mm:ss", "dd/MM/yyyy HH:mm"
        };

        private readonly tbseedDBEntities _db;

        public StudentAttendanceService(tbseedDBEntities db)
        {
            _db = db;
        }

        public static bool TryParseLectureDate(string raw, out DateTime value)
        {
            value = DateTime.MinValue;
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            string trimmed = raw.Trim();
            if (DateTime.TryParseExact(trimmed, KnownDateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out value))
                return true;
            return DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out value);
        }

        public static int ParseMinTimeMinutes(string lectureMinTime)
        {
            int minutes;
            return int.TryParse((lectureMinTime ?? string.Empty).Trim(), out minutes) && minutes > 0 ? minutes : 0;
        }

        public static bool HasAssignment(Lecture lecture)
        {
            return lecture != null && lecture.Assignments != null && lecture.Assignments.Any();
        }

        /// <summary>
        /// الطالب يحق له الوصول إلى دورة المحاضرة: اشتراك مفعّل (مجاني، أو مدفوع اعتمدته الإدارة) في الدورة
        /// أو في الدورة الأم (الدبلوم). الاشتراك المعلّق بانتظار الإدارة لا يُعدّ تسجيلاً (CourseAccessService).
        /// </summary>
        public bool IsStudentRegistered(string studentId, Lecture lecture)
        {
            if (string.IsNullOrEmpty(studentId) || lecture == null)
                return false;

            return new CourseAccessService(_db).CanAccess(studentId, lecture.Category_ID);
        }

        /// <summary>معرّفات الطلاب المسجلين في دورة المحاضرة أو دورتها الأم.</summary>
        public List<string> GetRegisteredStudentIds(Lecture lecture)
        {
            int catId = lecture.Category_ID;
            int parentId = lecture.Category != null && lecture.Category.ParentCateogry_ID.HasValue ? lecture.Category.ParentCateogry_ID.Value : -1;
            return _db.Std_Reg_Category
                .Where(r => r.Std_ID != null && (r.Category_ID == catId || r.Category_ID == parentId))
                .Select(r => r.Std_ID)
                .Distinct()
                .ToList();
        }

        public List<string> GetRegisteredStudentIds(Category category)
        {
            int catId = category.ID;
            int parentId = category.ParentCateogry_ID.HasValue ? category.ParentCateogry_ID.Value : -1;
            return _db.Std_Reg_Category
                .Where(r => r.Std_ID != null && (r.Category_ID == catId || r.Category_ID == parentId))
                .Select(r => r.Std_ID)
                .Distinct()
                .ToList();
        }

        /// <summary>بدء (أو استئناف) مشاهدة محاضرة مسجلة. يعيد الحالة الحالية والثواني المطلوبة.</summary>
        public AttendanceStatusModel StartRecorded(string studentId, Lecture lecture, int? durationSeconds)
        {
            int minTime = ParseMinTimeMinutes(lecture.LectureMinTime);
            bool hasAssignment = HasAssignment(lecture);
            DateTime now = DateTime.UtcNow;

            using (var ctx = new StudentAttendanceDbContext())
            {
                bool created;
                var row = FindOrCreate(ctx, studentId, lecture.ID, out created);
                if (row.StartedAt == null) row.StartedAt = now;
                row.LastHeartbeatAt = now;
                row.RequiredSeconds = AttendancePolicy.ComputeRequiredSeconds(minTime, durationSeconds);
                if (string.IsNullOrEmpty(row.Source) && row.IsFinish != 1) row.Source = SourceRecorded;
                if (row.IsTimeEnd != 1 && row.WatchedSeconds >= row.RequiredSeconds.Value) row.IsTimeEnd = 1;
                ApplyFinishRule(row, hasAssignment, now);
                ctx.SaveChanges();
                return ToStatus(row, hasAssignment);
            }
        }

        /// <summary>
        /// نبضة من المتصفح أثناء التشغيل: تُضاف ثوانٍ لا تتجاوز ما بلّغه المتصفح ولا الزمن الفعلي المنقضي منذ النبضة السابقة.
        /// </summary>
        public AttendanceStatusModel Heartbeat(string studentId, Lecture lecture, int playedSeconds, int? durationSeconds)
        {
            int minTime = ParseMinTimeMinutes(lecture.LectureMinTime);
            bool hasAssignment = HasAssignment(lecture);
            DateTime now = DateTime.UtcNow;

            using (var ctx = new StudentAttendanceDbContext())
            {
                bool created;
                var row = FindOrCreate(ctx, studentId, lecture.ID, out created);

                int credit = 0;
                if (created || row.LastHeartbeatAt == null)
                {
                    if (row.StartedAt == null) row.StartedAt = now;
                }
                else
                {
                    double wall = (now - row.LastHeartbeatAt.Value).TotalSeconds;
                    credit = AttendancePolicy.ComputeCredit(playedSeconds, wall, AttendancePolicy.HeartbeatSeconds);
                }
                row.LastHeartbeatAt = now;

                if (row.RequiredSeconds == null || row.RequiredSeconds <= 0 || (durationSeconds.HasValue && durationSeconds.Value > 0))
                {
                    row.RequiredSeconds = AttendancePolicy.ComputeRequiredSeconds(minTime, durationSeconds);
                }

                if (credit > 0)
                {
                    long total = (long)row.WatchedSeconds + credit;
                    row.WatchedSeconds = total > int.MaxValue ? int.MaxValue : (int)total;
                }

                if (string.IsNullOrEmpty(row.Source) && row.IsFinish != 1) row.Source = SourceRecorded;
                if (row.IsTimeEnd != 1 && row.WatchedSeconds >= row.RequiredSeconds.Value) row.IsTimeEnd = 1;
                ApplyFinishRule(row, hasAssignment, now);
                ctx.SaveChanges();
                return ToStatus(row, hasAssignment);
            }
        }

        /// <summary>تسجيل دخول محاضرة مباشرة ضمن نافذة موعدها. يعيد null مع رسالة خطأ إن كان خارج الموعد.</summary>
        public AttendanceStatusModel JoinLive(string studentId, Lecture lecture, DateTime localNow, out string error)
        {
            error = null;
            DateTime start;
            if (!TryParseLectureDate(lecture.DayandTime, out start))
            {
                error = "موعد المحاضرة المباشرة غير محدد، لا يمكن تسجيل الحضور";
                return null;
            }

            if (!AttendancePolicy.IsWithinLiveWindow(start, localNow))
            {
                error = "لا يمكن تسجيل الحضور خارج موعد المحاضرة المباشرة";
                return null;
            }

            bool hasAssignment = HasAssignment(lecture);
            DateTime now = DateTime.UtcNow;
            using (var ctx = new StudentAttendanceDbContext())
            {
                bool created;
                var row = FindOrCreate(ctx, studentId, lecture.ID, out created);
                if (row.StartedAt == null) row.StartedAt = now;
                row.LastHeartbeatAt = now;
                if (row.IsTimeEnd != 1)
                {
                    row.IsTimeEnd = 1;
                    row.Source = SourceLive;
                }
                ApplyFinishRule(row, hasAssignment, now);
                ctx.SaveChanges();
                return ToStatus(row, hasAssignment);
            }
        }

        /// <summary>اجتياز واجب المحاضرة. الاكتمال يبقى مشروطاً بالمشاهدة إلا إذا سمحت السياسة بغير ذلك.</summary>
        public AttendanceStatusModel RecordAssignmentPassed(string studentId, Lecture lecture)
        {
            DateTime now = DateTime.UtcNow;
            using (var ctx = new StudentAttendanceDbContext())
            {
                bool created;
                var row = FindOrCreate(ctx, studentId, lecture.ID, out created);
                row.IsAssingmetFinish = 1;
                if (AttendancePolicy.AssignmentAloneCompletes && row.IsTimeEnd != 1)
                {
                    row.IsTimeEnd = 1;
                    if (string.IsNullOrEmpty(row.Source)) row.Source = SourceAssignment;
                }
                ApplyFinishRule(row, true, now);
                ctx.SaveChanges();
                return ToStatus(row, true);
            }
        }

        /// <summary>تسجيل يدوي من المدرس (حضور أو إلغاء حضور).</summary>
        public AttendanceStatusModel SetManual(string teacherId, Lecture lecture, string studentId, bool attended)
        {
            bool hasAssignment = HasAssignment(lecture);
            DateTime now = DateTime.UtcNow;
            using (var ctx = new StudentAttendanceDbContext())
            {
                bool created;
                var row = FindOrCreate(ctx, studentId, lecture.ID, out created);
                row.Source = SourceManual;
                row.MarkedBy = teacherId;
                row.MarkedAt = now;
                if (attended)
                {
                    if (row.StartedAt == null) row.StartedAt = now;
                    row.IsTimeEnd = 1;
                    ApplyFinishRule(row, hasAssignment, now);
                }
                else
                {
                    row.IsTimeEnd = 0;
                    row.IsFinish = 0;
                    row.CompletedAt = null;
                }
                ctx.SaveChanges();
                return ToStatus(row, hasAssignment);
            }
        }

        public AttendanceStatusModel GetStatus(string studentId, Lecture lecture)
        {
            bool hasAssignment = HasAssignment(lecture);
            using (var ctx = new StudentAttendanceDbContext())
            {
                int lectureId = lecture.ID;
                var row = ctx.StudentLectures.FirstOrDefault(r => r.Std_ID == studentId && r.Lecture_ID == lectureId);
                if (row == null)
                {
                    return new AttendanceStatusModel
                    {
                        LectureId = lectureId,
                        HasAssignment = hasAssignment,
                        RequiredSeconds = AttendancePolicy.ComputeRequiredSeconds(ParseMinTimeMinutes(lecture.LectureMinTime), null)
                    };
                }
                return ToStatus(row, hasAssignment);
            }
        }

        /// <summary>قائمة حضور محاضرة واحدة لكل الطلاب المسجلين في دورتها.</summary>
        public List<AttendanceReportRowModel> GetLectureAttendance(Lecture lecture)
        {
            var studentIds = GetRegisteredStudentIds(lecture);
            var users = LoadUsers(studentIds);
            bool hasAssignment = HasAssignment(lecture);
            int lectureId = lecture.ID;

            Dictionary<string, StudentLectureAttendanceEntity> rows;
            using (var ctx = new StudentAttendanceDbContext())
            {
                rows = ctx.StudentLectures
                    .Where(r => r.Lecture_ID == lectureId && r.Std_ID != null && studentIds.Contains(r.Std_ID))
                    .ToList()
                    .GroupBy(r => r.Std_ID)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.IsFinish ?? 0).ThenBy(r => r.ID).First());
            }

            var result = new List<AttendanceReportRowModel>();
            foreach (var user in users)
            {
                StudentLectureAttendanceEntity row;
                rows.TryGetValue(user.Id, out row);
                result.Add(BuildRow(user, lecture, row, hasAssignment));
            }
            return result.OrderBy(r => r.StudentName, StringComparer.CurrentCulture).ToList();
        }

        /// <summary>تقرير دورة كاملة: كل طالب مسجل × كل محاضرة.</summary>
        public List<AttendanceReportRowModel> GetCategoryAttendanceReport(Category category)
        {
            var studentIds = GetRegisteredStudentIds(category);
            var users = LoadUsers(studentIds);
            var lectures = category.Lectures.OrderBy(l => l.ID).ToList();
            var lectureIds = lectures.Select(l => l.ID).ToList();

            Dictionary<string, StudentLectureAttendanceEntity> rows;
            using (var ctx = new StudentAttendanceDbContext())
            {
                rows = ctx.StudentLectures
                    .Where(r => r.Std_ID != null && r.Lecture_ID != null && lectureIds.Contains(r.Lecture_ID.Value) && studentIds.Contains(r.Std_ID))
                    .ToList()
                    .GroupBy(r => r.Std_ID + "|" + r.Lecture_ID)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.IsFinish ?? 0).ThenBy(r => r.ID).First());
            }

            var result = new List<AttendanceReportRowModel>();
            foreach (var user in users.OrderBy(u => u.RealName ?? u.Email, StringComparer.CurrentCulture))
            {
                foreach (var lecture in lectures)
                {
                    StudentLectureAttendanceEntity row;
                    rows.TryGetValue(user.Id + "|" + lecture.ID, out row);
                    result.Add(BuildRow(user, lecture, row, HasAssignment(lecture)));
                }
            }
            return result;
        }

        private class UserInfo
        {
            public string Id;
            public string RealName;
            public string Email;
        }

        private List<UserInfo> LoadUsers(List<string> studentIds)
        {
            if (studentIds == null || studentIds.Count == 0)
                return new List<UserInfo>();

            return _db.AspNetUsers
                .Where(u => studentIds.Contains(u.Id))
                .Select(u => new { u.Id, u.RealName, u.Email, u.FirstName, u.LastName })
                .ToList()
                .Select(u => new UserInfo
                {
                    Id = u.Id,
                    RealName = !string.IsNullOrWhiteSpace(u.RealName) ? u.RealName.Trim() : ((u.FirstName ?? string.Empty) + " " + (u.LastName ?? string.Empty)).Trim(),
                    Email = u.Email
                })
                .ToList();
        }

        private static AttendanceReportRowModel BuildRow(UserInfo user, Lecture lecture, StudentLectureAttendanceEntity row, bool hasAssignment)
        {
            var model = new AttendanceReportRowModel
            {
                StudentId = user.Id,
                StudentName = string.IsNullOrWhiteSpace(user.RealName) ? user.Email : user.RealName,
                Email = user.Email,
                LectureId = lecture.ID,
                LectureName = lecture.Lecture_Name,
                LectureType = lecture.LectureTypeOnOff,
                HasAssignment = hasAssignment,
                RequiredSeconds = AttendancePolicy.ComputeRequiredSeconds(ParseMinTimeMinutes(lecture.LectureMinTime), null)
            };

            if (row != null)
            {
                model.WatchedSeconds = row.WatchedSeconds;
                if (row.RequiredSeconds.HasValue && row.RequiredSeconds.Value > 0) model.RequiredSeconds = row.RequiredSeconds.Value;
                model.IsTimeEnd = row.IsTimeEnd == 1;
                model.IsAssignmentFinish = row.IsAssingmetFinish == 1;
                model.IsFinish = row.IsFinish == 1;
                model.Source = row.Source;
                model.MarkedBy = row.MarkedBy;
                model.StartedAt = row.StartedAt;
                model.CompletedAt = row.CompletedAt;
            }
            return model;
        }

        private static StudentLectureAttendanceEntity FindOrCreate(StudentAttendanceDbContext ctx, string studentId, int lectureId, out bool created)
        {
            created = false;
            var row = ctx.StudentLectures
                .Where(r => r.Std_ID == studentId && r.Lecture_ID == lectureId)
                .OrderByDescending(r => r.IsFinish ?? 0)
                .ThenBy(r => r.ID)
                .FirstOrDefault();

            if (row == null)
            {
                row = new StudentLectureAttendanceEntity
                {
                    Std_ID = studentId,
                    Lecture_ID = lectureId,
                    IsFinish = 0,
                    IsTimeEnd = 0,
                    IsAssingmetFinish = 0,
                    WatchedSeconds = 0
                };
                ctx.StudentLectures.Add(row);
                created = true;
            }
            return row;
        }

        /// <summary>قاعدة الاكتمال (لا تُلغي اكتمالاً سابقاً؛ الإلغاء يتم يدوياً فقط).</summary>
        private static void ApplyFinishRule(StudentLectureAttendanceEntity row, bool hasAssignment, DateTime now)
        {
            if (AttendancePolicy.IsLectureFinished(row.IsTimeEnd == 1, hasAssignment, row.IsAssingmetFinish == 1))
            {
                if (row.IsFinish != 1)
                {
                    row.IsFinish = 1;
                }
                if (row.CompletedAt == null) row.CompletedAt = now;
            }
        }

        private static AttendanceStatusModel ToStatus(StudentLectureAttendanceEntity row, bool hasAssignment)
        {
            return new AttendanceStatusModel
            {
                LectureId = row.Lecture_ID ?? 0,
                WatchedSeconds = row.WatchedSeconds,
                RequiredSeconds = row.RequiredSeconds ?? 0,
                IsTimeEnd = row.IsTimeEnd == 1,
                HasAssignment = hasAssignment,
                IsAssignmentFinish = row.IsAssingmetFinish == 1,
                IsFinish = row.IsFinish == 1,
                Source = row.Source,
                StartedAt = row.StartedAt,
                CompletedAt = row.CompletedAt
            };
        }
    }
}
