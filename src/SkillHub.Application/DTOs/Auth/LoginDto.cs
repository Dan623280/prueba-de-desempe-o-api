using System.ComponentModel.DataAnnotations;

namespace SkillHub.Application.DTOs.Auth;

public class LoginDto
{
    [Required(ErrorMessage = "Email is required."), EmailAddress, StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required."), StringLength(128)]
    public string Password { get; set; } = string.Empty;
}
