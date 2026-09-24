using Microlens.Proto.Models;
using Microlens.Proto.Shared;
using System;
using System.Collections.Generic;

namespace Microlens.Proto.Formatters;

internal sealed class NoneProtoFormatter : IProtoFormatter {
    [Obsolete]
    public Registry.ProtoFormatterKey Key => Registry.ProtoFormatterKey.None;

    public string Name => "None";

    public string Format(IReadOnlyList<ProtoNode> nodes) {
        return string.Empty;
    }
}
