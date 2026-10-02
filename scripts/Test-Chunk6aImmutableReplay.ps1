param(
 [string]$LabRoot='C:/Dev/KingmakerMountedCombatLab',
 [string]$LedgerLeaf='analysis-cache/chunk6a-causal/ledger-preview150.json',
 [ValidateSet('Debug','Release')][string]$Configuration='Release')
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
# Immutable preview.150 action-economy artifacts - bound by the SHA-256 their retained-failure
# ledger entries record - re-evaluated under the current external reader and the compiled
# structural check. Nothing is re-qualified and no preview.150 verdict changes. The test proves
# that the corrected reader accepts the lawful native envelopes the compiled preview.150 validator
# refused, that the compiled structural check accepts the same evidence without deciding it, and
# that the one restoration defect is still refused - under a reader digest that differs from the
# harness that qualified preview.150 while the product payload stays the frozen preview.150 identity.
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'runtime/Chunk6aSupportingEvidence.ps1')
. (Join-Path $PSScriptRoot 'runtime/Chunk6aAdditionalEvidence.ps1')
. (Join-Path $PSScriptRoot 'runtime/Chunk6aHarnessIdentity.ps1')
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'));$paths=[xml](Get-Content -Raw (Join-Path $repo 'LocalGamePaths.props'));$managed=Join-Path $paths.Project.PropertyGroup.KingmakerInstallDir 'Kingmaker_Data/Managed'
[Reflection.Assembly]::LoadFrom((Join-Path $managed 'Newtonsoft.Json.dll'))|Out-Null
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $repo ('bin/'+$Configuration+'/KingmakerMountedCombat.dll')))
$method=$assembly.GetType('KingmakerMountedCombat.Diagnostics.NativeActionEconomyEvidence',$true).GetMethod('AssertStructure',[Reflection.BindingFlags]'Static,NonPublic')
if($null-eq$method-or$method.GetParameters().Count-ne1){throw 'Compiled structural check is missing'}
$ledgerPath=Join-Path $LabRoot $LedgerLeaf
if(-not(Test-Path -LiteralPath $ledgerPath -PathType Leaf)){throw ('Immutable preview.150 ledger is absent: '+$ledgerPath)}
$ledger=Get-Content -Raw -LiteralPath $ledgerPath|ConvertFrom-Json
$frozenCommit='2c128426c952dd521f887e75add5ffb07e07b876'
if([string]$ledger.payload.version-cne'0.1.0-chunk6a-preview.150'-or[string]$ledger.payload.commit-cne$frozenCommit){throw 'Ledger is not the frozen preview.150 ledger'}
$recorded=$ledger.harness;$current=Get-KmcChunk6aHarnessIdentity $repo
if([string]$recorded.readerDigest-cnotmatch'^[0-9a-f]{64}$'-or[string]$recorded.readerDigest-ceq[string]$current.readerDigest){throw 'The current reader digest equals the preview.150 harness digest; nothing is re-evaluated under a new harness identity'}
$productDigest=Get-KmcChunk6aSourceTreeDigest $repo $frozenCommit
if($productDigest-cne[string]$ledger.payload.sourceTreeDigest){throw 'Frozen product source tree digest differs from the ledger payload'}
$cases=@(
 [pscustomobject]@{run='c6a-action-economy150-a-mount-spent-standard';id='CM03-mount-spent-standard';rowStatus='FAIL';restorationRefusal=$null},
 [pscustomobject]@{run='c6a-action-economy150-a-dismount-after-rider';id='CM05-after-rider-expenditure';rowStatus='FAIL';restorationRefusal=$null},
 [pscustomobject]@{run='c6a-action-economy150-a-dismount-after-mount';id='CM05-after-mount-expenditure';rowStatus='FAIL';restorationRefusal=$null},
 [pscustomobject]@{run='c6a-action-economy150-a-dismount-immediate';id='CM05-immediately-after-mount';rowStatus='FAIL';restorationRefusal=$null},
 # In preview.150 the lease anomaly threw before the automatic-End probe was disposed, so neither
 # restoration was recorded: the first refusal is the automatic-End one, and with that fact patched
 # on a copy the unrelated-lease refusal still stands. Preview.151 restores both unconditionally.
 [pscustomobject]@{run='c6a-action-economy150-a-unrelated-between';id='CM03-unrelated-candidate-between';rowStatus='PASS';restorationRefusal='automatic End preference was not restored exactly';secondRefusal='unrelated initiative input not restored exactly'}
)
$checks=0;$lines=@()
foreach($case in $cases){
 $retained=@($ledger.retainedFailures|Where-Object {[string]$_.runId-ceq$case.run})
 if($retained.Count-ne1){throw ('Retained failure entry missing: '+$case.run)}
 $retained=$retained[0]
 if([string]$retained.id-cne$case.id-or[string]$retained.status-cne'FAIL'){throw ('Retained failure identity differs: '+$case.run)}
 $sha=[string]$retained.diagnosticEvidence.horseArtifact.sha256
 if($sha-cnotmatch'^[0-9a-f]{64}$'){throw ('Retained failure binds no Horse artifact hash: '+$case.run)}
 $root=Join-Path $LabRoot ('runtime-evidence/'+$case.run)
 $artifact=Get-KmcBoundJson (Join-Path $root 'phase3d-horse-scenario-evidence.json') $sha
 $request=Get-Content -Raw -LiteralPath (Join-Path $root 'runtime-request.json')|ConvertFrom-Json
 foreach($name in @('runId','scenario','commit','productVersion','dllSha256','dllMvid')){if([string]$request.$name-cne[string]$artifact.$name){throw ('Request/artifact identity differs: '+$name+' of '+$case.run)}}
 if([string]$request.commit-cne$frozenCommit-or[string]$request.productVersion-cne'0.1.0-chunk6a-preview.150'-or[string]$request.dllSha256-cne[string]$ledger.payload.dllSha256){throw ('Artifact is not a preview.150 transaction: '+$case.run)}
 $v=Get-KmcActionEconomyVariant ([string]$request.scenario)
 if($null-eq$v-or[string]$v.row-cne$case.id){throw ('Variant differs: '+$case.run)}
 # The preview.150 verdict stands exactly as retained.
 $row=@($artifact.rows|Where-Object name -CEQ $v.row)
 if($row.Count-ne1-or[string]$row[0].status-cne$case.rowStatus){throw ('Immutable row verdict differs: '+$case.run)}
 if($case.rowStatus-ceq'FAIL'){
  $detail=[string]$row[0].detail;$assertion=[string]$retained.failingAssertion
  if([string]::IsNullOrEmpty($assertion)-or($detail.IndexOf($assertion,[StringComparison]::Ordinal)-lt0-and$assertion.IndexOf($detail,[StringComparison]::Ordinal)-lt0)){throw ('Immutable failing assertion differs: '+$case.run)}
 }
 $checks++
 # The compiled structural check accepts the same evidence without deciding it.
 $e=$artifact.observations.chunk6aActionEconomy
 $invokeArgs=[object[]]::new(1);$invokeArgs[0]=[Newtonsoft.Json.Linq.JObject]::Parse(($e|ConvertTo-Json -Depth 100 -Compress))
 try{$null=$method.Invoke($null,$invokeArgs)}catch{throw ('Compiled structural check refused immutable evidence '+$case.run+': '+$(if($null-ne$_.Exception.InnerException){$_.Exception.InnerException.Message}else{$_.Exception.Message}))}
 $checks++
 # The external authority accepts the bound envelope facts (proofs, order fixture, full trace).
 $facts=@(Assert-KmcActionEconomyEnvelopeFacts $request $artifact)[-1]
 if([string]$facts.v.row-cne$v.row){throw ('Envelope facts variant differs: '+$case.run)}
 $checks++
 # Restoration is judged separately: exact for four transactions, refused for the one defect.
 $message=$null;try{Assert-KmcActionEconomyRestoration $e}catch{$message=$_.Exception.Message}
 if($null-eq$case.restorationRefusal){if($null-ne$message){throw ('Restoration refused on '+$case.run+': '+$message)}}
 elseif($null-eq$message-or$message.IndexOf($case.restorationRefusal,[StringComparison]::Ordinal)-lt0){throw ('Restoration defect no longer refused on '+$case.run+': '+$message)}
 else{
  $second=[string]($case.PSObject.Properties['secondRefusal'].Value)
  if(-not[string]::IsNullOrEmpty($second)){
   if($null-ne$e.unrelated.PSObject.Properties['restoration']){throw ('Immutable evidence unexpectedly records a lease restoration: '+$case.run)}
   $patched=$e|ConvertTo-Json -Depth 100|ConvertFrom-Json;$patched.automaticEnd|Add-Member -NotePropertyName restored -NotePropertyValue $true -Force
   $message2=$null;try{Assert-KmcActionEconomyRestoration $patched}catch{$message2=$_.Exception.Message}
   if($null-eq$message2-or$message2.IndexOf($second,[StringComparison]::Ordinal)-lt0){throw ('Second restoration defect no longer refused on '+$case.run+': '+$message2)}
  }
 }
 $checks++
 $lines+=($case.run+': row '+$case.rowStatus+' unchanged ('+$sha.Substring(0,12)+'); structure accepted; external envelope accepted; restoration '+$(if($null-eq$case.restorationRefusal){'exact'}else{'refused: '+$case.restorationRefusal+$(if(-not[string]::IsNullOrEmpty([string]($case.PSObject.Properties['secondRefusal'].Value))){'; then '+[string]$case.secondRefusal}else{''})}))
}
$lines|ForEach-Object {Write-Output ('  '+$_)}
Write-Output ('IMMUTABLE REPLAY PASS='+$checks+' FAIL=0; frozen preview.150 payload '+$frozenCommit.Substring(0,12)+' (source tree '+$productDigest.Substring(0,12)+') unchanged; harness digest '+([string]$recorded.readerDigest).Substring(0,12)+' -> '+([string]$current.readerDigest).Substring(0,12)+'; no verdict re-qualified')
