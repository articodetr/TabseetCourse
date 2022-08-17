using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace tabseetCourse.Data
{
    public class UserIdentityContext : IdentityDbContext<MyIdentityUser>
    {
        public UserIdentityContext() : base("TabseetConnection")
        {

        }

    }
    public class MyIdentityUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public string Password { set; get; }
        public string role { set; get; }
        public string Categories_ID { get; set; }
        public string Per_Photo { get; set; }
        //public HttpPostedFileBase PersionImageFile { get; set; }
        public string PersoinDocument { get; set; }
        public int userOrTetcherOrStudent { get; set; }
        public int IsActive { get; set; }
        public string Country { get; set; }
        public string RealName { get; set; }
        //[AllowHtml]
        public string Studing { get; set; }
        //[AllowHtml]
        public string Experience { get; set; }
        //[AllowHtml]
        public string Education { get; set; }
        public string Location { get; set; }


    }

}