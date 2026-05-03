namespace EduLearn.Notification.API.Templates;

// HTML email templates — using standard string concatenation (no raw strings for CSS braces)
public static class EmailTemplates
{
    private const string Css = @"
        body { font-family: 'Segoe UI', Arial, sans-serif; background:#f4f4f4; margin:0; padding:0; }
        .container { max-width:600px; margin:20px auto; background:#fff; border-radius:8px; overflow:hidden; box-shadow:0 2px 8px rgba(0,0,0,.1); }
        .header { background:linear-gradient(135deg,#667eea,#764ba2); color:#fff; padding:30px 40px; }
        .header h1 { margin:0; font-size:22px; }
        .body { padding:30px 40px; color:#333; line-height:1.6; }
        .btn { display:inline-block; padding:12px 28px; background:#667eea; color:#fff; text-decoration:none; border-radius:6px; margin-top:16px; font-weight:bold; }
        .footer { background:#f9f9f9; border-top:1px solid #eee; padding:16px 40px; font-size:12px; color:#999; text-align:center; }";

    private static string Wrap(string title, string body) =>
        "<!DOCTYPE html><html><head><meta charset='utf-8'><style>" + Css + "</style></head>" +
        "<body><div class='container'>" +
        "<div class='header'><h1>🎓 EduLearn</h1><p style='margin:4px 0 0'>" + title + "</p></div>" +
        "<div class='body'>" + body + "</div>" +
        "<div class='footer'>© 2024 EduLearn. All rights reserved.</div>" +
        "</div></body></html>";

    public static string WelcomeEmail(string name, string role) => Wrap("Welcome to EduLearn!",
        "<h2>Hello, " + name + "! 👋</h2>" +
        "<p>Your <strong>" + role + "</strong> account has been created successfully.</p>" +
        "<p>You can now log in and start your learning journey.</p>" +
        "<a class='btn' href='http://localhost:4200/login'>Get Started</a>");

    public static string EnrollmentConfirmation(string name, string courseTitle) => Wrap("Enrollment Confirmed!",
        "<h2>You're enrolled! 🎉</h2>" +
        "<p>Hi <strong>" + name + "</strong>, you have successfully enrolled in:</p>" +
        "<h3 style='color:#667eea'>" + courseTitle + "</h3>" +
        "<p>Start learning at your own pace. Good luck!</p>" +
        "<a class='btn' href='http://localhost:4200/my-courses'>Go to My Courses</a>");

    public static string PaymentSuccess(string name, string courseTitle, decimal amount) => Wrap("Payment Successful!",
        "<h2>Payment Confirmed ✅</h2>" +
        "<p>Hi <strong>" + name + "</strong>, your payment of <strong>₹" + amount + "</strong> for <strong>" + courseTitle + "</strong> was successful.</p>" +
        "<p>You are now enrolled. Happy learning!</p>" +
        "<a class='btn' href='http://localhost:4200/my-courses'>Start Learning</a>");

    public static string PaymentFailed(string name, string reason) => Wrap("Payment Failed",
        "<h2>Payment Unsuccessful ❌</h2>" +
        "<p>Hi <strong>" + name + "</strong>, your payment could not be processed.</p>" +
        "<p>Reason: <em>" + reason + "</em></p>" +
        "<a class='btn' href='http://localhost:4200/support'>Contact Support</a>");

    public static string QuizResult(string name, string quizTitle, decimal score, decimal maxScore, bool passed) =>
        Wrap("Quiz Result: " + quizTitle,
            "<h2>Quiz Result 📊</h2>" +
            "<p>Hi <strong>" + name + "</strong>, here are your results for <strong>" + quizTitle + "</strong>:</p>" +
            "<table style='width:100%;border-collapse:collapse;margin:16px 0'>" +
            "<tr><td style='padding:8px;border:1px solid #eee'>Score</td>" +
            "<td style='padding:8px;border:1px solid #eee'><strong>" + score + " / " + maxScore + "</strong></td></tr>" +
            "<tr><td style='padding:8px;border:1px solid #eee'>Status</td>" +
            "<td style='padding:8px;border:1px solid #eee;color:" + (passed ? "green" : "red") + "'>" +
            "<strong>" + (passed ? "✅ Passed" : "❌ Failed") + "</strong></td></tr></table>" +
            "<a class='btn' href='http://localhost:4200/my-courses'>Back to Course</a>");

    public static string CertificateReady(string name, string courseTitle, string downloadUrl) =>
        Wrap("Your Certificate is Ready!",
            "<h2>Congratulations! 🏆</h2>" +
            "<p>Hi <strong>" + name + "</strong>, you have successfully completed:</p>" +
            "<h3 style='color:#667eea'>" + courseTitle + "</h3>" +
            "<p>Your certificate of completion is ready to download.</p>" +
            "<a class='btn' href='" + downloadUrl + "'>Download Certificate</a>");

    public static string AssignmentGraded(string name, string assignmentTitle, decimal score, decimal max, string feedback) =>
        Wrap("Assignment Graded: " + assignmentTitle,
            "<h2>Assignment Graded 📝</h2>" +
            "<p>Hi <strong>" + name + "</strong>, your submission for <strong>" + assignmentTitle + "</strong> has been graded.</p>" +
            "<p>Score: <strong>" + score + " / " + max + "</strong></p>" +
            "<p>Feedback: <em>" + feedback + "</em></p>" +
            "<a class='btn' href='http://localhost:4200/my-courses'>View Course</a>");

    public static string EmailVerification(string name, string verificationLink) => Wrap("Verify Your Email",
        "<h2>Welcome to EduLearn! 🎓</h2>" +
        "<p>Hi <strong>" + name + "</strong>, thank you for registering with EduLearn.</p>" +
        "<p>Please verify your email address by clicking the button below:</p>" +
        "<a class='btn' href='" + verificationLink + "'>Verify Email</a>" +
        "<p style='margin-top:20px;font-size:14px;color:#666'>This link will expire in 24 hours.</p>" +
        "<p style='font-size:14px;color:#666'>If you didn't create an account, you can safely ignore this email.</p>");

    public static string PasswordReset(string name, string resetLink) => Wrap("Reset Your Password",
        "<h2>Password Reset Request 🔐</h2>" +
        "<p>Hi <strong>" + name + "</strong>, we received a request to reset your password.</p>" +
        "<p>Click the button below to set a new password:</p>" +
        "<a class='btn' href='" + resetLink + "'>Reset Password</a>" +
        "<p style='margin-top:20px;font-size:14px;color:#666'>This link will expire in 1 hour.</p>" +
        "<p style='font-size:14px;color:#666'>If you didn't request a password reset, you can safely ignore this email.</p>");
}
