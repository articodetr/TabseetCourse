using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using tabseetCourse.Classes;
using tabseetCourse.Data;
using tabseetCourse.Models;
using Microsoft.AspNet.Identity;

namespace tabseetCourse.Controllers
{
    public class TeacherController : SharedCalss
    {
        // GET: Teacher
        public ActionResult Index()
        {
            //var username = GetCurrentUser();
            //if (username != null)
            //{

            //}
            return View();
        }
        #region Categores
        [HttpGet]
        public ActionResult TeacherCourse()
        {
            List<CategoryModel> L_cat = new List<CategoryModel>();
            try
            {

                if (CurrentUser != null)
                {
                    Session["username"] = CurrentUser.RealName;
                    Session["photo"] = CurrentUser.Per_Photo;
                    var value = userManager.Users.Where(e => e.userOrTetcherOrStudent == 1 && e.Id == CurrentUser.ID).Select(i => new { i.Categories_ID }).FirstOrDefault();
                    if (value != null)
                    {
                        string List_Cat = value.Categories_ID;
                        var cat = SpliteString(List_Cat, ',');
                        foreach (var item2 in cat)
                        {
                            var result = SpliteString(item2, '/');
                            if (result.Count > 1)
                            {
                                int categoryid = int.Parse(result[0].ToString());
                                var result1 = dbEntity.Categories.Find(categoryid);

                                CategoryModel ctm = new CategoryModel();
                                ctm.ID = result1.ID;
                                ctm.NameAr = result1.NameAr;
                                ctm.NameEn = result1.NameEn;
                                if (result1.Exams != null && result1.Exams.Count > 0)
                                {
                                    ctm.exam_Name = result1.Exams.FirstOrDefault().Name;
                                }
                                else
                                {
                                    ctm.exam_Name = null;
                                }


                                if (result1.CategoryType_ID != null)
                                {
                                    ctm.CategoryType_ID = result1.CategoryType_ID.Value;
                                    ctm.CategoryTypeName = result1.CategoryType.NameAR;
                                }
                                if (result1.IsPayed != null)
                                {
                                    ctm.IsPayed = result1.IsPayed.Value;
                                    ctm.PayedValue = result1.PayedValue;
                                }
                                if (result1.ParentCateogry_ID != null)
                                {
                                    ctm.ParentCateogry_ID = result1.ParentCateogry_ID.Value;
                                    ctm.ParentCategoryName = result1.Category1.NameAr;
                                }

                                L_cat.Add(ctm);




                                //.Select(x => new { x.ID, x.NameAr, x.NameEn, x.Category2, x.CategoryType_ID, x.CategoryType.NameAR, x.IsPayed, x.PayedValue, x.ParentCateogry_ID, examName = x.Exams.FirstOrDefault().Name }).OrderByDescending(i => i.ID);

                            }

                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return View(L_cat);
        }

        [HttpGet]
        public ActionResult TeacherCoursestd(int ID)
        {
            List<UserModel> L_iden = new List<UserModel>();
            //try
            //{

            //    var result = dbEntity.Std_Reg_Category.Where(e => e.Category_ID == ID).Select(x => new { x.AspNetUser.Id, x.AspNetUser.RealName, x.AspNetUser.Email, x.Category.NameAr, x.Category.Lectures.Count, x.Category.Lectures }).ToList();
            //    foreach (var item in result)
            //    {
            //        ViewBag.categoryName = item.NameAr;
            //        UserModel myi = new UserModel();
            //        myi.ID = item.Id;
            //        myi.RealName = item.RealName;
            //        myi.Email = item.Email;
            //        myi.lectureCount = item.Count;
            //        foreach (var item3 in item.Lectures)
            //        {
            //            var value = dbEntity.std_lecture.Select(e => new { e.ID, e.IsFinish, e.Std_ID, e.Lecture_ID }).Where(e => e.IsFinish == 1 && e.Std_ID == myi.ID && e.Lecture_ID == item3.ID).ToList();
            //            foreach (var item2 in value)
            //            {
            //                myi.lectureFinishCount += 1;
            //            }
            //        }
            //        L_iden.Add(myi);
            //    }

            //}
            //catch (Exception ex)
            //{
            //}
            return View(L_iden);
        }
        private List<int> GetTeacherCategoryIds(string categoriesString)
        {
            List<int> result = new List<int>();
            if (string.IsNullOrWhiteSpace(categoriesString))
                return result;

            string[] items = categoriesString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var item in items)
            {
                string[] parts = item.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0 && int.TryParse(parts[0].Trim(), out int catId))
                {
                    if (!result.Contains(catId))
                    {
                        result.Add(catId);
                    }
                }
            }
            return result;
        }

        private bool IsAuthorizedForCategory(int categoryId, out string errorMessage)
        {
            errorMessage = null;
            if (!Request.IsAuthenticated || User == null || User.Identity == null || !User.Identity.IsAuthenticated)
            {
                errorMessage = "المستخدم غير مسجل الدخول";
                return false;
            }

            string userId = User.Identity.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                errorMessage = "المستخدم غير مسجل الدخول";
                return false;
            }

            var user = userManager.FindById(userId);
            if (user == null || user.IsActive != 1)
            {
                errorMessage = "حساب المستخدم غير فعال أو غير موجود";
                return false;
            }

            var roles = userManager.GetRoles(userId);
            if (roles != null && roles.Contains("Admin"))
            {
                return true;
            }

            if (roles != null && roles.Contains("Teacher"))
            {
                var catIds = GetTeacherCategoryIds(user.Categories_ID);
                if (catIds.Contains(categoryId))
                {
                    return true;
                }
                errorMessage = "الدورة غير مسندة لهذا المدرس";
                return false;
            }

            errorMessage = "غير مصرح لك بإدارة محاضرات هذه الدورة";
            return false;
        }

        [HttpGet]
        public ActionResult LecturesByCategoryStudend(int Cat_ID, string categoryName)
        {
            if (!IsAuthorizedForCategory(Cat_ID, out string errorRedirect))
            {
                return RedirectToAction("TeacherCourse", "Teacher");
            }

            ViewBag.CategoryID = Cat_ID;
            ViewBag.Name = categoryName;
            return View();
        }

        [HttpPost]
        public JsonResult LectureByCategorysStd(int Cat_ID)
        {
            try
            {
                if (!IsAuthorizedForCategory(Cat_ID, out string authError))
                {
                    Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    return Json(new { success = false, response = authError }, JsonRequestBehavior.AllowGet);
                }

                var values = dbEntity.Lectures.Where(i => (i.Category_ID == Cat_ID))
                    .Select(i => new {
                        i.ID,
                        i.Lecture_Name,
                        i.Category_ID,
                        i.Category.NameAr,
                        i.Category.NameEn,
                        i.LectureTypeOnOff,
                        LectureLink_Host = string.Empty,
                        i.LectureLink_Client,
                        i.Assignments.Count
                    }).ToList();
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
        [HttpPost]
        public JsonResult GetAllCategories4Teacher()
        {
            List<CategoryModel> catid = new List<CategoryModel>();
            try
            {


                if (CurrentUser != null)
                {

                    //List<int> catid = new List<int>();

                    var cat = SpliteString(CurrentUser.Category_ID, ',');
                    foreach (var item in cat)
                    {
                        var result = SpliteString(item, '/');
                        if (result.Count > 1)
                        {
                            CategoryModel ctm = new CategoryModel();
                            catid.Add(new CategoryModel()
                            {
                                ID = int.Parse(result[0]),
                                NameAr = result[1]
                            });
                        }
                    }
                }
                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(catid, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, responseText = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public async Task<ActionResult> StdonlineCourse()
        {
            List<LectureModel> LM = new List<LectureModel>();
            try
            {

                if (CurrentUser != null)
                {
                    var cat = SpliteString(CurrentUser.Category_ID, ',');
                    foreach (var item in cat)
                    {
                        var result = SpliteString(item, '/');
                        if (result.Count > 1)
                        {

                            int lec = int.Parse(result[0].ToString());
                            var lecture = dbEntity.Lectures.Where(e => e.Category_ID == lec && e.LectureTypeOnOff == "1").Select(e => new { e.ID, e.Category.NameAr, e.Lecture_Name, e.DayandTime, e.LectureLink_Client, e.LectureLink_Host, e.MediaLink }).ToList();
                            foreach (var item2 in lecture)
                            {
                                LectureModel ctm = new LectureModel();
                                ctm.ID = item2.ID;
                                ctm.CategoresName = item2.NameAr;
                                ctm.Lecture_Name = item2.Lecture_Name;
                                ctm.DayandTime = DateTime.Parse(item2.DayandTime).ToString("MM/dd/yyyy h:mm");
                                if (DateTime.Parse(item2.DayandTime).AddHours(2) < DateTime.Now)
                                {
                                    ctm.Is_Lecture_End = true;
                                }
                                else
                                {
                                    ctm.Is_Lecture_End = false;
                                }
                                //ctm.DocumenFile = item2.DocumenFile;
                                ctm.LectureLink_Client = item2.LectureLink_Client;
                                ctm.LectureLink_Host = item2.LectureLink_Host;
                                ctm.MediaLink = item2.MediaLink;
                                LM.Add(ctm);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Printerror("StdonlineCourse", ex.Message, ex.InnerException.Message, ex.StackTrace);
            }


            return View(LM);
        }
        #endregion

        #region Assignment

        [HttpGet]
        public ActionResult Assignment()
        {
            return View();
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
                        else if (item.Question.Type == 3)
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
            var lectureinfo = dbEntity.Lectures.Select(i => new { i.ID, i.Lecture_Name, i.Category_ID }).Where(i => i.ID == Lecture_ID).FirstOrDefault();
            ViewBag.Lecture = lectureinfo.Lecture_Name;
            ViewBag.LectureID = Lecture_ID;
            ViewBag.categoryName = categoryName;
            ViewBag.categorieID = lectureinfo.Category_ID;

            return View();
        }

        [HttpGet]
        public ActionResult ViewAssingment(int lecture_ID, string categoryName)
        {

            var lectureinfo = dbEntity.Lectures.Select(i => new { lectureID = i.ID, i.Lecture_Name, i.Category_ID, assingment_ID = i.Assignments.FirstOrDefault().ID }).Where(i => i.lectureID == lecture_ID).FirstOrDefault();
            ViewBag.Lecture_ID = lectureinfo.lectureID;
            ViewBag.Lecture = lectureinfo.Lecture_Name;
            ViewBag.categoryName = categoryName;
            ViewBag.categorieID = lectureinfo.Category_ID;
            ViewBag.AssignmentID = lectureinfo.assingment_ID;
            return View();
        }

        #endregion

        #region Exams
        [HttpGet]
        public ActionResult ExamsTeacher(string Category_ID, string categoryName)
        {
            ViewBag.categoryName = categoryName;
            ViewBag.categorieID = Category_ID;
            return View();
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
        public ActionResult ViewExamTeacher(int Category_ID)
        {
            var result = dbEntity.Exams.Where(e => e.Category_ID == Category_ID).Select(i => new { i.ID, i.Category.NameAr }).FirstOrDefault();
            ViewBag.Exam_ID = result.ID;
            ViewBag.CategoresName = result.NameAr;
            ViewBag.Categores_ID = Category_ID;
            return View();
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
        public JsonResult CreateExamsTeacher(ExamModel em)
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
        public JsonResult EditExamsTeacher(ExamModel em)
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
        }
        #endregion

        #region QuestionBank
        [HttpGet]
        public ActionResult QuestionBankTeacher()
        {

            return View();
        }


        [HttpPost]
        public JsonResult GetAllQuesinBankTeacher(int cat_ID = 0)
        {
            List<QuestionsModel> Lqm = new List<QuestionsModel>();

            try
            {
                //var testcher = GetCurrentUser();
                //var category = await userManager.FindByIdAsync(testcher.ID);
                //if (category != null)
                //{

                //List<int> catid = new List<int>();

                //var cat = SpliteString(category.Categories_ID, ',');
                //foreach (var item in cat)
                //{
                //    var result = SpliteString(item, '/');
                //    if (result.Count > 1)
                //    {
                //        catid.Add(int.Parse(result[0]));
                //    }

                //}

                List<Question> L_q = new List<Question>();
                //foreach (var item in catid)
                //{
                var value = dbEntity.Questions.Where(i => i.Category_ID == cat_ID).ToList();
                L_q.AddRange(value);
                //}

                if (L_q != null)
                {
                    foreach (var item in L_q)
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
                //}
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
        public async Task<JsonResult> GetQuesinBankTeacher()
        {
            List<QuestionsModel> Lqm = new List<QuestionsModel>();

            try
            {

                var category = await userManager.FindByIdAsync(CurrentUser.ID);
                if (category != null)
                {

                    List<int> catid = new List<int>();

                    var cat = SpliteString(category.Categories_ID, ',');
                    foreach (var item in cat)
                    {
                        var result = SpliteString(item, '/');
                        if (result.Count > 1)
                        {
                            catid.Add(int.Parse(result[0]));
                        }

                    }

                    List<Question> L_q = new List<Question>();
                    foreach (var item in catid)
                    {
                        var value = dbEntity.Questions.Where(i => i.Category_ID == item).ToList();
                        L_q.AddRange(value);
                    }

                    if (L_q != null)
                    {
                        foreach (var item in L_q)
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
                            else
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

            return View();
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
            return View();
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
                    return RedirectToAction("EditeMatchingTeacher", new { id = que.ID });
                }
            }
            return View();
        }
        [HttpGet]
        public ActionResult EditTrueOrFalse(int id)
        {
            ViewBag.question_ID = id;
            return View();
        }
        [HttpGet]
        public ActionResult EditMultyChoise(int id)
        {
            ViewBag.question_ID = id;
            return View();
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
        public ActionResult CreateMatchingChoiseQuestionTeacher()
        {

            return View();
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
        public ActionResult EditeMatchingTeacher(int id)
        {
            ViewBag.question_ID = id;
            return View();
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

        #region Chat
        [HttpGet]
        public ActionResult ChatPageTeacher()
        {
            List<CategoryModel> L_cat = new List<CategoryModel>();
            try
            {

                if (CurrentUser != null)
                {
                    ViewBag.stdname = CurrentUser.RealName;
                    ViewBag.stdID = CurrentUser.ID;
                }
                if (CurrentUser != null)
                {
                    var value = userManager.Users.Where(e => e.userOrTetcherOrStudent == 1 && e.Id == CurrentUser.ID).Select(i => new { i.Categories_ID }).FirstOrDefault();
                    if (value != null)
                    {
                        string List_Cat = value.Categories_ID;
                        var cat = SpliteString(List_Cat, ',');
                        foreach (var item2 in cat)
                        {
                            var result = SpliteString(item2, '/');
                            if (result.Count > 1)
                            {
                                int categoryid = int.Parse(result[0].ToString());
                                var result1 = dbEntity.Categories.Find(categoryid);

                                CategoryModel ctm = new CategoryModel();
                                ctm.ID = result1.ID;
                                ctm.NameAr = result1.NameAr;
                                ctm.NameEn = result1.NameEn;
                                if (result1.Exams != null && result1.Exams.Count > 0)
                                {
                                    ctm.exam_Name = result1.Exams.FirstOrDefault().Name;
                                }
                                else
                                {
                                    ctm.exam_Name = null;
                                }


                                if (result1.CategoryType_ID != null)
                                {
                                    ctm.CategoryType_ID = result1.CategoryType_ID.Value;
                                    ctm.CategoryTypeName = result1.CategoryType.NameAR;
                                }
                                if (result1.IsPayed != null)
                                {
                                    ctm.IsPayed = result1.IsPayed.Value;
                                    ctm.PayedValue = result1.PayedValue;
                                }
                                if (result1.ParentCateogry_ID != null)
                                {
                                    ctm.ParentCateogry_ID = result1.ParentCateogry_ID.Value;
                                    ctm.ParentCategoryName = result1.Category1.NameAr;
                                }

                                L_cat.Add(ctm);




                                //.Select(x => new { x.ID, x.NameAr, x.NameEn, x.Category2, x.CategoryType_ID, x.CategoryType.NameAR, x.IsPayed, x.PayedValue, x.ParentCateogry_ID, examName = x.Exams.FirstOrDefault().Name }).OrderByDescending(i => i.ID);

                            }

                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return View(L_cat);
        }
        #endregion

        #region PersonalInfo 
        [HttpGet]
        public ActionResult TecPersonalInfo()
        {

            var result = CurrentUser;

            return View(result);
        }
        [HttpPost]
        public async Task<JsonResult> SaveInfo(UserModel PInFom)
        {
            MyIdentityUser pin;
            try
            {
                pin = await userManager.FindByIdAsync(PInFom.ID);
                if (pin != null)
                {
                    pin.Location = PInFom.Location;
                    pin.Education = PInFom.Education;
                    pin.Email = PInFom.Email;
                    pin.Experience = PInFom.Experience;
                    pin.Password = PInFom.Password;
                    if (PInFom.Password != null)
                    {
                        var value = MapingUserModelToIdentity(pin);
                        ChangePassword(value, PInFom.Password);
                    }
                    pin.PhoneNumber = PInFom.PhoneNumber;
                    pin.RealName = PInFom.RealName;
                    pin.FirstName = PInFom.RealName;
                    pin.LastName = PInFom.RealName;
                    pin.Studing = PInFom.Studing;
                    if (PInFom.PersionImageFile != null)
                    {
                        pin.Per_Photo = SaveImage(PInFom.PersionImageFile);
                    }
                    await userManager.UpdateAsync(pin);
                    Response.StatusCode = (int)HttpStatusCode.OK;
                    return Json(new { sucsess = true, message = "" }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { sucsess = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
            Response.StatusCode = (int)HttpStatusCode.BadRequest;
            return Json(new { sucsess = false, message = "" }, JsonRequestBehavior.AllowGet);
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
    }
}