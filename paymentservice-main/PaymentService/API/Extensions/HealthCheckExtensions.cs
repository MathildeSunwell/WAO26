using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;

namespace PaymentService.API.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddCustomHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddCheck(
                "LivenessHealthCheck",
                () => HealthCheckResult.Healthy("The API is healthy"),
                new[] { "liveness" })
            .AddSqlServer(
                configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException(),
                name: "SQL Server",
                tags: new[] { "readiness" })
            .AddRabbitMQ(
                (Func<IServiceProvider,IConnection>)(sp => sp.GetRequiredService<IConnection>()),
                name: "RabbitMQ",
                tags: new[] { "readiness" });

        return services;
    }

    public static IApplicationBuilder UseCustomHealthChecks(this IApplicationBuilder app)
    {
        app.UseHealthChecks("/health/live", 
        new HealthCheckOptions
        {
            Predicate = x => x.Tags.Contains("liveness")
        });
        
        app.UseHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = hc => hc.Tags.Contains("readiness"),
        });

        return app;
    }
}