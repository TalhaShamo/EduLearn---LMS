namespace EduLearn.Course.API.Application.DTOs;

// ── COURSE DTOs ───────────────────────────────────────────────

// Used for creating or updating a course
public record CreateCourseRequest(
    string Title,
    string Description,
    string Category,
    string Level,        // "Beginner" | "Intermediate" | "Advanced"
    decimal Price,
    string Language
);

public record UpdateCourseRequest(
    string Title,
    string Description,
    string Category,
    string Level,
    decimal Price
);

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
    string  Slug,
    string  Description,
    string  Category,
    string  Level,
    decimal Price,
    string  Language,
    string  Status,
    string? ThumbnailUrl,
    string? AdminFeedback,
    Guid    InstructorId,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IEnumerable<SectionDto> Sections
);

// ── SECTION DTOs ──────────────────────────────────────────────
public record CreateSectionRequest(string Title, int SortOrder);
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
    string Type,       // "Video" | "Article" | "Quiz" | "Assignment"
    int    SortOrder,
    bool   IsFreePreview
);

public record LessonDto(
    Guid    LessonId,
    string  Title,
    string  Type,
    string? VideoPath,
    int     DurationSeconds,
    bool    IsFreePreview,
    int     SortOrder,
    bool    IsPublished
);
