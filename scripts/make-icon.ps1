# Genera Resources\app.png e Resources\app.ico (icona originale: schermo con cavo).
param([string]$OutDir = (Join-Path $PSScriptRoot '..\src\CableCast\Resources'))
Add-Type -AssemblyName System.Drawing
New-Item -ItemType Directory -Force $OutDir | Out-Null

function New-IconBitmap([int]$s) {
    $bmp = New-Object System.Drawing.Bitmap $s, $s
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.Clear([System.Drawing.Color]::Transparent)
    $k = $s / 256.0
    # sfondo arrotondato con gradiente
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $r = 56 * $k; $d = $r * 2
    $path.AddArc(0, 0, $d, $d, 180, 90)
    $path.AddArc($s - $d - 1, 0, $d, $d, 270, 90)
    $path.AddArc($s - $d - 1, $s - $d - 1, $d, $d, 0, 90)
    $path.AddArc(0, $s - $d - 1, $d, $d, 90, 90)
    $path.CloseFigure()
    $rect = New-Object System.Drawing.RectangleF 0, 0, $s, $s
    $grad = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rect, ([System.Drawing.Color]::FromArgb(255, 0, 120, 212)), ([System.Drawing.Color]::FromArgb(255, 110, 60, 220)), 45
    $g.FillPath($grad, $path)
    # telefono
    $white = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::White)
    $pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::White), (14 * $k)
    $pen.LineJoin = 'Round'
    $g.DrawRectangle($pen, 84 * $k, 40 * $k, 88 * $k, 140 * $k)
    $g.FillEllipse($white, 120 * $k, 156 * $k, 16 * $k, 16 * $k)
    # cavo
    $cable = New-Object System.Drawing.Pen ([System.Drawing.Color]::White), (14 * $k)
    $cable.StartCap = 'Round'; $cable.EndCap = 'Round'
    $g.DrawBezier($cable, (128 * $k), (180 * $k), (128 * $k), (230 * $k), (190 * $k), (200 * $k), (205 * $k), (232 * $k))
    $g.Dispose()
    return $bmp
}

$sizes = 16, 24, 32, 48, 64, 128, 256
$pngs = @()
foreach ($s in $sizes) {
    $b = New-IconBitmap $s
    $ms = New-Object System.IO.MemoryStream
    $b.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngs += , $ms.ToArray()
    if ($s -eq 256) { $b.Save((Join-Path $OutDir 'app.png'), [System.Drawing.Imaging.ImageFormat]::Png) }
    $b.Dispose()
}

$icoPath = Join-Path $OutDir 'app.ico'
$fs = [System.IO.File]::Create($icoPath)
$bw = New-Object System.IO.BinaryWriter $fs
$bw.Write([uint16]0); $bw.Write([uint16]1); $bw.Write([uint16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $s = $sizes[$i]; $wh = if ($s -ge 256) { 0 } else { $s }
    $bw.Write([byte]$wh); $bw.Write([byte]$wh); $bw.Write([byte]0); $bw.Write([byte]0)
    $bw.Write([uint16]1); $bw.Write([uint16]32)
    $bw.Write([uint32]$pngs[$i].Length); $bw.Write([uint32]$offset)
    $offset += $pngs[$i].Length
}
foreach ($p in $pngs) { $bw.Write($p) }
$bw.Dispose(); $fs.Dispose()
Write-Host "Icone generate in $OutDir"
