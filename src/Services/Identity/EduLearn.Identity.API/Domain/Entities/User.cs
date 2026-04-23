using EduLearn.Identity.API.Domain.Enums;

namespace EduLearn.Identity.API.Domain.Entities;

// Core user entity — persisted to EduLearnIdentityDb
// OOP: encapsulates business rules (Update, Deactivate) inside the entity itself
public class User
{
    public Guid     UserId        { get; private set; } = Guid.NewGuid();
    public string   FullName      { get; private set; } = string.Empty;
    public string   Email         { get; private set; } = string.Empty;
    public string   PasswordHash  { get; private set; } = string.Empty; // bcrypt hash
    public UserRole Role          { get; private set; }
    public bool     IsVerified    { get; private set; }   // Email verified?
    public bool     IsActive      { get; private set; } = true;
    public bool     IsBanned      { get; private set; }
    public DateTime CreatedAt     { get; private set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt  { get; private set; }

    // Refresh tokens stored as a collection (EF Core navigation property)
    public ICollection<RefreshToken> RefreshTokens { get; private set; } = new List<RefreshToken>();

    // Private constructor — EF Core requires a parameterless constructor
    private User() { }

    // Factory method — enforces required fields when creating a new user
    public static User Create(string fullName, string email, string passwordHash, UserRole role)
    {
        return new User
        {
            FullName     = fullName.Trim(),
            Email        = email.ToLowerInvariant().Trim(),
            PasswordHash = passwordHash,
            Role         = role,
            IsVerified   = true // Simplified: auto-verify for this learning project
        };
    }

    // Domain behaviour: update profile info
    public void UpdateProfile(string fullName)
    {
        FullName = fullName.Trim();
    }

    // Domain behaviour: record last login timestamp
    public void RecordLogin() => LastLoginAt = DateTime.UtcNow;

    // Domain behaviour: admin deactivates account
    public void Deactivate() => IsActive = false;

    // Domain behaviour: admin bans account permanently
    public void Ban()
    {
        IsBanned = true;
        IsActive = false;
    }

    // Domain behaviour: admin re-activates account
    public void Activate()
    {
        IsBanned = false;
        IsActive = true;
    }
}
