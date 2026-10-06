using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints.Root;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Commands;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    // Charge checkpoints extend P04's native Mammoth/archive/cold-process fixture. The producer
    // records facts; Chunk6bPersistenceEvidence.ps1 owns acceptance. No charge is restored/replayed.
    internal sealed partial class RuntimePersistenceScenario
    {
        private bool RealtimeCharge => Checkpoint.StartsWith("mounted-charge-", StringComparison.Ordinal);
        private bool chargeSettingBefore;
        private UnitUseAbility persistenceChargeShell;
        private MountedPairAttackCommand persistenceChargeCommand;
        private JObject chargeSnapshot, chargeInput;
        private JObject chargeReadiness;
        private bool chargeFaultFired, chargeFaultObserved, chargeStopSent, chargeCommitRecorded;
        private int chargeCleanupAtRequest;
        private float chargePeakRiderStandard, chargePeakRiderMove, chargePeakMountStandard, chargePeakMountMove;

        private void BindChargePersistence()
        {
            if (!RealtimeCase || !RealtimeCharge) return;
            chargeSettingBefore = settings.EnableMountedCharge;
            settings.EnableMountedCharge = true;
            if (!Cold) persistence.SaveSnapshotStaged += ObserveChargeSnapshot;
        }

        private void DisposeChargePersistence()
        {
            if (!RealtimeCase || !RealtimeCharge) return;
            MountedChargeAdmissionFault.AfterQueue = null;
            MountedChargeAdmissionFault.BeforeCleanupStep = null;
            persistence.SaveSnapshotStaged -= ObserveChargeSnapshot;
            combat.ChargeOwnershipDrained -= ObserveChargeLifecycleDrain;
            settings.EnableMountedCharge = chargeSettingBefore;
        }

        private JObject ChargePersistenceObservation()
        {
            if (!RealtimeCharge) return null;
            var chargeBuff = BlueprintRoot.Instance?.SystemMechanics?.ChargeBuff;
            return new JObject
            {
                ["case"] = Checkpoint, ["cold"] = Cold, ["input"] = chargeInput,
                ["readiness"] = chargeReadiness,
                ["owner"] = combat.CaptureChargeOwnership(), ["lastDrained"] = combat.LastDrainedChargeOwnership?.DeepClone(),
                ["nativeBoundaries"] = combat.CaptureChargeNativeBoundaries(),
                ["cleanupAttempts"] = combat.ChargeCleanupAttemptCount,
                ["admitted"] = combat.MountedChargeAdmittedCount, ["refused"] = combat.MountedChargeRefusedCount,
                ["shell"] = DescribeFoundationCommand(persistenceChargeShell),
                ["shellActionType"] = persistenceChargeShell?.Spell?.ActionType.ToString(),
                ["shellFullRound"] = persistenceChargeShell?.Spell?.RequireFullRoundAction,
                ["shellProcessEnded"] = persistenceChargeShell?.ExecutionProcess == null || persistenceChargeShell.ExecutionProcess.IsEnded,
                ["command"] = DescribeFoundationCommand(persistenceChargeCommand),
                ["lease"] = persistenceChargeCommand?.CaptureChargeLeaseEvidence(),
                ["riderStandardSlot"] = DescribeFoundationCommand(rider?.Commands?.Standard),
                ["mountMoveSlot"] = DescribeFoundationCommand(mount?.Commands?.Move),
                ["riderCommandsEmpty"] = rider?.Commands.Empty, ["mountCommandsEmpty"] = mount?.Commands.Empty,
                ["mountPathPresent"] = mount?.View?.AgentASP?.Path != null,
                ["mountMoving"] = mount?.View?.AgentASP?.IsReallyMoving,
                ["mountCharging"] = mount?.View?.AgentASP?.IsCharging,
                ["mountSpeedOverride"] = mount?.View?.AgentASP?.MaxSpeedOverride,
                ["riderCharging"] = rider?.Descriptor?.State.IsCharging,
                ["chargeBuffCount"] = rider == null || chargeBuff == null ? -1 : rider.Buffs.Enumerable.Count(b => b.Blueprint == chargeBuff),
                ["chargeBuffGuid"] = chargeBuff?.AssetGuid,
                ["faultFired"] = chargeFaultFired, ["faultObserved"] = chargeFaultObserved,
                ["costMax"] = new JObject { ["riderStandard"] = chargePeakRiderStandard, ["riderMove"] = chargePeakRiderMove,
                    ["mountStandard"] = chargePeakMountStandard, ["mountMove"] = chargePeakMountMove },
                ["straightRoute"] = MountedChargeGeometry.StraightRoute(mount, combatTarget),
                ["landingBlocked"] = MountedChargeGeometry.LandingBlocked(mount, rider, combatTarget),
                ["nativeStopSent"] = chargeStopSent, ["workerRunning"] = persistence.ActiveSaveWorkerRunning,
                ["callback"] = callback, ["snapshot"] = chargeSnapshot?.DeepClone()
                , ["actors"] = rider == null || mount == null ? null : new JArray(new[] { rider, mount }.Select(actor => new JObject
                {
                    ["id"] = actor.UniqueId, ["prepared"] = actor.CombatState.Prepared, ["inCombat"] = actor.IsInCombat,
                    ["native"] = JObject.FromObject(MountedPersistenceService.CaptureActor(actor), MountedSaveCodec.CreateSerializer())
                }))
            };
        }

        private void BeginChargePersistenceInput()
        {
            EnsureFoundationRiderSelection();
            controls.Update();
            var ability = rider.Descriptor.Abilities.GetAbility(controls.MountedChargeAbility)?.Data;
            chargeReadiness = new JObject
            {
                ["prepared"] = rider.CombatState.Prepared, ["canAct"] = rider.CombatState.CanActInCombat,
                ["riderCommandsEmpty"] = rider.Commands.Empty, ["mountCommandsEmpty"] = mount.Commands.Empty,
                ["available"] = ability?.IsAvailableForCast, ["canTarget"] = ability?.CanTarget(combatTarget),
                ["reason"] = combat.ObserveMountedChargeTarget(rider, combatTarget).Reason
            };
            if (!rider.CombatState.Prepared || !rider.Commands.Empty || !mount.Commands.Empty ||
                rider.CombatState.Cooldown.StandardAction > 0.001f || rider.CombatState.Cooldown.MoveAction > 0.001f) return;
            Write("charge-readiness", RealtimeObservation());
            if (ability?.IsAvailableForCast != true || ability.CanTarget(combatTarget) != true ||
                !MountedChargeGeometry.StraightRoute(mount, combatTarget) || MountedChargeGeometry.LandingBlocked(mount, rider, combatTarget))
                throw new InvalidOperationException("P04 charge fixture did not establish legal input geometry: " + chargeReadiness);
            Check(targetService.PrepareForPlayerClick(combatTarget) && targetService.BeginExpectedAttackDispatch(combatTarget),
                "charge-owned-target-input-ready");
            Write("charge-before-input", RealtimeObservation());
            if (Checkpoint == "mounted-charge-failed")
                MountedChargeAdmissionFault.AfterQueue = () =>
                {
                    MountedChargeAdmissionFault.AfterQueue = null;
                    chargeFaultFired = true;
                    throw new InvalidOperationException("Owned P04 post-queue charge fault");
                };
            chargeInput = TryFoundationAbilityClick(controls.MountedChargeAbility, combatTarget);
            persistenceChargeShell = combat.OwnedChargeShell;
            Write("charge-input", RealtimeObservation());
            if (chargeInput.Value<bool?>("clicked") != true || persistenceChargeShell == null ||
                persistenceChargeShell.Spell?.Blueprint?.AssetGuid != NativeMountedControlService.MountedChargeAbilityGuid ||
                persistenceChargeShell.Executor != rider || persistenceChargeShell.Target?.Unit != combatTarget)
                throw new InvalidOperationException("P04 charge click did not yield the exact retained native shell.");
            stage = 60;
        }

        private void AdvanceChargePersistenceRequest()
        {
            if (persistenceChargeCommand == null) persistenceChargeCommand = combat.OwnedChargeCommand;
            if (Checkpoint == "mounted-charge-failed")
            {
                if (!chargeFaultFired || combat.HasChargeOwnership) return;
                Write("charge-postcommit-failed-drained", RealtimeObservation());
                RequestRealtimeSave(); return;
            }
            if (Checkpoint == "mounted-charge-settled")
            {
                if (realtimeProbe.RiderResolvedCount < 1 || combat.HasChargeOwnership) return;
                Write("charge-settled", RealtimeObservation());
                RequestRealtimeSave(); return;
            }
            if (chargeStopSent)
            {
                if (combat.HasChargeOwnership) return;
                Write("charge-cancelled-drained", RealtimeObservation());
                RequestRealtimeSave(); return;
            }
            // A real committed moving charge, before any rider attack. Waiting observes the
            // native boundary; no command, action debt or movement is synthesized to reach it.
            if (persistenceChargeShell?.IsActed != true || persistenceChargeCommand == null ||
                persistenceChargeCommand.IsFinished || mount.View?.AgentASP?.IsCharging != true ||
                mount.View.AgentASP.IsReallyMoving != true || realtimeProbe.RiderNonOpportunityAttackRuleCount != 0) return;
            Write("charge-live-before-boundary", RealtimeObservation());
            if (ChargeLifecycleCase) { BeginChargeLifecycleBoundary(); return; }
            if (Checkpoint == "mounted-charge-cancelled")
            {
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                SelectionManager.Instance.Stop();
                chargeStopSent = true;
                return;
            }
            chargeCleanupAtRequest = combat.ChargeCleanupAttemptCount;
            if (Checkpoint == "mounted-charge-drained")
                MountedChargeAdmissionFault.BeforeCleanupStep = stepName =>
                {
                    if (stepName != "mount-speed-override") return;
                    chargeFaultFired = true;
                    throw new InvalidOperationException("Owned P04 held speed cleanup fault");
                };
            RequestRealtimeSave();
        }

        // Called before LoadingProcess's normal early return, so our held fault cannot deadlock
        // its own diagnostic observation. Clearing it permits only ordinary production retry.
        private void ObserveChargeSaveWait()
        {
            if (RealtimeCharge && !Cold && rider != null && mount != null && (stage == 60 || stage == 4))
            {
                chargePeakRiderStandard = Math.Max(chargePeakRiderStandard, rider.CombatState.Cooldown.StandardAction);
                chargePeakRiderMove = Math.Max(chargePeakRiderMove, rider.CombatState.Cooldown.MoveAction);
                chargePeakMountStandard = Math.Max(chargePeakMountStandard, mount.CombatState.Cooldown.StandardAction);
                chargePeakMountMove = Math.Max(chargePeakMountMove, mount.CombatState.Cooldown.MoveAction);
                if (!chargeCommitRecorded && persistenceChargeShell?.IsActed == true)
                {
                    chargeCommitRecorded = true;
                    Write("charge-commit-observed", RealtimeObservation());
                }
            }
            if (!RealtimeCharge || Cold || stage != 4 || Checkpoint != "mounted-charge-drained" || chargeFaultObserved ||
                !chargeFaultFired || combat.ChargeCleanupAttemptCount <= chargeCleanupAtRequest) return;
            Write("charge-cleanup-debt-held", RealtimeObservation());
            chargeFaultObserved = true;
            MountedChargeAdmissionFault.BeforeCleanupStep = null;
        }

        private void ObserveChargeSnapshot()
        {
            chargeSnapshot = new JObject
            {
                ["owner"] = combat.CaptureChargeOwnership(), ["lastDrained"] = combat.LastDrainedChargeOwnership?.DeepClone(),
                ["riderCommandsEmpty"] = rider.Commands.Empty, ["mountCommandsEmpty"] = mount.Commands.Empty,
                ["unresolvedAbilities"] = NativeSaveEffectBoundary.HasUnresolvedAbilities(),
                ["faultObserved"] = chargeFaultObserved, ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks
            };
        }
    }
}
