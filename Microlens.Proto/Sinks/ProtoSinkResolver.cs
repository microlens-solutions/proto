using Microlens.Proto.Shared;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Microlens.Proto.Sinks;

internal sealed class ProtoSinkResolver(IServiceProvider provider) : IProtoSinkResolver {
    private readonly IServiceProvider _provider = provider;

    public IProtoSink Get(string key) {
        Guard.NotNullOrWhiteSpace(key);

        return _provider.GetKeyedService<IProtoSink>(key)
            ?? throw new InvalidOperationException($"No {nameof(IProtoSink)} is registered under '{key}'. Register it with AddSink<TProtoSink>(\"{key}\").");
    }
}
