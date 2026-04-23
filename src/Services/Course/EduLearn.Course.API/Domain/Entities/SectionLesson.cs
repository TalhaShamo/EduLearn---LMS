using EduLearn.Course.API.Domain.Enums;

namespace EduLearn.Course.API.Domain.Entities;

// Section groups related lessons within a course (e.g., "Module 1: Basics")
public class Section
{
    public Guid     SectionId { get; private set; } = Guid.NewGuid();
    public Guid     CourseId  { get; private set; }
    public string   Title     { get; private set; } = string.Empty;
    public int      SortOrder { get; private set; }  // Controls display order

    // Navigation: one section → many lessons
    public ICollection<Lesson> Lessons { get; private set; } = new List<Lesson>();
    public Course Course { get; private set; } = null!;

    private Section() { }

    public static Section Create(Guid courseId, string title, int sortOrder) =>
        new() { CourseId = courseId, Title = title.Trim(), SortOrder = sortOrder };

    public void Update(string title, int sortOrder)
    {
        Title     = title.Trim();
        SortOrder = sortOrder;
    }
}

// Lesson is a single unit of content within a section
public class Lesson
{
    public Guid       LessonId        { get; private set; } = Guid.NewGuid();
    public Guid       SectionId       { get; private set; }
    public string     Title           { get; private set; } = string.Empty;
    public LessonType Type            { get; private set; }
    public string?    VideoPath       { get; private set; }  // Relative path to local video file
    public string?    RichContent     { get; private set; }  // HTML for Article lessons
    public int        DurationSeconds { get; private set; }  // Estimated duration
    public bool       IsFreePreview   { get; private set; }  // Visible to unenrolled users?
    public int        SortOrder       { get; private set; }
    public bool       IsPublished     { get; private set; }

    public Section Section { get; private set; } = null!;

    private Lesson() { }

    public static Lesson Create(Guid sectionId, string title, LessonType type, int sortOrder) =>
        new() { SectionId = sectionId, Title = title.Trim(), Type = type, SortOrder = sortOrder };

    // Called after video upload completes — stores the local file path
    public void SetVideoPath(string path, int durationSeconds)
    {
        VideoPath       = path;
        DurationSeconds = durationSeconds;
    }

    public void SetArticleContent(string html) => RichContent = html;

    public void Publish()       => IsPublished = true;
    public void Unpublish()     => IsPublished = false;
    public void SetFreePreview(bool val) => IsFreePreview = val;
}
