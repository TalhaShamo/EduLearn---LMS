using MediatR;
using EduLearn.Identity.API.Application.DTOs;
using EduLearn.Identity.API.Application.Interfaces;
using EduLearn.Identity.API.Domain.Entities;
using EduLearn.Identity.API.Domain.Enums;
using EduLearn.Shared.Exceptions;
using EduLearn.Shared.Events;
using MassTransit;

namespace EduLearn.Identity.API.Application.Commands;

// ── REGISTER COMMAND ──────────────────────────────────────────

// MediatR command — carries all data needed to register a user
public record RegisterCommand(
    string FullName,
    string Email,
    string Password,
    string Role
) : IRequest<UserDto>;   // Returns a UserDto on success

// Handler — orchestrates the registration workflow
public class RegisterCommandHandler : IRequestHandler<RegisterCommand, UserDto>
{
    private readonly IUserRepository _userRepo;
    private readonly IPublishEndpoint _bus;  // MassTransit — to publish UserRegisteredEvent
    private readonly IMediator _mediator;

    public RegisterCommandHandler(IUserRepository userRepo, IPublishEndpoint bus, IMediator mediator)
    {
        _userRepo = userRepo;
        _bus      = bus;
        _mediator = mediator;
    }

    public async Task<UserDto> Handle(RegisterCommand cmd, CancellationToken ct)
    {
        // 1. Check email uniqueness
        if (await _userRepo.EmailExistsAsync(cmd.Email))
            throw new ConflictException($"An account with email '{cmd.Email}' already exists.");

        // 2. Parse role string to enum
        var role = Enum.Parse<UserRole>(cmd.Role, ignoreCase: true);

        // 3. Hash the password with bcrypt (cost factor 12)
        var hash = BCrypt.Net.BCrypt.HashPassword(cmd.Password, workFactor: 12);

        // 4. Create the user entity via factory method (OOP)
        var user = User.Create(cmd.FullName, cmd.Email, hash, role);

        // 5. Persist to DB
        await _userRepo.AddAsync(user);
        await _userRepo.SaveChangesAsync();

        // 6. Publish event to RabbitMQ → Notification.API will send welcome email
        await _bus.Publish(new UserRegisteredEvent(
            user.UserId, user.FullName, user.Email, role.ToString(), user.CreatedAt), ct);

        // 7. Send email verification
        await _mediator.Send(new SendEmailVerificationCommand(user.UserId), ct);

        // 8. Return DTO (never return the entity directly)
        return MapToDto(user);
    }

    private static UserDto MapToDto(User u) =>
        new(u.UserId, u.FullName, u.Email, u.Role.ToString(), u.IsActive, u.IsBanned, u.CreatedAt);
}

// ── LOGIN COMMAND ─────────────────────────────────────────────

public record LoginCommand(string Email, string Password) : IRequest<(AuthResponse Auth, string RawRefreshToken)>;

public class LoginCommandHandler : IRequestHandler<LoginCommand, (AuthResponse, string)>
{
    private readonly IUserRepository       _userRepo;
    private readonly IRefreshTokenRepository _tokenRepo;
    private readonly IJwtService           _jwt;

    public LoginCommandHandler(IUserRepository userRepo, IRefreshTokenRepository tokenRepo, IJwtService jwt)
    {
        _userRepo  = userRepo;
        _tokenRepo = tokenRepo;
        _jwt       = jwt;
    }

    public async Task<(AuthResponse, string)> Handle(LoginCommand cmd, CancellationToken ct)
    {
        // 1. Find user by email
        var user = await _userRepo.GetByEmailAsync(cmd.Email)
                   ?? throw new UnauthorizedAccessException("Invalid credentials.");

        // 2. Verify password against bcrypt hash
        if (!BCrypt.Net.BCrypt.Verify(cmd.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        // 3. Check account status
        if (user.IsBanned) throw new ForbiddenException("Your account has been suspended.");
        if (!user.IsActive) throw new ForbiddenException("Your account is inactive.");
        if (!user.IsVerified) throw new ForbiddenException("Please verify your email before logging in. Check your inbox for the verification link.");

        // 4. Generate access token (15-min JWT)
        var accessToken = _jwt.GenerateAccessToken(user.UserId, user.Email, user.Role.ToString());

        // 5. Generate and store refresh token (7-day)
        var rawRefreshToken = _jwt.GenerateRefreshToken();
        var refreshToken = new RefreshToken
        {
            UserId    = user.UserId,
            Token     = _jwt.HashRefreshToken(rawRefreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        await _tokenRepo.AddAsync(refreshToken);

        // 6. Record last login time on the user entity
        user.RecordLogin();
        _userRepo.Update(user);
        await _userRepo.SaveChangesAsync();

        var authResponse = new AuthResponse(
            AccessToken: accessToken,
            ExpiresInSeconds: 900,
            User: new UserDto(user.UserId, user.FullName, user.Email, user.Role.ToString(), user.IsActive, user.IsBanned, user.CreatedAt)
        );

        return (authResponse, rawRefreshToken);
    }
}

// ── REFRESH TOKEN COMMAND ─────────────────────────────────────

public record RefreshTokenCommand(string RawRefreshToken) : IRequest<(AuthResponse Auth, string NewRawToken)>;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, (AuthResponse, string)>
{
    private readonly IRefreshTokenRepository _tokenRepo;
    private readonly IUserRepository         _userRepo;
    private readonly IJwtService             _jwt;

    public RefreshTokenCommandHandler(IRefreshTokenRepository tokenRepo, IUserRepository userRepo, IJwtService jwt)
    {
        _tokenRepo = tokenRepo;
        _userRepo  = userRepo;
        _jwt       = jwt;
    }

    public async Task<(AuthResponse, string)> Handle(RefreshTokenCommand cmd, CancellationToken ct)
    {
        // 1. Hash the incoming token to look it up in DB
        var hash  = _jwt.HashRefreshToken(cmd.RawRefreshToken);
        var token = await _tokenRepo.GetByTokenAsync(hash)
                    ?? throw new UnauthorizedAccessException("Refresh token is invalid or expired.");

        if (!token.IsActive)
            throw new UnauthorizedAccessException("Refresh token has expired or been revoked.");

        // 2. Rotate: revoke old token
        token.Revoke();
        _tokenRepo.Update(token);

        // 3. Load user
        var user = await _userRepo.GetByIdAsync(token.UserId)
                   ?? throw new NotFoundException("User", token.UserId);

        // 4. Issue new access + refresh token
        var newAccessToken = _jwt.GenerateAccessToken(user.UserId, user.Email, user.Role.ToString());
        var rawNewRefresh  = _jwt.GenerateRefreshToken();
        var newToken = new RefreshToken
        {
            UserId    = user.UserId,
            Token     = _jwt.HashRefreshToken(rawNewRefresh),
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        await _tokenRepo.AddAsync(newToken);
        await _tokenRepo.SaveChangesAsync();

        var authResponse = new AuthResponse(
            newAccessToken, 900,
            new UserDto(user.UserId, user.FullName, user.Email, user.Role.ToString(), user.IsActive, user.IsBanned, user.CreatedAt)
        );

        return (authResponse, rawNewRefresh);
    }
}

// ── LOGOUT COMMAND ────────────────────────────────────────────

public record LogoutCommand(Guid UserId) : IRequest;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IRefreshTokenRepository _tokenRepo;

    public LogoutCommandHandler(IRefreshTokenRepository tokenRepo) => _tokenRepo = tokenRepo;

    public async Task Handle(LogoutCommand cmd, CancellationToken ct)
    {
        // Revoke all refresh tokens for this user across all devices
        await _tokenRepo.RevokeAllUserTokensAsync(cmd.UserId);
        await _tokenRepo.SaveChangesAsync();
    }
}
