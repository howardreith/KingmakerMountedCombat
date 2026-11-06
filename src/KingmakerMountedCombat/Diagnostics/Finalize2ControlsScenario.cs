using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Localization;
using Kingmaker.UI.ActionBar;
using Kingmaker.UI.Selection;
using Kingmaker.UI.UnitSettings;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.Utility;
using Kingmaker.View;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityModManagerNet;

namespace KingmakerMountedCombat.Diagnostics
{
    // F2 (ART-02, CTRL-01/02/03) real-time rows. Every input is the native one a player's toolbar or hotkey reaches: the
    // MechanicActionBarSlotAbility.OnClick of the rider's own bar slot, then the native selected-ability click on a unit.
    // Setup is disclosed in the measurement (the removal, the injected old-build bindings, the hostile's places, the hidden
    // shells driven through the existing native ability-click helper, the direct refusal probes). The rows record raw
    // native observations; the external reader owns PASS/FAIL. The Sprint 7 charge-terminal helpers are shared, so the
    // mounted Charge's delivery is recorded exactly as the accepted S7-rt-charge row records it.
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        internal const string Finalize2ControlsRealTime = "finalize-f2-controls-rt";
        internal static bool IsFinalize2ControlsScenario(string scenario) => scenario == Finalize2ControlsRealTime;
        private bool IsFinalize2Controls => IsFinalize2ControlsScenario(request.Scenario);
        internal const int Finalize2SchemaVersion = 53;
        internal const double Finalize2ScenarioDeadlineSeconds = 360.0d, Finalize2HostDeadlineSeconds = 520.0d;
        internal static readonly string[] Finalize2Cases = { "F2-placement", "F2-strings", "F2-mount-cycle", "F2-alias-and-removal", "F2-charge-mounted", "F2-refusals", "F2-charge-unmounted" };
        internal const string Finalize2ShotUnmounted = "public-control-unmounted", Finalize2ShotMounted = "public-control-mounted", Finalize2ShotSelection = "charge-target-selection-mounted";
        private const float Finalize2StepBoundSeconds = 25f, Finalize2CaptureBoundSeconds = 5f, Finalize2StandardWaitSeconds = 15f;
        private const int Finalize2DismountAttempts = 12;
        private static readonly float[] Finalize2LaneRadii = { 12f, 10f, 14f, 9f, 16f, 8f };
        private static readonly float[] Finalize2StockRadii = { 10f, 12f, 9f, 14f, 8f, 16f };
        private string Finalize2Case => Finalize2Cases[f2CaseIndex];

        private int f2CaseIndex, f2Stage = 1, f2Step, f2Frames, f2BoundStep = -1, f2ControlIndex = -1, f2ChargeIndex = -1, f2K1 = -1, f2K2 = -1, f2Attempts, f2CaptureGeneration;
        private int f2WatchFrames, f2WatchAbsent, f2WatchFactReplaced, f2WatchCountNotOne, f2WatchDataNotFact, f2WatchIndexMoved, f2StockFrames, f2StockWatched, f2AdmittedBefore;
        private float f2BoundAt, f2CaptureStartedAt, f2PeakMove;
        private bool f2CaptureActive, f2Watching, f2StockWatching;
        private object f2ControlFact;
        private BlueprintAbility f2StockBlueprint;
        private UnitUseAbility f2Shell, f2StockCommand;
        private JObject f2Facts, f2Measure;
        private JArray f2Shots, f2Setup, f2WatchAnomalies;
        private ChargeWarningObserver f2Warnings;

        private void BeginFinalize2Controls()
        {
            if (!settings.EnablePairedActivation || settings.EnableUnifiedMountedTurn ||
                settings.EnablePairedCommandScheduler || settings.EnableDiagnosticOverlay || playerAction.OverlayPresent)
                throw new InvalidOperationException("F2 rows require the accepted single paired authority.");
            if (nativeControls.MountStateAbility == null || nativeControls.MountAbility == null ||
                nativeControls.DismountAbility == null || nativeControls.MountedChargeAbility == null)
                throw new InvalidOperationException("F2 rows require the registered public control and its delivery shells.");
            CaptureIdleFixturePartyForCleanup();
            castingTrace = new NativeCastingItemTrace(rider, horse);
            castingCosts = new NativeActorAllocationTrace(rider, horse, combat);
            s3Rules = new Chunk4IncomingRuleObserver(rider, horse);
            // Shared with the Sprint 7 terminal helpers: one rule observer, and s7Facts always names the current F2 row.
            s7Rules = new Sprint6RuleObserver(rider, horse);
            f2Warnings = new ChargeWarningObserver();
            var shots = new JArray(); var setup = new JArray();
            f2Measure = new JObject { ["contract"] = "finalize-f2-canonical-controls-v1", ["mode"] = "RT",
                ["rider"] = rider.UniqueId, ["mount"] = horse.UniqueId, ["mountBlueprint"] = horse.Blueprint.AssetGuid, ["cases"] = new JArray(Finalize2Cases),
                ["identities"] = new JObject { ["publicControl"] = nativeControls.MountStateAbility.AssetGuid, ["mountShell"] = nativeControls.MountAbility.AssetGuid,
                    ["dismountShell"] = nativeControls.DismountAbility.AssetGuid, ["mountedChargeShell"] = nativeControls.MountedChargeAbility.AssetGuid,
                    ["stockCharge"] = MountedChargeSafetyPolicy.ChargeBlueprintId, ["expectedFeedback"] = MountedChargeSafetyPolicy.Feedback,
                    ["mainBarSlots"] = MountedControlSlotPolicy.MainBarSlotCount },
                ["defaultFeatureEnabled"] = settings.EnableMountedCharge, ["attackRules"] = s7Rules.Attacks, ["screenshots"] = shots, ["disclosedSetup"] = setup,
                ["startBar"] = F2Bar(), ["startCounters"] = F2Counters(), ["startRelationship"] = relationship.State.ToString(),
                ["policy"] = "One public Mount / Dismount control and the ordinary Charge, driven through the rider's own bar slots." };
            f2Shots = shots; f2Setup = setup;
            observations["finalize2Controls"] = f2Measure;
            castingOriginalSlots = new NativeCastingOriginalSlots(rider, castingTrace);
            F2NewFacts();
            step = Phase3dHorseStep.Phase3gControls;
            ResetLeafClock();
        }

        private JObject CaptureFinalize2DeadlineProgress() => new JObject { ["stage"] = f2Stage, ["step"] = f2Step, ["caseIndex"] = f2CaseIndex,
            ["case"] = f2CaseIndex < Finalize2Cases.Length ? Finalize2Case : null, ["facts"] = f2Facts?.DeepClone(), ["state"] = F2State() };

        private bool DrainFinalize2Fixture()
        {
            if (f2CaptureActive && Time.realtimeSinceStartup - f2CaptureStartedAt <= Finalize2CaptureBoundSeconds) return false;
            f2CaptureActive = false;
            var handler = Game.Instance?.SelectedAbilityHandler;
            if (handler?.Ability != null) handler.DropAbility();
            if (f2Warnings != null) { f2Measure["warnings"] = f2Warnings.Capture(); f2Warnings.Dispose(); f2Warnings = null; }
            if (f2Measure != null && rider?.UISettings != null) f2Measure["finalBar"] = F2Bar();
            return DrainSprint7Fixture();
        }

        private void F2NewFacts()
        {
            f2Facts = new JObject { ["case"] = Finalize2Case, ["steps"] = new JObject() };
            s7Facts = f2Facts;
        }

        private void F2Disclose(string action) =>
            f2Setup.Add(new JObject { ["case"] = f2CaseIndex < Finalize2Cases.Length ? Finalize2Case : null, ["frame"] = Time.frameCount, ["action"] = action });

        private void FinishF2Row(bool passed, string detail)
        {
            F2EndWatch(); F2EndStockWatch();
            f2Facts["after"] = F2State();
            AddRow(Finalize2Case, passed, detail, f2Facts);
            f2CaseIndex++; f2Step = 0; f2Attempts = 0;
            if (f2CaseIndex == Finalize2Cases.Length || !passed) { BeginCleanup(); return; }
            F2NewFacts();
            ResetLeafClock();
        }

        private void F2Fail(string why) { f2Facts["failure"] = why; FinishF2Row(false, why); }

        private void TickFinalize2Controls()
        {
            var game = Game.Instance;
            if (game.IsPaused) { game.IsPaused = false; return; }
            CastingMeasurement["progress"] = new JObject { ["stage"] = f2Stage, ["caseIndex"] = f2CaseIndex, ["step"] = f2Step };
            if (f2Watching) F2WatchControl();
            if (f2StockWatching) F2WatchStockCharge();
            if (f2CaptureActive)
            {
                if (Time.realtimeSinceStartup - f2CaptureStartedAt <= Finalize2CaptureBoundSeconds) return;
                f2CaptureActive = false;
                f2Shots.Add(new JObject { ["error"] = "The end-of-frame capture did not complete within its bound.", ["frame"] = Time.frameCount });
            }
            if (f2Stage == 1)
            {
                if (!rider.Commands.Empty || !horse.Commands.Empty || rider.AreHandsBusyWithAnimation) return;
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                var isolationReady = PrepareUnmountedHorseAiIsolation();
                if (!CastingEntryReady(false, relationship.State == RelationshipState.Mounted, isolationReady)) return;
                f2Measure["entry"] = F2State();
                f2Step = 0; f2BoundStep = -1; f2Stage = 2; ResetLeafClock(); return;
            }
            if (f2Step != f2BoundStep) { f2BoundStep = f2Step; f2BoundAt = Time.time; f2Frames = 0; ResetLeafClock(); }
            else if (Time.time - f2BoundAt > Finalize2StepBoundSeconds) { F2Fail("Step " + f2Step + " exceeded its bound."); return; }
            targetService?.ObserveTargetLifeState(); targetService?.RefreshBidirectionalCombatMemoryLease();
            switch (Finalize2Case)
            {
                case "F2-placement": TickF2Placement(); return;
                case "F2-strings": TickF2Strings(); return;
                case "F2-mount-cycle": TickF2MountCycle(); return;
                case "F2-alias-and-removal": TickF2AliasAndRemoval(); return;
                case "F2-charge-mounted": TickF2ChargeMounted(); return;
                case "F2-refusals": TickF2Refusals(); return;
                case "F2-charge-unmounted": TickF2ChargeUnmounted(); return;
                default: throw new InvalidOperationException("Unknown F2 case " + Finalize2Case);
            }
        }

        // ---- observation ------------------------------------------------------------------------------------------------
        private static int F2Id(object value) => value == null ? 0 : RuntimeHelpers.GetHashCode(value);

        // The key the placement observation uses for the main bar: "" for an empty or stale slot, else the ability's
        // blueprint, else the slot's type.
        private static string F2SlotKey(MechanicActionBarSlot slot)
        {
            if (slot == null || slot is MechanicActionBarSlotEmpty || slot.IsBad()) return "";
            return (slot as MechanicActionBarSlotAbility)?.Ability?.Blueprint?.AssetGuid ?? slot.GetType().Name;
        }

        private JObject F2Bar()
        {
            var keys = new JArray(); var types = new JArray();
            var slots = rider.UISettings?.Slots;
            if (slots != null)
                for (var index = 0; index < slots.Length; index++)
                {
                    keys.Add(F2SlotKey(slots[index]));
                    types.Add(slots[index] == null ? "" : slots[index].GetType().Name);
                }
            return new JObject { ["keys"] = keys, ["types"] = types };
        }

        private List<int> F2Indices(BlueprintAbility blueprint)
        {
            var result = new List<int>();
            var slots = rider.UISettings?.Slots;
            if (slots == null || blueprint == null) return result;
            for (var index = 0; index < slots.Length; index++)
                if (ReferenceEquals((slots[index] as MechanicActionBarSlotAbility)?.Ability?.Blueprint, blueprint)) result.Add(index);
            return result;
        }

        private int F2FirstEmpty()
        {
            var slots = rider.UISettings?.Slots;
            if (slots == null) return -1;
            for (var index = 0; index < slots.Length; index++)
                if (F2SlotKey(slots[index]) == "") return index;
            return -1;
        }

        private JObject F2Counters() => new JObject {
            ["initialPlacement"] = nativeControls.InitialPlacementCount, ["initialPlacementFullBar"] = nativeControls.InitialPlacementFullBarCount,
            ["legacySlotRepair"] = nativeControls.LegacySlotRepairCount, ["publicControlRefused"] = nativeControls.PublicControlCommandRefusedCount,
            ["mountedChargeAdmitted"] = combat.MountedChargeAdmittedCount, ["mountedChargeRefused"] = combat.MountedChargeRefusedCount,
            ["nativeCastRequests"] = nativeControls.NativeCastRequestCount, ["nativeRefusals"] = nativeControls.NativeRefusalCount,
            ["placementRecorded"] = nativeControls.IsPlacementRecorded(rider) };

        private JObject F2State()
        {
            var state = Sprint7State();
            state["relationship"] = relationship.State.ToString();
            state["generation"] = relationship.MountedPairGeneration;
            state["riderInCombat"] = rider.IsInCombat;
            state["controlIndices"] = new JArray(F2Indices(nativeControls.MountStateAbility));
            state["selectedAbility"] = Game.Instance.SelectedAbilityHandler?.Ability?.Blueprint?.AssetGuid;
            state["counters"] = F2Counters();
            return state;
        }

        private static JArray F2Commands(UnitEntityData unit) =>
            new JArray(unit.Commands.Raw.Where(command => command != null).Select(command => F2Command(command, false))
                .Concat(unit.Commands.Queue.Where(command => command != null).Select(command => F2Command(command, true))));

        private static JObject F2Command(UnitCommand command, bool queued) => new JObject { ["type"] = command.GetType().Name,
            ["blueprint"] = (command as UnitUseAbility)?.Spell?.Blueprint?.AssetGuid, ["queued"] = queued, ["started"] = command.IsStarted,
            ["finished"] = command.IsFinished, ["result"] = command.Result.ToString(), ["createdByPlayer"] = command.CreatedByPlayer, ["ai"] = command.AiAction != null };

        private static UnitUseAbility F2FindCommand(UnitEntityData unit, BlueprintAbility blueprint) =>
            unit.Commands.Raw.OfType<UnitUseAbility>().Concat(unit.Commands.Queue.OfType<UnitUseAbility>())
                .FirstOrDefault(command => ReferenceEquals(command.Spell?.Blueprint, blueprint));

        private static bool F2Holds(UnitEntityData unit, UnitCommand command) =>
            unit.Commands.Raw.Any(item => ReferenceEquals(item, command)) || unit.Commands.Queue.Any(item => ReferenceEquals(item, command));

        private Kingmaker.UnitLogic.Abilities.Ability F2StockCharge() =>
            rider.Descriptor.Abilities.Enumerable.FirstOrDefault(fact => fact.Active && MountedChargeSafetyService.IsExactNativeCharge(fact.Data));

        private bool F2PairIdle() => rider.Commands.Empty && horse.Commands.Empty && !rider.AreHandsBusyWithAnimation;

        private bool F2RiderChargeRule(JToken rule) => (string)rule["attacker"] == rider.UniqueId && (string)rule["target"] == target?.UniqueId && (bool?)rule["charge"] == true;

        private int F2RiderChargeRules() => s7Rules.Attacks.Skip(s7RulesAtClick).Count(F2RiderChargeRule);

        // The native toolbar the screenshot shows: its selected unit and each main-bar slot's content. Read only.
        private JObject F2ActionBarUi()
        {
            var result = new JObject();
            try
            {
                const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
                var manager = ActionBarManager.Instance;
                result["manager"] = manager != null;
                if (manager == null) return result;
                result["managerActive"] = manager.gameObject.activeInHierarchy;
                result["selectedIsRider"] = ReferenceEquals(typeof(ActionBarManager).GetField("m_Selected", flags)?.GetValue(manager), rider);
                var main = manager.Slots;
                var list = main == null ? null : typeof(ActionBarSlots).GetField("m_Slots", flags)?.GetValue(main) as IEnumerable;
                var shown = new JArray();
                var settingsSlots = rider.UISettings?.Slots;
                if (list != null)
                    foreach (var item in list)
                    {
                        var slot = item as ActionBarSlot;
                        var mechanic = slot == null ? null : slot.MechanicSlot;
                        var settingsIndex = -1;
                        if (mechanic != null && settingsSlots != null)
                            for (var index = 0; index < settingsSlots.Length; index++)
                                if (ReferenceEquals(settingsSlots[index], mechanic)) { settingsIndex = index; break; }
                        shown.Add(new JObject { ["active"] = slot != null && slot.gameObject.activeInHierarchy, ["key"] = mechanic == null ? null : F2SlotKey(mechanic), ["settingsIndex"] = settingsIndex });
                    }
                result["mainBar"] = shown;
            }
            catch (Exception exception) { result["error"] = exception.GetType().Name + ": " + exception.Message; }
            return result;
        }

        // Screenshot WITH the game UI: no camera render, the composited frame read at its end (Kingmaker.Utility.Screenshot
        // with no camera reads the frame buffer), written beside the other fixture visuals for the manifest.
        private void F2Shot(string name)
        {
            var row = new JObject { ["name"] = name, ["requestedFrame"] = Time.frameCount };
            var runner = UnityModManager.UI.Instance;
            if (runner == null) { row["error"] = "The end-of-frame runner is missing."; f2Shots.Add(row); return; }
            // Native SelectUnit clears the pointer mode, which drops a selected ability (run 20261105-finalize-f2-r02 lost
            // its Charge target-selection frame this way). Select the rider only when it is not already the one selection.
            var selection = SelectionManager.Instance.SelectedUnits;
            if (selection == null || selection.Count != 1 || selection[0] != rider)
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            row["selectedAbilityAtRequest"] = Game.Instance.SelectedAbilityHandler?.Ability?.Blueprint?.AssetGuid;
            var generation = ++f2CaptureGeneration;
            f2CaptureActive = true; f2CaptureStartedAt = Time.realtimeSinceStartup;
            runner.StartCoroutine(F2Capture(row, generation));
        }

        private IEnumerator F2Capture(JObject row, int generation)
        {
            yield return new WaitForEndOfFrame();
            try
            {
                row["frame"] = Time.frameCount;
                row["state"] = F2State();
                row["ui"] = F2ActionBarUi();
                var bytes = Screenshot.CapturePNG();
                if (bytes == null || bytes.Length == 0) throw new InvalidOperationException("The frame encoder returned no bytes.");
                var folder = Path.Combine(request.EvidenceRoot, "movement-visuals");
                Directory.CreateDirectory(folder);
                var file = "finalize-f2-" + (string)row["name"] + ".png";
                File.WriteAllBytes(Path.Combine(folder, file), bytes);
                using (var algorithm = SHA256.Create())
                    row["sha256"] = BitConverter.ToString(algorithm.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
                row["file"] = "movement-visuals/" + file; row["bytes"] = bytes.Length; row["width"] = Screen.width; row["height"] = Screen.height;
            }
            catch (Exception exception) { row["error"] = exception.GetType().Name + ": " + exception.Message; }
            f2Shots.Add(row);
            if (generation == f2CaptureGeneration) { f2CaptureActive = false; ResetLeafClock(); }
        }

        // ---- input ------------------------------------------------------------------------------------------------------
        // The native slot entry the toolbar button and its hotkey call. Nothing is forged: the slot is the rider's own.
        private JObject F2SlotClick(string name, int index)
        {
            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            nativeControls.Update();
            var handler = Game.Instance.SelectedAbilityHandler;
            var slots = rider.UISettings?.Slots;
            var slot = slots != null && index >= 0 && index < slots.Length ? slots[index] as MechanicActionBarSlotAbility : null;
            var data = slot?.Ability;
            var castsBefore = nativeControls.NativeCastRequestCount; var refusalsBefore = nativeControls.NativeRefusalCount;
            var record = new JObject { ["index"] = index, ["slotFound"] = slot != null, ["slotAbilityBefore"] = data?.Blueprint?.AssetGuid, ["slotDataId"] = F2Id(data),
                ["possible"] = slot?.IsPossibleActive, ["handlerBefore"] = handler?.Ability?.Blueprint?.AssetGuid, ["before"] = F2State() };
            ((JObject)f2Facts["steps"])[name] = record;
            if (slot == null) return record;
            slot.OnClick();
            record["slotAbilityAfter"] = slot.Ability?.Blueprint?.AssetGuid;
            record["slotDataRestored"] = ReferenceEquals(slot.Ability, data);
            record["slotStillBound"] = ReferenceEquals(rider.UISettings.Slots[index], slot);
            record["handlerAfter"] = handler?.Ability?.Blueprint?.AssetGuid;
            record["handlerCaster"] = handler?.Ability?.Caster?.Unit?.UniqueId;
            record["castDelta"] = nativeControls.NativeCastRequestCount - castsBefore;
            record["refusalDelta"] = nativeControls.NativeRefusalCount - refusalsBefore;
            record["commands"] = F2Commands(rider);
            record["afterClick"] = F2State();
            return record;
        }

        // The native selected-ability click on a unit, as Sprint 7's ordinary click makes it; the handler is dropped after.
        private JObject F2TargetClick(string name, UnitEntityData clicked)
        {
            var handler = Game.Instance.SelectedAbilityHandler;
            var castsBefore = nativeControls.NativeCastRequestCount; var admittedBefore = combat.MountedChargeAdmittedCount;
            var record = new JObject { ["clickedId"] = clicked?.UniqueId, ["handlerAbility"] = handler?.Ability?.Blueprint?.AssetGuid, ["before"] = F2State() };
            ((JObject)f2Facts["steps"])[name] = record;
            var targetObject = clicked?.View?.gameObject;
            var clickedResult = false;
            try { clickedResult = handler?.Ability != null && targetObject != null && handler.OnClick(targetObject, clicked.Position, 0, false, false); }
            finally { if (handler?.Ability != null) handler.DropAbility(); }
            record["clicked"] = clickedResult;
            record["castDelta"] = nativeControls.NativeCastRequestCount - castsBefore;
            record["admittedDelta"] = combat.MountedChargeAdmittedCount - admittedBefore;
            record["commands"] = F2Commands(rider);
            record["afterClick"] = F2State();
            return record;
        }

        // Mount: the slot selects the Mount shell, then the companion is clicked. Dismount: the slot runs it at once.
        private void F2RelationshipClick(string name, bool mounting)
        {
            var shell = mounting ? nativeControls.MountAbility : nativeControls.DismountAbility;
            var stateBefore = relationship.State;
            var click = F2SlotClick(name + "Click", f2ControlIndex);
            var handler = Game.Instance.SelectedAbilityHandler;
            click["handlerIsShell"] = ReferenceEquals(handler?.Ability?.Blueprint, shell);
            if (mounting && ReferenceEquals(handler?.Ability?.Blueprint, shell)) F2TargetClick(name + "Target", horse);
            f2Shell = F2FindCommand(rider, shell);
            var command = new JObject { ["found"] = f2Shell != null, ["blueprint"] = f2Shell?.Spell?.Blueprint?.AssetGuid, ["targetId"] = f2Shell?.Target?.Unit?.UniqueId,
                ["createdByPlayer"] = f2Shell?.CreatedByPlayer, ["fallback"] = false };
            ((JObject)f2Facts["steps"])[name + "Command"] = command;
            if (f2Shell != null || relationship.State != stateBefore) return;
            // Disclosed fallback so the later rows still run: the hidden shell through the existing native ability-click
            // helper. The row records it and cannot pass.
            f2Facts["fallbackUsed"] = true; command["fallback"] = true;
            F2Disclose("Fallback: " + name + " used the hidden shell through the native ability-click helper because the public slot produced no command.");
            if (handler?.Ability != null) handler.DropAbility();
            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            command["fallbackClicked"] = TryNativeAbilityTargetClick(shell, mounting ? horse : rider, "f2-fallback-" + name);
            f2Shell = F2FindCommand(rider, shell);
        }

        private void F2Transition(string name) =>
            ((JObject)f2Facts["steps"])[name] = new JObject { ["state"] = F2State(), ["shellBlueprint"] = f2Shell?.Spell?.Blueprint?.AssetGuid,
                ["shellFinished"] = f2Shell?.IsFinished, ["shellResult"] = f2Shell?.Result.ToString() };

        private void F2BeginWatch()
        {
            f2WatchFrames = f2WatchAbsent = f2WatchFactReplaced = f2WatchCountNotOne = f2WatchDataNotFact = f2WatchIndexMoved = 0;
            f2WatchAnomalies = new JArray(); f2Watching = true;
        }

        // Every frame of the cycle: the public control stays in its slot, bound to the same live fact.
        private void F2WatchControl()
        {
            var fact = rider.Descriptor.Abilities.GetAbility(nativeControls.MountStateAbility);
            var slots = rider.UISettings?.Slots;
            var bound = slots != null && f2ControlIndex >= 0 && f2ControlIndex < slots.Length ? (slots[f2ControlIndex] as MechanicActionBarSlotAbility)?.Ability : null;
            var indices = F2Indices(nativeControls.MountStateAbility);
            var absent = fact == null || !fact.Active || !ReferenceEquals(bound?.Blueprint, nativeControls.MountStateAbility);
            var replaced = !ReferenceEquals(fact, f2ControlFact);
            var dataNotFact = bound != null && fact != null && !ReferenceEquals(bound, fact.Data);
            var moved = indices.Count != 1 || indices[0] != f2ControlIndex;
            f2WatchFrames++;
            if (absent) f2WatchAbsent++;
            if (replaced) f2WatchFactReplaced++;
            if (indices.Count != 1) f2WatchCountNotOne++;
            if (dataNotFact) f2WatchDataNotFact++;
            if (moved) f2WatchIndexMoved++;
            if ((absent || replaced || dataNotFact || moved) && f2WatchAnomalies.Count < 8)
                f2WatchAnomalies.Add(new JObject { ["frame"] = Time.frameCount, ["absent"] = absent, ["replaced"] = replaced, ["dataNotFact"] = dataNotFact,
                    ["indices"] = new JArray(indices), ["relationship"] = relationship.State.ToString() });
        }

        private void F2EndWatch()
        {
            if (!f2Watching) return;
            f2Watching = false;
            f2Facts["frameWatch"] = new JObject { ["index"] = f2ControlIndex, ["frames"] = f2WatchFrames, ["absentFrames"] = f2WatchAbsent, ["factReplacedFrames"] = f2WatchFactReplaced,
                ["countNotOneFrames"] = f2WatchCountNotOne, ["slotDataNotFactFrames"] = f2WatchDataNotFact, ["indexMovedFrames"] = f2WatchIndexMoved, ["anomalies"] = f2WatchAnomalies };
        }

        private void F2BeginStockWatch() { f2StockFrames = 0; f2StockWatched = 0; f2StockWatching = true; F2WatchStockCharge(); }

        // No stock Charge command ever reaches the mounted rider's own commands during the mounted Charge.
        private void F2WatchStockCharge()
        {
            f2StockWatched++;
            if (F2FindCommand(rider, f2StockBlueprint) != null) f2StockFrames++;
        }

        private void F2EndStockWatch()
        {
            if (!f2StockWatching) return;
            f2StockWatching = false;
            f2Facts["stockChargeCommandFrames"] = f2StockFrames; f2Facts["stockChargeWatchedFrames"] = f2StockWatched;
        }

        private JObject F2ChargeProbe(string name, UnitEntityData from)
        {
            var stock = F2StockCharge()?.Data;
            var availability = combat.ObserveMountedChargeTarget(rider, target);
            var record = new JObject { ["available"] = stock?.IsAvailableForCast, ["canTarget"] = stock?.CanTarget(target), ["reason"] = stock?.GetUnavailableReason(),
                ["policyAllowed"] = availability.IsAllowed, ["policyReason"] = availability.Reason, ["policyCode"] = availability.RejectionCode?.ToString(),
                ["distance"] = HorizontalDistance(from.Position, target.Position), ["state"] = F2State() };
            ((JObject)f2Facts["steps"])[name] = record;
            return record;
        }

        // Disclosed setup: a walkable point the rider reaches in a straight navmesh line, where the stock Charge can target.
        private bool F2PlaceForStockCharge(AbilityData stock, out JObject place)
        {
            place = null;
            var origin = rider.Position;
            foreach (var radius in Finalize2StockRadii)
                for (var angle = 0; angle < 360; angle += 15)
                {
                    var wanted = origin + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * radius;
                    var point = ObstacleAnalyzer.GetNearestNode(wanted).clampedPosition;
                    if ((point.To2D() - wanted.To2D()).sqrMagnitude > 0.09f || ObstacleAnalyzer.TraceAlongNavmesh(origin, point) != point) continue;
                    target.Translocate(point, null);
                    if (stock.CanTarget(target) && target.IsVisibleForPlayer)
                    {
                        place = new JObject { ["point"] = CapturePosition(point), ["radius"] = radius, ["angle"] = angle, ["distance"] = HorizontalDistance(origin, point) };
                        return true;
                    }
                }
            return false;
        }

        // ---- F2-placement: the one automatic placement, read from the native bar ------------------------------------------
        private void TickF2Placement()
        {
            // The placement belongs to the load; the row waits a bounded moment for it to have been recorded.
            if (!nativeControls.IsPlacementRecorded(rider) && ++f2Frames < 120) return;
            var slots = rider.UISettings?.Slots;
            if (f2Step == 0)
            {
                // Phase 1: what the load's own placement did. The Working fixture rider's main bar is full, so the native-route
                // outcome is "nothing placed, nothing overwritten". Recorded, then one slot is freed for a first placement.
                var first = nativeControls.InitialPlacements.Where(placement => placement.UnitId == rider.UniqueId).ToArray();
                var mainNow = new JArray();
                for (var index = 0; index < MountedControlSlotPolicy.MainBarSlotCount; index++)
                    mainNow.Add(slots != null && index < slots.Length ? F2SlotKey(slots[index]) : null);
                f2Facts["loadObservation"] = new JObject { ["placements"] = new JArray(first.Select(placement => new JObject { ["index"] = placement.Index,
                        ["frame"] = placement.Frame, ["mainBarBefore"] = new JArray(placement.MainBarBefore ?? new string[0]) })),
                    ["controlIndices"] = new JArray(F2Indices(nativeControls.MountStateAbility)), ["mainBarNow"] = mainNow,
                    ["fullBarCount"] = nativeControls.InitialPlacementFullBarCount, ["placedCount"] = nativeControls.InitialPlacementCount };
                if (first.Length == 1 && first[0].Index < 0 && slots != null && slots.Length > MountedControlSlotPolicy.MainBarSlotCount - 1)
                {
                    var freed = MountedControlSlotPolicy.MainBarSlotCount - 1;
                    f2Facts["freedSlot"] = freed;
                    f2Facts["freedSlotHeld"] = F2SlotKey(slots[freed]);
                    rider.UISettings.SetSlot(new MechanicActionBarSlotEmpty { Unit = rider }, freed);
                    F2Disclose("The full main bar's last slot (" + freed + ") was emptied, as a player removes an ability from it.");
                    nativeControls.BeginLoadedWorld();
                    F2Disclose("The placement records were forgotten as a newly loaded world's are, so the eligible rider gets its first placement.");
                    f2Step = 1; f2Frames = 0;
                    return;
                }
                f2Facts["freedSlot"] = null;
            }
            var fact = rider.Descriptor.Abilities.GetAbility(nativeControls.MountStateAbility);
            var indices = F2Indices(nativeControls.MountStateAbility);
            var main = new JArray();
            for (var index = 0; index < MountedControlSlotPolicy.MainBarSlotCount; index++)
                main.Add(slots != null && index < slots.Length ? F2SlotKey(slots[index]) : null);
            f2Facts["autofill"] = Kingmaker.UI.SettingsUI.SettingsRoot.Instance?.AutofillActionbarSlots?.CurrentValue;
            f2Facts["placements"] = new JArray(nativeControls.InitialPlacements.Select(placement => new JObject { ["unitId"] = placement.UnitId, ["index"] = placement.Index,
                ["frame"] = placement.Frame, ["mainBarBefore"] = new JArray(placement.MainBarBefore ?? new string[0]) }));
            f2Facts["controlIndices"] = new JArray(indices);
            f2Facts["mainBarNow"] = main;
            f2Facts["bar"] = F2Bar();
            f2Facts["factPresent"] = fact != null;
            f2Facts["factActive"] = fact != null && fact.Active;
            f2Facts["slotHoldsLiveFact"] = indices.Count == 1 && fact != null && ReferenceEquals((slots[indices[0]] as MechanicActionBarSlotAbility)?.Ability, fact.Data);
            f2Facts["placementRecorded"] = nativeControls.IsPlacementRecorded(rider);
            f2Facts["placementSettled"] = nativeControls.PlacementSettled == null ? (bool?)null : nativeControls.PlacementSettled();
            f2Facts["countersAtRow"] = F2Counters();
            f2Facts["waitFrames"] = f2Frames;
            f2Facts["rider"] = new JObject { ["id"] = rider.UniqueId, ["name"] = rider.CharacterName, ["level"] = rider.Descriptor.Progression.CharacterLevel };
            FinishF2Row(true, "Placement observations recorded.");
        }

        // ---- F2-strings: the owned keys and the control names -------------------------------------------------------------
        private void TickF2Strings()
        {
            var pack = LocalizationManager.CurrentPack;
            // Diagnostic read of the service's own key table, so a key added later is checked without a second list.
            var table = typeof(NativeMountedControlService).GetField("localization", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(nativeControls) as Dictionary<string, string>;
            var keys = new JArray();
            if (table != null)
                foreach (var entry in table.OrderBy(value => value.Key, StringComparer.Ordinal))
                {
                    string actual = null;
                    var present = pack?.Strings != null && pack.Strings.TryGetValue(entry.Key, out actual);
                    keys.Add(new JObject { ["key"] = entry.Key, ["expected"] = entry.Value, ["present"] = present, ["actual"] = actual,
                        ["text"] = pack == null ? null : pack.GetText(entry.Key, false) });
                }
            f2Facts["source"] = "NativeMountedControlService localization table (diagnostic reflection, read only)";
            f2Facts["tableFound"] = table != null;
            f2Facts["packPresent"] = pack != null;
            f2Facts["locale"] = pack == null ? null : pack.Locale.ToString();
            f2Facts["keys"] = keys;
            var controls = new JArray();
            F2Control(controls, "MountState", nativeControls.MountStateAbility);
            F2Control(controls, "MountCompanion", nativeControls.MountAbility);
            F2Control(controls, "Dismount", nativeControls.DismountAbility);
            F2Control(controls, "MountedCharge", nativeControls.MountedChargeAbility);
            F2Control(controls, "RiderPrimary", nativeControls.RiderPrimaryAbility);
            F2Control(controls, "MountPrimary", nativeControls.MountPrimaryAbility);
            f2Facts["controls"] = controls;
            FinishF2Row(true, "String observations recorded.");
        }

        private static void F2Control(JArray list, string kind, BlueprintAbility blueprint) =>
            list.Add(new JObject { ["kind"] = kind, ["guid"] = blueprint?.AssetGuid, ["name"] = blueprint?.Name, ["description"] = blueprint?.Description,
                ["hidden"] = blueprint == null ? (bool?)null : blueprint.Hidden });

        // ---- F2-mount-cycle: Mount, Dismount, Mount from the same public slot ---------------------------------------------
        private void TickF2MountCycle()
        {
            switch (f2Step)
            {
                case 0:
                {
                    if (relationship.State != RelationshipState.Unmounted || !F2PairIdle()) return;
                    var indices = F2Indices(nativeControls.MountStateAbility);
                    var fact = rider.Descriptor.Abilities.GetAbility(nativeControls.MountStateAbility);
                    f2Facts["controlPlacedBySetup"] = false;
                    var empty = F2FirstEmpty();
                    if (indices.Count == 0 && fact != null && fact.Active && empty >= 0 && empty < MountedControlSlotPolicy.MainBarSlotCount)
                    {
                        // Disclosed setup only when no automatic placement exists (F2-placement has then already failed):
                        // the player's own drag of the live control onto the bar, so the later rows still run.
                        rider.UISettings.SetSlot(new MechanicActionBarSlotAbility { Unit = rider, Ability = fact.Data }, empty);
                        F2Disclose("No public control was on the bar: the live control was placed in empty slot " + empty + " as a player places it.");
                        f2Facts["controlPlacedBySetup"] = true;
                        indices = F2Indices(nativeControls.MountStateAbility);
                    }
                    f2Facts["start"] = new JObject { ["indices"] = new JArray(indices), ["index"] = indices.Count == 1 ? indices[0] : -1,
                        ["factId"] = F2Id(fact), ["dataId"] = F2Id(fact?.Data), ["state"] = F2State() };
                    f2Facts["fallbackUsed"] = false;
                    if (indices.Count != 1 || fact == null) { F2Fail("The rider has no single public control slot to click."); return; }
                    f2ControlIndex = indices[0]; f2ControlFact = fact;
                    F2BeginWatch();
                    F2Shot(Finalize2ShotUnmounted);
                    f2Step = 1; return;
                }
                case 1:
                    F2RelationshipClick("mount1", true); f2Step = 2; return;
                case 2:
                    if (relationship.State != RelationshipState.Mounted || !F2PairIdle() || f2Shell != null && !f2Shell.IsFinished) return;
                    F2Transition("mounted1");
                    F2Shot(Finalize2ShotMounted);
                    f2Step = 3; return;
                case 3:
                    if (++f2Frames < 10) return;
                    F2RelationshipClick("dismount", false); f2Step = 4; return;
                case 4:
                    if (relationship.State != RelationshipState.Unmounted || !F2PairIdle() || f2Shell != null && !f2Shell.IsFinished) return;
                    F2Transition("dismounted"); f2Step = 5; return;
                case 5:
                    if (++f2Frames < 10) return;
                    F2RelationshipClick("mount2", true); f2Step = 6; return;
                case 6:
                    if (relationship.State != RelationshipState.Mounted || !F2PairIdle() || f2Shell != null && !f2Shell.IsFinished) return;
                    F2Transition("mounted2");
                    FinishF2Row(true, "Mount-cycle observations recorded.");
                    return;
            }
        }

        // ---- F2-alias-and-removal: a removed control stays removed; exact old bindings are repaired in place --------------
        private void TickF2AliasAndRemoval()
        {
            switch (f2Step)
            {
                case 0:
                {
                    if (relationship.State != RelationshipState.Mounted || !F2PairIdle()) return;
                    var indices = F2Indices(nativeControls.MountStateAbility);
                    var removal = new JObject { ["index"] = -1, ["indicesBefore"] = new JArray(indices), ["countersBefore"] = F2Counters(), ["barBefore"] = F2Bar() };
                    f2Facts["removal"] = removal;
                    if (indices.Count != 1) { F2Fail("The rider has no single public control to remove."); return; }
                    rider.UISettings.SetSlot(new MechanicActionBarSlotEmpty { Unit = rider }, indices[0]);
                    F2Disclose("The public control was removed from slot " + indices[0] + " (UnitUISettings.SetSlot with an empty slot), as a player removes it.");
                    removal["index"] = indices[0]; removal["frame"] = Time.frameCount; removal["barAfterRemoval"] = F2Bar();
                    f2Step = 1; return;
                }
                case 1:
                {
                    if (++f2Frames < 60) return;
                    f2Facts["after60"] = new JObject { ["frames"] = f2Frames, ["indices"] = new JArray(F2Indices(nativeControls.MountStateAbility)), ["counters"] = F2Counters() };
                    SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                    F2Disclose("Dismount through the hidden Dismount shell with the existing native ability-click helper (removed public control).");
                    var clicked = TryNativeAbilityTargetClick(nativeControls.DismountAbility, rider, "f2-removal-dismount");
                    f2Facts["removalDismountClicked"] = clicked;
                    if (!clicked) { F2Fail("The removal cycle's Dismount click was refused."); return; }
                    f2Step = 2; return;
                }
                case 2:
                {
                    if (relationship.State != RelationshipState.Unmounted || !F2PairIdle()) return;
                    f2Facts["removalDismounted"] = F2State();
                    SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                    F2Disclose("Mount through the hidden Mount shell with the existing native ability-click helper (removed public control).");
                    var clicked = TryNativeAbilityTargetClick(nativeControls.MountAbility, horse, "f2-removal-mount");
                    f2Facts["removalMountClicked"] = clicked;
                    if (!clicked) { F2Fail("The removal cycle's Mount click was refused."); return; }
                    f2Step = 3; return;
                }
                case 3:
                    if (relationship.State != RelationshipState.Mounted || !F2PairIdle()) return;
                    f2Facts["removalMounted"] = F2State(); f2Step = 4; return;
                case 4:
                    if (++f2Frames < 30) return;
                    f2Facts["removalResult"] = new JObject { ["indices"] = new JArray(F2Indices(nativeControls.MountStateAbility)), ["counters"] = F2Counters(),
                        ["placementRecorded"] = nativeControls.IsPlacementRecorded(rider), ["bar"] = F2Bar() };
                    f2Step = 5; return;
                case 5:
                    F2InjectOldBindings(); return;
                case 6:
                    if (++f2Frames < 10) return;
                    F2RecordRepair();
                    FinishF2Row(true, "Removal and old-binding observations recorded.");
                    return;
            }
        }

        // Disclosed setup: the exact leased facts an older build kept on its visible bar, put in two empty slots.
        private void F2InjectOldBindings()
        {
            var slots = rider.UISettings.Slots;
            var empty = new List<int>();
            for (var index = 0; index < slots.Length && empty.Count < 2; index++)
                if (F2SlotKey(slots[index]) == "") empty.Add(index);
            var relationshipShell = rider.Descriptor.Abilities.GetAbility(nativeControls.DismountAbility);
            if (relationshipShell == null || !relationshipShell.Active) relationshipShell = rider.Descriptor.Abilities.GetAbility(nativeControls.MountAbility);
            var chargeShell = rider.Descriptor.Abilities.GetAbility(nativeControls.MountedChargeAbility);
            var stock = F2StockCharge();
            var injection = new JObject { ["k1"] = -1, ["k2"] = -1, ["emptyFound"] = new JArray(empty),
                ["outsideMainBar"] = empty.Any(index => index >= MountedControlSlotPolicy.MainBarSlotCount),
                ["k1Guid"] = relationshipShell?.Blueprint?.AssetGuid, ["k1Active"] = relationshipShell != null && relationshipShell.Active,
                ["k2Guid"] = chargeShell?.Blueprint?.AssetGuid, ["k2Active"] = chargeShell != null && chargeShell.Active,
                ["stockChargePresent"] = stock != null, ["stockChargeOnBarBefore"] = stock != null && F2Indices(stock.Blueprint).Count > 0,
                ["barBefore"] = F2Bar(), ["countersBefore"] = F2Counters(), ["state"] = F2State() };
            f2Facts["injection"] = injection;
            if (empty.Count < 2 || relationshipShell == null || !relationshipShell.Active || chargeShell == null || !chargeShell.Active)
            { F2Fail("The old-binding injection has no two empty slots or no leased shell facts."); return; }
            f2K1 = empty[0]; f2K2 = empty[1];
            rider.UISettings.SetSlot(new MechanicActionBarSlotAbility { Unit = rider, Ability = relationshipShell.Data }, f2K1);
            rider.UISettings.SetSlot(new MechanicActionBarSlotAbility { Unit = rider, Ability = chargeShell.Data }, f2K2);
            F2Disclose("Old-build bindings injected: the leased " + relationshipShell.Blueprint.name + " fact at slot " + f2K1 +
                " and the leased Mounted Charge shell fact at slot " + f2K2 + ".");
            injection["k1"] = f2K1; injection["k2"] = f2K2; injection["frame"] = Time.frameCount;
            f2Frames = 0; f2Step = 6;
        }

        private void F2RecordRepair()
        {
            var slots = rider.UISettings.Slots;
            var publicFact = rider.Descriptor.Abilities.GetAbility(nativeControls.MountStateAbility);
            var stock = F2StockCharge();
            var k1 = (slots[f2K1] as MechanicActionBarSlotAbility)?.Ability;
            var k2 = (slots[f2K2] as MechanicActionBarSlotAbility)?.Ability;
            f2Facts["repaired"] = new JObject { ["frames"] = f2Frames, ["bar"] = F2Bar(), ["k1Key"] = F2SlotKey(slots[f2K1]), ["k2Key"] = F2SlotKey(slots[f2K2]),
                ["k1IsLiveControl"] = publicFact != null && ReferenceEquals(k1, publicFact.Data), ["k2IsLiveStock"] = stock != null && ReferenceEquals(k2, stock.Data),
                ["controlIndices"] = new JArray(F2Indices(nativeControls.MountStateAbility)), ["countersAfter"] = F2Counters() };
        }

        // ---- F2-charge-mounted: the ordinary Charge slot carries the mounted Charge ---------------------------------------
        private void TickF2ChargeMounted()
        {
            switch (f2Step)
            {
                case 0:
                {
                    if (relationship.State != RelationshipState.Mounted || !F2PairIdle()) return;
                    var stock = F2StockCharge();
                    if (stock == null) { F2Fail("The rider has no exact stock Charge."); return; }
                    f2StockBlueprint = stock.Blueprint;
                    var indices = F2Indices(stock.Blueprint);
                    var preexisting = ((JArray)f2Measure["startBar"]["keys"]).Any(key => (string)key == MountedChargeSafetyPolicy.ChargeBlueprintId);
                    string source;
                    if (indices.Count > 0) { f2ChargeIndex = indices[0]; source = preexisting ? "pre-existing" : "repaired-old-binding"; }
                    else
                    {
                        f2ChargeIndex = F2FirstEmpty();
                        if (f2ChargeIndex < 0) { F2Fail("No empty slot for the stock Charge."); return; }
                        rider.UISettings.SetSlot(new MechanicActionBarSlotAbility { Unit = rider, Ability = stock.Data }, f2ChargeIndex);
                        F2Disclose("The rider's stock Charge fact was placed in empty slot " + f2ChargeIndex + ".");
                        source = "placed-setup";
                    }
                    f2Facts["chargeSlot"] = new JObject { ["index"] = f2ChargeIndex, ["source"] = source, ["preexistingAtScenarioStart"] = preexisting,
                        ["indicesBefore"] = new JArray(indices), ["sameData"] = ReferenceEquals((rider.UISettings.Slots[f2ChargeIndex] as MechanicActionBarSlotAbility)?.Ability, stock.Data) };
                    BeginTarget(10f, "f2-charge"); ruleProbe.Arm(target, false); s7Rules.Watch(target);
                    F2Disclose("A hostile was spawned 10 m away (BeginTarget) to start real-time combat.");
                    f2Step = 1; return;
                }
                case 1:
                    if (!rider.IsInCombat || !horse.IsInCombat || !rider.CombatState.CanActInCombat || !target.IsVisibleForPlayer) return;
                    f2Facts["combatEntered"] = F2State(); f2Step = 2; return;
                // Too near: the ordinary Charge's reason is the player's, never an internal identity.
                case 2:
                    if (!Sprint7StandardFree()) return;
                    target.Translocate(FindWalkablePoint(horse.Position, 3f, .75f), null);
                    F2Disclose("The hostile was translocated about 3 m from the mount (too near).");
                    F2ChargeProbe("tooNear", horse); f2Step = 3; return;
                // The target-selection frame, captured with the toolbar. The real charge clicks again below.
                case 3:
                {
                    if (!Sprint7StandardFree() || !Sprint7PlaceOnLane(Finalize2LaneRadii)) return;
                    var availability = combat.ObserveMountedChargeTarget(rider, target);
                    f2Facts["lastAvailability"] = availability.Reason;
                    if (!availability.IsAllowed) return;
                    F2Disclose("The hostile was placed on a clear lane (MountedChargeFixture.TryPlaceOnLane) for the target-selection frame.");
                    F2SlotClick("selectionClick", f2ChargeIndex);
                    F2Shot(Finalize2ShotSelection);
                    f2Step = 4; return;
                }
                case 4:
                {
                    var handler = Game.Instance.SelectedAbilityHandler;
                    if (handler?.Ability != null) handler.DropAbility();
                    Sprint7Step("afterSelectionDrop"); f2Step = 5; return;
                }
                // Placement and click in one tick: the live brain walks the pair toward any target left waiting.
                case 5:
                {
                    if (!Sprint7StandardFree() || !Sprint7PlaceOnLane(Finalize2LaneRadii)) return;
                    var availability = combat.ObserveMountedChargeTarget(rider, target);
                    f2Facts["lastAvailability"] = availability.Reason;
                    if (!availability.IsAllowed) return;
                    F2Disclose("The hostile was placed on a clear lane for the charge, in the same tick as the click.");
                    Sprint7ResetTravel();
                    F2ChargeProbe("chargeProbe", horse);
                    F2SlotClick("chargeClick", f2ChargeIndex);
                    var handler = Game.Instance.SelectedAbilityHandler;
                    if (!ReferenceEquals(handler?.Ability?.Blueprint, nativeControls.MountedChargeAbility))
                    {
                        if (handler?.Ability != null) handler.DropAbility();
                        F2Fail("The ordinary Charge slot did not select the mounted Charge."); return;
                    }
                    var click = F2TargetClick("chargeTarget", target);
                    click["shell"] = combat.OwnedChargeShell != null; click["shellTarget"] = combat.OwnedChargeShell?.Target?.Unit?.UniqueId;
                    F2BeginStockWatch();
                    if (click.Value<bool>("clicked") != true) { F2Fail("The ordinary Charge target click was refused: " + combat.LastMountedChargeRefusal); return; }
                    f2Step = 6; return;
                }
                case 6:
                    Sprint7ObserveTravel();
                    if (!Sprint7ChargeTerminal()) return;
                    Sprint7RecordTerminal("terminal");
                    f2Facts["chargeRules"] = F2RiderChargeRules();
                    {
                        // A caster-level refusal while the rider's Standard is spent, read on the ordinary Charge itself.
                        var spent = F2StockCharge()?.Data;
                        f2Facts["spentRefusal"] = new JObject { ["riderStandard"] = rider.CombatState.Cooldown.StandardAction,
                            ["available"] = spent?.IsAvailableForCast, ["reason"] = spent?.GetUnavailableReason() };
                    }
                    FinishF2Row(true, "Mounted ordinary Charge observations recorded.");
                    return;
            }
        }

        // ---- F2-refusals: direct commands of the public control and of the mounted rider's stock Charge -------------------
        private void TickF2Refusals()
        {
            switch (f2Step)
            {
                case 0:
                {
                    var free = Sprint7StandardFree();
                    if (!free && Time.time - f2BoundAt < Finalize2StandardWaitSeconds) return;
                    f2Facts["standardFreeAtStart"] = free;
                    var publicFact = rider.Descriptor.Abilities.GetAbility(nativeControls.MountStateAbility);
                    var stock = F2StockCharge();
                    if (publicFact == null || stock == null) { F2Fail("The refusal probes have no public control or stock Charge fact."); return; }
                    F2Disclose("Refusal probes: direct UnitUseAbility.CreateCastCommand commands of the public control and of the stock Charge were Run on the rider.");
                    var refusedBefore = nativeControls.PublicControlCommandRefusedCount;
                    var before = F2State();
                    var command = UnitUseAbility.CreateCastCommand(publicFact.Data, rider);
                    rider.Commands.Run(command);
                    f2Facts["publicCommand"] = new JObject { ["before"] = before, ["after"] = F2State(), ["refusedDelta"] = nativeControls.PublicControlCommandRefusedCount - refusedBefore,
                        ["started"] = command.IsStarted, ["finished"] = command.IsFinished, ["inCommands"] = F2Holds(rider, command) };
                    var warningsBefore = f2Warnings.Count; var feedbackBefore = combat.LastFeedback; var admittedBefore = combat.MountedChargeAdmittedCount;
                    var stockBefore = F2State();
                    var stockCommand = UnitUseAbility.CreateCastCommand(stock.Data, target);
                    rider.Commands.Run(stockCommand);
                    f2Facts["stockCommand"] = new JObject { ["before"] = stockBefore, ["after"] = F2State(), ["feedbackBefore"] = feedbackBefore, ["feedbackAfter"] = combat.LastFeedback,
                        ["warningDelta"] = f2Warnings.Count - warningsBefore, ["admittedDelta"] = combat.MountedChargeAdmittedCount - admittedBefore,
                        ["started"] = stockCommand.IsStarted, ["finished"] = stockCommand.IsFinished, ["inCommands"] = F2Holds(rider, stockCommand) };
                    f2Step = 1; return;
                }
                case 1:
                    if (++f2Frames < 10) return;
                    f2Facts["settled"] = F2State();
                    f2Facts["warnings"] = f2Warnings.Capture();
                    FinishF2Row(true, "Refusal observations recorded.");
                    return;
            }
        }

        // ---- F2-charge-unmounted: Dismount from the public slot in combat, then the untouched native Charge --------------
        private void TickF2ChargeUnmounted()
        {
            switch (f2Step)
            {
                case 0:
                {
                    var indices = F2Indices(nativeControls.MountStateAbility);
                    if (indices.Count != 1) { F2Fail("The rider has no single public control slot for the combat Dismount."); return; }
                    if (relationship.State != RelationshipState.Mounted) { F2Fail("The pair is not mounted before the combat Dismount."); return; }
                    if (rider.CombatState.HasCooldownForCommand(UnitCommand.CommandType.Move) || rider.AreHandsBusyWithAnimation) return;
                    f2ControlIndex = indices[0]; f2PeakMove = rider.CombatState.Cooldown.MoveAction;
                    var click = F2SlotClick("dismountClick", f2ControlIndex);
                    var handler = Game.Instance.SelectedAbilityHandler;
                    click["handlerIsShell"] = ReferenceEquals(handler?.Ability?.Blueprint, nativeControls.DismountAbility);
                    f2Shell = F2FindCommand(rider, nativeControls.DismountAbility);
                    click["shellCommand"] = f2Shell?.Spell?.Blueprint?.AssetGuid;
                    f2Attempts++;
                    var log = (JArray)f2Facts["dismountAttemptLog"];
                    if (log == null) { log = new JArray(); f2Facts["dismountAttemptLog"] = log; }
                    log.Add(click.DeepClone());
                    f2Facts["dismountAttempts"] = f2Attempts;
                    if (f2Shell == null && relationship.State == RelationshipState.Mounted)
                    {
                        if (f2Attempts >= Finalize2DismountAttempts) { F2Fail("The public control did not dismount in combat."); return; }
                        f2Step = 10; return;
                    }
                    f2Step = 1; return;
                }
                case 10:
                    if (++f2Frames < 30) return;
                    f2Step = 0; return;
                case 1:
                {
                    var move = rider.CombatState.Cooldown.MoveAction;
                    if (move > f2PeakMove) f2PeakMove = move;
                    if (relationship.State != RelationshipState.Unmounted || f2Shell != null && !f2Shell.IsFinished) return;
                    F2Transition("dismounted");
                    ((JObject)((JObject)f2Facts["steps"])["dismounted"])["peakRiderMove"] = f2PeakMove;
                    f2Step = 2; return;
                }
                case 2:
                {
                    var move = rider.CombatState.Cooldown.MoveAction;
                    if (move > f2PeakMove) { f2PeakMove = move; ((JObject)((JObject)f2Facts["steps"])["dismounted"])["peakRiderMove"] = f2PeakMove; }
                    // The ordinary Charge is a full-round action: both of the rider's actions are free before the click.
                    if (rider.CombatState.HasCooldownForCommand(UnitCommand.CommandType.Standard) ||
                        rider.CombatState.HasCooldownForCommand(UnitCommand.CommandType.Move) || rider.AreHandsBusyWithAnimation) return;
                    var stock = F2StockCharge();
                    if (stock == null) { F2Fail("The rider has no exact stock Charge after the Dismount."); return; }
                    JObject place;
                    if (!F2PlaceForStockCharge(stock.Data, out place)) return;
                    f2Facts["placement"] = place;
                    F2Disclose("The hostile was placed on a clear straight line from the unmounted rider, in the same tick as the click.");
                    Sprint7ResetTravel();
                    f2AdmittedBefore = combat.MountedChargeAdmittedCount;
                    f2Facts["admittedBefore"] = f2AdmittedBefore;
                    F2ChargeProbe("stockProbe", rider);
                    var click = F2SlotClick("stockClick", f2ChargeIndex);
                    var handler = Game.Instance.SelectedAbilityHandler;
                    var selected = handler?.Ability;
                    click["handlerIsStock"] = selected != null && MountedChargeSafetyService.IsExactNativeCharge(selected);
                    click["handlerIsShell"] = ReferenceEquals(selected?.Blueprint, nativeControls.MountedChargeAbility);
                    click["handlerIsSlotData"] = selected != null && ReferenceEquals(selected, (rider.UISettings.Slots[f2ChargeIndex] as MechanicActionBarSlotAbility)?.Ability);
                    if (selected == null || !MountedChargeSafetyService.IsExactNativeCharge(selected))
                    {
                        if (selected != null) handler.DropAbility();
                        F2Fail("The unmounted Charge slot did not select the stock Charge."); return;
                    }
                    var result = F2TargetClick("stockTarget", target);
                    f2StockCommand = F2FindCommand(rider, stock.Blueprint);
                    result["commandFound"] = f2StockCommand != null; result["commandBlueprint"] = f2StockCommand?.Spell?.Blueprint?.AssetGuid;
                    result["commandTarget"] = f2StockCommand?.Target?.Unit?.UniqueId;
                    if (result.Value<bool>("clicked") != true) { F2Fail("The unmounted stock Charge target click was refused."); return; }
                    f2Step = 3; return;
                }
                case 3:
                {
                    if (Sprint7Buff() != null && f2Facts["buffSeen"] == null) f2Facts["buffSeen"] = F2State();
                    if (rider.Descriptor.State.IsCharging && f2Facts["chargingSeen"] == null) f2Facts["chargingSeen"] = F2State();
                    var rule = s7Rules.Attacks.Skip(s7RulesAtClick).FirstOrDefault(F2RiderChargeRule);
                    if (rule != null && f2Facts["chargeRule"] == null) f2Facts["chargeRule"] = rule.DeepClone();
                    var finished = f2StockCommand == null || f2StockCommand.IsFinished;
                    if (!finished || rule == null && ++f2Frames < 90) return;
                    f2Facts["stockResult"] = new JObject { ["finished"] = f2StockCommand?.IsFinished, ["result"] = f2StockCommand?.Result.ToString(),
                        ["admittedDelta"] = combat.MountedChargeAdmittedCount - f2AdmittedBefore, ["chargeRules"] = F2RiderChargeRules(), ["state"] = F2State() };
                    FinishF2Row(true, "Unmounted ordinary Charge observations recorded.");
                    return;
                }
            }
        }
    }
}
