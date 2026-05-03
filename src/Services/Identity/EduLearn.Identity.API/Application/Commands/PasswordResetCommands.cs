using MediatR;
using MassTransit;
using EduLearn.Identity.API.Infrastructure.Data;
using EduLearn.Identity.API.Domain.Entities;
using EduLearn.Identity.API.Application.Interfaces;
using EduLearn.Shared.Exceptions;
using EduLearn.Shared.Events;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.Identity.API.Application.Commands;

// ── REQUEST PASSWORD RESET ────────────────────────────────────
public record RequestPasswordResetCommand(string Email) : IRequest;

public class RequestPasswordResetCommandHandler : IRequestHandler<RequestPasswordResetCommand>
{
    private readonly IdentityDbContext _db;
    private readonly IUserRepository _userRepo;
    private readonly IPublishEndpoint _publisher;
    private readonly IConfiguration _config;

    public RequestPasswordResetCommandHandler(
        IdentityDbContext db,
        IUserRepository userRepo,
        IPublishEndpoint publisher,
        IConfiguration config)
    {
        _db = db;
        _userRepo = userRepo;
        _publisher = publisher;
        _config = config;
    }

    public async Task Handle(RequestPasswordResetCommand cmd, CancellationToken ct)
    {
        var user = await _userRepo.GetByEmailAsync(cmd.Email);

        // Don't reveal if email exists or not (security best practice)
        if (user == null)
            return;

        // Generate reset token
        var token = Guid.NewGuid().ToString();
        var resetToken = new PasswordResetToken(user.UserId, token, expiryHours: 1);

        await _db.PasswordResetTokens.AddAsync(resetToken, ct);
        await _db.SaveChangesAsync(ct);

        // Build reset link
        var frontendUrl = _config["Frontend:Url"] ?? "http://localhost:4200";
        var resetLink = $"{frontendUrl}/auth/reset-password?token={token}";

        // Publish event for Notification API to send email
        await _publisher.Publish(new PasswordResetRequestedEvent(
            user.UserId,
            user.FullName,
            user.Email,
            resetLink
        ), ct);
    }
}

// ── RESET PASSWORD ────────────────────────────────────────────
public record ResetPasswordCommand(string Token, string NewPassword) : IRequest;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand>
{
    private readonly IdentityDbContext _db;

    public ResetPasswordCommandHandler(IdentityDbContext db) => _db = db;

    public async Task Handle(ResetPasswordCommand cmd, CancellationToken ct)
    {
        var resetToken = await _db.PasswordResetTokens
            .FirstOrDefaultAsync(t => t.Token == cmd.Token, ct)
            ?? throw new NotFoundException("PasswordResetToken", cmd.Token);

        if (!resetToken.IsValid())
            throw new InvalidOperationException("Reset token has expired or already been used.");

        var user = await _db.Users.FindAsync(resetToken.UserId)
                   ?? throw new NotFoundException("User", resetToken.UserId);

        // Hash the new password
        var hash = BCrypt.Net.BCrypt.HashPassword(cmd.NewPassword, workFactor: 12);
        user.UpdatePassword(hash); // Update password with hashed version
        resetToken.MarkAsUsed();

        await _db.SaveChangesAsync(ct);
    }
}
