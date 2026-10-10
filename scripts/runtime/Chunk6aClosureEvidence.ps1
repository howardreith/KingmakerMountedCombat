# Chunk 6 closeout readers: the remaining Chunk 6A behaviors that the frozen preview.205 ledger left BLOCKED or
# FAIL. Read-only, deterministic, and the sole acceptance authority for their rows:
#   - the seven approach invalidations (CM02-left-area, CM02-view-agent-lost, CM02-loading-cutscene,
#     CM02-generation-change, CM04-injected-exception, CM04-rider-death, CM04-mount-death),
#   - the two turn-based boundaries (CM04-turn-end, CM04-mode-exit),
#   - the four P04 closure checkpoints (CM07-save-slot-routes, CM07-unsettled-save-deferred, CM07-area-reload,
#     CM04-area-session-transition),
#   - the Chunk 5 area-reload run as the supported area restoration (CM08-area-restoration),
#   - the accepted save schema of a combat-mounted archive (CM07-schema-unchanged).
# A diagnostic stimulus (generation invalidation, injected exception) is named as such in its contract and is
# never presented as a native fact. Every refusal of this file starts with 'Chunk 6A closure: ' so that a pure
# reader correction can re-evaluate immutable evidence through the reviewed re-evaluation contract.
Set-StrictMode -Version Latest
function ClosureFail([string]$Why){throw ('Chunk 6A closure: '+$Why)}
function ClosureProp($Object,[string]$Name){ if($null-ne$Object-and$null-ne$Object.PSObject.Properties[$Name]){$Object.$Name}else{$null} }
function ClosureNumber($Value,[string]$Why){
 if($Value-isnot[int]-and$Value-isnot[long]-and$Value-isnot[single]-and$Value-isnot[double]-and$Value-isnot[decimal]){ClosureFail ('number missing: '+$Why)}
 $n=[double]$Value;if([double]::IsNaN($n)-or[double]::IsInfinity($n)){ClosureFail ('nonfinite number: '+$Why)};$n
}
function ClosureLong($Value,[string]$Why){ if($Value-isnot[int]-and$Value-isnot[long]){ClosureFail ('integer missing: '+$Why)};[long]$Value }
function ClosureSameJson($A,$B){ (ConvertTo-Json -InputObject $A -Depth 100 -Compress)-ceq(ConvertTo-Json -InputObject $B -Depth 100 -Compress) }
# A foundation counter is a top-level member (castRequests, dispatchAccepted, adoptionCount, generation) or one of
# the transition ledger counters under transitionCounters (admittedMount, acceptedMount, forcedDetach, ...).
function ClosureCounter($Object,[string]$Name){ $v=ClosureProp $Object $Name; if($null-eq$v){ $v=ClosureProp (ClosureProp $Object 'transitionCounters') $Name }; ClosureLong $v $Name }
function ClosureDelta($After,$Before,[string]$Name){ (ClosureCounter $After $Name)-(ClosureCounter $Before $Name) }

# ---------------------------------------------------------------------------------------------
# Approach invalidations (schema 30, real time): one exact stimulus during the measured non-adjacent
# pending Mount approach; the transition must be refused (never delivered) and leave no residue.
# ---------------------------------------------------------------------------------------------
function Get-KmcChunk6aInvalidationScenarios {
 @('chunk6a-left-area','chunk6a-view-agent-lost','chunk6a-loading-cutscene','chunk6a-generation-change','chunk6a-injected-exception','chunk6a-rider-death-approach','chunk6a-mount-death-approach')
}
function Test-KmcChunk6aInvalidationScenario([string]$Scenario) { [string]$Scenario -cin @(Get-KmcChunk6aInvalidationScenarios) }
function Get-KmcChunk6aInvalidationRow([string]$Scenario) {
 switch -CaseSensitive ($Scenario) {
  'chunk6a-left-area' {'CM02-left-area'}
  'chunk6a-view-agent-lost' {'CM02-view-agent-lost'}
  'chunk6a-loading-cutscene' {'CM02-loading-cutscene'}
  'chunk6a-generation-change' {'CM02-generation-change'}
  'chunk6a-injected-exception' {'CM04-injected-exception'}
  'chunk6a-rider-death-approach' {'CM04-rider-death'}
  'chunk6a-mount-death-approach' {'CM04-mount-death'}
  default {$null}
 }
}
function Get-KmcChunk6aInvalidationContract([string]$Scenario) {
 switch -CaseSensitive ($Scenario) {
  'chunk6a-left-area' {'native-area-departure-during-exact-mount-approach'}
  'chunk6a-view-agent-lost' {'native-view-replacement-during-exact-mount-approach'}
  'chunk6a-loading-cutscene' {'native-cutscene-mode-during-exact-mount-approach'}
  'chunk6a-generation-change' {'diagnostic-generation-invalidation-before-exact-mount-delivery'}
  'chunk6a-injected-exception' {'diagnostic-exception-at-exact-mount-execution-boundary'}
  'chunk6a-rider-death-approach' {'native-rider-death-during-exact-mount-approach'}
  'chunk6a-mount-death-approach' {'native-mount-death-during-exact-mount-approach'}
  default {$null}
 }
}
# The lifecycle state carries the pair's native cooldowns as rider/mount {standard, move, swift}.
function Assert-KmcChunk6aClosureCooldownsNotRaised($Before,$After,[string]$Where) {
 foreach($actor in @('rider','mount')){
  foreach($f in @('standard','move','swift')){
   $b=ClosureProp (ClosureProp $Before $actor) $f;$a=ClosureProp (ClosureProp $After $actor) $f
   if($null-eq$b-or$null-eq$a){ClosureFail ($Where+' lacks the '+$actor+' '+$f+' cooldown')}
   if((ClosureNumber $a ($actor+' '+$f))-gt(ClosureNumber $b ($actor+' '+$f))+0.0001){ClosureFail ($Where+' raised '+$actor+' '+$f)}
  }
 }
}
function Assert-KmcChunk6aClosureClick($Click,[string]$Where) {
 if($null-eq$Click-or$Click.clicked-ne$true-or$Click.nativeCastRequestDelta-ne1-or$Click.nativeRefusalDelta-ne0-or$Click.dispatchAcceptedDelta-ne0-or$Click.dispatchRejectedDelta-ne0-or[string]$Click.abilityGuid-cne'f053faad986631688defa003cd7bda0e'){ClosureFail ($Where+': the one native Mount click was not admitted exactly once')}
}
function Assert-KmcChunk6aClosureTrigger($Trigger,[string]$Where) {
 if($null-eq$Trigger-or$Trigger.approachObserved-ne$true-or$Trigger.riderReallyMoving-ne$true-or(ClosureNumber $Trigger.riderDisplacement 'displacement')-le0.25-or$Trigger.started-ne$false-or$Trigger.acted-ne$false-or$Trigger.finished-ne$false-or$Trigger.moveSlotObject-ne$Trigger.commandObject-or$Trigger.geometry.isAdjacent-ne$false){ClosureFail ($Where+': the boundary did not fire during the measured non-adjacent pending approach')}
}
# Residue is decided from primitives (the compiled noResidue flag is recorded, not trusted): nothing in flight,
# both command containers empty, no paired identity or partner context, exactly one shell registration for the
# one click.
function Assert-KmcChunk6aClosureNoResidueState($After,$Case,[string]$Where) {
 if($After.transitionInFlight-ne$false-or$After.riderCommandsEmpty-ne$true-or$After.horseCommandsEmpty-ne$true-or$null-ne(ClosureProp $After 'pairedIdentity')-or$null-ne(ClosureProp $After 'partnerContextActor')-or$Case.relationshipShellsDelta-ne1){ClosureFail ($Where+' left relationship, command, activation or shell residue')}
}
function Assert-KmcChunk6aApproachInvalidation([string]$Scenario,$Artifact) {
 $row=Get-KmcChunk6aInvalidationRow $Scenario;$contract=Get-KmcChunk6aInvalidationContract $Scenario
 if($null-eq$row){ClosureFail ('not an approach invalidation scenario: '+$Scenario)}
 $Case=ClosureProp $Artifact.observations 'chunk6aApproachInvalidation'
 if($null-eq$Case){ClosureFail 'the approach invalidation evidence is absent'}
 $rows=@($Artifact.rows|Where-Object name -CEQ $row)
 if($rows.Count-ne1-or[string]$rows[0].status-cne'PASS'){ClosureFail ('the exact mandatory row '+$row+' is absent')}
 if(-not(ClosureSameJson $rows[0].evidence $Case)){ClosureFail 'the row evidence differs from the recorded case'}
 if([string]$Case.contract-cne$contract-or[string]$Case.scenario-cne$Scenario-or[string]$Case.row-cne$row){ClosureFail 'the contract differs'}
 $diagnostic=$Scenario-cin@('chunk6a-generation-change','chunk6a-injected-exception');$death=$Scenario-cin@('chunk6a-rider-death-approach','chunk6a-mount-death-approach')
 if($Case.diagnosticStimulus-ne$diagnostic-or$Case.externalRestorationRequired-ne$false){ClosureFail 'the stimulus kind declaration differs'}
 # The cutscene case has two lawful shapes: the pending shell reached its terminal under the native lock (refused or
 # interrupted), or the engine froze the party's commands under the lock, the fixture released the lock after its bounded
 # hold and the shell then reached its own terminal in the Default mode (a delivery after the cutscene, never during it).
 $cutsceneDeferred=$Scenario-ceq'chunk6a-loading-cutscene'-and[string]$Case.outcome-ceq'delivered'-and$null-ne(ClosureProp $Case 'cutsceneHoldExpired')
 $before=$Case.before
 if($Case.start.isAdjacent-ne$false-or[string]$before.relationshipState-cne'Unmounted'-or$before.partyInCombat-ne$true){ClosureFail 'the Mount did not start outside transition reach from an unmounted pair in combat'}
 Assert-KmcChunk6aClosureClick $Case.click 'approach invalidation'
 $trigger=ClosureProp $Case 'trigger'
 Assert-KmcChunk6aClosureTrigger $trigger 'approach invalidation'
 if([string]$trigger.state.relationshipState-cne'Unmounted'){ClosureFail 'the pair was already mounted at the stimulus'}
 $riderId=[string]$trigger.state.riderActor.id;$mountId=[string]$trigger.state.mountActor.id
 if([string]::IsNullOrEmpty($riderId)-or[string]::IsNullOrEmpty($mountId)-or$riderId-ceq$mountId){ClosureFail 'the trigger state lacks the exact pair'}
 $stimulus=ClosureProp $Case 'stimulus'
 if($null-eq$stimulus-or$stimulus.count-ne1-or$Case.stimulusCount-ne1-or(ClosureLong $stimulus.frameAfter 'frameAfter')-lt(ClosureLong $stimulus.frameBefore 'frameBefore')-or(ClosureLong $stimulus.gameTicksAfter 'gameTicksAfter')-lt(ClosureLong $stimulus.gameTicksBefore 'gameTicksBefore')){ClosureFail 'the stimulus was not delivered exactly once at its frame'}
 if($stimulus.commandBefore.finished-ne$false-or$stimulus.commandBefore.acted-ne$false-or$stimulus.commandBefore.id-ne$trigger.commandObject){ClosureFail 'the exact pending command had already settled before the stimulus'}
 $terminal=ClosureProp $Case 'terminal';$after=ClosureProp $Case 'after';$terminalCommand=ClosureProp $Case 'terminalCommand'
 if($null-eq$terminal-or$null-eq$after-or$null-eq$terminalCommand-or$terminalCommand.finished-ne$true-or$terminalCommand.id-ne$trigger.commandObject){ClosureFail 'no terminal of the exact command was observed'}
 if($Case.allocationTraceComplete-ne$true-or$Case.diagnosticInterruptCount-ne0){ClosureFail 'the native allocation trace is incomplete or the command was interrupted diagnostically'}
 if($Case.castRequestDelta-ne1){ClosureFail 'the window holds other than the one native cast request'}
 $delta=$Case.ledgerDelta
 if($cutsceneDeferred){
  # Delivered after the cutscene: exactly one accepted Mount, nothing in flight, both containers empty, one shell.
  if($after.transitionInFlight-ne$false-or$after.riderCommandsEmpty-ne$true-or$after.horseCommandsEmpty-ne$true-or$Case.relationshipShellsDelta-ne1){ClosureFail 'the deferred delivery left command, transition or shell residue'}
  if([string]$after.relationshipState-cne'Mounted'-or$delta.acceptedMount-ne1-or$delta.admittedMount-ne1-or$delta.refusedVoluntary-ne0-or$delta.forcedDetach-ne0-or$Case.dispatchAcceptedDelta-ne1-or$Case.generationDelta-ne1-or$terminalCommand.acted-ne$true){ClosureFail 'the deferred delivery is not exactly one accepted Mount'}
 } else {
  Assert-KmcChunk6aClosureNoResidueState $after $Case 'the invalidation sequence'
  if([string]$after.relationshipState-cne'Unmounted'-or$delta.acceptedMount-ne0-or$Case.dispatchAcceptedDelta-ne0-or$delta.acceptedDismount-ne0-or$delta.admittedDismount-ne0){ClosureFail 'the invalidated Mount was delivered'}
 }
 if($Case.pairPrepareCallbacks-ne0){ClosureFail 'a native preparation ran during the invalidation window'}
 $outcome=[string]$Case.outcome
 switch -Regex -CaseSensitive ($outcome){
  '^unacted-(Interrupt|Fail)$' {
   if($terminalCommand.acted-ne$false-or$Case.pairCostCallbacks-ne0-or$delta.admittedMount-ne0-or$delta.refusedVoluntary-ne0){ClosureFail 'an unacted terminal committed a cost, an admission or a refusal'}
   Assert-KmcChunk6aClosureCooldownsNotRaised $before $after 'the unacted outcome'
  }
  '^acted-not-mounted$' {
   if($terminalCommand.acted-ne$true){ClosureFail 'an acted outcome names an unacted terminal'}
  }
  '^delivered$' {
   if(-not$cutsceneDeferred){ClosureFail 'the invalidated Mount was delivered'}
  }
  default { ClosureFail ('unlawful outcome '+$outcome) }
 }
 $restoration=ClosureProp $Case 'restoration'
 $generationExpected=0;$detachExpected=0;$shape=$null
 switch -CaseSensitive ($Scenario) {
  'chunk6a-left-area' {
   if([string]$stimulus.contract-cne'one-native-entity-is-in-game-false-on-the-exact-mount'-or[string]$stimulus.token-cne'06007E95'-or$stimulus.mountBefore.inGame-ne$true-or$stimulus.mountAfter.inGame-ne$false-or$stimulus.riderAfter.inGame-ne$true-or[string]$stimulus.mountAfter.id-cne$mountId){ClosureFail 'the mount did not leave the area through the exact native setter'}
   if($null-eq$restoration-or$restoration.restored-ne$true-or$Case.restorationCount-ne1-or$after.mountActor.inGame-ne$true-or$after.mountActor.viewPresent-ne$true-or$after.mountActor.viewBound-ne$true-or$after.mountActor.inState-ne$true){ClosureFail 'the mount did not return to the area exactly once'}
  }
  'chunk6a-view-agent-lost' {
   $lease=ClosureProp $stimulus 'lease'
   if([string]$stimulus.contract-cne'one-authored-beast-shape-view-replacement-on-the-exact-rider'-or$null-eq$lease-or[string]$lease.blueprint-cne'00d8fbe9cf61dc24298be8d95500c84b'-or[string]$lease.actor-cne$riderId-or$lease.applyCalls-ne1){ClosureFail 'the rider view was not replaced through the exact authored native effect'}
   if($stimulus.riderAfter.polymorphed-ne$true-or$stimulus.riderAfter.viewObject-eq$stimulus.riderBefore.viewObject-or$lease.afterApply.view-eq$lease.before.view){ClosureFail 'the native view replacement was not observed on the rider'}
   $restoredLease=ClosureProp $restoration 'lease'
   if($null-eq$restoration-or$restoration.restored-ne$true-or$null-eq$restoredLease-or$restoredLease.restored-ne$true-or$restoredLease.removeCalls-ne1-or$restoredLease.originalRetired-ne$true-or$restoredLease.replacementRetired-ne$true-or$restoredLease.factDisposed-ne$true-or$restoredLease.listenersRemaining-ne0-or$Case.restorationCount-ne1){ClosureFail 'the authored view effect was not removed exactly once with every owned component retired'}
   if($after.riderActor.polymorphed-ne$false-or$after.riderActor.viewBound-ne$true-or$after.riderActor.viewPresent-ne$true){ClosureFail 'the rider view was not restored'}
  }
  'chunk6a-loading-cutscene' {
   # The engine's own cutscene entry: the counting-guard lock (Game.SetCutsceneLock 06000CE6) starts and holds the Cutscene
   # mode; the same entry releases it.
   if([string]$stimulus.contract-cne'one-native-cutscene-lock-with-its-cutscene-game-mode'-or[string]$stimulus.method-cne'Kingmaker.Game.SetCutsceneLock'-or[string]$stimulus.token-cne'06000CE6'-or[string]$stimulus.modeBefore-cne'Default'-or$stimulus.lockBefore-ne$false-or$stimulus.lockAfterRequest-ne$true){ClosureFail 'the cutscene lock was not engaged through the exact native entry'}
   if($null-eq$restoration-or$restoration.restored-ne$true-or[string]$restoration.method-cne'Kingmaker.Game.SetCutsceneLock'-or[string]$restoration.token-cne'06000CE6'-or$restoration.lockNow-ne$false-or[string]$restoration.modeNow-cne'Default'-or$Case.restorationCount-ne1-or$after.cutsceneLock-ne$false-or[string]$after.currentMode-cne'Default'){ClosureFail 'the cutscene lock was not released exactly once through the exact native entry'}
   $releaseFrame=ClosureLong $restoration.releaseFrame 'releaseFrame';$stimulusFrame=ClosureLong $stimulus.frameAfter 'stimulus frame';$terminalFrame=ClosureLong $terminal.frame 'terminal frame'
   $heldSamples=@(@($Case.samples)|Where-Object {$null-ne$_-and(ClosureLong $_.frame 'sample frame')-gt($stimulusFrame+1)-and(ClosureLong $_.frame 'sample frame')-lt$releaseFrame})
   if($heldSamples.Count-lt1-or@($heldSamples|Where-Object {[string](ClosureProp $_ 'currentMode')-cne'Cutscene'-or(ClosureProp $_ 'cutsceneLock')-ne$true}).Count-ne0){ClosureFail 'the native Cutscene mode and lock were not held for the whole stimulus window'}
   $delivers=@(@((ClosureProp (ClosureProp $Case 'commandWindow') 'samples'))|Where-Object {$null-ne$_-and[string](ClosureProp $_ 'boundary')-ceq'deliver'})
   if(@($delivers|Where-Object {(ClosureLong (ClosureProp $_ 'frame') 'deliver frame')-le$releaseFrame}).Count-ne0){ClosureFail 'the Mount was delivered while the native cutscene lock was held'}
   if($cutsceneDeferred){
    $hold=$Case.cutsceneHoldExpired
    if((ClosureLong $hold.frame 'hold frame')-lt($stimulusFrame+(ClosureLong $hold.holdFrames 'holdFrames'))-or(ClosureLong $hold.holdFrames 'holdFrames')-lt60-or$hold.command.finished-ne$false-or$releaseFrame-lt(ClosureLong $hold.frame 'hold frame')-or$terminalFrame-le$releaseFrame-or[string]$hold.state.currentMode-cne'Cutscene'){ClosureFail 'the deferred delivery did not follow the bounded hold and the lock release'}
    if($delivers.Count-ne1){ClosureFail 'the deferred delivery is not exactly one delivery after the release'}
   } else {
    if($null-ne(ClosureProp $Case 'cutsceneHoldExpired')){ClosureFail 'the bounded hold expired but the shell did not deliver after the release'}
    if($terminalFrame-gt$releaseFrame-or$terminal.cutsceneLock-ne$true){ClosureFail 'the refusal terminal was not reached under the native cutscene lock'}
   }
  }
  'chunk6a-generation-change' {
   if([string]$stimulus.contract-cne'one-owned-diagnostic-generation-invalidation-before-the-shell-generation-check'-or$stimulus.armed-ne$true-or$before.generationFaultArmed-ne$false){ClosureFail 'the diagnostic generation invalidation was not armed once from a disarmed seam'}
   if($Case.diagnosticGenerationInvalidations-ne1-or$Case.generationDelta-ne1-or$null-eq$restoration-or$restoration.restored-ne$true-or$restoration.armedAtRestoration-ne$false-or$after.generationFaultArmed-ne$false){ClosureFail 'the diagnostic generation invalidation was not consumed exactly once and disarmed'}
   if([string]$Case.lastShellRefusal-cne'The mounted relationship changed after this transition was requested.'){ClosureFail 'the stale shell was not refused by the generation guard'}
   if($Case.dispatchRejectedDelta-lt1){ClosureFail 'the stale shell delivery was not refused'}
   # The stale shell is retired at delivery resolution, before any admission: nothing is admitted or refused
   # in the voluntary ledger and nothing is cleaned up.
   $generationExpected=1;$shape='rejected-before-admission'
  }
  'chunk6a-injected-exception' {
   if([string]$stimulus.contract-cne'one-owned-diagnostic-exception-at-the-native-mount-execution-boundary'-or$stimulus.armed-ne$true-or$before.mountExecutionFaultArmed-ne$false){ClosureFail 'the diagnostic Mount execution fault was not armed once from a disarmed seam'}
   if($null-eq$restoration-or$restoration.restored-ne$true-or$restoration.armedAtRestoration-ne$false-or$after.mountExecutionFaultArmed-ne$false){ClosureFail 'the diagnostic fault was not consumed exactly once and disarmed'}
   if([string]$Case.feedback-cnotlike'Native Mount Companion failed closed: *'){ClosureFail 'the Mount execution did not fail closed'}
   if($outcome-cne'acted-not-mounted'){ClosureFail 'the injected execution fault requires the acted native cast that reached the owned boundary'}
   # Fail-closed: the admitted transition settles as one refusal and the exception cleanup announces one forced
   # detach with nothing attached; the relationship generation never moves.
   $detachExpected=1;$shape='failed-closed'
  }
  default {
   $subject=if($Scenario-ceq'chunk6a-rider-death-approach'){'riderActor'}else{'mountActor'};$other=if($subject-ceq'riderActor'){'mountActor'}else{'riderActor'}
   $policy=ClosureProp $stimulus 'policy'
   if([string]$stimulus.contract-cne'one-native-lethal-ruledeal-damage-under-permanent-death-policy'-or$stimulus.damageDispatches-ne1-or$stimulus.nativeDamageBeforeDifficulty-ne$stimulus.requestedDamage-or(ClosureLong $stimulus.requestedDamage 'requestedDamage')-lt1-or$null-eq$policy-or$policy.permanentDeathFixture-ne$true-or$policy.effective.trueDeath-ne$true){ClosureFail 'the lethal native damage was not delivered exactly once under the permanent death policy'}
   if([string]$stimulus.subjectId-cne$(if($subject-ceq'riderActor'){$riderId}else{$mountId})){ClosureFail 'the lethal damage did not target the exact subject'}
   $subjectBefore=if($subject-ceq'riderActor'){'riderBefore'}else{'mountBefore'}
   if($stimulus.$subjectBefore.dead-ne$false-or$terminal.$subject.dead-ne$true-or$terminal.$other.dead-ne$false-or$terminal.$other.conscious-ne$true){ClosureFail 'the native death did not land on the exact subject alone'}
   if($Case.pairCostCallbacks-ne0-or$delta.admittedMount-ne0-or$delta.refusedVoluntary-ne0){ClosureFail 'the forced detach paid or admitted a voluntary transition'}
   # Nothing was attached when the subject died (the pair was pending, never mounted), so the forced-detach ledger has
   # nothing to record: the one cleanup is the engine's own interruption of the dead unit's command; the voluntary and
   # involuntary counters stay exactly where the fixture left them (preview.206 stages 6 and 7).
   if($delta.forcedDetach-ne0-or$delta.duplicateSuppressed-ne0){ClosureFail 'a death during the pending approach moved the transition ledger although nothing was attached'}
   $restoredPolicy=ClosureProp $restoration 'policy';$resurrection=ClosureProp $restoration 'resurrection'
   if($null-eq$restoration-or$restoration.restored-ne$true-or$null-eq$restoredPolicy-or$null-eq(ClosureProp $restoredPolicy 'restoration')-or$restoredPolicy.restoration.restored-ne$true-or$Case.restorationCount-ne1){ClosureFail 'the death policy lease was not restored exactly once'}
   # The fixture's own lethal stimulus is restored in-process through the engine's resurrection entry after the death was
   # observed; restoration is complete only when the subject is conscious, undamaged and in state.
   if($restoration.subjectDeadBefore-ne$true-or$restoration.resurrected-ne$true-or$null-eq$resurrection-or[string]$resurrection.method-cne'Kingmaker.UnitLogic.UnitDescriptor.ResurrectAndFullRestore'-or[string]$resurrection.token-cne'06001F12'-or$restoration.subjectDead-ne$false-or$restoration.subjectConscious-ne$true-or$restoration.subjectDamage-ne0-or$restoration.subjectInState-ne$true){ClosureFail 'the dead subject was not restored exactly once through the exact native resurrection'}
   if($after.$subject.dead-ne$false-or$after.$subject.conscious-ne$true-or$after.$subject.inState-ne$true-or$after.$subject.viewBound-ne$true-or$after.$other.dead-ne$false){ClosureFail 'the pair was not whole after the restoration'}
  }
 }
 if($outcome-ceq'acted-not-mounted'){
  # An acted native cast that did not mount has exactly one lawful ledger shape: rejected at delivery resolution
  # before any admission (nothing admitted, nothing refused, at least one rejected dispatch), refused at the owned
  # admission (one admission settled as one refusal, nothing attached), compensated after an attach whose
  # adoption was refused (the lifecycle rule: one admission, one refusal, one compensating forced detach, one
  # generation), or the diagnostic fail-closed path. The native cases take whichever shape the engine produced;
  # the diagnostic cases are pinned to theirs.
  $observed=if($delta.admittedMount-eq0-and$delta.refusedVoluntary-eq0-and$Case.dispatchRejectedDelta-ge1-and$delta.forcedDetach-eq0-and$Case.generationDelta-eq$generationExpected){'rejected-before-admission'}
   elseif($delta.admittedMount-eq1-and$delta.refusedVoluntary-eq1-and$delta.forcedDetach-eq0-and$Case.generationDelta-eq0-and$after.compensatedMounts-eq$before.compensatedMounts){'refused-at-admission'}
   elseif($delta.admittedMount-eq1-and$delta.refusedVoluntary-eq1-and$delta.forcedDetach-eq1-and$Case.generationDelta-eq1-and$after.compensatedMounts-eq($before.compensatedMounts+1)){'compensated'}
   elseif($delta.admittedMount-eq1-and$delta.refusedVoluntary-eq1-and$delta.forcedDetach-eq1-and$Case.generationDelta-eq0-and$after.compensatedMounts-eq$before.compensatedMounts){'failed-closed'}
   else{$null}
  if($null-eq$observed){ClosureFail 'the acted outcome has no lawful ledger shape (admission, refusal, forced detach, generation and compensation do not agree)'}
  if($null-ne$shape-and$observed-cne$shape){ClosureFail ('the acted outcome took the shape '+$observed+' instead of '+$shape)}
  if($death-and$observed-cne'rejected-before-admission'){ClosureFail 'a death during the approach must be refused before any voluntary admission'}
 } elseif($cutsceneDeferred) {
  if($null-ne$shape){ClosureFail 'a diagnostic case cannot defer its delivery'}
 } else {
  if($null-ne$shape){ClosureFail ('the diagnostic case requires its acted outcome, observed '+$outcome)}
  if($Case.generationDelta-ne$generationExpected){ClosureFail 'the relationship generation moved other than expected'}
  if($delta.forcedDetach-ne$detachExpected){ClosureFail ('the forced detach count moved by '+$delta.forcedDetach+' instead of '+$detachExpected)}
 }
}

# ---------------------------------------------------------------------------------------------
# Turn-based boundaries (schema 30).
# ---------------------------------------------------------------------------------------------
function Assert-KmcChunk6aTurnEnd($Artifact) {
 $Case=ClosureProp $Artifact.observations 'chunk6aTurnEndApproach'
 if($null-eq$Case){ClosureFail 'the turn-end evidence is absent'}
 $rows=@($Artifact.rows|Where-Object name -CEQ 'CM04-turn-end')
 if($rows.Count-ne1-or[string]$rows[0].status-cne'PASS'){ClosureFail 'the exact mandatory row CM04-turn-end is absent'}
 if(-not(ClosureSameJson $rows[0].evidence $Case)){ClosureFail 'the row evidence differs from the recorded case'}
 if([string]$Case.contract-cne'native-end-turn-during-exact-turn-based-mount-approach'-or[string]$Case.boundary-cne'turn-end'){ClosureFail 'the contract differs'}
 $before=$Case.before
 if($Case.start.isAdjacent-ne$false-or[string]$before.relationshipState-cne'Unmounted'-or$before.turnBasedCombat-ne$true-or$before.turnBasedSetting-ne$true-or$before.partyInCombat-ne$true){ClosureFail 'the Mount did not start outside transition reach in turn-based combat'}
 $riderId=[string]$before.rider.actor;$mountId=[string]$before.mount.actor
 if([string]::IsNullOrEmpty($riderId)-or[string]::IsNullOrEmpty($mountId)-or$riderId-ceq$mountId){ClosureFail 'the before state lacks the exact pair'}
 if([string]$before.turnActor-cne$riderId-or[string]$before.turnStatus-cne'Acting'){ClosureFail 'the Mount was not clicked on the rider''s own acting turn'}
 $trigger=ClosureProp $Case 'trigger'
 Assert-KmcChunk6aClosureTrigger $trigger 'turn end'
 $input=ClosureProp $Case 'endInput';$beforeEnd=ClosureProp $Case 'beforeEndInput';$afterInput=ClosureProp $Case 'afterEndInput'
 # The native End Turn input while the rider still acts is the UI's (InGameInputLayerView.OnTurnBasedEndTurn 06005D30, the
 # in-game menu button 06003E87): admitted through TurnController.CanEndTurn (06000C4A), delivered as ForceToEnd(true)
 # (06000C47), which forfeits the turn with the engine's own Standard/Move/Swift debt and interrupts the running command.
 if($null-eq$input-or$Case.endInputCount-ne1-or[string]$input.method-cne'TurnBased.Controllers.TurnController.ForceToEnd'-or[string]$input.token-cne'06000C47'-or$input.argument-ne$true-or[string]$input.admissionToken-cne'06000C4A'-or$input.count-ne1){ClosureFail 'the one native End Turn input was not issued exactly once through the exact UI path'}
 if($null-eq$beforeEnd-or$null-eq$afterInput-or[string]$beforeEnd.turnActor-cne$riderId-or$beforeEnd.turnIsActing-ne$true-or$beforeEnd.turnCanEnd-ne$true-or$beforeEnd.waitingForUi-ne$false-or$beforeEnd.turnObject-ne$before.turnObject-or[string]$beforeEnd.relationshipState-cne'Unmounted'){ClosureFail 'the End Turn input was not issued on the rider''s own unchanged acting turn'}
 if($beforeEnd.command.finished-ne$false-or$beforeEnd.command.acted-ne$false-or$beforeEnd.command.id-ne$trigger.commandObject){ClosureFail 'the exact pending shell had already settled before the End Turn input'}
 $terminal=ClosureProp $Case 'terminal';$after=ClosureProp $Case 'after';$terminalCommand=ClosureProp $Case 'terminalCommand'
 if($null-eq$terminal-or$null-eq$after-or$null-eq$terminalCommand-or$terminalCommand.finished-ne$true-or$terminalCommand.acted-ne$false-or$terminalCommand.id-ne$trigger.commandObject){ClosureFail 'the pending shell did not retire unacted'}
 if($terminal.turnObject-eq$beforeEnd.turnObject-or$after.turnObject-eq$beforeEnd.turnObject){ClosureFail 'the rider''s native turn did not end'}
 if([string]$Case.outcome-cnotmatch'^unacted-(Interrupt|Fail)$'){ClosureFail ('unlawful outcome '+[string]$Case.outcome)}
 Assert-KmcChunk6aClosureNoResidueState $after $Case 'the turn end'
 if([string]$after.relationshipState-cne'Unmounted'){ClosureFail 'the turn end delivered the Mount'}
 $delta=$Case.ledgerDelta
 foreach($name in @('admittedMount','acceptedMount','admittedDismount','acceptedDismount','refusedVoluntary','forcedDetach')){ if($delta.$name-ne0){ClosureFail ('the turn end moved the transition ledger: '+$name)} }
 # KMC charged nothing (no pair cost callback); the native ForceToEnd(true) forfeit itself writes the engine's own turn-end
 # debt, so the cooldown values are recorded, not bounded, here.
 if($Case.dispatchAcceptedDelta-ne0-or$Case.generationDelta-ne0-or$Case.pairCostCallbacks-ne0-or$Case.pairPrepareCallbacks-ne0-or$Case.allocationTraceComplete-ne$true){ClosureFail 'the turn end charged, prepared or delivered'}
 $roundDelta=ClosureLong $Case.roundDelta 'roundDelta'
 if($roundDelta-lt0-or$roundDelta-gt1){ClosureFail 'the native round advanced other than by at most one'}
 if($after.partyInCombat-ne$true-or$after.riderInCombat-ne$true-or$after.horseInCombat-ne$true-or$after.turnBasedCombat-ne$true-or$after.targetPresent-ne$true-or[string]$after.rider.actor-cne$riderId-or[string]$after.mount.actor-cne$mountId){ClosureFail 'the actors or the encounter were not valid after the turn end'}
}
function Assert-KmcChunk6aModeExit($Artifact) {
 $Case=ClosureProp $Artifact.observations 'chunk6aModeExit'
 if($null-eq$Case){ClosureFail 'the mode-exit evidence is absent'}
 $rows=@($Artifact.rows|Where-Object name -CEQ 'CM04-mode-exit')
 if($rows.Count-ne1-or[string]$rows[0].status-cne'PASS'){ClosureFail 'the exact mandatory row CM04-mode-exit is absent'}
 if(-not(ClosureSameJson $rows[0].evidence $Case)){ClosureFail 'the row evidence differs from the recorded case'}
 if([string]$Case.contract-cne'native-turn-based-mode-exit-while-mounted-on-adopted-turn'-or[string]$Case.boundary-cne'mode-exit'-or[string]$Case.mountWindow-cne'positive-mount'){ClosureFail 'the contract differs'}
 $before=$Case.before
 if([string]$before.relationshipState-cne'Mounted'-or$before.turnBasedCombat-ne$true-or$before.turnBasedSetting-ne$true-or$before.partyInCombat-ne$true-or[string]::IsNullOrEmpty([string](ClosureProp $before 'pairedIdentity'))-or$before.transitionInFlight-ne$false){ClosureFail 'the pair was not mounted and settled on an adopted turn-based activation before the exit'}
 $input=ClosureProp $Case 'exitInput';$afterInput=ClosureProp $Case 'afterExitInput'
 if($null-eq$input-or$null-eq$afterInput-or$Case.exitInputCount-ne1-or[string]$input.method-cne'SettingsEntityBase.OnInvokeUpdateCallback'-or[string]$input.token-cne'06003359'-or[string]$input.cacheToken-cne'04002275'-or$input.temporaryValue-ne$false-or$input.settingAfter-ne$false-or$afterInput.turnBasedSetting-ne$false){ClosureFail 'the native mode exit was not dispatched exactly once through the exact settings callback'}
 $terminal=ClosureProp $Case 'terminal';$after=ClosureProp $Case 'after'
 if($null-eq$terminal-or$null-eq$after-or$terminal.turnBasedCombat-ne$false-or$after.turnBasedCombat-ne$false-or$after.partyInCombat-ne$true){ClosureFail 'the native controller did not leave turn-based combat inside the live encounter'}
 # Forfeited exactly once: the product observed the exit while mounted (its exit AI lease is armed through both of its
 # observation paths and reasserted exactly once), released the partner context and granted no fresh paired activation;
 # the pair stays mounted in real time without any KMC charge (the engine's own real-time cooldowns are recorded, not
 # bounded: the controller converts the turn-based state when it leaves turn-based combat).
 if([string]$after.relationshipState-cne'Mounted'-or$after.exitAiLeaseArmed-le$before.exitAiLeaseArmed-or$after.exitAiLeaseAttempts-le$before.exitAiLeaseAttempts-or$after.exitAiLeaseMutations-ne($before.exitAiLeaseMutations+1)){ClosureFail 'the mode exit while mounted was not observed and reasserted exactly once by the product'}
 $pairedAfter=[string](ClosureProp $after 'pairedIdentity')
 if(-not[string]::IsNullOrEmpty($pairedAfter)-and$pairedAfter-cne[string]$before.pairedIdentity){ClosureFail 'the mode exit granted a fresh paired activation'}
 if($null-ne(ClosureProp $after 'partnerContextActor')){ClosureFail 'the mode exit retained the turn-based partner context'}
 if($after.transitionInFlight-ne$false-or$after.riderCommandsEmpty-ne$true-or$after.horseCommandsEmpty-ne$true){ClosureFail 'the mode exit left command or transition residue'}
 if($Case.pairCostCallbacks-ne0-or$Case.pairPrepareCallbacks-ne0-or$Case.allocationTraceComplete-ne$true){ClosureFail 'the mode exit charged or prepared the pair again'}
 foreach($name in @('admittedMount','acceptedMount','admittedDismount','acceptedDismount','refusedVoluntary','forcedDetach')){ if($Case.ledgerDelta.$name-ne0){ClosureFail ('the mode exit moved the transition ledger: '+$name)} }
 $restore=ClosureProp $Case 'restoreInput';$afterRestore=ClosureProp $Case 'afterRestore'
 if($null-eq$restore-or$restore.restoreDeliveryCompleted-ne$true-or$restore.persistedValueUnchanged-ne$true-or$restore.settingAfter-ne$true){ClosureFail 'the declared turn-based mode was not restored through the same exact path without touching persisted settings'}
 if($null-eq$afterRestore-or$afterRestore.turnBasedCombat-ne$true-or$afterRestore.turnBasedSetting-ne$true-or[string]$afterRestore.relationshipState-cne'Mounted'-or$afterRestore.exitAiLeaseMutations-ne$after.exitAiLeaseMutations){ClosureFail 'the encounter did not return to turn-based combat with the pair mounted and no second reassertion'}
}

# ---------------------------------------------------------------------------------------------
# P04 closure checkpoints (persistence-observations.jsonl). Rows carry a ClosureObservation (the foundation
# observation plus persistence, loading, view, component and command facts) directly in `detail`, or a
# real-time observation whose foundation sits under `detail.foundation`.
# ---------------------------------------------------------------------------------------------
function Get-KmcChunk6aClosureCases { @('combat-mount-routes','combat-mount-unsettled-save','combat-mount-area-reload','pending-mount-area') }
function Test-KmcChunk6aClosureCase([string]$Case) { [string]$Case -cin @(Get-KmcChunk6aClosureCases) }
function Get-KmcChunk6aClosureRowName([string]$Scenario,[string]$Case) {
 if($Scenario-ceq'persistence-p07-save'-and$Case-ceq'area-reload'){return 'P07-save-area-reload'}
 if($Scenario-cne'persistence-p04-save'-or-not(Test-KmcChunk6aClosureCase $Case)){ClosureFail ('no closure row for '+$Scenario+'/'+$Case)}
 'P04-save-'+$Case
}
function Get-KmcChunk6aClosureRowNames { @(@(Get-KmcChunk6aClosureCases|ForEach-Object {'P04-save-'+$_})+@('P07-save-area-reload')) }
function Get-KmcChunk6aClosureIdMap {
 [ordered]@{
  'CM07-save-slot-routes'=@('persistence-p04-save','combat-mount-routes')
  'CM07-unsettled-save-deferred'=@('persistence-p04-save','combat-mount-unsettled-save')
  'CM07-area-reload'=@('persistence-p04-save','combat-mount-area-reload')
  'CM04-area-session-transition'=@('persistence-p04-save','pending-mount-area')
  'CM08-area-restoration'=@('persistence-p07-save','area-reload')
 }
}
function Test-KmcChunk6aClosureId([string]$Id) { (Get-KmcChunk6aClosureIdMap).Contains([string]$Id) }
function Get-KmcChunk6aClosureTerminalKind([string]$Case) {
 switch -CaseSensitive ($Case) {
  'combat-mount-routes' {'routes-complete'}
  'combat-mount-unsettled-save' {'unsettled-save-complete'}
  'combat-mount-area-reload' {'combat-area-reload-complete'}
  'pending-mount-area' {'pending-mount-area-complete'}
  'area-reload' {'native-write-complete'}
  default {ClosureFail ('no closure terminal for '+$Case)}
 }
}
function Get-KmcChunk6aClosureFoundationOf($Row) {
 $d=ClosureProp $Row 'detail'
 if($null-eq$d){return $null}
 if($null-ne(ClosureProp $d 'transitionCounters')){return $d}
 $f=ClosureProp $d 'foundation';if($null-ne$f){return $f}
 $actual=ClosureProp $d 'actual';if($null-ne$actual){ if($null-ne(ClosureProp $actual 'transitionCounters')){return $actual}; $f=ClosureProp $actual 'foundation';if($null-ne$f){return $f} }
 $null
}
function Assert-KmcChunk6aClosureIdentity($Request,$Rows,$GameResult,[string]$Case) {
 $rows=@($Rows)
 if($rows.Count-lt6-or$rows.Count-gt28){ClosureFail 'row count is out of bounds'}
 $initial=@($rows|Where-Object kind -CEQ 'initial');if($initial.Count-ne1){ClosureFail 'no unique initial row'}
 $riderId=[string]$initial[0].rider.Id;$mountId=[string]$initial[0].mount.Id
 if([string]::IsNullOrEmpty($riderId)-or[string]::IsNullOrEmpty($mountId)-or$riderId-ceq$mountId){ClosureFail 'initial row lacks the exact pair'}
 foreach($r in $rows){
  if($r.runId-cne$Request.runId-or$r.scenario-cne$Request.scenario-or$r.source-cne$Request.commit-or$r.dll-cne$Request.dllSha256-or$r.processId-ne$GameResult.processId-or[string]$r.checkpoint-cne$Case){ClosureFail ('row identity differs at '+$r.kind)}
  if($null-eq$r.controls-or$r.controls.DuplicateFactCount-ne0){ClosureFail ('a duplicate owned control exists at '+$r.kind)}
  if($r.native.tbSetting-ne$false-or$r.native.tbInitialized-ne$false){ClosureFail ('a real-time row ran in turn-based mode at '+$r.kind)}
  if($null-eq$r.rider-or$null-eq$r.mount-or[string]$r.rider.Id-cne$riderId-or[string]$r.mount.Id-cne$mountId){ClosureFail ('row actors differ at '+$r.kind)}
  if([string]$r.relationship-cnotin@('Mounted','Unmounted')){ClosureFail ('a row relationship is outside mounted and unmounted at '+$r.kind)}
 }
 if(@($rows|Where-Object kind -CIn @('assertion-failed','scenario-failed')).Count-ne0){ClosureFail 'a failure row is present'}
 [pscustomobject]@{riderId=$riderId;mountId=$mountId}
}
function Get-KmcChunk6aClosureKind($Rows,[string]$Kind,[int]$Expected=1) {
 $found=@($Rows|Where-Object kind -CEQ $Kind)
 if($found.Count-ne$Expected){ClosureFail ('the row '+$Kind+' is present '+$found.Count+' times instead of '+$Expected)}
 $found
}
function Assert-KmcChunk6aClosureOrder($Rows,[string[]]$Kinds) {
 $names=@($Rows|ForEach-Object {[string]$_.kind});$last=-1
 foreach($k in $Kinds){ $i=[Array]::IndexOf($names,$k); if($i-le$last){ClosureFail ('the row '+$k+' is missing or out of order')}; $last=$i }
}
function Assert-KmcChunk6aClosureArchive([string]$RunId,$Detail,[string]$Where) {
 if([string]$Detail.sha256-cnotmatch'^[0-9a-f]{64}$'-or(ClosureNumber $Detail.length 'length')-le0){ClosureFail ($Where+' lacks an exact archive identity')}
 $leaf=[IO.Path]::GetFileName([string]$Detail.path)
 if([string]::IsNullOrEmpty($leaf)-or$leaf-cnotmatch'^[A-Za-z0-9_]+\.zks$'){ClosureFail ($Where+' names an invalid archive leaf')}
 $staged=Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$RunId+'/Saved Games/'+$leaf)
 if(-not(Test-Path -LiteralPath $staged -PathType Leaf)){ClosureFail ($Where+' staged archive is absent: '+$leaf)}
 if((Get-KmcSha256 $staged)-cne[string]$Detail.sha256-or(Get-Item -LiteralPath $staged).Length-ne[long]$Detail.length){ClosureFail ($Where+' staged archive differs from the recorded write')}
 $leaf
}
function Assert-KmcChunk6aClosureSnapshot($Snapshot,[string]$RiderId,[string]$MountId,$Request,[string]$Where) {
 if($null-eq$Snapshot-or$Snapshot.Mounted-ne$true-or[string]$Snapshot.Rider.Id-cne$RiderId-or[string]$Snapshot.Mount.Id-cne$MountId-or$null-eq(ClosureProp $Snapshot 'Combat')-or$Snapshot.Combat.TurnBased-ne$false-or$Snapshot.SchemaVersion-ne2-or[string]$Snapshot.CampaignId-cne[string]$Request.fixture.working.gameId-or[string]$Snapshot.AreaId-cne[string]$Request.fixture.working.area){ClosureFail ($Where+' archive does not carry the settled combat-mounted pair of this world')}
}
function Assert-KmcChunk6aClosureNoResidue($Detail,[string]$Where) {
 $bad=@()
 if([string]$Detail.relationship-cne'Unmounted'){$bad+='relationship'}
 if($Detail.transitionInFlight-ne$false){$bad+='transitionInFlight'}
 if(@(ClosureProp $Detail 'riderOverrideComponents').Count-ne0){$bad+='riderOverrideComponents'}
 if(@(ClosureProp $Detail 'mountOverrideComponents').Count-ne0){$bad+='mountOverrideComponents'}
 if($Detail.runtimeMovementAgent-ne0){$bad+='runtimeMovementAgent'}
 if($Detail.riderAgentOverride-ne0){$bad+='riderAgentOverride'}
 if($Detail.liveRiderCommandsEmpty-ne$true){$bad+='liveRiderCommandsEmpty'}
 if($Detail.liveMountCommandsEmpty-ne$true){$bad+='liveMountCommandsEmpty'}
 if($Detail.duplicateFactCount-ne0){$bad+='duplicateFactCount'}
 if($Detail.riderViewBound-ne$true){$bad+='riderViewBound'}
 if($Detail.mountViewBound-ne$true){$bad+='mountViewBound'}
 if($Detail.nativeActorCounts.rider-ne1){$bad+='nativeActorCounts.rider'}
 if($Detail.nativeActorCounts.mount-ne1){$bad+='nativeActorCounts.mount'}
 if($null-ne(ClosureProp $Detail 'pairedIdentity')){$bad+='pairedIdentity'}
 if($null-ne(ClosureProp $Detail 'partnerContextActor')){$bad+='partnerContextActor'}
 if($Detail.riderStockAgentEnabled-ne$true){$bad+='riderStockAgentEnabled'}
 if($Detail.mountStockAgentEnabled-ne$true){$bad+='mountStockAgentEnabled'}
 if($bad.Count-ne0){ClosureFail ($Where+' retained relationship, movement, presentation, activation or command residue: '+($bad -join ', '))}
}
function Assert-KmcChunk6aClosureReload($Detail,[string]$Where) {
 $bad=@()
 if($Detail.loadingObserved-ne$true){$bad+='loadingObserved'}
 if((ClosureLong $Detail.loadingFrames 'loadingFrames')-le0){$bad+='loadingFrames'}
 if($Detail.sameWorld-ne$true){$bad+='sameWorld'}
 if([string]$Detail.area-cne[string]$Detail.sourceArea){$bad+='area'}
 if($Detail.areaPending-ne$false){$bad+='areaPending'}
 if($Detail.suspensions-ne0){$bad+='suspensions'}
 if($Detail.resumes-ne0){$bad+='resumes'}
 if($Detail.areaRefused-ne0){$bad+='areaRefused'}
 if([string]$Detail.currentMode-cne'Default'){$bad+='currentMode'}
 if($Detail.loadingInProcess-ne$false){$bad+='loadingInProcess'}
 if($Detail.fixtureReleased-ne$true){$bad+='fixtureReleased'}
 if($bad.Count-ne0){ClosureFail ($Where+' did not complete the real native area reload into the same world without carrying the combat pair: '+($bad -join ', '))}
}
function Assert-KmcChunk6aClosurePersistenceEvidence {
 param($Request,$Rows,$GameResult)
 $case=[string](ClosureProp $Request 'persistenceCase');$scenario=[string]$Request.scenario
 if($scenario-cne'persistence-p04-save'-or-not(Test-KmcChunk6aClosureCase $case)){ClosureFail 'the request is not a closure checkpoint'}
 $pair=Assert-KmcChunk6aClosureIdentity $Request $Rows $GameResult $case
 $rows=@($Rows);$riderId=$pair.riderId;$mountId=$pair.mountId
 foreach($kind in @('initial','rt-foundation-combat-ready','rt-combat-mount-click')){$null=Get-KmcChunk6aClosureKind $rows $kind}
 $click=(Get-KmcChunk6aClosureKind $rows 'rt-combat-mount-click')[0]
 $c=$click.detail.click
 if($c.clicked-ne$true-or$c.nativeCastRequestDelta-ne1-or$c.nativeRefusalDelta-ne0-or$c.dispatchRejectedDelta-ne0-or[string]$c.abilityGuid-cne'f053faad986631688defa003cd7bda0e'-or[string]$c.clickedTargetId-cne$mountId-or[string]$click.relationship-cne'Unmounted'){ClosureFail 'the one native Mount click was not admitted exactly once from an unmounted pair'}
 $selected=@(ClosureProp $c 'selectedIds')
 if($click.detail.availability.enabled-ne$true-or(ClosureProp $c 'availableForCast')-ne$true-or$selected.Count-ne1-or[string]$selected[0]-cne$riderId){ClosureFail 'the click selection or availability differs'}
 $terminalKind=Get-KmcChunk6aClosureTerminalKind $case
 $terminal=(Get-KmcChunk6aClosureKind $rows $terminalKind)[0]
 if([string]$rows[-1].kind-cne$terminalKind){ClosureFail 'the terminal row is not the last observation'}
 $clickFoundation=$click.detail.before
 if($null-eq$clickFoundation-or$null-eq(ClosureProp $clickFoundation 'transitionCounters')){ClosureFail 'the click row lacks its transition baseline'}
 switch -CaseSensitive ($case) {
  'combat-mount-routes' {
   Assert-KmcChunk6aClosureOrder $rows @('initial','rt-foundation-combat-ready','rt-combat-mount-click','rt-combat-mount-settled','routes-write-requested','routes-write-complete','routes-complete')
   $settled=(Get-KmcChunk6aClosureKind $rows 'rt-combat-mount-settled')[0];$sf=Get-KmcChunk6aClosureFoundationOf $settled
   if([string]$settled.relationship-cne'Mounted'-or$null-eq$sf-or$sf.transitionInFlight-ne$false-or[string]::IsNullOrEmpty([string]$sf.pairedIdentity)-or(ClosureDelta $sf $clickFoundation 'acceptedMount')-ne1-or(ClosureDelta $sf $clickFoundation 'adoptionCount')-ne1){ClosureFail 'the combat Mount did not settle as one adopted delivery before the route writes'}
   $requested=@(Get-KmcChunk6aClosureKind $rows 'routes-write-requested' 3);$complete=@(Get-KmcChunk6aClosureKind $rows 'routes-write-complete' 3)
   $types=@('Manual','Quick','Auto');$leaves=@()
   for($i=0;$i-lt3;$i++){
    $rq=$requested[$i];$cp=$complete[$i]
    if($rq.detail.ordinal-ne($i+1)-or[string]$rq.detail.nativeType-cne$types[$i]-or$cp.detail.ordinal-ne($i+1)-or[string]$cp.detail.nativeType-cne$types[$i]-or$cp.detail.nativeCallback-ne$true-or[string]$cp.detail.operation-cne'None'-or[string]$cp.detail.name-cne[string]$rq.detail.name){ClosureFail ('route '+$types[$i]+' was not written exactly once in order through its native type')}
    if([string]$rq.relationship-cne'Mounted'-or[string]$cp.relationship-cne'Mounted'-or[string]$rq.detail.actual.relationship-cne'Mounted'-or[string]$cp.detail.actual.relationship-cne'Mounted'){ClosureFail ('the pair left Mounted around the '+$types[$i]+' route')}
    if($rq.detail.actual.snapshots-ne$i-or$rq.detail.actual.deferredSaves-ne0-or$rq.detail.actual.transitionInFlight-ne$false){ClosureFail ('route '+$types[$i]+' was requested with an unsettled pair or an unexpected snapshot count')}
    Assert-KmcChunk6aClosureSnapshot $cp.detail.snapshot $riderId $mountId $Request ('route '+$types[$i])
    $leaves+=Assert-KmcChunk6aClosureArchive ([string]$Request.runId) $cp.detail ('route '+$types[$i])
    if($cp.detail.actual.snapshots-ne($i+1)-or$cp.detail.actual.failedSaves-ne0-or$cp.detail.actual.deferredSaves-ne0-or$cp.detail.actual.saveSuspended-ne$false-or$cp.detail.actual.serializationSuspended-ne$false){ClosureFail ('route '+$types[$i]+' changed the snapshot, failure, deferral or suspension state')}
   }
   if($leaves[0]-cnotlike'Manual_*'-or$leaves[1]-cnotlike'Quick_*'-or$leaves[2]-cnotlike'Auto_*'){ClosureFail 'the three routes did not write through the native manual, quick and auto slots'}
   if(@($complete|ForEach-Object {[string]$_.detail.sha256}|Select-Object -Unique).Count-ne3){ClosureFail 'the three route archives are not three distinct writes'}
   $t=$terminal.detail
   if([string]$terminal.relationship-cne'Mounted'-or[string]$t.relationship-cne'Mounted'-or$t.snapshots-ne3-or$t.failedSaves-ne0-or$t.deferredSaves-ne0-or$t.duplicateFactCount-ne0-or$t.exactFactCount-ne$settled.controls.ExactFactCount-or$t.transitionInFlight-ne$false-or(ClosureDelta $t $sf 'acceptedMount')-ne0-or(ClosureDelta $t $sf 'forcedDetach')-ne0-or(ClosureDelta $t $sf 'castRequests')-ne0){ClosureFail 'the routes left the mounted controls, counters or relationship changed'}
  }
  'combat-mount-unsettled-save' {
   Assert-KmcChunk6aClosureOrder $rows @('initial','rt-foundation-combat-ready','rt-combat-mount-click','rt-unsettled-save-requested','rt-unsettled-save-deferred','rt-unsettled-save-transition-settled','native-write-complete','unsettled-save-complete')
   $requested=(Get-KmcChunk6aClosureKind $rows 'rt-unsettled-save-requested')[0];$deferred=(Get-KmcChunk6aClosureKind $rows 'rt-unsettled-save-deferred')[0]
   $settled=(Get-KmcChunk6aClosureKind $rows 'rt-unsettled-save-transition-settled')[0];$written=(Get-KmcChunk6aClosureKind $rows 'native-write-complete')[0]
   if([string]$requested.relationship-cne'Unmounted'-or$requested.detail.transitionInFlight-ne$true-or$requested.detail.snapshots-ne0-or$requested.detail.deferredSaves-ne0){ClosureFail 'the save was not requested while the Mount transition was unsettled'}
   if([string]$deferred.relationship-cne'Unmounted'-or$deferred.detail.deferredSaves-ne1-or$deferred.detail.snapshots-ne0-or$deferred.detail.failedSaves-ne0-or$deferred.detail.nativeSaveWaiting-ne$true-or$deferred.detail.transitionInFlight-ne$true-or$deferred.detail.saveSuspended-ne$false-or$deferred.detail.serializationSuspended-ne$false){ClosureFail 'the product did not truthfully defer the unsettled save before capture'}
   if([string]$settled.relationship-cne'Mounted'-or$settled.detail.transitionInFlight-ne$false-or$settled.detail.snapshots-ne0-or[string]::IsNullOrEmpty([string]$settled.detail.pairedIdentity)-or(ClosureDelta $settled.detail $clickFoundation 'acceptedMount')-ne1){ClosureFail 'the transition did not settle mounted as one accepted delivery before the capture'}
   $w=$written.detail
   if([string]$written.relationship-cne'Mounted'-or$w.nativeCallback-ne$true-or[string]$w.operation-cne'None'-or[string]$w.nativeType-cne'Manual'-or$w.actual.deferredSaves-ne1-or$w.actual.snapshots-ne1-or$w.actual.failedSaves-ne0-or$w.actual.saveSuspended-ne$false-or$w.actual.serializationSuspended-ne$false-or[string]$w.actual.relationship-cne'Mounted'){ClosureFail 'the deferred save was not captured exactly once after settlement'}
   if($null-eq(ClosureProp $w 'requested')-or$w.requested.transitionInFlight-ne$true-or$w.requested.snapshots-ne0){ClosureFail 'the write does not carry its unsettled request baseline'}
   Assert-KmcChunk6aClosureSnapshot $w.snapshot $riderId $mountId $Request 'deferred save'
   $leaf=Assert-KmcChunk6aClosureArchive ([string]$Request.runId) $w 'deferred save'
   if($leaf-cne'Manual_300_KMC_P01.zks'){ClosureFail 'the deferred save did not land in the owned manual archive'}
   $t=$terminal.detail
   if([string]$terminal.relationship-cne'Mounted'-or$t.deferredSaves-ne1-or$t.snapshots-ne1-or$t.failedSaves-ne0-or$t.transitionInFlight-ne$false-or$t.duplicateFactCount-ne0){ClosureFail 'the unsettled-save checkpoint did not end mounted with one deferred and one captured save'}
  }
  'combat-mount-area-reload' {
   Assert-KmcChunk6aClosureOrder $rows @('initial','rt-foundation-combat-ready','rt-combat-mount-click','rt-combat-mount-settled','rt-before-save','rt-native-save-requested','native-write-complete','combat-area-reload-requested','combat-area-reload-complete')
   $settled=(Get-KmcChunk6aClosureKind $rows 'rt-combat-mount-settled')[0];$written=(Get-KmcChunk6aClosureKind $rows 'native-write-complete')[0];$requested=(Get-KmcChunk6aClosureKind $rows 'combat-area-reload-requested')[0]
   $sf=Get-KmcChunk6aClosureFoundationOf $settled
   if([string]$settled.relationship-cne'Mounted'-or$null-eq$sf-or$sf.transitionInFlight-ne$false-or(ClosureDelta $sf $clickFoundation 'acceptedMount')-ne1){ClosureFail 'the combat Mount did not settle as one accepted delivery'}
   if([string]$written.relationship-cne'Mounted'-or$written.detail.nativeCallback-ne$true-or[string]$written.detail.operation-cne'None'-or[string]$written.detail.nativeType-cne'Manual'){ClosureFail 'the combat-mounted archive was not written from the settled pair'}
   Assert-KmcChunk6aClosureSnapshot $written.detail.snapshot $riderId $mountId $Request 'combat-mounted save'
   $leaf=Assert-KmcChunk6aClosureArchive ([string]$Request.runId) $written.detail 'combat-mounted save'
   $rq=$requested.detail
   if([string]$requested.relationship-cne'Mounted'-or[string]$rq.relationship-cne'Mounted'-or$rq.suspensions-ne0-or$rq.resumes-ne0-or$rq.snapshots-ne1-or$rq.deferredSaves-ne0-or$rq.liveRiderInCombat-ne$true-or$rq.transitionInFlight-ne$false-or$rq.loadingInProcess-ne$false-or$rq.fixtureReleased-ne$false){ClosureFail 'the area reload was not requested from the saved, settled combat-mounted pair'}
   $t=$terminal.detail
   Assert-KmcChunk6aClosureReload $t 'the combat area reload'
   Assert-KmcChunk6aClosureNoResidue $t 'the combat area reload'
   if([string]$t.archiveSha256After-cne[string]$t.archiveSha256Before-or[string]$t.archiveSha256Before-cne[string]$written.detail.sha256-or[IO.Path]::GetFileName([string]$t.archivePath)-cne$leaf-or$t.snapshots-ne1-or$t.deferredSaves-ne0-or$t.failedSaves-ne0){ClosureFail 'the combat-mounted archive changed across the area reload'}
   $baseline=ClosureProp $t 'baseline'
   if($null-eq$baseline-or-not(ClosureSameJson $baseline $rq)){ClosureFail 'the completion does not carry its exact request baseline'}
   # The supported lifecycle at the native area unload of a combat pair is exactly one cleanup (the transfer is
   # never armed in combat): one fresh forced detach, no voluntary transition, no replayed cast.
   if((ClosureDelta $t $baseline 'forcedDetach')-ne1-or(ClosureDelta $t $baseline 'acceptedMount')-ne0-or(ClosureDelta $t $baseline 'admittedMount')-ne0-or(ClosureDelta $t $baseline 'acceptedDismount')-ne0-or(ClosureDelta $t $baseline 'castRequests')-ne0-or(ClosureDelta $t $baseline 'dispatchAccepted')-ne0){ClosureFail 'the supported lifecycle cleanup did not run exactly once at the area unload'}
   if($t.generation-ne$baseline.generation){ClosureFail 'the area reload changed the relationship generation without a transition'}
  }
  'pending-mount-area' {
   Assert-KmcChunk6aClosureOrder $rows @('initial','rt-foundation-combat-ready','rt-combat-mount-click','pending-mount-approach-observed','pending-mount-area-requested','pending-mount-area-complete')
   $observed=(Get-KmcChunk6aClosureKind $rows 'pending-mount-approach-observed')[0];$requested=(Get-KmcChunk6aClosureKind $rows 'pending-mount-area-requested')[0]
   $o=$observed.detail
   if([string]$observed.relationship-cne'Unmounted'-or(ClosureNumber $o.riderDisplacement 'displacement')-le0.25-or$o.transitionInFlight-ne$true-or$o.riderReallyMoving-ne$true-or$null-eq(ClosureProp $o 'shell')-or$o.shell.acted-ne$false-or$o.shell.finished-ne$false-or[string]$o.shell.abilityGuid-cne'f053faad986631688defa003cd7bda0e'-or[string]$o.shell.executor-cne$riderId-or[string]$o.shell.target-cne$mountId-or$o.snapshots-ne0){ClosureFail 'the area reload was not requested during the measured pending approach of the exact Mount shell'}
   $rq=$requested.detail
   if([string]$requested.relationship-cne'Unmounted'-or$rq.snapshots-ne0-or$rq.transitionInFlight-ne$true-or$rq.suspensions-ne0-or$rq.fixtureReleased-ne$false){ClosureFail 'the pending-approach reload request differs'}
   $t=$terminal.detail
   Assert-KmcChunk6aClosureReload $t 'the pending-approach area reload'
   Assert-KmcChunk6aClosureNoResidue $t 'the pending-approach area reload'
   $baseline=ClosureProp $t 'baseline'
   if($null-eq$baseline-or-not(ClosureSameJson $baseline $rq)){ClosureFail 'the completion does not carry its exact request baseline'}
   if($t.snapshots-ne0-or$t.deferredSaves-ne0-or$t.castRequests-ne1-or(ClosureDelta $t $baseline 'castRequests')-ne0-or(ClosureDelta $t $baseline 'acceptedMount')-ne0-or(ClosureDelta $t $baseline 'admittedMount')-ne0-or(ClosureDelta $t $baseline 'dispatchAccepted')-ne0-or$t.generation-ne$baseline.generation){ClosureFail 'the pending Mount was delivered or replayed across the area reload'}
   # Exactly one cleanup announcement at the unload: a fresh forced detach, or the suppressed repeat of the
   # fixture-load cleanup recorded for the same unmounted generation; never both, never none.
   $cleanups=(ClosureDelta $t $baseline 'forcedDetach')+(ClosureDelta $t $baseline 'duplicateSuppressed')
   if($cleanups-ne1-or(ClosureDelta $t $baseline 'forcedDetach')-lt0-or(ClosureDelta $t $baseline 'duplicateSuppressed')-lt0){ClosureFail 'the area transition did not clean up exactly once'}
  }
 }
}

# ---------------------------------------------------------------------------------------------
# CM08-area-restoration on the Chunk 5 area-reload run: the supported lifecycle is suspension at the
# native area unload and exact restoration after native placement (not the legacy clean dismount).
# ---------------------------------------------------------------------------------------------
function Assert-KmcChunk6aAreaRestoration {
 param($Request,$Rows,$GameResult)
 if([string]$Request.scenario-cne'persistence-p07-save'-or[string](ClosureProp $Request 'persistenceCase')-cne'area-reload'){ClosureFail 'the request is not the area-reload checkpoint'}
 $rows=@($Rows)
 $initial=@($rows|Where-Object kind -CEQ 'initial');if($initial.Count-ne1){ClosureFail 'no unique initial row'}
 $riderId=[string]$initial[0].rider.Id;$mountId=[string]$initial[0].mount.Id
 if([string]::IsNullOrEmpty($riderId)-or[string]::IsNullOrEmpty($mountId)-or$riderId-ceq$mountId){ClosureFail 'initial row lacks the exact pair'}
 foreach($r in $rows){ if($r.runId-cne$Request.runId-or$r.scenario-cne$Request.scenario-or$r.source-cne$Request.commit-or$r.dll-cne$Request.dllSha256-or$r.processId-ne$GameResult.processId){ClosureFail ('row identity differs at '+$r.kind)} }
 if(@($rows|Where-Object kind -CIn @('assertion-failed','scenario-failed')).Count-ne0){ClosureFail 'a failure row is present'}
 Assert-KmcChunk6aClosureOrder $rows @('initial','area-initial-write','area-reload-requested','area-reload-observed','area-reload-complete','native-write-complete')
 $requested=(Get-KmcChunk6aClosureKind $rows 'area-reload-requested')[0];$observed=(Get-KmcChunk6aClosureKind $rows 'area-reload-observed')[0];$complete=(Get-KmcChunk6aClosureKind $rows 'area-reload-complete')[0]
 $before=$requested.detail;$after=$complete.detail
 if([string]$requested.relationship-cne'Mounted'-or$before.suspensions-ne0-or$before.resumes-ne0-or$before.pending-ne$false-or$before.snapshots-ne1-or[string]$requested.rider.Id-cne$riderId-or[string]$requested.mount.Id-cne$mountId){ClosureFail 'the reload was not requested from the saved, mounted, unsuspended pair'}
 $authorityBefore=ClosureProp $before 'movementAuthority'
 if($null-eq$authorityBefore-or@($authorityBefore.riderOverrideComponents).Count-ne1-or$authorityBefore.riderOverrideBaseline-eq0-or$authorityBefore.riderOverrideComponents[0]-ne$authorityBefore.riderOverrideBaseline-or$authorityBefore.riderOverrideComponents[0]-ne$authorityBefore.runtimeMovementAgent-or$authorityBefore.riderOverrideIsRuntimeAgent-ne$true-or$authorityBefore.riderStockAgentEnabled-ne$false-or$authorityBefore.mountStockAgentEnabled-ne$true-or@($authorityBefore.mountOverrideComponents).Count-ne0-or$authorityBefore.mountAgentOverride-ne0){ClosureFail 'the mounted movement authority before the reload was not the exact owned override'}
 foreach($actor in @('riderActor','mountActor')){ $b=$before.$actor; if($b.nativeActorCount-ne1-or$b.viewAlive-ne$true-or$b.viewBound-ne$true-or$b.viewId-ne$b.baselineViewId-or$b.boundToRelationship-ne$true){ClosureFail ('the '+$actor+' before the reload is not the bound baseline view')} }
 if([string]$observed.relationship-cne'Mounted'-or[string]$complete.relationship-cne'Mounted'-or[string]$complete.rider.Id-cne$riderId-or[string]$complete.mount.Id-cne$mountId){ClosureFail 'the pair was not restored after the native area placement'}
 if((ClosureLong $after.loadingFrames 'loadingFrames')-le0-or$after.suspensionObserved-ne$true-or$after.suspensions-ne1-or$after.resumes-ne1-or$after.pending-ne$false-or$after.sameWorld-ne$true-or[string]$after.area-cne[string]$after.expectedArea-or[string]$after.area-cne[string]$before.area-or$after.nativeCastRequests-ne0-or$null-ne(ClosureProp $after 'mountedInvariant')-or$after.loadingInProcess-ne$false-or$after.snapshots-ne1){ClosureFail 'the area transition did not suspend once, restore once and complete in the same world without a replayed cast'}
 foreach($actor in @('riderActor','mountActor')){
  $a=$after.$actor
  if($a.nativeActorCount-ne1-or$a.viewAlive-ne$true-or$a.viewBound-ne$true-or[string]$a.viewDisposition-cne'retained'-or[string]$after.expectedViewDisposition-cne'retained'-or$a.viewExactForPair-ne$true-or$a.boundToRelationship-ne$true-or$a.viewId-ne$a.baselineViewId-or$a.baselineViewId-ne$before.$actor.baselineViewId){ClosureFail ('the restored '+$actor+' is not the exact retained bound view')}
 }
 if([string]$after.riderActor.id-cne$riderId-or[string]$after.mountActor.id-cne$mountId){ClosureFail 'the restored pair is not the saved pair'}
 if($after.generationBaseline-ne$before.generation-or$after.generation-ne($after.generationBaseline+1)){ClosureFail 'the relationship generation did not advance exactly once through the saved-pair restoration'}
 $authority=ClosureProp $after 'movementAuthority'
 if($null-eq$authority-or$authority.riderOverrideBaseline-ne$authorityBefore.riderOverrideBaseline-or@($authority.riderOverrideComponents).Count-ne1-or$authority.riderOverrideComponents[0]-eq$authority.riderOverrideBaseline-or$authority.riderOverrideComponents[0]-ne$authority.runtimeMovementAgent-or$authority.riderOverrideIsRuntimeAgent-ne$true-or$authority.riderStockAgentPresent-ne$true-or$authority.riderStockAgentEnabled-ne$false-or$authority.mountStockAgentPresent-ne$true-or$authority.mountStockAgentEnabled-ne$true-or$authority.mountAgentOverride-ne0-or@($authority.mountOverrideComponents).Count-ne0-or$authority.riderCommandsEmpty-ne$true-or$authority.mountCommandsEmpty-ne$true-or$authority.riderReallyMoving-ne$false-or$authority.mountReallyMoving-ne$false){ClosureFail 'the restored movement authority is not one fresh owned override with the stale component gone and both command containers empty'}
}

# ---------------------------------------------------------------------------------------------
# CM07-schema-unchanged: the combat-mounted archive carries exactly the accepted Chunk 5 member set.
# ---------------------------------------------------------------------------------------------
function Get-KmcChunk6aAcceptedSaveSchema {
 [pscustomobject]@{member='kmc-mounted-state';schemaVersion=2;policy='rider-principal-distinct-native-v1';rules='crpg-transport-v1';
  keys=@('SchemaVersion','CampaignId','AreaId','GameTimeTicks','Policy','RulesId','Mounted','ProfileId','Rider','Mount','Slots','Combat');
  # SavedCombatData as accepted with chunk5-preview.105 (commit 471e1df9); the codec serializes every member, nulls included.
  combatKeys=@('TurnBased','Round','StartTicks','RoundStartTicks','TurnStartTicks','TimeSinceStart','TimeToNextRound','HasSurpriseRound','HasEnemy','HadEnemy','NextActor','BeforeCombatSelected','Actors','Roster','Engagements','Current','Paired','Allocations')}
}
function Assert-KmcChunk6aSchemaUnchanged {
 param($Request,$Rows,[string]$LabRoot)
 if([string]$Request.scenario-cne'persistence-p04-save'-or[string](ClosureProp $Request 'persistenceCase')-cne'combat-mount-rt'){ClosureFail 'the schema claim binds the settled combat-mounted save'}
 $written=@($Rows|Where-Object kind -CEQ 'native-write-complete');if($written.Count-ne1){ClosureFail 'no unique native write'}
 $detail=$written[0].detail
 $staged=Join-Path $LabRoot ('runtime-staging/persistence-'+[string]$Request.runId+'/Saved Games/Manual_300_KMC_P01.zks')
 if(-not(Test-Path -LiteralPath $staged -PathType Leaf)){ClosureFail 'the staged combat-mounted archive is absent'}
 if((Get-FileHash -LiteralPath $staged -Algorithm SHA256).Hash.ToLowerInvariant()-cne[string]$detail.sha256){ClosureFail 'the staged archive differs from the recorded write'}
 $accepted=Get-KmcChunk6aAcceptedSaveSchema
 Add-Type -AssemblyName System.IO.Compression.FileSystem
 $zip=[IO.Compression.ZipFile]::OpenRead($staged)
 try {
  $kmc=@($zip.Entries|Where-Object {$_.FullName-clike'kmc*'})
  if($kmc.Count-ne1-or[string]$kmc[0].FullName-cne$accepted.member){ClosureFail ('the archive carries other KMC members than the accepted one: '+(@($kmc|ForEach-Object FullName) -join ','))}
  if($kmc[0].Length-le0-or$kmc[0].Length-gt128KB){ClosureFail 'the KMC member size is outside the accepted bound'}
  $reader=New-Object IO.StreamReader($kmc[0].Open(),[Text.UTF8Encoding]::new($false,$true))
  try { $text=$reader.ReadToEnd() } finally { $reader.Dispose() }
 } finally { $zip.Dispose() }
 Assert-KmcJsonObjectMembersUnique $text 'combat-mounted archive member'
 $json=$text|ConvertFrom-Json
 $keys=@($json.PSObject.Properties.Name|Sort-Object)
 if((@($keys) -join ',')-cne(@($accepted.keys|Sort-Object) -join ',')){ClosureFail ('the persisted member set differs from the accepted schema: '+($keys -join ','))}
 if($json.SchemaVersion-ne$accepted.schemaVersion-or[string]$json.Policy-cne$accepted.policy-or[string]$json.RulesId-cne$accepted.rules-or$json.Mounted-ne$true){ClosureFail 'the persisted schema identity differs from the accepted Chunk 5 schema'}
 if($null-eq(ClosureProp $json 'Combat')){ClosureFail 'the combat-mounted archive carries no combat record'}
 $combatKeys=@($json.Combat.PSObject.Properties.Name|Sort-Object)
 if((@($combatKeys) -join ',')-cne(@($accepted.combatKeys|Sort-Object) -join ',')){ClosureFail ('the persisted combat member set differs from the accepted schema: '+($combatKeys -join ','))}
 if($detail.snapshot.SchemaVersion-ne$accepted.schemaVersion-or[string]$detail.snapshot.Policy-cne$accepted.policy-or$detail.snapshot.Mounted-ne$true){ClosureFail 'the recorded snapshot does not match the accepted schema'}
}

# ---------------------------------------------------------------------------------------------
# Ledger projection and isolated binding for the persistence-backed closure claims.
# ---------------------------------------------------------------------------------------------
function Get-KmcChunk6aClosureRecords([string]$Path,$Game) {
 if([IO.Path]::GetFileName($Path)-cne'persistence-observations.jsonl'){ClosureFail 'the reader requires the persistence JSONL'}
 $records=@(foreach($line in Get-Content -LiteralPath $Path -Encoding UTF8){
  if([string]::IsNullOrWhiteSpace($line)){continue}
  Assert-KmcJsonObjectMembersUnique $line 'bound closure record'
  $line|ConvertFrom-Json
 })
 if($records.Count-lt1){ClosureFail 'the closure artifact is empty'}
 foreach($r in $records){ if($r.runId-cne$Game.runId-or$r.scenario-cne$Game.scenario-or$r.source-cne$Game.commit-or$r.dll-cne$Game.dllSha256){ClosureFail 'a closure record identity differs'} }
 $records
}
function Get-KmcChunk6aClosureArtifactCase([string]$Path) {
 $found=@(foreach($line in Get-Content -LiteralPath $Path -Encoding UTF8){ if(-not[string]::IsNullOrWhiteSpace($line)){ [string](ClosureProp ($line|ConvertFrom-Json) 'checkpoint') } })
 $cases=@($found|Select-Object -Unique)
 if($cases.Count-ne1){ClosureFail 'the artifact checkpoint is not unique'}
 $cases[0]
}
# The closure rows of a persistence artifact: a P04 closure case or the Chunk 5 P07 area-reload case.
function Test-KmcChunk6aClosureArtifact([string]$Path,[string]$Scenario) {
 if([IO.Path]::GetFileName($Path)-cne'persistence-observations.jsonl'){return $false}
 $case=Get-KmcChunk6aClosureArtifactCase $Path
 ($Scenario-ceq'persistence-p04-save'-and(Test-KmcChunk6aClosureCase $case))-or($Scenario-ceq'persistence-p07-save'-and$case-ceq'area-reload')
}
function Get-KmcChunk6aClosureRows([string]$Path,[string]$Scenario,$Game) {
 if([string]$Game.scenario-cne$Scenario){ClosureFail 'the scenario differs from the native result'}
 $records=@(Get-KmcChunk6aClosureRecords $Path $Game)
 $cases=@($records|ForEach-Object {[string]$_.checkpoint}|Select-Object -Unique)
 if($cases.Count-ne1){ClosureFail 'the artifact checkpoint is not unique'}
 $case=$cases[0];$rowName=Get-KmcChunk6aClosureRowName $Scenario $case
 $terminal=@($records|Where-Object kind -CEQ (Get-KmcChunk6aClosureTerminalKind $case));$failed=@($records|Where-Object kind -CIn @('assertion-failed','scenario-failed'))
 $status=if($terminal.Count-eq1-and$failed.Count-eq0-and[string]$Game.status-ceq'PASS'){'PASS'}else{'FAIL'}
 [pscustomobject]@{name=$rowName;status=$status;assertionPassCount=$Game.assertionPassCount;assertionFailCount=$Game.assertionFailCount;errors=@($Game.errors)}
}
function Test-KmcChunk6aClosureBinding($Binding) {
 $rows=@(ClosureProp $Binding 'rows')
 $rows.Count-eq1-and[string]$rows[0]-cin@(Get-KmcChunk6aClosureRowNames)-and[string](ClosureProp $Binding 'scenario')-cin@('persistence-p04-save','persistence-p07-save')
}
function Get-KmcChunk6aClosureProjection($Binding,$Request,$Game,$Result,[string]$Root) {
 if(-not(Test-KmcChunk6aClosureBinding $Binding)-or[string]$Binding.evidenceLeaf-cne'persistence-observations.jsonl'){ClosureFail 'a closure binding requires the persistence scenario, the JSONL leaf and one closure row'}
 if([IO.Path]::GetFullPath([string]$Request.evidenceRoot).TrimEnd('\')-cne[IO.Path]::GetFullPath($Root).TrimEnd('\')){ClosureFail 'the request evidence root differs'}
 foreach($item in @($Request,$Game,$Result)){ if([string]$item.runId-cne[string]$Binding.runId-or[string]$item.scenario-cne[string]$Binding.scenario){ClosureFail 'the run identity differs'} }
 # A re-evaluated binding carries the earlier reader's refusal in its overall facet: that facet must then be
 # exactly a FAIL made only of this reader's own refusals while the native facet stays an exact PASS.
 $reevaluated=$null-ne(ClosureProp $Binding 'reevaluation')
 foreach($item in @($Game,$Result)){
  if($reevaluated-and[object]::ReferenceEquals($item,$Result)){
   if([string]$item.status-cne'FAIL'-or-not(Test-KmcExactJsonInteger $item.assertionFailCount)-or$item.assertionFailCount-lt1-or$item.errors-isnot[Array]-or@($item.errors).Count-lt1-or@($item.errors|Where-Object {[string]$_-cnotmatch'^Chunk 6A closure: '}).Count-ne0){ClosureFail 'the re-evaluated overall facet is not exactly this reader''s refusal'}
  } elseif([string]$item.status-cne'PASS'-or-not(Test-KmcExactJsonInteger $item.assertionPassCount)-or$item.assertionPassCount-lt1-or-not(Test-KmcExactJsonInteger $item.assertionFailCount)-or$item.assertionFailCount-ne0-or$item.errors-isnot[Array]-or@($item.errors).Count-ne0){ClosureFail 'the native or overall facet is not an exact PASS'}
  if([string]$item.evidenceManifestSha256-cne[string]$Binding.artifactManifestSha256){ClosureFail 'the result manifest binding differs'}
 }
 Assert-KmcReadOnlyArtifactManifest -Request $Request -ExpectedSha256 $Binding.artifactManifestSha256
 $manifest=Get-KmcBoundJson (Join-Path $Root 'runtime-artifacts.json') $Binding.artifactManifestSha256
 $leaves=@($manifest.artifacts|Where-Object relativePath -CEQ 'persistence-observations.jsonl')
 if($leaves.Count-ne1-or[string]$leaves[0].sha256-cne[string]$Binding.evidenceSha256-or[string]$leaves[0].kind-cne'persistence-evidence'){ClosureFail 'the artifact hash differs from the manifest'}
 $case=[string](ClosureProp $Request 'persistenceCase')
 $rowName=Get-KmcChunk6aClosureRowName ([string]$Binding.scenario) $case
 if([string]$Binding.rows[0]-cne$rowName){ClosureFail 'the binding row differs from the request checkpoint'}
 # The complete persistence validator re-runs on the immutable bytes (the closure reader for the P04 closure
 # cases, the Chunk 5 reader for the area reload), then the claim-specific identity facts.
 Assert-KmcPersistenceScenarioEvidence -Request $Request -Manifest $manifest -Status 'PASS' -GameResult $Game
 $path=Join-Path $Root 'persistence-observations.jsonl'
 $records=@(Get-KmcChunk6aClosureRecords $path $Game)
 if($case-ceq'area-reload'){ Assert-KmcChunk6aAreaRestoration $Request $records $Game }
 else { Assert-KmcChunk6aClosurePersistenceEvidence $Request $records $Game }
 $row=Get-KmcChunk6aClosureRows $path ([string]$Binding.scenario) $Game
 if([string]$row.status-cne'PASS'-or[string]$row.name-cne$rowName-or$row.assertionPassCount-ne$Binding.passCount-or$row.assertionFailCount-ne0){ClosureFail 'the row totals differ'}
 $projection=[ordered]@{}
 foreach($field in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid')){$projection[$field]=$Game.$field}
 $projection.status='PASS';$projection.errors=@();$projection.rows=@($row)
 [pscustomobject]$projection
}
function Assert-KmcChunk6aClosureIsolated([string]$Id,$Binding,[string]$LabRoot) {
 $map=Get-KmcChunk6aClosureIdMap
 if(-not$map.Contains($Id)){ClosureFail ('not a closure id: '+$Id)}
 $expected=$map[$Id]
 if([string]$Binding.scenario-cne$expected[0]){ClosureFail ($Id+' requires its exact scenario')}
 $root=Join-Path $LabRoot ('runtime-evidence/'+$Binding.runId)
 $request=Get-KmcBoundJson (Join-Path $root 'runtime-request.json') $Binding.requestSha256
 if([string](ClosureProp $request 'persistenceCase')-cne$expected[1]){ClosureFail ($Id+' requires its exact checkpoint')}
 $rowName=Get-KmcChunk6aClosureRowName $expected[0] $expected[1]
 if(@($Binding.rows).Count-ne1-or[string]$Binding.rows[0]-cne$rowName){ClosureFail ($Id+' binding row differs')}
}
# CM07-schema-unchanged binds a settled combat-mounted save run (the CM07-mount-save-rt checkpoint) and adds
# the archive member check on the immutable staged bytes.
function Assert-KmcChunk6aSchemaClaimIsolated([string]$Id,$Binding,[string]$LabRoot) {
 if($Id-cne'CM07-schema-unchanged'){ClosureFail ('not the schema claim: '+$Id)}
 if([string]$Binding.scenario-cne'persistence-p04-save'-or@($Binding.rows).Count-ne1-or[string]$Binding.rows[0]-cne'P04-save-combat-mount-rt'){ClosureFail 'the schema claim binds the settled combat-mounted save row'}
 $root=Join-Path $LabRoot ('runtime-evidence/'+$Binding.runId)
 $request=Get-KmcBoundJson (Join-Path $root 'runtime-request.json') $Binding.requestSha256
 if([string](ClosureProp $request 'persistenceCase')-cne'combat-mount-rt'){ClosureFail 'the schema claim requires the combat-mount-rt checkpoint'}
 $game=Get-KmcBoundJson (Join-Path $root 'runtime-game-result.json') $Binding.gameResultSha256
 $records=@(Get-KmcChunk6aClosureRecords (Join-Path $root 'persistence-observations.jsonl') $game)
 Assert-KmcChunk6aSchemaUnchanged $request $records $LabRoot
}
# CM07-cold-load combines the two settled cold loads (real time and turn based) of combat-mounted archives.
function Assert-KmcChunk6aColdLoadRole($Role,$Request) {
 $expectedCase=if([string]$Role.scenario-ceq'persistence-p04-load'){'combat-mount-rt'}elseif([string]$Role.scenario-ceq'persistence-p02-load'){'combat-mount-tb'}else{ClosureFail ('not a cold-load role scenario: '+[string]$Role.scenario)}
 if([string]$Request.scenario-cne[string]$Role.scenario-or[string](ClosureProp $Request 'persistenceCase')-cne$expectedCase){ClosureFail ('the cold-load role '+[string]$Role.name+' did not run its exact checkpoint')}
 $load=ClosureProp $Request 'persistenceLoad'
 if($null-eq$load-or[string]$load.sha256-cnotmatch'^[0-9a-f]{64}$'-or[string]$load.fileName-cne'Manual_300_KMC_P01.zks'){ClosureFail ('the cold-load role '+[string]$Role.name+' lacks its exact archive descriptor')}
}
