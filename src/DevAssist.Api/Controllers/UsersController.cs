using DevAssist.Application.Interfaces.Auth;
using DevAssist.Contracts.Admin;
using DevAssist.Contracts.Common;
using DevAssist.Domain.Entities;
using DevAssist.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevAssist.Api.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "Admin")]
public sealed class UsersController(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UserDto>>>> GetAll(CancellationToken cancellationToken)
    {
        var users = await userRepository.GetAllAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<UserDto>>.Ok(users.Select(ToDto).ToList()));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserDto>>> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Length < 3)
            return BadRequest(ApiResponse<UserDto>.Fail("Username must be at least 3 characters."));

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            return BadRequest(ApiResponse<UserDto>.Fail("Password must be at least 6 characters."));

        var existing = await userRepository.GetByUsernameAsync(request.Username, cancellationToken);
        if (existing is not null)
            return Conflict(ApiResponse<UserDto>.Fail($"Username '{request.Username}' is already taken."));

        if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var role))
            role = UserRole.User;

        var (hash, salt) = passwordHasher.HashPassword(request.Password);

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Username = request.Username.ToLower().Trim(),
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName)
                ? request.Username
                : request.DisplayName.Trim(),
            PasswordHash = hash,
            PasswordSalt = salt,
            Role = role,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await userRepository.CreateAsync(user, cancellationToken);
        return CreatedAtAction(nameof(GetAll), ApiResponse<UserDto>.Ok(ToDto(user)));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> Update(
        Guid id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(id, cancellationToken);
        if (user is null)
            return NotFound(ApiResponse<UserDto>.Fail("User not found."));

        if (!string.IsNullOrWhiteSpace(request.DisplayName))
            user.DisplayName = request.DisplayName.Trim();

        if (!string.IsNullOrWhiteSpace(request.Role) &&
            Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var newRole))
            user.Role = newRole;

        if (request.IsActive.HasValue)
            user.IsActive = request.IsActive.Value;

        await userRepository.UpdateAsync(user, cancellationToken);
        return Ok(ApiResponse<UserDto>.Ok(ToDto(user)));
    }

    [HttpPost("{id:guid}/reset-password")]
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword(
        Guid id,
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
            return BadRequest(ApiResponse<object>.Fail("Password must be at least 6 characters."));

        var user = await userRepository.GetByIdAsync(id, cancellationToken);
        if (user is null)
            return NotFound(ApiResponse<object>.Fail("User not found."));

        var (hash, salt) = passwordHasher.HashPassword(request.NewPassword);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        await userRepository.UpdateAsync(user, cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { }));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(id, cancellationToken);
        if (user is null)
            return NotFound(ApiResponse<object>.Fail("User not found."));

        await userRepository.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    private static UserDto ToDto(AppUser u) => new(u.Id, u.Username, u.DisplayName, u.Role.ToString(), u.IsActive, u.CreatedAt);
}
