using System;
using System.IO;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.PubSubSystem;
using Kingmaker.UI.Selection;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class RuntimePersistenceScenario
    {
        private bool RealtimeMounted => Checkpoint == "mounted-spent";
        private Phase3dCombatRuleProbe realtimeProbe;
        private RealtimeRoundProbe realtimeRounds;
        private long realtimeSavedTicks;
        private long realtimeReadyTicks;
        private int realtimeAttackBaseline;
        private int realtimeRoundBaseline;
        private int realtimeLaterAttacks;
        private bool realtimeDebtWaitObserved;
        private bool realtimeBoundaryObserved;
        private Action restoreRealtimeAi;

        private sealed class RealtimeRoundProbe : IUnitNewCombatRoundHandler, IDisposable
        {
            private readonly UnitEntityData actor;
            private readonly IDisposable subscription;
            internal int Count;
            internal RealtimeRoundProbe(UnitEntityData actor)
            { this.actor = actor; subscription = EventBus.Subscribe(this); }
            public void HandleNewCombatRound(UnitEntityData unit) { if (unit == actor) Count++; }
            public void Dispose() => subscription.Dispose();
        }

        private JObject RealtimeObservation() => new JObject {
            ["target"] = combatTarget?.UniqueId, ["targetDamage"] = combatTarget?.Damage,
            ["riderAiEnabled"] = rider?.IsAIEnabled, ["mountAiEnabled"] = mount?.IsAIEnabled,
            ["riderRounds"] = realtimeRounds?.Count, ["resolved"] = realtimeProbe?.RiderResolvedCount,
            ["ordinaryAttacks"] = realtimeProbe?.RiderNonOpportunityAttackRuleCount, ["forcedD20"] = realtimeProbe?.PairForcedD20Count,
            ["rules"] = realtimeProbe?.CapturePairEvidence(),
            ["riderCommandsEmpty"] = rider?.Commands.Empty, ["mountCommandsEmpty"] = mount?.Commands.Empty,
            ["unresolvedProjectiles"] = NativeSaveEffectBoundary.HasUnresolvedProjectiles(),
            ["nativeCommands"] = new JArray(Game.Instance.State.Units.Where(u => u.IsInCombat).Select(u => new JObject {
                ["actor"] = u.UniqueId, ["standard"] = u.CombatState.Cooldown.StandardAction,
                ["raw"] = new JArray(u.Commands.Raw.Where(c => c != null).Select(c => new JObject {
                    ["type"] = c.GetType().Name, ["started"] = c.IsStarted, ["finished"] = c.IsFinished })),
                ["queue"] = new JArray(u.Commands.Queue.Select(c => c.GetType().Name)) })),
            ["savedTicks"] = realtimeSavedTicks, ["readyTicks"] = realtimeReadyTicks };

        private void AdvanceRealtime()
        {
            if (clock.Elapsed.TotalSeconds > 150)
                throw new InvalidOperationException("P04 " + Checkpoint + " timed out at " + stage + ": " + persistence.Feedback);
            var game = Game.Instance;
            if (LoadingProcess.Instance.IsLoadingInProcess ||
                (game.CurrentMode != Kingmaker.GameModes.GameModeType.Default &&
                 game.CurrentMode != Kingmaker.GameModes.GameModeType.Pause)) return;
            if (stage > 0 && !Cold && combatTarget != null && !targetService.RefreshBidirectionalCombatMemoryLease())
                throw new InvalidOperationException("RT owned native combat memory was lost.");
            if (stage > 0 && game.IsPaused)
            {
                Write("fixture-native-unpause");
                game.IsPaused = false;
                return;
            }
            if (stage == 0)
            {
                Check(settings.EnablePairedActivation && !settings.EnableUnifiedMountedTurn &&
                    !settings.EnablePairedCommandScheduler && !settings.EnableDiagnosticOverlay &&
                    !Kingmaker.UI.SettingsUI.SettingsRoot.Instance.EnableTurnBasedMode.CurrentValue,
                    "P04-exact-declared-RT-paired-policy");
                if (Cold)
                {
                    var data = persistence.LoadedData;
                    Check(data?.Combat != null && !data.Combat.TurnBased && data.Mounted == RealtimeMounted &&
                        !persistence.CombatRestorationPending, "RT-selected-archive-semantic-restoration-complete");
                    var owners = game.Player.Party.Where(u => u.Descriptor.Pet?.Blueprint.AssetGuid ==
                        KingmakerMountedPairRuntime.MammothBlueprintGuid).ToArray();
                    Check(owners.Length == 1, "RT-same-unique-native-companion-owner");
                    rider = owners[0]; mount = rider.Descriptor.Pet;
                    Check(data.Combat.Actors.Any(a => a.Native.Id == rider.UniqueId) &&
                        data.Combat.Actors.Any(a => a.Native.Id == mount.UniqueId), "RT-native-actors-belong-to-selected-archive");
                    combatTarget = data.Combat.Actors.Select(a => game.State.Units.Single(u => u.UniqueId == a.Native.Id))
                        .Single(u => u.IsEnemy(rider) && u.IsInCombat);
                    Check(combatTarget.Stats.HitPoints.BaseValue == 256 && combatTarget.Descriptor.State.IsConscious,
                        "RT-native-target-health-without-cold-provision");
                    Check(persistence.SemanticRestoreCount == data.Combat.Actors.Length &&
                        persistence.PresentationRestoreCount == (RealtimeMounted ? 1 : 0) &&
                        controls.NativeCastRequestCount == 0, "RT-no-new-acquisition-mount-or-duplicate-restore");
                    Check(!rider.IsAIEnabled && !mount.IsAIEnabled, "RT-native-saved-AI-switch-without-cold-injection");
                    BindRealtimeObservers();
                    ValidateRealtimeRemainder(data);
                    Write("initial", RealtimeObservation());
                    Write("rt-cold-debt-restored", new JObject {
                        ["snapshot"] = JObject.FromObject(data, MountedSaveCodec.CreateSerializer()),
                        ["actual"] = RealtimeObservation() });
                    BeginRealtimeContinuation();
                    return;
                }
                string error;
                if (!relationship.TryResolveAutomationPair(out rider, out mount, out error))
                    throw new InvalidOperationException(error);
                Check(!game.Player.IsInCombat, "RT-source-starts-outside-combat");
                // Native Stop retires only unstarted orders. Use the player's
                // native AI-off state for this idle control so finished manual
                // routines cannot immediately reacquire an autonomous target.
                var ai = NativeCombatActorPersistence.Field(typeof(UnitEntityData), "m_AiEnabled", 0x040054BA, typeof(bool));
                var riderAi = (bool)ai.GetValue(rider);
                var mountAi = (bool)ai.GetValue(mount);
                restoreRealtimeAi = () => { rider.IsAIEnabled = riderAi; mount.IsAIEnabled = mountAi; };
                rider.IsAIEnabled = false; mount.IsAIEnabled = false;
                if (RealtimeMounted) Check(relationship.MountRiderOn(rider, mount).Succeeded, "RT-source-mounted-before-combat");
                controls.Update();
                if (RealtimeMounted) BindOwnedControlSlots();
                beforeControls = controls.CaptureSnapshot();
                targetService = new DiagnosticCombatTargetService(logger);
                combatTarget = targetService.Spawn(rider, mount, FindDestination(7f), request.RunId, true, false, true);
                Check(targetService.PrepareForPlayerClick(combatTarget) &&
                    targetService.QueueBidirectionalCombatMemory(rider, combatTarget), "RT-native-unmounted-or-mounted-control-combat");
                BindRealtimeObservers();
                Write("initial", RealtimeObservation());
                stage = 1; return;
            }
            if (stage == 1)
            {
                if (!game.Player.IsInCombat || !rider.CombatState.CanActInCombat) return;
                Check(!CombatController.IsInTurnBasedCombat(), "RT-native-combat-mode");
                Check(targetService.PrepareForPlayerClick(combatTarget) &&
                    targetService.BeginExpectedAttackDispatch(combatTarget), "RT-owned-target-native-input-ready");
                QueueRealtimeAttack();
                Write("rt-repeated-attack-requested", RealtimeObservation());
                stage = 2; return;
            }
            if (stage == 2)
            {
                if (realtimeProbe.RiderNonOpportunityAttackRuleCount < 2 || realtimeProbe.RiderResolvedCount < 2) return;
                Check(realtimeProbe.PairForcedD20Count == 0, "RT-ordinary-attacks-have-native-rolls");
                StopRealtimePartyOrders();
                Write("rt-repeated-attack-resolved", RealtimeObservation());
                stage = 3; return;
            }
            if (stage == 3)
            {
                if (game.State.Units.Any(u => u.IsInCombat && !u.Commands.Empty) ||
                    NativeSaveEffectBoundary.HasUnresolvedProjectiles())
                {
                    if (!realtimeBoundaryObserved)
                    {
                        Write("rt-waiting-command-boundary", RealtimeObservation());
                        realtimeBoundaryObserved = true;
                    }
                    return;
                }
                Check(rider.CombatState.Cooldown.StandardAction > 0.1f && game.SaveManager.IsSaveAllowed(),
                    "RT-actual-spent-native-standard-at-safe-save-boundary");
                Write("rt-before-save", RealtimeObservation());
                var save = game.SaveManager.CreateNewSave("KMC_P01");
                callback = false;
                game.SaveGame(save, () => callback = true);
                Write("rt-native-save-requested", RealtimeObservation());
                stage = 4; return;
            }
            if (stage == 4)
            {
                if (!callback || NativePersistenceIsolation.HasPendingWrites) return;
                var save = game.SaveManager.SingleOrDefault(s => s.Name == "KMC_P01");
                if (save == null || !save.HasFileOnDisk || save.OperationState != SaveInfo.StateType.None) return;
                var read = NativeMountedSaveStorage.Read(save.Saver);
                Check(read.Kind == MountedSaveReadKind.Current && read.Data.Combat != null &&
                    !read.Data.Combat.TurnBased && read.Data.Mounted == RealtimeMounted &&
                    persistence.SnapshotCount == 1, "RT-actual-native-archive-contains-current-combat-debt");
                ValidateRealtimeRemainder(read.Data);
                Check(!persistence.SaveSuspended && !controls.SerializationSuspended &&
                    controls.CaptureSnapshot().ExactFactCount == beforeControls.ExactFactCount &&
                    controls.CaptureSnapshot().ManagedHotbarSlotCount == beforeControls.ManagedHotbarSlotCount,
                    "RT-save-restores-live-control-scopes-without-end-or-refund");
                Write("native-write-complete", new JObject {
                    ["path"] = save.FolderName, ["sha256"] = Hash(save.FolderName), ["length"] = new FileInfo(save.FolderName).Length,
                    ["nativeType"] = save.Type.ToString(), ["nativeCallback"] = callback, ["operation"] = save.OperationState.ToString(),
                    ["snapshot"] = JObject.FromObject(read.Data, MountedSaveCodec.CreateSerializer()),
                    ["actual"] = RealtimeObservation() });
                BeginRealtimeContinuation(); return;
            }
            if (stage == 10)
            {
                if (game.TimeController.GameTime.Ticks + TimeSpan.TicksPerMillisecond * 10 < realtimeReadyTicks)
                {
                    if (realtimeProbe.RiderResolvedCount != realtimeAttackBaseline || realtimeRounds.Count != realtimeRoundBaseline)
                        Check(false, "RT-spent-standard-cannot-deliver-or-refresh-early");
                    if (!realtimeDebtWaitObserved)
                    {
                        Write("rt-native-debt-wait", RealtimeObservation());
                        realtimeDebtWaitObserved = true;
                    }
                    return;
                }
                var delivered = realtimeProbe.RiderResolvedCount - realtimeAttackBaseline;
                if (delivered <= realtimeLaterAttacks) return;
                Check(delivered == realtimeLaterAttacks + 1 && realtimeProbe.PairForcedD20Count == 0,
                    "RT-one-native-delivery-per-later-attack");
                realtimeLaterAttacks = delivered;
                Check(realtimeRounds.Count - realtimeRoundBaseline == realtimeLaterAttacks,
                    "RT-exactly-once-native-refresh-at-real-cooldown-expiry");
                Write("rt-later-attack", RealtimeObservation());
                if (realtimeLaterAttacks < 2) return;
                Check(realtimeDebtWaitObserved, "RT-observed-native-wait-for-saved-debt");
                StopRealtimePartyOrders();
                Check(relationship.State == (RealtimeMounted ? RelationshipState.Mounted : RelationshipState.Unmounted) &&
                    controls.CaptureSnapshot().DuplicateFactCount == 0, "RT-usable-continuation-retains-save-specific-pair");
                Write("usable-continuation-complete", RealtimeObservation());
                Dispose();
                Result = new RuntimeSubscenarioResult { Name = request.Scenario, Status = "PASS",
                    AssertionPassCount = passed, AssertionFailCount = 0, Errors = new string[0] };
                Completed = true;
            }
        }

        private void BindRealtimeObservers()
        {
            realtimeProbe = new Phase3dCombatRuleProbe(rider, mount);
            realtimeProbe.Arm(combatTarget, false);
            realtimeRounds = new RealtimeRoundProbe(rider);
        }

        private void ValidateRealtimeRemainder(MountedSaveData data)
        {
            var game = Game.Instance;
            realtimeSavedTicks = data.GameTimeTicks;
            var elapsed = (game.TimeController.GameTime.Ticks - data.GameTimeTicks) / (double)TimeSpan.TicksPerSecond;
            var saved = data.Combat.Actors.Single(a => a.Native.Id == rider.UniqueId);
            Check(saved.Native.Standard > 0.1f && rider.CombatState.Cooldown.StandardAction > 0 &&
                data.Combat.Current == null && data.Combat.Roster.Length == 0 &&
                data.Combat.Paired?.Activation == null && !game.TurnBasedCombatController.Initialized,
                "RT-no-fresh-standard-or-turn-based-activation");
            Check(data.Combat.Actors.All(a => {
                var actor = game.State.Units.Single(u => u.UniqueId == a.Native.Id);
                return actor.CombatState.Prepared == a.Prepared && actor.IsInCombat == a.InCombat &&
                    LegitimateContinuation(a.Native, MountedPersistenceService.CaptureActor(actor), elapsed);
            }), "RT-current-native-debt-follows-restored-game-clock");
            Check(rider.CombatState.ExecutedAttackNumber == saved.ExecutedAttacks &&
                relationship.State == (RealtimeMounted ? RelationshipState.Mounted : RelationshipState.Unmounted),
                "RT-no-replayed-round-effect-or-invented-pair");
            var bindings = controls.CapturePersistentSlots();
            Check(bindings.Length == data.Slots.Length && data.Slots.All(s =>
                bindings.Any(a => a.ActorId == s.ActorId && a.Index == s.Index && a.Kind == s.Kind)) &&
                controls.CaptureSnapshot().DuplicateFactCount == 0, "RT-exact-owned-controls-once");
        }

        private void BeginRealtimeContinuation()
        {
            realtimeAttackBaseline = realtimeProbe.RiderResolvedCount;
            realtimeRoundBaseline = realtimeRounds.Count;
            realtimeReadyTicks = Game.Instance.TimeController.GameTime.Ticks +
                (long)(rider.CombatState.Cooldown.StandardAction * TimeSpan.TicksPerSecond);
            QueueRealtimeAttack();
            Write("rt-spent-attack-queued", RealtimeObservation());
            stage = 10;
        }

        private void QueueRealtimeAttack()
        {
            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            Game.Instance.DefaultPointerController.ClearPointerMode();
            using (var input = new NativeOrdinaryAttackInput(combatTarget))
                Check(input.Click(), "RT-ordinary-native-attack-input");
        }

        private void StopRealtimePartyOrders()
        {
            foreach (var actor in Game.Instance.Player.Party.Concat(new[] { mount }).Distinct()
                .Where(u => u.IsDirectlyControllable && u.View != null))
            {
                SelectionManager.Instance.SelectUnit(actor.View, true, true, false);
                SelectionManager.Instance.Stop();
            }
            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
        }
    }
}
