$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Initialize-Chunk7Tests.ps1')
$source=Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7WorldScenarioEngine.cs'
$text=[IO.File]::ReadAllText($source)
$start=$text.IndexOf('        private void BeginCleanup()');$end=$text.IndexOf('        private static int Id',$start)
$dispose=$text.IndexOf('        public void Dispose()',$end);$close=$text.IndexOf(([string][char]10)+'    }',$dispose)
if($start-lt0-or$end-le$start-or$dispose-le$end-or$close-le$dispose){throw 'Exact world cleanup method extraction failed.'}
$methods=$text.Substring($start,$end-$start)+[Environment]::NewLine+$text.Substring($dispose,$close-$dispose)
$probe=Join-Path $chunk7AnalysisRoot ('probe-'+[Guid]::NewGuid().ToString('N'));[void][IO.Directory]::CreateDirectory($probe)
$scope=Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7NativeWorldScope.cs'
$adapter=[IO.File]::ReadAllText((Join-Path $chunk7TestSource 'WorldCleanupAdapterTemplate.cs')).Replace('__METHODS__',$methods)
$sixSource=Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7WorldScenarioEngine.SixPair.cs'
$six=[IO.File]::ReadAllText($sixSource);$s=$six.IndexOf('private void ReleaseSixSamplePins()',[StringComparison]::Ordinal)
if($s-lt0){throw 'Actual six-pair cleanup release absent.'};$open=$six.IndexOf('{',$s);$depth=1;$e=$open+1
while($depth-gt0-and$e-lt$six.Length){if($six[$e]-ceq'{'){$depth++}elseif($six[$e]-ceq'}'){$depth--};$e++}
if($depth-ne0){throw 'Unbalanced six-pair cleanup release.'};$adapter=$adapter.Replace('__SIX_RELEASE__',$six.Substring($s,$e-$s))
[IO.File]::WriteAllText((Join-Path $probe 'WorldCleanupAdapter.cs'),$adapter)
[ordered]@{source=$source;sha256=(Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash.ToLowerInvariant();sixSource=$sixSource;sixSha256=(Get-KmcSha256 $sixSource);methods=@('BeginCleanup','TryCleanup','NativeCommandsSettled','PairCommandsSettled','RetainCleanup','PollCleanup','CompleteCleanupEvidence','DrainForHost','Dispose','ReleaseSixSamplePins')}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $probe 'world-cleanup-source.json')
$vs=Join-Path ([Environment]::GetFolderPath('ProgramFilesX86')) 'Microsoft Visual Studio/Installer/vswhere.exe'
$compiler=@(& $vs -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild/**/Bin/Roslyn/csc.exe')|Select-Object -First 1
$exe=Join-Path $probe 'WorldCleanupAdapter.exe'
& $compiler /nologo /target:exe /langversion:7.3 /nowarn:0649 "/out:$exe" (Join-Path $probe 'WorldCleanupAdapter.cs') $scope
if($LASTEXITCODE-ne0){throw 'Actual world cleanup adapter compilation failed.'}
& $exe
if($LASTEXITCODE-ne0){throw 'Actual world cleanup lifecycle regression failed.'}
