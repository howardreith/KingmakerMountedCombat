using System;
using Kingmaker.Assets.UI.LevelUp;
using Kingmaker.ElementsSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.FactLogic;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Integration
{
    /// <summary>
    /// Keeps the native AddPet spawn, ownership, area-load, rank, and upgrade
    /// contract while closing the one KMC-specific respec edge: stock AddPet
    /// clears the master link but intentionally leaves the spawned unit alive.
    /// </summary>
    internal sealed class HorseCompanionAddPet : AddPet
    {
        private bool deferredProgressionPending;
        private bool deferredProgressionFailed;
        private bool deferredProgressionFailureReported;
        private int deferredNativeAttempts;
        private int defaultBuildContextWaitFrames;

        internal bool ActivationDefaultBuildContextPresent { get; private set; }

        internal int ActivationCharacterLevelAfterNativeTry { get; private set; } = -1;

        internal bool DeferredProgressionPending => deferredProgressionPending;

        internal bool DeferredProgressionFailed => deferredProgressionFailed;

        internal int DeferredNativeAttempts => deferredNativeAttempts;

        internal int DefaultBuildContextWaitFrames => defaultBuildContextWaitFrames;

        internal bool LastDeferredDefaultBuildContextPresent { get; private set; }

        internal int DeferredCharacterLevelBefore { get; private set; } = -1;

        internal int DeferredCharacterLevelAfter { get; private set; } = -1;

        internal int ExpectedCharacterLevel
        {
            get
            {
                var rank = LevelRank == null ? 0 : Owner.GetFact(LevelRank)?.GetRank() ?? 0;
                return HorseCompanionProgressionPolicy.ExpectedCharacterLevel(rank);
            }
        }

        public override void OnFactActivate()
        {
            ActivationDefaultBuildContextPresent = HasDefaultBuildContext();
            base.OnFactActivate();
            ActivationCharacterLevelAfterNativeTry = SpawnedPet?.Descriptor?.Progression?.CharacterLevel ?? -1;
            ArmDeferredProgressionSynchronization();
        }

        public override void OnTurnOn()
        {
            base.OnTurnOn();
            ArmDeferredProgressionSynchronization();
        }

        internal void TryDeferredProgressionSynchronization()
        {
            if (!deferredProgressionPending) { return; }

            var spawned = SpawnedPet;
            var exactHorse = IsExactHorse(spawned);
            var exactOwnership = spawned != null &&
                                 spawned.Descriptor?.Master.Value == Owner.Unit &&
                                 Owner.Pet == spawned;
            var rank = LevelRank == null ? 0 : Owner.GetFact(LevelRank)?.GetRank() ?? 0;
            var currentLevel = spawned?.Descriptor?.Progression?.CharacterLevel ?? -1;
            var defaultBuildContextPresent = HasDefaultBuildContext();
            LastDeferredDefaultBuildContextPresent = defaultBuildContextPresent;

            if (defaultBuildContextPresent)
            {
                defaultBuildContextWaitFrames++;
                if (defaultBuildContextWaitFrames > HorseCompanionProgressionPolicy.MaximumDefaultBuildContextWaitFrames)
                {
                    deferredProgressionPending = false;
                    deferredProgressionFailed = true;
                }
                return;
            }

            if (!HorseCompanionProgressionPolicy.CanInvokeDeferredNativeUpdate(
                    exactHorse,
                    exactOwnership,
                    false,
                    rank,
                    currentLevel,
                    deferredNativeAttempts))
            {
                deferredProgressionPending = false;
                deferredProgressionFailed = !exactHorse ||
                                            !exactOwnership ||
                                            HorseCompanionProgressionPolicy.RequiresSynchronization(rank, currentLevel);
                return;
            }

            DeferredCharacterLevelBefore = currentLevel;
            deferredNativeAttempts++;
            try
            {
                // This is the exact native AddPet operation. The only KMC
                // behavior is deferring it beyond the activation call stack so
                // AddClassLevels cannot be diverted into DefaultBuildData plans.
                TryUpdatePet();
                DeferredCharacterLevelAfter = spawned.Descriptor.Progression.CharacterLevel;
                deferredProgressionPending = HorseCompanionProgressionPolicy.RequiresSynchronization(
                    rank,
                    DeferredCharacterLevelAfter);
                deferredProgressionFailed = deferredProgressionPending;
            }
            catch
            {
                deferredProgressionPending = false;
                deferredProgressionFailed = true;
                throw;
            }
        }

        internal bool TryMarkDeferredProgressionFailureReported()
        {
            if (!deferredProgressionFailed || deferredProgressionFailureReported) { return false; }
            deferredProgressionFailureReported = true;
            return true;
        }

        public override void OnFactDeactivate()
        {
            deferredProgressionPending = false;
            var spawned = SpawnedPet;
            base.OnFactDeactivate();

            if (spawned == null ||
                !IsExactHorse(spawned) ||
                spawned.Descriptor?.Master.Value != null)
            {
                return;
            }

            spawned.Destroy();
        }

        private void ArmDeferredProgressionSynchronization()
        {
            var spawned = SpawnedPet;
            if (!IsExactHorse(spawned))
            {
                deferredProgressionPending = false;
                return;
            }

            var rank = LevelRank == null ? 0 : Owner.GetFact(LevelRank)?.GetRank() ?? 0;
            var characterLevel = spawned.Descriptor.Progression.CharacterLevel;
            deferredProgressionPending = HorseCompanionProgressionPolicy.RequiresSynchronization(rank, characterLevel);
            deferredProgressionFailed = false;
            deferredProgressionFailureReported = false;
            deferredNativeAttempts = 0;
            defaultBuildContextWaitFrames = 0;
            LastDeferredDefaultBuildContextPresent = false;
            DeferredCharacterLevelBefore = characterLevel;
            DeferredCharacterLevelAfter = characterLevel;
        }

        private static bool HasDefaultBuildContext()
        {
            return ElementsContext.GetData<DefaultBuildData>() != null;
        }

        private static bool IsExactHorse(Kingmaker.EntitySystem.Entities.UnitEntityData unit)
        {
            return unit != null &&
                   string.Equals(unit.Blueprint?.AssetGuid, HorseCompanionBlueprintService.UnitGuid, StringComparison.Ordinal);
        }
    }
}
