using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SkillHub.Application.DTOs.Auth;
using SkillHub.Application.Interfaces.Services;

namespace SkillHub.Infrastructure.Service;

public class JwtService : ITokenService
{
    private readonly IOptions<JwtSettings> _jwtSettings;
    private readonly TimeProvider _timeProvider;

    public JwtService(IOptions<JwtSettings> jwtSettings, TimeProvider timeProvider)
    {
        _jwtSettings = jwtSettings;
        _timeProvider = timeProvider;
    }

    public TokenResultDto Create(UserDto user)
    {
        var settings = _jwtSettings.Value;
        var now = _timeProvider.GetUtcNow();
        var expiresAt = now.AddMinutes(settings.ExpirationMinutes);

        // The user id and role are signed claims used by authorization and ownership checks.
        List<Claim> claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        TokenResultDto responseDto = new TokenResultDto
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAt = expiresAt
        };

        return responseDto;
    }
}
