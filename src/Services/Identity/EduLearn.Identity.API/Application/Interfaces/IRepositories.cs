using EduLearn.Identity.API.Domain.Entities;

namespace EduLearn.Identity.API.Application.Interfaces;

// Generic repository contract — Repository Pattern
// T must be a class so EF Core can track it
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task AddAsync(T entity);
    void Update(T entity);
    void Delete(T entity);
    Task SaveChangesAsync();
}

// User-specific repository — extends generic with user-specific queries
public interface IUserRepository : IRepository<User>
{
    Task<User?>  GetByEmailAsync(string email);
    Task<bool>   EmailExistsAsync(string email);
    Task<IEnumerable<User>> GetAllUsersPagedAsync(int page, int pageSize);
}

// Refresh token repository
public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> GetByTokenAsync(string token);       // Find token by hash
    Task RevokeAllUserTokensAsync(Guid userId);              // Revoke on logout
}
