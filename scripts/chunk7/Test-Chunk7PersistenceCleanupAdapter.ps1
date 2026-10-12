$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Initialize-Chunk7Tests.ps1')
$source=Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7PersistenceScenario.cs'
$text=[IO.File]::ReadAllText($source)
$start=$text.IndexOf('        private void BeginCleanup()');$end=$text.IndexOf('        private static JObject Point',$start)
$dispose=$text.IndexOf('        public void Dispose()',$end);$close=$text.IndexOf(([string][char]10)+'    }',$dispose)
if($start-lt0-or$end-le$start-or$dispose-le$end-or$close-le$dispose){throw 'Exact persistence cleanup method extraction failed.'}
$rows=$text.IndexOf('        private void Pass(',$end)
$methods=$text.Substring($start,$end-$start)+[Environment]::NewLine+$text.Substring($rows,$dispose-$rows)+[Environment]::NewLine+$text.Substring($dispose,$close-$dispose)
$writerSource=Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/RuntimeLoaderInventory.cs'
$writerText=[IO.File]::ReadAllText($writerSource);$writerStart=$writerText.IndexOf('        internal static void WriteArtifact(');$writerEnd=$writerText.LastIndexOf(([string][char]10)+'    }')
if($writerStart-lt0-or$writerEnd-le$writerStart){throw 'Actual immutable writer extraction failed.'}
$writer=$writerText.Substring($writerStart,$writerEnd-$writerStart)
$probe=Join-Path $chunk7AnalysisRoot ('probe-'+[Guid]::NewGuid().ToString('N'));[void][IO.Directory]::CreateDirectory($probe)
$adapter=[IO.File]::ReadAllText((Join-Path $chunk7TestSource 'PersistenceCleanupAdapterTemplate.cs')).Replace('__METHODS__',$methods).Replace('__WRITER__',$writer)
[IO.File]::WriteAllText((Join-Path $probe 'PersistenceCleanupAdapter.cs'),$adapter)
[ordered]@{source=$source;sha256=(Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash.ToLowerInvariant();writerSource=$writerSource;writerSha256=(Get-FileHash -LiteralPath $writerSource -Algorithm SHA256).Hash.ToLowerInvariant();methods=@('BeginCleanup','TryCleanup','RetainCleanup','PerformCleanup','PollCleanup','DrainForHost','Dispose')}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $probe 'persistence-cleanup-source.json')
$vs=Join-Path ([Environment]::GetFolderPath('ProgramFilesX86')) 'Microsoft Visual Studio/Installer/vswhere.exe'
$compiler=@(& $vs -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild/**/Bin/Roslyn/csc.exe')|Select-Object -First 1
$exe=Join-Path $probe 'PersistenceCleanupAdapter.exe'
& $compiler /nologo /target:exe /langversion:7.3 /nowarn:0649 /reference:Microsoft.CSharp.dll "/out:$exe" (Join-Path $probe 'PersistenceCleanupAdapter.cs') (Join-Path $repo 'src/KingmakerMountedCombat/BuildIdentity.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/RuntimeProtocol.cs')
if($LASTEXITCODE-ne0){throw 'Actual persistence cleanup adapter compilation failed.'}
$artifactRoot=Join-Path ([IO.Path]::GetFullPath((Join-Path $repo '../..'))) ('analysis-cache/c7w-'+[Guid]::NewGuid().ToString('N'))
& $exe $artifactRoot
if($LASTEXITCODE-ne0){throw 'Actual persistence cleanup lifecycle regression failed.'}
