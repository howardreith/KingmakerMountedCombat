[CmdletBinding()]
param([string]$RepositoryRoot,[string]$ReaderDirectory,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('ReaderDirectory')){$ReaderDirectory=Join-Path $PSScriptRoot '../runtime'}
if(-not$PSBoundParameters.ContainsKey('OutputDirectory')){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/progression-archive-'+[Guid]::NewGuid().ToString('N'))}
$archiveOutput=[IO.Path]::GetFullPath($OutputDirectory)
if(-not$archiveOutput.StartsWith('C:\Dev\KingmakerMountedCombatLab\analysis-cache\',[StringComparison]::OrdinalIgnoreCase)){throw 'Synthetic progression archives must remain under analysis-cache.'}
[void][IO.Directory]::CreateDirectory($archiveOutput)
. (Join-Path $PSScriptRoot 'Test-Chunk7ProgressionEvidence.ps1') -RepositoryRoot $RepositoryRoot -ReaderDirectory $ReaderDirectory -FixturesOnly
Add-Type -AssemblyName System.IO.Compression
function New-ProgressionArchive([string]$Case='horse-native-level',[string]$Mutation=''){
    $f=New-ProgressionFixture $Case;$lab=Join-Path $archiveOutput ([Guid]::NewGuid().ToString('N'));$f.request.evidenceRoot=Join-Path $lab ('runtime-evidence/'+$f.request.runId)
    $root=Join-Path $lab ('runtime-staging/persistence-'+$f.request.runId+'/Saved Games');[void][IO.Directory]::CreateDirectory($root)
    $o=$f.artifact.observations;$o.authorityBefore.root=$root;$o.cleanup.authority.root=$root
    $w=$o.session.nativeWrite;if($null-eq$w){return $f};$w.path=Join-Path $root $w.leaf
    if($null-ne$o.session.PSObject.Properties['manualSave']-and$null-ne$o.session.manualSave){foreach($event in $o.session.manualSave.events){if($null-ne$event.path){$event.path=$w.path}}}
    $header=[pscustomobject]@{Name=$w.name;Type=$w.type;GameId=$w.gameId;GameName=$w.gameName;Area=$w.area;CompatibilityVersion=1;IsAutoLevelupSave=$w.nativeAutoLevelup}
    switch($Mutation){'header-type-array'{$header.Type=@('Auto')}'header-id-array'{$header.GameId=@($header.GameId)}'header-name-array'{$header.Name=@($header.Name)}'header-false-auto'{$header.IsAutoLevelupSave=$false}'header-text-compatibility'{$header.CompatibilityVersion='1'}}
    $metadata=Copy-O $w.archive;if($Mutation-ceq'changed-mounted-owner'){$metadata.Pairs[0].Generation++}
    $headerText=ConvertTo-KmcChunk7Canonical $header;$metadataText=ConvertTo-KmcChunk7Canonical $metadata
    if($Mutation-ceq'duplicate-header-key'){$headerText=$headerText.Replace('"Type":"Auto"','"Type":"Auto","Type":"Auto"')}
    if($Mutation-ceq'oversized-metadata'){$metadataText+=' '*(128KB)}
    $entries=@([pscustomobject]@{name='header.json';text=$headerText},[pscustomobject]@{name='kmc-mounted-state';text=$metadataText})
    if($Mutation-ceq'duplicate-header'){$entries+=@($entries[0])};if($Mutation-ceq'duplicate-metadata'){$entries+=@($entries[1])}
    $stream=[IO.FileStream]::new($w.path,[IO.FileMode]::CreateNew);$zip=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Create,$false)
    try{foreach($entry in $entries){$writer=[IO.StreamWriter]::new($zip.CreateEntry($entry.name).Open(),[Text.UTF8Encoding]::new($false));try{$writer.Write($entry.text)}finally{$writer.Dispose()}}}finally{$zip.Dispose();$stream.Dispose()}
    $w.sha256=Get-KmcSha256 $w.path;$w.length=[long](Get-Item -LiteralPath $w.path).Length
    if($Mutation-ceq'changed-final-bytes'){[IO.File]::AppendAllText($w.path,'changed')};return $f
}
$script:passes=0
foreach($case in @(Get-KmcChunk7ProgressionCases|Where-Object{-not(Test-KmcChunk7PaidProgressionCase $_)})){Good ('closed native output category '+$case){$f=New-ProgressionArchive $case;Assert-KmcChunk7ProgressionNativeArchive $f.request $f.artifact}}
foreach($mutation in @('header-type-array','header-id-array','header-name-array','header-false-auto','header-text-compatibility','changed-mounted-owner','duplicate-header-key','duplicate-header','duplicate-metadata','oversized-metadata','changed-final-bytes')){
    Bad ('bounded native metadata '+$mutation){$f=New-ProgressionArchive 'horse-native-level' $mutation;Assert-KmcChunk7ProgressionNativeArchive $f.request $f.artifact}
}
Write-Host ('TOTAL PASS '+$script:passes+' FAIL 0; generated synthetic metadata only, no real native archive access')
