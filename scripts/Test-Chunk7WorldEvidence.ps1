$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
$passed=0
function Check([string]$Name,[scriptblock]$TrialBody){& $TrialBody;$script:passed++;Write-Host ('PASS '+$Name)}
function Reject([string]$Name,[scriptblock]$RejectedTrial){$refused=$false;try{& $RejectedTrial}catch{$refused=$true};if(-not$refused){throw ('Unexpected acceptance: '+$Name)};$script:passed++;Write-Host ('PASS refuses '+$Name)}
function Copy-TestValue($Value){return ($Value|ConvertTo-Json -Depth 90 -Compress|ConvertFrom-Json)}
$first='b6628a77-4962-47a4-a17c-88d9836fc9d5';$mammoth='d79a4f6c-b74e-4868-95bd-533899131acb';$second='d17c8fd0-6627-40c4-bf9a-024cf001c1f7';$horse='77777777-7777-4777-8777-777777777777'
$ids=@($first,$mammoth,$second,$horse)
$request=[pscustomobject]@{schemaVersion=2;scenario='chunk7-two-pair-exploration';runId='synthetic-c7-reader';branch='codex/mounted-combat-chunk7-foundation';commit='1'*40;
    productVersion='synthetic';dllSha256='2'*64;dllMvid='33333333-3333-4333-8333-333333333333';fixture=[pscustomobject]@{working=[pscustomobject]@{sha256='5eb4e0b4cbd8d60dc879a02ff71aadfde3f517304754857f0cc68d0f9a93f1c6'}}}
function New-Artifact([string]$Kind,$Observations){
    $value=[ordered]@{schemaVersion=1;evidenceKind=$Kind;createdAtUtc='2026-10-12T00:00:00Z';status='PASS';errors=@();observations=$Observations}
    foreach($field in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid')){$value[$field]=$request.$field}
    return [pscustomobject]$value
}
function Actor([string]$Id,[string]$Blueprint,[AllowNull()][string]$Pet,[AllowNull()][string]$Master){[pscustomobject]@{id=$Id;blueprint=$Blueprint;petId=$Pet;masterId=$Master}}
$party=@((Actor $first 'rider-one' $mammoth $null),(Actor $mammoth 'e7aa96d15a45238438ae4cfb476f6bb9' $null $first),(Actor $second 'rider-two' $null $null))
$withHorse=Copy-TestValue $party;$withHorse[2].petId=$horse;$withHorse+=Actor $horse '4016c7db400ab721ff125aef9e65e202' $null $second
$resources=[pscustomobject]@{gameTicks=10000000L;actors=@($ids|ForEach-Object{[pscustomobject]@{id=$_;move=0.0;standard=0.0;swift=0.0;prepared=$false;reactionAllowance=1;initiative=0}})}
$inputRows=@();$specs=@(@('mount-mammoth',$first,$mammoth,'f053faad986631688defa003cd7bda0e',$second),@('mount-horse',$second,$horse,'f053faad986631688defa003cd7bda0e',$first),
    @('dismount-mammoth',$first,$first,'3af2b81f4d72bbb30501fa730fcdf36e',$second),@('dismount-horse',$second,$second,'3af2b81f4d72bbb30501fa730fcdf36e',$first))
for($i=0;$i-lt4;$i++){$s=$specs[$i];$inputRows+=[pscustomobject]@{label=$s[0];casterId=$s[1];targetId=$s[2];abilityGuid=$s[3];clicked=$true;nativeCastDelta=1;nativeRefusalDelta=0;commandObject=100+$i;
    shellControl=('shell-'+$i);generationAtInit=1;selectionAfterInput=$s[4];shellTerminal=$true;processTerminal=$true;resourcesBefore=(Copy-TestValue $resources);resourcesAfter=(Copy-TestValue $resources)}}
# The producer's own forms: owner records hold rider/mount ids as the registry key
# (MountedPairRegistry.Key: Guid "N"), while actor UniqueIds are Guid "D".
function ConvertTo-OwnerKey([string]$ActorId){[Guid]::ParseExact($ActorId,'D').ToString('N')}
$owners=@([pscustomobject]@{ownerId='11111111-1111-4111-8111-111111111111';riderId=(ConvertTo-OwnerKey $first);mountId=(ConvertTo-OwnerKey $mammoth);generation=1;state='Active';relationshipState='Mounted'},
    [pscustomobject]@{ownerId='22222222-2222-4222-8222-222222222222';riderId=(ConvertTo-OwnerKey $second);mountId=(ConvertTo-OwnerKey $horse);generation=2;state='Active';relationshipState='Mounted'})
$snapshots=@([pscustomobject]@{label='both-mounted';owners=(Copy-TestValue $owners)},[pscustomobject]@{label='movement-complete';owners=(Copy-TestValue $owners)},
    [pscustomobject]@{label='first-dismounted-second-retained';owners=@((Copy-TestValue $owners[1]))})
$moveRows=@();$moves=@(@('mammoth',$first,$mammoth,$second),@('horse',$second,$horse,$first),@('unmounted-control',$first,$first,$second))
for($i=0;$i-lt3;$i++){$s=$moves[$i];$commands=@($ids|ForEach-Object{[pscustomobject]@{id=$_;moveObject=0}});$after=Copy-TestValue $commands;@($after|Where-Object id -CEQ $s[2])[0].moveObject=200+$i
    $moveRows+=[pscustomobject]@{label=$s[0];selectedId=$s[1];executorId=$s[2];commandObject=200+$i;selectionAfterInput=$s[3];commandTerminal=$true;
        commandsBefore=$commands;commandsAfter=$after;start=[pscustomobject]@{x=0.0;y=0.0;z=0.0};end=[pscustomobject]@{x=2.0;y=0.0;z=0.0};requestedGoal=[pscustomobject]@{x=2.0;y=0.0;z=0.0}}}
$observation=[pscustomobject]@{capability=[pscustomobject]@{archiveNativeEnabled=$true;nativeWriterSchema=3;nativePairCapacity=2;workingSha256=$request.fixture.working.sha256};partyBefore=$party;partyWithHorse=$withHorse;
    acquisition=[pscustomobject]@{nativeRangerCommitCount=4;huntersBondSelectionLevel=4;rangerCompanionSelectionLevel=4;horseFactRankAtCommit=1;horsePresentAtNativeCommit=$true;horseFeatureSourceGuid='native-ranger'};
    inputs=$inputRows;snapshots=$snapshots;movements=$moveRows;combatOrPersistenceProof=$false;thirdUnrelatedActorProven=$false;
    cleanup=[pscustomobject]@{pairResidueAbsent=$true;horseRemoved=$true;controlsSettled=$true;settingsAndSelectionRestored=$true;nativeProcessesSettled=$true;cleanupCustodySettled=$true;nativeSaveWritten=$false;ordinaryRespecProven=$false;
        nativeProgressionRequiresExternalWorkingRestoration=$true;partyAfter=(Copy-TestValue $party);selectionBefore=@($first);selectionAfter=@($first);pauseBefore=$true;pauseAfter=$true;
        movementBefore=$false;movementAfter=$false;pairedBefore=$true;pairedAfter=$true;world=[pscustomobject]@{owners=@()};
        pairBoundaries=@([pscustomobject]@{state='Unmounted';residueAbsent=$true;ownerPresent=$false},[pscustomobject]@{state='Unmounted';residueAbsent=$true;ownerPresent=$false})}}
$subscriptions=[pscustomobject]@{executing=$false;uniqueOwnedSubscribers=1;ownedInterfaceEntries=1;entries=@([pscustomobject]@{interface='native-interface';type='KmcSubscriber';identity=77})}
$measured=@();$labels=@('unmounted-0','mounted-1','mounted-2','unmounted-after-cycles');$counts=@(0,1,2,0)
for($i=0;$i-lt4;$i++) {
    $ownership=[pscustomobject]@{active=$counts[$i];owners=$counts[$i];boundaries=2;failedConstructions=0;participants=2;liveControlBoundaries=0;preparedOrViewResidue=$counts[$i];allocationActors=0}
    $measured+=[pscustomobject]@{label=$labels[$i];warmupFrames=30;sampleFrames=120;startFrame=1;endFrame=121;stopwatchFrequency=1000000L;
        serviceTicks=@(1..120|ForEach-Object{100L});frameSeconds=@(1..120|ForEach-Object{0.016});serviceMeanMs=0.1;serviceP50Ms=0.1;serviceP95Ms=0.1;frameP50Ms=16.0;frameP95Ms=16.0;
        managedHeapBefore=100000L;managedHeapAfter=110000L;managedHeapDelta=10000L;forcedCollection=$false;collectionsBefore=@(1,1,1);collectionsAfter=@(1,1,1);kmcLogMessages=2L;kmcLogCharacters=30L;
        ownersBefore=(Copy-TestValue $ownership);ownersAfter=(Copy-TestValue $ownership);subscriptionsBefore=(Copy-TestValue $subscriptions);subscriptionsAfter=(Copy-TestValue $subscriptions)}
}
$cycleRows=@();$cycleSnapshots=@()
for($n=1;$n-le3;$n++) {
    foreach($input in $inputRows){$row=Copy-TestValue $input;$row.label='cycle-'+$n+'-'+$row.label;$row.commandObject+=400*$n;$row.shellControl='cycle-'+$n+'-'+$row.shellControl;$row.generationAtInit+=2*$n;$cycleRows+=$row}
    $cycleOwners=Copy-TestValue $owners;foreach($owner in $cycleOwners){$owner.ownerId=[Guid]::NewGuid().ToString();$owner.generation+=2*$n}
    $cycleSnapshots+=[pscustomobject]@{label=('cycle-'+$n+'-mounted');owners=$cycleOwners;boundaryCount=2;participantCount=2;subscriptions=(Copy-TestValue $subscriptions)}
    $cycleSnapshots+=[pscustomobject]@{label=('cycle-'+$n+'-settled');owners=@();boundaryCount=2;participantCount=2;subscriptions=(Copy-TestValue $subscriptions)}
}
$observation|Add-Member measurements $measured
$observation|Add-Member cycleInputs $cycleRows
$observation|Add-Member cycles $cycleSnapshots
$removalCommands=@(@($inputRows)+@($cycleRows)+@($moveRows)|ForEach-Object{[pscustomobject]@{commandObject=$_.commandObject;finished=$true;hasExecutionProcess=$true;processEnded=$true}})
$observation|Add-Member factRemovalAdmission ([pscustomobject]@{frame=123;gameTicks=10000000L;owners=0;activePairs=0;preparationPending=$false;serializationBusy=$false;pendingNativeWrites=$false;commands=$removalCommands})
# Settled cut state at the six lease boundaries: a toggling cut is the inverse of the open state;
# a static cut keeps its captured value throughout.
function New-DoorCutObservations([bool]$StartOpen,[bool]$Toggles,[bool]$StaticCut){
 @(@('captured',$StartOpen),@('input-ready-0',$false),@('after-click-0',$true),@('input-ready-1',$false),@('after-click-1',$true),@('restored',$StartOpen))|ForEach-Object{
  [pscustomobject]@{boundary=$_[0];open=[bool]$_[1];cutEnabled=$(if($Toggles){-not[bool]$_[1]}else{$StaticCut})}}
}
$doorRiders=@($first,$second);$doorMounts=@($mammoth,$horse)
$doorInputs=@();for($i=0;$i-lt2;$i++){$own=$owners[$i];$doorInputs+=[pscustomobject]@{pair=$i;riderId=$doorRiders[$i];mountId=$doorMounts[$i];ownerId=$own.ownerId;generation=$own.generation;commandObject=1800+$i;executorId=$doorRiders[$i];distanceBefore=8.0;radius=2.0;selectionAfterInput=$doorRiders[1-$i];accepted=$true;unrelatedIdleAtInput=$true;terminal=$true;ownerContinuity=$true;unrelatedIdleAtCompletion=$true;distanceMoved=6.0;outcome=[pscustomobject]@{Result='Success';RiderId=$doorRiders[$i];MountId=$doorMounts[$i];InteractionCount=1;DelegatedMoveStartCount=1;DelegatedMoveTickCount=0;DoorStateChanged=$true;RiderPathSuppressed=$true;MountMoveSlotRestored=$true;TerminalReason=$null}}}
$door=[pscustomobject]@{contract='two-pair-native-door-click-selection-change';interactions=$doorInputs;setupInteractions=2;cutObservations=@(New-DoorCutObservations $true $true $false);completed=$true;fixture=[pscustomobject]@{name='native fixture door';object=-100;position=[pscustomobject]@{x=10.0;y=0.0;z=0.0};originalOpen=$true;originalEnabled=$false;originalCut=$false;cutMode='toggles';disableOnOpen=$true;radius=2.0;interactable=$true;availableInteractions=$true;revealed=$true;hasVisibleTrap=$false;trapActive=$false;originalAlreadyUnlocked=$false;restrictionCount=0;hasDialog=$false;dataPresent=$true;viewTriggerCount=0;viewTriggerActions=$false};firstPairReturn=[pscustomobject]@{command=1802;executorId=$mammoth;terminal=$true;result='Success';distanceToStart=0.5};restoration=[pscustomobject]@{exact=$true;commandsSettled=$true;open=$true;enabled=$false;cutEnabled=$false;observedAlreadyUnlockedBeforeRestore=$true;alreadyUnlocked=$false}}
$observation|Add-Member door $door
$observation.snapshots+= [pscustomobject]@{label='door-complete';owners=(Copy-TestValue $owners)}
$observation.factRemovalAdmission.commands+=@((1800..1802)|ForEach-Object{[pscustomobject]@{commandObject=$_;finished=$true;hasExecutionProcess=$false;processEnded=$true}})


for($i=0;$i-lt3;$i++) {
    $move=$observation.movements[$i];$liveOwners=if($i-lt2){Copy-TestValue $owners}else{@((Copy-TestValue $owners[1]))}
    $entry=[pscustomobject]@{label=('quiet-before-'+$move.label);frame=(200+$i*40);selection=@($move.selectionAfterInput);resources=(Copy-TestValue $resources);boundaryCount=2;participantCount=2;subscriptions=(Copy-TestValue $subscriptions);owners=@($liveOwners)}
    $exit=Copy-TestValue $entry;$exit.label='quiet-after-'+$move.label;$exit.frame+=20;$exit.resources.gameTicks+=5000000L
    $timer=[pscustomobject]@{lastSampleSeconds=2.0;sampleSequence=0L;fileCreated=$false;samplingIntervalSeconds=0.1;clockSeconds=3.0}
    $afterTimer=Copy-TestValue $timer;$afterTimer.clockSeconds=3.5
    $move|Add-Member optionalCaptureWindow ([pscustomobject]@{contract='chunk7-optional-capture-quiet-window-v1';label=$move.label;startFrame=$entry.frame;endFrame=$exit.frame;startSeconds=10.0;endSeconds=10.5;minimumFrames=12;maximumFrames=1800;minimumSeconds=0.25;maximumSeconds=15.0;telemetryBefore=$timer;telemetryAfter=$afterTimer;measurementBeforePresent=$false;measurementAfterPresent=$false;fullyInert=$false;performanceProof=$false;allocationRateProof=$false;remainingObservers=@('guarded-host-heartbeat','exact-native-world-guard','production-lifecycle-records','native-command-settlement-polling','entry-exit-snapshots');gateReleased=$true;before=$entry;after=$exit})
}

$good=New-Artifact 'chunk7-world-evidence' $observation
Check 'complete synthetic exploration observations' {Assert-KmcChunk7WorldEvidence $request $good 'PASS'}

$slowCadence=Copy-TestValue $good;$q=$slowCadence.observations.movements[0].optionalCaptureWindow;$q.telemetryBefore.samplingIntervalSeconds=1.0;$q.telemetryAfter.samplingIntervalSeconds=1.0;$q.endSeconds=$q.startSeconds+2.0;$q.telemetryAfter.clockSeconds=$q.telemetryBefore.clockSeconds+2.0
Check 'quiet captured one-second cadence spans both exact two-second clocks' { Assert-KmcChunk7WorldEvidence $request $slowCadence 'PASS' }
$quietMutations=@(
 @('quiet slow prior cadence before required seconds',{param($v)$q=$v.observations.movements[0].optionalCaptureWindow;$q.telemetryBefore.samplingIntervalSeconds=1.0;$q.telemetryAfter.samplingIntervalSeconds=1.0;$q.endSeconds=$q.startSeconds+1.5;$q.telemetryAfter.clockSeconds=$q.telemetryBefore.clockSeconds+1.5}),
 @('quiet native clock shorter than captured cadence bound',{param($v)$q=$v.observations.movements[0].optionalCaptureWindow;$q.telemetryBefore.samplingIntervalSeconds=1.0;$q.telemetryAfter.samplingIntervalSeconds=1.0;$q.endSeconds=$q.startSeconds+1.5;$q.telemetryAfter.clockSeconds=$q.telemetryBefore.clockSeconds+2.0}),
 @('quiet writer clock shorter than captured cadence bound',{param($v)$q=$v.observations.movements[0].optionalCaptureWindow;$q.telemetryBefore.samplingIntervalSeconds=1.0;$q.telemetryAfter.samplingIntervalSeconds=1.0;$q.endSeconds=$q.startSeconds+2.0;$q.telemetryAfter.clockSeconds=$q.telemetryBefore.clockSeconds+1.5}),
 @('quiet native command polling omitted',{param($v)$v.observations.movements[0].optionalCaptureWindow.remainingObservers=@('guarded-host-heartbeat','exact-native-world-guard','production-lifecycle-records','entry-exit-snapshots')}),
 @('quiet window omitted',{param($v)$v.observations.movements[0].PSObject.Properties.Remove('optionalCaptureWindow')}),
 @('quiet gate retained',{param($v)$v.observations.movements[0].optionalCaptureWindow.gateReleased=$false}),
 @('quiet start frame text',{param($v)$v.observations.movements[0].optionalCaptureWindow.startFrame='200'}),
 @('quiet too few native frames',{param($v)$q=$v.observations.movements[0].optionalCaptureWindow;$q.endFrame=$q.startFrame+1;$q.after.frame=$q.endFrame}),
 @('quiet wall interval too short',{param($v)$q=$v.observations.movements[0].optionalCaptureWindow;$q.endSeconds=$q.startSeconds+0.1}),
 @('quiet wall interval over bound',{param($v)$q=$v.observations.movements[0].optionalCaptureWindow;$q.endSeconds=$q.startSeconds+16}),
 @('quiet timer advanced despite zero rows',{param($v)$v.observations.movements[0].optionalCaptureWindow.telemetryAfter.lastSampleSeconds=2.1}),
 @('quiet fake zero elapsed sampling clock',{param($v)$v.observations.movements[0].optionalCaptureWindow.telemetryAfter.clockSeconds=3.0}),
 @('quiet row written',{param($v)$v.observations.movements[0].optionalCaptureWindow.telemetryAfter.sampleSequence=1L}),
 @('quiet row counter text',{param($v)$v.observations.movements[0].optionalCaptureWindow.telemetryAfter.sampleSequence='0'}),
 @('quiet timer text',{param($v)$v.observations.movements[0].optionalCaptureWindow.telemetryAfter.lastSampleSeconds='2.0'}),
 @('quiet fabricated telemetry file',{param($v)$v.observations.movements[0].optionalCaptureWindow.telemetryAfter.fileCreated=$true}),
 @('quiet interval changed',{param($v)$v.observations.movements[0].optionalCaptureWindow.telemetryAfter.samplingIntervalSeconds=0.2}),
 @('quiet measurement admitted',{param($v)$v.observations.movements[0].optionalCaptureWindow.measurementAfterPresent=$true}),
 @('quiet measurement boolean text',{param($v)$v.observations.movements[0].optionalCaptureWindow.measurementBeforePresent='false'}),
 @('quiet claims inert play',{param($v)$v.observations.movements[0].optionalCaptureWindow.fullyInert=$true}),
 @('quiet claims allocation rate',{param($v)$v.observations.movements[0].optionalCaptureWindow.allocationRateProof=$true}),
 @('quiet heartbeat omitted',{param($v)$v.observations.movements[0].optionalCaptureWindow.remainingObservers=@('exact-native-world-guard','production-lifecycle-records','entry-exit-snapshots')}),
 @('quiet wrong phase frame',{param($v)$v.observations.movements[0].optionalCaptureWindow.after.frame++}),
 @('quiet selected execution actor',{param($v)$v.observations.movements[0].optionalCaptureWindow.after.selection=@($mammoth)}),
 @('quiet lost neighboring owner',{param($v)$v.observations.movements[0].optionalCaptureWindow.after.owners=@($v.observations.movements[0].optionalCaptureWindow.after.owners[0])}),
 @('quiet neighbor generation changed',{param($v)$v.observations.movements[0].optionalCaptureWindow.after.owners[1].generation++}),
 @('quiet neighbor debt changed',{param($v)$v.observations.movements[0].optionalCaptureWindow.after.resources.actors[2].standard=6.0}),
 @('quiet unmounted control reattached',{param($v)$v.observations.movements[2].optionalCaptureWindow.after.owners+=Copy-TestValue $owners[0]}),
 @('quiet subscription accumulation',{param($v)$v.observations.movements[0].optionalCaptureWindow.after.subscriptions.entries[0].identity++})
)
foreach($mutation in $quietMutations){$copy=Copy-TestValue $good;& $mutation[1] $copy;Reject $mutation[0] {Assert-KmcChunk7WorldEvidence $request $copy 'PASS'}}

$doorMutations=@(
 @('door missing',{param($v)$v.observations.PSObject.Properties.Remove('door')}),
 @('door completion string',{param($v)$v.observations.door.completed='true'}),
 @('door setup count coercion',{param($v)$v.observations.door.setupInteractions='2'}),
 @('door missing second input',{param($v)$v.observations.door.interactions=@($v.observations.door.interactions[0])}),
 @('door wrong pair generation',{param($v)$v.observations.door.interactions[0].generation++}),
 @('door selected pair takeover',{param($v)$v.observations.door.interactions[0].executorId=$second}),
 @('door duplicate command',{param($v)$v.observations.door.interactions[1].commandObject=$v.observations.door.interactions[0].commandObject}),
 @('door no changed selection',{param($v)$v.observations.door.interactions[0].selectionAfterInput=$first}),
 @('door immediate interaction',{param($v)$v.observations.door.interactions[0].distanceBefore=2.0}),
 @('door no approach movement',{param($v)$v.observations.door.interactions[0].distanceMoved=0.0}),
 @('door nonfinite distance',{param($v)$v.observations.door.interactions[0].distanceMoved=[double]::NaN}),
 @('door other pair affected',{param($v)$v.observations.door.interactions[0].unrelatedIdleAtCompletion=$false}),
 @('door another native interaction',{param($v)$v.observations.door.interactions[0].outcome.InteractionCount=2}),
 @('door retained move slot',{param($v)$v.observations.door.interactions[0].outcome.MountMoveSlotRestored=$false}),
 @('door terminal failure',{param($v)$v.observations.door.interactions[0].outcome.Result='Fail'}),
 @('door false exact restoration',{param($v)$v.observations.door.restoration.enabled=$true}),
 @('door restore before commands settle',{param($v)$v.observations.door.restoration.commandsSettled=$false}),
 @('door first pair return lost',{param($v)$v.observations.door.firstPairReturn.executorId=$horse}),
 @('door command excluded from fact-removal fence',{param($v)$v.observations.factRemovalAdmission.commands=@($v.observations.factRemovalAdmission.commands|Where-Object commandObject -NE 1800)}),
 @('door capture owner changed',{param($v)@($v.observations.snapshots|Where-Object label -CEQ 'door-complete')[0].owners[0].generation++})
)
foreach($mutation in $doorMutations){$copy=Copy-TestValue $good;& $mutation[1] $copy;Reject $mutation[0] {Assert-KmcChunk7WorldEvidence $request $copy 'PASS'}}
# A door captured closed: only the second input is preceded by a setup close, and
# restoration returns it closed with its active cut and captured unlock flag. The
# current fixture shape records why ordinary player input could open it.
function Set-CurrentDoorShape($v){$d=$v.observations.door
 foreach($pair in @(@('interactable',$true),@('availableInteractions',$true),@('revealed',$true),@('hasVisibleTrap',$false),@('trapActive',$false),@('originalAlreadyUnlocked',$false),@('restrictionCount',0),@('hasDialog',$false),@('dataPresent',$true),@('viewTriggerCount',0),@('viewTriggerActions',$false))){$d.fixture|Add-Member $pair[0] $pair[1] -Force}
 $d.restoration|Add-Member alreadyUnlocked $false -Force}
function Set-ClosedDoorStart($v){Set-CurrentDoorShape $v;$d=$v.observations.door;$d.setupInteractions=1;$d.fixture.originalOpen=$false;$d.fixture.originalCut=$true;$d.fixture.originalEnabled=$true;$d.restoration.open=$false;$d.restoration.cutEnabled=$true;$d.restoration.enabled=$true;$d.cutObservations=@(New-DoorCutObservations $false $true $false)}
Check 'door captured closed with one setup close and closed restoration' {$copy=Copy-TestValue $good;Set-ClosedDoorStart $copy;Assert-KmcChunk7WorldEvidence $request $copy 'PASS'}
Check 'current-shape open door with recorded clickability flags' {$copy=Copy-TestValue $good;Set-CurrentDoorShape $copy;Assert-KmcChunk7WorldEvidence $request $copy 'PASS'}
Check 'closed door already unlocked without a restriction' {$copy=Copy-TestValue $good;Set-ClosedDoorStart $copy;$copy.observations.door.fixture.originalAlreadyUnlocked=$true;$copy.observations.door.restoration.alreadyUnlocked=$true;Assert-KmcChunk7WorldEvidence $request $copy 'PASS'}
Check 'closed door with an inert door-view trigger' {$copy=Copy-TestValue $good;Set-ClosedDoorStart $copy;$copy.observations.door.fixture.viewTriggerCount=1;Assert-KmcChunk7WorldEvidence $request $copy 'PASS'}
$closedDoorMutations=@(
 @('closed door claims two setup closes',{param($v)$v.observations.door.setupInteractions=2}),
 @('closed door claims no setup close',{param($v)$v.observations.door.setupInteractions=0}),
 @('closed door without its active cut',{param($v)$v.observations.door.fixture.originalCut=$false;$v.observations.door.restoration.cutEnabled=$false}),
 @('closed door restored open',{param($v)$v.observations.door.restoration.open=$true}),
 @('closed door restored without its cut',{param($v)$v.observations.door.restoration.cutEnabled=$false}),
 @('door open state as text',{param($v)$v.observations.door.fixture.originalOpen='false'}),
 @('closed door in the legacy fixture shape',{param($v)foreach($k in @('interactable','availableInteractions','revealed','hasVisibleTrap','trapActive','originalAlreadyUnlocked','restrictionCount','hasDialog')){$v.observations.door.fixture.PSObject.Properties.Remove($k)};$v.observations.door.restoration.PSObject.Properties.Remove('alreadyUnlocked');$v.observations.door.restoration.PSObject.Properties.Remove('observedAlreadyUnlockedBeforeRestore')}),
 @('closed door not interactable',{param($v)$v.observations.door.fixture.interactable=$false}),
 @('closed door without available interactions',{param($v)$v.observations.door.fixture.availableInteractions=$false}),
 @('closed unrevealed secret door',{param($v)$v.observations.door.fixture.revealed=$false}),
 @('closed door with a visible trap',{param($v)$v.observations.door.fixture.hasVisibleTrap=$true}),
 @('closed door with an active trap',{param($v)$v.observations.door.fixture.trapActive=$true}),
 @('closed locked door behind a restriction',{param($v)$v.observations.door.fixture.restrictionCount=1}),
 @('closed door unlocked behind a restriction',{param($v)$f=$v.observations.door.fixture;$f.restrictionCount=1;$f.originalAlreadyUnlocked=$true;$v.observations.door.restoration.alreadyUnlocked=$true}),
 @('closed door with door-view trigger actions',{param($v)$v.observations.door.fixture.viewTriggerCount=1;$v.observations.door.fixture.viewTriggerActions=$true}),
 @('closed door without persistent data',{param($v)$v.observations.door.fixture.dataPresent=$false}),
 @('closed door start not natively enabled',{param($v)$v.observations.door.fixture.originalEnabled=$false;$v.observations.door.restoration.enabled=$false}),
 @('closed door without trigger flags',{param($v)$v.observations.door.fixture.PSObject.Properties.Remove('viewTriggerActions')}),
 @('closed door trigger count as text',{param($v)$v.observations.door.fixture.viewTriggerCount='0'}),
 @('closed dialog door',{param($v)$v.observations.door.fixture.hasDialog=$true}),
 @('closed door flag as text',{param($v)$v.observations.door.fixture.revealed='true'}),
 @('closed door restoration lost the captured unlock flag',{param($v)$v.observations.door.restoration.alreadyUnlocked=$true}),
 @('closed door restoration without the unlock flag',{param($v)$v.observations.door.restoration.PSObject.Properties.Remove('alreadyUnlocked')})
)
foreach($mutation in $closedDoorMutations){$copy=Copy-TestValue $good;Set-ClosedDoorStart $copy;& $mutation[1] $copy;Reject $mutation[0] {Assert-KmcChunk7WorldEvidence $request $copy 'PASS'}}
# A door whose cut the door never writes (DisableNavmeshCutWhenOpen=false): the cut keeps its
# captured state for the whole lease and at restoration.
function Set-StaticDoorCut($v,[bool]$Cut=$true){$d=$v.observations.door;$d.fixture.cutMode='static';$d.fixture.originalCut=$Cut;$d.restoration.cutEnabled=$Cut;$d.cutObservations=@(New-DoorCutObservations $d.fixture.originalOpen $false $Cut)}
Check 'static-cut open door with its cut unchanged' {$copy=Copy-TestValue $good;Set-CurrentDoorShape $copy;Set-StaticDoorCut $copy;Assert-KmcChunk7WorldEvidence $request $copy 'PASS'}
Check 'static-cut open door whose cut is disabled throughout' {$copy=Copy-TestValue $good;Set-CurrentDoorShape $copy;Set-StaticDoorCut $copy $false;Assert-KmcChunk7WorldEvidence $request $copy 'PASS'}
Check 'static-cut closed door with its cut unchanged' {$copy=Copy-TestValue $good;Set-ClosedDoorStart $copy;Set-StaticDoorCut $copy;Assert-KmcChunk7WorldEvidence $request $copy 'PASS'}
$staticCutMutations=@(
 @('static cut toggled during the lease',{param($v)$v.observations.door.cutObservations[2].cutEnabled=$false}),
 @('static cut not restored',{param($v)$v.observations.door.restoration.cutEnabled=$false;$v.observations.door.cutObservations[5].cutEnabled=$false}),
 @('static restoration record without its cut',{param($v)$v.observations.door.restoration.cutEnabled=$false}),
 @('cut mode missing',{param($v)$v.observations.door.fixture.PSObject.Properties.Remove('cutMode')}),
 @('toggling mode claimed for a static door',{param($v)$v.observations.door.fixture.cutMode='toggles'}),
 @('unknown cut mode',{param($v)$v.observations.door.fixture.cutMode='none'}),
 @('cut observations missing',{param($v)$v.observations.door.PSObject.Properties.Remove('cutObservations')}),
 @('cut observation boundary missing',{param($v)$v.observations.door.cutObservations=@($v.observations.door.cutObservations|Select-Object -First 5)}),
 @('cut observations out of order',{param($v)$o=$v.observations.door.cutObservations;$v.observations.door.cutObservations=@($o[0],$o[2],$o[1],$o[3],$o[4],$o[5])}),
 @('cut observation open state wrong',{param($v)$v.observations.door.cutObservations[1].open=$true}),
 @('cut observation state as text',{param($v)$v.observations.door.cutObservations[0].cutEnabled='true'})
)
foreach($mutation in $staticCutMutations){$copy=Copy-TestValue $good;Set-CurrentDoorShape $copy;Set-StaticDoorCut $copy;& $mutation[1] $copy;Reject $mutation[0] {Assert-KmcChunk7WorldEvidence $request $copy 'PASS'}}
Reject 'toggling door whose cut stayed static' {$copy=Copy-TestValue $good;foreach($o in $copy.observations.door.cutObservations){$o.cutEnabled=$false};Assert-KmcChunk7WorldEvidence $request $copy 'PASS'}
Reject 'static mode claimed for a toggling door' {$copy=Copy-TestValue $good;$copy.observations.door.fixture.cutMode='static';Assert-KmcChunk7WorldEvidence $request $copy 'PASS'}
# v2 door return (F4 r30): the first pair returns to the first ring candidate clear of every other unit's native
# avoidance footprint (corpulence 0.5 + 0.5 out of combat, plus the 0.5 m margin); v1 evidence (the $good fixture)
# keeps its original return path. Candidates are built independently of the reader: all walkable, clamp exact.
function New-DoorReturnV2Fixture($HorseX,$HorseZ){
    $copy=Copy-TestValue $good;$d=$copy.observations.door;$d.contract='two-pair-native-door-click-selection-change-v2'
    $units=@([pscustomobject]@{id=$horse;position=[pscustomobject]@{x=$HorseX;y=0.0;z=$HorseZ};corpulence=0.5},[pscustomobject]@{id=$second;position=[pscustomobject]@{x=$HorseX;y=0.0;z=$HorseZ};corpulence=0.5})
    $rings=@(0.5,1.0,1.5,2.0,2.5,3.0,3.5,4.0,5.0);$candidates=@();$selected=-1
    for($i=0;$i-lt145-and$selected-lt0;$i++){
        if($i-eq0){$ring=0.0;$bearing=0.0}else{$ring=$rings[[int][Math]::Floor(($i-1)/16)];$bearing=22.5*(($i-1)%16)}
        $x=[Math]::Sin($bearing*[Math]::PI/180)*$ring;$z=[Math]::Cos($bearing*[Math]::PI/180)*$ring
        $slack=[Math]::Sqrt(($x-$HorseX)*($x-$HorseX)+($z-$HorseZ)*($z-$HorseZ))-1.5
        $doorDistance=[Math]::Sqrt((10-$x)*(10-$x)+$z*$z)
        $reason=if($slack-lt0){'inside-unit-footprint'}elseif($doorDistance-le2.5){'inside-door-approach'}else{'admitted'}
        $p=[pscustomobject]@{x=$x;y=0.0;z=$z}
        $candidates+=[pscustomobject]@{index=$i;ring=$ring;bearing=$bearing;requested=$p;walkable=$true;reason=$reason;clamped=(Copy-TestValue $p);clampResidual=0.0;traceResidual=0.0;doorDistance=$doorDistance;minimumSlack=$slack;nearestBlockerId=$horse}
        if($reason-ceq'admitted'){$selected=$i}
    }
    $goal=$candidates[$selected].clamped;$end=[pscustomobject]@{x=$goal.x+0.1;y=0.0;z=$goal.z}
    $d.firstPairReturn=[pscustomobject]@{command=1802;executorId=$mammoth;terminal=$true;result='Success';goal=(Copy-TestValue $goal);commandTarget=(Copy-TestValue $goal);position=$end;
        distanceToGoal=0.1;distanceToStart=[Math]::Sqrt($end.x*$end.x+$end.z*$end.z);
        goalSearch=[pscustomobject]@{contract='door-return-goal-clear-of-units/v1';start=[pscustomobject]@{x=0.0;y=0.0;z=0.0};moverId=$mammoth;moverCorpulence=0.5;moverPosition=[pscustomobject]@{x=8.0;y=0.0;z=0.0};
            margin=0.5;doorPosition=[pscustomobject]@{x=10.0;y=0.0;z=0.0};doorRadius=2.0;scanRadius=28;unitRadius=11;units=$units;candidates=$candidates;selectedIndex=$selected}}
    return $copy
}
$returnF3=New-DoorReturnV2Fixture 0.0 3.10;$returnF4=New-DoorReturnV2Fixture (-0.36) 0.78
Check 'v2 door return keeps a captured start 3.10 m from pair B (F3 geometry)' {if($returnF3.observations.door.firstPairReturn.goalSearch.selectedIndex-ne0){throw 'F3 start was not kept.'};Assert-KmcChunk7WorldEvidence $request (Copy-TestValue $returnF3) 'PASS'}
Check 'v2 door return searches clear of pair B 0.86 m from the start (F4 geometry)' {$s=$returnF4.observations.door.firstPairReturn.goalSearch;if($s.selectedIndex-le0-or$s.candidates[0].reason-cne'inside-unit-footprint'){throw 'F4 start was not refused.'};Assert-KmcChunk7WorldEvidence $request (Copy-TestValue $returnF4) 'PASS'}
Check 'v1 door return keeps its original path' {if($good.observations.door.contract-cne'two-pair-native-door-click-selection-change'){throw 'v1 fixture changed.'};Assert-KmcChunk7WorldEvidence $request (Copy-TestValue $good) 'PASS'}
$returnMutations=@(
 @('v2 contract with the v1 return shape',{param($v)$v.observations.door.firstPairReturn=(Copy-TestValue $good.observations.door.firstPairReturn)}),
 @('v1 contract carrying the v2 return shape',{param($v)$v.observations.door.contract='two-pair-native-door-click-selection-change'}),
 @('v2 return without its goal search',{param($v)$v.observations.door.firstPairReturn.PSObject.Properties.Remove('goalSearch')}),
 @('F4 start claimed clear of pair B',{param($v)$r=$v.observations.door.firstPairReturn;$s=$r.goalSearch;$c=$s.candidates[0];$c.reason='admitted';$s.candidates=@($c);$s.selectedIndex=0;$r.goal=(Copy-TestValue $c.clamped);$r.commandTarget=(Copy-TestValue $c.clamped);$r.position=[pscustomobject]@{x=0.1;y=0.0;z=0.0};$r.distanceToGoal=0.1;$r.distanceToStart=0.1}),
 @('F4 start clearance misrecorded as clear',{param($v)$v.observations.door.firstPairReturn.goalSearch.candidates[0].minimumSlack=0.2}),
 @('other pair Horse omitted from the search',{param($v)$s=$v.observations.door.firstPairReturn.goalSearch;$s.units=@($s.units|Where-Object id -CNE $horse);foreach($c in $s.candidates){$c.nearestBlockerId=$second}}),
 @('returning mount counted as an obstacle',{param($v)$v.observations.door.firstPairReturn.goalSearch.units+=[pscustomobject]@{id=$mammoth;position=[pscustomobject]@{x=50.0;y=0.0;z=0.0};corpulence=0.5}}),
 @('search mover is not the first pair mount',{param($v)$v.observations.door.firstPairReturn.goalSearch.moverId=$horse}),
 @('candidate reason differs from its values',{param($v)$v.observations.door.firstPairReturn.goalSearch.candidates[1].reason='off-navmesh'}),
 @('candidate bearing out of policy order',{param($v)$v.observations.door.firstPairReturn.goalSearch.candidates[1].bearing=45.0}),
 @('candidate requested point off its ring',{param($v)$v.observations.door.firstPairReturn.goalSearch.candidates[1].requested.x=0.3}),
 @('candidate clamp residual misrecorded',{param($v)$v.observations.door.firstPairReturn.goalSearch.candidates[1].clampResidual=0.05}),
 @('candidate navmesh trace beyond its bound',{param($v)$v.observations.door.firstPairReturn.goalSearch.candidates[-1].traceResidual=0.06}),
 @('candidate door distance misrecorded',{param($v)$v.observations.door.firstPairReturn.goalSearch.candidates[-1].doorDistance=20.0}),
 @('unwalkable candidate with native clamp fields',{param($v)$v.observations.door.firstPairReturn.goalSearch.candidates[0].walkable=$false;$v.observations.door.firstPairReturn.goalSearch.candidates[0].reason='no-walkable-native-node'}),
 @('search continued past its admitted goal',{param($v)$s=$v.observations.door.firstPairReturn.goalSearch;$s.candidates+=(Copy-TestValue $s.candidates[-1])}),
 @('goal is not the admitted candidate',{param($v)$v.observations.door.firstPairReturn.goal.x+=0.5}),
 @('native command aimed elsewhere',{param($v)$v.observations.door.firstPairReturn.commandTarget.x+=0.5}),
 @('return ended beyond 1.25 m of its goal',{param($v)$r=$v.observations.door.firstPairReturn;$r.position.x=$r.goal.x+1.3;$r.distanceToGoal=1.3;$r.distanceToStart=[Math]::Sqrt($r.position.x*$r.position.x+$r.position.z*$r.position.z)}),
 @('return distance does not recompute',{param($v)$v.observations.door.firstPairReturn.distanceToGoal=0.0}),
 @('return interrupted natively',{param($v)$v.observations.door.firstPairReturn.result='Interrupted'}),
 @('return not terminal',{param($v)$v.observations.door.firstPairReturn.terminal=$false}),
 @('search margin weakened',{param($v)$v.observations.door.firstPairReturn.goalSearch.margin=0.0}),
 @('search door radius differs from the fixture',{param($v)$v.observations.door.firstPairReturn.goalSearch.doorRadius=1.0}),
 @('unknown door contract',{param($v)$v.observations.door.contract='two-pair-native-door-click-selection-change-v3'})
)
foreach($mutation in $returnMutations){$copy=Copy-TestValue $returnF4;& $mutation[1] $copy;Reject $mutation[0] {Assert-KmcChunk7WorldEvidence $request $copy 'PASS'}}
# v3 door (Sprint 2, DOOR-01/SPACE-01): the long approach, click-while-moving and occupancy records on top of the v2 return.
function New-TestPoint([double]$X,[double]$Y,[double]$Z){return [pscustomobject]@{x=$X;y=$Y;z=$Z}}
function New-TestGoalSearch($Start,$Direction,[double]$Distance,$Clamped,[double]$DoorDistance){
    return [pscustomobject]@{start=$Start;direction=$Direction;distance=$Distance;selectedIndex=0;candidates=@([pscustomobject]@{index=0;bearingOffset=0.0;requested=(Copy-TestValue $Clamped);walkable=$true;clamped=$Clamped;clampResidual=0.0;traceResidual=0.0;doorDistance=$DoorDistance;minimumSlack=5.0;reason='admitted'})}
}
function New-DoorV3Fixture(){
    $copy=Copy-TestValue $returnF3;$d=$copy.observations.door;$d.contract='two-pair-native-door-click-selection-change-v3'
    $d|Add-Member approach ([pscustomobject]@{approachPoint=(New-TestPoint 10 0 0);nativeTargetNode=(New-TestPoint 9.5 0 0);losObjectId=-4242;mapObjectIsDoorObject=$true})
    $d|Add-Member longApproach ([pscustomobject]@{applicable=$true;goalSearch=(New-TestGoalSearch (New-TestPoint 0 0 0) (New-TestPoint -1 0 0) 12.0 (New-TestPoint -12 0 0) 22.0);goal=(New-TestPoint -12 0 0);startDistanceToDoor=8.0;setupCommand=1810;setupResult='Success';setupEnd=(New-TestPoint -12 0 0);distanceToDoorAfterSetup=22.0})
    $d.interactions[0].distanceBefore=22.0;$d.interactions[0].distanceMoved=19.0
    $d|Add-Member clickWhileMoving ([pscustomobject]@{applicable=$true;goalSearch=(New-TestGoalSearch (New-TestPoint 8 0 0) (New-TestPoint 0 0 1) 6.0 (New-TestPoint 8 0 6) 6.3);goal=(New-TestPoint 8 0 6);moveCommand=1811;mountPositionAtClick=(New-TestPoint 8 0 1);moveRunningAtClick=$true;moveFinishedAtAdmission=$true;moveResult='Interrupt';replacedMoveCount=1})
    for($i=0;$i-lt2;$i++){$v=$d.interactions[$i];$v|Add-Member clickWhileMoving ($i-eq1)
        $v|Add-Member occupancy ([pscustomobject]@{mountPosition=(New-TestPoint 8.2 0 0);riderPosition=(New-TestPoint 8.2 1.2 0);mountInsideNavmesh=$true;riderInsideNavmesh=$false;mountDistanceToApproachPoint=1.8;mountDistanceToDoor=1.8;doorOpen=$true;cutEnabled=$false;mountCanMove=$true;mountCommandsEmpty=$true})
        $o=$v.outcome;$o|Add-Member ApproachesWithoutProgress 0;$o|Add-Member ApproachElapsedGameSeconds 3.5;$o|Add-Member DistanceAtInteraction 1.8;$o|Add-Member LineOfSightAtInteraction $true
        $o|Add-Member OccupancyInsideNavmesh $true;$o|Add-Member OccupancyRecovery 'none';$o|Add-Member OccupancyNearestNodeDistance 0.0}
    $copy.observations.factRemovalAdmission.commands+=@((1810..1811)|ForEach-Object{[pscustomobject]@{commandObject=$_;finished=$true;hasExecutionProcess=$false;processEnded=$true}})
    return $copy
}
$doorV3=New-DoorV3Fixture
Check 'v3 door with long approach, click while moving and navmesh occupancy' {Assert-KmcChunk7WorldEvidence $request (Copy-TestValue $doorV3) 'PASS'}
Check 'v3 door with the long approach and click-while-moving recorded as not applicable' {$copy=Copy-TestValue $doorV3;$d=$copy.observations.door
    $d.longApproach=[pscustomobject]@{applicable=$false;goalSearch=$d.longApproach.goalSearch;reason='no clear native goal away from the door'};$d.interactions[0].distanceBefore=8.0;$d.interactions[0].distanceMoved=6.0
    $d.clickWhileMoving=[pscustomobject]@{applicable=$false;goalSearch=$d.clickWhileMoving.goalSearch;reason='no clear lateral native goal'};$d.interactions[1].clickWhileMoving=$false
    $copy.observations.factRemovalAdmission.commands=@($copy.observations.factRemovalAdmission.commands|Where-Object{$_.commandObject-notin@(1810,1811)});Assert-KmcChunk7WorldEvidence $request $copy 'PASS'}
Check 'v3 door retrying one finished approach natively' {$copy=Copy-TestValue $doorV3;$copy.observations.door.interactions[0].outcome.DelegatedMoveStartCount=2;$copy.observations.door.interactions[0].outcome.ApproachesWithoutProgress=1;Assert-KmcChunk7WorldEvidence $request $copy 'PASS'}
$v3Mutations=@(
 @('v3 contract in the v2 shape',{param($v)foreach($k in @('approach','longApproach','clickWhileMoving')){$v.observations.door.PSObject.Properties.Remove($k)}}),
 @('v3 without its approach record',{param($v)$v.observations.door.PSObject.Properties.Remove('approach')}),
 @('v3 approach without the native LOS object',{param($v)$v.observations.door.approach.losObjectId=0}),
 @('v3 interaction in the v2 row shape',{param($v)$v.observations.door.interactions[0].PSObject.Properties.Remove('occupancy')}),
 @('v3 outcome in the v2 shape',{param($v)$v.observations.door.interactions[0].outcome.PSObject.Properties.Remove('OccupancyRecovery')}),
 @('v3 mount off the navmesh after the door opened',{param($v)$v.observations.door.interactions[0].occupancy.mountInsideNavmesh=$false}),
 @('v3 outcome left the navmesh',{param($v)$v.observations.door.interactions[1].outcome.OccupancyInsideNavmesh=$false}),
 @('v3 outcome needed an occupancy repath',{param($v)$v.observations.door.interactions[1].outcome.OccupancyRecovery='repath'}),
 @('v3 interaction outside the proximity radius',{param($v)$v.observations.door.interactions[0].outcome.DistanceAtInteraction=2.5}),
 @('v3 interaction without line of sight',{param($v)$v.observations.door.interactions[0].outcome.LineOfSightAtInteraction=$false}),
 @('v3 approach exhausted without progress',{param($v)$v.observations.door.interactions[0].outcome.ApproachesWithoutProgress=3}),
 @('v3 approach restarted beyond the bound',{param($v)$v.observations.door.interactions[0].outcome.DelegatedMoveStartCount=5}),
 @('v3 occupancy outside the interaction radius',{param($v)$v.observations.door.interactions[0].occupancy.mountDistanceToApproachPoint=2.6}),
 @('v3 long approach not farther from the door',{param($v)$v.observations.door.longApproach.distanceToDoorAfterSetup=5.0;$v.observations.door.interactions[0].distanceBefore=5.0}),
 @('v3 long approach disagreeing with the first click',{param($v)$v.observations.door.interactions[0].distanceBefore=10.0}),
 @('v3 long approach setup move failed',{param($v)$v.observations.door.longApproach.setupResult='Interrupt'}),
 @('v3 click while moving not replacing the move',{param($v)$v.observations.door.clickWhileMoving.moveFinishedAtAdmission=$false}),
 @('v3 click while moving with the move still owned',{param($v)$v.observations.door.clickWhileMoving.moveResult='Success'}),
 @('v3 click while moving without the controller replacement',{param($v)$v.observations.door.clickWhileMoving.replacedMoveCount=0}),
 @('v3 click flag disagreeing with its record',{param($v)$v.observations.door.interactions[1].clickWhileMoving=$false}),
 @('v3 goal search continuing past an admitted candidate',{param($v)$s=$v.observations.door.longApproach.goalSearch;$s.candidates=@($s.candidates[0],(Copy-TestValue $s.candidates[0]));$s.candidates[1].index=1;$s.selectedIndex=1}),
 @('v3 setup move excluded from the fact-removal fence',{param($v)$v.observations.factRemovalAdmission.commands=@($v.observations.factRemovalAdmission.commands|Where-Object commandObject -NE 1810)}),
 @('v3 lateral move excluded from the fact-removal fence',{param($v)$v.observations.factRemovalAdmission.commands=@($v.observations.factRemovalAdmission.commands|Where-Object commandObject -NE 1811)})
)
foreach($mutation in $v3Mutations){$copy=Copy-TestValue $doorV3;& $mutation[1] $copy;Reject $mutation[0] {Assert-KmcChunk7WorldEvidence $request $copy 'PASS'}}
# v4 door (Sprint 2 completion): unmounted control, endpoint neighbor, Stop control, unstarted-while-moving samples and the
# owned settlement (settled cut, body footprint, recovery) on top of v3. Built independently of the reader.
function New-TestOccupancy([double]$Distance){[pscustomobject]@{mountPosition=(New-TestPoint 8.2 0 0);riderPosition=(New-TestPoint 8.2 1.2 0);mountInsideNavmesh=$true;riderInsideNavmesh=$false;mountDistanceToApproachPoint=$Distance;mountDistanceToDoor=$Distance;doorOpen=$true;cutEnabled=$false;mountCanMove=$true;mountCommandsEmpty=$true}}
function New-DoorV4Fixture(){
    $copy=Copy-TestValue $doorV3;$d=$copy.observations.door;$d.contract='two-pair-native-door-click-selection-change-v4'
    $d.longApproach|Add-Member requestedDistances @(26.0,20.0)
    $d.setupInteractions=$(if($d.fixture.originalOpen){2}else{1})+2
    $d|Add-Member unmountedControl ([pscustomobject]@{applicable=$true;actorId='8265f8cf-0969-4ec7-a266-2448de91c8c4';accepted=$true;commandType='Kingmaker.UnitLogic.Commands.UnitInteractWithObject';commandObject=1830;distanceBefore=9.0;routedByKmc=$false;result='Success';doorOpenAfter=$true;distanceAfter=1.5;actorInsideNavmesh=$true})
    $d|Add-Member endpointNeighbor ([pscustomobject]@{applicable=$true;goalSearch=(New-TestGoalSearch (New-TestPoint 20 0 0) (New-TestPoint -1 0 0) 7.0 (New-TestPoint 13 0 0) 3.3);goal=(New-TestPoint 13 0 0);moveCommand=1831;moveResult='Success';positionAtFirstClick=(New-TestPoint 13 0 0);distanceToApproachPointAtFirstClick=3.3;occupancyAfterFirstInteraction=(New-TestOccupancy 3.3)})
    $d|Add-Member stopControl ([pscustomobject]@{applicable=$true;commandObject=1832;distanceBefore=14.0;mountPositionAtStop=(New-TestPoint -5 0 0);commandStartedAtStop=$false;distanceMovedAtStop=2.5;stopFrame=5000;commandResult='Interrupt';doorOpenAfter=$false;interactionCount=0;outcomeResult='Interrupt';mountMoveSlotRestored=$true;mountCommandsEmpty=$true;riderCommandsEmpty=$true;pendingSettlement=$false;framesToSettle=4;mountPositionAfter=(New-TestPoint -5 0 0)})
    for($i=0;$i-lt2;$i++){$v=$d.interactions[$i];$v|Add-Member movingPolls 40;$v|Add-Member startedWhileMoving $false;$v|Add-Member unstartedWhileMoving $true
        $o=$v.outcome;$o|Add-Member PreventionRefused $false;$o|Add-Member OccupancySettled $true;$o|Add-Member OccupancySettleGameSeconds 0.05;$o|Add-Member MountCorpulence 0.9
        $o|Add-Member FootprintClear $true;$o|Add-Member FootprintRingInside 8;$o|Add-Member FootprintRingReachable 8;$o|Add-Member RecoveryDisposition 'NotNeeded';$o|Add-Member RecoveryTerminal 'None'
        $o|Add-Member RecoveryCandidatesEvaluated 0;$o|Add-Member RecoveryCandidateReasons '';$o|Add-Member RecoveryDestinationDistance ([double]::NaN);$o|Add-Member FootprintClearAfterRecovery $null;$o|Add-Member RecoveryCancelReason $null}
    $copy.observations.factRemovalAdmission.commands+=@((1830..1832)|ForEach-Object{[pscustomobject]@{commandObject=$_;finished=$true;hasExecutionProcess=$false;processEnded=$true}})
    return $copy
}
$doorV4=New-DoorV4Fixture
# v4 layout restore (r08 correction): optional block; each pair's ordinary move back toward its pre-setup start.
function New-DoorV4LayoutFixture(){
    $copy=Copy-TestValue $doorV4;$d=$copy.observations.door
    $moveA=[pscustomobject]@{pair=0;preSetupStart=(New-TestPoint 0 0 0);positionBefore=(New-TestPoint -12 0 0);distanceToStartBefore=12.0;goalSearch=(New-TestGoalSearch (New-TestPoint -12 0 0) (New-TestPoint 1 0 0) 12.0 (New-TestPoint -0.5 0 0) 10.5);goal=(New-TestPoint -0.5 0 0);command=1840;applicable=$true;positionAfter=(New-TestPoint -0.5 0 0);distanceToStartAfter=0.5;result='Success'}
    $moveB=[pscustomobject]@{pair=1;preSetupStart=(New-TestPoint 8 0 0);positionBefore=(New-TestPoint 8.4 0 0);distanceToStartBefore=0.4;applicable=$false;reason='already within 1.5 m of the pre-setup start';positionAfter=(New-TestPoint 8.4 0 0);distanceToStartAfter=0.4}
    $d|Add-Member layoutRestore ([pscustomobject]@{applicable=$true;moves=@($moveA,$moveB)})
    $copy.observations.factRemovalAdmission.commands+=@([pscustomobject]@{commandObject=1840;finished=$true;hasExecutionProcess=$false;processEnded=$true})
    return $copy
}
$doorV4Layout=New-DoorV4LayoutFixture
Check 'v4 door with the layout restored toward the pre-setup starts' {Assert-KmcChunk7WorldEvidence $request (Copy-TestValue $doorV4Layout) 'PASS'}
$layoutMutations=@(
 @('v4 layout restore move failed',{param($v)$v.observations.door.layoutRestore.moves[0].result='Interrupt'}),
 @('v4 layout restore ending farther from the start',{param($v)$v.observations.door.layoutRestore.moves[0].distanceToStartAfter=13.0}),
 @('v4 layout restore applicability disagreeing with its moves',{param($v)$v.observations.door.layoutRestore.applicable=$false}),
 @('v4 layout restore with one move only',{param($v)$v.observations.door.layoutRestore.moves=@($v.observations.door.layoutRestore.moves[0])}),
 @('v4 layout restore move excluded from the fact-removal fence',{param($v)$v.observations.factRemovalAdmission.commands=@($v.observations.factRemovalAdmission.commands|Where-Object commandObject -NE 1840)})
)
foreach($mutation in $layoutMutations){$copy=Copy-TestValue $doorV4Layout;& $mutation[1] $copy;Reject $mutation[0] {Assert-KmcChunk7WorldEvidence $request $copy 'PASS'}}
Check 'v4 door with unmounted control, endpoint neighbor, Stop control and clear settled footprints' {Assert-KmcChunk7WorldEvidence $request (Copy-TestValue $doorV4) 'PASS'}
Check 'v4 door with one completed owned recovery that left the footprint clear' {$copy=Copy-TestValue $doorV4;$o=$copy.observations.door.interactions[1].outcome
    $o.FootprintClear=$false;$o.FootprintRingInside=7;$o.FootprintRingReachable=7;$o.RecoveryDisposition='Admitted';$o.RecoveryTerminal='Completed';$o.OccupancyRecovery='repath'
    $o.RecoveryCandidatesEvaluated=3;$o.RecoveryCandidateReasons='admitted=1,footprint-blocked=2';$o.RecoveryDestinationDistance=0.5;$o.FootprintClearAfterRecovery=$true
    # The occupancy is recorded after the settlement: the recovered mount stands its recovery distance farther from the interaction point (r05 shape).
    $copy.observations.door.interactions[1].occupancy.mountDistanceToApproachPoint=2.46;$copy.observations.door.interactions[1].occupancy.mountDistanceToDoor=2.46;Assert-KmcChunk7WorldEvidence $request $copy 'PASS'}
Reject 'v4 recovered pair standing beyond its recovery distance from the interaction point' {$copy=Copy-TestValue $doorV4;$o=$copy.observations.door.interactions[1].outcome
    $o.FootprintClear=$false;$o.FootprintRingInside=7;$o.FootprintRingReachable=7;$o.RecoveryDisposition='Admitted';$o.RecoveryTerminal='Completed';$o.OccupancyRecovery='repath'
    $o.RecoveryCandidatesEvaluated=1;$o.RecoveryCandidateReasons='admitted=1';$o.RecoveryDestinationDistance=0.5;$o.FootprintClearAfterRecovery=$true
    $copy.observations.door.interactions[1].occupancy.mountDistanceToApproachPoint=2.6;Assert-KmcChunk7WorldEvidence $request $copy 'PASS'}
Reject 'v3 occupancy keeps the exact interaction-radius bound' {$copy=Copy-TestValue $doorV3;$copy.observations.door.interactions[1].occupancy.mountDistanceToApproachPoint=2.3;Assert-KmcChunk7WorldEvidence $request $copy 'PASS'}
Check 'v4 door with the controls recorded as not applicable' {$copy=Copy-TestValue $doorV4;$d=$copy.observations.door
    $d.unmountedControl=[pscustomobject]@{applicable=$false;reason='no idle unmounted controllable party member within the scan radius'};$d.setupInteractions=$(if($d.fixture.originalOpen){2}else{1})+1
    $d.endpointNeighbor=[pscustomobject]@{applicable=$false;goalSearch=[pscustomobject]@{skipped='second pair already within the neighbor distance'};reason='no clear native goal near the door approach point for the second pair'}
    $d.stopControl=[pscustomobject]@{applicable=$false;reason='the first pair stands inside the door''s immediate range'}
    $copy.observations.factRemovalAdmission.commands=@($copy.observations.factRemovalAdmission.commands|Where-Object{$_.commandObject-notin@(1830,1831,1832)});Assert-KmcChunk7WorldEvidence $request $copy 'PASS'}
$v4Mutations=@(
 @('v4 contract in the v3 shape',{param($v)foreach($k in @('unmountedControl','endpointNeighbor','stopControl')){$v.observations.door.PSObject.Properties.Remove($k)}}),
 @('v4 interaction in the v3 row shape',{param($v)$v.observations.door.interactions[0].PSObject.Properties.Remove('unstartedWhileMoving')}),
 @('v4 outcome in the v3 shape',{param($v)$v.observations.door.interactions[0].outcome.PSObject.Properties.Remove('FootprintClear')}),
 @('v4 command started while the mount moved',{param($v)$v.observations.door.interactions[0].startedWhileMoving=$true;$v.observations.door.interactions[0].unstartedWhileMoving=$false}),
 @('v4 mount never observed moving',{param($v)$v.observations.door.interactions[1].movingPolls=0}),
 @('v4 prevention refused the interaction',{param($v)$v.observations.door.interactions[0].outcome.PreventionRefused=$true}),
 @('v4 settlement never settled',{param($v)$v.observations.door.interactions[0].outcome.OccupancySettled=$false}),
 @('v4 settlement exceeded its bound',{param($v)$v.observations.door.interactions[0].outcome.OccupancySettleGameSeconds=4.0}),
 @('v4 footprint not clear without recovery',{param($v)$v.observations.door.interactions[0].outcome.FootprintClear=$false;$v.observations.door.interactions[0].outcome.FootprintRingReachable=7}),
 @('v4 clear footprint reporting a recovery',{param($v)$v.observations.door.interactions[0].outcome.OccupancyRecovery='repath'}),
 @('v4 footprint unrecoverable',{param($v)$o=$v.observations.door.interactions[0].outcome;$o.FootprintClear=$false;$o.RecoveryDisposition='Unrecoverable';$o.OccupancyRecovery='unrecoverable'}),
 @('v4 recovery interrupted',{param($v)$o=$v.observations.door.interactions[1].outcome;$o.FootprintClear=$false;$o.RecoveryDisposition='Admitted';$o.RecoveryTerminal='Interrupted';$o.OccupancyRecovery='repath';$o.RecoveryCandidatesEvaluated=1;$o.RecoveryCandidateReasons='admitted=1';$o.RecoveryDestinationDistance=0.5;$o.FootprintClearAfterRecovery=$false}),
 @('v4 recovery replaced by another order',{param($v)$o=$v.observations.door.interactions[1].outcome;$o.FootprintClear=$false;$o.RecoveryDisposition='Admitted';$o.RecoveryTerminal='Replaced';$o.OccupancyRecovery='repath';$o.RecoveryCandidatesEvaluated=1;$o.RecoveryCandidateReasons='admitted=1';$o.RecoveryDestinationDistance=0.5;$o.FootprintClearAfterRecovery=$true}),
 @('v4 recovery destination beyond the recovery radius',{param($v)$o=$v.observations.door.interactions[1].outcome;$o.FootprintClear=$false;$o.RecoveryDisposition='Admitted';$o.RecoveryTerminal='Completed';$o.OccupancyRecovery='repath';$o.RecoveryCandidatesEvaluated=1;$o.RecoveryCandidateReasons='admitted=1';$o.RecoveryDestinationDistance=2.6;$o.FootprintClearAfterRecovery=$true}),
 @('v4 recovery leaving the footprint blocked',{param($v)$o=$v.observations.door.interactions[1].outcome;$o.FootprintClear=$false;$o.RecoveryDisposition='Admitted';$o.RecoveryTerminal='Completed';$o.OccupancyRecovery='repath';$o.RecoveryCandidatesEvaluated=1;$o.RecoveryCandidateReasons='admitted=1';$o.RecoveryDestinationDistance=0.5;$o.FootprintClearAfterRecovery=$false}),
 @('v4 settlement cancelled',{param($v)$v.observations.door.interactions[0].outcome.RecoveryCancelReason='ground command'}),
 @('v4 unmounted control routed by KMC',{param($v)$v.observations.door.unmountedControl.routedByKmc=$true}),
 @('v4 unmounted control not the native interaction',{param($v)$v.observations.door.unmountedControl.commandType='KingmakerMountedCombat.Integration.MountedDoorInteractionCommand'}),
 @('v4 unmounted control by a declared rider',{param($v)$v.observations.door.unmountedControl.actorId=$first}),
 @('v4 unmounted control that did not open the door',{param($v)$v.observations.door.unmountedControl.doorOpenAfter=$false}),
 @('v4 setup closes not accounting for the unmounted control',{param($v)$v.observations.door.setupInteractions=$v.observations.door.setupInteractions-1}),
 @('v4 endpoint neighbor inside the door approach',{param($v)$v.observations.door.endpointNeighbor.distanceToApproachPointAtFirstClick=2.3}),
 @('v4 endpoint neighbor far from the door',{param($v)$v.observations.door.endpointNeighbor.distanceToApproachPointAtFirstClick=6.0}),
 @('v4 endpoint neighbor off the navmesh after the first interaction',{param($v)$v.observations.door.endpointNeighbor.occupancyAfterFirstInteraction.mountInsideNavmesh=$false}),
 @('v4 endpoint neighbor move failed',{param($v)$v.observations.door.endpointNeighbor.moveResult='Interrupt'}),
 @('v4 Stop control command started at the stop',{param($v)$v.observations.door.stopControl.commandStartedAtStop=$true}),
 @('v4 Stop control that interacted',{param($v)$v.observations.door.stopControl.interactionCount=1}),
 @('v4 Stop control leaving a pending settlement',{param($v)$v.observations.door.stopControl.pendingSettlement=$true}),
 @('v4 Stop control leaving a mount command',{param($v)$v.observations.door.stopControl.mountCommandsEmpty=$false}),
 @('v4 Stop control ending as success',{param($v)$v.observations.door.stopControl.commandResult='Success'}),
 @('v4 long approach requesting beyond the click bound',{param($v)$v.observations.door.longApproach.requestedDistances=@(40.0)}),
 @('v4 control command excluded from the fact-removal fence',{param($v)$v.observations.factRemovalAdmission.commands=@($v.observations.factRemovalAdmission.commands|Where-Object commandObject -NE 1830)}),
 @('v4 neighbor move excluded from the fact-removal fence',{param($v)$v.observations.factRemovalAdmission.commands=@($v.observations.factRemovalAdmission.commands|Where-Object commandObject -NE 1831)}),
 @('v4 Stop command excluded from the fact-removal fence',{param($v)$v.observations.factRemovalAdmission.commands=@($v.observations.factRemovalAdmission.commands|Where-Object commandObject -NE 1832)})
)
foreach($mutation in $v4Mutations){$copy=Copy-TestValue $doorV4;& $mutation[1] $copy;Reject $mutation[0] {Assert-KmcChunk7WorldEvidence $request $copy 'PASS'}}
# Sprint 2 (MOVE-01) speed legs: optional, recorded with their contract; the primary mover must complete a measured native route.
function New-SpeedLegsFixture(){
    $copy=Copy-TestValue $doorV3;$o=$copy.observations
    $mover=[pscustomobject]@{id=$horse;role='mount';commandObject=1820;executorId=$horse;speedLimitAtClick=3.048;movementTypeAtClick='Normal';start=(New-TestPoint 0 0 0);end=(New-TestPoint 9 0 0);result='Success';pathLength=9.0;motionGameSeconds=3.0;effectiveSpeedMps=3.0;samples=100;movingSamples=90;meanAgentSpeedMps=3.0;maximumAgentSpeedMps=3.1;meanEntitySpeedMps=3.0;maximumEntitySpeedMps=3.0;gaits=[pscustomobject]@{Slow=90};accelerated=$false;movementTypeAtEnd='Normal';goalResidual=0.2;factsAfter=[pscustomobject]@{id=$horse}}
    $leg=[pscustomobject]@{label='mounted-party';selection='Party';primaryMoverId=$horse;distance=9.0;applicable=$true;goalSearch=(New-TestGoalSearch (New-TestPoint 0 0 0) (New-TestPoint 1 0 0) 9.0 (New-TestPoint 9 0 0) 12.0);start=(New-TestPoint 0 0 0);goal=(New-TestPoint 9 0 0);environment=[pscustomobject]@{area='x'};selectedIds=@($first,$second);factsBeforeClick=@([pscustomobject]@{id=$horse});movers=@($mover);gameSeconds=3.2}
    $skipped=[pscustomobject]@{label='mounted-solo';selection='SecondRiderAlone';primaryMoverId=$horse;distance=9.0;applicable=$false;goalSearch=(New-TestGoalSearch (New-TestPoint 9 0 0) (New-TestPoint -1 0 0) 9.0 (New-TestPoint 0 0 0) 10.0);reason='no clear route'}
    $o|Add-Member speedLegContract 'sprint2-speed-legs/v1';$o|Add-Member speedLegs @($leg,$skipped)
    $o.factRemovalAdmission.commands+=@([pscustomobject]@{commandObject=1820;finished=$true;hasExecutionProcess=$false;processEnded=$true})
    return $copy
}
$speedLegs=New-SpeedLegsFixture
Check 'Sprint 2 speed legs with one measured party leg and one not-applicable leg' {Assert-KmcChunk7WorldEvidence $request (Copy-TestValue $speedLegs) 'PASS'}
$legMutations=@(
 @('speed legs without their contract',{param($v)$v.observations.PSObject.Properties.Remove('speedLegContract')}),
 @('speed leg contract without legs',{param($v)$v.observations.PSObject.Properties.Remove('speedLegs')}),
 @('speed legs out of order',{param($v)$v.observations.speedLegs[0].label='mounted-solo'}),
 @('speed leg primary mover failed',{param($v)$v.observations.speedLegs[0].movers[0].result='Interrupt'}),
 @('speed leg primary mover too short',{param($v)$v.observations.speedLegs[0].movers[0].pathLength=2.0}),
 @('speed leg primary mover not sampled moving',{param($v)$v.observations.speedLegs[0].movers[0].movingSamples=0}),
 @('speed leg mover in an unknown role',{param($v)$v.observations.speedLegs[0].movers[0].role='camera'}),
 @('speed leg move excluded from the fact-removal fence',{param($v)$v.observations.factRemovalAdmission.commands=@($v.observations.factRemovalAdmission.commands|Where-Object commandObject -NE 1820)})
)
foreach($mutation in $legMutations){$copy=Copy-TestValue $speedLegs;& $mutation[1] $copy;Reject $mutation[0] {Assert-KmcChunk7WorldEvidence $request $copy 'PASS'}}
# v2 speed legs (Sprint 2 completion): the aligned motion interval and the v1 figure beside it.
function New-SpeedLegsV2Fixture(){
    $copy=Copy-TestValue $speedLegs;$o=$copy.observations;$o.speedLegContract='sprint2-speed-legs/v2'
    $mv=$o.speedLegs[0].movers[0];$mv|Add-Member samplingContract 'aligned-interval/v2';$mv|Add-Member firstMotionGameSeconds 0.1;$mv|Add-Member unalignedMotionGameSeconds 2.9;$mv|Add-Member effectiveSpeedMpsUnaligned 3.1
    return $copy
}
$speedLegsV2=New-SpeedLegsV2Fixture
Check 'Sprint 2 v2 speed legs with the aligned interval' {Assert-KmcChunk7WorldEvidence $request (Copy-TestValue $speedLegsV2) 'PASS'}
$legV2Mutations=@(
 @('v2 speed leg in the v1 mover shape',{param($v)$v.observations.speedLegs[0].movers[0].PSObject.Properties.Remove('samplingContract')}),
 @('v2 speed leg with another sampling contract',{param($v)$v.observations.speedLegs[0].movers[0].samplingContract='aligned-interval/v1'}),
 @('v2 speed leg aligned interval shorter than the v1 interval',{param($v)$v.observations.speedLegs[0].movers[0].unalignedMotionGameSeconds=3.5}),
 @('v2 speed leg first motion before the click',{param($v)$v.observations.speedLegs[0].movers[0].firstMotionGameSeconds=-0.5}),
 @('v1 speed leg carrying v2 fields',{param($v)$v.observations.speedLegContract='sprint2-speed-legs/v1'})
)
foreach($mutation in $legV2Mutations){$copy=Copy-TestValue $speedLegsV2;& $mutation[1] $copy;Reject $mutation[0] {Assert-KmcChunk7WorldEvidence $request $copy 'PASS'}}
Check 'door probe and speed legs write the contracts the reader requires' {$source=[IO.File]::ReadAllText((Join-Path $PSScriptRoot '..\src\KingmakerMountedCombat\Diagnostics\Chunk7NativeDoorProbe.cs'));if($source-cnotmatch'Contract = "two-pair-native-door-click-selection-change-v4"'-or$source-cnotmatch'evidence\["contract"\] = Contract;'){throw 'Door probe contract differs from the v4 reader.'}
    $engine=[IO.File]::ReadAllText((Join-Path $PSScriptRoot '..\src\KingmakerMountedCombat\Diagnostics\Chunk7WorldScenarioEngine.Sprint2.cs'));if($engine-cnotmatch'SpeedLegContract = "sprint2-speed-legs/v2"'){throw 'Speed leg contract differs from the v2 reader.'}
    $sampler=[IO.File]::ReadAllText((Join-Path $PSScriptRoot '..\src\KingmakerMountedCombat\Diagnostics\SpeedLegSampler.cs'));if($sampler-cnotmatch'Contract = "aligned-interval/v2"'){throw 'Speed leg sampler contract differs from the v2 reader.'}
    $policy=[IO.File]::ReadAllText((Join-Path $PSScriptRoot '..\src\KingmakerMountedCombat\Diagnostics\DoorReturnGoalPolicy.cs'))
    foreach($pattern in @('Contract = "door-return-goal-clear-of-units/v1"','ClearanceMargin = 0.5f','MaximumClampResidual = 0.1f','MaximumTraceResidual = 0.05f','UnitConsiderationRadius = 11f','MaximumUnits = 256','RingRadii = { 0.5f, 1f, 1.5f, 2f, 2.5f, 3f, 3.5f, 4f, 5f }','BearingsPerRing = 16')){if(-not$policy.Contains($pattern)){throw ('Door return policy constant changed without the reader: '+$pattern)}}}
# v2 route moves (F5 hardening): each fixed 2 m move goes to the first route candidate (declared direction first, then
# the bearing offsets) clear of every other scenario actor's native avoidance footprint (0.5 + 0.5 out of combat, plus the
# 0.5 m margin); v1 evidence (the $good fixture) keeps its fixed-goal path. Candidates are built independently of the reader.
function New-RouteV2Fixture($BlockerX,$BlockerZ){
    $copy=Copy-TestValue $good;$o=$copy.observations
    $offsets=@(0.0,22.5,-22.5,45.0,-45.0,67.5,-67.5,90.0,-90.0,112.5,-112.5,135.0,-135.0,157.5,-157.5,180.0)
    $routes=@(@('mammoth',$first,$mammoth,1.0,0.0),@('horse',$second,$horse,0.0,1.0),@('unmounted-control',$first,$first,-1.0,0.0))
    $far=@{$first=@(10.0,10.0);$mammoth=@(-10.0,10.0);$second=@(10.0,-10.0);$horse=@(-10.0,-10.0)}
    $searches=@()
    for($m=0;$m-lt3;$m++){$r=$routes[$m]
        $units=@(foreach($id in @($ids|Sort-Object)){if($id-cne$r[1]-and$id-cne$r[2]){$p=$far[$id];if($m-eq1-and$id-ceq$first){$p=@($BlockerX,$BlockerZ)};[pscustomobject]@{id=$id;position=[pscustomobject]@{x=$p[0];y=0.0;z=$p[1]};corpulence=0.5}}})
        $candidates=@();$selected=-1
        for($i=0;$i-lt16-and$selected-lt0;$i++){$rad=$offsets[$i]*[Math]::PI/180;$x=2*($r[3]*[Math]::Cos($rad)+$r[4]*[Math]::Sin($rad));$z=2*(-$r[3]*[Math]::Sin($rad)+$r[4]*[Math]::Cos($rad))
            $slack=[double]::PositiveInfinity;$blocker=$null
            foreach($u in $units){$v=[Math]::Sqrt(($x-$u.position.x)*($x-$u.position.x)+($z-$u.position.z)*($z-$u.position.z))-1.5;if($v-lt$slack){$slack=$v;$blocker=$u.id}}
            $reason=if($slack-lt0){'inside-unit-footprint'}else{'admitted'};$p=[pscustomobject]@{x=$x;y=0.0;z=$z}
            $candidates+=[pscustomobject]@{index=$i;bearingOffset=$offsets[$i];requested=$p;walkable=$true;reason=$reason;clamped=(Copy-TestValue $p);clampResidual=0.0;traceResidual=0.0;minimumSlack=$slack;nearestBlockerId=$blocker}
            if($reason-ceq'admitted'){$selected=$i}}
        $searches+=[pscustomobject]@{contract='world-route-goal-clear-of-units/v1';label=$r[0];selectedId=$r[1];moverId=$r[2];start=[pscustomobject]@{x=0.0;y=0.0;z=0.0};direction=[pscustomobject]@{x=$r[3];y=0.0;z=$r[4]};
            distance=2.0;margin=0.5;unitRadius=11.0;moverCorpulence=0.5;units=$units;candidates=$candidates;selectedIndex=$selected}
        $goal=$candidates[$selected].requested;$move=$o.movements[$m];$move.requestedGoal=(Copy-TestValue $goal);$move.end=[pscustomobject]@{x=$goal.x+0.05;y=0.0;z=$goal.z}
    }
    $o|Add-Member routeGoalContract 'world-route-goal-clear-of-units/v1';$o|Add-Member routeGoalSearches $searches;return $copy
}
$routeClear=New-RouteV2Fixture 10.0 10.0;$routeF4=New-RouteV2Fixture 0.3 2.7
Check 'v2 route keeps every declared fixed goal clear of the other actors' {foreach($s in $routeClear.observations.routeGoalSearches){if($s.selectedIndex-ne0){throw 'A clear declared goal was not kept.'}};Assert-KmcChunk7WorldEvidence $request (Copy-TestValue $routeClear) 'PASS'}
Check 'v2 route searches clear of rider A beside the horse goal (F4 geometry)' {$s=$routeF4.observations.routeGoalSearches[1];if($s.selectedIndex-le0-or$s.candidates[0].reason-cne'inside-unit-footprint'){throw 'F4 horse goal was not refused.'};Assert-KmcChunk7WorldEvidence $request (Copy-TestValue $routeF4) 'PASS'}
Check 'v1 movements keep their fixed-goal path' {if($null-ne$good.observations.PSObject.Properties['routeGoalSearches']){throw 'v1 fixture changed.'};Assert-KmcChunk7WorldEvidence $request (Copy-TestValue $good) 'PASS'}
$routeMutations=@(
 @('route marker without its searches',{param($v)$v.observations.PSObject.Properties.Remove('routeGoalSearches')}),
 @('route searches without their marker',{param($v)$v.observations.PSObject.Properties.Remove('routeGoalContract')}),
 @('unknown route marker',{param($v)$v.observations.routeGoalContract='world-route-goal-clear-of-units/v2'}),
 @('a movement without its route search',{param($v)$v.observations.routeGoalSearches=@($v.observations.routeGoalSearches[0],$v.observations.routeGoalSearches[1])}),
 @('route searches out of movement order',{param($v)$s=$v.observations.routeGoalSearches;$v.observations.routeGoalSearches=@($s[1],$s[0],$s[2])}),
 @('route search for another executor',{param($v)$v.observations.routeGoalSearches[1].moverId=$second}),
 @('unknown route contract',{param($v)$v.observations.routeGoalSearches[1].contract='world-route-goal-clear-of-units/v2'}),
 @('F4 horse goal claimed clear',{param($v)$s=$v.observations.routeGoalSearches[1];$c=$s.candidates[0];$c.reason='admitted';$s.candidates=@($c);$s.selectedIndex=0;$m=$v.observations.movements[1];$m.requestedGoal=(Copy-TestValue $c.requested);$m.end=[pscustomobject]@{x=$c.requested.x;y=0.0;z=$c.requested.z+0.05}}),
 @('F4 horse clearance misrecorded as clear',{param($v)$v.observations.routeGoalSearches[1].candidates[0].minimumSlack=0.2}),
 @('rider A omitted from the horse search',{param($v)$s=$v.observations.routeGoalSearches[1];$s.units=@($s.units|Where-Object id -CNE $first);foreach($c in $s.candidates){$c.nearestBlockerId=$mammoth}}),
 @('moving Horse counted as an obstacle',{param($v)$v.observations.routeGoalSearches[1].units+=[pscustomobject]@{id=$horse;position=[pscustomobject]@{x=50.0;y=0.0;z=0.0};corpulence=0.5}}),
 @('route candidate reason differs from its values',{param($v)$v.observations.routeGoalSearches[1].candidates[1].reason='off-navmesh'}),
 @('route candidate out of policy order',{param($v)$v.observations.routeGoalSearches[1].candidates[1].bearingOffset=45.0}),
 @('route requested point off its route',{param($v)$v.observations.routeGoalSearches[1].candidates[1].requested.x=0.3}),
 @('route declared direction changed',{param($v)$v.observations.routeGoalSearches[1].direction.x=1.0;$v.observations.routeGoalSearches[1].direction.z=0.0}),
 @('route clamp residual misrecorded',{param($v)$v.observations.routeGoalSearches[1].candidates[1].clampResidual=0.05}),
 @('route navmesh trace beyond its bound',{param($v)$v.observations.routeGoalSearches[1].candidates[-1].traceResidual=0.06}),
 @('route search continued past its admitted goal',{param($v)$s=$v.observations.routeGoalSearches[1];$s.candidates+=(Copy-TestValue $s.candidates[-1])}),
 @('route search without an admitted goal',{param($v)$s=$v.observations.routeGoalSearches[1];$s.candidates=@($s.candidates|Select-Object -First 1);$s.selectedIndex=-1}),
 @('movement aimed elsewhere than its route goal',{param($v)$m=$v.observations.movements[1];$m.requestedGoal.x+=0.5;$m.end.x+=0.5}),
 @('movement started elsewhere than its search',{param($v)$v.observations.movements[1].start.x=-0.5}),
 @('route margin weakened',{param($v)$v.observations.routeGoalSearches[1].margin=0.0}),
 @('route length changed',{param($v)$v.observations.routeGoalSearches[1].distance=1.0}),
 @('route unwalkable candidate with native clamp fields',{param($v)$c=$v.observations.routeGoalSearches[1].candidates[0];$c.walkable=$false;$c.reason='no-walkable-native-node'})
)
foreach($mutation in $routeMutations){$copy=Copy-TestValue $routeF4;& $mutation[1] $copy;Reject $mutation[0] {Assert-KmcChunk7WorldEvidence $request $copy 'PASS'}}
Check 'world engine writes the route search the reader requires' {
    $engine=[IO.File]::ReadAllText((Join-Path $PSScriptRoot '..\src\KingmakerMountedCombat\Diagnostics\Chunk7WorldScenarioEngine.cs'))
    foreach($pattern in @('evidence["routeGoalContract"] = GroundGoalFootprintPolicy.RouteContract','MoveOnClearRoute(secondRider, secondMount, Vector3.forward, firstRider, "horse")','MoveOnClearRoute(firstRider, firstRider, Vector3.left, secondRider, "unmounted-control")','MoveOnClearRoute(firstRider, firstMount, Vector3.right, secondRider, "mammoth")')){if(-not$engine.Contains($pattern)){throw ('World engine route differs from the reader: '+$pattern)}}
    $policy=[IO.File]::ReadAllText((Join-Path $PSScriptRoot '..\src\KingmakerMountedCombat\Diagnostics\GroundGoalFootprintPolicy.cs'))
    foreach($pattern in @('RouteContract = "world-route-goal-clear-of-units/v1"','RouteDistance = 2f','RouteBearingOffsets() => NativeGroundStepDestinationPolicy.BearingOffsets()','DoorReturnGoalPolicy.Slack(distance, moverCorpulence, otherCorpulence)')){if(-not$policy.Contains($pattern)){throw ('Route policy changed without the reader: '+$pattern)}}
    $step=[IO.File]::ReadAllText((Join-Path $PSScriptRoot '..\src\KingmakerMountedCombat\Diagnostics\NativeGroundStepDestinationPolicy.cs'))
    if(-not$step.Contains('offsets[i] = (i % 2 == 1 ? 1f : -1f) * 22.5f * ((i + 1) / 2);')-or-not$step.Contains('offsets[15] = 180f;')){throw 'Bearing offsets changed without the route reader.'}}
# The reader's door predicate is the probe's: tie the term lists to DoorFacts by source text.
function Get-DoorFactsTerms([string]$Property){
 $source=[IO.File]::ReadAllText((Join-Path $PSScriptRoot '../src/KingmakerMountedCombat/Diagnostics/Chunk7NativeDoorProbe.cs'))
 $m=[regex]::Match($source,'internal bool '+$Property+' =>([^;]+);')
 if(-not$m.Success){throw ('DoorFacts.'+$Property+' is missing.')}
 @(($m.Groups[1].Value-replace'MountDistances\.All\(distance => distance > Radius \+ 0\.5f && distance <= ScanRadius\)','')-split'&&'|ForEach-Object{($_-replace'\s','')}|Where-Object{$_-and$_-notmatch'^\(Open\|\|ClosedClickable\)$'-and$_-notmatch'^MountDistances\.All'}|ForEach-Object{
  $neg=$_.StartsWith('!');$t=$_.TrimStart('!');$t=$t.Substring(0,1).ToLowerInvariant()+$t.Substring(1);$(if($neg){'!'}else{''})+$t})
}
Check 'reader closed-clickable terms equal DoorFacts.ClosedClickable' {
 $a=@(Get-DoorFactsTerms 'ClosedClickable'|Sort-Object);$b=@(Get-KmcChunk7DoorClosedClickableTerms|Sort-Object)
 if(($a-join'|')-cne($b-join'|')){throw ('ClosedClickable drifted: '+($a-join','))}}
Check 'reader gate terms equal DoorFacts.Eligible outside open/clickable and distance' {
 $source=[IO.File]::ReadAllText((Join-Path $PSScriptRoot '../src/KingmakerMountedCombat/Diagnostics/Chunk7NativeDoorProbe.cs'))
 if($source-notmatch'\(Open \|\| ClosedClickable\)'-or$source-notmatch'MountDistances\.All\(distance => distance > Radius \+ 0\.5f && distance <= ScanRadius\)'){throw 'Eligible open/clickable or distance clause drifted.'}
 $a=@(Get-DoorFactsTerms 'Eligible'|Sort-Object);$b=@(Get-KmcChunk7DoorGateTerms|Sort-Object)
 if(($a-join'|')-cne($b-join'|')){throw ('Eligible drifted: '+($a-join','))}}
Check 'door probe serializes no evidence through bare JObject.FromObject' {
 $source=[IO.File]::ReadAllText((Join-Path $PSScriptRoot '../src/KingmakerMountedCombat/Diagnostics/Chunk7NativeDoorProbe.cs'))
 if($source-match'JObject\.FromObject\([^,()]*\)'){throw 'Bare JObject.FromObject uses the game''s global settings and can emit an empty {"$id"} shell.'}
 foreach($member in @('Result','RiderId','MountId','InteractionCount','DelegatedMoveStartCount','DelegatedMoveTickCount','DoorStateChanged','RiderPathSuppressed','MountMoveSlotRestored','TerminalReason')){if($source-notmatch('\["'+$member+'"\] = outcome\.'+$member)){throw ('Door outcome does not record '+$member)}}}
Check 'probe rows and fixture record every reader click field' {
 $source=[IO.File]::ReadAllText((Join-Path $PSScriptRoot '../src/KingmakerMountedCombat/Diagnostics/Chunk7NativeDoorProbe.cs'))
 foreach($field in Get-KmcChunk7DoorClickFields){$fixtureField=if($field-ceq'alreadyUnlocked'){'originalAlreadyUnlocked'}else{$field}
  if($source-notmatch('\["'+$field+'"\] = ')-or$source-notmatch('\["'+$fixtureField+'"\] = ')){throw ('Probe does not record '+$field)}}}
Reject 'open door with only one setup close' {$copy=Copy-TestValue $good;$copy.observations.door.setupInteractions=1;Assert-KmcChunk7WorldEvidence $request $copy 'PASS'}
Reject 'current-shape open door behind a restriction' {$copy=Copy-TestValue $good;Set-CurrentDoorShape $copy;$copy.observations.door.fixture.restrictionCount=1;Assert-KmcChunk7WorldEvidence $request $copy 'PASS'}
Reject 'current-shape open door with door-view trigger actions' {$copy=Copy-TestValue $good;Set-CurrentDoorShape $copy;$copy.observations.door.fixture.viewTriggerActions=$true;Assert-KmcChunk7WorldEvidence $request $copy 'PASS'}
Reject 'open door in the legacy fixture shape' {$copy=Copy-TestValue $good;foreach($k in @('interactable','availableInteractions','revealed','hasVisibleTrap','trapActive','originalAlreadyUnlocked','restrictionCount','hasDialog','dataPresent','viewTriggerCount','viewTriggerActions')){$copy.observations.door.fixture.PSObject.Properties.Remove($k)};foreach($k in @('observedAlreadyUnlockedBeforeRestore','alreadyUnlocked')){$copy.observations.door.restoration.PSObject.Properties.Remove($k)};Assert-KmcChunk7WorldEvidence $request $copy 'PASS'}
Check 'unchanged unlock flag observed before restoration' {$copy=Copy-TestValue $good;$copy.observations.door.restoration.observedAlreadyUnlockedBeforeRestore=$false;Assert-KmcChunk7WorldEvidence $request $copy 'PASS'}
Reject 'unlock flag observed cleared on a door captured unlocked' {$copy=Copy-TestValue $good;$copy.observations.door.fixture.originalAlreadyUnlocked=$true;$copy.observations.door.restoration.alreadyUnlocked=$true;$copy.observations.door.restoration.observedAlreadyUnlockedBeforeRestore=$false;Assert-KmcChunk7WorldEvidence $request $copy 'PASS'}
Reject 'observed unlock flag before restoration missing' {$copy=Copy-TestValue $good;$copy.observations.door.restoration.PSObject.Properties.Remove('observedAlreadyUnlockedBeforeRestore');Assert-KmcChunk7WorldEvidence $request $copy 'PASS'}
Reject 'observed unlock flag before restoration as text' {$copy=Copy-TestValue $good;$copy.observations.door.restoration.observedAlreadyUnlockedBeforeRestore='true';Assert-KmcChunk7WorldEvidence $request $copy 'PASS'}
Reject 'current-shape open door restoration lost the unlock flag' {$copy=Copy-TestValue $good;Set-CurrentDoorShape $copy;$copy.observations.door.restoration.alreadyUnlocked=$true;Assert-KmcChunk7WorldEvidence $request $copy 'PASS'}
Reject 'open door with an active cut' {$copy=Copy-TestValue $good;$copy.observations.door.fixture.originalCut=$true;$copy.observations.door.restoration.cutEnabled=$true;Assert-KmcChunk7WorldEvidence $request $copy 'PASS'}

$mutations=@(
    @('artifact schema string',{param($v)$v.schemaVersion='1'}),
    @('native cast count string',{param($v)$v.observations.inputs[0].nativeCastDelta='1'}),
    @('native generation string',{param($v)$v.observations.inputs[0].generationAtInit='1'}),
    @('native generation fraction',{param($v)$v.observations.inputs[0].generationAtInit=1.5}),
    @('sample count string',{param($v)$v.observations.measurements[0].sampleFrames='120'}),
    @('active pair count string',{param($v)$v.observations.measurements[0].ownersAfter.active='0'}),
    @('subscriber count string',{param($v)$v.observations.measurements[0].subscriptionsAfter.uniqueOwnedSubscribers='1'}),
    @('writer schema string',{param($v)$v.observations.capability.nativeWriterSchema='3'}),
    @('native reaction expenditure changes',{param($v)$v.observations.inputs[0].resourcesAfter.actors[0].reactionAllowance=2}),
    @('native reaction counter string',{param($v)$v.observations.inputs[0].resourcesAfter.actors[0].reactionAllowance='1'}),
    @('native initiative counter string',{param($v)$v.observations.inputs[0].resourcesBefore.actors[0].initiative='0'}),
    @('native command object over Int32',{param($v)$v.observations.inputs[0].commandObject=2147483648L;$v.observations.factRemovalAdmission.commands[0].commandObject=2147483648L}),
    @('native command object fraction',{param($v)$v.observations.inputs[0].commandObject=100.5}),
    @('native subscriber object string',{param($v)$v.observations.measurements[0].subscriptionsAfter.entries[0].identity='77'}),
    @('owner generation string',{param($v)$v.observations.snapshots[0].owners[0].generation='1'}),
    @('cycle owner generation fraction',{param($v)$v.observations.cycles[0].owners[0].generation=3.5}),
    @('cycle boundary count string',{param($v)$v.observations.cycles[0].boundaryCount='2'}),
    @('native Ranger count string',{param($v)$v.observations.acquisition.nativeRangerCommitCount='4'}),
    @('native acquisition boolean string',{param($v)$v.observations.acquisition.horsePresentAtNativeCommit='true'}),
    @('saved proof scope string',{param($v)$v.observations.cleanup.nativeSaveWritten='false'}),
    @('combat proof scope string',{param($v)$v.observations.combatOrPersistenceProof='false'}),
    @('restored pause strings',{param($v)$v.observations.cleanup.pauseBefore='true';$v.observations.cleanup.pauseAfter='true'}),
    @('pair residue string',{param($v)$v.observations.cleanup.pairBoundaries[0].residueAbsent='true'}),
    @('source identity',{param($v)$v.commit='9'*40}),@('closed native gate',{param($v)$v.observations.capability.archiveNativeEnabled=$false}),
    @('native schema2',{param($v)$v.observations.capability.nativeWriterSchema=2}),@('one-pair native capacity',{param($v)$v.observations.capability.nativePairCapacity=1}),
    @('unqualified fixture',{param($v)$v.observations.capability.workingSha256='9'*64}),@('invented original actor',{param($v)$v.observations.partyBefore[0].id=$horse}),
    @('foreign companion owner',{param($v)$v.observations.partyWithHorse[3].masterId=$first}),@('original Mammoth replaced',{param($v)$v.observations.partyBefore[0].petId=$horse}),
    @('direct fact instead of native commits',{param($v)$v.observations.acquisition.nativeRangerCommitCount=0}),@('missing native input',{param($v)$v.observations.inputs=@($v.observations.inputs[0..2])}),
    @('wrong input actor',{param($v)$v.observations.inputs[1].casterId=$first}),@('repeat input command',{param($v)$v.observations.inputs[1].commandObject=100}),
    @('missing shell binding',{param($v)$v.observations.inputs[1].shellControl=$null}),@('unobserved native cast',{param($v)$v.observations.inputs[1].nativeCastDelta=0}),
    @('input boolean string',{param($v)$v.observations.inputs[1].clicked='true'}),@('unfinished native execution process',{param($v)$v.observations.inputs[1].processTerminal=$false}),@('execution process boolean string',{param($v)$v.observations.inputs[1].processTerminal='true'}),@('missing cooldown',{param($v)$v.observations.inputs[1].resourcesAfter.actors[0].move=$null}),
    @('native move spending',{param($v)$v.observations.inputs[1].resourcesAfter.actors[0].move=3.0}),@('native action refund',{param($v)$v.observations.inputs[1].resourcesBefore.actors[0].standard=3.0}),
    @('invented preparation',{param($v)$v.observations.inputs[1].resourcesAfter.actors[0].prepared=$true}),@('actor resource dropped',{param($v)$v.observations.inputs[1].resourcesAfter.actors=@($v.observations.inputs[1].resourcesAfter.actors[0..2])}),
    @('same pair owner',{param($v)$v.observations.snapshots[0].owners[1].ownerId=$v.observations.snapshots[0].owners[0].ownerId}),
    @('selection changes generation',{param($v)$v.observations.snapshots[1].owners[1].generation=3}),@('dismount drops other owner',{param($v)$v.observations.snapshots[2].owners=@()}),
    @('movement routes to rider',{param($v)$v.observations.movements[0].executorId=$first}),@('movement commands another actor',{param($v)$v.observations.movements[0].commandsAfter[0].moveObject=999}),
    @('no actual movement',{param($v)$v.observations.movements[0].end.x=0.0}),@('null movement coordinate',{param($v)$v.observations.movements[0].end.x=$null}),
    @('late movement command',{param($v)$v.observations.movements[0].commandTerminal=$false}),@('retained cleanup owner',{param($v)$v.observations.cleanup.pairBoundaries[0].ownerPresent=$true}),
    @('native execution process unfinished at cleanup',{param($v)$v.observations.cleanup.nativeProcessesSettled=$false}),@('host abandoned native custody',{param($v)$v.observations.cleanup.cleanupCustodySettled=$false}),@('string native process proof',{param($v)$v.observations.cleanup.nativeProcessesSettled='true'}),
    @('native process alive at fact removal',{param($v)$v.observations.factRemovalAdmission.commands[0].processEnded=$false}),
    @('missing native command at fact removal',{param($v)$v.observations.factRemovalAdmission.commands=@($v.observations.factRemovalAdmission.commands|Select-Object -Skip 1)}),
    @('foreign native command at fact removal',{param($v)$v.observations.factRemovalAdmission.commands[0].commandObject=99999}),
    @('pair retained at fact removal',{param($v)$v.observations.factRemovalAdmission.owners=1}),
    @('preparation retained at fact removal',{param($v)$v.observations.factRemovalAdmission.preparationPending=$true}),
    @('archive retained at fact removal',{param($v)$v.observations.factRemovalAdmission.serializationBusy=$true}),
    @('worker retained at fact removal',{param($v)$v.observations.factRemovalAdmission.pendingNativeWrites=$true}),
    @('string fact-removal count',{param($v)$v.observations.factRemovalAdmission.owners='0'}),
    @('fact removal before last sample',{param($v)$v.observations.factRemovalAdmission.frame=100}),
    @('settings not restored',{param($v)$v.observations.cleanup.pairedAfter=$false}),@('claim unproved combat',{param($v)$v.observations.combatOrPersistenceProof=$true}),
    @('claim ordinary respec from cleanup',{param($v)$v.observations.cleanup.ordinaryRespecProven=$true}),
    @('invented timing summary',{param($v)$v.observations.measurements[2].serviceP95Ms=0.0}),
    @('missing sample window',{param($v)$v.observations.measurements=@($v.observations.measurements[0..2])}),
    @('empty timer samples',{param($v)$v.observations.measurements[1].serviceTicks=@()}),
    @('invented heap change',{param($v)$v.observations.measurements[2].managedHeapDelta=0}),
    @('forced collection substitutes quiet heap',{param($v)$v.observations.measurements[2].forcedCollection=$true}),
    @('additional global subscriber',{param($v)$v.observations.measurements[2].subscriptionsAfter.entries[0].identity=99}),
    @('wrong pair count sample',{param($v)$v.observations.measurements[1].ownersAfter.active=2}),
    @('missing cycle input',{param($v)$v.observations.cycleInputs=@($v.observations.cycleInputs[0..10])}),
    @('reused native cycle command',{param($v)$v.observations.cycleInputs[0].commandObject=100}),
    @('stale cycle generation',{param($v)$v.observations.cycles[0].owners[0].generation=1}),
    @('retained cycle owner',{param($v)$v.observations.cycles[1].owners=@($v.observations.cycles[0].owners[0])}),
    @('accumulated pair boundaries',{param($v)$v.observations.cycles[5].boundaryCount=3})
)
foreach($mutation in $mutations){$changed=Copy-TestValue $good;& $mutation[1] $changed;Reject $mutation[0] {Assert-KmcChunk7WorldEvidence $request $changed 'PASS'}}
# Producer-to-reader identity forms. The producer writes pair-owner rider/mount ids as the
# registry key (Guid "N", compact lowercase) and actor UniqueIds as Guid "D" (lowercase,
# hyphenated); the reader compares the two by Guid value only after each side matches its own
# exact form, and refuses with the identity-form message, not some later incidental check.
function RejectIdentity([string]$Name,[scriptblock]$RejectedTrial){$message=$null;try{& $RejectedTrial}catch{$message=$_.Exception.Message}
    if($null-eq$message){throw ('Unexpected acceptance: '+$Name)}
    if($message-notmatch'Guid \((N|D)\) form|identity set differs|lost its captured pair|did not return through its own|Remount reused|Dismount changed the other pair owner'){throw ('Refused for an unrelated reason: '+$Name+': '+$message)}
    $script:passed++;Write-Host ('PASS refuses '+$Name)}
Check 'owner records in N form with actor UniqueIds in D form are accepted' {
    $o=$good.observations;$active=@($o.snapshots[0].owners)
    if(@($active|Where-Object{$_.riderId-cnotmatch'\A[0-9a-f]{32}\z'-or$_.mountId-cnotmatch'\A[0-9a-f]{32}\z'}).Count-ne0-or
        @($o.partyWithHorse|Where-Object{$_.id-cnotmatch'\A[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\z'}).Count-ne0-or
        @($o.door.interactions|Where-Object{$_.riderId-cnotmatch'-'}).Count-ne0){throw 'Fixture does not carry the producer forms.'}
    Assert-KmcChunk7WorldEvidence $request $good 'PASS'
}
Check 'owner/actor identity helper matches only the exact native pair of forms' {
    $n=$first.Replace('-','')
    if(-not(Test-KmcChunk7OwnerActorMatch $n $first)-or(Test-KmcChunk7OwnerActorMatch $n $second)){throw 'Exact N/D Guid value match differs.'}
    foreach($bad in @(@($first,$first),@($n,$n),@($n.ToUpperInvariant(),$first),@($n,$first.ToUpperInvariant()),@(('{'+$first+'}'),$first),@($n,('{'+$first+'}')),
        @((' '+$n),$first),@(($n+"`n"),$first),@($n,($first+' ')),@($n.Substring(1),$first),@(($n.Substring(1)+'g'),$first),@($n,$first.Replace('-','_')),@($null,$first),@($n,$null),@(12,$first))){
        $refused=$false;try{[void](Test-KmcChunk7OwnerActorMatch $bad[0] $bad[1])}catch{$refused=$true}
        if(-not$refused){throw ('Wrong-form identity accepted: '+[string]$bad[0]+' / '+[string]$bad[1])}
    }
}
$otherGuid='0f0e0d0c-0b0a-4908-8706-050403020100'
$identityMutations=@(
    @('owner riderId in actor D form',{param($v)$v.observations.snapshots[0].owners[0].riderId=$first}),
    @('owner mountId in actor D form',{param($v)$v.observations.snapshots[0].owners[1].mountId=$horse}),
    @('owner riderId uppercase N',{param($v)$v.observations.snapshots[0].owners[0].riderId=$first.Replace('-','').ToUpperInvariant()}),
    @('owner riderId uppercase D',{param($v)$v.observations.snapshots[0].owners[0].riderId=$first.ToUpperInvariant()}),
    @('owner riderId braced',{param($v)$v.observations.snapshots[0].owners[0].riderId='{'+$first+'}'}),
    @('owner riderId padded N',{param($v)$v.observations.snapshots[0].owners[0].riderId=' '+$first.Replace('-','')}),
    @('owner riderId malformed hex',{param($v)$v.observations.snapshots[0].owners[0].riderId=$first.Replace('-','').Substring(1)+'z'}),
    @('owner riderId non-string',{param($v)$v.observations.snapshots[0].owners[0].riderId=12345}),
    @('owner riderId a different Guid',{param($v)$v.observations.snapshots[0].owners[0].riderId=$otherGuid.Replace('-','')}),
    @('owner mountId swapped to the other pair',{param($v)$v.observations.snapshots[0].owners[0].mountId=$horse.Replace('-','')}),
    @('cycle owner rider in actor D form',{param($v)$v.observations.cycles[0].owners[0].riderId=$first}),
    @('cycle owner mount a different Guid',{param($v)$v.observations.cycles[2].owners[1].mountId=$otherGuid.Replace('-','')}),
    @('dismount retained owner in actor D form',{param($v)$v.observations.snapshots[2].owners[0].riderId=$second}),
    @('door rider actor in owner N form',{param($v)$v.observations.door.interactions[0].riderId=$first.Replace('-','');$v.observations.door.interactions[0].executorId=$v.observations.door.interactions[0].riderId;$v.observations.door.interactions[0].outcome.RiderId=$v.observations.door.interactions[0].riderId}),
    @('door mount actor uppercase D',{param($v)$v.observations.door.interactions[0].mountId=$mammoth.ToUpperInvariant();$v.observations.door.interactions[0].outcome.MountId=$v.observations.door.interactions[0].mountId}),
    @('door mount actor a different Guid',{param($v)$v.observations.door.interactions[1].mountId=$otherGuid;$v.observations.door.interactions[1].outcome.MountId=$otherGuid}),
    @('first-pair return executor in owner N form',{param($v)$v.observations.door.firstPairReturn.executorId=$mammoth.Replace('-','')})
)
if($mammoth.ToUpperInvariant()-ceq$mammoth-or$first.ToUpperInvariant()-ceq$first){throw 'Uppercase identity fixtures need hex letters.'}
foreach($mutation in $identityMutations){$changed=Copy-TestValue $good;& $mutation[1] $changed;RejectIdentity $mutation[0] {Assert-KmcChunk7WorldEvidence $request $changed 'PASS'}}
Check 'signed native Int32 command identities remain valid' {
    $signed=Copy-TestValue $good
    foreach($row in @($signed.observations.inputs)+@($signed.observations.cycleInputs)+@($signed.observations.movements)+@($signed.observations.door.interactions)+@($signed.observations.factRemovalAdmission.commands)){$row.commandObject=-$row.commandObject}
    $signed.observations.door.firstPairReturn.command=-$signed.observations.door.firstPairReturn.command
    foreach($row in $signed.observations.movements){foreach($cmd in @($row.commandsBefore)+@($row.commandsAfter)){$cmd.moveObject=-$cmd.moveObject}}
    Assert-KmcChunk7WorldEvidence $request $signed 'PASS'
}
Check 'signed native initiative and reaction observations remain unchanged' {
    $signed=Copy-TestValue $good
    foreach($row in @($signed.observations.inputs)+@($signed.observations.cycleInputs)){
        foreach($window in @($row.resourcesBefore,$row.resourcesAfter)){foreach($actor in $window.actors){$actor.initiative=-100;$actor.reactionAllowance=-1}}
    }
    Assert-KmcChunk7WorldEvidence $request $signed 'PASS'
}
Check 'FAIL partial artifact preserves failed prefix' {$partial=Copy-TestValue $good;$partial.status='FAIL';$partial.errors=@('fixture unavailable');$partial.observations=[pscustomobject]@{};Assert-KmcChunk7WorldEvidence $request $partial 'FAIL'}
Reject 'FAIL artifact cannot fulfill PASS run' {$partial=Copy-TestValue $good;$partial.status='FAIL';Assert-KmcChunk7WorldEvidence $request $partial 'PASS'}
$dependencies=[pscustomobject]@{gameRoot='C:\SyntheticKingmaker';modsRoot='C:\SyntheticKingmaker\Mods';files=@()}
$assemblyRows=@([pscustomobject]@{name='KingmakerMountedCombat';fullName='KingmakerMountedCombat';dynamic=$false;location='C:\SyntheticKingmaker\Mods\KingmakerMountedCombat\KingmakerMountedCombat.dll';sha256=$request.dllSha256;mvid=$request.dllMvid})
foreach($relative in @('Kingmaker_Data/Managed/Assembly-CSharp.dll','Kingmaker_Data/Managed/UnityModManager/UnityModManager.dll','Kingmaker_Data/Managed/UnityModManager/0Harmony12.dll')){
    $dependencies.files+=[pscustomobject]@{relativePath=$relative;sha256='8'*64};$assemblyRows+=[pscustomobject]@{name=([IO.Path]::GetFileNameWithoutExtension($relative));fullName=$relative;dynamic=$false;location=(Join-Path $dependencies.gameRoot $relative);sha256='8'*64;mvid='99999999-9999-4999-8999-999999999999'}
}
$inventory=New-Artifact 'runtime-loader-inventory' ([pscustomobject]@{capturedInNativeProcess=$true;processId=100;modEntries=@([pscustomobject]@{id='KingmakerMountedCombat';active=$true;enabled=$true});loadedAssemblies=$assemblyRows})
Check 'exact native base-only loader inventory' {Assert-KmcBaseGameNativeInventory $request $inventory $dependencies}
function New-TestAssembly([string]$Name,[string]$Location) {
    return [pscustomobject]@{name=$Name;fullName=$Name;dynamic=$false;location=$Location;sha256='7'*64;mvid='77777777-7777-4777-8777-777777777777'}
}
Check 'additional native Managed assembly is recorded with hash and MVID' {
    $extra=Copy-TestValue $inventory;$extra.observations.loadedAssemblies+=New-TestAssembly 'System' 'C:\SyntheticKingmaker\Kingmaker_Data\Managed\System.dll'
    $scope=Assert-KmcBaseGameNativeInventory $request $extra $dependencies -PassThru
    if($scope.fileBackedAssemblyCount-ne5-or$scope.completeStockAssemblyAllowlistProven-ne$false){throw 'Directory allowance was mistaken for a complete stock identity allowlist.'}
}
Check 'additional native UMM dependency has a file-backed identity' {
    $extra=Copy-TestValue $inventory;$extra.observations.loadedAssemblies+=New-TestAssembly 'dnlib' 'C:\SyntheticKingmaker\Kingmaker_Data\Managed\UnityModManager\dnlib.dll'
    Assert-KmcBaseGameNativeInventory $request $extra $dependencies
}
Check 'dynamic assembly identities retained without qualification claim' {
    $extra=Copy-TestValue $inventory;$extra.observations.loadedAssemblies+=[pscustomobject]@{name='DynamicNative';fullName='DynamicNative, Version=0.0.0.0';dynamic=$true;location=$null;sha256=$null;mvid=$null}
    $scope=Assert-KmcBaseGameNativeInventory $request $extra $dependencies -PassThru
    if($scope.dynamicAssemblyCount-ne1-or$scope.dynamicAssemblies[0].fullName-cne'DynamicNative, Version=0.0.0.0'-or
        $scope.dynamicAssembliesQualified-ne$false-or$scope.completeStockAssemblyAllowlistProven-ne$false-or[string]::IsNullOrWhiteSpace($scope.limitation)){throw 'Dynamic evidence limit was dropped.'}
}
$inventoryMutations=@(@('foreign active mod',{param($v)$v.observations.modEntries+=[pscustomobject]@{id='ForeignMod';active=$true;enabled=$true}}),
    @('foreign disabled mod',{param($v)$v.observations.modEntries+=[pscustomobject]@{id='ForeignMod';active=$false;enabled=$false}}),@('KMC inactive',{param($v)$v.observations.modEntries[0].active=$false}),
    @('KMC enabled string',{param($v)$v.observations.modEntries[0].enabled='true'}),@('missing loaded Harmony',{param($v)$v.observations.loadedAssemblies=@($v.observations.loadedAssemblies[0..2])}),
    @('changed loaded KMC',{param($v)$v.observations.loadedAssemblies[0].sha256='1'*64}),@('changed loaded gameplay',{param($v)$v.observations.loadedAssemblies[1].sha256='1'*64}),
    @('foreign Mods assembly',{param($v)$v.observations.loadedAssemblies+=New-TestAssembly 'ForeignMod' 'C:\SyntheticKingmaker\Mods\ForeignMod\Foreign.dll'}),
    @('external file-backed assembly',{param($v)$v.observations.loadedAssemblies+=New-TestAssembly 'ForeignMod' 'C:\ForeignMods\Foreign.dll'}),
    @('Managed sibling prefix path',{param($v)$v.observations.loadedAssemblies+=New-TestAssembly 'ForeignMod' 'C:\SyntheticKingmaker\Kingmaker_Data\Managed-foreign\Foreign.dll'}),
    @('Managed nested foreign directory',{param($v)$v.observations.loadedAssemblies+=New-TestAssembly 'ForeignMod' 'C:\SyntheticKingmaker\Kingmaker_Data\Managed\Foreign\Foreign.dll'}),
    @('Managed traversal outside exact roots',{param($v)$v.observations.loadedAssemblies+=New-TestAssembly 'ForeignMod' 'C:\SyntheticKingmaker\Kingmaker_Data\Managed\..\Foreign.dll'}),
    @('non-dynamic assembly without location',{param($v)$v.observations.loadedAssemblies+=New-TestAssembly 'Locationless' $null}),
    @('non-dynamic assembly with whitespace location',{param($v)$v.observations.loadedAssemblies+=New-TestAssembly 'Locationless' ' '}),
    @('relative file-backed assembly location',{param($v)$v.observations.loadedAssemblies[1].location='Kingmaker_Data\Managed\Assembly-CSharp.dll'}),
    @('KMC loaded from Managed instead of candidate path',{param($v)$v.observations.loadedAssemblies[0].location='C:\SyntheticKingmaker\Kingmaker_Data\Managed\KingmakerMountedCombat.dll'}),
    @('KMC loaded from another Mods directory',{param($v)$v.observations.loadedAssemblies[0].location='C:\SyntheticKingmaker\Mods\ForeignMod\KingmakerMountedCombat.dll'}),
    @('missing observed assembly hash',{param($v)$v.observations.loadedAssemblies[1].sha256=$null}),
    @('malformed observed assembly hash',{param($v)$v.observations.loadedAssemblies[1].sha256='not-a-hash'}),
    @('missing observed MVID',{param($v)$v.observations.loadedAssemblies[1].mvid=$null}),
    @('malformed observed MVID',{param($v)$v.observations.loadedAssemblies[1].mvid='not-a-guid'}),
    @('empty observed MVID',{param($v)$v.observations.loadedAssemblies[1].mvid='00000000-0000-0000-0000-000000000000'}),
    @('missing assembly identity field',{param($v)$v.observations.loadedAssemblies[1].PSObject.Properties.Remove('fullName')}),
    @('assembly observation error hidden by PASS',{param($v)$v.observations.loadedAssemblies[1]|Add-Member observationError 'hash unavailable'}),
    @('dynamic assembly claims a file identity',{param($v)$v.observations.loadedAssemblies[1].dynamic=$true}),
    @('dynamic KMC cannot stand in for candidate',{param($v)$v.observations.loadedAssemblies[0].dynamic=$true;$v.observations.loadedAssemblies[0].location=$null;$v.observations.loadedAssemblies[0].sha256=$null;$v.observations.loadedAssemblies[0].mvid=$null}),
    @('duplicated required loaded gameplay assembly',{param($v)$v.observations.loadedAssemblies+=(Copy-TestValue $v.observations.loadedAssemblies[1])}),
    @('process ID string',{param($v)$v.observations.processId='100'}))
foreach($mutation in $inventoryMutations){$changed=Copy-TestValue $inventory;& $mutation[1] $changed;Reject $mutation[0] {Assert-KmcBaseGameNativeInventory $request $changed $dependencies}}
Check 'exact compound/request case registered once' {if(-not(Test-KmcCompoundRuntimeScenario $request.scenario)-or@(Get-KmcSaveBackedRuntimeScenarios|Where-Object {$_-ceq$request.scenario}).Count-ne1){throw 'World case registration differs.'}}
Check 'new scripts parse' {foreach($file in @('runtime/Chunk7WorldEvidence.ps1','runtime/Invoke-KingmakerRuntimeScenario.ps1','runtime/Test-RuntimeRequest.ps1','runtime/Test-RuntimeResult.ps1','runtime/Test-RuntimeGameResult.ps1')){$syntax=$null;$tokens=$null;[void][Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot $file),[ref]$tokens,[ref]$syntax);if($syntax.Count-ne0){throw ($file+': '+($syntax|Out-String))}}}
# Synthetic filesystem artifacts verify the exact manifest/immutable-byte boundary.
$fixtureRoot=Join-Path ([IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))) ('obj/chunk7-reader-tests/'+[Guid]::NewGuid().ToString('N'))
[void](New-Item -ItemType Directory -Path $fixtureRoot)
$boundRequest=Copy-TestValue $request;$boundRequest|Add-Member evidenceRoot $fixtureRoot
$worldPath=Join-Path $fixtureRoot 'chunk7-world-evidence.json';$loaderPath=Join-Path $fixtureRoot 'runtime-loader-inventory.json'
Write-KmcJsonAtomic $worldPath $good;Write-KmcJsonAtomic $loaderPath $inventory
$visualRoot=Join-Path $fixtureRoot 'movement-visuals';[void](New-Item -ItemType Directory -Path $visualRoot)
$visualPath=Join-Path $visualRoot 'chunk7-two-pair-mounted.png'
[IO.File]::WriteAllBytes($visualPath,[Convert]::FromBase64String('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII='))
$artifactRows=@(foreach($pair in @(@($worldPath,'chunk7-world-evidence.json','chunk7-world-evidence'),@($loaderPath,'runtime-loader-inventory.json','runtime-loader-inventory'),@($visualPath,'movement-visuals/chunk7-two-pair-mounted.png','screenshot'))){[pscustomobject]@{relativePath=$pair[1];kind=$pair[2];length=[long](Get-Item -LiteralPath $pair[0]).Length;sha256=(Get-KmcSha256 $pair[0])}})
$manifest=[pscustomobject]@{schemaVersion=1;runId=$request.runId;scenario=$request.scenario;createdAtUtc='2026-10-12T00:00:00Z';artifacts=$artifactRows}
$manifestPath=Join-Path $fixtureRoot 'runtime-artifacts.json';Write-KmcJsonAtomic $manifestPath $manifest
Check 'exact world and loader artifact manifest allowed' {[void](Assert-KmcReadOnlyArtifactManifest $boundRequest (Get-KmcSha256 $manifestPath));Assert-KmcChunk7WorldArtifact $boundRequest $manifest 'PASS'}
Reject 'missing screenshot manifest receipt' {$missing=Copy-TestValue $manifest;$missing.artifacts=@($missing.artifacts[0..1]);Assert-KmcChunk7WorldArtifact $boundRequest $missing 'PASS'}
Reject 'world artifact under other request' {$other=Copy-TestValue $boundRequest;$other.scenario='chunk7-two-pair-combat';Assert-KmcChunk7WorldArtifact $other $manifest 'PASS'}
Reject 'artifact identity changed' {$bad=Copy-TestValue $good;$bad.dllSha256='9'*64;Write-KmcJsonAtomic $worldPath $bad;Assert-KmcChunk7WorldArtifact $boundRequest $manifest 'PASS'}
Write-KmcJsonAtomic $worldPath $good
Reject 'unregistered world evidence kind' {$bad=Copy-TestValue $manifest;$bad.artifacts[0].kind='chunk7-combat-evidence';Write-KmcJsonAtomic $manifestPath $bad;[void](Assert-KmcReadOnlyArtifactManifest $boundRequest (Get-KmcSha256 $manifestPath))}
Write-KmcJsonAtomic $manifestPath $manifest
Check 'foreign scenario cannot claim missing world evidence as PASS' {$other=Copy-TestValue $boundRequest;$other.scenario='mod-load-smoke';$empty=Copy-TestValue $manifest;$empty.artifacts=@();Assert-KmcChunk7WorldArtifact $other $empty 'PASS'}
Write-Host ('TOTAL PASS='+$passed+' FAIL=0; synthetic reader contracts only, no native qualification')
