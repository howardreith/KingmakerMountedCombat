using System;
using System.IO;
using System.Linq;
using Kingmaker;
using Kingmaker.Visual.CharacterSystem;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Integration
{
    internal sealed partial class MountedPersistenceService
    {
        private NativeMountedPresentationReadiness archivePresentationReadiness;
        private string archivePresentationWaiting;
        private bool ArchiveHasDeclaredSmallPresentation => RestorableArchive?.Pairs.Any(pair =>
            MountedRiderProfileCatalog.IsDeclaredSmallProfile(pair.ProfileId)) == true;
        // Selected only for a recognized Small archive. This uses the existing
        // native command/Prepare/cooldown admission fence while its loaded rig is
        // incomplete. It never pauses/writes the game clock or any actor budget.
        private bool ArchiveSmallPresentationPending => Enabled && presentationPending && ArchiveHasDeclaredSmallPresentation;

        internal void ObserveNativePresentationLateUpdate(Character avatar) =>
            archivePresentationReadiness?.ObserveLateUpdate(avatar);

        private bool PreflightArchivePresentation()
        {
            if (!ArchiveHasDeclaredSmallPresentation) return true;
            if (!ArchiveSmallPresentationPending || CombatSemanticRestorationPending ||
                restoreLoad == null || !restoreLoad.World.CanPresent(Game.Instance?.Player))
                throw new InvalidDataException("Small rig preflight lost its exact completed semantic world and native admission fence.");
            ValidateArchiveActorIdentity();
            var data = RestorableArchive;
            foreach (var actor in ArchivePresentationNativeActorIds())
                if (!restoredActors.TryGetValue(actor, out var restored) || !ReferenceEquals(restored, archiveActors[actor]))
                    throw new InvalidDataException("Small rig preflight requires every exact native actor to be restored once before visual waiting.");
            var custody = CapturePresentationCustody();
            if (archivePresentationReadiness == null)
                archivePresentationReadiness = new NativeMountedPresentationReadiness(data, archiveActors, custody);
            var result = archivePresentationReadiness.Check(custody);
            if (result.Kind == MountedPresentationReadinessKind.Incompatible)
                throw new InvalidDataException(result.Reason);
            if (result.Kind == MountedPresentationReadinessKind.Waiting)
            {
                if (archivePresentationWaiting != result.Reason)
                { archivePresentationWaiting = result.Reason; Report(result.Reason); }
                return false;
            }
            archivePresentationWaiting = null;
            return true;
        }

        private string[] ArchivePresentationNativeActorIds() =>
            (RestorableArchive.Combat?.Actors.Select(actor => actor.Native.Id) ?? Enumerable.Empty<string>())
                .Concat(RestorableArchive.Pairs.SelectMany(pair => new[] { pair.Rider.Id, pair.Mount.Id }))
                .Distinct(StringComparer.Ordinal).ToArray();

        private PresentationRestoreCustody CapturePresentationCustody()
        {
            // This pin begins AFTER semantic restoration. Any legitimate native
            // decay before this boundary is already reflected in current debt;
            // it is not compared with an earlier archive amount. During this
            // exact wait CombatCooldownPrefix denies native TickOnUnit, as it
            // does during semantic loading. GameTime is free to advance and is
            // deliberately absent from the equality fingerprint. If that fence
            // is no longer selected, refuse instead of treating elapsed time as
            // permission for an unobserved refund or preparation.
            if (!ArchiveSmallPresentationPending || !CombatRestorationPending)
                throw new InvalidDataException("Pending Small rig lost native cooldown and command admission custody.");
            var data = RestorableArchive;
            var serializer = MountedSaveCodec.CreateSerializer();
            var actors = ArchivePresentationNativeActorIds().Select(id => {
                var actor = archiveActors[id];
                var state = new JObject { ["native"] = JObject.FromObject(CaptureActor(actor), serializer),
                    ["prepared"] = actor.CombatState.Prepared };
                return new PresentationActorCustody(id, actor, state.ToString(Formatting.None));
            }).ToArray();
            var owners = mountedWorld.Registry.Owners.Select(owner => {
                if (owner.Boundary.PairOwner != owner || !ReferenceEquals(owner.Session, Game.Instance.Player))
                    throw new InvalidDataException("Pending Small rig lost exact imported native ownership.");
                var pair = mountedWorld.Pairs.SingleOrDefault(p => ReferenceEquals(p.Relationship, owner.Boundary));
                if (pair == null) throw new InvalidDataException("Pending owner has no unique native service boundary.");
                var participation = pair.Participant.CaptureParticipation();
                return new PresentationOwnerCustody(owner.Id, owner.Generation, owner, owner.Boundary, owner.Rider, owner.Mount,
                    owner.State.ToString(), participation == null ? "null" : JObject.FromObject(participation, serializer).ToString(Formatting.None));
            }).ToArray();
            var allocationSnapshot = new SavedCombatData();
            unifiedTurn.CaptureSharedPersistence(allocationSnapshot);
            return new PresentationRestoreCustody(Game.Instance.Player, loadSequence, data,
                JObject.FromObject(data, serializer).ToString(Formatting.None),
                JArray.FromObject(allocationSnapshot.Allocations, serializer).ToString(Formatting.None),
                data.Pairs.Select(p => p.PairId), actors, owners);
        }

        private void ClearArchivePresentationReadiness()
        {
            archivePresentationReadiness = null;
            archivePresentationWaiting = null;
        }
    }
}
