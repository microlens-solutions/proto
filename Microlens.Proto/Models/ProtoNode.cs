using Google.Protobuf;

namespace Microlens.Proto.Models;

public sealed record ProtoNode {
    public required int FieldNumber { get; init; }

    public required WireFormat.WireType WireType { get; init; }

    public required ReadOnlyMemory<byte> RawData { get; init; }

    public ProtoValue? Value { get; init; }

    public IReadOnlyList<ProtoNode> Children { get; init; } = [];
}
