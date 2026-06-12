namespace PRM.Application.DTOs.Auth;

public record LoginRequestDto(string Username, string Password);

public record LoginResponseDto(
    string Token,
    bool ForcePasswordChange,
    string RoleName,
    string FullName,
    int UserId,
    int EmployeeId);

public record ChangePasswordDto(string NewPassword, string ConfirmPassword);

public record ResetPasswordDto(string NewPassword);
