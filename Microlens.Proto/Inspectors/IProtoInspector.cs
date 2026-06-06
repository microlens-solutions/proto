using Google.Protobuf;
using Microlens.Proto.Models;
using System.Buffers;

namespace Microlens.Proto.Inspectors;

public interface IProtoInspector {
    IReadOnlyList<ProtoNode> Inspect(IMessage message);

    IReadOnlyList<ProtoNode> Inspect(ReadOnlySequence<byte> sequence);
}
