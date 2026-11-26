[CmdletBinding()]
param([ValidateSet('Debug','Release')][string]$Configuration = 'Release')
# Focused selection for PR-5 MMR-07 (Horse bite binding and presentation diagnostics) (child of the Mounted Melee Repair decomposition; see the mission under
# docs/missions/mmr-split/). It declares exactly these checks and nothing broader (docs/VALIDATION-POLICY.md 3):
#   1. Test-MmrHorseBiteNativeContract.ps1: the native handle SpeedScale the strike line reports and the pinned identity of the
#      native horse animation bundle.
#   2. Test-MountedAlpha2BiteContract.ps1: MMR-07 (review R1-F13) the production Horse Bite handle binding and strike reader on
#      genuine Kingmaker handle/action/manager objects, and the compiled postfix/OnAction wiring.
#   3. Test-MountedPresentationContracts.ps1: the paired presentation seams beside the attack animation path.
#   Regression neighbors (preserved behavior): Test-MountedShieldNativeContract.ps1 and Test-MountedShieldSelectionRollback.ps1
#   (Mounted Shield registration, amounts, AC/check writes, publication, embedded icon identity); Test-MountedArcheryFeatContract.ps1;
#   chunk7\Test-Chunk7RangerSelectionContract.ps1 (Horse/Riding Dog selection); chunk7\Test-Chunk7ReplaySubscriberContract.ps1
#   (subscriber inventory and version); Test-FinalizeF1LocalizationKeys.ps1; loader baseline Test-UmmBuildReference.ps1,
#   Test-Chunk7NativeLoaderContracts.ps1, Test-FrozenProductSource.ps1, Test-UmmLoaderContracts.ps1 (genuine 0.31.1 reference,
#   installed 0.33.0 loader).
# Known baseline failures on main, not selected: Test-MountedAttackStallRepair.ps1 and Test-WorldOwnershipContracts.ps1.
# The Fast tier runs the component tests and Validate-Source itself. None of this is native gameplay or visual evidence.
$ErrorActionPreference='Stop'
$failed=New-Object 'System.Collections.Generic.List[string]'
# A PowerShell 7 parent leaves its own module directories in PSModulePath; a Windows PowerShell 5.1 grandchild then
# cannot autoload its built-in Get-FileHash. Each child computes its own default module path instead.
$env:PSModulePath=$null
$repoRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$reference=Join-Path $repoRoot 'obj\references\umm-0.31.1\UnityModManager.dll'
$children=@(
    @('Test-MmrHorseBiteNativeContract.ps1'),@('Test-MountedAlpha2BiteContract.ps1'),@('Test-MountedPresentationContracts.ps1'),
    @('Test-MountedShieldNativeContract.ps1'),@('Test-MountedShieldSelectionRollback.ps1'),@('Test-MountedArcheryFeatContract.ps1'),
    @('chunk7\Test-Chunk7RangerSelectionContract.ps1'),@('chunk7\Test-Chunk7ReplaySubscriberContract.ps1'),
    @('Test-FinalizeF1LocalizationKeys.ps1'),
    @('Test-UmmBuildReference.ps1'),@('Test-Chunk7NativeLoaderContracts.ps1'),@('Test-FrozenProductSource.ps1'),
    @('Test-UmmLoaderContracts.ps1','-Reference',$reference,'-Profile','umm-0.31.1','-IgnoreLocalProfile'),
    @('Test-UmmLoaderContracts.ps1','-Profile','umm-0.33.0','-IgnoreLocalProfile'))
# PR-5 review R1-F8: the tier's Configuration reaches every child that declares one (the same rule Test-Tiers applies to this
# wrapper), so product-reading children inspect the selected build. The routing is checked for both configurations before
# any child runs: every declaring child receives exactly that configuration, and each product-reading child resolves its
# product under bin/<Configuration>.
function Get-ChildArguments($Child, [string]$Selected) {
    $path = Join-Path $PSScriptRoot $Child[0]
    $arguments = @('-NoLogo','-NoProfile','-ExecutionPolicy','Bypass','-File',$path)+@($Child | Select-Object -Skip 1)
    if ((Get-Command $path).Parameters.ContainsKey('Configuration')) { $arguments += @('-Configuration',$Selected) }
    ,$arguments
}
$productReaders=@('Test-MmrHorseBiteNativeContract.ps1','Test-MountedAlpha2BiteContract.ps1','Test-MountedPresentationContracts.ps1',
    'Test-MountedShieldNativeContract.ps1','Test-MountedShieldSelectionRollback.ps1','Test-MountedArcheryFeatContract.ps1',
    'chunk7\Test-Chunk7ReplaySubscriberContract.ps1','Test-UmmLoaderContracts.ps1')
foreach($probe in @('Debug','Release')){
    $routed=0
    foreach($child in $children){
        $arguments=Get-ChildArguments $child $probe
        $declares=(Get-Command (Join-Path $PSScriptRoot $child[0])).Parameters.ContainsKey('Configuration')
        $given=@(for($i=0;$i -lt $arguments.Count-1;$i++){ if($arguments[$i] -ceq '-Configuration'){ $arguments[$i+1] } })
        if($declares -ne ($given.Count -eq 1) -or ($declares -and $given[0] -cne $probe)){ throw ('Configuration routing: '+$child[0]+' received '+($given -join ',')+' for '+$probe) }
        if($productReaders -contains $child[0]){
            if(-not $declares){ throw ('Configuration routing: product reader '+$child[0]+' declares no Configuration') }
            $text=Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot $child[0])
            if(-not ($text.Contains("bin\' + `$Configuration + '\KingmakerMountedCombat.dll") -or $text.Contains('bin/$Configuration/KingmakerMountedCombat.dll') -or $text.Contains('Get-KmcUmmProductAssembly $repoRoot $Configuration'))){
                throw ('Configuration routing: '+$child[0]+' does not resolve its product from its Configuration') }
            $routed++
        }
    }
    Write-Host ('MMR-HORSE-BITE ROUTING '+$probe+': '+$routed+' product-reading child invocations receive -Configuration '+$probe+' and resolve bin\'+$probe+'\KingmakerMountedCombat.dll')
}
Write-Host ('MMR-HORSE-BITE CONFIGURATION '+$Configuration)
foreach($child in $children){
    $label=($child -join ' ')
    Write-Host ('MMR-HORSE-BITE CHILD '+$label)
    $arguments=Get-ChildArguments $child $Configuration
    & powershell.exe @arguments
    if($LASTEXITCODE -ne 0){$failed.Add($label)}
}
if($failed.Count -ne 0){Write-Host ('MMR-HORSE-BITE FOCUSED FAIL: '+($failed -join ', ')+' nativeProof=false');exit 1}
Write-Host ('MMR-HORSE-BITE FOCUSED PASS ('+$children.Count+' of '+$children.Count+' children) nativeProof=false')
