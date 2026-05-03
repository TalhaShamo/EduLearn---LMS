-- Script to manually verify existing test users
-- Run this in SQL Server Management Studio or Azure Data Studio

USE EduLearnIdentityDb;
GO

-- Show all users and their verification status
SELECT 
    UserId,
    FullName,
    Email,
    IsVerified,
    CreatedAt
FROM Users
ORDER BY CreatedAt DESC;
GO

-- Verify all existing users (for testing purposes)
UPDATE Users
SET IsVerified = 1
WHERE IsVerified = 0;
GO

-- Check the results
SELECT 
    UserId,
    FullName,
    Email,
    IsVerified,
    CreatedAt
FROM Users
ORDER BY CreatedAt DESC;
GO

PRINT 'All users have been verified!';
