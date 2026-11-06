using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Kingmaker;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.UI.Group;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.View;
using Kingmaker.View.MapObjects;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // F4 (WORLD-03, WORLD-01, GAIT-01) on the owner's surface, the explicit opt-in on the pre-owned single pair. After the
    // ordinary Mount: one ordinary group order on the hub's nearest exact native loot container (the whole party selected, the
    // native ClickMapObjectHandler choosing the one interactor), the native loot window closed by its own Close button, then
    // the native party selection (ShowPartySelection, Go with the party unchanged: Player.ReInitPartyCharacters, the boundary
    // that dismounted every rider at the Beneath the Stolen Lands entrance). The aligned speed legs follow unchanged, with one
    // normal-camera frame of the Horse inside the party leg and the solo leg. Nothing is opened, moved or decided directly.
    internal sealed partial class Chunk7WorldScenarioEngine
    {
        internal const string Finalize4ExplorationContract = "finalize4-owner-surface-exploration-v1";
        private const float Finalize4ContainerMinimumMetres = 6f, Finalize4ContainerMaximumMetres = 30f;
        private bool OwnerSurfaceF4Selected => OwnerSurfaceSpeed && request.OwnedPairs?.OwnerSurfaceF4Selected == true;
        private JObject f4;
        private int f4Step;
        private MapObjectView f4Container;
        private InteractionComponent f4Interaction;
        private UnitInteractWithObject f4Command;
        private long f4BindBaseline, f4TerminalBaseline, f4TrapStopBaseline;
        private Finalize4LootObserver f4Loot;
        private GroupManager f4Group;
        private bool f4GoRequested, f4PreMoved;
        private long f4ReplacedBaseline;
        private Kingmaker.UnitLogic.Commands.Base.UnitCommand f4TravelCommand;

        private bool Fail4(string error) { Require(false, error); return false; }
        private readonly HashSet<string> f4CaptureLegs = new HashSet<string>(StringComparer.Ordinal);
        private MovementScreenshotCaptureCoordinator f4Shots;
        private JArray f4Captures;

        private MountedCombatController F4Combat => world.ForActor(secondRider)?.Combat;
        private GameMountedRelationshipService F4Relationship => world.ForActor(secondRider)?.Relationship;

        private void BeginFinalize4Exploration()
        {
            Require(f4 == null && OwnerSurfaceF4Selected && F4Combat != null, "The F4 exploration stage runs once on its explicit opt-in.");
            f4Captures = new JArray();
            f4 = new JObject { ["contract"] = Finalize4ExplorationContract, ["riderId"] = secondRider.UniqueId, ["horseId"] = secondMount.UniqueId,
                ["captures"] = f4Captures, ["disclosedSetup"] = new JArray("camera scrolled to the Horse for each frame (presentation only)") };
            evidence["finalize4"] = f4;
            f4Loot = new Finalize4LootObserver();
            f4Step = 0;
        }

        // Returns true when the chest order and the party selection are complete.
        private bool PollFinalize4Exploration()
        {
            switch (f4Step)
            {
                case 0: return F4StartContainerOrder();
                case 1: return F4AwaitContainerOrder();
                case 2: return F4CloseLoot();
                case 3: return F4OpenPartySelection();
                case 4: return F4AwaitPartySelection();
                case 5:
                    if (secondMount.Commands.Empty && secondMount.View?.MovementAgent?.IsReallyMoving != true && Actors().All(Idle)) { f4Step = 0; Next(220); }
                    return false;
                case 6: return F4ClickContainerWhileTravelling();
                default: return true;
            }
        }

        private bool F4StartContainerOrder()
        {
            if (!Actors().All(Idle)) return false;
            var horse = secondMount.Position;
            var views = Game.Instance.State.MapObjects.Select(m => m.View as MapObjectView).Where(v => v != null).ToArray();
            var candidates = views.Select(v => new { View = v, Interaction = v.Interactions?.FirstOrDefault(i => i != null && i.GetType() == typeof(LootComponent)) })
                .Where(c => c.Interaction != null).Select(c => new { c.View, c.Interaction, Distance = Vector3.Distance(c.View.transform.position, horse),
                    Available = ClickMapObjectHandler.HasAvailableInteractions(c.View.gameObject) && c.Interaction.CanInteract() })
                .OrderBy(c => c.Distance).ToArray();
            f4["containers"] = new JArray(candidates.Select(c => new JObject { ["name"] = c.View.name, ["id"] = c.View.Data?.UniqueId, ["distance"] = c.Distance,
                ["available"] = c.Available, ["type"] = c.Interaction.Type.ToString(), ["proximityRadius"] = c.Interaction.ProximityRadius }));
            // Native InteractionComponent.SelectUnit picks the closest selected non-pet: the row needs a container the rider is
            // closest to (by a margin), or it would observe another member's ordinary unmounted interaction instead.
            var others = originalParty.Where(u => u != secondRider && !u.Descriptor.IsPet).ToArray();
            Func<MapObjectView, float> margin = v => others.Length == 0 ? float.MaxValue :
                others.Min(u => (u.Position - v.transform.position).sqrMagnitude) - (secondRider.Position - v.transform.position).sqrMagnitude;
            var chosen = candidates.FirstOrDefault(c => c.Available && c.Interaction.Type == InteractionType.Approach &&
                c.Distance >= Finalize4ContainerMinimumMetres && c.Distance <= Finalize4ContainerMaximumMetres && margin(c.View) > 1f);
            f4["riderNearestMarginSqr"] = chosen == null ? (JToken)null : margin(chosen.View);
            if (chosen == null && !f4PreMoved)
            {
                // Disclosed setup, once: the pair alone takes one ordinary rider-selected ground step of about 3 m toward the
                // nearest available container, so the rider is the closest selected non-pet; the group order still starts
                // out of the interaction range.
                var target = candidates.FirstOrDefault(c => c.Available && c.Interaction.Type == InteractionType.Approach &&
                    c.Distance >= Finalize4ContainerMinimumMetres + 3f && c.Distance <= Finalize4ContainerMaximumMetres);
                Require(target != null, "No available exact native loot container between 9 and 30 m of the Horse on this surface.");
                var goal = Kingmaker.View.ObstacleAnalyzer.GetNearestNode(horse + (target.View.transform.position - horse).normalized * 3f).clampedPosition;
                Chunk7NativeFixture.Select(secondRider);
                ClickGroundHandler.MoveSelectedUnitsToPoint(goal, false);
                f4PreMoved = true;
                ((JArray)f4["disclosedSetup"]).Add("one rider-selected ordinary ground step of about 3 m toward " + target.View.name + " so the rider is the closest selected non-pet");
                f4["preMoveGoal"] = Point(goal);
                f4Step = 5; Next(224); return false;
            }
            Require(chosen != null, "No available exact native loot container between 6 and 30 m of the Horse that the rider is nearest to on this surface.");
            f4Container = chosen.View; f4Interaction = chosen.Interaction;
            // The whole party selected through the native selection manager, exactly as the owner's group selection.
            SelectionManager.Instance.MultiSelect(originalParty.Where(u => u.View != null).Select(u => u.View).ToArray(), false);
            var selected = SelectionManager.Instance.SelectedUnits.ToArray();
            Require(selected.Length >= 2 && selected.Contains(secondRider) && !selected.Contains(secondMount), "Native selection did not take the group with the pair through its rider.");
            // F4 completion control (review MAJOR): the chest order is given while the group is already travelling, so the
            // carried approach must replace exactly that earlier pair travel. A short ordinary group move sideways first.
            var side = Vector3.Cross(Vector3.up, (f4Container.transform.position - horse).normalized);
            f4TravelGoal = Kingmaker.View.ObstacleAnalyzer.GetNearestNode(horse + side * 5f).clampedPosition;
            ClickGroundHandler.MoveSelectedUnitsToPoint(f4TravelGoal, false);
            f4TravelSelected = selected; f4TravelChosenDistance = chosen.Distance; f4TravelOrderedAt = Time.frameCount;
            f4["travelBeforeOrder"] = new JObject { ["goal"] = Point(f4TravelGoal), ["frame"] = Time.frameCount, ["inputPath"] = "ClickGroundHandler.MoveSelectedUnitsToPoint" };
            f4Step = 6; Next(225); return false;
        }

        private Vector3 f4TravelGoal;
        private UnitEntityData[] f4TravelSelected;
        private float f4TravelChosenDistance;
        private int f4TravelOrderedAt;

        // The chest click once the Horse is actually travelling on the group order (bounded by the step deadline).
        private bool F4ClickContainerWhileTravelling()
        {
            var agent = secondMount.View?.MovementAgent;
            var travel = secondMount.Commands.GetCommand(Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Move);
            if (agent == null || !agent.IsReallyMoving || travel == null || travel.IsFinished) return Time.frameCount - f4TravelOrderedAt > 90 ? Fail4("The group travel order never moved the Horse.") : false;
            var selected = f4TravelSelected; var horse = secondMount.Position;
            ((JObject)f4["travelBeforeOrder"])["travelCommandObject"] = Id(travel);
            ((JObject)f4["travelBeforeOrder"])["horseMovingAtClick"] = true;
            var chosenDistance = f4TravelChosenDistance;
            var combat = F4Combat; f4ReplacedBaseline = combat.CarriedApproachReplacedTravelCount; f4TravelCommand = travel; f4BindBaseline = combat.CarriedApproachBindCount; f4TerminalBaseline = combat.CarriedApproachTerminalCount; f4TrapStopBaseline = F4Relationship.TrapGroupStopForwardCount;
            var before = new JObject { ["selectedIds"] = new JArray(selected.Select(u => u.UniqueId)), ["horseStart"] = Point(horse),
                ["riderStart"] = Point(secondRider.Position), ["containerPosition"] = Point(f4Container.transform.position), ["container"] = f4Container.name,
                ["containerId"] = f4Container.Data?.UniqueId, ["distanceAtClick"] = Vector3.Distance(f4Container.transform.position, horse),
                ["distanceAtChoice"] = chosenDistance, ["frame"] = Time.frameCount };
            // Review MINOR: SelectUnit decides at the click, so the rider-nearest margin is recomputed there (members are moving).
            var containerAt = f4Container.transform.position; var rivals = selected.Where(u => u != secondRider && !u.Descriptor.IsPet).ToArray();
            before["riderNearestMarginSqrAtClick"] = rivals.Length == 0 ? (JToken)null :
                rivals.Min(u => (u.Position - containerAt).sqrMagnitude) - (secondRider.Position - containerAt).sqrMagnitude;
            var handler = Game.Instance.DefaultPointerController?.GetHandler<ClickMapObjectHandler>();
            Require(handler != null, "The native map-object click handler is unavailable.");
            before["clicked"] = handler.OnClick(f4Container.gameObject, f4Container.transform.position, 0);
            before["inputPath"] = "ClickMapObjectHandler.OnClick";
            // Which selected unit native SelectUnit chose: the one that now holds the native interaction command.
            var holders = selected.Where(u => u.Commands.Raw.Any(c => c is UnitInteractWithObject w && w.Interaction == f4Interaction)).ToArray();
            before["interactorIds"] = new JArray(holders.Select(u => u.UniqueId));
            f4Command = holders.Length == 1 ? holders[0].Commands.Raw.OfType<UnitInteractWithObject>().First(w => w.Interaction == f4Interaction) : null;
            before["commandObject"] = f4Command == null ? 0 : Id(f4Command);
            f4["containerOrder"] = before;
            Require((bool)before["clicked"] && f4Command != null, "The ordinary group chest order did not give exactly one selected unit the native interaction.");
            f4Step = 1; Next(220); return false;
        }

        private bool F4AwaitContainerOrder()
        {
            var combat = F4Combat; var order = (JObject)f4["containerOrder"];
            if (combat.CarriedApproachBindCount > f4BindBaseline) order["carriedBound"] = true;
            // r02 correction: the bound approach publishes its terminal record on the controller's update after the command ends;
            // wait (bounded by the step deadline) for that terminal, never an earlier or arbitrary outcome; a command that ended
            // with no bind at all has no terminal to wait for and is recorded at once for the reader to refuse.
            if (!f4Command.IsFinished || combat.HasLiveCarriedApproach) return false;
            if (combat.CarriedApproachBindCount > f4BindBaseline && combat.CarriedApproachTerminalCount == f4TerminalBaseline) return false;
            var outcome = combat.CarriedApproachTerminalCount > f4TerminalBaseline ? combat.LastCarriedApproachOutcome : null;
            order["terminalsSinceClick"] = combat.CarriedApproachTerminalCount - f4TerminalBaseline;
            order["terminalMatchesOrder"] = outcome != null && outcome.TargetId == f4Container.Data?.UniqueId && outcome.Kind == MountedCarriedCommandKind.ContainerInteraction.ToString();
            order["executorId"] = f4Command.Executor?.UniqueId; order["result"] = f4Command.Result.ToString();
            order["carriedBinds"] = combat.CarriedApproachBindCount - f4BindBaseline;
            order["carriedOutcome"] = outcome == null ? null : new JObject { ["kind"] = outcome.Kind, ["satisfied"] = outcome.Satisfied,
                ["moveStarts"] = outcome.DelegatedMoveStartCount, ["distance"] = outcome.DistanceAtSatisfaction, ["los"] = outcome.LineOfSightAtSatisfaction,
                ["slotRestored"] = outcome.MountMoveSlotRestored, ["reason"] = outcome.TerminalReason, ["targetId"] = outcome.TargetId, ["commandType"] = outcome.CommandType, ["result"] = outcome.Result };
            order["horseEnd"] = Point(secondMount.Position); order["horseDisplacement"] = HorizontalDistance(secondMount.Position, Vec((JObject)order["horseStart"]));
            order["riderToContainerAtEnd"] = Vector3.Distance(secondRider.Position, f4Container.transform.position);
            order["stillMounted"] = Mounted(secondRider, secondMount);
            order["lootInteractions"] = new JArray(f4Loot.Events.Select(e => new JObject { ["unitId"] = e.Key, ["frame"] = e.Value }));
            order["trapStopsForwarded"] = F4Relationship.TrapGroupStopForwardCount - f4TrapStopBaseline;
            order["replacedTravel"] = combat.CarriedApproachReplacedTravelCount - f4ReplacedBaseline;
            order["earlierTravelFinished"] = f4TravelCommand != null && f4TravelCommand.IsFinished;
            order["earlierTravelResult"] = f4TravelCommand?.Result.ToString();
            // Other selected members keep their own group travel natively (a chest order stops nobody else).
            order["othersAfter"] = new JArray(f4TravelSelected.Where(u => u != secondRider).Select(u => new JObject { ["id"] = u.UniqueId,
                ["distanceToTravelGoal"] = HorizontalDistance(u.Position, f4TravelGoal) }));
            order["frameEnd"] = Time.frameCount;
            f4Step = 2; Next(221); return false;
        }

        private bool F4CloseLoot()
        {
            // The native loot window, closed by its own Close button handler; bounded by the step deadline.
            var window = UnityEngine.Object.FindObjectsOfType<Kingmaker.UI.Loot.LootWindowController>().FirstOrDefault(w => w.gameObject.activeInHierarchy);
            if (window != null) { window.OnButtonClose(); ((JObject)f4["containerOrder"])["lootWindowClosedByButton"] = true; return false; }
            if (Game.Instance.CurrentMode != Kingmaker.GameModes.GameModeType.Default) return false;
            f4Step = 3; Next(222); return false;
        }

        private bool F4OpenPartySelection()
        {
            if (!Actors().All(Idle)) return false;
            var relationship = F4Relationship;
            f4Group = Game.Instance.UI.Canvas.GroupManager;
            Require(f4Group != null && !f4Group.gameObject.activeSelf, "A pre-existing party selector cannot be borrowed.");
            var party = new JObject { ["ownerBefore"] = world.Registry.FindActive(secondRider)?.Id.ToString(), ["generationBefore"] = relationship.MountedPairGeneration,
                ["stateBefore"] = relationship.State.ToString(), ["partyBefore"] = new JArray(Game.Instance.Player.PartyCharacters.Select(u => u.Value?.UniqueId)),
                ["action"] = "native-ShowPartySelection" };
            f4["partySelection"] = party;
            new ShowPartySelection().RunAction();
            Require(ReferenceEquals(Game.Instance.UI.Canvas.GroupManager, f4Group) && f4Group.gameObject.activeSelf, "Native selector did not open as the owned instance.");
            f4GoRequested = false; f4Step = 4; Next(223); return false;
        }

        private bool F4AwaitPartySelection()
        {
            var party = (JObject)f4["partySelection"];
            if (!f4GoRequested)
            {
                if (!f4Group.gameObject.activeInHierarchy) return false;
                f4GoRequested = true; party["goFrame"] = Time.frameCount;
                // The native Go button: SetGroup (Player.ReInitPartyCharacters, every member removed and re-added), then close.
                f4Group.Go();
            }
            if (f4Group.gameObject.activeSelf) { if (clock.Elapsed.TotalSeconds - stepStarted > 10) { party["goRefused"] = true; f4Group.Close(); } return false; }
            var relationship = F4Relationship;
            party["stateAfter"] = relationship.State.ToString(); party["generationAfter"] = relationship.MountedPairGeneration;
            party["ownerAfter"] = world.Registry.FindActive(secondRider)?.Id.ToString();
            party["partyAfter"] = new JArray(Game.Instance.Player.PartyCharacters.Select(u => u.Value?.UniqueId));
            party["stillMounted"] = Mounted(secondRider, secondMount);
            f4Step = 5; return true;
        }

        // One normal-camera frame of the Horse inside each named leg, once it moves on its own native command.
        private void PumpFinalize4Gait(string leg)
        {
            if (f4 == null) return;
            if (f4Shots == null)
                f4Shots = new MovementScreenshotCaptureCoordinator(F4CaptureCommitted, (capture, error) =>
                    f4Captures.Add(new JObject { ["leg"] = capture.Milestone, ["error"] = error }), logger);
            var agent = secondMount.View?.MovementAgent; var manager = secondMount.View?.AnimationManager;
            // Steady motion first: the Horse has moved for 15 frames on its own native command.
            if (leg != null && agent != null && agent.IsReallyMoving && agent.Speed > 0.5f && manager != null)
            {
                int moving; f4MovingFrames.TryGetValue(leg, out moving); f4MovingFrames[leg] = ++moving;
                if (moving == 15 && !f4CaptureLegs.Contains(leg))
                {
                    f4CaptureLegs.Add(leg);
                    Game.Instance.UI.GetCameraRig()?.ScrollToImmediately(secondMount.Position);
                    f4["gaitAt_" + leg] = F4Gait();
                    // An ordered short motion sample: four frames about a twelfth of a second apart, for inspection.
                    for (var index = 1; index <= 4; index++) f4Shots.Enqueue("finalize4-gait", leg + "-" + index, Time.frameCount + 2 + (index - 1) * 5);
                    f4Motion[leg] = new JArray();
                }
                JArray trace;
                if (f4Motion.TryGetValue(leg, out trace) && trace.Count < 40) trace.Add(F4MotionSample());
            }
            f4Shots.Pump(Time.frameCount);
        }

        private readonly Dictionary<string, int> f4MovingFrames = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, JArray> f4Motion = new Dictionary<string, JArray>(StringComparer.Ordinal);

        // Velocity, command limit, gait and the locomotion playback driver at one frame (presentation is read, never written).
        private JObject F4MotionSample()
        {
            var agent = secondMount.View.MovementAgent; var manager = secondMount.View.AnimationManager;
            var command = secondMount.Commands.GetCommand(Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Move);
            var loco = manager.ActiveActions.FirstOrDefault(a => a.Action is Kingmaker.Visual.Animation.Kingmaker.Actions.UnitAnimationActionLocoMotion);
            return new JObject { ["frame"] = Time.frameCount, ["deltaTime"] = Time.deltaTime, ["position"] = Point(secondMount.Position),
                ["agentSpeed"] = agent.Speed, ["speedLimit"] = command?.SpeedLimit, ["walkSpeedType"] = manager.WalkSpeedType.ToString(),
                ["animationSpeed"] = manager.Speed, ["locoAction"] = loco?.Action?.name, ["locoAnimation"] = loco?.ActiveAnimation?.GetType().Name };
        }

        // The genuine slow control: the native Slowed condition halves the Horse's own speed (CalculateSpeedModifier), the
        // regime in which the native gait is Slow and the F4 presentation must leave it so. Disclosed setup, removed after.
        private bool f4SlowedApplied;

        private void BeginFinalize4SlowLeg()
        {
            Require(!f4SlowedApplied, "The slowed control runs once.");
            var before = secondMount.ModifiedSpeedMps;
            secondMount.Descriptor.State.AddCondition(Kingmaker.UnitLogic.UnitCondition.Slowed);
            f4SlowedApplied = true;
            ((JArray)f4["disclosedSetup"]).Add("native Slowed condition on the Horse for the slow-control leg only (removed after the leg)");
            f4["slowControl"] = new JObject { ["modifiedSpeedBefore"] = before, ["modifiedSpeedSlowed"] = secondMount.ModifiedSpeedMps,
                ["blueprintSpeedMps"] = secondMount.Blueprint.Speed.Meters / 3f };
            BeginSpeedLeg("mounted-slowed", SpeedLegSelection.SecondRiderAlone);
        }

        private void EndFinalize4SlowLeg()
        {
            if (!f4SlowedApplied) return;
            secondMount.Descriptor.State.RemoveCondition(Kingmaker.UnitLogic.UnitCondition.Slowed);
            f4SlowedApplied = false;
            if (f4["slowControl"] is JObject slow) { slow["modifiedSpeedAfter"] = secondMount.ModifiedSpeedMps; slow["removed"] = true; }
        }

        private JObject F4Gait()
        {
            var agent = secondMount.View.MovementAgent; var manager = secondMount.View.AnimationManager;
            var command = secondMount.Commands.GetCommand(Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Move);
            return new JObject { ["walkSpeedType"] = manager.WalkSpeedType.ToString(), ["animationSpeed"] = manager.Speed, ["agentSpeed"] = agent.Speed,
                ["speedLimit"] = command?.SpeedLimit, ["movementType"] = command?.MovementType.ToString(), ["currentSpeedMps"] = secondMount.CurrentSpeedMps,
                ["modifiedSpeedMps"] = secondMount.ModifiedSpeedMps, ["blueprintSpeedMps"] = secondMount.Blueprint.Speed.Meters / 3f,
                ["trotTicks"] = MountedAnimationAdapter.NaturalGaitTrotTicks, ["frame"] = Time.frameCount };
        }

        private void F4CaptureCommitted(MovementScreenshotCaptureRequest capture, byte[] bytes)
        {
            var folder = Path.Combine(request.EvidenceRoot, "movement-visuals"); Directory.CreateDirectory(folder);
            var name = "finalize4-gait-" + capture.Milestone + ".png";
            var file = Path.Combine(folder, name);
            if (File.Exists(file)) throw new InvalidOperationException("F4 gait capture already exists.");
            File.WriteAllBytes(file, bytes);
            string sha;
            using (var algorithm = SHA256.Create()) sha = BitConverter.ToString(algorithm.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
            f4Captures.Add(new JObject { ["leg"] = capture.Milestone, ["file"] = "movement-visuals/" + name, ["sha256"] = sha, ["bytes"] = bytes.Length,
                ["frame"] = Time.frameCount, ["gait"] = F4Gait() });
        }

        private void CompleteFinalize4()
        {
            if (f4 == null) return;
            f4["trotTicksTotal"] = MountedAnimationAdapter.NaturalGaitTrotTicks;
            f4["lastTrotSpeedLimit"] = MountedAnimationAdapter.LastNaturalGaitSpeedLimit;
            f4["motion"] = new JObject(f4Motion.Select(pair => new JProperty(pair.Key, pair.Value)));
            DisposeFinalize4();
        }

        private void DisposeFinalize4()
        {
            if (f4SlowedApplied && secondMount != null) EndFinalize4SlowLeg();
            if (f4Loot != null) { f4Loot.Dispose(); f4Loot = null; }
            if (f4Shots != null) { f4Shots.Dispose(); f4Shots = null; }
            if (f4Group != null && f4Group.gameObject.activeSelf && ReferenceEquals(Game.Instance?.UI?.Canvas?.GroupManager, f4Group)) f4Group.Close();
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b) { a.y = 0f; b.y = 0f; return Vector3.Distance(a, b); }

        private static Vector3 Vec(JObject point) => new Vector3((float)point["x"], (float)point["y"], (float)point["z"]);

        // The native loot interaction event (LootComponent.OnInteract raises it once per opened interaction).
        private sealed class Finalize4LootObserver : ILootInterractionHandler, IDisposable
        {
            private readonly IDisposable subscription;
            internal readonly List<KeyValuePair<string, int>> Events = new List<KeyValuePair<string, int>>();
            internal Finalize4LootObserver() { subscription = EventBus.Subscribe(this); }
            public void HandleLootInterraction(UnitEntityData unit, ItemsCollection loot, string lootContainerName) => Events.Add(new KeyValuePair<string, int>(unit?.UniqueId, Time.frameCount));
            public void HandleLootInterraction(UnitEntityData unit, EntityViewBase[] objects, LootContainerType containerType, Action closeCallback) => Events.Add(new KeyValuePair<string, int>(unit?.UniqueId, Time.frameCount));
            public void HandleZoneLootInterraction(AreaTransition areaTransition) { }
            public void Dispose() => subscription?.Dispose();
        }
    }
}
