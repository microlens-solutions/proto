using Microlens.Proto.Extensions;
using Microlens.Proto.Shared;
using Microsoft.Extensions.Logging;
using System;

namespace Microlens.Proto.Models;

public sealed class ProtoOptions {
    private string _formatter = string.Empty;

    private string _sink = string.Empty;

    public ProtoRegistry.FormatterKind Formatter { get; set; } = ProtoRegistry.FormatterKind.Default;

    public ProtoRegistry.SinkKind Sink { get; set; } = ProtoRegistry.SinkKind.Default;

    public ProtoRegistry.InterceptingMode Intercepting { get; set; } = ProtoRegistry.InterceptingMode.Both;

    public ProtoRegistry.LoggingMode Logging { get; set; } = ProtoRegistry.LoggingMode.Both;

    public LogLevel LogLevel { get; set; } = LogLevel.Debug;

    public string CustomFormatterName {
        get {
            return !string.IsNullOrWhiteSpace(_formatter) && Formatter == ProtoRegistry.FormatterKind.Custom ? _formatter : Formatter.ToString();
        }
        set {
            _formatter = value;
        }
    }

    public string CustomSinkName {
        get {
            return !string.IsNullOrWhiteSpace(_sink) && Sink == ProtoRegistry.SinkKind.Custom ? _sink : Sink.ToString();
        }
        set {
            _sink = value;
        }
    }

    public long? MaximumBytesCaptured { get; set; }

    public bool GlobalHandlerEnabled { get; set; } = true;

    public bool GlobalMiddlewareEnabled { get; set; } = true;

    public bool GlobalClientInterceptorEnabled { get; set; } = true;

    public bool GlobalServerInterceptorEnabled { get; set; } = true;

    [Obsolete]
    private Registry.ProtoFormatterKey _legacyFormatter = Registry.ProtoFormatterKey.Default;

    [Obsolete("It will be removed in 3.0.0, use ProtoOptions.Formatter")]
    public Registry.ProtoFormatterKey FormatterKey {
        get {
            return _legacyFormatter;
        }
        set {
            _legacyFormatter = Formatter.Convert();
        }
    }

    [Obsolete]
    private Registry.ProtoSinkKey _legacySink = Registry.ProtoSinkKey.Default;

    [Obsolete("It will be removed in 3.0.0, use ProtoOptions.Sink")]
    public Registry.ProtoSinkKey SinkKey {
        get {
            return _legacySink;
        }
        set {
            _legacySink = Sink.Convert();
        }
    }

    [Obsolete]
    private Registry.ProtoCaptureMode _legacyIntercepting = Registry.ProtoCaptureMode.Both;

    [Obsolete("It will be removed in 3.0.0, use ProtoOptions.Intercepting")]
    public Registry.ProtoCaptureMode CaptureMode {
        get {
            return _legacyIntercepting;
        }
        set {
            _legacyIntercepting = Intercepting.Convert();
        }
    }

    [Obsolete]
    private Registry.ProtoLogScope _legacylogging = Registry.ProtoLogScope.Both;

    [Obsolete("It will be removed in 3.0.0, use ProtoOptions.Logging")]
    public Registry.ProtoLogScope LogScope {
        get {
            return _legacylogging;
        }
        set {
            _legacylogging = Logging.Convert();
        }
    }
}
