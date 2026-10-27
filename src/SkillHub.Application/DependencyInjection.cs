using Microsoft.Extensions.DependencyInjection;
using SkillHub.Application.Interfaces.Services;
using SkillHub.Application.Service;
using SkillHub.Domain;

namespace SkillHub.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddDomain();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IWorkshopService, WorkshopService>();
        services.AddScoped<IEnrollmentService, EnrollmentService>();
        services.AddScoped<IReportService, ReportService>();
        return services;
    }
}
