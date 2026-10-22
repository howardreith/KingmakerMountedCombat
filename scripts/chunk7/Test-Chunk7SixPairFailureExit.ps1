[CmdletBinding()]
param([string]$OutputDirectory)
# Actual-method check of the unattended retained six-pair terminal FAIL exit: the
# unchanged partial, completion gate and host drain/writer/argument/hash methods,
# launched with genuine runtime command-line bindings. Offline evidence only.
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Initialize-Chunk7Tests.ps1')
if(-not$OutputDirectory){$OutputDirectory=Join-Path $chunk7AnalysisRoot ('six-failure-exit-'+[Guid]::NewGuid().ToString('N'))}
$OutputDirectory=Assert-KmcChildPath $OutputDirectory $chunk7AnalysisRoot 'six-pair failure exit checks'
if(Test-Path -LiteralPath $OutputDirectory){throw 'Preserve prior six-pair failure exit evidence.'};[void][IO.Directory]::CreateDirectory($OutputDirectory)
function Extract([string]$Text,[string]$Signature){$start=$Text.IndexOf($Signature,[StringComparison]::Ordinal);if($start-lt0){throw ('Actual host member absent: '+$Signature)};if($Text.IndexOf($Signature,$start+1,[StringComparison]::Ordinal)-ge0){throw ('Actual host member is ambiguous: '+$Signature)};$brace=$Text.IndexOf('{',$start);$depth=0;for($i=$brace;$i-lt$Text.Length;$i++){if($Text[$i]-eq'{'){$depth++}elseif($Text[$i]-eq'}'){$depth--;if($depth-eq0){return $Text.Substring($start,$i-$start+1)}}};throw 'Unbalanced actual host member.'}
$hostPath=Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/RuntimeAutomationHost.cs';$hostText=[IO.File]::ReadAllText($hostPath)
$partial=Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/RuntimeAutomationHost.SixPairFailure.cs'
$gate=Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7HostCompletionGate.cs'
$template=Join-Path $chunk7TestSource 'SixPairFailureExitTemplate.cs'
$members=[ordered]@{
 'DRAIN'=(Extract $hostText 'private void DrainChunk7Completion()')
 'WRITE_JSON'=(Extract $hostText 'private static void WriteJsonAtomic(string path, object value)')
 'FIND_ARGUMENT'=(Extract $hostText 'private static string FindSingleArgument(string[] arguments, string name)')
 'SHA_PATH'=(Extract $hostText 'private static string ComputeSha256(string path)')
}
$text=[IO.File]::ReadAllText($template);foreach($entry in $members.GetEnumerator()){$marker='/* '+$entry.Key+' */';if(-not$text.Contains($marker)){throw ('Template lacks '+$marker)};$text=$text.Replace($marker,$entry.Value)}
$adapter=Join-Path $OutputDirectory 'SixPairFailureExit.cs';[IO.File]::WriteAllText($adapter,$text)
$json='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed/Newtonsoft.Json.dll'
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$exe=Join-Path $OutputDirectory 'SixPairFailureExit.exe'
&$compiler /nologo /target:exe /langversion:7.3 /warn:0 "/reference:$json" "/out:$exe" $partial $gate $adapter
if($LASTEXITCODE-ne0){throw 'Actual six-pair failure exit compilation failed.'}
Copy-Item -LiteralPath $json -Destination (Join-Path $OutputDirectory 'Newtonsoft.Json.dll')
$evidence=Join-Path $OutputDirectory 'evidence';[void][IO.Directory]::CreateDirectory($evidence)
$request=Join-Path $evidence 'runtime-request.json';[IO.File]::WriteAllText($request,'{"runId":"b188-exit","scenario":"chunk7-owned-two-pair-campaign"}')
$token=[Guid]::NewGuid().ToString('N');$hash=(Get-KmcSha256 $request)
$lines=@(&$exe -kmcRuntimeRequest $request -kmcRuntimeToken $token -kmcRuntimeRequestSha256 $hash -kmcManagedDirectory (Split-Path -Parent $json));$code=$LASTEXITCODE;$lines|Write-Output
$pass=@($lines|Where-Object{$_-cmatch'^PASS '}).Count;$fail=@($lines|Where-Object{$_-cmatch'^FAIL '}).Count
[ordered]@{nativeInvoked=$false;passes=$pass;failures=$fail;exitCode=$code;inputs=@($hostPath,$partial,$gate,$template,$json)|ForEach-Object{[ordered]@{path=$_;sha256=(Get-KmcSha256 $_)}};adapterSha256=(Get-KmcSha256 $adapter);limits='Unchanged exit partial, completion gate and host drain/writer/argument/hash methods with a real process command line. Native persistence, loading, engine scope and Unity quit are facades; no native qualification.'}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
if($code-ne0-or$pass-eq0-or$fail-ne0){throw 'Actual six-pair failure exit regressed.'}
