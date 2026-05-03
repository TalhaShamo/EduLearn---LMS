using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Logging;

namespace EduLearn.Notification.API.Services;

// Email service using MailKit — sends via MailHog (local SMTP) in dev
// Change appsettings SMTP host to production SMTP for live deployment
public class EmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    { _config = config; _logger = logger; }

    // Core send method — all notification consumers call this
    public async Task SendAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            _config["Email:SenderName"]!, _config["Email:SenderEmail"]!));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = subject;

        // Build HTML body using MimeKit's BodyBuilder
        var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
        message.Body    = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();

        var smtpHost = _config["Email:SmtpHost"]!;
        var smtpPort = int.Parse(_config["Email:SmtpPort"]!);
        var smtpUsername = _config["Email:SmtpUsername"];
        var smtpPassword = _config["Email:SmtpPassword"];

        // Determine security options based on port and configuration
        var secureSocketOptions = smtpPort == 465 
            ? SecureSocketOptions.SslOnConnect  // Port 465 uses SSL
            : smtpPort == 587 
                ? SecureSocketOptions.StartTls  // Port 587 uses STARTTLS
                : SecureSocketOptions.None;     // Port 1025 (MailHog) uses no encryption

        await client.ConnectAsync(smtpHost, smtpPort, secureSocketOptions);

        // Authenticate if credentials are provided (Gmail, SendGrid, etc.)
        if (!string.IsNullOrEmpty(smtpUsername) && !string.IsNullOrEmpty(smtpPassword))
        {
            await client.AuthenticateAsync(smtpUsername, smtpPassword);
        }

        await client.SendAsync(message);
        await client.DisconnectAsync(true);

        _logger.LogInformation("Email sent to {Email}: {Subject}", toEmail, subject);
    }
}
