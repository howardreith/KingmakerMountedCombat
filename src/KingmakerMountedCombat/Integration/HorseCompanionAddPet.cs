using System;
using Kingmaker.UnitLogic.FactLogic;

namespace KingmakerMountedCombat.Integration
{
    /// <summary>
    /// Keeps the native AddPet spawn, ownership, area-load, rank, and upgrade
    /// contract while closing the one KMC-specific respec edge: stock AddPet
    /// clears the master link but intentionally leaves the spawned unit alive.
    /// </summary>
    internal sealed class HorseCompanionAddPet : AddPet
    {
        public override void OnFactDeactivate()
        {
            var spawned = SpawnedPet;
            base.OnFactDeactivate();

            if (spawned == null ||
                !string.Equals(spawned.Blueprint?.AssetGuid, HorseCompanionBlueprintService.UnitGuid, StringComparison.Ordinal) ||
                spawned.Descriptor?.Master.Value != null)
            {
                return;
            }

            spawned.Destroy();
        }
    }
}
