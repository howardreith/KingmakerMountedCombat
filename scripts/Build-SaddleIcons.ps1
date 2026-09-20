[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$assetRoot=Join-Path (Split-Path $PSScriptRoot -Parent) 'src\KingmakerMountedCombat\Assets'
foreach($name in @('Mount','Dismount')) {
    $source=[Drawing.Image]::FromFile((Join-Path $assetRoot ($name+'SaddleIconMaster.png')))
    $bitmap=New-Object Drawing.Bitmap(96,96,([Drawing.Imaging.PixelFormat]::Format24bppRgb))
    $graphics=[Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.PixelOffsetMode=[Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.DrawImage($source,(New-Object Drawing.Rectangle(0,0,96,96)))
        $bitmap.Save((Join-Path $assetRoot ($name+'SaddleIcon.png')),[Drawing.Imaging.ImageFormat]::Png)
    } finally {$graphics.Dispose();$bitmap.Dispose();$source.Dispose()}
}
