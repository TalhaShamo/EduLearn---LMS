using EduLearn.Course.API.Domain.Enums;

namespace EduLearn.Course.API.Domain.Entities;

// Course aggregate root — owns Sections which own Lessons
// OOP: business rules (Submit, Publish, Archive) are inside the entity
public class Course
{
    public Guid         CourseId         { get; private set; } = Guid.NewGuid();
    public Guid         InstructorId     { get; private set; }  // FK to Identity.API user
    public string       Title            { get; private set; } = string.Empty;
    public string       Slug             { get; private set; } = string.Empty; // URL-friendly title
    public string       Description      { get; private set; } = string.Empty;
    public string       Category         { get; private set; } = string.Empty;
    public CourseLevel  Level            { get; private set; }
    public string       Language         { get; private set; } = "English";
    public string?      ThumbnailUrl     { get; private set; }
    public decimal      Price            { get; private set; }   // 0 = free
    public CourseStatus Status           { get; private set; } = CourseStatus.Draft;
    public string?      AdminFeedback    { get; private set; }   // Populated when admin requests changes
    public DateTime     CreatedAt        { get; private set; } = DateTime.UtcNow;
    public DateTime     UpdatedAt        { get; private set; } = DateTime.UtcNow;

    // Navigation: one course → many sections
    public ICollection<Section> Sections { get; private set; } = new List<Section>();

    private Course() { }

    // Factory — creates a new draft course
    public static Course Create(Guid instructorId, string title, string description,
        string category, CourseLevel level, decimal price, string language = "English")
    {
        return new Course
        {
            InstructorId = instructorId,
            Title        = title.Trim(),
            Slug         = GenerateSlug(title),
            Description  = description.Trim(),
            Category     = category,
            Level        = level,
            Price        = price,
            Language     = language
        };
    }

    // Business rules: transition methods ─────────────────────────
    public void Update(string title, string description, string category, CourseLevel level, decimal price)
    {
        Title       = title.Trim();
        Slug        = GenerateSlug(title);
        Description = description.Trim();
        Category    = category;
        Level       = level;
        Price       = price;
        UpdatedAt   = DateTime.UtcNow;
    }

    public void SetThumbnail(string url) { ThumbnailUrl = url; UpdatedAt = DateTime.UtcNow; }

    // Instructor submits for admin review
    public void SubmitForReview()
    {
        if (Status != CourseStatus.Draft && Status != CourseStatus.PendingReview)
            throw new InvalidOperationException("Only draft courses can be submitted for review.");
        Status    = CourseStatus.PendingReview;
        UpdatedAt = DateTime.UtcNow;
    }

    // Admin approves → goes live
    public void Publish()
    {
        Status    = CourseStatus.Published;
        UpdatedAt = DateTime.UtcNow;
    }

    // Admin requests changes → back to draft
    public void RequestChanges(string feedback)
    {
        Status        = CourseStatus.Draft;
        AdminFeedback = feedback;
        UpdatedAt     = DateTime.UtcNow;
    }

    // Instructor archives course (keeps enrollment data)
    public void Archive()
    {
        Status    = CourseStatus.Archived;
        UpdatedAt = DateTime.UtcNow;
    }

    // Generate a URL slug from the title (e.g., "Intro to C#" → "intro-to-c")
    private static string GenerateSlug(string title) =>
        System.Text.RegularExpressions.Regex
            .Replace(title.ToLowerInvariant().Trim(), @"[^a-z0-9\s-]", "")
            .Replace(" ", "-");
}
