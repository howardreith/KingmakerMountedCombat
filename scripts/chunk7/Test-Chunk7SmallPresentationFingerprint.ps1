[CmdletBinding()]
param([string]$RepositoryRoot,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/small-presentation-fingerprint-'+[Guid]::NewGuid().ToString('N'))}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
$source=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat'
$sources=@(
    (Join-Path $source 'Domain/PoseVector3.cs'),
    (Join-Path $source 'Domain/MountedRiderPoseProfile.cs'),
    (Join-Path $source 'Domain/DeclaredSmallRiderBody.cs'),
    (Join-Path $source 'Integration/SmallRiderBodyFingerprint.cs'),
    (Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/SmallRiderBodyFingerprintTests.cs')
)
$json='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed/Newtonsoft.Json.dll'
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$dll=Join-Path $OutputDirectory 'actual-small-fingerprint.dll'
& $compiler /nologo /target:library /langversion:7.3 /warnaserror+ "/reference:$json" "/out:$dll" $sources
if($LASTEXITCODE-ne0){throw 'Actual Small presentation fingerprint compile failed.'}
[void][Reflection.Assembly]::LoadFrom($json)
$assembly=[Reflection.Assembly]::LoadFrom($dll)
$test=$assembly.GetType('KingmakerMountedCombat.Tests.SmallRiderBodyFingerprintTests',$true)
$count=$test.GetMethod('Run').Invoke($null,@())
if($count-ne8){throw 'Actual Small presentation fingerprint test count differed.'}
Write-Host 'SMALL PRESENTATION FINGERPRINT PASS=8 FAIL=0; actual projection, no game operation.'
