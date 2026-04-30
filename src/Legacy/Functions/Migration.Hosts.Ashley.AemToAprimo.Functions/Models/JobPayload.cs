namespace Migration.Hosts.Ashley.AemToAprimo.Functions.Models;

public sealed class JobPayload
{
    public string JobId { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public string Input { get; set; } = string.Empty;
}
