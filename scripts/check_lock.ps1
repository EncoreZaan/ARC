$dll = "C:\Users\teoba\AppData\Roaming\Autodesk\Revit\Addins\2027\RevitMCP.dll"
try {
    $stream = [System.IO.File]::Open($dll, [System.IO.FileMode]::Open, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)
    $stream.Close()
    Write-Host "DLL IS UNLOCKED" -ForegroundColor Green
} catch {
    Write-Host "DLL IS LOCKED: $($_.Exception.Message)" -ForegroundColor Yellow
}
