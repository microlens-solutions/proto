using Microlens.Proto.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace Microlens.Proto.Sinks;

internal sealed class ProtoSinkResolver(IServiceProvider provider) : IProtoSinkResolver {
    private readonly IServiceProvider _provider = provider;

    public IProtoSink Get(string key) {
        if (string.IsNullOrWhiteSpace(key)) {
            key = ProtoSinkKey.Default.ToString();
        }

        return _provider.GetKeyedService<IProtoSink>(key) ?? _provider.GetKeyedService<IProtoSink>(ProtoSinkKey.Default.ToString())!;
    }
}
