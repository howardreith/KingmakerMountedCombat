using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // MMR-06 existing horses, PR-4 rounds 12-13 (OWNER AMENDMENT revision 1, owner instruction 2026-11-27b section 4): nothing is removed
    // automatically; one fact is removed from one horse only through the owner's confirmed in-game maintenance action. Every case runs the
    // PRODUCTION orchestration HorseTripRepairUpdater.Run (the method HorseCompanionBlueprintService.ProcessTripRepairConsole calls) with the
    // production HorseTripRepairConsole and HorseTripRemoval; only the game-facing host calls (party, session marker, world-write gate inputs,
    // feature collection, native removal, logger) are substituted (a simulated engine boundary). The composition root's lifecycle wiring
    // (disable and failure stop -> InvalidateTripRepair) needs the game and is checked in Test-MmrHorseTripNativeContract.ps1. UI appearance and
    // gameplay save/load are pending the owner's integrated-alpha playtest. Retired file-protocol tests: PR-4-amendment-test-mapping.md.
    internal static class HorseTripRepairPolicyTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("MMR-06: a candidate needs the exact Horse, the stock identity and shape, and no provenance", CandidatePredicate);
            runner.Run("PR-4 round 13: discovery presents stable snapshots only while the panel is drawn; nothing is removed without a confirmation", DiscoveryIsReadOnly);
            runner.Run("PR-4 round 13: two horses: selecting, switching and cancelling change nothing; the confirmed horse alone is repaired once", TwoHorsesAndCancellation);
            runner.Run("PR-4 round 13: repeated clicks and updates consume one confirmation once (one removal)", RepeatedClicksAndUpdates);
            runner.Run("PR-4 round 13: a reload (same stable id, new unit object, new session) or a new visit to the same area invalidates the consent and token", ReloadInvalidatesConsent);
            runner.Run("PR-4 round 13 (R12-F1): disable/re-enable and a failure stop drop the consent; a fresh confirmation repairs once", DisableAndFailureStop);
            runner.Run("PR-4 round 13 (R12-F2): an unavailable party drops offers and consent; re-entry offers a fresh token", PartyUnavailable);
            runner.Run("PR-4 round 13 (R12-F3): the stable id is required and rechecked; a rename records the confirmed name", StableIdAndRename);
            runner.Run("PR-4 round 13 (R12-F4): a changed fact, a second candidate or a failing check after confirmation is a terminal record for that horse", RefreshCannotEraseConsent);
            runner.Run("PR-4 round 13: saving, loading or an unsettled world refuse (no removal; confirm again)", GatesRefuse);
            runner.Run("PR-4 round 13: sourced, modified and ambiguous facts are never offered or removed", SourcedModifiedAmbiguous);
            runner.Run("PR-4 round 13 (R12-F5): removal outcomes are recorded truthfully: exact, verified unchanged, uncertain (neighbour, unreadable result, unreadable message)", RemovalOutcomes);
            runner.Run("PR-4 round 13: a reporting failure after the outcome never repeats the removal", ReportingFailureDoesNotRepeat);
            runner.Run("PR-4 round 13: old request and result files cause no mutation and stay byte-identical", OldRequestFilesAreInert);
            runner.Run("PR-4 round 13: protected neighbours (bite, hooves, natural armor, trip defense, other companions) stay", NeighboursStay);
        }

        private sealed class Fact
        {
            public string Name;
            public bool StockIdentity, BiteTripShape, Sourceless;
        }

        private sealed class Horse
        {
            public string Id, Name;
            public bool ExactKmcHorse = true;
            public readonly List<Fact> Facts = new List<Fact>();
        }

        private static Fact StockGrant(string name = "TrippingBite") =>
            new Fact { Name = name, StockIdentity = true, BiteTripShape = true, Sourceless = true };

        private static Horse LegacyHorse(string id)
        {
            var horse = new Horse { Id = id, Name = "Horse " + id };
            horse.Facts.Add(StockGrant());
            foreach (var name in new[] { "NaturalArmor7", "TripDefenseFourLegs", "Bite1d4", "Hoof1d4", "Hoof1d4" }) horse.Facts.Add(new Fact { Name = name });
            return horse;
        }

        private static List<Fact> Candidates(Horse horse) => horse.Facts.Where(fact =>
            HorseTripRepairPolicy.IsCandidate(horse.ExactKmcHorse, fact.StockIdentity, fact.BiteTripShape, fact.Sourceless)).ToList();

        private sealed class UnreadableMessageException : Exception { public override string Message => throw new InvalidOperationException("message getter failed"); }

        // The substituted game boundary. The world-write gate is the production HorseInheritedFactRepairPolicy.MayRepair rule over the same
        // inputs the service reads (service live, no load in progress, the composition root's write gate open).
        private sealed class Host : IHorseTripRepairHost<Horse, Fact>
        {
            public readonly HorseTripRepairConsole<Horse, Fact> Console = new HorseTripRepairConsole<Horse, Fact>();
            public object Marker = new object();
            public bool Party = true, ServiceLive = true, Loading, WriteGateOpen = true;
            public readonly List<Horse> Loaded = new List<Horse>();
            public readonly Dictionary<Horse, string> IdOverride = new Dictionary<Horse, string>();
            public Action<Horse, Fact> NativeRemove = (h, f) => h.Facts.Remove(f);
            public Func<Horse, IEnumerable<Fact>> ReadAfter;   // the post-removal read (null: the live list)
            public Exception CheckFailure, DiscoveryFailure;
            public int NativeCalls, Reads;
            public bool LogThrows;
            public readonly List<string> Log = new List<string>();

            public bool PartyAvailable => Party;
            public object SessionMarker() => Marker;
            public IEnumerable<HorseTripRepairConsole<Horse, Fact>.Live> LoadedHorses()
            {
                if (DiscoveryFailure != null) throw DiscoveryFailure;
                return Loaded.Select(h => new HorseTripRepairConsole<Horse, Fact>.Live { Unit = h, Id = IdOf(h), Name = h.Name, Owner = "owner of " + h.Id, Candidates = Candidates(h) }).ToList();
            }
            public bool GateAdmits() => HorseInheritedFactRepairPolicy.MayRepair(ServiceLive, !Loading, !WriteGateOpen);
            bool IHorseTripRepairHost<Horse, Fact>.Loaded(Horse unit) => Loaded.Contains(unit);
            public string IdOf(Horse h) { string id; return IdOverride.TryGetValue(h, out id) ? id : h.Id; }
            public IList<Fact> CandidatesOf(Horse h) { if (CheckFailure != null) throw CheckFailure; return Candidates(h); }
            public HorseTripRemovalResult Remove(Horse h, Fact f) => HorseTripRemoval.RemoveExactly(f, () =>
            {
                Reads++;
                return Reads > 1 && ReadAfter != null ? ReadAfter(h) : h.Facts.ToList();
            }, x => { NativeCalls++; NativeRemove(h, x); });
            public void Report(HorseTripRepairRecord record) { if (LogThrows) throw new IOException("simulated logger failure"); Log.Add(record.ToString()); }

            // One ordinary update with the panel drawn (the UI's RequestPresentation, intent only).
            public HorseTripRepairRecord Update(bool panelOpen = true)
            {
                if (panelOpen) Console.RequestPresentation();
                return HorseTripRepairUpdater.Run(Console, this);
            }
            public HorseTripRepairOffer Offer(Horse horse) => Console.Offers.Single(o => o.HorseId == IdOf(horse));
            public void ConfirmOffer(Horse horse) { var t = Offer(horse).Token; Console.Select(t); Console.Confirm(t); }
        }

        private static void CandidatePredicate()
        {
            TestRunner.True(HorseTripRepairPolicy.IsCandidate(true, true, true, true), "The exact sourceless stock grant on the Horse was not a candidate.");
            TestRunner.True(!HorseTripRepairPolicy.IsCandidate(true, true, true, false), "A grant with a source, item or cutscene was a candidate.");
            TestRunner.True(!HorseTripRepairPolicy.IsCandidate(true, false, true, true), "A different GUID with the same shape was a candidate.");
            TestRunner.True(!HorseTripRepairPolicy.IsCandidate(true, true, false, true), "The stock GUID with a different maneuver or category was a candidate.");
            TestRunner.True(!HorseTripRepairPolicy.IsCandidate(false, true, true, true), "A dog, wolf or other unit's TrippingBite was a candidate.");
        }

        private static void DiscoveryIsReadOnly()
        {
            var host = new Host(); var a = LegacyHorse("horse-A"); host.Loaded.Add(a);
            var clean = new Horse { Id = "horse-clean", Name = "Clean" }; clean.Facts.Add(new Fact { Name = "Bite1d4" }); host.Loaded.Add(clean);
            for (var i = 0; i < 3; i++) host.Update(false);
            var closedScans = host.Console.ObserveCount;
            host.Update();
            var offer = host.Offer(a);
            TestRunner.True(closedScans == 0 && host.Console.ObserveCount == 1 && host.Console.Offers.Count == 1 && offer.Eligible && offer.HorseName == "Horse horse-A" &&
                offer.OwnerName == "owner of horse-A" && host.NativeCalls == 0 && Candidates(a).Count == 1 && host.Console.LastRecord == null && host.Console.Offers.IsReadOnly,
                "Discovery scanned while the panel was closed, removed something, offered a clean horse or exposed a mutable list.");
        }

        private static void TwoHorsesAndCancellation()
        {
            var host = new Host(); var a = LegacyHorse("horse-A"); var b = LegacyHorse("horse-B"); host.Loaded.Add(a); host.Loaded.Add(b);
            host.Update();
            var tokenA = host.Offer(a).Token; var tokenB = host.Offer(b).Token;
            host.Console.Select(tokenA); host.Update();
            host.Console.Select(tokenB); host.Console.Cancel(); host.Update();
            var afterCancel = host.NativeCalls;
            var wrongConfirm = host.Console.Confirm(tokenA);
            host.Console.Select(tokenB);
            var confirmedB = host.Console.Confirm(tokenB);
            var record = host.Update();
            TestRunner.True(afterCancel == 0 && !wrongConfirm && confirmedB && record != null && record.Disposition == "removed" && record.HorseId == "horse-B" &&
                Candidates(a).Count == 1 && Candidates(b).Count == 0 && host.Console.RemovalCount == 1,
                "Selection, switching or cancellation changed a horse, or the confirmed horse was not the only one repaired.");
        }

        private static void RepeatedClicksAndUpdates()
        {
            var host = new Host(); var a = LegacyHorse("horse-A"); host.Loaded.Add(a);
            host.Update();
            var token = host.Offer(a).Token;
            host.Console.Select(token); host.Console.Confirm(token); host.Console.Confirm(token); host.Console.Select(token); host.Console.Confirm(token);
            var first = host.Update(); var second = host.Update(); var third = host.Update();
            TestRunner.True(first != null && first.Disposition == "removed" && second == null && third == null && !host.Console.Confirm(token) && host.NativeCalls == 1 &&
                host.Console.RemovalCount == 1 && host.Console.ExecutedCount == 1 && host.Console.Offers.Count == 0,
                "Repeated clicks or updates repeated the removal or left a consent armed.");
        }

        private static void ReloadInvalidatesConsent()
        {
            foreach (var kase in new[] { "reload (new unit object, same id)", "new visit to the same area (same carried unit object)" })
            {
                var host = new Host(); var before = LegacyHorse("horse-A"); host.Loaded.Add(before);
                host.Update();
                var oldToken = host.Offer(before).Token;
                host.ConfirmOffer(before);
                host.Marker = new object();   // a new Player object or a new loaded area state
                var after = before;
                if (kase.StartsWith("reload")) { after = LegacyHorse("horse-A"); host.Loaded.Clear(); host.Loaded.Add(after); }
                var record = host.Update();
                var oldTokenWorks = host.Console.Select(oldToken);
                var fresh = host.Offer(after);
                host.ConfirmOffer(after);
                var applied = host.Update();
                TestRunner.True(record != null && record.Disposition.StartsWith("not removed: the game was loaded", StringComparison.Ordinal) && record.HorseId == "horse-A" &&
                    !oldTokenWorks && fresh.Token != oldToken && applied != null && applied.Disposition == "removed" && host.NativeCalls == 1 &&
                    (kase.StartsWith("reload") ? Candidates(before).Count == 1 : true),
                    "PR-4 round 13 (" + kase + "): the earlier consent or token survived, or the fresh confirmation did not repair once.");
            }
        }

        private static void DisableAndFailureStop()
        {
            foreach (var kase in new[] { "disable and re-enable (CompositionRoot.SetEnabled)", "failure stop (PerformFailureCleanup)" })
            {
                var host = new Host(); var a = LegacyHorse("horse-A"); host.Loaded.Add(a);
                host.Update();
                var oldToken = host.Offer(a).Token;
                host.ConfirmOffer(a);
                host.Console.Invalidate();   // what CompositionRoot calls through HorseCompanionBlueprintService.InvalidateTripRepair on both paths
                var noUpdateRecord = host.Update();
                var oldTokenWorks = host.Console.Select(oldToken);
                host.ConfirmOffer(a);
                var applied = host.Update();
                TestRunner.True(noUpdateRecord == null && !oldTokenWorks && applied != null && applied.Disposition == "removed" && host.NativeCalls == 1,
                    "PR-4 round 13 (" + kase + "): a consent survived the stop, or a fresh confirmation did not repair once.");
            }
        }

        private static void PartyUnavailable()
        {
            var host = new Host(); var a = LegacyHorse("horse-A"); host.Loaded.Add(a);
            host.Update();
            var oldToken = host.Offer(a).Token;
            host.ConfirmOffer(a);
            host.Party = false;
            var during = host.Update();
            var offersWhileGone = host.Console.Offers.Count;
            host.Party = true;   // re-entry with the same carried horse and the same area blueprint
            host.Update();
            var oldTokenWorks = host.Console.Select(oldToken);
            var fresh = host.Offer(a);
            TestRunner.True(during == null && offersWhileGone == 0 && !host.Console.Armed && !oldTokenWorks && fresh.Token != oldToken && fresh.Eligible && host.NativeCalls == 0,
                "An unavailable party kept offers or consent, or re-entry reused the old token.");
        }

        private static void StableIdAndRename()
        {
            foreach (var kase in new[] { "id changed", "id unavailable" })
            {
                var host = new Host(); var a = LegacyHorse("horse-A"); host.Loaded.Add(a);
                host.Update();
                host.ConfirmOffer(a);
                host.IdOverride[a] = kase == "id changed" ? "horse-other" : null;
                var record = host.Update();
                TestRunner.True(record != null && record.Disposition.StartsWith("not removed: the horse's stable identity changed", StringComparison.Ordinal) && host.NativeCalls == 0 && Candidates(a).Count == 1,
                    "PR-4 round 13 (" + kase + "): a consent authorized a removal after the stable id changed.");
                var unavailable = host.Console.Offers.FirstOrDefault(o => o.HorseId == null);
                if (kase == "id unavailable") TestRunner.True(unavailable != null && !unavailable.Eligible && !host.Console.Select(unavailable.Token), "A horse without a stable id was offered.");
            }
            {
                var host = new Host(); var a = LegacyHorse("horse-A"); host.Loaded.Add(a);
                host.Update();
                host.ConfirmOffer(a);
                a.Name = "Renamed Horse";   // an ordinary companion rename after confirmation, same id
                var record = host.Update();
                TestRunner.True(record != null && record.Disposition == "removed" && record.HorseName == "Horse horse-A" && record.HorseId == "horse-A",
                    "A rename after confirmation changed the horse identified in the result.");
            }
        }

        private static void RefreshCannotEraseConsent()
        {
            foreach (var kase in new[] { "fact replaced", "second candidate added", "eligibility read throws", "discovery throws" })
            {
                var host = new Host(); var a = LegacyHorse("horse-A"); var b = LegacyHorse("horse-B"); host.Loaded.Add(a); host.Loaded.Add(b);
                host.Update();
                host.ConfirmOffer(b); host.Update();   // an earlier result for B
                host.ConfirmOffer(a);
                if (kase == "fact replaced") a.Facts[0] = StockGrant();
                if (kase == "second candidate added") a.Facts.Add(StockGrant("TrippingBite (second)"));
                if (kase == "eligibility read throws") host.CheckFailure = new InvalidOperationException("feature collection unavailable");
                if (kase == "discovery throws") host.DiscoveryFailure = new InvalidOperationException("party enumeration failed");
                HorseTripRepairRecord record = null; var threw = false;
                try { record = host.Update(); } catch (InvalidOperationException) { threw = true; }
                var stored = host.Console.LastRecord;
                host.CheckFailure = null; host.DiscoveryFailure = null;
                var later = host.Update();
                // The armed consent is executed before the presentation refresh: a discovery failure afterwards cannot erase it (the exact,
                // still-valid removal of A is recorded first and the failure then surfaces); every validation failure is a terminal record for A.
                var discovery = kase == "discovery throws";
                TestRunner.True(stored != null && stored.HorseId == "horse-A" && later == null && !host.Console.Armed &&
                    (discovery ? threw && stored.Disposition == "removed" && host.NativeCalls == 2
                               : record == stored && stored.Disposition.StartsWith("not removed:", StringComparison.Ordinal) && host.NativeCalls == 1),
                    "PR-4 round 13 (" + kase + "): the confirmed action was erased, misattributed or executed later (" + (stored?.ToString() ?? "<none>") + ").");
            }
        }

        private static void GatesRefuse()
        {
            foreach (var kase in new[] { "saving (write gate closed)", "loading in progress", "service not live" })
            {
                var host = new Host(); var a = LegacyHorse("horse-A"); host.Loaded.Add(a);
                host.Update();
                host.ConfirmOffer(a);
                if (kase.StartsWith("saving")) host.WriteGateOpen = false; else if (kase.StartsWith("loading")) host.Loading = true; else host.ServiceLive = false;
                var record = host.Update();
                host.WriteGateOpen = true; host.Loading = false; host.ServiceLive = true;
                var later = host.Update();
                TestRunner.True(record != null && record.Disposition.StartsWith("not removed: the world was not settled", StringComparison.Ordinal) && later == null && host.NativeCalls == 0 && Candidates(a).Count == 1,
                    "PR-4 round 13 (" + kase + "): the repair ran without a settled world or kept its consent for later.");
            }
        }

        private static void SourcedModifiedAmbiguous()
        {
            var host = new Host();
            var sourced = LegacyHorse("horse-sourced"); sourced.Facts[0].Sourceless = false;
            var modified = LegacyHorse("horse-modified"); modified.Facts[0].BiteTripShape = false;
            var ambiguous = LegacyHorse("horse-two"); ambiguous.Facts.Add(StockGrant("TrippingBite (second)"));
            host.Loaded.AddRange(new[] { sourced, modified, ambiguous });
            host.Update();
            var ids = host.Console.Offers.Select(o => o.HorseId).ToArray();
            var amb = host.Offer(ambiguous);
            TestRunner.True(!ids.Contains("horse-sourced") && !ids.Contains("horse-modified") && !amb.Eligible && amb.CandidateCount == 2 && !host.Console.Select(amb.Token) &&
                host.NativeCalls == 0 && sourced.Facts.Count == 6 && modified.Facts.Count == 6 && Candidates(ambiguous).Count == 2,
                "A sourced, modified or ambiguous fact was offered or removed.");
        }

        private static void RemovalOutcomes()
        {
            foreach (var kase in new[] { "exact", "throws after the exact removal", "throws before mutating", "target and a neighbour removed", "post-removal read fails", "native message getter throws" })
            {
                var host = new Host(); var a = LegacyHorse("horse-A"); host.Loaded.Add(a);
                var neighbour = a.Facts[1];
                if (kase == "throws after the exact removal") host.NativeRemove = (h, f) => { h.Facts.Remove(f); throw new InvalidOperationException("after mutation"); };
                if (kase == "throws before mutating") host.NativeRemove = (h, f) => { throw new InvalidOperationException("before mutation"); };
                if (kase == "target and a neighbour removed") host.NativeRemove = (h, f) => { h.Facts.Remove(f); h.Facts.Remove(neighbour); };
                if (kase == "post-removal read fails") host.ReadAfter = h => { throw new InvalidOperationException("collection unavailable after removal"); };
                if (kase == "native message getter throws") host.NativeRemove = (h, f) => { h.Facts.Remove(f); throw new UnreadableMessageException(); };
                host.Update();
                host.ConfirmOffer(a);
                var record = host.Update();
                var again = host.Update();
                string expect;
                if (kase == "exact" || kase == "throws after the exact removal" || kase == "native message getter throws") expect = "removed";
                else if (kase == "throws before mutating") expect = "not removed: the horse was verified unchanged";
                else expect = "uncertain:";
                TestRunner.True(record != null && record.Disposition.StartsWith(expect, StringComparison.Ordinal) && again == null && host.NativeCalls == 1 &&
                    host.Console.RemovalCount == (expect == "removed" ? 1 : 0),
                    "PR-4 round 13 (" + kase + "): recorded '" + record?.Disposition + "', expected '" + expect + "', or repeated.");
            }
        }

        private static void ReportingFailureDoesNotRepeat()
        {
            var host = new Host { LogThrows = true }; var a = LegacyHorse("horse-A"); host.Loaded.Add(a);
            host.Update();
            host.ConfirmOffer(a);
            var threw = false;
            try { host.Update(); } catch (IOException) { threw = true; }
            host.LogThrows = false;
            var next = host.Update();
            TestRunner.True(threw && host.Console.LastRecord != null && host.Console.LastRecord.Disposition == "removed" && next == null && host.NativeCalls == 1 && host.Console.RemovalCount == 1,
                "A reporting failure lost the recorded outcome or repeated the removal.");
        }

        private static void OldRequestFilesAreInert()
        {
            var root = Path.Combine(Path.GetTempPath(), "kmc-trip-old-files-" + Guid.NewGuid().ToString("N"));
            var folder = Path.Combine(root, "KingmakerMountedCombat");
            Directory.CreateDirectory(folder);
            try
            {
                foreach (var file in new Dictionary<string, string> { { "horse-trip-repair-request.txt", "horse=horse-A\r\n" }, { "horse-trip-repair-request.txt.claimed", "horse=horse-A\r\n" },
                    { "horse-trip-repair-result.txt", "2026-11-26 10:00:00Z\r\nApplied\r\n" }, { "horse-trip-repair-request.txt.claimed.0a1b2c3d.retiring", "horse=horse-A\r\n" } })
                    File.WriteAllText(Path.Combine(folder, file.Key), file.Value);
                Func<string> digest = () =>
                {
                    using (var sha = SHA256.Create())
                        return string.Join("|", Directory.GetFiles(folder).OrderBy(f => f, StringComparer.Ordinal).Select(f => Path.GetFileName(f) + ":" +
                            BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(f)))).ToArray());
                };
                var before = digest();
                var host = new Host(); var a = LegacyHorse("horse-A"); host.Loaded.Add(a);
                for (var i = 0; i < 5; i++) host.Update();
                TestRunner.True(digest() == before && host.NativeCalls == 0 && Candidates(a).Count == 1 && host.Console.LastRecord == null,
                    "An old request, claim, retiring entry or result file caused a mutation or was changed.");
            }
            finally { try { Directory.Delete(root, true); } catch (IOException) { } }
        }

        private static void NeighboursStay()
        {
            var host = new Host(); var a = LegacyHorse("horse-A"); var other = LegacyHorse("horse-other"); host.Loaded.Add(a); host.Loaded.Add(other);
            var dog = new Horse { Id = "dog", Name = "Riding Dog", ExactKmcHorse = false }; dog.Facts.Add(StockGrant()); host.Loaded.Add(dog);
            host.Update();
            host.ConfirmOffer(a);
            host.Update();
            TestRunner.True(a.Facts.Select(f => f.Name).SequenceEqual(new[] { "NaturalArmor7", "TripDefenseFourLegs", "Bite1d4", "Hoof1d4", "Hoof1d4" }) && Candidates(other).Count == 1 &&
                dog.Facts.Count == 1 && !host.Console.Offers.Any(o => o.HorseId == "dog"),
                "A protected neighbour, another horse or a non-Horse companion was changed or offered.");
        }
    }
}
