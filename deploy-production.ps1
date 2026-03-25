#!/usr/bin/env pwsh
# Deploy EnoseLogger to Linode Production Server
# Usage: .\deploy-production.ps1

param(
    [string]$Server = "139.162.12.61",
    [string]$User = "root"
)

$ErrorActionPreference = "Stop"

Write-Host "🚀 E-Nose Logger - Production Deployment" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Build project
Write-Host "📦 Building self-contained Linux binary..." -ForegroundColor Yellow
dotnet publish -c Release -r linux-x64 --self-contained true -o publish-deploy

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Build successful" -ForegroundColor Green
Write-Host ""

# Step 2: Create production package
Write-Host "📦 Creating deployment package..." -ForegroundColor Yellow
Push-Location publish-deploy
tar -czf ../enose-deploy.tar.gz *
Pop-Location

Write-Host "✅ Package created" -ForegroundColor Green
Write-Host ""

# Step 3: Upload to server
Write-Host "📤 Uploading to $Server..." -ForegroundColor Yellow
scp enose-deploy.tar.gz "$User@${Server}:/tmp/"
scp deploy-server.sh "$User@${Server}:/tmp/"

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Upload failed!" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Upload complete" -ForegroundColor Green
Write-Host ""

# Step 4: Deploy on server
Write-Host "🔧 Installing on server..." -ForegroundColor Yellow
ssh "$User@$Server" "bash /tmp/deploy-server.sh"

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "🎉 Deployment Successful!" -ForegroundColor Green
    Write-Host "=====================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "🌐 Web UI: http://139.162.12.61:5003" -ForegroundColor Cyan
    Write-Host "📱 iPad:   http://139.162.12.61:5003" -ForegroundColor Cyan
    Write-Host ""
} else {
    Write-Host ""
    Write-Host "❌ Deployment Failed!" -ForegroundColor Red
}

# Cleanup
Write-Host "🧹 Cleaning up..." -ForegroundColor Yellow
Remove-Item enose-deploy.tar.gz -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force publish-deploy -ErrorAction SilentlyContinue
Write-Host "✅ Done!" -ForegroundColor Green
