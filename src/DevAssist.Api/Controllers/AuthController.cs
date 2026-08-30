using DevAssist.Application.Interfaces.Auth;
using DevAssist.Contracts.Auth;
using DevAssist.Contracts.Common;
using DevAssist.Domain.Entities;
using DevAssist.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevAssist.Api.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public sealed class AuthController(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    ICurrentUserService currentUser,
    IConfiguration configuration) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByUsernameAsync(request.Username, cancellationToken);
        if (user is null || !user.IsActive || !passwordHasher.VerifyPassword(request.Password, user.PasswordHash, user.PasswordSalt))
            return Unauthorized(ApiResponse<LoginResponse>.Fail("Invalid username or password."));

        var token = jwtTokenService.GenerateToken(user);
        var expiryMinutes = configuration.GetValue<int>("Jwt:ExpiryMinutes", 480);

        return Ok(ApiResponse<LoginResponse>.Ok(new LoginResponse(
            token,
            user.Username,
            user.DisplayName,
            user.Role.ToString(),
            DateTimeOffset.UtcNow.AddMinutes(expiryMinutes))));
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Unauthorized(ApiResponse<object>.Fail("Not authenticated."));

        var user = await userRepository.GetByIdAsync(currentUser.UserId.Value, cancellationToken);
        if (user is null)
            return NotFound(ApiResponse<object>.Fail("User not found."));

        if (!passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash, user.PasswordSalt))
            return BadRequest(ApiResponse<object>.Fail("Current password is incorrect."));

        var (hash, salt) = passwordHasher.HashPassword(request.NewPassword);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        await userRepository.UpdateAsync(user, cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { }));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Me(CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Unauthorized(ApiResponse<LoginResponse>.Fail("Not authenticated."));

        var user = await userRepository.GetByIdAsync(currentUser.UserId.Value, cancellationToken);
        if (user is null)
            return NotFound(ApiResponse<LoginResponse>.Fail("User not found."));

        var token = jwtTokenService.GenerateToken(user);
        var expiryMinutes = configuration.GetValue<int>("Jwt:ExpiryMinutes", 480);

        return Ok(ApiResponse<LoginResponse>.Ok(new LoginResponse(
            token,
            user.Username,
            user.DisplayName,
            user.Role.ToString(),
            DateTimeOffset.UtcNow.AddMinutes(expiryMinutes))));
    }
}
