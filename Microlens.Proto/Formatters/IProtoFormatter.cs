using Microlens.Proto.Models;
using Microlens.Proto.Shared;
using System;
using System.Collections.Generic;

namespace Microlens.Proto.Formatters;

public interface IProtoFormatter {
    [Obsolete("It will be replaced by ProtoRegistry.FormatterKey in 3.0.0")]
    Registry.ProtoFormatterKey Key { get; }

    string Name { get; }

    string Format(IReadOnlyList<ProtoNode> nodes);
}
