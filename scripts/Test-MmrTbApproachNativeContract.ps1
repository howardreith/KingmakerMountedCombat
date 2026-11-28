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
# PR #70 R1-F7 (round 4): the dispatch bracket. 060093BA is PointerController.Tick; it calls TurnController.IgnoreClick, and no other installed
# method does, so a proof produced by the IgnoreClick postfix inside a PointerController.Tick belongs to that dispatch.
$pointerTick = MemberAt 0x060093BA
Check ($pointerTick.Name -ceq 'Tick' -and $pointerTick.DeclaringType.FullName -ceq 'Kingmaker.Controllers.Clicks.PointerController' -and $pointerTick.GetParameters().Count -eq 0) 'PR-2 R1-F7: 060093BA is PointerController.Tick()'
Check ((IlTokens $pointerTick).ContainsKey(0x06000C2F)) 'PR-2 R1-F7: installed PointerController.Tick calls TurnController.IgnoreClick'
$ignoreCallers = @()
foreach ($t in @(try { $game.GetTypes() } catch [Reflection.ReflectionTypeLoadException] { $_.Exception.Types | Where-Object { $_ } })) {
  foreach ($m in @($t.GetMethods([Reflection.BindingFlags]'Instance,Static,Public,NonPublic,DeclaredOnly')) + @($t.GetConstructors([Reflection.BindingFlags]'Instance,Static,Public,NonPublic,DeclaredOnly'))) {
    try { $b = $m.GetMethodBody() } catch { continue }; if (-not $b) { continue }; $il = $b.GetILAsByteArray()
    for ($i = 0; $i -le $il.Length - 5; $i++) { if (($il[$i] -eq 0x28 -or $il[$i] -eq 0x6F) -and [BitConverter]::ToInt32($il, $i + 1) -eq 0x06000C2F) { $ignoreCallers += ($t.FullName + '::' + $m.Name); break } } } }
Check ($ignoreCallers.Count -eq 1 -and $ignoreCallers[0] -ceq 'Kingmaker.Controllers.Clicks.PointerController::Tick') ('PR-2 R1-F7: PointerController.Tick is the only installed caller of IgnoreClick (' + ($ignoreCallers -join ', ') + ')')
Check ($null -ne $patches.GetMethod('PointerTickTranspiler', $any) -and $null -ne $patches.GetMethod('PointerTickPrefix', $any) -and $null -ne $patches.GetMethod('PointerTickPostfix', $any)) 'PR-2 R1-F7: the product carries the PointerController.Tick try/finally dispatch transpiler'
# PR-2 R1-F7 (round 5): the product's PointerController.Tick transpiler EXECUTED under the installed Harmony 1.2 (UnityModManager\0Harmony12.dll) on
# stand-in methods: the dispatch closes (PointerTickPostfix runs) once when the transpiled body throws, and once on an early and on a normal return.
$harmony = [Reflection.Assembly]::LoadFrom((Join-Path $managed 'UnityModManager\0Harmony12.dll'))
Add-Type -TypeDefinition 'public static class KmcDispatchProbe { public static int Body; public static void Throws() { Body++; throw new System.InvalidOperationException("probe"); } public static void Returns(int x) { Body++; if (x > 0) return; Body++; } }'
$patchMethods = $kmc.GetType('KingmakerMountedCombat.Integration.MountedPatchController', $true).GetNestedType('PatchMethods', [Reflection.BindingFlags]'NonPublic')
$transpiler = $patchMethods.GetMethod('PointerTickTranspiler', [Reflection.BindingFlags]'NonPublic,Static')
$closures = $patchMethods.GetField('PointerDispatchClosures', [Reflection.BindingFlags]'NonPublic,Static')
$instance = $harmony.GetType('Harmony12.HarmonyInstance', $true).GetMethod('Create').Invoke($null, @('kmc.contract.pointer-dispatch'))
$hm = [Activator]::CreateInstance($harmony.GetType('Harmony12.HarmonyMethod', $true), @($transpiler))
foreach ($name in 'Throws', 'Returns') { [void]$instance.Patch([KmcDispatchProbe].GetMethod($name), $null, $null, $hm) }
$before = [int]$closures.GetValue($null)
$threw = $false; try { [KmcDispatchProbe]::Throws() } catch { $threw = $true }
$afterThrow = [int]$closures.GetValue($null)
[KmcDispatchProbe]::Returns(1); [KmcDispatchProbe]::Returns(0)
$afterReturns = [int]$closures.GetValue($null)
Check ($threw -and $afterThrow -eq $before + 1) ('PR-2 R1-F7: the transpiled body that throws still closes its pointer dispatch (closures ' + $before + ' -> ' + $afterThrow + ')')
Check ($afterReturns -eq $afterThrow + 2 -and [KmcDispatchProbe]::Body -eq 4) ('PR-2 R1-F7: an early return and a normal return each close it once (closures -> ' + $afterReturns + ', body ' + [KmcDispatchProbe]::Body + ')')
# ---- PR-2 R1-F6 (round 6): the installed per-frame scheduler order that fixes when a mount's first movement tick, and its duration, are known
# (docs/missions/mmr-split/PR-2-first-tick-boundary.md). Member references are resolved with their declaring types, in IL order.
function IlMembers([Reflection.MethodBase]$Method) {
    $il = $Method.GetMethodBody().GetILAsByteArray(); $i = 0; $out = New-Object 'System.Collections.Generic.List[string]'
    while ($i -lt $il.Length) {
        $c = $il[$i]; $i++
        if ($c -eq 0xFE) { $op = $script:opTwo[[int]$il[$i]]; $i++ } else { $op = $script:opOne[[int]$c] }
        switch ($op.OperandType.ToString()) {
            'InlineNone' { }
            { $_ -in 'ShortInlineBrTarget','ShortInlineI','ShortInlineVar' } { $i += 1 }
            'InlineVar' { $i += 2 }
            { $_ -in 'InlineI','InlineBrTarget','ShortInlineR','InlineString','InlineSig' } { $i += 4 }
            { $_ -in 'InlineI8','InlineR' } { $i += 8 }
            'InlineSwitch' { $n = [BitConverter]::ToInt32($il, $i); $i += 4 + 4 * $n }
            default { try { $r = $Method.Module.ResolveMember([BitConverter]::ToInt32($il, $i)); if ($r.DeclaringType) { $out.Add($op.Name + ' ' + $r.DeclaringType.Name + '.' + $r.Name) } } catch { }; $i += 4 }
        }
    }
    $out
}
$factory = GameType 'Kingmaker.GameModes.GameModesFactory'
$initRefs = @(IlMembers ($factory.GetMethod('Initialize', $any)))
function RefIndex([string]$Ref) { $at = -1; for ($k = 0; $k -lt $initRefs.Count; $k++) { if ($initRefs[$k] -ceq $Ref) { if ($at -ge 0) { return -2 }; $at = $k } }; $at }
$timeAt = RefIndex 'ldfld Game.TimeController'; $pointerAt = RefIndex 'ldfld Game.DefaultPointerController'
$moveAt = RefIndex 'newobj UnitMoveController..ctor'; $actionAt = RefIndex 'newobj UnitActionController..ctor'
Check ($timeAt -ge 0 -and $pointerAt -gt $timeAt -and $moveAt -gt $pointerAt -and $actionAt -gt $moveAt) ('PR-2 R1-F6 (round 6): installed GameModesFactory.Initialize registers TimeController (' + $timeAt + ') < DefaultPointerController (' + $pointerAt + ') < UnitMoveController (' + $moveAt + ') < UnitActionController (' + $actionAt + ')')
$createRefs = @(IlMembers ($factory.GetMethod('Create', $any))); $modeTick = (GameType 'Kingmaker.GameModes.GameMode').GetMethod('Tick', $any)
Check (($createRefs -join ';').Contains('ldsfld GameModesFactory.AllControllers;') -and ($createRefs -join ';').Contains('call Enumerable.Where;') -and ($createRefs -join ';').Contains('call Enumerable.Select;newobj GameMode..ctor') -and
    (IlText $modeTick).Contains(';ldfld m_Controllers;') -and (IlText $modeTick).Contains(';ldelem.ref;stloc.2;ldloc.2;callvirt Tick;')) 'PR-2 R1-F6 (round 6): a game mode ticks its controllers in registration order (GameModesFactory.Create filters AllControllers with order-preserving Where/Select; GameMode.Tick walks m_Controllers by index)'
$timeTick = (GameType 'Kingmaker.Controllers.TimeController').GetMethod('Tick', $any)
Check ((IlText $timeTick).Contains(';call get_deltaTime;') -and (@(IlMembers $timeTick) -join ';').Contains('call TimeController.set_DeltaTime')) 'PR-2 R1-F6 (round 6): installed TimeController.Tick sets this frame''s DeltaTime from Unity''s frame delta (it is not known before that frame''s TimeController.Tick)'
$moveTick = (GameType 'Kingmaker.Controllers.Units.UnitMoveController').GetMethod('Tick', $any)
Check ((@(IlMembers $moveTick) -join ';').Contains('ldfld Game.TimeController;callvirt TimeController.get_DeltaTime;callvirt UnitMovementAgentBase.TickMovement')) 'PR-2 R1-F6 (round 6): installed UnitMoveController.Tick passes this frame''s TimeController.DeltaTime to each moving agent''s TickMovement'
$agentType = GameType 'Kingmaker.View.UnitMovementAgent'
Check ((@(IlMembers ($agentType.GetMethod('TickMovement', $any))) -join ';').Contains('call UnitMovementAgent.CanMoveInTurnBased') -and (@(IlMembers ($agentType.GetMethod('CanMoveInTurnBased', $any))) -join ';').Contains('callvirt TurnController.TickMovement')) 'PR-2 R1-F6 (round 6): UnitMovementAgent.TickMovement reaches CanMoveInTurnBased (the product''s MountMovementPrefix, then TickDelegated) and TurnController.TickMovement with that delta'
$startCallers = @()
foreach ($t in @(try { $game.GetTypes() } catch [Reflection.ReflectionTypeLoadException] { $_.Exception.Types | Where-Object { $_ } })) {
  if ($t.Namespace -cne 'Kingmaker.Controllers.Units') { continue }
  foreach ($m in @($t.GetMethods([Reflection.BindingFlags]'Instance,Static,Public,NonPublic,DeclaredOnly'))) { try { if (((@(IlMembers $m) -join ';') + ';') -match '(call|callvirt) UnitCommand\.Start;') { $startCallers += ($t.Name + '::' + $m.Name) } } catch { } } }
Check ($startCallers -contains 'UnitActionController::TickCommand') ('PR-2 R1-F6 (round 6): commands (an order''s wrapper and its carrier) start in UnitActionController.TickCommand, AFTER UnitMoveController in the frame, so a carrier issued by a click moves first in a LATER frame whose DeltaTime is unknown at the click (' + ($startCallers -join ', ') + ')')
$participantType = KmcType 'KingmakerMountedCombat.Integration.MountedPairTurnParticipant'
$adapterType = KmcType 'KingmakerMountedCombat.Integration.MountedMovementStateAdapter'
$controllerType = KmcType 'KingmakerMountedCombat.Integration.MountedCombatController'
Check ($null -eq $participantType.GetMethod('ExcludeFirstTickOnlySingleMove', $any) -and $null -eq $adapterType.GetMethod('SingleMoveTruncatesRestrictedMove', $any) -and
    $null -ne $adapterType.GetField('FirstTickGate', $any) -and $null -ne $controllerType.GetMethod('DecideFirstTickProposal', $any) -and $null -ne $controllerType.GetMethod('SweepFirstTickProposal', $any)) 'PR-2 R1-F6 (round 6): the product decides a restricted single-action replacement at its own carrier''s first TickDelegated (MountedMovementStateAdapter.FirstTickGate, MountedCombatController.DecideFirstTickProposal/SweepFirstTickProposal) and no longer rewrites the selected single-action limit (ExcludeFirstTickOnlySingleMove and SingleMoveTruncatesRestrictedMove absent)'
# ---- PR-2 R1-F6 / R5-F3 (round 7): the installed turn-phase progression the provisional order relies on ----------------------------------
# A provisional order is the RIDER's own pair command: installed TurnController.Tick moves a Preparing turn to Acting only when IsActed(), when
# its own m_Commands (the turn unit's command container) is not Empty, or when the unit cannot act; a bare mount UnitMoveTo is in none of these.
$turnStatus = $turnType.GetNestedType('TurnStatus', $any)
Check ([int][Enum]::Parse($turnStatus, 'Preparing') -eq 2 -and [int][Enum]::Parse($turnStatus, 'Acting') -eq 3) 'PR-2 R1-F6 (round 7): installed TurnController.TurnStatus has Preparing = 2 and Acting = 3'
$isActing = $turnType.GetProperty('IsActing', $any).GetGetMethod($true)
Check ($isActing.MetadataToken -eq 0x06000C24 -and (IlText $isActing) -ceq ';ldarg.0;call get_Status;ldc.i4.3;ceq;ret;') 'PR-2 R5-F3 (round 7): installed TurnController.get_IsActing (06000C24) is exactly Status == Acting (an Acting turn whose IsActing is false cannot exist)'
$turnTick = $turnType.GetMethod('Tick', [Reflection.BindingFlags]'Instance,Public,NonPublic,DeclaredOnly', $null, [Type[]]@(), $null)
Check ($turnTick.MetadataToken -eq 0x06000C34 -and (IlText $turnTick).Contains(';ldarg.0;call get_Status;ldc.i4.2;bne.un.s;ldarg.0;call IsActed;brtrue.s;ldarg.0;ldfld m_Commands;callvirt get_Empty;brfalse.s;ldarg.0;ldfld Unit;callvirt IsAbleToAct;brtrue.s;ldarg.0;ldc.i4.3;call set_Status;')) 'PR-2 R1-F6 (round 7): installed TurnController.Tick (06000C34) sets a Preparing turn Acting only when IsActed(), its m_Commands is not Empty, or its unit is not IsAbleToAct'
$turnCtor = @($turnType.GetConstructors($any) | Where-Object { $_.GetParameters().Count -eq 1 })[0]
Check ($null -ne $turnCtor -and (IlText $turnCtor).Contains(';ldarg.0;ldarg.1;callvirt get_Commands;stfld m_Commands;')) 'PR-2 R1-F6 (round 7): installed TurnController(UnitEntityData) sets m_Commands to that unit''s own Commands (the rider''s container, not the mount''s)'
$commandType = KmcType 'KingmakerMountedCombat.Integration.MountedPairAttackCommand'
$approachType = KmcType 'KingmakerMountedCombat.Integration.PairedAttackClickProbe+Approach'
Check ($null -ne $controllerType.GetMethod('BeginProvisionalOrder', $any) -and $null -ne $controllerType.GetMethod('CommitAcceptedProvisional', $any) -and
    $null -ne $controllerType.GetMethod('DefersClickTargetAssignment', $any) -and $null -ne $controllerType.GetMethod('BindFirstTickCarrier', $any) -and
    $null -eq $controllerType.GetMethod('BeginFirstTickProposal', $any) -and $null -ne $commandType.GetField('FirstTickCarrierIssued', $any) -and
    $null -ne $commandType.GetField('ProvisionalStrikeGate', $any) -and $null -ne $approachType.GetField('FirstTick', $any)) 'PR-2 R1-F6 (round 7): the provisional request is a rider pair command (BeginProvisionalOrder/CommitAcceptedProvisional), its gate is bound by its own carrier creation (FirstTickCarrierIssued -> BindFirstTickCarrier; Approach.FirstTick), the click''s native assignment is deferred (DefersClickTargetAssignment), and round 6''s bare mount carrier (BeginFirstTickProposal) is absent'
# ---- PR-2 round 8 (R6-F2, R7-F2): the first restricted tick is judged on the movement agent's own steering state ------------------------------
$steering = KmcType 'KingmakerMountedCombat.Integration.MountedAgentSteering'
$nextIndex = $steering.GetField('NextIndex', $any).GetValue($null); $nextWaypoint = $steering.GetField('NextWaypoint', $any).GetValue($null)
Check ($null -ne $nextIndex -and $null -ne $nextWaypoint -and $nextIndex.DeclaringType.FullName -ceq 'Kingmaker.View.UnitMovementAgent' -and $nextIndex.MetadataToken -eq 0x04001193 -and
    $nextWaypoint.MetadataToken -eq 0x04001196 -and $nextWaypoint.FieldType.FullName -ceq 'UnityEngine.Vector2' -and $null -ne $agentType.GetProperty('Path', $any)) 'PR-2 R6-F2 (round 8): the product reads the installed agent''s own steering (UnitMovementAgent.Path, m_NextPointIndex 04001193, m_NextWaypoint 04001196 as Vector2) for the first restricted tick'
$gateType = KmcType 'KingmakerMountedCombat.Domain.IMountedFirstTickGate'
$decideParameters = @($gateType.GetMethod('Decide').GetParameters())
Check ($decideParameters.Count -eq 8 -and $decideParameters[0].Name -ceq 'movementContext' -and $decideParameters[1].Name -ceq 'allowedTick' -and $decideParameters[3].Name -ceq 'speed' -and
    $decideParameters[4].Name -ceq 'steering' -and $decideParameters[4].ParameterType.Name -ceq 'MountedSteeringFacts' -and $decideParameters[5].Name -ceq 'stepTravelLeft' -and
    $decideParameters[6].IsOut -and $decideParameters[6].Name -ceq 'approvedTick' -and $null -ne $commandType.GetMethod('LivePlanDisagreement', $any)) 'PR-2 R6-F2/R7-F2/R8-F5 (rounds 8-9): a first-tick decision receives the executing movement context, a commit states its granted metres (the adapter shortens the tick to them), and the live wrapper''s three-plan comparison is asked before any commit'
# ---- PR-2 R8-F6 (round 9): the installed shift of one turn-based movement tick, from the movement hook to the applied move -------------------
$moveText = IlText ($agentType.GetMethod('TickMovement', [Reflection.BindingFlags]'Instance,Public,NonPublic,DeclaredOnly'))
Check ($moveText.Contains(';call CanMoveInTurnBased;brtrue.s;ret;ldarg.0;ldfld m_WarmupTime;ldc.r4 0;ble.un.s;ldarg.0;ldarg.0;ldfld m_WarmupTime;ldarg.1;sub;stfld m_WarmupTime;ret;')) 'PR-2 R8-F6 (round 9): installed TickMovement returns without moving while m_WarmupTime > 0 after the movement hook (TickDelegated sets it to 0 for an allowed tick)'
Check ($moveText.Contains(';call get_MaxSpeed;stloc.s 7;') -and $moveText.Contains(';ldloc.s 10;ldloc.s 7;ldarg.0;ldfld m_MinSpeed;mul;bge.un.s;ldloc.s 7;ldarg.0;ldfld m_MinSpeed;mul;stloc.s 10;ldarg.0;ldfld m_SlowDownTime;ldc.r4 0;ble.un.s;')) 'PR-2 R8-F6 (round 9): installed TickMovement speed is MaxSpeed, raised to at least MaxSpeed x m_MinSpeed (TickDelegated: 1) and cut by m_SlowDownTime only while positive (TickDelegated: 0)'
Check ($moveText.Contains(';call IsInTurnBasedCombat;brtrue.s;ldarg.0;ldloca.s 9;ldloca.s 10;ldloca.s 12;call UpdateAvoidance;')) 'PR-2 R8-F6 (round 9): installed TickMovement skips UpdateAvoidance in turn-based combat'
Check ($moveText.Contains(';call Angle;ldarg.1;ldarg.0;ldfld m_AngularSpeed;mul;ble.un.s;ldarg.0;ldloca.s 10;call SlowDown;')) 'PR-2 R8-F6 (round 9): installed TickMovement slows only when the move direction must turn more than this tick''s angular step (the decision refuses a turning agent)'
Check ($moveText.Contains(';ldarg.0;ldloc.s 10;stfld m_Speed;ldarg.0;ldloc.s 5;call To3D;ldarg.0;ldfld m_Speed;call op_Multiply;stfld m_NextVelocity;ldarg.0;ldarg.0;ldfld m_NextVelocity;ldarg.1;call op_Multiply;call Move;')) 'PR-2 R8-F6 (round 9): installed TickMovement applies the shift MoveDirection x speed x the delta the movement hook returned'
$speedProperty = $steering.GetField('MaxSpeed', $any).GetValue($null)
Check ($null -ne $speedProperty -and $speedProperty.PropertyType -eq [float] -and $speedProperty.DeclaringType.FullName -ceq 'Kingmaker.View.UnitMovementAgentBase' -and
    (IlText ($speedProperty.GetGetMethod($true))).Contains(';call get_MaxSpeedOverride;') -and (IlText ($speedProperty.GetGetMethod($true))).Contains(';call get_EntityData;callvirt get_CurrentSpeedMps;ret;') -and
    $null -ne $agentType.GetProperty('MoveDirection', $any)) 'PR-2 R8-F6 (round 9): the product reads the installed agent''s MaxSpeed (UnitMovementAgentBase: MaxSpeedOverride, else the unit''s CurrentSpeedMps) and MoveDirection for the restricted tick''s physical travel'
# ---- PR-2 R8-F6 / R9-F3 (round 10): the installed steering at the delta returned to native movement, and the native step ledger --------------
Check ($moveText.Contains(';ldarg.0;ldfld m_IsInForceMode;brtrue.s;ldarg.0;ldfld m_FollowPrecomputedPath;brfalse.s;ldarg.0;call get_OnLastSegment;brtrue.s;ldloc.s 6;ldarg.0;ldfld m_Speed;ldarg.1;mul;blt.s;')) 'PR-2 R8-F6 (round 10): installed TickMovement skips to the next waypoint while (not forced, m_FollowPrecomputedPath, not last) its distance is below m_Speed x the returned delta'
Check ($moveText.Contains(';ldloc.s 5;ldloc.s 9;ldc.r4 0.5;call Lerp;stloc.s 9;ldarg.0;call get_CombatMode;brtrue.s;ldarg.0;ldfld m_AngularSpeed;br.s;ldarg.0;ldfld m_CombatAngularSpeed;stloc.s 19;')) 'PR-2 R8-F6 (round 10): installed TickMovement (no first-tick snap in combat) steers toward Lerp(MoveDirection, toWaypoint, 0.5) at m_CombatAngularSpeed in combat'
Check ($moveText.Contains(';ldloc.s 20;ldc.r4 2;mul;ldloc.s 8;ble.un.s;ldloc.s 19;ldc.r4 2;ldloc.s 20;ldloc.s 8;div;mul;mul;stloc.s 19;') -and
    $moveText.Contains(';ldloc.s 19;ldarg.1;mul;ldc.r4 0.0174532924;mul;ldc.r4 1;call RotateTowards;')) 'PR-2 R8-F6 (round 10): installed TickMovement raises the angular speed on the last segment when the turn would outlast the arrival, then RotateTowards(MoveDirection, target, angular x returned delta x Deg2Rad, 1)'
Check ($moveText.Contains(';ldloc.s 14;ldarg.0;ldfld m_NextWaypoint;call Distance;stloc.s 21;ldloc.s 18;ldloc.s 21;ldc.r4 0.05;clt;or;') -and
    $moveText.Contains(';call Last;ldarg.0;call get_transform;callvirt get_position;call op_Subtraction;call Move;ldarg.0;call CompleteMovement;')) 'PR-2 R8-F6 (round 10): installed TickMovement, on the last segment, completes a waypoint passed or within 0.05 m by moving onto the path''s end in turn-based combat'
$slowText = IlText ($agentType.GetMethod('SlowDown', $any))
$combatModeText = IlText ($agentType.BaseType.GetProperty('CombatMode', $any).GetGetMethod($true))
Check ($slowText.StartsWith(';call IsInTurnBasedCombat;brfalse.s;') -and $slowText.Contains(';call get_IsInCombat;newobj .ctor;stloc.0;ldloca.s 0;call get_HasValue;brtrue.s;ldc.i4.0;br.s;ldloca.s 0;call GetValueOrDefault;brfalse.s;ret;') -and
    $combatModeText.Contains(';call get_EntityData;callvirt get_IsInCombat;ret;')) 'PR-2 R8-F6 (round 10): installed SlowDown returns at once for a unit in turn-based combat; CombatMode is the unit''s IsInCombat'
Check ($tickText.Contains(';call get_MetersMovedByFiveFootStep;ldarg.1;ldind.r4;ldarg.0;ldfld Unit;callvirt get_CurrentSpeedMps;mul;add;call set_MetersMovedByFiveFootStep;')) 'PR-2 R9-F3 (rounds 10-11): installed TurnController.TickMovement counts a step as delta x the unit''s CurrentSpeedMps (nominal); round 11: the paired allocation keeps its step counter in that nominal unit and the physical step travel (the steered displacement) separately'
$probeType = KmcType 'KingmakerMountedCombat.Integration.PairedAttackClickProbe'
Check ($null -ne $steering.GetField('FollowPrecomputed', $any).GetValue($null) -and $null -ne $steering.GetField('ForceMode', $any).GetValue($null) -and
    $null -ne $steering.GetField('CombatAngularSpeed', $any).GetValue($null) -and $null -ne $steering.GetField('CurrentSpeed', $any).GetValue($null) -and
    $null -ne $probeType.GetMethod('EvaluateFirstTickNative', $any) -and @($probeType.GetMethods($any) | Where-Object { $_.Name -eq 'NativeCombatShift' }).Count -eq 2 -and $null -eq $controllerType.GetField('FirstTickAlignedDegrees', $any)) 'PR-2 R8-F6 (round 10): the product reads the installed steering facts (m_FollowPrecomputedPath, m_IsInForceMode, m_CombatAngularSpeed, m_Speed) and predicts the shift with the installed rule (EvaluateFirstTickNative, NativeCombatShift); the fixed one-degree cutoff is gone'
# ---- PR-2 round 11 (R8-F1, R9-F3, R10-F2): the product's movement units, first-debit route and final-tick input ----------------------------------
$stateType = KmcType 'KingmakerMountedCombat.Domain.MountedMovementState'
$adapterType = KmcType 'KingmakerMountedCombat.Integration.MountedMovementStateAdapter'
$approachType = KmcType 'KingmakerMountedCombat.Integration.PairedAttackClickProbe+Approach'
$advanceNominal = $stateType.GetMethod('Advance', [Type[]]@([single], [single], [single], [single], [single], [bool], [bool], [bool], [bool], [bool], [bool], [bool], [single].MakeByRefType()))
Check ($null -ne $stateType.GetField('MetresStepTravelled') -and $null -ne $advanceNominal -and $null -ne $adapterType.GetMethod('NominalSpeedOf', $any)) 'PR-2 R9-F3 (round 11): the paired step allocation is bounded by both the installed counter (nominal metres = CurrentSpeedMps x time, NominalSpeedOf) and the physical step travel (MetresStepTravelled, settled to the steered displacement)'
Check ($null -ne $approachType.GetField('NativeRoute') -and $null -ne $approachType.GetField('DestinationMetres') -and @($probeType.GetMethod('RouteInsideDiscAdmits', $any).GetParameters()).Count -eq 6 -and
    @($probeType.GetMethod('EvaluateRemainingRoute', $any).GetParameters()).Count -eq 8) 'PR-2 R8-F1 (round 11): the first debit revalidates the carrier''s own revalidated native route and destination arc length with the full arrival radius (no inward margin), skipping only the part already travelled'
$timeDelta = (GameType 'Kingmaker.Controllers.TimeController').GetProperty('DeltaTime', $any)
Check ($null -ne $timeDelta -and $null -ne $timeDelta.GetGetMethod($true) -and $timeDelta.PropertyType -eq [single]) 'PR-2 R10-F2 (round 11, restated round 13): installed TimeController.DeltaTime is the delta UnitMoveController passes to every TickMovement (each frame its own); the product reads only the imminent tick''s delta, never a forecast of later frames'
# ---- PR-2 round 13 (owner amendment 2026-11-27g section 3): the installed native baseline of an ordinary attack approach ----------------------------
# Read-only IL of Assembly-CSharp (Kingmaker 2.1.7b): acceptance, variable ticks, exhaustion, waiting/termination and Standard consumption.
$unitCommandType = GameType 'Kingmaker.UnitLogic.Commands.Base.UnitCommand'
$actionControllerType = GameType 'Kingmaker.Controllers.Units.UnitActionController'
$moveControllerTick = (GameType 'Kingmaker.Controllers.Units.UnitMoveController').GetMethod('Tick', $any)
$moveTickText = IlText $moveControllerTick
Check ($moveControllerTick.MetadataToken -eq 0x06009183 -and $moveTickText.Contains(';callvirt get_DeltaTime;callvirt TickMovement;')) 'PR-2 round 13 baseline: installed UnitMoveController.Tick (06009183) ticks each moving agent with that frame''s TimeController.DeltaTime (variable-duration ticks)'
Check ($tickText.Contains(';call GetRemainingMovementTime;stloc.0;ldarg.1;ldind.r4;ldloc.0;blt.un.s;') -and $tickText.Contains(';callvirt get_NearTheEnd;brfalse.s;ldstr ?;') -and
    $tickText.Contains(';ldarg.1;ldloc.0;stind.r4;ldarg.1;ldind.r4;ldc.r4 0;bgt.un.s;ret;') -and -not $tickText.Contains('Interrupt') -and -not $tickText.Contains('ForceFinish')) 'PR-2 round 13 baseline: installed TurnController.TickMovement clamps each tick to GetRemainingMovementTime (except on NearTheEnd, its borrowed-time endpoint correction) and returns without movement once the time is exhausted; it never interrupts or finishes the command itself (round 14, R13-F4: the refused tick is then handled by CanMoveInTurnBased, which completes the movement, and TickCommandTurnBased force-finishes the approach; see below)'
$tickTb = $actionControllerType.GetMethod('TickCommandTurnBased', $any); $tickTbText = IlText $tickTb
Check ($tickTb.MetadataToken -eq 0x0600911D -and $tickTbText.Contains(';callvirt get_ShouldUnitApproach;brfalse.s;') -and $tickTbText.Contains(';callvirt IsMoving;brtrue.s;') -and
    @([regex]::Matches($tickTbText, ';callvirt ForceFinishForTurnBased;')).Count -eq 2) 'PR-2 round 13 baseline: installed UnitActionController.TickCommandTurnBased (0600911D) force-finishes an approaching command whose approach was issued only once its agent no longer wants to move (UnitEntityView.IsMoving = WantsToMove); while it wants to move the command waits'
$approaching = $unitCommandType.GetMethod('TickApproaching', $any); $approachingText = IlText $approaching
Check ($approaching.MetadataToken -eq 0x060027A6 -and $approachingText.Contains(';callvirt FollowPrecomputedPath;ldarg.0;ldsfld MaxValue;stfld NextApproachTime;') -and $approachingText.Contains(';call IsInTurnBasedCombat;') -and
    $approachingText.Contains(';callvirt StopMoving;')) 'PR-2 round 13 baseline: installed UnitCommand.TickApproaching (060027A6) issues ONE turn-based approach on the visualized path (FollowPrecomputedPath, NextApproachTime = MaxValue: no re-path) and, once in range, stops the mover and turns to the target (an empty visualized path interrupts the command)'
$startText = IlText ($unitCommandType.GetMethod('Start', $any))
Check ($startText.Contains(';call get_IsUnitEnoughClose;brtrue.s;')) 'PR-2 round 13 baseline: installed UnitCommand.Start starts the action only from an actually close-enough position (otherwise it interrupts)'
$tickCommand = $actionControllerType.GetMethod('TickCommand', $any); $tickCommandText = IlText $tickCommand
$updateCooldownsText = IlText ($actionControllerType.GetMethod('UpdateCooldowns', $any))
Check ($tickCommand.MetadataToken -eq 0x0600911E -and @([regex]::Matches($tickCommandText, ';call UpdateCooldowns;')).Count -eq 1 -and $tickCommandText.Contains(';callvirt get_IsActed;brfalse.s;') -and
    $updateCooldownsText.Contains(';callvirt UpdateCooldowns;ret;') -and $updateCooldownsText.Contains(';callvirt set_StandardAction;')) 'PR-2 round 13 baseline: installed UnitActionController.TickCommand (0600911E) spends the command''s action (UpdateCooldowns: the turn-based unit''s cooldowns, the Standard included) only when the command becomes IsActed after its Tick: an approach that never strikes spends no Standard; movement is debited tick by tick (TickMovement) and stays spent'
Check ($null -eq $probeType.GetMethod('EvaluateNativeTicks', $any) -and $null -eq $controllerType.GetField('NativeMaxTurnBasedFrameSeconds', $any) -and $null -eq $controllerType.GetMethod('ShortOrdinaryApproach', $any) -and
    $null -ne $stateType.GetMethod('StepCounterFinal', $any) -and $null -ne $adapterType.GetMethod('StepCounterFinal', $any)) 'PR-2 round 13 (amendment section 4; R12-F1): the speculative ordinary forecast and the frame-bound short-ordinary click classification are removed; only a genuinely single-tick allowance (restricted tick, ordinary grant this tick exhausts, step whose native counter this tick makes final) is decided before its debit'
$rowsType = KmcType 'KingmakerMountedCombat.Integration.MountedMovementPersistenceRows'
$savedValues = KmcType 'KingmakerMountedCombat.Integration.SavedMovementValues'
$stepTravelType = KmcType 'KingmakerMountedCombat.Integration.SavedStepTravel'
$archiveType = KmcType 'KingmakerMountedCombat.Integration.MountedArchiveData'
Check ($null -ne $rowsType.GetMethod('Capture', $any) -and $null -ne $rowsType.GetMethod('CaptureTravel', $any) -and @($rowsType.GetMethod('Restore', $any).GetParameters()).Count -eq 2 -and
    @($savedValues.GetProperties()).Count -eq 7 -and $null -eq $savedValues.GetProperty('MetresStepTravelled') -and @($stepTravelType.GetProperties()).Count -eq 2 -and
    [int]$archiveType.GetField('Schema', $any).GetValue($null) -eq 4 -and $null -ne $archiveType.GetProperty('StepTravel')) 'PR-2 R11-F1 (round 13): the shared seven-field movement row is unchanged; archive schema 4 stores the settled physical step travel independently (SavedStepTravel rows) beside the exact native counter, with an explicit schema-3 reader'
Check ($null -ne $controllerType.GetMethod('SettleRetainedCarrierCleanups', $any) -and $null -ne $controllerType.GetMethod('SettleOutstandingEffects', $any) -and $null -eq $controllerType.GetField('settlingProposal', $any) -and
    $null -ne $controllerType.GetField('settlingProposals', $any)) 'PR-2 R11-F7/R11-F8 (round 12): terminal settlement retains an accepted command''s unfinished carrier cleanup, and every committed request with outstanding deferred effects is retained (a list, bound to its exact actors)'
# ---- PR-2 round 14 (R13-F4): the installed transition after a zero-delta tick (read-only IL, Kingmaker 2.1.7b) ----------------------------------
$agentType = GameType 'Kingmaker.View.UnitMovementAgent'
$wants = $agentType.GetProperty('WantsToMove', $any).GetGetMethod($true); $wantsText = IlText $wants
Check ($wants.MetadataToken -eq 0x060018BE -and $wantsText -ceq ';ldarg.0;callvirt get_Path;brtrue.s;ldarg.0;call get_RequestedPath;ldnull;cgt.un;ret;ldc.i4.1;ret;' -and
    (GameType 'Kingmaker.View.UnitMovementAgentBase').GetProperty('WantsToMove', $any).GetGetMethod($true).IsAbstract) 'PR-2 R13-F4 (round 14): installed UnitMovementAgent.get_WantsToMove (060018BE, the override of the abstract UnitMovementAgentBase.get_WantsToMove) is Path != null || RequestedPath != null'
$isMovingText = IlText ((GameType 'Kingmaker.View.UnitEntityView').GetMethod('IsMoving', $any))
Check ($isMovingText.Contains(';call get_MovementAgent;callvirt get_WantsToMove;ret;')) 'PR-2 R13-F4 (round 14): installed UnitEntityView.IsMoving (the TickCommandTurnBased test) is the agent''s WantsToMove'
$agentTick = $agentType.GetMethod('TickMovement', $any); $agentTickText = IlText $agentTick
Check ($agentTick.MetadataToken -eq 0x060018AA -and $agentTickText.StartsWith(';ldarg.0;ldflda m_SavedMinSpeed;call get_HasValue;brfalse.s;ldarg.0;ldarg.0;ldflda m_SavedMinSpeed;call get_Value;stfld m_MinSpeed;ldarg.0;ldflda m_SavedMinSpeed;initobj Nullable`1;ldarg.0;ldarga.s 1;call CanMoveInTurnBased;brtrue.s;ret;')) 'PR-2 R13-F4 (round 14): installed UnitMovementAgent.TickMovement (060018AA) returns at once when CanMoveInTurnBased refuses the tick (no shift, no stuck clock)'
$canMove = $agentType.GetMethod('CanMoveInTurnBased', $any); $canMoveText = IlText $canMove
Check ($canMove.MetadataToken -eq 0x060018A9 -and $canMoveText.Contains(';ldloc.3;ldarg.1;ldloc.1;callvirt TickMovement;ldarg.1;ldind.r4;ldc.r4 0;bgt.un.s;ldc.i4.0;stloc.0;') -and
    $canMoveText.Contains(';ldfld DontInterruptInTurnBasedMode;ldc.i4.0;ceq;brfalse.s;ldarg.0;call CompleteMovement;br.s;ldarg.0;ldc.i4.1;stfld m_NoMoreTurnBasedMovementForUinterruptableCommand;ldc.i4.0;ret;ldc.i4.1;ret;')) 'PR-2 R13-F4 (round 14): installed CanMoveInTurnBased (060018A9): a tick TurnController.TickMovement leaves at zero refuses movement and, outside force mode and cutscene control, completes the movement (CompleteMovement) unless the Move slot command is DontInterruptInTurnBasedMode'
$completeText = IlText ($agentType.GetMethod('CompleteMovement', $any)); $interruptText = IlText ($agentType.GetMethod('InterruptMovement', $any)); $stopText = IlText ($agentType.GetMethod('Stop', $any))
Check ($completeText.Contains(';callvirt Stop;ret;ldarg.0;call InterruptMovement;ret;') -and $interruptText.EndsWith(';ldarg.0;callvirt Stop;ret;') -and
    $stopText.Contains(';ldarg.0;ldnull;callvirt set_Path;ldarg.0;ldnull;call set_RequestedPath;')) 'PR-2 R13-F4 (round 14): installed CompleteMovement stops (inside the approach radius) or interrupts the movement, both through Stop, which clears Path and RequestedPath: WantsToMove becomes false'
Check ($tickTbText.Contains(';callvirt get_ShouldUnitApproach;brfalse.s;ldarg.1;ldflda NextApproachTime;call get_Ticks;ldc.i4.0;conv.i8;ble.s;ldloc.0;callvirt get_View;callvirt IsMoving;brtrue.s;ldarg.1;ldc.i4.3;callvirt ForceFinishForTurnBased;') -and
    $tickCommandText.StartsWith(';ldarg.0;ldarg.1;call TickCommandTurnBased;brtrue.s;ret;')) 'PR-2 R13-F4 (round 14): installed TickCommandTurnBased then force-finishes the approaching command that is not close enough once its approach was issued (NextApproachTime > 0) and its executor no longer moves, and TickCommand returns before Tick and UpdateCooldowns (no strike, no Standard)'
$commandType = KmcType 'KingmakerMountedCombat.Integration.MountedPairAttackCommand'
Check ($null -ne $commandType.GetProperty('NativeApproachEndedCount', $any) -and $null -ne $commandType.GetMethod('MountAgentStoppedAfterCarrierMoved', $any)) 'PR-2 R13-F3/R13-F4 (round 14): an accepted route-bound ordinary turn-based order ends with that disposition (no re-path) when its carrier finished out of reach or the mount''s agent no longer wants to move'
$probeIl = IlTokens ($probeType.GetMethod('EvaluateRemainingRoute', $any))
Check ($null -eq $probeType.GetField('PlannedSegmentIdentityMetres', $any) -and $null -ne $probeType.GetMethod('LiveLegAdmits', $any) -and $probeIl.ContainsKey($probeType.GetMethod('LiveLegAdmits', $any).MetadataToken)) 'PR-2 R11-F3 (round 14): the 20 micrometre planned-segment identity is removed; the first debit validates the live leg (LiveLegAdmits) unless the mount stands exactly on the planned segment start'
# ---- PR-2 round 15 (lead's movement findings; compiled product) -----------------------------------------------------------------------------
$liveLegText = IlText ($probeType.GetMethod('LiveLegAdmits', $any))
Check ($liveLegText.Length -gt 2 -and -not $liveLegText.Contains('Lerp')) 'PR-2 R14-F1 (round 15): LiveLegAdmits computes every live-chord sample explicitly, never with Vector3.Lerp (Unity clamps its parameter to [0, 1]); its sampling by live-chord distance is exercised by the attack-path harness'
$readerInvoke = (KmcType 'KingmakerMountedCombat.Integration.MountedCombatController+MountGrantReader').GetMethod('Invoke')
$lifecycleType = KmcType 'KingmakerMountedCombat.Integration.MountedPairTurnParticipant'
$admitText = IlText ($controllerType.GetMethod('AdmitTurnBasedAttackClick', $any))
Check ($readerInvoke.GetParameters().Count -eq 4 -and $readerInvoke.GetParameters()[3].Name -ceq 'stepCounterBinds' -and $null -eq $lifecycleType.GetMethod('PairedMountStepCounterBinds', $any) -and
    -not $admitText.Contains('StepCounter')) 'PR-2 R14-F2 (round 15): the click''s grant reader returns the step-counter classification priced with the same speed observations; the click rereads nothing to classify the step'
$adapterType = KmcType 'KingmakerMountedCombat.Integration.MountedMovementStateAdapter'
$tickDelegatedText = IlText ($adapterType.GetMethod('TickDelegated', $any))
Check ($null -eq $adapterType.GetMethod('StepEndpointCap', $any) -and $null -ne $adapterType.GetMethod('StepExecutableTick', $any) -and $tickDelegatedText.Contains('StepExecutableTick') -and
    $tickDelegatedText.IndexOf('StepExecutableTick') -lt $tickDelegatedText.IndexOf('Decide;') -and $tickDelegatedText.IndexOf('Decide;') -lt $tickDelegatedText.IndexOf('Advance;')) 'PR-2 R13-F5 (round 15), round 16 (R13-F5/R15-F1/R15-F2): a step tick is bounded by installed TickMovement''s endpoint completion at the waypoint native selects for the returned duration (StepExecutableTick) BEFORE the first-tick decision, which receives that executable tick, and before the allocation advances it; the cursor-based StepEndpointCap is removed'
$simulateTick = $probeType.GetMethod('SimulateTick', $any); $firstTickNativeText = IlText ($probeType.GetMethod('EvaluateFirstTickNative', $any)); $stepDurationText = IlText ($probeType.GetMethod('StepExecutableDuration', $any))
Check ($null -ne $simulateTick -and $firstTickNativeText.Contains('call SimulateTick;') -and $firstTickNativeText.Contains('call StepSettlementAdmits;') -and $stepDurationText.Contains('call SimulateTick;') -and
    $stepDurationText.Contains('call StepSettlementAdmits;') -and @($probeType.GetMethod('EvaluateFirstTickNative', $any).GetParameters()).Count -eq 12) 'PR-2 round 16 (R13-F5/R15-F1/R15-F2): one installed-tick simulation (SimulateTick: native selection with skipping at the candidate duration, steering, completion only on a selected last segment) serves the first-tick candidates, the interior stops and the step-executable duration; every candidate settles within the physical step remainder (StepSettlementAdmits)'
$firstTickNativeMethod = $probeType.GetMethod('EvaluateFirstTickNative', $any)
Check ($firstTickNativeMethod.GetParameters()[5].Name -ceq 'allowedTick' -and -not $firstTickNativeText.Contains(';ldarg.s 5;ldarg.s 6;div;') -and $null -ne $probeType.GetNestedType('Result', $any).GetField('Duration', $any)) 'PR-2 round 17 (R16-F4): the first-tick decision works in durations: EvaluateFirstTickNative receives the permitted duration (allowedTick, never allowedMetres / speed) and returns the selected duration (Result.Duration) that the gate approves and the adapter returns unchanged'
$decisionText = IlText ($controllerType.GetMethod('EvaluateFirstTickDecision', $any))
Check ($adapterType.GetMethod('StepExecutableTick', $any).ReturnType.Name -ceq 'StepBound' -and (IlText ($probeType.GetMethod('ClassifyStep', $any))).Contains('call TurnEstablished;') -and (IlText ($probeType.GetMethod('EvaluateFirstTickNative', $any))).Contains('call TurnEstablished;') -and $stepDurationText.Contains('call InsideVerified;') -and $stepDurationText.Contains('call Executable;') -and $stepDurationText.Contains('call ClassifyStep;') -and $null -ne $probeType.GetMethod('InsideVerified', $any) -and $null -ne $probeType.GetMethod('ClassifyStep', $any)) 'PR-2 round 18 (R16-F3), round 19 (R18-F1), round 20 (R19-F1), rounds 21-22 (R20-F1), round 23 (R22-F2): every margin back-off of the step-duration search stays inside its verified interval (InsideVerified: the interval midpoint when the back-off would leave it) and the interval above zero is resolved to an executable, displacing duration before zero is returned, each sample classified (ClassifyStep: a sample short of the 0.01 mm floor raises the lower bound, one beyond the settlement limit lowers the upper bound) and the selected duration re-checked (Executable); every positive return (the permitted duration, the scan back-off and sample, the bisection result) is Executable at its float-representable settled stop (displacement at least the floor and within the physical remainder, completion margin kept); the adapter applies the search also when the path end lies within the physical remainder, and its result is explicit (StepExecutableTick returns StepBound: NoBoundApplies, Bounded with a verified duration, or Unestablished, which TickDelegated turns into a zero tick: unreadable or unmodelled steering, a readable zero combat angular speed with a non-zero required turn, no representable legal move, a fault; no affordable-end exemption); at a readable zero combat angular speed the exact-alignment requirement (TurnEstablished) applies to EACH candidate, in the step search (ClassifyStep: a non-aligned candidate is too long, so a shorter aligned duration is kept) and in the first-tick evaluation (EvaluateFirstTickNative, whose final-step and shortened-strike branches admit a readable zero angular speed); its connected cases (50.15 mm, 50.112 mm, 50.6 mm, 45 mm; X = 256 m with 0.005, 0.020 and 0.040 mm left; X = 256 m with an affordable end: interior waypoint, native-limited, completion control) are in the attack-path harness'
$stepTickText = IlText ($adapterType.GetMethod('StepExecutableTick', $any)); $readFactsText = IlText ($adapterType.GetMethod('ReadSteeringFacts', $any))
Check (-not $decisionText.Contains('call TryReadSteering;') -and -not $decisionText.Contains('call PhysicalSpeed;') -and -not $stepTickText.Contains('call TryReadSteering;') -and
    $readFactsText.Contains('call TryReadSteering;') -and $tickDelegatedText.Contains('call ReadSteeringFacts;') -and
    $tickDelegatedText.IndexOf('call ReadSteeringFacts;') -lt $tickDelegatedText.IndexOf('call StepExecutableTick;')) 'PR-2 round 17 (R14-F2 single-read rule): the steering facts (agent speed, heading, combat angular speed, follow, force, combat) and MaxSpeed are read once per movement tick by the adapter (ReadSteeringFacts) and passed to the step bound and the first-tick decision, which read no speed or steering input again'
$decisionMethod = $controllerType.GetMethod('EvaluateFirstTickDecision', $any); $decisionIl = $decisionMethod.GetMethodBody().GetILAsByteArray(); $decisionStrings = New-Object 'System.Collections.Generic.List[string]'
for ($k = 0; $k -lt $decisionIl.Length - 4; $k++) { if ($decisionIl[$k] -eq 0x72) { $token = [BitConverter]::ToInt32($decisionIl, $k + 1); if (($token -shr 24) -eq 0x70) { try { $decisionStrings.Add($decisionMethod.Module.ResolveString($token)) } catch { } } } }
Check ($decisionText.IndexOf('call EvaluateRemainingRoute;') -ge 0 -and $decisionText.IndexOf('call EvaluateRemainingRoute;') -lt $decisionText.IndexOf('call EvaluateFirstTickNative;') -and
    $decisionText.IndexOf('PairedMountStepCounterFinal;') -ge 0 -and $decisionText.IndexOf('PairedMountStepCounterFinal;') -lt $decisionText.IndexOf('call EvaluateRemainingRoute;') -and
    $decisionStrings.Contains('final-ordinary-tick-') -and $decisionStrings.Contains('final-step-counter-')) 'PR-2 round 16 (R15-F3 and the counter-final step): the common live route and arrival check (EvaluateRemainingRoute) runs before the executable-stop decision (EvaluateFirstTickNative) of the final ordinary tick and of the step whose native counter becomes final (each with its own refusal label), as for the larger ordinary grant and the non-final step; the restricted single-move first tick keeps its executable-stop admission (its connected behaviour: attack-path harness)'
Check ($null -ne $probeType.GetField('InteriorStopCandidatesPerTick', $any) -and (IlText ($probeType.GetMethod('EvaluateFirstTickNative', $any))).Contains('ldc.r4 4096;')) 'PR-2 R12-F2 (round 15): the current steered tick''s interior stops are evaluated at the established resolution (first-tick-interior)'
$tickApproachText = IlText ($commandType.GetMethod('TickApproach', $any))
$firstEnd = $tickApproachText.IndexOf('EndIfNativeTurnBasedApproachEnded'); $firstRepath = $tickApproachText.IndexOf('call Repath')
Check ($firstEnd -ge 0 -and $firstRepath -gt $firstEnd -and $null -ne $commandType.GetProperty('NativeTurnBasedDispositionApplies', $any)) 'PR-2 R13-F3 (round 15): TickApproach resolves the end of the issued ordinary turn-based approach before the target-drift re-path, for the override and the native-accepted entries'
$beginProvisionalText = IlText ($controllerType.GetMethod('BeginProvisionalOrder', $any))
Check ($beginProvisionalText.Contains('CompetingMountWork') -and $beginProvisionalText.Contains('MountHoldsOtherWork')) 'PR-2 R14-F3 (round 15): a step request is classified with the accepted-work rule (CompetingMountWork; compatible older work waits), the restricted single tick keeps its idle-mount precondition'
# ---- PR-2 round 19 (R17-F1, R18-F2): the exact carrier's issuance and delivery boundaries (installed IL, read-only) and the product's hooks ----
$pathTo = MemberAt 0x060018A3; $pathComplete = MemberAt 0x060018B9; $requestedField = MemberAt 0x0400118E
Check ($pathTo.Name -ceq 'PathTo' -and $pathTo.DeclaringType.FullName -ceq 'Kingmaker.View.UnitMovementAgent' -and @($pathTo.GetParameters()).Count -eq 5 -and
    $pathTo.GetParameters()[0].Name -ceq 'command' -and $pathTo.GetParameters()[0].ParameterType.FullName -ceq 'Kingmaker.UnitLogic.Commands.Base.UnitCommand' -and
    $pathComplete.Name -ceq 'OnPathComplete' -and $pathComplete.DeclaringType.FullName -ceq 'Kingmaker.View.UnitMovementAgent' -and @($pathComplete.GetParameters()).Count -eq 1 -and
    $pathComplete.GetParameters()[0].Name -ceq 'p' -and $pathComplete.GetParameters()[0].ParameterType.FullName -ceq 'Pathfinding.Path' -and
    $requestedField.Name -ceq 'm_RequestedPath' -and $requestedField.FieldType.FullName -ceq 'Pathfinding.Path' -and $requestedField.DeclaringType.FullName -ceq 'Kingmaker.View.UnitMovementAgent') 'PR-2 round 19 (R17-F1, R18-F2): 060018A3 is UnitMovementAgent.PathTo(UnitCommand command, ...), 060018B9 is UnitMovementAgent.OnPathComplete(Path p), 0400118E is UnitMovementAgent.m_RequestedPath (Pathfinding.Path)'
$pathToText = IlText $pathTo; $pathCompleteText = IlText $pathComplete
$setPathText = IlText (MemberAt 0x06001894); $setRequestedText = IlText (MemberAt 0x06001896)
Check ($pathToText.Contains(';ldarg.0;call get_RequestedPath;brfalse.s;ldarg.0;ldfld m_RequestedNewPath;brtrue.s;ret;') -and $pathToText.Contains(';ldftn OnPathComplete;') -and
    $pathToText.Contains(';call set_RequestedPath;') -and $pathCompleteText.StartsWith(';ldarg.1;ldarg.0;call get_RequestedPath;beq.s;ret;') -and
    $pathCompleteText.Contains(';ldarg.0;ldnull;call set_RequestedPath;') -and $pathCompleteText.Contains(';call OnPathNotFound;ret;') -and
    $pathCompleteText.Contains(';ldarg.0;ldarg.1;callvirt set_Path;ldarg.0;ldc.i4.1;call SetWaypoint;') -and
    $setPathText.Contains(';ldfld m_Path;call ReleasePath;ldarg.0;ldarg.1;stfld m_Path;ldarg.0;ldarg.0;ldfld m_Path;call ClaimPath;ret;') -and
    $setRequestedText.Contains(';ldfld m_RequestedPath;call ReleasePath;ldarg.0;ldarg.1;stfld m_RequestedPath;ldarg.0;ldarg.0;ldfld m_RequestedPath;call ClaimPath;ret;')) 'PR-2 round 19 (R17-F1, R18-F2): installed PathTo keeps an outstanding request it is not replacing (no new request) and otherwise stores the new request in m_RequestedPath with OnPathComplete as its callback; OnPathComplete acts only on the outstanding request, clears it, and either makes it the agent''s Path (set_Path, SetWaypoint(1): the delivery) or reports OnPathNotFound without delivering; set_Path and set_RequestedPath claim their path (a held Path is never recycled by A*)'
$tickApproachingText = IlText (MemberAt 0x060027A6); $viewMoveText = IlText (MemberAt 0x0600184D); $isReallyText = IlText (MemberAt 0x060018C0); $moveControllerText = IlText (MemberAt 0x06009183)
Check ($tickApproachingText.Contains(';callvirt FollowPrecomputedPath;') -and $tickApproachingText.Contains(';callvirt MoveTo;') -and $viewMoveText.Contains(';callvirt PathTo;') -and
    $isReallyText.Contains(';callvirt get_Path;') -and $isReallyText.Contains(';ldfld m_NextPointIndex;ldc.i4.0;cgt;ret;') -and
    $moveControllerText.IndexOf(';callvirt get_IsReallyMoving;') -ge 0 -and $moveControllerText.IndexOf(';callvirt get_IsReallyMoving;') -lt $moveControllerText.IndexOf(';callvirt TickMovement;')) 'PR-2 round 19 (R17-F1, R18-F2): installed UnitCommand.TickApproaching issues an unforced approach through UnitEntityView.MoveTo -> PathTo(this command) and a forced one through FollowPrecomputedPath (no PathTo); UnitMoveController.Tick ticks an agent (CanMoveInTurnBased, the paired hook) only while IsReallyMoving (a delivered Path, cursor past its first waypoint), whatever command''s request that path answered'
$patchControllerType = KmcType 'KingmakerMountedCombat.Integration.MountedPatchController'
$installers = @(@($patchControllerType.GetConstructors($any)) + @($patchControllerType.GetMethods(($any -bor [Reflection.BindingFlags]::DeclaredOnly))) | Where-Object { $_.GetMethodBody() -ne $null -and (IlText $_).Contains(';ldc.i4 100669603;') })
$installText = if ($installers.Count -eq 1) { IlText $installers[0] } else { '' }
Check ($installers.Count -eq 1 -and $installText.Contains(';ldc.i4 100669625;') -and $installText.Contains(';ldc.i4 67113358;call RequestedPathContractHolds;') -and
    $installText.IndexOf(';ldc.i4 100669603;') -lt $installText.IndexOf(';ldc.i4 100669625;') -and @([regex]::Matches($installText, ';call PatchExact;')).Count -ge 2) 'PR-2 round 19 (R17-F1, R18-F2): the product installs its carrier issuance/delivery hooks once, with PatchExact on PathTo (060018A3) and OnPathComplete (060018B9) after verifying m_RequestedPath (0400118E), in the same installer as every production patch (removed together by UnpatchAll of the product''s Harmony id)'
$patchMethodsType = KmcType 'KingmakerMountedCombat.Integration.MountedPatchController+PatchMethods'
$observerType = KmcType 'KingmakerMountedCombat.Integration.MountedCarrierPathObserver'
$ledgerType19 = KmcType 'KingmakerMountedCombat.Domain.MountedCarrierApproachLedger'
$hookNames = @('CarrierPathRequestPrefix','CarrierPathRequestPostfix','CarrierPathCompletePrefix','CarrierPathCompletePostfix')
$hooks = @($hookNames | ForEach-Object { $patchMethodsType.GetMethod($_, $any) })
$hookParams = @($hooks | ForEach-Object { if ($null -eq $_) { '' } else { (@($_.GetParameters() | ForEach-Object { $_.ParameterType.Name + ' ' + $_.Name }) -join ',') } })
Check ($hookParams[0] -ceq 'UnitMovementAgent __instance,Path& __state' -and $hookParams[1] -ceq 'UnitMovementAgent __instance,UnitCommand command,Path __state' -and
    $hookParams[2] -ceq 'UnitMovementAgent __instance,Path p,Int32& __state' -and $hookParams[3] -ceq 'UnitMovementAgent __instance,Path p,Int32 __state' -and
    (IlText $hooks[0]).Contains(';call BeforeRequest;') -and (IlText $hooks[1]).Contains(';call AfterRequest;') -and (IlText $hooks[2]).Contains(';call BeforeCompletion;') -and (IlText $hooks[3]).Contains(';call AfterCompletion;') -and
    (IlText ($observerType.GetMethod('AfterRequest', $any))).Contains(';call RecordRequest;') -and (IlText ($observerType.GetMethod('AfterCompletion', $any))).Contains(';call RecordCompletion;') -and
    -not (IlText ($observerType.GetMethod('AfterCompletion', $any))).Contains('set_')) 'PR-2 round 19 (R17-F1, R18-F2): the hooks bind PathTo''s exact command and OnPathComplete''s p by their installed parameter names and delegate to MountedCarrierPathObserver, which records the request and its delivery in MountedCarrierApproachLedger and changes no native state'
# ---- PR-2 round 19 follow-up: the forced request (FollowPrecomputedPath) attributed to the exact command whose TickApproaching issues it ----
$follow = MemberAt 0x060018B6; $followText = IlText $follow
Check ($follow.Name -ceq 'FollowPrecomputedPath' -and $follow.DeclaringType.FullName -ceq 'Kingmaker.View.UnitMovementAgent' -and @($follow.GetParameters()).Count -eq 2 -and
    $follow.GetParameters()[0].Name -ceq 'p' -and $follow.GetParameters()[0].ParameterType.FullName -ceq 'Pathfinding.Path' -and
    $followText.StartsWith(';ldarg.0;ldarg.1;call set_RequestedPath;') -and $followText.Contains(';ldarg.0;ldarg.1;call OnPathComplete;') -and
    $tickApproachingText.Contains(';ldfld vectorPath;newobj .ctor;newobj .ctor;ldarg.0;call get_ApproachRadius;callvirt FollowPrecomputedPath;')) 'PR-2 round 19 follow-up: 060018B6 is UnitMovementAgent.FollowPrecomputedPath(Path p, float): it sets m_RequestedPath to p and completes it synchronously (OnPathComplete), with no PathTo; TickApproaching passes it a FRESH ForcedPath copy of the ticking command''s path (no command argument), so the issuer is the command whose TickApproaching is running, not a ForcedPath reference'
$forcedHook = $patchMethodsType.GetMethod('CarrierForcedPathRequestPrefix', $any)
$approachPrefixText = IlText ($patchMethodsType.GetMethod('MountedAttackApproachPrefix', $any))
Check ($null -ne $forcedHook -and (@($forcedHook.GetParameters() | ForEach-Object { $_.ParameterType.Name + ' ' + $_.Name }) -join ',') -ceq 'UnitMovementAgent __instance,Path p' -and
    (IlText $forcedHook).Contains(';call BeforeForcedRequest;') -and (IlText ($observerType.GetMethod('BeforeForcedRequest', $any))).Contains(';call RecordRequest;') -and
    -not $approachPrefixText.Contains('EnterApproach') -and $null -eq $patchMethodsType.GetMethod('MountedAttackApproachPostfix', $any) -and
    $installText.Contains(';ldc.i4 100669622;') -and $installText.IndexOf(';ldc.i4 100669625;') -lt $installText.IndexOf(';ldc.i4 100669622;')) 'PR-2 round 19 follow-up, round 22 (R19-F5): the product''s FollowPrecomputedPath prefix (PatchExact 060018B6, binding p by name) records the forced request for the exact issuer; the TickApproaching prefix keeps no scope (no EnterApproach, no scope postfix)'
# ---- PR-2 round 22 (R19-F5): the exact caller binding at the TickApproaching -> FollowPrecomputedPath call site (installed IL and the product transpiler) ----
Check (@([regex]::Matches($tickApproachingText, ';callvirt FollowPrecomputedPath;')).Count -eq 1 -and @([regex]::Matches($tickApproachingText, ';call FollowPrecomputedPath;')).Count -eq 0 -and
    @([regex]::Matches($tickApproachingText, ';newobj \.ctor;ldarg\.0;call get_ApproachRadius;callvirt FollowPrecomputedPath;')).Count -eq 1) 'PR-2 round 22 (R19-F5): installed UnitCommand.TickApproaching (060027A6) makes exactly one FollowPrecomputedPath call, with the stack (agent, newobj ForcedPath, ldarg.0; call get_ApproachRadius)'
$transpiler22 = $patchMethodsType.GetMethod('ForcedPathCallerTranspiler', $any)
$ciType22 = $transpiler22.GetParameters()[0].ParameterType.GetGenericArguments()[0]
$helper22 = $observerType.GetMethod('FollowPrecomputedPathFrom', $any)
$radius22 = (GameType 'Kingmaker.UnitLogic.Commands.Base.UnitCommand').GetProperty('ApproachRadius', $any).GetGetMethod($true)
$forcedType22 = $follow.GetParameters()[0].ParameterType.Assembly.GetType('Pathfinding.ForcedPath', $true)
$vector22 = $pathTo.GetParameters()[1].ParameterType
$forcedCtor22 = $forcedType22.GetConstructor([Type[]]@([System.Collections.Generic.List``1].MakeGenericType($vector22)))
function New-Ci22($op, $operand) { [Activator]::CreateInstance($ciType22, @($op, $operand)) }
function Invoke-Transpiler22([object[]]$Items) {
    $list = [Activator]::CreateInstance([System.Collections.Generic.List``1].MakeGenericType($ciType22)); foreach ($item in $Items) { [void]$list.Add($item) }
    try { , @($transpiler22.Invoke($null, [object[]]@(, $list)) | ForEach-Object { $_ }) } catch { $null }
}
function New-Site22 { @((New-Ci22 ([Reflection.Emit.OpCodes]::Newobj) $forcedCtor22), (New-Ci22 ([Reflection.Emit.OpCodes]::Ldarg_0) $null), (New-Ci22 ([Reflection.Emit.OpCodes]::Call) $radius22), (New-Ci22 ([Reflection.Emit.OpCodes]::Callvirt) $follow)) }
$out22 = Invoke-Transpiler22 (New-Site22)
$twice22 = Invoke-Transpiler22 (@(New-Site22) + @(New-Site22))
$none22 = Invoke-Transpiler22 @((New-Ci22 ([Reflection.Emit.OpCodes]::Newobj) $forcedCtor22), (New-Ci22 ([Reflection.Emit.OpCodes]::Ldarg_0) $null), (New-Ci22 ([Reflection.Emit.OpCodes]::Call) $radius22))
$shape22 = Invoke-Transpiler22 @((New-Ci22 ([Reflection.Emit.OpCodes]::Ldnull) $null), (New-Ci22 ([Reflection.Emit.OpCodes]::Ldarg_0) $null), (New-Ci22 ([Reflection.Emit.OpCodes]::Call) $radius22), (New-Ci22 ([Reflection.Emit.OpCodes]::Callvirt) $follow))
Check ($null -ne $forcedCtor22 -and $null -ne $helper22 -and (@($helper22.GetParameters() | ForEach-Object { $_.ParameterType.Name }) -join ',') -ceq 'UnitMovementAgent,Path,Single,UnitCommand' -and
    $null -ne $out22 -and $out22.Count -eq 5 -and $out22[0].opcode -eq [Reflection.Emit.OpCodes]::Newobj -and $out22[1].opcode -eq [Reflection.Emit.OpCodes]::Ldarg_0 -and
    $out22[2].opcode -eq [Reflection.Emit.OpCodes]::Call -and $out22[3].opcode -eq [Reflection.Emit.OpCodes]::Ldarg_0 -and $out22[4].opcode -eq [Reflection.Emit.OpCodes]::Call -and
    $out22[4].operand -eq $helper22 -and $null -eq $twice22 -and $null -eq $none22 -and $null -eq $shape22 -and
    (IlText $helper22).Contains(';callvirt FollowPrecomputedPath;') -and @([regex]::Matches((IlText $helper22), 'FollowPrecomputedPath;')).Count -eq 1) 'PR-2 round 22 (R19-F5): the product transpiler (on the existing TickApproaching PatchExact) turns the installed site `newobj ForcedPath(List<Vector3>); ldarg.0; call get_ApproachRadius; callvirt FollowPrecomputedPath` into `...; ldarg.0; call MountedCarrierPathObserver.FollowPrecomputedPathFrom(UnitMovementAgent, Path, float, UnitCommand)`, which makes the native call once; it refuses two sites, no site and a different argument shape (fail closed at install)'
# ---- PR-2 round 22 (R19-F5), real transpile: the product's TickApproaching transpiler alone, applied with the installed Harmony12 to the REAL installed
# UnitCommand.TickApproaching (060027A6) in this detached process, its patched instructions read back with HarmonyLib, then unpatched ----
$h12Assembly = $ciType22.Assembly
$hiType22 = $h12Assembly.GetType('Harmony12.HarmonyInstance', $true); $hmType22 = $h12Assembly.GetType('Harmony12.HarmonyMethod', $true)
$harmonyLib22 = @([AppDomain]::CurrentDomain.GetAssemblies() | Where-Object { $_.GetName().Name -ceq '0Harmony' }) | Select-Object -First 1
if ($null -eq $harmonyLib22) { $harmonyLib22 = [Reflection.Assembly]::LoadFrom((Join-Path $managed 'UnityModManager\0Harmony.dll')) }
$processor22 = $harmonyLib22.GetType('HarmonyLib.PatchProcessor', $true)
$getCurrent22 = @($processor22.GetMethods() | Where-Object { $_.Name -ceq 'GetCurrentInstructions' -and $_.IsStatic -and $_.GetParameters().Count -eq 3 })[0]
$getOriginal22 = @($processor22.GetMethods() | Where-Object { $_.Name -ceq 'GetOriginalInstructions' -and $_.IsStatic -and $_.GetParameters().Count -eq 2 })[0]
$tick22 = MemberAt 0x060027A6
$patchMethod22 = @($hiType22.GetMethods() | Where-Object { $_.Name -ceq 'Patch' -and $_.GetParameters().Count -eq 4 })[0]
$contractId22 = 'kmc.contract.forcedpath-transpile'; $boundaryId22 = 'kmc.contract.forcedpath-transpile.boundary'
$harmony22 = $hiType22.GetMethod('Create').Invoke($null, @($contractId22)); $boundary22 = $hiType22.GetMethod('Create').Invoke($null, @($boundaryId22))
$transpilerHm22 = [Activator]::CreateInstance($hmType22, @([Reflection.MethodInfo]$transpiler22))
$original22 = @($getOriginal22.Invoke($null, @($tick22, $null)))
$directError22 = $null; $detourSkipped22 = $false; $current22 = $null; $patchError22 = $null; $ownersAfter22 = -1
try
{
    try { [void]$patchMethod22.Invoke($harmony22, @($tick22, $null, $null, $transpilerHm22)) }
    catch { $directError22 = $_.Exception; while ($directError22.InnerException) { $directError22 = $directError22.InnerException } }
    if ($null -ne $directError22)
    {
        # The precedent of scripts/Test-GunslingerClickComposition.ps1: only the jump into the live original (HarmonyLib.PatchTools.DetourMethod) is
        # skipped, for this one method; the transpiler and Harmony's replacement build run for real.
        Write-Host ('INFO real-transpile direct patch refused detached: ' + $directError22.GetType().FullName + ': ' + $directError22.Message)
        if (-not ('KmcForcedPathBoundary' -as [type])) { Add-Type -TypeDefinition 'public static class KmcForcedPathBoundary { public static System.Reflection.MethodBase Target; public static int Skipped; public static bool SkipDetour(System.Reflection.MethodBase method) { if (Target == null || method != Target) return true; Skipped++; return false; } }' }
        [KmcForcedPathBoundary]::Target = $tick22
        $detour22 = $harmonyLib22.GetType('HarmonyLib.PatchTools', $true).GetMethod('DetourMethod', [Reflection.BindingFlags]'Static,Public,NonPublic')
        [void]$patchMethod22.Invoke($boundary22, @($detour22, [Activator]::CreateInstance($hmType22, @([Reflection.MethodInfo][KmcForcedPathBoundary].GetMethod('SkipDetour'))), $null, $null))
        try { [void]$patchMethod22.Invoke($harmony22, @($tick22, $null, $null, $transpilerHm22)); $detourSkipped22 = [KmcForcedPathBoundary]::Skipped -ge 1 }
        catch { $patchError22 = $_.Exception; while ($patchError22.InnerException) { $patchError22 = $patchError22.InnerException }; Write-Host ('INFO real-transpile patch with the detour skipped also refused: ' + $patchError22.GetType().FullName + ': ' + $patchError22.Message) }
    }
    if ($null -eq $patchError22) { $current22 = @($getCurrent22.Invoke($null, @($tick22, [int]::MaxValue, $null))) }
}
finally
{
    $hiType22.GetMethod('UnpatchAll', [Type[]]@([string])).Invoke($harmony22, @($contractId22))
    $info22 = $hiType22.GetMethod('GetPatchInfo').Invoke($harmony22, @($tick22))
    $ownersAfter22 = if ($null -eq $info22) { 0 } else { @($info22.Owners).Count }
    $hiType22.GetMethod('UnpatchAll', [Type[]]@([string])).Invoke($boundary22, @($boundaryId22))
    if ('KmcForcedPathBoundary' -as [type]) { [KmcForcedPathBoundary]::Target = $null }
}
function Norm22($op) { $op.Name -replace '\.s$', '' }
$sites22 = @(); $remaining22 = 0; $sameRest22 = $false
if ($null -ne $current22)
{
    for ($k = 0; $k -lt $current22.Count; $k++)
    {
        $operand = $current22[$k].operand
        if ($current22[$k].opcode -eq [Reflection.Emit.OpCodes]::Call -and $operand -eq $helper22) { $sites22 += $k }
        if ($operand -is [Reflection.MethodBase] -and $operand.Name -ceq 'FollowPrecomputedPath' -and $operand.MetadataToken -eq 0x060018B6) { $remaining22++ }
    }
    if ($sites22.Count -eq 1 -and $sites22[0] -ge 1 -and $current22.Count -eq $original22.Count + 1)
    {
        $site = $sites22[0]; $sameRest22 = $true
        for ($k = 0; $k -lt $original22.Count; $k++)
        {
            $c = if ($k -lt $site - 1) { $k } elseif ($k -eq $site - 1) { $site } else { $k + 1 }
            if ($k -eq $site - 1) { $sameRest22 = $sameRest22 -and $original22[$k].opcode -eq [Reflection.Emit.OpCodes]::Callvirt -and $original22[$k].operand.MetadataToken -eq 0x060018B6; continue }
            if ((Norm22 $original22[$k].opcode) -cne (Norm22 $current22[$c].opcode)) { $sameRest22 = $false; break }
        }
    }
}
$originalSites22 = @($original22 | Where-Object { $_.operand -is [Reflection.MethodBase] -and $_.operand.MetadataToken -eq 0x060018B6 }).Count
Write-Host ('INFO real-transpile: Harmony12 ' + $h12Assembly.GetName().Version + ' over HarmonyLib ' + $harmonyLib22.GetName().Version + '; original instructions ' + $original22.Count + ' (FollowPrecomputedPath sites ' + $originalSites22 +
    '); patched ' + $(if ($null -eq $current22) { 'none' } else { $current22.Count }) + '; direct patch ' + $(if ($null -eq $directError22) { 'succeeded' } else { 'refused, detour skipped ' + $detourSkipped22 }) + '; owners after UnpatchAll ' + $ownersAfter22)
Check ($null -eq $patchError22 -and $null -ne $current22 -and $originalSites22 -eq 1 -and $sites22.Count -eq 1 -and $current22[$sites22[0] - 1].opcode -eq [Reflection.Emit.OpCodes]::Ldarg_0 -and
    $remaining22 -eq 0 -and $current22.Count -eq $original22.Count + 1 -and $sameRest22 -and $ownersAfter22 -eq 0) ('PR-2 round 22 (R19-F5), real transpile: the product''s TickApproaching transpiler alone, applied through the installed Harmony12 to the REAL installed UnitCommand.TickApproaching (060027A6), yields exactly one `ldarg.0; call MountedCarrierPathObserver.FollowPrecomputedPathFrom`, no remaining FollowPrecomputedPath call, one more instruction than the original and the same opcode sequence elsewhere (short and long branch forms equated); UnpatchAll leaves no patch (' + $(if ($null -eq $directError22) { 'detour into the live original performed' } else { 'detour into the live original skipped for this method only, as in Test-GunslingerClickComposition.ps1' }) + ')')
Write-Host ('Mounted alpha 2 native contract (approach) PASS=' + $script:passed + ' FAIL=0')
