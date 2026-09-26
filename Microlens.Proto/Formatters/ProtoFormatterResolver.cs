using Microlens.Internal;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Microlens.Proto.Formatters;

internal sealed class ProtoFormatterResolver(IServiceProvider provider) : IProtoFormatterResolver {
    private readonly IServiceProvider _provider = provider;

    public IProtoFormatter Get(string key) {
        Guard.NotNullOrWhiteSpace(key);

        return _provider.GetKeyedService<IProtoFormatter>(key)
            ?? throw new InvalidOperationException($"No {nameof(IProtoFormatter)} is registered under '{key}'. Register it with AddFormatter<TProtoFormatter>(\"{key}\").");
    }
}
