# 🔬 E-Nose Session Logger

Blazor Server application for collecting E-Nose sensor data via MQTT for machine learning analysis.

## 📋 Features

- **Session-based logging** - Start/stop data collection sessions
- **iPad-friendly UI** - Responsive design for mobile use
- **Auto-timeout** - Sessions automatically stop after 1 hour
- **CSV Export** - Download session data for ML training
- **Real-time monitoring** - Live sample counter and duration display
- **MQTT Integration** - Subscribe to TWEN-001/TWEN-002 devices

## 🎯 Use Case

Collect scent/odor samples from E-Nose devices for:
- Machine learning classification
- Pattern recognition
- Quality control analysis

## 🏗️ Architecture

```
iPad/Browser
    ↓ http://139.162.12.61:5003
Blazor Server (Port 5003)
    ↓ MQTT Subscribe
MQTT Broker (139.162.62.210:1883)
    ← TWEN-001, TWEN-002
```

## 🚀 Deployment

### Local Development

```bash
dotnet run
```

Open: `http://localhost:5003`

### Production (Linode)

1. **Build & Publish:**
   ```bash
   dotnet publish -c Release -o /var/www/enose-logger/publish
   ```

2. **Create systemd service:**
   ```bash
   sudo nano /etc/systemd/system/enose-logger.service
   ```

   Content:
   ```
   [Unit]
   Description=E-Nose Session Logger
   After=network.target

   [Service]
   Type=simple
   User=www-data
   WorkingDirectory=/var/www/enose-logger/publish
   ExecStart=/usr/bin/dotnet /var/www/enose-logger/publish/EnoseLogger.dll
   Restart=always
   RestartSec=10
   TimeoutStartSec=300
   TimeoutStopSec=30

   Environment=ASPNETCORE_ENVIRONMENT=Production
   Environment=ASPNETCORE_URLS=http://0.0.0.0:5003

   [Install]
   WantedBy=multi-user.target
   ```

3. **Start service:**
   ```bash
   sudo systemctl daemon-reload
   sudo systemctl start enose-logger
   sudo systemctl enable enose-logger
   sudo systemctl status enose-logger
   ```

4. **Configure firewall:**
   ```bash
   sudo ufw allow 5003/tcp
   ```

## 📱 Usage (iPad)

1. Open Safari: `http://139.162.12.61:5003`
2. Add to Home Screen (optional)
3. Select device (TWEN-001 or TWEN-002)
4. Click **Start Logging**
5. Wait for data collection (monitoring counter)
6. Enter label (e.g., "coffee", "smoke")
7. Click **Stop & Save**
8. Download CSV file

## 📊 Data Format

**CSV Output:**
```csv
timestamp,SO2,NO2,NO,CO,VOC1,VOC2,Temp,RH
2026-03-26 08:30:01,1093,2378,1108,1096,920,1134,494,297
2026-03-26 08:30:02,1095,2380,1110,1098,922,1136,495,298
```

**Metadata (JSON):**
```json
{
  "session_id": "20260326_0830",
  "device_id": "TWEN-002",
  "label": "coffee_arabica",
  "start_time": "2026-03-26 08:30:00",
  "end_time": "2026-03-26 08:45:30",
  "sample_count": 930,
  "duration_seconds": 930
}
```

## ⚙️ Configuration

Edit `appsettings.json`:

```json
{
  "MqttConfiguration": {
    "BrokerHost": "139.162.62.210",
    "BrokerPort": 1883,
    "ClientId": "EnoseLogger_Linode_Port5003"
  }
}
```

## 🗂️ Output Structure

```
sessions/
  session_20260326_0830_TWEN-002/
    data.csv          (sensor readings)
    metadata.json     (session info)
  session_20260326_0945_TWEN-001/
    data.csv
    metadata.json
```

## 🔧 Tech Stack

- **.NET 9** - Framework
- **Blazor Server** - Interactive UI
- **MQTTnet 4.3.6** - MQTT client
- **Bootstrap 5** - Responsive CSS

## 📝 Logs

View service logs:
```bash
sudo journalctl -u enose-logger -f
```

## 🛠️ Troubleshooting

**MQTT not connecting:**
- Check broker IP: `ping 139.162.62.210`
- Verify firewall allows outbound on port 1883

**Service not starting:**
```bash
sudo systemctl status enose-logger
journalctl -u enose-logger -n 50
```

**CSV download fails:**
- Check permissions: `ls -la /var/www/enose-logger/publish/sessions`
- Ensure www-data has write access

## 📄 License

Internal use only - TAW E-Nose Project 2026

---

**Deployed:** Port 5003  
**URL:** http://139.162.12.61:5003  
**MQTT Broker:** 139.162.62.210:1883
