using Microlens.Proto.Shared;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Microlens.Proto.Formatters;

internal sealed class ProtoFormatterResolver(IServiceProvider provider) : IProtoFormatterResolver {
    private readonly IServiceProvider _provider = provider;

    public IProtoFormatter Get(string key) {
        if (string.IsNullOrWhiteSpace(key)) {
            key = ProtoRegistry.FormatterKind.Default.ToString();
        }

        return _provider.GetKeyedService<IProtoFormatter>(key) ?? _provider.GetKeyedService<IProtoFormatter>(ProtoRegistry.FormatterKind.Default.ToString())!;
    }
}
