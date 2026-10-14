[CmdletBinding()]
param([string]$RepositoryRoot,[string]$SourceOverlayRoot,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('OutputDirectory')){$OutputDirectory=Join-Path ([IO.Path]::GetFullPath((Join-Path $RepositoryRoot '../..'))) ('analysis-cache/chunk7-assisted-'+[Guid]::NewGuid().ToString('N'))}
$RepositoryRoot=[IO.Path]::GetFullPath($RepositoryRoot);$OutputDirectory=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $OutputDirectory){throw 'Preserve existing assisted test output.'}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
$inputs=@{};$sourceMethods=@();$substitutions=@();$managedJson=$false
function Pin([string]$Path){$inputs[$Path]=(Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant();return $Path}
function Source([string]$Relative){$p=Join-Path $RepositoryRoot $Relative;if(-not[string]::IsNullOrWhiteSpace($SourceOverlayRoot)){$o=Join-Path $SourceOverlayRoot $Relative;if(Test-Path -LiteralPath $o -PathType Leaf){$p=$o}};return (Pin $p)}
function Member([string]$Text,[string]$Signature){$start=$Text.IndexOf($Signature,[StringComparison]::Ordinal);if($start-lt0-or$Text.IndexOf($Signature,$start+$Signature.Length,[StringComparison]::Ordinal)-ge0){throw ('Missing/ambiguous actual method '+$Signature)};$open=$Text.IndexOf('{',$start);$depth=1;$end=$open+1;while($depth-gt0-and$end-lt$Text.Length){if($Text[$end]-ceq'{'){$depth++}elseif($Text[$end]-ceq'}'){$depth--};$end++};if($depth-ne0){throw 'Unclosed actual method.'};$method=$Text.Substring($start,$end-$start);if($Text.Substring($start,$open-$start).Contains('=>')){$method+=';'};return $method}
function Write-AssistedTestFile([string]$Name,[string]$Text){$p=Join-Path $OutputDirectory $Name;[IO.File]::WriteAllText($p,$Text,[Text.UTF8Encoding]::new($false));return $p}
[void](Pin $PSCommandPath)
$kind='actual-assisted-fixture-probe-contracts';$main='KingmakerMountedCombat.Diagnostics.Tests';$managedJson=$true
$substitutions=@('Native descriptors/world, patch install, worker and event sink are facades; exact constructors/factory/Expect/PreparedAfter execute')
$ordinary=[IO.File]::ReadAllText((Source 'src/KingmakerMountedCombat/Diagnostics/NativeOrdinaryAcquisitionProbe.cs'))
$manual=[IO.File]::ReadAllText((Source 'src/KingmakerMountedCombat/Diagnostics/Chunk7ManualSeedSaveProbe.cs'))
$sourceMethods=@('NativeOrdinaryAcquisitionProbe::.ctor legacy','NativeOrdinaryAcquisitionProbe::.ctor assisted','NativeOrdinaryAcquisitionProbe::ForAssistedFixture','NativeOrdinaryAcquisitionProbe::PreparedAfter','Chunk7ManualSeedSaveProbe::.ctor','Chunk7ManualSeedSaveProbe::Expect','Chunk7ManualSeedSaveProbe::PreparedAfter')
$factory=[regex]::Matches($ordinary,'(?m)^\s*internal static NativeOrdinaryAcquisitionProbe ForAssistedFixture\([^\r\n]+;$')
if($factory.Count-ne1){throw 'Missing/ambiguous actual assisted probe factory.'}
$constructors=(Member $ordinary 'internal NativeOrdinaryAcquisitionProbe(Func<UnitEntityData> actor)')+[Environment]::NewLine+$factory[0].Value.TrimStart()+[Environment]::NewLine+(Member $ordinary 'private NativeOrdinaryAcquisitionProbe(')
$template=[IO.File]::ReadAllText((Source 'tests/Chunk7Diagnostics/AssistedFixtureProbeTemplate.cs'))
$actual=$template.Replace('/*ORDINARY_CTORS*/',$constructors).Replace('/*ORDINARY_PREPARED*/',(Member $ordinary 'internal static void PreparedAfter(')).Replace('/*MANUAL_CTOR*/',(Member $manual 'internal Chunk7ManualSeedSaveProbe(')).Replace('/*MANUAL_EXPECT*/',(Member $manual 'internal void Expect(')).Replace('/*MANUAL_PREPARED*/',(Member $manual 'internal static void PreparedAfter('))
$sources=@((Write-AssistedTestFile 'actual.cs' $actual))

$compiler=Pin 'C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$framework='C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7'
$references=@('mscorlib.dll','System.dll','System.Core.dll'|ForEach-Object{Pin (Join-Path $framework $_)})
if($managedJson){$json=Pin 'C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed/Newtonsoft.Json.dll';$references+=,$json}
$exe=Join-Path $OutputDirectory 'checks.exe'
$arguments=@('/nologo','/target:exe','/langversion:7.3','/nostdlib+','/deterministic+','/nowarn:0649,0414',('/out:'+$exe),('/main:'+$main))+@($references|ForEach-Object{'/reference:'+$_})+@($sources)
$rsp=Write-AssistedTestFile 'compile.rsp' (($arguments|ForEach-Object{'"'+$_+'"'})-join[Environment]::NewLine)
$clock=[Diagnostics.Stopwatch]::StartNew();$global:LASTEXITCODE=0
$compileOutput=@(& $compiler /noconfig ('@'+$rsp) 2>&1);$compileExit=$global:LASTEXITCODE;$compileSeconds=$clock.Elapsed.TotalSeconds
[void](Write-AssistedTestFile 'compile.log' (($compileOutput|ForEach-Object{$_.ToString()})-join[Environment]::NewLine))
$loaderExit=0;$testExit=$null;$testSeconds=0.0;$passed=0;$failed=0;$shape=$false;$runOutput=@()
if($compileExit-eq0){
 $runExe=$exe;$runArguments=@()
 if($managedJson){
  $loader='using System;using System.Reflection;static class Loader{static int Main(string[] args){var j=Assembly.LoadFrom(@"'+$json+'");AppDomain.CurrentDomain.AssemblyResolve+=(s,e)=>e.Name==j.FullName?j:null;var a=Assembly.LoadFrom(args[0]);return (int)a.EntryPoint.Invoke(null,a.EntryPoint.GetParameters().Length==0?null:new object[]{new[]{args[1]}});}}'
  $loaderSource=Write-AssistedTestFile 'Loader.cs' $loader;$runExe=Join-Path $OutputDirectory 'Loader.exe';$global:LASTEXITCODE=0
  $loaderOutput=@(& $compiler /noconfig /nologo /target:exe ('/out:'+$runExe) $loaderSource 2>&1);$loaderExit=$global:LASTEXITCODE
  [void](Write-AssistedTestFile 'loader-compile.log' (($loaderOutput|ForEach-Object{$_.ToString()})-join[Environment]::NewLine));$runArguments=@($exe,(Join-Path $OutputDirectory 'fragments'))
 }
 if($loaderExit-eq0){
  $clock.Restart();$global:LASTEXITCODE=0;$runOutput=@(& $runExe @runArguments 2>&1);$testExit=$global:LASTEXITCODE;$testSeconds=$clock.Elapsed.TotalSeconds
  $lines=@($runOutput|ForEach-Object{$_.ToString()});$totals=@($lines|Where-Object{$_-cmatch'^TOTAL PASS=([0-9]+) FAIL=([0-9]+)$'})
  if($totals.Count-eq1-and$totals[0]-cmatch'^TOTAL PASS=([0-9]+) FAIL=([0-9]+)$'){$passed=[int]$Matches[1];$failed=[int]$Matches[2];$shape=$passed-gt0-and@($lines|Where-Object{$_-clike'PASS *'}).Count-eq$passed-and@($lines|Where-Object{$_-clike'FAIL *'}).Count-eq$failed}
  $lines|ForEach-Object{Write-Host $_}
 }
}
[void](Write-AssistedTestFile 'run.log' (($runOutput|ForEach-Object{$_.ToString()})-join[Environment]::NewLine))
$changed=@($inputs.Keys|Where-Object{(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLowerInvariant()-cne$inputs[$_]})
$ok=$compileExit-eq0-and$loaderExit-eq0-and$testExit-eq0-and$shape-and$failed-eq0-and$changed.Count-eq0
$inputRows=@($inputs.Keys|Sort-Object|ForEach-Object{[ordered]@{path=$_;sha256=$inputs[$_]}})
$outputRows=@([IO.Directory]::GetFiles($OutputDirectory,'*', [IO.SearchOption]::AllDirectories)|Where-Object{$_-notlike'*.exe'}|Sort-Object|ForEach-Object{[ordered]@{path=$_;sha256=(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLowerInvariant()}})
[ordered]@{kind=$kind;status=$(if($ok){'PASS'}else{'FAIL'});compileExit=$compileExit;loaderCompileExit=$loaderExit;exitCode=$testExit;passed=$passed;failed=$failed;resultShapeValid=$shape;compileDurationSeconds=$compileSeconds;testDurationSeconds=$testSeconds;sourceMethods=$sourceMethods;inputs=$inputRows;outputs=$outputRows;changedInputs=$changed;substitutions=$substitutions;nativeInvoked=$false;archivesRead=0}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
if(-not$ok){exit 1};exit 0
