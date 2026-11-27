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
# ---- PR #70 R1-F6: the actual conditional branches, as opcode sequences of the installed IL (operands resolved by name) ----------------
$script:opOne = @{}; $script:opTwo = @{}
foreach ($f in [Reflection.Emit.OpCodes].GetFields()) { $o = $f.GetValue($null); if ($o.Size -eq 1) { $script:opOne[[int]$o.Value] = $o } else { $script:opTwo[[int]($o.Value -band 0xFF)] = $o } }
function IlText([Reflection.MethodBase]$Method) {
    $il = $Method.GetMethodBody().GetILAsByteArray(); $i = 0; $out = New-Object 'System.Collections.Generic.List[string]'
    while ($i -lt $il.Length) {
        $c = $il[$i]; $i++
        if ($c -eq 0xFE) { $op = $script:opTwo[[int]$il[$i]]; $i++ } else { $op = $script:opOne[[int]$c] }
        $arg = ''
        switch ($op.OperandType.ToString()) {
            'InlineNone' { }
            'ShortInlineBrTarget' { $i += 1 }
            'InlineBrTarget' { $i += 4 }
            { $_ -in 'ShortInlineI','ShortInlineVar' } { $arg = $il[$i].ToString(); $i += 1 }
            'InlineVar' { $i += 2 }
            'InlineI' { $arg = [BitConverter]::ToInt32($il, $i).ToString(); $i += 4 }
            'InlineI8' { $i += 8 }
            'ShortInlineR' { $arg = [BitConverter]::ToSingle($il, $i).ToString('R'); $i += 4 }
            'InlineR' { $i += 8 }
            'InlineSwitch' { $n = [BitConverter]::ToInt32($il, $i); $i += 4 + 4 * $n }
            default { try { $arg = $Method.Module.ResolveMember([BitConverter]::ToInt32($il, $i)).Name } catch { $arg = '?' }; $i += 4 }
        }
        $out.Add(($op.Name + ' ' + $arg).Trim())
    }
    ';' + ($out -join ';') + ';'
}
$remainingText = IlText $remainingMove
$tickText = IlText ($turnType.GetMethod('TickMovement', $any))
$selectText = IlText $select
# GetRemainingMovementTime(bool, bool singleAction): with the single-action limit, remaining > 3 grants remaining - 3; otherwise a positive
# MoveAction grants 0, else the remaining time.
Check ($remainingText.Contains(';ldarg.2;brfalse.s;ldloc.0;ldc.r4 3;ble.un.s;ldloc.0;ldc.r4 3;sub;ret;ldarg.0;ldfld Unit;callvirt get_CombatState;ldfld Cooldown;callvirt get_MoveAction;ldc.r4 0;ble.un.s;ldc.r4 0;ret;ldloc.0;ret;')) 'PR-2 R1-F6: installed GetRemainingMovementTime single-action branch: remaining > 3 -> remaining - 3; MoveAction > 0 -> 0; else remaining'
# TickMovement: before every non-forced tick, remaining = GetRemainingMovementTime(false, EnabledSingleActionMove); a tick at or above it is
# clamped to it (ldarg.1; ldloc.0; stind.r4) and a non-positive one ends; the granted time is added to the Move cooldown.
Check ($tickText.Contains(';ldarg.0;ldc.i4.0;ldarg.0;call get_EnabledSingleActionMove;call GetRemainingMovementTime;stloc.0;ldarg.1;ldind.r4;ldloc.0;blt.un.s;') -and
    $tickText.Contains(';ldarg.1;ldloc.0;stind.r4;ldarg.1;ldind.r4;ldc.r4 0;bgt.un.s;ret;') -and
    $tickText.Contains(';callvirt get_MoveAction;ldarg.1;ldind.r4;add;callvirt set_MoveAction;')) 'PR-2 R1-F6: installed TickMovement re-reads GetRemainingMovementTime(false, EnabledSingleActionMove) before every tick, clamps the tick to it and debits the Move cooldown'
# TrySelectMovementLimit: canSingle = HasNormalMovement && MoveAction < 3 && !UsedStandardAction; a SingleAction (2) limit without canSingle
# is set back to None (0). HasNormalMovement = !ShouldRestrictNormalMovement && GetRemainingTime > 0, so a restricted Move keeps the limit.
$hasNormal = $turnType.GetMethod('HasNormalMovement', $any)
Check ($selectText.Contains(';call HasNormalMovement;brfalse.s;ldarg.0;ldfld Unit;callvirt get_CombatState;ldfld Cooldown;callvirt get_MoveAction;ldc.r4 3;bge.un.s;ldarg.0;ldfld Unit;callvirt UsedStandardAction;ldc.i4.0;ceq;br.s;ldc.i4.0;stloc.1;') -and
    $selectText.Contains(';ldfld m_MovementLimit;ldc.i4.2;bne.un.s;ldloc.1;brtrue.s;ldarg.0;ldc.i4.0;call SetMovementLimit;') -and
    (IlText $hasNormal).Contains(';ldarg.0;call ShouldRestrictNormalMovement;brtrue.s;ldarg.0;call GetRemainingTime;ldc.r4 0;cgt;ret;')) 'PR-2 R1-F6: installed TrySelectMovementLimit keeps the single-action limit only while HasNormalMovement, MoveAction < 3 and the Standard is unused; HasNormalMovement does not exclude a restricted Move'
Write-Host ('Mounted alpha 2 native contract (approach) PASS=' + $script:passed + ' FAIL=0')
