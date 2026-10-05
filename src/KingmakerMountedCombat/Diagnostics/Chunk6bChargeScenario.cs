using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.Utility;
using Kingmaker.View;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Chunk 6B increment 6B.2: the real-time delivery of the pair-owned Mounted Charge, driven through the
    // player-facing native surfaces only. The fixture clicks the rider's own ability through the game's
    // selected-ability handler, exactly as a player does, and then records what the engine did: the native
    // full-round shell on the rider, the mount's forced path, the single rider-owned attack and its native
    // charge rule stamp, the action economy, the lease restoration and the residue.
    //
    // The compiled side records facts and checks only its own structure. scripts/runtime/Chunk6bChargeEvidence.ps1
    // is the one acceptance authority for these rows.
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        internal const string Chunk6bChargeRtScenario = "chunk6b-charge-rt";
        internal const string Chunk6bChargeTbScenario = "chunk6b-charge-tb";
        internal static bool IsChunk6bChargeScenario(string scenario) =>
            string.Equals(scenario, Chunk6bChargeRtScenario, StringComparison.Ordinal) ||
            string.Equals(scenario, Chunk6bChargeTbScenario, StringComparison.Ordinal);
        private bool IsChunk6bCharge => IsChunk6bChargeScenario(request.Scenario);
        private bool Chunk6bChargeTb => string.Equals(request.Scenario, Chunk6bChargeTbScenario, StringComparison.Ordinal);
        // Real time carries the whole 6B.2 row set. Turn-based carries the delivery and refusal core only:
        // the two lifecycle rows intervene inside a live real-time path and are not part of 6B.3.
        private static readonly string[] Chunk6bChargeRealTimeCases =
        {
            "C6B-CHARGE-default-off",
            "C6B-CHARGE-positive",
            "C6B-CHARGE-below-minimum",
            "C6B-CHARGE-stock-rejected",
            "C6B-CHARGE-interrupted",
            "C6B-CHARGE-combat-ended",
            "C6B-CHARGE-obstructed-line",
            "C6B-CHARGE-blocked-clearance",
            "C6B-CHARGE-cancelled",
            "C6B-CHARGE-exception-cleanup",
            "C6B-CHARGE-target-moved",
            "C6B-CHARGE-target-lost",
            "C6B-CHARGE-rider-incapacitated",
            "C6B-CHARGE-mount-incapacitated"
        };
        private static readonly string[] Chunk6bChargeTurnBasedCases =
        {
            "C6B-CHARGE-default-off",
            "C6B-CHARGE-positive",
            "C6B-CHARGE-below-minimum",
            "C6B-CHARGE-stock-rejected"
        };
        private string[] Chunk6bChargeCases =>
            Chunk6bChargeTb ? Chunk6bChargeTurnBasedCases : Chunk6bChargeRealTimeCases;

        private int chunk6bChargeCase, chunk6bChargeStage;
        private bool chunk6bChargeControlSent;
        private bool chunk6bChargeOriginalSetting, chunk6bChargeSettingCaptured, chunk6bChargeSettingRestored;
        private AbilityData chunk6bChargeAbility;
        private UnitUseAbility chunk6bChargeShell;
        private JObject chunk6bChargeBefore, chunk6bChargeInput, chunk6bChargeLeaseEvidence;
        private readonly JArray chunk6bChargeSamples = new JArray();
        private bool chunk6bChargeHoverPure;
        private bool chunk6bChargeClicked;
        private Vector3 chunk6bChargeMountOrigin, chunk6bChargeRiderOrigin;
        private double chunk6bChargeStarted, chunk6bChargeLastSample;
        private float chunk6bChargeMountDistance, chunk6bChargeRiderDistance, chunk6bChargePeakSpeed;
        private float chunk6bChargeMaxRiderStandard, chunk6bChargeMaxRiderMove;
        private float chunk6bChargeMaxMountStandard, chunk6bChargeMaxMountMove;
        private bool chunk6bChargeObservedCharging, chunk6bChargeObservedForceMode, chunk6bChargeObservedBuff;
        private int chunk6bChargeAttackRulesBefore, chunk6bChargeOpportunityRulesBefore;
        private int chunk6bChargeAdmittedBefore, chunk6bChargeRefusedBefore;
        private bool chunk6bChargeAttemptAdmitted;
        private JObject chunk6bChargeIntervention;
        private bool chunk6bChargeInterventionDone;
        private TurnController chunk6bChargeLastSeenTurn;
        private float chunk6bChargeBaseRiderStandard, chunk6bChargeBaseRiderMove;
        private float chunk6bChargeBaseMountStandard, chunk6bChargeBaseMountMove;
        private int chunk6bChargeRepeatStage;
        private JObject chunk6bChargeRepeatBefore, chunk6bChargeRepeatInput;
        private double chunk6bChargeRepeatStarted;
        private Vector3 chunk6bChargeRepeatMountOrigin, chunk6bChargeRepeatRiderOrigin;
        private float chunk6bChargeRepeatMountDistance, chunk6bChargeRepeatRiderDistance;
        private float chunk6bChargeRepeatMaxRiderStandard, chunk6bChargeRepeatMaxRiderMove;
        private float chunk6bChargeRepeatMaxMountStandard, chunk6bChargeRepeatMaxMountMove;
        private int chunk6bChargeRepeatAttackRulesBefore, chunk6bChargeRepeatOpportunityRulesBefore;
        private int chunk6bChargeRepeatAdmittedBefore, chunk6bChargeRepeatRefusedBefore;
        private int chunk6bChargeShellCount;
        private string chunk6bChargeFeedback;
        private JArray chunk6bChargeRejectionCodes = new JArray();
        // The admission-fault row: the one diagnostics-only seam, armed for exactly one admission.
        private JObject chunk6bChargeFaultEvidence;
        private bool chunk6bChargeFaultArmed, chunk6bChargeFaultFired, chunk6bChargeFaultClearedAfterClick;
        private int chunk6bChargeCompensationBefore;
        // The clearance row: the landing point the policy refuses, and who was standing on it.
        private JArray chunk6bChargeLandingBlockers;
        // The lifecycle rows: the target body, the moved target and the two incapacity subjects.
        private JObject chunk6bChargeTargetLossEvidence, chunk6bChargeTargetMoveEvidence, chunk6bChargeLifeEvidence;
        private PairedConditionObserver chunk6bChargeLifeObserver;
        private UnitMoveTo chunk6bChargeTargetMove;
        private string chunk6bChargeLostTargetId;
        private Vector3 chunk6bChargeTargetMoveOrigin;
        private int chunk6bChargeLifeDamageDispatchCount;
        private int chunk6bChargeLifeDamageBefore;
        private bool chunk6bChargeLifeRestored;

        private const string Chunk6bChargeRepeatRow = "C6B-CHARGE-spent-standard";

        private string Chunk6bChargeCaseId => Chunk6bChargeCases[chunk6bChargeCase];
        private JObject Chunk6bChargeMeasurement => (JObject)observations["chunk6bCharge"];

        private float Chunk6bChargeCaseDistance =>
            string.Equals(Chunk6bChargeCaseId, "C6B-CHARGE-below-minimum", StringComparison.Ordinal) ? 3.5f : 9f;

        // The cases that must actually deliver a charge, addressed by identity rather than by index so the
        // real-time and turn-based case lists can differ. No turn-based case can deliver one while
        // increment 6B.3 is deferred: the policy refuses every turn-based charge, so a turn-based case
        // records that refusal and must not wait for a targetable geometry it can never be offered.
        private bool Chunk6bChargeCaseMustDeliver =>
            !Chunk6bChargeTb &&
            (string.Equals(Chunk6bChargeCaseId, "C6B-CHARGE-positive", StringComparison.Ordinal) ||
             Chunk6bChargeCaseIntervention != null || Chunk6bChargeCaseArmsAdmissionFault);

        // The one case that arms the diagnostics-only admission seam. It needs a lawful geometry,
        // because the compensation it measures only exists once the charge has genuinely entered the
        // rider command queue, but it performs no mid-charge intervention: the stimulus is the fault.
        private bool Chunk6bChargeCaseArmsAdmissionFault =>
            !Chunk6bChargeTb &&
            string.Equals(Chunk6bChargeCaseId, "C6B-CHARGE-exception-cleanup", StringComparison.Ordinal);

        // The native intervention this case performs once the charge is committed, or null for a case that
        // lets the charge run to its own end.
        private string Chunk6bChargeCaseIntervention
        {
            get
            {
                if (string.Equals(Chunk6bChargeCaseId, "C6B-CHARGE-interrupted", StringComparison.Ordinal)) return "native-command-interrupt";
                if (string.Equals(Chunk6bChargeCaseId, "C6B-CHARGE-combat-ended", StringComparison.Ordinal)) return "native-combat-end";
                if (string.Equals(Chunk6bChargeCaseId, "C6B-CHARGE-target-moved", StringComparison.Ordinal)) return "native-target-move";
                if (string.Equals(Chunk6bChargeCaseId, "C6B-CHARGE-target-lost", StringComparison.Ordinal)) return "native-target-removed";
                if (string.Equals(Chunk6bChargeCaseId, "C6B-CHARGE-rider-incapacitated", StringComparison.Ordinal)) return "native-rider-incapacity";
                if (string.Equals(Chunk6bChargeCaseId, "C6B-CHARGE-mount-incapacitated", StringComparison.Ordinal)) return "native-mount-incapacity";
                return null;
            }
        }

        // A lawful charge geometry read from the mount, which is the mover: the straight native route must
        // reach the point and the landing point must be clear. Preview.161 measured what happens without
        // this: an obstructed line made the policy refuse the target, correctly, and the row recorded
        // nothing. Every attempt is published so the chosen point is never a bare assertion.
        private Vector3 FindChunk6bChargeTargetPoint(float distance)
        {
            var attempts = new JArray();
            Chunk6bChargeMeasurement["placement-" + Chunk6bChargeCaseId] = new JObject
            {
                ["origin"] = CapturePosition(horse.Position),
                ["wantedDistance"] = distance,
                ["attempts"] = attempts
            };
            return FindWalkablePoint(horse.Position, distance, 0.5f, point =>
            {
                var endpoint = ObstacleAnalyzer.TraceAlongNavmesh(horse.Position, point);
                var blocked = Chunk6bLandingBlocked(point, 0.5f);
                var riderDistance = HorizontalDistance(rider.Position, point);
                var within = MountedCombatSpatialPolicy.IsWithinDiagnosticSpawnBounds(riderDistance);
                attempts.Add(new JObject
                {
                    ["point"] = CapturePosition(point),
                    ["nativeTrace"] = CapturePosition(endpoint),
                    ["landingBlockedEstimate"] = blocked,
                    ["riderDistance"] = riderDistance,
                    ["withinFixtureBounds"] = within
                });
                return within && endpoint == point && !blocked;
            });
        }

        // A charge line the native navmesh trace cannot follow, for the row that must be refused before
        // the landing check and before any cost. The sweep is written out here rather than reusing the
        // throwing FindWalkablePoint, because "this area offers no obstructed line" is a measurement that
        // must be recorded, not an exception to be caught - catching one would also mask a genuine failure
        // inside the trace or the landing probe.
        private Vector3? FindChunk6bChargeObstructedPoint(float distance)
        {
            var attempts = new JArray();
            Chunk6bChargeMeasurement["placement-" + Chunk6bChargeCaseId] = new JObject
            {
                ["origin"] = CapturePosition(horse.Position),
                ["wantedDistance"] = distance,
                ["wants"] = "obstructed-straight-line",
                ["attempts"] = attempts
            };
            if (global::AstarPath.active == null)
            {
                throw new InvalidOperationException("Active native navigation graph is unavailable.");
            }

            var baseDirection = horse.View == null ? Vector3.forward : horse.View.transform.forward;
            baseDirection.y = 0f;
            if (baseDirection.sqrMagnitude < 0.01f) { baseDirection = Vector3.forward; }
            baseDirection.Normalize();
            for (var index = 0; index < 16; index++)
            {
                var direction = Quaternion.Euler(0f, index * 22.5f, 0f) * baseDirection;
                var nearest = global::AstarPath.active.GetNearest(horse.Position + direction * distance);
                if (nearest.node == null || !nearest.node.Walkable) { continue; }
                var point = nearest.clampedPosition;
                var mountDistance = HorizontalDistance(horse.Position, point);
                if (mountDistance <= 0.25f || Math.Abs(mountDistance - distance) > 0.5f) { continue; }
                var endpoint = ObstacleAnalyzer.TraceAlongNavmesh(horse.Position, point);
                var blocked = Chunk6bLandingBlocked(point, 0.5f);
                var riderDistance = HorizontalDistance(rider.Position, point);
                var within = MountedCombatSpatialPolicy.IsWithinDiagnosticSpawnBounds(riderDistance);
                var straight = endpoint == point;
                attempts.Add(new JObject
                {
                    ["point"] = CapturePosition(point),
                    ["nativeTrace"] = CapturePosition(endpoint),
                    ["straightRoute"] = straight,
                    ["landingBlockedEstimate"] = blocked,
                    ["riderDistance"] = riderDistance,
                    ["withinFixtureBounds"] = within
                });
                // The refusal must come from the line, so the landing must be clear and the distance lawful.
                if (within && !straight && !blocked) { return point; }
            }

            return null;
        }

        // Who is standing on the landing point, read exactly the way the policy reads it. A bare
        // "landingBlocked: true" is not evidence of the clearance gate; this names the actors the gate
        // counts, with their distances and their own thresholds, so the refusal can be checked.
        private JArray DescribeChunk6bChargeLandingBlockers(Vector3 point, float targetCorpulence)
        {
            var blockers = new JArray();
            var weapon = rider.GetFirstWeapon();
            var separation = weapon == null
                ? 0f
                : horse.View.Corpulence + targetCorpulence + weapon.AttackRange.Meters;
            var landing = point.To2D() - (point - horse.Position).To2D().normalized * separation;
            foreach (var actor in Game.Instance.State.AwakeUnits)
            {
                if (actor == horse || actor == rider || actor == target || !actor.View) continue;
                if (actor.View.MovementAgent.AvoidanceDisabled) continue;
                var distance = (landing - actor.Position.To2D()).magnitude;
                var threshold = (horse.View.Corpulence + actor.View.Corpulence) * 0.8f;
                if (distance >= threshold) continue;
                blockers.Add(new JObject
                {
                    ["actorId"] = actor.UniqueId,
                    ["blueprint"] = actor.Blueprint == null ? null : actor.Blueprint.AssetGuid,
                    ["playerFaction"] = actor.IsPlayerFaction,
                    ["corpulence"] = actor.View.Corpulence,
                    ["distanceToLanding"] = distance,
                    ["threshold"] = threshold,
                    ["position"] = CapturePosition(actor.Position)
                });
            }

            return blockers;
        }

        // A lawful charge line whose landing point one weapon reach short of the target is occupied by
        // another awake native actor, for the row the policy must refuse through its clearance gate
        // rather than through its line or its distance. Written out like the obstructed-line sweep and
        // for the same reason: "this area offers no blocked landing at a lawful distance" is a
        // measurement to record, not an exception to catch.
        private Vector3? FindChunk6bChargeBlockedClearancePoint()
        {
            var attempts = new JArray();
            Chunk6bChargeMeasurement["placement-" + Chunk6bChargeCaseId] = new JObject
            {
                ["origin"] = CapturePosition(horse.Position),
                ["wants"] = "clear-straight-line-blocked-landing",
                ["minimumSweptDistance"] = 5f,
                ["maximumSweptDistance"] = 14f,
                ["attempts"] = attempts
            };
            if (global::AstarPath.active == null)
            {
                throw new InvalidOperationException("Active native navigation graph is unavailable.");
            }

            var baseDirection = horse.View == null ? Vector3.forward : horse.View.transform.forward;
            baseDirection.y = 0f;
            if (baseDirection.sqrMagnitude < 0.01f) { baseDirection = Vector3.forward; }
            baseDirection.Normalize();
            for (var ring = 0; ring <= 18; ring++)
            {
                var wanted = 5f + ring * 0.5f;
                for (var index = 0; index < 24; index++)
                {
                    var direction = Quaternion.Euler(0f, index * 15f, 0f) * baseDirection;
                    var nearest = global::AstarPath.active.GetNearest(horse.Position + direction * wanted);
                    if (nearest.node == null || !nearest.node.Walkable) { continue; }
                    var point = nearest.clampedPosition;
                    var mountDistance = HorizontalDistance(horse.Position, point);
                    if (Math.Abs(mountDistance - wanted) > 0.6f || mountDistance < 5f) { continue; }
                    var endpoint = ObstacleAnalyzer.TraceAlongNavmesh(horse.Position, point);
                    var straight = endpoint == point;
                    var blockers = DescribeChunk6bChargeLandingBlockers(point, 0.5f);
                    var riderDistance = HorizontalDistance(rider.Position, point);
                    var within = MountedCombatSpatialPolicy.IsWithinDiagnosticSpawnBounds(riderDistance);
                    if (attempts.Count < 200)
                    {
                        attempts.Add(new JObject
                        {
                            ["point"] = CapturePosition(point),
                            ["wantedDistance"] = wanted,
                            ["mountDistance"] = mountDistance,
                            ["nativeTrace"] = CapturePosition(endpoint),
                            ["straightRoute"] = straight,
                            ["landingBlockerCount"] = blockers.Count,
                            ["riderDistance"] = riderDistance,
                            ["withinFixtureBounds"] = within
                        });
                    }

                    // The refusal must come from the clearance gate, so the line must be clear and the
                    // distance lawful: only the landing point may be the problem.
                    if (within && straight && blockers.Count > 0) { return point; }
                }
            }

            return null;
        }

        // A row the fixture area cannot present, recorded in the same shape as the unreachable maximum
        // charge distance: the measurement is published and the case advances, and the row is NOT
        // claimed as delivered.
        private void AddChunk6bChargeLimitationRow(string limitation, JObject extra)
        {
            var evidence = new JObject
            {
                ["level"] = "NATIVE DELIVERY",
                ["mode"] = Chunk6bChargeTb ? "TB" : "RT",
                ["case"] = Chunk6bChargeCaseId,
                ["limitation"] = limitation,
                ["placement"] = Chunk6bChargeMeasurement["placement-" + Chunk6bChargeCaseId]
            };
            if (extra != null)
            {
                foreach (var property in extra.Properties())
                {
                    evidence[property.Name] = property.Value;
                }
            }

            AddRow(Chunk6bChargeCaseId, true, Chunk6bChargeRowClaim(), evidence);
        }

        // The exact geometry the policy reads, for a refusal message that explains itself.
        private string DescribeChunk6bChargeGeometry()
        {
            var endpoint = ObstacleAnalyzer.TraceAlongNavmesh(horse.Position, target.Position);
            return "distance=" + HorizontalDistance(horse.Position, target.Position).ToString("0.###",
                    System.Globalization.CultureInfo.InvariantCulture) +
                "; straightRoute=" + (endpoint == target.Position) +
                "; landingBlocked=" + Chunk6bLandingBlocked(target.Position, 0.5f) +
                "; available=" + (chunk6bChargeAbility == null ? "<none>" : chunk6bChargeAbility.IsAvailableForCast.ToString());
        }

        private void BeginChunk6bCharge()
        {
            if (!settings.EnablePairedActivation || settings.EnableUnifiedMountedTurn ||
                settings.EnablePairedCommandScheduler || settings.EnableDiagnosticOverlay || playerAction.OverlayPresent)
                throw new InvalidOperationException("Chunk 6B requires the accepted paired configuration.");
            CaptureIdleFixturePartyForCleanup();
            if (Chunk6bChargeTb) pairedAutomaticEndProbe = new NativeAutomaticEndProbe(false);
            chunk6bChargeOriginalSetting = settings.EnableMountedCharge;
            chunk6bChargeSettingCaptured = true;
            observations["chunk6bCharge"] = new JObject
            {
                ["contract"] = "chunk6b-pair-charge-delivery",
                ["mode"] = Chunk6bChargeTb ? "TB" : "RT",
                ["cases"] = new JArray(Chunk6bChargeCases),
                ["settingBefore"] = chunk6bChargeOriginalSetting,
                ["abilityGuid"] = NativeMountedControlService.MountedChargeAbilityGuid,
                ["stockChargeBlueprint"] = MountedChargeSafetyPolicy.ChargeBlueprintId,
                // The diagnostic spawn envelope is 3 to 20 m, while the stock maximum charge distance is the
                // mount combat speed times six. A native beyond-maximum refusal is therefore not reachable in
                // this fixture and is covered by the pure policy only; it is named here, never claimed.
                ["beyondMaximumReachable"] = false,
                ["spawnEnvelopeMinimum"] = MountedCombatSpatialPolicy.MinimumDiagnosticSpawnDistance,
                ["spawnEnvelopeMaximum"] = MountedCombatSpatialPolicy.MaximumDiagnosticSpawnDistance
            };
            step = Phase3dHorseStep.Phase3gControls;
            ResetLeafClock();
        }

        private void ResetChunk6bChargeCase()
        {
            chunk6bChargeControlSent = false;
            chunk6bChargeAbility = null;
            chunk6bChargeShell = null;
            chunk6bChargeBefore = null;
            chunk6bChargeInput = null;
            chunk6bChargeLeaseEvidence = null;
            chunk6bChargeAttemptAdmitted = false;
            chunk6bChargeIntervention = null;
            chunk6bChargeInterventionDone = false;
            chunk6bChargeSamples.Clear();
            chunk6bChargeHoverPure = false;
            chunk6bChargeClicked = false;
            chunk6bChargeMountDistance = chunk6bChargeRiderDistance = chunk6bChargePeakSpeed = 0f;
            chunk6bChargeMaxRiderStandard = chunk6bChargeMaxRiderMove = 0f;
            chunk6bChargeMaxMountStandard = chunk6bChargeMaxMountMove = 0f;
            chunk6bChargeObservedCharging = chunk6bChargeObservedForceMode = chunk6bChargeObservedBuff = false;
            chunk6bChargeShellCount = 0;
            chunk6bChargeFeedback = null;
            chunk6bChargeRejectionCodes = new JArray();
            chunk6bChargeLastSample = -1;
            chunk6bChargeBaseRiderStandard = chunk6bChargeBaseRiderMove = 0f;
            chunk6bChargeBaseMountStandard = chunk6bChargeBaseMountMove = 0f;
            chunk6bChargeRepeatStage = 0;
            chunk6bChargeRepeatBefore = null;
            chunk6bChargeRepeatInput = null;
            chunk6bChargeRepeatMountDistance = chunk6bChargeRepeatRiderDistance = 0f;
            chunk6bChargeRepeatMaxRiderStandard = chunk6bChargeRepeatMaxRiderMove = 0f;
            chunk6bChargeRepeatMaxMountStandard = chunk6bChargeRepeatMaxMountMove = 0f;
            chunk6bChargeFaultEvidence = null;
            chunk6bChargeFaultArmed = chunk6bChargeFaultFired = chunk6bChargeFaultClearedAfterClick = false;
            chunk6bChargeCompensationBefore = 0;
            chunk6bChargeLandingBlockers = null;
            chunk6bChargeTargetLossEvidence = null;
            chunk6bChargeTargetMoveEvidence = null;
            chunk6bChargeLifeEvidence = null;
            chunk6bChargeTargetMove = null;
            chunk6bChargeLostTargetId = null;
            chunk6bChargeLifeDamageDispatchCount = 0;
            chunk6bChargeLifeDamageBefore = 0;
            chunk6bChargeLifeRestored = false;
            // The seam is never left armed across cases: an arming that did not fire is cleared here.
            MountedChargeAdmissionFault.AfterQueue = null;
            if (chunk6bChargeLifeObserver != null) { chunk6bChargeLifeObserver.Dispose(); chunk6bChargeLifeObserver = null; }
        }

        // The rider's own KMC charge ability fact, by exact asset id. Absent while the feature is off.
        private AbilityData FindChunk6bChargeAbility()
        {
            var facts = rider.Descriptor?.Abilities?.Enumerable;
            if (facts == null) return null;
            var match = facts.FirstOrDefault(fact => fact.Blueprint != null &&
                string.Equals(fact.Blueprint.AssetGuid, NativeMountedControlService.MountedChargeAbilityGuid, StringComparison.Ordinal));
            return match?.Data;
        }

        private JObject CaptureChunk6bChargeAbilityIdentity()
        {
            var facts = rider.Descriptor?.Abilities?.Enumerable?.ToArray() ?? new Ability[0];
            var charge = facts.FirstOrDefault(fact => fact.Blueprint != null &&
                string.Equals(fact.Blueprint.AssetGuid, NativeMountedControlService.MountedChargeAbilityGuid, StringComparison.Ordinal));
            var stock = facts.FirstOrDefault(fact => fact.Blueprint != null &&
                string.Equals(fact.Blueprint.AssetGuid, MountedChargeSafetyPolicy.ChargeBlueprintId, StringComparison.Ordinal));
            return new JObject
            {
                ["setting"] = settings.EnableMountedCharge,
                ["kmcChargePresent"] = charge != null,
                ["kmcChargeBlueprint"] = charge?.Blueprint?.AssetGuid,
                ["kmcChargeActionType"] = charge?.Blueprint?.ActionType.ToString(),
                ["kmcChargeFullRound"] = charge?.Blueprint?.IsFullRoundAction,
                ["kmcChargeRequiresFullRound"] = charge?.Data?.RequireFullRoundAction,
                ["kmcChargeMinRange"] = charge?.Data?.MinRangeMeters,
                ["kmcChargeComponent"] = charge?.Blueprint?.GetComponent<MountedChargeAbilityLogic>() == null
                    ? null : typeof(MountedChargeAbilityLogic).FullName,
                ["kmcChargeIsStockLogic"] = charge?.Blueprint?.GetComponent<AbilityCustomCharge>() != null,
                ["stockChargePresent"] = stock != null,
                ["stockChargeIsStockLogic"] = stock?.Blueprint?.GetComponent<AbilityCustomCharge>() != null,
                ["abilityCount"] = facts.Length
            };
        }

        private JObject CaptureChunk6bChargeActors(string kind)
        {
            var agent = horse.View == null ? null : horse.View.AgentASP;
            var buff = rider.Descriptor?.Buffs?.Enumerable?.FirstOrDefault(item => item.Blueprint != null &&
                ReferenceEquals(item.Blueprint, Kingmaker.Blueprints.Root.BlueprintRoot.Instance.SystemMechanics.ChargeBuff));
            return new JObject
            {
                ["kind"] = kind,
                ["frame"] = Time.frameCount,
                ["nativeSeconds"] = Game.Instance.TimeController.GameTime.TotalSeconds,
                ["rider"] = CaptureOrdinaryActor(rider),
                ["mount"] = CaptureOrdinaryActor(horse),
                ["relationship"] = relationship.State.ToString(),
                ["distanceToTarget"] = target == null ? (float?)null : horse.DistanceTo(target),
                ["riderDistanceToTarget"] = target == null ? (float?)null : rider.DistanceTo(target),
                ["mountCharging"] = agent != null && agent.IsCharging,
                ["mountSpeedOverride"] = agent?.MaxSpeedOverride,
                ["mountMoving"] = agent != null && agent.IsReallyMoving,
                ["mountCombatSpeedMps"] = horse.CombatSpeedMps,
                ["riderStateCharging"] = rider.Descriptor != null && rider.Descriptor.State.IsCharging,
                ["mountStateCharging"] = horse.Descriptor != null && horse.Descriptor.State.IsCharging,
                ["chargeBuffPresent"] = buff != null,
                ["chargeBuffRounds"] = buff?.TimeLeft.TotalSeconds,
                ["pairCommandActive"] = combat.HasActiveCommand,
                ["pairMovement"] = combat.LastPairedMovementObservation,
                ["turn"] = Game.Instance?.TurnBasedCombatController?.CurrentTurn == null ? null : new JObject
                {
                    ["unit"] = Game.Instance.TurnBasedCombatController.CurrentTurn.Unit?.UniqueId,
                    ["isRider"] = Game.Instance.TurnBasedCombatController.CurrentTurn.Unit == rider,
                    ["status"] = Game.Instance.TurnBasedCombatController.CurrentTurn.Status.ToString(),
                    ["acting"] = Game.Instance.TurnBasedCombatController.CurrentTurn.IsActing,
                    ["timeMoved"] = Game.Instance.TurnBasedCombatController.CurrentTurn.TimeMoved,
                    ["timeMovedInForceMode"] = Game.Instance.TurnBasedCombatController.CurrentTurn.TimeMovedInForceMode
                }
            };
        }

        // A native target move large enough to change the charge geometry, issued through an ordinary
        // player-created UnitMoveTo on the target, which is the same stimulus the Chunk 4 moving-target
        // interrupt row uses. The charge must then re-read its own conditions and re-force the straight
        // line onto a newly admitted carrier, or terminate with its own exact reason.
        private void InterveneChunk6bChargeTargetMove()
        {
            var attempts = new JArray();
            var toTarget = target.Position - horse.Position;
            toTarget.y = 0f;
            var forward = toTarget.sqrMagnitude < 0.01f ? Vector3.forward : toTarget.normalized;
            var lateral = Vector3.Cross(Vector3.up, forward);
            chunk6bChargeTargetMoveOrigin = target.Position;
            Vector3? destination = null;
            foreach (var offset in new[]
            {
                lateral * 3f, lateral * -3f, lateral * 4.5f, lateral * -4.5f, forward * 3f
            })
            {
                if (global::AstarPath.active == null) { break; }
                var nearest = global::AstarPath.active.GetNearest(chunk6bChargeTargetMoveOrigin + offset);
                if (nearest.node == null || !nearest.node.Walkable) { continue; }
                var point = nearest.clampedPosition;
                var moved = HorizontalDistance(chunk6bChargeTargetMoveOrigin, point);
                var riderDistance = HorizontalDistance(rider.Position, point);
                var within = MountedCombatSpatialPolicy.IsWithinDiagnosticSpawnBounds(riderDistance);
                attempts.Add(new JObject
                {
                    ["point"] = CapturePosition(point),
                    ["movedFromOrigin"] = moved,
                    ["riderDistance"] = riderDistance,
                    ["withinFixtureBounds"] = within
                });
                if (moved >= 2f && within) { destination = point; break; }
            }

            chunk6bChargeTargetMoveEvidence = new JObject
            {
                ["contract"] = "native-target-move-forces-charge-revalidation-and-repath",
                ["targetId"] = target.UniqueId,
                ["targetOrigin"] = CapturePosition(chunk6bChargeTargetMoveOrigin),
                ["attempts"] = attempts,
                ["destination"] = destination.HasValue ? CapturePosition(destination.Value) : null,
                ["issued"] = false,
                ["targetCommandsEmptyBefore"] = target.Commands.Empty,
                ["mountDistanceAtStimulus"] = HorizontalDistance(horse.Position, chunk6bChargeTargetMoveOrigin),
                ["frame"] = Time.frameCount
            };
            if (!destination.HasValue) { return; }
            chunk6bChargeTargetMove = new UnitMoveTo(destination.Value, 0.3f) { CreatedByPlayer = true };
            target.Commands.Run(chunk6bChargeTargetMove);
            chunk6bChargeTargetMoveEvidence["issued"] = true;
        }

        // The target body is removed mid-approach through the diagnostic service own bounded destroy,
        // and the service itself is deliberately NOT disposed: preview.165 withdrew this row because a
        // mid-path destroy that disposed the service left the tranche holding a disposed object and the
        // run ended through its exception path without writing its artifact. The service stays alive for
        // the fixture own cleanup, which disposes it at the ordinary teardown point like every other case.
        private void InterveneChunk6bChargeTargetRemoval()
        {
            chunk6bChargeLostTargetId = target.UniqueId;
            var before = CaptureChunk6bChargeActors("target-removal-before");
            var destroyed = targetService.DestroyAndVerify();
            chunk6bChargeTargetLossEvidence = new JObject
            {
                ["contract"] = "native-target-body-removed-mid-approach-shared-service-retained",
                ["lostTargetId"] = chunk6bChargeLostTargetId,
                ["destroyConfirmed"] = destroyed,
                ["serviceRetained"] = targetService != null,
                ["serviceDisposedByRow"] = false,
                ["serviceState"] = targetService == null ? null : targetService.State.ToString(),
                ["targetEntityRemoved"] = targetService != null && targetService.TargetEntityRemoved,
                ["riderInCombat"] = rider.IsInCombat,
                ["mountInCombat"] = horse.IsInCombat,
                ["frame"] = Time.frameCount,
                ["before"] = before
            };
            // Nothing may dereference a removed body afterwards.
            target = null;
        }

        // One real native RuleDealDamage into the exact nonlethal unconscious window, which is the same
        // damage path Chunk6aPendingIncapacityScenario qualified, with its state capture and its window
        // guards shared rather than duplicated. The fixture records the damage it dealt and restores it
        // exactly afterwards, because later cases must start from a healthy pair.
        private void InterveneChunk6bChargeIncapacity(string kind)
        {
            var mountSubject = string.Equals(kind, "native-mount-incapacity", StringComparison.Ordinal);
            var subject = mountSubject ? horse : rider;
            var state = CaptureChunk6aPendingIncapacityState();
            var subjectBefore = (JObject)state[mountSubject ? "mount" : "rider"];
            chunk6bChargeLifeEvidence = new JObject
            {
                ["contract"] = "one-native-ruledeal-damage-to-incapacitation-window-mid-charge",
                ["subjectKind"] = mountSubject ? "mount" : "rider",
                ["subjectId"] = subject.UniqueId,
                ["window"] = "present",
                ["stateBefore"] = state.DeepClone(),
                ["frame"] = Time.frameCount
            };
            var difficulty = Game.Instance.Player.Difficulty.DamageToParty;
            var hitPoints = subject.Stats.HitPoints.ModifiedValue;
            var constitution = subject.Stats.Constitution.ModifiedValue;
            var temporaryHitPoints = subject.Stats.TemporaryHitPoints.ModifiedValue;
            var deathThreshold = hitPoints + constitution;
            var desiredDamage = hitPoints + 1;
            var needed = desiredDamage - subject.Damage + temporaryHitPoints;
            var window = new JObject
            {
                ["difficulty"] = difficulty,
                ["hitPoints"] = hitPoints,
                ["constitution"] = constitution,
                ["temporaryHitPoints"] = temporaryHitPoints,
                ["damageBefore"] = subject.Damage,
                ["desiredDamage"] = desiredDamage,
                ["deathThreshold"] = deathThreshold,
                ["needed"] = needed
            };
            chunk6bChargeLifeEvidence["measurement"] = window;
            // The same guards the qualified Chunk 6A row applies: a disposable conscious subject with a
            // real nonlethal window, and a native enemy source for the damage.
            if (!(bool)subjectBefore["conscious"] || (bool)subjectBefore["dead"] ||
                !(bool)subjectBefore["allowDyingCondition"] || (bool)subjectBefore["immortal"] ||
                (bool)subjectBefore["essential"] || (bool)subjectBefore["mainCharacter"] ||
                target == null || !target.IsInState || !target.IsPlayersEnemy ||
                difficulty <= 0f || float.IsNaN(difficulty) || float.IsInfinity(difficulty) || needed <= 0)
            {
                chunk6bChargeLifeEvidence["window"] = "absent";
                chunk6bChargeLifeEvidence["windowReason"] = "no disposable conscious subject with a nonlethal window and a native enemy source";
                return;
            }

            var requested = checked((int)Math.Ceiling((needed + 1d) / difficulty));
            var projected = subject.Damage + requested * difficulty - temporaryHitPoints;
            window["requestedDamage"] = requested;
            window["projectedDamage"] = projected;
            if (projected >= deathThreshold)
            {
                chunk6bChargeLifeEvidence["window"] = "absent";
                chunk6bChargeLifeEvidence["windowReason"] = "native difficulty leaves no safe incapacity window below death";
                return;
            }

            chunk6bChargeLifeObserver = new PairedConditionObserver(rider, horse, true);
            chunk6bChargeLifeDamageBefore = subject.Damage;
            chunk6bChargeLifeDamageDispatchCount++;
            var damage = Rulebook.Trigger(new RuleDealDamage(target, subject,
                new DamageBundle(new DirectDamage(new DiceFormula(0, DiceType.Zero), requested))));
            chunk6bChargeLifeEvidence["sourceId"] = target.UniqueId;
            chunk6bChargeLifeEvidence["damageDispatchCount"] = chunk6bChargeLifeDamageDispatchCount;
            chunk6bChargeLifeEvidence["nativeDamage"] = damage.Damage;
            chunk6bChargeLifeEvidence["nativeDamageBeforeDifficulty"] = damage.DamageBeforeDifficulty;
            chunk6bChargeLifeEvidence["difficultyUnchanged"] = Game.Instance.Player.Difficulty.DamageToParty == difficulty;
            chunk6bChargeLifeEvidence["stateAfterDamage"] = CaptureChunk6aPendingIncapacityState();
            chunk6bChargeLifeEvidence["damageAfter"] = subject.Damage;
        }

        // The fixture returns exactly the damage it dealt and then waits for the native life controller
        // to bring the subject back, rather than declaring it restored. Returns true once the subject is
        // observably conscious again at its original damage.
        private bool RestoreChunk6bChargeIncapacity()
        {
            var mountSubject = string.Equals(Chunk6bChargeCaseIntervention, "native-mount-incapacity", StringComparison.Ordinal);
            var subject = mountSubject ? horse : rider;
            if (chunk6bChargeLifeEvidence["restore"] == null)
            {
                chunk6bChargeLifeEvidence["lifeEventsAtSettle"] =
                    chunk6bChargeLifeObserver == null ? null : chunk6bChargeLifeObserver.Capture();
                chunk6bChargeLifeEvidence["restore"] = new JObject
                {
                    ["contract"] = "fixture-returns-exactly-the-damage-it-dealt",
                    ["damageAtRestoreStart"] = subject.Damage,
                    ["damageToRestore"] = chunk6bChargeLifeDamageBefore,
                    ["stateBeforeRestore"] = CaptureChunk6aPendingIncapacityState(),
                    ["frame"] = Time.frameCount,
                    ["restored"] = false
                };
                subject.Descriptor.Damage = chunk6bChargeLifeDamageBefore;
            }

            var liveState = subject.Descriptor.State;
            if (!liveState.IsConscious || liveState.IsDead || subject.Damage != chunk6bChargeLifeDamageBefore)
            {
                return false;
            }

            var restore = (JObject)chunk6bChargeLifeEvidence["restore"];
            restore["damageAfterRestore"] = subject.Damage;
            restore["conscious"] = liveState.IsConscious;
            restore["lifeState"] = liveState.LifeState.ToString();
            restore["stateAfterRestore"] = CaptureChunk6aPendingIncapacityState();
            restore["lifeEvents"] = chunk6bChargeLifeObserver == null ? null : chunk6bChargeLifeObserver.Capture();
            restore["restored"] = true;
            if (chunk6bChargeLifeObserver != null) { chunk6bChargeLifeObserver.Dispose(); chunk6bChargeLifeObserver = null; }
            chunk6bChargeLifeRestored = true;
            return true;
        }

        // What the charge transaction itself recorded: its revalidations, its step order, its carrier
        // release proof and its cleanup. Bound to this attempt, because the controller keeps the last
        // admitted charge command across cases and a row that admitted nothing must publish nothing.
        private JObject CaptureChunk6bChargeTransaction()
        {
            var command = combat.LastMountedChargeCommand;
            if (!chunk6bChargeAttemptAdmitted || command == null) return null;
            return new JObject
            {
                ["chargeMode"] = command.ChargeMode,
                ["revalidationCount"] = command.ChargeRevalidationCount,
                ["revalidationPhases"] = command.ChargeRevalidationPhases,
                ["revalidationFailed"] = command.ChargeRevalidationFailed,
                ["revalidationFailurePhase"] = command.ChargeRevalidationFailurePhase,
                ["revalidationFailureReason"] = command.ChargeRevalidationFailureReason,
                ["revalidationFailureCode"] = command.ChargeRevalidationFailureCode,
                ["sequence"] = command.ChargeSequence,
                ["sequenceLawful"] = command.ChargeSequenceLawful,
                ["carrierReleaseProven"] = command.CarrierReleaseProvenForAttack,
                ["leaseRestored"] = command.ChargeLeaseRestored,
                ["leaseDescription"] = command.ChargeLeaseDescription,
                ["cleanupComplete"] = command.ChargeCleanupComplete,
                ["cleanupDebt"] = command.ChargeCleanupDebt,
                ["cleanupDebtAtEnd"] = command.ChargeCleanupDebtAtEnd,
                ["leaseApplicationFailed"] = command.ChargeLeaseApplicationFailed,
                ["leaseApplicationFailure"] = command.ChargeLeaseApplicationFailure,
                ["leaseApplicationFailedStep"] = command.ChargeLeaseApplicationFailedStep,
                ["leaseRolledBackOnFailure"] = command.ChargeLeaseRolledBackOnFailure,
                ["repathObservations"] = combat.LastOutcome == null ? null : combat.LastOutcome.RepathObservations,
                ["finished"] = command.IsFinished,
                ["result"] = command.IsFinished ? command.Result.ToString() : null
            };
        }

        private void TickChunk6bCharge()
        {
            var game = Game.Instance;
            var controller = game.TurnBasedCombatController;
            var turn = controller.CurrentTurn;
            if (game.IsPaused) { game.IsPaused = false; return; }
            // A turn boundary is progress, so the harness leaf deadline measures a stall here rather than
            // the length of a lawful turn-based round.
            if (Chunk6bChargeTb && !ReferenceEquals(turn, chunk6bChargeLastSeenTurn))
            {
                chunk6bChargeLastSeenTurn = turn;
                ResetLeafClock();
            }
            Chunk6bChargeMeasurement["progress"] = new JObject
            {
                ["case"] = Chunk6bChargeCaseId, ["stage"] = chunk6bChargeStage, ["frame"] = Time.frameCount,
                ["relationship"] = relationship.State.ToString(), ["samples"] = chunk6bChargeSamples.Count
            };

            if (chunk6bChargeStage == 0)
            {
                if (!rider.Commands.Empty || !horse.Commands.Empty || rider.AreHandsBusyWithAnimation) return;
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                if (relationship.State != RelationshipState.Mounted)
                {
                    if (!chunk6bChargeControlSent)
                        chunk6bChargeControlSent = TryNativeAbilityTargetClick(nativeControls.MountAbility, horse,
                            "chunk6b-charge-mount-" + Chunk6bChargeCaseId);
                    return;
                }

                if (string.Equals(Chunk6bChargeCaseId, "C6B-CHARGE-default-off", StringComparison.Ordinal))
                {
                    TickChunk6bChargeDefaultOff();
                    return;
                }

                if (rider.IsInCombat || horse.IsInCombat || !PrepareUnmountedHorseAiIsolation() || !PrepareCombatMountRiderAiIsolation()) return;
                if (turnBasedModeProbe == null) turnBasedModeProbe = new NativeModeTransitionProbe(Chunk6bChargeTb);
                if (!turnBasedModeProbe.TemporaryValueIsCurrent) { turnBasedModeProbe.DispatchTemporaryValueIfRequired(); return; }
                if (string.Equals(Chunk6bChargeCaseId, "C6B-CHARGE-obstructed-line", StringComparison.Ordinal))
                {
                    var obstructed = FindChunk6bChargeObstructedPoint(Chunk6bChargeCaseDistance);
                    Chunk6bChargeMeasurement["obstructedLineReachable"] = obstructed.HasValue;
                    if (!obstructed.HasValue)
                    {
                        // Measured, not omitted: every direction this area offers at the lawful distance has a
                        // clear native line, so the row cannot be presented here. Recorded in the same shape as
                        // the unreachable maximum charge distance.
                        AddRow(Chunk6bChargeCaseId, true, Chunk6bChargeRowClaim(), new JObject
                        {
                            ["level"] = "NATIVE DELIVERY",
                            ["mode"] = Chunk6bChargeTb ? "TB" : "RT",
                            ["case"] = Chunk6bChargeCaseId,
                            ["limitation"] = "no-obstructed-line-in-fixture-area",
                            ["obstructedLineReachable"] = false,
                            ["placement"] = Chunk6bChargeMeasurement["placement-" + Chunk6bChargeCaseId]
                        });
                        chunk6bChargeCase++;
                        ResetChunk6bChargeCase();
                        chunk6bChargeStage = 0;
                        ResetLeafClock();
                        return;
                    }

                    BeginTarget(Chunk6bChargeCaseDistance, Chunk6bChargeCaseId, obstructed.Value);
                    ruleProbe.Arm(target, false);
                    chunk6bChargeStage = 1; ResetLeafClock(); return;
                }

                if (string.Equals(Chunk6bChargeCaseId, "C6B-CHARGE-blocked-clearance", StringComparison.Ordinal))
                {
                    var clearance = FindChunk6bChargeBlockedClearancePoint();
                    Chunk6bChargeMeasurement["blockedClearanceReachable"] = clearance.HasValue;
                    if (!clearance.HasValue)
                    {
                        AddChunk6bChargeLimitationRow("no-blocked-landing-in-fixture-area", new JObject
                        {
                            ["blockedClearanceReachable"] = false
                        });
                        chunk6bChargeCase++;
                        ResetChunk6bChargeCase();
                        chunk6bChargeStage = 0;
                        ResetLeafClock();
                        return;
                    }

                    BeginTarget(HorizontalDistance(horse.Position, clearance.Value), Chunk6bChargeCaseId, clearance.Value);
                    ruleProbe.Arm(target, false);
                    // Re-measured with the spawned body own corpulence, because the sweep used an
                    // estimate and a row may not claim a refusal the policy does not actually make.
                    chunk6bChargeLandingBlockers = DescribeChunk6bChargeLandingBlockers(
                        target.Position, target.View == null ? 0.5f : target.View.Corpulence);
                    chunk6bChargeStage = 1; ResetLeafClock(); return;
                }

                BeginTarget(Chunk6bChargeCaseDistance, Chunk6bChargeCaseId, FindChunk6bChargeTargetPoint(Chunk6bChargeCaseDistance));
                ruleProbe.Arm(target, false);
                chunk6bChargeStage = 1; ResetLeafClock(); return;
            }

            if (chunk6bChargeStage == 1)
            {
                // Every gate below is published before it is applied, so a stall in this stage names its
                // own cause in the retained progress record. IsCombatReady is called exactly once a frame
                // and its result is both recorded and used.
                var combatReady = IsCombatReady(true);
                var gates = new JObject
                {
                    ["combatReady"] = combatReady,
                    ["riderCommandsEmpty"] = rider.Commands.Empty,
                    ["mountCommandsEmpty"] = horse.Commands.Empty,
                    ["riderHandsBusy"] = rider.AreHandsBusyWithAnimation,
                    ["riderPrepared"] = rider.CombatState.Prepared,
                    ["pairHasActiveCommand"] = combat.HasActiveCommand,
                    ["riderCanActInCombat"] = rider.CombatState.CanActInCombat,
                    ["riderAbleToAct"] = rider.IsAbleToAct(),
                    ["riderStandardCooldown"] = rider.CombatState.Cooldown.StandardAction,
                    ["riderMoveCooldown"] = rider.CombatState.Cooldown.MoveAction,
                    ["mustDeliver"] = Chunk6bChargeCaseMustDeliver
                };
                if (Chunk6bChargeTb)
                {
                    gates["turnPresent"] = turn != null;
                    gates["turnIsRider"] = turn != null && ReferenceEquals(turn.Unit, rider);
                    gates["turnUnit"] = turn == null || turn.Unit == null ? null : turn.Unit.UniqueId;
                    gates["turnStatus"] = turn == null ? null : turn.Status.ToString();
                    gates["turnActing"] = turn != null && turn.IsActing;
                    gates["turnTimeMoved"] = turn == null ? 0f : turn.TimeMoved;
                    gates["controllerWaitingForUi"] = (bool)controller.WaitingForUI;
                    gates["controllerPendingNextUnit"] = GetPendingNextUnit(controller) != null;
                }
                ((JObject)Chunk6bChargeMeasurement["progress"])["gates"] = gates;
                if (!combatReady) return;
                if (!rider.Commands.Empty || !horse.Commands.Empty || rider.AreHandsBusyWithAnimation ||
                    !rider.CombatState.Prepared || combat.HasActiveCommand) return;
                // Preview.158 measured the rider still waiting initiative for a moment after real-time combat
                // starts: UnitCombatState.CanActInCombat is m_InCombat && !IsWaitingInitiative, so the charge was
                // correctly refused as "requires a rider who can act". Wait for the same native readiness the
                // stock charge fixture waits for before it clicks.
                // Turn-based: turn ownership is resolved before the rider-readiness gates below, because
                // those two deadlock otherwise. UnitCombatState.CanActInCombat is
                // m_InCombat && !IsWaitingInitiative, so while another actor holds the turn the rider is
                // waiting initiative and can never satisfy it, and the call that ends a foreign fixture
                // turn used to sit below it and was never reached. Preview.168 measured exactly that: the
                // stage stalled with riderCanActInCombat=false, riderPrepared=true, both queues empty, both
                // cooldowns zero and the turn held by one of the fixture's own allocated party actors in
                // Preparing. Preview.165 cleared this point only because the rider held the turn itself.
                if (Chunk6bChargeTb)
                {
                    if (turn == null) return;
                    if (!ReferenceEquals(turn.Unit, rider)) { TryEndPhase3gFixtureTurn(turn); return; }
                }
                if (!rider.CombatState.CanActInCombat || !rider.IsAbleToAct()) return;
                if (rider.CombatState.Cooldown.StandardAction > 0.001f || rider.CombatState.Cooldown.MoveAction > 0.001f) return;
                // Turn-based: the charge is a full-round action, so it belongs to the rider own turn and
                // that turn must not have moved yet. While another fixture actor holds the turn, its turn is
                // ended through the same helper the Chunk 6A turn scenarios use, which refuses a foreign
                // native turn and ignores a unit that is not directly controllable.
                if (Chunk6bChargeTb)
                {
                    // Ownership was resolved above; what remains is what the rider's own turn must satisfy.
                    // The turn-based charge is refused outright while increment 6B.3 is deferred, so the
                    // fixture asks for it from the ordinary start of the rider turn and records the refusal.
                    if (turn.Status != TurnController.TurnStatus.Preparing && !turn.IsActing) return;
                    if (turn.TimeMoved > 0.0001f) return;
                    if (controller.WaitingForUI || GetPendingNextUnit(controller) != null) return;
                }

                chunk6bChargeAbility = FindChunk6bChargeAbility();
                if (chunk6bChargeAbility == null)
                    throw new InvalidOperationException("The KMC Mounted Charge ability is not leased on the rider.");
                if (string.Equals(Chunk6bChargeCaseId, "C6B-CHARGE-blocked-clearance", StringComparison.Ordinal))
                {
                    // The spawned body may have moved the landing point out from under the blocker, in
                    // which case this is no longer the clearance row and must not be claimed as one.
                    chunk6bChargeLandingBlockers = DescribeChunk6bChargeLandingBlockers(
                        target.Position, target.View == null ? 0.5f : target.View.Corpulence);
                    var straightNow = ObstacleAnalyzer.TraceAlongNavmesh(horse.Position, target.Position) == target.Position;
                    if (chunk6bChargeLandingBlockers.Count == 0 || !straightNow)
                    {
                        AddChunk6bChargeLimitationRow("blocked-landing-not-reproducible-after-spawn", new JObject
                        {
                            ["landingBlockers"] = chunk6bChargeLandingBlockers,
                            ["straightRouteAfterSpawn"] = straightNow,
                            ["geometry"] = new JObject
                            {
                                ["mountDistanceToTarget"] = HorizontalDistance(horse.Position, target.Position),
                                ["landingBlocked"] = Chunk6bLandingBlocked(target.Position,
                                    target.View == null ? 0.5f : target.View.Corpulence)
                            }
                        });
                        chunk6bChargeStage = 4; ResetLeafClock(); return;
                    }
                }
                // A case that must deliver a charge waits for the mod own targeting to admit the target, so a
                // row never records a lawful click against a geometry the policy rightly refuses.
                if (Chunk6bChargeCaseMustDeliver)
                {
                    if (!chunk6bChargeAbility.CanTarget(new TargetWrapper(target)))
                    {
                        if (leafClock.Elapsed.TotalSeconds < 8.0) return;
                        throw new InvalidOperationException(
                            "The charge fixture could not present a targetable charge geometry: " + DescribeChunk6bChargeGeometry());
                    }
                }
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                var nativeTarget = new TargetWrapper(target);
                chunk6bChargeBefore = new JObject
                {
                    ["identity"] = CaptureChunk6bChargeAbilityIdentity(),
                    ["state"] = CaptureChunk6bChargeActors("before"),
                    ["available"] = chunk6bChargeAbility.IsAvailableForCast,
                    ["unavailableReason"] = chunk6bChargeAbility.GetUnavailableReason(),
                    ["kmcAvailabilityReason"] = nativeControls.Evaluate(NativeMountedControlKind.MountedCharge, rider).Reason,
                    ["canTarget"] = chunk6bChargeAbility.CanTarget(nativeTarget),
                    ["geometry"] = new JObject
                    {
                        ["straightRoute"] = ObstacleAnalyzer.TraceAlongNavmesh(horse.Position, target.Position) == target.Position,
                        ["landingBlocked"] = Chunk6bLandingBlocked(target.Position, target.View == null ? 0.5f : target.View.Corpulence),
                        ["mountDistanceToTarget"] = HorizontalDistance(horse.Position, target.Position)
                    },
                    ["minRangeMeters"] = chunk6bChargeAbility.MinRangeMeters,
                    ["approachDistance"] = chunk6bChargeAbility.GetApproachDistance(target),
                    ["requireFullRound"] = chunk6bChargeAbility.RequireFullRoundAction,
                    ["commandType"] = chunk6bChargeAbility.ActionType.ToString(),
                    ["pairCommandState"] = CapturePairCommandState()
                };
                chunk6bChargeAttackRulesBefore = ruleProbe.PairAttackRuleCount;
                chunk6bChargeOpportunityRulesBefore = ruleProbe.PairOpportunityAttackRuleCount;
                // The native full-round shell delivers on a later frame than the click, so the controller
                // counters are read again once the attempt has settled; only the delta this attempt caused
                // is published.
                chunk6bChargeAdmittedBefore = combat.MountedChargeAdmittedCount;
                chunk6bChargeRefusedBefore = combat.MountedChargeRefusedCount;
                chunk6bChargeBaseRiderStandard = rider.CombatState.Cooldown.StandardAction;
                chunk6bChargeBaseRiderMove = rider.CombatState.Cooldown.MoveAction;
                chunk6bChargeBaseMountStandard = horse.CombatState.Cooldown.StandardAction;
                chunk6bChargeBaseMountMove = horse.CombatState.Cooldown.MoveAction;

                if (string.Equals(Chunk6bChargeCaseId, "C6B-CHARGE-stock-rejected", StringComparison.Ordinal))
                {
                    TickChunk6bChargeStockRejected();
                    return;
                }

                // The player-facing path: the real selected-ability handler. Hovering must change nothing.
                var handler = game.SelectedAbilityHandler;
                handler.SetAbility(chunk6bChargeAbility);
                if (string.Equals(Chunk6bChargeCaseId, "C6B-CHARGE-cancelled", StringComparison.Ordinal))
                {
                    // Cancellation before commitment. The selection is taken through the same surface a
                    // player uses, hovered the same way, and then released with the native cancel
                    // SetAbility(null). No click is issued and no attack dispatch is expected, so the
                    // target service is left exactly as it was found.
                    var selectedAfterSet = ReferenceEquals(handler.Ability, chunk6bChargeAbility);
                    var beforeCancelHover = CaptureOrdinaryLiveState();
                    var beforeCancelActor = CaptureOrdinaryActor(rider);
                    for (var index = 0; index < 3; index++)
                    {
                        handler.GetPriority(target.View.gameObject, target.Position);
                        handler.GetTarget(target.View.gameObject, target.Position, chunk6bChargeAbility);
                    }

                    chunk6bChargeHoverPure = JToken.DeepEquals(beforeCancelHover, CaptureOrdinaryLiveState()) &&
                        JToken.DeepEquals(beforeCancelActor, CaptureOrdinaryActor(rider));
                    chunk6bChargeMountOrigin = horse.Position;
                    chunk6bChargeRiderOrigin = rider.Position;
                    handler.SetAbility(null);
                    chunk6bChargeClicked = false;
                    chunk6bChargeShell = rider.Commands.Raw.Concat(rider.Commands.Queue).OfType<UnitUseAbility>()
                        .FirstOrDefault(command => ReferenceEquals(command.Spell, chunk6bChargeAbility));
                    chunk6bChargeShellCount = rider.Commands.Raw.Concat(rider.Commands.Queue).OfType<UnitUseAbility>()
                        .Count(command => ReferenceEquals(command.Spell, chunk6bChargeAbility));
                    chunk6bChargeFeedback = combat.LastFeedback;
                    chunk6bChargeRejectionCodes = new JArray((combat.LastRejectionCodes ?? new MountedCombatRejectionCode[0])
                        .Select(code => code.ToString()).ToArray());
                    chunk6bChargeInput = new JObject
                    {
                        ["clicked"] = chunk6bChargeClicked,
                        ["hoverPure"] = chunk6bChargeHoverPure,
                        ["frame"] = Time.frameCount,
                        ["selectedAfterSet"] = selectedAfterSet,
                        ["selectedAfterCancel"] = handler.Ability != null,
                        ["shell"] = CaptureNativeAbilityShell(chunk6bChargeShell),
                        ["shellCount"] = chunk6bChargeShellCount,
                        ["feedback"] = chunk6bChargeFeedback,
                        ["rejectionCodes"] = chunk6bChargeRejectionCodes,
                        ["chargeAdmitted"] = combat.MountedChargeAdmittedCount,
                        ["chargeRefused"] = combat.MountedChargeRefusedCount,
                        ["lastRefusal"] = combat.LastMountedChargeRefusal,
                        ["after"] = CaptureChunk6bChargeActors("input-after")
                    };
                    chunk6bChargeStarted = game.TimeController.GameTime.TotalSeconds;
                    chunk6bChargeLastSample = -1;
                    chunk6bChargeStage = 2; ResetLeafClock(); return;
                }

                var beforeHover = CaptureOrdinaryLiveState();
                var beforeHoverActor = CaptureOrdinaryActor(rider);
                for (var index = 0; index < 3; index++)
                {
                    handler.GetPriority(target.View.gameObject, target.Position);
                    handler.GetTarget(target.View.gameObject, target.Position, chunk6bChargeAbility);
                }
                chunk6bChargeHoverPure = JToken.DeepEquals(beforeHover, CaptureOrdinaryLiveState()) &&
                    JToken.DeepEquals(beforeHoverActor, CaptureOrdinaryActor(rider));
                if (!targetService.BeginExpectedAttackDispatch(target))
                    throw new InvalidOperationException("The charge fixture lost its native target before input.");
                chunk6bChargeMountOrigin = horse.Position;
                chunk6bChargeRiderOrigin = rider.Position;
                if (Chunk6bChargeCaseArmsAdmissionFault)
                {
                    // The compensation path cannot be reached by waiting for a real engine fault, so the
                    // fixture arms the one diagnostics-only seam for exactly this admission. The hook can
                    // only throw: it grants nothing and relaxes nothing, and the admission path fires it
                    // immediately after AddToQueueFirst, which is the boundary this row exists to measure.
                    chunk6bChargeCompensationBefore = combat.ChargeCompensationCount;
                    chunk6bChargeFaultArmed = true;
                    MountedChargeAdmissionFault.AfterQueue = () =>
                    {
                        chunk6bChargeFaultFired = true;
                        throw new InvalidOperationException(
                            "Diagnostic charge admission fault immediately after AddToQueueFirst.");
                    };
                }

                chunk6bChargeClicked = handler.OnClick(target.View.gameObject, target.Position, 0, false, false);
                if (Chunk6bChargeCaseArmsAdmissionFault)
                {
                    // One shot: the seam clears itself before invoking, and the fixture proves that rather
                    // than assuming it, then clears it again so an unfired arming can never leak.
                    chunk6bChargeFaultClearedAfterClick = MountedChargeAdmissionFault.AfterQueue == null;
                    MountedChargeAdmissionFault.AfterQueue = null;
                    chunk6bChargeFaultEvidence = new JObject
                    {
                        ["contract"] = "post-queue-charge-admission-fault-compensated-exactly",
                        ["armed"] = chunk6bChargeFaultArmed,
                        ["fired"] = chunk6bChargeFaultFired,
                        ["seamClearedAfterClick"] = chunk6bChargeFaultClearedAfterClick,
                        ["frame"] = Time.frameCount,
                        ["compensationCount"] = combat.ChargeCompensationCount - chunk6bChargeCompensationBefore,
                        ["compensation"] = combat.LastChargeCompensation,
                        ["compensationComplete"] = combat.LastChargeCompensationComplete,
                        ["commandResident"] = combat.LastChargeCompensationCommandResident,
                        ["leaseRestored"] = combat.LastChargeCompensationLeaseRestored,
                        ["activeCommandCleared"] = combat.LastChargeCompensationActiveCommandCleared,
                        ["unmetPostconditions"] = combat.LastChargeCompensationUnmet,
                        ["faultedCleanupOwner"] = combat.HasFaultedChargeCleanupOwner,
                        ["riderCommandsEmpty"] = rider.Commands.Empty,
                        ["mountCommandsEmpty"] = horse.Commands.Empty,
                        ["afterClick"] = CaptureChunk6bChargeActors("admission-fault-after-click")
                    };
                }
                chunk6bChargeShell = rider.Commands.Raw.Concat(rider.Commands.Queue).OfType<UnitUseAbility>()
                    .FirstOrDefault(command => ReferenceEquals(command.Spell, chunk6bChargeAbility));
                chunk6bChargeShellCount = rider.Commands.Raw.Concat(rider.Commands.Queue).OfType<UnitUseAbility>()
                    .Count(command => ReferenceEquals(command.Spell, chunk6bChargeAbility));
                chunk6bChargeFeedback = combat.LastFeedback;
                chunk6bChargeRejectionCodes = new JArray((combat.LastRejectionCodes ?? new MountedCombatRejectionCode[0])
                    .Select(code => code.ToString()).ToArray());
                chunk6bChargeInput = new JObject
                {
                    ["clicked"] = chunk6bChargeClicked,
                    ["hoverPure"] = chunk6bChargeHoverPure,
                    ["frame"] = Time.frameCount,
                    ["shell"] = CaptureNativeAbilityShell(chunk6bChargeShell),
                    ["shellCount"] = chunk6bChargeShellCount,
                    ["feedback"] = chunk6bChargeFeedback,
                    ["rejectionCodes"] = chunk6bChargeRejectionCodes,
                    ["chargeAdmitted"] = combat.MountedChargeAdmittedCount,
                    ["chargeRefused"] = combat.MountedChargeRefusedCount,
                    ["lastRefusal"] = combat.LastMountedChargeRefusal,
                    ["after"] = CaptureChunk6bChargeActors("input-after")
                };
                chunk6bChargeStarted = game.TimeController.GameTime.TotalSeconds;
                chunk6bChargeLastSample = -1;
                chunk6bChargeStage = 2; ResetLeafClock(); return;
            }

            if (chunk6bChargeStage == 2)
            {
                var agent = horse.View == null ? null : horse.View.AgentASP;
                var elapsed = game.TimeController.GameTime.TotalSeconds - chunk6bChargeStarted;
                chunk6bChargeMountDistance = Math.Max(chunk6bChargeMountDistance, HorizontalDistance(horse.Position, chunk6bChargeMountOrigin));
                chunk6bChargeRiderDistance = Math.Max(chunk6bChargeRiderDistance, HorizontalDistance(rider.Position, chunk6bChargeRiderOrigin));
                chunk6bChargePeakSpeed = Math.Max(chunk6bChargePeakSpeed, agent == null ? 0f : agent.Speed);
                // The increase this attempt caused, never the absolute cooldown: a repeated request made while
                // an earlier charge's cost still stands must report zero, not the earlier charge's cost.
                chunk6bChargeMaxRiderStandard = Math.Max(chunk6bChargeMaxRiderStandard, rider.CombatState.Cooldown.StandardAction - chunk6bChargeBaseRiderStandard);
                chunk6bChargeMaxRiderMove = Math.Max(chunk6bChargeMaxRiderMove, rider.CombatState.Cooldown.MoveAction - chunk6bChargeBaseRiderMove);
                chunk6bChargeMaxMountStandard = Math.Max(chunk6bChargeMaxMountStandard, horse.CombatState.Cooldown.StandardAction - chunk6bChargeBaseMountStandard);
                chunk6bChargeMaxMountMove = Math.Max(chunk6bChargeMaxMountMove, horse.CombatState.Cooldown.MoveAction - chunk6bChargeBaseMountMove);
                chunk6bChargeObservedCharging |= agent != null && agent.IsCharging;
                chunk6bChargeObservedForceMode |= combat.LastMountedChargeCommand != null &&
                    combat.LastMountedChargeCommand.ChargeMode;
                chunk6bChargeObservedBuff |= rider.Descriptor != null && rider.Descriptor.State.IsCharging;
                if (elapsed - chunk6bChargeLastSample >= 0.2)
                {
                    chunk6bChargeLastSample = elapsed;
                    if (chunk6bChargeSamples.Count < 80)
                    {
                        chunk6bChargeSamples.Add(new JObject
                        {
                            ["nativeSeconds"] = elapsed,
                            ["frame"] = Time.frameCount,
                            ["state"] = CaptureChunk6bChargeActors("sample"),
                            ["shellRunning"] = chunk6bChargeShell != null && chunk6bChargeShell.IsRunning,
                            ["shellFinished"] = chunk6bChargeShell == null || chunk6bChargeShell.IsFinished,
                            ["pairAttackRules"] = ruleProbe.PairAttackRuleCount - chunk6bChargeAttackRulesBefore
                        });
                    }
                }

                // Cases 4 and 5 intervene through a native surface once the pair has genuinely committed:
                // the charge was admitted, the pair command is live and the mount has carried at least a
                // metre and a half of the forced path. Nothing is written to any actor resource.
                if (!chunk6bChargeInterventionDone && Chunk6bChargeCaseIntervention != null &&
                    chunk6bChargeMountDistance >= 1.5f && combat.HasActiveCommand &&
                    combat.LastMountedChargeCommand != null && !combat.LastMountedChargeCommand.IsFinished)
                {
                    chunk6bChargeInterventionDone = true;
                    var kind = Chunk6bChargeCaseIntervention;
                    chunk6bChargeIntervention = new JObject
                    {
                        ["kind"] = kind,
                        ["frame"] = Time.frameCount,
                        ["nativeSeconds"] = elapsed,
                        ["mountDistanceAtIntervention"] = chunk6bChargeMountDistance,
                        ["pairCommandActiveBefore"] = true,
                        ["riderInCombatBefore"] = rider.IsInCombat,
                        ["riderStandardBefore"] = rider.CombatState.Cooldown.StandardAction,
                        ["before"] = CaptureChunk6bChargeActors("intervention-before")
                    };
                    if (string.Equals(kind, "native-command-interrupt", StringComparison.Ordinal))
                    {
                        combat.LastMountedChargeCommand.Interrupt();
                    }
                    else if (string.Equals(kind, "native-combat-end", StringComparison.Ordinal))
                    {
                        TryLeaveCombat(target); TryLeaveCombat(rider); TryLeaveCombat(horse);
                    }
                    else if (string.Equals(kind, "native-target-move", StringComparison.Ordinal))
                    {
                        InterveneChunk6bChargeTargetMove();
                    }
                    else if (string.Equals(kind, "native-target-removed", StringComparison.Ordinal))
                    {
                        InterveneChunk6bChargeTargetRemoval();
                    }
                    else
                    {
                        InterveneChunk6bChargeIncapacity(kind);
                    }
                    chunk6bChargeIntervention["after"] = CaptureChunk6bChargeActors("intervention-after");
                    chunk6bChargeIntervention["riderInCombatAfter"] = rider.IsInCombat;
                    chunk6bChargeIntervention["riderStandardAfter"] = rider.CombatState.Cooldown.StandardAction;
                    return;
                }

                var settled = (chunk6bChargeShell == null || chunk6bChargeShell.IsFinished) &&
                    !combat.HasActiveCommand && rider.Commands.Empty && horse.Commands.Empty &&
                    (agent == null || !agent.IsReallyMoving) &&
                    ruleProbe.RiderResolvedCount >= ruleProbe.RiderNonOpportunityAttackRuleCount;
                if (!settled && elapsed < 14.0) return;
                // The controller keeps the last admitted charge command across cases, so the lease is
                // published only when this attempt is the one that admitted a charge. A row that admitted
                // nothing must record no lease, not the previous row's.
                chunk6bChargeAttemptAdmitted = combat.MountedChargeAdmittedCount - chunk6bChargeAdmittedBefore >= 1;
                chunk6bChargeLeaseEvidence = !chunk6bChargeAttemptAdmitted || combat.LastMountedChargeCommand == null
                    ? null
                    : combat.LastMountedChargeCommand.CaptureChargeLeaseEvidence();
                chunk6bChargeStage = 3; ResetLeafClock(); return;
            }

            if (chunk6bChargeStage == 3)
            {
                // The repeated request belongs to this combat, because the rider's standard action must still
                // be spent when it is attempted.
                if (string.Equals(Chunk6bChargeCaseId, "C6B-CHARGE-positive", StringComparison.Ordinal) && !Chunk6bChargeTb && chunk6bChargeRepeatStage < 2)
                {
                    TickChunk6bChargeRepeat();
                    return;
                }

                // What the moved target actually did, read once the charge has settled.
                if (chunk6bChargeTargetMoveEvidence != null && chunk6bChargeTargetMoveEvidence["moveFinished"] == null)
                {
                    chunk6bChargeTargetMoveEvidence["moveFinished"] =
                        chunk6bChargeTargetMove == null || chunk6bChargeTargetMove.IsFinished;
                    chunk6bChargeTargetMoveEvidence["moveResult"] =
                        chunk6bChargeTargetMove == null ? null : chunk6bChargeTargetMove.Result.ToString();
                    chunk6bChargeTargetMoveEvidence["targetMovedDistance"] = target == null
                        ? (float?)null
                        : HorizontalDistance(target.Position, chunk6bChargeTargetMoveOrigin);
                    chunk6bChargeTargetMoveEvidence["targetPositionAtSettle"] =
                        target == null ? null : CapturePosition(target.Position);
                    chunk6bChargeTargetMoveEvidence["mountDistanceAtSettle"] = target == null
                        ? (float?)null
                        : HorizontalDistance(horse.Position, target.Position);
                }

                // The pair must be whole and healthy again before the fixture leaves this case.
                if (chunk6bChargeLifeEvidence != null &&
                    string.Equals((string)chunk6bChargeLifeEvidence["window"], "present", StringComparison.Ordinal) &&
                    !chunk6bChargeLifeRestored && !RestoreChunk6bChargeIncapacity())
                {
                    return;
                }

                // A window the fixture could not present is recorded as the limitation it is, never as a
                // delivered incapacity row.
                if (chunk6bChargeLifeEvidence != null &&
                    string.Equals((string)chunk6bChargeLifeEvidence["window"], "absent", StringComparison.Ordinal))
                {
                    AddChunk6bChargeLimitationRow("native-incapacity-window-absent", new JObject
                    {
                        ["incapacity"] = chunk6bChargeLifeEvidence,
                        ["intervention"] = chunk6bChargeIntervention
                    });
                    chunk6bChargeStage = 4; ResetLeafClock(); return;
                }

                var evidence = new JObject
                {
                    ["level"] = "NATIVE DELIVERY",
                    ["mode"] = Chunk6bChargeTb ? "TB" : "RT",
                    ["case"] = Chunk6bChargeCaseId,
                    ["mounted"] = relationship.State == RelationshipState.Mounted &&
                        relationship.Rider == rider && relationship.Mount == horse,
                    ["before"] = chunk6bChargeBefore,
                    ["input"] = chunk6bChargeInput,
                    ["samples"] = chunk6bChargeSamples.DeepClone(),
                    ["after"] = CaptureChunk6bChargeActors("after"),
                    ["identityAfter"] = CaptureChunk6bChargeAbilityIdentity(),
                    ["movement"] = new JObject
                    {
                        ["mountDistance"] = chunk6bChargeMountDistance,
                        ["riderDistance"] = chunk6bChargeRiderDistance,
                        ["peakSpeedMps"] = chunk6bChargePeakSpeed,
                        ["mountCombatSpeedMps"] = horse.CombatSpeedMps,
                        ["chargingObserved"] = chunk6bChargeObservedCharging,
                        ["chargeModeObserved"] = chunk6bChargeObservedForceMode,
                        ["riderChargeStateObserved"] = chunk6bChargeObservedBuff
                    },
                    ["economy"] = new JObject
                    {
                        ["riderStandardMax"] = chunk6bChargeMaxRiderStandard,
                        ["riderMoveMax"] = chunk6bChargeMaxRiderMove,
                        ["mountStandardMax"] = chunk6bChargeMaxMountStandard,
                        ["mountMoveMax"] = chunk6bChargeMaxMountMove,
                        ["riderStandardNow"] = rider.CombatState.Cooldown.StandardAction,
                        ["riderMoveNow"] = rider.CombatState.Cooldown.MoveAction,
                        ["mountStandardNow"] = horse.CombatState.Cooldown.StandardAction,
                        ["mountMoveNow"] = horse.CombatState.Cooldown.MoveAction
                    },
                    ["lease"] = chunk6bChargeLeaseEvidence,
                    ["transaction"] = CaptureChunk6bChargeTransaction(),
                    ["admissionFault"] = chunk6bChargeFaultEvidence,
                    ["landingBlockers"] = chunk6bChargeLandingBlockers,
                    ["targetLoss"] = chunk6bChargeTargetLossEvidence,
                    ["targetMove"] = chunk6bChargeTargetMoveEvidence,
                    ["incapacity"] = chunk6bChargeLifeEvidence,
                    ["intervention"] = chunk6bChargeIntervention,
                    // What the controller did with this attempt, read after it settled rather than at the
                    // click: the shell spends the rider action and asks for delivery on a later frame.
                    ["delivery"] = new JObject
                    {
                        ["chargeAdmitted"] = combat.MountedChargeAdmittedCount - chunk6bChargeAdmittedBefore,
                        ["chargeRefused"] = combat.MountedChargeRefusedCount - chunk6bChargeRefusedBefore,
                        ["lastRefusal"] = combat.LastMountedChargeRefusal,
                        ["feedback"] = combat.LastFeedback,
                        ["rejectionCodes"] = new JArray((combat.LastRejectionCodes ?? new MountedCombatRejectionCode[0])
                            .Select(code => code.ToString()).ToArray())
                    },
                    ["rules"] = ruleProbe.CapturePairEvidence(),
                    ["attackRules"] = ruleProbe.PairAttackRuleCount - chunk6bChargeAttackRulesBefore,
                    ["attackRulesOpportunity"] = ruleProbe.PairOpportunityAttackRuleCount - chunk6bChargeOpportunityRulesBefore,
                    ["pairCommandState"] = CapturePairCommandState(),
                    // Bound to this attempt for the same reason as the lease: LastOutcome outlives a case.
                    ["terminal"] = !chunk6bChargeAttemptAdmitted || combat.LastOutcome == null ? null : new JObject
                    {
                        ["action"] = combat.LastOutcome.Action.ToString(),
                        ["actorId"] = combat.LastOutcome.ActorId,
                        ["resourceOwnerId"] = combat.LastOutcome.ResourceOwnerId,
                        ["targetId"] = combat.LastOutcome.TargetId,
                        ["result"] = combat.LastOutcome.Result,
                        ["childAttackStartCount"] = combat.LastOutcome.ChildAttackStartCount,
                        ["singleAttackMode"] = combat.LastOutcome.SingleAttackMode,
                        ["nativeFullAttack"] = combat.LastOutcome.NativeFullAttack,
                        ["nativePlannedAttackCount"] = combat.LastOutcome.NativePlannedAttackCount,
                        ["nativeCompletedAttackCount"] = combat.LastOutcome.NativeCompletedAttackCount,
                        ["repathCount"] = combat.LastOutcome.RepathCount
                    }
                };

                // Structure only: the fixture asks whether it observed what it set out to observe. Whether the
                // behaviour is lawful is decided by the external reader.
                var structural = chunk6bChargeBefore != null && chunk6bChargeInput != null &&
                    (bool)((JObject)chunk6bChargeBefore["identity"])["kmcChargePresent"];
                AddRow(Chunk6bChargeCaseId, structural, Chunk6bChargeRowClaim(), evidence);
                chunk6bChargeStage = 4; ResetLeafClock(); return;
            }

            if (chunk6bChargeStage == 4)
            {
                if (combat.HasActiveCommand || !rider.Commands.Empty || !horse.Commands.Empty) return;
                if (horse.View != null && horse.View.AgentASP.IsReallyMoving) return;
                // TryLeaveCombat ignores a null or removed unit, which is what the target-lost row leaves.
                TryLeaveCombat(target); TryLeaveCombat(rider); TryLeaveCombat(horse);
                if (targetService != null)
                {
                    if (!targetService.DestroyAndVerify()) return;
                    targetService.Dispose(); targetService = null; target = null;
                }
                if (CombatController.IsInTurnBasedCombat() || controller.Initialized || game.Player.IsInCombat ||
                    !rider.Commands.Empty || !horse.Commands.Empty) return;
                if (!RestoreCombatMountRiderAiIsolation() || !RestoreUnmountedHorseAiIsolation())
                    throw new InvalidOperationException("Charge fixture AI restoration failed.");
                combatMountRiderAiLease = null; unmountedHorseAiLease = null; unmountedHorseAiSettleRequested = false;
                chunk6bChargeCase++;
                ResetChunk6bChargeCase();
                chunk6bChargeStage = 0;
                ResetLeafClock();
                if (chunk6bChargeCase >= Chunk6bChargeCases.Length)
                {
                    RestoreChunk6bChargeSetting();
                    if (turnBasedModeProbe != null) { turnBasedModeProbe.Dispose(); turnBasedModeProbe = null; }
                    BeginCleanup();
                }
            }
        }

        private string Chunk6bChargeRowClaim()
        {
            switch (Chunk6bChargeCaseId)
            {
                case "C6B-CHARGE-default-off": return "The Mounted Charge control is absent while its setting is off and leased on the rider once it is on.";
                case "C6B-CHARGE-positive": return "One player click delivered one rider-owned full-round charge: the mount carried the forced path and the rider struck once with the native charge rule.";
                case "C6B-CHARGE-below-minimum": return "A target inside the stock minimum charge distance was refused before any cost, path or attack.";
                case "C6B-CHARGE-stock-rejected": return "The stock native Charge remained rejected while mounted.";
                case "C6B-CHARGE-interrupted": return "A charge interrupted after commitment stopped at once, restored every leased value, delivered no attack and kept the cost the engine had taken.";
                case "C6B-CHARGE-obstructed-line": return "A charge whose straight line the native navmesh cannot follow was refused before any cost, path or attack.";
                case "C6B-CHARGE-cancelled": return "A charge selected over a lawful geometry and cancelled before commitment took no cost, no path and no attack.";
                case "C6B-CHARGE-blocked-clearance": return "A charge whose landing point one weapon reach short of the target was occupied by another native actor was refused before any cost, path or attack.";
                case "C6B-CHARGE-exception-cleanup": return "A charge that failed immediately after entering the rider command queue resolved every native owner it had acquired, restored its lease exactly and left no residue.";
                case "C6B-CHARGE-target-moved": return "A charge whose target moved mid-approach re-read its own conditions, re-forced the straight line onto a newly admitted carrier, and ended lawfully.";
                case "C6B-CHARGE-target-lost": return "A charge whose target body was removed mid-approach terminated at once without an attack, restored every leased value, and left the shared diagnostic target service alive for the fixture own cleanup.";
                case "C6B-CHARGE-rider-incapacitated": return "A charge whose rider was placed in the native nonlethal unconscious window mid-approach terminated at once without an attack and restored every leased value.";
                case "C6B-CHARGE-mount-incapacitated": return "A charge whose mount was placed in the native nonlethal unconscious window mid-approach terminated at once without an attack and restored every leased value.";
                default: return "A charge whose combat ended mid-path terminated bounded, restored every leased value and delivered no attack.";
            }
        }

        // Case 0: default-off, then leased once the setting is on. No combat and no target are needed, and the
        // only thing the fixture writes is the mod setting it captured and restores.
        private void TickChunk6bChargeDefaultOff()
        {
            var before = CaptureChunk6bChargeAbilityIdentity();
            if (settings.EnableMountedCharge)
            {
                throw new InvalidOperationException("The Mounted Charge setting was already on at fixture entry.");
            }

            settings.EnableMountedCharge = true;
            nativeControls.Update();
            var after = CaptureChunk6bChargeAbilityIdentity();
            var evidence = new JObject
            {
                ["level"] = "NATIVE DELIVERY",
                ["mode"] = Chunk6bChargeTb ? "TB" : "RT",
                ["case"] = Chunk6bChargeCaseId,
                ["mounted"] = relationship.State == RelationshipState.Mounted,
                ["settingOff"] = before,
                ["settingOn"] = after,
                ["abilityGuid"] = NativeMountedControlService.MountedChargeAbilityGuid
            };
            AddRow(Chunk6bChargeCaseId,
                !(bool)before["kmcChargePresent"] && (bool)after["kmcChargePresent"],
                Chunk6bChargeRowClaim(), evidence);
            chunk6bChargeCase++;
            ResetChunk6bChargeCase();
            chunk6bChargeStage = 0;
            ResetLeafClock();
        }

        // Case 4: the stock native Charge is still rejected while mounted. The rider's stock Charge fact is
        // clicked through the same player path and must be refused with the Chunk 4 reason, with no movement,
        // no cost and no attack rule.
        private void TickChunk6bChargeStockRejected()
        {
            var stock = rider.Descriptor?.Abilities?.Enumerable?.FirstOrDefault(fact => fact.Blueprint != null &&
                fact.Blueprint.GetComponent<AbilityCustomCharge>()?.GetType() == typeof(AbilityCustomCharge));
            if (stock == null)
            {
                throw new InvalidOperationException("The rider has no stock native Charge ability for the rejection case.");
            }

            var stockAbility = stock.Data;
            var nativeTarget = new TargetWrapper(target);
            var handler = Game.Instance.SelectedAbilityHandler;
            handler.SetAbility(stockAbility);
            chunk6bChargeMountOrigin = horse.Position;
            chunk6bChargeRiderOrigin = rider.Position;
            var beforeClick = CaptureChunk6bChargeActors("stock-before");
            chunk6bChargeClicked = handler.OnClick(target.View.gameObject, target.Position, 0, false, false);
            chunk6bChargeShell = rider.Commands.Raw.Concat(rider.Commands.Queue).OfType<UnitUseAbility>()
                .FirstOrDefault(command => ReferenceEquals(command.Spell, stockAbility));
            chunk6bChargeInput = new JObject
            {
                ["clicked"] = chunk6bChargeClicked,
                ["hoverPure"] = true,
                ["frame"] = Time.frameCount,
                ["stockCanTarget"] = stockAbility.CanTarget(nativeTarget),
                ["stockAvailable"] = stockAbility.IsAvailableForCast,
                ["stockBlueprint"] = stockAbility.Blueprint.AssetGuid,
                ["shell"] = CaptureNativeAbilityShell(chunk6bChargeShell),
                ["shellCount"] = chunk6bChargeShell == null ? 0 : 1,
                ["feedback"] = combat.LastFeedback,
                ["safetyFeedback"] = MountedChargeSafetyPolicy.Feedback,
                ["rejectionCodes"] = new JArray((combat.LastRejectionCodes ?? new MountedCombatRejectionCode[0])
                    .Select(code => code.ToString()).ToArray()),
                ["before"] = beforeClick,
                ["after"] = CaptureChunk6bChargeActors("stock-after")
            };
            chunk6bChargeStarted = Game.Instance.TimeController.GameTime.TotalSeconds;
            chunk6bChargeLastSample = -1;
            chunk6bChargeStage = 2;
            ResetLeafClock();
        }

        // The repeated request: a second charge while the rider's standard action is still spent by the first.
        // It must be refused before any cost, path or attack, and it is measured in this combat so the spent
        // action is real rather than reconstructed.
        private void TickChunk6bChargeRepeat()
        {
            var game = Game.Instance;
            if (chunk6bChargeRepeatStage == 0)
            {
                if (!rider.Commands.Empty || !horse.Commands.Empty || combat.HasActiveCommand ||
                    rider.AreHandsBusyWithAnimation || (horse.View != null && horse.View.AgentASP.IsReallyMoving)) return;
                chunk6bChargeRepeatAttackRulesBefore = ruleProbe.PairAttackRuleCount;
                chunk6bChargeRepeatOpportunityRulesBefore = ruleProbe.PairOpportunityAttackRuleCount;
                chunk6bChargeRepeatAdmittedBefore = combat.MountedChargeAdmittedCount;
                chunk6bChargeRepeatRefusedBefore = combat.MountedChargeRefusedCount;
                chunk6bChargeBaseRiderStandard = rider.CombatState.Cooldown.StandardAction;
                chunk6bChargeBaseRiderMove = rider.CombatState.Cooldown.MoveAction;
                chunk6bChargeBaseMountStandard = horse.CombatState.Cooldown.StandardAction;
                chunk6bChargeBaseMountMove = horse.CombatState.Cooldown.MoveAction;
                var nativeTarget = new TargetWrapper(target);
                chunk6bChargeRepeatBefore = new JObject
                {
                    ["identity"] = CaptureChunk6bChargeAbilityIdentity(),
                    ["state"] = CaptureChunk6bChargeActors("repeat-before"),
                    ["available"] = chunk6bChargeAbility.IsAvailableForCast,
                    ["unavailableReason"] = chunk6bChargeAbility.GetUnavailableReason(),
                    ["kmcAvailabilityReason"] = nativeControls.Evaluate(NativeMountedControlKind.MountedCharge, rider).Reason,
                    ["canTarget"] = chunk6bChargeAbility.CanTarget(nativeTarget),
                    ["geometry"] = new JObject
                    {
                        ["straightRoute"] = ObstacleAnalyzer.TraceAlongNavmesh(horse.Position, target.Position) == target.Position,
                        ["landingBlocked"] = Chunk6bLandingBlocked(target.Position, target.View == null ? 0.5f : target.View.Corpulence),
                        ["mountDistanceToTarget"] = HorizontalDistance(horse.Position, target.Position)
                    },
                    ["minRangeMeters"] = chunk6bChargeAbility.MinRangeMeters,
                    ["approachDistance"] = chunk6bChargeAbility.GetApproachDistance(target),
                    ["requireFullRound"] = chunk6bChargeAbility.RequireFullRoundAction,
                    ["commandType"] = chunk6bChargeAbility.ActionType.ToString(),
                    ["pairCommandState"] = CapturePairCommandState()
                };
                var admittedBefore = combat.MountedChargeAdmittedCount;
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                var handler = game.SelectedAbilityHandler;
                handler.SetAbility(chunk6bChargeAbility);
                chunk6bChargeRepeatMountOrigin = horse.Position;
                chunk6bChargeRepeatRiderOrigin = rider.Position;
                var clicked = handler.OnClick(target.View.gameObject, target.Position, 0, false, false);
                var shell = rider.Commands.Raw.Concat(rider.Commands.Queue).OfType<UnitUseAbility>()
                    .FirstOrDefault(command => ReferenceEquals(command.Spell, chunk6bChargeAbility));
                chunk6bChargeRepeatInput = new JObject
                {
                    ["clicked"] = clicked,
                    ["hoverPure"] = true,
                    ["frame"] = Time.frameCount,
                    ["shell"] = CaptureNativeAbilityShell(shell),
                    ["shellCount"] = rider.Commands.Raw.Concat(rider.Commands.Queue).OfType<UnitUseAbility>()
                        .Count(command => ReferenceEquals(command.Spell, chunk6bChargeAbility)),
                    ["feedback"] = combat.LastFeedback,
                    ["rejectionCodes"] = new JArray((combat.LastRejectionCodes ?? new MountedCombatRejectionCode[0])
                        .Select(code => code.ToString()).ToArray()),
                    ["chargeAdmitted"] = combat.MountedChargeAdmittedCount - admittedBefore,
                    ["chargeRefused"] = combat.MountedChargeRefusedCount,
                    ["lastRefusal"] = combat.LastMountedChargeRefusal,
                    ["after"] = CaptureChunk6bChargeActors("repeat-input-after")
                };
                chunk6bChargeRepeatStarted = game.TimeController.GameTime.TotalSeconds;
                chunk6bChargeRepeatStage = 1;
                return;
            }

            chunk6bChargeRepeatMountDistance = Math.Max(chunk6bChargeRepeatMountDistance, HorizontalDistance(horse.Position, chunk6bChargeRepeatMountOrigin));
            chunk6bChargeRepeatRiderDistance = Math.Max(chunk6bChargeRepeatRiderDistance, HorizontalDistance(rider.Position, chunk6bChargeRepeatRiderOrigin));
            chunk6bChargeRepeatMaxRiderStandard = Math.Max(chunk6bChargeRepeatMaxRiderStandard, rider.CombatState.Cooldown.StandardAction - chunk6bChargeBaseRiderStandard);
            chunk6bChargeRepeatMaxRiderMove = Math.Max(chunk6bChargeRepeatMaxRiderMove, rider.CombatState.Cooldown.MoveAction - chunk6bChargeBaseRiderMove);
            chunk6bChargeRepeatMaxMountStandard = Math.Max(chunk6bChargeRepeatMaxMountStandard, horse.CombatState.Cooldown.StandardAction - chunk6bChargeBaseMountStandard);
            chunk6bChargeRepeatMaxMountMove = Math.Max(chunk6bChargeRepeatMaxMountMove, horse.CombatState.Cooldown.MoveAction - chunk6bChargeBaseMountMove);
            if (game.TimeController.GameTime.TotalSeconds - chunk6bChargeRepeatStarted < 0.6) return;

            var evidence = new JObject
            {
                ["level"] = "NATIVE DELIVERY",
                ["mode"] = Chunk6bChargeTb ? "TB" : "RT",
                ["case"] = Chunk6bChargeRepeatRow,
                ["mounted"] = relationship.State == RelationshipState.Mounted &&
                    relationship.Rider == rider && relationship.Mount == horse,
                ["before"] = chunk6bChargeRepeatBefore,
                ["input"] = chunk6bChargeRepeatInput,
                ["samples"] = new JArray(),
                ["after"] = CaptureChunk6bChargeActors("repeat-after"),
                ["identityAfter"] = CaptureChunk6bChargeAbilityIdentity(),
                ["movement"] = new JObject
                {
                    ["mountDistance"] = chunk6bChargeRepeatMountDistance,
                    ["riderDistance"] = chunk6bChargeRepeatRiderDistance,
                    ["peakSpeedMps"] = 0f,
                    ["mountCombatSpeedMps"] = horse.CombatSpeedMps,
                    ["chargingObserved"] = false,
                    ["chargeModeObserved"] = false,
                    ["riderChargeStateObserved"] = false
                },
                ["economy"] = new JObject
                {
                    ["riderStandardMax"] = chunk6bChargeRepeatMaxRiderStandard,
                    ["riderMoveMax"] = chunk6bChargeRepeatMaxRiderMove,
                    ["mountStandardMax"] = chunk6bChargeRepeatMaxMountStandard,
                    ["mountMoveMax"] = chunk6bChargeRepeatMaxMountMove,
                    ["riderStandardNow"] = rider.CombatState.Cooldown.StandardAction,
                    ["riderMoveNow"] = rider.CombatState.Cooldown.MoveAction,
                    ["mountStandardNow"] = horse.CombatState.Cooldown.StandardAction,
                    ["mountMoveNow"] = horse.CombatState.Cooldown.MoveAction
                },
                ["lease"] = null,
                ["transaction"] = null,
                ["admissionFault"] = null,
                ["landingBlockers"] = null,
                ["targetLoss"] = null,
                ["targetMove"] = null,
                ["incapacity"] = null,
                ["intervention"] = null,
                ["delivery"] = new JObject
                {
                    ["chargeAdmitted"] = combat.MountedChargeAdmittedCount - chunk6bChargeRepeatAdmittedBefore,
                    ["chargeRefused"] = combat.MountedChargeRefusedCount - chunk6bChargeRepeatRefusedBefore,
                    ["lastRefusal"] = combat.LastMountedChargeRefusal,
                    ["feedback"] = combat.LastFeedback,
                    ["rejectionCodes"] = new JArray((combat.LastRejectionCodes ?? new MountedCombatRejectionCode[0])
                        .Select(code => code.ToString()).ToArray())
                },
                ["rules"] = ruleProbe.CapturePairEvidence(),
                ["attackRules"] = ruleProbe.PairAttackRuleCount - chunk6bChargeRepeatAttackRulesBefore,
                ["attackRulesOpportunity"] = ruleProbe.PairOpportunityAttackRuleCount - chunk6bChargeRepeatOpportunityRulesBefore,
                ["pairCommandState"] = CapturePairCommandState(),
                ["terminal"] = null
            };
            AddRow(Chunk6bChargeRepeatRow,
                chunk6bChargeRepeatBefore != null && chunk6bChargeRepeatInput != null,
                "A repeated charge without the rider's standard action was refused before any cost, path or attack.",
                evidence);
            chunk6bChargeRepeatStage = 2;
        }

        private void RestoreChunk6bChargeSetting()
        {
            if (!chunk6bChargeSettingCaptured || chunk6bChargeSettingRestored)
            {
                return;
            }

            settings.EnableMountedCharge = chunk6bChargeOriginalSetting;
            nativeControls.Update();
            chunk6bChargeSettingRestored = settings.EnableMountedCharge == chunk6bChargeOriginalSetting;
            Chunk6bChargeMeasurement["settingRestored"] = chunk6bChargeSettingRestored;
            Chunk6bChargeMeasurement["settingAfter"] = settings.EnableMountedCharge;
            if (!chunk6bChargeSettingRestored)
            {
                throw new InvalidOperationException("The Mounted Charge setting was not restored exactly.");
            }
        }
    }
}
