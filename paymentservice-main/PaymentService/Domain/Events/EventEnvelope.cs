using System.Text.Json.Serialization;
using PaymentService.Domain.Enums;

namespace PaymentService.Domain.Events;

public record EventEnvelope<TPayload>
{
    public Guid MessageId { get; init; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OrderEventType EventType { get; init; }
    public DateTime Timestamp { get; init; }
    public required TPayload Payload { get; init; }
}

public record RawEventEnvelope
{
    public Guid MessageId { get; init; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OrderEventType EventType { get; init; }
    public DateTime Timestamp { get; init; }
    public Guid CorrelationId { get; init; }
    public string? TraceParent { get; init; }
    public required object Payload { get; init; }
}


/*
    EventEnvelope<TPayload> and RawEventEnvelope define the standard structure for events 
    sent to and received from RabbitMQ in the PaymentService.

    EventEnvelope<TPayload>:
    - Used when publishing events.
    - Provides strong typing for the payload, which improves compile-time safety and clarity.
    - Contains metadata: MessageId, EventType (enum), Timestamp, and the typed Payload.

    RawEventEnvelope:
    - Used when consuming events.
    - Deserialized first to determine the EventType before binding the Payload to a concrete type.
    - Contains generic metadata along with CorrelationId and TraceParent for tracing and observability.
    - Payload is stored as 'object' to allow dynamic resolution based on EventType.

    This separation ensures type safety during publishing and runtime flexibility during consuming,
    and supports consistent structure, logging, and distributed tracing across the event pipeline.
*/
