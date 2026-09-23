using Microlens.Proto.Shared;
using System;
using System.Net.Http;

namespace Microlens.Proto.Extensions;

internal static class HttpRequestMessageExtensions {
    internal static HttpRequestMessage SkipProtoHandler(this HttpRequestMessage request) {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.Headers.Contains(Registry.K_SKIP_PROTO_HANDLER)) {
            request.Headers.Add(Registry.K_SKIP_PROTO_HANDLER, "true");
        }

        return request;
    }
}
