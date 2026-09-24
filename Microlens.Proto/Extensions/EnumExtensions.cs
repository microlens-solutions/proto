using Microlens.Proto.Shared;
using System;

namespace Microlens.Proto.Extensions;

internal static class EnumExtensions {
    [Obsolete]
    internal static ProtoRegistry.ValueKind Convert(this Registry.ProtoValueType value) {
        return value.Convert<Registry.ProtoValueType, ProtoRegistry.ValueKind>();
    }

    [Obsolete]
    internal static Registry.ProtoValueType Convert(this ProtoRegistry.ValueKind value) {
        return value.Convert<ProtoRegistry.ValueKind, Registry.ProtoValueType>();
    }

    [Obsolete]
    internal static Registry.ProtoFormatterKey Convert(this ProtoRegistry.FormatterKind value) {
        return value.Convert<ProtoRegistry.FormatterKind, Registry.ProtoFormatterKey>();
    }

    [Obsolete]
    internal static Registry.ProtoSinkKey Convert(this ProtoRegistry.SinkKind value) {
        return value.Convert<ProtoRegistry.SinkKind, Registry.ProtoSinkKey>();
    }

    [Obsolete]
    internal static Registry.ProtoCaptureMode Convert(this ProtoRegistry.InterceptingMode value) {
        return value.Convert<ProtoRegistry.InterceptingMode, Registry.ProtoCaptureMode>();
    }

    [Obsolete]
    internal static Registry.ProtoLogScope Convert(this ProtoRegistry.LoggingMode value) {
        return value.Convert<ProtoRegistry.LoggingMode, Registry.ProtoLogScope>();
    }

    internal static TTarget Convert<TSource, TTarget>(this TSource value) where TSource : Enum where TTarget : Enum {
        return (TTarget)Enum.ToObject(typeof(TTarget), value);
    }
}
