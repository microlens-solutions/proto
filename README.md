# `Microlens.Proto`

Decode and inspect `Protocol Buffer (Protobuf)` payloads without `.proto` files, generated classes or schema definitions.

**`Microlens.Proto`** is a schema-less `Protobuf` inspection toolkit for .NET that automatically **intercepts**, **decodes**, **visualizes** and **logs** `Protobuf` traffic across `HTTP` and `gRPC` boundaries.

Unlike traditional `Protobuf` libraries that require compile-time contracts, **`Microlens.Proto`** works directly against raw wire-format payloads, making it useful for **diagnostics**, **auditing**, **reverse engineering** and **production troubleshooting**.

[Why `Microlens.Proto`?](#why-microlensproto) | [Quick Start](#quick-start) | [Quick Example](#quick-example) | [Features](#features) | [Extensible Architecture](#extensible-architecture) | [Performance Characteristics](#performance-characteristics) | [Comparison](#comparison) | [When Not To Use `Microlens.Proto`](#when-not-to-use-microlensproto) | [License](#license)

---

## Why `Microlens.Proto`?

Most `Protobuf` tooling assumes you already have:

* `.proto` files
* generated C# classes
* source-code access

In many real-world scenarios, you have none of those.

Examples of typical use cases:

* **Production Diagnostics**: Capture payload structures during incident investigation and troubleshooting.
* **Debugging `gRPC` Requests**: inspect request and response messages without modifying service code.
* **Auditing Binary Traffic**: Understand exactly what is crossing service boundaries.
* **Reverse Engineering Legacy Systems**: Analyze `Protobuf` payloads when schemas are unavailable.
* **API Discovery**: Understand third-party `Protobuf` protocols without source access.
* **Understanding Service Behavior**: Analyze what a service is actually sending.

---

## Quick Start

### Installation

```bash
dotnet add package Microlens.Proto
```

### Register Services

```csharp
builder.Services.AddMicrolensProto();
```

### Register Middleware

```csharp
app.UseMicrolensProto();
```

That's it.

`HTTP` and `gRPC` payload inspection are automatically enabled.

---

## Quick Example

Given a raw `Protobuf` payload:

```csharp
byte[] payload = ...;
```

**`Microlens.Proto`** automatically parses raw `Protobuf` payloads and converts them into a readable tree structure.

```text
├── Field 1 (Varint): 42
├── Field 2 (LengthDelimited): Device-01
├── Field 3 (LengthDelimited)
│   ├── Field 1 (Varint): 123
│   └── Field 2 (LengthDelimited): Active
└── Field 4 (Fixed64): 987654321
```

* No `.proto` files required.
* No schema definitions required.
* No generated classes required.
* No reflection required.
* No custom parsers required.

---

## Features

**`Microlens.Proto`** is **NOT** intended to replace `Google.Protobuf` or `protobuf-net` for **serialization** and **deserialization** of known contracts.
Instead, it complements them by providing visibility into raw `Protobuf` traffic.

### Schema-less `Protobuf` Decoding

Decode raw `Protobuf` wire data without `.proto` definitions.

Supported wire types:

* Varint
* Fixed32
* Fixed64
* Length Delimited

### Nested Message Discovery

Automatically detects and recursively decodes embedded `Protobuf` messages.

```text
Field 5
├── Field 1: HardwareRev
└── Field 2: v3.2
```

### HTTP Payload Inspection

Intercept **outbound** and **inbound** `Protobuf` traffic automatically.

* HttpClient DelegatingHandler
* ASP.NET Core Middleware

### gRPC Message Inspection

Capture and inspect `gRPC` messages transparently.

* Client Interceptors
* Server Interceptors

### Human-Readable Output

Convert binary payloads into readable tree structures.

```text
├── Field 1 (Varint): 999
├── Field 2 (LengthDelimited): Connected
└── Field 3 (Fixed32): 1098488218
```

### `JSON` Output

Emit structured `JSON` for:

* `Elasticsearch`
* `Splunk`
* `OpenSearch`
* `DataDog`
* Custom analytics pipelines

### High Performance

Built on:

* `ReadOnlySequence<byte>`
* `Span<T>`
* `RecyclableMemoryStream`

Designed for high-throughput services and production workloads.

---

## Extensible Architecture

Register:

* Custom formatters
* Custom sinks
* Custom telemetry pipelines

### Formatters

#### Default Formatter
The default formatter generates a human-readable tree structure that can be written directly to your existing logging infrastructure.

```text
TimestampUtc = 2026-06-08T12:00:00Z
Channel = Grpc
Direction = Inbound
Phase = Request
Path = /EnvelopeService/Post

Payload:

├── Field 1 (LengthDelimited): HardwareRev
├── Field 2 (LengthDelimited): v3.2
├── Field 3 (Varint): 42
└── Field 4 (Fixed64): 987654321
```

### Custom Formatters

**`Microlens.Proto`** supports custom visualization formats.

#### Example: Compact Formatter

```csharp
public sealed class CompactProtoFormatter : IProtoFormatter {
    public ProtoFormatterKey Key => ProtoFormatterKey.Custom;

    public string Name => "Compact";

    public string Format(IReadOnlyList<ProtoNode> nodes) {
        return $"Fields: {nodes.Count}";
    }
}
```

#### Register: Compact Formatter

```csharp
builder.Services.AddFormatter<CompactProtoFormatter>("Compact");

builder.Services.AddMicrolensProto(options => {
    options.FormatterKey = ProtoFormatterKey.Custom;
    options.CustomFormatterName = "Compact";
});
```

### Custom Sinks

Send decoded payload information to any destination.

#### Example: Serilog Sink

```csharp
public sealed class SerilogProtoSink : IProtoSink {
    // Custom implementation
}
```

#### Register: Serilog Sink

```csharp
builder.Services.AddSink<SerilogProtoSink>("Serilog");

builder.Services.AddMicrolensProto(options =>
{
    options.SinkKey = ProtoSinkKey.Custom;
    options.CustomSinkName = "Serilog";
});
```

More Examples:

* `Seq`
* `Elasticsearch`
* `Splunk`
* `OpenSearch`
* `Datadog`
* `Application Insights`
* Custom telemetry platforms

and many more.

### What Gets Captured and Logged?

**`Microlens.Proto`** can inspect both requests and responses across multiple communication channels.

| Channel                 | Request | Response |
| ----------------------- | ------- | -------- |
| HttpClient              | ✓       | ✓        |
| ASP.NET Core Middleware | ✓       | ✓        |
| gRPC Client             | ✓       | ✓        |
| gRPC Server             | ✓       | ✓        |

Capture and logging behavior can be configured through `ProtoOptions`.

```csharp
builder.Services.AddMicrolensProto(options =>
{
    options.CaptureMode = ProtoCaptureMode.Both;
    options.LogScope = ProtoLogScope.Both;
});
```

---

## Performance Characteristics

**`Microlens.Proto`** is designed for production environments and high-throughput workloads.

Key implementation details:

- `ReadOnlySequence<byte>` based parsing
- `Span<T>` friendly processing
- Streaming payload inspection
- Buffer pooling through `RecyclableMemoryStream`
- Recursive nested message decoding
- Minimal allocations where possible

The library is optimized for **observability** and **diagnostics** while minimizing runtime overhead.

---

## Comparison

| Capability | Microlens.Proto | Google.Protobuf / protobuf-net |
|-------------|-------------|-------------|
| Serialize known contracts | No | Yes |
| Deserialize known contracts | No | Yes |
| Decode unknown Protobuf payloads | Yes | Limited |
| Works without `.proto` files | Yes | Limited |
| Requires generated classes | No | Usually |
| Schema-less inspection | Yes | Limited |
| Nested message discovery | Yes | Limited |
| `HTTP` payload interception | Yes | No |
| `gRPC` payload interception | Yes | No |
| Logging integration | Yes | No |
| Custom sinks and formatters | Yes | No |

**`Microlens.Proto`** focuses on **inspection**, **diagnostics**, **observability** and **traffic analysis** rather than contract-based serialization.

---

## When Not To Use `Microlens.Proto`

`Microlens.Proto` is not intended for:

* Generating `C#` classes from `.proto` files
* Contract-based serialization
* Contract-based deserialization
* Replacing `Google.Protobuf`
* Replacing `protobuf-net`

If you already have schema definitions and generated types, use a traditional `Protobuf` library.

---

## License

Licensed under the **Apache License 2.0**.

See the [LICENSE](LICENSE) file for details.

---
