using System;
using System.Collections.Generic;
using System.Linq;

namespace KingmakerMountedCombat.Domain
{
    // MMR-06 existing horses. A horse created by KMC 0.2.16-alpha.1 or earlier received the stock Mammoth's TrippingBite as a sourceless
    // fact. A sourceless TrippingBite granted any other way (a console command, another mod) looks exactly the same in the save: neither
    // its list position, its neighbours, its GUID nor the missing source proves who granted it. So nothing is removed automatically.
    // PR-4 round 12 (OWNER AMENDMENT revision 1, owner instruction 2026-11-27b section 4): the owner removes it through a deliberate,
    // confirmed in-game maintenance action (HorseTripRepairConsole) for exactly one horse and one fact; the earlier file-request protocol
    // (request/claim/archive/recovery and its platform file machinery) is withdrawn and removed. Old request/result files are never read.
    public static class HorseTripRepairPolicy
    {
        public static bool IsCandidate(bool exactKmcHorse, bool stockIdentity, bool biteTripShape, bool sourceless) =>
            exactKmcHorse && stockIdentity && biteTripShape && sourceless;
    }

    // PR-4 reviews R1-F2/R2-F1: the classified outcome of one native removal of one fact instance.
    public enum HorseTripRemovalOutcome
    {
        // The collection could not be read before the removal: nothing was attempted.
        NotAttempted,
        // Exactly the requested instance is gone and every other captured feature is still present.
        Removed,
        // The collection holds exactly the captured features: nothing changed.
        Unchanged,
        // Anything else (the target and a neighbour gone, or the target present with another change): terminal.
        Inconsistent,
        // The collection could not be read after the removal: terminal.
        Unreadable
    }

    public struct HorseTripRemovalResult
    {
        public HorseTripRemovalResult(HorseTripRemovalOutcome outcome, Exception nativeFailure, string detail)
        {
            Outcome = outcome; NativeFailure = nativeFailure; Detail = detail;
        }

        public HorseTripRemovalOutcome Outcome { get; }
        // The native removal's own exception, if it threw (preserved for the retry path and the log).
        public Exception NativeFailure { get; }
        public string Detail { get; }
    }

    public static class HorseTripRemoval
    {
        // Captures the feature identities, calls the native removal, and classifies the collection afterwards by reference identity,
        // on a normal return and on an exception alike. The caller supplies the read of the live collection (throwing when it is
        // unavailable) and the native removal itself; no check runs inside the native call.
        public static HorseTripRemovalResult RemoveExactly<T>(T target, Func<IEnumerable<T>> readFeatures, Action<T> nativeRemove) where T : class
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            List<T> before;
            try { before = Snapshot(readFeatures()); }
            catch (Exception unreadable) { return new HorseTripRemovalResult(HorseTripRemovalOutcome.NotAttempted, unreadable, "the features could not be read before removal: " + MessageOf(unreadable)); }
            Exception nativeFailure = null;
            try { nativeRemove(target); }
            catch (Exception exception) { nativeFailure = exception; }
            List<T> after;
            try { after = Snapshot(readFeatures()); }
            catch (Exception unreadable)
            {
                return new HorseTripRemovalResult(HorseTripRemovalOutcome.Unreadable, nativeFailure,
                    "the features could not be read after removal: " + MessageOf(unreadable) + Native(nativeFailure));
            }
            return new HorseTripRemovalResult(Classify(before, target, after), nativeFailure,
                "features " + before.Count + "->" + after.Count + Native(nativeFailure));
        }

        // By reference: Removed only when after is exactly before without one occurrence of the target and holds no other copy of it.
        public static HorseTripRemovalOutcome Classify<T>(IList<T> before, T target, IList<T> after) where T : class
        {
            if (SameMultiset(before, after)) return HorseTripRemovalOutcome.Unchanged;
            var expected = new List<T>(before);
            var index = expected.FindIndex(item => ReferenceEquals(item, target));
            if (index < 0) return HorseTripRemovalOutcome.Inconsistent;
            expected.RemoveAt(index);
            return SameMultiset(expected, after) && !after.Any(item => ReferenceEquals(item, target))
                ? HorseTripRemovalOutcome.Removed : HorseTripRemovalOutcome.Inconsistent;
        }

        private static List<T> Snapshot<T>(IEnumerable<T> features) where T : class
        {
            if (features == null) throw new InvalidOperationException("The horse's feature collection is unavailable.");
            return features.ToList();
        }

        private static bool SameMultiset<T>(IList<T> first, IList<T> second) where T : class
        {
            if (first.Count != second.Count) return false;
            var remaining = new List<T>(second);
            foreach (var item in first)
            {
                var index = remaining.FindIndex(other => ReferenceEquals(other, item));
                if (index < 0) return false;
                remaining.RemoveAt(index);
            }
            return true;
        }

        private static string Native(Exception failure) => failure == null ? string.Empty : "; native removal threw " + failure.GetType().Name + ": " + MessageOf(failure);

        // PR-4 review R3-F1 (round 5): an exception's text is diagnostic only; an unreadable message (its getter throws) becomes a safe
        // fallback, so the classified outcome always reaches the policy.
        private static string MessageOf(Exception failure)
        {
            try { return failure.Message; }
            catch (Exception) { return "<message unreadable>"; }
        }
    }

    // PR-4 round 12: one loaded horse as the maintenance surface presents it: an immutable snapshot with an opaque token. Presentation
    // never holds the unit or the fact; the console binds the token to them privately and revalidates them at execution.
    public sealed class HorseTripRepairOffer
    {
        public HorseTripRepairOffer(long token, string horseId, string horseName, string ownerName, int candidateCount)
        {
            Token = token; HorseId = horseId; HorseName = horseName; OwnerName = ownerName; CandidateCount = candidateCount;
        }
        public long Token { get; }
        public string HorseId { get; }
        public string HorseName { get; }
        public string OwnerName { get; }
        public int CandidateCount { get; }
        // Exactly one candidate: more than one cannot be singled out (the inherited copy is not identifiable), so nothing is offered.
        public bool Eligible => CandidateCount == 1;
    }

    // The classified result of one executed (or refused) confirmation, recorded before any logging or display.
    public sealed class HorseTripRepairRecord
    {
        public HorseTripRepairRecord(string horseId, string horseName, string disposition, HorseTripRemovalResult? removal)
        {
            HorseId = horseId; HorseName = horseName; Disposition = disposition; Removal = removal;
        }
        public string HorseId { get; }
        public string HorseName { get; }
        // "removed", "not-removed:<outcome>", "refused:<reason>" or "stale:<reason>".
        public string Disposition { get; }
        public HorseTripRemovalResult? Removal { get; }
        public override string ToString() => "horse=" + HorseId + "; name=" + HorseName + "; disposition=" + Disposition +
            (Removal.HasValue ? "; outcome=" + Removal.Value.Outcome + "; " + Removal.Value.Detail : string.Empty);
    }

    // PR-4 round 12: the confirmed maintenance action. UI drawing and dialog callbacks only call Select/Confirm/Cancel (intent); the
    // service's ordinary update calls Observe and Execute, the only places that read or change game objects. A consent is bound to the
    // live unit OBJECT, its stable id, the exact fact INSTANCE and the load/session epoch; it is consumed once, before anything fallible,
    // and the classified record is stored before the caller logs or displays it, so a reporting failure can never repeat a removal.
    // A new session marker (a load, an area or session replacement), disabling, unloading or a changed candidate drops the consent; it
    // is never persisted.
    public sealed class HorseTripRepairConsole<TUnit, TFact> where TUnit : class where TFact : class
    {
        public struct Live
        {
            public TUnit Unit; public string Id, Name, Owner; public IList<TFact> Candidates;
        }

        private sealed class Binding
        {
            public long Token, Epoch; public TUnit Unit; public string Id, Name; public TFact Fact;
        }

        private readonly List<Binding> bindings = new List<Binding>();
        private object session;
        private long epoch, nextToken;
        private Binding selected, confirmed;

        public IList<HorseTripRepairOffer> Offers { get; private set; } = new HorseTripRepairOffer[0];
        public long SelectedToken => selected?.Token ?? -1;
        public bool Armed => confirmed != null;
        public HorseTripRepairRecord LastRecord { get; private set; }
        public int RemovalCount { get; private set; }
        public int ExecutedCount { get; private set; }

        // Update path: the loaded party horses of this enumeration. A new session marker starts a new epoch (all consent dropped).
        public void Observe(object sessionMarker, IEnumerable<Live> live)
        {
            if (!ReferenceEquals(sessionMarker, session)) { session = sessionMarker; epoch++; bindings.Clear(); selected = confirmed = null; }
            var next = new List<Binding>();
            var offers = new List<HorseTripRepairOffer>();
            foreach (var horse in live ?? Enumerable.Empty<Live>())
            {
                if (horse.Unit == null) continue;
                var candidates = horse.Candidates ?? new TFact[0];
                if (candidates.Count == 0) continue;
                var fact = candidates.Count == 1 ? candidates[0] : null;
                var kept = bindings.FirstOrDefault(b => ReferenceEquals(b.Unit, horse.Unit) && ReferenceEquals(b.Fact, fact) && b.Epoch == epoch);
                var binding = kept ?? new Binding { Token = ++nextToken, Epoch = epoch, Unit = horse.Unit, Id = horse.Id, Name = horse.Name, Fact = fact };
                next.Add(binding);
                offers.Add(new HorseTripRepairOffer(binding.Token, horse.Id, horse.Name, horse.Owner, candidates.Count));
            }
            bindings.Clear(); bindings.AddRange(next);
            if (selected != null && !bindings.Contains(selected)) selected = null;
            if (confirmed != null && !bindings.Contains(confirmed)) confirmed = null;
            Offers = offers.AsReadOnly();
        }

        // Intent (UI): choose one eligible offer to confirm. Nothing changes in the game.
        public bool Select(long token)
        {
            var binding = bindings.FirstOrDefault(b => b.Token == token && b.Fact != null);
            if (binding == null) return false;
            selected = binding; confirmed = null;
            return true;
        }

        // Intent (UI): the explicit confirmation of the selected offer.
        public bool Confirm(long token)
        {
            if (selected == null || selected.Token != token) return false;
            confirmed = selected;
            return true;
        }

        public void Cancel() { selected = null; confirmed = null; }

        // Disabling or unloading: every binding and consent is dropped.
        public void Invalidate()
        {
            bindings.Clear(); selected = confirmed = null; session = null; epoch++;
            Offers = new HorseTripRepairOffer[0];
        }

        // Update path: executes the armed consent once, after rechecking the session, the world-write gate, the live object and its exact
        // single candidate. Returns the record (also LastRecord), or null when nothing was armed.
        public HorseTripRepairRecord Execute(object sessionMarker, bool gateAdmits, Func<TUnit, bool> loaded, Func<TUnit, IList<TFact>> candidatesOf,
            Func<TUnit, TFact, HorseTripRemovalResult> removeExactly)
        {
            var consent = confirmed;
            if (consent == null) return null;
            confirmed = null; selected = null;   // consumed once, before anything fallible
            ExecutedCount++;
            HorseTripRepairRecord record;
            IList<TFact> current = null;
            if (!ReferenceEquals(sessionMarker, session) || consent.Epoch != epoch)
                record = new HorseTripRepairRecord(consent.Id, consent.Name, "stale:the game was loaded or the session changed after confirmation", null);
            else if (!gateAdmits)
                record = new HorseTripRepairRecord(consent.Id, consent.Name, "refused:the world is not settled (saving, loading, an area transition or a restoration)", null);
            else if (!loaded(consent.Unit))
                record = new HorseTripRepairRecord(consent.Id, consent.Name, "stale:the confirmed horse is no longer loaded", null);
            else if ((current = candidatesOf(consent.Unit) ?? new TFact[0]).Count != 1 || !ReferenceEquals(current[0], consent.Fact))
                record = new HorseTripRepairRecord(consent.Id, consent.Name, "stale:the horse's candidate changed after confirmation (" + current.Count + " now)", null);
            else
            {
                var removal = removeExactly(consent.Unit, consent.Fact);
                if (removal.Outcome == HorseTripRemovalOutcome.Removed) RemovalCount++;
                record = new HorseTripRepairRecord(consent.Id, consent.Name,
                    removal.Outcome == HorseTripRemovalOutcome.Removed ? "removed" : "not-removed:" + removal.Outcome, removal);
            }
            bindings.Remove(consent);
            LastRecord = record;
            return record;
        }
    }
}
