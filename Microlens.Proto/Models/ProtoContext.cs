namespace Microlens.Proto.Models;

internal sealed class ProtoContext() : IProtoContext {
    public required DateTime TimestampUtc { get; set; }

    public required string Channel { get; set; }

    public required string Direction { get; set; }

    public required string Phase { get; set; }

    public string? Path { get; set; }
}
