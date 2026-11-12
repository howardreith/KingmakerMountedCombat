using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints.Area;
using Kingmaker.EntitySystem.Persistence;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Integration
{
    internal sealed partial class MountedPersistenceService
    {
        private AreaTransfer areaTransfer;
        private AreaTransfer failedAreaTransfer;
        internal int FailedAreaPairCount => failedAreaTransfer?.Pairs?.Length ?? 0;
        private string areaRestoreFailure;
        internal bool AreaTransitionPending => areaTransfer != null;
        internal int AreaSuspensionCount { get; private set; }
        internal int AreaResumeCount { get; private set; }
        internal int AreaRetiredPairCount { get; private set; }
        internal int RefusedAreaTransferCount { get; private set; }

        internal bool AdmitChargeAreaTransition()
        {
            if (areaRestoreFailure == null && !HasAreaParticipationObligations() &&
                !HasActiveSaveScope && !HasSaveAdmissionFence && !AnyChargeFence &&
                DrainWorldCharge("area transition admission")) return true;
            RefusedAreaTransferCount++;
            Report("Area change refused: a mounted activation, pending survivor, command, cleanup or save still owns this world boundary.");
            return false;
        }

        private bool HasAreaParticipationObligations()
        {
            var unsettled = false;
            foreach (var participant in unifiedTurn.SnapshotParticipants)
                if (!participant.TrySettleExplorationParticipation() || controls.HasUnsettledShellFor(participant.Relationship))
                    unsettled = true;
            return unsettled;
        }

        internal void BeginAreaTransition(BlueprintArea area, SaveInfo saveInfo)
        {
            var world = Game.Instance?.Player;
            if (saveInfo != null) { CancelAreaTransition(); DropRetainedIntent("a save is being loaded"); return; }
            if (areaTransfer != null && areaTransfer.World.Target == world && areaTransfer.AreaId == area?.AssetGuidThreadSafe) return;
            CancelAreaTransition();
            if (restoreLoad != null && (!restoreLoad.World.NativeCompleted || presentationPending))
            {
                FenceAbandonedCombat(); restoreLoad.World.Close(); restoreLoad = null;
                presentationPending = false; restoredActors.Clear(); loaded = null; ClearArchiveRestoration(); loadSequence++;
            }
            if (!Enabled || world == null || area == null) return;
            if (HasAreaParticipationObligations())
                throw new InvalidOperationException("Area transfer cannot discard retained native participation.");
            var mounted = WorldRelationships.Where(r => r.State == RelationshipState.Mounted).ToArray();
            // Exploration repair (issue 7): intent retained by world-map travel or by a scripted event in this world
            // is carried by the transfer like the pairs mounted at departure; an area change never discards it.
            var carried = retainedIntent != null && ReferenceEquals(retainedIntent.World.Target, world) &&
                retainedIntent.CampaignId == world.GameId ? retainedIntent : null;
            if (retainedIntent != null && carried == null) DropRetainedIntent("the world changed");
            if (mounted.Length == 0 && carried == null) return;
            if (world.IsInCombat || mounted.Any(r => r.Rider.IsInCombat || r.Mount.IsInCombat))
            {
                if (carried != null) DropRetainedIntent("combat began");
                return;
            }
            if (SaveDraining || SaveSuspended || HasSaveAdmissionFence)
            {
                RefusedAreaTransferCount++;
                if (carried != null) DropRetainedIntent("a mounted save is still owned");
                Report("A mounted save is still owned; pairs are not carried across this area change.");
                return;
            }
            var departing = mounted.Select(r => new AreaPair { RiderId = r.Rider.UniqueId, MountId = r.Mount.UniqueId,
                ProfileId = r.Runtime.MountProfileId, OwnerId = r.PairOwner?.Id.ToString("N"),
                Generation = r.PairOwner?.Generation ?? 0, Participation = unifiedTurn.SnapshotParticipants
                    .Single(p => ReferenceEquals(p.Relationship, r)).CaptureParticipation() }).ToArray();
            var departingIds = new HashSet<string>(departing.SelectMany(p => new[] { p.RiderId, p.MountId }), StringComparer.Ordinal);
            var carriedPairs = (carried?.Pairs ?? new AreaPair[0])
                .Where(p => !departingIds.Contains(p.RiderId) && !departingIds.Contains(p.MountId))
                .Select(p => new AreaPair { RiderId = p.RiderId, MountId = p.MountId, ProfileId = p.ProfileId, OwnerId = p.OwnerId,
                    Generation = p.Generation, Participation = p.Participation, Carried = true }).ToArray();
            var slots = controls.CapturePersistentSlots();
            var slotActors = new HashSet<string>(slots.Select(s => s.ActorId), StringComparer.Ordinal);
            areaTransfer = new AreaTransfer {
                World = new WeakReference(world), CampaignId = world.GameId, AreaId = area.AssetGuidThreadSafe,
                Pairs = departing.Concat(carriedPairs).ToArray(),
                TurnBased = TurnBased.Controllers.CombatController.IsInTurnBasedCombat(),
                Slots = slots.Concat((carried?.Slots ?? new SavedMountedSlot[0]).Where(s => !slotActors.Contains(s.ActorId))).ToArray(),
                WorldMap = IsWorldMapArea(area)
            };
            if (carried != null) { retainedIntent = null; ResetRetainedSettle(); }
        }

        internal bool SuspendAreaPair()
        {
            var transfer = areaTransfer;
            if (transfer == null || !Enabled || transfer.World.Target != Game.Instance?.Player) return false;
            if (transfer.Suspended) return true;
            try
            {
                if (!DrainWorldCharge("area suspension"))
                    throw new InvalidOperationException("Area suspension retained charge cleanup ownership.");
                if (HasAreaParticipationObligations())
                    throw new InvalidOperationException("Area suspension encountered retained native participation.");
                // Ordinary exploration area transfer does NOT clear the shared
                // actor allocation ledger. Native encounter/actor lifetime owns
                // its retirement; attachment cleanup cannot erase expenditure.
                controls.SuspendAreaControls();
                var errors = new List<Exception>();
                foreach (var owner in WorldRelationships)
                    try
                    {
                        if (!owner.GuardBoundary(CleanupTrigger.AreaSuspension))
                            errors.Add(new InvalidOperationException("Area suspension retained mounted references."));
                    }
                    catch (Exception error) { errors.Add(error); }
                if (errors.Count != 0) throw new AggregateException(errors);
                mountedWorld?.DiscardAdmissionBindings();
                transfer.Suspended = true; AreaSuspensionCount++;
                Report("All " + transfer.Pairs.Length + " mounted relationships suspended for native area placement.");
                return true;
            }
            catch (Exception error)
            {
                failedAreaTransfer = transfer;
                areaRestoreFailure = "Mounted area suspension failed: " + error.Message;
                Report(areaRestoreFailure + " Original transfer custody is retained; new participation and saving remain blocked.");
                CancelAreaTransition();
                throw;
            }
        }

        internal void RestoreAreaPair()
        {
            var transfer = areaTransfer;
            if (transfer == null || !transfer.Suspended || transfer.ResumeAttempted) return;
            transfer.ResumeAttempted = true;
            var game = Game.Instance;
            try
            {
                if (!Enabled || transfer.World.Target != game?.Player || game.Player.GameId != transfer.CampaignId ||
                    game.CurrentlyLoadedArea?.AssetGuidThreadSafe != transfer.AreaId)
                    throw new InvalidOperationException("Area world changed before mounted restoration.");
                // Exploration repair (issue 7): the world map has no physical rider-and-mount placement (native
                // GlobalMapRules enters GlobalMap mode during scene init, so attachment is never admitted there).
                // The suspended pairs travel as retained intent and restore at the next local area's arrival;
                // nothing is latched, so saving and area entry stay admitted.
                if (transfer.WorldMap)
                {
                    retainedIntent = new AreaTransfer { World = transfer.World, CampaignId = transfer.CampaignId, AreaId = transfer.AreaId,
                        Pairs = transfer.Pairs, Slots = transfer.Slots };
                    retainedIntentKind = MountedRetainedIntentKind.WorldMapTravel;
                    ResetRetainedSettle();
                    areaTransfer = null;
                    RetainedSuspensionCount++;
                    Report(transfer.Pairs.Length + " mounted relationship" + (transfer.Pairs.Length == 1 ? "" : "s") +
                        " carried across the world map; the pair remounts on arrival in the next area.", true);
                    return;
                }
                var carriedDropped = new List<string>();
                var arrivalInCombat = game.Player.IsInCombat;
                // Resolve every pair and slot before changing the first attachment.
                var resolved = new Dictionary<string, Kingmaker.EntitySystem.Entities.UnitEntityData>(StringComparer.Ordinal);
                foreach (var id in transfer.Pairs.SelectMany(p => new[] { p.RiderId, p.MountId })
                    .Concat(transfer.Slots.Select(s => s.ActorId)).Distinct(StringComparer.Ordinal))
                {
                    var matches = game.State.Units.Concat(game.Player.AllCharacters).Where(u => u != null && u.UniqueId == id).Distinct().ToArray();
                    if (matches.Length == 1) { resolved.Add(id, matches[0]); continue; }
                    // A carried pair (intent retained over the world map or an earlier event) whose actor is gone stays
                    // dismounted on its own; it never blocks the other pairs or later area changes.
                    if (transfer.Pairs.Any(p => p.Carried && (p.RiderId == id || p.MountId == id)) &&
                        !transfer.Pairs.Any(p => !p.Carried && (p.RiderId == id || p.MountId == id))) continue;
                    throw new InvalidOperationException("An area actor did not resolve uniquely: " + id);
                }
                // Admission is all-pair: a destination that cannot directly
                // control the second pair must not attach the first pair. The one
                // exception is the native capital rule (owner decision): that pair is
                // retired, dismounted, after its separation is observed, and the rest restore.
                var area = game.CurrentlyLoadedArea;
                var restore = new List<AreaPair>(); var retire = new List<AreaPair>();
                foreach (var pair in transfer.Pairs)
                {
                    if (pair.Carried)
                    {
                        Kingmaker.EntitySystem.Entities.UnitEntityData carriedRider, carriedMount;
                        resolved.TryGetValue(pair.RiderId, out carriedRider); resolved.TryGetValue(pair.MountId, out carriedMount);
                        var present = carriedRider != null && carriedMount != null;
                        var carriedRefusal = !present ? "an actor is not present" :
                            SupportedMountedProfiles.Resolve(carriedRider, carriedMount)?.Id != pair.ProfileId ? "the mount profile changed" :
                            KingmakerMountedPairRuntime.CreateCandidate(carriedRider, carriedMount)?.ValidateSavedRelationship();
                        if (MountedRetainedIntentPolicy.CarriedArrivalRestores(arrivalInCombat, present, carriedRefusal)) restore.Add(pair);
                        else carriedDropped.Add((carriedRider?.CharacterName ?? pair.RiderId) + " and " + (carriedMount?.CharacterName ?? pair.MountId) +
                            " (" + (arrivalInCombat ? "the party arrived in combat" : carriedRefusal) + ")");
                        continue;
                    }
                    var rider = resolved[pair.RiderId]; var mount = resolved[pair.MountId];
                    var profile = SupportedMountedProfiles.Resolve(rider, mount);
                    var candidate = KingmakerMountedPairRuntime.CreateCandidate(rider, mount);
                    var refusal = candidate?.ValidateSavedRelationship();
                    if (profile?.Id != pair.ProfileId)
                        throw new InvalidOperationException("Area relationship was not eligible: The saved mount profile changed.");
                    if (refusal == null) { restore.Add(pair); continue; }
                    if (!IsCapitalControlRetirement(area, rider, mount, candidate, refusal))
                        throw new InvalidOperationException("Area relationship was not eligible: " + refusal);
                    retire.Add(pair);
                }
                foreach (var pair in retire)
                {
                    var residue = RetiredAreaPairResidue(pair, resolved[pair.RiderId], resolved[pair.MountId]);
                    if (residue != null) throw new InvalidOperationException("Capital-arrival retirement was not clean: " + residue);
                }
                foreach (var pair in restore)
                {
                    if (pair.Carried)
                    {
                        // Retained intent (world-map travel or an earlier in-area event): the pair was dismounted outside
                        // combat with settled participation, so it is admitted like an ordinary pair boundary and
                        // re-attached without Mount or preparation; a refusal leaves only that pair dismounted.
                        var carriedRider = resolved[pair.RiderId]; var carriedMount = resolved[pair.MountId];
                        var admitted = mountedWorld != null ? mountedWorld.GetOrCreateAdmission(carriedRider, carriedMount) : relationship;
                        var carriedResult = admitted?.RestoreSavedPair(carriedRider, carriedMount, pair.ProfileId);
                        if (carriedResult == null || !carriedResult.Succeeded)
                        {
                            carriedDropped.Add(carriedRider.CharacterName + " and " + carriedMount.CharacterName + " (" +
                                (carriedResult == null ? "no pair boundary is available" : string.Join("; ", carriedResult.Errors)) + ")");
                            continue;
                        }
                        AreaResumeCount++; RetainedRestoreCount++;
                        continue;
                    }
                    var owner = ArchiveNativeEnabled ? mountedWorld.CreateRestoredBoundary().Relationship : relationship;
                    if (ArchiveNativeEnabled)
                        owner.ImportSavedOwnership(resolved[pair.RiderId], resolved[pair.MountId], pair.OwnerId, pair.Generation, true);
                    // Native OnAreaLoaded completed placement; this hook still
                    // precedes after-entry autosaving for every attachment.
                    var result = owner.RestoreSavedPair(resolved[pair.RiderId], resolved[pair.MountId], pair.ProfileId);
                    if (!result.Succeeded) throw new InvalidOperationException("Area relationship was not eligible: " + string.Join("; ", result.Errors));
                    var participant = unifiedTurn.SnapshotParticipants.Single(p => ReferenceEquals(p.Relationship, owner));
                    participant.RestoreParticipation(pair.Participation, resolved, transfer.TurnBased);
                    owner.CompleteSavedParticipationImport();
                    AreaResumeCount++;
                }
                controls.ResumeAreaControls();
                var restoredActors = new HashSet<string>(restore.SelectMany(p => new[] { p.RiderId, p.MountId }), StringComparer.Ordinal);
                var droppedActors = new HashSet<string>(transfer.Pairs.Where(p => p.Carried && !restore.Contains(p))
                    .SelectMany(p => new[] { p.RiderId, p.MountId }).Where(id => !restoredActors.Contains(id)), StringComparer.Ordinal);
                controls.RestorePersistentSlots(transfer.Slots.Where(s => !droppedActors.Contains(s.ActorId)).ToArray());
                AreaRetiredPairCount += retire.Count;
                RetainedDropCount += carriedDropped.Count;
                if (carriedDropped.Count != 0)
                {
                    Report("Stayed dismounted on arrival: " + string.Join(", ", carriedDropped.ToArray()) + ". Mount again when ready. " +
                        (transfer.Pairs.Length - carriedDropped.Count - retire.Count) + " of " + transfer.Pairs.Length +
                        " mounted relationships restored without Mount or preparation.");
                    return;
                }
                // Report keeps one feedback line, so the dismounted pairs are named in the summary itself.
                Report(retire.Count == 0
                    ? "All " + transfer.Pairs.Length + " mounted relationships restored before area autosave without Mount or preparation."
                    : "Dismounted on arrival because the companion is not directly controllable in this area: " +
                        string.Join(", ", retire.Select(pair => resolved[pair.RiderId].CharacterName + " and " + resolved[pair.MountId].CharacterName).ToArray()) +
                        ". Mount again after leaving. " + restore.Count + " of " + transfer.Pairs.Length +
                        " mounted relationships restored without Mount or preparation.");
            }
            catch (Exception error)
            {
                failedAreaTransfer = transfer;
                areaRestoreFailure = "All-pair area restoration failed: " + error.Message;
                Report(areaRestoreFailure + " Saving and new native participation remain blocked; load a valid save to resume.");
            }
            finally { controls.ResumeAreaControls(); }
        }

        // Narrow to the native capital rule: every other refusal keeps the all-pair failure.
        // The hypothetical re-validation only asks whether anything else would refuse; it grants nothing.
        private static bool IsCapitalControlRetirement(BlueprintArea area, Kingmaker.EntitySystem.Entities.UnitEntityData rider,
            Kingmaker.EntitySystem.Entities.UnitEntityData mount, MountedPairCandidate candidate, string refusal)
        {
            if (area == null || candidate == null) return false;
            var main = Game.Instance?.Player?.MainCharacter.Value;
            bool MainOrMainPet(Kingmaker.EntitySystem.Entities.UnitEntityData unit) =>
                main != null && (unit == main || unit.Descriptor?.Master.Value == main);
            var assumed = KingmakerMountedPairRuntime.CreateCandidate(rider, mount);
            if (assumed == null) return false;
            assumed.RiderIsDirectlyControllable = true; assumed.MountIsDirectlyControllable = true;
            return MountedAreaControlRule.IsCapitalControlRetirement(area.IsCapital, refusal, assumed.ValidateSavedRelationship(),
                candidate.RiderIsDirectlyControllable, MainOrMainPet(rider), candidate.MountIsDirectlyControllable, MainOrMainPet(mount));
        }

        // Observed separation for a retired pair: no active owner, no world relationship holding
        // either unit or any owned residue, settled saved participation, distinct live views at
        // finite positions and no installed agent override. Any residue fails closed.
        private string RetiredAreaPairResidue(AreaPair pair, Kingmaker.EntitySystem.Entities.UnitEntityData rider,
            Kingmaker.EntitySystem.Entities.UnitEntityData mount)
        {
            var p = pair.Participation;
            if (p != null && (p.Activation != null || p.Boundary != null || p.Partner != null || p.PendingSplitId != null))
                return "saved participation still carries obligations";
            if (mountedWorld != null && (mountedWorld.Registry.FindActive(rider) != null || mountedWorld.Registry.FindActive(mount) != null))
                return "a pair owner is still active";
            foreach (var r in WorldRelationships)
                if ((ReferenceEquals(r.Rider, rider) || ReferenceEquals(r.Mount, mount) || ReferenceEquals(r.Rider, mount) || ReferenceEquals(r.Mount, rider)) &&
                    (r.State != RelationshipState.Unmounted || !r.Runtime.NoPreparedPairResidue || r.Runtime.HasPresentationAttachmentResidue))
                    return "a world relationship still holds the pair or owned residue";
            if (rider.View == null || mount.View == null || rider.View.transform.IsChildOf(mount.View.transform) ||
                mount.View.transform.IsChildOf(rider.View.transform))
                return "rider and mount views are not separate";
            if (!Finite(rider.Position) || !Finite(mount.Position)) return "a unit position is not finite";
            if (rider.View.AgentOverride != null || mount.View.AgentOverride != null) return "an agent override remains installed";
            return null;
        }
        private static bool Finite(UnityEngine.Vector3 v) =>
            !float.IsNaN(v.x) && !float.IsInfinity(v.x) && !float.IsNaN(v.y) && !float.IsInfinity(v.y) && !float.IsNaN(v.z) && !float.IsInfinity(v.z);

        private void CompleteAreaTransitionIfReady()
        {
            if (areaTransfer == null) return;
            if (areaTransfer.World.Target != Game.Instance?.Player) { CancelAreaTransition(); return; }
            if (LoadingProcess.Instance.IsLoadingInProcess || LoadingProcess.Instance.QueuedNames.Any() ||
                NativeDeferredSave.Waiting(LoadingProcess.Instance)) return;
            if (!areaTransfer.ResumeAttempted)
            {
                failedAreaTransfer = areaTransfer;
                areaRestoreFailure = "Native area placement did not complete before the mounted restore boundary.";
                Report(areaRestoreFailure);
            }
            CancelAreaTransition();
        }

        private void CancelAreaTransition()
        {
            if (areaTransfer == null) return;
            areaTransfer = null; controls.ResumeAreaControls();
        }

        private sealed class AreaPair
        { internal string RiderId, MountId, ProfileId, OwnerId; internal long Generation; internal SavedPairedState Participation; internal bool Carried; }
        private sealed class AreaTransfer
        {
            internal WeakReference World;
            internal string CampaignId, AreaId;
            internal AreaPair[] Pairs;
            internal SavedMountedSlot[] Slots;
            internal bool Suspended, ResumeAttempted, TurnBased;
            // Exploration repair (issue 7): the destination is the world map, so arrival keeps retained intent.
            internal bool WorldMap;
        }
    }
}
