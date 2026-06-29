# Uninstaller for Android USB Assistant
# Cleans up files, shortcuts, registry keys, and processes.

$ErrorActionPreference = "Stop"

$AppName = "AndroidUsbAssistant"
$RegistryKeyName = "AndroidUsbAssistant"

# Get current script folder as the install directory
$InstallDir = $PSScriptRoot

# 1. Kill active processes
Write-Host "Stopping any running instances of $AppName..." -ForegroundColor Cyan
Get-Process -Name $AppName -ErrorAction SilentlyContinue | Stop-Process -Force

# 2. Remove Shortcuts
Write-Host "Removing shortcuts..." -ForegroundColor Cyan
$DesktopShortcut = Join-Path ([Environment]::GetFolderPath("Desktop")) "$AppName.lnk"
if (Test-Path $DesktopShortcut) {
    Remove-Item $DesktopShortcut -Force
}

$StartMenuFolder = Join-Path ([Environment]::GetFolderPath("StartMenu")) "Programs\$AppName"
if (Test-Path $StartMenuFolder) {
    Remove-Item $StartMenuFolder -Recurse -Force
}

# 3. Remove Startup Registry Key
Write-Host "Removing Windows Startup registry key..." -ForegroundColor Cyan
try {
    $RegPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
    if (Get-ItemProperty -Path $RegPath -Name $RegistryKeyName -ErrorAction SilentlyContinue) {
        Remove-ItemProperty -Path $RegPath -Name $RegistryKeyName -Force
    }
} catch {
    Write-Warning "Could not remove registry startup key: $_"
}

# 4. Trigger detached directory deletion and exit
Write-Host "Uninstallation complete. Cleaning up installation directory..." -ForegroundColor Green

# First delete the main application executable immediately
$ExePath = Join-Path $InstallDir "AndroidUsbAssistant.App.exe"
if (Test-Path $ExePath) {
    try {
        Remove-Item $ExePath -Force -ErrorAction SilentlyContinue
    } catch {}
}

# Delete remaining files and attempt folder removal in a background script to release file locks
$Command = "Start-Sleep -Seconds 2; Get-ChildItem -Path '$InstallDir' -Exclude 'Uninstall.ps1' -Recurse -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue; Remove-Item -Path '$InstallDir' -Recurse -Force -ErrorAction SilentlyContinue"
Start-Process powershell -ArgumentList @("-NoProfile", "-WindowStyle", "Hidden", "-Command", $Command)
