using Microlens.Proto.Models;
using Microlens.Proto.Shared;
using System.Collections.Generic;

namespace Microlens.Proto.Formatters;

internal sealed class NoneProtoFormatter : IProtoFormatter {
    public ProtoRegistry.FormatterKind Key => ProtoRegistry.FormatterKind.None;

    public string Name => "None";

    public string Format(IReadOnlyList<ProtoNode> nodes) {
        return string.Empty;
    }
}
