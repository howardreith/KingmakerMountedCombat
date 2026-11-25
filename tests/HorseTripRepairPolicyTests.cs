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
            runner.Run("R5-F1: a request replaced between observation and claim is judged by the claimed bytes (apply and refusal)", ReplacedBeforeClaim);
            runner.Run("R5-F1: a request written after the claim is a separate later request; a waiting claim yields to it unchanged", WrittenAfterClaim);
            runner.Run("R6-F4: a one-time read or lookup failure after the claim keeps this process's exact claim; the next poll applies it once", TransientFailureAfterClaim);
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
            public readonly HorseTripRepairFiles files;
            public int Removals;
            // Which horse each removal touched, in order (the integration logs the same horse ID at its removal).
            public readonly List<string> RemovedFrom = new List<string>();
            public bool FailRemovalBefore, FailRemovalAfter, FailClaim, FailResultWrite, FailClose;
            // One-time failures armed to fire only after a successful claim rename (review R6-F4).
            public bool FailClaimReadAfterClaim, FailLookupAfterClaim;
            private bool failNextClaimRead, failNextLookup;
            public readonly List<Horse> Loaded = new List<Horse>();
            private Horse current;

            public Host(Folders folders) { files = new HorseTripRepairFiles(folders.UserData); }

            public bool TryReadRequest(out string text) => files.TryReadRequest(out text);
            public bool TryReadClaim(out string text)
            {
                if (failNextClaimRead) { failNextClaimRead = false; throw new IOException("simulated read failure of the claimed request"); }
                return files.TryReadClaim(out text);
            }

            public IList<Fact> CandidatesOf(string horseId, out bool loaded)
            {
                if (failNextLookup) { failNextLookup = false; throw new InvalidOperationException("simulated horse lookup failure"); }
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
                RemovedFrom.Add(current.Id);
                if (FailRemovalAfter) { FailRemovalAfter = false; throw new InvalidOperationException("simulated failure after RemoveFact"); }
            }

            public void Claim()
            {
                if (FailClaim) { FailClaim = false; throw new IOException("simulated claim rename failure"); }
                files.Claim();
                if (FailClaimReadAfterClaim) { FailClaimReadAfterClaim = false; failNextClaimRead = true; }
                if (FailLookupAfterClaim) { FailLookupAfterClaim = false; failNextLookup = true; }
            }

            public bool ReleaseClaim() => files.ReleaseClaim();

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
        private static readonly string Superseded = HorseTripRepairPolicy.RequestFileName + HorseTripRepairPolicy.SupersededSuffix;

        private static string Archive(Folders folders, string name) => File.ReadAllText(Path.Combine(folders.UserData, name));

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

        // Review R5-F1 counterexample through the production policy and the production file host: the owner replaces a valid
        // request for horse A with one for horse B at the exact moment between the policy's observation and its claim rename.
        private static void ReplacedBeforeClaim()
        {
            var forA = HorseTripRepairPolicy.FormatRequestLine("horse-A") + "\r\n";
            var forB = "# replaced\r\n" + HorseTripRepairPolicy.FormatRequestLine("horse-B") + "\r\n";
            // Applicable: A observed, B claimed. Only B's fact is removed; archive, result and removal log all name B.
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession();
                var a = LegacyHorse("horse-A"); var b = LegacyHorse("horse-B"); host.Loaded.Add(a); host.Loaded.Add(b);
                folders.Request(forA);
                host.files.BeforeClaim = () => folders.Request(forB);
                TestRunner.Equal(HorseTripRepairDecision.Apply, Run(host, session), "The replaced request was not applied.");
                TestRunner.True(host.RemovedFrom.SequenceEqual(new[] { "horse-B" }) && Candidates(b).Count == 0 && Candidates(a).Count == 1 && a.Facts.Count == 4,
                    "The fact removed was not the one named by the claimed bytes: " + string.Join(",", host.RemovedFrom.ToArray()));
                TestRunner.True(folders.UserFiles().SequenceEqual(Files(Applied, ResultFile)) && Archive(folders, Applied) == forB &&
                    folders.Result().Contains("horse-B") && !folders.Result().Contains("horse-A"),
                    "The archive, result and removal disagree about which request was claimed.");
            }
            // Refusal: a valid A replaced by a malformed B before the claim. Nothing is removed; the refused archive is B's bytes.
            using (var folders = new Folders())
            {
                var host = new Host(folders); var a = LegacyHorse("horse-A"); host.Loaded.Add(a);
                folders.Request(forA);
                const string malformed = "horse=horse-A\r\nhorse=horse-B\r\n";
                host.files.BeforeClaim = () => folders.Request(malformed);
                TestRunner.True(Run(host, new HorseTripRepairSession()) == HorseTripRepairDecision.Malformed && host.Removals == 0 && Candidates(a).Count == 1,
                    "A valid observation overrode the malformed claimed bytes.");
                TestRunner.True(folders.UserFiles().SequenceEqual(Files(Refused, ResultFile)) && Archive(folders, Refused) == malformed &&
                    folders.Result().StartsWith(folders.Result().Substring(0, 0)) && folders.Result().Contains("Refused"),
                    "The refusal did not archive the claimed bytes.");
            }
            // Refusal for a horse without a candidate: B names a horse with only a sourced copy; refused quoting B, A untouched.
            using (var folders = new Folders())
            {
                var host = new Host(folders); var a = LegacyHorse("horse-A"); var b = LegacyHorse("horse-B"); b.Facts[0].Sourceless = false;
                host.Loaded.Add(a); host.Loaded.Add(b);
                folders.Request(forA);
                host.files.BeforeClaim = () => folders.Request(forB);
                TestRunner.True(Run(host, new HorseTripRepairSession()) == HorseTripRepairDecision.NoCandidate && host.Removals == 0 &&
                    Candidates(a).Count == 1 && Archive(folders, Refused) == forB && folders.Result().Contains("horse-B") && !folders.Result().Contains("horse-A"),
                    "A refusal of the claimed request acted on, or named, the observed one.");
            }
            // The reverse: a malformed observation replaced by a valid B is applied to B (the observation decides nothing).
            using (var folders = new Folders())
            {
                var host = new Host(folders); var b = LegacyHorse("horse-B"); host.Loaded.Add(b);
                folders.Request("please fix my horse");
                host.files.BeforeClaim = () => folders.Request(forB);
                TestRunner.True(Run(host, new HorseTripRepairSession()) == HorseTripRepairDecision.Apply && host.RemovedFrom.SequenceEqual(new[] { "horse-B" }) &&
                    Archive(folders, Applied) == forB, "A malformed observation refused a valid claimed request.");
            }
            // Control: no replacement; the request for A applies to A with A's bytes archived.
            using (var folders = new Folders())
            {
                var host = new Host(folders); var a = LegacyHorse("horse-A"); var b = LegacyHorse("horse-B"); host.Loaded.Add(a); host.Loaded.Add(b);
                folders.Request(forA);
                TestRunner.True(Run(host, new HorseTripRepairSession()) == HorseTripRepairDecision.Apply && host.RemovedFrom.SequenceEqual(new[] { "horse-A" }) &&
                    Candidates(b).Count == 1 && Archive(folders, Applied) == forA && folders.Result().Contains("horse-A"),
                    "The unreplaced control did not apply to its own horse.");
            }
            // A closed write gate claims nothing: the request (whatever its bytes) stays untouched.
            using (var folders = new Folders())
            {
                var host = new Host(folders); host.Loaded.Add(LegacyHorse("horse-A"));
                folders.Request(forA);
                var claims = 0; host.files.BeforeClaim = () => claims++;
                TestRunner.True(Run(host, new HorseTripRepairSession(), false) == HorseTripRepairDecision.Pending && claims == 0 &&
                    folders.UserFiles().SequenceEqual(Files(Request)) && host.Removals == 0, "A closed gate claimed or changed the request.");
            }
        }

        private static void WrittenAfterClaim()
        {
            var forA = HorseTripRepairPolicy.FormatRequestLine("horse-A");
            var forB = HorseTripRepairPolicy.FormatRequestLine("horse-B");
            // A request written right after the claim is not merged into the claimed one: A applies now, B on the next check.
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession();
                var a = LegacyHorse("horse-A"); var b = LegacyHorse("horse-B"); host.Loaded.Add(a); host.Loaded.Add(b);
                folders.Request(forA);
                host.files.AfterClaim = () => { folders.Request(forB); host.files.AfterClaim = null; };
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.Apply && host.RemovedFrom.SequenceEqual(new[] { "horse-A" }) &&
                    Archive(folders, Applied) == forA && folders.UserFiles().Contains(Request) && folders.Result().Contains("horse-A"),
                    "The claimed request was not processed on its own bytes, or the later request was lost.");
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.Apply && host.RemovedFrom.SequenceEqual(new[] { "horse-A", "horse-B" }) &&
                    Candidates(a).Count == 0 && Candidates(b).Count == 0 && folders.Result().Contains("horse-B") && !folders.UserFiles().Contains(Request),
                    "The later request was not processed separately.");
                var archives = folders.UserFiles().Where(name => name.EndsWith(HorseTripRepairPolicy.AppliedSuffix, StringComparison.Ordinal)).ToArray();
                TestRunner.True(archives.Length == 2 && archives.Select(name => Archive(folders, name)).OrderBy(x => x, StringComparer.Ordinal)
                    .SequenceEqual(new[] { forA, forB }), "The two applied archives do not hold exactly the two claimed requests.");
            }
            // A waiting claim (its horse not loaded) yields to a newer request written meanwhile: set aside unchanged as
            // superseded, the newer request kept and processed on its own; the waiting horse is never touched.
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession(); var b = LegacyHorse("horse-B"); host.Loaded.Add(b);
                folders.Request(forA);
                host.files.AfterClaim = () => { folders.Request(forB); host.files.AfterClaim = null; };
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.Superseded && host.Removals == 0 &&
                    folders.UserFiles().SequenceEqual(Files(Request, Superseded, ResultFile)) && Archive(folders, Superseded) == forA &&
                    Archive(folders, Request) == forB && folders.Result().Contains("horse-A") && folders.Result().Contains("Superseded"),
                    "A waiting claim overwrote, merged or dropped the newer request.");
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.Apply && host.RemovedFrom.SequenceEqual(new[] { "horse-B" }) &&
                    Archive(folders, Applied) == forB, "The newer request was not processed on its own after the superseded one.");
            }
            // Control: a waiting claim with no newer request returns to the request name, byte for byte, for a later update.
            using (var folders = new Folders())
            {
                var host = new Host(folders); host.Loaded.Add(LegacyHorse("horse-Z"));
                folders.Request(forA);
                TestRunner.True(Run(host, new HorseTripRepairSession()) == HorseTripRepairDecision.HorseNotLoaded && host.Removals == 0 &&
                    folders.UserFiles().SequenceEqual(Files(Request)) && Archive(folders, Request) == forA, "A waiting claim was not returned unchanged.");
            }
        }

        // Review R6-F4: a transient failure after the claim rename and before the removal (reading the claimed bytes, or the
        // horse lookup) keeps this process's exact claim; the next poll in the same session re-processes those bytes and applies
        // them once. Controls: a restarted session closes a claim it did not take as interrupted; a closed gate keeps the claim;
        // a newer request is still respected; post-removal recovery is unchanged (ReportingFailures, StrandedClaims).
        private static void TransientFailureAfterClaim()
        {
            var forA = "# from the log\r\n" + HorseTripRepairPolicy.FormatRequestLine("horse-A") + "\r\n";
            foreach (var failRead in new[] { true, false })
                using (var folders = new Folders())
                {
                    var host = new Host(folders); var session = new HorseTripRepairSession();
                    var a = LegacyHorse("horse-A"); var b = LegacyHorse("horse-B"); host.Loaded.Add(a); host.Loaded.Add(b);
                    folders.Request(forA);
                    if (failRead) host.FailClaimReadAfterClaim = true; else host.FailLookupAfterClaim = true;
                    var label = failRead ? "read" : "lookup";
                    TestRunner.True(Throws(() => Run(host, session)) && host.Removals == 0 && session.HoldsClaim &&
                        folders.UserFiles().SequenceEqual(Files(Claimed)) && Archive(folders, Claimed) == forA,
                        "A " + label + " failure after the claim lost or changed the exact claim.");
                    // A closed gate on the next poll keeps the claim untouched.
                    TestRunner.True(Run(host, session, false) == HorseTripRepairDecision.Pending && host.Removals == 0 &&
                        folders.UserFiles().SequenceEqual(Files(Claimed)), "A closed gate disposed of the retained claim (" + label + ").");
                    TestRunner.True(Run(host, session) == HorseTripRepairDecision.Apply && host.RemovedFrom.SequenceEqual(new[] { "horse-A" }) &&
                        Candidates(a).Count == 0 && Candidates(b).Count == 1 && folders.UserFiles().SequenceEqual(Files(Applied, ResultFile)) &&
                        Archive(folders, Applied) == forA && folders.Result().Contains("horse-A") && !folders.Result().Contains("Interrupted") &&
                        !session.HoldsClaim, "The retained claim was not applied once to its horse after a " + label + " failure.");
                    TestRunner.True(Run(host, session) == HorseTripRepairDecision.NoRequest && host.Removals == 1, "The applied claim was processed again (" + label + ").");
                }
            // Restarted-session control: the same stranded claim in a new process (no record of taking it) is interrupted, unchanged.
            using (var folders = new Folders())
            {
                var host = new Host(folders); var a = LegacyHorse("horse-A"); host.Loaded.Add(a);
                folders.Request(forA);
                host.FailLookupAfterClaim = true;
                TestRunner.True(Throws(() => Run(host, new HorseTripRepairSession())) && host.Removals == 0, "The lookup failure was not injected.");
                TestRunner.True(Run(host, new HorseTripRepairSession()) == HorseTripRepairDecision.Interrupted && host.Removals == 0 && Candidates(a).Count == 1 &&
                    folders.UserFiles().SequenceEqual(Files(Interrupted, ResultFile)) && Archive(folders, Interrupted) == forA,
                    "A claim not taken by this process was re-processed.");
            }
            // A newer request is respected: the retained claim's horse is not loaded on the next poll and the owner wrote a newer
            // request meanwhile, so the retained claim is set aside unchanged and the newer one is processed on its own.
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession(); var b = LegacyHorse("horse-B"); host.Loaded.Add(LegacyHorse("horse-A"));
                folders.Request(forA);
                host.FailClaimReadAfterClaim = true;
                TestRunner.True(Throws(() => Run(host, session)) && session.HoldsClaim, "The read failure was not injected.");
                var forB = HorseTripRepairPolicy.FormatRequestLine("horse-B");
                folders.Request(forB);
                host.Loaded.Clear(); host.Loaded.Add(b);
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.Superseded && host.Removals == 0 && Archive(folders, Superseded) == forA &&
                    Archive(folders, Request) == forB && !session.HoldsClaim, "The retained claim overrode a newer request.");
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.Apply && host.RemovedFrom.SequenceEqual(new[] { "horse-B" }) &&
                    Archive(folders, Applied) == forB, "The newer request was not processed on its own.");
            }
        }
    }
}
