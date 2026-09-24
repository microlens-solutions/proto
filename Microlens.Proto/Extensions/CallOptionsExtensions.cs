using Grpc.Core;
using Microlens.Proto.Shared;

namespace Microlens.Proto.Extensions;

public static class CallOptionsExtensions {
    public static CallOptions SkipProtoInterceptor(this CallOptions options) {
        var headers = options.Headers;

        if (headers is null) {
            headers = [];
            options = options.WithHeaders(headers);
        }

        if (!headers.Contains(Registry.K_SKIP_PROTO_INTERCEPTOR)) {
            headers.Add(Registry.K_SKIP_PROTO_INTERCEPTOR, "true");
        }

        return options;
    }
}
