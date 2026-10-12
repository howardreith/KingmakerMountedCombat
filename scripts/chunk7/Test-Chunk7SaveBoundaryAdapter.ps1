param([string]$ScenarioSource,[switch]$BeforeGuardRepairs)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Initialize-Chunk7Tests.ps1')
function Get-ActualBoundaryMethod([string]$Text,[string]$Signature){$a=$Text.IndexOf($Signature);if($a-lt0){throw ('Missing exact method '+$Signature)};$o=$Text.IndexOf('{',$a);$semi=$Text.IndexOf(';',$a);$arrow=$Text.IndexOf('=>',$a);if($arrow-ge0-and$arrow-lt$o-and$semi-lt$o){return $Text.Substring($a,$semi+1-$a)};$d=1;$e=$o+1;while($d-gt0-and$e-lt$Text.Length){if($Text[$e]-eq'{'){$d++};if($Text[$e]-eq'}'){$d--};$e++};if($d-ne0){throw 'Unbalanced actual method'};$Text.Substring($a,$e-$a)}
if(-not$ScenarioSource){$ScenarioSource=Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7PersistenceScenario.SaveBoundary.cs'}
$text=[IO.File]::ReadAllText($ScenarioSource)
$methods=''
foreach($signature in @('private void BeginSaveBoundaryObservation()','private void RequireSaveBoundaryAuthority()','private void RequireHealthySaveOwner()','private void BeginWorkerCancelSave()','private bool UpdateSaveBoundary()','private void RequirePendingSaveWait()','private void RequireWorkerCustody()','private void CompleteSaveBoundary(','private void ReleaseSaveBoundaryHold()','private void EndSaveBoundaryObservation()')){$methods+=[Environment]::NewLine+(Get-ActualBoundaryMethod $text $signature)}
if($BeforeGuardRepairs){$original=Get-ActualBoundaryMethod $text 'private void BeginPendingMountSave()';$begin=$original.IndexOf('            Chunk7NativeFixture.Select(secondRider);');if($begin-lt0){throw 'Pre-repair exact native-input body absent'};$methods+=[Environment]::NewLine+'private void IssuePendingApproach(){'+$original.Substring($begin)}else{$methods+=[Environment]::NewLine+(Get-ActualBoundaryMethod $text 'private void IssuePendingApproach()')}
$probeSource=Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7SaveBoundaryProbe.cs'
$probeText=[IO.File]::ReadAllText($probeSource)
$probeMethods=(Get-ActualBoundaryMethod $probeText 'internal void Install()')+[Environment]::NewLine+(Get-ActualBoundaryMethod $probeText 'public void Dispose()')
$nativeSource=Join-Path $repo 'src/KingmakerMountedCombat/Integration/NativeDeferredSave.cs'
$patchSource=Join-Path $repo 'src/KingmakerMountedCombat/Integration/MountedPatchController.cs'
$deferSource=Join-Path $repo 'src/KingmakerMountedCombat/Integration/MountedDeferredSave.cs'
$patchText=[IO.File]::ReadAllText($patchSource)
$patchMethods=(Get-ActualBoundaryMethod $patchText 'internal static bool SaveCommandStartPrefix(')+[Environment]::NewLine+(Get-ActualBoundaryMethod $patchText 'internal static void DeferredSaveLoadingPostfix(')
$adapter=[IO.File]::ReadAllText((Join-Path $chunk7TestSource 'SaveBoundaryAdapterTemplate.cs')).Replace('__METHODS__',$methods).Replace('__PROBE_METHODS__',$probeMethods).Replace('__PATCH_METHODS__',$patchMethods).Replace('__START_SCREEN__',(Get-ActualBoundaryMethod ([IO.File]::ReadAllText($nativeSource)) 'internal static void StartScreen(')).Replace('__DEFER_METHOD__',(Get-ActualBoundaryMethod ([IO.File]::ReadAllText($deferSource)) 'private IEnumerator<object> DeferNativeSave('))
$probe=Join-Path $chunk7AnalysisRoot ('save-boundary-adapter-'+[Guid]::NewGuid().ToString('N'));[void][IO.Directory]::CreateDirectory($probe)
[IO.File]::WriteAllText((Join-Path $probe 'SaveBoundaryAdapter.cs'),$adapter)
[ordered]@{scenarioSha256=(Get-FileHash -LiteralPath $ScenarioSource -Algorithm SHA256).Hash.ToLowerInvariant();probeSha256=(Get-FileHash -LiteralPath $probeSource -Algorithm SHA256).Hash.ToLowerInvariant();nativeDeferredSha256=(Get-FileHash -LiteralPath $nativeSource -Algorithm SHA256).Hash.ToLowerInvariant();patchSha256=(Get-FileHash -LiteralPath $patchSource -Algorithm SHA256).Hash.ToLowerInvariant();deferSha256=(Get-FileHash -LiteralPath $deferSource -Algorithm SHA256).Hash.ToLowerInvariant();actualMethods=$true;nativeEvidence=$false}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $probe 'source-receipt.json')
$vs=Join-Path ([Environment]::GetFolderPath('ProgramFilesX86')) 'Microsoft Visual Studio/Installer/vswhere.exe'
$compiler=@(& $vs -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild/**/Bin/Roslyn/csc.exe')|Select-Object -First 1
[xml]$paths=[IO.File]::ReadAllText((Join-Path $repo 'LocalGamePaths.props'))
$install=$paths.SelectSingleNode('//*[local-name()="KingmakerInstallDir"]').InnerText
$managed=$paths.SelectSingleNode('//*[local-name()="KingmakerManagedDir"]').InnerText.Replace('$(KingmakerInstallDir)',$install)
$json=Join-Path $managed 'Newtonsoft.Json.dll'
if(-not(Test-Path -LiteralPath $json)){throw 'Installed JSON contract absent'}
$exe=Join-Path $probe 'SaveBoundaryAdapter.exe'
& $compiler /nologo /target:exe /langversion:7.3 /nowarn:0649 /reference:Microsoft.CSharp.dll "/reference:$json" "/out:$exe" (Join-Path $probe 'SaveBoundaryAdapter.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/RetainedObjectIdentity.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Domain/DeferredSaveEnumerator.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Domain/ScopedEnumerator.cs')
if($LASTEXITCODE-ne0){throw 'Actual save-boundary adapter compilation failed.'}
Copy-Item -LiteralPath $json -Destination $probe
& $exe
if($LASTEXITCODE-ne0){throw 'Actual save-boundary custody regression failed.'}
