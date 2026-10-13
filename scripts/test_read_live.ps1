# Diagnostic en lecture seule via les outils et routes réelles du pont Revit AI Bridge
$ErrorActionPreference = 'Continue'

$endpoints = @(
    @{ Name = '1. Connexion / Health Check'; Tool = 'revit_health_check'; Url = 'http://localhost:6543/healthz'; Method = 'GET'; Body = $null },
    @{ Name = '2. Info Document & Version'; Tool = 'revit_get_model_info'; Url = 'http://localhost:6543/api/model/info'; Method = 'POST'; Body = '{}' },
    @{ Name = '3. Niveaux du projet'; Tool = 'revit_get_levels'; Url = 'http://localhost:6543/api/levels/list'; Method = 'POST'; Body = '{}' },
    @{ Name = '4. Vues du projet'; Tool = 'revit_get_views'; Url = 'http://localhost:6543/api/views/list'; Method = 'POST'; Body = '{}' },
    @{ Name = '5. Feuilles du projet'; Tool = 'revit_get_sheets'; Url = 'http://localhost:6543/api/sheets/list'; Method = 'POST'; Body = '{}' },
    @{ Name = '6. Comptage des elements'; Tool = 'revit_count_elements'; Url = 'http://localhost:6543/api/elements/count'; Method = 'POST'; Body = '{}' },
    @{ Name = '7. Selection actuelle'; Tool = 'revit_get_current_selection'; Url = 'http://localhost:6543/api/selection/get'; Method = 'POST'; Body = '{}' }
)

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "    DIAGNOSTIC EN LECTURE SEULE DES OUTILS MCP REVIT      " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

foreach ($ep in $endpoints) {
    Write-Host "`n--- [$($ep.Name)] ---" -ForegroundColor Yellow
    Write-Host "Outil MCP : $($ep.Tool)" -ForegroundColor Cyan
    Write-Host "Route     : $($ep.Url) ($($ep.Method))"

    try {
        if ($ep.Method -eq 'GET') {
            $resp = Invoke-RestMethod -Uri $ep.Url -Method Get -TimeoutSec 3 -ErrorAction Stop
        } else {
            $resp = Invoke-RestMethod -Uri $ep.Url -Method Post -Body $ep.Body -ContentType 'application/json' -TimeoutSec 3 -ErrorAction Stop
        }
        Write-Host "STATUT : SUCCES REEL (Execute par l addin Revit)" -ForegroundColor Green
        Write-Host ($resp | ConvertTo-Json -Depth 5)
    } catch {
        Write-Host "STATUT : ECHEC DE CONNEXION REEL" -ForegroundColor Red
        Write-Host "Detail de l exception : $($_.Exception.Message)" -ForegroundColor Red
    }
}
