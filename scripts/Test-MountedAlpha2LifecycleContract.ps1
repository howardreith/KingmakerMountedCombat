[CmdletBinding()]
param([string]$Configuration = 'Release')
# Mounted Combat alpha 2, review round 4 (R3-F1, R3-F2, R3-F3, R1-F5, R1-F8, R1-F11, R2-F5): the installed Kingmaker 2.1.7b
# members the corrected production paths bind, and the compiled product's wiring to them. Offline, read-only:
#  1. Native acceptance: every UnitCommands queue insertion is inside AddToQueueInternal (060026B8) and every slot
#     acceptance (UnitCommand.OnRun) is inside the private Run overload (060026B3); KMC observes accepted work only from
#     their postfixes (never from the public Run prefix that precedes acceptance).
#  2. Hold: UnitMoveTo.OnAction (06002702) writes HoldState; KMC brackets it and restores the write only for its own carrier;
#     the native Hold input is observed for the mount.
#  3. Pre-start approach: native TickApproaching reaches the pair command's TickPreStartApproach; the command neither
#     overrides CanStart nor sets IsIgnoreCooldown (native start gate untouched); each delegated move is announced before
#     native Run; the start adopts the live carrier.
#  4. Turn-based click: the IgnoreClick postfix decides through PairedAttackClickAdmissionPolicy with the click probe (the
#     strike's production admission and the mount agent's native FindPath) and the mount's granted movement.
#  5. Native target comparison: the installed Character_Brain actions and considerations (exact serialized bytes at pinned
#     offsets of the pinned sharedassets1) and the brain context fields the proposal ranking reader reads.
# Not covered: any game run.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$lab = [IO.Path]::GetFullPath((Join-Path $repoRoot '..\..'))
$layout = (Get-Content -Raw -LiteralPath (Join-Path $lab 'environment-intake.json') | ConvertFrom-Json).requestedLayout
$data = Join-Path $layout.kingmakerInstallDir 'Kingmaker_Data'
$managed = Join-Path $data 'Managed'
$productDll = Join-Path $repoRoot ('bin\' + $Configuration + '\KingmakerMountedCombat.dll')
$script:passed = 0
function Check([bool]$Value, [string]$Label) { if (-not $Value) { throw ('Mounted alpha 2 lifecycle contract: ' + $Label) }; $script:passed++; Write-Host ('PASS ' + $Label) }
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
$any = [Reflection.BindingFlags]'Instance,Static,Public,NonPublic,DeclaredOnly'
$module = $game.ManifestModule
function MemberAt([int]$Token) { $module.ResolveMember($Token) }
# Call sites (call/callvirt/newobj/ldfld/stfld/ldsfld/stsfld/ldftn) resolved by the method's own module, in IL order.
$tokenOps = @{ 0x28 = 1; 0x6F = 1; 0x73 = 1; 0x7B = 1; 0x7D = 1; 0x7E = 1; 0x80 = 1; 0x72 = 1 }
function Refs([Reflection.MethodBase]$Method) {
    $body = $Method.GetMethodBody(); if ($null -eq $body) { return }
    $il = $body.GetILAsByteArray(); $found = New-Object System.Collections.ArrayList
    for ($i = 0; $i -le $il.Length - 5; $i++) {
        if (-not $tokenOps.ContainsKey([int]$il[$i])) { continue }
        $token = [BitConverter]::ToInt32($il, $i + 1)
        try {
            if ($il[$i] -eq 0x72) { [void]$found.Add([pscustomobject]@{ Offset = $i; Member = $null; Text = $Method.Module.ResolveString($token) }) }
            else { [void]$found.Add([pscustomobject]@{ Offset = $i; Member = $Method.Module.ResolveMember($token, $null, $null); Text = $null }) }
        } catch { }
    }
    $found.ToArray()
}
function Calls([Reflection.MethodBase]$Method, [Reflection.MemberInfo]$Target) { @(Refs $Method | Where-Object { $_.Member -eq $Target }).Count }
function FirstOffset([Reflection.MethodBase]$Method, [Reflection.MemberInfo]$Target) { $hit = @(Refs $Method | Where-Object { $_.Member -eq $Target }) | Select-Object -First 1; if ($hit) { $hit.Offset } else { -1 } }
function Method([Type]$Type, [string]$Name) { @($Type.GetMethods($any) | Where-Object { $_.Name -ceq $Name }) }

# ---- 1. Native acceptance ----------------------------------------------------------------------------------------------
$commands = GameType 'Kingmaker.UnitLogic.Commands.UnitCommands'
$command = GameType 'Kingmaker.UnitLogic.Commands.Base.UnitCommand'
$run = MemberAt 0x060026B3; $enqueue = MemberAt 0x060026B8
Check ($run.Name -ceq 'Run' -and $run.DeclaringType -eq $commands -and $run.GetParameters().Count -eq 3) 'R1-F5: 060026B3 is the private UnitCommands.Run(cmd, fromQueue, doNotClearQueue)'
Check ($enqueue.Name -ceq 'AddToQueueInternal' -and $enqueue.DeclaringType -eq $commands) 'R1-F5: 060026B8 is UnitCommands.AddToQueueInternal'
$queueType = (GameType 'Kingmaker.UnitLogic.Commands.UnitCommands').GetField('m_Queue', $any).FieldType
$addFirst = $queueType.GetMethod('AddFirst', [Type[]]@($command)); $addLast = $queueType.GetMethod('AddLast', [Type[]]@($command))
$inserters = @($commands.GetMethods($any) | Where-Object { (Calls $_ $addFirst) + (Calls $_ $addLast) -gt 0 })
Check ($inserters.Count -eq 1 -and $inserters[0] -eq $enqueue) 'R1-F5: every native queue insertion is inside AddToQueueInternal'
$onRun = $command.GetMethod('OnRun', $any)
$slotters = @($commands.GetMethods($any) | Where-Object { (Calls $_ $onRun) -gt 0 })
Check ($slotters.Count -eq 1 -and $slotters[0] -eq $run) 'R1-F5: every native slot acceptance (UnitCommand.OnRun) is inside the private Run overload'
$patches = KmcType 'KingmakerMountedCombat.Integration.MountedPatchController+PatchMethods'
$controller = KmcType 'KingmakerMountedCombat.Integration.MountedCombatController'
$observe = @(Method $controller 'ObserveAcceptedWork')[0]
Check ((Calls (@(Method $patches 'InternalRunPostfix')[0]) $observe) -eq 1 -and (Calls (@(Method $patches 'QueueAcceptedPostfix')[0]) $observe) -eq 1) 'R1-F5: accepted work is observed from the private Run postfix and the AddToQueueInternal postfix'
Check ((Calls (@(Method $controller 'AdmitPairCommandRun')[0]) $observe) -eq 0 -and (Calls (@(Method $patches 'UnitCommandRunPrefix')[0]) $observe) -eq 0) 'R1-F5: nothing is recorded before native acceptance (public Run prefix)'
$contains = $commands.GetMethod('Contains', [Type[]]@($command))
Check ((Calls (@(Method $patches 'InternalRunPostfix')[0]) $contains) -ge 1) 'R1-F5: the Run postfix checks that native holds the command'
$patchStrings = @(@($patches.DeclaringType.GetMethods($any)) + @($patches.DeclaringType.GetConstructors($any)) + @($patches.DeclaringType.GetNestedTypes($any).GetMethods($any)) | ForEach-Object { Refs $_ } | Where-Object { $_.Text } | ForEach-Object { $_.Text })
Check ($patchStrings -contains 'QueueAcceptedPostfix' -and $patchStrings -contains 'MoveActionHoldPrefix' -and $patchStrings -contains 'MoveActionHoldPostfix' -and $patchStrings -contains 'HoldPrefix') 'R1-F5/R1-F8: the queue-acceptance, carrier-Hold and Hold-input patches are registered'
$ledger = KmcType 'KingmakerMountedCombat.Domain.MountedRetainedOrderLedger'
$tryExecute = @(Method $controller 'TryExecuteAction')[0]
$issuing = (KmcType 'KingmakerMountedCombat.Integration.MountedPairAttackCommand').GetField('OwnCommandIssuing', $any)
Check ((Calls $tryExecute $issuing) -ge 1 -and (Calls $tryExecute ($ledger.GetMethod('RecordOwnIssued'))) -ge 1) 'R3-F3: the dispatcher records the order''s wrapper and subscribes to every move it issues'

# ---- 2. Hold ---------------------------------------------------------------------------------------------------------
$moveAction = MemberAt 0x06002702
$hold = (GameType 'Kingmaker.EntitySystem.Entities.UnitEntityData').GetField('HoldState')
Check ($moveAction.Name -ceq 'OnAction' -and $moveAction.DeclaringType.FullName -ceq 'Kingmaker.UnitLogic.Commands.UnitMoveTo' -and (Calls $moveAction $hold) -ge 1) 'R1-F8: 06002702 is UnitMoveTo.OnAction and writes HoldState'
$restore = @(Method $controller 'RestoreHoldAfterMoveAction')[0]
Check ((Calls (@(Method $patches 'MoveActionHoldPostfix')[0]) $restore) -eq 1 -and (Calls $restore ((KmcType 'KingmakerMountedCombat.Domain.MountedHoldProvenance').GetMethod('RestoreHoldAfterMoveAction'))) -eq 1) 'R1-F8: the OnAction Hold write is restored only through the own-carrier policy'
Check ((Calls (@(Method $patches 'HoldPrefix')[0]) (@(Method $controller 'ObserveHoldInput')[0])) -ge 1) 'R1-F8: the player''s Hold input is observed for the mount'
$forbidden = @(Method $controller 'AutomaticMovementForbidden')[0]
Check ((Calls $forbidden ((KmcType 'KingmakerMountedCombat.Domain.MountedHoldProvenance').GetMethod('AutomaticMovementForbidden'))) -eq 1) 'R1-F8: automatic movement reads the mount''s player Hold'

# ---- 3. Pre-start approach ---------------------------------------------------------------------------------------------
$pair = KmcType 'KingmakerMountedCombat.Integration.MountedPairAttackCommand'
$tick = @(Method $pair 'TickPreStartApproach')[0]
Check ((Calls (@(Method $patches 'MountedAttackApproachPrefix')[0]) $tick) -eq 1) 'R3-F1: native TickApproaching of the unstarted pair command drives its pre-start approach'
$tickApproaching = MemberAt 0x060027A6
Check ($tickApproaching.Name -ceq 'TickApproaching') 'R3-F1: 060027A6 is UnitCommand.TickApproaching (the patched native approach tick)'
$declaredNames = @($pair.GetMembers($any) | ForEach-Object { $_.Name })
Check (-not ($declaredNames -contains 'get_CanStart') -and -not ($declaredNames -contains 'get_IsIgnoreCooldown')) 'R3-F1: the pair command overrides neither CanStart nor IsIgnoreCooldown (native start gate untouched)'
Check (-not (@($pair.GetMethods($any) | Where-Object { (Calls $_ ($command.GetMethod('IgnoreCooldown', $any))) -gt 0 }).Count)) 'R3-F1: the pair command never asks native to ignore a cooldown'
$announce = @(Method $pair 'AnnounceOwnMove')[0]
$publicRun = MemberAt 0x060026B2
foreach ($name in @('BeginDelegatedMove', 'BeginPreStartMove')) {
    $m = @(Method $pair $name)[0]
    $a = FirstOffset $m $announce; $r = FirstOffset $m $publicRun
    Check ($a -ge 0 -and $r -gt $a) ('R3-F3: ' + $name + ' announces its exact move before native Run')
}
$adopted = $pair.GetProperty('PreStartMoveAdoptedAtStart', $any).GetSetMethod($true)
Check ((Calls (@(Method $pair 'OnStart')[0]) $adopted) -eq 1) 'R3-F1: native start adopts the live pre-start carrier (one movement owner)'
$dispatchEarly = (KmcType 'KingmakerMountedCombat.Domain.MountedPreStartApproachPolicy').GetMethod('DispatchBeforeStrikeReadiness')
Check ((Calls (@(Method $controller 'TryDriveStockAttackIntent')[0]) $dispatchEarly) -eq 1) 'R3-F1: real-time dispatch before readiness is decided by the production policy'
$canAct = (GameType 'Kingmaker.Controllers.Combat.UnitCombatState').GetProperty('CanActInCombat').GetGetMethod()
$should = @(Method (GameType 'Kingmaker.Controllers.Units.UnitActionController') 'ShouldStartCommand')[0]
$cooldowns = @(Method (GameType 'Kingmaker.Controllers.Combat.UnitCombatState') 'HasCooldownForCommand')
Check ((Calls $should $canAct) -ge 1 -and (@($cooldowns | Where-Object { (Calls $should $_) -ge 1 }).Count) -ge 1) 'R3-F1: native ShouldStartCommand gates the start on CanActInCombat and the cooldown'

# ---- 4. Turn-based click --------------------------------------------------------------------------------------------
$participant = KmcType 'KingmakerMountedCombat.Integration.MountedPairTurnParticipant'
$admit = @(Method $participant 'AdmitPairedAttackClick')[0]
$policy = (KmcType 'KingmakerMountedCombat.Domain.PairedAttackClickAdmissionPolicy').GetMethod('Decide')
$probe = (KmcType 'KingmakerMountedCombat.Integration.PairedAttackClickProbe').GetMethod('Evaluate', $any)
$granted = (KmcType 'KingmakerMountedCombat.Integration.MountedMovementStateAdapter').GetMethod('RemainingGrantedMetres', $any)
Check ((Calls $admit $policy) -eq 1 -and (Calls $admit $probe) -eq 1 -and (Calls $admit $granted) -eq 1) 'R3-F2: the IgnoreClick postfix decides through the policy, the probe and the mount''s granted movement'
$findPath = (GameType 'Kingmaker.View.UnitMovementAgent').GetMethod('FindPath')
$closeEnough = $command.GetMethod('IsUnitCloseEnough', [Reflection.BindingFlags]'Static,Public')
$evaluate = (KmcType 'KingmakerMountedCombat.Integration.MountedPairSingleAttack').GetMethod('EvaluateCurrentNativeAdmission', $any)
Check ((Calls $probe $findPath) -eq 1 -and (Calls $probe $closeEnough) -eq 1 -and (Calls $probe $evaluate) -eq 1) 'R3-F2: the probe uses the strike''s production admission, the mount agent''s native FindPath and native IsUnitCloseEnough'
$visual = @(Method (GameType 'Kingmaker.TurnBasedMode.PathVisualizer') 'CalculatePathForCommand')[0]
Check ((Calls $visual $findPath) -eq 1) 'R3-F2: native path preview computes its route with the same UnitMovementAgent.FindPath'

# ---- 5. Native target comparison --------------------------------------------------------------------------------------
$assets = Join-Path $data 'sharedassets1.assets'
Check ((Get-FileHash -LiteralPath $assets).Hash.ToLowerInvariant() -ceq 'cc779caf2fef21d111856a57d40b510677b0c236ad2723d1849564626445f785') 'pinned sharedassets1 identity'
$stream = [IO.File]::OpenRead($assets)
try {
    function Tail([long]$Offset, [int]$Size, [int]$Count) { $buffer = New-Object byte[] $Count; [void]$stream.Seek($Offset + $Size - $Count, [IO.SeekOrigin]::Begin); [void]$stream.Read($buffer, 0, $Count); ([BitConverter]::ToString($buffer) -replace '-', '').ToLowerInvariant() }
    Check ((Tail 151003888 128 28) -ceq '0200000000000000a319010000000000000000004219010000000000') 'R1-F11: Character_Brain actions = [ManualAttackAiAction 72099, AttackAiAction 72002]'
    Check ((Tail 150987592 168 52) -ceq '00000000000000000100000001000000000020410100000000000000361a0100000000000100000000000000471a010000000000') 'R1-F11: ManualAttackAiAction: UseWhenAIDisabled, BaseScore 10, actor HasManualTarget 72246, target ManualOnly 72263'
    Check ((Tail 151014240 112 8) -ceq '0000803f00000000') 'R1-F11: HasManualTarget scores 1 with a manual target, 0 without'
    Check ((Tail 151016368 112 16) -ceq '0000803f0000803f0000000000000000') 'R1-F11: ManualOnly scores the manual target 1, others 0'
    Check ((Tail 150968680 152 28) -ceq '010000000000803f0000000001000000000000002c1a010000000000') 'R1-F11: AttackAiAction: BaseScore 1, target AttackTargetsPriority 72236'
    Check ((Tail 151012904 152 44) -ceq '0000803f03000000000000009c1a01000000000000000000a71a01000000000000000000981a010000000000') 'R1-F11: AttackTargetsPriority = [StandStill 72348, UnconsciousPenalty 72359, NotImpatient 72344]'
    Check ((Tail 151027688 112 16) -ceq '0000803f0000803fcdcc4c3f00000000') 'R1-F11: StandStill (InRange) scores 1.0 in reach, 0.8 outside'
    Check ((Tail 151029072 120 16) -ceq '0000803f0000803f000000006f12833a') 'R1-F11: UnconsciousPenalty (LifeState) alive 1, dead 0, unconscious 0.001'
    Check ((Tail 151027184 108 12) -ceq '0000803f0000003f0000803f') 'R1-F11: NotImpatient disliked 0.5, otherwise 1.0'
} finally { $stream.Dispose() }
$brain = GameType 'Kingmaker.Controllers.Brain.AiBrainController'
$decision = GameType 'Kingmaker.Controllers.Brain.DecisionContext'
$select = @(Method $brain 'SelectAction')[0]
Check ((Calls $select ($decision.GetProperty('Enemies').GetGetMethod())) -ge 1 -and (Calls $select ((GameType 'Kingmaker.UnitLogic.AiAction').GetMethod('ScoreTarget'))) -ge 1 -and (Calls $select ($decision.GetField('BestScore'))) -ge 1) 'R1-F11: SelectAction walks the sorted enemies, scores each target and keeps the best score'
$complex = @(Method (GameType 'Kingmaker.Controllers.Brain.Blueprints.Considerations.ComplexConsideration') 'Score')[0]
Check ((Calls $complex ($decision.GetField('CurrentScore'))) -ge 1) 'R1-F11: ComplexConsideration multiplies the running score'
$contextField = MemberAt 0x04005EC9; $enemiesField = MemberAt 0x04005ECF
$reader = KmcType 'KingmakerMountedCombat.Integration.NativeBrainRankingReader'
Check ($reader.GetField('ContextField', $any).GetValue($null) -eq $contextField -and $reader.GetField('EnemiesField', $any).GetValue($null) -eq $enemiesField) 'R1-F11: the proposal ranking reader reads AiBrainController.s_Context and DecisionContext.m_Enemies (04005EC9, 04005ECF)'
Check ((Calls (@(Method $controller 'ObserveAutomaticProposal')[0]) ($reader.GetMethod('Describe', $any))) -eq 1) 'R1-F11: every real-time automatic proposal carries native''s own ranking'
Write-Host ('Mounted alpha 2 lifecycle contract PASS=' + $script:passed + ' FAIL=0')
