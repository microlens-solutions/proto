using Microlens.Proto.Shared;
using Microsoft.Extensions.Logging;

namespace Microlens.Proto.Models;

public sealed class ProtoOptions {
    private string _formatterName = string.Empty;
    private string _sinkName = string.Empty;

    public ProtoFormatterKey FormatterKey { get; set; } = ProtoFormatterKey.Default;

    public ProtoSinkKey SinkKey { get; set; } = ProtoSinkKey.Default;

    public ProtoCaptureMode CaptureMode { get; set; } = ProtoCaptureMode.Both;

    public ProtoLogScope LogScope { get; set; } = ProtoLogScope.Both;

    public LogLevel LogLevel { get; set; } = LogLevel.Debug;

    public string CustomFormatterName {
        get {
            return !string.IsNullOrWhiteSpace(_formatterName) && FormatterKey == ProtoFormatterKey.Custom ? _formatterName : FormatterKey.ToString();
        }
        set {
            _formatterName = value;
        }
    }

    public string CustomSinkName {
        get {
            return !string.IsNullOrWhiteSpace(_sinkName) && SinkKey == ProtoSinkKey.Custom ? _sinkName : SinkKey.ToString();
        }
        set {
            _sinkName = value;
        }
    }

    public bool GlobalHandlerEnabled { get; set; } = true;

    public bool GlobalMiddlewareEnabled { get; set; } = true;

    public bool GlobalClientInterceptorEnabled { get; set; } = true;

    public bool GlobalServerInterceptorEnabled { get; set; } = true;
}
