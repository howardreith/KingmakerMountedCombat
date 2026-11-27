using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
        public HorseTripRepairOffer(long token, string horseId, string horseName, string ownerName, int candidateCount, bool eligible)
        {
            Token = token; HorseId = horseId; HorseName = horseName; OwnerName = ownerName; CandidateCount = candidateCount; Eligible = eligible;
        }
        public long Token { get; }
        public string HorseId { get; }
        public string HorseName { get; }
        public string OwnerName { get; }
        public int CandidateCount { get; }
        // Exactly one candidate on a horse with a stable id: more than one cannot be singled out (the inherited copy is not identifiable).
        public bool Eligible { get; }
        // PR-4 round 14 (R13-F3): why an ineligible row is not offered, from its own stable id and candidate count.
        public string RefusalText => Eligible ? null : string.IsNullOrEmpty(HorseId)
            ? "This horse has no stable identity in the save, so no repair is offered."
            : "More than one copy: the inherited copy cannot be singled out, so no repair is offered.";
    }

    // The classified result of one confirmation, recorded before any logging or display. Disposition is the player-facing text.
    public sealed class HorseTripRepairRecord
    {
        public HorseTripRepairRecord(string horseId, string horseName, string disposition, HorseTripRemovalResult? removal)
        {
            HorseId = horseId; HorseName = horseName; Disposition = disposition; Removal = removal;
        }
        public string HorseId { get; }
        public string HorseName { get; }
        public string Disposition { get; }
        public HorseTripRemovalResult? Removal { get; }
        public override string ToString() => "horse=" + HorseId + "; name=" + HorseName + "; disposition=" + Disposition +
            (Removal.HasValue ? "; outcome=" + Removal.Value.Outcome + "; " + Removal.Value.Detail : string.Empty);

        // PR-4 round 13 (R12-F5): truthful terminal text for every classified outcome.
        public static string DispositionOf(HorseTripRemovalOutcome outcome)
        {
            switch (outcome)
            {
                case HorseTripRemovalOutcome.Removed: return "removed";
                case HorseTripRemovalOutcome.Unchanged: return "not removed: the horse still has the same feature entries, including the Tripping Bite fact";
                case HorseTripRemovalOutcome.NotAttempted: return "not removed: the horse's features could not be read, so nothing was attempted";
                case HorseTripRemovalOutcome.Inconsistent: return "uncertain: the horse's features changed unexpectedly (more or other than the one fact); check the horse before saving";
                case HorseTripRemovalOutcome.Unreadable: return "uncertain: the removal ran but its result could not be verified; check the horse before saving";
                default: return "uncertain: " + outcome + "; check the horse before saving";
            }
        }
    }

    // PR-4 round 13 (R12-F7): the game-facing calls of the repair update, implemented by HorseCompanionBlueprintService in production and by
    // fakes in the tests; both drive the same HorseTripRepairUpdater.Run, so the tests exercise the production orchestration.
    public interface IHorseTripRepairHost<TUnit, TFact> where TUnit : class where TFact : class
    {
        bool PartyAvailable { get; }
        object SessionMarker();
        IEnumerable<HorseTripRepairConsole<TUnit, TFact>.Live> LoadedHorses();
        bool GateAdmits();
        bool Loaded(TUnit unit);
        string IdOf(TUnit unit);
        IList<TFact> CandidatesOf(TUnit unit);
        HorseTripRemovalResult Remove(TUnit unit, TFact fact);
        void Report(HorseTripRepairRecord record);
    }

    // PR-4 round 13: one ordinary update of the repair action. An unavailable party (a world discard or unload) drops every offer and
    // consent. An armed confirmation is executed FIRST (taken before any fallible refresh), then the presentation snapshots are rebuilt only
    // when the UI asked for them, and the stored record is reported last (a reporting failure leaves the record and repeats nothing).
    // PR-4 round 14 (R12-F4, R12-F2): the pending confirmation is taken BEFORE any read; a failed session read is a terminal record naming the
    // confirmed horse; a session change retires every old binding on every update (also while the panel is closed, with no feature scan);
    // the stored record is reported before, and independently of, the presentation refresh, which runs only when the panel asked for it.
    public static class HorseTripRepairUpdater
    {
        public static HorseTripRepairRecord Run<TUnit, TFact>(HorseTripRepairConsole<TUnit, TFact> console, IHorseTripRepairHost<TUnit, TFact> host)
            where TUnit : class where TFact : class
        {
            if (!host.PartyAvailable) { console.Invalidate(); return null; }
            var consent = console.TakeConsent();
            object marker = null;
            Exception markerFailure = null;
            try { marker = host.SessionMarker(); }
            catch (Exception exception) { markerFailure = exception; }
            var record = consent == null ? null : console.ExecuteTaken(consent, marker, markerFailure, host);
            if (markerFailure == null) console.SyncSession(marker);
            if (record != null) host.Report(record);
            if (markerFailure != null) return record;
            if (console.TakePresentationRequest()) console.Observe(marker, host.LoadedHorses());
            return record;
        }
    }

    // PR-4 round 14 (R13-F1): the Unity Mod Manager OnGUI observation. The panel is the authorized repair-only maintenance section only when
    // the COMPLETE callback is exactly that one static handler; any other or additional handler is an options panel.
    public static class UmmPanelPolicy
    {
        public static void Classify(Delegate onGui, MethodInfo authorized, out bool optionsPanel, out bool maintenancePanel)
        {
            var handlers = onGui == null ? new Delegate[0] : onGui.GetInvocationList();
            maintenancePanel = handlers.Length == 1 && authorized != null && handlers[0].Target == null && handlers[0].Method == authorized;
            optionsPanel = handlers.Length > 0 && !maintenancePanel;
        }
    }

    // PR-4 round 12-13: the confirmed maintenance action. UI drawing and dialog callbacks only call RequestPresentation, Select, Confirm
    // and Cancel (intent); HorseTripRepairUpdater.Run calls Observe and Execute in the ordinary update, the only places that read or change
    // game objects. A consent is bound to the live unit OBJECT, its stable id, the exact fact INSTANCE and the load/session epoch; it is
    // consumed once, before anything fallible, every validation failure produces a terminal record naming the confirmed horse, and the
    // record is stored before it is reported. A new session marker, an unavailable party, disabling, unloading or a changed candidate
    // drops the consent; it is never persisted.
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

        private sealed class Consent
        {
            public Binding Binding; public string ConfirmedName;
        }

        private readonly List<Binding> bindings = new List<Binding>();
        private object session;
        private long epoch, nextToken;
        private Binding selected;
        private Consent confirmed;
        private bool presentationRequested, stopped;

        public IList<HorseTripRepairOffer> Offers { get; private set; } = new HorseTripRepairOffer[0];
        public long SelectedToken => selected?.Token ?? -1;
        public bool Armed => confirmed != null;
        public HorseTripRepairRecord LastRecord { get; private set; }
        public int RemovalCount { get; private set; }
        public int ExecutedCount { get; private set; }
        public int ObserveCount { get; private set; }

        // Intent (UI): the panel is being drawn, so the next update rebuilds its snapshots (no feature scans while it is closed).
        public void RequestPresentation() { if (!stopped) presentationRequested = true; }
        public bool TakePresentationRequest() { var requested = presentationRequested; presentationRequested = false; return requested; }

        // Update path: the loaded party horses. A new session marker starts a new epoch (all bindings and consent dropped). A binding is
        // kept only for the same unit object, the same stable id and the same single fact instance; a horse without a stable id is not offered.
        public void Observe(object sessionMarker, IEnumerable<Live> live)
        {
            ObserveCount++;
            SyncSession(sessionMarker);
            if (stopped) { Offers = new HorseTripRepairOffer[0]; return; }
            var next = new List<Binding>();
            var offers = new List<HorseTripRepairOffer>();
            foreach (var horse in live ?? Enumerable.Empty<Live>())
            {
                if (horse.Unit == null) continue;
                var candidates = horse.Candidates ?? new TFact[0];
                if (candidates.Count == 0) continue;
                var fact = candidates.Count == 1 && !string.IsNullOrEmpty(horse.Id) ? candidates[0] : null;
                var binding = bindings.FirstOrDefault(b => ReferenceEquals(b.Unit, horse.Unit) && ReferenceEquals(b.Fact, fact) && b.Epoch == epoch &&
                    string.Equals(b.Id, horse.Id, StringComparison.Ordinal)) ?? new Binding { Token = ++nextToken, Epoch = epoch, Unit = horse.Unit, Id = horse.Id, Fact = fact };
                binding.Name = horse.Name;
                next.Add(binding);
                offers.Add(new HorseTripRepairOffer(binding.Token, horse.Id, horse.Name, horse.Owner, candidates.Count, fact != null));
            }
            bindings.Clear(); bindings.AddRange(next);
            if (selected != null && !bindings.Contains(selected)) selected = null;
            Offers = offers.AsReadOnly();
        }

        // Intent (UI): choose one eligible offer to confirm. Nothing changes in the game.
        public bool Select(long token)
        {
            if (stopped) return false;
            var binding = bindings.FirstOrDefault(b => b.Token == token && b.Fact != null);
            if (binding == null) return false;
            selected = binding; confirmed = null;
            return true;
        }

        // Intent (UI): the explicit confirmation of the selected offer; the name shown at confirmation is the name recorded.
        public bool Confirm(long token)
        {
            if (stopped || selected == null || selected.Token != token) return false;
            confirmed = new Consent { Binding = selected, ConfirmedName = selected.Name };
            return true;
        }

        public void Cancel() { selected = null; confirmed = null; }

        // Disabling, a failure stop, unloading or an unavailable party: every binding and consent is dropped and no reference kept.
        public void Invalidate()
        {
            bindings.Clear(); selected = null; confirmed = null; session = null; epoch++; presentationRequested = false;
            Offers = new HorseTripRepairOffer[0];
        }

        // PR-4 round 14 (R12-F1): the service stopped (successful disable or failure stop): everything is dropped and no intent is accepted
        // until Start (re-enable); a stopped console offers nothing.
        public bool Stopped => stopped;
        public void Stop() { Invalidate(); stopped = true; }
        public void Start() { stopped = false; }

        // PR-4 round 14 (R12-F2): a new session marker retires every binding, selection and offer, without scanning features.
        public void SyncSession(object sessionMarker)
        {
            if (ReferenceEquals(sessionMarker, session)) return;
            session = sessionMarker; epoch++; bindings.Clear(); selected = null; confirmed = null;
            Offers = new HorseTripRepairOffer[0];
        }

        // PR-4 round 14 (R12-F4): the pending confirmation, taken (and so consumed) before any read of the game.
        public object TakeConsent()
        {
            var consent = confirmed;
            confirmed = null; selected = null;
            return consent;
        }

        // Update path: executes the armed consent once. The consent is taken before anything fallible; the session, the world-write gate,
        // the live object, its stable id and its exact single candidate are rechecked; every refusal is a terminal record for that horse.
        public HorseTripRepairRecord Execute(object sessionMarker, IHorseTripRepairHost<TUnit, TFact> host) => ExecuteTaken(TakeConsent(), sessionMarker, null, host);

        public HorseTripRepairRecord ExecuteTaken(object taken, object sessionMarker, Exception sessionFailure, IHorseTripRepairHost<TUnit, TFact> host)
        {
            var consent = taken as Consent;
            if (consent == null) return null;
            ExecutedCount++;
            var b = consent.Binding;
            var name = consent.ConfirmedName;
            HorseTripRepairRecord record = null;
            TFact verified = null;
            try
            {
                IList<TFact> current;
                if (sessionFailure != null)
                    record = new HorseTripRepairRecord(b.Id, name, "not removed: the game session could not be checked (" + sessionFailure.GetType().Name + "); confirm again", null);
                else if (stopped)
                    record = new HorseTripRepairRecord(b.Id, name, "not removed: Mounted Combat was stopped after confirmation; confirm again", null);
                else if (!ReferenceEquals(sessionMarker, session) || b.Epoch != epoch)
                    record = new HorseTripRepairRecord(b.Id, name, "not removed: the game was loaded or the session changed after confirmation; confirm again", null);
                else if (!host.GateAdmits())
                    record = new HorseTripRepairRecord(b.Id, name, "not removed: the world was not settled (saving, loading, an area transition or a restoration); confirm again", null);
                else if (!host.Loaded(b.Unit))
                    record = new HorseTripRepairRecord(b.Id, name, "not removed: the confirmed horse is no longer loaded; confirm again", null);
                else if (string.IsNullOrEmpty(b.Id) || !string.Equals(host.IdOf(b.Unit), b.Id, StringComparison.Ordinal))
                    record = new HorseTripRepairRecord(b.Id, name, "not removed: the horse's stable identity changed after confirmation; confirm again", null);
                else if ((current = host.CandidatesOf(b.Unit) ?? new TFact[0]).Count != 1 || !ReferenceEquals(current[0], b.Fact))
                    record = new HorseTripRepairRecord(b.Id, name, "not removed: the horse's Tripping Bite fact changed after confirmation (" + current.Count + " candidate(s) now); confirm again", null);
                else verified = b.Fact;
            }
            catch (Exception exception)
            {
                record = new HorseTripRepairRecord(b.Id, name, "not removed: the horse could not be checked (" + exception.GetType().Name + "); nothing was attempted", null);
            }
            if (verified != null)
            {
                HorseTripRemovalResult removal;
                try { removal = host.Remove(b.Unit, verified); }
                catch (Exception exception) { removal = new HorseTripRemovalResult(HorseTripRemovalOutcome.Unreadable, exception, "the removal path threw " + exception.GetType().Name); }
                if (removal.Outcome == HorseTripRemovalOutcome.Removed) RemovalCount++;
                record = new HorseTripRepairRecord(b.Id, name, HorseTripRepairRecord.DispositionOf(removal.Outcome), removal);
            }
            bindings.Remove(b);
            LastRecord = record;
            return record;
        }
    }
}
