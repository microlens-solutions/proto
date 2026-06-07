# Microlens.Proto

**`Microlens.Proto`** is a high-performance diagnostic and logging library for .NET designed to automatically intercept, decode, display and log **`Protocol Buffers (Protobuf)`** payloads across various network boundaries. 

It analyzes `binary Protobuf` data on the fly **without requiring original `.proto` schema definitions**, making it an ideal tool for debugging, auditing and reverse-engineering distributed microservices.

---

## High-level Architecture

```text
┌────────────────────────────────┐        ┌────────────────────────────────┐
│       HTTP Network Layer       │        │       gRPC Network Layer       │
│      (Handler/Middleware)      │        │ (Interceptors [Client/Server]) │
└───────────────┬────────────────┘        └───────────────┬────────────────┘
                │                                         │
                │ ReadOnlySequence<byte>                  │ IMessage
                │                                         │
                ▼                                         ▼
┌────────────────────────────────┐        ┌────────────────────────────────┐
│          ProtoDecoder          │        │         ProtoInspector         │
└───────────────┬────────────────┘        └───────────────┬────────────────┘
                │                                         │
                │ IReadOnlyList<ProtoNode>                │ IReadOnlyList<ProtoNode>
                │                                         │
                └────────────────────┬────────────────────┘
                                     │
                                     ▼
┌──────────────────────────────────────────────────────────────────────────┐
│                  ProtoFormatterResolver / ProtoFormatter                 │
│                   (Default: Tree, Json, None or Custom)                  │
└────────────────────────────────────┬─────────────────────────────────────┘
                                     │
                                     ▼
┌──────────────────────────────────────────────────────────────────────────┐
│                       ProtoSinkResolver / ProtoSink                      │
│                     (Default: Logger, None or Custom)                    │
└──────────────────────────────────────────────────────────────────────────┘
```

---

## Features

* **Schema-less Protobuf Decoding:** Manually parses wire formats (`Varints`, `Fixed32`, `Fixed64`, `Length-Delimited`) directly from binary data streams.

* **Deep Nested Inspection:** Heuristically detects and recursively decodes nested sub-messages up to a maximum depth of 64.

* **Intelligent String Heuristics:** Safely attempts `UTF-8` string decoding and automatically falling back to raw byte presentation if control characters are detected.

* **End-to-End Interception:** Comprehensive support across the .NET network landscape:
  * **HttpClient Delegating Handler:** Intercepts outbound `HTTP` payloads.
  
  * **ASP.NET Core Middleware:** Intercepts inbound `REST/Protobuf` traffic.
  
  * **gRPC Server Interceptor:** Audits inbound server-side `gRPC` requests and responses.
  
  * **gRPC Client Interceptor:** Audits outbound client-side `gRPC` requests and responses.

* **Memory Efficient:** Engineered using modern performance types (`ReadOnlySequence<byte>`, `Span<T>`) and optimized buffer pooling via `Microsoft.IO.RecyclableMemoryStream`.

* **Extensible Architecture:** Easily register custom visual formatters and custom log sinks to route payload telemetry where you need it.

* **Configuration Options** You can control the capture and log behavior via below `ProtoOptions`:

| Property | Type | Default | Description |
| :--- | :--- | :--- | :--- |
| `CaptureMode` | `ProtoCaptureMode` | `Both` | Dictates whether to inspect `Request`, `Response`, or `Both`. |
| `LogScope` | `ProtoLogScope` | `Both` | Dictates whether to write logs for `Request`, `Response`, or `Both`. |
| `LogLevel` | `LogLevel` | `Debug` | The standard Microsoft logging level to emit data under. |
| `FormatterKey` | `ProtoFormatterKey` | `Default` | Selects structural visualization style (`Default`, `Json`, `None`, `Custom`). |
| `SinkKey` | `ProtoSinkKey` | `Default` | Selects structural destination target (`Default`, `None`, `Custom`). |
| `CustomFormatterName` | `string` | `""` | The lookup key name of your custom formatter registration if `FormatterKey` is set to `Custom`. |
| `CustomSinkName` | `string` | `""` | The lookup key name of your custom log sink registration if `SinkKey` is set to `Custom`. |
| `GlobalHandlerEnabled` | `bool` | `true` | Globally activates or deactivates the outbound `Http` payload handler layer. |
| `GlobalMiddlewareEnabled` | `bool` | `true` | Globally activates or deactivates the inbound `ASP.NET Core` request pipeline middleware layer. |
| `GlobalClientInterceptorEnabled` | `bool` | `true` | Globally activates or deactivates the outbound client-side `gRPC` message channel inspector. |
| `GlobalServerInterceptorEnabled` | `bool` | `true` | Globally activates or deactivates the inbound server-side `gRPC` service method inspector. |

---

## Getting Started

### 1. Add NuGet Package
```bash
dotnet add package Microlens.Proto
```

### 2. Registration via Dependency Injection

#### i. Add namespaces for service and middleware registration and startup options:

```csharp
using Microlens.Proto.Extensions;
using Microlens.Proto.Shared;
```

#### ii. Register service:

- With default options:

```csharp
builder.Services.AddMicrolensProto();
```

- With customized options:

```csharp
builder.Services.AddMicrolensProto(options => {
    options.FormatterKey = ProtoFormatterKey.Json;
    options.SinkKey = ProtoSinkKey.Custom;
    options.CustomSinkName = "Serilog";
    options.CaptureMode = ProtoCaptureMode.Both;
    options.LogScope = ProtoLogScope.Both;
    options.LogLevel = LogLevel.Information;
});
```

#### iii. Register middleware:

```csharp
app.UseMicrolensProto();
```

### 3. Configure HTTP Payload Handling

HTTP payloads are automatically intercepted and parsed by the `Microlens.Proto` delegating handler. To ensure your outgoing requests are properly captured, make sure to set the `Content-Type` header to `application/x-protobuf` or `application/protobuf` when sending Protobuf data.

```csharp
var payload = request.Payload.ToByteArray();
var content = new ByteArrayContent(payload);
content.Headers.ContentType = new MediaTypeHeaderValue("application/x-protobuf");
```

### 4. Opt-Out (Skip Microlens.Proto on Selective Endpoints)

If specific endpoints or outbound calls process highly confidential information or require zero processing overhead, you can selectively bypass `Microlens.Proto` parsing.

#### i. Skip HttpClient Payloads
Explicitly tag your outgoing `HttpContent` using the `SkipProtoHandler()` utility extension:

```csharp
var content = new StringContent(protobufBytes, Encoding.UTF8, "application/x-protobuf");
content.SkipProtoHandler();
await _httpClient.PostAsync("/api/resource", content);
```

#### ii. Skip ASP.NET Core Middleware
Decorate your `Controller` endpoints or Minimal API routes with the `[SkipProtoMiddleware]` attribute:

```csharp
[SkipProtoMiddleware]
[ApiController]
[Route("[controller]/[action]")]
public class ProtoController : ControllerBase {
    [HttpPost("/sensitive-data")]
	public IActionResult SecureEndpoint() {
		// ...
		return Ok();
	}
}
```

#### iii. Skip gRPC Client Interceptor
Use the fluent `SkipProtoInterceptor()` extension method directly on your client call configurations:

```csharp
var options = new CallOptions().SkipProtoInterceptor();
await _grpcClient.SayHelloAsync(request, options);
```

#### iv. Skip gRPC Server Interceptor
Decorate your `Service` with the `[SkipProtoInterceptor]` attribute:

```csharp
[SkipProtoInterceptor]
public class ProtoService : EnvelopeService.EnvelopeServiceBase {
    public override async Task<EnvelopeReply> Post(EnvelopeRequest request, ServerCallContext context) {
        // ...
        return new EnvelopeReply();
    }
}
```

---

## Formatters

### 1. Default Textual Trees

The `DefaultProtoFormatter` converts raw binary wire streams into a neat human-readable tree diagram structure directly inside your logs:

```text
Payload:
	├── Field 1 (Varint): 42
    ├── Field 2 (Fixed64): 1234567890
    ├── Field 3 (LengthDelimited)
    │   ├── Field 1 (Fixed64): 4630513161858373072
    │   └── Field 2 (Fixed32): 1098488218
    ├── Field 4 (LengthDelimited): System.ReadOnlyMemory<Byte>[6]
    ├── Field 5 (LengthDelimited)
    │   ├── Field 1 (LengthDelimited): HardwareRev
    │   └── Field 2 (LengthDelimited): v3.2
    ├── Field 5 (LengthDelimited)
    │   ├── Field 1 (LengthDelimited): NetworkMode
    │   └── Field 2 (LengthDelimited): Cellular
    ├── Field 6 (Varint): 999
    └── Field 7 (LengthDelimited): Last property is initialized
```

### 2. JSON

Switch the configuration to `ProtoFormatterKey.Json` to automatically output fully serialized syntax nodes for consumption by analytical tools like `Elasticsearch` or `Splunk`.

```json
Payload:
	[
	  {
		"FieldNumber": 1,
		"WireType": 0,
		"RawData": "Kg==",
		"Value": {
		  "Type": 1,
		  "Data": 42
		},
		"Children": []
	  },
	  {
		"FieldNumber": 2,
		"WireType": 1,
		"RawData": "0gKWSQAAAAA=",
		"Value": {
		  "Type": 4,
		  "Data": 1234567890
		},
		"Children": []
	  },
	  {
		"FieldNumber": 3,
		"WireType": 2,
		"RawData": "CdDVVuwv40JAEVD8GHPXml7AHZqZeUE=",
		"Value": {
		  "Type": 32,
		  "Data": [
			{
			  "FieldNumber": 1,
			  "WireType": 1,
			  "RawData": "0NVW7C/jQkA=",
			  "Value": {
				"Type": 4,
				"Data": 4630513161858373000
			  },
			  "Children": []
			},
			{
			  "FieldNumber": 2,
			  "WireType": 5,
			  "RawData": "mpl5QQ==",
			  "Value": {
				"Type": 2,
				"Data": 1098488218
			  },
			  "Children": []
			}
		  ]
		},
		"Children": [
		  {
			"FieldNumber": 1,
			"WireType": 1,
			"RawData": "0NVW7C/jQkA=",
			"Value": {
			  "Type": 4,
			  "Data": 4630513161858373000
			},
			"Children": []
		  },
		  {
			"FieldNumber": 2,
			"WireType": 5,
			"RawData": "mpl5QQ==",
			"Value": {
			  "Type": 2,
			  "Data": 1098488218
			},
			"Children": []
		  }
		]
	  },
	  {
		"FieldNumber": 4,
		"WireType": 2,
		"RawData": "yAGUA/cD",
		"Value": {
		  "Type": 16,
		  "Data": "yAGUA/cD"
		},
		"Children": []
	  },
	  {
		"FieldNumber": 5,
		"WireType": 2,
		"RawData": "CgtIYXJkd2FyZVJldhIEdjMuMg==",
		"Value": {
		  "Type": 32,
		  "Data": [
			{
			  "FieldNumber": 1,
			  "WireType": 2,
			  "RawData": "SGFyZHdhcmVSZXY=",
			  "Value": {
				"Type": 8,
				"Data": "HardwareRev"
			  },
			  "Children": []
			},
			{
			  "FieldNumber": 2,
			  "WireType": 2,
			  "RawData": "djMuMg==",
			  "Value": {
				"Type": 8,
				"Data": "v3.2"
			  },
			  "Children": []
			}
		  ]
		},
		"Children": [
		  {
			"FieldNumber": 1,
			"WireType": 2,
			"RawData": "SGFyZHdhcmVSZXY=",
			"Value": {
			  "Type": 8,
			  "Data": "HardwareRev"
			},
			"Children": []
		  },
		  {
			"FieldNumber": 2,
			"WireType": 2,
			"RawData": "djMuMg==",
			"Value": {
			  "Type": 8,
			  "Data": "v3.2"
			},
			"Children": []
		  }
		]
	  },
	  {
		"FieldNumber": 5,
		"WireType": 2,
		"RawData": "CgtOZXR3b3JrTW9kZRIIQ2VsbHVsYXI=",
		"Value": {
		  "Type": 32,
		  "Data": [
			{
			  "FieldNumber": 1,
			  "WireType": 2,
			  "RawData": "TmV0d29ya01vZGU=",
			  "Value": {
				"Type": 8,
				"Data": "NetworkMode"
			  },
			  "Children": []
			},
			{
			  "FieldNumber": 2,
			  "WireType": 2,
			  "RawData": "Q2VsbHVsYXI=",
			  "Value": {
				"Type": 8,
				"Data": "Cellular"
			  },
			  "Children": []
			}
		  ]
		},
		"Children": [
		  {
			"FieldNumber": 1,
			"WireType": 2,
			"RawData": "TmV0d29ya01vZGU=",
			"Value": {
			  "Type": 8,
			  "Data": "NetworkMode"
			},
			"Children": []
		  },
		  {
			"FieldNumber": 2,
			"WireType": 2,
			"RawData": "Q2VsbHVsYXI=",
			"Value": {
			  "Type": 8,
			  "Data": "Cellular"
			},
			"Children": []
		  }
		]
	  },
	  {
		"FieldNumber": 6,
		"WireType": 0,
		"RawData": "5wc=",
		"Value": {
		  "Type": 1,
		  "Data": 999
		},
		"Children": []
	  },
	  {
		"FieldNumber": 7,
		"WireType": 2,
		"RawData": "TGFzdCBwcm9wZXJ0eSBpcyBpbml0aWFsaXplZA==",
		"Value": {
		  "Type": 8,
		  "Data": "Last property is initialized"
		},
		"Children": []
	  }
	]
```

---
