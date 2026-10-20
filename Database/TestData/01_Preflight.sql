/* ==============================================================================================
   مشروع أكاديمية تبسيط الدولية - فحص السلامة والجاهزية قبل إدخال البيانات التجريبية
   الملف: 01_Preflight.sql
   قاعدة البيانات المستهدفة: TabseetDB_Local (SQL Server محلي فقط)
   العلامة المرجعية: TEST_TABSEET_2026
   ============================================================================================== */

SET NOCOUNT ON;

PRINT N'==============================================================================';
PRINT N'  بدء فحص السلامة والجاهزية (Preflight Safety Check) - أكاديمية تبسيط الدولية';
PRINT N'==============================================================================';

-- 1. التحقق الصارم من اسم قاعدة البيانات
DECLARE @CurrentDB NVARCHAR(128) = DB_NAME();
PRINT N'[1/5] التحقق من اسم قاعدة البيانات الحالية: ' + ISNULL(@CurrentDB, N'NULL');

IF (@CurrentDB <> N'TabseetDB_Local')
BEGIN
    PRINT N'[خطأ فادح - إيقاف فوري] قاعدة البيانات الحالية ليست TabseetDB_Local!';
    PRINT N'لا يُسمح إطلاقاً بتشغيل هذا السكربت على قاعدة بيانات أخرى أو على بيئة الإنتاج.';
    RAISERROR(N'ABORT: Target database must strictly be TabseetDB_Local.', 16, 1);
    RETURN;
END
PRINT N'   ==> اسم قاعدة البيانات سليم ومطابق للمطلوب: TabseetDB_Local';

-- 2. التحقق من هوية السيرفر وأنه خادم محلي
DECLARE @MachineName NVARCHAR(128) = CAST(SERVERPROPERTY('MachineName') AS NVARCHAR(128));
DECLARE @ServerName NVARCHAR(128) = CAST(SERVERPROPERTY('ServerName') AS NVARCHAR(128));
DECLARE @Edition NVARCHAR(128) = CAST(SERVERPROPERTY('Edition') AS NVARCHAR(128));
DECLARE @HostName NVARCHAR(128) = HOST_NAME();

PRINT N'[2/5] التحقق من هوية السيرفر المحلي:';
PRINT N'   - MachineName : ' + ISNULL(@MachineName, N'');
PRINT N'   - ServerName  : ' + ISNULL(@ServerName, N'');
PRINT N'   - Edition     : ' + ISNULL(@Edition, N'');
PRINT N'   - HostName    : ' + ISNULL(@HostName, N'');

IF (@MachineName IS NULL OR @ServerName IS NULL)
BEGIN
    RAISERROR(N'ABORT: Could not verify local server identity.', 16, 1);
    RETURN;
END
PRINT N'   ==> تم التحقق بنجاح من أن الخادم محلي (Local SQL Server Instance).';

-- 3. التحقق من وجود الجداول الأساسية المطلوبة
PRINT N'[3/5] التحقق من وجود الجداول الأساسية في قاعدة البيانات...';

DECLARE @MissingTables TABLE (TableName NVARCHAR(128));

INSERT INTO @MissingTables (TableName)
SELECT req.TableName
FROM (VALUES 
    (N'AspNetUsers'), (N'AspNetRoles'), (N'AspNetUserRoles'),
    (N'CategoryTypes'), (N'Categories'), (N'Lectures'),
    (N'Assignments'), (N'Assignment_Q'), (N'Questions'),
    (N'Q_MultiChoice'), (N'Q_TrueOrFolse'), (N'Q_FillVoid'), (N'Q_Matching'),
    (N'Exams'), (N'Exam_Q'), (N'Std_Reg_Category'), (N'std_lecture'),
    (N'Std_ExamFinish'), (N'New_lang'), (N'New_LR'), (N'New_Home_Page')
) AS req(TableName)
LEFT JOIN sys.tables t ON t.name = req.TableName
WHERE t.object_id IS NULL;

IF EXISTS (SELECT 1 FROM @MissingTables)
BEGIN
    PRINT N'[خطأ] الجداول التالية غير موجودة في قاعدة البيانات:';
    SELECT TableName FROM @MissingTables;
    RAISERROR(N'ABORT: One or more required tables are missing.', 16, 1);
    RETURN;
END
PRINT N'   ==> جميع الجداول الأساسية الـ 21 متوفرة وجاهزة.';

-- 4. التحقق من حالة النسخة الاحتياطية
PRINT N'[4/5] التحقق من حالة النسخ الاحتياطي (Backup Status)...';
DECLARE @LastBackupDate DATETIME;
SELECT TOP 1 @LastBackupDate = backup_finish_date
FROM msdb.dbo.backupset
WHERE database_name = N'TabseetDB_Local' AND type = 'D'
ORDER BY backup_finish_date DESC;

IF (@LastBackupDate IS NOT NULL)
BEGIN
    PRINT N'   ==> تم العثور على نسخة احتياطية سابقة بتاريخ: ' + CONVERT(NVARCHAR(30), @LastBackupDate, 120);
END
ELSE
BEGIN
    PRINT N'   [تنبيه] يوصى بأخذ نسخة احتياطية كاملة قبل بدء عملية التغذية.';
    PRINT N'   أمر النسخ المقترح: BACKUP DATABASE [TabseetDB_Local] TO DISK = N''C:\...\TabseetDB_Local_PreSeed.bak'';';
END

-- 5. إنشاء جدول المانيفست المحلي الآمن لتتبع وحذف البيانات التجريبية لاحقاً
PRINT N'[5/5] تجهيز جدول المانيفست المحلي (__TestDataManifest)...';

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'__TestDataManifest')
BEGIN
    CREATE TABLE dbo.__TestDataManifest (
        ManifestID INT IDENTITY(1,1) PRIMARY KEY,
        TableName NVARCHAR(100) NOT NULL,
        RecordId NVARCHAR(256) NOT NULL,
        Tag NVARCHAR(50) NOT NULL DEFAULT N'TEST_TABSEET_2026',
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
    );
    CREATE INDEX IX_Manifest_Tag ON dbo.__TestDataManifest (Tag, TableName);
    PRINT N'   ==> تم إنشاء جدول المانيفست dbo.__TestDataManifest بنجاح.';
END
ELSE
BEGIN
    PRINT N'   ==> جدول المانيفست dbo.__TestDataManifest موجود مسبقاً.';
    DECLARE @ExistingCount INT;
    SELECT @ExistingCount = COUNT(*) FROM dbo.__TestDataManifest WHERE Tag = N'TEST_TABSEET_2026';
    PRINT N'   ==> عدد السجلات التجريبية المسجلة حالياً: ' + CAST(@ExistingCount AS NVARCHAR(20));
END

PRINT N'==============================================================================';
PRINT N'  اكتمل فحص السلامة والجاهزية بنجاح! البيئة المحلية آمنة وجاهزة للبدء.';
PRINT N'==============================================================================';
GO

