using Microsoft.IO;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microlens.Proto.Pipeline;

internal sealed class ProtoBufferedContent : HttpContent {
    private readonly HttpContent _original;

    private readonly RecyclableMemoryStream _buffer;

    internal ProtoBufferedContent(HttpContent original, RecyclableMemoryStream buffer) {
        _original = original;
        _buffer = buffer;

        foreach (var header in original.Headers) {
            _ = Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
    }

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) {
        return WriteToAsync(stream, CancellationToken.None);
    }

#if NET5_0_OR_GREATER
    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken) {
        return WriteToAsync(stream, cancellationToken);
    }
#endif

    protected override Task<Stream> CreateContentReadStreamAsync() {
        return Task.FromResult<Stream>(new MemoryStream(_buffer.GetBuffer(), 0, checked((int)_buffer.Length), writable: false));
    }

    protected override bool TryComputeLength(out long length) {
        length = _buffer.Length;
        return true;
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            _buffer.Dispose();
            _original.Dispose();
        }

        base.Dispose(disposing);
    }

    private Task WriteToAsync(Stream stream, CancellationToken cancellationToken) {
        return stream.WriteAsync(_buffer.GetBuffer(), 0, checked((int)_buffer.Length), cancellationToken);
    }
}
