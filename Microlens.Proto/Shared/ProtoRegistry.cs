using System;

namespace Microlens.Proto.Shared;

public static class ProtoRegistry {
    public enum ValueKind {
        None = 0,

        Varint = 1,

        Fixed32 = 2,

        Fixed64 = 4,

        String = 8,

        Bytes = 16,

        Nested = 32
    }

    public enum FormatterKind {
        None = 0,

        Default = 1,

        Json = 2,

        Custom = 4
    }

    public enum SinkKind {
        None = 0,

        Default = 1,

        Custom = 2
    }

    [Flags]
    public enum InterceptingMode {
        None = 0,

        Request = 1,

        Response = 2,

        Both = Request | Response
    }

    [Flags]
    public enum LoggingMode {
        None = 0,

        Request = 1,

        Response = 2,

        Both = Request | Response
    }
}
