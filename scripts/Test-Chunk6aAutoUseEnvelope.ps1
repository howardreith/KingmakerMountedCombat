param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
$checks=0
function Copy-Envelope($v){$v|ConvertTo-Json -Depth 100|ConvertFrom-Json}
foreach($kind in @('mount','dismount')){
 $p=Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot ('fixtures/chunk6a-auto-use/envelope-'+$kind+'.json'))|ConvertFrom-Json
 $request=[pscustomobject]@{scenario=$p.scenario}
 Assert-KmcChunk6aCombatMountEvidence $request $p 'PASS';$checks++
 function Reject-Envelope([scriptblock]$change){$x=Copy-Envelope $p;&$change $x;$failed=$false;try{Assert-KmcChunk6aCombatMountEvidence $request $x 'PASS'}catch{$failed=$true};if(-not$failed){throw ('Bad full envelope accepted: '+$change.ToString())};$script:checks++}
 Reject-Envelope {param($x)$x.rows=@()};Reject-Envelope {param($x)$x.rows+=@($x.rows[-1])};Reject-Envelope {param($x)$x.rows[-1].status='FAIL'}
 Reject-Envelope {param($x)$x.rows[-1].evidence.input.suitable=$true}
 Reject-Envelope {param($x)$x.observations.actorAllocationTrace.events=@()};Reject-Envelope {param($x)$x.observations.actorAllocationTrace.observerHooks=@()};Reject-Envelope {param($x)$x.observations.actorAllocationTrace.observationErrors=1}
 Reject-Envelope {param($x)$x.observations.chunk6aAutoUse.scenario='chunk6a-mount-approach';$x.rows[-1].evidence=Copy-Envelope $x.observations.chunk6aAutoUse}
 foreach($proofIndex in 0..($p.observations.chunk6aCommandProofs.Count-1)){
  foreach($field in @('reactions','reactionCooldown')){Reject-Envelope {param($x)$x.observations.chunk6aCommandProofs[$proofIndex].resourceWindow.events[0].state.$field++}}
  Reject-Envelope {param($x)$x.observations.chunk6aCommandProofs[$proofIndex].samples=@($x.observations.chunk6aCommandProofs[$proofIndex].samples|Where-Object boundary -CNE 'acted')}
 }
 foreach($role in @('rider','mount')){foreach($field in @('reactions','reactionCooldown','initiativeOrder','initiativeCooldown')){
  Reject-Envelope {param($x)$e=$x.observations.chunk6aAutoUse;$e.input.afterLease.state.$role.$field++;$x.rows[-1].evidence=Copy-Envelope $e}
 }}
 foreach($name in @('CM02-adoption-plan-invalidated','CM02-geometry-change','CM02-obstruction','CM05-combat-dismount-accepted','CM06-hotbar-path')){Reject-Envelope {param($x)$x.rows+=@([pscustomobject]@{name=$name;status='PASS'})}}
 if($kind-ceq'dismount'){
  Reject-Envelope {param($x)$e=$x.observations.chunk6aAutoUse;$e.mountProof.samples=@($e.mountProof.samples|Where-Object boundary -CNE 'acted');$positive=@($x.observations.chunk6aCommandProofs|Where-Object window -CEQ 'positive-mount')[0];$positive.samples=Copy-Envelope $e.mountProof.samples;$x.rows[-1].evidence=Copy-Envelope $e}
 }
}
'AUTO USE FULL ENVELOPE PASS='+$checks+' FAIL=0; synthetic only, no native qualification'
