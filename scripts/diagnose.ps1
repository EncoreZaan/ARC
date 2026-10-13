# Diagnostic script for Revit AI Bridge
[CmdletBinding()]
param()

$root = Split-Path -Parent $PSScriptRoot

Write-Host '==========================================================' -ForegroundColor Cyan
Write-Host '       REVIT AI BRIDGE — DIAGNOSTIC ET CONTROLE           ' -ForegroundColor Cyan
Write-Host '==========================================================' -ForegroundColor Cyan

# 1. Revit Installation
Write-Host "`n[1] Installations Revit :" -ForegroundColor Yellow
$revitYears = @('2027', '2026', '2025')
$foundRevit = $false
foreach ($yr in $revitYears) {
    $rPath = "C:\Program Files\Autodesk\Revit $yr"
    if (Test-Path $rPath) {
        Write-Host "  [OK] Revit $yr detecte dans $rPath" -ForegroundColor Green
        $apiDll = Join-Path $rPath 'RevitAPI.dll'
        if (Test-Path $apiDll) {
            $len = (Get-Item $apiDll).Length
            Write-Host "       RevitAPI.dll present ($len octets)" -ForegroundColor Green
        }
        $foundRevit = $true
    }
}
if (-not $foundRevit) {
    Write-Host '  [ATTENTION] Aucune installation Revit standard trouvee dans C:\Program Files\Autodesk\' -ForegroundColor Red
}

# 2. Addin Deployment
Write-Host "`n[2] Deploiement de l Add-in Revit :" -ForegroundColor Yellow
$addinDir = Join-Path $env:APPDATA 'Autodesk\Revit\Addins\2027'
if (Test-Path $addinDir) {
    Write-Host "  [OK] Dossier present : $addinDir" -ForegroundColor Green
    $addinManifest = Join-Path $addinDir 'RevitMCP.addin'
    $addinDll = Join-Path $addinDir 'RevitMCP.dll'
    if (Test-Path $addinManifest) {
        Write-Host '  [OK] RevitMCP.addin installe' -ForegroundColor Green
    } else {
        Write-Host '  [MANQUANT] RevitMCP.addin absent' -ForegroundColor Red
    }
    if (Test-Path $addinDll) {
        $dllLen = (Get-Item $addinDll).Length
        Write-Host "  [OK] RevitMCP.dll installe ($dllLen octets)" -ForegroundColor Green
    } else {
        Write-Host '  [MANQUANT] RevitMCP.dll absent' -ForegroundColor Red
    }
} else {
    Write-Host "  [ATTENTION] Dossier Addins 2027 introuvable : $addinDir" -ForegroundColor Red
}

# 3. .NET and Node Runtimes
Write-Host "`n[3] Runtimes et SDK de developpement :" -ForegroundColor Yellow
try {
    $sdks = dotnet --list-sdks
    Write-Host '  [OK] .NET SDKs installes :' -ForegroundColor Green
    $sdks | ForEach-Object { Write-Host "       $_" }
} catch {
    Write-Host '  [ERREUR] dotnet introuvable' -ForegroundColor Red
}

try {
    $nodeVer = node -v
    $npmVer = npm -v
    Write-Host "  [OK] Node.js : $nodeVer | NPM : $npmVer" -ForegroundColor Green
} catch {
    Write-Host '  [ERREUR] Node.js ou npm introuvable' -ForegroundColor Red
}

# 4. MCP Server Build
Write-Host "`n[4] Etat du serveur MCP TypeScript :" -ForegroundColor Yellow
$mcpDist = Join-Path $root 'mcp-server\dist\index.js'
if (Test-Path $mcpDist) {
    Write-Host "  [OK] Serveur MCP compile present : $mcpDist" -ForegroundColor Green
} else {
    Write-Host '  [ATTENTION] Serveur MCP non compile (lancer build.ps1)' -ForegroundColor Red
}

# 5. Claude Desktop & Claude Code Configuration
Write-Host "`n[5] Configuration MCP de Claude Desktop :" -ForegroundColor Yellow
$claudeDesktop = Join-Path $env:APPDATA 'Claude\claude_desktop_config.json'
if (Test-Path $claudeDesktop) {
    Write-Host "  [OK] Configuration Claude Desktop trouvee : $claudeDesktop" -ForegroundColor Green
    $content = Get-Content $claudeDesktop -Raw
    if ($content -match 'revit') {
        Write-Host '       Entree revit configuree avec succes.' -ForegroundColor Green
    } else {
        Write-Host '       Entree revit absente du fichier JSON.' -ForegroundColor Yellow
    }
} else {
    Write-Host "  [INFO] Configuration Claude Desktop non creee : $claudeDesktop" -ForegroundColor Cyan
}

# 6. Live Revit Bridge Health Check
Write-Host "`n[6] Test de connexion en direct au pont Revit (http://localhost:6543/healthz) :" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri 'http://localhost:6543/healthz' -Method Get -TimeoutSec 2 -ErrorAction Stop
    Write-Host '  [EN LIGNE] Le pont Revit AI Bridge repond activement dans Revit !' -ForegroundColor Green
    Write-Host "             Service: $($response.service) v$($response.version)" -ForegroundColor Green
    Write-Host "             Uptime: $($response.uptimeSeconds) secondes" -ForegroundColor Green
} catch {
    Write-Host '  [HORS LIGNE] Pont non joignable (normal si Revit n est pas encore lance).' -ForegroundColor Yellow
    Write-Host '               Des que Revit 2027 sera ouvert avec un projet, le pont repondra.' -ForegroundColor Cyan
}

Write-Host "`n==========================================================" -ForegroundColor Cyan
Write-Host '       DIAGNOSTIC TERMINE                                 ' -ForegroundColor Cyan
Write-Host '==========================================================' -ForegroundColor Cyan
