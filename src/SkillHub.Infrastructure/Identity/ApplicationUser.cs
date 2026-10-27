using Microsoft.AspNetCore.Identity;

namespace SkillHub.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
}
