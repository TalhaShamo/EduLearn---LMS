-- Script to insert 10 dummy courses with sections and lessons
-- Run this in SQL Server Management Studio or Azure Data Studio

USE EduLearnCourseDb;
GO

-- Get an instructor ID (use the first instructor in the database)
DECLARE @InstructorId UNIQUEIDENTIFIER;
SELECT TOP 1 @InstructorId = UserId FROM EduLearnIdentityDb.dbo.Users WHERE Role = 'Instructor';

-- If no instructor exists, create a dummy one
IF @InstructorId IS NULL
BEGIN
    SET @InstructorId = NEWID();
    PRINT 'Warning: No instructor found. Using dummy instructor ID: ' + CAST(@InstructorId AS VARCHAR(50));
END
ELSE
BEGIN
    PRINT 'Using instructor ID: ' + CAST(@InstructorId AS VARCHAR(50));
END

-- ============================================================================
-- COURSE 1: Complete Python Programming Bootcamp
-- ============================================================================
DECLARE @Course1Id UNIQUEIDENTIFIER = NEWID();
INSERT INTO Courses (CourseId, InstructorId, Title, Subtitle, Slug, Description, Category, Level, Price, Language, Status, ThumbnailUrl, InstructorName, EnrollmentCount, AverageRating, ReviewCount, DurationMinutes, Tags, LearningObjectives, CreatedAt, UpdatedAt)
VALUES (
    @Course1Id,
    @InstructorId,
    'Complete Python Programming Bootcamp',
    'Master Python from basics to advanced concepts',
    'complete-python-programming-bootcamp',
    'Learn Python programming from scratch. This comprehensive course covers everything from basic syntax to advanced topics like OOP, file handling, and web scraping.',
    'Programming',
    1, -- Beginner
    1999.00,
    'English',
    2, -- Published
    NULL,
    'Dr. Sarah Johnson',
    1247,
    4.7,
    523,
    420, -- 7 hours
    '["python","programming","beginner","coding"]',
    '["Write Python programs from scratch","Understand object-oriented programming","Work with files and databases","Build real-world projects"]',
    GETDATE(),
    GETDATE()
);

-- Sections for Course 1
DECLARE @C1S1 UNIQUEIDENTIFIER = NEWID();
DECLARE @C1S2 UNIQUEIDENTIFIER = NEWID();

INSERT INTO Sections (SectionId, CourseId, Title, SortOrder) VALUES
(@C1S1, @Course1Id, 'Python Fundamentals', 1),
(@C1S2, @Course1Id, 'Object-Oriented Programming', 2);

-- Lessons for Course 1
INSERT INTO Lessons (LessonId, SectionId, Title, Type, VideoPath, DurationSeconds, IsFreePreview, SortOrder, IsPublished) VALUES
(NEWID(), @C1S1, 'Introduction to Python', 0, NULL, 0, 1, 1, 1),
(NEWID(), @C1S1, 'Variables and Data Types', 0, NULL, 0, 0, 2, 1),
(NEWID(), @C1S2, 'Classes and Objects', 0, NULL, 0, 0, 1, 1),
(NEWID(), @C1S2, 'Inheritance and Polymorphism', 0, NULL, 0, 0, 2, 1);

-- ============================================================================
-- COURSE 2: Web Development with React
-- ============================================================================
DECLARE @Course2Id UNIQUEIDENTIFIER = NEWID();
INSERT INTO Courses (CourseId, InstructorId, Title, Subtitle, Slug, Description, Category, Level, Price, Language, Status, ThumbnailUrl, InstructorName, EnrollmentCount, AverageRating, ReviewCount, DurationMinutes, Tags, LearningObjectives, CreatedAt, UpdatedAt)
VALUES (
    @Course2Id,
    @InstructorId,
    'Web Development with React',
    'Build modern web applications with React and Redux',
    'web-development-with-react',
    'Master React.js and build dynamic, responsive web applications. Learn hooks, state management, routing, and best practices for modern web development.',
    'Web Development',
    2, -- Intermediate
    2499.00,
    'English',
    2, -- Published
    NULL,
    'Michael Chen',
    2156,
    4.8,
    892,
    540, -- 9 hours
    '["react","javascript","web development","frontend"]',
    '["Build React applications from scratch","Master React Hooks and Context API","Implement state management with Redux","Create responsive user interfaces"]',
    GETDATE(),
    GETDATE()
);

-- Sections for Course 2
DECLARE @C2S1 UNIQUEIDENTIFIER = NEWID();
DECLARE @C2S2 UNIQUEIDENTIFIER = NEWID();
DECLARE @C2S3 UNIQUEIDENTIFIER = NEWID();

INSERT INTO Sections (SectionId, CourseId, Title, SortOrder) VALUES
(@C2S1, @Course2Id, 'React Basics', 1),
(@C2S2, @Course2Id, 'Advanced React Patterns', 2),
(@C2S3, @Course2Id, 'State Management', 3);

-- Lessons for Course 2
INSERT INTO Lessons (LessonId, SectionId, Title, Type, VideoPath, DurationSeconds, IsFreePreview, SortOrder, IsPublished) VALUES
(NEWID(), @C2S1, 'Getting Started with React', 0, NULL, 0, 1, 1, 1),
(NEWID(), @C2S1, 'Components and Props', 0, NULL, 0, 0, 2, 1),
(NEWID(), @C2S2, 'React Hooks Deep Dive', 0, NULL, 0, 0, 1, 1),
(NEWID(), @C2S2, 'Custom Hooks', 0, NULL, 0, 0, 2, 1),
(NEWID(), @C2S3, 'Introduction to Redux', 0, NULL, 0, 0, 1, 1),
(NEWID(), @C2S3, 'Redux Toolkit', 0, NULL, 0, 0, 2, 1);

-- ============================================================================
-- COURSE 3: Digital Marketing Masterclass
-- ============================================================================
DECLARE @Course3Id UNIQUEIDENTIFIER = NEWID();
INSERT INTO Courses (CourseId, InstructorId, Title, Subtitle, Slug, Description, Category, Level, Price, Language, Status, ThumbnailUrl, InstructorName, EnrollmentCount, AverageRating, ReviewCount, DurationMinutes, Tags, LearningObjectives, CreatedAt, UpdatedAt)
VALUES (
    @Course3Id,
    @InstructorId,
    'Digital Marketing Masterclass',
    'Complete guide to SEO, social media, and content marketing',
    'digital-marketing-masterclass',
    'Learn everything about digital marketing including SEO, social media marketing, email campaigns, and analytics. Perfect for beginners and business owners.',
    'Marketing',
    1, -- Beginner
    1799.00,
    'English',
    2, -- Published
    NULL,
    'Emma Rodriguez',
    3421,
    4.6,
    1205,
    360, -- 6 hours
    '["marketing","seo","social media","business"]',
    '["Create effective marketing strategies","Master SEO techniques","Run successful social media campaigns","Analyze marketing metrics"]',
    GETDATE(),
    GETDATE()
);

-- Sections for Course 3
DECLARE @C3S1 UNIQUEIDENTIFIER = NEWID();
DECLARE @C3S2 UNIQUEIDENTIFIER = NEWID();

INSERT INTO Sections (SectionId, CourseId, Title, SortOrder) VALUES
(@C3S1, @Course3Id, 'Marketing Fundamentals', 1),
(@C3S2, @Course3Id, 'Social Media Marketing', 2);

-- Lessons for Course 3
INSERT INTO Lessons (LessonId, SectionId, Title, Type, VideoPath, DurationSeconds, IsFreePreview, SortOrder, IsPublished) VALUES
(NEWID(), @C3S1, 'Introduction to Digital Marketing', 0, NULL, 0, 1, 1, 1),
(NEWID(), @C3S1, 'SEO Basics', 0, NULL, 0, 0, 2, 1),
(NEWID(), @C3S2, 'Facebook Marketing', 0, NULL, 0, 0, 1, 1),
(NEWID(), @C3S2, 'Instagram Growth Strategies', 0, NULL, 0, 0, 2, 1);

-- ============================================================================
-- COURSE 4: Machine Learning A-Z
-- ============================================================================
DECLARE @Course4Id UNIQUEIDENTIFIER = NEWID();
INSERT INTO Courses (CourseId, InstructorId, Title, Subtitle, Slug, Description, Category, Level, Price, Language, Status, ThumbnailUrl, InstructorName, EnrollmentCount, AverageRating, ReviewCount, DurationMinutes, Tags, LearningObjectives, CreatedAt, UpdatedAt)
VALUES (
    @Course4Id,
    @InstructorId,
    'Machine Learning A-Z',
    'Hands-on Python & R in data science',
    'machine-learning-a-z',
    'Master machine learning algorithms and build real-world AI applications. Learn regression, classification, clustering, and deep learning with practical projects.',
    'Data Science',
    3, -- Advanced
    2999.00,
    'English',
    2, -- Published
    NULL,
    'Prof. David Kumar',
    1876,
    4.9,
    743,
    600, -- 10 hours
    '["machine learning","ai","python","data science"]',
    '["Implement ML algorithms from scratch","Build predictive models","Work with real datasets","Deploy ML models"]',
    GETDATE(),
    GETDATE()
);

-- Sections for Course 4
DECLARE @C4S1 UNIQUEIDENTIFIER = NEWID();
DECLARE @C4S2 UNIQUEIDENTIFIER = NEWID();

INSERT INTO Sections (SectionId, CourseId, Title, SortOrder) VALUES
(@C4S1, @Course4Id, 'Introduction to Machine Learning', 1),
(@C4S2, @Course4Id, 'Supervised Learning', 2);

-- Lessons for Course 4
INSERT INTO Lessons (LessonId, SectionId, Title, Type, VideoPath, DurationSeconds, IsFreePreview, SortOrder, IsPublished) VALUES
(NEWID(), @C4S1, 'What is Machine Learning?', 0, NULL, 0, 1, 1, 1),
(NEWID(), @C4S1, 'Setting Up Your Environment', 0, NULL, 0, 0, 2, 1),
(NEWID(), @C4S2, 'Linear Regression', 0, NULL, 0, 0, 1, 1),
(NEWID(), @C4S2, 'Logistic Regression', 0, NULL, 0, 0, 2, 1);

-- ============================================================================
-- COURSE 5: Graphic Design Fundamentals
-- ============================================================================
DECLARE @Course5Id UNIQUEIDENTIFIER = NEWID();
INSERT INTO Courses (CourseId, InstructorId, Title, Subtitle, Slug, Description, Category, Level, Price, Language, Status, ThumbnailUrl, InstructorName, EnrollmentCount, AverageRating, ReviewCount, DurationMinutes, Tags, LearningObjectives, CreatedAt, UpdatedAt)
VALUES (
    @Course5Id,
    @InstructorId,
    'Graphic Design Fundamentals',
    'Master Adobe Photoshop and Illustrator',
    'graphic-design-fundamentals',
    'Learn the principles of graphic design and master industry-standard tools. Create stunning visuals, logos, and marketing materials.',
    'Design',
    1, -- Beginner
    1599.00,
    'English',
    2, -- Published
    NULL,
    'Lisa Anderson',
    2543,
    4.7,
    967,
    300, -- 5 hours
    '["design","photoshop","illustrator","creative"]',
    '["Understand design principles","Master Photoshop tools","Create professional logos","Design marketing materials"]',
    GETDATE(),
    GETDATE()
);

-- Sections for Course 5
DECLARE @C5S1 UNIQUEIDENTIFIER = NEWID();
DECLARE @C5S2 UNIQUEIDENTIFIER = NEWID();

INSERT INTO Sections (SectionId, CourseId, Title, SortOrder) VALUES
(@C5S1, @Course5Id, 'Design Principles', 1),
(@C5S2, @Course5Id, 'Adobe Photoshop Basics', 2);

-- Lessons for Course 5
INSERT INTO Lessons (LessonId, SectionId, Title, Type, VideoPath, DurationSeconds, IsFreePreview, SortOrder, IsPublished) VALUES
(NEWID(), @C5S1, 'Color Theory', 0, NULL, 0, 1, 1, 1),
(NEWID(), @C5S1, 'Typography Basics', 0, NULL, 0, 0, 2, 1),
(NEWID(), @C5S2, 'Photoshop Interface', 0, NULL, 0, 0, 1, 1),
(NEWID(), @C5S2, 'Working with Layers', 0, NULL, 0, 0, 2, 1);

-- ============================================================================
-- COURSE 6: Financial Analysis and Modeling
-- ============================================================================
DECLARE @Course6Id UNIQUEIDENTIFIER = NEWID();
INSERT INTO Courses (CourseId, InstructorId, Title, Subtitle, Slug, Description, Category, Level, Price, Language, Status, ThumbnailUrl, InstructorName, EnrollmentCount, AverageRating, ReviewCount, DurationMinutes, Tags, LearningObjectives, CreatedAt, UpdatedAt)
VALUES (
    @Course6Id,
    @InstructorId,
    'Financial Analysis and Modeling',
    'Excel-based financial modeling for business',
    'financial-analysis-and-modeling',
    'Master financial analysis and build professional financial models in Excel. Learn valuation, forecasting, and investment analysis techniques.',
    'Finance',
    2, -- Intermediate
    2199.00,
    'English',
    2, -- Published
    NULL,
    'Robert Williams',
    1654,
    4.8,
    621,
    480, -- 8 hours
    '["finance","excel","modeling","business"]',
    '["Build financial models in Excel","Perform company valuations","Create financial forecasts","Analyze investment opportunities"]',
    GETDATE(),
    GETDATE()
);

-- Sections for Course 6
DECLARE @C6S1 UNIQUEIDENTIFIER = NEWID();
DECLARE @C6S2 UNIQUEIDENTIFIER = NEWID();

INSERT INTO Sections (SectionId, CourseId, Title, SortOrder) VALUES
(@C6S1, @Course6Id, 'Financial Statements Analysis', 1),
(@C6S2, @Course6Id, 'Building Financial Models', 2);

-- Lessons for Course 6
INSERT INTO Lessons (LessonId, SectionId, Title, Type, VideoPath, DurationSeconds, IsFreePreview, SortOrder, IsPublished) VALUES
(NEWID(), @C6S1, 'Understanding Financial Statements', 0, NULL, 0, 1, 1, 1),
(NEWID(), @C6S1, 'Ratio Analysis', 0, NULL, 0, 0, 2, 1),
(NEWID(), @C6S2, 'Three Statement Model', 0, NULL, 0, 0, 1, 1),
(NEWID(), @C6S2, 'DCF Valuation', 0, NULL, 0, 0, 2, 1);

-- ============================================================================
-- COURSE 7: Mobile App Development with Flutter
-- ============================================================================
DECLARE @Course7Id UNIQUEIDENTIFIER = NEWID();
INSERT INTO Courses (CourseId, InstructorId, Title, Subtitle, Slug, Description, Category, Level, Price, Language, Status, ThumbnailUrl, InstructorName, EnrollmentCount, AverageRating, ReviewCount, DurationMinutes, Tags, LearningObjectives, CreatedAt, UpdatedAt)
VALUES (
    @Course7Id,
    @InstructorId,
    'Mobile App Development with Flutter',
    'Build iOS and Android apps with one codebase',
    'mobile-app-development-with-flutter',
    'Learn Flutter and Dart to build beautiful, natively compiled mobile applications for iOS and Android from a single codebase.',
    'Mobile Development',
    2, -- Intermediate
    2299.00,
    'English',
    2, -- Published
    NULL,
    'Priya Sharma',
    1923,
    4.7,
    756,
    540, -- 9 hours
    '["flutter","dart","mobile","app development"]',
    '["Build cross-platform mobile apps","Master Flutter widgets","Implement state management","Publish apps to stores"]',
    GETDATE(),
    GETDATE()
);

-- Sections for Course 7
DECLARE @C7S1 UNIQUEIDENTIFIER = NEWID();
DECLARE @C7S2 UNIQUEIDENTIFIER = NEWID();
DECLARE @C7S3 UNIQUEIDENTIFIER = NEWID();

INSERT INTO Sections (SectionId, CourseId, Title, SortOrder) VALUES
(@C7S1, @Course7Id, 'Flutter Basics', 1),
(@C7S2, @Course7Id, 'Building User Interfaces', 2),
(@C7S3, @Course7Id, 'State Management', 3);

-- Lessons for Course 7
INSERT INTO Lessons (LessonId, SectionId, Title, Type, VideoPath, DurationSeconds, IsFreePreview, SortOrder, IsPublished) VALUES
(NEWID(), @C7S1, 'Introduction to Flutter', 0, NULL, 0, 1, 1, 1),
(NEWID(), @C7S1, 'Dart Programming Basics', 0, NULL, 0, 0, 2, 1),
(NEWID(), @C7S2, 'Flutter Widgets', 0, NULL, 0, 0, 1, 1),
(NEWID(), @C7S2, 'Layouts and Navigation', 0, NULL, 0, 0, 2, 1),
(NEWID(), @C7S3, 'Provider Pattern', 0, NULL, 0, 0, 1, 1),
(NEWID(), @C7S3, 'BLoC Architecture', 0, NULL, 0, 0, 2, 1);

-- ============================================================================
-- COURSE 8: Cybersecurity Essentials
-- ============================================================================
DECLARE @Course8Id UNIQUEIDENTIFIER = NEWID();
INSERT INTO Courses (CourseId, InstructorId, Title, Subtitle, Slug, Description, Category, Level, Price, Language, Status, ThumbnailUrl, InstructorName, EnrollmentCount, AverageRating, ReviewCount, DurationMinutes, Tags, LearningObjectives, CreatedAt, UpdatedAt)
VALUES (
    @Course8Id,
    @InstructorId,
    'Cybersecurity Essentials',
    'Protect systems and networks from cyber threats',
    'cybersecurity-essentials',
    'Learn the fundamentals of cybersecurity including network security, cryptography, ethical hacking, and security best practices.',
    'IT & Security',
    2, -- Intermediate
    2599.00,
    'English',
    2, -- Published
    NULL,
    'James Mitchell',
    1432,
    4.8,
    589,
    420, -- 7 hours
    '["cybersecurity","security","hacking","networking"]',
    '["Understand security threats","Implement security measures","Perform security audits","Protect against cyber attacks"]',
    GETDATE(),
    GETDATE()
);

-- Sections for Course 8
DECLARE @C8S1 UNIQUEIDENTIFIER = NEWID();
DECLARE @C8S2 UNIQUEIDENTIFIER = NEWID();

INSERT INTO Sections (SectionId, CourseId, Title, SortOrder) VALUES
(@C8S1, @Course8Id, 'Security Fundamentals', 1),
(@C8S2, @Course8Id, 'Network Security', 2);

-- Lessons for Course 8
INSERT INTO Lessons (LessonId, SectionId, Title, Type, VideoPath, DurationSeconds, IsFreePreview, SortOrder, IsPublished) VALUES
(NEWID(), @C8S1, 'Introduction to Cybersecurity', 0, NULL, 0, 1, 1, 1),
(NEWID(), @C8S1, 'Common Security Threats', 0, NULL, 0, 0, 2, 1),
(NEWID(), @C8S2, 'Firewalls and VPNs', 0, NULL, 0, 0, 1, 1),
(NEWID(), @C8S2, 'Intrusion Detection Systems', 0, NULL, 0, 0, 2, 1);

-- ============================================================================
-- COURSE 9: Content Writing Masterclass
-- ============================================================================
DECLARE @Course9Id UNIQUEIDENTIFIER = NEWID();
INSERT INTO Courses (CourseId, InstructorId, Title, Subtitle, Slug, Description, Category, Level, Price, Language, Status, ThumbnailUrl, InstructorName, EnrollmentCount, AverageRating, ReviewCount, DurationMinutes, Tags, LearningObjectives, CreatedAt, UpdatedAt)
VALUES (
    @Course9Id,
    @InstructorId,
    'Content Writing Masterclass',
    'Write compelling content that converts',
    'content-writing-masterclass',
    'Master the art of content writing for blogs, websites, and social media. Learn SEO writing, copywriting, and storytelling techniques.',
    'Writing',
    1, -- Beginner
    1299.00,
    'English',
    2, -- Published
    NULL,
    'Amanda Taylor',
    2876,
    4.6,
    1123,
    360, -- 6 hours
    '["writing","content","copywriting","seo"]',
    '["Write engaging blog posts","Master SEO writing","Create compelling copy","Develop your writing style"]',
    GETDATE(),
    GETDATE()
);

-- Sections for Course 9
DECLARE @C9S1 UNIQUEIDENTIFIER = NEWID();
DECLARE @C9S2 UNIQUEIDENTIFIER = NEWID();

INSERT INTO Sections (SectionId, CourseId, Title, SortOrder) VALUES
(@C9S1, @Course9Id, 'Writing Fundamentals', 1),
(@C9S2, @Course9Id, 'SEO Content Writing', 2);

-- Lessons for Course 9
INSERT INTO Lessons (LessonId, SectionId, Title, Type, VideoPath, DurationSeconds, IsFreePreview, SortOrder, IsPublished) VALUES
(NEWID(), @C9S1, 'Finding Your Writing Voice', 0, NULL, 0, 1, 1, 1),
(NEWID(), @C9S1, 'Writing Compelling Headlines', 0, NULL, 0, 0, 2, 1),
(NEWID(), @C9S2, 'Keyword Research', 0, NULL, 0, 0, 1, 1),
(NEWID(), @C9S2, 'Writing SEO-Friendly Content', 0, NULL, 0, 0, 2, 1);

-- ============================================================================
-- COURSE 10: Cloud Computing with AWS
-- ============================================================================
DECLARE @Course10Id UNIQUEIDENTIFIER = NEWID();
INSERT INTO Courses (CourseId, InstructorId, Title, Subtitle, Slug, Description, Category, Level, Price, Language, Status, ThumbnailUrl, InstructorName, EnrollmentCount, AverageRating, ReviewCount, DurationMinutes, Tags, LearningObjectives, CreatedAt, UpdatedAt)
VALUES (
    @Course10Id,
    @InstructorId,
    'Cloud Computing with AWS',
    'Master Amazon Web Services from scratch',
    'cloud-computing-with-aws',
    'Learn AWS cloud computing including EC2, S3, Lambda, and more. Prepare for AWS certification and build scalable cloud applications.',
    'Cloud Computing',
    2, -- Intermediate
    2799.00,
    'English',
    2, -- Published
    NULL,
    'Kevin Zhang',
    1765,
    4.9,
    698,
    540, -- 9 hours
    '["aws","cloud","devops","infrastructure"]',
    '["Deploy applications on AWS","Master core AWS services","Implement cloud security","Prepare for AWS certification"]',
    GETDATE(),
    GETDATE()
);

-- Sections for Course 10
DECLARE @C10S1 UNIQUEIDENTIFIER = NEWID();
DECLARE @C10S2 UNIQUEIDENTIFIER = NEWID();
DECLARE @C10S3 UNIQUEIDENTIFIER = NEWID();

INSERT INTO Sections (SectionId, CourseId, Title, SortOrder) VALUES
(@C10S1, @Course10Id, 'AWS Fundamentals', 1),
(@C10S2, @Course10Id, 'Core AWS Services', 2),
(@C10S3, @Course10Id, 'Advanced AWS', 3);

-- Lessons for Course 10
INSERT INTO Lessons (LessonId, SectionId, Title, Type, VideoPath, DurationSeconds, IsFreePreview, SortOrder, IsPublished) VALUES
(NEWID(), @C10S1, 'Introduction to AWS', 0, NULL, 0, 1, 1, 1),
(NEWID(), @C10S1, 'AWS Account Setup', 0, NULL, 0, 0, 2, 1),
(NEWID(), @C10S2, 'EC2 Instances', 0, NULL, 0, 0, 1, 1),
(NEWID(), @C10S2, 'S3 Storage', 0, NULL, 0, 0, 2, 1),
(NEWID(), @C10S3, 'Lambda Functions', 0, NULL, 0, 0, 1, 1),
(NEWID(), @C10S3, 'CloudFormation', 0, NULL, 0, 0, 2, 1);

GO

PRINT '✅ Successfully inserted 10 dummy courses with sections and lessons!';
PRINT '';
PRINT 'Course Summary:';
PRINT '1. Complete Python Programming Bootcamp (7 hours) - 2 sections, 4 lessons';
PRINT '2. Web Development with React (9 hours) - 3 sections, 6 lessons';
PRINT '3. Digital Marketing Masterclass (6 hours) - 2 sections, 4 lessons';
PRINT '4. Machine Learning A-Z (10 hours) - 2 sections, 4 lessons';
PRINT '5. Graphic Design Fundamentals (5 hours) - 2 sections, 4 lessons';
PRINT '6. Financial Analysis and Modeling (8 hours) - 2 sections, 4 lessons';
PRINT '7. Mobile App Development with Flutter (9 hours) - 3 sections, 6 lessons';
PRINT '8. Cybersecurity Essentials (7 hours) - 2 sections, 4 lessons';
PRINT '9. Content Writing Masterclass (6 hours) - 2 sections, 4 lessons';
PRINT '10. Cloud Computing with AWS (9 hours) - 3 sections, 6 lessons';
PRINT '';
PRINT 'All courses are published and ready for enrollment!';
PRINT 'You can now add videos to the lessons through the instructor dashboard.';
