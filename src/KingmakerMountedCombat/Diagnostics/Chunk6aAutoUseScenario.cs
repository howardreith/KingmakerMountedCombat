using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker;
using Kingmaker.UnitLogic.Abilities;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;
namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        internal static bool IsChunk6aAutoUseScenario(string scenario)=>scenario=="chunk6a-auto-use-mount-rt"||scenario=="chunk6a-auto-use-dismount-rt";
        private bool Chunk6aAutoUseOnly=>IsChunk6aAutoUseScenario(request.Scenario);
        private bool Chunk6aAutoUseDismount=>request.Scenario=="chunk6a-auto-use-dismount-rt";
        private NativeMountActionBarUiLease chunk6aAutoUseUi;
        private NativeAutoUseControlProbe chunk6aAutoUseProbe;
        private NativePassiveResourceProbe chunk6aAutoUseWait;
        private JObject chunk6aAutoUseEvidence;
        private bool chunk6aAutoUsePauseOwned,chunk6aAutoUseOriginalPause,chunk6aAutoUseInvoked;
        private const string Chunk6aAutoUseRow="CM06-ai-auto-use";
        private JObject CaptureChunk6aAutoUseBoundary()=>new JObject{
            ["frame"]=Time.frameCount,["gameTicks"]=Game.Instance.TimeController.GameTime.Ticks,["allocationSequence"]=allocationTrace.EventCount,
            ["paused"]=Game.Instance.IsPaused,["turnBased"]=CombatController.IsInTurnBasedCombat(),["state"]=CaptureChunk6aCausalState(),
            ["riderResources"]=allocationTrace.Snapshot(rider),["mountResources"]=allocationTrace.Snapshot(horse),
            ["pairIdle"]=Chunk6aIdle,["riderObject"]=RuntimeHelpers.GetHashCode(rider),["mountObject"]=RuntimeHelpers.GetHashCode(horse),
            ["targetId"]=target?.UniqueId,["targetObject"]=target==null?0:RuntimeHelpers.GetHashCode(target),
            ["targetInCombat"]=target?.IsInCombat==true,["hostileTarget"]=target!=null&&rider.IsEnemy(target)&&target.IsEnemy(rider)
        };
        private void BeginChunk6aAutoUse(JObject mountProof)
        {
            if(!Chunk6aAutoUseOnly||Chunk6aTurnBased||!Chunk6aIdle||chunk6aAutoUseEvidence!=null||
                relationship.State!=(Chunk6aAutoUseDismount?RelationshipState.Mounted:RelationshipState.Unmounted)||
                Chunk6aAutoUseDismount&&((bool?)mountProof?["pass"]!=true||chunk6aCommandWindow!=null)||
                !Chunk6aAutoUseDismount&&mountProof!=null)
                throw new InvalidOperationException("Auto-use case must own its exact fresh RT control state.");
            if(!EnsureChunk6aRiderSelection(Chunk6aAutoUseRow))return;
            chunk6aAutoUseOriginalPause=Game.Instance.IsPaused;
            chunk6aAutoUseEvidence=new JObject{["contract"]="independent-fresh-rt-native-auto-use-control-window",["scenario"]=request.Scenario,
                ["case"]=Chunk6aAutoUseDismount?"dismount":"mount",["beforeSetup"]=CaptureChunk6aAutoUseBoundary(),["originalPause"]=chunk6aAutoUseOriginalPause,
                ["mountProof"]=mountProof?.DeepClone()};
            observations["chunk6aAutoUse"]=chunk6aAutoUseEvidence;
            if(mountProof!=null){
                var bridge=NativeRelationshipTerminalBridge.Capture(allocationTrace,mountProof,(JObject)chunk6aAutoUseEvidence["beforeSetup"],false);
                chunk6aAutoUseEvidence["mountTerminalBridge"]=bridge;
                NativeTerminalBridgeEvidence.AssertComplete(mountProof,(JObject)chunk6aAutoUseEvidence["beforeSetup"],bridge,false,"Mounted");
            }
            chunk6aAutoUseWait=new NativePassiveResourceProbe(allocationTrace,rider,horse);
            chunk6aAutoUsePauseOwned=true;Game.Instance.IsPaused=true;
            if(!Game.Instance.IsPaused)throw new InvalidOperationException("Native pause refused before auto-use setup.");
            chunk6aStage=32;ResetLeafClock();
        }
        private void TickChunk6aAutoUse()
        {
            if(!Chunk6aAutoUseOnly||chunk6aAutoUseEvidence==null||chunk6aAutoUseInvoked||!chunk6aAutoUsePauseOwned||!Game.Instance.IsPaused||!Chunk6aIdle)
                throw new InvalidOperationException("Auto-use case lost its exact paused empty-command setup.");
            if(!EnsureChunk6aRiderSelection(Chunk6aAutoUseRow))return;
            if(!NativeMountActionBarUiLease.IsReady(rider))return;
            if(chunk6aAutoUseUi==null){chunk6aAutoUseUi=new NativeMountActionBarUiLease(rider);chunk6aAutoUseEvidence["ui"]=chunk6aAutoUseUi.Capture();return;}
            var blueprint=Chunk6aAutoUseDismount?nativeControls.DismountAbility:nativeControls.MountAbility;
            var slot=chunk6aAutoUseUi.FindControlSlot(blueprint.AssetGuid);
            chunk6aAutoUseEvidence["ui"]=chunk6aAutoUseUi.Capture();
            if(slot==null)return;
            // Read only: use one already owned native ability to execute both constructor observers.
            // This control is never initialized, admitted, cast, added to a fact list or put on a bar.
            var candidates=rider.Descriptor.Abilities.Enumerable.Select(f=>f.Data)
                .Concat(rider.Descriptor.Spellbooks.SelectMany(book=>book.GetAllKnownSpells()))
                .Where(a=>a?.Caster?.Unit==rider&&!nativeControls.IsPlayerOnlyRelationshipControl(a)).Distinct().ToArray();
            if(candidates.Length>256)throw new InvalidOperationException("Ordinary auto-use inventory exceeds bounded fixture scope.");
            chunk6aAutoUseEvidence["ordinaryCandidates"]=new JArray(candidates.Select(a=>new JObject{["object"]=RuntimeHelpers.GetHashCode(a),["guid"]=a.Blueprint.AssetGuid,["suitable"]=a.IsSuitableForAutoUse}));
            var ordinary=candidates.FirstOrDefault(a=>a.IsSuitableForAutoUse);
            if(ordinary==null)throw new InvalidOperationException("Native fixture has no owned ordinary eligible auto-use control; exact inventory retained.");
            var wait=chunk6aAutoUseWait.Finish();chunk6aAutoUseEvidence["setupResources"]=wait;
            chunk6aAutoUseWait.Dispose();chunk6aAutoUseWait=null;
            if((bool?)wait["pass"]!=true)throw new InvalidOperationException("Auto-use setup native resources failed: "+wait["failure"]);
            chunk6aAutoUseEvidence["beforeInput"]=CaptureChunk6aAutoUseBoundary();
            chunk6aAutoUseProbe=new NativeAutoUseControlProbe(nativeControls,allocationTrace,rider,horse,target,slot,ordinary,CaptureChunk6aCausalState);
            chunk6aAutoUseInvoked=true;
            try{chunk6aAutoUseEvidence["input"]=chunk6aAutoUseProbe.Invoke();}
            finally{chunk6aAutoUseEvidence["input"]=chunk6aAutoUseProbe.Capture();chunk6aAutoUseProbe.Dispose();chunk6aAutoUseProbe=null;}
            chunk6aAutoUseEvidence["afterInput"]=CaptureChunk6aAutoUseBoundary();
            RestoreChunk6aAutoUseUiAndPause();
            chunk6aAutoUseEvidence["afterRestoration"]=CaptureChunk6aAutoUseBoundary();
            chunk6aAutoUseEvidence["allocationTrace"]=allocationTrace.Capture();
            string failure=null;
            try{NativeAutoUseCaseEvidence.AssertComplete(chunk6aAutoUseEvidence);}catch(Exception error){failure=error.Message;}
            AddRow(Chunk6aAutoUseRow,failure==null,failure??"The exact native UI toggle and AI revalidation refused the relationship control; no command was admitted, no action/reaction resource changed, and all owned references and UI state restored.",chunk6aAutoUseEvidence);
            chunk6aStage=99;BeginCleanup();
        }
        private void RestoreChunk6aAutoUseUiAndPause()
        {
            Exception uiFailure=null,pauseFailure=null;
            if(chunk6aAutoUseUi!=null){
                try{chunk6aAutoUseUi.Dispose();}catch(Exception error){uiFailure=error;}
                finally{chunk6aAutoUseEvidence["ui"]=chunk6aAutoUseUi.Capture();chunk6aAutoUseUi=null;}
            }
            if(chunk6aAutoUsePauseOwned){
                try{
                    if(!Game.Instance.IsPaused)throw new InvalidOperationException("Auto-use pause lease changed outside its owner.");
                    Game.Instance.IsPaused=chunk6aAutoUseOriginalPause;chunk6aAutoUsePauseOwned=false;
                    chunk6aAutoUseEvidence["pauseRestored"]=Game.Instance.IsPaused==chunk6aAutoUseOriginalPause;
                }catch(Exception error){pauseFailure=error;}
            }
            if(uiFailure!=null&&pauseFailure!=null)throw new AggregateException("Auto-use UI and pause restoration failed.",uiFailure,pauseFailure);
            if(uiFailure!=null)throw uiFailure;if(pauseFailure!=null)throw pauseFailure;
        }
        private void CleanupChunk6aAutoUse()
        {
            if(chunk6aAutoUseProbe!=null){try{chunk6aAutoUseEvidence["inputAtCleanup"]=chunk6aAutoUseProbe.Capture();}finally{chunk6aAutoUseProbe.Dispose();chunk6aAutoUseProbe=null;}}
            if(chunk6aAutoUseWait!=null){try{chunk6aAutoUseEvidence["setupAtCleanup"]=chunk6aAutoUseWait.Capture();}finally{chunk6aAutoUseWait.Dispose();chunk6aAutoUseWait=null;}}
            RestoreChunk6aAutoUseUiAndPause();
        }
    }
}
