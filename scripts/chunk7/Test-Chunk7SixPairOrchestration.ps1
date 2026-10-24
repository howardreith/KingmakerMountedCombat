[CmdletBinding()]
param([string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Initialize-Chunk7Tests.ps1')
if(-not$OutputDirectory){$OutputDirectory=Join-Path $chunk7AnalysisRoot ('six-orchestration-'+[Guid]::NewGuid().ToString('N'))}
$OutputDirectory=Assert-KmcChildPath $OutputDirectory $chunk7AnalysisRoot 'six-pair orchestration checks'
if(Test-Path -LiteralPath $OutputDirectory){throw 'Preserve prior orchestration evidence.'};[void][IO.Directory]::CreateDirectory($OutputDirectory)
$source=Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7WorldScenarioEngine.SixPair.cs'
$identity=Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/RetainedObjectIdentity.cs'
$template=Join-Path $chunk7TestSource 'SixPairOrchestrationTemplate.cs'
$json='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed/Newtonsoft.Json.dll'
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$exe=Join-Path $OutputDirectory 'six-orchestration.exe'
&$compiler /nologo /target:exe /langversion:7.3 /nowarn:0649 "/reference:$json" "/out:$exe" $source $identity $template
if($LASTEXITCODE-ne0){throw 'Actual six-pair partial compilation failed.'}
Copy-Item -LiteralPath $json -Destination (Join-Path $OutputDirectory 'Newtonsoft.Json.dll')
$lines=@(&$exe);$code=$LASTEXITCODE;$lines|Write-Output
[ordered]@{nativeInvoked=$false;archiveRead=$false;exitCode=$code;inputs=@($source,$identity,$template,$json)|ForEach-Object{[ordered]@{path=$_;sha256=(Get-KmcSha256 $_)}};results=$lines;limits='Unchanged production six-pair partial and actual retained identity registry. Native engine, input, resources and sampler are external facades; no native gameplay, performance or preservation qualification.'}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
if($code-ne0){throw 'Actual six-pair orchestration failed.'}
