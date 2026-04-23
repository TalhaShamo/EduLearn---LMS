namespace EduLearn.Shared.Events;

// Published by Assessment.API when a quiz attempt is fully graded
// Consumed by Notification.API → sends quiz result email
public record QuizGradedEvent(
    Guid AttemptId,
    Guid StudentId,
    Guid QuizId,
    string StudentEmail,
    string StudentName,
    string QuizTitle,
    decimal Score,
    decimal MaxScore,
    bool Passed,
    DateTime GradedAt
);

// Published by Assessment.API when an instructor grades an assignment
// Consumed by Notification.API → sends grading notification email
public record AssignmentGradedEvent(
    Guid SubmissionId,
    Guid StudentId,
    Guid AssignmentId,
    string StudentEmail,
    string StudentName,
    string AssignmentTitle,
    decimal Score,
    decimal MaxScore,
    string Feedback,
    DateTime GradedAt
);
