using Grpc.Core;

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

    internal static Metadata Remove(this Metadata? metadata, string key) {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        for (int i = metadata.Count - 1; i >= 0; i--) {
            if (metadata[i].Key.Equals(key, StringComparison.OrdinalIgnoreCase)) {
                metadata.RemoveAt(i);
            }
        }

        return metadata;
    }
}
