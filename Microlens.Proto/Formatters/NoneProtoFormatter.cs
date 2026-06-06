using Microlens.Proto.Models;
using Microlens.Proto.Shared;

namespace Microlens.Proto.Formatters;

internal sealed class NoneProtoFormatter : IProtoFormatter {
    public ProtoFormatterKey Key => ProtoFormatterKey.None;

    public string Name => "None";

    public string Format(IReadOnlyList<ProtoNode> nodes) {
        return string.Empty;
    }
}
