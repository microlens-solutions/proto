using Microlens.Proto.Shared;
using Microlens.Proto.Tracers;
using Microsoft.AspNetCore.Http;
using Microsoft.IO;

namespace Microlens.Proto.Pipeline;

internal sealed class ProtoMiddleware {
    private readonly RequestDelegate _next;

    private readonly ProtoTracer _tracer;

    internal ProtoMiddleware(RequestDelegate next, ProtoTracer tracer) {
        _next = next;
        _tracer = tracer;
    }

    public Task InvokeAsync(HttpContext context) {
        if (!_tracer.Options.GlobalMiddlewareEnabled) {
            return _next(context);
        }

        HttpRequest request = context.Request;
        bool protobufRequest = Helpers.IsProtobuf(request.ContentType);
        bool traceRequest = _tracer.TraceRequest && protobufRequest;
        bool traceResponse = _tracer.TraceResponse && (protobufRequest || Helpers.AcceptsProtobuf(request.Headers.Accept));

        return (!traceRequest && !traceResponse) || !_tracer.IsActive || Helpers.ShouldSkipMiddleware(context.GetEndpoint()?.Metadata)
            ? _next(context)
            : InterceptAsync(context, traceRequest, traceResponse);
    }

    private async Task InterceptAsync(HttpContext context, bool traceRequest, bool traceResponse) {
        HttpRequest request = context.Request;
        string path = string.Concat(request.PathBase.Value, request.Path.Value);
        CancellationToken aborted = context.RequestAborted;

        if (traceRequest) {
            request.EnableBuffering();

            using RecyclableMemoryStream requestBuffer = ProtoTracer.Streams.GetStream();
            await request.Body.CopyToAsync(requestBuffer, aborted).ConfigureAwait(false);

            request.Body.Position = 0;
            await _tracer.TraceAsync(requestBuffer.GetReadOnlySequence(), Registry.ProtoChannelType.Http, Registry.ProtoDirectionType.Inbound, Registry.ProtoPhaseType.Request, path, aborted).ConfigureAwait(false);
        }

        if (!traceResponse) {
            await _next(context).ConfigureAwait(false);
            return;
        }

        HttpResponse response = context.Response;
        Stream original = response.Body;
        using RecyclableMemoryStream responseBuffer = ProtoTracer.Streams.GetStream();
        response.Body = responseBuffer;

        try {
            await _next(context).ConfigureAwait(false);
        }
        finally {
            response.Body = original;
        }

        if (responseBuffer.Length > 0) {
            responseBuffer.Position = 0;
            await responseBuffer.CopyToAsync(original, aborted).ConfigureAwait(false);
        }

        if (Helpers.IsProtobuf(response.ContentType)) {
            await _tracer.TraceAsync(responseBuffer.GetReadOnlySequence(), Registry.ProtoChannelType.Http, Registry.ProtoDirectionType.Outbound, Registry.ProtoPhaseType.Response, path, aborted).ConfigureAwait(false);
        }
    }
}
