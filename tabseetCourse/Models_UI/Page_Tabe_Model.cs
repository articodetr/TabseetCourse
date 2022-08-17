using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace tabseetCourse.Models_UI
{
    public class Page_Tabe_Model
    {
        public int ID { get; set; }
        [Display(Name = "Language")]
        public int Lang_ID { get; set; }
        [Display(Name = "Language")]
        public string Lang_name { get; set; }
        public int New_Page_ID { get; set; }

        public int page_H_V_tabe { get; set; }
        [Display(Name = "Image Cover")]
        public string Image_Cover { get; set; }
        public HttpPostedFileBase CoverImage { get; set; }

        [Display(Name = "Title")]
        public string Title { get; set; }

        public List<Page_Tabe_Content_Model> TabeContent { get; set; }
    }
}