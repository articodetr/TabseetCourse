using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Services.Description;
using tabseetCourse.Classes;
using tabseetCourse.Data;
using tabseetCourse.Models;
using tabseetCourse.Models_UI;

namespace tabseetCourse.Controllers
{
    public class ControlpanelController : SharedCalss
    {
        public UserModel _UsermodelModel { get; set; }
        public ControlpanelController()
        {
            //this._UsermodelModel = new UserModel();//has property PageTitle
            ;
            if (CurrentUser != null)
            {


                this._UsermodelModel = CurrentUser;

                this.ViewData["_UsermodelModel"] = this._UsermodelModel;
            }

        }
        // GET: Controlpanel//
        public ActionResult Index()
        {
            var username = _UsermodelModel;
            if (username != null)
            {
                Session["username"] = username.RealName;
                List<string> roleaccepted = new List<string>();
                roleaccepted.Add("Admin");
                roleaccepted.Add("CategoriesView");
                roleaccepted.Add("CategoriesEdit");
                roleaccepted.Add("CategoriesDelete");
                roleaccepted.Add("LectureView");
                roleaccepted.Add("LectureEdit");
                roleaccepted.Add("LectureDelete");
                roleaccepted.Add("QuestionBankView");
                roleaccepted.Add("QuestionBankEdit");
                roleaccepted.Add("QuestionBankDelete");
                roleaccepted.Add("UsersStudentView");
                roleaccepted.Add("UsersTeachersView");
                roleaccepted.Add("UsersTeachersEdit");
                roleaccepted.Add("UsersTeachersDelete");
                roleaccepted.Add("QuestionBankDelete");
                roleaccepted.Add("UsersStudentEdit");
                roleaccepted.Add("UsersStudentDelete");
                roleaccepted.Add("AssingmentView");
                roleaccepted.Add("AssingmentEdit");
                roleaccepted.Add("PaymentView");
                roleaccepted.Add("PaymentActive");
                roleaccepted.Add("Chat");
                roleaccepted.Add("ExamView");
                roleaccepted.Add("ExamEdit");
                roleaccepted.Add("ExamDelet");
                roleaccepted.Add("CodeGenerate");

                bool resut = IsTrustedUser(username, roleaccepted);
                if (resut == true)
                {
                    ViewBag.StudentCount = userManager.Users.Where(e => e.role == Roles.Student.ToString()).Count();
                    ViewBag.PayedCategory = dbEntity.Std_Reg_Category.Where(e => e.category_Ispayed == 1).Count();
                    ViewBag.FreeCategory = dbEntity.Std_Reg_Category.Where(e => e.category_Ispayed == 0).Count();
                    ViewBag.Certfication = dbEntity.Std_ExamFinish.Where(e => e.isFinishExam == 1).Count();
                    return View();
                }
            }
            return RedirectToAction("LogIn", "Home");
        }

        #region CategoriesType
        //[Authorize(Roles = "Admin")]
        [HttpGet]
        public ActionResult CategoriesType()
        {
            List<CategoryTypeModel> L_ctm = new List<CategoryTypeModel>();
            try
            {
                var values = dbEntity.CategoryTypes.ToList();
                foreach (var item in values)
                {
                    CategoryTypeModel ctm = new CategoryTypeModel();
                    ctm.ID = item.ID;
                    ctm.NameAR = item.NameAR;
                    ctm.NameEN = item.NameEn;
                    //ctm.ParentID = item.ParentType.Value;
                    L_ctm.Add(ctm);
                }
                var username = _UsermodelModel;
                if (username != null)
                {
                    Session["username"] = username.RealName;
                    var role = username.L_role;
                    if (role.Count > 0)
                    {
                        foreach (var item in role)
                        {
                            if (item.RoleName == "Admin")
                            {
                                return View(L_ctm);
                            }

                        }
                    }


                }

            }
            catch (Exception ex)
            {

            }
            return RedirectToAction("LogIn", "Home");
        }
        [HttpPost]
        public JsonResult GetAllCategoryType()
        {
            List<CategoryTypeModel> L_ctm = new List<CategoryTypeModel>();
            try
            {
                var values = dbEntity.CategoryTypes.ToList();
                foreach (var item in values)
                {
                    CategoryTypeModel ctm = new CategoryTypeModel();
                    ctm.ID = item.ID;
                    ctm.NameAR = item.NameAR;
                    ctm.NameEN = item.NameEn;
                    L_ctm.Add(ctm);
                }
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = "" }, JsonRequestBehavior.AllowGet);
            }
            Response.StatusCode = (int)HttpStatusCode.OK;
            return Json(L_ctm, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public JsonResult CreateCategoriesType(CategoryTypeModel ctm)
        {
            using (var context = new tbseedDBEntities())
            {
                using (DbContextTransaction dbTran = context.Database.BeginTransaction())
                {
                    try
                    {
                        CategoryType ct = new CategoryType();

                        ct.NameAR = ctm.NameAR;
                        ct.NameEn = ctm.NameEN;

                        if (ctm.ParentID != 0)
                        {
                            ct.ParentType = ctm.ParentID;
                        }
                        context.CategoryTypes.Add(ct);
                        context.SaveChanges();
                        New_pages pg = new New_pages();
                        pg.Page_Type = 7;

                        pg.name = ctm.NameAR;
                        pg.IsUiUx = 1;
                        pg.CategoryType_ID = ct.ID;
                        context.New_pages.Add(pg);
                        context.SaveChanges();
                        var gp = new NewGridPage();
                        gp.Pages_ID = pg.ID;
                        gp.Title = pg.name;

                        context.NewGridPages.Add(gp);
                        context.SaveChanges();
                        dbTran.Commit();
                        Response.StatusCode = (int)HttpStatusCode.OK;
                        return Json(new { success = true, response = "" }, JsonRequestBehavior.AllowGet);
                    }

                    catch (Exception ex)
                    {
                        dbTran.Rollback();
                        Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        return Json(new { success = false, response = "" }, JsonRequestBehavior.AllowGet);
                    }
                }
            }
        }
        [HttpPost]
        public JsonResult GetCategoriesTypeByID(int ctmid)
        {
            try
            {
                CategoryTypeModel ctm = new CategoryTypeModel();
                var value = dbEntity.CategoryTypes.Find(ctmid);
                if (value != null)
                {
                    ctm.ID = value.ID;
                    ctm.NameAR = value.NameAR;
                    ctm.NameEN = value.NameEn;
                    if (value.ParentType != null)
                    {
                        ctm.ParentID = value.ParentType.Value;

                    }
                    return Json(ctm, JsonRequestBehavior.AllowGet);
                }
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = "Can't find Categories type" }, JsonRequestBehavior.AllowGet);
            }

            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        public JsonResult EditeCategoriesType(CategoryTypeModel ctm)
        {
            try
            {
                var value = dbEntity.CategoryTypes.Find(ctm.ID);
                if (value != null)
                {
                    value.NameAR = ctm.NameAR;
                    value.NameEn = ctm.NameEN;
                    if (ctm.ParentID != 0)
                    {
                        value.ParentType = ctm.ParentID;
                    }

                    dbEntity.CategoryTypes.Attach(value);
                    dbEntity.Entry(value).State = System.Data.Entity.EntityState.Modified;
                    dbEntity.SaveChanges();
                    Response.StatusCode = (int)HttpStatusCode.OK;
                    return Json(new { success = true, response = "" }, JsonRequestBehavior.AllowGet);
                }
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = "Can't find Categories type" }, JsonRequestBehavior.AllowGet);
            }

            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        public JsonResult DeleteCategoriesType(int cattID)
        {
            try
            {
                var value = dbEntity.CategoryTypes.Find(cattID);
                if (value != null)
                {
                    dbEntity.CategoryTypes.Remove(value);
                    dbEntity.SaveChanges();
                    Response.StatusCode = (int)HttpStatusCode.OK;
                    return Json(new { success = true, response = "" }, JsonRequestBehavior.AllowGet);
                }
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = "Can't find Categories type" }, JsonRequestBehavior.AllowGet);
            }

            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        public JsonResult GetAllCategoriesType()
        {
            List<CategoryTypeModel> L_ctm = new List<CategoryTypeModel>();
            try
            {
                var values = dbEntity.CategoryTypes.ToList();
                foreach (var item in values)
                {
                    CategoryTypeModel ctm = new CategoryTypeModel();
                    ctm.ID = item.ID;
                    ctm.NameAR = item.NameAR;
                    ctm.NameEN = item.NameEn;
                    L_ctm.Add(ctm);
                }
            }
            catch (Exception ex)
            {

            }
            Response.StatusCode = (int)HttpStatusCode.OK;
            return Json(L_ctm, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region Categories
        //[Authorize(Roles = "Admin, CategoriesView, CategoriesEdit , CategoriesDelete")]
        [HttpGet]
        public ActionResult Categories()
        {
            List<CategoryModel> L_ctm = new List<CategoryModel>();
            bool isAuthorize = false;
            try
            {
                var username = _UsermodelModel;
                if (username != null)
                {
                    Session["username"] = username.RealName;
                    var role = username.L_role;
                    if (role.Count > 0)
                    {
                        List<string> roleaccepted = new List<string>();
                        roleaccepted.Add("Admin");
                        roleaccepted.Add("CategoriesView");
                        roleaccepted.Add("CategoriesEdit");
                        roleaccepted.Add("CategoriesDelete");
                        isAuthorize = IsTrustedUser(username, roleaccepted);
                    }
                }


                if (isAuthorize == true)
                {
                    var values = dbEntity.Categories
                               .Select(x => new { x.ID, x.NameAr, x.NameEn, x.Category1, x.CategoryType_ID, x.New_Page_ID, x.CategoryType.NameAR, x.IsPayed, x.PayedValue, x.ParentCateogry_ID, examName = x.Exams.FirstOrDefault().Name, x.CanRegister }).OrderByDescending(i => i.ID);
                    foreach (var item in values)
                    {
                        CategoryModel ctm = new CategoryModel();
                        ctm.ID = item.ID;
                        ctm.NameAr = item.NameAr;
                        ctm.NameEn = item.NameEn;
                        ctm.New_Page_ID = item.New_Page_ID.Value;
                        ctm.exam_Name = item.examName;
                        if (item.CanRegister != null)
                        {
                            ctm.CanRegister = item.CanRegister.Value;
                        }
                        if (item.CategoryType_ID != null)
                        {
                            ctm.CategoryType_ID = item.CategoryType_ID.Value;
                            ctm.CategoryTypeName = item.NameAR;
                        }
                        if (item.IsPayed != 0)
                        {
                            ctm.IsPayed = 1;
                            ctm.PayedValue = item.PayedValue;
                        }
                        else
                        {

                            ctm.IsPayed = 0;
                            ctm.PayedValue = "0";
                        }
                        if (item.ParentCateogry_ID != null)
                        {
                            ctm.ParentCateogry_ID = item.ParentCateogry_ID.Value;
                            ctm.ParentCategoryName = item.Category1.NameAr;
                        }

                        L_ctm.Add(ctm);
                    }
                    return View(L_ctm);
                }
            }
            catch (Exception ex)
            {

            }
            return RedirectToAction("LogIn", "Home");
        }
        //[Authorize(Roles = "Admin, Teacher, Viewcategores , Editcategores")]
        [HttpPost]
        public JsonResult GetCategoriesByType(int type)
        {
            try
            {
                var categorytype = dbEntity.CategoryTypes.Where(e => e.ID == type).FirstOrDefault();
                if (categorytype != null)
                {
                    //var value2 = dbEntity.Categories.Where(i => i.CategoryType_ID == type).Select(i => new { i.ID, i.NameAr }).ToList();
                    //var value1 = dbEntity.Categories.Where(i => i.CategoryType_ID == categorytype.ID).Select(i => new { i.ID, i.NameAr, i.CategoryType_ID }).ToList();
                    var value = dbEntity.Categories.Where(i => i.CategoryType_ID == categorytype.ParentType).Select(i => new { i.ID, i.NameAr, i.CategoryType_ID }).ToList();

                    if (value != null)
                    {
                        Response.StatusCode = (int)HttpStatusCode.OK;
                        return Json(value, JsonRequestBehavior.AllowGet);
                    }
                }
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = "Can't find Categories" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        public JsonResult GetAllCategories()
        {
            var value = dbEntity.Categories.Select(i => new { i.ID, i.NameAr, i.CategoryType_ID });
            Response.StatusCode = (int)HttpStatusCode.OK;
            return Json(value, JsonRequestBehavior.AllowGet);
        }
        //[Authorize(Roles = "Admin, Teacher, Viewcategores , Editcategores")]
        [HttpPost]
        public JsonResult CreateCategories(CategoryModel value)
        {
            using (var context = new tbseedDBEntities())
            {
                using (DbContextTransaction dbTran = context.Database.BeginTransaction())
                {
                    try
                    {
                        var isroot = context.CategoryTypes.Where(e => e.ID == value.CategoryType_ID).FirstOrDefault();
                        New_pages pg = new New_pages();
                        if (isroot.ParentType == null || isroot.ParentType == 0)
                        {
                      
                            pg.Page_Type = 7;//Grid page
                        }
                        else
                        {
                            pg.Page_Type = 4;//virtical tabe page

                        }

                        pg.name = value.NameAr;
                        pg.IsUiUx = 0;
                        pg.CategoryType_ID = isroot.ID;
                        context.New_pages.Add(pg);
                        context.SaveChanges();
                        if (pg.Page_Type == 7)
                        {
                            var gp = new NewGridPage();
                            gp.Pages_ID = pg.ID;
                            gp.Title = pg.name;
                            if (value.UploadCover != null)
                            {
                                gp.Image_Cover = SaveImage(value.UploadCover);
                            }
                            context.NewGridPages.Add(gp);
                            context.SaveChanges();
                        }
                        else if (pg.Page_Type == 4)
                        {
                            var pt = new New_Page_Tabe();

                            pt.Title = pg.name;
                            pt.New_Page_ID = pg.ID;
                            pt.Lang_ID = 1;
                            pt.page_H_V_tabe = 1;
                            context.New_Page_Tabe.Add(pt);
                            context.SaveChanges();
                            var newpage = context.New_pages.Where(e => e.CategoryType_ID == isroot.ID).FirstOrDefault();
                            var gp = context.NewGridPages.Where(e => e.Pages_ID == newpage.ID).FirstOrDefault();
                            var gpcontent = new NewGridPage_Content();
                            gpcontent.Title = value.NameAr;
                            gpcontent.GridPageID = gp.ID;
                            gpcontent.DocLinke = gp.ID.ToString(); ;
                            gpcontent.Description = value.Description;
                            context.NewGridPage_Content.Add(gpcontent);
                            context.SaveChanges();
                        }

                        Category ctm = new Category();
                        ctm.NameAr = value.NameAr;
                        ctm.NameEn = value.NameEn;
                        ctm.Description = value.Description;
                        if (value.CanRegister == 1)
                        {
                            ctm.CanRegister = value.CanRegister;
                            ctm.IsPayed = value.IsPayed;
                            ctm.PayedValue = value.PayedValue;
                            ctm.EndTime = value.timeEnd;
                        }
                        else
                        {
                            ctm.CanRegister = value.CanRegister;
                            ctm.IsPayed = 0;
                            ctm.PayedValue = string.Empty;
                            ctm.EndTime = string.Empty;
                        }


                        if (value.FileImagePost != null)
                        {
                            ctm.CertificationAR = SaveImage(value.FileImagePost);
                        }

                        if (value.FileImagePost_EN != null)
                        {
                            ctm.CertificationEN = SaveImage(value.FileImagePost_EN);
                        }
                        if (value.UploadIcon != null)
                        {
                            ctm.Icon = SaveImage(value.UploadIcon);
                        }
                        if (value.UploadDocumentBook != null)
                        {
                            ctm.DocumentBook = SaveImage(value.UploadDocumentBook);
                        }
                        if (value.CategoryType_ID != 0)
                        {
                            ctm.CategoryType_ID = value.CategoryType_ID;
                        }
                        if (value.ParentCateogry_ID != 0)
                        {
                            ctm.ParentCateogry_ID = value.ParentCateogry_ID;
                        }

                        ctm.New_Page_ID = pg.ID;
                        context.Categories.Add(ctm);
                        context.SaveChanges();


                        dbTran.Commit();
                        //SendEmailAsync(Email, supject, message);
                        Response.StatusCode = (int)HttpStatusCode.OK;
                        return Json(new { success = true, response = "" }, JsonRequestBehavior.AllowGet);
                    }

                    catch (Exception ex)
                    {
                        dbTran.Rollback();
                        Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        return Json(new { success = false, response = ex.Message }, JsonRequestBehavior.AllowGet);
                    }
                }
            }
        }
        //[Authorize(Roles = "Admin, Teacher, Viewcategores , Editcategores")]
        [HttpPost]
        public JsonResult EditCategorie(int catID)
        {
            try
            {
                CategoryModel ctm = new CategoryModel();
                var value = dbEntity.Categories.Select(i => new { i.ID, i.NameAr, i.NameEn, i.CategoryType_ID, i.IsPayed, i.ParentCateogry_ID, i.PayedValue, i.EndTime, i.CanRegister, i.Description }).Where(i => i.ID == catID).FirstOrDefault();
                if (value != null)
                {
                    ctm.ID = value.ID;
                    ctm.NameAr = value.NameAr;
                    ctm.NameEn = value.NameEn;
                    ctm.timeEnd = value.EndTime;
                    ctm.Description = value.Description;
                    if (value.CanRegister != null)
                    {
                        ctm.CanRegister = value.CanRegister.Value;
                    }
                    if (value.CategoryType_ID != null)
                    {
                        ctm.CategoryType_ID = value.CategoryType_ID.Value;
                    }
                    if (value.IsPayed != null)
                    {
                        ctm.IsPayed = value.IsPayed.Value;
                        ctm.PayedValue = value.PayedValue;
                    }
                    if (value.ParentCateogry_ID != null)
                    {
                        ctm.ParentCateogry_ID = value.ParentCateogry_ID.Value;
                    }
                    Response.StatusCode = (int)HttpStatusCode.OK;
                    return Json(ctm, JsonRequestBehavior.AllowGet);
                }
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = "Can't find Categories" }, JsonRequestBehavior.AllowGet);
            }

            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        //[Authorize(Roles = "Admin, Teacher, Viewcategores , Editcategores")]
        [HttpPost]
        public JsonResult EditeCategories(CategoryModel ctm)
        {
            try
            {
                var value = dbEntity.Categories.Find(ctm.ID);
                if (value != null)
                {
                    //ctm.ID = value.ID;
                    value.NameAr = ctm.NameAr;
                    value.NameEn = ctm.NameEn;
                    value.Description = ctm.Description;

                    if (ctm.CategoryType_ID != 0)
                    {
                        value.CategoryType_ID = ctm.CategoryType_ID;
                    }
                    if (ctm.FileImagePost != null)
                    {
                        value.CertificationAR = SaveImage(ctm.FileImagePost);
                    }
                    if (ctm.FileImagePost_EN != null)
                    {
                        value.CertificationEN = SaveImage(ctm.FileImagePost_EN);
                    }
                    if (ctm.UploadIcon != null && ctm.UploadIcon.ContentLength > 0)
                    {
                        value.Icon = SaveImage(ctm.UploadIcon);
                    }
                    if (ctm.UploadDocumentBook != null && ctm.UploadDocumentBook.ContentLength > 0)
                    {
                        value.DocumentBook = SaveImage(ctm.UploadDocumentBook);
                    }
                    if (ctm.UploadCover != null && ctm.UploadCover.ContentLength > 0)
                    {
                        var result = dbEntity.New_pages.Find(value.New_Page_ID);
                        if (result != null)
                        {
                            if (result.Page_Type == 7)
                            {
                                var gp = dbEntity.NewGridPages.Find(result.ID);
                                if (gp != null)
                                {
                                    gp.Image_Cover = SaveImage(ctm.UploadCover);
                                    dbEntity.NewGridPages.Attach(gp);
                                    dbEntity.Entry(value).State = System.Data.Entity.EntityState.Modified;
                                    dbEntity.SaveChanges();
                                }
                            }
                            else if (result.Page_Type == 4)
                            {

                            }
                        }

                    }

                    value.CanRegister = ctm.CanRegister;
                    if (value.CanRegister == 1)
                    {
                        if (ctm.IsPayed != 0)
                        {
                            value.IsPayed = ctm.IsPayed;
                            value.PayedValue = ctm.PayedValue;
                        }
                        else
                        {
                            value.IsPayed = 0;
                            value.PayedValue = "0";
                        }
                        value.EndTime = ctm.timeEnd;
                    }
                    else
                    {
                        value.IsPayed = 0;
                        value.PayedValue = "0";
                        value.EndTime = string.Empty;
                    }

                    if (ctm.ParentCateogry_ID != 0)
                    {
                        value.ParentCateogry_ID = ctm.ParentCateogry_ID;
                    }
                    dbEntity.Categories.Attach(value);
                    dbEntity.Entry(value).State = System.Data.Entity.EntityState.Modified;
                    dbEntity.SaveChanges();
                    Response.StatusCode = (int)HttpStatusCode.OK;
                    return Json(new { success = true, response = "" }, JsonRequestBehavior.AllowGet);
                }
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = "Can't find Categories" }, JsonRequestBehavior.AllowGet);
            }

            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        //[Authorize(Roles = "Admin, Teacher, Viewcategores , Editcategores")]
        [HttpPost]
        public JsonResult DeleteCategories(int Categories_ID)
        {
            try
            {
                var value = dbEntity.Categories.Find(Categories_ID);
                if (value != null)
                {
                    dbEntity.Categories.Remove(value);
                    dbEntity.SaveChanges();
                    Response.StatusCode = (int)HttpStatusCode.OK;
                    return Json(new { success = true, response = "" }, JsonRequestBehavior.AllowGet);
                }
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { response = "Can't find Categories" }, JsonRequestBehavior.AllowGet);
            }

            catch (Exception ex)
            {

                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { response = "Can't delete linked with other data" }, JsonRequestBehavior.AllowGet);
            }
        }

        //[Authorize(Roles = "Admin")]
        [HttpGet]
        public ActionResult ActiveCourse()
        {
            List<Std_Reg_CategoryModel> L_SRCM = new List<Std_Reg_CategoryModel>();
            try
            {
                bool isAuthorize = false;
                var username = _UsermodelModel;
                if (username != null)
                {
                    Session["username"] = username.RealName;
                    var role = username.L_role;

                    List<string> roleaccepted = new List<string>();
                    roleaccepted.Add("Admin");
                    roleaccepted.Add("PaymentView");
                    roleaccepted.Add("PaymentActive");
                    isAuthorize = IsTrustedUser(username, roleaccepted);


                }
                if (isAuthorize == true)
                {
                    var result = dbEntity.Std_Reg_Category.Where(e => e.category_Ispayed == 1 && e.std_category_Ispayed == 0).ToList();
                    foreach (var item in result)
                    {
                        Std_Reg_CategoryModel srcm = new Std_Reg_CategoryModel();
                        srcm.ID = item.ID;
                        srcm.CategoryName = item.Category.NameAr;
                        srcm.Category_ID = item.Category_ID.Value;
                        srcm.std_category_Ispayed = item.std_category_Ispayed.Value;
                        srcm.category_Ispayed = item.category_Ispayed.Value;
                        srcm.Std_ID = item.Std_ID;
                        srcm.Student_Name = dbEntity.AspNetUsers.FirstOrDefault(e => e.Id == item.Std_ID).RealName;
                        //srcm.Student_Name = item.AspNetUser.RealName;

                        if (item.PayMent_Method != null)
                        {
                            srcm.PayMent_Method = item.PayMent_Method.Value;
                        }
                        srcm.PayMent_Value = item.PayMent_Value;
                        srcm.Code = item.Code;
                        srcm.TransactionFile = item.TransactionFile;
                        L_SRCM.Add(srcm);
                    }
                    return View(L_SRCM);
                }
            }
            catch (Exception ex)
            {
            }
            return RedirectToAction("LogIn", "Home");

        }
        //[Authorize(Roles = "Admin")]
        [HttpGet]
        public ActionResult ActiveStudenCourse(int Reg_ID)
        {
            try
            {
                bool isAuthorize = false;
                var username = _UsermodelModel;
                if (username != null)
                {
                    Session["username"] = username.RealName;
                    if (username != null)
                    {
                        Session["username"] = username.RealName;
                        var role = username.L_role;
                        List<string> roleaccepted = new List<string>();
                        roleaccepted.Add("Admin");
                        roleaccepted.Add("PaymentActive");
                        isAuthorize = IsTrustedUser(username, roleaccepted);
                    }
                }

                if (isAuthorize == true)
                {
                    var result = dbEntity.Std_Reg_Category.Find(Reg_ID);

                    if (result != null)
                    {

                        result.std_category_Ispayed = 1;
                        dbEntity.Std_Reg_Category.Attach(result);
                        dbEntity.Entry(result).State = EntityState.Modified;
                        dbEntity.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                Printerror("ActiveStudenCourse", ex.Message, ex.InnerException.ToString(), ex.StackTrace);
            }
            return RedirectToAction("ActiveCourse");
        }
        [HttpPost]
        public JsonResult GetActivateCourseCounter()
        {
            int counter = dbEntity.Std_Reg_Category.Where(e => e.category_Ispayed == 1 && e.std_category_Ispayed == 0).ToList().Count;
            Response.StatusCode = (int)HttpStatusCode.OK;
            return Json(new { success = true, Count = counter }, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Lectures
        //[Authorize(Roles = "Admin, LectureView ")]
        [HttpGet]
        public ActionResult Lectures()
        {
            bool isAuthorize = false;
            var username = _UsermodelModel;
            if (username != null)
            {
                Session["username"] = username.RealName;
                if (username != null)
                {
                    Session["username"] = username.RealName;
                    var role = username.L_role;
                    List<string> roleaccepted = new List<string>();
                    roleaccepted.Add("Admin");
                    roleaccepted.Add("LectureView");
                    roleaccepted.Add("LectureEdit");
                    roleaccepted.Add("LectureDelete");
                    isAuthorize = IsTrustedUser(username, roleaccepted);
                }
            }
            if (isAuthorize)
            {
                return View();
            }
            return RedirectToAction("LogIn", "Home");
        }
        //[Authorize(Roles = "Admin, LectureView")]
        [HttpGet]
        public ActionResult LecturesByCategory(int Category_ID, string categoryName)
        {

            ViewBag.CategoryID = Category_ID;
            ViewBag.Name = categoryName;
            return View();
        }
        //[Authorize(Roles = "Admin, Teacher, ViewLectures ,  EditLectures")]
        [HttpPost]
        public JsonResult AllLectures(int byCategorieType = 0, int byCategorie = 0)
        {
            try
            {
                var values = dbEntity.Lectures.Where(i => (byCategorie == 0 && byCategorieType == 0) || (byCategorie == 0 && i.Category.CategoryType_ID == byCategorieType) || (i.Category.CategoryType_ID == byCategorieType || i.Category_ID == byCategorie))
                    .Select(i => new { i.ID, i.Lecture_Name, i.Category_ID, i.Category.NameAr, i.Category.NameEn, i.LectureTypeOnOff, i.LectureLink_Host, i.LectureLink_Client, i.Assignments.Count }).ToList();
                if (values != null)
                {
                    Response.StatusCode = (int)HttpStatusCode.OK;
                    return Json(values, JsonRequestBehavior.AllowGet);
                }

                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = "Can't find Categories" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        //[Authorize(Roles = "Admin, Teacher, ViewLectures ,  EditLectures")]
        [HttpPost]
        public async Task<JsonResult> CreateLectuer(LectureModel LM)
        {
            try
            {
                Lecture l = new Lecture();
                LectureModel lm = new LectureModel();
                if (LM.LectureTypeOnOff == "1")
                {


                    var result = _UsermodelModel;

                    if (result != null)
                    {
                        lm = await GenerateLectureLink(LM.Lecture_Name, "isa.academy2020@gmail.com", LM.DayandTime);
                        if (lm.Code == "201")
                        {
                            LM.LectureLink_Client = lm.LectureLink_Client;
                            LM.LectureLink_Host = lm.LectureLink_Host;
                        }
                        else
                        {
                            Response.StatusCode = (int)HttpStatusCode.OK;
                            return Json(new { success = false, responseText = "Zoom Message " + lm.Message }, JsonRequestBehavior.AllowGet);

                        }
                    }
                    else
                    {
                        Response.StatusCode = (int)HttpStatusCode.OK;
                        return Json(new { success = false, responseText = "لا يوجد حساب على الزوم" }, JsonRequestBehavior.AllowGet);
                    }
                }
                if (LM.LectureTypeOnOff == "1")
                {
                    l.LectureLink_Client = LM.LectureLink_Client;
                    l.LectureLink_Host = LM.LectureLink_Host;
                    l.LectureTypeOnOff = "1";
                    l.DayandTime = LM.DayandTime;
                    l.LectureMinTime = "0";
                }
                else
                {
                    l.LectureLink_Client = LM.LectureLink_Client;
                    l.LectureLink_Host = string.Empty;
                    l.LectureTypeOnOff = "0";
                    l.DayandTime = string.Empty;
                    l.LectureMinTime = LM.LectureMinTime.ToString();
                }
                l.Category_ID = LM.Category_ID;
                l.Lecture_Name = LM.Lecture_Name;
                l.MediaLink = LM.MediaLink;
                l.Others = LM.Others;
                if (LM.DocumentFileUpload != null)
                {
                    l.DocumenFile = SaveImage(LM.DocumentFileUpload);
                }
                dbEntity.Lectures.Add(l);
                dbEntity.SaveChanges();
                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(new { success = true, responseText = "" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(new { success = false, responseText = ex.Message }, JsonRequestBehavior.AllowGet);
            }

        }
        //[Authorize(Roles = "Admin, Teacher, ViewLectures ,  EditLectures")]
        [HttpPost]
        public JsonResult EditLecture(int id)
        {
            LectureModel lm = new LectureModel();
            try
            {
                var value = dbEntity.Lectures.Find(id);
                if (value != null)
                {
                    lm.ID = value.ID;
                    lm.Category_ID = value.Category_ID;
                    lm.DayandTime = value.DayandTime;
                    lm.LectureLink_Client = value.LectureLink_Client;
                    lm.LectureLink_Host = value.LectureLink_Host;
                    lm.LectureTypeOnOff = value.LectureTypeOnOff;
                    lm.Lecture_Name = value.Lecture_Name;
                    lm.MediaLink = value.MediaLink;
                    lm.Others = value.Others;
                    lm.DocumenFile = value.DocumenFile;
                    if (value.LectureMinTime != null)
                    {
                        int minitime;
                        if (int.TryParse(value.LectureMinTime, out minitime))
                        {
                            lm.LectureMinTime = minitime;
                        }


                    }



                }
                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(lm, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = ex.Message }, JsonRequestBehavior.AllowGet);
            }

        }
        //[Authorize(Roles = "Admin, Teacher, ViewLectures ,  EditLectures")]
        [HttpPost]
        public async Task<JsonResult> EditLectuer(LectureModel LM)
        {
            try
            {
                var l = dbEntity.Lectures.Find(LM.ID);
                LectureModel lm = new LectureModel();
                if (LM.LectureTypeOnOff == "1")
                {

                    string code;
                    string LectureLink_Host;
                    string LectureLink_Client;
                    string errorMessage;
                    var result = _UsermodelModel;

                    if (result != null)
                    {
                        lm = await GenerateLectureLink(LM.Lecture_Name, "isa.academy2020@gmail.com", LM.DayandTime);
                        if (lm.Code == "201")
                        {

                            l.Category_ID = LM.Category_ID;
                            l.DayandTime = LM.DayandTime;
                            l.LectureLink_Client = lm.LectureLink_Client;
                            l.LectureLink_Host = lm.LectureLink_Host;
                            l.LectureTypeOnOff = "1";
                            l.Lecture_Name = LM.Lecture_Name;
                            l.MediaLink = LM.MediaLink;
                            l.Others = LM.Others;
                            l.LectureMinTime = "0";
                            if (LM.DocumentFileUpload != null)
                            {
                                l.DocumenFile = SaveImage(LM.DocumentFileUpload);
                            }
                            dbEntity.Lectures.Attach(l);
                            dbEntity.Entry(l).State = System.Data.Entity.EntityState.Modified;
                            dbEntity.SaveChanges();
                        }
                        else
                        {
                            Response.StatusCode = (int)HttpStatusCode.OK;
                            return Json(new { success = false, responseText = "Zoom Message " + lm.Message }, JsonRequestBehavior.AllowGet);

                        }
                    }
                    else
                    {
                        Response.StatusCode = (int)HttpStatusCode.OK;
                        return Json(new { success = false, responseText = "لا يوجد حساب على الزوم" }, JsonRequestBehavior.AllowGet);
                    }
                }
                else
                {

                    l.Category_ID = LM.Category_ID;
                    l.DayandTime = string.Empty;
                    l.LectureLink_Client = LM.LectureLink_Client;
                    l.LectureLink_Host = string.Empty;
                    l.LectureTypeOnOff = "0";
                    l.Lecture_Name = LM.Lecture_Name;
                    l.MediaLink = LM.MediaLink;
                    l.Others = LM.Others;
                    l.LectureMinTime = LM.LectureMinTime.ToString();
                    if (LM.DocumentFileUpload != null)
                    {
                        l.DocumenFile = SaveImage(LM.DocumentFileUpload);
                    }
                    dbEntity.Lectures.Attach(l);
                    dbEntity.Entry(l).State = System.Data.Entity.EntityState.Modified;
                    dbEntity.SaveChanges();
                }
                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(new { success = true, response = "" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        //[Authorize(Roles = "Admin, Teacher, ViewLectures ,  EditLectures")]
        [HttpPost]
        public JsonResult DeleteLecture(int ID)
        {
            try
            {
                var value = dbEntity.Lectures.Find(ID);
                dbEntity.Lectures.Remove(value);
                dbEntity.SaveChanges();
                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(new { success = true, response = "" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public ActionResult GetStdCourse(string std_ID)
        {
            try
            {
                bool isAuthorize = false;
                var username = _UsermodelModel;
                ViewBag.stdname = dbEntity.AspNetUsers.Find(std_ID).RealName;
                if (username != null)
                {
                    Session["username"] = username.RealName;
                    if (username != null)
                    {
                        Session["username"] = username.RealName;
                        var role = username.L_role;
                        List<string> roleaccepted = new List<string>();
                        roleaccepted.Add("Admin");
                        roleaccepted.Add("PaymentActive");
                        isAuthorize = IsTrustedUser(username, roleaccepted);
                    }
                }

                if (isAuthorize == true)
                {
                  var result =  from s in dbEntity.Std_Reg_Category
                    join ca in dbEntity.Categories on s.Category_ID equals ca.ID
                    where s.Std_ID == std_ID
                    select ca;
                    //var result = dbEntity.Std_Reg_Category.Where(e=>e.Std_ID== std_ID).ToList();
                    //foreach (var item in result)
                    //{
                    //    var categoryname = item.Category.NameAr;
                    //}
                    

                    if (result != null)
                    {

                        return View(result);
                    }
                }
            }
            catch (Exception ex)
            {
                Printerror("ActiveStudenCourse", ex.Message, ex.InnerException.ToString(), ex.StackTrace);
            }
            return RedirectToAction("ActiveCourse");
        }

        #endregion

        #region QuestionBank
        [HttpGet]
        public ActionResult QuestionBank()
        {
            bool isAuthorize = false;
            var username = _UsermodelModel;
            if (username != null)
            {
                Session["username"] = username.RealName;
                if (username != null)
                {
                    Session["username"] = username.RealName;
                    var role = username.L_role;
                    List<string> roleaccepted = new List<string>();
                    roleaccepted.Add("Admin");
                    roleaccepted.Add("QuestionBankDelete");
                    roleaccepted.Add("QuestionBankEdit");
                    roleaccepted.Add("QuestionBankView");
                    isAuthorize = IsTrustedUser(username, roleaccepted);
                }
            }
            if (isAuthorize)
            {
                return View();
            }
            return RedirectToAction("LogIn", "Home");

        }
        [HttpPost]
        public JsonResult GetAllQuesinBank(int type = 0, int category_ID = 0)
        {
            List<QuestionsModel> Lqm = new List<QuestionsModel>();
            try
            {
                var value = dbEntity.Questions.Where(i => (type == 0 && category_ID == 0) || (category_ID == 0 && i.Type == type) || (type == 0 && i.Category_ID == category_ID)).ToList();
                if (value != null)
                {
                    foreach (var item in value)
                    {
                        QuestionsModel qm = new QuestionsModel();
                        qm.ID = item.ID;
                        qm.Q_Name = item.Q_Name;
                        qm.Q_Text = item.Q_Text;
                        qm.Q_Description = item.Q_Description;
                        qm.Type = item.Type;
                        if (item.Category_ID != null)
                        {
                            qm.Category_ID = item.Category_ID.Value;
                            qm.Category_Name = item.Category.NameAr;
                        }
                        Lqm.Add(qm);

                    }
                }
                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(Lqm, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, responseText = ex.Message }, JsonRequestBehavior.AllowGet);
            }

        }
        [HttpPost]
        public JsonResult GetQuestionListByID(List<int> L_Q)
        {
            List<QuestionsModel> LQ_M = new List<QuestionsModel>();
            try
            {
                if (L_Q != null && L_Q.Count > 0)
                {
                    foreach (var item in L_Q)
                    {
                        var ques = dbEntity.Questions.Find(item);
                        if (ques != null)
                        {
                            QuestionsModel qm = new QuestionsModel();
                            qm.ID = ques.ID;
                            qm.Q_Name = ques.Q_Name;
                            qm.Type = ques.Type;
                            if (ques.Type == 1)
                            {
                                qm.Q_Degry = (int)ques.Q_TrueOrFolse.FirstOrDefault().Degry;
                            }
                            else if (qm.Type == 2)
                            {
                                foreach (var mc in ques.Q_MultiChoice)
                                {
                                    if (mc.Degry > 0)
                                    {
                                        qm.Q_Degry += (int)mc.Degry;
                                    }

                                }

                            }
                            else if (qm.Type == 3)
                            {
                                foreach (var mc in ques.Q_Matching)
                                {
                                    if (mc.Degry > 0)
                                    {
                                        qm.Q_Degry += (int)mc.Degry;
                                    }

                                }
                            }
                            LQ_M.Add(qm);
                        }
                    }
                }
                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(LQ_M, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, responseText = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpGet]
        public ActionResult CreateTruOrFalseQuestion()
        {
            bool isAuthorize = false;
            var username = _UsermodelModel;
            if (username != null)
            {
                Session["username"] = username.RealName;
                if (username != null)
                {
                    Session["username"] = username.RealName;
                    List<string> roleaccepted = new List<string>();
                    roleaccepted.Add("Admin");
                    roleaccepted.Add("QuestionBankDelete");
                    roleaccepted.Add("QuestionBankEdit");
                    roleaccepted.Add("QuestionBankView");
                    isAuthorize = IsTrustedUser(username, roleaccepted);
                }
            }
            if (isAuthorize)
            {
                return View();
            }
            return RedirectToAction("LogIn", "Home");

        }

        [HttpPost]
        public JsonResult CreateTruOrFalseQuestion(QuestionsModel QM)
        {
            using (var context = new tbseedDBEntities())
            {
                using (DbContextTransaction dbTran = context.Database.BeginTransaction())
                {
                    try
                    {
                        Question question = new Question()
                        {
                            //Q_Description = QM.Q_Description,
                            Q_Name = QM.Q_Name,
                            Q_Text = QM.Q_Text,
                            Category_ID = QM.Category_ID,
                            Type = 1,
                        };
                        context.Questions.Add(question);
                        context.SaveChanges();

                        Q_TrueOrFolse qt = new Q_TrueOrFolse()
                        {
                            Degry = QM.Q_TrueOrFalse.Degry,
                            Question_ID = question.ID,
                            TrueValue = QM.Q_TrueOrFalse.TrueValue
                        };
                        context.Q_TrueOrFolse.Add(qt);
                        context.SaveChanges();
                        dbTran.Commit();
                        Response.StatusCode = (int)HttpStatusCode.OK;
                        return Json(new { success = true, response = "" }, JsonRequestBehavior.AllowGet);
                    }
                    catch (Exception ex)
                    {
                        dbTran.Rollback();
                        Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        return Json(new { success = false, response = ex.Message }, JsonRequestBehavior.AllowGet);
                    }
                }
            }
        }
        [HttpPost]
        public JsonResult EditTruOrFalseQuestion(QuestionsModel QM)
        {
            using (var context = new tbseedDBEntities())
            {
                using (DbContextTransaction dbTran = context.Database.BeginTransaction())
                {
                    try
                    {
                        var trueorfalse = context.Q_TrueOrFolse.Where(e => e.Question_ID == QM.ID).FirstOrDefault();
                        if (trueorfalse != null)
                        {
                            trueorfalse.Degry = QM.Q_TrueOrFalse.Degry;
                            trueorfalse.TrueValue = QM.Q_TrueOrFalse.TrueValue;
                            context.Q_TrueOrFolse.Attach(trueorfalse);
                            context.Entry(trueorfalse).State = System.Data.Entity.EntityState.Modified;
                            context.SaveChanges();
                        }
                        var question = context.Questions.Find(QM.ID);
                        if (question != null)
                        {
                            question.Q_Name = QM.Q_Name;
                            question.Q_Text = QM.Q_Text;
                            question.Category_ID = QM.Category_ID;
                            context.Questions.Attach(question);
                            context.Entry(question).State = System.Data.Entity.EntityState.Modified;
                            context.SaveChanges();
                        }


                        dbTran.Commit();
                        Response.StatusCode = (int)HttpStatusCode.OK;
                        return Json(new { success = true, response = "" }, JsonRequestBehavior.AllowGet);
                    }
                    catch (Exception ex)
                    {
                        dbTran.Rollback();
                        Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        return Json(new { success = false, response = "" }, JsonRequestBehavior.AllowGet);
                    }
                }
            }


        }
        [HttpGet]
        public ActionResult CreateMultiChoiseQusetion()
        {
            bool isAuthorize = false;
            var username = _UsermodelModel;
            if (username != null)
            {
                Session["username"] = username.RealName;
                if (username != null)
                {
                    Session["username"] = username.RealName;
                    var role = username.L_role;
                    List<string> roleaccepted = new List<string>();
                    roleaccepted.Add("Admin");
                    roleaccepted.Add("QuestionBankDelete");
                    roleaccepted.Add("QuestionBankEdit");
                    roleaccepted.Add("QuestionBankView");
                    isAuthorize = IsTrustedUser(username, roleaccepted);
                }
            }
            if (isAuthorize)
            {
                return View();
            }
            return RedirectToAction("LogIn", "Home");
        }
        [HttpPost]
        public JsonResult CreateMultiChoiseQusetion(QuestionsModel QM)
        {
            using (var context = new tbseedDBEntities())
            {
                using (DbContextTransaction dbTran = context.Database.BeginTransaction())
                {
                    try
                    {
                        Question question = new Question()
                        {
                            Q_Description = QM.Q_Description,
                            Q_Name = QM.Q_Name,
                            Q_Text = QM.Q_Text,
                            Category_ID = QM.Category_ID,
                            Type = 2,
                        };
                        context.Questions.Add(question);
                        context.SaveChanges();

                        var id = question.ID;
                        List<Q_MultiChoice> LMC = new List<Q_MultiChoice>();
                        Q_MultiChoice qM;
                        foreach (var item in QM.Q_MultiChoice)
                        {
                            qM = new Q_MultiChoice()
                            {
                                Degry = item.Degry,
                                Question_ID = id,
                                Choose = item.Choose,
                            };
                            LMC.Add(qM);
                        }

                        context.Q_MultiChoice.AddRange(LMC);
                        context.SaveChanges();
                        dbTran.Commit();
                        Response.StatusCode = (int)HttpStatusCode.OK;
                        return Json(new { success = true, response = "" }, JsonRequestBehavior.AllowGet);
                    }
                    catch (Exception ex)
                    {
                        dbTran.Rollback();

                        Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        return Json(new { success = false, response = ex.Message }, JsonRequestBehavior.AllowGet);
                    }
                }
            }

        }
        [HttpPost]
        public JsonResult EditMultiChoiseQusetion(QuestionsModel QM)
        {
            using (var context = new tbseedDBEntities())
            {
                using (DbContextTransaction dbTran = context.Database.BeginTransaction())
                {

                    try
                    {
                        var mutliOption = context.Q_MultiChoice.Where(e => e.Question_ID == QM.ID).ToList();
                        foreach (var item in mutliOption)
                        {
                            context.Q_MultiChoice.Remove(item);
                            context.SaveChanges();
                        }
                        var question = context.Questions.Find(QM.ID);
                        if (question != null)
                        {
                            question.Q_Name = QM.Q_Name;
                            question.Q_Text = QM.Q_Text;
                            question.Category_ID = QM.Category_ID;
                            context.Questions.Attach(question);
                            context.Entry(question).State = System.Data.Entity.EntityState.Modified;
                            context.SaveChanges();
                        }

                        foreach (var item in QM.Q_MultiChoice)
                        {
                            Q_MultiChoice qm = new Q_MultiChoice();
                            qm.Question_ID = QM.ID;
                            qm.Choose = item.Choose;
                            qm.Degry = item.Degry;
                            context.Q_MultiChoice.Add(qm);
                            context.SaveChanges();
                        }
                        dbTran.Commit();
                        Response.StatusCode = (int)HttpStatusCode.OK;
                        return Json(new { success = true, response = "" }, JsonRequestBehavior.AllowGet);
                    }
                    catch (Exception ex)
                    {
                        dbTran.Rollback();
                        Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        return Json(new { success = false, response = "" }, JsonRequestBehavior.AllowGet);
                    }
                }
            }

        }
        [HttpPost]
        public JsonResult GetQuestionById(int q_id)
        {
            QuestionsModel qm = new QuestionsModel();
            try
            {
                var q = dbEntity.Questions.Find(q_id);
                if (q != null)
                {
                    qm.ID = q.ID;
                    //qm.Q_Description = q.Q_Description;
                    qm.Q_Name = q.Q_Name;
                    qm.Q_Text = q.Q_Text;
                    qm.Type = q.Type;
                    if (q.Category_ID != null)
                    {
                        qm.Category_ID = q.Category_ID.Value;
                    }

                    if (q.Type == 1)
                    {
                        qm.Q_TrueOrFalse = new Q_TrueOrFolseModel();
                        foreach (var item in q.Q_TrueOrFolse)
                        {
                            qm.Q_TrueOrFalse.Degry = item.Degry;
                            qm.Q_TrueOrFalse.ID = item.ID;
                            qm.Q_TrueOrFalse.Question_ID = item.Question_ID;
                            qm.Q_TrueOrFalse.TrueValue = item.TrueValue;
                        }
                    }
                    else
                        if (q.Type == 2)
                    {
                        qm.Q_MultiChoice = new List<Q_MultiChoiceModel>();
                        foreach (var item in q.Q_MultiChoice)
                        {
                            qm.Q_MultiChoice.Add(new Q_MultiChoiceModel
                            {
                                ID = item.ID,
                                Choose = item.Choose,
                                Degry = item.Degry,
                                Question_ID = item.Question_ID
                            });
                        }
                    }
                    else if (q.Type == 3)
                    {
                        qm.Q_MatchingChoise = new List<Q_MatchingModel>();
                        foreach (var item in q.Q_Matching)
                        {
                            qm.Q_MatchingChoise.Add(new Q_MatchingModel
                            {
                                ID = item.ID,
                                ChoiceText = item.ChoiceText,
                                ChoiseTrueAnswer = item.ChoiseTrueAnswer,
                                Degry = item.Degry,
                                Question_ID = item.Question_ID
                            });
                        }
                    }
                }
                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(qm, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpGet]
        public ActionResult GoToEditSutableQuestion(int ID)
        {
            bool isAuthorize = false;
            var username = _UsermodelModel;
            if (username != null)
            {
                Session["username"] = username.RealName;
                if (username != null)
                {
                    Session["username"] = username.RealName;
                    var role = username.L_role;
                    List<string> roleaccepted = new List<string>();
                    roleaccepted.Add("Admin");
                    roleaccepted.Add("QuestionBankDelete");
                    roleaccepted.Add("QuestionBankEdit");
                    roleaccepted.Add("QuestionBankView");
                    isAuthorize = IsTrustedUser(username, roleaccepted);
                }
            }
            if (isAuthorize)
            {
                var que = dbEntity.Questions.Find(ID);
                if (que != null)
                {
                    if (que.Type == 1)
                    {
                        return RedirectToAction("EditTrueOrFalse", new { id = que.ID });
                    }
                    else if (que.Type == 2)
                    {
                        return RedirectToAction("EditMultyChoise", new { id = que.ID });
                    }
                    else if (que.Type == 3)
                    {
                        return RedirectToAction("EditeMatching", new { id = que.ID });
                    }
                }
                return View();
            }
            return RedirectToAction("LogIn", "Home");
        }
        [HttpGet]
        public ActionResult EditTrueOrFalse(int id)
        {
            bool isAuthorize = false;
            var username = _UsermodelModel;
            if (username != null)
            {
                Session["username"] = username.RealName;
                if (username != null)
                {
                    Session["username"] = username.RealName;
                    List<string> roleaccepted = new List<string>();
                    roleaccepted.Add("Admin");
                    roleaccepted.Add("QuestionBankDelete");
                    roleaccepted.Add("QuestionBankEdit");
                    roleaccepted.Add("QuestionBankView");
                    isAuthorize = IsTrustedUser(username, roleaccepted);
                }
            }
            if (isAuthorize)
            {
                ViewBag.question_ID = id;
                return View();
            }
            return RedirectToAction("LogIn", "Home");
        }
        [HttpGet]
        public ActionResult EditMultyChoise(int id)
        {
            bool isAuthorize = false;
            var username = _UsermodelModel;
            if (username != null)
            {
                Session["username"] = username.RealName;
                if (username != null)
                {
                    Session["username"] = username.RealName;
                    List<string> roleaccepted = new List<string>();
                    roleaccepted.Add("Admin");
                    roleaccepted.Add("QuestionBankDelete");
                    roleaccepted.Add("QuestionBankEdit");
                    roleaccepted.Add("QuestionBankView");
                    isAuthorize = IsTrustedUser(username, roleaccepted);
                }
            }
            if (isAuthorize)
            {
                ViewBag.question_ID = id;
                return View();
            }
            return RedirectToAction("LogIn", "Home");
        }
        [HttpPost]
        public JsonResult DeletQuestion(int Q_ID)
        {
            using (var context = new tbseedDBEntities())
            {
                using (DbContextTransaction dbTran = context.Database.BeginTransaction())
                {
                    try
                    {


                        var q = context.Questions.Find(Q_ID);
                        if (q.Type == 1)
                        {
                            var value = context.Q_TrueOrFolse.Where(e => e.Question_ID == q.ID).FirstOrDefault();

                            context.Q_TrueOrFolse.Remove(value);

                            context.SaveChanges();
                        }
                        else if (q.Type == 2)
                        {
                            var value = context.Q_MultiChoice.Where(e => e.Question_ID == q.ID);
                            foreach (var item in value)
                            {
                                context.Q_MultiChoice.Remove(item);
                                context.SaveChanges();
                            }
                        }
                        else if (q.Type == 3)
                        {
                            var value = context.Q_Matching.Where(e => e.Question_ID == q.ID);
                            foreach (var item in value)
                            {
                                context.Q_Matching.Remove(item);
                                context.SaveChanges();
                            }
                        }
                        context.Questions.Remove(q);
                        context.SaveChanges();
                        dbTran.Commit();
                        Response.StatusCode = (int)HttpStatusCode.OK;
                        return Json(new { success = true, response = "" }, JsonRequestBehavior.AllowGet);
                    }
                    catch (Exception ex)
                    {
                        dbTran.Rollback();
                        Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        return Json(new { success = false, response = ex.Message }, JsonRequestBehavior.AllowGet);
                    }
                }
            }
        }

        [HttpGet]
        public ActionResult CreateMatchingChoiseQuestion()
        {
            bool isAuthorize = false;
            var username = _UsermodelModel;
            if (username != null)
            {
                Session["username"] = username.RealName;
                if (username != null)
                {
                    Session["username"] = username.RealName;
                    List<string> roleaccepted = new List<string>();
                    roleaccepted.Add("Admin");
                    roleaccepted.Add("QuestionBankDelete");
                    roleaccepted.Add("QuestionBankEdit");
                    roleaccepted.Add("QuestionBankView");
                    isAuthorize = IsTrustedUser(username, roleaccepted);
                }
            }
            if (isAuthorize)
            {

                return View();
            }
            return RedirectToAction("LogIn", "Home");

        }
        [HttpPost]
        public JsonResult CreateMatchingChoiseQuestion(QuestionsModel QM)
        {
            using (var context = new tbseedDBEntities())
            {
                using (DbContextTransaction dbTran = context.Database.BeginTransaction())
                {
                    try
                    {
                        Question question = new Question()
                        {
                            Q_Description = QM.Q_Description,
                            Q_Name = QM.Q_Name,
                            Q_Text = QM.Q_Text,
                            Category_ID = QM.Category_ID,
                            Type = 3,
                        };
                        context.Questions.Add(question);
                        context.SaveChanges();

                        var id = question.ID;
                        List<Q_Matching> LMC = new List<Q_Matching>();
                        Q_Matching qM;
                        foreach (var item in QM.Q_MatchingChoise)
                        {
                            qM = new Q_Matching()
                            {
                                Degry = item.Degry,
                                Question_ID = id,
                                ChoiceText = item.ChoiceText,
                                ChoiseTrueAnswer = item.ChoiseTrueAnswer
                            };
                            LMC.Add(qM);
                        }

                        context.Q_Matching.AddRange(LMC);
                        context.SaveChanges();
                        dbTran.Commit();
                        Response.StatusCode = (int)HttpStatusCode.OK;
                        return Json(new { success = true, response = "" }, JsonRequestBehavior.AllowGet);
                    }
                    catch (Exception ex)
                    {
                        dbTran.Rollback();

                        Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        return Json(new { success = false, response = ex.Message }, JsonRequestBehavior.AllowGet);
                    }
                }
            }
        }
        [HttpGet]
        public ActionResult EditeMatching(int id)
        {
            bool isAuthorize = false;
            var username = _UsermodelModel;
            if (username != null)
            {
                Session["username"] = username.RealName;
                if (username != null)
                {
                    Session["username"] = username.RealName;
                    List<string> roleaccepted = new List<string>();
                    roleaccepted.Add("Admin");
                    roleaccepted.Add("QuestionBankDelete");
                    roleaccepted.Add("QuestionBankEdit");
                    roleaccepted.Add("QuestionBankView");
                    isAuthorize = IsTrustedUser(username, roleaccepted);
                }
            }
            if (isAuthorize)
            {
                ViewBag.question_ID = id;
                return View();
            }
            return RedirectToAction("LogIn", "Home");

        }
        [HttpPost]
        public JsonResult EditMathcingQusetion(QuestionsModel QM)
        {
            using (var context = new tbseedDBEntities())
            {
                using (DbContextTransaction dbTran = context.Database.BeginTransaction())
                {

                    try
                    {
                        var mutliOption = context.Q_Matching.Where(e => e.Question_ID == QM.ID).ToList();
                        foreach (var item in mutliOption)
                        {
                            context.Q_Matching.Remove(item);
                            context.SaveChanges();
                        }
                        var question = context.Questions.Find(QM.ID);
                        if (question != null)
                        {
                            question.Q_Name = QM.Q_Name;
                            question.Q_Text = QM.Q_Text;
                            question.Category_ID = QM.Category_ID;
                            context.Questions.Attach(question);
                            context.Entry(question).State = System.Data.Entity.EntityState.Modified;
                            context.SaveChanges();
                        }

                        foreach (var item in QM.Q_MatchingChoise)
                        {
                            Q_Matching qm = new Q_Matching();
                            qm.Question_ID = QM.ID;
                            qm.ChoiceText = item.ChoiceText;
                            qm.Degry = item.Degry;
                            qm.ChoiseTrueAnswer = item.ChoiseTrueAnswer;
                            context.Q_Matching.Add(qm);
                            context.SaveChanges();
                        }
                        dbTran.Commit();
                        Response.StatusCode = (int)HttpStatusCode.OK;
                        return Json(new { success = true, response = "" }, JsonRequestBehavior.AllowGet);
                    }
                    catch (Exception ex)
                    {
                        dbTran.Rollback();
                        Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        return Json(new { success = false, response = "" }, JsonRequestBehavior.AllowGet);
                    }
                }
            }

        }
        #endregion

        #region Assignment

        [HttpGet]
        public ActionResult Assignment()
        {
            bool isAuthorize = false;
            var username = _UsermodelModel;
            if (username != null)
            {
                Session["username"] = username.RealName;
                if (username != null)
                {
                    Session["username"] = username.RealName;

                    List<string> roleaccepted = new List<string>();
                    roleaccepted.Add("Admin");
                    roleaccepted.Add("AssingmentDelete");
                    roleaccepted.Add("AssingmentEdit");
                    roleaccepted.Add("AssingmentView");
                    isAuthorize = IsTrustedUser(username, roleaccepted);
                }
            }
            if (isAuthorize)
            {

                return View();
            }
            return RedirectToAction("LogIn", "Home");

        }

        [HttpPost]
        public JsonResult GetAllAssinment()
        {
            List<AssignmentModel> L_am = new List<AssignmentModel>();
            try
            {
                var value = dbEntity.Assignments.ToList();
                if (value != null)
                {
                    foreach (var item in value)
                    {
                        AssignmentModel am = new AssignmentModel();
                        am.ID = item.ID;
                        am.Name = item.Name;
                        am.Description = item.Description;
                        if (item.Lecture_ID != null)
                        {
                            am.Lecture_ID = item.Lecture_ID.Value;
                        }
                        L_am.Add(am);
                    }
                }
                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(L_am, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult GetAssignmentByID(int Assingment_ID)
        {
            AssignmentModel am = new AssignmentModel();
            try
            {
                var value = dbEntity.Assignments.Select(i => new { i.ID, i.Name, i.Description, i.Lecture_ID, i.Assignment_Q, i.Assignmet_Degry, i.Assignmet_degry_Win }).Where(i => i.ID == Assingment_ID).FirstOrDefault();
                if (value != null)
                {
                    am.ID = value.ID;
                    am.Name = value.Name;
                    am.Description = value.Description;
                    am.Assignmet_Degry = (float)value.Assignmet_Degry.Value;
                    am.Assignmet_degry_Win = value.Assignmet_degry_Win.Value;
                    if (value.Lecture_ID != null)
                    {
                        am.Lecture_ID = value.Lecture_ID.Value;
                    }
                    am.L_Assignment_Q = new List<Assignment_Q_model>();
                    foreach (var item in value.Assignment_Q)
                    {
                        Assignment_Q_model aqm = new Assignment_Q_model();

                        aqm.Assignment_ID = value.ID;
                        aqm.ID = item.ID;
                        aqm.Q_ID = item.Q_ID;
                        aqm.QM = new QuestionsModel();
                        aqm.QM.ID = item.Q_ID;
                        aqm.QM.Q_Name = item.Question.Q_Name;
                        aqm.QM.Type = item.Question.Type;
                        if (item.Question.Type == 1)
                        {
                            aqm.QM.Q_Degry = (int)item.Question.Q_TrueOrFolse.FirstOrDefault().Degry;
                        }
                        else if (item.Question.Type == 2)
                        {
                            foreach (var item2 in item.Question.Q_MultiChoice)
                            {
                                if (item2.Degry > 0)
                                {
                                    aqm.QM.Q_Degry += (int)item2.Degry;
                                }

                            }

                        }
                        else
                        {
                            foreach (var item2 in item.Question.Q_Matching)
                            {
                                if (item2.Degry > 0)
                                {
                                    aqm.QM.Q_Degry += (int)item2.Degry;
                                }
                            }
                        }

                        am.L_Assignment_Q.Add(aqm);
                    }
                }
                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(am, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = ex.Message }, JsonRequestBehavior.AllowGet);
            }

        }

        [HttpPost]
        public JsonResult AssingQuestionToAssignment(AssignmentModel am)
        {
            using (var context = new tbseedDBEntities())
            {
                using (DbContextTransaction dbTran = context.Database.BeginTransaction())
                {

                    try
                    {
                        Assignment asm = new Assignment();
                        asm.Name = am.Name;
                        asm.Description = am.Description;
                        asm.Lecture_ID = am.Lecture_ID;
                        asm.Assignmet_Degry = am.Assignmet_Degry;
                        asm.Assignmet_degry_Win = am.Assignmet_degry_Win;
                        context.Assignments.Add(asm);
                        context.SaveChanges();
                        if (am.L_Assignment_Q != null)
                        {
                            foreach (var item in am.L_Assignment_Q)
                            {

                                Assignment_Q aq = new Assignment_Q();
                                aq.Assignment_ID = asm.ID;
                                aq.Q_ID = item.Q_ID;
                                context.Assignment_Q.Add(aq);
                                context.SaveChanges();
                            }
                        }

                        dbTran.Commit();
                        Response.StatusCode = (int)HttpStatusCode.OK;
                        return Json(new { success = true, response = "" }, JsonRequestBehavior.AllowGet);
                    }
                    catch (Exception ex)
                    {
                        dbTran.Rollback();
                        Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        return Json(new { success = false, response = ex.Message }, JsonRequestBehavior.AllowGet);
                    }
                }
            }

        }
        [HttpPost]
        public JsonResult UpdateAssingQuestionToAssignment(AssignmentModel am)
        {
            using (var context = new tbseedDBEntities())
            {
                using (DbContextTransaction dbTran = context.Database.BeginTransaction())
                {
                    try
                    {
                        Assignment asm = context.Assignments.Find(am.ID);
                        if (asm != null)
                        {
                            if (am.L_Assignment_Q != null)
                            {
                                if (asm.Assignment_Q != null && asm.Assignment_Q.Count > 0)
                                {
                                    foreach (var item in asm.Assignment_Q)
                                    {
                                        var q = dbEntity.Assignment_Q.Find(item.ID);
                                        dbEntity.Assignment_Q.Remove(q);
                                        dbEntity.SaveChanges();

                                    }
                                }

                                foreach (var item in am.L_Assignment_Q)
                                {
                                    Assignment_Q aq = new Assignment_Q();
                                    aq.Assignment_ID = asm.ID;
                                    aq.Q_ID = item.Q_ID;
                                    context.Assignment_Q.Add(aq);
                                    context.SaveChanges();
                                }
                            }

                            asm.Name = am.Name;
                            asm.Description = am.Description;
                            asm.Lecture_ID = am.Lecture_ID;
                            asm.Assignmet_degry_Win = am.Assignmet_degry_Win;
                            context.Assignments.Attach(asm);
                            context.Entry(asm).State = System.Data.Entity.EntityState.Modified;
                            context.SaveChanges();
                            dbTran.Commit();
                        }
                        Response.StatusCode = (int)HttpStatusCode.OK;
                        return Json(new { success = true, response = "" }, JsonRequestBehavior.AllowGet);
                    }
                    catch (Exception ex)
                    {
                        dbTran.Rollback();
                        Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        return Json(new { success = false, response = ex.Message }, JsonRequestBehavior.AllowGet);
                    }
                }
            }

        }

        [HttpPost]
        public JsonResult DeleteQuestionFromAssignment(int Q_ID, int A_ID)
        {
            try
            {
                var value = dbEntity.Assignment_Q.Where(i => i.Assignment_ID == A_ID && i.Q_ID == Q_ID).FirstOrDefault();
                if (value != null)
                {
                    dbEntity.Assignment_Q.Remove(value);
                    dbEntity.SaveChanges();
                }
                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(new { success = true, response = "" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public ActionResult AddAssignment(int Lecture_ID, string categoryName)
        {
            bool isAuthorize = false;
            var username = _UsermodelModel;
            if (username != null)
            {
                Session["username"] = username.RealName;
                if (username != null)
                {
                    Session["username"] = username.RealName;

                    List<string> roleaccepted = new List<string>();
                    roleaccepted.Add("Admin");
                    roleaccepted.Add("AssingmentEdit");
                    roleaccepted.Add("AssingmentView");
                    isAuthorize = IsTrustedUser(username, roleaccepted);
                }
            }
            if (isAuthorize)
            {
                var lectureinfo = dbEntity.Lectures.Select(i => new { i.ID, i.Lecture_Name, i.Category_ID }).Where(i => i.ID == Lecture_ID).FirstOrDefault();
                ViewBag.Lecture = lectureinfo.Lecture_Name;
                ViewBag.LectureID = Lecture_ID;
                ViewBag.categoryName = categoryName;
                ViewBag.categorieID = lectureinfo.Category_ID;
                return View(); ;
            }
            return RedirectToAction("LogIn", "Home");

        }

        [HttpGet]
        public ActionResult ViewAssingment(int lecture_ID, string categoryName)
        {
            bool isAuthorize = false;
            var username = _UsermodelModel;
            if (username != null)
            {
                Session["username"] = username.RealName;
                if (username != null)
                {
                    Session["username"] = username.RealName;

                    List<string> roleaccepted = new List<string>();
                    roleaccepted.Add("Admin");
                    roleaccepted.Add("AssingmentView");
                    isAuthorize = IsTrustedUser(username, roleaccepted);
                }
            }
            if (isAuthorize)
            {
                var lectureinfo = dbEntity.Lectures.Select(i => new { lectureID = i.ID, i.Lecture_Name, i.Category_ID, assingment_ID = i.Assignments.FirstOrDefault().ID }).Where(i => i.lectureID == lecture_ID).FirstOrDefault();
                ViewBag.Lecture_ID = lectureinfo.lectureID;
                ViewBag.Lecture = lectureinfo.Lecture_Name;
                ViewBag.categoryName = categoryName;
                ViewBag.categorieID = lectureinfo.Category_ID;
                ViewBag.AssignmentID = lectureinfo.assingment_ID;
                return View();
            }
            return RedirectToAction("LogIn", "Home");


        }

        #endregion

        #region Exams
        [HttpGet]
        public ActionResult Exams(string Category_ID, string categoryName)
        {
            bool isAuthorize = false;
            var username = _UsermodelModel;
            if (username != null)
            {
                Session["username"] = username.RealName;
                if (username != null)
                {
                    Session["username"] = username.RealName;
                    List<string> roleaccepted = new List<string>();
                    roleaccepted.Add("Admin");
                    roleaccepted.Add("ExamEdit");
                    roleaccepted.Add("ExamView");
                    roleaccepted.Add("ExamDelet");

                    isAuthorize = IsTrustedUser(username, roleaccepted);
                }
            }
            if (isAuthorize)
            {
                ViewBag.categoryName = categoryName;
                ViewBag.categorieID = Category_ID;
                return View();
            }
            return RedirectToAction("LogIn", "Home");

        }
        [HttpPost]
        public JsonResult getAllExams(int Duration = 0, int category = 0)
        {
            try
            {
                List<ExamModel> L_Exam = new List<ExamModel>();
                var exams = dbEntity.Exams.Where(i =>
                   (Duration == 0 && category == 0)
                || (Duration != 0 && category == 0 && i.Duration == Duration)
                || (Duration == 0 && category != 0 && i.Category_ID == category))
                    .Select(e => new { e.ID, e.Name, e.Start_Time, e.Category_ID, e.Day_Time, e.Duration }).ToList();
                if (exams != null)
                {
                    foreach (var item in exams)
                    {
                        ExamModel em = new ExamModel();
                        em.ID = item.ID;
                        em.Name = item.Name;
                        em.Start_Time = item.Start_Time;
                        em.Day_Time = item.Day_Time;
                        if (item.Category_ID != null)
                        {
                            em.Category_ID = item.Category_ID.Value;
                        }
                        if (item.Duration != null)
                        {
                            em.Duration = (float)item.Duration.Value;
                        }
                        L_Exam.Add(em);
                    }
                }
                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(L_Exam, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpGet]
        public ActionResult ViewExam(int Category_ID)
        {
            bool isAuthorize = false;
            var username = _UsermodelModel;
            if (username != null)
            {
                Session["username"] = username.RealName;
                if (username != null)
                {
                    Session["username"] = username.RealName;
                    List<string> roleaccepted = new List<string>();
                    roleaccepted.Add("Admin");
                    roleaccepted.Add("ExamEdit");
                    roleaccepted.Add("ExamView");
                    roleaccepted.Add("ExamDelet");
                    isAuthorize = IsTrustedUser(username, roleaccepted);
                }
            }
            if (isAuthorize)
            {
                var result = dbEntity.Exams.Where(e => e.Category_ID == Category_ID).Select(i => new { i.ID, i.Category.NameAr }).FirstOrDefault();
                ViewBag.Exam_ID = result.ID;
                ViewBag.CategoresName = result.NameAr;
                ViewBag.Categores_ID = Category_ID;
                return View();
            }
            return RedirectToAction("LogIn", "Home");

        }
        [HttpPost]
        public JsonResult GetExamByID(int Exam_ID)
        {
            ExamModel em = new ExamModel();
            try
            {
                var value = dbEntity.Exams.Find(Exam_ID);
                if (value != null)
                {
                    em.ID = value.ID;
                    em.Description = value.Description;
                    em.Name = value.Name;
                    em.Exam_Degry = (int)value.Exam_Degry;
                    em.Exam_win_Degry = (int)value.Exam_win_Degry;
                    if (value.Duration != null)
                    {
                        em.Duration = (float)value.Duration.Value;
                    }

                    if (value.Category_ID != null)
                    {
                        em.Category_ID = value.Category_ID.Value;
                    }
                    em.Exam_Q = new List<Exam_Q_Model>();
                    foreach (var item in value.Exam_Q)
                    {
                        Exam_Q_Model aqm = new Exam_Q_Model();
                        aqm.Exam_ID = value.ID;
                        aqm.ID = item.ID;
                        aqm.Q_ID = item.Q_ID;
                        aqm.QM = new QuestionsModel();
                        aqm.QM.ID = item.Q_ID;
                        aqm.QM.Q_Name = item.Question.Q_Name;
                        aqm.QM.Type = item.Question.Type;
                        if (item.Question.Type == 1)
                        {
                            aqm.QM.Q_Degry = (int)item.Question.Q_TrueOrFolse.FirstOrDefault().Degry;
                        }
                        else if (item.Question.Type == 2)
                        {
                            foreach (var item2 in item.Question.Q_MultiChoice)
                            {
                                if (item2.Degry > 0)
                                {
                                    aqm.QM.Q_Degry += (int)item2.Degry;
                                }

                            }
                        }
                        else
                        {
                            foreach (var item2 in item.Question.Q_Matching)
                            {
                                if (item2.Degry > 0)
                                {
                                    aqm.QM.Q_Degry += (int)item2.Degry;
                                }

                            }
                        }
                        em.Exam_Q.Add(aqm);
                    }
                }
                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(em, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Printerror("GetExamByID", ex.Message, null, ex.StackTrace);
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        public JsonResult CreateExams(ExamModel em)
        {
            using (var context = new tbseedDBEntities())
            {
                using (DbContextTransaction dbTran = context.Database.BeginTransaction())
                {

                    try
                    {
                        Exam asm = new Exam();
                        asm.Name = em.Name;
                        asm.Description = em.Description;
                        asm.Category_ID = em.Category_ID;
                        asm.Duration = em.Duration;
                        asm.Exam_Degry = em.Exam_Degry;
                        asm.Exam_win_Degry = em.Exam_win_Degry;
                        context.Exams.Add(asm);
                        context.SaveChanges();
                        if (em.Exam_Q != null)
                        {
                            foreach (var item in em.Exam_Q)
                            {
                                Exam_Q aq = new Exam_Q();
                                aq.Exam_ID = asm.ID;
                                aq.Q_ID = item.Q_ID;
                                context.Exam_Q.Add(aq);
                                context.SaveChanges();
                            }
                        }
                        dbTran.Commit();
                        Response.StatusCode = (int)HttpStatusCode.OK;
                        return Json(new { success = true, response = "" }, JsonRequestBehavior.AllowGet);
                    }
                    catch (Exception ex)
                    {
                        dbTran.Rollback();
                        Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        return Json(new { success = false, response = ex.Message }, JsonRequestBehavior.AllowGet);
                    }
                }
            }
        }
        [HttpPost]
        public JsonResult EditExams(ExamModel em)
        {
            using (var context = new tbseedDBEntities())
            {
                using (DbContextTransaction dbTran = context.Database.BeginTransaction())
                {
                    try
                    {
                        Exam asm = context.Exams.Find(em.ID);
                        if (asm != null)
                        {
                            if (em.Exam_Q != null)
                            {
                                if (asm.Exam_Q != null && asm.Exam_Q.Count > 0)
                                {
                                    foreach (var item in asm.Exam_Q)
                                    {
                                        var q = dbEntity.Exam_Q.Find(item.ID);
                                        dbEntity.Exam_Q.Remove(q);
                                        dbEntity.SaveChanges();

                                    }
                                }

                                foreach (var item in em.Exam_Q)
                                {
                                    Exam_Q aq = new Exam_Q();
                                    aq.Exam_ID = asm.ID;
                                    aq.Q_ID = item.Q_ID;
                                    context.Exam_Q.Add(aq);
                                    context.SaveChanges();
                                }
                            }

                            asm.Name = em.Name;
                            asm.Description = em.Description;
                            asm.Category_ID = em.Category_ID;
                            asm.Duration = em.Duration;
                            asm.Exam_Degry = em.Exam_Degry;
                            asm.Exam_win_Degry = em.Exam_win_Degry;
                            context.Exams.Attach(asm);
                            context.Entry(asm).State = System.Data.Entity.EntityState.Modified;
                            context.SaveChanges();
                            dbTran.Commit();
                        }
                        Response.StatusCode = (int)HttpStatusCode.OK;
                        return Json(new { success = true, response = "" }, JsonRequestBehavior.AllowGet);
                    }
                    catch (Exception ex)
                    {
                        dbTran.Rollback();
                        Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        return Json(new { success = false, response = ex.Message }, JsonRequestBehavior.AllowGet);
                    }
                }
            }

            //try
            //{
            //    var exam = dbEntity.Exams.Find(em.ID);
            //    if (exam!=null)
            //    {
            //        exam.Start_Time = em.Start_Time;
            //        exam.Name = em.Name;
            //        exam.Duration = em.Duration;
            //        exam.Description = em.Description;
            //        exam.Day_Time = em.Day_Time;
            //        exam.Category_ID = em.Category_ID;
            //        dbEntity.Exams.Attach(exam);
            //        dbEntity.Entry(exam).State = System.Data.Entity.EntityState.Modified;
            //        dbEntity.SaveChanges();
            //    }
            //    Response.StatusCode = (int)HttpStatusCode.OK;
            //    return Json(new { success = true, response = "" }, JsonRequestBehavior.AllowGet);
            //}
            //catch (Exception ex)
            //{
            //    Response.StatusCode = (int)HttpStatusCode.BadRequest;
            //    return Json(new { success = false, response = ex.Message }, JsonRequestBehavior.AllowGet);
            //}
        }
        #endregion


        #region Users
        [HttpGet]
        public ActionResult Users()
        {
            bool isAuthorize = false;
            var username = _UsermodelModel;
            if (username != null)
            {
                Session["username"] = username.RealName;
                if (username != null)
                {
                    Session["username"] = username.RealName;
                    List<string> roleaccepted = new List<string>();
                    roleaccepted.Add("Admin");
                    isAuthorize = IsTrustedUser(username, roleaccepted);
                }
            }
            if (isAuthorize)
            {
                return View(); ;
            }
            return RedirectToAction("LogIn", "Home");

        }
        [HttpPost]
        public async Task<JsonResult> GetUsers()
        {
            List<UserModel> L_User = new List<UserModel>();
            try
            {
                var value = userManager.Users.Where(e => e.userOrTetcherOrStudent == 0).Select(i => new { i.Id, i.RealName, i.Email, i.PhoneNumber, i.Password }).ToList();
                if (value != null)
                {
                    foreach (var item in value)
                    {
                        UserModel um = new UserModel();
                        um.ID = item.Id;
                        um.RealName = item.RealName;
                        um.Email = item.Email;
                        um.PhoneNumber = item.PhoneNumber;
                        um.Password = item.Password;
                        um.L_role = new List<RoleModel>();
                        var roles = await userManager.GetRolesAsync(item.Id);
                        foreach (var item2 in roles)
                        {
                            RoleModel rm = new RoleModel();
                            rm.RoleName = item2;
                            um.L_role.Add(rm);
                        }
                        L_User.Add(um);
                    }
                }


            }
            catch (Exception ex)
            {
            }


            Response.StatusCode = (int)HttpStatusCode.OK;
            return Json(L_User, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public async Task<JsonResult> GetUserByID(string UserID)
        {
            UserModel um = new UserModel();
            try
            {
                var user = await userManager.FindByIdAsync(UserID);
                if (user != null)
                {
                    um.ID = user.Id;
                    um.RealName = user.RealName;
                    um.Email = user.Email;
                    um.PhoneNumber = user.PhoneNumber;
                    um.Password = user.Password;
                    var result = await userManager.GetRolesAsync(user.Id);
                    if (result != null && result.Count > 0)
                    {
                        um.L_role = new List<RoleModel>();
                        foreach (var item in result)
                        {
                            um.L_role.Add(new RoleModel
                            {
                                RoleName = item
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
            Response.StatusCode = (int)HttpStatusCode.OK;
            return Json(um, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult CreateUser(UserModel tm)
        {
            try
            {

                string persionImage = "";
                if (tm.Image_File != null)
                {
                    persionImage = SaveImage(tm.Image_File);
                }

                //var l_role=SpliteString(tm.L_role[0].RoleName, ',');
                var l_role = tm.L_role.Select(i => i.RoleName).ToList();
                int result = 0;
                if (tm.ID == null)
                {
                    result = CreateUsers(tm.RealName, tm.RealName, tm.Email, tm.PhoneNumber, tm.Password, l_role, persionImage);

                }
                else
                {
                    result = UpdateUsers(tm.ID, tm.RealName, tm.RealName, tm.Email, tm.PhoneNumber, tm.Password, l_role, persionImage);
                }


                if (result == 1)
                {
                    Response.StatusCode = (int)HttpStatusCode.OK;
                    return Json(new { success = true, response = "" }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return Json(new { success = true, response = "Can't create User" }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = true, response = ex.Message }, JsonRequestBehavior.AllowGet);
            }

        }

        [HttpPost]
        public async Task<JsonResult> DeleteUsers(string ID)
        {
            var users = await userManager.FindByIdAsync(ID);
            if (users != null)
            {
                await userManager.DeleteAsync(users);
                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(new { success = true, response = "" }, JsonRequestBehavior.AllowGet);
            }
            Response.StatusCode = (int)HttpStatusCode.BadRequest;
            return Json(new { success = false, response = "" }, JsonRequestBehavior.AllowGet);

        }

        #endregion

        #region Tetchers
        [HttpGet]
        public ActionResult Teachers()
        {
            bool isAuthorize = false;
            var username = _UsermodelModel;
            if (username != null)
            {
                Session["username"] = username.RealName;
                if (username != null)
                {
                    Session["username"] = username.RealName;
                    List<string> roleaccepted = new List<string>();
                    roleaccepted.Add("Admin");
                    roleaccepted.Add("UsersTeachersDelete");
                    roleaccepted.Add("UsersTeachersEdit");
                    roleaccepted.Add("UsersTeachersView");
                    isAuthorize = IsTrustedUser(username, roleaccepted);
                }
            }
            if (isAuthorize)
            {
                return View(); ;
            }
            return RedirectToAction("LogIn", "Home");

        }
        [HttpPost]
        public async Task<JsonResult> SaveTeachers(TetcherModel tm)
        {
            try
            {
                if (tm.Categores_ID==null)
                {
                    Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return Json(new { response = "يجب اضافة مادة التدريس"   }, JsonRequestBehavior.AllowGet);
                }
                var value = await userManager.FindByEmailAsync(tm.Email);
                if (value != null)
                {
                    Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return Json(new { response = " يوجد حساب بنفس هذا الايميل" + value.FirstName }, JsonRequestBehavior.AllowGet);
                }
                string persionImage = "";
                if (tm.Image_File != null)
                {
                    persionImage = SaveImage(tm.Image_File);
                }

                int result = CreateTeatcher(tm);
                if (result == 1)
                {

                    // var valueResult =  CreateuserInZoom(tm.Email, tm.FullName, tm.FullName, tm.Password);

                    Response.StatusCode = (int)HttpStatusCode.OK;
                    return Json(new { success = true, response = "" }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return Json(new { success = true, response = "Can't create User" }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = true, response = ex.Message }, JsonRequestBehavior.AllowGet);
            }

        }

        [HttpPost]
        public JsonResult UpdateTeachers(TetcherModel tm)
        {
            try
            {

                string persionImage = "";
                if (tm.Image_File != null)
                {
                    persionImage = SaveImage(tm.Image_File);
                }

                int result = UdateTeatcherInfo(tm);
                if (result == 1)
                {
                    Response.StatusCode = (int)HttpStatusCode.OK;
                    return Json(new { success = true, response = "" }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return Json(new { success = true, response = "Can't create User" }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = true, response = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult GetTeacherByID(string Teacher_ID)
        {
            try
            {
                var value = userManager.Users.Where(e => e.Id == Teacher_ID).Select(i => new { i.Categories_ID, i.Id, i.FirstName, i.Email, i.PhoneNumber, i.Password }).FirstOrDefault();
                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(value, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        public JsonResult GetTeachers()
        {
            List<MyIdentityUser> L_cat = new List<MyIdentityUser>();
            var value = userManager.Users.Where(e => e.userOrTetcherOrStudent == 1).Select(i => new { i.Categories_ID, i.Id, i.FirstName, i.Email, i.PhoneNumber, i.Per_Photo }).ToList();
            try
            {
                foreach (var item in value)
                {
                    MyIdentityUser catm = new MyIdentityUser();
                    catm.Id = item.Id;
                    var cat = SpliteString(item.Categories_ID, ',');
                    foreach (var item2 in cat)
                    {
                        var result = SpliteString(item2, '/');
                        if (result.Count > 1)
                        {
                            catm.Categories_ID += "<br>" + result[1];
                        }

                    }

                    catm.FirstName = item.FirstName;
                    catm.Email = item.Email;
                    catm.PhoneNumber = item.PhoneNumber;
                    catm.Per_Photo = item.Per_Photo;
                    L_cat.Add(catm);
                }
            }
            catch (Exception ex)
            {
            }
            Response.StatusCode = (int)HttpStatusCode.OK;
            return Json(L_cat, JsonRequestBehavior.AllowGet);

        }
        #endregion

        #region Student
        [HttpGet]
        public ActionResult Student()
        {
            List<MyIdentityUser> myid = new List<MyIdentityUser>();
            try
            {
                bool isAuthorize = false;
                var username = _UsermodelModel;
                if (username != null)
                {
                    Session["username"] = username.RealName;
                    if (username != null)
                    {
                        Session["username"] = username.RealName;
                        List<string> roleaccepted = new List<string>();
                        roleaccepted.Add("Admin");
                        roleaccepted.Add("UsersStudentDelete");
                        roleaccepted.Add("UsersStudentEdit");
                        roleaccepted.Add("UsersStudentView");
                        isAuthorize = IsTrustedUser(username, roleaccepted);
                    }
                }
                if (isAuthorize)
                {
                    myid = userManager.Users.Where(e => e.userOrTetcherOrStudent == 2).ToList();
                    return View(myid);
                }
            }
            catch (Exception ex)
            {
            }
            return RedirectToAction("LogIn", "Home");
        }
        [HttpPost]
        public JsonResult GetAllStudent()
        {
            try
            {

            }
            catch (Exception ex)
            {
            }
            Response.StatusCode = (int)HttpStatusCode.OK;
            return Json(new { success = true, response = "" }, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public async Task<JsonResult> AddNewStd(UserModel student)
        {
            try
            {
                await CreateStudent(student.RealName, student.RealName, student.Email, student.PhoneNumber, student.Password, null);
                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(new { success = true, response = "" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        public JsonResult UpdateStd(UserModel student)
        {
            try
            {
                var value = UpdateStudent(student.ID, student.RealName, student.RealName, student.Email, student.PhoneNumber, student.Password);
                if (value != 1)
                {
                    Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return Json(new { success = false, response = "" }, JsonRequestBehavior.AllowGet);
                }
                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(new { success = true, response = "" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public async Task<JsonResult> GetStdByID(string ID)
        {
            MyIdentityUser identity = await userManager.FindByIdAsync(ID);
            try
            {

                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(identity, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        public async Task<JsonResult> UpdateStdActivate(string ID, int IsActive)
        {
            MyIdentityUser identity = await userManager.FindByIdAsync(ID);
            try
            {
                identity.IsActive = IsActive;
                await userManager.UpdateAsync(identity);
                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(new { success = true, response = "" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }


        #endregion

        #region sendemail
        public JsonResult sendEmail(string Email, string supject, string message)
        {
            try
            {

                SendEmailAsync(Email, supject, message);
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(new { success = false, response = ex.Message }, JsonRequestBehavior.AllowGet);

            }
            Response.StatusCode = (int)HttpStatusCode.OK;
            return Json(new { success = true, response = "" }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region certification
        [HttpGet]
        public ActionResult certification()
        {
            return View();
        }
        #endregion

        #region Chat
        [HttpGet]
        public async Task<ActionResult> ChatPage()
        {
            try
            {
                bool isAuthorize = false;
                var username = _UsermodelModel;
                if (username != null)
                {
                    Session["username"] = username.RealName;
                    if (username != null)
                    {
                        Session["username"] = username.RealName;
                        List<string> roleaccepted = new List<string>();
                        roleaccepted.Add("Admin");
                        roleaccepted.Add("Chat");
                        isAuthorize = IsTrustedUser(username, roleaccepted);
                    }
                }
                if (isAuthorize)
                {
                    var result = username;
                    if (result != null)
                    {
                        ViewBag.stdname = result.FirstName;
                        ViewBag.stdID = result.ID;
                    }
                    else
                    {
                        ViewBag.stdname = "admin";
                        ViewBag.stdID = "0";
                    }
                    List<CategoryModel> L_CM = new List<CategoryModel>();
                    var categores = dbEntity.Categories.Select(x => new { x.ID, x.NameAr }).ToList();
                    foreach (var item in categores)
                    {
                        CategoryModel cm = new CategoryModel();
                        cm.ID = item.ID;
                        cm.NameAr = item.NameAr;
                        L_CM.Add(cm);
                    }
                    return View(L_CM);
                }
            }
            catch (Exception ex)
            {
            }
            return RedirectToAction("LogIn", "Home");

        }




        [HttpPost]
        public async Task<JsonResult> GetCategoryChat(string categoryID)
        {
            List<ChatTableModel> L_chat = new List<ChatTableModel>();
            try
            {
                int category = int.Parse(categoryID);
                var values = dbEntity.ChatTables.Where(e => e.Category_ID == category).ToList();
                foreach (var item in values)
                {
                    ChatTableModel ctm = new ChatTableModel();
                    ctm.Category_ID = item.Category_ID.Value;
                    ctm.date_Time = item.date_Time;
                    ctm.Message = item.Message;
                    if (item.User_ID != null)
                    {
                        ctm.User_ID = item.User_ID;
                        //var name = await dbEntity.AspNetUsers.FindAsync(item.User_ID);
                        //ctm.username = name.FirstName;
                    }
                    L_chat.Add(ctm);
                }
            }
            catch (Exception ex)
            {

            }
            Response.StatusCode = (int)HttpStatusCode.OK;
            return Json(L_chat, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region analtic
        [HttpPost]
        public JsonResult userlocation()
        {
            List<userlocationModel> L_ul = new List<userlocationModel>();

            var result = dbEntity.UserLocations.ToList();
            foreach (var item in result)
            {
                userlocationModel lm = new userlocationModel();
                lm.count = item.Count.Value;
                lm.Country = item.CountryName;
                L_ul.Add(lm);
            }
            Response.StatusCode = (int)HttpStatusCode.OK;
            return Json(L_ul, JsonRequestBehavior.AllowGet);
        }


        #endregion

        #region ReadFromExecl
        public async Task<JsonResult> ReadQuesionFromExcelTrueOrFalse(HttpPostedFileBase file)
        {
            List<QuestionsModel> L_Q = new List<QuestionsModel>();
            try
            {
                string path = Server.MapPath("~/Uploade/StudentFile/");
                string filePath = string.Empty;
                string extension = string.Empty;
                DataTable dtSheet = new DataTable();
                if (file != null)
                {
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    filePath = path + Path.GetFileName(file.FileName);
                    extension = Path.GetExtension(file.FileName);
                    file.SaveAs(filePath);
                    string connectionString = string.Empty;
                    switch (extension)
                    {
                        case ".xls":
                            connectionString = ConfigurationManager.ConnectionStrings["Excel03ConString"].ConnectionString;
                            break;
                        case ".xlsx":
                            connectionString = ConfigurationManager.ConnectionStrings["Excel07ConString"].ConnectionString;
                            break;
                        default:
                            break;
                    }
                    connectionString = string.Format(connectionString, filePath);
                    using (OleDbConnection connExcel = new OleDbConnection(connectionString))
                    {
                        using (OleDbCommand cmdExcel = new OleDbCommand())
                        {
                            using (OleDbDataAdapter odaExcel = new OleDbDataAdapter())
                            {
                                cmdExcel.Connection = connExcel;
                                connExcel.Open();
                                DataTable dtExcelSchema;
                                dtExcelSchema = connExcel.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
                                string sheetName = dtExcelSchema.Rows[0]["TABLE_Name"].ToString();
                                connExcel.Close();


                                connExcel.Open();
                                cmdExcel.CommandText = "select * from [" + sheetName + "]";
                                odaExcel.SelectCommand = cmdExcel;
                                odaExcel.Fill(dtSheet);
                                for (int i = 0; i < dtSheet.Rows.Count; i++)
                                {

                                    QuestionsModel q = new QuestionsModel();
                                    q.Q_TrueOrFalse = new Q_TrueOrFolseModel();
                                    q.Q_Name = dtSheet.Rows[i][1].ToString();
                                    q.Q_Text = dtSheet.Rows[i][2].ToString();
                                    int Category;
                                    if (int.TryParse(dtSheet.Rows[i][0].ToString(), out Category))
                                    {
                                        q.Category_ID = Category;
                                    }
                                    else
                                    {
                                        Response.StatusCode = (int)HttpStatusCode.OK;
                                        return Json(new { success = false, response = "يوجد خطأ في السطر " + (i + 1) + "العامود الأول" }, JsonRequestBehavior.AllowGet);
                                    }
                                    int degry;
                                    if (int.TryParse(dtSheet.Rows[i][4].ToString(), out degry))
                                    {
                                        q.Q_TrueOrFalse.Degry = degry;
                                    }
                                    else
                                    {
                                        Response.StatusCode = (int)HttpStatusCode.OK;
                                        return Json(new { success = false, response = "يوجد خطأ في السطر " + (i + 1) + "العامود الخامس" }, JsonRequestBehavior.AllowGet);
                                    }


                                    if (dtSheet.Rows[i][3].ToString() == "صح")
                                    {
                                        q.Q_TrueOrFalse.TrueValue = 1;
                                    }
                                    else
                                        if (dtSheet.Rows[i][3].ToString() == "خطا")
                                    {
                                        q.Q_TrueOrFalse.TrueValue = 0;
                                    }
                                    else
                                    {
                                        Response.StatusCode = (int)HttpStatusCode.OK;
                                        return Json(new { success = false, response = "يوجد خطأ في السطر " + (i + 1) + "العامود الرابع" }, JsonRequestBehavior.AllowGet);
                                    }
                                    //q.Q_TrueOrFolse.Add(tf);
                                    L_Q.Add(q);
                                }
                                connExcel.Close();
                            }
                        }
                    }
                    using (var context = new tbseedDBEntities())
                    {
                        using (DbContextTransaction dbTran = context.Database.BeginTransaction())
                        {
                            foreach (var item in L_Q)
                            {
                                Question question = new Question()
                                {
                                    //Q_Description = QM.Q_Description,
                                    Q_Name = item.Q_Name,
                                    Q_Text = item.Q_Text,
                                    Category_ID = item.Category_ID,
                                    Type = 1,
                                };
                                context.Questions.Add(question);
                                context.SaveChanges();

                                Q_TrueOrFolse qt = new Q_TrueOrFolse()
                                {
                                    Degry = item.Q_TrueOrFalse.Degry,
                                    Question_ID = question.ID,
                                    TrueValue = item.Q_TrueOrFalse.TrueValue
                                };
                                context.Q_TrueOrFolse.Add(qt);
                                context.SaveChanges();
                            }
                            dbTran.Commit();
                            Response.StatusCode = (int)HttpStatusCode.OK;
                            return Json(new { success = true, response = "" }, JsonRequestBehavior.AllowGet);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                if (ex.InnerException.ToString().Contains("INSERT statement conflicted"))
                {
                    Response.StatusCode = (int)HttpStatusCode.OK;
                    return Json(new { success = true, response = "يوجد خطأ في رقم الفئة التابعة " }, JsonRequestBehavior.AllowGet);
                }

            }
            return null;
        }

        public async Task<JsonResult> ReadQuesionFromExcelMultiChoise(HttpPostedFileBase file)
        {
            List<QuestionsModel> L_Q = new List<QuestionsModel>();
            try
            {
                string path = Server.MapPath("~/Uploade/StudentFile/");
                string filePath = string.Empty;
                string extension = string.Empty;
                DataTable dtSheet = new DataTable();
                if (file != null)
                {
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    filePath = path + Path.GetFileName(file.FileName);
                    extension = Path.GetExtension(file.FileName);
                    file.SaveAs(filePath);
                    string connectionString = string.Empty;
                    switch (extension)
                    {
                        case ".xls":
                            connectionString = ConfigurationManager.ConnectionStrings["Excel03ConString"].ConnectionString;
                            break;
                        case ".xlsx":
                            connectionString = ConfigurationManager.ConnectionStrings["Excel07ConString"].ConnectionString;
                            break;
                        default:
                            break;
                    }
                    connectionString = string.Format(connectionString, filePath);
                    using (OleDbConnection connExcel = new OleDbConnection(connectionString))
                    {
                        using (OleDbCommand cmdExcel = new OleDbCommand())
                        {
                            using (OleDbDataAdapter odaExcel = new OleDbDataAdapter())
                            {
                                cmdExcel.Connection = connExcel;
                                connExcel.Open();
                                DataTable dtExcelSchema;
                                dtExcelSchema = connExcel.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
                                string sheetName = dtExcelSchema.Rows[0]["TABLE_Name"].ToString();
                                connExcel.Close();


                                connExcel.Open();
                                cmdExcel.CommandText = "select * from [" + sheetName + "]";
                                odaExcel.SelectCommand = cmdExcel;
                                odaExcel.Fill(dtSheet);
                                for (int i = 0; i < dtSheet.Rows.Count; i++)
                                {

                                    QuestionsModel q = new QuestionsModel();
                                    q.Q_MultiChoice = new List<Q_MultiChoiceModel>();
                                    int Category;
                                    if (int.TryParse(dtSheet.Rows[i][0].ToString(), out Category))
                                    {
                                        q.Category_ID = Category;
                                    }
                                    else
                                    {
                                        Response.StatusCode = (int)HttpStatusCode.OK;
                                        return Json(new { success = false, response = "يوجد خطأ في السطر " + (i + 1) + "العامود الأول" }, JsonRequestBehavior.AllowGet);
                                    }
                                    q.Q_Name = dtSheet.Rows[i][1].ToString();
                                    q.Q_Text = dtSheet.Rows[i][2].ToString();

                                    Q_MultiChoiceModel QMC = new Q_MultiChoiceModel();
                                    QMC.Choose = dtSheet.Rows[i][3].ToString();
                                    int degry;
                                    if (int.TryParse(dtSheet.Rows[i][4].ToString(), out degry))
                                    {
                                        QMC.Degry = degry;
                                    }
                                    else
                                    {
                                        Response.StatusCode = (int)HttpStatusCode.OK;
                                        return Json(new { success = false, response = "يوجد خطأ في السطر " + (i + 1) + "العامود الخامس" }, JsonRequestBehavior.AllowGet);
                                    }
                                    q.Q_MultiChoice.Add(QMC);
                                    QMC = null;
                                    QMC = new Q_MultiChoiceModel();
                                    QMC.Choose = dtSheet.Rows[i][5].ToString();
                                    if (int.TryParse(dtSheet.Rows[i][6].ToString(), out degry))
                                    {
                                        QMC.Degry = degry;
                                    }
                                    else
                                    {
                                        Response.StatusCode = (int)HttpStatusCode.OK;
                                        return Json(new { success = false, response = "يوجد خطأ في السطر " + (i + 1) + "العامود الخامس" }, JsonRequestBehavior.AllowGet);
                                    }
                                    q.Q_MultiChoice.Add(QMC);
                                    QMC = null;
                                    QMC = new Q_MultiChoiceModel();
                                    QMC.Choose = dtSheet.Rows[i][7].ToString();
                                    if (int.TryParse(dtSheet.Rows[i][8].ToString(), out degry))
                                    {
                                        QMC.Degry = degry;
                                    }
                                    else
                                    {
                                        Response.StatusCode = (int)HttpStatusCode.OK;
                                        return Json(new { success = false, response = "يوجد خطأ في السطر " + (i + 1) + "العامود الخامس" }, JsonRequestBehavior.AllowGet);
                                    }
                                    q.Q_MultiChoice.Add(QMC);
                                    QMC = null;
                                    QMC = new Q_MultiChoiceModel();
                                    QMC.Choose = dtSheet.Rows[i][9].ToString();
                                    if (int.TryParse(dtSheet.Rows[i][10].ToString(), out degry))
                                    {
                                        QMC.Degry = degry;
                                    }
                                    else
                                    {
                                        Response.StatusCode = (int)HttpStatusCode.OK;
                                        return Json(new { success = false, response = "يوجد خطأ في السطر " + (i + 1) + "العامود الخامس" }, JsonRequestBehavior.AllowGet);
                                    }
                                    q.Q_MultiChoice.Add(QMC);
                                    QMC = null;
                                    QMC = new Q_MultiChoiceModel();
                                    //q.Q_TrueOrFolse.Add(tf);
                                    L_Q.Add(q);
                                }
                                connExcel.Close();
                            }
                        }
                    }

                    using (var context = new tbseedDBEntities())
                    {
                        using (DbContextTransaction dbTran = context.Database.BeginTransaction())
                        {
                            List<Q_MultiChoice> LMC = new List<Q_MultiChoice>();

                            foreach (var item in L_Q)
                            {


                                Question question = new Question()
                                {
                                    Q_Description = item.Q_Description,
                                    Q_Name = item.Q_Name,
                                    Q_Text = item.Q_Text,
                                    Category_ID = item.Category_ID,
                                    Type = 2,
                                };
                                context.Questions.Add(question);
                                context.SaveChanges();

                                var id = question.ID;

                                Q_MultiChoice qM;
                                foreach (var item2 in item.Q_MultiChoice)
                                {
                                    qM = new Q_MultiChoice()
                                    {
                                        Degry = item2.Degry,
                                        Question_ID = id,
                                        Choose = item2.Choose,
                                    };
                                    LMC.Add(qM);
                                }
                            }
                            context.Q_MultiChoice.AddRange(LMC);
                            context.SaveChanges();
                            dbTran.Commit();
                            Response.StatusCode = (int)HttpStatusCode.OK;
                            return Json(new { success = true, response = "" }, JsonRequestBehavior.AllowGet);

                        }
                    }

                }
            }
            catch (Exception ex)
            {
                if (ex.InnerException.ToString().Contains("INSERT statement conflicted"))
                {
                    Response.StatusCode = (int)HttpStatusCode.OK;
                    return Json(new { success = true, response = "يوجد خطأ في رقم الفئة التابعة " }, JsonRequestBehavior.AllowGet);
                }

            }
            return null;
        }
        #endregion

        #region CodeGenerate
        [HttpGet]
        public ActionResult CodeGenerated()
        {
            List<CodeGeneratedModel> cgm = new List<CodeGeneratedModel>();
            try
            {
                bool isAuthorize = false;
                var username = _UsermodelModel;
                if (username != null)
                {
                    Session["username"] = username.RealName;
                    if (username != null)
                    {
                        Session["username"] = username.RealName;
                        List<string> roleaccepted = new List<string>();
                        roleaccepted.Add("Admin");
                        roleaccepted.Add("CodeGenerate");
                        isAuthorize = IsTrustedUser(username, roleaccepted);
                    }
                }
                if (isAuthorize)
                {
                    var value = dbEntity.CodeGenerateds.ToList();
                    foreach (var item in value)
                    {
                        CodeGeneratedModel cg = new CodeGeneratedModel();
                        cg.ID = item.ID;
                        cg.Code = item.Code;
                        cg.Count = item.Count.Value;
                        cg.TimeEnd = item.TimeEnd;
                        cg.UserName = item.User_ID;
                        cgm.Add(cg);
                    }
                    return View(cgm);
                }
            }
            catch (Exception ex)
            {
            }
            return RedirectToAction("LogIn", "Home");
        }
        [HttpPost]
        public JsonResult SaveCodeGenerated(CodeGeneratedModel cgm)
        {
            try
            {
                CodeGenerated cg = new CodeGenerated();
                cg.Code = cgm.Code;
                cg.Count = cgm.Count;
                cg.TimeEnd = cgm.TimeEnd;
                cg.User_ID = cgm.UserName;

                dbEntity.CodeGenerateds.Add(cg);
                dbEntity.SaveChanges();
            }
            catch (Exception ex)
            {
            }
            Response.StatusCode = (int)HttpStatusCode.OK;
            return Json(new { success = true, response = "" }, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public JsonResult UpdateGeneratedCode(CodeGeneratedModel cgm)
        {
            try
            {
                if (cgm.ID != 0)
                {
                    var cg = dbEntity.CodeGenerateds.Find(cgm.ID);
                    if (cg != null)
                    {
                        cg.Code = cgm.Code;
                        cg.Count = cgm.Count;
                        cg.TimeEnd = cgm.TimeEnd;
                        cg.User_ID = cgm.UserName;

                        dbEntity.CodeGenerateds.Attach(cg);
                        dbEntity.Entry(cg).State = EntityState.Modified;
                        dbEntity.SaveChanges();
                    }
                }

            }
            catch (Exception ex)
            {
            }
            Response.StatusCode = (int)HttpStatusCode.OK;
            return Json(new { success = true, response = "" }, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public JsonResult GetGeneratedCodeByID(int ID)
        {
            CodeGeneratedModel cg = new CodeGeneratedModel();
            try
            {
                if (ID != 0)
                {
                    var cgm = dbEntity.CodeGenerateds.Find(ID);
                    if (cg != null)
                    {
                        cg.ID = cgm.ID;
                        cg.Code = cgm.Code;
                        cg.Count = cgm.Count.Value;
                        cg.TimeEnd = cgm.TimeEnd;
                        cg.UserName = cgm.User_ID;
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
            Response.StatusCode = (int)HttpStatusCode.OK;
            return Json(cg, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public JsonResult GenerateCode()
        {
            string code = GenerateCodes(6);
            Response.StatusCode = (int)HttpStatusCode.OK;
            return Json(new { success = true, response = code }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        [HttpGet]
        public ActionResult LogOut()
        {
            Session["username"] = null;
            Session["userRole"] = null;
            SingOut();
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public JsonResult GetUserRoles()
        {
            try
            {

                if (CurrentUser != null)
                {
                    Session["username"] = CurrentUser.RealName;
                }
                var roles = CurrentUser.L_role;

                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(roles.Select(x => new
                {
                    ID = x.ID,
                    Name = x.RoleName
                }).ToList(), JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }


        #region UI_UX
        #region categories
        [HttpGet]
        public ActionResult categoriesUIUX()
        {
            List<CategoryModel> L_ctm = new List<CategoryModel>();
            try
            {
                var values = dbEntity.Categories
                           .Select(x => new { x.ID, x.NameAr, x.NameEn, x.Category1, x.CategoryType_ID, x.CategoryType.NameAR, x.IsPayed, x.PayedValue, x.ParentCateogry_ID, examName = x.Exams.FirstOrDefault().Name, x.New_Page_ID }).OrderByDescending(i => i.ID);
                foreach (var item in values)
                {
                    CategoryModel ctm = new CategoryModel();
                    ctm.ID = item.ID;
                    ctm.NameAr = item.NameAr;
                    ctm.NameEn = item.NameEn;
                    ctm.exam_Name = item.examName;
                    ctm.New_Page_ID = item.New_Page_ID.Value;
                    if (item.CategoryType_ID != null)
                    {
                        ctm.CategoryType_ID = item.CategoryType_ID.Value;
                        ctm.CategoryTypeName = item.NameAR;
                    }
                    if (item.IsPayed != null)
                    {
                        ctm.IsPayed = item.IsPayed.Value;
                        ctm.PayedValue = item.PayedValue;
                    }
                    if (item.ParentCateogry_ID != null)
                    {
                        ctm.ParentCateogry_ID = item.ParentCateogry_ID.Value;
                        ctm.ParentCategoryName = item.Category1.NameAr;
                    }

                    L_ctm.Add(ctm);
                }
            }
            catch (Exception ex)
            {

            }
            return View(L_ctm);
        }



        #endregion



        #region Language
        [HttpGet]
        public ActionResult Languages()
        {
            var value = dbEntity.New_lang.ToList();
            List<Lang_Model> L_MM = new List<Lang_Model>();
            foreach (var item in value)
            {
                var mm = new Lang_Model();
                mm.ID = item.ID;
                mm.DefultLang = item.DefultLang;
                mm.Name = item.Name;
                mm.Direction = item.Direction;
                if (item.Direction == 0)
                {
                    mm.DirectionName = "Left";
                }
                else
                {
                    mm.DirectionName = "Right";
                }

                L_MM.Add(mm);
            }
            return View(L_MM);

        }
        [HttpPost]
        public ActionResult Languages(string langName, int Deriction)
        {
            var lang = new New_lang();
            lang.DefultLang = 0;
            lang.Direction = Deriction;
            lang.Name = langName;
            dbEntity.New_lang.Add(lang);
            var result = dbEntity.SaveChanges();
            if (result > 0)
            {
                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(new { success = true, responseText = ViewBag.Message }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, responseText = ViewBag.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        public static int CurrentLangForOneUser { set; get; }
        [HttpPost]
        public JsonResult SetLangForUser(int LangID)
        {
            CurrentLangForOneUser = LangID;
            Response.StatusCode = (int)HttpStatusCode.OK;
            return Json(new { success = true, responseText = ViewBag.Message }, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public JsonResult ChangeDefultLang(int lang_ID)
        {
            using (var context = new tbseedDBEntities())
            {
                using (DbContextTransaction dbTran = context.Database.BeginTransaction())
                {
                    try
                    {
                        var L_lang = dbEntity.New_lang.ToList();
                        foreach (var item in L_lang)
                        {
                            item.DefultLang = 0;
                            dbEntity.New_lang.Attach(item);
                            dbEntity.Entry(item).State = System.Data.Entity.EntityState.Modified;
                            dbEntity.SaveChanges();
                        }
                        var value = dbEntity.New_lang.Find(lang_ID);
                        value.DefultLang = 1;
                        dbEntity.New_lang.Attach(value);
                        dbEntity.Entry(value).State = System.Data.Entity.EntityState.Modified;
                        var result = dbEntity.SaveChanges();
                        context.SaveChanges();

                        if (result > 0)
                        {
                            CurrentLangForOneUser = value.DefultLang.Value;
                            dbTran.Commit();
                            Response.StatusCode = (int)HttpStatusCode.OK;
                            return Json(new { success = true, responseText = ViewBag.Message }, JsonRequestBehavior.AllowGet);

                        }
                        else
                        {
                            dbTran.Rollback();
                            Response.StatusCode = (int)HttpStatusCode.BadRequest;
                            return Json(new { success = false, responseText = ViewBag.Message }, JsonRequestBehavior.AllowGet);
                        }

                    }
                    catch (Exception ex)
                    {
                        dbTran.Rollback();
                        Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        return Json(new { success = false, responseText = ViewBag.Message }, JsonRequestBehavior.AllowGet);
                    }
                }
            }

        }

        [HttpPost]
        public JsonResult GetAllLanguages()
        {
            var LangList = dbEntity.New_lang.ToList();
            //CurrentLangForOneUser = LangList.FirstOrDefault().ID;
            //subjectmodel.FollowSelectedList_ID = new SelectList(collegeList, "ID", "Name");
            return Json(LangList.Select(x => new
            {
                ID = x.ID,
                Name = x.Name
            }).ToList(), JsonRequestBehavior.AllowGet);

        }

        [HttpPost]
        public JsonResult DeleteLanguage(int Page_ID)
        {
            try
            {
                var result = dbEntity.New_lang.Find(Page_ID);
                if (result != null)
                {
                    dbEntity.New_lang.Remove(result);
                    dbEntity.SaveChanges();
                }
                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(new { success = true, response = "" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = ex.Message }, JsonRequestBehavior.AllowGet);
            }

        }
        #endregion

        #region Pages
        [HttpPost]
        public JsonResult GetAllPages()
        {
            var PageList = dbEntity.New_pages.ToList();


            return Json(PageList.Select(x => new
            {
                ID = x.ID,
                Name = x.name
            }).ToList(), JsonRequestBehavior.AllowGet);

        }
        [HttpGet]
        public ActionResult Pages()
        {
            var vlaue = dbEntity.New_pages.Where(e => e.IsUiUx == 1).ToList();

            return View(vlaue);
        }
        [HttpPost]
        public ActionResult Pages(string pagesName, int pageType)
        {
            New_pages pg = new New_pages();
            pg.name = pagesName;
            pg.Page_Type = pageType;
            pg.IsUiUx = 1;
            dbEntity.New_pages.Add(pg);
            var result = dbEntity.SaveChanges();
            if (result > 0)
            {
                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(new { success = true, responseText = ViewBag.Message }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, responseText = ViewBag.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpGet]
        public ActionResult GoPage(int PageID)
        {
            var page = dbEntity.New_pages.Find(PageID);
            if (page.Page_Type == 1)
            {
                return RedirectToAction("aboutPage", new { pageID = PageID });
            }
            if (page.Page_Type == 2)
            {
                return RedirectToAction("ContactUsPage", new { pageID = PageID });
            }
            if (page.Page_Type == 3)
            {
                ///Horizantal tab
                return RedirectToAction("HorizantalPageTabe", new { pageID = PageID, VH = 1 });
            }
            if (page.Page_Type == 4)
            {
                ///virtcal Tab
                return RedirectToAction("HorizantalPageTabe", new { pageID = PageID, VH = 2 });
            }
            if (page.Page_Type == 5)
            {
                return RedirectToAction("PageText", new { pageID = PageID });
            }
            if (page.Page_Type == 8)
            {
                return RedirectToAction("Facality");
            }
            if (page.Page_Type == 7)
            {
                return RedirectToAction("GridPage", new { pageID = PageID });
            }
            return null;
        }

        [HttpPost]
        public ActionResult DeletePage(int Page_ID)
        {
            try
            {
                var result = dbEntity.New_pages.Find(Page_ID);
                if (result != null)
                {
                    dbEntity.New_pages.Remove(result);
                    dbEntity.SaveChanges();
                }
                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(new { success = true, response = "" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion

        #region GridPage
        //public static int GridPageID;
        //public static int GridPageContentID;
        [HttpGet]
        public ActionResult GridPage(int pageID)
        {
            TempData["GridPageID"] = pageID;
            var Lam = dbEntity.NewGridPages.Where(e => e.Pages_ID == pageID).ToList();
            List<GridPageModel> am = new List<GridPageModel>();
            foreach (var item in Lam)
            {
                var result = new GridPageModel();
                result.ID = item.ID;
                result.Title = item.Title;
                //result.Lang_Name = item.New_lang.Name;
                result.Lang_ID = item.Lang_ID.Value;
                result.Image_Cover = item.Image_Cover;
                am.Add(result);
            }
            return View(am);
        }
        [HttpPost]
        public ActionResult GridPage(GridPageModel am)
        {
            NewGridPage na = new NewGridPage();
            if (am.ID == 0)
            {
                if (am.Image_CoverFile != null)
                {
                    am.Image_Cover = SaveImage(am.Image_CoverFile);
                }
                na.Title = am.Title;
                na.Pages_ID = (int)TempData["GridPageID"];
                na.Lang_ID = am.Lang_ID;
                na.Image_Cover = am.Image_Cover;
                dbEntity.NewGridPages.Add(na);
                dbEntity.SaveChanges();

            }
            else
            {
                var value = dbEntity.NewGridPages.Find(am.ID);
                if (am.Image_CoverFile != null)
                {
                    value.Image_Cover = SaveImage(am.Image_CoverFile);
                }
                value.Title = am.Title;

                dbEntity.NewGridPages.Attach(value);
                dbEntity.Entry(value).State = System.Data.Entity.EntityState.Modified;
                dbEntity.SaveChanges();

            }
            return RedirectToAction("GridPage", new { pageID = TempData["GridPageID"] });
        }
        [HttpPost]
        public JsonResult GetGridPageModelByID(int ID)
        {
            var result = dbEntity.NewGridPages.Find(ID);
            var value = new GridPageModel();
            if (result != null)
            {
                value.ID = result.ID;
                value.Title = result.Title;
            }
            Response.StatusCode = (int)HttpStatusCode.OK;
            return Json(value, JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public ActionResult AddContentGridPageModel(int PageID)
        {
            TempData["GridPage_ID"] = PageID;

            List<GridpageContentModel> ac = new List<GridpageContentModel>();
            var result = dbEntity.NewGridPage_Content.Where(e => e.GridPageID == PageID).ToList();
            foreach (var item in result)
            {
                var value = new GridpageContentModel();
                value.ID = item.ID;
                value.Description = item.Description;
                value.DocLinke = item.DocLinke;
                value.GridPageID = item.GridPageID.Value;
                value.Image = item.Image;
                value.Title = item.Title;
                ac.Add(value);
            }
            return View(ac);
        }

        [HttpPost]
        public ActionResult AddContentGridPageModel(GridpageContentModel tm)
        {
            try
            {

                NewGridPage_Content abco = new NewGridPage_Content();

                if (tm.ID == 0)
                {
                    abco.Description = tm.Description;
                    abco.DocLinke = tm.DocLinke;
                    abco.GridPageID = int.Parse(TempData["GridPage_ID"].ToString());
                    abco.Title = tm.Title;
                    if (tm.ImageFile != null)
                    {
                        abco.Image = SaveImage(tm.ImageFile);
                    }
                    dbEntity.NewGridPage_Content.Add(abco);
                    dbEntity.SaveChanges();
                }
                else
                {
                    var value = dbEntity.NewGridPage_Content.Find(tm.ID);
                    value.Description = tm.Description;
                    value.DocLinke = tm.DocLinke;
                    value.GridPageID = int.Parse(TempData["GridPage_ID"].ToString());
                    value.Title = tm.Title;
                    if (tm.ImageFile != null)
                    {
                        value.Image = SaveImage(tm.ImageFile);
                    }
                    dbEntity.NewGridPage_Content.Attach(value);
                    dbEntity.Entry(value).State = System.Data.Entity.EntityState.Modified;
                    dbEntity.SaveChanges();
                }

                return RedirectToAction("AddContentGridPageModel", new { PageID = TempData["GridPage_ID"] });

            }
            catch (Exception ex)
            {
                return RedirectToAction("AddContentGridPageModel", new { PageID = TempData["GridPage_ID"] });
            }

        }
        [HttpPost]
        public JsonResult GetContentForGridPageModelByID(int ID)
        {
            var result = dbEntity.NewGridPage_Content.Find(ID);
            var value = new GridpageContentModel();
            if (result != null)
            {
                value.ID = result.ID;
                value.Description = result.Description;
                value.DocLinke = result.DocLinke;
                value.GridPageID = result.GridPageID.Value;
                value.Image = result.Image;
                value.Title = result.Title;
            }
            Response.StatusCode = (int)HttpStatusCode.OK;
            return Json(value, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult DeleteGridPageModelPage(int id)
        {
            try
            {
                var value = dbEntity.NewGridPages.Find(id);
                if (value != null)
                {
                    dbEntity.NewGridPages.Remove(value);
                    dbEntity.SaveChanges();
                }

                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(new { success = true, responseText = "" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, responseText = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        public ActionResult DeleteGridPageModelContent(int ID)
        {
            try
            {
                var value = dbEntity.NewGridPage_Content.Find(ID);
                if (value != null)
                {
                    dbEntity.NewGridPage_Content.Remove(value);
                    dbEntity.SaveChanges();
                }

                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(new { success = true, responseText = "" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, responseText = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        #endregion


        #region HorizantalPage


        public static int PageTabeID { set; get; }
        public static int VHID { set; get; }
        public static int HorizantalPageTabeID { set; get; }
        [HttpGet]
        public ActionResult HorizantalPageTabe(int pageID, int VH)
        {
            PageTabeID = pageID;
            VHID = VH;
            List<Page_Tabe_Model> ptm = new List<Page_Tabe_Model>();
            var LPage = dbEntity.New_Page_Tabe.Where(e => e.New_Page_ID == pageID && e.page_H_V_tabe == VH).ToList();
            foreach (var item in LPage)
            {
                var value = new Page_Tabe_Model();
                value.ID = item.ID;
                value.Image_Cover = item.Image_Cover;
                value.Lang_ID = item.Lang_ID;
                //value.Lang_name = item.New_lang.Name;
                value.New_Page_ID = item.New_Page_ID;
                value.page_H_V_tabe = item.page_H_V_tabe;
                value.Title = item.Title;
                ptm.Add(value);
            }
            return View(ptm);
        }
        [HttpPost]
        public ActionResult HorizantalPageTabe(Page_Tabe_Model am)
        {
            New_Page_Tabe na = new New_Page_Tabe();
            try
            {


                if (am.ID == 0)
                {
                    if (am.CoverImage != null)
                    {
                        am.Image_Cover = SaveImage(am.CoverImage);
                    }
                    na.Title = am.Title;
                    na.New_Page_ID = PageTabeID;
                    na.Lang_ID = am.Lang_ID; 
                    na.Image_Cover = am.Image_Cover;
                    na.page_H_V_tabe = VHID;
                    dbEntity.New_Page_Tabe.Add(na);
                    dbEntity.SaveChanges();
                }
                else
                {
                    var value = dbEntity.New_Page_Tabe.Find(am.ID);
                    if (am.CoverImage != null)
                    {
                        value.Image_Cover = SaveImage(am.CoverImage);
                    }
                    value.Title = am.Title;

                    dbEntity.New_Page_Tabe.Attach(value);
                    dbEntity.Entry(value).State = System.Data.Entity.EntityState.Modified;
                    dbEntity.SaveChanges();

                }
            }
            catch (Exception ex)
            {

            }
            return RedirectToAction("HorizantalPageTabe", new { pageID = PageTabeID, VH = VHID });
        }
        [HttpPost]
        public ActionResult GetHorizantalPageTabe(int ID)
        {
            var result = dbEntity.New_Page_Tabe.Find(ID);
            var value = new New_Page_Tabe();
            if (result != null)
            {
                value.ID = result.ID;
                value.Title = result.Title;
            }
            Response.StatusCode = (int)HttpStatusCode.OK;
            return Json(value, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult DeleteHorizantalPageTabe(int id)
        {
            try
            {
                if (id != 0)
                {
                    var res = dbEntity.New_Page_Tabe.Find(id);
                    if (res != null)
                    {
                        dbEntity.New_Page_Tabe.Remove(res);
                        dbEntity.SaveChanges();
                    }

                }
                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(new { success = true, response = "" }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = "" }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public ActionResult AddTaHorizantalPageTabe(int PageID)
        {
            HorizantalPageTabeID = PageID;
            List<Page_Tabe_Content_Model> ac = new List<Page_Tabe_Content_Model>();
            try
            {
                var result = dbEntity.New_Page_tabe_Content.Where(e => e.Page_Tabe_ID == PageID).ToList();
                foreach (var item in result)
                {
                    var value = new Page_Tabe_Content_Model();
                    value.ID = item.ID;
                    value.Page_Tabe_ID = item.Page_Tabe_ID;
                    value.Tabe_title = item.Tabe_title;
                    value.Tab_Description = item.Tab_Description;
                    ac.Add(value);
                }
            }
            catch (Exception)
            {

            }

            return View(ac);
        }

        [HttpPost]
        public ActionResult AddTaHorizantalPageTabe(Page_Tabe_Content_Model tm)
        {
            try
            {
                if (tm.ID == 0)
                {
                    New_Page_tabe_Content abco = new New_Page_tabe_Content();
                    abco.Page_Tabe_ID = HorizantalPageTabeID;
                    abco.Tabe_title = tm.Tabe_title;
                    abco.Tab_Description = tm.Tab_Description;
                    dbEntity.New_Page_tabe_Content.Add(abco);
                    dbEntity.SaveChanges();
                }
                else
                {
                    var value = dbEntity.New_Page_tabe_Content.Find(tm.ID);
                    value.Tabe_title = tm.Tabe_title;
                    value.Tab_Description = tm.Tab_Description;
                    dbEntity.New_Page_tabe_Content.Attach(value);
                    dbEntity.Entry(value).State = System.Data.Entity.EntityState.Modified;
                    dbEntity.SaveChanges();
                }
                return RedirectToAction("AddTaHorizantalPageTabe", new { PageID = HorizantalPageTabeID });

            }
            catch (Exception)
            {
                return RedirectToAction("AddTaHorizantalPageTabe", new { PageID = HorizantalPageTabeID });
            }

        }

        [HttpPost]
        public ActionResult GettabHorizantalpageTabe(int ID)
        {
            var result = dbEntity.New_Page_tabe_Content.Find(ID);
            var value = new New_Page_tabe_Content();
            if (result != null)
            {
                value.ID = result.ID;
                value.Tabe_title = result.Tabe_title;
                value.Tab_Description = result.Tab_Description;
            }
            Response.StatusCode = (int)HttpStatusCode.OK;
            return Json(value, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult DeleteTabeHorizantalPageTabe(int id)
        {
            try
            {
                if (id != 0)
                {
                    var res = dbEntity.New_Page_tabe_Content.Find(id);
                    if (res != null)
                    {
                        dbEntity.New_Page_tabe_Content.Remove(res);
                        dbEntity.SaveChanges();
                    }
                }
                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(new { success = true, response = "" }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = "" }, JsonRequestBehavior.AllowGet);
            }
        }


        #endregion

        #region ContactUs Pages
        public static int? ContactUsPages { get; set; }
        [HttpGet]
        public ActionResult ContactUsPage(int? pageID)
        {
            ContactUsPages = pageID;
            List<ContactUsModel> am = new List<ContactUsModel>();
            return View(am);
        }
        [HttpPost]
        public ActionResult ContactUsPage(ContactUsModel am)
        {
            try
            {
                New_ContactUs nco = new New_ContactUs();
                if (am.ImageCover != null)
                {
                    nco.Image_Cover = SaveImage(am.ImageCover);
                }
                if (am.ID == 0)
                {

                    //nco.Image_Cover = am.Image_Cover;
                    nco.title = am.title;
                    nco.Lang_ID = am.Lang_ID;
                    nco.Map_Link = am.Map_Link;
                    nco.UN_Adress = am.UN_Adress;
                    nco.UN_Email = am.UN_Email;
                    nco.UN_PhoneNumber = am.UN_PhoneNumber;

                    nco.Email_word_trans = am.Email_word_trans;
                    nco.Message_word_trans = am.Message_word_trans;
                    nco.Name_word_trans = am.Name_word_trans;
                    nco.PhoneNumber_word_trans = am.PhoneNumber_word_trans;
                    nco.Send_word_trans = am.Send_word_trans;
                    nco.Adress_word_trans = am.Adress_word_trans;
                    if (am.New_Page_ID != 0)
                    {
                        nco.New_Page_ID = am.New_Page_ID;
                    }
                    else
                    {
                        nco.New_Page_ID = ContactUsPages.Value;
                    }
                    dbEntity.New_ContactUs.Add(nco);
                    dbEntity.SaveChanges();

                    ViewBag.Message = "Contact Us information was Added";
                    return View();
                }
                else
                {
                    var res = dbEntity.New_ContactUs.Where(e => e.ID == am.ID).FirstOrDefault();
                    if (am.ImageCover != null)
                    {
                        res.Image_Cover = SaveImage(am.ImageCover);
                    }
                    res.title = am.title;
                    res.Lang_ID = am.Lang_ID;
                    res.Map_Link = am.Map_Link;
                    res.UN_Adress = am.UN_Adress;
                    res.UN_Email = am.UN_Email;
                    res.UN_PhoneNumber = am.UN_PhoneNumber;
                    res.Email_word_trans = am.Email_word_trans;
                    res.Message_word_trans = am.Message_word_trans;
                    res.Name_word_trans = am.Name_word_trans;
                    res.PhoneNumber_word_trans = am.PhoneNumber_word_trans;
                    res.Send_word_trans = am.Send_word_trans;
                    res.Adress_word_trans = am.Adress_word_trans;
                    if (am.New_Page_ID != 0)
                    {
                        res.New_Page_ID = am.New_Page_ID;
                    }
                    else
                    {
                        res.New_Page_ID = ContactUsPages.Value;
                    }
                    dbEntity.New_ContactUs.Attach(res);
                    dbEntity.Entry(res).State = System.Data.Entity.EntityState.Modified;
                    dbEntity.SaveChanges();
                    ViewBag.Message = "Contact Us information was Added";
                    return View();
                }
            }
            catch (Exception ex)
            {
                ViewBag.error = ex.Message;


                return View();

            }
            //return RedirectToAction("ContactUsPage", new { pageID = aboutPageID });
        }

        [HttpPost]
        public JsonResult GetContactUsByLang(int LangID)
        {
            try
            {
                New_ContactUs homepage = dbEntity.New_ContactUs.Where(e => e.Lang_ID == LangID).FirstOrDefault();
                ContactUsModel ho = new ContactUsModel();
                if (homepage != null)
                {
                    ho.ID = homepage.ID;
                    ho.title = homepage.title;
                    ho.Image_Cover = homepage.Image_Cover;
                    ho.Lang_ID = homepage.Lang_ID;
                    ho.Map_Link = homepage.Map_Link;
                    ho.New_Page_ID = homepage.New_Page_ID;
                    ho.UN_Adress = homepage.UN_Adress;
                    ho.UN_Email = homepage.UN_Email;
                    ho.UN_PhoneNumber = homepage.UN_PhoneNumber;
                    ho.Email_word_trans = homepage.Email_word_trans;
                    ho.Message_word_trans = homepage.Message_word_trans;
                    ho.Name_word_trans = homepage.Name_word_trans;
                    ho.PhoneNumber_word_trans = homepage.PhoneNumber_word_trans;
                    ho.Send_word_trans = homepage.Send_word_trans;
                    ho.Adress_word_trans = homepage.Adress_word_trans;
                }
                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(ho, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = ex.Message }, JsonRequestBehavior.AllowGet);
            }

        }
        #endregion

        #region Page_Text
        public static int? Page_Tex { set; get; }
        [HttpGet]
        public ActionResult PageText(int? pageID)
        {

            Page_Tex = pageID;

            return View();
        }
        [HttpPost]
        public ActionResult PageText(PagesTextModel PT)
        {
            try
            {

                if (PT.ImageCover != null)
                {
                    PT.Image_Cover = SaveImage(PT.ImageCover);
                }
                if (PT.New_pages_ID == 0)
                {
                    New_pages_Text pt = new New_pages_Text();
                    pt.Lang_ID = PT.Lang_ID;
                    pt.New_pages_ID = Page_Tex.Value;
                    pt.Text_Description = PT.Text_Description;
                    pt.title = PT.title;
                    pt.Image_Cover = PT.Image_Cover;
                    dbEntity.New_pages_Text.Add(pt);
                    dbEntity.SaveChanges();
                    ViewBag.Message = "Information added";
                    return View();
                }
                else
                {
                    var result = dbEntity.New_pages_Text.Where(e =>  e.New_pages_ID == PT.New_pages_ID).FirstOrDefault();
                    result.Lang_ID = PT.Lang_ID;
                    result.New_pages_ID = Page_Tex.Value;
                    result.Text_Description = PT.Text_Description;
                    result.title = PT.title;
                    result.Image_Cover = PT.Image_Cover;
                    dbEntity.New_pages_Text.Attach(result);
                    dbEntity.Entry(result).State = System.Data.Entity.EntityState.Modified;
                    dbEntity.SaveChanges();
                    ViewBag.Message = "Information added";
                    return View();
                }
            }
            catch (Exception ex)
            {
                ViewBag.error = ex.Message;
                return View();
            }

        }
        [HttpPost]
        public JsonResult GetPageTextPyID(int Lang_ID)
        {
            try
            {
                var result = dbEntity.New_pages_Text.Where(e => e.Lang_ID == Lang_ID && e.New_pages_ID == Page_Tex.Value).FirstOrDefault();
                PagesTextModel ptm = new PagesTextModel();
                if (result != null)
                {
                    ptm.ID = result.ID;
                    ptm.Image_Cover = result.Image_Cover;
                    ptm.Lang_ID = result.Lang_ID;
                    ptm.New_pages_ID = result.New_pages_ID;
                    ptm.Text_Description = result.Text_Description;
                    ptm.title = result.title;

                }
                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(ptm, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion

        #region AboutPages
        public static int aboutPageID;
        public static int AboutPageTabeID;
        [HttpGet]
        public ActionResult AboutPage(int pageID)
        {
            aboutPageID = pageID;
            var Lam = dbEntity.New_About.Where(e => e.new_pages_ID == pageID).ToList();
            List<AboutModel> am = new List<AboutModel>();
            foreach (var item in Lam)
            {
                var result = new AboutModel();
                result.ID = item.ID;
                result.Title = item.Title;
                result.Lang_name = item.New_lang.Name;
                result.Lang_ID = item.Lang_ID;
                result.new_pages_ID = item.new_pages_ID;
                am.Add(result);
            }
            return View(am);
        }
        [HttpPost]
        public ActionResult AboutPage(AboutModel am)
        {
            New_About na = new New_About();
            if (am.ID == 0)
            {
                if (am.CoverImage != null)
                {
                    am.Image_Cover = SaveImage(am.CoverImage);
                }
                na.Title = am.Title;
                na.new_pages_ID = aboutPageID;
                na.Lang_ID = am.Lang_ID;
                na.Image_Cover = am.Image_Cover;
                dbEntity.New_About.Add(na);
                dbEntity.SaveChanges();

            }
            else
            {
                var value = dbEntity.New_About.Find(am.ID);
                if (am.CoverImage != null)
                {
                    value.Image_Cover = SaveImage(am.CoverImage);
                }
                value.Title = am.Title;
                dbEntity.New_About.Attach(value);
                dbEntity.Entry(value).State = System.Data.Entity.EntityState.Modified;
                dbEntity.SaveChanges();

            }
            return RedirectToAction("AboutPage", new { pageID = aboutPageID });
        }
        [HttpPost]
        public JsonResult GetAboutUsByID(int ID)
        {
            var result = dbEntity.New_About.Find(ID);
            var value = new AboutModel();
            if (result != null)
            {
                value.ID = result.ID;
                value.Title = result.Title;
            }
            Response.StatusCode = (int)HttpStatusCode.OK;
            return Json(value, JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public ActionResult AddTabeForAbout(int PageID)
        {
            AboutPageTabeID = PageID;
            List<AboutTabeModel> ac = new List<AboutTabeModel>();
            var result = dbEntity.New_About_Content.Where(e => e.New_About_ID == PageID).ToList();
            foreach (var item in result)
            {
                var value = new AboutTabeModel();
                value.ID = item.ID;
                value.New_About_ID = item.New_About_ID;
                value.Tab_Content = item.Tab_Content;
                value.Tab_Title = item.Tab_Title;
                value.AboutName = item.New_About.Title;
                ac.Add(value);
            }
            return View(ac);
        }

        [HttpPost]
        public ActionResult AddTabeForAbout(AboutTabeModel tm)
        {
            try
            {

                New_About_Content abco = new New_About_Content();
                if (tm.ID == 0)
                {
                    abco.New_About_ID = AboutPageTabeID;
                    abco.Tab_Content = tm.Tab_Content;
                    abco.Tab_Title = tm.Tab_Title;
                    dbEntity.New_About_Content.Add(abco);
                    dbEntity.SaveChanges();
                }
                else
                {
                    var value = dbEntity.New_About_Content.Find(tm.ID);
                    value.Tab_Title = tm.Tab_Title;
                    value.Tab_Content = tm.Tab_Content;
                    dbEntity.New_About_Content.Attach(value);
                    dbEntity.Entry(value).State = System.Data.Entity.EntityState.Modified;
                    dbEntity.SaveChanges();
                }

                return RedirectToAction("AddTabeForAbout", new { PageID = AboutPageTabeID });

            }
            catch (Exception)
            {
                return RedirectToAction("AddTabeForAbout", new { PageID = AboutPageTabeID });
            }

        }
        [HttpPost]
        public JsonResult GetTabeForAboutByID(int ID)
        {
            var result = dbEntity.New_About_Content.Find(ID);
            var value = new AboutTabeModel();
            if (result != null)
            {
                value.ID = result.ID;
                value.Tab_Title = result.Tab_Title;
                value.Tab_Content = result.Tab_Content;
            }
            Response.StatusCode = (int)HttpStatusCode.OK;
            return Json(value, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult DeleteAboutUsPage(int id)
        {
            try
            {
                var value = dbEntity.New_About.Find(id);
                if (value != null)
                {
                    dbEntity.New_About.Remove(value);
                    dbEntity.SaveChanges();
                }

                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(new { success = true, responseText = "" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, responseText = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        public ActionResult DeleteAcoutUsTabe(int ID)
        {
            try
            {
                var value = dbEntity.New_About_Content.Find(ID);
                if (value != null)
                {
                    dbEntity.New_About_Content.Remove(value);
                    dbEntity.SaveChanges();
                }

                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(new { success = true, responseText = "" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, responseText = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        #endregion

        #region Menu
        [HttpGet]
        public ActionResult Menu()
        {
            var value = dbEntity.New_Menu.ToList();
            List<MenuModel> L_MM = new List<MenuModel>();
            foreach (var item in value)
            {
                var mm = new MenuModel();
                mm.ID = item.ID;
                mm.New_Page_ID = item.New_pages.ID;
                mm.New_Page_Name = item.New_pages.name;
                mm.Lang_name = item.New_lang.Name;
                mm.Title = item.Title;
                L_MM.Add(mm);
            }
            return View(L_MM);
        }
        [HttpPost]
        public JsonResult Menu(string title, int PageID,int LangID)
        {
            New_Menu mm = new New_Menu();
            
            mm.New_Page_ID = PageID;
            mm.Title = title;
            mm.Lang_ID =  LangID;
            dbEntity.New_Menu.Add(mm);
            var result = dbEntity.SaveChanges();
            if (result > 0)
            {
                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(new { success = true, responseText = ViewBag.Message }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, responseText = ViewBag.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        public ActionResult DeleteMenu(int ID)
        {
            try
            {
                var result = dbEntity.New_Menu.Find(ID);
                if (result != null)
                {
                    dbEntity.New_Menu.Remove(result);
                    dbEntity.SaveChanges();
                }
                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(new { success = true, response = "" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion

        #region HomePage
        [HttpGet]
        public ActionResult HomePage()
        {
            HomePageModel hompage = new HomePageModel();
            var value = dbEntity.New_Home_Page.FirstOrDefault();
            if (value != null)
            {
                hompage.ID = value.ID;
                hompage.phoneNumber = value.phoneNumber;
                hompage.MainText = value.MainText;
                hompage.MainVideo = value.MainVideo;
                hompage.MainImage = value.MainImage;
            }
            //var L_lang = dbEntity.New_lang.ToList();
            //hompage.L_Lang = new SelectList(L_lang, "ID", "Name");
            return View(hompage);
        }

        [HttpPost]
        public ActionResult HomePage(HomePageModel hp)
        {
            try
            {
                New_Home_Page hompage = new New_Home_Page();

                if (hp.ID == 0)
                {
                    hompage.MainText = hp.MainText;
                    hompage.phoneNumber = hp.phoneNumber;
                    hompage.MainVideo = hp.MainVideo;
                    if (hp._MainImage != null)
                    {
                        hompage.MainImage = SaveImage(hp._MainImage);
                    }
                    hompage.email = hp.email;

                    dbEntity.New_Home_Page.Add(hompage);
                    dbEntity.SaveChanges();

                    ViewBag.Message = "Home Page Added";

                    return View();
                }
                else
                {
                    var hop = dbEntity.New_Home_Page.Find(hp.ID);
                    hop.MainText = hp.MainText;
                    hop.phoneNumber = hp.phoneNumber;
                    hop.MainVideo = hp.MainVideo;
                    if (hp._MainImage != null)
                    {
                        hop.MainImage = SaveImage(hp._MainImage);
                    }

                    hop.email = hp.email;
                    dbEntity.New_Home_Page.Attach(hop);
                    dbEntity.Entry(hop).State = System.Data.Entity.EntityState.Modified;
                    dbEntity.SaveChanges();

                    Response.StatusCode = (int)HttpStatusCode.OK;
                    ViewBag.Message = "Home Page Updated";

                    return View();
                }



            }
            catch (Exception ex)
            {
                ViewBag.error = "Error message " + ex.Message;
                return View();


            }


        }

        [HttpPost]
        public JsonResult GetHomePageByLang(int LangID)
        {
            try
            {
                New_Home_Page homepage = dbEntity.New_Home_Page.FirstOrDefault();
                HomePageModel ho = new HomePageModel();
                if (homepage != null)
                {
                    ho.ID = homepage.ID;

                }
                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(ho, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = ex.Message }, JsonRequestBehavior.AllowGet);
            }

        }
        #endregion

        #region Slider
        [HttpGet]
        public ActionResult Slider()
        {
            var result = new List<SliderModel>();
            var value = dbEntity.New_Slider.ToList();
            foreach (var item in value)
            {
                var sli = new SliderModel();
                sli.ID = item.ID;
                sli.Image = item.Slider_Image;
                sli.Lang_ID = item.Lang_ID;
                //sli.Lang_Name = item.New_lang.Name;
                sli.TextMessage = item.TextMessage;
                result.Add(sli);
            }

            return View(result);
        }
        [HttpPost]
        public ActionResult Slider(SliderModel image)
        {
            string imagepath;
            try
            {
                if (image != null)
                {
                    imagepath = SaveImage(image.Image_File);
                    New_Slider ns = new New_Slider();
                    ns.Slider_Image = imagepath;
                    ns.TextMessage = image.TextMessage;
                    ns.Lang_ID = 1;
                    dbEntity.New_Slider.Add(ns);
                    var result = dbEntity.SaveChanges();
                    if (result > 0)
                    {
                        return RedirectToAction("Slider");
                    }
                }
            }
            catch (Exception ex)
            {
            }

            return RedirectToAction("Slider");
        }

        [HttpPost]
        public ActionResult DeleteSlider(int SID)
        {
            try
            {
                var value = dbEntity.New_Slider.Find(SID);
                if (value != null)
                {
                    dbEntity.New_Slider.Remove(value);
                    dbEntity.SaveChanges();
                }

                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(new { success = true, responseText = "" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, responseText = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion

        #region Footer_pages
        [HttpGet]
        public ActionResult FooterPages()
        {
            var result = dbEntity.New_FooterMenu.ToList();
            List<FooterPagesModel> L_FP = new List<FooterPagesModel>();
            foreach (var item in result)
            {
                var value = new FooterPagesModel();
                value.ID = item.ID;
                value.PageName = item.New_pages.name;
                L_FP.Add(value);
            }
            return View(L_FP);
        }

        [HttpPost]
        public ActionResult FooterPage(int fpm)
        {
            New_FooterMenu fm = new New_FooterMenu();
            fm.New_page_ID = fpm;

            dbEntity.New_FooterMenu.Add(fm);
            var result = dbEntity.SaveChanges();
            if (result > 0)
            {
                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(new { success = true }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);
            }


        }

        [HttpPost]
        public ActionResult DeleteFooter(int Footer_ID)
        {
            try
            {
                var value = dbEntity.New_FooterMenu.Find(Footer_ID);
                if (value != null)
                {
                    dbEntity.New_FooterMenu.Remove(value);
                    dbEntity.SaveChanges();
                }
                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(new { success = true, responseText = "" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, responseText = "" }, JsonRequestBehavior.AllowGet);
                throw;
            }
        }

        #endregion

        #region News
        [HttpGet]
        public ActionResult News()
        {
            List<New_news> L_news = dbEntity.New_news.ToList();
            List<NewsModel> Lvalue = new List<NewsModel>();
            foreach (var item in L_news)
            {
                var result = new NewsModel();
                result.DateTime = item.DateTime;
                result.FullDescription = item.FullDescription;
                result.ID = item.ID;
                result.Lang_ID = item.Lang_ID;
                //result.Lang_name = item.New_lang.Name;
                result.Title = item.Title;
                Lvalue.Add(result);
            }

            return View(Lvalue);
        }
        [HttpPost]
        public ActionResult News(NewsModel News)
        {
            try
            {
                if (News.ID == 0)
                {
                    if (News.CoverImage != null)
                    {
                        News.Cover_image = SaveImage(News.CoverImage);
                    }
                    if (News.ShourtImage != null)
                    {
                        News.Shourt_image = SaveImage(News.ShourtImage);
                    }

                    New_news nn = new New_news();
                    nn.Cover_image = News.Cover_image;
                    nn.DateTime = News.DateTime;
                    nn.FullDescription = News.FullDescription;
                    nn.Lang_ID = News.Lang_ID;
                    nn.ShortDescription = News.ShortDescription;
                    nn.FullDescription = News.FullDescription;
                    nn.Shourt_image = News.Shourt_image;
                    nn.Title = News.Title;
                    dbEntity.New_news.Add(nn);
                    dbEntity.SaveChanges();
                }
                else
                {
                    New_news nn = dbEntity.New_news.Find(News.ID);
                    if (News.CoverImage != null)
                    {
                        nn.Cover_image = SaveImage(News.CoverImage);
                    }
                    if (News.ShourtImage != null)
                    {
                        nn.Shourt_image = SaveImage(News.ShourtImage);
                    }

                    //nn.Cover_image = News.Cover_image;
                    nn.DateTime = News.DateTime;
                    nn.FullDescription = News.FullDescription;
                    //nn.Lang_ID = News.Lang_ID;
                    nn.ShortDescription = News.ShortDescription;
                    nn.FullDescription = News.FullDescription;
                    //nn.Shourt_image = News.Shourt_image;
                    nn.Title = News.Title;
                    dbEntity.New_news.Attach(nn);
                    dbEntity.Entry(nn).State = System.Data.Entity.EntityState.Modified;
                    dbEntity.SaveChanges();
                }

                return RedirectToAction("News");
            }
            catch (Exception ex)
            {

                return RedirectToAction("News");
            }

        }

        [HttpPost]
        public JsonResult DeleteNews(int News_ID)
        {
            try
            {
                var value = dbEntity.New_news.Find(News_ID);
                if (value != null)
                {
                    dbEntity.New_news.Remove(value);
                    dbEntity.SaveChanges();
                }
                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(new { success = true, responseText = "" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, responseText = "" }, JsonRequestBehavior.AllowGet);
                throw;
            }

        }


        [HttpGet]
        public JsonResult EditNews(int News_ID)
        {
            try
            {
                NewsModel nm = new NewsModel();
                var value = dbEntity.New_news.Find(News_ID);
                if (value != null)
                {
                    nm.ID = value.ID;
                    nm.Title = value.Title;
                    nm.DateTime = value.DateTime;
                    nm.Lang_ID = value.Lang_ID;
                    //nm.Lang_name = value.New_lang.Name;
                    nm.ShortDescription = value.ShortDescription;
                    nm.FullDescription = value.FullDescription;


                }
                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(nm, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(null, JsonRequestBehavior.AllowGet);

            }

        }
        #endregion

        #region Fast_News
        [HttpGet]
        public ActionResult FastNews()
        {
            List<New_FastNewsModel> L_NFNM = new List<New_FastNewsModel>();
            try
            {
                var valus = dbEntity.New_FastNews.ToList();
                foreach (var item in valus)
                {
                    New_FastNewsModel fn = new New_FastNewsModel();
                    fn.ID = item.ID;
                    fn.Lang_ID = item.Lang_ID.Value;
                    //fn.Lang_Name = item.New_lang.Name;
                    fn.NewsText = item.NewsText;
                    L_NFNM.Add(fn);
                }
            }
            catch (Exception ex)
            {
            }
            return View(L_NFNM);
        }
        [HttpPost]
        public JsonResult SaveFastNews(int Lang_ID, string message)
        {
            New_FastNews nfn = new New_FastNews();
            try
            {
                nfn.Lang_ID = Lang_ID;
                nfn.NewsText = message;
                dbEntity.New_FastNews.Add(nfn);
                dbEntity.SaveChanges();
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = true, responseText = ex.Message }, JsonRequestBehavior.AllowGet);
            }
            Response.StatusCode = (int)HttpStatusCode.OK;
            return Json(new { success = true, responseText = "" }, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public JsonResult EditFastNews(int ID, int Lang_ID, string Message)
        {
            try
            {
                var value = dbEntity.New_FastNews.Find(ID);
                if (value != null)
                {
                    value.Lang_ID = Lang_ID;
                    value.NewsText = Message;
                    dbEntity.New_FastNews.Attach(value);
                    dbEntity.Entry(value).State = EntityState.Modified;
                    dbEntity.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = true, responseText = ex.Message }, JsonRequestBehavior.AllowGet);
            }
            Response.StatusCode = (int)HttpStatusCode.OK;
            return Json(new { success = true, responseText = "" }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult GetFastNews(int ID)
        {
            New_FastNewsModel nfn = new New_FastNewsModel();
            try
            {
                var value = dbEntity.New_FastNews.Find(ID);
                nfn.ID = value.ID;
                nfn.Lang_ID = value.Lang_ID.Value;
                nfn.NewsText = value.NewsText;
            }
            catch (Exception ex)
            {
            }
            Response.StatusCode = (int)HttpStatusCode.OK;
            return Json(nfn, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public JsonResult DeleteFastNews(int id)
        {
            try
            {
                var value = dbEntity.New_FastNews.Find(id);
                if (value != null)
                {
                    dbEntity.New_FastNews.Remove(value);
                    dbEntity.SaveChanges();
                }

            }
            catch (Exception ex)
            {
            }
            Response.StatusCode = (int)HttpStatusCode.OK;
            return Json(new { success = true, responseText = "" }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region Pracktes

        [HttpGet]
        public ActionResult Pracktes()
        {
            List<New_pracktes> L_news = dbEntity.New_pracktes.ToList();
            List<PracktesModel> Lvalue = new List<PracktesModel>();
            foreach (var item in L_news)
            {
                var result = new PracktesModel();
                result.DateTime = item.DateTime;
                result.FullDescription = item.FullDescription;
                result.ID = item.ID;
                result.Lang_ID = item.Lang_ID;
                result.Lang_name = item.New_lang.Name;
                result.Title = item.Title;
                Lvalue.Add(result);
            }

            return View(Lvalue);
        }

        [HttpPost]
        public ActionResult Pracktes(PracktesModel News)
        {
            try
            {
                if (News.ID == 0)
                {
                    if (News.CoverImage != null)
                    {
                        News.Cover_image = SaveImage(News.CoverImage);
                    }
                    if (News.ShourtImage != null)
                    {
                        News.Shourt_image = SaveImage(News.ShourtImage);
                    }

                    New_pracktes nn = new New_pracktes();
                    nn.Cover_image = News.Cover_image;
                    nn.DateTime = News.DateTime;
                    nn.FullDescription = News.FullDescription;
                    nn.Lang_ID = News.Lang_ID;
                    nn.ShortDescription = News.ShortDescription;
                    nn.FullDescription = News.FullDescription;
                    nn.Shourt_image = News.Shourt_image;
                    nn.Title = News.Title;
                    dbEntity.New_pracktes.Add(nn);
                    dbEntity.SaveChanges();
                }
                else
                {
                    New_pracktes nn = dbEntity.New_pracktes.Find(News.ID);
                    if (News.CoverImage != null)
                    {
                        nn.Cover_image = SaveImage(News.CoverImage);
                    }
                    if (News.ShourtImage != null)
                    {
                        nn.Shourt_image = SaveImage(News.ShourtImage);
                    }

                    //nn.Cover_image = News.Cover_image;
                    nn.DateTime = News.DateTime;
                    nn.FullDescription = News.FullDescription;
                    nn.Lang_ID = News.Lang_ID;
                    nn.ShortDescription = News.ShortDescription;
                    nn.FullDescription = News.FullDescription;
                    //nn.Shourt_image = News.Shourt_image;
                    nn.Title = News.Title;
                    dbEntity.New_pracktes.Attach(nn);
                    dbEntity.Entry(nn).State = System.Data.Entity.EntityState.Modified;
                    dbEntity.SaveChanges();
                }
                return RedirectToAction("Pracktes");
            }
            catch (Exception ex)
            {

                return RedirectToAction("Pracktes");
            }

        }

        [HttpPost]
        public JsonResult DeletePracktes(int PR_ID)
        {
            try
            {
                var value = dbEntity.New_pracktes.Find(PR_ID);
                if (value != null)
                {
                    dbEntity.New_pracktes.Remove(value);
                    dbEntity.SaveChanges();
                }
                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(new { success = true, responseText = "" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, responseText = "" }, JsonRequestBehavior.AllowGet);
                throw;
            }

        }


        [HttpGet]
        public JsonResult EditPracktes(int PR_ID)
        {
            try
            {
                PracktesModel nm = new PracktesModel();
                var value = dbEntity.New_pracktes.Find(PR_ID);
                if (value != null)
                {
                    nm.ID = value.ID;
                    nm.Title = value.Title;
                    nm.DateTime = value.DateTime;
                    nm.Lang_ID = value.Lang_ID;
                    //nm.Lang_name = value.New_lang.Name;
                    nm.ShortDescription = value.ShortDescription;
                    nm.FullDescription = value.FullDescription;


                }
                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(nm, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(null, JsonRequestBehavior.AllowGet);

            }

        }

        #endregion

        #region socialmedia
        [HttpGet]
        public ActionResult socialmedia()
        {
            //New_SocialMedia Socialmodel = new New_SocialMedia();
            //var L_lang = dbEntity.New_lang.ToList();
            //hompage.L_Lang = new SelectList(L_lang, "ID", "Name");
            return View();
        }

        [HttpPost]
        public ActionResult socialmedia(New_SocialMedia hp)
        {
            try
            {

                if (hp.ID == 0)
                {

                    dbEntity.New_SocialMedia.Add(hp);
                    dbEntity.SaveChanges();

                    ViewBag.Message = "Social Media Added";

                    return View();
                }
                else
                {
                    var hop = dbEntity.New_SocialMedia.Find(hp.ID);
                    hop.Facebook = hp.Facebook;
                    hop.Instgram = hp.Instgram;
                    //hop.Linkedin = hp.Linkedin;
                    hop.WhatsApp = hp.WhatsApp;
                    hop.Twitter = hp.Twitter;
                    //hop.Lang_ID = hp.Lang_ID;
                    hop.Telegram = hp.Telegram;
                    hop.Tiktok = hp.Tiktok;
                    hop.Youtube = hp.Youtube;
                    dbEntity.New_SocialMedia.Attach(hop);
                    dbEntity.Entry(hop).State = System.Data.Entity.EntityState.Modified;
                    dbEntity.SaveChanges();

                    Response.StatusCode = (int)HttpStatusCode.OK;
                    ViewBag.Message = "Social Media Updated";

                    return View();
                }



            }
            catch (Exception ex)
            {
                ViewBag.error = "Error message " + ex.Message;
                return View();
            }


        }

        [HttpPost]
        public JsonResult GetsocialmediaByLang(int LangID)
        {
            try
            {
                New_SocialMedia data = dbEntity.New_SocialMedia.Where(e => e.Lang_ID == LangID).FirstOrDefault();
                SocialMediaModel sm = new SocialMediaModel();
                if (data != null)
                {
                    sm.ID = data.ID;
                    sm.Instgram = data.Instgram;
                    sm.Facebook = data.Facebook;
                    sm.WhatsApp = data.WhatsApp;
                    sm.Twitter = data.Twitter;
                    sm.Youtube = data.Youtube;
                    sm.Tiktok = data.Tiktok;
                    sm.Telegram = data.Telegram;
                }
                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(sm, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = ex.Message }, JsonRequestBehavior.AllowGet);
            }

        }
        #endregion

        #region LincesReg
        [HttpGet]
        public ActionResult LincesReg()
        {

            LR_Model LR = new LR_Model();
            //var L_lang = dbEntity.New_lang.ToList();
            //hompage.L_Lang = new SelectList(L_lang, "ID", "Name");
            return View(LR);
        }

        [HttpPost]
        public ActionResult LincesReg(LR_Model hp)
        {
            try
            {
                if (hp.Imagecover != null)
                {
                    hp.Image_cover = SaveImage(hp.Imagecover);
                }


                if (hp.ID == 0)
                {
                    New_LR lr = new New_LR();
                    if (hp.Imagecover != null)
                    {
                        lr.Image_cover = SaveImage(hp.Imagecover);
                    }
                    lr.Title = hp.Title;
                    lr.FirstName_tran = hp.FirstName_tran;
                    lr.Lang_ID = hp.Lang_ID;
                    lr.Password_tran = hp.Password_tran;
                    lr.LastName_tran = hp.LastName_tran;
                    lr.Mobile_number_tran = hp.Mobile_number_tran;
                    lr.Place_of_birth_tran = hp.Place_of_birth_tran;
                    lr.Private_one = hp.Private_one;
                    lr.Private_three = hp.Private_three;
                    lr.Private_two = hp.Private_two;
                    lr.Country = hp.Country_tran;
                    dbEntity.New_LR.Add(lr);
                    dbEntity.SaveChanges();
                    ViewBag.Message = "Info Adedd";
                    return View();
                }
                else
                {
                    var lr = dbEntity.New_LR.Find(hp.ID);
                    if (hp.Imagecover != null)
                    {
                        lr.Image_cover = SaveImage(hp.Imagecover);
                    }
                    lr.Title = hp.Title;
                    lr.Email_tran = hp.Email_tran;
                    lr.FirstName_tran = hp.FirstName_tran;
                    lr.Lang_ID = hp.Lang_ID;
                    lr.LastName_tran = hp.LastName_tran;
                    lr.Password_tran = hp.Password_tran;
                    lr.Mobile_number_tran = hp.Mobile_number_tran;
                    lr.Place_of_birth_tran = hp.Place_of_birth_tran;
                    lr.Private_one = hp.Private_one;
                    lr.Private_three = hp.Private_three;
                    lr.Private_two = hp.Private_two;
                    lr.Country = hp.Country_tran;
                    dbEntity.New_LR.Attach(lr);
                    dbEntity.Entry(lr).State = System.Data.Entity.EntityState.Modified;
                    var val = dbEntity.SaveChanges();
                    if (val > 0)
                    {
                        Response.StatusCode = (int)HttpStatusCode.OK;
                        ViewBag.Message = "Info Updated";
                    }
                    else
                    {
                        ViewBag.Message = "Can't Update info";
                    }

                    return View();
                }
            }
            catch (Exception ex)
            {
                ViewBag.error = "Error message " + ex.Message;
                return View();
            }
        }

        [HttpPost]
        public JsonResult GetLincesRegByLang(int LangID)
        {
            try
            {
                var hp = dbEntity.New_LR.Where(e => e.Lang_ID == LangID).FirstOrDefault();
                LR_Model lr = new LR_Model();
                if (hp != null)
                {
                    lr.ID = hp.ID;
                    lr.Title = hp.Title;
                    lr.Email_tran = hp.Email_tran;
                    lr.FirstName_tran = hp.FirstName_tran;
                    lr.Lang_ID = hp.Lang_ID;
                    lr.LastName_tran = hp.LastName_tran;
                    lr.Image_cover = hp.Image_cover;
                    lr.Password_tran = hp.Password_tran;
                    lr.Mobile_number_tran = hp.Mobile_number_tran;
                    lr.Place_of_birth_tran = hp.Place_of_birth_tran;
                    lr.Private_one = hp.Private_one;
                    lr.Private_two = hp.Private_two;
                    lr.Private_three = hp.Private_three;
                    lr.Country_tran = hp.Country;
                }
                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(lr, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion

        #region Color_UI_UX

        [HttpGet]
        public ActionResult ChangeColor()
        {
            StyleColorModel model = new StyleColorModel();
            try
            {
                var result = dbEntity.StyleTables.FirstOrDefault();
                if (result != null)
                {
                    model.PracticeBackgroundColor = result.PracticeBackgroundColor;
                    model.UpderMenuColor = result.UpderMenuColor;
                    model.BackGroundMinuColor = result.BackGroundMinuColor;
                    model.FooterColor = result.FooterColor;
                    model.MinuColor = result.MinuColor;
                }
            }
            catch (Exception)
            {

            }


            return View(model);
        }
        [HttpPost]
        public JsonResult UpdateStyle(string UM_ID, string BGMC_ID, string MinuColor_ID, string NewsBackground_ID, string FooterColor_ID)
        {
            var result = dbEntity.StyleTables.FirstOrDefault();
            if (result != null)
            {
                result.PracticeBackgroundColor = NewsBackground_ID;
                result.UpderMenuColor = UM_ID;
                result.BackGroundMinuColor = BGMC_ID;
                result.FooterColor = FooterColor_ID;
                result.MinuColor = MinuColor_ID;
                dbEntity.StyleTables.Attach(result);
                dbEntity.Entry(result).State = EntityState.Modified;
                dbEntity.SaveChanges();
            }
            var value = Server.MapPath("~/Content/RootStyle.css");
            var jsonData = System.IO.File.ReadAllText(value);

            var ccfile = ":root {--UpderMenuColor:" + UM_ID + "; --BackGroundMinuColor:" + BGMC_ID + "; --MinuColor:" + MinuColor_ID + "; --PracticeBackgroundColor:" + NewsBackground_ID +
                "; --FooterColor:" + FooterColor_ID + ";}";

            System.IO.File.WriteAllText(value, ccfile);
            return Json(new { Success = true, response = new { Success = false, response = new { } } });
        }
        #endregion
        #endregion

        #region Store
        #region storeCategoryes
        [HttpGet]
        public ActionResult StoreCategory()
        {
            var result = dbEntity.StoreCategories.ToList();
            List<StoreCategoryesModel> Lsgm = new List<StoreCategoryesModel>();
            foreach (var item in result)
            {
                StoreCategoryesModel sgm = new StoreCategoryesModel();
                sgm.ID = item.ID;
                sgm.Name = item.Name;
                Lsgm.Add(sgm);
            }
            return View(Lsgm);
        }
        [HttpPost]
        public JsonResult StoreCategory(string catName)
        {
            try
            {
                StoreCategory sc = new StoreCategory();
                sc.Name = catName;
                dbEntity.StoreCategories.Add(sc);
                dbEntity.SaveChanges();
                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(new { success = false, response = "تم الإضافة" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = ex.Message }, JsonRequestBehavior.AllowGet);
            }

        }
        [HttpPost]
        public async Task<JsonResult> EStoreCategory(string catName, int id)
        {
            try
            {
                var result = await dbEntity.StoreCategories.FindAsync(id);
                if (result != null)
                {
                    result.Name = catName;
                    dbEntity.StoreCategories.Attach(result);
                    dbEntity.Entry(result).State = EntityState.Modified;
                    dbEntity.SaveChanges();
                }
                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(new { success = false, response = "" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = ex.Message }, JsonRequestBehavior.AllowGet);
            }

        }
        [HttpPost]
        public JsonResult DeleteStoreCategory(int id)
        {
            try
            {
                var result = dbEntity.StoreCategories.Find(id);
                dbEntity.StoreCategories.Remove(result);
                dbEntity.SaveChanges();
                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(new { success = false, response = "تم الحذف" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        public JsonResult GetStoreCategory(int CatID)
        {
            var result = dbEntity.StoreCategories.Find(CatID);
            StoreCategoryesModel scm = new StoreCategoryesModel();
            scm.ID = result.ID;
            scm.Name = result.Name;
            Response.StatusCode = (int)HttpStatusCode.OK;
            return Json(new { success = true, response = scm }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region Product
        [HttpGet]
        public ActionResult StoreProduct()
        {
            List<StoreProductModel> L_sp = new List<StoreProductModel>();
            try
            {

                var result = dbEntity.StoreProducts.ToList();
                foreach (var item in result)
                {
                    StoreProductModel sp = new StoreProductModel();
                    sp.ID = item.ID;
                    sp.name = item.name;
                    sp.Category_ID = item.Category_ID;
                    sp.categoryName = item.StoreCategory.Name;
                    sp.Price = (float)item.Price;
                    L_sp.Add(sp);
                }
            }
            catch (Exception ex)
            {
            }
            return View(L_sp);
        }
        [HttpPost]
        public ActionResult CreateProduct(StoreProductModel spm)
        {
            StoreProduct sc = new StoreProduct();
            sc.name = spm.name;
            sc.Category_ID = spm.Category_ID;
            sc.Description = spm.Description;
            sc.Price = spm.Price;
            foreach (var item in spm.LPFB)
            {
                sc.Images += SaveImage(item) + ";";
            }
            dbEntity.StoreProducts.Add(sc);
            dbEntity.SaveChanges();
            return RedirectToAction("StoreProduct");
        }
        [HttpPost]
        public ActionResult EditeProduct(StoreProductModel spm)
        {
            try
            {
                var pro = dbEntity.StoreProducts.Find(spm.ID);
                if (pro != null)
                {
                    pro.name = spm.name;
                    pro.Price = spm.Price;
                    pro.Category_ID = spm.Category_ID;
                    pro.Description = spm.Description;
                    foreach (var item in spm.LPFB)
                    {
                        pro.Images += SaveImage(item) + ";";
                    }
                    dbEntity.StoreProducts.Attach(pro);
                    dbEntity.Entry(pro).State = System.Data.Entity.EntityState.Modified;
                    dbEntity.SaveChanges();
                }
            }
            catch (Exception ex)
            {
            }

            return RedirectToAction("StoreProduct");
        }

        [HttpPost]
        public JsonResult getProductInfo(int id)
        {
            var pro = dbEntity.StoreProducts.Find(id);
            StoreProductModel spm = new StoreProductModel();
            spm.ID = id;
            spm.Category_ID = pro.Category_ID;
            spm.name = pro.name;
            spm.Price = (float)pro.Price;
            spm.Description = pro.Description;
            Response.StatusCode = (int)HttpStatusCode.OK;
            return Json(new { success = false, response = spm }, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public JsonResult DeleteStoreProduct(int id)
        {
            try
            {
                var pro = dbEntity.StoreProducts.Find(id);
                dbEntity.StoreProducts.Remove(pro);
                dbEntity.SaveChanges();
            }
            catch (Exception ex)
            {
            }
            Response.StatusCode = (int)HttpStatusCode.OK;
            return Json(new { success = false, response = "" }, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public JsonResult GetAllStoreCategory()
        {
            List<StoreCategoryesModel> L_ctm = new List<StoreCategoryesModel>();
            try
            {
                var values = dbEntity.StoreCategories.ToList();
                foreach (var item in values)
                {
                    StoreCategoryesModel ctm = new StoreCategoryesModel();
                    ctm.ID = item.ID;
                    ctm.Name = item.Name;

                    L_ctm.Add(ctm);
                }
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = "" }, JsonRequestBehavior.AllowGet);
            }
            Response.StatusCode = (int)HttpStatusCode.OK;
            return Json(L_ctm, JsonRequestBehavior.AllowGet);
        }
        #endregion
        #endregion
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