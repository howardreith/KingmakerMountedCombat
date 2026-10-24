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
            if (saveInfo != null) { CancelAreaTransition(); return; }
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
            if (!Enabled || world == null || area == null || mounted.Length == 0 || world.IsInCombat ||
                mounted.Any(r => r.Rider.IsInCombat || r.Mount.IsInCombat)) return;
            if (SaveDraining || SaveSuspended || HasSaveAdmissionFence)
            {
                RefusedAreaTransferCount++;
                Report("A mounted save is still owned; pairs are not carried across this area change.");
                return;
            }
            areaTransfer = new AreaTransfer {
                World = new WeakReference(world), CampaignId = world.GameId, AreaId = area.AssetGuidThreadSafe,
                Pairs = mounted.Select(r => new AreaPair { RiderId = r.Rider.UniqueId, MountId = r.Mount.UniqueId,
                    ProfileId = r.Runtime.MountProfileId, OwnerId = r.PairOwner?.Id.ToString("N"),
                    Generation = r.PairOwner?.Generation ?? 0, Participation = unifiedTurn.SnapshotParticipants
                        .Single(p => ReferenceEquals(p.Relationship, r)).CaptureParticipation() }).ToArray(),
                TurnBased = TurnBased.Controllers.CombatController.IsInTurnBasedCombat(), Slots = controls.CapturePersistentSlots()
            };
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
                // Resolve every pair and slot before changing the first attachment.
                var resolved = new Dictionary<string, Kingmaker.EntitySystem.Entities.UnitEntityData>(StringComparer.Ordinal);
                foreach (var id in transfer.Pairs.SelectMany(p => new[] { p.RiderId, p.MountId })
                    .Concat(transfer.Slots.Select(s => s.ActorId)).Distinct(StringComparer.Ordinal))
                {
                    var matches = game.State.Units.Concat(game.Player.AllCharacters).Where(u => u != null && u.UniqueId == id).Distinct().ToArray();
                    if (matches.Length != 1) throw new InvalidOperationException("An area actor did not resolve uniquely: " + id);
                    resolved.Add(id, matches[0]);
                }
                // Admission is all-pair: a destination that cannot directly
                // control the second pair must not attach the first pair. The one
                // exception is the native capital rule (owner decision): that pair is
                // retired, dismounted, after its separation is observed, and the rest restore.
                var area = game.CurrentlyLoadedArea;
                var restore = new List<AreaPair>(); var retire = new List<AreaPair>();
                foreach (var pair in transfer.Pairs)
                {
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
                controls.RestorePersistentSlots(transfer.Slots);
                AreaRetiredPairCount += retire.Count;
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
        { internal string RiderId, MountId, ProfileId, OwnerId; internal long Generation; internal SavedPairedState Participation; }
        private sealed class AreaTransfer
        {
            internal WeakReference World;
            internal string CampaignId, AreaId;
            internal AreaPair[] Pairs;
            internal SavedMountedSlot[] Slots;
            internal bool Suspended, ResumeAttempted, TurnBased;
        }
    }
}
