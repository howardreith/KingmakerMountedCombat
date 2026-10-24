[CmdletBinding()]
param([string]$RepositoryRoot,[string]$ReaderDirectory,[string]$FixturePath,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('ReaderDirectory')){$ReaderDirectory=Join-Path $RepositoryRoot 'scripts/runtime'}
if(-not$PSBoundParameters.ContainsKey('FixturePath')){$FixturePath=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/BootstrapInventoryEvidenceFixture.json'}
if(-not$PSBoundParameters.ContainsKey('OutputDirectory')){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/bootstrap-inventory-source-'+[Guid]::NewGuid().ToString('N'))}
$bridgeReader=[IO.Path]::GetFullPath($ReaderDirectory);$bridgeFixture=[IO.Path]::GetFullPath($FixturePath);$bridgeOutput=[IO.Path]::GetFullPath($OutputDirectory)
. (Join-Path $bridgeReader 'RuntimeHarness.Common.ps1');. (Join-Path $bridgeReader 'Chunk7BootstrapInventoryFixtures.ps1')
[void][IO.Directory]::CreateDirectory($bridgeOutput);$script:passes=0;$script:failures=0
function Good([string]$Name,[scriptblock]$Action){try{&$Action;$script:passes++;Write-Host ('PASS '+$Name)}catch{$script:failures++;Write-Host ('FAIL '+$Name+': '+$_)}}
function Bad([string]$Name,[scriptblock]$Action){$rejected=$false;try{&$Action}catch{$rejected=$true};if($rejected){$script:passes++;Write-Host ('PASS rejects '+$Name)}else{$script:failures++;Write-Host ('FAIL accepted '+$Name)}}
# Only external filesystem/manifest/ZIP boundaries are substituted. The actual
# source guard runs with real strict bootstrap artifact and metadata readers.
# No directory, save or archive at these virtual paths is opened or created.
function Get-KmcLabRoot {'C:\BridgeVirtual'}
function Assert-KmcChildPath($Path,$Parent,$Description){$Path}
function Assert-KmcDirectoryTreeCloneable($Path,$Description){if($script:boundaryFailure-ceq'tree'){throw 'reparse tree'}}
function Assert-KmcNotReparsePoint($Path,$Description){if($script:boundaryFailure-ceq'reparse'){throw 'reparse'}}
function Assert-KmcNotHardLink($Path,$Description){if($script:boundaryFailure-ceq'hardlink'){throw 'hardlink'}}
function Assert-KmcManifestJsonMembersUnique($Path,$Description){if($script:boundaryFailure-ceq'duplicate'){throw 'duplicate JSON member'}}
function Get-KmcSha256($Path){$leaf=[IO.Path]::GetFileName($Path);if($leaf-ceq'chunk7-native-bootstrap-evidence.json'){return $script:virtualArtifactHash};if($leaf-ceq'Auto_1.zks'){return $script:f.request.bootstrapInventory.sourceArchiveSha256};return ('f'*64)}
function Get-Item($LiteralPath){[pscustomobject]@{Length=100;LastWriteTimeUtc=[datetime]::new(1);PSIsContainer=$false;Attributes=[IO.FileAttributes]::Normal}}
function Read-KmcJson($Path){switch([IO.Path]::GetFileName($Path)){'runtime-request.json'{$script:f.sourceRequest;break}'runtime-result.json'{$script:result;break}'runtime-game-result.json'{$script:game;break}'runtime-artifacts.json'{$script:manifest;break}'base-game-isolation-result.json'{$script:isolation;break}'owner.json'{$script:owner;break}default{throw 'Unexpected virtual read'}}}
function Assert-KmcReadOnlyArtifactManifest($Request,$Sha,[switch]$GameResult){if($Sha-cne('f'*64)){throw 'manifest hash'}}
function Read-KmcChunk7ManifestArtifact($Request,$Manifest,$Leaf,$Kind){if($Leaf-cne'chunk7-native-bootstrap-evidence.json'-or$Kind-cne'chunk7-native-bootstrap-evidence'){throw 'unexpected native source artifact'};$script:f.source}
function Assert-KmcChunk7BootstrapInventoryArchive($Request,$Artifact){$script:archiveBoundaries++;Assert-KmcChunk7NativeBootstrapOutput $Request $Artifact}
function Reset-Source {
 $script:f=[IO.File]::ReadAllText($bridgeFixture).Replace('C:\\Dev\\KingmakerMountedCombatLab','C:\\BridgeVirtual')|ConvertFrom-Json
 $script:archiveBoundaries=0;$script:boundaryFailure='';$script:virtualArtifactHash=$script:f.request.bootstrapInventory.sourceArtifactSha256;$r=$script:f.sourceRequest
 $receipt=[ordered]@{};foreach($key in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid','transactionToken')){$receipt[$key]=$r.$key}
 $receipt.status='PASS';$receipt.errors=@();$receipt.evidenceManifestSha256='f'*64
 $script:result=([pscustomobject]$receipt|ConvertTo-Json|ConvertFrom-Json);$script:game=([pscustomobject]$receipt|ConvertTo-Json|ConvertFrom-Json)
 foreach($key in @('modsRestored','workingRestored','baselineImmutable','saveProtectionPassed','saveWriteAllowlistPassed')){$script:result|Add-Member -NotePropertyName $key -NotePropertyValue $true}
 $script:result|Add-Member -NotePropertyName restoredSaveInventoryDigest -NotePropertyValue ('e'*64);$script:result|Add-Member -NotePropertyName gameResultSha256 -NotePropertyValue ('f'*64)
 foreach($key in @('assertionFailCount','subscenarioFailCount','loadRequestCount','workingLoadRequestCount','baselineLoadRequestCount','unauthorizedLoadRequestCount','unauthorizedSaveRequestCount')){$script:game|Add-Member -NotePropertyName $key -NotePropertyValue 0}
 foreach($key in @('saveRequestCount','workingSaveRequestCount')){$script:game|Add-Member -NotePropertyName $key -NotePropertyValue 1};$script:game|Add-Member -NotePropertyName fixtureIdentityVerified -NotePropertyValue $false
 $profile=Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$r.runId)
 $script:owner=[pscustomobject]@{runId=$r.runId;scenario=$r.scenario;transactionToken=$r.transactionToken;sourceSha256=$r.fixture.working.sha256;saveRoot=(Join-Path $profile 'Saved Games');persistenceCase=$null}
 $script:isolation=[pscustomobject]@{runId=$r.runId;stagingMode='base-game-only-kmc';stageVerified=$true;dependenciesUnchanged=$true;processExited=$true;modsRestored=$true;saveProtectionPassed=$true;gameReportedPass=$true;nativeInventoryObserved=$true;nativeCompatibilityQualified=$true}
 $script:manifest=[pscustomobject]@{artifacts=@([pscustomobject]@{relativePath='chunk7-native-bootstrap-evidence.json';kind='chunk7-native-bootstrap-evidence'})}
}
function Get-Source {Get-KmcChunk7BootstrapInventorySource $script:f.request.bootstrapInventory $script:f.request.fixture $script:f.request}
Good 'actual provenance gate reaches bounded archive once only' {Reset-Source;$out=Get-Source;if($script:archiveBoundaries-ne1-or$out.descriptor.gameId-cne$script:f.source.observations.nativeWorld.gameId-or$out.descriptor.fileName-cne'Auto_1.zks'){throw 'Qualified native source lost exact output identity'}}
$cases=[ordered]@{
 'foreign candidate DLL'={$script:f.sourceRequest.dllSha256='e'*64}
 'foreign candidate commit'={$script:f.sourceRequest.commit='e'*40}
 'foreign candidate MVID'={$script:f.sourceRequest.dllMvid='aaaaaaaa-1111-4111-8111-111111111111'}
 'foreign source family'={$script:f.sourceRequest.scenario='chunk7-two-pair-exploration'}
 'source FAIL'={$script:result.status='FAIL'}
 'game FAIL'={$script:game.status='FAIL'}
 'incomplete restoration'={$script:result.modsRestored=$false}
 'missing process exit'={$script:isolation.processExited=$false}
 'non-base native run'={$script:isolation.stagingMode='installed-mods'}
 'foreign profile owner'={$script:owner.runId='foreign'}
 'foreign owner token'={$script:owner.transactionToken='e'*64}
 'writable protected anchor changed'={$script:owner.sourceSha256='e'*64}
 'foreign isolated root'={$script:owner.saveRoot='C:\Foreign'}
 'owner persistence case'={$script:owner.persistenceCase='multi-exploration-manual'}
 'loaded Working bootstrap'={$script:game.fixtureIdentityVerified=$true}
 'bootstrap loaded archive'={$script:game.loadRequestCount=1}
 'bootstrap extra native write'={$script:game.saveRequestCount=2}
 'bootstrap refused write'={$script:game.unauthorizedSaveRequestCount=1}
 'source error list'={$script:result.errors=@('failed')}
 'source artifact SHA mismatch'={$script:f.request.bootstrapInventory.sourceArtifactSha256='e'*64}
 'source exact output leaf'={$script:f.source.observations.nativeWrite.fileName='Auto_2.zks'}
 'source exact output root'={$script:f.source.observations.nativeWrite.path='C:\Foreign\Auto_1.zks'}
 'source hash differs'={$script:f.source.observations.nativeWrite.sha256='e'*64}
 'source decode claims cold proof'={$script:f.source.observations.nativeWrite.coldLoadQualified=$true}
 'source Horse refused'={$script:f.source.observations.preflight.horseState='Faulted'}
 'source unsupported Ranger list'={$script:f.source.observations.preflight.rangerAllFeaturesCount=9}
 'candidate hash array'={$script:f.request.dllSha256=@($script:f.request.dllSha256)}
 'owner token array'={$script:owner.transactionToken=@($script:owner.transactionToken)}
 'result status array'={$script:result.status=@('PASS')}
 'quota text'={$script:game.loadRequestCount='0'}
}
foreach($pair in $cases.GetEnumerator()){Good ('pre-archive refuses '+$pair.Key){Reset-Source;&$pair.Value;$refused=$false;try{[void](Get-Source)}catch{$refused=$true};if(-not$refused-or$script:archiveBoundaries-ne0){throw 'Bad provenance reached an archive boundary'}}}
foreach($boundary in @('tree','reparse','hardlink','duplicate')){Good ('unsafe external boundary '+$boundary){Reset-Source;$script:boundaryFailure=$boundary;$refused=$false;try{[void](Get-Source)}catch{$refused=$true};if(-not$refused-or$script:archiveBoundaries-ne0){throw 'Unsafe source reached archive'}}}
function MetadataFixture {$d=([IO.File]::ReadAllText($bridgeFixture)|ConvertFrom-Json).request.persistenceLoad;[pscustomobject]@{descriptor=$d;header=[pscustomobject]@{Name=$d.internalName;Type='Auto';GameId=$d.gameId;GameName=$d.gameName;Area=$d.area;CompatibilityVersion=1;IsAutoLevelupSave=$false};archive=[pscustomobject]@{SchemaVersion=3;CampaignId=$d.gameId;AreaId=$d.area;Pairs=@();Participations=@();Slots=@();Combat=$null}}}
Good 'bounded metadata admits only empty schema3 identity' {$m=MetadataFixture;Assert-KmcChunk7BootstrapEmptyMetadata $m.descriptor $m.header $m.archive}
foreach($key in @('Pairs','Participations','Slots')){Bad ('owned metadata '+$key){$m=MetadataFixture;$m.archive.$key=@([pscustomobject]@{Id='unexpected'});Assert-KmcChunk7BootstrapEmptyMetadata $m.descriptor $m.header $m.archive}}
foreach($key in @('Pairs','Participations','Slots')){Bad ('null metadata '+$key){$m=MetadataFixture;$m.archive.$key=$null;Assert-KmcChunk7BootstrapEmptyMetadata $m.descriptor $m.header $m.archive}}
foreach($key in @('Name','Type','GameId','GameName','Area')){Bad ('header scalar array '+$key){$m=MetadataFixture;$m.header.$key=@($m.header.$key);Assert-KmcChunk7BootstrapEmptyMetadata $m.descriptor $m.header $m.archive}}
foreach($case in @('schema1','numeric schema','combat','auto levelup','wrong campaign','future')){Bad ('metadata '+$case){$m=MetadataFixture;switch($case){'schema1'{$m.archive.SchemaVersion=1}'numeric schema'{$m.archive.SchemaVersion='3'}'combat'{$m.archive.Combat=[pscustomobject]@{Actors=@()}}'auto levelup'{$m.header.IsAutoLevelupSave=$true}'wrong campaign'{$m.archive.CampaignId='foreign'}'future'{$m.archive.SchemaVersion=4}};Assert-KmcChunk7BootstrapEmptyMetadata $m.descriptor $m.header $m.archive}}
@{passes=$script:passes;failures=$script:failures;externalFilesystemAndArchiveBoundarySubstituted=$true;actualMetadataAndBootstrapReaders=$true;archivesOpened=0;nativeExecution=$false;sourceSha256=(Get-FileHash (Join-Path $bridgeReader 'Chunk7BootstrapInventoryFixtures.ps1')).Hash.ToLowerInvariant()}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $bridgeOutput 'receipt.json') -Encoding UTF8
Write-Host ('TOTAL PASS='+$script:passes+' FAIL='+$script:failures);if($script:failures){throw 'Bootstrap source qualification regressions failed'}
