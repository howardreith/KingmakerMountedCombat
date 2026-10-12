[CmdletBinding()]
param([string]$RepositoryRoot,[string]$DiagnosticSourceDirectory,[string]$TestSource,[string]$EvidenceDtoSource,[string]$FixtureScript,[string]$ReaderDirectory,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('DiagnosticSourceDirectory')){$DiagnosticSourceDirectory=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics'}
if(-not$PSBoundParameters.ContainsKey('TestSource')){$TestSource=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/ProgressionDtoTests.cs'}
if(-not$PSBoundParameters.ContainsKey('EvidenceDtoSource')){$EvidenceDtoSource=Join-Path $DiagnosticSourceDirectory 'Chunk7EvidenceDto.cs'}
if(-not$PSBoundParameters.ContainsKey('FixtureScript')){$FixtureScript=Join-Path $RepositoryRoot 'scripts/chunk7/Test-Chunk7ProgressionEvidence.ps1'}
if(-not$PSBoundParameters.ContainsKey('ReaderDirectory')){$ReaderDirectory=Join-Path $RepositoryRoot 'scripts/runtime'}
if(-not$PSBoundParameters.ContainsKey('OutputDirectory')){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/progression-dto-'+[Guid]::NewGuid().ToString('N'))}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
[xml]$project=Get-Content -Raw -LiteralPath (Join-Path $RepositoryRoot 'tests/KingmakerMountedCombat.Tests.csproj')
$sources=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach($node in $project.SelectNodes("//*[local-name()='Compile']")){
    $path=[IO.Path]::GetFullPath((Join-Path (Join-Path $RepositoryRoot 'tests') $node.Include));$leaf=Split-Path -Leaf $path
    if($leaf-cin@('RuntimeProtocol.cs','PersistenceSaveAuthorization.cs','Chunk7OrdinaryAcquisitionSelection.cs','Chunk7OrdinaryFieldSelection.cs','Chunk7SmallCalibrationSelection.cs','Chunk7ProgressionSelection.cs','Chunk7NativeBootstrapSelection.cs')){
        $override=Join-Path $DiagnosticSourceDirectory $leaf;if(Test-Path -LiteralPath $override){$path=$override}
    };[void]$sources.Add($path)
}
foreach($leaf in @('Chunk7OrdinaryAcquisitionSelection.cs','Chunk7OrdinaryFieldSelection.cs','Chunk7SmallCalibrationSelection.cs','Chunk7ProgressionSelection.cs','Chunk7NativeBootstrapSelection.cs')){
    $path=Join-Path $DiagnosticSourceDirectory $leaf;if(-not(Test-Path -LiteralPath $path)){$path=Join-Path $RepositoryRoot ('src/KingmakerMountedCombat/Diagnostics/'+$leaf)};[void]$sources.Add($path)
}
[void]$sources.Add($TestSource);[void]$sources.Add($EvidenceDtoSource)
$framework='C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7';$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$newtonsoft='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed/Newtonsoft.Json.dll';$dll=Join-Path $OutputDirectory 'progression-dto.dll';$rsp=Join-Path $OutputDirectory 'compile.rsp'
$arguments=@('/nologo','/target:library','/langversion:7.3','/warnaserror+','/optimize+','/deterministic+','/nostdlib+',('/out:'+$dll),('/reference:'+$newtonsoft))
$arguments+=@('mscorlib.dll','System.dll','System.Core.dll')|ForEach-Object{'/reference:'+(Join-Path $framework $_)}
[IO.File]::WriteAllLines($rsp,@(($arguments+@($sources))|ForEach-Object{'"'+$_+'"'}),[Text.UTF8Encoding]::new($false))
&$compiler ('@'+$rsp);if($LASTEXITCODE-ne0){throw 'Actual progression DTO compilation failed.'}
[void][Reflection.Assembly]::LoadFrom($newtonsoft);[void][Reflection.Assembly]::LoadFrom($dll)
. $FixtureScript -RepositoryRoot $RepositoryRoot -ReaderDirectory $ReaderDirectory -FixturesOnly
$script:passes=0
foreach($case in Get-KmcChunk7ProgressionCases){
    $f=New-ProgressionFixture $case;$raw=[KingmakerMountedCombat.Tests.ProgressionDtoTests]::Project(($f.request|ConvertTo-Json -Depth 60));$actual=$raw|ConvertFrom-Json
    [IO.File]::WriteAllText((Join-Path $OutputDirectory ($case+'.json')),$raw,[Text.UTF8Encoding]::new($false))
    Good ($case+' actual DTO projection matches exact request') {if((ConvertTo-KmcChunk7Canonical $actual.selection)-cne(ConvertTo-KmcChunk7Canonical $f.request.progression)-or(ConvertTo-KmcChunk7Canonical $actual.descriptor)-cne(ConvertTo-KmcChunk7Canonical $f.request.persistenceLoad)){throw 'Actual DTO evidence shape differs.'}}
    Good ($case+' actual DTO evidence passes strict reader') {$f.artifact.observations.selection=$actual.selection;$f.artifact.observations.loadedDescriptor=$actual.descriptor;Assert-KmcChunk7ProgressionEvidence $f.request $f.artifact}
    Bad ($case+' default selection serialization cannot pass') {$g=New-ProgressionFixture $case;$g.artifact.observations.selection=$actual.defaultSelection;Assert-KmcChunk7ProgressionEvidence $g.request $g.artifact}
    Bad ($case+' default source descriptor serialization cannot pass') {$g=New-ProgressionFixture $case;$g.artifact.observations.loadedDescriptor=$actual.defaultDescriptor;Assert-KmcChunk7ProgressionEvidence $g.request $g.artifact}
}
[ordered]@{status='PASS';passes=$script:passes;failures=0;nativeProof=$false;sourceHashes=@($sources|ForEach-Object{@{path=$_;sha256=(Get-FileHash -LiteralPath $_).Hash.ToLowerInvariant()}})}|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
Write-Host ('TOTAL PASS '+$script:passes+' FAIL 0; actual progression DTO serialization and strict reader only')
