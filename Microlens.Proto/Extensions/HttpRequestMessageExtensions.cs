using Microlens.Internal;
using Microlens.Proto.Shared;
using System.Net.Http;

namespace Microlens.Proto.Extensions;

public static class HttpRequestMessageExtensions {
    public static HttpRequestMessage SkipProtoHandler(this HttpRequestMessage request) {
        Guard.NotNull(request);

        if (!request.Headers.Contains(Registry.K_SKIP_PROTO_HANDLER)) {
            request.Headers.Add(Registry.K_SKIP_PROTO_HANDLER, "true");
        }

        return request;
    }
}
