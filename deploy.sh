#!/bin/bash

# E-Nose Logger - Linode Deployment Script
# Port: 5003
# URL: http://139.162.12.61:5003

set -e  # Exit on error

echo "🔬 E-Nose Logger - Deployment Script"
echo "======================================"

# Configuration
APP_NAME="enose-logger"
SERVICE_NAME="enose-logger.service"
PORT=5003
INSTALL_DIR="/var/www/enose-logger"
PUBLISH_DIR="$INSTALL_DIR/publish"
GIT_REPO="https://github.com/koson/ScadaSvg.git"  # Update with actual EnoseLogger repo
DOTNET_VERSION="9.0"

# Step 1: Update system
echo ""
echo "📦 Step 1/9: Updating system packages..."
sudo apt-get update

# Step 2: Install .NET 9 SDK (if not installed)
echo ""
echo "📦 Step 2/9: Checking .NET installation..."
if ! command -v dotnet &> /dev/null; then
    echo "Installing .NET $DOTNET_VERSION SDK..."
    wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
    chmod +x ./dotnet-install.sh
    ./dotnet-install.sh --channel $DOTNET_VERSION
    export PATH="$PATH:$HOME/.dotnet"
else
    echo ".NET already installed: $(dotnet --version)"
fi

# Step 3: Create directory structure
echo ""
echo "📁 Step 3/9: Creating directory structure..."
sudo mkdir -p $INSTALL_DIR
sudo mkdir -p $PUBLISH_DIR
sudo chown -R $USER:$USER $INSTALL_DIR

# Step 4: Clone/Pull code (if using Git)
echo ""
echo "📥 Step 4/9: Getting latest code..."
# If deploying from local files, skip this and copy files manually
# git clone $GIT_REPO $INSTALL_DIR/source || (cd $INSTALL_DIR/source && git pull)

# For now, assume code is already present or will be copied manually
echo "Assuming code is present in: $(pwd)"

# Step 5: Build project
echo ""
echo "🔨 Step 5/9: Building project..."
cd "$(dirname "$0")"  # Navigate to script directory
dotnet publish -c Release -o $PUBLISH_DIR

# Step 6: Set permissions
echo ""
echo "🔐 Step 6/9: Setting permissions..."
sudo chown -R www-data:www-data $PUBLISH_DIR
sudo chmod -R 755 $PUBLISH_DIR

# Ensure sessions directory exists and is writable
mkdir -p $PUBLISH_DIR/sessions
sudo chown -R www-data:www-data $PUBLISH_DIR/sessions
sudo chmod -R 775 $PUBLISH_DIR/sessions

# Step 7: Create systemd service
echo ""
echo "⚙️  Step 7/9: Creating systemd service..."
sudo tee /etc/systemd/system/$SERVICE_NAME > /dev/null <<EOF
[Unit]
Description=E-Nose Session Logger
After=network.target

[Service]
Type=simple
User=www-data
WorkingDirectory=$PUBLISH_DIR
ExecStart=/usr/bin/dotnet $PUBLISH_DIR/EnoseLogger.dll
Restart=always
RestartSec=10
TimeoutStartSec=300
TimeoutStopSec=30

Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://0.0.0.0:$PORT

[Install]
WantedBy=multi-user.target
EOF

# Step 8: Configure firewall
echo ""
echo "🔥 Step 8/9: Configuring firewall..."
if command -v ufw &> /dev/null; then
    sudo ufw allow $PORT/tcp
    sudo ufw status
else
    echo "UFW not installed, skipping firewall configuration"
fi

# Step 9: Start service
echo ""
echo "🚀 Step 9/9: Starting service..."
sudo systemctl daemon-reload
sudo systemctl stop $SERVICE_NAME 2>/dev/null || true
sudo systemctl start $SERVICE_NAME
sudo systemctl enable $SERVICE_NAME

# Wait for service to start
echo ""
echo "⏳ Waiting for service to start..."
sleep 10

# Check service status
echo ""
echo "✅ Deployment complete!"
echo "======================================"
echo ""
sudo systemctl status $SERVICE_NAME --no-pager
echo ""
echo "🌐 Application URL: http://$(hostname -I | awk '{print $1}'):$PORT"
echo "📊 View logs: sudo journalctl -u $SERVICE_NAME -f"
echo "🔄 Restart: sudo systemctl restart $SERVICE_NAME"
echo "⏹️  Stop: sudo systemctl stop $SERVICE_NAME"
echo ""
echo "🔬 E-Nose Logger is ready!"
