/* ==============================================================================================
   مشروع أكاديمية تبسيط الدولية - سكربت التنظيف الآمن للبيانات التجريبية
   الملف: 04_Cleanup_TestData.sql
   قاعدة البيانات المستهدفة: TabseetDB_Local (SQL Server محلي فقط)
   العلامة المرجعية: TEST_TABSEET_2026
   القاعدة الذهبية: حذف البيانات التجريبية حصراً دون المساس بأي بيانات أصلية
   ============================================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;

PRINT N'==============================================================================';
PRINT N'  بدء عملية التنظيف الآمن للبيانات التجريبية - TabseetDB_Local';
PRINT N'==============================================================================';

-- 1. صمام الأمان الإلزامي
IF (DB_NAME() <> N'TabseetDB_Local')
BEGIN
    RAISERROR(N'[إيقاف فوري] السكربت مخصص للتشغيل على TabseetDB_Local فقط!', 16, 1);
    RETURN;
END

-- 2. التحقق من وجود المانيفست
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'__TestDataManifest')
BEGIN
    PRINT N'[تنبيه] جدول المانيفست dbo.__TestDataManifest غير موجود. لا توجد بيانات تجريبية لحذفها.';
    RETURN;
END

DECLARE @TestRecordsCount INT = (SELECT COUNT(*) FROM dbo.__TestDataManifest WHERE Tag = N'TEST_TABSEET_2026');
IF (@TestRecordsCount = 0)
BEGIN
    PRINT N'[تنبيه] لا توجد أي سجلات تجريبية موسومة بـ TEST_TABSEET_2026 في المانيفست.';
    RETURN;
END

PRINT N'عدد السجلات التجريبية المستهدفة بالحذف الآمن: ' + CAST(@TestRecordsCount AS NVARCHAR(20));

BEGIN TRANSACTION;
BEGIN TRY

    -- حذف سجلات إتمام الاختبارات
    DELETE FROM dbo.Std_ExamFinish
    WHERE CAST([int] AS NVARCHAR(256)) IN (
        SELECT RecordId FROM dbo.__TestDataManifest WHERE TableName = N'Std_ExamFinish' AND Tag = N'TEST_TABSEET_2026'
    );
    PRINT N' [تم] تنظيف سجلات Std_ExamFinish.';

    -- حذف سجلات حضور المحاضرات
    DELETE FROM dbo.std_lecture
    WHERE CAST(ID AS NVARCHAR(256)) IN (
        SELECT RecordId FROM dbo.__TestDataManifest WHERE TableName = N'std_lecture' AND Tag = N'TEST_TABSEET_2026'
    );
    PRINT N' [تم] تنظيف سجلات std_lecture.';

    -- حذف تسجيلات الدورات
    DELETE FROM dbo.Std_Reg_Category
    WHERE CAST(ID AS NVARCHAR(256)) IN (
        SELECT RecordId FROM dbo.__TestDataManifest WHERE TableName = N'Std_Reg_Category' AND Tag = N'TEST_TABSEET_2026'
    );
    PRINT N' [تم] تنظيف سجلات Std_Reg_Category.';

    -- حذف روابط أسئلة الواجبات
    DELETE FROM dbo.Assignment_Q
    WHERE CAST(ID AS NVARCHAR(256)) IN (
        SELECT RecordId FROM dbo.__TestDataManifest WHERE TableName = N'Assignment_Q' AND Tag = N'TEST_TABSEET_2026'
    );
    PRINT N' [تم] تنظيف سجلات Assignment_Q.';

    -- حذف الواجبات
    DELETE FROM dbo.Assignments
    WHERE CAST(ID AS NVARCHAR(256)) IN (
        SELECT RecordId FROM dbo.__TestDataManifest WHERE TableName = N'Assignments' AND Tag = N'TEST_TABSEET_2026'
    );
    PRINT N' [تم] تنظيف سجلات Assignments.';

    -- حذف روابط أسئلة الاختبارات
    DELETE FROM dbo.Exam_Q
    WHERE CAST(ID AS NVARCHAR(256)) IN (
        SELECT RecordId FROM dbo.__TestDataManifest WHERE TableName = N'Exam_Q' AND Tag = N'TEST_TABSEET_2026'
    );
    PRINT N' [تم] تنظيف سجلات Exam_Q.';

    -- حذف الاختبارات
    DELETE FROM dbo.Exams
    WHERE CAST(ID AS NVARCHAR(256)) IN (
        SELECT RecordId FROM dbo.__TestDataManifest WHERE TableName = N'Exams' AND Tag = N'TEST_TABSEET_2026'
    );
    PRINT N' [تم] تنظيف سجلات Exams.';

    -- حذف إجابات وخيارات الأسئلة
    DELETE FROM dbo.Q_MultiChoice
    WHERE CAST(ID AS NVARCHAR(256)) IN (
        SELECT RecordId FROM dbo.__TestDataManifest WHERE TableName = N'Q_MultiChoice' AND Tag = N'TEST_TABSEET_2026'
    );

    DELETE FROM dbo.Q_TrueOrFolse
    WHERE CAST(ID AS NVARCHAR(256)) IN (
        SELECT RecordId FROM dbo.__TestDataManifest WHERE TableName = N'Q_TrueOrFolse' AND Tag = N'TEST_TABSEET_2026'
    );

    DELETE FROM dbo.Q_Matching
    WHERE CAST(ID AS NVARCHAR(256)) IN (
        SELECT RecordId FROM dbo.__TestDataManifest WHERE TableName = N'Q_Matching' AND Tag = N'TEST_TABSEET_2026'
    );

    DELETE FROM dbo.Q_FillVoid
    WHERE CAST(ID AS NVARCHAR(256)) IN (
        SELECT RecordId FROM dbo.__TestDataManifest WHERE TableName = N'Q_FillVoid' AND Tag = N'TEST_TABSEET_2026'
    );
    PRINT N' [تم] تنظيف جداول خيارات الأسئلة (Q_MultiChoice, Q_TrueOrFolse, Q_Matching, Q_FillVoid).';

    -- حذف الأسئلة
    DELETE FROM dbo.Questions
    WHERE CAST(ID AS NVARCHAR(256)) IN (
        SELECT RecordId FROM dbo.__TestDataManifest WHERE TableName = N'Questions' AND Tag = N'TEST_TABSEET_2026'
    );
    PRINT N' [تم] تنظيف سجلات Questions.';

    -- حذف المحاضرات
    DELETE FROM dbo.Lectures
    WHERE CAST(ID AS NVARCHAR(256)) IN (
        SELECT RecordId FROM dbo.__TestDataManifest WHERE TableName = N'Lectures' AND Tag = N'TEST_TABSEET_2026'
    );
    PRINT N' [تم] تنظيف سجلات Lectures.';

    -- حذف الدورات
    DELETE FROM dbo.Categories
    WHERE CAST(ID AS NVARCHAR(256)) IN (
        SELECT RecordId FROM dbo.__TestDataManifest WHERE TableName = N'Categories' AND Tag = N'TEST_TABSEET_2026'
    );
    PRINT N' [تم] تنظيف سجلات Categories.';

    -- حذف صفحات الشبكة والواجهة
    DELETE FROM dbo.NewGridPages
    WHERE CAST(ID AS NVARCHAR(256)) IN (
        SELECT RecordId FROM dbo.__TestDataManifest WHERE TableName = N'NewGridPages' AND Tag = N'TEST_TABSEET_2026'
    );

    DELETE FROM dbo.New_pages
    WHERE CAST(ID AS NVARCHAR(256)) IN (
        SELECT RecordId FROM dbo.__TestDataManifest WHERE TableName = N'New_pages' AND Tag = N'TEST_TABSEET_2026'
    );
    PRINT N' [تم] تنظيف صفحات NewGridPages و New_pages.';

    -- حذف الأقسام
    DELETE FROM dbo.CategoryTypes
    WHERE CAST(ID AS NVARCHAR(256)) IN (
        SELECT RecordId FROM dbo.__TestDataManifest WHERE TableName = N'CategoryTypes' AND Tag = N'TEST_TABSEET_2026'
    );
    PRINT N' [تم] تنظيف سجلات CategoryTypes.';

    -- حذف محتوى الموقع التجريبي
    DELETE FROM dbo.New_Slider
    WHERE CAST(ID AS NVARCHAR(256)) IN (
        SELECT RecordId FROM dbo.__TestDataManifest WHERE TableName = N'New_Slider' AND Tag = N'TEST_TABSEET_2026'
    );

    DELETE FROM dbo.New_LR
    WHERE CAST(ID AS NVARCHAR(256)) IN (
        SELECT RecordId FROM dbo.__TestDataManifest WHERE TableName = N'New_LR' AND Tag = N'TEST_TABSEET_2026'
    );

    DELETE FROM dbo.New_Home_Page
    WHERE CAST(ID AS NVARCHAR(256)) IN (
        SELECT RecordId FROM dbo.__TestDataManifest WHERE TableName = N'New_Home_Page' AND Tag = N'TEST_TABSEET_2026'
    );
    PRINT N' [تم] تنظيف محتوى الموقع (New_Home_Page, New_LR, New_Slider).';

    -- حذف تعيينات أدوار المستخدمين التجريبيين
    DELETE FROM dbo.AspNetUserRoles
    WHERE AspNetUsers_Id LIKE N'TEST-USR-%';
    PRINT N' [تم] تنظيف تعيينات AspNetUserRoles للحسابات التجريبية.';

    -- حذف حسابات المستخدمين التجريبيين (المدير، المدرسين، الطلاب)
    DELETE FROM dbo.AspNetUsers
    WHERE Id LIKE N'TEST-USR-%' OR Email LIKE N'%@example.test';
    PRINT N' [تم] تنظيف حسابات AspNetUsers التجريبية.';

    -- تفريغ سجلات المانيفست الخاصة بالعلامة
    DELETE FROM dbo.__TestDataManifest
    WHERE Tag = N'TEST_TABSEET_2026';
    PRINT N' [تم] إفراغ سجلات المانيفست.';

    -- التحقق الأخير من بقاء الحساب الأصلي
    IF NOT EXISTS (SELECT 1 FROM dbo.AspNetUsers WHERE Email = N'jallalalomary@gmail.com')
    BEGIN
        RAISERROR(N'[خطأ أمني فادح] الحساب الأصلي فُقد! تراجع فوري عن العملية.', 16, 1);
    END

    COMMIT TRANSACTION;

    PRINT N'==============================================================================';
    PRINT N'  اكتمل تنظيف البيانات التجريبية بنجاح تام! عادت قاعدة البيانات لحالتها السابقة.';
    PRINT N'==============================================================================';

END TRY
BEGIN CATCH
    IF (XACT_STATE() <> 0)
    BEGIN
        ROLLBACK TRANSACTION;
        PRINT N'[تراجع تلقائي] تم التراجع عن عملية التنظيف بسبب حدوث خطأ.';
    END

    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    PRINT N'[تفاصيل الخطأ]: ' + @ErrMsg;
    RAISERROR(@ErrMsg, 16, 1);
END CATCH
GO

