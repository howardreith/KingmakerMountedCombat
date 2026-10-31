$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
# Sprint 2 completion: synthetic reader contracts for the owner-surface pre-owned single-pair speed mode and the
# turn-based door control stage (selection validators, evidence readers, exact refusals). No native qualification.
$passed=0
function Check([string]$Name,[scriptblock]$TrialBody){& $TrialBody;$script:passed++;Write-Host ('PASS '+$Name)}
function Reject([string]$Name,[scriptblock]$RejectedTrial){$refused=$false;try{& $RejectedTrial}catch{$refused=$true};if(-not$refused){throw ('Unexpected acceptance: '+$Name)};$script:passed++;Write-Host ('PASS refuses '+$Name)}
function Copy-TestValue($Value){return ($Value|ConvertTo-Json -Depth 90 -Compress|ConvertFrom-Json)}
function ConvertTo-OwnerKey([string]$ActorId){[Guid]::ParseExact($ActorId,'D').ToString('N')}

# --- Owner-surface speed selection -------------------------------------------------------------------------------
$rider='121dc2f0-d277-4073-80fe-29a0b3bb62db';$horse='fb764728-de1f-4cb2-b657-575b004efbb2';$other='fc538865-6959-410f-bb93-5bb94f0d83d0';$pet2='995b1a9e-0e14-4b5f-b066-495fc4e8a159'
$seedSha='1fda72b1f10d7f1373b11971b52b44e8e8de3ce14797515706e892dfd1dcce0a'
$selection=[pscustomobject][ordered]@{case='owner-surface-speed';seedId='KMC_S2_ROGUELIKE_SURFACE_CANDIDATE';seedSha256=$seedSha;riderAId=$rider;mountAId=$horse;partyIds=@($other,$rider,$pet2,$horse);baseGameOnly=$true;expectedNativeLoadCount=1;expectedNativeWriteCount=0}
$descriptor=[pscustomobject]@{internalName='After mounting';fileName='Manual_301_After_mounting.zks';sha256=$seedSha;length=596816;lastWriteTimeUtcTicks=638000000000000000;gameId='48a4bec2-a553-4f01-a722-ceacbe389d44';gameName='Akasa';area='c49315fe499f0e5468af6f19242499a2'}
$fixture=[pscustomobject]@{baseline=[pscustomobject]@{sha256=('4'*64)};working=[pscustomobject]@{sha256=('5'*64);gameId='66666666-7777-4888-8999-aaaaaaaaaaaa'}}
function New-SurfaceRequest{[pscustomobject]@{schemaVersion=2;scenario='chunk7-owned-two-pair-campaign';runId='synthetic-owner-surface';ownedPairs=(Copy-TestValue $selection);persistenceLoad=(Copy-TestValue $descriptor);fixture=(Copy-TestValue $fixture);
    branch='codex/mounted-combat-sprint2-movement-doors';commit='1'*40;productVersion='synthetic';dllSha256='2'*64;dllMvid='33333333-3333-4333-8333-333333333333'}}
Check 'owner-surface selection admitted and mapped to exploration' {$r=New-SurfaceRequest;Assert-KmcChunk7OwnedPairsSelection $r;if((Get-KmcChunk7SelectedScenario $r)-cne'chunk7-two-pair-exploration'){throw 'scenario'};if((Get-KmcChunk7OwnedWriteCount $r)-ne0){throw 'writes'};Assert-KmcChunk7OwnedPairsDescriptor $r}
Check 'owner-surface source dispatch names the owner-seed registry route' {$text=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'runtime/Chunk7OwnedPairsFixtures.ps1') -Raw;if($text-notmatch"'owner-seed-exploration','owner-seed-persistence-save','owner-surface-speed'"){throw 'dispatch'}}
Check 'owner-surface seed registered with the exact master identity' {$registry=Read-KmcChunk7OwnerSeedRegistry;$entry=@($registry.registry.seeds|Where-Object seedId -CEQ 'KMC_S2_ROGUELIKE_SURFACE_CANDIDATE');if($entry.Count-ne1-or$entry[0].sha256-cne$seedSha-or$entry[0].internalName-cne'After mounting'-or$entry[0].area-cne'c49315fe499f0e5468af6f19242499a2'-or$entry[0].campaignDlc-cne'Endless'){throw 'registry'}}
$surfaceSelectionFaults=@(
    @('second rider pinned',{param($s)$s|Add-Member riderBId '7e5cf212-d860-45d5-ac9e-9dce3c5b0350'}),
    @('assistance pinned',{param($s)$s|Add-Member xpAssistance ([pscustomobject]@{unitId=$rider;targetCharacterLevel=4})}),
    @('producer pinned',{param($s)$s|Add-Member sourceRunId 'run'}),
    @('horse outside roster',{param($s)$s.partyIds=@($other,$rider,$pet2)}),
    @('rider equals horse',{param($s)$s.mountAId=$rider}),
    @('a native write',{param($s)$s.expectedNativeWriteCount=1}),
    @('two native loads',{param($s)$s.expectedNativeLoadCount=2}),
    @('not base-only',{param($s)$s.baseGameOnly=$false}),
    @('turn-based opt-in on the surface case',{param($s)$s|Add-Member turnBasedDoor $true}),
    @('malformed seed hash',{param($s)$s.seedSha256='zz'}))
foreach($fault in $surfaceSelectionFaults){Reject ('owner-surface selection '+$fault[0]) {$r=New-SurfaceRequest;& $fault[1] $r.ownedPairs;Assert-KmcChunk7OwnedPairsSelection $r}}
Reject 'owner-surface selection with a checkpoint' {$r=New-SurfaceRequest;$r|Add-Member persistenceCase 'multi-exploration-manual';Assert-KmcChunk7OwnedPairsSelection $r}

# --- Turn-based door opt-in on the owner-seed exploration selection -------------------------------------------------
$seedA='73681ef2-b281-4bc6-bc05-2de68c1616cb';$seedB='fd213210-71cd-41b0-b35b-68e1b18e9b43'
$explore=[pscustomobject][ordered]@{case='owner-seed-exploration';seedId='KMC_C7_DOOR_SEED_OLD_SYCAMORE_OPEN';seedSha256='9078910198a3cec7447fb36b7833c6a656d29886b9d95aa0f1f0e287f0f20a6a';riderAId=$seedA;riderBId=$seedB;
    partyIds=@($seedA,'5dd7bd1d-ae4c-4df7-9687-50fceb027344','8265f8cf-0969-4ec7-a266-2448de91c8c4',$seedB,'7e5cf212-d860-45d5-ac9e-9dce3c5b0350');xpAssistance=[pscustomobject]@{unitId=$seedB;targetCharacterLevel=8};baseGameOnly=$true;expectedNativeLoadCount=1;expectedNativeWriteCount=0;turnBasedDoor=$true}
function New-ExploreRequest{[pscustomobject]@{scenario='chunk7-owned-two-pair-campaign';runId='synthetic-tb-door';ownedPairs=(Copy-TestValue $explore)}}
Check 'turn-based opt-in admitted on owner-seed exploration' {$r=New-ExploreRequest;Assert-KmcChunk7OwnedPairsSelection $r;if(-not(Test-KmcChunk7OwnedPairsTurnBasedDoor $r)){throw 'flag'}}
Check 'exploration without the opt-in is not turn-based' {$r=New-ExploreRequest;$r.ownedPairs.PSObject.Properties.Remove('turnBasedDoor');Assert-KmcChunk7OwnedPairsSelection $r;if(Test-KmcChunk7OwnedPairsTurnBasedDoor $r){throw 'flag'}}
Reject 'turn-based opt-in false' {$r=New-ExploreRequest;$r.ownedPairs.turnBasedDoor=$false;Assert-KmcChunk7OwnedPairsSelection $r}
Reject 'turn-based opt-in as text' {$r=New-ExploreRequest;$r.ownedPairs.turnBasedDoor='true';Assert-KmcChunk7OwnedPairsSelection $r}
Reject 'turn-based opt-in on the Manual producer' {$r=New-ExploreRequest;$r.ownedPairs.case='owner-seed-persistence-save';$r.ownedPairs.expectedNativeWriteCount=1;$r|Add-Member persistenceCase 'multi-exploration-manual';Assert-KmcChunk7OwnedPairsSelection $r}

# --- Turn-based door evidence reader ----------------------------------------------------------------------------------
$mountA='aaaaaaaa-1111-4111-8111-111111111111';$mountB='bbbbbbbb-2222-4222-8222-222222222222';$target='cccccccc-3333-4333-8333-333333333333';$member='5dd7bd1d-ae4c-4df7-9687-50fceb027344'
$owners=@([pscustomobject]@{ownerId='11111111-1111-4111-8111-111111111111';riderId=(ConvertTo-OwnerKey $seedA);mountId=(ConvertTo-OwnerKey $mountA);generation=3;state='Active';relationshipState='Mounted'},
    [pscustomobject]@{ownerId='22222222-2222-4222-8222-222222222222';riderId=(ConvertTo-OwnerKey $seedB);mountId=(ConvertTo-OwnerKey $mountB);generation=4;state='Active';relationshipState='Mounted'})
$door=[pscustomobject]@{fixture=[pscustomobject]@{name='TavernDoor';originalOpen=$true;originalCut=$true;cutMode='static'}}
function Cool([double]$Standard,[double]$Move){[pscustomobject]@{id='x';standard=$Standard;move=$Move;swift=0.0;prepared=$false;reactions=1;gameTicks=10000000L}}
function CoolSet([double]$RiderStandard,[double]$MountMove){[pscustomobject]@{rider=(Cool $RiderStandard 0.0);mount=(Cool 0.0 $MountMove);secondRider=(Cool 0.0 0.0);secondMount=(Cool 0.0 0.0)}}
function Delta([double]$RiderStandard,[double]$MountMove){[pscustomobject]@{rider=[pscustomobject]@{standard=$RiderStandard;move=0.0;swift=0.0};mount=[pscustomobject]@{standard=0.0;move=$MountMove;swift=0.0};secondRider=[pscustomobject]@{standard=0.0;move=0.0;swift=0.0};secondMount=[pscustomobject]@{standard=0.0;move=0.0;swift=0.0}}}
function Rejection([string]$Label,[bool]$Live){$slots=@([pscustomobject]@{id=$seedB;raw=@(0,0,0,0);queue=0;move=0;standard=0},[pscustomobject]@{id=$mountB;raw=@(0,0,0,0);queue=0;move=0;standard=0})
    [pscustomobject]@{label=$Label;riderId=$seedB;mountId=$mountB;accepted=$false;doorCommandCreated=$false;secondPairDoorInteraction=$false;feedback='Open an approached door during the rider''s turn.';rejectionCodes=@('WrongTurn');kmcWrongTurnObserved=$true;
        retainedClickCountBefore=0;retainedClickCountAfter=0;commandsBefore=(Copy-TestValue $slots);commandsAfter=(Copy-TestValue $slots);turnUnchanged=$true;firstPairLiveBefore=$Live;firstPairCommandIntact=$true}}
$outcome=[pscustomobject]@{Result='Success';RiderId=$seedA;MountId=$mountA;InteractionCount=1;DelegatedMoveStartCount=1;DelegatedMoveTickCount=0;DoorStateChanged=$true;RiderPathSuppressed=$true;MountMoveSlotRestored=$true;TerminalReason=$null;
    ApproachesWithoutProgress=0;ApproachElapsedGameSeconds=1.9;DistanceAtInteraction=1.95;LineOfSightAtInteraction=$true;OccupancyInsideNavmesh=$true;OccupancyRecovery='none';OccupancyNearestNodeDistance=0.0;
    PreventionRefused=$false;OccupancySettled=$true;OccupancySettleGameSeconds=0.02;MountCorpulence=0.5;FootprintClear=$true;FootprintRingInside=8;FootprintRingReachable=8;RecoveryDisposition='NotNeeded';RecoveryTerminal='None';RecoveryCandidatesEvaluated=0;RecoveryCandidateReasons='';RecoveryDestinationDistance=0.0;FootprintClearAfterRecovery=$null;RecoveryCancelReason=$null}
$tb=[pscustomobject]@{contract='owner-seed-turn-based-door-control-v1';turns=@([pscustomobject]@{actorId=$seedA;round=1;turn=501;stage=5;isPairRider=$true;isMount=$false;isTarget=$false},[pscustomobject]@{actorId=$member;round=1;turn=502;stage=5;isPairRider=$false;isMount=$false;isTarget=$false});
    endedTurns=@([pscustomobject]@{label='before-rider-turn';actorId=$member;round=1;turn=502;api='Game.PauseBind (native End Turn binding)'});setupInteractions=1;
    fixture=[pscustomobject]@{name='TavernDoor';object=77;position=[pscustomobject]@{x=0.0;y=0.0;z=0.0};approachPoint=[pscustomobject]@{x=0.0;y=0.0;z=0.0};radius=2.0;originalOpen=$true;originalEnabled=$true;originalCut=$true;cutMode='static';notInCombat=$false;disableOnOpen=$false;restrictionCount=0;originalAlreadyUnlocked=$true};
    setup=[pscustomobject]@{applicable=$true;startDistanceToApproachPoint=17.9;standoff=7.0;goalSearch=[pscustomobject]@{};goal=[pscustomobject]@{x=7.0;y=0.0;z=0.0};command=601;result='Success';end=[pscustomobject]@{x=7.0;y=0.0;z=0.0};distanceToApproachPointBeforeEncounter=7.1};
    control=[pscustomobject]@{applicable=$false;reason='no unmounted party turn before the rider''s turn offered a native control'};
    encounter=[pscustomobject]@{contract='owner-seed-turn-based-door-encounter-v1';basis='owner-seed-disposable-copy-turn-based-door';ownerApproval='x';seedSha256=$explore.seedSha256;placements=@();area='fb4cffc2c4279e141a1c877785bce7ac';
        initiativeInputs=@(1,2,3,4|ForEach-Object{[pscustomobject]@{actorId='a';before=0;input=0;outsideCombat=$true}});targetId=$target;targetGroupId='g';targetPosition=[pscustomobject]@{x=1.0;y=0.0;z=0.0};cooldownsBeforeEncounter=(CoolSet 0 0);
        nativeInitialOrder=@($seedA,$seedB,$member,$target);nativeInitiatives=@();roundAtStart=1;turnBasedAtStart=$true;partyInCombat=@($seedA,$seedB)};
    riderTurn=[pscustomobject]@{round=1;turn=501;status='Preparing';acting=$false;timeMoved=0.0;remainingTime=6.0;cooldowns=(CoolSet 0 0);mountPosition=[pscustomobject]@{x=7.0;y=0.0;z=0.0};distanceToApproachPoint=7.1;surprised=$false};
    rejectionBeforeClick=(Rejection 'before-first-pair-click' $false);
    interaction=[pscustomobject]@{riderId=$seedA;mountId=$mountA;ownerId='11111111-1111-4111-8111-111111111111';generation=3;accepted=$true;commandObject=602;executorId=$seedA;round=1;turn=501;distanceBefore=7.1;radius=2.0;cooldownsAtClick=(CoolSet 0 0);timeMovedAtClick=0.0;selectionAfterInput=$seedB;feedback='';rejectionCodes=@();
        outcome=(Copy-TestValue $outcome);terminal=$true;distanceMoved=5.1;doorOpenAfter=$true;cooldownsAfter=(CoolSet 6.0 1.3);timeMovedAfter=1.3;maximumTimeMovedWhileLive=1.3;livePolls=40;movingPolls=30;offTurnMovingPolls=0;startedWhileMoving=$false;unstartedWhileMoving=$true;turnChangedWhileLive=$false;turnStillRiderAfter=$true;roundAfter=1;mountPositionAfter=[pscustomobject]@{x=2.0;y=0.0;z=0.0};mountInsideNavmesh=$true;cooldownDeltas=(Delta 6.0 1.3)};
    rejectionDuringApproach=(Rejection 'during-first-pair-approach' $true);
    encounterEnd=[pscustomobject]@{targetRemoved=$true;round=1;frame=100};servicesReleased=[pscustomobject]@{initiativeRestored=$true;aiLeaseRestored=$true;targetServiceDisposed=$true;realTimeModeReselected=$true};
    afterCombat=[pscustomobject]@{turnBased=$false;playerInCombat=$false;controllerInitialized=$false;actorsIdle=$true;pairs=@([pscustomobject]@{riderId=$seedA;mountId=$mountA;retainedPairCommands=$false;activeDoorInteraction=$false;pendingSettlement=$false;mounted=$true},[pscustomobject]@{riderId=$seedB;mountId=$mountB;retainedPairCommands=$false;activeDoorInteraction=$false;pendingSettlement=$false;mounted=$true});cooldowns=(CoolSet 0 0)};
    restoration=[pscustomobject]@{exact=$true;commandsSettled=$true;open=$true;enabled=$true;cutEnabled=$true;observedAlreadyUnlockedBeforeRestore=$true;alreadyUnlocked=$true};completed=$true}
$request=New-ExploreRequest
Check 'turn-based door evidence admitted' {Assert-KmcChunk7TurnBasedDoorEvidence $request (Copy-TestValue $tb) $door $owners @($seedA,$seedB) @($mountA,$mountB)}
Check 'turn-based door evidence with an applicable native control' {$v=Copy-TestValue $tb;$v.control=[pscustomobject]@{applicable=$true;actorId=$member;accepted=$true;commandType='Kingmaker.UnitLogic.Commands.UnitInteractWithObject';commandObject=603;routedByKmc=$false;distanceBefore=6.0;round=1;turn=502;cooldownsBefore=(Cool 0 0);timeMovedBefore=0.0;timeMoved=1.1;result='Success';doorOpenAfter=$true;cooldownsAfter=(Cool 6 0);distanceAfter=1.8;turnStillControl=$true};Assert-KmcChunk7TurnBasedDoorEvidence $request $v $door $owners @($seedA,$seedB) @($mountA,$mountB)}
$tbFaults=@(
    @('not completed',{param($v)$v.completed=$false}),
    @('door forbidden in combat',{param($v)$v.fixture.notInCombat=$true}),
    @('a mount received a roster turn',{param($v)$v.turns[1].actorId=$mountA;$v.turns[1].isMount=$true}),
    @('rider never had a turn',{param($v)$v.turns=@($v.turns[1])}),
    @('second rider before the first',{param($v)$v.encounter.nativeInitialOrder=@($seedB,$seedA,$member,$target)}),
    @('encounter under another basis',{param($v)$v.encounter.basis='owner-seed-derived-disposable-copy'}),
    @('encounter of another seed',{param($v)$v.encounter.seedSha256='9'*64}),
    @('approach began inside the radius',{param($v)$v.riderTurn.distanceToApproachPoint=2.2;$v.interaction.distanceBefore=2.2}),
    @('off-turn click created a door command',{param($v)$v.rejectionBeforeClick.doorCommandCreated=$true}),
    @('off-turn click changed second pair slots',{param($v)$v.rejectionDuringApproach.commandsAfter[0].standard=9}),
    @('off-turn click changed the turn',{param($v)$v.rejectionBeforeClick.turnUnchanged=$false}),
    @('live operation not intact after the rejected click',{param($v)$v.rejectionDuringApproach.firstPairCommandIntact=$false}),
    @('rejection during approach missing live flag',{param($v)$v.rejectionDuringApproach.firstPairLiveBefore=$false}),
    @('rejected click counted as a retained click',{param($v)$v.rejectionDuringApproach.retainedClickCountAfter=1}),
    @('interaction refused',{param($v)$v.interaction.outcome.Result='Fail'}),
    @('two interactions',{param($v)$v.interaction.outcome.InteractionCount=2}),
    @('interaction outside the radius',{param($v)$v.interaction.outcome.DistanceAtInteraction=2.6}),
    @('no line of sight',{param($v)$v.interaction.outcome.LineOfSightAtInteraction=$false}),
    @('mount moved off the rider turn',{param($v)$v.interaction.offTurnMovingPolls=2}),
    @('command started while moving',{param($v)$v.interaction.startedWhileMoving=$true;$v.interaction.unstartedWhileMoving=$false}),
    @('turn changed while live',{param($v)$v.interaction.turnChangedWhileLive=$true}),
    @('stall classified',{param($v)$v.interaction.outcome.ApproachesWithoutProgress=1}),
    @('rider Standard not spent',{param($v)$v.interaction.cooldownDeltas.rider.standard=0.0}),
    @('rider Standard spent twice',{param($v)$v.interaction.cooldownDeltas.rider.standard=12.0}),
    @('mount Standard spent',{param($v)$v.interaction.cooldownDeltas.mount.standard=6.0}),
    @('a cooldown refunded',{param($v)$v.interaction.cooldownDeltas.mount.move=-0.5}),
    @('second pair cooldown changed',{param($v)$v.interaction.cooldownDeltas.secondRider.standard=6.0}),
    @('interaction on another round',{param($v)$v.interaction.round=2}),
    @('interaction by another owner generation',{param($v)$v.interaction.generation=2}),
    @('mount outside the navmesh afterwards',{param($v)$v.interaction.mountInsideNavmesh=$false}),
    @('settlement unresolved',{param($v)$v.interaction.outcome.RecoveryDisposition='Unresolved';$v.interaction.outcome.OccupancySettled=$false}),
    @('target not removed',{param($v)$v.encounterEnd.targetRemoved=$false}),
    @('mode not reselected',{param($v)$v.servicesReleased.realTimeModeReselected=$false}),
    @('still in combat afterwards',{param($v)$v.afterCombat.playerInCombat=$true}),
    @('retained pair command afterwards',{param($v)$v.afterCombat.pairs[0].retainedPairCommands=$true}),
    @('pair dismounted afterwards',{param($v)$v.afterCombat.pairs[1].mounted=$false}),
    @('door restored closed',{param($v)$v.restoration.open=$false}),
    @('door restoration inexact',{param($v)$v.restoration.exact=$false}),
    @('control routed by KMC',{param($v)$v.control=[pscustomobject]@{applicable=$true;actorId=$member;accepted=$true;commandType='Kingmaker.UnitLogic.Commands.UnitInteractWithObject';commandObject=603;routedByKmc=$true;result='Success';doorOpenAfter=$true}}),
    @('control by a pair rider',{param($v)$v.control=[pscustomobject]@{applicable=$true;actorId=$seedB;accepted=$true;commandType='Kingmaker.UnitLogic.Commands.UnitInteractWithObject';commandObject=603;routedByKmc=$false;result='Success';doorOpenAfter=$true}}),
    @('not-applicable control without a reason',{param($v)$v.control=[pscustomobject]@{applicable=$false}}),
    @('unknown member',{param($v)$v|Add-Member extra 1}),
    @('fixture differs from the real-time door',{param($v)$v.fixture.cutMode='toggles'}))
foreach($fault in $tbFaults){Reject ('turn-based door evidence '+$fault[0]) {$v=Copy-TestValue $tb;& $fault[1] $v;Assert-KmcChunk7TurnBasedDoorEvidence $request $v $door $owners @($seedA,$seedB) @($mountA,$mountB)}}

# --- Owner-surface speed evidence reader -------------------------------------------------------------------------------
function Resources{[pscustomobject]@{gameTicks=10000000L;actors=@(@($rider,$horse)|ForEach-Object{[pscustomobject]@{id=$_;move=0.0;standard=0.0;swift=0.0;prepared=$false;reactionAllowance=1;initiative=0}})}}
function Input([string]$Label,[string]$Caster,[string]$Target,[string]$Ability,[int]$Command){[pscustomobject]@{label=$Label;casterId=$Caster;targetId=$Target;abilityGuid=$Ability;clicked=$true;nativeCastDelta=1;nativeRefusalDelta=0;commandObject=$Command;shellControl=('shell-'+$Command);generationAtInit=1;selectionAfterInput=$other;shellTerminal=$true;processTerminal=$true;explorationResourcesConserved=$true;observedExactProcess=$true;resourcesBefore=(Resources);resourcesAfter=(Resources)}}
function Mover([string]$Id,[string]$Role,[int]$Command,[double]$Speed){[pscustomobject]@{id=$Id;role=$Role;commandObject=$Command;executorId=$Id;speedLimitAtClick=0.0;movementTypeAtClick='Walk';start=[pscustomobject]@{x=0.0;y=0.0;z=0.0};end=[pscustomobject]@{x=9.0;y=0.0;z=0.0};result='Success';pathLength=9.0;motionGameSeconds=(9.0/$Speed);effectiveSpeedMps=$Speed;samples=60;movingSamples=50;meanAgentSpeedMps=$Speed;maximumAgentSpeedMps=$Speed;meanEntitySpeedMps=$Speed;maximumEntitySpeedMps=$Speed;gaits=[pscustomobject]@{Walk=50};accelerated=$false;movementTypeAtEnd='Walk';goalResidual=0.1;factsAfter=[pscustomobject]@{};samplingContract='aligned-interval/v2';firstMotionGameSeconds=0.1;unalignedMotionGameSeconds=(9.0/$Speed)-0.1;effectiveSpeedMpsUnaligned=$Speed+0.05}}
function Leg([string]$Label,[string]$Selection,[string]$Primary,[string[]]$Selected,$Movers){[pscustomobject]@{label=$Label;selection=$Selection;primaryMoverId=$Primary;distance=9.0;applicable=$true;goalSearch=[pscustomobject]@{start=[pscustomobject]@{x=0.0;y=0.0;z=0.0};direction=[pscustomobject]@{x=0.0;y=0.0;z=1.0};distance=9.0;candidates=@([pscustomobject]@{index=0;bearingOffset=0.0;requested=[pscustomobject]@{x=0.0;y=0.0;z=9.0};walkable=$true;clamped=[pscustomobject]@{x=0.0;y=0.0;z=9.0};clampResidual=0.0;traceResidual=0.0;doorDistance=99.0;minimumSlack=5.0;reason='admitted'});selectedIndex=0};
    start=[pscustomobject]@{x=0.0;y=0.0;z=0.0};goal=[pscustomobject]@{x=0.0;y=0.0;z=9.0};environment=[pscustomobject]@{area=$descriptor.area;isCapital=$false;partyEncumbrance='Light';encumbranceSlowdown=$true;weather='Clear';acceleratedMoveSetting=$false;gameMode='Default';gameTicks=10000000L};selectedIds=$Selected;factsBeforeClick=@();movers=$Movers;gameSeconds=3.0}}
$legs=@((Leg 'mounted-party' 'Party' $horse @($rider,$other) @((Mover $horse 'mount' 701 2.7),(Mover $rider 'rider' 702 2.7),(Mover $other 'on-foot' 703 2.7))),(Leg 'mounted-solo' 'SecondRiderAlone' $horse @($rider) @((Mover $horse 'mount' 704 4.5),(Mover $rider 'rider' 705 4.5))),
    (Leg 'horse-solo' 'SecondMountAlone' $horse @($horse) @((Mover $horse 'pet' 706 4.2))),(Leg 'rider-solo' 'SecondRiderAlone' $rider @($rider) @((Mover $rider 'on-foot' 707 3.0))))
$ownerA=[pscustomobject]@{ownerId='33333333-3333-4333-8333-333333333333';riderId=(ConvertTo-OwnerKey $rider);mountId=(ConvertTo-OwnerKey $horse);generation=1;state='Active';relationshipState='Mounted'}
$party=@([pscustomobject]@{id=$other;blueprint='human';petId=$pet2;masterId=$null},[pscustomobject]@{id=$rider;blueprint='human';petId=$horse;masterId=$null},[pscustomobject]@{id=$pet2;blueprint='wolf';petId=$null;masterId=$other},[pscustomobject]@{id=$horse;blueprint='4016c7db400ab721ff125aef9e65e202';petId=$null;masterId=$rider})
$commands=@(801,802)+@(701..707)|ForEach-Object{[pscustomobject]@{commandObject=$_;finished=$true;hasExecutionProcess=$false;processEnded=$true}}
$surface=[pscustomobject]@{capability=[pscustomobject]@{archiveNativeEnabled=$true;nativeWriterSchema=3;nativePairCapacity=2;workingSha256=$null;inheritedManualSha256=$null;ownerSeedSha256=$seedSha};partyBefore=$party;
    ownerSurface=[pscustomobject]@{contract='owner-surface-pre-owned-pair-speed-v1';seedId=$selection.seedId;seedSha256=$seedSha;riderId=$rider;horseId=$horse;profileId='medium-humanoid-horse-v1';horseBlueprint='4016c7db400ab721ff125aef9e65e202';horseFeatureSourceGuid='97261d609529d834eba4fd4da1bc44dc';
        preOwnedPair=[pscustomobject]@{riderId=$rider;horseId=$horse;petIsKmcHorse=$true;petReciprocal=$true;petInField=$true;horseFeature=$true;horseRank=$true;characterLevel=5;rangerLevel=4;experience=15000;readyLevel=5;source='taken by the owner in ordinary play with KMC installed; verified only'};
        fixtureSetupOnly=$false;acquisitionProof=$false;producerAncestry=$false;nativeLoadVerifiedByHost=$true;loadedArchiveKind='Missing';loadedArchiveCanRestore=$false;mutations='none: no build, grant, level, XP, item, money or save';secondPair=$false;selectionChangeActorId=$other;area=$descriptor.area;gameName='Akasa';gameId=$descriptor.gameId;partyIds=@($other,$rider,$pet2,$horse);legsCompleted=4;legsApplicable=4};
    inputs=@((Input 'mount-owner-pair' $rider $horse 'f053faad986631688defa003cd7bda0e' 801),(Input 'dismount-owner-pair' $rider $rider '3af2b81f4d72bbb30501fa730fcdf36e' 802));
    snapshots=@([pscustomobject]@{label='owner-surface-start';owners=@()},[pscustomobject]@{label='owner-surface-mounted';owners=@((Copy-TestValue $ownerA))},[pscustomobject]@{label='mount-owner-pair-settled';owners=@((Copy-TestValue $ownerA))},[pscustomobject]@{label='owner-surface-dismounted';owners=@()},[pscustomobject]@{label='owner-surface-legs-complete';owners=@()});
    cycles=@();measurements=@();movements=@();cycleInputs=@();groundInputs=@();speedLegContract='sprint2-speed-legs/v2';speedLegs=$legs;combatOrPersistenceProof=$false;thirdUnrelatedActorProven=$false;
    factRemovalAdmission=[pscustomobject]@{frame=500;gameTicks=20000000L;owners=0;activePairs=0;preparationPending=$false;serializationBusy=$false;pendingNativeWrites=$false;commands=$commands};
    cleanup=[pscustomobject]@{pairResidueAbsent=$true;horseRemoved=$false;inheritedPetsPreserved=$false;controlsSettled=$true;settingsAndSelectionRestored=$true;nativeProcessesSettled=$true;cleanupCustodySettled=$true;nativeProgressionRequiresExternalWorkingRestoration=$false;ordinaryRespecProven=$false;nativeSaveWritten=$false;ownerSurfacePairPreserved=$true;
        world=[pscustomobject]@{owners=@([pscustomobject]@{ownerId='33333333-3333-4333-8333-333333333333';state='Retired'})};partyAfter=(Copy-TestValue $party);selectionBefore=@($rider);selectionAfter=@($rider);pauseBefore=$false;pauseAfter=$false;movementBefore=$false;movementAfter=$false;pairedBefore=$true;pairedAfter=$true;
        pairBoundaries=@([pscustomobject]@{state='Unmounted';residueAbsent=$true;ownerPresent=$false})}}
$surfaceRequest=New-SurfaceRequest
Check 'owner-surface evidence admitted' {Assert-KmcChunk7OwnerSurfaceSpeedEvidence $surfaceRequest (Copy-TestValue $surface)}
$surfaceFaults=@(
    @('a leg not applicable',{param($v)$v.speedLegs[2]=[pscustomobject]@{label='horse-solo';selection='SecondMountAlone';primaryMoverId=$horse;distance=9.0;applicable=$false;goalSearch=$v.speedLegs[2].goalSearch;reason='no route'}}),
    @('three legs only',{param($v)$v.speedLegs=@($v.speedLegs[0..2]);$v.ownerSurface.legsCompleted=3;$v.ownerSurface.legsApplicable=3}),
    @('rider-solo measured the horse',{param($v)$v.speedLegs[3].primaryMoverId=$horse;$v.speedLegs[3].movers=@((Mover $horse 'pet' 707 3.0))}),
    @('leg on another area',{param($v)$v.speedLegs[1].environment.area='0'*32}),
    @('party leg without the party',{param($v)$v.speedLegs[0].selectedIds=@($rider)}),
    @('a second pair',{param($v)$v.ownerSurface.secondPair=$true}),
    @('fixture setup claimed',{param($v)$v.ownerSurface.fixtureSetupOnly=$true}),
    @('loaded through a KMC archive',{param($v)$v.ownerSurface.loadedArchiveKind='Current'}),
    @('a mutation declared',{param($v)$v.ownerSurface.mutations='built rider'}),
    @('another horse',{param($v)$v.ownerSurface.horseId=$pet2}),
    @('horse not reciprocal in the roster',{param($v)$v.partyBefore[3].masterId=$other}),
    @('a third input',{param($v)$v.inputs+=(Input 'mount-owner-pair' $rider $horse 'f053faad986631688defa003cd7bda0e' 803)}),
    @('mounted window without the owner',{param($v)$v.snapshots[1].owners=@()}),
    @('legs-complete window still mounted',{param($v)$v.snapshots[4].owners=@((Copy-TestValue $ownerA))}),
    @('owner pair removed by cleanup',{param($v)$v.cleanup.ownerSurfacePairPreserved=$false}),
    @('horse removed by cleanup',{param($v)$v.cleanup.horseRemoved=$true}),
    @('save written',{param($v)$v.cleanup.nativeSaveWritten=$true}),
    @('party changed by cleanup',{param($v)$v.cleanup.partyAfter=@($v.cleanup.partyAfter[0..2])}),
    @('cycles present',{param($v)$v.cycles=@(1)}),
    @('door present',{param($v)$v|Add-Member door ([pscustomobject]@{})}),
    @('route goal marker present',{param($v)$v|Add-Member routeGoalContract 'world-route-goal-clear-of-units/v1'}),
    @('admission missing a leg command',{param($v)$v.factRemovalAdmission.commands=@($v.factRemovalAdmission.commands[0..7])}),
    @('capability claims Working provenance',{param($v)$v.capability.workingSha256='5'*64}),
    @('legs applicable count wrong',{param($v)$v.ownerSurface.legsApplicable=3}))
foreach($fault in $surfaceFaults){Reject ('owner-surface evidence '+$fault[0]) {$v=Copy-TestValue $surface;& $fault[1] $v;Assert-KmcChunk7OwnerSurfaceSpeedEvidence $surfaceRequest $v}}
Check 'owner-surface and turn-based rows registered' {$rows=Get-KmcChunk7WorldRows;foreach($row in @('C7-world-owner-surface-mount','C7-world-owner-surface-legs','C7-world-turn-based-door')){if($rows-cnotcontains$row){throw $row}}}
Check 'new scripts parse' {foreach($file in @('runtime/Chunk7WorldEvidence.ps1','runtime/Chunk7OwnedPairsEvidence.ps1','runtime/Chunk7OwnedPairsFixtures.ps1','runtime/Chunk7MatchedPoolEvidence.ps1','runtime/Invoke-KingmakerRuntimeScenario.ps1')){$syntax=$null;$tokens=$null;[void][Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot $file),[ref]$tokens,[ref]$syntax);if(@($syntax).Count-ne0){throw ($file+': '+$syntax[0].Message)}}}
Write-Host ('TOTAL PASS='+$passed+' FAIL=0; synthetic reader contracts only, no native qualification')
