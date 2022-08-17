using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace tabseetCourse.Models_UI
{
    public class ContactUsModel
    {
        public int ID { get; set; }

        [Display(Name = "Title")]
        public string title { get; set; }

        [Display(Name = "Languages")]
        public int Lang_ID { get; set; }
        public SelectList L_Languages { get; set; }

        public int New_Page_ID { get; set; }

        [Display(Name = "Cover Image")]
        public string Image_Cover { get; set; }
        public HttpPostedFileBase ImageCover { get; set; }

        [Display(Name = "Map Linke")]
        [AllowHtml]
        public string Map_Link { get; set; }

        [Display(Name = "Adress")]
        public string UN_Adress { get; set; }

        [Display(Name = "Phone Number")]
        public string UN_PhoneNumber { get; set; }

        [Display(Name = "Email")]
        public string UN_Email { get; set; }

        [Display(Name = "Translate Name word")]
        public string Name_word_trans { get; set; }

        [Display(Name = "Translate Email word")]
        public string Email_word_trans { get; set; }

        [Display(Name = "Translate PhoneNumber word")]
        public string PhoneNumber_word_trans { get; set; }

        [Display(Name = "Translate Message word")]
        public string Message_word_trans { get; set; }

        [Display(Name = "Translate Send word")]
        public string Send_word_trans { get; set; }
        public string Adress_word_trans { get; set; }
    }
}