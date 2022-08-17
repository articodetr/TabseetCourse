using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace tabseetCourse.Models
{
    public class UserModel
    {
        public string ID { set; get; }
        public string Email { get; set; }
        //public string FullName { get; set; }

        public string RealName { get; set; }
        public string PhoneNumber { get; set; }
        public List<RoleModel> L_role { get; set; }
        public string Image { get; set; }
        public string Password { get; set; }
        public string Country { get; set; }
    
        public string Category_ID { get; set; }


        public string FirstName { get; set; }
        public string LastName { get; set; } 
        public string role { set; get; } 
        public string Per_Photo { get; set; }
        public HttpPostedFileBase PersionImageFile { get; set; }
        public string PersoinDocument { get; set; }
        public HttpPostedFileBase Image_File { get; set; }
        public int userOrTetcherOrStudent { get; set; }
        public int IsActive { get; set; } 
  
        [AllowHtml]
        public string Studing { get; set; }
        [AllowHtml]
        public string Experience { get; set; }
        [AllowHtml]
        public string Education { get; set; }
        public string Location { get; set; }

        public int lectureFinishCount { get; set; }
        public int lectureCount { get; set; }
    }
    public class RoleModel
    {
        public string ID { get; set; }
        public string RoleName { get; set; }
    }
}