using System;
using System.Configuration;

namespace tabseetCourse.Services
{
    /// <summary>
    /// سياسة احتساب حضور المحاضرات. القيم قابلة للضبط من Web.config (appSettings) دون تعديل الكود:
    ///   Attendance:WatchPercent            النسبة المطلوبة من مدة الفيديو عندما لا يحدد المدرس حداً أدنى بالدقائق (افتراضي 80)
    ///   Attendance:DefaultRequiredSeconds  الثواني المطلوبة عندما لا يُعرف الحد الأدنى ولا مدة الفيديو (افتراضي 60)
    ///   Attendance:HeartbeatSeconds        الفاصل بين نبضات المتصفح أثناء التشغيل (افتراضي 30)
    ///   Attendance:LiveJoinBeforeMinutes   يُقبل دخول المحاضرة المباشرة قبل موعدها بهذه الدقائق (افتراضي 30)
    ///   Attendance:LiveJoinAfterMinutes    ويُقبل بعد موعدها بهذه الدقائق (افتراضي 180)
    ///   Attendance:AssignmentAloneCompletes اجتياز الواجب وحده يكمل المحاضرة دون مشاهدة (افتراضي false)
    /// </summary>
    public static class AttendancePolicy
    {
        /// <summary>سماحية بالثواني لتعويض زمن الشبكة عند مقارنة ما يبلّغه المتصفح بالزمن الفعلي المنقضي.</summary>
        public const int HeartbeatToleranceSeconds = 5;

        public static int WatchPercent { get { return Clamp(ReadInt("Attendance:WatchPercent", 80), 1, 100); } }
        public static int DefaultRequiredSeconds { get { return Clamp(ReadInt("Attendance:DefaultRequiredSeconds", 60), 1, 86400); } }
        public static int HeartbeatSeconds { get { return Clamp(ReadInt("Attendance:HeartbeatSeconds", 30), 5, 600); } }
        public static int LiveJoinBeforeMinutes { get { return Clamp(ReadInt("Attendance:LiveJoinBeforeMinutes", 30), 0, 1440); } }
        public static int LiveJoinAfterMinutes { get { return Clamp(ReadInt("Attendance:LiveJoinAfterMinutes", 180), 0, 1440); } }
        public static bool AssignmentAloneCompletes { get { return ReadBool("Attendance:AssignmentAloneCompletes", false); } }

        /// <summary>
        /// الثواني المطلوبة لاحتساب المحاضرة المسجلة:
        /// الحد الأدنى بالدقائق الذي حدده المدرس (ولا يتجاوز مدة الفيديو إن عُرفت)،
        /// وإلا نسبة WatchPercent من مدة الفيديو، وإلا القيمة الافتراضية.
        /// </summary>
        public static int ComputeRequiredSeconds(int lectureMinTimeMinutes, int? videoDurationSeconds)
        {
            int duration = videoDurationSeconds.HasValue && videoDurationSeconds.Value > 0 ? videoDurationSeconds.Value : 0;

            if (lectureMinTimeMinutes > 0)
            {
                long required = (long)lectureMinTimeMinutes * 60;
                if (duration > 0 && required > duration)
                {
                    required = duration;
                }
                return (int)Math.Max(1, Math.Min(required, 86400));
            }

            if (duration > 0)
            {
                return Math.Max(1, (int)Math.Ceiling(duration * WatchPercent / 100.0));
            }

            return DefaultRequiredSeconds;
        }

        /// <summary>
        /// الثواني التي تُضاف فعلاً من نبضة واحدة: لا تتجاوز ما بلّغه المتصفح، ولا الزمن الفعلي المنقضي منذ النبضة السابقة (+ سماحية)،
        /// ولا ضعف فاصل النبضات (حماية من التلاعب بالطلبات).
        /// </summary>
        public static int ComputeCredit(int reportedSeconds, double wallClockSecondsSinceLast, int heartbeatSeconds)
        {
            if (reportedSeconds <= 0)
                return 0;

            int cap = Math.Max(1, heartbeatSeconds) * 2 + HeartbeatToleranceSeconds;
            double wall = double.IsNaN(wallClockSecondsSinceLast) || wallClockSecondsSinceLast < 0 ? 0 : wallClockSecondsSinceLast;
            int allowed = (int)Math.Floor(wall) + HeartbeatToleranceSeconds;

            return Math.Max(0, Math.Min(reportedSeconds, Math.Min(cap, allowed)));
        }

        /// <summary>هل الآن ضمن نافذة قبول دخول المحاضرة المباشرة؟</summary>
        public static bool IsWithinLiveWindow(DateTime lectureStart, DateTime now)
        {
            return now >= lectureStart.AddMinutes(-LiveJoinBeforeMinutes) && now <= lectureStart.AddMinutes(LiveJoinAfterMinutes);
        }

        /// <summary>قاعدة الاكتمال الموحدة: انقضى الوقت المطلوب، والواجب مُجتاز إن وُجد.</summary>
        public static bool IsLectureFinished(bool timeEnd, bool hasAssignment, bool assignmentFinished)
        {
            return timeEnd && (!hasAssignment || assignmentFinished);
        }

        private static int ReadInt(string key, int fallback)
        {
            try
            {
                string raw = ConfigurationManager.AppSettings[key];
                int value;
                if (!string.IsNullOrWhiteSpace(raw) && int.TryParse(raw.Trim(), out value))
                    return value;
            }
            catch { }
            return fallback;
        }

        private static bool ReadBool(string key, bool fallback)
        {
            try
            {
                string raw = ConfigurationManager.AppSettings[key];
                bool value;
                if (!string.IsNullOrWhiteSpace(raw) && bool.TryParse(raw.Trim(), out value))
                    return value;
            }
            catch { }
            return fallback;
        }

        private static int Clamp(int value, int min, int max)
        {
            return value < min ? min : (value > max ? max : value);
        }
    }
}
