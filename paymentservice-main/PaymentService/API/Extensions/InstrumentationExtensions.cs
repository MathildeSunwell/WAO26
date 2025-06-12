using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Sdk = OpenTelemetry.Sdk;

namespace PaymentService.API.Extensions;

public static class InstrumentationExtensions
{
    public static IServiceCollection AddInstrumentation(this IServiceCollection services, string serviceName)
    {
        Sdk.SetDefaultTextMapPropagator(new CompositeTextMapPropagator([
            new TraceContextPropagator(),
            new BaggagePropagator()
        ]));

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: serviceName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation(opts =>
                {
                    opts.FilterHttpRequestMessage = req => !req.RequestUri!.Host.Contains("health");
                })
                .AddRabbitMQInstrumentation()
                .AddSqlClientInstrumentation(opts =>
                {
                    opts.RecordException = true;
                    opts.SetDbStatementForText = true;
                })
                .AddEntityFrameworkCoreInstrumentation()
                .AddConsoleExporter());

        return services;
    }
}
