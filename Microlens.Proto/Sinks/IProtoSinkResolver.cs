namespace Microlens.Proto.Sinks;

public interface IProtoSinkResolver {
    IProtoSink Get(string key);
}
