using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace tabseetCourse.Models_UI
{
    public class Page_Tabe_Content_Model
    {
        public int ID { get; set; }
        public int Page_Tabe_ID { get; set; }

        [Display(Name = "Tabe Title")]
        public string Tabe_title { get; set; }

        [Display(Name = "Description")]
        [AllowHtml]
        public string Tab_Description { get; set; }
    }
}