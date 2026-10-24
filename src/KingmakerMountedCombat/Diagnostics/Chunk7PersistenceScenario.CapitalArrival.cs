using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.GameModes;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    // Matrix row 58 (owner decision after b195 x4c). A cold-loaded two-owner mounted Manual makes one
    // ordinary native transfer, autosave None, into the reviewed capital pin. Native IsDirectlyControllable
    // admits only the main character and its pet there, so KMC must restore the main pair without Mount
    // or preparation, retire the companion pair after its observed separation, keep saving allowed and
    // name the dismounted pair in the UMM summary. One genuine native Manual is then written in the
    // capital and read back with exactly one mounted pair. This is the only route that may enter a
    // capital; every other case keeps refusing one. Nothing is granted, refunded or synthesized here.
    internal sealed partial class Chunk7PersistenceScenario
    {
        private bool CapitalArrivalCase => Cold && request.OwnedPairs?.IsCapitalArrivalProducer(request.PersistenceCase) == true;
        // Row 58 loads outside the capital and writes inside it; every other cold case loads its archive area.
        private string ColdInputArea => CapitalArrivalCase ? request.PersistenceLoad.Area : ExpectedArchiveArea;
        private BlueprintArea capitalDestination;
        private bool capitalDispatched, capitalAdopted, capitalSuspensionObserved;
        private int capitalLoadingTicks;
        private long capitalCastBaseline;
        private SavedMountedPair capitalMainPair, capitalRetiredPair;
        private readonly Dictionary<string, int> capitalViewIds = new Dictionary<string, int>(StringComparer.Ordinal);
        private int capitalMainAgentBaseline;
        private JObject capitalRow;
        internal const string CapitalFeedbackPrefix = "Dismounted on arrival because the companion is not directly controllable in this area: ";

        private void BeginCapitalArrival()
        {
            Require(CapitalArrivalCase && stage == 0 && !capitalDispatched && !capitalAdopted && inherited != null && inherited.Bound &&
                saved != null && saved.Pairs.Length == 2 && actors?.Length == 4 && expectedOwners.Count == 2 && restoration != null,
                "Capital arrival starts once from its exact restored two-pair cold world and its live restoration observer.");
            RequireNativeWorld(); inherited.RequirePreserved(); AssertOwners(); AssertSlots();
            var game = Game.Instance; var target = request.PersistenceAreaTarget; var main = game.Player.MainCharacter.Value;
            // Exact roles make the native outcome exact: pair A is the main character and its pet, which the
            // capital rule admits; pair B is a companion and its own pet, which the rule excludes.
            Require(ReferenceEquals(firstRider, main) && ReferenceEquals(firstMount.Descriptor.Master.Value, main) &&
                !ReferenceEquals(secondRider, main) && !ReferenceEquals(secondMount, main) &&
                ReferenceEquals(secondMount.Descriptor.Master.Value, secondRider) && secondRider.Descriptor.Master.Value == null,
                "Capital arrival requires pair A to be the main character with its pet and pair B a companion with its own pet.");
            Require(!game.Player.IsInCombat && game.CurrentMode == GameModeType.Default && !LoadingProcess.Instance.IsLoadingInProcess &&
                !LoadingProcess.Instance.QueuedNames.Any() && !persistence.HasActiveSaveScope && !persistence.HasSaveAdmissionFence &&
                !persistence.AreaTransitionPending && !NativePersistenceIsolation.HasPendingWrites && !world.SerializationBusy &&
                !controls.HasUnsettledRelationshipTransition && !world.UnifiedTurn.HasUnsettledPreparation &&
                persistence.AreaSuspensionCount == 0 && persistence.AreaResumeCount == 0 && persistence.AreaRetiredPairCount == 0 &&
                persistence.FailedAreaPairCount == 0 && persistence.RefusedAreaTransferCount == 0 && persistence.NativeWorldDisposalCount == 0 &&
                persistence.SemanticRestoreCount == 4 && persistence.PresentationRestoreCount == 2 && persistence.ArchivePostLoadObservedCount == 4 &&
                persistence.SnapshotCount == 0 && controls.NativeCastRequestCount == 0 &&
                replayAuthorization.AuthorizedLoadCount == 1 && replayAuthorization.AuthorizedWriteCount == 0 &&
                replaySaveAuthority.AdmittedNativeLoadRequests == 1 && replaySaveAuthority.AdmittedNativeWriteRequests == 0 &&
                actors.All(actor => actor.IsDirectlyControllable && Chunk7NativeFixture.Idle(actor) && actor.View != null),
                "Capital arrival started across an unsettled native boundary, a spent quota or an uncontrollable restored actor.");
            var enter = target == null ? null : ResourcesLibrary.TryGetBlueprint<BlueprintAreaEnterPoint>(target.EnterPoint);
            Require(target != null && target.AutoSaveMode == "None" && target.EnterPoint == Chunk7OwnedPairsSelection.CapitalArrivalEntry &&
                target.Area == Chunk7OwnedPairsSelection.CapitalArrivalArea && enter != null && enter.AssetGuidThreadSafe == target.EnterPoint &&
                enter.Area != null && enter.Area.AssetGuidThreadSafe == target.Area &&
                game.CurrentlyLoadedArea.AssetGuidThreadSafe == request.PersistenceLoad.Area &&
                !ReferenceEquals(enter.Area, game.CurrentlyLoadedArea) && !game.CurrentlyLoadedArea.IsCapital,
                "Capital arrival cannot resolve its exact declared native entry from its non-capital start.");
            // The scoped admission: this one route must natively enter a capital.
            Require(enter.Area.IsCapital, "The declared capital-arrival destination is not natively a capital.");
            capitalMainPair = saved.Pairs.Single(pair => pair.Rider.Id == firstRider.UniqueId);
            capitalRetiredPair = saved.Pairs.Single(pair => pair.Rider.Id == secondRider.UniqueId);
            Require(capitalMainPair.Mount.Id == firstMount.UniqueId && capitalRetiredPair.Mount.Id == secondMount.UniqueId &&
                saved.Slots.Count(slot => slot.ActorId == firstRider.UniqueId) == 2 && saved.Slots.Count(slot => slot.ActorId == secondRider.UniqueId) == 2,
                "Capital arrival saved roles or control bindings differ from the restored pairs.");
            foreach (var actor in actors) capitalViewIds.Add(actor.UniqueId, actor.View.GetInstanceID());
            var mainRelation = world.Registry.FindActive(firstRider).Boundary;
            Require(mainRelation.Runtime.MovementAgent != null && mainRelation.Runtime.ValidateMountedInvariants() == null,
                "Capital arrival baseline lacks the main pair's exact mounted movement authority.");
            capitalMainAgentBaseline = mainRelation.Runtime.MovementAgent.GetInstanceID();
            areaActorDebt = actors.Select(MountedPersistenceService.CaptureActor).ToArray();
            areaNativeTicks = game.TimeController.GameTime.Ticks;
            capitalCastBaseline = controls.NativeCastRequestCount; snapshotBefore = persistence.SnapshotCount;
            inherited.BeginCapitalArrival(enter.Area);
            capitalRow = new JObject { ["contract"] = "chunk7-capital-arrival-v1", ["enterPoint"] = target.EnterPoint,
                ["sourceArea"] = request.PersistenceLoad.Area, ["destinationArea"] = target.Area, ["autoSaveMode"] = "None",
                ["destinationIsCapital"] = true, ["mainPairId"] = capitalMainPair.PairId, ["retiredPairId"] = capitalRetiredPair.PairId,
                ["retiredRiderName"] = secondRider.CharacterName, ["retiredMountName"] = secondMount.CharacterName,
                ["transferRequests"] = 1, ["nativeAutosaveRequested"] = false, ["before"] = CaptureCapitalWorld("before-native-transfer") };
            evidence["capitalArrival"] = capitalRow;
            capitalDestination = enter.Area; capitalDispatched = true; Next(50);
            game.LoadArea(enter, AutoSaveMode.None);
            capitalRow["dispatchReturned"] = true;
        }

        // Called before the ordinary LoadingProcess early return. Returns true while row 58 owns Update.
        private bool UpdateCapitalArrival()
        {
            if (!CapitalArrivalCase || stage != 50 && stage != 51) return false;
            if (stage == 50)
            {
                nativeScope.RequireTransferWorld();
                if (LoadingProcess.Instance.IsLoadingInProcess || LoadingProcess.Instance.QueuedNames.Any())
                {
                    capitalLoadingTicks++;
                    if (persistence.AreaSuspensionCount == 1 && world.Registry.ActiveCount == 0) capitalSuspensionObserved = true;
                    capitalRow["awaitingNativeReady"] = CaptureCapitalBoundary();
                    return true;
                }
                // Read-only: wait for the product's single restore attempt and the transfer to close.
                if (persistence.AreaTransitionPending || NativePersistenceIsolation.HasPendingWrites || persistence.HasActiveSaveScope ||
                    persistence.HasSaveAdmissionFence || persistence.AreaResumeCount + persistence.AreaRetiredPairCount + persistence.FailedAreaPairCount == 0)
                {
                    capitalRow["awaitingNativeReady"] = CaptureCapitalBoundary();
                    return true;
                }
                capitalRow["arrival"] = CaptureCapitalBoundary();
                AdoptCapitalArrival();
                // Saving stays allowed: the generic owned admission requires native IsSaveAllowed.
                RequestSave(); Next(51);
                return true;
            }
            if (LoadingProcess.Instance.IsLoadingInProcess || LoadingProcess.Instance.QueuedNames.Any()) return true;
            RequireNativeWorld(); inherited.RequirePreserved();
            if (!callback || NativePersistenceIsolation.HasPendingWrites || persistence.HasActiveSaveScope || persistence.HasSaveAdmissionFence) return true;
            ObserveCapitalSaved(); BeginCleanup();
            return true;
        }

        private void AdoptCapitalArrival()
        {
            var target = request.PersistenceAreaTarget;
            Require(CapitalArrivalCase && stage == 50 && capitalDispatched && !capitalAdopted && capitalDestination != null &&
                !LoadingProcess.Instance.IsLoadingInProcess && !LoadingProcess.Instance.QueuedNames.Any(),
                "Capital destination custody cannot be adopted before its sole completed native entry.");
            Require(persistence.FailedAreaPairCount == 0, "Capital arrival restoration failed: " + persistence.Feedback);
            Require(capitalLoadingTicks > 0 && capitalSuspensionObserved, "Capital arrival missed its native loading or all-owner suspension boundary.");
            Require(persistence.AreaSuspensionCount == 1 && persistence.AreaResumeCount == 1 && persistence.AreaRetiredPairCount == 1 &&
                persistence.RefusedAreaTransferCount == 0 && !persistence.AreaTransitionPending && persistence.NativeWorldDisposalCount == 0 &&
                persistence.SemanticRestoreCount == 4 && persistence.PresentationRestoreCount == 2 && persistence.ArchivePostLoadObservedCount == 4 &&
                persistence.SnapshotCount == snapshotBefore && controls.NativeCastRequestCount == capitalCastBaseline &&
                replayAuthorization.AuthorizedLoadCount == 1 && replayAuthorization.AuthorizedWriteCount == 0 &&
                replaySaveAuthority.AdmittedNativeLoadRequests == 1 && replaySaveAuthority.AdmittedNativeWriteRequests == 0,
                "Capital arrival did not suspend both pairs, restore exactly the main pair and retire exactly one pair, without input, load or write.");
            nativeScope.RequireDeclaredAreaDestination(target.Area, capitalDestination);
            Require(Game.Instance.CurrentlyLoadedArea.IsCapital && Game.Instance.CurrentMode == GameModeType.Default && !Game.Instance.Player.IsInCombat,
                "Capital arrival did not settle in the exact default-mode capital.");
            // The prior scope is immutable. Only this completed declared transfer pins the capital world.
            inherited.AdoptCapitalArrival(capitalDestination);
            nativeScope = inherited.Scope; capitalAdopted = true;
            expectedOwners.Clear(); expectedOwners.Add(Guid.Parse(capitalMainPair.PairId), capitalMainPair.Generation);
            slots = saved.Slots.Where(slot => slot.ActorId == firstRider.UniqueId).ToArray();
            AssertCapitalOwners(); AssertCapitalMainPair(); AssertCapitalRetiredPair(); AssertSlots(); AssertAreaNativeDebt();
            var feedback = ExpectedCapitalFeedback(secondRider.CharacterName, secondMount.CharacterName, 1, 2);
            Require(persistence.Feedback == feedback, "The UMM summary does not name the dismounted pair exactly: " + persistence.Feedback);
            var ids = actors.Select(actor => actor.UniqueId).ToArray();
            Require(restoration != null && restoration.Complete && restoration.Count("prepare-before", ids) == 0 &&
                restoration.Count("round-before", ids) == 0 && restoration.Count("restore-actor-before", ids) == 0 &&
                restoration.Count("restore-debt-before", ids) == 4 && restoration.Count("restore-debt-after", ids) == 4,
                "Capital arrival invented native preparation, a round or a debt restore.");
            capitalRow["restoration"] = restoration.Capture(); restoration.Dispose(); restoration = null;
            capitalRow["umm"] = persistence.Feedback;
            capitalRow["after"] = CaptureCapitalWorld("after-native-transfer");
            capitalRow["loadingTicks"] = capitalLoadingTicks; capitalRow["suspensionObserved"] = capitalSuspensionObserved;
            snapshots.Add(Capture("capital-arrival-settled"));
            Pass("C7-persistence-capital-arrival-retirement");
        }

        // Cleanup may adopt the declared capital only through the same exact arrival proof; otherwise the
        // captured source scope still refuses and custody is retained for the bounded unattended exit.
        private void AdoptCapitalArrivalForCleanup()
        {
            if (CapitalArrivalCase && capitalDispatched && !capitalAdopted && stage == 50 && !LoadingProcess.Instance.IsLoadingInProcess &&
                !LoadingProcess.Instance.QueuedNames.Any() && ReferenceEquals(Game.Instance?.CurrentlyLoadedArea, capitalDestination))
                AdoptCapitalArrival();
        }

        internal static string ExpectedCapitalFeedback(string rider, string mount, int restored, int total) =>
            CapitalFeedbackPrefix + rider + " and " + mount + ". Mount again after leaving. " + restored + " of " + total +
            " mounted relationships restored without Mount or preparation.";

        // One owner after arrival (row 58) or after the capital cold load (row 59): the main pair, nothing else.
        private void AssertCapitalOwners()
        {
            Require(Mounted(firstRider, firstMount) && world.Registry.ActiveCount == 1 && world.Registry.Owners.Length == 1 &&
                world.Registry.Owners.All(owner => expectedOwners.TryGetValue(owner.Id, out var generation) && generation == owner.Generation) &&
                world.Registry.FindOwner(secondRider) == null && world.Registry.FindOwner(secondMount) == null &&
                world.Pairs.Count(pair => pair.Relationship.State != RelationshipState.Unmounted || pair.Relationship.PairOwner != null) == 1,
                "Capital world has an orphan, foreign or second mounted owner.");
        }

        private void AssertCapitalMainPair()
        {
            var relation = world.Registry.FindActive(firstRider).Boundary; var runtime = relation.Runtime;
            var overrides = firstRider.View.GetComponents<RiderMovementAgent>();
            Require(firstRider.IsDirectlyControllable && firstMount.IsDirectlyControllable &&
                actors.All(actor => actor.View != null && ReferenceEquals(actor.View.EntityData, actor) && actor.View.GetInstanceID() == capitalViewIds[actor.UniqueId]),
                "Capital arrival replaced an original native view or lost main-pair control.");
            Require(runtime.MovementAgent != null && runtime.MovementAgent.GetInstanceID() != capitalMainAgentBaseline && overrides.Length == 1 &&
                ReferenceEquals(overrides[0], runtime.MovementAgent) && ReferenceEquals(firstRider.View.AgentOverride, runtime.MovementAgent) &&
                !firstRider.View.AgentASP.enabled && firstMount.View.AgentASP.enabled && firstMount.View.AgentOverride == null &&
                firstMount.View.GetComponents<RiderMovementAgent>().Length == 0 && relation.IsExactCapturedView(firstRider) &&
                relation.IsExactCapturedView(firstMount) && !relation.SaveSerializationSuspended && runtime.ValidateMountedInvariants() == null &&
                !world.UnifiedTurn.HasUnsettledPreparation,
                "Capital main pair reused or duplicated movement authority or carries preparation.");
        }

        // Observed separation of the retired companion pair, matching the product's own residue checks.
        private void AssertCapitalRetiredPair(bool baselineViews = true)
        {
            Require(!secondRider.IsDirectlyControllable && !secondMount.IsDirectlyControllable &&
                ReferenceEquals(secondRider.Descriptor.Pet, secondMount) && ReferenceEquals(secondMount.Descriptor.Master.Value, secondRider),
                "Retired pair is not exactly the companion and its pet excluded by native capital control.");
            Require(world.Registry.FindActive(secondRider) == null && world.Registry.FindActive(secondMount) == null &&
                world.Pairs.All(pair => !(ReferenceEquals(pair.Relationship.Rider, secondRider) || ReferenceEquals(pair.Relationship.Mount, secondMount) ||
                    ReferenceEquals(pair.Relationship.Rider, secondMount) || ReferenceEquals(pair.Relationship.Mount, secondRider)) ||
                    pair.Relationship.State == RelationshipState.Unmounted && pair.Relationship.PairOwner == null &&
                    pair.Relationship.Runtime.NoPreparedPairResidue && !pair.Relationship.Runtime.HasPresentationAttachmentResidue &&
                    !pair.Combat.HasRetainedPairCommands && !controls.HasUnsettledShellFor(pair.Relationship)),
                "Retired pair retains an owner, a relationship or owned residue.");
            Require(secondRider.View != null && secondMount.View != null &&
                (!baselineViews || secondRider.View.GetInstanceID() == capitalViewIds[secondRider.UniqueId] && secondMount.View.GetInstanceID() == capitalViewIds[secondMount.UniqueId]) &&
                !secondRider.View.transform.IsChildOf(secondMount.View.transform) && !secondMount.View.transform.IsChildOf(secondRider.View.transform) &&
                secondRider.View.AgentOverride == null && secondMount.View.AgentOverride == null &&
                secondRider.View.GetComponents<RiderMovementAgent>().Length == 0 && secondMount.View.GetComponents<RiderMovementAgent>().Length == 0 &&
                CapitalFinite(secondRider.Position) && CapitalFinite(secondMount.Position),
                "Retired pair views are not separate, unreplaced and free of movement authority.");
        }
        private static bool CapitalFinite(UnityEngine.Vector3 v) => !float.IsNaN(v.x) && !float.IsInfinity(v.x) &&
            !float.IsNaN(v.y) && !float.IsInfinity(v.y) && !float.IsNaN(v.z) && !float.IsInfinity(v.z);

        private void ObserveCapitalSaved()
        {
            var type = SaveInfo.SaveType.Manual;
            savedDescriptor = Game.Instance.SaveManager.SingleOrDefault(s => s.Name == NativeName(type, request.OwnedPairs, request.PersistenceCase) && s.Type == type);
            Require(savedDescriptor != null && savedDescriptor.OperationState == SaveInfo.StateType.None && savedDescriptor.HasFileOnDisk &&
                savedDescriptor.FileName == SelectedOutputLeaf(type), "Capital Manual did not complete at its exact reserved leaf.");
            ObserveOwnedSaveCompletion();
            var read = NativeMountedSaveStorage.ReadArchive(savedDescriptor.Saver); var written = read.Data;
            Require(read.Kind == MountedSaveReadKind.Current && read.SourceSchema == 3 && written != null && written.SchemaVersion == 3 &&
                written.Pairs.Length == 1 && written.Participations.Length == 0 && written.Combat == null &&
                written.CampaignId == request.PersistenceLoad.GameId && written.AreaId == Chunk7OwnedPairsSelection.CapitalArrivalArea,
                "Capital Manual readback is not exactly one mounted exploration pair in the capital.");
            var pair = written.Pairs[0];
            Require(pair.PairId == capitalMainPair.PairId && pair.Generation == capitalMainPair.Generation && pair.ProfileId == capitalMainPair.ProfileId &&
                pair.Rider.Id == firstRider.UniqueId && pair.Mount.Id == firstMount.UniqueId &&
                written.Slots.Length == slots.Length && slots.All(slot => written.Slots.Count(value => value.ActorId == slot.ActorId &&
                    value.Index == slot.Index && value.Kind == slot.Kind) == 1),
                "Capital Manual readback changed the main pair's stable custody or omitted its exact controls.");
            AssertCapitalOwners(); AssertCapitalRetiredPair(); AssertSlots(); AssertDebt(written);
            Require(callback && ownedSaveCallbacks == 1 && persistence.SnapshotCount == snapshotBefore + 1 && controls.NativeCastRequestCount == castBefore &&
                !controls.SerializationSuspended && !persistence.SaveSuspended && persistence.AreaSuspensionCount == 1 && persistence.AreaResumeCount == 1 &&
                persistence.AreaRetiredPairCount == 1 && persistence.FailedAreaPairCount == 0 && Game.Instance.SaveManager.IsSaveAllowed(),
                "Capital Manual replayed input, retained a serialization lease or blocked later saving.");
            saved = written;
            var file = new FileInfo(savedDescriptor.FolderName);
            evidence["nativeWrite"] = new JObject { ["path"] = file.FullName, ["sha256"] = Hash(file.FullName), ["length"] = file.Length,
                ["nativeType"] = type.ToString(), ["nativeName"] = savedDescriptor.Name, ["nativeCallback"] = callback,
                ["operation"] = savedDescriptor.OperationState.ToString(), ["snapshotCount"] = persistence.SnapshotCount,
                ["area"] = savedDescriptor.Area?.AssetGuidThreadSafe, ["saveAllowedAfterWrite"] = true,
                ["archive"] = JObject.FromObject(written, MountedSaveCodec.CreateSerializer()) };
            snapshots.Add(Capture("native-write-complete"));
            Pass("C7-persistence-capital-arrival-native-manual");
        }

        private JObject CaptureCapitalWorld(string label)
        {
            var game = Game.Instance;
            var row = Capture(label);
            row["area"] = game.CurrentlyLoadedArea?.AssetGuidThreadSafe; row["capital"] = game.CurrentlyLoadedArea?.IsCapital;
            row["mode"] = game.CurrentMode.ToString();
            row["suspensions"] = persistence.AreaSuspensionCount; row["resumes"] = persistence.AreaResumeCount;
            row["retired"] = persistence.AreaRetiredPairCount; row["failedPairs"] = persistence.FailedAreaPairCount;
            row["refusedTransfers"] = persistence.RefusedAreaTransferCount; row["activeOwners"] = world.Registry.ActiveCount;
            row["saveAllowed"] = game.SaveManager.IsSaveAllowed(); row["feedback"] = persistence.Feedback;
            row["slots"] = JArray.FromObject(controls.CapturePersistentSlots(), MountedSaveCodec.CreateSerializer());
            row["views"] = new JArray(actors.Select(actor => new JObject { ["id"] = actor.UniqueId,
                ["viewId"] = actor.View == null ? 0 : actor.View.GetInstanceID(),
                ["baselineViewId"] = capitalViewIds.TryGetValue(actor.UniqueId, out var baseline) ? baseline : 0,
                ["directlyControllable"] = actor.IsDirectlyControllable, ["inGame"] = actor.IsInGame,
                ["masterId"] = actor.Descriptor.Master.Value?.UniqueId, ["petId"] = actor.Descriptor.Pet?.UniqueId,
                ["agentOverride"] = actor.View?.AgentOverride == null ? 0 : actor.View.AgentOverride.GetInstanceID(),
                ["riderAgents"] = actor.View == null ? 0 : actor.View.GetComponents<RiderMovementAgent>().Length,
                ["stockAgentEnabled"] = actor.View?.AgentASP?.enabled })) ;
            return row;
        }

        // Read-only bounded boundary record; grants no destination scope.
        private JObject CaptureCapitalBoundary() => new JObject {
            ["frame"] = UnityEngine.Time.frameCount,
            ["nativeLoading"] = LoadingProcess.Instance.IsLoadingInProcess,
            ["queuedLoadNames"] = LoadingProcess.Instance.QueuedNames.Count(),
            ["area"] = Game.Instance.CurrentlyLoadedArea?.AssetGuidThreadSafe,
            ["capital"] = Game.Instance.CurrentlyLoadedArea?.IsCapital,
            ["suspensions"] = persistence.AreaSuspensionCount, ["resumes"] = persistence.AreaResumeCount,
            ["retired"] = persistence.AreaRetiredPairCount, ["failedPairs"] = persistence.FailedAreaPairCount,
            ["transitionPending"] = persistence.AreaTransitionPending, ["activeOwners"] = world.Registry.ActiveCount,
            ["nativeWritePending"] = NativePersistenceIsolation.HasPendingWrites,
            ["saveScope"] = persistence.HasActiveSaveScope, ["saveFence"] = persistence.HasSaveAdmissionFence,
            ["feedback"] = persistence.Feedback,
            ["actorControl"] = new JArray((actors ?? new UnitEntityData[0]).Select(actor => new JObject {
                ["id"] = actor?.UniqueId, ["inGame"] = actor?.IsInGame, ["directlyControllable"] = actor?.IsDirectlyControllable,
                ["masterId"] = actor == null || actor.Descriptor == null ? null : actor.Descriptor.Master.Value?.UniqueId })) };
    }
}
