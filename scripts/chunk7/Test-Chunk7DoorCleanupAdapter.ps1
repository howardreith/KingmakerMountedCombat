$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Initialize-Chunk7Tests.ps1')
$source=Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7NativeDoorProbe.cs'
$text=[IO.File]::ReadAllText($source)
$start=$text.IndexOf('        internal bool Drain()');$end=$text.IndexOf('        private void RequirePairs()',$start)
if($start-lt0-or$end-le$start){throw 'Actual native door cleanup extraction failed.'}
$method=$text.Substring($start,$end-$start)
# Restoration must not depend on a rider, a restriction-checked interaction or combat state.
foreach($forbidden in @('riders','Interact(','IsInCombat','CanInteract')){if($method.Contains($forbidden)){throw ('Native door cleanup depends on '+$forbidden)}}
$probe=Join-Path $chunk7AnalysisRoot ('door-cleanup-'+[Guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($probe)
$template=[IO.File]::ReadAllText((Join-Path $chunk7TestSource 'DoorCleanupAdapterTemplate.cs'))
$adapter=Join-Path $probe 'DoorCleanupAdapter.cs'
[IO.File]::WriteAllText($adapter,$template.Replace('__METHOD__',$method))
[ordered]@{source=$source;sourceSha256=(Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash.ToLowerInvariant()}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $probe 'inputs.json')
$vs=Join-Path ([Environment]::GetFolderPath('ProgramFilesX86')) 'Microsoft Visual Studio/Installer/vswhere.exe'
$compiler=@(& $vs -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild/**/Bin/Roslyn/csc.exe')|Select-Object -First 1
$exe=Join-Path $probe 'DoorCleanupAdapter.exe'
& $compiler /nologo /target:exe /langversion:7.3 /warnaserror+ "/out:$exe" $adapter
if($LASTEXITCODE-ne0){throw 'Actual native door cleanup adapter compilation failed.'}
& $exe
if($LASTEXITCODE-ne0){throw 'Actual native door cleanup regression failed.'}
