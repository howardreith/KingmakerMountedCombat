using System;

namespace KingmakerMountedCombat.Domain
{
    public sealed class MountedPairCandidate
    {
        public MountedPairCandidate(string riderId, string mountId)
        {
            RiderId = riderId;
            MountId = mountId;
        }

        public string RiderId { get; }

        public string MountId { get; }

        public bool RiderIsDirectlyControllable { get; set; }

        public bool MountIsDirectlyControllable { get; set; }

        public bool RiderIsAliveAndConscious { get; set; }

        public bool MountIsAliveAndConscious { get; set; }

        public bool ExactReciprocalCompanionRelationship { get; set; }

        public bool RiderIsInCombat { get; set; }

        public bool MountIsInCombat { get; set; }

        public bool PartyIsInCombat { get; set; }

        public int RiderSizeOrdinal { get; set; }

        public int MountSizeOrdinal { get; set; }

        public bool RiderViewAndStockAgentAvailable { get; set; }

        public bool MountViewAndStockAgentAvailable { get; set; }

        public bool RiderStockAgentEnabled { get; set; }

        public bool MountStockAgentEnabled { get; set; }

        public bool RiderAgentOverrideAvailable { get; set; }

        public bool MountAgentOverrideAvailable { get; set; }

        public bool RiderIsExactlyMedium { get; set; }

        public bool DefaultGameMode { get; set; }

        public string Validate()
        {
            if (string.IsNullOrWhiteSpace(RiderId) || string.IsNullOrWhiteSpace(MountId))
            {
                return "Both stable unit IDs are required.";
            }

            if (string.Equals(RiderId, MountId, StringComparison.Ordinal))
            {
                return "Rider and mount must be distinct units.";
            }

            if (!RiderIsDirectlyControllable || !MountIsDirectlyControllable)
            {
                return "Both units must be directly controllable.";
            }

            if (!RiderIsAliveAndConscious || !MountIsAliveAndConscious)
            {
                return "Both units must be alive and conscious.";
            }

            if (!ExactReciprocalCompanionRelationship)
            {
                return "Mount must be the rider's exact active reciprocal companion.";
            }

            if (RiderIsInCombat || MountIsInCombat || PartyIsInCombat)
            {
                return "Phase 1 mounting is out-of-combat only.";
            }

            if (MountSizeOrdinal <= RiderSizeOrdinal)
            {
                return "Mount must be strictly larger than rider under current size rules.";
            }

            if (!RiderViewAndStockAgentAvailable || !MountViewAndStockAgentAvailable)
            {
                return "Both views and stock movement agents must be available.";
            }

            if (!RiderStockAgentEnabled || !MountStockAgentEnabled)
            {
                return "Both stock movement agents must be enabled before mounting.";
            }

            if (!RiderAgentOverrideAvailable)
            {
                return "Rider already owns an incompatible movement-agent override.";
            }

            if (!MountAgentOverrideAvailable)
            {
                return "Mount already owns an incompatible movement-agent override.";
            }

            if (!RiderIsExactlyMedium)
            {
                return "Phase 1 accepts exactly one Medium rider only.";
            }

            if (!DefaultGameMode)
            {
                return "Phase 1 mounting is available only in the ordinary Default game mode.";
            }

            return null;
        }
    }
}
