using System;
using System.Collections.Generic;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // Save/load and entrance repair (2026-11-12). OFFLINE decision evidence for the production admission and carry
    // policies, driven with the exact native orders from the installed assembly (Game.LoadGame 06000CE0 -> full
    // Game.LoadArea 06000CD5 with the selected SaveInfo; StartAreaTransition -> SelectAll -> RunUnitTransitionCommand per
    // selected unit). No native load, save or entrance click was executed by these tests.
    internal static class SaveLoadEntranceRepairTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("save/load: an in-session load during a prepared paired combat turn is not refused by the travel guard", LoadDuringPairedTurnIsReplacementNotTravel);
            runner.Run("save/load: a latched restore failure does not refuse a load (load housekeeping is its recovery) but still refuses travel", LatchedFailureRefusesTravelOnly);
            runner.Run("save/load: an archive worker, a queued save, charge custody and an incompatible archive still refuse a load", ReplacementKeepsProtectiveRefusals);
            runner.Run("save/load: a load refused by a writer, a queued save or charge custody reads no archive and releases nothing", RefusedLoadReadsAndReleasesNothing);
            runner.Run("save/load: retained charge cleanup on a secondary pair refuses a load with no charge fence until it settles", SecondaryRetainedChargeRefusesLoad);
            runner.Run("save/load: every pair's charge cleanup is drained only after the save guards admitted", ChargeDrainFollowsSaveGuards);
            runner.Run("save/load: area travel keeps the 0.2.4 terms, order and lazy evaluation", TravelKeepsOrderAndLaziness);
            runner.Run("save/load: only a non-null SaveInfo is a world replacement", ClassificationBySaveInfo);
            runner.Run("entrance: a mounted rider's entrance command gives its horse the native member command once", RiderCommandCarriesHorseOnce);
            runner.Run("entrance: an unmounted rider, the horse's own command, other commands and combat are untouched", EntranceNotApplicableCases);
            runner.Run("entrance: a refused, replaced, started or finished group and a horse outside the group get no carrier", EntranceRefusalsNeverOrderTheHorse);
        }

        // Log 3 lines 10911-10916: a prepared paired activation, then four refusals of in-session loads.
        private static void LoadDuringPairedTurnIsReplacementNotTravel()
        {
            var participationAsked = false;
            var travel = MountedWorldBoundaryAdmissionPolicy.DecideTravel(false, () => { participationAsked = true; return true; },
                false, false, false, () => true);
            TestRunner.Equal(MountedWorldBoundaryRefusal.ParticipationUnsettled, travel, "The 0.2.4 travel guard no longer refuses an unsettled paired turn.");
            TestRunner.True(participationAsked, "Travel did not consult participation.");

            var load = MountedWorldBoundaryAdmissionPolicy.DecideSavedReplacement(() => true, false, false, false, false, () => true);
            TestRunner.Equal(MountedWorldBoundaryRefusal.None, load, "A valid in-session load during a paired combat turn was refused.");
        }

        private static void LatchedFailureRefusesTravelOnly()
        {
            TestRunner.Equal(MountedWorldBoundaryRefusal.RestoreFailureLatched,
                MountedWorldBoundaryAdmissionPolicy.DecideTravel(true, () => false, false, false, false, () => true),
                "A latched restore failure stopped protecting area travel.");
            TestRunner.Equal(MountedWorldBoundaryRefusal.None,
                MountedWorldBoundaryAdmissionPolicy.DecideSavedReplacement(() => true, false, false, false, false, () => true),
                "The documented recovery (load a valid save) is still refused while the failure is latched.");
        }

        private static void ReplacementKeepsProtectiveRefusals()
        {
            MountedWorldBoundaryRefusal Load(bool scope = false, bool draining = false, bool fence = false, bool charge = false, bool selected = true,
                bool drained = true) =>
                MountedWorldBoundaryAdmissionPolicy.DecideSavedReplacement(() => selected, scope, draining, fence, charge, () => drained);
            TestRunner.Equal(MountedWorldBoundaryRefusal.SaveScopeActive, Load(scope: true), "A load raced a save being written.");
            TestRunner.Equal(MountedWorldBoundaryRefusal.SaveDraining, Load(draining: true), "A load raced an archive worker that can still commit.");
            TestRunner.Equal(MountedWorldBoundaryRefusal.SaveAdmissionFence, Load(fence: true), "A load raced a queued save admission.");
            TestRunner.Equal(MountedWorldBoundaryRefusal.ChargeFence, Load(charge: true), "A load discarded unresolved charge custody.");
            TestRunner.Equal(MountedWorldBoundaryRefusal.ChargeCleanupUnresolved, Load(drained: false), "A load discarded retained charge cleanup.");
            TestRunner.Equal(MountedWorldBoundaryRefusal.SelectedLoadRefused, Load(selected: false), "An incompatible or unavailable archive replaced the world.");
        }

        // Independent review F1/F2: admission itself never releases the outgoing world (WrapLoadRoutine does, after retiring
        // the previous load scope), and a refusing fence is decided before the selected archive is even read.
        private static void RefusedLoadReadsAndReleasesNothing()
        {
            foreach (var refusing in new[] { "scope", "draining", "fence", "charge" })
            {
                var archiveReads = 0; var drains = 0;
                var refusal = MountedWorldBoundaryAdmissionPolicy.DecideSavedReplacement(() => { archiveReads++; return true; },
                    refusing == "scope", refusing == "draining", refusing == "fence", refusing == "charge", () => { drains++; return true; });
                TestRunner.True(refusal != MountedWorldBoundaryRefusal.None, refusing + " did not refuse the load.");
                TestRunner.Equal(0, archiveReads, "The selected archive was read while " + refusing + " still owned the world.");
                TestRunner.Equal(0, drains, "Charge cleanup was drained while " + refusing + " still owned the world.");
            }
            var reads = 0;
            TestRunner.Equal(MountedWorldBoundaryRefusal.None,
                MountedWorldBoundaryAdmissionPolicy.DecideSavedReplacement(() => { reads++; return true; }, false, false, false, false, () => true),
                "A settled load was refused.");
            TestRunner.Equal(1, reads, "The selected-load admission was not consulted exactly once.");
        }

        // Review of PR #31 (P2): pair A (primary) is clean; pair B retains charge ownership because its native ability
        // process has not ended, and holds NO charge fence. The world drain visits every pair, as the production
        // DrainWorldCharge does; the load is refused (so the native unload never begins) until B settles. The detached
        // probe in Test-ChargeOwnershipContracts drives the same decision over two real MountedCombatController owners.
        private sealed class ChargeOwnerModel
        {
            internal bool Owned, ProcessEnded; internal int Attempts;
            internal bool TryDrain() { Attempts++; if (Owned && ProcessEnded) Owned = false; return !Owned; }
        }

        private static bool DrainEvery(IEnumerable<ChargeOwnerModel> owners)
        {
            var settled = true;
            foreach (var owner in owners) if (!owner.TryDrain()) settled = false;
            return settled;
        }

        private static void SecondaryRetainedChargeRefusesLoad()
        {
            var primary = new ChargeOwnerModel();
            var secondary = new ChargeOwnerModel { Owned = true };
            var owners = new[] { primary, secondary };
            var unloads = 0;
            Func<MountedWorldBoundaryRefusal> load = () =>
            {
                var refusal = MountedWorldBoundaryAdmissionPolicy.DecideSavedReplacement(() => !primary.Owned, false, false, false,
                    false, () => DrainEvery(owners)); // no pair holds a charge fence
                if (refusal == MountedWorldBoundaryRefusal.None) unloads++;
                return refusal;
            };
            TestRunner.Equal(MountedWorldBoundaryRefusal.ChargeCleanupUnresolved, load(),
                "A secondary pair's retained charge cleanup (no fence) did not refuse the load; the primary-only admission passed.");
            TestRunner.Equal(0, unloads, "The native unload began over retained charge cleanup.");
            TestRunner.True(primary.Attempts == 1 && secondary.Attempts == 1, "The world drain did not visit every pair exactly once.");
            secondary.ProcessEnded = true;
            TestRunner.Equal(MountedWorldBoundaryRefusal.None, load(), "The load stayed refused after the secondary pair's cleanup settled.");
            TestRunner.Equal(1, unloads, "The settled load did not proceed to the native unload exactly once.");
            // Control: an ordinary paired combat turn (unsettled participation, no charge) is still replaced by a valid load.
            TestRunner.Equal(MountedWorldBoundaryRefusal.None,
                MountedWorldBoundaryAdmissionPolicy.DecideSavedReplacement(() => true, false, false, false, false, () => DrainEvery(new[] { new ChargeOwnerModel() })),
                "An ordinary paired combat turn with no charge custody could not be replaced by a valid load.");
        }

        private static void ChargeDrainFollowsSaveGuards()
        {
            var order = new List<string>();
            MountedWorldBoundaryAdmissionPolicy.DecideSavedReplacement(() => { order.Add("selected"); return true; }, false, false, false, false,
                () => { order.Add("drain"); return true; });
            TestRunner.True(order.Count == 2 && order[0] == "drain" && order[1] == "selected",
                "The world charge drain did not run exactly once, after the save guards and before the selected archive.");
            order.Clear();
            TestRunner.Equal(MountedWorldBoundaryRefusal.ChargeCleanupUnresolved,
                MountedWorldBoundaryAdmissionPolicy.DecideSavedReplacement(() => { order.Add("selected"); return true; }, false, false, false, false,
                    () => { order.Add("drain"); return false; }), "Unresolved charge cleanup did not refuse the load.");
            TestRunner.True(order.Count == 1 && order[0] == "drain", "The selected archive was read after charge cleanup refused.");
        }

        private static void TravelKeepsOrderAndLaziness()
        {
            var asked = new List<string>();
            Func<bool> Participation(bool unsettled) => () => { asked.Add("participation"); return unsettled; };
            Func<bool> Drain(bool drained) => () => { asked.Add("drain"); return drained; };
            MountedWorldBoundaryAdmissionPolicy.DecideTravel(true, Participation(false), false, false, false, Drain(true));
            TestRunner.Equal(0, asked.Count, "A latched failure still evaluated participation or the charge drain (0.2.4 short-circuited both).");
            asked.Clear();
            TestRunner.Equal(MountedWorldBoundaryRefusal.SaveScopeActive,
                MountedWorldBoundaryAdmissionPolicy.DecideTravel(false, Participation(false), true, false, false, Drain(true)), "A save scope did not refuse travel.");
            TestRunner.True(asked.Count == 1 && asked[0] == "participation", "Travel evaluated the charge drain before an earlier refusing term.");
            asked.Clear();
            TestRunner.Equal(MountedWorldBoundaryRefusal.SaveAdmissionFence,
                MountedWorldBoundaryAdmissionPolicy.DecideTravel(false, Participation(false), false, true, false, Drain(true)), "A save fence did not refuse travel.");
            TestRunner.Equal(MountedWorldBoundaryRefusal.ChargeFence,
                MountedWorldBoundaryAdmissionPolicy.DecideTravel(false, Participation(false), false, false, true, Drain(true)), "A charge fence did not refuse travel.");
            TestRunner.Equal(MountedWorldBoundaryRefusal.ChargeCleanupUnresolved,
                MountedWorldBoundaryAdmissionPolicy.DecideTravel(false, Participation(false), false, false, false, Drain(false)), "An unresolved charge did not refuse travel.");
            asked.Clear();
            TestRunner.Equal(MountedWorldBoundaryRefusal.None,
                MountedWorldBoundaryAdmissionPolicy.DecideTravel(false, Participation(false), false, false, false, Drain(true)), "Settled travel was refused.");
            TestRunner.True(asked.Count == 2 && asked[0] == "participation" && asked[1] == "drain", "Admitted travel did not evaluate participation then the drain, once each.");
        }

        private static void ClassificationBySaveInfo()
        {
            TestRunner.Equal(MountedWorldBoundaryKind.SavedWorldReplacement, MountedWorldBoundaryAdmissionPolicy.Classify(true), "Game.LoadGame's call was treated as travel.");
            TestRunner.Equal(MountedWorldBoundaryKind.AreaTravel, MountedWorldBoundaryAdmissionPolicy.Classify(false), "A door or world-map arrival was treated as a load.");
        }

        private static MountedEntranceCarryDecision Carry(bool mounted = true, bool riderContainer = true, bool exact = true, bool combat = false,
            bool live = true, bool pending = true, bool member = true, bool mountOrdered = false) =>
            MountedEntranceCarryPolicy.Decide(mounted, riderContainer, exact, combat, live, pending, member, mountOrdered);

        private static void RiderCommandCarriesHorseOnce()
        {
            TestRunner.Equal(MountedEntranceCarryDecision.Carry, Carry(), "A mounted rider's entrance command left its horse without a member command.");
            // The same group run again (a repeated click re-creates a new group natively; a duplicate Run of the same group must not).
            TestRunner.Equal(MountedEntranceCarryDecision.MountAlreadyOrdered, Carry(mountOrdered: true), "The horse received a second member command for one group.");
        }

        private static void EntranceNotApplicableCases()
        {
            TestRunner.Equal(MountedEntranceCarryDecision.NotApplicable, Carry(mounted: false), "An unmounted party member's horse was ordered.");
            TestRunner.Equal(MountedEntranceCarryDecision.NotApplicable, Carry(riderContainer: false), "The horse's own command spawned another carrier.");
            TestRunner.Equal(MountedEntranceCarryDecision.NotApplicable, Carry(exact: false), "A ground move, door or loot command was treated as an entrance.");
            TestRunner.Equal(MountedEntranceCarryDecision.NotApplicable, Carry(combat: true), "An entrance carrier was issued in combat (native refuses entrances in combat).");
        }

        private static void EntranceRefusalsNeverOrderTheHorse()
        {
            TestRunner.Equal(MountedEntranceCarryDecision.RiderCommandNotLive, Carry(live: false), "A refused or replaced rider command still ordered the horse.");
            TestRunner.Equal(MountedEntranceCarryDecision.GroupNotPending, Carry(pending: false), "A started, finished or interrupted group was joined late.");
            TestRunner.Equal(MountedEntranceCarryDecision.MountNotGroupMember, Carry(member: false), "A horse outside the native group was added as an extra arrival.");
        }
    }
}
