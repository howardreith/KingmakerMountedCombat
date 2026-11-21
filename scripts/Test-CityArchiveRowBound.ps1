[CmdletBinding()]
param()
# The owned archive's control-row bound through the production DTO, validator and codec, compiled from THIS worktree's
# sources (the same component set Test-Chunk7CurrentRemovalArchive.ps1 compiles) with
# tests/Chunk7Diagnostics/ArchiveControlRowBoundTests.cs: an exact-fit 128-row archive round-trips every row; 127 rows
# plus an absent rider's arrangement are refused by MountedArchiveData's own validation. In-memory graphs only; no native
# storage, game callback or product build is exercised.
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$output=Join-Path ([IO.Path]::GetTempPath()) ('kmc-archive-row-bound-'+[Guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($output)
$relative=@('src/KingmakerMountedCombat/Integration/MountedSaveData.cs','src/KingmakerMountedCombat/Integration/SavedCombatData.cs',
    'src/KingmakerMountedCombat/Domain/PairedActivationSnapshot.cs','src/KingmakerMountedCombat/Integration/MountedLoadAdmissionPolicy.cs',
    'src/KingmakerMountedCombat/Integration/MountedArchiveData.cs','src/KingmakerMountedCombat/Domain/MountedControlSlotPolicy.cs',
    'src/KingmakerMountedCombat/Domain/MountedControlPlacementIntent.cs','src/KingmakerMountedCombat/Domain/NativeMountedControl.cs',
    'src/KingmakerMountedCombat/Integration/MountedArchiveCodec.cs','src/KingmakerMountedCombat/Integration/NativeMountedSaveFormat.cs',
    'src/KingmakerMountedCombat/Domain/DeclaredSmallRiderBody.cs','src/KingmakerMountedCombat/Domain/MountedRiderProfileCatalog.cs',
    'src/KingmakerMountedCombat/Domain/MountedRiderPoseProfile.cs','src/KingmakerMountedCombat/Domain/PoseVector3.cs',
    'tests/Chunk7Diagnostics/ArchiveControlRowBoundTests.cs')
$paths=@($relative|ForEach-Object{Join-Path $repo $_})
foreach($path in $paths){if(-not(Test-Path -LiteralPath $path -PathType Leaf)){throw "Archive bound input is missing: $path"}}
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$lab=[IO.Path]::GetFullPath((Join-Path $repo '../..'))
$layout=(Get-Content -Raw (Join-Path $lab 'environment-intake.json')|ConvertFrom-Json).requestedLayout
$json=Join-Path $layout.kingmakerInstallDir 'Kingmaker_Data/Managed/Newtonsoft.Json.dll'
$framework='C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7'
$exe=Join-Path $output 'ArchiveControlRowBoundTests.exe';$rsp=Join-Path $output 'compile.rsp'
$arguments=@('/nologo','/target:exe','/langversion:7.3','/warnaserror+','/nostdlib+',('/out:'+$exe),('/reference:'+$json))+@('mscorlib.dll','System.dll','System.Core.dll'|ForEach-Object{'/reference:'+(Join-Path $framework $_)})
[IO.File]::WriteAllLines($rsp,@(($arguments+$paths)|ForEach-Object{'"'+$_+'"'}),[Text.UTF8Encoding]::new($false))
$compile=@(& $compiler /noconfig ('@'+$rsp) 2>&1)
if($LASTEXITCODE-ne0){$compile|ForEach-Object{Write-Output $_};throw 'Archive row bound test did not compile.'}
Copy-Item -LiteralPath $json -Destination (Join-Path $output 'Newtonsoft.Json.dll')
$unity=Join-Path $layout.kingmakerInstallDir 'Kingmaker_Data/Managed/UnityEngine.dll'
$result=@(& $exe $unity 2>&1);$exit=$LASTEXITCODE
$result|ForEach-Object{Write-Output $_}
$passed=@($result|Where-Object{$_-cmatch'^PASS '}).Count;$failed=@($result|Where-Object{$_-cmatch'^FAIL '}).Count
Write-Output ('ARCHIVE ROW BOUND PASS='+$passed+' FAIL='+$failed+'; production MountedArchiveData/MountedArchiveCodec from '+$repo+'; not native evidence')
if($exit-ne0-or$failed-ne0-or$passed-lt3){exit 1}
