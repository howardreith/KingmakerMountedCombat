using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Kingmaker;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.GameModes;
using Kingmaker.UI.Selection;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using KingmakerMountedCombat.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    /// <summary>
    /// Exercises the four remaining core combat isolation controls without changing
    /// the already-qualified attack evidence schemas. Every row uses only the exact
    /// disposable Working fixture, the runtime-only diagnostic target, and production
    /// controller/relationship seams.
    /// </summary>
    internal sealed class RuntimeCombatControlScenarioEngine : IDisposable
    {
        internal const string Scenario = "combat-core-control-suite";
        internal const string EvidenceFileName = RuntimeCombatScenarioEngine.EvidenceFileName;

        private const string InvalidTargetRow = "mounted-rider-melee-invalid-target";
        private const string TargetDeathRow = "mounted-rider-melee-target-death";
        private const string CleanupRow = "mounted-rider-melee-cleanup";
        private const string NonMountedRow = "non-mounted-melee-control";
        private const double RowTimeoutSeconds = 30.0d;
        private const double CleanupTimeoutSeconds = 10.0d;
        private const float SpawnDistance = 6.0f;

        private static readonly string[] Rows =
        {
            InvalidTargetRow,
            TargetDeathRow,
            CleanupRow,
            NonMountedRow
        };

        private static readonly JsonSerializerSettings EvidenceJsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Formatting = Formatting.None,
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            PreserveReferencesHandling = PreserveReferencesHandling.None,
            ReferenceLoopHandling = ReferenceLoopHandling.Error,
            TypeNameHandling = TypeNameHandling.None
        };

        private readonly RuntimeRequest request;
        private readonly GameMountedRelationshipService relationship;
        private readonly MountedCombatController combat;
        private readonly DiagnosticSettings settings;
        private readonly IModLogger logger;
        private readonly List<RuntimeSubscenarioResult> results = new List<RuntimeSubscenarioResult>();
        private readonly List<string> errors = new List<string>();
        private readonly Stopwatch rowClock = new Stopwatch();
        private readonly string evidencePath;
        private readonly string dllSha256;
        private readonly string dllMvid;

        private int rowIndex;
        private int frameNumber;
        private int cleanupFrame;
        private double cleanupStartedAtSeconds;
        private string currentRow;
        private ControlStep step;
        private AssertionRecorder assertions;
        private DiagnosticCombatTargetService targetService;
        private MountedCombatRuleProbe ruleProbe;
        private UnitEntityData rider;
        private UnitEntityData mount;
        private UnitEntityData target;
        private string targetId;
        private bool started;
        private bool completed;
        private bool disposed;
        private bool originalUnsafeExperimentSetting;
        private bool settingLeaseOwned;
        private bool originalPause;
        private bool pauseLeaseOwned;
        private bool pauseRestored = true;
        private bool riderAgentInitiallyEnabled;
        private bool mountAgentInitiallyEnabled;
        private bool riderAvoidanceInitiallyDisabled;
        private bool mountAvoidanceInitiallyDisabled;
        private float riderStandardBefore;
        private float riderStandardAfter;
        private float mountStandardBefore;
        private float mountStandardAfter;
        private float riderMoveBefore;
        private float riderMoveAfter;
        private float mountMoveBefore;
        private float mountMoveAfter;
        private bool mountedAtExercise;
        private bool targetRemoved;
        private bool relationshipClean;
        private bool combatCleared;
        private bool agentsRestored;
        private ControlObservations observations;

        public RuntimeCombatControlScenarioEngine(
            RuntimeRequest request,
            GameMountedRelationshipService relationship,
            MountedCombatController combat,
            DiagnosticSettings settings,
            IModLogger logger)
        {
            this.request = request ?? throw new ArgumentNullException(nameof(request));
            this.relationship = relationship ?? throw new ArgumentNullException(nameof(relationship));
            this.combat = combat ?? throw new ArgumentNullException(nameof(combat));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            evidencePath = Path.Combine(request.EvidenceRoot, EvidenceFileName);
            var assembly = typeof(Main).Assembly;
            dllSha256 = ComputeSha256(assembly.Location);
            dllMvid = assembly.ManifestModule.ModuleVersionId.ToString();
        }

        public bool IsCompleted => completed;

        public IReadOnlyList<RuntimeSubscenarioResult> Results => results;

        public IReadOnlyList<string> Errors => errors;

        internal static bool SupportsScenario(string scenario)
        {
            return string.Equals(scenario, Scenario, StringComparison.Ordinal);
        }

        public void Start()
        {
            ThrowIfDisposed();
            if (started)
            {
                throw new InvalidOperationException("Combat-control suite has already started.");
            }
            if (!SupportsScenario(request.Scenario))
            {
                throw new InvalidOperationException("Scenario is outside the exact combat-control suite allowlist.");
            }

            started = true;
            originalUnsafeExperimentSetting = settings.EnableUnsafeMovementExperiment;
            settings.EnableUnsafeMovementExperiment = true;
            settingLeaseOwned = true;
            BeginNextRow();
            logger.Info("Combat-control runtime suite started.");
        }

        public void Update()
        {
            ThrowIfDisposed();
            if (!started || completed)
            {
                return;
            }

            frameNumber++;
            try
            {
                targetService?.ObserveTargetLifeState();
                if (rowClock.Elapsed.TotalSeconds > RowTimeoutSeconds && step != ControlStep.AwaitCleanup)
                {
                    assertions.Fail("Combat-control row exceeded its exact monotonic deadline at " + step + ".");
                    BeginCleanup();
                    return;
                }

                switch (step)
                {
                    case ControlStep.Begin:
                        PrepareRow();
                        break;
                    case ControlStep.AwaitCombat:
                        AwaitCombatAndExercise();
                        break;
                    case ControlStep.AwaitOutcome:
                        AwaitControlledOutcome();
                        break;
                    case ControlStep.AwaitCleanup:
                        VerifyCleanupAndAdvance();
                        break;
                    default:
                        throw new InvalidOperationException("Unexpected combat-control step: " + step + ".");
                }
            }
            catch (Exception exception)
            {
                logger.Exception("Combat-control runtime row threw", exception);
                assertions.Fail(exception.GetType().Name + ": " + exception.Message);
                BeginCleanup();
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            try
            {
                BestEffortCleanup();
            }
            finally
            {
                DisposeRowServices();
                RestorePause();
                RestoreSettings();
                rowClock.Stop();
                disposed = true;
            }
        }

        private void BeginNextRow()
        {
            if (rowIndex >= Rows.Length)
            {
                RestoreSettings();
                completed = true;
                rowClock.Stop();
                logger.Info("Combat-control runtime suite completed.");
                return;
            }

            currentRow = Rows[rowIndex];
            assertions = new AssertionRecorder();
            observations = new ControlObservations
            {
                ControlKind = currentRow,
                CleanupTrigger = "none"
            };
            targetRemoved = false;
            relationshipClean = false;
            combatCleared = false;
            agentsRestored = false;
            mountedAtExercise = false;
            pauseRestored = true;
            rowClock.Restart();
            step = ControlStep.Begin;
        }

        private void PrepareRow()
        {
            assertions.Check(relationship.State == RelationshipState.Unmounted,
                "Control row began Unmounted.");
            assertions.Check(!TurnBased.Controllers.CombatController.IsInTurnBasedCombat() &&
                    Game.Instance != null && Game.Instance.CurrentlyLoadedArea != null &&
                    Game.Instance.CurrentMode == GameModeType.Default,
                "Control row began in the exact real-time Default gameplay baseline.");

            string resolutionError;
            assertions.Check(relationship.TryResolveAutomationPair(out rider, out mount, out resolutionError),
                "Exact Medium-humanoid/Mammoth pair resolved: " + (resolutionError ?? "unknown error") + ".");
            if (rider?.View?.AgentASP == null || mount?.View?.AgentASP == null)
            {
                assertions.Fail("Resolved control pair lacks exact views and stock agents.");
                BeginCleanup();
                return;
            }

            riderAgentInitiallyEnabled = rider.View.AgentASP.enabled;
            mountAgentInitiallyEnabled = mount.View.AgentASP.enabled;
            riderAvoidanceInitiallyDisabled = rider.View.AgentASP.AvoidanceDisabled;
            mountAvoidanceInitiallyDisabled = mount.View.AgentASP.AvoidanceDisabled;
            assertions.Check(riderAgentInitiallyEnabled && mountAgentInitiallyEnabled &&
                    !riderAvoidanceInitiallyDisabled && !mountAvoidanceInitiallyDisabled,
                "Control pair began with exact stock agent and avoidance state.");
            if (assertions.FailureCount != 0)
            {
                BeginCleanup();
                return;
            }

            originalPause = Game.Instance.IsPaused;
            pauseLeaseOwned = true;
            pauseRestored = false;
            SelectionManager.Instance.SelectUnit(rider.View, true, false, false);

            if (!IsNonMountedRow)
            {
                var mounted = relationship.MountAutomationPair();
                assertions.Check(mounted.Succeeded && relationship.State == RelationshipState.Mounted,
                    "Exact pair mounted for the control row: " + FormatTransitionErrors(mounted) + ".");
                mountedAtExercise = mounted.Succeeded && relationship.State == RelationshipState.Mounted;
            }
            else
            {
                assertions.Check(relationship.State == RelationshipState.Unmounted,
                    "Non-mounted control retained exact Unmounted state.");
            }

            if (assertions.FailureCount != 0)
            {
                BeginCleanup();
                return;
            }

            targetService = new DiagnosticCombatTargetService(logger);
            var spawnPoint = FindWalkablePoint(mount.Position, SpawnDistance);
            var requireDurability = !IsTargetDeathRow;
            target = targetService.Spawn(
                rider,
                mount,
                spawnPoint,
                request.RunId + "-" + rowIndex.ToString(CultureInfo.InvariantCulture),
                true,
                requireDurability);
            targetId = target?.UniqueId;
            assertions.Check(target != null && target.IsInState && target.View != null &&
                    target.IsEnemy(rider) && rider.IsEnemy(target),
                "Runtime-only hostile target passed exact creation and hostility gates.");
            if (assertions.FailureCount != 0)
            {
                BeginCleanup();
                return;
            }
            var rangeProbe = new MountedPairSingleAttack(target, rider, mount, true);
            rangeProbe.Init(rider);
            float requestedDistance;
            assertions.Check(MountedCombatSpatialPolicy.TryCalculateDiagnosticApproachTargetDistance(
                    rangeProbe.PairApproachRadius,
                    out requestedDistance),
                "Control target admits a bounded placement outside the mounted pair attack radius.");
            if (assertions.FailureCount != 0)
            {
                BeginCleanup();
                return;
            }
            var exactPoint = FindWalkablePoint(
                mount.Position,
                requestedDistance,
                MountedCombatSpatialPolicy.DiagnosticPlacementTolerance);
            target.Translocate(exactPoint, null);
            target.View.ForcePlaceAboveGround();
            target.Commands.InterruptAll();
            target.HoldState = true;
            assertions.Check(targetService.PrepareForPlayerClick(target),
                "Runtime-only target passed exact visibility and stopped-agent gates.");
            assertions.Check(targetService.QueueBidirectionalCombatMemory(rider, target),
                "Exact rider/target combat memory was queued through the native seam.");
            if (assertions.FailureCount != 0)
            {
                BeginCleanup();
                return;
            }

            step = ControlStep.AwaitCombat;
        }

        private void AwaitCombatAndExercise()
        {
            var game = Game.Instance;
            if (game == null)
            {
                return;
            }
            if (game.IsPaused)
            {
                game.IsPaused = false;
            }
            if (!targetService.RefreshBidirectionalCombatMemoryLease() ||
                !rider.IsInCombat || !mount.IsInCombat || !target.IsInCombat ||
                !(game.Player?.IsInCombat ?? false) ||
                rider.CombatState == null || !rider.CombatState.Prepared ||
                !rider.CombatState.CanActInCombat ||
                rider.Descriptor?.State == null || !rider.Descriptor.State.IsConscious ||
                target.Descriptor?.State == null || !target.Descriptor.State.IsConscious ||
                game.State?.AwakeUnits == null || !game.State.AwakeUnits.Contains(rider) ||
                !game.State.AwakeUnits.Contains(target) || game.CurrentMode != GameModeType.Default)
            {
                return;
            }

            assertions.Check(IsNonMountedRow
                    ? relationship.State == RelationshipState.Unmounted &&
                        rider.View.AgentASP.enabled == riderAgentInitiallyEnabled &&
                        mount.View.AgentASP.enabled == mountAgentInitiallyEnabled
                    : relationship.State == RelationshipState.Mounted &&
                        relationship.Runtime.PoseConfigured && relationship.Runtime.PoseHealthy &&
                        relationship.Runtime.PoseFrameApplied && !rider.View.AgentASP.enabled &&
                        mount.View.AgentASP.enabled,
                IsNonMountedRow
                    ? "Non-mounted control retained exact stock movement state."
                    : "Mounted control retained the accepted pose and Mammoth-only pathfinding authority.");
            assertions.Check(targetService.PrepareForPlayerClick(target),
                "Target retained exact player-click safety immediately before the control exercise.");
            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            var selected = SelectionManager.Instance.SelectedUnits;
            assertions.Check(selected != null && selected.Count == 1 && selected[0] == rider,
                "Exactly the rider owned selection at the control boundary.");
            if (assertions.FailureCount != 0)
            {
                BeginCleanup();
                return;
            }

            CaptureResourcesBefore();
            ruleProbe = new MountedCombatRuleProbe();
            ruleProbe.Arm(rider, mount, rider, target, null);

            if (IsInvalidTargetRow)
            {
                ExerciseInvalidTarget();
                BeginCleanup();
                return;
            }
            if (IsNonMountedRow)
            {
                ExerciseNonMountedIsolation();
                BeginCleanup();
                return;
            }

            assertions.Check(targetService.BeginExpectedAttackDispatch(target),
                "Expected pair-action dispatch marker was set for the active-command control.");
            assertions.Check(combat.Arm(MountedCombatActionKind.RiderMelee),
                "Rider melee armed through the production combat controller.");
            var accepted = new ClickUnitHandler().OnClick(target.View.gameObject, target.Position, 0, false, false);
            observations.CommandAccepted = accepted && combat.HasActiveCommand;
            assertions.Check(observations.CommandAccepted,
                "Production ClickUnitHandler/Harmony path accepted one exact mounted rider command.");
            if (!observations.CommandAccepted)
            {
                BeginCleanup();
                return;
            }

            if (IsCleanupRow)
            {
                observations.CleanupTrigger = "Exception";
                var first = relationship.Dismount(CleanupTrigger.Exception);
                observations.FirstCleanupSucceeded = first.Succeeded &&
                    !first.MovementAuthorityResidual && !first.PresentationResidual;
                var repeated = relationship.Dismount(CleanupTrigger.Exception);
                observations.RepeatedCleanupSucceeded = repeated.Succeeded &&
                    !repeated.MovementAuthorityResidual && !repeated.PresentationResidual;
                combat.Cancel("control cleanup repeat");
                combat.Cancel("control cleanup repeat");
            }
            else
            {
                observations.TargetDamageBefore = target.Damage;
                observations.TargetDamageRequested = (int)target.Stats.HitPoints +
                    (int)target.Stats.Constitution + 1;
                target.Damage = observations.TargetDamageRequested;
                observations.TargetDamageAfter = target.Damage;
            }
            step = ControlStep.AwaitOutcome;
        }

        private void ExerciseInvalidTarget()
        {
            observations.RiderArmed = combat.Arm(MountedCombatActionKind.RiderMelee);
            var riderClick = new ClickUnitHandler().OnClick(null, Vector3.zero, 0, false, false);
            observations.RiderInvalidRejected = !riderClick &&
                string.Equals(combat.LastFeedback, "Choose one living, visible enemy.", StringComparison.Ordinal);
            observations.MountArmed = combat.Arm(MountedCombatActionKind.MountPrimaryNatural);
            var mountClick = new ClickUnitHandler().OnClick(null, Vector3.zero, 0, false, false);
            observations.MountInvalidRejected = !mountClick &&
                string.Equals(combat.LastFeedback, "Choose one living, visible enemy.", StringComparison.Ordinal);
            observations.ArmedCleared = combat.ArmedAction == MountedCombatActionKind.None;
            observations.ActiveCommandAbsent = !combat.HasActiveCommand && combat.LastOutcome == null;
            CaptureResourcesAfter();
            observations.ResourcesUnchanged = ResourcesUnchanged();
            assertions.Check(observations.RiderArmed && observations.RiderInvalidRejected &&
                    observations.MountArmed && observations.MountInvalidRejected &&
                    observations.ArmedCleared && observations.ActiveCommandAbsent,
                "Rider and Mammoth null-target clicks were consumed as exact fail-closed rejections without a command.");
            assertions.Check(observations.ResourcesUnchanged,
                "Invalid targets changed no rider or Mammoth action resource.");
        }

        private void ExerciseNonMountedIsolation()
        {
            observations.CombatActionsHidden = !combat.CanShowCombatActions;
            observations.ArmRejectedUnmounted = !combat.Arm(MountedCombatActionKind.RiderMelee);
            observations.ControllerNotHandledUnmounted = combat.TryHandleUnitClick(
                target.View.gameObject,
                0,
                false) == MountedCombatClickResult.NotHandled;
            observations.RiderAgentUnchangedNonMounted = rider.View.AgentASP.enabled == riderAgentInitiallyEnabled &&
                rider.View.AgentASP.AvoidanceDisabled == riderAvoidanceInitiallyDisabled;
            observations.MountAgentUnchangedNonMounted = mount.View.AgentASP.enabled == mountAgentInitiallyEnabled &&
                mount.View.AgentASP.AvoidanceDisabled == mountAvoidanceInitiallyDisabled;
            observations.ActiveCommandAbsent = !combat.HasActiveCommand && combat.LastOutcome == null;
            CaptureResourcesAfter();
            observations.ResourcesUnchanged = ResourcesUnchanged();
            assertions.Check(observations.CombatActionsHidden && observations.ArmRejectedUnmounted &&
                    observations.ControllerNotHandledUnmounted && observations.ActiveCommandAbsent,
                "Unmounted production controller remained hidden, rejected arming, and returned NotHandled to stock click routing.");
            assertions.Check(observations.RiderAgentUnchangedNonMounted &&
                    observations.MountAgentUnchangedNonMounted && observations.ResourcesUnchanged,
                "Non-mounted control changed no pair movement, avoidance, or action resource state.");
        }

        private void AwaitControlledOutcome()
        {
            if (targetService != null)
            {
                targetService.RefreshBidirectionalCombatMemoryLease();
            }

            var outcome = combat.LastOutcome;
            if (outcome == null || combat.HasActiveCommand)
            {
                return;
            }

            CaptureResourcesAfter();
            observations.CommandInterrupted = string.Equals(outcome.Result, "Interrupt", StringComparison.Ordinal) &&
                string.Equals(outcome.TerminalReason, "Interrupt", StringComparison.Ordinal);
            observations.ChildAttackStartCount = outcome.ChildAttackStartCount;
            observations.AttackRuleCount = ruleProbe?.AttackRuleCount ?? 0;
            observations.AttackRollCount = ruleProbe?.AttackRollCount ?? 0;
            observations.DamageRuleCount = ruleProbe?.DamageRuleCount ?? 0;
            observations.UnexpectedPairAttackCount = ruleProbe?.UnexpectedPairAttackCount ?? 0;
            observations.ForcedD20Count = ruleProbe?.ForcedD20Count ?? 0;
            observations.ResourcesUnchanged = ResourcesUnchanged();

            if (IsTargetDeathRow)
            {
                targetService.ObserveTargetLifeState();
                var life = targetService.LastObservedLife;
                observations.TargetLifeTransitionObserved = targetService.LifeTransitionCount > 0;
                observations.TargetDeadOrFinallyDead = life != null &&
                    (life.Dead || life.FinallyDead || !life.Conscious);
                observations.RelationshipPreservedAfterTargetDeath =
                    relationship.State == RelationshipState.Mounted;
                assertions.Check(observations.TargetDamageAfter == observations.TargetDamageRequested &&
                        observations.TargetDamageRequested > (int)target.Stats.HitPoints +
                            (int)target.Stats.Constitution,
                    "Target-death control used one exact public lethal Damage value on only the disposable target.");
                assertions.Check(observations.TargetLifeTransitionObserved &&
                        observations.TargetDeadOrFinallyDead && observations.CommandInterrupted &&
                        observations.ChildAttackStartCount == 0,
                    "Target death invalidated the active mounted command before child attack admission.");
                assertions.Check(observations.RelationshipPreservedAfterTargetDeath,
                    "Unrelated target death preserved the valid mounted relationship until ordinary cleanup.");
            }
            else
            {
                assertions.Check(observations.FirstCleanupSucceeded &&
                        observations.RepeatedCleanupSucceeded && observations.CommandInterrupted &&
                        observations.ChildAttackStartCount == 0 &&
                        relationship.State == RelationshipState.Unmounted,
                    "Exception-recovery cleanup interrupted the active command and repeated idempotently without residue.");
            }

            assertions.Check(observations.AttackRuleCount == 0 &&
                    observations.AttackRollCount == 0 && observations.DamageRuleCount == 0 &&
                    observations.UnexpectedPairAttackCount == 0 && observations.ForcedD20Count == 0,
                "Control boundary emitted zero attack, roll, damage, opportunity, or duplicate rule chains.");
            assertions.Check(observations.ResourcesUnchanged,
                "Pre-child target-death or cleanup interruption changed no pair action resource.");
            BeginCleanup();
        }

        private void BeginCleanup()
        {
            if (step == ControlStep.AwaitCleanup || completed)
            {
                return;
            }

            try
            {
                combat.Cancel("combat-control row cleanup");
                var cleanup = relationship.Dismount(CleanupTrigger.Manual);
                relationshipClean = cleanup.Succeeded && !cleanup.MovementAuthorityResidual &&
                    !cleanup.PresentationResidual && relationship.State == RelationshipState.Unmounted;
                TryLeaveCombat(target);
                TryLeaveCombat(mount);
                TryLeaveCombat(rider);
                targetRemoved = targetService == null || targetService.DestroyAndVerify();
            }
            catch (Exception exception)
            {
                assertions.Fail("Combat-control cleanup threw " + exception.GetType().Name + ": " + exception.Message);
                logger.Exception("Combat-control cleanup", exception);
            }
            cleanupStartedAtSeconds = rowClock.Elapsed.TotalSeconds;
            cleanupFrame = frameNumber;
            step = ControlStep.AwaitCleanup;
        }

        private void VerifyCleanupAndAdvance()
        {
            if (frameNumber <= cleanupFrame)
            {
                return;
            }
            if (!targetRemoved && targetService != null)
            {
                targetRemoved = targetService.DestroyAndVerify();
            }
            combatCleared = (rider == null || !rider.IsInCombat) &&
                (mount == null || !mount.IsInCombat) &&
                (target == null || !target.IsInState || !target.IsInCombat) &&
                !(Game.Instance?.Player?.IsInCombat ?? false);
            if ((!targetRemoved || !combatCleared) &&
                rowClock.Elapsed.TotalSeconds - cleanupStartedAtSeconds < CleanupTimeoutSeconds)
            {
                return;
            }

            RestorePause();
            agentsRestored = rider?.View?.AgentASP != null && mount?.View?.AgentASP != null &&
                rider.View.AgentASP.enabled == riderAgentInitiallyEnabled &&
                mount.View.AgentASP.enabled == mountAgentInitiallyEnabled &&
                rider.View.AgentASP.AvoidanceDisabled == riderAvoidanceInitiallyDisabled &&
                mount.View.AgentASP.AvoidanceDisabled == mountAvoidanceInitiallyDisabled;
            assertions.Check(targetRemoved && targetService != null &&
                    targetService.TargetEntityRemoved && targetService.RuntimeGroupRemoved &&
                    targetService.RuntimeFactionRemoved && targetService.CombatMemoryRemoved &&
                    targetService.TargetDurabilityLeaseReleased && targetService.TargetBrainLeaseReleased &&
                    targetService.TargetSleeplessLeaseReleased && targetService.NonPairPartyAiLeaseRestored,
                "Disposable target and every diagnostic lease were removed with zero residue.");
            assertions.Check(relationshipClean && relationship.State == RelationshipState.Unmounted &&
                    relationship.Rider == null && relationship.Mount == null &&
                    relationship.Runtime.MovementAgent == null &&
                    !relationship.Runtime.HasPresentationAttachmentResidue,
                "Control row retained no relationship, movement, or presentation residue.");
            assertions.Check(combatCleared && agentsRestored && pauseRestored,
                "Combat, stock agent/avoidance state, and exact pause state restored after the control row.");

            WriteEvidence();
            var status = assertions.FailureCount == 0 ? "PASS" : "FAIL";
            results.Add(new RuntimeSubscenarioResult
            {
                Name = currentRow,
                Status = status,
                AssertionPassCount = assertions.PassCount,
                AssertionFailCount = assertions.FailureCount,
                Errors = assertions.Errors
            });
            if (assertions.FailureCount != 0)
            {
                errors.AddRange(assertions.Errors);
            }

            DisposeRowServices();
            rowIndex++;
            BeginNextRow();
        }

        private void WriteEvidence()
        {
            var record = new ControlEvidenceRecord
            {
                SchemaVersion = 1,
                ArtifactKind = "combat-core-control-evidence",
                RunId = request.RunId,
                Scenario = request.Scenario,
                Row = currentRow,
                RowIndex = rowIndex,
                Sequence = rowIndex,
                Frame = frameNumber,
                UtcTimestamp = DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                Branch = request.Branch,
                Commit = request.Commit,
                ProductVersion = request.ProductVersion,
                DllSha256 = dllSha256,
                DllMvid = dllMvid,
                Status = assertions.FailureCount == 0 ? "PASS" : "FAIL",
                RiderId = rider?.UniqueId,
                MountId = mount?.UniqueId,
                TargetId = targetId,
                MountedAtExercise = mountedAtExercise,
                ProductionPath = ProductionPathFor(currentRow),
                Observations = observations,
                Resources = new ControlResourceEvidence
                {
                    RiderStandardBefore = riderStandardBefore,
                    RiderStandardAfter = riderStandardAfter,
                    RiderMoveBefore = riderMoveBefore,
                    RiderMoveAfter = riderMoveAfter,
                    MountStandardBefore = mountStandardBefore,
                    MountStandardAfter = mountStandardAfter,
                    MountMoveBefore = mountMoveBefore,
                    MountMoveAfter = mountMoveAfter
                },
                Cleanup = new ControlCleanupEvidence
                {
                    TargetRemoved = targetRemoved,
                    RelationshipClean = relationshipClean,
                    CombatCleared = combatCleared,
                    AgentsRestored = agentsRestored,
                    PauseRestored = pauseRestored,
                    RuntimeLockOrDeploymentCreated = false,
                    ResidualState = relationship.Rider != null || relationship.Mount != null ||
                        relationship.Runtime.MovementAgent != null ||
                        relationship.Runtime.HasPresentationAttachmentResidue
                },
                AssertionPassCount = assertions.PassCount,
                AssertionFailCount = assertions.FailureCount,
                Errors = assertions.Errors
            };

            var json = JsonConvert.SerializeObject(record, EvidenceJsonSettings);
            var mode = rowIndex == 0 ? FileMode.CreateNew : FileMode.Append;
            using (var stream = new FileStream(evidencePath, mode, FileAccess.Write, FileShare.Read))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false), 1024, true))
            {
                writer.WriteLine(json);
                writer.Flush();
                stream.Flush(true);
            }
        }

        private void CaptureResourcesBefore()
        {
            riderStandardBefore = rider.CombatState.Cooldown.StandardAction;
            mountStandardBefore = mount.CombatState.Cooldown.StandardAction;
            riderMoveBefore = rider.CombatState.Cooldown.MoveAction;
            mountMoveBefore = mount.CombatState.Cooldown.MoveAction;
            CaptureResourcesAfter();
        }

        private void CaptureResourcesAfter()
        {
            riderStandardAfter = rider?.CombatState == null ? float.MaxValue : rider.CombatState.Cooldown.StandardAction;
            mountStandardAfter = mount?.CombatState == null ? float.MaxValue : mount.CombatState.Cooldown.StandardAction;
            riderMoveAfter = rider?.CombatState == null ? float.MaxValue : rider.CombatState.Cooldown.MoveAction;
            mountMoveAfter = mount?.CombatState == null ? float.MaxValue : mount.CombatState.Cooldown.MoveAction;
        }

        private bool ResourcesUnchanged()
        {
            return Math.Abs(riderStandardAfter - riderStandardBefore) <= 0.01f &&
                Math.Abs(mountStandardAfter - mountStandardBefore) <= 0.01f &&
                Math.Abs(riderMoveAfter - riderMoveBefore) <= 0.01f &&
                Math.Abs(mountMoveAfter - mountMoveBefore) <= 0.01f;
        }

        private void DisposeRowServices()
        {
            try { ruleProbe?.Dispose(); }
            catch (Exception exception) { errors.Add("Control rule-probe disposal failed: " + exception.Message); }
            ruleProbe = null;
            try { targetService?.Dispose(); }
            catch (Exception exception) { errors.Add("Control target-service disposal failed: " + exception.Message); }
            targetService = null;
            target = null;
            targetId = null;
            rider = null;
            mount = null;
        }

        private void BestEffortCleanup()
        {
            try { combat.Cancel("combat-control engine disposal"); }
            catch (Exception exception) { errors.Add("Combat-control cancellation during disposal failed: " + exception.Message); }
            try
            {
                if (relationship.State != RelationshipState.Unmounted &&
                    relationship.State != RelationshipState.Disposed)
                {
                    relationship.Dismount(CleanupTrigger.ProcessTeardown);
                }
            }
            catch (Exception exception) { errors.Add("Combat-control relationship disposal failed: " + exception.Message); }
            TryLeaveCombat(target);
            TryLeaveCombat(mount);
            TryLeaveCombat(rider);
            try { targetService?.DestroyAndVerify(); }
            catch (Exception exception) { errors.Add("Combat-control target disposal failed: " + exception.Message); }
        }

        private Vector3 FindWalkablePoint(
            Vector3 origin,
            float requestedDistance,
            float distanceTolerance = 0.4f)
        {
            if (global::AstarPath.active == null)
            {
                throw new InvalidOperationException("Active native navigation graph is unavailable.");
            }
            var baseDirection = mount?.View == null ? Vector3.forward : mount.View.transform.forward;
            baseDirection.y = 0f;
            if (baseDirection.sqrMagnitude < 0.01f)
            {
                baseDirection = Vector3.forward;
            }
            baseDirection.Normalize();
            for (var index = 0; index < 16; index++)
            {
                var direction = Quaternion.Euler(0f, index * 22.5f, 0f) * baseDirection;
                var candidate = origin + direction * requestedDistance;
                var nearest = global::AstarPath.active.GetNearest(candidate);
                if (nearest.node == null || !nearest.node.Walkable)
                {
                    continue;
                }
                var point = nearest.clampedPosition;
                if (HorizontalDistance(origin, point) > MountedCombatSpatialPolicy.RangeTolerance &&
                    Math.Abs(HorizontalDistance(origin, point) - requestedDistance) <= distanceTolerance)
                {
                    return point;
                }
            }
            throw new InvalidOperationException("No bounded walkable combat-control target point was available.");
        }

        private void RestorePause()
        {
            if (!pauseLeaseOwned)
            {
                return;
            }
            if (Game.Instance != null)
            {
                Game.Instance.IsPaused = originalPause;
                pauseRestored = Game.Instance.IsPaused == originalPause;
            }
            pauseLeaseOwned = false;
        }

        private void RestoreSettings()
        {
            if (settingLeaseOwned)
            {
                settings.EnableUnsafeMovementExperiment = originalUnsafeExperimentSetting;
                settingLeaseOwned = false;
            }
        }

        private static void TryLeaveCombat(UnitEntityData unit)
        {
            if (unit != null && unit.IsInState && unit.IsInCombat)
            {
                unit.LeaveCombat();
            }
        }

        private static float HorizontalDistance(Vector3 first, Vector3 second)
        {
            var dx = first.x - second.x;
            var dz = first.z - second.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private static string ProductionPathFor(string row)
        {
            if (string.Equals(row, InvalidTargetRow, StringComparison.Ordinal))
            {
                return "ClickUnitHandler.OnClick -> MountedCombatController.TryHandleUnitClick -> MountedCombatActionEvaluator.Evaluate";
            }
            if (string.Equals(row, TargetDeathRow, StringComparison.Ordinal))
            {
                return "UnitEntityData.Damage -> mounted command liveness -> UnitCommand.Interrupt";
            }
            if (string.Equals(row, CleanupRow, StringComparison.Ordinal))
            {
                return "MountedRelationshipCoordinator.Dismount(Exception) -> MountedCombatController.HandleDismounting";
            }
            return "MountedCombatController.Arm/TryHandleUnitClick -> NotHandled stock delegation";
        }

        private static string FormatTransitionErrors(TransitionResult transition)
        {
            return transition == null || transition.Errors == null
                ? "no transition detail"
                : string.Join("; ", transition.Errors);
        }

        private static string ComputeSha256(string path)
        {
            using (var algorithm = SHA256.Create())
            using (var stream = File.OpenRead(path))
            {
                return BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        private bool IsInvalidTargetRow => string.Equals(currentRow, InvalidTargetRow, StringComparison.Ordinal);

        private bool IsTargetDeathRow => string.Equals(currentRow, TargetDeathRow, StringComparison.Ordinal);

        private bool IsCleanupRow => string.Equals(currentRow, CleanupRow, StringComparison.Ordinal);

        private bool IsNonMountedRow => string.Equals(currentRow, NonMountedRow, StringComparison.Ordinal);

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(RuntimeCombatControlScenarioEngine));
            }
        }

        private enum ControlStep
        {
            Begin,
            AwaitCombat,
            AwaitOutcome,
            AwaitCleanup
        }

        private sealed class AssertionRecorder
        {
            private readonly List<string> errors = new List<string>();

            public int PassCount { get; private set; }

            public int FailureCount { get; private set; }

            public IReadOnlyList<string> Errors => errors;

            public void Check(bool condition, string message)
            {
                if (condition)
                {
                    PassCount++;
                }
                else
                {
                    Fail(message);
                }
            }

            public void Fail(string message)
            {
                FailureCount++;
                errors.Add(message);
            }
        }

        private sealed class ControlEvidenceRecord
        {
            public int SchemaVersion { get; set; }
            public string ArtifactKind { get; set; }
            public string RunId { get; set; }
            public string Scenario { get; set; }
            public string Row { get; set; }
            public int RowIndex { get; set; }
            public int Sequence { get; set; }
            public int Frame { get; set; }
            public string UtcTimestamp { get; set; }
            public string Branch { get; set; }
            public string Commit { get; set; }
            public string ProductVersion { get; set; }
            public string DllSha256 { get; set; }
            public string DllMvid { get; set; }
            public string Status { get; set; }
            public string RiderId { get; set; }
            public string MountId { get; set; }
            public string TargetId { get; set; }
            public bool MountedAtExercise { get; set; }
            public string ProductionPath { get; set; }
            public ControlObservations Observations { get; set; }
            public ControlResourceEvidence Resources { get; set; }
            public ControlCleanupEvidence Cleanup { get; set; }
            public int AssertionPassCount { get; set; }
            public int AssertionFailCount { get; set; }
            public IReadOnlyList<string> Errors { get; set; }
        }

        private sealed class ControlObservations
        {
            public string ControlKind { get; set; }
            public bool RiderArmed { get; set; }
            public bool MountArmed { get; set; }
            public bool RiderInvalidRejected { get; set; }
            public bool MountInvalidRejected { get; set; }
            public bool ArmedCleared { get; set; }
            public bool ActiveCommandAbsent { get; set; }
            public bool CombatActionsHidden { get; set; }
            public bool ArmRejectedUnmounted { get; set; }
            public bool ControllerNotHandledUnmounted { get; set; }
            public bool RiderAgentUnchangedNonMounted { get; set; }
            public bool MountAgentUnchangedNonMounted { get; set; }
            public bool CommandAccepted { get; set; }
            public int TargetDamageBefore { get; set; }
            public int TargetDamageRequested { get; set; }
            public int TargetDamageAfter { get; set; }
            public bool TargetLifeTransitionObserved { get; set; }
            public bool TargetDeadOrFinallyDead { get; set; }
            public bool CommandInterrupted { get; set; }
            public string CleanupTrigger { get; set; }
            public bool FirstCleanupSucceeded { get; set; }
            public bool RepeatedCleanupSucceeded { get; set; }
            public int ChildAttackStartCount { get; set; }
            public int AttackRuleCount { get; set; }
            public int AttackRollCount { get; set; }
            public int DamageRuleCount { get; set; }
            public int UnexpectedPairAttackCount { get; set; }
            public int ForcedD20Count { get; set; }
            public bool RelationshipPreservedAfterTargetDeath { get; set; }
            public bool ResourcesUnchanged { get; set; }
        }

        private sealed class ControlResourceEvidence
        {
            public float RiderStandardBefore { get; set; }
            public float RiderStandardAfter { get; set; }
            public float RiderMoveBefore { get; set; }
            public float RiderMoveAfter { get; set; }
            public float MountStandardBefore { get; set; }
            public float MountStandardAfter { get; set; }
            public float MountMoveBefore { get; set; }
            public float MountMoveAfter { get; set; }
        }

        private sealed class ControlCleanupEvidence
        {
            public bool TargetRemoved { get; set; }
            public bool RelationshipClean { get; set; }
            public bool CombatCleared { get; set; }
            public bool AgentsRestored { get; set; }
            public bool PauseRestored { get; set; }
            public bool RuntimeLockOrDeploymentCreated { get; set; }
            public bool ResidualState { get; set; }
        }
    }
}
