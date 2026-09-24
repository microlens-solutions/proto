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

    public required ProtoRegistry.ValueKind Value { get; init; }

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
        return other is not null && Value == other.Value && Equals(Data, other.Data);
    }

    public override int GetHashCode() {
        return unchecked(((int)Value * Registry.HASH_MULTIPLIER) ^ (Data?.GetHashCode() ?? 0));
    }

    public override string ToString() {
        return Data switch {
            null => string.Empty,
            ReadOnlyMemory<byte> bytes => string.Concat("0x", ToHex(bytes.Span)),
            _ => Data.ToString() ?? string.Empty
        };
    }

    internal static ProtoValue FromNumber(ProtoRegistry.ValueKind value, ulong number) {
        return new ProtoValue { Value = value, _number = number, _typed = true };
    }

    internal static ProtoValue FromText(string text) {
        return new ProtoValue { Value = ProtoRegistry.ValueKind.String, _text = text, _typed = true };
    }

    internal static ProtoValue FromBytes(ReadOnlyMemory<byte> bytes) {
        return new ProtoValue { Value = ProtoRegistry.ValueKind.Bytes, _bytes = bytes, _typed = true };
    }

    internal static ProtoValue FromNodes(IReadOnlyList<ProtoNode> nodes) {
        return new ProtoValue { Value = ProtoRegistry.ValueKind.Nested, _nodes = nodes, _typed = true };
    }

    private static string ToHex(ReadOnlySpan<byte> bytes) {
#if NET5_0_OR_GREATER
        return Convert.ToHexString(bytes);
#else
        char[] chars = new char[bytes.Length * 2];

        for (int i = 0; i < bytes.Length; i++) {
            chars[i * 2] = Registry.HEX[bytes[i] >> 4];
            chars[(i * 2) + 1] = Registry.HEX[bytes[i] & 0x0F];
        }

        return new string(chars);
#endif
    }

    private object? Materialize() {
        return !_typed
            ? null
            : Value switch {
                ProtoRegistry.ValueKind.Varint or ProtoRegistry.ValueKind.Fixed64 => _number,
                ProtoRegistry.ValueKind.Fixed32 => (uint)_number,
                ProtoRegistry.ValueKind.String => _text,
                ProtoRegistry.ValueKind.Bytes => _bytes,
                ProtoRegistry.ValueKind.Nested => _nodes,
                _ => null
            };
    }
}
