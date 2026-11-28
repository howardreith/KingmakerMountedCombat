[CmdletBinding()]
param([string]$RepositoryRoot,[string]$TemplatePath,[string]$OutputDirectory)
# Production-path check of the mounted pair attack's approach and start (reviews R3-F1, R4-F2, R4-F1). Splices the ACTUAL source
# of: the whole MountedPairSingleAttack class (strike admission, position admission, start admission) and its admission enum;
# the MountedPairAttackCommand members on the approach/start paths (own-move registry, pre-start approach for the rider's
# carrier and the horse's native approach, automatic perception guard, OnStart, OnTick, target invalidation, OnEnded,
# TickApproach, Repath, BeginDelegatedMove, StopDelegatedMove, StartChildAttack, end-cause recording, fault ending);
# MountedPatchController.MountedAttackApproachPrefix; PairedAttackClickProbe; MountedCombatController.RevalidateAdmittedClickRoute;
# PR #70 round 2 (R1-F10): MountedCombatController's click-proof members (AdmitTurnBasedAttackClick, the proof's take/bind/attach),
# RiderMeleeClickAdmitted and the fresh target-life rule; PairedActivationControls.AdmitPairedAttackClick;
# PairedActivationLifecycle.PairedMountGrantedMetres; MountedMovementStateAdapter's allocation, Get, TickDelegated, RemainingOrderMetres
# and HasGrantedMovement
# PR #70 round 7 (R1-F6): MountedCombatController.OwnsExactPairedNativeCommand/OwnsExactUnifiedMountCommand, TryOverrideMountTurnMovement,
# NativeRunKeepsHold and CommitAcceptedProvisional; PairedActivationLifecycle.NativeActorEligibleForCommand
# into tests/Chunk7Diagnostics/AttackPathHarnessTemplate.cs, compiles them with every real Domain source and runs the
# reviewers' counterexamples with controls on a facade of the native command surface. Offline evidence only (nativeProof=false).
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))}
if(-not$TemplatePath){$TemplatePath=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/AttackPathHarnessTemplate.cs'}
if(-not$OutputDirectory){$lab=[IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot));$OutputDirectory=Join-Path $lab ('analysis-cache/mounted-alpha2-attack-path/run-'+[Guid]::NewGuid().ToString('N'))}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
function Extract([string]$Text,[string]$Signature){$start=$Text.IndexOf($Signature,[StringComparison]::Ordinal);if($start-lt0){throw ('Actual source member absent: '+$Signature)};if($Text.IndexOf($Signature,$start+1,[StringComparison]::Ordinal)-ge0){throw ('Actual source member is ambiguous: '+$Signature)};$brace=$Text.IndexOf('{',$start);$depth=0;for($i=$brace;$i-lt$Text.Length;$i++){if($Text[$i]-eq'{'){$depth++}elseif($Text[$i]-eq'}'){$depth--;if($depth-eq0){return $Text.Substring($start,$i-$start+1)}}};throw 'Unbalanced actual source member.'}
function ExtractExpression([string]$Text,[string]$Signature){$start=$Text.IndexOf($Signature,[StringComparison]::Ordinal);if($start-lt0){throw ('Actual source member absent: '+$Signature)};if($Text.IndexOf($Signature,$start+1,[StringComparison]::Ordinal)-ge0){throw ('Actual source member is ambiguous: '+$Signature)};$end=$Text.IndexOf(';',$start);if($end-lt0){throw 'Unterminated actual expression member.'};return $Text.Substring($start,$end-$start+1)}
function ExtractRegion([string]$Text,[string]$First,[string]$Next){$start=$Text.IndexOf($First,[StringComparison]::Ordinal);if($start-lt0){throw ('Actual source region absent: '+$First)};if($Text.IndexOf($First,$start+1,[StringComparison]::Ordinal)-ge0){throw ('Actual source region is ambiguous: '+$First)};$end=$Text.IndexOf($Next,$start,[StringComparison]::Ordinal);if($end-lt0){throw ('Actual source region end absent: '+$Next)};return $Text.Substring($start,$end-$start).TrimEnd()}
$integration=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Integration'
$commandPath=Join-Path $integration 'MountedPairAttackCommand.cs';$command=[IO.File]::ReadAllText($commandPath)
$childPath=Join-Path $integration 'MountedPairSingleAttack.cs';$child=[IO.File]::ReadAllText($childPath)
$patchPath=Join-Path $integration 'MountedPatchController.cs';$patch=[IO.File]::ReadAllText($patchPath)
$probePath=Join-Path $integration 'PairedAttackClickProbe.cs';$probe=[IO.File]::ReadAllText($probePath)
$controlsPath=Join-Path $integration 'PairedActivationControls.cs';$controls=[IO.File]::ReadAllText($controlsPath)
$lifecyclePath=Join-Path $integration 'PairedActivationLifecycle.cs';$lifecycle=[IO.File]::ReadAllText($lifecyclePath)
$adapterPath=Join-Path $integration 'MountedMovementStateAdapter.cs';$adapter=[IO.File]::ReadAllText($adapterPath)
$controllerPath=Join-Path $integration 'MountedCombatController.cs';$controller=[IO.File]::ReadAllText($controllerPath)
$terminalBranchesPath=Join-Path $integration 'NativeCommandTerminalBranches.cs';$terminalBranches=[IO.File]::ReadAllText($terminalBranchesPath)
$directInputPath=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics/NativeOrdinaryAttackInput.cs';$directInput=[IO.File]::ReadAllText($directInputPath)
$domain=@(Get-ChildItem -LiteralPath (Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Domain') -Filter '*.cs' | Sort-Object Name | ForEach-Object { $_.FullName })
$members=[ordered]@{
 'ADMISSION_ENUM'=(Extract $child 'internal enum MountedPairNativeAdmissionState')
 'CHILD_ATTACK_CLASS'=(Extract $child 'internal class MountedPairSingleAttack : UnitAttack')
 'APPROACH_PREFIX'=(Extract $patch 'internal static bool MountedAttackApproachPrefix(UnitCommand __instance)')
 'PROBE_CLASS'=(Extract $probe 'internal static class PairedAttackClickProbe')
 'REVALIDATE'=(Extract $controller 'private PairedAttackClickProbe.Approach RevalidateAdmittedClickRoute(UnitEntityData target, Vector3? established,')
 'CONTROLLER_REVALIDATE'=(Extract $controller 'private PairedAttackClickProbe.Approach RevalidateAdmittedClickRoute(UnitEntityData target, Vector3? established,')
 'CONTROLLER_CLICK_PROOF'=(ExtractRegion $controller '        // MMR-02 (review R4-F1; PR #70 R1-F1, R1-F7, R1-F9): the proof of an overridden turn-based click' '        // Review R4-F1: the proof for a carrier of a click-admitted turn-based order')
 'CONTROLLER_FRESH_LIFE'=(Extract $controller 'private static bool FreshExplicitClickTargetLifeAdmits(UnitEntityData target)')
 'CONTROLLER_RIDER_CLICK_ADMITTED'=(ExtractExpression $controller 'internal bool RiderMeleeClickAdmitted(UnitEntityData target) =>')
 'PAIR_ADMIT_CLICK'=(Extract $controls 'internal void AdmitPairedAttackClick(TurnController context, ref bool result)')
 'PAIR_GRANT'=(Extract $lifecycle 'internal float PairedMountGrantedMetres(TurnController turn, out bool step, out bool singleMove, out string reason, TurnController movementInput = null)')
 'PAIR_CAN_ADDRESS'=(Extract $lifecycle 'internal bool CanAddressActor(UnitEntityData actor, TurnController turn)')
 'PAIR_IS_PARTNER'=(ExtractExpression $lifecycle 'internal bool IsPartnerContext(TurnController turn) =>')
 'PAIR_PRESERVE_SELECTION'=(ExtractExpression $controls 'internal bool PreservePartnerSelection(TurnController turn) =>')
 'PAIR_SELECTED_INPUT'=(Extract $controls 'internal TurnController SelectedNativeInputContext(TurnController native)')
 'PAIR_MOVEMENT_CONTEXT'=(Extract $controls 'internal TurnController PairedMovementContext(TurnController native)')
 'PAIR_BEGIN_PREPARATION'=(Extract $lifecycle 'private bool BeginPairedPreparation(TurnController turn)')
 'PAIR_BIND_FIRST_TICK_GATE'=(Extract $lifecycle 'internal void BindMountFirstTickGate(IMountedFirstTickGate gate, bool bind)')
 'PAIR_SUPPRESS'=(Extract $lifecycle 'internal bool SuppressPairedCandidate(CombatController.TBUnitInfo candidate)')
 'ADAPTER_BEGIN_GRANTED'=(Extract $adapter 'internal void BeginGrantedPreparation(TurnController turn, string identity)')
 'ADAPTER_REMAINING_GRANTED'=(Extract $adapter 'internal float RemainingGrantedMetres(UnitEntityData actor, bool step, bool singleMove)')
 'ADAPTER_ALLOCATION'=(ExtractRegion $adapter '        private sealed class Allocation' '        private readonly ActorAllocationLifetime')
 'ADAPTER_GET'=(Extract $adapter 'private Allocation Get(UnitEntityData mount)')
 'ADAPTER_TICK'=(Extract $adapter 'internal string TickDelegated(TurnController inputTurn, UnitEntityData mount, ref float deltaTime)')
 'ADAPTER_ORDER'=(Extract $adapter 'internal float RemainingOrderMetres(UnitEntityData actor, bool step, bool singleMove)')
 'ADAPTER_HAS'=(Extract $adapter 'internal bool HasGrantedMovement(UnitEntityData actor, bool step, bool singleMove)')
 'ADAPTER_FIRST_TICK'=(Extract $adapter 'internal bool FirstTickOnly(UnitEntityData actor, bool step, bool singleMove)')
 'CONTROLLER_STOCK_ROUTE'=(Extract $controller 'public bool TryRouteMountedStockAttack(UnitCommands commands, UnitCommand command)')
 'CONTROLLER_ACCEPT_REQUEST'=(Extract $controller 'private bool AcceptStockRequest(')
 'CONTROLLER_STOCK_CLICK'=(Extract $controller 'private MountedStockClick ObserveStockClick(')
 'CONTROLLER_STOCK_LIVE'=(ExtractExpression $controller 'private bool StockIntentCommandLive =>')
 'CONTROLLER_STOCK_REQUESTED'=(Extract $controller 'internal void ObserveStockAttackRequested(UnitEntityData unit, UnitEntityData target)')
 'CONTROLLER_BIND_REQUEST'=(Extract $controller 'internal void BindObservedStockRequest(UnitCommands commands, UnitCommand command)')
 'APPROACH_FAILURE'=(Extract $command 'private sealed class ApproachFailure : InvalidOperationException')
 'RECORD_END_CAUSE'=(Extract $command 'internal void RecordEndCause(MountedAttackEndCause cause, string detail,')
 'END_AFTER_FAULT'=(Extract $command 'private void EndAfterFault(Exception exception, string phase)')
 'TARGET_LIFE'=(Extract $command 'private bool TargetLifeAdmits(UnitEntityData unit)')
 'OWN_MOVES_AND_PRE_START'=(ExtractRegion $command 'internal UnitMoveTo DelegatedMove => delegatedMove;' '        // A Hold that arrived while this command waited in the native queue')
 'CAN_MOVE_AFTER_START'=(ExtractExpression $command 'public override bool CanMoveAfterStart =>')
 'ON_START'=(Extract $command 'protected override void OnStart()')
 'ON_TICK'=(Extract $command 'protected override void OnTick()')
 'TARGET_INVALIDATION'=(Extract $command 'private bool TryEndExpectedTargetInvalidation()')
 'ON_ENDED'=(Extract $command 'protected override void OnEnded(bool raiseEvent = true)')
 'TICK_APPROACH'=(Extract $command 'private void TickApproach()')
 'REPATH'=(Extract $command 'private bool Repath(string cause, bool targetDrift = false)')
 'BEGIN_DELEGATED'=(Extract $command 'private bool BeginDelegatedMove()')
 'STOP_DELEGATED'=(Extract $command 'private void StopDelegatedMove(bool requireSuccess)')
 'FORBID_APPROACH'=(Extract $command 'internal void ForbidApproach()')
 'SUCCESSOR_STEERS'=(Extract $command 'private bool SuccessorSteersMount(UnitCommands commands, UnitMoveTo exactMove)')
 'DEFERRED_CARRIER'=(ExtractRegion $command '        // PR-1 review R1-F4: a carrier waiting behind older work.' '        private void TickApproach()')
 'APPROACHING_BEFORE_ATTACK'=(ExtractRegion $command '        // PR-1 review R1-F8: also before native start' '        internal bool ValidateNativeSequenceTarget()')
 'START_CHILD'=(Extract $command 'private void StartChildAttack()')
 'OBSERVE_ADMISSION'=(Extract $command 'private void ObserveNativeAdmission(MountedPairNativeAdmissionState state)')
 'ON_ACTION'=(Extract $command 'protected override ResultType OnAction()')
 'CONTROLLER_RETIREMENT'=(ExtractRegion $controller '        private bool RetireSupersededPlayerOrder()' '        // PR-1 review R1-F2/R3-F1: native promotion')
 'CONTROLLER_RETAINED_SUPERSEDED'=(ExtractExpression $controller 'private bool RetainedOrderSuperseded() =>')
 'CONTROLLER_ADMIT_EXPLICIT'=(Extract $controller 'internal bool AdmitQueuedExplicitRun(MountedPairSingleAttack command)')
 'CONTROLLER_OBSERVE_ACCEPTED'=(Extract $controller 'internal void ObserveAcceptedWork(UnitCommands container, UnitCommand command, MountedAcceptedWorkRoute route)')
 'CONTROLLER_COMMAND_NEEDS_APPROACH'=(Extract $controller 'private static bool CommandNeedsApproach(UnitCommand command)')
 'CONTROLLER_OWN_DELEGATED'=(ExtractExpression $controller 'internal bool IsOwnDelegatedMove(UnitCommand command) =>')
 'CONTROLLER_CLICK'=(ExtractRegion $controller '        internal long ForeignSelectionWriteCount' '        internal MountedRetainedOrderLedger RetainedOrder => retainedOrder;')
 'CONTROLLER_OWNERSHIP'=(ExtractRegion $controller '        // PR-1 review R1-F11 (round 4): ownership of each actor' '        private void ClearStockAttackIntent(string reason, bool countCancellation)')
 'CONTROLLER_CLEAR_INTENT'=(Extract $controller 'private void ClearStockAttackIntent(string reason, bool countCancellation)')
 'CONTROLLER_INTERRUPT_OWNED'=(Extract $controller 'private void InterruptOwnedCommand(MountedPairAttackCommand command, MountedAttackEndCause cause, string reason)')
 'CONTROLLER_SWEEP'=(Extract $controller 'private void SweepFinishedCommand()')
 'CONTROLLER_PROBE'=(Extract $controller 'private bool PlannedStrikeAdmissibleNow(MountedCombatActionKind action, UnitEntityData target)')
 'CONTROLLER_FOLLOW_THROUGH'=(ExtractExpression $controller 'private bool FollowThroughStrikeAdmissibleNow(UnitEntityData target) =>')
 'CONTROLLER_SETTLE_TERMINAL'=(Extract $controller 'private void SettleOrderTerminal(MountedPairAttackCommand command, bool stockIntentOwned, MountedAttackTerminalKind terminal,')
 'CONTROLLER_FINISH_TB'=(Extract $controller 'private void FinishTurnBasedIntent(string feedback, string trace)')
 'CONTROLLER_CANCEL_RETIRE'=(Extract $controller 'private void RetireOrderForCancel(string reason, bool ordinaryRetarget)')
 'CONTROLLER_INVALIDATION'=(Extract $controller 'private void RetireOrderOnTargetInvalidation()')
 'CONTROLLER_TRY_RETIRE'=(Extract $controller 'private bool TryRetireStockIntent(UnitEntityData rider, UnitEntityData mount, bool turnBased)')
 'CONTROLLER_STRIKE_ADMISSIBLE'=(ExtractExpression $controller 'private bool StockIntentStrikeAdmissibleNow() =>')
 'ADMIT_EXPLICIT_PROMOTION'=(Extract $patch 'internal static bool AdmitExplicitPromotion(MountedPairSingleAttack command, MountedCombatController owner)')
 'CONTROLLER_CANCEL_CORE'=(Extract $controller 'private void CancelCore(string reason, bool ordinaryRetarget)')
 'CONTROLLER_MOUNT_LEFT'=(ExtractExpression $controller 'private bool MountCommandLeftToNative() =>')
 'CONTROLLER_IF_INVALID'=(Extract $controller 'private bool RetireOrderIfTargetInvalid(bool targetValid)')
 'CONTROLLER_PLAN_DISPATCH'=(Extract $controller 'private MountedDispatchPlan PlanStockDispatch(UnitEntityData rider, UnitEntityData mount, UnitEntityData target, bool turnBased, bool targetValid,')
 'CONTROLLER_STATIONARY'=(ExtractExpression $controller 'private bool StationaryStrikeAdmissible(UnitEntityData target) =>')
 'OUTCOME_CLASS'=(Extract $command 'internal sealed class MountedPairAttackOutcome')
 'CLICK_RESULT_ENUM'=(Extract $controller 'internal enum MountedCombatClickResult')
 'COMMAND_DESCRIBE_OUTCOME'=(Extract $command 'private MountedPairAttackOutcome DescribeOutcome(MountedActorCapabilityBlock capabilityBlock, string actorName)')
 'COMMAND_REPORT_TERMINAL'=(Extract $command 'private void ReportTerminalOnce()')
 'COMMAND_TARGET_REASON'=(Extract $command 'private string TargetInvalidationReason()')
 'COMMAND_RECORD_TARGET_INVALIDATION'=(Extract $command 'internal void RecordTargetInvalidation()')
 'CONTROLLER_HANDLE_TERMINAL'=(Extract $controller 'private void HandleCommandTerminal(')
 'CONTROLLER_DESCRIBE_FAILURE'=(Extract $controller 'private string DescribeTerminalFailure(MountedPairAttackOutcome outcome, MountedAttackEndCause cause, MountedActorCapabilityBlock causalBlock)')
 'CONTROLLER_DESCRIBE_UNEXECUTED'=(Extract $controller 'private string DescribeUnexecutedTerminal(MountedPairAttackOutcome outcome, MountedAttackEndCause cause, MountedActorCapabilityBlock causalBlock)')
 'CONTROLLER_DRIVE'=(Extract $controller 'private void TryDriveStockAttackIntent()')
 'CONTROLLER_EXECUTE'=(Extract $controller 'private MountedCombatClickResult TryExecuteAction(')
 'CONTROLLER_PROPOSAL'=(Extract $controller 'private void ObserveAutomaticProposal(UnitAttack proposal, bool ownerIsRider, bool turnBased)')
 'CONTROLLER_TIE'=(Extract $controller 'private UnitEntityData ResolveEqualRankTie(UnitEntityData rider, UnitEntityData proposed, UnitAttack proposal)')
 'CONTROLLER_ADMIT_AUTOMATIC'=(Extract $controller 'internal bool AdmitQueuedAutomaticRun(MountedPairSingleAttack command)')
 'CONTROLLER_ADMISSION_DECISION'=(ExtractRegion $controller '        // ---- PR-1 round 11 (R10-F1..F4): one current admission decision' '        // ---- end of the automatic-order admission decision ----')
 'CONTROLLER_MOVEMENT_FORBIDDEN'=(Extract $controller 'private bool AutomaticMovementForbidden(UnitEntityData rider)')
 'CONTROLLER_END_QUEUED'=(Extract $controller 'private void EndQueuedCommand(MountedPairSingleAttack command, string reason)')
 'CONTROLLER_CAPTURE_CONTEXT'=(Extract $controller 'private MountedCombatActionContext CaptureContext(')
 'CONTROLLER_IN_COMBAT'=(Extract $controller 'private bool IsPairInCombat()')
 'CONTROLLER_TRY_CONTAIN'=(Extract $controller 'private void TryContain(string phase, Action step)')
 'CONTROLLER_CANCEL'=(ExtractExpression $controller 'public void Cancel(string reason) =>')
 'COMMAND_OPTIONAL_RANGED'=(Extract $command 'private static string DescribeOptionalRangedState(object weapon, params string[] terms)')
 'COMMAND_ADMIT_UNCONSCIOUS'=(Extract $command 'internal void AdmitTargetUnconsciousWhenOrdered()')
 'COMMAND_RECORD_NATIVE_TERMINAL'=(Extract $command 'internal void RecordNativeTerminal(MountedNativeTerminalBranch branch, MountedActorCapabilityBlock block)')
 'COMMAND_TARGET_INVALID_NOW'=(Extract $command 'internal bool TargetInvalidNow()')
 'COMMAND_CREATE_VALIDATE_CHILD'=(Extract $command 'private void CreateAndValidateChildAttack()')
 'CONTROLLER_EVALUATE_ACTOR'=(Extract $controller 'private MountedCombatActionAvailability EvaluateStockActor(MountedCombatActionKind action, UnitEntityData target,')
 'CLICK_ASSIGNMENT_HOOK'=(Extract $patch 'internal static void ClickManualTargetAssignment(UnitCombatState combatState, UnitEntityData target)')
 'TERMINAL_CAPTURE_CAN_ACT'=(ExtractExpression $terminalBranches 'internal static int CaptureCanAct(bool canAct) =>')
 'TERMINAL_TB_FORCE_FINISH'=(Extract $terminalBranches 'internal static void TurnBasedForceFinish(UnitCommand command, UnitCommand.ResultType result, int canActObservation)')
 'TERMINAL_RUNNING_SITE'=(Extract $terminalBranches 'internal static void InterruptAtRunningSite(UnitCommand command, bool raiseEvent, int canActObservation)')
 'TERMINAL_RECORD'=(Extract $terminalBranches 'private static void Record(UnitCommand command, MountedNativeTerminalBranch branch)')
 'POINTER_TICK_PREFIX'=(Extract $patch 'internal static void PointerTickPrefix()')
 'POINTER_TICK_POSTFIX'=(Extract $patch 'internal static void PointerTickPostfix()')
 'BIND_SCOPED_TARGET'=(Extract $patch 'internal static void BindScopedDispatchTarget(UnitEntityData target, object turn)')
 'BEGIN_SCOPED_DISPATCH'=(Extract $patch 'internal static void BeginScopedInputDispatch(UnitEntityData target, object turn)')
 'END_SCOPED_DISPATCH'=(ExtractExpression $patch 'internal static void EndScopedInputDispatch() =>')
 'NATIVE_INPUT_CLICK'=(Extract $directInput 'internal bool Click(bool respectNativeAdmission = true, int button = 0)')
 'CONTROLLER_OWNS_UNIFIED'=(Extract $controller 'internal bool OwnsExactUnifiedMountCommand(UnitCommand command)')
 'CONTROLLER_OWNS_PAIRED'=(Extract $controller 'internal bool OwnsExactPairedNativeCommand(UnitCommand command)')
 'CONTROLLER_OVERRIDE_MOUNT'=(Extract $controller 'public bool TryOverrideMountTurnMovement(')
 'CONTROLLER_RUN_KEEPS_HOLD'=(Extract $controller 'internal bool NativeRunKeepsHold(UnitEntityData owner, UnitCommand command, bool fromQueue, bool doNotClearQueue)')
 'CONTROLLER_COMMIT_PROVISIONAL'=(Extract $controller 'private bool CommitAcceptedProvisional(')
 'PAIR_ELIGIBLE'=(Extract $lifecycle 'internal bool NativeActorEligibleForCommand(UnitEntityData actor, UnitCommand command)')
 'ADAPTER_PHYSICAL_SPEED'=(Extract $adapter 'internal static float PhysicalSpeedOf(UnitEntityData actor)')
 'CONTROLLER_APPLY_EFFECTS'=(Extract $controller 'private void ApplyAcceptanceEffects(FirstTickProposal p)')
 'PAIR_REMEMBER_INPUT'=(Extract $controls 'internal void RememberPairedMovementInput()')
 'PAIR_MOVEMENT_INPUT'=(ExtractExpression $controls 'private TurnController MovementInputContext(TurnController turn) =>')
}
$text=[IO.File]::ReadAllText($TemplatePath);foreach($entry in $members.GetEnumerator()){$marker='/* '+$entry.Key+' */';if(-not$text.Contains($marker)){throw ('Template lacks '+$marker)};$text=$text.Replace($marker,$entry.Value)}
$actual=Join-Path $OutputDirectory 'actual.cs';[IO.File]::WriteAllText($actual,$text)
$exe=Join-Path $OutputDirectory 'actual.exe'
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
if(-not(Test-Path -LiteralPath $compiler)){$vswhere=Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe';if(Test-Path -LiteralPath $vswhere){$compiler=[string](@(& $vswhere -latest -products * -find 'MSBuild\**\Bin\Roslyn\csc.exe')|Select-Object -First 1)}}
if(-not$compiler -or -not(Test-Path -LiteralPath $compiler)){throw 'csc.exe not found.'}
$compile=@('/nologo','/langversion:7.3','/target:exe','/warn:0',('/out:'+$exe),$actual)+$domain
& $compiler @compile; if($LASTEXITCODE-ne0){throw 'Actual attack path members did not compile.'}
$timer=[Diagnostics.Stopwatch]::StartNew();$lines=@(& $exe);$code=$LASTEXITCODE;$timer.Stop();$lines|Write-Host
$pass=@($lines|Where-Object{$_-cmatch'^PASS '}).Count;$fail=@($lines|Where-Object{$_-cmatch'^FAIL '}).Count
$inputs=@($commandPath,$childPath,$patchPath,$probePath,$controlsPath,$lifecyclePath,$adapterPath,$controllerPath,$terminalBranchesPath,$directInputPath,$TemplatePath)+$domain|ForEach-Object{[pscustomobject]@{path=$_;sha256=(Get-FileHash -LiteralPath $_).Hash.ToLowerInvariant()}}
@{passes=$pass;failures=$fail;seconds=$timer.Elapsed.TotalSeconds;nativeProof=$false;exitCode=$code;inputs=$inputs;actualSha256=(Get-FileHash -LiteralPath $actual).Hash.ToLowerInvariant()}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json')
if($code-ne0 -or $pass-eq0 -or $fail-ne0){throw 'Actual attack path harness regressed.'}
Write-Host ('MOUNTED ATTACK PATH HARNESS PASS '+$pass+'/0 (offline, not native evidence)')
