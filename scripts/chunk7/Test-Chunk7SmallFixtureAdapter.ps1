[CmdletBinding()]
param([string]$RepositoryRoot,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
$analysis=[IO.Path]::GetFullPath('C:/Dev/KingmakerMountedCombatLab/analysis-cache')
if(-not$OutputDirectory){$OutputDirectory=Join-Path $analysis ('chunk7-continuation/f09-adapter-'+[Guid]::NewGuid().ToString('N'))}
$OutputDirectory=[IO.Path]::GetFullPath($OutputDirectory)
if(-not$OutputDirectory.StartsWith($analysis+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)-or(Test-Path -LiteralPath $OutputDirectory)){throw 'A fresh private analysis-cache adapter directory is required.'}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
function Method([string]$Text,[string]$Signature){$s=$Text.IndexOf($Signature,[StringComparison]::Ordinal);if($s-lt0-or$s-ne$Text.LastIndexOf($Signature,[StringComparison]::Ordinal)){throw 'Actual F09 method absent or ambiguous.'};$o=$Text.IndexOf('{',$s);$d=1;$e=$o+1;while($d-gt0-and$e-lt$Text.Length){if($Text[$e]-ceq'{'){$d++}elseif($Text[$e]-ceq'}'){$d--};$e++};if($d-ne0){throw 'Unbalanced actual F09 method.'};$Text.Substring($s,$e-$s)}
$diag=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics';$domain=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Domain'
$observation=Join-Path $diag 'Chunk7SmallFixtureScenario.Observation.cs';$text=[IO.File]::ReadAllText($observation)
$body=@((Method $text 'internal static void RequireSmallBody('),(Method $text 'internal static JObject CaptureBody('))
# CaptureBody is expression-bodied: bound by the following class closure, not by
# a block-body brace scanner (the first brace belongs to the JObject initializer).
$s=$text.IndexOf('internal static JObject CaptureBody(',[StringComparison]::Ordinal);$e=$text.IndexOf(';',$s)
if($s-lt0-or$e-lt$s){throw 'Actual F09 body projection absent.'};$body[1]=$text.Substring($s,$e-$s+1)
$template=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/SmallFixtureAdapterTemplate.cs';$actual=Join-Path $OutputDirectory 'ActualSmallFixtureAdapter.cs'
[IO.File]::WriteAllText($actual,([IO.File]::ReadAllText($template).Replace('__BODY_METHODS__',($body-join[Environment]::NewLine))),[Text.UTF8Encoding]::new($false))
$selection=Join-Path $diag 'Chunk7SmallFixtureSelection.cs';$authority=Join-Path $diag 'PersistenceSaveAuthorization.SmallFixture.cs'
$stockSelection=Join-Path $diag 'Chunk7StockMammothFixtureSelection.cs';$stockAuthority=Join-Path $diag 'PersistenceSaveAuthorization.StockMammothFixture.cs'
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe';$json='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed/Newtonsoft.Json.dll'
$exe=Join-Path $OutputDirectory 'SmallFixtureAdapter.exe';$watch=[Diagnostics.Stopwatch]::StartNew()
$sources=@($actual,$selection,$authority,$stockSelection,$stockAuthority,(Join-Path $domain 'DeclaredSmallRiderBody.cs'),(Join-Path $domain 'PoseVector3.cs'),(Join-Path $domain 'MountedRiderPoseProfile.cs'))
$compile=@(&$compiler /nologo /target:exe /langversion:7.3 '/nowarn:0649,0414' "/reference:$json" "/out:$exe" $sources 2>&1);$compileExit=$LASTEXITCODE;$compile|Set-Content -LiteralPath (Join-Path $OutputDirectory 'compile.log')
$test=@();$testExit=$null
if($compileExit-eq0){[void][Reflection.Assembly]::LoadFrom($json);$assembly=[Reflection.Assembly]::LoadFrom($exe);$capture=[IO.StringWriter]::new();$previous=[Console]::Out
 try{[Console]::SetOut($capture);$testExit=[int]$assembly.EntryPoint.Invoke($null,[object[]]@(,[string[]]@((Join-Path $OutputDirectory 'synthetic-storage'))))}finally{[Console]::SetOut($previous)}
 $test=@($capture.ToString()-split'\r?\n'|Where-Object{$_-ne''});$test|Set-Content -LiteralPath (Join-Path $OutputDirectory 'test.log');$test|Write-Output
}
$watch.Stop();$passes=@($test|Where-Object{$_-cmatch'^PASS '}).Count;$fails=@($test|Where-Object{$_-cmatch'^FAIL '}).Count;$ok=$compileExit-eq0-and$testExit-eq0-and$passes-gt0-and$fails-eq0
@{status=$(if($ok){'PASS'}else{'FAIL'});pass=$passes;fail=$fails;compileExit=$compileExit;testExit=$testExit;elapsedSeconds=$watch.Elapsed.TotalSeconds;nativeExecution=$false;sharedBuildOutputsTouched=$false;
 limits='Actual closed selection/preflight and new authority partial plus actual unmounted body methods. Common RuntimeRequest validation, authority storage backing and native game are facades. No product compile, shared wiring, native progression, hire, field or save qualification.';
 sourceHashes=@(($sources+@($observation,$template,$PSCommandPath))|ForEach-Object{@{path=$_;sha256=(Get-FileHash -LiteralPath $_).Hash.ToLowerInvariant()}})}|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
if(-not$ok){$compile|Write-Output;throw 'F09 pure adapter failed; preserve outputs.'}
