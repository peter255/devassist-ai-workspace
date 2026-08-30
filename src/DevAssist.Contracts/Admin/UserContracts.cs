namespace DevAssist.Contracts.Admin;

public sealed record UserDto(
    Guid Id,
    string Username,
    string DisplayName,
    string Role,
    bool IsActive,
    DateTimeOffset CreatedAt);

public sealed record CreateUserRequest(
    string Username,
    string DisplayName,
    string Password,
    string Role = "User");

public sealed record UpdateUserRequest(
    string? DisplayName,
    string? Role,
    bool? IsActive);

public sealed record ResetPasswordRequest(string NewPassword);
