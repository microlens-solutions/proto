using Grpc.Core;
using Microlens.Proto.Shared;

namespace Microlens.Proto.Extensions;

public static class CallOptionsExtensions {
    public static CallOptions SkipProtoInterceptor(this CallOptions options) {
        var headers = options.Headers;

        if (headers == null) {
            headers = [];
            options = options.WithHeaders(headers);
        }

        bool exists = false;

        foreach (var item in headers) {
            if (item.Key.Equals(Constants.K_SKIP_PROTO_INTERCEPTOR, StringComparison.OrdinalIgnoreCase)) {
                exists = true;
                break;
            }
        }

        if (!exists) {
            headers.Add(Constants.K_SKIP_PROTO_INTERCEPTOR, "true");
        }

        return options;
    }
}
