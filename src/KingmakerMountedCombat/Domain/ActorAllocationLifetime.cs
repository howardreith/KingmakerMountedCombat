using System;
using System.Collections.Generic;

namespace KingmakerMountedCombat.Domain
{
    // A lifetime boundary for supplemental records, never an action grant.
    // The native adapter decides when an actor is destroyed or fully settled.
    public sealed class ActorAllocationLifetime<TActor, TRecord>
    {
        private readonly IDictionary<TActor, TRecord> records;
        private object session;
        private string campaign;
        private bool initialized;

        public ActorAllocationLifetime(IDictionary<TActor, TRecord> records)
        {
            this.records = records ?? throw new ArgumentNullException(nameof(records));
        }

        public int SynchronizeSession(object currentSession, string currentCampaign)
        {
            if (currentSession == null) return 0;
            var changed = initialized && (!ReferenceEquals(session, currentSession) ||
                !string.Equals(campaign, currentCampaign, StringComparison.Ordinal));
            var retired = changed ? records.Count : 0;
            if (changed) records.Clear();
            session = currentSession;
            campaign = currentCampaign;
            initialized = true;
            return retired;
        }

        public bool RetireDestroyedActor(TActor actor) => records.Remove(actor);

        public int RetireSettledActors(IEnumerable<TActor> actors)
        {
            if (actors == null) throw new ArgumentNullException(nameof(actors));
            var retired = 0;
            foreach (var actor in actors) if (records.Remove(actor)) retired++;
            return retired;
        }

        public int Clear()
        {
            var count = records.Count;
            records.Clear();
            session = null;
            campaign = null;
            initialized = false;
            return count;
        }
    }
}
