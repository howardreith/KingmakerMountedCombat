[CmdletBinding()]
param([string]$RepositoryRoot,[string]$ReaderDirectory,[switch]$FixturesOnly)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('ReaderDirectory')){$ReaderDirectory=Join-Path $RepositoryRoot 'scripts/runtime'}
$progressionReaders=[IO.Path]::GetFullPath($ReaderDirectory);$only=$FixturesOnly.IsPresent
. (Join-Path $RepositoryRoot 'scripts/chunk7/Test-Chunk7OrdinaryEvidence.ps1') -RepositoryRoot $RepositoryRoot -ReaderDirectory (Join-Path $RepositoryRoot 'scripts/runtime') -FixturesOnly
. (Join-Path $progressionReaders 'Chunk7ProgressionEvidence.ps1')
. (Join-Path $progressionReaders 'Chunk7ProgressionFixtures.ps1')
. (Join-Path $PSScriptRoot '../../tests/Chunk7Diagnostics/OwnedProgressionEvidenceFixtures.ps1')
function New-ProgressionFixture([string]$Case='horse-native-level'){
    if(Test-KmcChunk7OwnedProgressionCase $Case){return New-OwnedProgressionFixture $Case}
    $old=New-OrdinaryFixture $false;$r=Copy-O $old.request;$r.scenario='chunk7-native-progression';$r.runId='synthetic-progression';$r.PSObject.Properties.Remove('ordinaryAcquisition')
    $r.evidenceRoot=Join-Path ([IO.Path]::GetDirectoryName($r.evidenceRoot)) $r.runId;$r.fixture|Add-Member NoteProperty baseline ([pscustomobject]@{sha256='b'*64})
    $write=$Case-cne'horse-native-dismiss';$respec=$Case-cin@('horse-native-retrain-horse','horse-native-retrain-mammoth')
    $s=[pscustomobject]@{case=$Case;sourceRunId='native-manual300';sourceArtifactSha256='d'*64;sourceArchiveSha256='e'*64;inventoryRunId='native-world';inventoryArtifactSha256='a'*64;
        actorId='d17c8fd0-6627-40c4-bf9a-024cf001c1f7';petId='66666666-7777-4888-8999-aaaaaaaaaaaa';controlActorId='b6628a77-4962-47a4-a17c-88d9836fc9d5';controlPetId='d79a4f6c-b74e-4868-95bd-533899131acb';
        raceGuid=$(if($respec){'1'*32}else{$null});presetGuid=$(if($respec){'2'*32}else{$null});skeletonName=$(if($respec){'ObservedMediumRig'}else{$null});gender=$(if($respec){'Male'}else{$null});leftHanded=$(if($respec){$false}else{$null});bodySize=$(if($respec){'Medium'}else{$null});minimumRangerLevel=$(if($write){4}else{0});expectedNativeWriteCount=[int]$write;nativeWriteLeaf=$(if($write){'Auto_1.zks'}else{$null})}
    $r|Add-Member NoteProperty progression $s
    $d=[pscustomobject]@{internalName='KMC_C7_WORLD';fileName='Manual_300_KMC_C7_WORLD.zks';sha256=$s.sourceArchiveSha256;length=1234L;lastWriteTimeUtcTicks=638900000000000000L;gameId=$r.fixture.working.gameId;gameName=$r.fixture.working.gameName;area=$r.fixture.working.area};$r|Add-Member NoteProperty persistenceLoad $d
    function Debt([string]$Id){[pscustomobject]@{Id=$Id;Standard=1.0;Move=0.5;Swift=0.0;Initiative=0.0;Reaction=0.0;ReactionsRemaining=1;LastSurpriseTicks=0L}}
    $pairs=@([pscustomobject]@{PairId='11111111111141118111111111111111';Generation=1L;ProfileId='medium-humanoid-mammoth-v1';Rider=(Debt $s.controlActorId);Mount=(Debt $s.controlPetId)},[pscustomobject]@{PairId='22222222222242228222222222222222';Generation=4L;ProfileId='medium-humanoid-horse-v1';Rider=(Debt $s.actorId);Mount=(Debt $s.petId)})
    $slots=@();foreach($id in @($s.actorId,$s.controlActorId)){foreach($kind in @(2,3)){$slots+=[pscustomobject]@{ActorId=$id;Index=10+$kind;Kind=$kind}}}
    $archive=[pscustomobject]@{SchemaVersion=3;CampaignId=$d.gameId;AreaId=$d.area;GameTimeTicks=10000000L;Policy='rider-principal-distinct-native-v1';RulesId='crpg-transport-v1';Pairs=$pairs;Participations=@();Slots=$slots;Combat=$null}
    $owners=@($pairs|ForEach-Object{[pscustomobject]@{ownerId=$_.PairId;generation=$_.Generation;riderId=$_.Rider.Id;mountId=$_.Mount.Id;state='Active'}})
    $hooks=@([pscustomobject]@{declaringType='TurnBased.Controllers.TurnController';method='Prepare';token='06000C3C';moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'},[pscustomobject]@{declaringType='Kingmaker.Controllers.Combat.UnitCombatState';method='NewRound';token='0600939D';moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'})
    foreach($method in @('RestoreActor','RestoreDebt','TryRestoreArchiveCombat','RestoreArchivePresentation')){$hooks+=[pscustomobject]@{declaringType='KingmakerMountedCombat.Integration.NativeCombatActorPersistence';method=$method;token='06001000';moduleMvid=$r.dllMvid}}
    $restore=@();foreach($actor in @($pairs|ForEach-Object{$_.Rider;$_.Mount})){foreach($kind in @('restore-debt-before','restore-debt-after')){$restore+=[pscustomobject]@{kind=$kind;actorId=$actor.Id;frame=1;restorePhase='validated-before-native-mutation';debt=(Copy-O $actor)}}}
    $restore+=[pscustomobject]@{kind='all-owner-presentation-restored';frame=2;restorePhase='all-pair-presentation-restored';snapshot=[pscustomobject]@{playerInCombat=$false;turnBased=$false;combat=$null;participations=@();owners=(Copy-O $owners);nativeControls=(Copy-O $slots)}}
    $root=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($r.evidenceRoot))) ('runtime-staging/persistence-'+$r.runId+'/Saved Games')
    $authority=[pscustomobject]@{root=$root;generation=2;exactScope=$true;maximumNativeLoads=1;maximumNativeWrites=$(if($write){1}else{$null});admittedNativeLoads=1;admittedNativeWrites=0;authorizedLoads=1;authorizedWrites=0;unauthorizedLoads=0;unauthorizedWrites=0;fatalViolations=0;suppressedWrites=0};$finalAuthority=Copy-O $authority;$finalAuthority.admittedNativeWrites=[int]$write;$finalAuthority.authorizedWrites=[int]$write
    $before=[pscustomobject]@{kind='native-progression-admitted';actorId=$s.actorId;object=3;level=4;xp=20000;petId=$s.petId;petLevel=4;money=20000L;gameTimeTicks=10000000L}
    $initial=if($respec){0}else{4};$end=if($respec){4}else{5};$events=[Collections.Generic.List[object]]::new();$nativeEvents=@((Copy-O $before));$moneyAfter=if($respec){11000L}else{20000L};$pet=if($respec){'77777777-7777-4777-8777-777777777777'}else{$s.petId}
    function Snap([int]$Level,[bool]$Complete=$false){$rebound=-not$respec-or$Level-gt0;[pscustomobject]@{actorId=$s.actorId;actorObject=$(if($rebound){3}else{99});descriptorObject=$(if($rebound){303}else{199});nativeControllerObject=555;level=$Level;rangerLevel=$Level;xp=20000;money=$(if($respec-and$rebound){$moneyAfter}else{20000L});nativeCanLevelUp=($Level-lt$end);nativeIsComplete=$Complete;nativeMode=$(if($respec-and$Level-eq0){'Respec'}else{'LevelUp'});phase=$(if($Complete){'Total'}else{'Class'});nextLevel=$Level+1;selectedClass='cda0615668a6df14eb36ba19ee881af6';attributePoints=0;distributionPoints=0;skillTotal=4;skillSpent=4;skillRemaining=0;pendingFeatures=0;pendingSpellGroups=0;petId=$(if(-not$respec-or$Level-ge4){$pet}else{$null});petBlueprint=$(if($Case-ceq'horse-native-retrain-mammoth'){'e7aa96d15a45238438ae4cfb476f6bb9'}else{'4016c7db400ab721ff125aef9e65e202'});petObject=403;petLevel=$Level;petExperience=20000;originalHorseRetained=(-not$respec)}}
    function Event([string]$Name,$Value){$Value|Add-Member NoteProperty event $Name;$Value|Add-Member NoteProperty runId $r.runId;$Value|Add-Member NoteProperty frame $events.Count;$Value|Add-Member NoteProperty elapsedSeconds ([double]$events.Count/10);$events.Add($Value)}
    $probe=$null;$nativeWrite=$null
    if($write){
        Event $(if($respec){'native-respec-build-admission'}else{'native-existing-horse-level-admission'}) (Snap $initial)
        for($level=$initial;$level-lt$end;$level++){
            Event 'native-level-open' (Snap $level)
            $specs=,@('class','cda0615668a6df14eb36ba19ee881af6',$null)
            if($respec-and$level-eq3){$companion=if($Case-ceq'horse-native-retrain-mammoth'){'6adc3aab7cde56b40aa189a797254271/None'}else{'7db7c50677e39f09feef56f3831fc723/None'};$specs+=@(@('feature','ee63330662126374e8785cc901941ac7/None','b705c5184a96a84428eeb35ae2517a14'),@('feature',$companion,'ee63330662126374e8785cc901941ac7'))}
            foreach($spec in $specs){$b=Snap $level;Event 'native-choice-requested' ([pscustomobject]@{kind=$spec[0];choice=$spec[1];nativeSelectionGuid=$spec[2];before=(Copy-O $b)});Event 'native-choice' ([pscustomobject]@{kind=$spec[0];choice=$spec[1];nativeSelectionGuid=$spec[2];before=(Copy-O $b);after=(Copy-O $b);nativeChoiceAccepted=$true})}
            $total=Snap $level $true;Event 'native-total-next-requested' $total
            if($respec-and$level-eq0){$rebound=Snap 1;$rebound|Add-Member NoteProperty nativeRespecRebound $true;Event 'native-respec-builder-rebound' $rebound}
            Event 'native-level-committed' ([pscustomobject]@{before=(Copy-O $total);after=(Snap ($level+1));moneyExpected=$moneyAfter;commitCount=$level-$initial+1})
        }
        $terminal=Snap $end;$terminal.nativeControllerObject=0;Event 'native-builder-complete' ([pscustomobject]@{actor=$terminal;ordinaryChoicesComplete=$true;nativeFieldAdmissionProven=$false;nativeSaveSettlementProven=$false;poseQualified=$false})
        $nativeEnd=Copy-O $before;$nativeEnd.kind='native-progression-builder-complete';$nativeEnd.level=$end;$nativeEnd.petId=$pet;$nativeEnd.petLevel=$end;$nativeEnd.money=$moneyAfter;$nativeEvents+=$nativeEnd
        $probe=Copy-O $old.artifact.observations.nativeObserver;$probe.events=@();$count=$end-$initial;$probe.completionEntries=$count;$probe.completionExits=$count
        for($i=1;$i-le$count;$i++){$row=[pscustomobject]@{kind='native-level-completion-enter';frame=$i;actorId=$s.actorId;actorObject=3;characterLevel=$initial+$i;inState=$true;isChargen=($i-lt$count);saveObject=0;saveType=$null;saveName=$null;saveLeaf=$null;autoLevelup=$null;savePath=$null;completionActorId=$s.actorId;insideObservedCompletion=$true};$probe.events+=Copy-O $row
            if($i-eq$count){$save=Copy-O $row;$save.kind='native-level-autosave-request';$save.saveObject=-999;$save.saveType='Auto';$save.saveLeaf='Auto_1.zks';$save.autoLevelup=$true;$save.saveName='Autosave';$probe.events+=Copy-O $save;$preparedSave=Copy-O $save;$preparedSave.kind='native-level-autosave-prepared';$preparedSave.saveObject=-1000;$preparedSave.frame++;$preparedSave.completionActorId=$null;$preparedSave.insideObservedCompletion=$false;$preparedSave|Add-Member NoteProperty requestedSaveObject (-999);$preparedSave|Add-Member NoteProperty accepted $true};$row.kind='native-level-completion-exit';$probe.events+=Copy-O $row}
        $probe.events+=,$preparedSave
        $output=Copy-O $archive;if($respec){$output.Pairs=@($output.Pairs[0])};$nativeWrite=[pscustomobject]@{path=(Join-Path $root 'Auto_1.zks');sha256='f'*64;length=1234L;type='Auto';leaf='Auto_1.zks';schema=3;name='Autosave';requestedObject=-999;writtenObject=-1000;completionSequence=1;completedWritesBefore=0;gameId=$d.gameId;gameName=$d.gameName;area=$d.area;operation='None';snapshotCount=1;completedWrites=1;nativeAutoLevelup=$true;nativeCallbackSupplied=$false;pairs=$output.Pairs.Count;archive=$output}
    }else{$nativeEnd=Copy-O $before;$nativeEnd.kind='native-dismissal-complete';$nativeEvents+=$nativeEnd}
    $ui=$null
    if($Case-cne'horse-native-level'){
        $ui=[pscustomobject]@{stage=3;respec=$respec;groupOwned=$false;selectorOwned=$false;dialogOwned=$false;confirmationSent=(-not$respec);selectorChoiceSent=$respec;events=@();nativeDismissal=$null;nativeRespec=$null}
        $labels=if($respec){@('native-respec-selector-opened','native-respec-target-selected','native-respec-preview-opened')}else{@('native-dismiss-selector-opened','native-dismiss-confirmation-opened','native-dismiss-confirmed')};foreach($label in $labels){$ui.events+=[pscustomobject]@{kind=$label;frame=$ui.events.Count;actorId=$s.actorId}}
        if(-not$respec){$dismiss=@();foreach($kind in @('native-dismiss-enter','native-dismiss-exit')){$dismiss+=[pscustomobject]@{kind=$kind;frame=4;actorId=$s.actorId;actorObject=3;playerObject=55;isInState=($kind-ceq'native-dismiss-enter');isInGame=($kind-ceq'native-dismiss-enter');destroyed=($kind-ceq'native-dismiss-exit');petId=$s.petId;insideExpectedNativeConfirmation=$true}};$ui.nativeDismissal=[pscustomobject]@{installed=$true;installationAttempted=$true;complete=$true;nativeEntries=1;nativeExits=1;errors=@();events=$dismiss}}
        else{
            $rh=@();$contracts=@(@('Kingmaker.Player','RespecCompanion','06000DE7'),@('Kingmaker.UI.LevelUp.CharacterBuildController','HandleLevelUpStart','060039BC'),@('Kingmaker.Designers.EventConditionActionSystem.Actions.RespecCompanion','FinishRespecialization','06008ED5'),@('Kingmaker.Game','AdvanceGameTime','06000CC6'),@('Kingmaker.Controllers.Rest.RestController','ApplyRest','060091EC'),@('Kingmaker.UI.LevelUp.CharacterBuildController','HandleUnitChangedAfterRespec','060039D7'),@('Kingmaker.UnitLogic.FactLogic.AddPet','TryUpdatePet','06002510'))
            foreach($contract in $contracts){$rh+=[pscustomobject]@{type=$contract[0];method=$contract[1];token=$contract[2];moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'}}
            $re=[Collections.Generic.List[object]]::new();function ResEvent([string]$Kind,$Data,[int]$Frame=2,[long]$Ticks=10000000L){$Data|Add-Member NoteProperty kind $Kind;$Data|Add-Member NoteProperty actorId $s.actorId;$Data|Add-Member NoteProperty actorObject 3;$Data|Add-Member NoteProperty frame $Frame;$Data|Add-Member NoteProperty gameTicks $Ticks;$re.Add($Data)}
            ResEvent 'native-respec-enter' ([pscustomobject]@{xp=20000;money=20000L;uses=3;cost=10000;scrollRefund=1000;descriptorObject=103;viewObject=203;petId=$s.petId})
            ResEvent 'native-respec-preview' ([pscustomobject]@{previewObject=99;descriptorObject=199;nativeCallbackToken='06001001'})
            ResEvent 'native-respec-ui-open' ([pscustomobject]@{})
            $restTargets=@([pscustomobject]@{id=$s.controlActorId;descriptorObject=11;move=1.0;standard=2.0;swift=0.0;prepared=$false;reactions=2;reactionAllowance=5;initiative=0.0})
            ResEvent 'native-respec-finish-enter' ([pscustomobject]@{money=21000L;nativeRestTargets=$restTargets}) 4
            ResEvent 'native-respec-time-advance' ([pscustomobject]@{deltaTicks=864000000000L}) 4
            foreach($kind in @('native-respec-rest-before','native-respec-rest-after')){ResEvent $kind (Copy-O $restTargets[0]) 4 864010000000L}
            ResEvent 'native-respec-finish-exit' ([pscustomobject]@{money=11000L;uses=4;nativeElapsedTicks=864000000000L}) 4 864010000000L
            ResEvent 'native-respec-original-rebound' ([pscustomobject]@{descriptorObject=303;viewObject=403;level=1;petId=$null;petObject=0;petDescriptorObject=0;petViewObject=0;oldPetId=$s.petId;oldPetRemoved=$true;oldPetShouldBeDestroyed=$true;oldPetDestroyed=$false;oldPetMasterId=$null}) 4 864010000000L
            ResEvent 'native-respec-pet-update' ([pscustomobject]@{sourceObject=701;ownerDescriptorObject=303;petId=$pet;petObject=403;petDescriptorObject=503;petViewObject=603;afterRebind=$true}) 5 864010000000L
            $ui.nativeRespec=[pscustomobject]@{installed=$true;installationAttempted=$true;complete=$true;errors=@();hooks=$rh;events=$re.ToArray();nativeRespecEntries=1;nativeRespecExits=1;nativePreviewEntries=1;nativeFinishEntries=1;nativeFinishExits=1;nativeAdvanceCalls=1;nativeRebinds=1;nativePetUpdates=1;petUpdatePending=$false;originalPetId=$s.petId;originalPetRemoved=$true;reboundPetId=$pet;expectedRestCount=1;observedRestEntries=1;observedRestExits=1;initialMoney=20000L;nativeCost=10000;nativeScrollRefund=1000;diagnosticMoneyWrites=0;diagnosticXpWrites=0;diagnosticRestCalls=0;diagnosticTimeAdvances=0}
        }
    }
    $observations=@();$labels=@('before-native-progression');if($Case-cne'horse-native-level'){$labels+=@('after-native-progression','native-target-cleanup-settled')};if($write){$labels+='native-terminal-save-settled'};$labels+='original-progression-pair-settled'
    foreach($label in $labels){$mounted=($label-ceq'before-native-progression')-or($Case-ceq'horse-native-level'-and$label-ceq'native-terminal-save-settled');$observations+=[pscustomobject]@{label=$label;originalOwnerPresent=$mounted;ownerState=$(if($mounted){'Active'}else{'Retired'});relationshipState=$(if($mounted){'Mounted'}else{'Unmounted'});currentGeneration=$(if($mounted){4L}else{0L});retainsObligations=$false;saveReady=$true;teardownReady=$true;noPreparedPairResidue=$true;nativeCommandsSettled=$true;nativeControlsSettled=$true;unrelatedOwners=@([pscustomobject]@{id=([Guid]$pairs[0].PairId).ToString('D');generation=1L;riderObject=11;mountObject=12;state='Active';relationshipState='Mounted'})}}
    $custody=[pscustomobject]@{ownerId=([Guid]$pairs[1].PairId).ToString('D');generation=4L;riderId=$s.actorId;mountId=$s.petId;targetSettled=$true;cleanupErrors=@();observations=$observations;manualActorAllocationWrites=0;manualPreparationCalls=0;manualCompanionRemovals=0}
    $session=[pscustomobject]@{case=$(switch -CaseSensitive($Case){'horse-native-level'{'ExistingHorseLevel'}'horse-native-dismiss'{'DismissCustomCompanion'}'horse-native-retrain-horse'{'RespecHorse'}'horse-native-retrain-mammoth'{'RespecMammoth'}});stage=2;started=$true;settled=$true;success=$true;errors=@();events=@($nativeEvents[0])+$events.ToArray()+@($nativeEvents[1]);pairCustody=$custody;nativeUi=$ui;nativeCompletion=$probe;nativeWrite=$nativeWrite;nativeAutomaticTargetCleanupObserved=($Case-cne'horse-native-level');manualMoneyWrites=0;manualXpWrites=0;manualCompanionRemovals=0;manualNativeRestCalls=0;manualNativeTimeAdvances=0;remainingWorldCleanupProven=$false;fixtureRollbackOwnedByOuterTransaction=$true}
    $body=[pscustomobject]@{OriginalSizeOrdinal=4;EffectiveSizeOrdinal=4;NativeRaceAndPresetIdentityExact=$true;NativeSkeletonIsExactPreset=$true;NativeRigAvailable=$true;RiderPolymorphed=$false;MountPolymorphed=$false;RaceGuid=$s.raceGuid;PresetGuid=$s.presetGuid;Gender=$s.gender;SkeletonName=$s.skeletonName;LeftHanded=$s.leftHanded}
    $o=[pscustomobject]@{selection=(Copy-O $s);loadedDescriptor=(Copy-O $d);loadedArchive=$archive;processId=456;elapsedSeconds=25.0;source=[pscustomobject]@{sourceRunId=$s.sourceRunId;sourceArtifactSha256=$s.sourceArtifactSha256;sourceArchiveSha256=$s.sourceArchiveSha256;inventoryRunId=$s.inventoryRunId;inventoryArtifactSha256=$s.inventoryArtifactSha256;sameCandidate=$true;nativeSourceGraphExact=$true;inventoryActor=[pscustomobject]@{id=$s.actorId}};authorityBefore=$authority;beforeOwners=[pscustomobject]@{owners=$owners};nativeBodyBefore=$body;session=$session;
        coldRestoration=[pscustomobject]@{complete=$true;installedBeforeWorldLoad=$true;contract='chunk7-native-restoration-entry-observer-v1';maximumEvents=2048;dropped=0;errors=0;prepareMetric='native-method-entries-including-production-refused-attempts';hooks=$hooks;events=$restore};
        cleanup=[pscustomobject]@{custodySettled=$true;remainingWorldCleanupProven=$true;workersSettled=$true;nativeProcessesSettled=$true;ownersRemaining=0;selectionRestoredToSurvivingActors=$true;pauseRestored=$true;nativeAcquiredStateRetained=$true;manualMoneyWrites=0;manualXpWrites=0;manualCompanionRemovals=0;manualNativeRestCalls=0;manualNativeTimeAdvances=0;sourceUnchanged=$true;authority=$finalAuthority}}
    $a=Copy-O $old.artifact;$a.scenario=$r.scenario;$a.runId=$r.runId;$a.evidenceKind='chunk7-progression-evidence';$a.observations=$o
    [pscustomobject]@{request=$r;artifact=$a}
}
if($only){return}
$script:passes=0
foreach($case in Get-KmcChunk7ProgressionCases){Good ('closed native progression evidence '+$case) {$f=New-ProgressionFixture $case;Assert-KmcChunk7ProgressionEvidence $f.request $f.artifact}}
Good 'actual closed builder keeps nullable native snapshot fields' {$f=New-ProgressionFixture;$rows=@($f.artifact.observations.session.events|Where-Object{$_.PSObject.Properties['event']});$admitted=$rows[0];$admitted.nativeMode=$null;$admitted.phase=$null;$admitted.selectedClass=$null;$admitted.nativeControllerObject=0;$terminal=($rows|Where-Object event -CEQ 'native-builder-complete').actor;$terminal.nativeMode=$null;$terminal.phase=$null;$terminal.selectedClass=$null;Assert-KmcChunk7ProgressionEvidence $f.request $f.artifact}
Good 'observed native AddPet no-op cannot replace the earlier sourced pet' {$f=New-ProgressionFixture 'horse-native-retrain-horse';$probe=$f.artifact.observations.session.nativeUi.nativeRespec;$noop=Copy-O $probe.events[-1];$noop.petId=$null;$noop.petObject=0;$noop.petDescriptorObject=0;$noop.petViewObject=0;$probe.events+=@($noop);$probe.nativePetUpdates=2;Assert-KmcChunk7ProgressionEvidence $f.request $f.artifact}
$cases=[ordered]@{
 'foreign case'={param($f)$f.request.progression.case='anything'}
 'foreign source hash'={param($f)$f.request.persistenceLoad.sha256='9'*64}
 'invented source leaf'={param($f)$f.request.persistenceLoad.fileName='Manual_320_KMC_PROGRESS.zks'}
 'late cold observer'={param($f)$f.artifact.observations.coldRestoration.installedBeforeWorldLoad=$false}
 'lost native debt restoration'={param($f)$f.artifact.observations.coldRestoration.events=@($f.artifact.observations.coldRestoration.events|Select-Object -Skip 1)}
 'same-area foreign owner generation'={param($f)$f.artifact.observations.beforeOwners.owners[1].generation++}
 'second native load'={param($f)$f.artifact.observations.cleanup.authority.admittedNativeLoads=2}
 'text native load cap'={param($f)$f.artifact.observations.authorityBefore.maximumNativeLoads='1'}
 'native scope replaced'={param($f)$f.artifact.observations.cleanup.authority.generation++}
 'manual XP write'={param($f)$f.artifact.observations.session.manualXpWrites=1}
 'lost control owner'={param($f)$f.artifact.observations.session.pairCustody.observations[1].unrelatedOwners=@()}
 'control object substituted'={param($f)$f.artifact.observations.session.pairCustody.observations[1].unrelatedOwners[0].riderObject++}
 'unsettled original native process'={param($f)$f.artifact.observations.session.pairCustody.observations[-1].nativeCommandsSettled=$false}
 'finished observer false'={param($f)$f.artifact.observations.session.nativeCompletion.complete=$false}
 'prepared save object string'={param($f)($f.artifact.observations.session.nativeCompletion.events|Where-Object kind -CEQ 'native-level-autosave-prepared').saveObject='-999'}
 'prepared save object float'={param($f)($f.artifact.observations.session.nativeCompletion.events|Where-Object kind -CEQ 'native-level-autosave-prepared').saveObject=-999.0}
 'completion outside native call'={param($f)($f.artifact.observations.session.nativeCompletion.events|Where-Object kind -CEQ 'native-level-autosave-request').insideObservedCompletion=$false}
 'unchanged original Horse level'={param($f)($f.artifact.observations.session.events|Where-Object{$_.PSObject.Properties['event']-and$_.event-ceq'native-builder-complete'}).actor.petLevel=4}
 'native choices unready'={param($f)($f.artifact.observations.session.events|Where-Object{$_.PSObject.Properties['event']-and$_.event-ceq'native-total-next-requested'}).nativeCanLevelUp=$false}
 'mandatory choice incomplete'={param($f)($f.artifact.observations.session.events|Where-Object{$_.PSObject.Properties['event']-and$_.event-ceq'native-total-next-requested'}).skillRemaining=1}
 'output lost level target owner'={param($f)$f.artifact.observations.session.nativeWrite.archive.Pairs=@($f.artifact.observations.session.nativeWrite.archive.Pairs[0]);$f.artifact.observations.session.nativeWrite.pairs=1}
 'worker custody falsely complete'={param($f)$f.artifact.observations.cleanup.workersSettled=$false}
 'text native cleanup flag'={param($f)$f.artifact.observations.cleanup.nativeProcessesSettled='true'}
 'source archive changed'={param($f)$f.artifact.observations.cleanup.sourceUnchanged=$false}
 'selection leaf array'={param($f)$f.request.progression.nativeWriteLeaf=@('Auto_1.zks')}
 'native write type array'={param($f)$f.artifact.observations.session.nativeWrite.type=@('Auto')}
 'native write leaf array'={param($f)$f.artifact.observations.session.nativeWrite.leaf=@('Auto_1.zks')}
 'native write operation array'={param($f)$f.artifact.observations.session.nativeWrite.operation=@('None')}
 'native actor array'={param($f)$f.artifact.observations.session.events[0].actorId=@($f.request.progression.actorId)}
 'native event kind array'={param($f)$f.artifact.observations.session.events[0].kind=@('native-progression-admitted')}
 'native snapshot actor array'={param($f)($f.artifact.observations.session.events|Where-Object{$_.PSObject.Properties['event']-and$_.event-ceq'native-level-open'}).actorId=@($f.request.progression.actorId)}
 'native choice event array'={param($f)($f.artifact.observations.session.events|Where-Object{$_.PSObject.Properties['event']-and$_.event-ceq'native-level-open'}).event=@('native-level-open')}
}
foreach($entry in $cases.GetEnumerator()){Bad $entry.Key {$f=New-ProgressionFixture;&$entry.Value $f;Assert-KmcChunk7ProgressionEvidence $f.request $f.artifact}}
foreach($case in @('horse-native-retrain-horse','horse-native-retrain-mammoth')){
    Bad ($case+' native one-day effect omitted') {$f=New-ProgressionFixture $case;($f.artifact.observations.session.nativeUi.nativeRespec.events|Where-Object kind -CEQ 'native-respec-finish-exit').nativeElapsedTicks=0;Assert-KmcChunk7ProgressionEvidence $f.request $f.artifact}
    Bad ($case+' stale original owner persisted') {$f=New-ProgressionFixture $case;$f.artifact.observations.session.nativeWrite.archive.Pairs+=@($f.artifact.observations.loadedArchive.Pairs[1]);$f.artifact.observations.session.nativeWrite.pairs=2;Assert-KmcChunk7ProgressionEvidence $f.request $f.artifact}
    Bad ($case+' native refund manufactured') {$f=New-ProgressionFixture $case;$f.artifact.observations.session.nativeUi.nativeRespec.diagnosticMoneyWrites=1;Assert-KmcChunk7ProgressionEvidence $f.request $f.artifact}
    foreach($mutation in @('missing-hook','missing-update','pending-update','foreign-source-owner','unobserved-pet-object','wrong-native-order','old-pet-retained','old-pet-master-retained','missing-reaction-count','text-reaction-allowance','repeated-pet-substitution','body-size-array','gender-array')){
        Bad ($case+' '+$mutation){$f=New-ProgressionFixture $case;$p=$f.artifact.observations.session.nativeUi.nativeRespec;switch($mutation){
            'missing-hook'{$p.hooks=@($p.hooks|Select-Object -First 6)}
            'missing-update'{$p.events=@($p.events|Where-Object kind -CNE 'native-respec-pet-update');$p.nativePetUpdates=0}
            'pending-update'{$p.petUpdatePending=$true}
            'foreign-source-owner'{$p.events[-1].ownerDescriptorObject++}
            'unobserved-pet-object'{$p.events[-1].petObject++}
            'wrong-native-order'{$p.events[-1].afterRebind=$false}
            'old-pet-retained'{$p.originalPetRemoved=$false}
            'old-pet-master-retained'{($p.events|Where-Object kind -CEQ 'native-respec-original-rebound').oldPetMasterId=$f.request.progression.actorId}
            'missing-reaction-count'{($p.events|Where-Object kind -CEQ 'native-respec-rest-before').PSObject.Properties.Remove('reactions')}
            'text-reaction-allowance'{($p.events|Where-Object kind -CEQ 'native-respec-rest-after').reactionAllowance='5'}
            'repeated-pet-substitution'{$extra=Copy-O $p.events[-1];$extra.petViewObject++;$p.events+=@($extra);$p.nativePetUpdates=2}
            'body-size-array'{$f.request.progression.bodySize=@('Medium')}
            'gender-array'{$f.request.progression.gender=@('Male')}
        };Assert-KmcChunk7ProgressionEvidence $f.request $f.artifact}
    }
}
Bad 'dismissal creates autosave authority' {$f=New-ProgressionFixture 'horse-native-dismiss';$f.artifact.observations.authorityBefore.maximumNativeWrites=1;Assert-KmcChunk7ProgressionEvidence $f.request $f.artifact}
Bad 'dismissal bypasses real confirmation' {$f=New-ProgressionFixture 'horse-native-dismiss';$f.artifact.observations.session.nativeUi.nativeDismissal.events[0].insideExpectedNativeConfirmation=$false;Assert-KmcChunk7ProgressionEvidence $f.request $f.artifact}
Write-Host ('TOTAL PASS '+$script:passes+' FAIL 0; strict synthetic progression evidence, no native gameplay proof')
