namespace Microlens.Proto.Formatters;

public interface IProtoFormatterResolver {
    IProtoFormatter Get(string key);
}
