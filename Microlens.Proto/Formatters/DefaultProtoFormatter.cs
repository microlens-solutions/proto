using Microlens.Proto.Models;
using Microlens.Proto.Shared;
using System.Text;

namespace Microlens.Proto.Formatters;

internal sealed class DefaultProtoFormatter : IProtoFormatter {
    public ProtoFormatterKey Key => ProtoFormatterKey.Default;

    public string Name => "Default";

    public string Format(IReadOnlyList<ProtoNode> nodes) {
        var sb = new StringBuilder();
        WriteNodes(sb, nodes);

        return sb.ToString();
    }

    private static void WriteNodes(StringBuilder sb, IReadOnlyList<ProtoNode> nodes, string indent = "") {
        for (int i = 0; i < nodes.Count; i++) {
            var node = nodes[i];
            bool last = i == nodes.Count - 1;
            _ = sb.Append(indent);
            _ = sb.Append(last ? "└── " : "├── ");
            _ = sb.Append($"Field {node.FieldNumber} ({node.WireType})");

            if (node.Value != null && node.Value.Type != ProtoValueType.Nested) {
                _ = sb.Append(": ");
                _ = sb.Append(node.Value.Data);
            }

            _ = sb.AppendLine();

            if (node.Children.Count > 0) {
                string nextIndent = indent + (last ? "    " : "│   ");
                WriteNodes(sb, node.Children, nextIndent);
            }
        }
    }
}
