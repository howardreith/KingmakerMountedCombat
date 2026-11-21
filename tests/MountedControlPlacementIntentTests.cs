using System;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // Toolbar persistence of the public Mount / Dismount control across temporary absence (city visits, area changes,
    // save scopes, the capital's reserve roster). The toolbar below is a labeled test double of one rider's native
    // UnitUISettings.Slots: null is empty, and Absent() also empties the control's slot, as native GetSlot /
    // CleanupBadSlots do once its fact is gone. Reconcile mirrors NativeMountedControlService.ReconcileChosenSlot.
    internal static class MountedControlPlacementIntentTests
    {
        private const string Control = "KMC_MountState";

        internal static void Register(TestRunner runner)
        {
            runner.Run("chosen nondefault toolbar slot survives repeated city entry and exit", SurvivesRepeatedCityVisits);
            runner.Run("chosen slot survives a save and load made while the rider is absent", SurvivesSaveLoadWhileAbsent);
            runner.Run("a moved shortcut returns to its new slot after an absence", RespectsMove);
            runner.Run("a deliberately removed shortcut is never re-added", RespectsRemoval);
            runner.Run("an occupied remembered slot is never overwritten and the control is not duplicated", RespectsOccupiedSlot);
            runner.Run("another rider's arrangement is independent and unrelated slots stay unchanged", RidersIndependent);
            runner.Run("permanent loss of eligibility and disable forget the chosen slot", PermanentLossForgets);
            runner.Run("no remembered slot means no automatic insertion", NoIntentNoInsertion);
        }

        private sealed class Toolbar
        {
            internal readonly string[] Slots = new string[140];
            internal readonly string Actor;
            internal readonly MountedControlPlacementIntent Intent;
            internal Toolbar(string actor, MountedControlPlacementIntent intent) { Actor = actor; Intent = intent; }

            internal int Find() => Array.IndexOf(Slots, Control);

            // The control's fact is temporarily gone: native empties its slot.
            internal void Absent(MountedControlAbsence absence = MountedControlAbsence.Temporary)
            {
                var at = Find();
                if (at >= 0) Slots[at] = null;
                Intent.ObserveAbsent(Actor, absence);
            }

            // The control is live again (or still live): the service's reconciliation.
            internal void Reconcile()
            {
                var bound = Find();
                if (bound >= 0) { Intent.ObserveBound(Actor, bound); return; }
                var index = Intent.ResolveUnbound(Actor, i => i >= 0 && i < Slots.Length && Slots[i] == null);
                if (index < 0) return;
                Slots[index] = Control;
                Intent.ObserveBound(Actor, index);
            }
        }

        private static void SurvivesRepeatedCityVisits()
        {
            var bar = new Toolbar("rider", new MountedControlPlacementIntent());
            bar.Slots[0] = "Attack"; bar.Slots[9] = Control; bar.Slots[10] = "Spell";
            bar.Reconcile();
            for (var visit = 0; visit < 3; visit++)
            {
                // Entering the city (area unload, or the capital moving the rider to the reserve roster) and returning.
                bar.Absent();
                TestRunner.Equal(-1, bar.Find(), "The test double did not model the native emptying of the slot.");
                bar.Reconcile();
                TestRunner.Equal(9, bar.Find(), "The control did not return to the chosen slot on visit " + visit + ".");
                TestRunner.True(bar.Slots[0] == "Attack" && bar.Slots[10] == "Spell", "An unrelated slot changed.");
            }
            // An ordinary interior transition while mounted is the same temporary absence.
            bar.Absent(); bar.Reconcile();
            TestRunner.Equal(9, bar.Find(), "An interior transition lost the chosen slot.");
        }

        private static void SurvivesSaveLoadWhileAbsent()
        {
            var session = new MountedControlPlacementIntent();
            var bar = new Toolbar("rider", session);
            bar.Slots[12] = Control; bar.Reconcile();
            bar.Absent();
            int saved;
            TestRunner.True(session.TryGetInterrupted("rider", out saved) && saved == 12,
                "A save made while the rider is absent does not carry its chosen slot.");
            // A new load starts from the saved row only (BeginLoadedWorld clears, RestorePersistentSlots seeds).
            var loaded = new MountedControlPlacementIntent();
            loaded.Seed("rider", saved);
            var after = new Toolbar("rider", loaded);
            after.Reconcile();
            TestRunner.Equal(12, after.Find(), "Loading did not return the control to its chosen slot.");
            // A live bound control is not an absence: a save made now writes only its live slot.
            TestRunner.True(!loaded.TryGetInterrupted("rider", out saved), "A live control was also written as an absent intent.");
        }

        private static void RespectsMove()
        {
            var bar = new Toolbar("rider", new MountedControlPlacementIntent());
            bar.Slots[9] = Control; bar.Reconcile();
            bar.Slots[9] = null; bar.Slots[3] = Control; bar.Reconcile();
            bar.Absent(); bar.Reconcile();
            TestRunner.Equal(3, bar.Find(), "A moved control returned to its old slot.");
            TestRunner.True(bar.Slots[9] == null, "The old slot was filled again.");
        }

        private static void RespectsRemoval()
        {
            var bar = new Toolbar("rider", new MountedControlPlacementIntent());
            bar.Slots[9] = Control; bar.Reconcile();
            // The player drags the live control off the bar.
            bar.Slots[9] = null; bar.Reconcile();
            TestRunner.Equal(-1, bar.Find(), "A removed control was put back immediately.");
            bar.Absent(); bar.Reconcile();
            bar.Absent(); bar.Reconcile();
            TestRunner.Equal(-1, bar.Find(), "A removed control was re-added after an absence.");
            TestRunner.Equal(0, bar.Intent.Count, "The removal left a remembered slot behind.");
        }

        private static void RespectsOccupiedSlot()
        {
            var bar = new Toolbar("rider", new MountedControlPlacementIntent());
            bar.Slots[9] = Control; bar.Reconcile();
            bar.Absent();
            // While the control was away, the player put another ability in that slot.
            bar.Slots[9] = "Cure Light Wounds";
            bar.Reconcile();
            TestRunner.True(bar.Slots[9] == "Cure Light Wounds", "Another ability was overwritten.");
            TestRunner.Equal(-1, bar.Find(), "The control was placed somewhere else instead.");
            bar.Absent(); bar.Slots[9] = null; bar.Reconcile();
            TestRunner.Equal(-1, bar.Find(), "A forgotten slot was used later.");
            // Never two copies: a live bound control keeps its one slot.
            bar.Slots[4] = Control; bar.Reconcile(); bar.Reconcile();
            TestRunner.Equal(1, Array.FindAll(bar.Slots, s => s == Control).Length, "The control was duplicated.");
        }

        private static void RidersIndependent()
        {
            var intent = new MountedControlPlacementIntent();
            var first = new Toolbar("first", intent);
            var second = new Toolbar("second", intent);
            first.Slots[9] = Control; first.Reconcile();
            second.Slots[2] = Control; second.Slots[9] = "Bow"; second.Reconcile();
            // Only the first rider leaves (reserve roster); the second stays in the party with a live control.
            first.Absent();
            second.Reconcile();
            first.Reconcile();
            TestRunner.Equal(9, first.Find(), "The first rider lost its slot.");
            TestRunner.True(second.Find() == 2 && second.Slots[9] == "Bow", "The second rider's bar changed.");
            // The second rider removes its own control; the first is unaffected.
            second.Slots[2] = null; second.Reconcile();
            first.Absent(); first.Reconcile();
            TestRunner.True(first.Find() == 9 && second.Find() == -1, "One rider's removal affected the other.");
        }

        private static void PermanentLossForgets()
        {
            var bar = new Toolbar("rider", new MountedControlPlacementIntent());
            bar.Slots[9] = Control; bar.Reconcile();
            // The companion is genuinely removed (no supported mount any more, or the rider left every roster).
            bar.Absent(MountedControlAbsence.Permanent);
            bar.Reconcile();
            TestRunner.Equal(-1, bar.Find(), "A permanently ineligible rider got the control back.");
            var disabled = new Toolbar("rider", new MountedControlPlacementIntent());
            disabled.Slots[9] = Control; disabled.Reconcile();
            disabled.Absent();
            disabled.Intent.Clear(); // SetEnabled(false), Dispose and a new load clear the records.
            disabled.Reconcile();
            TestRunner.Equal(-1, disabled.Find(), "Disable cleanup was undone from a remembered slot.");
            int index;
            TestRunner.True(!disabled.Intent.TryGetInterrupted("rider", out index), "A cleared record is still written.");
        }

        private static void NoIntentNoInsertion()
        {
            var bar = new Toolbar("rider", new MountedControlPlacementIntent());
            bar.Reconcile();
            bar.Absent(); bar.Reconcile();
            TestRunner.Equal(-1, bar.Find(), "A control without a remembered slot was inserted.");
            var intent = new MountedControlPlacementIntent();
            TestRunner.Equal(-1, intent.ResolveUnbound(null, i => true), "A missing actor resolved a slot.");
            intent.Seed("rider", -1);
            TestRunner.Equal(0, intent.Count, "An invalid saved index was remembered.");
        }
    }
}
