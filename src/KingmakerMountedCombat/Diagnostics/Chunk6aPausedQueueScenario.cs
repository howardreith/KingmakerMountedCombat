using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        internal const string Chunk6aPausedQueueScenario = "chunk6a-paused-queue";
        private bool Chunk6aPausedQueueOnly => request.Scenario == Chunk6aPausedQueueScenario;
        private readonly JArray chunk6aPausedSamples = new JArray();
        private JObject chunk6aPausedBefore, chunk6aPausedEvidence;
        private UnitUseAbility chunk6aPausedCommand;
        private int chunk6aPausedTraceStart;
        private bool chunk6aPausedReleased;
        private void PrepareChunk6aPausedQueue()
        {
            if (!Chunk6aPausedQueueOnly) return;
            if (CombatController.IsInTurnBasedCombat()) throw new InvalidOperationException("Paused queue case requires real time.");
            Game.Instance.IsPaused = true;
            if (!Game.Instance.IsPaused) throw new InvalidOperationException("Native pause was not established before the command baseline.");
        }
        private void BeginChunk6aPausedHold()
        {
            if (!Chunk6aPausedQueueOnly) return;
            chunk6aPausedCommand = chunk6aCommandWindow.Command;
            if (chunk6aPausedCommand == null || !ReferenceEquals(chunk6aPausedCommand, lastNativeAbilityShell))
                throw new InvalidOperationException("Paused queue did not admit its exact one Mount command.");
            chunk6aPausedTraceStart = allocationTrace.EventCount;
            chunk6aPausedBefore = CaptureChunk6aPausedSample();
            chunk6aPausedSamples.Add(chunk6aPausedBefore.DeepClone());
            chunk6aPausedEvidence = new JObject { ["contract"] = NativePausedMountEvidence.Contract,
                ["samples"] = chunk6aPausedSamples, ["unpauseCount"] = 0, ["complete"] = false };
            observations["chunk6aPausedQueue"] = chunk6aPausedEvidence;
            NativePausedMountEvidence.AssertHeld(chunk6aPausedBefore, chunk6aPausedBefore);
        }
        private JObject CaptureChunk6aPausedSample()
        {
            var c = chunk6aPausedCommand;
            var id = nativeControls.CaptureRelationshipCommandIdentity(c);
            return new JObject { ["observationSequence"] = chunk6aCommandWindow.NextObservationSequence(), ["frame"] = Time.frameCount, ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
                ["paused"] = Game.Instance.IsPaused, ["turnBased"] = CombatController.IsInTurnBasedCombat(),
                ["allocationSequence"] = allocationTrace.EventCount, ["traceComplete"] = allocationTrace.Complete,
                ["commandObject"] = c == null ? 0 : RuntimeHelpers.GetHashCode(c),
                ["controlIdentity"] = id?.Control, ["casterId"] = id?.Caster, ["targetId"] = id?.Target,
                ["generationAtInit"] = id?.Generation, ["commandType"] = id?.Action, ["abilityGuid"] = id?.Ability,
                ["processObject"] = id?.Process == null ? 0 : RuntimeHelpers.GetHashCode(id.Process),
                ["contextObject"] = id?.Context == null ? 0 : RuntimeHelpers.GetHashCode(id.Context),
                ["inMoveSlot"] = c != null && ReferenceEquals(rider.Commands.GetCommand(UnitCommand.CommandType.Move), c),
                ["createdByPlayer"] = c?.CreatedByPlayer, ["started"] = c?.IsStarted, ["acted"] = c?.IsActed,
                ["finished"] = c?.IsFinished, ["state"] = CaptureChunk6aCausalState() };
        }
        private bool TickChunk6aPausedHold()
        {
            if (!Chunk6aPausedQueueOnly || chunk6aPausedReleased) return false;
            var sample = CaptureChunk6aPausedSample();
            observations["chunk6aPausedCurrent"] = sample.DeepClone();
            NativePausedMountEvidence.AssertHeld(chunk6aPausedBefore, sample);
            if ((int)sample["frame"] <= (int)((JObject)chunk6aPausedSamples.Last)["frame"]) return true;
            chunk6aPausedSamples.Add(sample);
            if (chunk6aPausedSamples.Count < 11) return true;
            var events = allocationTrace.EventsSince(chunk6aPausedTraceStart);
            NativePausedMountEvidence.AssertEvents(events, rider.UniqueId, horse.UniqueId);
            chunk6aPausedEvidence["events"] = events;
            chunk6aPausedEvidence["beforeUnpause"] = sample.DeepClone();
            Game.Instance.IsPaused = false;
            chunk6aPausedEvidence["afterUnpause"] = CaptureChunk6aPausedSample();
            chunk6aPausedEvidence["unpauseCount"] = 1;
            chunk6aPausedReleased = true;
            return true;
        }
        private void FinishChunk6aPausedQueue(JObject proof)
        {
            if (!Chunk6aPausedQueueOnly) return;
            NativePausedMountEvidence.AssertComplete(chunk6aPausedEvidence, proof);
            chunk6aPausedEvidence["complete"] = true;
            AddRow("CM06-paused-queue", (bool)proof["pass"],
                "One exact native Mount remained unstarted in its Move slot through ten paused frame boundaries with unchanged action/reaction resources and geometry; one unpause released the same command through exact terminal delivery.",
                new JObject { ["pausedQueue"] = chunk6aPausedEvidence.DeepClone(), ["commandProof"] = proof.DeepClone() });
        }
    }
    internal static class NativePausedMountEvidence
    {
        internal const string Contract = "one-native-rt-mount-held-paused-then-unpaused-once";
        internal static void AssertHeld(JObject first, JObject sample)
        {
            if(first == null || sample == null) throw new InvalidOperationException("Paused Mount observation missing.");
            // Pinned selected-ability commands leave CreatedByPlayer false. Native code
            // reads that field only for movement acceleration; the exact callback and
            // command identity establish input provenance. Never manufacture the flag.
            foreach(var s in new[]{first,sample})
            {
                if((bool?)s["paused"]!=true || (bool?)s["turnBased"]!=false || (bool?)s["traceComplete"]!=true ||
                    ((int?)s["commandObject"]).GetValueOrDefault()==0 || string.IsNullOrEmpty((string)s["controlIdentity"]) ||
                    string.IsNullOrEmpty((string)s["casterId"]) || string.IsNullOrEmpty((string)s["targetId"]) ||
                    (string)s["commandType"]!="Move" || (string)s["abilityGuid"]!="f053faad986631688defa003cd7bda0e" ||
                    (int?)s["processObject"]!=0 || (int?)s["contextObject"]!=0 || (bool?)s["inMoveSlot"]!=true ||
                    (bool?)s["createdByPlayer"]!=false || (bool?)s["started"]!=false || (bool?)s["acted"]!=false || (bool?)s["finished"]!=false)
                    throw new InvalidOperationException("Paused Mount started, acted, left its exact slot, or lost its observed identity.");
                if((string)s["state"]?["relationshipState"]!="Unmounted" || (long?)s["state"]?["generation"]!=(long?)s["generationAtInit"] ||
                    !(s["state"]?["selectedIds"] is JArray selected) || selected.Count!=1 || (string)selected[0]!=(string)s["casterId"])
                    throw new InvalidOperationException("Paused Mount changed selection or relationship.");
            }
            foreach(var name in new[]{"commandObject","controlIdentity","casterId","targetId","generationAtInit","commandType","abilityGuid","gameTicks"})
                if(first[name]==null || !JToken.DeepEquals(first[name],sample[name]))throw new InvalidOperationException("Paused Mount changed "+name);
            foreach(var actor in new[]{"rider","mount"})
                foreach(var field in new[]{"standard","move","swift","reactions","reactionCooldown","initiativeCooldown","initiativeOrder","reactionsPerRound","nativePrepareCount"})
                {
                    var a=first["state"]?[actor]?[field];var b=sample["state"]?[actor]?[field];
                    if(a==null || b==null || a.Type==JTokenType.Null || b.Type==JTokenType.Null ||
                        double.IsNaN((double)a) || double.IsInfinity((double)a) || !JToken.DeepEquals(a,b))
                        throw new InvalidOperationException("Paused Mount changed "+actor+" "+field);
                }
            if(!(first["state"]["ledger"] is JObject) || !(sample["state"]["ledger"] is JObject) || !JToken.DeepEquals(first["state"]["ledger"],sample["state"]["ledger"]))throw new InvalidOperationException("Paused Mount changed transition ledger.");
            foreach(var actor in new[]{"riderPosition","horsePosition"})
                foreach(var axis in new[]{"x","y","z"})
                {
                    var a=(double?)first["state"]?["geometry"]?[actor]?[axis];var b=(double?)sample["state"]?["geometry"]?[actor]?[axis];
                    if(a==null || b==null || double.IsNaN(a.Value) || double.IsInfinity(a.Value) || a!=b)throw new InvalidOperationException("Paused Mount moved an actor.");
                }
        }
        internal static void AssertEvents(JArray events,string rider,string mount)
        {
            if(events==null)throw new InvalidOperationException("Paused allocation event window missing.");
            foreach(var e in events.OfType<JObject>().Where(x=>(string)x["state"]?["actor"]==rider || (string)x["state"]?["actor"]==mount))
            {
                var b=(string)e["boundary"];
                if(b==null || b.Contains("cost") || b.Contains("prepare") || b.Contains("clear") || b.Contains("opportunity"))
                    throw new InvalidOperationException("Paused Mount observed a cost, grant, clear or reaction callback.");
            }
        }
        internal static void AssertComplete(JObject hold,JObject proof)
        {
            if(hold==null || (string)hold["contract"]!=Contract || (int?)hold["unpauseCount"]!=1 || (bool?)proof?["pass"]!=true ||
                !(hold["samples"] is JArray samples) || samples.Count!=11)throw new InvalidOperationException("Paused Mount hold or terminal proof incomplete.");
            var first=(JObject)samples[0];var previous=(int)first["frame"];
            var baseline=(JObject)first.DeepClone();baseline["state"]=proof["preClick"]?["state"]?.DeepClone();baseline["gameTicks"]=proof["preClick"]?["gameTicks"]?.DeepClone();
            AssertHeld(baseline,first);
            if((int?)proof["preClick"]?["frame"]>(int?)first["frame"] || (int?)proof["preClick"]?["allocationSequence"]>(int?)first["allocationSequence"])throw new InvalidOperationException("Paused admission precedes its baseline.");
            for(var i=0;i<samples.Count;i++){
                var s=(JObject)samples[i];AssertHeld(first,s);
                if(i>0 && (int)s["frame"]<=previous)throw new InvalidOperationException("Paused observations did not advance real frames.");previous=(int)s["frame"];
            }
            if(!JToken.DeepEquals(samples.Last,hold["beforeUnpause"]))throw new InvalidOperationException("Paused final boundary differs.");
            var released=(JObject)hold["afterUnpause"];
            if((bool?)released?["paused"]!=false)throw new InvalidOperationException("Native unpause did not occur.");
            var compare=(JObject)released.DeepClone();compare["paused"]=true;AssertHeld(first,compare);
            if((int?)released["frame"]!=(int?)hold["beforeUnpause"]?["frame"])throw new InvalidOperationException("Unpause was not observed synchronously.");
            foreach(var key in new[]{"commandObject","controlIdentity","casterId","targetId","generationAtInit","commandType","abilityGuid"})
                if(!JToken.DeepEquals(first[key],proof["identity"]?[key]))throw new InvalidOperationException("Unpaused terminal Mount is another request.");
            var events=hold["events"] as JArray;
            AssertEvents(events,(string)first["casterId"],(string)first["targetId"]);
            var sequence=(int)first["allocationSequence"];
            foreach(var e in events){if((int?)e["sequence"]!=++sequence)throw new InvalidOperationException("Paused event sequence differs.");}
            if(sequence!=(int)released["allocationSequence"])throw new InvalidOperationException("Paused event window did not close.");
            foreach(var s in samples){if((int)s["allocationSequence"]<(int)first["allocationSequence"] || (int)s["allocationSequence"]>sequence)throw new InvalidOperationException("Paused sample sequence outside window.");}
            var exactEvents=new JArray(proof["resourceWindow"]["events"].Where(e=>(int)e["sequence"]>(int)first["allocationSequence"] && (int)e["sequence"]<=sequence).Select(e=>e.DeepClone()));
            if(!JToken.DeepEquals(events,exactEvents))throw new InvalidOperationException("Paused allocation events differ from the command proof.");
            var admitted=proof["samples"].OfType<JObject>().Single(x=>(string)x["boundary"]=="click-admission");
            foreach(var key in new[]{"frame","gameTicks","allocationSequence"})if(!JToken.DeepEquals(first[key],admitted[key]))throw new InvalidOperationException("Paused hold was not captured at exact click admission.");
            // Unity can execute native command callbacks later in the same frame as
            // synchronous unpause. One observer-owned ordinal orders both callbacks and
            // held samples; frame equality alone neither proves nor refutes causality.
            long lastOrder = 0;
            foreach (var s in proof["samples"].OfType<JObject>())
            {
                var order = (long?)s["observationSequence"];
                if (!order.HasValue || order.Value <= lastOrder) throw new InvalidOperationException("Command observation order missing or nonmonotonic.");
                lastOrder = order.Value;
            }
            var preOrder = (long?)proof["preClick"]?["observationSequence"];
            var admissionOrder = (long?)admitted["observationSequence"];
            if (!preOrder.HasValue || preOrder <= 0 || !admissionOrder.HasValue || admissionOrder <= preOrder ||
                (bool?)proof["preClick"]?["paused"] != true || (bool?)admitted["paused"] != true)
                throw new InvalidOperationException("Paused admission order or pause observation missing.");
            lastOrder = admissionOrder.Value;
            foreach (var s in samples.OfType<JObject>())
            {
                var order = (long?)s["observationSequence"];
                if (!order.HasValue || order.Value <= lastOrder) throw new InvalidOperationException("Held observation order missing or nonmonotonic.");
                lastOrder = order.Value;
            }
            var releaseOrder = (long?)released["observationSequence"];
            if (!releaseOrder.HasValue || releaseOrder <= lastOrder) throw new InvalidOperationException("Unpause observation does not follow the held interval.");
            var approach=proof["samples"].OfType<JObject>().Single(x=>(string)x["boundary"]=="approach-start");
            if ((long?)approach["observationSequence"] <= releaseOrder || (bool?)approach["paused"] != false ||
                (int)approach["frame"] < (int)released["frame"] || (long)approach["gameTicks"] < (long)released["gameTicks"])
                throw new InvalidOperationException("Native approach was not observed after unpause.");
        }
    }
}
