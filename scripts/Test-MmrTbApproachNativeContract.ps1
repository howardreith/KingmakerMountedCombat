[CmdletBinding()]
param([string]$Configuration = 'Release')
# Mounted Combat alpha 2 (docs/missions/mounted-alpha2-melee-pose/MISSION.md), MMR-02 turn-based approach: installed-contract and
# production-path checks, split by area from scripts/Test-MountedAlpha2NativeContract.ps1 at 46444b30 (PR #64, not accepted);
# every check line and label below is that script's, verbatim, with the shared preamble.
# MMR-02: the installed TurnController.IgnoreClick members the turn-based click admission binds (prediction flags,
# highlighted unit, prediction commands, PathVisualizer.IsCurrentPathForPoint) and the product's IgnoreClick patches.
# Not covered: any game run.
# chain, and a saved horse's actual facts on load.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$lab = [IO.Path]::GetFullPath((Join-Path $repoRoot '..\..'))
$layout = (Get-Content -Raw -LiteralPath (Join-Path $lab 'environment-intake.json') | ConvertFrom-Json).requestedLayout
$data = Join-Path $layout.kingmakerInstallDir 'Kingmaker_Data'
$managed = Join-Path $data 'Managed'
$productDll = Join-Path $repoRoot ('bin\' + $Configuration + '\KingmakerMountedCombat.dll')
$script:passed = 0
function Check([bool]$Value, [string]$Label) { if (-not $Value) { throw ('Mounted alpha 2 native contract: ' + $Label) }; $script:passed++; Write-Host ('PASS ' + $Label) }
Check (Test-Path -LiteralPath $productDll -PathType Leaf) ('built product present: ' + $productDll)
$script:resolving = @{}
[AppDomain]::CurrentDomain.add_AssemblyResolve({ param($sender, $eventArgs)
    $short = ($eventArgs.Name -split ',')[0]
    if ($short.EndsWith('.resources') -or $script:resolving.ContainsKey($short)) { return $null }
    $loaded = @([AppDomain]::CurrentDomain.GetAssemblies() | Where-Object { $_.GetName().Name -eq $short }) | Select-Object -First 1
    if ($loaded) { return $loaded }
    $script:resolving[$short] = $true
    try {
        foreach ($dir in @($managed, (Join-Path $managed 'UnityModManager'))) { $path = Join-Path $dir ($short + '.dll'); if (Test-Path -LiteralPath $path) { return [Reflection.Assembly]::LoadFile($path) } }
        return $null
    } finally { $script:resolving.Remove($short) } })
$game = [Reflection.Assembly]::LoadFrom((Join-Path $managed 'Assembly-CSharp.dll'))
$kmc = [Reflection.Assembly]::LoadFile($productDll)
function GameType([string]$Name) { $game.GetType($Name, $true) }
function KmcType([string]$Name) { $kmc.GetType($Name, $true) }
function New-Native([Type]$Type) { [Runtime.Serialization.FormatterServices]::GetUninitializedObject($Type) }
$any = [Reflection.BindingFlags]'Instance,Static,Public,NonPublic'
function SetField($Target, [Type]$Type, [string]$Field, $Value) {
    $info = $null; $t = $Type
    while ($null -ne $t -and $null -eq $info) { $info = $t.GetField($Field, [Reflection.BindingFlags]'Instance,Public,NonPublic,DeclaredOnly'); $t = $t.BaseType }
    if ($null -eq $info) { throw ('Missing native field ' + $Type.FullName + '.' + $Field) }
    $info.SetValue($Target, $Value)
}
$module = $game.ManifestModule
function MemberAt([int]$Token) { $module.ResolveMember($Token) }
function IlTokens([Reflection.MethodBase]$Method) { $il = $Method.GetMethodBody().GetILAsByteArray(); $set = @{}; for ($i = 0; $i -le $il.Length - 4; $i++) { $set[[BitConverter]::ToInt32($il, $i)] = $true }; $set }
$ignore = MemberAt 0x06000C2F
Check ($ignore.Name -ceq 'IgnoreClick' -and $ignore.DeclaringType.FullName -ceq 'TurnBased.Controllers.TurnController') 'MMR-02: 06000C2F is TurnController.IgnoreClick (the patched method)'
$need = MemberAt 0x04000691; $highlight = MemberAt 0x0400068B
Check ($need.Name -ceq 'm_NeedNewPredictions' -and $need.FieldType -eq [bool] -and $need.DeclaringType -eq $ignore.DeclaringType) 'MMR-02: 04000691 is TurnController.m_NeedNewPredictions (bool)'
Check ($highlight.Name -ceq 'm_HighlightedUnit' -and $highlight.DeclaringType -eq $ignore.DeclaringType) 'MMR-02: 0400068B is TurnController.m_HighlightedUnit'
$prediction = MemberAt 0x0400066E
Check ($prediction.Name -ceq 'm_PredictionCommands' -and $prediction.FieldType.FullName -ceq 'Kingmaker.UnitLogic.Commands.UnitCommands' -and $prediction.DeclaringType -eq $ignore.DeclaringType) 'MMR-02: 0400066E is TurnController.m_PredictionCommands (the simulated click''s commands)'
$pathCheck = MemberAt 0x06007018
Check ($pathCheck.Name -ceq 'IsCurrentPathForPoint' -and $pathCheck.DeclaringType.Name -ceq 'PathVisualizer') 'MMR-02: 06007018 is PathVisualizer.IsCurrentPathForPoint'
$ignoreTokens = IlTokens $ignore
Check ($ignoreTokens.ContainsKey(0x06007018)) 'MMR-02: native IgnoreClick drops a click whose visualized path does not end within the predicted approach radius'
$patches = KmcType 'KingmakerMountedCombat.Integration.MountedPatchController+PatchMethods'
Check ($null -ne $patches.GetMethod('PairedIgnoreClickPostfix', $any) -and $null -ne $patches.GetMethod('PairedIgnoreClickPrefix', $any)) 'MMR-02: the product carries the IgnoreClick prefix and the read-only drop-diagnostic postfix'
# ---- PR-2 (review R7-F4): the installed movement-limit contract -----------------------------------------------------------
$turnType = GameType 'TurnBased.Controllers.TurnController'
$select = $turnType.GetMethod('TrySelectMovementLimit', $any)
$selectIl = $select.GetMethodBody().GetILAsByteArray(); $clearsSingle = $false
for ($i = 0; $i -le $selectIl.Length - 6; $i++) { if ($selectIl[$i] -eq 0x16 -and $selectIl[$i + 1] -eq 0x28) { try { if ($select.Module.ResolveMethod([BitConverter]::ToInt32($selectIl, $i + 2)).Name -ceq 'SetMovementLimit') { $clearsSingle = $true } } catch { } } }
$usedStandard = (GameType 'Kingmaker.EntitySystem.Entities.UnitEntityData').GetMethod('UsedStandardAction', [Type[]]@())
$selectRefs = IlTokens $select
Check ($clearsSingle -and $selectRefs.ContainsKey($usedStandard.MetadataToken) -or ($clearsSingle -and @(0..($selectIl.Length - 5) | Where-Object { $selectIl[$_] -eq 0x6F -and $(try { $select.Module.ResolveMethod([BitConverter]::ToInt32($selectIl, $_ + 1)) -eq $usedStandard } catch { $false }) }).Count -ge 1)) 'MMR-02 (R7-F4): installed TurnController.TrySelectMovementLimit reads UsedStandardAction and sets the movement limit back to None (single-action limit cleared once the Standard is used)'
$remainingMove = $turnType.GetMethod('GetRemainingMovementTime', $any)
$cooldownMove = (GameType 'Kingmaker.Controllers.Combat.UnitCombatState').GetField('Cooldown').FieldType.GetProperty('MoveAction').GetGetMethod()
$remainingIl = $remainingMove.GetMethodBody().GetILAsByteArray(); $readsCooldown = $false
for ($i = 0; $i -le $remainingIl.Length - 5; $i++) { if ($remainingIl[$i] -eq 0x6F) { try { if ($remainingMove.Module.ResolveMethod([BitConverter]::ToInt32($remainingIl, $i + 1)) -eq $cooldownMove) { $readsCooldown = $true } } catch { } } }
Check $readsCooldown 'MMR-02 (R7-F4): installed GetRemainingMovementTime ends single-action movement once the unit''s Move cooldown is positive (the rule KMC mirrors only while the limit exists)'
Write-Host ('Mounted alpha 2 native contract (approach) PASS=' + $script:passed + ' FAIL=0')
