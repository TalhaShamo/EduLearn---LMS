using FluentAssertions;
using EduLearn.Identity.API.Infrastructure.Services;
using Microsoft.Extensions.Configuration;

namespace EduLearn.Identity.Tests;

// Unit tests for JwtService
public class JwtServiceTests
{
    // Build a minimal IConfiguration for testing
    private static JwtService CreateService()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"]               = "EduLearn_SuperSecret_JWT_Key_2024_MustBe32Chars!!",
                ["Jwt:Issuer"]                  = "EduLearn.Identity",
                ["Jwt:Audience"]                = "EduLearn.Clients",
                ["Jwt:AccessTokenExpiryMinutes"] = "15"
            })
            .Build();

        return new JwtService(config);
    }

    [Fact]
    public void GenerateAccessToken_ShouldReturnNonEmptyJwt()
    {
        var service = CreateService();
        var token   = service.GenerateAccessToken(Guid.NewGuid(), "test@example.com", "Student");

        token.Should().NotBeNullOrEmpty();
        // JWT format: three base64-encoded segments separated by dots
        token.Split('.').Should().HaveCount(3);
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturn64ByteBase64String()
    {
        var service = CreateService();
        var token   = service.GenerateRefreshToken();
        var bytes   = Convert.FromBase64String(token);

        bytes.Should().HaveCount(64); // 64 random bytes
    }

    [Fact]
    public void HashRefreshToken_ShouldReturnDifferentStringThanInput()
    {
        var service = CreateService();
        var raw     = service.GenerateRefreshToken();
        var hashed  = service.HashRefreshToken(raw);

        hashed.Should().NotBe(raw);        // Hash ≠ original
        hashed.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void HashRefreshToken_ShouldBeDeterministic()
    {
        // Same input should always produce the same hash (SHA-256 is deterministic)
        var service  = CreateService();
        var raw      = "some-fixed-token";
        var hash1    = service.HashRefreshToken(raw);
        var hash2    = service.HashRefreshToken(raw);

        hash1.Should().Be(hash2);
    }
}
