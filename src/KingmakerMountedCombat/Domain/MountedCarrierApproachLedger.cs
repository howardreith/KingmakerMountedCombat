using System.Runtime.CompilerServices;

namespace KingmakerMountedCombat.Domain
{
    // PR #70 R17-F1/R18-F2 (round 19): where an exact carrier stands in its own native approach lifecycle.
    public enum MountedCarrierApproachPhase
    {
        // Not a carrier the pair admitted (no record).
        NotAdmitted,
        // Admitted (the pair issued it to the mount's Move slot) and native has not issued a path request for it.
        AwaitingFirstRequest,
        // Native issued this carrier's request (its own PathTo) and that request is still the agent's outstanding request.
        RequestPending,
        // Native issued this carrier's request and ended it without delivering it (a failed or not-found completion, a request replaced by
        // another, or Stop clearing it in flight). Never a delivery.
        RequestUndelivered,
        // Native delivered this carrier's own request (OnPathComplete made it the agent's Path; a pooled Path object included) and the agent
        // still holds that delivery.
        Delivered,
        // Native delivered this carrier's own request and has since ended it: the agent no longer holds that delivery (Stop / CompleteMovement
        // cleared it, or another delivery replaced it) and no newer request of this carrier is outstanding.
        EndedByNative
    }

    // PR #70 R17-F1/R18-F2 (round 19): ONE lifecycle record per exact carrier, written only at the installed approach boundaries, replacing the
    // round-17/18 Move-slot markers (a positive tick and a non-null Path seen by the delegated movement hook, each credited to whatever command
    // held the Move slot). The pair admits each carrier it issues (Admit); the production hooks on UnitMovementAgent.PathTo (060018A3) record
    // the request THAT carrier issued (the exact command PathTo carries and the requested Path object, m_RequestedPath) and the hooks on
    // OnPathComplete (060018B9) record that request's delivery (native accepted it as the outstanding request and made it the agent's Path) or
    // its undelivered end. These facts are written when native issues and delivers, before any later movement tick, so neither native denial
    // route (the paired movement prefix returning to native before the delegated adapter, or the adapter's zero tick; both reach
    // CanMoveInTurnBased -> CompleteMovement) can erase them; and a carrier that merely holds the Move slot while a predecessor's path remains
    // acquires nothing, because that path answered no request of its own. The movement fact (MovedAlongDelivery) is credited only to the
    // carrier whose own delivered path the agent is steering on the positive tick (RecordMovement), never to the Move-slot occupant.
    // Engine-free (carriers, agents and paths are opaque objects); weakly keyed, so finished carriers and their agents leave nothing behind.
    public static class MountedCarrierApproachLedger
    {
        private sealed class Record
        {
            internal readonly object Agent;
            internal object RequestedPath, DeliveredPath;
            internal bool RequestOutstanding, EverDelivered, MovedAlongDelivery;
            internal int Requests, Deliveries;
            internal Record(object agent) { Agent = agent; }
        }

        // Per agent: the admitted carrier whose request is the agent's outstanding request, and the admitted carrier whose delivered path the
        // agent was last given (null once a path no admitted carrier requested, e.g. a forced path, replaced it).
        private sealed class AgentSlot
        {
            internal Record Pending, Holder;
        }

        private static readonly ConditionalWeakTable<object, Record> carriers = new ConditionalWeakTable<object, Record>();
        private static readonly ConditionalWeakTable<object, AgentSlot> agents = new ConditionalWeakTable<object, AgentSlot>();

        // The pair issued this carrier to the mount whose movement agent is given. Idempotent for one carrier.
        public static void Admit(object carrier, object agent)
        {
            if (carrier == null || agent == null) return;
            Record existing;
            if (carriers.TryGetValue(carrier, out existing)) return;
            carriers.Add(carrier, new Record(agent));
            agents.GetValue(agent, key => new AgentSlot());
        }

        // The cheap filter of both boundaries: only an agent some admitted carrier was issued to is observed.
        public static bool Tracks(object agent)
        {
            AgentSlot slot;
            return agent != null && agents.TryGetValue(agent, out slot);
        }

        // Installed PathTo issued a NEW request object for this agent on behalf of the command it carried.
        public static void RecordRequest(object agent, object command, object requestedPath)
        {
            AgentSlot slot;
            if (agent == null || requestedPath == null || !agents.TryGetValue(agent, out slot)) return;
            // set_RequestedPath replaced any earlier outstanding request: it can no longer be delivered (OnPathComplete ignores it).
            if (slot.Pending != null) slot.Pending.RequestOutstanding = false;
            slot.Pending = null;
            Record record;
            if (command == null || !carriers.TryGetValue(command, out record) || !ReferenceEquals(record.Agent, agent)) return;
            record.RequestedPath = requestedPath;
            record.RequestOutstanding = true;
            record.Requests++;
            slot.Pending = record;
        }

        // Installed OnPathComplete accepted this path as the agent's outstanding request (the only completion native acts on); delivered is
        // whether native made it the agent's Path.
        public static void RecordCompletion(object agent, object path, bool delivered)
        {
            AgentSlot slot;
            if (agent == null || path == null || !agents.TryGetValue(agent, out slot)) return;
            var pending = slot.Pending;
            slot.Pending = null;
            if (pending != null) pending.RequestOutstanding = false;
            if (pending != null && ReferenceEquals(pending.RequestedPath, path))
            {
                if (!delivered) return;
                pending.DeliveredPath = path;
                pending.EverDelivered = true;
                pending.Deliveries++;
                slot.Holder = pending;
                return;
            }
            // A path no admitted carrier requested now steers (a forced path, or another command's request).
            if (delivered) slot.Holder = null;
        }

        // The delegated movement hook returned a positive tick while the agent steered this path: credited to the carrier whose own delivered
        // path it is, if any.
        public static void RecordMovement(object agent, object agentPath)
        {
            AgentSlot slot;
            if (agent == null || agentPath == null || !agents.TryGetValue(agent, out slot)) return;
            var holder = slot.Holder;
            if (holder != null && ReferenceEquals(holder.DeliveredPath, agentPath)) holder.MovedAlongDelivery = true;
        }

        public static bool HasDelivery(object carrier)
        {
            Record record;
            return carrier != null && carriers.TryGetValue(carrier, out record) && record.EverDelivered;
        }

        public static bool MovedAlongDelivery(object carrier)
        {
            Record record;
            return carrier != null && carriers.TryGetValue(carrier, out record) && record.MovedAlongDelivery;
        }

        public static int RequestCount(object carrier)
        {
            Record record;
            return carrier != null && carriers.TryGetValue(carrier, out record) ? record.Requests : 0;
        }

        // The carrier's phase against the agent's current Path and outstanding request (m_RequestedPath).
        public static MountedCarrierApproachPhase PhaseOf(object carrier, object agentPath, object agentRequestedPath)
        {
            Record record;
            if (carrier == null || !carriers.TryGetValue(carrier, out record)) return MountedCarrierApproachPhase.NotAdmitted;
            AgentSlot slot;
            agents.TryGetValue(record.Agent, out slot);
            if (record.RequestOutstanding && slot != null && ReferenceEquals(slot.Pending, record) && ReferenceEquals(agentRequestedPath, record.RequestedPath))
                return MountedCarrierApproachPhase.RequestPending;
            if (record.EverDelivered)
                return slot != null && ReferenceEquals(slot.Holder, record) && ReferenceEquals(agentPath, record.DeliveredPath)
                    ? MountedCarrierApproachPhase.Delivered : MountedCarrierApproachPhase.EndedByNative;
            return record.Requests > 0 ? MountedCarrierApproachPhase.RequestUndelivered : MountedCarrierApproachPhase.AwaitingFirstRequest;
        }
    }
}
