param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'));$lab=[IO.Path]::GetFullPath((Join-Path $repo '../..'))
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'runtime/Chunk6aPrimaryClaimEvidence.ps1')
. (Join-Path $PSScriptRoot 'runtime/Chunk6aSupportingEvidence.ps1')
function Copy-Eligibility($Value){$Value|ConvertTo-Json -Depth 100|ConvertFrom-Json}
$original=Join-Path $lab 'runtime-evidence/c6a-envelope-a-obstruction/phase3d-horse-scenario-evidence.json';$originalHash=(Get-FileHash $original).Hash
$artifact=Get-Content -Raw $original|ConvertFrom-Json
function New-EligibilityProof([string]$Contract){
 $proof=Copy-Eligibility $artifact.observations.chunk6aObstruction.commandProof;$proof.contract=$Contract;$terminal=$proof.samples[-1]
 $start=Copy-Eligibility $proof.preClick.state.geometry;$geometry=Copy-Eligibility $start
 $dx=[double]$geometry.horsePosition.x-[double]$geometry.riderPosition.x;$dz=[double]$geometry.horsePosition.z-[double]$geometry.riderPosition.z;$length=[Math]::Sqrt($dx*$dx+$dz*$dz)
 $geometry.riderPosition.x=[double]$geometry.riderPosition.x+0.5*$dx/$length;$geometry.riderPosition.z=[double]$geometry.riderPosition.z+0.5*$dz/$length
 $geometry.horizontalDistance=$length-0.5;$geometry.centerDistance=$length-0.5;$terminal.state.geometry=Copy-Eligibility $geometry
 [pscustomobject]@{proof=$proof;start=$start;geometry=$geometry;terminal=$terminal;trigger=[pscustomobject]@{approachObserved=$true;riderReallyMoving=$true;gameTicks=$terminal.gameTicks;frame=$terminal.frame;commandObject=$proof.identity.commandObject;moveSlotObject=$proof.identity.commandObject;started=$false;acted=$false;finished=$false;geometry=$geometry;riderDisplacement=0.5}}
}
function New-Command($Proof,[bool]$Terminal){[pscustomobject]@{id=$Proof.identity.commandObject;type='Kingmaker.UnitLogic.Commands.UnitUseAbility';executor=$Proof.identity.casterId;started=$false;acted=$false;finished=$Terminal;result=$(if($Terminal){$Proof.nativeResult}else{'None'})}}

$sizeBase=New-EligibilityProof 'unacted-native-size-form-change-no-cost-or-transition';$sizeProof=$sizeBase.proof;$sizeCommand=New-Command $sizeProof $false;$buffObject=501
$sizeBefore=[pscustomobject]@{riderId=$sizeProof.identity.casterId;mountId=$sizeProof.identity.targetId;buffName='EnlargePersonBuff';buffGuid='11111111111111111111111111111111';changeUnitSizeComponents=1;buffCount=0;buffObjects=@();riderSize=4;mountSize=5;riderPolymorphObject=0;mountPolymorphObject=0}
$sizeActive=Copy-Eligibility $sizeBefore;$sizeActive.buffCount=1;$sizeActive.buffObjects=@($buffObject);$sizeActive.riderSize=5
$sizeCase=[pscustomobject]@{contract='native-size-fact-invalidates-exact-mount-approach';restored=$true;noResidue=$true;stimulusCount=1;removalCount=1;diagnosticInterruptCount=0;
 start=$sizeBase.start;trigger=$sizeBase.trigger;commandProof=$sizeProof;terminal=(New-Command $sizeProof $true);stateBefore=$sizeBefore;stateAfterApplication=(Copy-Eligibility $sizeActive);stateBeforeRemoval=(Copy-Eligibility $sizeActive);stateAfterRestoration=(Copy-Eligibility $sizeBefore);
 stimulus=[pscustomobject]@{contract='one-authored-enlarge-person-buff-through-native-rulebook';count=1;buffObject=$buffObject;before=(Copy-Eligibility $sizeBefore);after=(Copy-Eligibility $sizeActive);commandBefore=(Copy-Eligibility $sizeCommand);commandAfter=(Copy-Eligibility $sizeCommand);frameBefore=$sizeBase.terminal.frame;frameAfter=$sizeBase.terminal.frame;gameTicksBefore=$sizeBase.terminal.gameTicks;gameTicksAfter=$sizeBase.terminal.gameTicks};
 removal=[pscustomobject]@{contract='remove-only-exact-owned-native-buff-after-command-terminal';count=1;buffObject=$buffObject;frame=$sizeBase.terminal.frame;gameTicks=$sizeBase.terminal.gameTicks;after=(Copy-Eligibility $sizeBefore)}}

$controlBase=New-EligibilityProof 'unacted-native-lost-direct-control-no-cost-or-transition';$controlProof=$controlBase.proof;$controlCommand=New-Command $controlProof $false
function New-ControlState([bool]$Direct,[bool]$Panicked,[bool]$Frightened){[pscustomobject]@{riderId=$controlProof.identity.casterId;mountId=$controlProof.identity.targetId;directlyControllable=$Direct;inGame=$true;panicked=$Panicked;frightened=$Frightened;frightenedImmune=$false;visibleConsciousEnemies=1;visibleEnemyIds=@('enemy-1')}}
function New-Lease([string]$Phase){
 $immediate=$Phase-ceq'immediate';$lost=$Phase-ceq'lost'
 [pscustomobject]@{contract='owned-native-frightened-fact-awaits-unit-fear-controller';actorId=$controlProof.identity.casterId;blueprintId='22222222222222222222222222222222';templateId='33333333333333333333333333333333';factObject=601;templateComponentObject=602;activeComponentObject=603;componentType='Kingmaker.UnitLogic.FactLogic.AddCondition';condition='Frightened';activeFactCount=$(if($Phase-ceq'removed'){0}else{1});conditionActive=$($Phase-cne'removed');conditionImmune=$false;panicked=$(-not$immediate);directlyControllable=$immediate;onTurnOnToken='06002448';onTurnOffToken='06002449';moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7';disposed=$($Phase-ceq'removed')}
}
$lossBefore=[pscustomobject]@{conditionActive=$true;panicked=$false;directlyControllable=$true;commandObject=$controlProof.identity.commandObject;commandStarted=$false;commandActed=$false;commandFinished=$false;commandResult='None';commandProcessObject=0;moveSlotObject=$controlProof.identity.commandObject;moveSlotType='Kingmaker.UnitLogic.Commands.UnitUseAbility';commandsEmpty=$false;visibleConsciousEnemies=@('enemy-1')}
$lossAfter=Copy-Eligibility $lossBefore;$lossAfter.panicked=$true;$lossAfter.directlyControllable=$false;$lossAfter.commandFinished=$true;$lossAfter.commandResult=$controlProof.nativeResult;$lossAfter.moveSlotObject=777;$lossAfter.moveSlotType='Kingmaker.UnitLogic.Commands.UnitMoveTo'
$restoreBefore=Copy-Eligibility $lossAfter;$restoreBefore.conditionActive=$false
$restoreAfter=Copy-Eligibility $restoreBefore;$restoreAfter.panicked=$false;$restoreAfter.directlyControllable=$true;$restoreAfter.moveSlotObject=0;$restoreAfter.moveSlotType=$null;$restoreAfter.commandsEmpty=$true
$fearProbe=[pscustomobject]@{contract='installed-unit-fear-controller-removes-and-restores-direct-control';riderId=$controlProof.identity.casterId;commandObject=$controlProof.identity.commandObject;complete=$true;lossObserved=$true;restorationObserved=$true;lossCount=1;restorationCount=1;errors=@();observerHooks=@([pscustomobject]@{method='Kingmaker.Controllers.Units.UnitFearController.TickOnUnit';token='06009138';moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7';prefix='Before';postfix='After'});events=@([pscustomobject]@{ordinal=1;before=$lossBefore;after=$lossAfter;lossTransition=$true;restorationTransition=$false},[pscustomobject]@{ordinal=2;before=$restoreBefore;after=$restoreAfter;lossTransition=$false;restorationTransition=$true})}
$controlBefore=New-ControlState $true $false $false;$controlImmediate=New-ControlState $true $false $true;$controlLost=New-ControlState $false $true $true;$controlRemoved=New-ControlState $false $true $false;$controlRestored=New-ControlState $true $false $false
$controlCase=[pscustomobject]@{contract='native-fear-controller-invalidates-exact-mount-approach';restored=$true;noResidue=$true;stimulusCount=1;removalCount=1;diagnosticInterruptCount=0;
 start=$controlBase.start;trigger=$controlBase.trigger;commandProof=$controlProof;terminal=(New-Command $controlProof $true);stateBefore=$controlBefore;stateAtControlLoss=$controlLost;leaseBeforeRemoval=(New-Lease 'lost');leaseAfterRemoval=(New-Lease 'removed');stateAfterFactRemoval=$controlRemoved;fearController=$fearProbe;
 stimulus=[pscustomobject]@{contract='one-owned-native-frightened-fact';count=1;commandBefore=(Copy-Eligibility $controlCommand);lease=(New-Lease 'immediate');immediateState=$controlImmediate;frame=$controlBase.terminal.frame;gameTicks=$controlBase.terminal.gameTicks};
 restoration=[pscustomobject]@{contract='native-fear-controller-restores-control-after-owned-fact-removal';removalCount=1;state=$controlRestored;allocationEvents=@();noCostOrPreparationCallbacks=$true;frame=$controlBase.terminal.frame;gameTicks=$controlBase.terminal.gameTicks}}

$script:checks=0
function Reject-Size([scriptblock]$Mutate,[string]$Reason){$copy=Copy-Eligibility $sizeCase;&$Mutate $copy;$rejected=$false;try{Assert-KmcSizeFormChange $copy}catch{if($_.Exception.Message.IndexOf($Reason,[StringComparison]::OrdinalIgnoreCase)-lt0){throw};$rejected=$true};if(-not$rejected){throw 'Invalid size/form proof admitted'};$script:checks++}
function Reject-Control([scriptblock]$Mutate,[string]$Reason){$copy=Copy-Eligibility $controlCase;&$Mutate $copy;$rejected=$false;try{Assert-KmcLostDirectControl $copy}catch{if($_.Exception.Message.IndexOf($Reason,[StringComparison]::OrdinalIgnoreCase)-lt0){throw};$rejected=$true};if(-not$rejected){throw 'Invalid direct-control proof admitted'};$script:checks++}
Assert-KmcSizeFormChange $sizeCase;$script:checks++
Assert-KmcLostDirectControl $controlCase;$script:checks++

function Assert-Envelope([string]$Scenario,[string]$Row,[string]$Observation,$Case){
 $envelope=Copy-Eligibility $artifact
 $envelope.rows=@($envelope.rows|Where-Object {$_.name-cne'CM02-obstruction'})+@([pscustomobject]@{name=$Row;status='PASS';assertionPassCount=1;assertionFailCount=0;errors=@()})
 [void]$envelope.observations.PSObject.Properties.Remove('chunk6aDoorFixture');[void]$envelope.observations.PSObject.Properties.Remove('chunk6aObstruction')
 $envelope.observations|Add-Member -NotePropertyName $Observation -NotePropertyValue (Copy-Eligibility $Case) -Force
 Assert-KmcChunk6aCombatMountEvidence ([pscustomobject]@{scenario=$Scenario}) $envelope 'PASS';$script:checks++
}
Assert-Envelope 'chunk6a-size-form-change' 'CM02-size-form-change' 'chunk6aSizeFormChange' $sizeCase
Assert-Envelope 'chunk6a-lost-direct-control' 'CM02-lost-direct-control' 'chunk6aLostDirectControl' $controlCase

Reject-Size {param($c)$c.commandProof.resourceWindow.reactionResources.pass=$false} 'Reaction resource contract'
Reject-Control {param($c)$c.commandProof.samples[-1].state.rider.reactionCooldown=[double]$c.commandProof.samples[-1].state.rider.reactionCooldown+0.5} 'reaction allowance or cooldown'
Reject-Size {param($c)$c.stimulus.after.riderSize=4} 'equal-or-larger'
Reject-Size {param($c)$c.stimulus.after.changeUnitSizeComponents=2} 'identity differs'
Reject-Size {param($c)$c.removal.buffObject++} 'exact terminal command'
Reject-Size {param($c)$c.commandProof.samples[3].identity.commandObject++} 'Mixed eligibility command identity'
Reject-Control {param($c)$c.stimulus.lease.onTurnOnToken='06000000'} 'Fact identity differs'
Reject-Control {param($c)$c.stateAtControlLoss.directlyControllable=$true} 'did not remove direct control'
Reject-Control {param($c)$c.fearController.observerHooks[0].token='06000000'} 'hook identity differs'
Reject-Control {param($c)$c.fearController.events[0].after.commandFinished=$false} 'loss boundary differs'
Reject-Control {param($c)$c.restoration.state.panicked=$true} 'did not restore'
Reject-Control {param($c)$c.restoration.allocationEvents=@([pscustomobject]@{boundary='cost-before'})} 'native cost'

foreach($case in @(@('CM02-size-form-change','chunk6a-size-form-change'),@('CM02-lost-direct-control','chunk6a-lost-direct-control'))){
 if(@(Get-KmcSaveBackedRuntimeScenarios|Where-Object {$_-ceq$case[1]}).Count-ne1){throw 'Eligibility scenario registration missing or duplicate'};$script:checks++
 if(@(Get-KmcPhase3dHorseRuntimeRows|Where-Object {$_-ceq$case[0]}).Count-ne1){throw 'Eligibility row registration missing or duplicate'};$script:checks++
 $binding=[pscustomobject]@{scenario=$case[1];rows=@($case[0]);evidenceLeaf='phase3d-horse-scenario-evidence.json'}
 if(-not(Assert-KmcIsolatedScenarioRows $case[0] $binding)){throw 'Eligibility isolated mapping missing'};$script:checks++
 Assert-KmcChunk6aPrimaryClaim $case[0] $binding;$script:checks++
}
$source=Get-Content -Raw (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk6aEligibilityChangeScenario.cs')
$sizeSource=$source.Substring($source.IndexOf('private void TickChunk6aSizeFormChange()'),$source.IndexOf('private void TickChunk6aLostDirectControl()')-$source.IndexOf('private void TickChunk6aSizeFormChange()'))
$controlSource=$source.Substring($source.IndexOf('private void TickChunk6aLostDirectControl()'),$source.IndexOf('private JObject CaptureChunk6aSizeState')-$source.IndexOf('private void TickChunk6aLostDirectControl()'))
foreach($pair in @(@($sizeSource,'Chunk6aSizeFormRow','chunk6aSizeOriginal = CaptureChunk6aSizeState','chunk6a-size-form-change-click'),@($controlSource,'Chunk6aLostDirectControlRow','chunk6aControlOriginal = CaptureChunk6aDirectControlState','chunk6a-lost-direct-control-click'))){
 $selection=$pair[0].IndexOf('EnsureChunk6aRiderSelection('+$pair[1]+')');$baseline=$pair[0].IndexOf($pair[2]);$probe=$pair[0].IndexOf('new NativeRelationshipCommandProbe');$click=$pair[0].IndexOf($pair[3])
 if($selection-lt0-or$selection-ge$baseline-or$baseline-ge$probe-or$probe-ge$click){throw 'Eligibility selection/baseline/click order differs'};$script:checks++
}
if([regex]::Matches($sizeSource,'rider[.]Buffs[.]AddBuff[(]').Count-ne1-or$sizeSource.IndexOf('FinishUnacted("unacted-native-size-form-change-no-cost-or-transition")')-gt$sizeSource.IndexOf('chunk6aSizeBuff.Remove()')){throw 'Size native stimulus/removal order differs'};$script:checks++
if($controlSource.IndexOf('new NativeFearControlProbe')-gt$controlSource.IndexOf('new NativePendingMountFearLease')-or$controlSource.IndexOf('FinishUnacted("unacted-native-lost-direct-control-no-cost-or-transition")')-gt$controlSource.IndexOf('chunk6aFearLease.Dispose()')){throw 'Fear observer/stimulus/removal order differs'};$script:checks++
$fearSource=Get-Content -Raw (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/NativePendingMountFear.cs')
foreach($forbidden in @('IsPanicked =','Cooldowns.Clear','TurnController.Prepare','EndTurn','StandardAction =','MoveAction =','SwiftAction =')){if($fearSource.Contains($forbidden)){throw "Fear fixture writes forbidden production state: $forbidden"}};$script:checks++
foreach($pin in @('0x06009138','OnTurnOn','OnTurnOff','UnitCondition.Frightened')){if(-not$fearSource.Contains($pin)){throw "Fear fixture omitted exact native pin: $pin"}};$script:checks++
if((Get-FileHash $original).Hash-cne$originalHash){throw 'Original obstruction artifact changed'}
Write-Host "ELIGIBILITY PASS=$script:checks FAIL=0; synthetic/source contracts only, no native qualification."