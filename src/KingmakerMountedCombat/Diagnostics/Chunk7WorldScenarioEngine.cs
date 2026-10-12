using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.Enums;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using KingmakerMountedCombat.Logging;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // An independent two-pair fixture. The old one-pair scenario guards remain intact.
    // No native save, combat, resource writes, direct Mount or artificial preparation.
    internal sealed class Chunk7WorldScenarioEngine : IDisposable
    {
        internal const string ScenarioName = "chunk7-two-pair-exploration";
        internal const string EvidenceFileName = "chunk7-world-evidence.json";
        internal const string EvidenceKind = "chunk7-world-evidence";
        internal const string WorkingHash = "5eb4e0b4cbd8d60dc879a02ff71aadfde3f517304754857f0cc68d0f9a93f1c6";
        internal const string MammothRiderId = "b6628a77-4962-47a4-a17c-88d9836fc9d5";
        internal const string MammothId = "d79a4f6c-b74e-4868-95bd-533899131acb";
        internal const string HorseRiderId = "d17c8fd0-6627-40c4-bf9a-024cf001c1f7";
        private readonly RuntimeRequest request;
        private readonly MountedWorldService world;
        private readonly NativeMountedControlService controls;
        private readonly HorseCompanionBlueprintService horses;
        private readonly DiagnosticSettings settings;
        private readonly IModLogger logger;
        private readonly Func<MovementTelemetryWriter> optionalTelemetry;
        private MountedOptionalDiagnosticsWindow quietWindow;
        private JObject quietBefore;
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private readonly List<RuntimeSubscenarioResult> results = new List<RuntimeSubscenarioResult>();
        private readonly List<string> errors = new List<string>();
        private readonly JObject evidence = new JObject();
        private readonly JArray snapshots = new JArray(), inputs = new JArray(), movements = new JArray();
        private UnitEntityData firstRider, firstMount, secondRider, secondMount;
        private UnitEntityData[] originalSelection, originalParty;
        private Chunk7NativeWorldScope nativeScope;
        private Feature horseFeature, rankFeature;
        private bool started, completed, disposed, captured, originalPause, originalPaired, originalMovement;
        private bool cleanupStarted, cleanupProven, cleanupPendingReported, screenshotReady;
        private readonly Dictionary<string, int> cleanupAttempts = new Dictionary<string, int>();
        private int step;
        private double stepStarted;
        private IDisposable ummLease;
        private NativeModeTransitionProbe mode;
        private MovementScreenshotCaptureCoordinator screenshots;
        private UnitCommand issuedMove;
        private UnitEntityData movingActor;
        private Vector3 moveStart, moveGoal;
        private JObject moveRecord;
        private JObject resourceBefore;
        private string activeInput;
        private UnitUseAbility shell;
        private bool observedExactProcess;
        private readonly List<UnitCommand> issuedCommands = new List<UnitCommand>();
        private Guid firstOwner, secondOwner;
        private readonly JArray measurements = new JArray(), cycleInputs = new JArray(), cycles = new JArray();
        private MountedRuntimeMeasurement measurement;
        private Chunk7NativeDoorProbe doorProbe;
        private int cycle;
        private JArray InputRecords => cycle == 0 ? inputs : cycleInputs;

        internal Chunk7WorldScenarioEngine(RuntimeRequest request, GameMountedRelationshipService relationship,
            NativeMountedControlService controls, HorseCompanionBlueprintService horses,
            DiagnosticSettings settings, IModLogger logger, Func<MovementTelemetryWriter> optionalTelemetry = null)
        {
            this.request = request; world = relationship.World; this.controls = controls;
            this.horses = horses; this.settings = settings; this.logger = logger; this.optionalTelemetry = optionalTelemetry;
        }
        internal static bool SupportsScenario(string scenario) => scenario == ScenarioName;
        internal bool IsCompleted => completed;
        internal IReadOnlyList<RuntimeSubscenarioResult> Results => results;
        internal IReadOnlyList<string> Errors => errors;
        internal void Start()
        {
            if (started || disposed || !SupportsScenario(request.Scenario)) throw new InvalidOperationException("Chunk 7 scenario admission differs.");
            started = true;
            try
            {
                Require(request.SchemaVersion == RuntimeRequest.SaveBackedSchemaVersion && request.Fixture?.Working?.Sha256 == WorkingHash,
                    "The two-pair fixture requires the exact qualified disposable Working bytes.");
                Require(world != null && NativeMountedSaveFormat.ArchiveNativeSupportEnabled &&
                    NativeMountedSaveFormat.SelectedWriterSchema == 3 && GameMountedRelationshipService.NativePairCapacity >= 2,
                    "Reviewed native all-pair integration, schema 3 and at least two-pair capacity must be selected in source before this scenario.");
                var game = Game.Instance;
                Require(game?.Player != null && SelectionManager.Instance != null && !game.Player.IsInCombat &&
                    game.Player.GameId == request.Fixture.Working.GameId, "The exact loaded Working world must be idle outside combat.");
                firstRider = game.Player.Party.SingleOrDefault(x => x != null && x.UniqueId == MammothRiderId);
                Require(firstRider?.Group != null && firstRider.Group.IsPlayerParty, "Exact original Mammoth owner party group is unavailable.");
                originalParty = PartyActors();
                evidence["partyBefore"] = new JArray(originalParty.Select(CaptureRig));
                Require(originalParty.Select(x => x.UniqueId).OrderBy(x => x, StringComparer.Ordinal).SequenceEqual(
                    new[] { MammothRiderId, MammothId, HorseRiderId }.OrderBy(x => x, StringComparer.Ordinal)),
                    "Party actor IDs differ from the declared three-actor fixture. No substitute actor is selected.");
                firstRider = Exact(MammothRiderId); firstMount = Exact(MammothId);
                secondRider = Exact(HorseRiderId);
                Require(originalParty.All(Idle) && firstMount.Blueprint?.AssetGuid == SupportedMountedProfiles.MammothBlueprintGuid &&
                    ReferenceEquals(firstRider.Descriptor.Pet, firstMount) && ReferenceEquals(firstMount.Descriptor.Master.Value, firstRider),
                    "Original Mammoth ownership or idle party admission differs.");
                Require(secondRider.Descriptor.Pet == null && !secondRider.Descriptor.IsPet && secondRider.IsDirectlyControllable &&
                    secondRider.Descriptor.State.Size == Size.Medium && secondRider.Descriptor.GetFact(horses.LevelRank) == null &&
                    secondRider.GetActivePolymorph() == null, "Exact Horse owner is not an eligible idle Medium native progression owner.");
                string rigError;
                Require(MountedRiderPoseAdapter.TryValidateSupportedSurface(secondRider.View, SupportedMountedProfiles.Horse.RiderPoseProfile, out rigError),
                    "Exact Horse rider rig refused: " + rigError);
                Require(world.Registry.ActiveCount == 0 && world.Pairs.All(x => x.Relationship.State == RelationshipState.Unmounted &&
                    x.Relationship.Runtime.NoPreparedPairResidue) && !controls.HasUnsettledRelationshipTransition,
                    "The fixture already contains mounted ownership or unfinished cleanup.");
                Require(!settings.EnableUnifiedMountedTurn && !settings.EnablePairedCommandScheduler && !settings.EnableDiagnosticOverlay &&
                    MountedPlayerActionController.CountOverlayObjects() == 0, "Accepted paired configuration requires legacy authorities and overlay absent.");
                nativeScope = new Chunk7NativeWorldScope(request, originalParty);
                originalSelection = SelectionManager.Instance.SelectedUnits.ToArray(); originalPause = game.IsPaused;
                originalPaired = settings.EnablePairedActivation; originalMovement = settings.EnableUnsafeMovementExperiment; captured = true;
                Require(world.Primary.Combat.TryConfigurePairedActivation(true), "Paired authority configuration was refused.");
                settings.EnableUnsafeMovementExperiment = true;
                ummLease = MovementScreenshotCaptureCoordinator.AcquireClosedUmmLease();
                mode = new NativeModeTransitionProbe(false); mode.DispatchTemporaryValueIfRequired();
                evidence["capability"] = new JObject { ["archiveNativeEnabled"] = true, ["nativeWriterSchema"] = 3,
                    ["nativePairCapacity"] = GameMountedRelationshipService.NativePairCapacity, ["workingSha256"] = WorkingHash };
                evidence["snapshots"] = snapshots; evidence["inputs"] = inputs; evidence["movements"] = movements;
                evidence["measurements"] = measurements; evidence["cycleInputs"] = cycleInputs; evidence["cycles"] = cycles;
                evidence["nativeProfileInventory"] = Chunk7NativeProfileInventory.Capture(originalParty);
                Pass("C7-world-fixture"); Next(1);
            }
            catch (Exception error) { Fail("C7-world-fixture", error.ToString()); BeginCleanup(); }
        }
        internal void Update()
        {
            if (!started || completed || disposed) return;
            try
            {
                if (cleanupStarted) { PollCleanup(); return; }
                nativeScope.RequireCurrent();
                screenshots?.Pump(Time.frameCount);
                Require(clock.Elapsed.TotalSeconds <= 300 && clock.Elapsed.TotalSeconds - stepStarted <= (step == 40 ? 120 : 30),
                    "Bounded two-pair scenario deadline at step " + step + ".");
                Require(!Game.Instance.Player.IsInCombat, "Exploration scenario unexpectedly entered combat.");
                if (shell != null && controls.HasExactRelationshipProcessBinding(shell)) observedExactProcess = true;
                switch (step)
                {
                    case 1:
                        if (CombatController.IsInTurnBasedCombat()) return;
                        var acquisition = new JObject(); evidence["acquisition"] = acquisition;
                        bool acquired;
                        try { acquired = NativeHorseFixtureAcquisition.Create(secondRider, horses, logger, acquisition); }
                        finally
                        {
                            horseFeature = secondRider.Descriptor.GetFact(horses.HorseFeature) as Feature;
                            rankFeature = secondRider.Descriptor.GetFact(horses.LevelRank) as Feature;
                            secondMount = secondRider.Descriptor.Pet;
                            nativeScope.Track(secondMount);
                        }
                        Require(acquired, "Native Ranger preview/select/commit acquisition failed its exact assertions.");
                        Pass("C7-world-native-acquisition"); Next(2); break;
                    case 2:
                        if (secondMount?.View?.AgentASP == null || !secondMount.IsInState) return;
                        Require(secondMount.Blueprint?.AssetGuid == HorseCompanionBlueprintService.UnitGuid &&
                            ReferenceEquals(secondMount.Descriptor.Master.Value, secondRider), "New Horse is not the exact reciprocal native pet.");
                        evidence["partyWithHorse"] = new JArray(PartyActors().Select(CaptureRig));
                        BeginMeasurement("unmounted-0"); Next(20); break;
                    case 3:
                        if (!Mounted(firstRider, firstMount)) return;
                        if (!InputSettled()) return;
                        firstOwner = world.Registry.FindActive(firstRider).Id;
                        FinishInput(); BeginMeasurement("mounted-1"); Next(21); break;
                    case 4:
                        if (!Mounted(secondRider, secondMount) || !InputSettled()) return;
                        secondOwner = world.Registry.FindActive(secondRider).Id; FinishInput(); AssertBoth();
                        Require(firstOwner != secondOwner, "Two mounted pairs share one registry owner.");
                        Pass("C7-world-two-native-mounts"); snapshots.Add(CaptureWorld("both-mounted"));
                        screenshots = new MovementScreenshotCaptureCoordinator((capture, bytes) => {
                            var folder = Path.Combine(request.EvidenceRoot, "movement-visuals"); Directory.CreateDirectory(folder);
                            var file = Path.Combine(folder, "chunk7-two-pair-mounted.png");
                            if (File.Exists(file)) throw new InvalidOperationException("Chunk 7 screenshot already exists.");
                            File.WriteAllBytes(file, bytes); screenshotReady = true;
                        }, (capture, error) => { Fail("C7-world-visual", error); }, logger);
                        screenshots.Enqueue("C7-world-two-native-mounts", "both-mounted", Time.frameCount + 1); Next(5); break;
                    case 5:
                        if (!screenshotReady) return;
                        BeginMeasurement("mounted-2"); Next(22); break;
                    case 6:
                        AssertBoth(); if (!MovementSettled()) return;
                        Move(secondRider, secondMount, secondMount.Position + Vector3.forward * 2f, firstRider, "horse"); Next(7); break;
                    case 7:
                        AssertBoth(); if (!MovementSettled()) return;
                        Pass("C7-world-selection-routing"); snapshots.Add(CaptureWorld("movement-complete"));
                        doorProbe = new Chunk7NativeDoorProbe(world, nativeScope, firstRider, firstMount, secondRider, secondMount);
                        evidence["door"] = doorProbe.Evidence; doorProbe.Begin(); Next(40); break;
                    case 40:
                        AssertBoth(); if (!doorProbe.Poll()) return;
                        Pass("C7-world-selection-routing"); snapshots.Add(CaptureWorld("door-complete"));
                        Input(firstRider, firstRider, controls.DismountAbility, "dismount-mammoth", secondRider); Next(8); break;
                    case 8:
                        if (Mounted(firstRider, firstMount) || !InputSettled()) return;
                        Require(Mounted(secondRider, secondMount) && world.Registry.FindActive(secondRider).Id == secondOwner,
                            "First native Dismount altered the second pair owner.");
                        FinishInput(); snapshots.Add(CaptureWorld("first-dismounted-second-retained"));
                        Move(firstRider, firstRider, firstRider.Position + Vector3.left * 2f, secondRider, "unmounted-control"); Next(9); break;
                    case 9:
                        Require(Mounted(secondRider, secondMount) && world.Registry.FindActive(secondRider).Id == secondOwner,
                            "Unmounted control altered the remaining mounted owner.");
                        if (!MovementSettled()) return;
                        Input(secondRider, secondRider, controls.DismountAbility, "dismount-horse", firstRider); Next(10); break;
                    case 10:
                        if (Mounted(secondRider, secondMount) || !InputSettled()) return;
                        FinishInput(); Pass("C7-world-independent-dismount");
                        cycle = 1; BeginCycle(); break;
                    case 20:
                        if (!CompleteMeasurement()) return;
                        Input(firstRider, firstMount, controls.MountAbility, "mount-mammoth", secondRider); Next(3); break;
                    case 21:
                        if (!CompleteMeasurement()) return;
                        Input(secondRider, secondMount, controls.MountAbility, "mount-horse", firstRider); Next(4); break;
                    case 22:
                        AssertBoth(); if (!CompleteMeasurement()) return;
                        Move(firstRider, firstMount, firstMount.Position + Vector3.right * 2f, secondRider, "mammoth"); Next(6); break;
                    case 30:
                        if (!Mounted(firstRider, firstMount) || !InputSettled()) return;
                        firstOwner = world.Registry.FindActive(firstRider).Id; FinishInput();
                        Input(secondRider, secondMount, controls.MountAbility, "cycle-" + cycle + "-mount-horse", firstRider); Next(31); break;
                    case 31:
                        if (!Mounted(secondRider, secondMount) || !InputSettled()) return;
                        secondOwner = world.Registry.FindActive(secondRider).Id; FinishInput(); AssertBoth();
                        cycles.Add(CaptureWorld("cycle-" + cycle + "-mounted"));
                        Input(firstRider, firstRider, controls.DismountAbility, "cycle-" + cycle + "-dismount-mammoth", secondRider); Next(32); break;
                    case 32:
                        if (Mounted(firstRider, firstMount) || !InputSettled()) return;
                        Require(Mounted(secondRider, secondMount) && world.Registry.FindActive(secondRider).Id == secondOwner, "Cycle cleanup changed another pair.");
                        FinishInput(); Input(secondRider, secondRider, controls.DismountAbility, "cycle-" + cycle + "-dismount-horse", firstRider); Next(33); break;
                    case 33:
                        if (Mounted(secondRider, secondMount) || !InputSettled()) return;
                        FinishInput(); world.ReleaseSettledAdmissions();
                        Require(world.Registry.Owners.Length == 0 && world.BoundaryCount == 2 &&
                            world.Pairs.All(x => x.Relationship.Runtime.NoPreparedPairResidue && !controls.HasUnsettledShellFor(x.Relationship)),
                            "Settled lifecycle cycle retained unexpected owners, native commands, views or service groups.");
                        cycles.Add(CaptureWorld("cycle-" + cycle + "-settled"));
                        if (++cycle <= 3) BeginCycle();
                        else { Pass("C7-world-cycles"); BeginMeasurement("unmounted-after-cycles"); Next(23); }
                        break;
                    case 23:
                        if (!CompleteMeasurement()) return;
                        Pass("C7-world-sampling"); BeginCleanup(); break;
                }
            }
            catch (Exception error) { Fail("C7-world-runtime", error.ToString()); BeginCleanup(); }
        }
        private UnitEntityData[] PartyActors() => Enumerable.Range(0, firstRider.Group.Count).Select(index => firstRider.Group[index]).ToArray();
        private UnitEntityData Exact(string id) => originalParty.Single(x => x.UniqueId == id);
        private static bool Idle(UnitEntityData actor) => actor != null && actor.IsInState && actor.IsInGame &&
            actor.View?.AgentASP != null && actor.Descriptor.State.IsConscious && !actor.IsInCombat && actor.Commands != null && actor.Commands.Empty;
        private bool Mounted(UnitEntityData rider, UnitEntityData mount)
        {
            var owner = world.Registry.FindActive(rider);
            return owner != null && ReferenceEquals(owner.Mount, mount) && ReferenceEquals(world.Registry.FindActive(mount), owner) &&
                owner.Boundary.State == RelationshipState.Mounted;
        }
        private void AssertBoth()
        {
            Require(Mounted(firstRider, firstMount) && Mounted(secondRider, secondMount) && world.Registry.ActiveCount == 2 &&
                world.Registry.FindActive(firstRider).Id == firstOwner && world.Registry.FindActive(secondRider).Id == secondOwner,
                "Selection or command delivery changed a pair's exact actor/owner binding.");
        }
        private void Select(UnitEntityData actor)
        {
            SelectionManager.Instance.SelectUnit(actor.View, true, true, false);
            Require(SelectionManager.Instance.SelectedUnits.Count == 1 && ReferenceEquals(SelectionManager.Instance.SelectedUnits[0], actor),
                "Native exact single selection failed for " + actor.UniqueId + ".");
        }
        private void Input(UnitEntityData caster, UnitEntityData target, BlueprintAbility ability, string label, UnitEntityData changeSelectionTo)
        {
            Game.Instance.IsPaused = true; Select(caster); controls.Update();
            resourceBefore = Resources(); activeInput = label; observedExactProcess = false;
            var fact = caster.Descriptor.Abilities.GetAbility(ability); var handler = Game.Instance.SelectedAbilityHandler;
            Require(fact?.Data != null && handler != null && target.View != null, "Native selected ability input is unavailable.");
            var before = controls.NativeCastRequestCount; var refused = controls.NativeRefusalCount;
            handler.SetAbility(fact.Data);
            bool clicked;
            try { clicked = handler.OnClick(target.View.gameObject, target.Position, 0, false, false); }
            finally { handler.DropAbility(); }
            shell = caster.Commands.Raw.OfType<UnitUseAbility>().SingleOrDefault(x => ReferenceEquals(x.Spell?.Blueprint, ability));
            if (shell != null) issuedCommands.Add(shell);
            var identity = controls.CaptureRelationshipCommandIdentity(shell);
            InputRecords.Add(new JObject { ["label"] = label, ["casterId"] = caster.UniqueId, ["targetId"] = target.UniqueId,
                ["abilityGuid"] = ability.AssetGuid, ["clicked"] = clicked, ["nativeCastDelta"] = controls.NativeCastRequestCount - before,
                ["nativeRefusalDelta"] = controls.NativeRefusalCount - refused, ["commandObject"] = Id(shell),
                ["shellControl"] = identity?.Control, ["generationAtInit"] = identity?.Generation,
                ["resourcesBefore"] = resourceBefore.DeepClone(), ["selectionAfterInput"] = changeSelectionTo.UniqueId });
            Require(clicked && controls.NativeCastRequestCount == before + 1 && controls.NativeRefusalCount == refused &&
                shell != null && identity != null && identity.Caster == caster.UniqueId && identity.Target == target.UniqueId,
                "Native selected-ability input did not create its exact shell once: " + label + ".");
            Select(changeSelectionTo); Game.Instance.IsPaused = false;
        }
        private bool InputSettled() => !controls.HasUnsettledRelationshipTransition && shell != null && shell.IsFinished &&
            (shell.ExecutionProcess == null || shell.ExecutionProcess.IsEnded);
        private void FinishInput()
        {
            var row = (JObject)InputRecords[InputRecords.Count - 1]; var after = Resources();
            row["resourcesAfter"] = after; row["observedExactProcess"] = observedExactProcess;
            // A short native callback may begin and finish between Update samples. Its typed lifecycle
            // ledger is retained below; polling is not advertised as observing every callback.
            row["shellTerminal"] = shell.IsFinished;
            row["processTerminal"] = shell.ExecutionProcess == null || shell.ExecutionProcess.IsEnded;
            row["explorationResourcesConserved"] = ResourcesConserved(resourceBefore, after);
            Require((bool)row["explorationResourcesConserved"], "Exploration Mount/Dismount changed native action spending: " + activeInput + ".");
            snapshots.Add(CaptureWorld(activeInput + "-settled")); shell = null;
        }
        private void Move(UnitEntityData selected, UnitEntityData executor, Vector3 goal, UnitEntityData switchTo, string label)
        {
            Select(selected); Game.Instance.IsPaused = true;
            var all = Actors(); var before = all.ToDictionary(x => x, x => x.Commands.Move);
            moveStart = executor.Position; moveGoal = goal;
            ClickGroundHandler.MoveSelectedUnitsToPoint(goal, false);
            issuedMove = executor.Commands.Move; movingActor = executor;
            Require(issuedMove != null && !ReferenceEquals(issuedMove, before[executor]), "Native ground click failed to issue the intended command.");
            Require(all.Where(x => !ReferenceEquals(x, executor)).All(x => ReferenceEquals(x.Commands.Move, before[x])),
                "Native ground click changed another actor's movement command.");
            issuedCommands.Add(issuedMove);
            moveRecord = new JObject { ["label"] = label, ["selectedId"] = selected.UniqueId, ["executorId"] = executor.UniqueId,
                ["commandObject"] = Id(issuedMove), ["selectionAfterInput"] = switchTo.UniqueId,
                ["otherCommandsUnchangedAtInput"] = true, ["start"] = Point(moveStart), ["requestedGoal"] = Point(goal),
                ["commandsBefore"] = new JArray(all.Select(x => new JObject { ["id"] = x.UniqueId, ["moveObject"] = Id(before[x]) })),
                ["commandsAfter"] = new JArray(all.Select(x => new JObject { ["id"] = x.UniqueId, ["moveObject"] = Id(x.Commands.Move) })) };
            movements.Add(moveRecord); Select(switchTo);
            quietBefore = CaptureWorld("quiet-before-" + label);
            Require(quietWindow == null && measurement == null && optionalTelemetry != null, "Exact optional telemetry owner is unavailable.");
            quietWindow = new MountedOptionalDiagnosticsWindow(optionalTelemetry(), label);
            quietWindow.Install(Time.frameCount, clock.Elapsed.TotalSeconds);
            Game.Instance.IsPaused = false;
        }
        private bool MovementSettled()
        {
            Require(quietWindow != null, "Native movement lost its optional-capture window owner.");
            if (!quietWindow.Ready(Time.frameCount, clock.Elapsed.TotalSeconds)) return false;
            if (!issuedMove.IsFinished || movingActor.View.AgentASP.IsReallyMoving) return false;
            var distance = Vector3.Distance(moveStart, movingActor.Position); var residual = Vector3.Distance(moveGoal, movingActor.Position);
            moveRecord["end"] = Point(movingActor.Position); moveRecord["distance"] = distance; moveRecord["goalResidual"] = residual;
            moveRecord["commandTerminal"] = issuedMove.IsFinished; moveRecord["ownerContinuity"] = true;
            Require(distance >= 0.75f && residual <= 1.0f, "Native movement failed the fixed two-metre route endpoint.");
            var quietAfter = CaptureWorld("quiet-after-" + (string)moveRecord["label"]);
            Require(ResourcesConserved((JObject)quietBefore["resources"], (JObject)quietAfter["resources"]),
                "Quiet native movement changed unrelated exploration resource ownership.");
            Require(JToken.DeepEquals(ActiveQuietOwners(quietBefore), ActiveQuietOwners(quietAfter)),
                "Quiet native movement changed active pair ownership or generation.");
            Require(JToken.DeepEquals(quietBefore["selection"], quietAfter["selection"]),
                "Quiet native movement changed the deliberately switched selection.");
            var quietReceipt = quietWindow.Complete(Time.frameCount, clock.Elapsed.TotalSeconds); quietWindow = null;
            quietReceipt["before"] = quietBefore; quietReceipt["after"] = quietAfter;
            moveRecord["optionalCaptureWindow"] = quietReceipt; quietBefore = null;
            return true;
        }
        private static JArray ActiveQuietOwners(JObject snapshot) => new JArray(((JArray)snapshot["owners"])
            .Where(x => (string)x["state"] == "Active").OrderBy(x => (string)x["ownerId"], StringComparer.Ordinal)
            .Select(x => new JObject { ["ownerId"] = x["ownerId"], ["riderId"] = x["riderId"],
                ["mountId"] = x["mountId"], ["generation"] = x["generation"], ["state"] = x["state"],
                ["relationshipState"] = x["relationshipState"] }));
        private UnitEntityData[] Actors() => new[] { firstRider, firstMount, secondRider, secondMount }.Where(x => x != null).ToArray();
        private JObject Resources() => new JObject { ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
            ["actors"] = new JArray(Actors().Select(x => new JObject { ["id"] = x.UniqueId,
                ["move"] = x.CombatState.Cooldown.MoveAction, ["standard"] = x.CombatState.Cooldown.StandardAction,
                ["swift"] = x.CombatState.Cooldown.SwiftAction, ["prepared"] = x.CombatState.Prepared,
                ["reactionAllowance"] = x.CombatState.AttackOfOpportunityCount, ["initiative"] = x.CombatState.Initiative })) };
        private static bool ResourcesConserved(JObject before, JObject after)
        {
            var elapsed = ((long)after["gameTicks"] - (long)before["gameTicks"]) / (double)TimeSpan.TicksPerSecond;
            var aa = (JArray)after["actors"];
            foreach (var a in (JArray)before["actors"])
            {
                var b = aa.SingleOrDefault(x => (string)x["id"] == (string)a["id"]); if (b == null) return false;
                foreach (var field in new[] { "move", "standard", "swift" })
                    if (!NativeResourceWindowPolicy.EndpointConserved((double)a[field], (double)b[field], elapsed, false, 0.03)) return false;
                if ((bool)a["prepared"] != (bool)b["prepared"] || (int)a["initiative"] != (int)b["initiative"] ||
                    (int)a["reactionAllowance"] != (int)b["reactionAllowance"]) return false;
            }
            return true;
        }
        private JObject CaptureWorld(string label) => new JObject { ["label"] = label, ["frame"] = Time.frameCount,
            ["selection"] = new JArray(SelectionManager.Instance.SelectedUnits.Select(x => x.UniqueId)), ["resources"] = Resources(),
            ["boundaryCount"] = world.BoundaryCount, ["participantCount"] = world.UnifiedTurn.SnapshotParticipants.Length,
            ["subscriptions"] = Chunk4SubscriptionSnapshot.Capture(),
            ["owners"] = new JArray(world.Registry.Owners.Select(x => new JObject { ["ownerId"] = x.Id.ToString(),
                ["riderId"] = x.RiderId, ["mountId"] = x.MountId, ["generation"] = x.Generation, ["state"] = x.State.ToString(),
                ["relationshipState"] = x.Boundary.State.ToString(), ["retainsObligations"] = x.RetainsObligations(),
                ["saveReady"] = x.SaveReady(), ["teardownReady"] = x.TeardownReady() })) };
        private static JObject CaptureRig(UnitEntityData actor)
        {
            if (actor == null) return null;
            var bones = actor.View == null ? new string[0] : actor.View.GetComponentsInChildren<Transform>(true)
                .Where(x => x.name == "Pelvis" || x.name == "Spine" || x.name == "Chest" || x.name.Contains("Thigh") ||
                    x.name.Contains("Calf") || x.name.Contains("Foot") || x.name.Contains("Stirrup")).Select(x => x.name).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            return new JObject { ["id"] = actor.UniqueId, ["object"] = Id(actor), ["blueprint"] = actor.Blueprint?.AssetGuid,
                ["size"] = actor.Descriptor.State.Size.ToString(), ["viewName"] = actor.View?.name, ["bodyType"] = actor.View?.GetType().FullName,
                ["polymorphed"] = actor.GetActivePolymorph() != null, ["petId"] = actor.Descriptor.Pet?.UniqueId,
                ["masterId"] = actor.Descriptor.Master.Value?.UniqueId, ["directlyControllable"] = actor.IsDirectlyControllable,
                ["characterLevel"] = actor.Descriptor.Progression.CharacterLevel, ["bones"] = new JArray(bones) };
        }
        private void Next(int next) { step = next; stepStarted = clock.Elapsed.TotalSeconds; }
        private void BeginCycle()
        {
            Input(firstRider, firstMount, controls.MountAbility, "cycle-" + cycle + "-mount-mammoth", secondRider); Next(30);
        }
        private void BeginMeasurement(string label)
        {
            Require(Actors().All(Idle), "Performance sampling requires the same idle native four-actor party.");
            Game.Instance.IsPaused = false; measurement = new MountedRuntimeMeasurement(label, world);
        }
        private bool CompleteMeasurement()
        {
            if (!measurement.Ready) return false;
            measurements.Add(measurement.Complete()); measurement = null; return true;
        }
        private void BeginCleanup()
        {
            if (cleanupStarted) return;
            cleanupStarted = true; stepStarted = clock.Elapsed.TotalSeconds;
            TryCleanup("optional capture window", () => { quietWindow?.Dispose(); quietWindow = null; });
            TryCleanup("measurement", () => { measurement?.Dispose(); measurement = null; });
        }
        private bool TryCleanup(string part, Action attempt)
        {
            try { attempt(); return true; }
            catch (Exception error)
            {
                var key = part + "|" + error.GetType().FullName + "|" + error.Message;
                cleanupAttempts.TryGetValue(key, out var count); cleanupAttempts[key] = count + 1;
                if (count == 0) Fail("C7-world-cleanup", part + ": " + error);
                evidence["cleanupFailureAttempts"] = JObject.FromObject(cleanupAttempts);
                return false;
            }
        }
        private bool NativeCommandsSettled() => issuedCommands.Where(command => command != null).All(command =>
            command.IsFinished && (!(command is UnitUseAbility ability) || ability.ExecutionProcess == null || ability.ExecutionProcess.IsEnded));
        private bool PairCommandsSettled(MountedPairServices pair) => !pair.Combat.HasActiveCommand &&
            !pair.Combat.HasActiveGroundMovement && !pair.Combat.HasChargeOwnership && !controls.HasUnsettledShellFor(pair.Relationship);
        private void RetainCleanup(string reason)
        {
            if (clock.Elapsed.TotalSeconds - stepStarted < 15 || cleanupPendingReported) return;
            cleanupPendingReported = true;
            Fail("C7-world-cleanup", reason + " Fifteen-second cleanup budget expired; custody remains retained for host polling.");
            evidence["cleanupPendingAtHostBoundary"] = true;
            evidence["pendingCleanupReason"] = reason;
            evidence["combatOrPersistenceProof"] = false;
            RuntimeLoaderInventory.WriteArtifact(request, "chunk7-world-cleanup-pending.json", "chunk7-world-cleanup-pending", "FAIL", evidence, errors);
        }
        private void PollCleanup()
        {
            if (completed) return;
            var observersGone = TryCleanup("optional capture window", () => { quietWindow?.Dispose(); quietWindow = null; });
            observersGone &= TryCleanup("measurement", () => { measurement?.Dispose(); measurement = null; });
            if (!observersGone) { RetainCleanup("An owned observer has not detached."); return; }
            if (!captured) { cleanupProven = true; CompleteCleanupEvidence(); return; }
            // Neither fact removal nor actor/view teardown can race a native load,
            // archive iterator/worker or shared serialization lease.
            if (LoadingProcess.Instance.IsLoadingInProcess || LoadingProcess.Instance.QueuedNames.Any() ||
                NativePersistenceIsolation.HasPendingWrites || world.SerializationBusy)
            { RetainCleanup("A native load or archive worker still owns this world."); return; }
            if (!TryCleanup("captured native world", () => {
                Require(nativeScope != null, "Captured native world authority is absent."); nativeScope.RequireCurrent();
            })) { RetainCleanup("Captured native world, area or actor identity changed."); return; }
            if (!TryCleanup("screenshots", () => { screenshots?.Dispose(); screenshots = null; }))
            { RetainCleanup("The captured screenshot overlay lease has not restored."); return; }
            if (!TryCleanup("unpause for native drain", () => Game.Instance.IsPaused = false)) return;
            foreach (var command in issuedCommands.Where(x => x != null && !x.IsFinished))
                TryCleanup("command " + Id(command), () => command.Interrupt());
            var doorReady = doorProbe == null;
            if (doorProbe != null) TryCleanup("native door lease", () => {
                foreach (var command in doorProbe.IssuedCommands) if (!issuedCommands.Contains(command)) issuedCommands.Add(command);
                doorReady = doorProbe.Drain();
            });
            // Independent pairs receive service even when another cancellation or
            // cleanup fails. Each pair keeps its own real command/process custody.
            foreach (var pair in world.Pairs)
            {
                TryCleanup("combat " + Id(pair.Relationship), () => pair.Combat.Cancel("Chunk 7 fixture cleanup"));
                if (!PairCommandsSettled(pair)) continue;
                TryCleanup("relationship " + Id(pair.Relationship), () => {
                    if (pair.Relationship.State != RelationshipState.Unmounted || pair.Relationship.PairOwner != null ||
                        !pair.Relationship.Runtime.NoPreparedPairResidue)
                        Require(pair.Relationship.Dismount(CleanupTrigger.Exception).Succeeded, "Pair cleanup remains unfinished.");
                });
            }
            var pairClean = world.Pairs.All(pair => pair.Relationship.State == RelationshipState.Unmounted &&
                pair.Relationship.Runtime.NoPreparedPairResidue && pair.Relationship.PairOwner == null && PairCommandsSettled(pair));
            if (!doorReady || !NativeCommandsSettled() || !pairClean || controls.HasUnsettledRelationshipTransition || world.UnifiedTurn.HasUnsettledPreparation)
            { RetainCleanup("A native execution process, pair or preparation still owns cleanup."); return; }
            if (evidence["factRemovalAdmission"] == null)
                evidence["factRemovalAdmission"] = new JObject {
                    ["frame"] = Time.frameCount, ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
                    ["owners"] = world.Registry.Owners.Length, ["activePairs"] = world.Registry.ActiveCount,
                    ["preparationPending"] = world.UnifiedTurn.HasUnsettledPreparation, ["serializationBusy"] = world.SerializationBusy,
                    ["pendingNativeWrites"] = NativePersistenceIsolation.HasPendingWrites,
                    ["commands"] = new JArray(issuedCommands.Where(command => command != null).Select(command => {
                        var ability = command as UnitUseAbility;
                        return new JObject { ["commandObject"] = Id(command), ["finished"] = command.IsFinished,
                            ["hasExecutionProcess"] = ability?.ExecutionProcess != null,
                            ["processEnded"] = ability?.ExecutionProcess == null || ability.ExecutionProcess.IsEnded };
                    })) };
            // The temporary progression fact owns the pet entity. Remove it only
            // after every issued native process and pair lease has truly ended.
            if (secondRider != null && horseFeature != null)
                TryCleanup("Horse feature", () => {
                    Require(secondRider.Descriptor.Pet == null || ReferenceEquals(secondRider.Descriptor.Pet, secondMount),
                        "Cleanup refuses to remove a replaced companion.");
                    secondRider.Descriptor.Progression.Features.RemoveFact(horseFeature); horseFeature = null;
                });
            if (secondRider != null && rankFeature != null)
                TryCleanup("owned companion rank", () => { secondRider.Descriptor.Progression.Features.RemoveFact(rankFeature); rankFeature = null; });
            var entitiesDrained = TryCleanup("native entity drain", () => Game.Instance.EntityDestroyer.Tick());
            var horseClean = horseFeature == null && rankFeature == null && (secondMount == null ||
                !secondMount.IsInState && !Game.Instance.State.Units.Contains(secondMount) &&
                !Game.Instance.Player.ControllableCharacters.Contains(secondMount) && secondRider.Descriptor.Pet == null);
            if (!entitiesDrained || !horseClean) { RetainCleanup("The owned Horse entity or progression facts have not settled."); return; }
            var restored = TryCleanup("original native mode", () => { mode?.Dispose(); mode = null; });
            restored &= TryCleanup("original paired configuration", () => {
                Require(world.Primary.Combat.TryConfigurePairedActivation(originalPaired), "Original paired configuration restoration refused.");
                settings.EnableUnsafeMovementExperiment = originalMovement;
            });
            restored &= TryCleanup("original native selection", () => {
                SelectionManager.Instance.MultiSelect(originalSelection.Where(x => x.IsInState && x.View != null).Select(x => x.View), false);
                Require(SelectionManager.Instance.SelectedUnits.SequenceEqual(originalSelection), "Original selection was not restored.");
            });
            restored &= TryCleanup("UMM presentation lease", () => { ummLease?.Dispose(); ummLease = null; });
            if (!restored) { RetainCleanup("A captured fixture lease has not restored."); return; }
            Game.Instance.IsPaused = originalPause;
            Require(ReferenceEquals(firstRider.Descriptor.Pet, firstMount) && ReferenceEquals(firstMount.Descriptor.Master.Value, firstRider),
                "Original Mammoth ownership was not restored.");
            cleanupProven = true;
            evidence["cleanup"] = new JObject { ["pairResidueAbsent"] = pairClean, ["horseRemoved"] = horseClean,
                ["controlsSettled"] = !controls.HasUnsettledRelationshipTransition, ["settingsAndSelectionRestored"] = true,
                ["nativeProcessesSettled"] = NativeCommandsSettled(), ["cleanupCustodySettled"] = cleanupProven,
                ["nativeProgressionRequiresExternalWorkingRestoration"] = true,
                ["ordinaryRespecProven"] = false, ["nativeSaveWritten"] = false,
                ["world"] = CaptureWorld("cleanup"), ["partyAfter"] = new JArray(PartyActors().Select(CaptureRig)),
                ["selectionBefore"] = new JArray(originalSelection.Select(x => x.UniqueId)),
                ["selectionAfter"] = new JArray(SelectionManager.Instance.SelectedUnits.Select(x => x.UniqueId)),
                ["pauseBefore"] = originalPause, ["pauseAfter"] = Game.Instance.IsPaused,
                ["movementBefore"] = originalMovement, ["movementAfter"] = settings.EnableUnsafeMovementExperiment,
                ["pairedBefore"] = originalPaired, ["pairedAfter"] = settings.EnablePairedActivation,
                ["pairBoundaries"] = new JArray(world.Pairs.Select(x => new JObject { ["state"] = x.Relationship.State.ToString(),
                    ["residueAbsent"] = x.Relationship.Runtime.NoPreparedPairResidue, ["ownerPresent"] = x.Relationship.PairOwner != null })) };
            Pass("C7-world-cleanup"); CompleteCleanupEvidence();
        }
        private void CompleteCleanupEvidence()
        {
            Require(cleanupProven, "World scenario cannot complete while native cleanup is retained.");
            evidence["nativeShellLifecycle"] = controls.DescribeNativeShellLifecycle();
            evidence["elapsedSeconds"] = clock.Elapsed.TotalSeconds;
            evidence["combatOrPersistenceProof"] = false;
            evidence["thirdUnrelatedActorProven"] = false;
            RuntimeLoaderInventory.WriteArtifact(request, EvidenceFileName, EvidenceKind, errors.Count == 0 ? "PASS" : "FAIL", evidence, errors);
            completed = true;
        }
        internal bool DrainForHost()
        {
            if (disposed) return true;
            if (!cleanupStarted)
            {
                Fail("C7-world-interrupted", "World scenario interrupted before its bounded terminal result.");
                BeginCleanup();
            }
            if (!completed) PollCleanup();
            return completed && cleanupProven;
        }
        private static int Id(object value) => value == null ? 0 : RuntimeHelpers.GetHashCode(value);
        private static JObject Point(Vector3 point) => new JObject { ["x"] = point.x, ["y"] = point.y, ["z"] = point.z };
        private static void Require(bool condition, string error) { if (!condition) throw new InvalidOperationException(error); }
        private void Pass(string row)
        {
            var existing = results.SingleOrDefault(x => x.Name == row);
            if (existing == null) results.Add(new RuntimeSubscenarioResult { Name = row, Status = "PASS", AssertionPassCount = 1, Errors = new string[0] });
            else existing.AssertionPassCount++;
        }
        private void Fail(string row, string error)
        {
            if (errors.Contains(error)) return;
            errors.Add(error);
            var existing = results.SingleOrDefault(x => x.Name == row);
            if (existing == null) results.Add(new RuntimeSubscenarioResult { Name = row, Status = "FAIL", AssertionFailCount = 1, Errors = new[] { error } });
            else { existing.Status = "FAIL"; existing.AssertionFailCount++; existing.Errors = existing.Errors.Concat(new[] { error }).ToArray(); }
        }
        public void Dispose()
        {
            if (disposed) return;
            if (!DrainForHost()) throw new InvalidOperationException("World fixture retains native cleanup and requires owner polling.");
            disposed = true;
        }
    }
}
