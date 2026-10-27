using Microsoft.OpenApi;
using SkillHub.Api.Middleware;
using SkillHub.Api.Swagger;
using SkillHub.Application;
using SkillHub.Infrastructure;

namespace SkillHub.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddApplication().AddInfrastructure(configuration);
        services.AddControllers();
        services.AddAuthorization();
        services.AddProblemDetails();
        services.AddExceptionHandler<ApiExceptionHandler>();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "SkillHub API", Version = "v1", Description = "Workshop scheduling and participant enrollment management." });
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Paste the accessToken returned by login or registration."
            });
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = []
            });
            options.OperationFilter<PublicEndpointsFilter>();
        });
        return services;
    }
}
