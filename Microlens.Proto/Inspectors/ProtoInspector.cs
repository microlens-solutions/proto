using Google.Protobuf;
using Microlens.Proto.Decoders;
using Microlens.Proto.Models;
using System.Buffers;

namespace Microlens.Proto.Inspectors;

internal sealed class ProtoInspector(IProtoDecoder decoder) : IProtoInspector {
    private readonly IProtoDecoder _decoder = decoder;

    public IReadOnlyList<ProtoNode> Inspect(IMessage message) {
        if (message is null) {
            return [];
        }

        try {
            byte[] bytes = message.ToByteArray();

            if (bytes.Length == 0) {
                return [];
            }

            var sequence = new ReadOnlySequence<byte>(bytes);
            return _decoder.Decode(sequence);
        }
        catch {
            return [];
        }
    }

    public IReadOnlyList<ProtoNode> Inspect(ReadOnlySequence<byte> sequence) {
        if (sequence.IsEmpty) {
            return [];
        }

        try {
            return _decoder.Decode(sequence);
        }
        catch {
            return [];
        }
    }
}
