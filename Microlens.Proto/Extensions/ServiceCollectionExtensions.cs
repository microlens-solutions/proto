using Grpc.AspNetCore.Server;
using Grpc.Net.ClientFactory;
using Microlens.Proto.Decoders;
using Microlens.Proto.Formatters;
using Microlens.Proto.Inspectors;
using Microlens.Proto.Models;
using Microlens.Proto.Pipeline;
using Microlens.Proto.Sinks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;

namespace Microlens.Proto.Extensions;

public static class ServiceCollectionExtensions {
    public static IServiceCollection AddMicrolensProto(this IServiceCollection services) {
        return services.AddMicrolensProto(_ => { });
    }

    public static IServiceCollection AddMicrolensProto(this IServiceCollection services, Action<ProtoOptions> options) {
        _ = services.Configure(options);

        _ = services.AddSingleton<IProtoDecoder, ProtoDecoder>();
        _ = services.AddSingleton<IProtoInspector, ProtoInspector>();
        _ = services.AddTransient<IProtoContext, ProtoContext>();

        _ = services.AddSingleton<IProtoFormatterResolver, ProtoFormatterResolver>();
        _ = services.AddKeyedSingleton<IProtoFormatter, NoneProtoFormatter>("None");
        _ = services.AddKeyedSingleton<IProtoFormatter, DefaultProtoFormatter>("Default");
        _ = services.AddKeyedSingleton<IProtoFormatter, JsonProtoFormatter>("Json");

        _ = services.AddSingleton<IProtoSinkResolver, ProtoSinkResolver>();
        _ = services.AddKeyedSingleton<IProtoSink, NoneProtoSink>("None");
        _ = services.AddKeyedSingleton<IProtoSink, DefaultProtoSink>("Default");

        _ = services.AddTransient(sp => new ProtoHandler(
            sp.GetRequiredService<IOptions<ProtoOptions>>(),
            sp.GetRequiredService<IProtoContext>(),
            sp.GetRequiredService<IProtoInspector>(),
            sp.GetRequiredService<IProtoFormatterResolver>(),
            sp.GetRequiredService<IProtoSinkResolver>())
        );

        _ = services.AddTransient(sp => new ProtoClientInterceptor(
            sp.GetRequiredService<IOptions<ProtoOptions>>(),
            sp.GetRequiredService<IProtoContext>(),
            sp.GetRequiredService<IProtoInspector>(),
            sp.GetRequiredService<IProtoFormatterResolver>(),
            sp.GetRequiredService<IProtoSinkResolver>())
        );

        _ = services.AddTransient(sp => new ProtoServerInterceptor(
            sp.GetRequiredService<IOptions<ProtoOptions>>(),
            sp.GetRequiredService<IProtoContext>(),
            sp.GetRequiredService<IProtoInspector>(),
            sp.GetRequiredService<IProtoFormatterResolver>(),
            sp.GetRequiredService<IProtoSinkResolver>())
        );

        _ = services.ConfigureAll<HttpClientFactoryOptions>(options => {
            options.HttpMessageHandlerBuilderActions.Add(builder => {
                builder.AdditionalHandlers.Add(builder.Services.GetRequiredService<ProtoHandler>());
            });
        });

        _ = services.ConfigureAll<GrpcClientFactoryOptions>(options => {
            options.InterceptorRegistrations.Add(new Grpc.Net.ClientFactory.InterceptorRegistration(InterceptorScope.Channel, provider => provider.GetRequiredService<ProtoClientInterceptor>()));
        });

        _ = services.ConfigureAll<GrpcServiceOptions>(options => {
            options.Interceptors.Add<ProtoServerInterceptor>();
        });

        return services;
    }

    public static IServiceCollection AddFormatter<TProtoFormatter>(this IServiceCollection services, string key) where TProtoFormatter : class, IProtoFormatter {
        return services.AddKeyedSingleton<IProtoFormatter, TProtoFormatter>(key);
    }

    public static IServiceCollection AddSink<TProtoSink>(this IServiceCollection services, string key) where TProtoSink : class, IProtoSink {
        return services.AddKeyedSingleton<IProtoSink, TProtoSink>(key);
    }
}
