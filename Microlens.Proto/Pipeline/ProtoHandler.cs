using Microlens.Proto.Formatters;
using Microlens.Proto.Inspectors;
using Microlens.Proto.Models;
using Microlens.Proto.Shared;
using Microlens.Proto.Sinks;
using Microsoft.Extensions.Options;
using Microsoft.IO;

namespace Microlens.Proto.Pipeline;

internal sealed class ProtoHandler : DelegatingHandler {
    private readonly ProtoOptions _options;
    private readonly IProtoContext _context;
    private readonly IProtoInspector _inspector;
    private readonly IProtoFormatter _formatter;
    private readonly IProtoSink _sink;
    private static readonly RecyclableMemoryStreamManager _stream = new();

    internal ProtoHandler(IOptions<ProtoOptions> options, IProtoContext context, IProtoInspector inspector, IProtoFormatterResolver formatter, IProtoSinkResolver sink) {
        _options = options.Value;
        _context = context;
        _inspector = inspector;
        _formatter = formatter.Get(_options.CustomFormatterName);
        _sink = sink.Get(_options.CustomSinkName);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_options.GlobalHandlerEnabled || !Helpers.IsHandlerApplicable(request.Content?.Headers.ContentType)) {
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        _context.Channel = ProtoChannelType.Http.ToString();
        _context.Path = request.RequestUri?.AbsolutePath ?? string.Empty;

        if (_options.CaptureMode.HasFlag(ProtoCaptureMode.Request)) {
            _context.Direction = ProtoDirectionType.Outbound.ToString();
            _context.Phase = ProtoPhaseType.Request.ToString();
            await TraceMessage(request.Content, _options.LogScope.HasFlag(ProtoLogScope.Request), cancellationToken).ConfigureAwait(false);
        }

        HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (_options.CaptureMode.HasFlag(ProtoCaptureMode.Response)) {
            if (response.Content != null) {
                await response.Content.LoadIntoBufferAsync().ConfigureAwait(false);
            }

            _context.Direction = ProtoDirectionType.Inbound.ToString();
            _context.Phase = ProtoPhaseType.Response.ToString();
            await TraceMessage(response.Content, _options.LogScope.HasFlag(ProtoLogScope.Response), cancellationToken).ConfigureAwait(false);
        }

        return response;
    }

    private async Task TraceMessage(HttpContent? content, bool log, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();

        if (!Helpers.IsHandlerApplicable(content?.Headers.ContentType)) {
            return;
        }

        try {
            if (content != null) {
                var stream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                using var recyclable = _stream.GetStream();
                await stream.CopyToAsync(recyclable, cancellationToken).ConfigureAwait(false);

                if (stream.CanSeek) {
                    stream.Position = 0;
                }

                var sequence = recyclable.GetReadOnlySequence();
                var nodes = _inspector.Inspect(sequence);
                string description = _formatter.Format(nodes);

                if (log) {
                    _context.TimestampUtc = DateTime.UtcNow;
                    await _sink.LogAsync(_options.LogLevel, _context, description, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        catch { }
    }
}
