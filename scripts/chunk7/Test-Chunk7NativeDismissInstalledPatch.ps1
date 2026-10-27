[CmdletBinding()]
param([string]$RepositoryRoot,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
if(-not$PSBoundParameters.ContainsKey('OutputDirectory')){$OutputDirectory=Join-Path (Get-KmcLabRoot) ('analysis-cache/chunk7-continuation/native-dismiss-installed-'+[Guid]::NewGuid().ToString('N'))}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
$native='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed'
$framework='C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7'
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$source=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/NativeDismissInstalledPatch.cs'
$product=Join-Path $RepositoryRoot 'bin/Release/KingmakerMountedCombat.dll'
$exe=Join-Path $OutputDirectory 'native-dismiss-installed.exe';$rsp=Join-Path $OutputDirectory 'compile.rsp'
$arguments=@('/nologo','/target:exe','/langversion:7.3','/warnaserror+','/optimize+','/deterministic+','/nostdlib+',('/out:'+$exe))
$arguments+=@('mscorlib.dll','System.dll','System.Core.dll')|ForEach-Object{'/reference:'+(Join-Path $framework $_)}
$arguments+=@('Assembly-CSharp.dll','UnityModManager/0Harmony12.dll')|ForEach-Object{'/reference:'+(Join-Path $native $_)}
$arguments+=$source
[IO.File]::WriteAllLines($rsp,@($arguments|ForEach-Object{'"'+$_+'"'}),[Text.UTF8Encoding]::new($false))
$watch=[Diagnostics.Stopwatch]::StartNew();&$compiler ('@'+$rsp);if($LASTEXITCODE-ne0){throw 'Installed native dismissal patch test compilation failed.'}
$output=@(&$exe $native $product);$code=$LASTEXITCODE;$watch.Stop();$output|ForEach-Object{Write-Output $_}
$receipt=[ordered]@{status=$(if($code-eq0){'PASS'}else{'FAIL'});exitCode=$code;seconds=$watch.Elapsed.TotalSeconds;results=$output;sourceSha256=(Get-FileHash -LiteralPath $source).Hash.ToLowerInvariant();productSha256=(Get-FileHash -LiteralPath $product).Hash.ToLowerInvariant();nativeInvoked=$false;boundary='Exact installed Kingmaker/Harmony12 patch and removal in disposable process; native dismissal is not invoked.'}
$receipt|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
if($code-ne0){throw 'Installed native dismissal patch test failed.'}
