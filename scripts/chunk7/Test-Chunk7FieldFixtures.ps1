[CmdletBinding()]
param([string]$RepositoryRoot,[string]$ReaderDirectory,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('ReaderDirectory')){$ReaderDirectory=Join-Path $PSScriptRoot '../runtime'}
if(-not$PSBoundParameters.ContainsKey('OutputDirectory')){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/field-fixtures-'+[Guid]::NewGuid().ToString('N'))}
$script:fieldLab=[IO.Path]::GetFullPath($OutputDirectory)
if(-not$script:fieldLab.StartsWith('C:\Dev\KingmakerMountedCombatLab\analysis-cache\',[StringComparison]::OrdinalIgnoreCase)){throw 'Synthetic field fixtures must stay under analysis-cache.'}
[void][IO.Directory]::CreateDirectory($script:fieldLab)
$readerPin=$ReaderDirectory
. (Join-Path $PSScriptRoot 'Test-Chunk7FieldEvidence.ps1') -RepositoryRoot $RepositoryRoot -ReaderDirectory $readerPin -FixturesOnly
function Get-KmcLabRoot {$script:fieldLab}
function Write-F([string]$Path,$Value){[IO.File]::WriteAllText($Path,(ConvertTo-KmcChunk7Canonical $Value),[Text.UTF8Encoding]::new($false))}
function Pin-F($F){
    Write-F (Join-Path $F.root 'chunk7-ordinary-acquisition-evidence.json') $F.artifact
    $F.selection.sourceArtifactSha256=Get-KmcSha256 (Join-Path $F.root 'chunk7-ordinary-acquisition-evidence.json')
    $F.manifest.artifacts[0].sha256=$F.selection.sourceArtifactSha256;$F.manifest.artifacts[0].length=[long](Get-Item -LiteralPath (Join-Path $F.root 'chunk7-ordinary-acquisition-evidence.json')).Length
    Write-F (Join-Path $F.root 'runtime-artifacts.json') $F.manifest
    $F.game.evidenceManifestSha256=Get-KmcSha256 (Join-Path $F.root 'runtime-artifacts.json');Write-F (Join-Path $F.root 'runtime-game-result.json') $F.game
    $F.result.evidenceManifestSha256=$F.game.evidenceManifestSha256;$F.result.gameResultSha256=Get-KmcSha256 (Join-Path $F.root 'runtime-game-result.json')
    Write-F (Join-Path $F.root 'runtime-result.json') $F.result;Write-F (Join-Path $F.root 'runtime-request.json') $F.request
    Write-F (Join-Path $F.root 'base-game-isolation-result.json') $F.isolation;Write-F (Join-Path $F.profile 'owner.json') $F.owner
}
function New-FSource {
    $run='paid-'+[Guid]::NewGuid().ToString('N')
    $paid=(ConvertTo-KmcChunk7Canonical (New-OrdinaryFixture)).Replace('synthetic-ordinary',$run)|ConvertFrom-Json
    $r=$paid.request;$a=$paid.artifact
    $root=Join-Path $script:fieldLab ('runtime-evidence/'+$run);$profile=Join-Path $script:fieldLab ('runtime-staging/persistence-'+$run);$saves=Join-Path $profile 'Saved Games'
    [void][IO.Directory]::CreateDirectory($root);[void][IO.Directory]::CreateDirectory($saves);$r.evidenceRoot=$root
    $r|Add-Member -NotePropertyName transactionToken -NotePropertyValue ('a'*64);$r.fixture|Add-Member -NotePropertyName baseline -NotePropertyValue ([pscustomobject]@{sha256='b'*64})
    foreach($choice in $a.observations.choices){$choice.runId=$run}
    $a.observations.authority.root=$saves;$a.observations.cleanup.authority.root=$saves;$a.observations.nativeWrite.path=Join-Path $saves 'Auto_1.zks'
    $header=[ordered]@{Name='Autosave';Type='Auto';GameId=$r.fixture.working.gameId;GameName=$r.fixture.working.gameName;Area=$r.fixture.working.area;CompatibilityVersion=1;IsAutoLevelupSave=$true}
    $metadata=[ordered]@{SchemaVersion=3;CampaignId=$r.fixture.working.gameId;AreaId=$r.fixture.working.area;Pairs=@();Participations=@();Combat=$null}
    Add-Type -AssemblyName System.IO.Compression
    $stream=[IO.FileStream]::new($a.observations.nativeWrite.path,[IO.FileMode]::CreateNew);$zip=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Create,$false)
    try{foreach($item in @(@{name='header.json';value=$header},@{name='kmc-mounted-state';value=$metadata})){$entry=$zip.CreateEntry($item.name);$writer=[IO.StreamWriter]::new($entry.Open(),[Text.UTF8Encoding]::new($false));try{$writer.Write((ConvertTo-KmcChunk7Canonical $item.value))}finally{$writer.Dispose()}}}finally{$zip.Dispose();$stream.Dispose()}
    $a.observations.nativeWrite.sha256=Get-KmcSha256 $a.observations.nativeWrite.path;$a.observations.nativeWrite.length=[long](Get-Item -LiteralPath $a.observations.nativeWrite.path).Length
    $selection=[pscustomobject]@{case='paid-small-remote-field';sourceRunId=$run;sourceArtifactSha256='e'*64;sourceArchiveSha256=$a.observations.nativeWrite.sha256;actorId=$a.observations.field.actorId;petId=$a.observations.field.petId}
    $game=[ordered]@{status='PASS';errors=@();assertionFailCount=0;subscenarioFailCount=0;evidenceManifestSha256='0'*64}
    foreach($key in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid','transactionToken')){$game[$key]=$r.$key}
    $result=Copy-O ([pscustomobject]$game)
    foreach($key in @('modsRestored','workingRestored','baselineImmutable','saveProtectionPassed','saveWriteAllowlistPassed')){$result|Add-Member -NotePropertyName $key -NotePropertyValue $true}
    $result|Add-Member -NotePropertyName restoredSaveInventoryDigest -NotePropertyValue ('d'*64);$result|Add-Member -NotePropertyName gameResultSha256 -NotePropertyValue ('0'*64)
    $isolation=[ordered]@{runId=$run;stagingMode='base-game-only-kmc'}
    foreach($key in @('stageVerified','dependenciesUnchanged','processExited','modsRestored','saveProtectionPassed','gameReportedPass','nativeInventoryObserved','nativeCompatibilityQualified')){$isolation[$key]=$true}
    $manifest=[pscustomobject]@{schemaVersion=1;runId=$run;scenario=$r.scenario;createdAtUtc='2026-10-12T00:00:00Z';artifacts=@([pscustomobject]@{relativePath='chunk7-ordinary-acquisition-evidence.json';kind='chunk7-ordinary-acquisition-evidence';length=1L;sha256='0'*64})}
    $owner=[pscustomobject]@{runId=$run;scenario=$r.scenario;transactionToken=$r.transactionToken;persistenceCase=$null;sourceSha256=$r.fixture.working.sha256;saveRoot=$saves}
    $f=[pscustomobject]@{root=$root;profile=$profile;request=$r;artifact=$a;selection=$selection;game=[pscustomobject]$game;result=$result;isolation=[pscustomobject]$isolation;manifest=$manifest;owner=$owner;candidate=(Copy-O $r)}
    Pin-F $f;return $f
}
$script:passes=0
Good 'exact restored paid acquisition source admits unchanged Auto_1 read-only' {
    $f=New-FSource;$before=Get-KmcDirectoryManifest $script:fieldLab;$source=Get-KmcChunk7FieldSource $f.selection $f.request.fixture $f.candidate
    if($source.descriptor.sha256-cne$f.selection.sourceArchiveSha256-or$source.descriptor.fileName-cne'Auto_1.zks'){throw 'Wrong source admitted.'}
    if((ConvertTo-KmcChunk7Canonical $before)-cne(ConvertTo-KmcChunk7Canonical (Get-KmcDirectoryManifest $script:fieldLab))){throw 'Read-only source admission mutated files.'}
}
$cases=[ordered]@{
 'foreign candidate'={param($f)$f.request.dllSha256='9'*64}
 'foreign process family'={param($f)$f.request.scenario='chunk7-persistence-save'}
 'not paid acquisition'={param($f)$f.request.ordinaryAcquisition.case='existing-ranger-horse'}
 'foreign profile owner'={param($f)$f.owner.runId='foreign'}
 'different profile token'={param($f)$f.owner.transactionToken='c'*64}
 'missing native restoration'={param($f)$f.result.workingRestored=$false}
 'text native restoration'={param($f)$f.result.modsRestored='true'}
 'native FAIL'={param($f)$f.game.status='FAIL'}
 'string native failure counter'={param($f)$f.game.assertionFailCount='0'}
 'missing base-only observation'={param($f)$f.isolation.nativeCompatibilityQualified=$false}
 'wrong native actor'={param($f)$f.selection.actorId='99999999-1111-4111-8111-111111111111'}
 'wrong native pet'={param($f)$f.selection.petId='99999999-1111-4111-8111-111111111111'}
 'field-before-autosave false claim'={param($f)$f.artifact.observations.nativeWrite.saveBeforeFieldSelection=$false}
 'archive path escapes profile'={param($f)$f.artifact.observations.nativeWrite.path=Join-Path $f.root 'Auto_1.zks'}
 'invented ordinary choices'={param($f)$f.artifact.observations.choices=@()}
 'wrong archive hash'={param($f)$f.selection.sourceArchiveSha256='9'*64}
}
foreach($case in $cases.GetEnumerator()){Bad $case.Key {$f=New-FSource;&$case.Value $f;Pin-F $f;[void](Get-KmcChunk7FieldSource $f.selection $f.request.fixture $f.candidate)}}
Bad 'immutable native archive changed after completed write' {$f=New-FSource;[IO.File]::AppendAllText($f.artifact.observations.nativeWrite.path,'changed');[void](Get-KmcChunk7FieldSource $f.selection $f.request.fixture $f.candidate)}
Good 'owned copy preserves exact source artifact under real private lock' {
    $f=New-FSource;$source=Get-KmcChunk7FieldSource $f.selection $f.request.fixture $f.candidate
    $run='field-copy';$profile=Join-Path $script:fieldLab ('runtime-staging/persistence-'+$run);[void][IO.Directory]::CreateDirectory($profile)
    $lock=Open-KmcRuntimeLock (Join-Path $script:fieldLab 'runtime-state') $run
    try{
        Write-F (Join-Path $profile 'owner.json') ([pscustomobject]@{runId=$run;scenario='chunk7-ordinary-field-admission';transactionToken=$lock.Token;sourceSha256=$source.descriptor.sha256})
        Copy-KmcChunk7FieldSourceArtifact $source $profile $lock
        if((Get-KmcSha256 (Join-Path $profile 'ordinary-acquisition-source.json'))-cne$source.artifactSha256){throw 'Copied source artifact differs.'}
        Bad 'existing staged source is immutable' {Copy-KmcChunk7FieldSourceArtifact $source $profile $lock}
    }finally{Close-KmcRuntimeLock $lock}
}
Good 'explicit field source pins read only' {$f=New-FSource;$path=Join-Path $script:fieldLab 'selection.json';Write-F $path $f.selection;$hash=Get-KmcSha256 $path;$r=Read-KmcChunk7FieldSelectionFile $path 'new-field';if($r.sha256-cne$hash){throw 'Selection mutated.'}}
Write-Host ('TOTAL PASS '+$script:passes+' FAIL 0; synthetic archives/source receipts/private filesystem only; '+$script:fieldLab)
