using System;
using System.Collections.Generic;

namespace KingmakerMountedCombat.Domain
{
    // Everything the Prepare-to-Disable / removal operation must establish before
    // KMC may be described as removable, decided from primitive facts the caller
    // reads. Nothing here touches the game, so every rule is testable without
    // it. The rules are deliberately closed: an unknown or failed inspection is
    // a refusal, never a clean result.
    public sealed class RemovalWorldFacts
    {
        public bool WorldLoaded { get; set; }
        public bool PartyInCombat { get; set; }
        public bool AnyPartyMemberInCombat { get; set; }
        public bool ActiveMountedCommand { get; set; }
        public bool StockAttackIntent { get; set; }
        public bool PairedActivation { get; set; }
        public bool CombatRestorationPending { get; set; }
        public bool LoadInFlight { get; set; }
        public bool LoadingWorld { get; set; }
        public bool LoadingProcess { get; set; }
        public bool SaveSuspended { get; set; }
        public bool ActiveSaveScope { get; set; }
        public bool SaveDraining { get; set; }
        public bool WorldHoldReleasePending { get; set; }
        public bool ResetToMainMenuPending { get; set; }
        public bool DefaultMode { get; set; }
        public bool SaveAllowed { get; set; }
        public bool InspectionFailed { get; set; }
        public string InspectionFailure { get; set; }
    }

    public sealed class RemovalMemberFacts
    {
        // "Missing", "Current", "Future" or "Invalid".
        public string Kind { get; set; }
        public bool Mounted { get; set; }
        public bool RiderPresent { get; set; }
        public bool MountPresent { get; set; }
        public bool CombatPresent { get; set; }
        public int SlotCount { get; set; }
        public string CampaignId { get; set; }
    }

    public sealed class RemovalWrittenArchiveFacts
    {
        public bool DescriptorRegistered { get; set; }
        public bool HasFileOnDisk { get; set; }
        public string OperationState { get; set; }
        public string Path { get; set; }
        public string CommittedDestination { get; set; }
        public int CommitsSinceRequest { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string GameId { get; set; }
    }

    public static class RemovalReadinessPolicy
    {
        // The world must be settled: out of combat with no paired obligation, no
        // save or load in any phase, in the default mode, and inspected.
        public static List<string> WorldReasons(RemovalWorldFacts facts)
        {
            if (facts == null) throw new ArgumentNullException(nameof(facts));
            var reasons = new List<string>();
            if (!facts.WorldLoaded) { reasons.Add("No loaded world: load the campaign first."); return reasons; }
            if (facts.PartyInCombat || facts.AnyPartyMemberInCombat)
                reasons.Add("The party is in combat; end the encounter first.");
            if (facts.ActiveMountedCommand || facts.StockAttackIntent || facts.PairedActivation)
                reasons.Add("A mounted command, attack intent or paired activation is still outstanding; let it finish first.");
            if (facts.CombatRestorationPending)
                reasons.Add("A loaded save's combat participation is still being restored; retry once it settles.");
            if (facts.SaveSuspended || facts.ActiveSaveScope || facts.SaveDraining)
                reasons.Add("A mounted save is still being written; retry once it finishes.");
            if (facts.LoadInFlight || facts.LoadingWorld || facts.LoadingProcess)
                reasons.Add("A save is still being loaded; retry once the area has finished loading.");
            if (facts.WorldHoldReleasePending || facts.ResetToMainMenuPending)
                reasons.Add("A world hold or main-menu reset is still pending; retry once it completes.");
            if (!facts.DefaultMode)
                reasons.Add("The game is not in its default mode (dialog, cutscene, rest, kingdom or pause); retry from ordinary play.");
            if (!facts.SaveAllowed)
                reasons.Add("Saving is not allowed right now; retry when the game allows a manual save.");
            if (facts.InspectionFailed)
                reasons.Add("The loaded world could not be inspected for KMC references (" + (facts.InspectionFailure ?? "unknown failure") +
                    "); removal cannot be established and nothing was changed.");
            return reasons;
        }

        // The written archive must be the one this operation requested: the same
        // registered descriptor, committed exactly once since the request at the
        // path KMC's own commit record names, complete, and of the requested
        // name, type and campaign. Anything else is unconfirmed, not ready.
        public static string BindingReason(RemovalWrittenArchiveFacts facts, string expectedName, string expectedGameId)
        {
            if (facts == null) throw new ArgumentNullException(nameof(facts));
            if (!facts.DescriptorRegistered) return "the engine's save list no longer holds the requested descriptor";
            if (!facts.HasFileOnDisk) return "the requested descriptor has no complete archive on disk";
            if (facts.OperationState != "None") return "the requested save is still in operation state " + facts.OperationState;
            if (string.IsNullOrEmpty(facts.Path)) return "the requested descriptor names no archive path";
            if (facts.CommitsSinceRequest != 1) return "KMC recorded " + facts.CommitsSinceRequest + " archive commits since the request instead of exactly one";
            if (!string.Equals(facts.CommittedDestination, facts.Path, StringComparison.Ordinal))
                return "KMC's last recorded commit (" + facts.CommittedDestination + ") is not the requested archive";
            if (facts.Name != expectedName) return "the written archive is named " + facts.Name + " instead of " + expectedName;
            if (facts.Type != "Manual") return "the written archive is a " + facts.Type + " save instead of a manual one";
            if (string.IsNullOrEmpty(expectedGameId) || facts.GameId != expectedGameId)
                return "the written archive belongs to campaign " + facts.GameId + " instead of " + expectedGameId;
            return null;
        }

        // The KMC member of a cleanup archive may be absent or must record no
        // pair, no combat supplement and no owned control binding, in the
        // campaign that requested it. A future or unreadable member is refused.
        public static List<string> MemberReasons(RemovalMemberFacts facts, string expectedCampaign)
        {
            if (facts == null) throw new ArgumentNullException(nameof(facts));
            var reasons = new List<string>();
            if (facts.Kind == "Missing") return reasons;
            if (facts.Kind != "Current") { reasons.Add("the cleanup archive's KMC member is " + (facts.Kind ?? "unknown") + " and cannot be verified"); return reasons; }
            if (facts.Mounted || facts.RiderPresent || facts.MountPresent) reasons.Add("the cleanup archive still records a mounted pair");
            if (facts.CombatPresent) reasons.Add("the cleanup archive still records combat participation that only KMC can restore");
            if (facts.SlotCount != 0) reasons.Add("the cleanup archive still records " + facts.SlotCount + " KMC control binding(s)");
            if (string.IsNullOrEmpty(expectedCampaign) || facts.CampaignId != expectedCampaign)
                reasons.Add("the cleanup archive's KMC member belongs to campaign " + facts.CampaignId + " instead of " + expectedCampaign);
            return reasons;
        }

        // Every archive member the caller read, searched for every KMC-registered
        // blueprint identity. A hit names the member and the identity, so the
        // refusal says exactly which dependency the archive still carries.
        public static List<string> ReferenceHits(IEnumerable<KeyValuePair<string, string>> members, IEnumerable<string> tokens)
        {
            if (members == null) throw new ArgumentNullException(nameof(members));
            if (tokens == null) throw new ArgumentNullException(nameof(tokens));
            var hits = new List<string>();
            var identities = new List<string>();
            foreach (var token in tokens)
            {
                if (string.IsNullOrEmpty(token) || token.Length < 8) throw new ArgumentException("A KMC blueprint identity token is too short to search for.");
                identities.Add(token);
            }
            if (identities.Count == 0) throw new ArgumentException("No KMC blueprint identities to search for.");
            foreach (var member in members)
            {
                if (member.Value == null) continue;
                foreach (var token in identities)
                    if (member.Value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0) hits.Add(member.Key + ":" + token);
            }
            return hits;
        }
    }
}
