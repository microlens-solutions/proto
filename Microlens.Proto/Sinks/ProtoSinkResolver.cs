using Microlens.Proto.Shared;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Microlens.Proto.Sinks;

internal sealed class ProtoSinkResolver(IServiceProvider provider) : IProtoSinkResolver {
    private readonly IServiceProvider _provider = provider;

    public IProtoSink Get(string key) {
        if (string.IsNullOrWhiteSpace(key)) {
            key = ProtoRegistry.SinkKind.Default.ToString();
        }

        return _provider.GetKeyedService<IProtoSink>(key) ?? _provider.GetKeyedService<IProtoSink>(ProtoRegistry.SinkKind.Default.ToString())!;
    }
}
