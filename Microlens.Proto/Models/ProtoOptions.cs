using Microlens.Proto.Shared;
using Microsoft.Extensions.Logging;
using System;

namespace Microlens.Proto.Models;

public sealed class ProtoOptions {
    public ProtoRegistry.FormatterKind Formatter { get; set; } = ProtoRegistry.FormatterKind.Default;

    public ProtoRegistry.SinkKind Sink { get; set; } = ProtoRegistry.SinkKind.Default;

    public ProtoRegistry.InterceptingMode Intercepting { get; set; } = ProtoRegistry.InterceptingMode.Both;

    public ProtoRegistry.LoggingMode Logging { get; set; } = ProtoRegistry.LoggingMode.Both;

    public LogLevel LogLevel { get; set; } = LogLevel.Debug;

    public string CustomFormatterName { get; set; } = string.Empty;

    public string CustomSinkName { get; set; } = string.Empty;

    public long? MaximumBytesCaptured { get; set; }

    public bool GlobalHandlerEnabled { get; set; } = true;

    public bool GlobalMiddlewareEnabled { get; set; } = true;

    public bool GlobalClientInterceptorEnabled { get; set; } = true;

    public bool GlobalServerInterceptorEnabled { get; set; } = true;

    internal string FormatterName => Formatter == ProtoRegistry.FormatterKind.Custom ? CustomFormatterName : Formatter.ToString();

    internal string SinkName => Sink == ProtoRegistry.SinkKind.Custom ? CustomSinkName : Sink.ToString();

    internal void Validate() {
        if (MaximumBytesCaptured is <= 0L) {
            throw new InvalidOperationException($"{nameof(ProtoOptions)}.{nameof(MaximumBytesCaptured)} must be greater than zero when set.");
        }

        if (Formatter == ProtoRegistry.FormatterKind.Custom && string.IsNullOrWhiteSpace(CustomFormatterName)) {
            throw new InvalidOperationException($"{nameof(ProtoOptions)}.{nameof(CustomFormatterName)} is required when {nameof(Formatter)} is {nameof(ProtoRegistry.FormatterKind.Custom)}.");
        }

        if (Sink == ProtoRegistry.SinkKind.Custom && string.IsNullOrWhiteSpace(CustomSinkName)) {
            throw new InvalidOperationException($"{nameof(ProtoOptions)}.{nameof(CustomSinkName)} is required when {nameof(Sink)} is {nameof(ProtoRegistry.SinkKind.Custom)}.");
        }
    }
}
