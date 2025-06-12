using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OrderTrackingService.Application;

namespace OrderTrackingService.API.Extensions;

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
    
    /// <summary>
    /// Starts and enriches a W3C‐based Server <see cref="Activity"/> for every incoming HTTP request.
    /// </summary>
    public static IApplicationBuilder UseHttpServerTracing(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            using var activity = TracingHelper.StartServerActivity(context.Request);

            try
            {
                await next();

                if (activity is not null)
                {
                    activity.SetTag("http.status_code", context.Response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                if (activity is not null)
                {
                    activity.SetTag("otel.status_code", "ERROR");
                    activity.SetTag("otel.status_description", ex.Message);
                }
                throw;
            }
        });
    }
}