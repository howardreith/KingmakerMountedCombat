using System;
using System.IO;
using System.Linq;
using Kingmaker;
using Kingmaker.Controllers.Combat;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Commands;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    // Settled rider spell + charged/consumable item checkpoints on the existing
    // P02/P04 isolation, native SaveGame, archive and cold-process machinery.
    // No new save schema, resource restoration or in-flight transaction is added.
    internal sealed partial class RuntimePersistenceScenario
    {
        private bool CastingBaselinePersistence => Checkpoint == "mounted-casting-items" || Checkpoint == "casting-items";
        private bool CastingBaselineTb => Checkpoint == "casting-items";
        private NativeCastingItemTrace baselineCastTrace;
        private NativeActorAllocationTrace baselineCostTrace;
        private NativeCastingItemLease baselineRod, baselinePotion;
        private Kingmaker.Items.ItemEntityUsable baselineLoadedRod;
        private SpellSlot baselineSpellSlot;
        private UnitUseAbility baselineShell;
        private int baselineInputCount, baselineStable, baselineCostOffset;
        private bool baselineAttackInput, baselineObserversClosed;
        private JObject baselineInput;
        private const string BaselineSpell = "9f10909f0be1f5141bf1c102041f93d9";

        private JObject CastingBaselineObservation() => new JObject {
            ["contract"] = "settled-rider-spell-items-v1", ["tb"] = CastingBaselineTb, ["cold"] = Cold,
            ["rider"] = rider.UniqueId, ["mount"] = mount.UniqueId, ["mountBlueprint"] = mount.Blueprint.AssetGuid,
            ["inputs"] = baselineInputCount, ["slotAvailable"] = baselineSpellSlot.Available,
            ["slotBlueprint"] = baselineSpellSlot.Spell.Blueprint.AssetGuid,
            ["rod"] = CastingBaselineRod(), ["riderDamage"] = rider.Damage, ["mountDamage"] = mount.Damage,
            ["riderBudget"] = baselineCostTrace.Snapshot(rider), ["mountBudget"] = baselineCostTrace.Snapshot(mount),
            ["relationship"] = relationship.State.ToString(), ["generation"] = relationship.MountedPairGeneration,
            ["riderCommandsEmpty"] = rider.Commands.Empty, ["mountCommandsEmpty"] = mount.Commands.Empty,
            ["pairCommand"] = combat.HasActiveCommand, ["pairMovement"] = combat.HasActiveGroundMovement,
            ["abilitiesPending"] = NativeSaveEffectBoundary.HasUnresolvedAbilities(),
            ["projectilesPending"] = NativeSaveEffectBoundary.HasUnresolvedProjectiles(),
            ["processesSettled"] = baselineCastTrace.ProcessesSettled,
            ["snapshotCount"] = persistence.SnapshotCount, ["deferredSaves"] = persistence.DeferredSaveCount,
            ["castTrace"] = baselineCastTrace.Capture(), ["costTrace"] = baselineCostTrace.Capture(),
            ["ordinaryResolved"] = realtimeProbe.RiderResolvedCount, ["ordinaryInputs"] = baselineAttackInput ? 1 : 0
        };
        private JObject CastingBaselineRod()
        {
            var item = baselineRod?.Item ?? baselineLoadedRod;
            var slots = rider.Body.QuickSlots.ToArray();
            return new JObject { ["blueprint"] = item?.Blueprint.AssetGuid, ["count"] = item?.Count,
                ["charges"] = item?.Charges, ["slotIndex"] = Array.FindIndex(slots, s => ReferenceEquals(s.MaybeItem, item)),
                ["exactCollection"] = item != null && item.Collection == rider.Inventory,
                ["nativeSourceItemExact"] = item != null && ReferenceEquals(item.ActivatableAbility?.SourceItem, item),
                ["nativeActivatableOn"] = item?.ActivatableAbility?.IsOn,
                ["sameBlueprintInventoryCount"] = rider.Inventory.Items.Count(i => i.Blueprint.AssetGuid == NativeCastingItemLease.LesserQuickenRod) };
        }
        private void BindCastingBaseline()
        {
            var slots = rider.Descriptor.Spellbooks.SelectMany(b => b.GetAllMemorizedSpells())
                .Where(s => s.Spell.Blueprint.AssetGuid == BaselineSpell).ToArray();
            if (slots.Length != 1) throw new InvalidOperationException("6C persistence lacks one exact rider native prepared Snowball slot.");
            baselineSpellSlot = slots[0];
            baselineCastTrace = new NativeCastingItemTrace(rider, mount);
            baselineCastTrace.BeginCase(Checkpoint);
            baselineCostTrace = new NativeActorAllocationTrace(rider, mount, combat);
            baselineCostTrace.BeginEncounter(request.RunId + ":casting-items");
            if (!Cold) {
                baselineRod = new NativeCastingItemLease(rider, baselineCastTrace, NativeCastingItemLease.LesserQuickenRod); baselineRod.Acquire();
                baselinePotion = new NativeCastingItemLease(rider, baselineCastTrace, NativeCastingItemLease.CurePotion); baselinePotion.Acquire();
                Write("6c-fixture-items-created", new JObject { ["rod"] = baselineRod.Evidence.DeepClone(), ["potion"] = baselinePotion.Evidence.DeepClone() });
            } else {
                var rods = rider.Inventory.Items.OfType<Kingmaker.Items.ItemEntityUsable>()
                    .Where(i => i.Blueprint.AssetGuid == NativeCastingItemLease.LesserQuickenRod).ToArray();
                if (rods.Length != 1) throw new InvalidOperationException("Cold 6C archive lacks its unique native saved rod; no acquisition is permitted.");
                baselineLoadedRod = rods[0];
            }
        }
        private void AdvanceCastingBaselinePersistence()
        {
            if (clock.Elapsed.TotalSeconds > 150) throw new InvalidOperationException("6C settled persistence timed out at " + stage);
            var game = Game.Instance;
            if (LoadingProcess.Instance.IsLoadingInProcess || persistence.CombatRestorationPending) return;
            if (game.IsPaused) { game.IsPaused = false; return; }
            if (stage > 0 && !Cold && !targetService.RefreshBidirectionalCombatMemoryLease())
                throw new InvalidOperationException("6C settled persistence lost its owned native encounter.");
            var turn = game.TurnBasedCombatController.CurrentTurn;
            if (stage == 0) {
                if (realtime == null) realtime = new NativeModeTransitionProbe(CastingBaselineTb);
                if (!realtime.TemporaryValueIsCurrent) { realtime.DispatchTemporaryValueIfRequired(); return; }
                Check(settings.EnablePairedActivation && !settings.EnableUnifiedMountedTurn && !settings.EnablePairedCommandScheduler && !settings.EnableDiagnosticOverlay,
                    "6C-existing-paired-policy");
                if (Cold) {
                    var data = persistence.LoadedData;
                    Check(data?.Mounted == true && data.Combat?.TurnBased == CastingBaselineTb && relationship.State == RelationshipState.Mounted,
                        "6C-cold-selected-archive-mode-pair");
                    rider = relationship.Rider; mount = relationship.Mount;
                    Check(data.Rider.Id == rider.UniqueId && data.Mount.Id == mount.UniqueId && controls.NativeCastRequestCount == 0,
                        "6C-cold-exact-actors-no-replayed-mount");
                    combatTarget = data.Combat.Actors.Select(a => game.State.Units.Single(u => u.UniqueId == a.Native.Id))
                        .Single(u => u.IsEnemy(rider) && u.IsInCombat);
                    BindCastingBaseline(); BindRealtimeObservers();
                    Write("initial", CastingBaselineObservation());
                    Write("6c-cold-settled-observed", new JObject { ["snapshot"] = JObject.FromObject(data, MountedSaveCodec.CreateSerializer()), ["actual"] = CastingBaselineObservation() });
                    Write("6c-cold-budget-observed", new JObject { ["rider"] = baselineCostTrace.Snapshot(rider), ["mount"] = baselineCostTrace.Snapshot(mount) });
                    Write("6c-cold-native-items-observed", CastingBaselineRod());
                    stage = 8; return;
                }
                string error;
                if (!relationship.TryResolveAutomationPair(out rider, out mount, out error)) throw new InvalidOperationException(error);
                Check(!game.Player.IsInCombat && mount.Blueprint.AssetGuid == SupportedMountedProfiles.MammothBlueprintGuid, "6C-source-original-native-pair");
                var riderAi = rider.IsAIEnabled; var mountAi = mount.IsAIEnabled;
                restoreRealtimeAi = () => { rider.IsAIEnabled = riderAi; mount.IsAIEnabled = mountAi; };
                rider.IsAIEnabled = false; mount.IsAIEnabled = false;
                BindCastingBaseline();
                Check(relationship.MountRiderOn(rider, mount).Succeeded, "6C-source-mounted-before-combat");
                controls.Update(); BindOwnedControlSlots(); beforeControls = controls.CaptureSnapshot();
                targetService = new DiagnosticCombatTargetService(logger);
                combatTarget = targetService.Spawn(rider, mount, FindDestination(7f), request.RunId, true, false, true);
                Check(targetService.PrepareForPlayerClick(combatTarget) && targetService.QueueBidirectionalCombatMemory(rider, combatTarget), "6C-source-native-encounter");
                BindRealtimeObservers(); Write("initial", CastingBaselineObservation()); stage = 1; return;
            }
            if (stage == 1) {
                if (!rider.IsInCombat || !rider.CombatState.CanActInCombat || !PairIdle) return;
                if (CastingBaselineTb && turn?.Unit != rider) { EndFixtureTurn(turn); return; }
                if (!rider.HasSwiftAction()) return;
                baselineCostOffset = baselineCostTrace.EventCount;
                baselineInput = new JObject { ["before"] = CastingBaselineObservation(), ["inputs"] = new JArray() };
                BaselineCastInput(baselineSpellSlot.Spell, combatTarget); stage = 2; return;
            }
            if (stage == 2) {
                if (!BaselineCastSettled()) return;
                baselineInput["afterSpell"] = CastingBaselineObservation();
                var factor = game.Player.Difficulty.DamageToParty;
                if (rider.Damage != 0 || factor <= 0 || float.IsNaN(factor) || float.IsInfinity(factor)) throw new InvalidOperationException("6C potion stimulus lacks intact disposable rider.");
                var amount = (int)Math.Ceiling(3d / factor);
                if (amount * factor >= rider.Stats.HitPoints.ModifiedValue - 2) throw new InvalidOperationException("6C potion stimulus has no native nonlethal margin.");
                Rulebook.Trigger(new RuleDealDamage(combatTarget, rider, new DamageBundle(new DirectDamage(new DiceFormula(0, DiceType.Zero), amount))));
                baselineInput["beforePotion"] = CastingBaselineObservation();
                BaselineCastInput(baselinePotion.Item.Ability.Data, rider); stage = 3; return;
            }
            if (stage == 3) {
                if (!BaselineCastSettled()) return;
                if (!game.SaveManager.IsSaveAllowed()) return;
                baselineInput["afterPotion"] = CastingBaselineObservation();
                baselineInput["costWindow"] = baselineCostTrace.EventsSince(baselineCostOffset);
                Write("6c-settled-use", baselineInput);
                Write("6c-save-request", CastingBaselineObservation());
                if (!game.SaveManager.IsSaveAllowed()) return;
                var save = game.SaveManager.CreateNewSave("KMC_P01"); callback = false;
                game.SaveGame(save, () => callback = true); stage = 4; return;
            }
            if (stage == 4) {
                if (!callback) return;
                var save = game.SaveManager.SingleOrDefault(s => s.Name == "KMC_P01");
                if (save == null || save.OperationState != SaveInfo.StateType.None || !save.HasFileOnDisk) return;
                var read = NativeMountedSaveStorage.Read(save.Saver);
                Check(read.Kind == MountedSaveReadKind.Current && read.Data.Mounted && read.Data.Combat?.TurnBased == CastingBaselineTb,
                    "6C-existing-native-archive-read-complete");
                Write("native-write-complete", new JObject { ["path"] = save.FolderName, ["sha256"] = Hash(save.FolderName),
                    ["length"] = new FileInfo(save.FolderName).Length, ["nativeType"] = save.Type.ToString(), ["nativeCallback"] = callback,
                    ["operation"] = save.OperationState.ToString(), ["snapshot"] = JObject.FromObject(read.Data, MountedSaveCodec.CreateSerializer()),
                    ["actual"] = CastingBaselineObservation() }); stage = 8; return;
            }
            if (stage == 8) {
                if (!PairIdle || !rider.CombatState.CanActInCombat || !rider.HasStandardAction()) return;
                if (CastingBaselineTb && turn?.Unit != rider) { EndFixtureTurn(turn); return; }
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                using (var input = new NativeOrdinaryAttackInput(combatTarget)) { if (!input.Click()) return; }
                baselineAttackInput = true; Write("6c-ordinary-continuation-input", CastingBaselineObservation()); stage = 9; return;
            }
            if (stage == 9) {
                if (realtimeProbe.RiderResolvedCount == 0) return;
                rider.Commands.InterruptAll(); mount.Commands.InterruptAll();
                rider.Commands.RemoveFinishedAndUpdateQueue(); mount.Commands.RemoveFinishedAndUpdateQueue();
                if (!BaselineCastSettled()) return;
                Write("usable-continuation-complete", CastingBaselineObservation());
                Dispose(); Result = new RuntimeSubscenarioResult { Name = request.Scenario, Status = "PASS", AssertionPassCount = passed, AssertionFailCount = 0, Errors = new string[0] }; Completed = true;
            }
        }
        private void BaselineCastInput(AbilityData ability, Kingmaker.EntitySystem.Entities.UnitEntityData subject)
        {
            if (ability?.Caster?.Unit != rider) throw new InvalidOperationException("6C persistence native caster is not rider.");
            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            var handler = Game.Instance.SelectedAbilityHandler; handler.SetAbility(ability);
            var resolved = handler.GetTarget(subject.View.gameObject, subject.Position, ability);
            if (resolved?.Unit != subject || !ability.CanTarget(resolved) || !handler.OnClick(subject.View.gameObject, subject.Position, 0, false, false))
                throw new InvalidOperationException("6C persistence normal native cast input was not admitted.");
            var shells = rider.Commands.Raw.Concat(rider.Commands.Queue).OfType<UnitUseAbility>().Where(c => c.Spell.Blueprint == ability.Blueprint).Distinct().ToArray();
            if (shells.Length != 1) throw new InvalidOperationException("6C persistence input lacks its exact unique native shell.");
            baselineShell = shells[0]; baselineInputCount++; baselineStable = 0;
            ((JArray)baselineInput["inputs"]).Add(new JObject { ["ability"] = baselineCastTrace.Ability(ability), ["target"] = subject.UniqueId,
                ["castShell"] = baselineCastTrace.Identity(baselineShell), ["costShell"] = baselineCostTrace.ObjectIdentity(baselineShell) });
        }
        private bool BaselineCastSettled()
        {
            if ((baselineShell != null && !baselineShell.IsFinished) || !PairIdle ||
                !baselineCastTrace.ProcessesSettled || NativeSaveEffectBoundary.HasUnresolvedAbilities() ||
                NativeSaveEffectBoundary.HasUnresolvedProjectiles()) { baselineStable = 0; return false; }
            return ++baselineStable >= 10;
        }
        private void DisposeCastingBaseline()
        {
            if (baselineCastTrace == null || baselineObserversClosed) return;
            if (baselineShell != null && !baselineShell.IsFinished) baselineShell.Interrupt();
            if (!PairIdle || !baselineCastTrace.ProcessesSettled || NativeSaveEffectBoundary.HasUnresolvedAbilities() || NativeSaveEffectBoundary.HasUnresolvedProjectiles())
                throw new InvalidOperationException("6C native process remains live; casting/item owners retained.");
            baselinePotion?.Dispose(); baselineRod?.Dispose();
            baselineCostTrace.Dispose(); baselineCastTrace.Dispose();
            Write("6c-observers-closed", new JObject {
                ["castTrace"] = baselineCastTrace.Capture(), ["costTrace"] = baselineCostTrace.Capture(),
                ["items"] = new JArray(new[] { baselinePotion, baselineRod }.Where(i => i != null).Select(i => i.Evidence.DeepClone())),
                ["coldItemWasReadOnly"] = Cold, ["nativeProcessesSettled"] = true,
                ["riderCommandsEmpty"] = rider.Commands.Empty, ["mountCommandsEmpty"] = mount.Commands.Empty
            });
            baselineObserversClosed = true;
        }
    }
}