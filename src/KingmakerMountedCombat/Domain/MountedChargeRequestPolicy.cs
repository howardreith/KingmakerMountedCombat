using System;

namespace KingmakerMountedCombat.Domain
{
    // How a request reaches a pair container while a charge owner exists. Native UnitCommands has three separate routes, and only
    // an admitted public Run can displace anything: it may still merge or queue, and a later gate can still refuse it.
    public enum MountedChargeRequestRoute
    {
        Run,
        Enqueue,
        QueuePromotion
    }

    public enum MountedChargeRequestDecision
    {
        Pass,
        Refuse,
        RefuseMountEnqueue,
        RefuseStarting,
        ReturnToQueue,
        DrainThenPass,
        DrainThenRefuse,
        DrainElseReturnToQueue
    }

    // No outcome of a live charge is destructive: cleanup belongs to the charge's own terminal, reached when native Run actually
    // displaces it or a deliberate Stop ends it. Only a charge already over may be drained here.
    public static class MountedChargeRequestPolicy
    {
        public static MountedChargeRequestDecision Decide(MountedChargeRequestRoute route, bool ownedRequest, bool chargeLive,
            bool attackWaitingInQueue, bool mountContainer, bool brainProposal, bool mountRunWouldQueue = false)
        {
            if (!chargeLive)
            {
                if (ownedRequest) return MountedChargeRequestDecision.DrainThenRefuse; // A retired shell or carrier may never resume.
                return route == MountedChargeRequestRoute.QueuePromotion
                    ? MountedChargeRequestDecision.DrainElseReturnToQueue
                    : MountedChargeRequestDecision.DrainThenPass;
            }

            if (ownedRequest) return MountedChargeRequestDecision.Pass;
            // Native removed a promotion from the queue before asking; refusing it would lose the order.
            if (route == MountedChargeRequestRoute.QueuePromotion) return MountedChargeRequestDecision.ReturnToQueue;
            if (brainProposal) return MountedChargeRequestDecision.Refuse;
            // Native Run clears the queue that holds the waiting attack, so nothing may race the shell's few frames.
            if (attackWaitingInQueue) return MountedChargeRequestDecision.RefuseStarting;
            // A carrier's own native Run clears the mount's queue, so an order accepted here would be dropped silently. A Run that
            // native would queue first marks the carrier to end, so it is the same request and is refused before that mark.
            if (mountContainer && (route == MountedChargeRequestRoute.Enqueue || mountRunWouldQueue)) return MountedChargeRequestDecision.RefuseMountEnqueue;
            return MountedChargeRequestDecision.Pass;
        }
    }

    // The forced lane's body rule. Pure over a navmesh probe, so the mount's width and the sampling are one tested decision.
    public static class MountedChargeLane
    {
        private const float FlankShare = 0.8f;

        // Both flanks up to the landing point, which is always sampled: that is the footprint the mount will occupy. Ground the mount
        // stands on now is not asked. flankClear(along, lateral) reads the navmesh on both sides, at that distance along the line.
        public static bool Clear(float total, float reach, float corpulence, Func<float, float, bool> flankClear)
        {
            var landing = total - reach;
            // A stop inside the body's own length stands on ground it already occupies. The native landing check still reads other units.
            if (landing <= corpulence) return true;
            var lateral = corpulence * FlankShare;
            var step = Math.Max(corpulence * 0.5f, 0.25f);
            for (var along = corpulence; along < landing; along += step)
                if (!flankClear(along, lateral)) return false;
            return flankClear(landing, lateral);
        }

        // A forced line is installed only here: asking this first is what keeps a re-force from running on an unread lane.
        public static bool MustAuthorizeReforce(bool carrierAlive, bool forceMode, bool pathPresent) =>
            carrierAlive && !(forceMode && pathPresent);
    }
}