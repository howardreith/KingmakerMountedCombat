using System;
using Kingmaker;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Parts;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class RuntimePersistenceScenario
    {
        private bool ConditionPreparingCase => Checkpoint == "condition-preparing";
        private bool conditionSaveRequested;
        private bool conditionWaitObserved;
        private bool conditionBarrierObserved;
        private double conditionRequestTime;

        private void RequestConditionPreparationSave()
        {
            if (!ConditionPreparingCase || Cold || conditionSaveRequested)
                throw new InvalidOperationException("Unexpected native preparation save request.");
            Check(combat.IsPreparingPairedActor(mount) && mount.Get<UnitPartConfusion>()?.Cmd == null &&
                persistence.SnapshotCount == 0 && Game.Instance.SaveManager.IsSaveAllowed(),
                "P03-save-request-inside-native-preparation-before-command-creation");
            conditionSaveRequested = true;
            conditionRequestTime = clock.Elapsed.TotalSeconds;
            Write("condition-preparation-save-request", new JObject {
                ["nativePreparing"] = combat.IsPreparingPairedActor(mount),
                ["commandPresent"] = mount.Get<UnitPartConfusion>()?.Cmd != null,
                ["snapshotCount"] = persistence.SnapshotCount
            });
            var descriptor = Game.Instance.SaveManager.CreateNewSave("KMC_P01");
            Check(descriptor.Type == SaveInfo.SaveType.Manual, "P03-preparation-native-manual-descriptor");
            // Native loading activation occurs synchronously here; it must defer
            // before its screen pauses the still-running preparation/effect.
            Game.Instance.SaveGame(descriptor, () => callback = true);
        }

        private void ObserveConditionPreparationWait()
        {
            if (!ConditionPreparingCase || Cold || !conditionSaveRequested || conditionBarrierObserved) return;
            if (NativeDeferredSave.Waiting(LoadingProcess.Instance) && !conditionWaitObserved)
            {
                conditionWaitObserved = true;
                conditionCommand = mount.Get<UnitPartConfusion>()?.Cmd;
                var unowned = new Kingmaker.UnitLogic.Commands.UnitAttack(combatTarget);
                unowned.Init(mount); // Query only: never queued or started.
                var ownedStart = combat.MayStartNativePreparationDuringSave(conditionCommand);
                var otherStart = combat.MayStartNativePreparationDuringSave(unowned);
                Check(ownedStart && !otherStart && !combat.MayStartNativePreparationDuringSave(null),
                    "P03-save-wait-admits-only-the-exact-owned-preparation-command");
                Write("condition-preparation-wait", new JObject {
                    ["waiting"] = true, ["ownedPreparationStart"] = ownedStart, ["unownedOrdinaryStart"] = otherStart,
                    ["deferredSaves"] = persistence.DeferredSaveCount,
                    ["snapshotCount"] = persistence.SnapshotCount, ["condition"] = ConditionObservation()
                });
            }
            if (clock.Elapsed.TotalSeconds - conditionRequestTime > 35)
            {
                Write("condition-preparation-not-settled", ConditionObservation());
                throw new InvalidOperationException("Native preparation save did not reach its barrier; callback=" +
                    callback + "; waiting=" + NativeDeferredSave.Waiting(LoadingProcess.Instance) +
                    "; snapshots=" + persistence.SnapshotCount);
            }
        }

        private void BeforeConditionPreparationSnapshot()
        {
            if (!ConditionPreparingCase || Cold) return;
            conditionCommand = mount.Get<UnitPartConfusion>()?.Cmd;
            Check(conditionSaveRequested && conditionWaitObserved && !conditionBarrierObserved &&
                persistence.DeferredSaveCount == 1 && persistence.SnapshotCount == 0 &&
                conditionCommand != null && conditionCommand.IsStarted && conditionCommand.IsFinished,
                "P03-preparation-save-waits-for-exact-native-command-completion");
            savedBoundary = Game.Instance.TurnBasedCombatController.CurrentTurn;
            savedSequence = combat.PairedActivationSequence;
            savedRound = Game.Instance.TurnBasedCombatController.RoundNumber;
            RetireConditionStimulus();
            conditionBarrierObserved = true;
            Write("condition-preparation-barrier", new JObject {
                ["snapshotCount"] = persistence.SnapshotCount, ["deferredSaves"] = persistence.DeferredSaveCount,
                ["condition"] = ConditionObservation()
            });
            stage = 4;
        }

        private void RetireConditionStimulus()
        {
            Check(conditionCommand.Result == UnitCommand.ResultType.Success &&
                (int)conditionLease.Evidence["nativeSelfDamageRules"] == 1 &&
                (int)conditionLease.Evidence["choiceOverrides"] == 1 &&
                mount.Damage > conditionOriginalDamage && combat.PairedActorEnded(mount) && !combat.PairedActorEnded(rider) &&
                ReferenceEquals(Game.Instance.TurnBasedCombatController.CurrentTurn, savedBoundary) &&
                relationship.State == Domain.RelationshipState.Unmounted && persistence.SaveEffectsReady() &&
                rider.CombatState.Cooldown.StandardAction == 0 && rider.CombatState.Cooldown.MoveAction == 0,
                "P03-native-condition-forfeits-only-mount-and-retains-principal");
            conditionSavedDamage = mount.Damage;
            conditionLease.Dispose(); conditionFact.Dispose();
            Check(mount.Damage == conditionSavedDamage &&
                (bool)conditionLease.Evidence["cleanupResourcesUnchanged"] &&
                (bool)conditionLease.Evidence["directControlRestored"] && (bool)conditionFact.Capture()["restored"],
                "P03-test-stimuli-removed-before-save-with-native-harm-and-costs-retained");
            controls.Update(); beforeControls = controls.CaptureSnapshot();
            conditionInitialRiderGrants = conditionTrace.GrantCount(rider);
            conditionInitialMountGrants = conditionTrace.GrantCount(mount);
            Write("condition-forfeit-retained", ConditionObservation());
        }
    }
}
