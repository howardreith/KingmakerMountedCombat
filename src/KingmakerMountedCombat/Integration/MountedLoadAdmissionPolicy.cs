namespace KingmakerMountedCombat.Integration
{
    internal static class MountedLoadAdmissionPolicy
    {
        // Decide before native world disposal; never mutate metadata or grants.
        internal static string Rejection(MountedSaveReadResult read, string campaign, string area,
            bool enabled, bool paired, bool unified, bool scheduler, bool turnBased)
        {
            if (read == null) return "Mounted save metadata could not be read.";
            if (read.Kind == MountedSaveReadKind.Missing) return null;
            if (read.Kind == MountedSaveReadKind.Future)
                return "This save needs a newer mounted persistence version; its original data is retained.";
            if (read.Kind != MountedSaveReadKind.Current || read.Data == null)
                return "Mounted save metadata is invalid; the original archive is retained.";
            var data = read.Data;
            if (data.CampaignId != campaign || data.AreaId != area)
                return "Mounted metadata does not match the selected native campaign and area.";
            if (data.Mounted || data.Combat != null)
            {
                if (!enabled)
                    return "This save contains mounted participation. Enable the compatible mod before loading it.";
                if (!paired || unified || scheduler)
                    return "This save requires paired activation with both legacy turn authorities disabled.";
            }
            if (data.Combat != null && data.Combat.TurnBased != turnBased)
                return "The saved combat mode differs from the active mode. Select the saved mode before loading.";
            return null;
        }
    }
}
