using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;

namespace tabseetCourse.Data
{
    [Table("TeacherLectureStatus")]
    public class TeacherLectureStatusEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int LectureId { get; set; }

        [Required]
        [MaxLength(50)]
        public string CurrentStatus { get; set; } // Scheduled, Completed, Postponed

        [MaxLength(128)]
        public string TeacherId { get; set; }

        [MaxLength(256)]
        public string TeacherName { get; set; }

        public DateTime? ActualCompletionDate { get; set; }

        public string Notes { get; set; }

        public string PostponeReason { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    }

    [Table("TeacherLectureEvents")]
    public class TeacherLectureEventEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int EventId { get; set; }

        public int? LectureId { get; set; }

        public int? CategoryId { get; set; }

        [Required]
        [MaxLength(256)]
        public string CourseName { get; set; }

        [Required]
        [MaxLength(256)]
        public string LectureTitle { get; set; }

        [MaxLength(128)]
        public string TeacherId { get; set; }

        [Required]
        [MaxLength(256)]
        public string TeacherName { get; set; }

        [Required]
        [MaxLength(100)]
        public string ActionType { get; set; } // LectureCompleted, LecturePostponed, LectureReopened, LectureCreated, LectureTimeChanged, LectureLinkChanged, LectureUpdated, LectureDeleted

        public string OldValue { get; set; }

        public string NewValue { get; set; }

        public string Notes { get; set; }

        public string Details { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<AdminNotificationEntity> Notifications { get; set; } = new HashSet<AdminNotificationEntity>();
    }

    [Table("AdminNotifications")]
    public class AdminNotificationEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int NotificationId { get; set; }

        public int EventId { get; set; }

        [ForeignKey("EventId")]
        public virtual TeacherLectureEventEntity Event { get; set; }

        [Required]
        [MaxLength(128)]
        public string AdminUserId { get; set; }

        [Required]
        [MaxLength(256)]
        public string Title { get; set; }

        [Required]
        public string Message { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime? ReadAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class NotificationDbContext : DbContext
    {
        public NotificationDbContext() : base("TabseetConnection")
        {
            Database.SetInitializer<NotificationDbContext>(null);
            Configuration.LazyLoadingEnabled = true;
            Configuration.ProxyCreationEnabled = true;
        }

        public virtual DbSet<TeacherLectureStatusEntity> TeacherLectureStatuses { get; set; }
        public virtual DbSet<TeacherLectureEventEntity> TeacherLectureEvents { get; set; }
        public virtual DbSet<AdminNotificationEntity> AdminNotifications { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TeacherLectureStatusEntity>()
                .ToTable("TeacherLectureStatus");

            modelBuilder.Entity<TeacherLectureEventEntity>()
                .ToTable("TeacherLectureEvents");

            modelBuilder.Entity<AdminNotificationEntity>()
                .ToTable("AdminNotifications");
        }
    }
}

