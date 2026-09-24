using Microlens.Proto.Models;
using Microlens.Proto.Shared;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microlens.Proto.Sinks;

internal sealed class NoneProtoSink : IProtoSink {
    [Obsolete]
    public Registry.ProtoSinkKey Key => Registry.ProtoSinkKey.None;

    public string Name => "None";

    public bool IsEnabled(LogLevel level) {
        return false;
    }

    public Task LogAsync(LogLevel level, IProtoScope scope, string payload, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public Task LogAsync(LogLevel level, IProtoScope scope, string payload, Exception exception, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}
