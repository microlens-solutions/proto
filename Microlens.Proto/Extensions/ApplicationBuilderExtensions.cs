using Microlens.Proto.Formatters;
using Microlens.Proto.Inspectors;
using Microlens.Proto.Models;
using Microlens.Proto.Pipeline;
using Microlens.Proto.Sinks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microlens.Proto.Extensions;

public static class ApplicationBuilderExtensions {
    public static IApplicationBuilder UseMicrolensProto(this IApplicationBuilder app) {
        return app.Use(next => {
            var options = app.ApplicationServices.GetRequiredService<IOptions<ProtoOptions>>();
            var context = app.ApplicationServices.GetRequiredService<IProtoContext>();
            var inspector = app.ApplicationServices.GetRequiredService<IProtoInspector>();
            var formatter = app.ApplicationServices.GetRequiredService<IProtoFormatterResolver>();
            var sink = app.ApplicationServices.GetRequiredService<IProtoSinkResolver>();
            var middleware = new ProtoMiddleware(next, options, context, inspector, formatter, sink);
            return context => middleware.InvokeAsync(context);
        });
    }
}
