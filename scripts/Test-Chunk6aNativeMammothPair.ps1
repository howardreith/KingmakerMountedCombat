param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$repoRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$labRoot=[IO.Path]::GetFullPath((Join-Path $repoRoot '../..'))
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/Chunk6aNativeMammothEvidence.ps1')
$paths=[xml](Get-Content -Raw (Join-Path $repoRoot 'LocalGamePaths.props'))
$managed=Join-Path $paths.Project.PropertyGroup.KingmakerInstallDir 'Kingmaker_Data/Managed'
[Reflection.Assembly]::LoadFrom((Join-Path $managed 'Newtonsoft.Json.dll'))|Out-Null
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $repoRoot ('bin/'+$Configuration+'/KingmakerMountedCombat.dll')))
$method=$assembly.GetType('KingmakerMountedCombat.Diagnostics.Chunk6aMammothScenarioEngine',$true).GetMethod('SamePair',[Reflection.BindingFlags]'Static,NonPublic')
function Copy-Mammoth($v){$v|ConvertTo-Json -Depth 40|ConvertFrom-Json}
$base=[pscustomobject]@{riderId='rider';mountId='mount';riderObject=101;mountObject=102;mountBlueprint='e7aa96d15a45238438ae4cfb476f6bb9';petObject=102;petId='mount';masterObject=101;masterId='rider';riderInState=$true;mountInState=$true;riderIsPartyMember=$true;samePlayerPartyGroup=$true}
$script:checks=0
function Check-Mammoth($b,$a,[bool]$expected) {
    $producer=$false
    try{
        $jBefore=$null;if($null -ne $b){$jBefore=[Newtonsoft.Json.Linq.JObject]::Parse(($b|ConvertTo-Json -Depth 10 -Compress))}
        $jAfter=$null;if($null -ne $a){$jAfter=[Newtonsoft.Json.Linq.JObject]::Parse(($a|ConvertTo-Json -Depth 10 -Compress))}
        $arguments=[object[]]::new(2);$arguments[0]=$jBefore;$arguments[1]=$jAfter;$producer=[bool]$method.Invoke($null,$arguments)
    }catch{if($expected){throw}}
    $external=$true;try{Assert-KmcNativeMammothPair $b $a}catch{$external=$false;if($expected){throw}}
    if($producer -ne $expected -or $external -ne $expected){throw 'Producer/external native Mammoth identity result differs'}
    $script:checks+=2
}
Check-Mammoth $base (Copy-Mammoth $base) $true
Check-Mammoth $null $base $false
Check-Mammoth $base $null $false
foreach($field in @('riderObject','mountObject','petObject','masterObject')){
    $a=Copy-Mammoth $base;$a.$field=0;Check-Mammoth $base $a $false
    $a=Copy-Mammoth $base;$a.$field=$null;Check-Mammoth $a (Copy-Mammoth $a) $false
    $a=Copy-Mammoth $base;$a.$field++;Check-Mammoth $base $a $false
}
foreach($field in @('riderId','mountId','petId','masterId','mountBlueprint')){
    $a=Copy-Mammoth $base;$a.$field='foreign';Check-Mammoth $base $a $false
    $a=Copy-Mammoth $base;$a.$field=$null;Check-Mammoth $a (Copy-Mammoth $a) $false
}
foreach($field in @('riderInState','mountInState','riderIsPartyMember','samePlayerPartyGroup')){
    $a=Copy-Mammoth $base;$a.$field=$false;Check-Mammoth $a (Copy-Mammoth $a) $false
}
$a=Copy-Mammoth $base;$a.mountId=$a.riderId;$a.petId=$a.riderId;Check-Mammoth $a (Copy-Mammoth $a) $false
$a=Copy-Mammoth $base;$a.riderObject=201;$a.masterObject=201;Check-Mammoth $base $a $false
$a=Copy-Mammoth $base;$a.mountObject=202;$a.petObject=202;Check-Mammoth $base $a $false
Write-Output ('NATIVE MAMMOTH PAIR PASS='+$script:checks+' FAIL=0; shared producer/external synthetic checks, not native qualification')