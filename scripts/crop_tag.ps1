Add-Type -AssemblyName System.Drawing
$file = Get-ChildItem "C:\Users\teoba\Documents\Plan conception\_claude_plan_rooms_fixed*.png" | Select-Object -First 1
$bmp = [System.Drawing.Bitmap]::FromFile($file.FullName)

# Crop around Sejour text: x around 600..750, y around 850..950
$rect = [System.Drawing.Rectangle]::FromLTRB(550, 850, 750, 960)
$crop = $bmp.Clone($rect, $bmp.PixelFormat)
$crop.Save("C:\Users\teoba\Documents\Plan conception\_crop_sejour.png")
$crop.Dispose()
$bmp.Dispose()
Write-Host "Saved _crop_sejour.png"
