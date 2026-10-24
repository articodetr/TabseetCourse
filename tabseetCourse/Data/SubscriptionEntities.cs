using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;

namespace tabseetCourse.Data
{
    /// <summary>
    /// تعيين جزئي لجدول Std_Reg_Category يشمل أعمدة التوثيق المضافة في ترحيل
    /// 20261024_SubscriptionApproval.sql (RequestedAt / ApprovedAt / ApprovedBy).
    /// يُستخدم عبر سياق مستقل على نمط نظامي الحضور والإشعارات حتى لا يُمس ملف EDMX الحالي.
    /// أسماء الأعمدة القديمة أُبقيت كما هي للتوافق (category_Ispayed = الدورة مدفوعة،
    /// std_category_Ispayed = الإدارة أكدت الدفع وفعّلت الاشتراك).
    /// </summary>
    [Table("Std_Reg_Category")]
    public class StudentSubscriptionEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        public int? Category_ID { get; set; }

        [MaxLength(256)]
        public string Std_ID { get; set; }

        /// <summary>1 = الدورة مدفوعة (تحتاج موافقة الإدارة)، 0 = مجانية.</summary>
        public int? category_Ispayed { get; set; }

        /// <summary>1 = الاشتراك مفعّل بموافقة الإدارة (للدورات المدفوعة).</summary>
        public int? std_category_Ispayed { get; set; }

        /// <summary>تاريخ طلب الاشتراك (UTC).</summary>
        public DateTime? RequestedAt { get; set; }

        /// <summary>تاريخ موافقة الإدارة (UTC).</summary>
        public DateTime? ApprovedAt { get; set; }

        /// <summary>معرّف حساب الإدارة الذي اعتمد الطلب.</summary>
        [MaxLength(128)]
        public string ApprovedBy { get; set; }
    }

    public class SubscriptionDbContext : DbContext
    {
        public SubscriptionDbContext() : base("TabseetConnection")
        {
            Database.SetInitializer<SubscriptionDbContext>(null);
            Configuration.LazyLoadingEnabled = false;
            Configuration.ProxyCreationEnabled = false;
        }

        public virtual DbSet<StudentSubscriptionEntity> Subscriptions { get; set; }
    }
}
