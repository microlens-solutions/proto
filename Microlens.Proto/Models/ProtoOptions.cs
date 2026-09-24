using Microlens.Proto.Shared;
using Microsoft.Extensions.Logging;

namespace Microlens.Proto.Models;

public sealed class ProtoOptions {
    private string _formatterName = string.Empty;

    private string _sinkName = string.Empty;

    public Registry.ProtoFormatterKey FormatterKey { get; set; } = Registry.ProtoFormatterKey.Default;

    public Registry.ProtoSinkKey SinkKey { get; set; } = Registry.ProtoSinkKey.Default;

    public Registry.ProtoCaptureMode CaptureMode { get; set; } = Registry.ProtoCaptureMode.Both;

    public Registry.ProtoLogScope LogScope { get; set; } = Registry.ProtoLogScope.Both;

    public LogLevel LogLevel { get; set; } = LogLevel.Debug;

    public string CustomFormatterName {
        get {
            return !string.IsNullOrWhiteSpace(_formatterName) && FormatterKey == Registry.ProtoFormatterKey.Custom ? _formatterName : FormatterKey.ToString();
        }
        set {
            _formatterName = value;
        }
    }

    public string CustomSinkName {
        get {
            return !string.IsNullOrWhiteSpace(_sinkName) && SinkKey == Registry.ProtoSinkKey.Custom ? _sinkName : SinkKey.ToString();
        }
        set {
            _sinkName = value;
        }
    }

    public long? MaximumBytesCaptured { get; set; }

    public bool GlobalHandlerEnabled { get; set; } = true;

    public bool GlobalMiddlewareEnabled { get; set; } = true;

    public bool GlobalClientInterceptorEnabled { get; set; } = true;

    public bool GlobalServerInterceptorEnabled { get; set; } = true;
}
