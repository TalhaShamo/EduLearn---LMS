using Microsoft.EntityFrameworkCore;
using EduLearn.Identity.API.Application.Interfaces;
using EduLearn.Identity.API.Domain.Entities;
using EduLearn.Identity.API.Infrastructure.Data;

namespace EduLearn.Identity.API.Infrastructure.Repositories;

// Generic repository implementation — satisfies IRepository<T>
// OOP: base class provides common CRUD; specific repos extend it
public abstract class BaseRepository<T> : IRepository<T> where T : class
{
    protected readonly IdentityDbContext _db;

    protected BaseRepository(IdentityDbContext db) => _db = db;

    public async Task<T?> GetByIdAsync(Guid id)         => await _db.Set<T>().FindAsync(id);
    public async Task<IEnumerable<T>> GetAllAsync()     => await _db.Set<T>().ToListAsync();
    public async Task AddAsync(T entity)                 => await _db.Set<T>().AddAsync(entity);
    public void Update(T entity)                         => _db.Set<T>().Update(entity);
    public void Delete(T entity)                         => _db.Set<T>().Remove(entity);
    public async Task SaveChangesAsync()                 => await _db.SaveChangesAsync();
}

// ── USER REPOSITORY ───────────────────────────────────────────
public class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(IdentityDbContext db) : base(db) { }

    // Find user by email (case-insensitive via DB collation)
    public async Task<User?> GetByEmailAsync(string email) =>
        await _db.Users.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());

    // Quick existence check to validate email uniqueness before creating user
    public async Task<bool> EmailExistsAsync(string email) =>
        await _db.Users.AnyAsync(u => u.Email == email.ToLowerInvariant());

    // Paged user list for admin dashboard — uses LINQ Skip/Take (Collections)
    public async Task<IEnumerable<User>> GetAllUsersPagedAsync(int page, int pageSize) =>
        await _db.Users
                 .OrderByDescending(u => u.CreatedAt)
                 .Skip((page - 1) * pageSize)
                 .Take(pageSize)
                 .ToListAsync();
}

// ── REFRESH TOKEN REPOSITORY ──────────────────────────────────
public class RefreshTokenRepository : BaseRepository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(IdentityDbContext db) : base(db) { }

    // Lookup token by its SHA-256 hash value
    public async Task<RefreshToken?> GetByTokenAsync(string tokenHash) =>
        await _db.RefreshTokens
                 .Include(t => t.User)  // Eagerly load user for role claims
                 .FirstOrDefaultAsync(t => t.Token == tokenHash);

    // Revoke all tokens — used on logout (security: invalidate all sessions)
    public async Task RevokeAllUserTokensAsync(Guid userId)
    {
        var tokens = await _db.RefreshTokens
                              .Where(t => t.UserId == userId && !t.IsRevoked)
                              .ToListAsync();

        // Iterate collection and revoke each token (Collections + OOP)
        foreach (var token in tokens)
            token.Revoke();
    }
}
