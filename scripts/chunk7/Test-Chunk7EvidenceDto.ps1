[CmdletBinding()]
param([string]$RepositoryRoot,[string]$SourceOverlayRoot,[string]$FieldFixtureScript,[string]$ReaderDirectory,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('SourceOverlayRoot')){$SourceOverlayRoot=$RepositoryRoot}
if(-not$PSBoundParameters.ContainsKey('FieldFixtureScript')){$FieldFixtureScript=Join-Path $RepositoryRoot 'scripts/chunk7/Test-Chunk7FieldEvidence.ps1'}
if(-not$PSBoundParameters.ContainsKey('ReaderDirectory')){$ReaderDirectory=Join-Path $RepositoryRoot 'scripts/runtime'}
if(-not$PSBoundParameters.ContainsKey('OutputDirectory')){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/evidence-dto-'+[Guid]::NewGuid().ToString('N'))}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
. $FieldFixtureScript -RepositoryRoot $RepositoryRoot -ReaderDirectory $ReaderDirectory -FixturesOnly
$fixture=New-FieldFixture
$inputPath=Join-Path $OutputDirectory 'actual-dto-input.json';$outputPath=Join-Path $OutputDirectory 'actual-dto-output.json'
[IO.File]::WriteAllText($inputPath,([ordered]@{ordinary=$fixture.artifact.observations.source.bodySelection;field=$fixture.request.ordinaryField;sourceRunId=$fixture.request.ordinaryField.sourceRunId}|ConvertTo-Json -Depth 30),[Text.UTF8Encoding]::new($false))
$sources=@('src/KingmakerMountedCombat/Diagnostics/Chunk7EvidenceDto.cs','src/KingmakerMountedCombat/Diagnostics/Chunk7OrdinaryAcquisitionSelection.cs','src/KingmakerMountedCombat/Diagnostics/Chunk7OrdinaryFieldSelection.cs','tests/Chunk7Diagnostics/EvidenceDtoTests.cs')|ForEach-Object{
    $candidate=Join-Path $SourceOverlayRoot $_
    if(Test-Path -LiteralPath $candidate){$candidate}else{Join-Path $RepositoryRoot $_}
}
$framework='C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7'
$newtonsoft='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed/Newtonsoft.Json.dll'
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$dll=Join-Path $OutputDirectory 'evidence-dto.dll';$rsp=Join-Path $OutputDirectory 'compile.rsp'
$arguments=@('/nologo','/target:library','/langversion:7.3','/warnaserror+','/optimize+','/deterministic+','/nostdlib+',('/out:'+$dll),('/reference:'+$newtonsoft))
$arguments+=@('mscorlib.dll','System.dll','System.Core.dll')|ForEach-Object{'/reference:'+(Join-Path $framework $_)}
[IO.File]::WriteAllLines($rsp,@(($arguments+$sources)|ForEach-Object{'"'+$_+'"'}),[Text.UTF8Encoding]::new($false))
&$compiler ('@'+$rsp);if($LASTEXITCODE-ne0){throw 'Actual evidence DTO compile failed.'}
[void][Reflection.Assembly]::LoadFrom($newtonsoft);[void][Reflection.Assembly]::LoadFrom($dll)
$dtoPasses=[KingmakerMountedCombat.Tests.EvidenceDtoTests]::Run($inputPath,$outputPath)
$actual=Get-Content -Raw -LiteralPath $outputPath|ConvertFrom-Json
$script:passes=0
Good 'actual new field DTO and projected deployed source pass strict field reader' {
    $f=New-FieldFixture;$f.artifact.observations.selection=$actual.field;$f.artifact.observations.source.bodySelection=$actual.ordinary
    Assert-KmcChunk7FieldEvidence $f.request $f.artifact
}
Bad 'actual old field DTO is rejected by exact request evidence contract' {
    $f=New-FieldFixture;$f.artifact.observations.selection=$actual.deployedField
    Assert-KmcChunk7FieldEvidence $f.request $f.artifact
}
Bad 'deployed source must pass deliberate projection before new evidence' {
    $f=New-FieldFixture;$f.artifact.observations.source.bodySelection=$actual.deployedOrdinary
    Assert-KmcChunk7FieldEvidence $f.request $f.artifact
}
[ordered]@{status='PASS';actualDtoChecks=$dtoPasses;strictReaderChecks=$script:passes;nativeProof=$false;sourceHashes=@($sources|ForEach-Object{@{path=$_;sha256=(Get-FileHash -LiteralPath $_).Hash.ToLowerInvariant()}})}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
Write-Host ('TOTAL PASS '+($dtoPasses+$script:passes)+' FAIL 0; actual serialized DTO and strict reader only')
