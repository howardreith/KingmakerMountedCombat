[CmdletBinding()]
param([string]$RepositoryRoot,[string]$NativeControlSource,[string]$TemplatePath,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$NativeControlSource){$NativeControlSource=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs'}
if(-not$TemplatePath){$TemplatePath=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/NativeMoveCallbackAdapterTemplate.cs'}
if(-not$OutputDirectory){$lab=[IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot));$OutputDirectory=Join-Path $lab ('analysis-cache/chunk7-continuation/native-move-callback-'+[Guid]::NewGuid().ToString('N'))}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
function Extract([string]$Text,[string]$Signature){$start=$Text.IndexOf($Signature,[StringComparison]::Ordinal);if($start-lt0){throw ('Actual source method absent: '+$Signature)};$brace=$Text.IndexOf('{',$start);$depth=0;for($i=$brace;$i-lt$Text.Length;$i++){if($Text[$i]-eq'{'){$depth++}elseif($Text[$i]-eq'}'){$depth--;if($depth-eq0){return $Text.Substring($start,$i-$start+1)}}};throw 'Unbalanced actual source method.'}
$control=[IO.File]::ReadAllText($NativeControlSource)
$ownershipPath=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Integration/NativeMountedControlService.Ownership.cs';$ownership=[IO.File]::ReadAllText($ownershipPath)
$combatPath=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Integration/MountedCombatController.cs';$combat=[IO.File]::ReadAllText($combatPath)
$runtimePath=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Integration/KingmakerMountedPairRuntime.cs';$runtime=[IO.File]::ReadAllText($runtimePath)
$policyPath=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Domain/MountedStockAttack.cs'
$methods=[ordered]@{
 'RUNTIME_CANCEL'=(Extract $runtime 'public void CancelMountMovement()')
 'COMBAT_CLEAR_INTENT'=(Extract $combat 'private void ClearStockAttackIntent(string reason, bool countCancellation)')
 'COMBAT_CLEAR_OBSERVED'=(Extract $combat 'private void ClearObservedStockRequest()')
 'COMBAT_CLEAR_GROUND'=(Extract $combat 'private void CancelRiderTurnGroundMovement()')
 'COMBAT_CLEAR_PROJECTED'=(Extract $combat 'private void ClearProjectedNativeMountTurnGroundMove()')
 'COMBAT_CANCEL_SELECTED'=(Extract $combat 'internal void CancelSelectedInput(string reason)')
 'COMBAT_CANCEL'=(Extract $combat 'public void Cancel(string reason)')
 'VISIT_INPUT_OWNERS'=(Extract $ownership 'private void VisitInputOwners(Action<MountedCombatController> action)')
 'ON_MOVE'=(Extract $control 'public void OnMoveRequested(Vector3 target)')
 'ON_ITEM'=(Extract $control 'public void OnItemUseRequested(Kingmaker.Items.ItemEntity item, TargetWrapper target)')
}
$text=[IO.File]::ReadAllText($TemplatePath);foreach($entry in $methods.GetEnumerator()){$marker='/* '+$entry.Key+' */';if(-not$text.Contains($marker)){throw ('Template lacks '+$marker)};$text=$text.Replace($marker,$entry.Value)}
$actual=Join-Path $OutputDirectory 'actual.cs';[IO.File]::WriteAllText($actual,$text)
$exe=Join-Path $OutputDirectory 'actual.exe';$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$compile=@('/nologo','/langversion:7.3','/target:exe','/warn:0',('/out:'+$exe),$actual,$policyPath)
& $compiler @compile; if($LASTEXITCODE-ne0){throw 'Actual native move callback adapter did not compile.'}
$timer=[Diagnostics.Stopwatch]::StartNew();$lines=@(& $exe);$code=$LASTEXITCODE;$timer.Stop();$lines|Write-Host
$pass=@($lines|Where-Object{$_-cmatch'^PASS '}).Count;$fail=@($lines|Where-Object{$_-cmatch'^FAIL '}).Count
$inputs=@($NativeControlSource,$ownershipPath,$combatPath,$runtimePath,$policyPath,$TemplatePath)|ForEach-Object{[pscustomobject]@{path=$_;sha256=(Get-FileHash -LiteralPath $_).Hash.ToLowerInvariant()}}
@{passes=$pass;failures=$fail;seconds=$timer.Elapsed.TotalSeconds;nativeProof=$false;exitCode=$code;inputs=$inputs;actualSha256=(Get-FileHash -LiteralPath $actual).Hash.ToLowerInvariant()}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json')
if($code-ne0){throw 'Actual native move callback ordering regressed.'}
