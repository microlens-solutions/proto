using Grpc.Core;
using System.Net.Http.Headers;

namespace Microlens.Proto.Shared;

internal static class Helpers {
    internal static bool IsMiddlewareApplicable(string? contentType) {
        return string.Equals(contentType, "application/protobuf", StringComparison.OrdinalIgnoreCase) || string.Equals(contentType, "application/x-protobuf", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsHandlerApplicable(MediaTypeHeaderValue? contentType) {
        return IsMiddlewareApplicable(contentType?.MediaType);
    }

    internal static bool IsInceptorApplicable(string? contentType) {
        return string.Equals(contentType, "application/grpc", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsInceptorApplicable(string methodName, MethodType methodType) {
        return string.Equals(methodName, methodType.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
