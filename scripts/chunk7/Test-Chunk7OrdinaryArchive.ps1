[CmdletBinding()]
param([string]$RepositoryRoot,[string]$ReaderDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
# Windows PowerShell -File resolves script-root defaults after parameter binding.
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=Join-Path $PSScriptRoot '../..'}
$RepositoryRoot=[IO.Path]::GetFullPath($RepositoryRoot)
if(-not$PSBoundParameters.ContainsKey('ReaderDirectory')){$ReaderDirectory=Join-Path $PSScriptRoot '../runtime'}
$ReaderDirectory=[IO.Path]::GetFullPath($ReaderDirectory)
. (Join-Path $PSScriptRoot 'Test-Chunk7OrdinaryEvidence.ps1') -RepositoryRoot $RepositoryRoot -ReaderDirectory $ReaderDirectory -FixturesOnly
$private=Join-Path (Get-KmcLabRoot) ('analysis-cache/chunk7-continuation/ordinary-archive-'+[Guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($private)
Add-Type -AssemblyName System.IO.Compression
function New-ArchiveFixture {
    $f=New-OrdinaryFixture
    $lab=Join-Path $private ([Guid]::NewGuid().ToString('N'))
    $f.request.evidenceRoot=Join-Path $lab ('runtime-evidence/'+$f.request.runId)
    $root=Join-Path $lab ('runtime-staging/persistence-'+$f.request.runId+'/Saved Games')
    [void][IO.Directory]::CreateDirectory($root)
    $f.artifact.observations.authority.root=$root;$f.artifact.observations.cleanup.authority.root=$root
    $f.artifact.observations.nativeWrite.path=Join-Path $root 'Auto_1.zks'
    $f|Add-Member NoteProperty header ([pscustomobject]@{Name=$f.artifact.observations.nativeWrite.name;Type='Auto';GameId=$f.request.fixture.working.gameId;GameName=$f.request.fixture.working.gameName;Area=$f.request.fixture.working.area;CompatibilityVersion=1;IsAutoLevelupSave=$true})
    $f|Add-Member NoteProperty metadata ([pscustomobject]@{SchemaVersion=3;CampaignId=$f.request.fixture.working.gameId;AreaId=$f.request.fixture.working.area;Pairs=@();Participations=@();Combat=$null})
    return $f
}
function Write-ArchiveFixture($f,[string]$ExtraEntry,[string]$HeaderJson,[string]$MetadataJson){
    $path=$f.artifact.observations.nativeWrite.path
    if(Test-Path -LiteralPath $path){throw 'Synthetic archive leaf must be unique; tests do not replace existing files.'}
    $stream=$null;$zip=$null
    try{
        $stream=[IO.FileStream]::new($path,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
        $zip=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Create,$false)
        foreach($name in @('header.json','kmc-mounted-state')+$(if($ExtraEntry){@($ExtraEntry)}else{@()})){
            $json=if($name-ceq'header.json'){if($HeaderJson){$HeaderJson}else{ConvertTo-KmcChunk7Canonical $f.header}}else{if($MetadataJson){$MetadataJson}else{ConvertTo-KmcChunk7Canonical $f.metadata}}
            $writer=[IO.StreamWriter]::new($zip.CreateEntry($name).Open(),[Text.UTF8Encoding]::new($false))
            try{$writer.Write($json)}finally{$writer.Dispose()}
        }
    }finally{if($zip){$zip.Dispose()};if($stream){$stream.Dispose()}}
    $f.artifact.observations.nativeWrite.sha256=Get-KmcSha256 $path
    $f.artifact.observations.nativeWrite.length=[long](Get-Item -LiteralPath $path).Length
}
$script:passes=0
Good 'completed native-shaped isolated archive is validated read-only' {
    $f=New-ArchiveFixture;Write-ArchiveFixture $f
    $path=$f.artifact.observations.nativeWrite.path;$before=Get-KmcSha256 $path;$time=(Get-Item -LiteralPath $path).LastWriteTimeUtc.Ticks
    Assert-KmcChunk7OrdinaryNativeArchive $f.request $f.artifact
    if((Get-KmcSha256 $path)-cne$before-or(Get-Item -LiteralPath $path).LastWriteTimeUtc.Ticks-ne$time){throw 'Reader changed synthetic archive bytes or timestamp.'}
}
$mutations=[ordered]@{
 'foreign campaign'={param($f)$f.header.GameId='ffffffff-ffff-4fff-8fff-ffffffffffff'}
 'foreign area'={param($f)$f.header.Area='f'*32}
 'manual save category'={param($f)$f.header.Type='Manual'}
 'non-level-up autosave'={param($f)$f.header.IsAutoLevelupSave=$false}
 'text native autosave flag'={param($f)$f.header.IsAutoLevelupSave='true'}
 'text native header version'={param($f)$f.header.CompatibilityVersion='1'}
 'historical mounted schema'={param($f)$f.metadata.SchemaVersion=2}
 'text mounted schema'={param($f)$f.metadata.SchemaVersion='3'}
 'foreign metadata campaign'={param($f)$f.metadata.CampaignId='ffffffff-ffff-4fff-8fff-ffffffffffff'}
 'invented mounted owner'={param($f)$f.metadata.Pairs=@([pscustomobject]@{RiderId='fake'})}
 'invented pending participation'={param($f)$f.metadata.Participations=@([pscustomobject]@{ActorId='fake'})}
 'invented combat custody'={param($f)$f.metadata.Combat=[pscustomobject]@{Actors=@()}}
}
foreach($item in $mutations.GetEnumerator()){Bad $item.Key {$f=New-ArchiveFixture;& $item.Value $f;Write-ArchiveFixture $f;Assert-KmcChunk7OrdinaryNativeArchive $f.request $f.artifact}}
foreach($entry in @('header.json','kmc-mounted-state')){Bad ('duplicate exact archive member '+$entry) {$f=New-ArchiveFixture;Write-ArchiveFixture $f $entry;Assert-KmcChunk7OrdinaryNativeArchive $f.request $f.artifact}}
Bad 'duplicate JSON header member' {$f=New-ArchiveFixture;$json=(ConvertTo-KmcChunk7Canonical $f.header).Insert(1,'"Type":"Auto",');Write-ArchiveFixture $f -HeaderJson $json;Assert-KmcChunk7OrdinaryNativeArchive $f.request $f.artifact}
Bad 'duplicate JSON metadata member' {$f=New-ArchiveFixture;$json=(ConvertTo-KmcChunk7Canonical $f.metadata).Insert(1,'"SchemaVersion":3,');Write-ArchiveFixture $f -MetadataJson $json;Assert-KmcChunk7OrdinaryNativeArchive $f.request $f.artifact}
Bad 'oversized native metadata' {$f=New-ArchiveFixture;Write-ArchiveFixture $f -MetadataJson (' '*131073);Assert-KmcChunk7OrdinaryNativeArchive $f.request $f.artifact}
Bad 'completed archive hash mismatch' {$f=New-ArchiveFixture;Write-ArchiveFixture $f;$f.artifact.observations.nativeWrite.sha256='0'*64;Assert-KmcChunk7OrdinaryNativeArchive $f.request $f.artifact}
Write-Host ('TOTAL PASS '+$script:passes+' FAIL 0; synthetic archives and read-only isolated filesystem checks only; fixtures='+$private)
