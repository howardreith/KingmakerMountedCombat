using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Kingmaker;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.Selection;
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
    // One bounded combat checkpoint owned by the surrounding guarded save case.
    // Save/load, native archive eligibility and worker custody remain outside it.
    internal sealed class Chunk7CombatCheckpointDriver : IDisposable
    {
        private readonly RuntimeRequest request;
        private readonly MountedWorldService world;
        private readonly NativeMountedControlService controls;
        private readonly IModLogger logger;
        private readonly Chunk7CombatCheckpointKind kind;
        private readonly Chunk7OwnedPairFixture inherited;
        private UnitEntityData[] NativeParty => inherited?.Party ?? actors;
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private readonly JObject evidence = new JObject();
        private readonly JArray continuation = new JArray(), cleanupErrors = new JArray();
        private readonly List<UnitCommand> issued = new List<UnitCommand>();
        private readonly HashSet<UnitEntityData> independent = new HashSet<UnitEntityData>();
        private readonly Dictionary<UnitEntityData, int> grantsBefore = new Dictionary<UnitEntityData, int>();
        private Chunk7CombatScenario source;
        private UnitEntityData[] actors;
        private MountedPairServices[] retainedPairs;
        private UnitEntityData target;
        private Kingmaker.UnitLogic.Groups.UnitGroup targetGroup;
        private string targetId, targetGroupId;
        private NativeActorAllocationTrace trace;
        private NativeTurnCompletionProbe completion;
        private NativeRoundBoundaryProbe rounds;
        private MountedArchiveData saved;
        private TurnController originalTurn, endedTurn;
        private UnitMoveTo movement;
        private UnitEntityData movingActor;
        private Vector3 moveStart;
        private JObject movementBefore;
        private double begunSeconds;
        private int stage, continuationOffset, sourceRound;
        private bool cold, ready, beganContinuation, continuationComplete, cleanup, cleanupComplete, disposed, ownsObservers, repeatedCleanup, riderReentryObserved;
        internal bool Ready => ready;
        internal bool ContinuationComplete => continuationComplete;
        internal bool CleanupComplete => cleanupComplete;

        internal Chunk7CombatCheckpointDriver(RuntimeRequest request, MountedWorldService world,
            NativeMountedControlService controls, IModLogger logger, Chunk7CombatCheckpointKind kind, Chunk7OwnedPairFixture inherited = null)
        { this.request = request; this.world = world; this.controls = controls; this.logger = logger; this.kind = kind; this.inherited = inherited; }

        internal void StartSource(UnitEntityData[] selectedActors)
        {
            Require(source == null && actors == null && !disposed, "Checkpoint source already has an owner.");
            actors = selectedActors?.ToArray();
            source = new Chunk7CombatScenario(request, world, controls, logger, kind, inherited);
            source.StartCheckpointSource(actors);
            trace = source.CheckpointTrace; completion = source.CheckpointCompletion; rounds = source.CheckpointRounds;
            target = source.CheckpointTarget; targetId = target?.UniqueId; targetGroupId = target?.Group?.Id;
        }
        internal bool Update()
        {
            Require(source != null && !cold && !beganContinuation && !cleanup, "Checkpoint source is not updateable.");
            source.Update();
            if (source.Errors.Count != 0) throw new InvalidOperationException(string.Join(" | ", source.Errors));
            ready = source.CheckpointReady;
            if (ready) { targetId = target.UniqueId; targetGroupId = target.Group.Id; }
            return ready;
        }
        internal void BindCold(UnitEntityData[] selectedActors, MountedArchiveData archive)
        {
            Require(actors == null && source == null && !disposed && archive?.Combat != null && selectedActors?.Length == 4,
                "Cold checkpoint binding is absent or already owned.");
            actors = selectedActors.ToArray(); cold = true; saved = archive;
            if (inherited != null) { inherited.RequirePreserved(); Require(actors.SequenceEqual(inherited.Actors), "Cold checkpoint borrowed foreign inherited actor references."); }
            Require(actors.All(a => a != null && a.IsInState) && actors.Distinct().Count() == 4 &&
                (inherited != null || actors[0].UniqueId == Chunk7WorldScenarioEngine.MammothRiderId && actors[1].UniqueId == Chunk7WorldScenarioEngine.MammothId &&
                actors[2].UniqueId == Chunk7WorldScenarioEngine.HorseRiderId && actors[3].Blueprint.AssetGuid == HorseCompanionBlueprintService.UnitGuid),
                "Cold checkpoint actors differ from the exact saved fixture.");
            // Read only: the archive plus native singleton group/faction marker
            // supplies custody. No enemy/AI/initiative/progression is provisioned.
            var foreign = archive.Combat.Actors.Where(a => NativeParty.All(x => x.UniqueId != a.Native.Id))
                .Select(a => Game.Instance.State.Units.Single(u => u.UniqueId == a.Native.Id)).ToArray();
            Require(foreign.Length == 1, "Cold combat does not contain exactly one source-owned diagnostic actor.");
            target = foreign[0]; targetGroup = target.Group;
            Require(target.IsInState && target.IsInCombat && target.IsEnemy(actors[0]) &&
                target.Blueprint.AssetGuid == KingmakerMountedPairRuntime.MammothBlueprintGuid &&
                target.Faction != null && MountedSaveData.HexId(target.Faction.AssetGuid) &&
                target.Stats.HitPoints.BaseValue == 256 && target.Descriptor.State.IsConscious &&
                targetGroup != null && !targetGroup.IsPlayerParty && targetGroup.Count == 1 && ReferenceEquals(targetGroup[0], target) &&
                targetGroup.Id.StartsWith("KMC.RuntimeHostile.", StringComparison.Ordinal) &&
                targetGroup.Id.EndsWith(":two-pair-target", StringComparison.Ordinal) &&
                Game.Instance.UnitGroupsController.Groups.Contains(targetGroup),
                "Cold combat target lacks the complete durable source-owned native marker.");
            targetId = target.UniqueId; targetGroupId = targetGroup.Id;
            try
            {
                trace = new NativeActorAllocationTrace(actors[0], actors[1], world.Primary.Combat, deferInstallation: true) { ObserveReactionResources = false };
                ownsObservers = true;
                trace.Install();
                if (kind == Chunk7CombatCheckpointKind.TurnBasedDebts)
                {
                    completion = new NativeTurnCompletionProbe(trace, world); completion.Install();
                }
                rounds = new NativeRoundBoundaryProbe(); rounds.Install(); trace.BeginEncounter(request.RunId + ":cold-checkpoint-continuation");
            }
            catch (Exception acquisitionError)
            {
                try { DisposeObservers(); }
                catch (Exception cleanupError)
                { throw new AggregateException("Cold observer acquisition failed and retains rollback custody.", acquisitionError, cleanupError); }
                throw;
            }
            ready = true; evidence["coldBinding"] = Snapshot("read-only-cold-bind");
        }
        internal void ValidateArchive(MountedArchiveData archive)
        {
            Require(ready && archive != null && archive.Combat != null, "Native checkpoint archive is absent or source is not ready.");
            archive.Validate(); var tb = kind != Chunk7CombatCheckpointKind.RealTimeDebts;
            var zero = kind == Chunk7CombatCheckpointKind.ZeroMountedRetained; var pending = kind == Chunk7CombatCheckpointKind.MountedPendingOnly;
            Require(archive.SchemaVersion == 3 && archive.Combat.TurnBased == tb && archive.Combat.Paired == null &&
                archive.Pairs.Length == (zero ? 0 : pending ? 1 : 2) && archive.Participations.Length == (pending ? 1 : 2) &&
                archive.Combat.Actors.Length == NativeParty.Length + 1 && NativeParty.All(a => archive.Combat.Actors.Any(x => x.Native.Id == a.UniqueId)) &&
                archive.Combat.Actors.Any(a => a.Native.Id == targetId), "Archive does not preserve the complete selected native world.");
            Func<UnitEntityData, SavedNativeActor> debt = actor => archive.Combat.Actors.Single(a => a.Native.Id == actor.UniqueId).Native;
            if (pending)
            {
                var p = archive.Participations.Single(); var s = p.State;
                Require(archive.Pairs[0].Rider.Id == actors[0].UniqueId && archive.Pairs[0].Mount.Id == actors[1].UniqueId &&
                    p.MountedPairId == p.OwnerId && s.RiderId == null && s.MountId == null && s.Activation == null &&
                    !s.BoundaryIsCurrent && s.Boundary == null && s.Partner == null && s.PendingSplitId == actors[1].UniqueId &&
                    s.PendingSplitRound == archive.Combat.Round && debt(actors[1]).Standard > .1f &&
                    !archive.Combat.Actors.Single(x => x.Native.Id == actors[0].UniqueId).InCombat,
                    "Migration checkpoint invented an activation or dropped pending mount debt.");
            }
            else if (zero)
                Require(archive.Participations.All(p => p.MountedPairId == null && p.State.Activation?.Split == true &&
                    p.State.SplitReleaseRound == archive.Combat.Round), "Zero-mounted archive dropped original split ownership.");
            else if (tb)
            {
                // Completed A keeps its native end expenditure, decayed natively by
                // the game time that later turns consumed before the save.
                var first = debt(actors[0]);
                Require(archive.Combat.Current?.ActorId == actors[2].UniqueId && first.Standard > .1f && first.Standard <= 6.001f &&
                    debt(actors[2]).Standard > .1f && debt(actors[3]).Standard == 0f &&
                    archive.Participations.Count(p => p.State.BoundaryIsCurrent) == 1,
                    "TB archive did not retain completed A and current B's distinct native expenditure: current=" + archive.Combat.Current?.ActorId +
                    " A=" + first.Standard + " B=" + debt(actors[2]).Standard + " mountB=" + debt(actors[3]).Standard +
                    " currentBoundaries=" + archive.Participations.Count(p => p.State.BoundaryIsCurrent) + ".");
                if (!cold) RequireCompletedDecay(first, archive);
            }
            else
                Require(debt(actors[0]).Standard > .1f && debt(actors[3]).Standard > .1f && debt(actors[2]).Standard == 0f &&
                    archive.Combat.Current == null && archive.Combat.Roster.Length == 0 && archive.Participations.All(p => p.State.Activation == null),
                    "RT archive lost its two native expenditures or invented a TB context.");
            if (!cold)
                foreach (var actor in actors)
                {
                    var current = MountedPersistenceService.CaptureActor(actor); var prior = debt(actor);
                    var elapsed = (Game.Instance.TimeController.GameTime.Ticks - archive.GameTimeTicks) / (double)TimeSpan.TicksPerSecond;
                    Require(elapsed >= 0 && elapsed <= 30 && current.Standard <= prior.Standard + .001f &&
                        current.Standard + elapsed * 1.1 + .001 >= prior.Standard && current.Move <= prior.Move + .001f &&
                        current.Move + elapsed * 1.1 + .001 >= prior.Move && current.ReactionsRemaining == prior.ReactionsRemaining,
                        "Source native save changed actor expenditure beyond native elapsed time.");
                }
            evidence["archiveValidated"] = true; evidence["archiveRound"] = archive.Combat.Round;
            evidence["archiveDebt"] = new JArray(actors.Select(a => JObject.FromObject(debt(a), MountedSaveCodec.CreateSerializer())));
        }
        private void RequireCompletedDecay(SavedNativeActor first, MountedArchiveData archive)
        {
            var ends = ((JArray)completion.Capture()["calls"]).Where(c => (string)c["kind"] == "end" &&
                (string)c["before"]?["actorId"] == actors[0].UniqueId && (bool?)c["complete"] == true).ToArray();
            Require(ends.Length == 1, "Completed A has no single observed native End.");
            var end = ends[0]["after"]; var endStandard = (double)end["resources"]["standard"];
            var elapsed = (archive.GameTimeTicks - (long)end["gameTicks"]) / (double)TimeSpan.TicksPerSecond;
            Require(endStandard >= 5.9 && elapsed >= 0 && elapsed <= 30 && first.Standard <= endStandard + .001 &&
                first.Standard + elapsed * 1.1 + .001 >= endStandard,
                "Completed A's archived expenditure is not its native end value decayed by elapsed native time: end=" + endStandard +
                " archived=" + first.Standard + " elapsed=" + elapsed + ".");
        }
        internal void BeginContinuation(MountedArchiveData archive)
        {
            Require(ready && !beganContinuation && !cleanup && !world.SerializationBusy, "Continuation is not ready or save still owns native work.");
            ValidateArchive(archive); saved = archive; beganContinuation = true;
            begunSeconds = clock.Elapsed.TotalSeconds;
            continuationOffset = trace.EventCount; originalTurn = Game.Instance.TurnBasedCombatController.CurrentTurn; sourceRound = archive.Combat.Round;
            foreach (var actor in actors) grantsBefore.Add(actor, trace.GrantCount(actor));
            retainedPairs = archive.Participations.Select(p => world.Pairs.Single(x => x.Relationship.PairOwner?.Id == Guid.Parse(p.OwnerId) ||
                // Legacy schema-2 migration mints deterministic IDs while this
                // source process still owns its original reviewed native owner.
                !cold && kind == Chunk7CombatCheckpointKind.MountedPendingOnly && ReferenceEquals(x, world.Primary))).ToArray();
            continuation.Add(Snapshot("continuation-before-input"));
            if (kind == Chunk7CombatCheckpointKind.ZeroMountedRetained) rounds.AfterNativeRound = RepeatDetachedCleanupAfterNativeRound;
            Game.Instance.IsPaused = false;
        }
        internal bool UpdateContinuation()
        {
            Require(beganContinuation && !cleanup, "Native continuation was not begun.");
            if (continuationComplete) return true;
            Require(clock.Elapsed.TotalSeconds - begunSeconds <= 120, "Native continuation exceeded its selected bounded deadline.");
            source?.RefreshCheckpointEncounter(); CheckObservers();
            if (Game.Instance.IsPaused) Game.Instance.IsPaused = false;
            switch (kind)
            {
                case Chunk7CombatCheckpointKind.RealTimeDebts: TickRealtimeContinuation(); break;
                case Chunk7CombatCheckpointKind.TurnBasedDebts: TickTurnBasedContinuation(); break;
                default: TickIndependentContinuation(); break;
            }
            return continuationComplete;
        }
        private void TickRealtimeContinuation()
        {
            Require(!CombatController.IsInTurnBasedCombat() && Game.Instance.Player.IsInCombat &&
                actors.All(a => trace.GrantCount(a) == grantsBefore[a]), "RT continuation manufactured a turn grant or lost combat.");
            if (stage == 0)
            {
                var elapsed = (Game.Instance.TimeController.GameTime.Ticks - saved.GameTimeTicks) / (double)TimeSpan.TicksPerSecond;
                foreach (var actor in new[] { actors[0], actors[3] })
                {
                    var debt = saved.Combat.Actors.Single(a => a.Native.Id == actor.UniqueId).Native.Standard;
                    var current = actor.CombatState.Cooldown.StandardAction;
                    Require(current <= debt + .001 && current + elapsed * 1.1 + .01 >= debt,
                        "RT debt disappeared faster than native time or increased without input.");
                }
                if (!actors[0].HasStandardAction() || !actors[3].HasStandardAction() ||
                    actors[0].CombatState.Cooldown.StandardAction > .001f || actors[3].CombatState.Cooldown.StandardAction > .001f) return;
                continuation.Add(Snapshot("native-rt-readiness")); BeginMovement(actors[2], actors[3]); stage = 1; return;
            }
            if (!MovementCompleted()) return;
            Require(actors[2].CombatState.Cooldown.StandardAction == 0f &&
                actors.All(a => trace.GrantCount(a) == grantsBefore[a]), "RT movement invented standard debt or a native preparation.");
            CompleteContinuation();
        }
        private void TickTurnBasedContinuation()
        {
            var turn = Game.Instance.TurnBasedCombatController.CurrentTurn;
            if (stage == 0)
            {
                Require(ReferenceEquals(turn, originalTurn) && turn?.Unit == actors[2] && Chunk7NativeFixture.ReadyForTurnInput(turn) &&
                    world.ForActor(actors[2])?.Participant.PartnerContext != null && actors[3].HasMoveAction(),
                    "Restored native turn has no eligible exact saved partner context.");
                BeginMovement(actors[2], actors[3]); stage = 1; return;
            }
            if (stage == 1)
            {
                if (!MovementCompleted()) return;
                Require(ReferenceEquals(turn, originalTurn) && actors.All(a => trace.GrantCount(a) == grantsBefore[a]) &&
                    actors[2].CombatState.Cooldown.StandardAction == saved.Combat.Actors.Single(a => a.Native.Id == actors[2].UniqueId).Native.Standard &&
                    actors[3].CombatState.Cooldown.StandardAction == 0f,
                    "Saved partner movement refreshed a grant, changed current context or transferred standard expenditure.");
                continuation.Add(Snapshot("saved-partner-movement-no-prepare")); stage = 2;
            }
            if (stage == 2) { if (EndNative(turn)) stage = 3; return; }
            if (ReferenceEquals(turn, originalTurn)) return;
            var calls = (JArray)completion.Capture()["calls"];
            Require(new[] { actors[2], actors[3] }.All(actor => calls.Any(c => (string)c["kind"] == "end" &&
                (string)c["before"]?["actorId"] == actor.UniqueId && (bool?)c["complete"] == true &&
                (int?)c["before"]?["allocationSequence"] >= continuationOffset)),
                "Native continuation did not complete both exact restored actor contexts.");
            CompleteContinuation();
        }
        private void TickIndependentContinuation()
        {
            var controller = Game.Instance.TurnBasedCombatController; var turn = controller.CurrentTurn;
            var pending = kind == Chunk7CombatCheckpointKind.MountedPendingOnly;
            var expected = pending ? cold ? new[] { actors[0], actors[1] } : new[] { actors[1] } :
                new[] { actors[1], actors[3] };
            if (pending && cold)
            {
                var rider = actors[0];
                Require(trace.GrantCount(rider) <= grantsBefore[rider] + (controller.RoundNumber > sourceRound ? 1 : 0),
                    "Retained rider received a duplicate or early native turn grant.");
                if (controller.RoundNumber == sourceRound)
                    Require(!rider.IsInCombat && !rider.CombatState.Prepared && trace.GrantCount(rider) == grantsBefore[rider],
                        "Retained rider rejoined or prepared within its saved allocation.");
                else if (!riderReentryObserved && rider.IsInCombat && rider.CombatState.Prepared)
                {
                    Require(controller.RoundNumber == sourceRound + 1 &&
                        rider.CombatState.Cooldown.StandardAction <= .001f &&
                        rider.CombatState.Cooldown.MoveAction <= .001f &&
                        rider.CombatState.Cooldown.SwiftAction <= .001f,
                        "Retained rider did not receive ordinary native preparation at the new round.");
                    riderReentryObserved = true;
                    continuation.Add(Snapshot("native-rider-reentry-new-round"));
                }
            }
            foreach (var actor in expected)
                if (turn?.Unit == actor && Chunk7NativeFixture.ReadyForTurnInput(turn) && !independent.Contains(actor))
                {
                    Require(controller.RoundNumber == sourceRound + 1 && trace.GrantCount(actor) == grantsBefore[actor] + 1 &&
                        (pending || repeatedCleanup) && (!pending || !cold || riderReentryObserved),
                        "Pending native actor was released early/late or received duplicate native preparation.");
                    independent.Add(actor); continuation.Add(Snapshot("independent-native-actor-" + actor.UniqueId));
                }
            if (independent.Count == expected.Length) { CompleteContinuation(); return; }
            Require(controller.RoundNumber <= sourceRound + 1, "Pending native mount skipped its first legitimate independent round.");
            EndNative(turn);
        }
        private void RepeatDetachedCleanupAfterNativeRound(CombatController controller)
        {
            if (repeatedCleanup || controller.RoundNumber <= sourceRound) return;
            Require(controller.RoundNumber == sourceRound + 1, "Repeated cleanup missed the exact native N+1 boundary.");
            var before = Snapshot("duplicate-detached-cleanup-before");
            foreach (var pair in retainedPairs)
            {
                Require(pair.Relationship.PairOwner != null && pair.Participant.CaptureParticipation()?.SplitReleaseRound == sourceRound,
                    "Original detached owner disappeared before repeated cleanup observation.");
                var result = pair.Relationship.Dismount(CleanupTrigger.Incapacitated);
                Require(result.Succeeded && pair.Participant.CaptureParticipation()?.SplitReleaseRound == sourceRound,
                    "Repeated cleanup restarted its retained split release round.");
            }
            var after = Snapshot("duplicate-detached-cleanup-after");
            Require(JToken.DeepEquals(before["nativeActors"], after["nativeActors"]), "Repeated detached cleanup changed native actor debt.");
            continuation.Add(new JObject { ["stimulus"] = "repeat cleanup at actual native StartRound return; no round/resource mutation",
                ["before"] = before, ["after"] = after });
            repeatedCleanup = true; rounds.AfterNativeRound = null;
        }
        private void BeginMovement(UnitEntityData selected, UnitEntityData executor)
        {
            Require(actors.All(a => a.Commands.Empty) && !world.Pairs.Any(p => p.Combat.HasActiveCommand || p.Combat.HasActiveGroundMovement),
                "Continuation movement cannot replace existing native work.");
            Chunk7NativeFixture.Select(selected); Game.Instance.IsPaused = true;
            moveStart = executor.Position; movingActor = executor; movementBefore = Snapshot("continuation-move-before");
            var destination = moveStart + Vector3.forward * .5f;
            using (var input = new NativeOrdinaryAttackInput(destination))
            {
                input.Predict(); var turn = Game.Instance.TurnBasedCombatController.CurrentTurn; var changes = 0;
                while (CombatController.IsInTurnBasedCombat() && (turn.EnabledFiveFootStep || turn.EnabledSingleActionMove) && changes++ < 4)
                { input.Click(button: 1); input.Predict(); }
                Require(input.Click(), "Native continuation ground input was refused.");
            }
            movement = executor.Commands.Move as UnitMoveTo;
            Require(movement?.Executor == executor && actors.Where(a => a != executor).All(a => a.Commands.Move == null),
                "Native continuation movement changed another actor's command container.");
            issued.Add(movement); evidence["movementCommand"] = trace.ObjectIdentity(movement);
            evidence["movementExecutorId"] = executor.UniqueId;
            Chunk7NativeFixture.Select(actors[0]); Game.Instance.IsPaused = false;
        }
        private bool MovementCompleted()
        {
            if (!movement.IsFinished || movingActor.View.MovementAgent.IsReallyMoving ||
                world.Pairs.Any(p => p.Combat.HasActiveGroundMovement || p.Combat.HasActiveCommand)) return false;
            var after = Snapshot("continuation-move-after"); var distance = Vector3.Distance(moveStart, movingActor.Position);
            Require(distance > .02f, "Native continuation movement did not move the saved eligible actor.");
            evidence["movement"] = new JObject { ["before"] = movementBefore, ["after"] = after, ["distance"] = distance,
                ["executorId"] = movingActor.UniqueId, ["command"] = trace.ObjectIdentity(movement), ["terminal"] = true };
            return true;
        }
        private bool EndNative(TurnController turn)
        {
            if (turn?.Unit == null || ReferenceEquals(turn, endedTurn) || !NativeParty.Contains(turn.Unit) ||
                !turn.Unit.IsDirectlyControllable || !turn.Unit.Commands.Empty || turn.Unit.AreHandsBusyWithAnimation ||
                !turn.CanEndTurnAndNoActing() || Game.Instance.TurnBasedCombatController.WaitingForUI ||
                MountedPairTurnParticipant.NativeNextCandidate(Game.Instance.TurnBasedCombatController) != null ||
                world.Pairs.Any(p => p.Combat.HasActiveCommand || p.Combat.HasActiveGroundMovement)) return false;
            trace.Record("chunk7-continuation-native-end-before", turn.Unit); Game.Instance.PauseBind();
            trace.Record("chunk7-continuation-native-end-after", turn.Unit); endedTurn = turn; return true;
        }
        private void CompleteContinuation()
        { CheckObservers(); continuation.Add(Snapshot("continuation-complete")); continuationComplete = true; Game.Instance.IsPaused = true; }
        private void CheckObservers()
        {
            Require(trace.Complete && (completion == null || ((JArray)completion.Capture()["errors"]).Count == 0) &&
                (rounds == null || ((JArray)rounds.Capture()["errors"]).Count == 0), "Native continuation observation is incomplete.");
        }
        private JObject Snapshot(string label) => new JObject { ["label"] = label, ["frame"] = Time.frameCount,
            ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks, ["round"] = Game.Instance.TurnBasedCombatController.RoundNumber,
            ["currentActor"] = Game.Instance.TurnBasedCombatController.CurrentTurn?.Unit?.UniqueId,
            ["nativeActors"] = new JArray((actors ?? new UnitEntityData[0]).Select(a => JObject.FromObject(MountedPersistenceService.CaptureActor(a), MountedSaveCodec.CreateSerializer()))),
            ["positions"] = new JArray((actors ?? new UnitEntityData[0]).Select(a => new JObject { ["actorId"] = a.UniqueId,
                ["x"] = a.Position.x, ["y"] = a.Position.y, ["z"] = a.Position.z })),
            ["grants"] = new JArray((actors ?? new UnitEntityData[0]).Select(a => new JObject { ["actorId"] = a.UniqueId, ["count"] = trace?.GrantCount(a) })),
            ["participations"] = new JArray(world.Pairs.Select(p => p.Participant.CaptureParticipation()).Where(p => p != null)
                .Select(p => JObject.FromObject(p, MountedSaveCodec.CreateSerializer()))) };
        internal JObject Capture()
        {
            var value = source?.CaptureCheckpoint() ?? new JObject();
            value["ready"] = ready; value["continuationComplete"] = continuationComplete; value["kind"] = kind.ToString();
            value["actorIds"] = new JArray((actors ?? new UnitEntityData[0]).Select(a => a.UniqueId));
            value["targetId"] = targetId; value["targetGroupId"] = targetGroupId; value["provenance"] = cold ? "cold-native-archive-binding" : "source-native-setup";
            value["continuationTraceOffset"] = continuationOffset; value["continuation"] = continuation.DeepClone();
            value["trace"] = trace?.Capture() ?? evidence["finalTrace"]?.DeepClone();
            value["nativeCompletion"] = completion?.Capture() ?? evidence["finalCompletion"]?.DeepClone();
            value["nativeRounds"] = rounds?.Capture() ?? evidence["finalRounds"]?.DeepClone();
            value["driver"] = evidence.DeepClone(); value["cleanupComplete"] = cleanupComplete; value["cleanupErrors"] = cleanupErrors.DeepClone();
            return value;
        }
        internal void BeginCleanup()
        {
            if (!cleanup)
            {
                cleanup = true; begunSeconds = clock.Elapsed.TotalSeconds;
                if (rounds != null) rounds.AfterNativeRound = null;
                evidence["finalTrace"] = trace?.Capture(); evidence["finalCompletion"] = completion?.Capture(); evidence["finalRounds"] = rounds?.Capture();
            }
            // Beginning cleanup only captures ownership; worker and queued-load
            // admission is checked before releasing observers or unpausing.
            if (source != null) source.BeginCheckpointCleanup();
        }
        internal bool UpdateCleanup()
        {
            Require(cleanup, "Checkpoint cleanup was not begun.");
            if (cleanupComplete) return true;
            if (world.SerializationBusy || LoadingProcess.Instance.IsLoadingInProcess ||
                LoadingProcess.Instance.QueuedNames.Any() || NativePersistenceIsolation.HasPendingWrites) return false;
            if (source != null)
            {
                if (!source.UpdateCheckpointCleanup()) return false;
                Require(source.CheckpointCleanupComplete, "Source checkpoint cleanup did not prove complete restoration.");
                source.Dispose(); trace = null; completion = null; rounds = null; cleanupComplete = true; return true;
            }
            DisposeObservers();
            Game.Instance.IsPaused = false;
            var failures = new List<Exception>();
            foreach (var command in issued.Where(c => c != null && !c.IsFinished))
                try { command.Interrupt(); } catch (Exception error) { failures.Add(error); }
            failures.AddRange(world.Visit(pair => pair.Combat.Cancel("Chunk 7 cold checkpoint cleanup")));
            if (failures.Count != 0)
            {
                foreach (var failure in failures) if (!cleanupErrors.Contains(failure.ToString())) cleanupErrors.Add(failure.ToString());
                throw new AggregateException("Cold checkpoint independently visited every command owner but retains failed cleanup.", failures);
            }
            if (issued.Any(c => c != null && (!c.IsFinished || c is UnitUseAbility &&
                    ((UnitUseAbility)c).ExecutionProcess != null && !((UnitUseAbility)c).ExecutionProcess.IsEnded)) ||
                world.Pairs.Any(p => p.Combat.HasActiveCommand || p.Combat.HasActiveGroundMovement || p.Combat.HasChargeOwnership || controls.HasUnsettledShellFor(p.Relationship)) ||
                world.UnifiedTurn.HasUnsettledPreparation || controls.HasUnsettledRelationshipTransition)
            { Require(clock.Elapsed.TotalSeconds - begunSeconds <= 30, "Cold native commands did not settle before target cleanup."); return false; }
            if (target != null && target.IsInState)
            {
                Require(target.UniqueId == targetId && target.Group == targetGroup && targetGroup.Id == targetGroupId &&
                    targetGroup.Count == 1 && ReferenceEquals(targetGroup[0], target), "Cold target cleanup lost its exact captured custody.");
                target.Commands.InterruptAll(); target.Destroy();
            }
            Game.Instance.EntityDestroyer.Tick();
            var actorFailures = new List<Exception>();
            foreach (var actor in NativeParty.Where(a => a.IsInState && a.IsInCombat))
                try { actor.LeaveCombat(); } catch (Exception error) { actorFailures.Add(error); }
            if (actorFailures.Count != 0)
            {
                foreach (var failure in actorFailures) if (!cleanupErrors.Contains(failure.ToString())) cleanupErrors.Add(failure.ToString());
                throw new AggregateException("Cold cleanup independently visited every captured actor and retains failed native cleanup.", actorFailures);
            }
            var targetGone = target == null || !target.IsInState && !Game.Instance.State.Units.Contains(target);
            if (targetGone && targetGroup != null && !targetGroup.Disposed)
            {
                Require(targetGroup.Empty() && !Game.Instance.UnitGroupsController.Groups.Any(g => g != targetGroup && g?.Id == targetGroupId),
                    "Captured diagnostic group contains an unowned actor or duplicate identity.");
                Game.Instance.UnitGroupsController.Groups.Remove(targetGroup); targetGroup.Dispose();
            }
            var settled = targetGone && (targetGroup == null || targetGroup.Disposed && !Game.Instance.UnitGroupsController.Groups.Contains(targetGroup)) &&
                !Game.Instance.Player.IsInCombat && !Game.Instance.TurnBasedCombatController.Initialized &&
                world.Pairs.All(p => !p.Combat.HasActiveCommand && !p.Combat.HasActiveGroundMovement && !controls.HasUnsettledShellFor(p.Relationship)) &&
                !world.UnifiedTurn.HasUnsettledPreparation && !controls.HasUnsettledRelationshipTransition;
            if (!settled)
            { Require(clock.Elapsed.TotalSeconds - begunSeconds <= 30, "Cold checkpoint cleanup retained native actor, group, combat or command custody."); return false; }
            evidence["coldCleanup"] = new JObject { ["targetId"] = targetId, ["targetGroupId"] = targetGroupId,
                ["targetRemoved"] = true, ["groupRemoved"] = true, ["borrowedFactionDestroyed"] = false,
                ["nativeCombatEnded"] = true, ["resourceFieldsWritten"] = false };
            cleanupComplete = true; return true;
        }
        private void DisposeObservers()
        {
            if (!ownsObservers) return;
            var failures = new List<Exception>();
            foreach (var release in new Action[] { () => { completion?.Dispose(); completion = null; },
                () => { rounds?.Dispose(); rounds = null; }, () => { trace?.Dispose(); trace = null; } })
                try { release(); } catch (Exception error) { if (!cleanupErrors.Contains(error.ToString())) cleanupErrors.Add(error.ToString()); failures.Add(error); }
            if (failures.Count != 0) throw new AggregateException("Checkpoint observer cleanup retained ownership.", failures);
            completion = null; rounds = null; trace = null; ownsObservers = false;
        }
        public void Dispose()
        {
            if (disposed) return;
            if (!cleanup) BeginCleanup();
            if (!UpdateCleanup()) throw new InvalidOperationException("Checkpoint cleanup is still retained; poll its bounded owner.");
            disposed = true;
        }
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
