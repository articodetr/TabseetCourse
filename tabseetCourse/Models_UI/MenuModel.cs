using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace tabseetCourse.Models_UI
{
    public class MenuModel
    {
        public int ID { get; set; }
        [Display(Name = "Menu Item Name")]
        public string Title { get; set; }
        [Display(Name = "Page ID")]
        public int New_Page_ID { get; set; }

        [Display(Name = "Page Name")]
        public string New_Page_Name { get; set; }
        public SelectList L_Pages { set; get; }

        [Display(Name = "Language")]
        public int Lang_ID { get; set; }
        [Display(Name = "Language")]
        public string Lang_name { get; set; }
    }
}