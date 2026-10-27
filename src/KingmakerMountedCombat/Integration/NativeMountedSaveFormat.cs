using System;

namespace KingmakerMountedCombat.Integration
{
    // Codec success never selects native support. The historical schema-2 API
    // stays explicit; the complete archive adapter requires its separate world capability.
    internal static class NativeMountedSaveFormat
    {
        internal const int WriterSchema = 2;
        // Shared by native admission and the reviewed all-pair archive adapter.
        // Retained cleanup owners have their separate twelve-owner bound.
        internal const int NativePairCapacity = 6;
        // Enabled only with the reviewed all-pair world adapter, independently of decoding.
        internal static bool ArchiveNativeSupportEnabled { get; set; } = true;
        internal static int SelectedWriterSchema => ArchiveNativeSupportEnabled ? MountedArchiveData.Schema : WriterSchema;
        internal static MountedSaveReadResult Decode(string json) => MountedSaveCodec.Decode(json);
        internal static bool CanRestore(MountedSaveReadResult read) => read != null &&
            read.Kind == MountedSaveReadKind.Current && (read.SourceSchema == 1 || read.SourceSchema == 2) &&
            read.Data?.SchemaVersion == WriterSchema;

        internal static bool CanRestore(MountedArchiveReadResult read) => ArchiveNativeSupportEnabled && read != null &&
            read.Kind == MountedSaveReadKind.Current && read.Data?.SchemaVersion == MountedArchiveData.Schema &&
            read.Data.Pairs != null && read.Data.Pairs.Length <= NativePairCapacity &&
            (read.SourceSchema == 1 || read.SourceSchema == 2 || read.SourceSchema == MountedArchiveData.Schema);

        internal static string CaptureOrPreserve(MountedArchiveReadResult loaded, Func<MountedArchiveData> capture)
        {
            if (!ArchiveNativeSupportEnabled)
                throw new InvalidOperationException("All-pair native persistence has not been selected.");
            if (loaded != null && loaded.Kind != MountedSaveReadKind.Missing && !CanRestore(loaded))
            {
                if (loaded.OriginalJson == null)
                    throw new InvalidOperationException("Cannot preserve unreadable mounted metadata; source save remains intact.");
                return loaded.OriginalJson;
            }
            var snapshot = capture();
            if (snapshot?.Pairs == null || snapshot.Pairs.Length > NativePairCapacity)
                throw new InvalidOperationException("The mounted archive exceeds the selected native pair capacity.");
            return MountedArchiveCodec.Encode(snapshot);
        }

        internal static string Encode(MountedSaveData data)
        {
            if (data?.SchemaVersion != WriterSchema)
                throw new InvalidOperationException("Native writing is limited to schema 2.");
            return MountedSaveCodec.Encode(data);
        }

        internal static string CaptureOrPreserve(MountedSaveReadResult loaded, Func<MountedSaveData> capture)
        {
            if (loaded != null && loaded.Kind != MountedSaveReadKind.Missing && !CanRestore(loaded))
            {
                if (loaded.OriginalJson == null)
                    throw new InvalidOperationException("Cannot preserve unreadable mounted metadata; source save remains intact.");
                return loaded.OriginalJson;
            }
            return Encode(capture());
        }
    }
}
