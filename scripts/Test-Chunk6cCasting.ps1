$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
$passed=0
function Check([bool]$condition,[string]$why){if(-not$condition){throw $why};$script:passed++;Write-Host ('PASS '+$why)}
function Copy-CastingFixture($v){$v|ConvertTo-Json -Depth 40 -Compress|ConvertFrom-Json}
function New-CastingFixtureState {
 [pscustomobject]@{relationship='Mounted';generation=1;rider=[pscustomobject]@{actor='rider';standard=0.0;move=0.0;swift=0.0};mount=[pscustomobject]@{actor='mount';standard=0.0;move=0.0;swift=0.0};riderCommandsEmpty=$true;mountCommandsEmpty=$true;processesSettled=$true;nativeAbilitiesPending=$false;projectilesPending=$false;activePairCommand=$false;items=@();ability=[pscustomobject]@{caster='rider';blueprint='c3a8f31778c3980498d8f00c980be5f5';runtimeActionType='Standard';fullRound=$false};slotAvailable=$null}
}
function Positive([bool]$tb){
 $before=New-CastingFixtureState;$after=Copy-CastingFixture $before;$after.rider.standard=if($tb){6.0}else{4.0}
 $post=Copy-CastingFixture $before;$post|Add-Member shell ([pscustomobject]@{identity=10})
 [pscustomobject]@{name='C6C-standard-self';status='PASS';evidence=[pscustomobject]@{case='C6C-standard-self';before=$before;afterInput=$post;after=$after;selectionAfter=@('rider');target='rider';resolvedTarget='rider';selected=$true;canTarget=$true;clicked=$true;inputCount=1;admittedShellCount=1;costShell=100;events=@([pscustomobject]@{kind='action-after';identity=10;actor='rider';shell=[pscustomobject]@{ignoreCooldown=$false;timeSinceStart=2.0}},[pscustomobject]@{kind='cast-after';actor='rider';ability=[pscustomobject]@{blueprint='c3a8f31778c3980498d8f00c980be5f5'};process=20;spellFailed=$false;arcaneFailed=$false});costEvents=@([pscustomobject]@{boundary='cost-before';command=100;state=$before.rider},[pscustomobject]@{boundary='cost-after';command=100;state=$after.rider})}}
}
function Accept($row,[bool]$tb=$false,[bool]$mounted=$true){Assert-KmcChunk6cCastingRow $row 'rider' 'mount' $tb $mounted}
function Reject($row,[string]$why,[bool]$tb=$false){$refused=$false;try{Accept $row $tb}catch{$refused=$true};Check $refused $why}
foreach($tb in @($false,$true)){Accept (Positive $tb) $tb;Check $true ('native single Standard cost accepted '+$tb)}
$base=Positive $false
$r=Copy-CastingFixture $base;$r.evidence.before.ability.caster='mount';Reject $r 'mount caster refused'
$r=Copy-CastingFixture $base;$r.evidence.inputCount=2;Reject $r 'duplicate input refused'
$r=Copy-CastingFixture $base;$r.evidence.admittedShellCount=2;Reject $r 'duplicate source shell refused'
$r=Copy-CastingFixture $base;$r.evidence.events+=Copy-CastingFixture $r.evidence.events[1];Reject $r 'duplicate source delivery refused'
$r=Copy-CastingFixture $base;$r.evidence.after.riderCommandsEmpty=$false;Reject $r 'rider command residue refused'
$r=Copy-CastingFixture $base;$r.evidence.after.mountCommandsEmpty=$false;Reject $r 'mount command residue refused'
$r=Copy-CastingFixture $base;$r.evidence.after.processesSettled=$false;Reject $r 'pending native process refused'
$r=Copy-CastingFixture $base;$r.evidence.after.nativeAbilitiesPending=$true;Reject $r 'native execution controller residue refused'
$r=Copy-CastingFixture $base;$r.evidence.after.generation=2;Reject $r 'relationship generation substitution refused'
$r=Copy-CastingFixture $base;$r.evidence.after.mount.standard=1.0;Reject $r 'duplicate mount action cost refused'
$r=Copy-CastingFixture $base;$r.evidence.costEvents[1].state.standard=3.5;Reject $r 'incorrect native realtime debt refused'
$r=Copy-CastingFixture $base;$r.evidence.costEvents[1].state.swift=1.0;Reject $r 'extra rider Swift cost refused'
$r=Positive $true;$r.evidence.costEvents[1].state.standard=3.0;Reject $r 'incorrect additive TB debt refused' $true
$r=Copy-CastingFixture $base;$r.evidence.costEvents+=[pscustomobject]@{boundary='prepare-before';state=$r.evidence.before.rider};Reject $r 'pair preparation replay refused'
$r=Copy-CastingFixture $base;foreach($s in @($r.evidence.before,$r.evidence.afterInput,$r.evidence.after)){$s.relationship='Unmounted'};Accept $r $false $false;Check $true 'normal unmounted cost owner preserved'
foreach($name in @('C6C-invalid-target','C6C-cancel-before')){
 $r=Copy-CastingFixture $base;$r.name=$name;$r.evidence.case=$name;$r.evidence.events=@();$r.evidence.costEvents=@();$r.evidence.admittedShellCount=0;$r.evidence.inputCount=if($name-ceq'C6C-cancel-before'){0}else{1};$r.evidence.canTarget=$false;$r.evidence|Add-Member cancelledSelection $true
 Accept $r;Check $true ('cost-free native refusal '+$name)
 $r.evidence.costEvents=@($base.evidence.costEvents[0]);Reject $r ('cost cannot masquerade as refusal '+$name)
}
foreach($scenario in @('chunk6c-casting-rt','chunk6c-casting-tb','chunk6c-casting-unmounted-rt','chunk6c-casting-unmounted-tb')){
 Check (Test-KmcPhase3dSchemaRegistration 44 $scenario) ('exact schema registered '+$scenario)
 Check (-not(Test-KmcPhase3dSchemaRegistration 43 $scenario)) ('stale schema rejected '+$scenario)
 Check ((Get-KmcPhase3dEvidenceDispatch 44 $scenario).validator-ceq'Assert-KmcChunk6cCastingEvidence') ('exact reader selected '+$scenario)
 Check (-not(Test-KmcChildEntryExpectsMounted $scenario)) ('native original-pair exploration handoff '+$scenario)
}
foreach($v in @('0.1.0-chunk6c-preview.189','0.1.0-chunk6d-preview.200','0.1.0-chunk6e-preview.201','0.1.0-chunk6f-preview.202')){Check (Test-KmcChildEntryPreambleRequired $v) ('later phase retains preamble guard '+$v)}
foreach($v in @('0.1.0-chunk6g-preview.189','0.1.0-chunk6c-preview.bad','0.1.0-chunk6c-preview.149')){Check (-not(Test-KmcChildEntryPreambleRequired $v)) ('malformed/unregistered product refused '+$v)}
function Set-CastingAction($row,[bool]$tb,[string]$action){
 $e=$row.evidence;$e.before.ability.runtimeActionType=$action
 foreach($f in @('standard','move','swift')){$e.after.rider.$f=0.0}
 $field=if($action-ceq'Swift'){'swift'}elseif($action-ceq'Move'){'move'}else{'standard'}
 $nominal=if($action-ceq'Move'){3.0}else{6.0};$e.after.rider.$field=if($tb){$nominal}else{$nominal-2.0}
 $e.costEvents[0].state=Copy-CastingFixture $e.before.rider;$e.costEvents[1].state=Copy-CastingFixture $e.after.rider
}
function New-CastingRow([string]$name,[bool]$tb,[bool]$mounted){
 $r=Positive $tb;$r.name=$name;$e=$r.evidence;$e.case=$name
 if(-not$mounted){foreach($v in @($e.before,$e.afterInput,$e.after)){$v.relationship='Unmounted'}}
 $blueprint=switch -CaseSensitive($name){'C6C-standard-hostile'{'9f10909f0be1f5141bf1c102041f93d9'} 'C6C-prepared-interrupt-after'{'5590652e1c2225c4ca30c4a699ab3649'} 'C6C-potion-self'{'5590652e1c2225c4ca30c4a699ab3649'} 'C6C-scroll-friendly'{'5590652e1c2225c4ca30c4a699ab3649'} 'C6C-full-round'{'c6147854641924442a3bb736080cfeb6'} default{'c3a8f31778c3980498d8f00c980be5f5'}}
 $e.before.ability.blueprint=$blueprint;$e.events[1].ability.blueprint=$blueprint
 if($name-ceq'C6C-quickened-self'){
  Set-CastingAction $r $tb 'Swift'
  $e.before.items=@([pscustomobject]@{blueprint='55a059b32df920c4abe65b8ee8b56056';item=60;activatableSourceItem=60;activatableOn=$true;charges=3});$e.after.items=@([pscustomobject]@{blueprint='55a059b32df920c4abe65b8ee8b56056';item=60;charges=2})
 }
 if($name-cin@('C6C-standard-hostile','C6C-prepared-interrupt-after','C6C-full-round')){$e.before.slotAvailable=$true;$e.after.slotAvailable=$false}
 if($name-ceq'C6C-prepared-interrupt-after'){$e|Add-Member interrupted $true;$e|Add-Member interruptionBefore ([pscustomobject]@{shell=[pscustomobject]@{acted=$true;finished=$false}})}
 if($name-cin@('C6C-invalid-target','C6C-cancel-before','C6C-interrupt-before','C6C-rider-incapacity')){
  $e.events=@();$e.costEvents=@();$e.after.rider.standard=0.0
  if($name-ceq'C6C-invalid-target'){$e.canTarget=$false;$e.admittedShellCount=0}
  if($name-ceq'C6C-cancel-before'){$e.inputCount=0;$e|Add-Member cancelledSelection $true}
  if($name-ceq'C6C-interrupt-before'){$e|Add-Member interrupted $true;$e|Add-Member interruptionBefore ([pscustomobject]@{shell=[pscustomobject]@{started=$true;acted=$false}})}
 }
 if($name-cin@('C6C-potion-self','C6C-scroll-friendly')){
  $itemBlueprint=if($name-ceq'C6C-potion-self'){'d52566ae8cbe8dc4dae977ef51c27d91'}else{'cd635d5720937b044a354dba17abad8d2'}
  $e.before.ability|Add-Member sourceItem 60;$e.before.ability|Add-Member sourceItemBlueprint $itemBlueprint
  if($name-ceq'C6C-potion-self'){Set-CastingAction $r $tb 'Move'}
  $e.events+=@([pscustomobject]@{kind='item-spend-before';identity=60;actor='rider';charges=1;count=1},[pscustomobject]@{kind='item-spend-after';identity=60;actor='rider';charges=0;count=1;result=$true},[pscustomobject]@{kind='heal';actor='rider';target='rider';value=3})
 }
 if($name-ceq'C6C-full-round'){
  $e.before.ability.fullRound=$true
  if($tb){$e.after.rider.move=3.0;$e.costEvents[1].state.move=3.0}
  $e.events+=[pscustomobject]@{kind='summon';actor='rider';unitObject=70;context=20}
 }
 if($name-cin@('C6C-rider-incapacity','C6C-mount-incapacity')){
  $e.after.relationship='Unmounted';$e.after|Add-Member relationshipRider $null;$e.after|Add-Member relationshipMount $null
  $e.after|Add-Member riderLife ([pscustomobject]@{conscious=$true;dead=$false});$e.after|Add-Member mountLife ([pscustomobject]@{conscious=$true;dead=$false})
  $e|Add-Member boundary ([pscustomobject]@{subject=$(if($name-ceq'C6C-rider-incapacity'){'rider'}else{'mount'});nativeDamage=11;damageAfter=11;hitPoints=10;deathThreshold=20;unconsciousObserved=$true;deadObserved=$false;healthRestored=$true;damageRestored=0;damageBefore=0;settledBeforeHealthRestore=[pscustomobject]@{riderCommandsEmpty=$true;mountCommandsEmpty=$true;processesSettled=$true}})
 }
 if($name-ceq'C6C-movement-policy'){
  $e.after|Add-Member pairMovement $false
  $e|Add-Member boundary ([pscustomobject]@{nativeGroundInputCount=1;carrier=40;costCarrier=400;carrierExecutor=$(if($mounted){'mount'}else{'rider'});setupCostEvents=@()})
  $e|Add-Member motionSamples @([pscustomobject]@{carrierFinished=$true;moverMoveSlotOwnsCarrier=$false})
 }
 if($name-ceq'C6C-under-threat'){
  $e|Add-Member boundary ([pscustomobject]@{riderEngaged=$true;nativeHostileAttackInputCount=1;nativeAttackTerminalBeforeCast=$true})
  $e.events+=[pscustomobject]@{kind='defensive-rule-after';actor='rider';dc=17;roll=19;success=$true}
 }
 $r
}
foreach($tb in @($false,$true)){foreach($mounted in @($false,$true)){foreach($name in Get-KmcChunk6cCastingCases){Accept (New-CastingRow $name $tb $mounted) $tb $mounted;Check $true ('native fact row '+$name+' TB='+$tb+' mounted='+$mounted)}}}
$r=New-CastingRow 'C6C-standard-self' $false $true;$r.evidence.costShell=10;Reject $r 'cost trace may not reuse another registry identity'
$r=New-CastingRow 'C6C-full-round' $true $true;$r.evidence.costEvents[1].state.move=0.0;Reject $r 'TB full-round Move commitment not dropped' $true
$r=New-CastingRow 'C6C-rider-incapacity' $false $true;$r.evidence.boundary.healthRestored=$false;Reject $r 'unrestored fixture incapacity cannot qualify'
$r=New-CastingRow 'C6C-movement-policy' $false $true;$r.evidence.motionSamples[0].carrierFinished=$false;Reject $r 'live delegated movement cannot disappear'
$r=New-CastingRow 'C6C-under-threat' $false $true;$r.evidence.boundary.riderEngaged=$false;Reject $r 'mere nearby enemy cannot stand for native threat'
$r=New-CastingRow 'C6C-potion-self' $false $true;$r.evidence.events+=$r.evidence.events[3];Reject $r 'duplicate potion spending rejected'
# Same file-backed envelope and original-pair preamble used by the real runtime gate.
$tokens=$null;$parseErrors=$null;$ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'Test-ChildEntryPreamble.ps1'),[ref]$tokens,[ref]$parseErrors)
$definition=@($ast.FindAll({param($n)$n-is[Management.Automation.Language.FunctionDefinitionAst]-and$n.Name-ceq'New-Preamble'},$true));if($parseErrors.Count-ne0-or$definition.Count-ne1){throw 'Existing preamble fixture unavailable'};. ([scriptblock]::Create($definition[0].Extent.Text))
$root=Join-Path (Get-KmcRepositoryRoot) ('obj/chunk6c-envelope/'+[Guid]::NewGuid().ToString('N'))
foreach($scenario in @('chunk6c-casting-rt','chunk6c-casting-tb','chunk6c-casting-unmounted-rt','chunk6c-casting-unmounted-tb')){
 $tb=$scenario.EndsWith('-tb');$mounted=-not$scenario.Contains('-unmounted-');$rows=@(Get-KmcChunk6cCastingCases|ForEach-Object {New-CastingRow $_ $tb $mounted})
 $pre=New-Preamble $false;$pre.childScenario=$scenario;$pre.parentScenario=$scenario;$pre.parentEngine='KingmakerMountedCombat.Diagnostics.Chunk6aMammothScenarioEngine';$pre.runId='casting-envelope'
 $trace=[pscustomobject]@{faults=0;dropped=0;observationErrors=0;identityRegistry=[pscustomobject]@{faults=0;released=$true;retainedCount=0}}
 $artifact=[pscustomobject]@{schemaVersion=44;evidenceKind='phase3d-horse-scenario-evidence';runId='casting-envelope';scenario=$scenario;branch='codex/mounted-combat-phase3f-playable-core';commit=('a'*40);productVersion='0.1.0-chunk6c-preview.189';dllSha256=('b'*64);dllMvid='00000000-0000-0000-0000-000000000001';createdAtUtc='2026-10-07T00:00:00Z';status='PASS';rows=$rows;subscenarioPassCount=$rows.Count;subscenarioFailCount=0;errors=@();observations=[pscustomobject]@{riderId='rider';horseId='mount';childEntryPreamble=$pre;chunk6cCasting=[pscustomobject]@{contract='native-mounted-casting-items-v1';rider='rider';mount='mount';mountBlueprint='e7aa96d15a45238438ae4cfb476f6bb9';mode=$(if($tb){'TB'}else{'RT'});mounted=$mounted;cases=@(Get-KmcChunk6cCastingCases);castTrace=$trace;costTrace=$trace;items=@([pscustomobject]@{disposed=$true;slotRestored=$true;noOwnedItemResident=$true});summonCleanup=@();final=$rows[0].evidence.after}}}
 $request=@{runId=$artifact.runId;scenario=$scenario;branch=$artifact.branch;commit=$artifact.commit;productVersion=$artifact.productVersion;dllSha256=$artifact.dllSha256;dllMvid=$artifact.dllMvid;evidenceRoot=(Join-Path $root $scenario)};[void][IO.Directory]::CreateDirectory($request.evidenceRoot);$path=Join-Path $request.evidenceRoot 'phase3d-horse-scenario-evidence.json';$manifest=@{artifacts=@(@{relativePath='phase3d-horse-scenario-evidence.json';kind='phase3d-horse-scenario-evidence'})}
 function Envelope($value){$value|ConvertTo-Json -Depth 60|Set-Content -LiteralPath $path -Encoding UTF8;Assert-KmcPhase3dHorseScenarioEvidence $request $manifest 'PASS'}
 Envelope $artifact;Check $true ('complete current envelope '+$scenario)
 foreach($mutation in @({param($a)$a.schemaVersion=43},{param($a)$a.commit=('c'*40)},{param($a)$a.rows=@($a.rows|Select-Object -Skip 1);$a.subscenarioPassCount--},{param($a)$a.observations.childEntryPreamble.parentEngine='KingmakerMountedCombat.Diagnostics.HorseCompanionUnmountedScenarioEngine'},{param($a)$a.observations.chunk6cCasting.castTrace.faults=1},{param($a)$a.rows[1].evidence.after.mount.standard=1.0})){
  $changed=Copy-CastingFixture $artifact;& $mutation $changed;$rejected=$false;try{Envelope $changed}catch{$rejected=$true};Check $rejected ('full envelope retains substantive refusal '+$scenario)
 }
 Envelope $artifact
}
Write-Host ('TOTAL PASS='+$passed+' FAIL=0')