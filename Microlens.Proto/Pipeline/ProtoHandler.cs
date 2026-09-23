using Microlens.Proto.Shared;
using Microlens.Proto.Tracers;
using Microsoft.IO;
using System;
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

        if (_tracer.TraceRequest && request.Content is { } content && Helpers.IsProtobuf(content.Headers.ContentType?.MediaType)) {
            await TraceAsync(content, Registry.ProtoDirectionType.Outbound, Registry.ProtoPhaseType.Request, GetPath(request), cancellationToken).ConfigureAwait(false);
        }

        HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (_tracer.TraceResponse && Helpers.IsProtobuf(response.Content.Headers.ContentType?.MediaType)) {
            await TraceAsync(response.Content, Registry.ProtoDirectionType.Inbound, Registry.ProtoPhaseType.Response, GetPath(request), cancellationToken).ConfigureAwait(false);
        }

        return response;
    }

    private async Task TraceAsync(HttpContent content, Registry.ProtoDirectionType direction, Registry.ProtoPhaseType phase, string path, CancellationToken cancellationToken) {
        try {
#if NET9_0_OR_GREATER
            await content.LoadIntoBufferAsync(cancellationToken).ConfigureAwait(false);
#else
            await content.LoadIntoBufferAsync().ConfigureAwait(false);
#endif

            using RecyclableMemoryStream buffer = ProtoTracer.Streams.GetStream();
            await content.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
            await _tracer.TraceAsync(buffer.GetReadOnlySequence(), Registry.ProtoChannelType.Http, direction, phase, path, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested) { }
    }

    private static string GetPath(HttpRequestMessage request) {
        return request.RequestUri is null ? string.Empty : request.RequestUri.IsAbsoluteUri ? request.RequestUri.AbsolutePath : request.RequestUri.OriginalString;
    }
}
