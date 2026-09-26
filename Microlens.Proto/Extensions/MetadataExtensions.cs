using Grpc.Core;
using Microlens.Internal;
using System;

namespace Microlens.Proto.Extensions;

internal static class MetadataExtensions {
    internal static bool Contains(this Metadata? metadata, string key) {
        if (metadata == null) {
            return false;
        }

        foreach (var entry in metadata) {
            if (entry.Key.Equals(key, StringComparison.OrdinalIgnoreCase)) {
                return true;
            }
        }

        return false;
    }

    internal static Metadata Without(this Metadata metadata, string key) {
        Guard.NotNull(metadata);
        Guard.NotNullOrWhiteSpace(key);

        var copy = new Metadata();

        for (int i = 0; i < metadata.Count; i++) {
            Metadata.Entry entry = metadata[i];

            if (!entry.Key.Equals(key, StringComparison.OrdinalIgnoreCase)) {
                copy.Add(entry);
            }
        }

        return copy;
    }
}
