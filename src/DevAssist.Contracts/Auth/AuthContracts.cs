namespace DevAssist.Contracts.Auth;

public sealed record LoginRequest(string Username, string Password);

public sealed record LoginResponse(
    string Token,
    string Username,
    string DisplayName,
    string Role,
    DateTimeOffset ExpiresAt);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
