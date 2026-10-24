using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Root;
using Kingmaker.Enums;
using Kingmaker.UnitLogic;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7ManualDerivedScenario
    {
        // Recheck the exact current template and native catalogue immediately
        // before the unchanged paid creation/choice pipeline may acquire custody.
        private bool NativeMediumBodyMatches()
        {
            var pins = selection.MediumBody;
            if (pins == null || selection.Case != Chunk7ManualDerivedSelection.PaidMediumCase) return false;
            var template = actors.SingleOrDefault(value => value.UniqueId == pins.TemplateActorId);
            var descriptor = template?.Descriptor; var doll = descriptor?.Doll;
            var nativeRaces = BlueprintRoot.Instance?.Progression?.CharacterRaces;
            if (template == null || !template.IsInState || !template.IsInGame || template.View?.AgentASP == null ||
                game.Player.PartyCharacters.Count(value => ReferenceEquals(value.Value, template)) != 1 ||
                descriptor == null || descriptor.OriginalSize != Size.Medium || descriptor.State.Size != Size.Medium ||
                template.GetActivePolymorph() != null || descriptor.Body?.IsPolymorphed != false || doll == null ||
                nativeRaces == null || nativeRaces.Length > 64) return false;
            var matches = nativeRaces.Where(value => value?.AssetGuid == pins.RaceGuid).ToArray();
            if (matches.Length != 1) return false;
            var race = matches[0]; var presets = race.Presets;
            if (race.Size != Size.Medium || !ReferenceEquals(descriptor.Progression.Race, race) || presets == null || presets.Length > 32) return false;
            var selectedPresets = presets.Where(value => value?.AssetGuid == pins.PresetGuid).ToArray();
            if (selectedPresets.Length != 1) return false;
            var preset = selectedPresets[0];
            var skeleton = pins.Gender == "Female" ? preset.FemaleSkeleton : preset.MaleSkeleton;
            var avatar = template.View.CharacterAvatar;
            return preset.RaceId == race.RaceId && ReferenceEquals(doll.RacePreset, preset) &&
                doll.Gender == (pins.Gender == "Female" ? Gender.Female : Gender.Male) && doll.LeftHanded == pins.LeftHanded &&
                skeleton != null && skeleton.name == pins.SkeletonName && avatar != null &&
                ReferenceEquals(avatar.Skeleton, skeleton) && template.View.Animator != null && avatar.Animator != null;
        }
    }
}
