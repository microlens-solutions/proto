using Microlens.Proto.Shared;
using Microlens.Proto.Tracers;
using Microsoft.AspNetCore.Http;
using Microsoft.IO;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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

        if (traceRequest && _tracer.CanCapture(request.ContentLength)) {
            RecyclableMemoryStream requestBuffer = ProtoTracer.Streams.GetStream();
            context.Response.RegisterForDispose(requestBuffer);
            await request.Body.CopyToAsync(requestBuffer, aborted).ConfigureAwait(false);
            requestBuffer.Position = 0;
            request.Body = requestBuffer;
            await _tracer.TraceAsync(requestBuffer.GetReadOnlySequence(), Registry.ProtoChannelType.Http, Registry.ProtoDirectionType.Inbound, Registry.ProtoPhaseType.Request, path, aborted).ConfigureAwait(false);
        }

        if (!traceResponse) {
            await _next(context).ConfigureAwait(false);
            return;
        }

        HttpResponse response = context.Response;
        Stream original = response.Body;
        using var capture = new ProtoCaptureStream(original, response, _tracer.Options.MaximumBytesCaptured);
        response.Body = capture;

        try {
            await _next(context).ConfigureAwait(false);
        }
        finally {
            response.Body = original;
        }

        if (capture.TryGetPayload(out ReadOnlySequence<byte> payload)) {
            await _tracer.TraceAsync(payload, Registry.ProtoChannelType.Http, Registry.ProtoDirectionType.Outbound, Registry.ProtoPhaseType.Response, path, aborted).ConfigureAwait(false);
        }
    }
}
