# 🚀 Quick Start Guide - E-Nose Logger

## 📱 For iPad Users (Recommended)

### 1. Access the App
- Open Safari
- Navigate to: `http://139.162.12.61:5003`
- (Optional) Tap Share → **Add to Home Screen** for app-like experience

### 2. Start a Session
1. Select your device: **TWEN-001** or **TWEN-002**
2. Tap **▶️ Start Logging**
3. Watch the sample counter increase

### 3. Stop & Save
1. Wait until you've collected enough samples
2. Enter a **Label**: "coffee", "smoke", "normal", etc.
   - *Leave empty to use timestamp*
3. Tap **⏹️ Stop & Save**

### 4. Download Data
- Scroll down to **Recent Sessions**
- Tap **⬇️ CSV** to download
- File saved to Downloads folder

---

## 💻 For Developers

### Local Testing

```bash
cd D:\GitHubRepos\__ENOSE_TOYOTA\Enose_2026\EnoseLogger
dotnet run
```

Open: http://localhost:5003

### Deployment to Linode

**Method 1: Using deployment script**
```bash
# On local machine
scp -r EnoseLogger root@139.162.12.61:/root/

# On Linode server
cd /root/EnoseLogger
chmod +x deploy.sh
./deploy.sh
```

**Method 2: Manual deployment**
```bash
# Build locally
dotnet publish -c Release -o ./publish

# Copy to server
scp -r publish root@139.162.12.61:/var/www/enose-logger/

# On server: setup systemd service (see README.md)
sudo systemctl start enose-logger
```

### Check Service Status

```bash
# Service status
sudo systemctl status enose-logger

# View logs (live)
sudo journalctl -u enose-logger -f

# View last 50 lines
sudo journalctl -u enose-logger -n 50
```

---

## 🎯 Typical Workflow

### Example: Coffee Scent Collection

1. **Prepare**
   - Open iPad Safari → `http://139.162.12.61:5003`
   - Ensure TWEN-002 is running and publishing MQTT

2. **Start Collection** (09:00)
   - Device: TWEN-002
   - Tap Start → Counter begins: 0, 1, 2, 3...

3. **Monitor** (09:00-09:15)
   - Samples collected: ~900 (15 min × 60 sec)
   - Duration shows: 15:00
   - Time remaining: 45:00

4. **Stop & Label** (09:15)
   - Label: "coffee_arabica_medium_roast"
   - Tap Stop → Session saved

5. **Download**
   - Download CSV: `enose_20260326_0900.csv`
   - Ready for ML training in Python/R

---

## 📊 Data Analysis (Python)

```python
import pandas as pd
import matplotlib.pyplot as plt

# Load session data
df = pd.read_csv('enose_20260326_0900.csv')

# Quick stats
print(df.describe())

# Plot VOC sensors
df.plot(y=['VOC1', 'VOC2'], figsize=(12,6))
plt.title('VOC Sensor Readings - Coffee Sample')
plt.ylabel('Sensor Value')
plt.show()

# Prepare for ML
X = df[['SO2', 'NO2', 'NO', 'CO', 'VOC1', 'VOC2', 'Temp', 'RH']]
# Add your labels...
```

---

## ⚠️ Important Notes

### Auto-Timeout
- Sessions automatically stop after **1 hour**
- If network disconnects, session stops at timeout
- Always enter label before closing browser!

### Network Requirements
- iPad must be on same network or have access to:
  - Web server: 139.162.12.61:5003
  - (Server needs access to MQTT: 139.162.62.210:1883)

### Storage
- Each session: ~50-500KB (depending on duration)
- 10 sessions/day = ~5MB/day
- Sessions stored in: `/var/www/enose-logger/publish/sessions/`

---

## 🔧 Troubleshooting

**Problem: Can't connect to http://139.162.12.61:5003**
- Check server: `sudo systemctl status enose-logger`
- Check firewall: `sudo ufw status`
- Check app is running: `sudo netstat -tlnp | grep 5003`

**Problem: Counter not increasing**
- Verify MQTT broker: `mosquitto_sub -h 139.162.62.210 -t "TAW/ENOSE/#"`
- Check device is publishing
- View logs: `sudo journalctl -u enose-logger -f`

**Problem: CSV download fails**
- Check file exists: `ls -la /var/www/enose-logger/publish/sessions/`
- Check permissions: `sudo chown -R www-data:www-data sessions/`

**Problem: Session stuck "Loading..."**
- Refresh browser
- Check logs for errors
- Restart service: `sudo systemctl restart enose-logger`

---

## 📞 Support

**Logs location:**
- Production: `/var/log/syslog` or `journalctl -u enose-logger`
- Local: Check console output

**Quick commands:**
```bash
# Restart service
sudo systemctl restart enose-logger

# View real-time logs
sudo journalctl -u enose-logger -f

# Check disk space
df -h

# List recent sessions
ls -lh /var/www/enose-logger/publish/sessions/
```

---

**Ready to collect data! 🔬**
