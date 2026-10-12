[CmdletBinding()]
param([string]$RepositoryRoot,[string]$ReaderDirectory,[string]$FixturePath,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(!$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(!$ReaderDirectory){$ReaderDirectory=Join-Path $RepositoryRoot 'scripts/runtime'}
if(!$FixturePath){$FixturePath=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/BootstrapInventoryEvidenceFixture.json'}
if(!$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/bootstrap-object-label-'+[Guid]::NewGuid().ToString('N'))}
$labelReader=[IO.Path]::GetFullPath($ReaderDirectory);$labelFixture=[IO.Path]::GetFullPath($FixturePath);$labelOutput=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $labelOutput){throw 'Preserve prior identity-domain results.'};[void][IO.Directory]::CreateDirectory($labelOutput)
. (Join-Path $labelReader 'RuntimeHarness.Common.ps1')
function New-LabelFixture{[IO.File]::ReadAllText($labelFixture)|ConvertFrom-Json}
function Convert-Label([int]$Value,[string]$Mode){
 if($Value-eq0){return [int]0}
 switch -CaseSensitive ($Mode){
  'large-positive'{return [int](1000+$Value)}
  'negative'{return [int](-1000-$Value)}
  'minimum-int32'{return [int]([long][int]::MinValue+$Value-1)}
  'maximum-int32'{return [int]([long][int]::MaxValue-$Value+1)}
  'mixed-sign'{if($Value%2-eq0){return [int](-1000-$Value)};return [int](1000+$Value)}
  default{return $Value}
 }
}
function Convert-LabelFields($Row,[string[]]$Keys,[string]$Mode){foreach($key in $Keys){$Row.$key=Convert-Label $Row.$key $Mode}}
function Convert-LabelWorld($Row,[string]$Mode){Convert-LabelFields $Row @('game','saveManager','player','state','areaState','mainState') $Mode;foreach($actor in $Row.party){Convert-LabelFields $actor @('object','descriptor','view') $Mode}}
function Convert-LabelGraph($Fixture,[string]$Family,[string]$Mode){
 if($Family-ceq'bootstrap'){
  $o=$Fixture.source.observations
  foreach($row in @($o.before,$o.nativeWorld)){Convert-LabelFields $row @('game','player','saveManager','mainActor','mainView') $Mode}
  Convert-LabelFields $o.menuAdmission @('menu') $Mode
  foreach($actor in $o.nativeWorld.party){Convert-LabelFields $actor @('object','descriptor','view') $Mode}
  $o.probe.identities.ids=@($o.probe.identities.ids|ForEach-Object{Convert-Label $_ $Mode})
  foreach($event in $o.probe.events){Convert-LabelFields $event @('game','player','menuPlayer','saveManager','preset','save') $Mode}
 }else{
  $o=$Fixture.artifact.observations
  Convert-LabelFields $o.beforeLoad @('game','saveManager','menuPlayer') $Mode
  foreach($row in @($o.loadedWorld,$o.settledWorld)){Convert-LabelWorld $row $Mode}
  $o.nativeLoad.identities.ids=@($o.nativeLoad.identities.ids|ForEach-Object{Convert-Label $_ $Mode})
  foreach($event in $o.nativeLoad.events){Convert-LabelFields $event @('game','saveManager','player') $Mode}
 }
}
function Get-LabelObservation($Fixture,[string]$Family){if($Family-ceq'bootstrap'){return $Fixture.source.observations};return $Fixture.artifact.observations}
function Get-LabelRegistry($Observation,[string]$Family){if($Family-ceq'bootstrap'){return $Observation.probe.identities};return $Observation.nativeLoad.identities}
function Assert-LabelFixture($Fixture,[string]$Family){if($Family-ceq'bootstrap'){Assert-KmcChunk7NativeBootstrapEvidence $Fixture.sourceRequest $Fixture.source}else{Assert-KmcChunk7BootstrapInventoryEvidence $Fixture.request $Fixture.artifact $Fixture.source}}
$script:labelRows=New-Object 'System.Collections.Generic.List[object]';$script:labelPass=0;$script:labelFail=0
function Test-LabelCase([string]$Name,[bool]$Accept,[scriptblock]$Action){
 $errorText=$null;try{&$Action}catch{$errorText=$_.Exception.Message}
 $accepted=$null-eq$errorText;$passed=$accepted-eq$Accept
 if($passed){$script:labelPass++}else{$script:labelFail++}
 $script:labelRows.Add([ordered]@{name=$Name;expectedAcceptance=$Accept;accepted=$accepted;pass=$passed;error=$errorText})
 if(-not$passed){Write-Host ('FAIL '+$Name+': '+$errorText)}
}
foreach($family in @('bootstrap','cold-inventory')){
 foreach($mode in @('original','large-positive','negative','minimum-int32','maximum-int32','mixed-sign')){
  Test-LabelCase ($family+' graph '+$mode) $true {$f=New-LabelFixture;Convert-LabelGraph $f $family $mode;Assert-LabelFixture $f $family}
 }
 $badValues=[ordered]@{zero=0;text='1001';fraction=1001.5;boolean=$true;null=$null;array=@(1001);aboveInt32=[long]2147483648;belowInt32=[long]-2147483649}
 foreach($entry in $badValues.GetEnumerator()){
  foreach($location in @('world','registry','callback')){
   Test-LabelCase ($family+' rejects '+$location+' '+$entry.Key) $false {
    $f=New-LabelFixture;Convert-LabelGraph $f $family 'large-positive';$o=Get-LabelObservation $f $family;$r=Get-LabelRegistry $o $family
    if($location-ceq'registry'){$r.ids[0]=$entry.Value}
    elseif($family-ceq'bootstrap'){if($location-ceq'world'){$o.before.game=$entry.Value}else{$o.probe.events[0].game=$entry.Value}}
    else{if($location-ceq'world'){$o.beforeLoad.game=$entry.Value}else{$o.nativeLoad.events[0].game=$entry.Value}}
    Assert-LabelFixture $f $family
   }
  }
 }
 foreach($case in @('duplicate-label','unretained-label','count-mismatch','expanded-capacity','contracted-capacity','above-capacity','registry-fault','released-registry','wrong-callback-owner','wrong-world-owner','zero-actor-label')){
  Test-LabelCase ($family+' rejects '+$case) $false {
   $f=New-LabelFixture;Convert-LabelGraph $f $family 'negative';$o=Get-LabelObservation $f $family;$r=Get-LabelRegistry $o $family
   switch -CaseSensitive ($case){
    'duplicate-label'{$r.ids[1]=$r.ids[0]}
    'unretained-label'{$r.ids=@($r.ids|Where-Object{$_-ne$r.ids[0]});$r.retainedCount=$r.ids.Count}
    'count-mismatch'{$r.retainedCount--}
    'expanded-capacity'{$r.capacity=129}
    'contracted-capacity'{$r.capacity=127}
    'above-capacity'{$r.ids=@(1..129);$r.retainedCount=129}
    'registry-fault'{$r.faults=1}
    'released-registry'{$r.released=$true}
    'wrong-callback-owner'{if($family-ceq'bootstrap'){$o.probe.events[0].game=$o.before.player}else{$o.nativeLoad.events[0].game=$o.beforeLoad.saveManager}}
    'wrong-world-owner'{if($family-ceq'bootstrap'){$o.nativeWorld.game=$o.nativeWorld.player}else{$o.loadedWorld.game=$o.loadedWorld.player}}
    'zero-actor-label'{if($family-ceq'bootstrap'){$o.nativeWorld.party[0].object=0}else{$o.loadedWorld.party[0].object=0}}
   }
   Assert-LabelFixture $f $family
  }
 }
 Test-LabelCase ($family+' accepts exact registry capacity with registered extra observations') $true {
  $f=New-LabelFixture;Convert-LabelGraph $f $family 'large-positive';$o=Get-LabelObservation $f $family;$r=Get-LabelRegistry $o $family
  $count=128-$r.ids.Count;$r.ids=@($r.ids)+@(2001..(2000+$count));$r.retainedCount=128
  Assert-LabelFixture $f $family
 }
}
$inputs=@((Join-Path $labelReader 'Chunk7NativeBootstrapEvidence.ps1'),(Join-Path $labelReader 'Chunk7BootstrapInventoryEvidence.ps1'),(Join-Path $labelReader 'RuntimeHarness.Common.ps1'),$labelFixture)|ForEach-Object{[ordered]@{path=$_;sha256=(Get-FileHash -LiteralPath $_).Hash.ToLowerInvariant()}}
[ordered]@{schemaVersion=1;status=$(if($script:labelFail-eq0){'PASS'}else{'FAIL'});pass=$script:labelPass;fail=$script:labelFail;rows=@($script:labelRows.ToArray());inputs=@($inputs);nativeExecution=$false;archiveReads=0}|ConvertTo-Json -Depth 10|Set-Content -LiteralPath (Join-Path $labelOutput 'receipt.json') -Encoding UTF8
Write-Host ('BOOTSTRAP OBJECT IDENTITY PASS='+$script:labelPass+' FAIL='+$script:labelFail+'; JSON-only readers, no native or archive execution.')
if($script:labelFail-ne0){exit 1};exit 0
