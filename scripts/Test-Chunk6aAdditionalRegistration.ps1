param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'runtime/Chunk6aSupportingEvidence.ps1')
$checks=0
function Copy-Additional($x){$x|ConvertTo-Json -Depth 30|ConvertFrom-Json}
$expectedRoles=[ordered]@{'CM03-early-end-turn'=2;'CM03-next-round-activation'=2;'CM05-dismount-survives-feature-policy-disable'=2;'CM06-ai-auto-use'=2;'CM05-forced-detach'=3}
foreach($id in @($expectedRoles.Keys)){
 $roles=@(Get-KmcChunk6aAdditionalRoles $id)
 if($roles.Count-ne$expectedRoles[$id]){throw 'Combined claim lost a required case'};$checks++
 $bindings=@($roles|ForEach-Object{[pscustomobject]@{role=$_.name;scenario=$_.scenario;runId=('synthetic-'+$_.name);rows=@($_.rows);evidenceSha256=('a'*64)}})
 $primary=Copy-Additional $bindings[0]
 Assert-KmcChunk6aAdditionalBindings $id $primary $bindings;$checks++
 foreach($name in @($id)+@($roles|ForEach-Object scenario)){
  if(@(Get-KmcPhase3dHorseRuntimeRows|Where-Object {$_-ceq$name}).Count-ne1){throw ('Missing or duplicate runtime row '+$name)};$checks++
 }
 function Reject-Additional([scriptblock]$Mutate){
  $b=Copy-Additional $bindings;$p=Copy-Additional $primary;& $Mutate $p $b
  $failed=$false;try{Assert-KmcChunk6aAdditionalBindings $id $p $b}catch{$failed=$true}
  if(-not$failed){throw ('Invalid combined qualification accepted: '+$Mutate)};$script:checks++
 }
 foreach($index in 0..($roles.Count-1)){
  Reject-Additional {param($p,$b)$b[$index].role='foreign'}
  Reject-Additional {param($p,$b)$b[$index].scenario='chunk6a-mount-approach'}
  Reject-Additional {param($p,$b)$b[$index].rows=@()}
  Reject-Additional {param($p,$b)$b[$index].rows+=@($b[$index].rows[0])}
 }
 Reject-Additional {param($p,$b)$b[1].runId=$b[0].runId}
 Reject-Additional {param($p,$b)$p.runId='unbound'}
 Reject-Additional {param($p,$b)$p.scenario='foreign'}
 Reject-Additional {param($p,$b)$p.evidenceSha256='b'*64}
 Reject-Additional {param($p,$b)$p.rows=@()}
 foreach($count in @(0,($roles.Count-1),($roles.Count+1))){
  $b=@();for($i=0;$i-lt$count;$i++){$b+=@($bindings[$i%$roles.Count])};$failed=$false;try{Assert-KmcChunk6aAdditionalBindings $id $primary $b}catch{$failed=$true};if(-not$failed){throw 'Missing/repeated allocation accepted'};$checks++
 }
}
foreach($case in @(@('CM03-rider-before-mount-slot','chunk6a-allocation-rider-first-tb'),@('CM03-mount-slot-before-rider','chunk6a-allocation-mount-first-tb'))){
 $binding=[pscustomobject]@{scenario=$case[1];rows=@($case[0],'CM03-next-round-activation')}
 if(-not(Assert-KmcIsolatedScenarioRows $case[0] $binding)){throw 'Missing exact order mapping'};$checks++
 $binding.scenario='chunk6a-mount-approach';$failed=$false;try{$null=Assert-KmcIsolatedScenarioRows $case[0] $binding}catch{$failed=$true};if(-not$failed){throw 'Order credit from another scenario accepted'};$checks++
}
$source=Get-Content -Raw (Join-Path $PSScriptRoot 'Test-Chunk6aLedger.ps1')
if(-not$source.Contains('Assert-KmcChunk6aAdditionalQualification $id $payload $binding')){throw 'Ledger omitted combined qualification'};$checks++
'ADDITIONAL CASE REGISTRATION PASS='+$checks+' FAIL=0; no native qualification'
