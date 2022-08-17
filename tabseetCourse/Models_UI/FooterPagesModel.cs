using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace tabseetCourse.Models_UI
{
    public class FooterPagesModel
    {
        public int ID { get; set; }

        public int New_page_ID { get; set; }

        [Display(Name = "Page Name")]
        public string PageName { get; set; }
    }
}