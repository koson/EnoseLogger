using EnoseLogger.Models;
using System.Text.Json;

namespace EnoseLogger.Services;

public class SessionManager : IDisposable
{
    private readonly MqttService _mqtt;
    private readonly ILogger<SessionManager> _logger;
    private readonly string _sessionsPath = "sessions";
    private readonly TimeZoneInfo _timeZone;
    
    private Session? _activeSession;
    private StreamWriter? _csvWriter;
    private System.Threading.Timer? _timeoutTimer;
    
    public Session? ActiveSession => _activeSession;
    public event Action? OnSessionChanged;
    
    public SessionManager(MqttService mqtt, ILogger<SessionManager> logger)
    {
        _mqtt = mqtt;
        _logger = logger;
        
        // Use Thailand timezone (UTC+7)
        try
        {
            _timeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok");
        }
        catch
        {
            // Fallback if timezone not found
            _timeZone = TimeZoneInfo.CreateCustomTimeZone(
                "SE Asia Standard Time", 
                TimeSpan.FromHours(7), 
                "SE Asia Standard Time", 
                "SE Asia Standard Time"
            );
        }
        
        Directory.CreateDirectory(_sessionsPath);
        _mqtt.OnMessageReceived += OnMqttMessage;
    }
    
    private DateTime GetLocalTime()
    {
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timeZone);
    }
    
    public async Task<Session> StartSessionAsync(string deviceId)
    {
        if (_activeSession != null)
        {
            _logger.LogWarning("Session already active");
            throw new InvalidOperationException("A session is already running");
        }
        
        var localTime = GetLocalTime();
        var timestamp = localTime.ToString("yyyyMMdd_HHmmss");
        var session = new Session
        {
            SessionId = timestamp,
            DeviceId = deviceId,
            StartTime = DateTime.UtcNow,  // Store as UTC for consistency across timezones
            FolderPath = Path.Combine(_sessionsPath, $"session_{timestamp}_{deviceId}")
        };
        
        try
        {
            // Create session folder
            Directory.CreateDirectory(session.FolderPath);
            
            // Create CSV file
            var csvPath = Path.Combine(session.FolderPath, "data.csv");
            _csvWriter = new StreamWriter(csvPath, false, System.Text.Encoding.UTF8);
            await _csvWriter.WriteLineAsync("timestamp,SO2,NO2,NO,CO,VOC1,VOC2,Temp,RH");
            await _csvWriter.FlushAsync();
            
            // Subscribe to MQTT topic
            await _mqtt.SubscribeAsync($"TAW/ENOSE/{deviceId}");
            
            // Start auto-timeout timer (1 hour)
            _timeoutTimer = new System.Threading.Timer(async _ =>
            {
                _logger.LogInformation("⏰ Session auto-timeout reached (1 hour)");
                await StopSessionAsync(session.SessionId, "auto-timeout");
            }, null, TimeSpan.FromHours(1), Timeout.InfiniteTimeSpan);
            
            _activeSession = session;
            _logger.LogInformation("▶️ Session started: {SessionId} for device {DeviceId}", 
                session.SessionId, deviceId);
            
            OnSessionChanged?.Invoke();
            
            return session;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start session");
            _csvWriter?.Dispose();
            _timeoutTimer?.Dispose();
            throw;
        }
    }
    
    private void OnMqttMessage(string topic, string payload)
    {
        if (_activeSession == null || _csvWriter == null) return;
        
        // Check if message is for our device
        if (!topic.EndsWith(_activeSession.DeviceId)) return;
        
        try
        {
            // Parse JSON payload
            var data = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(payload);
            if (data == null || data.Count < 10) return;
            
            var timestamp = data[1]["Time"].GetString();
            var so2 = data[2]["SO2"].GetInt32();
            var no2 = data[3]["NO2"].GetInt32();
            var no = data[4]["NO"].GetInt32();
            var co = data[5]["CO"].GetInt32();
            var voc1 = data[6]["VOC1"].GetInt32();
            var voc2 = data[7]["VOC2"].GetInt32();
            var temp = data[8]["Temp"].GetInt32();
            var rh = data[9]["RH"].GetInt32();
            
            // Write to CSV
            _csvWriter.WriteLine($"{timestamp},{so2},{no2},{no},{co},{voc1},{voc2},{temp},{rh}");
            _csvWriter.Flush();
            
            _activeSession.SampleCount++;
            // Duration will be calculated on client side using UTC
            
            OnSessionChanged?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing MQTT message");
        }
    }
    
    public async Task StopSessionAsync(string sessionId, string label)
    {
        if (_activeSession == null || _activeSession.SessionId != sessionId)
        {
            _logger.LogWarning("No active session to stop");
            return;
        }
        
        try
        {
            // Stop timers
            _timeoutTimer?.Dispose();
            _timeoutTimer = null;
            
            // Close CSV file
            if (_csvWriter != null)
            {
                await _csvWriter.FlushAsync();
                _csvWriter.Close();
                _csvWriter.Dispose();
                _csvWriter = null;
            }
            
            // Update session info
            _activeSession.EndTime = DateTime.UtcNow;  // Store as UTC
            _activeSession.Label = label;
            _activeSession.Duration = _activeSession.EndTime.Value - _activeSession.StartTime;
            
            // Save metadata
            var metadata = new
            {
                session_id = _activeSession.SessionId,
                device_id = _activeSession.DeviceId,
                label = _activeSession.Label,
                start_time = _activeSession.StartTime.ToString("yyyy-MM-dd HH:mm:ss"),
                end_time = _activeSession.EndTime?.ToString("yyyy-MM-dd HH:mm:ss"),
                sample_count = _activeSession.SampleCount,
                duration_seconds = (int)_activeSession.Duration.TotalSeconds
            };
            
            var metadataPath = Path.Combine(_activeSession.FolderPath, "metadata.json");
            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
            await File.WriteAllTextAsync(metadataPath, 
                JsonSerializer.Serialize(metadata, jsonOptions));
            
            // Unsubscribe from MQTT
            await _mqtt.UnsubscribeAsync($"TAW/ENOSE/{_activeSession.DeviceId}");
            
            _logger.LogInformation("⏹️ Session stopped: {SessionId} | Samples: {Count} | Label: {Label}",
                _activeSession.SessionId, _activeSession.SampleCount, label);
            
            _activeSession = null;
            OnSessionChanged?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping session");
            throw;
        }
    }
    
    public List<Session> GetRecentSessions(int count = 10)
    {
        var sessions = new List<Session>();
        
        try
        {
            var dirs = Directory.GetDirectories(_sessionsPath)
                .OrderByDescending(d => d)
                .Take(count);
            
            foreach (var dir in dirs)
            {
                var metadataPath = Path.Combine(dir, "metadata.json");
                if (!File.Exists(metadataPath)) continue;
                
                try
                {
                    var json = File.ReadAllText(metadataPath);
                    var meta = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                    
                    if (meta == null) continue;
                    
                    sessions.Add(new Session
                    {
                        SessionId = meta["session_id"].GetString() ?? "",
                        DeviceId = meta["device_id"].GetString() ?? "",
                        Label = meta.ContainsKey("label") ? meta["label"].GetString() ?? "" : "",
                        SampleCount = meta["sample_count"].GetInt32(),
                        StartTime = DateTime.Parse(meta["start_time"].GetString() ?? GetLocalTime().ToString()),
                        EndTime = meta.ContainsKey("end_time") 
                            ? DateTime.Parse(meta["end_time"].GetString() ?? GetLocalTime().ToString())
                            : null,
                        FolderPath = dir
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error reading session metadata: {Path}", metadataPath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent sessions");
        }
        
        return sessions;
    }
    
    public string? GetCsvPath(string sessionId)
    {
        var sessions = GetRecentSessions(50);
        var session = sessions.FirstOrDefault(s => s.SessionId == sessionId);
        
        if (session == null) return null;
        
        var csvPath = Path.Combine(session.FolderPath, "data.csv");
        return File.Exists(csvPath) ? csvPath : null;
    }
    
    public void Dispose()
    {
        _timeoutTimer?.Dispose();
        _csvWriter?.Dispose();
    }
}
