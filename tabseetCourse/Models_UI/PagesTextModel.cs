using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace tabseetCourse.Models_UI
{
    public class PagesTextModel
    {
        public int ID { get; set; }
        public int New_pages_ID { get; set; }
        public int Lang_ID { get; set; }
        public string title { get; set; }

        [Display(Name = "Image Cover")]
        public string Image_Cover { get; set; }
        public HttpPostedFileBase ImageCover { set; get; }

        [AllowHtml]
        [Display(Name = "Description")]
        public string Text_Description { get; set; }
    }
}