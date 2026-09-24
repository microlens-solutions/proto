using Microlens.Proto.Shared;
using System.Net.Http;

namespace Microlens.Proto.Extensions;

public static class HttpContentExtensions {
    public static HttpContent SkipProtoHandler(this HttpContent content) {
        Guard.NotNull(content);

        if (!content.Headers.Contains(Registry.K_SKIP_PROTO_HANDLER)) {
            content.Headers.Add(Registry.K_SKIP_PROTO_HANDLER, "true");
        }

        return content;
    }
}
