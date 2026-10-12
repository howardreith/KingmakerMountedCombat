[CmdletBinding()]
param([string]$RepositoryRoot,[string]$ReaderDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
# Windows PowerShell -File resolves script-root defaults after parameter binding.
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=Join-Path $PSScriptRoot '../..'}
$RepositoryRoot=[IO.Path]::GetFullPath($RepositoryRoot)
if(-not$PSBoundParameters.ContainsKey('ReaderDirectory')){$ReaderDirectory=Join-Path $PSScriptRoot '../runtime'}
$ReaderDirectory=[IO.Path]::GetFullPath($ReaderDirectory)
. (Join-Path $PSScriptRoot 'Test-Chunk7OrdinaryEvidence.ps1') -RepositoryRoot $RepositoryRoot -ReaderDirectory $ReaderDirectory -FixturesOnly
$script:sourceLab=Join-Path (Split-Path -Parent (Split-Path -Parent $RepositoryRoot)) ('analysis-cache/chunk7-continuation/ordinary-choice/source-fixtures/'+[Guid]::NewGuid().ToString('N'))
if(-not$script:sourceLab.StartsWith('C:\Dev\KingmakerMountedCombatLab\analysis-cache\',[StringComparison]::OrdinalIgnoreCase)){throw 'Focused ordinary fixtures must remain inside the dedicated analysis cache.'}
[void][IO.Directory]::CreateDirectory($script:sourceLab)
function Get-KmcLabRoot {return $script:sourceLab}
function Write-OJson([string]$Path,$Value){[IO.File]::WriteAllText($Path,(ConvertTo-KmcChunk7Canonical $Value),[Text.UTF8Encoding]::new($false))}
function Pin-OSource($Fixture){
    $artifactPath=Join-Path $Fixture.root 'chunk7-world-evidence.json';Write-OJson $artifactPath $Fixture.artifact
    $Fixture.selection.inventoryArtifactSha256=Get-KmcSha256 $artifactPath
    $Fixture.manifest.artifacts[0].sha256=$Fixture.selection.inventoryArtifactSha256;$Fixture.manifest.artifacts[0].length=[long](Get-Item -LiteralPath $artifactPath).Length
    $manifestPath=Join-Path $Fixture.root 'runtime-artifacts.json';Write-OJson $manifestPath $Fixture.manifest
    $Fixture.game.evidenceManifestSha256=Get-KmcSha256 $manifestPath;Write-OJson (Join-Path $Fixture.root 'runtime-game-result.json') $Fixture.game
    $Fixture.result.gameResultSha256=Get-KmcSha256 (Join-Path $Fixture.root 'runtime-game-result.json');$Fixture.result.evidenceManifestSha256=$Fixture.game.evidenceManifestSha256
    Write-OJson (Join-Path $Fixture.root 'runtime-result.json') $Fixture.result
    Write-OJson (Join-Path $Fixture.root 'runtime-request.json') $Fixture.request
    Write-OJson (Join-Path $Fixture.root 'base-game-isolation-result.json') $Fixture.isolation
}
function New-OSource {
    # Reuse only the existing synthetic world setup, stopping before its tests.
    # No native artifacts or protected archive members are opened by this fixture.
    $source=[IO.File]::ReadAllText((Join-Path $RepositoryRoot 'scripts/Test-Chunk7WorldEvidence.ps1'))
    $marker="Check 'complete synthetic exploration observations'"
    $end=$source.IndexOf($marker,[StringComparison]::Ordinal);if($end-lt0){throw 'Reviewed world fixture setup boundary changed.'}
    $prefix=$source.Substring(0,$end);$prefix=$prefix.Replace(". (Join-Path `$PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')",'')
    . ([scriptblock]::Create($prefix))
    $request|Add-Member -NotePropertyName transactionToken -NotePropertyValue ('a'*64)
    $root=Join-Path $script:sourceLab ('runtime-evidence/source-'+[Guid]::NewGuid().ToString('N'))
    [void][IO.Directory]::CreateDirectory($root);$request.runId=Split-Path -Leaf $root
    $request|Add-Member -NotePropertyName evidenceRoot -NotePropertyValue $root
    $request.fixture|Add-Member -NotePropertyName baseline -NotePropertyValue ([pscustomobject]@{sha256='b'*64})
    $selection=(New-OrdinaryFixture).request.ordinaryAcquisition;$selection.inventoryRunId=$request.runId
    $rig=[pscustomobject]@{present=$true;name=$selection.skeletonName}
    $preset=[pscustomobject]@{present=$true;presetGuid=$selection.smallPresetGuid;raceId='SyntheticSmall';maleSkeleton=$rig;femaleSkeleton=(Copy-O $rig)}
    $inventory=[pscustomobject]@{schemaVersion=1;captureStatus='PASS';gameplayAssemblyMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7';profileAdmissionChanged=$false;
        smallRaces=@([pscustomobject]@{raceGuid=$selection.smallRaceGuid;raceId='SyntheticSmall';size='Small';presets=@($preset)});party=@([pscustomobject]@{id='d17c8fd0-6627-40c4-bf9a-024cf001c1f7';petId=$null})}
    $good.runId=$request.runId;$good.observations|Add-Member -NotePropertyName nativeProfileInventory -NotePropertyValue $inventory
    $game=[ordered]@{status='PASS';errors=@();assertionFailCount=0;subscenarioFailCount=0;evidenceManifestSha256='0'*64}
    foreach($field in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid','transactionToken')){$game[$field]=$request.$field}
    $result=Copy-O ([pscustomobject]$game)
    foreach($field in @('modsRestored','workingRestored','baselineImmutable','saveProtectionPassed','saveWriteAllowlistPassed')){$result|Add-Member -NotePropertyName $field -NotePropertyValue $true}
    $result|Add-Member -NotePropertyName restoredSaveInventoryDigest -NotePropertyValue ('d'*64);$result|Add-Member -NotePropertyName gameResultSha256 -NotePropertyValue ('0'*64)
    $isolation=[ordered]@{runId=$request.runId;stagingMode='base-game-only-kmc'}
    foreach($field in @('stageVerified','dependenciesUnchanged','processExited','modsRestored','saveProtectionPassed','gameReportedPass','nativeInventoryObserved','nativeCompatibilityQualified')){$isolation[$field]=$true}
    $visuals=Join-Path $root 'movement-visuals';[void][IO.Directory]::CreateDirectory($visuals)
    $shot=Join-Path $visuals 'chunk7-two-pair-mounted.png';[IO.File]::WriteAllBytes($shot,[Convert]::FromBase64String('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAusB9Wl2m7IAAAAASUVORK5CYII='))
    $manifest=[pscustomobject]@{schemaVersion=1;runId=$request.runId;scenario=$request.scenario;createdAtUtc='2026-10-12T00:00:00Z';artifacts=@([pscustomobject]@{relativePath='chunk7-world-evidence.json';kind='chunk7-world-evidence';length=1L;sha256='0'*64},
        [pscustomobject]@{relativePath='movement-visuals/chunk7-two-pair-mounted.png';kind='screenshot';length=[long](Get-Item -LiteralPath $shot).Length;sha256=(Get-KmcSha256 $shot)})}
    $fixture=[pscustomobject]@{root=$root;request=$request;artifact=$good;selection=$selection;game=[pscustomobject]$game;result=$result;isolation=[pscustomobject]$isolation;manifest=$manifest}
    Pin-OSource $fixture;return $fixture
}
$script:passes=0
Good 'exact restored same-candidate inventory source admits read-only' {
    $f=New-OSource;$before=Get-KmcDirectoryManifest $f.root
    $admitted=Get-KmcChunk7OrdinaryInventorySource $f.selection $f.request.fixture $f.request
    if($admitted.sha256-cne$f.selection.inventoryArtifactSha256){throw 'Source result hash differs.'}
    $after=Get-KmcDirectoryManifest $f.root
    if((ConvertTo-KmcChunk7Canonical $before)-cne(ConvertTo-KmcChunk7Canonical $after)){throw 'Read-only source admission mutated bytes.'}
}
$mutations=[ordered]@{
 'foreign candidate'={param($f)$f.request.dllSha256='f'*64}
 'unrestored actual mods'={param($f)$f.result.modsRestored=$false}
 'string restoration boolean'={param($f)$f.result.workingRestored='true'}
 'source native FAIL'={param($f)$f.game.status='FAIL'}
 'string native failure counter'={param($f)$f.game.assertionFailCount='0'}
 'missing original world proof'={param($f)$f.artifact.observations.inputs=@()}
 'unobserved base-only isolation'={param($f)$f.isolation.nativeInventoryObserved=$false}
 'body admitted by inventory writer'={param($f)$f.artifact.observations.nativeProfileInventory.profileAdmissionChanged=$true}
 'unobserved Small race'={param($f)$f.selection.smallRaceGuid='9'*32}
 'unobserved Small preset'={param($f)$f.selection.smallPresetGuid='9'*32}
 'wrong Small skeleton'={param($f)$f.selection.skeletonName='NotObserved'}
 'duplicate body choice'={param($f)$f.artifact.observations.nativeProfileInventory.smallRaces+=Copy-O $f.artifact.observations.nativeProfileInventory.smallRaces[0]}
 'foreign preset race'={param($f)$f.artifact.observations.nativeProfileInventory.smallRaces[0].presets[0].raceId='Other'}
 'missing native rig'={param($f)$f.artifact.observations.nativeProfileInventory.smallRaces[0].presets[0].maleSkeleton.present=$false}
 'unknown future inventory schema'={param($f)$f.artifact.observations.nativeProfileInventory.schemaVersion=2}
}
foreach($item in $mutations.GetEnumerator()){Bad $item.Key {$f=New-OSource;& $item.Value $f;Pin-OSource $f;[void](Get-KmcChunk7OrdinaryInventorySource $f.selection $f.request.fixture $f.request)}}
Bad 'caller-pinned artifact bytes changed' {$f=New-OSource;[IO.File]::AppendAllText((Join-Path $f.root 'chunk7-world-evidence.json'),' ');[void](Get-KmcChunk7OrdinaryInventorySource $f.selection $f.request.fixture $f.request)}
Good 'immutable inventory copied under one actual owned private lock' {
    $f=New-OSource;$source=Get-KmcChunk7OrdinaryInventorySource $f.selection $f.request.fixture $f.request
    $runId='ordinary-copy';$profile=Join-Path $script:sourceLab ('runtime-staging/persistence-'+$runId)
    [void][IO.Directory]::CreateDirectory($profile);$lock=Open-KmcRuntimeLock (Join-Path $script:sourceLab 'runtime-state') $runId
    try{
        Write-OJson (Join-Path $profile 'owner.json') ([pscustomobject]@{runId=$runId;scenario='chunk7-ordinary-acquisition';transactionToken=$lock.Token})
        $receipt=Copy-KmcChunk7OrdinaryInventory $source $profile $lock
        if($receipt.sha256-cne$source.sha256-or(Get-KmcSha256 $receipt.path)-cne$source.sha256){throw 'Guarded copied inventory differs.'}
        Bad 'existing immutable source inventory overwritten' {[void](Copy-KmcChunk7OrdinaryInventory $source $profile $lock)}
        $badProfile=Join-Path $script:sourceLab 'runtime-staging/persistence-foreign';[void][IO.Directory]::CreateDirectory($badProfile)
        Write-OJson (Join-Path $badProfile 'owner.json') ([pscustomobject]@{runId='foreign';scenario='chunk7-ordinary-acquisition';transactionToken=$lock.Token})
        Bad 'other owned profile under this lock' {[void](Copy-KmcChunk7OrdinaryInventory $source $badProfile $lock)}
    }finally{Close-KmcRuntimeLock $lock}
}
Good 'explicit pins file admitted read-only' {$f=New-OSource;$path=Join-Path $script:sourceLab 'explicit-selection.json';Write-OJson $path $f.selection;$hash=Get-KmcSha256 $path;$r=Read-KmcChunk7OrdinarySelectionFile $path 'new-run';if($r.sha256-cne$hash-or(Get-KmcSha256 $path)-cne$hash){throw 'Selection read mutated file.'}}
Bad 'selection outside isolated test lab' {[void](Read-KmcChunk7OrdinarySelectionFile (Join-Path (Split-Path -Parent $script:sourceLab) 'foreign.json') 'new-run')}
Write-Host ('TOTAL PASS '+$script:passes+' FAIL 0; synthetic source and actual private filesystem/lock checks only; fixtures retained at '+$script:sourceLab)
