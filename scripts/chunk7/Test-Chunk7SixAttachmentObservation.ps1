[CmdletBinding()]
param()
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Initialize-Chunk7Tests.ps1')
$output=Join-Path $chunk7AnalysisRoot ('six-attachment-'+[Guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($output)
$json='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed/Newtonsoft.Json.dll'
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$source=Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/SixPairAttachmentObservation.cs'
$logger=Join-Path $repo 'src/KingmakerMountedCombat/Logging/IModLogger.cs'
$tests=Join-Path $chunk7TestSource 'SixAttachmentObservationTests.cs'
$exe=Join-Path $output 'six-attachment.exe'
&$compiler /nologo /target:exe /langversion:7.3 "/reference:$json" "/out:$exe" $source $logger $tests
if($LASTEXITCODE-ne0){throw 'Actual attachment observer compilation failed.'}
Copy-Item -LiteralPath $json -Destination (Join-Path $output 'Newtonsoft.Json.dll')
&$exe (Split-Path $json)
if($LASTEXITCODE-ne0){throw 'Actual attachment observer behavior failed.'}
