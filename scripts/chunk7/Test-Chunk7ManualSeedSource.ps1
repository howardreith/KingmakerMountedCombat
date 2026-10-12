[CmdletBinding()]
param([string]$RepositoryRoot,[string]$ReaderDirectory,[string]$FixturePath,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('ReaderDirectory')){$ReaderDirectory=Join-Path $RepositoryRoot 'scripts/runtime'}
if(-not$PSBoundParameters.ContainsKey('FixturePath')){$FixturePath=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/BootstrapInventoryEvidenceFixture.json'}
if(-not$PSBoundParameters.ContainsKey('OutputDirectory')){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/manual-seed-source-'+[Guid]::NewGuid().ToString('N'))}
$manualSourceOutput=[IO.Path]::GetFullPath($OutputDirectory);$manualSourceReaders=[IO.Path]::GetFullPath($ReaderDirectory)
. (Join-Path $PSScriptRoot 'Test-Chunk7ManualSeedEvidence.ps1') -RepositoryRoot $RepositoryRoot -ReaderDirectory $ReaderDirectory -FixturePath $FixturePath -OutputDirectory $OutputDirectory -FixturesOnly
. (Join-Path $manualSourceReaders 'Chunk7BootstrapInventoryFixtures.ps1');. (Join-Path $manualSourceReaders 'Chunk7ManualSeedFixtures.ps1')
[void][IO.Directory]::CreateDirectory($manualSourceOutput);$script:passes=0;$script:failures=0
function Good([string]$Name,[scriptblock]$Action){try{&$Action;$script:passes++;Write-Host ('PASS '+$Name)}catch{$script:failures++;Write-Host ('FAIL '+$Name+': '+$_)}}
# Only external filesystem/manifest/ZIP boundaries are substituted. The actual
# source guard runs with real strict bootstrap artifact and metadata readers.
# No directory, save or archive at these virtual paths is opened or created.
function Get-KmcLabRoot {'C:\ManualSeedVirtual'}
function Assert-KmcChildPath($Path,$Parent,$Description){$Path}
function Assert-KmcDirectoryTreeCloneable($Path,$Description){if($script:boundaryFailure-ceq'tree'){throw 'reparse tree'}}
function Assert-KmcNotReparsePoint($Path,$Description){if($script:boundaryFailure-ceq'reparse'){throw 'reparse'}}
function Assert-KmcNotHardLink($Path,$Description){if($script:boundaryFailure-ceq'hardlink'){throw 'hardlink'}}
function Assert-KmcManifestJsonMembersUnique($Path,$Description){if($script:boundaryFailure-ceq'duplicate'){throw 'duplicate JSON member'}}
function Get-KmcSha256($Path){if([IO.Path]::GetFileName($Path)-ceq'chunk7-bootstrap-inventory-evidence.json'){return $script:virtualInventoryHash};$leaf=[IO.Path]::GetFileName($Path);if($leaf-ceq'chunk7-native-bootstrap-evidence.json'){return $script:virtualArtifactHash};if($leaf-ceq'Auto_1.zks'){return $script:f.request.manualSeed.sourceArchiveSha256};return ('f'*64)}
function Get-Item($LiteralPath){[pscustomobject]@{Length=100;LastWriteTimeUtc=[datetime]::new(1);PSIsContainer=$false;Attributes=[IO.FileAttributes]::Normal}}
function Read-KmcJson($Path){if([IO.Path]::GetFileName($Path)-ceq'chunk7-native-bootstrap-evidence.json'){return $script:f.source};if($Path.Contains($script:f.request.manualSeed.inventoryRunId)){return $script:coldFiles[[IO.Path]::GetFileName($Path)]};switch([IO.Path]::GetFileName($Path)){'runtime-request.json'{$script:f.sourceRequest;break}'runtime-result.json'{$script:result;break}'runtime-game-result.json'{$script:game;break}'runtime-artifacts.json'{$script:manifest;break}'base-game-isolation-result.json'{$script:isolation;break}'owner.json'{$script:owner;break}default{throw 'Unexpected virtual read'}}}
function Assert-KmcReadOnlyArtifactManifest($Request,$Sha,[switch]$GameResult){if($Sha-cne('f'*64)){throw 'manifest hash'}}
function Read-KmcChunk7ManifestArtifact($Request,$Manifest,$Leaf,$Kind){if($Leaf-ceq'chunk7-bootstrap-inventory-evidence.json'-and$Kind-ceq'chunk7-bootstrap-inventory-evidence'){return $script:f.inventory};if($Leaf-cne'chunk7-native-bootstrap-evidence.json'-or$Kind-cne'chunk7-native-bootstrap-evidence'){throw 'unexpected native source artifact'};$script:f.source}
function Assert-KmcChunk7BootstrapInventoryArchive($Request,$Artifact){$script:archiveBoundaries++;Assert-KmcChunk7NativeBootstrapOutput $Request $Artifact}
function Reset-BootstrapSource {
 $script:f=(ConvertTo-Json -InputObject (New-ManualSeedFixture) -Depth 100 -Compress).Replace('C:\\Dev\\KingmakerMountedCombatLab','C:\\ManualSeedVirtual')|ConvertFrom-Json
 $script:archiveBoundaries=0;$script:boundaryFailure='';$script:virtualArtifactHash=$script:f.request.manualSeed.sourceArtifactSha256;$r=$script:f.sourceRequest
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

function Reset-Source {
 Reset-BootstrapSource;$script:virtualInventoryHash=$script:f.request.manualSeed.inventoryArtifactSha256;$script:coldFiles=@{}
 $r=$script:f.inventoryRequest;$receipt=[ordered]@{};foreach($key in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid','transactionToken')){$receipt[$key]=$r.$key};$receipt.status='PASS';$receipt.errors=@();$receipt.evidenceManifestSha256='f'*64
 $coldResult=Copy-SeedJson ([pscustomobject]$receipt);$coldGame=Copy-SeedJson ([pscustomobject]$receipt)
 foreach($key in @('modsRestored','workingRestored','baselineImmutable','saveProtectionPassed','saveWriteAllowlistPassed')){$coldResult|Add-Member -NotePropertyName $key -NotePropertyValue $true}
 $coldResult|Add-Member -NotePropertyName restoredSaveInventoryDigest -NotePropertyValue ('e'*64);$coldResult|Add-Member -NotePropertyName gameResultSha256 -NotePropertyValue ('f'*64)
 foreach($key in @('assertionFailCount','subscenarioFailCount','baselineLoadRequestCount','unauthorizedLoadRequestCount','unauthorizedSaveRequestCount','saveRequestCount','workingSaveRequestCount')){$coldGame|Add-Member -NotePropertyName $key -NotePropertyValue 0}
 foreach($key in @('loadRequestCount','workingLoadRequestCount')){$coldGame|Add-Member -NotePropertyName $key -NotePropertyValue 1};$coldGame|Add-Member -NotePropertyName fixtureIdentityVerified -NotePropertyValue $true
 $profile=Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$r.runId)
 $coldOwner=[pscustomobject]@{runId=$r.runId;scenario=$r.scenario;transactionToken=$r.transactionToken;sourceSha256=$r.bootstrapInventory.sourceArchiveSha256;saveRoot=(Join-Path $profile 'Saved Games');persistenceCase=$null}
 $coldIsolation=Copy-SeedJson $script:isolation;$coldIsolation.runId=$r.runId
 $script:coldFiles=@{'runtime-request.json'=$r;'runtime-result.json'=$coldResult;'runtime-game-result.json'=$coldGame;'base-game-isolation-result.json'=$coldIsolation;'owner.json'=$coldOwner;'runtime-artifacts.json'=[pscustomobject]@{artifacts=@([pscustomobject]@{relativePath='chunk7-bootstrap-inventory-evidence.json';kind='chunk7-bootstrap-inventory-evidence'})}}
}
function Get-Source {Get-KmcChunk7ManualSeedSource $script:f.request.manualSeed $script:f.request.fixture $script:f.request}
Good 'same-candidate native bootstrap and cold lineage qualifies once' {Reset-Source;$out=Get-Source;if($script:archiveBoundaries-ne1-or$out.descriptor.fileName-cne'Auto_1.zks'-or$out.inventorySha256-cne$script:f.request.manualSeed.inventoryArtifactSha256){throw 'Qualified immutable lineage lost identity'}}
$cases=[ordered]@{
 'bootstrap failed'={$script:result.status='FAIL'}
 'bootstrap missing restoration'={$script:result.modsRestored=$false}
 'bootstrap other candidate'={$script:f.sourceRequest.dllMvid='aaaaaaaa-1111-4111-8111-111111111111'}
 'cold failed'={$script:coldFiles['runtime-result.json'].status='FAIL'}
 'cold game failed'={$script:coldFiles['runtime-game-result.json'].status='FAIL'}
 'cold missing Mods restoration'={$script:coldFiles['runtime-result.json'].modsRestored=$false}
 'cold missing Working restoration'={$script:coldFiles['runtime-result.json'].workingRestored=$false}
 'cold protection failed'={$script:coldFiles['runtime-result.json'].saveProtectionPassed=$false}
 'cold missing restoration digest'={$script:coldFiles['runtime-result.json'].restoredSaveInventoryDigest=''}
 'cold installed mods rather than base only'={$script:coldFiles['base-game-isolation-result.json'].stagingMode='installed-mods'}
 'cold process not exited'={$script:coldFiles['base-game-isolation-result.json'].processExited=$false}
 'cold compatibility not proved'={$script:coldFiles['base-game-isolation-result.json'].nativeCompatibilityQualified=$false}
 'cold other candidate'={$script:f.inventoryRequest.dllSha256='f'*64}
 'cold source changed'={$script:f.inventoryRequest.persistenceLoad.sha256='f'*64}
 'cold not exact load'={$script:coldFiles['runtime-game-result.json'].fixtureIdentityVerified=$false}
 'cold second load'={$script:coldFiles['runtime-game-result.json'].loadRequestCount=2}
 'cold native write'={$script:coldFiles['runtime-game-result.json'].saveRequestCount=1}
 'cold wrong owner token'={$script:coldFiles['owner.json'].transactionToken='a'*64}
 'cold source hash missing'={$script:coldFiles['owner.json'].sourceSha256='a'*64}
 'cold wrong run'={$script:coldFiles['owner.json'].runId='foreign'}
 'cold artifact pin changed'={$script:virtualInventoryHash='a'*64}
 'cold one-save mode'={$script:f.inventory.observations.fixtureReadiness.saveSettings.onlyOneSave=$true}
 'cold preparation observed'={$script:f.inventory.observations.noPreparation.events=@([pscustomobject]@{kind='prepare-before'})}
 'cold cleanup pending'={$script:f.inventory.observations.cleanup.custodySettled=$false}
 'cold artifact candidate array'={$script:f.inventory.dllMvid=@($script:f.inventory.dllMvid)}
 'cold result status array'={$script:coldFiles['runtime-result.json'].status=@('PASS')}
 'cold native quota text'={$script:coldFiles['runtime-game-result.json'].loadRequestCount='1'}
}
foreach($pair in $cases.GetEnumerator()){Good ('refuses '+$pair.Key){Reset-Source;&$pair.Value;$refused=$false;try{[void](Get-Source)}catch{$refused=$true};if(-not$refused){throw 'Unqualified lineage admitted'}}}
foreach($boundary in @('tree','reparse','hardlink','duplicate')){Good ('unsafe external boundary '+$boundary){Reset-Source;$script:boundaryFailure=$boundary;$refused=$false;try{[void](Get-Source)}catch{$refused=$true};if(-not$refused-or$script:archiveBoundaries-ne0){throw 'Unsafe provenance reached archive boundary'}}}
@{schemaVersion=1;pass=$script:passes;fail=$script:failures;actualBootstrapAndColdReaders=$true;externalFilesystemManifestAndArchiveBoundarySubstituted=$true;archivesOpened=0;nativeExecution=$false;sourceSha256=(Get-FileHash -LiteralPath (Join-Path $manualSourceReaders 'Chunk7ManualSeedFixtures.ps1')).Hash.ToLowerInvariant()}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $manualSourceOutput 'receipt.json') -Encoding UTF8
Write-Host ('TOTAL PASS='+$script:passes+' FAIL='+$script:failures);if($script:failures){throw 'Manual seed source qualification failed'}
