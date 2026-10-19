using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Harmony12;
using Kingmaker;
using Kingmaker.Controllers.Combat;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Installed before cold native LoadGame. Observation only: no native argument,
    // return, resource, preparation or patch ordering contract is rewritten.
    internal sealed class NativeArchiveRestorationProbe : IDisposable
    {
        private const string HarmonyId = "KingmakerMountedCombat.Diagnostics.Chunk7Restoration";
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        private const int MaximumEvents = 2048;
        private static NativeArchiveRestorationProbe active;
        private readonly MountedPersistenceService persistence;
        private readonly MountedWorldService world;
        private HarmonyInstance harmony;
        private readonly JArray events = new JArray(), hooks = new JArray();
        private int dropped, errors;
        private bool disposed, installed, installationStarted;
        private readonly RuntimeRequest debtRequest;
        private readonly string debtRequestKey;
        private readonly JArray debtEvents = new JArray();
        private readonly HashSet<int> debtOpenCalls = new HashSet<int>();
        private UnitEntityData[] debtActors;
        private MountedArchiveData debtArchive;
        private Game debtGame;
        private int debtSequence, debtErrors, debtDropped;
        private const int MaximumDebtEvents = 128;
        internal bool Complete => installed && dropped == 0 && errors == 0 && debtErrors == 0 && debtDropped == 0 &&
            debtOpenCalls.Count == 0 && (debtRequest == null || DebtIdentityCurrent());
        internal NativeArchiveRestorationProbe(MountedPersistenceService persistence, MountedWorldService world = null, RuntimeRequest debtDiagnosticRequest = null)
        {
            this.persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
            this.world = world;
            if (debtDiagnosticRequest != null && (!IsDebtDiagnosticRequest(debtDiagnosticRequest) || world == null))
                throw new ArgumentException("Debt-boundary diagnostics require their exact owner-seed schema2 cold/resave case.");
            debtRequest = debtDiagnosticRequest;
            debtRequestKey = DebtRequestKey(debtRequest);
        }
        internal void Install()
        {
            if (disposed) throw new ObjectDisposedException(nameof(NativeArchiveRestorationProbe));
            if (installed) return;
            if (installationStarted) throw new InvalidOperationException("A failed observer installation retains cleanup custody and cannot be replayed.");
            if (active != null) throw new InvalidOperationException("Another observer already owns this native boundary.");
            installationStarted = true;
            // Native contract validation; no patch ownership exists before the Harmony boundary.
            if (Game.Instance?.CurrentlyLoadedArea != null)
                throw new InvalidOperationException("Cold restoration observation must begin before any native world is loaded.");
            if (typeof(UnitEntityData).Assembly.ManifestModule.ModuleVersionId != new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7"))
                throw new InvalidOperationException("Native restoration observer MVID differs.");
            harmony = HarmonyInstance.Create(HarmonyId); active = this;
            debtGame = Game.Instance;
            Patch(typeof(TurnController), 0x06000C3C, "PrepareBefore", "PrepareAfter");
            Patch(typeof(UnitCombatState), 0x0600939D, "RoundBefore", "RoundAfter");
            PatchProduct(nameof(NativeCombatActorPersistence.RestoreActor), "RestoreActorBefore", "RestoreActorAfter");
            PatchProduct(nameof(NativeCombatActorPersistence.RestoreDebt), "RestoreDebtBefore", "RestoreDebtAfter");
            if (world != null)
            {
                Install(typeof(MountedPersistenceService).GetMethod("TryRestoreArchiveCombat", Flags), "SharedWorldBefore", "SharedWorldAfter");
                Install(typeof(MountedPersistenceService).GetMethod("RestoreArchivePresentation", Flags), "PresentationBefore", "PresentationAfter");
            }
            if (debtRequest != null)
            {
                Patch(typeof(UnitEntityData), 0x06008355, "DebtJoinBefore", "DebtJoinAfter");
                Patch(typeof(UnitCombatPrepareController), 0x0600936F, "DebtPrepareBefore", "DebtPrepareAfter");
            }
            installed = true;
        }
        private void Patch(Type type, int token, string prefix, string postfix)
        {
            var method = type.GetMethods(Flags).SingleOrDefault(m => m.MetadataToken == token);
            if (method == null) throw new MissingMethodException(type.FullName, token.ToString("X8"));
            Install(method, prefix, postfix);
        }
        private void PatchProduct(string name, string prefix, string postfix)
        {
            var method = typeof(NativeCombatActorPersistence).GetMethod(name, Flags);
            if (method == null) throw new MissingMethodException(typeof(NativeCombatActorPersistence).FullName, name);
            Install(method, prefix, postfix);
        }
        private void Install(MethodInfo method, string prefix, string postfix)
        {
            if (method == null) throw new MissingMethodException("Required native archive observation boundary is absent.");
            harmony.Patch(method, prefix == null ? null : new HarmonyMethod(typeof(Hooks).GetMethod(prefix, Flags)) { prioritiy = Priority.First },
                new HarmonyMethod(typeof(Hooks).GetMethod(postfix, Flags)) { prioritiy = Priority.Last });
            hooks.Add(new JObject { ["declaringType"] = method.DeclaringType.FullName, ["method"] = method.Name,
                ["token"] = method.MetadataToken.ToString("X8"), ["moduleMvid"] = method.Module.ModuleVersionId.ToString(),
                ["prefix"] = prefix, ["postfix"] = postfix });
        }
        private void Record(string kind, UnitEntityData actor)
        {
            try
            {
                if (events.Count >= MaximumEvents) { dropped++; return; }
                events.Add(new JObject { ["kind"] = kind, ["actorId"] = actor?.UniqueId, ["frame"] = Time.frameCount,
                    ["gameTicks"] = Game.Instance?.TimeController?.GameTime.Ticks,
                    ["restorePhase"] = persistence.ArchiveRestorePhase,
                    ["restorationPending"] = persistence.CombatRestorationPending,
                    ["debt"] = actor == null ? null : JObject.FromObject(MountedPersistenceService.CaptureActor(actor), MountedSaveCodec.CreateSerializer()),
                    ["prepared"] = actor?.CombatState.Prepared, ["inCombat"] = actor?.IsInCombat });
            }
            catch { errors++; }
        }
        private void RecordWorld(string kind)
        {
            try
            {
                if (events.Count >= MaximumEvents) { dropped++; return; }
                events.Add(new JObject { ["kind"] = kind, ["frame"] = Time.frameCount,
                    ["restorePhase"] = persistence.ArchiveRestorePhase,
                    ["snapshot"] = Chunk7ArchiveNativeSnapshot.Capture(world, persistence.LoadedArchive) });
            }
            catch { errors++; }
        }
        internal int Count(string kind, IEnumerable<string> actors)
        {
            var ids = new HashSet<string>(actors, StringComparer.Ordinal);
            return events.Count(e => (string)e["kind"] == kind && ids.Contains((string)e["actorId"]));
        }
        internal JObject Capture()
        {
            var value = new JObject { ["contract"] = "chunk7-native-restoration-entry-observer-v1",
            ["events"] = events.DeepClone(), ["hooks"] = hooks.DeepClone(), ["maximumEvents"] = MaximumEvents,
            ["dropped"] = dropped, ["errors"] = errors + (installed ? 0 : 1), ["complete"] = Complete,
            ["installedBeforeWorldLoad"] = true, ["prepareMetric"] = "native-method-entries-including-production-refused-attempts" };
            if (debtRequest != null) value["debtDiagnostics"] = new JObject {
                ["contract"] = "chunk7-native-debt-boundary-observer-v2", ["runId"] = debtRequest.RunId,
                ["events"] = debtEvents.DeepClone(), ["maximumEvents"] = MaximumDebtEvents, ["errors"] = debtErrors,
                ["dropped"] = debtDropped, ["openCalls"] = debtOpenCalls.Count, ["requestUnchanged"] = DebtRequestUnchanged(),
                ["complete"] = debtErrors == 0 && debtDropped == 0 && debtOpenCalls.Count == 0 && DebtIdentityCurrent(),
                ["scope"] = debtRequest.PersistenceCase == "pending-schema3-control-manual" ?
                    "exact-schema3-control-cold-after-presentation" : "exact-schema2-cold-after-presentation",
                ["methodEntryMayBeRefusedByProductionPrefix"] = true };
            return value;
        }
        internal static bool IsDebtDiagnosticRequest(RuntimeRequest request) =>
            request?.Scenario == Chunk7OwnedPairsSelection.Scenario && request.OwnedPairs?.IsOwnerSeedCheckpointLoad == true &&
            request.SelectedChunk7Scenario == Chunk7PersistenceScenario.LoadScenario &&
            (request.PersistenceCase == "legacy-mounted-pending-tb-manual" || request.PersistenceCase == "pending-schema3-control-manual") &&
            request.OwnedPairs.ExpectedNativeLoadCount == 1 && request.OwnedPairs.ExpectedNativeWriteCount ==
                (request.PersistenceCase == "pending-schema3-control-manual" ? 0 : 1);
        // The entire serialized immutable request binds source/seed ancestry, roles,
        // party, fixture and both campaign/area descriptors as well as quotas/token.
        private static string DebtRequestKey(RuntimeRequest request) => request == null ? null :
            JObject.FromObject(request, MountedSaveCodec.CreateSerializer()).ToString(Newtonsoft.Json.Formatting.None);
        private bool DebtRequestUnchanged() => IsDebtDiagnosticRequest(debtRequest) && DebtRequestKey(debtRequest) == debtRequestKey;
        private bool DebtIdentityCurrent()
        {
            try { ResolveDebtActors(); return true; }
            catch { return false; }
        }
        private sealed class DebtBoundaryCall
        {
            internal NativeArchiveRestorationProbe Owner;
            internal string Kind;
            internal UnitEntityData Actor;
            internal int Id;
        }
        private UnitEntityData[] ResolveDebtActors()
        {
            if (!DebtRequestUnchanged() || !ReferenceEquals(Game.Instance, debtGame))
                throw new InvalidOperationException("Debt-boundary request or native game changed.");
            var source = persistence.LoadedArchive;
            if (source?.Combat?.Actors == null || source.Combat.Actors.Length == 0 || source.Combat.Actors.Length > 64)
                throw new InvalidOperationException("Debt-boundary actor graph is absent or unbounded.");
            // Re-resolve both current native rosters on every event/completion.
            // A shared reference in both rosters is lawful; two objects with the
            // same saved ID are ambiguous, including an old Player alias.
            var units = Game.Instance.State.Units.Concat(Game.Instance.Player.AllCharacters).Where(unit => unit != null).ToArray();
            var resolved = source.Combat.Actors.Select(row => {
                var matches = units.Where(unit => unit.UniqueId == row.Native.Id).ToArray();
                if (matches.Length == 0 || matches.Any(unit => !ReferenceEquals(unit, matches[0])))
                    throw new InvalidOperationException("Debt-boundary exact current native actor is absent or ambiguous.");
                return matches[0];
            }).ToArray();
            if (debtActors == null) { debtActors = resolved; debtArchive = source; }
            if (!ReferenceEquals(debtArchive, source) || !source.Combat.Actors.Select(row => row.Native.Id)
                .SequenceEqual(debtActors.Select(actor => actor.UniqueId)) || resolved.Length != debtActors.Length ||
                resolved.Where((actor, index) => !ReferenceEquals(actor, debtActors[index])).Any())
                throw new InvalidOperationException("Debt-boundary loaded archive or exact actor identity changed.");
            return resolved;
        }
        private DebtBoundaryCall BeginDebtBoundary(string kind, UnitEntityData actor)
        {
            try
            {
                if (debtRequest == null || disposed || !installed || persistence.CombatRestorationPending ||
                    persistence.ArchiveRestorePhase != "all-pair-presentation-restored") return null;
                var actors = ResolveDebtActors();
                if (actor != null && !actors.Any(value => ReferenceEquals(value, actor))) return null;
                // Filtered prepare ticks still recheck the full request, current
                // rosters and flags; they serialize no native snapshots/events.
                if (kind == "native-combat-prepare" && !actors.Any(value => value.IsInCombat && !value.CombatState.Prepared)) return null;
                if (debtEvents.Count + debtOpenCalls.Count + 2 > MaximumDebtEvents) { debtDropped++; return null; }
                var call = new DebtBoundaryCall { Owner = this, Kind = kind, Actor = actor, Id = ++debtSequence };
                debtEvents.Add(CaptureDebtBoundary(call, "before")); debtOpenCalls.Add(call.Id);
                return call;
            }
            catch { debtErrors++; return null; }
        }
        private void EndDebtBoundary(DebtBoundaryCall call)
        {
            try
            {
                if (!ReferenceEquals(call?.Owner, this) || !ReferenceEquals(active, this) || !debtOpenCalls.Contains(call.Id))
                    throw new InvalidOperationException("Debt-boundary callback outlived its exact observer.");
                debtEvents.Add(CaptureDebtBoundary(call, "after")); debtOpenCalls.Remove(call.Id);
            }
            catch { debtErrors++; }
        }
        private JObject CaptureDebtBoundary(DebtBoundaryCall call, string edge)
        {
            var actors = ResolveDebtActors();
            var controller = Game.Instance.TurnBasedCombatController;
            // Only the exact selected post-restoration Join boundary records a
            // bounded method path. StackTrace(false) records no file paths or
            // arguments and does not replace, delay or suppress the native call.
            var callers = new JArray();
            if (call.Kind == "native-join-combat" && edge == "before")
                foreach (var frame in (new StackTrace(1, false).GetFrames() ?? new StackFrame[0]).Take(64))
                {
                    if (callers.Count == 24) break;
                    var method = frame.GetMethod();
                    if (method?.DeclaringType == null) continue;
                    var identity = method.DeclaringType.FullName + "." + method.Name;
                    if (identity.Length > 512) throw new InvalidOperationException("Native join caller identity exceeds the bounded observation.");
                    callers.Add(identity);
                }
            return new JObject { ["kind"] = call.Kind, ["edge"] = edge, ["callId"] = call.Id,
                ["actorId"] = call.Actor?.UniqueId, ["frame"] = Time.frameCount,
                ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks, ["paused"] = Game.Instance.IsPaused,
                ["nativePassing"] = TurnBased.Controllers.CombatController.IsPassing(),
                ["nativeRound"] = controller.RoundNumber, ["currentTurnActorId"] = controller.CurrentTurn?.Unit?.UniqueId,
                ["callerFrames"] = callers,
                ["restorePhase"] = persistence.ArchiveRestorePhase, ["restorationPending"] = persistence.CombatRestorationPending,
                ["actors"] = new JArray(actors.Select(actor => new JObject { ["native"] = JObject.FromObject(
                    MountedPersistenceService.CaptureActor(actor), MountedSaveCodec.CreateSerializer()),
                    ["inCombat"] = actor.IsInCombat, ["prepared"] = actor.CombatState.Prepared })) };
        }
        public void Dispose()
        {
            if (disposed) return;
            harmony?.UnpatchAll(HarmonyId);
            if (ReferenceEquals(active, this)) active = null;
            disposed = true;
        }
        private static class Hooks
        {
            internal static void DebtJoinBefore(UnitEntityData __instance, out DebtBoundaryCall __state) =>
                __state = active?.BeginDebtBoundary("native-join-combat", __instance);
            internal static void DebtJoinAfter(DebtBoundaryCall __state) { if (__state != null) __state.Owner.EndDebtBoundary(__state); }
            internal static void DebtPrepareBefore(out DebtBoundaryCall __state) =>
                __state = active?.BeginDebtBoundary("native-combat-prepare", null);
            internal static void DebtPrepareAfter(DebtBoundaryCall __state) { if (__state != null) __state.Owner.EndDebtBoundary(__state); }
            internal static void PrepareBefore(TurnController __instance) => active?.Record("prepare-before", __instance.Unit);
            internal static void PrepareAfter(TurnController __instance) => active?.Record("prepare-after", __instance.Unit);
            internal static void RoundBefore(UnitCombatState __instance) => active?.Record("round-before", __instance.Unit);
            internal static void RoundAfter(UnitCombatState __instance) => active?.Record("round-after", __instance.Unit);
            internal static void RestoreActorBefore(UnitEntityData unit) => active?.Record("restore-actor-before", unit);
            internal static void RestoreActorAfter(UnitEntityData unit) => active?.Record("restore-actor-after", unit);
            internal static void RestoreDebtBefore(UnitEntityData unit) => active?.Record("restore-debt-before", unit);
            internal static void RestoreDebtAfter(UnitEntityData unit) => active?.Record("restore-debt-after", unit);
            internal static void SharedWorldBefore(MountedPersistenceService __instance, out string __state)
            {
                __state = ReferenceEquals(active?.persistence, __instance) ? __instance.ArchiveRestorePhase : null;
            }
            internal static void SharedWorldAfter(MountedPersistenceService __instance, string __state)
            {
                if (ReferenceEquals(active?.persistence, __instance) && __state != null &&
                    __state != "shared-and-pair-participation-restored" &&
                    __instance.ArchiveRestorePhase == "shared-and-pair-participation-restored")
                    active.RecordWorld("shared-world-restored");
            }
            internal static void PresentationBefore(MountedPersistenceService __instance, out string __state)
            {
                __state = ReferenceEquals(active?.persistence, __instance) ? __instance.ArchiveRestorePhase : null;
            }
            internal static void PresentationAfter(MountedPersistenceService __instance, bool __result, string __state)
            {
                if (__result && ReferenceEquals(active?.persistence, __instance) && __state != null &&
                    __state != "all-pair-presentation-restored" && __instance.ArchiveRestorePhase == "all-pair-presentation-restored")
                    active.RecordWorld("all-owner-presentation-restored");
            }
        }
    }
}
