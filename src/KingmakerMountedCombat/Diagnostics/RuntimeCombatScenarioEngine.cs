using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Kingmaker;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.Selection;
using Kingmaker.View;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using KingmakerMountedCombat.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    /// <summary>
    /// Runs exact, save-backed combat probes against a runtime-only hostile target. The
    /// first qualified row is intentionally narrow: one stationary rider melee hit in
    /// real time, entered through the real ClickUnitHandler Harmony seam.
    /// </summary>
    internal sealed class RuntimeCombatScenarioEngine : IDisposable
    {
        internal const string EvidenceFileName = "combat-scenario-evidence.jsonl";
        private const string RiderHitRealTime = "mounted-rider-melee-hit-rt";
        private const double RowTimeoutSeconds = 30.0d;
        private const float SpawnDistance = 6.0f;
        private const float MinimumFinalTargetDistance = 3.02f;
        private const float RangeInset = 0.12f;

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

        private DiagnosticCombatTargetService targetService;
        private MountedCombatRuleProbe ruleProbe;
        private AssertionRecorder assertions;
        private UnitEntityData rider;
        private UnitEntityData mount;
        private UnitEntityData target;
        private string targetId;
        private string currentRow;
        private CombatEngineStep step;
        private bool originalUnsafeExperimentSetting;
        private bool settingLeaseOwned;
        private bool started;
        private bool completed;
        private bool disposed;
        private int frameNumber;
        private int cleanupFrame;
        private float riderStandardBefore;
        private float mountStandardBefore;
        private float riderMoveBefore;
        private float mountMoveBefore;
        private float riderStandardAfter;
        private float mountStandardAfter;
        private float riderMoveAfter;
        private float mountMoveAfter;
        private float pairApproachRadius;
        private float targetDistanceAtClick;
        private Vector3 riderPositionAtClick;
        private Vector3 mountPositionAtClick;
        private Vector3 targetPositionAtClick;
        private bool clickAccepted;
        private MountedPairAttackOutcome outcome;
        private bool targetRemoved;
        private bool targetEntityRemoved;
        private bool targetRuntimeGroupRemoved;
        private bool targetRuntimeFactionRemoved;
        private bool relationshipClean;
        private bool combatCleared;
        private bool riderAgentInitiallyEnabled;
        private bool mountAgentInitiallyEnabled;
        private bool riderAvoidanceInitiallyDisabled;
        private bool mountAvoidanceInitiallyDisabled;
        private bool rowEvidenceWritten;
        private string poseProfileAtOutcome;
        private bool poseHealthyAtOutcome;

        public RuntimeCombatScenarioEngine(
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
            return string.Equals(scenario, RiderHitRealTime, StringComparison.Ordinal);
        }

        public void Start()
        {
            ThrowIfDisposed();
            if (started)
            {
                throw new InvalidOperationException("Combat scenario engine has already started.");
            }
            if (!SupportsScenario(request.Scenario))
            {
                throw new InvalidOperationException("Scenario is outside the exact combat runtime allowlist.");
            }

            started = true;
            currentRow = request.Scenario;
            assertions = new AssertionRecorder();
            originalUnsafeExperimentSetting = settings.EnableUnsafeMovementExperiment;
            settings.EnableUnsafeMovementExperiment = true;
            settingLeaseOwned = true;
            rowClock.Start();
            step = CombatEngineStep.BeginRow;
            logger.Info("Combat runtime engine started for " + currentRow + ".");
        }

        public void Update()
        {
            ThrowIfDisposed();
            if (!started)
            {
                throw new InvalidOperationException("Combat scenario engine must be started before Update.");
            }
            if (completed)
            {
                return;
            }

            frameNumber++;
            try
            {
                if (rowClock.Elapsed.TotalSeconds > RowTimeoutSeconds && step != CombatEngineStep.AwaitCleanupFrame)
                {
                    assertions.Fail("Combat row exceeded its " + RowTimeoutSeconds + " second monotonic deadline at " + step + ".");
                    BeginCleanup();
                    return;
                }

                switch (step)
                {
                    case CombatEngineStep.BeginRow:
                        BeginRow();
                        break;
                    case CombatEngineStep.AwaitMountedFrame:
                        EnterCombat();
                        break;
                    case CombatEngineStep.AwaitCombatFrame:
                        IssueAttackWhenReady();
                        break;
                    case CombatEngineStep.AwaitOutcome:
                        ObserveOutcome();
                        break;
                    case CombatEngineStep.AwaitCleanupFrame:
                        VerifyCleanupAndComplete();
                        break;
                    default:
                        throw new InvalidOperationException("Unexpected combat engine step: " + step + ".");
                }
            }
            catch (Exception exception)
            {
                logger.Exception("Combat runtime row threw", exception);
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

            if (started && !completed)
            {
                errors.Add("Combat engine was disposed before its exact row completed.");
            }

            try
            {
                BestEffortCleanup();
                if (!rowEvidenceWritten && assertions != null)
                {
                    WriteRowEvidence();
                }
            }
            finally
            {
                ruleProbe?.Dispose();
                ruleProbe = null;
                targetService?.Dispose();
                targetService = null;
                RestoreSettings();
                rowClock.Stop();
                disposed = true;
            }
        }

        private void BeginRow()
        {
            assertions.Check(relationship.State == RelationshipState.Unmounted,
                "Relationship began Unmounted.");
            assertions.Check(!CombatController.IsInTurnBasedCombat(),
                "Real-time row began outside turn-based combat.");
            assertions.Check(Game.Instance != null && Game.Instance.CurrentlyLoadedArea != null &&
                    Game.Instance.CurrentMode == Kingmaker.GameModes.GameModeType.Default,
                "Loaded Working fixture began in exact Default gameplay mode.");
            if (assertions.FailureCount != 0)
            {
                BeginCleanup();
                return;
            }

            string resolutionError;
            assertions.Check(relationship.TryResolveAutomationPair(out rider, out mount, out resolutionError),
                "Exact Medium-humanoid/Mammoth automation pair resolved: " + (resolutionError ?? "unknown error") + ".");
            if (rider == null || mount == null || rider.View?.AgentASP == null || mount.View?.AgentASP == null)
            {
                assertions.Fail("Resolved combat pair lacks exact views and stock agents.");
                BeginCleanup();
                return;
            }

            riderAgentInitiallyEnabled = rider.View.AgentASP.enabled;
            mountAgentInitiallyEnabled = mount.View.AgentASP.enabled;
            riderAvoidanceInitiallyDisabled = rider.View.AgentASP.AvoidanceDisabled;
            mountAvoidanceInitiallyDisabled = mount.View.AgentASP.AvoidanceDisabled;
            assertions.Check(riderAgentInitiallyEnabled && mountAgentInitiallyEnabled &&
                    !riderAvoidanceInitiallyDisabled && !mountAvoidanceInitiallyDisabled,
                "Pair began with exact stock movement authority and avoidance.");
            assertions.Check(string.Equals(mount.Blueprint.AssetGuid, KingmakerMountedPairRuntime.MammothBlueprintGuid, StringComparison.Ordinal),
                "Resolved mount is the exact supported Mammoth profile.");
            assertions.Check(SelectionManager.Instance != null,
                "Native SelectionManager is available.");
            if (assertions.FailureCount != 0)
            {
                BeginCleanup();
                return;
            }

            SelectionManager.Instance.SelectUnit(rider.View, true, false, false);
            var mounted = relationship.MountAutomationPair();
            assertions.Check(mounted.Succeeded && relationship.State == RelationshipState.Mounted,
                "Exact automation pair mounted for combat: " + FormatTransitionErrors(mounted) + ".");
            if (!mounted.Succeeded || relationship.State != RelationshipState.Mounted)
            {
                BeginCleanup();
                return;
            }
            step = CombatEngineStep.AwaitMountedFrame;
        }

        private void EnterCombat()
        {
            assertions.Check(relationship.State == RelationshipState.Mounted &&
                    relationship.Runtime.PoseConfigured && relationship.Runtime.PoseHealthy &&
                    relationship.Runtime.PoseFrameApplied,
                "Accepted Mammoth-specific pose remained healthy on the mounted combat frame.");
            assertions.Check(!rider.View.AgentASP.enabled && mount.View.AgentASP.enabled &&
                    rider.View.AgentASP.AvoidanceDisabled && !mount.View.AgentASP.AvoidanceDisabled,
                "Mammoth is the sole stock pathfinding authority while mounted.");
            if (assertions.FailureCount != 0)
            {
                BeginCleanup();
                return;
            }

            targetService = new DiagnosticCombatTargetService(logger);
            var spawnPoint = FindWalkablePoint(mount.Position, rider.Position, SpawnDistance, 0.4f, false);
            target = targetService.Spawn(rider, spawnPoint, request.RunId, true);
            targetId = target.UniqueId;
            assertions.Check(target != null && target.IsInState && target.View != null && target.IsEnemy(rider) && rider.IsEnemy(target),
                "Runtime-only hostile Mammoth target passed exact creation gates.");

            var rangeProbe = new MountedPairSingleAttack(target, rider, mount, true);
            rangeProbe.Init(rider);
            pairApproachRadius = rangeProbe.PairApproachRadius;
            assertions.Check(pairApproachRadius > MinimumFinalTargetDistance,
                "Mounted rider pair approach radius admits the bounded diagnostic placement.");
            if (assertions.FailureCount != 0)
            {
                BeginCleanup();
                return;
            }

            var finalDistance = Math.Max(MinimumFinalTargetDistance, pairApproachRadius - RangeInset);
            var attackPoint = FindWalkablePoint(mount.Position, rider.Position, finalDistance, RangeInset * 0.5f, true);
            target.Translocate(attackPoint, null);
            target.View.ForcePlaceAboveGround();
            target.Commands.InterruptAll();
            target.HoldState = true;

            target.JoinCombat();
            rider.JoinCombat();
            mount.JoinCombat();
            step = CombatEngineStep.AwaitCombatFrame;
        }

        private void IssueAttackWhenReady()
        {
            if (!rider.IsInCombat || !mount.IsInCombat || !target.IsInCombat || !(Game.Instance?.Player?.IsInCombat ?? false))
            {
                return;
            }

            assertions.Check(!CombatController.IsInTurnBasedCombat(),
                "Combat remained real time at dispatch.");
            assertions.Check(relationship.State == RelationshipState.Mounted,
                "Native combat entry retained the mounted relationship.");
            assertions.Check(target.IsInState && target.Descriptor.State.IsConscious && !target.Descriptor.State.IsFinallyDead,
                "Diagnostic target remained live at dispatch.");

            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            var selected = SelectionManager.Instance.SelectedUnits;
            assertions.Check(selected != null && selected.Count == 1 && selected[0] == rider,
                "Exactly the rider owned player selection at dispatch.");
            assertions.Check(combat.CanShowCombatActions,
                "Mounted combat actions were available only for the exact selected pair in combat.");
            assertions.Check(rider.HasStandardAction(),
                "Rider owned an available Standard action before dispatch.");
            if (assertions.FailureCount != 0)
            {
                BeginCleanup();
                return;
            }

            riderStandardBefore = rider.CombatState.Cooldown.StandardAction;
            mountStandardBefore = mount.CombatState.Cooldown.StandardAction;
            riderMoveBefore = rider.CombatState.Cooldown.MoveAction;
            mountMoveBefore = mount.CombatState.Cooldown.MoveAction;
            riderPositionAtClick = rider.Position;
            mountPositionAtClick = mount.Position;
            targetPositionAtClick = target.Position;
            targetDistanceAtClick = HorizontalDistance(mountPositionAtClick, targetPositionAtClick);
            assertions.Check(targetDistanceAtClick <= pairApproachRadius + MountedCombatSpatialPolicy.RangeTolerance,
                "Target was inside the exact Mammoth-origin rider melee radius at dispatch.");
            assertions.Check(HorizontalDistance(riderPositionAtClick, targetPositionAtClick) >= 3.0f,
                "Diagnostic target retained the bounded minimum rider-relative separation.");

            ruleProbe = new MountedCombatRuleProbe();
            ruleProbe.Arm(rider, mount, rider, target, 20);
            assertions.Check(combat.Arm(MountedCombatActionKind.RiderMelee),
                "Rider melee armed through the combat controller.");
            if (assertions.FailureCount != 0)
            {
                BeginCleanup();
                return;
            }

            clickAccepted = new ClickUnitHandler().OnClick(target.View.gameObject, target.Position, 0, false, false);
            assertions.Check(clickAccepted,
                "Native ClickUnitHandler/Harmony path consumed the exact enemy click.");
            step = CombatEngineStep.AwaitOutcome;
        }

        private void ObserveOutcome()
        {
            if (combat.LastOutcome == null)
            {
                return;
            }

            outcome = combat.LastOutcome;
            riderStandardAfter = rider.CombatState.Cooldown.StandardAction;
            mountStandardAfter = mount.CombatState.Cooldown.StandardAction;
            riderMoveAfter = rider.CombatState.Cooldown.MoveAction;
            mountMoveAfter = mount.CombatState.Cooldown.MoveAction;

            assertions.Check(outcome.Action == MountedCombatActionKind.RiderMelee,
                "Terminal command retained rider-melee action identity.");
            assertions.Check(string.Equals(outcome.ActorId, rider.UniqueId, StringComparison.Ordinal) &&
                    string.Equals(outcome.TargetId, targetId, StringComparison.Ordinal),
                "Terminal command retained exact rider actor and target identity.");
            assertions.Check(string.Equals(outcome.Result, "Success", StringComparison.Ordinal),
                "Native single attack completed successfully.");
            assertions.Check(outcome.ChildAttackStartCount == 1 && outcome.NativeAttackRuleObserved,
                "Exactly one native child attack started and exposed its native attack rule.");
            assertions.Check(outcome.RepathCount == 0,
                "Stationary in-range attack required no delegated movement or repath.");
            assertions.Check(outcome.RiderStandardCharged && riderStandardAfter > riderStandardBefore,
                "Exactly the rider Standard wrapper charged an action resource.");
            assertions.Check(Math.Abs(mountStandardAfter - mountStandardBefore) <= 0.01f &&
                    Math.Abs(mountMoveAfter - mountMoveBefore) <= 0.01f,
                "Mammoth Standard and Move resources were unchanged by the rider attack.");
            assertions.Check(Math.Abs(riderMoveAfter - riderMoveBefore) <= 0.01f,
                "Stationary rider attack did not charge a rider Move action.");
            assertions.Check(ruleProbe.AttackRuleCount == 1 && ruleProbe.AttackRollCount == 1 &&
                    ruleProbe.DamageRuleCount <= 1 && ruleProbe.UnexpectedPairAttackCount == 0,
                "Rulebook observed exactly one rider attack/roll, at most one damage event, and no pair duplicate.");
            assertions.Check(ruleProbe.ForcedD20 == 20 && ruleProbe.ForcedD20Count >= 1 &&
                    (string.Equals(ruleProbe.LastAttackResult, "Hit", StringComparison.Ordinal) ||
                     string.Equals(ruleProbe.LastAttackResult, "CriticalHit", StringComparison.Ordinal)),
                "Deterministic natural 20 produced a hit or critical-hit result.");
            assertions.Check(string.Equals(ruleProbe.LastInitiatorId, rider.UniqueId, StringComparison.Ordinal) &&
                    string.Equals(ruleProbe.LastTargetId, targetId, StringComparison.Ordinal),
                "Rulebook identities remained the exact rider and diagnostic target.");
            assertions.Check(relationship.State == RelationshipState.Mounted &&
                    relationship.Runtime.PoseHealthy && relationship.Runtime.PoseFrameApplied,
                "Mounted relationship and accepted pose remained healthy after the attack.");
            assertions.Check(HorizontalDistance(mountPositionAtClick, mount.Position) <= 0.05f &&
                    HorizontalDistance(targetPositionAtClick, target.Position) <= 0.05f,
                "Rider and target attack remained stationary at the authoritative Mammoth origin.");

            poseProfileAtOutcome = relationship.Runtime.PoseProfileId;
            poseHealthyAtOutcome = relationship.Runtime.PoseHealthy && relationship.Runtime.PoseFrameApplied;

            BeginCleanup();
        }

        private void BeginCleanup()
        {
            if (step == CombatEngineStep.AwaitCleanupFrame || completed)
            {
                return;
            }

            try
            {
                combat.Cancel("runtime combat row cleanup");
                var cleanup = relationship.Dismount(CleanupTrigger.Manual);
                relationshipClean = cleanup.Succeeded && !cleanup.MovementAuthorityResidual &&
                    !cleanup.PresentationResidual && relationship.State == RelationshipState.Unmounted;
                assertions.Check(relationshipClean,
                    "Relationship cleanup restored Unmounted state without movement or presentation residue.");

                TryLeaveCombat(target);
                TryLeaveCombat(mount);
                TryLeaveCombat(rider);
                if (targetService != null)
                {
                    targetRemoved = targetService.DestroyAndVerify();
                    CaptureTargetCleanupState();
                }
                else
                {
                    targetRemoved = true;
                    targetEntityRemoved = true;
                    targetRuntimeGroupRemoved = true;
                    targetRuntimeFactionRemoved = true;
                }
            }
            catch (Exception exception)
            {
                assertions.Fail("Combat cleanup threw " + exception.GetType().Name + ": " + exception.Message);
                logger.Exception("Combat runtime cleanup", exception);
            }
            cleanupFrame = frameNumber;
            step = CombatEngineStep.AwaitCleanupFrame;
        }

        private void VerifyCleanupAndComplete()
        {
            if (frameNumber <= cleanupFrame)
            {
                return;
            }

            if (targetService != null && !targetRemoved)
            {
                targetRemoved = targetService.DestroyAndVerify();
                CaptureTargetCleanupState();
            }

            combatCleared = (rider == null || !rider.IsInCombat) &&
                (mount == null || !mount.IsInCombat) &&
                (target == null || !target.IsInState || !target.IsInCombat) &&
                !(Game.Instance?.Player?.IsInCombat ?? false);
            if ((!combatCleared || !targetRemoved) && rowClock.Elapsed.TotalSeconds < RowTimeoutSeconds)
            {
                return;
            }

            assertions.Check(targetRemoved && targetEntityRemoved &&
                    targetRuntimeGroupRemoved && targetRuntimeFactionRemoved,
                "Runtime-only combat target, project group, and runtime faction were removed with zero residue.");
            assertions.Check(combatCleared,
                "Pair, target, and party left combat before final evidence.");
            assertions.Check(relationship.State == RelationshipState.Unmounted &&
                    relationship.Rider == null && relationship.Mount == null &&
                    relationship.Runtime.MovementAgent == null &&
                    !relationship.Runtime.HasPresentationAttachmentResidue,
                "Final combat row retained no relationship, movement, or presentation residue.");
            if (rider?.View?.AgentASP != null && mount?.View?.AgentASP != null)
            {
                assertions.Check(rider.View.AgentASP.enabled == riderAgentInitiallyEnabled &&
                        mount.View.AgentASP.enabled == mountAgentInitiallyEnabled &&
                        rider.View.AgentASP.AvoidanceDisabled == riderAvoidanceInitiallyDisabled &&
                        mount.View.AgentASP.AvoidanceDisabled == mountAvoidanceInitiallyDisabled,
                    "Pair restored the exact pre-row stock agent and avoidance state.");
            }

            WriteRowEvidence();
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
            RestoreSettings();
            completed = true;
            rowClock.Stop();
            logger.Info("Combat runtime engine completed " + currentRow + " with " + status + ".");
        }

        private void WriteRowEvidence()
        {
            if (rowEvidenceWritten)
            {
                return;
            }

            var selected = SelectionManager.Instance?.SelectedUnits;
            var record = new CombatEvidenceRecord
            {
                SchemaVersion = 1,
                ArtifactKind = "combat-scenario-evidence",
                RunId = request.RunId,
                Scenario = request.Scenario,
                Row = currentRow,
                RowIndex = 0,
                Sequence = 0,
                Frame = frameNumber,
                UtcTimestamp = DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                Branch = request.Branch,
                Commit = request.Commit,
                ProductVersion = request.ProductVersion,
                DllSha256 = dllSha256,
                DllMvid = dllMvid,
                Status = assertions.FailureCount == 0 ? "PASS" : "FAIL",
                Mode = "real-time",
                Action = MountedCombatActionKind.RiderMelee.ToString(),
                ExpectedActor = "rider",
                RiderId = rider?.UniqueId,
                MountId = mount?.UniqueId,
                TargetId = targetId,
                ClickAccepted = clickAccepted,
                PairApproachRadius = pairApproachRadius,
                TargetDistanceAtClick = targetDistanceAtClick,
                RiderPositionAtClick = PositionEvidence.From(riderPositionAtClick),
                MountPositionAtClick = PositionEvidence.From(mountPositionAtClick),
                TargetPositionAtClick = PositionEvidence.From(targetPositionAtClick),
                Resources = new CombatResourceEvidence
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
                Command = CombatCommandEvidence.From(outcome),
                Rules = CombatRuleEvidence.From(ruleProbe),
                Movement = new CombatMovementEvidence
                {
                    AuthoritativeMover = "mount",
                    RepathCount = outcome?.RepathCount ?? 0,
                    RiderStockAgentEnabledAtEnd = rider?.View?.AgentASP == null ? (bool?)null : rider.View.AgentASP.enabled,
                    MountStockAgentEnabledAtEnd = mount?.View?.AgentASP == null ? (bool?)null : mount.View.AgentASP.enabled,
                    RiderAvoidanceDisabledAtEnd = rider?.View?.AgentASP == null ? (bool?)null : rider.View.AgentASP.AvoidanceDisabled,
                    MountAvoidanceDisabledAtEnd = mount?.View?.AgentASP == null ? (bool?)null : mount.View.AgentASP.AvoidanceDisabled
                },
                Pose = new CombatPoseEvidence
                {
                    ProfileId = poseProfileAtOutcome,
                    HealthyAtOutcome = poseHealthyAtOutcome,
                    ConfiguredAtEnd = relationship.Runtime.PoseConfigured,
                    AttachmentLeaseAtEnd = relationship.Runtime.PresentationAttachmentLeaseActive,
                    ResidueAtEnd = relationship.Runtime.HasPresentationAttachmentResidue
                },
                Cleanup = new CombatCleanupEvidence
                {
                    TargetRemoved = targetRemoved,
                    TargetEntityRemoved = targetEntityRemoved,
                    RuntimeGroupRemoved = targetRuntimeGroupRemoved,
                    RuntimeFactionRemoved = targetRuntimeFactionRemoved,
                    RelationshipClean = relationshipClean,
                    CombatCleared = combatCleared,
                    RelationshipState = relationship.State.ToString(),
                    ResidualState = relationship.Rider != null || relationship.Mount != null ||
                        relationship.Runtime.MovementAgent != null || relationship.Runtime.HasPresentationAttachmentResidue,
                    PresentationResidual = relationship.Runtime.HasPresentationAttachmentResidue
                },
                Selection = selected == null
                    ? new string[0]
                    : System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(
                        System.Linq.Enumerable.Where(selected, unit => unit != null),
                        unit => unit.UniqueId)),
                AssertionPassCount = assertions.PassCount,
                AssertionFailCount = assertions.FailureCount,
                Errors = assertions.Errors
            };

            var json = JsonConvert.SerializeObject(record, EvidenceJsonSettings);
            using (var stream = new FileStream(evidencePath, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false), 1024, true))
            {
                writer.WriteLine(json);
                writer.Flush();
                stream.Flush(true);
            }
            rowEvidenceWritten = true;
        }

        private Vector3 FindWalkablePoint(
            Vector3 origin,
            Vector3 riderOrigin,
            float requestedDistance,
            float distanceTolerance,
            bool requireRiderMinimum)
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
                var candidate = origin + (direction * requestedDistance);
                var nearest = global::AstarPath.active.GetNearest(candidate);
                if (nearest.node == null || !nearest.node.Walkable)
                {
                    continue;
                }
                var point = nearest.clampedPosition;
                var originDistance = HorizontalDistance(origin, point);
                var riderDistance = HorizontalDistance(riderOrigin, point);
                if (Math.Abs(originDistance - requestedDistance) <= distanceTolerance &&
                    (!requireRiderMinimum || riderDistance >= 3.0f))
                {
                    return point;
                }
            }

            throw new InvalidOperationException("No bounded walkable diagnostic target point satisfied the exact distance contract.");
        }

        private void BestEffortCleanup()
        {
            try { combat.Cancel("combat engine disposal"); }
            catch (Exception exception) { errors.Add("Combat cancellation during disposal failed: " + exception.Message); }
            try
            {
                if (relationship.State != RelationshipState.Unmounted && relationship.State != RelationshipState.Disposed)
                {
                    var cleanup = relationship.Dismount(CleanupTrigger.ProcessTeardown);
                    if (!cleanup.Succeeded || cleanup.MovementAuthorityResidual || cleanup.PresentationResidual)
                    {
                        errors.Add("Combat engine disposal retained mounted residue.");
                    }
                }
            }
            catch (Exception exception) { errors.Add("Combat relationship disposal cleanup failed: " + exception.Message); }
            TryLeaveCombat(target);
            TryLeaveCombat(mount);
            TryLeaveCombat(rider);
            try
            {
                if (targetService != null && !targetService.DestroyAndVerify())
                {
                    errors.Add("Combat engine disposal retained diagnostic target residue.");
                }
            }
            catch (Exception exception) { errors.Add("Combat target disposal cleanup failed: " + exception.Message); }
        }

        private void CaptureTargetCleanupState()
        {
            targetEntityRemoved = targetService != null && targetService.TargetEntityRemoved;
            targetRuntimeGroupRemoved = targetService != null && targetService.RuntimeGroupRemoved;
            targetRuntimeFactionRemoved = targetService != null && targetService.RuntimeFactionRemoved;
        }

        private static void TryLeaveCombat(UnitEntityData unit)
        {
            if (unit != null && unit.IsInState && unit.IsInCombat)
            {
                unit.LeaveCombat();
            }
        }

        private void RestoreSettings()
        {
            if (settingLeaseOwned)
            {
                settings.EnableUnsafeMovementExperiment = originalUnsafeExperimentSetting;
                settingLeaseOwned = false;
            }
        }

        private static string FormatTransitionErrors(TransitionResult transition)
        {
            return transition == null || transition.Errors == null
                ? "no transition detail"
                : string.Join("; ", transition.Errors);
        }

        private static float HorizontalDistance(Vector3 first, Vector3 second)
        {
            var dx = first.x - second.x;
            var dz = first.z - second.z;
            return Mathf.Sqrt((dx * dx) + (dz * dz));
        }

        private static string ComputeSha256(string path)
        {
            using (var algorithm = SHA256.Create())
            using (var stream = File.OpenRead(path))
            {
                return BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(RuntimeCombatScenarioEngine));
            }
        }

        private enum CombatEngineStep
        {
            BeginRow,
            AwaitMountedFrame,
            AwaitCombatFrame,
            AwaitOutcome,
            AwaitCleanupFrame
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

        private sealed class CombatEvidenceRecord
        {
            public int SchemaVersion { get; set; }
            public string ArtifactKind { get; set; }
            public string RunId { get; set; }
            public string Scenario { get; set; }
            public string Row { get; set; }
            public int RowIndex { get; set; }
            public long Sequence { get; set; }
            public int Frame { get; set; }
            public string UtcTimestamp { get; set; }
            public string Branch { get; set; }
            public string Commit { get; set; }
            public string ProductVersion { get; set; }
            public string DllSha256 { get; set; }
            public string DllMvid { get; set; }
            public string Status { get; set; }
            public string Mode { get; set; }
            public string Action { get; set; }
            public string ExpectedActor { get; set; }
            public string RiderId { get; set; }
            public string MountId { get; set; }
            public string TargetId { get; set; }
            public bool ClickAccepted { get; set; }
            public float PairApproachRadius { get; set; }
            public float TargetDistanceAtClick { get; set; }
            public PositionEvidence RiderPositionAtClick { get; set; }
            public PositionEvidence MountPositionAtClick { get; set; }
            public PositionEvidence TargetPositionAtClick { get; set; }
            public CombatResourceEvidence Resources { get; set; }
            public CombatCommandEvidence Command { get; set; }
            public CombatRuleEvidence Rules { get; set; }
            public CombatMovementEvidence Movement { get; set; }
            public CombatPoseEvidence Pose { get; set; }
            public CombatCleanupEvidence Cleanup { get; set; }
            public IReadOnlyList<string> Selection { get; set; }
            public int AssertionPassCount { get; set; }
            public int AssertionFailCount { get; set; }
            public IReadOnlyList<string> Errors { get; set; }
        }

        private sealed class CombatResourceEvidence
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

        private sealed class CombatCommandEvidence
        {
            public string Action { get; set; }
            public string ActorId { get; set; }
            public string TargetId { get; set; }
            public string Result { get; set; }
            public int ChildAttackStartCount { get; set; }
            public int RepathCount { get; set; }
            public bool RiderStandardCharged { get; set; }
            public bool NativeAttackRuleObserved { get; set; }

            public static CombatCommandEvidence From(MountedPairAttackOutcome value)
            {
                return value == null ? null : new CombatCommandEvidence
                {
                    Action = value.Action.ToString(),
                    ActorId = value.ActorId,
                    TargetId = value.TargetId,
                    Result = value.Result,
                    ChildAttackStartCount = value.ChildAttackStartCount,
                    RepathCount = value.RepathCount,
                    RiderStandardCharged = value.RiderStandardCharged,
                    NativeAttackRuleObserved = value.NativeAttackRuleObserved
                };
            }
        }

        private sealed class CombatRuleEvidence
        {
            public int? ForcedD20 { get; set; }
            public int ForcedD20Count { get; set; }
            public int AttackRuleCount { get; set; }
            public int AttackRollCount { get; set; }
            public int DamageRuleCount { get; set; }
            public int UnexpectedPairAttackCount { get; set; }
            public int TotalDamage { get; set; }
            public string LastInitiatorId { get; set; }
            public string LastTargetId { get; set; }
            public string LastAttackResult { get; set; }

            public static CombatRuleEvidence From(MountedCombatRuleProbe value)
            {
                return value == null ? null : new CombatRuleEvidence
                {
                    ForcedD20 = value.ForcedD20,
                    ForcedD20Count = value.ForcedD20Count,
                    AttackRuleCount = value.AttackRuleCount,
                    AttackRollCount = value.AttackRollCount,
                    DamageRuleCount = value.DamageRuleCount,
                    UnexpectedPairAttackCount = value.UnexpectedPairAttackCount,
                    TotalDamage = value.TotalDamage,
                    LastInitiatorId = value.LastInitiatorId,
                    LastTargetId = value.LastTargetId,
                    LastAttackResult = value.LastAttackResult
                };
            }
        }

        private sealed class CombatMovementEvidence
        {
            public string AuthoritativeMover { get; set; }
            public int RepathCount { get; set; }
            public bool? RiderStockAgentEnabledAtEnd { get; set; }
            public bool? MountStockAgentEnabledAtEnd { get; set; }
            public bool? RiderAvoidanceDisabledAtEnd { get; set; }
            public bool? MountAvoidanceDisabledAtEnd { get; set; }
        }

        private sealed class CombatPoseEvidence
        {
            public string ProfileId { get; set; }
            public bool HealthyAtOutcome { get; set; }
            public bool ConfiguredAtEnd { get; set; }
            public bool AttachmentLeaseAtEnd { get; set; }
            public bool ResidueAtEnd { get; set; }
        }

        private sealed class CombatCleanupEvidence
        {
            public bool TargetRemoved { get; set; }
            public bool TargetEntityRemoved { get; set; }
            public bool RuntimeGroupRemoved { get; set; }
            public bool RuntimeFactionRemoved { get; set; }
            public bool RelationshipClean { get; set; }
            public bool CombatCleared { get; set; }
            public string RelationshipState { get; set; }
            public bool ResidualState { get; set; }
            public bool PresentationResidual { get; set; }
        }

        private sealed class PositionEvidence
        {
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }

            public static PositionEvidence From(Vector3 value)
            {
                return new PositionEvidence { X = value.x, Y = value.y, Z = value.z };
            }
        }
    }
}
