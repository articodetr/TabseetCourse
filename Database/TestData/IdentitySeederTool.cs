/* ==============================================================================================
   مشروع أكاديمية تبسيط الدولية - أداة C# البرمجية لإدارة حسابات Identity عبر UserManager
   الملف: IdentitySeederTool.cs
   المسار: Database/TestData/IdentitySeederTool.cs
   المكتبات المستخدمة:
     - Microsoft.AspNet.Identity.Core
     - Microsoft.AspNet.Identity.EntityFramework
     - EntityFramework
   ============================================================================================== */

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using tabseetCourse.Data;

namespace TabseetCourse.TestData
{
    /// <summary>
    /// أداة برمجية مستقلة لتغذية وإدارة حسابات ASP.NET Identity في أكاديمية تبسيط الدولية برمجياً عبر UserManager
    /// </summary>
    public class IdentitySeederTool
    {
        private readonly UserIdentityContext _identityContext;
        private readonly UserManager<MyIdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public IdentitySeederTool()
        {
            _identityContext = new UserIdentityContext();
            _userManager = new UserManager<MyIdentityUser>(new UserStore<MyIdentityUser>(_identityContext));
            _roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(_identityContext));

            // سياسات التحقق من كلمات المرور بما يتطابق مع النظام
            _userManager.PasswordValidator = new PasswordValidator
            {
                RequiredLength = 6,
                RequireNonLetterOrDigit = false,
                RequireDigit = false,
                RequireLowercase = false,
                RequireUppercase = false
            };
        }

        /// <summary>
        /// التأكد من وجود الأدوار الأساسية وإضافتها إذا لم تكن موجودة
        /// </summary>
        public void EnsureRoles(IEnumerable<string> roles)
        {
            foreach (var role in roles)
            {
                if (!_roleManager.RoleExists(role))
                {
                    _roleManager.Create(new IdentityRole(role));
                    Console.WriteLine($"[Role Created] {role}");
                }
            }
        }

        /// <summary>
        /// إنشاء حساب مستخدم Identity مع إسناد الدور والتفاصيل بصورة آمنة
        /// </summary>
        public IdentityResult CreateTestUser(
            string userId,
            string userName,
            string email,
            string password,
            string realName,
            string firstName,
            string lastName,
            string phone,
            string country,
            int userType, // 0 = Admin, 1 = Teacher, 2 = Student
            int isActive,
            string roleName,
            string categoriesId = null)
        {
            var existingUser = _userManager.FindByEmail(email);
            if (existingUser != null)
            {
                return IdentityResult.Failed($"User already exists with email: {email}");
            }

            var newUser = new MyIdentityUser
            {
                Id = userId,
                UserName = userName,
                Email = email,
                EmailConfirmed = true,
                FirstName = firstName,
                LastName = lastName,
                RealName = realName,
                PhoneNumber = phone,
                PhoneNumberConfirmed = true,
                Password = password,
                Country = country,
                userOrTetcherOrStudent = userType,
                IsActive = isActive,
                role = roleName,
                Categories_ID = categoriesId,
                Location = $"{country} [TEST_TABSEET_2026]"
            };

            var createResult = _userManager.Create(newUser, password);
            if (createResult.Succeeded)
            {
                _userManager.AddToRole(newUser.Id, roleName);
                Console.WriteLine($"[User Created] {email} as {roleName}");
            }

            return createResult;
        }
    }
}

