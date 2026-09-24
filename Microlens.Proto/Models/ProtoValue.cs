using Microlens.Proto.Shared;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Microlens.Proto.Models;

public sealed record ProtoValue {
    private ulong _number;

    private string? _text;

    private ReadOnlyMemory<byte> _bytes;

    private IReadOnlyList<ProtoNode>? _nodes;

    private bool _typed;

    private object? _data;

    private int _materialized;

    public required Registry.ProtoValueType Type { get; init; }

    public object? Data {
        get {
            if (Volatile.Read(ref _materialized) != 0) {
                return _data;
            }

            object? data = Materialize();
            _data = data;
            Volatile.Write(ref _materialized, 1);
            return data;
        }
        init {
            _data = value;
            _typed = false;
            _materialized = 1;
        }
    }

    internal bool Typed => _typed;

    internal ulong Number => _number;

    internal string? Text => _text;

    internal ReadOnlyMemory<byte> Bytes => _bytes;

    public bool Equals(ProtoValue? other) {
        return other is not null && Type == other.Type && object.Equals(Data, other.Data);
    }

    public override int GetHashCode() {
        return HashCode.Combine(Type, Data);
    }

    public override string ToString() {
        return Data switch {
            null => string.Empty,
            ReadOnlyMemory<byte> bytes => string.Concat("0x", Convert.ToHexString(bytes.Span)),
            _ => Data.ToString() ?? string.Empty
        };
    }

    internal static ProtoValue FromNumber(Registry.ProtoValueType type, ulong number) {
        return new ProtoValue { Type = type, _number = number, _typed = true };
    }

    internal static ProtoValue FromText(string text) {
        return new ProtoValue { Type = Registry.ProtoValueType.String, _text = text, _typed = true };
    }

    internal static ProtoValue FromBytes(ReadOnlyMemory<byte> bytes) {
        return new ProtoValue { Type = Registry.ProtoValueType.Bytes, _bytes = bytes, _typed = true };
    }

    internal static ProtoValue FromNodes(IReadOnlyList<ProtoNode> nodes) {
        return new ProtoValue { Type = Registry.ProtoValueType.Nested, _nodes = nodes, _typed = true };
    }

    private object? Materialize() {
        return !_typed
            ? null
            : Type switch {
                Registry.ProtoValueType.Varint or Registry.ProtoValueType.Fixed64 => _number,
                Registry.ProtoValueType.Fixed32 => (uint)_number,
                Registry.ProtoValueType.String => _text,
                Registry.ProtoValueType.Bytes => _bytes,
                Registry.ProtoValueType.Nested => _nodes,
                _ => null
            };
    }
}
