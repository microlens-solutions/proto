using Microlens.Proto.Shared;
using Microlens.Proto.Tracers;
using Microsoft.AspNetCore.Http;
using Microsoft.IO;
using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microlens.Proto.Pipeline;

internal sealed class ProtoCaptureStream : Stream {
    private readonly Stream _inner;

    private readonly HttpResponse _response;

    private readonly long? _limit;

    private RecyclableMemoryStream? _buffer;

    private int _state;

    internal ProtoCaptureStream(Stream inner, HttpResponse response, long? limit) {
        _inner = inner;
        _response = response;
        _limit = limit;
    }

    public override bool CanRead => false;

    public override bool CanSeek => false;

    public override bool CanWrite => true;

    public override long Length => throw new NotSupportedException();

    public override long Position {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush() {
        _inner.Flush();
    }

    public override Task FlushAsync(CancellationToken cancellationToken) {
        return _inner.FlushAsync(cancellationToken);
    }

    public override int Read(byte[] buffer, int offset, int count) {
        throw new NotSupportedException();
    }

    public override long Seek(long offset, SeekOrigin origin) {
        throw new NotSupportedException();
    }

    public override void SetLength(long value) {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count) {
        Capture(buffer.AsSpan(offset, count));
        _inner.Write(buffer, offset, count);
    }

    public override void Write(ReadOnlySpan<byte> buffer) {
        Capture(buffer);
        _inner.Write(buffer);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
        Capture(buffer.AsSpan(offset, count));
        return _inner.WriteAsync(buffer, offset, count, cancellationToken);
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) {
        Capture(buffer.Span);
        return _inner.WriteAsync(buffer, cancellationToken);
    }

    internal bool TryGetPayload(out ReadOnlySequence<byte> payload) {
        Decide();

        if (_state != (int)Registry.StreamStateType.Capturing) {
            payload = default;
            return false;
        }

        payload = _buffer is null ? ReadOnlySequence<byte>.Empty : _buffer.GetReadOnlySequence();
        return true;
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            _buffer?.Dispose();
            _buffer = null;
        }

        base.Dispose(disposing);
    }

    private void Decide() {
        if (_state == (int)Registry.StreamStateType.Undecided) {
            _state = Helpers.IsProtobuf(_response.ContentType) ? (int)Registry.StreamStateType.Capturing : (int)Registry.StreamStateType.Bypassed;
        }
    }

    private void Capture(ReadOnlySpan<byte> data) {
        Decide();

        if (_state != (int)Registry.StreamStateType.Capturing || data.IsEmpty) {
            return;
        }

        _buffer ??= ProtoTracer.Streams.GetStream();

        if (_limit is { } limit && _buffer.Length + data.Length > limit) {
            _state = (int)Registry.StreamStateType.Bypassed;
            _buffer.Dispose();
            _buffer = null;
            return;
        }

        _buffer.Write(data);
    }
}
