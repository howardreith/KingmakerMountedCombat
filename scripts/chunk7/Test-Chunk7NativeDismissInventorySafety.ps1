[CmdletBinding()]
param([string]$RepositoryRoot,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
if(-not$PSBoundParameters.ContainsKey('OutputDirectory')){$OutputDirectory=Join-Path (Get-KmcLabRoot) ('analysis-cache/chunk7-continuation/native-dismiss-inventory-'+[Guid]::NewGuid().ToString('N'))}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
$source=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Integration/NativeMountedDismissInventorySafety.cs'
$template=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/NativeDismissInventorySafetyTemplate.cs'
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$framework='C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7'
$exe=Join-Path $OutputDirectory 'native-dismiss-inventory.exe';$rsp=Join-Path $OutputDirectory 'compile.rsp'
$arguments=@('/nologo','/target:exe','/langversion:7.3','/warnaserror+','/optimize+','/deterministic+','/nostdlib+',('/out:'+$exe))
$arguments+=@('mscorlib.dll','System.dll','System.Core.dll')|ForEach-Object{'/reference:'+(Join-Path $framework $_)}
$arguments+=@($source,$template)
[IO.File]::WriteAllLines($rsp,@($arguments|ForEach-Object{'"'+$_+'"'}),[Text.UTF8Encoding]::new($false))
$watch=[Diagnostics.Stopwatch]::StartNew();&$compiler ('@'+$rsp);if($LASTEXITCODE-ne0){throw 'Actual native dismiss inventory rewrite compilation failed.'}
$output=@(&$exe);$code=$LASTEXITCODE;$watch.Stop();$output|ForEach-Object{Write-Output $_}
$receipt=[ordered]@{status=$(if($code-eq0){'PASS'}else{'FAIL'});exitCode=$code;seconds=$watch.Elapsed.TotalSeconds;results=$output;sourceSha256=(Get-FileHash -LiteralPath $source).Hash.ToLowerInvariant();templateSha256=(Get-FileHash -LiteralPath $template).Hash.ToLowerInvariant();nativeInvoked=$false;externalBoundary='Exact production rewrite and selector compiled with native dismissal/inventory contracts represented at the game boundary.'}
$receipt|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
if($code-ne0){throw 'Native mounted dismissal inventory regression failed.'}
