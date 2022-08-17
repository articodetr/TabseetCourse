using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace tabseetCourse.Models
{
    public class TetcherModel
    {
        public string ID { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string PhoneNumber { get; set; }
        public string Categores_ID { get; set; }
        public string Image { get; set; }
        public HttpPostedFileBase Image_File { get; set; }
    }
}