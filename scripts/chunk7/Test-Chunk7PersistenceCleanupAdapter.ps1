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
$scopeSource=Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7PersistenceScenario.Scope.cs'
$scopeText=[IO.File]::ReadAllText($scopeSource);$scopeStart=$scopeText.IndexOf('        private Chunk7NativeWorldScope');$scopeEnd=$scopeText.LastIndexOf(([string][char]10)+'    }')
if($scopeStart-lt0-or$scopeEnd-le$scopeStart){throw 'Exact persistence scope extraction failed.'}
$scopeMethods=$scopeText.Substring($scopeStart,$scopeEnd-$scopeStart)
function Extract-ScopeMethod([string]$Text,[string]$Signature){$a=$Text.IndexOf($Signature);if($a-lt0){throw ('Missing exact scope method '+$Signature)};$o=$Text.IndexOf('{',$a);$d=1;$e=$o+1;while($d-gt0-and$e-lt$Text.Length){if($Text[$e]-eq'{'){$d++};if($Text[$e]-eq'}'){$d--};$e++};if($d-ne0){throw 'Unbalanced exact method'};return $Text.Substring($a,$e-$a)}
$areaText=[IO.File]::ReadAllText((Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7PersistenceScenario.Area.cs'))
$scopeMethods+=[Environment]::NewLine+(Extract-ScopeMethod $areaText 'private void AdoptDeclaredAreaWorld()')
$scopeMethods+=[Environment]::NewLine+(Extract-ScopeMethod $text 'private void RequestSave()')
$scopeMethods+=[Environment]::NewLine+(Extract-ScopeMethod $text 'internal bool ReapplyModeAfterNativeRefresh()')
$writerSource=Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/RuntimeLoaderInventory.cs'
$writerText=[IO.File]::ReadAllText($writerSource);$writerStart=$writerText.IndexOf('        internal static void WriteArtifact(');$writerEnd=$writerText.LastIndexOf(([string][char]10)+'    }')
if($writerStart-lt0-or$writerEnd-le$writerStart){throw 'Actual immutable writer extraction failed.'}
$writer=$writerText.Substring($writerStart,$writerEnd-$writerStart)
$probe=Join-Path $chunk7AnalysisRoot ('probe-'+[Guid]::NewGuid().ToString('N'));[void][IO.Directory]::CreateDirectory($probe)
$adapter=[IO.File]::ReadAllText((Join-Path $chunk7TestSource 'PersistenceCleanupAdapterTemplate.cs')).Replace('__METHODS__',$methods).Replace('__WRITER__',$writer).Replace('__SCOPE_METHODS__',$scopeMethods)
[IO.File]::WriteAllText((Join-Path $probe 'PersistenceCleanupAdapter.cs'),$adapter)
[ordered]@{source=$source;sha256=(Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash.ToLowerInvariant();writerSource=$writerSource;writerSha256=(Get-FileHash -LiteralPath $writerSource -Algorithm SHA256).Hash.ToLowerInvariant();methods=@('BeginCleanup','TryCleanup','RetainCleanup','PerformCleanup','PollCleanup','DrainForHost','Dispose')}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $probe 'persistence-cleanup-source.json')
$vs=Join-Path ([Environment]::GetFolderPath('ProgramFilesX86')) 'Microsoft Visual Studio/Installer/vswhere.exe'
$compiler=@(& $vs -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild/**/Bin/Roslyn/csc.exe')|Select-Object -First 1
$exe=Join-Path $probe 'PersistenceCleanupAdapter.exe'
& $compiler /nologo /target:exe /langversion:7.3 /nowarn:0649 /reference:Microsoft.CSharp.dll "/out:$exe" (Join-Path $probe 'PersistenceCleanupAdapter.cs') (Join-Path $repo 'src/KingmakerMountedCombat/BuildIdentity.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/RuntimeProtocol.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7NativeBootstrapSelection.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7BootstrapInventorySelection.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7ManualSeedSelection.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7NativeWorldScope.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7OrdinaryAcquisitionSelection.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7OrdinaryFieldSelection.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7SmallCalibrationSelection.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7ProgressionSelection.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Domain/PoseVector3.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Domain/MountedRiderPoseProfile.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Domain/DeclaredSmallRiderBody.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Domain/MountedRiderProfileCatalog.cs')
if($LASTEXITCODE-ne0){throw 'Actual persistence cleanup adapter compilation failed.'}
$artifactRoot=Join-Path ([IO.Path]::GetFullPath((Join-Path $repo '../..'))) ('analysis-cache/c7w-'+[Guid]::NewGuid().ToString('N'))
& $exe $artifactRoot
if($LASTEXITCODE-ne0){throw 'Actual persistence cleanup lifecycle regression failed.'}
