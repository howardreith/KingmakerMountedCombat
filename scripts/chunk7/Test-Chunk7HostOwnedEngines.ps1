$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Initialize-Chunk7Tests.ps1')
$source=Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/RuntimeAutomationHost.cs'
$text=[IO.File]::ReadAllText($source)
$start=$text.IndexOf('        private bool TrySettleChunk7Custody()');$end=$text.IndexOf('        private bool SettleChunk7World()',$start)
if($start-lt0-or$end-le$start){throw 'Exact host owner adapter extraction failed.'}
$probe=Join-Path $chunk7AnalysisRoot ('probe-'+[Guid]::NewGuid().ToString('N'));[void][IO.Directory]::CreateDirectory($probe)
$worldEnd=$text.IndexOf('        private void DrainChunk7Completion()',$end)
if($worldEnd-le$end){throw 'Exact host world method extraction failed.'}
$adapter=[IO.File]::ReadAllText((Join-Path $chunk7TestSource 'HostOwnedEnginesAdapterTemplate.cs')).Replace('__METHOD__',$text.Substring($start,$end-$start)).Replace('__WORLD_METHOD__',$text.Substring($end,$worldEnd-$end))
[IO.File]::WriteAllText((Join-Path $probe 'HostOwnedEnginesAdapter.cs'),$adapter)
[ordered]@{source=$source;sha256=(Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash.ToLowerInvariant();method='TrySettleChunk7Custody'}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $probe 'host-owner-source.json')
$vs=Join-Path ([Environment]::GetFolderPath('ProgramFilesX86')) 'Microsoft Visual Studio/Installer/vswhere.exe'
$compiler=@(& $vs -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild/**/Bin/Roslyn/csc.exe')|Select-Object -First 1
$exe=Join-Path $probe 'HostOwnedEnginesAdapter.exe'
& $compiler /nologo /target:exe /langversion:7.3 /nowarn:0649 "/out:$exe" (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7HostCompletionGate.cs') (Join-Path $probe 'HostOwnedEnginesAdapter.cs')
if($LASTEXITCODE-ne0){throw 'Actual host owner adapter compilation failed.'}
& $exe
if($LASTEXITCODE-ne0){throw 'Actual host owner lifecycle regression failed.'}
