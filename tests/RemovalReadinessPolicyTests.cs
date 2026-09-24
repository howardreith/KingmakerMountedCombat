using System;
using System.Collections.Generic;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // The removal preparation's closed rules: a settled world, an archive bound
    // to the operation that requested it, a clean KMC member, and no
    // KMC-registered identity anywhere in the archive. No game types.
    internal static class RemovalReadinessPolicyTests
    {
        private const string Campaign = "d41185be-edc1-47c0-b9d5-e1d7a9c8e65f";
        private const string Other = "c63b5e10-4db1-47d5-ae61-5c0788137a5d";
        private const string Cleanup = @"C:\saves\Manual_301_KMC_CLEANUP.zks";
        private static readonly string[] Tokens = { "4016c7db400ab721ff125aef9e65e202", "7db7c50677e39f09feef56f3831fc723", "f053faad986631688defa003cd7bda0e" };

        public static void Register(TestRunner runner)
        {
            runner.Run("removal: a settled out-of-combat world has no reason to refuse", SettledWorld);
            runner.Run("removal: party combat is refused even when the engine allows a save", CombatRefused);
            runner.Run("removal: an outstanding paired obligation is refused", ObligationRefused);
            runner.Run("removal: any pending save, load, hold or reset is refused", PendingRefused);
            runner.Run("removal: a failed inspection refuses instead of reading as clean", InspectionFailsClosed);
            runner.Run("removal: no loaded world is the only reason reported", NoWorld);
            runner.Run("removal: the requested archive binds when it is the one KMC committed once", Bound);
            runner.Run("removal: a same-named archive that KMC did not commit for this request is unconfirmed", SameNameUnrelated);
            runner.Run("removal: a superseded request whose commit landed elsewhere is unconfirmed", Superseded);
            runner.Run("removal: an unregistered, incomplete or foreign-campaign archive is unconfirmed", BindingRefusals);
            runner.Run("removal: a missing KMC member is clean", MemberMissing);
            runner.Run("removal: a current member with no pair, combat or binding is clean", MemberClean);
            runner.Run("removal: a member with combat participation is not clean", MemberCombat);
            runner.Run("removal: a member with control bindings or a pair is not clean", MemberBindingsOrPair);
            runner.Run("removal: a future, invalid or foreign-campaign member is not clean", MemberRefusals);
            runner.Run("removal: a KMC identity in any archive member is named with its member", ReferenceHit);
            runner.Run("removal: an archive with no KMC identity has no hit", ReferenceClean);
            runner.Run("removal: the scan refuses empty or short identity tokens", ReferenceTokens);
        }

        private static RemovalWorldFacts Settled() => new RemovalWorldFacts { WorldLoaded = true, DefaultMode = true, SaveAllowed = true };

        private static void SettledWorld()
        {
            TestRunner.Equal(0, RemovalReadinessPolicy.WorldReasons(Settled()).Count, "A settled world refused.");
        }

        private static void CombatRefused()
        {
            var facts = Settled(); facts.PartyInCombat = true;
            var reasons = RemovalReadinessPolicy.WorldReasons(facts);
            TestRunner.Equal(1, reasons.Count, "Combat produced a different reason count.");
            TestRunner.True(reasons[0].Contains("in combat"), "Combat reason not named.");
            facts = Settled(); facts.AnyPartyMemberInCombat = true;
            TestRunner.Equal(1, RemovalReadinessPolicy.WorldReasons(facts).Count, "A single member in combat was admitted.");
        }

        private static void ObligationRefused()
        {
            foreach (var setter in new Action<RemovalWorldFacts>[] { f => f.ActiveMountedCommand = true, f => f.StockAttackIntent = true, f => f.PairedActivation = true, f => f.CombatRestorationPending = true })
            {
                var facts = Settled(); setter(facts);
                TestRunner.True(RemovalReadinessPolicy.WorldReasons(facts).Count == 1, "An outstanding obligation was admitted.");
            }
        }

        private static void PendingRefused()
        {
            foreach (var setter in new Action<RemovalWorldFacts>[] { f => f.SaveSuspended = true, f => f.ActiveSaveScope = true, f => f.SaveDraining = true,
                f => f.LoadInFlight = true, f => f.LoadingWorld = true, f => f.LoadingProcess = true, f => f.WorldHoldReleasePending = true,
                f => f.ResetToMainMenuPending = true, f => f.DefaultMode = false, f => f.SaveAllowed = false })
            {
                var facts = Settled(); setter(facts);
                TestRunner.True(RemovalReadinessPolicy.WorldReasons(facts).Count == 1, "A pending operation was admitted.");
            }
        }

        private static void InspectionFailsClosed()
        {
            var facts = Settled(); facts.InspectionFailed = true; facts.InspectionFailure = "InvalidOperationException: boom";
            var reasons = RemovalReadinessPolicy.WorldReasons(facts);
            TestRunner.Equal(1, reasons.Count, "A failed inspection was admitted.");
            TestRunner.True(reasons[0].Contains("boom") && reasons[0].Contains("nothing was changed"), "Inspection failure not named.");
        }

        private static void NoWorld()
        {
            var facts = new RemovalWorldFacts { PartyInCombat = true, InspectionFailed = true };
            var reasons = RemovalReadinessPolicy.WorldReasons(facts);
            TestRunner.Equal(1, reasons.Count, "No-world reason count.");
            TestRunner.True(reasons[0].StartsWith("No loaded world"), "No-world reason not first.");
        }

        private static RemovalWrittenArchiveFacts Written() => new RemovalWrittenArchiveFacts
        {
            DescriptorRegistered = true, HasFileOnDisk = true, OperationState = "None", Path = Cleanup,
            CommittedDestination = Cleanup, CommitsSinceRequest = 1, Name = "KMC_CLEANUP", Type = "Manual", GameId = Campaign
        };

        private static void Bound()
        {
            TestRunner.Equal(null, RemovalReadinessPolicy.BindingReason(Written(), "KMC_CLEANUP", Campaign), "The requested archive did not bind.");
        }

        private static void SameNameUnrelated()
        {
            // A same-named archive already on disk, with no commit for this request.
            var facts = Written(); facts.CommitsSinceRequest = 0; facts.CommittedDestination = @"C:\saves\Manual_290_KMC_CLEANUP.zks";
            var reason = RemovalReadinessPolicy.BindingReason(facts, "KMC_CLEANUP", Campaign);
            TestRunner.True(reason != null && reason.Contains("0 archive commits"), "An uncommitted same-named archive bound.");
            facts = Written(); facts.CommittedDestination = @"C:\saves\Manual_290_KMC_CLEANUP.zks";
            reason = RemovalReadinessPolicy.BindingReason(facts, "KMC_CLEANUP", Campaign);
            TestRunner.True(reason != null && reason.Contains("not the requested archive"), "A commit elsewhere bound.");
        }

        private static void Superseded()
        {
            var facts = Written(); facts.CommitsSinceRequest = 2;
            var reason = RemovalReadinessPolicy.BindingReason(facts, "KMC_CLEANUP", Campaign);
            TestRunner.True(reason != null && reason.Contains("2 archive commits"), "A superseded request bound.");
        }

        private static void BindingRefusals()
        {
            var facts = Written(); facts.DescriptorRegistered = false;
            TestRunner.True(RemovalReadinessPolicy.BindingReason(facts, "KMC_CLEANUP", Campaign) != null, "Unregistered descriptor bound.");
            facts = Written(); facts.HasFileOnDisk = false;
            TestRunner.True(RemovalReadinessPolicy.BindingReason(facts, "KMC_CLEANUP", Campaign) != null, "Incomplete archive bound.");
            facts = Written(); facts.OperationState = "Saving";
            TestRunner.True(RemovalReadinessPolicy.BindingReason(facts, "KMC_CLEANUP", Campaign) != null, "In-operation archive bound.");
            facts = Written(); facts.GameId = Other;
            TestRunner.True(RemovalReadinessPolicy.BindingReason(facts, "KMC_CLEANUP", Campaign) != null, "Foreign campaign bound.");
            facts = Written(); facts.Name = "KMC_P01";
            TestRunner.True(RemovalReadinessPolicy.BindingReason(facts, "KMC_CLEANUP", Campaign) != null, "Other name bound.");
            facts = Written(); facts.Type = "Auto";
            TestRunner.True(RemovalReadinessPolicy.BindingReason(facts, "KMC_CLEANUP", Campaign) != null, "Auto save bound.");
            TestRunner.True(RemovalReadinessPolicy.BindingReason(Written(), "KMC_CLEANUP", null) != null, "Unknown expected campaign bound.");
        }

        private static RemovalMemberFacts Member() => new RemovalMemberFacts { Kind = "Current", CampaignId = Campaign };

        private static void MemberMissing()
        {
            TestRunner.Equal(0, RemovalReadinessPolicy.MemberReasons(new RemovalMemberFacts { Kind = "Missing" }, Campaign).Count, "A missing member was refused.");
        }

        private static void MemberClean()
        {
            TestRunner.Equal(0, RemovalReadinessPolicy.MemberReasons(Member(), Campaign).Count, "A clean member was refused.");
        }

        private static void MemberCombat()
        {
            var facts = Member(); facts.CombatPresent = true;
            var reasons = RemovalReadinessPolicy.MemberReasons(facts, Campaign);
            TestRunner.Equal(1, reasons.Count, "Combat supplement reason count.");
            TestRunner.True(reasons[0].Contains("combat participation"), "Combat supplement not named.");
        }

        private static void MemberBindingsOrPair()
        {
            var facts = Member(); facts.SlotCount = 2;
            TestRunner.True(RemovalReadinessPolicy.MemberReasons(facts, Campaign).Count == 1, "Bindings admitted.");
            facts = Member(); facts.Mounted = true; facts.RiderPresent = true; facts.MountPresent = true;
            TestRunner.True(RemovalReadinessPolicy.MemberReasons(facts, Campaign).Count == 1, "A pair admitted.");
            facts = Member(); facts.RiderPresent = true;
            TestRunner.True(RemovalReadinessPolicy.MemberReasons(facts, Campaign).Count == 1, "A lone rider admitted.");
        }

        private static void MemberRefusals()
        {
            foreach (var kind in new[] { "Future", "Invalid", null })
                TestRunner.True(RemovalReadinessPolicy.MemberReasons(new RemovalMemberFacts { Kind = kind, CampaignId = Campaign }, Campaign).Count == 1, "Unverifiable member admitted: " + kind);
            var facts = Member(); facts.CampaignId = Other;
            TestRunner.True(RemovalReadinessPolicy.MemberReasons(facts, Campaign).Count == 1, "Foreign member campaign admitted.");
        }

        private static void ReferenceHit()
        {
            var members = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("party.json", "{\"Units\":[{\"Blueprint\":\"native\"}]}"),
                new KeyValuePair<string, string>("9d1278a2f599b2a4daab53abdfe88d2e.json", "{\"m_Blueprint\":\"4016C7DB400AB721FF125AEF9E65E202\"}"),
                new KeyValuePair<string, string>("player.json", "{\"Facts\":[\"f053faad986631688defa003cd7bda0e\"]}")
            };
            var hits = RemovalReadinessPolicy.ReferenceHits(members, Tokens);
            TestRunner.Equal(2, hits.Count, "Hit count.");
            TestRunner.Equal("9d1278a2f599b2a4daab53abdfe88d2e.json:4016c7db400ab721ff125aef9e65e202", hits[0], "Stashed-area hit not named.");
            TestRunner.Equal("player.json:f053faad986631688defa003cd7bda0e", hits[1], "Ability hit not named.");
        }

        private static void ReferenceClean()
        {
            var members = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("party.json", "{\"Units\":[]}"), new KeyValuePair<string, string>("header.json", null) };
            TestRunner.Equal(0, RemovalReadinessPolicy.ReferenceHits(members, Tokens).Count, "A clean archive reported a hit.");
        }

        private static void ReferenceTokens()
        {
            var members = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("party.json", "{}") };
            foreach (var tokens in new[] { new string[0], new[] { "" }, new[] { "abc" } })
            {
                var thrown = false;
                try { RemovalReadinessPolicy.ReferenceHits(members, tokens); }
                catch (ArgumentException) { thrown = true; }
                TestRunner.True(thrown, "Degenerate tokens were accepted.");
            }
        }
    }
}
