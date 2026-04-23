using EduLearn.Enrollment.API.Domain.Enums;

namespace EduLearn.Enrollment.API.Domain.Entities;

// Tracks a single student's progress on a single lesson
public class LessonProgress
{
    public Guid                 ProgressId     { get; private set; } = Guid.NewGuid();
    public Guid                 EnrollmentId   { get; private set; }
    public Guid                 LessonId       { get; private set; }  // FK → Course.API lesson
    public LessonProgressStatus Status         { get; private set; } = LessonProgressStatus.NotStarted;
    public int                  WatchedSeconds { get; private set; }  // For video lessons
    public DateTime?            CompletedAt    { get; private set; }
    public DateTime             LastAccessedAt { get; private set; } = DateTime.UtcNow;

    public Enrollment Enrollment { get; private set; } = null!;

    private LessonProgress() { }

    public static LessonProgress Create(Guid enrollmentId, Guid lessonId) =>
        new() { EnrollmentId = enrollmentId, LessonId = lessonId };

    // Update video watch progress — called by heartbeat every 10 seconds
    public void UpdateVideoProgress(int watchedSeconds, int totalSeconds)
    {
        WatchedSeconds   = watchedSeconds;
        LastAccessedAt   = DateTime.UtcNow;
        Status           = LessonProgressStatus.InProgress;

        // Auto-complete when student has watched ≥ 80% of the video
        var pct = totalSeconds > 0 ? (double)watchedSeconds / totalSeconds * 100 : 0;
        if (pct >= 80 && Status != LessonProgressStatus.Completed)
            MarkCompleted();
    }

    // Called for Article lessons (scroll to bottom) or Quiz/Assignment pass
    public void MarkCompleted()
    {
        Status      = LessonProgressStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }
}
