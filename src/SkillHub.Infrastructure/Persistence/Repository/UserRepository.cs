using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SkillHub.Application.DTOs.Auth;
using SkillHub.Application.Enums;
using SkillHub.Application.Exceptions;
using SkillHub.Application.Interfaces.Repositories;
using SkillHub.Domain.Exceptions;
using SkillHub.Infrastructure.Identity;

namespace SkillHub.Infrastructure.Persistence.Repository;

public class UserRepository : IUserRepository
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SkillHubDbContext _context;

    public UserRepository(UserManager<ApplicationUser> userManager, SkillHubDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public async Task<UserDto> RegisterParticipantAsync(RegisterUserDto dto, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var email = dto.Email.Trim().ToLowerInvariant();
        var existingUser = await _userManager.FindByEmailAsync(email);

        if (existingUser != null)
        {
            throw new BusinessRuleException("This email is already registered.");
        }

        ApplicationUser user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FullName = dto.FullName.Trim(),
            Email = email,
            UserName = email
        };

        // Save the account and its participant role together. Clients cannot choose an employee role.
        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        var createResult = await _userManager.CreateAsync(user, dto.Password);

        if (!createResult.Succeeded)
        {
            throw new ValidationException(string.Join(" ", createResult.Errors.Select(error => error.Description)));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, nameof(UserRole.Participant));

        if (!roleResult.Succeeded)
        {
            throw new ValidationException(string.Join(" ", roleResult.Errors.Select(error => error.Description)));
        }

        await transaction.CommitAsync(ct);

        UserDto responseDto = new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = email,
            Role = nameof(UserRole.Participant)
        };

        return responseDto;
    }

    public async Task<UserDto?> ValidateCredentialsAsync(LoginDto dto, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var user = await _userManager.FindByEmailAsync(dto.Email.Trim());

        if (user == null || await _userManager.IsLockedOutAsync(user))
        {
            return null;
        }

        var validPassword = await _userManager.CheckPasswordAsync(user, dto.Password);

        if (!validPassword)
        {
            await _userManager.AccessFailedAsync(user);
            return null;
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        var responseDto = await ToDtoAsync(user);
        return responseDto;
    }

    public async Task<UserDto?> GetAsync(Guid id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var user = await _userManager.FindByIdAsync(id.ToString());

        if (user == null)
        {
            return null;
        }

        var responseDto = await ToDtoAsync(user);
        return responseDto;
    }

    public async Task<UserDto?> GetParticipantByEmailAsync(string email, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var user = await _userManager.FindByEmailAsync(email.Trim());

        if (user == null)
        {
            return null;
        }

        var isParticipant = await _userManager.IsInRoleAsync(user, nameof(UserRole.Participant));

        if (!isParticipant)
        {
            return null;
        }

        var responseDto = await ToDtoAsync(user);
        return responseDto;
    }

    private async Task<UserDto> ToDtoAsync(ApplicationUser user)
    {
        var isEmployee = await _userManager.IsInRoleAsync(user, nameof(UserRole.Employee));
        var role = nameof(UserRole.Participant);

        if (isEmployee)
        {
            role = nameof(UserRole.Employee);
        }

        UserDto responseDto = new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email!,
            Role = role
        };

        return responseDto;
    }
}
