[CmdletBinding()]
param()
# Synthetic acceptance/refusal regression for the Chunk 6 closeout readers (scripts/runtime/Chunk6aClosureEvidence.ps1):
# the seven approach invalidations, the two turn-based boundaries, the four P04 closure checkpoints, the Chunk 5
# area-reload restoration identity and the accepted save schema, plus the ledger row projection and binding
# helpers. Never launches the game; every fixture lives in an owned test lab under obj/ and is retained.
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'runtime/PersistenceSaveFixtures.ps1')
. (Join-Path $PSScriptRoot 'runtime/Chunk6aSupportingEvidence.ps1')
. (Join-Path $PSScriptRoot 'runtime/Chunk6aArtifactRowsEvidence.ps1')
. (Join-Path $PSScriptRoot 'runtime/Chunk6aClosureEvidence.ps1')
$script:ownedTestLab=Join-Path ([IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))) ('obj/closure-reader-tests/'+[Guid]::NewGuid().ToString('N'))
function Get-KmcLabRoot { return $script:ownedTestLab }
[void][IO.Directory]::CreateDirectory($script:ownedTestLab)
$script:checks=0
function Accept([string]$Name,[scriptblock]$Action){ & $Action | Out-Null; $script:checks++ }
function Reject([string]$Name,[scriptblock]$Action){
 $refused=$false;$message=''
 try{ & $Action | Out-Null }catch{ $refused=$true; $message=$_.Exception.Message }
 if(-not$refused){throw ('Unexpected acceptance: '+$Name)}
 if($message -cnotlike 'Chunk 6A closure:*'){throw ('Refusal of '+$Name+' came from outside the closure reader: '+$message)}
 $script:checks++
}
function J($Value){ $parsed=$Value | ConvertTo-Json -Depth 100 | ConvertFrom-Json; if($parsed -is [Array]){ $parsed | ForEach-Object {$_} } else { $parsed } }
$riderId='b6628a77-4962-47a4-a17c-88d9836fc9d5';$mountId='d79a4f6c-b74e-4868-95bd-533899131acb';$targetId='c8fa7500-1ec8-4b71-8642-b620d5cc8034'
$mountGuid='f053faad986631688defa003cd7bda0e';$commit=('c'*40);$dll=('d'*64);$gameId='89c49a86-a171-4ea9-871a-f6b3b53d19b9';$area=('a'*32)

# ---------------------------------------------------------------------------------------------
# Schema-30 lifecycle state fixtures (the compiled CaptureChunk6aLifecycleState and its invalidation/turn-based wrappers).
# ---------------------------------------------------------------------------------------------
function Cool([string]$Id){ @{actor=$Id;standard=0.0;move=0.0;swift=0.0} }
function Ledger([int]$Admitted,[int]$Accepted,[int]$Refused,[int]$Forced,[int]$Dup){ @{admittedMount=$Admitted;acceptedMount=$Accepted;admittedDismount=0;acceptedDismount=0;refusedVoluntary=$Refused;forcedDetach=$Forced;duplicateSuppressed=$Dup;concurrentSuppressed=0} }
function Actor([string]$Id,[int]$View,[hashtable]$Over=@{}){ $a=@{id=$Id;inGame=$true;inState=$true;viewPresent=$true;viewObject=$View;viewActive=$true;viewBound=$true;agentPresent=$true;agentEnabled=$true;agentOverride=0;groupId='g';directlyControllable=$true;conscious=$true;dead=$false;finallyDead=$false;damage=0;hitPoints=30;polymorphed=$false;commandsEmpty=$true;reallyMoving=$false;cooldowns=(Cool $Id)}; foreach($k in $Over.Keys){$a[$k]=$Over[$k]}; $a }
function Cmd([int]$Id,[bool]$Started,[bool]$Acted,[bool]$Finished,[string]$Result){ @{id=$Id;type='Kingmaker.UnitLogic.Commands.UnitUseAbility';executor=$riderId;started=$Started;acted=$Acted;finished=$Finished;result=$Result} }
function Life([string]$Kind,[hashtable]$Over=@{}){
 $s=@{kind=$Kind;frame=100;seconds=1.0;gameTicks=1000;partyInCombat=$true;riderInCombat=$true;horseInCombat=$true;targetPresent=$true;targetInCombat=$true;relationshipState='Unmounted';generation=1;transitionInFlight=$false;transitionLedger='';ledger=(Ledger 0 0 0 0 0);relationshipShells=1;shellState=$null;dispatchAccepted=0;dispatchRejected=0;castRequests=1;pairedIdentity=$null;partnerContextActor=$null;adoptionCount=0;adoptionRollbacks=0;compensatedMounts=0;riderCommandsEmpty=$true;horseCommandsEmpty=$true;riderReallyMoving=$false;controls=@{};rider=(Cool $riderId);mount=(Cool $mountId);command=$null;allocationSequence=0;combatFeedback=$null;playerActionFeedback=$null}
 foreach($k in $Over.Keys){$s[$k]=$Over[$k]}; $s
}
function Inv([string]$Kind,[hashtable]$Over=@{},[hashtable]$RiderOver=@{},[hashtable]$MountOver=@{}){
 $s=Life $Kind
 $s['currentMode']='Default';$s['cutsceneLock']=$false;$s['loadingInProcess']=$false;$s['riderActor']=(Actor $riderId 11 $RiderOver);$s['mountActor']=(Actor $mountId 22 $MountOver);$s['lastShellRefusal']=$null;$s['diagnosticGenerationInvalidations']=0;$s['mountExecutionFaultArmed']=$false;$s['generationFaultArmed']=$false
 foreach($k in $Over.Keys){$s[$k]=$Over[$k]}; $s
}
function Tb([string]$Kind,[hashtable]$Over=@{}){
 $s=Life $Kind
 $s['turnBasedSetting']=$true;$s['turnBasedCombat']=$true;$s['controllerInitialized']=$true;$s['round']=2;$s['turnObject']=5001;$s['turnActor']=$riderId;$s['turnStatus']='Acting';$s['turnIsActing']=$true;$s['turnCanEnd']=$true;$s['waitingForUi']=$false;$s['pairedSequence']=0;$s['pairedSplit']=$false;$s['pairedFinalized']=$false;$s['lifetimeRetirements']=0;$s['lifetimeRetirementsDeferred']=0;$s['lastLifetimeRetirement']='none';$s['exitAiLeaseArmed']=0;$s['exitAiLeaseAttempts']=0;$s['exitAiLeaseMutations']=0
 foreach($k in $Over.Keys){$s[$k]=$Over[$k]}; $s
}
$click=@{abilityGuid=$mountGuid;clickedTargetId=$mountId;resolvedTargetId=$mountId;priority='Normal';clicked=$true;targetSelectionStartDelta=0;targetSelectionEndDelta=0;nativeCastRequestDelta=1;nativeRefusalDelta=0;dispatchAcceptedDelta=0;dispatchRejectedDelta=0}
function Trigger([hashtable]$State){ $t=@{frame=120;gameTicks=1200;approachObserved=$true;riderReallyMoving=$true;riderDisplacement=0.6;commandObject=777;moveSlotObject=777;started=$false;acted=$false;finished=$false;geometry=@{boundary='trigger';isAdjacent=$false}}; if($null-ne$State){$t['state']=$State}; $t }
function Stimulus([hashtable]$Over,[hashtable]$RiderAfter=@{},[hashtable]$MountAfter=@{}){
 $s=@{frameBefore=120;gameTicksBefore=1200;commandBefore=(Cmd 777 $false $false $false 'None');riderBefore=(Actor $riderId 11);mountBefore=(Actor $mountId 22);count=1;frameAfter=120;gameTicksAfter=1200;commandAfter=(Cmd 777 $false $false $false 'None');riderAfter=(Actor $riderId 11 $RiderAfter);mountAfter=(Actor $mountId 22 $MountAfter)}
 foreach($k in $Over.Keys){$s[$k]=$Over[$k]}; $s
}
function New-InvalidationCase([string]$Scenario){
 $row=Get-KmcChunk6aInvalidationRow $Scenario;$contract=Get-KmcChunk6aInvalidationContract $Scenario
 $diagnostic=$Scenario-cin@('chunk6a-generation-change','chunk6a-injected-exception');$death=$Scenario-cin@('chunk6a-rider-death-approach','chunk6a-mount-death-approach')
 $c=@{contract=$contract;scenario=$Scenario;row=$row;diagnosticStimulus=$diagnostic;externalRestorationRequired=$death;start=@{boundary='invalidation-pre-click';isAdjacent=$false;riderPosition=@{x=0.0;y=0.0;z=0.0}};before=(Inv 'before-click');samples=@();stimulusCount=1;restorationCount=1;diagnosticInterruptCount=0;click=$click;afterClick=(Inv 'after-click');trigger=(Trigger (Inv 'trigger'));terminal=(Inv 'terminal');terminalCommand=(Cmd 777 $false $false $true 'Interrupt');commandWindow=$null;after=(Inv 'after');allocationEvents=@();allocationTraceComplete=$true;observerHooks=@();interrupts=@();pairCostCallbacks=0;pairPrepareCallbacks=0;ledgerDelta=(Ledger 0 0 0 0 0);generationDelta=0;dispatchAcceptedDelta=0;dispatchRejectedDelta=0;castRequestDelta=1;relationshipShellsDelta=1;noResidue=$true;outcome='unacted-Interrupt';settledFrames=10;feedback=$null;lastShellRefusal=$null;diagnosticGenerationInvalidations=0;lastDiagnosticGenerationInvalidation=$null;restoration=@{cleanup=$false;attempts=1;frameFirst=200;restored=$true;frameLast=200;riderAfter=(Actor $riderId 11);mountAfter=(Actor $mountId 22)}}
 switch -CaseSensitive ($Scenario){
  'chunk6a-left-area' { $c['stimulus']=Stimulus @{contract='one-native-entity-is-in-game-false-on-the-exact-mount';method='Kingmaker.EntitySystem.EntityDataBase.set_IsInGame';token='06007E95'} @{} @{inGame=$false} }
  'chunk6a-view-agent-lost' {
   $c['stimulus']=Stimulus @{contract='one-authored-beast-shape-view-replacement-on-the-exact-rider';lease=@{actor=$riderId;blueprint='00d8fbe9cf61dc24298be8d95500c84b';applyCalls=1;before=@{view=11};afterApply=@{view=33}}} @{polymorphed=$true;viewObject=33} @{}
   $c.restoration['lease']=@{restored=$true;removeCalls=1;originalRetired=$true;replacementRetired=$true;factDisposed=$true;listenersRemaining=0}
  }
  'chunk6a-loading-cutscene' {
   $c['stimulus']=Stimulus @{contract='one-native-cutscene-game-mode-start';method='Kingmaker.Game.StartMode';token='06000CBD';modeBefore='Default';modeAfterRequest='Cutscene'}
   $c['samples']=@((Inv 'tick' @{currentMode='Cutscene'}))
   $c.restoration['method']='Kingmaker.Game.StopMode';$c.restoration['token']='06000CBE';$c.restoration['modeNow']='Default'
  }
  'chunk6a-generation-change' {
   $c['stimulus']=Stimulus @{contract='one-owned-diagnostic-generation-invalidation-before-the-shell-generation-check';generationBefore=1;armed=$true}
   $c['outcome']='acted-not-mounted';$c['terminalCommand']=Cmd 777 $true $true $true 'Success';$c['generationDelta']=1;$c['dispatchRejectedDelta']=1;$c['diagnosticGenerationInvalidations']=1
   $c['lastShellRefusal']='The mounted relationship changed after this transition was requested.';$c.restoration['armedAtRestoration']=$false
   $c['after']=Inv 'after' @{generation=2;dispatchRejected=1;lastShellRefusal='The mounted relationship changed after this transition was requested.';diagnosticGenerationInvalidations=1}
  }
  'chunk6a-injected-exception' {
   $c['stimulus']=Stimulus @{contract='one-owned-diagnostic-exception-at-the-native-mount-execution-boundary';armed=$true}
   $c['outcome']='acted-not-mounted';$c['terminalCommand']=Cmd 777 $true $true $true 'Success';$c['ledgerDelta']=Ledger 1 0 1 1 0;$c['feedback']='Native Mount Companion failed closed: InvalidOperationException.';$c.restoration['armedAtRestoration']=$false
   $c['after']=Inv 'after' @{ledger=(Ledger 1 0 1 1 0);playerActionFeedback='Native Mount Companion failed closed: InvalidOperationException.'}
  }
  default {
   $subject=if($Scenario-ceq'chunk6a-rider-death-approach'){'riderActor'}else{'mountActor'};$subjectId=if($subject-ceq'riderActor'){$riderId}else{$mountId}
   $c['stimulus']=Stimulus @{contract='one-native-lethal-ruledeal-damage-under-permanent-death-policy';policy=@{permanentDeathFixture=$true;before=@{};effective=@{trueDeath=$true;deathDoorCondition=$false};restoration=$null};subjectId=$subjectId;sourceId=$targetId;damageToParty=1.0;deathThreshold=39;requestedDamage=41;damageDispatches=1;nativeDamage=41;nativeDamageBeforeDifficulty=41}
   $c['ledgerDelta']=Ledger 0 0 0 1 1
   $c['after']=if($subject-ceq'riderActor'){Inv 'after' @{ledger=(Ledger 0 0 0 1 1)} @{dead=$true;conscious=$false;finallyDead=$true} @{}}else{Inv 'after' @{ledger=(Ledger 0 0 0 1 1)} @{} @{dead=$true;conscious=$false;finallyDead=$true}}
   $c['restoration']=@{cleanup=$false;attempts=1;frameFirst=200;restored=$true;frameLast=200;nativeLifeEvents=@();policy=@{permanentDeathFixture=$true;before=@{};effective=@{trueDeath=$true};restoration=@{restored=$true;state=@{}}};subjectDead=$true;riderAfter=(Actor $riderId 11);mountAfter=(Actor $mountId 22)}
  }
 }
 $c
}
function New-InvalidationArtifact([string]$Scenario,[scriptblock]$Mutate=$null){
 $case=New-InvalidationCase $Scenario
 if($null-ne$Mutate){ & $Mutate $case }
 J @{schemaVersion=30;productVersion='0.1.0-chunk6d-preview.206';observations=@{chunk6aApproachInvalidation=$case};rows=@(@{name=(Get-KmcChunk6aInvalidationRow $Scenario);status='PASS';evidence=$case})}
}
foreach($scenario in @(Get-KmcChunk6aInvalidationScenarios)){
 Accept ($scenario+' accepted') { Assert-KmcChunk6aApproachInvalidation $scenario (New-InvalidationArtifact $scenario) }
 Reject ($scenario+' delivered') { Assert-KmcChunk6aApproachInvalidation $scenario (New-InvalidationArtifact $scenario { param($c) $c.after.relationshipState='Mounted';$c.outcome='delivered';$c.terminalCommand.acted=$true }) }
 Reject ($scenario+' residue') { Assert-KmcChunk6aApproachInvalidation $scenario (New-InvalidationArtifact $scenario { param($c) $c.after.riderCommandsEmpty=$false }) }
 Reject ($scenario+' paired residue') { Assert-KmcChunk6aApproachInvalidation $scenario (New-InvalidationArtifact $scenario { param($c) $c.after.pairedIdentity='enc:1' }) }
 Reject ($scenario+' second shell') { Assert-KmcChunk6aApproachInvalidation $scenario (New-InvalidationArtifact $scenario { param($c) $c.relationshipShellsDelta=2 }) }
 Reject ($scenario+' repeated stimulus') { Assert-KmcChunk6aApproachInvalidation $scenario (New-InvalidationArtifact $scenario { param($c) $c.stimulusCount=2;$c.stimulus.count=2 }) }
 Reject ($scenario+' trigger before approach') { Assert-KmcChunk6aApproachInvalidation $scenario (New-InvalidationArtifact $scenario { param($c) $c.trigger.riderDisplacement=0.1 }) }
 Reject ($scenario+' adjacent start') { Assert-KmcChunk6aApproachInvalidation $scenario (New-InvalidationArtifact $scenario { param($c) $c.start.isAdjacent=$true }) }
 Reject ($scenario+' second cast') { Assert-KmcChunk6aApproachInvalidation $scenario (New-InvalidationArtifact $scenario { param($c) $c.castRequestDelta=2 }) }
 Reject ($scenario+' preparation ran') { Assert-KmcChunk6aApproachInvalidation $scenario (New-InvalidationArtifact $scenario { param($c) $c.pairPrepareCallbacks=1 }) }
 Reject ($scenario+' incomplete trace') { Assert-KmcChunk6aApproachInvalidation $scenario (New-InvalidationArtifact $scenario { param($c) $c.allocationTraceComplete=$false }) }
 Reject ($scenario+' diagnostic interrupt') { Assert-KmcChunk6aApproachInvalidation $scenario (New-InvalidationArtifact $scenario { param($c) $c.diagnosticInterruptCount=1 }) }
 Reject ($scenario+' other command terminal') { Assert-KmcChunk6aApproachInvalidation $scenario (New-InvalidationArtifact $scenario { param($c) $c.terminalCommand.id=778 }) }
 Reject ($scenario+' wrong contract') { Assert-KmcChunk6aApproachInvalidation $scenario (New-InvalidationArtifact $scenario { param($c) $c.contract='other' }) }
 Reject ($scenario+' row evidence differs') { $a=New-InvalidationArtifact $scenario; $a.rows[0].evidence.outcome='other'; Assert-KmcChunk6aApproachInvalidation $scenario $a }
 Reject ($scenario+' row absent') { $a=New-InvalidationArtifact $scenario; $a.rows[0].name='CM02-other'; Assert-KmcChunk6aApproachInvalidation $scenario $a }
 Reject ($scenario+' kind declaration') { Assert-KmcChunk6aApproachInvalidation $scenario (New-InvalidationArtifact $scenario { param($c) $c.diagnosticStimulus=-not$c.diagnosticStimulus }) }
}
foreach($scenario in @('chunk6a-left-area','chunk6a-view-agent-lost','chunk6a-loading-cutscene','chunk6a-rider-death-approach','chunk6a-mount-death-approach')){
 Reject ($scenario+' unacted cost') { Assert-KmcChunk6aApproachInvalidation $scenario (New-InvalidationArtifact $scenario { param($c) $c.pairCostCallbacks=1 }) }
 Reject ($scenario+' unacted cooldown raised') { Assert-KmcChunk6aApproachInvalidation $scenario (New-InvalidationArtifact $scenario { param($c) $c.after.rider.standard=6.0 }) }
 Reject ($scenario+' unacted admission') { Assert-KmcChunk6aApproachInvalidation $scenario (New-InvalidationArtifact $scenario { param($c) $c.ledgerDelta.admittedMount=1 }) }
 Reject ($scenario+' restoration absent') { Assert-KmcChunk6aApproachInvalidation $scenario (New-InvalidationArtifact $scenario { param($c) $c.restoration.restored=$false }) }
 Reject ($scenario+' restored twice') { Assert-KmcChunk6aApproachInvalidation $scenario (New-InvalidationArtifact $scenario { param($c) $c.restorationCount=2 }) }
}
Reject 'left-area mount stayed in game' { Assert-KmcChunk6aApproachInvalidation 'chunk6a-left-area' (New-InvalidationArtifact 'chunk6a-left-area' { param($c) $c.stimulus.mountAfter.inGame=$true }) }
Reject 'left-area mount not returned' { Assert-KmcChunk6aApproachInvalidation 'chunk6a-left-area' (New-InvalidationArtifact 'chunk6a-left-area' { param($c) $c.after.mountActor.inGame=$false }) }
Reject 'left-area generation moved' { Assert-KmcChunk6aApproachInvalidation 'chunk6a-left-area' (New-InvalidationArtifact 'chunk6a-left-area' { param($c) $c.generationDelta=1 }) }
Reject 'left-area forced detach' { Assert-KmcChunk6aApproachInvalidation 'chunk6a-left-area' (New-InvalidationArtifact 'chunk6a-left-area' { param($c) $c.ledgerDelta.forcedDetach=1 }) }
Accept 'left-area acted cast rejected before admission' { Assert-KmcChunk6aApproachInvalidation 'chunk6a-left-area' (New-InvalidationArtifact 'chunk6a-left-area' { param($c) $c.outcome='acted-not-mounted';$c.terminalCommand.acted=$true;$c.dispatchRejectedDelta=1 }) }
Accept 'left-area acted cast refused at admission' { Assert-KmcChunk6aApproachInvalidation 'chunk6a-left-area' (New-InvalidationArtifact 'chunk6a-left-area' { param($c) $c.outcome='acted-not-mounted';$c.terminalCommand.acted=$true;$c.ledgerDelta.admittedMount=1;$c.ledgerDelta.refusedVoluntary=1 }) }
Accept 'left-area acted cast compensated' { Assert-KmcChunk6aApproachInvalidation 'chunk6a-left-area' (New-InvalidationArtifact 'chunk6a-left-area' { param($c) $c.outcome='acted-not-mounted';$c.terminalCommand.acted=$true;$c.ledgerDelta.admittedMount=1;$c.ledgerDelta.refusedVoluntary=1;$c.ledgerDelta.forcedDetach=1;$c.generationDelta=1;$c.after.compensatedMounts=1 }) }
Reject 'left-area acted cast without any refusal' { Assert-KmcChunk6aApproachInvalidation 'chunk6a-left-area' (New-InvalidationArtifact 'chunk6a-left-area' { param($c) $c.outcome='acted-not-mounted';$c.terminalCommand.acted=$true }) }
Reject 'left-area acted cast admitted twice' { Assert-KmcChunk6aApproachInvalidation 'chunk6a-left-area' (New-InvalidationArtifact 'chunk6a-left-area' { param($c) $c.outcome='acted-not-mounted';$c.terminalCommand.acted=$true;$c.ledgerDelta.admittedMount=2;$c.ledgerDelta.refusedVoluntary=1 }) }
Reject 'view lease not retired' { Assert-KmcChunk6aApproachInvalidation 'chunk6a-view-agent-lost' (New-InvalidationArtifact 'chunk6a-view-agent-lost' { param($c) $c.restoration.lease.replacementRetired=$false }) }
Reject 'view not replaced' { Assert-KmcChunk6aApproachInvalidation 'chunk6a-view-agent-lost' (New-InvalidationArtifact 'chunk6a-view-agent-lost' { param($c) $c.stimulus.riderAfter.viewObject=11;$c.stimulus.lease.afterApply.view=11 }) }
Reject 'view lease on another actor' { Assert-KmcChunk6aApproachInvalidation 'chunk6a-view-agent-lost' (New-InvalidationArtifact 'chunk6a-view-agent-lost' { param($c) $c.stimulus.lease.actor=$mountId }) }
Reject 'cutscene never observed' { Assert-KmcChunk6aApproachInvalidation 'chunk6a-loading-cutscene' (New-InvalidationArtifact 'chunk6a-loading-cutscene' { param($c) $c.samples=@();$c.stimulus.modeAfterRequest='Default' }) }
Reject 'cutscene not restored' { Assert-KmcChunk6aApproachInvalidation 'chunk6a-loading-cutscene' (New-InvalidationArtifact 'chunk6a-loading-cutscene' { param($c) $c.after.currentMode='Cutscene';$c.restoration.modeNow='Cutscene' }) }
Reject 'generation guard silent' { Assert-KmcChunk6aApproachInvalidation 'chunk6a-generation-change' (New-InvalidationArtifact 'chunk6a-generation-change' { param($c) $c.lastShellRefusal=$null }) }
Reject 'generation invalidated twice' { Assert-KmcChunk6aApproachInvalidation 'chunk6a-generation-change' (New-InvalidationArtifact 'chunk6a-generation-change' { param($c) $c.diagnosticGenerationInvalidations=2;$c.generationDelta=2 }) }
Reject 'generation seam left armed' { Assert-KmcChunk6aApproachInvalidation 'chunk6a-generation-change' (New-InvalidationArtifact 'chunk6a-generation-change' { param($c) $c.restoration.armedAtRestoration=$true }) }
Reject 'generation stale shell admitted' { Assert-KmcChunk6aApproachInvalidation 'chunk6a-generation-change' (New-InvalidationArtifact 'chunk6a-generation-change' { param($c) $c.ledgerDelta.admittedMount=1;$c.ledgerDelta.refusedVoluntary=1 }) }
Reject 'generation unacted outcome' { Assert-KmcChunk6aApproachInvalidation 'chunk6a-generation-change' (New-InvalidationArtifact 'chunk6a-generation-change' { param($c) $c.outcome='unacted-Interrupt';$c.terminalCommand.acted=$false;$c.generationDelta=1 }) }
Reject 'exception not failed closed' { Assert-KmcChunk6aApproachInvalidation 'chunk6a-injected-exception' (New-InvalidationArtifact 'chunk6a-injected-exception' { param($c) $c.feedback='ok' }) }
Reject 'exception without cleanup' { Assert-KmcChunk6aApproachInvalidation 'chunk6a-injected-exception' (New-InvalidationArtifact 'chunk6a-injected-exception' { param($c) $c.ledgerDelta.forcedDetach=0 }) }
Reject 'exception moved generation' { Assert-KmcChunk6aApproachInvalidation 'chunk6a-injected-exception' (New-InvalidationArtifact 'chunk6a-injected-exception' { param($c) $c.generationDelta=1 }) }
Reject 'exception seam left armed' { Assert-KmcChunk6aApproachInvalidation 'chunk6a-injected-exception' (New-InvalidationArtifact 'chunk6a-injected-exception' { param($c) $c.after.mountExecutionFaultArmed=$true }) }
Reject 'death subject alive' { Assert-KmcChunk6aApproachInvalidation 'chunk6a-rider-death-approach' (New-InvalidationArtifact 'chunk6a-rider-death-approach' { param($c) $c.after.riderActor.dead=$false }) }
Reject 'death killed both' { Assert-KmcChunk6aApproachInvalidation 'chunk6a-mount-death-approach' (New-InvalidationArtifact 'chunk6a-mount-death-approach' { param($c) $c.after.riderActor.dead=$true }) }
Reject 'death without cleanup' { Assert-KmcChunk6aApproachInvalidation 'chunk6a-rider-death-approach' (New-InvalidationArtifact 'chunk6a-rider-death-approach' { param($c) $c.ledgerDelta.forcedDetach=0 }) }
Reject 'death paid a cost' { Assert-KmcChunk6aApproachInvalidation 'chunk6a-rider-death-approach' (New-InvalidationArtifact 'chunk6a-rider-death-approach' { param($c) $c.pairCostCallbacks=1 }) }
Reject 'death policy not restored' { Assert-KmcChunk6aApproachInvalidation 'chunk6a-rider-death-approach' (New-InvalidationArtifact 'chunk6a-rider-death-approach' { param($c) $c.restoration.policy.restoration.restored=$false }) }
Reject 'death damage twice' { Assert-KmcChunk6aApproachInvalidation 'chunk6a-rider-death-approach' (New-InvalidationArtifact 'chunk6a-rider-death-approach' { param($c) $c.stimulus.damageDispatches=2 }) }
Reject 'death under live policy' { Assert-KmcChunk6aApproachInvalidation 'chunk6a-rider-death-approach' (New-InvalidationArtifact 'chunk6a-rider-death-approach' { param($c) $c.stimulus.policy.permanentDeathFixture=$false }) }
Accept 'death acted cast rejected before admission' { Assert-KmcChunk6aApproachInvalidation 'chunk6a-mount-death-approach' (New-InvalidationArtifact 'chunk6a-mount-death-approach' { param($c) $c.outcome='acted-not-mounted';$c.terminalCommand.acted=$true;$c.dispatchRejectedDelta=1 }) }
Reject 'death acted cast admitted' { Assert-KmcChunk6aApproachInvalidation 'chunk6a-mount-death-approach' (New-InvalidationArtifact 'chunk6a-mount-death-approach' { param($c) $c.outcome='acted-not-mounted';$c.terminalCommand.acted=$true;$c.ledgerDelta.admittedMount=1;$c.ledgerDelta.refusedVoluntary=1 }) }

# ---------------------------------------------------------------------------------------------
# Turn end and mode exit.
# ---------------------------------------------------------------------------------------------
function New-TurnEndArtifact([scriptblock]$Mutate=$null){
 $pending=Cmd 777 $false $false $false 'None'
 $c=@{contract='native-end-turn-during-exact-turn-based-mount-approach';boundary='turn-end';start=@{boundary='positive-pre-click';isAdjacent=$false};before=(Tb 'after-click' @{command=$pending});samples=@();endInputCount=1;trigger=(Trigger $null);beforeEndInput=(Tb 'before-end-input' @{command=$pending});endInput=@{method='Kingmaker.Game.PauseBind';token='06000CB7';moduleMvid='m';count=1;frame=130;gameTicks=1300};afterEndInput=(Tb 'after-end-input' @{command=$pending});terminal=(Tb 'terminal' @{turnObject=5002;turnActor=$targetId;command=(Cmd 777 $false $false $true 'Interrupt')});terminalCommand=(Cmd 777 $false $false $true 'Interrupt');commandWindow=$null;after=(Tb 'after' @{turnObject=5002;turnActor=$targetId;command=(Cmd 777 $false $false $true 'Interrupt')});allocationEvents=@();allocationTraceComplete=$true;observerHooks=@();interrupts=@();pairCostCallbacks=0;pairPrepareCallbacks=0;ledgerDelta=(Ledger 0 0 0 0 0);generationDelta=0;dispatchAcceptedDelta=0;dispatchRejectedDelta=0;relationshipShellsDelta=1;roundDelta=0;noResidue=$true;outcome='unacted-Interrupt';settledFrames=10;feedback=$null;lastShellRefusal=$null}
 if($null-ne$Mutate){ & $Mutate $c }
 J @{schemaVersion=30;observations=@{chunk6aTurnEndApproach=$c};rows=@(@{name='CM04-turn-end';status='PASS';evidence=$c})}
}
Accept 'turn end accepted' { Assert-KmcChunk6aTurnEnd (New-TurnEndArtifact) }
Accept 'turn end with one round rollover' { Assert-KmcChunk6aTurnEnd (New-TurnEndArtifact { param($c) $c.roundDelta=1 }) }
Reject 'turn end turn unchanged' { Assert-KmcChunk6aTurnEnd (New-TurnEndArtifact { param($c) $c.terminal.turnObject=5001;$c.after.turnObject=5001 }) }
Reject 'turn end acted terminal' { Assert-KmcChunk6aTurnEnd (New-TurnEndArtifact { param($c) $c.terminalCommand.acted=$true }) }
Reject 'turn end delivered' { Assert-KmcChunk6aTurnEnd (New-TurnEndArtifact { param($c) $c.after.relationshipState='Mounted';$c.outcome='delivered' }) }
Reject 'turn end ledger moved' { Assert-KmcChunk6aTurnEnd (New-TurnEndArtifact { param($c) $c.ledgerDelta.forcedDetach=1 }) }
Reject 'turn end cost' { Assert-KmcChunk6aTurnEnd (New-TurnEndArtifact { param($c) $c.pairCostCallbacks=1 }) }
Reject 'turn end input twice' { Assert-KmcChunk6aTurnEnd (New-TurnEndArtifact { param($c) $c.endInputCount=2;$c.endInput.count=2 }) }
Reject 'turn end on another turn' { Assert-KmcChunk6aTurnEnd (New-TurnEndArtifact { param($c) $c.beforeEndInput.turnActor=$targetId }) }
Reject 'turn end while waiting for ui' { Assert-KmcChunk6aTurnEnd (New-TurnEndArtifact { param($c) $c.beforeEndInput.waitingForUi=$true }) }
Reject 'turn end encounter ended' { Assert-KmcChunk6aTurnEnd (New-TurnEndArtifact { param($c) $c.after.partyInCombat=$false }) }
Reject 'turn end residue' { Assert-KmcChunk6aTurnEnd (New-TurnEndArtifact { param($c) $c.after.horseCommandsEmpty=$false }) }
Reject 'turn end two rounds' { Assert-KmcChunk6aTurnEnd (New-TurnEndArtifact { param($c) $c.roundDelta=2 }) }
Reject 'turn end from preparing turn' { Assert-KmcChunk6aTurnEnd (New-TurnEndArtifact { param($c) $c.before.turnStatus='Preparing' }) }
Reject 'turn end cooldown raised' { Assert-KmcChunk6aTurnEnd (New-TurnEndArtifact { param($c) $c.after.mount.move=3.0 }) }
function New-ModeExitArtifact([scriptblock]$Mutate=$null){
 $feedback='Mounted combat cancelled: real-time/turn-based mode changed.'
 $c=@{contract='native-turn-based-mode-exit-while-mounted-on-adopted-turn';boundary='mode-exit';mountWindow='positive-mount';before=(Tb 'before-exit' @{relationshipState='Mounted';pairedIdentity='enc:1';pairedSequence=1});samples=@();exitInputCount=1;exitInput=@{method='SettingsEntityBase.OnInvokeUpdateCallback';token='06003359';cacheToken='04002275';temporaryValue=$false;settingAfter=$false;frame=140;gameTicks=1400};afterExitInput=(Tb 'after-exit-input' @{relationshipState='Mounted';pairedIdentity='enc:1';turnBasedSetting=$false});terminal=(Tb 'terminal' @{relationshipState='Mounted';turnBasedSetting=$false;turnBasedCombat=$false;exitAiLeaseArmed=1});after=(Tb 'after' @{relationshipState='Mounted';turnBasedSetting=$false;turnBasedCombat=$false;exitAiLeaseArmed=1;combatFeedback=$feedback});allocationEvents=@();allocationTraceComplete=$true;observerHooks=@();pairCostCallbacks=0;pairPrepareCallbacks=0;ledgerDelta=(Ledger 0 0 0 0 0);noResidue=$true;settledFrames=10;feedback=$feedback;restoreInput=@{frame=160;settingAfter=$true;persistedValueUnchanged=$true;restoreDeliveryCompleted=$true};afterRestore=(Tb 'after-restore' @{relationshipState='Mounted';exitAiLeaseArmed=1;combatFeedback=$feedback})}
 if($null-ne$Mutate){ & $Mutate $c }
 J @{schemaVersion=30;observations=@{chunk6aModeExit=$c};rows=@(@{name='CM04-mode-exit';status='PASS';evidence=$c})}
}
Accept 'mode exit accepted' { Assert-KmcChunk6aModeExit (New-ModeExitArtifact) }
Accept 'mode exit retaining the same activation' { Assert-KmcChunk6aModeExit (New-ModeExitArtifact { param($c) $c.after.pairedIdentity='enc:1' }) }
Reject 'mode exit not observed by the product' { Assert-KmcChunk6aModeExit (New-ModeExitArtifact { param($c) $c.after.exitAiLeaseArmed=0;$c.terminal.exitAiLeaseArmed=0;$c.afterRestore.exitAiLeaseArmed=0 }) }
Reject 'mode exit observed twice' { Assert-KmcChunk6aModeExit (New-ModeExitArtifact { param($c) $c.afterRestore.exitAiLeaseArmed=2 }) }
Reject 'mode exit fresh activation' { Assert-KmcChunk6aModeExit (New-ModeExitArtifact { param($c) $c.after.pairedIdentity='enc:2' }) }
Reject 'mode exit dismounted' { Assert-KmcChunk6aModeExit (New-ModeExitArtifact { param($c) $c.after.relationshipState='Unmounted' }) }
Reject 'mode exit cost' { Assert-KmcChunk6aModeExit (New-ModeExitArtifact { param($c) $c.pairCostCallbacks=1 }) }
Reject 'mode exit ledger moved' { Assert-KmcChunk6aModeExit (New-ModeExitArtifact { param($c) $c.ledgerDelta.acceptedDismount=1 }) }
Reject 'mode exit controller stayed turn based' { Assert-KmcChunk6aModeExit (New-ModeExitArtifact { param($c) $c.terminal.turnBasedCombat=$true;$c.after.turnBasedCombat=$true }) }
Reject 'mode exit not restored' { Assert-KmcChunk6aModeExit (New-ModeExitArtifact { param($c) $c.afterRestore.turnBasedCombat=$false }) }
Reject 'mode exit touched persisted settings' { Assert-KmcChunk6aModeExit (New-ModeExitArtifact { param($c) $c.restoreInput.persistedValueUnchanged=$false }) }
Reject 'mode exit input twice' { Assert-KmcChunk6aModeExit (New-ModeExitArtifact { param($c) $c.exitInputCount=2 }) }
Reject 'mode exit from unsettled pair' { Assert-KmcChunk6aModeExit (New-ModeExitArtifact { param($c) $c.before.transitionInFlight=$true }) }
Reject 'mode exit residue' { Assert-KmcChunk6aModeExit (New-ModeExitArtifact { param($c) $c.after.riderCommandsEmpty=$false }) }
Reject 'mode exit without cancellation feedback' { Assert-KmcChunk6aModeExit (New-ModeExitArtifact { param($c) $c.after.combatFeedback=$null }) }
Reject 'mode exit cooldown raised' { Assert-KmcChunk6aModeExit (New-ModeExitArtifact { param($c) $c.after.rider.standard=6.0 }) }

# ---------------------------------------------------------------------------------------------
# P04 closure checkpoints.
# ---------------------------------------------------------------------------------------------
function Found([string]$Rel,[int]$Gen,[bool]$InFlight,[int]$Casts,[int]$Accepted,$Counters,$Paired,[int]$Adopt){
 @{relationship=$Rel;generation=$Gen;transitionInFlight=$InFlight;transitionSettlement='';relationshipShells=1;castRequests=$Casts;dispatchAccepted=$Accepted;dispatchRejected=0;transitionCounters=$Counters;pairedIdentity=$Paired;pairedSequence=1;pairedSplit=$false;pairedFinalized=$false;partnerContextActor=$null;adoptionCount=$Adopt;adoptionObservation=$null;initiativeObservation='not-observed';persistenceWorldDiscards=0;lastPersistenceWorldDiscard=$null;riderInCombat=$true;mountInCombat=$true;riderHasMove=$true;riderHasStandard=$true;riderCommandsEmpty=$true;mountCommandsEmpty=$true;riderReallyMoving=$false;mountReallyMoving=$false;riderPosition=@(0.0,0.0,0.0);mountPosition=@(1.0,0.0,0.0);pairDistance=1.0;riderMoveSlot=$null}
}
function Clo([string]$Case,[string]$Rel,[hashtable]$Over=@{}){
 $mounted=$Rel-ceq'Mounted'
 $o=Found $Rel $(if($mounted){2}else{1}) $false 1 $(if($mounted){1}else{0}) (Ledger $(if($mounted){1}else{0}) $(if($mounted){1}else{0}) 0 1 1) $(if($mounted){'enc:1'}else{$null}) $(if($mounted){1}else{0})
 foreach($pair in @{case=$Case;frame=100;gameTicks=1000;area=$area;sourceArea=$null;sameWorld=$true;currentMode='Default';loadingInProcess=$false;loadingObserved=$false;loadingFrames=0;suspensions=0;resumes=0;areaRefused=0;areaPending=$false;deferredSaves=0;snapshots=0;failedSaves=0;nativeSaveWaiting=$false;saveSuspended=$false;serializationSuspended=$false;exactFactCount=3;duplicateFactCount=0;managedHotbarSlotCount=2;riderView=11;mountView=22;riderViewBound=$true;mountViewBound=$true;riderOverrideComponents=@();mountOverrideComponents=@();runtimeMovementAgent=0;riderAgentOverride=0;riderStockAgentEnabled=$true;mountStockAgentEnabled=$true;liveRiderCommandsEmpty=$true;liveMountCommandsEmpty=$true;liveRiderInCombat=$true;liveMountInCombat=$true;nativeActorCounts=@{rider=1;mount=1};riderReallyMoving=$false;fixtureReleased=$false;feedback=$null}.GetEnumerator()){ $o[$pair.Key]=$pair.Value }
 if($mounted){ $o['riderOverrideComponents']=@(501);$o['runtimeMovementAgent']=501;$o['riderAgentOverride']=501;$o['riderStockAgentEnabled']=$false }
 foreach($k in $Over.Keys){$o[$k]=$Over[$k]}; $o
}
function Snap(){ @{SchemaVersion=2;CampaignId=$gameId;AreaId=$area;GameTimeTicks=1;Policy='rider-principal-distinct-native-v1';RulesId='crpg-transport-v1';Mounted=$true;ProfileId='medium-humanoid-mammoth-v1';Rider=@{Id=$riderId};Mount=@{Id=$mountId};Slots=@();Combat=@{TurnBased=$false}} }
function Row([string]$RunId,[string]$Scenario,[string]$Case,[string]$Kind,[string]$Rel,$Detail){
 @{runId=$RunId;scenario=$Scenario;processId=4242;kind=$Kind;checkpoint=$Case;stage=0;time='2026-10-10T00:00:00+00:00';gameTicks=1000;source=$commit;dll=$dll;relationship=$Rel;rider=@{Id=$riderId;Standard=0.0;Move=0.0};mount=@{Id=$mountId;Standard=0.0;Move=0.0};controls=@{Registered=$true;Enabled=$true;SerializationSuspended=$false;ExactFactCount=3;DuplicateFactCount=0;ManagedHotbarSlotCount=2;NativeCastRequestCount=1};native=@{paused=$false;mode='Default';partyCombat=$true;tbSetting=$false;tbInitialized=$false;tbCurrent=$null};persistence=@{semantics=0;presentation=0};detail=$Detail}
}
function Archive([string]$RunId,[string]$Leaf,[string]$Content){
 $root=Join-Path $script:ownedTestLab ('runtime-staging/persistence-'+$RunId+'/Saved Games');[void][IO.Directory]::CreateDirectory($root)
 $path=Join-Path $root $Leaf
 [IO.File]::WriteAllText($path,$Content,[Text.UTF8Encoding]::new($false))
 @{path=$path;sha256=(Get-KmcSha256 $path);length=(Get-Item -LiteralPath $path).Length}
}
function WriteDetail([string]$RunId,$Archive,[hashtable]$Over=@{}){
 $w=@{path=$Archive.path;sha256=$Archive.sha256;length=$Archive.length;nativeType='Manual';nativeCallback=$true;operation='None';snapshot=(Snap)}
 foreach($k in $Over.Keys){$w[$k]=$Over[$k]}; $w
}
function New-Request([string]$RunId,[string]$Scenario,[string]$Case){ @{schemaVersion=2;runId=$RunId;scenario=$Scenario;commit=$commit;dllSha256=$dll;persistenceCase=$Case;evidenceRoot=(Join-Path $script:ownedTestLab ('runtime-evidence/'+$RunId));fixture=@{working=@{gameId=$gameId;area=$area}}} }
function New-Game([string]$RunId,[string]$Scenario){ @{runId=$RunId;scenario=$Scenario;processId=4242;status='PASS';assertionPassCount=9;assertionFailCount=0;errors=@();commit=$commit;dllSha256=$dll} }
function New-ClosureRows([string]$Case){
 $run='closure-'+$Case;$s='persistence-p04-save'
 $foundationClick=$click.Clone();$foundationClick['selectedIds']=@($riderId);$foundationClick['availableForCast']=$true;$foundationClick['casterId']=$riderId
 $clickDetail=@{click=$foundationClick;availability=@{visible=$true;enabled=$true;transitionReady=$true;reason=$null};before=(Found 'Unmounted' 1 $false 0 0 (Ledger 0 0 0 1 1) $null 0)}
 $rows=@((Row $run $s $Case 'initial' 'Unmounted' $null),(Row $run $s $Case 'rt-foundation-combat-ready' 'Unmounted' $null),(Row $run $s $Case 'rt-combat-mount-click' 'Unmounted' $clickDetail))
 $settledDetail=@{foundation=(Found 'Mounted' 2 $false 1 1 (Ledger 1 1 0 1 1) 'enc:1' 1);settledFrames=10}
 switch -CaseSensitive ($Case){
  'combat-mount-routes' {
   $rows+=(Row $run $s $Case 'rt-combat-mount-settled' 'Mounted' $settledDetail)
   $types=@(@('Manual','Manual_300_KMC_P01.zks','KMC_P01'),@('Quick','Quick_1.zks','Quick_1'),@('Auto','Auto_1.zks','Auto_1'))
   for($i=0;$i-lt3;$i++){
    $a=Archive $run $types[$i][1] ('route '+$types[$i][0]+' '+$run)
    $rows+=(Row $run $s $Case 'routes-write-requested' 'Mounted' @{ordinal=($i+1);nativeType=$types[$i][0];name=$types[$i][2];overwrite=$false;actual=(Clo $Case 'Mounted' @{snapshots=$i})})
    $rows+=(Row $run $s $Case 'routes-write-complete' 'Mounted' (WriteDetail $run $a @{ordinal=($i+1);nativeType=$types[$i][0];name=$types[$i][2];actual=(Clo $Case 'Mounted' @{snapshots=($i+1)})}))
   }
   $rows+=(Row $run $s $Case 'routes-complete' 'Mounted' (Clo $Case 'Mounted' @{snapshots=3}))
  }
  'combat-mount-unsettled-save' {
   $a=Archive $run 'Manual_300_KMC_P01.zks' ('deferred '+$run)
   $requested=Clo $Case 'Unmounted' @{transitionInFlight=$true}
   $rows+=(Row $run $s $Case 'rt-unsettled-save-requested' 'Unmounted' $requested)
   $rows+=(Row $run $s $Case 'rt-unsettled-save-deferred' 'Unmounted' (Clo $Case 'Unmounted' @{transitionInFlight=$true;deferredSaves=1;nativeSaveWaiting=$true}))
   $rows+=(Row $run $s $Case 'rt-unsettled-save-transition-settled' 'Mounted' (Clo $Case 'Mounted' @{deferredSaves=1}))
   $rows+=(Row $run $s $Case 'native-write-complete' 'Mounted' (WriteDetail $run $a @{actual=(Clo $Case 'Mounted' @{deferredSaves=1;snapshots=1});requested=$requested}))
   $rows+=(Row $run $s $Case 'unsettled-save-complete' 'Mounted' (Clo $Case 'Mounted' @{deferredSaves=1;snapshots=1}))
  }
  'combat-mount-area-reload' {
   $a=Archive $run 'Manual_300_KMC_P01.zks' ('combat mounted '+$run)
   $rows+=(Row $run $s $Case 'rt-combat-mount-settled' 'Mounted' $settledDetail)
   $rows+=(Row $run $s $Case 'rt-before-save' 'Mounted' @{foundation=(Found 'Mounted' 2 $false 1 1 (Ledger 1 1 0 1 1) 'enc:1' 1)})
   $rows+=(Row $run $s $Case 'rt-native-save-requested' 'Mounted' @{foundation=(Found 'Mounted' 2 $false 1 1 (Ledger 1 1 0 1 1) 'enc:1' 1)})
   $rows+=(Row $run $s $Case 'native-write-complete' 'Mounted' (WriteDetail $run $a @{actual=@{foundation=(Found 'Mounted' 2 $false 1 1 (Ledger 1 1 0 1 1) 'enc:1' 1)}}))
   $requested=Clo $Case 'Mounted' @{snapshots=1}
   $rows+=(Row $run $s $Case 'combat-area-reload-requested' 'Mounted' $requested)
   $complete=Clo $Case 'Unmounted' @{generation=2;castRequests=1;dispatchAccepted=1;transitionCounters=(Ledger 1 1 0 2 1);snapshots=1;sourceArea=$area;loadingObserved=$true;loadingFrames=5;fixtureReleased=$true;archivePath=$a.path;archiveSha256Before=$a.sha256;archiveSha256After=$a.sha256;baseline=$requested}
   $rows+=(Row $run $s $Case 'combat-area-reload-complete' 'Unmounted' $complete)
  }
  'pending-mount-area' {
   $observed=Clo $Case 'Unmounted' @{transitionInFlight=$true;riderReallyMoving=$true;riderDisplacement=0.6;shell=@{type='UnitUseAbility';abilityGuid=$mountGuid;executor=$riderId;target=$mountId;started=$false;acted=$false;finished=$false;result='None';createdByPlayer=$true}}
   $rows+=(Row $run $s $Case 'pending-mount-approach-observed' 'Unmounted' $observed)
   $requested=Clo $Case 'Unmounted' @{transitionInFlight=$true}
   $rows+=(Row $run $s $Case 'pending-mount-area-requested' 'Unmounted' $requested)
   $complete=Clo $Case 'Unmounted' @{transitionCounters=(Ledger 0 0 0 1 2);sourceArea=$area;loadingObserved=$true;loadingFrames=5;fixtureReleased=$true;baseline=$requested}
   $rows+=(Row $run $s $Case 'pending-mount-area-complete' 'Unmounted' $complete)
  }
 }
 ,$rows
}
function Test-Closure([string]$Case,[scriptblock]$Mutate=$null){
 $rows=@(J (New-ClosureRows $Case))
 if($null-ne$Mutate){ & $Mutate $rows }
 Assert-KmcChunk6aClosurePersistenceEvidence (J (New-Request ('closure-'+$Case) 'persistence-p04-save' $Case)) $rows (J (New-Game ('closure-'+$Case) 'persistence-p04-save'))
}
foreach($case in @(Get-KmcChunk6aClosureCases)){
 Accept ($case+' accepted') { Test-Closure $case }
 Reject ($case+' failure row') { Test-Closure $case { param($r) $r[1].kind='assertion-failed' } }
 Reject ($case+' foreign actor') { Test-Closure $case { param($r) $r[2].mount.Id=$targetId } }
 Reject ($case+' turn-based row') { Test-Closure $case { param($r) $r[0].native.tbSetting=$true } }
 Reject ($case+' click refused') { Test-Closure $case { param($r) $r[2].detail.click.nativeRefusalDelta=1 } }
 Reject ($case+' terminal not last') { Test-Closure $case { param($r) $r[-1].kind='rt-foundation-continuation' } }
 Reject ($case+' duplicate control') { Test-Closure $case { param($r) $r[-1].controls.DuplicateFactCount=1 } }
}
Reject 'routes archives identical' { Test-Closure 'combat-mount-routes' { param($r) $c=@($r|Where-Object kind -CEQ 'routes-write-complete'); $c[1].detail.sha256=$c[0].detail.sha256;$c[1].detail.path=$c[0].detail.path;$c[1].detail.length=$c[0].detail.length } }
Reject 'routes out of order' { Test-Closure 'combat-mount-routes' { param($r) $c=@($r|Where-Object kind -CEQ 'routes-write-complete'); $c[0].detail.nativeType='Quick';$c[1].detail.nativeType='Manual' } }
Reject 'routes left mounted controls changed' { Test-Closure 'combat-mount-routes' { param($r) $r[-1].detail.exactFactCount=4 } }
Reject 'routes deferred a save' { Test-Closure 'combat-mount-routes' { param($r) $r[-1].detail.deferredSaves=1 } }
Reject 'routes dismounted' { Test-Closure 'combat-mount-routes' { param($r) $c=@($r|Where-Object kind -CEQ 'routes-write-complete'); $c[2].relationship='Unmounted' } }
Reject 'routes staged archive differs' { Test-Closure 'combat-mount-routes' { param($r) $c=@($r|Where-Object kind -CEQ 'routes-write-complete'); $c[2].detail.sha256=('0'*64) } }
Reject 'unsettled save not deferred' { Test-Closure 'combat-mount-unsettled-save' { param($r) $d=@($r|Where-Object kind -CEQ 'rt-unsettled-save-deferred')[0]; $d.detail.deferredSaves=0;$d.detail.nativeSaveWaiting=$false } }
Reject 'unsettled save captured early' { Test-Closure 'combat-mount-unsettled-save' { param($r) $d=@($r|Where-Object kind -CEQ 'rt-unsettled-save-deferred')[0]; $d.detail.snapshots=1 } }
Reject 'unsettled save requested while settled' { Test-Closure 'combat-mount-unsettled-save' { param($r) $d=@($r|Where-Object kind -CEQ 'rt-unsettled-save-requested')[0]; $d.detail.transitionInFlight=$false } }
Reject 'unsettled save written unmounted' { Test-Closure 'combat-mount-unsettled-save' { param($r) $d=@($r|Where-Object kind -CEQ 'native-write-complete')[0]; $d.relationship='Unmounted' } }
Reject 'area reload without cleanup' { Test-Closure 'combat-mount-area-reload' { param($r) $r[-1].detail.transitionCounters.forcedDetach=1 } }
Reject 'area reload cleaned twice' { Test-Closure 'combat-mount-area-reload' { param($r) $r[-1].detail.transitionCounters.forcedDetach=3 } }
Reject 'area reload archive changed' { Test-Closure 'combat-mount-area-reload' { param($r) $r[-1].detail.archiveSha256After=('1'*64) } }
Reject 'area reload carried the pair' { Test-Closure 'combat-mount-area-reload' { param($r) $r[-1].detail.suspensions=1;$r[-1].detail.resumes=1 } }
Reject 'area reload stale override component' { Test-Closure 'combat-mount-area-reload' { param($r) $r[-1].detail.riderOverrideComponents=@(501);$r[-1].detail.riderAgentOverride=501 } }
Reject 'area reload no native unload' { Test-Closure 'combat-mount-area-reload' { param($r) $r[-1].detail.loadingObserved=$false;$r[-1].detail.loadingFrames=0 } }
Reject 'area reload other world' { Test-Closure 'combat-mount-area-reload' { param($r) $r[-1].detail.sameWorld=$false } }
Reject 'area reload replayed cast' { Test-Closure 'combat-mount-area-reload' { param($r) $r[-1].detail.castRequests=2 } }
Reject 'area reload baseline differs' { Test-Closure 'combat-mount-area-reload' { param($r) $r[-1].detail.baseline.snapshots=0 } }
Reject 'pending area no cleanup' { Test-Closure 'pending-mount-area' { param($r) $r[-1].detail.transitionCounters.duplicateSuppressed=1 } }
Reject 'pending area two cleanups' { Test-Closure 'pending-mount-area' { param($r) $r[-1].detail.transitionCounters.forcedDetach=2 } }
Accept 'pending area fresh forced detach' { Test-Closure 'pending-mount-area' { param($r) $r[-1].detail.transitionCounters.forcedDetach=2;$r[-1].detail.transitionCounters.duplicateSuppressed=1 } }
Reject 'pending area delivered' { Test-Closure 'pending-mount-area' { param($r) $r[-1].detail.transitionCounters.acceptedMount=1;$r[-1].detail.transitionCounters.admittedMount=1 } }
Reject 'pending area wrote a save' { Test-Closure 'pending-mount-area' { param($r) $r[-1].detail.snapshots=1 } }
Reject 'pending area before approach' { Test-Closure 'pending-mount-area' { param($r) $o=@($r|Where-Object kind -CEQ 'pending-mount-approach-observed')[0]; $o.detail.riderDisplacement=0.1 } }
Reject 'pending area shell acted' { Test-Closure 'pending-mount-area' { param($r) $o=@($r|Where-Object kind -CEQ 'pending-mount-approach-observed')[0]; $o.detail.shell.acted=$true } }
Reject 'pending area replayed cast' { Test-Closure 'pending-mount-area' { param($r) $r[-1].detail.castRequests=2 } }

# ---------------------------------------------------------------------------------------------
# Area restoration (Chunk 5 persistence-p07-save area-reload).
# ---------------------------------------------------------------------------------------------
function AreaActor([string]$Id,[int]$View){ @{id=$Id;nativeActorCount=1;baselineViewId=$View;viewId=$View;viewAlive=$true;viewBound=$true;viewDisposition='retained';viewExactForPair=$true;boundToRelationship=$true} }
function Authority([int]$Component){ @{riderOverrideBaseline=500;riderOverrideComponents=@($Component);mountOverrideComponents=@();runtimeMovementAgent=$Component;riderAgentOverride=$Component;riderOverrideIsRuntimeAgent=$true;riderStockAgentPresent=$true;riderStockAgentEnabled=$false;mountStockAgentPresent=$true;mountStockAgentEnabled=$true;mountAgentOverride=0;riderCommandsEmpty=$true;mountCommandsEmpty=$true;riderReallyMoving=$false;mountReallyMoving=$false} }
function AreaDetail([bool]$After){
 @{case='area-reload';area=$area;expectedArea=$area;sourceArea=$area;autoSaveMode=$null;loadingFrames=$(if($After){5}else{0});suspensionObserved=$After;suspensions=$(if($After){1}else{0});resumes=$(if($After){1}else{0});pending=$false;sameWorld=$true;riderView=11;mountView=22;nativeCastRequests=0;expectedViewDisposition='retained';loadingInProcess=$false;queuedLoads=0;deferredSaveWaiting=$false;snapshots=1;mountedInvariant=$null;presentation=$null;riderActor=(AreaActor $riderId 11);mountActor=(AreaActor $mountId 22);generation=$(if($After){2}else{1});generationBaseline=1;movementAuthority=(Authority $(if($After){501}else{500}))}
}
function New-AreaRows(){
 $run='closure-area-reload';$s='persistence-p07-save';$c='area-reload'
 $a=Archive $run 'Manual_300_KMC_P01.zks' 'area reload archive'
 @((Row $run $s $c 'initial' 'Mounted' $null),(Row $run $s $c 'area-initial-write' 'Mounted' @{sha256=$a.sha256;path=$a.path}),(Row $run $s $c 'area-reload-requested' 'Mounted' (AreaDetail $false)),(Row $run $s $c 'area-reload-observed' 'Mounted' (AreaDetail $true)),(Row $run $s $c 'area-reload-complete' 'Mounted' (AreaDetail $true)),(Row $run $s $c 'native-write-complete' 'Mounted' (WriteDetail $run $a)))
}
function Test-Area([scriptblock]$Mutate=$null){
 $rows=@(J (New-AreaRows))
 if($null-ne$Mutate){ & $Mutate $rows }
 Assert-KmcChunk6aAreaRestoration (J (New-Request 'closure-area-reload' 'persistence-p07-save' 'area-reload')) $rows (J (New-Game 'closure-area-reload' 'persistence-p07-save'))
}
Accept 'area restoration accepted' { Test-Area }
Reject 'area restoration suspended twice' { Test-Area { param($r) $r[4].detail.suspensions=2 } }
Reject 'area restoration never suspended' { Test-Area { param($r) $r[4].detail.suspensionObserved=$false;$r[4].detail.suspensions=0;$r[4].detail.resumes=0 } }
Reject 'area restoration replaced view' { Test-Area { param($r) $r[4].detail.riderActor.viewDisposition='replaced';$r[4].detail.riderActor.viewId=99 } }
Reject 'area restoration stale component retained' { Test-Area { param($r) $r[4].detail.movementAuthority.riderOverrideComponents=@(500,501) } }
Reject 'area restoration same component' { Test-Area { param($r) $r[4].detail.movementAuthority.riderOverrideComponents=@(500);$r[4].detail.movementAuthority.runtimeMovementAgent=500;$r[4].detail.movementAuthority.riderAgentOverride=500 } }
Reject 'area restoration generation unchanged' { Test-Area { param($r) $r[4].detail.generation=1 } }
Reject 'area restoration generation twice' { Test-Area { param($r) $r[4].detail.generation=3 } }
Reject 'area restoration not mounted' { Test-Area { param($r) $r[4].relationship='Unmounted' } }
Reject 'area restoration replayed cast' { Test-Area { param($r) $r[4].detail.nativeCastRequests=1 } }
Reject 'area restoration commands pending' { Test-Area { param($r) $r[4].detail.movementAuthority.riderCommandsEmpty=$false } }
Reject 'area restoration rider stock agent live' { Test-Area { param($r) $r[4].detail.movementAuthority.riderStockAgentEnabled=$true } }
Reject 'area restoration duplicate actor' { Test-Area { param($r) $r[4].detail.mountActor.nativeActorCount=2 } }
Reject 'area restoration invariant failed' { Test-Area { param($r) $r[4].detail.mountedInvariant='stale agent' } }
Reject 'area restoration other area' { Test-Area { param($r) $r[4].detail.area=('b'*32) } }
Reject 'area restoration failure row' { Test-Area { param($r) $r[1].kind='assertion-failed' } }
Reject 'area restoration before-state unsettled' { Test-Area { param($r) $r[2].detail.suspensions=1 } }

# ---------------------------------------------------------------------------------------------
# Accepted save schema.
# ---------------------------------------------------------------------------------------------
Add-Type -AssemblyName System.IO.Compression.FileSystem
Add-Type -AssemblyName System.IO.Compression
function New-SchemaArchive([string]$RunId,[scriptblock]$Mutate=$null,[string[]]$ExtraMembers=@()){
 $accepted=Get-KmcChunk6aAcceptedSaveSchema
 $combat=[ordered]@{};foreach($k in $accepted.combatKeys){$combat[$k]=$null};$combat['TurnBased']=$false;$combat['Round']=0;$combat['Actors']=@();$combat['Roster']=@();$combat['Engagements']=@();$combat['Allocations']=@()
 $data=[ordered]@{};foreach($k in $accepted.keys){$data[$k]=$null}
 $data['SchemaVersion']=2;$data['CampaignId']=$gameId;$data['AreaId']=$area;$data['GameTimeTicks']=1;$data['Policy']=$accepted.policy;$data['RulesId']=$accepted.rules;$data['Mounted']=$true;$data['ProfileId']='medium-humanoid-mammoth-v1';$data['Rider']=@{Id=$riderId};$data['Mount']=@{Id=$mountId};$data['Slots']=@();$data['Combat']=$combat
 if($null-ne$Mutate){ & $Mutate $data }
 $root=Join-Path $script:ownedTestLab ('runtime-staging/persistence-'+$RunId+'/Saved Games');[void][IO.Directory]::CreateDirectory($root)
 $path=Join-Path $root 'Manual_300_KMC_P01.zks'
 if(Test-Path -LiteralPath $path){Remove-Item -LiteralPath $path -Force}
 $zip=[IO.Compression.ZipFile]::Open($path,[IO.Compression.ZipArchiveMode]::Create)
 try{
  foreach($entry in @(@('header.json','{"Name":"KMC_P01"}'),@('kmc-mounted-state',($data|ConvertTo-Json -Depth 20 -Compress)))+@($ExtraMembers|ForEach-Object {,@($_,'{}')})){
   $e=$zip.CreateEntry($entry[0]);$w=New-Object IO.StreamWriter($e.Open(),[Text.UTF8Encoding]::new($false));try{$w.Write($entry[1])}finally{$w.Dispose()}
  }
 }finally{$zip.Dispose()}
 @{path=$path;sha256=(Get-KmcSha256 $path);length=(Get-Item -LiteralPath $path).Length}
}
function Test-Schema([string]$RunId,[scriptblock]$Mutate=$null,[string[]]$ExtraMembers=@()){
 $a=New-SchemaArchive $RunId $Mutate $ExtraMembers
 $rows=@(J @((Row $RunId 'persistence-p04-save' 'combat-mount-rt' 'initial' 'Unmounted' $null),(Row $RunId 'persistence-p04-save' 'combat-mount-rt' 'native-write-complete' 'Mounted' (WriteDetail $RunId $a))))
 Assert-KmcChunk6aSchemaUnchanged (J (New-Request $RunId 'persistence-p04-save' 'combat-mount-rt')) $rows $script:ownedTestLab
}
Accept 'schema accepted' { Test-Schema 'schema-ok' }
Reject 'schema supplemental member' { Test-Schema 'schema-member' $null @('kmc-transition-state') }
Reject 'schema extra key' { Test-Schema 'schema-key' { param($d) $d['Transition']=@{} } }
Reject 'schema missing key' { Test-Schema 'schema-missing' { param($d) $d.Remove('Slots') } }
Reject 'schema extra combat key' { Test-Schema 'schema-combat' { param($d) $d['Combat']['Shells']=@() } }
Reject 'schema version changed' { Test-Schema 'schema-version' { param($d) $d['SchemaVersion']=3 } }
Reject 'schema policy changed' { Test-Schema 'schema-policy' { param($d) $d['Policy']='other' } }
Reject 'schema not mounted' { Test-Schema 'schema-unmounted' { param($d) $d['Mounted']=$false } }
Reject 'schema wrong checkpoint' { $a=New-SchemaArchive 'schema-case'; $rows=@(J @((Row 'schema-case' 'persistence-p04-save' 'combat-dismount-rt' 'native-write-complete' 'Unmounted' (WriteDetail 'schema-case' $a)))); Assert-KmcChunk6aSchemaUnchanged (J (New-Request 'schema-case' 'persistence-p04-save' 'combat-dismount-rt')) $rows $script:ownedTestLab }

# ---------------------------------------------------------------------------------------------
# Row projection, binding recognition and id maps.
# ---------------------------------------------------------------------------------------------
$routesRun='closure-combat-mount-routes';$evidenceRoot=Join-Path $script:ownedTestLab ('runtime-evidence/'+$routesRun);[void][IO.Directory]::CreateDirectory($evidenceRoot)
$jsonl=Join-Path $evidenceRoot 'persistence-observations.jsonl'
[IO.File]::WriteAllLines($jsonl,@(@(J (New-ClosureRows 'combat-mount-routes'))|ForEach-Object { $_|ConvertTo-Json -Depth 100 -Compress }),[Text.UTF8Encoding]::new($false))
$game=J (New-Game $routesRun 'persistence-p04-save')
Accept 'closure row projection' { $row=Get-KmcChunk6aClosureRows $jsonl 'persistence-p04-save' $game; if([string]$row.name-cne'P04-save-combat-mount-routes'-or[string]$row.status-cne'PASS'){throw 'Chunk 6A closure: projection differs'} }
Accept 'closure artifact recognized' { if(-not(Test-KmcChunk6aClosureArtifact $jsonl 'persistence-p04-save')){throw 'Chunk 6A closure: artifact not recognized'} }
Accept 'artifact rows dispatch to the closure projection' { $rows=@(Get-KmcChunk6aArtifactRows $jsonl 'persistence-p04-save' $game -PassRowsOnly); if($rows.Count-ne1-or[string]$rows[0].name-cne'P04-save-combat-mount-routes'){throw 'Chunk 6A closure: artifact rows differ'} }
Reject 'closure row projection under another scenario' { Get-KmcChunk6aClosureRows $jsonl 'persistence-p04-load' $game }
$foundationJsonl=Join-Path $script:ownedTestLab 'foundation.jsonl'
[IO.File]::WriteAllLines($foundationJsonl,@(@(J @((Row 'f1' 'persistence-p04-save' 'combat-mount-rt' 'initial' 'Unmounted' $null)))|ForEach-Object { $_|ConvertTo-Json -Depth 100 -Compress }),[Text.UTF8Encoding]::new($false))
Accept 'foundation artifact not claimed by the closure projection' { if(Test-KmcChunk6aClosureArtifact $foundationJsonl 'persistence-p04-save'){throw 'Chunk 6A closure: foundation artifact claimed'} }
Accept 'binding recognition' {
 if(-not(Test-KmcChunk6aClosureBinding ([pscustomobject]@{scenario='persistence-p04-save';rows=@('P04-save-combat-mount-routes')}))){throw 'Chunk 6A closure: routes binding not recognized'}
 if(-not(Test-KmcChunk6aClosureBinding ([pscustomobject]@{scenario='persistence-p07-save';rows=@('P07-save-area-reload')}))){throw 'Chunk 6A closure: area binding not recognized'}
 if(Test-KmcChunk6aClosureBinding ([pscustomobject]@{scenario='persistence-p04-save';rows=@('P04-save-combat-mount-rt')})){throw 'Chunk 6A closure: foundation binding claimed'}
 if(Test-KmcChunk6aClosureBinding ([pscustomobject]@{scenario='persistence-p04-load';rows=@('P04-save-combat-mount-routes')})){throw 'Chunk 6A closure: load binding claimed'}
}
Accept 'id map and row names' {
 $map=Get-KmcChunk6aClosureIdMap
 if(@($map.Keys).Count-ne5){throw 'Chunk 6A closure: id map size'}
 foreach($id in @($map.Keys)){ $e=$map[$id]; if([string](Get-KmcChunk6aClosureRowName $e[0] $e[1])-cnotin@(Get-KmcChunk6aClosureRowNames)){throw ('Chunk 6A closure: row name unregistered for '+$id)} }
 if(-not(Test-KmcChunk6aClosureId 'CM08-area-restoration')-or(Test-KmcChunk6aClosureId 'CM07-mount-save-rt')){throw 'Chunk 6A closure: id membership differs'}
 if((Get-KmcChunk6aInvalidationRow 'chunk6a-mount-death-approach')-cne'CM04-mount-death'-or$null-ne(Get-KmcChunk6aInvalidationRow 'chunk6a-mount-approach')){throw 'Chunk 6A closure: invalidation row map differs'}
}
Reject 'row name for a foreign checkpoint' { Get-KmcChunk6aClosureRowName 'persistence-p04-save' 'combat-mount-rt' }
Accept 'cold-load role contract' {
 foreach($pair in @(@('rt','persistence-p04-load','combat-mount-rt'),@('tb','persistence-p02-load','combat-mount-tb'))){
  $role=[pscustomobject]@{name=$pair[0];scenario=$pair[1]}
  Assert-KmcChunk6aColdLoadRole $role ([pscustomobject]@{scenario=$pair[1];persistenceCase=$pair[2];persistenceLoad=[pscustomobject]@{sha256=('e'*64);fileName='Manual_300_KMC_P01.zks'}})
 }
}
Reject 'cold-load role wrong checkpoint' { Assert-KmcChunk6aColdLoadRole ([pscustomobject]@{name='rt';scenario='persistence-p04-load'}) ([pscustomobject]@{scenario='persistence-p04-load';persistenceCase='combat-dismount-rt';persistenceLoad=[pscustomobject]@{sha256=('e'*64);fileName='Manual_300_KMC_P01.zks'}}) }
Reject 'cold-load role without archive descriptor' { Assert-KmcChunk6aColdLoadRole ([pscustomobject]@{name='tb';scenario='persistence-p02-load'}) ([pscustomobject]@{scenario='persistence-p02-load';persistenceCase='combat-mount-tb'}) }
Write-Host ('Synthetic closure fixtures retained: '+$script:ownedTestLab)
Write-Host ('CLOSURE READER PASS='+$script:checks+' FAIL=0; synthetic acceptance and refusal only, no native qualification')
