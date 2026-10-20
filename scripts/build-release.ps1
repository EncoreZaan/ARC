<#
.SYNOPSIS
    Script de génération complète de la Release de Revit AI Bridge.
.DESCRIPTION
    1. Compile l'add-in Revit C# .NET 10 en mode Release.
    2. Compile et package le serveur MCP TypeScript en un bundle autonome (dist/index.bundle.mjs).
    3. Compile l'installateur natif universel en Rust (revit-mcp-installer.exe).
    4. Génère l'exécutable final prêt à distribuer dans dist/RevitMCP-Setup-v2.1.1.exe.
#>

param (
    [string]$Version = "2.1.1"
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "  GÉNÉRATION DE LA RELEASE REVIT AI BRIDGE v$Version (RUST)  " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Compilation C# .NET
Write-Host "`n[1/4] Compilation de l'add-in Revit C# (Release)..." -ForegroundColor Yellow
$csproj = Join-Path $RepoRoot "revit-addin\RevitMCP\RevitMCP.csproj"
dotnet build $csproj -c Release -p:DeployAddin=false --no-incremental
if ($LASTEXITCODE -ne 0) { throw "Erreur lors du build C#" }

# 2. Build et bundle du serveur MCP TypeScript
Write-Host "`n[2/5] Compilation et bundling du serveur MCP TypeScript..." -ForegroundColor Yellow
$mcpDir = Join-Path $RepoRoot "mcp-server"
Push-Location $mcpDir
try {
    npm run build
    if ($LASTEXITCODE -ne 0) { throw "Erreur npm run build" }
    npm run bundle
    if ($LASTEXITCODE -ne 0) { throw "Erreur npm run bundle" }
} finally {
    Pop-Location
}

# 3. Extraction du manifeste et compilation du serveur MCP Rust natif
Write-Host "`n[3/5] Compilation du serveur MCP natif en Rust (revit-mcp-server)..." -ForegroundColor Yellow
node (Join-Path $RepoRoot "scripts\extract-tools-manifest.js")
if ($LASTEXITCODE -ne 0) { throw "Erreur extract-tools-manifest.js" }

$serverCargo = Join-Path $RepoRoot "revit-mcp-server\Cargo.toml"
cargo build --release --manifest-path $serverCargo
if ($LASTEXITCODE -ne 0) { throw "Erreur lors du build revit-mcp-server" }

# 4. Compilation de l'installateur en Rust
Write-Host "`n[4/5] Compilation de l'installateur natif Rust (Cargo Release)..." -ForegroundColor Yellow
$cargoToml = Join-Path $RepoRoot "installer\Cargo.toml"
cargo build --release --manifest-path $cargoToml
if ($LASTEXITCODE -ne 0) { throw "Erreur lors du build Cargo installer" }

# 5. Packaging dans le dossier dist
Write-Host "`n[5/5] Finalisation du package de distribution..." -ForegroundColor Yellow
$distDir = Join-Path $RepoRoot "dist"
if (-not (Test-Path $distDir)) {
    New-Item -ItemType Directory -Path $distDir | Out-Null
}

$rustExe = Join-Path $RepoRoot "installer\target\release\revit-mcp-installer.exe"
$outExe = Join-Path $distDir "RevitMCP-Setup-v$Version.exe"
Copy-Item -Path $rustExe -Destination $outExe -Force

$sizeMb = [math]::Round(((Get-Item $outExe).Length / 1MB), 2)

Write-Host "`n==========================================================" -ForegroundColor Green
Write-Host "  SUCCÈS : Installateur autonome généré !" -ForegroundColor Green
Write-Host "  Fichier : $outExe" -ForegroundColor White
Write-Host "  Taille  : $sizeMb Mo (Add-in .NET + Compilateurs Roslyn + Serveur MCP inclus)" -ForegroundColor White
Write-Host "==========================================================" -ForegroundColor Green
