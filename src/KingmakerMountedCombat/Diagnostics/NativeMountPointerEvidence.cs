using System;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace KingmakerMountedCombat.Diagnostics
{
    internal static class NativeMountPointerEvidence
    {
        internal static void AssertComplete(JObject input, JObject proof)
        {
            if ((string)input?["contract"] != "native-selected-ability-hover-prediction-and-ignore-click-before-one-commit" ||
                (bool?)input["clicked"] != true || (bool?)input["ready"] != true || (bool?)input["restored"] != true ||
                !(input["samples"] is JArray samples) || samples.Count < 5 || samples.Count > 128)
                throw new InvalidOperationException("Native Mount pointer admission/restoration incomplete.");
            NativePredictionCommandEvidence.AssertComplete((JObject)proof["predictionCommands"], proof);
            if (((JArray)proof["predictionCommands"]["commands"]).Count == 0)
                throw new InvalidOperationException("Native TB pointer observed no speculative Init.");
            var id = proof["identity"]; var first = samples[0]; var before = samples[samples.Count - 2]; var after = samples.Last;
            var admitted = samples[samples.Count - 3];
            if ((string)input["casterId"] != (string)id["casterId"] || (string)input["targetId"] != (string)id["targetId"] ||
                (string)id["abilityGuid"] != "f053faad986631688defa003cd7bda0e" ||
                (string)first["boundary"] != "before-set-ability" || (string)samples[1]["boundary"] != "after-set-ability" ||
                (string)before["boundary"] != "before-real-click" || (string)after["boundary"] != "after-real-click")
                throw new InvalidOperationException("Native Mount pointer order or actor differs.");
            foreach (var pair in new[] { new[] { "riderPosition", "riderPosition" }, new[] { "targetPosition", "horsePosition" } })
                for (var axis = 0; axis < 3; axis++)
                {
                    var measured = (double?)first[pair[0]]?[axis];
                    var baseline = (double?)proof["preClick"]?["state"]?["geometry"]?[pair[1]]?[new[] { "x", "y", "z" }[axis]];
                    if (measured == null || baseline == null || measured != baseline)
                        throw new InvalidOperationException("Native pointer differs from the post-selection geometry baseline.");
                }
            if ((int?)first["frame"] != (int?)proof["preClick"]["frame"] || (long?)first["gameTicks"] != (long?)proof["preClick"]["gameTicks"])
                throw new InvalidOperationException("Native pointer did not begin at the exact pre-click baseline.");
            var frame = (int)proof["preClick"]["frame"]; var ticks = (long)proof["preClick"]["gameTicks"];
            for (var index = 0; index < samples.Count; index++)
            {
                var s = samples[index];
                if ((int?)s["frame"] == null || (long?)s["gameTicks"] == null || (int)s["frame"] < frame || (long)s["gameTicks"] < ticks ||
                    (bool?)s["simulatingClick"] != false || (string)s["turnStatus"] != "Acting" ||
                    (string)s["turnActor"] != (string)id["casterId"] || (string)s["casterId"] != (string)id["casterId"] ||
                    (string)s["targetId"] != (string)id["targetId"] || (string)s["abilityGuid"] != (string)id["abilityGuid"] ||
                    !(s["selectedIds"] is JArray selected) || selected.Count != 1 || (string)selected[0] != (string)id["casterId"])
                    throw new InvalidOperationException("Native pointer lost exact selection, mode or time order.");
                frame = (int)s["frame"]; ticks = (long)s["gameTicks"];
                foreach (var field in new[] { "turnObject", "pointerObject", "handlerObject", "abilityObject" })
                    if ((int?)s[field] == null || (int)s[field] == 0 || !JToken.DeepEquals(s[field], first[field]))
                        throw new InvalidOperationException("Native pointer replaced " + field + ".");
                foreach (var actor in new[] { "riderPosition", "targetPosition" })
                {
                    if (!(s[actor] is JArray pos) || pos.Count != 3 || !JToken.DeepEquals(pos, first[actor]) ||
                        pos.Any(v => v.Type == JTokenType.Null || double.IsNaN((double)v) || double.IsInfinity((double)v)))
                        throw new InvalidOperationException("Native prediction moved an actor.");
                }
                if (index < samples.Count - 1 && ((bool?)s["riderCommandsEmpty"] != true || (int?)s["moveSlotObject"] != 0))
                    throw new InvalidOperationException("Native pointer had a real command before its one click.");
                if (index > 0 && index < samples.Count - 1 &&
                    (!JToken.DeepEquals(s["selectedHandlerObject"], s["handlerObject"]) || !JToken.DeepEquals(s["selectedAbilityObject"], s["abilityObject"])))
                    throw new InvalidOperationException("Native prediction selected another handler or ability.");
                if (index >= 2 && index < samples.Count - 2 &&
                    ((string)s["boundary"] != "native-admission" || (bool?)s["ignored"] != (index != samples.Count - 3)))
                    throw new InvalidOperationException("Native pointer bypassed IgnoreClick.");
            }
            var init = ((JArray)proof["samples"]).Single(s => (string)s["boundary"] == "init");
            if ((int?)first["selectedAbilityObject"] != 0 || (bool?)after["riderCommandsEmpty"] != false ||
                (int?)after["moveSlotObject"] != (int?)id["commandObject"] || (bool?)before["ignored"] != false ||
                (bool?)after["ignored"] != false || (int?)before["frame"] != (int?)admitted["frame"] ||
                (int?)after["frame"] != (int?)before["frame"] || (int?)init["frame"] != (int?)before["frame"] ||
                (long?)admitted["gameTicks"] != (long?)init["gameTicks"] || (long?)before["gameTicks"] != (long?)init["gameTicks"] ||
                (long?)after["gameTicks"] != (long?)init["gameTicks"])
                throw new InvalidOperationException("Native click did not commit its admitted frame and exact command.");
            var pathEvents = (JArray)proof["nativeApproachPath"]?["events"];
            var pathBefore = pathEvents?.Single(e => (string)e["boundary"] == "preview-before-click");
            var returned = pathEvents?.Single(e => (string)e["boundary"] == "preview-return");
            if (pathBefore == null || returned == null || (int?)pathBefore["frame"] != (int?)before["frame"])
                throw new InvalidOperationException("Native pointer lacks its synchronous path observation.");
            foreach (var path in new[] { admitted["preview"], before["preview"], pathBefore["path"], returned["path"] })
                if ((int?)path?["pathObject"] == null || (int)path["pathObject"] == 0 ||
                    (string)path["pathState"] != "Complete" || (bool?)path["pathError"] != false ||
                    !(path["points"] is JArray points) || points.Count == 0 ||
                    !JToken.DeepEquals(points, admitted["preview"]["points"]) ||
                    (int?)path["pathObject"] != (int?)admitted["preview"]["pathObject"])
                    throw new InvalidOperationException("Actual Mount consumed a different or incomplete admitted preview.");
            if ((int?)returned["turnObject"] != (int?)first["turnObject"])
                throw new InvalidOperationException("Native preview belongs to another turn.");
        }
    }
}
