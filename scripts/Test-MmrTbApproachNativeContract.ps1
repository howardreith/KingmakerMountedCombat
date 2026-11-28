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
Check ($decideParameters.Count -eq 5 -and $decideParameters[0].Name -ceq 'movementContext' -and $decideParameters[3].IsOut -and $null -ne $commandType.GetMethod('LivePlanDisagreement', $any)) 'PR-2 R6-F2/R7-F2/R8-F5 (rounds 8-9): a first-tick decision receives the executing movement context, a commit states its granted metres (the adapter shortens the tick to them), and the live wrapper''s three-plan comparison is asked before any commit'
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
Check ($null -ne $timeDelta -and $null -ne $timeDelta.GetGetMethod($true) -and $timeDelta.PropertyType -eq [single]) 'PR-2 R10-F2 (round 11): installed TimeController.DeltaTime (the delta UnitMoveController passes to TickMovement) is readable at the click; an ordinary grant ending within one tick at that delta makes the click a provisional request decided at its first debit by the installed steering rule'
# ---- PR-2 round 12 (R10-F2, R11-F1, R11-F7, R11-F8): the installed turn-based frame bound and the product's retained obligations ----------------
$timeTickText = IlText ((GameType 'Kingmaker.Controllers.TimeController').GetMethod('Tick', $any))
Check ($timeTickText.Contains(';call get_PlayerTimeScale;ldc.r4 3;ble.un.s;ldarg.0;ldc.r4 3;call set_PlayerTimeScale;') -and
    $timeTickText.Contains(';call get_PlayerTimeScale;ldarg.0;ldfld DebugTimeScale;mul;ldc.r4 12;div;ldarg.0;ldfld m_JustActivated;brtrue.s;call get_deltaTime;br.s;call get_unscaledDeltaTime;call Min;call set_DeltaTime;') -and
    [single]$controllerType.GetField('NativeMaxTurnBasedFrameSeconds', $any).GetValue($null) -eq [single](3.0 / 12.0)) 'PR-2 R10-F2 (round 12): installed TimeController.Tick clamps the turn-based PlayerTimeScale to 3 and sets DeltaTime = Min(PlayerTimeScale x DebugTimeScale / 12, the frame delta), so no turn-based movement tick exceeds 3/12 s at DebugTimeScale 1 (the product''s NativeMaxTurnBasedFrameSeconds); a click whose ordinary allowance is within two such ticks is held provisional'
$rowsType = KmcType 'KingmakerMountedCombat.Integration.MountedMovementPersistenceRows'
$savedValues = KmcType 'KingmakerMountedCombat.Integration.SavedMovementValues'
Check ($null -ne $rowsType.GetMethod('Capture', $any) -and $null -ne $rowsType.GetMethod('Restore', $any) -and @($savedValues.GetProperties()).Count -eq 7 -and $null -eq $savedValues.GetProperty('MetresStepTravelled')) 'PR-2 R11-F1 (round 12): the saved allocation row keeps its versioned seven-field shape; the production row capture/restore carries the step commitment as the larger of the native counter and the settled physical travel and restores both limits from it'
Check ($null -ne $controllerType.GetMethod('SettleRetainedCarrierCleanups', $any) -and $null -ne $controllerType.GetMethod('SettleOutstandingEffects', $any) -and $null -eq $controllerType.GetField('settlingProposal', $any) -and
    $null -ne $controllerType.GetField('settlingProposals', $any)) 'PR-2 R11-F7/R11-F8 (round 12): terminal settlement retains an accepted command''s unfinished carrier cleanup, and every committed request with outstanding deferred effects is retained (a list, bound to its exact actors)'
Write-Host ('Mounted alpha 2 native contract (approach) PASS=' + $script:passed + ' FAIL=0')
