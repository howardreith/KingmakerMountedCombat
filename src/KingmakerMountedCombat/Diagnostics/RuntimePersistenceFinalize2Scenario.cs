using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.Localization;
using Kingmaker.UI.ActionBar;
using Kingmaker.UI.Selection;
using Kingmaker.UI.UnitSettings;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.Utility;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityModManagerNet;

namespace KingmakerMountedCombat.Diagnostics
{
    // F2 on the Sprint 1 UPG-01 chain: the authentic preview.211 Quick carries an old Dismount slot row. The producer
    // records the archive rows against the live slots after the native load, runs the one first placement with the
    // rider selected and reads the visible bar before anything else can rebuild it, cycles Dismount and Mount from that
    // slot, then writes two native Manuals: the control swapped to a chosen main-bar slot, and the control removed. The
    // consumer, a new process, cold-loads the first, reads the restored and drawn slot before any input, cycles it, then
    // loads the second in the same process and watches the removal stay removed. Every setup edit is disclosed; the
    // external reader decides PASS from the recorded facts.
    internal sealed partial class RuntimePersistenceScenario
    {
        private const string F2Contract = "finalize-f2-upgrade-public-control-v1";
        private const int F2SettleFrames = 10, F2VisibleFrames = 30, F2WatchFrames = 30, F2ShotFrames = 120, F2RemovedWatchFrames = 60;
        private const double F2DeadlineSeconds = 300;
        // In real-time combat a Dismount spends the rider's native Move action; the Mount shell is possible again only when it
        // returns (run 20261105-finalize-f2-r04 clicked 10 frames after the dismount, with 2.8 s of Move cooldown left).
        private const double F2ReadySeconds = 15;
        private int f2ReadyFrames;
        private readonly Stopwatch f2ReadyClock = new Stopwatch();
        private JObject f2WaitStart;
        private bool F2Case => request.Finalize2Controls == true;
        private readonly Stopwatch f2Clock = new Stopwatch();
        private int f2Frames, f2Index = -1, f2Legacy = -1, f2Chosen = -1, f2ShotsPending;
        private long f2PlacementsAtWatch;
        private Kingmaker.UnitLogic.Abilities.Ability f2Fact;
        private UnitUseAbility f2Command;
        private JArray f2Disclosed = new JArray(), f2Shots = new JArray(), f2Anomalies = new JArray();
        private int f2Watched, f2Moved, f2NotLive, f2Replaced, f2Reinserted;
        private bool f2WatchRemoved, f2LoadCallback;
        private JArray f2BarBeforeSave;

        private BlueprintAbility F2Public => controls.MountStateAbility;

        private static string F2Key(MechanicActionBarSlot slot) =>
            slot == null || slot is MechanicActionBarSlotEmpty ? "" : (slot as MechanicActionBarSlotAbility)?.Ability?.Blueprint?.AssetGuid ?? slot.GetType().Name;

        private JArray F2Bar(int count)
        {
            var keys = new JArray();
            var slots = rider.UISettings?.Slots;
            for (var index = 0; index < count; index++) keys.Add(slots != null && index < slots.Length ? F2Key(slots[index]) : null);
            return keys;
        }

        private List<int> F2Indices()
        {
            var result = new List<int>();
            var slots = rider?.UISettings?.Slots;
            if (slots == null || F2Public == null) return result;
            for (var index = 0; index < slots.Length; index++)
                if (ReferenceEquals((slots[index] as MechanicActionBarSlotAbility)?.Ability?.Blueprint, F2Public)) result.Add(index);
            return result;
        }

        private Kingmaker.UnitLogic.Abilities.Ability F2PublicFact() => rider?.Descriptor?.Abilities?.GetAbility(F2Public);

        private bool F2HoldsLive(int index)
        {
            var fact = F2PublicFact();
            var slots = rider?.UISettings?.Slots;
            return fact != null && fact.Active && slots != null && index >= 0 && index < slots.Length &&
                ReferenceEquals((slots[index] as MechanicActionBarSlotAbility)?.Ability, fact.Data);
        }

        private bool F2OnlyAt(int index) { var indices = F2Indices(); return indices.Count == 1 && indices[0] == index; }

        private JObject F2Counters() => new JObject {
            ["initialPlacement"] = controls.InitialPlacementCount, ["initialPlacementFullBar"] = controls.InitialPlacementFullBarCount,
            ["initialPlacementObservations"] = controls.InitialPlacements.Count, ["legacySlotRepair"] = controls.LegacySlotRepairCount,
            ["retiredPrimarySlotsCleared"] = controls.RetiredPrimarySlotsCleared, ["publicControlRefused"] = controls.PublicControlCommandRefusedCount,
            ["nativeCastRequests"] = controls.NativeCastRequestCount, ["nativeRefusals"] = controls.NativeRefusalCount };

        private static JArray F2Rows(IEnumerable<SavedMountedSlot> rows) =>
            new JArray((rows ?? new SavedMountedSlot[0]).Select(row => new JObject { ["ActorId"] = row.ActorId, ["Index"] = row.Index, ["Kind"] = row.Kind }));

        // The hidden delivery shells and the old Mounted Charge must never sit in a slot; the stock Charge is the one Charge.
        private JObject F2Exposure()
        {
            var hidden = new[] { controls.MountAbility, controls.DismountAbility, controls.MountedChargeAbility }.Where(b => b != null).ToArray();
            var slots = rider.UISettings?.Slots ?? new MechanicActionBarSlot[0];
            var exposed = new JArray();
            var stock = new JArray();
            for (var index = 0; index < slots.Length; index++)
            {
                var data = (slots[index] as MechanicActionBarSlotAbility)?.Ability;
                if (data == null) continue;
                if (hidden.Any(b => ReferenceEquals(b, data.Blueprint))) exposed.Add(new JObject { ["index"] = index, ["guid"] = data.Blueprint.AssetGuid });
                if (MountedChargeSafetyService.IsExactNativeCharge(data)) stock.Add(index);
            }
            var charge = rider.Descriptor.Abilities.Enumerable.FirstOrDefault(fact => fact.Active && MountedChargeSafetyService.IsExactNativeCharge(fact.Data));
            return new JObject { ["hiddenExposed"] = exposed, ["stockChargeSlots"] = stock, ["stockChargeFact"] = charge?.Blueprint?.AssetGuid,
                ["stockChargeName"] = charge?.Blueprint?.Name, ["hiddenFlags"] = new JArray(hidden.Select(b => b.Hidden)), ["publicHidden"] = F2Public?.Hidden };
        }

        private JObject F2Actor(string label) => new JObject {
            ["label"] = label, ["frame"] = Time.frameCount, ["relationship"] = relationship.State.ToString(),
            ["mounted"] = F2Mounted(), ["placementRecorded"] = controls.IsPlacementRecorded(rider), ["factActive"] = F2PublicFact()?.Active == true,
            ["indices"] = new JArray(F2Indices()), ["bar"] = F2Bar(MountedControlSlotPolicy.MainBarSlotCount + 8),
            ["placements"] = new JArray(controls.InitialPlacements.Where(p => p.UnitId == rider.UniqueId).Select(p =>
                new JObject { ["index"] = p.Index, ["frame"] = p.Frame, ["mainBarBefore"] = new JArray(p.MainBarBefore ?? new string[0]) })),
            ["exposure"] = F2Exposure(), ["counters"] = F2Counters() };

        private bool F2Mounted() => relationship.State == RelationshipState.Mounted && relationship.Rider == rider && relationship.Mount == mount;

        private bool F2PairIdle() => rider.Commands.Empty && mount.Commands.Empty && !rider.AreHandsBusyWithAnimation &&
            !controls.HasUnsettledRelationshipTransition && !controls.HasUnsettledShellFor(relationship);

        private void F2Disclose(string action) => f2Disclosed.Add(new JObject { ["stage"] = stage, ["frame"] = Time.frameCount, ["action"] = action });

        private void F2Deadline()
        {
            if (!f2Clock.IsRunning) f2Clock.Start();
            if (f2Clock.Elapsed.TotalSeconds > F2DeadlineSeconds) throw new InvalidOperationException("F2 public-control stage timed out: " + stage);
        }

        // The native toolbar the player sees: its selected unit and each visible main-bar slot's content. Read only.
        private JObject F2VisibleBar()
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
                var list = manager.Slots == null ? null : typeof(ActionBarSlots).GetField("m_Slots", flags)?.GetValue(manager.Slots) as IEnumerable;
                var shown = new JArray();
                var settings = rider.UISettings?.Slots;
                if (list != null)
                    foreach (var item in list)
                    {
                        var slot = item as ActionBarSlot;
                        var mechanic = slot == null ? null : slot.MechanicSlot;
                        var settingsIndex = -1;
                        if (mechanic != null && settings != null)
                            for (var index = 0; index < settings.Length; index++)
                                if (ReferenceEquals(settings[index], mechanic)) { settingsIndex = index; break; }
                        shown.Add(new JObject { ["active"] = slot != null && slot.gameObject.activeInHierarchy, ["key"] = mechanic == null ? null : F2Key(mechanic),
                            ["settingsIndex"] = settingsIndex });
                    }
                result["mainBar"] = shown;
            }
            catch (Exception exception) { result["error"] = exception.GetType().Name + ": " + exception.Message; }
            return result;
        }

        // The visible main bar draws exactly the settings slot at that index (the same object), and its content is the key.
        private static bool F2VisibleShows(JObject view, int index, string key)
        {
            var bar = view["mainBar"] as JArray;
            if ((bool?)view["selectedIsRider"] != true || bar == null || index < 0 || index >= bar.Count) return false;
            var entry = (JObject)bar[index];
            return (bool?)entry["active"] == true && (int?)entry["settingsIndex"] == index && (string)entry["key"] == key;
        }

        private void F2Select()
        {
            var selection = SelectionManager.Instance.SelectedUnits;
            if (selection == null || selection.Count != 1 || selection[0] != rider) SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
        }

        // The composited frame WITH the game UI, read at its end (Sprint 8/F2 recipe), beside the run's other visuals.
        private void F2Shot(string name)
        {
            var row = new JObject { ["name"] = name, ["requestedFrame"] = Time.frameCount, ["stage"] = stage };
            var runner = UnityModManager.UI.Instance;
            if (runner == null) { row["error"] = "The end-of-frame runner is missing."; f2Shots.Add(row); return; }
            // The mod-manager window opens at start in base-game staging and covers the action bar (r04/r05 captures): it is
            // closed for the frame, as the movement capture coordinator does, and reopened after.
            row["ummClosedForCapture"] = runner.Opened;
            if (runner.Opened) runner.ToggleWindow(false);
            f2ShotsPending++;
            runner.StartCoroutine(F2Capture(row, runner));
        }

        private IEnumerator F2Capture(JObject row, UnityModManager.UI runner)
        {
            yield return null;
            yield return new WaitForEndOfFrame();
            try
            {
                row["frame"] = Time.frameCount; row["visible"] = F2VisibleBar();
                var bytes = Screenshot.CapturePNG();
                if (bytes == null || bytes.Length == 0) throw new InvalidOperationException("The frame encoder returned no bytes.");
                var folder = Path.Combine(request.EvidenceRoot, "movement-visuals");
                Directory.CreateDirectory(folder);
                var file = "finalize-f2-p05-" + (string)row["name"] + ".png";
                File.WriteAllBytes(Path.Combine(folder, file), bytes);
                using (var algorithm = SHA256.Create())
                    row["sha256"] = BitConverter.ToString(algorithm.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
                row["file"] = "movement-visuals/" + file; row["bytes"] = bytes.Length; row["width"] = Screen.width; row["height"] = Screen.height;
            }
            catch (Exception exception) { row["error"] = exception.GetType().Name + ": " + exception.Message; }
            if ((bool?)row["ummClosedForCapture"] == true && ReferenceEquals(UnityModManager.UI.Instance, runner)) runner.ToggleWindow(true);
            f2Shots.Add(row);
            f2ShotsPending--;
        }

        private bool F2ShotsSettled() => f2ShotsPending == 0 || ++f2Frames > F2ShotFrames;

        // ---- watch: the control's slot and fact identity, or the removal ----------------------------------------------------
        private void F2BeginWatch(bool removed)
        {
            f2WatchRemoved = removed; f2Fact = F2PublicFact(); f2PlacementsAtWatch = controls.InitialPlacementCount;
            f2Watched = f2Moved = f2NotLive = f2Replaced = f2Reinserted = 0; f2Anomalies = new JArray();
        }

        private void F2Watch()
        {
            var indices = F2Indices();
            var fact = F2PublicFact();
            var moved = !f2WatchRemoved && (indices.Count != 1 || indices[0] != f2Index);
            var notLive = !f2WatchRemoved && !F2HoldsLive(f2Index);
            var replaced = !ReferenceEquals(fact, f2Fact) || fact == null || !fact.Active;
            var reinserted = f2WatchRemoved && indices.Count != 0 || controls.InitialPlacementCount != f2PlacementsAtWatch;
            f2Watched++;
            if (moved) f2Moved++;
            if (notLive) f2NotLive++;
            if (replaced) f2Replaced++;
            if (reinserted) f2Reinserted++;
            if ((moved || notLive || replaced || reinserted) && f2Anomalies.Count < 8)
                f2Anomalies.Add(new JObject { ["frame"] = Time.frameCount, ["stage"] = stage, ["indices"] = new JArray(indices), ["relationship"] = relationship.State.ToString(),
                    ["initialPlacement"] = controls.InitialPlacementCount });
        }

        private JObject F2WatchRecord(string phase) => new JObject { ["phase"] = phase, ["index"] = f2Index, ["removed"] = f2WatchRemoved, ["frames"] = f2Watched,
            ["movedFrames"] = f2Moved, ["notLiveFrames"] = f2NotLive, ["factReplacedFrames"] = f2Replaced, ["reinsertedFrames"] = f2Reinserted, ["anomalies"] = f2Anomalies };

        private bool F2WatchClean => f2Moved == 0 && f2NotLive == 0 && f2Replaced == 0 && f2Reinserted == 0;

        // ---- input: the rider's own slot through the native slot body, then the native target click ----------------------
        private static UnitUseAbility F2FindCommand(UnitEntityData unit, BlueprintAbility blueprint) =>
            unit.Commands.Raw.OfType<UnitUseAbility>().Concat(unit.Commands.Queue.OfType<UnitUseAbility>())
                .FirstOrDefault(command => ReferenceEquals(command.Spell?.Blueprint, blueprint));

        // slot: the rider's own bar slot, or (drawer) an unplaced slot entry for the same live fact, as the ability drawer builds.
        private JObject F2Click(MechanicActionBarSlotAbility slot, string route, bool mounting)
        {
            F2Select();
            controls.Update();
            var handler = Game.Instance.SelectedAbilityHandler;
            var shell = mounting ? controls.MountAbility : controls.DismountAbility;
            var data = slot?.Ability;
            var casts = controls.NativeCastRequestCount; var refusals = controls.NativeRefusalCount;
            var record = new JObject { ["route"] = route, ["mounting"] = mounting, ["slotIndex"] = f2Index, ["slotAbilityBefore"] = data?.Blueprint?.AssetGuid,
                ["slotHeldLiveFact"] = data != null && ReferenceEquals(data, F2PublicFact()?.Data), ["possible"] = slot?.IsPossibleActive,
                ["stateBefore"] = relationship.State.ToString(), ["frame"] = Time.frameCount, ["readyFrames"] = f2ReadyFrames,
                ["riderMoveCooldown"] = rider.CombatState?.Cooldown?.MoveAction, ["partyCombat"] = Game.Instance.Player.IsInCombat, ["waitStart"] = f2WaitStart,
                ["waitSeconds"] = f2ReadyClock.Elapsed.TotalSeconds };
            f2ReadyFrames = 0; f2WaitStart = null; f2ReadyClock.Reset();
            Check(slot != null && handler != null, "f2-public-slot-and-native-handler-available");
            slot.OnClick();
            record["slotAbilityAfter"] = slot.Ability?.Blueprint?.AssetGuid;
            record["slotDataRestored"] = ReferenceEquals(slot.Ability, data);
            record["handlerAfterSlot"] = handler.Ability?.Blueprint?.AssetGuid;
            if (mounting)
            {
                var clicked = false;
                try { clicked = ReferenceEquals(handler.Ability?.Blueprint, shell) && mount.View != null && handler.OnClick(mount.View.gameObject, mount.Position, 0, false, false); }
                finally { if (handler.Ability != null) handler.DropAbility(); }
                record["targetClicked"] = clicked; record["targetId"] = mount.UniqueId;
            }
            else if (handler.Ability != null) { record["unexpectedSelection"] = handler.Ability.Blueprint?.AssetGuid; handler.DropAbility(); }
            f2Command = F2FindCommand(rider, shell);
            var identity = controls.CaptureRelationshipCommandIdentity(f2Command);
            record["commandBlueprint"] = f2Command?.Spell?.Blueprint?.AssetGuid; record["control"] = identity?.Control;
            record["caster"] = identity?.Caster; record["target"] = identity?.Target; record["generationAtInit"] = identity?.Generation;
            record["castDelta"] = controls.NativeCastRequestCount - casts; record["refusalDelta"] = controls.NativeRefusalCount - refusals;
            record["stateAfterClick"] = relationship.State.ToString();
            return record;
        }

        // The native slot body admits its shell only while that action is possible: wait for it, bounded, instead of clicking
        // early. Past the bound the click still runs and its record shows the refusal.
        // The first not-ready frame records why (the native cooldowns then), so a wait is told apart from a product refusal.
        private bool F2Ready(MechanicActionBarSlotAbility slot)
        {
            if (slot?.IsPossibleActive == true) return true;
            if (f2WaitStart == null)
            {
                f2WaitStart = new JObject { ["frame"] = Time.frameCount, ["slotPresent"] = slot != null, ["riderMoveCooldown"] = rider.CombatState?.Cooldown?.MoveAction,
                    ["riderStandardCooldown"] = rider.CombatState?.Cooldown?.StandardAction, ["partyCombat"] = Game.Instance.Player.IsInCombat, ["paused"] = Game.Instance.IsPaused };
                f2ReadyClock.Reset(); f2ReadyClock.Start();
            }
            f2ReadyFrames++;
            return f2ReadyClock.Elapsed.TotalSeconds >= F2ReadySeconds;
        }

        private MechanicActionBarSlotAbility F2Slot() =>
            rider.UISettings?.Slots != null && f2Index >= 0 && f2Index < rider.UISettings.Slots.Length ? rider.UISettings.Slots[f2Index] as MechanicActionBarSlotAbility : null;

        // ---- strings after the native load ---------------------------------------------------------------------------------
        private JObject F2Strings()
        {
            var pack = LocalizationManager.CurrentPack;
            var table = typeof(NativeMountedControlService).GetField("localization", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(controls) as Dictionary<string, string>;
            var keys = new JArray();
            if (table != null)
                foreach (var entry in table.OrderBy(value => value.Key, StringComparer.Ordinal))
                {
                    string actual = null;
                    var present = pack?.Strings != null && pack.Strings.TryGetValue(entry.Key, out actual);
                    keys.Add(new JObject { ["key"] = entry.Key, ["expected"] = entry.Value, ["present"] = present, ["actual"] = actual });
                }
            var list = new JArray();
            foreach (var control in new[] { F2Public, controls.MountAbility, controls.DismountAbility, controls.MountedChargeAbility })
                list.Add(new JObject { ["guid"] = control?.AssetGuid, ["name"] = control?.Name, ["hidden"] = control == null ? (bool?)null : control.Hidden });
            return new JObject { ["source"] = "NativeMountedControlService localization table (diagnostic reflection, read only)", ["tableFound"] = table != null,
                ["packPresent"] = pack != null, ["keys"] = keys, ["controls"] = list };
        }

        // ---- the cold load's first observation (stage 0, before any input or slot write by this scenario) ---------------------
        private void ObserveFinalize2Cold(SavedMountedSlot[] savedSlots, SavedMountedSlot[] bindings, int sourceSchema)
        {
            F2Deadline();
            Check(F2Public != null && F2Public.AssetGuid == NativeMountedControlService.MountStateAbilityGuid && controls.MountAbility != null &&
                controls.DismountAbility != null, "f2-public-control-and-shells-registered");
            var detail = new JObject { ["contract"] = F2Contract, ["role"] = request.IsFinalize2UpgradeProducer ? "producer" : "consumer",
                ["sourceSchema"] = sourceSchema, ["savedRows"] = F2Rows(savedSlots), ["liveRows"] = F2Rows(bindings), ["actor"] = F2Actor("cold-loaded"),
                ["visibleAsLoaded"] = F2VisibleBar(),
                ["autofill"] = Kingmaker.UI.SettingsUI.SettingsRoot.Instance?.AutofillActionbarSlots?.CurrentValue,
                ["placementSettled"] = controls.PlacementSettled == null ? (bool?)null : controls.PlacementSettled(),
                ["retiredHelpersSelected"] = controls.RetiredPrimaryHelpersSelected,
                ["identities"] = new JObject { ["publicControl"] = F2Public.AssetGuid, ["mountShell"] = controls.MountAbility.AssetGuid,
                    ["dismountShell"] = controls.DismountAbility.AssetGuid, ["mountedCharge"] = controls.MountedChargeAbility?.AssetGuid } };
            var controlRows = savedSlots.Where(row => row.ActorId == rider.UniqueId && MountedControlSlotPolicy.RestoreSavedSlot(row.Kind) == MountedControlSlotRepair.MountState).ToArray();
            f2Index = controlRows.Length >= 1 ? controlRows[0].Index : -1;
            if (request.IsFinalize2UpgradeProducer)
            {
                // The authentic old archive: its one Dismount row becomes the public control at that index; a retired rider
                // primary row is not restored in ordinary play; the actor's placement is recorded, so nothing is placed.
                f2Legacy = f2Index;
                Check(sourceSchema < MountedArchiveData.Schema && controlRows.Length == 1 && controlRows[0].Kind != MountedControlSlotPolicy.SavedMountState &&
                    !controls.RetiredPrimaryHelpersSelected, "f2-legacy-input-carries-one-old-control-row");
                Check(F2OnlyAt(f2Legacy) && F2HoldsLive(f2Legacy) && controls.LegacySlotRepairCount == 1 && controls.InitialPlacementCount == 0 &&
                    controls.IsPlacementRecorded(rider), "f2-legacy-row-is-the-public-control-in-place");
                Check(bindings.Length == 2 && bindings.Count(b => b.ActorId == rider.UniqueId && b.Kind == MountedControlSlotPolicy.SavedMountState && b.Index == f2Legacy) == 1 &&
                    bindings.Count(b => b.ActorId == rider.UniqueId && b.Kind == MountedControlSlotPolicy.SavedPlacementRecord && b.Index == 0) == 1,
                    "f2-legacy-live-rows-are-the-public-control-and-its-record");
                detail["legacyIndex"] = f2Legacy;
                detail["retiredRows"] = new JArray(savedSlots.Where(row => row.Kind == MountedControlSlotPolicy.SavedRiderPrimary || row.Kind == MountedControlSlotPolicy.SavedMountPrimary)
                    .Select(row => new JObject { ["index"] = row.Index, ["kind"] = row.Kind, ["now"] = F2Key(row.Index < rider.UISettings.Slots.Length ? rider.UISettings.Slots[row.Index] : null) }));
            }
            else
            {
                // The F2 producer's output: the control at its chosen slot, the record, no placement and no migration.
                Check(sourceSchema == MountedArchiveData.Schema && controlRows.Length == 1 && controlRows[0].Kind == MountedControlSlotPolicy.SavedMountState &&
                    F2OnlyAt(f2Index) && F2HoldsLive(f2Index) && controls.IsPlacementRecorded(rider) && controls.InitialPlacementCount == 0 &&
                    controls.InitialPlacements.Count == 0 && controls.LegacySlotRepairCount == 0, "f2-cold-control-restored-at-its-saved-slot");
            }
            Check(((JArray)F2Exposure()["hiddenExposed"]).Count == 0, "f2-no-hidden-shell-on-the-bar");
            Write("f2-cold-observed", detail);
        }

        // ---- after the existing continuation: the producer and consumer F2 stages ------------------------------------------
        private void BeginFinalize2() { f2Frames = 0; F2Select(); stage = request.IsFinalize2UpgradeProducer ? 300 : 320; }

        private void AdvanceFinalize2()
        {
            F2Deadline();
            var game = Game.Instance;
            if (LoadingProcess.Instance.IsLoadingInProcess) return;
            if (stage >= 304 && stage <= 306 || stage >= 321 && stage <= 323 || stage >= 333 && stage <= 335) F2Watch();
            switch (stage)
            {
                // Producer. 300: the rider is the one selection and its bar is drawn; the in-place control is removed (player).
                case 300:
                {
                    if (++f2Frames < F2SettleFrames) return;
                    var before = F2VisibleBar();
                    Check(F2VisibleShows(before, 0, F2Key(rider.UISettings.GetSlot(0, rider))), "f2-rider-bar-drawn-before-setup");
                    var record = new JObject { ["visibleBefore"] = before, ["actor"] = F2Actor("before-setup") };
                    rider.UISettings.SetSlot(new MechanicActionBarSlotEmpty { Unit = rider }, f2Legacy);
                    rider.UISettings.SetDirty();
                    F2Disclose("Player removal: the migrated public control in slot " + f2Legacy + " was replaced with an empty slot (UnitUISettings.SetSlot, then SetDirty as the toolbar's own edit does).");
                    Write("f2-setup-removed-in-place", record);
                    F2BeginWatch(true); f2Frames = 0; stage = 301; return;
                }
                case 301:
                {
                    F2Watch();
                    if (++f2Frames < F2WatchFrames) return;
                    var watch = F2WatchRecord("in-session-removal");
                    Check(F2WatchClean && controls.IsPlacementRecorded(rider), "f2-in-session-removal-stays-removed");
                    // Disclosed setup for the first-placement event: the main bar's last slot emptied as a player removes an
                    // ability, drawn, then this world's placement records forgotten as a newly loaded world's are (r03 recipe).
                    var last = MountedControlSlotPolicy.MainBarSlotCount - 1;
                    var held = F2Key(rider.UISettings.GetSlot(last, rider));
                    rider.UISettings.SetSlot(new MechanicActionBarSlotEmpty { Unit = rider }, last);
                    rider.UISettings.SetDirty();
                    F2Disclose("Player removal: main-bar slot " + last + " (" + held + ") was emptied (UnitUISettings.SetSlot, then SetDirty).");
                    Write("f2-setup-freed-slot", new JObject { ["watch"] = watch, ["freedIndex"] = last, ["freedHeld"] = held, ["actor"] = F2Actor("slot-freed") });
                    f2Frames = 0; stage = 302; return;
                }
                case 302:
                {
                    if (++f2Frames < F2SettleFrames) return;
                    var last = MountedControlSlotPolicy.MainBarSlotCount - 1;
                    var drawnEmpty = F2VisibleBar();
                    Check(F2VisibleShows(drawnEmpty, last, ""), "f2-freed-slot-drawn-empty-before-placement");
                    Check(F2Indices().Count == 0 && controls.InitialPlacementCount == 0, "f2-no-control-before-the-placement-event");
                    // The native new-ability route needs the autofill setting; off, no placement is expected (not a repaint defect).
                    var autofill = Kingmaker.UI.SettingsUI.SettingsRoot.Instance?.AutofillActionbarSlots?.CurrentValue == true;
                    Check(autofill, "f2-native-autofill-setting-on-before-the-placement-event");
                    controls.BeginLoadedWorld();
                    F2Disclose("The placement records were forgotten as a newly loaded world's are (NativeMountedControlService.BeginLoadedWorld), so the rider gets its first placement.");
                    Write("f2-placement-armed", new JObject { ["autofill"] = autofill, ["visibleBefore"] = drawnEmpty, ["frame"] = Time.frameCount, ["actor"] = F2Actor("armed") });
                    f2Frames = 0; stage = 303; return;
                }
                // 303: no selection, drawer, refresh or mount happens here: only frames pass until the bar draws the control.
                case 303:
                {
                    var placements = controls.InitialPlacements.Where(p => p.UnitId == rider.UniqueId).ToArray();
                    var view = F2VisibleBar();
                    f2Frames++;
                    var placed = placements.Length == 1 && placements[0].Index >= 0;
                    var shown = placed && F2VisibleShows(view, placements[0].Index, F2Public.AssetGuid);
                    if (!shown && f2Frames < F2VisibleFrames) return;
                    var record = new JObject { ["framesObserved"] = f2Frames, ["frame"] = Time.frameCount, ["visible"] = view, ["actor"] = F2Actor("placed"),
                        ["placementFrame"] = placed ? placements[0].Frame : (int?)null, ["shown"] = shown };
                    Write("f2-placement-visible", record);
                    Check(placed && shown, "f2-first-placement-drawn-without-another-refresh");
                    f2Index = placements[0].Index;
                    var before = placements[0].MainBarBefore ?? new string[0];
                    var now = F2Bar(MountedControlSlotPolicy.MainBarSlotCount);
                    Check(before.Length == MountedControlSlotPolicy.MainBarSlotCount && f2Index == Array.FindIndex(before, key => key == "") && F2OnlyAt(f2Index) &&
                        F2HoldsLive(f2Index) && controls.InitialPlacementCount == 1 &&
                        Enumerable.Range(0, before.Length).All(i => i == f2Index ? (string)now[i] == F2Public.AssetGuid : (string)now[i] == before[i]),
                        "f2-one-placement-in-the-first-empty-main-slot");
                    F2Shot("placed");
                    F2BeginWatch(false); f2Frames = 0; stage = 304; return;
                }
                case 304:
                {
                    if (!F2ShotsSettled() || !F2PairIdle() || !F2Ready(F2Slot())) return;
                    Check(F2Mounted(), "f2-pair-mounted-before-cycle");
                    var record = F2Click(F2Slot(), "bar-slot", false);
                    Write("f2-cycle-dismount-click", record);
                    Check(f2Command != null || relationship.State != RelationshipState.Mounted, "f2-slot-runs-dismount-once");
                    f2Frames = 0; stage = 305; return;
                }
                case 305:
                {
                    if (relationship.State != RelationshipState.Unmounted || f2Command != null && !f2Command.IsFinished || !F2PairIdle()) return;
                    if (++f2Frames < F2SettleFrames) return;
                    if (f2Frames == F2SettleFrames) Write("f2-cycle-dismounted", F2Actor("dismounted"));
                    if (!F2Ready(F2Slot())) return;
                    var record = F2Click(F2Slot(), "bar-slot", true);
                    Write("f2-cycle-mount-click", record);
                    Check((bool?)record["targetClicked"] == true && f2Command != null, "f2-slot-selects-mount-and-the-target-click-creates-one-shell");
                    f2Frames = 0; stage = 306; return;
                }
                case 306:
                {
                    if (!F2Mounted() || f2Command != null && !f2Command.IsFinished || !F2PairIdle()) return;
                    if (++f2Frames < F2SettleFrames) return;
                    var watch = F2WatchRecord("cycle");
                    Write("f2-cycle-mounted", new JObject { ["watch"] = watch, ["actor"] = F2Actor("remounted") });
                    Check(F2WatchClean, "f2-control-kept-its-slot-and-fact-through-the-cycle");
                    // Disclosed player drag: the control swapped with an ability in another main-bar slot.
                    f2Chosen = Enumerable.Range(0, MountedControlSlotPolicy.MainBarSlotCount).Reverse()
                        .FirstOrDefault(i => i != f2Index && i < f2Index - 3 && rider.UISettings.GetSlot(i, rider) is MechanicActionBarSlotAbility);
                    Check(f2Chosen >= 0 && f2Chosen != f2Index && rider.UISettings.GetSlot(f2Chosen, rider) is MechanicActionBarSlotAbility, "f2-a-main-bar-ability-to-swap-with");
                    var moved = rider.UISettings.Slots[f2Chosen];
                    var swap = new JObject { ["from"] = f2Index, ["to"] = f2Chosen, ["movedKey"] = F2Key(moved), ["barBefore"] = F2Bar(MountedControlSlotPolicy.MainBarSlotCount) };
                    rider.UISettings.SetSlot(new MechanicActionBarSlotAbility { Unit = rider, Ability = F2PublicFact().Data }, f2Chosen);
                    rider.UISettings.SetSlot(moved, f2Index);
                    rider.UISettings.SetDirty();
                    F2Disclose("Player drag: the public control in slot " + f2Index + " was swapped with the ability in slot " + f2Chosen + " (UnitUISettings.SetSlot both ways, then SetDirty).");
                    f2Index = f2Chosen;
                    swap["barAfter"] = F2Bar(MountedControlSlotPolicy.MainBarSlotCount);
                    Write("f2-setup-swapped", swap);
                    F2BeginWatch(false); f2Frames = 0; stage = 307; return;
                }
                case 307:
                {
                    F2Watch();
                    if (++f2Frames < F2WatchFrames) return;
                    var watch = F2WatchRecord("before-kept-save");
                    Check(F2WatchClean && F2OnlyAt(f2Index) && F2HoldsLive(f2Index), "f2-swapped-control-holds-its-chosen-slot");
                    f2BarBeforeSave = F2Bar(MountedControlSlotPolicy.MainBarSlotCount + 8);
                    var rows = controls.CapturePersistentSlots();
                    Check(F2ExpectRows(rows, f2Index), "f2-kept-rows-before-save");
                    Write("f2-kept-before-save", new JObject { ["watch"] = watch, ["rows"] = F2Rows(rows), ["actor"] = F2Actor("kept-before-save") });
                    F2RequestWrite("KMC_P01"); stage = 308; return;
                }
                case 308:
                {
                    if (!F2ObserveWrite("KMC_P01", "Manual_1_KMC_P01.zks", 1, f2Index)) return;
                    var slot = f2Index;
                    rider.UISettings.SetSlot(new MechanicActionBarSlotEmpty { Unit = rider }, slot);
                    rider.UISettings.SetDirty();
                    F2Disclose("Deliberate removal: the public control in slot " + slot + " was replaced with an empty slot (UnitUISettings.SetSlot, then SetDirty).");
                    Write("f2-setup-removed", F2Actor("removed"));
                    F2BeginWatch(true); f2Frames = 0; stage = 309; return;
                }
                case 309:
                {
                    F2Watch();
                    if (++f2Frames < F2WatchFrames) return;
                    var watch = F2WatchRecord("before-removed-save");
                    var rows = controls.CapturePersistentSlots();
                    Check(F2WatchClean && controls.IsPlacementRecorded(rider) && F2ExpectRows(rows, -1), "f2-removed-control-stays-removed-before-save");
                    f2BarBeforeSave = F2Bar(MountedControlSlotPolicy.MainBarSlotCount + 8);
                    Write("f2-removed-before-save", new JObject { ["watch"] = watch, ["rows"] = F2Rows(rows), ["actor"] = F2Actor("removed-before-save") });
                    F2Shot("removed");
                    f2Frames = 0; stage = 310; return;
                }
                case 310:
                    if (!F2ShotsSettled()) return;
                    F2RequestWrite(RuntimeRequest.Finalize2RemovedSaveName); stage = 311; return;
                case 311:
                {
                    if (!F2ObserveWrite(RuntimeRequest.Finalize2RemovedSaveName, RuntimeRequest.Finalize2RemovedSaveLeaf, 2, -1)) return;
                    Check(Directory.GetFiles(game.SaveManager.SavePath, "*.zks").Length == 3, "f2-producer-root-holds-the-input-and-two-outputs");
                    F2Complete(); return;
                }

                // Consumer. 320: the restored slot drawn for the selected rider before any input of this scenario.
                case 320:
                {
                    if (++f2Frames < F2SettleFrames) return;
                    var view = F2VisibleBar();
                    Write("f2-cold-visible", new JObject { ["visible"] = view, ["actor"] = F2Actor("cold-selected"), ["strings"] = F2Strings() });
                    Check(F2VisibleShows(view, f2Index, F2Public.AssetGuid), "f2-restored-control-drawn-at-its-saved-slot");
                    F2Shot("cold-kept");
                    F2BeginWatch(false); f2Frames = 0; stage = 321; return;
                }
                case 321:
                {
                    if (!F2ShotsSettled() || !F2PairIdle() || !F2Ready(F2Slot())) return;
                    Check(F2Mounted(), "f2-cold-pair-mounted-before-cycle");
                    var record = F2Click(F2Slot(), "bar-slot", false);
                    Write("f2-cycle-dismount-click", record);
                    Check(f2Command != null || relationship.State != RelationshipState.Mounted, "f2-cold-slot-runs-dismount-once");
                    f2Frames = 0; stage = 322; return;
                }
                case 322:
                {
                    if (relationship.State != RelationshipState.Unmounted || f2Command != null && !f2Command.IsFinished || !F2PairIdle()) return;
                    if (++f2Frames < F2SettleFrames) return;
                    if (f2Frames == F2SettleFrames) Write("f2-cycle-dismounted", F2Actor("dismounted"));
                    if (!F2Ready(F2Slot())) return;
                    var record = F2Click(F2Slot(), "bar-slot", true);
                    Write("f2-cycle-mount-click", record);
                    Check((bool?)record["targetClicked"] == true && f2Command != null, "f2-cold-slot-selects-mount-and-the-target-click-creates-one-shell");
                    f2Frames = 0; stage = 323; return;
                }
                case 323:
                {
                    if (!F2Mounted() || f2Command != null && !f2Command.IsFinished || !F2PairIdle()) return;
                    if (++f2Frames < F2SettleFrames) return;
                    var watch = F2WatchRecord("cold-cycle");
                    Write("f2-cycle-mounted", new JObject { ["watch"] = watch, ["actor"] = F2Actor("remounted"), ["strings"] = F2Strings() });
                    Check(F2WatchClean, "f2-cold-control-kept-its-slot-and-fact-through-the-cycle");
                    Check(Hash(Path.Combine(game.SaveManager.SavePath, request.PersistenceLoad.FileName)) == request.PersistenceLoad.Sha256,
                        "f2-kept-archive-unchanged-before-the-second-load");
                    F2RequestLoad(request.PersistenceAlternate); stage = 330; return;
                }
                // 330: the removed-control archive, loaded natively in this process.
                case 330:
                {
                    if (!f2LoadCallback || game.CurrentlyLoadedArea == null || game.CurrentMode != Kingmaker.GameModes.GameModeType.Default) return;
                    var settled = controls.PlacementSettled == null || controls.PlacementSettled();
                    if (++f2Frames < F2SettleFrames || !settled && f2Frames < F2ShotFrames) return;
                    var riders = game.State.Units.Where(u => u.UniqueId == rider.UniqueId).ToArray();
                    var mounts = game.State.Units.Where(u => u.UniqueId == mount.UniqueId).ToArray();
                    Check(riders.Length == 1 && mounts.Length == 1 && !ReferenceEquals(riders[0], rider) && !ReferenceEquals(mounts[0], mount),
                        "f2-removed-archive-is-a-new-native-world-with-the-same-actors");
                    rider = riders[0]; mount = mounts[0];
                    var archive = persistence.LoadedArchiveData;
                    var saved = archive?.Slots ?? new SavedMountedSlot[0];
                    var live = controls.CapturePersistentSlots();
                    var record = new JObject { ["settled"] = settled, ["savedRows"] = F2Rows(saved), ["liveRows"] = F2Rows(live), ["actor"] = F2Actor("removed-loaded") };
                    Write("f2-removed-loaded", record);
                    Check(settled && archive != null && archive.SchemaVersion == MountedArchiveData.Schema && F2Mounted(), "f2-removed-archive-restored-the-pair");
                    Check(F2ExpectRows(saved, -1) && F2ExpectRows(live, -1) && F2Indices().Count == 0 && F2PublicFact()?.Active == true &&
                        controls.IsPlacementRecorded(rider) && controls.InitialPlacementCount == 0 && controls.InitialPlacements.Count == 0 &&
                        controls.LegacySlotRepairCount == 0, "f2-removed-control-not-reinserted-by-the-load");
                    Check(((JArray)F2Exposure()["hiddenExposed"]).Count == 0, "f2-removed-no-hidden-shell-on-the-bar");
                    F2BeginWatch(true); f2Frames = 0; stage = 331; return;
                }
                case 331:
                {
                    F2Watch();
                    if (++f2Frames < F2RemovedWatchFrames) return;
                    var watch = F2WatchRecord("removed-update-ticks");
                    // Reselection: another party member, then the rider again.
                    var other = game.Player.ControllableCharacters.FirstOrDefault(u => u != rider && u != mount && u.View != null);
                    Check(other != null, "f2-another-party-member-to-reselect-from");
                    SelectionManager.Instance.SelectUnit(other.View, true, true, false);
                    Write("f2-removed-ticks", new JObject { ["watch"] = watch, ["reselectVia"] = other?.UniqueId });
                    Check(F2WatchClean, "f2-removed-control-absent-through-update-ticks");
                    f2Frames = 0; stage = 332; return;
                }
                case 332:
                {
                    F2Watch();
                    if (++f2Frames == F2SettleFrames) SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                    if (f2Frames < F2SettleFrames * 2) return;
                    var view = F2VisibleBar();
                    Write("f2-removed-reselected", new JObject { ["visible"] = view, ["watch"] = F2WatchRecord("removed-reselection"), ["actor"] = F2Actor("reselected") });
                    var bar = view["mainBar"] as JArray;
                    Check(F2WatchClean && (bool?)view["selectedIsRider"] == true && bar != null && bar.All(entry => (string)entry["key"] != F2Public.AssetGuid),
                        "f2-removed-control-absent-after-reselection");
                    F2Shot("cold-removed");
                    f2Frames = 0; stage = 333; return;
                }
                // 333-338: the view/control cycle through the drawer's slot entry (the control is not on the bar).
                case 333:
                {
                    var drawn = F2PublicFact()?.Data;
                    var entry = drawn == null ? null : new MechanicActionBarSlotAbility { Unit = rider, Ability = drawn };
                    if (!F2ShotsSettled() || !F2PairIdle() || !F2Ready(entry)) return;
                    var record = F2Click(entry, "drawer-entry", false);
                    Write("f2-removed-dismount-click", record);
                    Check(f2Command != null || relationship.State != RelationshipState.Mounted, "f2-removed-drawer-runs-dismount-once");
                    f2Frames = 0; stage = 334; return;
                }
                case 334:
                {
                    if (relationship.State != RelationshipState.Unmounted || f2Command != null && !f2Command.IsFinished || !F2PairIdle()) return;
                    if (++f2Frames < F2SettleFrames) return;
                    var drawn = F2PublicFact()?.Data;
                    var entry = drawn == null ? null : new MechanicActionBarSlotAbility { Unit = rider, Ability = drawn };
                    if (!F2Ready(entry)) return;
                    var record = F2Click(entry, "drawer-entry", true);
                    Write("f2-removed-mount-click", record);
                    Check((bool?)record["targetClicked"] == true && f2Command != null, "f2-removed-drawer-mount-creates-one-shell");
                    f2Frames = 0; stage = 335; return;
                }
                case 335:
                {
                    if (!F2Mounted() || f2Command != null && !f2Command.IsFinished || !F2PairIdle()) return;
                    if (++f2Frames < F2WatchFrames) return;
                    var watch = F2WatchRecord("removed-control-cycle");
                    Write("f2-removed-cycled", new JObject { ["watch"] = watch, ["actor"] = F2Actor("removed-cycled"), ["strings"] = F2Strings() });
                    Check(F2WatchClean && F2Indices().Count == 0 && controls.InitialPlacementCount == 0, "f2-removed-control-absent-after-a-control-cycle");
                    Check(Hash(Path.Combine(game.SaveManager.SavePath, request.PersistenceLoad.FileName)) == request.PersistenceLoad.Sha256 &&
                        Hash(Path.Combine(game.SaveManager.SavePath, request.PersistenceAlternate.FileName)) == request.PersistenceAlternate.Sha256,
                        "f2-consumer-input-archives-unchanged");
                    F2Complete(); return;
                }
            }
        }

        // Exactly the control at index (or none when index < 0) and the actor's one placement record; nothing else.
        private bool F2ExpectRows(IEnumerable<SavedMountedSlot> rows, int index)
        {
            var list = (rows ?? new SavedMountedSlot[0]).ToArray();
            var controlRows = list.Count(r => r.ActorId == rider.UniqueId && r.Kind == MountedControlSlotPolicy.SavedMountState && r.Index == index);
            return list.Length == (index >= 0 ? 2 : 1) && (index < 0 || controlRows == 1) &&
                list.Count(r => r.ActorId == rider.UniqueId && r.Kind == MountedControlSlotPolicy.SavedPlacementRecord && r.Index == 0) == 1;
        }

        private void F2RequestWrite(string name)
        {
            var game = Game.Instance;
            Check(game.SaveManager.IsSaveAllowed(), "f2-native-save-admission");
            var descriptor = game.SaveManager.CreateNewSave(name);
            Check(descriptor.Name == name && descriptor.Type == SaveInfo.SaveType.Manual && !descriptor.IsActuallySaved, "f2-new-exact-native-manual");
            callback = false;
            Write("f2-write-requested", new JObject { ["name"] = name });
            game.SaveGame(descriptor, () => callback = true);
        }

        // The written archive's KMC rows, read back natively, and every native JSON member searched for the KMC identities.
        private bool F2ObserveWrite(string name, string leaf, int ordinal, int index)
        {
            if (!callback || NativePersistenceIsolation.HasPendingWrites) return false;
            var game = Game.Instance;
            var saved = game.SaveManager.SingleOrDefault(s => s.Name == name);
            if (saved == null || saved.OperationState != SaveInfo.StateType.None || !saved.HasFileOnDisk) return false;
            var archive = NativeMountedSaveStorage.ReadArchive(saved.Saver);
            var pair = NativeMountedSaveFormat.CanRestore(archive) && archive.Data.Pairs.Length == 1 ? archive.Data.Pairs[0] : null;
            var tokens = new[] { NativeMountedControlService.MountStateAbilityGuid, NativeMountedControlService.MountAbilityGuid, NativeMountedControlService.DismountAbilityGuid };
            int members; long bytes;
            var hits = NativeMountedSaveStorage.FindReferences(saved.Saver, tokens, out members, out bytes);
            var publicHits = hits.Count(hit => hit.EndsWith(":" + NativeMountedControlService.MountStateAbilityGuid, StringComparison.Ordinal));
            Write("native-write-complete", new JObject {
                ["ordinal"] = ordinal, ["name"] = name, ["path"] = saved.FolderName, ["sha256"] = Hash(saved.FolderName),
                ["length"] = new FileInfo(saved.FolderName).Length, ["nativeType"] = saved.Type.ToString(),
                ["nativeCallback"] = callback, ["operation"] = saved.OperationState.ToString(), ["sourceSha256"] = request.PersistenceLoad.Sha256,
                ["controlIndex"] = index, ["barBeforeSave"] = f2BarBeforeSave,
                ["scan"] = new JObject { ["tokens"] = new JArray(tokens), ["hits"] = new JArray(hits.Take(64)), ["hitCount"] = hits.Count, ["publicHits"] = publicHits,
                    ["members"] = members, ["bytes"] = bytes },
                ["snapshot"] = archive.Data == null ? null : JObject.FromObject(archive.Data, MountedSaveCodec.CreateSerializer()) });
            Check(saved.FileName == leaf && pair != null && pair.Rider.Id == rider.UniqueId && pair.Mount.Id == mount.UniqueId &&
                archive.Data.AreaId == request.PersistenceLoad.Area && archive.Data.CampaignId == request.PersistenceLoad.GameId &&
                archive.Data.SchemaVersion == MountedArchiveData.Schema && persistence.SnapshotCount == ordinal &&
                Hash(saved.FolderName) != request.PersistenceLoad.Sha256, "f2-native-manual-carries-the-restored-pair");
            Check(F2ExpectRows(archive.Data.Slots, index) && publicHits == 0, "f2-archive-rows-are-exactly-the-control-state-and-no-native-member-names-it");
            return true;
        }

        private void F2RequestLoad(RuntimeSaveDescriptor expected)
        {
            var game = Game.Instance;
            var path = Path.Combine(game.SaveManager.SavePath, expected.FileName);
            Check(Hash(path) == expected.Sha256, "f2-exact-removed-archive-hash");
            var descriptor = game.SaveManager.Single(s => s.FileName == expected.FileName);
            Check(descriptor.Name == expected.InternalName && descriptor.Type == SaveInfo.SaveType.Manual && descriptor.GameId == expected.GameId &&
                descriptor.GameName == expected.GameName && descriptor.Area.AssetGuidThreadSafe == expected.Area, "f2-exact-removed-native-load-descriptor");
            f2LoadCallback = false; f2Frames = 0;
            Write("f2-load-requested", new JObject { ["path"] = path, ["sha256"] = expected.Sha256 });
            game.SaveManager.AddCallbackAfterLoad(() => f2LoadCallback = true);
            game.LoadGame(descriptor);
        }

        private void F2Complete()
        {
            Write("f2-complete", new JObject { ["disclosedSetup"] = f2Disclosed, ["shots"] = f2Shots, ["counters"] = F2Counters(), ["seconds"] = f2Clock.Elapsed.TotalSeconds });
            Dispose();
            Result = new RuntimeSubscenarioResult { Name = request.Scenario, Status = "PASS", AssertionPassCount = passed, AssertionFailCount = 0, Errors = new string[0] };
            Completed = true;
        }
    }
}
