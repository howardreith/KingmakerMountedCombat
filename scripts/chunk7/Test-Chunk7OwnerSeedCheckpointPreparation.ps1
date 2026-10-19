[CmdletBinding()]
param([string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$analysis=[IO.Path]::GetFullPath((Join-Path $repo '../../analysis-cache'))
if(-not$OutputDirectory){$OutputDirectory=Join-Path $analysis ('chunk7-continuation/owner-seed-preparation-'+[Guid]::NewGuid().ToString('N'))}
$OutputDirectory=[IO.Path]::GetFullPath($OutputDirectory)
if(-not$OutputDirectory.StartsWith($analysis+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)-or(Test-Path -LiteralPath $OutputDirectory)){throw 'Fresh analysis-cache output is required.'}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
$source=Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7PersistenceScenario.OwnerSeedCheckpoint.cs'
$text=[IO.File]::ReadAllText($source)
function Get-ActualPreparationMethod([string]$Signature){
 $start=$text.IndexOf($Signature,[StringComparison]::Ordinal)
 if($start-lt0-or$text.LastIndexOf($Signature,[StringComparison]::Ordinal)-ne$start){throw 'Actual preparation method absent or ambiguous.'}
 $open=$text.IndexOf('{',$start);$depth=1;$end=$open+1
 while($depth-gt0-and$end-lt$text.Length){if($text[$end]-ceq'{'){$depth++}elseif($text[$end]-ceq'}'){$depth--};$end++}
 if($depth-ne0){throw 'Unbalanced actual preparation method.'}
 $text.Substring($start,$end-$start)
}
$methods=@('private void BeginOwnerSeedCheckpointPreparation()','private void PollOwnerSeedCheckpointPreparation()',
 'private void RequireOwnerSeedPreparedSlots()','private void RememberOwnerSeedRemountedSecondPair()')|ForEach-Object{Get-ActualPreparationMethod $_}
$template=Join-Path $repo 'tests/Chunk7Diagnostics/OwnerSeedCheckpointPreparationTemplate.cs'
$factSource=Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7PersistenceScenario.ControlFacts.cs'
$text=[IO.File]::ReadAllText($factSource)
$methods+=Get-ActualPreparationMethod '        private JObject CaptureOwnerSeedControlFacts()'
$schema1Source=Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7PersistenceScenario.Schema1.cs'
$text=[IO.File]::ReadAllText($schema1Source)
$methods+=Get-ActualPreparationMethod '        private void BeginOwnedSchema1Source()'
$actual=Join-Path $OutputDirectory 'ActualPreparation.cs'
[IO.File]::WriteAllText($actual,([IO.File]::ReadAllText($template).Replace('__METHODS__',($methods-join[Environment]::NewLine))),[Text.UTF8Encoding]::new($false))
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$json='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed/Newtonsoft.Json.dll'
$exe=Join-Path $OutputDirectory 'Preparation.exe';$clock=[Diagnostics.Stopwatch]::StartNew()
$compile=@(&$compiler /nologo /target:exe /langversion:7.3 '/nowarn:0649,0414' "/reference:$json" "/out:$exe" $actual 2>&1);$compileExit=$LASTEXITCODE
$compile|Set-Content -LiteralPath (Join-Path $OutputDirectory 'compile.log')
$test=@();$testExit=$null
if($compileExit-eq0){Copy-Item -LiteralPath $json -Destination $OutputDirectory;$test=@(&$exe 2>&1);$testExit=$LASTEXITCODE;$test|Set-Content -LiteralPath (Join-Path $OutputDirectory 'test.log');$test|ForEach-Object{Write-Output $_}}
$passed=@($test|Where-Object{$_-cmatch'^PASS '}).Count;$failed=@($test|Where-Object{$_-cmatch'^FAIL '}).Count
$ok=$compileExit-eq0-and$testExit-eq0-and$passed-gt0-and$failed-eq0
[ordered]@{status=$(if($ok){'PASS'}else{'FAIL'});passed=$passed;failed=$failed;compileExit=$compileExit;testExit=$testExit;elapsedSeconds=$clock.Elapsed.TotalSeconds;sourceSha256=(Get-FileHash -LiteralPath $source).Hash.ToLowerInvariant();factSourceSha256=(Get-FileHash -LiteralPath $factSource).Hash.ToLowerInvariant();schema1SourceSha256=(Get-FileHash -LiteralPath $schema1Source).Hash.ToLowerInvariant();templateSha256=(Get-FileHash -LiteralPath $template).Hash.ToLowerInvariant();actualSha256=(Get-FileHash -LiteralPath $actual).Hash.ToLowerInvariant();nativeInvoked=$false;limits='Six exact diagnostic methods including native fact observation and schema1 source slot adoption; native input/world/debt and inherited fixture boundaries use facades. No loader or engine delivery is executed.'}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json')
if(-not$ok){$compile|Write-Output;throw 'Actual owner-seed preparation regression failed; retain evidence.'}
