namespace Microlens.Proto.Shared;

public static class Registry {
    internal const int MAXIMUM_NESTED_DEPTH = 64;

    internal const string DEFAULT_LOG_FORMAT = "TimestampUtc = `{TimestampUtc:O}`, Channel = `{Channel}`, Direction = `{Direction}`, Phase = `{Phase}`, Path = `{Path}`\nPayload:\n{Payload}";

    internal const string K_SKIP_PROTO_HANDLER = "k-skip-proto-handler";

    internal const string K_SKIP_PROTO_INTERCEPTOR = "k-skip-proto-interceptor";

    internal const string MEDIA_TYPE_PROTOBUF = "application/protobuf";

    internal const string MEDIA_TYPE_X_PROTOBUF = "application/x-protobuf";

    internal const string MEDIA_TYPE_GRPC = "application/grpc";

    internal const string HEX = "0123456789ABCDEF";

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

    public enum ProtoValueType {
        None = 0,

        Varint = 1,

        Fixed32 = 2,

        Fixed64 = 4,

        String = 8,

        Bytes = 16,

        Nested = 32
    }

    public enum ProtoFormatterKey {
        None = 0,

        Default = 1,

        Json = 2,

        Custom = 4
    }

    public enum ProtoSinkKey {
        None = 0,

        Default = 1,

        Custom = 2
    }

    [Flags]
    public enum ProtoCaptureMode {
        None = 0,

        Request = 1,

        Response = 2,

        Both = Request | Response
    }

    [Flags]
    public enum ProtoLogScope {
        None = 0,

        Request = 1,

        Response = 2,

        Both = Request | Response
    }
}
