Add-Type -AssemblyName System.Drawing
$file = Get-ChildItem "C:\Users\teoba\Documents\Plan conception\_claude_after_sejour*.png" | Select-Object -First 1
$bmp = [System.Drawing.Bitmap]::FromFile($file.FullName)
Write-Host "File: $($file.Name)"

$minX = 9999; $maxX = 0; $minY = 9999; $maxY = 0; $count = 0
for ($y = 0; $y -lt $bmp.Height; $y += 2) {
    for ($x = 0; $x -lt $bmp.Width; $x += 2) {
        $c = $bmp.GetPixel($x, $y)
        if ($c.G -gt 180 -and $c.B -gt 180 -and $c.R -lt 50) {
            $count++
            if ($x -lt $minX) { $minX = $x }
            if ($x -gt $maxX) { $maxX = $x }
            if ($y -lt $minY) { $minY = $y }
            if ($y -gt $maxY) { $maxY = $y }
        }
    }
}
Write-Host "Cyan pixels count: $count"
Write-Host "Cyan bounding box: X=[$minX, $maxX] Y=[$minY, $maxY]"
$bmp.Dispose()
