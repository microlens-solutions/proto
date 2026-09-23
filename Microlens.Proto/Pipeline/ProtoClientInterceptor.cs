using Grpc.Core;
using Grpc.Core.Interceptors;
using Microlens.Proto.Shared;
using Microlens.Proto.Tracers;
using System;
using System.Threading.Tasks;

namespace Microlens.Proto.Pipeline;

internal sealed class ProtoClientInterceptor : Interceptor {
    private readonly ProtoTracer _tracer;

    internal ProtoClientInterceptor(ProtoTracer tracer) {
        _tracer = tracer;
    }

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, AsyncUnaryCallContinuation<TRequest, TResponse> continuation) {
        if (!ShouldIntercept(context)) {
            return continuation(request, context);
        }

        string path = context.Method.FullName;

        if (_tracer.TraceRequest) {
            _ = _tracer.TraceAsync(request, Registry.ProtoChannelType.Grpc, Registry.ProtoDirectionType.Outbound, Registry.ProtoPhaseType.Request, path);
        }

        AsyncUnaryCall<TResponse> call = continuation(request, context);

        if (_tracer.TraceResponse) {
            _ = TraceResponseAsync(call.ResponseAsync, path);
        }

        return call;
    }

    public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation) {
        if (!ShouldIntercept(context)) {
            return continuation(request, context);
        }

        string path = context.Method.FullName;

        if (_tracer.TraceRequest) {
            _ = _tracer.TraceAsync(request, Registry.ProtoChannelType.Grpc, Registry.ProtoDirectionType.Outbound, Registry.ProtoPhaseType.Request, path);
        }

        AsyncServerStreamingCall<TResponse> call = continuation(request, context);

        if (!_tracer.TraceResponse) {
            return call;
        }

        var responseStream = new ProtoStreamReader<TResponse>(call.ResponseStream, CreateTracer<TResponse>(Registry.ProtoDirectionType.Inbound, Registry.ProtoPhaseType.Response, path));
        return new AsyncServerStreamingCall<TResponse>(responseStream, call.ResponseHeadersAsync, call.GetStatus, call.GetTrailers, call.Dispose);
    }

    public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(ClientInterceptorContext<TRequest, TResponse> context, AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation) {
        if (!ShouldIntercept(context)) {
            return continuation(context);
        }

        string path = context.Method.FullName;
        AsyncClientStreamingCall<TRequest, TResponse> call = continuation(context);

        if (_tracer.TraceResponse) {
            _ = TraceResponseAsync(call.ResponseAsync, path);
        }

        if (!_tracer.TraceRequest) {
            return call;
        }

        var requestStream = new ProtoClientStreamWriter<TRequest>(call.RequestStream, CreateTracer<TRequest>(Registry.ProtoDirectionType.Outbound, Registry.ProtoPhaseType.Request, path));
        return new AsyncClientStreamingCall<TRequest, TResponse>(requestStream, call.ResponseAsync, call.ResponseHeadersAsync, call.GetStatus, call.GetTrailers, call.Dispose);
    }

    public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(ClientInterceptorContext<TRequest, TResponse> context, AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation) {
        if (!ShouldIntercept(context)) {
            return continuation(context);
        }

        string path = context.Method.FullName;
        AsyncDuplexStreamingCall<TRequest, TResponse> call = continuation(context);

        IClientStreamWriter<TRequest> requestStream = _tracer.TraceRequest
            ? new ProtoClientStreamWriter<TRequest>(call.RequestStream, CreateTracer<TRequest>(Registry.ProtoDirectionType.Outbound, Registry.ProtoPhaseType.Request, path))
            : call.RequestStream;

        IAsyncStreamReader<TResponse> responseStream = _tracer.TraceResponse
            ? new ProtoStreamReader<TResponse>(call.ResponseStream, CreateTracer<TResponse>(Registry.ProtoDirectionType.Inbound, Registry.ProtoPhaseType.Response, path))
            : call.ResponseStream;

        return new AsyncDuplexStreamingCall<TRequest, TResponse>(requestStream, responseStream, call.ResponseHeadersAsync, call.GetStatus, call.GetTrailers, call.Dispose);
    }

    private async Task TraceResponseAsync<TResponse>(Task<TResponse> responseTask, string path) {
        try {
            TResponse response = await responseTask.ConfigureAwait(false);
            await _tracer.TraceAsync(response, Registry.ProtoChannelType.Grpc, Registry.ProtoDirectionType.Inbound, Registry.ProtoPhaseType.Response, path).ConfigureAwait(false);
        }
        catch { }
    }

    private Func<TMessage, Task> CreateTracer<TMessage>(Registry.ProtoDirectionType direction, Registry.ProtoPhaseType phase, string path) where TMessage : class {
        return message => _tracer.TraceAsync(message, Registry.ProtoChannelType.Grpc, direction, phase, path);
    }

    private bool ShouldIntercept<TRequest, TResponse>(ClientInterceptorContext<TRequest, TResponse> context) where TRequest : class where TResponse : class {
        return !Helpers.TryConsumeSkipHeader(context.Options.Headers) && _tracer.Options.GlobalClientInterceptorEnabled && _tracer.IsActive;
    }
}
