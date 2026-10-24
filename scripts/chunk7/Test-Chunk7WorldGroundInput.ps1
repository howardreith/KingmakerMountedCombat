[CmdletBinding()]
param([string]$RepositoryRoot,[string]$WorldSource,[string]$TemplatePath,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$WorldSource){$WorldSource=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics/Chunk7WorldScenarioEngine.cs'}
if(-not$TemplatePath){$TemplatePath=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/WorldGroundInputAdapterTemplate.cs'}
if(-not$OutputDirectory){$lab=[IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot));$OutputDirectory=Join-Path $lab ('analysis-cache/chunk7-continuation/world-ground-input-'+[Guid]::NewGuid().ToString('N'))}
if(Test-Path -LiteralPath $OutputDirectory){throw 'Fresh focused output directory is required; prior evidence is immutable.'}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
function Extract([string]$Text,[string]$Signature,[bool]$Optional=$false){
 $start=$Text.IndexOf($Signature,[StringComparison]::Ordinal)
 if($start-lt0){if($Optional){return ''};throw ('Actual source method absent: '+$Signature)}
 $brace=$Text.IndexOf('{',$start);$arrow=$Text.IndexOf('=>',$start);$semi=$Text.IndexOf(';',$start)
 if($arrow-ge0-and$arrow-lt$brace){return $Text.Substring($start,$semi-$start+1)}
 $depth=0;for($i=$brace;$i-lt$Text.Length;$i++){if($Text[$i]-eq'{'){$depth++}elseif($Text[$i]-eq'}'){$depth--;if($depth-eq0){return $Text.Substring($start,$i-$start+1)}}}
 throw 'Unbalanced actual source method.'
}
$source=[IO.File]::ReadAllText($WorldSource);$template=[IO.File]::ReadAllText($TemplatePath)
$methods=[ordered]@{
 MOVE=(Extract $source 'private void Move(UnitEntityData selected, UnitEntityData executor, Vector3 goal, UnitEntityData switchTo, string label)')
 SELECT=(Extract $source 'private void Select(UnitEntityData actor)')
 ACTORS=(Extract $source 'private UnitEntityData[] Actors()')
 POINT=(Extract $source 'private static JObject Point(Vector3 point)')
 ID=(Extract $source 'private static int Id(object value)')
 REQUIRE=(Extract $source 'private static void Require(bool condition, string error)')
 SETTLED=(Extract $source 'private bool NativeCommandsSettled()')
 SNAPSHOT=(Extract $source 'private sealed class GroundInputSnapshot' $true)
 RECORD_ERROR=(Extract $source 'private static void RecordGroundInputError(JObject row, string field, Exception error)' $true)
 COMMAND=(Extract $source 'private static JObject CaptureGroundInputCommand(UnitCommand command)' $true)
 CAPTURE=(Extract $source 'private GroundInputSnapshot CaptureGroundInput(UnitEntityData[] actors)' $true)
 RETAIN=(Extract $source 'private void RetainGroundInputCommands(UnitEntityData[] actors, GroundInputSnapshot before, GroundInputSnapshot after, JObject attempt)' $true)
 GROUND_SETTLED=(Extract $source 'private bool GroundInputCommandsSettled()' $true)
}
$match=[regex]::Match($source,'foreach \(var command in issuedCommands\.Where\(x => x != null && !x\.IsFinished\)\)\r?\n\s*TryCleanup\("command " \+ Id\(command\), \(\) => command\.Interrupt\(\)\);')
if(-not$match.Success){throw 'Actual cleanup interruption loop is absent.'}
$methods['CLEANUP_INTERRUPT']=$match.Value
foreach($item in $methods.GetEnumerator()){$marker='/* '+$item.Key+' */';if(-not$template.Contains($marker)){throw ('Template lacks '+$marker)};$template=$template.Replace($marker,$item.Value)}
$actual=Join-Path $OutputDirectory 'actual.cs';[IO.File]::WriteAllText($actual,$template)
Copy-Item -LiteralPath $WorldSource -Destination (Join-Path $OutputDirectory 'source.cs')
Copy-Item -LiteralPath $TemplatePath -Destination (Join-Path $OutputDirectory 'template.cs')
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$json='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed/Newtonsoft.Json.dll'
Copy-Item -LiteralPath $json -Destination (Join-Path $OutputDirectory 'Newtonsoft.Json.dll')
$exe=Join-Path $OutputDirectory 'actual.exe';$timer=[Diagnostics.Stopwatch]::StartNew()
$compile=@('/nologo','/langversion:7.3','/target:exe','/warn:0',('/reference:'+$json),('/out:'+$exe),$actual)
$global:LASTEXITCODE=0;$compileLog=@(& $compiler @compile);$compileExit=$global:LASTEXITCODE
$compileLog|Set-Content -LiteralPath (Join-Path $OutputDirectory 'compile.log')
$lines=@();$code=$null
if($compileExit-eq0){$global:LASTEXITCODE=0;$lines=@(& $exe);$code=$global:LASTEXITCODE;$lines|Write-Host}
$timer.Stop();$lines|Set-Content -LiteralPath (Join-Path $OutputDirectory 'test.log')
$passes=@($lines|Where-Object{$_-cmatch'^PASS '}).Count;$failures=@($lines|Where-Object{$_-cmatch'^FAIL '}).Count
$inputs=@($WorldSource,$TemplatePath,$compiler,$json,$actual)|ForEach-Object{[pscustomobject]@{path=$_;sha256=(Get-FileHash -LiteralPath $_).Hash.ToLowerInvariant()}}
$status=if($compileExit-eq0-and$code-eq0-and$passes-gt0-and$failures-eq0){'PASS'}else{'FAIL'}
[pscustomobject]@{kind='chunk7-actual-world-ground-input';status=$status;passed=$passes;failed=$failures;compileExitCode=$compileExit;exitCode=$code;durationSeconds=$timer.Elapsed.TotalSeconds;durationBasis='compiler plus actual extracted adapter';inputs=$inputs;nativeInvoked=$false;archiveRead=$false;limits='Native publication, Unity state and optional telemetry are facades. Exact Move, Select, command snapshots, reference custody, settlement and cleanup interruption loop are production extracts; no full engine/native routing or performance proof.'}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json')
if($status-cne'PASS'){exit 1}
