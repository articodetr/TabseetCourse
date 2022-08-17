using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Web.WebPages.OAuth;
namespace tabseetCourse.App_Start
{
    public static class AuthConfig
    {
        public static void RegisterAuth()
        {
            OAuthWebSecurity.RegisterFacebookClient
                (
                appId: "595446208355942",
                appSecret: "941b9d73cd15d9da390300b7ef7b5494"
                );
        }
    }
}