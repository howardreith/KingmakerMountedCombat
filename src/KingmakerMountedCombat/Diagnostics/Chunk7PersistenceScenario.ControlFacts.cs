using System;
using System.Linq;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7PersistenceScenario
    {
        // Read native facts independently of retained hotbar templates and live
        // persistent bindings. A same-GUID foreign object is observed and cannot
        // hide behind the aggregate's registered-object identity comparison.
        private JObject CaptureOwnerSeedControlFacts()
        {
            Require(actors != null && actors.Length == 4 && actors.All(a => a?.Descriptor?.Abilities != null) &&
                actors.Select(a => a.UniqueId).Distinct(StringComparer.Ordinal).Count() == 4,
                "Native control facts require the four exact inherited actors.");
            var blueprints = new[] { controls.MountAbility, controls.DismountAbility, controls.RiderPrimaryAbility,
                controls.MountPrimaryAbility, controls.MountedChargeAbility };
            var guids = new[] { NativeMountedControlService.MountAbilityGuid, NativeMountedControlService.DismountAbilityGuid,
                NativeMountedControlService.RiderPrimaryAbilityGuid, NativeMountedControlService.MountPrimaryAbilityGuid,
                NativeMountedControlService.MountedChargeAbilityGuid };
            var rows = new JArray(); var total = 0; var active = 0;
            foreach (var actor in actors)
                for (var index = 0; index < blueprints.Length; index++)
                {
                    var blueprint = blueprints[index]; var guid = guids[index];
                    var facts = actor.Descriptor.Abilities.Enumerable.Where(fact => fact != null &&
                        (ReferenceEquals(fact.Blueprint, blueprint) || fact.Blueprint?.AssetGuid == guid ||
                         ReferenceEquals(fact.Data?.Blueprint, blueprint) || fact.Data?.Blueprint?.AssetGuid == guid)).ToArray();
                    var current = blueprint == null ? null : actor.Descriptor.Abilities.GetAbility(blueprint);
                    var count = facts.Count(fact => fact.Active); total += facts.Length; active += count;
                    rows.Add(new JObject { ["actorId"] = actor.UniqueId, ["kind"] = index + 1,
                        ["blueprintGuid"] = blueprint?.AssetGuid, ["totalFacts"] = facts.Length, ["activeFacts"] = count,
                        ["facts"] = new JArray(facts.Select(fact => new JObject { ["active"] = fact.Active,
                            ["factBlueprintExact"] = blueprint != null && ReferenceEquals(fact.Blueprint, blueprint),
                            ["dataPresent"] = fact.Data != null,
                            ["dataBlueprintExact"] = blueprint != null && ReferenceEquals(fact.Data?.Blueprint, blueprint),
                            ["casterExact"] = ReferenceEquals(fact.Data?.Caster?.Unit, actor),
                            ["nativeLookupExact"] = ReferenceEquals(current, fact) && ReferenceEquals(current?.Data, fact.Data) })) });
                }
            return new JObject { ["contract"] = "owner-seed-control-facts-v1", ["rows"] = rows,
                ["totalFacts"] = total, ["activeFacts"] = active };
        }
    }
}
