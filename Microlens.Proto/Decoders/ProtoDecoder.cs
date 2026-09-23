using Google.Protobuf;
using Microlens.Proto.Models;
using Microlens.Proto.Shared;
using System.Buffers;
using System.Buffers.Binary;
using System.Text;
using System.Text.Unicode;

namespace Microlens.Proto.Decoders;

internal sealed class ProtoDecoder : IProtoDecoder {
    public IReadOnlyList<ProtoNode> Decode(ReadOnlySequence<byte> sequence) {
        _ = TryDecode(sequence, strict: false, depth: 0, out var nodes);
        return nodes;
    }

    public bool TryDecodeNested(ReadOnlyMemory<byte> data, out IReadOnlyList<ProtoNode> nodes) {
        return TryDecodeNested(data, depth: 0, out nodes);
    }

    private bool TryDecodeNested(ReadOnlyMemory<byte> data, int depth, out IReadOnlyList<ProtoNode> nodes) {
        nodes = [];

        if (data.IsEmpty || depth >= Registry.MAXIMUM_NESTED_DEPTH) {
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
            IReadOnlyList<ProtoNode> children = [];

            if (wireType == WireFormat.WireType.LengthDelimited) {
                if (TryDecodeNested(rawData, depth, out var nested)) {
                    children = nested;
                    value = new ProtoValue {
                        Type = Registry.ProtoValueType.Nested,
                        Data = nested
                    };
                }
                else if (TryDecodeUtf8Text(rawData.Span, out string text)) {
                    value = new ProtoValue {
                        Type = Registry.ProtoValueType.String,
                        Data = text
                    };
                }
            }

            var node = new ProtoNode {
                FieldNumber = fieldNumber,
                WireType = wireType,
                RawData = rawData,
                Value = value ?? new ProtoValue {
                    Type = Registry.ProtoValueType.Bytes,
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
                        Type = Registry.ProtoValueType.Varint,
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
                        Type = Registry.ProtoValueType.Fixed32,
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
                        Type = Registry.ProtoValueType.Fixed64,
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

    private static bool TryDecodeUtf8Text(ReadOnlySpan<byte> data, out string text) {
        text = string.Empty;

        if (!Utf8.IsValid(data)) {
            return false;
        }

        for (int i = 0; i < data.Length; i++) {
            byte b = data[i];

            if (b < 0x20) {
                if (b is not 0x09 and not 0x0A and not 0x0D) {
                    return false;
                }
            }
            else if (b == 0x7F) {
                return false;
            }
            else if (b == 0xC2 && i + 1 < data.Length && data[i + 1] <= 0x9F) {
                return false;
            }
        }

        text = Encoding.UTF8.GetString(data);
        return true;
    }

    private static ReadOnlyMemory<byte> GetMemoryFromSequence(ReadOnlySequence<byte> sequence) {
        return sequence.IsSingleSegment ? sequence.First : sequence.ToArray();
    }
}
