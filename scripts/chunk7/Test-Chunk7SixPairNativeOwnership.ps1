[CmdletBinding()]
param([string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Initialize-Chunk7Tests.ps1')
if(-not$OutputDirectory){$OutputDirectory=Join-Path $chunk7AnalysisRoot ('six-native-ownership-'+[Guid]::NewGuid().ToString('N'))}
$OutputDirectory=Assert-KmcChildPath $OutputDirectory $chunk7AnalysisRoot 'six-pair native ownership test'
if(Test-Path -LiteralPath $OutputDirectory){throw 'Preserve earlier six-pair ownership checks.'}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
$integration=Join-Path $repo 'src/KingmakerMountedCombat/Integration';$domain=Join-Path $repo 'src/KingmakerMountedCombat/Domain'
$sources=@((Join-Path $chunk7TestSource 'SixPairNativeOwnershipTests.cs'))
$sources+=@('GameMountedRelationshipService.Ownership.cs','NativeMountedSaveFormat.cs','MountedSaveData.cs','SavedCombatData.cs','MountedArchiveData.cs','MountedArchiveCodec.cs')|ForEach-Object{Join-Path $integration $_}
$sources+=@('MountedPairRegistry.cs','RelationshipState.cs','PairedActivationSnapshot.cs','DeclaredSmallRiderBody.cs','MountedRiderProfileCatalog.cs','MountedRiderPoseProfile.cs','PoseVector3.cs')|ForEach-Object{Join-Path $domain $_}
$json='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed/Newtonsoft.Json.dll'
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$exe=Join-Path $OutputDirectory 'six-native-ownership.exe'
$pins=@($sources|ForEach-Object{[ordered]@{path=$_;sha256=(Get-KmcSha256 $_)}})
$watch=[Diagnostics.Stopwatch]::StartNew()
&$compiler /nologo /target:exe /langversion:7.3 /nowarn:0649 "/out:$exe" "/reference:$json" $sources
if($LASTEXITCODE-ne0){throw 'Actual native ownership adapter compile failed.'}
Copy-Item -LiteralPath $json -Destination (Join-Path $OutputDirectory 'Newtonsoft.Json.dll')
$output=@(&$exe);$code=$LASTEXITCODE;$output|ForEach-Object{Write-Output $_}
[ordered]@{nativeInvoked=$false;sourcePins=$pins;seconds=$watch.Elapsed.TotalSeconds;exitCode=$code;results=$output}|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
if($code-ne0){throw 'Actual native ownership adapter behavior failed.'}
