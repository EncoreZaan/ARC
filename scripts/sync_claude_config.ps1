# Check and sync Claude config between standard AppData and MSIX LocalCache
$stdDir = Join-Path $env:APPDATA 'Claude'
$stdConfig = Join-Path $stdDir 'claude_desktop_config.json'

$msixDir = 'C:\Users\teoba\AppData\Local\Packages\Claude_pzs8sxrjxfjjc\LocalCache\Roaming\Claude'
$msixConfig = Join-Path $msixDir 'claude_desktop_config.json'

Write-Host "1. Standard config ($stdConfig): $(Test-Path $stdConfig)"
if (Test-Path $msixDir) {
    Write-Host "2. MSIX directory exists: $msixDir"
    Write-Host "3. MSIX config currently exists: $(Test-Path $msixConfig)"
    
    if (Test-Path $stdConfig) {
        Copy-Item -Path $stdConfig -Destination $msixConfig -Force
        Write-Host "-> Synced claude_desktop_config.json to MSIX LocalCache: $msixConfig" -ForegroundColor Green
    }
}
