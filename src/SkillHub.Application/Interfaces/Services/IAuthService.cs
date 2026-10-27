using SkillHub.Application.DTOs.Auth;

namespace SkillHub.Application.Interfaces.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterUserDto request, CancellationToken ct);
    Task<AuthResponseDto> LoginAsync(LoginDto request, CancellationToken ct);
}
