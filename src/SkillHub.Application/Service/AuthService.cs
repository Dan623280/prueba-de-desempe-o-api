using SkillHub.Application.DTOs.Auth;
using SkillHub.Application.Exceptions;
using SkillHub.Application.Interfaces.Repositories;
using SkillHub.Application.Interfaces.Services;

namespace SkillHub.Application.Service;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;

    public AuthService(IUserRepository userRepository, ITokenService tokenService)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterUserDto dto, CancellationToken ct)
    {
        var user = await _userRepository.RegisterParticipantAsync(dto, ct);
        var token = _tokenService.Create(user);

        AuthResponseDto responseDto = new AuthResponseDto
        {
            AccessToken = token.AccessToken,
            ExpiresAt = token.ExpiresAt,
            User = user
        };

        return responseDto;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken ct)
    {
        var user = await _userRepository.ValidateCredentialsAsync(dto, ct);

        if (user == null)
        {
            throw new AuthenticationException();
        }

        var token = _tokenService.Create(user);

        AuthResponseDto responseDto = new AuthResponseDto
        {
            AccessToken = token.AccessToken,
            ExpiresAt = token.ExpiresAt,
            User = user
        };

        return responseDto;
    }
}
