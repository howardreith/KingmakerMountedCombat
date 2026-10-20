using System;
using Kingmaker.EntitySystem.Persistence;
using KingmakerMountedCombat.Integration;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class RuntimePersistenceScenario
    {
        // A read-only view for this existing single-pair diagnostic. The actual
        // native schema3 graph remains recorded and independently compared to
        // the ZIP member. This view is never passed to a writer or restoration.
        internal sealed class RemovalArchiveObservation
        {
            internal const string ProjectionFormat = "schema3-single-pair-observation-v1";
            internal MountedSaveReadKind Kind => MountedSaveReadKind.Current;
            internal int SourceSchema => 3;
            internal MountedSaveData Data;
            internal MountedArchiveData NativeArchive;
        }

        private RemovalArchiveObservation ReadCurrentRemovalArchive(ISaver saver, bool mounted)
        {
            Check(RemovalCase && NativeMountedSaveFormat.ArchiveNativeSupportEnabled &&
                NativeMountedSaveFormat.SelectedWriterSchema == 3,
                "P07-current-removal-keeps-the-current-native-writer");
            return ValidateCurrentRemovalArchive(NativeMountedSaveStorage.ReadArchive(saver), mounted,
                (request.PersistenceLoad ?? request.Fixture.Working).GameId,
                (request.PersistenceLoad ?? request.Fixture.Working).Area, removalRiderId, removalMountId);
        }

        internal static RemovalArchiveObservation ValidateCurrentRemovalArchive(MountedArchiveReadResult read,
            bool mounted, string campaign, string area, string riderId, string mountId)
        {
            if (read == null || read.Kind != MountedSaveReadKind.Current || read.SourceSchema != 3 ||
                read.HistoricalData != null || read.Data == null || read.Data.SchemaVersion != 3)
                throw new InvalidOperationException("P07 removal requires its actual native schema3 readback.");
            var archive = read.Data;
            archive.Validate();
            if (archive.CampaignId != campaign || archive.AreaId != area || archive.Combat != null ||
                archive.Participations.Length != 0 || archive.Pairs.Length != (mounted ? 1 : 0) ||
                (!mounted && archive.Slots.Length != 0))
                throw new InvalidOperationException("P07 removal readback has foreign identity, combat or retained ownership.");
            var pair = mounted ? archive.Pairs[0] : null;
            if (mounted && (pair.ProfileId != "medium-humanoid-mammoth-v1" ||
                pair.Rider.Id != riderId || pair.Mount.Id != mountId))
                throw new InvalidOperationException("P07 positive removal opening is not its exact reference-free native Mammoth pair.");
            var view = new MountedSaveData {
                // Explicit observation projection for the old diagnostic's
                // debt/continuation comparisons, not a decoded schema2 input.
                SchemaVersion = MountedSaveData.CurrentSchema,
                CampaignId = archive.CampaignId, AreaId = archive.AreaId, GameTimeTicks = archive.GameTimeTicks,
                Policy = archive.Policy, RulesId = archive.RulesId, Mounted = mounted,
                ProfileId = pair?.ProfileId, Rider = pair?.Rider, Mount = pair?.Mount,
                Slots = archive.Slots, Combat = null };
            view.Validate();
            return new RemovalArchiveObservation { Data = view, NativeArchive = archive };
        }
    }
}
