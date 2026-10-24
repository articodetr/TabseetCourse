
-- --------------------------------------------------
-- Entity Designer DDL Script for SQL Server 2005, 2008, 2012 and Azure
-- --------------------------------------------------
-- Date Created: 02/09/2023 02:12:28
-- Generated from EDMX file: C:\Users\khaled\source\repos\tabseetCourse\tabseetCourse\Data\TabseetDB.edmx
-- --------------------------------------------------

SET QUOTED_IDENTIFIER OFF;
GO
USE [db_a64c1e_tabseetsql];
GO
IF SCHEMA_ID(N'dbo') IS NULL EXECUTE(N'CREATE SCHEMA [dbo]');
GO

-- --------------------------------------------------
-- Dropping existing FOREIGN KEY constraints
-- --------------------------------------------------

IF OBJECT_ID(N'[dbo].[FK_Assignment_Lecture]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Assignments] DROP CONSTRAINT [FK_Assignment_Lecture];
GO
IF OBJECT_ID(N'[dbo].[FK_Assignment_Q_Assignment]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Assignment_Q] DROP CONSTRAINT [FK_Assignment_Q_Assignment];
GO
IF OBJECT_ID(N'[dbo].[FK_Assignment_Q_Questions]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Assignment_Q] DROP CONSTRAINT [FK_Assignment_Q_Questions];
GO
IF OBJECT_ID(N'[dbo].[FK_Category_Category]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Categories] DROP CONSTRAINT [FK_Category_Category];
GO
IF OBJECT_ID(N'[dbo].[FK_Category_CategoryType]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Categories] DROP CONSTRAINT [FK_Category_CategoryType];
GO
IF OBJECT_ID(N'[dbo].[FK_CategoryType_CategoryType]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[CategoryTypes] DROP CONSTRAINT [FK_CategoryType_CategoryType];
GO
IF OBJECT_ID(N'[dbo].[FK_dbo_AspNetUserClaims_dbo_AspNetUsers_UserId]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[AspNetUserClaims] DROP CONSTRAINT [FK_dbo_AspNetUserClaims_dbo_AspNetUsers_UserId];
GO
IF OBJECT_ID(N'[dbo].[FK_dbo_AspNetUserLogins_dbo_AspNetUsers_UserId]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[AspNetUserLogins] DROP CONSTRAINT [FK_dbo_AspNetUserLogins_dbo_AspNetUsers_UserId];
GO
IF OBJECT_ID(N'[dbo].[FK_dbo_AspNetUserRoles_dbo_AspNetRoles_RoleId]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[AspNetUserRoles] DROP CONSTRAINT [FK_dbo_AspNetUserRoles_dbo_AspNetRoles_RoleId];
GO
IF OBJECT_ID(N'[dbo].[FK_dbo_AspNetUserRoles_dbo_AspNetUsers_UserId]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[AspNetUserRoles] DROP CONSTRAINT [FK_dbo_AspNetUserRoles_dbo_AspNetUsers_UserId];
GO
IF OBJECT_ID(N'[dbo].[FK_Exam_Q_Exams]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Exam_Q] DROP CONSTRAINT [FK_Exam_Q_Exams];
GO
IF OBJECT_ID(N'[dbo].[FK_Exam_Q_Questions]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Exam_Q] DROP CONSTRAINT [FK_Exam_Q_Questions];
GO
IF OBJECT_ID(N'[dbo].[FK_Exams_Category]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Exams] DROP CONSTRAINT [FK_Exams_Category];
GO
IF OBJECT_ID(N'[dbo].[FK_GridPage_Content_GridPage]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[NewGridPage_Content] DROP CONSTRAINT [FK_GridPage_Content_GridPage];
GO
IF OBJECT_ID(N'[dbo].[FK_GridPage_New_lang]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[NewGridPages] DROP CONSTRAINT [FK_GridPage_New_lang];
GO
IF OBJECT_ID(N'[dbo].[FK_GridPage_New_pages]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[NewGridPages] DROP CONSTRAINT [FK_GridPage_New_pages];
GO
IF OBJECT_ID(N'[dbo].[FK_Lecture_Category]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Lectures] DROP CONSTRAINT [FK_Lecture_Category];
GO
IF OBJECT_ID(N'[dbo].[FK_New_About_Content_New_About]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[New_About_Content] DROP CONSTRAINT [FK_New_About_Content_New_About];
GO
IF OBJECT_ID(N'[dbo].[FK_New_About_New_lang]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[New_About] DROP CONSTRAINT [FK_New_About_New_lang];
GO
IF OBJECT_ID(N'[dbo].[FK_New_About_New_pages]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[New_About] DROP CONSTRAINT [FK_New_About_New_pages];
GO
IF OBJECT_ID(N'[dbo].[FK_New_ContactUs_New_lang]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[New_ContactUs] DROP CONSTRAINT [FK_New_ContactUs_New_lang];
GO
IF OBJECT_ID(N'[dbo].[FK_New_ContactUs_New_pages]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[New_ContactUs] DROP CONSTRAINT [FK_New_ContactUs_New_pages];
GO
IF OBJECT_ID(N'[dbo].[FK_New_FastNews_New_lang]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[New_FastNews] DROP CONSTRAINT [FK_New_FastNews_New_lang];
GO
IF OBJECT_ID(N'[dbo].[FK_New_FooterMenu_New_pages]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[New_FooterMenu] DROP CONSTRAINT [FK_New_FooterMenu_New_pages];
GO
IF OBJECT_ID(N'[dbo].[FK_New_LR_New_lang]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[New_LR] DROP CONSTRAINT [FK_New_LR_New_lang];
GO
IF OBJECT_ID(N'[dbo].[FK_New_Menu_New_lang]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[New_Menu] DROP CONSTRAINT [FK_New_Menu_New_lang];
GO
IF OBJECT_ID(N'[dbo].[FK_New_Menu_New_pages]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[New_Menu] DROP CONSTRAINT [FK_New_Menu_New_pages];
GO
IF OBJECT_ID(N'[dbo].[FK_New_news_New_lang]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[New_news] DROP CONSTRAINT [FK_New_news_New_lang];
GO
IF OBJECT_ID(N'[dbo].[FK_New_page_department_New_lang]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[New_page_department] DROP CONSTRAINT [FK_New_page_department_New_lang];
GO
IF OBJECT_ID(N'[dbo].[FK_New_page_department_New_Page_V_A_Horizantal_Tabe]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[New_page_department] DROP CONSTRAINT [FK_New_page_department_New_Page_V_A_Horizantal_Tabe];
GO
IF OBJECT_ID(N'[dbo].[FK_New_page_Department_tab_New_lang]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[New_page_Department_tab] DROP CONSTRAINT [FK_New_page_Department_tab_New_lang];
GO
IF OBJECT_ID(N'[dbo].[FK_New_page_Department_tab_New_page_department]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[New_page_Department_tab] DROP CONSTRAINT [FK_New_page_Department_tab_New_page_department];
GO
IF OBJECT_ID(N'[dbo].[FK_New_Page_tabe_Content_New_Page_Tabe]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[New_Page_tabe_Content] DROP CONSTRAINT [FK_New_Page_tabe_Content_New_Page_Tabe];
GO
IF OBJECT_ID(N'[dbo].[FK_New_Page_Tabe_New_lang]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[New_Page_Tabe] DROP CONSTRAINT [FK_New_Page_Tabe_New_lang];
GO
IF OBJECT_ID(N'[dbo].[FK_New_Page_Tabe_New_pages]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[New_Page_Tabe] DROP CONSTRAINT [FK_New_Page_Tabe_New_pages];
GO
IF OBJECT_ID(N'[dbo].[FK_New_Page_V_A_Horizantal_Tabe_New_lang]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[New_Page_V_A_Horizantal_Tabe] DROP CONSTRAINT [FK_New_Page_V_A_Horizantal_Tabe_New_lang];
GO
IF OBJECT_ID(N'[dbo].[FK_New_Page_V_A_Horizantal_Tabe_New_pages_facality]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[New_Page_V_A_Horizantal_Tabe] DROP CONSTRAINT [FK_New_Page_V_A_Horizantal_Tabe_New_pages_facality];
GO
IF OBJECT_ID(N'[dbo].[FK_New_Page_Vertical_and_H_Tabe_New_lang]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[New_Page_Vertical_and_H_Tabe] DROP CONSTRAINT [FK_New_Page_Vertical_and_H_Tabe_New_lang];
GO
IF OBJECT_ID(N'[dbo].[FK_New_Page_Vertical_and_H_Tabe_New_Page_V_A_Horizantal_Tabe]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[New_Page_Vertical_and_H_Tabe] DROP CONSTRAINT [FK_New_Page_Vertical_and_H_Tabe_New_Page_V_A_Horizantal_Tabe];
GO
IF OBJECT_ID(N'[dbo].[FK_New_pages_facality_New_lang]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[New_pages_facality] DROP CONSTRAINT [FK_New_pages_facality_New_lang];
GO
IF OBJECT_ID(N'[dbo].[FK_New_pages_Text_New_lang]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[New_pages_Text] DROP CONSTRAINT [FK_New_pages_Text_New_lang];
GO
IF OBJECT_ID(N'[dbo].[FK_New_pages_Text_New_pages]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[New_pages_Text] DROP CONSTRAINT [FK_New_pages_Text_New_pages];
GO
IF OBJECT_ID(N'[dbo].[FK_New_pracktes_New_lang]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[New_pracktes] DROP CONSTRAINT [FK_New_pracktes_New_lang];
GO
IF OBJECT_ID(N'[dbo].[FK_New_Slider_New_lang]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[New_Slider] DROP CONSTRAINT [FK_New_Slider_New_lang];
GO
IF OBJECT_ID(N'[dbo].[FK_New_SocialMedia_New_lang]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[New_SocialMedia] DROP CONSTRAINT [FK_New_SocialMedia_New_lang];
GO
IF OBJECT_ID(N'[dbo].[FK_Q_FillVoid_Questions]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Q_FillVoid] DROP CONSTRAINT [FK_Q_FillVoid_Questions];
GO
IF OBJECT_ID(N'[dbo].[FK_Q_Matching_Questions]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Q_Matching] DROP CONSTRAINT [FK_Q_Matching_Questions];
GO
IF OBJECT_ID(N'[dbo].[FK_Q_MultiChoice_Questions]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Q_MultiChoice] DROP CONSTRAINT [FK_Q_MultiChoice_Questions];
GO
IF OBJECT_ID(N'[dbo].[FK_Q_TrueOrFolse_Questions]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Q_TrueOrFolse] DROP CONSTRAINT [FK_Q_TrueOrFolse_Questions];
GO
IF OBJECT_ID(N'[dbo].[FK_Questions_Category]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Questions] DROP CONSTRAINT [FK_Questions_Category];
GO
IF OBJECT_ID(N'[dbo].[FK_Std_ExamFinish_Exams]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Std_ExamFinish] DROP CONSTRAINT [FK_Std_ExamFinish_Exams];
GO
IF OBJECT_ID(N'[dbo].[FK_std_lecture_Lecture]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[std_lecture] DROP CONSTRAINT [FK_std_lecture_Lecture];
GO
IF OBJECT_ID(N'[dbo].[FK_Std_Reg_Category_Category]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Std_Reg_Category] DROP CONSTRAINT [FK_Std_Reg_Category_Category];
GO
IF OBJECT_ID(N'[dbo].[FK_StoreProduct_StoreCategory]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[StoreProduct] DROP CONSTRAINT [FK_StoreProduct_StoreCategory];
GO

-- --------------------------------------------------
-- Dropping existing tables
-- --------------------------------------------------

IF OBJECT_ID(N'[dbo].[__MigrationHistory]', 'U') IS NOT NULL
    DROP TABLE [dbo].[__MigrationHistory];
GO
IF OBJECT_ID(N'[dbo].[AspNetRoles]', 'U') IS NOT NULL
    DROP TABLE [dbo].[AspNetRoles];
GO
IF OBJECT_ID(N'[dbo].[AspNetUserClaims]', 'U') IS NOT NULL
    DROP TABLE [dbo].[AspNetUserClaims];
GO
IF OBJECT_ID(N'[dbo].[AspNetUserLogins]', 'U') IS NOT NULL
    DROP TABLE [dbo].[AspNetUserLogins];
GO
IF OBJECT_ID(N'[dbo].[AspNetUserRoles]', 'U') IS NOT NULL
    DROP TABLE [dbo].[AspNetUserRoles];
GO
IF OBJECT_ID(N'[dbo].[AspNetUsers]', 'U') IS NOT NULL
    DROP TABLE [dbo].[AspNetUsers];
GO
IF OBJECT_ID(N'[dbo].[Assignment_Q]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Assignment_Q];
GO
IF OBJECT_ID(N'[dbo].[Assignments]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Assignments];
GO
IF OBJECT_ID(N'[dbo].[Categories]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Categories];
GO
IF OBJECT_ID(N'[dbo].[CategoryTypes]', 'U') IS NOT NULL
    DROP TABLE [dbo].[CategoryTypes];
GO
IF OBJECT_ID(N'[dbo].[ChatTables]', 'U') IS NOT NULL
    DROP TABLE [dbo].[ChatTables];
GO
IF OBJECT_ID(N'[dbo].[CodeGenerateds]', 'U') IS NOT NULL
    DROP TABLE [dbo].[CodeGenerateds];
GO
IF OBJECT_ID(N'[dbo].[Exam_Q]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Exam_Q];
GO
IF OBJECT_ID(N'[dbo].[Exams]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Exams];
GO
IF OBJECT_ID(N'[dbo].[Lectures]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Lectures];
GO
IF OBJECT_ID(N'[dbo].[New_About]', 'U') IS NOT NULL
    DROP TABLE [dbo].[New_About];
GO
IF OBJECT_ID(N'[dbo].[New_About_Content]', 'U') IS NOT NULL
    DROP TABLE [dbo].[New_About_Content];
GO
IF OBJECT_ID(N'[dbo].[New_ContactUs]', 'U') IS NOT NULL
    DROP TABLE [dbo].[New_ContactUs];
GO
IF OBJECT_ID(N'[dbo].[New_CustomerContactUs]', 'U') IS NOT NULL
    DROP TABLE [dbo].[New_CustomerContactUs];
GO
IF OBJECT_ID(N'[dbo].[New_FastNews]', 'U') IS NOT NULL
    DROP TABLE [dbo].[New_FastNews];
GO
IF OBJECT_ID(N'[dbo].[New_FooterMenu]', 'U') IS NOT NULL
    DROP TABLE [dbo].[New_FooterMenu];
GO
IF OBJECT_ID(N'[dbo].[New_Home_Page]', 'U') IS NOT NULL
    DROP TABLE [dbo].[New_Home_Page];
GO
IF OBJECT_ID(N'[dbo].[New_lang]', 'U') IS NOT NULL
    DROP TABLE [dbo].[New_lang];
GO
IF OBJECT_ID(N'[dbo].[New_LR]', 'U') IS NOT NULL
    DROP TABLE [dbo].[New_LR];
GO
IF OBJECT_ID(N'[dbo].[New_Menu]', 'U') IS NOT NULL
    DROP TABLE [dbo].[New_Menu];
GO
IF OBJECT_ID(N'[dbo].[New_news]', 'U') IS NOT NULL
    DROP TABLE [dbo].[New_news];
GO
IF OBJECT_ID(N'[dbo].[New_page_department]', 'U') IS NOT NULL
    DROP TABLE [dbo].[New_page_department];
GO
IF OBJECT_ID(N'[dbo].[New_page_Department_tab]', 'U') IS NOT NULL
    DROP TABLE [dbo].[New_page_Department_tab];
GO
IF OBJECT_ID(N'[dbo].[New_Page_Tabe]', 'U') IS NOT NULL
    DROP TABLE [dbo].[New_Page_Tabe];
GO
IF OBJECT_ID(N'[dbo].[New_Page_tabe_Content]', 'U') IS NOT NULL
    DROP TABLE [dbo].[New_Page_tabe_Content];
GO
IF OBJECT_ID(N'[dbo].[New_Page_V_A_Horizantal_Tabe]', 'U') IS NOT NULL
    DROP TABLE [dbo].[New_Page_V_A_Horizantal_Tabe];
GO
IF OBJECT_ID(N'[dbo].[New_Page_Vertical_and_H_Tabe]', 'U') IS NOT NULL
    DROP TABLE [dbo].[New_Page_Vertical_and_H_Tabe];
GO
IF OBJECT_ID(N'[dbo].[New_pages]', 'U') IS NOT NULL
    DROP TABLE [dbo].[New_pages];
GO
IF OBJECT_ID(N'[dbo].[New_pages_facality]', 'U') IS NOT NULL
    DROP TABLE [dbo].[New_pages_facality];
GO
IF OBJECT_ID(N'[dbo].[New_pages_Text]', 'U') IS NOT NULL
    DROP TABLE [dbo].[New_pages_Text];
GO
IF OBJECT_ID(N'[dbo].[New_pracktes]', 'U') IS NOT NULL
    DROP TABLE [dbo].[New_pracktes];
GO
IF OBJECT_ID(N'[dbo].[New_Slider]', 'U') IS NOT NULL
    DROP TABLE [dbo].[New_Slider];
GO
IF OBJECT_ID(N'[dbo].[New_SocialMedia]', 'U') IS NOT NULL
    DROP TABLE [dbo].[New_SocialMedia];
GO
IF OBJECT_ID(N'[dbo].[NewGridPage_Content]', 'U') IS NOT NULL
    DROP TABLE [dbo].[NewGridPage_Content];
GO
IF OBJECT_ID(N'[dbo].[NewGridPages]', 'U') IS NOT NULL
    DROP TABLE [dbo].[NewGridPages];
GO
IF OBJECT_ID(N'[dbo].[Q_FillVoid]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Q_FillVoid];
GO
IF OBJECT_ID(N'[dbo].[Q_Matching]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Q_Matching];
GO
IF OBJECT_ID(N'[dbo].[Q_MultiChoice]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Q_MultiChoice];
GO
IF OBJECT_ID(N'[dbo].[Q_TrueOrFolse]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Q_TrueOrFolse];
GO
IF OBJECT_ID(N'[dbo].[Questions]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Questions];
GO
IF OBJECT_ID(N'[dbo].[Std_ExamFinish]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Std_ExamFinish];
GO
IF OBJECT_ID(N'[dbo].[std_lecture]', 'U') IS NOT NULL
    DROP TABLE [dbo].[std_lecture];
GO
IF OBJECT_ID(N'[dbo].[Std_Reg_Category]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Std_Reg_Category];
GO
IF OBJECT_ID(N'[dbo].[StoreCategory]', 'U') IS NOT NULL
    DROP TABLE [dbo].[StoreCategory];
GO
IF OBJECT_ID(N'[dbo].[StoreProduct]', 'U') IS NOT NULL
    DROP TABLE [dbo].[StoreProduct];
GO
IF OBJECT_ID(N'[dbo].[StyleTables]', 'U') IS NOT NULL
    DROP TABLE [dbo].[StyleTables];
GO
IF OBJECT_ID(N'[dbo].[sysdiagrams]', 'U') IS NOT NULL
    DROP TABLE [dbo].[sysdiagrams];
GO
IF OBJECT_ID(N'[dbo].[UserLocations]', 'U') IS NOT NULL
    DROP TABLE [dbo].[UserLocations];
GO

-- --------------------------------------------------
-- Creating all tables
-- --------------------------------------------------

-- Creating table 'C__MigrationHistory'
CREATE TABLE [dbo].[C__MigrationHistory] (
    [MigrationId] nvarchar(150)  NOT NULL,
    [ContextKey] nvarchar(300)  NOT NULL,
    [Model] varbinary(max)  NOT NULL,
    [ProductVersion] nvarchar(32)  NOT NULL
);
GO

-- Creating table 'AspNetRoles'
CREATE TABLE [dbo].[AspNetRoles] (
    [Id] nvarchar(128)  NOT NULL,
    [Name] nvarchar(256)  NOT NULL
);
GO

-- Creating table 'AspNetUserClaims'
CREATE TABLE [dbo].[AspNetUserClaims] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [UserId] nvarchar(128)  NOT NULL,
    [ClaimType] nvarchar(max)  NULL,
    [ClaimValue] nvarchar(max)  NULL
);
GO

-- Creating table 'AspNetUserLogins'
CREATE TABLE [dbo].[AspNetUserLogins] (
    [LoginProvider] nvarchar(128)  NOT NULL,
    [ProviderKey] nvarchar(128)  NOT NULL,
    [UserId] nvarchar(128)  NOT NULL
);
GO

-- Creating table 'AspNetUsers'
CREATE TABLE [dbo].[AspNetUsers] (
    [Id] nvarchar(128)  NOT NULL,
    [FirstName] nvarchar(max)  NULL,
    [LastName] nvarchar(max)  NULL,
    [Password] nvarchar(max)  NULL,
    [role] nvarchar(max)  NULL,
    [Categories_ID] nvarchar(max)  NULL,
    [Per_Photo] nvarchar(max)  NULL,
    [PersoinDocument] nvarchar(max)  NULL,
    [userOrTetcherOrStudent] int  NOT NULL,
    [IsActive] int  NOT NULL,
    [Country] nvarchar(max)  NULL,
    [RealName] nvarchar(max)  NULL,
    [Studing] nvarchar(max)  NULL,
    [Experience] nvarchar(max)  NULL,
    [Education] nvarchar(max)  NULL,
    [Location] nvarchar(max)  NULL,
    [Email] nvarchar(256)  NULL,
    [EmailConfirmed] bit  NOT NULL,
    [PasswordHash] nvarchar(max)  NULL,
    [SecurityStamp] nvarchar(max)  NULL,
    [PhoneNumber] nvarchar(max)  NULL,
    [PhoneNumberConfirmed] bit  NOT NULL,
    [TwoFactorEnabled] bit  NOT NULL,
    [LockoutEndDateUtc] datetime  NULL,
    [LockoutEnabled] bit  NOT NULL,
    [AccessFailedCount] int  NOT NULL,
    [UserName] nvarchar(256)  NOT NULL
);
GO

-- Creating table 'Assignment_Q'
CREATE TABLE [dbo].[Assignment_Q] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [Q_ID] int  NOT NULL,
    [Assignment_ID] int  NOT NULL
);
GO

-- Creating table 'Assignments'
CREATE TABLE [dbo].[Assignments] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [Name] nvarchar(max)  NULL,
    [Description] nvarchar(max)  NULL,
    [Lecture_ID] int  NULL,
    [Assignmet_Degry] float  NULL,
    [Assignmet_degry_Win] int  NULL
);
GO

-- Creating table 'Categories'
CREATE TABLE [dbo].[Categories] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [NameAr] nvarchar(max)  NULL,
    [NameEn] nvarchar(max)  NULL,
    [ParentCateogry_ID] int  NULL,
    [IsPayed] int  NULL,
    [PayedValue] nvarchar(50)  NULL,
    [Description] nvarchar(max)  NULL,
    [CategoryType_ID] int  NULL,
    [DescriptionEN] nvarchar(max)  NULL,
    [New_Page_ID] int  NULL,
    [CertificationAR] nvarchar(max)  NULL,
    [EndTime] nvarchar(50)  NULL,
    [CertificationEN] nvarchar(max)  NULL,
    [CanRegister] int  NULL,
    [Icon] nvarchar(max)  NULL,
    [DocumentBook] nvarchar(max)  NULL
);
GO

-- Creating table 'CategoryTypes'
CREATE TABLE [dbo].[CategoryTypes] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [NameAR] nvarchar(100)  NULL,
    [NameEn] nvarchar(100)  NULL,
    [ParentType] int  NULL
);
GO

-- Creating table 'ChatTables'
CREATE TABLE [dbo].[ChatTables] (
    [int] int IDENTITY(1,1) NOT NULL,
    [Message] nvarchar(max)  NULL,
    [Category_ID] int  NULL,
    [User_ID] nvarchar(128)  NULL,
    [date_Time] nvarchar(50)  NULL
);
GO

-- Creating table 'CodeGenerateds'
CREATE TABLE [dbo].[CodeGenerateds] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [Code] nvarchar(50)  NULL,
    [TimeEnd] nvarchar(50)  NULL,
    [Count] int  NULL,
    [User_ID] nvarchar(128)  NULL
);
GO

-- Creating table 'Exam_Q'
CREATE TABLE [dbo].[Exam_Q] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [Q_ID] int  NOT NULL,
    [Exam_ID] int  NOT NULL
);
GO

-- Creating table 'Exams'
CREATE TABLE [dbo].[Exams] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [Name] nvarchar(max)  NULL,
    [Description] nvarchar(max)  NULL,
    [Day_Time] nvarchar(max)  NULL,
    [Start_Time] nvarchar(max)  NULL,
    [Duration] float  NULL,
    [Category_ID] int  NULL,
    [Exam_Degry] float  NULL,
    [Exam_win_Degry] float  NULL
);
GO

-- Creating table 'Lectures'
CREATE TABLE [dbo].[Lectures] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [Lecture_Name] nvarchar(max)  NOT NULL,
    [Category_ID] int  NOT NULL,
    [DayandTime] nvarchar(max)  NULL,
    [LectureTypeOnOff] nvarchar(max)  NULL,
    [LectureLink_Host] nvarchar(max)  NULL,
    [DocumenFile] nvarchar(max)  NULL,
    [MediaLink] nvarchar(max)  NULL,
    [Others] nvarchar(max)  NULL,
    [LectureLink_Client] nvarchar(max)  NULL,
    [LectureMinTime] nvarchar(50)  NULL
);
GO

-- Creating table 'New_About'
CREATE TABLE [dbo].[New_About] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [new_pages_ID] int  NOT NULL,
    [Lang_ID] int  NOT NULL,
    [Title] nvarchar(50)  NULL,
    [Image_Cover] nvarchar(max)  NULL
);
GO

-- Creating table 'New_About_Content'
CREATE TABLE [dbo].[New_About_Content] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [New_About_ID] int  NOT NULL,
    [Tab_Title] nvarchar(50)  NOT NULL,
    [Tab_Content] nvarchar(max)  NOT NULL
);
GO

-- Creating table 'New_ContactUs'
CREATE TABLE [dbo].[New_ContactUs] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [title] nvarchar(max)  NOT NULL,
    [Lang_ID] int  NOT NULL,
    [New_Page_ID] int  NOT NULL,
    [Image_Cover] nvarchar(max)  NULL,
    [Map_Link] nvarchar(max)  NULL,
    [UN_Adress] nvarchar(max)  NULL,
    [UN_PhoneNumber] nvarchar(max)  NULL,
    [UN_Email] nvarchar(max)  NULL,
    [Name_word_trans] nvarchar(max)  NULL,
    [Email_word_trans] nvarchar(max)  NULL,
    [PhoneNumber_word_trans] nvarchar(max)  NULL,
    [Message_word_trans] nvarchar(max)  NULL,
    [Send_word_trans] nvarchar(max)  NULL,
    [Adress_word_trans] nvarchar(max)  NULL
);
GO

-- Creating table 'New_CustomerContactUs'
CREATE TABLE [dbo].[New_CustomerContactUs] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [Name] nvarchar(50)  NULL,
    [Email] nvarchar(50)  NULL,
    [PhoneNumber] nvarchar(50)  NULL,
    [Message] nvarchar(max)  NULL
);
GO

-- Creating table 'New_FastNews'
CREATE TABLE [dbo].[New_FastNews] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [Lang_ID] int  NULL,
    [NewsText] nvarchar(max)  NULL
);
GO

-- Creating table 'New_FooterMenu'
CREATE TABLE [dbo].[New_FooterMenu] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [New_page_ID] int  NOT NULL
);
GO

-- Creating table 'New_Home_Page'
CREATE TABLE [dbo].[New_Home_Page] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [phoneNumber] nvarchar(max)  NULL,
    [email] nvarchar(max)  NULL,
    [MainVideo] nvarchar(max)  NULL,
    [MainImage] nvarchar(max)  NULL,
    [MainText] nvarchar(max)  NULL
);
GO

-- Creating table 'New_lang'
CREATE TABLE [dbo].[New_lang] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [Name] nvarchar(50)  NOT NULL,
    [Direction] int  NOT NULL,
    [DefultLang] int  NULL
);
GO

-- Creating table 'New_LR'
CREATE TABLE [dbo].[New_LR] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [Lang_ID] int  NOT NULL,
    [Title] nvarchar(max)  NULL,
    [Image_cover] nvarchar(max)  NULL,
    [Password_tran] nvarchar(max)  NULL,
    [FirstName_tran] nvarchar(max)  NULL,
    [LastName_tran] nvarchar(max)  NULL,
    [Place_of_birth_tran] nvarchar(max)  NULL,
    [Mobile_number_tran] nvarchar(max)  NULL,
    [Email_tran] nvarchar(max)  NULL,
    [Private_one] nvarchar(max)  NULL,
    [Private_two] nvarchar(max)  NULL,
    [Private_three] nvarchar(max)  NULL,
    [Country] nvarchar(max)  NULL
);
GO

-- Creating table 'New_Menu'
CREATE TABLE [dbo].[New_Menu] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [Title] nvarchar(max)  NOT NULL,
    [New_Page_ID] int  NOT NULL,
    [Lang_ID] int  NOT NULL
);
GO

-- Creating table 'New_news'
CREATE TABLE [dbo].[New_news] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [Title] nvarchar(max)  NOT NULL,
    [Lang_ID] int  NOT NULL,
    [DateTime] nvarchar(max)  NULL,
    [ShortDescription] nvarchar(max)  NULL,
    [FullDescription] nvarchar(max)  NULL,
    [Cover_image] nvarchar(max)  NULL,
    [Shourt_image] nvarchar(max)  NULL
);
GO

-- Creating table 'New_page_department'
CREATE TABLE [dbo].[New_page_department] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [Title] nvarchar(max)  NOT NULL,
    [Cover_image] nvarchar(max)  NULL,
    [Shourt_image] nvarchar(max)  NULL,
    [Facility_ID] int  NOT NULL,
    [Lang_ID] int  NOT NULL
);
GO

-- Creating table 'New_page_Department_tab'
CREATE TABLE [dbo].[New_page_Department_tab] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [Title] nvarchar(max)  NOT NULL,
    [Description] nvarchar(max)  NULL,
    [Department_ID] int  NOT NULL,
    [Lang_ID] int  NOT NULL
);
GO

-- Creating table 'New_Page_Tabe'
CREATE TABLE [dbo].[New_Page_Tabe] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [Lang_ID] int  NOT NULL,
    [New_Page_ID] int  NOT NULL,
    [page_H_V_tabe] int  NOT NULL,
    [Image_Cover] nvarchar(max)  NULL,
    [Title] nvarchar(max)  NULL,
    [Category_ID] int  NULL
);
GO

-- Creating table 'New_Page_tabe_Content'
CREATE TABLE [dbo].[New_Page_tabe_Content] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [Page_Tabe_ID] int  NOT NULL,
    [Tabe_title] nvarchar(max)  NOT NULL,
    [Tab_Description] nvarchar(max)  NULL
);
GO

-- Creating table 'New_Page_V_A_Horizantal_Tabe'
CREATE TABLE [dbo].[New_Page_V_A_Horizantal_Tabe] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [Title] nvarchar(max)  NOT NULL,
    [Lang_ID] int  NOT NULL,
    [New_page_V_A_H_ID] int  NOT NULL
);
GO

-- Creating table 'New_Page_Vertical_and_H_Tabe'
CREATE TABLE [dbo].[New_Page_Vertical_and_H_Tabe] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [Title] nvarchar(max)  NOT NULL,
    [Lang_ID] int  NOT NULL,
    [New_PageHorizantal_Tabe_ID] int  NOT NULL,
    [Description] nvarchar(max)  NOT NULL
);
GO

-- Creating table 'New_pages'
CREATE TABLE [dbo].[New_pages] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [name] nvarchar(max)  NOT NULL,
    [Page_Type] int  NOT NULL,
    [IsUiUx] int  NULL,
    [CategoryType_ID] int  NULL
);
GO

-- Creating table 'New_pages_facality'
CREATE TABLE [dbo].[New_pages_facality] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [Title] nvarchar(max)  NOT NULL,
    [Image_Cover] nvarchar(max)  NOT NULL,
    [Lang_ID] int  NOT NULL,
    [New_pages_ID] int  NOT NULL
);
GO

-- Creating table 'New_pages_Text'
CREATE TABLE [dbo].[New_pages_Text] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [New_pages_ID] int  NOT NULL,
    [Lang_ID] int  NOT NULL,
    [title] nvarchar(max)  NOT NULL,
    [Image_Cover] nvarchar(max)  NULL,
    [Text_Description] nvarchar(max)  NULL
);
GO

-- Creating table 'New_pracktes'
CREATE TABLE [dbo].[New_pracktes] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [Title] nvarchar(max)  NOT NULL,
    [Lang_ID] int  NOT NULL,
    [DateTime] nvarchar(max)  NULL,
    [ShortDescription] nvarchar(max)  NULL,
    [FullDescription] nvarchar(max)  NULL,
    [Cover_image] nvarchar(max)  NULL,
    [Shourt_image] nvarchar(max)  NULL
);
GO

-- Creating table 'New_Slider'
CREATE TABLE [dbo].[New_Slider] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [Slider_Image] nvarchar(max)  NOT NULL,
    [Lang_ID] int  NOT NULL,
    [TextMessage] nvarchar(250)  NULL
);
GO

-- Creating table 'New_SocialMedia'
CREATE TABLE [dbo].[New_SocialMedia] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [Facebook] nvarchar(max)  NULL,
    [Twitter] nvarchar(max)  NULL,
    [Instgram] nvarchar(max)  NULL,
    [WhatsApp] nvarchar(max)  NULL,
    [Linkedin] nvarchar(max)  NULL,
    [Lang_ID] int  NOT NULL,
    [Youtube] nvarchar(max)  NULL,
    [Tiktok] nvarchar(max)  NULL,
    [Telegram] nvarchar(max)  NULL
);
GO

-- Creating table 'NewGridPage_Content'
CREATE TABLE [dbo].[NewGridPage_Content] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [Title] nvarchar(max)  NULL,
    [Description] nvarchar(max)  NULL,
    [Image] nvarchar(max)  NULL,
    [DocLinke] nvarchar(max)  NULL,
    [GridPageID] int  NULL
);
GO

-- Creating table 'NewGridPages'
CREATE TABLE [dbo].[NewGridPages] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [Lang_ID] int  NULL,
    [Pages_ID] int  NULL,
    [Title] nvarchar(max)  NULL,
    [Image_Cover] nvarchar(max)  NULL
);
GO

-- Creating table 'Q_FillVoid'
CREATE TABLE [dbo].[Q_FillVoid] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [Question_ID] int  NOT NULL,
    [TrueValue] nvarchar(max)  NOT NULL,
    [Degry] float  NOT NULL
);
GO

-- Creating table 'Q_Matching'
CREATE TABLE [dbo].[Q_Matching] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [Question_ID] int  NOT NULL,
    [ChoiceText] nvarchar(max)  NOT NULL,
    [Degry] float  NOT NULL,
    [ChoiseTrueAnswer] nvarchar(max)  NULL
);
GO

-- Creating table 'Q_MultiChoice'
CREATE TABLE [dbo].[Q_MultiChoice] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [Question_ID] int  NOT NULL,
    [Choose] nvarchar(max)  NOT NULL,
    [Degry] float  NOT NULL
);
GO

-- Creating table 'Q_TrueOrFolse'
CREATE TABLE [dbo].[Q_TrueOrFolse] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [Question_ID] int  NOT NULL,
    [TrueValue] int  NOT NULL,
    [Degry] float  NOT NULL
);
GO

-- Creating table 'Questions'
CREATE TABLE [dbo].[Questions] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [Type] int  NOT NULL,
    [Q_Name] nvarchar(max)  NULL,
    [Q_Text] nvarchar(max)  NOT NULL,
    [Q_Description] nvarchar(max)  NULL,
    [Category_ID] int  NULL
);
GO

-- Creating table 'Std_ExamFinish'
CREATE TABLE [dbo].[Std_ExamFinish] (
    [int] int  NOT NULL,
    [Std_ID] int  NULL,
    [Exam_ID] int  NULL,
    [std_Req_category_ID] int  NULL,
    [isFinishExam] int  NULL
);
GO

-- Creating table 'std_lecture'
CREATE TABLE [dbo].[std_lecture] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [Lecture_ID] int  NULL,
    [Std_ID] nvarchar(128)  NULL,
    [IsFinish] int  NULL,
    [IsTimeEnd] int  NULL,
    [IsAssingmetFinish] int  NULL
);
GO

-- Creating table 'Std_Reg_Category'
CREATE TABLE [dbo].[Std_Reg_Category] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [Category_ID] int  NULL,
    [Std_ID] nvarchar(128)  NULL,
    [category_Ispayed] int  NULL,
    [std_category_Ispayed] int  NULL,
    [std_exam_isFinish] int  NULL,
    [PayMent_Method] int  NULL,
    [TransactionFile] nvarchar(max)  NULL,
    [Code] nvarchar(50)  NULL,
    [PayMent_Value] nvarchar(50)  NULL,
    [std_exam_Finish_Time] nvarchar(50)  NULL,
    [Certificate_appreciation] nchar(10)  NULL,
    [Serial_Number] nchar(10)  NULL
);
GO

-- Creating table 'StoreCategories'
CREATE TABLE [dbo].[StoreCategories] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [Name] nvarchar(max)  NULL
);
GO

-- Creating table 'StoreProducts'
CREATE TABLE [dbo].[StoreProducts] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [name] nvarchar(max)  NULL,
    [Category_ID] int  NOT NULL,
    [Description] nvarchar(max)  NULL,
    [Price] float  NULL,
    [Images] nvarchar(max)  NULL
);
GO

-- Creating table 'StyleTables'
CREATE TABLE [dbo].[StyleTables] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [MinuColor] nvarchar(50)  NULL,
    [BackGroundMinuColor] nvarchar(50)  NULL,
    [UpderMenuColor] nvarchar(50)  NULL,
    [PracticeBackgroundColor] nvarchar(50)  NULL,
    [FooterColor] nvarchar(50)  NULL
);
GO

-- Creating table 'sysdiagrams'
CREATE TABLE [dbo].[sysdiagrams] (
    [name] nvarchar(128)  NOT NULL,
    [principal_id] int  NOT NULL,
    [diagram_id] int IDENTITY(1,1) NOT NULL,
    [version] int  NULL,
    [definition] varbinary(max)  NULL
);
GO

-- Creating table 'UserLocations'
CREATE TABLE [dbo].[UserLocations] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [CountryName] nvarchar(50)  NULL,
    [Count] int  NULL
);
GO

-- Creating table 'AspNetUserRoles'
CREATE TABLE [dbo].[AspNetUserRoles] (
    [RoleId] nvarchar(128)  NOT NULL,
    [UserId] nvarchar(128)  NOT NULL
);
GO

-- --------------------------------------------------
-- Creating all PRIMARY KEY constraints
-- --------------------------------------------------

-- Creating primary key on [MigrationId], [ContextKey] in table 'C__MigrationHistory'
ALTER TABLE [dbo].[C__MigrationHistory]
ADD CONSTRAINT [PK_C__MigrationHistory]
    PRIMARY KEY CLUSTERED ([MigrationId], [ContextKey] ASC);
GO

-- Creating primary key on [Id] in table 'AspNetRoles'
ALTER TABLE [dbo].[AspNetRoles]
ADD CONSTRAINT [PK_AspNetRoles]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'AspNetUserClaims'
ALTER TABLE [dbo].[AspNetUserClaims]
ADD CONSTRAINT [PK_AspNetUserClaims]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [LoginProvider], [ProviderKey], [UserId] in table 'AspNetUserLogins'
ALTER TABLE [dbo].[AspNetUserLogins]
ADD CONSTRAINT [PK_AspNetUserLogins]
    PRIMARY KEY CLUSTERED ([LoginProvider], [ProviderKey], [UserId] ASC);
GO

-- Creating primary key on [Id] in table 'AspNetUsers'
ALTER TABLE [dbo].[AspNetUsers]
ADD CONSTRAINT [PK_AspNetUsers]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [ID] in table 'Assignment_Q'
ALTER TABLE [dbo].[Assignment_Q]
ADD CONSTRAINT [PK_Assignment_Q]
    PRIMARY KEY CLUSTERED ([ID] ASC);
GO

-- Creating primary key on [ID] in table 'Assignments'
ALTER TABLE [dbo].[Assignments]
ADD CONSTRAINT [PK_Assignments]
    PRIMARY KEY CLUSTERED ([ID] ASC);
GO

-- Creating primary key on [ID] in table 'Categories'
ALTER TABLE [dbo].[Categories]
ADD CONSTRAINT [PK_Categories]
    PRIMARY KEY CLUSTERED ([ID] ASC);
GO

-- Creating primary key on [ID] in table 'CategoryTypes'
ALTER TABLE [dbo].[CategoryTypes]
ADD CONSTRAINT [PK_CategoryTypes]
    PRIMARY KEY CLUSTERED ([ID] ASC);
GO

-- Creating primary key on [int] in table 'ChatTables'
ALTER TABLE [dbo].[ChatTables]
ADD CONSTRAINT [PK_ChatTables]
    PRIMARY KEY CLUSTERED ([int] ASC);
GO

-- Creating primary key on [ID] in table 'CodeGenerateds'
ALTER TABLE [dbo].[CodeGenerateds]
ADD CONSTRAINT [PK_CodeGenerateds]
    PRIMARY KEY CLUSTERED ([ID] ASC);
GO

-- Creating primary key on [ID] in table 'Exam_Q'
ALTER TABLE [dbo].[Exam_Q]
ADD CONSTRAINT [PK_Exam_Q]
    PRIMARY KEY CLUSTERED ([ID] ASC);
GO

-- Creating primary key on [ID] in table 'Exams'
ALTER TABLE [dbo].[Exams]
ADD CONSTRAINT [PK_Exams]
    PRIMARY KEY CLUSTERED ([ID] ASC);
GO

-- Creating primary key on [ID] in table 'Lectures'
ALTER TABLE [dbo].[Lectures]
ADD CONSTRAINT [PK_Lectures]
    PRIMARY KEY CLUSTERED ([ID] ASC);
GO

-- Creating primary key on [ID] in table 'New_About'
ALTER TABLE [dbo].[New_About]
ADD CONSTRAINT [PK_New_About]
    PRIMARY KEY CLUSTERED ([ID] ASC);
GO

-- Creating primary key on [ID] in table 'New_About_Content'
ALTER TABLE [dbo].[New_About_Content]
ADD CONSTRAINT [PK_New_About_Content]
    PRIMARY KEY CLUSTERED ([ID] ASC);
GO

-- Creating primary key on [ID] in table 'New_ContactUs'
ALTER TABLE [dbo].[New_ContactUs]
ADD CONSTRAINT [PK_New_ContactUs]
    PRIMARY KEY CLUSTERED ([ID] ASC);
GO

-- Creating primary key on [ID] in table 'New_CustomerContactUs'
ALTER TABLE [dbo].[New_CustomerContactUs]
ADD CONSTRAINT [PK_New_CustomerContactUs]
    PRIMARY KEY CLUSTERED ([ID] ASC);
GO

-- Creating primary key on [ID] in table 'New_FastNews'
ALTER TABLE [dbo].[New_FastNews]
ADD CONSTRAINT [PK_New_FastNews]
    PRIMARY KEY CLUSTERED ([ID] ASC);
GO

-- Creating primary key on [ID] in table 'New_FooterMenu'
ALTER TABLE [dbo].[New_FooterMenu]
ADD CONSTRAINT [PK_New_FooterMenu]
    PRIMARY KEY CLUSTERED ([ID] ASC);
GO

-- Creating primary key on [ID] in table 'New_Home_Page'
ALTER TABLE [dbo].[New_Home_Page]
ADD CONSTRAINT [PK_New_Home_Page]
    PRIMARY KEY CLUSTERED ([ID] ASC);
GO

-- Creating primary key on [ID] in table 'New_lang'
ALTER TABLE [dbo].[New_lang]
ADD CONSTRAINT [PK_New_lang]
    PRIMARY KEY CLUSTERED ([ID] ASC);
GO

-- Creating primary key on [ID] in table 'New_LR'
ALTER TABLE [dbo].[New_LR]
ADD CONSTRAINT [PK_New_LR]
    PRIMARY KEY CLUSTERED ([ID] ASC);
GO

-- Creating primary key on [ID] in table 'New_Menu'
ALTER TABLE [dbo].[New_Menu]
ADD CONSTRAINT [PK_New_Menu]
    PRIMARY KEY CLUSTERED ([ID] ASC);
GO

-- Creating primary key on [ID] in table 'New_news'
ALTER TABLE [dbo].[New_news]
ADD CONSTRAINT [PK_New_news]
    PRIMARY KEY CLUSTERED ([ID] ASC);
GO

-- Creating primary key on [ID] in table 'New_page_department'
ALTER TABLE [dbo].[New_page_department]
ADD CONSTRAINT [PK_New_page_department]
    PRIMARY KEY CLUSTERED ([ID] ASC);
GO

-- Creating primary key on [ID] in table 'New_page_Department_tab'
ALTER TABLE [dbo].[New_page_Department_tab]
ADD CONSTRAINT [PK_New_page_Department_tab]
    PRIMARY KEY CLUSTERED ([ID] ASC);
GO

-- Creating primary key on [ID] in table 'New_Page_Tabe'
ALTER TABLE [dbo].[New_Page_Tabe]
ADD CONSTRAINT [PK_New_Page_Tabe]
    PRIMARY KEY CLUSTERED ([ID] ASC);
GO

-- Creating primary key on [ID] in table 'New_Page_tabe_Content'
ALTER TABLE [dbo].[New_Page_tabe_Content]
ADD CONSTRAINT [PK_New_Page_tabe_Content]
    PRIMARY KEY CLUSTERED ([ID] ASC);
GO

-- Creating primary key on [ID] in table 'New_Page_V_A_Horizantal_Tabe'
ALTER TABLE [dbo].[New_Page_V_A_Horizantal_Tabe]
ADD CONSTRAINT [PK_New_Page_V_A_Horizantal_Tabe]
    PRIMARY KEY CLUSTERED ([ID] ASC);
GO

-- Creating primary key on [ID] in table 'New_Page_Vertical_and_H_Tabe'
ALTER TABLE [dbo].[New_Page_Vertical_and_H_Tabe]
ADD CONSTRAINT [PK_New_Page_Vertical_and_H_Tabe]
    PRIMARY KEY CLUSTERED ([ID] ASC);
GO

-- Creating primary key on [ID] in table 'New_pages'
ALTER TABLE [dbo].[New_pages]
ADD CONSTRAINT [PK_New_pages]
    PRIMARY KEY CLUSTERED ([ID] ASC);
GO

-- Creating primary key on [ID] in table 'New_pages_facality'
ALTER TABLE [dbo].[New_pages_facality]
ADD CONSTRAINT [PK_New_pages_facality]
    PRIMARY KEY CLUSTERED ([ID] ASC);
GO

-- Creating primary key on [ID] in table 'New_pages_Text'
ALTER TABLE [dbo].[New_pages_Text]
ADD CONSTRAINT [PK_New_pages_Text]
    PRIMARY KEY CLUSTERED ([ID] ASC);
GO

-- Creating primary key on [ID] in table 'New_pracktes'
ALTER TABLE [dbo].[New_pracktes]
ADD CONSTRAINT [PK_New_pracktes]
    PRIMARY KEY CLUSTERED ([ID] ASC);
GO

-- Creating primary key on [ID] in table 'New_Slider'
ALTER TABLE [dbo].[New_Slider]
ADD CONSTRAINT [PK_New_Slider]
    PRIMARY KEY CLUSTERED ([ID] ASC);
GO

-- Creating primary key on [ID] in table 'New_SocialMedia'
ALTER TABLE [dbo].[New_SocialMedia]
ADD CONSTRAINT [PK_New_SocialMedia]
    PRIMARY KEY CLUSTERED ([ID] ASC);
GO

-- Creating primary key on [ID] in table 'NewGridPage_Content'
ALTER TABLE [dbo].[NewGridPage_Content]
ADD CONSTRAINT [PK_NewGridPage_Content]
    PRIMARY KEY CLUSTERED ([ID] ASC);
GO

-- Creating primary key on [ID] in table 'NewGridPages'
ALTER TABLE [dbo].[NewGridPages]
ADD CONSTRAINT [PK_NewGridPages]
    PRIMARY KEY CLUSTERED ([ID] ASC);
GO

-- Creating primary key on [ID] in table 'Q_FillVoid'
ALTER TABLE [dbo].[Q_FillVoid]
ADD CONSTRAINT [PK_Q_FillVoid]
    PRIMARY KEY CLUSTERED ([ID] ASC);
GO

-- Creating primary key on [ID] in table 'Q_Matching'
ALTER TABLE [dbo].[Q_Matching]
ADD CONSTRAINT [PK_Q_Matching]
    PRIMARY KEY CLUSTERED ([ID] ASC);
GO

-- Creating primary key on [ID] in table 'Q_MultiChoice'
ALTER TABLE [dbo].[Q_MultiChoice]
ADD CONSTRAINT [PK_Q_MultiChoice]
    PRIMARY KEY CLUSTERED ([ID] ASC);
GO

-- Creating primary key on [ID] in table 'Q_TrueOrFolse'
ALTER TABLE [dbo].[Q_TrueOrFolse]
ADD CONSTRAINT [PK_Q_TrueOrFolse]
    PRIMARY KEY CLUSTERED ([ID] ASC);
GO

-- Creating primary key on [ID] in table 'Questions'
ALTER TABLE [dbo].[Questions]
ADD CONSTRAINT [PK_Questions]
    PRIMARY KEY CLUSTERED ([ID] ASC);
GO

-- Creating primary key on [int] in table 'Std_ExamFinish'
ALTER TABLE [dbo].[Std_ExamFinish]
ADD CONSTRAINT [PK_Std_ExamFinish]
    PRIMARY KEY CLUSTERED ([int] ASC);
GO

-- Creating primary key on [ID] in table 'std_lecture'
ALTER TABLE [dbo].[std_lecture]
ADD CONSTRAINT [PK_std_lecture]
    PRIMARY KEY CLUSTERED ([ID] ASC);
GO

-- Creating primary key on [ID] in table 'Std_Reg_Category'
ALTER TABLE [dbo].[Std_Reg_Category]
ADD CONSTRAINT [PK_Std_Reg_Category]
    PRIMARY KEY CLUSTERED ([ID] ASC);
GO

-- Creating primary key on [ID] in table 'StoreCategories'
ALTER TABLE [dbo].[StoreCategories]
ADD CONSTRAINT [PK_StoreCategories]
    PRIMARY KEY CLUSTERED ([ID] ASC);
GO

-- Creating primary key on [ID] in table 'StoreProducts'
ALTER TABLE [dbo].[StoreProducts]
ADD CONSTRAINT [PK_StoreProducts]
    PRIMARY KEY CLUSTERED ([ID] ASC);
GO

-- Creating primary key on [ID] in table 'StyleTables'
ALTER TABLE [dbo].[StyleTables]
ADD CONSTRAINT [PK_StyleTables]
    PRIMARY KEY CLUSTERED ([ID] ASC);
GO

-- Creating primary key on [diagram_id] in table 'sysdiagrams'
ALTER TABLE [dbo].[sysdiagrams]
ADD CONSTRAINT [PK_sysdiagrams]
    PRIMARY KEY CLUSTERED ([diagram_id] ASC);
GO

-- Creating primary key on [ID] in table 'UserLocations'
ALTER TABLE [dbo].[UserLocations]
ADD CONSTRAINT [PK_UserLocations]
    PRIMARY KEY CLUSTERED ([ID] ASC);
GO

-- Creating primary key on [RoleId], [UserId] in table 'AspNetUserRoles'
ALTER TABLE [dbo].[AspNetUserRoles]
ADD CONSTRAINT [PK_AspNetUserRoles]
    PRIMARY KEY CLUSTERED ([RoleId], [UserId] ASC);
GO

-- --------------------------------------------------
-- Creating all FOREIGN KEY constraints
-- --------------------------------------------------

-- Creating foreign key on [UserId] in table 'AspNetUserClaims'
ALTER TABLE [dbo].[AspNetUserClaims]
ADD CONSTRAINT [FK_dbo_AspNetUserClaims_dbo_AspNetUsers_UserId]
    FOREIGN KEY ([UserId])
    REFERENCES [dbo].[AspNetUsers]
        ([Id])
    ON DELETE CASCADE ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_dbo_AspNetUserClaims_dbo_AspNetUsers_UserId'
CREATE INDEX [IX_FK_dbo_AspNetUserClaims_dbo_AspNetUsers_UserId]
ON [dbo].[AspNetUserClaims]
    ([UserId]);
GO

-- Creating foreign key on [UserId] in table 'AspNetUserLogins'
ALTER TABLE [dbo].[AspNetUserLogins]
ADD CONSTRAINT [FK_dbo_AspNetUserLogins_dbo_AspNetUsers_UserId]
    FOREIGN KEY ([UserId])
    REFERENCES [dbo].[AspNetUsers]
        ([Id])
    ON DELETE CASCADE ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_dbo_AspNetUserLogins_dbo_AspNetUsers_UserId'
CREATE INDEX [IX_FK_dbo_AspNetUserLogins_dbo_AspNetUsers_UserId]
ON [dbo].[AspNetUserLogins]
    ([UserId]);
GO

-- Creating foreign key on [Assignment_ID] in table 'Assignment_Q'
ALTER TABLE [dbo].[Assignment_Q]
ADD CONSTRAINT [FK_Assignment_Q_Assignment]
    FOREIGN KEY ([Assignment_ID])
    REFERENCES [dbo].[Assignments]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_Assignment_Q_Assignment'
CREATE INDEX [IX_FK_Assignment_Q_Assignment]
ON [dbo].[Assignment_Q]
    ([Assignment_ID]);
GO

-- Creating foreign key on [Q_ID] in table 'Assignment_Q'
ALTER TABLE [dbo].[Assignment_Q]
ADD CONSTRAINT [FK_Assignment_Q_Questions]
    FOREIGN KEY ([Q_ID])
    REFERENCES [dbo].[Questions]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_Assignment_Q_Questions'
CREATE INDEX [IX_FK_Assignment_Q_Questions]
ON [dbo].[Assignment_Q]
    ([Q_ID]);
GO

-- Creating foreign key on [Lecture_ID] in table 'Assignments'
ALTER TABLE [dbo].[Assignments]
ADD CONSTRAINT [FK_Assignment_Lecture]
    FOREIGN KEY ([Lecture_ID])
    REFERENCES [dbo].[Lectures]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_Assignment_Lecture'
CREATE INDEX [IX_FK_Assignment_Lecture]
ON [dbo].[Assignments]
    ([Lecture_ID]);
GO

-- Creating foreign key on [ParentCateogry_ID] in table 'Categories'
ALTER TABLE [dbo].[Categories]
ADD CONSTRAINT [FK_Category_Category]
    FOREIGN KEY ([ParentCateogry_ID])
    REFERENCES [dbo].[Categories]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_Category_Category'
CREATE INDEX [IX_FK_Category_Category]
ON [dbo].[Categories]
    ([ParentCateogry_ID]);
GO

-- Creating foreign key on [CategoryType_ID] in table 'Categories'
ALTER TABLE [dbo].[Categories]
ADD CONSTRAINT [FK_Category_CategoryType]
    FOREIGN KEY ([CategoryType_ID])
    REFERENCES [dbo].[CategoryTypes]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_Category_CategoryType'
CREATE INDEX [IX_FK_Category_CategoryType]
ON [dbo].[Categories]
    ([CategoryType_ID]);
GO

-- Creating foreign key on [Category_ID] in table 'Exams'
ALTER TABLE [dbo].[Exams]
ADD CONSTRAINT [FK_Exams_Category]
    FOREIGN KEY ([Category_ID])
    REFERENCES [dbo].[Categories]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_Exams_Category'
CREATE INDEX [IX_FK_Exams_Category]
ON [dbo].[Exams]
    ([Category_ID]);
GO

-- Creating foreign key on [Category_ID] in table 'Lectures'
ALTER TABLE [dbo].[Lectures]
ADD CONSTRAINT [FK_Lecture_Category]
    FOREIGN KEY ([Category_ID])
    REFERENCES [dbo].[Categories]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_Lecture_Category'
CREATE INDEX [IX_FK_Lecture_Category]
ON [dbo].[Lectures]
    ([Category_ID]);
GO

-- Creating foreign key on [Category_ID] in table 'Questions'
ALTER TABLE [dbo].[Questions]
ADD CONSTRAINT [FK_Questions_Category]
    FOREIGN KEY ([Category_ID])
    REFERENCES [dbo].[Categories]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_Questions_Category'
CREATE INDEX [IX_FK_Questions_Category]
ON [dbo].[Questions]
    ([Category_ID]);
GO

-- Creating foreign key on [Category_ID] in table 'Std_Reg_Category'
ALTER TABLE [dbo].[Std_Reg_Category]
ADD CONSTRAINT [FK_Std_Reg_Category_Category]
    FOREIGN KEY ([Category_ID])
    REFERENCES [dbo].[Categories]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_Std_Reg_Category_Category'
CREATE INDEX [IX_FK_Std_Reg_Category_Category]
ON [dbo].[Std_Reg_Category]
    ([Category_ID]);
GO

-- Creating foreign key on [ParentType] in table 'CategoryTypes'
ALTER TABLE [dbo].[CategoryTypes]
ADD CONSTRAINT [FK_CategoryType_CategoryType]
    FOREIGN KEY ([ParentType])
    REFERENCES [dbo].[CategoryTypes]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_CategoryType_CategoryType'
CREATE INDEX [IX_FK_CategoryType_CategoryType]
ON [dbo].[CategoryTypes]
    ([ParentType]);
GO

-- Creating foreign key on [Exam_ID] in table 'Exam_Q'
ALTER TABLE [dbo].[Exam_Q]
ADD CONSTRAINT [FK_Exam_Q_Exams]
    FOREIGN KEY ([Exam_ID])
    REFERENCES [dbo].[Exams]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_Exam_Q_Exams'
CREATE INDEX [IX_FK_Exam_Q_Exams]
ON [dbo].[Exam_Q]
    ([Exam_ID]);
GO

-- Creating foreign key on [Q_ID] in table 'Exam_Q'
ALTER TABLE [dbo].[Exam_Q]
ADD CONSTRAINT [FK_Exam_Q_Questions]
    FOREIGN KEY ([Q_ID])
    REFERENCES [dbo].[Questions]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_Exam_Q_Questions'
CREATE INDEX [IX_FK_Exam_Q_Questions]
ON [dbo].[Exam_Q]
    ([Q_ID]);
GO

-- Creating foreign key on [Exam_ID] in table 'Std_ExamFinish'
ALTER TABLE [dbo].[Std_ExamFinish]
ADD CONSTRAINT [FK_Std_ExamFinish_Exams]
    FOREIGN KEY ([Exam_ID])
    REFERENCES [dbo].[Exams]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_Std_ExamFinish_Exams'
CREATE INDEX [IX_FK_Std_ExamFinish_Exams]
ON [dbo].[Std_ExamFinish]
    ([Exam_ID]);
GO

-- Creating foreign key on [Lecture_ID] in table 'std_lecture'
ALTER TABLE [dbo].[std_lecture]
ADD CONSTRAINT [FK_std_lecture_Lecture]
    FOREIGN KEY ([Lecture_ID])
    REFERENCES [dbo].[Lectures]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_std_lecture_Lecture'
CREATE INDEX [IX_FK_std_lecture_Lecture]
ON [dbo].[std_lecture]
    ([Lecture_ID]);
GO

-- Creating foreign key on [New_About_ID] in table 'New_About_Content'
ALTER TABLE [dbo].[New_About_Content]
ADD CONSTRAINT [FK_New_About_Content_New_About]
    FOREIGN KEY ([New_About_ID])
    REFERENCES [dbo].[New_About]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_New_About_Content_New_About'
CREATE INDEX [IX_FK_New_About_Content_New_About]
ON [dbo].[New_About_Content]
    ([New_About_ID]);
GO

-- Creating foreign key on [Lang_ID] in table 'New_About'
ALTER TABLE [dbo].[New_About]
ADD CONSTRAINT [FK_New_About_New_lang]
    FOREIGN KEY ([Lang_ID])
    REFERENCES [dbo].[New_lang]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_New_About_New_lang'
CREATE INDEX [IX_FK_New_About_New_lang]
ON [dbo].[New_About]
    ([Lang_ID]);
GO

-- Creating foreign key on [new_pages_ID] in table 'New_About'
ALTER TABLE [dbo].[New_About]
ADD CONSTRAINT [FK_New_About_New_pages]
    FOREIGN KEY ([new_pages_ID])
    REFERENCES [dbo].[New_pages]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_New_About_New_pages'
CREATE INDEX [IX_FK_New_About_New_pages]
ON [dbo].[New_About]
    ([new_pages_ID]);
GO

-- Creating foreign key on [Lang_ID] in table 'New_ContactUs'
ALTER TABLE [dbo].[New_ContactUs]
ADD CONSTRAINT [FK_New_ContactUs_New_lang]
    FOREIGN KEY ([Lang_ID])
    REFERENCES [dbo].[New_lang]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_New_ContactUs_New_lang'
CREATE INDEX [IX_FK_New_ContactUs_New_lang]
ON [dbo].[New_ContactUs]
    ([Lang_ID]);
GO

-- Creating foreign key on [New_Page_ID] in table 'New_ContactUs'
ALTER TABLE [dbo].[New_ContactUs]
ADD CONSTRAINT [FK_New_ContactUs_New_pages]
    FOREIGN KEY ([New_Page_ID])
    REFERENCES [dbo].[New_pages]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_New_ContactUs_New_pages'
CREATE INDEX [IX_FK_New_ContactUs_New_pages]
ON [dbo].[New_ContactUs]
    ([New_Page_ID]);
GO

-- Creating foreign key on [Lang_ID] in table 'New_FastNews'
ALTER TABLE [dbo].[New_FastNews]
ADD CONSTRAINT [FK_New_FastNews_New_lang]
    FOREIGN KEY ([Lang_ID])
    REFERENCES [dbo].[New_lang]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_New_FastNews_New_lang'
CREATE INDEX [IX_FK_New_FastNews_New_lang]
ON [dbo].[New_FastNews]
    ([Lang_ID]);
GO

-- Creating foreign key on [New_page_ID] in table 'New_FooterMenu'
ALTER TABLE [dbo].[New_FooterMenu]
ADD CONSTRAINT [FK_New_FooterMenu_New_pages]
    FOREIGN KEY ([New_page_ID])
    REFERENCES [dbo].[New_pages]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_New_FooterMenu_New_pages'
CREATE INDEX [IX_FK_New_FooterMenu_New_pages]
ON [dbo].[New_FooterMenu]
    ([New_page_ID]);
GO

-- Creating foreign key on [Lang_ID] in table 'NewGridPages'
ALTER TABLE [dbo].[NewGridPages]
ADD CONSTRAINT [FK_GridPage_New_lang]
    FOREIGN KEY ([Lang_ID])
    REFERENCES [dbo].[New_lang]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_GridPage_New_lang'
CREATE INDEX [IX_FK_GridPage_New_lang]
ON [dbo].[NewGridPages]
    ([Lang_ID]);
GO

-- Creating foreign key on [Lang_ID] in table 'New_LR'
ALTER TABLE [dbo].[New_LR]
ADD CONSTRAINT [FK_New_LR_New_lang]
    FOREIGN KEY ([Lang_ID])
    REFERENCES [dbo].[New_lang]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_New_LR_New_lang'
CREATE INDEX [IX_FK_New_LR_New_lang]
ON [dbo].[New_LR]
    ([Lang_ID]);
GO

-- Creating foreign key on [Lang_ID] in table 'New_Menu'
ALTER TABLE [dbo].[New_Menu]
ADD CONSTRAINT [FK_New_Menu_New_lang]
    FOREIGN KEY ([Lang_ID])
    REFERENCES [dbo].[New_lang]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_New_Menu_New_lang'
CREATE INDEX [IX_FK_New_Menu_New_lang]
ON [dbo].[New_Menu]
    ([Lang_ID]);
GO

-- Creating foreign key on [Lang_ID] in table 'New_news'
ALTER TABLE [dbo].[New_news]
ADD CONSTRAINT [FK_New_news_New_lang]
    FOREIGN KEY ([Lang_ID])
    REFERENCES [dbo].[New_lang]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_New_news_New_lang'
CREATE INDEX [IX_FK_New_news_New_lang]
ON [dbo].[New_news]
    ([Lang_ID]);
GO

-- Creating foreign key on [Lang_ID] in table 'New_page_department'
ALTER TABLE [dbo].[New_page_department]
ADD CONSTRAINT [FK_New_page_department_New_lang]
    FOREIGN KEY ([Lang_ID])
    REFERENCES [dbo].[New_lang]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_New_page_department_New_lang'
CREATE INDEX [IX_FK_New_page_department_New_lang]
ON [dbo].[New_page_department]
    ([Lang_ID]);
GO

-- Creating foreign key on [Lang_ID] in table 'New_page_Department_tab'
ALTER TABLE [dbo].[New_page_Department_tab]
ADD CONSTRAINT [FK_New_page_Department_tab_New_lang]
    FOREIGN KEY ([Lang_ID])
    REFERENCES [dbo].[New_lang]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_New_page_Department_tab_New_lang'
CREATE INDEX [IX_FK_New_page_Department_tab_New_lang]
ON [dbo].[New_page_Department_tab]
    ([Lang_ID]);
GO

-- Creating foreign key on [Lang_ID] in table 'New_Page_Tabe'
ALTER TABLE [dbo].[New_Page_Tabe]
ADD CONSTRAINT [FK_New_Page_Tabe_New_lang]
    FOREIGN KEY ([Lang_ID])
    REFERENCES [dbo].[New_lang]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_New_Page_Tabe_New_lang'
CREATE INDEX [IX_FK_New_Page_Tabe_New_lang]
ON [dbo].[New_Page_Tabe]
    ([Lang_ID]);
GO

-- Creating foreign key on [Lang_ID] in table 'New_Page_V_A_Horizantal_Tabe'
ALTER TABLE [dbo].[New_Page_V_A_Horizantal_Tabe]
ADD CONSTRAINT [FK_New_Page_V_A_Horizantal_Tabe_New_lang]
    FOREIGN KEY ([Lang_ID])
    REFERENCES [dbo].[New_lang]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_New_Page_V_A_Horizantal_Tabe_New_lang'
CREATE INDEX [IX_FK_New_Page_V_A_Horizantal_Tabe_New_lang]
ON [dbo].[New_Page_V_A_Horizantal_Tabe]
    ([Lang_ID]);
GO

-- Creating foreign key on [Lang_ID] in table 'New_Page_Vertical_and_H_Tabe'
ALTER TABLE [dbo].[New_Page_Vertical_and_H_Tabe]
ADD CONSTRAINT [FK_New_Page_Vertical_and_H_Tabe_New_lang]
    FOREIGN KEY ([Lang_ID])
    REFERENCES [dbo].[New_lang]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_New_Page_Vertical_and_H_Tabe_New_lang'
CREATE INDEX [IX_FK_New_Page_Vertical_and_H_Tabe_New_lang]
ON [dbo].[New_Page_Vertical_and_H_Tabe]
    ([Lang_ID]);
GO

-- Creating foreign key on [Lang_ID] in table 'New_pages_facality'
ALTER TABLE [dbo].[New_pages_facality]
ADD CONSTRAINT [FK_New_pages_facality_New_lang]
    FOREIGN KEY ([Lang_ID])
    REFERENCES [dbo].[New_lang]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_New_pages_facality_New_lang'
CREATE INDEX [IX_FK_New_pages_facality_New_lang]
ON [dbo].[New_pages_facality]
    ([Lang_ID]);
GO

-- Creating foreign key on [Lang_ID] in table 'New_pages_Text'
ALTER TABLE [dbo].[New_pages_Text]
ADD CONSTRAINT [FK_New_pages_Text_New_lang]
    FOREIGN KEY ([Lang_ID])
    REFERENCES [dbo].[New_lang]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_New_pages_Text_New_lang'
CREATE INDEX [IX_FK_New_pages_Text_New_lang]
ON [dbo].[New_pages_Text]
    ([Lang_ID]);
GO

-- Creating foreign key on [Lang_ID] in table 'New_pracktes'
ALTER TABLE [dbo].[New_pracktes]
ADD CONSTRAINT [FK_New_pracktes_New_lang]
    FOREIGN KEY ([Lang_ID])
    REFERENCES [dbo].[New_lang]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_New_pracktes_New_lang'
CREATE INDEX [IX_FK_New_pracktes_New_lang]
ON [dbo].[New_pracktes]
    ([Lang_ID]);
GO

-- Creating foreign key on [Lang_ID] in table 'New_Slider'
ALTER TABLE [dbo].[New_Slider]
ADD CONSTRAINT [FK_New_Slider_New_lang]
    FOREIGN KEY ([Lang_ID])
    REFERENCES [dbo].[New_lang]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_New_Slider_New_lang'
CREATE INDEX [IX_FK_New_Slider_New_lang]
ON [dbo].[New_Slider]
    ([Lang_ID]);
GO

-- Creating foreign key on [Lang_ID] in table 'New_SocialMedia'
ALTER TABLE [dbo].[New_SocialMedia]
ADD CONSTRAINT [FK_New_SocialMedia_New_lang]
    FOREIGN KEY ([Lang_ID])
    REFERENCES [dbo].[New_lang]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_New_SocialMedia_New_lang'
CREATE INDEX [IX_FK_New_SocialMedia_New_lang]
ON [dbo].[New_SocialMedia]
    ([Lang_ID]);
GO

-- Creating foreign key on [New_Page_ID] in table 'New_Menu'
ALTER TABLE [dbo].[New_Menu]
ADD CONSTRAINT [FK_New_Menu_New_pages]
    FOREIGN KEY ([New_Page_ID])
    REFERENCES [dbo].[New_pages]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_New_Menu_New_pages'
CREATE INDEX [IX_FK_New_Menu_New_pages]
ON [dbo].[New_Menu]
    ([New_Page_ID]);
GO

-- Creating foreign key on [Facility_ID] in table 'New_page_department'
ALTER TABLE [dbo].[New_page_department]
ADD CONSTRAINT [FK_New_page_department_New_Page_V_A_Horizantal_Tabe]
    FOREIGN KEY ([Facility_ID])
    REFERENCES [dbo].[New_Page_V_A_Horizantal_Tabe]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_New_page_department_New_Page_V_A_Horizantal_Tabe'
CREATE INDEX [IX_FK_New_page_department_New_Page_V_A_Horizantal_Tabe]
ON [dbo].[New_page_department]
    ([Facility_ID]);
GO

-- Creating foreign key on [Department_ID] in table 'New_page_Department_tab'
ALTER TABLE [dbo].[New_page_Department_tab]
ADD CONSTRAINT [FK_New_page_Department_tab_New_page_department]
    FOREIGN KEY ([Department_ID])
    REFERENCES [dbo].[New_page_department]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_New_page_Department_tab_New_page_department'
CREATE INDEX [IX_FK_New_page_Department_tab_New_page_department]
ON [dbo].[New_page_Department_tab]
    ([Department_ID]);
GO

-- Creating foreign key on [Page_Tabe_ID] in table 'New_Page_tabe_Content'
ALTER TABLE [dbo].[New_Page_tabe_Content]
ADD CONSTRAINT [FK_New_Page_tabe_Content_New_Page_Tabe]
    FOREIGN KEY ([Page_Tabe_ID])
    REFERENCES [dbo].[New_Page_Tabe]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_New_Page_tabe_Content_New_Page_Tabe'
CREATE INDEX [IX_FK_New_Page_tabe_Content_New_Page_Tabe]
ON [dbo].[New_Page_tabe_Content]
    ([Page_Tabe_ID]);
GO

-- Creating foreign key on [New_Page_ID] in table 'New_Page_Tabe'
ALTER TABLE [dbo].[New_Page_Tabe]
ADD CONSTRAINT [FK_New_Page_Tabe_New_pages]
    FOREIGN KEY ([New_Page_ID])
    REFERENCES [dbo].[New_pages]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_New_Page_Tabe_New_pages'
CREATE INDEX [IX_FK_New_Page_Tabe_New_pages]
ON [dbo].[New_Page_Tabe]
    ([New_Page_ID]);
GO

-- Creating foreign key on [New_page_V_A_H_ID] in table 'New_Page_V_A_Horizantal_Tabe'
ALTER TABLE [dbo].[New_Page_V_A_Horizantal_Tabe]
ADD CONSTRAINT [FK_New_Page_V_A_Horizantal_Tabe_New_pages_facality]
    FOREIGN KEY ([New_page_V_A_H_ID])
    REFERENCES [dbo].[New_pages_facality]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_New_Page_V_A_Horizantal_Tabe_New_pages_facality'
CREATE INDEX [IX_FK_New_Page_V_A_Horizantal_Tabe_New_pages_facality]
ON [dbo].[New_Page_V_A_Horizantal_Tabe]
    ([New_page_V_A_H_ID]);
GO

-- Creating foreign key on [New_PageHorizantal_Tabe_ID] in table 'New_Page_Vertical_and_H_Tabe'
ALTER TABLE [dbo].[New_Page_Vertical_and_H_Tabe]
ADD CONSTRAINT [FK_New_Page_Vertical_and_H_Tabe_New_Page_V_A_Horizantal_Tabe]
    FOREIGN KEY ([New_PageHorizantal_Tabe_ID])
    REFERENCES [dbo].[New_Page_V_A_Horizantal_Tabe]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_New_Page_Vertical_and_H_Tabe_New_Page_V_A_Horizantal_Tabe'
CREATE INDEX [IX_FK_New_Page_Vertical_and_H_Tabe_New_Page_V_A_Horizantal_Tabe]
ON [dbo].[New_Page_Vertical_and_H_Tabe]
    ([New_PageHorizantal_Tabe_ID]);
GO

-- Creating foreign key on [Pages_ID] in table 'NewGridPages'
ALTER TABLE [dbo].[NewGridPages]
ADD CONSTRAINT [FK_GridPage_New_pages]
    FOREIGN KEY ([Pages_ID])
    REFERENCES [dbo].[New_pages]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_GridPage_New_pages'
CREATE INDEX [IX_FK_GridPage_New_pages]
ON [dbo].[NewGridPages]
    ([Pages_ID]);
GO

-- Creating foreign key on [New_pages_ID] in table 'New_pages_Text'
ALTER TABLE [dbo].[New_pages_Text]
ADD CONSTRAINT [FK_New_pages_Text_New_pages]
    FOREIGN KEY ([New_pages_ID])
    REFERENCES [dbo].[New_pages]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_New_pages_Text_New_pages'
CREATE INDEX [IX_FK_New_pages_Text_New_pages]
ON [dbo].[New_pages_Text]
    ([New_pages_ID]);
GO

-- Creating foreign key on [GridPageID] in table 'NewGridPage_Content'
ALTER TABLE [dbo].[NewGridPage_Content]
ADD CONSTRAINT [FK_GridPage_Content_GridPage]
    FOREIGN KEY ([GridPageID])
    REFERENCES [dbo].[NewGridPages]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_GridPage_Content_GridPage'
CREATE INDEX [IX_FK_GridPage_Content_GridPage]
ON [dbo].[NewGridPage_Content]
    ([GridPageID]);
GO

-- Creating foreign key on [Question_ID] in table 'Q_FillVoid'
ALTER TABLE [dbo].[Q_FillVoid]
ADD CONSTRAINT [FK_Q_FillVoid_Questions]
    FOREIGN KEY ([Question_ID])
    REFERENCES [dbo].[Questions]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_Q_FillVoid_Questions'
CREATE INDEX [IX_FK_Q_FillVoid_Questions]
ON [dbo].[Q_FillVoid]
    ([Question_ID]);
GO

-- Creating foreign key on [Question_ID] in table 'Q_Matching'
ALTER TABLE [dbo].[Q_Matching]
ADD CONSTRAINT [FK_Q_Matching_Questions]
    FOREIGN KEY ([Question_ID])
    REFERENCES [dbo].[Questions]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_Q_Matching_Questions'
CREATE INDEX [IX_FK_Q_Matching_Questions]
ON [dbo].[Q_Matching]
    ([Question_ID]);
GO

-- Creating foreign key on [Question_ID] in table 'Q_MultiChoice'
ALTER TABLE [dbo].[Q_MultiChoice]
ADD CONSTRAINT [FK_Q_MultiChoice_Questions]
    FOREIGN KEY ([Question_ID])
    REFERENCES [dbo].[Questions]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_Q_MultiChoice_Questions'
CREATE INDEX [IX_FK_Q_MultiChoice_Questions]
ON [dbo].[Q_MultiChoice]
    ([Question_ID]);
GO

-- Creating foreign key on [Question_ID] in table 'Q_TrueOrFolse'
ALTER TABLE [dbo].[Q_TrueOrFolse]
ADD CONSTRAINT [FK_Q_TrueOrFolse_Questions]
    FOREIGN KEY ([Question_ID])
    REFERENCES [dbo].[Questions]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_Q_TrueOrFolse_Questions'
CREATE INDEX [IX_FK_Q_TrueOrFolse_Questions]
ON [dbo].[Q_TrueOrFolse]
    ([Question_ID]);
GO

-- Creating foreign key on [Category_ID] in table 'StoreProducts'
ALTER TABLE [dbo].[StoreProducts]
ADD CONSTRAINT [FK_StoreProduct_StoreCategory]
    FOREIGN KEY ([Category_ID])
    REFERENCES [dbo].[StoreCategories]
        ([ID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_StoreProduct_StoreCategory'
CREATE INDEX [IX_FK_StoreProduct_StoreCategory]
ON [dbo].[StoreProducts]
    ([Category_ID]);
GO

-- Creating foreign key on [RoleId] in table 'AspNetUserRoles'
ALTER TABLE [dbo].[AspNetUserRoles]
ADD CONSTRAINT [FK_AspNetUserRoles_AspNetRoles]
    FOREIGN KEY ([RoleId])
    REFERENCES [dbo].[AspNetRoles]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating foreign key on [UserId] in table 'AspNetUserRoles'
ALTER TABLE [dbo].[AspNetUserRoles]
ADD CONSTRAINT [FK_AspNetUserRoles_AspNetUsers]
    FOREIGN KEY ([UserId])
    REFERENCES [dbo].[AspNetUsers]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_AspNetUserRoles_AspNetUsers'
CREATE INDEX [IX_FK_AspNetUserRoles_AspNetUsers]
ON [dbo].[AspNetUserRoles]
    ([UserId]);
GO

-- --------------------------------------------------
-- Script has ended
-- --------------------------------------------------