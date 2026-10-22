[CmdletBinding()]
param([string]$OutputDirectory)
# Six-mounted native Manual producer and its separate-process cold consumer, offline:
# selection/descriptor/scenario/quota contracts, the actual routed Manual308 gate for the
# producer, the reserved Manual309 leaf, and the closed six-pair persistence reader and
# cold proof on generated producer/consumer evidence. No native run, archive or producer
# receipt exists for these cases yet; nothing here is native qualification.
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Initialize-Chunk7Tests.ps1')
if(-not$OutputDirectory){$OutputDirectory=Join-Path $chunk7AnalysisRoot ('six-mounted-persistence-'+[Guid]::NewGuid().ToString('N'))}
$OutputDirectory=Assert-KmcChildPath $OutputDirectory $chunk7AnalysisRoot 'six-mounted persistence checks'
if(Test-Path -LiteralPath $OutputDirectory){throw 'Preserve prior six-mounted persistence checks.'};[void][IO.Directory]::CreateDirectory($OutputDirectory)
function Clone($Value){
 $decoded=$Value|ConvertTo-Json -Depth 90|ConvertFrom-Json
 if($decoded-is[Array]){foreach($item in $decoded){$item}}else{$decoded}
}
$script:pass=0;$script:fail=0
function Check([string]$Name,[bool]$Expected,[scriptblock]$Action){
 $actual=$false;$detail=$null
 try{&$Action;$actual=$true}catch{$detail=$_.Exception.Message}
 if($actual-eq$Expected){$script:pass++;Write-Output ('PASS '+$Name)}else{$script:fail++;Write-Output ('FAIL '+$Name+': '+$detail)}
}
$sourceRequest=Read-KmcJson (Join-Path (Get-KmcLabRoot) 'runtime-evidence/20261022-c7b-world185-b185s1-sixthhorse-cold/runtime-request.json')
$roles=@(Get-KmcChunk7SixPairRoles);$riders=@(for($i=0;$i-lt6;$i++){$roles[$i*2]})
$producerRun='generated-six-mounted-producer';$coldRun='generated-six-mounted-cold'
$mountedSha='7'*64;$producerArtifactSha='9'*64
function New-SixRequest([bool]$Cold){
 $r=Clone $sourceRequest;$r.scenario='chunk7-owned-two-pair-campaign';$r.runId=$(if($Cold){$coldRun}else{$producerRun})
 $r.evidenceRoot=Join-Path (Get-KmcLabRoot) ('runtime-evidence/'+$r.runId)
 $r.PSObject.Properties.Remove('assistedColdInventory');$r|Add-Member persistenceCase 'multi-exploration-manual'
 $r|Add-Member ownedPairs ([pscustomobject][ordered]@{case=$(if($Cold){'six-horse-mounted-persistence-load'}else{'six-horse-mounted-persistence-save'})
  sourceRunId=$sourceRequest.runId;sourceArtifactSha256='012d1caff175d34614fcd255f0b6ad897e0ef1a9650791cb3c0d55ce13b47241';sourceArchiveSha256=$sourceRequest.persistenceLoad.sha256
  checkpointSourceRunId=$(if($Cold){$producerRun}else{$null});checkpointSourceArtifactSha256=$(if($Cold){$producerArtifactSha}else{$null})
  riderAId=$roles[0];mountAId=$roles[1];riderBId=$roles[2];mountBId=$roles[3];partyIds=$roles;baseGameOnly=$true;expectedNativeLoadCount=1;expectedNativeWriteCount=$(if($Cold){0}else{1})})
 if($Cold){$d=$sourceRequest.persistenceLoad
  $r.persistenceLoad=[pscustomobject][ordered]@{internalName='KMC_C7_SIX_MOUNTED_WORLD';fileName='Manual_309_KMC_C7_SIX_MOUNTED_WORLD.zks';sha256=$mountedSha
   length=1400000;lastWriteTimeUtcTicks=639283000000000000L;gameId=$d.gameId;gameName=$d.gameName;area=$d.area}}
 $r
}
$save=New-SixRequest $false;$load=New-SixRequest $true

# Selection, descriptor, scenario and quota contracts.
Check 'six-mounted producer selection and Manual308 input admitted' $true {Assert-KmcChunk7OwnedPairsDescriptor $save}
Check 'six-pair cold consumer selection and Manual309 input admitted' $true {Assert-KmcChunk7OwnedPairsDescriptor $load}
Check 'six persistence cases route to their exact scenarios, roles and quotas' $true {
 Assert-KmcBootstrapInventoryString (Get-KmcChunk7SelectedScenario $save) 'chunk7-persistence-save'
 Assert-KmcBootstrapInventoryString (Get-KmcChunk7SelectedScenario $load) 'chunk7-persistence-load'
 if((Get-KmcChunk7OwnedWriteCount $save)-ne1-or(Get-KmcChunk7OwnedWriteCount $load)-ne0-or(Get-KmcChunk7OwnedLoadCount $save)-ne1-or(Get-KmcChunk7OwnedLoadCount $load)-ne1){throw 'quota'}
 Assert-KmcAssistedSame @(Get-KmcChunk7OwnedPairIds $save) $roles 'producer roles';Assert-KmcAssistedSame @(Get-KmcChunk7OwnedPairIds $load) $roles 'consumer roles'
 Assert-KmcBootstrapInventoryString (Get-KmcChunk7OwnedManualOutputName $save) 'KMC_C7_SIX_MOUNTED_WORLD'
 Assert-KmcBootstrapInventoryString (Get-KmcChunk7OwnedWriteArtifactLeaf $save) 'chunk7-persistence-evidence.json'
 if(-not(Test-KmcChunk7SixPairLineage $save)-or-not(Test-KmcChunk7SixPairLineage $load)-or(Test-KmcChunk7SixPairStress $save)){throw 'lineage'}
}
$selectionFaults=[ordered]@{
 'producer without its checkpoint'={param($r)$r.persistenceCase=$null}
 'producer selecting a quicksave'={param($r)$r.persistenceCase='multi-exploration-quick'}
 'producer with zero writes'={param($r)$r.ownedPairs.expectedNativeWriteCount=0}
 'producer with two writes'={param($r)$r.ownedPairs.expectedNativeWriteCount=2}
 'producer borrowing a producer pin'={param($r)$r.ownedPairs.checkpointSourceRunId=$producerRun;$r.ownedPairs.checkpointSourceArtifactSha256=$producerArtifactSha}
 'producer with another fixture receipt'={param($r)$r.ownedPairs.sourceArtifactSha256='f'*64}
 'producer with another fixture run'={param($r)$r.ownedPairs.sourceRunId='20261022-c7b-world184-b184s1-sixthhorse-manual'}
 'producer with eleven actors'={param($r)$r.ownedPairs.partyIds=@($roles|Select-Object -First 11)}
 'producer with a swapped role'={param($r)$r.ownedPairs.riderBId=$roles[0]}
 'producer with seed pins'={param($r)$r.ownedPairs|Add-Member seedId 'KMC_C7_NO_MODS_OWNER_SEED'}
 'producer with a cold receipt'={param($r)$r.ownedPairs|Add-Member coldReceiptSha256 ('6'*64)}
 'producer aliasing the fixture run'={param($r)$r.runId=$sourceRequest.runId}
 'producer with a substituted Manual308'={param($r)$r.persistenceLoad.sha256='e'*64}
 'producer with another Manual308 length'={param($r)$r.persistenceLoad.length=1332309}
}
foreach($entry in $selectionFaults.GetEnumerator()){$mutate=$entry.Value;Check ('refuses '+$entry.Key) $false {$r=Clone $save;&$mutate $r;Assert-KmcChunk7OwnedPairsDescriptor $r}}
$coldFaults=[ordered]@{
 'cold without its producer'={param($r)$r.ownedPairs.checkpointSourceRunId=$null;$r.ownedPairs.checkpointSourceArtifactSha256=$null}
 'cold naming itself as producer'={param($r)$r.ownedPairs.checkpointSourceRunId=$r.runId}
 'cold naming the fixture as producer'={param($r)$r.ownedPairs.checkpointSourceRunId=$r.ownedPairs.sourceRunId}
 'cold producer receipt aliasing the fixture receipt'={param($r)$r.ownedPairs.checkpointSourceArtifactSha256=$r.ownedPairs.sourceArtifactSha256}
 'cold producer receipt aliasing Manual308'={param($r)$r.ownedPairs.checkpointSourceArtifactSha256=$r.ownedPairs.sourceArchiveSha256}
 'cold with a write'={param($r)$r.ownedPairs.expectedNativeWriteCount=1}
 'cold loading the unmounted Manual308'={param($r)$r.persistenceLoad.sha256=$r.ownedPairs.sourceArchiveSha256}
 'cold loading another Manual number'={param($r)$r.persistenceLoad.fileName='Manual_310_KMC_C7_SIX_MOUNTED_WORLD.zks'}
 'cold loading another Manual name'={param($r)$r.persistenceLoad.internalName='KMC_C7_SIX_HORSE_WORLD';$r.persistenceLoad.fileName='Manual_309_KMC_C7_SIX_HORSE_WORLD.zks'}
 'cold loading the producer receipt bytes'={param($r)$r.persistenceLoad.sha256=$producerArtifactSha}
 'cold in another campaign'={param($r)$r.persistenceLoad.gameId='44444444-1111-4111-8111-111111111111'}
 'cold in another area'={param($r)$r.persistenceLoad.area='fedcba9876543210fedcba9876543210'}
}
foreach($entry in $coldFaults.GetEnumerator()){$mutate=$entry.Value;Check ('refuses '+$entry.Key) $false {$r=Clone $load;&$mutate $r;Assert-KmcChunk7OwnedPairsDescriptor $r}}

# Reserved output: exactly the next native Manual after Manual308.
Check 'producer reserves exactly Manual309 under its own name' $true {
 Assert-KmcBootstrapInventoryString (Get-KmcChunk7OwnedOutputLeaf $save ([pscustomobject]@{ownedOutputAdmission=[pscustomobject]@{number=309;leaf='Manual_309_KMC_C7_SIX_MOUNTED_WORLD.zks'}})) 'Manual_309_KMC_C7_SIX_MOUNTED_WORLD.zks'}
Check 'refuses a skipped native Manual number' $false {[void](Get-KmcChunk7OwnedOutputLeaf $save ([pscustomobject]@{ownedOutputAdmission=[pscustomobject]@{number=310;leaf='Manual_310_KMC_C7_SIX_MOUNTED_WORLD.zks'}}))}
Check 'refuses a reserved leaf under another name' $false {[void](Get-KmcChunk7OwnedOutputLeaf $save ([pscustomobject]@{ownedOutputAdmission=[pscustomobject]@{number=309;leaf='Manual_309_KMC_C7_SIX_HORSE_WORLD.zks'}}))}
Check 'refuses an output leaf for the read-only cold consumer' $false {[void](Get-KmcChunk7OwnedOutputLeaf $load ([pscustomobject]@{ownedOutputAdmission=[pscustomobject]@{number=310;leaf='Manual_310_KMC_C7_SIX_MOUNTED_WORLD.zks'}}))}

# The actual routed producer gate: the batch185 Manual308 qualification itself.
$qualified=$null
Check 'actual routed producer source is the exact Manual308 qualification with its marker' $true {
 $script:qualified=Get-KmcChunk7OwnedPairsSource $save.ownedPairs 'multi-exploration-manual' $save.fixture $save
 if($qualified.archiveProducerRunId-cne'20261022-c7b-world184-b184s1-sixthhorse-manual'-or$qualified.qualificationRunId-cne$sourceRequest.runId-or
    $qualified.sourceRunId-cne$sourceRequest.runId-or$qualified.receiptPins.Count-ne16-or$qualified.nativeAuthorityGranted-ne$false-or$qualified.sixPairCheckpoint-ne$true){throw 'qualification'}
 Assert-KmcAssistedSame $qualified.descriptor $save.persistenceLoad 'producer Manual308 descriptor'
}
if($null-eq$qualified){throw 'The actual routed six-pair gate must pass before generated reader checks.'}
Check 'refuses the producer source with an area target' $false {[void](Get-KmcChunk7OwnedPairsSource $save.ownedPairs 'multi-exploration-manual' $save.fixture $save ([pscustomobject]@{enterPoint='e'*32;area='f'*32;autoSaveMode='AfterEntry'}))}
Check 'refuses a cold source whose producer never ran' $false {[void](Get-KmcChunk7OwnedPairsSource $load.ownedPairs 'multi-exploration-manual' $load.fixture $load)}
# After the actual gate succeeds, only that external qualification boundary is
# substituted for the generated mutations; reader methods stay unmodified.
function Get-KmcChunk7SixPairCheckpointSource($Fixture){Assert-KmcAssistedSame $Fixture $sourceRequest.fixture 'six-mounted source fixture delegation';$qualified}
$settled=(Read-KmcJson $qualified.artifactPath).observations.settledWorld
$arrangement=[pscustomobject]@{money=$settled.nativeMoney;experienceRatePercent=100;actors=@($settled.party|Sort-Object id -CaseSensitive|ForEach-Object{
 [pscustomobject]@{actorId=$_.id;blueprint=$_.blueprint;experience=$_.experience;characterLevel=$_.characterLevel;
  classes=@($_.classes|Sort-Object guid -CaseSensitive|ForEach-Object{[pscustomobject]@{classGuid=$_.guid;level=$_.level}})}})}
$identityPairs=@(for($i=0;$i-lt6;$i++){[pscustomobject][ordered]@{riderId=$roles[$i*2];mountId=$roles[$i*2+1];rider=$i*4+1;mount=$i*4+2;feature=$i*4+3;rankFact=$i*4+4;
 sourceGuid='97261d609529d834eba4fd4da1bc44dc';rank=1;profileId=(Get-KmcChunk7SixPairProfile $i)}})
$remote=[pscustomobject][ordered]@{actorId='8265f8cf-0969-4ec7-a266-2448de91c8c4';object=25;descriptor=26;petId=$null;masterId=$null;
 arrangement=[pscustomobject]@{money=357290;experienceRatePercent=100;actors=@([pscustomobject]@{actorId='8265f8cf-0969-4ec7-a266-2448de91c8c4';blueprint='77c11edb92ce0fd408ad96b40fd27121';experience=9000;characterLevel=4;
  classes=@([pscustomobject]@{classGuid='772c83a25e2268e448e841dcd548235f';level=4})})}}
function Inherited($Request,[string]$Phase){[pscustomobject][ordered]@{contract='cold-qualified-six-horse-native-field-v1';phase=$Phase;sourceRunId=$Request.ownedPairs.sourceRunId;
 sourceArtifactSha256=$Request.ownedPairs.sourceArtifactSha256;sourceArchiveSha256=$Request.ownedPairs.sourceArchiveSha256;sourceCase='sixth-horse-manual-readonly-inventory';
 nativeLoadVerifiedByHost=$true;decodedReceiptAuthorizesRestoration=$false;inheritedPetsPreserved=$true;ownedFactRemovalCount=0;partyIds=$roles;currentPartyIds=$roles;
 currentCharacterIds=$riders;remoteCapturedIds=@();arrangement=(Clone $arrangement);pairs=@(Clone $identityPairs);identities=[pscustomobject]@{capacity=128;ids=@(1..26);faults=0};remote=(Clone $remote)}}
function Debt([string]$Id){[pscustomobject][ordered]@{Id=$Id;Standard=0.0;Move=0.0;Swift=0.0;Initiative=0.0;Reaction=0.0;ReactionsRemaining=1;LastSurpriseTicks=0}}
$ownerGuids=@(for($i=0;$i-lt6;$i++){[Guid]::Parse(('{0:x8}-6a1c-4c00-8000-00000000000{1}' -f (0x51c0+$i),$i))})
$archive=[pscustomobject][ordered]@{SchemaVersion=3;CampaignId=$sourceRequest.persistenceLoad.gameId;AreaId=$sourceRequest.persistenceLoad.area;GameTimeTicks=100000000L;
 Policy='rider-principal-distinct-native-v1';RulesId='crpg-transport-v1'
 Pairs=@(for($i=0;$i-lt6;$i++){[pscustomobject][ordered]@{PairId=$ownerGuids[$i].ToString('D');Generation=1;ProfileId=(Get-KmcChunk7SixPairProfile $i);Rider=(Debt $roles[$i*2]);Mount=(Debt $roles[$i*2+1])}})
 Participations=@();Slots=@(for($i=0;$i-lt6;$i++){[pscustomobject][ordered]@{ActorId=$roles[$i*2];Index=10;Kind=2};[pscustomobject][ordered]@{ActorId=$roles[$i*2];Index=11;Kind=3}});Combat=$null}
function OwnerRow([int]$Index){[pscustomobject][ordered]@{ownerId=$ownerGuids[$Index].ToString('N');generation=1;riderId=$roles[$Index*2];mountId=$roles[$Index*2+1];state='Active'}}
function Capture([string]$Label,[int]$Owners,[long]$Ticks,[int]$Semantic,[int]$Presentation,[int]$Snapshots,[int]$Casts,[string]$Phase='not-selected'){
 [pscustomobject][ordered]@{label=$Label;frame=1000;gameTicks=$Ticks;owners=@(for($i=0;$i-lt$Owners;$i++){OwnerRow $i})
  actors=@($roles|ForEach-Object{[pscustomobject][ordered]@{native=(Debt $_);prepared=$false;inCombat=$false}})
  controls=[pscustomobject][ordered]@{DuplicateFactCount=0;SerializationSuspended=$false;ManagedHotbarSlotCount=$(if($Owners-eq6){12}else{0})}
  semanticRestores=$Semantic;presentationRestores=$Presentation;nativePostLoads=$Semantic;snapshots=$Snapshots;nativeCastRequests=$Casts;archivePhase=$Phase}}
function Movements{@(for($i=0;$i-lt6;$i++){[pscustomobject][ordered]@{label=('six-pair-'+$i);selectedId=$roles[$i*2];executorId=$roles[$i*2+1];otherCommandsUnchanged=$true
 start=[pscustomobject]@{x=[double]$i;y=0.0;z=1.0};goal=[pscustomobject]@{x=[double]$i+2;y=0.0;z=1.0};end=[pscustomobject]@{x=[double]$i+2;y=0.0;z=1.0};terminal=$true;distance=2.0;residual=0.0}})}
function Cleanup{[pscustomobject][ordered]@{pairsClear=$true;sourceHorseRemoved=$false;inheritedPetsPreserved=$true;controlsSettled=$true;saveCustodyClear=$true;selectionRestored=$true
 nativeProgressionRequiresExternalWorkingRestoration=$false;ordinaryRespecProven=$false;world=(Capture 'cleanup' 0 100000000L 0 0 0 0)}}
function OwnerRows{@(for($i=0;$i-lt6;$i++){[pscustomobject][ordered]@{index=$i;riderId=$roles[$i*2];mountId=$roles[$i*2+1];ownerId=$ownerGuids[$i].ToString('N');generation=1;profileId=(Get-KmcChunk7SixPairProfile $i)}})}
function Envelope($Request,$Observations,[string]$Status='PASS'){[pscustomobject][ordered]@{schemaVersion=1;evidenceKind='chunk7-persistence-evidence';runId=$Request.runId;scenario=$Request.scenario
 branch=$Request.branch;commit=$Request.commit;productVersion=$Request.productVersion;dllSha256=$Request.dllSha256;dllMvid=$Request.dllMvid;createdAtUtc='2026-10-22T20:00:00.0000000+00:00'
 status=$Status;observations=$Observations;errors=@()}}
function Common($Request,[bool]$Cold,[int]$ProcessId){[ordered]@{checkpoint='multi-exploration-manual';cold=$Cold;sourceWorkingSha256=$null;inheritedManualSha256=$Request.ownedPairs.sourceArchiveSha256
 processId=$ProcessId;snapshots=@();inputs=@();movements=@(Movements);nativeProgressionRequiresExternalWorkingRestoration=$false;ordinaryRespecProven=$false;combatDebtProof=$false
 nativeSchema=3;nativeCapacity=6;inheritedFixture=(Inherited $Request 'admitted');sixPairPersistence=$null;inheritedCleanup=(Inherited $Request 'cleanup');cleanup=(Cleanup)
 elapsedSeconds=95.5;nativeShellLifecycle='settled'}}
$saveRoot=Join-Path (Join-Path (Join-Path (Split-Path (Split-Path $save.evidenceRoot -Parent) -Parent) 'runtime-staging') ('persistence-'+$save.runId)) 'Saved Games'
$leaf='Manual_309_KMC_C7_SIX_MOUNTED_WORLD.zks'
function Inventory([string]$File,[string]$Name){[pscustomobject][ordered]@{fileName=$File;nativeName=$Name;nativeType='Manual';gameId=$save.persistenceLoad.gameId
 gameName=$save.persistenceLoad.gameName;area=$save.persistenceLoad.area;hasFileOnDisk=$true;isActuallySaved=$true;operation='None';autoLevelup=$false}}
function Authority([int]$Writes){[pscustomobject][ordered]@{root=$saveRoot;loadCap=1;writeCap=1;nativeLoads=1;nativeWrites=$Writes;authorizedLoads=1;authorizedWrites=$Writes
 reservationCount=1;number=309;leaf=$leaf;nativeName='KMC_C7_SIX_MOUNTED_WORLD';nativeType='Manual'}}
function New-ProducerArtifact{
 $o=Common $save $false 4100
 $o.snapshots=@((Capture 'before-native-save' 6 99000000L 0 0 0 6),(Capture 'native-write-complete' 6 101000000L 0 0 1 6),(Capture 'continuation-complete' 6 140000000L 0 0 1 6))
 $o.inputs=@(for($i=0;$i-lt6;$i++){[pscustomobject][ordered]@{label=('six-mount-'+$i);before=(Capture 'input-before' $i 90000000L 0 0 0 $i);casterId=$roles[$i*2];targetId=$roles[$i*2+1]
  abilityGuid='f053faad986631688defa003cd7bda0e';clicked=$true;nativeCastDelta=1;nativeRefusalDelta=0;generationAtInit=0;control='MountCompanion';selectionAfterInput=$roles[(($i+1)%6)*2]
  terminal=$true;after=(Capture 'input-after' ($i+1) 91000000L 0 0 0 ($i+1))}})
 $o.sixPairPersistence=[pscustomobject][ordered]@{contract='six-horse-mounted-manual-producer-v1';fixtureRunId=$save.ownedPairs.sourceRunId;requiredPairs=6;requiredSlots=12
  requiredMountInputs=6;requiredGroundInputs=6;nativeLoadRequests=1;nativeSaveRequests=1;fixtureSetupWrites=0;performanceAccepted=$false;requestFreeControlsProven=$false
  managedHotbarSlotsBeforeBinding=0;savedPairs=6;savedSlots=12;owners=@(OwnerRows);nativeCastRequestsBeforeSave=6;nativeRefusalsBeforeSave=0;nativeCastRequests=6;nativeRefusals=0;groundInputs=6}
 $o.ownedOutputAdmission=[pscustomobject][ordered]@{method='FindUnusedSaveNumber';token='06008024';moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7';number=309;leaf=$leaf
  nativeInventory=@(Inventory $save.persistenceLoad.fileName $save.persistenceLoad.internalName);authority=(Authority 0)}
 $o.saveRequested=[pscustomobject][ordered]@{nativeType='Manual';nativeName='KMC_C7_SIX_MOUNTED_WORLD';gameTicks=100000000L;snapshotCount=0;nativeSaveAllowed=$true;newSlot=$true}
 $o.ownedOutputCompletion=[pscustomobject][ordered]@{completionSequence=1;callbackCount=1;nativeWorkerSettled=$true
  nativeInventory=@((Inventory $save.persistenceLoad.fileName $save.persistenceLoad.internalName),(Inventory $leaf 'KMC_C7_SIX_MOUNTED_WORLD'));authority=(Authority 1)}
 $o.nativeWrite=[pscustomobject][ordered]@{path=(Join-Path $saveRoot $leaf);sha256=$mountedSha;length=1400000;nativeType='Manual';nativeName='KMC_C7_SIX_MOUNTED_WORLD'
  nativeCallback=$true;operation='None';snapshotCount=1;archive=(Clone $archive)}
 Envelope $save ([pscustomobject]$o)
}
function ProbeEvent([string]$Kind,$Saved){[pscustomobject][ordered]@{kind=$Kind;actorId=$Saved.Id;frame=500;gameTicks=100000000L;restorePhase='validated-before-native-mutation'
 restorationPending=$true;debt=(Clone $Saved);prepared=$false;inCombat=$false}}
function New-ColdArtifact{
 $o=Common $load $true 5200
 $o.snapshots=@((Capture 'cold-restored-before-input' 6 100500000L 12 6 0 0 'all-pair-presentation-restored'),(Capture 'continuation-complete' 6 130000000L 12 6 0 0 'all-pair-presentation-restored'))
 $o.sixPairPersistence=[pscustomobject][ordered]@{contract='six-horse-mounted-manual-cold-v1';fixtureRunId=$load.ownedPairs.sourceRunId;producerRunId=$producerRun
  producerArtifactSha256=$producerArtifactSha;requiredPairs=6;requiredSlots=12;requiredGroundInputs=6;nativeLoadRequests=1;nativeSaveRequests=0;semanticRestores=12
  presentationRestores=6;nativePostLoads=12;owners=@(OwnerRows);performanceAccepted=$false;requestFreeControlsProven=$false;nativeCastRequests=0;nativeRefusals=0;groundInputs=6}
 $saved=@($archive.Pairs|ForEach-Object{$_.Rider;$_.Mount})
 $o.coldRestoration=[pscustomobject][ordered]@{contract='chunk7-native-restoration-entry-observer-v1'
  events=@($saved|ForEach-Object{(ProbeEvent 'restore-debt-before' $_);(ProbeEvent 'restore-debt-after' $_)})
  hooks=@([pscustomobject][ordered]@{declaringType='TurnBased.Controllers.TurnController';method='Prepare';token='06000C3C';moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7';prefix='PrepareBefore';postfix='PrepareAfter'},
   [pscustomobject][ordered]@{declaringType='Kingmaker.Controllers.Combat.UnitCombatState';method='NewRound';token='0600939D';moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7';prefix='RoundBefore';postfix='RoundAfter'},
   [pscustomobject][ordered]@{declaringType='KingmakerMountedCombat.Integration.NativeCombatActorPersistence';method='RestoreActor';token='06001000';moduleMvid=$load.dllMvid;prefix='RestoreActorBefore';postfix='RestoreActorAfter'},
   [pscustomobject][ordered]@{declaringType='KingmakerMountedCombat.Integration.NativeCombatActorPersistence';method='RestoreDebt';token='06001001';moduleMvid=$load.dllMvid;prefix='RestoreDebtBefore';postfix='RestoreDebtAfter'})
  maximumEvents=2048;dropped=0;errors=0;complete=$true;installedBeforeWorldLoad=$true;prepareMetric='native-method-entries-including-production-refused-attempts'}
 $o.loadedArchive=(Clone $archive);$o.loadedDescriptor=(Clone $load.persistenceLoad)
 Envelope $load ([pscustomobject]$o)
}
$producer=New-ProducerArtifact;$cold=New-ColdArtifact
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'generated-producer.json'),($producer|ConvertTo-Json -Depth 90))
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'generated-cold.json'),($cold|ConvertTo-Json -Depth 90))
Check 'generated six-mounted producer evidence passes the closed reader' $true {Assert-KmcChunk7PersistenceObservation $save (Clone $producer) 'PASS'}
Check 'generated six-pair cold evidence passes the closed reader' $true {Assert-KmcChunk7PersistenceObservation $load (Clone $cold) 'PASS'}
Check 'six-pair separate-process cold proof accepts the exact producer graph' $true {Assert-KmcChunk7SixPairMountedColdSource $load (Clone $producer) (Clone $cold)}
Check 'six-pair evidence is refused for another persistence case' $false {$r=Clone $save;$r.ownedPairs.case='persistence-save';Assert-KmcChunk7SixPairPersistenceObservation $r (Clone $producer)}

$producerFaults=[ordered]@{
 'a FAIL producer artifact'={param($a)$a.status='FAIL';$a.errors=@('failed')}
 'five archived pairs'={param($a)$a.observations.nativeWrite.archive.Pairs=@($a.observations.nativeWrite.archive.Pairs|Select-Object -First 5)}
 'a swapped archived mount'={param($a)$a.observations.nativeWrite.archive.Pairs[0].Mount.Id=$roles[3]}
 'a changed archived profile'={param($a)$a.observations.nativeWrite.archive.Pairs[1].ProfileId='medium-humanoid-horse-v1'}
 'a foreign archived slot kind'={param($a)$a.observations.nativeWrite.archive.Slots[0].Kind=4}
 'a missing archived slot'={param($a)$a.observations.nativeWrite.archive.Slots=@($a.observations.nativeWrite.archive.Slots|Select-Object -Skip 1)}
 'archived combat'={param($a)$a.observations.nativeWrite.archive.Combat=[pscustomobject]@{TurnBased=$false}}
 'an archived participation'={param($a)$a.observations.nativeWrite.archive.Participations=@([pscustomobject]@{OwnerId='x'})}
 'a duplicated archived owner'={param($a)$a.observations.nativeWrite.archive.Pairs[1].PairId=$a.observations.nativeWrite.archive.Pairs[0].PairId}
 'a changed recorded owner generation'={param($a)$a.observations.sixPairPersistence.owners[2].generation=2}
 'an undeclared native cast'={param($a)$a.observations.sixPairPersistence.nativeCastRequests=7}
 'claimed performance acceptance'={param($a)$a.observations.sixPairPersistence.performanceAccepted=$true}
 'managed hotbar controls before binding'={param($a)$a.observations.sixPairPersistence.managedHotbarSlotsBeforeBinding=1}
 'five Mount inputs'={param($a)$a.observations.inputs=@($a.observations.inputs|Select-Object -First 5)}
 'a relabelled Mount input'={param($a)$a.observations.inputs[3].label='mount-horse'}
 'a Mount on another role'={param($a)$a.observations.inputs[2].targetId=$roles[1]}
 'a Mount refused natively'={param($a)$a.observations.inputs[4].nativeRefusalDelta=1}
 'a wrong post-input selection'={param($a)$a.observations.inputs[5].selectionAfterInput=$roles[2]}
 'an owner minted out of order'={param($a)$a.observations.inputs[2].after.owners=@($a.observations.inputs[2].after.owners|Select-Object -First 2)}
 'an extra owner during a Mount'={param($a)$a.observations.inputs[2].after.owners+=@(Clone $a.observations.inputs[5].after.owners[5])}
 'an owner missing before a Mount'={param($a)$a.observations.inputs[3].before.owners=@($a.observations.inputs[3].before.owners|Select-Object -First 2)}
 'a consistently changed declared profile'={param($a)$a.observations.nativeWrite.archive.Pairs[1].ProfileId='medium-humanoid-horse-v1';$a.observations.sixPairPersistence.owners[1].profileId='medium-humanoid-horse-v1'}
 'an owner reminted before the save'={param($a)$a.observations.inputs[0].after.owners[0].generation=3}
 'five ground continuations'={param($a)$a.observations.movements=@($a.observations.movements|Select-Object -First 5)}
 'a continuation routed to the rider'={param($a)$a.observations.movements[1].executorId=$roles[2]}
 'a continuation goal outside its fixed offset'={param($a)$a.observations.movements[2].goal.x=9.0;$a.observations.movements[2].end.x=9.0;$a.observations.movements[2].distance=9.0-2}
 'a continuation missing its endpoint'={param($a)$a.observations.movements[3].end.x=0.0;$a.observations.movements[3].distance=3.0;$a.observations.movements[3].residual=2.0}
 'a missing continuation snapshot'={param($a)$a.observations.snapshots=@($a.observations.snapshots|Select-Object -First 2)}
 'reordered snapshots'={param($a)$a.observations.snapshots=@($a.observations.snapshots[1],$a.observations.snapshots[0],$a.observations.snapshots[2])}
 'a changed owner at the write boundary'={param($a)$a.observations.snapshots[1].owners[4].generation=2}
 'a replayed Mount before the save'={param($a)$a.observations.snapshots[0].nativeCastRequests=7}
 'a second save during continuation'={param($a)$a.observations.snapshots[2].snapshots=2}
 'debt beyond elapsed native clock'={param($a)$a.observations.snapshots[1].actors[0].native.Standard=6.0}
 'a renamed save request'={param($a)$a.observations.saveRequested.nativeName='KMC_C7_WORLD'}
 'a callback-free native write'={param($a)$a.observations.nativeWrite.nativeCallback=$false}
 'a written Manual aliasing Manual308'={param($a)$a.observations.nativeWrite.sha256=$save.ownedPairs.sourceArchiveSha256}
 'a skipped reserved number'={param($a)$a.observations.ownedOutputAdmission.number=310}
 'an extra Manticore observation'={param($a)$a.observations|Add-Member manticoreFixturePreparation ([pscustomobject]@{})}
 'a producer claiming a loaded archive'={param($a)$a.observations|Add-Member loadedArchive (Clone $archive)}
 'a missing six-pair observation'={param($a)$a.observations.PSObject.Properties.Remove('sixPairPersistence')}
 'capacity two'={param($a)$a.observations.nativeCapacity=2}
 'retained pair custody at cleanup'={param($a)$a.observations.cleanup.pairsClear=$false}
 'retained owners at cleanup'={param($a)$a.observations.cleanup.world.owners=@(OwnerRow 0)}
 'changed progression at cleanup'={param($a)$a.observations.inheritedCleanup.arrangement.money=1}
 'an unbounded scenario window'={param($a)$a.observations.elapsedSeconds=400.0}
 'a historical Working identity'={param($a)$a.observations.sourceWorkingSha256='5'*64}
}
foreach($entry in $producerFaults.GetEnumerator()){$mutate=$entry.Value;Check ('producer reader refuses '+$entry.Key) $false {$a=Clone $producer;&$mutate $a;Assert-KmcChunk7PersistenceObservation $save $a 'PASS'}}
$coldFaultsReader=[ordered]@{
 'another loaded descriptor'={param($a)$a.observations.loadedDescriptor.sha256='6'*64}
 'eleven semantic restores'={param($a)$a.observations.snapshots[0].semanticRestores=11}
 'five presentation restores'={param($a)$a.observations.snapshots[0].presentationRestores=5}
 'a restored phase short of presentation'={param($a)$a.observations.snapshots[0].archivePhase='all-native-actor-state-restored'}
 'native preparation during the cold load'={param($a)$a.observations.coldRestoration.events+=@(ProbeEvent 'prepare-before' $archive.Pairs[0].Rider)}
 'a combat actor restoration'={param($a)$a.observations.coldRestoration.events+=@(ProbeEvent 'restore-actor-before' $archive.Pairs[0].Mount)}
 'a missing debt restore'={param($a)$a.observations.coldRestoration.events=@($a.observations.coldRestoration.events|Select-Object -SkipLast 1)}
 'a repeated debt restore'={param($a)$a.observations.coldRestoration.events+=@(ProbeEvent 'restore-debt-after' $archive.Pairs[5].Mount)}
 'a restored debt differing from the archive'={param($a)$a.observations.coldRestoration.events[1].debt.Standard=1.0}
 'an extra observer hook'={param($a)$a.observations.coldRestoration.hooks+=@($a.observations.coldRestoration.hooks[0])}
 'a restore hook from another candidate'={param($a)$a.observations.coldRestoration.hooks[3].moduleMvid='00000000-0000-0000-0000-000000000000'}
 'an incomplete observer'={param($a)$a.observations.coldRestoration.complete=$false}
 'a replayed Mount input'={param($a)$a.observations.inputs=@($producer.observations.inputs[0])}
 'another producer run'={param($a)$a.observations.sixPairPersistence.producerRunId='other-producer'}
 'another producer receipt'={param($a)$a.observations.sixPairPersistence.producerArtifactSha256='5'*64}
 'a cast during cold continuation'={param($a)$a.observations.snapshots[1].nativeCastRequests=1}
 'a cold write'={param($a)$a.observations|Add-Member nativeWrite (Clone $producer.observations.nativeWrite)}
 'a changed restored owner'={param($a)$a.observations.loadedArchive.Pairs[0].Generation=2}
 'a cold archive with five pairs'={param($a)$a.observations.loadedArchive.Pairs=@($a.observations.loadedArchive.Pairs|Select-Object -First 5)}
 'a cold archive with a consistently changed declared profile'={param($a)$a.observations.loadedArchive.Pairs[4].ProfileId='medium-humanoid-horse-v1';$a.observations.sixPairPersistence.owners[4].profileId='medium-humanoid-horse-v1'}
 'a cold continuation through five pairs'={param($a)$a.observations.movements=@($a.observations.movements|Select-Object -First 5)}
}
foreach($entry in $coldFaultsReader.GetEnumerator()){$mutate=$entry.Value;Check ('cold reader refuses '+$entry.Key) $false {$a=Clone $cold;&$mutate $a;Assert-KmcChunk7PersistenceObservation $load $a 'PASS'}}
$proofFaults=[ordered]@{
 'the same process'={param($p,$c)$c.observations.processId=$p.observations.processId}
 'another restored graph'={param($p,$c)$c.observations.loadedArchive.GameTimeTicks=100000001L}
 'a FAIL producer'={param($p,$c)$p.status='FAIL'}
 'another producer candidate'={param($p,$c)$p.dllMvid='00000000-0000-0000-0000-000000000000'}
 'another producer run'={param($p,$c)$p.runId='other-producer'}
 'another written Manual'={param($p,$c)$p.observations.nativeWrite.sha256='5'*64}
 'a written Manual outside the producer root'={param($p,$c)$p.observations.nativeWrite.path='C:\Other\Manual_309_KMC_C7_SIX_MOUNTED_WORLD.zks'}
 'reminted restored owners'={param($p,$c)$c.observations.sixPairPersistence.owners[0].ownerId=('1'*32)}
 'a changed cold roster'={param($p,$c)$c.observations.inheritedFixture.currentCharacterIds=@($riders|Select-Object -First 5)}
 'changed cold progression'={param($p,$c)$c.observations.inheritedFixture.arrangement.money=1}
 'a changed remote companion'={param($p,$c)$c.observations.inheritedFixture.remote.arrangement.money=1}
 'changed pair provenance'={param($p,$c)$c.observations.inheritedFixture.pairs[4].profileId='medium-humanoid-horse-v1'}
 'a producer under another contract'={param($p,$c)$p.observations.sixPairPersistence.contract='six-horse-cold-fixture-lifecycle-v1'}
 'a cold consumer under another contract'={param($p,$c)$c.observations.sixPairPersistence.contract='six-horse-mounted-manual-producer-v1'}
}
foreach($entry in $proofFaults.GetEnumerator()){$mutate=$entry.Value;Check ('cold proof refuses '+$entry.Key) $false {$p=Clone $producer;$c=Clone $cold;&$mutate $p $c;Assert-KmcChunk7SixPairMountedColdSource $load $p $c}}
Check 'cold proof refuses the producer request' $false {Assert-KmcChunk7SixPairMountedColdSource $save (Clone $producer) (Clone $cold)}

[ordered]@{nativeInvoked=$false;producerReceiptExists=$false;passes=$pass;failures=$fail
 inputs=@('scripts/runtime/Chunk7SixPairPersistenceEvidence.ps1','scripts/runtime/Chunk7OwnedPairsEvidence.ps1','scripts/runtime/Chunk7PersistenceEvidence.ps1','scripts/runtime/Chunk7OwnedPairsFixtures.ps1')|ForEach-Object{[ordered]@{path=$_;sha256=(Get-KmcSha256 (Join-Path $repo $_))}}
 limits='Generated producer/consumer evidence and the actual b185 producer gate only; the cold gate path through a completed producer run cannot be exercised before that run exists.'}|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
Write-Output ('SIX-MOUNTED PERSISTENCE PASS='+$pass+' FAIL='+$fail+'; generated evidence and actual b185 gate only, no native qualification')
if($fail-ne0){throw 'Six-mounted persistence contracts regressed.'}
