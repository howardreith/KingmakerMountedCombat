[CmdletBinding()]
param()
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Initialize-Chunk7Tests.ps1')
$source=Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7WorldScenarioEngine.cs'
$text=[IO.File]::ReadAllText($source)
$start=$text.IndexOf('        private bool TryCleanup(');$end=$text.IndexOf('        private bool NativeCommandsSettled()', $start)
if($start-lt0-or$end-le$start){throw 'Actual cleanup method absent'}
$output=Join-Path $chunk7AnalysisRoot ('cleanup-json-'+[Guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($output)
$probe=Join-Path $output 'CleanupJson.cs'
$template=[IO.File]::ReadAllText((Join-Path $chunk7TestSource 'WorldCleanupJsonTemplate.cs'))
[IO.File]::WriteAllText($probe,$template.Replace('__TRY_CLEANUP__',$text.Substring($start,$end-$start)))
$json='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed/Newtonsoft.Json.dll'
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$exe=Join-Path $output 'cleanup-json.exe'
&$compiler /nologo /target:exe /langversion:7.3 "/reference:$json" "/out:$exe" $probe
if($LASTEXITCODE-ne0){throw 'Actual cleanup JSON compilation failed.'}
Copy-Item -LiteralPath $json -Destination (Join-Path $output 'Newtonsoft.Json.dll')
&$exe (Split-Path $json)
if($LASTEXITCODE-ne0){throw 'Actual cleanup JSON behavior failed.'}
