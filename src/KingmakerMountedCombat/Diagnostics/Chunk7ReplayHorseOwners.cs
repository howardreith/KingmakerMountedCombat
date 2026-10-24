using System;
using System.Collections.Generic;
using System.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    // Native AddPet keeps one HorseCompanionAddPet ISceneHandler subscriber per owner of a KMC
    // Horse companion feature. Each saved pair is one owner with its own pet, so the saved pairs
    // whose profile mounts the KMC Horse blueprint stand in for those owners: Medium Horse and
    // Small Halfling Horse count, Mammoth and unknown profiles do not.
    internal static class Chunk7ReplayHorseOwners
    {
        internal static int Count(IEnumerable<string> profileIds, Func<string, string> mountBlueprint, string horseBlueprint)
        {
            if (profileIds == null) throw new ArgumentNullException(nameof(profileIds));
            if (mountBlueprint == null) throw new ArgumentNullException(nameof(mountBlueprint));
            if (string.IsNullOrEmpty(horseBlueprint)) throw new ArgumentException("Horse blueprint is required.", nameof(horseBlueprint));
            return profileIds.Count(id => string.Equals(mountBlueprint(id), horseBlueprint, StringComparison.Ordinal));
        }
    }
}
