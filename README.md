# `Microlens.Proto`

Decode and inspect `Protocol Buffer (Protobuf)` payloads without `.proto` files, generated classes or schema definitions.

**`Microlens.Proto`** is a schemaless `Protobuf` inspection toolkit for .NET that automatically **intercepts**, **decodes**, **visualizes** and **logs** `Protobuf` traffic across `HTTP` and `gRPC` boundaries.

Unlike traditional `Protobuf` libraries that require compile-time contracts, **`Microlens.Proto`** works directly against raw wire-format payloads, making it useful for **diagnostics**, **auditing**, **reverse engineering** and **production troubleshooting**.

[What's New](#whats-new) | [Why `Microlens.Proto`?](#why-microlensproto) | [Requirements](#requirements) | [Quick Start](#quick-start) | [Quick Example](#quick-example) | [Features](#features) | [Configuration](#configuration) | [Extensible Architecture](#extensible-architecture) | [Performance Characteristics](#performance-characteristics) | [Limitations](#limitations) | [Comparison](#comparison) | [When Not To Use `Microlens.Proto`](#when-not-to-use-microlensproto) | [Articles](#articles) | [License](#license)

---

## What's New

* **Zero cost when disabled**: No buffering, decoding or formatting happens when the sink is disabled for the configured `LogLevel`, or when a phase is excluded by `CaptureMode` or `LogScope`.
* **`HttpClient`**:
  * Request and response are traced independently, each by its own `Content-Type`.
  * `Protobuf` responses to bodiless requests (e.g. `GET`) are now captured.
  * Non-`Protobuf` responses are no longer decoded.
  * Non-seekable request content (e.g. `StreamContent`) is buffered before inspection instead of being consumed.
  * New `HttpRequestMessage.SkipProtoHandler()` opts out bodiless requests.
* **ASP.NET Core Middleware**:
  * Media-type parameters (`; charset=…`) are honored.
  * Responses are captured when the request is `Protobuf` or its `Accept` header lists a `Protobuf` media type.
* **`gRPC` Server**: `application/grpc+proto` and `gRPC-Web` content types are intercepted.
* **`Path` normalized**:
  * `gRPC`: `/package.Service/Method` on both client and server.
  * `HTTP`: the request path, without scheme, host or query.
* **`JSON` formatter**: Reflection-free, single-line output with enum names; nested trees are no longer duplicated.
* **Default output**: `bytes` fields render as hex (`0x…`); `TimestampUtc` renders as ISO 8601.
* **Fixed**: Client-streaming responses are tagged with `Direction` and `Phase`.
* **Fixed**: Concurrent traces of one call no longer share mutable scope.
* **Faster decoding**: Exception-free `UTF-8` detection.
* **New**: `IProtoSink.IsEnabled(LogLevel)`, a default interface member (non-breaking).

Full history: [Releases](https://github.com/microlens-solutions/proto/releases).

---

## Why `Microlens.Proto`?

Most `Protobuf` tooling assumes you already have:

* `.proto` files
* generated C# classes
* source-code access

In many real-world scenarios, you have none of those.

Examples of typical use cases:

* **Production Diagnostics**: Capture payload structures during incident investigation and troubleshooting.
* **Debugging `gRPC` Requests**: Inspect request and response messages — unary and streamed — without modifying service code.
* **Auditing Binary Traffic**: Understand exactly what is crossing service boundaries.
* **Reverse Engineering Legacy Systems**: Analyze `Protobuf` payloads when schemas are unavailable.
* **API Discovery**: Understand third-party `Protobuf` protocols without source access.
* **Understanding Service Behavior**: Analyze what a service is actually sending.

---

## Requirements

* `.NET 8` or `.NET 10`
* ASP.NET Core shared framework (pulled in by `Grpc.AspNetCore.Server`). This also applies to client-only hosts.
* `gRPC` tracing requires `Google.Protobuf` messages (`IMessage`).

---

## Quick Start

### Installation

```bash
dotnet add package Microlens.Proto
```

### Register Services

```csharp
using Microlens.Proto.Extensions;

builder.Services.AddMicrolensProto();
```

### Register Middleware

```csharp
app.UseRouting();
app.UseMicrolensProto();
```

Place `UseMicrolensProto()` after `UseRouting()` when routing is registered explicitly, so `[SkipProtoMiddleware]` can be resolved from endpoint metadata.

### Enable Log Output

The default sink writes through `ILogger` at `LogLevel.Debug`. Either enable the category:

```json
{
  "Logging": {
    "LogLevel": {
      "Microlens.Proto": "Debug"
    }
  }
}
```

or change the emitted level:

```csharp
builder.Services.AddMicrolensProto(options => options.LogLevel = LogLevel.Information);
```

That's it. The following are now inspected:

* `HttpClient` instances created via `IHttpClientFactory` (`AddHttpClient`)
* `gRPC` clients registered via `AddGrpcClient<T>()`
* `gRPC` services registered via `AddGrpc()`
* Incoming `HTTP` requests through `UseMicrolensProto()`

---

## Quick Example

Given a raw `Protobuf` payload:

```csharp
byte[] payload = [
    0x08, 0x2A,
    0x12, 0x09, 0x44, 0x65, 0x76, 0x69, 0x63, 0x65, 0x2D, 0x30, 0x31,
    0x1A, 0x0A, 0x08, 0x7B, 0x12, 0x06, 0x41, 0x63, 0x74, 0x69, 0x76, 0x65,
    0x21, 0xB1, 0x68, 0xDE, 0x3A, 0x00, 0x00, 0x00, 0x00
];
```

Decode and format it:

```csharp
using System.Buffers;
using Microlens.Proto.Formatters;
using Microlens.Proto.Inspectors;
using Microlens.Proto.Models;

var inspector = app.Services.GetRequiredService<IProtoInspector>();
var formatter = app.Services.GetRequiredService<IProtoFormatterResolver>().Get("Default");

IReadOnlyList<ProtoNode> nodes = inspector.Inspect(new ReadOnlySequence<byte>(payload));
string tree = formatter.Format(nodes);
```

Output:

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

### Schemaless `Protobuf` Decoding

Decode raw `Protobuf` wire data without `.proto` definitions.

Supported wire types:

* Varint
* Fixed32
* Fixed64
* Length Delimited

### Nested Message Discovery

Automatically detects and recursively decodes embedded `Protobuf` messages, up to 64 levels deep.

```text
└── Field 5 (LengthDelimited)
    ├── Field 1 (LengthDelimited): HardwareRev
    └── Field 2 (LengthDelimited): v3.2
```

### HTTP Payload Inspection

Intercept **outbound** and **inbound** `Protobuf` traffic automatically:

* `HttpClient` `DelegatingHandler`
* ASP.NET Core Middleware

### gRPC Message Inspection

Capture and inspect `gRPC` messages transparently, across all four call shapes:

* Unary
* Server Streaming
* Client Streaming
* Bidirectional (Duplex) Streaming

Enabled through:

* Client Interceptors
* Server Interceptors

Streamed calls are inspected message-by-message as they are read or written, not buffered in full before tracing.

### What Gets Captured

| Channel | Request traced when | Response traced when |
| :--- | :--- | :--- |
| `HttpClient` | Request media type is `application/protobuf` or `application/x-protobuf` | Response media type is `application/protobuf` or `application/x-protobuf` |
| ASP.NET Core Middleware | Request media type is `Protobuf` | Response media type is `Protobuf`, and the request is `Protobuf` or its `Accept` lists a `Protobuf` media type |
| `gRPC` Client | Always, for each message | Always, for each message |
| `gRPC` Server | Request media type is `application/grpc`, `application/grpc+proto` or `application/grpc-web*` | Same as request |

### Human-Readable Output

Convert binary payloads into readable tree structures. Non-text, non-message `bytes` fields render as hex.

```text
├── Field 1 (Varint): 999
├── Field 2 (LengthDelimited): Connected
├── Field 3 (Fixed32): 1098488218
└── Field 4 (LengthDelimited): 0x00FF10
```

### `JSON` Output

Reflection-free, single-line `JSON`, suited for log pipelines such as `Elasticsearch`, `Splunk`, `OpenSearch` and `Datadog`:

```csharp
builder.Services.AddMicrolensProto(options => options.FormatterKey = Registry.ProtoFormatterKey.Json);
```

The [Quick Example](#quick-example) payload formats as:

```json
[{"field":1,"wireType":"Varint","type":"Varint","value":42},{"field":2,"wireType":"LengthDelimited","type":"String","value":"Device-01"},{"field":3,"wireType":"LengthDelimited","type":"Nested","children":[{"field":1,"wireType":"Varint","type":"Varint","value":123},{"field":2,"wireType":"LengthDelimited","type":"String","value":"Active"}]},{"field":4,"wireType":"Fixed64","type":"Fixed64","value":987654321}]
```

`bytes` values are emitted as base64. The default sink wraps the formatted payload in its log template. For raw `JSON` ingestion, pair the `JSON` formatter with a [custom sink](#custom-sinks).

---

## Configuration

### Options

```csharp
using Microlens.Proto.Extensions;
using Microlens.Proto.Shared;
using Microsoft.Extensions.Logging;

builder.Services.AddMicrolensProto(options => {
    options.FormatterKey = Registry.ProtoFormatterKey.Default;
    options.SinkKey = Registry.ProtoSinkKey.Default;
    options.CaptureMode = Registry.ProtoCaptureMode.Both;
    options.LogScope = Registry.ProtoLogScope.Both;
    options.LogLevel = LogLevel.Debug;
});
```

| Option | Default | Effect |
| :--- | :--- | :--- |
| `FormatterKey` | `Default` | `None`, `Default`, `Json` or `Custom`. `None` logs metadata only and skips decoding. |
| `CustomFormatterName` | — | Keyed formatter name. Used only when `FormatterKey = Custom`. Unknown names fall back to `Default`. |
| `SinkKey` | `Default` | `None`, `Default` or `Custom`. `None` disables all tracing work. |
| `CustomSinkName` | — | Keyed sink name. Used only when `SinkKey = Custom`. Unknown names fall back to `Default`. |
| `CaptureMode` | `Both` | Phases that are intercepted. |
| `LogScope` | `Both` | Phases that are emitted. A phase is traced only when enabled in **both** `CaptureMode` and `LogScope`. |
| `LogLevel` | `Debug` | Level passed to the sink. Nothing is buffered or decoded when the sink is disabled for this level. |
| `GlobalHandlerEnabled` | `true` | `HttpClient` handler on/off. |
| `GlobalMiddlewareEnabled` | `true` | ASP.NET Core middleware on/off. |
| `GlobalClientInterceptorEnabled` | `true` | `gRPC` client interceptor on/off. |
| `GlobalServerInterceptorEnabled` | `true` | `gRPC` server interceptor on/off. |

### Opting Out

| Channel | Per request / endpoint |
| :--- | :--- |
| `HttpClient` | `request.SkipProtoHandler()` or `request.Content.SkipProtoHandler()` |
| ASP.NET Core Middleware | `[SkipProtoMiddleware]` on the controller class, or `.WithMetadata(new SkipProtoMiddlewareAttribute())` on a minimal API endpoint |
| `gRPC` Client | `callOptions.SkipProtoInterceptor()` |
| `gRPC` Server | `[SkipProtoInterceptor]` on the service class |

The extensions live in `Microlens.Proto.Extensions`; the attributes in `Microlens.Proto.Attributes`. Skip markers are removed before the request leaves the process.

---

## Extensible Architecture

Register:

* Custom formatters: control how decoded nodes are rendered.
* Custom sinks: control where the rendered output goes.

### Output Anatomy

The **formatter** renders the payload. The **sink** adds the metadata and writes it. The default sink produces:

```text
TimestampUtc = `2026-06-08T12:00:00.0000000Z`, Channel = `Grpc`, Direction = `Inbound`, Phase = `Request`, Path = `/envelope.EnvelopeService/Post`
Payload:
├── Field 1 (LengthDelimited): HardwareRev
├── Field 2 (LengthDelimited): v3.2
├── Field 3 (Varint): 42
└── Field 4 (Fixed64): 987654321
```

Structured providers (Serilog, Seq, Application Insights, OpenTelemetry) receive the properties `TimestampUtc`, `Channel`, `Direction`, `Phase`, `Path` and `Payload` through `ILogger`. No custom sink is needed for them.

### Custom Formatters

#### Example: Compact Formatter

```csharp
using Microlens.Proto.Formatters;
using Microlens.Proto.Models;
using Microlens.Proto.Shared;

public sealed class CompactProtoFormatter : IProtoFormatter {
    public Registry.ProtoFormatterKey Key => Registry.ProtoFormatterKey.Custom;

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
    options.FormatterKey = Registry.ProtoFormatterKey.Custom;
    options.CustomFormatterName = "Compact";
});
```

The registration key (`"Compact"`) is what resolves the formatter. Nodes passed to `Format` reference the inspected buffer, so they are only valid for the duration of the call.

### Custom Sinks

#### Example: Console Sink

```csharp
using Microlens.Proto.Models;
using Microlens.Proto.Shared;
using Microlens.Proto.Sinks;
using Microsoft.Extensions.Logging;

public sealed class ConsoleProtoSink : IProtoSink {
    public Registry.ProtoSinkKey Key => Registry.ProtoSinkKey.Custom;

    public string Name => "Console";

    public bool IsEnabled(LogLevel level) {
        return level >= LogLevel.Information;
    }

    public Task LogAsync(LogLevel level, IProtoScope scope, string payload, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        Console.WriteLine($"[{level}] {scope.TimestampUtc:O} {scope.Channel} {scope.Direction} {scope.Phase} {scope.Path}{Environment.NewLine}{payload}");
        return Task.CompletedTask;
    }

    public Task LogAsync(LogLevel level, IProtoScope scope, string payload, Exception exception, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        Console.WriteLine($"[{level}] {scope.TimestampUtc:O} {scope.Channel} {scope.Direction} {scope.Phase} {scope.Path}{Environment.NewLine}{payload}{Environment.NewLine}{exception}");
        return Task.CompletedTask;
    }
}
```

#### Register: Console Sink

```csharp
builder.Services.AddSink<ConsoleProtoSink>("Console");

builder.Services.AddMicrolensProto(options => {
    options.SinkKey = Registry.ProtoSinkKey.Custom;
    options.CustomSinkName = "Console";
    options.LogLevel = LogLevel.Information;
});
```

`IsEnabled` is optional and defaults to `level != LogLevel.None`. Returning `false` skips buffering, decoding and formatting entirely.

---

## Performance Characteristics

**`Microlens.Proto`** is designed for production environments and high-throughput workloads.

Key implementation details:

- `ReadOnlySequence<byte>` / `SequenceReader<byte>` based parsing
- `stackalloc` fixed-width reads
- Exception-free `UTF-8` detection
- Buffer pooling for `HTTP` bodies through `RecyclableMemoryStream`
- Streamed `gRPC` messages traced individually, without stream buffering
- No work performed when the sink is disabled for the configured `LogLevel`
- Decoding skipped entirely with the `None` formatter
- Reflection-free `JSON` output via `Utf8JsonWriter`

Costs to be aware of:

- Each traced `gRPC` message is re-serialized once (`IMessage.ToByteArray()`) for inspection.
- Captured `HTTP` bodies are buffered in full.
- A buffered middleware response is delivered to the client when the downstream pipeline completes, not incrementally.
- Decoding and formatting run synchronously on the calling thread.

---

## Limitations

* **Groups**: `StartGroup` / `EndGroup` (deprecated) are not supported. Decoding stops at the first group, and the fields decoded so far are returned.
* **Malformed payloads**: These return a partial tree without error.
* **Varints**: Shown as raw unsigned values. There is no ZigZag (`sint32`/`sint64`) decoding, and negative `int32`/`int64` values appear as large unsigned numbers.
* **`Fixed32` / `Fixed64`**: Shown as unsigned integers. `float`/`double` values are not decoded (e.g. `1098488218` is `15.6f`).
* **Strings vs messages**: A length-delimited field that is also a valid message is shown as nested, even if it was a string (e.g. `"hi"` decodes as `Field 13 (Varint): 105`).
* **Packed repeated fields**: These appear as a single length-delimited field.
* **Non-`IMessage` gRPC types**: `gRPC` messages that are not `Google.Protobuf` `IMessage` instances (e.g. `protobuf-net` code-first contracts) are not traced.
* **Wildcard `Accept`**: Values such as `*/*` do not trigger middleware response capture.

---

## Comparison

| Capability | `Microlens.Proto` | Traditional `Protobuf` Libraries |
| :--- | :---: | :---: |
| Serialize known contracts | No | Yes |
| Deserialize known contracts | No | Yes |
| Schemaless inspection | Yes | Limited |
| Nested message discovery | Yes | Limited |
| Works without `.proto` files | Yes | No |
| Works without generated classes | Yes | No |
| Decode unknown `Protobuf` payloads | Yes | No |
| `HTTP` payload interception | Yes | No |
| `gRPC` payload interception | Yes | No |
| Logging integration | Yes | No |
| Extensible formatters | Yes | No |
| Extensible sinks | Yes | No |

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

## Articles

* [Schemaless Protocol Buffers (Protobuf) Decoder & Inspector — Part 1](https://medium.com/@mansoor.afzal/schemaless-protocol-buffers-protobuf-decoder-inspector-part-1-3d265c671e35)
* [Schemaless Protocol Buffers (Protobuf) Decoder & Inspector — Part 2](https://medium.com/@mansoor.afzal/schemaless-protocol-buffers-protobuf-decoder-inspector-part-2-8d1d63bad109)

---

## License

Licensed under the **Apache License 2.0**.

---
