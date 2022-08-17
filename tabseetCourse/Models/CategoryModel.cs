using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace tabseetCourse.Models
{
    public class CategoryModel
    {
        public int ID { get; set; }
        public string NameAr { get; set; }
        public string NameEn { get; set; }
        public  int  ParentCateogry_ID { get; set; }
        public string ParentCategoryName { get; set; }
        public  int  IsPayed { get; set; }
        public string PayedValue { get; set; }
        public string Description { get; set; }
        public int CategoryType_ID { get; set; }
        public string CategoryTypeName { get; set; }
 
        public int Exam_ID { get; set; }
        public int lectureCount { get; set; }
        public string exam_Name { get; set; }
        public int New_Page_ID { get; set; } 
        public HttpPostedFileBase FileImagePost { get; set; }
        public HttpPostedFileBase FileImagePost_EN { get; set; }
        public string FileImage { get; set; }
        
        public string timeEnd { get; set; }
        public int CanRegister { get; set; }

        public string Icon { get; set; }
        public string DocumentBook { get; set; }
        public HttpPostedFileBase UploadIcon { get; set; }
        public HttpPostedFileBase UploadDocumentBook { get; set; }


    }
}