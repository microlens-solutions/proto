using Microlens.Proto.Models;
using Microlens.Proto.Shared;
using System.Text.Json;

namespace Microlens.Proto.Formatters;

internal sealed class JsonProtoFormatter : IProtoFormatter {
    private static readonly JsonSerializerOptions _options = new() { WriteIndented = true };

    public ProtoFormatterKey Key => ProtoFormatterKey.Json;

    public string Name => "Json";

    public string Format(IReadOnlyList<ProtoNode> nodes) {
        return JsonSerializer.Serialize(nodes, _options);
    }
}
