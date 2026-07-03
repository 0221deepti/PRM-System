using System.ComponentModel.DataAnnotations;

namespace PRM.Application.DTOs.Auth;

public record LoginRequestDto(
    [Required(ErrorMessage = "Username is required.")] 
    [StringLength(100, ErrorMessage = "Username cannot exceed 100 characters.")] 
    string Username,

    [Required(ErrorMessage = "Password is required.")] 
    [StringLength(100, ErrorMessage = "Password cannot exceed 100 characters.")] 
    string Password);

public record LoginResponseDto(
    string Token,
    bool ForcePasswordChange,
    string RoleName,
    string FullName,
    int UserId,
    int EmployeeId);

public record ChangePasswordDto(
    [Required(ErrorMessage = "New password is required.")] 
    [StringLength(100, MinimumLength = 8, ErrorMessage = "New password must be at least 8 characters.")] 
    string NewPassword,

    [Required(ErrorMessage = "Confirm password is required.")] 
    [StringLength(100, ErrorMessage = "Confirm password cannot exceed 100 characters.")] 
    string ConfirmPassword);

public record ResetPasswordDto(
    [Required(ErrorMessage = "New password is required.")] 
    [StringLength(100, MinimumLength = 8, ErrorMessage = "New password must be at least 8 characters.")] 
    string NewPassword);
