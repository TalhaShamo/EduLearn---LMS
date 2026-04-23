using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EduLearn.Identity.API.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace EduLearn.Identity.API.Infrastructure.Services;

// JWT service — handles token generation and hashing
// Uses HS256 (symmetric key) for simplicity in this learning project
public class JwtService : IJwtService
{
    private readonly IConfiguration _config;

    public JwtService(IConfiguration config) => _config = config;

    // Generate a signed JWT access token
    public string GenerateAccessToken(Guid userId, string email, string role)
    {
        var secretKey = _config["Jwt:SecretKey"]!;
        var issuer    = _config["Jwt:Issuer"]!;
        var audience  = _config["Jwt:Audience"]!;
        var expiryMin = int.Parse(_config["Jwt:AccessTokenExpiryMinutes"] ?? "15");

        // Create the signing key from the secret
        var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Build claims — these become the token payload (visible to Angular via JWT decode)
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub,   userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(ClaimTypes.Role,               role),
            new(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()), // Unique token ID
            new(JwtRegisteredClaimNames.Iat,   DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
        };

        var token = new JwtSecurityToken(
            issuer:             issuer,
            audience:           audience,
            claims:             claims,
            notBefore:          DateTime.UtcNow,
            expires:            DateTime.UtcNow.AddMinutes(expiryMin),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // Generate a cryptographically random 64-byte token (for refresh token cookie)
    public string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);         // Secure random using File I/O analog
        return Convert.ToBase64String(bytes);
    }

    // SHA-256 hash the raw refresh token before storing in DB
    // Never store raw tokens — if DB is compromised, hashed tokens are useless
    public string HashRefreshToken(string rawToken)
    {
        var bytes = Encoding.UTF8.GetBytes(rawToken);
        var hash  = SHA256.HashData(bytes);         // Built-in .NET crypto
        return Convert.ToBase64String(hash);
    }
}
