using Microlens.Proto.Shared;
using System;

namespace Microlens.Proto.Models;

public sealed record ProtoValue {
    public required Registry.ProtoValueType Type { get; init; }

    public object? Data { get; init; }

    public override string ToString() {
        return Data switch {
            null => string.Empty,
            ReadOnlyMemory<byte> bytes => string.Concat("0x", Convert.ToHexString(bytes.Span)),
            _ => Data.ToString() ?? string.Empty
        };
    }
}
