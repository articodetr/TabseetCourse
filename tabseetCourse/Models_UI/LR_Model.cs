using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace tabseetCourse.Models_UI
{
    public class LR_Model
    {
        public int ID { get; set; }
        public int Lang_ID { get; set; }
        public string Title { get; set; }
        public HttpPostedFileBase Imagecover { get; set; }
        public string Image_cover { get; set; }
        public string Password_tran { get; set; }
        public string FirstName_tran { get; set; }
        public string LastName_tran { get; set; }  
        public string Place_of_birth_tran { get; set; } 
        public string Country_tran { get; set; }
        public string Mobile_number_tran { get; set; }
        public string Email_tran { get; set; }  
        public string Private_one { get; set; }
        public string Private_two { get; set; }
        public string Private_three { get; set; }
    }
}