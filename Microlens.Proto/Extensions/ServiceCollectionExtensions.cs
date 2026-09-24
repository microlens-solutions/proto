using Grpc.Net.ClientFactory;
using Microlens.Proto.Decoders;
using Microlens.Proto.Formatters;
using Microlens.Proto.Inspectors;
using Microlens.Proto.Models;
using Microlens.Proto.Pipeline;
using Microlens.Proto.Shared;
using Microlens.Proto.Sinks;
using Microlens.Proto.Tracers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using System;

#if NET
using Grpc.AspNetCore.Server;
#endif

namespace Microlens.Proto.Extensions;

public static class ServiceCollectionExtensions {
    public static IServiceCollection AddMicrolensProto(this IServiceCollection services) {
        return services.AddMicrolensProto(_ => { });
    }

    public static IServiceCollection AddMicrolensProto(this IServiceCollection services, Action<ProtoOptions> options) {
        Guard.NotNull(services);
        Guard.NotNull(options);

        _ = services.Configure(options);

        if (IsRegistered(services)) {
            return services;
        }

        _ = services.AddSingleton<IProtoDecoder, ProtoDecoder>();
        _ = services.AddSingleton<IProtoInspector, ProtoInspector>();

        _ = services.AddSingleton<IProtoFormatterResolver, ProtoFormatterResolver>();
        _ = services.AddKeyedSingleton<IProtoFormatter, NoneProtoFormatter>("None");
        _ = services.AddKeyedSingleton<IProtoFormatter, DefaultProtoFormatter>("Default");
        _ = services.AddKeyedSingleton<IProtoFormatter, JsonProtoFormatter>("Json");

        _ = services.AddSingleton<IProtoSinkResolver, ProtoSinkResolver>();
        _ = services.AddKeyedSingleton<IProtoSink, NoneProtoSink>("None");
        _ = services.AddKeyedSingleton<IProtoSink, DefaultProtoSink>("Default");

        _ = services.AddSingleton(sp => new ProtoTracer(
            sp.GetRequiredService<IOptions<ProtoOptions>>(),
            sp.GetRequiredService<IProtoInspector>(),
            sp.GetRequiredService<IProtoFormatterResolver>(),
            sp.GetRequiredService<IProtoSinkResolver>())
        );

        _ = services.AddTransient(sp => new ProtoHandler(sp.GetRequiredService<ProtoTracer>()));
        _ = services.AddTransient(sp => new ProtoClientInterceptor(sp.GetRequiredService<ProtoTracer>()));
#if NET
        _ = services.AddTransient(sp => new ProtoServerInterceptor(sp.GetRequiredService<ProtoTracer>()));
#endif

        _ = services.ConfigureAll<HttpClientFactoryOptions>(options => {
            options.HttpMessageHandlerBuilderActions.Add(builder => {
                builder.AdditionalHandlers.Add(builder.Services.GetRequiredService<ProtoHandler>());
            });
        });

        _ = services.ConfigureAll<GrpcClientFactoryOptions>(options => {
            options.InterceptorRegistrations.Add(new Grpc.Net.ClientFactory.InterceptorRegistration(InterceptorScope.Channel, provider => provider.GetRequiredService<ProtoClientInterceptor>()));
        });

#if NET
        _ = services.ConfigureAll<GrpcServiceOptions>(options => {
            options.Interceptors.Add<ProtoServerInterceptor>();
        });
#endif

        return services;
    }

    public static IServiceCollection AddFormatter<TProtoFormatter>(this IServiceCollection services, string key) where TProtoFormatter : class, IProtoFormatter {
        Guard.NotNull(services);
        Guard.NotNullOrWhiteSpace(key);

        return services.AddKeyedSingleton<IProtoFormatter, TProtoFormatter>(key);
    }

    public static IServiceCollection AddSink<TProtoSink>(this IServiceCollection services, string key) where TProtoSink : class, IProtoSink {
        Guard.NotNull(services);
        Guard.NotNullOrWhiteSpace(key);

        return services.AddKeyedSingleton<IProtoSink, TProtoSink>(key);
    }

    private static bool IsRegistered(IServiceCollection services) {
        for (int i = 0; i < services.Count; i++) {
            if (services[i].ServiceType == typeof(ProtoTracer)) {
                return true;
            }
        }

        return false;
    }
}
