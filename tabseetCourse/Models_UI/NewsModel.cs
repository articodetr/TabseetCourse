using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace tabseetCourse.Models_UI
{
    public class NewsModel
    {
        public int ID { get; set; }
        public string Title { get; set; }
        [Display(Name = "Languages")]
        public int Lang_ID { get; set; }

        [Display(Name = "Language name")]
        public string Lang_name { set; get; }
        //public SelectList L_Lang { set; get; }
        public string DateTime { get; set; }
        [AllowHtml]
        [Display(Name = "Short Description")]
        public string ShortDescription { get; set; }
        [AllowHtml]
        [Display(Name = "Full Description")]
        public string FullDescription { get; set; }
        public HttpPostedFileBase CoverImage { get; set; }
        [Display(Name = "Cover Image")]
        public string Cover_image { get; set; }
        public HttpPostedFileBase ShourtImage { get; set; }
        [Display(Name = "Shourt Image")]
        public string Shourt_image { get; set; }
    }
}