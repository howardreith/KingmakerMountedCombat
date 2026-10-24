[CmdletBinding()]
param([string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Initialize-Chunk7Tests.ps1')
if(-not$OutputDirectory){$OutputDirectory=Join-Path $chunk7AnalysisRoot ('six-reader-'+[Guid]::NewGuid().ToString('N'))}
$OutputDirectory=Assert-KmcChildPath $OutputDirectory $chunk7AnalysisRoot 'six-pair generated reader checks'
if(Test-Path -LiteralPath $OutputDirectory){throw 'Preserve prior six-pair reader checks.'};[void][IO.Directory]::CreateDirectory($OutputDirectory)
function Clone($Value){
 $decoded=$Value|ConvertTo-Json -Depth 90|ConvertFrom-Json
 # PS5 adds extended Count/value properties when a returned array is wrapped.
 # Enumerate the decoded members so generated wire arrays stay JSON arrays.
 if($decoded-is[Array]){foreach($item in $decoded){$item}}else{$decoded}
}
$sourceRequest=Read-KmcJson (Join-Path (Get-KmcLabRoot) 'runtime-evidence/20261022-c7b-world185-b185s1-sixthhorse-cold/runtime-request.json')
$roles=@(Get-KmcChunk7SixPairRoles);$request=Clone $sourceRequest;$request.scenario='chunk7-owned-two-pair-campaign';$request.runId='generated-six-reader'
$request.PSObject.Properties.Remove('assistedColdInventory');$request|Add-Member persistenceCase $null
$selection=[pscustomobject][ordered]@{case='six-horse-cold-fixture-lifecycle-stress';sourceRunId=$sourceRequest.runId;
 sourceArtifactSha256='012d1caff175d34614fcd255f0b6ad897e0ef1a9650791cb3c0d55ce13b47241';sourceArchiveSha256=$sourceRequest.persistenceLoad.sha256;
 checkpointSourceRunId=$null;checkpointSourceArtifactSha256=$null;riderAId=$roles[0];mountAId=$roles[1];riderBId=$roles[2];mountBId=$roles[3];
 partyIds=$roles;baseGameOnly=$true;expectedNativeLoadCount=1;expectedNativeWriteCount=0}
$request|Add-Member ownedPairs $selection
Assert-KmcChunk7OwnedPairsDescriptor $request
$qualified=Get-KmcChunk7OwnedPairsSource $selection $null $request.fixture $request
if($qualified.archiveProducerRunId-cne'20261022-c7b-world184-b184s1-sixthhorse-manual'-or$qualified.qualificationRunId-cne$sourceRequest.runId-or
 $qualified.receiptPins.Count-ne16-or$qualified.nativeAuthorityGranted-ne$false-or$qualified.sixPairCheckpoint-ne$true){throw 'Actual six-pair source routing lost its exact checkpoint or granted authority.'}
$cold=(Read-KmcJson $qualified.artifactPath).observations.settledWorld
# After the actual complete routed gate succeeds, only that external qualification
# boundary is substituted for generated reader mutations. Exact reader methods
# and retained cold observation decoding below remain unmodified.
function Get-KmcChunk7SixPairCheckpointSource($Fixture){Assert-KmcAssistedSame $Fixture $sourceRequest.fixture 'six-reader source fixture delegation';$qualified}
$arrangement=[pscustomobject]@{money=$cold.nativeMoney;experienceRatePercent=100;actors=@($cold.party|Sort-Object id -CaseSensitive|ForEach-Object{
 [pscustomobject]@{actorId=$_.id;blueprint=$_.blueprint;experience=$_.experience;characterLevel=$_.characterLevel;
 classes=@($_.classes|Sort-Object guid -CaseSensitive|ForEach-Object{[pscustomobject]@{classGuid=$_.guid;level=$_.level}})}})}
$subs=[pscustomobject]@{assemblyMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7';executing=$false;uniqueOwnedSubscribers=1;ownedInterfaceEntries=1;
 entries=@([pscustomobject]@{interface='native-interface';type='KmcSubscriber';identity=-77})}
function Resources([long]$Ticks){[pscustomobject]@{gameTicks=$Ticks;actors=@($roles|ForEach-Object{[pscustomobject]@{id=$_;move=0.0;standard=0.0;swift=0.0;prepared=$false;reactionAllowance=1;initiative=0}})}}
function Owners([int]$Count,[int]$Cycle=1){for($i=0;$i-lt$Count;$i++){
 [pscustomobject]@{ownerId=('00000000-0000-0000-0000-'+($Cycle*100+$i+1).ToString('000000000000'));riderId=([Guid]$roles[$i*2]).ToString('N');mountId=([Guid]$roles[$i*2+1]).ToString('N');
 generation=$Cycle;state='Active';relationshipState='Mounted';retainsObligations=$true;saveReady=$true;teardownReady=$false}
}}
function World([string]$Label,[int]$Active,[int]$Frame=100,[int]$Cycle=1){[pscustomobject]@{label=$Label;frame=$Frame;boundaryCount=6;participantCount=6;
 selection=@($roles[0]);resources=(Resources ([long]$Frame*100000));subscriptions=(Clone $subs);owners=@(Owners $Active $Cycle)}}
$pairs=@(for($i=0;$i-lt6;$i++){[pscustomobject]@{riderId=$roles[$i*2];mountId=$roles[$i*2+1];rider=$i*4+1;mount=$i*4+2;feature=$i*4+3;rankFact=$i*4+4;
 sourceGuid='97261d609529d834eba4fd4da1bc44dc';rank=1;profileId=$(if($i-cin@(1,4,5)){'small-default-halfling-horse-v1'}else{'medium-humanoid-horse-v1'})}})
$remote=[pscustomobject]@{actorId='8265f8cf-0969-4ec7-a266-2448de91c8c4';object=25;descriptor=26;petId=$null;masterId=$null;
 arrangement=[pscustomobject]@{money=357290;experienceRatePercent=100;actors=@([pscustomobject]@{actorId='8265f8cf-0969-4ec7-a266-2448de91c8c4';blueprint='77c11edb92ce0fd408ad96b40fd27121';experience=9000;characterLevel=4;classes=@([pscustomobject]@{classGuid='772c83a25e2268e448e841dcd548235f';level=4})})}}
function Inherited([string]$Phase){[pscustomobject]@{contract='cold-qualified-six-horse-native-field-v1';phase=$Phase;sourceRunId=$selection.sourceRunId;sourceArtifactSha256=$selection.sourceArtifactSha256;
 sourceArchiveSha256=$selection.sourceArchiveSha256;sourceCase='sixth-horse-manual-readonly-inventory';nativeLoadVerifiedByHost=$true;decodedReceiptAuthorizesRestoration=$false;inheritedPetsPreserved=$true;ownedFactRemovalCount=0;
 partyIds=$roles;currentPartyIds=$roles;currentCharacterIds=@(for($i=0;$i-lt6;$i++){$roles[$i*2]});remoteCapturedIds=@();arrangement=(Clone $arrangement);pairs=(Clone $pairs);
 identities=[pscustomobject]@{capacity=128;ids=@(1..26);faults=0};remote=(Clone $remote)}}
$inputs=@();$n=0
for($cycle=1;$cycle-le3;$cycle++){foreach($mount in @($true,$false)){for($i=0;$i-lt6;$i++){
 $n++;$verb=if($mount){'mount'}else{'dismount'}
 $inputs+=[pscustomobject]@{label=('six-cycle-'+$cycle+'-'+$verb+'-'+$i);casterId=$roles[$i*2];targetId=$(if($mount){$roles[$i*2+1]}else{$roles[$i*2]});
 abilityGuid=$(if($mount){'f053faad986631688defa003cd7bda0e'}else{'3af2b81f4d72bbb30501fa730fcdf36e'});clicked=$true;nativeCastDelta=1;nativeRefusalDelta=0;commandObject=10000+$n;
 shellControl=('shell-'+$n);generationAtInit=$cycle;resourcesBefore=(Resources 100000000L);resourcesAfter=(Resources 110000000L);selectionAfterInput=$roles[(($i+1)%6)*2];
 shellTerminal=$true;processTerminal=$true;observedExactProcess=$true;explorationResourcesConserved=$true}
}}}
function Point([double]$X){[pscustomobject]@{x=$X;y=0.0;z=0.0}}
function GroundSnapshot([int]$Index,[int]$Command=0){
 [pscustomobject]@{frame=100;gameTicks=10000000L;paused=$true;turnBased=$false;gamepad=$false;simulatingClick=$false;
 serializationBusy=$false;saveWorkerRunning=$false;restorationPending=$false;selectionTruncated=$false;commandsComplete=$true;
 selectionCount=1;selection=@([pscustomobject]@{id=$roles[$Index*2];object=100+$Index*2});actors=@(for($j=0;$j-lt12;$j++){
  $move=$(if($j-eq$Index*2+1){$Command}else{0})
  $raw=@([pscustomobject]@{object=0});if($move-ne0){$raw=@([pscustomobject]@{object=$move;executorId=$roles[$j];executorObject=100+$j;createdByPlayer=$true})}
  [pscustomobject]@{id=$roles[$j];object=100+$j;commandsObject=200+$j;rawCount=1;queueCount=0;rawTruncated=$false;queueTruncated=$false;
   raw=$raw;queue=@();previous=[pscustomobject]@{object=0};publicMoveObject=$move;rawMoveObject=$move}
 })}
}
$moves=@();$ground=@()
for($i=0;$i-lt6;$i++){
 $label='six-ground-'+$i;$command=20000+$i;$selected=$roles[(($i+1)%6)*2]
 $before=World ('quiet-before-'+$label) 6 100;$after=World ('quiet-after-'+$label) 6 130;$before.selection=@($selected);$after.selection=@($selected)
 $t=[pscustomobject]@{lastSampleSeconds=0.0;sampleSequence=0;fileCreated=$false;samplingIntervalSeconds=.1;clockSeconds=10.0};$end=Clone $t;$end.clockSeconds=11.0
 $quiet=[pscustomobject]@{contract='chunk7-optional-capture-quiet-window-v1';label=$label;startFrame=100;endFrame=130;startSeconds=10.0;endSeconds=11.0;
 minimumFrames=12;maximumFrames=1800;minimumSeconds=.25;maximumSeconds=15.0;telemetryBefore=$t;telemetryAfter=$end;measurementBeforePresent=$false;measurementAfterPresent=$false;
 fullyInert=$false;performanceProof=$false;allocationRateProof=$false;remainingObservers=@('guarded-host-heartbeat','exact-native-world-guard','production-lifecycle-records','native-command-settlement-polling','entry-exit-snapshots');gateReleased=$true;before=$before;after=$after}
 $cmdBefore=@($roles|ForEach-Object{[pscustomobject]@{id=$_;moveObject=0}});$cmdAfter=Clone $cmdBefore;($cmdAfter|Where-Object id -CEQ $roles[$i*2+1]).moveObject=$command
 $moves+=[pscustomobject]@{label=$label;selectedId=$roles[$i*2];executorId=$roles[$i*2+1];commandObject=$command;selectionAfterInput=$selected;otherCommandsUnchangedAtInput=$true;
 start=(Point 0);requestedGoal=(Point 2);end=(Point 2);distance=2.0;goalResidual=0.0;commandTerminal=$true;ownerContinuity=$true;commandsBefore=$cmdBefore;commandsAfter=$cmdAfter;optionalCaptureWindow=$quiet}
 $ground+=[pscustomobject]@{contract='chunk7-world-ground-input/v1';label=$label;selectedId=$roles[$i*2];executorId=$roles[$i*2+1];nativeCallEntered=$true;nativeCallReturned=$true;commandValidationPassed=$true;
 before=(GroundSnapshot $i);after=(GroundSnapshot $i $command)}
}
$samples=@()
for($i=0;$i-lt15;$i++){
 $group=[int][Math]::Floor($i/3);$window=$i%3;$active=@(0,1,2,6,0)[$group];$start=1000+$i*200;$finish=$start+120
 $before=World 'six-matched-boundary' $active ($start-30) 3;$after=World 'six-matched-boundary' $active $finish 3
 foreach($w in @($before,$after)){foreach($actor in $w.resources.actors){$actor|Add-Member reactions 1}}
 $ownership=[pscustomobject]@{boundaries=6;failedConstructions=0;owners=$active;active=$active;allocationActors=0;participants=6;preparedOrViewResidue=$active;liveControlBoundaries=0}
 $sample=[pscustomobject]@{label=('six-state-'+$group+'-window-'+$window);warmupFrames=30;sampleFrames=120;startFrame=$start;endFrame=$finish;stopwatchFrequency=1000000L;
 serviceTicks=@(1..120|ForEach-Object{10L});frameSeconds=@(1..120|ForEach-Object{.016});phaseContract='chunk7-update-phases-v1';pairUpdateTicks=@(1..120|ForEach-Object{3L});turnUpdateTicks=@(1..120|ForEach-Object{2L});
 serviceMeanMs=.01;serviceP50Ms=.01;serviceP95Ms=.01;frameP50Ms=16.0;frameP95Ms=16.0;managedHeapBefore=1000L;managedHeapAfter=1000L;managedHeapDelta=0L;forcedCollection=$false;
 collectionsBefore=@(0,0,0);collectionsAfter=@(0,0,0);kmcLogMessages=0L;kmcLogCharacters=0L;ownersBefore=(Clone $ownership);ownersAfter=(Clone $ownership);
 subscriptionsBefore=(Clone $subs);subscriptionsAfter=(Clone $subs);limits='Generated external sampler boundary; no native or performance qualification.'}
 $samples+=[pscustomobject]@{group=$group;window=$window;activePairs=$active;
 before=[pscustomobject]@{world=$before;identityPins=@(1000..1053);activePairs=$active;allocationActors=0};
 after=[pscustomobject]@{world=$after;identityPins=@(1000..1053);activePairs=$active;allocationActors=0};sample=$sample}
}
$stress=[pscustomobject]@{contract='cold-qualified-six-pair-lifecycle-v1';archiveProducerRunId='20261022-c7b-world184-b184s1-sixthhorse-manual';coldQualificationRunId=$selection.sourceRunId;
 fixtureSetupWrites=0;nativeSaveRequests=0;nativeLoadRequests=1;requiredCycles=3;requiredGroundInputs=6;requiredAbilityInputs=36;requiredWindows=15;windowsPerState=3;
 performanceAccepted=$false;requestFreeControlsProven=$false;identityScopeReleased=$true;cycles=@(for($i=1;$i-le3;$i++){World ('six-cycle-'+$i+'-settled') 0 (100*$i)});samples=$samples;
 nativeCastRequests=36;nativeRefusals=0;saveSnapshots=0;semanticRestores=0;presentationRestores=0;riderParents=1;riderParentChildren=16;siblingOrderVerifiedCycles=3}
$o=[pscustomobject]@{inheritedFixture=(Inherited 'admitted');inheritedCleanup=(Inherited 'cleanup');sixPairStress=$stress;
 capability=[pscustomobject]@{archiveNativeEnabled=$true;nativeWriterSchema=3;nativePairCapacity=6;workingSha256=$null;inheritedManualSha256=$selection.sourceArchiveSha256};
 inputs=$inputs;cycleInputs=@();cycles=@();measurements=@();snapshots=@((World 'six-first-native-mounts' 6));movements=$moves;groundInputs=$ground;
 cleanup=[pscustomobject]@{pairResidueAbsent=$true;horseRemoved=$false;inheritedPetsPreserved=$true;controlsSettled=$true;settingsAndSelectionRestored=$true;nativeProcessesSettled=$true;cleanupCustodySettled=$true;
 nativeProgressionRequiresExternalWorkingRestoration=$false;ordinaryRespecProven=$false;nativeSaveWritten=$false;world=(World 'cleanup' 0);selectionBefore=@();selectionAfter=@()}}
$a=[ordered]@{schemaVersion=1;evidenceKind='chunk7-world-evidence';createdAtUtc='2026-10-22T00:00:00Z';status='PASS';errors=@();observations=$o}
foreach($key in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid')){$a[$key]=$request.$key};$artifact=[pscustomobject]$a
$checks=[Collections.Generic.List[object]]::new()
function Check([string]$Name,[scriptblock]$Body,[bool]$Reject=$false){
 $accepted=$true;$message=$null;try{&$Body|Out-Null}catch{$accepted=$false;$message=$_.Exception.Message}
 $ok=$accepted-ne$Reject;$checks.Add([pscustomobject]@{name=$Name;pass=$ok;expectedRefusal=$Reject;error=$message})
 Write-Output ($(if($ok){'PASS '}else{'FAIL '})+$Name+$(if(-not$ok){': '+$message}else{''}))
}
Check 'complete six-pair generated raw evidence under actual reader dispatch' {Assert-KmcChunk7WorldEvidence $request $artifact 'PASS'}
# v2 declares that full inherited custody ran only at sampling-window boundaries.
function New-V2Artifact{$v=Clone $artifact;$v.observations.sixPairStress.contract='cold-qualified-six-pair-lifecycle-v2';$v.observations.sixPairStress|Add-Member measuredFramesCarryFullCustody $false;$v}
Check 'v2 six-pair evidence with boundary-only full custody under actual reader dispatch' {Assert-KmcChunk7WorldEvidence $request (New-V2Artifact) 'PASS'}
Check 'strict six-pair reader refuses v2 claiming full custody on measured frames' {$v=New-V2Artifact;$v.observations.sixPairStress.measuredFramesCarryFullCustody=$true;Assert-KmcChunk7WorldEvidence $request $v 'PASS'} $true
Check 'strict six-pair reader refuses v2 without its custody declaration' {$v=New-V2Artifact;$v.observations.sixPairStress.PSObject.Properties.Remove('measuredFramesCarryFullCustody');Assert-KmcChunk7WorldEvidence $request $v 'PASS'} $true
Check 'strict six-pair reader refuses a mistyped v2 custody declaration' {$v=New-V2Artifact;$v.observations.sixPairStress.measuredFramesCarryFullCustody='false';Assert-KmcChunk7WorldEvidence $request $v 'PASS'} $true
Check 'strict six-pair reader refuses v1 carrying the v2 declaration' {$v=Clone $artifact;$v.observations.sixPairStress|Add-Member measuredFramesCarryFullCustody $false;Assert-KmcChunk7WorldEvidence $request $v 'PASS'} $true
Check 'strict six-pair reader refuses an unknown contract version' {$v=Clone $artifact;$v.observations.sixPairStress.contract='cold-qualified-six-pair-lifecycle-v3';Assert-KmcChunk7WorldEvidence $request $v 'PASS'} $true
foreach($fault in @('missing-sixth-owner','duplicate-mounted-role','wrong-sixth-profile','remote-XP','remote-descriptor','remote-field-alias','changed-source-class','wrong-producer',
 'load2','write1','missing-input','duplicated-shell','foreign-caster','wrong-selected-neighbor','debt-refund','prepared-change','missing-move','neighbor-command','wrong-ground-call',
 'raw-missing-twelfth','raw-missing-command','raw-neighbor-command','raw-truncated','raw-read-failure',
 'missing-window','wrong-resident-count','wrong-active-count','replayed-frame','changed-pin','aliased-pin','missing-twelfth-resource','lost-neighbor-owner','increased-subscriptions',
 'wrong-summary','native-log','fake-allocation-rate','false-performance-PASS','unreleased-pins','cleanup-owner','extra-save-proof',
 'unverified-sibling-order','missing-sibling-order','unparented-riders')){
 $v=Clone $artifact;$b=$v.observations
 switch($fault){
  'missing-sixth-owner'{$b.snapshots[0].owners=@($b.snapshots[0].owners|Select-Object -Skip 1)}
  'duplicate-mounted-role'{$b.snapshots[0].owners[5].riderId=$b.snapshots[0].owners[0].riderId;$b.snapshots[0].owners[5].mountId=$b.snapshots[0].owners[0].mountId}
  'wrong-sixth-profile'{$b.inheritedFixture.pairs[5].profileId='medium-humanoid-horse-v1'}
  'remote-XP'{$b.inheritedFixture.remote.arrangement.actors[0].experience++;$b.inheritedCleanup.remote.arrangement.actors[0].experience++}
  'remote-descriptor'{$b.inheritedCleanup.remote.descriptor=25};'remote-field-alias'{$b.inheritedFixture.remote.object=1}
  'changed-source-class'{$b.inheritedFixture.arrangement.actors[0].classes[0].level++}
  'wrong-producer'{$b.sixPairStress.archiveProducerRunId=$selection.sourceRunId};'load2'{$b.sixPairStress.nativeLoadRequests=2};'write1'{$b.sixPairStress.nativeSaveRequests=1}
  'missing-input'{$b.inputs=@($b.inputs|Select-Object -Skip 1)};'duplicated-shell'{$b.inputs[35].shellControl=$b.inputs[0].shellControl};'foreign-caster'{$b.inputs[35].casterId=$roles[0]}
  'wrong-selected-neighbor'{$b.inputs[35].selectionAfterInput=$roles[2]}
  'debt-refund'{$b.inputs[35].resourcesAfter.actors[11].move=1.0};'prepared-change'{$b.inputs[35].resourcesAfter.actors[11].prepared=$true}
  'missing-move'{$b.movements=@($b.movements|Select-Object -Skip 1)};'neighbor-command'{$b.movements[5].commandsAfter[0].moveObject=9999}
  'wrong-ground-call'{$b.groundInputs[5].nativeCallReturned=$false};'missing-window'{$b.sixPairStress.samples=@($b.sixPairStress.samples|Select-Object -Skip 1)}
  'raw-missing-twelfth'{$b.groundInputs[5].after.actors=@($b.groundInputs[5].after.actors|Select-Object -First 11)}
  'raw-missing-command'{$b.groundInputs[5].after.actors[11].raw[0].object=0}
  'raw-neighbor-command'{$b.groundInputs[5].after.actors[8].publicMoveObject=9999}
  'raw-truncated'{$b.groundInputs[5].after.actors[11].rawTruncated=$true}
  'raw-read-failure'{$b.groundInputs[5].after.actors[11]|Add-Member readError 'native read failed'}
  'wrong-resident-count'{$b.sixPairStress.samples[9].sample.ownersBefore.boundaries=2};'wrong-active-count'{$b.sixPairStress.samples[9].activePairs=2}
  'replayed-frame'{$b.sixPairStress.samples[14].sample.startFrame=1000};'changed-pin'{$b.sixPairStress.samples[14].after.identityPins[53]=2000}
  'aliased-pin'{$b.sixPairStress.samples[9].before.identityPins[53]=$b.sixPairStress.samples[9].before.identityPins[0]}
  'missing-twelfth-resource'{$b.sixPairStress.samples[9].after.world.resources.actors=@($b.sixPairStress.samples[9].after.world.resources.actors|Select-Object -Skip 1)}
  'lost-neighbor-owner'{$b.sixPairStress.samples[9].before.world.owners[0].ownerId='00000000-0000-0000-0000-000000009999'}
  'increased-subscriptions'{$b.sixPairStress.samples[14].after.world.subscriptions.entries[0].type='OtherSubscriber'}
  'wrong-summary'{$b.sixPairStress.samples[9].sample.serviceMeanMs=100.0};'native-log'{$b.sixPairStress.samples[9].sample.kmcLogMessages=1}
  'fake-allocation-rate'{$b.sixPairStress.samples[9].sample|Add-Member nativeAllocation ([pscustomobject]@{fake=$true})}
  'false-performance-PASS'{$b.sixPairStress.performanceAccepted=$true};'unreleased-pins'{$b.sixPairStress.identityScopeReleased=$false}
  'cleanup-owner'{$b.cleanup.world.owners=@($b.snapshots[0].owners[0])};'extra-save-proof'{$b|Add-Member nativeWrite ([pscustomobject]@{fake=$true})}
  'unverified-sibling-order'{$b.sixPairStress.siblingOrderVerifiedCycles=2};'missing-sibling-order'{$b.sixPairStress.PSObject.Properties.Remove('siblingOrderVerifiedCycles')}
  'unparented-riders'{$b.sixPairStress.riderParents=0}
 }
 Check ('strict six-pair reader refuses '+$fault) {Assert-KmcChunk7WorldEvidence $request $v 'PASS'} $true
}
$failed=@($checks|Where-Object {-not$_.pass}).Count
[ordered]@{nativeInvoked=$false;status=$(if($failed){'FAIL'}else{'PASS'});sourceQualificationPerformed=$true;archiveRead=$false;results=$checks.ToArray();limits='Generated native/sampler/input boundary evidence. Actual pinned source gate ran once; subsequent source qualification is substituted, and the actual retained cold observation remains read-only. No game launch or native behavior qualification.'}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
Write-Output ('SIX-PAIR READER PASS='+($checks.Count-$failed)+' FAIL='+$failed)
if($failed){exit 1}
