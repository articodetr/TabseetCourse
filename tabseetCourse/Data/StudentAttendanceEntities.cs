using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;

namespace tabseetCourse.Data
{
    /// <summary>
    /// تعيين كامل لجدول std_lecture (الأعلام القديمة + أعمدة الحضور الجديدة من ترحيل 20261024_StudentLectureAttendance.sql).
    /// يُستخدم عبر سياق مستقل على نمط نظام الإشعارات حتى لا يُمس ملف EDMX الحالي.
    /// </summary>
    [Table("std_lecture")]
    public class StudentLectureAttendanceEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        public int? Lecture_ID { get; set; }

        public string Std_ID { get; set; }

        public int? IsFinish { get; set; }

        public int? IsTimeEnd { get; set; }

        public int? IsAssingmetFinish { get; set; }

        /// <summary>أول بدء تشغيل (UTC).</summary>
        public DateTime? StartedAt { get; set; }

        /// <summary>آخر نبضة أو بدء (UTC) — تُستخدم لتقييد الثواني المحتسبة بالزمن الفعلي.</summary>
        public DateTime? LastHeartbeatAt { get; set; }

        /// <summary>مجموع ثواني التشغيل الفعلي المحتسبة.</summary>
        public int WatchedSeconds { get; set; }

        /// <summary>الثواني المطلوبة لاحتساب المحاضرة (تُحسب عند البدء وتُحدَّث عند معرفة مدة الفيديو).</summary>
        public int? RequiredSeconds { get; set; }

        /// <summary>وقت الاكتمال (UTC).</summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>Recorded | Live | Assignment | Manual | Legacy</summary>
        [MaxLength(20)]
        public string Source { get; set; }

        /// <summary>معرّف المدرس عند التسجيل اليدوي.</summary>
        [MaxLength(128)]
        public string MarkedBy { get; set; }

        public DateTime? MarkedAt { get; set; }
    }

    public class StudentAttendanceDbContext : DbContext
    {
        public StudentAttendanceDbContext() : base("TabseetConnection")
        {
            Database.SetInitializer<StudentAttendanceDbContext>(null);
            Configuration.LazyLoadingEnabled = false;
            Configuration.ProxyCreationEnabled = false;
        }

        public virtual DbSet<StudentLectureAttendanceEntity> StudentLectures { get; set; }
    }
}
