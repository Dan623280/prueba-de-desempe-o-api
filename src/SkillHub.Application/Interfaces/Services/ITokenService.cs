using SkillHub.Application.DTOs.Auth;

namespace SkillHub.Application.Interfaces.Services;

public interface ITokenService
{
    TokenResultDto Create(UserDto user);
}
