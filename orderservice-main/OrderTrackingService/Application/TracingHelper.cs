using System.Diagnostics;
using System.Text;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OrderTrackingService.Application;

public static class TracingHelper
{
    static TracingHelper()
    {
        Activity.DefaultIdFormat     = ActivityIdFormat.W3C;
        Activity.ForceDefaultIdFormat = true;
    }
    
    private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;
    private static readonly ActivitySource ActivitySource = new ActivitySource("YourService.Messaging");
    
    public static Activity StartConsumerActivity(BasicDeliverEventArgs args, string operationName)
    {
        // extract the raw traceparent header
        string? traceParent = null;
        if (args.BasicProperties?.Headers?.TryGetValue("traceparent", out var rawTp) == true)
        {
            traceParent = rawTp switch
            {
                byte[] b             => Encoding.UTF8.GetString(b),
                ReadOnlyMemory<byte> rom => Encoding.UTF8.GetString(rom.ToArray()),
                string s             => s,
                _                    => null
            };
        }

        Activity activity;
        if (!string.IsNullOrEmpty(traceParent))
        {
            // link parent ID and force W3C format
            activity = new Activity(operationName)
                .SetParentId(traceParent)
                .SetIdFormat(ActivityIdFormat.W3C)
                .Start();
        }
        else
        {
            // fallback to a normal consumer Activity if no header was present
            var parentCtx = Propagator.Extract(default, args.BasicProperties?.Headers, ExtractHeader);
            activity = ActivitySource
                           .StartActivity(operationName, ActivityKind.Consumer, parentCtx.ActivityContext)
                       ?? new Activity(operationName).Start();
        }

        return activity;
    }
    
    public static void InjectContextIntoProperties(IBasicProperties props, Activity? activity = null)
    {
        if (props.Headers == null)
            props.Headers = new Dictionary<string, object>();

        var context = activity != null
            ? new PropagationContext(activity.Context, Baggage.Current)
            : Propagators.DefaultTextMapPropagator.Extract(default, props.Headers, (h, k) => Array.Empty<string>());

        Propagator.Inject(
            context,
            props.Headers,
            (headers, key, value) =>
            {
                headers[key] = Encoding.UTF8.GetBytes(value);
            }
        );
    }
    
    public static Activity? StartServerActivity(HttpRequest request)
    {
        // extract incoming context
        var parentContext = Propagator.Extract(
            default,
            request.Headers,
            (headers, key) =>
            {
                if (headers.TryGetValue(key, out var values))
                    return values.ToArray();
                return Array.Empty<string>();
            });
        
        Baggage.Current = parentContext.Baggage;
        
        var route = request.Path.HasValue
            ? request.Path.Value!
            : request.Path.ToString();
        var operationName = $"{request.Method} {route}";

        var activity = ActivitySource.StartActivity(
            operationName,
            ActivityKind.Server,
            parentContext.ActivityContext);

        if (activity is not null)
        {
            activity.SetTag("http.method", request.Method);
            activity.SetTag("http.scheme", request.Scheme);
            activity.SetTag("http.host", request.Host.Value);
            activity.SetTag("http.target", request.Path + request.QueryString);
        }

        return activity;
    }
    
    private static IEnumerable<string>? ExtractHeader(IDictionary<string, object?>? headers, string key)
    {
        if (headers != null && headers.TryGetValue(key, out var raw))
        {
            if (raw is byte[] b) yield return Encoding.UTF8.GetString(b);
            else if (raw is ReadOnlyMemory<byte> rom) yield return Encoding.UTF8.GetString(rom.ToArray());
            else if (raw is string s) yield return s;
        }
    }
}