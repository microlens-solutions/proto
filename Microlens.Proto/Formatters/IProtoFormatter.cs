using Microlens.Proto.Models;
using Microlens.Proto.Shared;
using System.Collections.Generic;

namespace Microlens.Proto.Formatters;

public interface IProtoFormatter {
    Registry.ProtoFormatterKey Key { get; }

    string Name { get; }

    string Format(IReadOnlyList<ProtoNode> nodes);
}
