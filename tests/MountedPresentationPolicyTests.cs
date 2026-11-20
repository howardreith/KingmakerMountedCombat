using System;
using System.Collections.Generic;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // Known issues Sprint 7: the presentation projections of U01-U04, executed as production code. Each reads only
    // the authoritative facts it is handed and decides what native UI shows; none changes gameplay state.
    internal static class MountedPresentationPolicyTests
    {
        private const float Speed = 5f;
        private const float Step = 2.286f;

        public static void Register(TestRunner runner)
        {
            runner.Run("U01 delivered or cancelled charge travel closes the exact activation's usable distance and step", ChargeTravelClosesDisplay);
            runner.Run("U01 a refused charge, an unowned charge and the next activation keep native movement display", UnclosedDisplayStaysNative);
            runner.Run("U01 the display reads the same forced-travel fact that refuses execution", DisplayReadsExecutionFact);
            runner.Run("U02 only every identity of the recorded slot read live again keeps the selection mark", TargetingIdentityIsExact);
            runner.Run("U02 a mapping is recorded only for a completed swap whose delivery owns selection", TargetingRecordAdmission);
            runner.Run("U02 another selection operation or a dropped delivery ends the mapping", TargetingLifetime);
            runner.Run("U03 the class panel promises weapon-bond upgrades only on paths that receive them", BondProgressionRows);
            runner.Run("U03 the panel path follows the shown descriptor, preview or committed, with no state", BondPathFollowsDescriptor);
            runner.Run("U04 the badge belongs to the exact rider of one live mounted relationship", BadgeIsExact);
            runner.Run("U04 a folded mount's hover highlights only its own rider's existing entry", FoldedHoverIsExact);
            runner.Run("U04 an independent mount and native-found hovers stay native", IndependentMountStaysNative);
            runner.Run("U04 only the exact owning pair's cleanup ends the active hover redirect", CleanupOwnsOnlyItsRedirect);
        }

        private static void ChargeTravelClosesDisplay()
        {
            var mount = new MountedMovementState();
            float debit;
            mount.Advance(.6f, Speed * 2f, Step, 0f, false, false, false, false, true, false, false, out debit);
            TestRunner.True(mount.ForcedTravelRecorded, "Delivered charge travel is forced travel.");
            var closed = MountedMovementDisplayPolicy.LocomotionClosed("act-1", "act-1", true, mount.ForcedTravelRecorded);
            TestRunner.True(closed, "Forced travel in the exact activation closes the display.");
            TestRunner.Equal<float?>(0f, MountedMovementDisplayPolicy.UsableDistanceFeet(closed, 32f), "A closed Move shows no usable distance.");
            TestRunner.Equal<float?>(null, MountedMovementDisplayPolicy.UsableDistanceFeet(closed, null), "An absent distance stays absent.");
            TestRunner.True(!MountedMovementDisplayPolicy.PromisesFiveFootStep(closed, true), "No five-foot step is promised after charge travel.");
            // A charge that travelled and was then cancelled keeps its forced travel: nothing is refunded.
            var cancelled = new MountedMovementState { TimeMoved = .2f, TimeForced = .2f };
            TestRunner.True(MountedMovementDisplayPolicy.LocomotionClosed("act-1", "act-1", true, cancelled.ForcedTravelRecorded),
                "A travelled then cancelled charge closes the rest of the activation.");
            TestRunner.Equal(.6f, mount.TimeMoved, "The projection never changes TimeMoved.");
            TestRunner.Equal(.6f, mount.TimeForced, "The projection never changes TimeForced.");
        }

        private static void UnclosedDisplayStaysNative()
        {
            var refused = new MountedMovementState();
            TestRunner.True(!MountedMovementDisplayPolicy.LocomotionClosed("act-1", "act-1", true, refused.ForcedTravelRecorded),
                "A refused charge leaves the display native.");
            TestRunner.Equal<float?>(32f, MountedMovementDisplayPolicy.UsableDistanceFeet(false, 32f), "An open activation shows native distance.");
            TestRunner.True(MountedMovementDisplayPolicy.PromisesFiveFootStep(false, true), "An open activation keeps native's step.");
            TestRunner.True(!MountedMovementDisplayPolicy.PromisesFiveFootStep(false, false), "Native's own refusal of a step stays.");
            TestRunner.True(!MountedMovementDisplayPolicy.LocomotionClosed("act-2", "act-1", true, true),
                "An earlier activation's allocation never closes the next activation.");
            TestRunner.True(!MountedMovementDisplayPolicy.LocomotionClosed("act-1", "act-1", false, true),
                "An allocation that is not prepared closes nothing.");
            TestRunner.True(!MountedMovementDisplayPolicy.LocomotionClosed(null, null, true, true), "No live activation closes nothing.");
            TestRunner.True(!MountedMovementDisplayPolicy.LocomotionClosed("", "", true, true), "An empty activation closes nothing.");
        }

        private static void DisplayReadsExecutionFact()
        {
            foreach (var forced in new[] { 0f, .01f, 1f })
            {
                var state = new MountedMovementState { TimeMoved = forced, TimeForced = forced };
                var executionClosed = state.Remaining(Speed, Step, 0f, false, false, false, false) == 0f;
                TestRunner.Equal(executionClosed, MountedMovementDisplayPolicy.LocomotionClosed("a", "a", true, state.ForcedTravelRecorded),
                    "Display and execution disagree at forced time " + forced + ".");
            }
        }

        private static MountedTargetingSlotIdentity Slot(long slot = 1, long binding = 2, string owner = "rider", long generation = 3,
            int state = 2, string publicAbility = "public", string delivery = "shell", long deliveryData = 4, long operation = 5,
            Guid? pair = null) =>
            new MountedTargetingSlotIdentity(slot, binding, pair ?? PairA, generation, state, owner, publicAbility, delivery, deliveryData, operation);

        private static readonly Guid PairA = new Guid("11111111-1111-1111-1111-111111111111");
        private static readonly Guid PairB = new Guid("22222222-2222-2222-2222-222222222222");

        private static void TargetingIdentityIsExact()
        {
            var recorded = Slot();
            TestRunner.True(MountedTargetingGlowPolicy.Glows(recorded, Slot()), "The exact initiating slot glows.");
            TestRunner.True(!MountedTargetingGlowPolicy.Glows(recorded, Slot(slot: 9)), "Another slot never glows.");
            TestRunner.True(!MountedTargetingGlowPolicy.Glows(recorded, Slot(binding: 9)), "A recycled row with another ability does not glow.");
            TestRunner.True(!MountedTargetingGlowPolicy.Glows(recorded, Slot(binding: 0)), "A destroyed public ability does not glow.");
            TestRunner.True(!MountedTargetingGlowPolicy.Glows(recorded, Slot(pair: PairB)), "Another pair cannot inherit the mapping.");
            TestRunner.True(!MountedTargetingGlowPolicy.Glows(recorded, Slot(generation: 4)), "A new pair generation ends the mapping.");
            TestRunner.True(!MountedTargetingGlowPolicy.Glows(recorded, Slot(state: 0)), "A dismount ends the mapping.");
            TestRunner.True(!MountedTargetingGlowPolicy.Glows(recorded, Slot(owner: "other")), "Another action owner never glows.");
            TestRunner.True(!MountedTargetingGlowPolicy.Glows(recorded, Slot(publicAbility: "other")), "Another public ability never glows.");
            TestRunner.True(!MountedTargetingGlowPolicy.Glows(recorded, Slot(delivery: "other")), "Another delivery never glows.");
            TestRunner.True(!MountedTargetingGlowPolicy.Glows(recorded, Slot(deliveryData: 0)), "A destroyed delivery shell never glows.");
            TestRunner.True(!MountedTargetingGlowPolicy.Glows(recorded, Slot(operation: 6)), "A later selection operation never glows.");
            TestRunner.True(!MountedTargetingGlowPolicy.Glows(null, Slot()), "No mapping, no glow.");
        }

        private static void TargetingRecordAdmission()
        {
            TestRunner.True(MountedTargetingGlowPolicy.MayRecord(true, true, true, true), "A completed swap that owns selection is recorded.");
            TestRunner.True(!MountedTargetingGlowPolicy.MayRecord(false, true, true, true), "An ordinary ability click records nothing.");
            TestRunner.True(!MountedTargetingGlowPolicy.MayRecord(true, false, true, true), "A slot not restored to its public identity records nothing.");
            TestRunner.True(!MountedTargetingGlowPolicy.MayRecord(true, true, false, true), "A self cast or refused selection records nothing.");
            TestRunner.True(!MountedTargetingGlowPolicy.MayRecord(true, true, true, false), "An inactive service records nothing.");
        }

        private static void TargetingLifetime()
        {
            var recorded = Slot();
            TestRunner.True(MountedTargetingGlowPolicy.Retains(recorded, 5, 4), "The live operation keeps its mapping.");
            TestRunner.True(!MountedTargetingGlowPolicy.Retains(recorded, 6, 4), "Delivery, cancellation or replacement ends it.");
            TestRunner.True(!MountedTargetingGlowPolicy.Retains(recorded, 5, 0), "A dropped selection ends it.");
            TestRunner.True(!MountedTargetingGlowPolicy.Retains(recorded, 5, 7), "Another ability in selection ends it.");
            TestRunner.True(!MountedTargetingGlowPolicy.Retains(null, 5, 4), "No mapping retains nothing.");
        }

        private static void BondProgressionRows()
        {
            TestRunner.True(!DivineMountPolicy.ShowsProgressionRow(true, DivineBondDisplayPath.DivineMount), "Divine Mount promises no weapon upgrade.");
            TestRunner.True(DivineMountPolicy.ShowsProgressionRow(true, DivineBondDisplayPath.WeaponBond), "Weapon Bond keeps its upgrades.");
            TestRunner.True(DivineMountPolicy.ShowsProgressionRow(true, DivineBondDisplayPath.Stock), "A stock character keeps its upgrades.");
            TestRunner.True(!DivineMountPolicy.ShowsProgressionRow(true, DivineBondDisplayPath.Undecided), "An undecided bond guarantees neither path.");
            foreach (DivineBondDisplayPath path in Enum.GetValues(typeof(DivineBondDisplayPath)))
                TestRunner.True(DivineMountPolicy.ShowsProgressionRow(false, path), "Every other row stays for " + path + ".");
        }

        private static void BondPathFollowsDescriptor()
        {
            TestRunner.Equal(DivineBondDisplayPath.DivineMount, DivineMountPolicy.ProgressionDisplayPath(true, false, true, false), "Divine Mount.");
            TestRunner.Equal(DivineBondDisplayPath.WeaponBond, DivineMountPolicy.ProgressionDisplayPath(true, false, false, true), "Weapon Bond.");
            TestRunner.Equal(DivineBondDisplayPath.Undecided, DivineMountPolicy.ProgressionDisplayPath(true, false, false, false), "No choice yet.");
            TestRunner.Equal(DivineBondDisplayPath.Stock, DivineMountPolicy.ProgressionDisplayPath(false, false, false, false), "Choice not applied.");
            TestRunner.Equal(DivineBondDisplayPath.Stock, DivineMountPolicy.ProgressionDisplayPath(true, true, false, false), "Outside the party.");
            TestRunner.Equal(DivineBondDisplayPath.DivineMount, DivineMountPolicy.ProgressionDisplayPath(false, false, true, false),
                "A mount owner's grants drop the upgrades even with the choice not applied, and so does its panel.");
            // Respec previews either way and a cancelled preview are the same function of whichever descriptor is shown.
            TestRunner.Equal(DivineBondDisplayPath.DivineMount, DivineMountPolicy.ProgressionDisplayPath(true, false, true, false), "Weapon Bond to Divine Mount preview.");
            TestRunner.Equal(DivineBondDisplayPath.WeaponBond, DivineMountPolicy.ProgressionDisplayPath(true, false, false, true), "Divine Mount to Weapon Bond preview.");
            TestRunner.True(DivineMountPolicy.KeepsLevelEntryFeature(true, false) == DivineMountPolicy.ShowsProgressionRow(true, DivineBondDisplayPath.WeaponBond) &&
                DivineMountPolicy.KeepsLevelEntryFeature(true, true) == DivineMountPolicy.ShowsProgressionRow(true, DivineBondDisplayPath.DivineMount),
                "The panel promises exactly what the grant filter grants on a decided path.");
        }

        private static List<MountedPairPresentationFact> TwoPairs(bool folded = true) => new List<MountedPairPresentationFact>
        {
            new MountedPairPresentationFact(PairA, 1, "riderA", "mountA", true, folded),
            new MountedPairPresentationFact(PairB, 7, "riderB", "mountB", true, folded),
        };

        private static void BadgeIsExact()
        {
            var badge = MountedPairPresentationPolicy.Badge("riderA", TwoPairs());
            TestRunner.True(badge != null && badge.Owner == PairA && badge.Generation == 1 && badge.MountId == "mountA", "Rider A's badge names mount A.");
            var other = MountedPairPresentationPolicy.Badge("riderB", TwoPairs());
            TestRunner.True(other != null && other.Owner == PairB && other.MountId == "mountB", "Badges of two pairs never cross.");
            TestRunner.True(MountedPairPresentationPolicy.Badge("mountA", TwoPairs()) == null, "A mount's own portrait has no rider badge.");
            TestRunner.True(MountedPairPresentationPolicy.Badge("stranger", TwoPairs()) == null, "An unmounted actor has no badge.");
            var dismounted = new List<MountedPairPresentationFact> { new MountedPairPresentationFact(PairA, 1, "riderA", "mountA", false, false) };
            TestRunner.True(MountedPairPresentationPolicy.Badge("riderA", dismounted) == null, "Dismount, death or disposal clears the badge.");
            var remounted = new List<MountedPairPresentationFact> { new MountedPairPresentationFact(PairA, 2, "riderA", "mountC", true, false) };
            var renewed = MountedPairPresentationPolicy.Badge("riderA", remounted);
            TestRunner.True(renewed != null && renewed.Generation == 2 && renewed.MountId == "mountC" && !renewed.Equals(badge),
                "A remount or a different mount maps to its own new identity.");
            var sameMountOtherRider = new List<MountedPairPresentationFact> { new MountedPairPresentationFact(PairB, 8, "riderB", "mountA", true, false) };
            TestRunner.True(MountedPairPresentationPolicy.Badge("riderA", sameMountOtherRider) == null &&
                MountedPairPresentationPolicy.Badge("riderB", sameMountOtherRider)?.MountId == "mountA", "The same mount with another rider moves the badge.");
            var ambiguous = new List<MountedPairPresentationFact>(TwoPairs()) { new MountedPairPresentationFact(PairB, 9, "riderA", "mountB", true, false) };
            TestRunner.True(MountedPairPresentationPolicy.Badge("riderA", ambiguous) == null, "Two relationships for one rider show nothing.");
            TestRunner.True(MountedPairPresentationPolicy.Badge("riderA", new List<MountedPairPresentationFact>()) == null, "A load or area boundary with no pairs shows nothing.");
        }

        private static void FoldedHoverIsExact()
        {
            var entries = new List<string> { "enemy", "riderB", "riderA", "ally" };
            TestRunner.Equal(2, MountedPairPresentationPolicy.FoldedMountHoverEntry("mountA", false, TwoPairs(), entries), "Mount A highlights rider A's entry.");
            TestRunner.Equal(1, MountedPairPresentationPolicy.FoldedMountHoverEntry("mountB", false, TwoPairs(), entries), "Mount B highlights rider B's entry.");
            TestRunner.Equal(-1, MountedPairPresentationPolicy.FoldedMountHoverEntry("riderA", false, TwoPairs(), entries), "A rider is not a folded mount.");
            TestRunner.Equal(-1, MountedPairPresentationPolicy.FoldedMountHoverEntry("mountA", false, TwoPairs(), new List<string> { "enemy" }),
                "No rider entry, no highlight, and no entry is created.");
            TestRunner.Equal(-1, MountedPairPresentationPolicy.FoldedMountHoverEntry("mountA", false, TwoPairs(), new List<string> { "riderA", "riderA" }),
                "An ambiguous rebuilt tracker highlights nothing.");
            var stale = new List<MountedPairPresentationFact> { new MountedPairPresentationFact(PairA, 1, "riderA", "mountA", false, true) };
            TestRunner.Equal(-1, MountedPairPresentationPolicy.FoldedMountHoverEntry("mountA", false, stale, entries), "A dismounted or stale pair redirects nothing.");
            TestRunner.Equal("enemy,riderB,riderA,ally", string.Join(",", entries), "The tracker's entry order is never changed.");
        }

        private static void IndependentMountStaysNative()
        {
            var entries = new List<string> { "riderA", "mountA" };
            TestRunner.Equal(-1, MountedPairPresentationPolicy.FoldedMountHoverEntry("mountA", false, TwoPairs(false), entries),
                "A mount acting on its own keeps its own hover.");
            TestRunner.Equal(-1, MountedPairPresentationPolicy.FoldedMountHoverEntry("mountA", false, TwoPairs(), entries),
                "A mount with its own entry is never redirected.");
            TestRunner.Equal(-1, MountedPairPresentationPolicy.FoldedMountHoverEntry("mountA", true, TwoPairs(), new List<string> { "riderA" }),
                "A hover native resolved stays native.");
        }

        private static void CleanupOwnsOnlyItsRedirect()
        {
            var owner = Guid.NewGuid();
            var redirect = new MountedPairPresentationIdentity(owner, 6, "riderB", "mountB");
            TestRunner.True(MountedPairPresentationPolicy.CleanupOwnsRedirect(new MountedPairPresentationIdentity(owner, 6, "riderB", "mountB"), redirect),
                "The exact owning pair's cleanup ends its redirect.");
            TestRunner.True(!MountedPairPresentationPolicy.CleanupOwnsRedirect(new MountedPairPresentationIdentity(Guid.NewGuid(), 1, "riderA", "mountA"), redirect),
                "An unrelated pair's cleanup ends nothing.");
            TestRunner.True(!MountedPairPresentationPolicy.CleanupOwnsRedirect(new MountedPairPresentationIdentity(owner, 7, "riderB", "mountB"), redirect),
                "The same actors under another generation end nothing.");
            TestRunner.True(!MountedPairPresentationPolicy.CleanupOwnsRedirect(new MountedPairPresentationIdentity(Guid.NewGuid(), 6, "riderB", "mountB"), redirect),
                "The same actors under another owner end nothing.");
            TestRunner.True(!MountedPairPresentationPolicy.CleanupOwnsRedirect(new MountedPairPresentationIdentity(owner, 6, "riderB", "mountA"), redirect),
                "Another mount ends nothing.");
            TestRunner.True(!MountedPairPresentationPolicy.CleanupOwnsRedirect(new MountedPairPresentationIdentity(owner, 6, "riderA", "mountB"), redirect),
                "Another rider ends nothing.");
            TestRunner.True(!MountedPairPresentationPolicy.CleanupOwnsRedirect(null, redirect) &&
                !MountedPairPresentationPolicy.CleanupOwnsRedirect(new MountedPairPresentationIdentity(Guid.Empty, 6, "riderB", "mountB"),
                    new MountedPairPresentationIdentity(Guid.Empty, 6, "riderB", "mountB")) &&
                !MountedPairPresentationPolicy.CleanupOwnsRedirect(new MountedPairPresentationIdentity(owner, 6, "riderB", "mountB"), null),
                "An unestablished identity ends nothing.");
        }
    }
}
