[CmdletBinding()]
param([string]$RepositoryRoot,[string]$ReaderDirectory,[string]$FixturePath,[string]$OutputDirectory,[switch]$FixturesOnly)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))};if(-not$ReaderDirectory){$ReaderDirectory=Join-Path $RepositoryRoot 'scripts/runtime'}
if(-not$FixturePath){$FixturePath=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/SmallManualFixture.json'}
if(-not$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/small-manual-evidence-'+[Guid]::NewGuid().ToString('N'))}
. (Join-Path $ReaderDirectory 'RuntimeHarness.Common.ps1')
$script:smallManualFixturePath=$FixturePath
function New-SmallManualFixture{[IO.File]::ReadAllText($script:smallManualFixturePath)|ConvertFrom-Json}
function Check-SmallManual($F){Assert-KmcChunk7SmallEvidence $F.request $F.artifact}
if($FixturesOnly){return}
[void][IO.Directory]::CreateDirectory($OutputDirectory);$script:pass=0;$script:fail=0;$watch=[Diagnostics.Stopwatch]::StartNew()
function Good([string]$Name,[scriptblock]$Action){try{&$Action;$script:pass++;Write-Host ('PASS '+$Name)}catch{$script:fail++;Write-Host ('FAIL '+$Name+': '+$_+' '+$_.ScriptStackTrace)}}
function Bad([string]$Name,[scriptblock]$Action){$caught=$false;try{&$Action}catch{$caught=$true};if($caught){$script:pass++;Write-Host ('PASS refuses '+$Name)}else{$script:fail++;Write-Host ('FAIL accepted '+$Name)}}
Good 'new field-durable paid Small source/real reader no promotion' {Check-SmallManual (New-SmallManualFixture)}
$cases=[ordered]@{
 'native inventory status singleton'={param($f)$f.artifact.observations.profileInventory.captureStatus=@($f.artifact.observations.profileInventory.captureStatus)}
 'native inventory module singleton'={param($f)$f.artifact.observations.profileInventory.gameplayAssemblyMvid=@($f.artifact.observations.profileInventory.gameplayAssemblyMvid)}
 'old source alias'={param($f)$f.request.smallCalibration|Add-Member source ([pscustomobject]@{})}
 'cumulative source'={param($f)$f.artifact.observations.source.acquiredManual.selection.case='preserve-two-acquired-field'}
 'two owner observation'={param($f)$f.artifact.observations.source.acquiredManual|Add-Member twoOwners @()}
 'existing acquisition'={param($f)$f.artifact.observations.source.acquiredManual.sourceAcquisition.selection.case='existing-ranger-horse'}
 'Medium acquisition alias'={param($f)$f.artifact.observations.source.acquiredManual.sourceAcquisition.selection.case='paid-medium-ranger-horse'}
 'Working campaign'={param($f)$f.request.persistenceLoad.gameId=$f.request.fixture.working.gameId}
 'Working bytes'={param($f)$f.request.persistenceLoad.sha256=$f.request.fixture.working.sha256}
 'Auto input'={param($f)$f.request.persistenceLoad.fileName='Auto_1.zks'}
 'overflow Manual number'={param($f)$f.request.persistenceLoad.fileName='Manual_2147483648_KMC_C7_ACQUIRED_FIELD.zks'}
 'zero Manual number'={param($f)$f.request.persistenceLoad.fileName='Manual_0_KMC_C7_ACQUIRED_FIELD.zks'}
 'missing main actor'={param($f)$f.artifact.observations.manualNativeRoster.mainActorId=$f.request.smallCalibration.manualSource.actorId}
 'source same run'={param($f)$f.request.smallCalibration.manualSource.sourceRunId=$f.request.runId}
 'source dot segment'={param($f)$f.request.smallCalibration.manualSource.sourceRunId='..'}
 'source output hash'={param($f)$f.artifact.observations.source.acquiredManual.nativeWrite.sha256='a'*64}
 'source output type'={param($f)$f.artifact.observations.source.acquiredManual.nativeWrite.type='Auto'}
 'source not base only'={param($f)$f.artifact.observations.source.acquiredManual.selection.baseGameOnly=$false}
 'source remote owner'={param($f)$f.artifact.observations.source.acquiredManual.savedOwner.remoteCompanions=@($f.request.smallCalibration.manualSource.actorId)}
 'source reciprocal pet'={param($f)$f.artifact.observations.source.acquiredManual.savedOwner.reciprocal=$false}
 'source graph master'={param($f)$f.artifact.observations.source.acquiredManual.nativeProfileInventory.party[2].masterId=$null}
 'source extra owner'={param($f)$f.artifact.observations.source.acquiredManual.nativeProfileInventory.party[0].petId=$f.request.smallCalibration.manualSource.petId}
 'source mounted archive'={param($f)$f.artifact.observations.source.acquiredManual.archive.pairs=1}
 'source preparation'={param($f)$f.artifact.observations.source.acquiredManual.savedWorld.semanticRestores=1}
 'source unsettled cleanup'={param($f)$f.artifact.observations.source.acquiredManual.cleanup.custodySettled=$false}
 'source XP change'={param($f)$f.artifact.observations.source.acquiredManual.afterSaveArrangement.actors[0].experience++}
 'cold roster removed'={param($f)$f.artifact.observations.manualNativeRoster.actors=@($f.artifact.observations.manualNativeRoster.actors|Select-Object -Skip 1)}
 'cold roster repeated'={param($f)$f.artifact.observations.manualNativeRoster.actors[0]=$f.artifact.observations.manualNativeRoster.actors[1]}
 'cold descriptor alias'={param($f)$f.artifact.observations.manualNativeRoster.actors[0].descriptor=$f.artifact.observations.manualNativeRoster.actors[0].object}
 'cold view replaced'={param($f)$f.artifact.observations.manualNativeRoster.actors[1].object=999}
 'cold XP invented'={param($f)$f.artifact.observations.manualNativeRoster.arrangement.actors[0].experience++}
 'cold money invented'={param($f)$f.artifact.observations.manualNativeRoster.arrangement.money++}
 'cold allowance invented'={param($f)$f.artifact.observations.manualNativeRoster.resources.actors[0].reactionAllowance++}
 'cold source body changed'={param($f)$f.artifact.observations.bodyObservation.RaceGuid='9'*32}
 'cold snapshot XP mismatch'={param($f)$f.artifact.observations.before.xp++;$f.artifact.observations.field.xp++}
 'cold snapshot pet level mismatch'={param($f)$f.artifact.observations.before.petLevel++;$f.artifact.observations.field.petLevel++}
 'cold field phase mutation'={param($f)$f.artifact.observations.field.money++}
 'selector silently opened'={param($f)$f.artifact.observations.selector.openCount=1}
 'cold observation installed late'={param($f)$f.artifact.observations.beforeLoad.nativeLoads=1}
 'native Prepare entered'={param($f)$f.artifact.observations.coldNoPreparation.events=@([pscustomobject]@{kind='prepare-before'})}
 'native observation incomplete'={param($f)$f.artifact.observations.coldNoPreparation.complete=$false}
 'native observer wrong token'={param($f)$f.artifact.observations.coldNoPreparation.hooks[0].token='06000000'}
 'native observer wrong product'={param($f)$f.artifact.observations.coldNoPreparation.hooks[3].moduleMvid='aaaaaaaa-1111-4111-8111-111111111111'}
 'extra load admitted'={param($f)$f.artifact.observations.authority.admittedNativeLoads=2}
 'save write admitted'={param($f)$f.artifact.observations.authority.admittedNativeWrites=1}
 'retained observer cleanup'={param($f)$f.artifact.observations.cleanup.observerReleased=$false}
 'retained native worker'={param($f)$f.artifact.observations.cleanup.nativeWorkerSettled=$false}
 'manual fact removal'={param($f)$f.artifact.observations.cleanup.manualFactRemovals=1}
 'invented human acceptance'={param($f)$f.artifact.observations.humanVisualAcceptance=$true}
 'profile declaration bypass'={param($f)$f.artifact.observations.declaration.profileId='medium-humanoid-horse-v1'}
 'unknown action actor'={param($f)$f.artifact.observations.inputs[1].after.actors[0].id='aaaaaaaa-1111-4111-8111-111111111111'}
 'native preparation manufactured'={param($f)$f.artifact.observations.nativePreparationManufactured=$true}
}
foreach($c in $cases.GetEnumerator()){Bad $c.Key {$f=New-SmallManualFixture;&$c.Value $f;Check-SmallManual $f}}
foreach($key in @('sourceRunId','sourceArtifactSha256','sourceArchiveSha256','actorId','petId')){Bad ('source token array '+$key){$f=New-SmallManualFixture;$f.request.smallCalibration.manualSource.$key=@($f.request.smallCalibration.manualSource.$key);Check-SmallManual $f}}
foreach($key in @('contract','prepareMetric')){Bad ('cold token array '+$key){$f=New-SmallManualFixture;$f.artifact.observations.coldNoPreparation.$key=@($f.artifact.observations.coldNoPreparation.$key);Check-SmallManual $f}}
foreach($key in @('declaringType','method','token','moduleMvid','prefix','postfix')){Bad ('native hook token array '+$key){$f=New-SmallManualFixture;$f.artifact.observations.coldNoPreparation.hooks[0].$key=@($f.artifact.observations.coldNoPreparation.hooks[0].$key);Check-SmallManual $f}}
foreach($key in @('object','descriptor','view')){Bad ('native object numeric text '+$key){$f=New-SmallManualFixture;$f.artifact.observations.manualNativeRoster.actors[0].$key=[string]$f.artifact.observations.manualNativeRoster.actors[0].$key;Check-SmallManual $f}}
$watch.Stop();[ordered]@{pass=$script:pass;fail=$script:fail;seconds=$watch.Elapsed.TotalSeconds;readerSha256=(Get-FileHash -LiteralPath (Join-Path $ReaderDirectory 'Chunk7SmallCalibrationEvidence.ps1')).Hash.ToLowerInvariant();fixtureSha256=(Get-FileHash -LiteralPath $FixturePath).Hash.ToLowerInvariant();nativeProof=$false;archiveReads=0;testBoundary='Real strict reader; JSON-only explicitly synthetic lineage and native observations'}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
Write-Host ('TOTAL PASS='+$script:pass+' FAIL='+$script:fail);if($script:fail){throw 'Small Manual evidence checks failed.'}
