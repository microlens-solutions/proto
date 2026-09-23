using Microlens.Proto.Pipeline;
using Microlens.Proto.Tracers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microlens.Proto.Extensions;

public static class ApplicationBuilderExtensions {
    public static IApplicationBuilder UseMicrolensProto(this IApplicationBuilder app) {
        ArgumentNullException.ThrowIfNull(app);
        return app.Use(next => new ProtoMiddleware(next, app.ApplicationServices.GetRequiredService<ProtoTracer>()).InvokeAsync);
    }
}
