[CmdletBinding()]
param([string]$RepositoryRoot,[string]$SourceOverlayRoot,[string]$OutputRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('OutputRoot')){$OutputRoot=Join-Path ([IO.Path]::GetFullPath((Join-Path $RepositoryRoot '../..'))) ('analysis-cache/chunk7-bootstrap-scheduled-'+[Guid]::NewGuid().ToString('N'))}
$OutputRoot=[IO.Path]::GetFullPath($OutputRoot)
if(Test-Path -LiteralPath $OutputRoot){throw 'Preserve existing scheduled-readiness output.'}
[void][IO.Directory]::CreateDirectory($OutputRoot)
$inputs=@{}
function Pin([string]$Path){$inputs[$Path]=(Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant();return $Path}
function Source([string]$Relative){
 $path=Join-Path $RepositoryRoot $Relative
 if(-not[string]::IsNullOrWhiteSpace($SourceOverlayRoot)){$candidate=Join-Path $SourceOverlayRoot $Relative;if(Test-Path -LiteralPath $candidate -PathType Leaf){$path=$candidate}}
 return (Pin $path)
}
[void](Pin $PSCommandPath)
$sourcePath=Source 'src/KingmakerMountedCombat/Diagnostics/Chunk7NativeBootstrapScenario.cs'
$templatePath=Source 'tests/Chunk7Diagnostics/BootstrapScheduledReadinessTemplate.cs'
$source=[IO.File]::ReadAllText($sourcePath);$template=[IO.File]::ReadAllText($templatePath)
$signature='        internal void Update()';$start=$source.IndexOf($signature,[StringComparison]::Ordinal)
if($start-lt0-or$source.IndexOf($signature,$start+$signature.Length,[StringComparison]::Ordinal)-ge0){throw 'Missing/ambiguous actual Update method.'}
$open=$source.IndexOf('{',$start);$depth=1;$end=$open+1
while($depth-gt0-and$end-lt$source.Length){if($source[$end]-ceq'{'){$depth++}elseif($source[$end]-ceq'}'){$depth--};$end++}
if($depth-ne0){throw 'Unclosed actual Update method.'}
$update=$source.Substring($start,$end-$start)
$cut=$update.IndexOf('                RequireNoOwners();',[StringComparison]::Ordinal)
$catch=$update.LastIndexOf('            catch (Exception error)',[StringComparison]::Ordinal)
if($cut-lt0-or$catch-lt$cut){throw 'Actual readiness prefix/catch boundary changed.'}
# Preserve the complete actual entry/readiness decision and original catch.
# Native objects, authority/probe/world reads and cleanup are external facades.
# The subsequent save/evidence body is deliberately outside this focused test.
$actual=$update.Substring(0,$cut)+'                ReadyReached++;'+[Environment]::NewLine+'            }'+[Environment]::NewLine+$update.Substring($catch)
if(-not$template.Contains('/*ACTUAL_UPDATE_READINESS*/')){throw 'Actual readiness template marker missing.'}
$body=$template.Replace('/*ACTUAL_UPDATE_READINESS*/',$actual)
$actualPath=Join-Path $OutputRoot 'actual.cs';[IO.File]::WriteAllText($actualPath,$body,[Text.UTF8Encoding]::new($false))
[IO.File]::Copy($sourcePath,(Join-Path $OutputRoot 'scenario.cs'),$false);[IO.File]::Copy($templatePath,(Join-Path $OutputRoot 'template.cs'),$false)
$compiler=Pin 'C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$framework='C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7'
$references=@('mscorlib.dll','System.dll','System.Core.dll'|ForEach-Object{Pin (Join-Path $framework $_)})
$exe=Join-Path $OutputRoot 'checks.exe';$rsp=Join-Path $OutputRoot 'compile.rsp'
$arguments=@('/nologo','/target:exe','/langversion:7.3','/optimize+','/deterministic+','/nostdlib+','/nowarn:0649,0169,0414',('/out:'+$exe))+@($references|ForEach-Object{'/reference:'+$_})+@($actualPath)
[IO.File]::WriteAllLines($rsp,@($arguments|ForEach-Object{'"'+$_+'"'}),[Text.UTF8Encoding]::new($false))
$clock=[Diagnostics.Stopwatch]::StartNew();$global:LASTEXITCODE=0
$compileOutput=@(& $compiler ('@'+$rsp) 2>&1);$compileExit=$global:LASTEXITCODE;$compileSeconds=$clock.Elapsed.TotalSeconds
[IO.File]::WriteAllText((Join-Path $OutputRoot 'compile.log'),(($compileOutput|ForEach-Object{$_.ToString()})-join[Environment]::NewLine))
$testExit=$null;$testSeconds=0.0;$passed=0;$failed=0;$shape=$false;$lines=@()
if($compileExit-eq0){
 $clock.Restart();$global:LASTEXITCODE=0;$testOutput=@(& $exe 2>&1);$testExit=$global:LASTEXITCODE;$testSeconds=$clock.Elapsed.TotalSeconds
 $lines=@($testOutput|ForEach-Object{$_.ToString()});$totals=@($lines|Where-Object{$_-cmatch'^TOTAL PASS=([0-9]+) FAIL=([0-9]+)$'})
 if($totals.Count-eq1-and$totals[0]-cmatch'^TOTAL PASS=([0-9]+) FAIL=([0-9]+)$'){$passed=[int]$Matches[1];$failed=[int]$Matches[2];$shape=$passed-gt0-and(@($lines|Where-Object{$_-clike'PASS *'}).Count-eq$passed)-and(@($lines|Where-Object{$_-clike'FAIL *'}).Count-eq$failed)}
 $lines|ForEach-Object{Write-Host $_}
}
[IO.File]::WriteAllText((Join-Path $OutputRoot 'run.log'),($lines-join[Environment]::NewLine))
$unchanged=$true;foreach($entry in $inputs.GetEnumerator()){if((Get-FileHash -LiteralPath $entry.Key -Algorithm SHA256).Hash.ToLowerInvariant()-cne$entry.Value){$unchanged=$false}}
$ok=$compileExit-eq0-and$testExit-eq0-and$shape-and$failed-eq0-and$unchanged
$receipt=[ordered]@{kind='bootstrap-scheduled-readiness-actual-prefix';status=$(if($ok){'PASS'}else{'FAIL'});passed=$passed;failed=$failed;compileExitCode=$compileExit;testExitCode=$testExit;compileSeconds=$compileSeconds;testSeconds=$testSeconds;inputsUnchanged=$unchanged;nativeInvoked=$false;archivesRead=$false;externalBoundaries=@('native Game/Player/DialogState/views and getters','authority and observer/world admission','clock and cleanup/error delivery');inputs=@($inputs.GetEnumerator()|Sort-Object Key|ForEach-Object{[ordered]@{path=$_.Key;sha256=$_.Value}});actualSha256=(Get-FileHash -LiteralPath $actualPath).Hash.ToLowerInvariant()}
[IO.File]::WriteAllText((Join-Path $OutputRoot 'receipt.json'),($receipt|ConvertTo-Json -Depth 12),[Text.UTF8Encoding]::new($false))
if(-not$ok){exit 1}
