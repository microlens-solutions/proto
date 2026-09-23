using Microlens.Proto.Models;
using Microlens.Proto.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microlens.Proto.Formatters;

internal sealed class DefaultProtoFormatter : IProtoFormatter {
    public Registry.ProtoFormatterKey Key => Registry.ProtoFormatterKey.Default;

    public string Name => "Default";

    public string Format(IReadOnlyList<ProtoNode> nodes) {
        if (nodes.Count == 0) {
            return string.Empty;
        }

        var builder = new StringBuilder();
        WriteNodes(builder, nodes, string.Empty);

        return builder.ToString();
    }

    private static void WriteNodes(StringBuilder builder, IReadOnlyList<ProtoNode> nodes, string indent) {
        for (int i = 0; i < nodes.Count; i++) {
            ProtoNode node = nodes[i];
            bool last = i == nodes.Count - 1;

            _ = builder.Append(indent).Append(last ? "└── " : "├── ").Append($"Field {node.FieldNumber} ({node.WireType})");

            if (node.Value is { Type: not Registry.ProtoValueType.Nested } value) {
                _ = builder.Append(": ");
                AppendValue(builder, value.Data);
            }

            _ = builder.AppendLine();

            if (node.Children.Count > 0) {
                WriteNodes(builder, node.Children, indent + (last ? "    " : "│   "));
            }
        }
    }

    private static void AppendValue(StringBuilder builder, object? data) {
        switch (data) {
            case ulong number:
                _ = builder.Append(number);
                break;

            case uint number:
                _ = builder.Append(number);
                break;

            case string text:
                _ = builder.Append(text);
                break;

            case ReadOnlyMemory<byte> bytes:
                AppendHex(builder, bytes.Span);
                break;

            case null:
                break;

            default:
                _ = builder.Append(data);
                break;
        }
    }

    private static void AppendHex(StringBuilder builder, ReadOnlySpan<byte> bytes) {
        _ = builder.Append("0x");

        for (int i = 0; i < bytes.Length; i++) {
            byte value = bytes[i];
            _ = builder.Append(Registry.HEX[value >> 4]).Append(Registry.HEX[value & 0x0F]);
        }
    }
}
