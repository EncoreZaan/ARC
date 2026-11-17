# Lanceur Unifié ARC v4.0 + Autonomous Studio API (Port 7878)
$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$StudioExe = Join-Path $RepoRoot "arc-autonomous-studio\target\release\arc-studio.exe"

if (-not (Test-Path $StudioExe)) {
    Write-Host "Compilation d'arc-studio.exe en cours..." -ForegroundColor Yellow
    Push-Location (Join-Path $RepoRoot "arc-autonomous-studio")
    cargo build --release
    Pop-Location
}

Write-Host "Démarrage d'ARC Autonomous Studio sur http://127.0.0.1:7878/ ..." -ForegroundColor Cyan
Start-Process -FilePath $StudioExe -ArgumentList "serve", "7878" -WindowStyle Minimized
Start-Sleep -Milliseconds 600
Start-Process "http://127.0.0.1:7878/"
