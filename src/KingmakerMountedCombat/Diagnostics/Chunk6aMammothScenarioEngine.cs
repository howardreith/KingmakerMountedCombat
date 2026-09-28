using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using KingmakerMountedCombat.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;

namespace KingmakerMountedCombat.Diagnostics
{
    // Isolated diagnostic outer runner for the existing native Mammoth companion; no companion swap or spawn.
    internal sealed class Chunk6aMammothScenarioEngine : IDisposable
    {
        internal const string RealTimeScenario = "chunk6a-mammoth-mount-rt";
        internal const string TurnBasedScenario = "chunk6a-mammoth-mount-tb";
        internal const string EvidenceFileName = "chunk6a-native-mammoth-profile.json";
        internal const string EvidenceKind = "chunk6a-native-mammoth-profile";
        private readonly RuntimeRequest request;
        private readonly GameMountedRelationshipService relationship;
        private readonly MountedPlayerActionController playerAction;
        private readonly MountedCombatController combat;
        private readonly NativeMountedControlService nativeControls;
        private readonly HorseCompanionBlueprintService horseService;
        private readonly DiagnosticSettings settings;
        private readonly IModLogger logger;
        private readonly List<RuntimeSubscenarioResult> results = new List<RuntimeSubscenarioResult>();
        private readonly List<string> errors = new List<string>();
        private readonly JObject evidence = new JObject();
        private Phase3dHorseScenarioTranche tranche;
        private UnitEntityData rider, mount;
        private UnitEntityData[] originalSelection;
        private IDisposable ummLease;
        private bool started, completed, disposed, capturedIntake;
        private bool originalPause, originalTurnBased, originalPaired;
        private JObject before;
        internal Chunk6aMammothScenarioEngine(RuntimeRequest request, GameMountedRelationshipService relationship,
            MountedPlayerActionController playerAction, MountedCombatController combat, NativeMountedControlService controls,
            HorseCompanionBlueprintService horseService, DiagnosticSettings settings, IModLogger logger)
        {
            this.request=request;this.relationship=relationship;this.playerAction=playerAction;this.combat=combat;
            nativeControls=controls;this.horseService=horseService;this.settings=settings;this.logger=logger;
        }
        internal static bool SupportsScenario(string scenario) => scenario==RealTimeScenario || scenario==TurnBasedScenario;
        internal bool IsCompleted => completed;
        internal IReadOnlyList<RuntimeSubscenarioResult> Results => results;
        internal IReadOnlyList<string> Errors => errors;
        internal void ObserveNativeTurnBasedCommandEligibility(UnitCommand command, bool eligible) =>
            tranche?.ObserveNativeTurnBasedCommandEligibility(command,eligible);
        internal void Start()
        {
            if(started || disposed || !SupportsScenario(request.Scenario))throw new InvalidOperationException("Native Mammoth runner admission differs.");
            started=true;
            try
            {
                var game=Game.Instance;var selection=SelectionManager.Instance;
                if(game?.Player==null || selection==null)throw new InvalidOperationException("Loaded native fixture or selection unavailable.");
                originalPause=game.IsPaused;originalTurnBased=CombatController.IsInTurnBasedCombat();originalPaired=settings.EnablePairedActivation;
                originalSelection=selection.SelectedUnits.Where(x=>x!=null).ToArray();capturedIntake=true;
                string refusal;
                if(!relationship.TryResolveAutomationPair(SupportedMountedProfiles.MammothBlueprintGuid,out rider,out mount,out refusal))
                    throw new InvalidOperationException("Exact original Mammoth pair refused: "+refusal);
                before=CapturePair();
                evidence["before"]=before.DeepClone();
                if(!SamePair(before,before))throw new InvalidOperationException("Native Mammoth pair observation is incomplete or not reciprocal.");
                if(relationship.State!=RelationshipState.Unmounted || game.Player.IsInCombat || rider.Group.Any(x=>x.IsInCombat || !x.Commands.Empty))
                    throw new InvalidOperationException("Native Mammoth handoff requires an unmounted, idle, outside-combat original party.");
                evidence["preset"] = new JObject {
                    ["movementEnabled"] = settings.EnableUnsafeMovementExperiment,
                    ["pairedEnabled"] = settings.EnablePairedActivation,
                    ["legacyUnified"] = settings.EnableUnifiedMountedTurn,
                    ["legacyScheduler"] = settings.EnablePairedCommandScheduler,
                    ["diagnosticOverlayEnabled"] = settings.EnableDiagnosticOverlay,
                    ["overlayPresent"] = playerAction.OverlayPresent,
                    ["overlayObjectCount"] = MountedPlayerActionController.CountOverlayObjects()
                };
                if(settings.EnableUnifiedMountedTurn || settings.EnablePairedCommandScheduler || settings.EnableDiagnosticOverlay || playerAction.OverlayPresent)
                    throw new InvalidOperationException("Native Mammoth qualification requires the existing single paired authority preset: " + evidence["preset"].ToString(Formatting.None));
                if(!combat.TryConfigurePairedActivation(true))throw new InvalidOperationException("Accepted paired authority could not be established.");
                ummLease=MovementScreenshotCaptureCoordinator.AcquireClosedUmmLease();
                selection.SelectUnit(rider.View,true,true,false);
                var selected=selection.SelectedUnits;
                if(selected.Count!=1 || selected[0]!=rider)throw new InvalidOperationException("Native Mammoth setup could not select its exact original rider.");
                nativeControls.Update();
                tranche=new Phase3dHorseScenarioTranche(request,relationship,playerAction,combat,nativeControls,horseService,settings,logger,rider,mount);
                tranche.Start(false);
                results.Add(new RuntimeSubscenarioResult {Name="CM01-native-mammoth-fixture",Status="PASS",AssertionPassCount=1,AssertionFailCount=0,Errors=new string[0]});
            }
            catch(Exception exception){Fail("CM01-native-mammoth-fixture",exception.ToString());Finish();}
        }
        internal void Update()
        {
            if(disposed || !started || completed)return;
            try
            {
                if(!SamePair(before,CapturePair()))throw new InvalidOperationException("Original native Mammoth ownership changed during its exact scenario.");
                tranche.Update();
                if(!tranche.IsCompleted)return;
                results.AddRange(tranche.Results);
                errors.AddRange(tranche.Errors);
                evidence["childScenario"]=request.Scenario;
                evidence["childRows"]=new JArray(tranche.Results.Select(x=>new JObject {["name"]=x.Name,["status"]=x.Status}));
                Finish();
            }
            catch(Exception exception){Fail("CM01-native-mammoth-continuity",exception.ToString());Finish();}
        }
        private static int Id(object value)=>value==null?0:RuntimeHelpers.GetHashCode(value);
        private JObject CapturePair()=>new JObject {
            ["riderId"]=rider?.UniqueId,["mountId"]=mount?.UniqueId,
            ["riderObject"]=Id(rider),["mountObject"]=Id(mount),
            ["mountBlueprint"]=mount?.Blueprint?.AssetGuid,
            ["petObject"]=Id(rider?.Descriptor?.Pet),["petId"]=rider?.Descriptor?.Pet?.UniqueId,
            ["masterObject"]=Id(mount?.Descriptor?.Master.Value),["masterId"]=mount?.Descriptor?.Master.Value?.UniqueId,
            ["riderInState"]=rider?.IsInState,["mountInState"]=mount?.IsInState,
            ["riderIsPartyMember"]=Game.Instance.Player.Party.Contains(rider),
            ["samePlayerPartyGroup"]=rider?.Group!=null && rider.Group==mount?.Group && rider.Group.IsPlayerParty
        };
        internal static bool SamePair(JObject first,JObject second)
        {
            if(first==null || second==null)return false;
            foreach(var p in new[]{first,second})
                if((string)p["mountBlueprint"]!=SupportedMountedProfiles.MammothBlueprintGuid ||
                    string.IsNullOrEmpty((string)p["riderId"]) || string.IsNullOrEmpty((string)p["mountId"]) ||
                    (string)p["riderId"]==(string)p["mountId"] || ((int?)p["riderObject"]).GetValueOrDefault()==0 || ((int?)p["mountObject"]).GetValueOrDefault()==0 ||
                    (int?)p["petObject"]!=(int?)p["mountObject"] || (int?)p["masterObject"]!=(int?)p["riderObject"] ||
                    (string)p["petId"]!=(string)p["mountId"] || (string)p["masterId"]!=(string)p["riderId"] ||
                    (bool?)p["riderInState"]!=true || (bool?)p["mountInState"]!=true ||
                    (bool?)p["riderIsPartyMember"]!=true || (bool?)p["samePlayerPartyGroup"]!=true)return false;
            return JToken.DeepEquals(first,second);
        }
        private void Fail(string row,string detail)
        {
            errors.Add(detail);
            results.Add(new RuntimeSubscenarioResult {Name=row,Status="FAIL",AssertionFailCount=1,AssertionPassCount=0,Errors=new[]{detail}});
        }
        private void Finish()
        {
            if(completed)return;
            try {tranche?.Dispose();}
            catch(Exception e){Fail("CM01-native-mammoth-child-cleanup","Child cleanup: "+e);}
            tranche=null;
            if(capturedIntake)
            {
                try
                {
                    if(settings.EnablePairedActivation!=originalPaired && !combat.TryConfigurePairedActivation(originalPaired))
                        throw new InvalidOperationException("Native Mammoth outer paired configuration restoration failed.");
                    SelectionManager.Instance.MultiSelect(originalSelection.Where(x=>x.IsInState && x.View!=null).Select(x=>x.View),false);
                    Game.Instance.IsPaused=originalPause;
                    ummLease?.Dispose();ummLease=null;
                    var after=CapturePair();evidence["after"]=after;
                    var selectionExact=SelectionManager.Instance.SelectedUnits.SequenceEqual(originalSelection);
                    var restored=SamePair(before,after) && selectionExact && Game.Instance.IsPaused==originalPause &&
                        CombatController.IsInTurnBasedCombat()==originalTurnBased && settings.EnablePairedActivation==originalPaired;
                    evidence["selectionBefore"]=new JArray(originalSelection.Select(x=>x.UniqueId));
                    evidence["selectionAfter"]=new JArray(SelectionManager.Instance.SelectedUnits.Select(x=>x.UniqueId));
                    evidence["pauseBefore"]=originalPause;evidence["pauseAfter"]=Game.Instance.IsPaused;
                    evidence["turnBasedBefore"]=originalTurnBased;evidence["turnBasedAfter"]=CombatController.IsInTurnBasedCombat();
                    evidence["pairedBefore"]=originalPaired;evidence["pairedAfter"]=settings.EnablePairedActivation;
                    evidence["restored"]=restored;
                    if(!restored)throw new InvalidOperationException("Original native Mammoth pair or exact outer fixture state was not restored.");
                    results.Add(new RuntimeSubscenarioResult {Name="CM01-native-mammoth-restoration",Status="PASS",AssertionPassCount=1,AssertionFailCount=0,Errors=new string[0]});
                }
                catch(Exception e){Fail("CM01-native-mammoth-restoration",e.ToString());}
            }
            completed=true;
            try {WriteEvidence();}
            catch(Exception e){Fail("CM01-native-mammoth-artifact","Profile artifact write: "+e);}
        }
        private void WriteEvidence()
        {
            var p=Path.Combine(request.EvidenceRoot,EvidenceFileName);
            if(File.Exists(p))throw new InvalidOperationException("Native profile artifact already exists.");
            var artifact=new JObject {["schemaVersion"]=1,["evidenceKind"]=EvidenceKind,["runId"]=request.RunId,
                ["scenario"]=request.Scenario,["branch"]=request.Branch,["commit"]=request.Commit,
                ["productVersion"]=request.ProductVersion,["dllSha256"]=Sha(typeof(Main).Assembly.Location),
                ["dllMvid"]=typeof(Main).Assembly.ManifestModule.ModuleVersionId.ToString(),["createdAtUtc"]=DateTimeOffset.UtcNow.ToString("o"),
                ["status"]=errors.Count==0?"PASS":"FAIL",["observations"]=evidence,["errors"]=new JArray(errors)};
            var temporary=p+"."+Guid.NewGuid().ToString("N")+".tmp";
            try {File.WriteAllText(temporary,artifact.ToString(Formatting.None),new UTF8Encoding(false));File.Move(temporary,p);}
            finally {if(File.Exists(temporary))File.Delete(temporary);}
        }
        private static string Sha(string path)
        {using(var hash=SHA256.Create())using(var input=File.OpenRead(path))return BitConverter.ToString(hash.ComputeHash(input)).Replace("-","").ToLowerInvariant();}
        public void Dispose()
        {
            if(disposed)return;
            if(started && !completed){Fail("CM01-native-mammoth-interrupted","Native Mammoth scenario interrupted before terminal result.");Finish();}
            disposed=true;
        }
    }
}