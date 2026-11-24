using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // MMR-06 existing horses (review R3 R1-F1): nothing is removed automatically; one fact is removed from one horse only on
    // the owner's exact request, through HorseTripRepairPolicy.Process (the integration's file host implements the same
    // interface over the mod folder and the horse's FeatureCollection).
    internal static class HorseTripRepairPolicyTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("MMR-06: a candidate needs the exact Horse, the stock identity and shape, and no provenance", CandidatePredicate);
            runner.Run("MMR-06: a request names one horse by one exact line", RequestParsing);
            runner.Run("MMR-06: the exact request removes exactly one fact from that horse and is consumed", ExactRequestApplies);
            runner.Run("MMR-06: legacy-ordered and independently granted facts stay without a request; refusals change nothing", NegativeCases);
            runner.Run("MMR-06: a closed gate, an unloaded horse and a failed removal keep the request for a retry", PendingAndRetry);
        }

        private sealed class Fact
        {
            public string Name;
            public bool StockIdentity, BiteTripShape, Sourceless;
        }

        private sealed class Horse
        {
            public string Id;
            public bool ExactKmcHorse = true;
            public readonly List<Fact> Facts = new List<Fact>();
        }

        private static Fact StockGrant(string name = "TrippingBite") =>
            new Fact { Name = name, StockIdentity = true, BiteTripShape = true, Sourceless = true };

        // The alpha-1 creation order: [TrippingBite, NaturalArmor7, TripDefenseFourLegs] then class and level features.
        private static Horse LegacyHorse(string id)
        {
            var horse = new Horse { Id = id };
            horse.Facts.Add(StockGrant());
            horse.Facts.Add(new Fact { Name = "NaturalArmor7", Sourceless = true });
            horse.Facts.Add(new Fact { Name = "TripDefenseFourLegs", Sourceless = true });
            horse.Facts.Add(new Fact { Name = "AnimalCompanionClassFeature" });
            return horse;
        }

        private sealed class Host : IHorseTripRepairHost<Fact>
        {
            public string Request;
            public string Consumed;
            public string Result;
            public int Removals;
            public bool FailNextRemoval;
            public readonly List<Horse> Loaded = new List<Horse>();
            private Horse current;

            public bool TryReadRequest(out string text) { text = Request; return text != null; }

            public IList<Fact> CandidatesOf(string horseId, out bool loaded)
            {
                current = Loaded.FirstOrDefault(horse => horse.Id == horseId);
                loaded = current != null;
                return current == null ? new List<Fact>() : Candidates(current);
            }

            public void Remove(Fact fact)
            {
                if (FailNextRemoval) { FailNextRemoval = false; throw new InvalidOperationException("simulated removal failure"); }
                TestRunner.True(current.Facts.Remove(fact), "The removed fact was not the horse's own instance.");
                Removals++;
            }

            public void Consume(string suffix, string result) { Consumed = suffix; Result = result; Request = null; }
        }

        private static List<Fact> Candidates(Horse horse) => horse.Facts.Where(fact =>
            HorseTripRepairPolicy.IsCandidate(horse.ExactKmcHorse, fact.StockIdentity, fact.BiteTripShape, fact.Sourceless)).ToList();

        private static void CandidatePredicate()
        {
            TestRunner.True(HorseTripRepairPolicy.IsCandidate(true, true, true, true), "The exact sourceless stock grant on the Horse was not a candidate.");
            TestRunner.True(!HorseTripRepairPolicy.IsCandidate(true, true, true, false), "A grant with a source, item or cutscene was a candidate.");
            TestRunner.True(!HorseTripRepairPolicy.IsCandidate(true, false, true, true), "A different GUID with the same shape was a candidate.");
            TestRunner.True(!HorseTripRepairPolicy.IsCandidate(true, true, false, true), "The stock GUID with a different maneuver or category was a candidate.");
            TestRunner.True(!HorseTripRepairPolicy.IsCandidate(false, true, true, true), "A dog, wolf or other unit's TrippingBite was a candidate.");
        }

        private static void RequestParsing()
        {
            TestRunner.Equal("horse=ab12", HorseTripRepairPolicy.FormatRequestLine("ab12"), "The request line format changed.");
            TestRunner.Equal("ab12", HorseTripRepairPolicy.ParseRequest("# copied\r\n  horse=ab12  \r\n"), "A valid request was not read.");
            TestRunner.Equal("ab12", HorseTripRepairPolicy.ParseRequest(HorseTripRepairPolicy.FormatRequestLine("ab12")), "The formatted line did not round-trip.");
            foreach (var bad in new[] { "", "ab12", "horse=", "horse=a b", "horse=a=b", "remove all", "\r\n# only a comment\r\n" })
                TestRunner.True(HorseTripRepairPolicy.ParseRequest(bad) == null, "A malformed request selected a horse: '" + bad + "'.");
            TestRunner.True(HorseTripRepairPolicy.ParseRequest(null) == null, "A missing request selected a horse.");
        }

        private static void ExactRequestApplies()
        {
            var host = new Host();
            var horse = LegacyHorse("horse-1");
            var other = LegacyHorse("horse-2");
            host.Loaded.Add(horse); host.Loaded.Add(other);
            var target = horse.Facts[0];
            host.Request = HorseTripRepairPolicy.FormatRequestLine("horse-1");
            TestRunner.Equal(HorseTripRepairDecision.Apply, HorseTripRepairPolicy.Process(host, true), "The exact request was not applied.");
            TestRunner.True(host.Removals == 1 && !horse.Facts.Contains(target) && horse.Facts.Count == 3 &&
                horse.Facts.Select(fact => fact.Name).SequenceEqual(new[] { "NaturalArmor7", "TripDefenseFourLegs", "AnimalCompanionClassFeature" }),
                "Something other than exactly the requested TrippingBite changed on that horse.");
            TestRunner.True(Candidates(other).Count == 1 && other.Facts.Count == 4, "The other horse was touched.");
            TestRunner.True(host.Consumed == HorseTripRepairPolicy.AppliedSuffix && host.Result.Contains("horse-1") && host.Result.Contains("reload"),
                "The applied request was not consumed with its result and recovery note.");
            // Consumed: a later update and a reload of the same save (new loaded objects, same IDs) apply nothing.
            TestRunner.Equal(HorseTripRepairDecision.NoRequest, HorseTripRepairPolicy.Process(host, true), "A consumed request was read again.");
            host.Loaded.Clear(); host.Loaded.Add(LegacyHorse("horse-1"));
            TestRunner.True(HorseTripRepairPolicy.Process(host, true) == HorseTripRepairDecision.NoRequest && host.Removals == 1,
                "A consumed request was re-applied after a reload.");
        }

        private static void NegativeCases()
        {
            // No request: a legacy-ordered sourceless copy is only a candidate; nothing is removed.
            var host = new Host();
            var legacy = LegacyHorse("horse-1");
            host.Loaded.Add(legacy);
            TestRunner.True(HorseTripRepairPolicy.Process(host, true) == HorseTripRepairDecision.NoRequest && host.Removals == 0 &&
                Candidates(legacy).Count == 1 && legacy.Facts.Count == 4, "A candidate was removed without the owner's request.");

            // An independently granted copy in exactly the legacy visible order, but recording its source: not a candidate,
            // so even an exact request for that horse is refused and changes nothing.
            var sourced = LegacyHorse("horse-3");
            sourced.Facts[0].Sourceless = false;
            host.Loaded.Add(sourced);
            host.Request = HorseTripRepairPolicy.FormatRequestLine("horse-3");
            TestRunner.True(HorseTripRepairPolicy.Process(host, true) == HorseTripRepairDecision.NoCandidate && host.Removals == 0 &&
                sourced.Facts.Count == 4 && host.Consumed == HorseTripRepairPolicy.RefusedSuffix, "An independently granted (sourced) fact was removed.");

            // Same GUID, different maneuver or category; a different GUID with the same shape: not candidates.
            foreach (var mutate in new Action<Fact>[] { fact => fact.BiteTripShape = false, fact => fact.StockIdentity = false })
            {
                var horse = LegacyHorse("horse-4"); mutate(horse.Facts[0]);
                var h = new Host(); h.Loaded.Add(horse); h.Request = HorseTripRepairPolicy.FormatRequestLine("horse-4");
                TestRunner.True(HorseTripRepairPolicy.Process(h, true) == HorseTripRepairDecision.NoCandidate && h.Removals == 0 && horse.Facts.Count == 4,
                    "A fact that is not the exact stock grant was removed.");
            }

            // A dog or wolf (not the KMC Horse blueprint) keeps its legitimate TrippingBite even if its ID is requested.
            var wolf = LegacyHorse("wolf-1"); wolf.ExactKmcHorse = false;
            var wh = new Host(); wh.Loaded.Add(wolf); wh.Request = HorseTripRepairPolicy.FormatRequestLine("wolf-1");
            TestRunner.True(HorseTripRepairPolicy.Process(wh, true) == HorseTripRepairDecision.NoCandidate && wh.Removals == 0 && wolf.Facts.Count == 4,
                "A wolf's or dog's TrippingBite was removed.");

            // Two sourceless stock copies: the inherited one cannot be singled out; refused, nothing removed.
            var doubled = LegacyHorse("horse-5"); doubled.Facts.Add(StockGrant("TrippingBite (second)"));
            var dh = new Host(); dh.Loaded.Add(doubled); dh.Request = HorseTripRepairPolicy.FormatRequestLine("horse-5");
            TestRunner.True(HorseTripRepairPolicy.Process(dh, true) == HorseTripRepairDecision.Ambiguous && dh.Removals == 0 && doubled.Facts.Count == 5 &&
                dh.Consumed == HorseTripRepairPolicy.RefusedSuffix, "An ambiguous horse was changed.");

            // A malformed request is refused and consumed; nothing is removed.
            var mh = new Host(); var m = LegacyHorse("horse-6"); mh.Loaded.Add(m); mh.Request = "please fix my horse";
            TestRunner.True(HorseTripRepairPolicy.Process(mh, true) == HorseTripRepairDecision.Malformed && mh.Removals == 0 && m.Facts.Count == 4 &&
                mh.Consumed == HorseTripRepairPolicy.RefusedSuffix, "A malformed request changed a horse.");
        }

        private static void PendingAndRetry()
        {
            var host = new Host();
            var horse = LegacyHorse("horse-1");
            host.Request = HorseTripRepairPolicy.FormatRequestLine("horse-1");
            // A wrong horse ID (or that horse not loaded in the party): kept for later, nothing removed.
            host.Loaded.Add(LegacyHorse("horse-9"));
            TestRunner.True(HorseTripRepairPolicy.Process(host, true) == HorseTripRepairDecision.HorseNotLoaded && host.Request != null &&
                host.Removals == 0 && host.Consumed == null, "A request for an unloaded horse was consumed or changed another horse.");
            host.Loaded.Add(horse);
            // The write gate closed (save, area transition, restoration, load): pending, the horse is not even read.
            TestRunner.True(HorseTripRepairPolicy.Process(host, false) == HorseTripRepairDecision.Pending && host.Request != null &&
                host.Removals == 0 && Candidates(horse).Count == 1, "A closed write gate let the repair run.");
            // A removal that throws leaves the request in place; the next settled update retries and applies it once.
            host.FailNextRemoval = true;
            var threw = false;
            try { HorseTripRepairPolicy.Process(host, true); } catch (InvalidOperationException) { threw = true; }
            TestRunner.True(threw && host.Request != null && host.Consumed == null && Candidates(horse).Count == 1,
                "A failed removal consumed the request or changed the horse.");
            TestRunner.True(HorseTripRepairPolicy.Process(host, true) == HorseTripRepairDecision.Apply && host.Removals == 1 &&
                Candidates(horse).Count == 0 && host.Consumed == HorseTripRepairPolicy.AppliedSuffix, "The retry did not apply exactly once.");
        }
    }
}
