using Microlens.Proto.Models;
using Microlens.Proto.Shared;
using Microsoft.Extensions.Logging;

namespace Microlens.Proto.Sinks {
    internal sealed class DefaultProtoSink(ILogger<DefaultProtoSink> logger) : IProtoSink {
        private readonly ILogger _logger = logger;

        public ProtoSinkKey Key => ProtoSinkKey.Default;

        public string Name => "Default";

        public Task LogAsync(LogLevel level, IProtoScope scope, string payload, CancellationToken cancellationToken) {
            cancellationToken.ThrowIfCancellationRequested();

            if (_logger.IsEnabled(level)) {
                _logger.Log(level, Constants.DEFAULT_LOG_FORMAT, scope.TimestampUtc, scope.Channel, scope.Direction, scope.Phase, scope.Path, Environment.NewLine, Environment.NewLine, payload);
            }

            return Task.CompletedTask;
        }

        public Task LogAsync(LogLevel level, IProtoScope scope, string payload, Exception exception, CancellationToken cancellationToken) {
            cancellationToken.ThrowIfCancellationRequested();

            if (_logger.IsEnabled(level)) {
                _logger.Log(level, exception, Constants.DEFAULT_LOG_FORMAT, scope.TimestampUtc, scope.Channel, scope.Direction, scope.Phase, scope.Path, Environment.NewLine, Environment.NewLine, payload);
            }

            return Task.CompletedTask;
        }
    }
}
