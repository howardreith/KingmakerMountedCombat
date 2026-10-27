[CmdletBinding()]
param([string]$RepositoryRoot,[string]$DiagnosticSourceDirectory,[string]$TestSource,[string]$EvidenceDtoSource,[string]$FixtureScript,[string]$FixtureDirectory,[string]$ReaderDirectory,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('DiagnosticSourceDirectory')){$DiagnosticSourceDirectory=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics'}
if(-not$PSBoundParameters.ContainsKey('TestSource')){$TestSource=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/ManualDerivedDtoTests.cs'}
if(-not$PSBoundParameters.ContainsKey('EvidenceDtoSource')){$EvidenceDtoSource=Join-Path $DiagnosticSourceDirectory 'Chunk7EvidenceDto.cs'}
if(-not$PSBoundParameters.ContainsKey('FixtureScript')){$FixtureScript=Join-Path $RepositoryRoot 'scripts/chunk7/Test-Chunk7ManualDerivedEvidence.ps1'}
if(-not$PSBoundParameters.ContainsKey('ReaderDirectory')){$ReaderDirectory=Join-Path $RepositoryRoot 'scripts/runtime'}
if(-not$PSBoundParameters.ContainsKey('OutputDirectory')){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/manual-derived-dto-'+[Guid]::NewGuid().ToString('N'))}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
[xml]$project=Get-Content -Raw -LiteralPath (Join-Path $RepositoryRoot 'tests/KingmakerMountedCombat.Tests.csproj')
$sources=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach($node in $project.SelectNodes("//*[local-name()='Compile']")){
    $path=[IO.Path]::GetFullPath((Join-Path (Join-Path $RepositoryRoot 'tests') $node.Include));$leaf=Split-Path -Leaf $path
    if($leaf-cin@('RuntimeProtocol.cs','PersistenceSaveAuthorization.cs','Chunk7OrdinaryAcquisitionSelection.cs','Chunk7OrdinaryFieldSelection.cs','Chunk7SmallCalibrationSelection.cs','Chunk7ProgressionSelection.cs','Chunk7NativeBootstrapSelection.cs','Chunk7BootstrapInventorySelection.cs','Chunk7ManualSeedSelection.cs','Chunk7ManualDerivedSelection.cs','Chunk7AcquiredManualSelection.cs','Chunk7AssistedFixtureSelection.cs','Chunk7AssistedColdInventorySelection.cs','Chunk7CumulativeAcquisitionSelection.cs','Chunk7OwnedPairsSelection.cs','PersistenceSaveAuthorization.AssistedFixture.cs','PersistenceSaveAuthorization.AssistedColdInventory.cs','PersistenceSaveAuthorization.OwnedPairs.cs')){
        $override=Join-Path $DiagnosticSourceDirectory $leaf;if(Test-Path -LiteralPath $override){$path=$override}
    };[void]$sources.Add($path)
}
foreach($leaf in @('Chunk7OrdinaryAcquisitionSelection.cs','Chunk7OrdinaryFieldSelection.cs','Chunk7SmallCalibrationSelection.cs','Chunk7ProgressionSelection.cs','Chunk7NativeBootstrapSelection.cs','Chunk7BootstrapInventorySelection.cs','Chunk7ManualSeedSelection.cs','Chunk7ManualDerivedSelection.cs','Chunk7AcquiredManualSelection.cs','Chunk7AssistedFixtureSelection.cs','Chunk7AssistedColdInventorySelection.cs','Chunk7CumulativeAcquisitionSelection.cs','Chunk7OwnedPairsSelection.cs','PersistenceSaveAuthorization.AssistedFixture.cs','PersistenceSaveAuthorization.AssistedColdInventory.cs','PersistenceSaveAuthorization.OwnedPairs.cs')){
    $path=Join-Path $DiagnosticSourceDirectory $leaf;if(-not(Test-Path -LiteralPath $path)){$path=Join-Path $RepositoryRoot ('src/KingmakerMountedCombat/Diagnostics/'+$leaf)};[void]$sources.Add($path)
}
[void]$sources.Add($TestSource);[void]$sources.Add($EvidenceDtoSource)
$framework='C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7';$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$newtonsoft='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed/Newtonsoft.Json.dll';$dll=Join-Path $OutputDirectory 'manual-derived-dto.dll';$rsp=Join-Path $OutputDirectory 'compile.rsp'
$arguments=@('/nologo','/target:library','/langversion:7.3','/warnaserror+','/optimize+','/deterministic+','/nostdlib+',('/out:'+$dll),('/reference:'+$newtonsoft))
$arguments+=@('mscorlib.dll','System.dll','System.Core.dll')|ForEach-Object{'/reference:'+(Join-Path $framework $_)}
[IO.File]::WriteAllLines($rsp,@(($arguments+@($sources))|ForEach-Object{'"'+$_+'"'}),[Text.UTF8Encoding]::new($false))
&$compiler ('@'+$rsp);if($LASTEXITCODE-ne0){throw 'Actual progression DTO compilation failed.'}
[void][Reflection.Assembly]::LoadFrom($newtonsoft);[void][Reflection.Assembly]::LoadFrom($dll)
if(-not$FixtureDirectory){$FixtureDirectory=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics'}
. $FixtureScript -RepositoryRoot $RepositoryRoot -ReaderDirectory $ReaderDirectory -FixtureDirectory $FixtureDirectory -OutputDirectory $OutputDirectory -FixturesOnly
$script:passes=0
function Good([string]$Name,[scriptblock]$Action){&$Action;$script:passes++;Write-Host ('PASS '+$Name)}
function Bad([string]$Name,[scriptblock]$Action){$bad=$false;try{&$Action}catch{$bad=$true};if(-not$bad){throw ('Accepted '+$Name)};$script:passes++;Write-Host ('PASS refuses '+$Name)}
foreach($paid in @($false,$true)){
 $f=New-ManualDerivedFixture $paid;$raw=[KingmakerMountedCombat.Tests.ManualDerivedDtoTests]::Project(($f.request|ConvertTo-Json -Depth 60));$actual=$raw|ConvertFrom-Json
 [IO.File]::WriteAllText((Join-Path $OutputDirectory ($f.request.manualDerived.case+'.json')),$raw,[Text.UTF8Encoding]::new($false))
 Good 'actual nested request projection preserves exact types/casing' {if((ConvertTo-KmcChunk7Canonical $actual.selection)-cne(ConvertTo-KmcChunk7Canonical $f.request.manualDerived)){throw 'Request evidence projection differs.'}}
 Good 'actual selection projection passes complete strict reader' {$f.artifact.observations.selection=$actual.selection;Check-ManualDerived $f}
 Bad 'default PascalCase selection is refused' {$g=New-ManualDerivedFixture $paid;$g.artifact.observations.selection=$actual.defaultSelection;Check-ManualDerived $g}
 Bad 'default PascalCase nested readiness is refused' {$g=New-ManualDerivedFixture $paid;$g.artifact.observations.selection.readiness=$actual.defaultReadiness;Check-ManualDerived $g}
}
@{schemaVersion=1;pass=$script:passes;fail=0;nativeExecution=$false;archiveAccess=$false;serializer='exact-installed-Newtonsoft';sourceHashes=@($sources|ForEach-Object{@{path=$_;sha256=(Get-FileHash -LiteralPath $_).Hash.ToLowerInvariant()}})}|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
Write-Host ('TOTAL PASS='+$script:passes+' FAIL=0')
