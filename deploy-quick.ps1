# Quick Deploy to Linode - No Git Required
# Usage: .\deploy-quick.ps1

$SERVER = "139.162.12.61"
$USER = "root"
$REMOTE_DIR = "/var/www/enose-logger"

Write-Host "🚀 E-Nose Logger - Quick Deploy to Linode" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host ""

# Step 1: Build project
Write-Host "📦 Step 1/4: Building project..." -ForegroundColor Yellow
dotnet publish -c Release -o publish

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Build successful" -ForegroundColor Green
Write-Host ""

# Step 2: Create deployment package
Write-Host "📦 Step 2/4: Creating deployment package..." -ForegroundColor Yellow
Compress-Archive -Path publish\* -DestinationPath enose-logger.zip -Force
Write-Host "✅ Package created: enose-logger.zip" -ForegroundColor Green
Write-Host ""

# Step 3: Upload to server
Write-Host "📤 Step 3/4: Uploading to Linode..." -ForegroundColor Yellow
Write-Host "Target: $USER@$SERVER" -ForegroundColor Cyan

scp enose-logger.zip "$USER@${SERVER}:/tmp/"
scp deploy.sh "$USER@${SERVER}:/tmp/"

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Upload failed!" -ForegroundColor Red
    Write-Host "Make sure you can SSH to $SERVER" -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ Upload complete" -ForegroundColor Green
Write-Host ""

# Step 4: Execute deployment on server
Write-Host "🔧 Step 4/4: Installing on server..." -ForegroundColor Yellow
Write-Host ""

ssh "$USER@$SERVER" @"
set -e
echo '📁 Preparing directories...'
sudo mkdir -p $REMOTE_DIR
sudo mkdir -p $REMOTE_DIR/publish

echo '📦 Extracting files...'
cd $REMOTE_DIR
sudo unzip -o /tmp/enose-logger.zip -d publish/
sudo chown -R www-data:www-data publish/
sudo chmod -R 755 publish/

echo '📁 Creating sessions directory...'
sudo mkdir -p publish/sessions
sudo chown -R www-data:www-data publish/sessions
sudo chmod -R 775 publish/sessions

echo '⚙️  Creating systemd service...'
sudo tee /etc/systemd/system/enose-logger.service > /dev/null <<'EOF'
[Unit]
Description=E-Nose Session Logger
After=network.target

[Service]
Type=simple
User=www-data
WorkingDirectory=$REMOTE_DIR/publish
ExecStart=/usr/bin/dotnet $REMOTE_DIR/publish/EnoseLogger.dll
Restart=always
RestartSec=10
TimeoutStartSec=300
TimeoutStopSec=30

Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://0.0.0.0:5003

[Install]
WantedBy=multi-user.target
EOF

echo '🔄 Reloading systemd...'
sudo systemctl daemon-reload

echo '🔥 Configuring firewall...'
sudo ufw allow 5003/tcp 2>/dev/null || echo 'UFW not available'

echo '🚀 Starting service...'
sudo systemctl stop enose-logger 2>/dev/null || true
sudo systemctl start enose-logger
sudo systemctl enable enose-logger

echo ''
echo '⏳ Waiting for service to start...'
sleep 10

echo ''
echo '✅ Deployment Complete!'
echo '======================================'
sudo systemctl status enose-logger --no-pager
echo ''
echo '🌐 Application URL: http://139.162.12.61:5003'
echo '📊 View logs: sudo journalctl -u enose-logger -f'
echo ''
"@

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "🎉 Deployment Successful!" -ForegroundColor Green
    Write-Host "=====================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "🌐 Open: http://139.162.12.61:5003" -ForegroundColor Cyan
    Write-Host "📱 iPad: http://139.162.12.61:5003" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "📝 Useful Commands:" -ForegroundColor Yellow
    Write-Host "  View logs:    ssh root@139.162.12.61 'sudo journalctl -u enose-logger -f'" -ForegroundColor White
    Write-Host "  Restart:      ssh root@139.162.12.61 'sudo systemctl restart enose-logger'" -ForegroundColor White
    Write-Host "  Status:       ssh root@139.162.12.61 'sudo systemctl status enose-logger'" -ForegroundColor White
    Write-Host ""
} else {
    Write-Host ""
    Write-Host "❌ Deployment Failed!" -ForegroundColor Red
    Write-Host "Check the error messages above" -ForegroundColor Yellow
}

# Cleanup
Remove-Item enose-logger.zip -ErrorAction SilentlyContinue
