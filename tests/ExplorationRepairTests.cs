using System;
using System.Linq;
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
            runner.Run("second review 1 after an attempted arrival a save records only the deferred pair; before exit it records travelling intent", ArrivalThenSaveRecordsOnlyPending);
            runner.Run("second review 2 a carried capital arrival retires an excluded companion pair and keeps a main-character pair", CarriedCapitalRetirement);
            runner.Run("second review 3 the retained pairs' Mount/Dismount slot rows and placement records survive the world-map save", RetainedSlotRowsRoundTrip);
            runner.Run("third review the departure merge keeps a retained slot behind a live placement marker and invents no binding", DepartureSlotMergeKeepsRetainedBinding);
            runner.Run("exploration P1 a mounted rider's familiar follows the mount's path end or ground position under the native leash", FamiliarFollow);
            runner.Run("exploration event suspension feedback says the pair remounts automatically", SuspensionFeedback);
        }

        private static void EventModesSuspend()
        {
            // Rest still suspends at its mode boundary (camp translocation); the attachment cannot stay through it.
            TestRunner.True(MountedRetainedIntentPolicy.SuspendsForEvent("Rest"), "Rest did not retain the pair's mounted intent.");
            TestRunner.Equal(MountedGameModeDisposition.CleanDismount, MountedGameModePolicy.Classify("Rest"),
                "Rest must not keep the physical attachment through camp translocation.");
            // Cutscene continuity repair (owner order, supersedes the exploration-sprint decision): Dialog and Cutscene keep
            // the attachment; only a scripted staging operation hands a pair off (CutsceneContinuityTests).
            foreach (var mode in new[] { "Dialog", "Cutscene" })
                TestRunner.True(!MountedRetainedIntentPolicy.SuspendsForEvent(mode), mode + " still suspended every mounted pair at its mode boundary.");
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
                bool eventMode = false, bool loading = false, bool boundary = false, bool camp = false,
                MountedRetainedIntentKind kind = MountedRetainedIntentKind.Camp) =>
                MountedRetainedIntentPolicy.Decide(kind, sameWorld, combat, worldMap, mode, eventMode, loading, boundary, camp);
            TestRunner.Equal(MountedRetainedIntentDecision.Restore, Decide(), "Settled ordinary play did not restore.");
            TestRunner.Equal(MountedRetainedIntentDecision.Cancel, Decide(sameWorld: false), "A different world or save kept stale intent.");
            // Camp and world-map custody still never adopt an encounter; scene staging holds through it instead.
            TestRunner.Equal(MountedRetainedIntentDecision.Cancel, Decide(combat: true), "Restoration adopted a native encounter (camp ambush).");
            TestRunner.Equal(MountedRetainedIntentDecision.Cancel, Decide(combat: true, kind: MountedRetainedIntentKind.WorldMapTravel), "World-map intent adopted an encounter.");
            TestRunner.Equal(MountedRetainedIntentDecision.Hold, Decide(combat: true, kind: MountedRetainedIntentKind.ScriptedEvent),
                "A pair still staged when a scene's encounter began lost its mounted intent.");
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
            TestRunner.Equal(MountedRetainedIntentDecision.Hold, MountedRetainedIntentPolicy.Decide(MountedRetainedIntentKind.WorldMapTravel, true, false, true, "GlobalMap", false, false, false, false),
                "A loaded world-map record restored a pair on the world map.");
            TestRunner.Equal(MountedCarriedArrivalDecision.Restore, MountedRetainedIntentPolicy.DecideCarriedArrival(false, true, true, false, null),
                "The loaded intent did not restore at the next local arrival.");
            // The post-rest autosave: a Camp record reloads in its local area and restores in ordinary play under camp rules.
            var camp = MountedRetainedIntentRecord.Decode(new MountedRetainedIntentRecord { CampaignId = Campaign, AreaId = WorldMapArea,
                GameTimeTicks = 5L, Kind = MountedRetainedIntentKind.Camp, Pairs = new[] { PairRecord(RiderA, MountA, OwnerA, 3) } }.Encode());
            TestRunner.Equal(MountedRetainedIntentKind.Camp, camp.Kind, "The post-rest autosave lost its camp rules.");
            TestRunner.Equal(MountedRetainedIntentDecision.Restore, MountedRetainedIntentPolicy.Decide(MountedRetainedIntentKind.Camp, true, false, false, "Default", false, false, false, false),
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

        private static void ArrivalThenSaveRecordsOnlyPending()
        {
            // A carried arrival of four pairs: A restores, B is deferred (a transient refusal), C is dropped (its companion
            // left), D is retired by the native capital rule. The arrival handling keeps exactly its Retain rows as the
            // authoritative retained set; the transfer stays alive (arrival attempted) until loading completes.
            var a = PairRecord(RiderA, MountA, OwnerA, 3);
            var b = PairRecord(RiderB, MountB, OwnerB, 7);
            var c = PairRecord(RiderC, MountC, null, 0);
            var d = PairRecord("77777777-7777-7777-7777-777777777777", "88888888-8888-8888-8888-888888888888", null, 0);
            var decisions = new System.Collections.Generic.Dictionary<MountedRetainedIntentPairRecord, MountedCarriedArrivalDecision>
            {
                { a, MountedRetainedIntentPolicy.DecideCarriedArrival(false, true, true, false, null) },
                { b, MountedRetainedIntentPolicy.DecideCarriedArrival(false, true, true, false, "Both stock movement agents must be enabled before mounting.") },
                { c, MountedRetainedIntentPolicy.DecideCarriedArrival(false, false, false, false, null) },
                { d, MountedRetainedIntentPolicy.DecideCarriedArrival(false, true, true, false, MountedAreaControlRule.ControlRefusal,
                    MountedAreaControlRule.IsCapitalControlRetirement(true, MountedAreaControlRule.ControlRefusal, null, false, false, false, false)) }
            };
            TestRunner.Equal(MountedCarriedArrivalDecision.Restore, decisions[a], "A did not restore.");
            TestRunner.Equal(MountedCarriedArrivalDecision.Retain, decisions[b], "B was not deferred.");
            TestRunner.Equal(MountedCarriedArrivalDecision.Drop, decisions[c], "C was not dropped.");
            TestRunner.Equal(MountedCarriedArrivalDecision.Retire, decisions[d], "D was not retired.");
            var retained = decisions.Where(x => x.Value == MountedCarriedArrivalDecision.Retain).Select(x => x.Key).ToArray();
            var transfer = new[] { a, b, c, d };
            // The after-entry save: A is physically mounted (kmc-mounted-state); only B is pending intent.
            var afterEntry = MountedRetainedIntentRecord.Recordable(
                MountedRetainedIntentCapture.PendingRows(retained, transfer, true), new System.Collections.Generic.HashSet<string> { RiderA, MountA });
            TestRunner.Equal(1, afterEntry.Length, "The after-entry save recorded a restored, dropped or retired pair as pending intent.");
            TestRunner.Equal(RiderB, afterEntry[0].RiderId, "The after-entry save lost the deferred pair.");
            // An immediate subsequent departure carries only the authoritative retained set.
            var nextDeparture = MountedRetainedIntentCapture.PendingRows(new MountedRetainedIntentPairRecord[0], retained, false).ToArray();
            TestRunner.True(nextDeparture.Length == 1 && nextDeparture[0] == b, "A retired or dropped pair travelled into the next transfer.");
            // A before-exit save of a transfer whose arrival is not yet attempted keeps the legitimate travelling intent.
            var beforeExit = MountedRetainedIntentRecord.Recordable(
                MountedRetainedIntentCapture.PendingRows(new MountedRetainedIntentPairRecord[0], transfer, false), new System.Collections.Generic.HashSet<string>());
            TestRunner.Equal(4, beforeExit.Length, "A before-exit save lost travelling intent.");
            TestRunner.Equal(MountedRetainedIntentKind.WorldMapTravel, MountedRetainedIntentCapture.Kind(MountedRetainedIntentKind.ScriptedEvent, true, true,
                MountedRetainedIntentKind.Camp), "A pending world-map transfer was not recorded as world-map travel.");
            TestRunner.Equal(MountedRetainedIntentKind.Camp, MountedRetainedIntentCapture.Kind(MountedRetainedIntentKind.Camp, false, true,
                MountedRetainedIntentKind.WorldMapTravel), "After arrival the retained kind was not authoritative.");
        }

        private static void CarriedCapitalRetirement()
        {
            var control = MountedAreaControlRule.ControlRefusal;
            // An ordinary companion pair that the native capital rule makes not directly controllable.
            var companion = MountedRetainedIntentPolicy.DecideCarriedArrival(false, true, true, false, control,
                MountedAreaControlRule.IsCapitalControlRetirement(true, control, null, false, false, false, false));
            TestRunner.Equal(MountedCarriedArrivalDecision.Retire, companion, "A capital-excluded carried companion pair became pending intent.");
            // The main character and its pet stay controllable in the capital: no refusal, ordinary restoration.
            var main = MountedRetainedIntentPolicy.DecideCarriedArrival(false, true, true, false, null,
                false);
            TestRunner.Equal(MountedCarriedArrivalDecision.Restore, main, "The main-character pair did not restore in the capital.");
            // A main-character pair that lost control is not explained by the capital rule: it stays retryable, not retired.
            var lostMain = MountedRetainedIntentPolicy.DecideCarriedArrival(false, true, true, false, control,
                MountedAreaControlRule.IsCapitalControlRetirement(true, control, null, false, true, false, true));
            TestRunner.Equal(MountedCarriedArrivalDecision.Retain, lostMain, "A main-character pair was retired by the capital rule.");
            // Outside a capital the same refusal is an ordinary transient refusal.
            TestRunner.Equal(MountedCarriedArrivalDecision.Retain, MountedRetainedIntentPolicy.DecideCarriedArrival(false, true, true, false, control,
                MountedAreaControlRule.IsCapitalControlRetirement(false, control, null, false, false, false, false)), "A non-capital control refusal was retired.");
        }

        private static void RetainedSlotRowsRoundTrip()
        {
            var pairs = new[] { PairRecord(RiderA, MountA, OwnerA, 3), PairRecord(RiderB, MountB, OwnerB, 7) };
            var slots = MountedRetainedIntentRecord.RecordableSlots(new[]
            {
                // Rider A: the public Mount/Dismount control in a non-default slot plus its one-placement record.
                new MountedRetainedIntentSlotRecord { ActorId = RiderA, Index = 9, Kind = MountedControlSlotPolicy.SavedMountState },
                new MountedRetainedIntentSlotRecord { ActorId = RiderA, Index = 0, Kind = MountedControlSlotPolicy.SavedPlacementRecord },
                // Rider B deliberately removed the control: a placement record and no slot row.
                new MountedRetainedIntentSlotRecord { ActorId = RiderB, Index = 0, Kind = MountedControlSlotPolicy.SavedPlacementRecord },
                // Another unit's row and an invalid row never travel.
                new MountedRetainedIntentSlotRecord { ActorId = RiderC, Index = 3, Kind = MountedControlSlotPolicy.SavedMountState },
                new MountedRetainedIntentSlotRecord { ActorId = RiderA, Index = 200, Kind = MountedControlSlotPolicy.SavedMountState },
                new MountedRetainedIntentSlotRecord { ActorId = RiderA, Index = 9, Kind = MountedControlSlotPolicy.SavedMountState }
            }, pairs);
            TestRunner.Equal(3, slots.Length, "Only the retained pairs' valid, distinct control rows may travel.");
            var decoded = MountedRetainedIntentRecord.Decode(new MountedRetainedIntentRecord { CampaignId = Campaign, AreaId = WorldMapArea,
                GameTimeTicks = 9L, Kind = MountedRetainedIntentKind.WorldMapTravel, Pairs = pairs, Slots = slots }.Encode());
            TestRunner.Equal(3, decoded.Slots.Length, "Slot rows did not survive the world-map save.");
            TestRunner.True(decoded.Slots.Any(x => x.ActorId == RiderA && x.Index == 9 && x.Kind == MountedControlSlotPolicy.SavedMountState),
                "The exact non-default Mount/Dismount slot did not survive the save.");
            TestRunner.True(decoded.Slots.Any(x => x.ActorId == RiderB && x.Kind == MountedControlSlotPolicy.SavedPlacementRecord) &&
                !decoded.Slots.Any(x => x.ActorId == RiderB && x.Kind == MountedControlSlotPolicy.SavedMountState),
                "A deliberately removed control would be replaced after loading.");
            // A slot row for an actor outside the recorded pairs, or an invalid row, makes the whole record invalid.
            var good = new MountedRetainedIntentRecord { CampaignId = Campaign, AreaId = WorldMapArea, GameTimeTicks = 9L,
                Kind = MountedRetainedIntentKind.WorldMapTravel, Pairs = pairs, Slots = slots }.Encode();
            foreach (var malformed in new[] { good + "slot " + RiderC + " 3 6\n", good + "slot " + RiderA + " 200 6\n",
                good + "slot " + RiderA + " 9 6\n", good + "pair " + RiderC + " " + MountC + " " + MountedRiderProfileCatalog.MediumHorseId + " - 0\n" })
            {
                var refused = false;
                try { MountedRetainedIntentRecord.Decode(malformed); } catch (System.IO.InvalidDataException) { refused = true; }
                TestRunner.True(refused, "A malformed slot section was accepted.");
            }
        }

        private static void DepartureSlotMergeKeepsRetainedBinding()
        {
            var placement = MountedControlSlotPolicy.SavedPlacementRecord;
            var control = MountedControlSlotPolicy.SavedMountState;
            // Rider A placed Mount/Dismount in slot 9. Area suspension cleared it; on the world map the live capture holds
            // only A's placement marker. The retained snapshot (in memory, or decoded from a world-map save) keeps slot 9
            // and the marker. Rider B deliberately removed the control: its retained rows are a marker only.
            var pairs = new[] { PairRecord(RiderA, MountA, OwnerA, 3), PairRecord(RiderB, MountB, OwnerB, 7) };
            var savedRetained = MountedRetainedIntentRecord.Decode(new MountedRetainedIntentRecord { CampaignId = Campaign, AreaId = WorldMapArea,
                GameTimeTicks = 11L, Kind = MountedRetainedIntentKind.WorldMapTravel, Pairs = pairs,
                Slots = MountedRetainedIntentRecord.RecordableSlots(new[] {
                    new MountedRetainedIntentSlotRecord { ActorId = RiderA, Index = 9, Kind = control },
                    new MountedRetainedIntentSlotRecord { ActorId = RiderA, Index = 0, Kind = placement },
                    new MountedRetainedIntentSlotRecord { ActorId = RiderB, Index = 0, Kind = placement } }, pairs) }.Encode()).Slots;
            var live = new[] { new MountedRetainedIntentSlotRecord { ActorId = RiderA, Index = 0, Kind = placement } };
            var merged = MountedRetainedIntentRecord.MergeSlotRows(live, savedRetained);
            TestRunner.True(merged.Any(x => x.ActorId == RiderA && x.Index == 9 && x.Kind == control),
                "A live placement-only marker discarded rider A's retained slot-9 binding at departure.");
            TestRunner.Equal(1, merged.Count(x => x.ActorId == RiderA && x.Kind == placement), "Rider A's placement marker was duplicated.");
            TestRunner.True(merged.Any(x => x.ActorId == RiderB && x.Kind == placement) && !merged.Any(x => x.ActorId == RiderB && x.Kind != placement),
                "A binding was invented for rider B's deliberately removed control.");
            TestRunner.Equal(3, merged.Length, "The merge produced unexpected rows.");
            // A binding the player holds now keeps its slot and comes first, so it wins the public control's first index.
            var current = MountedRetainedIntentRecord.MergeSlotRows(
                new[] { new MountedRetainedIntentSlotRecord { ActorId = RiderA, Index = 3, Kind = control } }, savedRetained);
            TestRunner.True(current.Length > 0 && current[0].ActorId == RiderA && current[0].Index == 3 && current[0].Kind == control,
                "A legitimate current binding did not keep precedence.");
            // The same slot index holds one binding: the live row wins over a retained row of another kind.
            var sameIndex = MountedRetainedIntentRecord.MergeSlotRows(
                new[] { new MountedRetainedIntentSlotRecord { ActorId = RiderA, Index = 9, Kind = MountedControlSlotPolicy.SavedMountCompanion } }, savedRetained);
            TestRunner.Equal(1, sameIndex.Count(x => x.ActorId == RiderA && x.Index == 9), "One slot index carried two bindings.");
            TestRunner.Equal(MountedControlSlotPolicy.SavedMountCompanion, sameIndex.Single(x => x.ActorId == RiderA && x.Index == 9).Kind,
                "A retained row replaced the player's current binding in the same slot.");
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
