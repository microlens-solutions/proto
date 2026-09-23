using Grpc.Core;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microlens.Proto.Pipeline;

internal sealed class ProtoStreamReader<TMessage>(IAsyncStreamReader<TMessage> inner, Func<TMessage, Task> trace) : IAsyncStreamReader<TMessage> where TMessage : class {
    public TMessage Current => inner.Current;

    public async Task<bool> MoveNext(CancellationToken cancellationToken) {
        bool hasNext = await inner.MoveNext(cancellationToken).ConfigureAwait(false);

        if (hasNext) {
            _ = trace(inner.Current);
        }

        return hasNext;
    }
}
