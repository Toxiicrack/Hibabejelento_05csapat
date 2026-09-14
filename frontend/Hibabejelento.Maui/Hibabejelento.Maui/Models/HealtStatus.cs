namespace Hibabejelento.Maui.Models;

public class HealthStatus
{
    public string Status { get; set; } = "Unknown";
    public string Database { get; set; } = "Unknown";
    public DateTime Timestamp { get; set; }
    public string Version { get; set; } = "-";
}