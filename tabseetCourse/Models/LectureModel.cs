using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace tabseetCourse.Models
{
    public class LectureModel
    {
        public int ID { get; set; }
        public string Lecture_Name { get; set; }
        public int Category_ID { get; set; }
        public string CategoresName { get; set; }
        public string DayandTime { get; set; }
        public string LectureTypeOnOff { get; set; }
        [AllowHtml]
        public string LectureLink_Client { get; set; }
        public string LectureLink_Host { get; set; }
        public string DocumenFile { get; set; }
        public HttpPostedFileBase DocumentFileUpload { get; set; }
        public string MediaLink { get; set; }
        public string Others { get; set; } 
        public string Assingment { get; set; }

        public int LectureMinTime { get; set; }
        public int Assingn_ID { get; set; }
        public int Is_Std_Finish_Lecture { get; set; }
        public bool Is_Lecture_End { get; set; }
        public  CategoryModel Category { get; set; }
        public string Code { get; set; }
        public string Message { get; set; }
    }
}