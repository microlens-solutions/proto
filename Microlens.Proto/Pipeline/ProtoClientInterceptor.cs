using Google.Protobuf;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microlens.Proto.Extensions;
using Microlens.Proto.Formatters;
using Microlens.Proto.Inspectors;
using Microlens.Proto.Models;
using Microlens.Proto.Shared;
using Microlens.Proto.Sinks;
using Microsoft.Extensions.Options;

namespace Microlens.Proto.Pipeline;

internal sealed class ProtoClientInterceptor : Interceptor {
    private readonly ProtoOptions _options;
    private readonly IProtoContext _context;
    private readonly IProtoInspector _inspector;
    private readonly IProtoFormatter _formatter;
    private readonly IProtoSink _sink;

    internal ProtoClientInterceptor(IOptions<ProtoOptions> options, IProtoContext context, IProtoInspector inspector, IProtoFormatterResolver formatter, IProtoSinkResolver sink) {
        _options = options.Value;
        _context = context;
        _inspector = inspector;
        _formatter = formatter.Get(_options.CustomFormatterName);
        _sink = sink.Get(_options.CustomSinkName);
    }

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, AsyncUnaryCallContinuation<TRequest, TResponse> continuation) {
        string methodName = "Unary";

        if (!_options.GlobalClientInterceptorEnabled) {
            return continuation(request, context);
        }

        if (!Helpers.ShouldApplyInterceptor(methodName, context.Method.Type)) {
            return continuation(request, context);
        }

        if (Helpers.ShouldSkipInterceptor(context.Options.Headers)) {
            _ = context.Options.Headers.Remove(Constants.K_SKIP_PROTO_INTERCEPTOR);
            return continuation(request, context);
        }

        if (_options.CaptureMode.HasFlag(ProtoCaptureMode.Request)) {
            _context.Direction = ProtoDirectionType.Outbound.ToString();
            _context.Phase = ProtoPhaseType.Request.ToString();
            _ = TraceMessage(request, _options.LogScope.HasFlag(ProtoLogScope.Request), methodName, context.Method.Type).ConfigureAwait(false);
        }

        var call = continuation(request, context);
        var response = call.ResponseAsync;

        if (_options.CaptureMode.HasFlag(ProtoCaptureMode.Response)) {
            _context.Direction = ProtoDirectionType.Inbound.ToString();
            _context.Phase = ProtoPhaseType.Response.ToString();
            _ = TraceMessage(response, _options.LogScope.HasFlag(ProtoLogScope.Response), methodName, context.Method.Type).ConfigureAwait(false);
        }

        return new AsyncUnaryCall<TResponse>(response, call.ResponseHeadersAsync, call.GetStatus, call.GetTrailers, call.Dispose);
    }

    private async Task TraceMessage<TMessage>(TMessage target, bool log, string methodName, MethodType methodType) where TMessage : class {
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
