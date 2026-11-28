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
# PR #70 R11-F8 (round 13): the private Run postfix observes through MountedAcceptedWorkRouting.Route (the actor's owner, plus a former owner that
# still owes an outstanding operation on that exact actor).
$routing = KmcType 'KingmakerMountedCombat.Integration.MountedAcceptedWorkRouting'
$route = @(Method $routing 'Route')[0]
$observeOutstanding = @(Method $controller 'ObserveOutstandingAssignment')[0]
Check ((Calls (@(Method $patches 'InternalRunPostfix')[0]) $route) -eq 1 -and (Calls $route $observe) -eq 1 -and (Calls $route $observeOutstanding) -eq 1 -and
    (Calls $route (@(Method $controller 'ObservesOutstandingActor')[0])) -eq 1 -and (Calls $observe $observeOutstanding) -eq 1 -and
    (Calls (@(Method $patches 'QueueAcceptedPostfix')[0]) $observe) -eq 1) 'R1-F5: accepted work is observed from the private Run postfix (through the accepted-work routing, R11-F8) and the AddToQueueInternal postfix'
Check ((Calls (@(Method $controller 'AdmitPairCommandRun')[0]) $observe) -eq 0 -and (Calls (@(Method $patches 'UnitCommandRunPrefix')[0]) $observe) -eq 0) 'R1-F5: nothing is recorded before native acceptance (public Run prefix)'
$contains = $commands.GetMethod('Contains', [Type[]]@($command))
Check ((Calls (@(Method $patches 'InternalRunPostfix')[0]) $route) -eq 1 -and (Calls $route $contains) -ge 1) 'R1-F5: the Run postfix checks that native holds the command (in its accepted-work routing, round 13)'
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
$dispatchPart = @(Method $controller 'PlanStockDispatch')[0]
Check ((Calls $drive $dispatchPart) -eq 1 -and (Calls $dispatchPart ($preStartPolicy.GetMethod('DispatchBeforeStrikeReadiness'))) -eq 1 -and (Calls $dispatchPart ($preStartPolicy.GetMethod('SelectStriker'))) -eq 1) 'R3-F1: real-time dispatch before readiness selects the rider or horse striker by the production policy (in the drive''s dispatch operation)'
Check ((Calls $dispatchPart (@(Method $controller 'PlannedStrikeAdmissibleNow')[0])) -eq 1 -and (Calls (@(Method $controller 'PlannedStrikeAdmissibleNow')[0]) ((KmcType 'KingmakerMountedCombat.Integration.MountedPairSingleAttack').GetMethod('EvaluateCurrentNativeAdmission', $any))) -eq 1) 'R3-F1: the initial reach decision is the planned strike''s production admission'
Check ((Calls $tick (@(Method $pair 'TickNativePreStartApproach')[0])) -eq 1) 'R3-F1: a horse strike waiting to start approaches with native''s own approach of that command'
$tickOnUnit = @((GameType 'Kingmaker.Controllers.Units.UnitActionController').GetMethods($any) | Where-Object { (Calls $_ (@(Method (GameType 'Kingmaker.Controllers.Units.UnitActionController') 'TickCommand')[0])) -gt 0 })
Check ($tickOnUnit.Count -eq 1 -and (Calls $tickOnUnit[0] ($commands.GetProperty('Raw').GetGetMethod())) -ge 1 -and (Calls $tickOnUnit[0] ($commands.GetProperty('Queue').GetGetMethod())) -eq 0) 'R3-F1: native ticks only slot commands (Raw), never the queue: a queued order moves only after promotion, as an unmounted one'
$canAct = (GameType 'Kingmaker.Controllers.Combat.UnitCombatState').GetProperty('CanActInCombat').GetGetMethod()
$should = @(Method (GameType 'Kingmaker.Controllers.Units.UnitActionController') 'ShouldStartCommand')[0]
$cooldowns = @(Method (GameType 'Kingmaker.Controllers.Combat.UnitCombatState') 'HasCooldownForCommand')
Check ((Calls $should $canAct) -ge 1 -and (@($cooldowns | Where-Object { (Calls $should $_) -ge 1 }).Count) -ge 1) 'R3-F1: native ShouldStartCommand gates the start on CanActInCombat and the cooldown'

# ---- 4. Turn-based click --------------------------------------------------------------------------------------------
$participant = KmcType 'KingmakerMountedCombat.Integration.MountedPairTurnParticipant'
$admit = @(Method $participant 'AdmitPairedAttackClick')[0]
$policy = (KmcType 'KingmakerMountedCombat.Domain.PairedAttackClickAdmissionPolicy').GetMethod('Decide')
$probe = (KmcType 'KingmakerMountedCombat.Integration.PairedAttackClickProbe').GetMethod('Evaluate', $any)
$movementState = KmcType 'KingmakerMountedCombat.Integration.MountedMovementStateAdapter'
# PR #70 R14-F2 (round 15): the click's reader calls the priced overload (the grant with its step-counter classification from the same speed reads).
$grantHelper = @(Method $participant 'PairedMountGrantedMetres' | Where-Object { @($_.GetParameters() | ForEach-Object { $_.Name }) -contains 'stepCounterBinds' })[0]
$grantEntry = @(Method $participant 'PairedMountGrantedMetres' | Where-Object { @($_.GetParameters() | ForEach-Object { $_.Name }) -notcontains 'stepCounterBinds' })[0]
Check ((Calls $grantEntry $grantHelper) -eq 1) 'R14-F2 (PR #70 round 15): the unpriced grant entry is the priced grant itself (one implementation)'
# PR #70 round 2: the decision moved into the controller's AdmitTurnBasedAttackClick (spliced into the attack-path harness); the postfix
# reads the native input and passes the shared mount grant as a reader (a closure calling PairedMountGrantedMetres).
$admitCore = @(Method (KmcType 'KingmakerMountedCombat.Integration.MountedCombatController') 'AdmitTurnBasedAttackClick')[0]
$grantReaders = @($participant.GetNestedTypes($any) | ForEach-Object { $_.GetMethods($any) } | Where-Object { (Calls $_ $grantHelper) -ge 1 })
Check ((Calls $admit $admitCore) -eq 1 -and (Calls $admitCore $policy) -eq 1 -and (Calls $admitCore $probe) -eq 1 -and $grantReaders.Count -ge 1) 'R3-F2 (PR #70 round 2): the IgnoreClick postfix decides through the controller admission (the policy and the probe) with the shared mount grant passed as its reader'
$canMoveHelper = @(Method $participant 'CanMovePairedMount')[0]
foreach ($term in @(@(Method $participant 'PairedMovementContext')[0], $movementState.GetMethod('HasGrantedMovement', $any), @(Method $participant 'CanAddressActor')[0], (GameType 'Kingmaker.EntitySystem.Entities.UnitEntityData').GetMethod('IsAbleToAct'))) {
    Check ((Calls $grantHelper $term) -ge 1 -and (Calls $canMoveHelper $term) -ge 1) ('R3-F2: the click grant and paired execution share ' + $term.Name)
}
Check ((Calls $grantHelper ($movementState.GetMethod('RemainingOrderMetres', $any))) -eq 1 -and (Calls ($movementState.GetMethod('RemainingOrderMetres', $any)) ((KmcType 'KingmakerMountedCombat.Domain.MountedMovementState').GetMethod('RemainingAcrossOrder', [Type[]]@([single], [single], [single], [single], [bool], [bool], [bool], [bool], [bool])))) -eq 1) 'R3-F2/R6-F3: the click grant is the mount allocation''s distance across the order (same state Advance spends)'
Check ((Calls $admit (@(Method $participant 'PairedMovementContext')[0])) -eq 0 -and (Calls $admit ((GameType 'TurnBased.Controllers.TurnController').GetProperty('EnabledFiveFootStep').GetGetMethod())) -eq 0) 'R3-F2: the click decision reads no rider-context step flags of its own'
$findPath = (GameType 'Kingmaker.View.UnitMovementAgent').GetMethod('FindPath')
$closeEnough = $command.GetMethod('IsUnitCloseEnough', [Reflection.BindingFlags]'Static,Public')
$single = KmcType 'KingmakerMountedCombat.Integration.MountedPairSingleAttack'
$evaluate = $single.GetMethod('EvaluateCurrentNativeAdmission', $any)
$evaluateAt = $single.GetMethod('EvaluateNativeAdmissionAtMountPosition', $any)
$walkerType = KmcType 'KingmakerMountedCombat.Domain.MountedRouteWalker'
# PR #70 round 2: the native query is Query (classified by the path's native CompleteState: R1-F5), called for the mount-centred route and
# for each bounded lateral query (R1-F4); the bounded walk over openings is TryApproach (R1-F3), the unbounded walk is the diagnostic only.
$probeType = $probe.DeclaringType
$query = @(Method $probeType 'Query')[0]
$tryApproach = @(Method $probeType 'TryApproach')[0]
$completeState = $findPath.ReturnType.GetProperty('CompleteState').GetGetMethod()
function WalkCalls([Reflection.MethodBase]$m) { @(Refs $m | Where-Object { $_.Member -and $_.Member.Name -ceq 'Walk' -and $_.Member.DeclaringType -eq $walkerType }).Count }
$walksRoute = (WalkCalls $tryApproach) -eq 1 -and (WalkCalls (@(Method $probeType 'DiagnosticReach')[0])) -eq 1 -and (Calls $probe $query) -eq 2 -and (Calls $probe $tryApproach) -eq 2 -and (Calls $query $completeState) -ge 2
$admitsAlongRoute = @($probe.DeclaringType.GetNestedTypes($any) | ForEach-Object { $_.GetMethods($any) } | Where-Object { (Calls $_ $evaluateAt) -ge 1 }).Count -ge 1
Check ((Calls $query $findPath) -eq 1 -and (Calls $probe $evaluate) -eq 1 -and $walksRoute -and $admitsAlongRoute) 'R4-F1/R5-F2: the probe walks the mount agent''s native route with the production walker, bounded by the mount grant (and once unbounded for the diagnostic), until the strike''s production admission passes'
$offset = (KmcType 'KingmakerMountedCombat.Domain.MountedCombatSpatialPolicy').GetMethod('TryCalculateNativeExecutorAdmissionRadius')
Check ((Calls $evaluateAt $offset) -eq 1 -and (Calls $evaluateAt $closeEnough) -eq 1) 'R4-F1: admission along the route keeps the supported rider offset and the native command line of sight'
$turnDegrees = (KmcType 'KingmakerMountedCombat.Domain.MountedHeading').GetMethod('TurnDegrees')
$orientation = (GameType 'Kingmaker.EntitySystem.Entities.UnitEntityData').GetProperty('Orientation').GetGetMethod()
Check ((Calls $evaluateAt $turnDegrees) -eq 1 -and (Calls $evaluateAt $orientation) -eq 1) 'R6-F2: admission along the route turns the rider and eye offsets from the mount''s current orientation to its heading on the segment'
$setForced = $command.GetProperty('ForcedPath').GetSetMethod()
$getForced = $command.GetProperty('ForcedPath').GetGetMethod()
Check ((Calls (@(Method $pair 'BeginDelegatedMove')[0]) $setForced) -eq 1) 'R4-F1: the click-admitted order''s carrier follows the validated route (ForcedPath)'
$nativeApproach = MemberAt 0x060027A6
$currentPathForUnit = MemberAt 0x0600700F
Check ((FirstOffset $nativeApproach $getForced) -ge 0 -and (FirstOffset $nativeApproach $getForced) -lt (FirstOffset $nativeApproach $currentPathForUnit)) 'R4-F1: native turn-based TickApproaching follows a ForcedPath before any visualizer path'
$attachProof = @(Method $controller 'AttachClickProof')[0]
Check ((Calls $admitCore (@(Method $controller 'RememberValidatedClickProof')[0])) -eq 1 -and (Calls $tryExecute $attachProof) -eq 1 -and (Calls $attachProof ($pair.GetField('ValidatedApproachRoute', $any))) -ge 1 -and (Calls $attachProof ($pair.GetField('RouteRevalidator', $any))) -ge 1 -and (Calls $attachProof ($pair.GetField('ClickPlan', $any))) -ge 1) 'R4-F1: an admitted click binds its order as route-bound with a revalidator'
$revalidate = @(Method $controller 'RevalidateAdmittedClickRoute')[0]
Check ((Calls $revalidate $probe) -eq 1 -and (Calls $revalidate $grantEntry) -eq 1 -and (Calls $revalidate ((KmcType 'KingmakerMountedCombat.Domain.PairedAttackClickAdmissionPolicy').GetMethod('RouteWithinGrant'))) -eq 1) 'R4-F1: every carrier proof is recomputed from the current origin and grant by the click''s own walk and rule'
$invoke = [Func`2].MakeGenericType((KmcType 'KingmakerMountedCombat.Integration.PairedAttackClickProbe+PlanIdentity'), (KmcType 'KingmakerMountedCombat.Integration.PairedAttackClickProbe+Approach')).GetMethod('Invoke')
$moveCtor = (GameType 'Kingmaker.UnitLogic.Commands.UnitMoveTo').GetConstructor([Type[]]@([UnityEngine.Vector3], [single]))
$moveCtorOffsets = @(Refs (@(Method $pair 'BeginDelegatedMove')[0]) | Where-Object { $_.Member -eq $moveCtor } | ForEach-Object { $_.Offset })
# PR-2 reconciliation: on PR-1's accepted source the ordinary carrier is built by NewApproachMove, so the route-bound point command is the
# last (and only direct) UnitMoveTo(Vector3, float) construction in BeginDelegatedMove.
Check ($moveCtorOffsets.Count -ge 1 -and $moveCtorOffsets[-1] -gt (FirstOffset (@(Method $pair 'BeginDelegatedMove')[0]) $invoke) -and $moveCtorOffsets[-1] -lt (FirstOffset (@(Method $pair 'BeginDelegatedMove')[0]) $setForced)) 'R6-F1: a route-bound carrier is a new point command to the revalidated endpoint, built after revalidation and before its route is set'
Check ((FirstOffset (@(Method $pair 'BeginDelegatedMove')[0]) $invoke) -ge 0 -and (FirstOffset (@(Method $pair 'BeginDelegatedMove')[0]) $invoke) -lt (FirstOffset (@(Method $pair 'BeginDelegatedMove')[0]) $setForced)) 'R4-F1: a route-bound carrier revalidates before its route is set; there is no unchecked fallback'
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
# ---- 6. Round-4 corrections (R4-F2, R4-F3, R4-F8, R1-F11) ---------------------------------------------------------------
$endUnperceived = @(Method $pair 'EndIfAutomaticOrderNotAdmitted')[0]
foreach ($name in @('OnStart', 'TickApproach', 'TickPreStartApproach', 'StartChildAttack')) { Check ((Calls (@(Method $pair $name)[0]) $endUnperceived) -ge 1) ('R4-F2/R10-F1: ' + $name + ' asks the automatic order''s admission decision before any destination or strike') }
Check ((Calls $tryExecute ($pair.GetField('AutomaticOrderAdmissionLost', $any))) -ge 1) 'R4-F2/R10-F1: every automatic order''s command carries the controller''s admission decision'
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
$planDispatch = @(Method $controller 'PlanStockDispatch')[0]
Check ((Calls $drive $planDispatch) -eq 1 -and (CallsWithin $controller 'PlanStockDispatch' (@(Method $controller 'FollowThroughStrikeAdmissibleNow')[0])) -eq 1 -and (Calls $planDispatch ($planner.GetMethod('Plan'))) -eq 1 -and (Calls ($planner.GetMethod('Plan')) ((KmcType 'KingmakerMountedCombat.Domain.UnifiedMountedStockAttackPolicy').GetMethod('MountJoinsIntent'))) -eq 1 -and (Calls (@(Method $controller 'FollowThroughStrikeAdmissibleNow')[0]) (@(Method $controller 'PlannedStrikeAdmissibleNow')[0])) -eq 1 -and @((KmcType 'KingmakerMountedCombat.Domain.UnifiedMountedStockAttackPolicy').GetMethod('MountJoinsIntent').GetParameters()).Count -eq 9) 'PR-1 R1-F1/R3-F4/R3-F5/R4-F5: the drive dispatches through PlanStockDispatch and the production MountedDispatchPlanner.Plan, whose MountJoinsIntent reads the planned horse strike''s own admission'
Check ((Calls (@(Method $controller 'SettleOrderTerminal')[0]) ($ledger.GetMethod('MarkRiderEnded'))) -ge 1 -and (Calls (@(Method $controller 'PlanStockDispatch')[0]) ($ledger.GetMethod('RiderEnded'))) -ge 1) 'PR-1 R1-F3: an actor-specific rider end keeps the order for the horse and is read at the next drive'
Check ((Calls (@(Method $controller 'SweepOrphanedAutomaticCommand')[0]) ($controller.GetProperty('ExplicitOrphanReleasedCount', $any).GetSetMethod($true))) -ge 1) 'PR-1 R1-F5: an explicit wrapper dropped from slot and queue is released by the sweep'
$retireHidden = @(Method $controller 'RetireAutomaticOrderIfNotAdmitted')[0]
Check ((Calls $drive $retireHidden) -ge 1 -and (FirstOffset $drive $retireHidden) -lt (FirstOffset $drive (@(Method $controller 'PlanStockDispatch')[0])) -and (Calls (@(Method $controller 'ObserveAutomaticProposal')[0]) $retireHidden) -ge 1) 'PR-1 R1-F9: a hidden automatic target is released before any readiness or geometry probe and before it can refuse a proposal'
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
    (Calls $terminalHandler ($ledger.GetMethod('MarkRiderEnded'))) -eq 1 -and (Calls (@(Method $controller 'PlanStockDispatch')[0]) ($ledger.GetMethod('RiderEnded'))) -ge 1) 'PR-1 R1-F3: the terminal disposition comes from DecideTerminal over the wrapper''s delivery facts; a rider end is read at the next drive'
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
    (Calls (@(Method $controller 'RetireAutomaticOrderIfNotAdmitted')[0]) $retireWrapper) -eq 2 -and (Calls (@(Method $controller 'RetireOrderForCancel')[0]) $retireWrapper) -eq 2 -and
    (Calls (@(Method $controller 'RetireOrderOnTargetInvalidation')[0]) $retireWrapper) -eq 1 -and $collections.Count -eq 0) ('PR-1 R2-F1/R3-F1/R3-F2/R4-F1/R11-F1: turn-based, real-time, perception, cancellation and invalidation retirement share RetireOrderWrapper (the automatic decision retires through it for its own boundaries and for perception or native ineligibility); turn-based, real-time and perception retirement share RetireOrderWrapper (released strike left to native, retirement held on the wrapper); no controller collection holds wrappers (' + (($collections | ForEach-Object { $_.Name }) -join ',') + ')')
Check ((Calls (@(Method $controller 'AdmitQueuedExplicitRun')[0]) $orderRetired) -eq 1 -and (Calls (@(Method $controller 'AdmitQueuedAutomaticRun')[0]) $orderRetired) -eq 1 -and
    (Calls (@(Method $patches 'InternalRunPrefix')[0]) (@(Method $patches 'AdmitExplicitPromotion')[0])) -eq 1 -and
    (FirstOffset (@(Method $patches 'AdmitExplicitPromotion')[0]) $orderRetired) -ge 0 -and (FirstOffset (@(Method $patches 'AdmitExplicitPromotion')[0]) $orderRetired) -lt (FirstOffset (@(Method $patches 'AdmitExplicitPromotion')[0]) (@(Method $controller 'AdmitQueuedExplicitRun')[0]))) 'PR-1 R3-F1: explicit and automatic queue promotion refuse a wrapper retired with its order, the explicit one before any owner lookup (also after detachment)'
$cancelCore = @(Method $controller 'CancelCore')[0]
$freshClick = @($controller.GetMethods($any) | Where-Object { @(Refs $_ | Where-Object { $_.Text -ceq 'new stock hostile attack' }).Count -gt 0 })
Check ((Calls (@(Method $controller 'Cancel')[0]) $cancelCore) -eq 1 -and (Calls $cancelCore (@(Method $controller 'RetireOrderForCancel')[0])) -eq 1 -and $freshClick.Count -eq 1 -and (Calls $freshClick[0] $cancelCore) -eq 1 -and
    (Calls $drive (@(Method $controller 'RetireOrderIfTargetInvalid')[0])) -eq 1 -and (Calls (@(Method $controller 'RetireOrderIfTargetInvalid')[0]) (@(Method $controller 'RetireOrderOnTargetInvalidation')[0])) -eq 1 -and
    (Calls (@(Method $controller 'RetireOrderIfTargetInvalid')[0]) ($controller.GetProperty('HasActiveCommand').GetGetMethod())) -ge 1) 'PR-1 R4-F1/R3-F1/R5-F1/R5-F2: a fresh click cancels through CancelCore; observed invalidation retires the generation''s exact wrapper, scoped to its generation while another generation''s command is active'
$mountLeft = @(Method $controller 'MountCommandLeftToNative')[0]
$cancelMove = @($cancelCore.GetMethodBody() | ForEach-Object { Refs $cancelCore } | Where-Object { $null -ne $_.Member -and $_.Member.Name -ceq 'CancelMountMovement' })
# PR-2 round 16 (R12-F4): CancelCore has two CancelMountMovement sites (the ordinary one and the mod-disable successor-yielding overload); both follow the guard.
Check ((Calls $cancelCore $mountLeft) -eq 1 -and $cancelMove.Count -eq 2 -and @($cancelMove | Where-Object { $_.Offset -lt (FirstOffset $cancelCore $mountLeft) }).Count -eq 0 -and (Calls $mountLeft $released) -eq 1) 'PR-1 R4-F1: on ordinary retargeting a delivered mount command is kept out of the mount-wide move interrupt (CancelMountMovement; both round-16 sites after the guard)'
Check ((Calls (@(Method $controller 'StockIntentStrikeAdmissibleNow')[0]) (@(Method $controller 'StationaryStrikeAdmissible')[0])) -eq 1 -and (Calls (@(Method $controller 'ObserveAutomaticProposal')[0]) (@(Method $controller 'StationaryStrikeAdmissible')[0])) -eq 1 -and
    (Calls (@(Method $controller 'StationaryStrikeAdmissible')[0]) (@(Method $controller 'PlannedStrikeAdmissibleNow')[0])) -eq 1) 'PR-1 R5-F3: restricted automatic proposals and no-wrapper retirement use the planned mounted melee strike''s stationary admission'
Check ((Calls (@(Method $controller 'DecideAutomaticOrderAdmission')[0]) (@(Method $controller 'StockIntentStrikeAdmissibleNow')[0])) -eq 1 -and (Calls (@(Method $controller 'StockIntentStrikeAdmissibleNow')[0]) (@(Method $pair 'StrikeAdmissibleNow')[0])) -eq 1) 'PR-1 R4-F3/R12-F4: restricted automatic movement retires only a strike that is not admissible from the current position (the automatic decision''s stationary strike check)'
$describe = @(Method $pair 'DescribeOutcome')[0]
# PR-1 review R4-F6 (round 7): the actor and both charge flags are assigned once, on the outcome actually delivered (detailed or fallback):
# ReportTerminalOnce calls AttachActorCharges once, which takes them from DescribeStandardCharge (charged only for a released strike);
# DescribeOutcome itself no longer assigns the charge flags.
$attach = @(Method $pair 'AttachActorCharges')[0]
Check ($null -ne $attach -and (Calls (@(Method $pair 'ReportTerminalOnce')[0]) $attach) -eq 1 -and (Calls $attach (@(Method $pair 'DescribeStandardCharge')[0])) -eq 1 -and
    (Calls $describe (@(Method $pair 'DescribeStandardCharge')[0])) -eq 0 -and (Calls (@(Method $pair 'DescribeStandardCharge')[0]) $released) -eq 2) 'PR-1 R4-F6: the delivered outcome (detailed or fallback) carries the actor and a Standard charge only for a delivered strike (a force-finish spends nothing), assigned once by AttachActorCharges'
Check (@(Refs (@(Method $patches 'ClickUnitPrefix')[0]) | Where-Object { $null -ne $_.Member -and ($_.Member.Name -ceq 'get_SelectedUnits' -or $_.Member.Name -ceq 'SelectedUnits') }).Count -eq 1 -and (Calls (@(Method $patches 'ClickUnitPrefix')[0]) (@(Method $controller 'BeginClickObservation')[0])) -eq 1 -and
    @(Refs (@(Method $patches 'ClickUnitPrefix')[0]) | Where-Object { $null -ne $_.Member -and ($_.Member.Name -ceq 'get_SimulatingClick' -or $_.Member.Name -ceq 'SimulatingClick') }).Count -eq 1 -and
    @((@(Method $patches 'ClickUnitPrefix')[0]).GetParameters() | Where-Object { $_.Name -ceq 'simulate' }).Count -eq 1 -and @((@(Method $patches 'ClickUnitPostfix')[0]).GetParameters() | Where-Object { $_.Name -ceq '__result' }).Count -eq 1) 'PR-1 R1-F11: the click bracket records the commanded actors and skips simulated (prediction) and unhandled clicks'
$ownership = KmcType 'KingmakerMountedCombat.Domain.MountedManualTargetOwnership'
$observeAccepted = @(Method $controller 'ObserveAcceptedWork')[0]
Check ((Calls (@(Method $controller 'ClearStockAttackIntent')[0]) ($ownership.GetMethod('ClearsOnRetirement'))) -eq 2 -and (Calls $observeAccepted ($ownership.GetMethod('ObserveForeignAssignment'))) -eq 1 -and
    (FirstOffset $observeAccepted ($ownership.GetMethod('ObserveForeignAssignment'))) -lt (FirstOffset $observeAccepted ($ledger.GetMethod('Classify'))) -and
    (Calls (@(Method $controller 'OwnManualTarget')[0]) ($ownership.GetMethod('RecordOwnWrite'))) -eq 1 -and
    (Calls (@(Method $patches 'InternalRunPostfix')[0]) $route) -eq 1 -and (Calls $route ((KmcType 'KingmakerMountedCombat.Domain.MountedAcceptedWorkRoutes').GetMethod('AfterRun'))) -eq 1) 'PR-1 R1-F11: ownership follows the actual assignment, observed before the competing classification; only an installed promotion is observed; retirement clears only a still-owned selection'
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
# PR-1 round 7 (R1-F11, R6-F1, R6-F2): compiled wiring of the source corrections.
# R1-F11 (round 8): the click's own ManualTarget assignment is observed at its installed call site.
function TranspilerNames([Reflection.MethodBase]$Method) { $il = $Method.GetMethodBody().GetILAsByteArray(); $names = @(); for ($i = 0; $i -le $il.Length - 5; $i++) { if ($il[$i] -eq 0x72) { try { $names += $Method.Module.ResolveString([BitConverter]::ToInt32($il, $i + 1)) } catch { } } }; , $names }
$clickHook = @(Method $patches 'ClickManualTargetAssignment')[0]
$clickTranspiler = @(Method $patches 'ClickAssignmentTranspiler')[0]
$observeAssignment = @(Method $controller 'ObserveClickTargetAssignment')[0]
$installedOnClick = $game.GetType('Kingmaker.Controllers.Clicks.Handlers.ClickUnitHandler', $true).GetMethod('OnClick', $any)
$manualSetter = $game.GetType('Kingmaker.Controllers.Combat.UnitCombatState', $true).GetProperty('ManualTarget').GetSetMethod()
$onClickIl = $installedOnClick.GetMethodBody().GetILAsByteArray(); $setterSites = 0
for ($i = 0; $i -le $onClickIl.Length - 5; $i++) { if ($onClickIl[$i] -eq 0x6F -or $onClickIl[$i] -eq 0x28) { try { if ($installedOnClick.Module.ResolveMethod([BitConverter]::ToInt32($onClickIl, $i + 1)) -eq $manualSetter) { $setterSites++ } } catch { } } }
$hookParameters = @($clickHook.GetParameters() | ForEach-Object { $_.ParameterType.FullName })
$runPrefixObserves = @(Refs (@(Method $patches 'UnitCommandRunPrefix')[0]) | Where-Object { $null -ne $_.Member -and $_.Member.Name -like 'ObserveClick*' }).Count
Check ($installedOnClick.MetadataToken -eq 0x060093ED -and $setterSites -eq 1 -and $null -ne $clickTranspiler -and $null -ne $observeAssignment -and
    ($hookParameters -join ',') -ceq 'Kingmaker.Controllers.Combat.UnitCombatState,Kingmaker.EntitySystem.Entities.UnitEntityData' -and $clickHook.ReturnType -eq [void] -and
    (Calls $clickHook $manualSetter) -eq 1 -and (Calls $clickHook $observeAssignment) -eq 1 -and $runPrefixObserves -eq 0 -and
    (TranspilerNames $clickTranspiler) -contains 'ClickManualTargetAssignment' -and (TranspilerNames $clickTranspiler) -contains 'ManualTarget') 'PR-1 R1-F11: installed ClickUnitHandler.OnClick has exactly one ManualTarget assignment site; the transpiler replaces it with ClickManualTargetAssignment (same stack: combat state, target), which performs the native assignment and reports it to ObserveClickTargetAssignment; no Run is taken as an assignment'
$unperceived = @(Method $controller 'RetireAutomaticOrderIfNotAdmitted')[0]
$decideAdmission = @(Method $controller 'DecideAutomaticOrderAdmission')[0]
$admissionInvalid = @(Method $controller 'AutomaticOrderTargetInvalid')[0]
Check ((Calls $unperceived $decideAdmission) -eq 1 -and (Calls $decideAdmission $admissionInvalid) -eq 1 -and (Calls $admissionInvalid (@(Method $pair 'TargetInvalidNow')[0])) -eq 1 -and
    (FirstOffset $decideAdmission $admissionInvalid) -lt (FirstOffset $decideAdmission (@(Method $controller 'AutomaticTargetOutsideNativeAdmission')[0])) -and
    (Calls $unperceived (@(Method $controller 'RetireOrderOnTargetInvalidation')[0])) -eq 1 -and
    (FirstOffset $unperceived (@(Method $controller 'RetireOrderOnTargetInvalidation')[0])) -lt (FirstOffset $unperceived (@(Method $controller 'ClearStockAttackIntent')[0]))) 'PR-1 R6-F1/R10-F2: the automatic order''s one admission decision tests target invalidation (the command''s own test) before perception and native eligibility, and retires it as target invalidation first'
$tie = @(Method $controller 'ResolveEqualRankTie')[0]
$aiForbidden = $game.GetType('Kingmaker.EntitySystem.Entities.UnitEntityData', $true).GetProperty('AiMovementForbidden', $any).GetGetMethod($true)
$rankingReader = KmcType 'KingmakerMountedCombat.Integration.NativeBrainRankingReader'
$readerCandidates = @(Method $rankingReader 'Candidates')[0]
$nativeInLos = $game.GetType('Kingmaker.Controllers.Brain.TargetInfo', $true).GetMethod('InLos', $any)
$nativeGetRange = $game.GetType('Kingmaker.Controllers.Brain.Blueprints.BlueprintAiAction', $true).GetMethod('GetRange', $any)
$nativeIsTargetValid = $game.GetType('Kingmaker.Controllers.Brain.Blueprints.BlueprintAiAction', $true).GetMethod('IsTargetValid', $any)
$hasLos = @($game.GetType('Kingmaker.EntitySystem.Entities.UnitEntityData', $true).GetMethods($any) | Where-Object { $_.Name -ceq 'HasLOS' -and $_.GetParameters().Count -eq 1 })[0]
Check ($nativeIsTargetValid.MetadataToken -eq 0x0600942A -and $nativeInLos.MetadataToken -eq 0x06009422 -and
    (Calls $readerCandidates $nativeInLos) -eq 1 -and (Calls $readerCandidates $nativeGetRange) -eq 1 -and (Calls $tie $readerCandidates) -eq 1 -and
    (Calls $tie $aiForbidden) -eq 1 -and (Calls $tie $hasLos) -eq 0 -and (Calls $tie (@(Method $controller 'StationaryStrikeAdmissible')[0])) -eq 1) 'PR-1 R7-F2/R8-F1: the tie-break applies native admission for the proposal''s action (the reader takes BlueprintAiAction.GetRange and TargetInfo.InLos, the facts IsTargetValid 0x0600942A reads) where the rider is held or the candidate unreachable, and the stationary mounted strike under restricted travel'
$tryRetire = @(Method $controller 'TryRetireStockIntent')[0]
Check ((Calls $tryRetire (@(Method $controller 'RetireAutomaticOrderIfNotAdmitted')[0])) -eq 1 -and
    (FirstOffset $tryRetire (@(Method $controller 'RetireAutomaticOrderIfNotAdmitted')[0])) -lt (FirstOffset $tryRetire (@(Method $controller 'ClearStockAttackIntent')[0])) -and
    (Calls (@(Method $controller 'AdmitQueuedAutomaticRun')[0]) $tryRetire) -eq 1) 'PR-1 R8-F2/R10-F2: every automatic retirement reason in the shared retirement (drive and queued promotion) follows the typed admission decision, so target invalidation takes precedence over geometry'
$retireInvalid = @(Method $controller 'RetireOrderOnTargetInvalidation')[0]
$recordInvalid = @(Method $pair 'RecordTargetInvalidation')[0]
$retireWrapper = @(Method $controller 'RetireOrderWrapper')[0]
Check ($null -ne $recordInvalid -and (Calls $retireInvalid $recordInvalid) -eq 1 -and (FirstOffset $retireInvalid $recordInvalid) -lt (FirstOffset $retireInvalid $retireWrapper) -and
    (Calls (@(Method $pair 'TryEndExpectedTargetInvalidation')[0]) (@(Method $pair 'TargetInvalidationReason')[0])) -eq 1 -and (Calls $recordInvalid (@(Method $pair 'TargetInvalidationReason')[0])) -eq 1) 'PR-1 R6-F1: controller-first loss records the wrapper''s own target-invalidation cause and reason (shared with its own observation) before retiring it'
Check ((Calls (@(Method $controller 'ResolveEqualRankTie')[0]) (@(Method $controller 'StationaryStrikeAdmissible')[0])) -eq 1) 'PR-1 R6-F2: under restricted travel the tie-break admits a melee candidate only through the stationary mounted-strike admission'
$admissionLost = @(Method $controller 'AutomaticTargetOutsideNativeAdmission')[0]
$currentRange = @(Method $rankingReader 'CurrentActionRange')[0]
$nativeLos = @(Method $rankingReader 'NativeLos')[0]
$checkLos = $game.GetType('Kingmaker.Designers.GameHelper', $true).GetMethod('CheckLOS', [Type[]]@($game.GetType('Kingmaker.EntitySystem.Entities.UnitEntityData', $true), $game.GetType('Kingmaker.EntitySystem.Entities.UnitEntityData', $true)))
$decisionContextCtor = $game.GetType('Kingmaker.Controllers.Brain.DecisionContext', $true).GetConstructor([Type[]]@())
$retireReason = (KmcType 'KingmakerMountedCombat.Domain.MountedAutomaticAttackPolicy').GetMethod('RetireReason')
Check ((Calls $decideAdmission $retireReason) -eq 1 -and (Calls $tryRetire $unperceived) -eq 1 -and
    (FirstOffset $decideAdmission $admissionLost) -lt (FirstOffset $decideAdmission $retireReason)) 'PR-1 R11-F1/R11-F2: the automatic order''s own boundaries (AI off, Stop, newer player work, the stationary strike check whenever pair movement is forbidden) are part of its one admission decision, after invalidation, perception and native eligibility'
Check ((Calls (@(Method $pair 'TryEndExpectedTargetInvalidation')[0]) $endUnperceived) -eq 1) 'PR-1 R11-F4: an unreleased automatic command''s own target-loss observation is answered by its order''s decision'
Check ($checkLos.MetadataToken -eq 0x06008555 -and (Calls $nativeLos $checkLos) -eq 1 -and (Calls $admissionLost $nativeLos) -eq 1 -and (Calls $admissionLost $aiForbidden) -eq 1 -and
    (Calls $admissionLost $currentRange) -eq 1 -and (Calls $admissionLost $readerCandidates) -eq 0 -and (Calls $currentRange $nativeGetRange) -eq 1 -and
    $decisionContextCtor.MetadataToken -eq 0x0600941F -and (Calls $currentRange $decisionContextCtor) -eq 1 -and
    (Calls $decideAdmission $admissionLost) -eq 1) 'PR-1 R9-F1/R10-F3: the automatic order''s decision rechecks native IsTargetValid''s restricted rule with the proposing action''s CURRENT range (BlueprintAiAction.GetRange on a fresh DecisionContext 0x0600941F for the rider), both bodies and GameHelper.CheckLOS 0x06008555, independent of the optional ranking reader'
# ---- PR-2 R12-F4 (round 13): outstanding owned cleanup takes part in registry retention and teardown readiness before anything is unpatched ----
# Round 15: the teardown ORDER is the production MountedServiceTeardownSequence (CompositionRoot.SetEnabled/UpdateCore/Dispose and
# MountedLifecycleSubscriber.HandleModDisable delegate to it; the attack-path harness executes it). These checks bind each entry point to the
# sequence, the sequence's order, and each step adapter to the exact service it stood for before the extraction.
$lifecycleSubscriber = KmcType 'KingmakerMountedCombat.Integration.MountedLifecycleSubscriber'
$worldService = KmcType 'KingmakerMountedCombat.Integration.MountedWorldService'
$handleDisable = @(Method $lifecycleSubscriber 'HandleModDisable')[0]
$settlePairs = @(Method $worldService 'SettleOutstandingPairCleanup')[0]
$trySettle = @(Method $controller 'TrySettleOutstandingCleanup')[0]
$outstandingGetter = $controller.GetProperty('HasOutstandingCleanup', $any).GetGetMethod($true)
$retainedGetter = $controller.GetProperty('HasRetainedPairCommands', $any).GetGetMethod($true)
$teardownReadyGetter = (KmcType 'KingmakerMountedCombat.Integration.GameMountedRelationshipService').GetProperty('AllTeardownOwnersReady', $any).GetGetMethod($true)
$cleanupMethod = @(Method $lifecycleSubscriber 'Cleanup')[0]
$teardownClosures = @($worldService.GetNestedTypes($any) | ForEach-Object { $_.GetMethods($any) } | Where-Object { (Calls $_ $outstandingGetter) -ge 1 -and (Calls $_ ($controller.GetProperty('ChargeAdmissionFenced', $any).GetGetMethod($true))) -ge 1 })
$sequence = KmcType 'KingmakerMountedCombat.Integration.MountedServiceTeardownSequence'
$stepsType = $sequence.GetNestedType('ISteps', $any); $modStepsType = $sequence.GetNestedType('IModDisableSteps', $any)
function StepMethod([Type]$Type, [string]$Name) { $m = $Type.GetMethod($Name, $any); if ($null -eq $m) { $m = $Type.GetProperty($Name, $any).GetGetMethod($true) }; $m }
$seqModDisable = @(Method $sequence 'ModDisable')[0]; $seqBeginDisable = @(Method $sequence 'BeginDisable')[0]; $seqRetry = @(Method $sequence 'RetryWhileDisabled')[0]; $seqUnload = @(Method $sequence 'Unload')[0]
$mDrain = StepMethod $modStepsType 'DrainOutstandingPairCleanup'; $mReady = StepMethod $modStepsType 'TeardownOwnersReady'; $mCleanup = StepMethod $modStepsType 'CleanupPairs'
$modStepsImpl = $lifecycleSubscriber.GetNestedType('ModDisableSteps', $any)
$modIl = $seqModDisable.GetMethodBody().GetILAsByteArray()
$modDrainSites = @(Refs $seqModDisable | Where-Object { $_.Member -eq $mDrain })
Check ((Calls $retainedGetter $outstandingGetter) -eq 1 -and (Calls $settlePairs $trySettle) -eq 1 -and $teardownClosures.Count -ge 1 -and
    (Calls $handleDisable $seqModDisable) -eq 1 -and $modDrainSites.Count -eq 2 -and (Calls $seqModDisable $mReady) -ge 2 -and
    (FirstOffset $seqModDisable $mDrain) -lt (FirstOffset $seqModDisable $mReady) -and
    @(Refs $seqModDisable | Where-Object { $_.Member -eq $mReady } | Select-Object -Last 1)[0].Offset -gt (FirstOffset $seqModDisable $mCleanup) -and
    $null -ne $modStepsImpl -and (Calls (@(Method $modStepsImpl 'DrainOutstandingPairCleanup')[0]) $settlePairs) -eq 1 -and
    (Calls (@(Method $modStepsImpl 'get_TeardownOwnersReady')[0]) $teardownReadyGetter) -eq 1 -and (Calls (@(Method $modStepsImpl 'CleanupPairs')[0]) $cleanupMethod) -eq 1) 'PR-2 R12-F4 (round 13; sequence extracted in round 15): outstanding acceptance effects, retained carrier cleanup and an unfinished refused-request cleanup keep the pair owner registered (HasRetainedPairCommands) and the pair teardown not ready; HandleModDisable (run by disable and by unload before patches.Dispose) runs MountedServiceTeardownSequence.ModDisable: one drain (MountedWorldService.SettleOutstandingPairCleanup), refusal while work remains (AllTeardownOwnersReady), the lifecycle cleanup, a second drain for work the cancellation created, ready only when none remains'
# ---- PR-2 R12-F4 (round 14): drain before readiness, a lifecycle-owned retry after ordinary updates stop, the drain's verdict consumed ----
$root = KmcType 'KingmakerMountedCombat.CompositionRoot'
$setEnabled = @(Method $root 'SetEnabled')[0]
$updateCore = @(Method $root 'UpdateCore')[0]
$rootDispose = @($root.GetMethods($any) | Where-Object { $_.Name -ceq 'Dispose' -and $_.GetParameters().Count -eq 0 })[0]
$updatePairs = @(Method $worldService 'UpdatePairs')[0]
$enabledGetter = $root.GetProperty('IsEnabled', $any).GetGetMethod($true)
$persistenceType = KmcType 'KingmakerMountedCombat.Integration.MountedPersistenceService'
$saveSuspended = $persistenceType.GetProperty('SaveSuspended', $any).GetGetMethod($true)
$loadInFlight = $persistenceType.GetProperty('LoadInFlight', $any).GetGetMethod($true)
$drainForTeardown = @(Method $persistenceType 'DrainForTeardown')[0]
$patchDispose = @((KmcType 'KingmakerMountedCombat.Integration.MountedPatchController').GetMethods($any) | Where-Object { $_.Name -ceq 'Dispose' -and $_.GetParameters().Count -eq 0 })[0]
$sDrain = StepMethod $stepsType 'DrainOutstandingPairCleanup'; $sReady = StepMethod $stepsType 'TeardownOwnersReady'; $sSave = StepMethod $stepsType 'SaveSuspended'
$sLoad = StepMethod $stepsType 'LoadInFlight'; $sWorker = StepMethod $stepsType 'ActiveSaveWorkerRunning'; $sArea = StepMethod $stepsType 'AreaTransitionPending'
$sRestore = StepMethod $stepsType 'CombatRestorationPending'; $sEnabled = StepMethod $stepsType 'IsEnabled'; $sHandle = StepMethod $stepsType 'HandleModDisable'
$sOwnedWorker = StepMethod $stepsType 'OwnedWorkerCanStillCommit'; $sRelease = StepMethod $stepsType 'ReleaseForUnload'; $sRemove = StepMethod $stepsType 'RemoveHooksAndServices'
$rootSteps = $root.GetNestedType('TeardownSteps', $any)
Check ($null -ne $rootSteps -and (Calls (@(Method $rootSteps 'DrainOutstandingPairCleanup')[0]) $settlePairs) -eq 1 -and (Calls (@(Method $rootSteps 'get_TeardownOwnersReady')[0]) $teardownReadyGetter) -eq 1 -and
    (Calls (@(Method $rootSteps 'get_LoadInFlight')[0]) $loadInFlight) -eq 1 -and (Calls (@(Method $rootSteps 'get_SaveSuspended')[0]) $saveSuspended) -eq 1 -and
    (Calls (@(Method $rootSteps 'get_IsEnabled')[0]) $enabledGetter) -eq 1 -and (Calls (@(Method $rootSteps 'OwnedWorkerCanStillCommit')[0]) $drainForTeardown) -eq 1 -and
    (Calls (@(Method $rootSteps 'HandleModDisable')[0]) $handleDisable) -eq 1 -and (Calls (@(Method $rootSteps 'RemoveHooksAndServices')[0]) $patchDispose) -eq 1) 'PR-2 R12-F4/R14-F5 (round 15): CompositionRoot.TeardownSteps binds the sequence''s steps to the services the root called before the extraction (MountedWorldService.SettleOutstandingPairCleanup, AllTeardownOwnersReady, the persistence fences, IsEnabled, DrainForTeardown, MountedLifecycleSubscriber.HandleModDisable, patches.Dispose)'
Check ((Calls $setEnabled $seqBeginDisable) -eq 1 -and (Calls $setEnabled $settlePairs) -eq 0 -and (Calls $setEnabled $handleDisable) -eq 0 -and
    (FirstOffset $seqBeginDisable $sDrain) -lt (FirstOffset $seqBeginDisable $sReady) -and
    (FirstOffset $seqBeginDisable $sSave) -lt (FirstOffset $seqBeginDisable $sDrain) -and (FirstOffset $seqBeginDisable $sLoad) -lt (FirstOffset $seqBeginDisable $sDrain) -and
    (FirstOffset $seqBeginDisable $sWorker) -lt (FirstOffset $seqBeginDisable $sDrain) -and
    @(Refs $seqBeginDisable | Where-Object { $_.Member -eq $sLoad } | Select-Object -Last 1)[0].Offset -lt (FirstOffset $seqBeginDisable $sHandle)) 'PR-2 R12-F4 (round 14; round 15 sequence): CompositionRoot.SetEnabled(false) runs MountedServiceTeardownSequence.BeginDisable: outstanding owned pair cleanup is drained once, outside the save/load fences, BEFORE the AllTeardownOwnersReady check, and HandleModDisable runs only after the owned-load refusal (a refusal leaves everything patched and retryable)'
Check ((Calls $updateCore $seqRetry) -eq 1 -and (Calls $updateCore $settlePairs) -eq 0 -and (FirstOffset $updateCore $seqRetry) -lt (FirstOffset $updateCore $updatePairs) -and
    (FirstOffset $seqRetry $sEnabled) -lt (FirstOffset $seqRetry $sDrain) -and (FirstOffset $seqRetry $sLoad) -lt (FirstOffset $seqRetry $sDrain) -and (FirstOffset $seqRetry $sSave) -lt (FirstOffset $seqRetry $sDrain) -and
    (FirstOffset $seqRetry $sWorker) -lt (FirstOffset $seqRetry $sDrain) -and (FirstOffset $seqRetry $sArea) -lt (FirstOffset $seqRetry $sDrain) -and
    (FirstOffset $seqRetry $sRestore) -lt (FirstOffset $seqRetry $sDrain) -and (Calls $seqRetry $sDrain) -eq 1) 'PR-2 R12-F4 (round 14; round 15 sequence): UpdateCore retries outstanding owned pair cleanup through MountedServiceTeardownSequence.RetryWhileDisabled, which drains only while IsEnabled is false (after PerformFailureCleanup or a refused disable stopped the ordinary pair update), ahead of the ordinary gates and outside the save/load/area/restoration fences'
Check ($modDrainSites.Count -eq 2 -and @($modDrainSites | Where-Object { $modIl[$_.Offset + 5] -eq 0x26 }).Count -eq 0) 'PR-2 R12-F4 (round 14; round 15 sequence): HandleModDisable''s sequence (MountedServiceTeardownSequence.ModDisable) consumes both drain verdicts (before the readiness check and after the cancellation its own cleanup performs); neither result is discarded'
# ---- PR-2 R14-F5 (round 15): the owned-load fence precedes every drain, cancellation and unpatch on the unload path ----
Check ((Calls $rootDispose $seqUnload) -eq 1 -and (Calls $rootDispose $handleDisable) -eq 0 -and (Calls $rootDispose $drainForTeardown) -eq 0 -and (Calls $rootDispose $patchDispose) -eq 0 -and
    (Calls $rootDispose $settlePairs) -eq 0 -and (Calls $seqUnload $sLoad) -eq 1 -and @(Refs $seqUnload | Where-Object { $_.Member -and $_.Member.DeclaringType -eq $stepsType } | Select-Object -First 1)[0].Member -eq $sLoad -and
    (FirstOffset $seqUnload $sLoad) -lt (FirstOffset $seqUnload $sOwnedWorker) -and (FirstOffset $seqUnload $sOwnedWorker) -lt (FirstOffset $seqUnload $sHandle) -and
    (FirstOffset $seqUnload $sHandle) -lt (FirstOffset $seqUnload $sRelease) -and (FirstOffset $seqUnload $sRelease) -lt (FirstOffset $seqUnload $sRemove) -and (Calls $seqUnload $sDrain) -eq 0) 'PR-2 R14-F5 (round 15): CompositionRoot.Dispose (UMM unload) runs MountedServiceTeardownSequence.Unload, whose FIRST step is the owned-load fence (LoadInFlight) before the bounded owned-worker wait, HandleModDisable (the only drain/cancellation), the Mounted Shield release and patches.Dispose; Dispose itself calls none of them directly'
# ---- PR-2 R12-F4 (round 16): the production disable/unload cleanup reaches the relationship Dismount, the coordinator and the runtime's
# movement-authority restoration, which consults the pair's exact mount-command ownership before it stops the mount (the attack-path harness
# executes this chain with the spliced members; these checks bind the compiled wiring the harness stands in for: the constructor binding).
$relationshipType = KmcType 'KingmakerMountedCombat.Integration.GameMountedRelationshipService'
$runtimeType = KmcType 'KingmakerMountedCombat.Integration.KingmakerMountedPairRuntime'
$coordinatorType = KmcType 'KingmakerMountedCombat.Domain.MountedRelationshipCoordinator'
$runtimeInterface = KmcType 'KingmakerMountedCombat.Domain.IMountedPairRuntime'
$commandOwnedSetter = $runtimeType.GetProperty('MountCommandOwned', $any).GetSetMethod($true)
$commandOwnedByPair = @(Method $relationshipType 'MountCommandOwnedByPair')[0]
# The delegate is created with ldftn (FE 06 <token>), which the one-byte call-site scan above does not decode.
function LoadsFunction([Reflection.MethodBase]$Method, [Reflection.MethodBase]$Target) { $il = $Method.GetMethodBody().GetILAsByteArray(); for ($i = 0; $i -le $il.Length - 6; $i++) { if ($il[$i] -eq 0xFE -and $il[$i + 1] -eq 0x06 -and [BitConverter]::ToInt32($il, $i + 2) -eq $Target.MetadataToken) { return $true } }; $false }
$relationshipCtors = @($relationshipType.GetConstructors($any) | Where-Object { (Calls $_ $commandOwnedSetter) -eq 1 -and (LoadsFunction $_ $commandOwnedByPair) })
$ownsMotion = @(Method $controller 'OwnsMountMotionCommand')[0]
$driveOwnsCommand = (KmcType 'KingmakerMountedCombat.Domain.MountedContinuousDriveOwner').GetMethod('OwnsCommand', $any)
$restoreMovement = @(Method $runtimeType 'RestoreMovementAuthority')[0]
$preserves = @(Method $runtimeType 'PreservesMountSuccessor')[0]
# Round 17 (R16-F1): CancelMountMovement(trigger) and PreservesMountSuccessor both read the steering successor (MountSuccessor).
$mountSuccessor = @(Method $runtimeType 'MountSuccessor')[0]
$commandOwnedGetter = $runtimeType.GetProperty('MountCommandOwned', $any).GetGetMethod($true)
$interruptMove = @(Method (GameType 'Kingmaker.UnitLogic.Commands.UnitCommands') 'InterruptMove')[0]
$relationshipDismount = @(Method $relationshipType 'Dismount')[0]
$cleanupOwner = @(Method $lifecycleSubscriber 'CleanupOwner')[0]
$coordinatorCleanup = @(Method $coordinatorType 'Cleanup')[0]
Check ($relationshipCtors.Count -ge 1 -and (Calls $commandOwnedByPair $ownsMotion) -eq 1 -and (Calls $commandOwnedByPair $driveOwnsCommand) -eq 1 -and
    (Calls $ownsMotion (@(Method $controller 'OwnsExactPairedNativeCommand')[0])) -eq 1 -and (Calls $ownsMotion (@(Method $controller 'IsOwnDelegatedMove')[0])) -eq 1 -and
    (Calls $restoreMovement $preserves) -eq 1 -and (FirstOffset $restoreMovement $preserves) -lt (FirstOffset $restoreMovement $interruptMove) -and (Calls $restoreMovement $interruptMove) -eq 2 -and
    (Calls $cleanupMethod $cleanupOwner) -ge 1 -and (Calls $cleanupOwner $relationshipDismount) -eq 1 -and
    (Calls $relationshipDismount ($coordinatorType.GetMethod('Dismount', $any))) -eq 1 -and (Calls $coordinatorCleanup ($runtimeInterface.GetMethod('RestoreMovementAuthority'))) -eq 1) 'PR-2 R12-F4 (round 16): the ModDisable cleanup reaches CleanupOwner -> relationship Dismount -> coordinator Cleanup -> RestoreMovementAuthority, which consults PreservesMountSuccessor before either InterruptMove; the relationship constructor binds MountCommandOwned to MountCommandOwnedByPair (combat OwnsMountMotionCommand: exact paired native commands and own carriers; stick-movement OwnsCommand)'
$handleDismounting = @(Method $controller 'HandleDismounting')[0]; $cancelCore = @(Method $controller 'CancelCore')[0]
$cancelMountTrigger = @($runtimeType.GetMethods($any) | Where-Object { $_.Name -ceq 'CancelMountMovement' -and $_.GetParameters().Count -eq 1 })[0]
$cancelMountPlain = @($runtimeType.GetMethods($any) | Where-Object { $_.Name -ceq 'CancelMountMovement' -and $_.GetParameters().Count -eq 0 })[0]
Check ((Calls $handleDismounting $cancelCore) -eq 1 -and (Calls $cancelCore (@(Method $controller 'ReleaseRiderTurnGroundMovementForModDisable')[0])) -eq 1 -and
    (Calls $cancelCore (@(Method $controller 'CancelRiderTurnGroundMovement')[0])) -eq 1 -and (Calls $cancelCore $cancelMountTrigger) -eq 1 -and (Calls $cancelCore $cancelMountPlain) -eq 1 -and
    (Calls $cancelMountTrigger $mountSuccessor) -eq 1 -and (Calls $cancelMountTrigger $interruptMove) -eq 0) 'PR-2 R12-F4 (round 16): the combat owner''s Dismounting handler reaches CancelCore, which at mod disable releases the admitted ground order''s accounting (no interrupt) and cancels mount movement through the successor-yielding CancelMountMovement(trigger)'
# ---- PR-2 round 17: R16-F1 the mod-disable successor includes an unowned approaching command outside the Move slot; R13-F3 the exact carrier's
# movement fact is recorded at the delegated movement debit and read by the approach termination ----
$approachSteers = @(Method (KmcType 'KingmakerMountedCombat.Integration.MountedCarrierCleanup') 'ApproachSteers')[0]
$ledgerType = KmcType 'KingmakerMountedCombat.Domain.MountedCarrierApproachLedger'
$tickDelegatedMethod = @(Method (KmcType 'KingmakerMountedCombat.Integration.MountedMovementStateAdapter') 'TickDelegated')[0]
$acquiredGetter = (KmcType 'KingmakerMountedCombat.Integration.MountedPairAttackCommand').GetProperty('CarrierAcquiredPath', $any).GetGetMethod($true)
Check ((Calls $preserves $mountSuccessor) -eq 1 -and (Calls $mountSuccessor $approachSteers) -eq 1 -and (Calls $mountSuccessor $commandOwnedGetter) -ge 2 -and
    (Calls (@(Method (KmcType 'KingmakerMountedCombat.Integration.MountedCarrierCleanup') 'OtherCommandSteers')[0]) $approachSteers) -eq 1) 'PR-2 round 17: MountSuccessor (read by PreservesMountSuccessor and CancelMountMovement(trigger)) takes an unowned Move-slot command or an unowned approaching command in another slot by the exact-carrier cleanup''s ApproachSteers (R16-F1); the round-17 Move-slot positive-tick marker (R13-F3) is replaced in round 19 (below)'
# ---- PR-2 round 18: R17-F1 the delivery fact is recorded on entry to the delegated movement hook; R16-F1 with an unowned live command on the
# mount the mod-disable paths interrupt only the pair's own commands (never native InterruptMove) and the cleanup keeps an unstarted IsMoveUnit command's motion ----
$holdsUnowned = @(Method $runtimeType 'MountHoldsUnownedCommand')[0]; $interruptOwned = @(Method $runtimeType 'InterruptOwnedMountCommands')[0]
$isMoveUnitGetter = (GameType 'Kingmaker.UnitLogic.Commands.Base.UnitCommand').GetProperty('IsMoveUnit', $any).GetGetMethod($true)
Check ((Calls $restoreMovement $holdsUnowned) -eq 1 -and (Calls $restoreMovement $interruptOwned) -eq 1 -and (FirstOffset $restoreMovement $holdsUnowned) -lt (FirstOffset $restoreMovement $interruptMove) -and
    (Calls $cancelMountTrigger $holdsUnowned) -eq 1 -and (Calls $cancelMountTrigger $interruptOwned) -eq 1 -and (Calls $mountSuccessor $isMoveUnitGetter) -ge 1 -and
    (Calls (@(Method (KmcType 'KingmakerMountedCombat.Integration.MountedCarrierCleanup') 'OtherCommandSteers')[0]) $isMoveUnitGetter) -ge 1) 'PR-2 round 18: (the round-18 delivered-Path marker on entry to TickDelegated (R17-F1) is replaced in round 19, below) RestoreMovementAuthority and CancelMountMovement(trigger) decide MountHoldsUnownedCommand before any interruption and then interrupt only the pair''s own commands, MountSuccessor and the exact-carrier cleanup read IsMoveUnit (R16-F1)'
# ---- PR-2 round 19 (R17-F1, R18-F2): one issuance/delivery record per exact carrier, written at the native boundaries; no Move-slot marker ----
$commandType19 = KmcType 'KingmakerMountedCombat.Integration.MountedPairAttackCommand'
$observerType19 = KmcType 'KingmakerMountedCombat.Integration.MountedCarrierPathObserver'
$ledgerWriters = @('Admit','RecordRequest','RecordCompletion') | ForEach-Object { $ledgerType.GetMethod($_) }
Check ($null -eq $kmc.GetType('KingmakerMountedCombat.Domain.MountedCarrierMovementLedger', $false) -and
    (Calls $tickDelegatedMethod ($ledgerType.GetMethod('RecordMovement'))) -eq 1 -and @($ledgerWriters | Where-Object { (Calls $tickDelegatedMethod $_) -ne 0 }).Count -eq 0 -and
    (Calls $acquiredGetter ($ledgerType.GetMethod('HasDelivery'))) -eq 1 -and (Calls $acquiredGetter ($ledgerType.GetMethod('MovedAlongDelivery'))) -eq 0 -and (Calls $acquiredGetter ($ledgerType.GetMethod('PhaseOf'))) -eq 0 -and
    (Calls (@(Method $commandType19 'NoteCarrierIssued')[0]) ($ledgerType.GetMethod('Admit'))) -eq 1 -and
    (Calls (@(Method $observerType19 'AfterRequest')[0]) ($ledgerType.GetMethod('RecordRequest'))) -eq 1 -and (Calls (@(Method $observerType19 'AfterCompletion')[0]) ($ledgerType.GetMethod('RecordCompletion'))) -eq 1 -and
    $null -eq $commandType19.GetField('pathAtCarrierIssue', $any) -and $null -eq $commandType19.GetField('pathRequestPendingCarrier', $any)) 'PR-2 round 19 (R17-F1, R18-F2): the exact carrier''s issuance and delivery are written only by the PathTo/OnPathComplete observer (MountedCarrierApproachLedger.RecordRequest/RecordCompletion) for a carrier the command admitted at issue; TickDelegated writes no issuance or delivery and credits its positive tick only to the carrier whose own delivered path moved (RecordMovement); CarrierAcquiredPath reads the delivery (not the movement fact); the round-17/18 Move-slot markers and the rider-sampled baseline heuristics are gone'
# ---- PR-2 round 20 (R18-F2, R19-F2, R19-F5): the exact carrier's delivery at both decision points; forced-path scope release ----
$controller20 = KmcType 'KingmakerMountedCombat.Integration.MountedCombatController'
$evaluate20 = @(Method $controller20 'EvaluateFirstTickDecision')[0]; $decide20 = @(Method $controller20 'DecideFirstTickProposal')[0]
$phaseOf20 = $observerType19.GetMethod('PhaseOf', $any); $tryRead20 = (KmcType 'KingmakerMountedCombat.Integration.MountedAgentSteering').GetMethod('TryRead', $any)
$inPlace20 = (KmcType 'KingmakerMountedCombat.Integration.PairedAttackClickProbe').GetMethod('EvaluateFirstTick', $any)
$endIfNative20 = @(Method $commandType19 'EndIfNativeTurnBasedApproachEnded')[0]; $endIfProvisional20 = @(Method $commandType19 'EndIfProvisionalApproachEnded')[0]
$appliesGetter20 = $commandType19.GetProperty('NativeTurnBasedDispositionApplies', $any).GetGetMethod($true)
$distance20 = @((GameType 'Kingmaker.Utility.GeometryUtils').GetMethods($any) | Where-Object { $_.Name -ceq 'MechanicsDistance' })
$teardownModDisable20 = (KmcType 'KingmakerMountedCombat.Integration.MountedServiceTeardownSequence').GetMethod('ModDisable', $any)
$scopeField20 = $observerType19.GetField('approaching', $any)
Check ((Calls $evaluate20 $phaseOf20) -eq 1 -and (FirstOffset $evaluate20 $inPlace20) -lt (FirstOffset $evaluate20 $phaseOf20) -and (FirstOffset $evaluate20 $phaseOf20) -lt (FirstOffset $evaluate20 $tryRead20) -and
    [Enum]::GetNames((KmcType 'KingmakerMountedCombat.Domain.MountedFirstTickVerdict')) -contains 'Defer' -and
    @($distance20 | Where-Object { (Calls $acquiredGetter $_) -ne 0 }).Count -eq 0 -and (Calls $acquiredGetter ($ledgerType.GetMethod('HasDelivery'))) -eq 1 -and
    (Calls $endIfNative20 $endIfProvisional20) -eq 1 -and (FirstOffset $endIfNative20 $endIfProvisional20) -lt (FirstOffset $endIfNative20 $appliesGetter20) -and
    (Calls $endIfProvisional20 ($commandType19.GetProperty('CarrierApproachPhase', $any).GetGetMethod($true))) -eq 1 -and
    (Calls $teardownModDisable20 ($observerType19.GetMethod('ReleaseScopes', $any))) -eq 1 -and $null -ne $scopeField20 -and
    $scopeField20.FieldType.GetGenericArguments()[0].GetField('Command', $any).FieldType -eq [WeakReference] -and
    @($observerType19.GetFields($any) | Where-Object { $_.FieldType.IsGenericType -and $_.FieldType.GetGenericArguments()[0].FullName -ceq 'Kingmaker.UnitLogic.Commands.Base.UnitCommand' }).Count -eq 0) 'PR-2 round 20 (R18-F2, R19-F2, R19-F5): the first-tick decision reads the exact carrier''s phase after the independently legal in-place check and before reading the agent''s path (anything but its own delivery defers: MountedFirstTickVerdict.Defer); CarrierAcquiredPath is the carrier''s own delivery only (no displacement criterion); a provisional order''s ended own delivery is settled (EndIfProvisionalApproachEnded) before the accepted-order disposition and the drift re-path; the forced-path scope holds commands weakly and the teardown ModDisable releases it'
Write-Host ('Mounted alpha 2 lifecycle contract PASS=' + $script:passed + ' FAIL=0 nativeProof=false')
