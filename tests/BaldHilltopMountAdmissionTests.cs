using System;
using System.Linq;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // Bald Hilltop Mount lockout repair (owner report 2026-11-12). The production scene-control decision feeding the real
    // MountedPlayerActionEvaluator, as MountedPlayerActionController.CaptureContext composes it. Native facts per actor:
    // the CutceneControlledUnit holder (lazily created by MarkUnit, never cleared), its active entry, and that entry's
    // cutscene player (alive per Unity, finished per CutscenePlayerData.IsFinished). Native acceptance is NOT RUN.
    internal static class BaldHilltopMountAdmissionTests
    {
        private const string SceneRefusal = "Mounting is blocked during loading, area transitions, and cutscenes.";

        public static void Register(TestRunner runner)
        {
            runner.Run("bald hilltop: a holder left by an earlier scene no longer refuses Mount in ordinary play", StaleHolderAdmitsMount);
            runner.Run("bald hilltop: live scene control of the exact rider or horse, loading, the lock and cutscene modes still refuse", GenuineBlockersRefuse);
            runner.Run("bald hilltop: availability recovers when the scene completes, is skipped or its player is destroyed", RecoveryWhenSceneEnds);
            runner.Run("bald hilltop: paused queueing, active execution and combat eligibility are unchanged", QueueExecutionAndCombat);
        }

        private struct Actor
        {
            internal bool Holder, ActiveEntry, PlayerAlive, Finished;
            internal static Actor Never => new Actor();
            // An earlier cutscene command controlled the unit and released it: the holder stays, the active entry is gone.
            internal static Actor ReleasedEarlier => new Actor { Holder = true };
            internal static Actor Live => new Actor { Holder = true, ActiveEntry = true, PlayerAlive = true };
            internal bool UnderLiveControl => MountedSceneContinuityPolicy.ActorUnderLiveSceneControl(Holder, ActiveEntry, PlayerAlive, Finished);
        }

        private static bool Blocks(Actor rider, Actor mount, string mode = "Default", bool loading = false, bool cutsceneLock = false) =>
            MountedSceneContinuityPolicy.BlocksPlayerTransition(loading, cutsceneLock, mode, rider.UnderLiveControl, mount.UnderLiveControl);

        private static MountedPlayerActionContext Context(bool sceneBlocked, string mode = "Default", bool executing = false, bool combat = false)
        {
            return new MountedPlayerActionContext
            {
                RelationshipState = RelationshipState.Unmounted, GameAvailable = true, FeatureEnabled = true, ExactlyOneRiderSelected = true,
                RiderIsExactlyMedium = true, RiderBodyProfileSupported = true, ExactActiveOwnedSupportedMount = true, MountDisplayName = "Horse",
                MountIsStrictlyLarger = true, RiderIsAliveAndConscious = true, MountIsAliveAndConscious = true,
                RiderIsDirectlyControllableAndInGame = true, MountIsDirectlyControllableAndInGame = true,
                LoadingTransitionOrCutscene = sceneBlocked, InCombat = combat, CombatTurnEligible = true, RiderHasMoveAction = true,
                CombatMountAuthorityQualified = true, PairedAdoptionAvailable = true, PairAdjacent = true,
                // The controller's exact composition: queue admission while choosing, active-world admission when executing.
                SafeGameMode = executing ? MountedGameModePolicy.CanAdmitMountedAction(mode) : MountedGameModePolicy.CanQueueMountedAction(mode),
                ViewsAndStockAgentsAvailable = true, StockAgentsReady = true, AgentOverridesAvailable = true
            };
        }

        private static MountedPlayerActionAvailability Evaluate(MountedPlayerActionContext context) => MountedPlayerActionEvaluator.Evaluate(context);

        private static void StaleHolderAdmitsMount()
        {
            // The reported state: ordinary exploration, the only refusal the scene message. Both actors were controlled by an
            // earlier scene (an arrival or story scene) and released; the 0.2.7 predicate read the holder's presence.
            foreach (var rider in new[] { Actor.Never, Actor.ReleasedEarlier })
            foreach (var mount in new[] { Actor.Never, Actor.ReleasedEarlier })
            {
                TestRunner.True(!Blocks(rider, mount), "A holder left by an earlier scene still refused Mount (rider holder=" + rider.Holder + ", horse holder=" + mount.Holder + ").");
                var availability = Evaluate(Context(Blocks(rider, mount)));
                TestRunner.True(availability.IsEnabled && availability.Action == MountedPlayerActionKind.Mount && availability.TransitionReady,
                    "An eligible adjacent pair was not offered Mount: " + availability.Feedback);
            }
            // The old refusal itself, for comparison: the same evaluator with the scene flag set shows exactly the reported text.
            var old = Evaluate(Context(true));
            TestRunner.True(!old.IsEnabled && old.UnavailableReasons.Count == 1 && old.UnavailableReasons[0] == SceneRefusal,
                "The reported refusal is not the single scene reason it was in the owner's screenshot.");
        }

        private static void GenuineBlockersRefuse()
        {
            TestRunner.True(Blocks(Actor.Live, Actor.Never), "A rider a playing cutscene controls was offered Mount.");
            TestRunner.True(Blocks(Actor.Never, Actor.Live), "A horse a playing cutscene controls was offered Mount.");
            TestRunner.True(Blocks(Actor.Never, Actor.Never, loading: true), "Mount was offered during loading or world replacement.");
            TestRunner.True(Blocks(Actor.Never, Actor.Never, cutsceneLock: true), "Mount was offered under the cutscene lock.");
            foreach (var mode in new[] { "Cutscene", "CutsceneGlobalMap" })
                TestRunner.True(Blocks(Actor.Never, Actor.Never, mode), "Mount was offered in " + mode + ".");
            foreach (var mode in new[] { "Default", "Pause" })
                TestRunner.True(!Blocks(Actor.ReleasedEarlier, Actor.ReleasedEarlier, mode), mode + " with released holders still refused.");
            var refused = Evaluate(Context(Blocks(Actor.Live, Actor.Never)));
            TestRunner.True(!refused.IsEnabled && refused.UnavailableReasons.Contains(SceneRefusal), "Live control did not refuse with the scene reason.");
            // Every other refusal is untouched: an unrelated eligibility failure still refuses on its own.
            var other = Context(false);
            other.MountIsStrictlyLarger = false;
            TestRunner.True(!Evaluate(other).IsEnabled, "Removing the stale scene refusal admitted an otherwise ineligible pair.");
        }

        private static void RecoveryWhenSceneEnds()
        {
            var live = Actor.Live;
            TestRunner.True(Blocks(live, Actor.Never), "Precondition: live control refuses.");
            // Completion: the track's command ends, native ReleaseUnit drops the entry (holder stays).
            TestRunner.True(!Blocks(Actor.ReleasedEarlier, Actor.Never), "Mount stayed disabled after the scene released the rider.");
            // Skip/stop: the cutscene player finished (IsFinished) or was destroyed while a missed release left the entry.
            TestRunner.True(!Blocks(new Actor { Holder = true, ActiveEntry = true, PlayerAlive = true, Finished = true }, Actor.Never),
                "A finished (skipped) cutscene kept Mount disabled.");
            TestRunner.True(!Blocks(new Actor { Holder = true, ActiveEntry = true, PlayerAlive = false }, Actor.Never),
                "A destroyed cutscene player left by a missed release kept Mount disabled.");
            TestRunner.True(Evaluate(Context(Blocks(Actor.ReleasedEarlier, Actor.ReleasedEarlier))).IsEnabled, "Availability did not recover.");
        }

        private static void QueueExecutionAndCombat()
        {
            // Paused: the order may be chosen and queued, but the transition executes only in the active world.
            TestRunner.True(Evaluate(Context(false, "Pause")).IsEnabled, "A paused Mount order could not be queued.");
            TestRunner.True(!Evaluate(Context(false, "Pause", executing: true)).IsEnabled, "A Mount transition executed while paused.");
            TestRunner.True(Evaluate(Context(false, "Default", executing: true)).IsEnabled, "A resumed Mount transition was refused.");
            // Dialog keeps refusing fresh Mount through the world-view rule, not through actor control.
            TestRunner.True(!Evaluate(Context(Blocks(Actor.Never, Actor.Never, "Dialog"), "Dialog")).IsEnabled, "Mount was offered during a dialog.");
            // Combat: normal eligibility. The scene decision adds nothing, and the Move requirement still applies.
            TestRunner.True(Evaluate(Context(Blocks(Actor.ReleasedEarlier, Actor.Never), combat: true)).IsEnabled,
                "An eligible combat Mount was refused by a released holder.");
            var noMove = Context(false, combat: true);
            noMove.TurnBasedCombat = true;
            noMove.RiderHasMoveAction = false;
            TestRunner.True(!Evaluate(noMove).IsEnabled, "A combat Mount without a Move action was offered.");
        }
    }
}
