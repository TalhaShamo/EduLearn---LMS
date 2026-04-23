namespace EduLearn.Identity.API.Application.Interfaces;

// JWT service contract — decouples token logic from controllers
public interface IJwtService
{
    // Generate a signed JWT access token for the given user
    string GenerateAccessToken(Guid userId, string email, string role);

    // Generate a secure random refresh token (raw value for cookie)
    string GenerateRefreshToken();

    // Hash a raw refresh token for safe storage in DB
    string HashRefreshToken(string rawToken);
}
