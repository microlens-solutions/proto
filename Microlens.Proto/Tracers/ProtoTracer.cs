using Google.Protobuf;
using Microlens.Proto.Formatters;
using Microlens.Proto.Inspectors;
using Microlens.Proto.Models;
using Microlens.Proto.Shared;
using Microlens.Proto.Sinks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IO;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microlens.Proto.Tracers;

internal sealed class ProtoTracer {
    private readonly IProtoInspector _inspector;

    private readonly IProtoFormatter _formatter;

    private readonly IProtoSink _sink;

    private readonly LogLevel _level;

    private readonly bool _decode;

    internal ProtoTracer(IOptions<ProtoOptions> options, IProtoInspector inspector, IProtoFormatterResolver formatter, IProtoSinkResolver sink) {
        Options = options.Value;
        _inspector = inspector;
        _formatter = formatter.Get(Options.CustomFormatterName);
        _sink = sink.Get(Options.CustomSinkName);
        _level = Options.LogLevel;
        _decode = _formatter.Key != Registry.ProtoFormatterKey.None;

        TraceRequest = Options.CaptureMode.HasFlag(Registry.ProtoCaptureMode.Request) && Options.LogScope.HasFlag(Registry.ProtoLogScope.Request);
        TraceResponse = Options.CaptureMode.HasFlag(Registry.ProtoCaptureMode.Response) && Options.LogScope.HasFlag(Registry.ProtoLogScope.Response);
    }

    internal static RecyclableMemoryStreamManager Streams { get; } = new();

    internal ProtoOptions Options { get; }

    internal bool TraceRequest { get; }

    internal bool TraceResponse { get; }

    internal bool IsActive => (TraceRequest || TraceResponse) && _sink.IsEnabled(_level);

    internal async Task TraceAsync(object? message, Registry.ProtoChannelType channel, Registry.ProtoDirectionType direction, Registry.ProtoPhaseType phase, string? path) {
        if (message is not IMessage protobuf || !_sink.IsEnabled(_level)) {
            return;
        }

        try {
            IReadOnlyList<ProtoNode> nodes = _decode ? _inspector.Inspect(protobuf) : [];
            await _sink.LogAsync(_level, Helpers.BuildScope(channel, direction, phase, path), _formatter.Format(nodes), CancellationToken.None).ConfigureAwait(false);
        }
        catch { }
    }

    internal async Task TraceAsync(ReadOnlySequence<byte> sequence, Registry.ProtoChannelType channel, Registry.ProtoDirectionType direction, Registry.ProtoPhaseType phase, string? path, CancellationToken cancellationToken) {
        if (!_sink.IsEnabled(_level)) {
            return;
        }

        try {
            IReadOnlyList<ProtoNode> nodes = _decode ? _inspector.Inspect(sequence) : [];
            await _sink.LogAsync(_level, Helpers.BuildScope(channel, direction, phase, path), _formatter.Format(nodes), cancellationToken).ConfigureAwait(false);
        }
        catch { }
    }
}
