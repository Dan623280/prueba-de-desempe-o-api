using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SkillHub.Application.Enums;
using SkillHub.Application.Exceptions;
using SkillHub.Application.Interfaces.Services;

namespace SkillHub.Infrastructure.Service;

public class CurrentUserService : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var validUserId = Guid.TryParse(claim, out var userId);

            if (!validUserId)
            {
                throw new AuthenticationException("Authentication is required.");
            }

            return userId;
        }
    }

    public bool IsEmployee
    {
        get
        {
            return _httpContextAccessor.HttpContext?.User.IsInRole(nameof(UserRole.Employee)) == true;
        }
    }

    public bool IsParticipant
    {
        get
        {
            return _httpContextAccessor.HttpContext?.User.IsInRole(nameof(UserRole.Participant)) == true;
        }
    }
}
