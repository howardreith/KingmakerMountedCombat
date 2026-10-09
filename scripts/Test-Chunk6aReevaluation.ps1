param(
 [string]$LabRoot='C:/Dev/KingmakerMountedCombatLab',
 [string]$LedgerLeaf='analysis-cache/chunk6a-causal/ledger-preview151.json')
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
# The re-evaluation protocol on immutable preview.151 artifacts (lab-bound). A native PASS whose
# run-time overall facet was an external-reader refusal binds with a re-evaluation record and is
# accepted only when the complete scenario validator accepts the immutable bytes under the current
# harness; a native failure, a refusal outside the dedicated readers, a foreign reader digest or a
# tampered refusal record is refused. Nothing is qualified here; the ledger scripts do that.
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'runtime/Chunk6aSupportingEvidence.ps1')
. (Join-Path $PSScriptRoot 'runtime/Chunk6aHarnessIdentity.ps1')
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$ledgerPath=Join-Path $LabRoot $LedgerLeaf
if(-not(Test-Path -LiteralPath $ledgerPath -PathType Leaf)){throw ('Frozen preview.151 ledger is absent: '+$ledgerPath)}
$ledger=Get-Content -Raw -LiteralPath $ledgerPath|ConvertFrom-Json
if([string]$ledger.payload.version-cne'0.1.0-chunk6a-preview.151'){throw 'Ledger is not the frozen preview.151 ledger'}
$payload=$ledger.payload
$checks=0
function Copy-Value($x){$x|ConvertTo-Json -Depth 100|ConvertFrom-Json}
function Reject([scriptblock]$Action,[string]$Expected){
 $rejected=$false
 try{& $Action}catch{if($_.Exception.Message.IndexOf($Expected,[StringComparison]::Ordinal)-lt0){throw ('Unexpected refusal: '+$_.Exception.Message)};$rejected=$true}
 if(-not$rejected){throw ('Accepted: '+$Expected)};$script:checks++
}
foreach($prefix in @('Action economy: x','Child entry preamble: x','Additional Chunk6A binding differs: x','Chunk 6A foundation: x','TB positioning candidate violates unchanged route and clearance bounds.','Prediction escaped temporary admission or had a native side effect.','Prediction lacks its exact temporary admission.','Native speculation added a resource or movement callback.','Missing bounded native prediction evidence.')){if(-not(Test-KmcExternalReaderRefusal $prefix)){throw 'Reader refusal prefix not recognised'};$checks++}
foreach($other in @('Phase 3D Horse tranche leaf exceeded 30 seconds at Phase3gControls.','Game reported FAIL: x','Action economy:x','','Prediction escaped temporary admission or had a native side effect. extra','CM03-rider-other-action: The exact adjacent combat Mount command proof failed: ["Prediction observation: Prediction escaped temporary admission or had a native side effect."]','Prediction has another .','phase3d-horse-runtime-exception: InvalidOperationException: Prediction escaped temporary admission or had a native side effect.')){if(Test-KmcExternalReaderRefusal $other){throw ('Non-reader message accepted as a reader refusal: '+$other)};$checks++}
# Re-evaluated native PASS transactions (overall facet refused by the preview.151 freeze reader).
$cases=@(
 [pscustomobject]@{run='c6a-stabilization151-a-mount-spent-move';id='CM03-mount-spent-move';refusal='Action economy: mount slot end: '},
 [pscustomobject]@{run='c6a-stabilization151-a-rider-without-move';id='CM03-rider-without-move';refusal='Action economy: native click created a command, shell or dispatch'}
)
foreach($case in $cases){
 $root=Join-Path $LabRoot ('runtime-evidence/'+$case.run)
 $result=Get-Content -Raw -LiteralPath (Join-Path $root 'runtime-result.json')|ConvertFrom-Json
 $game=Get-Content -Raw -LiteralPath (Join-Path $root 'runtime-game-result.json')|ConvertFrom-Json
 if([string]$result.status-cne'FAIL'-or[string]$game.status-cne'PASS'){throw ('Case is not a native PASS with an overall refusal: '+$case.run)}
 $binding=Get-KmcSupportingBinding $case.id $case.run @($case.id) $LabRoot
 $re=$binding.reevaluation
 if($null-eq$re-or[string]$re.contract-cne'immutable-artifact-reevaluated-under-new-harness-identity'-or@($re.originalOverall.errors).Count-lt1-or([string]$re.originalOverall.errors[0]).IndexOf($case.refusal,[StringComparison]::Ordinal)-ne0){throw ('Binding lacks the exact re-evaluation record: '+$case.run)}
 if([int]$binding.passCount-ne[int]$game.assertionPassCount-or[int]$binding.failCount-ne0){throw 'Re-evaluated binding counts are not the native facet'}
 Assert-KmcSupportingRun $payload $binding $LabRoot;$checks++
 $plain=Copy-Value $binding;$plain.PSObject.Properties.Remove('reevaluation')
 Reject {Assert-KmcSupportingRun $payload $plain $LabRoot} 'Supporting native or overall run is not an exact PASS.'
 $foreign=Copy-Value $binding;$foreign.reevaluation.readerDigest='0'*64
 Reject {Assert-KmcSupportingRun $payload $foreign $LabRoot} 'names another reader digest'
 $tampered=Copy-Value $binding;$tampered.reevaluation.originalOverall.errors=@('Action economy: something else')
 Reject {Assert-KmcSupportingRun $payload $tampered $LabRoot} 'does not record its original overall refusal exactly'
 $counts=Copy-Value $binding;$counts.passCount=[int]$counts.passCount-1
 Reject {Assert-KmcSupportingRun $payload $counts $LabRoot} 'is not an exact native PASS'
}
# A native failure is never re-evaluated: its binding carries no record and is refused as before.
$nativeFail='c6a-stabilization151-a-rider-other-action'
$failBinding=Get-KmcSupportingBinding 'CM03-rider-other-action' $nativeFail @('CM03-rider-other-action') $LabRoot
if($null-ne$failBinding.PSObject.Properties['reevaluation']){throw 'A native failure received a re-evaluation record'};$checks++
Reject {Assert-KmcSupportingRun $payload $failBinding $LabRoot} 'Supporting native or overall run is not an exact PASS.'
# A refusal outside the dedicated readers is never re-evaluated.
$synthetic=[pscustomobject]@{status='FAIL';assertionPassCount=0;assertionFailCount=1;errors=@('Phase 3D Horse evidence identity mismatch: commit')}
$syntheticGame=[pscustomobject]@{status='PASS';assertionPassCount=5;assertionFailCount=0;errors=@()}
if($null-ne(Get-KmcChunk6aReevaluation $synthetic $syntheticGame $repo)){throw 'A harness identity failure was offered for re-evaluation'};$checks++
if($null-ne(Get-KmcChunk6aReevaluation ([pscustomobject]@{status='FAIL';assertionPassCount=0;assertionFailCount=1;errors=@('Action economy: x')}) ([pscustomobject]@{status='FAIL';assertionPassCount=5;assertionFailCount=1;errors=@()}) $repo)){throw 'A native failure was offered for re-evaluation'};$checks++
$offered=Get-KmcChunk6aReevaluation ([pscustomobject]@{status='FAIL';assertionPassCount=0;assertionFailCount=1;errors=@('Action economy: x')}) $syntheticGame $repo
if($null-eq$offered-or[string]$offered.readerDigest-cne(Get-KmcChunk6aHarnessIdentity $repo).readerDigest){throw 'A reader refusal was not offered under the current harness'};$checks++
Write-Output ('REEVALUATION PROTOCOL PASS='+$checks+' FAIL=0; immutable preview.151 artifacts under the current harness, nothing qualified')
