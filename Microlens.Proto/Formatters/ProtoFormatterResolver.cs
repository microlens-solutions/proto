using Microlens.Proto.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace Microlens.Proto.Formatters;

internal sealed class ProtoFormatterResolver(IServiceProvider provider) : IProtoFormatterResolver {
    private readonly IServiceProvider _provider = provider;

    public IProtoFormatter Get(string key) {
        if (string.IsNullOrWhiteSpace(key)) {
            key = ProtoFormatterKey.Default.ToString();
        }

        return _provider.GetKeyedService<IProtoFormatter>(key) ?? _provider.GetKeyedService<IProtoFormatter>(ProtoFormatterKey.Default.ToString())!;
    }
}
