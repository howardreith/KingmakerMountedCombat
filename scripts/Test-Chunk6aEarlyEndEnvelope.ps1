$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'));$lab=[IO.Path]::GetFullPath((Join-Path $repo '../..'));$kmcTestRoot=Join-Path $lab ('analysis-cache/early-end-envelope-tests/'+[Guid]::NewGuid().ToString('N'));[IO.Directory]::CreateDirectory($kmcTestRoot)|Out-Null
$factory=[IO.File]::ReadAllText((Join-Path $repo 'scripts/Test-Chunk6aOrderEnvelope.ps1'));$cut=$factory.IndexOf('$paths=[xml]');if($cut-lt0){throw 'Factory boundary absent'}
. ([scriptblock]::Create($factory.Substring(0,$cut).Replace('$PSScriptRoot',"'$repo/scripts'"))) -Configuration Release
$tokens=$null;$errors=$null;$ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'Test-NativeEarlyEndMount.ps1'),[ref]$tokens,[ref]$errors)
$f=@($ast.FindAll({param($n)$n-is[Management.Automation.Language.FunctionDefinitionAst]-and$n.Name-ceq'New-EarlyEndFixture'},$true));if($f.Count-ne1){throw 'Early factory missing'};. ([scriptblock]::Create($f[0].Extent.Text))
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
$checks=0
function Sync-EarlyEnvelope($a){
 $e=$a.observations.chunk6aMountOrder;$a.observations.chunk6aCommandProofs[-1]=Copy-Value $e.positiveProof
 $a.observations.actorAllocationTrace=Copy-Value $e.allocationTrace
 # Carry the same declared synthetic unused Swift through the exact setup baseline.
 foreach($phase in @('before','after')){$a.observations.chunk6aNativeActingSetup.$phase.rider.swift=$e.positiveProof.preClick.state.rider.swift}
 $copy=Copy-Value $e;$copy.PSObject.Properties.Remove('restoration')
 foreach($row in @($a.rows|Where-Object name -CIn @('CM03-rider-before-mount-slot','CM03-mount-slot-before-rider','CM03-next-round-activation','CM03-early-end-turn'))){$row.evidence=Copy-Value $copy}
}
foreach($first in @($true,$false)){
 $a=New-OrderEnvelope $first;$a.observations.chunk6aMountOrder=New-EarlyEndFixture $first
 function Add-SyntheticAvailability($x){
  if($null-eq$x-or$x-is[string]-or$x-is[ValueType]){return}
  if($x-is[System.Array]){foreach($v in $x){Add-SyntheticAvailability $v};return}
  if($null-ne$x.PSObject.Properties['actor']-and$null-ne$x.PSObject.Properties['swift']){Put-Value $x hasSwift ([double]$x.swift-le0)}
  foreach($prop in @($x.PSObject.Properties)){Add-SyntheticAvailability $prop.Value}
 }
 Add-SyntheticAvailability $a
 $a.rows+=@([pscustomobject]@{name='CM03-early-end-turn';status='PASS';evidence=$null});$a.productVersion='0.1.0-chunk6a-preview.131'
 Sync-EarlyEnvelope $a;$request=[pscustomobject]@{scenario=$a.scenario}
 Assert-KmcChunk6aCombatMountEvidence $request $a 'PASS';$checks++
 function Reject-EarlyEnvelope([scriptblock]$Change){
  $x=Copy-Value $a;& $Change $x;$errorText=$null;try{Assert-KmcChunk6aCombatMountEvidence $request $x 'PASS'}catch{$errorText=$_.Exception.Message}
  if($null-eq$errorText){throw ('Early End envelope accepted '+$Change)};$script:checks++
 }
 Reject-EarlyEnvelope {param($x)$x.rows=@($x.rows|Where-Object name -CNE 'CM03-early-end-turn')}
 Reject-EarlyEnvelope {param($x)$x.rows+=@($x.rows|Where-Object name -CEQ 'CM03-early-end-turn')}
 Reject-EarlyEnvelope {param($x)($x.rows|Where-Object name -CEQ 'CM03-early-end-turn').status='FAIL'}
 Reject-EarlyEnvelope {param($x)($x.rows|Where-Object name -CEQ 'CM03-early-end-turn').evidence.beforeEndInput.turnObject++}
 Reject-EarlyEnvelope {param($x)$x.observations.chunk6aMountOrder.beforeEndInput.riderResources.hasSwift=$false;Sync-EarlyEnvelope $x}
 Reject-EarlyEnvelope {param($x)$x.observations.chunk6aMountOrder.afterEndInput.riderResources.hasSwift=$true;Sync-EarlyEnvelope $x}
 Reject-EarlyEnvelope {param($x)$x.observations.chunk6aMountOrder.afterEndInput.status='Acting';Sync-EarlyEnvelope $x}
 Reject-EarlyEnvelope {param($x)$x.observations.chunk6aMountOrder.beforeEndInput.round++;Sync-EarlyEnvelope $x}
 Reject-EarlyEnvelope {param($x)$x.observations.chunk6aMountOrder.completion.calls[0].setCooldowns=$false;Sync-EarlyEnvelope $x}
 foreach($role in @('riderResources','mountResources')){foreach($field in @('standard','move','swift','reactions','reactionCooldown','initiativeCooldown','initiativeOrder')){Reject-EarlyEnvelope {param($x)$x.observations.chunk6aMountOrder.beforeEndInput.$role.$field++;Sync-EarlyEnvelope $x}}}
 # Historical versions retain their exact old contract; unknown/current versions must emit the new row.
 $old=Copy-Value $a;$old.productVersion='0.1.0-chunk6a-preview.130';$old.rows=@($old.rows|Where-Object name -CNE 'CM03-early-end-turn')
 Assert-KmcChunk6aCombatMountEvidence $request $old 'PASS';$checks++
 Reject-EarlyEnvelope {param($x)$x.productVersion='unknown';$x.rows=@($x.rows|Where-Object name -CNE 'CM03-early-end-turn')}
 Reject-EarlyEnvelope {param($x)$x.productVersion='0.1.0-chunk6a-preview.130';$x.observations.chunk6aMountOrder.beforeEndInput.riderResources.hasSwift=$false;Sync-EarlyEnvelope $x}
}
[pscustomobject]@{status='PASS';checks=$checks;atUtc=[DateTime]::UtcNow.ToString('o');scope='Complete synthetic order envelope only; no native qualification'}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $kmcTestRoot 'envelope-test-receipt.json') -Encoding UTF8
Write-Host ('EARLY END COMPLETE ENVELOPE PASS='+$checks+' FAIL=0; draft only')
