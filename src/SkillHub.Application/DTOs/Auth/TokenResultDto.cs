namespace SkillHub.Application.DTOs.Auth;

public class TokenResultDto
{
    public string AccessToken { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }
}
