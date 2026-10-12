[CmdletBinding()]
param([string]$RepositoryRoot,[string]$ReaderDirectory,[string]$FieldTestSource,[string]$FieldReaderDirectory,[switch]$FixturesOnly)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$ReaderDirectory){$ReaderDirectory=Join-Path $PSScriptRoot '../runtime'}
if(-not$FieldTestSource){$FieldTestSource=Join-Path $PSScriptRoot 'Test-Chunk7FieldEvidence.ps1'}
if(-not$FieldReaderDirectory){$FieldReaderDirectory=$ReaderDirectory}
$smallReader=$ReaderDirectory;$smallOnly=$FixturesOnly.IsPresent
. $FieldTestSource -RepositoryRoot $RepositoryRoot -ReaderDirectory $FieldReaderDirectory -FixturesOnly
. (Join-Path $smallReader 'Chunk7SmallCalibrationEvidence.ps1')
function New-SmallFixture {
 $f=New-FieldFixture;$r=$f.request;$o=$f.artifact.observations;$source=$r.ordinaryField;$r.PSObject.Properties.Remove('ordinaryField')
 $sel=[pscustomobject]@{case='compiled-small-horse-rt';source=$source;profileId='small-synthetic-horse-v1';nativeInventorySha256='1'*64;nativeCalibrationSha256='2'*64}
 $r.scenario='chunk7-small-profile-calibration';$r|Add-Member -NotePropertyName smallCalibration -NotePropertyValue $sel
 $f.artifact.scenario=$r.scenario;$f.artifact.evidenceKind='chunk7-small-calibration-evidence';$o.selection=Copy-O $sel
 $pose=[pscustomobject]@{Id=$sel.profileId;PelvisBoneName='Pelvis';LeftLeg=[pscustomobject]@{ThighBoneName='L_Up_leg';LowerLegBoneName='L_leg';FootBoneName='L_foot'};RightLeg=[pscustomobject]@{ThighBoneName='R_Up_leg';LowerLegBoneName='R_leg';FootBoneName='R_foot'}}
 $o|Add-Member -NotePropertyName declaration -NotePropertyValue ([pscustomobject]@{profileId=$sel.profileId;nativeInventorySha256=$sel.nativeInventorySha256;nativeCalibrationSha256=$sel.nativeCalibrationSha256;qualificationStatus='IN PROGRESS';pose=$pose})
 foreach($k in @('diagnosticsOffProven','humanVisualAcceptance','nativePreparationManufactured','resourceFieldsWritten','poseQualified')){$o|Add-Member -NotePropertyName $k -NotePropertyValue $false}
 $o|Add-Member -NotePropertyName representativeNativeActionsObserved -NotePropertyValue $true;$o|Add-Member -NotePropertyName breadthQualification -NotePropertyValue 'IN PROGRESS'
 $ids=@('b6628a77-4962-47a4-a17c-88d9836fc9d5','d79a4f6c-b74e-4868-95bd-533899131acb','d17c8fd0-6627-40c4-bf9a-024cf001c1f7',$source.actorId,$source.petId)
 $actors=@();$id=30;foreach($actor in $ids){$actors+=,[pscustomobject]@{id=$actor;object=$id++;standard=0.0;move=0.0;swift=0.0;reactionCooldown=0.0;reactions=0;reactionAllowance=1;position=@(0.0,0.0,0.0)}}
 $state=[pscustomobject]@{frame=1;gameTicks=100L;actors=$actors;owners=@()}
 $owner=[pscustomobject]@{id='aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee';riderId=$source.actorId;mountId=$source.petId;generation=1L;state='Active'}
 $kinds=@('native-mount','rider-primary','mount-primary','native-dismount');$abilities=@('f053faad986631688defa003cd7bda0e','27364df661b3c121eabb97a31aa73a83','f88a50d6fdbebbd709c3e323d2f52f5e','3af2b81f4d72bbb30501fa730fcdf36e')
 $target='99999999-1111-4111-8111-111111111111';$o|Add-Member -NotePropertyName targetId -NotePropertyValue $target
 $o|Add-Member -NotePropertyName target -NotePropertyValue ([pscustomobject]@{id=$target;object=401;blueprint='e7aa96d15a45238438ae4cfb476f6bb9';groupId=('KMC.RuntimeHostile.'+$r.runId+':small-profile-target');nativeDiagnosticFixture=$true;playerResourceWrites=0;initiativeWrites=0})
 $rows=@();$events=@();for($i=0;$i-lt4;$i++){
  $b=Copy-O $state;$b.frame=2+$i*10;$a=Copy-O $b;$a.frame++
  if($i-gt0){$b.owners=@(Copy-O $owner)};if($i-lt3){$a.owners=@(Copy-O $owner)}
  $row=[pscustomobject]@{kind=$kinds[$i];casterId=$source.actorId;targetId=$(if($i-eq0){$source.petId}elseif($i-eq3){$source.actorId}else{$target});abilityGuid=$abilities[$i];selectionAfterInput=$ids[0];clicked=$true;terminal=$true;processTerminal=$true;castDelta=1L;refusalDelta=0L;shell=100+$i;generation=1L;before=$b;after=$a}
  if($i-eq1-or$i-eq2){$actor=if($i-eq1){$source.actorId}else{$source.petId};$ev=@();foreach($boundary in @('cost-before','cost-after')){$ev+=,[pscustomobject]@{boundary=$boundary;sequence=$events.Count+$ev.Count+1;frame=$b.frame;command=200+$i;commandActor=$actor;commandType='KingmakerMountedCombat.Integration.MountedPairAttackCommand';acted=$true;actionType='Standard';nativeTurnBased=$false;state=[pscustomobject]@{actor=$actor;move=0.0;swift=0.0;reactionCooldown=0.0;reactions=0;standard=$(if($boundary-ceq'cost-before'){0.0}else{6.0})}}}
   $row|Add-Member -NotePropertyName traceOffset -NotePropertyValue $events.Count;$events+=@($ev)
   $row|Add-Member -NotePropertyName nativeEvents -NotePropertyValue $ev;$row|Add-Member -NotePropertyName resourceOwnerId -NotePropertyValue $actor
   $row|Add-Member -NotePropertyName outcome -NotePropertyValue ([pscustomobject]@{actorId=$actor;resourceOwnerId=$actor;targetId=$target;result='Success';nativeAttackRule=$true;completedAttacks=1;singleAttack=$true})
  };$rows+=,$row
 }
 $o|Add-Member -NotePropertyName inputs -NotePropertyValue $rows
 $hooks=@();foreach($token in @('06000C3C','06009120','0600838F','06000C46','0600939D')){$hooks+=,[pscustomobject]@{method='Native.ExactObservedMethod';token=$token;moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'}}
 foreach($event in $events){$event.state|Add-Member -NotePropertyName actorObject -NotePropertyValue $(if($event.state.actor-ceq$source.actorId){33}else{34})}
 foreach($boundary in @('round-before','combat-clear-after')){$events+=,[pscustomobject]@{boundary=$boundary;sequence=$events.Count+1;frame=33;command=0;state=[pscustomobject]@{actor=$target;actorObject=401}}}
 $o|Add-Member -NotePropertyName allocationTrace -NotePropertyValue ([pscustomobject]@{events=$events;dropped=0;observationErrors=0;observerHooks=$hooks;identityRegistry=[pscustomobject]@{contract='retained-native-object-identity-v1';capacity=16000;ids=@(30,31,32,33,34,77,100,101,102,103,201,202,401);retainedCount=13;released=$false;faults=0}})
 $mb=Copy-O $state;$mb.frame=4;$ma=Copy-O $state;$ma.frame=5;$ma.actors[4].position=@(0.0,0.0,2.0)
 $mb.owners=@(Copy-O $owner);$ma.owners=@(Copy-O $owner)
 $o|Add-Member -NotePropertyName movement -NotePropertyValue ([pscustomobject]@{before=$mb;after=$ma;clicked=$true;terminal=$true;actualDistance=2.0;casterId=$source.actorId;executorId=$source.petId;selectionAfterInput=$ids[0];command=77})
 $v=[pscustomobject]@{x=0.0;y=0.0;z=0.0};$bones=@();$anchors=@();$n=70
 foreach($name in @('Pelvis','L_Up_leg','L_leg','L_foot','R_Up_leg','R_leg','R_foot','Chest','L_Stirrup','R_Stirrup')){
  $node=[pscustomobject]@{name=$name;instanceId=$n++;parentId=42;localPosition=(Copy-O $v);localEuler=(Copy-O $v);localScale=(Copy-O $v);worldPosition=(Copy-O $v);worldScale=(Copy-O $v)}
  if($bones.Count-lt7){$bones+=,$node}else{$anchors+=,$node}
 }
 $roots=@{riderRoot=(Copy-O $bones[0]);horseRoot=(Copy-O $anchors[0])};$roots.riderRoot.instanceId=80;$roots.horseRoot.instanceId=81
 $frames=@(1,3,4,5,13,23,33);$poses=@();$frame=0;foreach($phase in @('unmounted-field','mounted-idle','mounted-moving','mounted-arrived','rider-primary-complete','mount-primary-complete','dismounted-field')){
  $p=[pscustomobject]@{frame=$frames[$frame++];riderId=$source.actorId;mountId=$source.petId;profileId=$sel.profileId;inventorySha256=$sel.nativeInventorySha256;declarationSha256=$sel.nativeCalibrationSha256;coordinateConvention='native-Unity';qualificationStatus='IN PROGRESS';visualAcceptanceProven=$false;body=(Copy-O $o.bodyObservation);riderBones=(Copy-O $bones);horseAnchors=(Copy-O $anchors);pelvisFromChestMountLocal=(Copy-O $v);leftFootToLeftStirrup=.1;leftFootToRightStirrup=.6;rightFootToLeftStirrup=.6;rightFootToRightStirrup=.1;leftUpperLength=.4;leftLowerLength=.4;rightUpperLength=.4;rightLowerLength=.4;runtime=[pscustomobject]@{profileId=$sel.profileId;poseProfileId=$sel.profileId;configured=$true;healthy=$true;frameApplied=$true;applications=10L;footClamps=0L;componentCount=1;boneCount=7;maximumFootResidual=.00001;maximumKneeResidual=.00001;maximumSegmentResidual=.00001;failure=$null}}
  foreach($rootName in @('riderRoot','horseRoot')){$p|Add-Member -NotePropertyName $rootName -NotePropertyValue (Copy-O $roots[$rootName])}
  foreach($name in @('leftFootToLeftStirrup','leftFootToRightStirrup','rightFootToLeftStirrup','rightFootToRightStirrup','leftUpperLength','leftLowerLength','rightUpperLength','rightLowerLength')){$p.$name=0.0}
  $poses+=,[pscustomobject]@{phase=$phase;observation=$p}
 };$o|Add-Member -NotePropertyName poses -NotePropertyValue $poses
 foreach($k in @('nativeCommandsSettled','poseResidueAbsent','targetRemoved','observerReleased','modeRestored','pairAiRestored')){$o.cleanup|Add-Member -NotePropertyName $k -NotePropertyValue $true};$o.cleanup|Add-Member -NotePropertyName ownerCount -NotePropertyValue 0
 return $f
}
if($smallOnly){return}
$script:passes=0
Good 'synthetic unqualified compiled-profile native evidence has no automatic visual promotion' {$f=New-SmallFixture;Assert-KmcChunk7SmallEvidence $f.request $f.artifact}
$cases=[ordered]@{
 'unknown case'={param($f)$f.request.smallCalibration.case='any'}
 'Medium profile substitution'={param($f)$f.request.smallCalibration.profileId='medium-humanoid-horse-v1'}
 'numeric declaration hash'={param($f)$f.request.smallCalibration.nativeCalibrationSha256=12}
 'foreign body provenance'={param($f)$f.artifact.observations.declaration.nativeInventorySha256='3'*64}
 'prior visual promotion'={param($f)$f.artifact.observations.declaration.qualificationStatus='PASS'}
 'dynamic pose field'={param($f)$f.request.smallCalibration|Add-Member -NotePropertyName pose -NotePropertyValue @{x=1}}
 'native load count text'={param($f)$f.artifact.observations.authority.admittedNativeLoads='1'}
 'native write'={param($f)$f.artifact.observations.cleanup.nativeWrites=1}
 'body replaced after mount'={param($f)$f.artifact.observations.poses[1].observation.body.RaceGuid='9'*32}
 'wrong production pose'={param($f)$f.artifact.observations.poses[1].observation.runtime.poseProfileId='medium-humanoid-horse-v1'}
 'mounted owner absent'={param($f)$f.artifact.observations.inputs[0].after.owners=@()}
 'wrong mounted owner actor'={param($f)$f.artifact.observations.inputs[1].before.owners[0].riderId=$f.request.smallCalibration.source.petId}
 'owner generation changes'={param($f)$f.artifact.observations.inputs[2].after.owners[0].generation=2L}
 'foreign actor object'={param($f)$f.artifact.observations.inputs[2].after.actors[0].object=99}
 'wrong pose frame window'={param($f)$f.artifact.observations.poses[3].observation.frame=6}
 'registered bone replacement'={param($f)$f.artifact.observations.poses[2].observation.riderBones[0].instanceId=71}
 'rig root replacement'={param($f)$f.artifact.observations.poses[2].observation.riderRoot.instanceId=81}
 'raw distance substitution'={param($f)$f.artifact.observations.poses[2].observation.leftFootToLeftStirrup=.5}
 'RT cost uses TB-only hook'={param($f)$f.artifact.observations.inputs[1].nativeEvents[0].boundary='actor-cost-before';$f.artifact.observations.inputs[1].nativeEvents[1].boundary='actor-cost-after'}
 'unknown bone'={param($f)$f.artifact.observations.poses[1].observation.riderBones[0].name='Unknown'}
 'native transform numeric text'={param($f)$f.artifact.observations.poses[1].observation.riderBones[0].instanceId='70'}
 'missing moving pose'={param($f)$f.artifact.observations.poses[2].phase='mounted-idle'}
 'nonfinite measured foot distance'={param($f)$f.artifact.observations.poses[2].observation.leftFootToLeftStirrup=[double]::NaN}
 'text application counter'={param($f)$f.artifact.observations.poses[2].observation.runtime.applications='10'}
 'unhealthy production pose'={param($f)$f.artifact.observations.poses[2].observation.runtime.healthy=$false}
 'invented visual acceptance'={param($f)$f.artifact.observations.humanVisualAcceptance=$true}
 'invented diagnostics off'={param($f)$f.artifact.observations.diagnosticsOffProven=$true}
 'foreign native command caster'={param($f)$f.artifact.observations.inputs[1].casterId=$f.request.smallCalibration.source.petId}
 'wrong resource owner'={param($f)$f.artifact.observations.inputs[1].resourceOwnerId=$f.request.smallCalibration.source.petId}
 'primary repeated attack'={param($f)$f.artifact.observations.inputs[1].outcome.completedAttacks=2}
 'native success boolean text'={param($f)$f.artifact.observations.inputs[1].outcome.nativeAttackRule='true'}
 'primary missing concrete spend'={param($f)$f.artifact.observations.inputs[1].nativeEvents=@()}
 'cost event substitution'={param($f)$f.artifact.observations.inputs[1].nativeEvents[1].command=300}
 'raw trace substitution'={param($f)$f.artifact.observations.allocationTrace.events[1].state.actor=$f.request.smallCalibration.source.petId}
 'native spend numeric text'={param($f)$f.artifact.observations.inputs[1].nativeEvents[1].state.standard='6'}
 'zero native spend'={param($f)$f.artifact.observations.inputs[1].nativeEvents[1].state.standard=0.0}
 'false displacement'={param($f)$f.artifact.observations.movement.actualDistance=1.5}
 'other actor moved command'={param($f)$f.artifact.observations.movement.executorId=$f.request.smallCalibration.source.actorId}
 'relationship refunded actor'={param($f)$f.artifact.observations.inputs[0].after.actors[0].standard=1.0}
 'retained shell cleanup'={param($f)$f.artifact.observations.cleanup.nativeCommandsSettled=$false}
 'retained native target'={param($f)$f.artifact.observations.cleanup.targetRemoved=$false}
 'retained observer'={param($f)$f.artifact.observations.cleanup.observerReleased=$false}
 'retained owner'={param($f)$f.artifact.observations.cleanup.ownerCount=1}
}
foreach($c in $cases.GetEnumerator()){Bad $c.Key {$f=New-SmallFixture;&$c.Value $f;Assert-KmcChunk7SmallEvidence $f.request $f.artifact}}
Good 'large raw stirrup gap remains observation and cannot claim visual PASS' {$f=New-SmallFixture;$p=$f.artifact.observations.poses[2].observation;$p.horseAnchors[1].worldPosition.x=1.7;$p.leftFootToLeftStirrup=1.7;$p.rightFootToLeftStirrup=1.7;Assert-KmcChunk7SmallEvidence $f.request $f.artifact}
Write-Host ('TOTAL PASS '+$script:passes+' FAIL 0; synthetic Small calibration reader, no native/visual qualification')
