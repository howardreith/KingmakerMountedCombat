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
        internal readonly Guid Id = Guid.NewGuid();
        internal readonly object Session;
        internal readonly string CampaignId, RiderId, MountId;
        internal readonly long Generation;
        internal readonly TActor Rider, Mount;
        internal readonly TBoundary Boundary;
        internal readonly Func<bool> RetainsObligations, SaveReady, TeardownReady;
        internal MountedPairOwnershipState State { get; private set; }

        internal MountedPairOwner(object session, string campaignId, TActor rider, TActor mount,
            string riderId, string mountId, long generation, TBoundary boundary,
            Func<bool> retainsObligations, Func<bool> saveReady, Func<bool> teardownReady)
        {
            Session = session; CampaignId = campaignId; Rider = rider; Mount = mount;
            RiderId = riderId; MountId = mountId; Generation = generation; Boundary = boundary;
            RetainsObligations = retainsObligations; SaveReady = saveReady; TeardownReady = teardownReady;
            State = MountedPairOwnershipState.Reserved;
        }

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
        private object session;
        private string campaignId;

        internal MountedPairRegistry(Func<TActor, string> actorId)
        { this.actorId = actorId ?? throw new ArgumentNullException(nameof(actorId)); }
        internal MountedPairOwner<TActor, TBoundary>[] Owners => owners.ToArray();
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
            foreach (var owner in Owners)
            {
                if (owner.State != MountedPairOwnershipState.Detached || owner.RetainsObligations()) continue;
                if (owner.RiderId != null) actors.Remove(owner.RiderId);
                if (owner.MountId != null) actors.Remove(owner.MountId);
                owners.Remove(owner); owner.Retire();
            }
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
                owners.Count >= MaximumOwners || nativeCapacity == 1 && owners.Count != 0)
                return "Native pair capacity is unavailable until world integration supports another owner.";
            return null;
        }

        internal bool TryReserve(object world, string campaign, TActor rider, TActor mount, long generation,
            TBoundary boundary, Func<bool> retained, Func<bool> saveReady, Func<bool> teardownReady,
            out MountedPairOwner<TActor, TBoundary> owner, out string refusal, int nativeCapacity = MaximumActivePairs)
        {
            owner = null;
            if (boundary == null || retained == null || saveReady == null || teardownReady == null || generation <= 0)
                throw new ArgumentException("Pair ownership requires an exact boundary, generation and readiness predicates.");
            refusal = AdmissionRefusal(world, campaign, rider, mount, nativeCapacity);
            if (refusal != null) return false;
            if (owners.Count == 0) { session = world; campaignId = campaign; }
            owner = new MountedPairOwner<TActor, TBoundary>(world, campaign, rider, mount,
                Key(rider), Key(mount), generation, boundary, retained, saveReady, teardownReady);
            // All checks precede either index mutation. Both roles acquire one owner.
            actors.Add(owner.RiderId, owner); actors.Add(owner.MountId, owner); owners.Add(owner);
            return true;
        }

        // Legacy native saves can retain only a surviving pending-split actor.
        // It remains cleanup/participation custody, never an active membership.
        internal bool TryRetainDetached(object world, string campaign, TActor rider, TActor mount,
            long generation, TBoundary boundary, Func<bool> retained, Func<bool> saveReady, Func<bool> teardownReady,
            out MountedPairOwner<TActor, TBoundary> owner)
        {
            owner = null;
            var riderKey = Key(rider); var mountKey = Key(mount);
            if (world == null || string.IsNullOrWhiteSpace(campaign) || generation <= 0 || boundary == null ||
                retained == null || saveReady == null || teardownReady == null ||
                rider != null && string.IsNullOrWhiteSpace(riderKey) || mount != null && string.IsNullOrWhiteSpace(mountKey) ||
                riderKey == null && mountKey == null || riderKey != null && riderKey == mountKey ||
                owners.Count >= MaximumOwners || owners.Count != 0 && (!ReferenceEquals(world, session) || campaignId != campaign) ||
                riderKey != null && actors.ContainsKey(riderKey) || mountKey != null && actors.ContainsKey(mountKey)) return false;
            if (owners.Count == 0) { session = world; campaignId = campaign; }
            owner = new MountedPairOwner<TActor, TBoundary>(world, campaign, rider, mount, riderKey, mountKey,
                generation, boundary, retained, saveReady, teardownReady);
            owner.Detach();
            if (riderKey != null) actors.Add(riderKey, owner);
            if (mountKey != null) actors.Add(mountKey, owner);
            owners.Add(owner);
            return true;
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
