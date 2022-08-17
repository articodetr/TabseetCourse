using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace tabseetCourse.Models_UI
{
    public class HomePageModel
    { 
        public int ID { get; set; }

        public string phoneNumber { get; set; }
        public string email { get; set; }

        [AllowHtml]
        public string MainVideo { get; set; }
        public string MainImage { get; set; }
        public HttpPostedFileBase _MainImage { get; set; }
        public string MainText { get; set; }
    }
}