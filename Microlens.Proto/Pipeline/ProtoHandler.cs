using Microlens.Proto.Shared;
using Microlens.Proto.Tracers;
using Microsoft.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microlens.Proto.Pipeline;

internal sealed class ProtoHandler : DelegatingHandler {
    private readonly ProtoTracer _tracer;

    internal ProtoHandler(ProtoTracer tracer) {
        _tracer = tracer;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        if (Helpers.TryConsumeSkipHeader(request) || !_tracer.Options.GlobalHandlerEnabled || !_tracer.IsActive) {
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        string path = GetPath(request);

        if (_tracer.TraceRequest && request.Content is { } content && Helpers.IsProtobuf(content.Headers.ContentType?.MediaType) && _tracer.CanCapture(content.Headers.ContentLength)) {
            RecyclableMemoryStream buffer = await BufferAsync(content, cancellationToken).ConfigureAwait(false);
            request.Content = new ProtoBufferedContent(content, buffer);
            await _tracer.TraceAsync(buffer.GetReadOnlySequence(), Registry.ProtoChannelType.Http, Registry.ProtoDirectionType.Outbound, Registry.ProtoPhaseType.Request, path, cancellationToken).ConfigureAwait(false);
        }

        HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (_tracer.TraceResponse && response.Content is { } body && Helpers.IsProtobuf(body.Headers.ContentType?.MediaType) && _tracer.CanCapture(body.Headers.ContentLength)) {
            RecyclableMemoryStream buffer;

            try {
                buffer = await BufferAsync(body, cancellationToken).ConfigureAwait(false);
            }
            catch {
                response.Dispose();
                throw;
            }

            response.Content = new ProtoBufferedContent(body, buffer);
            await _tracer.TraceAsync(buffer.GetReadOnlySequence(), Registry.ProtoChannelType.Http, Registry.ProtoDirectionType.Inbound, Registry.ProtoPhaseType.Response, path, cancellationToken).ConfigureAwait(false);
        }

        return response;
    }

    private static async Task<RecyclableMemoryStream> BufferAsync(HttpContent content, CancellationToken cancellationToken) {
        RecyclableMemoryStream buffer = ProtoTracer.Streams.GetStream();

        try {
#if NET5_0_OR_GREATER
            await content.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
#else
            cancellationToken.ThrowIfCancellationRequested();
            await content.CopyToAsync(buffer).ConfigureAwait(false);
#endif
            return buffer;
        }
        catch {
            buffer.Dispose();
            throw;
        }
    }

    private static string GetPath(HttpRequestMessage request) {
        return request.RequestUri is null ? string.Empty : request.RequestUri.IsAbsoluteUri ? request.RequestUri.AbsolutePath : request.RequestUri.OriginalString;
    }
}
