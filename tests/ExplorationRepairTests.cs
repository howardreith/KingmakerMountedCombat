using System;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // Exploration repair sprint (2026-11-12): retained mounted intent across scripted events, camp and world-map
    // travel, and the mounted item-familiar follow rule. Domain decisions only; native acceptance is separate.
    internal static class ExplorationRepairTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("exploration E1/C1 only native scripted-event modes suspend with retained intent; other boundaries keep the clean dismount", EventModesSuspend);
            runner.Run("exploration issue 7 the world map is recognised only by its native global-map UI scene", WorldMapRecognition);
            runner.Run("exploration E3 restoration waits for settled ordinary play and cancels for a changed world or combat", RestorationGate);
            runner.Run("exploration E3 a retained pair restores once eligible, retries while transient and drops when invalidated or separated", PairDecision);
            runner.Run("exploration issue 7 a carried arrival restores, retains a transient refusal for retry and drops only an invalidated pair", CarriedArrival);
            runner.Run("review 1 a restore failure is a clean refusal only when the real coordinator rolled every attachment back", RestoreFailureClassification);
            runner.Run("review 2 suspended intent round-trips through its own bounded save record for two pairs, never an unmounted or mounted control", RetainedRecordRoundTrip);
            runner.Run("review 2 a saved record is adopted only by its exact world and malformed records are refused", RetainedRecordAdmission);
            runner.Run("review 3 a camp that follows a scripted event adopts the camp separation rules", EventThenCampPromotion);
            runner.Run("review 4 the captured owner identity is imported once its previous owner retired, and refuses cleanly before", OwnerIdentityImport);
            runner.Run("exploration P1 a mounted rider's familiar follows the mount's path end or ground position under the native leash", FamiliarFollow);
            runner.Run("exploration event suspension feedback says the pair remounts automatically", SuspensionFeedback);
        }

        private static void EventModesSuspend()
        {
            foreach (var mode in new[] { "Dialog", "Cutscene", "Rest" })
            {
                TestRunner.True(MountedRetainedIntentPolicy.SuspendsForEvent(mode), mode + " did not retain the pair's mounted intent.");
                // The relationship itself still may not stay attached through the event: the attachment is suspended.
                TestRunner.Equal(MountedGameModeDisposition.CleanDismount, MountedGameModePolicy.Classify(mode),
                    mode + " must not keep the physical attachment through a native event that can move or hide units.");
            }
            foreach (var mode in new[] { "Default", "Pause", "FullScreenUi", "EscMode", "None", "GlobalMap", "Kingdom", "KingdomSettlement",
                "CutsceneGlobalMap", "GameOver", "BugReport", "dialog", "" })
                TestRunner.True(!MountedRetainedIntentPolicy.SuspendsForEvent(mode), mode + " was treated as a retained scripted event.");
        }

        private static void WorldMapRecognition()
        {
            TestRunner.True(MountedRetainedIntentPolicy.IsWorldMapArea(true, "UI_GlobalMap", "UI_GlobalMap"), "The global map area was not recognised.");
            TestRunner.True(!MountedRetainedIntentPolicy.IsWorldMapArea(false, "UI_GlobalMap", "UI_GlobalMap"), "An area without a custom UI is a local area.");
            TestRunner.True(!MountedRetainedIntentPolicy.IsWorldMapArea(true, "UI_Kingdom", "UI_GlobalMap"), "Another custom UI scene is not the world map.");
            TestRunner.True(!MountedRetainedIntentPolicy.IsWorldMapArea(true, null, "UI_GlobalMap"), "A missing scene name is not the world map.");
        }

        private static void RestorationGate()
        {
            MountedRetainedIntentDecision Decide(bool sameWorld = true, bool combat = false, bool worldMap = false, string mode = "Default",
                bool eventMode = false, bool loading = false, bool boundary = false, bool camp = false) =>
                MountedRetainedIntentPolicy.Decide(sameWorld, combat, worldMap, mode, eventMode, loading, boundary, camp);
            TestRunner.Equal(MountedRetainedIntentDecision.Restore, Decide(), "Settled ordinary play did not restore.");
            TestRunner.Equal(MountedRetainedIntentDecision.Cancel, Decide(sameWorld: false), "A different world or save kept stale intent.");
            TestRunner.Equal(MountedRetainedIntentDecision.Cancel, Decide(combat: true), "Restoration adopted a native encounter (ambush or hostile dialog).");
            TestRunner.Equal(MountedRetainedIntentDecision.Hold, Decide(worldMap: true), "The world map restored a physical pair.");
            foreach (var mode in new[] { "Dialog", "Cutscene", "Rest", "Pause", "FullScreenUi", "EscMode", "None", "GlobalMap" })
                TestRunner.Equal(MountedRetainedIntentDecision.Hold, Decide(mode: mode), mode + " restored before ordinary play resumed.");
            TestRunner.Equal(MountedRetainedIntentDecision.Hold, Decide(eventMode: true), "A dialog nested under a cutscene (still on the mode stack) was treated as finished.");
            TestRunner.Equal(MountedRetainedIntentDecision.Hold, Decide(loading: true), "Restoration ran during native loading.");
            TestRunner.Equal(MountedRetainedIntentDecision.Hold, Decide(boundary: true), "Restoration ran over an owned save or area boundary.");
            TestRunner.Equal(MountedRetainedIntentDecision.Hold, Decide(camp: true), "Restoration ran while the camp assembly was still gathering the party.");
            TestRunner.Equal(MountedRetainedIntentDecision.Cancel, Decide(sameWorld: false, worldMap: true, mode: "Dialog"), "A changed world must cancel before anything holds.");
        }

        private static void PairDecision()
        {
            MountedRetainedPairDecision Pair(bool resolved = true, bool profile = true, bool already = false, bool permanent = false,
                string refusal = null, float separation = 1f, MountedRetainedIntentKind kind = MountedRetainedIntentKind.ScriptedEvent,
                bool sameArea = true, bool expired = false) =>
                MountedRetainedIntentPolicy.DecidePair(resolved, profile, already, permanent, refusal, separation,
                    MountedRetainedIntentPolicy.MaximumSeparationMetres(kind), sameArea, expired);
            TestRunner.Equal(MountedRetainedPairDecision.Restore, Pair(), "An eligible retained pair did not restore.");
            TestRunner.Equal(MountedRetainedPairDecision.Drop, Pair(resolved: false), "A removed companion was restored.");
            TestRunner.Equal(MountedRetainedPairDecision.Drop, Pair(profile: false), "A changed pairing or profile was restored.");
            TestRunner.Equal(MountedRetainedPairDecision.Drop, Pair(already: true), "A pair the player already remounted (or re-paired) was attached twice.");
            TestRunner.Equal(MountedRetainedPairDecision.Drop, Pair(permanent: true, refusal: "Both units must be alive and conscious."), "A dead or unconscious member was restored.");
            TestRunner.Equal(MountedRetainedPairDecision.Retry, Pair(refusal: "Both stock movement agents must be enabled before mounting."), "A transiently unattachable pair was dropped too early.");
            TestRunner.Equal(MountedRetainedPairDecision.Drop, Pair(refusal: "Both stock movement agents must be enabled before mounting.", expired: true), "A pair never became attachable but intent was kept forever.");
            TestRunner.Equal(MountedRetainedPairDecision.Drop, Pair(separation: 6.5f), "A scripted relocation that separated the pair snapped the rider across the scene.");
            TestRunner.Equal(MountedRetainedPairDecision.Restore, Pair(separation: 6.5f, kind: MountedRetainedIntentKind.Camp), "Separate native campsite slots refused the camp restoration.");
            TestRunner.Equal(MountedRetainedPairDecision.Drop, Pair(separation: 30f, kind: MountedRetainedIntentKind.Camp), "A pair outside one campsite was snapped together.");
            TestRunner.Equal(MountedRetainedPairDecision.Drop, Pair(sameArea: false), "A rider was seated across a navmesh boundary.");
            TestRunner.Equal(MountedRetainedPairDecision.Drop, Pair(separation: float.NaN), "A non-finite separation was accepted.");
        }

        private static void CarriedArrival()
        {
            MountedCarriedArrivalDecision Arrive(bool combat = false, bool resolved = true, bool profile = true, bool permanent = false, string refusal = null) =>
                MountedRetainedIntentPolicy.DecideCarriedArrival(combat, resolved, profile, permanent, refusal);
            TestRunner.Equal(MountedCarriedArrivalDecision.Restore, Arrive(), "An eligible carried pair did not restore on arrival.");
            TestRunner.Equal(MountedCarriedArrivalDecision.Drop, Arrive(combat: true), "A carried pair was restored into an arrival encounter.");
            TestRunner.Equal(MountedCarriedArrivalDecision.Drop, Arrive(resolved: false), "A carried pair with a missing actor was kept.");
            TestRunner.Equal(MountedCarriedArrivalDecision.Drop, Arrive(profile: false), "A carried pair whose profile or pairing changed was kept.");
            TestRunner.Equal(MountedCarriedArrivalDecision.Drop, Arrive(permanent: true, refusal: "Both units must be alive and conscious."), "A dead carried member was kept.");
            // The original world-map refusal is never restored and never latched; on a local area it is retried in ordinary play.
            TestRunner.Equal(MountedCarriedArrivalDecision.Retain, Arrive(refusal: "Mounting is available only in Default or Pause game mode."),
                "A transient arrival refusal discarded the retained intent instead of retrying it.");
            TestRunner.Equal(MountedCarriedArrivalDecision.Retain, Arrive(refusal: "Both stock movement agents must be enabled before mounting."),
                "A not-yet-ready agent at OnAreaLoaded discarded the retained intent.");
        }

        // A minimal IMountedPairRuntime: the real coordinator drives it, with failures injected at the attachment and
        // rollback steps exactly where the native runtime can fail.
        private sealed class FailingRuntime : IMountedPairRuntime
        {
            internal bool FailAttach, FailRollbackPresentation;
            public void AcquireMovementAuthority(MountedPair pair) { }
            public void AttachPresentation(MountedPair pair) { if (FailAttach) throw new InvalidOperationException("attach failed"); }
            public void RestorePresentation(MountedPair pair) { if (FailRollbackPresentation) throw new InvalidOperationException("rollback failed"); }
            public void RestoreMovementAuthority(MountedPair pair, CleanupTrigger trigger) { }
        }

        private static MountedPairCandidate SavedCandidate(bool safeMode = true) => new MountedPairCandidate("rider", "mount")
        {
            RiderIsDirectlyControllable = true, MountIsDirectlyControllable = true, RiderIsAliveAndConscious = true, MountIsAliveAndConscious = true,
            ExactReciprocalCompanionRelationship = true, RiderSizeOrdinal = 4, MountSizeOrdinal = 5, RiderViewAndStockAgentAvailable = true,
            MountViewAndStockAgentAvailable = true, RiderStockAgentEnabled = true, MountStockAgentEnabled = true, RiderAgentOverrideAvailable = true,
            MountAgentOverrideAvailable = true, RiderIsExactlyMedium = true, SafeMovementMode = safeMode
        };

        // The same observation the integration passes: the coordinator's own result and final state.
        private static MountedRetainedRestoreOutcome Classify(MountedRelationshipCoordinator coordinator, TransitionResult result) =>
            MountedRetainedIntentPolicy.ClassifyRestore(result.Succeeded, result.MovementAuthorityResidual, result.PresentationResidual,
                coordinator.State == RelationshipState.Unmounted, true, coordinator.State != RelationshipState.Faulted);

        private static void RestoreFailureClassification()
        {
            var ok = new MountedRelationshipCoordinator(new FailingRuntime());
            TestRunner.Equal(MountedRetainedRestoreOutcome.Restored, Classify(ok, ok.RestoreSaved(SavedCandidate())), "A successful saved restore was not classified as restored.");

            // Control: the original world-map refusal (not a safe movement mode) attaches nothing and stays a clean refusal.
            var refused = new MountedRelationshipCoordinator(new FailingRuntime());
            var refusal = refused.RestoreSaved(SavedCandidate(false));
            TestRunner.True(!refusal.Succeeded && refused.State == RelationshipState.Unmounted, "The mode refusal changed the relationship.");
            TestRunner.Equal(MountedRetainedRestoreOutcome.CleanRefusal, Classify(refused, refusal),
                "A clean eligibility refusal was treated as residue, which would reintroduce the world-map lockout.");

            // Attachment failure with a successful rollback: cleanly dismounted.
            var rolledBack = new MountedRelationshipCoordinator(new FailingRuntime { FailAttach = true });
            var rollback = rolledBack.RestoreSaved(SavedCandidate());
            TestRunner.True(!rollback.Succeeded && rolledBack.State == RelationshipState.Unmounted, "The rolled-back attachment did not unmount.");
            TestRunner.Equal(MountedRetainedRestoreOutcome.CleanRefusal, Classify(rolledBack, rollback), "A fully rolled-back attachment failure was not a clean refusal.");

            // Attachment failure followed by a rollback failure: the real coordinator keeps owned presentation state.
            var faulted = new MountedRelationshipCoordinator(new FailingRuntime { FailAttach = true, FailRollbackPresentation = true });
            var fault = faulted.RestoreSaved(SavedCandidate());
            TestRunner.True(!fault.Succeeded && (fault.PresentationResidual || faulted.State != RelationshipState.Unmounted),
                "The injected rollback failure did not leave the coordinator owning state.");
            TestRunner.Equal(MountedRetainedRestoreOutcome.RetainedResidue, Classify(faulted, fault),
                "A failure that retained native state was reported as a clean dismount.");

            // Any one residue observation alone keeps the latch.
            TestRunner.Equal(MountedRetainedRestoreOutcome.RetainedResidue, MountedRetainedIntentPolicy.ClassifyRestore(false, true, false, true, true, true), "Movement authority residue was dropped.");
            TestRunner.Equal(MountedRetainedRestoreOutcome.RetainedResidue, MountedRetainedIntentPolicy.ClassifyRestore(false, false, false, true, false, true), "A prepared pair residue was dropped.");
            TestRunner.Equal(MountedRetainedRestoreOutcome.RetainedResidue, MountedRetainedIntentPolicy.ClassifyRestore(false, false, false, true, true, false), "An attachment residue was dropped.");
            TestRunner.Equal(MountedRetainedRestoreOutcome.RetainedResidue, MountedRetainedIntentPolicy.ClassifyRestore(false, false, false, false, true, true), "A relationship that is not unmounted was dropped.");
        }

        private const string Campaign = "6f9619ff-8b86-d011-b42d-00cf4fc964ff";
        private const string WorldMapArea = "0123456789abcdef0123456789abcdef";
        private const string RiderA = "11111111-1111-1111-1111-111111111111", MountA = "22222222-2222-2222-2222-222222222222";
        private const string RiderB = "33333333-3333-3333-3333-333333333333", MountB = "44444444-4444-4444-4444-444444444444";
        private const string RiderC = "55555555-5555-5555-5555-555555555555", MountC = "66666666-6666-6666-6666-666666666666";
        private const string OwnerA = "aaaaaaaabbbbccccddddeeeeeeeeeeee", OwnerB = "bbbbbbbbccccddddeeeeffffffffffff";

        private static MountedRetainedIntentPairRecord PairRecord(string rider, string mount, string owner, long generation) =>
            new MountedRetainedIntentPairRecord { RiderId = rider, MountId = mount, ProfileId = MountedRiderProfileCatalog.MediumHorseId,
                OwnerId = owner, Generation = generation };

        private static void RetainedRecordRoundTrip()
        {
            // Two suspended pairs; pair C is physically mounted at the barrier (kmc-mounted-state owns it). An initially
            // unmounted pair is never in the retained set, so it cannot be recorded at all.
            var recordable = MountedRetainedIntentRecord.Recordable(new[] {
                PairRecord(RiderA, MountA, OwnerA, 3), PairRecord(RiderB, MountB, OwnerB, 7), PairRecord(RiderC, MountC, null, 0) },
                new System.Collections.Generic.HashSet<string> { RiderC, MountC });
            TestRunner.Equal(2, recordable.Length, "The record did not hold exactly the two suspended pairs.");
            var record = new MountedRetainedIntentRecord { CampaignId = Campaign, AreaId = WorldMapArea, GameTimeTicks = 123456789L,
                Kind = MountedRetainedIntentKind.WorldMapTravel, Pairs = recordable };
            var decoded = MountedRetainedIntentRecord.Decode(record.Encode());
            TestRunner.Equal(Campaign, decoded.CampaignId, "Campaign did not round-trip.");
            TestRunner.Equal(WorldMapArea, decoded.AreaId, "Area did not round-trip.");
            TestRunner.Equal(123456789L, decoded.GameTimeTicks, "Game time did not round-trip.");
            TestRunner.Equal(MountedRetainedIntentKind.WorldMapTravel, decoded.Kind, "Kind did not round-trip.");
            TestRunner.Equal(2, decoded.Pairs.Length, "Pairs did not round-trip.");
            TestRunner.Equal(OwnerA, decoded.Pairs[0].OwnerId, "Owner identity A did not survive the save.");
            TestRunner.Equal(3L, decoded.Pairs[0].Generation, "Generation A did not survive the save.");
            TestRunner.Equal(OwnerB, decoded.Pairs[1].OwnerId, "Owner identity B did not survive the save.");
            TestRunner.Equal(7L, decoded.Pairs[1].Generation, "Generation B did not survive the save.");
            TestRunner.Equal(RiderB, decoded.Pairs[1].RiderId, "Rider B did not survive the save.");
            TestRunner.Equal(MountedRiderProfileCatalog.MediumHorseId, decoded.Pairs[1].ProfileId, "Profile did not survive the save.");
            // After a cold load on the world map the intent holds; the next local arrival restores the eligible pair.
            TestRunner.Equal(MountedRetainedIntentDecision.Hold, MountedRetainedIntentPolicy.Decide(true, false, true, "GlobalMap", false, false, false, false),
                "A loaded world-map record restored a pair on the world map.");
            TestRunner.Equal(MountedCarriedArrivalDecision.Restore, MountedRetainedIntentPolicy.DecideCarriedArrival(false, true, true, false, null),
                "The loaded intent did not restore at the next local arrival.");
            // The post-rest autosave: a Camp record reloads in its local area and restores in ordinary play under camp rules.
            var camp = MountedRetainedIntentRecord.Decode(new MountedRetainedIntentRecord { CampaignId = Campaign, AreaId = WorldMapArea,
                GameTimeTicks = 5L, Kind = MountedRetainedIntentKind.Camp, Pairs = new[] { PairRecord(RiderA, MountA, OwnerA, 3) } }.Encode());
            TestRunner.Equal(MountedRetainedIntentKind.Camp, camp.Kind, "The post-rest autosave lost its camp rules.");
            TestRunner.Equal(MountedRetainedIntentDecision.Restore, MountedRetainedIntentPolicy.Decide(true, false, false, "Default", false, false, false, false),
                "The reloaded post-rest intent did not restore in ordinary play.");
        }

        private static void RetainedRecordAdmission()
        {
            var record = new MountedRetainedIntentRecord { CampaignId = Campaign, AreaId = WorldMapArea, GameTimeTicks = 42L,
                Kind = MountedRetainedIntentKind.Camp, Pairs = new[] { PairRecord(RiderA, MountA, OwnerA, 1) } };
            TestRunner.True(record.MatchesLoadedWorld(Campaign, WorldMapArea, 42L), "The exact saved world was refused.");
            TestRunner.True(!record.MatchesLoadedWorld("7f9619ff-8b86-d011-b42d-00cf4fc964ff", WorldMapArea, 42L), "Another campaign adopted the record.");
            TestRunner.True(!record.MatchesLoadedWorld(Campaign, "fedcba9876543210fedcba9876543210", 42L), "Another area adopted the record.");
            TestRunner.True(!record.MatchesLoadedWorld(Campaign, WorldMapArea, 43L), "A stale record (a different native time) was adopted.");
            var text = record.Encode();
            foreach (var malformed in new[] {
                text.Replace("KMC-RETAINED-INTENT 1", "KMC-RETAINED-INTENT 2"),
                text.TrimEnd('\n'),
                text + "extra field\n",
                text.Replace("kind Camp", "kind Teleport"),
                text.Replace(MountA, RiderA),
                text.Replace(MountedRiderProfileCatalog.MediumHorseId, "unsupported-profile"),
                text.Replace(" " + OwnerA + " 1", " - 1"),
                text.Replace(" " + OwnerA + " 1", " " + OwnerA + " 0"),
                "",
                new string('x', MountedRetainedIntentRecord.MaximumCharacters + 1) })
            {
                var refused = false;
                try { MountedRetainedIntentRecord.Decode(malformed); } catch (System.IO.InvalidDataException) { refused = true; }
                TestRunner.True(refused, "A malformed retained-intent record was accepted: " + (malformed.Length > 40 ? malformed.Substring(0, 40) : malformed));
            }
            var tooMany = new System.Collections.Generic.List<MountedRetainedIntentPairRecord>();
            for (var i = 0; i < 7; i++) tooMany.Add(PairRecord(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), null, 0));
            TestRunner.Equal(6, MountedRetainedIntentRecord.Recordable(tooMany, new System.Collections.Generic.HashSet<string>()).Length,
                "The record exceeded the six-pair bound.");
        }

        private static void EventThenCampPromotion()
        {
            // A dialog suspends the pair as a scripted event; camping begins before it is restored.
            var kind = MountedRetainedIntentKind.ScriptedEvent;
            kind = MountedRetainedIntentPolicy.Promote(kind, MountedRetainedIntentKind.Camp);
            TestRunner.Equal(MountedRetainedIntentKind.Camp, kind, "The retained intent kept scripted-event rules through the camp.");
            MountedRetainedPairDecision After(MountedRetainedIntentKind k) => MountedRetainedIntentPolicy.DecidePair(true, true, false, false, null, 8f,
                MountedRetainedIntentPolicy.MaximumSeparationMetres(k), true, false);
            TestRunner.Equal(MountedRetainedPairDecision.Restore, After(kind), "Native camp slots 8 m apart did not restore after the promoted camp.");
            TestRunner.Equal(MountedRetainedPairDecision.Drop, After(MountedRetainedIntentKind.ScriptedEvent), "The un-promoted control should refuse 8 m.");
            TestRunner.Equal(MountedRetainedIntentKind.Camp, MountedRetainedIntentPolicy.Promote(MountedRetainedIntentKind.Camp, MountedRetainedIntentKind.ScriptedEvent),
                "A later dialog weakened the camp rules.");
            TestRunner.Equal(MountedRetainedIntentKind.WorldMapTravel, MountedRetainedIntentPolicy.Promote(MountedRetainedIntentKind.WorldMapTravel, MountedRetainedIntentKind.Camp),
                "World-map travel stopped restoring through the next arrival.");
        }

        private sealed class OwnerActor { internal readonly string Id = Guid.NewGuid().ToString(); }

        private static void OwnerIdentityImport()
        {
            var registry = new MountedPairRegistry<OwnerActor, object>(a => a.Id);
            var session = new object();
            var rider = new OwnerActor(); var mount = new OwnerActor(); var boundary = new object();
            var obligations = false;
            TestRunner.True(registry.TryReserve(session, "campaign", rider, mount, 4, boundary, () => obligations, () => true, () => true,
                out var original, out var refusal), refusal);
            registry.Commit(original);
            var id = original.Id;
            // The event suspension detaches the owner; while it still holds an obligation its identity is in use.
            registry.Detach(original);
            obligations = true;
            registry.RetireSettled();
            TestRunner.True(!registry.TryImport(session, "campaign", id, 4, rider, mount, boundary, true, () => false, () => true, () => true,
                out var early, out var earlyRefusal), "The identity was imported while its previous owner still held obligations.");
            TestRunner.True(earlyRefusal != null && early == null, "An early import was not a clean refusal.");
            // Once settled, the exact identity and generation are restored.
            obligations = false;
            registry.RetireSettled();
            TestRunner.True(registry.TryImport(session, "campaign", id, 4, rider, mount, boundary, true, () => false, () => true, () => true,
                out var restored, out var importRefusal), importRefusal);
            TestRunner.Equal(id, restored.Id, "The restored pair did not keep its owner identity.");
            TestRunner.Equal(4L, restored.Generation, "The restored pair did not keep its generation.");
            registry.Commit(restored);
        }

        private static void FamiliarFollow()
        {
            TestRunner.Equal(MountedFamiliarFollowSource.NativeMaster, MountedFamiliarFollowPolicy.Source(false, true), "An unmounted master's familiar left the native rule.");
            TestRunner.Equal(MountedFamiliarFollowSource.MountPathEnd, MountedFamiliarFollowPolicy.Source(true, true), "A moving mount's fixed path end was not followed.");
            TestRunner.Equal(MountedFamiliarFollowSource.MountGround, MountedFamiliarFollowPolicy.Source(true, false), "An idle mount's ground position was not followed.");
            TestRunner.True(!MountedFamiliarFollowPolicy.ShouldFollow(false, 9.9f), "The familiar started following inside the native leash.");
            TestRunner.True(MountedFamiliarFollowPolicy.ShouldFollow(false, 10.1f), "The familiar did not follow past the native leash.");
            TestRunner.True(MountedFamiliarFollowPolicy.ShouldFollow(true, 0f), "A following familiar stopped following before arriving.");
            TestRunner.True(MountedFamiliarFollowPolicy.MustLeaveOwnedAnchor(true, true), "A mounted rider's familiar stayed under the owned attachment anchor.");
            TestRunner.True(!MountedFamiliarFollowPolicy.MustLeaveOwnedAnchor(false, true), "An unmounted master's familiar parent was changed.");
            TestRunner.True(!MountedFamiliarFollowPolicy.MustLeaveOwnedAnchor(true, false), "A familiar with its own native parent was moved.");
        }

        private static void SuspensionFeedback()
        {
            var text = MountedCleanupFeedbackPolicy.Describe(CleanupTrigger.EventSuspension);
            TestRunner.True(text.IndexOf("remount", StringComparison.OrdinalIgnoreCase) >= 0, "Event suspension feedback did not say the pair remounts.");
            TestRunner.True((int)CleanupTrigger.EventSuspension > (int)CleanupTrigger.GameModeBoundary &&
                (int)CleanupTrigger.EventSuspension < (int)CleanupTrigger.ViewDetached, "The event suspension trigger changed cleanup priority order.");
        }
    }
}
