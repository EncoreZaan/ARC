# Detailed Inspection of Model Info, Levels, Warnings and Families
$ErrorActionPreference = 'Stop'

Write-Host "=== 1. MODEL INFO ===" -ForegroundColor Cyan
$info = Invoke-RestMethod -Uri 'http://localhost:6543/api/model/info' -Method Post -Body '{}' -ContentType 'application/json'
$info | ConvertTo-Json -Depth 5

Write-Host "`n=== 2. LEVELS ===" -ForegroundColor Cyan
$levels = Invoke-RestMethod -Uri 'http://localhost:6543/api/levels/list' -Method Post -Body '{}' -ContentType 'application/json'
$levels | ConvertTo-Json -Depth 5

Write-Host "`n=== 3. WARNINGS ===" -ForegroundColor Cyan
$warnings = Invoke-RestMethod -Uri 'http://localhost:6543/api/model/warnings' -Method Post -Body '{}' -ContentType 'application/json'
Write-Host "Warnings Count: $($warnings.count)"
if ($warnings.count -gt 0) {
    $warnings.warnings | Select-Object -First 5 | Format-Table -AutoSize
}

Write-Host "`n=== 4. FAMILIES (First 10) ===" -ForegroundColor Cyan
$families = Invoke-RestMethod -Uri 'http://localhost:6543/api/families/list' -Method Post -Body '{}' -ContentType 'application/json'
$families.families | Select-Object -First 10 | Format-Table -AutoSize
