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
using tabseetCourse.Services;

namespace tabseetCourse.Controllers
{
    public class StudentsController : SharedCalss
    {
        // GET: Students
        public HomePageModel HomepageViewModel { get; set; }

        private StudentAttendanceService _attendance;
        /// <summary>خدمة احتساب الحضور بوقت التشغيل الفعلي (Services/StudentAttendanceService.cs).</summary>
        private StudentAttendanceService Attendance
        {
            get { return _attendance ?? (_attendance = new StudentAttendanceService(dbEntity)); }
        }

        private CourseAccessService _access;
        /// <summary>الآلية الموحدة للتحقق من صلاحية الطالب في الوصول إلى دورة (Services/CourseAccessService.cs).</summary>
        private CourseAccessService Access
        {
            get { return _access ?? (_access = new CourseAccessService(dbEntity)); }
        }

        #region Course access guards (حماية المحتوى المحمي على مستوى الخادم)
        /// <summary>
        /// تحقق لصفحات Razor: يعيد true عند السماح (اشتراك مفعّل)، وإلا يملأ denial بإعادة توجيه مناسبة
        /// (تسجيل الدخول لغير المسجل دخوله، أو صفحة «برامجي التعليمية» مع رسالة توضح الحالة).
        /// </summary>
        private bool TryAuthorizeCourse(int? categoryId, out ActionResult denial)
        {
            denial = null;
            if (CurrentUser == null)
            {
                denial = RedirectToAction("LogIn", "Home");
                return false;
            }

            Category category = categoryId.HasValue ? dbEntity.Categories.Find(categoryId.Value) : null;
            var state = Access.GetState(CurrentUser.ID, category);
            if (category != null && state == SubscriptionState.Active)
                return true;

            TempData["SubscriptionMessage"] = AccessDeniedMessage(state, category != null ? category.NameAr : null);
            TempData["SubscriptionMessageType"] = state == SubscriptionState.PendingApproval ? "info" : "warning";
            denial = RedirectToAction("CategoriesStd");
            return false;
        }

        /// <summary>تحقق لنقاط JSON (الاختبارات، الواجبات...): يعيد true عند السماح، وإلا 403 دون أي بيانات محمية.</summary>
        private bool TryAuthorizeCourseJson(int? categoryId, out JsonResult error)
        {
            error = null;
            if (CurrentUser == null)
            {
                Response.StatusCode = (int)HttpStatusCode.Forbidden;
                error = Json(new { success = false, requiresLogin = true, response = "المستخدم غير مسجل الدخول" }, JsonRequestBehavior.AllowGet);
                return false;
            }

            Category category = categoryId.HasValue ? dbEntity.Categories.Find(categoryId.Value) : null;
            var state = Access.GetState(CurrentUser.ID, category);
            if (category != null && state == SubscriptionState.Active)
                return true;

            Response.StatusCode = (int)HttpStatusCode.Forbidden;
            error = Json(new { success = false, state = state.ToString(), response = AccessDeniedMessage(state, category != null ? category.NameAr : null) }, JsonRequestBehavior.AllowGet);
            return false;
        }

        private static string AccessDeniedMessage(SubscriptionState state, string courseName)
        {
            string name = string.IsNullOrWhiteSpace(courseName) ? "هذه الدورة" : "دورة «" + courseName.Trim() + "»";
            return state == SubscriptionState.PendingApproval
                ? "اشتراكك في " + name + " بانتظار موافقة الإدارة. ستتمكن من دخول الدورة بعد تأكيد الإدارة."
                : "يجب الاشتراك في " + name + " أولاً للوصول إلى محتواها.";
        }
        #endregion

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
                        ctm.category_Ispayed = item.category_Ispayed ?? 0;
                        ctm.std_category_Ispayed = item.std_category_Ispayed ?? 0;
                        // حالة الاشتراك تُحسب على الخادم: مجانية → مفعّلة، مدفوعة → حسب موافقة الإدارة (مع مراعاة الدبلوم الأم)
                        var subscriptionState = Access.GetState(CurrentUser.ID, item.Category);
                        ctm.SubscriptionState = subscriptionState.ToString();
                        ctm.IsSubscriptionActive = subscriptionState == SubscriptionState.Active;
                        ctm.IsSubscriptionPending = subscriptionState == SubscriptionState.PendingApproval;
                        ctm.lectureCount = 0;
                        // الكتاب وروابط المحاضرات تُرسل إلى المتصفح فقط عندما يكون الاشتراك مفعّلاً
                        ctm.DocumentBook = ctm.IsSubscriptionActive ? item.Category.DocumentBook : null;
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
                            if (ctm.IsSubscriptionActive)
                            {
                                lm.LectureLink_Client = lec.LectureLink_Client;
                                lm.LectureLink_Host = lec.LectureLink_Host;
                                lm.MediaLink = lec.MediaLink;
                                lm.DocumenFile = lec.DocumenFile;
                            }
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
            ActionResult denial;
            if (!TryAuthorizeCourse(Deplom_ID, out denial)) return denial;
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
            ActionResult denial;
            if (!TryAuthorizeCourse(cat_ID, out denial)) return denial;
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
                        // حالة اشتراك الطالب في الدورة الفرعية (قد تكون مدفوعة مستقلة ومعلّقة)
                        var childState = Access.GetState(CurrentUser.ID, item);
                        cat.SubscriptionState = childState.ToString();
                        cat.IsSubscriptionActive = childState == SubscriptionState.Active;
                        cat.IsSubscriptionPending = childState == SubscriptionState.PendingApproval;
                        if (!cat.IsSubscriptionActive)
                        {
                            cat.DocumentBook = null;
                        }

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

            // لا تُعرض محاضرات الدورة (ولا روابطها) إلا لاشتراك مفعّل، حتى عند كتابة الرابط مباشرة
            ActionResult denial;
            if (!TryAuthorizeCourse(cat_ID, out denial)) return denial;

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


        /// <summary>
        /// [للتوافق مع الصفحات المفتوحة قديماً فقط] أُلغي احتساب الحضور بالمؤقّت من المتصفح؛ الحضور يُحتسب الآن عبر
        /// StartLecture / LectureHeartbeat بوقت التشغيل الفعلي. تعيد هذه النقطة الحالة الحالية دون أي تعديل في قاعدة البيانات.
        /// </summary>
        [HttpPost]
        public JsonResult LecutreTimeEnd(int Lect_ID)
        {
            try
            {
                if (CurrentUser != null && !string.IsNullOrEmpty(CurrentUser.ID))
                {
                    var lecture = dbEntity.Lectures.Find(Lect_ID);
                    if (lecture != null && Attendance.IsStudentRegistered(CurrentUser.ID, lecture))
                    {
                        var status = Attendance.GetStatus(CurrentUser.ID, lecture);
                        Response.StatusCode = (int)HttpStatusCode.OK;
                        return Json(new { success = true, response = "", status = ToClientStatus(status) }, JsonRequestBehavior.AllowGet);
                    }
                }
            }
            catch (Exception)
            {
            }

            Response.StatusCode = (int)HttpStatusCode.OK;
            return Json(new { success = false, response = "" }, JsonRequestBehavior.AllowGet);
        }

        #region Attendance (احتساب الحضور بوقت التشغيل الفعلي)
        private static object ToClientStatus(AttendanceStatusModel s)
        {
            if (s == null) return null;
            return new
            {
                lectureId = s.LectureId,
                watchedSeconds = s.WatchedSeconds,
                requiredSeconds = s.RequiredSeconds,
                progressPercent = s.ProgressPercent,
                isTimeEnd = s.IsTimeEnd,
                hasAssignment = s.HasAssignment,
                isAssignmentFinish = s.IsAssignmentFinish,
                isFinish = s.IsFinish,
                source = s.Source
            };
        }

        /// <summary>تحقق موحد: طالب مسجل الدخول، المحاضرة موجودة، الطالب مسجل في دورتها، ونوعها يطابق العملية.</summary>
        private bool TryGetAttendanceLecture(int lectureId, string requiredType, out Lecture lecture, out string studentId, out JsonResult error)
        {
            lecture = null;
            studentId = CurrentUser != null ? CurrentUser.ID : null;
            error = null;

            if (string.IsNullOrEmpty(studentId))
            {
                Response.StatusCode = (int)HttpStatusCode.Forbidden;
                error = Json(new { success = false, response = "المستخدم غير مسجل الدخول" }, JsonRequestBehavior.AllowGet);
                return false;
            }

            lecture = dbEntity.Lectures.Find(lectureId);
            if (lecture == null)
            {
                Response.StatusCode = (int)HttpStatusCode.NotFound;
                error = Json(new { success = false, response = "المحاضرة غير موجودة" }, JsonRequestBehavior.AllowGet);
                return false;
            }

            if (!Attendance.IsStudentRegistered(studentId, lecture))
            {
                Response.StatusCode = (int)HttpStatusCode.Forbidden;
                error = Json(new { success = false, response = "أنت غير مسجل في دورة هذه المحاضرة" }, JsonRequestBehavior.AllowGet);
                return false;
            }

            if (requiredType != null && (lecture.LectureTypeOnOff ?? "0") != requiredType)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                error = Json(new { success = false, response = "نوع المحاضرة لا يطابق هذه العملية" }, JsonRequestBehavior.AllowGet);
                return false;
            }

            return true;
        }

        /// <summary>بدء (أو استئناف) مشاهدة محاضرة مسجلة؛ يُستدعى من المشغّل عند أول تشغيل ويعيد الثواني المطلوبة والمحتسبة.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult StartLecture(int lectureId, int? durationSeconds)
        {
            try
            {
                Lecture lecture; string studentId; JsonResult error;
                if (!TryGetAttendanceLecture(lectureId, "0", out lecture, out studentId, out error)) return error;

                var status = Attendance.StartRecorded(studentId, lecture, durationSeconds);
                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(new { success = true, status = ToClientStatus(status) }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>نبضة أثناء التشغيل: الثواني التي شُغّل فيها الفيديو فعلاً منذ النبضة السابقة (الخادم يقيّدها بالزمن الفعلي).</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult LectureHeartbeat(int lectureId, int playedSeconds, int? durationSeconds)
        {
            try
            {
                Lecture lecture; string studentId; JsonResult error;
                if (!TryGetAttendanceLecture(lectureId, "0", out lecture, out studentId, out error)) return error;

                var status = Attendance.Heartbeat(studentId, lecture, playedSeconds, durationSeconds);
                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(new { success = true, status = ToClientStatus(status) }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>تسجيل دخول محاضرة مباشرة عند الضغط على زر الدخول (يُقبل ضمن نافذة موعد المحاضرة فقط).</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult JoinLiveLecture(int lectureId)
        {
            try
            {
                Lecture lecture; string studentId; JsonResult error;
                if (!TryGetAttendanceLecture(lectureId, "1", out lecture, out studentId, out error)) return error;

                string joinError;
                var status = Attendance.JoinLive(studentId, lecture, DateTime.Now, out joinError);
                Response.StatusCode = (int)HttpStatusCode.OK;
                if (status == null)
                {
                    return Json(new { success = false, response = joinError }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { success = true, status = ToClientStatus(status) }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, response = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion
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
                // جدول الحصص يعرض محاضرات الاشتراكات المفعّلة فقط (المعلّقة بانتظار الإدارة لا تُرسل روابطها للمتصفح)
                var registeredCategoryIds = Access.GetAccessibleCategoryIds(studentId);

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
            int assid;
            if (!int.TryParse(A_Q, out assid))
            {
                return RedirectToAction("CategoriesStd");
            }
            var assingment = dbEntity.Assignments.Find(assid);
            ActionResult denial;
            if (!TryAuthorizeCourse(assingment != null && assingment.Lecture != null ? assingment.Lecture.Category_ID : (int?)null, out denial)) return denial;
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
                JsonResult accessError;
                if (!TryAuthorizeCourseJson(assingment != null && assingment.Lecture != null ? assingment.Lecture.Category_ID : (int?)null, out accessError)) return accessError;
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
                // التحقق من صلاحية الدورة أولاً (قبل أي قراءة قد تفشل) حتى يُرفض الاشتراك المعلّق بـ 403 واضحة
                var ass = await dbEntity.Assignments.FindAsync(ass_ID);
                JsonResult accessError;
                if (!TryAuthorizeCourseJson(ass != null && ass.Lecture != null ? ass.Lecture.Category_ID : (int?)null, out accessError)) return accessError;

                var stdID = CurrentUser.ID;
                float degry = 0;
                int lectureID;
                if (TempData["Lecture_ID"] == null || !int.TryParse(TempData["Lecture_ID"].ToString(), out lectureID))
                {
                    lectureID = ass.Lecture_ID ?? 0;
                }
                if (L_Degry != null)
                {
                    for (int i = 0; i < L_Degry.Count; i++)
                    {
                        int resu = 0;
                        int.TryParse(L_Degry[i], out resu);
                        degry += resu;
                    }
                }
                if (ass.Assignmet_degry_Win <= degry)
                {
                    // اجتياز الواجب يُسجَّل عبر خدمة الحضور؛ الاكتمال يبقى مشروطاً بمشاهدة المحاضرة فعلاً
                    // (ما لم تُفعَّل Attendance:AssignmentAloneCompletes في Web.config).
                    var lecture = dbEntity.Lectures.Find(lectureID) ?? ass.Lecture;
                    if (lecture != null)
                    {
                        Attendance.RecordAssignmentPassed(stdID, lecture);
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
            var exam = dbEntity.Exams.Find(examID);
            if (exam == null)
            {
                return RedirectToAction("CategoriesStd");
            }
            // صفحة الاختبار محمية بحالة اشتراك دورة الاختبار (من بيانات الخادم)
            ActionResult denial;
            if (!TryAuthorizeCourse(exam.Category_ID, out denial)) return denial;

            ViewBag.examID = examID;
            ViewBag.examTime = exam.Duration;
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
                    // الصلاحية تُقاس بدورة الاختبار المسجلة على الخادم، ثم بالدورة المرسلة إن اختلفت عنها
                    var examEntity = await dbEntity.Exams.FindAsync(Exam_ID);
                    JsonResult accessError;
                    if (!TryAuthorizeCourseJson(examEntity != null ? examEntity.Category_ID : (int?)null, out accessError)) return accessError;
                    if (examEntity.Category_ID != category_ID && !TryAuthorizeCourseJson(category_ID, out accessError)) return accessError;

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
                JsonResult accessError;
                if (!TryAuthorizeCourseJson(exams != null ? exams.Category_ID : (int?)null, out accessError)) return accessError;

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
                JsonResult accessError;
                if (!TryAuthorizeCourseJson(exa != null ? exa.Category_ID : (int?)null, out accessError)) return accessError;
                var ExamDegry = exa.Exam_Degry;
                var res = exa.Exam_win_Degry;
                if (exa.Exam_win_Degry <= Stddegry)
                {

                    var values = dbEntity.Std_Reg_Category.Where(e => e.Std_ID == stdID && e.Category_ID == exa.Category_ID).FirstOrDefault();
                    if (values == null)
                    {
                        Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        return Json(new { success = false, response = "لا يوجد تسجيل مباشر لهذه الدورة لحفظ نتيجة الاختبار." }, JsonRequestBehavior.AllowGet);
                    }
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

        /// <summary>
        /// Renders the student's certificate (type == 1, Arabic template) or ijazah (any other type, English template)
        /// by drawing the student's data on the category's template image and returning it as a PNG download.
        /// Any problem (missing registration, missing template, drawing failure) is logged and the student is
        /// sent back to the certificates page with a readable message instead of a server error page.
        /// </summary>
        [HttpGet]
        public ActionResult Certficationsstd(int Categores_ID, int type)
        {
            bool isArabic = type == 1;
            string documentName = isArabic ? "الشهادة" : "الإجازة";
            try
            {
                if (CurrentUser == null)
                {
                    return RedirectToAction("LogIn", "Home");
                }

                var stdID = CurrentUser.ID;
                var category = dbEntity.Categories.Find(Categores_ID);
                var stdinfo = dbEntity.Std_Reg_Category.Where(e => e.Std_ID == stdID && e.Category_ID == Categores_ID).FirstOrDefault();
                if (category == null || stdinfo == null || stdinfo.std_exam_isFinish != 1)
                {
                    return CertificateError("لا توجد " + (isArabic ? "شهادة" : "إجازة") + " متاحة لك في هذه الدورة.");
                }

                string templateName = isArabic ? category.CertificationAR : category.CertificationEN;
                if (string.IsNullOrWhiteSpace(templateName))
                {
                    return CertificateError("لم يتم رفع قالب " + documentName + " لهذه الدورة بعد.");
                }

                string imageFilePath = Server.MapPath("~/Uploade/Images/" + templateName);
                if (!System.IO.File.Exists(imageFilePath))
                {
                    Printerror("Certficationsstd", "Certificate template file not found: " + templateName + " (Category_ID=" + Categores_ID + ", type=" + type + ")", null, null);
                    return CertificateError("ملف قالب " + documentName + " غير موجود على الخادم، يرجى التواصل مع الإدارة.");
                }

                string studentName = CurrentUser.RealName ?? string.Empty;
                string serialNumber = (stdinfo.Serial_Number ?? string.Empty).Trim();
                List<string> finishTime = SpliteString(stdinfo.std_exam_Finish_Time ?? string.Empty, ':');
                string hijriDate = finishTime.Count > 0 ? finishTime[0] : string.Empty;
                string gregorianDate = finishTime.Count > 1 ? finishTime[1] : string.Empty;

                // Draw on a fresh 32bpp copy: Graphics.FromImage() rejects indexed-colour (8-bit) PNG templates,
                // and the template file handle is released as soon as the copy is made.
                using (Image template = Image.FromFile(imageFilePath))
                using (Bitmap bitmap = new Bitmap(template.Width, template.Height, PixelFormat.Format32bppArgb))
                {
                    bitmap.SetResolution(template.HorizontalResolution, template.VerticalResolution);
                    using (Graphics graphicsImage = Graphics.FromImage(bitmap))
                    using (StringFormat stringformat = new StringFormat())
                    using (SolidBrush goldBrush = new SolidBrush(ColorTranslator.FromHtml("#927123")))
                    using (SolidBrush blackBrush = new SolidBrush(ColorTranslator.FromHtml("#000000")))
                    using (MemoryStream mem = new MemoryStream())
                    {
                        graphicsImage.DrawImage(template, 0, 0, template.Width, template.Height);
                        stringformat.Alignment = StringAlignment.Center;
                        stringformat.LineAlignment = StringAlignment.Center;

                        if (isArabic)
                        {
                            int serial;
                            string arabicSerial = int.TryParse(serialNumber, out serial) ? (serial + 1).ToString() : serialNumber;
                            DrawCertificateText(graphicsImage, studentName, 42, goldBrush, new Point(850, 580), stringformat);
                            DrawCertificateText(graphicsImage, stdinfo.Certificate_appreciation, 32, goldBrush, new Point(710, 810), stringformat);
                            DrawCertificateText(graphicsImage, hijriDate, 16, blackBrush, new Point(950, 950), stringformat);
                            DrawCertificateText(graphicsImage, gregorianDate, 16, blackBrush, new Point(650, 950), stringformat);
                            DrawCertificateText(graphicsImage, arabicSerial, 14, blackBrush, new Point(1450, 1190), stringformat);
                        }
                        else
                        {
                            DrawCertificateText(graphicsImage, studentName, 42, goldBrush, new Point(630, 610), stringformat);
                            DrawCertificateText(graphicsImage, hijriDate, 12, blackBrush, new Point(690, 1660), stringformat);
                            DrawCertificateText(graphicsImage, gregorianDate, 12, blackBrush, new Point(440, 1660), stringformat);
                            DrawCertificateText(graphicsImage, serialNumber, 12, blackBrush, new Point(630, 1710), stringformat);
                        }

                        bitmap.Save(mem, ImageFormat.Png);
                        return File(mem.ToArray(), "image/png", isArabic ? "Certificate.png" : "Ijazah.png");
                    }
                }
            }
            catch (Exception ex)
            {
                Printerror("Certficationsstd", ex.Message, ex.InnerException != null ? ex.InnerException.Message : null, ex.StackTrace);
                return CertificateError("حدث خطأ أثناء إنشاء " + documentName + "، يرجى المحاولة لاحقاً.");
            }
        }

        private static void DrawCertificateText(Graphics graphics, string text, float fontSize, Brush brush, Point position, StringFormat format)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }
            using (Font font = new Font("Andalus", fontSize, FontStyle.Regular))
            {
                graphics.DrawString(text, font, brush, position, format);
            }
        }

        private ActionResult CertificateError(string message)
        {
            TempData["CertificateError"] = message;
            return RedirectToAction("Certfications");
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

        #region Payment (مسار قديم مُعطَّل)
        /// <summary>
        /// [مُعطَّل] كان يستقبل من الطالب طريقة الدفع وقيمته وإيصالات التحويل وأكواد المنح ويحفظها في Std_Reg_Category.
        /// أُلغي الدفع داخل الموقع نهائياً: الطالب لا يستطيع تغيير حالة دفعه بنفسه، ولا يُقبل أي مبلغ أو ملف أو كود.
        /// أُبقي اسم المسار كي لا تنكسر الصفحات القديمة المفتوحة؛ يعيد 410 Gone دون أي تعديل في قاعدة البيانات،
        /// والسجلات التاريخية (PayMent_Method / TransactionFile / Code / PayMent_Value) تبقى محفوظة كما هي.
        /// </summary>
        [HttpPost]
        public JsonResult payforcategory()
        {
            Response.StatusCode = (int)HttpStatusCode.Gone;
            Response.TrySkipIisCustomErrors = true;
            return Json(new { success = false, response = "تم إيقاف الدفع داخل الموقع. اشتراكك في الدورات المدفوعة تفعّله الإدارة بعد تأكيد استلام القيمة." }, JsonRequestBehavior.AllowGet);
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