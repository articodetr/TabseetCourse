-- ==============================================================================
-- Migration: 20261023_CreateTeacherNotificationsSystem.sql
-- Description: Creates TeacherLectureStatus, TeacherLectureEvents, and AdminNotifications tables.
-- Safe, idempotent, preserves all existing tables and data.
-- ==============================================================================

-- Target database: pass it with  sqlcmd -d  (tbseedDB on the server, TabseetDB_Local locally)
-- or select it in SSMS. The guard below skips the whole script when it runs inside master.
IF DB_NAME() = N'master'
BEGIN
    RAISERROR(N'Select the Tabseet database first (sqlcmd -d tbseedDB). Nothing was changed.', 16, 1);
    SET NOEXEC ON;
END
GO

-- 1. Table: TeacherLectureStatus
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TeacherLectureStatus')
BEGIN
    CREATE TABLE [dbo].[TeacherLectureStatus] (
        [LectureId] INT NOT NULL,
        [CurrentStatus] NVARCHAR(50) NOT NULL CONSTRAINT DF_TeacherLectureStatus_CurrentStatus DEFAULT 'Scheduled',
        [TeacherId] NVARCHAR(128) NULL,
        [TeacherName] NVARCHAR(256) NULL,
        [ActualCompletionDate] DATETIME2 NULL,
        [Notes] NVARCHAR(MAX) NULL,
        [PostponeReason] NVARCHAR(MAX) NULL,
        [CreatedDate] DATETIME2 NOT NULL CONSTRAINT DF_TeacherLectureStatus_CreatedDate DEFAULT SYSUTCDATETIME(),
        [UpdatedDate] DATETIME2 NOT NULL CONSTRAINT DF_TeacherLectureStatus_UpdatedDate DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_TeacherLectureStatus PRIMARY KEY CLUSTERED ([LectureId] ASC),
        CONSTRAINT FK_TeacherLectureStatus_Lectures FOREIGN KEY ([LectureId]) REFERENCES [dbo].[Lectures] ([ID]) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_TeacherLectureStatus_TeacherId ON [dbo].[TeacherLectureStatus] ([TeacherId]);
    CREATE NONCLUSTERED INDEX IX_TeacherLectureStatus_CurrentStatus ON [dbo].[TeacherLectureStatus] ([CurrentStatus]);
    
    PRINT 'Created table [dbo].[TeacherLectureStatus] successfully.';
END
ELSE
BEGIN
    PRINT 'Table [dbo].[TeacherLectureStatus] already exists.';
END
GO

-- 2. Table: TeacherLectureEvents
-- Retains audit trail even if lecture is deleted (no FK cascade, preserves snapshot data)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TeacherLectureEvents')
BEGIN
    CREATE TABLE [dbo].[TeacherLectureEvents] (
        [EventId] INT IDENTITY(1,1) NOT NULL,
        [LectureId] INT NULL,
        [CategoryId] INT NULL,
        [CourseName] NVARCHAR(256) NOT NULL,
        [LectureTitle] NVARCHAR(256) NOT NULL,
        [TeacherId] NVARCHAR(128) NULL,
        [TeacherName] NVARCHAR(256) NOT NULL,
        [ActionType] NVARCHAR(100) NOT NULL,
        [OldValue] NVARCHAR(MAX) NULL,
        [NewValue] NVARCHAR(MAX) NULL,
        [Notes] NVARCHAR(MAX) NULL,
        [Details] NVARCHAR(MAX) NULL,
        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT DF_TeacherLectureEvents_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_TeacherLectureEvents PRIMARY KEY CLUSTERED ([EventId] ASC)
    );

    CREATE NONCLUSTERED INDEX IX_TeacherLectureEvents_LectureId ON [dbo].[TeacherLectureEvents] ([LectureId]);
    CREATE NONCLUSTERED INDEX IX_TeacherLectureEvents_TeacherId ON [dbo].[TeacherLectureEvents] ([TeacherId]);
    CREATE NONCLUSTERED INDEX IX_TeacherLectureEvents_ActionType ON [dbo].[TeacherLectureEvents] ([ActionType]);
    CREATE NONCLUSTERED INDEX IX_TeacherLectureEvents_CreatedAt ON [dbo].[TeacherLectureEvents] ([CreatedAt] DESC);

    PRINT 'Created table [dbo].[TeacherLectureEvents] successfully.';
END
ELSE
BEGIN
    PRINT 'Table [dbo].[TeacherLectureEvents] already exists.';
END
GO

-- 3. Table: AdminNotifications
-- Independent read tracking per admin user
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AdminNotifications')
BEGIN
    CREATE TABLE [dbo].[AdminNotifications] (
        [NotificationId] INT IDENTITY(1,1) NOT NULL,
        [EventId] INT NOT NULL,
        [AdminUserId] NVARCHAR(128) NOT NULL,
        [Title] NVARCHAR(256) NOT NULL,
        [Message] NVARCHAR(MAX) NOT NULL,
        [IsRead] BIT NOT NULL CONSTRAINT DF_AdminNotifications_IsRead DEFAULT 0,
        [ReadAt] DATETIME2 NULL,
        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT DF_AdminNotifications_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_AdminNotifications PRIMARY KEY CLUSTERED ([NotificationId] ASC),
        CONSTRAINT FK_AdminNotifications_TeacherLectureEvents FOREIGN KEY ([EventId]) REFERENCES [dbo].[TeacherLectureEvents] ([EventId]) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_AdminNotifications_AdminUserId_IsRead ON [dbo].[AdminNotifications] ([AdminUserId], [IsRead], [CreatedAt] DESC);
    CREATE NONCLUSTERED INDEX IX_AdminNotifications_EventId ON [dbo].[AdminNotifications] ([EventId]);

    PRINT 'Created table [dbo].[AdminNotifications] successfully.';
END
ELSE
BEGIN
    PRINT 'Table [dbo].[AdminNotifications] already exists.';
END
GO

SET NOEXEC OFF;
GO
