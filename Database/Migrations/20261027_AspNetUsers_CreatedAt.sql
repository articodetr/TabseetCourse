-- ==============================================================================
-- Migration: 20261027_AspNetUsers_CreatedAt.sql
-- Description: قاعدة «الأحدث أولاً» (Newest First) لقوائم النظام.
--              كل الجداول الأخرى تملك معرّفاً تسلسلياً IDENTITY يعكس ترتيب الإنشاء، أما جدول
--              AspNetUsers فمفتاحه GUID عشوائي بلا أي عمود تاريخ، فلا يمكن استنتاج ترتيب إنشاء
--              الطلاب/المدرسين/مستخدمي الإدارة منه. يضيف هذا الترحيل عموداً واحداً اختيارياً:
--                CreatedAt : تاريخ إنشاء الحساب (UTC)، يُملأ تلقائياً بقيمة افتراضية للصفوف الجديدة فقط.
-- Safe & idempotent: لا يحذف ولا يعدّل أي صف موجود (الحسابات الحالية تبقى CreatedAt = NULL
--   وتُعرض بعد الحسابات الجديدة بترتيبها السابق). لا يمس EDMX ولا نموذج Identity (الكود يقرأ
--   العمود بطلب SQL مستقل ويعمل بدونه إن لم يُطبَّق الترحيل بعد).
-- ==============================================================================

IF DB_NAME() = N'master'
BEGIN
    RAISERROR(N'Select the Tabseet database first (sqlcmd -d tbseedDB). Nothing was changed.', 16, 1);
    SET NOEXEC ON;
END
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('dbo.AspNetUsers', 'CreatedAt') IS NULL
BEGIN
    ALTER TABLE [dbo].[AspNetUsers]
        ADD [CreatedAt] DATETIME2 NULL
            CONSTRAINT [DF_AspNetUsers_CreatedAt] DEFAULT (SYSUTCDATETIME());
    PRINT 'AspNetUsers.CreatedAt added (NULL for existing rows, default SYSUTCDATETIME() for new rows).';
END
ELSE
    PRINT 'AspNetUsers.CreatedAt already exists.';
GO
SET NOEXEC OFF;
GO
