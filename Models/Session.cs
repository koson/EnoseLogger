namespace EnoseLogger.Models;

public class Session
{
    public string SessionId { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public int SampleCount { get; set; }
    public string FolderPath { get; set; } = string.Empty;
}
