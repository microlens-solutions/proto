namespace Microlens.Proto.Shared;

internal static class Registry {
    internal const int MAXIMUM_NESTED_DEPTH = 64;

    internal const string DEFAULT_LOG_FORMAT = "TimestampUtc = `{TimestampUtc:O}`, Channel = `{Channel}`, Direction = `{Direction}`, Phase = `{Phase}`, Path = `{Path}`\nPayload:\n{Payload}";

    internal const string K_SKIP_PROTO_HANDLER = "k-skip-proto-handler";

    internal const string K_SKIP_PROTO_INTERCEPTOR = "k-skip-proto-interceptor";

    internal const string MEDIA_TYPE_PROTOBUF = "application/protobuf";

    internal const string MEDIA_TYPE_X_PROTOBUF = "application/x-protobuf";

    internal const string MEDIA_TYPE_GRPC = "application/grpc";

    internal const string HEX = "0123456789ABCDEF";

    internal const int HASH_MULTIPLIER = 397;

    internal enum ProtoChannelType {
        None = 0,

        Http = 1,

        Grpc = 2
    }

    internal enum ProtoDirectionType {
        None = 0,

        Inbound = 1,

        Outbound = 2
    }

    internal enum ProtoPhaseType {
        None = 0,

        Request = 1,

        Response = 2
    }

    internal enum StreamStateType {
        Undecided = 0,

        Capturing = 1,

        Bypassed = 2
    }
}
