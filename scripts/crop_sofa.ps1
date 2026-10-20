Add-Type -AssemblyName System.Drawing
$file = Get-ChildItem "C:\Users\teoba\Documents\Plan conception\_claude_sofa_test*.png" | Select-Object -First 1
$bmp = [System.Drawing.Bitmap]::FromFile($file.FullName)

$rect = [System.Drawing.Rectangle]::FromLTRB(200, 300, 600, 800)
$crop = $bmp.Clone($rect, $bmp.PixelFormat)
$crop.Save("C:\Users\teoba\Documents\Plan conception\_crop_sofa.png")
$crop.Dispose()
$bmp.Dispose()
Write-Host "Saved _crop_sofa.png"
