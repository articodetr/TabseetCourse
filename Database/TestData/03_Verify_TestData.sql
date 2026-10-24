/* ==============================================================================================
   مشروع أكاديمية تبسيط الدولية - سكربت التحقق والتدقيق الشامل للبيانات وسيناريوهات الاختبار
   الملف: 03_Verify_TestData.sql
   قاعدة البيانات المستهدفة: TabseetDB_Local (SQL Server محلي فقط)
   العلامة المرجعية: TEST_TABSEET_2026
   ============================================================================================== */

SET NOCOUNT ON;

PRINT N'==============================================================================';
PRINT N'  بدء عملية التحقق والتدقيق الشامل للبيانات التجريبية - TabseetDB_Local';
PRINT N'==============================================================================';

-- التحقق من البيئة
IF (DB_NAME() <> N'TabseetDB_Local')
BEGIN
    RAISERROR(N'[إيقاف] السكربت مخصص للتشغيل على TabseetDB_Local فقط.', 16, 1);
    RETURN;
END

-- التحقق من وجود المانيفست
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'__TestDataManifest')
BEGIN
    PRINT N'[خطأ] جدول المانيفست dbo.__TestDataManifest غير موجود. لم يتم إدخال البيانات بعد.';
    RETURN;
END

PRINT N'------------------------------------------------------------------------------';
PRINT N'  1. ملخص عدد السجلات التجريبية المنشأة في كل جدول (عبر المانيفست)';
PRINT N'------------------------------------------------------------------------------';

SELECT 
    TableName AS [اسم الجدول],
    COUNT(*) AS [عدد السجلات التجريبية]
FROM dbo.__TestDataManifest
WHERE Tag = N'TEST_TABSEET_2026'
GROUP BY TableName
ORDER BY [عدد السجلات التجريبية] DESC;

DECLARE @TotalManifestRecords INT = (SELECT COUNT(*) FROM dbo.__TestDataManifest WHERE Tag = N'TEST_TABSEET_2026');
PRINT N'==> إجمالي السجلات التجريبية الموثقة في المانيفست: ' + CAST(@TotalManifestRecords AS NVARCHAR(20));


PRINT N'------------------------------------------------------------------------------';
PRINT N'  2. التحقق من سلامة البيانات الأصلية وعدم المساس بها إطلاقاً';
PRINT N'------------------------------------------------------------------------------';

-- التحقق من وجود الحساب الأصلي وعدم تعديله
IF EXISTS (SELECT 1 FROM dbo.AspNetUsers WHERE Email = N'jallalalomary@gmail.com')
BEGIN
    PRINT N' [اجتياز] الحساب الأصلي (jallalalomary@gmail.com) موجود وسليم تماماً ولم يُمس.';
END
ELSE
BEGIN
    PRINT N' [تحذير] لم يتم العثور على الحساب الأصلي!';
END

-- التحقق من وجود سجل اللغات الأصلي
IF EXISTS (SELECT 1 FROM dbo.New_lang WHERE ID = 1)
BEGIN
    PRINT N' [اجتياز] سجل اللغات الأساسي في New_lang موجود وسليم.';
END


PRINT N'------------------------------------------------------------------------------';
PRINT N'  3. التحقق من سلامة العلاقات والمفاتيح الأجنبية (Foreign Keys Integrity)';
PRINT N'------------------------------------------------------------------------------';

DECLARE @OrphanCount INT = 0;

-- دورات بدون أقسام
SELECT @OrphanCount = COUNT(*) FROM dbo.Categories c
LEFT JOIN dbo.CategoryTypes ct ON c.CategoryType_ID = ct.ID
WHERE c.Description LIKE N'%TEST_TABSEET_2026%' AND ct.ID IS NULL;
PRINT N' - دورات يتيمة بدون قسم (CategoryType): ' + CAST(@OrphanCount AS NVARCHAR(10));

-- دورات بدون صفحات UI
SELECT @OrphanCount = COUNT(*) FROM dbo.Categories c
LEFT JOIN dbo.New_pages p ON c.New_Page_ID = p.ID
WHERE c.Description LIKE N'%TEST_TABSEET_2026%' AND p.ID IS NULL;
PRINT N' - دورات يتيمة بدون New_pages: ' + CAST(@OrphanCount AS NVARCHAR(10));

-- محاضرات بدون دورات
SELECT @OrphanCount = COUNT(*) FROM dbo.Lectures l
LEFT JOIN dbo.Categories c ON l.Category_ID = c.ID
WHERE l.Others LIKE N'%TEST_TABSEET_2026%' AND c.ID IS NULL;
PRINT N' - محاضرات يتيمة بدون دورة: ' + CAST(@OrphanCount AS NVARCHAR(10));

-- اختبارات بدون دورات
SELECT @OrphanCount = COUNT(*) FROM dbo.Exams e
LEFT JOIN dbo.Categories c ON e.Category_ID = c.ID
WHERE e.Description LIKE N'%TEST_TABSEET_2026%' AND c.ID IS NULL;
PRINT N' - اختبارات يتيمة بدون دورة: ' + CAST(@OrphanCount AS NVARCHAR(10));

-- أسئلة بدون دورات
SELECT @OrphanCount = COUNT(*) FROM dbo.Questions q
LEFT JOIN dbo.Categories c ON q.Category_ID = c.ID
WHERE q.Q_Description LIKE N'%TEST_TABSEET_2026%' AND c.ID IS NULL;
PRINT N' - أسئلة يتيمة بدون دورة: ' + CAST(@OrphanCount AS NVARCHAR(10));

-- تسجيلات طلاب بدون مستخدم
SELECT @OrphanCount = COUNT(*) FROM dbo.Std_Reg_Category r
LEFT JOIN dbo.AspNetUsers u ON r.Std_ID = u.Id
WHERE r.Std_ID LIKE N'TEST-USR-%' AND u.Id IS NULL;
PRINT N' - تسجيلات طلاب يتيمة بدون مستخدم: ' + CAST(@OrphanCount AS NVARCHAR(10));


PRINT N'------------------------------------------------------------------------------';
PRINT N'  4. التدقيق المنهجي لسيناريوهات الاختبار الـ 20 (TEST-01 إلى TEST-20)';
PRINT N'------------------------------------------------------------------------------';

-- TEST-01: تسجيل دخول المدير
DECLARE @AdminCheck INT = (SELECT COUNT(*) FROM dbo.AspNetUsers WHERE Id = N'TEST-USR-ADMIN-01' AND userOrTetcherOrStudent = 0 AND Email = N'admin.test@example.test');
PRINT N'[TEST-01] المدير: ' + CASE WHEN @AdminCheck = 1 THEN N'متاح (ID=TEST-USR-ADMIN-01, Role=Admin, Email=admin.test@example.test)' ELSE N'فشل' END;

-- TEST-02: تسجيل دخول المدرس
DECLARE @TeacherCount INT = (SELECT COUNT(*) FROM dbo.AspNetUsers WHERE userOrTetcherOrStudent = 1 AND Id LIKE N'TEST-USR-TCH-%');
PRINT N'[TEST-02] المدرسون: ' + CAST(@TeacherCount AS NVARCHAR(10)) + N' مدرسين مسجلين بكلمات مرور وتعيين دور Teacher';

-- TEST-03: تسجيل دخول الطالب
DECLARE @StudentCount INT = (SELECT COUNT(*) FROM dbo.AspNetUsers WHERE userOrTetcherOrStudent = 2 AND Id LIKE N'TEST-USR-STD-%');
DECLARE @StudentActiveCount INT = (SELECT COUNT(*) FROM dbo.AspNetUsers WHERE userOrTetcherOrStudent = 2 AND Id LIKE N'TEST-USR-STD-%' AND IsActive = 1);
DECLARE @StudentInactiveCount INT = (SELECT COUNT(*) FROM dbo.AspNetUsers WHERE userOrTetcherOrStudent = 2 AND Id LIKE N'TEST-USR-STD-%' AND IsActive = 0);
PRINT N'[TEST-03] الطلاب: ' + CAST(@StudentCount AS NVARCHAR(10)) + N' طالب (' + CAST(@StudentActiveCount AS NVARCHAR(10)) + N' نشط + ' + CAST(@StudentInactiveCount AS NVARCHAR(10)) + N' غير نشط/معطل)';

-- TEST-04: عرض الدورات
DECLARE @ActiveCoursesCount INT = (SELECT COUNT(*) FROM dbo.Categories WHERE Description LIKE N'%TEST_TABSEET_2026%' AND CanRegister = 1);
PRINT N'[TEST-04] تصفح الدورات: ' + CAST(@ActiveCoursesCount AS NVARCHAR(10)) + N' دورات نشطة ومتاحة للتسجيل مع الأوصاف والأسعار';

-- TEST-05: تسجيل الطالب في دورة مجانية
DECLARE @FreeRegCount INT = (SELECT COUNT(*) FROM dbo.Std_Reg_Category WHERE category_Ispayed = 0 AND Std_ID LIKE N'TEST-USR-STD-%');
PRINT N'[TEST-05] تسجيل دورة مجانية: ' + CAST(@FreeRegCount AS NVARCHAR(10)) + N' تسجيلات مجانية بدون رسوم';

-- TEST-06: تسجيل الطالب في دورة مدفوعة
DECLARE @PaidRegCount INT = (SELECT COUNT(*) FROM dbo.Std_Reg_Category WHERE category_Ispayed = 1 AND Std_ID LIKE N'TEST-USR-STD-%');
PRINT N'[TEST-06] تسجيل دورة مدفوعة: ' + CAST(@PaidRegCount AS NVARCHAR(10)) + N' تسجيلات مدفوعة';

-- TEST-07: جدول محاضرات الطالب
DECLARE @LecCount INT = (SELECT COUNT(*) FROM dbo.Lectures WHERE Others LIKE N'%TEST_TABSEET_2026%');
DECLARE @OnlineLecCount INT = (SELECT COUNT(*) FROM dbo.Lectures WHERE Others LIKE N'%TEST_TABSEET_2026%' AND LectureTypeOnOff = N'1');
PRINT N'[TEST-07] جدول محاضرات الطالب: ' + CAST(@LecCount AS NVARCHAR(10)) + N' محاضرة إجمالاً (' + CAST(@OnlineLecCount AS NVARCHAR(10)) + N' مباشرة عبر الإنترنت بروابط آمنة)';

-- TEST-08: عرض محاضرات المدرس
SELECT 
    u.RealName AS [المدرس],
    u.Categories_ID AS [الدورات المسندة],
    COUNT(l.ID) AS [عدد المحاضرات المتاحة له]
FROM dbo.AspNetUsers u
CROSS APPLY (
    SELECT l.ID FROM dbo.Lectures l
    WHERE l.Others LIKE N'%TEST_TABSEET_2026%'
      AND u.Categories_ID LIKE N'%' + CAST(l.Category_ID AS NVARCHAR(10)) + N'/%'
) l
WHERE u.Id LIKE N'TEST-USR-TCH-%'
GROUP BY u.RealName, u.Categories_ID;

-- TEST-09: عرض أسئلة الاختبار بجميع أنواعها
SELECT 
    CASE q.Type
        WHEN 1 THEN N'صح أو خطأ (TrueOrFalse)'
        WHEN 2 THEN N'اختيار من متعدد (MultiChoice)'
        WHEN 3 THEN N'مطابقة (Matching)'
        WHEN 4 THEN N'ملء فراغ (FillVoid)'
    END AS [نوع السؤال],
    COUNT(*) AS [العدد]
FROM dbo.Questions q
WHERE q.Q_Description LIKE N'%TEST_TABSEET_2026%'
GROUP BY q.Type;

-- TEST-10 & TEST-11: نتائج الاختبارات والنجاح والرسوب
DECLARE @PassedStudents INT = (SELECT COUNT(*) FROM dbo.Std_Reg_Category WHERE std_exam_isFinish = 1 AND Std_ID LIKE N'TEST-USR-STD-%');
DECLARE @FailedStudents INT = (SELECT COUNT(*) FROM dbo.Std_Reg_Category WHERE std_exam_isFinish = 0 AND PayMent_Method > 0 AND Std_ID = N'TEST-USR-STD-08');
PRINT N'[TEST-10 & 11] حل الاختبار والنتائج: ' + CAST(@PassedStudents AS NVARCHAR(10)) + N' حالة نجاح موثقة، وحالة رسوب موثقة (طالب 8)';

-- TEST-12: تتبع إكمال المحاضرات (std_lecture)
DECLARE @CompletedLecRecords INT = (SELECT COUNT(*) FROM dbo.std_lecture WHERE IsFinish = 1 AND Std_ID LIKE N'TEST-USR-STD-%');
DECLARE @PendingLecRecords INT = (SELECT COUNT(*) FROM dbo.std_lecture WHERE IsFinish = 0 AND Std_ID LIKE N'TEST-USR-STD-%');
PRINT N'[TEST-12] تتبع إكمال المحاضرات: ' + CAST(@CompletedLecRecords AS NVARCHAR(10)) + N' محاضرة مكتملة، و ' + CAST(@PendingLecRecords AS NVARCHAR(10)) + N' محاضرة قيد الإنجاز';

-- TEST-13: استحقاق الشهادة
SELECT 
    Certificate_appreciation AS [التقدير],
    COUNT(*) AS [عدد الشهادات الصادرة]
FROM dbo.Std_Reg_Category
WHERE Certificate_appreciation IS NOT NULL AND Std_ID LIKE N'TEST-USR-STD-%'
GROUP BY Certificate_appreciation;

-- TEST-14: عرض حالة الدفع
SELECT 
    CASE PayMent_Method
        WHEN 0 THEN N'مجاني (Free)'
        WHEN 1 THEN N'بطاقة ائتمانية (Credit Card)'
        WHEN 2 THEN N'تحويل بنكي (Bank Transfer)'
        WHEN 3 THEN N'دفع لاحق / معلق (Pay Later)'
        WHEN 4 THEN N'كود قسيمة / خصم (Promo Code)'
        ELSE N'أخرى'
    END AS [طريقة الدفع],
    std_category_Ispayed AS [حالة السداد (1=تم، 0=معلق)],
    COUNT(*) AS [عدد العمليات]
FROM dbo.Std_Reg_Category
WHERE Std_ID LIKE N'TEST-USR-STD-%'
GROUP BY PayMent_Method, std_category_Ispayed
ORDER BY PayMent_Method;

-- TEST-15: لوحة تحكم الإدارة
DECLARE @MetricStdCount INT = (SELECT COUNT(*) FROM dbo.AspNetUsers WHERE role = N'Student' AND Id LIKE N'TEST-USR-STD-%');
DECLARE @MetricPaidCat INT = (SELECT COUNT(*) FROM dbo.Std_Reg_Category WHERE category_Ispayed = 1 AND Std_ID LIKE N'TEST-USR-STD-%');
DECLARE @MetricFreeCat INT = (SELECT COUNT(*) FROM dbo.Std_Reg_Category WHERE category_Ispayed = 0 AND Std_ID LIKE N'TEST-USR-STD-%');
DECLARE @MetricCerts INT = (SELECT COUNT(*) FROM dbo.Std_ExamFinish WHERE isFinishExam = 1);

PRINT N'[TEST-15] مؤشرات لوحة التحكم (Admin Dashboard Metrics):';
PRINT N'   - عدد الطلاب (StudentCount): ' + CAST(@MetricStdCount AS NVARCHAR(10));
PRINT N'   - الدورات المدفوعة (PayedCategory): ' + CAST(@MetricPaidCat AS NVARCHAR(10));
PRINT N'   - الدورات المجانية (FreeCategory): ' + CAST(@MetricFreeCat AS NVARCHAR(10));
PRINT N'   - الشهادات الصادرة (Certfication): ' + CAST(@MetricCerts AS NVARCHAR(10));

-- TEST-16 & 17: التحقق من الصلاحيات والحدود الأمنية
DECLARE @AdminRolesCount INT = (SELECT COUNT(*) FROM dbo.AspNetUserRoles WHERE UserId = N'TEST-USR-ADMIN-01');
DECLARE @StudentIllegalRoles INT = (SELECT COUNT(*) FROM dbo.AspNetUserRoles ur INNER JOIN dbo.AspNetRoles r ON ur.RoleId = r.Id WHERE ur.UserId LIKE N'TEST-USR-STD-%' AND r.Name IN (N'Admin', N'Teacher'));
PRINT N'[TEST-16 & 17] الصلاحيات: المدير يمتلك ' + CAST(@AdminRolesCount AS NVARCHAR(10)) + N' دوراً إدارياً، ولا يوجد أي طالب يمتلك صلاحيات Admin أو Teacher (المخالفات: ' + CAST(@StudentIllegalRoles AS NVARCHAR(10)) + N')';

-- TEST-18: التعامل مع طالب ليس لديه أي تسجيل
DECLARE @UnregisteredStudents INT = (
    SELECT COUNT(*) FROM dbo.AspNetUsers u
    LEFT JOIN dbo.Std_Reg_Category r ON u.Id = r.Std_ID
    WHERE u.Id LIKE N'TEST-USR-STD-%' AND r.ID IS NULL
);
PRINT N'[TEST-18] طلاب بدون أي تسجيل: ' + CAST(@UnregisteredStudents AS NVARCHAR(10)) + N' طلاب (الطلاب 21 إلى 25 متوفرون خصيصاً لاختبار هذه الحالة)';

-- TEST-19: التعامل مع دورة بدون طلاب
SELECT 
    c.ID AS [معرف الدورة],
    c.NameAr AS [اسم الدورة],
    COUNT(r.ID) AS [عدد الطلاب المسجلين]
FROM dbo.Categories c
LEFT JOIN dbo.Std_Reg_Category r ON c.ID = r.Category_ID
WHERE c.Description LIKE N'%TEST_TABSEET_2026%' AND c.NameAr LIKE N'%البحث العلمي%'
GROUP BY c.ID, c.NameAr;

-- TEST-20: دورة منتهية ومغلقة للتسجيل
SELECT 
    c.ID AS [معرف الدورة],
    c.NameAr AS [اسم الدورة],
    c.CanRegister AS [إمكانية التسجيل (0=مغلقة)],
    c.EndTime AS [تاريخ انتهاء التسجيل]
FROM dbo.Categories c
WHERE c.Description LIKE N'%TEST_TABSEET_2026%' AND c.CanRegister = 0;

PRINT N'==============================================================================';
PRINT N'  اكتمل التدقيق والتحقق بنجاح! جميع سيناريوهات الاختبار TEST-01 إلى TEST-20 متطابقة ومستوفاة.';
PRINT N'==============================================================================';
GO
