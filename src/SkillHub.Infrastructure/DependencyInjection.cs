using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using PdfSharp.Fonts;
using SkillHub.Application.Interfaces.Repositories;
using SkillHub.Application.Interfaces.Services;
using SkillHub.Infrastructure.Identity;
using SkillHub.Infrastructure.Persistence;
using SkillHub.Infrastructure.Persistence.Repository;
using SkillHub.Infrastructure.Persistence.Repository.External;
using SkillHub.Infrastructure.Service;

namespace SkillHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Set ConnectionStrings:DefaultConnection before starting the API.");
        services.AddDbContext<SkillHubDbContext>(options => options.UseNpgsql(connection,
            postgres => postgres.MigrationsAssembly(typeof(SkillHubDbContext).Assembly.FullName)));
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = 10;
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        }).AddRoles<IdentityRole<Guid>>().AddEntityFrameworkStores<SkillHubDbContext>();
        var jwt = configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();
        if (Encoding.UTF8.GetByteCount(jwt.Key) < 32)
        {
            throw new InvalidOperationException("Set Jwt:Key to a secret of at least 32 bytes.");
        }

        if (string.IsNullOrWhiteSpace(jwt.Issuer) || string.IsNullOrWhiteSpace(jwt.Audience) || jwt.ExpirationMinutes is < 1 or > 1440)
        {
            throw new InvalidOperationException("Configure a JWT issuer, audience and expiration from 1 to 1440 minutes.");
        }

        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwt.Issuer,
                ValidAudience = jwt.Audience,
                ClockSkew = TimeSpan.Zero,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
            };
        });
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUserService>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IWorkshopRepository, WorkshopRepository>();
        services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
        services.AddScoped<IEnrollmentTransaction, EnrollmentTransaction>();
        services.AddScoped<ITokenService, JwtService>();
        services.AddSingleton<ExcelWorkbookService>();
        services.AddSingleton<PdfCertificateService>();
        services.AddScoped<IExcelService, ExcelRepository>();
        services.AddScoped<ICertificateService, CertificateRepository>();
        services.AddScoped<DatabaseInitializer>();
        GlobalFontSettings.FontResolver ??= new CertificateFontResolver();
        return services;
    }
}
