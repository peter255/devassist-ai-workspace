using DevAssist.Domain.Entities;
using DevAssist.Domain.Enums;

namespace DevAssist.Application.Interfaces.Auth;

public interface IPasswordHasher
{
    (string hash, string salt) HashPassword(string password);
    bool VerifyPassword(string password, string hash, string salt);
}

public interface IJwtTokenService
{
    string GenerateToken(AppUser user);
}

public interface IUserRepository
{
    Task<AppUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<AppUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken);
    Task<IReadOnlyList<AppUser>> GetAllAsync(CancellationToken cancellationToken);
    Task<AppUser> CreateAsync(AppUser user, CancellationToken cancellationToken);
    Task UpdateAsync(AppUser user, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> AnyAsync(CancellationToken cancellationToken);
}

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Username { get; }
    UserRole? Role { get; }
    bool IsAdmin { get; }
}
