namespace EduLearn.Course.API.Domain.Enums;

public enum CourseStatus
{
    Draft          = 1,  // Instructor is building it
    PendingReview  = 2,  // Submitted — awaiting admin approval
    Published      = 3,  // Live on catalog
    Archived       = 4   // Hidden but enrollment data preserved
}

public enum CourseLevel
{
    Beginner     = 1,
    Intermediate = 2,
    Advanced     = 3
}

public enum LessonType
{
    Video       = 1,   // MP4 video — served via byte-range streaming
    Article     = 2,   // Rich text / HTML content
    Quiz        = 3,   // Links to a quiz in Assessment.API
    Assignment  = 4    // Links to an assignment in Assessment.API
}
