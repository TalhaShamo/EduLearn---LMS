namespace EduLearn.Identity.API.Domain.Entities;

// Email verification token - sent when user registers
public class EmailVerificationToken
{
    public Guid TokenId { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; private set; }
    public bool IsUsed { get; private set; }

    // EF Core constructor
    private EmailVerificationToken() { }

    public EmailVerificationToken(Guid userId, string token, int expiryHours = 24)
    {
        UserId = userId;
        Token = token;
        ExpiresAt = DateTime.UtcNow.AddHours(expiryHours);
        IsUsed = false;
    }

    public bool IsValid() => !IsUsed && DateTime.UtcNow < ExpiresAt;

    public void MarkAsUsed() => IsUsed = true;
}
