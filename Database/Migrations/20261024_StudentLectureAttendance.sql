-- ==============================================================================
-- Migration: 20261024_StudentLectureAttendance.sql
-- Description: يضيف إلى جدول std_lecture أعمدة قياس الحضور الفعلي للمحاضرات
--              (وقت البدء، آخر نبضة، ثواني المشاهدة، المطلوب، وقت الاكتمال، المصدر، من سجّل يدوياً)
--              ويزيل الصفوف المكررة ثم يضيف قيداً فريداً على (الطالب، المحاضرة).
-- Safe, idempotent, preserves all existing rows and flags (IsFinish / IsTimeEnd / IsAssingmetFinish).
-- ==============================================================================

USE [TabseetDB_Local];
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- 1. إزالة الصفوف المكررة لنفس الطالب والمحاضرة (يُحتفظ بالصف الأكثر اكتمالاً ثم الأقدم)
;WITH dup AS (
    SELECT ID,
           ROW_NUMBER() OVER (
               PARTITION BY Std_ID, Lecture_ID
               ORDER BY ISNULL(IsFinish, 0) DESC, ISNULL(IsTimeEnd, 0) DESC, ISNULL(IsAssingmetFinish, 0) DESC, ID ASC
           ) AS rn
    FROM [dbo].[std_lecture]
    WHERE Std_ID IS NOT NULL AND Lecture_ID IS NOT NULL
)
DELETE FROM dup WHERE rn > 1;
PRINT 'Duplicate std_lecture rows removed (if any).';
GO

-- 2. الأعمدة الجديدة (كلها اختيارية أو بقيمة افتراضية، لا تؤثر على الكود القديم)
IF COL_LENGTH('dbo.std_lecture', 'StartedAt') IS NULL
    ALTER TABLE [dbo].[std_lecture] ADD [StartedAt] DATETIME2 NULL;
IF COL_LENGTH('dbo.std_lecture', 'LastHeartbeatAt') IS NULL
    ALTER TABLE [dbo].[std_lecture] ADD [LastHeartbeatAt] DATETIME2 NULL;
IF COL_LENGTH('dbo.std_lecture', 'WatchedSeconds') IS NULL
    ALTER TABLE [dbo].[std_lecture] ADD [WatchedSeconds] INT NOT NULL CONSTRAINT DF_std_lecture_WatchedSeconds DEFAULT 0;
IF COL_LENGTH('dbo.std_lecture', 'RequiredSeconds') IS NULL
    ALTER TABLE [dbo].[std_lecture] ADD [RequiredSeconds] INT NULL;
IF COL_LENGTH('dbo.std_lecture', 'CompletedAt') IS NULL
    ALTER TABLE [dbo].[std_lecture] ADD [CompletedAt] DATETIME2 NULL;
IF COL_LENGTH('dbo.std_lecture', 'Source') IS NULL
    ALTER TABLE [dbo].[std_lecture] ADD [Source] NVARCHAR(20) NULL;   -- Recorded | Live | Assignment | Manual | Legacy
IF COL_LENGTH('dbo.std_lecture', 'MarkedBy') IS NULL
    ALTER TABLE [dbo].[std_lecture] ADD [MarkedBy] NVARCHAR(128) NULL; -- معرّف المدرس عند التسجيل اليدوي
IF COL_LENGTH('dbo.std_lecture', 'MarkedAt') IS NULL
    ALTER TABLE [dbo].[std_lecture] ADD [MarkedAt] DATETIME2 NULL;
PRINT 'std_lecture attendance columns ensured.';
GO

-- 3. وسم الصفوف القديمة المكتملة كبيانات سابقة لهذا النظام
UPDATE [dbo].[std_lecture]
SET [Source] = N'Legacy'
WHERE [Source] IS NULL AND ISNULL([IsFinish], 0) = 1;
GO

-- 4. قيد فريد يمنع تكرار صف الطالب/المحاضرة مستقبلاً
--    (فهرس عادي غير مُرشَّح عمداً: الفهرس المُرشَّح يفرض QUOTED_IDENTIFIER ON على كل أوامر التعديل،
--     فتفشل سكربتات sqlcmd الافتراضية وأدوات الصيانة عند تعديل الجدول)
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_std_lecture_Std_Lecture' AND object_id = OBJECT_ID('dbo.std_lecture') AND has_filter = 1)
BEGIN
    DROP INDEX UX_std_lecture_Std_Lecture ON [dbo].[std_lecture];
    PRINT 'Dropped filtered index UX_std_lecture_Std_Lecture (replaced by a plain unique index).';
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_std_lecture_Std_Lecture' AND object_id = OBJECT_ID('dbo.std_lecture'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_std_lecture_Std_Lecture
        ON [dbo].[std_lecture] ([Std_ID], [Lecture_ID]);
    PRINT 'Created unique index UX_std_lecture_Std_Lecture.';
END
ELSE
    PRINT 'Unique index UX_std_lecture_Std_Lecture already exists.';
GO
