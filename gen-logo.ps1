# gen-logo.ps1  (WPF renderer - GDI+ FillPath is buggy for this path)
# Renders the official DeepSeek whale logo into black PNGs + multi-size DeepSeek.ico,
# and emits LogoImage.cs (base64 384px PNG) for embedding in the app.
# Verified against Edge headless + pure-math shoelace area (882.8 in 50-space).
# Run with: powershell.exe -Sta -File gen-logo.ps1
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationCore

$src = 'D:\DSH_start\ds-logo-d.txt'
$outDir = 'D:\DSH_start\icons'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

# ---------- 1. Parse SVG path data ----------
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
        $pv = @()
        for ($k = 0; $k -lt $need; $k++) { $pv += [double]$nums[$k] }
        [void]$segments.Add("$cur " + ($pv -join ' '))
        $nums.RemoveRange(0, $need)
    }
}
"segments: $($segments.Count)"

# ---------- 2. WPF PathGeometry ----------
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
$geo = New-Object System.Windows.Media.PathGeometry
$geo.Figures = $figs
$geo.FillRule = [System.Windows.Media.FillRule]::Nonzero

# ---------- 3. Render sizes ----------
$sizes = 16,20,24,32,40,48,64,128,256,384
foreach ($size in $sizes) {
    $pad = [double]($size * 0.08)
    $scale = [double](($size - 2.0 * $pad) / 50.0)
    $geo.Transform = New-Object System.Windows.Media.MatrixTransform($scale, 0, 0, $scale, $pad, $pad)

    $dv = New-Object System.Windows.Media.DrawingVisual
    $dc = $dv.RenderOpen()
    $dc.DrawGeometry([System.Windows.Media.Brushes]::Black, $null, $geo)
    $dc.Close()

    $rtb = New-Object System.Windows.Media.Imaging.RenderTargetBitmap($size, $size, 96, 96, [System.Windows.Media.PixelFormats]::Pbgra32)
    $rtb.Render($dv)

    $enc = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
    $enc.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($rtb))
    $fs = [System.IO.File]::Create("$outDir\$size.png")
    $enc.Save($fs)
    $fs.Close()

    if ($size -eq 256) {
        $stride = 256 * 4
        $buf = New-Object byte[] (256 * $stride)
        $rtb.CopyPixels($buf, $stride, 0)
        $black = 0
        for ($y = 0; $y -lt 256; $y++) {
            for ($x = 0; $x -lt 256; $x++) {
                $i = $y * $stride + $x * 4
                if ($buf[$i+3] -gt 128 -and ($buf[$i] + $buf[$i+1] + $buf[$i+2]) -lt 120) { $black++ }
            }
        }
        "256px black pixels: $black  (expect ~16300; GDI+ buggy render gave 13221)"
    }
}
"rendered $($sizes.Count) pngs"

# ---------- 4. Pack multi-size ICO ----------
$icoSizes = 16,20,24,32,40,48,64,128,256
$ms = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter($ms)
$bw.Write([UInt16]0); $bw.Write([UInt16]1); $bw.Write([UInt16]$icoSizes.Count)
$offset = 6 + 16 * $icoSizes.Count
$blobs = @()
foreach ($size in $icoSizes) { $blobs += ,[System.IO.File]::ReadAllBytes("$outDir\$size.png") }
for ($k = 0; $k -lt $icoSizes.Count; $k++) {
    $size = $icoSizes[$k]
    $bytes = $blobs[$k]
    if ($size -ge 256) { $bw.Write([byte]0) } else { $bw.Write([byte]$size) }
    if ($size -ge 256) { $bw.Write([byte]0) } else { $bw.Write([byte]$size) }
    $bw.Write([byte]0); $bw.Write([byte]0)
    $bw.Write([UInt16]1); $bw.Write([UInt16]32)
    $bw.Write([UInt32]$bytes.Length)
    $bw.Write([UInt32]$offset)
    $offset += $bytes.Length
}
foreach ($bytes in $blobs) { $bw.Write($bytes) }
$bw.Flush()
[System.IO.File]::WriteAllBytes('D:\DSH_start\DeepSeek.ico', $ms.ToArray())
$bw.Dispose(); $ms.Dispose()
"ico written: $((Get-Item 'D:\DSH_start\DeepSeek.ico').Length) bytes"

# ---------- 5. Emit C# base64 for app embedding ----------
$png384 = [Convert]::ToBase64String([System.IO.File]::ReadAllBytes("$outDir\384.png"))
$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('// Auto-generated by gen-logo.ps1 (WPF render of the official DeepSeek whale logo, black)')
[void]$sb.AppendLine('public static class LogoImage')
[void]$sb.AppendLine('{')
[void]$sb.AppendLine('    public const string Base64 = "' + $png384 + '";')
[void]$sb.AppendLine('}')
[System.IO.File]::WriteAllText('D:\DSH_start\LogoImage.cs', $sb.ToString(), (New-Object System.Text.ASCIIEncoding))
"LogoImage.cs written: $((Get-Item 'D:\DSH_start\LogoImage.cs').Length) bytes"
"DONE"
