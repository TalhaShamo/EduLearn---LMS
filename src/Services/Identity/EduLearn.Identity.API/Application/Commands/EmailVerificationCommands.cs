using MediatR;
using MassTransit;
using EduLearn.Identity.API.Infrastructure.Data;
using EduLearn.Identity.API.Domain.Entities;
using EduLearn.Shared.Exceptions;
using EduLearn.Shared.Events;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.Identity.API.Application.Commands;

// ── SEND EMAIL VERIFICATION ───────────────────────────────────
public record SendEmailVerificationCommand(Guid UserId) : IRequest;

public class SendEmailVerificationCommandHandler : IRequestHandler<SendEmailVerificationCommand>
{
    private readonly IdentityDbContext _db;
    private readonly IPublishEndpoint _publisher;
    private readonly IConfiguration _config;

    public SendEmailVerificationCommandHandler(IdentityDbContext db, IPublishEndpoint publisher, IConfiguration config)
    {
        _db = db;
        _publisher = publisher;
        _config = config;
    }

    public async Task Handle(SendEmailVerificationCommand cmd, CancellationToken ct)
    {
        var user = await _db.Users.FindAsync(cmd.UserId)
                   ?? throw new NotFoundException("User", cmd.UserId);

        if (user.IsVerified)
            throw new InvalidOperationException("Email is already verified.");

        // Generate verification token
        var token = Guid.NewGuid().ToString();
        var verificationToken = new EmailVerificationToken(user.UserId, token, expiryHours: 24);

        await _db.EmailVerificationTokens.AddAsync(verificationToken, ct);
        await _db.SaveChangesAsync(ct);

        // Build verification link
        var frontendUrl = _config["Frontend:Url"] ?? "http://localhost:4200";
        var verificationLink = $"{frontendUrl}/auth/verify-email?token={token}";

        // Publish event for Notification API to send email
        await _publisher.Publish(new EmailVerificationRequestedEvent(
            user.UserId,
            user.FullName,
            user.Email,
            verificationLink
        ), ct);
    }
}

// ── VERIFY EMAIL ──────────────────────────────────────────────
public record VerifyEmailCommand(string Token) : IRequest;

public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand>
{
    private readonly IdentityDbContext _db;

    public VerifyEmailCommandHandler(IdentityDbContext db) => _db = db;

    public async Task Handle(VerifyEmailCommand cmd, CancellationToken ct)
    {
        var verificationToken = await _db.EmailVerificationTokens
            .FirstOrDefaultAsync(t => t.Token == cmd.Token, ct)
            ?? throw new NotFoundException("EmailVerificationToken", cmd.Token);

        if (!verificationToken.IsValid())
            throw new InvalidOperationException("Verification token has expired or already been used.");

        var user = await _db.Users.FindAsync(verificationToken.UserId)
                   ?? throw new NotFoundException("User", verificationToken.UserId);

        user.Verify(); // Mark user as verified
        verificationToken.MarkAsUsed();

        await _db.SaveChangesAsync(ct);
    }
}
