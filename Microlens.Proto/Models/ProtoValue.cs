using Microlens.Proto.Shared;

namespace Microlens.Proto.Models;

public sealed record ProtoValue {
    public required ProtoValueType Type { get; init; }

    public object? Data { get; init; }

    public override string ToString() {
        return Data?.ToString() ?? string.Empty;
    }
}
