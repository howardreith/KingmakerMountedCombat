[CmdletBinding()]
param([string]$RepositoryRoot,[string]$DiagnosticSourceDirectory,[string]$TemplatePath,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('DiagnosticSourceDirectory')){$DiagnosticSourceDirectory=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics'}
if(-not$PSBoundParameters.ContainsKey('TemplatePath')){$TemplatePath=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/AcquiredManualSaveProbeTemplate.cs'}
if(-not$PSBoundParameters.ContainsKey('OutputDirectory')){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/acquired-manual-probe-'+[Guid]::NewGuid().ToString('N'))}
if(Test-Path -LiteralPath $OutputDirectory){throw 'Preserve existing focused output.'};[void][IO.Directory]::CreateDirectory($OutputDirectory)
$path=Join-Path $DiagnosticSourceDirectory 'Chunk7ManualSeedSaveProbe.cs';$source=[IO.File]::ReadAllText($path)
function Extract([string]$signature){$matches=[regex]::Matches($source,[regex]::Escape($signature));if($matches.Count-ne1){throw 'Exact actual method absent/ambiguous.'};$start=$matches[0].Index;$open=$source.IndexOf('{',$start);$depth=1;$end=$open+1;while($depth-gt0-and$end-lt$source.Length){if($source[$end]-ceq'{'){$depth++}elseif($source[$end]-ceq'}'){$depth--};$end++};if($depth-ne0){throw 'Unbalanced source method.'};$source.Substring($start,$end-$start)}
$template=[IO.File]::ReadAllText($TemplatePath)
$methods=@{'//__CONSTRUCTOR__'='internal Chunk7ManualSeedSaveProbe(Game game,';'//__EXPECT__'='internal void Expect(SaveInfo value)';'//__OBSERVE__'='private void Observe(Action action)';'//__PREPARED__'='internal static void PreparedAfter(SaveManager __instance, SaveInfo save)'}
foreach($marker in $methods.Keys){$template=$template.Replace($marker,(Extract $methods[$marker]))}
$actual=Join-Path $OutputDirectory 'actual.cs';[IO.File]::WriteAllText($actual,$template,[Text.UTF8Encoding]::new($false));[IO.File]::WriteAllText((Join-Path $OutputDirectory 'source.cs'),$source,[Text.UTF8Encoding]::new($false))
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe';$refs='C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7';$exe=Join-Path $OutputDirectory 'actual.exe';$watch=[Diagnostics.Stopwatch]::StartNew()
&$compiler /nologo /target:exe /langversion:7.3 /warnaserror+ /nostdlib+ /deterministic+ ('/out:'+$exe) ('/reference:'+(Join-Path $refs 'mscorlib.dll')) ('/reference:'+(Join-Path $refs 'System.dll')) ('/reference:'+(Join-Path $refs 'System.Core.dll')) $actual
if($LASTEXITCODE-ne0){throw 'Actual save-probe boundary compile failed.'}; &$exe;if($LASTEXITCODE-ne0){throw 'Actual save-probe boundary check failed.'};$watch.Stop()
[ordered]@{status='PASS';seconds=$watch.Elapsed.TotalSeconds;sourceSha256=(Get-FileHash -LiteralPath $path).Hash.ToLowerInvariant();actualSha256=(Get-FileHash -LiteralPath $actual).Hash.ToLowerInvariant();templateSha256=(Get-FileHash -LiteralPath $TemplatePath).Hash.ToLowerInvariant();boundary='Exact actual constructor, Expect, Observe, PreparedAfter; external Game/SaveInfo/manager and record storage substituted. No patch, native or archive operation.';nativeProof=$false}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
