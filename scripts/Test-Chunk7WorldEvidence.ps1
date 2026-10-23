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
