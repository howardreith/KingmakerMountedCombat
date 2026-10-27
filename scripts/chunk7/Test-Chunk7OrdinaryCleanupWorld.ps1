[CmdletBinding()]
param()
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Initialize-Chunk7Tests.ps1')
$sourcePath=Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7OrdinaryAcquisitionScenario.cs'
$source=[IO.File]::ReadAllText($sourcePath).Replace("
","
")
$start=$source.IndexOf('        private void PollCleanup()',[StringComparison]::Ordinal)
$end=$source.IndexOf('        private void Retain(',$start,[StringComparison]::Ordinal)
if($start-lt0-or$end-le$start){throw 'Exact ordinary cleanup method boundary is unavailable.'}
$method=$source.Substring($start,$end-$start)
$template=[IO.File]::ReadAllText((Join-Path $chunk7TestSource 'OrdinaryCleanupWorldTemplate.cs'))
if(($template.Split(@('/*ACTUAL_POLL_CLEANUP*/'),[StringSplitOptions]::None)).Length-ne2){throw 'Actual-method template marker is ambiguous.'}
$run=Join-Path $chunk7AnalysisRoot ('ordinary-cleanup-world-'+[Guid]::NewGuid().ToString('N'));[void][IO.Directory]::CreateDirectory($run)
$generated=Join-Path $run 'actual.cs';[IO.File]::WriteAllText($generated,$template.Replace('/*ACTUAL_POLL_CLEANUP*/',$method),[Text.UTF8Encoding]::new($false))
$compiler='C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe'
$framework='C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7'
$exe=Join-Path $run 'check.exe';$log=Join-Path $run 'result.log';$response=Join-Path $run 'compile.rsp'
$args=@('/nologo','/target:exe','/langversion:7.3','/deterministic+','/optimize+','/nowarn:0649,0169','/nostdlib+',('/out:'+$exe));$args+=@('mscorlib.dll','System.dll','System.Core.dll')|ForEach-Object{'/reference:'+(Join-Path $framework $_)}
[IO.File]::WriteAllLines($response,@(($args+$generated)|ForEach-Object{'"'+$_+'"'}),[Text.UTF8Encoding]::new($false))
$timer=[Diagnostics.Stopwatch]::StartNew();& $compiler ('@'+$response) 2>&1|Tee-Object -FilePath $log;$compile=$LASTEXITCODE;$test=-1
if($compile-eq0){& $exe 2>&1|Tee-Object -FilePath $log -Append;$test=$LASTEXITCODE};$timer.Stop()
@{compile=$compile;test=$test;elapsedSeconds=$timer.Elapsed.TotalSeconds;actualSourceSha256=(Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash.ToLowerInvariant();adapterSourceSha256=(Get-FileHash -LiteralPath $generated -Algorithm SHA256).Hash.ToLowerInvariant();nativeClaim=$false;scope='Unchanged actual PollCleanup method; only native/UI/storage boundary stubs, no game or save archive accessed.'}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $run 'result.json')
if($compile-ne0-or$test-ne0){throw ('Ordinary actual cleanup world contract failed; receipt '+$run)}
