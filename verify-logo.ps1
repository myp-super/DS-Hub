# verify-logo.ps1
# Cross-check the GDI+ render (icons/256.png) against an independent WPF render
# of the same SVG path data. Also reports geometry sanity numbers.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName PresentationCore

$src = 'D:\DSH_start\ds-logo-d.txt'

# ---------- parse (same as gen-logo.ps1) ----------
$d = Get-Content -Raw $src
$tokens = [regex]::Matches($d, '[A-Za-z]|[-+]?\d*\.?\d+(?:[eE][-+]?\d+)?') | ForEach-Object { $_.Value }
$segments = New-Object System.Collections.ArrayList
$cur = ''; $nums = New-Object System.Collections.ArrayList
foreach ($t in $tokens) {
    if ($t -match '^[A-Za-z]$') {
        if ($t -eq 'Z') { [void]$segments.Add('Z'); $cur = 'Z'; $nums.Clear(); continue }
        $cur = $t; $nums.Clear(); continue
    }
    $nums.Add([double]$t) | Out-Null
    $need = 0
    if ($cur -eq 'M') { $need = 2 } elseif ($cur -eq 'C') { $need = 6 }
    while ($need -gt 0 -and $nums.Count -ge $need) {
        $pts = @()
        for ($k = 0; $k -lt $need; $k++) { $pts += [double]$nums[$k] }
        [void]$segments.Add("$cur " + ($pts -join ' '))
        $nums.RemoveRange(0, $need)
    }
}

# ---------- GDI+ numbers ----------
$path = New-Object System.Drawing.Drawing2D.GraphicsPath
$path.FillMode = [System.Drawing.Drawing2D.FillMode]::Winding
$cx = 0.0; $cy = 0.0
foreach ($s in $segments) {
    $p = $s -split ' '; $cmd = $p[0]
    if ($cmd -eq 'Z') { $path.CloseFigure(); continue }
    if ($cmd -eq 'M') {
        $path.StartFigure()
        $pt = New-Object System.Drawing.PointF([double]$p[1], [double]$p[2])
        $path.AddLine($pt, $pt); $cx = [double]$p[1]; $cy = [double]$p[2]
    } else {
        $p0 = New-Object System.Drawing.PointF($cx, $cy)
        $p1 = New-Object System.Drawing.PointF([double]$p[1], [double]$p[2])
        $p2 = New-Object System.Drawing.PointF([double]$p[3], [double]$p[4])
        $p3 = New-Object System.Drawing.PointF([double]$p[5], [double]$p[6])
        $path.AddBezier($p0, $p1, $p2, $p3); $cx = [double]$p[5]; $cy = [double]$p[6]
    }
}
$bb = $path.GetBounds()
"GDI bbox (50-space): x=$([math]::Round($bb.X,1)) y=$([math]::Round($bb.Y,1)) w=$([math]::Round($bb.Width,1)) h=$([math]::Round($bb.Height,1))"

# pixel stats of existing GDI+ png
$bmp = New-Object System.Drawing.Bitmap('D:\DSH_start\icons\256.png')
$gdiBlack = 0
$gdiPix = New-Object 'bool[,]' 256,256
for ($y = 0; $y -lt 256; $y++) {
    for ($x = 0; $x -lt 256; $x++) {
        $c = $bmp.GetPixel($x, $y)
        if ($c.A -gt 128 -and ($c.R + $c.G + $c.B) -lt 120) { $gdiBlack++; $gdiPix[$x,$y] = $true }
    }
}
$bmp.Dispose()
$gdiCov = $gdiBlack / 65536.0
"GDI+ black pixels: $gdiBlack  coverage: $([math]::Round($gdiCov,4))"

# ---------- WPF render (independent implementation; needs -Sta) ----------
try {
    $figs = New-Object System.Windows.Media.PathFigureCollection
    $fig = $null
    foreach ($s in $segments) {
        $p = $s -split ' '; $cmd = $p[0]
        if ($cmd -eq 'M') {
            $fig = New-Object System.Windows.Media.PathFigure((New-Object System.Windows.Point([double]$p[1], [double]$p[2])), (New-Object System.Windows.Media.PathSegmentCollection([System.Windows.Media.PathSegment[]]@())), $false)
            $figs.Add($fig)
        } elseif ($cmd -eq 'C') {
            $pbs = New-Object System.Windows.Media.PolyBezierSegment
            $pbs.Points.Add((New-Object System.Windows.Point([double]$p[1], [double]$p[2]))) | Out-Null
            $pbs.Points.Add((New-Object System.Windows.Point([double]$p[3], [double]$p[4]))) | Out-Null
            $pbs.Points.Add((New-Object System.Windows.Point([double]$p[5], [double]$p[6]))) | Out-Null
            $fig.Segments.Add($pbs)
        } elseif ($cmd -eq 'Z') {
            $fig.IsClosed = $true
        }
    }

    foreach ($rule in @('Nonzero', 'EvenOdd')) {
        $geo = New-Object System.Windows.Media.PathGeometry
        $geo.Figures = $figs
        if ($rule -eq 'Nonzero') { $geo.FillRule = [System.Windows.Media.FillRule]::Nonzero } else { $geo.FillRule = [System.Windows.Media.FillRule]::EvenOdd }

        $pad = 256 * 0.08; $scale = (256 - 2 * $pad) / 50.0
        $geo.Transform = New-Object System.Windows.Media.MatrixTransform($scale, 0, 0, $scale, $pad, $pad)

        $dv = New-Object System.Windows.Media.DrawingVisual
        $dc = $dv.RenderOpen()
        $dc.DrawGeometry([System.Windows.Media.Brushes]::Black, $null, $geo)
        $dc.Close()

        $rtb = New-Object System.Windows.Media.Imaging.RenderTargetBitmap(256, 256, 96, 96, [System.Windows.Media.PixelFormats]::Pbgra32)
        $rtb.Render($dv)
        $stride = 256 * 4
        $buf = New-Object byte[] (256 * $stride)
        $rtb.CopyPixels($buf, $stride, 0)

        $black = 0
        $bits = New-Object 'bool[,]' 256,256
        for ($y = 0; $y -lt 256; $y++) {
            for ($x = 0; $x -lt 256; $x++) {
                $i = $y * $stride + $x * 4
                $b = $buf[$i]; $g = $buf[$i+1]; $r = $buf[$i+2]; $a = $buf[$i+3]
                if ($a -gt 128 -and ($r + $g + $b) -lt 120) { $black++; $bits[$x,$y] = $true }
            }
        }
        $cov = $black / 65536.0
        "WPF $rule black pixels: $black  coverage: $([math]::Round($cov,4))"

        if ($rule -eq 'Nonzero') {
            $diff = 0
            for ($y = 0; $y -lt 256; $y++) {
                for ($x = 0; $x -lt 256; $x++) {
                    if ($bits[$x,$y] -ne $gdiPix[$x,$y]) { $diff++ }
                }
            }
            "pixel diff vs GDI+: $diff ($([math]::Round($diff/65536.0,4)))"
        }
    }
} catch {
    'WPF ERROR: ' + $_.Exception.Message
}
