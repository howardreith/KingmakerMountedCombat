$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
$passed=0
function Check([bool]$condition,[string]$why){if(-not$condition){throw $why};$script:passed++;Write-Host ('PASS '+$why)}
function Copy-Staged($v){$v|ConvertTo-Json -Depth 50 -Compress|ConvertFrom-Json}
function Copy-StagedItems($v){@((Copy-Staged $v)|ForEach-Object {$_})}
$Rider='rider';$Mount='mount';$Hostile='hostile'
$Scroll='cd635d5720937b044a354dba17abad8d';$Potion='d52566ae8cbe8dc4dae977ef51c27d91';$Clw='5590652e1c2225c4ca30c4a699ab3649'
$castingLeases=@([pscustomobject]@{blueprint=$Potion;equipped=[pscustomobject]@{item=61;count=1;exactSlot=$true}},[pscustomobject]@{blueprint=$Scroll;equipped=[pscustomobject]@{item=60;count=1;exactSlot=$true}})
# One StagedState snapshot as the compiled producer records it (CastingState plus the staged facts).
function New-StagedState([double]$riderStandard,[double]$mountMove,[double]$mountX,[bool]$mountHasMove,[bool]$usedTwo,[int]$scrollCount,[string]$turnActor,[double]$riderSwift=0.0){
 [pscustomobject]@{relationship='Mounted';generation=1;rider=[pscustomobject]@{actor=$Rider;standard=$riderStandard;move=0.0;swift=$riderSwift};mount=[pscustomobject]@{actor=$Mount;standard=0.0;move=$mountMove;swift=0.0}
  riderCommandsEmpty=$true;mountCommandsEmpty=$true;processesSettled=$true;nativeAbilitiesPending=$false;projectilesPending=$false;activePairCommand=$false;pairMovement=$false
  items=@([pscustomobject]@{blueprint=$Potion;item=61;count=2;charges=1;exactSlot=$true},[pscustomobject]@{blueprint=$Scroll;item=60;count=$scrollCount;charges=1;exactSlot=$true})
  slotAvailable=$null;mountUsedOneMove=($mountMove -gt 0);mountUsedTwoMove=$usedTwo;mountHasMove=$mountHasMove;mountMoveRestricted=$false
  riderHasStandard=($riderStandard -lt 6);riderHasSwift=$true;riderHasMove=$true;riderCanAct=$true;turnActor=$turnActor;turnIsActing=$true;turnTimeMoved=$mountMove
  mountPosition=[pscustomobject]@{x=$mountX;y=0.0;z=0.0};riderPosition=[pscustomobject]@{x=$mountX;y=0.0;z=0.0};rejectionCodes=@()}
}
function New-Carrier([int]$id){[pscustomobject]@{identity=$id;costIdentity=$id*10;type='UnitMoveTo';executor=$Mount;createdByPlayer=$true;started=$true;acted=$true;finished=$true;result='Success';ignoreCooldown=$false;commandType='Move';timeSinceStart=1.5}}
function New-MoveLeg([int]$index,$before,$after,[bool]$moves){
 $leg=[pscustomobject]@{index=$index;frame=100+$index;probe=$false;requestedDistance=7.0;destination=[pscustomobject]@{x=$after.mountPosition.x;y=0.0;z=0.0};plannedDistance=7.0;before=$before;fiveFootStepMode=$false;singleActionMoveMode=$false;ignoreClick=$false;inputPath='pointer';cursorCycles=0;clicked=$true;inputCount=1
  carrier=$(if($moves){New-Carrier (40+$index)}else{$null});riderMoveSlot=0;mountMoveSlot=$(if($moves){40+$index}else{0});pairMovementAfterInput=$moves;afterInput=$before;after=$after;terminal=$(if($moves){New-Carrier (40+$index)}else{$null});costEvents=@();events=@()}
 $leg
}
function New-MoveStep([int]$index,[string]$kind,$legs){
 [pscustomobject]@{index=$index;kind=$kind;frame=100;legs=@($legs);after=$legs[-1].after;costEvents=@();events=@();terminal=$legs[-1].terminal;hostileAttackTerminal=$null}
}
function New-CastStep([int]$index,$before,$after,[bool]$tb){
 $shellIdentity=10+$index;$cost=100+$index
 [pscustomobject]@{index=$index;kind='cast-standard-scroll';frame=200;ability=[pscustomobject]@{identity=5;blueprint=$Clw;caster=$Rider;runtimeActionType='Standard';fullRound=$false;sourceItem=60;sourceItemBlueprint=$Scroll;available=$true;availableForCast=$true}
  target=$Rider;before=$before;turnActor=$Rider;ignoreClick=$false;riderCanAct=$true;selected=$true;resolvedTarget=$Rider;canTarget=$true;clicked=$true;inputCount=1;admittedShellCount=1;shell=[pscustomobject]@{identity=$shellIdentity;executor=$Rider};costShell=$cost;queued=$false;selectionAfter=@($Rider);afterInput=$before;after=$after
  terminal=[pscustomobject]@{identity=$shellIdentity;costIdentity=$cost;type='UnitUseAbility';executor=$Rider;createdByPlayer=$true;started=$true;acted=$true;finished=$true;result='Success';ignoreCooldown=$false;commandType='Standard';timeSinceStart=2.0}
  hostileAttackTerminal=$null
  events=@([pscustomobject]@{kind='action-after';identity=$shellIdentity;actor=$Rider;shell=[pscustomobject]@{ignoreCooldown=$false;timeSinceStart=2.0}},[pscustomobject]@{kind='cast-after';actor=$Rider;ability=[pscustomobject]@{blueprint=$Clw};process=20;spellFailed=$false;arcaneFailed=$false},[pscustomobject]@{kind='item-spend-before';identity=60;actor=$Rider;charges=1;count=$before.items[1].count},[pscustomobject]@{kind='item-spend-after';identity=60;actor=$Rider;charges=1;count=$after.items[1].count;result=$true})
  costEvents=@([pscustomobject]@{boundary='cost-before';command=$cost;actionType='Standard';state=$before.rider},[pscustomobject]@{boundary='cost-after';command=$cost;actionType='Standard';state=$after.rider})}
}
function New-RangedStep([int]$index,$before,$after){
 [pscustomobject]@{index=$index;kind='attack-ranged';frame=300;before=$before;weapon=[pscustomobject]@{blueprint='sling';ranged=$true};fullAttackMode=$false;ignoreClick=$false;cursorCycles=0;clicked=$true;inputCount=1;admitted=$null;rejectionCodes=@('MountedRangedUnsupported');rejectionFeedback='Mounted ranged attacks are not supported in this private alpha.';afterInput=$before;after=$after;terminal=$null;hostileAttackTerminal=$null;costEvents=@();events=@()}
}
# Rows with native-shaped numbers: TB additive debt (short leg .7, long leg 2.3), RT absolute debt.
function New-StagedRow([string]$name,[bool]$tb){
 $steps=@();$scrollCount=10;$standard=0.0;$move=0.0;$x=0.0;$usedTwo=$false;$hasMove=$true
 function S([double]$st,[double]$mv,[double]$px,[bool]$hm,[bool]$u2,[int]$sc){New-StagedState $st $mv $px $hm $u2 $sc $Rider}
 $before=S $standard $move $x $hasMove $usedTwo $scrollCount
 $plan=@(Get-KmcChunk6dStagedPlan $name)
 for($i=0;$i-lt$plan.Count;$i++){
  $kind=$plan[$i]
  switch -CaseSensitive($kind){
   'cast-standard-scroll' { $b=S $standard $move $x $hasMove $usedTwo $scrollCount; $standard=if($tb){$standard+6.0}else{4.0}; $scrollCount--; $a=S $standard $move $x $hasMove $usedTwo $scrollCount; $steps+=New-CastStep $i $b $a $tb }
   'attack-ranged' { $b=S $standard $move $x $hasMove $usedTwo $scrollCount; $steps+=New-RangedStep $i $b $b }
   default {
    $legs=@();$legCount=switch -CaseSensitive($kind){'move-two-moves'{if($tb){2}else{2}} 'move-exhaust'{if($tb){3}else{2}} default{1}}
    for($l=0;$l-lt$legCount;$l++){
     $b=S $standard $move $x $hasMove $usedTwo $scrollCount
     $moves=-not$tb-or$hasMove
     if($moves){
      $delta=switch -CaseSensitive($kind){'move-short'{0.7} 'move-extended'{3.0} default{2.3}}
      if($tb){ $move=[Math]::Min(6.0,$move+$delta); $usedTwo=$move-gt3.0; $hasMove=$move-lt6.0 }
      $x+=7.0
     }
     $a=S $standard $move $x $hasMove $usedTwo $scrollCount
     $legs+=New-MoveLeg $l $b $a $moves
    }
    $steps+=New-MoveStep $i $kind $legs
   }
  }
 }
 $after=S $standard $move $x $hasMove $usedTwo $scrollCount
 $events=@($steps|ForEach-Object {@($_.events)}|Where-Object {$_});$costs=@($steps|ForEach-Object {@($_.costEvents)}|Where-Object {$_})
 [pscustomobject]@{name=$name;status='PASS';evidence=[pscustomobject]@{case=$name;plan=$plan;before=$before;rowTurnActor=$Rider;steps=$steps;after=$after;events=$events;costEvents=$costs;ruleEvents=[pscustomobject]@{dropped=0;events=@();attacks=@()};rulesComplete=$true}}
}
function Accept($row,[bool]$tb){Assert-KmcChunk6dStagedRow $row $Rider $Mount $tb $castingLeases $true}
function Reject($row,[string]$why,[bool]$tb=$true){$refused=$false;try{Accept $row $tb}catch{$refused=$true};Check $refused $why}
foreach($tb in @($true,$false)){foreach($name in Get-KmcChunk6dStagedCases){Accept (New-StagedRow $name $tb) $tb;Check $true ('native fact row '+$name+' TB='+$tb)}}
# Movement ownership and conservation.
$r=New-StagedRow 'C6D-move-cast-move' $true;$r.evidence.steps[0].legs[0].carrier.executor=$Rider;Reject $r 'movement carried by the rider instead of the mount is refused'
$r=New-StagedRow 'C6D-move-cast-move' $true;$r.evidence.steps[0].legs[0].after.rider.move=3.0;Reject $r 'rider Move debt charged for mount movement is refused'
$r=New-StagedRow 'C6D-move-cast-move' $true;$r.evidence.steps[0].legs[0].costEvents=@([pscustomobject]@{boundary='cost-after';command=40;actionType='Move';state=[pscustomobject]@{actor=$Rider}});Reject $r 'rider cost event during mount movement is refused'
$r=New-StagedRow 'C6D-move-cast-move' $true;$r.evidence.steps[0].legs[0].after.mount.move=0.0;Reject $r 'TB mount movement without native Move debt is refused'
$r=New-StagedRow 'C6D-move-cast-move' $true;$r.evidence.steps[2].legs[0].after.mount.move=3.5;$r.evidence.after.mount.move=3.5;Reject $r 'two short legs exceeding one Move are refused'
$r=New-StagedRow 'C6D-move-cast-move' $true;$r.evidence.steps[0].legs[0].after.mountPosition.x=0.0;Reject $r 'an admitted leg that did not displace the mount is refused'
$r=New-StagedRow 'C6D-move-cast-move' $true;$r.evidence.steps[0].legs[0].carrier.createdByPlayer=$false;Reject $r 'a non-player carrier is refused'
$r=New-StagedRow 'C6D-move-cast-move' $true;$r.evidence.costEvents=@([pscustomobject]@{boundary='prepare-before';command=0;state=$r.evidence.before.mount});Reject $r 'pair preparation replay inside the row is refused'
# Cast ownership.
$r=New-StagedRow 'C6D-move-cast-move' $true;$r.evidence.steps[1].after.mount.standard=6.0;Reject $r 'mount Standard charged for the rider cast is refused'
$r=New-StagedRow 'C6D-move-cast-move' $true;$r.evidence.steps[1].ability.sourceItem=59;Reject $r 'scroll cast from another entity is refused'
$r=New-StagedRow 'C6D-move-cast-move' $true;$r.evidence.steps[1].after.items[1].count=10;Reject $r 'scroll stack that did not decrement in place is refused'
$r=New-StagedRow 'C6D-move-cast-move' $true;$r.evidence.steps[1].after.rider.standard=3.0;Reject $r 'incorrect additive Standard debt is refused'
$r=New-StagedRow 'C6D-move-cast-move' $false;$r.evidence.steps[1].after.rider.standard=5.0;Reject $r 'incorrect real-time Standard debt is refused' $false
$r=New-StagedRow 'C6D-cast-then-move' $true;$r.evidence.steps[1].legs[0].before.mount.standard=6.0;Reject $r 'movement after the cast governed by a spent mount Standard is refused'
# Budget rows.
$r=New-StagedRow 'C6D-double-move-ranged' $true;$r.evidence.steps[0].legs[-1].after.mountUsedTwoMove=$false;Reject $r 'double move that never reached the native second Move is refused'
$r=New-StagedRow 'C6D-double-move-ranged' $true;$r.evidence.steps[1].admitted=New-Carrier 77;Reject $r 'an admitted mounted stock ranged attack is refused'
$r=New-StagedRow 'C6D-double-move-ranged' $true;$r.evidence.steps[1].rejectionCodes=@('WrongActionState');Reject $r 'a refusal without the exact mounted-ranged code is refused'
$r=New-StagedRow 'C6D-double-move-ranged' $true;$r.evidence.steps[2].before.rider.standard=6.0;$r.evidence.steps[2].before.riderHasStandard=$false;Reject $r 'rider Standard lost after the two Moves is refused'
$r=New-StagedRow 'C6D-movement-exhausted' $true;$r.evidence.steps[1].legs[0].before.mountHasMove=$true;Reject $r 'probe that did not start from native exhaustion is refused'
$r=New-StagedRow 'C6D-movement-exhausted' $true;$r.evidence.steps[1].legs[0].after.mountPosition.x+=2.0;Reject $r 'exhausted mount that still moved is refused'
$r=New-StagedRow 'C6D-movement-exhausted' $true;$r.evidence.steps[0].legs[-1].after.mountHasMove=$true;Reject $r 'exhaust step that left movement is refused'
$r=New-StagedRow 'C6D-auto-stop-boundary' $true;$r.evidence.steps[0].legs[0].after.mount.move=4.0;$r.evidence.steps[0].legs[0].after.mountUsedTwoMove=$true;Reject $r 'extended leg not stopped at the one-Move boundary under auto-stop is refused'
$r=New-StagedRow 'C6D-auto-stop-boundary' $true;$refused=$false;try{Assert-KmcChunk6dStagedRow $r $Rider $Mount $true $castingLeases $false}catch{$refused=$true};Check $refused 'without auto-stop the extended leg must continue into the second Move'
$r=New-StagedRow 'C6D-auto-stop-boundary' $true;$r.evidence.steps[1].admittedShellCount=0;$r.evidence.steps[1].shell=$null;Reject $r 'rider boundary ended before the cast is refused'
$r=New-StagedRow 'C6D-move-cast-move' $true;$r.evidence.plan=@('move-short','move-short');Reject $r 'row plan differing from the registered plan is refused'
$r=New-StagedRow 'C6D-move-cast-move' $true;$r.evidence.after.relationship='Unmounted';Reject $r 'row ending unmounted is refused'
# Registration: launcher, save-backed registry, schema bijection, dispatch, compound registry, handoff.
$launcher=Join-Path $PSScriptRoot 'runtime/Invoke-KingmakerRuntimeScenario.ps1'
$tokens=$null;$errors=$null;$launchAst=[Management.Automation.Language.Parser]::ParseFile($launcher,[ref]$tokens,[ref]$errors)
Check ($errors.Count-eq0) 'actual guarded launcher parses'
$metadata=Get-Command $launcher
$registered=@(($metadata.Parameters['Scenario'].Attributes|Where-Object {$_-is[Management.Automation.ValidateSetAttribute]}).ValidValues)
$bindOnly=[scriptblock]::Create($launchAst.ParamBlock.Extent.Text+"`n'bound-without-launch'")
foreach($scenario in @('chunk6d-staged-rt','chunk6d-staged-tb')){
 Check ($registered-ccontains$scenario) ('actual guarded launcher registers '+$scenario)
 Check ((& $bindOnly -Scenario $scenario -RunId 'c6d-registration-probe')-ceq'bound-without-launch') ('actual native parameter guard admits '+$scenario)
 Check ((Get-KmcSaveBackedRuntimeScenarios)-ccontains$scenario) ('request registry agrees '+$scenario)
 Check (Test-KmcPhase3dSchemaRegistration 45 $scenario) ('exact schema registered '+$scenario)
 Check (-not(Test-KmcPhase3dSchemaRegistration 44 $scenario)) ('casting schema rejected '+$scenario)
 Check ((Get-KmcPhase3dEvidenceDispatch 45 $scenario).validator-ceq'Assert-KmcChunk6dStagedEvidence') ('exact reader selected '+$scenario)
 Check (-not(Test-KmcChildEntryExpectsMounted $scenario)) ('native original-pair exploration handoff '+$scenario)
 Check (Test-KmcCompoundRuntimeScenario $scenario) ('compound root registered '+$scenario)
}
foreach($row in Get-KmcChunk6dStagedCases){Check ((Get-KmcPhase3dHorseRuntimeRows)-ccontains$row) ('shared row registry carries '+$row)}
# Full envelope through the real Phase 3D dispatch, with substantive mutations refused.
$tokens=$null;$parseErrors=$null;$ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'Test-ChildEntryPreamble.ps1'),[ref]$tokens,[ref]$parseErrors)
$definition=@($ast.FindAll({param($n)$n-is[Management.Automation.Language.FunctionDefinitionAst]-and$n.Name-ceq'New-Preamble'},$true));if($parseErrors.Count-ne0-or$definition.Count-ne1){throw 'Existing preamble fixture unavailable'};. ([scriptblock]::Create($definition[0].Extent.Text))
function New-StagedEnvelopeItems($rows){
 $spends=@($rows|ForEach-Object {@($_.evidence.events)}|Where-Object {$_.kind-ceq'item-spend-after'-and$_.identity-eq60-and$_.result-eq$true}).Count
 @([pscustomobject]@{blueprint='55a059b32df920c4abe65b8ee8b56056';requestedCount=1;equipped=[pscustomobject]@{item=62;count=1;exactSlot=$true};beforeCleanup=[pscustomobject]@{item=62;count=1;exactSlot=$true};disposed=$true;slotRestored=$true;noOwnedItemResident=$true},
   [pscustomobject]@{blueprint=$Potion;requestedCount=2;equipped=[pscustomobject]@{item=61;count=1;exactSlot=$true};stacked=[pscustomobject]@{item=61;count=2;exactSlot=$true};beforeCleanup=[pscustomobject]@{item=61;count=2;exactSlot=$true};disposed=$true;slotRestored=$true;noOwnedItemResident=$true},
   [pscustomobject]@{blueprint=$Scroll;requestedCount=10;equipped=[pscustomobject]@{item=60;count=1;exactSlot=$true};stacked=[pscustomobject]@{item=60;count=10;exactSlot=$true};beforeCleanup=[pscustomobject]@{item=60;count=(10-$spends);exactSlot=$true};disposed=$true;slotRestored=$true;noOwnedItemResident=$true})
}
$root=Join-Path (Get-KmcRepositoryRoot) ('obj/chunk6d-envelope/'+[Guid]::NewGuid().ToString('N'))
foreach($scenario in @('chunk6d-staged-rt','chunk6d-staged-tb')){
 $tb=$scenario.EndsWith('-tb');$rows=@(Get-KmcChunk6dStagedCases|ForEach-Object {New-StagedRow $_ $tb})
 $pre=New-Preamble $false;$pre.childScenario=$scenario;$pre.parentScenario=$scenario;$pre.parentEngine='KingmakerMountedCombat.Diagnostics.Chunk6aMammothScenarioEngine';$pre.runId='staged-envelope'
 $trace=[pscustomobject]@{faults=0;dropped=0;observationErrors=0;identityRegistry=[pscustomobject]@{faults=0;released=$true;retainedCount=0}}
 $plans=[pscustomobject]@{};foreach($c in Get-KmcChunk6dStagedCases){$plans|Add-Member $c @(Get-KmcChunk6dStagedPlan $c)}
 $artifact=[pscustomobject]@{schemaVersion=45;evidenceKind='phase3d-horse-scenario-evidence';runId='staged-envelope';scenario=$scenario;branch='codex/mounted-combat-phase3f-playable-core';commit=('a'*40);productVersion='0.1.0-chunk6d-preview.203';dllSha256=('b'*64);dllMvid='00000000-0000-0000-0000-000000000001';createdAtUtc='2026-10-09T00:00:00Z';status='PASS';rows=$rows;subscenarioPassCount=$rows.Count;subscenarioFailCount=0;errors=@();observations=[pscustomobject]@{riderId=$Rider;horseId=$Mount;childEntryPreamble=$pre;chunk6dStaged=[pscustomobject]@{contract='native-mounted-staged-actions-v1';rider=$Rider;mount=$Mount;mountBlueprint='e7aa96d15a45238438ae4cfb476f6bb9';mode=$(if($tb){'TB'}else{'RT'});mounted=$true;cases=@(Get-KmcChunk6dStagedCases);plans=$plans;autoStopAfterFirstMoveAction=$true;rangedWeapon=[pscustomobject]@{blueprint='sling';ranged=$true;category='Sling'};rangedWeaponReleased=$true;castTrace=$trace;costTrace=$trace;ruleTrace=[pscustomobject]@{dropped=0};items=(New-StagedEnvelopeItems $rows);summonCleanup=@();final=$rows[0].evidence.after}}}
 $request=@{runId=$artifact.runId;scenario=$scenario;branch=$artifact.branch;commit=$artifact.commit;productVersion=$artifact.productVersion;dllSha256=$artifact.dllSha256;dllMvid=$artifact.dllMvid;evidenceRoot=(Join-Path $root $scenario)};[void][IO.Directory]::CreateDirectory($request.evidenceRoot);$path=Join-Path $request.evidenceRoot 'phase3d-horse-scenario-evidence.json';$manifest=@{artifacts=@(@{relativePath='phase3d-horse-scenario-evidence.json';kind='phase3d-horse-scenario-evidence'})}
 function Envelope($value){$value|ConvertTo-Json -Depth 70|Set-Content -LiteralPath $path -Encoding UTF8;Assert-KmcPhase3dHorseScenarioEvidence $request $manifest 'PASS'}
 Envelope $artifact;Check $true ('complete current envelope '+$scenario)
 foreach($mutation in @({param($a)$a.schemaVersion=44},{param($a)$a.rows=@($a.rows|Select-Object -Skip 1);$a.subscenarioPassCount--},{param($a)$a.observations.childEntryPreamble.parentEngine='KingmakerMountedCombat.Diagnostics.HorseCompanionUnmountedScenarioEngine'},{param($a)$a.observations.chunk6dStaged.costTrace.dropped=1},{param($a)$a.rows[0].evidence.steps[0].legs[0].carrier.executor='rider'},{param($a)$a.observations.chunk6dStaged.rangedWeaponReleased=$false},{param($a)$a.observations.chunk6dStaged.items[2].beforeCleanup.count=9},{param($a)$a.observations.chunk6dStaged.plans.'C6D-cast-then-move'=@('move-long')})){
  $changed=Copy-Staged $artifact;& $mutation $changed;$rejected=$false;try{Envelope $changed}catch{$rejected=$true};Check $rejected ('full envelope retains substantive refusal '+$scenario)
 }
 Envelope $artifact
}
Write-Host ('TOTAL PASS='+$passed+' FAIL=0')
