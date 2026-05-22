using System.Text;
using System.Text.Json;

namespace Migration.ControlPlane.Queues;

public static class QueueMessageSerialization
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public static string ToJson(QueueMessageEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        return JsonSerializer.Serialize(envelope, Options);
    }

    public static BinaryData ToBinaryData(QueueMessageEnvelope envelope) =>
        BinaryData.FromString(ToJson(envelope));

    public static string ToBase64Json(QueueMessageEnvelope envelope)
    {
        var json = ToJson(envelope);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    public static QueueMessageEnvelope FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("Queue message JSON cannot be empty.", nameof(json));
        }

        return JsonSerializer.Deserialize<QueueMessageEnvelope>(json, Options)
               ?? throw new InvalidOperationException("Queue message JSON could not be deserialized.");
    }

    public static QueueMessageEnvelope FromBase64Json(string base64)
    {
        if (string.IsNullOrWhiteSpace(base64))
        {
            throw new ArgumentException("Queue message base64 payload cannot be empty.", nameof(base64));
        }

        var json = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
        return FromJson(json);
    }
}
