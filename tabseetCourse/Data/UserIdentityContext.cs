using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
namespace tabseetCourse.Data
{
    public class UserIdentityContext : IdentityDbContext<MyIdentityUser>
    {

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<IdentityUserRole>()
                .Property(x => x.UserId)
                .HasColumnName("AspNetUsers_Id");

            modelBuilder.Entity<IdentityUserRole>()
                .Property(x => x.RoleId)
                .HasColumnName("AspNetRoles_Id");
        }

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