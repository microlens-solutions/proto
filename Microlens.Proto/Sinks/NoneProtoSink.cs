using Microlens.Proto.Models;
using Microlens.Proto.Shared;
using Microsoft.Extensions.Logging;

namespace Microlens.Proto.Sinks;

internal sealed class NoneProtoSink : IProtoSink {
    public ProtoSinkKey Key => ProtoSinkKey.None;

    public string Name => "None";

    public Task LogAsync(LogLevel level, IProtoContext context, string payload, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public Task LogAsync(LogLevel level, IProtoContext context, string payload, Exception exception, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}
