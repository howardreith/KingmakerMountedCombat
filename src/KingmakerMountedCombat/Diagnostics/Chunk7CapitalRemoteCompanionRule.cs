using System;
using System.Collections.Generic;
using System.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    // Matrix rows 58-59, batch197 r58: in the capital, native state also moves the unpaired story companions
    // to the remote party (inParty false, remote true, inGame true, controllable false) while the main pair
    // stays. This rule admits exactly that shape and nothing else. It reads recorded flags only, so the same
    // compiled rule runs offline over a real arrival roster. It never grants control or changes game state.
    internal static class Chunk7CapitalRemoteCompanionRule
    {
        internal const string RecordField = "admittedNativeRemoteCompanions";
        // The exact field-roster checks the admission replaces for an admitted actor, named and recorded.
        internal static readonly string[] SkippedChecks = { "field-party-membership", "field-character-membership", "not-remote-companion" };

        internal sealed class Member
        {
            internal string Id;
            internal bool InParty, Character, Remote, InGame, InState, DirectlyControllable, ViewPresent;
            // Roles from native and KMC records: the main character, any KMC pair (inherited custody, the loaded
            // archive's pair records, a live relationship or registry owner), and native pet links.
            internal bool MainCharacter, PairMember, Pet, HasPet;
        }

        // Roster: every producer-roster actor except the separately admitted out-of-scene pair B.
        // Returns the admitted ids in ordinal order; refuses, naming the reason, every other shape.
        internal static string[] Admit(bool capital, IList<Member> roster, IEnumerable<string> outOfScenePairIds, IEnumerable<string> currentFieldIds)
        {
            if (roster == null || outOfScenePairIds == null || currentFieldIds == null) throw new InvalidOperationException("Capital roster rule inputs are missing.");
            var pairB = outOfScenePairIds.ToArray();
            if (roster.Any(member => member == null || string.IsNullOrWhiteSpace(member.Id) || pairB.Contains(member.Id, StringComparer.Ordinal)) ||
                roster.Select(member => member.Id).Distinct(StringComparer.Ordinal).Count() != roster.Count)
                throw new InvalidOperationException("Capital roster rule actor identity is missing, repeated or the out-of-scene pair.");
            if (roster.Count(member => member.MainCharacter) != 1) throw new InvalidOperationException("Capital roster rule requires exactly one main character.");
            foreach (var id in currentFieldIds)
                if (!roster.Any(member => member.Id == id) && !pairB.Contains(id, StringComparer.Ordinal))
                    throw new InvalidOperationException("Capital native field roster gained an actor outside the producer roster: " + id);
            var admitted = new List<string>();
            foreach (var member in roster)
            {
                if (!member.Remote)
                {
                    // The exact field contract: still in the field, in the scene, not remote.
                    if (!member.InParty) throw new InvalidOperationException("Capital native field roster lost an actor that is not a native remote companion: " + member.Id);
                    if (!member.InGame) throw new InvalidOperationException("Capital field actor left the scene: " + member.Id);
                    continue;
                }
                if (!capital) throw new InvalidOperationException("Native remote companion outside a capital: " + member.Id);
                if (member.MainCharacter) throw new InvalidOperationException("The main character moved to the remote party: " + member.Id);
                if (member.PairMember) throw new InvalidOperationException("A KMC paired actor moved to the remote party: " + member.Id);
                if (member.Pet || member.HasPet) throw new InvalidOperationException("A remote companion carries a native pet link: " + member.Id);
                if (member.InParty || member.Character) throw new InvalidOperationException("A remote companion is still in the native field roster: " + member.Id);
                if (!member.InGame) throw new InvalidOperationException("A remote companion is out of the game: " + member.Id);
                if (member.DirectlyControllable) throw new InvalidOperationException("A remote companion is directly controllable: " + member.Id);
                admitted.Add(member.Id);
            }
            return admitted.OrderBy(id => id, StringComparer.Ordinal).ToArray();
        }
    }
}
