using Grpc.Core;
using System;
using System.Threading.Tasks;

namespace Microlens.Proto.Pipeline;

internal sealed class ProtoClientStreamWriter<TMessage>(IClientStreamWriter<TMessage> inner, Func<TMessage, Task> trace) : IClientStreamWriter<TMessage> where TMessage : class {
    public WriteOptions? WriteOptions {
        get => inner.WriteOptions;
        set => inner.WriteOptions = value;
    }

    public Task WriteAsync(TMessage message) {
        _ = trace(message);
        return inner.WriteAsync(message);
    }

    public Task CompleteAsync() {
        return inner.CompleteAsync();
    }
}
