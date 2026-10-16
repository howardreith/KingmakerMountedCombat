
function New-AssistedCompositionFixture {
    # Controlled complete composition. The host/core formatter fragments are
    # cloned as schema inputs; native probes, profile, callback, timing and
    # progression are explicit generated external-boundary values, never native proof.
    $o=Copy-O $script:hostFragment;$ordinary=New-OrdinaryFixture $false
    $main=$o.fieldWorld.mainActorId;$pet=($o.fieldWorld.party|Where-Object id -CEQ $main).petId
    $run='synthetic-assisted-composition';$root=Join-Path (Join-Path (Join-Path (Get-KmcLabRoot) 'runtime-staging') ('persistence-'+$run)) 'Saved Games'
    $s=[pscustomobject]@{case='authored-main-ranger4-manual';ownerAuthorizationSha256='052eb32338bba397e852cba8605ebc6878deff1dd907675041d5b1d2f802e28e';baseGameOnly=$true;assistedSetupOnly=$true;storyPolicy='first-unused-eligible-row-else-root-system-button';assistancePolicy='native-main-ranger4-and-one-hire-shortfalls';expectedNativeLoadCount=0;maximumNativeWriteCount=3;targetRangerLevel=4;maximumTargetLevel=8;maximumStoryChoices=48;maximumStorySeconds=300;maximumExperienceShortfall=200000;maximumMoneyShortfall=100000;nativeInitialAutoLeaf='Auto_1.zks';nativeManualName='KMC_C7_ASSISTED_FIXTURE'}
    $request=Copy-O $ordinary.request;$request.scenario='chunk7-assisted-fixture-preparation';$request.runId=$run;$request.evidenceRoot=Join-Path (Join-Path (Get-KmcLabRoot) 'runtime-evidence') $run
    $request.PSObject.Properties.Remove('ordinaryAcquisition');$request|Add-Member NoteProperty assistedFixture $s
    $anchor=@($o.settledDescriptors|Where-Object fileName -CNE 'Auto_1.zks'|Where-Object fileName -CNE $o.finalManual.fileName)[0]
    $request.fixture.working=[pscustomobject]@{fileName=$anchor.fileName;internalName=$anchor.internalName;gameId=$anchor.gameId;gameName=$anchor.gameName;area=$anchor.area;sha256='1'*64;length=100}
    $request.fixture|Add-Member NoteProperty baseline ([pscustomobject]@{gameId='99999999-9999-9999-9999-999999999999';sha256='2'*64})
    $o|Add-Member NoteProperty selection (Copy-O $s);$o|Add-Member NoteProperty processId 123
    $o|Add-Member NoteProperty assistanceOnly $true
    foreach($key in @('ordinaryAcquisitionProven','ordinaryProgressionProven','mountedCombatProven','smallPoseQualified','workingDeserialized','diagnosticArchiveContentRead')){$o|Add-Member NoteProperty $key $false}
    function Set-Authority($a,[string]$Phase,[int]$Writes,[int]$Levels,[int]$Reservations,[int]$Number=0){
        $a.root=$root;$a.phase=$Phase;$a.admittedNativeWrites=$Writes;$a.authorizedWrites=$Writes;$a.freezeCount=[Math]::Min(1,$Writes);$a.levelupWrites=$Levels;$a.reservationCount=$Reservations;$a.manualNumber=$Number;$a.manualLeaf=if($Number){'Manual_'+$Number+'_KMC_C7_ASSISTED_FIXTURE.zks'}else{$null}
        if($Writes-eq0){$a.frozenGameId=$null;$a.frozenGameName=$null}
    }
    function Set-Actors($Rows){foreach($a in $Rows){
        if($a.id-ceq$main){$a.characterLevel=if($a.petId){4}else{1};$a.classes=@([pscustomobject]@{guid='cda0615668a6df14eb36ba19ee881af6';level=$a.characterLevel});if($a.petId){$a.features[0].guid='7db7c50677e39f09feef56f3831fc723';$a.features+=,[pscustomobject]@{object=77770005;guid='1670990255e4fe948a863bafd5dbda5d';source='cda0615668a6df14eb36ba19ee881af6';rank=4}};if($a.PSObject.Properties['experience']){$a.experience=6000}}
        if($a.id-ceq$pet){$a.blueprint='4016c7db400ab721ff125aef9e65e202'}
    }}
    foreach($world in @($o.initialWorld,$o.beforeSetup,$o.fieldWorld,$o.savedWorld,$o.settledWorld)){Set-Actors $world.party}
    Set-Actors $o.afterSetupActors
    $o.initialWorld.frame=100;$o.beforeSetup.frame=110;$o.fieldWorld.frame=200;$o.savedWorld.frame=220;$o.settledWorld.frame=221
    Set-Authority $o.initialWorld.authority 'initial-auto' 1 0 0
    Set-Authority $o.beforeSetup.authority 'setup' 1 0 0
    Set-Authority $o.fieldWorld.authority 'setup' 2 1 1
    $number=$o.finalManual.authority.manualNumber
    foreach($world in @($o.savedWorld,$o.settledWorld)){Set-Authority $world.authority 'complete' 3 1 2 $number}
    $before=Copy-O $o.initialWorld;$before.frame=90;$before.mainActor=0;$before.mainActorId=$null;$before.gameId=$null;$before.gameName=$null;$before.area=$null;$before.mode='None';$before.party=@();Set-Authority $before.authority 'initial-auto' 0 0 0
    $o|Add-Member NoteProperty before $before
    for($i=0;$i-lt3;$i++){$w=$o.nativeWrites[$i];$w.path=Join-Path $root $w.fileName;$w.frame=@(100,190,220)[$i];$w.sha256=(@('a','b','c')[$i])*64;$w.lastWriteTimeUtcTicks=10000+$i
        if($i-eq0){Set-Authority $w.authority 'initial-auto' 1 0 0}elseif($i-eq1){Set-Authority $w.authority 'levelup-auto' 2 0 1}else{Set-Authority $w.authority 'manual' 3 1 2 $number}}
    $o.finalManual=Copy-O $o.nativeWrites[2]
    $o|Add-Member NoteProperty derivedTarget ([pscustomobject]@{mainActorId=$main;characterLevel=1;rangerLevel=1;targetLevel=4})
    $setup=Copy-O $script:coreFragment
    foreach($key in @('runId','gameId','gameName','mainActorId')){$setup.$key=if($key-ceq'runId'){$run}else{$o.initialWorld.$key}}
    $setup.story.lastObserved.frame=109;$setup.story.lastObserved.elapsedMilliseconds=100
    foreach($i in @($setup.story.lastObserved.identity,$setup.experience.before.identity,$setup.experience.after.identity,$setup.hireFunds.before.identity,$setup.hireFunds.after.identity)){foreach($key in @('runId','gameId','gameName','mainActorId')){$i.$key=$setup.$key}}
    foreach($effect in @($setup.experience,$setup.hireFunds)){
        foreach($key in @('attempted','nativeEntered','nativeReturned','afterReadSucceeded')){$effect.$key=$true};$effect.noOp=$false
        foreach($snap in @($effect.before,$effect.after)){$snap.experience=6000;$snap.money=20000;$snap.targetLevel=4;$snap.targetThreshold=6000;$snap.nativeHireCost=25000;$snap.experienceRatePercent=100;$snap.hasPet=($effect.kind-ceq'native-hire-money-shortfall');$snap.isPet=$false;$snap.hasDualCompanion=$false;$snap.level=if($snap.hasPet){4}else{1};$snap.frame=if($snap.hasPet){200}else{110}}
    }
    $setup.experience.delta=5900;$setup.experience.before.experience=100;$setup.hireFunds.delta=5000;$setup.hireFunds.after.money=25000
    foreach($row in $o.preservation.rows){
        Set-Actors $row.state.actors;$row.frame=if($row.purpose.StartsWith('experience')){110}else{200};$row.state.experienceRatePercent=100
        $row.canonicalJson=ConvertTo-Json -InputObject $row.state -Depth 64 -Compress;$row.sha256=Hash-CompositionText $row.canonicalJson
    }
    foreach($pair in @(@($setup.experience,'experience'),@($setup.hireFunds,'hire-funds'))){$effect=$pair[0];$rows=@($o.preservation.rows|Where-Object{$_.purpose.StartsWith($pair[1])});$effect.before.preservedStateSha256=$rows[0].sha256;$effect.after.preservedStateSha256=$rows[-1].sha256}
    $o|Add-Member NoteProperty setup $setup;$o.setupAtCleanup=Copy-O $setup
    # Rebind an existing tested generated native choice chain to this controlled main.
    $oldMain=$ordinary.artifact.observations.builderComplete.actorId;$oldPet=$ordinary.artifact.observations.builderComplete.petId
    $chainText=ConvertTo-Json -InputObject $ordinary.artifact.observations.choices -Depth 64 -Compress
    $chainText=$chainText.Replace($oldMain,$main).Replace($oldPet,$pet).Replace($ordinary.request.runId,$run)
    $choiceRows=ConvertFrom-Json -InputObject $chainText
    function Shift-ChoiceFrames($value){if($null-eq$value-or$value-is[string]-or$value-is[ValueType]){return};if($value-is[Array]){foreach($item in $value){Shift-ChoiceFrames $item};return};foreach($property in $value.PSObject.Properties){if($property.Name-ceq'frame'){$property.Value+=120}else{Shift-ChoiceFrames $property.Value}}};Shift-ChoiceFrames $choiceRows
    $o|Add-Member NoteProperty choices $choiceRows
    $builderText=ConvertTo-Json -InputObject $ordinary.artifact.observations.builderComplete -Depth 64 -Compress
    $o|Add-Member NoteProperty builderComplete (ConvertFrom-Json -InputObject $builderText.Replace($oldMain,$main).Replace($oldPet,$pet))
    $o.ordinaryProbe=Copy-O $ordinary.artifact.observations.nativeObserver
    foreach($row in $o.ordinaryProbe.events){$row.actorId=$main;if($null-ne$row.completionActorId){$row.completionActorId=$main};$row.frame+=170}
    $o.ordinaryProbe.events[-1].saveLeaf='Auto_2.zks'
    $o.ordinaryIdentityBridge=[pscustomobject]@{contract='exact-object-label-domains-v1';hostDomain='retained-host-2048';observerDomain='runtime-object-hash';roles=[pscustomobject]@{actor=[pscustomobject]@{host=$o.fieldWorld.mainActor;observer=3};descriptor=[pscustomobject]@{host=$o.fieldWorld.party[0].descriptor;observer=103};requested=[pscustomobject]@{host=$o.nativeWrites[1].requestedDescriptor;observer=999};prepared=[pscustomobject]@{host=$o.nativeWrites[1].preparedDescriptor;observer=1000};saveManager=[pscustomobject]@{host=$o.fieldWorld.saveManager;observer=1001}}}
    foreach($arr in @($o.afterManualArrangement)){
        $arr.money=25000;$arr.experienceRatePercent=100
        foreach($a in $arr.actors){$field=@($o.fieldWorld.party|Where-Object id -CEQ $a.id)[0];$a.level=$field.characterLevel;$a.classes=Copy-O $field.classes;$a.blueprint=$field.blueprint;if($a.id-ceq$main){$a.experience=6000}}
    }
    $o|Add-Member NoteProperty beforeManualArrangement (Copy-O $o.afterManualArrangement)
    $o.fixtureReadiness.gameplayAssemblyMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7';$o.fixtureReadiness.frame=200;$o.fixtureReadiness.mainProgression.characterLevel=4;$o.fixtureReadiness.mainProgression.experience=6000;$o.fixtureReadiness.mainProgression.classes=@([pscustomobject]@{classGuid='cda0615668a6df14eb36ba19ee881af6';level=4;archetypeGuids=@()})
    $o.fixtureReadiness.isolatedSaveDescriptors.root=$root
    $profile=[pscustomobject]@{schemaVersion=1;captureKind='installed-native-small-body-catalogue';captureStatus='PASS';gameplayAssemblyMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7';profileAdmissionChanged=$false;nativeFixtureCreated=$false;poseQualified=$false;ordinaryAcquisitionProven=$false;party=@();ordinaryAcquisitionPrerequisites=[pscustomobject]@{observationOnly=$true;purchaseAuthorized=$false;sixPairFixtureQualified=$false;nativeMoney=25000;nativeHireCostNow=25000;nativeAffordableNow=$true;nativePartyLevel=4;nativeFieldCharacterLimit=6;nativePartyCharacterIds=@($o.fixtureReadiness.staticPartyFacts.characters.actorId);mainCharacterId=$main;mainCharacterLevel=4;mainCharacterExperience=6000;nativeDefaultHireExperienceNow=6000;defaultHireMatchesPlayerExperienceExactly=$false}}
    foreach($a in $o.fieldWorld.party){$profile.party+=,[pscustomobject]@{id=$a.id;blueprintGuid=$a.blueprint;masterId=$a.masterId;petId=$a.petId;nativeLevelUpReadiness=[pscustomobject]@{characterLevel=$a.characterLevel;experience=$(if($a.id-ceq$main){6000}else{$a.experience});nextLevelExperience=15000;canLevelUp=$false;isDead=$false;partyInCombat=$false;controllerCreated=$false;completeChoicesObserved=$false;ordinaryAcquisitionProven=$false}}}
    $o|Add-Member NoteProperty nativeProfileInventory $profile
    $o|Add-Member NoteProperty nativeNumbering ([pscustomobject]@{method='FindUnusedSaveNumber';token='06008024';moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7';frame=200;number=$number;fileName=$o.finalManual.fileName;sourceDescriptors=(Copy-O $o.afterLevelupDescriptors);outputInitiallyAbsent=$true;slotProjectionEnabled=$false})
    foreach($key in @('callbackEntered','callbackReturned','dispatched')){$o.cleanup.$key=$true};Set-Authority $o.cleanup.authority 'closed' 3 1 2 $number;$o.elapsedSeconds=10.0
    $presetGuid='abababababababababababababababab';$o|Add-Member NoteProperty preset ([pscustomobject]@{source='BlueprintRoot.NewGamePreset';guid=$presetGuid;area=$o.initialWorld.area;enterPoint='efefefefefefefefefefefefefefefef';enterPointArea=$o.initialWorld.area;makeAutosave=$true;charGen=$false;playerBlueprint=($o.initialWorld.party|Where-Object id -CEQ $main).blueprint;usesPresetPlayer=$true;companionCount=1})
    $nativePrefix=@('472091361cf118049a2b4339c4ea836a','f6f1cdcc404f10c4493dc1e51208fd6f','afb817d80b843cc4fa7b12289e6ebe3d','f9ef7717531f5914a9b6ecacfad63f46','f894e003d31461f48a02f5caec4e3359','e992949eba096644784592dc7f51a5c7','aa92fea676be33d4dafd176d699d7996','2ee2ba60850dd064e8b98bf5c2c946ba','6adc3aab7cde56b40aa189a797254271','ece6bde3dfc76ba4791376428e70621a','126712ef923ab204983d6f107629c895','67a9dc42b15d0954ca4689b13e8dedea','7db7c50677e39f09feef56f3831fc723')
    $o|Add-Member NoteProperty preflight ([pscustomobject]@{horseState='Registered';horseFailure=$null;libraryReady=$true;rangerSelectionGuid='ee63330662126374e8785cc901941ac7';rangerSelectionPresent=$true;rangerAllFeaturesCount=13;rangerFirst32Guids=$nativePrefix;horseFeatureGuid='7db7c50677e39f09feef56f3831fc723';horseUnitGuid='4016c7db400ab721ff125aef9e65e202';loadedAreaPresent=$false;mainActorPresent=$false;authorizedLoads=0;authorizedWrites=0;nativeNewGameDispatched=$false})
    $roles=[pscustomobject]@{};$roleNames=@('game','player','saveManager','main','requested','prepared','preset');$hostIds=@($o.initialWorld.game,$o.initialWorld.player,$o.initialWorld.saveManager,$o.initialWorld.mainActor,$o.nativeWrites[0].requestedDescriptor,$o.nativeWrites[0].preparedDescriptor,77770001)
    for($i=0;$i-lt7;$i++){$roles|Add-Member NoteProperty $roleNames[$i] ([pscustomobject]@{host=$hostIds[$i];observer=$i+1})}
    $o|Add-Member NoteProperty bootstrapIdentityBridge ([pscustomobject]@{contract='exact-object-label-domains-v1';hostDomain='retained-host-2048';observerDomain='retained-bootstrap-128';roles=$roles})
    $hooks=@();for($i=0;$i-lt3;$i++){$hooks+=,[pscustomobject]@{type=$(if($i-eq0){'Kingmaker.Game'}else{'Kingmaker.EntitySystem.Persistence.SaveManager'});method=@('LoadNewGame','SaveRoutine','PrepareSave')[$i];token=@('06000CDC','06008029','06008025')[$i];moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7';transpiler=$(if($i-eq0){'IdentityCallsite'}else{$null})}}
    $site=[pscustomobject]@{contract='load-new-game-guid-setter-callsite-v1';installed=$true;ownerToken='06000CDC';setterToken='06000DE3';stateFieldToken='040006B6';playerFieldToken='0400533C';setterIlOffset=302;moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7';nativeSetterPreserved=$true;setterPostfixUsed=$false;matchedSites=1}
    $events=@();for($i=0;$i-lt5;$i++){$events+=,[pscustomobject]@{sequence=$i+1;frame=91+$i;kind=@('native-new-game-enter','native-campaign-minted','native-bootstrap-autosave-request','native-new-game-exit','native-bootstrap-autosave-prepared')[$i];presetGuid=$presetGuid;game=1;player=2;saveManager=3;preset=7;menuPlayer=2;sameMenuPlayer=$true;insideNativeNewGame=($i-lt4);gameId=$o.initialWorld.gameId;mainActorId=$main;save=$(if($i-eq2){5}elseif($i-eq4){6}else{0});saveType='Auto';autoLevelup=$false;saveName='Autosave'}}
    $last=$events[-1];foreach($pair in @(@('requestedSave',5),@('observedSaveManager',3),@('accepted',$true),@('saveLeaf',$o.nativeWrites[0].fileName),@('savePath',$o.nativeWrites[0].path),@('saveArea',$o.nativeWrites[0].area),@('saveGameId',$o.nativeWrites[0].gameId),@('saveGameName',$o.nativeWrites[0].gameName))){$last|Add-Member NoteProperty $pair[0] $pair[1]}
    $checks=[pscustomobject]@{};foreach($key in @('ordered','sameGame','sameManager','samePlayer','requestRetained','distinctDescriptor','sameName','autoCategory','expectedLeaf','sameCampaign')){$checks|Add-Member NoteProperty $key $true};$last|Add-Member NoteProperty checks $checks
    $o.bootstrapProbe=[pscustomobject]@{hooks=$hooks;events=$events;errors=@();installed=$true;installationAttempted=$true;entered=1;exited=1;minted=1;saveRequests=1;prepared=1;complete=$true;identityCallsite=$site;identities=[pscustomobject]@{contract='retained-native-object-identity-v1';capacity=128;ids=@(1,2,3,4,5,6,7);retainedCount=7;faults=0;released=$false}}
    $manualHooks=@();for($i=0;$i-lt3;$i++){$manualHooks+=,[pscustomobject]@{type=$(if($i-eq2){'KingmakerMountedCombat.Integration.MountedPersistenceService'}else{'Kingmaker.EntitySystem.Persistence.SaveManager'});method=@('SaveRoutine','PrepareSave','RecordCompletedSave')[$i];token=@('06008029','06008025','0600F001')[$i];moduleMvid=$(if($i-eq2){$request.dllMvid}else{'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'})}}
    $manualEvents=@();for($i=0;$i-lt6;$i++){$manualEvents+=,[pscustomobject]@{kind=@('native-save-request','native-save-prepared','snapshot-starting','snapshot-staged','native-worker-completed','native-save-callback')[$i];sequence=$i+1;frame=210+$i;game=$o.fieldWorld.game;player=$o.fieldWorld.player;saveManager=$o.fieldWorld.saveManager;requested=$o.finalManual.requestedDescriptor;descriptor=$(if($i-eq0){$o.finalManual.requestedDescriptor}else{$o.finalManual.preparedDescriptor});internalName=$o.finalManual.internalName;type='Manual';gameName=$o.finalManual.gameName;area=$o.finalManual.area;fileName=$(if($i-eq0){$null}else{$o.finalManual.fileName});gameId=$(if($i-eq0){$null}else{$o.finalManual.gameId});path=$(if($i-eq0){$null}else{$o.finalManual.path});snapshotCount=$(if($i-lt3){2}else{3});completedSaveCount=$(if($i-lt5){2}else{3})}}
    $worker=$manualEvents[4];foreach($pair in @(@('iteratorType','Kingmaker.EntitySystem.Persistence.SaveManager+<SaveRoutine>d__46'),@('workerStatus','RanToCompletion'),@('workerId',1),@('nativeWorkerMatches',$true),@('workerCompleted',$true),@('workerFaulted',$false),@('workerCanceled',$false),@('scope',77770002),@('iterator',77770003),@('worker',77770004))){$worker|Add-Member NoteProperty $pair[0] $pair[1]}
    $o.manualProbe=[pscustomobject]@{contract='manual-seed-native-save-v1';installed=$true;installationAttempted=$true;complete=$true;requests=1;prepares=1;starts=1;stages=1;completions=1;callbacks=1;events=$manualEvents;hooks=$manualHooks;errors=@()}
    $o.identityRegistry.retainedIds+=@(77770001,77770002,77770003,77770004,77770005)
    $artifact=Copy-O $ordinary.artifact;$artifact.evidenceKind='chunk7-assisted-fixture-evidence';$artifact.runId=$run;$artifact.scenario=$request.scenario;$artifact.observations=$o
    $null=Get-KmcChunk7OrdinaryChoiceProof $run ([pscustomobject]@{existingActorId=$main;minimumRangerLevel=4}) $false @($o.beforeSetup.party.id) $o.choices $o.setup.experience.after.money $o.builderComplete
    [pscustomobject]@{request=$request;artifact=$artifact}
}

function New-AssistedTwoWriteFixture {
    $f=New-AssistedCompositionFixture;$o=$f.artifact.observations;$main=$o.fieldWorld.mainActorId
    $o.initialWorld.party=Copy-O $o.fieldWorld.party;$o.beforeSetup.party=Copy-O $o.fieldWorld.party
    $o.derivedTarget.characterLevel=4;$o.derivedTarget.rangerLevel=4
    $o.setup.experience.delta=0;$o.setup.experience.noOp=$true
    foreach($key in @('attempted','nativeEntered','nativeReturned')){$o.setup.experience.$key=$false}
    foreach($s in @($o.setup.experience.before,$o.setup.experience.after)){$s.level=4;$s.experience=6000;$s.hasPet=$true}
    $funds=@($o.preservation.rows|Where-Object{$_.purpose.StartsWith('hire-funds')})
    foreach($r in $funds){$r.state.snapshots=1;$r.state.completedSaves=1}
    $xp=Copy-O $funds[0];$xp.sequence=1;$xp.purpose='experience-before';$xp.frame=110
    $o.preservation.rows=@($xp)+$funds
    for($i=0;$i-lt$o.preservation.rows.Count;$i++){$r=$o.preservation.rows[$i];$r.sequence=$i+1;$r.canonicalJson=ConvertTo-Json -InputObject $r.state -Depth 64 -Compress;$r.sha256=Hash-CompositionText $r.canonicalJson}
    foreach($s in @($o.setup.experience.before,$o.setup.experience.after)){$s.preservedStateSha256=$xp.sha256}
    foreach($s in @($o.setup.hireFunds.before,$o.setup.hireFunds.after)){$s.preservedStateSha256=$funds[0].sha256}
    $o.setupAtCleanup=Copy-O $o.setup
    $o.nativeWrites=@($o.nativeWrites[0],$o.nativeWrites[2]);$manual=$o.nativeWrites[1]
    foreach($key in @('completionSequence','snapshots','completedSaves')){$manual.$key=2}
    foreach($a in @($manual.authority,$o.savedWorld.authority,$o.settledWorld.authority,$o.cleanup.authority)){$a.levelupWrites=0;$a.reservationCount=1;$a.admittedNativeWrites=2;$a.authorizedWrites=2}
    $o.fieldWorld.authority.levelupWrites=0;$o.fieldWorld.authority.reservationCount=0;$o.fieldWorld.authority.admittedNativeWrites=1;$o.fieldWorld.authority.authorizedWrites=1
    $o.finalManual=Copy-O $manual
    foreach($rows in @($o.nativeNumbering.sourceDescriptors,$o.settledDescriptors)){
        $a=@($rows|Where-Object fileName -CEQ 'Auto_1.zks')[0];$a.descriptor=$o.nativeWrites[0].preparedDescriptor;$a.autoLevelup=$false
    }
    $a=@($o.fixtureReadiness.isolatedSaveDescriptors.descriptors|Where-Object fileName -CEQ 'Auto_1.zks')[0];$a.isAutoLevelupSave=$false
    foreach($e in $o.manualProbe.events){$e.snapshotCount--;$e.completedSaveCount--}
    foreach($key in @('builderComplete','ordinaryProbe','ordinaryIdentityBridge','afterLevelupDescriptors')){$o.PSObject.Properties.Remove($key)}
    $o.choices=@()
    $f
}
