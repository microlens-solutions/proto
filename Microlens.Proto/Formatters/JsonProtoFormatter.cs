using Microlens.Proto.Models;
using Microlens.Proto.Shared;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Microlens.Proto.Formatters;

internal sealed class JsonProtoFormatter : IProtoFormatter {
    private static readonly JsonWriterOptions _options = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, Indented = false };

    private static readonly JsonEncodedText _field = JsonEncodedText.Encode("field");

    private static readonly JsonEncodedText _wireType = JsonEncodedText.Encode("wireType");

    private static readonly JsonEncodedText _type = JsonEncodedText.Encode("type");

    private static readonly JsonEncodedText _value = JsonEncodedText.Encode("value");

    private static readonly JsonEncodedText _children = JsonEncodedText.Encode("children");

    public ProtoRegistry.FormatterKind Key => ProtoRegistry.FormatterKind.Json;

    public string Name => "Json";

    public string Format(IReadOnlyList<ProtoNode> nodes) {
        var buffer = new ArrayBufferWriter<byte>();

        using (var writer = new Utf8JsonWriter(buffer, _options)) {
            WriteNodes(writer, nodes);
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static void WriteNodes(Utf8JsonWriter writer, IReadOnlyList<ProtoNode> nodes) {
        writer.WriteStartArray();

        for (int i = 0; i < nodes.Count; i++) {
            ProtoNode node = nodes[i];

            writer.WriteStartObject();
            writer.WriteNumber(_field, node.FieldNumber);
            writer.WriteString(_wireType, node.WireType.ToString());

            if (node.Value is { } value) {
                writer.WriteString(_type, value.Value.ToString());
                WriteValue(writer, value);
            }

            if (node.Children.Count > 0) {
                writer.WritePropertyName(_children);
                WriteNodes(writer, node.Children);
            }

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteValue(Utf8JsonWriter writer, ProtoValue value) {
        if (!value.Typed) {
            WriteData(writer, value.Data);
            return;
        }

        switch (value.Value) {
            case ProtoRegistry.ValueKind.Varint:
            case ProtoRegistry.ValueKind.Fixed32:
            case ProtoRegistry.ValueKind.Fixed64:
                writer.WriteNumber(_value, value.Number);
                break;

            case ProtoRegistry.ValueKind.String:
                writer.WriteString(_value, value.Text);
                break;

            case ProtoRegistry.ValueKind.Bytes:
                writer.WriteBase64String(_value, value.Bytes.Span);
                break;
        }
    }

    private static void WriteData(Utf8JsonWriter writer, object? data) {
        switch (data) {
            case ulong number:
                writer.WriteNumber(_value, number);
                break;

            case uint number:
                writer.WriteNumber(_value, number);
                break;

            case string text:
                writer.WriteString(_value, text);
                break;

            case ReadOnlyMemory<byte> bytes:
                writer.WriteBase64String(_value, bytes.Span);
                break;
        }
    }
}
