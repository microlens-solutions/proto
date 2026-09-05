using Grpc.Core;

namespace Microlens.Proto.Pipeline;

internal sealed class ProtoServerStreamWriter<TMessage>(IServerStreamWriter<TMessage> inner, Func<TMessage, Task> trace) : IServerStreamWriter<TMessage> where TMessage : class {
    public WriteOptions? WriteOptions {
        get => inner.WriteOptions;
        set => inner.WriteOptions = value;
    }

    public Task WriteAsync(TMessage message) {
        _ = trace(message);
        return inner.WriteAsync(message);
    }
}
