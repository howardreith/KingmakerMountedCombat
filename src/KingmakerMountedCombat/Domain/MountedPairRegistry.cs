using System;
using System.Collections.Generic;
using System.Linq;

namespace KingmakerMountedCombat.Domain
{
    internal enum MountedPairOwnershipState { Reserved, Active, Detached, Retired }

    // Process-local custody only. Actor allocations and native turns are deliberately
    // absent: their world/session authority outlives a relationship registration.
    internal sealed class MountedPairOwner<TActor, TBoundary> where TActor : class where TBoundary : class
    {
        internal readonly Guid Id;
        internal readonly object Session;
        internal readonly string CampaignId, RiderId, MountId;
        internal readonly long Generation;
        internal readonly TActor Rider, Mount;
        internal readonly TBoundary Boundary;
        internal readonly Func<bool> RetainsObligations, SaveReady, TeardownReady;
        // Known issues sprint 3 (review of fe22563a): what of RetainsObligations holds the pair's world. It differs
        // only by cleanup the owner's boundary keeps on its own, with its exact references (the stick-movement
        // command, view and agent). Unless the boundary declares that split, it is RetainsObligations itself.
        internal Func<bool> RetainsWorldObligations { get; private set; }
        internal MountedPairOwnershipState State { get; private set; }

        internal MountedPairOwner(object session, string campaignId, TActor rider, TActor mount,
            string riderId, string mountId, long generation, TBoundary boundary,
            Func<bool> retainsObligations, Func<bool> saveReady, Func<bool> teardownReady, Guid? importedId = null)
        {
            Id = importedId ?? Guid.NewGuid();
            Session = session; CampaignId = campaignId; Rider = rider; Mount = mount;
            RiderId = riderId; MountId = mountId; Generation = generation; Boundary = boundary;
            RetainsObligations = retainsObligations; SaveReady = saveReady; TeardownReady = teardownReady;
            RetainsWorldObligations = retainsObligations;
            State = MountedPairOwnershipState.Reserved;
        }

        internal void DeclareDetachedCleanup(Func<bool> retainsWorldObligations) { RetainsWorldObligations = retainsWorldObligations; }

        internal void Activate() { State = MountedPairOwnershipState.Active; }
        internal void Detach() { State = MountedPairOwnershipState.Detached; }
        internal void Retire() { State = MountedPairOwnershipState.Retired; }
        internal bool Contains(TActor actor) => ReferenceEquals(Rider, actor) || ReferenceEquals(Mount, actor);
    }

    // A request is captured at native command Init, then carried by the existing
    // command/context weak-table binding. Selection is never a delivery lookup.
    internal sealed class MountedPairRequest<TActor, TBoundary> where TActor : class where TBoundary : class
    {
        internal readonly object Session;
        internal readonly string CampaignId;
        internal readonly TActor Rider, Mount;
        internal readonly long Generation;
        internal readonly TBoundary Boundary;
        internal readonly MountedPairOwner<TActor, TBoundary> Owner;
        internal readonly int Capacity;
        internal MountedPairRequest(object session, string campaignId, TActor rider, TActor mount,
            long generation, TBoundary boundary, MountedPairOwner<TActor, TBoundary> owner, int capacity)
        {
            Session = session; CampaignId = campaignId; Rider = rider; Mount = mount;
            Generation = generation; Boundary = boundary; Owner = owner; Capacity = capacity;
        }
    }

    internal sealed class MountedPairRegistry<TActor, TBoundary> where TActor : class where TBoundary : class
    {
        internal const int MaximumActivePairs = 6;
        internal const int MaximumOwners = 12; // six mounted plus six retained split/cleanup owners
        private readonly Func<TActor, string> actorId;
        private readonly List<MountedPairOwner<TActor, TBoundary>> owners = new List<MountedPairOwner<TActor, TBoundary>>();
        private readonly Dictionary<string, MountedPairOwner<TActor, TBoundary>> actors =
            new Dictionary<string, MountedPairOwner<TActor, TBoundary>>(StringComparer.Ordinal);
        // Owners of a replaced world that owe nothing but detached cleanup. They are out of the current session's
        // roster, actor index and identities; they keep their boundary and retire when their cleanup is established.
        private readonly List<MountedPairOwner<TActor, TBoundary>> earlierWorldCleanup = new List<MountedPairOwner<TActor, TBoundary>>();
        private object session;
        private string campaignId;

        internal MountedPairRegistry(Func<TActor, string> actorId)
        { this.actorId = actorId ?? throw new ArgumentNullException(nameof(actorId)); }
        internal MountedPairOwner<TActor, TBoundary>[] Owners => owners.ToArray();
        internal MountedPairOwner<TActor, TBoundary>[] EarlierWorldCleanupOwners => earlierWorldCleanup.ToArray();
        internal int ActiveCount => owners.Count(o => o.State == MountedPairOwnershipState.Active ||
            o.State == MountedPairOwnershipState.Reserved);

        private string Key(TActor actor)
        {
            var value = actor == null ? null : actorId(actor);
            return Guid.TryParse(value, out var id) ? id.ToString("N") : value;
        }

        internal MountedPairOwner<TActor, TBoundary> FindOwner(TActor actor)
        {
            var key = Key(actor);
            return key != null && actors.TryGetValue(key, out var owner) && owner.Contains(actor) ? owner : null;
        }

        internal MountedPairOwner<TActor, TBoundary> FindActive(TActor actor)
        {
            var owner = FindOwner(actor);
            return owner?.State == MountedPairOwnershipState.Active ? owner : null;
        }

        internal bool Owns(MountedPairOwner<TActor, TBoundary> owner) => owner != null &&
            ReferenceEquals(session, owner.Session) && campaignId == owner.CampaignId && owners.Contains(owner);

        internal bool IsActiveOwner(MountedPairOwner<TActor, TBoundary> owner, TActor rider, TActor mount) =>
            Owns(owner) && owner.State == MountedPairOwnershipState.Active &&
            ReferenceEquals(owner.Rider, rider) && ReferenceEquals(owner.Mount, mount) &&
            ReferenceEquals(FindActive(rider), owner) && ReferenceEquals(FindActive(mount), owner);

        internal void RetireSettled()
        {
            // Update checks retirement every frame. Reserved and active owners
            // cannot settle here, so avoid a roster copy, callback and error
            // array until there is actual detached custody to examine.
            var detached = earlierWorldCleanup.Count != 0;
            for (var index = 0; !detached && index < owners.Count; index++)
                if (owners[index].State == MountedPairOwnershipState.Detached)
                { detached = true; break; }
            if (!detached) return;
            var errors = VisitOwners(owner =>
            {
                if (owner.State != MountedPairOwnershipState.Detached || owner.RetainsObligations()) return;
                if (owner.RiderId != null) actors.Remove(owner.RiderId);
                if (owner.MountId != null) actors.Remove(owner.MountId);
                owners.Remove(owner); owner.Retire();
            }).ToList();
            foreach (var owner in earlierWorldCleanup.ToArray())
                try
                {
                    if (owner.RetainsObligations()) continue;
                    earlierWorldCleanup.Remove(owner); owner.Retire();
                }
                catch (Exception error) { errors.Add(error); }
            if (errors.Count != 0)
                throw new AggregateException("Some mounted owners could not establish settled custody.", errors);
        }

        // Known issues sprint 3 (review of fe22563a). A detached owner whose only remaining obligation is cleanup its
        // boundary keeps on its own: the pair's world no longer depends on it.
        internal bool IsDetachedCleanupOnly(MountedPairOwner<TActor, TBoundary> owner) =>
            owner != null && owner.State == MountedPairOwnershipState.Detached && owner.RetainsObligations() &&
            !owner.RetainsWorldObligations();

        internal void DeclareDetachedCleanup(MountedPairOwner<TActor, TBoundary> owner, Func<bool> retainsWorldObligations)
        {
            if (!Owns(owner) || retainsWorldObligations == null)
                throw new InvalidOperationException("Only the exact owner can declare its detached cleanup.");
            owner.DeclareDetachedCleanup(retainsWorldObligations);
        }

        // Admission bindings of a world may be discarded when every owner is retired, or detached and holding nothing
        // of that world. Detached cleanup alone does not hold it; everything else does, as before.
        internal bool SettledForAdmissionDiscard() => owners.All(owner => owner.State == MountedPairOwnershipState.Retired ||
            owner.State == MountedPairOwnershipState.Detached && !owner.RetainsWorldObligations());

        // The pair is still held by its earlier owner, which owes only detached cleanup: its actors or its exact owner
        // identity. The pair stays dismounted (or deferred) until that owner retires; it is never admitted beside it.
        internal bool HeldForDetachedCleanup(TActor rider, TActor mount, Guid? id)
        {
            var riderKey = Key(rider); var mountKey = Key(mount);
            foreach (var owner in owners)
            {
                if (!IsDetachedCleanupOnly(owner)) continue;
                if (id.HasValue && owner.Id == id.Value) return true;
                foreach (var key in new[] { riderKey, mountKey })
                    if (key != null && (key == owner.RiderId || key == owner.MountId)) return true;
            }
            return false;
        }

        // Known issues sprint 3 (review of 119088ed): the exact earlier owner of this pair that owes only detached
        // cleanup: the same identity and generation, the same rider and mount. Null when there is none.
        internal MountedPairOwner<TActor, TBoundary> FindHeldOwner(TActor rider, TActor mount, Guid id, long generation)
        {
            var riderKey = Key(rider); var mountKey = Key(mount);
            foreach (var owner in owners)
                if (IsDetachedCleanupOnly(owner) && owner.Id == id && owner.Generation == generation &&
                    riderKey != null && riderKey == owner.RiderId && mountKey != null && mountKey == owner.MountId) return owner;
            return null;
        }

        // World replacement (a load). The old world's custody must have settled as for an admission discard. Owners
        // that still owe detached cleanup leave this session (roster, actor index, identities) with that obligation
        // and their boundary, and retire once it is established. A new world session can then begin.
        internal int SetAsideEarlierWorldCleanup()
        {
            if (!SettledForAdmissionDiscard())
                throw new InvalidOperationException("Old-world custody has not settled before world replacement.");
            RetireSettled();
            var moved = 0;
            foreach (var owner in owners.ToArray())
            {
                if (!IsDetachedCleanupOnly(owner)) continue;
                if (owner.RiderId != null && actors.TryGetValue(owner.RiderId, out var riderOwner) && ReferenceEquals(riderOwner, owner))
                    actors.Remove(owner.RiderId);
                if (owner.MountId != null && actors.TryGetValue(owner.MountId, out var mountOwner) && ReferenceEquals(mountOwner, owner))
                    actors.Remove(owner.MountId);
                owners.Remove(owner);
                earlierWorldCleanup.Add(owner);
                moved++;
            }
            // With the roster empty, the next admission begins the new world's session (TryReserve).
            return moved;
        }

        internal string AdmissionRefusal(object world, string campaign, TActor rider, TActor mount,
            int nativeCapacity = MaximumActivePairs)
        {
            if (world == null || string.IsNullOrWhiteSpace(campaign) || rider == null || mount == null ||
                string.IsNullOrWhiteSpace(Key(rider)) || string.IsNullOrWhiteSpace(Key(mount)) || Key(rider) == Key(mount))
                return "An exact session and two distinct actors are required.";
            if (owners.Count != 0 && (!ReferenceEquals(session, world) || campaignId != campaign))
                return "Previous world ownership has not settled.";
            if (actors.ContainsKey(Key(rider)) || actors.ContainsKey(Key(mount)))
                return "An actor already has reserved, mounted or unfinished pair ownership.";
            if (nativeCapacity < 1 || nativeCapacity > MaximumActivePairs || ActiveCount >= nativeCapacity ||
                owners.Count + earlierWorldCleanup.Count >= MaximumOwners || nativeCapacity == 1 && owners.Count != 0)
                return "Native pair capacity is unavailable until world integration supports another owner.";
            return null;
        }

        internal bool TryReserve(object world, string campaign, TActor rider, TActor mount, long generation,
            TBoundary boundary, Func<bool> retained, Func<bool> saveReady, Func<bool> teardownReady,
            out MountedPairOwner<TActor, TBoundary> owner, out string refusal, int nativeCapacity = MaximumActivePairs,
            Guid? importedId = null)
        {
            owner = null;
            if (boundary == null || retained == null || saveReady == null || teardownReady == null || generation <= 0)
                throw new ArgumentException("Pair ownership requires an exact boundary, generation and readiness predicates.");
            refusal = importedId.HasValue && (importedId.Value == Guid.Empty || owners.Any(o => o.Id == importedId.Value))
                ? "A persisted owner identity is invalid or already imported."
                : AdmissionRefusal(world, campaign, rider, mount, nativeCapacity);
            if (refusal != null) return false;
            if (owners.Count == 0) { session = world; campaignId = campaign; }
            owner = new MountedPairOwner<TActor, TBoundary>(world, campaign, rider, mount,
                Key(rider), Key(mount), generation, boundary, retained, saveReady, teardownReady, importedId);
            // All checks precede either index mutation. Both roles acquire one owner.
            actors.Add(owner.RiderId, owner); actors.Add(owner.MountId, owner); owners.Add(owner);
            return true;
        }

        // Legacy native saves can retain only a surviving pending-split actor.
        // It remains cleanup/participation custody, never an active membership.
        internal bool TryRetainDetached(object world, string campaign, TActor rider, TActor mount,
            long generation, TBoundary boundary, Func<bool> retained, Func<bool> saveReady, Func<bool> teardownReady,
            out MountedPairOwner<TActor, TBoundary> owner, Guid? importedId = null)
        {
            owner = null;
            var riderKey = Key(rider); var mountKey = Key(mount);
            if (world == null || string.IsNullOrWhiteSpace(campaign) || generation <= 0 || boundary == null ||
                retained == null || saveReady == null || teardownReady == null ||
                importedId.HasValue && (importedId.Value == Guid.Empty || owners.Any(o => o.Id == importedId.Value)) ||
                rider != null && string.IsNullOrWhiteSpace(riderKey) || mount != null && string.IsNullOrWhiteSpace(mountKey) ||
                riderKey == null && mountKey == null || riderKey != null && riderKey == mountKey ||
                owners.Count + earlierWorldCleanup.Count >= MaximumOwners || owners.Count != 0 && (!ReferenceEquals(world, session) || campaignId != campaign) ||
                riderKey != null && actors.ContainsKey(riderKey) || mountKey != null && actors.ContainsKey(mountKey)) return false;
            if (owners.Count == 0) { session = world; campaignId = campaign; }
            owner = new MountedPairOwner<TActor, TBoundary>(world, campaign, rider, mount, riderKey, mountKey,
                generation, boundary, retained, saveReady, teardownReady, importedId);
            owner.Detach();
            if (riderKey != null) actors.Add(riderKey, owner);
            if (mountKey != null) actors.Add(mountKey, owner);
            owners.Add(owner);
            return true;
        }

        // Persistence imports custody only, after whole-graph validation and
        // exact actor resolution. It neither creates a native grant nor activates
        // an attachment; mounted imports remain reserved until restore commits.
        internal bool TryImport(object world, string campaign, Guid id, long generation,
            TActor rider, TActor mount, TBoundary boundary, bool mounted, Func<bool> retained,
            Func<bool> saveReady, Func<bool> teardownReady, out MountedPairOwner<TActor, TBoundary> owner,
            out string refusal)
        {
            owner = null; refusal = null;
            if (id == Guid.Empty || generation <= 0 || owners.Any(o => o.Id == id))
            { refusal = "Persisted ownership identity or generation is invalid or duplicated."; return false; }
            if (mounted)
                return TryReserve(world, campaign, rider, mount, generation, boundary, retained, saveReady,
                    teardownReady, out owner, out refusal, MaximumActivePairs, id);
            if (TryRetainDetached(world, campaign, rider, mount, generation, boundary, retained, saveReady,
                teardownReady, out owner, id)) return true;
            refusal = "Persisted detached ownership conflicts with the resolved world.";
            return false;
        }

        internal void Commit(MountedPairOwner<TActor, TBoundary> owner)
        {
            if (!Owns(owner) || owner.State != MountedPairOwnershipState.Reserved)
                throw new InvalidOperationException("Only the exact reservation can commit.");
            owner.Activate();
        }

        internal void Detach(MountedPairOwner<TActor, TBoundary> owner)
        {
            if (!Owns(owner)) throw new InvalidOperationException("Cleanup must retain its exact owner.");
            owner.Detach();
            // Release is explicit, after every cleanup postcondition has been observed.
        }

        internal bool AllSaveReady() => Owners.All(o => o.SaveReady());
        internal bool AllTeardownReady() => Owners.All(o => o.TeardownReady());

        // Each release is attempted even if a different owner throws. A failure cannot
        // remove its registration or skip another owner's independently releasable lease.
        internal Exception[] VisitOwners(Action<MountedPairOwner<TActor, TBoundary>> action)
        {
            var errors = new List<Exception>();
            foreach (var owner in Owners)
                try { action(owner); } catch (Exception error) { errors.Add(error); }
            return errors.ToArray();
        }

        internal MountedPairRequest<TActor, TBoundary> CaptureRequest(object world, string campaign,
            TActor rider, TActor mount, long generation, TBoundary boundary, bool mounted, int capacity = MaximumActivePairs)
        {
            if (world == null || boundary == null || rider == null || mount == null) return null;
            var owner = FindActive(rider);
            if (mounted ? !IsActiveOwner(owner, rider, mount) ||
                !ReferenceEquals(owner.Boundary, boundary) || owner.Generation != generation :
                AdmissionRefusal(world, campaign, rider, mount, capacity) != null) return null;
            if (mounted && (!ReferenceEquals(owner.Session, world) || owner.CampaignId != campaign)) return null;
            return new MountedPairRequest<TActor, TBoundary>(world, campaign, rider, mount, generation, boundary, owner, capacity);
        }

        internal bool TryResolve(MountedPairRequest<TActor, TBoundary> request, object world, string campaign,
            long generation, out TBoundary boundary)
        {
            boundary = null;
            if (request == null || !ReferenceEquals(request.Session, world) || request.CampaignId != campaign ||
                request.Generation != generation) return false;
            if (request.Owner != null)
            {
                if (!IsActiveOwner(request.Owner, request.Rider, request.Mount)) return false;
            }
            else if (AdmissionRefusal(world, campaign, request.Rider, request.Mount, request.Capacity) != null) return false;
            boundary = request.Boundary;
            return true;
        }
    }
}
