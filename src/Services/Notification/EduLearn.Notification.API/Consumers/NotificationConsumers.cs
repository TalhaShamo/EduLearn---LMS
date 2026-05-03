using MassTransit;
using EduLearn.Notification.API.Services;
using EduLearn.Notification.API.Templates;
using EduLearn.Shared.Events;
using Microsoft.Extensions.Logging;

namespace EduLearn.Notification.API.Consumers;

// Each consumer handles one event type and sends the appropriate email

// ── USER REGISTERED ───────────────────────────────────────────
public class UserRegisteredConsumer : IConsumer<UserRegisteredEvent>
{
    private readonly EmailService _email;
    public UserRegisteredConsumer(EmailService email) => _email = email;

    public async Task Consume(ConsumeContext<UserRegisteredEvent> ctx)
    {
        var e = ctx.Message;
        await _email.SendAsync(e.Email, e.FullName, "Welcome to EduLearn! 🎓",
            EmailTemplates.WelcomeEmail(e.FullName, e.Role));
    }
}

// ── STUDENT ENROLLED ──────────────────────────────────────────
public class StudentEnrolledConsumer : IConsumer<StudentEnrolledEvent>
{
    private readonly EmailService _email;
    public StudentEnrolledConsumer(EmailService email) => _email = email;

    public async Task Consume(ConsumeContext<StudentEnrolledEvent> ctx)
    {
        var e = ctx.Message;
        await _email.SendAsync(e.StudentEmail, e.StudentName,
            $"Enrolled in {e.CourseTitle}!",
            EmailTemplates.EnrollmentConfirmation(e.StudentName, e.CourseTitle));
    }
}

// ── PAYMENT SUCCEEDED ─────────────────────────────────────────
public class PaymentSucceededConsumer : IConsumer<PaymentSucceededEvent>
{
    private readonly EmailService _email;
    private readonly ILogger<PaymentSucceededConsumer> _logger;

    public PaymentSucceededConsumer(EmailService email, ILogger<PaymentSucceededConsumer> logger)
    { _email = email; _logger = logger; }

    public async Task Consume(ConsumeContext<PaymentSucceededEvent> ctx)
    {
        var e = ctx.Message;
        _logger.LogInformation("Sending payment success email for order {OrderId}", e.OrderId);
        await _email.SendAsync("student@email.com", "Student",
            "Payment Successful ✅",
            EmailTemplates.PaymentSuccess("Student", "Course", e.Amount));
    }
}

// ── PAYMENT FAILED ────────────────────────────────────────────
public class PaymentFailedConsumer : IConsumer<PaymentFailedEvent>
{
    private readonly EmailService _email;
    public PaymentFailedConsumer(EmailService email) => _email = email;

    public async Task Consume(ConsumeContext<PaymentFailedEvent> ctx)
    {
        var e = ctx.Message;
        await _email.SendAsync("student@email.com", "Student",
            "Payment Failed ❌",
            EmailTemplates.PaymentFailed("Student", e.Reason));
    }
}

// ── QUIZ GRADED ───────────────────────────────────────────────
public class QuizGradedConsumer : IConsumer<QuizGradedEvent>
{
    private readonly EmailService _email;
    public QuizGradedConsumer(EmailService email) => _email = email;

    public async Task Consume(ConsumeContext<QuizGradedEvent> ctx)
    {
        var e = ctx.Message;
        await _email.SendAsync(e.StudentEmail, e.StudentName,
            $"Quiz Result: {e.QuizTitle}",
            EmailTemplates.QuizResult(e.StudentName, e.QuizTitle, e.Score, e.MaxScore, e.Passed));
    }
}

// ── ASSIGNMENT GRADED ─────────────────────────────────────────
public class AssignmentGradedConsumer : IConsumer<AssignmentGradedEvent>
{
    private readonly EmailService _email;
    public AssignmentGradedConsumer(EmailService email) => _email = email;

    public async Task Consume(ConsumeContext<AssignmentGradedEvent> ctx)
    {
        var e = ctx.Message;
        await _email.SendAsync(e.StudentEmail, e.StudentName,
            $"Assignment Graded: {e.AssignmentTitle}",
            EmailTemplates.AssignmentGraded(e.StudentName, e.AssignmentTitle, e.Score, e.MaxScore, e.Feedback));
    }
}

// ── CERTIFICATE ISSUED ────────────────────────────────────────
public class CertificateIssuedConsumer : IConsumer<CertificateIssuedEvent>
{
    private readonly EmailService _email;
    public CertificateIssuedConsumer(EmailService email) => _email = email;

    public async Task Consume(ConsumeContext<CertificateIssuedEvent> ctx)
    {
        var e = ctx.Message;
        await _email.SendAsync(e.StudentEmail, e.StudentName,
            "Your Certificate is Ready! 🏆",
            EmailTemplates.CertificateReady(e.StudentName, e.CourseTitle, e.DownloadUrl));
    }
}

// ── EMAIL VERIFICATION ────────────────────────────────────────
public class EmailVerificationConsumer : IConsumer<EmailVerificationRequestedEvent>
{
    private readonly EmailService _email;
    public EmailVerificationConsumer(EmailService email) => _email = email;

    public async Task Consume(ConsumeContext<EmailVerificationRequestedEvent> ctx)
    {
        var e = ctx.Message;
        await _email.SendAsync(e.Email, e.FullName,
            "Verify Your EduLearn Email",
            EmailTemplates.EmailVerification(e.FullName, e.VerificationLink));
    }
}

// ── PASSWORD RESET ────────────────────────────────────────────
public class PasswordResetConsumer : IConsumer<PasswordResetRequestedEvent>
{
    private readonly EmailService _email;
    public PasswordResetConsumer(EmailService email) => _email = email;

    public async Task Consume(ConsumeContext<PasswordResetRequestedEvent> ctx)
    {
        var e = ctx.Message;
        await _email.SendAsync(e.Email, e.FullName,
            "Reset Your EduLearn Password",
            EmailTemplates.PasswordReset(e.FullName, e.ResetLink));
    }
}
