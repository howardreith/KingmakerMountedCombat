# Original editable KMC vector compositions. Offline authoring only.
[CmdletBinding()] param()
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$repo=Split-Path $PSScriptRoot -Parent
$source=Join-Path $repo 'assets-source\finalize-f1'
$exports=Join-Path $repo 'src\KingmakerMountedCombat\Assets'
New-Item -ItemType Directory -Force $source | Out-Null
function Poly($g,$brush,$coords) {
    $points=New-Object 'System.Collections.Generic.List[Drawing.PointF]'
    for($i=0;$i -lt $coords.Count;$i+=2) { $points.Add([Drawing.PointF]::new($coords[$i],$coords[$i+1])) }
    $g.FillPolygon($brush,$points.ToArray())
}
function Horse($g,$ink) {
    Poly $g $ink @(17,31,23,29,37,29,40,23,43,13,48,11,48,17,53,22,52,26,47,26,44,33,41,37,42,49,38,49,36,38,29,38,26,49,22,49,23,37,20,35,16,41,12,40,15,33,14,30,10,33,9,30,14,27)
    $g.FillEllipse($ink,27,11,7,7)
    Poly $g $ink @(28,20,34,20,36,28,42,29,41,32,33,31,30,26,29,32,25,32)
}
foreach($concept in @('MountedCombatFeat','MountedCombatReaction','MountControlPilot')) {
    $master=[Drawing.Bitmap]::new(512,512,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g=[Drawing.Graphics]::FromImage($master)
    $g.SmoothingMode='AntiAlias'; $g.ScaleTransform(8,8)
    $red=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#9C493D'))
    $gold=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#F3DDA3'))
    $dark=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#55382D'))
    $pen=[Drawing.Pen]::new($red.Color,3)
    if($concept -eq 'MountedCombatFeat') {
        $g.DrawEllipse($pen,5,5,54,54)
        Horse $g $red
        Poly $g $gold @(34,20,42,22,41,29,37,32,33,27)
        Poly $g $red @(35,22,40,23,39,27,37,29,35,26)
    } elseif($concept -eq 'MountedCombatReaction') {
        Poly $g $gold @(10,52,12,35,20,22,19,11,26,17,34,14,39,19,44,26,40,31,32,29,29,36,28,52)
        $g.FillEllipse($dark,32,21,3,3)
        Poly $g $red @(32,34,44,29,57,34,55,48,45,57,35,49)
        $outline=[Drawing.Pen]::new($gold.Color,2)
        $points=[Drawing.PointF[]]@([Drawing.PointF]::new(32,34),[Drawing.PointF]::new(44,29),[Drawing.PointF]::new(57,34),[Drawing.PointF]::new(55,48),[Drawing.PointF]::new(45,57),[Drawing.PointF]::new(35,49))
        $g.DrawPolygon($outline,$points); $outline.Dispose()
        $stroke=[Drawing.Pen]::new($gold.Color,3)
        $g.DrawLine($stroke,41,44,51,38); $stroke.Dispose()
    } else {
        Horse $g $gold
        $arrow=[Drawing.Pen]::new($red.Color,4)
        $g.DrawArc($arrow,6,5,24,24,180,170)
        Poly $g $red @(26,13,33,24,20,23)
        $arrow.Dispose()
    }
    $g.Dispose(); $red.Dispose(); $gold.Dispose(); $dark.Dispose(); $pen.Dispose()
    $master.Save((Join-Path $source ($concept+'.512.png')),[Drawing.Imaging.ImageFormat]::Png)
    if($concept -ne 'MountControlPilot') {
        $out=[Drawing.Bitmap]::new(64,64,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $g=[Drawing.Graphics]::FromImage($out); $g.CompositingMode='SourceCopy'; $g.InterpolationMode='HighQualityBicubic'; $g.PixelOffsetMode='HighQuality'
        $g.DrawImage($master,[Drawing.Rectangle]::new(0,0,64,64)); $g.Dispose()
        $out.Save((Join-Path $exports ($concept+'.png')),[Drawing.Imaging.ImageFormat]::Png); $out.Dispose()
    }
    $master.Dispose()
}
