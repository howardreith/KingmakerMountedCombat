using System;
using System.Collections.Generic;
using System.Linq;

namespace KingmakerMountedCombat.Domain
{
    internal enum MountedPresentationReadinessKind { Waiting, Ready, Incompatible }

    internal sealed class MountedPresentationReadinessResult
    {
        internal MountedPresentationReadinessResult(MountedPresentationReadinessKind kind, string reason)
        {
            if (!Enum.IsDefined(typeof(MountedPresentationReadinessKind), kind) ||
                kind != MountedPresentationReadinessKind.Ready && string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("An exact readiness state and bounded explanation are required.");
            Kind = kind; Reason = reason;
        }
        internal MountedPresentationReadinessKind Kind { get; }
        internal string Reason { get; }
        internal static MountedPresentationReadinessResult Waiting(string reason) =>
            new MountedPresentationReadinessResult(MountedPresentationReadinessKind.Waiting, reason);
        internal static MountedPresentationReadinessResult Ready() =>
            new MountedPresentationReadinessResult(MountedPresentationReadinessKind.Ready, null);
        internal static MountedPresentationReadinessResult Incompatible(string reason) =>
            new MountedPresentationReadinessResult(MountedPresentationReadinessKind.Incompatible, reason);
    }

    // Exact references are retained only for one pending versioned restoration.
    // These immutable pins never authorize resource writes or a native Prepare.
    internal sealed class PresentationActorCustody
    {
        internal PresentationActorCustody(string id, object actor, string debtAndPreparation)
        {
            if (string.IsNullOrWhiteSpace(id) || actor == null || string.IsNullOrWhiteSpace(debtAndPreparation))
                throw new ArgumentException("An exact actor and native expenditure snapshot are required.");
            Id = id; Actor = actor; DebtAndPreparation = debtAndPreparation;
        }
        internal string Id { get; }
        internal object Actor { get; }
        internal string DebtAndPreparation { get; }
        internal bool Matches(PresentationActorCustody value) => value != null && Id == value.Id &&
            ReferenceEquals(Actor, value.Actor) && DebtAndPreparation == value.DebtAndPreparation;
    }

    internal sealed class PresentationOwnerCustody
    {
        internal PresentationOwnerCustody(Guid id, long generation, object owner, object boundary,
            object rider, object mount, string state, string participation)
        {
            if (id == Guid.Empty || generation <= 0 || owner == null || boundary == null ||
                rider == null && mount == null || string.IsNullOrWhiteSpace(state) || participation == null)
                throw new ArgumentException("An exact retained owner is required.");
            Id = id; Generation = generation; Owner = owner; Boundary = boundary; Rider = rider;
            Mount = mount; State = state; Participation = participation;
        }
        internal Guid Id { get; }
        internal long Generation { get; }
        internal object Owner { get; }
        internal object Boundary { get; }
        internal object Rider { get; }
        internal object Mount { get; }
        internal string State { get; }
        internal string Participation { get; }
        internal bool Matches(PresentationOwnerCustody value) => value != null && Id == value.Id &&
            Generation == value.Generation && ReferenceEquals(Owner, value.Owner) &&
            ReferenceEquals(Boundary, value.Boundary) && ReferenceEquals(Rider, value.Rider) &&
            ReferenceEquals(Mount, value.Mount) && State == value.State && Participation == value.Participation;
    }

    internal sealed class PresentationRestoreCustody
    {
        private readonly PresentationActorCustody[] actors;
        private readonly PresentationOwnerCustody[] owners;
        private readonly string[] pairs;
        internal PresentationRestoreCustody(object world, long loadSequence, object archive,
            string sourceFingerprint, string allocationFingerprint, IEnumerable<string> pairIds,
            IEnumerable<PresentationActorCustody> nativeActors, IEnumerable<PresentationOwnerCustody> retainedOwners)
        {
            if (world == null || archive == null || loadSequence <= 0 || string.IsNullOrWhiteSpace(sourceFingerprint) || allocationFingerprint == null)
                throw new ArgumentException("An exact loaded world, sequence and unchanged archive are required.");
            World = world; LoadSequence = loadSequence; Archive = archive; SourceFingerprint = sourceFingerprint;
            AllocationFingerprint = allocationFingerprint;
            pairs = (pairIds ?? throw new ArgumentNullException(nameof(pairIds))).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            actors = (nativeActors ?? throw new ArgumentNullException(nameof(nativeActors))).OrderBy(x => x?.Id, StringComparer.Ordinal).ToArray();
            owners = (retainedOwners ?? throw new ArgumentNullException(nameof(retainedOwners))).OrderBy(x => x?.Id).ToArray();
            if (pairs.Length == 0 || pairs.Length > 6 || pairs.Any(string.IsNullOrWhiteSpace) || pairs.Distinct(StringComparer.Ordinal).Count() != pairs.Length ||
                actors.Length == 0 || actors.Length > 128 || actors.Any(x => x == null) || actors.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count() != actors.Length ||
                owners.Length > 12 || owners.Any(x => x == null) || owners.Select(x => x.Id).Distinct().Count() != owners.Length)
                throw new ArgumentException("Pending presentation custody is missing, duplicated or unbounded.");
        }
        internal object World { get; }
        internal long LoadSequence { get; }
        internal object Archive { get; }
        internal string SourceFingerprint { get; }
        internal string AllocationFingerprint { get; }
        internal IReadOnlyList<string> PairIds => Array.AsReadOnly(pairs);
        internal string Refusal(PresentationRestoreCustody current)
        {
            if (current == null || !ReferenceEquals(World, current.World) || LoadSequence != current.LoadSequence ||
                !ReferenceEquals(Archive, current.Archive) || SourceFingerprint != current.SourceFingerprint)
                return "The exact loaded world, restoration sequence or source archive changed while its rig was pending.";
            if (AllocationFingerprint != current.AllocationFingerprint)
                return "Native actor allocations changed while their saved visual presentation was pending.";
            if (!pairs.SequenceEqual(current.pairs, StringComparer.Ordinal) || actors.Length != current.actors.Length || owners.Length != current.owners.Length)
                return "Saved pair, native actor or retained-owner membership changed while presentation was pending.";
            for (var i = 0; i < actors.Length; i++)
                if (!actors[i].Matches(current.actors[i])) return "An exact native actor, expenditure or Prepared state changed while its rig was pending: " + actors[i].Id;
            for (var i = 0; i < owners.Length; i++)
                if (!owners[i].Matches(current.owners[i])) return "A retained owner, generation, role or participation changed while its rig was pending: " + owners[i].Id;
            return null;
        }
    }

    // Caller supplies read-only native snapshots and observed rig completion.
    // This service owns no callbacks capable of attaching or mutating actors.
    internal sealed class MountedPresentationReadiness
    {
        internal const double MaximumWaitSeconds = 30d;
        private readonly PresentationRestoreCustody expected;
        private readonly double started;
        private string failure;
        internal MountedPresentationReadiness(PresentationRestoreCustody expected, double now)
        {
            if (expected == null || double.IsNaN(now) || double.IsInfinity(now) || now < 0)
                throw new ArgumentException("A bounded native presentation clock and custody are required.");
            this.expected = expected; started = now;
        }
        internal MountedPresentationReadinessResult Check(double now, PresentationRestoreCustody current,
            IReadOnlyDictionary<string, MountedPresentationReadinessResult> rigs)
        {
            if (failure != null) return MountedPresentationReadinessResult.Incompatible(failure);
            failure = expected.Refusal(current);
            if (failure == null && (double.IsNaN(now) || double.IsInfinity(now) || now < started || now - started > MaximumWaitSeconds))
                failure = "Native presentation did not reach its bounded settled rig boundary; saved ownership and expenditure remain retained.";
            if (failure == null && (rigs == null || rigs.Count != expected.PairIds.Count || expected.PairIds.Any(id => !rigs.ContainsKey(id) || rigs[id] == null)))
                failure = "The all-pair presentation preflight omitted or substituted a saved pair.";
            if (failure != null) return MountedPresentationReadinessResult.Incompatible(failure);
            foreach (var id in expected.PairIds)
                if (rigs[id].Kind == MountedPresentationReadinessKind.Incompatible)
                { failure = "Saved pair " + id + " has a finished incompatible rig: " + rigs[id].Reason; return MountedPresentationReadinessResult.Incompatible(failure); }
            foreach (var id in expected.PairIds)
                if (rigs[id].Kind == MountedPresentationReadinessKind.Waiting)
                    return MountedPresentationReadinessResult.Waiting("Saved pair " + id + " waits for native rig completion: " + rigs[id].Reason);
            return MountedPresentationReadinessResult.Ready();
        }
    }
}
