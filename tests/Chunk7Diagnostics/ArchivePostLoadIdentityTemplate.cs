using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KingmakerMountedCombat.Domain;

// Actual production methods are inserted below. Only the native graph, saved DTO
// selection and native restoration boundary are substitutes; no engine runs.
namespace KingmakerMountedCombat.Integration
{
    internal sealed class UnitEntityData
    {
        internal readonly string UniqueId;
        internal int Debt = 7;
        internal int Restores;
        internal bool ReferencesValid = true;
        internal UnitEntityData(string id) { UniqueId = id; }
    }
    internal sealed class Player { internal readonly List<UnitEntityData> AllCharacters = new List<UnitEntityData>(); }
    internal sealed class State { internal readonly List<UnitEntityData> Units = new List<UnitEntityData>(); }
    internal sealed class Game
    {
        internal static Game Instance;
        internal Player Player;
        internal State State;
    }
    internal sealed class SavedNativeActor { internal string Id; }
    internal sealed class SavedCombatActor { internal SavedNativeActor Native; }
    internal sealed class SavedCombatData { internal SavedCombatActor[] Actors; }
    internal sealed class SavedMountedPair { internal SavedNativeActor Rider, Mount; }
    internal sealed class SavedSlot { internal string ActorId; }
    internal sealed class MountedArchiveData
    {
        internal SavedCombatData Combat;
        internal SavedMountedPair[] Pairs;
        internal SavedSlot[] Slots = new SavedSlot[0];
        internal long GameTimeTicks = 123;
        internal void Validate() { }
    }
    internal sealed class LoadScope
    {
        internal int Sequence;
        internal NativeLoadWorld<Player> World = new NativeLoadWorld<Player>();
    }
    internal static class NativeCombatActorPersistence
    {
        internal static readonly List<string> Calls = new List<string>();
        internal static void ValidateActorReferences(UnitEntityData actor, SavedCombatActor saved)
        {
            Calls.Add("validate:" + actor.UniqueId);
            if (!actor.ReferencesValid) throw new InvalidDataException("generated incompatible actor references");
        }
        internal static void RestoreActor(UnitEntityData actor, SavedCombatActor saved, long ticks)
        { Apply(actor, "actor:"); }
        internal static void RestoreDebt(UnitEntityData actor, SavedNativeActor saved)
        { Apply(actor, "debt:"); }
        private static void Apply(UnitEntityData actor, string label)
        { Calls.Add(label + actor.UniqueId); actor.Debt = 11; actor.Restores++; }
    }
    internal sealed class MountedPersistenceService
    {
        internal bool Enabled = true;
        internal MountedArchiveData Selected;
        private MountedArchiveData RestorableArchive => Selected;
        private object loadedArchive;
        private bool archiveGraphReady, archiveOwnersReady;
        private readonly Dictionary<string, UnitEntityData> archiveActors = new Dictionary<string, UnitEntityData>(StringComparer.Ordinal);
        private readonly Dictionary<string, object> archiveOwners = new Dictionary<string, object>(StringComparer.Ordinal);
        __POSTLOAD_FIELD__
        private readonly Dictionary<string, UnitEntityData> restoredActors = new Dictionary<string, UnitEntityData>(StringComparer.Ordinal);
        internal string ArchiveRestorePhase = "not-selected";
        internal int SemanticRestoreCount;
        internal string combatRestoreFailure;
        // The retained pending re-entry guard is released with the rest of the archive custody.
        internal object retainedPendingReentry;
        internal LoadScope restoreLoad;
        internal int loadSequence;
        internal int Observed => archivePostLoadObserved.Count;
        internal int Resolved => archiveActors.Count;
        internal void Observe(UnitEntityData unit) { RestoreArchiveActorAfterPostLoad(unit); }
        internal void Clear() { ClearArchiveRestoration(); }
        private void ClearArchivePresentationReadiness() { }
        private void BlockCombatRestoration(string reason) { combatRestoreFailure = reason; }
        __METHODS__
    }
    internal sealed class Fixture
    {
        internal readonly MountedPersistenceService Service = new MountedPersistenceService();
        internal readonly Player OldPlayer = new Player();
        internal readonly Player Player = new Player();
        internal readonly State State = new State();
        internal readonly UnitEntityData[] Actors;
        internal Fixture(int pairs = 1, bool combat = false)
        {
            NativeCombatActorPersistence.Calls.Clear();
            Actors = Enumerable.Range(0, pairs * 2).Select(i => new UnitEntityData("actor" + i)).ToArray();
            var snapshots = Actors.Select(a => new SavedNativeActor { Id = a.UniqueId }).ToArray();
            Service.Selected = new MountedArchiveData {
                Pairs = Enumerable.Range(0, pairs).Select(i => new SavedMountedPair { Rider = snapshots[2*i], Mount = snapshots[2*i+1] }).ToArray(),
                Combat = combat ? new SavedCombatData { Actors = snapshots.Select(a => new SavedCombatActor { Native = a }).ToArray() } : null
            };
            Service.loadSequence = 3;
            Service.restoreLoad = new LoadScope { Sequence = 3 };
            Service.restoreLoad.World.Begin(OldPlayer);
            Game.Instance = new Game { State = State, Player = Player };
        }
        internal void Publish(params UnitEntityData[] units) { State.Units.AddRange(units); }
        internal void Observe(int index) { Service.Observe(Actors[index]); }
        internal bool Untouched(params UnitEntityData[] more) => Actors.Concat(more).All(a => a.Debt == 7 && a.Restores == 0) && Service.SemanticRestoreCount == 0;
        internal bool Blocked => Service.combatRestoreFailure != null;
    }
    internal static class ArchivePostLoadIdentityTests
    {
        private static int passed, failed;
        private static void Assert(bool value, string message) { if (!value) throw new Exception(message); }
        private static void Run(string name, Action test)
        {
            try { test(); passed++; Console.WriteLine("PASS " + name); }
            catch (Exception e) { failed++; Console.WriteLine("FAIL " + name + ": " + e.Message); }
        }
        public static int Main()
        {
            Run("two pairs wait for every exact actor then restore each once", () => {
                var f = new Fixture(2); f.Publish(f.Actors);
                for (int i = 0; i < 3; i++) { f.Observe(i); Assert(f.Untouched(), "mutation before all callbacks"); }
                f.Observe(3);
                Assert(!f.Blocked && f.Service.SemanticRestoreCount == 4 && f.Actors.All(a => a.Debt == 11 && a.Restores == 1), "incomplete or repeated restore");
                foreach (var a in f.Actors) f.Service.Observe(a);
                Assert(f.Service.SemanticRestoreCount == 4 && f.Actors.All(a => a.Restores == 1), "duplicate callback replayed restoration");
            });
            Run("missing graph actor waits without native mutation", () => {
                var f = new Fixture(); f.Publish(f.Actors[0]); f.Observe(0);
                Assert(f.Service.Observed == 1 && f.Service.Resolved == 0 && !f.Blocked && f.Untouched(), "missing graph was admitted");
                f.Publish(f.Actors[1]); f.Observe(1);
                Assert(!f.Blocked && f.Actors.All(a => a.Restores == 1), "stable delayed graph failed");
            });
            Run("replacement before graph resolution cannot inherit an earlier callback", () => {
                var f = new Fixture(); var replacement = new UnitEntityData(f.Actors[0].UniqueId);
                f.Publish(f.Actors[0]); f.Observe(0); f.State.Units.Clear(); f.Publish(replacement, f.Actors[1]); f.Observe(1);
                Assert(f.Blocked && f.Untouched(replacement), "replacement received unobserved restoration");
            });
            Run("foreign early callback cannot authorize the eventual graph actor", () => {
                var f = new Fixture(); var foreign = new UnitEntityData(f.Actors[0].UniqueId);
                f.Publish(f.Actors[0]); f.Service.Observe(foreign); f.Publish(f.Actors[1]); f.Observe(1);
                Assert(f.Blocked && f.Untouched(foreign), "foreign object granted graph restoration");
            });
            Run("conflicting same-ID callbacks fail while graph still waits", () => {
                var f = new Fixture(); var replacement = new UnitEntityData(f.Actors[0].UniqueId);
                f.Publish(f.Actors[0]); f.Observe(0); f.Service.Observe(replacement);
                Assert(f.Blocked && f.Untouched(replacement), "conflicting callback was retained without refusal");
            });
            Run("same-object callbacks remain idempotent while graph waits", () => {
                var f = new Fixture(); f.Publish(f.Actors[0]); f.Observe(0); f.Observe(0);
                Assert(!f.Blocked && f.Service.Observed == 1 && f.Untouched(), "exact repeated callback failed");
                f.Publish(f.Actors[1]); f.Observe(1);
                Assert(!f.Blocked && f.Service.SemanticRestoreCount == 2 && f.Actors.All(a => a.Restores == 1), "exact delayed callbacks replayed or omitted debt");
            });
            Run("foreign callback after graph resolution is refused", () => {
                var f = new Fixture(); f.Publish(f.Actors); f.Observe(0);
                var foreign = new UnitEntityData(f.Actors[1].UniqueId); f.Service.Observe(foreign);
                Assert(f.Blocked && f.Untouched(foreign), "foreign resolved callback restored debt");
            });
            Run("resolved graph replacement before final callback is refused", () => {
                var f = new Fixture(); f.Publish(f.Actors); f.Observe(0);
                var replacement = new UnitEntityData(f.Actors[0].UniqueId); f.State.Units.Remove(f.Actors[0]); f.Publish(replacement); f.Observe(1);
                Assert(f.Blocked && f.Untouched(replacement), "changed resolved graph was restored");
            });
            Run("all combat references validate before first native restoration", () => {
                var f = new Fixture(1, true); f.Publish(f.Actors); f.Observe(0); f.Observe(1);
                Assert(!f.Blocked && NativeCombatActorPersistence.Calls.SequenceEqual(new[] {"validate:actor0", "validate:actor1", "actor:actor0", "actor:actor1"}), "semantic validation/mutation order changed");
            });
            Run("invalid final combat actor leaves every actor expenditure intact", () => {
                var f = new Fixture(1, true); f.Actors[1].ReferencesValid = false; f.Publish(f.Actors); f.Observe(0); f.Observe(1);
                Assert(f.Blocked && f.Untouched() && NativeCombatActorPersistence.Calls.Count == 2, "partial native restoration preceded validation");
                f.Actors[1].ReferencesValid = true; f.Observe(1);
                Assert(f.Untouched() && NativeCombatActorPersistence.Calls.Count == 2, "failure fence allowed a retry to restore");
            });
            Run("unrelated and null actors do not count as saved callbacks", () => {
                var f = new Fixture(); f.Publish(f.Actors); f.Service.Observe(new UnitEntityData("unrelated")); f.Service.Observe(null);
                Assert(f.Service.Observed == 0 && !f.Blocked && f.Untouched(), "unrelated actor entered observation set");
            });
            Run("old, missing and replaced Player worlds cannot credit callbacks", () => {
                var f = new Fixture(); f.Publish(f.Actors); Game.Instance.Player = f.OldPlayer; f.Observe(0);
                Game.Instance.Player = null; f.Observe(0); Assert(f.Service.Observed == 0 && f.Untouched(), "invalid world credited callback");
                Game.Instance.Player = f.Player; f.Observe(0); Game.Instance.Player = new Player(); f.Observe(1);
                Assert(f.Service.Observed == 1 && f.Untouched(), "new Player inherited prior load identity");
            });
            Run("stale sequence and closed load cannot credit callbacks", () => {
                var f = new Fixture(); f.Publish(f.Actors); f.Service.restoreLoad.Sequence = 2; f.Observe(0);
                f.Service.restoreLoad.Sequence = 3; f.Service.restoreLoad.World.Close(); f.Observe(0);
                Assert(f.Service.Observed == 0 && f.Untouched(), "stale load credited callback");
            });
            Run("disabled or nonrestoring selection cannot credit callbacks", () => {
                var f = new Fixture(); f.Publish(f.Actors); f.Service.Enabled = false; f.Observe(0);
                f.Service.Enabled = true; f.Service.Selected = null; f.Observe(0);
                Assert(f.Service.Observed == 0 && f.Untouched(), "nonrestoring selection was admitted");
            });
            Run("archive reset clears retained PostLoad and graph custody", () => {
                var f = new Fixture(); f.Publish(f.Actors); f.Observe(0);
                Assert(f.Service.Observed == 1 && f.Service.Resolved == 2, "setup did not retain exact pending state");
                f.Service.retainedPendingReentry = new object(); f.Service.Clear(); Assert(f.Service.Observed == 0 && f.Service.Resolved == 0 && f.Service.ArchiveRestorePhase == "not-selected" && f.Service.retainedPendingReentry == null, "clear retained callback custody");
            });
            Run("duplicate graph actor IDs are refused before restoration", () => {
                var f = new Fixture(); var duplicate = new UnitEntityData(f.Actors[0].UniqueId); f.Publish(f.Actors); f.Publish(duplicate); f.Observe(0);
                Assert(f.Blocked && f.Untouched(duplicate), "ambiguous graph actor was restored");
            });
            Console.WriteLine("ARCHIVE POSTLOAD IDENTITY PASS=" + passed + " FAIL=" + failed);
            return failed == 0 && passed != 0 ? 0 : 1;
        }
    }
}
