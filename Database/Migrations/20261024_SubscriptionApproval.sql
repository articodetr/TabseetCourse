-- ==============================================================================
-- Migration: 20261024_SubscriptionApproval.sql
-- Description: نظام طلبات الاشتراك بموافقة الإدارة (بدون أي دفع داخل الموقع).
--              يضيف إلى جدول Std_Reg_Category ثلاثة أعمدة توثيق اختيارية فقط:
--                RequestedAt : تاريخ طلب الاشتراك
--                ApprovedAt  : تاريخ موافقة الإدارة (تأكيد الدفع خارج الموقع)
--                ApprovedBy  : معرّف حساب الإدارة الذي اعتمد الطلب
--              ويضيف فهرساً على (الطالب، الدورة) لتسريع التحقق من الصلاحية ومنع التكرار.
-- Safe & idempotent: لا يحذف ولا يعدّل أي صف موجود، ولا يغيّر حالات الطلاب الحاليين
--   (category_Ispayed / std_category_Ispayed تبقى كما هي وتُقرأ بنفس المعنى القديم).
-- ==============================================================================

-- Target database: pass it with  sqlcmd -d  (tbseedDB on the server, TabseetDB_Local locally)
-- or select it in SSMS. The guard below skips the whole script when it runs inside master.
IF DB_NAME() = N'master'
BEGIN
    RAISERROR(N'Select the Tabseet database first (sqlcmd -d tbseedDB). Nothing was changed.', 16, 1);
    SET NOEXEC ON;
END
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- 1. أعمدة التوثيق (كلها NULL، لا تؤثر على EDMX ولا على الكود القديم)
IF COL_LENGTH('dbo.Std_Reg_Category', 'RequestedAt') IS NULL
    ALTER TABLE [dbo].[Std_Reg_Category] ADD [RequestedAt] DATETIME2 NULL;
IF COL_LENGTH('dbo.Std_Reg_Category', 'ApprovedAt') IS NULL
    ALTER TABLE [dbo].[Std_Reg_Category] ADD [ApprovedAt] DATETIME2 NULL;
IF COL_LENGTH('dbo.Std_Reg_Category', 'ApprovedBy') IS NULL
    ALTER TABLE [dbo].[Std_Reg_Category] ADD [ApprovedBy] NVARCHAR(128) NULL;
PRINT 'Std_Reg_Category approval audit columns ensured.';
GO

-- 2. فهرس (الطالب، الدورة):
--    يُنشأ فريداً عندما لا توجد صفوف مكررة (يمنع تكرار الطلب على مستوى قاعدة البيانات)،
--    وإلا يُنشأ فهرساً عادياً مع تحذير حتى لا نحذف أي سجل تاريخي.
--    (فهرس غير مُرشَّح عمداً، انظر ملاحظة QUOTED_IDENTIFIER في ترحيل الحضور)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Std_Reg_Category_Std_Category' AND object_id = OBJECT_ID('dbo.Std_Reg_Category'))
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[Std_Reg_Category]
               WHERE Std_ID IS NOT NULL AND Category_ID IS NOT NULL
               GROUP BY Std_ID, Category_ID HAVING COUNT(*) > 1)
    BEGIN
        CREATE NONCLUSTERED INDEX IX_Std_Reg_Category_Std_Category
            ON [dbo].[Std_Reg_Category] ([Std_ID], [Category_ID]);
        PRINT 'WARNING: duplicate (Std_ID, Category_ID) rows exist; created a NON-unique index. Review duplicates manually.';
    END
    ELSE
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX IX_Std_Reg_Category_Std_Category
            ON [dbo].[Std_Reg_Category] ([Std_ID], [Category_ID]);
        PRINT 'Created unique index IX_Std_Reg_Category_Std_Category.';
    END
END
ELSE
    PRINT 'Index IX_Std_Reg_Category_Std_Category already exists.';
GO
SET NOEXEC OFF;
GO
