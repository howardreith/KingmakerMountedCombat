using System;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace KingmakerMountedCombat.Diagnostics
{
    internal static class NativeRelationshipTerminalBridge
    {
        internal static JObject Capture(NativeActorAllocationTrace trace,JObject proof,JObject boundary,bool turnBased)
        {
            var terminal=proof["samples"].OfType<JObject>().Single(s=>(string)s["boundary"]=="terminal");
            var before=(int)terminal["allocationSequence"];var after=(int)boundary["allocationSequence"];
            if(after<before)throw new InvalidOperationException("Terminal bridge allocation sequence reversed.");
            return new JObject { ["contract"]="same-allocation-no-command-cost-with-observed-native-time-only",
                ["commandIdentity"]=proof["identity"].DeepClone(),["riderId"]=proof["identity"]["casterId"].DeepClone(),["mountId"]=proof["mountId"].DeepClone(),
                ["turnBased"]=turnBased,["traceComplete"]=trace.Complete,["observerHooks"]=trace.ObserverHooks,
                ["before"]=new JObject { ["frame"]=terminal["frame"].DeepClone(),["gameTicks"]=terminal["gameTicks"].DeepClone(),["allocationSequence"]=before,
                    ["rider"]=terminal["nativeAllocation"]["rider"].DeepClone(),["mount"]=terminal["nativeAllocation"]["mount"].DeepClone() },
                ["after"]=new JObject { ["frame"]=boundary["frame"].DeepClone(),["gameTicks"]=boundary["gameTicks"].DeepClone(),["allocationSequence"]=after,
                    ["rider"]=boundary["riderResources"].DeepClone(),["mount"]=boundary["mountResources"].DeepClone() },
                ["events"]=new JArray(trace.EventsSince(before).Take(after-before)) };
        }
    }
}
