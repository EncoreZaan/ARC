# Build script for Revit AI Bridge (.NET C# addin + TypeScript MCP server)
[CmdletBinding()]
param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

Write-Host '==========================================================' -ForegroundColor Cyan
Write-Host '       REVIT AI BRIDGE — BUILD PIPELINE                   ' -ForegroundColor Cyan
Write-Host '==========================================================' -ForegroundColor Cyan

# 1. Build C# Revit Addin (.NET 10 / Revit 2027)
Write-Host "`n[1/2] Building C# Revit Addin ($Configuration)..." -ForegroundColor Yellow
$csproj = Join-Path $root 'revit-addin\RevitMCP\RevitMCP.csproj'
if (-not (Test-Path $csproj)) {
    throw "Cannot find $csproj"
}

dotnet build $csproj -c $Configuration
if ($LASTEXITCODE -ne 0) {
    throw "C# Addin build failed with code $LASTEXITCODE"
}
Write-Host '  -> C# Addin built successfully.' -ForegroundColor Green

# Sign binary if RevitMCP certificate is available
$targetDll = Join-Path $root "revit-addin\RevitMCP\bin\$Configuration\net10.0-windows\RevitMCP.dll"
if (Test-Path $targetDll) {
    $cert = Get-ChildItem Cert:\CurrentUser\My | Where-Object { $_.Subject -match 'RevitMCP' } | Select-Object -First 1
    if ($cert) {
        Set-AuthenticodeSignature -FilePath $targetDll -Certificate $cert | Out-Null
        Write-Host "  -> C# Addin digitally signed ($($cert.Thumbprint.Substring(0, 8))...)" -ForegroundColor Green
    }
}

# 2. Build TypeScript MCP Server
Write-Host "`n[2/2] Building TypeScript MCP Server..." -ForegroundColor Yellow
$serverDir = Join-Path $root 'mcp-server'
Push-Location $serverDir
try {
    npm run build
    if ($LASTEXITCODE -ne 0) {
        throw "TypeScript build failed with code $LASTEXITCODE"
    }
} finally {
    Pop-Location
}
Write-Host '  -> MCP Server built successfully.' -ForegroundColor Green

Write-Host "`n==========================================================" -ForegroundColor Green
Write-Host '       BUILD COMPLETED SUCCESSFULLY                       ' -ForegroundColor Green
Write-Host '==========================================================' -ForegroundColor Green
