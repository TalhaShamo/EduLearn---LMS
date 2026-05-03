namespace EduLearn.Course.API.Application.DTOs;

// ── COURSE DTOs ───────────────────────────────────────────────

// Used for creating or updating a course
public record CreateCourseRequest(
    string Title,
    string? Subtitle,
    string Description,
    string? Category,        // Optional - will use CategoryName if empty
    string? CategoryName,    // Frontend sends this instead of Category sometimes
    string Level,            // "Beginner" | "Intermediate" | "Advanced"
    decimal Price,
    string Language,
    string? Status,          // "Draft" | "PendingReview"
    List<string>? Tags,
    List<string>? LearningObjectives,
    List<CreateSectionRequest>? Sections
)
{
    // Map CategoryName to Category if Category is empty
    public string GetCategory() => !string.IsNullOrEmpty(Category) ? Category : (CategoryName ?? "Uncategorized");
};

public record UpdateCourseRequest(
    string Title,
    string? Subtitle,
    string Description,
    string? Category,
    string? CategoryName,
    string Level,
    decimal Price,
    string? Language,
    string? Status,
    List<string>? Tags,
    List<string>? LearningObjectives,
    List<CreateSectionRequest>? Sections
)
{
    public string GetCategory() => !string.IsNullOrEmpty(Category) ? Category : (CategoryName ?? "Uncategorized");
};

// Admin: request changes with a feedback message
public record RequestChangesRequest(string Feedback);

// Returned in catalog listings — lightweight
public record CourseListDto(
    Guid    CourseId,
    string  Title,
    string  Slug,
    string  Category,
    string  Level,
    decimal Price,
    string  Status,
    string? ThumbnailUrl,
    Guid    InstructorId,
    DateTime CreatedAt
);

// Full course detail — returned on GET /courses/{id}
public record CourseDetailDto(
    Guid    CourseId,
    string  Title,
    string? Subtitle,
    string  Slug,
    string  Description,
    string  Category,
    string? CategoryName,  // Same as Category for frontend compatibility
    string  Level,
    decimal Price,
    string  Language,
    string  Status,
    string? ThumbnailUrl,
    string? AdminFeedback,
    Guid    InstructorId,
    string? InstructorName,
    int     EnrollmentCount,
    decimal AverageRating,
    int     ReviewCount,
    int     DurationMinutes,
    List<string> Tags,
    List<string> LearningObjectives,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IEnumerable<SectionDto> Sections
);

// ── SECTION DTOs ──────────────────────────────────────────────
public record CreateSectionRequest(string Title, int SortOrder = 0, List<CreateLessonRequest>? Lessons = null);
public record UpdateSectionRequest(string Title, int SortOrder);

public record SectionDto(
    Guid   SectionId,
    string Title,
    int    SortOrder,
    IEnumerable<LessonDto> Lessons
);

// ── LESSON DTOs ───────────────────────────────────────────────
public record CreateLessonRequest(
    string Title,
    string? Type = null,       // "Video" | "Article" | "Quiz" | "Assignment"
    string? LessonType = null, // Frontend sends this instead of Type
    int SortOrder = 0,
    bool IsFreePreview = false
)
{
    // Map LessonType to Type if Type is empty
    public string GetLessonType() => !string.IsNullOrEmpty(Type) ? Type : (LessonType ?? "Video");
};

public record LessonDto(
    Guid    LessonId,
    string  Title,
    string  LessonType,
    string? VideoPath,
    int     DurationSeconds,
    bool    IsFreePreview,
    int     SortOrder,
    bool    IsPublished
);
