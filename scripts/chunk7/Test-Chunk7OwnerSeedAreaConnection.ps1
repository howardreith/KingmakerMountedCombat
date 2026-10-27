[CmdletBinding()]
param([string]$RepositoryRoot,[string]$DiagnosticSourceDirectory,[string]$TemplatePath,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$DiagnosticSourceDirectory){$DiagnosticSourceDirectory=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics'}
if(-not$TemplatePath){$TemplatePath=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/OwnerSeedAreaConnectionTemplate.cs'}
if(-not$OutputDirectory){throw 'An explicit fresh private analysis-cache output is required.'}
$output=[IO.Path]::GetFullPath($OutputDirectory);$analysis='C:\Dev\KingmakerMountedCombatLab\analysis-cache\'
if(-not$output.StartsWith($analysis,[StringComparison]::OrdinalIgnoreCase)-or(Test-Path -LiteralPath $output)){throw 'Fresh private analysis-cache output required.'}
[void][IO.Directory]::CreateDirectory($output)
function Method([string]$Text,[string]$Signature){$s=$Text.IndexOf($Signature,[StringComparison]::Ordinal);if($s-lt0-or$Text.LastIndexOf($Signature,[StringComparison]::Ordinal)-ne$s){throw 'Actual area method absent or ambiguous.'};$o=$Text.IndexOf('{',$s);$depth=1;$e=$o+1;while($depth-gt0-and$e-lt$Text.Length){if($Text[$e]-ceq'{'){$depth++}elseif($Text[$e]-ceq'}'){$depth--};$e++};if($depth-ne0){throw 'Unbalanced actual method.'};$Text.Substring($s,$e-$s)}
$fixture=Join-Path $DiagnosticSourceDirectory 'Chunk7OwnedPairFixture.Replay.cs';$gate=Join-Path $DiagnosticSourceDirectory 'Chunk7OwnerSeedAreaEntry.cs'
$text=[IO.File]::ReadAllText($fixture);$methods=@((Method $text '        internal void BeginAreaAutosave('),(Method $text '        internal void AdoptAreaAutosave('))
$sourceReader=Join-Path $DiagnosticSourceDirectory 'Chunk7OwnedPairFixture.cs';$sourceMethod=Method ([IO.File]::ReadAllText($sourceReader)) '        private void ReadOwnerSeedDerivedSource('
$actual=Join-Path $output 'ActualOwnerSeedAreaConnection.cs';[IO.File]::WriteAllText($actual,([IO.File]::ReadAllText($TemplatePath).Replace('/* ACTUAL_AREA_METHODS */',($methods-join[Environment]::NewLine)).Replace('/* ACTUAL_SOURCE_METHOD */',$sourceMethod)),[Text.UTF8Encoding]::new($false))
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe';$json='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed/Newtonsoft.Json.dll'
$exe=Join-Path $output 'AreaConnection.exe';$clock=[Diagnostics.Stopwatch]::StartNew();$compile=@(&$compiler /nologo /target:exe /langversion:7.3 '/nowarn:0649,0414' ('/reference:'+$json) ('/out:'+$exe) $actual $gate 2>&1);$compileExit=$LASTEXITCODE
[IO.File]::WriteAllLines((Join-Path $output 'compile.log'),[string[]]$compile,[Text.UTF8Encoding]::new($false));$testExit=$null;$test=@()
if($compileExit-eq0){Copy-Item -LiteralPath $json -Destination $output;$test=@(&$exe 2>&1);$testExit=$LASTEXITCODE;[IO.File]::WriteAllLines((Join-Path $output 'test.log'),[string[]]$test,[Text.UTF8Encoding]::new($false));$test|ForEach-Object{Write-Output $_}}
$passed=@($test|Where-Object{$_-cmatch'^PASS '}).Count;$failed=@($test|Where-Object{$_-cmatch'^FAIL '}).Count;$ok=$compileExit-eq0-and$testExit-eq0-and$failed-eq0-and$passed-gt0
$receipt=[ordered]@{status=$(if($ok){'PASS'}else{'FAIL'});passed=$passed;failed=$failed;compileExit=$compileExit;testExit=$testExit;seconds=$clock.Elapsed.TotalSeconds;inputs=@(@($fixture,$gate,$sourceReader,$TemplatePath,$json)|ForEach-Object{[ordered]@{path=$_;sha256=(Get-FileHash -LiteralPath $_).Hash.ToLowerInvariant()}});actualSha256=(Get-FileHash -LiteralPath $actual).Hash.ToLowerInvariant();nativeInvoked=$false;limits='Actual complete read-only entry gate and extracted area admission/adoption and source-read methods; request/selection, Game, Blueprint, location, field and authority containers are facades. Actual selection/authority is checked separately. No native entry, autosave or runtime restoration delivery.'}
[IO.File]::WriteAllText((Join-Path $output 'receipt.json'),($receipt|ConvertTo-Json -Depth 7),[Text.UTF8Encoding]::new($false))
if(-not$ok){$compile|ForEach-Object{Write-Output $_};throw 'Private owner-seed actual area connection regression failed; preserve outputs.'}
