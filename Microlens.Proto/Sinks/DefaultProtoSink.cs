using Microlens.Proto.Models;
using Microlens.Proto.Shared;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microlens.Proto.Sinks;

internal sealed class DefaultProtoSink(ILogger<DefaultProtoSink> logger) : IProtoSink {
    private readonly ILogger _logger = logger;

    [Obsolete]
    public Registry.ProtoSinkKey Key => Registry.ProtoSinkKey.Default;

    public string Name => "Default";

    public bool IsEnabled(LogLevel level) {
        return _logger.IsEnabled(level);
    }

    public Task LogAsync(LogLevel level, IProtoScope scope, string payload, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();

        if (_logger.IsEnabled(level)) {
            _logger.Log(level, Registry.DEFAULT_LOG_FORMAT, scope.TimestampUtc, scope.Channel, scope.Direction, scope.Phase, scope.Path, payload);
        }

        return Task.CompletedTask;
    }

    public Task LogAsync(LogLevel level, IProtoScope scope, string payload, Exception exception, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();

        if (_logger.IsEnabled(level)) {
            _logger.Log(level, exception, Registry.DEFAULT_LOG_FORMAT, scope.TimestampUtc, scope.Channel, scope.Direction, scope.Phase, scope.Path, payload);
        }

        return Task.CompletedTask;
    }
}
