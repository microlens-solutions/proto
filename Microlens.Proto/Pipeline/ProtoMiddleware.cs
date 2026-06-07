using Microlens.Proto.Formatters;
using Microlens.Proto.Inspectors;
using Microlens.Proto.Models;
using Microlens.Proto.Shared;
using Microlens.Proto.Sinks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IO;

namespace Microlens.Proto.Pipeline;

internal sealed class ProtoMiddleware {
    private readonly RequestDelegate _next;
    private readonly ProtoOptions _options;
    private readonly IProtoContext _context;
    private readonly IProtoInspector _inspector;
    private readonly IProtoFormatter _formatter;
    private readonly IProtoSink _sink;
    private static readonly RecyclableMemoryStreamManager _stream = new();

    internal ProtoMiddleware(RequestDelegate next, IOptions<ProtoOptions> options, IProtoContext context, IProtoInspector inspector, IProtoFormatterResolver formatter, IProtoSinkResolver sink) {
        _next = next;
        _options = options.Value;
        _context = context;
        _inspector = inspector;
        _formatter = formatter.Get(_options.CustomFormatterName);
        _sink = sink.Get(_options.CustomSinkName);
    }

    public async Task InvokeAsync(HttpContext context) {
        if (!_options.GlobalMiddlewareEnabled) {
            await _next(context).ConfigureAwait(false);
            return;
        }

        if (!Helpers.ShouldApplyMiddleware(context.Request.ContentType)) {
            await _next(context).ConfigureAwait(false);
            return;
        }

        if (Helpers.ShouldSkipMiddleware(context.GetEndpoint()?.Metadata)) {
            await _next(context).ConfigureAwait(false);
            return;
        }

        _context.Channel = ProtoChannelType.Http.ToString();
        _context.Path = context.Request.GetDisplayUrl();

        if (_options.CaptureMode.HasFlag(ProtoCaptureMode.Request)) {
            _context.Direction = ProtoDirectionType.Inbound.ToString();
            _context.Phase = ProtoPhaseType.Request.ToString();

            context.Request.EnableBuffering();
            await using var request = _stream.GetStream();
            await context.Request.Body.CopyToAsync(request).ConfigureAwait(false);

            context.Request.Body.Position = 0;
            await TraceMessage(request, _options.LogScope.HasFlag(ProtoLogScope.Request));
        }

        if (_options.CaptureMode.HasFlag(ProtoCaptureMode.Response)) {
            _context.Direction = ProtoDirectionType.Outbound.ToString();
            _context.Phase = ProtoPhaseType.Response.ToString();

            var body = context.Response.Body;
            await using var response = _stream.GetStream();
            context.Response.Body = response;

            try {
                await _next(context).ConfigureAwait(false);
                await TraceMessage(response, _options.LogScope.HasFlag(ProtoLogScope.Response)).ConfigureAwait(false);

                if (response.Length > 0) {
                    response.Position = 0;
                    await response.CopyToAsync(body).ConfigureAwait(false);
                }
            }
            finally {
                context.Response.Body = body;
            }
        }
        else {
            await _next(context).ConfigureAwait(false);
        }
    }

    private async Task TraceMessage(RecyclableMemoryStream recyclable, bool log) {
        try {
            if (recyclable.Length > 0) {
                var sequence = recyclable.GetReadOnlySequence();
                var nodes = _inspector.Inspect(sequence);
                string description = _formatter.Format(nodes);

                if (log) {
                    _context.TimestampUtc = DateTime.UtcNow;
                    await _sink.LogAsync(_options.LogLevel, _context, description, CancellationToken.None).ConfigureAwait(false);
                }
            }
        }
        catch { }
    }
}
