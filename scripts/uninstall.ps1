# Clean uninstallation script for Revit AI Bridge
[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "       REVIT AI BRIDGE — CLEAN UNINSTALLER                " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Remove Addin files
Write-Host "`n[1/2] Removing Revit Add-in files..." -ForegroundColor Yellow
$revitYears = @("2027", "2026", "2025")
foreach ($year in $revitYears) {
    $addinDir = Join-Path $env:APPDATA "Autodesk\Revit\Addins\$year"
    if (Test-Path $addinDir) {
        $manifest = Join-Path $addinDir "RevitMCP.addin"
        $dll = Join-Path $addinDir "RevitMCP.dll"
        
        if (Test-Path $manifest) {
            Remove-Item -Path $manifest -Force
            Write-Host "  -> Removed $manifest" -ForegroundColor Green
        }
        if (Test-Path $dll) {
            Remove-Item -Path $dll -Force
            Write-Host "  -> Removed $dll" -ForegroundColor Green
        }
    }
}

# 2. Clean Claude Desktop MCP configuration
Write-Host "`n[2/2] Cleaning Claude Desktop MCP config..." -ForegroundColor Yellow
$claudeConfig = Join-Path $env:APPDATA "Claude\claude_desktop_config.json"
if (Test-Path $claudeConfig) {
    $backupPath = "$claudeConfig.uninstall-bak-$((Get-Date).ToString('yyyyMMddHHmmss'))"
    Copy-Item -Path $claudeConfig -Destination $backupPath -Force
    Write-Host "  -> Backed up config to $backupPath" -ForegroundColor Cyan

    try {
        $raw = Get-Content $claudeConfig -Raw
        $configObj = $raw | ConvertFrom-Json -AsHashtable
        if ($configObj.ContainsKey("mcpServers") -and $configObj["mcpServers"].ContainsKey("revit")) {
            $configObj["mcpServers"].Remove("revit")
            $cleanJson = $configObj | ConvertTo-Json -Depth 10
            Set-Content -Path $claudeConfig -Value $cleanJson -Encoding UTF8
            Write-Host "  -> Removed 'revit' entry from Claude Desktop config." -ForegroundColor Green
        }
    } catch {
        Write-Host "  [WARN] Could not update $claudeConfig automatically" -ForegroundColor Yellow
    }
}

Write-Host "`n==========================================================" -ForegroundColor Green
Write-Host "       UNINSTALLATION COMPLETED CLEANLY                   " -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
