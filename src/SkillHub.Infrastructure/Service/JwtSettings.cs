namespace SkillHub.Infrastructure.Service;

public class JwtSettings
{
    public string Issuer { get; set; } = "SkillHub";
    public string Audience { get; set; } = "SkillHub.Client";
    public string Key { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 60;
}
