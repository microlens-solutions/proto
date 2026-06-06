using Microlens.Proto.Models;
using Microlens.Proto.Shared;
using Microsoft.Extensions.Logging;

namespace Microlens.Proto.Sinks;

public interface IProtoSink {
    ProtoSinkKey Key { get; }

    string Name { get; }

    Task LogAsync(LogLevel level, IProtoContext context, string payload, CancellationToken cancellationToken);

    Task LogAsync(LogLevel level, IProtoContext context, string payload, Exception exception, CancellationToken cancellationToken);
}
