using System.Text.Json;
using Migration.Application.Abstractions.OperationalStore;

namespace Migration.Application.OperationalStore;

public sealed class OperationalQueueMessageSerializer : IOperationalQueueMessageSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public string Serialize(
        OperationalQueueMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        return JsonSerializer.Serialize(
            message,
            SerializerOptions);
    }

    public OperationalQueueMessage Deserialize(
        string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new ArgumentException(
                "Queue message payload cannot be null or whitespace.",
                nameof(payload));
        }

        var message = JsonSerializer.Deserialize<OperationalQueueMessage>(
            payload,
            SerializerOptions);

        if (message is null)
        {
            throw new InvalidOperationException(
                "Queue message payload could not be deserialized.");
        }

        Validate(message);

        return message;
    }

    public bool TryDeserialize(
        string payload,
        out OperationalQueueMessage? message)
    {
        message = null;

        if (string.IsNullOrWhiteSpace(payload))
        {
            return false;
        }

        try
        {
            message = Deserialize(payload);

            return true;
        }
        catch
        {
            message = null;

            return false;
        }
    }

    private static void Validate(
        OperationalQueueMessage message)
    {
        if (message.RunId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Operational queue message is missing RunId.");
        }

        if (message.ManifestRecordId < 0)
        {
            throw new InvalidOperationException(
                "Operational queue message is missing ManifestRecordId.");
        }

        if (message.WorkItemId < 0)
        {
            throw new InvalidOperationException(
                "Operational queue message is missing WorkItemId.");
        }

        if (string.IsNullOrWhiteSpace(message.SourceId))
        {
            throw new InvalidOperationException(
                "Operational queue message is missing SourceId.");
        }
    }
}
