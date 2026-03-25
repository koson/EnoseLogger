#!/bin/bash
set -e

echo "⏸️  Stopping service..."
systemctl stop enose-logger 2>/dev/null || true

echo "📁 Preparing directory..."
mkdir -p /var/www/enose-logger/publish
cd /var/www/enose-logger/publish

echo "🗑️  Cleaning old files..."
rm -rf *

echo "📦 Extracting package..."
tar -xzf /tmp/enose-deploy.tar.gz

echo "🔒 Setting permissions..."
chmod +x EnoseLogger
chown -R www-data:www-data /var/www/enose-logger/publish

echo "📁 Creating sessions directory..."
mkdir -p sessions
chown -R www-data:www-data sessions
chmod -R 775 sessions

echo "⚙️  Configuring systemd service..."
cat > /etc/systemd/system/enose-logger.service <<'EOF'
[Unit]
Description=E-Nose Session Logger
After=network.target

[Service]
Type=simple
User=www-data
WorkingDirectory=/var/www/enose-logger/publish
ExecStart=/var/www/enose-logger/publish/EnoseLogger
Restart=always
RestartSec=10
TimeoutStartSec=300
TimeoutStopSec=30

Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://0.0.0.0:5003

[Install]
WantedBy=multi-user.target
EOF

echo "🔄 Reloading systemd..."
systemctl daemon-reload

echo "🚀 Starting service..."
systemctl start enose-logger
systemctl enable enose-logger 2>/dev/null || true

echo ""
echo "⏳ Waiting for startup..."
sleep 5

echo ""
echo "✅ Deployment Complete!"
echo "======================================"
systemctl status enose-logger --no-pager -l | head -20

echo ""
echo "🌐 Application URL: http://139.162.12.61:5003"
echo "📊 View logs: journalctl -u enose-logger -f"
