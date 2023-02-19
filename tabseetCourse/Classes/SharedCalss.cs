using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.IdentityModel.Tokens;
 
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using tabseetCourse.Data;
using tabseetCourse.Models;
using System.Collections.Generic;
using Microsoft.Owin;
using Microsoft.Owin.Host.SystemWeb;
namespace tabseetCourse.Classes
{
    public class SharedCalss : Controller
    {
        public readonly tbseedDBEntities dbEntity;
        public readonly UserIdentityContext dbIdentity;
        public readonly UserManager<MyIdentityUser> userManager;
        public readonly UserModel CurrentUser;
        public SharedCalss()
        {
            dbEntity = new tbseedDBEntities();
            dbIdentity = new UserIdentityContext();


            var UserStore = new UserStore<MyIdentityUser>(dbIdentity);
            userManager = new UserManager<MyIdentityUser>(UserStore);
            if (CurrentUser == null || ViewBag.username == null)
            {
                CurrentUser = GetCurrentUser();
                if (CurrentUser != null)
                {
                    ViewBag.username = CurrentUser.FirstName;
                    ViewBag.userRole = userManager.GetRoles(CurrentUser.ID).FirstOrDefault();
                }

            }

        }
        public void CreateRoles()
        {
            try
            {
                var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(dbIdentity));

                List<string> roleNames = ConvertEnumToList<Roles>();
                foreach (var roleName in roleNames)
                {
                    roleManager.Create(new IdentityRole(roleName));
                }
            }
            catch (Exception)
            {

            }
        }
        public bool IsTrustedUser(UserModel um, List<string> Role)
        {
            var result = um.L_role.Where(p => Role.Any(p2 => p2 == p.RoleName));
            if (result != null && result.Count() > 0)
            {
                return true;
            }
            return false;
        }
        public int CreateTeatcher(TetcherModel um)
        {
            try
            {
                var identityUser = new MyIdentityUser
                {
                    UserName = GenerateCodes(9),
                    FirstName = um.FullName,
                    LastName = um.FullName,
                    Email = um.Email,
                    RealName = um.FullName,
                    PhoneNumber = um.PhoneNumber,
                    Password = um.Password,
                    role = Roles.Teacher.ToString(),
                    Categories_ID = um.Categores_ID,
                    userOrTetcherOrStudent = 1,
                    IsActive = 1

                };
                var user = userManager.Create(identityUser, um.Password);
                if (user.Succeeded)
                {

                    ViewBag.User = user.Succeeded;
                    var userId = identityUser.Id;
                    userManager.AddToRole(userId, Roles.Teacher.ToString());
                    return 1;
                }
                else
                {
                    return -1;
                }
            }
            catch (Exception ex)
            {
                return -2;
            }
        }
        public int UdateTeatcherInfo(TetcherModel um)
        {
            try
            {

                var result = userManager.FindById(um.ID);
                if (result != null)
                {
                    result.FirstName = um.FullName;
                    result.LastName = um.FullName;
                    result.RealName = um.FullName;
                    result.Email = um.Email;
                    result.PhoneNumber = um.PhoneNumber;
                    if (um.Categores_ID != null)
                    {
                        result.Categories_ID = um.Categores_ID;
                    }
                    //if (um.Per !="")
                    //{
                    //    result.Per_Photo = um.Per_Photo;
                    //}

                    if (um.Password != null)
                    {
                        result.Password = um.Password;
                    }

                    result.role = Roles.Teacher.ToString();


                    var user = userManager.Update(result);
                    if (user.Succeeded)
                    {

                        ViewBag.User = user.Succeeded;
                        var userId = result.Id;
                        var roles = userManager.GetRoles(result.Id);
                        userManager.RemoveFromRoles(result.Id, roles.ToArray());
                        userManager.AddToRole(userId, Roles.Teacher.ToString());

                    }
                    if (um.Password != null || um.Password != string.Empty || um.Password != "")
                    {
                        var value = MapingUserModelToIdentity(result);
                        ChangePassword(value, um.Password);
                    }
                    return 1;
                }
                else
                    return -1;
            }
            catch (Exception ex)
            {
                return -2;

            }
        }


        public int CreateUsers(string firstName, string lastName, string email, string phonenumber, string password, List<string> LRole, string photo = null)
        {
            try
            {
                var identityUser = new MyIdentityUser() { };
                identityUser.UserName = GenerateCodes(9);
                identityUser.FirstName = firstName;
                identityUser.LastName = lastName;
                identityUser.RealName = firstName;
                identityUser.Email = email;
                identityUser.PhoneNumber = phonenumber;
                identityUser.Password = password;
                identityUser.userOrTetcherOrStudent = 0;
                identityUser.IsActive = 1;
                if (LRole != null)
                {
                    foreach (var item in LRole)
                    {

                        identityUser.role += "/" + item.ToString();
                    }
                }
                if (photo != "")
                {
                    identityUser.Per_Photo = photo;
                }
                var user = userManager.Create(identityUser, password);
                if (user.Succeeded)
                {
                    ViewBag.User = user.Succeeded;
                    var userId = identityUser.Id;
                    foreach (var Role in LRole)
                    {
                        userManager.AddToRole(userId, Role);
                    }
                    return 1;
                }
                else
                {
                    return -1;
                }
            }
            catch (Exception ex)
            {
                return -2;
            }
        }
        public int UpdateUsers(string ID, string firstName, string lastName, string email, string phonenumber, string password, List<string> LRole, string photo = null)
        {
            try
            {
                MyIdentityUser identityUser = userManager.FindById(ID);

                if (identityUser != null)
                {
                    identityUser.FirstName = firstName;
                    identityUser.LastName = lastName;
                    identityUser.Email = email;
                    identityUser.PhoneNumber = phonenumber;
                    identityUser.Password = password;
                    if (LRole != null)
                    {
                        identityUser.role = string.Empty;
                        foreach (var item in LRole)
                        {
                            identityUser.role += "/" + item.ToString();
                        }
                    }
                    if (photo != "")
                    {
                        identityUser.Per_Photo = photo;
                    }
                    if (password != null)
                    {
                        identityUser.Password = password;
                    }
                    var user = userManager.Update(identityUser);
                    if (user.Succeeded)
                    {
                        ViewBag.User = user.Succeeded;
                        var userId = identityUser.Id;
                        var roles = userManager.GetRoles(identityUser.Id);
                        userManager.RemoveFromRoles(identityUser.Id, roles.ToArray());
                        foreach (var item in LRole)
                        {
                            userManager.AddToRole(userId, item);
                        }
                    }
                    if (password != null || password != string.Empty || password != "")
                    {
                        var result = MapingUserModelToIdentity(identityUser);
                        ChangePassword(result, password);
                    }
                    return 1;
                }
                return -1;
            }
            catch (Exception ex)
            {
                return -2;
            }
        }
        public int UpdateStudent(string ID, string firstName, string lastName, string email, string phonenumber, string password)
        {
            try
            {
                MyIdentityUser identityUser = userManager.FindById(ID);

                if (identityUser != null)
                {


                    identityUser.FirstName = firstName;
                    identityUser.LastName = lastName;
                    identityUser.Email = email;
                    identityUser.PhoneNumber = phonenumber;
                    identityUser.Password = password;
                    identityUser.RealName = firstName;
                    var user = userManager.Update(identityUser);
                    var identity = MapingUserModelToIdentity(identityUser);
                    if (password != null || password != string.Empty || password != "")
                    {
                        ChangePassword(identity, password);
                    }
                    return 1;
                }
                return -1;
            }
            catch (Exception ex)
            {
                return -2;
            }
        }

        public async Task<int> CreateStudent(string firstName, string lastName, string email, string phonenumber, string password, string country)
        {
            try
            {
                var result = await userManager.FindByEmailAsync(email);
                if (result != null)
                {
                    return -3;
                }

                var identityUser = new MyIdentityUser() { };

                identityUser.UserName = GenerateCodes(9);
                identityUser.FirstName = firstName;
                identityUser.RealName = firstName;
                identityUser.LastName = lastName;
                identityUser.Email = email;
                identityUser.PhoneNumber = phonenumber;
                identityUser.Password = password;
                identityUser.userOrTetcherOrStudent = 2;
                identityUser.role = Roles.Student.ToString();
                identityUser.IsActive = 1;
                identityUser.Country = country;
                var user = userManager.Create(identityUser, password);
                if (user.Succeeded)
                {
                    ViewBag.User = user.Succeeded;
                    var userId = identityUser.Id;
                    userManager.AddToRole(userId, Roles.Student.ToString());

                    return 1;
                }
                else
                {
                    return -1;
                }
            }
            catch (Exception ex)
            {
                return -2;
            }
        }
        //public void createPersonalInfo(MyIdentityUser PInFo)
        //{ 
        //    pin.Education = PInFo.Education;
        //    pin.Email = PInFo.Email;
        //    pin.Experience = PInFo.Experience;
        //    pin.Location = PInFo.Location;
        //    pin.PhoneNumber = PInFo.PhoneNumber;
        //    pin.RealName = PInFo.RealName;
        //    pin.Studing = PInFo.Studing;
        //    pin.Password = PInFo.Password;
        //    if (PInFo.Per_photoFile!=null)
        //    {
        //        pin.Per_Photo = SaveImage(PInFo.Per_photoFile);
        //    }
        //    dbEntity.PersonalInfoes.Add(pin);
        //    dbEntity.SaveChanges();
        //}
        //public void EditPersionalInFo(PersonalInfoModel PInFo)
        //{
        //    PersonalInfo pin = dbEntity.PersonalInfoes.Find(PInFo.STD_ID);
        //    if (pin!=null)
        //    {
        //        pin.STD_ID = PInFo.STD_ID;
        //        pin.Education = PInFo.Education;
        //        pin.Email = PInFo.Email;
        //        pin.Experience = PInFo.Experience;
        //        pin.Location = PInFo.Location;
        //        pin.PhoneNumber = PInFo.PhoneNumber;
        //        pin.RealName = PInFo.RealName;
        //        pin.Studing = PInFo.Studing;
        //        if (PInFo.Password!=null)
        //        {
        //            pin.Password = PInFo.Password;
        //            var result = GetCurrentuserIdentity().Result;
        //            ChangePassword(result, pin.Password);
        //        }

        //        if (PInFo.Per_photoFile != null)
        //        {
        //            pin.Per_Photo = SaveImage(PInFo.Per_photoFile);
        //        }
        //        dbEntity.PersonalInfoes.Attach(pin);
        //        dbEntity.Entry(pin).State = System.Data.Entity.EntityState.Modified;
        //        dbEntity.SaveChanges();
        //    }

        //}
        public bool ChangePassword(UserModel UserIdntity, string newPass)
        {
            var useridentyty = MapingUserModelToIdentity(UserIdntity);
            userManager.RemovePassword(UserIdntity.ID);
            var value = userManager.AddPassword(UserIdntity.ID, newPass);
            if (value.Succeeded)
            {
                useridentyty.Password = newPass;
                userManager.Update(useridentyty);

                return true;
            }
            else
                return false;
        }
        public void AssingeRoleToUsers(string userID, string Role)
        {
            //var userId = userManager.FindById(userID);
            userManager.AddToRole(userID, Role);
        }

        //public async Task<UserModel>  GetCurrentuserIdentity()
        //{
        //    var userGuide = System.Web.HttpContext.Current.User.Identity.GetUserId();
        //    var user = await userManager.FindByIdAsync(userGuide);
        //    if (User != null)
        //    {
        //        return user;
        //    }
        //    else
        //        return null;
        //}
        //public MyIdentityUser GetCurrentPersonalInfoModel()
        //{
        //    PersonalInfoModel pim = new PersonalInfoModel();
        //    var userGuide = System.Web.HttpContext.Current.User.Identity.GetUserId();
        //    var pin = dbEntity.PersonalInfoes.Where(e => e.STD_ID == userGuide).FirstOrDefault(); ;
        //    if (pin != null)
        //    {
        //        pim.STD_ID = pin.STD_ID;
        //        pim.Education = pin.Education;
        //        pim.Email = pin.Email;
        //        pim.Experience = pin.Experience;
        //        pim.Location = pin.Location;
        //        pim.Per_Photo = pin.Per_Photo;
        //        pim.PhoneNumber = pin.PhoneNumber;
        //        pim.RealName = pin.RealName;
        //        pim.Studing = pin.Studing;
        //        pim.Password = pin.Password;
        //    }
        //    return pim;
        //}
        public string getCurrentUser_ID()
        {
            var userGuide = System.Web.HttpContext.Current.User.Identity.GetUserId();

            return userGuide;

        }
        public UserModel GetCurrentUser()
        {
            var userGuide = System.Web.HttpContext.Current.User.Identity.GetUserId();
            if (userGuide != null)
            {


                var user = userManager.FindById(userGuide);

                if (user != null)
                {
                    UserModel um = new UserModel();
                    um = MapingUserModelToIdentity(user);
                    var result = userManager.GetRoles(user.Id);
                    if (result != null)
                    {
                        um.L_role = new List<RoleModel>();
                        foreach (var item in result)
                        {
                            RoleModel rm = new RoleModel();
                            rm.RoleName = item;
                            um.L_role.Add(rm);
                        }
                    }

                    return um;
                }

            }
            return null;


        }
        protected async Task SingIn(MyIdentityUser user)
        {

            var identity = await userManager.CreateIdentityAsync(user, DefaultAuthenticationTypes.ApplicationCookie);
            var owinContetext = Request.GetOwinContext();
            var autManager = owinContetext.Authentication;
            autManager.SignIn(identity);
        }

        public bool SingOut()
        {
            try
            {
                var owinContetext = Request.GetOwinContext();
                var autManager = owinContetext.Authentication;
                autManager.SignOut();

            }
            catch (Exception)
            {
                return false;
            }
            return true;

        }
        public static Dictionary<string, string> GetAllCountry()
        {
            //Creating Dictionary
            Dictionary<string, string> cultureList = new Dictionary<string, string>();

            //getting the specific CultureInfo from CultureInfo class
            CultureInfo[] getCultureInfo = CultureInfo.GetCultures(CultureTypes.SpecificCultures);

            foreach (CultureInfo getCulture in getCultureInfo)
            {
                //creating the object of RegionInfo class
                RegionInfo getRegionInfo = new RegionInfo(getCulture.LCID);
                //adding each country Name into the Dictionary
                if (!(cultureList.ContainsKey(getRegionInfo.Name)))
                {
                    if (getRegionInfo.EnglishName == "Israel")
                    {
                        //cultureList.Add("PA", "Palestine");
                    }
                    else
                    {
                        cultureList.Add(getRegionInfo.Name, getRegionInfo.EnglishName);
                    }

                }
            }
            //returning country list
            return cultureList;
        }

        public static Dictionary<string, string> GetGender()
        {
            var enumData = from Kind_Gender e in Enum.GetValues(typeof(Kind_Gender))
                           select new
                           {
                               ID = (int)e,
                               Name = e.ToString()
                           };
            var listvalue = enumData.ToDictionary(e => e.ID.ToString(), e => e.Name);
            return listvalue;

        }
        public static Dictionary<string, string> GetSocial_status()
        {
            var Social_status = from Social_status e in Enum.GetValues(typeof(Social_status))
                                select new
                                {
                                    ID = (int)e,
                                    Name = e.ToString()
                                };
            var listvalue = Social_status.ToDictionary(e => e.ID.ToString(), e => e.Name);
            return listvalue;
        }
        public string SaveDocumentForStudent(HttpPostedFileBase imagefile, string CurrentImageID = null)
        {
            if (imagefile != null)
            {
                var fileName = imagefile.FileName;
                var imageGUID = Guid.NewGuid().ToString();
                string Image_ID = imageGUID + fileName;
                string filePath = Server.MapPath($"~/Uploade/StudentFile/{Image_ID}");
                imagefile.SaveAs(filePath);
                return Image_ID;
            }
            else
                return "";
        }
        public string SaveImage(HttpPostedFileBase imagefile, string CurrentImageID = null)
        {

            if (imagefile != null)
            {
                var fileName = imagefile.FileName;
                //var imageGUID = Guid.NewGuid().ToString();
                Random r = new Random();
                var imageGUID = r.Next(0, 999999999);
                string Image_ID = imageGUID + fileName;


                string filePath = Server.MapPath($"~/Uploade/Images/{Image_ID}");
                imagefile.SaveAs(filePath);

                if (CurrentImageID != null)
                {
                    string filePathOld = Server.MapPath($"~/Uploade/Images/{CurrentImageID}");
                    System.IO.File.Delete(filePathOld);
                }
                return Image_ID;
            }
            else
                return "";
        }

        public MyIdentityUser MapingUserModelToIdentity(UserModel UM)
        {
            MyIdentityUser my = new MyIdentityUser();
            my.Id = UM.ID;
            my.Categories_ID = UM.Category_ID;
            my.Country = UM.Country;
            my.Education = UM.Education;
            my.Email = UM.Email;
            my.Experience = UM.Experience;
            my.FirstName = UM.FirstName;
            my.IsActive = UM.IsActive;
            my.LastName = UM.LastName;
            my.Location = UM.Location;
            my.Password = UM.Password;
            my.Per_Photo = UM.Per_Photo;
            my.PhoneNumber = UM.PhoneNumber;
            my.RealName = UM.RealName;
            my.role = UM.role;
            my.Studing = UM.Studing;
            my.userOrTetcherOrStudent = UM.userOrTetcherOrStudent;
            return my;
        }
        public UserModel MapingUserModelToIdentity(MyIdentityUser UM)
        {
            UserModel my = new UserModel();
            my.ID = UM.Id;
            my.Category_ID = UM.Categories_ID;
            my.Country = UM.Country;
            my.Education = UM.Education;
            my.Email = UM.Email;
            my.Experience = UM.Experience;
            my.FirstName = UM.FirstName;
            my.IsActive = UM.IsActive;
            my.LastName = UM.LastName;
            my.Location = UM.Location;
            my.Password = UM.Password;
            my.Per_Photo = UM.Per_Photo;
            my.PhoneNumber = UM.PhoneNumber;
            my.RealName = UM.RealName;
            my.role = UM.role;
            my.Studing = UM.Studing;
            my.userOrTetcherOrStudent = UM.userOrTetcherOrStudent;
            return my;
        }
        private static DateTime ParseDate(string s)
        {
            DateTime result;
            if (!DateTime.TryParse(s, out result))
            {
                result = DateTime.ParseExact(s, "yyyy-MM-ddT24:mm:ssK", System.Globalization.CultureInfo.InvariantCulture);
                result = result.AddDays(1);
            }
            return result;
        }
        public string EMailTemplate(string template)
        {
            string body = System.IO.File.ReadAllText(HostingEnvironment.MapPath("~/Content/template/") + template + ".cshtml");
            return body.ToString();
        }
        public void SendEmailAsync(string email, string subject, string message)
        {
            try
            {

                var _email = "info@tabseet.org";
                var _epass = ConfigurationManager.AppSettings["EmailPassword"];
                var _dispName = "أكاديمية تبسيط الدولية";

                MailMessage myMessage = new MailMessage();
                myMessage.To.Add(email);
                myMessage.From = new MailAddress(_email, _dispName);
                myMessage.Subject = subject;
                myMessage.Body = message;
                myMessage.IsBodyHtml = true;
                using (SmtpClient smtp = new SmtpClient())
                {

                    //smtp.EnableSsl = false;
                    smtp.Host = "smtp.gmail.com";
                    smtp.Port = 25;
                    //smtp.UseDefaultCredentials = false;
                    smtp.Credentials = new NetworkCredential(_email, _epass);
                    //smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                    //smtp.SendCompleted += (s, e) => { smtp.Dispose(); };
                    smtp.SendCompleted += emailWasSended;
                    smtp.Send(myMessage);
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ex.InnerException);


            }

        }
        private static void emailWasSended(object sender, AsyncCompletedEventArgs e)
        {
            ((SmtpClient)sender).Dispose();

        }
        private async Task<int> GenerateRandomListNumber(List<int> L_StudenNum, int stID)
        {
            Random ran = new Random();

            var myTask = Task.Run(() => ran.Next(100, 999));
            int num = await myTask;
            for (int i = 0; i < L_StudenNum.Count(); i++)
            {
                if (num == L_StudenNum[i])
                {
                    await GenerateRandomListNumber(L_StudenNum, stID);
                }
                else
                {
                    var studendNum = stID.ToString() + num.ToString();
                    return int.Parse(studendNum);
                }
            }
            return 0;
        }

        public static List<string> SpliteString(string splite, char splitecaracter)
        {

            string[] words = splite.Split(splitecaracter);
            List<string> L_string = new List<string>();
            foreach (var word in words)
            {
                if (word != "")
                {
                    L_string.Add(word);
                }
            }
            return L_string;
        }
        public string hashcode(string paymentmode)
        {

            var bytes = System.Text.Encoding.UTF8.GetBytes(paymentmode);
            using (var hash = System.Security.Cryptography.SHA512.Create())
            {
                var hashedInputBytes = hash.ComputeHash(bytes);
                var hashedInputStringBuilder = new System.Text.StringBuilder(128);
                foreach (var b in hashedInputBytes)
                    hashedInputStringBuilder.Append(b.ToString("X2"));
                return hashedInputStringBuilder.ToString();
            }

        }

        public static T ParseEnum<T>(string value)
        {
            return (T)Enum.Parse(typeof(T), value, true);
        }
        public static List<string> ConvertEnumToList<T>()
        {
            return Enum.GetNames(typeof(T)).ToList();
        }
        public string GenerateCodes()
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var stringChars = new char[6];
            var random = new Random();

            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = chars[random.Next(chars.Length)];
            }

            string coponcode = new String(stringChars);
            return coponcode;
        }
        public string GenerateCodes(int CodeLenght)
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var stringChars = new char[CodeLenght];
            var random = new Random();

            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = chars[random.Next(chars.Length)];
            }

            string coponcode = new String(stringChars);
            return coponcode;
        }

        public void Printerror(string FunctonName = null, string message = null, string innerexception = null, string stackTrace = null)
        {
            var value = Server.MapPath("~/Data/errorLog.json");
            var jsonData = System.IO.File.ReadAllText(value);
            // De-serialize to object or create new list
            var erro = JsonConvert.DeserializeObject<List<errorModel>>(jsonData) ?? new List<errorModel>();
            errorModel em = new errorModel();
            em.errorcode = stackTrace;
            em.message = message;
            em.functionName = FunctonName;
            em.time = DateTime.Now.ToString();

            em.innermessage = innerexception;


            erro.Add(em);
            var json = new JavaScriptSerializer().Serialize(erro);


            System.IO.File.WriteAllText(value, json);
        }

        #region Zoom

        public async Task<LectureModel> GenerateLectureLink(string lecturname, string useremail, string time)
        {
            //var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            //var now = DateTime.UtcNow;
            //var apiSecret = "7HXAKckEBDNlX1anwKIF9bSRhvNhMW9ZO69g";
            //byte[] symmetricKey = Encoding.ASCII.GetBytes(apiSecret);

            //var tokenDescriptor = new SecurityTokenDescriptor
            //{
            //    Issuer = "aAW4PF6aSN6YhPuMWqi1Jw",
            //    Expires = now.AddHours(10),
            //    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(symmetricKey), SecurityAlgorithms.HmacSha256),
            //};
            //var token = tokenHandler.CreateToken(tokenDescriptor);
            //var tokenString = tokenHandler.WriteToken(token);
            //var client = new RestClient("https://api.zoom.us/v2/users/" + useremail + "/meetings");
            //var request = new RestRequest(Method.Post);
            //request.RequestFormat = DataFormat.Json;
            //request.AddJsonBody(new { topic = lecturname, duration = "10", start_time = time, type = "2" });
            //request.AddHeader("authorization", String.Format("Bearer {0}", tokenString));
            //IRestResponse restResponse = client.Execute(request);
            //HttpStatusCode statusCode = restResponse.StatusCode;
            //int numericStatusCode = (int)statusCode;
            //var jObject = JObject.Parse(restResponse.Content);

            //string host = (string)jObject["start_url"];
            //string Join = (string)jObject["join_url"];
            //string Code = Convert.ToString(numericStatusCode);
            //string Message = (string)jObject["message"];
            //var lecture = new LectureModel();
            //lecture.LectureLink_Host = host;
            //lecture.LectureLink_Client = Join;
            //lecture.Code = Code;
            //lecture.Message = Message;
            //return lecture;
            return null;
        }

        public int CreateuserInZoom(string email, string fullname, string lastname, string password)
        {
            //try
            //{
            //    var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            //    var now = DateTime.UtcNow;
            //    var apiSecret = "7HXAKckEBDNlX1anwKIF9bSRhvNhMW9ZO69g";
            //    byte[] symmetricKey = Encoding.ASCII.GetBytes(apiSecret);

            //    var tokenDescriptor = new SecurityTokenDescriptor
            //    {
            //        Issuer = "aAW4PF6aSN6YhPuMWqi1Jw",
            //        Expires = now.AddHours(10),
            //        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(symmetricKey), SecurityAlgorithms.HmacSha256),
            //    };
            //    var token = tokenHandler.CreateToken(tokenDescriptor);
            //    var tokenString = tokenHandler.WriteToken(token);
            //    var client = new RestClient("https://api.zoom.us/v2/users?access_token=" + tokenString);
            //    var request = new RestRequest(Method.POST);
            //    request.RequestFormat = DataFormat.Json;
            //    root ro = new root();
            //    ro.action = "create";
            //    ro.user_info = new user_info();
            //    ro.user_info.email = email;
            //    ro.user_info.type = "1";
            //    ro.user_info.first_name = fullname;
            //    ro.user_info.last_name = fullname;
            //    ro.user_info.password = password;
            //    //request.AddJsonBody(new { email = "khaled.idress321@gmail.com", type = "1", first_name = "khaled", last_name = "edris", password="!Q2w3e4r5t" });
            //    var userinfo = new JavaScriptSerializer().Serialize(ro);

            //    request.AddJsonBody(userinfo);
            //    request.AddHeader("content-type", "application/json");


            //    IRestResponse restResponse = client.Execute(request);
            //    HttpStatusCode statusCode = restResponse.StatusCode;
            //    int numericStatusCode = (int)statusCode;
            //    var jObject = JObject.Parse(restResponse.Content);
            //    if (numericStatusCode == 201)
            //    {
            //        string Code = Convert.ToString(numericStatusCode);
            //        //Response.StatusCode = (int)HttpStatusCode.OK;
            //        return 1;
            //    }
            //    else
            //        return -1;

            //}
            //catch (Exception ex)
            //{
            //    Printerror("CreateuserInZoom", ex.Message, ex.InnerException.Message, ex.StackTrace);
            //    //Response.StatusCode = (int)HttpStatusCode.BadRequest;
            //    return -2;
            //}
            return 0;
        }
        #endregion

        public static string ConvertDateCalendar(DateTime DateConv, string Calendar, string DateLangCulture)
        {
            DateTimeFormatInfo DTFormat;
            DateLangCulture = DateLangCulture.ToLower();
            /// We can't have the hijri date writen in English. We will get a runtime error

            if (Calendar == "Hijri" && DateLangCulture.StartsWith("en-"))
            {
                DateLangCulture = "ar-sa";
            }

            /// Set the date time format to the given culture
            DTFormat = new System.Globalization.CultureInfo(DateLangCulture, false).DateTimeFormat;

            /// Set the calendar property of the date time format to the given calendar
            switch (Calendar)
            {
                case "Hijri":
                    DTFormat.Calendar = new System.Globalization.HijriCalendar();
                    break;

                case "Gregorian":
                    DTFormat.Calendar = new System.Globalization.GregorianCalendar();
                    break;

                default:
                    return "";
            }

            /// We format the date structure to whatever we want
            DTFormat.LongDatePattern = "yyyy-MM-dd";
            return (DateConv.Date.ToString("f", DTFormat));
        }
        //private string getHashPassworde(byte[] passhash)
        //{
        //    using (SHA1Managed sha1 = new SHA1Managed())
        //    {
        //        // ComputeHash - returns byte array  
        //        //byte[] bytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(passhash));

        //        // Merge all bytes into a string of bytes  
        //        StringBuilder builder = new StringBuilder();
        //        for (int i = 0; i < passhash.Length; i++)
        //        {
        //            builder.Append(passhash[i].ToString("x2"));
        //        }


        //        // BitConverter can also be used to put all bytes into one string  
        //        string bitString = BitConverter.ToString(passhash);


        //        // UTF conversion - String from bytes  
        //        string utfString = Encoding.UTF8.GetString(passhash, 0, passhash.Length);


        //        // ASCII conversion - string from bytes  
        //        string asciiString = Encoding.ASCII.GetString(passhash, 0, passhash.Length);
        //        return asciiString;
        //    }

        //}
        public enum Roles
        {
            Admin,
            Teacher,
            Student,
            CategoriesView,
            CategoriesEdit,
            CategoriesDelete,
            LectureView,
            LectureEdit,
            LectureDelete,
            QuestionBankView,
            QuestionBankEdit,
            QuestionBankDelete,
            UsersTeachersView,
            UsersTeachersEdit,
            UsersTeachersDelete,
            UsersStudentView,
            UsersStudentEdit,
            UsersStudentDelete,
            AssingmentView,
            AssingmentEdit,
            AssingmentDelete,
            PaymentView,
            PaymentActive,
            Chat,
            CodeGenerate,//المنح
            ExamView,
            ExamEdit,
            ExamDelet,
        }
        public enum Days
        {
            Saturday,
            Sunday,
            Monday,
            Tuesday,
            Thursday,
            Wednesday,
            Friday
        }
        public enum Kind_Gender : int
        {

            Male = 1,

            Female = 2
        }
        public enum Users_Kind
        {
            admin,
            user,
            markter,
            categoryproduct,
            accounting,
            Desinger,
            CustomerService,
            TextEditor
        }
        public enum Social_status : int
        {

            Single = 1,

            Married = 2
        }

        public enum Order_State
        {
            InProgress,
            Shipping,
            Delivered
        }
    }
    public class errorModel
    {

        public string message { set; get; }
        public string innermessage { set; get; }
        public string functionName { set; get; }
        public string time { set; get; }
        public string lineincode { set; get; }
        public string errorcode { set; get; }
    }
    public class root
    {
        public string action { set; get; }
        public user_info user_info { get; set; }
    }
    public class user_info
    {
        public string email { get; set; }
        public string type { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string password { get; set; }
    }
}