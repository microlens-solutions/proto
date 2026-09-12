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

    private readonly IProtoInspector _inspector;

    private readonly IProtoFormatter _formatter;

    private readonly IProtoSink _sink;

    internal ProtoClientInterceptor(IOptions<ProtoOptions> options, IProtoInspector inspector, IProtoFormatterResolver formatter, IProtoSinkResolver sink) {
        _options = options.Value;
        _inspector = inspector;
        _formatter = formatter.Get(_options.CustomFormatterName);
        _sink = sink.Get(_options.CustomSinkName);
    }

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, AsyncUnaryCallContinuation<TRequest, TResponse> continuation) {
        string methodName = "Unary";

        if (!ShouldIntercept(context, methodName)) {
            return continuation(request, context);
        }

        var scope = Helpers.BuildGrpcScope(context.Method.FullName);

        if (_options.CaptureMode.HasFlag(ProtoCaptureMode.Request)) {
            scope.Direction = ProtoDirectionType.Outbound.ToString();
            scope.Phase = ProtoPhaseType.Request.ToString();
            _ = TraceMessage(scope, request, _options.LogScope.HasFlag(ProtoLogScope.Request)).ConfigureAwait(false);
        }

        var call = continuation(request, context);
        var response = call.ResponseAsync;

        if (_options.CaptureMode.HasFlag(ProtoCaptureMode.Response)) {
            scope.Direction = ProtoDirectionType.Inbound.ToString();
            scope.Phase = ProtoPhaseType.Response.ToString();
            _ = TraceResponseAsync(scope, response, _options.LogScope.HasFlag(ProtoLogScope.Response));
        }

        return new AsyncUnaryCall<TResponse>(response, call.ResponseHeadersAsync, call.GetStatus, call.GetTrailers, call.Dispose);
    }

    public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation) {
        string methodName = "ServerStreaming";

        if (!ShouldIntercept(context, methodName)) {
            return continuation(request, context);
        }

        var scope = Helpers.BuildGrpcScope(context.Method.FullName);

        if (_options.CaptureMode.HasFlag(ProtoCaptureMode.Request)) {
            scope.Direction = ProtoDirectionType.Outbound.ToString();
            scope.Phase = ProtoPhaseType.Request.ToString();
            _ = TraceMessage(scope, request, _options.LogScope.HasFlag(ProtoLogScope.Request)).ConfigureAwait(false);
        }

        var call = continuation(request, context);
        IAsyncStreamReader<TResponse> responseStream = call.ResponseStream;

        if (_options.CaptureMode.HasFlag(ProtoCaptureMode.Response)) {
            var tracer = CreateStreamTracer<TResponse>(scope.Channel, scope.Path, ProtoDirectionType.Inbound, ProtoPhaseType.Response, _options.LogScope.HasFlag(ProtoLogScope.Response));
            responseStream = new ProtoStreamReader<TResponse>(responseStream, tracer);
        }

        return new AsyncServerStreamingCall<TResponse>(responseStream, call.ResponseHeadersAsync, call.GetStatus, call.GetTrailers, call.Dispose);
    }

    public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(ClientInterceptorContext<TRequest, TResponse> context, AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation) {
        string methodName = "ClientStreaming";

        if (!ShouldIntercept(context, methodName)) {
            return continuation(context);
        }

        var scope = Helpers.BuildGrpcScope(context.Method.FullName);

        var call = continuation(context);
        IClientStreamWriter<TRequest> requestStream = call.RequestStream;

        if (_options.CaptureMode.HasFlag(ProtoCaptureMode.Request)) {
            var tracer = CreateStreamTracer<TRequest>(scope.Channel, scope.Path, ProtoDirectionType.Outbound, ProtoPhaseType.Request, _options.LogScope.HasFlag(ProtoLogScope.Request));
            requestStream = new ProtoClientStreamWriter<TRequest>(requestStream, tracer);
        }

        var response = call.ResponseAsync;

        if (_options.CaptureMode.HasFlag(ProtoCaptureMode.Response)) {
            _ = TraceResponseAsync(scope, response, _options.LogScope.HasFlag(ProtoLogScope.Response));
        }

        return new AsyncClientStreamingCall<TRequest, TResponse>(requestStream, response, call.ResponseHeadersAsync, call.GetStatus, call.GetTrailers, call.Dispose);
    }

    public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(ClientInterceptorContext<TRequest, TResponse> context, AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation) {
        string methodName = "DuplexStreaming";

        if (!ShouldIntercept(context, methodName)) {
            return continuation(context);
        }

        var scope = Helpers.BuildGrpcScope(context.Method.FullName);

        var call = continuation(context);
        IClientStreamWriter<TRequest> requestStream = call.RequestStream;
        IAsyncStreamReader<TResponse> responseStream = call.ResponseStream;

        if (_options.CaptureMode.HasFlag(ProtoCaptureMode.Request)) {
            var requestTracer = CreateStreamTracer<TRequest>(scope.Channel, scope.Path, ProtoDirectionType.Outbound, ProtoPhaseType.Request, _options.LogScope.HasFlag(ProtoLogScope.Request));
            requestStream = new ProtoClientStreamWriter<TRequest>(requestStream, requestTracer);
        }

        if (_options.CaptureMode.HasFlag(ProtoCaptureMode.Response)) {
            var responseTracer = CreateStreamTracer<TResponse>(scope.Channel, scope.Path, ProtoDirectionType.Inbound, ProtoPhaseType.Response, _options.LogScope.HasFlag(ProtoLogScope.Response));
            responseStream = new ProtoStreamReader<TResponse>(responseStream, responseTracer);
        }

        return new AsyncDuplexStreamingCall<TRequest, TResponse>(requestStream, responseStream, call.ResponseHeadersAsync, call.GetStatus, call.GetTrailers, call.Dispose);
    }

    private async Task TraceMessage<TMessage>(IProtoScope scope, TMessage target, bool log) where TMessage : class {
        try {
            if (target is IMessage message) {
                var nodes = _inspector.Inspect(message);
                string description = _formatter.Format(nodes);

                if (log) {
                    scope.TimestampUtc = DateTime.UtcNow;
                    await _sink.LogAsync(_options.LogLevel, scope, description, CancellationToken.None).ConfigureAwait(false);
                }
            }
        }
        catch { }
    }

    private async Task TraceResponseAsync<TResponse>(IProtoScope scope, Task<TResponse> responseTask, bool log) where TResponse : class {
        try {
            TResponse response = await responseTask.ConfigureAwait(false);
            await TraceMessage(scope, response, log).ConfigureAwait(false);
        }
        catch { }
    }

    private Func<TMessage, Task> CreateStreamTracer<TMessage>(string channel, string? path, ProtoDirectionType direction, ProtoPhaseType phase, bool log) where TMessage : class {
        return message => TraceStreamItem(message, channel, path, direction, phase, log);
    }

    private async Task TraceStreamItem<TMessage>(TMessage target, string channel, string? path, ProtoDirectionType direction, ProtoPhaseType phase, bool log) where TMessage : class {
        try {
            if (target is IMessage message) {
                var nodes = _inspector.Inspect(message);
                string description = _formatter.Format(nodes);

                if (log) {
                    var messageContext = new ProtoScope {
                        TimestampUtc = DateTime.UtcNow,
                        Channel = channel,
                        Direction = direction.ToString(),
                        Phase = phase.ToString(),
                        Path = path
                    };

                    await _sink.LogAsync(_options.LogLevel, messageContext, description, CancellationToken.None).ConfigureAwait(false);
                }
            }
        }
        catch { }
    }

    private bool ShouldIntercept<TRequest, TResponse>(ClientInterceptorContext<TRequest, TResponse> context, string methodName) where TRequest : class where TResponse : class {
        if (!_options.GlobalClientInterceptorEnabled) {
            return false;
        }

        if (!Helpers.ShouldApplyInterceptor(methodName, context.Method.Type)) {
            return false;
        }

        if (Helpers.ShouldSkipInterceptor(context.Options.Headers)) {
            _ = context.Options.Headers.Remove(Constants.K_SKIP_PROTO_INTERCEPTOR);
            return false;
        }

        return true;
    }
}
