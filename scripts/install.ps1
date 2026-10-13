# Automatic installation script for Revit AI Bridge
[CmdletBinding()]
param(
    [switch]$SkipClaudeConfig
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

Write-Host '==========================================================' -ForegroundColor Cyan
Write-Host '       REVIT AI BRIDGE — AUTOMATIC INSTALLER              ' -ForegroundColor Cyan
Write-Host '==========================================================' -ForegroundColor Cyan

# 1. Build Addin and Server
Write-Host "`n[Step 1/4] Compiling components..." -ForegroundColor Yellow
& "$PSScriptRoot\build.ps1"

# 2. Deploy Addin to Revit Addins folder
Write-Host "`n[Step 2/4] Deploying Revit Add-in..." -ForegroundColor Yellow
$revitYears = @('2027', '2026')
$deployed = $false

foreach ($year in $revitYears) {
    $addinDir = Join-Path $env:APPDATA "Autodesk\Revit\Addins\$year"
    if ((Test-Path "C:\Program Files\Autodesk\Revit $year") -or (Test-Path $addinDir)) {
        if (-not (Test-Path $addinDir)) {
            New-Item -ItemType Directory -Path $addinDir -Force | Out-Null
        }
        $srcDll = Join-Path $root 'revit-addin\RevitMCP\bin\Release\net10.0-windows\RevitMCP.dll'
        $srcAddin = Join-Path $root 'revit-addin\RevitMCP\RevitMCP.addin'
        
        Copy-Item -Path $srcDll -Destination $addinDir -Force
        Copy-Item -Path $srcAddin -Destination $addinDir -Force
        Write-Host "  -> Successfully deployed addin for Revit $year in $addinDir" -ForegroundColor Green
        $deployed = $true
    }
}

if (-not $deployed) {
    $addinDir = Join-Path $env:APPDATA 'Autodesk\Revit\Addins\2027'
    New-Item -ItemType Directory -Path $addinDir -Force | Out-Null
    Copy-Item -Path "$root\revit-addin\RevitMCP\bin\Release\net10.0-windows\RevitMCP.dll" -Destination $addinDir -Force
    Copy-Item -Path "$root\revit-addin\RevitMCP\RevitMCP.addin" -Destination $addinDir -Force
    Write-Host "  -> Deployed to default directory: $addinDir" -ForegroundColor Green
}

# 3. Configure Claude Desktop MCP
if (-not $SkipClaudeConfig) {
    Write-Host "`n[Step 3/4] Configuring Claude Desktop MCP..." -ForegroundColor Yellow
    $claudeDir = Join-Path $env:APPDATA 'Claude'
    $claudeConfig = Join-Path $claudeDir 'claude_desktop_config.json'
    
    if (-not (Test-Path $claudeDir)) {
        New-Item -ItemType Directory -Path $claudeDir -Force | Out-Null
        Write-Host "  -> Created Claude config directory: $claudeDir" -ForegroundColor Cyan
    }

    $configObj = @{}
    if (Test-Path $claudeConfig) {
        $backupPath = "$claudeConfig.bak-$((Get-Date).ToString('yyyyMMddHHmmss'))"
        Copy-Item -Path $claudeConfig -Destination $backupPath -Force
        Write-Host "  -> Backed up existing Claude config to $backupPath" -ForegroundColor Cyan
        try {
            $rawJson = Get-Content $claudeConfig -Raw
            $configObj = $rawJson | ConvertFrom-Json -AsHashtable
        } catch {
            Write-Host '  [WARN] Existing config was not valid JSON, starting fresh' -ForegroundColor Yellow
            $configObj = @{}
        }
    }

    if (-not $configObj.ContainsKey('mcpServers')) {
        $configObj['mcpServers'] = @{}
    }

    $serverScript = (Join-Path $root 'mcp-server\dist\index.js').Replace('\', '/')
    
    $configObj['mcpServers']['revit'] = @{
        'command' = 'node'
        'args' = @($serverScript)
        'env' = @{
            'REVIT_HOST' = 'http://localhost:6543'
            'REVIT_TIMEOUT_MS' = '120000'
        }
    }

    $finalJson = $configObj | ConvertTo-Json -Depth 10
    Set-Content -Path $claudeConfig -Value $finalJson -Encoding UTF8
    Write-Host "  -> Claude Desktop config successfully updated at $claudeConfig" -ForegroundColor Green

    # Also sync to Windows Store / MSIX package directory if present
    $msixDir = 'C:\Users\teoba\AppData\Local\Packages\Claude_pzs8sxrjxfjjc\LocalCache\Roaming\Claude'
    if (Test-Path $msixDir) {
        $msixConfig = Join-Path $msixDir 'claude_desktop_config.json'
        Set-Content -Path $msixConfig -Value $finalJson -Encoding UTF8
        Write-Host "  -> Synced config to Windows Store Claude at $msixConfig" -ForegroundColor Green
    }
} else {
    Write-Host "`n[Step 3/4] Skipped Claude Desktop configuration per flag." -ForegroundColor Cyan
}

# 4. Diagnostic
Write-Host "`n[Step 4/4] Running diagnostic verification..." -ForegroundColor Yellow
& "$PSScriptRoot\diagnose.ps1"

Write-Host "`n==========================================================" -ForegroundColor Green
Write-Host '       INSTALLATION SUCCESSFULLY COMPLETED!               ' -ForegroundColor Green
Write-Host '==========================================================' -ForegroundColor Green
