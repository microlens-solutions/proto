namespace Microlens.Proto.Models;

public interface IProtoContext {
    public DateTime TimestampUtc { get; internal set; }

    public string Channel { get; internal set; }

    public string Direction { get; internal set; }

    public string Phase { get; internal set; }

    public string? Path { get; internal set; }
}
