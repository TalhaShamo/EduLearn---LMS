namespace EduLearn.Shared.Events;

// Published by Certificate.API when a PDF certificate is ready
// Consumed by Notification.API → sends certificate download email
public record CertificateIssuedEvent(
    Guid CertificateId,
    Guid StudentId,
    Guid CourseId,
    string StudentEmail,
    string StudentName,
    string CourseTitle,
    string DownloadUrl,
    string VerificationUrl,
    DateTime IssuedAt
);
