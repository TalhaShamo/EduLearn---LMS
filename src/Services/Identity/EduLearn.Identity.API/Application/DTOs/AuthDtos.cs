namespace EduLearn.Identity.API.Application.DTOs;

// ── REQUEST DTOs ─────────────────────────────────────────────

// POST /api/v1/auth/register
public record RegisterRequest(
    string FullName,
    string Email,
    string Password,
    string Role     // "Student" or "Instructor"
);

// POST /api/v1/auth/login
public record LoginRequest(
    string Email,
    string Password
);

// POST /api/v1/auth/refresh-token
public record RefreshTokenRequest(
    string RefreshToken
);

// ── RESPONSE DTOs ────────────────────────────────────────────

// Returned on successful login or token refresh
public record AuthResponse(
    string AccessToken,
    int    ExpiresInSeconds,  // 900 (15 min)
    UserDto User
);

// User summary — returned inside AuthResponse and GET /users
public record UserDto(
    Guid   UserId,
    string FullName,
    string Email,
    string Role,
    bool   IsActive,
    bool   IsBanned,
    DateTime CreatedAt
);
