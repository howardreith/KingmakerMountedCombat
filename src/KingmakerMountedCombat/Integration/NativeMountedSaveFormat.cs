using System;

namespace KingmakerMountedCombat.Integration
{
    // Native support is selected independently of codec support. Schema 3 is
    // intentionally opaque here, even when its foundation codec calls it Current.
    internal static class NativeMountedSaveFormat
    {
        internal const int WriterSchema = 2;
        internal static MountedSaveReadResult Decode(string json) => MountedSaveCodec.Decode(json);
        internal static bool CanRestore(MountedSaveReadResult read) => read != null &&
            read.Kind == MountedSaveReadKind.Current && (read.SourceSchema == 1 || read.SourceSchema == 2) &&
            read.Data?.SchemaVersion == WriterSchema;

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
