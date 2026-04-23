using EduLearn.Enrollment.API.Domain.Enums;

namespace EduLearn.Enrollment.API.Domain.Entities;

// Represents a student's enrollment in a course
// OOP: domain methods encapsulate business rules for completion and progress
public class Enrollment
{
    public Guid             EnrollmentId  { get; private set; } = Guid.NewGuid();
    public Guid             StudentId     { get; private set; }  // FK → Identity.API
    public Guid             CourseId      { get; private set; }  // FK → Course.API
    public DateTime         EnrolledAt    { get; private set; } = DateTime.UtcNow;
    public EnrollmentStatus Status        { get; private set; } = EnrollmentStatus.Active;
    public decimal          ProgressPct   { get; private set; }  // 0–100
    public int              TotalLessons  { get; private set; }  // Snapshot at enrollment time
    public int              CompletedLessons { get; private set; }
    public DateTime?        CompletedAt   { get; private set; }
    public Guid?            PaymentId     { get; private set; }  // Null for free courses

    // Navigation: one enrollment → many lesson progress records
    public ICollection<LessonProgress> LessonProgresses { get; private set; } = new List<LessonProgress>();

    private Enrollment() { }

    // Factory for free course enrollment
    public static Enrollment CreateFree(Guid studentId, Guid courseId, int totalLessons) =>
        new() { StudentId = studentId, CourseId = courseId, TotalLessons = totalLessons };

    // Factory for paid course enrollment (triggered by Payment saga)
    public static Enrollment CreatePaid(Guid studentId, Guid courseId, int totalLessons, Guid paymentId) =>
        new() { StudentId = studentId, CourseId = courseId, TotalLessons = totalLessons, PaymentId = paymentId };

    // Called when a lesson is marked complete — recalculates progress
    public void RecordLessonCompletion()
    {
        CompletedLessons++;

        // Recalculate progress percentage (avoid divide-by-zero)
        ProgressPct = TotalLessons > 0
            ? Math.Round((decimal)CompletedLessons / TotalLessons * 100, 2)
            : 0;

        // Auto-complete the enrollment when all lessons done
        if (CompletedLessons >= TotalLessons)
            MarkCompleted();
    }

    private void MarkCompleted()
    {
        Status      = EnrollmentStatus.Completed;
        ProgressPct = 100;
        CompletedAt = DateTime.UtcNow;
    }
}
