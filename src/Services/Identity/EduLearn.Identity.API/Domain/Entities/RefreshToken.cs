namespace EduLearn.Identity.API.Domain.Entities;

// Refresh token entity — enables silent re-authentication
// Each user can have multiple active refresh tokens (multi-device support)
public class RefreshToken
{
    public Guid     Id         { get; set; } = Guid.NewGuid();
    public Guid     UserId     { get; set; }          // FK → User
    public string   Token      { get; set; } = string.Empty; // SHA-256 hashed token
    public DateTime ExpiresAt  { get; set; }
    public bool     IsRevoked  { get; set; }
    public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;

    // Navigation property back to user
    public User User { get; set; } = null!;

    // Check if this token is still usable
    public bool IsActive => !IsRevoked && ExpiresAt > DateTime.UtcNow;

    // Revoke this token (called on logout or token rotation)
    public void Revoke() => IsRevoked = true;
}
