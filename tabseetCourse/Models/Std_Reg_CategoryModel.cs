using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace tabseetCourse.Models
{
    public class Std_Reg_CategoryModel
    {
        public int ID { get; set; }
        public int Category_ID { get; set; }
        public string CategoryName { get; set; }
        public string Std_ID { get; set; }
        public string Student_Name { get; set; }
        public int category_Ispayed { get; set; }
        public int std_category_Ispayed { get; set; }
        public string categoryType_Nmae { get; set; }
        public int categoryPrice { get; set; }
        public int Exam_ID { get; set; }
        public int std_exam_isFinish { get; set; }
        public int PayMent_Method { get; set; }

        public string Icon { get; set; }
        public HttpPostedFileBase FileUpload { get; set; }
        public string TransactionFile { get; set; }
        public string Code { get; set; }
        public string PayMent_Value { get; set; }

        public int lectureCount { get; set; }
        public int lectureFinishCount { get; set; }

        public int New_Page_ID { get; set; }
        public string DocumentBook { get; set; }

        /// <summary>
        /// Doplom=0
        /// Course=1
        /// </summary>
        public int Std_Reg_CategoryType { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int Std_Reg_CategoryLecture { get; set; }


        public string certificate { get; set; }
        public string certificate_leave { get; set; }

        public string TeacherName { get; set; }
        public string Description { get; set; }
        public List<LectureModel> Lectures { get; set; }

        /// <summary>حالة الاشتراك المحسوبة على الخادم: NotRegistered | PendingApproval | Active (Services/CourseAccessService).</summary>
        public string SubscriptionState { get; set; }
        public bool IsSubscriptionActive { get; set; }
        public bool IsSubscriptionPending { get; set; }
        /// <summary>تاريخ طلب الاشتراك (من أعمدة ترحيل 20261024_SubscriptionApproval).</summary>
        public DateTime? RequestedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string Student_Email { get; set; }

        public Std_Reg_CategoryModel()
        {
            Lectures = new List<LectureModel>();
        }
    }
}
