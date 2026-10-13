$pkg = "C:\Users\teoba\AppData\Local\Packages\Claude_pzs8sxrjxfjjc"
if (Test-Path $pkg) {
    Write-Host "Found package dir: $pkg"
    Get-ChildItem -Path $pkg -Recurse -Depth 4 -Filter "*config*.json" -ErrorAction SilentlyContinue | Select-Object FullName
    Get-ChildItem -Path "$pkg\LocalCache\Roaming" -Recurse -Depth 3 -ErrorAction SilentlyContinue | Select-Object FullName
}
