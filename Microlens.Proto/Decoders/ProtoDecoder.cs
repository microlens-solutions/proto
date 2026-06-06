using Google.Protobuf;
using Microlens.Proto.Models;
using Microlens.Proto.Shared;
using System.Buffers;
using System.Buffers.Binary;
using System.Text;

namespace Microlens.Proto.Decoders;

internal sealed class ProtoDecoder : IProtoDecoder {
    private static readonly UTF8Encoding _strict = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public IReadOnlyList<ProtoNode> Decode(ReadOnlySequence<byte> sequence) {
        _ = TryDecode(sequence, strict: false, depth: 0, out var nodes);
        return nodes;
    }

    public bool TryDecodeNested(ReadOnlyMemory<byte> data, out IReadOnlyList<ProtoNode> nodes) {
        return TryDecodeNested(data, depth: 0, out nodes);
    }

    private bool TryDecodeNested(ReadOnlyMemory<byte> data, int depth, out IReadOnlyList<ProtoNode> nodes) {
        nodes = [];

        if (data.IsEmpty || depth >= Constants.MAXIMUM_NESTED_DEPTH) {
            return false;
        }

        var sequence = new ReadOnlySequence<byte>(data);

        if (!TryDecode(sequence, strict: true, depth: depth + 1, out var decoded) ||
            decoded.Count == 0) {
            return false;
        }

        nodes = decoded;
        return true;
    }

    private bool TryDecode(ReadOnlySequence<byte> sequence, bool strict, int depth, out IReadOnlyList<ProtoNode> nodes) {
        var decoded = new List<ProtoNode>();
        var reader = new SequenceReader<byte>(sequence);

        while (!reader.End) {
            if (!TryReadVarint(ref reader, out ulong rawTag) || rawTag > uint.MaxValue) {
                nodes = decoded;
                return !strict;
            }

            uint tag = (uint)rawTag;
            int fieldNumber = WireFormat.GetTagFieldNumber(tag);

            if (fieldNumber <= 0) {
                nodes = decoded;
                return !strict;
            }

            var wireType = WireFormat.GetTagWireType(tag);

            if (!TryReadValueData(ref reader, wireType, out var rawSequence, out var value)) {
                nodes = decoded;
                return !strict;
            }

            ReadOnlyMemory<byte> rawData = GetMemoryFromSequence(rawSequence);

            if (wireType == WireFormat.WireType.LengthDelimited && TryDecodeUtf8Text(rawData, out string text)) {
                value = new ProtoValue {
                    Type = ProtoValueType.String,
                    Data = text
                };
            }

            IReadOnlyList<ProtoNode>? children = [];

            if (wireType == WireFormat.WireType.LengthDelimited && TryDecodeNested(rawData, depth, out var nested)) {
                children = nested;
                value = new ProtoValue {
                    Type = ProtoValueType.Nested,
                    Data = nested
                };
            }

            var node = new ProtoNode {
                FieldNumber = fieldNumber,
                WireType = wireType,
                RawData = rawData,
                Value = value ?? new ProtoValue {
                    Type = ProtoValueType.Bytes,
                    Data = rawData
                },
                Children = children
            };

            decoded.Add(node);
        }

        nodes = decoded;
        return true;
    }

    private static bool TryReadVarint(ref SequenceReader<byte> reader, out ulong value) {
        value = 0;

        for (int byteIndex = 0; byteIndex < 10; byteIndex++) {
            if (!reader.TryRead(out byte b)) {
                return false;
            }

            if (byteIndex == 9 && (b & 0xFE) != 0) {
                return false;
            }

            value |= (ulong)(b & 0x7F) << (byteIndex * 7);

            if ((b & 0x80) == 0) {
                return true;
            }
        }

        return false;
    }

    private static bool TryReadValueData(ref SequenceReader<byte> reader, WireFormat.WireType wireType, out ReadOnlySequence<byte> payload, out ProtoValue? value) {
        payload = ReadOnlySequence<byte>.Empty;
        value = null;

        switch (wireType) {
            case WireFormat.WireType.Varint: {
                    var start = reader.Position;

                    if (!TryReadVarint(ref reader, out ulong varint)) {
                        return false;
                    }

                    payload = reader.Sequence.Slice(start, reader.Position);
                    value = new ProtoValue {
                        Type = ProtoValueType.Varint,
                        Data = varint
                    };

                    return true;
                }

            case WireFormat.WireType.Fixed32: {
                    if (reader.Remaining < 4) {
                        return false;
                    }

                    Span<byte> buffer = stackalloc byte[4];

                    if (!reader.TryCopyTo(buffer)) {
                        return false;
                    }

                    payload = reader.Sequence.Slice(reader.Position, 4);
                    value = new ProtoValue {
                        Type = ProtoValueType.Fixed32,
                        Data = BinaryPrimitives.ReadUInt32LittleEndian(buffer)
                    };

                    reader.Advance(4);
                    return true;
                }

            case WireFormat.WireType.Fixed64: {
                    if (reader.Remaining < 8) {
                        return false;
                    }

                    Span<byte> buffer = stackalloc byte[8];

                    if (!reader.TryCopyTo(buffer)) {
                        return false;
                    }

                    payload = reader.Sequence.Slice(reader.Position, 8);
                    value = new ProtoValue {
                        Type = ProtoValueType.Fixed64,
                        Data = BinaryPrimitives.ReadUInt64LittleEndian(buffer)
                    };

                    reader.Advance(8);
                    return true;
                }

            case WireFormat.WireType.LengthDelimited: {
                    if (!TryReadVarint(ref reader, out ulong length) || length > (ulong)reader.Remaining || length > long.MaxValue) {
                        return false;
                    }

                    payload = reader.Sequence.Slice(reader.Position, (long)length);
                    reader.Advance((long)length);
                    return true;
                }

            case WireFormat.WireType.StartGroup:
            case WireFormat.WireType.EndGroup:
                return false;

            default:
                return false;
        }
    }

    private static bool TryDecodeUtf8Text(ReadOnlyMemory<byte> data, out string text) {
        text = string.Empty;

        try {
            text = _strict.GetString(data.Span);
        }
        catch (DecoderFallbackException) {
            return false;
        }

        foreach (char c in text) {
            if (char.IsControl(c) && c is not '\r' and not '\n' and not '\t') {
                text = string.Empty;
                return false;
            }
        }

        return true;
    }

    private static ReadOnlyMemory<byte> GetMemoryFromSequence(ReadOnlySequence<byte> sequence) {
        return sequence.IsSingleSegment ? sequence.First : sequence.ToArray();
    }
}
