using Google.Protobuf;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microlens.Proto.Formatters;
using Microlens.Proto.Inspectors;
using Microlens.Proto.Models;
using Microlens.Proto.Shared;
using Microlens.Proto.Sinks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;

namespace Microlens.Proto.Pipeline;

internal sealed class ProtoServerInterceptor : Interceptor {
    private readonly ProtoOptions _options;

    private readonly IProtoContext _context;

    private readonly IProtoInspector _inspector;

    private readonly IProtoFormatter _formatter;

    private readonly IProtoSink _sink;

    internal ProtoServerInterceptor(IOptions<ProtoOptions> options, IProtoContext context, IProtoInspector inspector, IProtoFormatterResolver formatter, IProtoSinkResolver sink) {
        _options = options.Value;
        _context = context;
        _inspector = inspector;
        _formatter = formatter.Get(_options.CustomFormatterName);
        _sink = sink.Get(_options.CustomSinkName);
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation) {
        if (!ShouldIntercept(context)) {
            return await continuation(request, context).ConfigureAwait(false);
        }

        _context.Channel = ProtoChannelType.Grpc.ToString();
        _context.Path = context.GetHttpContext().Request.GetDisplayUrl();

        if (_options.CaptureMode.HasFlag(ProtoCaptureMode.Request)) {
            _context.Direction = ProtoDirectionType.Inbound.ToString();
            _context.Phase = ProtoPhaseType.Request.ToString();
            await TraceMessage(request, _options.LogScope.HasFlag(ProtoLogScope.Request)).ConfigureAwait(false);
        }

        TResponse response = await continuation(request, context).ConfigureAwait(false);

        if (_options.CaptureMode.HasFlag(ProtoCaptureMode.Response)) {
            _context.Direction = ProtoDirectionType.Outbound.ToString();
            _context.Phase = ProtoPhaseType.Response.ToString();
            await TraceMessage(response, _options.LogScope.HasFlag(ProtoLogScope.Response)).ConfigureAwait(false);
        }

        return response;
    }

    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(TRequest request, IServerStreamWriter<TResponse> responseStream, ServerCallContext context, ServerStreamingServerMethod<TRequest, TResponse> continuation) {
        if (!ShouldIntercept(context)) {
            await continuation(request, responseStream, context).ConfigureAwait(false);
            return;
        }

        _context.Channel = ProtoChannelType.Grpc.ToString();
        _context.Path = context.GetHttpContext().Request.GetDisplayUrl();

        if (_options.CaptureMode.HasFlag(ProtoCaptureMode.Request)) {
            _context.Direction = ProtoDirectionType.Inbound.ToString();
            _context.Phase = ProtoPhaseType.Request.ToString();
            await TraceMessage(request, _options.LogScope.HasFlag(ProtoLogScope.Request)).ConfigureAwait(false);
        }

        IServerStreamWriter<TResponse> tracedResponseStream = responseStream;

        if (_options.CaptureMode.HasFlag(ProtoCaptureMode.Response)) {
            var tracer = CreateStreamTracer<TResponse>(_context.Channel, _context.Path, ProtoDirectionType.Outbound, ProtoPhaseType.Response, _options.LogScope.HasFlag(ProtoLogScope.Response));
            tracedResponseStream = new ProtoServerStreamWriter<TResponse>(responseStream, tracer);
        }

        await continuation(request, tracedResponseStream, context).ConfigureAwait(false);
    }

    public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream, ServerCallContext context, ClientStreamingServerMethod<TRequest, TResponse> continuation) {
        if (!ShouldIntercept(context)) {
            return await continuation(requestStream, context).ConfigureAwait(false);
        }

        _context.Channel = ProtoChannelType.Grpc.ToString();
        _context.Path = context.GetHttpContext().Request.GetDisplayUrl();

        IAsyncStreamReader<TRequest> tracedRequestStream = requestStream;

        if (_options.CaptureMode.HasFlag(ProtoCaptureMode.Request)) {
            var tracer = CreateStreamTracer<TRequest>(_context.Channel, _context.Path, ProtoDirectionType.Inbound, ProtoPhaseType.Request, _options.LogScope.HasFlag(ProtoLogScope.Request));
            tracedRequestStream = new ProtoStreamReader<TRequest>(requestStream, tracer);
        }

        TResponse response = await continuation(tracedRequestStream, context).ConfigureAwait(false);

        if (_options.CaptureMode.HasFlag(ProtoCaptureMode.Response)) {
            _context.Direction = ProtoDirectionType.Outbound.ToString();
            _context.Phase = ProtoPhaseType.Response.ToString();
            await TraceMessage(response, _options.LogScope.HasFlag(ProtoLogScope.Response)).ConfigureAwait(false);
        }

        return response;
    }

    public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream, IServerStreamWriter<TResponse> responseStream, ServerCallContext context, DuplexStreamingServerMethod<TRequest, TResponse> continuation) {
        if (!ShouldIntercept(context)) {
            await continuation(requestStream, responseStream, context).ConfigureAwait(false);
            return;
        }

        _context.Channel = ProtoChannelType.Grpc.ToString();
        _context.Path = context.GetHttpContext().Request.GetDisplayUrl();

        IAsyncStreamReader<TRequest> tracedRequestStream = requestStream;
        IServerStreamWriter<TResponse> tracedResponseStream = responseStream;

        if (_options.CaptureMode.HasFlag(ProtoCaptureMode.Request)) {
            var requestTracer = CreateStreamTracer<TRequest>(_context.Channel, _context.Path, ProtoDirectionType.Inbound, ProtoPhaseType.Request, _options.LogScope.HasFlag(ProtoLogScope.Request));
            tracedRequestStream = new ProtoStreamReader<TRequest>(requestStream, requestTracer);
        }

        if (_options.CaptureMode.HasFlag(ProtoCaptureMode.Response)) {
            var responseTracer = CreateStreamTracer<TResponse>(_context.Channel, _context.Path, ProtoDirectionType.Outbound, ProtoPhaseType.Response, _options.LogScope.HasFlag(ProtoLogScope.Response));
            tracedResponseStream = new ProtoServerStreamWriter<TResponse>(responseStream, responseTracer);
        }

        await continuation(tracedRequestStream, tracedResponseStream, context).ConfigureAwait(false);
    }

    private async Task TraceMessage<TMessage>(TMessage target, bool log) where TMessage : class {
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

    private Func<TMessage, Task> CreateStreamTracer<TMessage>(string channel, string? path, ProtoDirectionType direction, ProtoPhaseType phase, bool log) where TMessage : class {
        return message => TraceStreamItem(message, channel, path, direction, phase, log);
    }

    private async Task TraceStreamItem<TMessage>(TMessage target, string channel, string? path, ProtoDirectionType direction, ProtoPhaseType phase, bool log) where TMessage : class {
        try {
            if (target is IMessage message) {
                var nodes = _inspector.Inspect(message);
                string description = _formatter.Format(nodes);

                if (log) {
                    var messageContext = new ProtoContext {
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

    private bool ShouldIntercept(ServerCallContext context) {
        return _options.GlobalServerInterceptorEnabled && Helpers.ShouldApplyInterceptor(context.GetHttpContext().Request.ContentType) && !Helpers.ShouldSkipInterceptor(context.GetHttpContext()?.GetEndpoint()?.Metadata);
    }
}
