namespace EduLearn.Shared.Events;

// Published by Enrollment.API when a student is enrolled (free or paid)
// Consumed by Notification.API → sends confirmation email
public record StudentEnrolledEvent(
    Guid EnrollmentId,
    Guid StudentId,
    Guid CourseId,
    string StudentEmail,
    string StudentName,
    string CourseTitle,
    DateTime EnrolledAt
);

// Published by Enrollment.API when a student completes 100% of a course
// Consumed by Certificate.API → triggers PDF generation
public record CourseCompletedEvent(
    Guid EnrollmentId,
    Guid StudentId,
    Guid CourseId,
    string StudentName,
    string StudentEmail,
    string CourseTitle,
    string InstructorName,
    DateTime CompletedAt
);
