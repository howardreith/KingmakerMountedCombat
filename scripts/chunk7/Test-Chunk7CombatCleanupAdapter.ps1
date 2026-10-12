$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Initialize-Chunk7Tests.ps1')
$source=Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7CombatCleanup.cs'
$text=[IO.File]::ReadAllText($source)
$start=$text.IndexOf('        private bool cleanupProven');$end=$text.LastIndexOf(([string][char]10)+'    }')
if($start-lt0-or$end-le$start){throw 'Exact combat cleanup extraction failed.'}
$methods=$text.Substring($start,$end-$start)
$scenario=[IO.File]::ReadAllText((Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7CombatScenario.cs'))
$helperStart=$scenario.IndexOf('        private void Pass(string name)');$helperEnd=$scenario.LastIndexOf(([string][char]10)+'    }')
if($helperStart-lt0-or$helperEnd-le$helperStart){throw 'Exact result helper extraction failed.'}
$methods+=[Environment]::NewLine+$scenario.Substring($helperStart,$helperEnd-$helperStart)
$probe=Join-Path $chunk7AnalysisRoot ('probe-'+[Guid]::NewGuid().ToString('N'));[void][IO.Directory]::CreateDirectory($probe)
$adapter=[IO.File]::ReadAllText((Join-Path $chunk7TestSource 'CombatCleanupAdapterTemplate.cs')).Replace('__METHODS__',$methods)
[IO.File]::WriteAllText((Join-Path $probe 'CombatCleanupAdapter.cs'),$adapter)
[ordered]@{source=$source;sha256=(Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash.ToLowerInvariant();methods=@('BeginCleanup','TryCleanup','ReleaseCleanupObservers','AwaitCleanup','PollCleanup','CompleteEvidence','DrainForHost','Dispose')}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $probe 'combat-cleanup-source.json')
$vs=Join-Path ([Environment]::GetFolderPath('ProgramFilesX86')) 'Microsoft Visual Studio/Installer/vswhere.exe'
$compiler=@(& $vs -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild/**/Bin/Roslyn/csc.exe')|Select-Object -First 1
$exe=Join-Path $probe 'CombatCleanupAdapter.exe'
& $compiler /nologo /target:exe /langversion:7.3 /nowarn:0649 "/out:$exe" (Join-Path $probe 'CombatCleanupAdapter.cs')
if($LASTEXITCODE-ne0){throw 'Actual combat cleanup adapter compilation failed.'}
& $exe
if($LASTEXITCODE-ne0){throw 'Actual combat cleanup regression failed.'}
