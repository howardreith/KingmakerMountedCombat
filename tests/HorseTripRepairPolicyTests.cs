using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // MMR-06 existing horses, PR-4 round 12 (OWNER AMENDMENT revision 1, owner instruction 2026-11-27b section 4): nothing is removed
    // automatically; one fact is removed from one horse only through the owner's confirmed in-game maintenance action. These tests drive
    // the production HorseTripRepairConsole (candidate snapshots, intent, consent, guarded dispatch, record) together with the production
    // HorseTripRemoval classification around a simulated native boundary (fake horses and facts behind the same delegates the
    // HorseCompanionBlueprintService passes). The service's own update method needs the game and is covered by the native contract
    // (Test-MmrHorseTripNativeContract.ps1); actual UI appearance and save/load are pending the owner's integrated-alpha playtest.
    // The file-request protocol tests are retired; docs/missions/mmr-split/PR-4-amendment-test-mapping.md maps each to the amendment.
    internal static class HorseTripRepairPolicyTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("MMR-06: a candidate needs the exact Horse, the stock identity and shape, and no provenance", CandidatePredicate);
            runner.Run("PR-4 round 12: discovery presents stable snapshots of each candidate horse; nothing is removed without a confirmation", DiscoveryIsReadOnly);
            runner.Run("PR-4 round 12: two horses: selecting one, switching to the other and cancelling change nothing; the confirmed horse alone is repaired once", TwoHorsesAndCancellation);
            runner.Run("PR-4 round 12: repeated clicks and updates consume one confirmation once (one removal)", RepeatedClicksAndUpdates);
            runner.Run("PR-4 round 12: a reload (same stable id, new unit object, new session) invalidates the earlier consent and token", ReloadInvalidatesConsent);
            runner.Run("PR-4 round 12: saving, loading or a disabled service refuse or drop the consent (no removal; confirm again)", GatesRefuse);
            runner.Run("PR-4 round 12: sourced, modified, changed and ambiguous facts are never removed", ChangedSourcedModifiedAmbiguous);
            runner.Run("PR-4 round 12: a native removal that throws before or after mutating is classified; only an exact removal counts", RemovalThrowing);
            runner.Run("PR-4 round 12: a reporting failure after the outcome never repeats the removal", ReportingFailureDoesNotRepeat);
            runner.Run("PR-4 round 12: old request and result files cause no mutation and stay byte-identical", OldRequestFilesAreInert);
            runner.Run("PR-4 round 12: protected neighbours (bite, hooves, natural armor, trip defense, other companions) stay", NeighboursStay);
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

        // The alpha-1 creation order: [TrippingBite, NaturalArmor7, TripDefenseFourLegs] then the attacks.
        private static Horse LegacyHorse(string id)
        {
            var horse = new Horse { Id = id, Name = "Horse " + id };
            horse.Facts.Add(StockGrant());
            horse.Facts.Add(new Fact { Name = "NaturalArmor7" });
            horse.Facts.Add(new Fact { Name = "TripDefenseFourLegs" });
            horse.Facts.Add(new Fact { Name = "Bite1d4" });
            horse.Facts.Add(new Fact { Name = "Hoof1d4" });
            horse.Facts.Add(new Fact { Name = "Hoof1d4" });
            return horse;
        }

        private static List<Fact> Candidates(Horse horse) => horse.Facts.Where(fact =>
            HorseTripRepairPolicy.IsCandidate(horse.ExactKmcHorse, fact.StockIdentity, fact.BiteTripShape, fact.Sourceless)).ToList();

        // The service's update path around the production console: observe the loaded horses, then execute an armed confirmation through
        // the production HorseTripRemoval classification and a simulated native boundary; the record is logged after it was stored.
        private sealed class Rig
        {
            public object Session = new object();
            public readonly List<Horse> Loaded = new List<Horse>();
            public readonly HorseTripRepairConsole<Horse, Fact> Console = new HorseTripRepairConsole<Horse, Fact>();
            public bool Gate = true;
            public Action<Horse, Fact> NativeRemove = (horse, fact) => horse.Facts.Remove(fact);
            public int NativeCalls;
            public bool LogThrows;
            public readonly List<string> Log = new List<string>();

            public HorseTripRemovalResult Remove(Horse h, Fact f) => HorseTripRemoval.RemoveExactly(f, () => h.Facts, x => { NativeCalls++; NativeRemove(h, x); });

            public HorseTripRepairRecord Update()
            {
                Console.Observe(Session, Loaded.Select(h => new HorseTripRepairConsole<Horse, Fact>.Live
                    { Unit = h, Id = h.Id, Name = h.Name, Owner = "owner of " + h.Id, Candidates = Candidates(h) }).ToList());
                var record = Console.Execute(Session, Gate, h => Loaded.Contains(h), h => Candidates(h), Remove);
                if (record != null)
                {
                    if (LogThrows) throw new IOException("simulated logger failure");
                    Log.Add(record.ToString());
                }
                return record;
            }

            public HorseTripRepairOffer Offer(Horse horse) => Console.Offers.Single(o => o.HorseId == horse.Id);
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
            var rig = new Rig(); var a = LegacyHorse("horse-A"); rig.Loaded.Add(a);
            var clean = new Horse { Id = "horse-clean", Name = "Clean" }; clean.Facts.Add(new Fact { Name = "Bite1d4" }); rig.Loaded.Add(clean);
            for (var i = 0; i < 3; i++) rig.Update();
            var offer = rig.Offer(a);
            TestRunner.True(rig.Console.Offers.Count == 1 && offer.Eligible && offer.CandidateCount == 1 && offer.HorseName == "Horse horse-A" && offer.OwnerName == "owner of horse-A" &&
                rig.NativeCalls == 0 && Candidates(a).Count == 1 && rig.Console.LastRecord == null && rig.Console.Offers.IsReadOnly,
                "Discovery removed something, offered a clean horse, exposed a mutable list, or lost the horse/owner identification.");
        }

        private static void TwoHorsesAndCancellation()
        {
            var rig = new Rig(); var a = LegacyHorse("horse-A"); var b = LegacyHorse("horse-B"); rig.Loaded.Add(a); rig.Loaded.Add(b);
            rig.Update();
            var tokenA = rig.Offer(a).Token; var tokenB = rig.Offer(b).Token;
            rig.Console.Select(tokenA); rig.Update();
            rig.Console.Select(tokenB); rig.Console.Cancel(); rig.Update();
            var afterCancel = rig.NativeCalls;
            var wrongConfirm = rig.Console.Confirm(tokenA);   // nothing selected any more: refused
            rig.Console.Select(tokenB);
            var confirmedB = rig.Console.Confirm(tokenB);
            var record = rig.Update();
            TestRunner.True(afterCancel == 0 && !wrongConfirm && confirmedB && record != null && record.Disposition == "removed" && record.HorseId == "horse-B" &&
                Candidates(a).Count == 1 && Candidates(b).Count == 0 && rig.Console.RemovalCount == 1,
                "Selection, switching or cancellation changed a horse, or the confirmed horse was not the only one repaired.");
        }

        private static void RepeatedClicksAndUpdates()
        {
            var rig = new Rig(); var a = LegacyHorse("horse-A"); rig.Loaded.Add(a);
            rig.Update();
            var token = rig.Offer(a).Token;
            rig.Console.Select(token); rig.Console.Confirm(token); rig.Console.Confirm(token); rig.Console.Select(token); rig.Console.Confirm(token);
            var first = rig.Update(); var second = rig.Update(); var third = rig.Update();
            var reconfirm = rig.Console.Confirm(token);
            TestRunner.True(first != null && first.Disposition == "removed" && second == null && third == null && !reconfirm && rig.NativeCalls == 1 && rig.Console.RemovalCount == 1 &&
                rig.Console.ExecutedCount == 1 && rig.Console.Offers.Count == 0,
                "Repeated clicks or updates repeated the removal or left a consent armed.");
        }

        private static void ReloadInvalidatesConsent()
        {
            var rig = new Rig(); var before = LegacyHorse("horse-A"); rig.Loaded.Add(before);
            rig.Update();
            var oldToken = rig.Offer(before).Token;
            rig.Console.Select(oldToken); rig.Console.Confirm(oldToken);
            // A load: a new session marker and a NEW unit object with the same stable id, before the armed consent executed.
            var reloaded = LegacyHorse("horse-A");
            rig.Session = new object(); rig.Loaded.Clear(); rig.Loaded.Add(reloaded);
            var record = rig.Update();
            var oldTokenWorks = rig.Console.Select(oldToken);
            var newOffer = rig.Offer(reloaded);
            TestRunner.True(record == null && !rig.Console.Armed && !oldTokenWorks && newOffer.Token != oldToken && rig.NativeCalls == 0 && Candidates(reloaded).Count == 1 && Candidates(before).Count == 1,
                "A reload kept the earlier consent or token, or removed a fact from either unit object.");
            // Same session, but the confirmed object was replaced by a new object with the same id before execution: stale.
            rig.Console.Select(newOffer.Token); rig.Console.Confirm(newOffer.Token);
            var replacement = LegacyHorse("horse-A");
            rig.Loaded.Clear(); rig.Loaded.Add(replacement);
            var stale = rig.Console.Execute(rig.Session, true, h => rig.Loaded.Contains(h), h => Candidates(h), rig.Remove);
            TestRunner.True(stale != null && stale.Disposition.StartsWith("stale:", StringComparison.Ordinal) && rig.NativeCalls == 0 && Candidates(replacement).Count == 1 && Candidates(reloaded).Count == 1,
                "A consent for a replaced unit object was executed against its successor.");
        }

        private static void GatesRefuse()
        {
            foreach (var kase in new[] { "saving/loading (gate closed)", "disabled service" })
            {
                var rig = new Rig(); var a = LegacyHorse("horse-A"); rig.Loaded.Add(a);
                rig.Update();
                var token = rig.Offer(a).Token;
                rig.Console.Select(token); rig.Console.Confirm(token);
                HorseTripRepairRecord record;
                if (kase.StartsWith("saving")) { rig.Gate = false; record = rig.Update(); }
                else { rig.Console.Invalidate(); record = rig.Console.Execute(rig.Session, true, h => true, h => Candidates(h), rig.Remove); }
                rig.Gate = true;
                var later = rig.Update();
                TestRunner.True(rig.NativeCalls == 0 && Candidates(a).Count == 1 && later == null && !rig.Console.Armed &&
                    (kase.StartsWith("saving") ? record != null && record.Disposition.StartsWith("refused:", StringComparison.Ordinal) : record == null),
                    "PR-4 round 12 (" + kase + "): the repair ran without a settled world or kept its consent for later.");
            }
        }

        private static void ChangedSourcedModifiedAmbiguous()
        {
            var rig = new Rig();
            var sourced = LegacyHorse("horse-sourced"); sourced.Facts[0].Sourceless = false;
            var modified = LegacyHorse("horse-modified"); modified.Facts[0].BiteTripShape = false;
            var ambiguous = LegacyHorse("horse-two"); ambiguous.Facts.Add(StockGrant("TrippingBite (second)"));
            var changing = LegacyHorse("horse-changing");
            rig.Loaded.AddRange(new[] { sourced, modified, ambiguous, changing });
            rig.Update();
            var offeredIds = rig.Console.Offers.Select(o => o.HorseId).ToArray();
            var ambiguousOffer = rig.Offer(ambiguous);
            var ambiguousSelectable = rig.Console.Select(ambiguousOffer.Token);
            var token = rig.Offer(changing).Token;
            rig.Console.Select(token); rig.Console.Confirm(token);
            // The candidate instance is replaced (the same name, a new fact object) between confirmation and execution.
            changing.Facts[0] = StockGrant();
            var record = rig.Console.Execute(rig.Session, true, h => rig.Loaded.Contains(h), h => Candidates(h), rig.Remove);
            TestRunner.True(!offeredIds.Contains("horse-sourced") && !offeredIds.Contains("horse-modified") && !ambiguousOffer.Eligible && !ambiguousSelectable &&
                record != null && record.Disposition.StartsWith("stale:", StringComparison.Ordinal) && rig.NativeCalls == 0 &&
                Candidates(ambiguous).Count == 2 && Candidates(changing).Count == 1 && sourced.Facts.Count == 6 && modified.Facts.Count == 6,
                "A sourced, modified, ambiguous or replaced fact was offered or removed.");
        }

        private static void RemovalThrowing()
        {
            foreach (var kase in new[] { "throws before mutating", "throws after the exact removal", "removes a neighbour too" })
            {
                var rig = new Rig(); var a = LegacyHorse("horse-A"); rig.Loaded.Add(a);
                var neighbour = a.Facts[1];
                if (kase == "throws before mutating") rig.NativeRemove = (h, f) => { throw new InvalidOperationException("native failure before mutation"); };
                if (kase == "throws after the exact removal") rig.NativeRemove = (h, f) => { h.Facts.Remove(f); throw new InvalidOperationException("native failure after mutation"); };
                if (kase == "removes a neighbour too") rig.NativeRemove = (h, f) => { h.Facts.Remove(f); h.Facts.Remove(neighbour); };
                rig.Update();
                var token = rig.Offer(a).Token;
                rig.Console.Select(token); rig.Console.Confirm(token);
                var record = rig.Update();
                var again = rig.Update();
                var outcome = record?.Removal?.Outcome;
                var ok = kase == "throws before mutating" ? outcome == HorseTripRemovalOutcome.Unchanged && rig.Console.RemovalCount == 0 && Candidates(a).Count == 1 :
                    kase == "throws after the exact removal" ? outcome == HorseTripRemovalOutcome.Removed && rig.Console.RemovalCount == 1 && Candidates(a).Count == 0 :
                    outcome != HorseTripRemovalOutcome.Removed && outcome != HorseTripRemovalOutcome.Unchanged && rig.Console.RemovalCount == 0;
                TestRunner.True(ok && again == null && rig.NativeCalls == 1, "PR-4 round 12 (" + kase + "): misclassified (" + outcome + ") or repeated.");
            }
        }

        private static void ReportingFailureDoesNotRepeat()
        {
            var rig = new Rig { LogThrows = true }; var a = LegacyHorse("horse-A"); rig.Loaded.Add(a);
            rig.Update();
            var token = rig.Offer(a).Token;
            rig.Console.Select(token); rig.Console.Confirm(token);
            var threw = false;
            try { rig.Update(); } catch (IOException) { threw = true; }
            rig.LogThrows = false;
            var next = rig.Update();
            TestRunner.True(threw && rig.Console.LastRecord != null && rig.Console.LastRecord.Disposition == "removed" && next == null && rig.NativeCalls == 1 &&
                rig.Console.RemovalCount == 1, "A reporting failure lost the recorded outcome or repeated the removal.");
        }

        private static void OldRequestFilesAreInert()
        {
            var root = Path.Combine(Path.GetTempPath(), "kmc-trip-old-files-" + Guid.NewGuid().ToString("N"));
            var folder = Path.Combine(root, "KingmakerMountedCombat");
            Directory.CreateDirectory(folder);
            try
            {
                var files = new Dictionary<string, string>
                {
                    { "horse-trip-repair-request.txt", "horse=horse-A\r\n" }, { "horse-trip-repair-request.txt.claimed", "horse=horse-A\r\n" },
                    { "horse-trip-repair-result.txt", "2026-11-26 10:00:00Z\r\nApplied\r\n" }, { "horse-trip-repair-request.txt.claimed.0a1b2c3d.retiring", "horse=horse-A\r\n" }
                };
                foreach (var file in files) File.WriteAllText(Path.Combine(folder, file.Key), file.Value);
                Func<string> digest = () =>
                {
                    using (var sha = SHA256.Create())
                        return string.Join("|", Directory.GetFiles(folder).OrderBy(f => f, StringComparer.Ordinal).Select(f => Path.GetFileName(f) + ":" +
                            BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(f)))).ToArray());
                };
                var before = digest();
                var rig = new Rig(); var a = LegacyHorse("horse-A"); rig.Loaded.Add(a);
                for (var i = 0; i < 5; i++) rig.Update();
                TestRunner.True(digest() == before && rig.NativeCalls == 0 && Candidates(a).Count == 1 && rig.Console.LastRecord == null,
                    "An old request, claim, retiring entry or result file caused a mutation or was changed.");
            }
            finally { try { Directory.Delete(root, true); } catch (IOException) { } }
        }

        private static void NeighboursStay()
        {
            var rig = new Rig(); var a = LegacyHorse("horse-A"); var other = LegacyHorse("horse-other"); rig.Loaded.Add(a); rig.Loaded.Add(other);
            var dog = new Horse { Id = "dog", Name = "Riding Dog", ExactKmcHorse = false }; dog.Facts.Add(StockGrant()); rig.Loaded.Add(dog);
            rig.Update();
            var token = rig.Offer(a).Token;
            rig.Console.Select(token); rig.Console.Confirm(token);
            rig.Update();
            var names = a.Facts.Select(f => f.Name).ToArray();
            TestRunner.True(names.SequenceEqual(new[] { "NaturalArmor7", "TripDefenseFourLegs", "Bite1d4", "Hoof1d4", "Hoof1d4" }) && Candidates(other).Count == 1 &&
                dog.Facts.Count == 1 && !rig.Console.Offers.Any(o => o.HorseId == "dog"),
                "A protected neighbour, another horse or a non-Horse companion was changed or offered.");
        }
    }
}
