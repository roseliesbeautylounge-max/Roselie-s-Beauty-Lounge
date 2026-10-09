$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$workspace = Split-Path -Parent $PSScriptRoot
$assetFolder = Join-Path $workspace 'src\Roselie.App\Assets'
$sourceLogo = [System.Drawing.Image]::FromFile((Join-Path $assetFolder 'logo.png'))
$sizes = @(16, 24, 32, 48, 64, 128, 256)
$frames = [System.Collections.Generic.List[byte[]]]::new()
try {
    foreach ($size in $sizes) {
        $bitmap = [System.Drawing.Bitmap]::new($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        $stream = [System.IO.MemoryStream]::new()
        try {
            $graphics.Clear([System.Drawing.Color]::White)
            $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
            $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
            $scale = ($size * 0.92) / [Math]::Max($sourceLogo.Width, $sourceLogo.Height)
            $width = [single]($sourceLogo.Width * $scale)
            $height = [single]($sourceLogo.Height * $scale)
            $bounds = [System.Drawing.RectangleF]::new([single](($size - $width) / 2), [single](($size - $height) / 2), $width, $height)
            $graphics.DrawImage($sourceLogo, $bounds)
            $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
            $frames.Add($stream.ToArray())
            if ($size -eq 256) { $bitmap.Save((Join-Path $workspace 'artifacts\desktop-icon-preview.png'), [System.Drawing.Imaging.ImageFormat]::Png) }
        } finally { $stream.Dispose(); $graphics.Dispose(); $bitmap.Dispose() }
    }
    $iconFile = [System.IO.File]::Create((Join-Path $assetFolder 'Roselie.Desktop.ico'))
    $writer = [System.IO.BinaryWriter]::new($iconFile)
    try {
        $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$sizes.Count)
        $offset = 6 + 16 * $sizes.Count
        for ($index = 0; $index -lt $sizes.Count; $index++) {
            $dimension = if ($sizes[$index] -eq 256) { 0 } else { $sizes[$index] }
            $writer.Write([byte]$dimension); $writer.Write([byte]$dimension)
            $writer.Write([byte]0); $writer.Write([byte]0)
            $writer.Write([uint16]1); $writer.Write([uint16]32)
            $writer.Write([uint32]$frames[$index].Length); $writer.Write([uint32]$offset)
            $offset += $frames[$index].Length
        }
        foreach ($frame in $frames) { $writer.Write($frame) }
    } finally { $writer.Dispose() }
} finally { $sourceLogo.Dispose() }
Write-Output 'Created a separate white-background desktop icon; application and receipt branding use the original asset.'
