using System.Diagnostics;
using Serilog;
using Serilog.Context;
using Serilog.Enrichers.Span;
using Serilog.Filters;

namespace OrderTrackingService.API.Extensions;

public static class SerilogExtensions
{
    private const string CorrelationHeader = "X-Correlation-ID";

    public static WebApplicationBuilder AddSerilogLogging(this WebApplicationBuilder builder)
    {
        var configuration = builder.Configuration;

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Filter.ByExcluding(Matching.WithProperty<string>(
                "RequestPath",
                p => p.StartsWith("/health/live") || p.StartsWith("/health/ready")))
            .Enrich.FromLogContext()
            .Enrich.WithSpan()
            .Enrich.WithProperty("Application", typeof(SerilogExtensions).Assembly.GetName().Name)
            .WriteTo.Console(
                outputTemplate: 
                  "[{Timestamp:yyyy-MM-dd HH:mm:ss} {CorrelationId} {Level:u3}] {Message:lj}  {Properties}{NewLine}{Exception}")
            .CreateLogger();

        builder.Host.UseSerilog();

        return builder;
    }

    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            var correlationId = context.Request.Headers.TryGetValue(CorrelationHeader, out var existing)
                         ? existing.ToString()
                         : Guid.NewGuid().ToString();
            
            string traceParent;
            if (context.Request.Headers.TryGetValue("traceparent", out var tpHeader))
            {
                traceParent = tpHeader.ToString();
            }
            else
            {
                traceParent = CreateTraceParentHeader();
                
                var activity = new Activity("Incoming HTTP Request")
                    .SetIdFormat(ActivityIdFormat.W3C)
                    .SetParentId(traceParent);
                activity.Start();
            }

            context.Response.OnStarting(() =>
            {
                context.Response.Headers[CorrelationHeader] = correlationId;
                context.Response.Headers.TraceParent = traceParent;
                return Task.CompletedTask;
            });

            using (LogContext.PushProperty("CorrelationId", correlationId))
            using (LogContext.PushProperty("TraceParent", traceParent))
            using (LogContext.PushProperty("RequestPath", context.Request.Path.Value))
            {
                await next();
            }
        });

        return app;
    }
    
    private static string CreateTraceParentHeader()
    {
        var traceId = ActivityTraceId.CreateRandom().ToHexString();
        var spanId = ActivitySpanId.CreateRandom().ToHexString();
        const string flags = "01";
        return $"00-{traceId}-{spanId}-{flags}";
    }
}