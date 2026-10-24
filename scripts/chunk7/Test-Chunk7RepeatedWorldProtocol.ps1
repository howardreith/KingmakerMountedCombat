$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Initialize-Chunk7Tests.ps1')
$probe=Join-Path $chunk7AnalysisRoot ('probe-'+[Guid]::NewGuid().ToString('N'));[void][IO.Directory]::CreateDirectory($probe)
$vs=Join-Path ([Environment]::GetFolderPath('ProgramFilesX86')) 'Microsoft Visual Studio/Installer/vswhere.exe'
$compiler=@(& $vs -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild/**/Bin/Roslyn/csc.exe')|Select-Object -First 1
$exe=Join-Path $probe 'Chunk7RepeatedWorldProtocolTests.exe'
& $compiler /nologo /target:exe /langversion:7.3 "/out:$exe" (Join-Path $repo 'src/KingmakerMountedCombat/BuildIdentity.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/RuntimeProtocol.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7SmallFixtureSelection.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7StockMammothFixtureSelection.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7NativeBootstrapSelection.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7BootstrapInventorySelection.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7ManualSeedSelection.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7ManualDerivedSelection.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7AcquiredManualSelection.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7AssistedFixtureSelection.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7AssistedColdInventorySelection.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7CumulativeAcquisitionSelection.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7OwnedPairsSelection.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7OrdinaryAcquisitionSelection.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7OrdinaryFieldSelection.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7SmallCalibrationSelection.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7ProgressionSelection.cs') (Join-Path $chunk7TestSource 'Chunk7RepeatedWorldProtocolTests.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Domain/PoseVector3.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Domain/MountedRiderPoseProfile.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Domain/DeclaredSmallRiderBody.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Domain/MountedRiderProfileCatalog.cs')
if($LASTEXITCODE-ne0){throw 'Private repeated-world protocol compilation failed.'}
& $exe
if($LASTEXITCODE-ne0){throw 'Private repeated-world protocol checks failed.'}

$runner=Join-Path $repo 'scripts/runtime/Invoke-KingmakerRuntimeScenario.ps1'
$parameters=(Get-Command $runner).Parameters
$set=@($parameters['PersistenceCase'].Attributes|Where-Object{$_-is[Management.Automation.ValidateSetAttribute]})[0].ValidValues
foreach($case in @('multi-exploration-three-loads-manual','multi-exploration-three-area-transfers-manual')){if($case-cnotin$set){throw 'Runner parameter rejected a selected exact replay case.'}}
foreach($name in @('PersistenceReplayAreaEnterPoints','PersistenceReplayAreaAreas')){if($parameters[$name].ParameterType-ne[string[]]){throw 'Replay route runner parameter is not an explicit string array.'}}
$text=[IO.File]::ReadAllText((Join-Path $repo 'scripts/runtime/Test-RuntimeGameResult.ps1'))
$line=@($text-split"\r?\n"|Where-Object{$_-match'^\s*\$expectedWorkingLoads = '})
if($line.Count-ne1){throw 'Expected native load count expression was not unique.'}
foreach($case in @('multi-exploration-three-loads-manual','multi-exploration-three-area-transfers-manual','multi-exploration-manual')){
    $game=[pscustomobject]@{scenario='chunk7-persistence-load'};$request=[pscustomobject]@{persistenceCase=$case};$absentKmc=$false;$assistedFixture=$false;$assistedWriteCount=0;$campaignB=$false;$alternating=$false;$nativeBootstrap=Test-KmcChunk7NativeBootstrapScenario $game.scenario
    Invoke-Expression $line[0]
    $expected=if($case-ceq'multi-exploration-three-loads-manual'){3}else{1}
    if($expectedWorkingLoads-ne$expected){throw 'Runtime completion native load count differs.'}
}
# b194 x4c: the Horse subscriber expectation counts every pair that mounts the KMC Horse blueprint
# (the Small Halfling Horse profile included). Behavioral checks run the exact product helper.
$horseExe=Join-Path $probe 'ReplayHorseOwnersTests.exe'
& $compiler /nologo /target:exe /langversion:7.3 "/out:$horseExe" (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7ReplayHorseOwners.cs') (Join-Path $repo 'tests/Chunk7Diagnostics/ReplayHorseOwnersTests.cs')
if($LASTEXITCODE-ne0){throw 'Replay Horse owner checks failed to compile.'}
& $horseExe
if($LASTEXITCODE-ne0){throw 'Replay Horse owner checks failed.'}
$replay=[IO.File]::ReadAllText((Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7PersistenceScenario.Replay.cs'))
if($replay-match'ProfileId"\]\s*==\s*SupportedMountedProfiles\.Horse\.Id\)'){throw 'Repeated-world Horse subscriber expectation counts only the Medium Horse profile.'}
if($replay-notmatch'var expectedHorses = Chunk7ReplayHorseOwners\.Count\(\(\(JArray\)replayArchive\["Pairs"\]\)\.Select\(p => \(string\)p\["ProfileId"\]\),\s*id => SupportedMountedProfiles\.ResolveProfileId\(id\)\?\.BlueprintGuid, SupportedMountedProfiles\.HorseBlueprintGuid\);'){throw 'Repeated-world Horse subscriber expectation does not use the product Horse owner helper with the KMC Horse blueprint.'}
# b195 x4c: a capital destination admits only the main character's pair natively; the route refuses it.
if($replay-notmatch'Require\(!enter\.Area\.IsCapital, "Declared area route enters a capital'){throw 'Repeated-area route does not refuse a capital destination.'}
Write-Host 'REPEATED WORLD RUNNER/COMPLETION PASS=9 FAIL=0; no native actions invoked.'
