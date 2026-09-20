using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.ElementsSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal static class Chunk4NativeAreaObservation
    {
        internal static JArray CaptureDefinition(BlueprintAbilityAreaEffect blueprint)
        {
            var remaining = 256;
            var result = new JArray();
            foreach (var component in blueprint.ComponentsArray.OfType<AbilityAreaEffectRunAction>())
            {
                result.Add(new JObject { ["type"] = component.GetType().FullName,
                    ["unitEnter"] = CaptureElement(component.UnitEnter, 0, ref remaining),
                    ["round"] = CaptureElement(component.Round, 0, ref remaining),
                    ["unitMove"] = CaptureElement(component.UnitMove, 0, ref remaining),
                    ["unitExit"] = CaptureElement(component.UnitExit, 0, ref remaining) });
            }
            return result;
        }

        private static JToken CaptureElement(object value, int depth, ref int remaining)
        {
            if (value == null) return JValue.CreateNull();
            if (--remaining < 0 || depth > 12) throw new InvalidOperationException("Area action metadata exceeds the bounded fixture inventory.");
            var blueprint = value as BlueprintScriptableObject;
            if (!ReferenceEquals(blueprint, null)) return new JObject { ["type"] = value.GetType().FullName, ["blueprint"] = blueprint.AssetGuid };
            if (value is bool || value is int || value is float || value is string) return JToken.FromObject(value);
            if (value.GetType().IsEnum) return new JValue(value.ToString());
            if (value is Array array)
            {
                var items = new JArray();
                foreach (var item in array) items.Add(CaptureElement(item, depth + 1, ref remaining));
                return items;
            }
            var result = new JObject { ["type"] = value.GetType().FullName };
            // Only the small action/condition metadata graph is followed. Unity
            // objects, private assets, textures and serialized stores are excluded.
            if (!(value is ActionList) && !(value is ConditionsChecker) && !(value is Element)) return result;
            // Native actions can declare "Type". Keep their fields separate from
            // our type discriminator for case-insensitive artifact consumers.
            var fields = new JObject();
            result["fields"] = fields;
            foreach (var field in value.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                var child = field.GetValue(value);
                if (child == null || child is ActionList || child is ConditionsChecker || child is Element || child is Array ||
                    child is BlueprintScriptableObject || child is bool || child is int || child is float || child is string || child.GetType().IsEnum)
                    fields[field.Name] = CaptureElement(child, depth + 1, ref remaining);
            }
            return result;
        }

        internal static JObject CaptureSaveSource(RuleSavingThrow rule)
        {
            var area = ElementsContext.GetData<AreaEffectContextData>()?.Entity;
            var callbacks = new JArray();
            foreach (var frame in new StackTrace(false).GetFrames() ?? new StackFrame[0])
            {
                var method = frame.GetMethod();
                if (method?.DeclaringType != typeof(AbilityAreaEffectRunAction)) continue;
                var token = method.MetadataToken;
                var kind = token == 0x06002CCD ? "unit-enter" : token == 0x06002CD0 ? "round" :
                    token == 0x06002CCF ? "unit-move" : token == 0x06002CCE ? "unit-exit" : "unknown";
                callbacks.Add(new JObject { ["kind"] = kind, ["token"] = token.ToString("x8"),
                    ["assemblyMvid"] = method.Module.ModuleVersionId.ToString() });
            }
            return new JObject { ["area"] = area?.UniqueId, ["areaBlueprint"] = area?.Blueprint.AssetGuid,
                ["caster"] = area?.Context.MaybeCaster?.UniqueId,
                ["sourceAbility"] = rule.Reason?.Context?.SourceAbility?.AssetGuid,
                ["contextBlueprint"] = rule.Reason?.Context?.AssociatedBlueprint?.AssetGuid,
                ["actorInside"] = area?.UnitsInside.Contains(rule.Initiator), ["callbacks"] = callbacks };
        }
    }
}
