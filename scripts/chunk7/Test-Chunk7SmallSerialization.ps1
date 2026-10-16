[CmdletBinding()]
param([string]$RepositoryRoot,[string]$SourceOverlayRoot,[string]$EvidenceDtoSource,[string]$FieldDiagnosticDirectory,[string]$ReaderDirectory,[string]$SmallFixtureScript,[string]$FieldFixtureScript,[string]$FieldReaderDirectory,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$SourceOverlayRoot){$SourceOverlayRoot=$RepositoryRoot}
if(-not$EvidenceDtoSource){$EvidenceDtoSource=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics/Chunk7EvidenceDto.cs'}
if(-not$FieldDiagnosticDirectory){$FieldDiagnosticDirectory=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics'}
if(-not$ReaderDirectory){$ReaderDirectory=Join-Path $RepositoryRoot 'scripts/runtime'}
if(-not$SmallFixtureScript){$SmallFixtureScript=Join-Path $PSScriptRoot 'Test-Chunk7SmallEvidence.ps1'}
if(-not$FieldFixtureScript){$FieldFixtureScript=Join-Path $PSScriptRoot 'Test-Chunk7FieldEvidence.ps1'}
if(-not$FieldReaderDirectory){$FieldReaderDirectory=$ReaderDirectory}
if(-not$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/small-serialization-'+[Guid]::NewGuid().ToString('N'))}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
. $SmallFixtureScript -RepositoryRoot $RepositoryRoot -ReaderDirectory $ReaderDirectory -FieldTestSource $FieldFixtureScript -FieldReaderDirectory $FieldReaderDirectory -FixturesOnly
$f=New-SmallFixture;$input=Join-Path $OutputDirectory 'input.json';$output=Join-Path $OutputDirectory 'output.json'
[IO.File]::WriteAllText($input,([ordered]@{small=$f.request.smallCalibration;ordinary=$f.artifact.observations.source.bodySelection;sourceRunId=$f.request.smallCalibration.source.sourceRunId}|ConvertTo-Json -Depth 40),[Text.UTF8Encoding]::new($false))
$sources=@($EvidenceDtoSource,(Join-Path $FieldDiagnosticDirectory 'Chunk7OrdinaryFieldSelection.cs'),(Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics/Chunk7OrdinaryAcquisitionSelection.cs'),(Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/SmallSerializationFacades.cs'),(Join-Path $SourceOverlayRoot 'src/KingmakerMountedCombat/Diagnostics/Chunk7SmallCalibrationSelection.cs'),(Join-Path $SourceOverlayRoot 'tests/Chunk7Diagnostics/SmallEvidenceDtoTests.cs'))
foreach($leaf in @('PoseVector3.cs','MountedRiderPoseProfile.cs','DeclaredSmallRiderBody.cs','MountedRiderProfileCatalog.cs')){$sources+=Join-Path $RepositoryRoot ('src/KingmakerMountedCombat/Domain/'+$leaf)}
$json='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed/Newtonsoft.Json.dll';$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe';$dll=Join-Path $OutputDirectory 'actual.dll'
&$compiler /nologo /target:library /langversion:7.3 /warnaserror+ "/reference:$json" "/out:$dll" $sources
if($LASTEXITCODE-ne0){throw 'Actual Small serialization compile failed.'};[void][Reflection.Assembly]::LoadFrom($json);[void][Reflection.Assembly]::LoadFrom($dll)
$checks=[KingmakerMountedCombat.Tests.SmallEvidenceDtoTests]::Run($input,$output);$actual=Get-Content -Raw -LiteralPath $output|ConvertFrom-Json;$script:passes=0
Good 'actual Small DTO projection and deployed source normalization pass strict Small reader' {$f=New-SmallFixture;$f.artifact.observations.selection=$actual.small;$f.artifact.observations.source.bodySelection=$actual.ordinary;Assert-KmcChunk7SmallEvidence $f.request $f.artifact}
Bad 'actual default Small DTO rejected by new exact request contract' {$f=New-SmallFixture;$f.artifact.observations.selection=$actual.deployedSmall;Assert-KmcChunk7SmallEvidence $f.request $f.artifact}
Bad 'deployed source must be normalized before new evidence' {$f=New-SmallFixture;$f.artifact.observations.source.bodySelection=$actual.deployedOrdinary;Assert-KmcChunk7SmallEvidence $f.request $f.artifact}
@{status='PASS';nativeProof=$false;actualDtoChecks=$checks;strictReaderChecks=$script:passes;sourceHashes=@($sources|ForEach-Object{@{path=$_;sha256=(Get-FileHash -LiteralPath $_).Hash.ToLowerInvariant()}})}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json')
Write-Host ('TOTAL PASS '+($checks+$script:passes)+' FAIL 0; actual serialized Small DTO and strict reader only')
