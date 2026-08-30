using System.Security.Claims;
using DevAssist.Application.Interfaces.Auth;
using DevAssist.Domain.Enums;

namespace DevAssist.Api.Services;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var sub = Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? Principal?.FindFirstValue("sub");
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public string? Username => Principal?.FindFirstValue(ClaimTypes.Name)
                             ?? Principal?.FindFirstValue("unique_name");

    public UserRole? Role
    {
        get
        {
            var roleClaim = Principal?.FindFirstValue(ClaimTypes.Role);
            return Enum.TryParse<UserRole>(roleClaim, out var role) ? role : null;
        }
    }

    public bool IsAdmin => Role == UserRole.Admin;
}
