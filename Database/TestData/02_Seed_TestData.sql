/* ==============================================================================================
   مشروع أكاديمية تبسيط الدولية - السكربت الشامل لإدخال البيانات التجريبية
   الملف: 02_Seed_TestData.sql
   قاعدة البيانات المستهدفة: TabseetDB_Local (SQL Server محلي حصراً)
   العلامة المرجعية الموحدة: TEST_TABSEET_2026
   معاملة موحدة آمنة: Transaction with XACT_ABORT ON
   ============================================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;

PRINT N'==============================================================================';
PRINT N'  بدء عملية إدخال البيانات التجريبية (TestData Seeder) - TabseetDB_Local';
PRINT N'==============================================================================';

-- 1. صمام الأمان الإلزامي - التحقق الحاسم من البيئة
IF (DB_NAME() <> N'TabseetDB_Local')
BEGIN
    RAISERROR(N'[إيقاف فوري] السكربت مخصص للتشغيل على TabseetDB_Local فقط! تم إلغاء العملية.', 16, 1);
    RETURN;
END

-- 2. التحقق من عدم وجود بيانات تجريبية سابقة بنفس العلامة
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'__TestDataManifest')
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.__TestDataManifest WHERE Tag = N'TEST_TABSEET_2026')
    BEGIN
        PRINT N'[تنبيه] البيانات التجريبية TEST_TABSEET_2026 مدخلة مسبقاً.';
        PRINT N'لإعادة إدخالها من جديد، يرجى تنفيذ سكربت التنظيف 04_Cleanup_TestData.sql أولاً.';
        RETURN;
    END
END
ELSE
BEGIN
    CREATE TABLE dbo.__TestDataManifest (
        ManifestID INT IDENTITY(1,1) PRIMARY KEY,
        TableName NVARCHAR(100) NOT NULL,
        RecordId NVARCHAR(256) NOT NULL,
        Tag NVARCHAR(50) NOT NULL DEFAULT N'TEST_TABSEET_2026',
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
    );
    CREATE INDEX IX_Manifest_Tag ON dbo.__TestDataManifest (Tag, TableName);
END

BEGIN TRANSACTION;
BEGIN TRY

    PRINT N'[1/14] تجهيز وإدخال الأدوار والصلاحيات (AspNetRoles)...';

    DECLARE @RolesTable TABLE (RoleName NVARCHAR(256));
    INSERT INTO @RolesTable (RoleName) VALUES
        (N'Admin'), (N'Teacher'), (N'Student'),
        (N'CategoriesView'), (N'CategoriesEdit'), (N'CategoriesDelete'),
        (N'LectureView'), (N'LectureEdit'), (N'LectureDelete'),
        (N'QuestionBankView'), (N'QuestionBankEdit'), (N'QuestionBankDelete'),
        (N'UsersTeachersView'), (N'UsersTeachersEdit'), (N'UsersTeachersDelete'),
        (N'UsersStudentView'), (N'UsersStudentEdit'), (N'UsersStudentDelete'),
        (N'AssingmentView'), (N'AssingmentEdit'), (N'AssingmentDelete'),
        (N'PaymentView'), (N'PaymentActive'),
        (N'Chat'), (N'CodeGenerate'),
        (N'ExamView'), (N'ExamEdit'), (N'ExamDelet');

    DECLARE @InsertedRoles TABLE (Id NVARCHAR(128), Name NVARCHAR(256));

    MERGE INTO dbo.AspNetRoles AS target
    USING @RolesTable AS source
    ON target.Name = source.RoleName
    WHEN NOT MATCHED THEN
        INSERT (Id, Name)
        VALUES (CAST(NEWID() AS NVARCHAR(128)), source.RoleName)
    OUTPUT inserted.Id, inserted.Name INTO @InsertedRoles;

    INSERT INTO dbo.__TestDataManifest (TableName, RecordId, Tag)
    SELECT N'AspNetRoles', Id, N'TEST_TABSEET_2026' FROM @InsertedRoles;

    DECLARE @RoleCount INT = (SELECT COUNT(*) FROM @RolesTable);
    PRINT N'   ==> تم تجهيز ' + CAST(@RoleCount AS NVARCHAR(10)) + N' دوراً في AspNetRoles.';


    -- كلمات مرور Identity مشفرة مسبقاً بنفس PasswordHasher الخاص بـ ASP.NET Identity v2:
    -- Admin@Tabseet2026!  => ADqqGAdEXdUimDidprGBSOBDWsC+W4R5q6i3ntMFL0f9dhWZVWJbH47rd59dhMvg5Q==
    -- Teach@Tabseet2026!  => AF3OK5u8LxP1ny5AKOqvYenRtSY4u79gpvrlo+T23/hKQcctbu0PfObeQ0o4hBgr9A==
    -- Std@Tabseet2026!    => ALptFqsixHLjmBRThZZZcLDgHR3b3dcYZsvHACrEzekExXbHQzao3CAI9vnGZtiFaQ==
    DECLARE @AdminHash NVARCHAR(MAX)   = N'ADqqGAdEXdUimDidprGBSOBDWsC+W4R5q6i3ntMFL0f9dhWZVWJbH47rd59dhMvg5Q==';
    DECLARE @TeacherHash NVARCHAR(MAX) = N'AF3OK5u8LxP1ny5AKOqvYenRtSY4u79gpvrlo+T23/hKQcctbu0PfObeQ0o4hBgr9A==';
    DECLARE @StudentHash NVARCHAR(MAX) = N'ALptFqsixHLjmBRThZZZcLDgHR3b3dcYZsvHACrEzekExXbHQzao3CAI9vnGZtiFaQ==';

    PRINT N'[2/14] إدخال حسابات الإدارة والمدرسين والطلاب (AspNetUsers)...';

    -- حساب المدير التجريبي
    INSERT INTO dbo.AspNetUsers (
        Id, UserName, Email, EmailConfirmed, PasswordHash, SecurityStamp, PhoneNumber, PhoneNumberConfirmed,
        TwoFactorEnabled, LockoutEnabled, AccessFailedCount, FirstName, LastName, RealName, Password,
        role, userOrTetcherOrStudent, IsActive, Country, Location, Education, Experience, Studing
    ) VALUES (
        N'TEST-USR-ADMIN-01', N'ADM900101', N'admin.test@example.test', 1, @AdminHash, CAST(NEWID() AS NVARCHAR(128)),
        N'+966501112233', 1, 0, 0, 0, N'عبد الرحمن', N'المنصور', N'أ. د. عبد الرحمن المنصور', N'Admin@Tabseet2026!',
        N'/Admin/CategoriesView/CategoriesEdit/CategoriesDelete/LectureView/LectureEdit/LectureDelete/QuestionBankView/QuestionBankEdit/QuestionBankDelete/UsersTeachersView/UsersTeachersEdit/UsersTeachersDelete/UsersStudentView/UsersStudentEdit/UsersStudentDelete/AssingmentView/AssingmentEdit/AssingmentDelete/PaymentView/PaymentActive/Chat/CodeGenerate/ExamView/ExamEdit/ExamDelet',
        0, 1, N'السعودية', N'الرياض - المقر الرئيسي للأكاديمية [TEST_TABSEET_2026]',
        N'دكتوراه في الإدارة التربوية والتعليم الإلكتروني', N'خبرة 18 عاماً في إدارة المنصات التعليمية الدولية', N'جامعة الملك سعود'
    );
    INSERT INTO dbo.__TestDataManifest (TableName, RecordId) VALUES (N'AspNetUsers', N'TEST-USR-ADMIN-01');

    -- إسناد الأدوار للمدير
    INSERT INTO dbo.AspNetUserRoles (RoleId, UserId)
    SELECT r.Id, N'TEST-USR-ADMIN-01'
    FROM dbo.AspNetRoles r
    WHERE r.Name IN (
        N'Admin', N'CategoriesView', N'CategoriesEdit', N'CategoriesDelete',
        N'LectureView', N'LectureEdit', N'LectureDelete',
        N'QuestionBankView', N'QuestionBankEdit', N'QuestionBankDelete',
        N'UsersTeachersView', N'UsersTeachersEdit', N'UsersTeachersDelete',
        N'UsersStudentView', N'UsersStudentEdit', N'UsersStudentDelete',
        N'AssingmentView', N'AssingmentEdit', N'AssingmentDelete',
        N'PaymentView', N'PaymentActive', N'Chat', N'CodeGenerate',
        N'ExamView', N'ExamEdit', N'ExamDelet'
    );
    INSERT INTO dbo.__TestDataManifest (TableName, RecordId)
    SELECT N'AspNetUserRoles', r.Id + N'/' + N'TEST-USR-ADMIN-01'
    FROM dbo.AspNetRoles r
    WHERE r.Name IN (N'Admin', N'CategoriesView', N'LectureView', N'QuestionBankView', N'UsersTeachersView', N'UsersStudentView', N'AssingmentView', N'PaymentView', N'ExamView');

    -- حسابات المدرسين الـ 6
    INSERT INTO dbo.AspNetUsers (
        Id, UserName, Email, EmailConfirmed, PasswordHash, SecurityStamp, PhoneNumber, PhoneNumberConfirmed,
        TwoFactorEnabled, LockoutEnabled, AccessFailedCount, FirstName, LastName, RealName, Password,
        role, userOrTetcherOrStudent, IsActive, Country, Location, Education, Experience, Studing
    ) VALUES
    (N'TEST-USR-TCH-01', N'TCH900201', N'teacher.ibrahim@example.test', 1, @TeacherHash, CAST(NEWID() AS NVARCHAR(128)),
     N'+966502223344', 1, 0, 0, 0, N'إبراهيم', N'النجار', N'د. إبراهيم خليل النجار', N'Teach@Tabseet2026!',
     N'Teacher', 1, 1, N'السعودية', N'المدينة المنورة [TEST_TABSEET_2026]',
     N'دكتوراه في الدراسات القرآنية والقراءات العشر', N'إجازة بالسند المتصل وخبرة 15 عاماً في الإقراء', N'الجامعة الإسلامية بالمدينة المنورة'),

    (N'TEST-USR-TCH-02', N'TCH900202', N'teacher.somaya@example.test', 1, @TeacherHash, CAST(NEWID() AS NVARCHAR(128)),
     N'+966503334455', 1, 0, 0, 0, N'سمية', N'الشنقيطي', N'أ. سمية أحمد الشنقيطي', N'Teach@Tabseet2026!',
     N'Teacher', 1, 1, N'السعودية', N'جدة [TEST_TABSEET_2026]',
     N'ماجستير في أصول التربية الإسلامية وإجازة في القاعدة النورانية', N'خبرة 10 سنوات في تأسيس الأطفال والمبتدئين', N'جامعة أم القرى'),

    (N'TEST-USR-TCH-03', N'TCH900203', N'teacher.mahmoud@example.test', 1, @TeacherHash, CAST(NEWID() AS NVARCHAR(128)),
     N'+201011223344', 1, 0, 0, 0, N'محمود', N'البغدادي', N'د. محمود فاروق البغدادي', N'Teach@Tabseet2026!',
     N'Teacher', 1, 1, N'مصر', N'القاهرة [TEST_TABSEET_2026]',
     N'دكتوراه في اللغة العربية وآدابها (نحو وصرف)', N'أستاذ النحو والبلاغة وخبرة 14 عاماً بالتدريس الجامعي', N'جامعة الأزهر الشريف'),

    (N'TEST-USR-TCH-04', N'TCH900204', N'teacher.sarah@example.test', 1, @TeacherHash, CAST(NEWID() AS NVARCHAR(128)),
     N'+971501234567', 1, 0, 0, 0, N'سارة', N'المهيري', N'أ. سارة ديفيد المهيري', N'Teach@Tabseet2026!',
     N'Teacher', 1, 1, N'الإمارات', N'دبي [TEST_TABSEET_2026]',
     N'Master in TESOL & Applied Linguistics', N'Cert TESOL certified trainer, 8 years teaching ESL', N'University of Leeds'),

    (N'TEST-USR-TCH-05', N'TCH900205', N'teacher.tariq@example.test', 1, @TeacherHash, CAST(NEWID() AS NVARCHAR(128)),
     N'+962791122334', 1, 0, 0, 0, N'طارق', N'القاضي', N'م. طارق زياد القاضي', N'Teach@Tabseet2026!',
     N'Teacher', 1, 1, N'الأردن', N'عمان [TEST_TABSEET_2026]',
     N'ماجستير علوم الحاسوب وهندسة البرمجيات', N'مهندس معماري سحابي واستشاري برمجي وخبرة 12 سنة', N'جامعة العلوم والتكنولوجيا الأردنية'),

    (N'TEST-USR-TCH-06', N'TCH900206', N'teacher.huda@example.test', 1, @TeacherHash, CAST(NEWID() AS NVARCHAR(128)),
     N'+212611223344', 1, 0, 0, 0, N'هدى', N'التلمساني', N'د. هدى عمر التلمساني', N'Teach@Tabseet2026!',
     N'Teacher', 1, 1, N'المغرب', N'الرباط [TEST_TABSEET_2026]',
     N'دكتوراه في الرياضيات ومناهج البحث العلمي والدراسات اللغوية', N'باحثة أكاديمية وأستاذة جامعية ومترجمة معتمدة', N'جامعة محمد الخامس');

    INSERT INTO dbo.__TestDataManifest (TableName, RecordId)
    VALUES 
        (N'AspNetUsers', N'TEST-USR-TCH-01'), (N'AspNetUsers', N'TEST-USR-TCH-02'),
        (N'AspNetUsers', N'TEST-USR-TCH-03'), (N'AspNetUsers', N'TEST-USR-TCH-04'),
        (N'AspNetUsers', N'TEST-USR-TCH-05'), (N'AspNetUsers', N'TEST-USR-TCH-06');

    -- إسناد دور المعلم للمدرسين الستة
    DECLARE @TeacherRoleId NVARCHAR(128) = (SELECT TOP 1 Id FROM dbo.AspNetRoles WHERE Name = N'Teacher');
    INSERT INTO dbo.AspNetUserRoles (RoleId, UserId)
    VALUES
        (@TeacherRoleId, N'TEST-USR-TCH-01'), (@TeacherRoleId, N'TEST-USR-TCH-02'),
        (@TeacherRoleId, N'TEST-USR-TCH-03'), (@TeacherRoleId, N'TEST-USR-TCH-04'),
        (@TeacherRoleId, N'TEST-USR-TCH-05'), (@TeacherRoleId, N'TEST-USR-TCH-06');

    INSERT INTO dbo.__TestDataManifest (TableName, RecordId)
    VALUES 
        (N'AspNetUserRoles', @TeacherRoleId + N'/TEST-USR-TCH-01'),
        (N'AspNetUserRoles', @TeacherRoleId + N'/TEST-USR-TCH-02'),
        (N'AspNetUserRoles', @TeacherRoleId + N'/TEST-USR-TCH-03'),
        (N'AspNetUserRoles', @TeacherRoleId + N'/TEST-USR-TCH-04'),
        (N'AspNetUserRoles', @TeacherRoleId + N'/TEST-USR-TCH-05'),
        (N'AspNetUserRoles', @TeacherRoleId + N'/TEST-USR-TCH-06');

    -- إدخال 30 طالباً (26 نشط + 4 غير نشط لاختبار حالات التعطيل)
    DECLARE @StudentRoleId NVARCHAR(128) = (SELECT TOP 1 Id FROM dbo.AspNetRoles WHERE Name = N'Student');

    DECLARE @StudentsData TABLE (
        Seq INT IDENTITY(1,1),
        Id NVARCHAR(128),
        UserName NVARCHAR(256),
        Email NVARCHAR(256),
        FirstName NVARCHAR(100),
        LastName NVARCHAR(100),
        RealName NVARCHAR(200),
        Country NVARCHAR(100),
        IsActive INT
    );

    INSERT INTO @StudentsData (Id, UserName, Email, FirstName, LastName, RealName, Country, IsActive) VALUES
    (N'TEST-USR-STD-01', N'STD900301', N'student01@example.test', N'عمر', N'السبيعي', N'عمر فهد السبيعي', N'السعودية', 1),
    (N'TEST-USR-STD-02', N'STD900302', N'student02@example.test', N'فاطمة', N'الإدريسي', N'فاطمة الزهراء الإدريسي', N'المغرب', 1),
    (N'TEST-USR-STD-03', N'STD900303', N'student03@example.test', N'زياد', N'العتيبي', N'زياد بدر العتيبي', N'السعودية', 1),
    (N'TEST-USR-STD-04', N'STD900304', N'student04@example.test', N'مريم', N'المنصوري', N'مريم خالد المنصوري', N'الإمارات', 1),
    (N'TEST-USR-STD-05', N'STD900305', N'student05@example.test', N'يوسف', N'عبد الرحمن', N'يوسف أحمد عبد الرحمن', N'مصر', 1),
    (N'TEST-USR-STD-06', N'STD900306', N'student06@example.test', N'عائشة', N'الكواري', N'عائشة ناصر الكواري', N'قطر', 1),
    (N'TEST-USR-STD-07', N'STD900307', N'student07@example.test', N'عبد الله', N'الهاشمي', N'عبد الله محمد الهاشمي', N'عُمان', 1),
    (N'TEST-USR-STD-08', N'STD900308', N'student08@example.test', N'نور الهدى', N'التميمي', N'نور الهدى صالح التميمي', N'الأردن', 1),
    (N'TEST-USR-STD-09', N'STD900309', N'student09@example.test', N'حسن', N'الشمري', N'حسن إبراهيم الشمري', N'الكويت', 1),
    (N'TEST-USR-STD-10', N'STD900310', N'student10@example.test', N'سارة', N'الغامدي', N'سارة عبد العزيز الغامدي', N'السعودية', 1),
    (N'TEST-USR-STD-11', N'STD900311', N'student11@example.test', N'بلال', N'الماجري', N'بلال كريم الماجري', N'تونس', 1),
    (N'TEST-USR-STD-12', N'STD900312', N'student12@example.test', N'هبة', N'الجزائري', N'هبة طارق الجزائري', N'الجزائر', 1),
    (N'TEST-USR-STD-13', N'STD900313', N'student13@example.test', N'راشد', N'الحارثي', N'راشد سعيد الحارثي', N'عُمان', 1),
    (N'TEST-USR-STD-14', N'STD900314', N'student14@example.test', N'زينب', N'البحراني', N'زينب علي البحراني', N'البحرين', 1),
    (N'TEST-USR-STD-15', N'STD900315', N'student15@example.test', N'سلطان', N'الهاجري', N'سلطان حمد الهاجري', N'قطر', 1),
    (N'TEST-USR-STD-16', N'STD900316', N'student16@example.test', N'دانة', N'الصباح', N'دانة مشعل الصباح', N'الكويت', 1),
    (N'TEST-USR-STD-17', N'STD900317', N'student17@example.test', N'أنس', N'المرابط', N'أنس ياسين المرابط', N'المغرب', 1),
    (N'TEST-USR-STD-18', N'STD900318', N'student18@example.test', N'خديجة', N'الفاسي', N'خديجة إدريس الفاسي', N'المغرب', 1),
    (N'TEST-USR-STD-19', N'STD900319', N'student19@example.test', N'ماجد', N'الدوسري', N'ماجد خالد الدوسري', N'السعودية', 1),
    (N'TEST-USR-STD-20', N'STD900320', N'student20@example.test', N'رزان', N'الكردي', N'رزان وليد الكردي', N'الأردن', 1),
    -- الطلاب 21 إلى 25: غير مسجلين في أي دورة لاختبار TEST-18
    (N'TEST-USR-STD-21', N'STD900321', N'student21@example.test', N'مهند', N'القحطاني', N'مهند يحيى القحطاني', N'السعودية', 1),
    (N'TEST-USR-STD-22', N'STD900322', N'student22@example.test', N'لمى', N'الرشيدي', N'لمى سعود الرشيدي', N'الكويت', 1),
    (N'TEST-USR-STD-23', N'STD900323', N'student23@example.test', N'حمزة', N'النمري', N'حمزة عثمان النمري', N'الأردن', 1),
    (N'TEST-USR-STD-24', N'STD900324', N'student24@example.test', N'أروى', N'الزهراني', N'أروى طلال الزهراني', N'السعودية', 1),
    (N'TEST-USR-STD-25', N'STD900325', N'student25@example.test', N'فيصل', N'السويدي', N'فيصل مبارك السويدي', N'الإمارات', 1),
    (N'TEST-USR-STD-26', N'STD900326', N'student26@example.test', N'ريان', N'النجار', N'ريان كمال النجار', N'مصر', 1),
    -- الطلاب 27 إلى 30: حسابات معطلة / غير نشطة (IsActive = 0)
    (N'TEST-USR-STD-27', N'STD900327', N'student27@example.test', N'ندى', N'الصاوي', N'ندى هشام الصاوي', N'مصر', 0),
    (N'TEST-USR-STD-28', N'STD900328', N'student28@example.test', N'تركي', N'الشريف', N'تركي عبد الإله الشريف', N'السعودية', 0),
    (N'TEST-USR-STD-29', N'STD900329', N'student29@example.test', N'ليان', N'الحلبي', N'ليان وسيم الحلبي', N'لبنان', 0),
    (N'TEST-USR-STD-30', N'STD900330', N'student30@example.test', N'قاسم', N'البلوشي', N'قاسم عيسى البلوشي', N'عُمان', 0);

    INSERT INTO dbo.AspNetUsers (
        Id, UserName, Email, EmailConfirmed, PasswordHash, SecurityStamp, PhoneNumber, PhoneNumberConfirmed,
        TwoFactorEnabled, LockoutEnabled, AccessFailedCount, FirstName, LastName, RealName, Password,
        role, userOrTetcherOrStudent, IsActive, Country, Location, Education, Experience, Studing
    )
    SELECT 
        s.Id, s.UserName, s.Email, 1, @StudentHash, CAST(NEWID() AS NVARCHAR(128)),
        N'+9665' + RIGHT(N'0000000' + CAST(s.Seq AS NVARCHAR(10)), 8), 1, 0, 0, 0,
        s.FirstName, s.LastName, s.RealName, N'Std@Tabseet2026!', N'Student', 2, s.IsActive,
        s.Country, s.Country + N' [TEST_TABSEET_2026]', N'طالب بأكاديمية تبسيط الدولية', N'تعلّم إلكتروني', N'أكاديمية تبسيط'
    FROM @StudentsData s;

    INSERT INTO dbo.AspNetUserRoles (RoleId, UserId)
    SELECT @StudentRoleId, s.Id FROM @StudentsData s;

    INSERT INTO dbo.__TestDataManifest (TableName, RecordId)
    SELECT N'AspNetUsers', s.Id FROM @StudentsData s;

    INSERT INTO dbo.__TestDataManifest (TableName, RecordId)
    SELECT N'AspNetUserRoles', @StudentRoleId + N'/' + s.Id FROM @StudentsData s;

    PRINT N'   ==> تم إدخال 37 حساباً بنجاح (1 مدير + 6 مدرسين + 30 طالباً).';


    PRINT N'[3/14] إدخال الأقسام الرئيسية (CategoryTypes)...';

    DECLARE @DeptIds TABLE (Seq INT IDENTITY(1,1), TypeID INT, NameAR NVARCHAR(100));

    INSERT INTO dbo.CategoryTypes (NameAR, NameEn, ParentType)
    OUTPUT inserted.ID, inserted.NameAR INTO @DeptIds (TypeID, NameAR)
    VALUES
        (N'الدراسات القرآنية والإسلامية', N'Quranic and Islamic Studies', NULL),
        (N'اللغات والترجمة', N'Languages and Translation', NULL),
        (N'العلوم والتكنولوجيا والرياضيات', N'Sciences, Technology and Mathematics', NULL),
        (N'المناهج والبحث العلمي', N'Methodology and Academic Research', NULL);

    INSERT INTO dbo.__TestDataManifest (TableName, RecordId)
    SELECT N'CategoryTypes', CAST(TypeID AS NVARCHAR(20)) FROM @DeptIds;

    DECLARE @DeptQuran INT = (SELECT TypeID FROM @DeptIds WHERE Seq = 1);
    DECLARE @DeptLang  INT = (SELECT TypeID FROM @DeptIds WHERE Seq = 2);
    DECLARE @DeptTech  INT = (SELECT TypeID FROM @DeptIds WHERE Seq = 3);
    DECLARE @DeptRes   INT = (SELECT TypeID FROM @DeptIds WHERE Seq = 4);


    PRINT N'[4/14] إدخال صفحات الواجهة (New_pages & NewGridPages) المرتبطة بالدورات...';

    DECLARE @CoursePages TABLE (Seq INT IDENTITY(1,1), PageID INT, CourseName NVARCHAR(200));

    INSERT INTO dbo.New_pages (name, Page_Type, IsUiUx, CategoryType_ID)
    OUTPUT inserted.ID, inserted.name INTO @CoursePages (PageID, CourseName)
    VALUES
        (N'تعليم القرآن الكريم والتجويد', 7, 0, @DeptQuran),
        (N'القاعدة النورانية للمبتدئين', 7, 0, @DeptQuran),
        (N'اللغة العربية', 7, 0, @DeptLang),
        (N'اللغة الإنجليزية للمبتدئين', 7, 0, @DeptLang),
        (N'أساسيات علوم الكمبيوتر', 7, 0, @DeptTech),
        (N'الرياضيات', 7, 0, @DeptTech),
        (N'مهارات البحث العلمي', 7, 0, @DeptRes),
        (N'اللغة الفرنسية', 7, 0, @DeptLang);

    INSERT INTO dbo.__TestDataManifest (TableName, RecordId)
    SELECT N'New_pages', CAST(PageID AS NVARCHAR(20)) FROM @CoursePages;

    DECLARE @GridPages TABLE (GridID INT);
    INSERT INTO dbo.NewGridPages (Lang_ID, Pages_ID, Title, Image_Cover)
    OUTPUT inserted.ID INTO @GridPages
    SELECT 1, cp.PageID, cp.CourseName, N'course_cover_' + CAST(cp.Seq AS NVARCHAR(10)) + N'.jpg'
    FROM @CoursePages cp;

    INSERT INTO dbo.__TestDataManifest (TableName, RecordId)
    SELECT N'NewGridPages', CAST(GridID AS NVARCHAR(20)) FROM @GridPages;


    PRINT N'[5/14] إدخال الدورات التعليمية الـ 8 (Categories)...';

    DECLARE @CoursesOut TABLE (
        Seq INT IDENTITY(1,1),
        CourseID INT,
        NameAr NVARCHAR(255),
        CategoryType_ID INT,
        IsPayed INT,
        PayedValue NVARCHAR(50)
    );

    DECLARE @P1 INT = (SELECT PageID FROM @CoursePages WHERE Seq = 1);
    DECLARE @P2 INT = (SELECT PageID FROM @CoursePages WHERE Seq = 2);
    DECLARE @P3 INT = (SELECT PageID FROM @CoursePages WHERE Seq = 3);
    DECLARE @P4 INT = (SELECT PageID FROM @CoursePages WHERE Seq = 4);
    DECLARE @P5 INT = (SELECT PageID FROM @CoursePages WHERE Seq = 5);
    DECLARE @P6 INT = (SELECT PageID FROM @CoursePages WHERE Seq = 6);
    DECLARE @P7 INT = (SELECT PageID FROM @CoursePages WHERE Seq = 7);
    DECLARE @P8 INT = (SELECT PageID FROM @CoursePages WHERE Seq = 8);

    -- الدورة 1: القرآن الكريم (مجانية، مفتوحة)
    -- الدورة 2: القاعدة النورانية (مدفوعة 150، مفتوحة)
    -- الدورة 3: اللغة العربية (مدفوعة 200، مفتوحة)
    -- الدورة 4: اللغة الإنجليزية (مدفوعة 250، مفتوحة)
    -- الدورة 5: علوم الكمبيوتر (مجانية، مفتوحة)
    -- الدورة 6: الرياضيات (مدفوعة 300، مفتوحة)
    -- الدورة 7: البحث العلمي (مجانية، مفتوحة، بدون طلاب مسجلين لاختبار TEST-19)
    -- الدورة 8: اللغة الفرنسية (مدفوعة 350، مغلقة ومنتهية الصلاحية CanRegister=0 لاختبار TEST-20)
    INSERT INTO dbo.Categories (
        NameAr, NameEn, CategoryType_ID, ParentCateogry_ID, IsPayed, PayedValue,
        Description, DescriptionEN, New_Page_ID, CertificationAR, CertificationEN,
        CanRegister, EndTime, Icon, DocumentBook
    )
    OUTPUT inserted.ID, inserted.NameAr, inserted.CategoryType_ID, inserted.IsPayed, inserted.PayedValue INTO @CoursesOut
    VALUES
    (N'تعليم القرآن الكريم والتجويد', N'Holy Quran Recitation and Tajweed', @DeptQuran, NULL, 0, N'0',
     N'دورة شاملة ومتقنة في أحكام التجويد ومخارج الحروف وصفاتها والتطبيق العملي مع نخبة من القراء المجازين. [TEST_TABSEET_2026]',
     N'Comprehensive course in Tajweed rules, Quranic articulation phonetics and applied recitation.',
     @P1, N'cert_quran_ar.png', N'cert_quran_en.png', 1, N'2027-12-31 23:59:59', N'icon_quran.png', N'book_quran_tajweed.pdf'),

    (N'القاعدة النورانية للمبتدئين', N'Nooraniyah Qaida for Beginners', @DeptQuran, NULL, 1, N'150',
     N'منهج تأسيسي متكامل لتعليم القراءة العربية وضبط مخارج الحروف وقراءة القرآن الكريم بالتهجي الصحيح. [TEST_TABSEET_2026]',
     N'Foundational method for Arabic reading, phonics and accurate Quranic spelling for all ages.',
     @P2, N'cert_nooraniyah_ar.png', N'cert_nooraniyah_en.png', 1, N'2027-12-31 23:59:59', N'icon_noor.png', N'book_nooraniyah.pdf'),

    (N'اللغة العربية', N'Arabic Language and Grammar', @DeptLang, NULL, 1, N'200',
     N'دورة تأسيسية ومتقدمة في النحو والصرف والبلاغة والإعراب التطبيقي للنصوص العربية الفصيحة. [TEST_TABSEET_2026]',
     N'Core and advanced principles of Arabic grammar, morphology, syntax and rhetoric.',
     @P3, N'cert_arabic_ar.png', N'cert_arabic_en.png', 1, N'2027-12-31 23:59:59', N'icon_arabic.png', N'book_arabic_grammar.pdf'),

    (N'اللغة الإنجليزية للمبتدئين', N'English for Beginners (A1-A2)', @DeptLang, NULL, 1, N'250',
     N'دورة مكثفة لتطوير مهارات المحادثة والاستماع والقراءة والكتابة وبناء المفردات الإنجليزية بثقة. [TEST_TABSEET_2026]',
     N'Intensive program developing speaking, listening, reading, writing and essential vocabulary.',
     @P4, N'cert_english_ar.png', N'cert_english_en.png', 1, N'2027-12-31 23:59:59', N'icon_english.png', N'book_english_starter.pdf'),

    (N'أساسيات علوم الكمبيوتر', N'Computer Science Fundamentals', @DeptTech, NULL, 0, N'0',
     N'مدخل علمي وعملي إلى مفاهيم الخوارزميات وهياكل البيانات وأساسيات البرمجة وتصميم النظم الرقمية. [TEST_TABSEET_2026]',
     N'Scientific foundation covering algorithms, data structures, software thinking and digital architectures.',
     @P5, N'cert_cs_ar.png', N'cert_cs_en.png', 1, N'2027-12-31 23:59:59', N'icon_cs.png', N'book_cs_basics.pdf'),

    (N'الرياضيات', N'Mathematics and Calculus', @DeptTech, NULL, 1, N'300',
     N'شرح منهجي لأسس الجبر وحساب التفاضل والتكامل والهندسة التحليلية والتفكير الرياضي المنطقي. [TEST_TABSEET_2026]',
     N'Systematic curriculum covering algebra, calculus, analytic geometry and mathematical problem-solving.',
     @P6, N'cert_math_ar.png', N'cert_math_en.png', 1, N'2027-12-31 23:59:59', N'icon_math.png', N'book_math_calculus.pdf'),

    (N'مهارات البحث العلمي', N'Scientific Research Skills', @DeptRes, NULL, 0, N'0',
     N'خطوات إعداد البحوث الأكاديمية وصياغة الإشكالية وفرضيات البحث والتوثيق العلمي الدقيق. (دورة بدون طلاب لاختبار TEST-19) [TEST_TABSEET_2026]',
     N'Methodologies of academic thesis formulation, literature review, data gathering and citation standards.',
     @P7, N'cert_research_ar.png', N'cert_research_en.png', 1, N'2027-12-31 23:59:59', N'icon_research.png', N'book_research_methods.pdf'),

    (N'اللغة الفرنسية', N'French Language (Debutant)', @DeptLang, NULL, 1, N'350',
     N'دورة لغة فرنسية تأسيسية. (دورة مغلقة ومكتملة الصلاحية لاختبار TEST-20) [TEST_TABSEET_2026]',
     N'Foundational French language course. (Archived/Closed course for testing TEST-20).',
     @P8, N'cert_french_ar.png', N'cert_french_en.png', 0, N'2025-01-01 00:00:00', N'icon_french.png', N'book_french_debutant.pdf');

    INSERT INTO dbo.__TestDataManifest (TableName, RecordId)
    SELECT N'Categories', CAST(CourseID AS NVARCHAR(20)) FROM @CoursesOut;

    DECLARE @C1 INT = (SELECT CourseID FROM @CoursesOut WHERE Seq = 1);
    DECLARE @C2 INT = (SELECT CourseID FROM @CoursesOut WHERE Seq = 2);
    DECLARE @C3 INT = (SELECT CourseID FROM @CoursesOut WHERE Seq = 3);
    DECLARE @C4 INT = (SELECT CourseID FROM @CoursesOut WHERE Seq = 4);
    DECLARE @C5 INT = (SELECT CourseID FROM @CoursesOut WHERE Seq = 5);
    DECLARE @C6 INT = (SELECT CourseID FROM @CoursesOut WHERE Seq = 6);
    DECLARE @C7 INT = (SELECT CourseID FROM @CoursesOut WHERE Seq = 7);
    DECLARE @C8 INT = (SELECT CourseID FROM @CoursesOut WHERE Seq = 8);


    PRINT N'[6/14] تحديث حقل Categories_ID للمدرسين بالصيغة الفعلية للكونترولر (ID/NameAr)...';

    -- Teacher 1 -> C1
    UPDATE dbo.AspNetUsers 
    SET Categories_ID = CAST(@C1 AS NVARCHAR(10)) + N'/تعليم القرآن الكريم والتجويد'
    WHERE Id = N'TEST-USR-TCH-01';

    -- Teacher 2 -> C2
    UPDATE dbo.AspNetUsers 
    SET Categories_ID = CAST(@C2 AS NVARCHAR(10)) + N'/القاعدة النورانية للمبتدئين'
    WHERE Id = N'TEST-USR-TCH-02';

    -- Teacher 3 -> C3
    UPDATE dbo.AspNetUsers 
    SET Categories_ID = CAST(@C3 AS NVARCHAR(10)) + N'/اللغة العربية'
    WHERE Id = N'TEST-USR-TCH-03';

    -- Teacher 4 -> C4
    UPDATE dbo.AspNetUsers 
    SET Categories_ID = CAST(@C4 AS NVARCHAR(10)) + N'/اللغة الإنجليزية للمبتدئين'
    WHERE Id = N'TEST-USR-TCH-04';

    -- Teacher 5 -> C5
    UPDATE dbo.AspNetUsers 
    SET Categories_ID = CAST(@C5 AS NVARCHAR(10)) + N'/أساسيات علوم الكمبيوتر'
    WHERE Id = N'TEST-USR-TCH-05';

    -- Teacher 6 -> C6, C7, C8 (تعدد الدورات بالفاصلة)
    UPDATE dbo.AspNetUsers 
    SET Categories_ID = CAST(@C6 AS NVARCHAR(10)) + N'/الرياضيات,' +
                        CAST(@C7 AS NVARCHAR(10)) + N'/مهارات البحث العلمي,' +
                        CAST(@C8 AS NVARCHAR(10)) + N'/اللغة الفرنسية'
    WHERE Id = N'TEST-USR-TCH-06';


    PRINT N'[7/14] إدخال المحاضرات (Lectures) - 4 محاضرات على الأقل لكل دورة...';

    DECLARE @LecOut TABLE (LecID INT, Category_ID INT, LecName NVARCHAR(255));

    -- تواريخ دقيقة وصالحة للتحليل DateTime.Parse: سابقة، اليوم، مستقبلية، ومسجلة (Offline)
    INSERT INTO dbo.Lectures (
        Lecture_Name, Category_ID, DayandTime, LectureTypeOnOff,
        LectureLink_Host, LectureLink_Client, DocumenFile, MediaLink, Others, LectureMinTime
    )
    OUTPUT inserted.ID, inserted.Category_ID, inserted.Lecture_Name INTO @LecOut
    VALUES
    -- الدورة 1: القرآن الكريم والتجويد
    (N'المحاضرة 1: مقدمة علم التجويد ومخارج الحروف', @C1, N'2026-10-10 10:00:00', N'1',
     N'https://meet.tabseet.test/host/quran-01', N'https://meet.tabseet.test/join/quran-01', N'slides_quran_01.pdf', N'https://video.tabseet.test/watch?v=quran01', N'محاضرة منتهية سابقة [TEST_TABSEET_2026]', N'0'),
    (N'المحاضرة 2: أحكام النون الساكنة والتنوين (الإظهار والإدغام)', @C1, N'2026-10-14 10:00:00', N'1',
     N'https://meet.tabseet.test/host/quran-02', N'https://meet.tabseet.test/join/quran-02', N'slides_quran_02.pdf', N'https://video.tabseet.test/watch?v=quran02', N'محاضرة مكتملة [TEST_TABSEET_2026]', N'0'),
    (N'المحاضرة 3: أحكام الميم الساكنة والمدود الطبيعية والفرعية', @C1, N'2026-10-18 10:00:00', N'1',
     N'https://meet.tabseet.test/host/quran-03', N'https://meet.tabseet.test/join/quran-03', N'slides_quran_03.pdf', N'https://video.tabseet.test/watch?v=quran03', N'محاضرة قادمة مجدولة [TEST_TABSEET_2026]', N'0'),
    (N'المحاضرة 4: تطبيق عملي على سورة الفاتحة وجزء عم (مسجلة)', @C1, N'', N'0',
     N'', N'', N'slides_quran_04.pdf', N'https://video.tabseet.test/watch?v=quran04-rec', N'محاضرة مسجلة للدراسة الذاتية [TEST_TABSEET_2026]', N'50'),

    -- الدورة 2: القاعدة النورانية
    (N'المحاضرة 1: الحروف الهجائية المفردة وطريقة النطق الصحيح', @C2, N'2026-10-11 16:00:00', N'1',
     N'https://meet.tabseet.test/host/noor-01', N'https://meet.tabseet.test/join/noor-01', N'slides_noor_01.pdf', N'https://video.tabseet.test/watch?v=noor01', N'محاضرة سابقة [TEST_TABSEET_2026]', N'0'),
    (N'المحاضرة 2: الحروف المركبة وحروف الهجاء المقطعة', @C2, N'2026-10-13 16:00:00', N'1',
     N'https://meet.tabseet.test/host/noor-02', N'https://meet.tabseet.test/join/noor-02', N'slides_noor_02.pdf', N'https://video.tabseet.test/watch?v=noor02', N'محاضرة مكتملة [TEST_TABSEET_2026]', N'0'),
    (N'المحاضرة 3: الحركات الثلاث: الفتحة والكسرة والضمة', @C2, N'2026-10-19 16:00:00', N'1',
     N'https://meet.tabseet.test/host/noor-03', N'https://meet.tabseet.test/join/noor-03', N'slides_noor_03.pdf', N'https://video.tabseet.test/watch?v=noor03', N'محاضرة قادمة [TEST_TABSEET_2026]', N'0'),
    (N'المحاضرة 4: التنوين وتطبيقات الحروف المنونة (مسجلة)', @C2, N'', N'0',
     N'', N'', N'slides_noor_04.pdf', N'https://video.tabseet.test/watch?v=noor04-rec', N'محاضرة مسجلة ذاتية [TEST_TABSEET_2026]', N'45'),

    -- الدورة 3: اللغة العربية
    (N'المحاضرة 1: أقسام الكلمة (اسم وفعل وحرف) وعلامات كل قسم', @C3, N'2026-10-09 18:00:00', N'1',
     N'https://meet.tabseet.test/host/arb-01', N'https://meet.tabseet.test/join/arb-01', N'slides_arb_01.pdf', N'https://video.tabseet.test/watch?v=arb01', N'محاضرة مكتملة [TEST_TABSEET_2026]', N'0'),
    (N'المحاضرة 2: الجملة الاسمية وركناها المبتدأ والخبر ونواسخها', @C3, N'2026-10-12 18:00:00', N'1',
     N'https://meet.tabseet.test/host/arb-02', N'https://meet.tabseet.test/join/arb-02', N'slides_arb_02.pdf', N'https://video.tabseet.test/watch?v=arb02', N'محاضرة مكتملة [TEST_TABSEET_2026]', N'0'),
    (N'المحاضرة 3: الجملة الفعلية: الفعل والفاعل والمفعول به', @C3, N'2026-10-20 18:00:00', N'1',
     N'https://meet.tabseet.test/host/arb-03', N'https://meet.tabseet.test/join/arb-03', N'slides_arb_03.pdf', N'https://video.tabseet.test/watch?v=arb03', N'محاضرة قادمة [TEST_TABSEET_2026]', N'0'),
    (N'المحاضرة 4: ورشة إعراب وتطبيقات النصوص الأدبية (مسجلة)', @C3, N'', N'0',
     N'', N'', N'slides_arb_04.pdf', N'https://video.tabseet.test/watch?v=arb04-rec', N'محاضرة مسجلة [TEST_TABSEET_2026]', N'60'),

    -- الدورة 4: اللغة الإنجليزية
    (N'Lecture 1: Greetings, Introductions & Present Simple', @C4, N'2026-10-08 20:00:00', N'1',
     N'https://meet.tabseet.test/host/eng-01', N'https://meet.tabseet.test/join/eng-01', N'slides_eng_01.pdf', N'https://video.tabseet.test/watch?v=eng01', N'Completed online [TEST_TABSEET_2026]', N'0'),
    (N'Lecture 2: Daily Routines, Prepositions of Time & Questions', @C4, N'2026-10-15 20:00:00', N'1',
     N'https://meet.tabseet.test/host/eng-02', N'https://meet.tabseet.test/join/eng-02', N'slides_eng_02.pdf', N'https://video.tabseet.test/watch?v=eng02', N'Completed online [TEST_TABSEET_2026]', N'0'),
    (N'Lecture 3: Speaking Workshop: In a Restaurant & Shopping', @C4, N'2026-10-21 20:00:00', N'1',
     N'https://meet.tabseet.test/host/eng-03', N'https://meet.tabseet.test/join/eng-03', N'slides_eng_03.pdf', N'https://video.tabseet.test/watch?v=eng03', N'Upcoming online [TEST_TABSEET_2026]', N'0'),
    (N'Lecture 4: Listening Comprehension & Short Stories (Recorded)', @C4, N'', N'0',
     N'', N'', N'slides_eng_04.pdf', N'https://video.tabseet.test/watch?v=eng04-rec', N'Recorded lecture [TEST_TABSEET_2026]', N'45'),

    -- الدورة 5: علوم الكمبيوتر
    (N'المحاضرة 1: مقدمة في علوم الحاسب وأنظمة العد الثنائي والست عشري', @C5, N'2026-10-07 14:00:00', N'1',
     N'https://meet.tabseet.test/host/cs-01', N'https://meet.tabseet.test/join/cs-01', N'slides_cs_01.pdf', N'https://video.tabseet.test/watch?v=cs01', N'محاضرة منتهية [TEST_TABSEET_2026]', N'0'),
    (N'المحاضرة 2: التفكير المنطقي وهياكل الخوارزميات (Flowcharts & Pseudocode)', @C5, N'2026-10-14 14:00:00', N'1',
     N'https://meet.tabseet.test/host/cs-02', N'https://meet.tabseet.test/join/cs-02', N'slides_cs_02.pdf', N'https://video.tabseet.test/watch?v=cs02', N'محاضرة منتهية [TEST_TABSEET_2026]', N'0'),
    (N'المحاضرة 3: هياكل البيانات الأساسية: المصفوفات والقوائم المترابطة', @C5, N'2026-10-22 14:00:00', N'1',
     N'https://meet.tabseet.test/host/cs-03', N'https://meet.tabseet.test/join/cs-03', N'slides_cs_03.pdf', N'https://video.tabseet.test/watch?v=cs03', N'محاضرة قادمة [TEST_TABSEET_2026]', N'0'),
    (N'المحاضرة 4: مبادئ البرمجة كائنية التوجه OOP (مسجلة)', @C5, N'', N'0',
     N'', N'', N'slides_cs_04.pdf', N'https://video.tabseet.test/watch?v=cs04-rec', N'محاضرة مسجلة [TEST_TABSEET_2026]', N'55'),

    -- الدورة 6: الرياضيات
    (N'المحاضرة 1: المعادلات الخطية والتربيعية ونظم المعادلات', @C6, N'2026-10-06 17:00:00', N'1',
     N'https://meet.tabseet.test/host/math-01', N'https://meet.tabseet.test/join/math-01', N'slides_math_01.pdf', N'https://video.tabseet.test/watch?v=math01', N'محاضرة سابقة [TEST_TABSEET_2026]', N'0'),
    (N'المحاضرة 2: الدوال الرياضية وخصائصها ومداها ومجالها', @C6, N'2026-10-13 17:00:00', N'1',
     N'https://meet.tabseet.test/host/math-02', N'https://meet.tabseet.test/join/math-02', N'slides_math_02.pdf', N'https://video.tabseet.test/watch?v=math02', N'محاضرة سابقة [TEST_TABSEET_2026]', N'0'),
    (N'المحاضرة 3: النهايات والاتصال ومقدمة في الاشتقاق', @C6, N'2026-10-23 17:00:00', N'1',
     N'https://meet.tabseet.test/host/math-03', N'https://meet.tabseet.test/join/math-03', N'slides_math_03.pdf', N'https://video.tabseet.test/watch?v=math03', N'محاضرة قادمة [TEST_TABSEET_2026]', N'0'),
    (N'المحاضرة 4: تطبيقات التفاضل في الهندسة والفيزياء (مسجلة)', @C6, N'', N'0',
     N'', N'', N'slides_math_04.pdf', N'https://video.tabseet.test/watch?v=math04-rec', N'محاضرة مسجلة [TEST_TABSEET_2026]', N'60'),

    -- الدورة 7: مهارات البحث العلمي (دورة بدون طلاب)
    (N'المحاضرة 1: أساسيات المنهج العلمي وخطوات بناء خطة البحث', @C7, N'2026-10-05 11:00:00', N'1',
     N'https://meet.tabseet.test/host/res-01', N'https://meet.tabseet.test/join/res-01', N'slides_res_01.pdf', N'https://video.tabseet.test/watch?v=res01', N'محاضرة بدون طلاب [TEST_TABSEET_2026]', N'0'),
    (N'المحاضرة 2: مراجعة الأدبيات السابقة وصياغة فرضيات البحث', @C7, N'2026-10-12 11:00:00', N'1',
     N'https://meet.tabseet.test/host/res-02', N'https://meet.tabseet.test/join/res-02', N'slides_res_02.pdf', N'https://video.tabseet.test/watch?v=res02', N'محاضرة بدون طلاب [TEST_TABSEET_2026]', N'0'),
    (N'المحاضرة 3: أدوات جمع البيانات: الاستبيان والمقابلة والملاحظة', @C7, N'2026-10-24 11:00:00', N'1',
     N'https://meet.tabseet.test/host/res-03', N'https://meet.tabseet.test/join/res-03', N'slides_res_03.pdf', N'https://video.tabseet.test/watch?v=res03', N'محاضرة بدون طلاب [TEST_TABSEET_2026]', N'0'),
    (N'المحاضرة 4: أساليب التوثيق المعتمدة APA ونزاهة البحث (مسجلة)', @C7, N'', N'0',
     N'', N'', N'slides_res_04.pdf', N'https://video.tabseet.test/watch?v=res04-rec', N'محاضرة مسجلة بدون طلاب [TEST_TABSEET_2026]', N'50'),

    -- الدورة 8: اللغة الفرنسية (دورة مغلقة)
    (N'Lecon 1: Les Salutations et l alphabet francais', @C8, N'2025-01-05 15:00:00', N'1',
     N'https://meet.tabseet.test/host/fr-01', N'https://meet.tabseet.test/join/fr-01', N'slides_fr_01.pdf', N'https://video.tabseet.test/watch?v=fr01', N'دورة مغلقة سابقة [TEST_TABSEET_2026]', N'0'),
    (N'Lecon 2: Se presenter et verbes etre et avoir', @C8, N'2025-01-12 15:00:00', N'1',
     N'https://meet.tabseet.test/host/fr-02', N'https://meet.tabseet.test/join/fr-02', N'slides_fr_02.pdf', N'https://video.tabseet.test/watch?v=fr02', N'دورة مغلقة سابقة [TEST_TABSEET_2026]', N'0'),
    (N'Lecon 3: Les articles et les noms au pluriel', @C8, N'2025-01-19 15:00:00', N'1',
     N'https://meet.tabseet.test/host/fr-03', N'https://meet.tabseet.test/join/fr-03', N'slides_fr_03.pdf', N'https://video.tabseet.test/watch?v=fr03', N'دورة مغلقة سابقة [TEST_TABSEET_2026]', N'0'),
    (N'Lecon 4: Expression orale et vocabulaire du quotidien (Enregistre)', @C8, N'', N'0',
     N'', N'', N'slides_fr_04.pdf', N'https://video.tabseet.test/watch?v=fr04-rec', N'دورة مغلقة مسجلة [TEST_TABSEET_2026]', N'45');

    INSERT INTO dbo.__TestDataManifest (TableName, RecordId)
    SELECT N'Lectures', CAST(LecID AS NVARCHAR(20)) FROM @LecOut;

    PRINT N'   ==> تم إدخال 32 محاضرة تغطي الفترات الماضية واليوم والمستقبلية والمسجلة.';


    PRINT N'[8/14] إدخال بنك الأسئلة التعليمية (Questions) بجميع أنواعها الأربعة...';
    -- Type 1: صح أو خطأ (Q_TrueOrFolse)
    -- Type 2: اختيار من متعدد (Q_MultiChoice)
    -- Type 3: مطابقة (Q_Matching)
    -- Type 4: ملء فراغ (Q_FillVoid)

    DECLARE @QOut TABLE (QID INT, QType INT, Category_ID INT, QName NVARCHAR(255));

    -- 1. أسئلة دورة القرآن الكريم
    INSERT INTO dbo.Questions (Type, Q_Name, Q_Text, Q_Description, Category_ID)
    OUTPUT inserted.ID, inserted.Type, inserted.Category_ID, inserted.Q_Name INTO @QOut
    VALUES
    (1, N'سؤال تجويد صح/خطأ', N'النون الساكنة عند حرف الراء حكمها الإدغام الكامل بغير غنة.', N'[TEST_TABSEET_2026]', @C1),
    (2, N'سؤال تجويد اختيار متعدد', N'حروف الإظهار الحلقي ستة أحرف مجموعة في أوائل كلمات:', N'[TEST_TABSEET_2026]', @C1),
    (3, N'سؤال تجويد مطابقة', N'طابق بين كل حكم تجويدي وتعريفه الاصطلاحي الدقيق:', N'[TEST_TABSEET_2026]', @C1),
    (4, N'سؤال تجويد ملء فراغ', N'أحكام النون الساكنة والتنوين أربعة هي: الإظهار، الإدغام، الإقلاب، و...', N'[TEST_TABSEET_2026]', @C1),

    -- 2. أسئلة دورة القاعدة النورانية
    (1, N'سؤال نورانية صح/خطأ', N'الحركات في اللغة العربية هي الفتحة والكسرة والضمة فقط.', N'[TEST_TABSEET_2026]', @C2),
    (2, N'سؤال نورانية اختيار متعدد', N'كم عدد حروف الهجاء المفردة المقررة في الدرس الأول من القاعدة النورانية؟', N'[TEST_TABSEET_2026]', @C2),
    (3, N'سؤال نورانية مطابقة', N'طابق بين الحرف ونوعه الصوتي عند النطق به:', N'[TEST_TABSEET_2026]', @C2),
    (4, N'سؤال نورانية ملء فراغ', N'التنوين هو نون ساكنة زائدة تلحق آخر الأسماء لفظاً لا...', N'[TEST_TABSEET_2026]', @C2),

    -- 3. أسئلة دورة اللغة العربية
    (1, N'سؤال نحو صح/خطأ', N'الفاعل في الجملة الفعلية يكون دائماً مجروراً بالكسرة.', N'[TEST_TABSEET_2026]', @C3),
    (2, N'سؤال نحو اختيار متعدد', N'ما إعراب كلمة "الطالبُ" في جملة: "حضر الطالبُ مبكراً"؟', N'[TEST_TABSEET_2026]', @C3),
    (3, N'سؤال نحو مطابقة', N'صل كل تركيب نحوي بحالته الإعرابية المقررة:', N'[TEST_TABSEET_2026]', @C3),
    (4, N'سؤال نحو ملء فراغ', N'الأفعال الخمسة تُرفع بثبوت النون وتُنصب وتُجزم بـ...', N'[TEST_TABSEET_2026]', @C3),

    -- 4. أسئلة دورة اللغة الإنجليزية
    (1, N'English Q1 T/F', N'The past tense of the verb "go" is "goed".', N'[TEST_TABSEET_2026]', @C4),
    (2, N'English Q2 MCQ', N'Choose the correct sentence in Present Simple tense:', N'[TEST_TABSEET_2026]', @C4),
    (3, N'English Q3 Match', N'Match each English word with its correct Arabic meaning:', N'[TEST_TABSEET_2026]', @C4),
    (4, N'English Q4 FillVoid', N'Fill in the blank: She _______ English fluently every day.', N'[TEST_TABSEET_2026]', @C4),

    -- 5. أسئلة علوم الكمبيوتر
    (1, N'سؤال حاسب صح/خطأ', N'الخوارزمية هي مجموعة من الخطوات المنطقية المرتبة لحل مشكلة محددة.', N'[TEST_TABSEET_2026]', @C5),
    (2, N'سؤال حاسب اختيار متعدد', N'أي من هياكل البيانات التالية يعمل بمبدأ الوارد أولاً يخرج أولاً (FIFO)؟', N'[TEST_TABSEET_2026]', @C5),
    (3, N'سؤال حاسب مطابقة', N'طابق المصطلح التقني بتعريفه الهندسي السليم:', N'[TEST_TABSEET_2026]', @C5),
    (4, N'سؤال حاسب ملء فراغ', N'أصغر وحدة بنائية لتخزين البيانات في الحاسوب هي الـ...', N'[TEST_TABSEET_2026]', @C5),

    -- 6. أسئلة الرياضيات
    (1, N'سؤال رياضيات صح/خطأ', N'مشتقة الدالة الثابتة f(x) = c تساوي دائماً صفراً.', N'[TEST_TABSEET_2026]', @C6),
    (2, N'سؤال رياضيات اختيار متعدد', N'ما حل المعادلة التربيعية: x² - 9 = 0 في مجموعة الأعداد الحقيقية؟', N'[TEST_TABSEET_2026]', @C6),
    (3, N'سؤال رياضيات مطابقة', N'طابق الدالة الرياضية بمشتقتها الأولى:', N'[TEST_TABSEET_2026]', @C6),
    (4, N'سؤال رياضيات ملء فراغ', N'المصفوفة التي عدد صفوفها يساوي عدد أعمدتها تسمى مصفوفة...', N'[TEST_TABSEET_2026]', @C6),

    -- 7. أسئلة البحث العلمي
    (1, N'سؤال بحث علمي صح/خطأ', N'الفرضية الصفرية تفترض وجود علاقة إيجابية قوية بين متغيرات الدراسة.', N'[TEST_TABSEET_2026]', @C7),
    (2, N'سؤال بحث علمي اختيار متعدد', N'الأسلوب الأكثر ملاءمة لقياس اتجاهات عينة كبيرة من المبحوثين هو:', N'[TEST_TABSEET_2026]', @C7),
    (3, N'سؤال بحث علمي مطابقة', N'طابق بين نوع المنهج العلمي وطبيعة استخدامه الأكاديمي:', N'[TEST_TABSEET_2026]', @C7),
    (4, N'سؤال بحث علمي ملء فراغ', N'نظام التوثيق الأكاديمي الصادر عن جمعية علم النفس الأمريكية يُعرف اختصاراً بـ...', N'[TEST_TABSEET_2026]', @C7),

    -- 8. أسئلة اللغة الفرنسية
    (1, N'French Q1 T/F', N'En francais, le verbe "etre" signifie "to have" en anglais.', N'[TEST_TABSEET_2026]', @C8),
    (2, N'French Q2 MCQ', N'Quelle est la traduction correcte de "Bonjour, comment allez-vous?" ?', N'[TEST_TABSEET_2026]', @C8),
    (3, N'French Q3 Match', N'Reliez chaque pronom personnel a sa forme appropriee du verbe avoir:', N'[TEST_TABSEET_2026]', @C8),
    (4, N'French Q4 FillVoid', N'Completez avec le verbe correct: Nous _______ des etudiants studieux.', N'[TEST_TABSEET_2026]', @C8);

    INSERT INTO dbo.__TestDataManifest (TableName, RecordId)
    SELECT N'Questions', CAST(QID AS NVARCHAR(20)) FROM @QOut;


    PRINT N'[9/14] إدخال تفاصيل الإجابات والدرجات في جداول الأسئلة الفرعية...';

    -- 1. جداول صح وخطأ (Q_TrueOrFolse) - Type = 1
    DECLARE @TF_Out TABLE (ID INT);
    INSERT INTO dbo.Q_TrueOrFolse (Question_ID, TrueValue, Degry)
    OUTPUT inserted.ID INTO @TF_Out
    SELECT q.QID, 
           CASE 
               WHEN q.QName LIKE N'%تجويد%' THEN 1
               WHEN q.QName LIKE N'%نورانية%' THEN 0
               WHEN q.QName LIKE N'%نحو%' THEN 0
               WHEN q.QName LIKE N'%English%' THEN 0
               WHEN q.QName LIKE N'%حاسب%' THEN 1
               WHEN q.QName LIKE N'%رياضيات%' THEN 1
               WHEN q.QName LIKE N'%بحث%' THEN 0
               WHEN q.QName LIKE N'%French%' THEN 0
               ELSE 1
           END, 
           5.0
    FROM @QOut q WHERE q.QType = 1;

    INSERT INTO dbo.__TestDataManifest (TableName, RecordId)
    SELECT N'Q_TrueOrFolse', CAST(ID AS NVARCHAR(20)) FROM @TF_Out;

    -- 2. جداول الاختيار من متعدد (Q_MultiChoice) - Type = 2
    DECLARE @MC_Out TABLE (ID INT);

    -- تجويد (QID تجويد اختيار متعدد)
    DECLARE @Q_MC_Quran INT = (SELECT QID FROM @QOut WHERE Category_ID = @C1 AND QType = 2);
    INSERT INTO dbo.Q_MultiChoice (Question_ID, Choose, Degry)
    OUTPUT inserted.ID INTO @MC_Out
    VALUES
        (@Q_MC_Quran, N'أخي هاك علماً حازه غير خاسر (همز، هاء، عين، حاء، غين، خاء)', 5.0),
        (@Q_MC_Quran, N'يرملون (ياء، راء، ميم، لام، واو، نون)', 0.0),
        (@Q_MC_Quran, N'قطب جد (قاف، طاء، باء، جيم، دال)', 0.0),
        (@Q_MC_Quran, N'فحثه شخص سكت', 0.0);

    -- نورانية
    DECLARE @Q_MC_Noor INT = (SELECT QID FROM @QOut WHERE Category_ID = @C2 AND QType = 2);
    INSERT INTO dbo.Q_MultiChoice (Question_ID, Choose, Degry)
    OUTPUT inserted.ID INTO @MC_Out
    VALUES
        (@Q_MC_Noor, N'29 حرفاً (باعتبار الهمزة والألف)', 5.0),
        (@Q_MC_Noor, N'20 حرفاً فقط', 0.0),
        (@Q_MC_Noor, N'35 حرفاً', 0.0),
        (@Q_MC_Noor, N'15 حرفاً', 0.0);

    -- نحو
    DECLARE @Q_MC_Arb INT = (SELECT QID FROM @QOut WHERE Category_ID = @C3 AND QType = 2);
    INSERT INTO dbo.Q_MultiChoice (Question_ID, Choose, Degry)
    OUTPUT inserted.ID INTO @MC_Out
    VALUES
        (@Q_MC_Arb, N'فاعل مرفوع وعلامة رفعه الضمة الظاهرة على آخره', 5.0),
        (@Q_MC_Arb, N'مفعول به منصوب وعلامة نصبه الفتحة', 0.0),
        (@Q_MC_Arb, N'مبتدأ مؤخر مرفوع', 0.0),
        (@Q_MC_Arb, N'اسم مجرور بالكسرة', 0.0);

    -- English
    DECLARE @Q_MC_Eng INT = (SELECT QID FROM @QOut WHERE Category_ID = @C4 AND QType = 2);
    INSERT INTO dbo.Q_MultiChoice (Question_ID, Choose, Degry)
    OUTPUT inserted.ID INTO @MC_Out
    VALUES
        (@Q_MC_Eng, N'He works at an international academy.', 5.0),
        (@Q_MC_Eng, N'He work at an international academy.', 0.0),
        (@Q_MC_Eng, N'He is work at an international academy.', 0.0),
        (@Q_MC_Eng, N'He working at an international academy.', 0.0);

    -- CS
    DECLARE @Q_MC_CS INT = (SELECT QID FROM @QOut WHERE Category_ID = @C5 AND QType = 2);
    INSERT INTO dbo.Q_MultiChoice (Question_ID, Choose, Degry)
    OUTPUT inserted.ID INTO @MC_Out
    VALUES
        (@Q_MC_CS, N'الطابور (Queue)', 5.0),
        (@Q_MC_CS, N'المكدس (Stack)', 0.0),
        (@Q_MC_CS, N'شجرة البحث الثنائية (Binary Search Tree)', 0.0),
        (@Q_MC_CS, N'الرسم البياني (Graph)', 0.0);

    -- Math
    DECLARE @Q_MC_Math INT = (SELECT QID FROM @QOut WHERE Category_ID = @C6 AND QType = 2);
    INSERT INTO dbo.Q_MultiChoice (Question_ID, Choose, Degry)
    OUTPUT inserted.ID INTO @MC_Out
    VALUES
        (@Q_MC_Math, N'x = 3 أو x = -3', 5.0),
        (@Q_MC_Math, N'x = 9 فقط', 0.0),
        (@Q_MC_Math, N'x = 0', 0.0),
        (@Q_MC_Math, N'لا يوجد حل حقيقي', 0.0);

    -- Research
    DECLARE @Q_MC_Res INT = (SELECT QID FROM @QOut WHERE Category_ID = @C7 AND QType = 2);
    INSERT INTO dbo.Q_MultiChoice (Question_ID, Choose, Degry)
    OUTPUT inserted.ID INTO @MC_Out
    VALUES
        (@Q_MC_Res, N'الاستبانة المقننة (Questionnaire)', 5.0),
        (@Q_MC_Res, N'التجربة المعملية الفردية', 0.0),
        (@Q_MC_Res, N'دراسة الحالة النادرة', 0.0),
        (@Q_MC_Res, N'الملاحظة العابرة غير المنظمة', 0.0);

    -- French
    DECLARE @Q_MC_Fr INT = (SELECT QID FROM @QOut WHERE Category_ID = @C8 AND QType = 2);
    INSERT INTO dbo.Q_MultiChoice (Question_ID, Choose, Degry)
    OUTPUT inserted.ID INTO @MC_Out
    VALUES
        (@Q_MC_Fr, N'مرحباً، كيف حالكم؟', 5.0),
        (@Q_MC_Fr, N'مع السلامة إلى الغد', 0.0),
        (@Q_MC_Fr, N'تصبح على خير', 0.0),
        (@Q_MC_Fr, N'أهلاً بك في منزلي', 0.0);

    INSERT INTO dbo.__TestDataManifest (TableName, RecordId)
    SELECT N'Q_MultiChoice', CAST(ID AS NVARCHAR(20)) FROM @MC_Out;

    -- 3. جداول المطابقة (Q_Matching) - Type = 3
    DECLARE @Mat_Out TABLE (ID INT);

    DECLARE @Q_Mat_Quran INT = (SELECT QID FROM @QOut WHERE Category_ID = @C1 AND QType = 3);
    INSERT INTO dbo.Q_Matching (Question_ID, ChoiceText, ChoiseTrueAnswer, Degry)
    OUTPUT inserted.ID INTO @Mat_Out
    VALUES
        (@Q_Mat_Quran, N'الإظهار', N'إخراج الحرف من مخرجه بغير غنة', 2.5),
        (@Q_Mat_Quran, N'الإدغام', N'إدخال حرف ساكن في متحرك بحيث يصيران حرفاً مشدداً', 2.5);

    DECLARE @Q_Mat_CS INT = (SELECT QID FROM @QOut WHERE Category_ID = @C5 AND QType = 3);
    INSERT INTO dbo.Q_Matching (Question_ID, ChoiceText, ChoiseTrueAnswer, Degry)
    OUTPUT inserted.ID INTO @Mat_Out
    VALUES
        (@Q_Mat_CS, N'Compiler', N'مترجم يقوم بتحويل الشيفرة بالكامل إلى لغة الآلة', 2.5),
        (@Q_Mat_CS, N'Interpreter', N'مفسر يقوم بتنفيذ الشيفرة البرمجية سطراً بسطر', 2.5);

    DECLARE @Q_Mat_Eng INT = (SELECT QID FROM @QOut WHERE Category_ID = @C4 AND QType = 3);
    INSERT INTO dbo.Q_Matching (Question_ID, ChoiceText, ChoiseTrueAnswer, Degry)
    OUTPUT inserted.ID INTO @Mat_Out
    VALUES
        (@Q_Mat_Eng, N'Knowledge', N'المعرفة', 2.5),
        (@Q_Mat_Eng, N'Academy', N'الأكاديمية', 2.5);

    DECLARE @Q_Mat_Math INT = (SELECT QID FROM @QOut WHERE Category_ID = @C6 AND QType = 3);
    INSERT INTO dbo.Q_Matching (Question_ID, ChoiceText, ChoiseTrueAnswer, Degry)
    OUTPUT inserted.ID INTO @Mat_Out
    VALUES
        (@Q_Mat_Math, N'مشتقة x²', N'2x', 2.5),
        (@Q_Mat_Math, N'مشتقة sin(x)', N'cos(x)', 2.5);

    INSERT INTO dbo.__TestDataManifest (TableName, RecordId)
    SELECT N'Q_Matching', CAST(ID AS NVARCHAR(20)) FROM @Mat_Out;

    -- 4. جداول ملء الفراغ (Q_FillVoid) - Type = 4
    DECLARE @FV_Out TABLE (ID INT);

    DECLARE @Q_FV_Quran INT = (SELECT QID FROM @QOut WHERE Category_ID = @C1 AND QType = 4);
    INSERT INTO dbo.Q_FillVoid (Question_ID, TrueValue, Degry)
    OUTPUT inserted.ID INTO @FV_Out
    VALUES (@Q_FV_Quran, N'الإخفاء', 5.0);

    DECLARE @Q_FV_CS INT = (SELECT QID FROM @QOut WHERE Category_ID = @C5 AND QType = 4);
    INSERT INTO dbo.Q_FillVoid (Question_ID, TrueValue, Degry)
    OUTPUT inserted.ID INTO @FV_Out
    VALUES (@Q_FV_CS, N'بت', 5.0);

    DECLARE @Q_FV_Arb INT = (SELECT QID FROM @QOut WHERE Category_ID = @C3 AND QType = 4);
    INSERT INTO dbo.Q_FillVoid (Question_ID, TrueValue, Degry)
    OUTPUT inserted.ID INTO @FV_Out
    VALUES (@Q_FV_Arb, N'حذف النون', 5.0);

    DECLARE @Q_FV_Eng INT = (SELECT QID FROM @QOut WHERE Category_ID = @C4 AND QType = 4);
    INSERT INTO dbo.Q_FillVoid (Question_ID, TrueValue, Degry)
    OUTPUT inserted.ID INTO @FV_Out
    VALUES (@Q_FV_Eng, N'speaks', 5.0);

    INSERT INTO dbo.__TestDataManifest (TableName, RecordId)
    SELECT N'Q_FillVoid', CAST(ID AS NVARCHAR(20)) FROM @FV_Out;


    PRINT N'[10/14] إدخال الاختبارات (Exams) وربط الأسئلة بالاختبارات (Exam_Q)...';

    DECLARE @ExamsOut TABLE (Seq INT IDENTITY(1,1), ExamID INT, Category_ID INT, ExamName NVARCHAR(255));

    -- اختباران لكل دورة (اختبار دوري نصفي + اختبار نهائي) = 16 اختباراً
    INSERT INTO dbo.Exams (Name, Description, Day_Time, Start_Time, Duration, Category_ID, Exam_Degry, Exam_win_Degry)
    OUTPUT inserted.ID, inserted.Category_ID, inserted.Name INTO @ExamsOut
    VALUES
    (N'الاختبار النصفي - تجويد النون الساكنة والتنوين', N'اختبار تقييمي نصفي لقياس استيعاب أحكام التلاوة. [TEST_TABSEET_2026]', N'2026-10-15', N'10:00:00', 45, @C1, 20, 12),
    (N'الامتحان النهائي المعتمد - تجويد القرآن الكريم', N'الامتحان النهائي الشامل لمنح الشهادة الأكاديمية المعتمدة. [TEST_TABSEET_2026]', N'2026-10-25', N'10:00:00', 60, @C1, 50, 30),

    (N'الاختبار الدوري - الحركات والتنوين النوراني', N'اختبار قياس تهجي الحروف المنونة والحركات. [TEST_TABSEET_2026]', N'2026-10-16', N'16:00:00', 45, @C2, 20, 12),
    (N'الامتحان النهائي المعتمد - القاعدة النورانية', N'الامتحان النهائي المؤهل لشهادة إتمام التأسيس النوراني. [TEST_TABSEET_2026]', N'2026-10-26', N'16:00:00', 60, @C2, 50, 30),

    (N'الاختبار النصفي - الجملة الاسمية والناسخة', N'اختبار تقويم نصفي في مبادئ النحو العربي. [TEST_TABSEET_2026]', N'2026-10-17', N'18:00:00', 45, @C3, 20, 12),
    (N'الامتحان النهائي المعتمد - اللغة العربية والنحو', N'الامتحان النهائي لنيل شهادة الكفاءة النحوية. [TEST_TABSEET_2026]', N'2026-10-27', N'18:00:00', 60, @C3, 50, 30),

    (N'Midterm Exam - English Grammar & Daily Routine', N'Midterm assessment for English basics. [TEST_TABSEET_2026]', N'2026-10-18', N'20:00:00', 45, @C4, 20, 12),
    (N'Final Examination - English A1 Comprehensive', N'Final qualification exam for Certificate. [TEST_TABSEET_2026]', N'2026-10-28', N'20:00:00', 60, @C4, 50, 30),

    (N'الاختبار النصفي - الخوارزميات وهياكل البيانات', N'اختبار عملي في التفكير المنطقي الخوارزمي. [TEST_TABSEET_2026]', N'2026-10-19', N'14:00:00', 45, @C5, 20, 12),
    (N'الامتحان النهائي المعتمد - علوم الحاسوب الأساسية', N'الامتحان النهائي لمبادئ علوم الحاسب. [TEST_TABSEET_2026]', N'2026-10-29', N'14:00:00', 60, @C5, 50, 30),

    (N'الاختبار النصفي - الجبر وحل المعادلات', N'اختبار نصفي في الجبر الرياضي. [TEST_TABSEET_2026]', N'2026-10-20', N'17:00:00', 45, @C6, 20, 12),
    (N'الامتحان النهائي المعتمد - الرياضيات والتفاضل', N'الامتحان النهائي الشامل لمقرر الرياضيات. [TEST_TABSEET_2026]', N'2026-10-30', N'17:00:00', 60, @C6, 50, 30),

    (N'الاختبار النصفي - مناهج وأدوات البحث العلمي', N'اختبار مناهج البحث (دورة بدون طلاب). [TEST_TABSEET_2026]', N'2026-10-21', N'11:00:00', 45, @C7, 20, 12),
    (N'الامتحان النهائي المعتمد - مهارات البحث الأكاديمي', N'الامتحان النهائي لمهارات البحث العلمي. [TEST_TABSEET_2026]', N'2026-10-31', N'11:00:00', 60, @C7, 50, 30),

    (N'Examen Partiel - Grammaire Francaise', N'Examen partiel (Cours cloture). [TEST_TABSEET_2026]', N'2025-01-20', N'15:00:00', 45, @C8, 20, 12),
    (N'Examen Final - Francais Debutant', N'Examen final certificat francais. [TEST_TABSEET_2026]', N'2025-01-30', N'15:00:00', 60, @C8, 50, 30);

    INSERT INTO dbo.__TestDataManifest (TableName, RecordId)
    SELECT N'Exams', CAST(ExamID AS NVARCHAR(20)) FROM @ExamsOut;

    -- ربط الأسئلة بالاختبارات المقابلة (Exam_Q)
    DECLARE @ExamQ_Out TABLE (ID INT);
    INSERT INTO dbo.Exam_Q (Exam_ID, Q_ID)
    OUTPUT inserted.ID INTO @ExamQ_Out
    SELECT e.ExamID, q.QID
    FROM @ExamsOut e
    INNER JOIN @QOut q ON e.Category_ID = q.Category_ID;

    INSERT INTO dbo.__TestDataManifest (TableName, RecordId)
    SELECT N'Exam_Q', CAST(ID AS NVARCHAR(20)) FROM @ExamQ_Out;


    PRINT N'[11/14] إدخال الواجبات (Assignments) وربطها بالمحاضرات والأسئلة...';

    DECLARE @AssOut TABLE (AssID INT, Lecture_ID INT, AssName NVARCHAR(255));

    -- واجبان لكل دورة (مرتبطان بالمحاضرة 1 والمحاضرة 2 من كل دورة)
    INSERT INTO dbo.Assignments (Name, Description, Lecture_ID, Assignmet_Degry, Assignmet_degry_Win)
    OUTPUT inserted.ID, inserted.Lecture_ID, inserted.Name INTO @AssOut
    SELECT 
        N'الوايج التطبيقي: ' + l.LecName,
        N'حل أسئلة وتطبيقات المحاضرة المقررة لتقييم الاستيعاب. [TEST_TABSEET_2026]',
        l.LecID,
        10.0,
        6
    FROM @LecOut l
    WHERE l.LecName LIKE N'%المحاضرة 1%' OR l.LecName LIKE N'%المحاضرة 2%' OR l.LecName LIKE N'%Lecture 1%' OR l.LecName LIKE N'%Lecture 2%' OR l.LecName LIKE N'%Lecon 1%' OR l.LecName LIKE N'%Lecon 2%';

    INSERT INTO dbo.__TestDataManifest (TableName, RecordId)
    SELECT N'Assignments', CAST(AssID AS NVARCHAR(20)) FROM @AssOut;

    -- ربط الأسئلة بالواجبات (Assignment_Q)
    DECLARE @AssQ_Out TABLE (ID INT);
    INSERT INTO dbo.Assignment_Q (Assignment_ID, Q_ID)
    OUTPUT inserted.ID INTO @AssQ_Out
    SELECT a.AssID, q.QID
    FROM @AssOut a
    INNER JOIN dbo.Lectures l ON a.Lecture_ID = l.ID
    INNER JOIN @QOut q ON l.Category_ID = q.Category_ID AND q.QType IN (1, 2);

    INSERT INTO dbo.__TestDataManifest (TableName, RecordId)
    SELECT N'Assignment_Q', CAST(ID AS NVARCHAR(20)) FROM @AssQ_Out;


    PRINT N'[12/14] إدخال تسجيلات الطلاب (Std_Reg_Category) ومصفوفة الحالات التعليمية والمالية...';

    DECLARE @RegOut TABLE (RegID INT, Category_ID INT, Std_ID NVARCHAR(128));

    -- سيناريوهات التسجيل:
    -- 1. دورات مجانية: طالب 1 وطالب 2 في C1 (قرآن) و C5 (كمبيوتر)
    -- 2. دورات مدفوعة تم الدفع:
    --    - بطاقة ائتمان (PayMent_Method = 1): طالب 1 في C2 (نورانية)، طالب 3 في C3 (عربي)
    --    - تحويل بنكي (PayMent_Method = 2 مع TransactionFile): طالب 2 في C4 (إنجليزي)، طالب 4 في C6 (رياضيات)
    -- 3. دفع معلق / لاحق (PayMent_Method = 3, std_category_Ispayed = 0):
    --    - طالب 5 في C2، طالب 6 في C3
    -- 4. كود خصم / قسيمة (PayMent_Method = 4, Code = 'TABSEET2026'):
    --    - طالب 7 في C4
    -- 5. طلاب استوفوا شروط الشهادة وأنهوا الاختبار بدرجات:
    --    - طالب 1 في C1: شهادة بتقدير ممتاز ورقم تسلسلي 30001
    --    - طالب 2 in C2: شهادة بتقدير جيد جدا ورقم تسلسلي 30002
    --    - طالب 3 in C3: شهادة بتقدير جيد ورقم تسلسلي 30003
    --    - طالب 4 in C4: شهادة بتقدير مقبول ورقم تسلسلي 30004
    -- 6. طالب رسب في الاختبار:
    --    - طالب 8 في C2: std_exam_isFinish = 0
    -- 7. دورة بدون طلاب: C7 (مهارات البحث العلمي) لا يسجل بها أي طالب لاختبار TEST-19!
    -- 8. طلاب ليس لديهم أي تسجيل: طالب 21 إلى طالب 25 لا يتم تسجيلهم بأي دورة لاختبار TEST-18!

    INSERT INTO dbo.Std_Reg_Category (
        Category_ID, Std_ID, category_Ispayed, std_category_Ispayed, std_exam_isFinish,
        PayMent_Method, TransactionFile, Code, PayMent_Value, std_exam_Finish_Time,
        Certificate_appreciation, Serial_Number
    )
    OUTPUT inserted.ID, inserted.Category_ID, inserted.Std_ID INTO @RegOut
    VALUES
    -- طالب 1: متفوق في القرآن (مجاني، أكمل وحصل على شهادة ممتاز)
    (@C1, N'TEST-USR-STD-01', 0, 1, 1, 0, N'', N'', N'0', N'1448/04/10:2026-10-17', N'ممتاز', N'30001'),
    -- طالب 1: مسجل في النورانية (مدفوع 150 بالبطاقة، مسدد ومكتمل)
    (@C2, N'TEST-USR-STD-01', 1, 1, 1, 1, N'', N'', N'150', N'1448/04/10:2026-10-17', N'ممتاز', N'30005'),

    -- طالب 2: متفوق في النورانية (مدفوع 150، أكمل وحصل على شهادة جيد جدا)
    (@C2, N'TEST-USR-STD-02', 1, 1, 1, 1, N'', N'', N'150', N'1448/04/10:2026-10-17', N'جيد جدا', N'30002'),
    -- طالب 2: مسجل في الإنجليزي بالتحويل البنكي ومكتمل الدفع
    (@C4, N'TEST-USR-STD-02', 1, 1, 0, 2, N'transfer_receipt_std02.png', N'', N'250', N'', NULL, NULL),

    -- طالب 3: في اللغة العربية (مدفوع 200 بالبطاقة، أكمل وحصل على شهادة جيد)
    (@C3, N'TEST-USR-STD-03', 1, 1, 1, 1, N'', N'', N'200', N'1448/04/10:2026-10-17', N'جيد', N'30003'),
    -- طالب 3: مسجل في علوم الكمبيوتر (مجاني)
    (@C5, N'TEST-USR-STD-03', 0, 1, 0, 0, N'', N'', N'0', N'', NULL, NULL),

    -- طالب 4: في اللغة الإنجليزية (مدفوع 250، أكمل وحصل على شهادة مقبول)
    (@C4, N'TEST-USR-STD-04', 1, 1, 1, 2, N'bank_wire_std04.jpg', N'', N'250', N'1448/04/10:2026-10-17', N'مقبول', N'30004'),
    -- طالب 4: في الرياضيات بالتحويل البنكي
    (@C6, N'TEST-USR-STD-04', 1, 1, 0, 2, N'bank_wire_math_std04.jpg', N'', N'300', N'', NULL, NULL),

    -- طالب 5: دفع معلق / لاحق في النورانية (std_category_Ispayed = 0)
    (@C2, N'TEST-USR-STD-05', 1, 0, 0, 3, N'', N'', N'150', N'', NULL, NULL),

    -- طالب 6: دفع معلق / لاحق في اللغة العربية (std_category_Ispayed = 0)
    (@C3, N'TEST-USR-STD-06', 1, 0, 0, 3, N'', N'', N'200', N'', NULL, NULL),

    -- طالب 7: دفع بقسيمة كود خصم في اللغة الإنجليزية (Method = 4)
    (@C4, N'TEST-USR-STD-07', 1, 1, 0, 4, N'', N'TABSEET2026', N'250', N'', NULL, NULL),

    -- طالب 8: رسب في اختبار النورانية ولم يستحق الشهادة (std_exam_isFinish = 0)
    (@C2, N'TEST-USR-STD-08', 1, 1, 0, 1, N'', N'', N'150', N'', NULL, NULL),

    -- تسجيلات متنوعة للطلاب 9 إلى 20 في الدورات المختلفة
    (@C1, N'TEST-USR-STD-09', 0, 1, 0, 0, N'', N'', N'0', N'', NULL, NULL),
    (@C1, N'TEST-USR-STD-10', 0, 1, 0, 0, N'', N'', N'0', N'', NULL, NULL),
    (@C2, N'TEST-USR-STD-11', 1, 1, 0, 1, N'', N'', N'150', N'', NULL, NULL),
    (@C3, N'TEST-USR-STD-12', 1, 1, 0, 2, N'receipt_std12.png', N'', N'200', N'', NULL, NULL),
    (@C4, N'TEST-USR-STD-13', 1, 0, 0, 3, N'', N'', N'250', N'', NULL, NULL),
    (@C5, N'TEST-USR-STD-14', 0, 1, 0, 0, N'', N'', N'0', N'', NULL, NULL),
    (@C6, N'TEST-USR-STD-15', 1, 1, 0, 1, N'', N'', N'300', N'', NULL, NULL),
    (@C1, N'TEST-USR-STD-16', 0, 1, 0, 0, N'', N'', N'0', N'', NULL, NULL),
    (@C2, N'TEST-USR-STD-17', 1, 1, 0, 4, N'', N'TABSEET2026', N'150', N'', NULL, NULL),
    (@C3, N'TEST-USR-STD-18', 1, 0, 0, 3, N'', N'', N'200', N'', NULL, NULL),
    (@C4, N'TEST-USR-STD-19', 1, 1, 0, 1, N'', N'', N'250', N'', NULL, NULL),
    (@C5, N'TEST-USR-STD-20', 0, 1, 0, 0, N'', N'', N'0', N'', NULL, NULL);

    INSERT INTO dbo.__TestDataManifest (TableName, RecordId)
    SELECT N'Std_Reg_Category', CAST(RegID AS NVARCHAR(20)) FROM @RegOut;


    PRINT N'[13/14] إدخال سجلات حضور وإكمال المحاضرات (std_lecture) والاختبارات (Std_ExamFinish)...';

    DECLARE @LecProgress TABLE (ID INT);

    -- طلاب أتموا جميع محاضرات دوراتهم (الطلاب 1، 2، 3، 4)
    INSERT INTO dbo.std_lecture (Lecture_ID, Std_ID, IsFinish, IsTimeEnd, IsAssingmetFinish)
    OUTPUT inserted.ID INTO @LecProgress
    SELECT l.ID, N'TEST-USR-STD-01', 1, 1, 1 FROM dbo.Lectures l WHERE l.Category_ID IN (@C1, @C2)
    UNION ALL
    SELECT l.ID, N'TEST-USR-STD-02', 1, 1, 1 FROM dbo.Lectures l WHERE l.Category_ID IN (@C2, @C4)
    UNION ALL
    SELECT l.ID, N'TEST-USR-STD-03', 1, 1, 1 FROM dbo.Lectures l WHERE l.Category_ID IN (@C3, @C5)
    UNION ALL
    SELECT l.ID, N'TEST-USR-STD-04', 1, 1, 1 FROM dbo.Lectures l WHERE l.Category_ID IN (@C4, @C6);

    -- طلاب قطعوا جزءاً من المحاضرات (أنهوا المحاضرتين 1 و 2، والمحاضرة 3 و 4 قيد التقدم)
    INSERT INTO dbo.std_lecture (Lecture_ID, Std_ID, IsFinish, IsTimeEnd, IsAssingmetFinish)
    OUTPUT inserted.ID INTO @LecProgress
    SELECT l.ID, N'TEST-USR-STD-05', 1, 1, 1 FROM dbo.Lectures l WHERE l.Category_ID = @C2 AND l.Lecture_Name LIKE N'%المحاضرة 1%'
    UNION ALL
    SELECT l.ID, N'TEST-USR-STD-05', 1, 1, 1 FROM dbo.Lectures l WHERE l.Category_ID = @C2 AND l.Lecture_Name LIKE N'%المحاضرة 2%'
    UNION ALL
    SELECT l.ID, N'TEST-USR-STD-05', 0, 0, 0 FROM dbo.Lectures l WHERE l.Category_ID = @C2 AND l.Lecture_Name LIKE N'%المحاضرة 3%'
    UNION ALL
    SELECT l.ID, N'TEST-USR-STD-06', 1, 1, 1 FROM dbo.Lectures l WHERE l.Category_ID = @C3 AND l.Lecture_Name LIKE N'%المحاضرة 1%'
    UNION ALL
    SELECT l.ID, N'TEST-USR-STD-06', 0, 0, 0 FROM dbo.Lectures l WHERE l.Category_ID = @C3 AND l.Lecture_Name LIKE N'%المحاضرة 2%';

    INSERT INTO dbo.__TestDataManifest (TableName, RecordId)
    SELECT N'std_lecture', CAST(ID AS NVARCHAR(20)) FROM @LecProgress;

    -- إدخال جدول Std_ExamFinish (ملاحظة: عمود [int] هو المفتاح الأساسي وليس Identity)
    DECLARE @StartFinishInt INT = ISNULL((SELECT MAX([int]) FROM dbo.Std_ExamFinish), 0) + 1;

    DECLARE @ExamC1_Final INT = (SELECT TOP 1 ExamID FROM @ExamsOut WHERE Category_ID = @C1 AND ExamName LIKE N'%النهائي%');
    DECLARE @ExamC2_Final INT = (SELECT TOP 1 ExamID FROM @ExamsOut WHERE Category_ID = @C2 AND ExamName LIKE N'%النهائي%');
    DECLARE @ExamC3_Final INT = (SELECT TOP 1 ExamID FROM @ExamsOut WHERE Category_ID = @C3 AND ExamName LIKE N'%النهائي%');
    DECLARE @ExamC4_Final INT = (SELECT TOP 1 ExamID FROM @ExamsOut WHERE Category_ID = @C4 AND ExamName LIKE N'%Final%');

    INSERT INTO dbo.Std_ExamFinish ([int], Std_ID, Exam_ID, std_Req_category_ID, isFinishExam)
    VALUES
        (@StartFinishInt + 0, 1, @ExamC1_Final, @C1, 1),
        (@StartFinishInt + 1, 2, @ExamC2_Final, @C2, 1),
        (@StartFinishInt + 2, 3, @ExamC3_Final, @C3, 1),
        (@StartFinishInt + 3, 4, @ExamC4_Final, @C4, 1),
        (@StartFinishInt + 4, 8, @ExamC2_Final, @C2, 0);

    INSERT INTO dbo.__TestDataManifest (TableName, RecordId)
    VALUES 
        (N'Std_ExamFinish', CAST(@StartFinishInt + 0 AS NVARCHAR(20))),
        (N'Std_ExamFinish', CAST(@StartFinishInt + 1 AS NVARCHAR(20))),
        (N'Std_ExamFinish', CAST(@StartFinishInt + 2 AS NVARCHAR(20))),
        (N'Std_ExamFinish', CAST(@StartFinishInt + 3 AS NVARCHAR(20))),
        (N'Std_ExamFinish', CAST(@StartFinishInt + 4 AS NVARCHAR(20)));


    PRINT N'[14/14] تجهيز محتوى الموقع (الصفحة الرئيسية ونموذج التسجيل والبانرات)...';

    -- الصفحة الرئيسية (New_Home_Page)
    IF NOT EXISTS (SELECT 1 FROM dbo.New_Home_Page)
    BEGIN
        DECLARE @HomePageOut TABLE (ID INT);
        INSERT INTO dbo.New_Home_Page (phoneNumber, email, MainVideo, MainImage, MainText)
        OUTPUT inserted.ID INTO @HomePageOut
        VALUES (
            N'+966112345678',
            N'contact@tbsetaca.example.test',
            N'https://www.youtube-nocookie.com/embed/sample_welcome',
            N'tabseet_hero_banner.jpg',
            N'أهلاً بكم في منصة أكاديمية تبسيط الدولية للتعليم والتدريب الأكاديمي والمهني المعتمد. [TEST_TABSEET_2026]'
        );
        INSERT INTO dbo.__TestDataManifest (TableName, RecordId)
        SELECT N'New_Home_Page', CAST(ID AS NVARCHAR(20)) FROM @HomePageOut;
    END

    -- صفحة ونموذج التسجيل (New_LR) لضمان عرض واجهة التسجيل العربي
    IF NOT EXISTS (SELECT 1 FROM dbo.New_LR WHERE Lang_ID = 1)
    BEGIN
        DECLARE @LROut TABLE (ID INT);
        INSERT INTO dbo.New_LR (
            Lang_ID, Title, Image_cover, Password_tran, FirstName_tran, LastName_tran,
            Place_of_birth_tran, Mobile_number_tran, Email_tran, Country, Private_one, Private_two, Private_three
        )
        OUTPUT inserted.ID INTO @LROut
        VALUES (
            1,
            N'إنشاء حساب طالب جديد - أكاديمية تبسيط الدولية',
            N'reg_cover_ar.jpg',
            N'كلمة المرور',
            N'الاسم الأول',
            N'اسم العائلة',
            N'مكان الميلاد',
            N'رقم الجوال',
            N'البريد الإلكتروني',
            N'الدولة',
            N'أوافق على الشروط والأحكام الخاصة بالأكاديمية',
            N'تسجيل الحساب',
            N'لديك حساب بالفعل؟ تسجيل الدخول'
        );
        INSERT INTO dbo.__TestDataManifest (TableName, RecordId)
        SELECT N'New_LR', CAST(ID AS NVARCHAR(20)) FROM @LROut;
    END

    -- بانرات وسلايدر الأكاديمية (New_Slider)
    IF NOT EXISTS (SELECT 1 FROM dbo.New_Slider)
    BEGIN
        DECLARE @SliderOut TABLE (ID INT);
        INSERT INTO dbo.New_Slider (Slider_Image, Lang_ID, TextMessage)
        OUTPUT inserted.ID INTO @SliderOut
        VALUES
            (N'slider_01_tabseet.jpg', 1, N'أكاديمية تبسيط الدولية - تعليم نوعي يجمع بين الأصالة والحداثة التقنية. [TEST_TABSEET_2026]'),
            (N'slider_02_tabseet.jpg', 1, N'دورات قرآنية ولغوية وعلمية معتمدة مع نخبة من كبار العلماء والمختصين. [TEST_TABSEET_2026]');
        INSERT INTO dbo.__TestDataManifest (TableName, RecordId)
        SELECT N'New_Slider', CAST(ID AS NVARCHAR(20)) FROM @SliderOut;
    END

    COMMIT TRANSACTION;

    PRINT N'==============================================================================';
    PRINT N'  تم إدخال كافة البيانات التجريبية بنجاح تام وتثبيت المعاملة (COMMIT)!';
    PRINT N'  جميع السجلات موثقة في dbo.__TestDataManifest بالعلامة TEST_TABSEET_2026.';
    PRINT N'==============================================================================';

END TRY
BEGIN CATCH
    IF (XACT_STATE() <> 0)
    BEGIN
        ROLLBACK TRANSACTION;
        PRINT N'[تراجع تلقائي] تم التراجع عن المعاملة (ROLLBACK) بسبب حدوث خطأ أثناء الإدخال.';
    END

    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSev INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    DECLARE @ErrLine INT = ERROR_LINE();

    PRINT N'[تفاصيل الخطأ]: السطر (' + CAST(@ErrLine AS NVARCHAR(10)) + N'): ' + @ErrMsg;
    RAISERROR(@ErrMsg, @ErrSev, @ErrState);
END CATCH
GO
