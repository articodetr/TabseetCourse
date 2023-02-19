using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using tabseetCourse.Classes;
using tabseetCourse.Data;
using tabseetCourse.Models;
using tabseetCourse.Models_UI;

namespace tabseetCourse.Controllers
{
    public class HomeController : SharedCalss
    {
        public HomePageModel HomepageViewModel { get; set; }
        //public static int Lang_ID { get; set; }
        //public static int Direction_ID { get; set; }
        public HomeController()
        {
            var value = dbEntity.New_Home_Page.FirstOrDefault();

            HomePageModel hp = new HomePageModel();
            if (value != null)
            {
                hp.ID = value.ID;
                hp.MainVideo = value.MainVideo;
                hp.MainText = value.MainText;
                hp.MainImage = value.MainImage;
                hp.email = value.email;
                hp.phoneNumber = value.phoneNumber;
                this.HomepageViewModel = hp;
                this.ViewData["HomepageViewModel"] = this.HomepageViewModel;
            }

        }

        public ActionResult Index()
        {
            var Langnguages = Session["Lang_ID"];
            HomePageModel hp = new HomePageModel();
            try
            {
                if (Langnguages == null)
                {
                    Session["Lang_ID"] = 1;// dbEntity.New_lang.Where(e => e.DefultLang == 1).FirstOrDefault().ID;
                    Session["Direction_ID"] = 1;// dbEntity.New_lang.Where(e => e.DefultLang == 1).FirstOrDefault().Direction;
                }
                var lang = Session["Lang_ID"];
                int Lang_ID = int.Parse(lang.ToString());

                var Direction = Session["Direction_ID"];
                if (Direction == null)
                {
                    Session["Direction_ID"] = dbEntity.New_lang.Where(e => e.DefultLang == 1).FirstOrDefault().Direction;
                    Direction = Session["Direction_ID"];
                }

                int Direction_ID = int.Parse(Direction.ToString());

                var value = HomepageViewModel;


                if (value != null)
                {
                    hp.ID = value.ID;
                    hp.MainVideo = value.MainVideo;
                    hp.MainText = value.MainText;
                    hp.MainImage = value.MainImage;
                    hp.email = value.email;
                    hp.phoneNumber = value.phoneNumber;
                    //this.HomepageViewModel = hp;
                    //this.ViewData["HomepageViewModel"] = this.HomepageViewModel;
                }


                List<CategoryModel> L_CM = new List<CategoryModel>();
                var values = dbEntity.Categories.Where(e => e.CanRegister == 1).OrderByDescending(x => x.ID).Take(5);
                foreach (var item in values)
                {
                    CategoryModel cm = new CategoryModel();
                    cm.ID = item.ID;
                    cm.NameAr = item.NameAr;
                    cm.NameEn = item.NameEn;
                    cm.IsPayed = item.IsPayed.Value;
                    cm.PayedValue = item.PayedValue;
                    cm.New_Page_ID = item.New_Page_ID.Value;
                    cm.timeEnd = item.EndTime;
                    cm.Icon = item.Icon;
                    cm.Description = item.Description;
                    if (cm.timeEnd != null && DateTime.Parse(cm.timeEnd) >= DateTime.Now)
                    {
                        L_CM.Add(cm);
                    }
                    else if (cm.timeEnd == null)
                    {
                        L_CM.Add(cm);
                    }
                }
                ViewBag.Category = L_CM;

                var Tetchres = dbEntity.NewGridPage_Content.Where(e => e.GridPageID == 4).OrderByDescending(x => x.ID).Take(5);
                List<GridpageContentModel> tetchers = new List<GridpageContentModel>();
                foreach (var item in Tetchres)
                {
                    GridpageContentModel cm = new GridpageContentModel();
                    cm.ID = item.ID;
                    cm.DocLinke = item.DocLinke;
                    cm.Title = item.Title;
                    cm.Description = item.Description;
                    cm.GridPageID = item.GridPageID.Value;
                    cm.Image = item.Image;
                    tetchers.Add(cm);

                }
                ViewBag.tetcher = tetchers;

            }
            catch (Exception ex)
            {

            }

            return View(hp);
        }

        [HttpPost]
        public ActionResult SetLocalLang(int lang_ID)
        {
            if (lang_ID != 0)
            {

                int Direction_ID = dbEntity.New_lang.Find(lang_ID).Direction;
                Session["Direction_ID"] = Direction_ID;
                Session["Lang_ID"] = lang_ID;

                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(new { success = true, response = "" }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = "" }, JsonRequestBehavior.AllowGet);
            }


        }

        [HttpPost]
        public JsonResult GetHomePage()
        {
            var lang = Session["Lang_ID"];
            if (lang == null)
            {
                Session["Lang_ID"] = dbEntity.New_lang.Where(e => e.DefultLang == 1).FirstOrDefault().ID;
                Session["Direction_ID"] = dbEntity.New_lang.Where(e => e.DefultLang == 1).FirstOrDefault().Direction;
            }
            int Lang_ID = int.Parse(Session["Lang_ID"].ToString());
            //Lang_ID = dbEntity.New_lang.Where(e => e.DefultLang == 1).FirstOrDefault().ID;
            var value = dbEntity.New_Home_Page.FirstOrDefault();
            HomePageModel hp = new HomePageModel();

            if (value != null)
            {
                hp.ID = value.ID;


            }
            return Json(hp, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult GetSosialMedia()
        {
            var lang = Session["Lang_ID"];
            if (lang == null)
            {
                Session["Lang_ID"] = dbEntity.New_lang.Where(e => e.DefultLang == 1).FirstOrDefault().ID;
                Session["Direction_ID"] = dbEntity.New_lang.Where(e => e.DefultLang == 1).FirstOrDefault().Direction;
            }
            int Lang_ID = int.Parse(Session["Lang_ID"].ToString());
            var value = dbEntity.New_SocialMedia.Where(e => e.Lang_ID == Lang_ID).FirstOrDefault();
            SocialMediaModel hp = new SocialMediaModel();

            if (value != null)
            {
                hp.ID = value.ID;
                hp.Instgram = value.Instgram;
                hp.Facebook = value.Facebook;
                //hp.Linkedin = value.Linkedin;
                hp.Twitter = value.Twitter;
                hp.WhatsApp = value.WhatsApp;
                hp.Youtube = value.Youtube;
                hp.Tiktok = value.Tiktok;
                hp.Telegram = value.Telegram;
            }
            return Json(hp, JsonRequestBehavior.AllowGet);
        }

        //[HttpGet]
        //public ActionResult WebHook(string type, string token, string userid, string timestamp)
        //{
        //    Console.Write("Type=" + type + "_token=" + token);
        //    return null;
        //}

        [HttpPost]
        public JsonResult GetAllLanguages()
        {
            List<New_lang> LangList = new List<New_lang>();
            var lang = Session["Lang_ID"];
            if (lang == null)
            {
                Session["Lang_ID"] = dbEntity.New_lang.Where(e => e.DefultLang == 1).FirstOrDefault().ID;
                Session["Direction_ID"] = dbEntity.New_lang.Where(e => e.DefultLang == 1).FirstOrDefault().Direction;
            }
            int Lang_ID = int.Parse(Session["Lang_ID"].ToString());
            if (Lang_ID == 0)
            {
                LangList = dbEntity.New_lang.ToList().OrderBy(e => e.DefultLang == 0).ToList();
            }
            else
            {
                LangList = dbEntity.New_lang.ToList().OrderByDescending(e => e.ID == Lang_ID).ToList();
            }

            //CurrentLangForOneUser = LangList.FirstOrDefault().ID;
            //subjectmodel.FollowSelectedList_ID = new SelectList(collegeList, "ID", "Name");
            return Json(LangList.Select(x => new
            {
                ID = x.ID,
                Name = x.Name
            }).ToList(), JsonRequestBehavior.AllowGet);

        }

        [HttpGet]
        public ActionResult GetMenu()
        {
            var value = new List<New_Menu>();
            var lang = Session["Lang_ID"];
            int Lang_ID = 0;
            if (lang != null)
            {
                Lang_ID = int.Parse(lang.ToString());
            }

            if (Lang_ID != 0)
            {
                value = dbEntity.New_Menu.Where(e => e.Lang_ID == Lang_ID).OrderByDescending(e => e.ID).ToList();
            }
            else
            {
                Lang_ID = dbEntity.New_lang.Where(e => e.DefultLang == 1).FirstOrDefault().ID;
                value = dbEntity.New_Menu.Where(e => e.Lang_ID == Lang_ID).ToList();
                Session["Lang_ID"] = Lang_ID;
            }
            List<MenuModel> hp = new List<MenuModel>();
            foreach (var item in value)
            {
                var ite = new MenuModel();
                ite.ID = item.ID;
                ite.New_Page_ID = item.New_Page_ID;
                ite.Title = item.Title;
                hp.Add(ite);
            }
            return PartialView(hp);
        }

        [HttpGet]
        public ActionResult GetFooterPage()
        {
            var lang = Session["Lang_ID"];
            if (lang == null)
            {
                Session["Lang_ID"] = dbEntity.New_lang.Where(e => e.DefultLang == 1).FirstOrDefault().ID;
                Session["Direction_ID"] = dbEntity.New_lang.Where(e => e.DefultLang == 1).FirstOrDefault().Direction;
            }
            int Lang_ID = int.Parse(Session["Lang_ID"].ToString());
            var L_footer = new List<FooterPagesModel>();
            var L_footer2 = new List<FooterPagesModel>();
            var value = dbEntity.New_FooterMenu.ToList();

            foreach (var item in value)
            {
                var ite = new FooterPagesModel();
                ite.ID = item.ID;
                ite.New_page_ID = item.New_page_ID;
                L_footer.Add(ite);
            }
            foreach (var item in L_footer)
            {
                var page = dbEntity.New_pages.Find(item.New_page_ID);
                if (page != null)
                {
                    if (page.Page_Type == 1)
                    {
                        var result = dbEntity.New_About.Where(e => e.new_pages_ID == page.ID && e.Lang_ID == Lang_ID).FirstOrDefault();
                        item.PageName = result.Title;
                        item.New_page_ID = result.new_pages_ID;
                        L_footer2.Add(item);

                    }
                    if (page.Page_Type == 2)
                    {
                        var result = dbEntity.New_ContactUs.Where(e => e.New_Page_ID == page.ID && e.Lang_ID == Lang_ID).FirstOrDefault();
                        if (result != null)
                        {
                            item.PageName = result.title;
                            item.New_page_ID = result.New_Page_ID;
                            L_footer2.Add(item);

                        }
                    }
                    if (page.Page_Type == 3)
                    {
                        var result = dbEntity.New_Page_Tabe.Where(e => e.New_Page_ID == page.ID && e.Lang_ID == Lang_ID).FirstOrDefault();
                        if (result != null)
                        {
                            item.PageName = result.Title;
                            item.New_page_ID = result.New_Page_ID;
                            L_footer2.Add(item);

                        }
                    }
                    if (page.Page_Type == 4)
                    {
                        var result = dbEntity.New_Page_Tabe.Where(e => e.New_Page_ID == page.ID && e.Lang_ID == Lang_ID).FirstOrDefault();
                        if (result != null)
                        {
                            item.PageName = result.Title;
                            item.New_page_ID = result.New_Page_ID;
                            L_footer2.Add(item);

                        }
                    }
                    if (page.Page_Type == 5)
                    {
                        var result = dbEntity.New_pages_Text.Where(e => e.New_pages_ID == page.ID && e.Lang_ID == Lang_ID).FirstOrDefault();
                        if (result != null)
                        {
                            item.PageName = result.title;
                            item.New_page_ID = result.New_pages_ID;
                            L_footer2.Add(item);

                        }
                    }
                    if (page.Page_Type == 7)
                    {
                        var result = dbEntity.NewGridPages.Where(e => e.Pages_ID == page.ID && e.Lang_ID == Lang_ID).FirstOrDefault();
                        if (result != null)
                        {
                            item.PageName = result.Title;
                            item.New_page_ID = result.Pages_ID.Value;
                            L_footer2.Add(item);

                        }
                    }

                }
            }
            return PartialView(L_footer2);
        }

        [HttpGet]
        public ActionResult IniSlider()
        {
       
            List<SliderModel> sl = new List<SliderModel>();
            try
            {

                var value = dbEntity.New_Slider.ToList();
                if (value != null)
                {
                    foreach (var item in value)
                    {
                        var result = new SliderModel();
                        result.ID = item.ID;
                        result.Image = item.Slider_Image;
                        result.Lang_ID = item.Lang_ID;
                        result.TextMessage = item.TextMessage;
                        sl.Add(result);
                    }
                }
                return PartialView(sl);
            }
            catch (Exception)
            {
                return PartialView(sl);
            }

        }

        [HttpPost]
        public JsonResult GetAllCategories()
        {
            List<CategoryModel> L_CM = new List<CategoryModel>();
            try
            {
                var values = dbEntity.Categories.Where(e => e.CanRegister == 1).ToList();

                foreach (var item in values)
                {
                    CategoryModel cm = new CategoryModel();
                    cm.ID = item.ID;
                    cm.NameAr = item.NameAr;
                    cm.NameEn = item.NameEn;
                    cm.IsPayed = item.IsPayed.Value;
                    cm.PayedValue = item.PayedValue;
                    cm.New_Page_ID = item.New_Page_ID.Value;
                    cm.timeEnd = item.EndTime;
                    cm.Icon = item.Icon;
                    if (cm.timeEnd != null && DateTime.Parse(cm.timeEnd) > DateTime.Now)
                    {
                        L_CM.Add(cm);
                    }
                    else if (cm.timeEnd == null)
                    {
                        L_CM.Add(cm);
                    }

                }
            }
            catch (Exception ex)
            {
            }
            Response.StatusCode = (int)HttpStatusCode.OK;
            return Json(L_CM, JsonRequestBehavior.AllowGet);
        }
        public ActionResult AllNews()
        {
     
            var value = dbEntity.New_news.ToList();
            List<NewsModel> L_new = new List<NewsModel>();
            try
            {
                foreach (var item in value)
                {
                    NewsModel fp = new NewsModel();
                    fp.ID = item.ID;
                    fp.Lang_ID = item.Lang_ID;
                    fp.Title = item.Title;
                    fp.ShortDescription = item.ShortDescription;
                    fp.Shourt_image = item.Shourt_image;
                    fp.Cover_image = item.Cover_image;
                    fp.DateTime = item.DateTime;
                    fp.FullDescription = item.FullDescription;
                    L_new.Add(fp);
                }
            }
            catch (Exception ex)
            {
            }
            return View(L_new);
        }
        [HttpGet]
        public ActionResult IniNews()
        {
     
            var value = dbEntity.New_news.ToList();
            List<NewsModel> Lf = new List<NewsModel>();
            foreach (var item in value)
            {
                NewsModel fp = new NewsModel();
                fp.ID = item.ID;
                fp.Lang_ID = item.Lang_ID;
                fp.Title = item.Title;
                fp.ShortDescription = item.ShortDescription;
                fp.Shourt_image = item.Shourt_image;
                fp.Cover_image = item.Cover_image;
                fp.DateTime = item.DateTime;
                fp.FullDescription = item.FullDescription;
                Lf.Add(fp);
            }
            return PartialView(Lf);
        }

        [HttpGet]
        public ActionResult ReadMoreNews(int News_ID)
        {
            var value = dbEntity.New_news.Find(News_ID);
            return View(value);
        }

        [HttpGet]
        public ActionResult IniPracktes()
        {
         
            var value = dbEntity.New_pracktes.ToList();
            List<PracktesModel> Lf = new List<PracktesModel>();
            foreach (var item in value)
            {
                PracktesModel fp = new PracktesModel();
                fp.ID = item.ID;
                fp.Lang_ID = item.Lang_ID;
                fp.Title = item.Title;
                fp.ShortDescription = item.ShortDescription;
                fp.Shourt_image = item.Shourt_image;
                fp.Cover_image = item.Cover_image;
                fp.DateTime = item.DateTime;
                fp.FullDescription = item.FullDescription;
                Lf.Add(fp);
            }
            return PartialView(Lf);
        }

        [HttpGet]
        public ActionResult GoPageMenu(int PageID)
        {
 
            var page = dbEntity.New_pages.Find(PageID);
            if (page != null)
            {
                if (page.Page_Type == 1)
                {

                    return RedirectToAction("aboutPage", new { pageID = PageID });
                }
                if (page.Page_Type == 2)
                {
                    var result = dbEntity.New_ContactUs.Where(e => e.New_Page_ID == page.ID).FirstOrDefault();
                    if (result != null)
                    {
                        return RedirectToAction("ContactUsPage", new { pageID = PageID });
                    }
                }
                if (page.Page_Type == 3)
                {
                    return RedirectToAction("HorizantalTabePagePage", new { pageID = PageID });
                }
                if (page.Page_Type == 4)
                {
                    return RedirectToAction("VerticalTabePagePage", new { pageID = PageID });
                }
                if (page.Page_Type == 5)
                {
                    return RedirectToAction("PageText", new { pageID = PageID });
                }
                if (page.Page_Type == 8)
                {
                    return RedirectToAction("FacalityPage", new { pageID = PageID });
                }

                if (page.Page_Type == 7)
                {
                    return RedirectToAction("GridPage", new { pageID = PageID });
                }

            }
            return RedirectToAction("Index");
        }
        [HttpGet]
        public ActionResult PageText(int pageID)
        {
            PagesTextModel ptm = new PagesTextModel();
            try
            {
                var lang = Session["Lang_ID"];
                if (lang == null)
                {
                    Session["Lang_ID"] = dbEntity.New_lang.Where(e => e.DefultLang == 1).FirstOrDefault().ID;
                    Session["Direction_ID"] = dbEntity.New_lang.Where(e => e.DefultLang == 1).FirstOrDefault().Direction;
                    lang = Session["Lang_ID"];
                }
                int Lang_ID = int.Parse(lang.ToString());
                var result = dbEntity.New_pages_Text.Where(e => e.Lang_ID == Lang_ID && e.New_pages_ID == pageID).FirstOrDefault();

                if (result != null)
                {
                    ptm.ID = result.ID;
                    ptm.Image_Cover = result.Image_Cover;
                    ptm.Lang_ID = result.Lang_ID;
                    ptm.New_pages_ID = result.New_pages_ID;
                    ptm.Text_Description = result.Text_Description;
                    ptm.title = result.title;

                }

            }
            catch (Exception ex)
            {
            }
            return View(ptm);
        }
        [HttpGet]
        public ActionResult GridPage(int pageID)
        {
            var result = new GridPageModel();
            try
            {
                //var lang = Session["Lang_ID"];
                //if (lang == null)
                //{
                //    Session["Lang_ID"] = dbEntity.New_lang.Where(e => e.DefultLang == 1).FirstOrDefault().ID;
                //    Session["Direction_ID"] = dbEntity.New_lang.Where(e => e.DefultLang == 1).FirstOrDefault().Direction;
                //    lang = Session["Lang_ID"];
                //}
                var newpage = dbEntity.New_pages.Find(pageID);
                //int Lang_ID = int.Parse(lang.ToString());
                var value = dbEntity.NewGridPages.Where(e =>  e.Pages_ID == pageID).FirstOrDefault();

                if (value != null)
                {
                    result.GirdContent = new List<GridpageContentModel>();
                    result.ID = value.ID; 
                    result.Pages_ID = value.Pages_ID.Value;
                    result.Title = value.Title;
                    result.Image_Cover = value.Image_Cover;
                    
                    var cat = dbEntity.Categories.Where(e => e.CategoryType_ID == newpage.CategoryType_ID).ToList();
                    foreach (var item in cat)
                    {
                        var grdCont = new GridpageContentModel();
                        if (item != null)
                        {

                            result.ContentBtnReg = item.CanRegister.Value;
                       
                            grdCont.ID = item.ID;
                            grdCont.Image = item.Icon;
                            grdCont.Is_reg = item.CanRegister.Value;
                            int payvalue = 0;
                            int.TryParse(item.PayedValue, out payvalue);
                            grdCont.PayValue = payvalue;
                        }
                        else
                        {
                            result.ContentBtnReg = 0;

                            grdCont.Is_reg = 0;
                            grdCont.PayValue = 0;
                            grdCont.ID = item.ID;
                        }
                        grdCont.Description = item.Description;
                        grdCont.Title = item.NameAr;
                        grdCont.GridPageID = item.New_Page_ID.Value;
                        result.GirdContent.Add(grdCont);
                    }

               
                     
               
                }

            }
            catch (Exception ex)
            {
            }
            return View(result);
        }
        [HttpGet]
        public ActionResult aboutPage(int pageID)
        {
         
            AboutModel am = new AboutModel();
            am.L_aboutTabmodel = new List<AboutTabeModel>();
            try
            {
                var result = dbEntity.New_About.Where(e =>   e.new_pages_ID == pageID).FirstOrDefault();

                am.ID = result.ID;
                am.Image_Cover = result.Image_Cover;
                am.new_pages_ID = result.new_pages_ID;
                am.Title = result.Title;
                foreach (var item2 in result.New_About_Content)
                {
                    var tab = new AboutTabeModel();
                    tab.ID = item2.ID;
                    tab.New_About_ID = item2.New_About_ID;
                    tab.Tab_Content = item2.Tab_Content;
                    tab.Tab_Title = item2.Tab_Title;
                    am.L_aboutTabmodel.Add(tab);
                }
                return View(am);
            }
            catch (Exception ex)
            {
                return View(am);
            }
        }

        [HttpGet]
        public ActionResult ContactUsPage(int pageID)
        {
            var lang = Session["Lang_ID"];
            if (lang == null)
            {
                Session["Lang_ID"] = dbEntity.New_lang.Where(e => e.DefultLang == 1).FirstOrDefault().ID;
                Session["Direction_ID"] = dbEntity.New_lang.Where(e => e.DefultLang == 1).FirstOrDefault().Direction;
                lang = Session["Lang_ID"];
            }
            int Lang_ID = int.Parse(lang.ToString());
            ContactUsModel am = new ContactUsModel();
            try
            {
                var result = dbEntity.New_ContactUs.Where(e => e.Lang_ID == Lang_ID && e.New_Page_ID == pageID).FirstOrDefault();

                am.ID = result.ID;
                am.Image_Cover = result.Image_Cover;
                am.New_Page_ID = result.New_Page_ID;
                am.title = result.title;
                am.Map_Link = result.Map_Link;
                am.UN_Adress = result.UN_Adress;
                am.UN_Email = result.UN_Email;
                am.UN_PhoneNumber = result.UN_PhoneNumber;
                am.Send_word_trans = result.Send_word_trans;
                am.PhoneNumber_word_trans = result.PhoneNumber_word_trans;
                am.Name_word_trans = result.Name_word_trans;
                am.Message_word_trans = result.Message_word_trans;
                am.Email_word_trans = result.Email_word_trans;
                am.Adress_word_trans = result.Adress_word_trans;
                return View(am);
            }
            catch (Exception ex)
            {
                return View(am);
            }
        }
        [HttpPost]
        public JsonResult SendContactUsMessage(string Name, string Phonenumber, string Email, string Message)
        {
            try
            {
                New_CustomerContactUs cu = new New_CustomerContactUs();
                cu.Name = Name;
                cu.PhoneNumber = Phonenumber;
                cu.Email = Email; ;
                cu.Message = Message;
                dbEntity.New_CustomerContactUs.Add(cu);
                dbEntity.SaveChanges();
            }
            catch (Exception ex)
            {
            }
            Response.StatusCode = (int)HttpStatusCode.OK;
            return Json(new { success = true, response = "" }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult HorizantalTabePagePage(int pageID)
        {

            Page_Tabe_Model ptm = new Page_Tabe_Model();
            ptm.TabeContent = new List<Page_Tabe_Content_Model>();
            //var lang = Session["Lang_ID"];
            //if (lang == null)
            //{
            //    Session["Lang_ID"] = dbEntity.New_lang.Where(e => e.DefultLang == 1).FirstOrDefault().ID;
            //    Session["Direction_ID"] = dbEntity.New_lang.Where(e => e.DefultLang == 1).FirstOrDefault().Direction;
            //    lang = Session["Lang_ID"];
            //}
            //int Lang_ID = int.Parse(lang.ToString());
            var LPage = dbEntity.New_Page_Tabe.Where(e => e.New_Page_ID == pageID && e.page_H_V_tabe == 1  ).FirstOrDefault();
            if (LPage != null)
            {

                ptm.ID = LPage.ID;
                ptm.Image_Cover = LPage.Image_Cover;
                ptm.Lang_ID = LPage.Lang_ID;
                ptm.Lang_name = LPage.New_lang.Name;
                ptm.New_Page_ID = LPage.New_Page_ID;
                ptm.page_H_V_tabe = LPage.page_H_V_tabe;
                ptm.Title = LPage.Title;

                foreach (var item in LPage.New_Page_tabe_Content)
                {
                    var result = new Page_Tabe_Content_Model();
                    result.ID = item.ID;
                    result.Page_Tabe_ID = item.Page_Tabe_ID;
                    result.Tabe_title = item.Tabe_title;
                    result.Tab_Description = item.Tab_Description;
                    ptm.TabeContent.Add(result);
                }

            }
            return View(ptm);
        }

        [HttpGet]
        public ActionResult VerticalTabePagePage(int pageID)
        {

            Page_Tabe_Model ptm = new Page_Tabe_Model();
            ptm.TabeContent = new List<Page_Tabe_Content_Model>();
            //var lang = Session["Lang_ID"];
            //if (lang == null)
            //{
            //    Session["Lang_ID"] = dbEntity.New_lang.Where(e => e.DefultLang == 1).FirstOrDefault().ID;
            //    Session["Direction_ID"] = dbEntity.New_lang.Where(e => e.DefultLang == 1).FirstOrDefault().Direction;
            //}
            //int Lang_ID = int.Parse(Session["Lang_ID"].ToString());
            var LPage = dbEntity.New_Page_Tabe.Where(e => e.New_Page_ID == pageID && e.page_H_V_tabe == 1).FirstOrDefault();
            if (LPage != null)
            {
                ptm.ID = LPage.ID;
                ptm.Image_Cover = LPage.Image_Cover;
                ptm.Lang_ID = LPage.Lang_ID;
                ptm.Lang_name = LPage.New_lang.Name;
                ptm.New_Page_ID = LPage.New_Page_ID;
                ptm.page_H_V_tabe = LPage.page_H_V_tabe;
                ptm.Title = LPage.Title;

                foreach (var item in LPage.New_Page_tabe_Content)
                {
                    var result = new Page_Tabe_Content_Model();
                    result.ID = item.ID;
                    result.Page_Tabe_ID = item.Page_Tabe_ID;
                    result.Tabe_title = item.Tabe_title;
                    result.Tab_Description = item.Tab_Description;
                    ptm.TabeContent.Add(result);
                }

            }
            return View(ptm);
        }

        [HttpGet]
        public ActionResult NewsArchive()
        {
            var lang = Session["Lang_ID"];
            if (lang == null)
            {
                Session["Lang_ID"] = dbEntity.New_lang.Where(e => e.DefultLang == 1).FirstOrDefault().ID;
                Session["Direction_ID"] = dbEntity.New_lang.Where(e => e.DefultLang == 1).FirstOrDefault().Direction;
            }
            int Lang_ID = int.Parse(Session["Lang_ID"].ToString());
            var value = dbEntity.New_news.Where(e => e.Lang_ID == Lang_ID).ToList();
            List<NewsModel> LM = new List<NewsModel>();
            foreach (var item in value)
            {
                var result = new NewsModel();
                result.ID = item.ID;
                result.Lang_ID = item.Lang_ID;
                result.ShortDescription = item.ShortDescription;
                result.Shourt_image = item.Shourt_image;
                result.Title = item.Title;
                result.DateTime = item.DateTime;
                LM.Add(result);
            }
            return View(LM);
        }

        [HttpPost]
        public JsonResult GetCountry()
        {
            var lr = GetAllCountry();
            Response.StatusCode = (int)HttpStatusCode.OK;
            return Json(lr.ToList().OrderBy(s => s.Value), JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult PayMent()
        {
            return View();
        }


        [HttpGet]
        [AllowAnonymous]
        public ActionResult LogIn(int MessageID = 0, string Coursid = null)
        {
            if (Coursid != null)
            {
                TempData["Course_ID"] = Coursid;
            }

            //var user= await UserManager.CreateAsync(new MyIdentityUser { Email = "kha@khw.com", UserName = "mater" },"123456");
            //UserModel login = new UserModel();
            var lang = Session["Lang_ID"];
            if (lang == null)
            {
                Session["Lang_ID"] = dbEntity.New_lang.Where(e => e.DefultLang == 1).FirstOrDefault().ID;
                Session["Direction_ID"] = dbEntity.New_lang.Where(e => e.DefultLang == 1).FirstOrDefault().Direction;
            }
            if (MessageID == 1)
            {
                ViewBag.Message = "يوجد خطأ بالإيميل او كلمة المرور!";
            }
            if (MessageID == 2)
            {
                ViewBag.Message = "يجب اضافة إيميل و كلمة المرور!";
            }
            if (MessageID == 3)
            {
                ViewBag.Message = "يجب اضافة إيميل و كلمة المرور!";
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<ActionResult> LogIn(MyIdentityUser loginfo)
        {
            try
            {
              
                if (loginfo.Email != null && loginfo.Password != null)
                {
                    var result = userManager.Users.ToList();
                    var existsUser = await userManager.FindByEmailAsync(loginfo.Email);
                    if (existsUser != null)
                    {
                        var user = await userManager.FindAsync(existsUser.UserName, loginfo.Password);
                        if (user != null)
                        {
                            await SingIn(existsUser);
                            ViewBag.username = user.FirstName;
                            ViewBag.userRole = userManager.GetRoles(user.Id).FirstOrDefault();
                            var userRoles = userManager.GetRoles(existsUser.Id);
                            var role = userRoles.FirstOrDefault();
                            if (role == "Admin" || role == "CategoriesView" || role == "CategoriesEdit" || role == "CategoriesDelete" || role == "LectureView" || role == "LectureEdit" || role == "LectureDelete" || role == "QuestionBankView" || role == "QuestionBankEdit" || role == "QuestionBankDelete" || role == "UsersTeachersView" || role == "UsersTeachersEdit" || role == "UsersTeachersDelete" || role == "UsersStudentView" || role == "UsersStudentEdit" || role == "UsersStudentDelete" || role == "AssingmentView" || role == "AssingmentEdit" || role == "AssingmentDelete" || role == "PaymentView" || role == "PaymentActive" || role == "Chat" || role == "CodeGenerate" || role == "ExamView" || role == "ExamEdit" || role == "ExamDelet")
                            {
                                return RedirectToAction("Index", "ControlPanel");
                            }
                            else if (role == "Teacher")
                            {
                                return RedirectToAction("TeacherCourse", "Teacher");
                            }
                            else if (role.ToLower() == "student".ToLower())
                            {
                                var coursid = TempData["Course_ID"];
                                if (coursid != null)
                                {
                                    int std = int.Parse(coursid.ToString());
                                    var value = dbEntity.Std_Reg_Category.Where(e => e.Category_ID == std && e.Std_ID == user.Id).ToList();
                                    if (value != null && value.Count > 0)
                                    {
                                        Response.StatusCode = (int)HttpStatusCode.OK;
                                        return RedirectToAction("CategoriesStd", "Students");
                                    }
                                    var catID = int.Parse(coursid.ToString());
                                    var category = dbEntity.Categories.Where(e => e.ID == catID || e.ParentCateogry_ID == catID).ToList();
                                    foreach (var item in category)
                                    {


                                        Std_Reg_Category std_Cat = new Std_Reg_Category();
                                        std_Cat.Category_ID = item.ID;
                                        std_Cat.Std_ID = user.Id;

                                        std_Cat.std_category_Ispayed = 0;
                                        if (item.PayedValue != "0")
                                        {
                                            std_Cat.category_Ispayed = 1;
                                        }
                                        else
                                        {
                                            std_Cat.category_Ispayed = 0;
                                        }
                                        dbEntity.Std_Reg_Category.Add(std_Cat);
                                        dbEntity.SaveChanges();
                                    }
                                }
                                return RedirectToAction("CategoriesStd", "Students");
                            }
                        }
                        return RedirectToAction("LogIn", new { MessageID = 1 });
                    }
                    else
                    {

                        return RedirectToAction("LogIn", new { MessageID = 1 });
                    }
                }
            }
            catch (Exception ex)
            {
                Printerror("LogIn", ex.Message, null, ex.StackTrace);
            }
            return RedirectToAction("LogIn", new { MessageID = 2 });
        }
        private async Task SingIn(MyIdentityUser user)
        {

            var identity = await userManager.CreateIdentityAsync(user, DefaultAuthenticationTypes.ApplicationCookie);
            var owinContetext = Request.GetOwinContext();
            var autManager = owinContetext.Authentication;
            autManager.SignIn(identity);
        }

        [HttpGet]
        public ActionResult ForgotPassword(int Message_ID = 0)
        {
            if (Message_ID == 1)
            {
                ViewBag.Message = "Can't Find User Name";
            }
            if (Message_ID == 2)
            {
                ViewBag.Message = "can't change password Now try later";
            }
            return View();
        }

        [HttpPost]
        public JsonResult SendCodeToEmail(string Email)
        {
            try
            {
                //var student = dbEntity.AspNetUsers.Where(e => e.Email == Email).FirstOrDefault();

                //if (student != null)
                //{
                //    Session["StudentUserName"] = student.RealName;
                //    //Random ran = new Random();
                //    //var RanDomNumber = Task.Run(() => ran.Next(125948, 999999));
                //    //Session["code"] = RanDomNumber.Result.ToString();
                //    SendEmailAsync(student.Email, "أكاديمية علوم الدولية", "مرحبا " + student.RealName + "  كلة المرور الخاصة بك هي" + student.RealName);
                //    Response.StatusCode = (int)HttpStatusCode.OK;
                //    return Json(new { success = true, response = "" }, JsonRequestBehavior.AllowGet);
                //}
                //else
                //{
                //    Response.StatusCode = (int)HttpStatusCode.BadRequest;
                //    return Json(new { success = false, response = "" }, JsonRequestBehavior.AllowGet);
                //}
            }
            catch (Exception)
            {
                //Response.StatusCode = (int)HttpStatusCode.BadRequest;
                //return Json(new { success = false, response = "" }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { success = false, response = "" }, JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public ActionResult EnterCode(string email = null, int Message = 0)
        {

            if (email != null)
            {
                ViewBag.email = email;
            }
            if (Message != 0)
            {
                ViewBag.Message = "invaled code please try agine";
            }
            return View();

        }
        [HttpPost]
        public ActionResult EnterCode(string code)
        {
            try
            {
                if (Session["code"].ToString().Equals(code))
                {
                    return RedirectToAction("EnterNewPassword", new { StudentName = Session["StudentUserName"].ToString() });
                }
            }
            catch (Exception)
            {
            }
            return RedirectToAction("EnterCode", new { Message = 1 });

        }
        [HttpGet]
        public ActionResult EnterNewPassword(string StudentName = null, string Message = null)
        {
            if (StudentName != null)
            {
                Session["StudentUserName"] = StudentName;
            }
            if (Message != null)
            {
                ViewBag.Message = Message;
            }
            return View();
        }
        [HttpPost]
        public ActionResult ChangePassword(string newPass)
        {
            try
            {
                //string student = Session["StudentUserName"].ToString();
                //var Newstudent = userManager.Users.Where(e => e.Student_ID.ToString().Equals(student)).FirstOrDefault();

                //if (Newstudent != null)
                //{

                //    UserManager.RemovePassword(Newstudent.Id);

                //    var value = UserManager.AddPassword(Newstudent.Id, newPass);

                //    if (value.Succeeded)
                //    {
                //        ViewBag.User = value.Succeeded;
                //        var userId = Newstudent.Id;
                //        UserManager.AddToRole(userId, "student");
                //        SendEmailAsync(Newstudent.Email, "International college", "you can login now  with username: " + Newstudent.UserName + " and password: " + newPass);

                //        return RedirectToAction("LogIn");
                //    }
                //}
            }
            catch (Exception ex)
            {
                return RedirectToAction("EnterNewPassword", new { Message = ex.Message });
            }
            return RedirectToAction("EnterNewPassword", new { Message = "Please it should more than 6 character" });
        }

        [HttpGet]
        public ActionResult RegesterNewStdView()
        {
            var lang = Session["Lang_ID"];
            if (lang == null)
            {
                Session["Lang_ID"] = dbEntity.New_lang.Where(e => e.DefultLang == 1).FirstOrDefault().ID;
                Session["Direction_ID"] = dbEntity.New_lang.Where(e => e.DefultLang == 1).FirstOrDefault().Direction;
                lang = Session["Lang_ID"];
            }
            int Lang_ID = int.Parse(lang.ToString());
            var value = dbEntity.New_LR.Where(e => e.Lang_ID == Lang_ID).FirstOrDefault();
            LR_Model lr = new LR_Model();
            if (value != null)
            {
                lr.ID = value.ID;
                lr.Lang_ID = value.Lang_ID;
                lr.Title = value.Title;
                lr.Image_cover = value.Image_cover;
                lr.FirstName_tran = value.FirstName_tran;

                lr.LastName_tran = value.LastName_tran;
                lr.Mobile_number_tran = value.Mobile_number_tran;
                lr.Country_tran = value.Country;
                lr.Email_tran = value.Email_tran;
                lr.Password_tran = value.Password_tran;
                lr.Private_one = value.Private_one;
                lr.Private_two = value.Private_two;
                lr.Private_three = value.Private_three;
            }
            return View(lr);
        }

        [HttpGet]
        public ActionResult MoreCourse()
        {
            return View();
        }

        [HttpPost]
        public JsonResult GetAllMoreCourse(string CourseName)
        {
            ViewData["CourseName"] = CourseName;
            List<CategoryModel> L_CM = new List<CategoryModel>();
            //var Langnguages = Session["Lang_ID"];
            try
            {
                //if (Langnguages == null)
                //{
                //    Session["Lang_ID"] = dbEntity.New_lang.Where(e => e.DefultLang == 1).FirstOrDefault().ID;
                //    Session["Direction_ID"] = dbEntity.New_lang.Where(e => e.DefultLang == 1).FirstOrDefault().Direction;
                //}
                //var lang = Session["Lang_ID"];
                //int Lang_ID = int.Parse(lang.ToString());

                //var Direction = Session["Direction_ID"];
                //if (Direction == null)
                //{
                //    Session["Direction_ID"] = dbEntity.New_lang.Where(e => e.DefultLang == 1).FirstOrDefault().Direction;
                //    Direction = Session["Direction_ID"];
                //}
                //int Direction_ID = int.Parse(Direction.ToString());
                var value = HomepageViewModel;
                List<Category> values;
                if (CourseName != null)
                {
                    values = dbEntity.Categories.Where(e => e.CanRegister == 1 && e.NameAr.Contains(CourseName)).ToList();
                }
                else
                {
                    values = dbEntity.Categories.Where(e => e.CanRegister == 1).ToList();
                }
                foreach (var item in values)
                {
                    CategoryModel cm = new CategoryModel();
                    cm.ID = item.ID;
                    cm.NameAr = item.NameAr;
                    cm.NameEn = item.NameEn;
                    cm.IsPayed = item.IsPayed.Value;
                    cm.PayedValue = item.PayedValue;
                    cm.New_Page_ID = item.New_Page_ID.Value;
                    cm.timeEnd = item.EndTime;
                    cm.CanRegister = item.CanRegister.Value;
                    cm.Icon = item.Icon;
                    if (cm.timeEnd != null && DateTime.Parse(cm.timeEnd) >= DateTime.Now)
                    {
                        L_CM.Add(cm);
                    }
                    else if (cm.timeEnd == null)
                    {
                        L_CM.Add(cm);
                    }
                }
                ViewBag.Category = L_CM;
            }
            catch (Exception ex)
            {

            }
            Response.StatusCode = (int)HttpStatusCode.OK;
            return Json(L_CM, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public async Task<JsonResult> RegesterNewStdView(UserModel student)
        {
            try
            {
                if (student.Email == null || student.RealName == null || student.Country == "-1" || student.Password == null || student.PhoneNumber == null)
                {
                    Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return Json(new { success = false, response = "" }, JsonRequestBehavior.AllowGet);
                }
                var result = await CreateStudent(student.RealName, student.RealName, student.Email, student.PhoneNumber, student.Password, student.Country);
                if (result == -3)
                {
                    Response.StatusCode = (int)HttpStatusCode.OK;
                    return Json(new { success = false, responsetext = "يوجد اسم مستخدم بهذا الإيميل" }, JsonRequestBehavior.AllowGet);
                }
                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(new { success = true, response = "" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Session["errormessage"] = ex.Message;
                ViewBag.Sucsess = ex.Message;
                //initilizeList(ref student);

                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = ex.Message }, JsonRequestBehavior.AllowGet);
            }

        }

        [HttpPost]
        public async Task<JsonResult> TackThisCurse(int course_ID)
        {
            try
            {
                var user = CurrentUser;
                if (user != null)
                {
                    Session["username"] = user.FirstName;
                    Session["userRole"] = userManager.GetRoles(user.ID).FirstOrDefault();
                    var value = dbEntity.Std_Reg_Category.Where(e => e.Category_ID == course_ID && e.Std_ID == user.ID).ToList();
                    if (value != null && value.Count > 0)
                    {
                        Response.StatusCode = (int)HttpStatusCode.OK;
                        return Json(new { success = false, response = "أنت بالفعل اشتركت هذا الكورس" }, JsonRequestBehavior.AllowGet);
                    }
                    var category = dbEntity.Categories.Where(e => e.ID == course_ID || e.ParentCateogry_ID == course_ID).ToList();
                    foreach (var item in category)
                    {
                        Std_Reg_Category std_Cat = new Std_Reg_Category();
                        std_Cat.Category_ID = item.ID;
                        std_Cat.Std_ID = user.ID;
                        std_Cat.std_category_Ispayed = 0;
                        if (item.IsPayed != 0)
                        {
                            std_Cat.category_Ispayed = 1;
                        }
                        else
                        {
                            std_Cat.category_Ispayed = 0;
                        }

                        dbEntity.Std_Reg_Category.Add(std_Cat);
                        dbEntity.SaveChanges();
                    }
                    Response.StatusCode = (int)HttpStatusCode.OK;
                    return Json(new { success = true, response = "" }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    TempData["Course_ID"] = course_ID;

                    Response.StatusCode = (int)HttpStatusCode.OK;
                    return Json(new { success = true, responsetext = course_ID }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                Printerror("TackThisCurse", ex.Message, ex.InnerException.Message, ex.StackTrace);
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult GetFastNews()
        {
            List<New_FastNewsModel> L_FN = new List<New_FastNewsModel>();
            try
            {
                //var lang = Session["Lang_ID"];
                //if (lang == null)
                //{
                //    Session["Lang_ID"] = dbEntity.New_lang.Where(e => e.DefultLang == 1).FirstOrDefault().ID;
                //    Session["Direction_ID"] = dbEntity.New_lang.Where(e => e.DefultLang == 1).FirstOrDefault().Direction;
                //}
                //int Lang_ID = int.Parse(Session["Lang_ID"].ToString());
                var values = dbEntity.New_FastNews.ToList();
                foreach (var item in values)
                {
                    New_FastNewsModel fn = new New_FastNewsModel();
                    fn.NewsText = item.NewsText;
                    L_FN.Add(fn);
                }
            }
            catch (Exception ex)
            {
            }
            return Json(L_FN, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult AllTeacher()
        {
            var result = new GridPageModel();
            try
            {
                var lang = Session["Lang_ID"];
                if (lang == null)
                {
                    Session["Lang_ID"] = dbEntity.New_lang.Where(e => e.DefultLang == 1).FirstOrDefault().ID;
                    Session["Direction_ID"] = dbEntity.New_lang.Where(e => e.DefultLang == 1).FirstOrDefault().Direction;
                    lang = Session["Lang_ID"];
                }
                int Lang_ID = int.Parse(lang.ToString());
                var value = dbEntity.NewGridPages.Where(e => e.Pages_ID == 46).FirstOrDefault();

                if (value != null)
                {


                    result.GirdContent = new List<GridpageContentModel>();
                    result.ID = value.ID;
                    result.Lang_ID = value.Lang_ID.Value;
                    result.Pages_ID = value.Pages_ID.Value;
                    result.Title = value.Title;
                    result.Image_Cover = value.Image_Cover;
                    var gridContent = dbEntity.NewGridPage_Content.Where(e => e.GridPageID == value.ID).ToList();
                    foreach (var item1 in gridContent)
                    {
                        var grdCont = new GridpageContentModel();
                        int page_ID = int.Parse(item1.DocLinke.ToString());

                        grdCont.ID = item1.ID;
                        grdCont.Image = item1.Image;
                        grdCont.Description = item1.Description;
                        grdCont.DocLinke = item1.DocLinke;
                        grdCont.Title = item1.Title;
                        grdCont.GridPageID = item1.GridPageID.Value;
                        result.GirdContent.Add(grdCont);
                    }
                }

            }
            catch (Exception ex)
            {
            }
            return View(result);
        }


        [HttpGet]
        public ActionResult Store(int cat_ID=0)
        {
            
            List<StoreProductModel> LSP = new List<StoreProductModel>();
            List<StoreProduct> result = new List<StoreProduct>();
            if (cat_ID!=0)
            {
                  result = dbEntity.StoreProducts.Where(e => e.Category_ID == cat_ID).ToList();
            }else
            {
                  result = dbEntity.StoreProducts.ToList();
            }
            
            foreach (var item in result)
            {
                StoreProductModel model = new StoreProductModel();
                model.ID = item.ID;
                model.name=item.name;
                model.Price = (float)item.Price;
                var image= SpliteString(item.Images, ';');
                model.Images = new List<string>();
                model.Images.Add( image[0]);
                LSP.Add(model);
            }

            var category = dbEntity.StoreCategories.ToList();
            List<StoreCategoryesModel> lscm = new List<StoreCategoryesModel>();
            foreach (var item in category)
            {
                StoreCategoryesModel scm = new StoreCategoryesModel();
                scm.ID = item.ID;
                scm.Name = item.Name;
                scm.ProductStoreCount = item.StoreProducts.Count.ToString();
                lscm.Add(scm);
            }
            ViewBag.StoreCategory = lscm;
            return View(LSP);
        }
        [HttpGet]
        public ActionResult ProductDetiles(int ProID)
        {
            StoreProductModel spm = new StoreProductModel();
            try
            {
                var result = dbEntity.StoreProducts.Find(ProID);
                if (result!=null)
                {
                    spm.ID = result.ID;
                    spm.name = result.name;
                    spm.categoryName = result.StoreCategory.Name;
                    spm.Description = result.Description;
                    spm.Price =(float) result.Price;
                    List<string> images = SpliteString(result.Images, ';');
                    spm.Images = new List<string>();
                    foreach (var item in images)
                    {
                        spm.Images.Add(item.ToString());
                    }
                    var products=result.StoreCategory.StoreProducts.ToList();
                 List<StoreProductModel> L_spm = new List<StoreProductModel>();
                    int productcount = 0;
                    foreach (var item in products)
                    {
                        if (productcount<3)
                        {
                            StoreProductModel pm = new StoreProductModel();
                            pm.ID = item.ID;
                            pm.name = item.name;
                            pm.Price = (float)item.Price;
                            pm.Images = new List<string>();
                            pm.Images = SpliteString(item.Images, ';');
                            L_spm.Add(pm);
                            productcount++;
                        }else
                        {
                            break;
                        }
                     
                    }
                    ViewBag.products= L_spm;
                }
                
            }
            catch (Exception ex)
            {
            }
            return View(spm);
        }
      
 
    }

}

