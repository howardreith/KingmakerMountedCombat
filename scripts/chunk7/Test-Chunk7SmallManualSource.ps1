[CmdletBinding()]
param([string]$RepositoryRoot,[string]$ReaderDirectory,[string]$FixtureDirectory,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$ReaderDirectory){$ReaderDirectory=Join-Path $RepositoryRoot 'scripts/runtime'}
if(-not$FixtureDirectory){$FixtureDirectory=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics'}
if(-not$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/cumulative-source-'+[Guid]::NewGuid().ToString('N'))}
$sourceReader=[IO.Path]::GetFullPath($ReaderDirectory);$sourceOutput=[IO.Path]::GetFullPath($OutputDirectory);$sourceFixtures=[IO.Path]::GetFullPath($FixtureDirectory)
[void][IO.Directory]::CreateDirectory($sourceOutput)
. (Join-Path $sourceReader 'RuntimeHarness.Common.ps1')
. (Join-Path $sourceReader 'Chunk7SmallCalibrationFixtures.ps1')
$script:passes=0;$script:virtualLab=Join-Path $sourceOutput 'virtual-lab';$script:failure=$null;$script:drift=$null
# External filesystem, previously-qualified lineage and manifest boundaries are
# substituted. This executes the real new qualifier and source-pin predicates;
# no archive file is created/opened and no old validator is redefined on disk.
function Get-KmcLabRoot {$script:virtualLab}
function Boundary([string]$Name){[void]$script:calls.Add($Name);if($script:failure-ceq$Name){throw ('external boundary refused: '+$Name)}}
function Assert-KmcChildPath($Path,$Parent,$Description){$p=[IO.Path]::GetFullPath($Path);$q=[IO.Path]::GetFullPath($Parent).TrimEnd('\')+'\';if(-not$p.StartsWith($q,[StringComparison]::OrdinalIgnoreCase)){throw 'foreign path'};$p}
function Assert-KmcDirectoryTreeCloneable($Path,$Description){Boundary 'cloneable'}
function Assert-KmcNotReparsePoint($Path,$Description){Boundary 'not-reparse'}
function Assert-KmcNotHardLink($Path,$Description){Boundary 'not-hardlink'}
function Assert-KmcManifestJsonMembersUnique($Path,$Description){Boundary 'unique-json'}
function Read-KmcJson($Path){if(-not$script:documents.ContainsKey($Path)){throw ('unadmitted receipt '+$Path)};return $script:documents[$Path]}
function Get-KmcSha256($Path){if(-not$script:hashes.ContainsKey($Path)){throw ('unadmitted hash '+$Path)};if(-not$script:readCounts.ContainsKey($Path)){$script:readCounts[$Path]=0};$script:readCounts[$Path]++;if($script:drift-ceq$Path-and$script:readCounts[$Path]-gt1){return ('0'*64)};return $script:hashes[$Path]}
function Get-Item([string]$LiteralPath){if(-not$script:hashes.ContainsKey($LiteralPath)){throw 'unknown virtual metadata'};[pscustomobject]@{Length=[long]100;LastWriteTimeUtc=[DateTime]::new(638960000000000000,[DateTimeKind]::Utc)}}
function Get-KmcChunk7AcquiredManualSource($Selection,$Fixture,$Candidate){Boundary 'full-bootstrap-cold-lineage';[pscustomobject]@{descriptor=$script:sourceRequest.persistenceLoad;bootstrapRunId='bootstrap';coldInventoryRunId='cold';manualRunId='manual'}}
function Assert-KmcReadOnlyArtifactManifest($Request,$Hash,[switch]$GameResult){Boundary 'strict-manifest';if($Hash-cne$script:manifestHash-or-not$GameResult){throw 'manifest bound incorrectly'}}
function Assert-KmcChunk7AcquiredManualArtifact($Request,$Manifest,$Status){Boundary 'full-manual-artifact';if($Status-cne'PASS'){throw 'source failure misclassified'}}
function Read-KmcChunk7ManifestArtifact($Request,$Manifest,$Leaf,$Kind){if($Leaf-cne'chunk7-acquired-manual-evidence.json'-or$Kind-cne'chunk7-acquired-manual-evidence'){throw 'foreign artifact role'};return $script:artifact}
function Assert-KmcChunk7AcquiredManualOutput($Request,$Artifact){Boundary 'settled-native-output'}
function New-Case([bool]$Paid=$true){
 $f=[IO.File]::ReadAllText((Join-Path $sourceFixtures 'SmallManualFixture.json'))|ConvertFrom-Json
 $sourceArtifact=$f.artifact|ConvertTo-Json -Depth 100|ConvertFrom-Json;$sourceArtifact.observations=$f.artifact.observations.source.acquiredManual;$sourceArtifact.scenario='chunk7-acquired-manual-derivative';$sourceArtifact.runId=$f.request.smallCalibration.manualSource.sourceRunId;$sourceArtifact.evidenceKind='chunk7-acquired-manual-evidence';$f|Add-Member source $sourceArtifact
 $script:calls=[Collections.Generic.List[string]]::new();$script:failure=$null;$script:drift=$null;$script:documents=@{};$script:hashes=@{};$script:readCounts=@{}
 $s=$f.request.smallCalibration.manualSource;$script:artifact=$f.source;$run=$s.sourceRunId;$root=Join-Path $script:virtualLab ('runtime-evidence/'+$run);$profile=Join-Path $script:virtualLab ('runtime-staging/persistence-'+$run);$script:sourceRoot=$root
 $script:sourceRequest=[pscustomobject]@{scenario='chunk7-acquired-manual-derivative';runId=$run;evidenceRoot=$root;branch=$f.request.branch;commit=$f.request.commit;productVersion=$f.request.productVersion;dllSha256=$f.request.dllSha256;dllMvid=$f.request.dllMvid;transactionToken=('a'*64);fixture=$f.request.fixture;acquiredManual=$f.source.observations.selection;
  persistenceLoad=[pscustomobject]@{internalName='Autosave';fileName='Auto_1.zks';sha256=$f.source.observations.selection.sourceArchiveSha256;length=100;lastWriteTimeUtcTicks=1;gameId=$f.request.persistenceLoad.gameId;gameName=$f.request.persistenceLoad.gameName;area=$f.request.persistenceLoad.area}}
 $script:manifestHash='b'*64;$gameHash='c'*64;$result=[ordered]@{status='PASS';errors=@();evidenceManifestSha256=$script:manifestHash;gameResultSha256=$gameHash;restoredSaveInventoryDigest=('d'*64);modsRestored=$true;workingRestored=$true;baselineImmutable=$true;saveProtectionPassed=$true;saveWriteAllowlistPassed=$true;assertionFailCount=0;subscenarioFailCount=0;baselineLoadRequestCount=0;unauthorizedLoadRequestCount=0;unauthorizedSaveRequestCount=0;loadRequestCount=1;workingLoadRequestCount=1;saveRequestCount=1;workingSaveRequestCount=1;fixtureIdentityVerified=$true}
 foreach($key in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid','transactionToken')){$result[$key]=$script:sourceRequest.$key}
 $result=[pscustomobject]$result;$game=$result|ConvertTo-Json -Depth 20|ConvertFrom-Json
 $isolation=[pscustomobject]@{runId=$run;stagingMode='base-game-only-kmc';stageVerified=$true;dependenciesUnchanged=$true;processExited=$true;modsRestored=$true;saveProtectionPassed=$true;gameReportedPass=$true;nativeInventoryObserved=$true;nativeCompatibilityQualified=$true}
 $owner=[pscustomobject]@{runId=$run;scenario=$script:sourceRequest.scenario;transactionToken=$script:sourceRequest.transactionToken;sourceSha256=$script:sourceRequest.acquiredManual.sourceArchiveSha256;saveRoot=(Join-Path $profile 'Saved Games');persistenceCase=$null}
 $script:documents[(Join-Path $root 'runtime-request.json')]=$script:sourceRequest;$script:documents[(Join-Path $root 'runtime-result.json')]=$result;$script:documents[(Join-Path $root 'runtime-game-result.json')]=$game;$script:documents[(Join-Path $root 'base-game-isolation-result.json')]=$isolation;$script:documents[(Join-Path $root 'runtime-artifacts.json')]=[pscustomobject]@{artifacts=@()};$script:documents[(Join-Path $root 'chunk7-acquired-manual-evidence.json')]=$script:artifact;$script:documents[(Join-Path $profile 'owner.json')]=$owner
 foreach($p in $script:documents.Keys){$script:hashes[$p]='e'*64};$script:hashes[(Join-Path $root 'runtime-artifacts.json')]=$script:manifestHash;$script:hashes[(Join-Path $root 'runtime-game-result.json')]=$gameHash;$script:hashes[(Join-Path $root 'chunk7-acquired-manual-evidence.json')]=$s.sourceArtifactSha256
 $script:artifact.observations.nativeWrite.path=Join-Path (Join-Path $profile 'Saved Games') $f.request.persistenceLoad.fileName;$script:hashes[$script:artifact.observations.nativeWrite.path]=$s.sourceArchiveSha256
 return [pscustomobject]@{selection=$s;fixture=$f.request.fixture;candidate=$f.request;result=$result;game=$game;isolation=$isolation;owner=$owner;request=$script:sourceRequest;artifact=$script:artifact}
}
function Qualify($C){Get-KmcChunk7SmallManualSource $C.selection $C.fixture $C.candidate}
function Good([string]$Name,[scriptblock]$Action){&$Action;$script:passes++;Write-Host ('PASS '+$Name)}
function Bad([string]$Name,[scriptblock]$Action){$bad=$false;try{&$Action}catch{$bad=$true};if(-not$bad){throw ('Accepted '+$Name)};$script:passes++;Write-Host ('PASS refuses '+$Name)}
# RETIRED (owner decision 2026-10-24, docs/CHUNK7-RETIRED-SMALL-MANUAL-CALIBRATION-2026-10-24.md). The fixture's one-owner source
# is the retired Auto-derived acquisition (Auto_1.zks), which the unchanged acquired-Manual reader refuses since ed40ef95 because it
# is not a closed field Manual. Its former refusal matrix ran against that refused base and passed vacuously; it is not run.
$retiredError=$null;try{[void](Qualify (New-Case $true))}catch{$retiredError=$_.Exception.Message}
if($retiredError-cne'One-owner acquired source is not a closed field Manual.'){throw ('The retired Small Manual source was not refused as a non-closed field Manual: '+$retiredError)}
Write-Host 'RETIRED PASS=1 FAIL=0: the retired Auto-derived Small Manual source is refused as a non-closed field Manual; no refusal matrix runs on a refused base.'
