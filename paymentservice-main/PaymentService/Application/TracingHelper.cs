using System.Diagnostics;
using System.Text;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PaymentService.Application;

// DISTRIBUTED TRACING UTILITY: This helper manages OpenTelemetry tracing
// across RabbitMQ messages and HTTP requests in a microservice architecture
// It ensures trace context (parent-child relationships) is preserved across services

public static class TracingHelper
{
    // STATIC CONSTRUCTOR: Set global tracing format to W3C standard
    // W3C is the industry standard for distributed tracing (used by all major cloud providers)
    static TracingHelper()
    {
        Activity.DefaultIdFormat = ActivityIdFormat.W3C;  
        Activity.ForceDefaultIdFormat = true;                 
    }

    // TRACING INFRASTRUCTURE: Core OpenTelemetry components
    private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;
    private static readonly ActivitySource ActivitySource = new ActivitySource("YourService.Messaging");

    // METHOD 1: Create tracing activity when CONSUMING messages from RabbitMQ
    // This links the current operation to the trace started by the message publisher
    public static Activity StartConsumerActivity(BasicDeliverEventArgs args, string operationName)
    {
        // EXTRACT TRACE CONTEXT: Look for W3C traceparent header in RabbitMQ message
        string? traceParent = null;
        if (args.BasicProperties?.Headers?.TryGetValue("traceparent", out var rawTp) == true)
        {
            // HANDLE DIFFERENT DATA TYPES: RabbitMQ headers can be stored as different types
            traceParent = rawTp switch
            {
                byte[] b => Encoding.UTF8.GetString(b),      
                ReadOnlyMemory<byte> rom => Encoding.UTF8.GetString(rom.ToArray()),     // Convert memory to string
                string s => s,                               
                _ => null        // Unknown type, ignore
            };
        }

        Activity activity;

        // SCENARIO 1: Traceparent header exists 
        if (!string.IsNullOrEmpty(traceParent))
        {
            // CREATE CHILD ACTIVITY: Link this operation to the parent trace
            activity = new Activity(operationName)
                .SetParentId(traceParent)        
                .SetIdFormat(ActivityIdFormat.W3C) 
                .Start();                        
        }
        else
        {
            // SCENARIO 2: No traceparent (fallback case)
            // Try to extract context using OpenTelemetry's propagator
            var parentCtx = Propagator.Extract(default, args.BasicProperties?.Headers, ExtractHeader);
            activity = ActivitySource
                           .StartActivity(operationName, ActivityKind.Consumer, parentCtx.ActivityContext)
                       ?? new Activity(operationName).Start(); // Fallback if extraction fails
        }

        return activity; 
    }

    // METHOD 2: Inject tracing context when PUBLISHING messages to RabbitMQ
    // This adds trace headers to outgoing messages so consumers can link back to this trace
    public static void InjectContextIntoProperties(IBasicProperties props, Activity? activity = null)
    {
        // ENSURE HEADERS EXIST: Create headers dictionary if it doesn't exist
        if (props.Headers == null)
            props.Headers = new Dictionary<string, object>();

        // GET CURRENT CONTEXT: Either from provided activity or extract from existing headers
        var context = activity != null
            ? new PropagationContext(activity.Context, Baggage.Current)  // Use provided activity
            : Propagators.DefaultTextMapPropagator.Extract(default, props.Headers, (h, k) => Array.Empty<string>());

        // INJECT CONTEXT: Add trace headers (like traceparent) to RabbitMQ message headers
        Propagator.Inject(
            context,
            props.Headers,
            (headers, key, value) =>
            {
                // ENCODE AS BYTES: RabbitMQ headers are stored as byte arrays
                headers[key] = Encoding.UTF8.GetBytes(value);
            }
        );
    }

    // METHOD 3: Create tracing activity for incoming HTTP requests
    // This extracts trace context from HTTP headers and creates server-side spans
    public static Activity? StartServerActivity(HttpRequest request)
    {
        // EXTRACT HTTP TRACE CONTEXT: Look for traceparent in HTTP headers
        var parentContext = Propagator.Extract(
            default,
            request.Headers,
            (headers, key) =>
            {
                // HEADER EXTRACTION: Get header values as string array
                if (headers.TryGetValue(key, out var values))
                    return values.ToArray();
                return Array.Empty<string>();
            });

        // SET BAGGAGE: Transfer any baggage from parent
        Baggage.Current = parentContext.Baggage;

        // CREATE OPERATION NAME: Combine HTTP method and path for span names
        var route = request.Path.HasValue
            ? request.Path.Value!
            : request.Path.ToString();
        var operationName = $"{request.Method} {route}"; 

        // CREATE SERVER ACTIVITY: Start a new span for this HTTP request
        var activity = ActivitySource.StartActivity(
            operationName,
            ActivityKind.Server,          
            parentContext.ActivityContext); // Link to parent if exists

        // ADD HTTP TAGS: Enrich the span with HTTP-specific information
        if (activity is not null)
        {
            activity.SetTag("http.method", request.Method);           
            activity.SetTag("http.scheme", request.Scheme);           
            activity.SetTag("http.host", request.Host.Value);         
            activity.SetTag("http.target", request.Path + request.QueryString); 
        }

        return activity; 
    }

    // HELPER METHOD: Extract trace headers from RabbitMQ message headers
    // Handles different data types that RabbitMQ might use for header values
    private static IEnumerable<string>? ExtractHeader(IDictionary<string, object?>? headers, string key)
    {
        if (headers != null && headers.TryGetValue(key, out var raw))
        {
            // CONVERT TO STRING: Handle different possible data types
            if (raw is byte[] b) yield return Encoding.UTF8.GetString(b);
            else if (raw is ReadOnlyMemory<byte> rom) yield return Encoding.UTF8.GetString(rom.ToArray());
            else if (raw is string s) yield return s;
        }
    }
}

/*
 * SUMMARY - Distributed Tracing Concepts:
 * 
 * WHAT IS DISTRIBUTED TRACING?
 * - Tracks a single request as it flows through multiple services
 * - Each service adds a "span" to the overall "trace"
 * - Helps debug performance issues and understand service interactions
 * 
 * W3C TRACE CONTEXT STANDARD:
 * - traceparent header: Contains trace ID, span ID, and flags
 * - Format: "00-{trace-id}-{span-id}-{flags}"
 * - Example: "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
 * 
 * KEY POINTS:
 * - CONSUMER PATTERN: Extract parent context → Create child activity
 * - PUBLISHER PATTERN: Inject current context → Add to message headers
 * - HTTP PATTERN: Extract from HTTP headers → Create server span
 * - ACTIVITY LIFECYCLE: Create → Start → Use in using() → Dispose automatically
 * - BAGGAGE: Additional context that flows with traces (like correlation IDs)
 * 
 * MICROSERVICE TRACING FLOW:
 * 1. HTTP Request → Service A (creates trace)
 * 2. Service A → RabbitMQ (injects trace context)
 * 3. RabbitMQ → Service B (extracts trace context, creates child span)
 * 4. Service B → Database (child span continues)
 * 
 * WHY THIS MATTERS:
 * - Observability: See how requests flow through your system
 * - Performance: Identify slow services or operations
 * - Debugging: Correlate logs and errors across services
 * - Monitoring: Track request success/failure rates
 */