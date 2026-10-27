[CmdletBinding()]
param([string]$RepositoryRoot,[string]$ScenarioSource,[string]$TemplatePath,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('ScenarioSource')){$ScenarioSource=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics/Chunk7BootstrapInventoryScenario.cs'}
if(-not$PSBoundParameters.ContainsKey('TemplatePath')){$TemplatePath=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/BootstrapFixtureReadinessAdapterTemplate.cs'}
if(-not$PSBoundParameters.ContainsKey('OutputDirectory')){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/bootstrap-readiness-adapter-'+[Guid]::NewGuid().ToString('N'))}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
$source=[IO.File]::ReadAllText($ScenarioSource);$start=$source.IndexOf('        private JObject CaptureFixtureReadiness()', [StringComparison]::Ordinal);$end=$source.IndexOf('        private void ReadSource()', [StringComparison]::Ordinal)
if($start-lt0-or$end-le$start){throw 'Actual readiness method boundaries are missing.'}
$method=$source.Substring($start,$end-$start);$template=[IO.File]::ReadAllText($TemplatePath);if(([regex]::Matches($template,'/\* ACTUAL_METHOD \*/')).Count-ne1){throw 'Actual adapter marker differs.'}
$actual=$template.Replace('/* ACTUAL_METHOD */',$method);$actualPath=Join-Path $OutputDirectory 'actual.cs';[IO.File]::WriteAllText($actualPath,$actual)
$csc='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe';$managed='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed';$json=Join-Path $managed 'Newtonsoft.Json.dll'
$exe=Join-Path $OutputDirectory 'actual.exe';$clock=[Diagnostics.Stopwatch]::StartNew()
& $csc /nologo /langversion:7.3 /warnaserror+ /nowarn:0649 /target:exe ('/out:'+$exe) ('/reference:'+$json) $actualPath
if($LASTEXITCODE-ne0){throw 'Actual readiness adapter compile failed.'}
Copy-Item -LiteralPath $json -Destination (Join-Path $OutputDirectory 'Newtonsoft.Json.dll')
$output=@(& $exe 2>&1);$exit=$LASTEXITCODE;$clock.Stop();$output|ForEach-Object{Write-Host $_};$output|Set-Content -LiteralPath (Join-Path $OutputDirectory 'actual.log') -Encoding UTF8
$summary=@($output|Where-Object{$_-match'^TOTAL PASS=([0-9]+) FAIL=([0-9]+)$'});if($summary.Count-ne1){throw 'Adapter summary missing.'};[void]($summary[0]-match'^TOTAL PASS=([0-9]+) FAIL=([0-9]+)$')
@{passes=[int]$Matches[1];failures=[int]$Matches[2];exitCode=$exit;durationSeconds=$clock.Elapsed.TotalSeconds;scenarioSha256=(Get-FileHash $ScenarioSource).Hash.ToLowerInvariant();templateSha256=(Get-FileHash $TemplatePath).Hash.ToLowerInvariant();actualMethodSha256=(Get-FileHash $actualPath).Hash.ToLowerInvariant();nativeExecution=$false;archiveReads=0}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
if($exit-ne0){throw 'Actual readiness method checks failed.'}
