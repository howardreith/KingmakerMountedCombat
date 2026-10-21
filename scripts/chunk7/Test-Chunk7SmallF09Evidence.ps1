[CmdletBinding()]
param([string]$RepositoryRoot,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/small-f09-evidence-'+[Guid]::NewGuid().ToString('N'))}
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
$fixturePath=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/SmallManualFixture.json'
function Clone($value){$value|ConvertTo-Json -Depth 90 -Compress|ConvertFrom-Json}
function New-F09Synthetic{
 $f=Get-Content -Raw $fixturePath|ConvertFrom-Json;$o=$f.artifact.observations
 $main='44444444-1111-4111-8111-111111111111';$small=$f.request.smallCalibration.manualSource.actorId;$pet=$f.request.smallCalibration.manualSource.petId
 $extra=@(
  [pscustomobject]@{id='77777777-1111-4111-8111-111111111111';blueprint='4016c7db400ab721ff125aef9e65e202';object=4000;descriptor=5000;view=6000;horse=$true},
  [pscustomobject]@{id='88888888-1111-4111-8111-111111111111';blueprint='88888888888888888888888888888888';object=4001;descriptor=5001;view=6001;horse=$false},
  [pscustomobject]@{id='cccccccc-1111-4111-8111-111111111111';blueprint='99999999999999999999999999999999';object=4002;descriptor=5002;view=6002;horse=$false},
  [pscustomobject]@{id='aaaaaaaa-1111-4111-8111-111111111111';blueprint='aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa';object=4003;descriptor=5003;view=6003;horse=$false})
 $states=@($o.inputs|ForEach-Object{$_.before;$_.after})+@($o.movement.before,$o.movement.after)
 foreach($state in $states){
  $mainState=@($state.actors|Where-Object id -CEQ $main)[0];$new=@()
  foreach($actor in $extra){$row=Clone $mainState;$row.id=$actor.id;$row.object=$actor.object;$new+=,$row}
  $state.actors=@($mainState)+@($new)+@($state.actors|Where-Object{$_.id-cne$main})
 }
 $o.allocationTrace.identityRegistry.ids=@($o.allocationTrace.identityRegistry.ids)+@($extra.object)
 $o.allocationTrace.identityRegistry.retainedCount=$o.allocationTrace.identityRegistry.ids.Count
 foreach($actor in $extra){
  $o.manualNativeRoster.actors+=,[pscustomobject]@{id=$actor.id;blueprint=$actor.blueprint;object=$actor.object;descriptor=$actor.descriptor;view=$actor.view}
  $arr=Clone @($o.manualNativeRoster.arrangement.actors|Where-Object actorId -CEQ $main)[0];$arr.actorId=$actor.id;$arr.blueprint=$actor.blueprint;$o.manualNativeRoster.arrangement.actors+=,$arr
  $res=Clone @($o.manualNativeRoster.resources.actors|Where-Object actorId -CEQ $main)[0];$res.actorId=$actor.id;$o.manualNativeRoster.resources.actors+=,$res
 }
 $characters=@($main,$small)+@($extra|Where-Object{-not$_.horse}|ForEach-Object{$_.id})
 $o.before.partyCharacters=$characters;$o.field.partyCharacters=$characters
 $o.manualNativeRoster.arrangement.actors=@($o.manualNativeRoster.arrangement.actors|Sort-Object actorId -CaseSensitive)
 $o.manualNativeRoster.resources.actors=@($o.manualNativeRoster.resources.actors|Sort-Object actorId -CaseSensitive)
 $savedParty=@($o.manualNativeRoster.actors|ForEach-Object{
  $actor=$_;$resource=@($o.manualNativeRoster.resources.actors|Where-Object actorId -CEQ $actor.id)[0]
  [pscustomobject]@{id=$actor.id;blueprint=$actor.blueprint;prepared=$false;inCombat=$false;native=[pscustomobject]@{
    Standard=$resource.standard;Move=$resource.move;Swift=$resource.swift;Initiative=$resource.initiative;Reaction=0;ReactionsRemaining=$resource.reactions}}
 })
 $savedActors=@($o.manualNativeRoster.arrangement.actors|ForEach-Object{$a=$_;[pscustomobject]@{id=$a.actorId;blueprint=$a.blueprint;experience=$a.experience;level=$a.characterLevel;classes=@($a.classes|ForEach-Object{[pscustomobject]@{guid=$_.classGuid;level=$_.level}})}})
 $arrangement=[pscustomobject]@{money=$o.manualNativeRoster.arrangement.money;experienceRatePercent=$o.manualNativeRoster.arrangement.experienceRatePercent;characters=$characters;remote=@();actors=$savedActors}
 $body=[ordered]@{};foreach($p in $o.bodyObservation.PSObject.Properties){$name=$p.Name.Substring(0,1).ToLowerInvariant()+$p.Name.Substring(1);$body[$name]=$p.Value}
 $body.mountBlueprintGuid='4016c7db400ab721ff125aef9e65e202';$body.mountSizeOrdinal=5;$body.avatarMirrored=$false
 $body.avatarLocalScale=[pscustomobject]@{x=1.1;y=1.0;z=0.9};$body.dollEquipmentEntityIds=@('bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb')
 $body.currentEquipmentBlueprints=@('cccccccccccccccccccccccccccccccc')+@($null)*11
 $body.activeVisualEquipmentNames=@('native-small');$body.additionalLimbCount=0
 $body=[pscustomobject]$body
 $o.bodyObservation=Clone $body;foreach($pose in $o.poses){$pose.observation.body=Clone $body}
 $pairA=[pscustomobject]@{riderId=$main;horseId=$extra[0].id;horseBlueprint=$extra[0].blueprint;featureGuid='7db7c50677e39f09feef56f3831fc723';featureSourceGuid='ee63330662126374e8785cc901941ac7';rank=1;reciprocal=$true}
 $pairB=[pscustomobject]@{riderId=$small;horseId=$pet;horseBlueprint='4016c7db400ab721ff125aef9e65e202';featureGuid=$pairA.featureGuid;featureSourceGuid=$o.before.featureSourceGuid;rank=1;reciprocal=$true}
 $write=Clone $o.source.acquiredManual.nativeWrite;$write.fileName='Manual_304_KMC_C7_SMALL_FIXTURE.zks';$write.internalName='KMC_C7_SMALL_FIXTURE'
 $saved=[pscustomobject]@{contract='owner-seed-unmounted-small-fixture-v1';beforeSaveArrangement=(Clone $arrangement);afterSaveArrangement=$arrangement;bodyObservation=(Clone $body);savedBodyObservation=$body;savedWorld=[pscustomobject]@{mainActorId=$main;party=$savedParty};acquiredOwners=@($pairA,$pairB);nativeWrite=$write;cleanup=[pscustomobject]@{custodySettled=$true;sourceUnchanged=$true}}
 $s=$f.request.smallCalibration.manualSource
 $f.request.smallCalibration.PSObject.Properties.Remove('manualSource')
 $f.request.smallCalibration.case='compiled-owner-seed-fixture-small-horse-rt'
 $f.request.smallCalibration|Add-Member fixtureSource ([pscustomobject]@{sourceRunId=$s.sourceRunId;sourceArtifactSha256=$s.sourceArtifactSha256;sourceArchiveSha256=$s.sourceArchiveSha256;coldReceiptSha256=('c'*64);actorId=$small;petId=$pet})
 $f.request.persistenceLoad.fileName=$write.fileName;$f.request.persistenceLoad.internalName=$write.internalName
 $source=$o.source;$source.runId=$s.sourceRunId;$source.archiveLeaf=$write.fileName;$source.archiveName=$write.internalName;$source.sameCandidate=$false;$source.kind='qualified-owner-seed-f09-small-fixture-manual'
 $source|Add-Member coldReceiptSha256 ('c'*64);$source|Add-Member fixtureSetupOnly $true
 $source|Add-Member producerCommit '784a6296f9e0d91c44ad25dda826db57f609e929'
 $source|Add-Member producerDllSha256 '8f7f89769c82e0ac15f5d972441d29e68d6f67ff4ad942fe55f25ece12047d89'
 $source|Add-Member producerDllMvid 'bf98be50-898a-41b3-88f0-78959a0ea1a3'
 $source.bodySelection=[pscustomobject]@{smallRaceGuid=$body.raceGuid;smallPresetGuid=$body.presetGuid;skeletonName=$body.skeletonName;gender=$body.gender;leftHanded=$body.leftHanded}
 $source.PSObject.Properties.Remove('acquiredManual');$source|Add-Member fixtureManual $saved
 $o.manualNativeRoster.mainActorId=$main
 $prior=[pscustomobject]@{actorId=$main;petId=$extra[0].id;actorObject=30;petObject=4000;actorDescriptor=130;petDescriptor=5000;actorView=230;petView=6000;featureObject=501;sourceObject=502;rankObject=503;actorBlueprint='55555555555555555555555555555555';petBlueprint=$pairA.horseBlueprint;featureGuid=$pairA.featureGuid;sourceGuid=$pairA.featureSourceGuid;rank=1;reciprocal=$true;resources=@($o.manualNativeRoster.resources.actors|Where-Object{$_.actorId-cin@($main,$extra[0].id)})}
 $o|Add-Member inheritedFixturePairBefore $prior;$o|Add-Member inheritedFixturePairAfter (Clone $prior)
 $admitted=Clone $prior;foreach($row in $admitted.resources){$row.prepared=$true;$row.initiative=if($row.actorId-ceq$main){7}else{5}}
 $o|Add-Member inheritedFixturePairNativeCombatAdmission ([pscustomobject]@{stage=3;frame=6;gameTicks=100;targetId=$o.targetId;targetInCombat=$true;ownerInCombat=$true;horseInCombat=$true;ownerCommandsEmpty=$true;horseCommandsEmpty=$true;before=(Clone $prior);after=$admitted})
 foreach($state in @($o.inputs|ForEach-Object{$_.before;$_.after})+@($o.movement.before,$o.movement.after)){foreach($owner in @($state.owners)){$owner.riderId=$small.Replace('-','');$owner.mountId=$pet.Replace('-','')}}
 $o.inputs[0].generation=0
 $o.selection=Clone $f.request.smallCalibration
 $f
}
function New-F09PresentationSynthetic{
 $f=New-F09Synthetic;$f.request.smallCalibration.case='compiled-owner-seed-fixture-small-horse-presentation-rt'
 $o=$f.artifact.observations;$o.selection.case=$f.request.smallCalibration.case
 $s=$f.request.smallCalibration.fixtureSource;$rows=@();$n=9000
 $castEvents=@();$castShells=@();$costEvents=@($o.allocationTrace.events)
 $slots=@(1..5|ForEach-Object{[pscustomobject]@{owner=$s.actorId;slot=(9500+$_);item=0;blueprint=$null;holdingSlot=0;count=$null;charges=$null}})
 foreach($spec in @(@('scroll-friendly','cd635d5720937b044a354dba17abad8d',$s.petId),@('potion-self','d52566ae8cbe8dc4dae977ef51c27d91',$s.actorId))){
  $n++;$item=[pscustomobject]@{item=$n;blueprint=$spec[1];count=2;exactSlot=$true;holdingSlot=(9500+$rows.Count+1)}
  $after=Clone $item;$after.count=1
  $beforeSpend=[pscustomobject]@{kind='item-spend-before';identity=$n;actor=$s.actorId;blueprint=$spec[1];count=2;result=$null}
  $afterSpend=[pscustomobject]@{kind='item-spend-after';identity=$n;actor=$s.actorId;blueprint=$spec[1];count=1;result=$true}
  $rootAbilityIdentity=$n+300
  $rootSpell=[pscustomobject]@{identity=$rootAbilityIdentity;blueprint=('a'*32);caster=$s.actorId;sourceItem=$n;sourceItemBlueprint=$spec[1]}
  $beforeCost=Clone $costEvents[0];$afterCost=Clone $costEvents[1]
  foreach($event in @($beforeCost,$afterCost)){$event.command=($n+100);$event.commandActor=$s.actorId;$event.commandType='Kingmaker.UnitLogic.Commands.UnitUseAbility';$event.state.actor=$s.actorId;$event.state.actorObject=$o.before.actorObject;$event|Add-Member ignoreCooldown $false -Force}
  $beforeCost.sequence=$costEvents.Count+1;$afterCost.sequence=$costEvents.Count+2
  $beforeCost.frame=$o.poses[5].observation.frame;$afterCost.frame=$beforeCost.frame
  $costOffset=$costEvents.Count;$eventOffset=$castEvents.Count
  $nativeCosts=@($beforeCost,$afterCost)
  $nativeEvents=@([pscustomobject]@{kind='start-before';identity=($n+100);actor=$s.actorId;shell=[pscustomobject]@{spell=(Clone $rootSpell)}},
   [pscustomobject]@{kind='cast-after';actor=$s.actorId;target=$spec[2];ability=(Clone $rootSpell)},$beforeSpend,$afterSpend)
  $castShells+=,[pscustomobject]@{identity=($n+100);executor=$s.actorId;spell=(Clone $rootSpell);ignoreCooldown=$false;
   started=$true;acted=$true;finished=$true;result='Success';processEnded=$true}
  $followupShell=0;$followupGuid=$null
  if($spec[0]-ceq'scroll-friendly'){
   $followupShell=$n+200;$followupGuid='47808d23c67033d4bbab86a1070fd62f'
   $childBefore=Clone $afterCost;$childAfter=Clone $afterCost
   foreach($event in @($childBefore,$childAfter)){$event.command=$followupShell;$event.ignoreCooldown=$true}
   $childBefore.boundary='cost-before';$childAfter.boundary='cost-after'
   $childBefore.sequence=$afterCost.sequence+1;$childAfter.sequence=$afterCost.sequence+2
   $nativeCosts+=@($childBefore,$childAfter)
   $childSpell=[pscustomobject]@{identity=($n+400);blueprint=$followupGuid;caster=$s.actorId;sourceItem=0;sourceItemBlueprint=$null}
   $nativeEvents+=@([pscustomobject]@{kind='start-before';identity=$followupShell;actor=$s.actorId;shell=[pscustomobject]@{spell=(Clone $childSpell);ignoreCooldown=$true}},
    [pscustomobject]@{kind='action-before';identity=$followupShell;actor=$s.actorId;shell=[pscustomobject]@{spell=(Clone $childSpell);ignoreCooldown=$true}},
    [pscustomobject]@{kind='cast-after';actor=$s.actorId;target=$s.petId;ability=(Clone $childSpell)},
    [pscustomobject]@{kind='heal';actor=$s.actorId;target=$s.petId;value=1})
   $castShells+=,[pscustomobject]@{identity=$followupShell;executor=$s.actorId;spell=(Clone $childSpell);
    ignoreCooldown=$false;started=$true;acted=$true;finished=$true;result='Success';processEnded=$true}
   $o.allocationTrace.identityRegistry.ids+=$followupShell
  }
  $costEvents+=$nativeCosts;$castEvents+=$nativeEvents
  $o.allocationTrace.identityRegistry.ids+=($n+100)
  $rows+=,[pscustomobject]@{kind=$spec[0];actorId=$s.actorId;targetId=$spec[2];itemBlueprint=$spec[1];abilityGuid=('a'*32);
   clicked=$true;terminal=$true;processTerminal=$true;nativeResult='Success';selectionAfterInput=$o.manualNativeRoster.mainActorId;shell=($n+100);
   nativeFollowupShell=$followupShell;nativeFollowupAbilityGuid=$followupGuid;
   woundBefore=0;woundAtInput=1;woundAfter=0;beforeItem=$item;afterItem=$after;
   beforePose=(Clone $o.poses[5].observation);activePose=(Clone $o.poses[5].observation);afterPose=(Clone $o.poses[5].observation);
   eventOffset=$eventOffset;costOffset=$costOffset;nativeEvents=$nativeEvents;nativeCosts=$nativeCosts}
 }
 $o.allocationTrace.events=$costEvents;$o.allocationTrace.identityRegistry.retainedCount=$o.allocationTrace.identityRegistry.ids.Count
 $items=@($rows|ForEach-Object{$row=$_;[pscustomobject]@{blueprint=$row.itemBlueprint;rider=$s.actorId;requestedCount=2;slot=$row.beforeItem.holdingSlot;
  slotOriginallyEmpty=$true;stacked=(Clone $row.beforeItem);beforeCleanup=(Clone $row.afterItem);
  removalPostconditions=[pscustomobject]@{item=$row.beforeItem.item;collection=0;holdingSlot=0;exactInventoryResident=$false};
  afterCleanup=[pscustomobject]@{item=$row.beforeItem.item;exactInventoryResident=$false};
  quickSlotsBefore=(Clone $slots);quickSlotsAfter=(Clone $slots);ownedItemReleased=$true;disposed=$true;slotRestored=$true;noOwnedItemResident=$true}})
 $o|Add-Member presentation ([pscustomobject]@{contract='small-native-item-presentation-v1';fixtureSetupOnly=$true;spellSlotWrites=0;
  nativePreparationManufactured=$false;inputs=$rows;originalSlots=[pscustomobject]@{restored=$true;before=$slots;after=(Clone $slots)};items=$items;
  castTrace=[pscustomobject]@{dropped=0;faults=0;events=$castEvents;shells=$castShells};released=$true})
 $f
}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
$script:pass=0;$script:fail=0;$watch=[Diagnostics.Stopwatch]::StartNew()
function Good([string]$name,[scriptblock]$action){try{&$action;$script:pass++;Write-Host ('PASS '+$name)}catch{$script:fail++;Write-Host ('FAIL '+$name+': '+$_+' '+$_.ScriptStackTrace)}}
function Bad([string]$name,[scriptblock]$action){try{&$action;$script:fail++;Write-Host ('FAIL accepted '+$name)}catch{if($_.ScriptStackTrace -notmatch 'Assert-KmcChunk7Small'){$script:fail++;Write-Host ('FAIL mutation '+$name+': '+$_+' '+$_.ScriptStackTrace)}else{$script:pass++;Write-Host ('PASS refuses '+$name)}}}
Good 'F09 synthetic seven-actor source and shared native mounted reader' { $f=New-F09Synthetic;Assert-KmcChunk7SmallEvidence $f.request $f.artifact }
Good 'F09 source decimal zero equals exact observed integer zero' { $f=New-F09Synthetic;$f.artifact.observations.source.fixtureManual.savedWorld.party[0].native.Initiative=[decimal]0.0;Assert-KmcChunk7SmallEvidence $f.request $f.artifact }
Good 'separate presentation case retains qualified F09 source selection' { $f=New-F09Synthetic;$f.request.smallCalibration.case='compiled-owner-seed-fixture-small-horse-presentation-rt';Assert-KmcChunk7SmallSelection $f.request }
Good 'F09 presentation reader binds two native item spends and costs' { $f=New-F09PresentationSynthetic;Assert-KmcChunk7SmallEvidence $f.request $f.artifact }
Bad 'presentation reader refuses scroll without native sticky-touch shell' { $f=New-F09PresentationSynthetic;$f.artifact.observations.presentation.inputs[0].nativeFollowupShell=0;Assert-KmcChunk7SmallEvidence $f.request $f.artifact }
Bad 'presentation reader refuses paid sticky-touch delivery' { $f=New-F09PresentationSynthetic;$r=$f.artifact.observations.presentation.inputs[0];$r.nativeCosts[3].state.standard=9;$f.artifact.observations.allocationTrace.events[$r.costOffset+3].state.standard=9;Assert-KmcChunk7SmallEvidence $f.request $f.artifact }
Bad 'presentation reader refuses foreign native delivery spell' { $f=New-F09PresentationSynthetic;$f.artifact.observations.presentation.castTrace.shells[1].spell.blueprint='b'*32;Assert-KmcChunk7SmallEvidence $f.request $f.artifact }
Bad 'presentation reader refuses delivery lacking start-time cooldown waiver' { $f=New-F09PresentationSynthetic;$r=$f.artifact.observations.presentation.inputs[0];$r.nativeEvents[4].shell.ignoreCooldown=$false;$f.artifact.observations.presentation.castTrace.events[$r.eventOffset+4].shell.ignoreCooldown=$false;Assert-KmcChunk7SmallEvidence $f.request $f.artifact }
Bad 'presentation reader refuses delivery lacking action-time cooldown waiver' { $f=New-F09PresentationSynthetic;$r=$f.artifact.observations.presentation.inputs[0];$r.nativeEvents[5].shell.ignoreCooldown=$false;$f.artifact.observations.presentation.castTrace.events[$r.eventOffset+5].shell.ignoreCooldown=$false;Assert-KmcChunk7SmallEvidence $f.request $f.artifact }
Bad 'presentation reader refuses wrong sticky-touch target' { $f=New-F09PresentationSynthetic;$r=$f.artifact.observations.presentation.inputs[0];$r.nativeEvents[6].target=$r.actorId;$f.artifact.observations.presentation.castTrace.events[$r.eventOffset+6].target=$r.actorId;Assert-KmcChunk7SmallEvidence $f.request $f.artifact }
Bad 'presentation reader refuses detached delivery ability identity' { $f=New-F09PresentationSynthetic;$r=$f.artifact.observations.presentation.inputs[0];$r.nativeEvents[6].ability.identity++;$f.artifact.observations.presentation.castTrace.events[$r.eventOffset+6].ability.identity++;Assert-KmcChunk7SmallEvidence $f.request $f.artifact }
Bad 'presentation reader refuses delivery before paid scroll cast' { $f=New-F09PresentationSynthetic;$r=$f.artifact.observations.presentation.inputs[0];$events=@($r.nativeEvents);$r.nativeEvents=@($events[4],$events[5],$events[6],$events[0],$events[1],$events[2],$events[3],$events[7]);for($k=0;$k-lt$r.nativeEvents.Count;$k++){$f.artifact.observations.presentation.castTrace.events[$r.eventOffset+$k]=$r.nativeEvents[$k]};Assert-KmcChunk7SmallEvidence $f.request $f.artifact }
Bad 'presentation reader refuses missing native rider cost' { $f=New-F09PresentationSynthetic;$f.artifact.observations.presentation.inputs[0].nativeCosts=@();Assert-KmcChunk7SmallEvidence $f.request $f.artifact }
Bad 'presentation reader refuses original slot drift' { $f=New-F09PresentationSynthetic;$f.artifact.observations.presentation.originalSlots.restored=$false;Assert-KmcChunk7SmallEvidence $f.request $f.artifact }
Bad 'presentation reader refuses spend from another item' { $f=New-F09PresentationSynthetic;$r=$f.artifact.observations.presentation.inputs[0];$r.nativeEvents[3].identity++;$f.artifact.observations.presentation.castTrace.events[$r.eventOffset+3].identity++;Assert-KmcChunk7SmallEvidence $f.request $f.artifact }
Bad 'presentation reader refuses raw trace mismatch' { $f=New-F09PresentationSynthetic;$f.artifact.observations.presentation.inputs[0].nativeEvents[3].count=2;Assert-KmcChunk7SmallEvidence $f.request $f.artifact }
Bad 'presentation reader refuses callback with no action spend' { $f=New-F09PresentationSynthetic;$r=$f.artifact.observations.presentation.inputs[0];$r.nativeCosts[1].state.standard=0;$f.artifact.observations.allocationTrace.events[$r.costOffset+1].state.standard=0;Assert-KmcChunk7SmallEvidence $f.request $f.artifact }
Bad 'presentation reader refuses extra pair action cost' { $f=New-F09PresentationSynthetic;$f.artifact.observations.presentation.inputs[0].nativeCosts+=,(Clone $f.artifact.observations.presentation.inputs[0].nativeCosts[1]);Assert-KmcChunk7SmallEvidence $f.request $f.artifact }
Bad 'presentation reader refuses foreign quick-slot occupant' { $f=New-F09PresentationSynthetic;$f.artifact.observations.presentation.originalSlots.after[0].item=999;Assert-KmcChunk7SmallEvidence $f.request $f.artifact }
Bad 'presentation reader refuses missing active pose' { $f=New-F09PresentationSynthetic;$f.artifact.observations.presentation.inputs[0].PSObject.Properties.Remove('activePose');Assert-KmcChunk7SmallEvidence $f.request $f.artifact }
Bad 'presentation reader refuses unhealthy active pose' { $f=New-F09PresentationSynthetic;$f.artifact.observations.presentation.inputs[0].activePose.runtime.healthy=$false;Assert-KmcChunk7SmallEvidence $f.request $f.artifact }
$cases=[ordered]@{
 'old acquired source alias'={param($f)$f.request.smallCalibration|Add-Member manualSource ([pscustomobject]@{})}
 'forged same candidate'={param($f)$f.artifact.observations.source.sameCandidate=$true}
 'wrong producer identity'={param($f)$f.artifact.observations.source.producerDllSha256='a'*64}
 'cold receipt altered'={param($f)$f.artifact.observations.source.coldReceiptSha256='a'*64}
 'foreign manual name'={param($f)$f.request.persistenceLoad.internalName='KMC_C7_ACQUIRED_FIELD'}
 'source Small pair changed'={param($f)$f.artifact.observations.source.fixtureManual.acquiredOwners[1].horseId='bbbbbbbb-1111-4111-8111-111111111111'}
 'cold Small body changed'={param($f)$f.artifact.observations.bodyObservation.RaceGuid='a'*32}
 'cold reaction debt changed'={param($f)$f.artifact.observations.manualNativeRoster.resources.actors[0].reactions++}
 'cold initiative value changed'={param($f)$f.artifact.observations.manualNativeRoster.resources.actors[0].initiative=1}
 'cold tiny observed debt'={param($f)$f.artifact.observations.manualNativeRoster.resources.actors[0].standard=[double]1e-29}
 'cold tiny saved debt'={param($f)$f.artifact.observations.source.fixtureManual.savedWorld.party[0].native.Standard=[double]1e-29}
 'cold fractional reaction debt'={param($f)$f.artifact.observations.manualNativeRoster.resources.actors[0].reactions=[decimal]::Parse('1.0000000000000000000000000001',[Globalization.CultureInfo]::InvariantCulture)}
 'cold actor ID singleton'={param($f)$f.artifact.observations.manualNativeRoster.actors[0].id=@($f.artifact.observations.manualNativeRoster.actors[0].id)}
 'cold class GUID singleton'={param($f)$f.artifact.observations.manualNativeRoster.arrangement.actors[0].classes[0].classGuid=@($f.artifact.observations.manualNativeRoster.arrangement.actors[0].classes[0].classGuid)}
 'neighboring pair changed'={param($f)$f.artifact.observations.inheritedFixturePairAfter.petObject++}
 'neighboring combat admission missing'={param($f)$f.artifact.observations.PSObject.Properties.Remove('inheritedFixturePairNativeCombatAdmission')}
 'neighboring combat action grant'={param($f)$f.artifact.observations.inheritedFixturePairNativeCombatAdmission.after.resources[0].standard=1}
 'neighboring combat false entry'={param($f)$f.artifact.observations.inheritedFixturePairNativeCombatAdmission.targetInCombat=$false}
 'neighboring combat late stage'={param($f)$f.artifact.observations.inheritedFixturePairNativeCombatAdmission.stage=8}
 'neighboring combat before movement'={param($f)$f.artifact.observations.inheritedFixturePairNativeCombatAdmission.frame=1}
 'neighboring combat after primary'={param($f)$f.artifact.observations.inheritedFixturePairNativeCombatAdmission.frame=100}
 'neighboring combat final not restored'={param($f)$f.artifact.observations.inheritedFixturePairAfter=Clone $f.artifact.observations.inheritedFixturePairNativeCombatAdmission.after}
 'compact mounted owner changed'={param($f)$f.artifact.observations.inputs[0].after.owners[0].riderId='a'*32}
 'compact mounted rider singleton'={param($f)$v=$f.artifact.observations.inputs[0].after.owners[0].riderId;$f.artifact.observations.inputs[0].after.owners[0].riderId=@($v)}
 'compact mounted horse singleton'={param($f)$v=$f.artifact.observations.inputs[0].after.owners[0].mountId;$f.artifact.observations.inputs[0].after.owners[0].mountId=@($v)}
 'mounted owner state singleton'={param($f)$f.artifact.observations.inputs[0].after.owners[0].state=@('Active')}
 'mounted owner nested singleton'={param($f)$v=$f.artifact.observations.inputs[0].after.owners[0];$f.artifact.observations.inputs[0].after.owners=@(,@($v))}
 'pre-mount generation changed'={param($f)$f.artifact.observations.inputs[0].generation=1}
 'neighboring pair labels jointly forged'={param($f)$f.artifact.observations.inheritedFixturePairBefore.actorObject=999;$f.artifact.observations.inheritedFixturePairAfter.actorObject=999}
 'profile inventory singleton status'={param($f)$f.artifact.observations.profileInventory.captureStatus=@('PASS')}
 'profile inventory singleton MVID'={param($f)$f.artifact.observations.profileInventory.gameplayAssemblyMvid=@('07fa1e4d-8618-41b3-9b8d-faa17d3b26f7')}
 'profile inventory singleton rider ID'={param($f)$f.artifact.observations.profileInventory.party[0].id=@($f.artifact.observations.profileInventory.party[0].id)}
 'preparation observed'={param($f)$f.artifact.observations.coldNoPreparation.events=@([pscustomobject]@{kind='prepare-before'})}
 'mounted action removed'={param($f)$f.artifact.observations.inputs=@($f.artifact.observations.inputs|Select-Object -Skip 1)}
 'presentation case cannot borrow primary-only PASS'={param($f)$f.request.smallCalibration.case='compiled-owner-seed-fixture-small-horse-presentation-rt';$f.artifact.observations.selection.case=$f.request.smallCalibration.case}
 'primary-only case cannot claim presentation'={param($f)$f.artifact.observations|Add-Member presentation ([pscustomobject]@{contract='small-native-item-presentation-v1'})}
}
foreach($case in $cases.GetEnumerator()){Bad $case.Key {$f=New-F09Synthetic;&$case.Value $f;Assert-KmcChunk7SmallEvidence $f.request $f.artifact}}
$watch.Stop()
[ordered]@{pass=$script:pass;fail=$script:fail;seconds=$watch.Elapsed.TotalSeconds;readerSha256=(Get-FileHash -LiteralPath (Join-Path $RepositoryRoot 'scripts/runtime/Chunk7SmallCalibrationEvidence.ps1')).Hash.ToLowerInvariant();fixtureSha256=(Get-FileHash -LiteralPath $fixturePath).Hash.ToLowerInvariant();nativeProof=$false;archiveReads=0;testBoundary='Strict reader; generated seven-actor F09 source and action evidence are explicitly synthetic'}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
Write-Host ('TOTAL PASS='+$script:pass+' FAIL='+$script:fail)
if($script:fail){throw 'F09 Small evidence checks failed.'}
