[CmdletBinding()]
param([string]$RepositoryRoot,[string]$ReaderDirectory,[string]$FixtureDirectory,[string]$OutputDirectory,[switch]$FixturesOnly)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))};if(-not$ReaderDirectory){$ReaderDirectory=Join-Path $RepositoryRoot 'scripts/runtime'};if(-not$FixtureDirectory){$FixtureDirectory=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics'}
if(-not$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/acquired-manual-evidence-'+[Guid]::NewGuid().ToString('N'))}
$acquiredReader=[IO.Path]::GetFullPath($ReaderDirectory);$acquiredFixtures=[IO.Path]::GetFullPath($FixtureDirectory);$acquiredOutput=[IO.Path]::GetFullPath($OutputDirectory)
. (Join-Path $acquiredReader 'RuntimeHarness.Common.ps1')
. (Join-Path $acquiredReader 'Chunk7AcquiredManualEvidence.ps1')
function New-AcquiredManualFixture([bool]$Paid=$true){
 $name=if($Paid){'Paid'}else{'Existing'}
 $fixture=[IO.File]::ReadAllText((Join-Path $acquiredFixtures ('AcquiredManual'+$name+'Fixture.json')))|ConvertFrom-Json
 $fixture.artifact.observations.noPreparation.hooks[1].method='OnNewRound'
 $fixture.artifact.observations.noPreparation.hooks[1].declaringType='Kingmaker.Controllers.Combat.UnitCombatState'
 return $fixture
}
function Check-AcquiredManual($F){Assert-KmcChunk7AcquiredManualEvidence $F.request $F.artifact $F.source}
if($FixturesOnly){return};[void][IO.Directory]::CreateDirectory($acquiredOutput);$script:passes=0;$script:failures=0;$watch=[Diagnostics.Stopwatch]::StartNew()
function Good([string]$Name,[scriptblock]$Body){try{&$Body;$script:passes++;Write-Host ('PASS '+$Name)}catch{$script:failures++;Write-Host ('FAIL '+$Name+': '+$_+' at '+$_.ScriptStackTrace)}}
function Bad([string]$Name,[scriptblock]$Body){$rejected=$false;try{&$Body}catch{$rejected=$true};if($rejected){$script:passes++;Write-Host ('PASS refuses '+$Name)}else{$script:failures++;Write-Host ('FAIL accepted '+$Name)}}
Good 'owner Manual304 PostLoad may report pending GameId before settled-world identity' {$f=New-AcquiredManualFixture $false;$f.request.persistenceLoad.fileName='Manual_304_KMC_C7_MANUAL_DERIVED_FIELD.zks';$f.artifact.observations.nativeLoad.events[0].gameId=$null;Assert-KmcAcquiredManualPostLoadGameId $f.artifact.observations.nativeLoad.events[0] $f.request}
Bad 'owner Manual304 PostLoad cannot report a foreign populated GameId' {$f=New-AcquiredManualFixture $false;$f.request.persistenceLoad.fileName='Manual_304_KMC_C7_MANUAL_DERIVED_FIELD.zks';$f.artifact.observations.nativeLoad.events[1].gameId='11111111-1111-4111-8111-111111111111';Assert-KmcAcquiredManualPostLoadGameId $f.artifact.observations.nativeLoad.events[1] $f.request}
Bad 'historical Manual2 PostLoad cannot lose GameId' {$f=New-AcquiredManualFixture $false;$f.artifact.observations.nativeLoad.events[0].gameId=$null;Assert-KmcAcquiredManualPostLoadGameId $f.artifact.observations.nativeLoad.events[0] $f.request}
$cases=[ordered]@{
'foreign source candidate'={param($f)$f.source.dllSha256='e'*64}
'failed source acquisition'={param($f)$f.source.status='FAIL'}
'same native process'={param($f)$f.artifact.observations.processId=$f.source.observations.processId}
'foreign Auto input'={param($f)$f.request.persistenceLoad.fileName='Auto_2.zks'}
'Working campaign alias'={param($f)$f.request.persistenceLoad.gameId=$f.request.fixture.working.gameId}
'missing acquisition lineage'={param($f)$f.artifact.observations.sourceAcquisition=$null}
 'source field omits acquired Horse'={param($f)$s=$f.source.observations.savedFieldWorld;$pet=$f.request.acquiredManual.petId;$s.party=@($s.party|Where-Object id -CNE $pet)}
 'cold field changes acquired Horse blueprint'={param($f)$o=$f.artifact.observations;$pet=@($o.loadedWorld.party|Where-Object id -CEQ $f.request.acquiredManual.petId)[0];$pet.blueprint='a'*32}
 'cold field changes acquired owner identity'={param($f)$o=$f.artifact.observations;$owner=@($o.loadedWorld.party|Where-Object id -CEQ $f.request.acquiredManual.actorId)[0];$owner.descriptor++}
'source copied graph changed'={param($f)$f.artifact.observations.sourceNativeWorld.party[0].id='aaaaaaaa-1111-4111-8111-111111111111'}
'actor and pet alias'={param($f)$f.request.acquiredManual.petId=$f.request.acquiredManual.actorId}
'excess load quota'={param($f)$f.request.acquiredManual.expectedNativeLoadCount=2}
'early write observed'={param($f)$f.artifact.observations.beforeLoad.authority.admittedNativeWrites=1}
'initial writable target'={param($f)$f.artifact.observations.reservation.initialWritableEntries=1}
'missing reserved output'={param($f)$f.artifact.observations.reservation.reservedWritableEntries=0}
'duplicate reservation'={param($f)$f.artifact.observations.reservation.count=2}
'foreign reserved source descriptor'={param($f)$f.artifact.observations.reservation.sourceDescriptor=29}
'reservation after native load'={param($f)$f.artifact.observations.reservation.beforeLoad=$false}
'reservation after load frame'={param($f)$f.artifact.observations.reservation.frame=21}
'missing native numbering pin'={param($f)$f.artifact.observations.reservation.nativeMethodToken='06000000'}
'changed native number before save'={param($f)$f.artifact.observations.nativeNumbering.number=8}
'unreserved output leaf'={param($f)$f.artifact.observations.nativeWrite.fileName='Manual_8_KMC_C7_ACQUIRED_FIELD.zks'}
'foreign live world'={param($f)$f.artifact.observations.fieldWorld.player=99}
'foreign native callback'={param($f)$f.artifact.observations.nativeLoad.events[1].player=99}
'field original actor dropped'={param($f)$f.artifact.observations.fieldWorld.party=@($f.artifact.observations.fieldWorld.party|Select-Object -Skip 1)}
'field original view replaced'={param($f)$f.artifact.observations.fieldWorld.party[0].view=99}
'cold actor binding replaced'={param($f)$f.artifact.observations.loadedOwner.actorObject=99}
'acquired descriptor replaced'={param($f)$f.artifact.observations.fieldOwner.actorDescriptor=99}
'pet sourced fact changed'={param($f)$f.artifact.observations.savedOwner.sourceGuid='a'*32}
'pet rank changed'={param($f)$f.artifact.observations.savedOwner.rank++}
'pet reciprocal binding lost'={param($f)$f.artifact.observations.fieldOwner.reciprocal=$false}
'owner resource changed'={param($f)$f.artifact.observations.resourcesAfterField.actors[0].standard=6}
'owner prepared changed'={param($f)$f.artifact.observations.resourcesAfterSave.actors[0].prepared=$true}
'owner XP changed'={param($f)$f.artifact.observations.afterSaveArrangement.actors[0].experience++}
'owner class changed'={param($f)$f.artifact.observations.loadedOwner.progression.actors[0].classes[0].level++}
'money granted'={param($f)$f.artifact.observations.beforeSaveArrangement.money++}
'projection active'={param($f)$f.artifact.observations.fixtureReadiness.saveSettings.projectionEnabled=$true}
'one-save native mode'={param($f)$f.artifact.observations.fixtureReadiness.saveSettings.onlyOneSave=$true}
'native preparation after save'={param($f)$f.artifact.observations.noPreparation.events=@([pscustomobject]@{kind='prepare-before'})}
'foreign restore module'={param($f)$f.artifact.observations.noPreparation.hooks[2].moduleMvid='44444444-4444-4444-8444-444444444444'}
'registry released before capture'={param($f)$f.artifact.observations.nativeLoad.identities.released=$true}
'zero native object label'={param($f)$f.artifact.observations.loadedWorld.game=0}
'world labels alias'={param($f)$f.artifact.observations.loadedWorld.state=$f.artifact.observations.loadedWorld.game}
'foreign source hash at output'={param($f)$f.artifact.observations.nativeWrite.sha256=$f.request.persistenceLoad.sha256}
'prepared descriptor equals request'={param($f)$f.artifact.observations.nativeWrite.writtenDescriptor=$f.artifact.observations.saveRequested.descriptor}
'foreign native save manager'={param($f)$f.artifact.observations.nativeSave.events[1].saveManager=99}
'unsaved descriptor already prepared'={param($f)$f.artifact.observations.nativeSave.events[0].fileName='Manual_7_KMC_C7_ACQUIRED_FIELD.zks'}
'worker incomplete'={param($f)$f.artifact.observations.nativeSave.events[4].workerCompleted=$false}
'worker identity alias'={param($f)$f.artifact.observations.nativeSave.events[4].worker=$f.artifact.observations.loadedWorld.game}
'duplicate callback'={param($f)$f.artifact.observations.nativeSave.callbacks=2}
'foreign final inventory'={param($f)$f.artifact.observations.settledDescriptors[0].fileName='Auto_2.zks'}
'final selector custody retained'={param($f)$f.artifact.observations.cleanup.selectorClosed=$false}
'final observer custody retained'={param($f)$f.artifact.observations.cleanup.observersReleased=$false}
'static observation claims selector authority'={param($f)$f.artifact.observations.fixtureReadiness.staticPartyFacts.liveSelectorRequiredAuthorityAvailable=$true}
}
foreach($paid in @($false,$true)){
 $flow=if($paid){'paid'}else{'existing'};Good ($flow+' exact acquired cold/field/Manual lineage'){$f=New-AcquiredManualFixture $paid;Check-AcquiredManual $f}
 Good ($flow+' unchanged observed prepared state does not manufacture entitlement'){$f=New-AcquiredManualFixture $paid;foreach($r in @($f.artifact.observations.resourcesBeforeField,$f.artifact.observations.resourcesAfterField,$f.artifact.observations.resourcesAfterSave)){$r.actors[0].prepared=$true};Check-AcquiredManual $f}
 foreach($name in $cases.Keys){$mutate=$cases[$name];Bad ($flow+' '+$name){$f=New-AcquiredManualFixture $paid;&$mutate $f;Check-AcquiredManual $f}}
 foreach($key in @('number','count','initialEntries','reservedEntries','initialWritableEntries','reservedWritableEntries','sourceDescriptor')){Bad ($flow+' numeric text '+$key){$f=New-AcquiredManualFixture $paid;$f.artifact.observations.reservation.$key=[string]$f.artifact.observations.reservation.$key;Check-AcquiredManual $f}}
 foreach($key in @('actorId','petId','featureGuid','sourceGuid')){Bad ($flow+' scalar-array owner '+$key){$f=New-AcquiredManualFixture $paid;$f.artifact.observations.loadedOwner.$key=@($f.artifact.observations.loadedOwner.$key);Check-AcquiredManual $f}}
 foreach($key in @('nativeMethod','nativeMethodToken','nativeMethodMvid','outputLeaf')){Bad ($flow+' scalar-array reservation '+$key){$f=New-AcquiredManualFixture $paid;$f.artifact.observations.reservation.$key=@($f.artifact.observations.reservation.$key);Check-AcquiredManual $f}}
}
Bad 'one-owner field Manual fabricates native selector' {$f=New-AcquiredManualFixture;$f.artifact.observations.selector.openCount=1;Check-AcquiredManual $f}
foreach($key in @('OriginalSizeOrdinal','EffectiveSizeOrdinal')){Bad ('paid body wrong '+$key){$f=New-AcquiredManualFixture;$f.artifact.observations.bodyObservation.$key=4;Check-AcquiredManual $f}}
Bad 'paid acquired actor claims remote after field Manual' {$f=New-AcquiredManualFixture;$f.artifact.observations.loadedOwner.actorInGame=$false;Check-AcquiredManual $f}
Bad 'existing route silently opens selector' {$f=New-AcquiredManualFixture $false;$f.artifact.observations.selector.openCount=1;Check-AcquiredManual $f}
$watch.Stop();[ordered]@{status=$(if($script:failures-eq0){'PASS'}else{'FAIL'});pass=$script:passes;fail=$script:failures;seconds=$watch.Elapsed.TotalSeconds;readerSha256=(Get-FileHash -LiteralPath (Join-Path $acquiredReader 'Chunk7AcquiredManualEvidence.ps1')).Hash.ToLowerInvariant();fixtures=@('Existing','Paid')|ForEach-Object{@{name=$_;sha256=(Get-FileHash -LiteralPath (Join-Path $acquiredFixtures ('AcquiredManual'+$_+'Fixture.json'))).Hash.ToLowerInvariant()}};nativeProof=$false;archiveReads=0}|ConvertTo-Json -Depth 4|Set-Content -LiteralPath (Join-Path $acquiredOutput 'receipt.json') -Encoding UTF8
Write-Host ('TOTAL PASS='+$script:passes+' FAIL='+$script:failures);if($script:failures-gt0){throw 'Acquired Manual evidence checks failed.'}
