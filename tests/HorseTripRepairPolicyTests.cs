using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // MMR-06 existing horses (reviews R3 R1-F1, R4-F4, R4-F5, R4-F6): nothing is removed automatically; one fact is removed
    // from one horse only on the owner's exact request, through the production HorseTripRepairPolicy.Process and the
    // production HorseTripRepairFiles on real temporary folders (a KMC user-data folder, and a guarded-style staged Mods
    // folder that must stay byte-identical). Horses and facts are fakes behind the same host interface the integration uses.
    internal static class HorseTripRepairPolicyTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("MMR-06: a candidate needs the exact Horse, the stock identity and shape, and no provenance", CandidatePredicate);
            runner.Run("R4-F4: the whole request is validated; two horses or extra content select nothing", RequestParsing);
            runner.Run("MMR-06/R4-F6: the exact request removes one fact, is claimed and closed in user data, Mods tree untouched", ExactRequestApplies);
            runner.Run("MMR-06: legacy-ordered and independently granted facts stay without a request; refusals change nothing", NegativeCases);
            runner.Run("MMR-06: a closed gate, an unloaded horse and a failed removal keep the request for a retry", PendingAndRetry);
            runner.Run("R4-F5: failures after removal, at the claim, the result write and the final rename never record a refusal", ReportingFailures);
            runner.Run("R4-F5: a stranded claim finishes as applied without a second removal, also after a reload; a restart closes it unchanged", StrandedClaims);
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

        // Real folders: <root>/Pathfinder Kingmaker/KingmakerMountedCombat (user data) and <root>/Mods/KingmakerMountedCombat (staged).
        private sealed class Folders : IDisposable
        {
            public readonly string Root = Path.Combine(Path.GetTempPath(), "kmc-trip-repair-" + Guid.NewGuid().ToString("N"));
            public readonly string UserData;
            public readonly string StagedMod;

            public Folders(bool createUserData = true)
            {
                UserData = HorseTripRepairPaths.Folder(Path.Combine(Root, "Pathfinder Kingmaker"));
                StagedMod = Path.Combine(Root, "Mods", "KingmakerMountedCombat");
                Directory.CreateDirectory(StagedMod);
                File.WriteAllText(Path.Combine(StagedMod, "Info.json"), "{\"Id\":\"KingmakerMountedCombat\"}");
                File.WriteAllBytes(Path.Combine(StagedMod, "KingmakerMountedCombat.dll"), new byte[] { 77, 90, 1, 2, 3 });
                if (createUserData) Directory.CreateDirectory(UserData);
            }

            public void Request(string text) => File.WriteAllText(Path.Combine(UserData, HorseTripRepairPolicy.RequestFileName), text);

            public string[] UserFiles() => Directory.Exists(UserData)
                ? Directory.GetFiles(UserData).Select(Path.GetFileName).OrderBy(name => name, StringComparer.Ordinal).ToArray() : new string[0];

            public string Result() => File.ReadAllText(Path.Combine(UserData, HorseTripRepairPolicy.ResultFileName));

            public string StagedDigest()
            {
                using (var sha = SHA256.Create())
                    return string.Join("|", Directory.GetFiles(StagedMod, "*", SearchOption.AllDirectories).OrderBy(path => path, StringComparer.Ordinal)
                        .Select(path => path.Substring(StagedMod.Length) + ":" + BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path)))).ToArray());
            }

            public void Dispose() { try { Directory.Delete(Root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
        }

        private sealed class Host : IHorseTripRepairHost<Fact>
        {
            private readonly HorseTripRepairFiles files;
            public int Removals;
            public bool FailRemovalBefore, FailRemovalAfter, FailClaim, FailResultWrite, FailClose;
            public readonly List<Horse> Loaded = new List<Horse>();
            private Horse current;

            public Host(Folders folders) { files = new HorseTripRepairFiles(folders.UserData); }

            public bool TryReadRequest(out string text) => files.TryReadRequest(out text);
            public bool TryReadClaim(out string text) => files.TryReadClaim(out text);

            public IList<Fact> CandidatesOf(string horseId, out bool loaded)
            {
                current = Loaded.FirstOrDefault(horse => horse.Id == horseId);
                loaded = current != null;
                return current == null ? new List<Fact>() : Candidates(current);
            }

            public bool StillPresent(Fact fact) => current != null && current.Facts.Contains(fact);

            public void Remove(Fact fact)
            {
                if (FailRemovalBefore) { FailRemovalBefore = false; throw new InvalidOperationException("simulated failure before removal"); }
                TestRunner.True(current.Facts.Remove(fact), "The removed fact was not the horse's own instance.");
                Removals++;
                if (FailRemovalAfter) { FailRemovalAfter = false; throw new InvalidOperationException("simulated failure after RemoveFact"); }
            }

            public void Claim()
            {
                if (FailClaim) { FailClaim = false; throw new IOException("simulated claim rename failure"); }
                files.Claim();
            }

            public void ReleaseClaim() => files.ReleaseClaim();

            public void WriteResult(string result)
            {
                if (FailResultWrite) { FailResultWrite = false; throw new IOException("simulated result write failure"); }
                files.WriteResult(result);
            }

            public void Close(bool claimed, string suffix)
            {
                if (FailClose) { FailClose = false; throw new IOException("simulated final rename failure"); }
                files.Close(claimed, suffix);
            }
        }

        private static List<Fact> Candidates(Horse horse) => horse.Facts.Where(fact =>
            HorseTripRepairPolicy.IsCandidate(horse.ExactKmcHorse, fact.StockIdentity, fact.BiteTripShape, fact.Sourceless)).ToList();

        private static HorseTripRepairDecision Run(Host host, HorseTripRepairSession session, bool gate = true) =>
            HorseTripRepairPolicy.Process(host, gate, session);

        private static bool Throws(Action action)
        {
            try { action(); return false; }
            catch (Exception) { return true; }
        }

        private static readonly string Request = HorseTripRepairPolicy.RequestFileName;
        private static readonly string Claimed = HorseTripRepairPolicy.RequestFileName + HorseTripRepairPolicy.ClaimedSuffix;
        private static readonly string Applied = HorseTripRepairPolicy.RequestFileName + HorseTripRepairPolicy.AppliedSuffix;
        private static readonly string Refused = HorseTripRepairPolicy.RequestFileName + HorseTripRepairPolicy.RefusedSuffix;
        private static readonly string Interrupted = HorseTripRepairPolicy.RequestFileName + HorseTripRepairPolicy.InterruptedSuffix;
        private static readonly string ResultFile = HorseTripRepairPolicy.ResultFileName;

        private static string[] Files(params string[] names) => names.OrderBy(name => name, StringComparer.Ordinal).ToArray();

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
            TestRunner.Equal("ab12", HorseTripRepairPolicy.ParseRequest("# copied from the log\r\n\r\n  horse=ab12  \r\n# my second horse stays\r\n"),
                "One valid ID with comments and blank lines was not read.");
            TestRunner.Equal("ab12", HorseTripRepairPolicy.ParseRequest(HorseTripRepairPolicy.FormatRequestLine("ab12")), "The formatted line did not round-trip.");
            foreach (var bad in new[] { "horse=ab12\r\nhorse=cd34", "horse=ab12\nhorse=ab12", "horse=ab12\r\nremove all", "horse=ab12\r\n  x",
                "", "ab12", "horse=", "horse=a b", "horse=a=b", "horse=a#b", "remove all", "\r\n# only a comment\r\n" })
                TestRunner.True(HorseTripRepairPolicy.ParseRequest(bad) == null, "A malformed or ambiguous request selected a horse: '" + bad + "'.");
            TestRunner.True(HorseTripRepairPolicy.ParseRequest(null) == null, "A missing request selected a horse.");
            // Both ambiguous inputs end in a refusal with zero removals, even with both horses loaded.
            foreach (var ambiguous in new[] { "horse=horse-1\r\nhorse=horse-2\r\n", "horse=horse-1\r\nplease also fix horse-2\r\n" })
                using (var folders = new Folders())
                {
                    var host = new Host(folders); var a = LegacyHorse("horse-1"); var b = LegacyHorse("horse-2");
                    host.Loaded.Add(a); host.Loaded.Add(b); folders.Request(ambiguous);
                    TestRunner.True(Run(host, new HorseTripRepairSession()) == HorseTripRepairDecision.Malformed && host.Removals == 0 &&
                        Candidates(a).Count == 1 && Candidates(b).Count == 1 && folders.UserFiles().SequenceEqual(Files(Refused, ResultFile)),
                        "An ambiguous request removed something or was not refused: " + ambiguous);
                }
        }

        private static void ExactRequestApplies()
        {
            using (var folders = new Folders())
            {
                var stagedBefore = folders.StagedDigest();
                var host = new Host(folders);
                var session = new HorseTripRepairSession();
                var horse = LegacyHorse("horse-1");
                var other = LegacyHorse("horse-2");
                host.Loaded.Add(horse); host.Loaded.Add(other);
                // Discovery writes nothing (candidates are logged only).
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.NoRequest && folders.UserFiles().Length == 0,
                    "Candidate discovery wrote into the user-data folder.");
                var target = horse.Facts[0];
                folders.Request("# from the log\r\n" + HorseTripRepairPolicy.FormatRequestLine("horse-1") + "\r\n");
                TestRunner.Equal(HorseTripRepairDecision.Apply, Run(host, session), "The exact request was not applied.");
                TestRunner.True(host.Removals == 1 && !horse.Facts.Contains(target) && horse.Facts.Count == 3 &&
                    horse.Facts.Select(fact => fact.Name).SequenceEqual(new[] { "NaturalArmor7", "TripDefenseFourLegs", "AnimalCompanionClassFeature" }),
                    "Something other than exactly the requested TrippingBite changed on that horse.");
                TestRunner.True(Candidates(other).Count == 1 && other.Facts.Count == 4, "The other horse was touched.");
                TestRunner.True(folders.UserFiles().SequenceEqual(Files(Applied, ResultFile)) && folders.Result().Contains("horse-1") &&
                    folders.Result().Contains("reload") && session.PendingReports == 0, "The applied request was not closed with its result and recovery note.");
                // Consumed: a later update and a reload of the same save (new loaded objects, same IDs) apply nothing.
                TestRunner.Equal(HorseTripRepairDecision.NoRequest, Run(host, session), "A consumed request was read again.");
                host.Loaded.Clear(); host.Loaded.Add(LegacyHorse("horse-1"));
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.NoRequest && host.Removals == 1, "A consumed request was re-applied after a reload.");
                TestRunner.Equal(stagedBefore, folders.StagedDigest(), "The guarded-style staged Mods folder changed.");
            }
            // No user-data folder: no request is read and nothing is created anywhere.
            using (var folders = new Folders(false))
            {
                var stagedBefore = folders.StagedDigest();
                var host = new Host(folders); host.Loaded.Add(LegacyHorse("horse-1"));
                TestRunner.True(Run(host, new HorseTripRepairSession()) == HorseTripRepairDecision.NoRequest && !Directory.Exists(folders.UserData) &&
                    host.Removals == 0 && folders.StagedDigest() == stagedBefore, "A missing user-data folder was created or read.");
            }
        }

        private static void NegativeCases()
        {
            using (var folders = new Folders())
            {
                var session = new HorseTripRepairSession();
                // No request: a legacy-ordered sourceless copy is only a candidate; nothing is removed.
                var host = new Host(folders);
                var legacy = LegacyHorse("horse-1");
                host.Loaded.Add(legacy);
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.NoRequest && host.Removals == 0 &&
                    Candidates(legacy).Count == 1 && legacy.Facts.Count == 4, "A candidate was removed without the owner's request.");
                // An independently granted copy in exactly the legacy visible order, recording its source: not a candidate, so
                // even an exact request for that horse is refused and changes nothing.
                var sourced = LegacyHorse("horse-3");
                sourced.Facts[0].Sourceless = false;
                host.Loaded.Add(sourced);
                folders.Request(HorseTripRepairPolicy.FormatRequestLine("horse-3"));
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.NoCandidate && host.Removals == 0 &&
                    sourced.Facts.Count == 4 && folders.UserFiles().Contains(Refused), "An independently granted (sourced) fact was removed.");
            }
            // Same GUID, different maneuver or category; a different GUID with the same shape: not candidates.
            foreach (var mutate in new Action<Fact>[] { fact => fact.BiteTripShape = false, fact => fact.StockIdentity = false })
                using (var folders = new Folders())
                {
                    var horse = LegacyHorse("horse-4"); mutate(horse.Facts[0]);
                    var h = new Host(folders); h.Loaded.Add(horse); folders.Request(HorseTripRepairPolicy.FormatRequestLine("horse-4"));
                    TestRunner.True(Run(h, new HorseTripRepairSession()) == HorseTripRepairDecision.NoCandidate && h.Removals == 0 && horse.Facts.Count == 4,
                        "A fact that is not the exact stock grant was removed.");
                }
            // A dog or wolf (not the KMC Horse blueprint) keeps its legitimate TrippingBite even if its ID is requested.
            using (var folders = new Folders())
            {
                var wolf = LegacyHorse("wolf-1"); wolf.ExactKmcHorse = false;
                var wh = new Host(folders); wh.Loaded.Add(wolf); folders.Request(HorseTripRepairPolicy.FormatRequestLine("wolf-1"));
                TestRunner.True(Run(wh, new HorseTripRepairSession()) == HorseTripRepairDecision.NoCandidate && wh.Removals == 0 && wolf.Facts.Count == 4,
                    "A wolf's or dog's TrippingBite was removed.");
            }
            // Two sourceless stock copies: the inherited one cannot be singled out; refused, nothing removed.
            using (var folders = new Folders())
            {
                var doubled = LegacyHorse("horse-5"); doubled.Facts.Add(StockGrant("TrippingBite (second)"));
                var dh = new Host(folders); dh.Loaded.Add(doubled); folders.Request(HorseTripRepairPolicy.FormatRequestLine("horse-5"));
                TestRunner.True(Run(dh, new HorseTripRepairSession()) == HorseTripRepairDecision.Ambiguous && dh.Removals == 0 && doubled.Facts.Count == 5 &&
                    folders.UserFiles().SequenceEqual(Files(Refused, ResultFile)), "An ambiguous horse was changed.");
            }
            // A malformed request is refused; nothing is removed.
            using (var folders = new Folders())
            {
                var mh = new Host(folders); var m = LegacyHorse("horse-6"); mh.Loaded.Add(m); folders.Request("please fix my horse");
                TestRunner.True(Run(mh, new HorseTripRepairSession()) == HorseTripRepairDecision.Malformed && mh.Removals == 0 && m.Facts.Count == 4 &&
                    folders.UserFiles().Contains(Refused), "A malformed request changed a horse.");
            }
        }

        private static void PendingAndRetry()
        {
            using (var folders = new Folders())
            {
                var host = new Host(folders);
                var session = new HorseTripRepairSession();
                var horse = LegacyHorse("horse-1");
                folders.Request(HorseTripRepairPolicy.FormatRequestLine("horse-1"));
                // A wrong horse ID (or that horse not loaded in the party): kept for later, nothing removed.
                host.Loaded.Add(LegacyHorse("horse-9"));
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.HorseNotLoaded && folders.UserFiles().SequenceEqual(Files(Request)) &&
                    host.Removals == 0, "A request for an unloaded horse was consumed or changed another horse.");
                host.Loaded.Add(horse);
                // The write gate closed (save, area transition, restoration, load): pending, the horse is not even read.
                TestRunner.True(Run(host, session, false) == HorseTripRepairDecision.Pending && folders.UserFiles().SequenceEqual(Files(Request)) &&
                    host.Removals == 0 && Candidates(horse).Count == 1, "A closed write gate let the repair run.");
                // A removal that throws while the fact is still present releases the claim; the retry applies it once.
                host.FailRemovalBefore = true;
                TestRunner.True(Throws(() => Run(host, session)) && folders.UserFiles().SequenceEqual(Files(Request)) && Candidates(horse).Count == 1 &&
                    session.PendingReports == 0, "A failure before removal consumed the request or changed the horse.");
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.Apply && host.Removals == 1 && Candidates(horse).Count == 0 &&
                    folders.UserFiles().SequenceEqual(Files(Applied, ResultFile)), "The retry did not apply exactly once.");
            }
        }

        private static void ReportingFailures()
        {
            // After RemoveFact: the fact is gone and the removal step throws; the outcome is applied, never a refusal.
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession(); var horse = LegacyHorse("horse-1"); host.Loaded.Add(horse);
                folders.Request(HorseTripRepairPolicy.FormatRequestLine("horse-1"));
                host.FailRemovalAfter = true;
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.Apply && host.Removals == 1 && Candidates(horse).Count == 0 &&
                    folders.UserFiles().SequenceEqual(Files(Applied, ResultFile)) && folders.Result().Contains("Applied"),
                    "A failure after RemoveFact was not finished as applied.");
            }
            // The claim rename fails: nothing changes, the request stays, the retry applies once.
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession(); var horse = LegacyHorse("horse-1"); host.Loaded.Add(horse);
                folders.Request(HorseTripRepairPolicy.FormatRequestLine("horse-1"));
                host.FailClaim = true;
                TestRunner.True(Throws(() => Run(host, session)) && host.Removals == 0 && Candidates(horse).Count == 1 &&
                    folders.UserFiles().SequenceEqual(Files(Request)), "A failed claim changed something.");
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.Apply && host.Removals == 1 &&
                    folders.UserFiles().SequenceEqual(Files(Applied, ResultFile)), "The retry after a failed claim did not apply once.");
            }
            // The result write fails after the removal: the claim stays; the retry finishes as applied with no second removal.
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession(); var horse = LegacyHorse("horse-1"); host.Loaded.Add(horse);
                folders.Request(HorseTripRepairPolicy.FormatRequestLine("horse-1"));
                host.FailResultWrite = true;
                TestRunner.True(Throws(() => Run(host, session)) && host.Removals == 1 && folders.UserFiles().SequenceEqual(Files(Claimed)) &&
                    session.PendingReports == 1, "A failed result write lost the claim or the applied record.");
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.FinishedApplied && host.Removals == 1 &&
                    folders.UserFiles().SequenceEqual(Files(Applied, ResultFile)) && folders.Result().Contains("Applied") && session.PendingReports == 0,
                    "The retry did not finish the applied report, or removed again.");
            }
            // The final rename fails after the result was written: the retry finishes as applied, never refused.
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession(); var horse = LegacyHorse("horse-1"); host.Loaded.Add(horse);
                folders.Request(HorseTripRepairPolicy.FormatRequestLine("horse-1"));
                host.FailClose = true;
                TestRunner.True(Throws(() => Run(host, session)) && host.Removals == 1 && folders.UserFiles().SequenceEqual(Files(Claimed, ResultFile)),
                    "A failed final rename lost the claim.");
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.FinishedApplied && host.Removals == 1 &&
                    folders.UserFiles().SequenceEqual(Files(Applied, ResultFile)) && !folders.Result().Contains("Refused"),
                    "The retry after a failed final rename did not finish as applied.");
            }
        }

        private static void StrandedClaims()
        {
            // Same session, reload of a backed-up save between the failure and the retry: the fact is back on a new horse object,
            // but this request already removed it once, so the retry only finishes reporting (single use).
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession(); host.Loaded.Add(LegacyHorse("horse-1"));
                folders.Request(HorseTripRepairPolicy.FormatRequestLine("horse-1"));
                host.FailResultWrite = true;
                TestRunner.True(Throws(() => Run(host, session)) && host.Removals == 1, "The first attempt did not remove once.");
                var reloaded = LegacyHorse("horse-1");
                host.Loaded.Clear(); host.Loaded.Add(reloaded);
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.FinishedApplied && host.Removals == 1 && Candidates(reloaded).Count == 1 &&
                    folders.UserFiles().SequenceEqual(Files(Applied, ResultFile)) && folders.Result().Contains("write a new request"),
                    "A reload after a stranded claim removed the fact a second time.");
                // A new request for the reloaded horse is an explicit new decision and applies once.
                folders.Request(HorseTripRepairPolicy.FormatRequestLine("horse-1"));
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.Apply && host.Removals == 2 && Candidates(reloaded).Count == 0,
                    "A new explicit request after the reload was not applied once.");
            }
            // A game restart mid-request (a claim with no record in this session): closed as interrupted, nothing removed.
            using (var folders = new Folders())
            {
                var host = new Host(folders); var horse = LegacyHorse("horse-1"); host.Loaded.Add(horse);
                File.WriteAllText(Path.Combine(folders.UserData, Claimed), HorseTripRepairPolicy.FormatRequestLine("horse-1"));
                TestRunner.True(Run(host, new HorseTripRepairSession()) == HorseTripRepairDecision.Interrupted && host.Removals == 0 &&
                    Candidates(horse).Count == 1 && folders.UserFiles().SequenceEqual(Files(Interrupted, ResultFile)) &&
                    folders.Result().Contains("Interrupted"), "A stranded claim from an earlier session removed something or was not closed.");
            }
        }
    }
}
