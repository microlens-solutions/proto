using Grpc.Core;
using Grpc.Core.Interceptors;
using Microlens.Proto.Shared;
using Microlens.Proto.Tracers;
using Microsoft.AspNetCore.Http;

namespace Microlens.Proto.Pipeline;

internal sealed class ProtoServerInterceptor : Interceptor {
    private readonly ProtoTracer _tracer;

    internal ProtoServerInterceptor(ProtoTracer tracer) {
        _tracer = tracer;
    }

    public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation) {
        return ShouldIntercept(context) ? InterceptUnaryAsync(request, context, continuation) : continuation(request, context);
    }

    public override Task ServerStreamingServerHandler<TRequest, TResponse>(TRequest request, IServerStreamWriter<TResponse> responseStream, ServerCallContext context, ServerStreamingServerMethod<TRequest, TResponse> continuation) {
        return ShouldIntercept(context) ? InterceptServerStreamingAsync(request, responseStream, context, continuation) : continuation(request, responseStream, context);
    }

    public override Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream, ServerCallContext context, ClientStreamingServerMethod<TRequest, TResponse> continuation) {
        return ShouldIntercept(context) ? InterceptClientStreamingAsync(requestStream, context, continuation) : continuation(requestStream, context);
    }

    public override Task DuplexStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream, IServerStreamWriter<TResponse> responseStream, ServerCallContext context, DuplexStreamingServerMethod<TRequest, TResponse> continuation) {
        if (!ShouldIntercept(context)) {
            return continuation(requestStream, responseStream, context);
        }

        IAsyncStreamReader<TRequest> reader = _tracer.TraceRequest
            ? new ProtoStreamReader<TRequest>(requestStream, CreateTracer<TRequest>(Registry.ProtoDirectionType.Inbound, Registry.ProtoPhaseType.Request, context.Method))
            : requestStream;

        IServerStreamWriter<TResponse> writer = _tracer.TraceResponse
            ? new ProtoServerStreamWriter<TResponse>(responseStream, CreateTracer<TResponse>(Registry.ProtoDirectionType.Outbound, Registry.ProtoPhaseType.Response, context.Method))
            : responseStream;

        return continuation(reader, writer, context);
    }

    private async Task<TResponse> InterceptUnaryAsync<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation) where TRequest : class where TResponse : class {
        if (_tracer.TraceRequest) {
            await _tracer.TraceAsync(request, Registry.ProtoChannelType.Grpc, Registry.ProtoDirectionType.Inbound, Registry.ProtoPhaseType.Request, context.Method).ConfigureAwait(false);
        }

        TResponse response = await continuation(request, context).ConfigureAwait(false);

        if (_tracer.TraceResponse) {
            await _tracer.TraceAsync(response, Registry.ProtoChannelType.Grpc, Registry.ProtoDirectionType.Outbound, Registry.ProtoPhaseType.Response, context.Method).ConfigureAwait(false);
        }

        return response;
    }

    private async Task InterceptServerStreamingAsync<TRequest, TResponse>(TRequest request, IServerStreamWriter<TResponse> responseStream, ServerCallContext context, ServerStreamingServerMethod<TRequest, TResponse> continuation) where TRequest : class where TResponse : class {
        if (_tracer.TraceRequest) {
            await _tracer.TraceAsync(request, Registry.ProtoChannelType.Grpc, Registry.ProtoDirectionType.Inbound, Registry.ProtoPhaseType.Request, context.Method).ConfigureAwait(false);
        }

        IServerStreamWriter<TResponse> writer = _tracer.TraceResponse
            ? new ProtoServerStreamWriter<TResponse>(responseStream, CreateTracer<TResponse>(Registry.ProtoDirectionType.Outbound, Registry.ProtoPhaseType.Response, context.Method))
            : responseStream;

        await continuation(request, writer, context).ConfigureAwait(false);
    }

    private async Task<TResponse> InterceptClientStreamingAsync<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream, ServerCallContext context, ClientStreamingServerMethod<TRequest, TResponse> continuation) where TRequest : class where TResponse : class {
        IAsyncStreamReader<TRequest> reader = _tracer.TraceRequest
            ? new ProtoStreamReader<TRequest>(requestStream, CreateTracer<TRequest>(Registry.ProtoDirectionType.Inbound, Registry.ProtoPhaseType.Request, context.Method))
            : requestStream;

        TResponse response = await continuation(reader, context).ConfigureAwait(false);

        if (_tracer.TraceResponse) {
            await _tracer.TraceAsync(response, Registry.ProtoChannelType.Grpc, Registry.ProtoDirectionType.Outbound, Registry.ProtoPhaseType.Response, context.Method).ConfigureAwait(false);
        }

        return response;
    }

    private Func<TMessage, Task> CreateTracer<TMessage>(Registry.ProtoDirectionType direction, Registry.ProtoPhaseType phase, string path) where TMessage : class {
        return message => _tracer.TraceAsync(message, Registry.ProtoChannelType.Grpc, direction, phase, path);
    }

    private bool ShouldIntercept(ServerCallContext context) {
        if (!_tracer.Options.GlobalServerInterceptorEnabled || !_tracer.IsActive) {
            return false;
        }

        HttpContext http = context.GetHttpContext();
        return Helpers.IsGrpc(http.Request.ContentType) && !Helpers.ShouldSkipInterceptor(http.GetEndpoint()?.Metadata);
    }
}
