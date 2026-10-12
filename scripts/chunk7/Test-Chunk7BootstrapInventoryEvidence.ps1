[CmdletBinding()]
param([string]$RepositoryRoot,[string]$ReaderDirectory,[string]$FixturePath,[string]$OutputDirectory,[switch]$FixturesOnly)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('ReaderDirectory')){$ReaderDirectory=Join-Path $RepositoryRoot 'scripts/runtime'}
if(-not$PSBoundParameters.ContainsKey('FixturePath')){$FixturePath=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/BootstrapInventoryEvidenceFixture.json'}
if(-not$PSBoundParameters.ContainsKey('OutputDirectory')){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/bootstrap-inventory-reader-'+[Guid]::NewGuid().ToString('N'))}
$bridgeReader=[IO.Path]::GetFullPath($ReaderDirectory);$bridgeFixture=[IO.Path]::GetFullPath($FixturePath);$bridgeOutput=[IO.Path]::GetFullPath($OutputDirectory)
. (Join-Path $bridgeReader 'RuntimeHarness.Common.ps1')
function New-BridgeFixture {[IO.File]::ReadAllText($bridgeFixture)|ConvertFrom-Json}
function Check-Bridge($f){Assert-KmcChunk7BootstrapInventoryEvidence $f.request $f.artifact $f.source}
if($FixturesOnly){return}
[void][IO.Directory]::CreateDirectory($bridgeOutput);$script:passes=0;$script:failures=0
function Good([string]$Name,[scriptblock]$Action){try{&$Action;$script:passes++;Write-Host ('PASS '+$Name)}catch{$script:failures++;Write-Host ('FAIL '+$Name+': '+$_)}}
function Bad([string]$Name,[scriptblock]$Action){$rejected=$false;try{&$Action}catch{$rejected=$true};if($rejected){$script:passes++;Write-Host ('PASS rejects '+$Name)}else{$script:failures++;Write-Host ('FAIL accepted '+$Name)}}
Good 'exact native cold inventory without grants' {$f=New-BridgeFixture;Check-Bridge $f}
Good 'pre-PostLoad main reference may be unresolved' {$f=New-BridgeFixture;$f.artifact.observations.nativeLoad.events[0].mainActorId=$null;Check-Bridge $f}
$cases=[ordered]@{
 'native default hire XP exceeds Int32'={param($f)$f.artifact.observations.nativeProfileInventory.ordinaryAcquisitionPrerequisites.nativeDefaultHireExperienceNow=2147483648}
 'state aliases game consistently'={param($f)$f.artifact.observations.loadedWorld.state=$f.artifact.observations.loadedWorld.game;$f.artifact.observations.settledWorld.state=$f.artifact.observations.settledWorld.game}
 'actor aliases world state consistently'={param($f)$f.artifact.observations.loadedWorld.party[0].object=$f.artifact.observations.loadedWorld.state;$f.artifact.observations.settledWorld.party[0].object=$f.artifact.observations.settledWorld.state}
 'manager aliases game throughout'={param($f)$o=$f.artifact.observations;$o.beforeLoad.saveManager=$o.beforeLoad.game;$o.loadedWorld.saveManager=$o.loadedWorld.game;$o.settledWorld.saveManager=$o.settledWorld.game;foreach($e in $o.nativeLoad.events){$e.saveManager=$e.game}}
 'before-load menu player aliases game'={param($f)$f.artifact.observations.beforeLoad.menuPlayer=$f.artifact.observations.beforeLoad.game}
 'source candidate'={param($f)$f.source.dllSha256='e'*64}
 'same native process'={param($f)$f.artifact.observations.processId=$f.source.observations.processId}
 'native preparation declaring type'={param($f)$f.artifact.observations.noPreparation.hooks[0].declaringType='Foreign.Controller'}
 'source run'={param($f)$f.source.runId='foreign'}
 'source FAIL'={param($f)$f.source.status='FAIL'}
 'source world substituted'={param($f)$f.artifact.observations.sourceNativeWorld.mainActorId='aaaaaaaa-1111-4111-8111-111111111111'}
 'same source and cold run'={param($f)$f.request.bootstrapInventory.sourceRunId=$f.request.runId}
 'source body alias'={param($f)$f.request.bootstrapInventory.sourceArchiveSha256=$f.request.fixture.working.sha256;$f.request.persistenceLoad.sha256=$f.request.fixture.working.sha256}
 'Working campaign'={param($f)$f.request.persistenceLoad.gameId=$f.request.fixture.working.gameId}
 'write permission'={param($f)$f.request.bootstrapInventory.expectedNativeWriteCount=1}
 'second load permission'={param($f)$f.request.bootstrapInventory.expectedNativeLoadCount=2}
 'not base-only'={param($f)$f.request.bootstrapInventory.baseGameOnly=$false}
 'extra selection authority'={param($f)$f.request.bootstrapInventory|Add-Member -NotePropertyName extraGrant -NotePropertyValue $true}
 'unobserved native entry'={param($f)$f.artifact.observations.nativeLoad.entries=0}
 'second native exit'={param($f)$f.artifact.observations.nativeLoad.exits=2}
 'foreign exact hook'={param($f)$f.artifact.observations.nativeLoad.hooks[0].token='06000DC8'}
 'foreign installed MVID'={param($f)$f.artifact.observations.nativeLoad.hooks[0].moduleMvid=$f.request.dllMvid}
 'postload different Player'={param($f)$f.artifact.observations.nativeLoad.events[1].player=3}
 'postload different manager'={param($f)$f.artifact.observations.nativeLoad.events[1].saveManager=1}
 'postload different frame'={param($f)$f.artifact.observations.nativeLoad.events[1].frame=11}
 'registry fault'={param($f)$f.artifact.observations.nativeLoad.identities.faults=1}
 'registry released too early'={param($f)$f.artifact.observations.nativeLoad.identities.released=$true}
 'registry label missing'={param($f)$f.artifact.observations.nativeLoad.identities.ids=@(1,2,3,4,5,6,7,8,9);$f.artifact.observations.nativeLoad.identities.retainedCount=9}
 'actor object aliases view'={param($f)$f.artifact.observations.loadedWorld.party[0].object=10}
 'source blueprint mismatch'={param($f)$f.artifact.observations.loadedWorld.party[0].blueprint='a'*32}
 'final native view replaced'={param($f)$f.artifact.observations.settledWorld.party[0].view=11}
 'final world replaced'={param($f)$f.artifact.observations.settledWorld.player=3}
 'native preparation entry'={param($f)$f.artifact.observations.noPreparation.events=@([pscustomobject]@{kind='prepare-before'})}
 'native round entry'={param($f)$f.artifact.observations.noPreparation.events=@([pscustomobject]@{kind='round-before'})}
 'actor debt restore entry'={param($f)$f.artifact.observations.noPreparation.events=@([pscustomobject]@{kind='restore-debt-before'})}
 'product restore hook candidate'={param($f)$f.artifact.observations.noPreparation.hooks[2].moduleMvid='11111111-1111-4111-8111-111111111111'}
 'schema2 silently admitted'={param($f)$f.artifact.observations.archive.schemaVersion=2}
 'decoded owner admitted'={param($f)$f.artifact.observations.archive.pairs=1}
 'decoded participation admitted'={param($f)$f.artifact.observations.archive.participations=1}
 'decoded slot admitted'={param($f)$f.artifact.observations.archive.slots=1}
 'decode grants restoration'={param($f)$f.artifact.observations.archive.nativeRestorationGrantedByDiagnostic=$true}
 'money grant claim'={param($f)$f.artifact.observations.moneyWrites=1}
 'XP grant claim'={param($f)$f.artifact.observations.xpWrites=1}
 'action grant claim'={param($f)$f.artifact.observations.resourceWrites=1}
 'six pairs qualified'={param($f)$f.artifact.observations.sixPairFixtureQualified=$true}
 'Small profile autoenabled'={param($f)$f.artifact.observations.nativeProfileInventory.profileAdmissionChanged=$true}
 'purchase authorized'={param($f)$f.artifact.observations.nativeProfileInventory.ordinaryAcquisitionPrerequisites.purchaseAuthorized=$true}
 'invented affordability'={param($f)$f.artifact.observations.nativeProfileInventory.ordinaryAcquisitionPrerequisites.nativeAffordableNow=$true}
 'foreign field character'={param($f)$f.artifact.observations.nativeProfileInventory.ordinaryAcquisitionPrerequisites.nativePartyCharacterIds=@('aaaaaaaa-1111-4111-8111-111111111111')}
 'profile main XP mismatch'={param($f)$f.artifact.observations.nativeProfileInventory.ordinaryAcquisitionPrerequisites.mainCharacterExperience=1}
 'unfinished cleanup'={param($f)$f.artifact.observations.cleanup.custodySettled=$false}
 'source changed'={param($f)$f.artifact.observations.cleanup.sourceUnchanged=$false}
}
foreach($pair in $cases.GetEnumerator()){Bad $pair.Key {$f=New-BridgeFixture;&$pair.Value $f;Check-Bridge $f}}
foreach($path in @('beforeLoad.authority.admittedNativeLoads','loadedWorld.authority.authorizedLoads','nativeLoad.entries','nativeLoad.identities.faults','loadedWorld.owners','archive.pairs','nativeProfileInventory.ordinaryAcquisitionPrerequisites.nativeMoney')){
 Bad ('numeric string '+$path){$f=New-BridgeFixture;$o=$f.artifact.observations;$keys=$path.Split('.');for($i=0;$i-lt$keys.Length-1;$i++){$o=$o.($keys[$i])};$o.($keys[-1])=[string]$o.($keys[-1]);Check-Bridge $f}
}
foreach($path in @('source.runId','source.archiveSha256','loadedWorld.gameId','loadedWorld.mainActorId','loadedWorld.mode','archive.campaignId','nativeLoad.contract','nativeLoad.identities.contract','noPreparation.contract','nativeProfileInventory.captureStatus')){
 Bad ('scalar array '+$path){$f=New-BridgeFixture;$o=$f.artifact.observations;$keys=$path.Split('.');for($i=0;$i-lt$keys.Length-1;$i++){$o=$o.($keys[$i])};$o.($keys[-1])=@($o.($keys[-1]));Check-Bridge $f}
}
foreach($key in @('maximumNativeLoads','admittedNativeLoads','authorizedLoads')){Bad ('final wrong quota '+$key){$f=New-BridgeFixture;$f.artifact.observations.cleanup.authority.$key=2;Check-Bridge $f}}
foreach($key in @('admittedNativeWrites','authorizedWrites','unauthorizedLoads','unauthorizedWrites','fatalViolations','suppressedWrites')){Bad ('final crossed boundary '+$key){$f=New-BridgeFixture;$f.artifact.observations.cleanup.authority.$key=1;Check-Bridge $f}}

Good 'fractional native slot setting remains an observation' {$f=New-BridgeFixture;$f.artifact.observations.fixtureReadiness.saveSettings.autosaveSlots=2.5;Check-Bridge $f}
Good 'negative native slot value is observed without invented capacity' {$f=New-BridgeFixture;$f.artifact.observations.fixtureReadiness.saveSettings.autosaveSlots=-0.5;Check-Bridge $f}
Good 'configured single-save and disabled autosaves confer no new authority' {$f=New-BridgeFixture;$f.artifact.observations.fixtureReadiness.saveSettings.onlyOneSave=$true;$f.artifact.observations.fixtureReadiness.saveSettings.autosaveEnabled=$false;Check-Bridge $f}
Good 'static locked-main fact does not qualify selector membership' {$f=New-BridgeFixture;$f.artifact.observations.fixtureReadiness.staticPartyFacts.characters[0].lockedCompanionComponent=$true;Check-Bridge $f}
Good 'native Int32 experience-rate extrema remain observations' {$f=New-BridgeFixture;foreach($rate in @([int]::MinValue,[int]::MaxValue)){$f.artifact.observations.fixtureReadiness.experienceRatePercent=$rate;Check-Bridge $f}}
Good 'exact multiple native class ranks and archetype' {$f=New-BridgeFixture;$o=$f.artifact.observations;$o.nativeProfileInventory.party[0].nativeLevelUpReadiness.characterLevel=2;$o.nativeProfileInventory.ordinaryAcquisitionPrerequisites.mainCharacterLevel=2;$o.fixtureReadiness.mainProgression.characterLevel=2;$o.fixtureReadiness.mainProgression.classes+=,[pscustomobject]@{classGuid='77777777777777777777777777777777';level=1;archetypeGuids=@('88888888888888888888888888888888')};Check-Bridge $f}
$readinessCases=[ordered]@{
 'unobserved readiness'={param($r)$r.captureStatus='IN PROGRESS'}
 'readiness outside native frame'={param($r)$r.frame=22}
 'wrong native assembly'={param($r)$r.gameplayAssemblyMvid='11111111-1111-4111-8111-111111111111'}
 'readiness grants authority'={param($r)$r.observationOnly=$false}
 'XP rate above native Int32'={param($r)$r.experienceRatePercent=2147483648}
 'XP rate below native Int32'={param($r)$r.experienceRatePercent=-2147483649}
 'XP rate numeric text'={param($r)$r.experienceRatePercent='100'}
 'XP rate fraction'={param($r)$r.experienceRatePercent=100.5}
 'active diagnostic slot projection'={param($r)$r.saveSettings.projectionEnabled=$true}
 'inferred native capacity'={param($r)$r.saveSettings.autosaveCapacityInferred=$true}
 'native fallback action admitted'={param($r)$r.saveSettings.sliderFallbackCallbackPresent=$true}
 'settings mutated'={param($r)$r.saveSettings.settingsWritten=$true}
 'slot setting called integer'={param($r)$r.saveSettings.autosaveSlotsNativeType='System.Int32'}
 'slot setting numeric text'={param($r)$r.saveSettings.autosaveSlots='3'}
 'slot setting array'={param($r)$r.saveSettings.autosaveSlots=@(3)}
 'slot setting NaN'={param($r)$r.saveSettings.autosaveSlots=[double]::NaN}
 'slot setting infinity'={param($r)$r.saveSettings.autosaveSlots=[double]::PositiveInfinity}
 'slot setting above native Single'={param($r)$r.saveSettings.autosaveSlots=[double]::MaxValue}
 'single-save flag numeric'={param($r)$r.saveSettings.onlyOneSave=0}
 'autosave flag text'={param($r)$r.saveSettings.autosaveEnabled='true'}
 'main progression foreign'={param($r)$r.mainProgression.actorId='99999999-1111-4111-8111-111111111111'}
 'main XP differs'={param($r)$r.mainProgression.experience=1}
 'class sum differs'={param($r)$r.mainProgression.classes[0].level=2}
 'class duplicate'={param($r)$r.mainProgression.classes+=,$r.mainProgression.classes[0]}
 'class GUID array'={param($r)$r.mainProgression.classes[0].classGuid=@($r.mainProgression.classes[0].classGuid)}
 'class rank numeric text'={param($r)$r.mainProgression.classes[0].level='1'}
 'archetype duplicate'={param($r)$r.mainProgression.classes[0].archetypeGuids=@(('8'*32),('8'*32))}
 'archetype unbounded'={param($r)$r.mainProgression.classes[0].archetypeGuids=@(1..17|ForEach-Object{$_.ToString('x32')})}
 'selector opened'={param($r)$r.staticPartyFacts.selectorOpened=$true}
 'live selector authority invented'={param($r)$r.staticPartyFacts.liveSelectorRequiredAuthorityAvailable=$true}
 'static membership qualified'={param($r)$r.staticPartyFacts.membershipOrRemovalQualified=$true}
 'static actor duplicate'={param($r)$r.staticPartyFacts.characters+=,$r.staticPartyFacts.characters[0]}
 'static actor foreign'={param($r)$r.staticPartyFacts.characters[0].actorId='99999999-1111-4111-8111-111111111111'}
 'static main flag differs'={param($r)$r.staticPartyFacts.characters[0].isMain=$false}
 'static blueprint differs'={param($r)$r.staticPartyFacts.characters[0].blueprintGuid='9'*32}
 'locked flag array'={param($r)$r.staticPartyFacts.characters[0].lockedCompanionComponent=@($false)}
 'foreign descriptor root'={param($r)$r.isolatedSaveDescriptors.root+='-foreign'}
 'descriptor content claimed'={param($r)$r.isolatedSaveDescriptors.metadataOnly=$false}
 'descriptor unbounded'={param($r)$r.isolatedSaveDescriptors.maximumObservedDescriptors=33}
 'descriptor repeated'={param($r)$r.isolatedSaveDescriptors.descriptors+=,$r.isolatedSaveDescriptors.descriptors[0]}
 'descriptor new writable slot'={param($r)$r.isolatedSaveDescriptors.descriptors[0].fileName='Auto_2.zks'}
 'descriptor name differs'={param($r)$r.isolatedSaveDescriptors.descriptors[0].internalName='Other'}
 'descriptor campaign differs'={param($r)$r.isolatedSaveDescriptors.descriptors[0].gameId='99999999-1111-4111-8111-111111111111'}
 'descriptor manual category'={param($r)$r.isolatedSaveDescriptors.descriptors[0].type='Manual'}
 'descriptor native level-up flag differs'={param($r)$r.isolatedSaveDescriptors.descriptors[0].isAutoLevelupSave=$true}
 'descriptor no native file'={param($r)$r.isolatedSaveDescriptors.descriptors[0].hasFileOnDisk=$false}
 'descriptor unfinished operation'={param($r)$r.isolatedSaveDescriptors.descriptors[0].operationState='Saving'}
 'descriptor timestamp text'={param($r)$r.isolatedSaveDescriptors.descriptors[0].systemSaveTimeTicks='1'}
 'descriptor timestamp above native range'={param($r)$r.isolatedSaveDescriptors.descriptors[0].systemSaveTimeTicks=[long]::MaxValue}
 'descriptor timestamp kind array'={param($r)$r.isolatedSaveDescriptors.descriptors[0].systemSaveTimeKind=@('Local')}
 'unexpected route grant'={param($r)$r|Add-Member -NotePropertyName nextSlotWritable -NotePropertyValue $true}
}
foreach($pair in $readinessCases.GetEnumerator()){Bad $pair.Key {$f=New-BridgeFixture;&$pair.Value $f.artifact.observations.fixtureReadiness;Check-Bridge $f}}
foreach($path in @('contract','captureStatus','saveSettings.observationKind','saveSettings.autosaveSlotsNativeType','isolatedSaveDescriptors.descriptors.0.fileName')){
 if($path-match'\.0\.'){continue}
 Bad ('readiness scalar array '+$path){$f=New-BridgeFixture;$o=$f.artifact.observations.fixtureReadiness;$keys=$path.Split('.');for($i=0;$i-lt$keys.Length-1;$i++){$o=$o.($keys[$i])};$o.($keys[-1])=@($o.($keys[-1]));Check-Bridge $f}
}


# Use the installed writer's actual Single spellings, including rounded decimal endpoints.
[void][Reflection.Assembly]::LoadFrom('C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed/Newtonsoft.Json.dll')
foreach($item in @(@('zero',[single]0),@('fraction',[single]0.1),@('negative',[single]-0.5),@('epsilon',[single]::Epsilon),@('maximum',[single]::MaxValue),@('minimum',[single]::MinValue))){
 Good ('actual serialized native Single '+$item[0]){$f=New-BridgeFixture;$json=[Newtonsoft.Json.JsonConvert]::SerializeObject($item[1]);$f.artifact.observations.fixtureReadiness.saveSettings.autosaveSlots=ConvertFrom-Json -InputObject $json;Check-Bridge $f}
}
foreach($item in @(@('underflow',[double]1e-100),@('negative-underflow',[double]-1e-100),@('overflow',[double]::MaxValue))){
 Bad ('actual serialized non-Single '+$item[0]){$f=New-BridgeFixture;$json=[Newtonsoft.Json.JsonConvert]::SerializeObject($item[1]);$f.artifact.observations.fixtureReadiness.saveSettings.autosaveSlots=ConvertFrom-Json -InputObject $json;Check-Bridge $f}
}
Bad 'contradictory native saved flags' {$f=New-BridgeFixture;$f.artifact.observations.fixtureReadiness.isolatedSaveDescriptors.descriptors[0].isActuallySaved=$false;Check-Bridge $f}

@{passes=$script:passes;failures=$script:failures;nativeExecution=$false;archiveReads=0;readerSha256=(Get-FileHash (Join-Path $bridgeReader 'Chunk7BootstrapInventoryEvidence.ps1')).Hash.ToLowerInvariant()}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $bridgeOutput 'receipt.json') -Encoding UTF8
Write-Host ('TOTAL PASS='+$script:passes+' FAIL='+$script:failures);if($script:failures){throw 'Cold inventory reader regressions failed'}
