namespace EduLearn.Certificate.API.Domain.Entities;

// Certificate record stored in EduLearnCertificateDb
public class Certificate
{
    public Guid     CertificateId    { get; set; } = Guid.NewGuid();
    public Guid     StudentId        { get; set; }
    public Guid     CourseId         { get; set; }
    public string   StudentName      { get; set; } = string.Empty;
    public string   CourseTitle      { get; set; } = string.Empty;
    public string   InstructorName   { get; set; } = string.Empty;
    public string   FilePath         { get; set; } = string.Empty;  // Local PDF path
    public string   VerificationCode { get; set; } = string.Empty;  // Unique public code
    public DateTime IssuedAt         { get; set; } = DateTime.UtcNow;
}
