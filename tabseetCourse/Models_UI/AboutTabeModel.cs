using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace tabseetCourse.Models_UI
{
    public class AboutTabeModel
    {
        public int ID { get; set; }
        [Display(Name = "About page Name")]
        public int New_About_ID { get; set; }
        public string AboutName { set; get; }

        [Display(Name = "Tab Title")]
        public string Tab_Title { get; set; }

        [Display(Name = "Tab Content")]
        [AllowHtml]
        public string Tab_Content { get; set; }

        //public virtual New_About New_About { get; set; }
    }
}