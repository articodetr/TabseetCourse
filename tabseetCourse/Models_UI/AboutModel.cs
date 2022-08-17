using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace tabseetCourse.Models_UI
{
    public class AboutModel
    {
        public int ID { get; set; }
        public int new_pages_ID { get; set; }

        [Display(Name = "Language")]
        public int Lang_ID { get; set; }
        public string Lang_name { get; set; }
        public string Title { get; set; }

        [Display(Name = "Image Cover")]
        public string Image_Cover { get; set; }
        public HttpPostedFileBase CoverImage { get; set; }
        public List<AboutTabeModel> L_aboutTabmodel { get; set; }
    }
}