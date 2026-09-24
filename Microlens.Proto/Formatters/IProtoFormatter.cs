using Microlens.Proto.Models;
using Microlens.Proto.Shared;
using System.Collections.Generic;

namespace Microlens.Proto.Formatters;

public interface IProtoFormatter {
    ProtoRegistry.FormatterKind Key { get; }

    string Name { get; }

    string Format(IReadOnlyList<ProtoNode> nodes);
}
