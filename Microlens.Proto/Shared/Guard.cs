using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Microlens.Proto.Shared;

internal static class Guard {
    internal static void NotNull<T>([NotNull] T? argument, [CallerArgumentExpression(nameof(argument))] string? name = null) {
        if (argument is null) {
            throw new ArgumentNullException(name);
        }
    }

    internal static void NotNullOrWhiteSpace([NotNull] string? argument, [CallerArgumentExpression(nameof(argument))] string? name = null) {
        NotNull(argument, name);

        if (string.IsNullOrWhiteSpace(argument)) {
            throw new ArgumentException("The value cannot be an empty string or composed entirely of whitespace.", name);
        }
    }
}
