using System.ComponentModel.DataAnnotations;

namespace SkillHub.Application.DTOs.Auth;

public class RegisterUserDto
{
    [Required(ErrorMessage = "Full name is required."), StringLength(150, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required."), EmailAddress, StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required."), StringLength(128, MinimumLength = 10)]
    public string Password { get; set; } = string.Empty;
}
