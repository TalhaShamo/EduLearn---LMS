using EduLearn.Assessment.API.Domain.Enums;

namespace EduLearn.Assessment.API.Domain.Entities;

// Assignment belongs to a lesson — file-upload based submission
public class Assignment
{
    public Guid     AssignmentId      { get; private set; } = Guid.NewGuid();
    public Guid     LessonId          { get; private set; }
    public Guid     CourseId          { get; private set; }
    public string   Title             { get; private set; } = string.Empty;
    public string   Description       { get; private set; } = string.Empty;
    public DateTime DueDate           { get; private set; }
    public decimal  MaxScore          { get; private set; }
    public int      MaxFileSizeMb     { get; private set; } = 50;
    public string   AllowedFormats    { get; private set; } = ".pdf,.docx,.zip";
    public decimal  LatePenaltyPct    { get; private set; } // Deducted from score if late

    public ICollection<Submission> Submissions { get; private set; } = new List<Submission>();

    private Assignment() { }

    public static Assignment Create(Guid lessonId, Guid courseId, string title,
        string description, DateTime dueDate, decimal maxScore, string allowedFormats) =>
        new()
        {
            LessonId       = lessonId, CourseId       = courseId,
            Title          = title.Trim(), Description = description.Trim(),
            DueDate        = dueDate, MaxScore          = maxScore,
            AllowedFormats = allowedFormats
        };
}

// Student's file submission for an assignment
public class Submission
{
    public Guid             SubmissionId { get; private set; } = Guid.NewGuid();
    public Guid             AssignmentId { get; private set; }
    public Guid             StudentId    { get; private set; }
    public string           FilePath     { get; private set; } = string.Empty; // Local path
    public string           FileName     { get; private set; } = string.Empty;
    public DateTime         SubmittedAt  { get; private set; } = DateTime.UtcNow;
    public bool             IsLate       { get; private set; }
    public SubmissionStatus Status       { get; private set; } = SubmissionStatus.Submitted;
    public decimal?         Score        { get; private set; }
    public string?          Feedback     { get; private set; }
    public Guid?            GradedByInstructorId { get; private set; }
    public DateTime?        GradedAt     { get; private set; }

    public Assignment Assignment { get; private set; } = null!;

    private Submission() { }

    public static Submission Create(Guid assignmentId, Guid studentId, string filePath,
        string fileName, bool isLate) =>
        new()
        {
            AssignmentId = assignmentId, StudentId = studentId,
            FilePath = filePath, FileName  = fileName, IsLate = isLate
        };

    // Instructor grades the submission
    public void Grade(decimal score, string feedback, Guid instructorId)
    {
        Score                = score;
        Feedback             = feedback;
        GradedByInstructorId = instructorId;
        GradedAt             = DateTime.UtcNow;
        Status               = SubmissionStatus.Graded;
    }
}
