using System;

namespace Microlens.Proto.Models;

public interface IProtoScope {
    DateTime TimestampUtc { get; }

    string Channel { get; }

    string Direction { get; }

    string Phase { get; }

    string? Path { get; }
}
