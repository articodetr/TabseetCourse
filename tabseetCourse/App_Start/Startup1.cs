using Microsoft.AspNet.Identity;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Owin;
using System;
using System.Threading.Tasks;

[assembly: OwinStartup(typeof(tabseetCourse.App_Start.Startup1))]

namespace tabseetCourse.App_Start
{
    public class Startup1
    {
        public void Configuration(IAppBuilder app)
        {
            // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=316888
            CookieAuthenticationOptions cookiauthentication = new CookieAuthenticationOptions();
            cookiauthentication.AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie;
            cookiauthentication.LoginPath = new PathString("/Home/LogIn");

            app.UseCookieAuthentication(cookiauthentication);
        }
    }
}
