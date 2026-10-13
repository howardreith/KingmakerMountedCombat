$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Initialize-Chunk7Tests.ps1')
$source=Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7PersistenceScenario.SaveBoundary.cs'
$bootstrap=Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/PersistenceIsolationBootstrap.cs'
$case=[regex]::Match([IO.File]::ReadAllText($source),'internal static bool IsSaveBoundaryCase\(string value\) => [^;]+;').Value
$body=[IO.File]::ReadAllText($bootstrap);$a=$body.IndexOf('declaresTransfer ? request.PersistenceAreaTarget.Area : null, ');if($a-lt0){throw 'Exact bootstrap write-quota argument absent'};$a+='declaresTransfer ? request.PersistenceAreaTarget.Area : null, '.Length;$e=$body.IndexOf(',',$a);$quota=$body.Substring($a,$e-$a)
$schema2=[regex]::Match($body,'var schema2Resave = request.Scenario == Chunk7PersistenceScenario.LoadScenario &&\s*request.PersistenceCase == "legacy-mounted-pending-tb-manual";').Value
if(-not$case-or$quota-notmatch'IsSaveBoundaryCase'-or-not$schema2){throw 'Closed native save quota selector absent'}
$probe=Join-Path $chunk7AnalysisRoot ('save-boundary-protocol-'+[Guid]::NewGuid().ToString('N'));[void][IO.Directory]::CreateDirectory($probe)
$text=[IO.File]::ReadAllText((Join-Path $chunk7TestSource 'SaveBoundaryProtocolTemplate.cs')).Replace('__CASE_METHOD__',$case).Replace('__WRITE_QUOTA__',$quota).Replace('__SCHEMA2_SELECTOR__',$schema2)
[IO.File]::WriteAllText((Join-Path $probe 'SaveBoundaryProtocol.cs'),$text)
[ordered]@{sourceSha256=(Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash.ToLowerInvariant();bootstrapSha256=(Get-FileHash -LiteralPath $bootstrap -Algorithm SHA256).Hash.ToLowerInvariant();nativeEvidence=$false}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $probe 'source-receipt.json')
$vs=Join-Path ([Environment]::GetFolderPath('ProgramFilesX86')) 'Microsoft Visual Studio/Installer/vswhere.exe'
$compiler=@(& $vs -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild/**/Bin/Roslyn/csc.exe')|Select-Object -First 1
$exe=Join-Path $probe 'SaveBoundaryProtocol.exe'
& $compiler /nologo /target:exe /langversion:7.3 "/out:$exe" (Join-Path $probe 'SaveBoundaryProtocol.cs') (Join-Path $repo 'src/KingmakerMountedCombat/BuildIdentity.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/RuntimeProtocol.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7NativeBootstrapSelection.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7BootstrapInventorySelection.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7ManualSeedSelection.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7ManualDerivedSelection.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7AcquiredManualSelection.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7CumulativeAcquisitionSelection.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7OwnedPairsSelection.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7OrdinaryAcquisitionSelection.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7OrdinaryFieldSelection.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7SmallCalibrationSelection.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7ProgressionSelection.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Domain/PoseVector3.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Domain/MountedRiderPoseProfile.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Domain/DeclaredSmallRiderBody.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Domain/MountedRiderProfileCatalog.cs')
if($LASTEXITCODE-ne0){throw 'Save-boundary protocol compilation failed'}
& $exe
if($LASTEXITCODE-ne0){throw 'Closed save-boundary protocol/quota checks failed'}
$parameters=(Get-Command (Join-Path $repo 'scripts/runtime/Invoke-KingmakerRuntimeScenario.ps1')).Parameters
$set=@($parameters['PersistenceCase'].Attributes|Where-Object{$_-is[Management.Automation.ValidateSetAttribute]})[0].ValidValues
foreach($value in @('multi-nonselected-mount-unsettled-manual','multi-owned-worker-cancel-manual')){if($value-cnotin$set){throw 'Actual runner rejects a selected exact case'}}
Write-Host 'SAVE BOUNDARY RUNNER PARAMETER PASS=2 FAIL=0; metadata only'
