param([string]$RepositoryRoot,[string]$SourceOverlayRoot,[string]$OutputRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('OutputRoot')){$OutputRoot=Join-Path ([IO.Path]::GetFullPath((Join-Path $RepositoryRoot '../..'))) ('analysis-cache/chunk7-tests/matched-pool-'+[Guid]::NewGuid().ToString('N'))}
if(Test-Path -LiteralPath $OutputRoot){throw 'Selected focused output already exists.'}
[void][IO.Directory]::CreateDirectory($OutputRoot)
function Source([string]$Relative){if($SourceOverlayRoot-and(Test-Path -LiteralPath (Join-Path $SourceOverlayRoot $Relative))){return Join-Path $SourceOverlayRoot $Relative};return Join-Path $RepositoryRoot $Relative}
$collector=Source 'src/KingmakerMountedCombat/Diagnostics/Chunk7MatchedPoolCollector.cs'
$identities=Source 'src/KingmakerMountedCombat/Diagnostics/RetainedObjectIdentity.cs'
$test=Source 'tests/Chunk7Diagnostics/MatchedPoolCollectorTests.cs'
$paths=[xml][IO.File]::ReadAllText((Join-Path $RepositoryRoot 'LocalGamePaths.props'))
$game=[string]$paths.Project.PropertyGroup.KingmakerInstallDir
$newtonsoft=Join-Path $game 'Kingmaker_Data/Managed/Newtonsoft.Json.dll'
$compiler='C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe'
$exe=Join-Path $OutputRoot 'MatchedPoolCollectorTests.exe'
$clock=[Diagnostics.Stopwatch]::StartNew()
& $compiler /nologo /target:exe /langversion:7.3 "/r:$newtonsoft" "/out:$exe" $collector $identities $test
if($LASTEXITCODE-ne0){throw 'Actual matched-pool collector compilation failed.'}
[void][Reflection.Assembly]::LoadFrom($newtonsoft)
$assembly=[Reflection.Assembly]::LoadFile($exe)
$serialized=Join-Path $OutputRoot 'actual-collector.json'
$result=$assembly.GetType('MatchedPoolCollectorTests').GetMethod('Main').Invoke($null,@(,[string[]]@($serialized)))
if($result-ne0){throw 'Actual matched-pool collector regression failed.'}
$engine=Source 'src/KingmakerMountedCombat/Diagnostics/Chunk7WorldScenarioEngine.cs';$engineText=[IO.File]::ReadAllText($engine)
$start=$engineText.IndexOf('        private void BeginMatchedPool(');$end=$engineText.IndexOf('        private JObject CaptureMatchedPool(',$start)
$deadline=[regex]::Match($engineText,'Require\(clock\.Elapsed\.TotalSeconds <= 300[\s\S]*?step \+ "\."\);').Value
if($start-lt0-or$end-le$start-or-not$deadline){throw 'Exact native admission extraction failed.'}
$methods=$engineText.Substring($start,$end-$start)
$adapter=[IO.File]::ReadAllText((Source 'tests/Chunk7Diagnostics/MatchedPoolAdmissionTemplate.cs')).Replace('__METHODS__',$methods).Replace('__DEADLINE__',$deadline)
$adapterPath=Join-Path $OutputRoot 'MatchedPoolAdmission.cs';[IO.File]::WriteAllText($adapterPath,$adapter)
$admissionExe=Join-Path $OutputRoot 'MatchedPoolAdmission.exe'
& $compiler /nologo /target:exe /langversion:7.3 /nowarn:0649 "/r:$newtonsoft" "/out:$admissionExe" $collector $identities $adapterPath
if($LASTEXITCODE-ne0){throw 'Actual native admission adapter compilation failed.'}
$admission=[Reflection.Assembly]::LoadFile($admissionExe)
if($admission.GetType('MatchedPoolAdmissionTests').GetMethod('Main').Invoke($null,@())-ne0){throw 'Actual native admission regression failed.'}
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
. (Source 'scripts/runtime/RuntimeArtifactManifestEvidence.ps1')
. (Source 'scripts/runtime/Chunk7MatchedPoolEvidence.ps1')
$fixturePath=Source 'tests/Chunk7Diagnostics/MatchedPoolReaderFixture.ps1'
. $fixturePath
Invoke-MatchedPoolReaderChecks -ActualCollector $serialized -OutputRoot $OutputRoot
[ordered]@{status='PASS';durationSeconds=$clock.Elapsed.TotalSeconds;source=@($collector,$identities,$test,$engine,(Source 'tests/Chunk7Diagnostics/MatchedPoolAdmissionTemplate.cs'),(Source 'scripts/runtime/Chunk7MatchedPoolEvidence.ps1'),$fixturePath)|ForEach-Object{[ordered]@{path=$_;sha256=(Get-FileHash -LiteralPath $_).Hash.ToLowerInvariant()}};nativeProof=$false;performanceAccepted=$false;archiveOperations=$false}|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $OutputRoot 'receipt.json')
