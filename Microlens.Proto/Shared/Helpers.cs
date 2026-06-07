using Grpc.Core;
using Microlens.Proto.Attributes;
using Microlens.Proto.Extensions;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;

namespace Microlens.Proto.Shared;

internal static class Helpers {
    internal static bool ShouldApplyHandler(HttpContentHeaders? headers) {
        return IsProtobuf(headers?.ContentType?.MediaType);
    }

    internal static bool ShouldApplyMiddleware(string? contentType) {
        return IsProtobuf(contentType);
    }

    internal static bool ShouldApplyInterceptor(string methodName, MethodType methodType) {
        return string.Equals(methodName, methodType.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    internal static bool ShouldApplyInterceptor(string? contentType) {
        return IsGrpc(contentType);
    }

    internal static bool ShouldSkipHandler(HttpContentHeaders? headers) {
        return headers?.Contains(Constants.K_SKIP_PROTO_HANDLER) == true;
    }

    internal static bool ShouldSkipMiddleware(EndpointMetadataCollection? metadata) {
        return metadata?.GetMetadata<SkipProtoMiddlewareAttribute>() != null;
    }

    internal static bool ShouldSkipInterceptor(Metadata? headers) {
        return headers.Contains(Constants.K_SKIP_PROTO_INTERCEPTOR) == true;
    }

    internal static bool ShouldSkipInterceptor(EndpointMetadataCollection? metadata) {
        return metadata?.GetMetadata<SkipProtoInterceptorAttribute>() != null;
    }

    private static bool IsProtobuf(string? contentType) {
        return string.Equals(contentType, "application/protobuf", StringComparison.OrdinalIgnoreCase) || string.Equals(contentType, "application/x-protobuf", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsGrpc(string? contentType) {
        return string.Equals(contentType, "application/grpc", StringComparison.OrdinalIgnoreCase);
    }
}
