# تقرير الفحص الشامل لمشروع TabseetCourse

تاريخ الفحص: **2026-10-24**. المستودع المحلي `D:\work\GitHub Pro\2222`، الفرع `main` عند `177d937`، مع تغييرات غير محفوظة (ميزة إشعارات المدرسين). هذا تقرير فحص فقط؛ لم يُعدَّل أي ملف من ملفات التطبيق، ولم يُنفَّذ أي استغلال لنقطة كتابة دون تصريح.

يكمل هذا التقرير تقرير الحالة الأساسية `QA/BASELINE_AUDIT.md` (2026-10-17) ويعيد تقييم نتائجه F01–F29 على الكود الحالي.

## 1. الخلاصة التنفيذية

- **البناء ينجح** (MSBuild، Debug): 0 خطأ، 0 تحذير.
- **الدخول يعمل الآن** للمدير والمدرس والطالب (مثبت تشغيلياً). F01 أُصلحت بربط أعمدة الأدوار في `UserIdentityContext`.
- **الخطر الأكبر لم يتغير**: سطح الكتابة الإداري كله تقريباً مكشوف للزائر غير المسجل. لا توجد أي سمة `[Authorize]` فعّالة في `ControlpanelController` (الـ11 الأصلية معلّقة)، و3 إجراءات فقط من 116 إجراء POST تحمل حماية CSRF. يشمل ذلك إنشاء حسابات بدور Admin، إعادة تعيين كلمة مرور أي مستخدم، حذف الحسابات، تعديل محتوى الموقع، بنك الأسئلة والاختبارات، ورفع ملفات بأي امتداد.
- **مثبت تشغيلياً دون جلسة دخول**: `GetUsers` يعيد حسابَي الإدارة مع كلمة المرور النصية؛ `GetTeachers` يعيد `PasswordHash` و`SecurityStamp` والبريد والهاتف لـ6 مدرسين؛ `GetExamQuestion` يعيد مفاتيح التصحيح؛ `/Data/errorLog.json` منشور؛ صفحات الإدارة مثل `/Controlpanel/HomePage` تُقدَّم لأي زائر.
- **مثبت تشغيلياً**: كل دالة عامة في الفئة الأساسية `SharedCalss` قابلة للاستدعاء كإجراء من أي متحكم (مثل `/Students/EMailTemplate`)، وبينها `ChangePassword` و`AssingeRoleToUsers` و`CreateUsers` و`SaveImage` و`SendEmailAsync`.
- **مثبت تشغيلياً**: الحساب المعطل (`IsActive = 0`) يدخل ويفتح صفحاته بنجاح.
- **ميزة الإشعارات الجديدة تعمل من طرف إلى طرف** (مثبت تشغيلياً) وهي أفضل كود في المشروع من حيث التصريح وCSRF، لكنها تحمل: XSS مخزَّن من المدرس إلى المدير في جرس الإشعارات (أُثبت تخزين الحمولة وإعادتها خاماً)، تواريخ **هجرية** غير مقصودة، قبول "إكمال" لمحاضرة موعدها في المستقبل، إزالة BOM من 3 ملفات معدلة و5 ملفات جديدة بلا BOM، وصفحة الإشعارات لا تحمّل jQuery (تحليل ساكن).
- **سكربتات QA الموجودة** فشل فيها 22 اختباراً من 71 **لسبب بيئي** لا لعطل في التطبيق: المدرس التجريبي لم يعد مسنداً للدورة 1 في القاعدة المحلية، فلم يُستخرج رمز CSRF وانهارت بقية الاختبارات. أعدتُ تنفيذ مسار الإشعارات يدوياً على الدورة 6 فنجح.

**الحكم**: المشروع غير جاهز لأي نشر عام قبل إغلاق بنود P0 في القسم 7. ميزة الإشعارات يجب ألا تُحفظ (commit) قبل إغلاق البنود الستة في القسم 6.

## 2. النطاق والمنهجية ومستويات الدليل

ما نُفِّذ:

- مراجعة كود كاملة: `ControlpanelController` (6304 سطراً، 174 إجراءً عاماً)، `HomeController`، `StudentsController`، `TeacherController`، `Classes/SharedCalss.cs`، الخدمة الجديدة `Services/TeacherLectureNotificationService.cs` مع كياناتها ونماذج عرضها ومتحكم `AdminNotificationsController`، 102 عرض Razor، ملفات الإعداد (`Web.config`، `Startup1`، `FilterConfig`، `RouteConfig`، `AuthConfig`)، سكربت الترحيل SQL، سكربتات QA.
- بناء المشروع بـ MSBuild (Visual Studio 18).
- تشغيل IIS Express محلياً (الموقع `tabseetCourse`، المنفذ 50895) وتنفيذ طلبات HTTP دون جلسة (قراءة فقط) وبجلسات حسابات الاختبار. الكتابة اقتصرت على محاضرة اختبار واحدة بعلامة `NOTIF_TEST_XSS` أُزيلت بعد الفحص؛ عادت جداول الإشعارات إلى حالتها قبل الفحص (6 حالات، 13 حدثاً، 26 إشعاراً).
- تشغيل سكربتات QA الثلاثة الموجودة في المجلد `QA/`.

| الوسم | المعنى |
| --- | --- |
| **مثبت تشغيلياً** | نتيجة طلب HTTP أو استعلام SQL فعلي في هذه الجولة |
| **مراجعة كود** | استنتاج من قراءة المصدر بأرقام الأسطر الحالية؛ لم يُنفَّذ الاستغلال |
| **تحليل ساكن** | استنتاج عن سلوك المتصفح أو JavaScript دون تشغيل متصفح |

ما لم يُنفَّذ: لم يُشغَّل متصفح ولا JavaScript؛ لم تُستدعَ نقاط كتابة دون تصريح؛ لم تُرفع ملفات؛ لم يُرسل بريد؛ لم تُفحص بيئة إنتاج أو إعدادات نشر خارجية؛ لم تُعاد مراجعة قوالب الشهادات المفقودة (E07) ولا بيانات التغذية (F15، F24، F25، F28).

## 3. حالة البناء والبيئة

| البند | النتيجة |
| --- | --- |
| MSBuild Debug | نجاح؛ 0 خطأ؛ 0 تحذير |
| الإطار | .NET Framework 4.7.2، MVC 5.2.9، EF 6.4.4 (Database-First للنواة + Code-First لجداول الإشعارات)، Identity 2.2.3، OWIN 4.2.2 |
| قاعدة الاختبار | `TabseetDB_Local` على `.`؛ جداول الإشعارات الثلاثة موجودة، أي أن الترحيل مُطبَّق محلياً |
| `Web.config` | `compilation debug="true"`، `customErrors mode="Off"`، لا تقييد للكوكي، لا رؤوس أمان، لا حجب لمجلد `Data` |
| الملفات غير المتتبعة | ملفات رفع تجريبية في `Uploade/Images` (ملف PDF وصورتا شاشة) يجب ألا تُضاف إلى المستودع |

### نتائج سكربتات QA الموجودة

| السكربت | النتيجة | السبب الفعلي |
| --- | --- | --- |
| `RunTests.ps1` | 2 نجاح / 12 فشل من 14 | المدرس `teacher.ibrahim` مسند في القاعدة إلى "6/الرياضيات,9/أساسيات الحاسوب - اختبار" وليس الدورة 1 التي يفترضها السكربت؛ صفحة `Cat_ID=1` تحوِّله بعيداً فلا يُستخرج رمز CSRF، وتفشل كل طلبات الإنشاء بـ `HttpAntiForgeryException` |
| `TestStudentPages.ps1` | 33 نجاح / 0 فشل | صفحات الطالب والتحويلات والقوائم تعمل |
| `TestTeacherNotifications.ps1` | 14 نجاح / 10 فشل من 24 | الاختبارات 01–10 و21–22 فشلت للسبب البيئي نفسه؛ اختبارات صفحة الإدارة وواجهتها البرمجية (11–20، 23، 24) نجحت |

الفشل البيئي يعني أن تغطية الاختبار الآلي لمسار المدرس كانت صفراً في هذه الجولة؛ القسم 6 يعرض ما أثبتُّه يدوياً بديلاً عنها.

## 4. حالة نتائج التدقيق الأساسي (F01–F29)

| الرقم | الموضوع | الحالة اليوم | الدليل |
| --- | --- | --- | --- |
| F01 | ربط أدوار Identity يمنع الدخول | **أُصلح** | `Data/UserIdentityContext.cs:16-22`؛ دخول المدير والمدرس والطالب مثبت تشغيلياً (302 مع `.AspNet.ApplicationCookie`) |
| F02 | تفويض نقاط البيانات والتعديل وملكية المدرس | **جزئي** | أُصلح لإجراءات المحاضرات فقط (`AllLectures` و`LectureByCategorysStd` تعيد 403 دون دخول، مثبت). كل ما عداه مفتوح: 0 `[Authorize]` في Controlpanel وTeacher وStudents |
| F03 | CSRF وتغييرات عبر GET | **مفتوح** | `[ValidateAntiForgeryToken]`: Controlpanel 3 من 116 POST، Teacher 1 من 24، Students 0 من 11، Home 1 من 15. `ActiveStudenCourse` 778 ما زال يعدّل عبر GET |
| F04 | IsActive لا يمنع الدخول | **مفتوح** | مثبت تشغيلياً: `student27@example.test` المعطل دخل وفتح `/Students/CategoriesStd` بـ 200 |
| F05 | كلمات مرور نصية وأسرار في المصدر | **مفتوح** | العمود النصي يُكتب في `SharedCalss.cs:330,408,624,646` ويُعاد في JSON (مثبت عبر `GetUsers`)؛ سر Facebook في `AuthConfig.cs:15`؛ سر Zoom في تعليقات `SharedCalss.cs:829,869` |
| F06 | سجل الأخطاء متاح عبر الويب | **مفتوح** | مثبت: `GET /Data/errorLog.json` → 200 بحجم 36 كيلوبايت |
| F07 | رفع الملفات بلا تحقق | **جزئي** | `IsValidPdfAttachment` 972 يُطبَّق في إنشاء/تعديل المحاضرة فقط (امتداد ونوع من العميل بلا فحص محتوى أو حجم). `SaveImage` ومستوردا Excel بلا تغيير |
| F08 | إنشاء Zoom الآلي معطل | **مفتوح** | `GenerateLectureLink` و`CreateuserInZoom` تعيدان null/0؛ كود ميت |
| F09 | الرابط اليدوي | **جزئي** | commit 923cca4 أضاف تحققاً من الروابط (HTTPS فقط). لم يُثبت تشغيلياً هنا بسبب فشل QA البيئي |
| F10 | قائمة طلاب المدرس فارغة | **مفتوح** | `TeacherController.cs:104-131` الجسم معلّق ويعيد قائمة فارغة |
| F11 | إكمال المدرس وإشعار الإدارة | **منفَّذ في التغييرات غير المحفوظة** | يعمل تشغيلياً مع عيوب في القسم 6 |
| F12 | الجدول الزمني | **جزئي** | صفحة جدول الحصص للطالب موجودة وتعمل (200، `TestStudentPages`)؛ لم يُقيَّم التقويم الوظيفي |
| F13 | وسائل الدفع | **مفتوح** | `payforcategory` 1282 يحفظ المبلغ من العميل؛ `PayMent` 888 عرض فارغ بلا ملف؛ مسار الوكيل غير منفَّذ |
| F14 | الوصول قبل السداد وغياب فحص التسجيل | **مفتوح** | `LecturesStd` 356-429 يفلتر بـ `cat_ID` فقط؛ `ShowSupDeploamCourse` 304 متاح للزائر |
| F15 | بيانات المجاني المزروعة | **غير معاد التقييم** | — |
| F16 | مفاتيح التصحيح والدرجات من العميل | **مفتوح** | مثبت: `POST /Students/GetExamQuestion` دون دخول يعيد `TrueValue` و`Degry`؛ `SubmetExamQuestion` 1041-1044 يجمع `L_Degry` المرسلة |
| F17 | معادلة التقدير معكوسة | **مفتوح** | `StudentsController.cs:1057-1058` بلا تغيير |
| F18 | نوع ملء الفراغ | **مفتوح** | الأنواع 1–3 فقط في 970-1010 و745-785 |
| F19 | الأهلية غير مفروضة | **مفتوح** | `StdExams` 883 مقارنة Nullable خاطئة؛ 1046-1080 بلا فحص أهلية أو تكرار؛ الشهادة 1106-1108 بلا فحص `std_exam_isFinish` |
| F20 | تسلسل الشهادات | **مفتوح** | عدّاد `static` 1031/1077؛ العربية تطبع المخزن + 1 (1108) |
| F21 | قوالب الشهادات وإخراجها | **مفتوح (كود)** | `Bitmap.FromFile` بلا فحص وجود 1115/1170، catch فارغ 1204-1208، PNG باسم `.jpeg` 1164/1199. الملفات المفقودة لم تُعاد مراجعتها |
| F22 | سجل النتائج القديم | **مفتوح** | `Std_ExamFinish.Std_ID` من نوع `int?` يُقارَن بـ GUID في 914 |
| F23 | تقدم الطالب بمؤقت المتصفح | **مفتوح** | `LecutreTimeEnd` 432-478؛ فرع أول إدخال لا يضبط `IsFinish` |
| F24 | بيانات الاختبار تمنح ثقة زائدة | **مفتوح وتفاقم** | إسناد المدرس التجريبي في القاعدة لم يعد يطابق السكربتات (القسم 3) |
| F25 | تعديل الاختبار والتراجع | **غير معاد التقييم** | — |
| F26 | عرض النتيجة يقرأ حقلاً مختلفاً | **مفتوح** | الخادم يرسل `WinDegry` (1082/1085) و`Exams.cshtml:505,511` يقرأ `stdDegry` |
| F27 | إغلاق التسجيل غير مفروض | **مفتوح** | `HomeController.cs:1290` و969-990 يسجّلان بلا فحص `CanRegister` أو `EndTime` |
| F28 | إحصاءات الشهادات | **غير معاد التقييم** | — |
| F29 | استرجاع كلمة المرور | **مفتوح** | `SendCodeToEmail` 1034 و`ChangePassword` 1108 معلّقان؛ عروض `EnterCode`/`EnterNewPassword` مفقودة |

## 5. النتائج مرتبة بالخطورة

أرقام الأسطر تشير إلى نسخة العمل الحالية. أسماء الملفات دون مسار تقع في `tabseetCourse/Controllers/` ما لم يُذكر غير ذلك.

### 5.1 حرجة (P0)

**C1 — دوال الفئة الأساسية قابلة للاستدعاء كإجراءات من أي متحكم — مثبت تشغيلياً.**
`SharedCalss : Controller` لا يحمل أي `[NonAction]`، فكل دالة عامة فيه تصبح إجراءً قابلاً للاستدعاء عبر `/Students/<الاسم>` و`/Teacher/<الاسم>` و`/Home/<الاسم>` و`/Controlpanel/<الاسم>` بأي فعل HTTP وبربط النموذج الافتراضي. الإثبات: `GET /Students/EMailTemplate?template=zz` أعاد 500 بإطار `SharedCalss.EMailTemplate` في تتبع المكدس، بينما إجراء غير موجود يعيد 404. القابل للوصول بذلك دون دخول: `CreateRoles` (55)، `CreateTeatcher` (81)، `UdateTeatcherInfo` (119)، `CreateUsers` (178)، `UpdateUsers` (225)، `UpdateStudent` (280)، `CreateStudent` (312)، `ChangePassword(UserModel, newPass)` (401)، `AssingeRoleToUsers(userID, Role)` (416)، `SaveDocumentForStudent` (571)، `SaveImage` (585)، `EMailTemplate` (665، يقرأ ملفاً بمسار من العميل)، `SendEmailAsync` (670)، `Printerror` (801). الأثر: ترقية أي حساب إلى Admin أو تغيير كلمة مرور أي مستخدم بطلب GET واحد. لم يُنفَّذ أي من هذه الاستدعاءات المعدِّلة. الإصلاح: `[NonAction]` على كل دالة عامة في `SharedCalss` فوراً، ثم نقلها إلى خدمة ليست متحكماً.

**C2 — إنشاء الحسابات والاستيلاء عليها دون دخول — مراجعة كود.**
`ControlpanelController`: `CreateUser` 3068 يقبل قائمة أدوار من العميل (بما فيها Admin) أو يعيد تعيين كلمة مرور وأدوار أي مستخدم عند تمرير `ID`؛ `UpdateTeachers` 3202 يعيد تعيين كلمة مرور أي مستخدم ويحوّله إلى Teacher؛ `UpdateStd` 3348؛ `DeleteUsers` 3113؛ `UpdateStdActivate` 3385؛ `SaveTeachers` 3157؛ `AddNewStd` 3333. `TeacherController.SaveInfo` 1651-1667 يحمّل المستخدم بـ `PInFom.ID` القادم من العميل ثم `ChangePassword` عليه؛ `StudentsController.SaveInfo` 70-85 يعدّل بيانات أي مستخدم. كلها POST بلا تصريح ولا رمز CSRF. الإصلاح: تصريح Admin + CSRF + قائمة أدوار مسموحة على الخادم + ربط المعرّف بالجلسة لا بالنموذج.

**C3 — كشف كلمات المرور وأسرار الهوية لأي زائر — مثبت تشغيلياً.**
`POST /Controlpanel/GetUsers` أعاد حسابَي الإدارة ومعهما حقل `Password` نصي مملوء (2994-3009). `POST /Controlpanel/GetTeachers` أعاد 6 مدرسين بكائن `IdentityUser` كاملاً: `PasswordHash` و`SecurityStamp` والبريد والهاتف (3248). بالكود أيضاً: `GetUserByID` 3033، `GetTeacherByID` 3233، `GetStdByID` 3369. الجذر: الخاصية `MyIdentityUser.Password` (`Data/UserIdentityContext.cs:36`) تُكتب نصاً في `SharedCalss.cs:330,408,624,646`. الإصلاح: حذف الخاصية والعمود، وإرجاع DTO بلا أي حقل سري، وتصريح Admin على هذه النقاط.

**C4 — رفع ملفات تعسفي دون دخول — مراجعة كود.**
`ReadQuesionFromExcelTrueOrFalse` 3541 و`ReadQuesionFromExcelMultiChoise` 3686 بلا فعل ولا تصريح، وتحفظ الملف باسمه الأصلي تحت `~/Uploade/StudentFile/` (3555-3558، 3700-3703) قبل فحص الامتداد، في مسار متوقع. `SaveImage` (`SharedCalss.cs:585-609`) تحتفظ باسم وامتداد العميل بلا قائمة سماح ولا حد حجم ولا `Path.GetFileName`، وتُستدعى من نحو 18 إجراءً مجهولاً (425، 597، 3068، 3157، 3202، 4351، 4419، 4555، 4749، 4882، 4976، 5214، 5318، 5451، 5696، 5916، 6192، 6208). `CurrentImageID` يُمرَّر إلى `Server.MapPath` ثم `File.Delete` (602-603) فيتيح حذف ملفات داخل جذر التطبيق. لا يوجد `web.config` في `Uploade/` يعطّل المعالجات. الإصلاح: أسماء مولَّدة على الخادم، قائمة امتدادات، فحص بايتات الملف، حد حجم، تعطيل المعالجات في مجلد الرفع، وإغلاق مستوردَي Excel خلف تصريح Admin.

**C5 — سطح الكتابة الإداري بلا تصريح ولا CSRF — مراجعة كود مع إثبات تشغيلي للوصول.**
لا توجد سمة `[Authorize]` فعّالة في `ControlpanelController` (الأسطر 90، 315، 389، 423، 550، 595، 695، 721، 776، 829، 856 معلّقة بتعليق)؛ `[ValidateAntiForgeryToken]` موجودة في 3 إجراءات فقط (1135، 1335، 1494). نحو 90 إجراء POST لإدارة فئات الدورات والدورات والأسئلة والاختبارات والواجبات والأكواد ومحتوى الموقع والمتجر قابلة للتعديل من زائر (القائمة الكاملة في الملحق ب). مثبت تشغيلياً أن صفحات الإدارة تُقدَّم لزائر: `GET /Controlpanel/HomePage` → 200، `GET /Controlpanel/LecturesByCategory?Category_ID=1` → 200. إضافات خطيرة: `sendEmail` 3406 مرحِّل بريد مفتوح باسم الأكاديمية؛ `UpdateStyle` 6053 يكتب `~/Content/RootStyle.css` العام من خمسة نصوص خام (6067-6073)؛ `socialmedia` 5825 يربط كيان EF مباشرة من الطلب (over-posting). الإصلاح: `[Authorize(Roles="Admin")]` على مستوى الفئة مع `[AllowAnonymous]` للاستثناءات الحقيقية، و`[ValidateAntiForgeryToken]` على كل POST.

**C6 — مفاتيح التصحيح مكشوفة والدرجات من العميل (F16) — مثبت تشغيلياً.**
`POST /Students/GetExamQuestion` بـ `exam_ID=1` دون جلسة أعاد `TrueValue` و`Degry` لكل خيار و`ChoiseTrueAnswer`. مثلها `GetAssignmentQuestion` 725، `TeacherController.GetQuestionById` 1299، `ControlpanelController.GetQuestionById` 1968، `GetAssignmentByID` 2407، `GetExamByID` 2769. `SubmetExamQuestion` 1041-1044 و`SubmetQuestion` 812-817 تجمع `L_Degry` المرسلة من المتصفح بلا فحص أهلية أو مدة أو تكرار، ثم تضبط النجاح والشهادة (1052-1079). الإصلاح: استلام معرّفات الإجابات فقط، وحساب الدرجة من القاعدة، وتخزين سجل محاولة، وفرض الأهلية والمحاولة الواحدة في معاملة واحدة.

**C7 — IsActive لا يُفرض (F04) — مثبت تشغيلياً.**
الحساب المعطل `student27@example.test` دخل (302 مع كوكي) وفتح `/Students/CategoriesStd` بـ 200. `HomeController.cs:942-944` يوقّع الدخول فور نجاح `FindAsync`، و`GetCurrentUser` (`SharedCalss.cs:460-492`) يتجاهل العلم. الإصلاح: رفض `IsActive != 1` عند الدخول وفي `GetCurrentUser` وإنهاء الجلسات الحالية عند التعطيل.

**C8 — إجراءات المدرس لإدارة المحتوى بلا تصريح — مراجعة كود.**
`TeacherController`: 526، 571، 629، 807، 850، 1107، 1148، 1198، 1249، 1402، 1454، 1511 (إنشاء/تعديل/حذف أسئلة واختبارات وواجبات لأي دورة) بلا دخول ولا ملكية ولا CSRF؛ القراءات 421، 453، 685، 734، 919، 1043 تكشف إعداد الاختبارات. المقابل الإيجابي: إجراءات المحاضرات (`LecturesByCategoryStudend` 202، `LectureByCategorysStd` 215، `UpdateLectureStatus` 272) محمية دوراً وملكية عبر `IsAuthorizedForCategory` 157-199، ومثبت تشغيلياً أنها تعيد 403 للدورة غير المسندة. الإصلاح: تطبيق `IsAuthorizedForCategory` على كل إجراء مع استخراج الدورة من الكيان الهدف على الخادم.

### 5.2 عالية (P1)

**H1 — XSS مخزَّن من المدرس إلى المدير عبر جرس الإشعارات — مثبت تخزين الحمولة وإعادتها خاماً.**
`Views/Shared/_AdminLayout.cshtml:616-628` يبني HTML من `item.Title` و`item.Details` و`item.TimeAgo` بالضم النصي ويحقنه بـ `.html()` دون ترميز، وهذه النصوص تتضمن اسم المحاضرة واسم المدرس والملاحظات وسبب التأجيل (الخدمة 220-244). الإثبات: سبب تأجيل يحوي `<b>` حُجب بـ ASP.NET Request Validation (لكن بصفحة خطأ 500 خام)، بينما سبب يحوي `" onmouseover="` خُزِّن وأُعيد خاماً في `GetLatest` الذي يستهلكه جرس كل صفحة إدارة كل 30 ثانية. السمات الحالية الأخرى: `Views/Teacher/LecturesByCategoryStudend.cshtml:463` (سمة `title` من `PostponeReason`)، و469-481 (تهريب `safeLecName` قابل للتجاوز بـ `&#39;` لأن المتصفح يفكّ الكيانات قبل تحليل JS)، و`Views/Controlpanel/LecturesByCategory.cshtml:652-664` (اسم المحاضرة ورابطها إلى صفحة المدير). صفحة `AdminNotifications/Index.cshtml` نفسها تستخدم `.text()` بشكل صحيح. الإصلاح: دالة `escapeHtml` واحدة تُطبَّق على كل حقل قادم من JSON، ونقل معالجات `onclick` إلى سمات `data-*` مع مستمعين مفوَّضين.

**H2 — تقوية المصادقة والجلسة ناقصة — مراجعة كود مع إثبات لصفحة الخطأ.**
لا قفل محاولات: `HomeController.cs:937-941` يحمّل جدول المستخدمين كله في كل محاولة ثم `FindAsync` دون `AccessFailedCount`. `App_Start/Startup1.cs:17-21` بلا `CookieSecure` أو `ExpireTimeSpan` أو `SameSite` أو مدقق `SecurityStamp`. `Web.config`: `debug="true"` و`customErrors mode="Off"` فتظهر صفحة الخطأ الصفراء بتتبع المكدس لأي زائر (مثبت: `POST /Controlpanel/GetQuestionById` → 500 مع "Stack Trace"). تسجيل الطالب `RegesterNewStdView` 1244 بلا CSRF ويربط `UserModel` كاملاً.

**H3 — الطالب يرى محاضرات أي دورة والدفع يحدده العميل (F13، F14) — مراجعة كود.**
`LecturesStd` 356-429 يفلتر بـ `cat_ID` فقط دون `Std_Reg_Category`، فأي حساب مسجَّل (حتى مدرس) يحصل على `LectureLink_Client` و`MediaLink` و`DocumenFile` لأي دورة؛ `ShowSupDeploamCourse` 304 متاح للزائر. `payforcategory` 1282 يحفظ `PayMent_Value` كما أرسله العميل، ووسيلة البطاقة تضبط علماً فقط، و`TransactionFile` يقبل أي ملف.

**H4 — سجل الأخطاء منشور وأسرار في المصدر (F05، F06) — مثبت تشغيلياً للسجل.**
`GET /Data/errorLog.json` → 200 بتتبعات مكدس وأسماء دوال ومسارات مطوِّر. لا `Data/Web.config`. سر تطبيق Facebook حي في `App_Start/AuthConfig.cs:15` (الدالة غير مستدعاة من `Global.asax` لكنها في تاريخ المستودع)؛ سر Zoom JWT في تعليقات `SharedCalss.cs:829,869`. `Printerror` 801-821 قراءة/كتابة غير ذرية تتلف تحت التزامن. الإصلاح: حجب `Data` بـ `<location>` أو نقل السجل خارج الجذر، تدوير السرّين وحذفهما من المصدر.

**H5 — حالة ساكنة مشتركة بين كل المستخدمين — مراجعة كود.**
`ControlpanelController`: `CurrentLangForOneUser` 4133 (يُضبط من `SetLangForUser` 4135 دون دخول)، معرّفات صفحات CMS 4530-4532، 4740، 4872، 4955-4956 تُضبط في GET وتُستهلك في POST لاحق فيكتب محرران محتوى كلٍّ في صفحة الآخر. `StudentsController`: عدّاد الشهادات `static` 1031/1077 (F20) يُصفَّر عند إعادة تدوير التطبيق ويتسابق بين الطلبات.

**H6 — سياقات قاعدة البيانات لا تُتخلَّص والمصادقة تضرب القاعدة في كل طلب — مراجعة كود.**
`SharedCalss.cs:35-54` ينشئ `dbEntity` و`dbIdentity` و`UserStore` و`userManager` لكل طلب دون أي `Dispose(bool)` في المشروع، ويستدعي `GetCurrentUser()` (استعلامان على الأقل) حتى للصفحات العامة. الإصلاح: تجاوز `Dispose` وتأجيل تحميل المستخدم.

**H7 — إرسال البريد معطَّل وغير آمن — مراجعة كود.**
`SendEmailAsync` (`SharedCalss.cs:670-708`): Gmail على المنفذ 25 بلا SSL، كلمة المرور من `AppSettings["EmailPassword"]` غير الموجود في `Web.config` (null)، متزامن رغم اسمه، يبتلع الأخطاء إلى `Console.WriteLine`. المستدعي الحي الوحيد هو مرحِّل البريد المفتوح في C5.

### 5.3 متوسطة (P2)

**M1 — تواريخ هجرية غير مقصودة في الإشعارات — مثبت تشغيلياً.**
`FormatLocalTime` (`Services/TeacherLectureNotificationService.cs:54-67`) يستخدم `new CultureInfo("ar-SA")` الذي تقويمه الافتراضي في .NET Framework هو أم القرى، فأعاد `CreatedAtFormatted` بقيمة `1448-05-13 01:15 م` بينما بقية التطبيق ميلادي. المنطقة الزمنية ثابتة "Arab Standard Time" بينما المرجع Europe/Istanbul (متطابقان حالياً UTC+3). الإصلاح: ثقافة ميلادية مع أسماء عربية (مثل `ar-EG`) أو تقويم ميلادي صريح، ومنطقة زمنية من الإعدادات.

**M2 — قواعد عمل حالة المحاضرة — مثبت تشغيلياً للأولى.**
قُبل "Completed" لمحاضرة موعدها 2026-11-20 (المستقبل). "Scheduled" لمحاضرة بلا صف حالة يولّد حدث "إعادة فتح" وهمياً لأن فحص التكرار (الخدمة 195-210) لا يعمل إلا عند وجود صف. `teacherName = RealName ?? UserName` (`TeacherController.cs:303`) لا يغطي الاسم الفارغ، و`UserName` رمز عشوائي من 9 حروف. `notes` و`postponeReason` بلا حد طول، و`Title` محدود بـ 256 بينما `Lecture_Name` من نوع `nvarchar(max)` فيفشل التحديث كله لأسماء المحاضرات الطويلة. `ex.Message` يُعاد للعميل (`AdminNotificationsController.cs:69,85,104,122,144` و`TeacherController.cs:317`). فشل تسجيل الإشعار في `ControlpanelController` 1240-1259، 1457-1481، 1529-1543 يُبتلع بـ `catch { }` دون تسجيل.

**M3 — واجهة الإشعارات — تحليل ساكن.**
`_AdminLayout.cshtml` يحمّل إضافات jQuery (527-560) دون jQuery الأساسي، ولا يعرّف `RenderSection`، و`AdminNotifications/Index.cshtml` لا يضمّن jQuery؛ لذا يُتوقع أن تفشل سكربتات الصفحة (تحديد كمقروء، التفاصيل) في المتصفح بخطأ `$ is not defined`. اختبار `TEST-NOTIF-11` يفحص وجود HTML فقط ولا يثبت العمل. المستطلِع يعمل لكل من يستخدم `_AdminLayout` (بما فيهم أدوار غير Admin تتلقى 302 كل 30 ثانية) بلا `dataType` ولا توقف عند الفشل؛ الجرس يظهر لكل الأدوار؛ `GetDetails` يغيّر حالة القراءة عبر GET. يلزم تأكيد بمتصفح.

**M4 — الترحيل والنشر.**
`Database/Migrations/20261023_CreateTeacherNotificationsSystem.sql` يبدأ بـ `USE [TabseetDB_Local]` فيفشل على أي اسم قاعدة آخر. عند غياب الجداول تعود قائمة محاضرات المدرس كلها 400 (`TeacherController.cs:238,263-267`) بدل التدهور إلى "مجدولة". الخدمة تُنشأ بـ `new` داخل المتحكمات الثلاثة فتمنع الحقن والاختبار.

**M5 — انكسار اتفاقية الترميز.**
التغييرات غير المحفوظة أزالت BOM من `ControlpanelController.cs` و`_AdminLayout.cshtml` و`LecturesByCategoryStudend.cshtml`، والملفات الجديدة الخمسة (`AdminNotificationsController.cs`، `TeacherNotificationEntities.cs`، `TeacherLectureNotificationViewModels.cs`، `TeacherLectureNotificationService.cs`، `AdminNotifications/Index.cshtml`) بلا BOM، وملفا `.cs` الجديدان بنهايات LF. يخالف `.editorconfig` وcommit 923cca4. لا تشوّه وقت التشغيل لأن `Web.config` يضبط `fileEncoding="utf-8"`، لكن الاتفاقية انكسرت وستظهر فروق مزعجة في Git.

**M6 — منطق التعليم الموروث (ما زال مفتوحاً).**
معادلة التقدير معكوسة `StudentsController.cs:1057-1058` (30/50 تعطي "جيد جداً" و40/50 "جيد")، وقسمة على صفر عند `Stddegry == 0`. `StdExams` 883 يقارن `Nullable<int>` بـ 0 فيصبح فرع الفئة الأم غير قابل للوصول، و914 يقارن `int?` بـ GUID. الشهادات: `Bitmap.FromFile` بلا فحص وجود، catch فارغ، `File(Response.OutputStream)` يرمي عند التنفيذ، PNG باسم `.jpeg`، وGDI بلا `Dispose`. `LecutreTimeEnd` 432-478 يعيد `success:true` دائماً ويعتمد على مؤقت المتصفح. `Exams.cshtml:505,511` يقرأ `stdDegry` والخادم يرسل `WinDegry`. `TeacherCoursestd` 104-131 قائمة فارغة عمداً.

**M7 — معاملات تمتد على سياقين — مراجعة كود.**
`UpdateAssingQuestionToAssignment` 2535-2540 و`EditExams` 2897-2902 و`TeacherController` 589/868 تحذف عبر `dbEntity` خارج المعاملة المفتوحة على `context`؛ `ChangeDefultLang` 4142 يفتح معاملة على سياق ويكتب على آخر؛ `EditeCategories` 635 يبحث في `NewGridPages` بمعرّف من `New_pages`. `DeletQuestion` (Teacher 1402-1445) لا يحذف أبناء النوعين 3 و4.

**M8 — نموذج الأدوار الضعيف.**
`GetAuthenticatedLectureUser` (`ControlpanelController.cs:1052-1055`) يمنح أدوار `LectureView/LectureEdit/LectureDelete` صلاحيات Admin الكاملة على المحاضرات ثم يطابق الأدوار نصياً جزئياً (1059-1062)؛ `Index` 48-74 لا يشمل Teacher فيُحوَّل المدرس إلى الدخول. الدخول يوجّه بأول دور فقط (`HomeController.cs:946-948`) ويعطي رسالة مضللة للمستخدم بلا أدوار.

**M9 — أخطاء 500 غير معالجة.**
`ex.InnerException.ToString()` أو `.Message` داخل catch دون فحص null: `ControlpanelController.cs:814,1599,3675,3865`، `HomeController.cs:1322`، `StudentsController.cs:278`، `TeacherController.cs:404`. `FirstOrDefault().ID` بلا فحص في نحو 12 إجراءً في Home (50، 159، 183، 218، 258، 280، 443، 578، 908، 1146). عروض مفقودة: `PayMent`، `PageText`، `NewsArchive`، `IniPracktes`، `EnterCode`، `EnterNewPassword`. تحليل تواريخ معتمد على الثقافة: `DateTime.Parse` في `HomeController.cs:95,418,1224` و`TeacherController.cs:383-384` و`ControlpanelController.cs:1187,1407`. `TempData` كمصدر للمعرّفات في `GridPage` 4361 و`AddContentGridPageModel` 4429 و`SubmetQuestion` 809 يرمي عند التحديث أو التبويب الثاني.

**M10 — سكربتات QA غير متزامنة مع القاعدة.**
تفترض `teacher.ibrahim` مسنداً للدورة 1 بينما `Categories_ID` الحالي "6/الرياضيات,9/أساسيات الحاسوب - اختبار". عند فشل استخراج الرمز تتدحرج الأخطاء إلى `sqlcmd` بجمل SQL ناقصة ("Incorrect syntax near ';'"). `TEST-14` يطبع "All successfully rejected" ويعلن FAIL. الإصلاح: قراءة دورة المدرس من القاعدة في بداية السكربت، والتوقف المبكر عند رمز فارغ.

### 5.4 منخفضة (P3)

**L1 — الواجهة.** jQuery محمَّل من خمس نسخ ومصادر مختلفة، و36 عرضاً يحمّله مرتين أو أكثر؛ `sweetalert2@11` عائم بلا SRI في 55 عرضاً؛ عنوان الإنتاج `https://tbsetaca.com/Uploade/Images/` ثابت في 5 عروض فيكسر أي بيئة أخرى؛ `appId` فيسبوك بين أقواس معقوفة في `Views/Home/LogIn.cshtml:118` فلا يعمل؛ `Views/Students/LecturesStd.cshtml:107` يبني `onclick` من اسم ملف في القاعدة.

**L2 — المستودع.** ملفات رفع تجريبية غير متتبعة في `Uploade/Images` (PDF وصورتا شاشة) يجب تجاهلها في `.gitignore` لا إضافتها؛ 244 ملف رفع متتبعة؛ `Data/errorLog.json` متتبع ويتغير وقت التشغيل؛ `README.md` سطر واحد.

**L3 — اتساق الاستجابات.** `success = true` مع 400 (`ControlpanelController.cs:3098-3107,3191-3197,3222-3229,5604,5627`) و`success = false` عند النجاح (6103، 6126، 6144، 6246، 6261)؛ مستوردا Excel يعيدان null؛ `LincesReg` يحفظ الرفع مرتين (5920/5929)؛ `GenerateCodes` بـ `System.Random`؛ تغييرات حالة عبر GET: `ActiveStudenCourse` 778 و`LogOut` في ثلاثة متحكمات و`GetDetails` في الإشعارات.

**L4 — كود ميت.** Zoom (`GenerateLectureLink` 825، `CreateuserInZoom` 863)، استرجاع كلمة المرور (F29)، `AuthConfig.RegisterAuth`، `ParseDate` بصيغة غير صالحة، `using` مكرر في `SharedCalss.cs:9,24`، 11 سمة `[Authorize]` معلّقة.

## 6. تقييم ميزة إشعارات المدرسين (التغييرات غير المحفوظة)

الملفات: `Services/TeacherLectureNotificationService.cs`، `Data/TeacherNotificationEntities.cs`، `Models/TeacherLectureNotificationViewModels.cs`، `Controllers/AdminNotificationsController.cs`، `Views/AdminNotifications/Index.cshtml`، تعديلات `ControlpanelController` و`TeacherController` و`_AdminLayout.cshtml` و`LecturesByCategoryStudend.cshtml`، `Database/Migrations/20261023_CreateTeacherNotificationsSystem.sql`، `QA/TestTeacherNotifications.ps1`.

**ما يعمل (مثبت تشغيلياً على الدورة 6 بحساب المدرس التجريبي):**

| الخطوة | النتيجة |
| --- | --- |
| إنشاء محاضرة كمدرس | 200، حدث `LectureCreated` واحد، إشعار لكل مدير نشط (2) |
| تأجيل بسبب، إكمال، إعادة فتح | 200 مع تحديث `TeacherLectureStatus` وحدث لكل تغيير |
| تغيير حالة محاضرة في دورة غير مسندة | 403 "الدورة غير مسندة لهذا المدرس" |
| تأجيل بلا سبب، وتكرار الإكمال | مرفوضان (TEST-NOTIF-05/07) |
| `/AdminNotifications` للزائر وللطالب | 302 إلى الدخول (TEST-NOTIF-19/20) |
| العدّاد والقائمة والتفاصيل وتحديد المقروء واستقلاله لكل مدير | نجاح (TEST-NOTIF-12..18) |

التصميم سليم من حيث الفصل: جداول مستقلة بـ Code-First مع `SetInitializer(null)`، لقطة بيانات في الأحداث تبقى بعد حذف المحاضرة، فهارس مناسبة، معاملات، `[Authorize(Roles="Admin")]` على المتحكم، CSRF على طلبات التعديل.

**يجب إغلاقه قبل الـ commit:**

1. ترميز HTML لكل حقل JSON في `_AdminLayout.cshtml` (616-628) و`LecturesByCategoryStudend.cshtml` (463، 469-481) و`Controlpanel/LecturesByCategory.cshtml` (652-664)، مع نقل `onclick` إلى `data-*` (H1).
2. استبدال `ar-SA` بثقافة ميلادية وضبط المنطقة الزمنية من الإعدادات (M1).
3. منع "Completed" قبل موعد المحاضرة أو تحذير صريح، ومنع حدث "إعادة فتح" لمحاضرة بلا حالة سابقة، و`IsNullOrWhiteSpace` لاسم المدرس مع بديل من الاسم الأول والأخير، وحدود طول للملاحظات، واقتصاص العنوان إلى 256 (M2).
4. تحميل jQuery في التخطيطين قبل `RenderBody` وإضافة `RenderSection("scripts")`؛ قصر الجرس والمستطلع على Admin مع `dataType: 'json'` وتوقف بعد فشل متكرر (M3).
5. رسائل خطأ عامة بدل `ex.Message`، وتسجيل فشل الإشعار بدل `catch { }` الفارغ (M2).
6. إعادة BOM إلى الملفات الثمانية وتوحيد CRLF (M5)؛ جعل اسم القاعدة في سكربت الترحيل معلمة والتدهور الرشيق عند غياب الجداول (M4)؛ تحديث سكربت QA ليقرأ دورة المدرس من القاعدة (M10).

## 7. خارطة الإصلاح المقترحة

**P0 — قبل أي نشر:**

1. `[NonAction]` على كل دالة عامة في `SharedCalss` (إصلاح دقائق يغلق C1).
2. `[Authorize]` على مستوى الفئة في `Controlpanel` و`Teacher` و`Students` مع `[AllowAnonymous]` للاستثناءات الحقيقية، و`[Authorize(Roles="Admin")]` لإدارة المستخدمين والمحتوى، وفحص ملكية المدرس في كل إجراء محتوى (C2، C5، C8).
3. حذف الخاصية والعمود `Password` النصي، وإرجاع DTO بلا أسرار من `GetUsers` و`GetUserByID` و`GetTeacherByID` و`GetStdByID` و`GetTeachers` (C3).
4. فرض `IsActive` عند الدخول وفي `GetCurrentUser` (C7).
5. CSRF شامل: `@Html.AntiForgeryToken()` في التخطيطات، `$.ajaxPrefilter` يضيف الرمز لكل طلب غير GET، `[ValidateAntiForgeryToken]` على كل POST؛ تحويل `ActiveStudenCourse` و`LogOut` و`GetDetails` إلى POST (F03).
6. الرفع: أسماء مولَّدة على الخادم، قائمة امتدادات، فحص بايتات، حد حجم، `web.config` في `Uploade/` يزيل المعالجات (C4).
7. التصحيح الخادمي للاختبارات والواجبات وإزالة مفاتيح الإجابات من استجابات الطالب (C6).
8. `Web.config`: `debug="false"`، `customErrors mode="On"`، `httpCookies requireSSL httpOnlyCookies`، `<location path="Data">` بالمنع، رؤوس أمان؛ `Startup1`: `CookieSecure`، `ExpireTimeSpan`، `SameSite`، مدقق `SecurityStamp`؛ قفل المحاولات بـ `AccessFailedAsync` (H2، H4).
9. حذف الأسرار من المصدر وتدويرها (H4).

**P1:** H1 (XSS الإشعارات والمحاضرات)، M1–M5 (ميزة الإشعارات)، H3 (تحقق التسجيل والدفع)، H5 (الحالة الساكنة وعدّاد الشهادات)، H6 (Dispose)، H7 (البريد).

**P2:** M6 (الدرجات والشهادات والإكمال)، M7 (المعاملات)، M8 (الأدوار)، M9 (الاستثناءات والعروض المفقودة)، M10 (QA).

**P3:** L1–L4.

## 8. حدود هذا التقرير

- لا اختبار متصفح؛ كل ما يتعلق بـ JavaScript (M3 وجزء من H1) تحليل ساكن يلزم تأكيده بمتصفح.
- لم تُستدعَ أي نقطة كتابة دون تصريح، ولم يُجرَّب رفع ملف أو إرسال بريد أو تغيير دور؛ C2 وC4 وC5 وC8 مراجعة كود مدعومة بإثبات تشغيلي لقابلية الوصول (C1، C3، C5 جزئياً).
- الكتابة الوحيدة على القاعدة المحلية: محاضرة اختبار واحدة وأحداثها وإشعاراتها أُزيلت بالكامل؛ سكربتات QA أنشأت وأزالت بياناتها بعلاماتها.
- لم تُفحص بيئة نشر أو قاعدة إنتاج.

## الملحق أ — الأدلة التشغيلية

### أ.1 طلبات دون جلسة دخول

| الطلب | النتيجة |
| --- | --- |
| `GET /Data/errorLog.json` | 200 `application/json`، 36 كيلوبايت |
| `GET /Data/TabseetDB.edmx.sql`، `/Data/tbseedDB.edmx`، `/Data/tbseedDB.Context.tt` | 404 (لا ربط MIME) — غير مكشوفة حالياً، وتُكشف بمجرد إضافة MIME |
| `GET /Uploade/Images/` | 403 (لا فهرسة) |
| `POST /Controlpanel/GetUsers` | 200؛ حسابان إداريان بحقل `Password` نصي مملوء |
| `POST /Controlpanel/GetTeachers` | 200؛ 6 مدرسين مع `PasswordHash` و`SecurityStamp` والبريد والهاتف |
| `POST /Students/GetExamQuestion` بـ `exam_ID=1` | 200؛ `TrueValue` و`Degry` لكل خيار |
| `POST /Controlpanel/GetAllCategories` | 200 |
| `POST /Controlpanel/AllLectures` | 403 (أُصلح منذ الأساسي) |
| `POST /Teacher/LectureByCategorysStd` بـ `Cat_ID=1` | 403 (أُصلح) |
| `POST /Controlpanel/GetQuestionById` بـ `ID=1` | 500 صفحة خطأ صفراء مع "Stack Trace" و`ArgumentException` |
| `GET /Controlpanel/HomePage` | 200 صفحة إدارة محتوى كاملة |
| `GET /Controlpanel/LecturesByCategory?Category_ID=1` | 200 (البيانات عبر AJAX محمية) |
| `GET /Controlpanel/Index`، `/Controlpanel/Users` | 302 إلى الدخول |
| `GET /AdminNotifications/Index`، `/GetLatest` | 302 إلى الدخول |
| `GET /Students/EMailTemplate?template=zz` و`/Teacher/EMailTemplate?template=zz` | 500 بإطار `SharedCalss.EMailTemplate` (الدالة الموروثة قابلة للاستدعاء) |
| `GET /Students/NoSuchActionXyz` | 404 (للمقارنة) |

### أ.2 طلبات بجلسات حسابات الاختبار

| الطلب | النتيجة |
| --- | --- |
| دخول `teacher.ibrahim@example.test` | 302 إلى `/Teacher/TeacherCourse` مع `.AspNet.ApplicationCookie` |
| دخول `admin.test@example.test` | 302 إلى `/ControlPanel` مع الكوكي |
| دخول `student27@example.test` (معطل) | 302 إلى `/Students/CategoriesStd` مع الكوكي، ثم الصفحة 200 |
| `GET /Teacher/LecturesByCategoryStudend?Cat_ID=1` كمدرس غير مسند | 302 إلى `/Teacher/TeacherCourse` |
| `GET /Teacher/LecturesByCategoryStudend?Cat_ID=6` كمدرس مسند | 200 مع رمز CSRF |
| `POST /Controlpanel/CreateLectuer` في الدورة 6 | 200؛ حدث واحد؛ إشعاران |
| `POST /Teacher/UpdateLectureStatus` بسبب يحوي `<b>` | 500 "A potentially dangerous Request.Form value" |
| `POST /Teacher/UpdateLectureStatus` بسبب يحوي `" onmouseover="` | 200؛ خُزِّن وأُعيد خاماً في `GetLatest` |
| `POST /Teacher/UpdateLectureStatus` إكمال لمحاضرة موعدها 2026-11-20 | 200 مقبول |
| `POST /Teacher/UpdateLectureStatus` لمحاضرة من الدورة 1 | 403 |
| `CreatedAtFormatted` في `GetLatest` | `1448-05-13 01:15 م` |

## الملحق ب — الإجراءات المعدِّلة بلا تصريح ولا CSRF في `ControlpanelController`

| المجموعة | الإجراءات (السطر) |
| --- | --- |
| المستخدمون والمدرسون والطلاب | CreateUser 3068، DeleteUsers 3113، SaveTeachers 3157، UpdateTeachers 3202، AddNewStd 3333، UpdateStd 3348، UpdateStdActivate 3385 |
| فئات الدورات والدورات | CreateCategoriesType 159، EditeCategoriesType 236، DeleteCategoriesType 267، CreateCategories 425، EditeCategories 597، DeleteCategories 697 |
| بنك الأسئلة | CreateTruOrFalseQuestion 1755، EditTruOrFalseQuestion 1796، CreateMultiChoiseQusetion 1867، EditMultiChoiseQusetion 1918، DeletQuestion 2132، CreateMatchingChoiseQuestion 2213، EditMathcingQusetion 2291 |
| استيراد Excel (بلا فعل HTTP) | ReadQuesionFromExcelTrueOrFalse 3541، ReadQuesionFromExcelMultiChoise 3686 |
| الواجبات والاختبارات | AssingQuestionToAssignment 2478، UpdateAssingQuestionToAssignment 2523، DeleteQuestionFromAssignment 2581، CreateExams 2842، EditExams 2885 |
| الأكواد ومتفرقات | SaveCodeGenerated 3917، UpdateGeneratedCode 3937، GenerateCode 3992، sendEmail 3406 (بلا فعل)، SetLangForUser 4135، UpdateStyle 6053 |
| محتوى الموقع (CMS) | Languages 4114، ChangeDefultLang 4142، DeleteLanguage 4207، Pages 4251، DeletePage 4308، GridPage 4351، AddContentGridPageModel 4419، DeleteGridPageModelPage 4482، DeleteGridPageModelContent 4503، HorizantalPageTabe 4555، DeleteHorizantalPageTabe 4612، AddTaHorizantalPageTabe 4664، DeleteTabeHorizantalPageTabe 4712، ContactUsPage 4749، PageText 4882، AboutPage 4976، AddTabeForAbout 5041، DeleteAboutUsPage 5089، DeleteAcoutUsTabe 5110، Menu 5152، DeleteMenu 5173، HomePage 5214، Slider 5318، DeleteSlider 5346، FooterPage 5385، DeleteFooter 5407، News 5451، DeleteNews 5514، SaveFastNews 5593، EditFastNews 5612، DeleteFastNews 5653، Pracktes 5696، DeletePracktes 5758، socialmedia 5825، LincesReg 5916 |
| المتجر | StoreCategory 6096، EStoreCategory 6115، DeleteStoreCategory 6138، CreateProduct 6192، EditeProduct 6208، DeleteStoreProduct 6249 |
| تعديل عبر GET | ActiveStudenCourse 778، LogOut 4001 |

نقاط القراءة بلا تصريح التي تكشف بيانات حساسة: GetQuestionById 1968، GetAssignmentByID 2407، GetExamByID 2769، GetUsers 2994، GetUserByID 3033، GetTeacherByID 3233، GetTeachers 3248، GetStdByID 3369، GetCategoryChat 3488، GetAllQuesinBank 1635، GetQuestionListByID 1671. الإجراءات المحمية بشكل صحيح: AllLectures 1080، EditLecture 1272، CreateLectuer 1136، EditLectuer 1336، DeleteLecture 1495.

## الملحق ج — الملفات بلا BOM في التغييرات الحالية

أزيل منها BOM: `Controllers/ControlpanelController.cs`، `Views/Shared/_AdminLayout.cshtml`، `Views/Teacher/LecturesByCategoryStudend.cshtml`.
جديدة بلا BOM: `Controllers/AdminNotificationsController.cs`، `Data/TeacherNotificationEntities.cs`، `Models/TeacherLectureNotificationViewModels.cs`، `Services/TeacherLectureNotificationService.cs`، `Views/AdminNotifications/Index.cshtml`.
موروثة بلا BOM (خارج هذه التغييرات): ملفات `Data/*.cs` المولَّدة من EDMX، `Models/Std_Reg_CategoryModel.cs`، `Models/StudentSubjectsViewModel.cs`، `Views/Students/StdSubjects.cshtml`.
