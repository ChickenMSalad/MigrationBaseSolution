using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MigrationBase.Core.Cloud.Azure.Worker.Dispatch;

public sealed class InMemoryAzureWorkerDispatchQueue :
    IAzureWorkerDispatchQueueReader,
    IAzureWorkerDispatchQueueWriter
{
    private readonly object gate = new();
    private readonly Queue<AzureWorkerDispatchEnvelope> readyQueue = new();
    private readonly List<AzureWorkerDispatchEnvelope> delayedQueue = new();

    public Task EnqueueAsync(
        AzureWorkerDispatchEnvelope envelope,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        cancellationToken.ThrowIfCancellationRequested();

        lock (gate)
        {
            if (envelope.NotBeforeUtc is null ||
                envelope.NotBeforeUtc <= DateTimeOffset.UtcNow)
            {
                readyQueue.Enqueue(envelope);
            }
            else
            {
                delayedQueue.Add(envelope);
            }
        }

        return Task.CompletedTask;
    }

    public Task<AzureWorkerDispatchReadResult> ReadAsync(
        AzureWorkerDispatchReadRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var maxMessages = Math.Max(1, request.MaxMessages);
        var envelopes = new List<AzureWorkerDispatchEnvelope>(maxMessages);

        lock (gate)
        {
            PromoteDelayedMessages(request);

            while (readyQueue.Count > 0 && envelopes.Count < maxMessages)
            {
                envelopes.Add(readyQueue.Dequeue());
            }
        }

        return Task.FromResult(
            envelopes.Count == 0
                ? AzureWorkerDispatchReadResult.Empty
                : AzureWorkerDispatchReadResult.FromEnvelopes(envelopes));
    }

    private void PromoteDelayedMessages(AzureWorkerDispatchReadRequest request)
    {
        if (delayedQueue.Count == 0)
        {
            return;
        }

        var eligible = delayedQueue
            .Where(message =>
                request.IncludeDelayedMessages ||
                message.NotBeforeUtc is null ||
                message.NotBeforeUtc <= request.RequestedAtUtc)
            .ToList();

        foreach (var message in eligible)
        {
            delayedQueue.Remove(message);
            readyQueue.Enqueue(message);
        }
    }
}
