using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Harmony12;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.TurnBasedMode;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.View;
using Newtonsoft.Json.Linq;
using Pathfinding;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Read-only ownership of the completed native TB preview and path consumed by one Mount.
    // No FindPath request contents are enumerated; no path is assigned, cleared, claimed or released.
    internal sealed class NativeMountApproachPathProbe : IDisposable
    {
        private const string HarmonyId = "KingmakerMountedCombat.Diagnostics.MountApproachPath";
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        private static NativeMountApproachPathProbe active;
        private readonly HarmonyInstance harmony;
        private readonly UnitEntityData rider, target;
        private readonly JArray events = new JArray(), hooks = new JArray(), errors = new JArray();
        private UnitUseAbility command;
        private int approachDepth;
        private bool disposed, approachObserved;
        internal NativeMountApproachPathProbe(UnitEntityData rider, UnitEntityData target)
        {
            if(active != null || rider?.View?.AgentASP == null || target?.View == null)
                throw new InvalidOperationException("Exact Mount path observation needs two live actors and exclusive observer ownership.");
            if(typeof(UnitCommand).Assembly.ManifestModule.ModuleVersionId != new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7"))
                throw new InvalidOperationException("Pinned native path module differs.");
            this.rider=rider;this.target=target;harmony=HarmonyInstance.Create(HarmonyId);active=this;
            try
            {
                Patch(typeof(UnitCommand),0x060027A6,"ApproachBefore","ApproachAfter");
                Patch(typeof(PathVisualizer),0x0600700F,null,"PreviewAfter");
                Patch(typeof(UnitMovementAgent),0x060018B6,"PrecomputedBefore","PrecomputedAfter");
                Patch(typeof(UnitMovementAgent),0x060018A3,"RequestedBefore",null);
            }
            catch { Dispose(); throw; }
        }
        internal void CaptureBeforeClick()
        {
            if(command != null || events.Count != 0)throw new InvalidOperationException("Pre-click path observation must precede this one command.");
            var preview=PathVisualizer.Instance;
            // The native getter returns the UI-owned completed path for the current actor.
            Record("preview-before-click",preview?.CurrentPathForUnit(rider.View),target.Position,preview);
        }
        internal void Bind(UnitUseAbility value)
        {
            if(command != null || value == null || value.Executor != rider || value.Target?.Unit != target ||
                value.Spell?.Blueprint?.AssetGuid != "f053faad986631688defa003cd7bda0e" || value.IsStarted || value.IsActed ||
                !ReferenceEquals(value,rider.Commands.GetCommand(UnitCommand.CommandType.Move)))
                throw new InvalidOperationException("Approach path probe requires the exact admitted unacted Mount.");
            command=value; Record("command-bound",null,value.ApproachPoint,null);
        }
        private static int Id(object obj)=>obj==null?0:RuntimeHelpers.GetHashCode(obj);
        private static JObject Point(Vector3 v)=>new JObject { ["x"]=v.x,["y"]=v.y,["z"]=v.z };
        internal static JObject SnapshotConsumedPath(Path path)
        {
            // Invoked only for synchronous completed preview returns or the caller-owned
            // precomputed path at FollowPrecomputedPath. Never for a worker-owned request.
            var points=path?.vectorPath;
            var copy=points==null?null:points.ToArray();
            double length=0;
            if(copy!=null)for(var i=1;i<copy.Length;i++) { var x=(double)copy[i].x-copy[i-1].x;var z=(double)copy[i].z-copy[i-1].z;length+=Math.Sqrt(x*x+z*z); }
            return new JObject { ["pathObject"]=Id(path),["pathType"]=path?.GetType().FullName,
                ["pathState"]=path?.CompleteState.ToString(),["pathError"]=path?.error,
                ["points"]=copy==null?null:new JArray(copy.Select(Point)),["horizontalLength"]=copy==null?JValue.CreateNull():new JValue(length) };
        }
        private void Record(string boundary,Path path,Vector3 destination,object receiver)
        {
            if(disposed)return;
            if(events.Count>=64){if(errors.Count==0)errors.Add("Approach path callback bound exceeded.");return;}
            var consumed=boundary=="preview-before-click"||boundary=="preview-return"||boundary=="precomputed-before"||boundary=="precomputed-after";
            var turn=Game.Instance.TurnBasedCombatController?.CurrentTurn;
            events.Add(new JObject { ["sequence"]=events.Count+1,["boundary"]=boundary,["frame"]=Time.frameCount,
                ["gameTicks"]=Game.Instance.TimeController.GameTime.Ticks,["commandObject"]=Id(command),
                ["moveSlotObject"]=Id(rider.Commands.GetCommand(UnitCommand.CommandType.Move)),["forcedPathObject"]=Id(command?.ForcedPath),
                ["casterId"]=rider.UniqueId,["targetId"]=target.UniqueId,["receiverObject"]=Id(receiver),
                ["approachDepth"]=approachDepth,["started"]=command?.IsStarted,["acted"]=command?.IsActed,["finished"]=command?.IsFinished,
                ["commandType"]=command?.Type.ToString(),["abilityGuid"]=command?.Spell?.Blueprint?.AssetGuid,
                ["processObject"]=Id(command?.ExecutionProcess),["contextObject"]=Id(command?.ExecutionProcess?.Context),
                ["turnObject"]=Id(turn),["turnActor"]=turn?.Unit?.UniqueId,["turnStatus"]=turn?.Status.ToString(),
                ["position"]=Point(rider.Position),["targetPosition"]=Point(target.Position),["destination"]=Point(destination),
                ["approachRadius"]=command?.ApproachRadius,["nextApproachTicks"]=command?.NextApproachTime.Ticks,
                ["path"]=consumed?SnapshotConsumedPath(path):null,["agentPathObject"]=Id(rider.View.AgentASP.Path),
                ["move"]=rider.CombatState.Cooldown.MoveAction,["reallyMoving"]=rider.View.AgentASP.IsReallyMoving });
        }
        private void Safe(Action callback){try{callback();}catch(Exception e){if(errors.Count==0)errors.Add(e.ToString());}}
        private bool OwnsApproach=>approachDepth==1&&command!=null&&ReferenceEquals(command,rider.Commands.GetCommand(UnitCommand.CommandType.Move));
        internal JObject Capture()=>new JObject { ["contract"]="exact-native-mount-preview-and-consumed-path-observation",
            ["commandObject"]=Id(command),["casterId"]=rider.UniqueId,["targetId"]=target.UniqueId,
            ["actorViewObject"]=Id(rider.View),["movementAgentObject"]=Id(rider.View.AgentASP),["pathVisualizerObject"]=Id(PathVisualizer.Instance),
            ["events"]=events.DeepClone(),["observerHooks"]=hooks.DeepClone(),["errors"]=errors.DeepClone(),
            ["complete"]=!disposed&&command!=null&&approachDepth==0&&errors.Count==0 };
        private void Patch(Type type,int token,string prefix,string postfix)
        {
            var m=type.GetMethods(Flags).Single(x=>x.MetadataToken==token);
            try
            {
                harmony.Patch(m,prefix==null?null:new HarmonyMethod(typeof(Hooks).GetMethod(prefix,Flags)){prioritiy=Priority.First},
                    postfix==null?null:new HarmonyMethod(typeof(Hooks).GetMethod(postfix,Flags)){prioritiy=Priority.Last});
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("Native Mount approach observer installation failed at " + m.DeclaringType.FullName + "." + m.Name +
                    " token=" + token.ToString("X8") + "; installed=" + hooks.ToString(Newtonsoft.Json.Formatting.None), exception);
            }
            hooks.Add(new JObject { ["method"]=m.DeclaringType.FullName+"."+m.Name,["token"]=token.ToString("X8"),
                ["moduleMvid"]=m.Module.ModuleVersionId.ToString(),["prefix"]=prefix,["postfix"]=postfix });
        }
        public void Dispose(){if(disposed)return;disposed=true;harmony.UnpatchAll(HarmonyId);if(ReferenceEquals(active,this))active=null;}
        private static class Hooks
        {
            internal static void ApproachBefore(UnitCommand __instance)
            {
                var p=active;if(p==null||!ReferenceEquals(__instance,p.command))return;
                p.Safe(()=>{p.approachDepth++;if(!p.approachObserved){p.Record("approach-before",null,__instance.ApproachPoint,__instance);p.approachObserved=true;}});
            }
            internal static void ApproachAfter(UnitCommand __instance)
            {var p=active;if(p==null||!ReferenceEquals(__instance,p.command))return;p.Safe(()=>{if(p.events.OfType<JObject>().Any(e=>(string)e["boundary"]=="precomputed-before")&&!p.events.OfType<JObject>().Any(e=>(string)e["boundary"]=="approach-after"))p.Record("approach-after",null,__instance.ApproachPoint,__instance);p.approachDepth--;});}
            internal static void PreviewAfter(PathVisualizer __instance,UnitEntityView unitView,Path __result)
            {var p=active;if(p==null||!p.OwnsApproach||unitView!=p.rider.View)return;p.Safe(()=>p.Record("preview-return",__result,p.command.ApproachPoint,__instance));}
            internal static void PrecomputedBefore(UnitMovementAgent __instance,Path p)
            {var owner=active;if(owner==null||!owner.OwnsApproach||__instance!=owner.rider.View.AgentASP)return;owner.Safe(()=>owner.Record("precomputed-before",p,owner.command.ApproachPoint,__instance));}
            internal static void PrecomputedAfter(UnitMovementAgent __instance,Path p)
            {var owner=active;if(owner==null||!owner.OwnsApproach||__instance!=owner.rider.View.AgentASP)return;owner.Safe(()=>owner.Record("precomputed-after",p,owner.command.ApproachPoint,__instance));}
            internal static void RequestedBefore(UnitMovementAgent __instance,UnitCommand command,Vector3 destination)
            {var p=active;if(p==null||!p.OwnsApproach||__instance!=p.rider.View.AgentASP||!ReferenceEquals(command,p.command))return;p.Safe(()=>p.Record("path-request-before",null,destination,__instance));}
        }
    }
}
