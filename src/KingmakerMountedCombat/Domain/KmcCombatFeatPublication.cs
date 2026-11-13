using System;

namespace KingmakerMountedCombat.Domain
{
    // The ordered set of KMC combat feats appended to each shared feat-selection array by one coordinated lease.
    public static class KmcCombatFeatPublication
    {
        // Mounted Combat is required; Mounted Archery is appended after it only when it registered.
        public static T[] Items<T>(T mountedCombat, T mountedArchery) where T : class
        {
            if (mountedCombat == null) throw new ArgumentNullException(nameof(mountedCombat));
            if (mountedArchery == null) return new[] { mountedCombat };
            if (ReferenceEquals(mountedCombat, mountedArchery))
                throw new ArgumentException("Mounted Archery is a distinct feature.", nameof(mountedArchery));
            return new[] { mountedCombat, mountedArchery };
        }
    }
}
