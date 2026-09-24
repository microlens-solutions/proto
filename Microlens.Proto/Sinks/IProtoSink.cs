using Microlens.Proto.Models;
using Microlens.Proto.Shared;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microlens.Proto.Sinks;

public interface IProtoSink {
    ProtoRegistry.SinkKind Key { get; }

    string Name { get; }

#if NETCOREAPP3_0_OR_GREATER
    bool IsEnabled(LogLevel level) {
        return level != LogLevel.None;
    }
#else
    bool IsEnabled(LogLevel level);
#endif

    Task LogAsync(LogLevel level, IProtoScope scope, string payload, CancellationToken cancellationToken);

    Task LogAsync(LogLevel level, IProtoScope scope, string payload, Exception exception, CancellationToken cancellationToken);
}
