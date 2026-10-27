using SkillHub.Application.DTOs.Auth;

namespace SkillHub.Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task<UserDto> RegisterParticipantAsync(RegisterUserDto request, CancellationToken ct);
    Task<UserDto?> ValidateCredentialsAsync(LoginDto request, CancellationToken ct);
    Task<UserDto?> GetAsync(Guid id, CancellationToken ct);
    Task<UserDto?> GetParticipantByEmailAsync(string email, CancellationToken ct);
}
