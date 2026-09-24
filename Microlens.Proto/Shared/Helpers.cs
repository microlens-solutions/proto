using Grpc.Core;
using Grpc.Core.Interceptors;
using Microlens.Proto.Extensions;
using Microlens.Proto.Models;
using System;
using System.Net.Http;

#if NET
using Microlens.Proto.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
#endif

namespace Microlens.Proto.Shared;

internal static class Helpers {
    internal static ProtoScope BuildScope(Registry.ProtoChannelType channel, Registry.ProtoDirectionType direction, Registry.ProtoPhaseType phase, string? path) {
        return new ProtoScope {
            TimestampUtc = DateTime.UtcNow,
            Channel = channel.ToString(),
            Direction = direction.ToString(),
            Phase = phase.ToString(),
            Path = path ?? string.Empty
        };
    }

    internal static bool IsProtobuf(string? contentType) {
        return IsProtobufMediaType(contentType.AsSpan());
    }

#if NET
    internal static bool AcceptsProtobuf(StringValues accept) {
        for (int i = 0; i < accept.Count; i++) {
            ReadOnlySpan<char> remaining = accept[i].AsSpan();

            while (!remaining.IsEmpty) {
                int separator = remaining.IndexOf(',');
                ReadOnlySpan<char> item = separator < 0 ? remaining : remaining[..separator];

                if (IsProtobufMediaType(item)) {
                    return true;
                }

                remaining = separator < 0 ? [] : remaining[(separator + 1)..];
            }
        }

        return false;
    }

    internal static bool IsGrpc(string? contentType) {
        ReadOnlySpan<char> media = GetMediaType(contentType.AsSpan());
        return media.StartsWith(Registry.MEDIA_TYPE_GRPC.AsSpan(), StringComparison.OrdinalIgnoreCase) && (media.Length == Registry.MEDIA_TYPE_GRPC.Length || media[Registry.MEDIA_TYPE_GRPC.Length] is '+' or '-');
    }

    internal static bool ShouldSkipMiddleware(EndpointMetadataCollection? metadata) {
        return metadata?.GetMetadata<SkipProtoMiddlewareAttribute>() is not null;
    }

    internal static bool ShouldSkipInterceptor(EndpointMetadataCollection? metadata) {
        return metadata?.GetMetadata<SkipProtoInterceptorAttribute>() is not null;
    }
#endif

    internal static bool TryConsumeSkipHeader(HttpRequestMessage request) {
        bool skip = request.Headers.Remove(Registry.K_SKIP_PROTO_HANDLER);

        if (request.Content is { } content) {
            skip |= content.Headers.Remove(Registry.K_SKIP_PROTO_HANDLER);
        }

        return skip;
    }

    internal static bool TryConsumeSkipHeader<TRequest, TResponse>(ref ClientInterceptorContext<TRequest, TResponse> context) where TRequest : class where TResponse : class {
        Metadata? headers = context.Options.Headers;

        if (headers is null || !headers.Contains(Registry.K_SKIP_PROTO_INTERCEPTOR)) {
            return false;
        }

        context = new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host, context.Options.WithHeaders(headers.Without(Registry.K_SKIP_PROTO_INTERCEPTOR)));
        return true;
    }

    private static bool IsProtobufMediaType(ReadOnlySpan<char> contentType) {
        ReadOnlySpan<char> media = GetMediaType(contentType);

        return media.Equals(Registry.MEDIA_TYPE_PROTOBUF.AsSpan(), StringComparison.OrdinalIgnoreCase) ||
               media.Equals(Registry.MEDIA_TYPE_X_PROTOBUF.AsSpan(), StringComparison.OrdinalIgnoreCase);
    }

    private static ReadOnlySpan<char> GetMediaType(ReadOnlySpan<char> contentType) {
        int separator = contentType.IndexOf(';');

#if NET
        return separator < 0 ? contentType : contentType[..separator];
#else
        return separator < 0 ? contentType : contentType.Slice(0, separator);
#endif
    }
}
