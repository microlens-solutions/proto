using Google.Protobuf;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microlens.Proto.Formatters;
using Microlens.Proto.Inspectors;
using Microlens.Proto.Models;
using Microlens.Proto.Shared;
using Microlens.Proto.Sinks;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;

namespace Microlens.Proto.Pipeline;

internal sealed class ProtoServerInterceptor : Interceptor {
    private readonly ProtoOptions _options;
    private readonly IProtoContext _context;
    private readonly IProtoInspector _inspector;
    private readonly IProtoFormatter _formatter;
    private readonly IProtoSink _sink;

    internal ProtoServerInterceptor(IOptions<ProtoOptions> options, IProtoContext context, IProtoInspector inspector, IProtoFormatterResolver formatter, IProtoSinkResolver sink) {
        _options = options.Value;
        _context = context;
        _inspector = inspector;
        _formatter = formatter.Get(_options.CustomFormatterName);
        _sink = sink.Get(_options.CustomSinkName);
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation) {
        if (!_options.GlobalInterceptorEnabled || !Helpers.IsInceptorApplicable(context.GetHttpContext().Request.ContentType)) {
            return await continuation(request, context).ConfigureAwait(false);
        }

        _context.Channel = ProtoChannelType.Grpc.ToString();
        _context.Path = context.GetHttpContext().Request.GetDisplayUrl();

        if (_options.CaptureMode.HasFlag(ProtoCaptureMode.Request)) {
            _context.Direction = ProtoDirectionType.Inbound.ToString();
            _context.Phase = ProtoPhaseType.Request.ToString();
            await TraceMessage(request, _options.LogScope.HasFlag(ProtoLogScope.Request), context.GetHttpContext().Request.ContentType).ConfigureAwait(false);
        }

        TResponse response = await continuation(request, context).ConfigureAwait(false);

        if (_options.CaptureMode.HasFlag(ProtoCaptureMode.Response)) {
            _context.Direction = ProtoDirectionType.Outbound.ToString();
            _context.Phase = ProtoPhaseType.Response.ToString();
            await TraceMessage(response, _options.LogScope.HasFlag(ProtoLogScope.Response), context.GetHttpContext().Request.ContentType).ConfigureAwait(false);
        }

        return response;
    }

    private async Task TraceMessage<TMessage>(TMessage target, bool log, string? contentType) where TMessage : class {
        if (!Helpers.IsInceptorApplicable(contentType)) {
            return;
        }

        try {
            if (target is IMessage message) {
                var nodes = _inspector.Inspect(message);
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
