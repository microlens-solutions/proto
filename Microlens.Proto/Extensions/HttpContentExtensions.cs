using Microlens.Proto.Shared;

namespace Microlens.Proto.Extensions;

public static class HttpContentExtensions {
    public static HttpContent SkipProtoHandler(this HttpContent content) {
        ArgumentNullException.ThrowIfNull(content);

        if (!content.Headers.Contains(Constants.K_SKIP_PROTO_HANDLER)) {
            content.Headers.Add(Constants.K_SKIP_PROTO_HANDLER, "true");
        }

        return content;
    }
}
