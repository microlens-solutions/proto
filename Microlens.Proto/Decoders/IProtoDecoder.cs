using Microlens.Proto.Models;
using System.Buffers;

namespace Microlens.Proto.Decoders;

public interface IProtoDecoder {
    IReadOnlyList<ProtoNode> Decode(ReadOnlySequence<byte> sequence);

    bool TryDecodeNested(ReadOnlyMemory<byte> data, out IReadOnlyList<ProtoNode> nodes);
}
