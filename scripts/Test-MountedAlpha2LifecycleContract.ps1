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
# Calls made by a method and by the lambdas it defines (compiler-generated '<Name>b__' methods on the type or its closure classes).
function CallsWithin([Type]$Type, [string]$Name, [Reflection.MemberInfo]$Target) {
    $total = 0
    foreach ($t in @($Type) + @($Type.GetNestedTypes($any))) {
        foreach ($m in @($t.GetMethods($any))) {
            if ($m.Name -ceq $Name -and $t -eq $Type -or $m.Name.StartsWith('<' + $Name + '>')) { $total += (Calls $m $Target) }
        }
    }
    $total
}

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
$preStartPolicy = KmcType 'KingmakerMountedCombat.Domain.MountedPreStartApproachPolicy'
$drive = @(Method $controller 'TryDriveStockAttackIntent')[0]
Check ((Calls $drive ($preStartPolicy.GetMethod('DispatchBeforeStrikeReadiness'))) -eq 1 -and (Calls $drive ($preStartPolicy.GetMethod('SelectStriker'))) -eq 1) 'R3-F1: real-time dispatch before readiness selects the rider or horse striker by the production policy'
Check ((Calls $drive (@(Method $controller 'PlannedStrikeAdmissibleNow')[0])) -eq 1 -and (Calls (@(Method $controller 'PlannedStrikeAdmissibleNow')[0]) ((KmcType 'KingmakerMountedCombat.Integration.MountedPairSingleAttack').GetMethod('EvaluateCurrentNativeAdmission', $any))) -eq 1) 'R3-F1: the initial reach decision is the planned strike''s production admission'
Check ((Calls $tick (@(Method $pair 'TickNativePreStartApproach')[0])) -eq 1) 'R3-F1: a horse strike waiting to start approaches with native''s own approach of that command'
$tickOnUnit = @((GameType 'Kingmaker.Controllers.Units.UnitActionController').GetMethods($any) | Where-Object { (Calls $_ (@(Method (GameType 'Kingmaker.Controllers.Units.UnitActionController') 'TickCommand')[0])) -gt 0 })
Check ($tickOnUnit.Count -eq 1 -and (Calls $tickOnUnit[0] ($commands.GetProperty('Raw').GetGetMethod())) -ge 1 -and (Calls $tickOnUnit[0] ($commands.GetProperty('Queue').GetGetMethod())) -eq 0) 'R3-F1: native ticks only slot commands (Raw), never the queue: a queued order moves only after promotion, as an unmounted one'
$canAct = (GameType 'Kingmaker.Controllers.Combat.UnitCombatState').GetProperty('CanActInCombat').GetGetMethod()
$should = @(Method (GameType 'Kingmaker.Controllers.Units.UnitActionController') 'ShouldStartCommand')[0]
$cooldowns = @(Method (GameType 'Kingmaker.Controllers.Combat.UnitCombatState') 'HasCooldownForCommand')
Check ((Calls $should $canAct) -ge 1 -and (@($cooldowns | Where-Object { (Calls $should $_) -ge 1 }).Count) -ge 1) 'R3-F1: native ShouldStartCommand gates the start on CanActInCombat and the cooldown'

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
# ---- 6. Round-4 corrections (R4-F2, R4-F3, R4-F8, R1-F11) ---------------------------------------------------------------
$endUnperceived = @(Method $pair 'EndIfAutomaticTargetUnperceived')[0]
foreach ($name in @('OnStart', 'TickApproach', 'TickPreStartApproach', 'StartChildAttack')) { Check ((Calls (@(Method $pair $name)[0]) $endUnperceived) -ge 1) ('R4-F2: ' + $name + ' ends an automatic order whose target left perception before any destination') }
Check ((Calls $tryExecute ($pair.GetField('AutomaticTargetPerceived', $any))) -ge 1) 'R4-F2: every automatic order carries the rider group''s perception check'
Check ((Calls (@(Method $controller 'TryRetireStockIntent')[0]) ($ledger.GetMethod('NewerPlayerWork'))) -eq 1) 'R4-F3: retirement distinguishes work accepted after an explicit order from older occupied slots'
Check ((Calls (@(Method $controller 'Update')[0]) ($ledger.GetMethod('ReleaseFinished'))) -eq 1) 'R4-F8: finished command identities are released every update'
$tieBreak = (KmcType 'KingmakerMountedCombat.Domain.NativeAttackTieBreak').GetMethod('Choose')
Check ((Calls (@(Method $controller 'ObserveAutomaticProposal')[0]) (@(Method $controller 'ResolveEqualRankTie')[0])) -eq 1 -and (Calls (@(Method $controller 'ResolveEqualRankTie')[0]) $tieBreak) -eq 1) 'R1-F11: equally ranked native candidates are resolved by the pair''s nearest-eligible tie-break before admission'
$engage = @(Method $controller 'ObserveEngagementProgress')[0]
Check ((Calls $engage ($pair.GetProperty('CurrentMovementOwner', $any).GetGetMethod($true))) -eq 1 -and (Calls $engage ((KmcType 'KingmakerMountedCombat.Domain.MountedEngagementRecord').GetMethod('ObserveInterval'))) -eq 1) 'R1-F11: travel is attributed to the exact carrier across each interval'
Check (@(@(Method $patches 'InternalRunPostfix')[0].GetParameters() | Where-Object { $_.Name -ceq 'fromQueue' -and $_.ParameterType -eq [bool] }).Count -eq 1) 'R5-F5: the Run postfix receives native''s own fromQueue (promotion) parameter'
# ---- 7. PR-1 round-1 corrections: the compiled controller binds each corrected path ------------------------------------------
$drive = @(Method $controller 'TryDriveStockAttackIntent')[0]
$planner = KmcType 'KingmakerMountedCombat.Domain.MountedDispatchPlanner'
Check ((CallsWithin $controller 'TryDriveStockAttackIntent' (@(Method $controller 'FollowThroughStrikeAdmissibleNow')[0])) -eq 1 -and (Calls $drive ($planner.GetMethod('Plan'))) -eq 1 -and (Calls ($planner.GetMethod('Plan')) ((KmcType 'KingmakerMountedCombat.Domain.UnifiedMountedStockAttackPolicy').GetMethod('MountJoinsIntent'))) -eq 1 -and (Calls (@(Method $controller 'FollowThroughStrikeAdmissibleNow')[0]) (@(Method $controller 'PlannedStrikeAdmissibleNow')[0])) -eq 1 -and @((KmcType 'KingmakerMountedCombat.Domain.UnifiedMountedStockAttackPolicy').GetMethod('MountJoinsIntent').GetParameters()).Count -eq 9) 'PR-1 R1-F1/R3-F4/R3-F5: the drive dispatches through the production MountedDispatchPlanner.Plan, whose MountJoinsIntent reads the planned horse strike''s own admission (the drive''s probe lambda)'
Check ((Calls (@(Method $controller 'SettleOrderTerminal')[0]) ($ledger.GetMethod('MarkRiderEnded'))) -ge 1 -and (Calls $drive ($ledger.GetMethod('RiderEnded'))) -ge 1) 'PR-1 R1-F3: an actor-specific rider end keeps the order for the horse and is read at the next drive'
Check ((Calls (@(Method $controller 'SweepOrphanedAutomaticCommand')[0]) ($controller.GetProperty('ExplicitOrphanReleasedCount', $any).GetSetMethod($true))) -ge 1) 'PR-1 R1-F5: an explicit wrapper dropped from slot and queue is released by the sweep'
$retireHidden = @(Method $controller 'RetireAutomaticIntentIfUnperceived')[0]
Check ((Calls $drive $retireHidden) -ge 1 -and (FirstOffset $drive $retireHidden) -lt (FirstOffset $drive (@(Method $controller 'PlannedStrikeAdmissibleNow')[0])) -and (Calls (@(Method $controller 'ObserveAutomaticProposal')[0]) $retireHidden) -ge 1) 'PR-1 R1-F9: a hidden automatic target is released before any readiness or geometry probe and before it can refuse a proposal'
$engageReport = (KmcType 'KingmakerMountedCombat.Domain.MountedEngagementRecord').GetProperty('Reported').GetGetMethod()
Check ((Calls (@(Method $controller 'ObserveEngagementProgress')[0]) $engageReport) -ge 1) 'PR-1 R1-F13: a completed one-shot engagement record stops sampling'
# ---- 8. PR-1 round-4 lifecycle correction (docs/missions/mmr-split/PR-1-lifecycle-record.md): native facts and compiled glue ---------
$nativeCommand = GameType 'Kingmaker.UnitLogic.Commands.Base.UnitCommand'
$nativeTick = @(Method $nativeCommand 'Tick')[0]
$nativeOnTick = @(Method $nativeCommand 'OnTick')[0]; $nativeOnAction = @(Method $nativeCommand 'OnAction')[0]
$isFinished = $nativeCommand.GetProperty('IsFinished').GetGetMethod(); $setActed = $nativeCommand.GetProperty('IsActed').GetSetMethod($true)
$tickIl = $nativeTick.GetMethodBody().GetILAsByteArray(); $deadline = $false
for ($i = 0; $i -le $tickIl.Length - 5; $i++) { if ($tickIl[$i] -eq 0x22 -and [BitConverter]::ToSingle($tickIl, $i + 1) -eq 9) { $deadline = $true } }
Check ((FirstOffset $nativeTick $nativeOnTick) -ge 0 -and (FirstOffset $nativeTick $nativeOnTick) -lt (FirstOffset $nativeTick $isFinished) -and (FirstOffset $nativeTick $isFinished) -lt (FirstOffset $nativeTick $nativeOnAction) -and
    (FirstOffset $nativeTick $nativeOnAction) -lt (FirstOffset $nativeTick $setActed) -and $deadline -and (Calls $nativeTick (@(Method $nativeCommand 'Interrupt')[0])) -ge 1) 'PR-1 R2-F1/R1-F3/R2-F3 native: UnitCommand.Tick runs OnTick, returns if finished, then Result = OnAction() and only then IsActed; a started command past 9 s is interrupted'
$forceFinish = @($nativeCommand.GetMethods($any) | Where-Object { $_.Name -ceq 'ForceFinish' })[0]
Check ((Calls $forceFinish $nativeOnAction) -eq 0 -and (Calls $forceFinish (@(Method $nativeCommand 'SetIsActed')[0])) -ge 1) 'PR-1 R1-F3 native: ForceFinish (ForceFinishForTurnBased) marks IsActed without calling OnAction'
$released = $pair.GetProperty('StrikeReleased', $any).GetGetMethod($true); $delivered = $pair.GetField('strikeDelivered', $any)
$pairOnAction = @($pair.GetMethods($any) | Where-Object { $_.Name -ceq 'OnAction' })[0]
$attackOnAction = $pair.BaseType.GetMethod('OnAction', [Reflection.BindingFlags]'Instance,Public,NonPublic'); $attackOnAction = $attackOnAction.DeclaringType.GetMethod('OnAction', $any)
Check ((Calls $released ($nativeCommand.GetProperty('IsActed').GetGetMethod())) -eq 0 -and (Calls $released $delivered) -ge 1 -and (FirstOffset $pairOnAction $delivered) -ge 0 -and
    (FirstOffset $pairOnAction $delivered) -lt (FirstOffset $pairOnAction $attackOnAction)) 'PR-1 R1-F3/R2-F1: delivery is the wrapper''s own OnAction (marked before native UnitAttack.OnAction), never IsActed'
$terminalHandler = @(Method $controller 'SettleOrderTerminal')[0]
Check ((Calls (@(Method $controller 'HandleCommandTerminal')[0]) $terminalHandler) -eq 1) 'PR-1 R4-F5: HandleCommandTerminal settles the order through SettleOrderTerminal (the transition the attack-path harness executes)'
Check ((Calls $terminalHandler ($ledger.GetMethod('DecideTerminal'))) -eq 1 -and (Calls $terminalHandler $released) -ge 1 -and (Calls $terminalHandler ($nativeCommand.GetProperty('IsActed').GetGetMethod())) -eq 0 -and
    (Calls $terminalHandler ($ledger.GetMethod('MarkRiderEnded'))) -eq 1 -and (Calls $drive ($ledger.GetMethod('RiderEnded'))) -ge 1) 'PR-1 R1-F3: the terminal disposition comes from DecideTerminal over the wrapper''s delivery facts; a rider end is read at the next drive'
Check ((Calls $terminalHandler ($ledger.GetMethod('HoldRiderRefusal'))) -eq 1 -and (Calls $terminalHandler ($ledger.GetMethod('SettleRefusalDelivered'))) -eq 1 -and (Calls $terminalHandler ($ledger.GetMethod('TakeRefusalOnRetirement'))) -eq 1 -and
    (Calls $drive (@(Method $controller 'FinishTurnBasedIntent')[0])) -eq 1 -and (Calls (@(Method $controller 'FinishTurnBasedIntent')[0]) ($ledger.GetMethod('TakeRefusalOnRetirement'))) -eq 1 -and $null -eq $controller.GetField('riderRefusalAwaitingHorse', $any) -and
    (Calls ($ledger.GetMethod('Clear')) ($ledger.GetMethod('DropRefusal', $any))) -eq 1 -and (Calls ($ledger.GetMethod('Begin')) ($ledger.GetMethod('DropRefusal', $any))) -eq 1) 'PR-1 R2-F4: the pending refusal lives in the generation''s ledger, settles on delivery or final retirement and is dropped on every Begin/Clear'
$retireWrapper = @(Method $controller 'RetireOrderWrapper')[0]
$singleType = KmcType 'KingmakerMountedCombat.Integration.MountedPairSingleAttack'
$orderRetired = $singleType.GetProperty('OrderRetired', $any).GetGetMethod($true)
$markRetired = @(Method $singleType 'MarkOrderRetired')[0]
$collections = @($controller.GetFields($any) | Where-Object { $_.FieldType.IsGenericType -and @($_.FieldType.GetGenericArguments() | Where-Object { $singleType.IsAssignableFrom($_) }).Count -gt 0 })
Check ((Calls $retireWrapper ($ledger.GetMethod('RetireWrapper'))) -eq 1 -and (Calls $retireWrapper $markRetired) -ge 2 -and (Calls $retireWrapper $released) -eq 1 -and
    (Calls (@(Method $controller 'TryRetireStockIntent')[0]) $retireWrapper) -eq 1 -and (Calls (@(Method $controller 'RetireSupersededPlayerOrder')[0]) $retireWrapper) -eq 1 -and
    (Calls (@(Method $controller 'RetireAutomaticIntentIfUnperceived')[0]) $retireWrapper) -eq 1 -and (Calls (@(Method $controller 'RetireOrderForCancel')[0]) $retireWrapper) -eq 2 -and
    (Calls (@(Method $controller 'RetireOrderOnTargetInvalidation')[0]) $retireWrapper) -eq 1 -and $collections.Count -eq 0) ('PR-1 R2-F1/R3-F1/R3-F2/R4-F1: turn-based, real-time, perception, cancellation and invalidation retirement share RetireOrderWrapper; turn-based, real-time and perception retirement share RetireOrderWrapper (released strike left to native, retirement held on the wrapper); no controller collection holds wrappers (' + (($collections | ForEach-Object { $_.Name }) -join ',') + ')')
Check ((Calls (@(Method $controller 'AdmitQueuedExplicitRun')[0]) $orderRetired) -eq 1 -and (Calls (@(Method $controller 'AdmitQueuedAutomaticRun')[0]) $orderRetired) -eq 1 -and
    (Calls (@(Method $patches 'InternalRunPrefix')[0]) (@(Method $patches 'AdmitExplicitPromotion')[0])) -eq 1 -and
    (FirstOffset (@(Method $patches 'AdmitExplicitPromotion')[0]) $orderRetired) -ge 0 -and (FirstOffset (@(Method $patches 'AdmitExplicitPromotion')[0]) $orderRetired) -lt (FirstOffset (@(Method $patches 'AdmitExplicitPromotion')[0]) (@(Method $controller 'AdmitQueuedExplicitRun')[0]))) 'PR-1 R3-F1: explicit and automatic queue promotion refuse a wrapper retired with its order, the explicit one before any owner lookup (also after detachment)'
$cancelCore = @(Method $controller 'CancelCore')[0]
$freshClick = @($controller.GetMethods($any) | Where-Object { @(Refs $_ | Where-Object { $_.Text -ceq 'new stock hostile attack' }).Count -gt 0 })
Check ((Calls (@(Method $controller 'Cancel')[0]) $cancelCore) -eq 1 -and (Calls $cancelCore (@(Method $controller 'RetireOrderForCancel')[0])) -eq 1 -and $freshClick.Count -eq 1 -and (Calls $freshClick[0] $cancelCore) -eq 1 -and
    (Calls $drive (@(Method $controller 'RetireOrderOnTargetInvalidation')[0])) -eq 1) 'PR-1 R4-F1/R3-F1: a fresh click cancels through CancelCore (delivery-aware retargeting); target invalidation retires the queued wrapper'
Check ((Calls (@(Method $controller 'TryRetireStockIntent')[0]) (@(Method $controller 'StockIntentStrikeAdmissibleNow')[0])) -eq 1 -and (Calls (@(Method $controller 'StockIntentStrikeAdmissibleNow')[0]) (@(Method $pair 'StrikeAdmissibleNow')[0])) -eq 1) 'PR-1 R4-F3: restricted automatic movement retires only a strike that is not admissible from the current position'
$describe = @(Method $pair 'DescribeOutcome')[0]
Check ((Calls $describe $released) -ge 2) 'PR-1 R4-F6: the outcome attributes a Standard charge only to a delivered strike (a force-finish spends nothing)'
Check (@(Refs (@(Method $patches 'ClickUnitPrefix')[0]) | Where-Object { $null -ne $_.Member -and ($_.Member.Name -ceq 'get_SelectedUnits' -or $_.Member.Name -ceq 'SelectedUnits') }).Count -eq 1 -and (Calls (@(Method $patches 'ClickUnitPrefix')[0]) (@(Method $controller 'BeginClickObservation')[0])) -eq 1) 'PR-1 R1-F11: the click bracket records which pair actors the click commanded (its selection)'
$ownership = KmcType 'KingmakerMountedCombat.Domain.MountedManualTargetOwnership'
$observeAccepted = @(Method $controller 'ObserveAcceptedWork')[0]
Check ((Calls (@(Method $controller 'ClearStockAttackIntent')[0]) ($ownership.GetMethod('ClearsOnRetirement'))) -eq 2 -and (Calls $observeAccepted ($ownership.GetMethod('ObserveForeignAssignment'))) -eq 1 -and
    (FirstOffset $observeAccepted ($ownership.GetMethod('ObserveForeignAssignment'))) -lt (FirstOffset $observeAccepted ($ledger.GetMethod('Classify'))) -and
    (Calls (@(Method $controller 'OwnManualTarget')[0]) ($ownership.GetMethod('RecordOwnWrite'))) -eq 1 -and
    (Calls (@(Method $patches 'InternalRunPostfix')[0]) ((KmcType 'KingmakerMountedCombat.Domain.MountedAcceptedWorkRoutes').GetMethod('AfterRun'))) -eq 1) 'PR-1 R1-F11: ownership follows the actual assignment, observed before the competing classification; only an installed promotion is observed; retirement clears only a still-owned selection'
$patchRoot = KmcType 'KingmakerMountedCombat.Integration.MountedPatchController'
$clickRegistered = $false
foreach ($m in @($patchRoot.GetMethods($any)) + @($patchRoot.GetConstructors($any))) {
    $body = $m.GetMethodBody(); if ($null -eq $body) { continue }; $il = $body.GetILAsByteArray(); $strings = @{}
    for ($i = 0; $i -le $il.Length - 5; $i++) { if ($il[$i] -eq 0x72) { try { $strings[$m.Module.ResolveString([BitConverter]::ToInt32($il, $i + 1))] = $true } catch { } } }
    if ($strings.ContainsKey('OnClick') -and $strings.ContainsKey('ClickUnitPrefix') -and $strings.ContainsKey('ClickUnitPostfix')) { $clickRegistered = $true }
}
$onClick = @((GameType 'Kingmaker.Controllers.Clicks.Handlers.ClickUnitHandler').GetMethods($any) | Where-Object { $_.Name -ceq 'OnClick' -and $_.MetadataToken -eq 0x060093ED })
Check ($clickRegistered -and $onClick.Count -eq 1 -and (Calls (@(Method $patches 'ClickUnitPrefix')[0]) (@(Method $controller 'BeginClickObservation')[0])) -eq 1 -and
    (Calls (@(Method $patches 'ClickUnitPostfix')[0]) (@(Method $controller 'EndClickObservation')[0])) -eq 1 -and
    (Calls (@(Method $controller 'EndClickObservation')[0]) ($ownership.GetMethod('ObserveClickAssignment'))) -eq 2 -and
    (Calls $onClick[0] ((GameType 'Kingmaker.Controllers.Combat.UnitCombatState').GetProperty('ManualTarget').GetSetMethod())) -ge 1) 'PR-1 R1-F11: ClickUnitHandler.OnClick (060093ED, which assigns ManualTarget after its Run) is bracketed so that assignment is attributed'
$pairOnTick = @($pair.GetMethods($any) | Where-Object { $_.Name -ceq 'OnTick' })[0]
$hold = @(Method $pair 'HoldClockWhileDeferred')[0]
Check ((Calls $pairOnTick $hold) -eq 1 -and (FirstOffset $pairOnTick $hold) -lt (FirstOffset $pairOnTick (@(Method $pair 'TickApproach')[0])) -and (Calls $hold (@(Method $nativeCommand 'SetTimeSinceStart')[0])) -eq 1 -and
    (Calls (@(Method $pair 'StartChildAttack')[0]) ($pair.GetField('approachAllowanceSeconds', $any))) -ge 1 -and $null -eq $pair.GetField('deferredSeconds', $any)) 'PR-1 R2-F3: a waiting carrier holds TimeSinceStart (native 9 s deadline and the KMC bound); the attack restarts its own clock and bound'
$tickApproach = @(Method $pair 'TickApproach')[0]
Check ((FirstOffset $tickApproach (@(Method $pair 'EndIfMovementForbiddenAtApproach')[0])) -gt (FirstOffset $tickApproach (@(Method $singleType 'EvaluateCurrentNativeAdmission')[0]))) 'PR-1 R3-F3: movement revocation is applied only after the current strike admission is evaluated'
Check ((Calls (@(Method $pair 'TryRiderSightGoal')[0]) (@(Method $pair 'NearFailedSightGoal')[0])) -eq 1 -and (Calls $tickApproach ($pair.GetField('failedSightGoals', $any))) -ge 1 -and (Calls (@(Method $pair 'NewApproachMove')[0]) ($pair.GetField('currentSightGoal', $any))) -ge 1) 'PR-1 R1-F7: sight goals needing no movement are skipped; a goal reached without admission is excluded with bounded alternatives'
Write-Host ('Mounted alpha 2 lifecycle contract PASS=' + $script:passed + ' FAIL=0 nativeProof=false')
