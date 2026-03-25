using MQTTnet;
using MQTTnet.Client;
using System.Text;

namespace EnoseLogger.Services;

public class MqttService
{
    private IMqttClient? _mqttClient;
    private MqttClientOptions? _mqttOptions;
    private readonly ILogger<MqttService> _logger;
    private bool _isConnected;
    
    public event Action<string, string>? OnMessageReceived;
    
    public MqttService(ILogger<MqttService> logger)
    {
        _logger = logger;
    }
    
    public async Task ConnectAsync(string broker, int port, string clientId)
    {
        if (_isConnected) return;
        
        try
        {
            var factory = new MqttFactory();
            _mqttClient = factory.CreateMqttClient();
            
            _mqttOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(broker, port)
                .WithClientId(clientId)
                .WithCleanSession(true)
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(60))
                .WithTimeout(TimeSpan.FromSeconds(10))
                .Build();
            
            _mqttClient.ApplicationMessageReceivedAsync += OnMqttMessageAsync;
            _mqttClient.DisconnectedAsync += OnDisconnectedAsync;
            
            await _mqttClient.ConnectAsync(_mqttOptions, CancellationToken.None);
            _isConnected = true;
            
            _logger.LogInformation("✅ Connected to MQTT Broker: {Broker}:{Port}", broker, port);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to connect to MQTT broker");
            throw;
        }
    }
    
    public async Task SubscribeAsync(string topic)
    {
        if (_mqttClient == null || !_isConnected)
        {
            _logger.LogWarning("MQTT client not connected");
            return;
        }
        
        var options = new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter(f => f.WithTopic(topic))
            .Build();
        
        await _mqttClient.SubscribeAsync(options, CancellationToken.None);
        _logger.LogInformation("📡 Subscribed to topic: {Topic}", topic);
    }
    
    public async Task UnsubscribeAsync(string topic)
    {
        if (_mqttClient == null || !_isConnected) return;
        
        var options = new MqttClientUnsubscribeOptionsBuilder()
            .WithTopicFilter(topic)
            .Build();
        
        await _mqttClient.UnsubscribeAsync(options, CancellationToken.None);
        _logger.LogInformation("🔕 Unsubscribed from topic: {Topic}", topic);
    }
    
    private Task OnMqttMessageAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        var topic = e.ApplicationMessage.Topic;
        var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
        
        _logger.LogDebug("📨 MQTT Message: {Topic} | {Payload}", topic, payload);
        
        OnMessageReceived?.Invoke(topic, payload);
        
        return Task.CompletedTask;
    }
    
    private async Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs e)
    {
        _isConnected = false;
        _logger.LogWarning("🔌 MQTT Disconnected: {Reason}", e.Reason);
        
        // Auto-reconnect
        await Task.Delay(5000);
        try
        {
            if (_mqttClient != null && _mqttOptions != null)
            {
                await _mqttClient.ConnectAsync(_mqttOptions, CancellationToken.None);
                _isConnected = true;
                _logger.LogInformation("🔄 MQTT Reconnected");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reconnect");
        }
    }
    
    public async Task DisconnectAsync()
    {
        if (_mqttClient == null) return;
        
        await _mqttClient.DisconnectAsync();
        _mqttClient.Dispose();
        _mqttClient = null;
        _isConnected = false;
        
        _logger.LogInformation("👋 Disconnected from MQTT broker");
    }
}
