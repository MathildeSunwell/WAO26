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