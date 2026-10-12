[CmdletBinding()]
param([string]$RepositoryRoot,[string]$ReaderDirectory,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('ReaderDirectory')){$ReaderDirectory=Join-Path $PSScriptRoot '../runtime'}
if(-not$PSBoundParameters.ContainsKey('OutputDirectory')){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/progression-fixtures-'+[Guid]::NewGuid().ToString('N'))}
$script:sourceLab=[IO.Path]::GetFullPath($OutputDirectory);$progressionRuntime=$ReaderDirectory
if(-not$script:sourceLab.StartsWith('C:\Dev\KingmakerMountedCombatLab\analysis-cache\',[StringComparison]::OrdinalIgnoreCase)){throw 'Synthetic progression fixtures must remain under analysis-cache.'}
[void][IO.Directory]::CreateDirectory($script:sourceLab)
. (Join-Path $PSScriptRoot 'Test-Chunk7ProgressionEvidence.ps1') -RepositoryRoot $RepositoryRoot -ReaderDirectory $progressionRuntime -FixturesOnly
. (Join-Path $progressionRuntime 'Chunk7PersistenceEvidence.ps1')
. (Join-Path $progressionRuntime 'Chunk7RepeatedWorldEvidence.ps1')
. (Join-Path $progressionRuntime 'Chunk7PersistenceSaveFixtures.ps1')
function Get-KmcLabRoot {$script:sourceLab}
function Write-F([string]$Path,$Value){[IO.File]::WriteAllText($Path,(ConvertTo-KmcChunk7Canonical $Value),[Text.UTF8Encoding]::new($false))}
function Test-FunctionText([string]$Path,[string]$Name){$tokens=$null;$errors=$null;$ast=[Management.Automation.Language.Parser]::ParseFile($Path,[ref]$tokens,[ref]$errors);if($errors){throw 'Existing fixture helper syntax changed.'};$f=@($ast.FindAll({param($n)$n-is[Management.Automation.Language.FunctionDefinitionAst]-and$n.Name-ceq$Name},$true));if($f.Count-ne1){throw ('Existing fixture helper missing: '+$Name)};$f[0].Extent.Text}
# Reuse the exact existing synthetic source builders, not historical native
# archives or protected save members. The strict production intakes run intact.
foreach($name in @('Write-TestJson','Write-TestZip','Pin-TestReceipts','New-TestSource')){. ([scriptblock]::Create((Test-FunctionText (Join-Path $RepositoryRoot 'scripts/chunk7/Test-Chunk7PersistenceSource.ps1') $name)))}
foreach($name in @('Write-OJson','Pin-OSource','New-OSource')){. ([scriptblock]::Create((Test-FunctionText (Join-Path $RepositoryRoot 'scripts/chunk7/Test-Chunk7OrdinaryFixtures.ps1') $name)))}
function New-ProgressionSourceFixture([bool]$Respec=$false){
    $text=[IO.File]::ReadAllText((Join-Path $RepositoryRoot 'scripts/chunk7/Test-Chunk7PersistenceEvidence.ps1'))
    $end=$text.IndexOf("Check 'complete synthetic manual all-pair native observation'",[StringComparison]::Ordinal);if($end-lt0){throw 'Existing persistence fixture boundary changed.'}
    $prefix=$text.Substring(0,$end)
    foreach($line in @('. (Join-Path $PSScriptRoot ''Initialize-Chunk7Tests.ps1'')','. (Join-Path $kmcRepo ''scripts/runtime/RuntimeHarness.Common.ps1'')','. (Join-Path $repo ''scripts/runtime/Chunk7PersistenceEvidence.ps1'')','. (Join-Path $repo ''scripts/runtime/Chunk7PersistenceSaveFixtures.ps1'')')){$prefix=$prefix.Replace($line,'# Existing reader already loaded.')}
    . ([scriptblock]::Create($prefix))
    $manual=New-TestSource ([Guid]::NewGuid().ToString('N'));$manual.request|Add-Member NoteProperty transactionToken $manual.owner.transactionToken
    Pin-TestReceipts $manual
    $inventory=New-OSource
    $inventory.request.fixture.baseline.sha256=$manual.request.fixture.baseline.sha256
    $body=$inventory.artifact.observations.nativeProfileInventory.party[0]
    foreach($item in @(@('raceGuid',('1'*32)),@('originalSize','Medium'),@('effectiveSize','Medium'),@('dollPresetGuid',('2'*32)),@('dollGender','Male'),@('dollLeftHanded',$false),@('character',[pscustomobject]@{skeleton=[pscustomobject]@{present=$true;name='ObservedMediumRig'}}))){$body|Add-Member NoteProperty $item[0] $item[1]}
    Pin-OSource $inventory
    $selection=(New-ProgressionFixture $(if($Respec){'horse-native-retrain-horse'}else{'horse-native-level'})).request.progression
    $selection.sourceRunId=$manual.request.runId;$selection.sourceArchiveSha256=$manual.sha;$selection.sourceArtifactSha256=Get-KmcSha256 (Join-Path $manual.evidence 'chunk7-persistence-evidence.json')
    $selection.inventoryRunId=$inventory.request.runId;$selection.inventoryArtifactSha256=$inventory.selection.inventoryArtifactSha256
    $selection.petId=$manual.artifact.observations.nativeWrite.archive.Pairs[1].Mount.Id
    [pscustomobject]@{manual=$manual;inventory=$inventory;selection=$selection;candidate=(Copy-O $manual.request)}
}
function Read-ProgressionSource($F){Get-KmcChunk7ProgressionSource $F.selection $F.manual.request.fixture $F.candidate}
function Pin-ProgressionSource($F){Pin-TestReceipts $F.manual;Pin-OSource $F.inventory;$F.selection.sourceArtifactSha256=Get-KmcSha256 (Join-Path $F.manual.evidence 'chunk7-persistence-evidence.json');$F.selection.inventoryArtifactSha256=$F.inventory.selection.inventoryArtifactSha256}
$script:passes=0
foreach($respec in @($false,$true)){Good ('two exact qualified inputs remain read-only; respec='+$respec){$f=New-ProgressionSourceFixture $respec;$before=Get-KmcDirectoryManifest $script:sourceLab;$source=Read-ProgressionSource $f;if($source.descriptor.fileName-cne'Manual_300_KMC_C7_WORLD.zks'-or$source.descriptor.sha256-cne$f.manual.sha-or$source.inventory.sha256-cne$f.selection.inventoryArtifactSha256){throw 'Wrong composed source admitted.'};if((ConvertTo-KmcChunk7Canonical $before)-cne(ConvertTo-KmcChunk7Canonical (Get-KmcDirectoryManifest $script:sourceLab))){throw 'Read-only source qualification mutated files.'}}}
$mutations=[ordered]@{
 'foreign source module'={param($f)$f.candidate.dllMvid='aaaaaaaa-1111-4111-8111-111111111111'}
 'foreign candidate DLL'={param($f)$f.candidate.dllSha256='9'*64}
 'missing protected restoration'={param($f)$f.manual.result.workingRestored=$false}
 'native source FAIL'={param($f)$f.manual.game.assertionFailCount=1}
 'missing base-only compatibility'={param($f)$f.manual.isolation.nativeCompatibilityQualified=$false}
 'foreign transaction token'={param($f)$f.manual.owner.transactionToken='8'*64}
 'foreign original pet'={param($f)$f.selection.petId='aaaaaaaa-1111-4111-8111-111111111111'}
 'foreign inventory module'={param($f)$f.inventory.request.dllMvid='aaaaaaaa-1111-4111-8111-111111111111';$f.inventory.game.dllMvid=$f.inventory.request.dllMvid;$f.inventory.result.dllMvid=$f.inventory.request.dllMvid;$f.inventory.artifact.dllMvid=$f.inventory.request.dllMvid}
 'missing inventory native result'={param($f)$f.inventory.game.status='FAIL'}
 'foreign retrain rig'={param($f)$f.inventory.artifact.observations.nativeProfileInventory.party[0].character.skeleton.name='AnotherRig'}
 'foreign retrain size'={param($f)$f.inventory.artifact.observations.nativeProfileInventory.party[0].effectiveSize='Small'}
}
foreach($entry in $mutations.GetEnumerator()){Bad $entry.Key {$f=New-ProgressionSourceFixture $true;&$entry.Value $f;Pin-ProgressionSource $f;[void](Read-ProgressionSource $f)}}
Bad 'pinned source artifact changed'{$f=New-ProgressionSourceFixture;[IO.File]::AppendAllText((Join-Path $f.manual.evidence 'chunk7-persistence-evidence.json'),' ');[void](Read-ProgressionSource $f)}
Bad 'pinned native source archive changed'{$f=New-ProgressionSourceFixture;[IO.File]::AppendAllText($f.manual.path,'changed');[void](Read-ProgressionSource $f)}
Good 'exact two input copies require one current private lock and profile owner'{
    $f=New-ProgressionSourceFixture;$source=Read-ProgressionSource $f;$run='progression-copy';$profile=Join-Path $script:sourceLab ('runtime-staging/persistence-'+$run);[void][IO.Directory]::CreateDirectory($profile)
    $lock=Open-KmcRuntimeLock (Join-Path $script:sourceLab 'runtime-state') $run
    try{Write-F (Join-Path $profile 'owner.json') ([pscustomobject]@{scenario='chunk7-native-progression';runId=$run;transactionToken=$lock.Token;sourceSha256=$source.descriptor.sha256});Copy-KmcChunk7ProgressionInputs $source $profile $lock
        if((Get-KmcSha256 (Join-Path $profile 'progression-source.json'))-cne$source.artifactSha256-or(Get-KmcSha256 (Join-Path $profile 'progression-inventory.json'))-cne$source.inventory.sha256){throw 'Owned immutable copies differ.'}
        Bad 'copied progression inputs cannot be overwritten'{Copy-KmcChunk7ProgressionInputs $source $profile $lock}
        $owner=Read-KmcJson (Join-Path $profile 'owner.json');$owner.transactionToken='f'*64;Write-F (Join-Path $profile 'owner.json') $owner
        Bad 'foreign profile ownership refuses copy'{Copy-KmcChunk7ProgressionInputs $source $profile $lock}
    }finally{Close-KmcRuntimeLock $lock}
}
Good 'explicit progression selection remains immutable on intake'{$f=New-ProgressionSourceFixture;$path=Join-Path $script:sourceLab 'selection.json';Write-F $path $f.selection;$hash=Get-KmcSha256 $path;$selected=Read-KmcChunk7ProgressionSelectionFile $path 'future-progression';if($selected.sha256-cne$hash){throw 'Selection changed.'}}
Write-Host ('TOTAL PASS '+$script:passes+' FAIL 0; synthetic source ZIPs/receipts and private lock only; no native fixture read')
