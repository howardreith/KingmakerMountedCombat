using System;

namespace KingmakerMountedCombat.Domain
{
    public sealed class MountedPair
    {
        public MountedPair(string riderId, string mountId)
        {
            if (string.IsNullOrWhiteSpace(riderId))
            {
                throw new ArgumentException("Rider ID is required.", nameof(riderId));
            }

            if (string.IsNullOrWhiteSpace(mountId))
            {
                throw new ArgumentException("Mount ID is required.", nameof(mountId));
            }

            RiderId = riderId;
            MountId = mountId;
        }

        public string RiderId { get; }

        public string MountId { get; }
    }
}
