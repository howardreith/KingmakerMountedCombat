param([string]$ReaderDirectory,[string]$FixturePath,[Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$draft=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if(-not$ReaderDirectory){$ReaderDirectory=Join-Path $draft 'scripts/runtime'}
if(-not$FixturePath){$FixturePath=Join-Path $draft 'tests/Chunk7Diagnostics/OwnedPairsEvidenceFixtures.json'}
. (Join-Path $ReaderDirectory 'RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'Chunk7NativeForfeitFixture.ps1')
$fixtures=(Get-Content -LiteralPath $FixturePath -Raw|ConvertFrom-Json).fixtures
$tokens=$null;$parseErrors=$null;$ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'Test-Chunk7OwnedPairsEvidence.ps1'),[ref]$tokens,[ref]$parseErrors)
if($parseErrors.Count-ne0){throw 'The pinned existing fixture generator has parse errors.'}
# Import only the existing generator functions; do not execute the unrelated 680 checks.
foreach($name in @('Copy-Value','New-KmcSeedNativeActor','Add-KmcSeedFirstActionMount','New-KmcOwnerSeedDerivedCombatFixture')){
 $nodes=@($ast.FindAll({param($node)$node-is[Management.Automation.Language.FunctionDefinitionAst]-and$node.Name-ceq$name},$true))
 if($nodes.Count-ne1){throw ('Expected one pinned fixture function '+$name)};. ([scriptblock]::Create($nodes[0].Extent.Text))
}
$clock=[Diagnostics.Stopwatch]::StartNew();$results=[Collections.Generic.List[object]]::new()
function Check([string]$Name,[scriptblock]$Body,[bool]$Refuse=$false){$errorText=$null;$ok=$false;try{&$Body;$ok=-not$Refuse}catch{$errorText=$_.Exception.Message;$ok=$Refuse};$results.Add([pscustomobject]@{name=$Name;pass=$ok;expectedRefusal=$Refuse;error=$errorText});Write-Output ($(if($ok){'PASS '}else{'FAIL '})+$Name+$(if(-not$ok){': '+$errorText}else{''}))}
function New-DisableControls([bool]$Enabled,[int]$Facts,[int]$Slots,[int]$Casts){[pscustomobject][ordered]@{Registered=$true;Enabled=$Enabled;SerializationSuspended=$false;ExactFactCount=$Facts;DuplicateFactCount=0;ManagedHotbarSlotCount=$Slots;TargetSelectionStartCount=$Casts;TargetSelectionEndCount=$Casts;NativeCastRequestCount=$Casts;NativeRefusalCount=0;DispatchAcceptedCount=$Casts;DispatchRejectedCount=0;NativePrimaryShellPrepareCount=0;LastNativePrimaryShellObservation=$null;ActivationRecordCount=$Casts;RiderPrimaryRelationshipEndCount=0}}
function New-DisableState($v,$world,[string]$label,[bool]$Combat){
 $r=$v.request;$o=$v.artifact.observations;$roles=@(Get-KmcChunk7OwnedPairIds $r);$world=Copy-Value $world;$world.label=$label
 $world.owners=@($o.mountedStart.world.owners|ForEach-Object{Copy-Value $_})
 foreach($actor in $world.actors){if($null-ne$actor.native){$actor.native|Add-Member actorObject (1001+[Array]::IndexOf($roles,$actor.id)) -Force}}
 $unrelated=@($r.ownedPairs.partyIds|Where-Object{$_-cnotin$roles});if($Combat){$unrelated+=,$o.targetId}
 [pscustomobject][ordered]@{world=$world;paused=$false;turnBased=$Combat;persistenceEnabled=$true;controls=(New-DisableControls $true 6 4 $(if($Combat){4}else{0}));slots=@($o.mountedStart.slots|ForEach-Object{[pscustomobject]@{ActorId=$_.actorId;Index=$_.index;Kind=$_.kind}})
  nativePrepares=@($roles|ForEach-Object{[pscustomobject]@{actorId=$_;count=$(if($Combat){1}else{0})}})
  unrelatedActors=@($unrelated|ForEach-Object{[pscustomobject]@{native=(New-KmcSeedNativeActor $_ 0.0);prepared=$true;inCombat=$Combat}});healthyAllocationId=$(if($Combat){9901}else{$null})
  snapshotCount=0;nativeWrites=0;authorizedWrites=0
  pairStates=@(for($i=0;$i-lt2;$i++){$owner=@($o.mountedStart.owners|Where-Object riderId -CEQ $roles[$i*2])[0];[pscustomobject][ordered]@{pairId=$owner.ownerId;generation=$owner.generation;riderId=$roles[$i*2].Replace('-','');mountId=$roles[$i*2+1].Replace('-','');relationshipState='Mounted';noPreparedPairResidue=$false;commandsRetained=$false;unsettledShell=$false;currentOwnerId=$owner.ownerId;currentGeneration=$owner.generation;currentOwnerState='Active';lastTransition=$null}})
  participationA=$null;participationB=$null;aCanRelease=$false;bCanRelease=$false}
}
function Set-DisableUnmounted($s,[bool]$Held){
 foreach($owner in $s.world.owners){$owner.state='Detached';$owner.retainsObligations=$true}
 for($i=0;$i-lt2;$i++){$p=$s.pairStates[$i];$p.relationshipState='Unmounted';$p.noPreparedPairResidue=$true;$p.currentOwnerState='Detached'
  $p.lastTransition=[pscustomobject][ordered]@{succeeded=(-not($Held-and$i-eq1));state='Unmounted';trigger='ModDisabled';errors=@(if($Held-and$i-eq1){'InvalidOperationException: C7 selected pair B ModDisabled subscriber failure'});movementAuthorityResidual=$false;presentationResidual=$false}}
 $s.slots=@()
}
function New-RegisteredDisableFixture{
 $v=New-KmcOwnerSeedDerivedCombatFixture 'combat-rider-first';$r=$v.request;$o=$v.artifact.observations;$roles=@(Get-KmcChunk7OwnedPairIds $r);$r.ownedPairs.case='owner-seed-combat-disable'
 foreach($key in @('r1','firstActionMount','localFailure')){$o.PSObject.Properties.Remove($key)}
 $o.snapshots=@();$o.inputs=@($o.inputs|Where-Object{$_.label-cin@('rider-primary','mount-primary')})
 $b=@($o.activations|Where-Object pair -EQ 1)[0];$b.PSObject.Properties.Remove('afterEnd');$b.PSObject.Properties.Remove('tbChargeRefusal')
 $o.nativeCompletion.calls=@($o.nativeCompletion.calls|Where-Object{$_.before.actorId-cin@($roles[0],$roles[1])})
 $attack=@($o.inputs|Where-Object{$_.pair-eq1-and$_.label-ceq'mount-primary'})[0]
 $pending=Copy-Value $o.pendingSurvivor;$pending.before=Copy-Value $attack.after;$pending.before.label='pending-survivor-before';$pending.before.frame=$attack.after.frame+1;$pending.after=Copy-Value $pending.before;$pending.after.label='pending-survivor-after';$pending.after.frame++
 $pending.participation.PendingSplitRound=$pending.after.round
 $o.pendingSurvivor=$pending;$o.allocationTrace.events=@($o.allocationTrace.events|Where-Object{$_.frame-le$attack.after.frame})
 Set-KmcChunk7NativeForfeitFixture $o
 $before=New-DisableState $v $o.pendingSurvivor.after 'registered-disable-before' $true
 $before.participationA=[pscustomobject][ordered]@{RiderId=$roles[0];MountId=$roles[1];Activation=[pscustomobject]@{Finalized=$true};BoundaryIsCurrent=$false;Boundary=$null;Partner=$null;SplitReleaseRound=-1;PendingSplitId=$null;PendingSplitRound=-1;RenewalNotBeforeTicks=0}
 $before.participationB=Copy-Value $pending.participation
 $held=Copy-Value $before;$held.world.label='registered-disable-refused';Set-DisableUnmounted $held $true
 $retry=Copy-Value $before;$retry.world.label='registered-disable-retry-accepted';Set-DisableUnmounted $retry $false;$retry.persistenceEnabled=$false;$retry.controls=New-DisableControls $false 0 0 4
 $later=Copy-Value $retry;$later.world.label='registered-disabled-later-frame';$later.world.frame++
 $reenabled=Copy-Value $later;$reenabled.world.label='registered-reenabled-before-native-teardown';$reenabled.persistenceEnabled=$true;$reenabled.controls=New-DisableControls $true 2 0 4
 $visits=@(foreach($phase in @('first-disable','retry-disable')){foreach($p in $before.pairStates){[pscustomobject][ordered]@{phase=$phase;frame=$before.world.frame;trigger='ModDisabled';pairId=$p.pairId;generation=$p.generation;riderId=$p.riderId;mountId=$p.mountId;observedOwnerId=$p.currentOwnerId;observedGeneration=$p.currentGeneration;relationshipState=$(if($phase-ceq'first-disable'){'Mounted'}else{'Unmounted'})}}})
 $o|Add-Member registeredDisable ([pscustomobject][ordered]@{contract='owner-seed-registered-disable-local-failure-v1';callback='UnityModManager.ModEntry.OnToggle';injectedPairId=$before.pairStates[1].pairId;before=$before;visits=$visits;firstAccepted=$false;held=$held;injectionCount=1;retryAccepted=$true;retry=$retry;retrySameUpdate=$true;disabledLaterFrame=$later;reenableAccepted=$true;reenabled=$reenabled;complete=$true}) -Force
 $pre=New-DisableState $v $o.mountedStart.world 'removal-refusal-before' $false;$post=Copy-Value $pre;$post.world.label='removal-refusal-after'
 $world=[ordered]@{};foreach($key in @('WorldLoaded','DefaultMode','SaveAllowed')){$world[$key]=$true};foreach($key in @('PartyInCombat','AnyPartyMemberInCombat','ActiveMountedCommand','StockAttackIntent','PairedActivation','CombatRestorationPending','LoadInFlight','LoadingWorld','LoadingProcess','SaveSuspended','ActiveSaveScope','SaveDraining','WorldHoldReleasePending','ResetToMainMenuPending','InspectionFailed')){$world[$key]=$false};$world['InspectionFailure']=$null
 $refs=@($roles[1],$roles[3]|ForEach-Object{'unit native Horse ['+$_+'] is the KMC Horse'})
 $o|Add-Member removalRefusal ([pscustomobject][ordered]@{contract='owner-seed-permanent-horse-removal-refusal-v1';assessment=[pscustomobject]@{safe=$false;mounted=$true;world=[pscustomobject]$world;reasons=$refs;permanentReferences=$refs};beginAccepted=$false;state='Refused';status='Permanent KMC references remain.';assessmentDelta=2;refusalDelta=1;cleanupSaveDelta=0;permanentHorses=@(for($i=0;$i-lt2;$i++){[pscustomobject]@{riderId=$roles[$i*2];horseId=$roles[$i*2+1];unitBlueprintGuid='4016c7db400ab721ff125aef9e65e202';featureBlueprintGuid='7db7c50677e39f09feef56f3831fc723';riderOwnsFeature=$true;reciprocalPetOwnership=$true}});before=$pre;after=$post;dismountVisits=@()}) -Force
 $v
}
Check 'closed two-owner registered disable and permanent Horse refusal' {$v=New-RegisteredDisableFixture;Assert-KmcChunk7CombatEvidence $v.request $v.artifact 'PASS'}
$faults=[ordered]@{
 'wrong callback'={param($v)$v.artifact.observations.registeredDisable.callback='direct service Cancel'}
 'first disable accepted'={param($v)$v.artifact.observations.registeredDisable.firstAccepted=$true}
 'injection count two'={param($v)$v.artifact.observations.registeredDisable.injectionCount=2}
 'retry not same update'={param($v)$v.artifact.observations.registeredDisable.retry.world.frame++}
 'later frame absent'={param($v)$d=$v.artifact.observations.registeredDisable;$d.disabledLaterFrame.world.frame=$d.retry.world.frame}
 'reenable native prepare'={param($v)$v.artifact.observations.registeredDisable.reenabled.nativePrepares[0].count++}
 'lost healthy allocation'={param($v)$v.artifact.observations.registeredDisable.held.healthyAllocationId=9902}
 'healthy mount debt changed'={param($v)$v.artifact.observations.registeredDisable.held.world.actors[1].standard++}
 'neighbor prepared change'={param($v)$v.artifact.observations.registeredDisable.held.world.actors[0].prepared=$false}
 'foreign target debt'={param($v)$v.artifact.observations.registeredDisable.retry.unrelatedActors[-1].native.Standard=3}
 'target omitted'={param($v)$v.artifact.observations.registeredDisable.retry.unrelatedActors=@()}
 'unobserved pending change'={param($v)$v.artifact.observations.registeredDisable.held.participationB.PendingSplitRound++}
 'B owner lost'={param($v)$v.artifact.observations.registeredDisable.held.pairStates[1].currentOwnerId=$null}
 'B generation replaced'={param($v)$v.artifact.observations.registeredDisable.retry.pairStates[1].currentGeneration++}
 'wrong native role form'={param($v)$v.artifact.observations.registeredDisable.visits[0].riderId=$v.request.ownedPairs.riderAId}
 'wrong visitor trigger'={param($v)$v.artifact.observations.registeredDisable.visits[0].trigger='Exception'}
 'duplicate visit'={param($v)$v.artifact.observations.registeredDisable.visits[1]=Copy-Value $v.artifact.observations.registeredDisable.visits[0]}
 'wrong observed owner'={param($v)$v.artifact.observations.registeredDisable.visits[0].observedOwnerId=$v.artifact.observations.registeredDisable.visits[1].pairId}
 'failure retained on A'={param($v)$v.artifact.observations.registeredDisable.held.pairStates[0].lastTransition.succeeded=$false}
 'failure missing on B'={param($v)$v.artifact.observations.registeredDisable.held.pairStates[1].lastTransition.errors=@()}
 'failure wrong exception'={param($v)$v.artifact.observations.registeredDisable.held.pairStates[1].lastTransition.errors=@('foreign failure')}
 'retry error retained'={param($v)$v.artifact.observations.registeredDisable.retry.pairStates[1].lastTransition.errors=@('failure')}
 'prepared pair residue'={param($v)$v.artifact.observations.registeredDisable.retry.pairStates[0].noPreparedPairResidue=$false}
 'retained command'={param($v)$v.artifact.observations.registeredDisable.retry.pairStates[1].commandsRetained=$true}
 'unsettled native shell'={param($v)$v.artifact.observations.registeredDisable.retry.pairStates[1].unsettledShell=$true}
 'disabled native fact retained'={param($v)$v.artifact.observations.registeredDisable.retry.controls.ExactFactCount=1}
 'disabled live slot retained'={param($v)$v.artifact.observations.registeredDisable.retry.slots=@([pscustomobject]@{ActorId=$v.request.ownedPairs.riderAId;Index=1;Kind=2})}
 'save during disable'={param($v)$v.artifact.observations.registeredDisable.retry.nativeWrites=1}
 'clock advanced'={param($v)$v.artifact.observations.registeredDisable.disabledLaterFrame.world.gameTicks++}
 'Pause claimed in TB'={param($v)$v.artifact.observations.registeredDisable.disabledLaterFrame.paused=$true}
 'TB mode lost later'={param($v)$v.artifact.observations.registeredDisable.disabledLaterFrame.turnBased=$false}
 'A not finalized'={param($v)$v.artifact.observations.registeredDisable.before.participationA.Activation.Finalized=$false}
 'B full end fabricated'={param($v)$v.artifact.observations.activations[1]|Add-Member afterEnd $v.artifact.observations.registeredDisable.before.world}
 'R1 route substituted'={param($v)$v.artifact.observations|Add-Member r1 ([pscustomobject]@{})}
 'first-action remount substituted'={param($v)$v.artifact.observations|Add-Member firstActionMount ([pscustomobject]@{})}
 'old local cancel substituted'={param($v)$v.artifact.observations|Add-Member localFailure ([pscustomobject]@{})}
 'Horse refusal accepted'={param($v)$v.artifact.observations.removalRefusal.beginAccepted=$true}
 'Horse assessment counter'={param($v)$v.artifact.observations.removalRefusal.assessmentDelta=1}
 'Horse cleanup write'={param($v)$v.artifact.observations.removalRefusal.cleanupSaveDelta=1}
 'Horse hidden combat refusal'={param($v)$v.artifact.observations.removalRefusal.assessment.world.PartyInCombat=$true}
 'Horse pending save refusal'={param($v)$v.artifact.observations.removalRefusal.assessment.world.SaveDraining=$true}
 'Horse permanent B missing'={param($v)$v.artifact.observations.removalRefusal.assessment.permanentReferences=@($v.artifact.observations.removalRefusal.assessment.permanentReferences[0])}
 'Horse feature absent'={param($v)$v.artifact.observations.removalRefusal.permanentHorses[0].riderOwnsFeature=$false}
 'Horse reciprocal custody broken'={param($v)$v.artifact.observations.removalRefusal.permanentHorses[1].reciprocalPetOwnership=$false}
 'Horse alternate blueprint'={param($v)$v.artifact.observations.removalRefusal.permanentHorses[1].unitBlueprintGuid='0'*32}
 'Horse refusal tears pair'={param($v)$v.artifact.observations.removalRefusal.dismountVisits=@([pscustomobject]@{trigger='ModDisabled'})}
 'Horse refusal changes debt'={param($v)$v.artifact.observations.removalRefusal.after.world.actors[1].move=2}
 'Horse refusal changes controls'={param($v)$v.artifact.observations.removalRefusal.after.controls.NativeCastRequestCount=1}
 'Horse refusal loses equal live slots'={param($v)$v.artifact.observations.removalRefusal.before.slots=@();$v.artifact.observations.removalRefusal.after.slots=@()}
 'cleanup inherited pets removed'={param($v)$v.artifact.observations.cleanup.ownedHorseRemoved=$true}
 'zero-write selection widened'={param($v)$v.request.ownedPairs.expectedNativeWriteCount=1}
 'acquired ancestry claimed'={param($v)$v.artifact.observations.inheritedFixture.contract='inherited-two-horse-native-field-v1'}
}
Check 'healthy A may retire only after its actual participation settles' {$v=New-RegisteredDisableFixture;$d=$v.artifact.observations.registeredDisable;$a=$d.before.pairStates[0].pairId
 foreach($s in @($d.held,$d.retry,$d.disabledLaterFrame,$d.reenabled)){$s.participationA=$null;$s.aCanRelease=$true;$s.world.owners=@($s.world.owners|Where-Object{(ConvertTo-KmcChunk7PairIdN $_.id)-cne$a});$p=@($s.pairStates|Where-Object pairId -CEQ $a)[0];$p.currentOwnerId=$null;$p.currentGeneration=$null;$p.currentOwnerState=$null}
 $visit=@($d.visits|Where-Object{$_.pairId-ceq$a-and$_.phase-ceq'retry-disable'})[0];$visit.observedOwnerId=$null;$visit.observedGeneration=$null
 Assert-KmcChunk7CombatEvidence $v.request $v.artifact 'PASS'}
foreach($key in $faults.Keys){$faultBody=$faults[$key];Check ('refuses '+$key) {$v=New-RegisteredDisableFixture;&$faultBody $v;Assert-KmcChunk7CombatEvidence $v.request $v.artifact 'PASS'} $true}
foreach($base in @('combat-rider-first','combat-mount-first')){Check ('old acquired family remains '+$base) {$v=Copy-Value @($fixtures|Where-Object name -CEQ $base)[0];Assert-KmcChunk7CombatEvidence $v.request $v.artifact 'PASS'}}
Check 'old family rejects new callback evidence' {$v=Copy-Value @($fixtures|Where-Object name -CEQ 'combat-rider-first')[0];$v.artifact.observations|Add-Member registeredDisable ([pscustomobject]@{});Assert-KmcChunk7CombatEvidence $v.request $v.artifact 'PASS'} $true
Check 'closed selected case and quota' {$v=New-RegisteredDisableFixture;Assert-KmcChunk7OwnedPairsSelection $v.request;if((Get-KmcChunk7SelectedScenario $v.request)-cne'chunk7-two-pair-combat-rider-first'-or(Get-KmcChunk7OwnedLoadCount $v.request)-ne1-or(Get-KmcChunk7OwnedWriteCount $v.request)-ne0){throw 'Closed mapping changed.'}}
Check 'exact emitted rows admitted by external result list' {$rows=Get-KmcChunk7CombatRuntimeRows;foreach($key in @('C7-combat-permanent-horse-removal-refusal','C7-combat-registered-disable-local-failure')){if(@($rows|Where-Object{$_-ceq$key}).Count-ne1){throw 'Exact emitted row is absent or duplicated.'}}}
foreach($leaf in @('Test-RuntimeGameResult.ps1','Test-RuntimeResult.ps1')){
 $rowAst=[Management.Automation.Language.Parser]::ParseFile((Join-Path $ReaderDirectory $leaf),[ref]$null,[ref]$null)
 $methods=@($rowAst.FindAll({param($node)$node-is[Management.Automation.Language.FunctionDefinitionAst]-and$node.Name-ceq'Assert-SubscenarioResults'},$true));if($methods.Count-ne1){throw 'Exact external runtime row validator is missing or repeated.'};. ([scriptblock]::Create($methods[0].Extent.Text))
 $runtime=[pscustomobject]@{scenario='chunk7-owned-two-pair-campaign';subscenarioTotal=3;subscenarioPassCount=3;subscenarioFailCount=0;assertionPassCount=3;assertionFailCount=0;subscenarioResults=@(@('chunk7-owned-two-pair-campaign','C7-combat-permanent-horse-removal-refusal','C7-combat-registered-disable-local-failure')|ForEach-Object{[pscustomobject]@{name=$_;status='PASS';assertionPassCount=1;assertionFailCount=0;errors=@()}})}
 Check ('composed disable artifact and external rows '+$leaf){$v=New-RegisteredDisableFixture;Assert-KmcChunk7CombatEvidence $v.request $v.artifact 'PASS';Assert-SubscenarioResults $runtime}
 Check ('external disable result rejects unknown row '+$leaf){$bad=Copy-Value $runtime;$bad.subscenarioResults[2].name+='-unknown';Assert-SubscenarioResults $bad} $true
 Check ('external disable result rejects duplicate row '+$leaf){$bad=Copy-Value $runtime;$bad.subscenarioResults[2].name=$bad.subscenarioResults[1].name;Assert-SubscenarioResults $bad} $true
}
$clock.Stop();[IO.Directory]::CreateDirectory($OutputDirectory)|Out-Null
$summary=[ordered]@{scope='private group44 generated external reader checks; no native credit';passed=@($results|Where-Object pass -EQ $true).Count;failed=@($results|Where-Object pass -EQ $false).Count;durationSeconds=$clock.Elapsed.TotalSeconds;readerDirectory=$ReaderDirectory;fixturePath=$FixturePath;inputs=@(@((Join-Path $ReaderDirectory 'Chunk7RegisteredDisableEvidence.ps1'),(Join-Path $ReaderDirectory 'Chunk7CombatEvidence.ps1'),(Join-Path $ReaderDirectory 'Chunk7OwnedPairsEvidence.ps1'),$PSCommandPath,$FixturePath)|ForEach-Object{[pscustomobject]@{path=$_;sha256=(Get-KmcSha256 $_)}});results=$results.ToArray()}
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'summary.json'),($summary|ConvertTo-Json -Depth 80),[Text.UTF8Encoding]::new($false))
Write-Output ('RESULT '+$summary.passed+' passed; '+$summary.failed+' failed; '+$clock.Elapsed.TotalSeconds+' seconds')
if($summary.failed-gt0){exit 1}
