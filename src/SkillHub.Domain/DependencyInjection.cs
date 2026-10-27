using Microsoft.Extensions.DependencyInjection;

namespace SkillHub.Domain;

public static class DependencyInjection
{
    // Domain entities are created by factories; the clock is shared by application services.
    public static IServiceCollection AddDomain(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        return services;
    }
}
