-- Migration: Add metadata fields to Courses table
-- Date: 2026-05-02

USE EduLearnCourseDb;
GO

-- Add new columns to Courses table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Courses]') AND name = 'Subtitle')
BEGIN
    ALTER TABLE [dbo].[Courses] ADD [Subtitle] NVARCHAR(500) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Courses]') AND name = 'InstructorName')
BEGIN
    ALTER TABLE [dbo].[Courses] ADD [InstructorName] NVARCHAR(200) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Courses]') AND name = 'EnrollmentCount')
BEGIN
    ALTER TABLE [dbo].[Courses] ADD [EnrollmentCount] INT NOT NULL DEFAULT 0;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Courses]') AND name = 'AverageRating')
BEGIN
    ALTER TABLE [dbo].[Courses] ADD [AverageRating] DECIMAL(3,2) NOT NULL DEFAULT 0;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Courses]') AND name = 'ReviewCount')
BEGIN
    ALTER TABLE [dbo].[Courses] ADD [ReviewCount] INT NOT NULL DEFAULT 0;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Courses]') AND name = 'DurationMinutes')
BEGIN
    ALTER TABLE [dbo].[Courses] ADD [DurationMinutes] INT NOT NULL DEFAULT 0;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Courses]') AND name = 'Tags')
BEGIN
    ALTER TABLE [dbo].[Courses] ADD [Tags] NVARCHAR(MAX) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Courses]') AND name = 'LearningObjectives')
BEGIN
    ALTER TABLE [dbo].[Courses] ADD [LearningObjectives] NVARCHAR(MAX) NULL;
END
GO

PRINT 'Migration completed: Added metadata fields to Courses table';
GO
