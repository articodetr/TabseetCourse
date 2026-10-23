using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
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
    public class StudentsController : SharedCalss
    {
        // GET: Students
        public HomePageModel HomepageViewModel { get; set; }
        public StudentsController()
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
        // GET: Students
        public ActionResult Index()
        {
            //var username = GetCurrentUser();
            //if (username != null)
            //{
            //    Session["username"] = username.FullName;
            //}

            return View();
        }

        #region PersonalInfo
        [HttpGet]
        public ActionResult PersonalInfo()
        {
            UserModel userModel = new UserModel();
            try
            {
                userModel = CurrentUser;
            }
            catch (Exception ex)
            {

            }


            return View(userModel);
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
                    pin.Education = PInFom.Education;
                    pin.Email = PInFom.Email;
                    pin.Experience = PInFom.Experience;
                    pin.Password = PInFom.Password;
                    if (PInFom.Password != null)
                    {

                        ChangePassword(CurrentUser, PInFom.Password);
                    }
                    pin.PhoneNumber = PInFom.PhoneNumber;
                    pin.RealName = PInFom.RealName;
                    pin.FirstName = PInFom.RealName;
                    pin.LastName = PInFom.LastName;
                    pin.Studing = PInFom.Studing;
                    if (PInFom.PersionImageFile != null)
                    {
                        pin.Per_Photo = SaveImage(PInFom.PersionImageFile);
                    }
                    await userManager.UpdateAsync(pin);
                    Session["username"] = pin.RealName;
                }
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { sucsess = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
            Response.StatusCode = (int)HttpStatusCode.OK;
            return Json(new { sucsess = true, message = "" }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region Categories

        [HttpGet]
        public ActionResult CategoriesStd()
        {

            List<Std_Reg_CategoryModel> L_ctm = new List<Std_Reg_CategoryModel>();
            List<Std_Reg_Category> L_ct = new List<Std_Reg_Category>();
            try
            {
                //  ViewBag.categoryType_ID = categoryType_ID;

                if (CurrentUser != null)
                {
                    Session["photo"] = CurrentUser.Per_Photo;
                    Session["username"] = CurrentUser.RealName;
                    var values = dbEntity.Std_Reg_Category.Where(e => e.Std_ID == CurrentUser.ID && e.Category.ParentCateogry_ID == null).OrderByDescending(i => i.std_category_Ispayed).ToList();
                    L_ct.AddRange(values);
                    var values2 = dbEntity.Std_Reg_Category.Where(e => e.Std_ID == CurrentUser.ID && e.Category.ParentCateogry_ID != null).ToList();
                    L_ct.AddRange(values2);


                    foreach (var item2 in values2)
                    {
                        foreach (var item in values)
                        {
                            if (item2.Category.ParentCateogry_ID == item.Category_ID)
                            {
                                L_ct.Remove(item2);
                            }
                        }
                    }

                    var teachers = dbEntity.AspNetUsers
                        .Where(u => u.userOrTetcherOrStudent == 1 || u.AspNetRoles.Any(r => r.Name == "Teacher"))
                        .ToList();

                    var categoryTeacherMap = new Dictionary<int, string>();
                    foreach (var t in teachers)
                    {
                        if (!string.IsNullOrWhiteSpace(t.Categories_ID))
                        {
                            var catIds = GetTeacherCategoryIds(t.Categories_ID);
                            string tName = !string.IsNullOrWhiteSpace(t.RealName) ? t.RealName.Trim() : ((t.FirstName ?? "") + " " + (t.LastName ?? "")).Trim();
                            foreach (var cid in catIds)
                            {
                                if (!categoryTeacherMap.ContainsKey(cid))
                                {
                                    categoryTeacherMap[cid] = tName;
                                }
                            }
                        }
                    }

                    foreach (var item in L_ct)
                    {

                        Std_Reg_CategoryModel ctm = new Std_Reg_CategoryModel();
                        ctm.ID = item.ID;
                        ctm.CategoryName = item.Category.NameAr;
                        ctm.Icon = item.Category.Icon;
                        ctm.Category_ID = item.Category_ID.Value;
                        ctm.category_Ispayed = item.category_Ispayed.Value;
                        ctm.std_category_Ispayed = item.std_category_Ispayed.Value;
                        ctm.lectureCount = 0;
                        ctm.DocumentBook = item.Category.DocumentBook;
                        ctm.Description = item.Category.Description;
                        if (categoryTeacherMap.ContainsKey(item.Category_ID.Value))
                        {
                            ctm.TeacherName = categoryTeacherMap[item.Category_ID.Value];
                        }

                        if (item.Category.ParentCateogry_ID == null)
                        {

                            ctm.lectureCount = item.Category.Lectures.Count;
                            foreach (var item3 in item.Category.Categories1)
                            {
                                ctm.lectureCount += item3.Lectures.Count;
                                var value = dbEntity.std_lecture.Select(e => new { e.ID, e.IsFinish, e.Std_ID, e.Lecture_ID, e.Lecture }).Where(e => e.IsFinish == 1 && e.Std_ID == CurrentUser.ID && e.Lecture.Category_ID == item3.ID).ToList();

                                foreach (var item2 in value)
                                {
                                    ctm.lectureFinishCount += 1;
                                }
                            }
                        }
                        else
                        {
                            if (item.Category.Lectures.Count > 0)
                            {

                                ctm.lectureCount = item.Category.Lectures.Count;
                            }

                        }
                        ctm.categoryType_Nmae = item.Category.CategoryType.NameAR;
                        ctm.Std_Reg_CategoryType = item.Category.CategoryType.ID;
                        foreach (var item3 in item.Category.Lectures)
                        {
                            var value = dbEntity.std_lecture.Select(e => new { e.ID, e.IsFinish, e.Std_ID, e.Lecture_ID }).Where(e => e.IsFinish == 1 && e.Std_ID == CurrentUser.ID && e.Lecture_ID == item3.ID).ToList();
                            foreach (var item2 in value)
                            {
                                ctm.lectureFinishCount += 1;
                            }
                        }

                        if (item.Category.Exams != null && item.Category.Exams.Count > 0)
                        {
                            ctm.Exam_ID = item.Category.Exams.FirstOrDefault().ID;
                        }
                        if (item.Category.PayedValue != "0")
                        {
                            int result = 0;
                            int.TryParse(item.Category.PayedValue, out result);
                            ctm.categoryPrice = result;
                        }
                        if (item.std_exam_isFinish != null)
                        {
                            ctm.std_exam_isFinish = item.std_exam_isFinish.Value;
                        }

                        List<int> relatedCatIds = new List<int> { item.Category_ID.Value };
                        if (item.Category.ParentCateogry_ID == null && item.Category.Categories1 != null)
                        {
                            foreach (var subCat in item.Category.Categories1)
                            {
                                relatedCatIds.Add(subCat.ID);
                            }
                        }

                        var catLectures = dbEntity.Lectures
                            .Where(l => relatedCatIds.Contains(l.Category_ID))
                            .ToList();

                        var finishedLectureIds = dbEntity.std_lecture
                            .Where(e => e.Std_ID == CurrentUser.ID && e.IsFinish == 1 && relatedCatIds.Contains(e.Lecture.Category_ID))
                            .Select(e => e.Lecture_ID)
                            .ToList();

                        foreach (var lec in catLectures)
                        {
                            LectureModel lm = new LectureModel();
                            lm.ID = lec.ID;
                            lm.Lecture_Name = !string.IsNullOrWhiteSpace(lec.Lecture_Name) ? lec.Lecture_Name.Trim() : "محاضرة بدون عنوان";
                            lm.Category_ID = lec.Category_ID;
                            lm.CategoresName = lec.Category != null ? lec.Category.NameAr : ctm.CategoryName;
                            lm.DayandTime = lec.DayandTime;
                            lm.LectureTypeOnOff = lec.LectureTypeOnOff;
                            lm.LectureLink_Client = lec.LectureLink_Client;
                            lm.LectureLink_Host = lec.LectureLink_Host;
                            lm.MediaLink = lec.MediaLink;
                            lm.DocumenFile = lec.DocumenFile;
                            int minTime = 0; int.TryParse(lec.LectureMinTime, out minTime); lm.LectureMinTime = minTime;
                            lm.Is_Std_Finish_Lecture = finishedLectureIds.Contains(lec.ID) ? 1 : 0;
                            ctm.Lectures.Add(lm);
                        }

                        L_ctm.Add(ctm);
                    }
                }
                else
                {
                    return RedirectToAction("LogIn", "Home");
                }
            }
            catch (Exception ex)
            {
                Printerror("Categories", ex.Message, ex.InnerException.Message, ex.StackTrace);
            }
            return View(L_ctm);
        }
        [HttpGet]
        public ActionResult Courses(int Deplom_ID, string LectureName = null)
        {
            try
            {
                var deploma = dbEntity.Categories.Select(e => new { e.ID, e.ParentCateogry_ID, e.Icon }).Where(e => e.ParentCateogry_ID == Deplom_ID).ToList();
                if (deploma != null && deploma.Count > 0)
                {
                    return RedirectToAction("ShowSupDeploamCourse", new { cat_ID = Deplom_ID, CategoryName = LectureName });
                }
                else
                {
                    return RedirectToAction("LecturesStd", new { cat_ID = Deplom_ID, lectureName = LectureName });
                }
            }
            catch (Exception ex)
            {
                return View();
            }

        }
        [HttpGet]
        public ActionResult ShowSupDeploamCourse(int cat_ID, string CategoryName = null)
        {
            List<Std_Reg_CategoryModel> catModel = new List<Std_Reg_CategoryModel>();
            try
            {
                var category = dbEntity.Categories.Find(cat_ID).DocumentBook;
                if (category != null)
                {
                    ViewBag.bookDocument = category;
                }

                var deploma = dbEntity.Categories.Where(e => e.ParentCateogry_ID == cat_ID).ToList();

                if (deploma != null && deploma.Count > 0)
                {
                    foreach (var item in deploma)
                    {
                        var cat = new Std_Reg_CategoryModel();
                        cat.ID = item.ID;
                        cat.Category_ID = item.ID;
                        cat.category_Ispayed = item.IsPayed.Value;
                        cat.CategoryName = item.NameAr;
                        cat.PayMent_Value = item.PayedValue;
                        cat.Exam_ID = (item.Exams.Count > 0) ? item.Exams.FirstOrDefault().ID : 0;
                        cat.lectureCount = item.Lectures.Count;
                        cat.DocumentBook = item.DocumentBook;

                        foreach (var itemLectur in item.Lectures)
                        {
                            var value = dbEntity.std_lecture.Select(e => new { e.ID, e.IsFinish, e.Std_ID, e.Lecture_ID }).Where(e => e.IsFinish == 1 && e.Std_ID == CurrentUser.ID && e.Lecture_ID == itemLectur.ID).ToList();
                            foreach (var item2 in value)
                            {
                                cat.lectureFinishCount += 1;
                            }
                        }
                        catModel.Add(cat);
                    }
                }
            }
            catch (Exception ex)
            {

            }
            ViewBag.CategoryName = CategoryName;

            return View(catModel);
        }

        #endregion

        #region Lecture
        [HttpGet]
        public ActionResult LecturesStd(int cat_ID, string lectureName)
        {
            ViewBag.LectureName = lectureName;
            ViewBag.cat_id = cat_ID;

            List<LectureModel> L_M = new List<LectureModel>();
            try
            {
                var std = CurrentUser.ID;
                ViewBag.BookDocument = dbEntity.Categories.Find(cat_ID).DocumentBook;
                var value = dbEntity.Lectures.Where(e => e.Category_ID == cat_ID).ToList();
                var std_lect = dbEntity.std_lecture.Where(e => e.IsFinish == 1 && e.Std_ID == std).ToList();
                foreach (var item in value)
                {
                    LectureModel lm = new LectureModel();
                    lm.ID = item.ID;
                    lm.LectureLink_Client = item.LectureLink_Client;
                    lm.Lecture_Name = item.Lecture_Name;
                    lm.LectureTypeOnOff = item.LectureTypeOnOff;
                    lm.MediaLink = item.MediaLink;
                    lm.DayandTime = item.DayandTime;
                    lm.DocumenFile = item.DocumenFile;
                    lm.Others = item.Others;
                    lm.CategoresName = item.Category.NameEn;

                    int mini;
                    int.TryParse(item.LectureMinTime, out mini);
                    if (mini != 0)
                    {
                        lm.LectureMinTime = mini;
                    }


                    foreach (var item2 in std_lect)
                    {
                        if (lm.ID == item2.Lecture_ID)
                        {
                            lm.Is_Std_Finish_Lecture = 1;
                            break;
                        }
                        else
                        {
                            lm.Is_Std_Finish_Lecture = 0;
                        }
                    }

                    if (item.Assignments != null && item.Assignments.Count > 0)
                    {
                        lm.Assingment = item.Assignments.FirstOrDefault().Name;
                        lm.Assingn_ID = item.Assignments.FirstOrDefault().ID;

                    }
                    else
                    {
                        lm.Assingn_ID = 0;
                        //lm.Is_Std_Finish_Lecture = 0;
                    }
                    if (lm.LectureTypeOnOff == "0")
                    {
                        L_M.Add(lm);
                    }
                }


            }
            catch (Exception ex)
            {

                return RedirectToAction("LogIn", "Home");

            }
            return View(L_M);
        }


        [HttpPost]
        public JsonResult LecutreTimeEnd(int Lect_ID)
        {
            try
            {
                var stdID = CurrentUser.ID;
                if (stdID != null)
                {
                    var lec = dbEntity.std_lecture.Where(e => e.Lecture_ID == Lect_ID && e.Std_ID == stdID).FirstOrDefault();
                    if (lec != null)
                    {
                        lec.IsTimeEnd = 1;
                        var value = lec.Lecture.Assignments.FirstOrDefault();
                        if (value != null)
                        {
                            if (lec.IsAssingmetFinish == 1)
                            {
                                lec.IsFinish = 1;
                            }
                        }
                        else
                        {
                            lec.IsFinish = 1;
                        }
                        dbEntity.std_lecture.Attach(lec);
                        dbEntity.Entry(lec).State = System.Data.Entity.EntityState.Modified;
                        dbEntity.SaveChanges();

                    }
                    else
                    {
                        std_lecture lecture = new std_lecture();
                        lecture.Std_ID = stdID;
                        lecture.IsTimeEnd = 1;
                        lecture.Lecture_ID = Lect_ID;
                        dbEntity.std_lecture.Add(lecture);
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
        //[HttpPost]
        //public JsonResult StdLectureEnd(List<int> Lcat_ID)
        //{
        //    List<LectureModel> LM = new List<LectureModel>();
        //    List<int> L_stdlec = new List<int>();
        //    try
        //    {
        //        var std = GetCurrentUser().ID;
        //        var lectureCount = dbEntity.Lectures.Select(e => new { e.Category_ID, e.ID }).Where(e => e.Category_ID == cat_ID).ToList();
        //        foreach (var item in lectureCount)
        //        {
        //            LectureModel lm = new LectureModel();
        //            lm.ID = item.ID;
        //            lm.Category_ID = item.Category_ID;
        //            LM.Add(lm);
        //        }

        //        foreach (var item in lectureCount)
        //        {

        //            var std_lect = dbEntity.std_lecture.Select(e => new { e.ID, e.IsFinish, e.Std_ID, e.Lecture_ID }).Where(e => e.IsFinish == 1 && e.Std_ID == std && e.Lecture_ID == item.ID).FirstOrDefault();
        //            if (std_lect != null)
        //            {
        //                L_stdlec.Add(1);
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {

        //    }
        //    Response.StatusCode = (int)HttpStatusCode.OK;
        //    return Json(new { lectureCount = LM.Count, LectureFinish = L_stdlec.Count() }, JsonRequestBehavior.AllowGet);
        //}
        #endregion

        #region OnlinCourse
        [HttpGet]
        public ActionResult StdonlineCourse()
        {
            List<LectureModel> L_M = new List<LectureModel>();
            try
            {
                if (CurrentUser == null)
                {
                    return RedirectToAction("LogIn", "Home");
                }

                string studentId = CurrentUser.ID;
                var registeredCategoryIds = dbEntity.Std_Reg_Category
                    .Where(e => e.Std_ID == studentId)
                    .Select(e => e.Category_ID)
                    .Distinct()
                    .ToList();

                if (registeredCategoryIds == null || registeredCategoryIds.Count == 0)
                {
                    return View(L_M);
                }

                var liveLectures = dbEntity.Lectures
                    .Where(l => registeredCategoryIds.Contains(l.Category_ID) && l.LectureTypeOnOff == "1")
                    .ToList();

                // Ensure each Lecture.ID appears only once in the final list
                var uniqueLectures = liveLectures
                    .GroupBy(l => l.ID)
                    .Select(g => g.First())
                    .ToList();

                string[] knownFormats = new string[]
                {
                    "yyyy-MM-dd HH:mm:ss",
                    "yyyy-MM-ddTHH:mm:ss",
                    "yyyy-MM-ddTHH:mm",
                    "yyyy-MM-dd HH:mm",
                    "yyyy-MM-dd",
                    "MM/dd/yyyy HH:mm:ss",
                    "MM/dd/yyyy HH:mm",
                    "MM/dd/yyyy h:mm tt",
                    "MM/dd/yyyy h:mm",
                    "MM/dd/yyyy",
                    "dd/MM/yyyy HH:mm:ss",
                    "dd/MM/yyyy HH:mm"
                };

                DateTime now = DateTime.Now;

                var parsedList = new List<Tuple<LectureModel, bool, DateTime>>();

                foreach (var lec in uniqueLectures)
                {
                    LectureModel lm = new LectureModel();
                    lm.ID = lec.ID;
                    lm.Category_ID = lec.Category_ID;
                    lm.LectureLink_Client = lec.LectureLink_Client;
                    lm.Lecture_Name = lec.Lecture_Name;
                    lm.CategoresName = (lec.Category != null && !string.IsNullOrEmpty(lec.Category.NameAr)) ? lec.Category.NameAr : "";
                    lm.LectureTypeOnOff = lec.LectureTypeOnOff;
                    lm.MediaLink = lec.MediaLink;
                    lm.DocumenFile = lec.DocumenFile;
                    lm.Others = lec.Others;

                    int minTime = 0;
                    if (int.TryParse(lec.LectureMinTime, out minTime))
                    {
                        lm.LectureMinTime = minTime;
                    }

                    bool hasValidDate = false;
                    DateTime dt = DateTime.MinValue;

                    if (!string.IsNullOrWhiteSpace(lec.DayandTime))
                    {
                        string trimmed = lec.DayandTime.Trim();
                        if (DateTime.TryParseExact(trimmed, knownFormats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out dt))
                        {
                            hasValidDate = true;
                        }
                        else if (DateTime.TryParse(trimmed, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out dt))
                        {
                            hasValidDate = true;
                        }
                    }

                    if (hasValidDate)
                    {
                        lm.DayandTime = dt.ToString("yyyy-MM-dd HH:mm");
                        // Preserve approximate 2-hour window to identify ended live lecture
                        lm.Is_Lecture_End = dt.AddHours(2) < now;
                    }
                    else
                    {
                        lm.DayandTime = "موعد غير محدد";
                        lm.Is_Lecture_End = true;
                    }

                    parsedList.Add(Tuple.Create(lm, hasValidDate, dt));
                }

                // 1) Upcoming / ongoing lectures first, sorted by start time ascending
                var upcoming = parsedList
                    .Where(x => x.Item2 && !x.Item1.Is_Lecture_End)
                    .OrderBy(x => x.Item3)
                    .Select(x => x.Item1);

                // 2) Previous lectures afterward, sorted by start time descending (most recent first)
                var previous = parsedList
                    .Where(x => x.Item2 && x.Item1.Is_Lecture_End)
                    .OrderByDescending(x => x.Item3)
                    .Select(x => x.Item1);

                // 3) Lectures without a valid date last
                var noDate = parsedList
                    .Where(x => !x.Item2)
                    .Select(x => x.Item1);

                L_M = upcoming.Concat(previous).Concat(noDate).ToList();
            }
            catch (Exception ex)
            {
                Printerror("StdonlineCourse", ex.Message, ex.InnerException != null ? ex.InnerException.Message : null, ex.StackTrace);
                ViewBag.ErrorMessage = "حدث خطأ أثناء تحميل جدول المحاضرات المباشرة. يرجى المحاولة لاحقًا.";
            }

            return View(L_M);
        }

        [HttpGet]
        public ActionResult Schedule()
        {
            return RedirectToAction("StdonlineCourse");
        }

        [HttpGet]
        public ActionResult Subjects()
        {
            return RedirectToAction("CategoriesStd");
        }

        [HttpGet]
        public ActionResult StdSubjects()
        {
            return RedirectToAction("CategoriesStd");
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
        #endregion

        #region Assignment
        [HttpGet]
        public ActionResult Assignment()
        {
            return View();
        }
        [HttpPost]
        public JsonResult GetAssignment()
        {
            AssignmentModel em = new AssignmentModel();
            try
            {
                var value = dbEntity.Assignments.ToList();

            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = "" }, JsonRequestBehavior.AllowGet);
            }
            Response.StatusCode = (int)HttpStatusCode.OK;
            return Json(em, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult AssignmentQuestion(string A_Q)
        {
            ViewBag.assingID = A_Q;
            int assid = int.Parse(A_Q);
            var assingment = dbEntity.Assignments.Find(assid);
            if (assingment != null)
            {
                ViewBag.categoryAssID = assingment.Lecture.Category_ID;
                ViewBag.categoryAssName = assingment.Lecture.Category.NameAr;
            }
            return View();
        }
        [HttpPost]
        public async Task<JsonResult> GetAssignmentQuestion(int A_Q)
        {
            List<QuestionsModel> L_Q = new List<QuestionsModel>();
            try
            {
                var assingment = await dbEntity.Assignments.FindAsync(A_Q);
                if (assingment != null)
                {

                    TempData["Lecture_ID"] = assingment.Lecture_ID;
                    var value = assingment.Assignment_Q.ToList();
                    if (value != null)
                    {
                        foreach (var item in value)
                        {
                            QuestionsModel QM = new QuestionsModel();
                            QM.ID = item.ID;
                            //qm.Q_Description = q.Q_Description;
                            QM.Q_Name = item.Question.Q_Name;
                            QM.Q_Text = item.Question.Q_Text;
                            QM.Type = item.Question.Type;
                            if (item.Question.Type == 1)
                            {
                                foreach (var item2 in item.Question.Q_TrueOrFolse)
                                {
                                    QM.Q_TrueOrFalse = new Q_TrueOrFolseModel();
                                    QM.Q_TrueOrFalse.Degry = item2.Degry;
                                    QM.Q_TrueOrFalse.ID = item2.ID;
                                    QM.Q_TrueOrFalse.Question_ID = item2.Question_ID;
                                    QM.Q_TrueOrFalse.TrueValue = item2.TrueValue;
                                }
                            }
                            else
                                if (item.Question.Type == 2)
                            {
                                QM.Q_MultiChoice = new List<Q_MultiChoiceModel>();
                                foreach (var item2 in item.Question.Q_MultiChoice)
                                {
                                    QM.Q_MultiChoice.Add(new Q_MultiChoiceModel
                                    {
                                        ID = item2.ID,
                                        Choose = item2.Choose,
                                        Degry = item2.Degry,
                                        Question_ID = item2.Question_ID
                                    });
                                }
                            }
                            else if (item.Question.Type == 3)
                            {
                                QM.Q_MatchingChoise = new List<Q_MatchingModel>();
                                foreach (var item2 in item.Question.Q_Matching)
                                {
                                    QM.Q_MatchingChoise.Add(new Q_MatchingModel
                                    {
                                        ID = item2.ID,
                                        ChoiceText = item2.ChoiceText,
                                        ChoiseTrueAnswer = item2.ChoiseTrueAnswer,
                                        Degry = item2.Degry,
                                        Question_ID = item2.Question_ID
                                    });
                                }
                            }
                            L_Q.Add(QM);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = "" }, JsonRequestBehavior.AllowGet);
            }
            Response.StatusCode = (int)HttpStatusCode.OK;
            return Json(L_Q, JsonRequestBehavior.AllowGet);
        }


        [HttpPost]
        public async Task<JsonResult> SubmetQuestion(List<string> L_Degry, int ass_ID)
        {
            try
            {
                var stdID = CurrentUser.ID;
                float degry = 0;
                int lectureID = int.Parse(TempData["Lecture_ID"].ToString());
                if (L_Degry != null)
                {
                    for (int i = 0; i < L_Degry.Count; i++)
                    {
                        int resu = 0;
                        int.TryParse(L_Degry[i], out resu);
                        degry += resu;
                    }
                }
                var ass = await dbEntity.Assignments.FindAsync(ass_ID);
                if (ass.Assignmet_degry_Win <= degry)
                {
                    var lec = dbEntity.std_lecture.Where(e => e.Lecture_ID == lectureID && e.Std_ID == stdID).FirstOrDefault();
                    if (lec != null)
                    {
                        lec.IsAssingmetFinish = 1;
                        if (lec.IsTimeEnd == 1)
                        {
                            lec.IsFinish = 1;
                        }
                        dbEntity.std_lecture.Attach(lec);
                        dbEntity.Entry(lec).State = System.Data.Entity.EntityState.Modified;
                        dbEntity.SaveChanges();

                    }
                    else
                    {
                        std_lecture lecture = new std_lecture();
                        lecture.Std_ID = stdID;
                        lecture.IsAssingmetFinish = 1;
                        lecture.IsTimeEnd = 1;
                        lecture.Lecture_ID = lectureID;
                        dbEntity.std_lecture.Add(lecture);
                        dbEntity.SaveChanges();
                    }

                    Response.StatusCode = (int)HttpStatusCode.OK;
                    return Json(new { success = true, Assdegry = ass.Assignmet_Degry, stdDegry = degry }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    Response.StatusCode = (int)HttpStatusCode.OK;
                    return Json(new { success = false, Assdegry = ass.Assignmet_Degry, stdDegry = degry }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = ex.Message }, JsonRequestBehavior.AllowGet);
            }

        }
        #endregion

        #region Std_Exams
        [HttpGet]
        public ActionResult Exams(int examID)
        {
            ViewBag.examID = examID;
            ViewBag.examTime = dbEntity.Exams.Find(examID).Duration;
            return View();
        }
        [HttpPost]
        public async Task<JsonResult> StdExams(int Exam_ID, int category_ID)
        {
            ExamModel em = new ExamModel();
            try
            {
                string std_ID = getCurrentUser_ID();
                if (std_ID != null)
                {
                    var category = dbEntity.Categories.Find(category_ID);

                    if (category != null && category.ParentCateogry_ID != 0)
                    {
                        int lecutrCount = category.Lectures.Count();
                        int stdLectureFinish = 0;
                        foreach (var item in category.Lectures)
                        {
                            stdLectureFinish += dbEntity.std_lecture.Where(e => e.Std_ID == std_ID && e.Lecture_ID == item.ID && e.IsFinish == 1).Count();
                        }
                        var value = await dbEntity.Exams.FindAsync(Exam_ID);
                        if (value != null)
                        {
                            em.ID = value.ID;
                            em.Description = value.Description;
                            em.Duration = (float)value.Duration;
                            em.Exam_Degry = (float)value.Exam_Degry;
                            em.Exam_win_Degry = (float)value.Exam_win_Degry;
                            em.Category_ID = (int)value.Category_ID;
                            em.Name = value.Name;
                            if (stdLectureFinish >= lecutrCount && stdLectureFinish != 0)
                            {
                                em.CanStartExam = 1;
                            }
                            else
                            {
                                em.CanStartExam = 0;
                            }
                        }
                    }
                    else if (category != null && category.ParentCateogry_ID == null)
                    {
                        var categorys = dbEntity.Categories.Select(e => new { e.ID, e.ParentCateogry_ID }).Where(e => e.ParentCateogry_ID == category_ID).ToList();
                        var stdexamFinish = dbEntity.Std_ExamFinish.Select(e => new { e.Std_ID, e.isFinishExam, e.std_Req_category_ID }).Where(e => e.Std_ID.ToString() == std_ID).ToList();
                        foreach (var item in categorys)
                        {
                            em.CanStartExam = 0;
                            foreach (var itemexam in stdexamFinish)
                            {
                                if (itemexam.std_Req_category_ID == item.ID)
                                {
                                    if (itemexam.isFinishExam == 1)
                                    {
                                        em.CanStartExam = 1;
                                        break;
                                    }
                                    if (itemexam.isFinishExam == 0)
                                    {
                                        em.CanStartExam = 0;
                                        break;
                                    }


                                }


                            }
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                Printerror("StdExams", ex.Message, null, ex.StackTrace);
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = "" }, JsonRequestBehavior.AllowGet);
            }

            Response.StatusCode = (int)HttpStatusCode.OK;
            return Json(em, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public async Task<JsonResult> GetExamQuestion(int exam_ID)
        {
            List<QuestionsModel> L_Q = new List<QuestionsModel>();
            try
            {
                var exams = await dbEntity.Exams.FindAsync(exam_ID);

                if (exams != null)
                {
                    var value = exams.Exam_Q.ToList();

                    if (value != null)
                    {
                        foreach (var item in value)
                        {
                            QuestionsModel QM = new QuestionsModel();
                            QM.ID = item.ID;
                            //qm.Q_Description = q.Q_Description;
                            QM.Q_Name = item.Question.Q_Name;
                            QM.Q_Text = item.Question.Q_Text;
                            QM.Type = item.Question.Type;
                            if (item.Question.Type == 1)
                            {
                                foreach (var item2 in item.Question.Q_TrueOrFolse)
                                {
                                    QM.Q_TrueOrFalse = new Q_TrueOrFolseModel();
                                    QM.Q_TrueOrFalse.Degry = item2.Degry;
                                    QM.Q_TrueOrFalse.ID = item2.ID;
                                    QM.Q_TrueOrFalse.Question_ID = item2.Question_ID;
                                    QM.Q_TrueOrFalse.TrueValue = item2.TrueValue;
                                }
                            }
                            else
                                if (item.Question.Type == 2)
                            {
                                QM.Q_MultiChoice = new List<Q_MultiChoiceModel>();
                                foreach (var item2 in item.Question.Q_MultiChoice)
                                {
                                    QM.Q_MultiChoice.Add(new Q_MultiChoiceModel
                                    {
                                        ID = item2.ID,
                                        Choose = item2.Choose,
                                        Degry = item2.Degry,
                                        Question_ID = item2.Question_ID
                                    });
                                }
                            }
                            else
                                if (item.Question.Type == 3)
                            {
                                QM.Q_MatchingChoise = new List<Q_MatchingModel>();
                                foreach (var item2 in item.Question.Q_Matching)
                                {
                                    QM.Q_MatchingChoise.Add(new Q_MatchingModel
                                    {
                                        ID = item2.ID,
                                        ChoiceText = item2.ChoiceText,
                                        Degry = item2.Degry,
                                        ChoiseTrueAnswer = item2.ChoiseTrueAnswer,
                                        Question_ID = item2.Question_ID
                                    });
                                }
                            }
                            L_Q.Add(QM);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = "" }, JsonRequestBehavior.AllowGet);
            }
            Response.StatusCode = (int)HttpStatusCode.OK;
            return Json(L_Q, JsonRequestBehavior.AllowGet);
        }

        private static int certficationContent { get; set; } = 30000;
        [HttpPost]
        public async Task<JsonResult> SubmetExamQuestion(List<string> L_Degry, int Exam_ID)
        {
            try
            {
                var stdID = CurrentUser.ID;
                float Stddegry = 0;
                if (L_Degry != null)
                {
                    for (int i = 0; i < L_Degry.Count; i++)
                    {
                        Stddegry += int.Parse(L_Degry[i]);
                    }
                }
                var exa = await dbEntity.Exams.FindAsync(Exam_ID);
                var ExamDegry = exa.Exam_Degry;
                var res = exa.Exam_win_Degry;
                if (exa.Exam_win_Degry <= Stddegry)
                {

                    var values = dbEntity.Std_Reg_Category.Where(e => e.Std_ID == stdID && e.Category_ID == exa.Category_ID).FirstOrDefault();
                    values.std_exam_isFinish = 1;
                    string datetime = ConvertDateCalendar(DateTime.Now.Date, "Hijri", "en-US");
                    List<string> date = SpliteString(datetime, ' ');
                    values.std_exam_Finish_Time = date[0] + ":" + DateTime.Now.Date.ToString("yyyy-MM-dd");
                    float stdPersentage = ((float)((100 * Stddegry) / ExamDegry));
                    float examPersentage = ((float)((ExamDegry * 100) / stdPersentage));
                    if (ExamDegry == Stddegry)
                    {
                        values.Certificate_appreciation = "ممتاز";
                    }
                    else
                        if (examPersentage >= 80)
                    {
                        values.Certificate_appreciation = "جيد جدا";
                    }
                    else
                    if (examPersentage >= 60)
                    {
                        values.Certificate_appreciation = "جيد";
                    }
                    else if (examPersentage >= 50)
                    {
                        values.Certificate_appreciation = "مقبول";
                    }
                    certficationContent = certficationContent + 1;
                    values.Serial_Number = certficationContent.ToString();
                    dbEntity.Entry(values).State = System.Data.Entity.EntityState.Modified;
                    dbEntity.SaveChanges();
                    Response.StatusCode = (int)HttpStatusCode.OK;
                    return Json(new { success = true, Assdegry = ExamDegry, WinDegry = Stddegry }, JsonRequestBehavior.AllowGet);
                }
                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(new { success = false, Assdegry = ExamDegry, WinDegry = Stddegry }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = ex.Message }, JsonRequestBehavior.AllowGet);
            }

        }
        #endregion

        #region Certfication

        [HttpGet]
        public ActionResult Certficationsstd(int Categores_ID, int type)
        {
            try
            {
                ViewBag.stdName = CurrentUser.RealName;
                ViewBag.categoryID = Categores_ID;
                var stdID = CurrentUser.ID;
                var result = dbEntity.Categories.Find(Categores_ID);
                var stdinfo = dbEntity.Std_Reg_Category.Where(e => e.Std_ID == stdID && e.Category_ID == Categores_ID).FirstOrDefault();
                int stdcode = int.Parse(stdinfo.Serial_Number) + 1;
                if (type == 1)
                {
                    ViewBag.Certification = result.CertificationAR;



                    string imageFilePath = Server.MapPath($"~/Uploade/Images/" + result.CertificationAR);
                    Image bitmap = (System.Drawing.Image)Bitmap.FromFile(imageFilePath); // set image     

                    Graphics graphicsImage = Graphics.FromImage(bitmap);

                    ////Set the alignment based on the coordinates      
                    ///Name
                    StringFormat stringformat = new StringFormat();
                    stringformat.Alignment = StringAlignment.Center;
                    stringformat.LineAlignment = StringAlignment.Center;



                    ////Set the font color/format/size etc..      
                    Color StringColor = System.Drawing.ColorTranslator.FromHtml("#927123");//direct color adding    

                    string Str_TextOnImage = ViewBag.stdName;//Your Text On Image    


                    graphicsImage.DrawString(Str_TextOnImage, new Font("Andalus", 42,
                    FontStyle.Regular), new SolidBrush(StringColor), new Point(850, 580),
                    stringformat);
                    ////
                    ///
                    /////تقدير
                    Color NameColor = System.Drawing.ColorTranslator.FromHtml("#927123");//direct color adding    
                    string stdreqular = stdinfo.Certificate_appreciation;//Your Text On Image    
                    graphicsImage.DrawString(stdreqular, new Font("Andalus", 32,
                    FontStyle.Regular), new SolidBrush(NameColor), new Point(710, 810),
                    stringformat);
                    ///////////////
                    ///
                    ///Date
                    Color TimeColor = System.Drawing.ColorTranslator.FromHtml("#000000");//direct color adding    
                    //string stdTime = stdinfo.std_exam_Finish_Time;//Your Text On Image    
                    List<string> ListDatetime = SpliteString(stdinfo.std_exam_Finish_Time, ':');//Your Text On Image  
                    graphicsImage.DrawString(ListDatetime[0], new Font("Andalus", 16,
                    FontStyle.Regular), new SolidBrush(TimeColor), new Point(950, 950),
                    stringformat);
                    graphicsImage.DrawString(ListDatetime[1], new Font("Andalus", 16,
                    FontStyle.Regular), new SolidBrush(TimeColor), new Point(650, 950),
                    stringformat);
                    graphicsImage.DrawString(stdcode.ToString(), new Font("Andalus", 14,
                   FontStyle.Regular), new SolidBrush(TimeColor), new Point(1450, 1190),
                   stringformat);
                    using (MemoryStream mem = new MemoryStream())
                    {
                        bitmap.Save(mem, ImageFormat.Png);
                        mem.Position = 0;
                        return File(mem.ToArray(), "image/png", "Certfication.jpeg");
                    }
                }
                else
                {
                    ViewBag.Certification = result.CertificationEN;
                    string imageFilePath = Server.MapPath($"~/Uploade/Images/" + result.CertificationEN);
                    Image bitmap = (System.Drawing.Image)Bitmap.FromFile(imageFilePath); // set image     
                    Graphics graphicsImage = Graphics.FromImage(bitmap);
                    ////Set the alignment based on the coordinates      
                    ///Name
                    StringFormat stringformat = new StringFormat();
                    stringformat.Alignment = StringAlignment.Center;
                    stringformat.LineAlignment = StringAlignment.Center;
                    ////Set the font color/format/size etc..      
                    Color StringColor = System.Drawing.ColorTranslator.FromHtml("#927123");//direct color adding    
                    string Str_TextOnImage = ViewBag.stdName;//Your Text On Image    
                    graphicsImage.DrawString(Str_TextOnImage, new Font("Andalus", 42,
                    FontStyle.Regular), new SolidBrush(StringColor), new Point(630, 610),
                    stringformat);
                    Color NameColor = System.Drawing.ColorTranslator.FromHtml("#000000");//direct color adding    
                    List<string> ListDatetime = SpliteString(stdinfo.std_exam_Finish_Time, ':');//Your Text On Image   
                    graphicsImage.DrawString(ListDatetime[0], new Font("Andalus", 12,
                    FontStyle.Regular), new SolidBrush(NameColor), new Point(690, 1660),
                    stringformat);
                    graphicsImage.DrawString(ListDatetime[1], new Font("Andalus", 12,
                 FontStyle.Regular), new SolidBrush(NameColor), new Point(440, 1660),
                 stringformat);
                    graphicsImage.DrawString((stdinfo.Serial_Number), new Font("Andalus", 12,
                FontStyle.Regular), new SolidBrush(NameColor), new Point(630, 1710),
                stringformat);
                    using (MemoryStream mem = new MemoryStream())
                    {
                        bitmap.Save(mem, ImageFormat.Png);
                        mem.Position = 0;
                        return File(mem.ToArray(), "image/png", "Certfication.jpeg");
                    }
                }
                //return View();
            }
            catch (Exception ex)
            {

            }

            return File(Response.OutputStream, "image/jpeg", "Certfication");
        }

        [HttpGet]
        public ActionResult Certfications()
        {
            List<Std_Reg_CategoryModel> categories = new List<Std_Reg_CategoryModel>();
            try
            {
                if (CurrentUser != null)
                {
                    var values2 = dbEntity.Std_Reg_Category.Where(e => e.Std_ID == CurrentUser.ID && e.std_exam_isFinish == 1).Select(e => new { e.Std_ID, e.std_exam_isFinish, e.Category.CertificationAR, e.Category.CertificationEN, e.Category.NameAr, e.Category.ID }).ToList();
                    foreach (var item in values2)
                    {
                        Std_Reg_CategoryModel src = new Std_Reg_CategoryModel();
                        src.Std_ID = item.Std_ID;
                        src.std_exam_isFinish = item.std_exam_isFinish.Value;
                        src.CategoryName = item.NameAr;
                        src.Category_ID = item.ID;
                        if (item.CertificationAR != null)
                        {
                            src.certificate = item.CertificationAR;
                        }
                        if (item.CertificationEN != null)
                        {
                            src.certificate_leave = item.CertificationEN;
                        }
                        categories.Add(src);
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return View(categories);
        }

        #endregion

        #region Payment
        [HttpPost]
        public async Task<JsonResult> payforcategory(Std_Reg_CategoryModel src)
        {

            try
            {

                var value = dbEntity.Std_Reg_Category.Where(e => e.Category_ID == src.Category_ID && e.Std_ID == CurrentUser.ID).FirstOrDefault();
                if (value != null)
                {
                    var payMethod = src.PayMent_Method;
                    if (payMethod == 1)
                    {
                        //cridedcard
                        value.PayMent_Method = 1;
                    }
                    else if (payMethod == 2)
                    {
                        //pancktransfer
                        value.PayMent_Method = 2;
                        value.TransactionFile = SaveImage(src.FileUpload);
                    }
                    else if (payMethod == 3)
                    {
                        //leater
                        value.PayMent_Method = 3;
                    }
                    else if (payMethod == 4)
                    {
                        //code
                        value.PayMent_Method = 4;
                        value.Code = src.Code;
                    }
                    value.PayMent_Value = src.PayMent_Value;
                    dbEntity.Std_Reg_Category.Attach(value);
                    dbEntity.Entry(value).State = System.Data.Entity.EntityState.Modified;
                    var result = await dbEntity.SaveChangesAsync();
                    if (result == 1)
                    {
                        Response.StatusCode = (int)HttpStatusCode.OK;
                        return Json(new { success = true, response = "" }, JsonRequestBehavior.AllowGet);
                    }
                }
            }
            catch (Exception ex)
            {
                Printerror("payforcategory", ex.Message, null, ex.StackTrace);
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = "" }, JsonRequestBehavior.AllowGet);
            }
            Response.StatusCode = (int)HttpStatusCode.OK;
            return Json(new { success = false, response = "" }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region Chat
        [HttpGet]
        public ActionResult ChatPageStd()
        {


            if (CurrentUser != null)
            {
                ViewBag.stdname = CurrentUser.RealName;
                ViewBag.stdID = CurrentUser.ID;

            }
            else
            {
                return RedirectToAction("LogIn", "Home");
            }
            return View();
        }
        [HttpPost]
        public JsonResult GetAllStdChat(string CourseName = null)
        {
            List<Std_Reg_CategoryModel> L_ctm = new List<Std_Reg_CategoryModel>();
            try
            {


                if (CurrentUser != null)
                {
                    ViewBag.stdname = CurrentUser.RealName;
                    ViewBag.stdID = CurrentUser.ID;

                    if (CourseName == null)
                    {
                        var values = dbEntity.Std_Reg_Category.Where(e => e.Std_ID == CurrentUser.ID)
                               .Select(x => new { x.Category_ID, x.Category.NameAr, });
                        foreach (var item in values)
                        {
                            Std_Reg_CategoryModel ctm = new Std_Reg_CategoryModel();

                            ctm.CategoryName = item.NameAr;
                            ctm.Category_ID = item.Category_ID.Value;
                            L_ctm.Add(ctm);
                        }
                    }
                    else
                    {
                        var values = dbEntity.Std_Reg_Category.Where(e => e.Std_ID == CurrentUser.ID && e.Category.NameAr.Contains(CourseName))
                                .Select(x => new { x.Category_ID, x.Category.NameAr, });
                        foreach (var item in values)
                        {
                            Std_Reg_CategoryModel ctm = new Std_Reg_CategoryModel();

                            ctm.CategoryName = item.NameAr;
                            ctm.Category_ID = item.Category_ID.Value;
                            L_ctm.Add(ctm);
                        }
                    }


                }
            }
            catch (Exception ex)
            {
            }
            Response.StatusCode = (int)HttpStatusCode.OK;
            return Json(L_ctm, JsonRequestBehavior.AllowGet);
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

    //public class QuestionSubmet
    //{
    //    public int Q_ID { get; set; }
    //    public int Q_Degry { get; set; }
    //}
}