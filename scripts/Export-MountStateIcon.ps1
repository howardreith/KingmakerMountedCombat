# Original editable KMC vector composition for the F2 Mount / Dismount control (ART-02). Offline authoring only;
# it does not run during a build. It develops F1's MountControlPilot concept (assets-source/finalize-f1) and leaves
# that F1 generator and its pinned outputs unchanged.
[CmdletBinding()] param()
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$repo=Split-Path $PSScriptRoot -Parent
$source=Join-Path $repo 'assets-source\finalize-f2'
$exports=Join-Path $repo 'src\KingmakerMountedCombat\Assets'
New-Item -ItemType Directory -Force $source | Out-Null
function Poly($g,$brush,$coords) {
    $points=New-Object 'System.Collections.Generic.List[Drawing.PointF]'
    for($i=0;$i -lt $coords.Count;$i+=2) { $points.Add([Drawing.PointF]::new($coords[$i],$coords[$i+1])) }
    $g.FillPolygon($brush,$points.ToArray())
}
$master=[Drawing.Bitmap]::new(512,512,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[Drawing.Graphics]::FromImage($master)
$g.SmoothingMode='AntiAlias'; $g.ScaleTransform(8,8)
$red=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#B4483A'))
$gold=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#F3DDA3'))
$edge=[Drawing.Pen]::new([Drawing.ColorTranslator]::FromHtml('#3A2418'),2.2)
$edge.LineJoin='Round'
# Horse and rider, a heavier version of F1's pilot silhouette, set low so the arc reads above it.
$horse=@(17,35,23,33,37,33,40,27,43,17,48,15,48,21,53,26,52,30,47,30,44,37,41,41,42,53,38,53,36,42,29,42,26,53,22,53,23,41,20,39,16,45,12,44,15,37,14,34,10,37,9,34,14,31)
$rider=@(28,24,34,24,36,32,42,33,41,36,33,35,30,30,29,36,25,36)
foreach($shape in @($horse,$rider)) {
    $points=New-Object 'System.Collections.Generic.List[Drawing.PointF]'
    for($i=0;$i -lt $shape.Count;$i+=2) { $points.Add([Drawing.PointF]::new($shape[$i],$shape[$i+1])) }
    $g.DrawPolygon($edge,$points.ToArray())
}
$g.DrawEllipse($edge,27,15,7,7)
Poly $g $gold $horse
$g.FillEllipse($gold,27,15,7,7)
Poly $g $gold $rider
# One arc with a head at each end: on and off the same companion, the control's single identity.
$arc=[Drawing.Pen]::new($red.Color,4.2)
$g.DrawArc($arc,9,3,30,26,198,144)
Poly $g $red @(4,14,15,13,8,23)
Poly $g $red @(44,14,33,13,40,23)
$arc.Dispose()
$g.Dispose(); $red.Dispose(); $gold.Dispose(); $edge.Dispose()
$master.Save((Join-Path $source 'MountStateControl.512.png'),[Drawing.Imaging.ImageFormat]::Png)
$out=[Drawing.Bitmap]::new(64,64,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[Drawing.Graphics]::FromImage($out); $g.CompositingMode='SourceCopy'; $g.InterpolationMode='HighQualityBicubic'; $g.PixelOffsetMode='HighQuality'
$g.DrawImage($master,[Drawing.Rectangle]::new(0,0,64,64)); $g.Dispose()
$out.Save((Join-Path $exports 'MountStateControl.png'),[Drawing.Imaging.ImageFormat]::Png); $out.Dispose()
# Review sheet: 32/48/64 px on parchment and on the dark action bar, beside the old saddle and F1's two emblems.
$sheet=[Drawing.Bitmap]::new(640,240,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[Drawing.Graphics]::FromImage($sheet); $g.InterpolationMode='HighQualityBicubic'
$g.FillRectangle([Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#E2D3B0')),0,0,640,120)
$g.FillRectangle([Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#1E1B18')),0,120,640,120)
$font=[Drawing.Font]::new('Arial',9)
$icons=@(
    @{label='Mount / Dismount (F2)';path=(Join-Path $exports 'MountStateControl.png')},
    @{label='old Mount saddle';path=(Join-Path $exports 'MountSaddleIcon.png')},
    @{label='feat (F1)';path=(Join-Path $exports 'MountedCombatFeat.png')},
    @{label='reaction (F1)';path=(Join-Path $exports 'MountedCombatReaction.png')})
for($row=0;$row -lt 2;$row++) {
    $ink=if($row -eq 0){[Drawing.Brushes]::Black}else{[Drawing.Brushes]::White}
    for($c=0;$c -lt $icons.Count;$c++) {
        $img=[Drawing.Image]::FromFile($icons[$c].path)
        $x=10+$c*158; $y=$row*120
        $g.DrawString($icons[$c].label,$font,$ink,$x,$y+4)
        $g.DrawImage($img,$x,$y+30,32,32); $g.DrawImage($img,$x+38,$y+30,48,48); $g.DrawImage($img,$x+92,$y+30,64,64)
        $img.Dispose()
    }
}
$g.Dispose(); $font.Dispose()
$sheet.Save((Join-Path $source 'review-thumbnails.png'),[Drawing.Imaging.ImageFormat]::Png); $sheet.Dispose()
