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
$adapter=[IO.File]::ReadAllText((Join-Path $chunk7TestSource 'WorldCleanupAdapterTemplate.cs')).Replace('__METHODS__',$methods)
[IO.File]::WriteAllText((Join-Path $probe 'WorldCleanupAdapter.cs'),$adapter)
[ordered]@{source=$source;sha256=(Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash.ToLowerInvariant();methods=@('BeginCleanup','TryCleanup','NativeCommandsSettled','PairCommandsSettled','RetainCleanup','PollCleanup','CompleteCleanupEvidence','DrainForHost','Dispose')}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $probe 'world-cleanup-source.json')
$vs=Join-Path ([Environment]::GetFolderPath('ProgramFilesX86')) 'Microsoft Visual Studio/Installer/vswhere.exe'
$compiler=@(& $vs -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild/**/Bin/Roslyn/csc.exe')|Select-Object -First 1
$exe=Join-Path $probe 'WorldCleanupAdapter.exe'
& $compiler /nologo /target:exe /langversion:7.3 /nowarn:0649 "/out:$exe" (Join-Path $probe 'WorldCleanupAdapter.cs')
if($LASTEXITCODE-ne0){throw 'Actual world cleanup adapter compilation failed.'}
& $exe
if($LASTEXITCODE-ne0){throw 'Actual world cleanup lifecycle regression failed.'}
