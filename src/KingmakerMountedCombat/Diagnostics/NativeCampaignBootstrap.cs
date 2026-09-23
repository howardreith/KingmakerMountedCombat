using System;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.Root;

namespace KingmakerMountedCombat.Diagnostics
{
    // The one disposable native new game a campaign-B run may start: resolved
    // from the engine's own authored start presets, never from a KMC-made
    // preset, character or campaign identity. Game.LoadNewGame 06000CDC takes
    // the preset's PlayerCharacter or BlueprintRoot.DefaultPlayerCharacter (no
    // character generation), mints Player.GameId with Guid.NewGuid, loads
    // preset.EnterPoint and, when the preset and the autosave setting allow,
    // queues the engine's own first autosave.
    internal sealed class NativeCampaignBootstrapPreset
    {
        internal BlueprintAreaPreset Preset;
        internal string Source;
        internal bool DlcEnabled;
        internal string Area;
        internal string EnterPointArea;
        internal bool MakeAutosave;
        internal bool CharGen;
        internal bool HasPlayerCharacter;
        internal int CompanionCount;
    }

    internal static class NativeCampaignBootstrap
    {
        internal const string AutosaveLeaf = "Auto_1.zks";
        internal const string ManualLeaf = "Manual_302_KMC_B.zks";
        internal const string ManualName = "KMC_B";

        // The stand-alone Beneath the Stolen Lands start is the smallest
        // authored new game, and it is used only when the installed license
        // enables it; otherwise the main campaign's own start preset.
        internal static NativeCampaignBootstrapPreset Resolve()
        {
            var root = BlueprintRoot.Instance;
            if (root == null) throw new InvalidOperationException("Native blueprint root is unavailable.");
            var endless = root.DlcSettings?.Get(DlcType.Endless);
            var dlcEnabled = endless != null && endless.Enabled && endless.StartGamePreset != null;
            var preset = dlcEnabled ? endless.StartGamePreset : root.NewGamePreset;
            if (preset == null || preset.EnterPoint == null || preset.Area == null)
                throw new InvalidOperationException("Native new game preset is incomplete.");
            var area = preset.Area.AssetGuidThreadSafe;
            var enterPointArea = preset.EnterPoint.Area?.AssetGuidThreadSafe;
            if (string.IsNullOrEmpty(area) || area != enterPointArea)
                throw new InvalidOperationException("Native new game preset area differs from its enter point area.");
            return new NativeCampaignBootstrapPreset
            {
                Preset = preset,
                Source = dlcEnabled ? "dlc-endless" : "main-campaign",
                DlcEnabled = endless != null && endless.Enabled,
                Area = area,
                EnterPointArea = enterPointArea,
                MakeAutosave = preset.MakeAutosave,
                CharGen = preset.CharGen,
                HasPlayerCharacter = preset.PlayerCharacter != null,
                CompanionCount = preset.Companions?.Count ?? 0
            };
        }
    }
}
