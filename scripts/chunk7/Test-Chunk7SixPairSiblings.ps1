[CmdletBinding()]
param([string]$OutputDirectory)
# Actual-code check of the read-only six-pair sibling-order observer against a
# minimal native hierarchy facade. Offline evidence only; no native qualification.
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Initialize-Chunk7Tests.ps1')
if(-not$OutputDirectory){$OutputDirectory=Join-Path $chunk7AnalysisRoot ('six-siblings-'+[Guid]::NewGuid().ToString('N'))}
$OutputDirectory=Assert-KmcChildPath $OutputDirectory $chunk7AnalysisRoot 'six-pair sibling-order checks'
if(Test-Path -LiteralPath $OutputDirectory){throw 'Preserve prior six-pair sibling-order evidence.'};[void][IO.Directory]::CreateDirectory($OutputDirectory)
$source=Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7WorldScenarioEngine.SixPairSiblings.cs'
$template=Join-Path $chunk7TestSource 'SixPairSiblingsTemplate.cs'
$json='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed/Newtonsoft.Json.dll'
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$exe=Join-Path $OutputDirectory 'six-siblings.exe'
&$compiler /nologo /target:exe /langversion:7.3 /warn:0 "/reference:$json" "/out:$exe" $source $template
if($LASTEXITCODE-ne0){throw 'Actual six-pair sibling-order partial compilation failed.'}
Copy-Item -LiteralPath $json -Destination (Join-Path $OutputDirectory 'Newtonsoft.Json.dll')
$lines=@(&$exe);$code=$LASTEXITCODE;$lines|Write-Output
[ordered]@{nativeInvoked=$false;exitCode=$code;inputs=@($source,$template,$json)|ForEach-Object{[ordered]@{path=$_;sha256=(Get-KmcSha256 $_)}};results=$lines;limits='Unchanged sibling-order partial with a facade hierarchy; no native hierarchy, timing or qualification.'}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
if($code-ne0){throw 'Actual six-pair sibling-order observer regressed.'}
