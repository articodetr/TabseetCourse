using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace tabseetCourse.Models_UI
{
    public class SliderModel
    {
        public int ID { get; set; }
        [Display(Name = "Sliders")]
        public string Image { get; set; }
        [Display(Name = "Languages")]
        public int Lang_ID { set; get; }
        public string Lang_Name { set; get; }
        public string TextMessage { get; set; }
        public HttpPostedFileBase Image_File { get; set; }
    }
}