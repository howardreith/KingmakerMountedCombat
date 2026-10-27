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
. (Join-Path $sourceReader 'Chunk7CumulativeAcquisitionFixtures.ps1')
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
 $tag=if($Paid){'Paid'}else{'Existing'};$json=[IO.File]::ReadAllText((Join-Path $sourceFixtures ('Cumulative'+$tag+'Fixture.json')));$f=$json.Replace('Manual_7_KMC_C7_ACQUIRED_FIELD.zks','Manual_302_KMC_C7_ACQUIRED_FIELD.zks')|ConvertFrom-Json
 $f.request.cumulativeAcquisition.nativeWriteLeaf='Manual_303_KMC_C7_MANUAL_DERIVED_FIELD.zks'
 $script:calls=[Collections.Generic.List[string]]::new();$script:failure=$null;$script:drift=$null;$script:documents=@{};$script:hashes=@{};$script:readCounts=@{}
 $s=$f.request.cumulativeAcquisition;$script:artifact=$f.source;$run=$s.sourceRunId;$root=Join-Path $script:virtualLab ('runtime-evidence/'+$run);$profile=Join-Path $script:virtualLab ('runtime-staging/persistence-'+$run);$script:sourceRoot=$root
 $script:sourceRequest=[pscustomobject]@{scenario='chunk7-acquired-manual-derivative';runId=$run;evidenceRoot=$root;branch=$f.request.branch;commit=$f.request.commit;productVersion=$f.request.productVersion;dllSha256=$f.request.dllSha256;dllMvid=$f.request.dllMvid;transactionToken=('a'*64);fixture=$f.request.fixture;acquiredManual=$f.source.observations.selection;
  persistenceLoad=[pscustomobject]@{internalName='KMC_C7_MANUAL_DERIVED_FIELD';fileName='Manual_304_KMC_C7_MANUAL_DERIVED_FIELD.zks';sha256=$f.source.observations.selection.sourceArchiveSha256;length=100;lastWriteTimeUtcTicks=1;gameId=$f.request.persistenceLoad.gameId;gameName=$f.request.persistenceLoad.gameName;area=$f.request.persistenceLoad.area}}
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
function Qualify($C){Get-KmcChunk7CumulativeSource $C.selection $C.fixture $C.candidate}
function Good([string]$Name,[scriptblock]$Action){&$Action;$script:passes++;Write-Host ('PASS '+$Name)}
function Bad([string]$Name,[scriptblock]$Action){$bad=$false;try{&$Action}catch{$bad=$true};if(-not$bad){throw ('Accepted '+$Name)};$script:passes++;Write-Host ('PASS refuses '+$Name)}
foreach($paid in @($true,$false)){Good ('exact real qualifier body '+$paid){$c=New-Case $paid;$r=Qualify $c;if($r.descriptor.fileName-cne'Manual_302_KMC_C7_ACQUIRED_FIELD.zks'-or$r.descriptor.sha256-cne$c.selection.sourceArchiveSha256){throw 'Observed descriptor lost'};foreach($gate in @('full-bootstrap-cold-lineage','strict-manifest','full-manual-artifact','settled-native-output')){if(@($script:calls|Where-Object{$_-ceq$gate}).Count-ne1){throw ('Required external gate omitted '+$gate)}}}}
function New-OwnerSeedCase {
 $c=New-Case
 $c.owner.persistenceCase=''
 $c.selection|Add-Member -NotePropertyName sourceMode -NotePropertyValue 'owner-seed-one-owner'
 $c.selection.nativeWriteLeaf='Manual_306_KMC_C7_MANUAL_DERIVED_FIELD.zks'
 $c.candidate.persistenceLoad.fileName='Manual_305_KMC_C7_ACQUIRED_FIELD.zks'
 $c.artifact.observations.nativeWrite.fileName='Manual_305_KMC_C7_ACQUIRED_FIELD.zks'
 $path=Join-Path (Split-Path $c.artifact.observations.nativeWrite.path -Parent) 'Manual_305_KMC_C7_ACQUIRED_FIELD.zks'
 $c.artifact.observations.nativeWrite.path=$path
 $script:hashes[$path]=$c.selection.sourceArchiveSha256
 $c
}
Good 'owner-seed one-owner source 305 qualifies only with exact Manual 304 ancestry' {$c=New-OwnerSeedCase;$r=Qualify $c;if($r.descriptor.fileName-cne'Manual_305_KMC_C7_ACQUIRED_FIELD.zks'){throw 'Owner-seed descriptor differs.'}}
Bad 'owner-seed 305 without explicit mode' {$c=New-OwnerSeedCase;$c.selection.PSObject.Properties.Remove('sourceMode');[void](Qualify $c)}
Bad 'owner-seed 305 with wrong output 303' {$c=New-OwnerSeedCase;$c.selection.nativeWriteLeaf='Manual_303_KMC_C7_MANUAL_DERIVED_FIELD.zks';[void](Qualify $c)}
Bad 'owner-seed 305 without Manual 304 ancestry' {$c=New-OwnerSeedCase;$script:sourceRequest.persistenceLoad.fileName='Manual_2_KMC_C7_MANUAL_DERIVED_FIELD.zks';[void](Qualify $c)}
Bad 'owner-seed 305 with named persistence case' {$c=New-OwnerSeedCase;$c.owner.persistenceCase='multi-exploration-manual';[void](Qualify $c)}
Bad 'owner-seed 305 with array persistence case' {$c=New-OwnerSeedCase;$c.owner.persistenceCase=@('');[void](Qualify $c)}
foreach($gate in @('cloneable','not-reparse','not-hardlink','unique-json','full-bootstrap-cold-lineage','strict-manifest','full-manual-artifact','settled-native-output')){Bad ('mandatory external refusal '+$gate){$c=New-Case;$script:failure=$gate;[void](Qualify $c)}}
$cases=[ordered]@{
 'source already has two owners'={param($c)$c.request.acquiredManual.case='preserve-two-acquired-field'}
 'prior owner substituted'={param($c)$c.selection.priorOwnerId='99999999-1111-4111-8111-111111111111'}
 'prior pet substituted'={param($c)$c.selection.priorPetId='99999999-2222-4222-8222-222222222222'}
 'native prior relationship removed'={param($c)$c.artifact.observations.nativeProfileInventory.party[0].petId=$null}
 'candidate SHA'={param($c)$c.candidate.dllSha256='0'*64}
 'candidate MVID'={param($c)$c.candidate.dllMvid='11111111-1111-4111-8111-111111111111'}
 'source status FAIL'={param($c)$c.result.status='FAIL'}
 'source error'={param($c)$c.game.errors=@('failed')}
 'missing Mods restoration'={param($c)$c.result.modsRestored=$false}
 'missing save restoration'={param($c)$c.result.saveProtectionPassed=$false}
 'source save changed'={param($c)$c.result.workingRestored=$false}
 'source second load'={param($c)$c.game.loadRequestCount=2}
 'source unauthorized write'={param($c)$c.game.unauthorizedSaveRequestCount=1}
 'native load identity not verified'={param($c)$c.game.fixtureIdentityVerified=$false}
 'source base-only unqualified'={param($c)$c.isolation.nativeCompatibilityQualified=$false}
 'owner token changed'={param($c)$c.owner.transactionToken='0'*64}
 'foreign source role'={param($c)$c.owner.scenario='chunk7-bootstrap-cold-inventory'}
 'foreign persistence case'={param($c)$c.owner.persistenceCase='multi-exploration-manual'}
 'source metadata substituted'={param($c)$c.artifact.observations.nativeWrite.sha256='0'*64}
 'other valid acquired Manual number'={param($c)$c.artifact.observations.nativeWrite.fileName='Manual_305_KMC_C7_ACQUIRED_FIELD.zks'}
 'caller readiness money changed'={param($c)$c.selection.readiness.money++}
 'native cost exceeds Int32'={param($c)$c.selection.readiness.nativeHireCost=2147483648}
 'source restore digest absent'={param($c)$c.result.restoredSaveInventoryDigest=$null}
 'source result body hash unbound'={param($c)$c.result.gameResultSha256='0'*64}
 'source receipt changes during qualification'={param($c)$script:drift=Join-Path $script:sourceRoot 'runtime-result.json'}
}
foreach($pair in $cases.GetEnumerator()){Bad $pair.Key {$c=New-Case;&$pair.Value $c;[void](Qualify $c)}}
@{schemaVersion=1;pass=$script:passes;fail=0;scope='actual new qualifier; existing lineage/manifest and OS file boundaries substituted';nativeExecution=$false;archiveAccess=$false;sourceSha256=(Get-FileHash -LiteralPath (Join-Path $sourceReader 'Chunk7CumulativeAcquisitionFixtures.ps1')).Hash.ToLowerInvariant()}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $sourceOutput 'receipt.json') -Encoding UTF8
Write-Host ('TOTAL PASS='+$script:passes+' FAIL=0')
