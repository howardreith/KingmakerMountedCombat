[CmdletBinding()]
param([string]$OutputDirectory)
# Actual-method check of the six-mounted Manual producer and cold consumer stages: the
# unchanged production partial compiled against native boundary facades. Offline only;
# no native Mount, save, restoration, movement or qualification is claimed.
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Initialize-Chunk7Tests.ps1')
if(-not$OutputDirectory){$OutputDirectory=Join-Path $chunk7AnalysisRoot ('six-persistence-'+[Guid]::NewGuid().ToString('N'))}
$OutputDirectory=Assert-KmcChildPath $OutputDirectory $chunk7AnalysisRoot 'six-pair persistence orchestration checks'
if(Test-Path -LiteralPath $OutputDirectory){throw 'Preserve prior six-pair persistence evidence.'};[void][IO.Directory]::CreateDirectory($OutputDirectory)
$source=Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7PersistenceScenario.SixPair.cs'
$template=Join-Path $chunk7TestSource 'SixPairPersistenceTemplate.cs'
$json='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed/Newtonsoft.Json.dll'
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$exe=Join-Path $OutputDirectory 'six-persistence.exe'
&$compiler /nologo /target:exe /langversion:7.3 /nowarn:0649,0169,0414 "/reference:$json" "/out:$exe" $source $template
if($LASTEXITCODE-ne0){throw 'Actual six-pair persistence partial compilation failed.'}
Copy-Item -LiteralPath $json -Destination (Join-Path $OutputDirectory 'Newtonsoft.Json.dll')
$lines=@(&$exe $json);$code=$LASTEXITCODE;$lines|Write-Output
[ordered]@{nativeInvoked=$false;archiveRead=$false;exitCode=$code;inputs=@($source,$template,$json)|ForEach-Object{[ordered]@{path=$_;sha256=(Get-KmcSha256 $_)}};results=$lines
 limits='Unchanged production six-pair persistence partial. Native Mount, hotbar, registry, save, restoration observer and movement are external facades; no native gameplay, save or restoration qualification.'}|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
if($code-ne0){throw 'Actual six-pair persistence orchestration failed.'}
