using DevAssist.Application.Interfaces.Auth;
using DevAssist.Domain.Entities;
using DevAssist.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevAssist.Infrastructure.Auth;

public sealed class UserRepository(DevAssistDbContext db) : IUserRepository
{
    public Task<AppUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => db.AppUsers.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<AppUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken)
        => db.AppUsers.FirstOrDefaultAsync(u => u.Username == username.ToLower(), cancellationToken);

    public async Task<IReadOnlyList<AppUser>> GetAllAsync(CancellationToken cancellationToken)
        => await db.AppUsers.OrderBy(u => u.CreatedAt).ToListAsync(cancellationToken);

    public async Task<AppUser> CreateAsync(AppUser user, CancellationToken cancellationToken)
    {
        db.AppUsers.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task UpdateAsync(AppUser user, CancellationToken cancellationToken)
    {
        db.AppUsers.Update(user);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await db.AppUsers.FindAsync([id], cancellationToken);
        if (user is not null)
        {
            db.AppUsers.Remove(user);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public Task<bool> AnyAsync(CancellationToken cancellationToken)
        => db.AppUsers.AnyAsync(cancellationToken);
}
