namespace Microlens.Proto.Shared;

internal static class Constants {
    internal const int MAXIMUM_NESTED_DEPTH = 64;
    internal const string DEFAULT_LOG_FORMAT = "TimestampUtc = `{TimestampUtc}`, Channel = `{Channel}`, Direction = `{Direction}`, Phase = `{Phase}`, Path = `{Path}`{break}Payload:{break}{Payload}";

    internal const string K_SKIP_PROTO_HANDLER = "k-skip-proto-handler";
    internal const string K_SKIP_PROTO_INTERCEPTOR = "k-skip-proto-interceptor";
}
