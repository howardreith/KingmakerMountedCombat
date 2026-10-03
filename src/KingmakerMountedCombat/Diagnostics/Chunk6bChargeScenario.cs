using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.Utility;
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
        internal static bool IsChunk6bChargeScenario(string scenario) =>
            string.Equals(scenario, Chunk6bChargeRtScenario, StringComparison.Ordinal);
        private bool IsChunk6bCharge => IsChunk6bChargeScenario(request.Scenario);
        private static readonly string[] Chunk6bChargeCases =
        {
            "C6B-CHARGE-default-off",
            "C6B-CHARGE-positive",
            "C6B-CHARGE-below-minimum",
            "C6B-CHARGE-stock-rejected"
        };

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

        private const string Chunk6bChargeRepeatRow = "C6B-CHARGE-spent-standard";

        private string Chunk6bChargeCaseId => Chunk6bChargeCases[chunk6bChargeCase];
        private JObject Chunk6bChargeMeasurement => (JObject)observations["chunk6bCharge"];

        private float Chunk6bChargeCaseDistance
        {
            get
            {
                switch (chunk6bChargeCase)
                {
                    case 2: return 3.5f;
                    default: return 9f;
                }
            }
        }

        private void BeginChunk6bCharge()
        {
            if (!settings.EnablePairedActivation || settings.EnableUnifiedMountedTurn ||
                settings.EnablePairedCommandScheduler || settings.EnableDiagnosticOverlay || playerAction.OverlayPresent)
                throw new InvalidOperationException("Chunk 6B requires the accepted paired configuration.");
            CaptureIdleFixturePartyForCleanup();
            chunk6bChargeOriginalSetting = settings.EnableMountedCharge;
            chunk6bChargeSettingCaptured = true;
            observations["chunk6bCharge"] = new JObject
            {
                ["contract"] = "chunk6b-pair-charge-delivery",
                ["mode"] = "RT",
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
                ["pairMovement"] = combat.LastPairedMovementObservation
            };
        }

        private void TickChunk6bCharge()
        {
            var game = Game.Instance;
            var controller = game.TurnBasedCombatController;
            var turn = controller.CurrentTurn;
            if (game.IsPaused) { game.IsPaused = false; return; }
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

                if (chunk6bChargeCase == 0)
                {
                    TickChunk6bChargeDefaultOff();
                    return;
                }

                if (rider.IsInCombat || horse.IsInCombat || !PrepareUnmountedHorseAiIsolation() || !PrepareCombatMountRiderAiIsolation()) return;
                if (turnBasedModeProbe == null) turnBasedModeProbe = new NativeModeTransitionProbe(false);
                if (!turnBasedModeProbe.TemporaryValueIsCurrent) { turnBasedModeProbe.DispatchTemporaryValueIfRequired(); return; }
                BeginTarget(Chunk6bChargeCaseDistance, Chunk6bChargeCaseId);
                ruleProbe.Arm(target, false);
                chunk6bChargeStage = 1; ResetLeafClock(); return;
            }

            if (chunk6bChargeStage == 1)
            {
                if (!IsCombatReady(true)) return;
                if (!rider.Commands.Empty || !horse.Commands.Empty || rider.AreHandsBusyWithAnimation ||
                    !rider.CombatState.Prepared || combat.HasActiveCommand) return;
                // Preview.158 measured the rider still waiting initiative for a moment after real-time combat
                // starts: UnitCombatState.CanActInCombat is m_InCombat && !IsWaitingInitiative, so the charge was
                // correctly refused as "requires a rider who can act". Wait for the same native readiness the
                // stock charge fixture waits for before it clicks.
                if (!rider.CombatState.CanActInCombat || !rider.IsAbleToAct()) return;
                if (rider.CombatState.Cooldown.StandardAction > 0.001f || rider.CombatState.Cooldown.MoveAction > 0.001f) return;

                chunk6bChargeAbility = FindChunk6bChargeAbility();
                if (chunk6bChargeAbility == null)
                    throw new InvalidOperationException("The KMC Mounted Charge ability is not leased on the rider.");
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                var nativeTarget = new TargetWrapper(target);
                chunk6bChargeBefore = new JObject
                {
                    ["identity"] = CaptureChunk6bChargeAbilityIdentity(),
                    ["state"] = CaptureChunk6bChargeActors("before"),
                    ["available"] = chunk6bChargeAbility.IsAvailableForCast,
                    ["unavailableReason"] = chunk6bChargeAbility.GetUnavailableReason(),
                    ["canTarget"] = chunk6bChargeAbility.CanTarget(nativeTarget),
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

                if (chunk6bChargeCase == 3)
                {
                    TickChunk6bChargeStockRejected();
                    return;
                }

                // The player-facing path: the real selected-ability handler. Hovering must change nothing.
                var handler = game.SelectedAbilityHandler;
                handler.SetAbility(chunk6bChargeAbility);
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
                chunk6bChargeClicked = handler.OnClick(target.View.gameObject, target.Position, 0, false, false);
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

                var settled = (chunk6bChargeShell == null || chunk6bChargeShell.IsFinished) &&
                    !combat.HasActiveCommand && rider.Commands.Empty && horse.Commands.Empty &&
                    (agent == null || !agent.IsReallyMoving) &&
                    ruleProbe.RiderResolvedCount >= ruleProbe.RiderNonOpportunityAttackRuleCount;
                if (!settled && elapsed < 14.0) return;
                chunk6bChargeLeaseEvidence = combat.LastMountedChargeCommand == null
                    ? null
                    : combat.LastMountedChargeCommand.CaptureChargeLeaseEvidence();
                chunk6bChargeStage = 3; ResetLeafClock(); return;
            }

            if (chunk6bChargeStage == 3)
            {
                // The repeated request belongs to this combat, because the rider's standard action must still
                // be spent when it is attempted.
                if (chunk6bChargeCase == 1 && chunk6bChargeRepeatStage < 2)
                {
                    TickChunk6bChargeRepeat();
                    return;
                }

                var evidence = new JObject
                {
                    ["level"] = "NATIVE DELIVERY",
                    ["mode"] = "RT",
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
                    ["terminal"] = combat.LastOutcome == null ? null : new JObject
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
            switch (chunk6bChargeCase)
            {
                case 0: return "The Mounted Charge control is absent while its setting is off and leased on the rider once it is on.";
                case 1: return "One player click delivered one rider-owned full-round charge: the mount carried the forced path and the rider struck once with the native charge rule.";
                case 2: return "A target inside the stock minimum charge distance was refused before any cost, path or attack.";
                default: return "The stock native Charge remained rejected while mounted.";
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
                ["mode"] = "RT",
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
                    ["canTarget"] = chunk6bChargeAbility.CanTarget(nativeTarget),
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
                ["mode"] = "RT",
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
