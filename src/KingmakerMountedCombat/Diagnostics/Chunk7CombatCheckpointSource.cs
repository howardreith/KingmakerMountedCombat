using System;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Commands;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using KingmakerMountedCombat.Logging;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;

namespace KingmakerMountedCombat.Diagnostics
{
    internal enum Chunk7CombatCheckpointKind { RealTimeDebts, TurnBasedDebts, ZeroMountedRetained, MountedPendingOnly }

    // Reuses the same native input and per-actor spending proof as the combat
    // family. This mode owns only transient combat setup, never mounts or saves.
    internal sealed partial class Chunk7CombatScenario
    {
        private readonly bool checkpointOnly;
        private readonly Chunk7CombatCheckpointKind checkpointKind;
        private bool checkpointReady;
        private JObject checkpointSnapshot;
        private RealtimeAttackCheckpoint realtimeA, realtimeB;
        internal bool CheckpointReady => checkpointReady;
        internal bool CheckpointCleanupComplete => cleanupProven;
        internal NativeActorAllocationTrace CheckpointTrace => trace;
        internal NativeTurnCompletionProbe CheckpointCompletion => completion;
        internal NativeRoundBoundaryProbe CheckpointRounds => rounds;
        internal UnitEntityData CheckpointTarget => target;
        internal Chunk7CombatScenario(RuntimeRequest request, MountedWorldService world,
            NativeMountedControlService controls, IModLogger logger, Chunk7CombatCheckpointKind kind, Chunk7OwnedPairFixture inherited = null)
        {
            this.request = request; this.world = world; this.controls = controls; this.logger = logger;
            checkpointOnly = true; checkpointKind = kind; this.inherited = inherited;
        }
        internal void StartCheckpointSource(UnitEntityData[] selectedActors)
        {
            Require(!started && checkpointOnly && world != null && selectedActors != null && selectedActors.Length == 4,
                "Checkpoint source has invalid ownership or actors.");
            started = true; actors = selectedActors.ToArray(); originalParty = inherited?.Party ?? actors;
            if (inherited != null) { inherited.RequirePreserved(); Require(actors.SequenceEqual(inherited.Actors), "Checkpoint borrowed a foreign inherited pair boundary."); }
            ar = actors[0]; am = actors[1]; br = actors[2]; bm = actors[3];
            Require(actors.Distinct().Count() == 4 && actors.All(x => x != null && x.IsInState && Chunk7NativeFixture.Idle(x)) &&
                (inherited != null || ar.UniqueId == Chunk7WorldScenarioEngine.MammothRiderId && am.UniqueId == Chunk7WorldScenarioEngine.MammothId &&
                br.UniqueId == Chunk7WorldScenarioEngine.HorseRiderId && bm.Blueprint.AssetGuid == HorseCompanionBlueprintService.UnitGuid) &&
                ReferenceEquals(ar.Descriptor.Pet, am) && ReferenceEquals(br.Descriptor.Pet, bm) && !Game.Instance.Player.IsInCombat,
                "Checkpoint source must use the exact already acquired native fixture outside combat.");
            a = world.ForActor(ar); b = world.ForActor(br);
            Require(a != null && a.Relationship.State == RelationshipState.Mounted && a.Relationship.Mount == am &&
                (checkpointKind == Chunk7CombatCheckpointKind.MountedPendingOnly ? (b == null ||
                    b.Relationship.State == RelationshipState.Unmounted && b.Relationship.PairOwner == null && b.Participant.CanReleaseBoundary) && ReferenceEquals(a, world.Primary) :
                    b != null && b != a && b.Relationship.State == RelationshipState.Mounted && b.Relationship.Mount == bm),
                "Checkpoint source mounted owners differ from its declared shape.");
            if (checkpointKind == Chunk7CombatCheckpointKind.MountedPendingOnly) b = null;
            nativeScope = inherited?.Scope ?? new Chunk7NativeWorldScope(request, actors);
            pauseBefore = Game.Instance.IsPaused; captured = true;
            evidence["inputs"] = inputs; evidence["activations"] = activations; evidence["turnVisits"] = visits; evidence["snapshots"] = snapshots;
            evidence["scenario"] = request.Scenario; evidence["kind"] = checkpointKind.ToString();
            evidence["sourceSetup"] = "already mounted exact actors; native combat/initiative fixture only";
            try { PrepareEncounter(); Next(checkpointKind == Chunk7CombatCheckpointKind.RealTimeDebts ? 50 : 5); }
            catch (Exception error) { Fail("C7-checkpoint-source", error.ToString()); BeginCleanup(); throw; }
        }
        private sealed class RealtimeAttackCheckpoint
        {
            internal MountedPairServices Pair;
            internal UnitEntityData Actor;
            internal UnitUseAbility Shell;
            internal MountedPairAttackOutcome Previous;
            internal JObject Observation;
        }
        private RealtimeAttackCheckpoint CaptureRealtimeAttack(UnitEntityData actor) => new RealtimeAttackCheckpoint {
            Pair = actingPair, Actor = actor, Shell = attackShell, Previous = priorOutcome, Observation = attackObservation };
        private bool RealtimeAttackFinished(RealtimeAttackCheckpoint attack)
        {
            if (ReferenceEquals(attack.Pair.Combat.LastOutcome, attack.Previous) || attack.Pair.Combat.HasActiveCommand ||
                attack.Pair.Combat.HasActiveGroundMovement || !attack.Pair.Relationship.Rider.Commands.Empty ||
                !attack.Pair.Relationship.Mount.Commands.Empty || !attack.Shell.IsFinished ||
                attack.Shell.ExecutionProcess != null && !attack.Shell.ExecutionProcess.IsEnded) return false;
            if (attack.Observation["outcome"] != null) return true;
            var outcome = attack.Pair.Combat.LastOutcome;
            Require(outcome.Result == "Success" && outcome.ActorId == attack.Actor.UniqueId && outcome.ResourceOwnerId == attack.Actor.UniqueId &&
                outcome.TargetId == target.UniqueId && outcome.NativeAttackRuleObserved && outcome.NativeCompletedAttackCount == 1,
                "RT checkpoint primary did not complete one exact native attack.");
            attack.Observation["outcome"] = new JObject { ["actorId"] = outcome.ActorId, ["resourceOwnerId"] = outcome.ResourceOwnerId,
                ["targetId"] = outcome.TargetId, ["result"] = outcome.Result, ["nativeAttackRule"] = outcome.NativeAttackRuleObserved,
                ["completedAttacks"] = outcome.NativeCompletedAttackCount, ["singleAttack"] = outcome.SingleAttackMode };
            attack.Observation["after"] = WorldSnapshot("rt-attack-settled"); return true;
        }
        // The checkpoint needs both native 6 s RT debts outstanding at once. Pair A's target is
        // placed 3.5 m from its Mammoth, but pair B's Horse may need an unbounded native approach
        // from the saved start (F5 r11: 5.2 m, settled 6.75 s after input), during which a debt
        // created first would decay to zero. Pair B therefore attacks first and pair A only after
        // pair B's attack settled, and only once every actor's native initiative wait has elapsed,
        // so neither the approach length nor the initiative roll bounds the checkpoint; the
        // measured debts are recorded before the assertion. Nothing writes or refunds a cooldown.
        internal const string RealtimeCheckpointContract = "rt-debt-checkpoint-sequenced-v2";
        private const float RealtimeDebtMinimum = .1f;
        private static bool RealtimeIdle(UnitEntityData x) => x.Commands.Empty && !x.AreHandsBusyWithAnimation &&
            x.View?.MovementAgent != null && !x.View.MovementAgent.IsReallyMoving && !x.View.MovementAgent.WantsToMove;
        private void TickRealtimeCheckpoint()
        {
            Require(!CombatController.IsInTurnBasedCombat(), "RT checkpoint unexpectedly changed mode.");
            if (!Game.Instance.Player.IsInCombat || actors.Any(x => !x.IsInCombat)) return;
            if (realtimeB == null)
            {
                // The native RT initiative wait (0.2-5.2 s observed) must have elapsed for every actor:
                // F5 r11's Horse B waited 5.22 s before its approach even began.
                if (!actors.All(RealtimeIdle) || actors.Any(x => x.CombatState.IsWaitingInitiative) ||
                    world.Pairs.Any(p => p.Combat.HasActiveCommand || p.Combat.HasActiveGroundMovement) ||
                    !ar.HasStandardAction() || !bm.HasStandardAction()) return;
                actingPair = b; actingIndex = 1; BeginAttack(true, "rt-pair-b-mount-primary"); realtimeB = CaptureRealtimeAttack(bm);
                return;
            }
            if (realtimeA == null)
            {
                if (!RealtimeAttackFinished(realtimeB) || !RealtimeIdle(ar) || !RealtimeIdle(am) ||
                    ar.CombatState.IsWaitingInitiative || am.CombatState.IsWaitingInitiative ||
                    a.Combat.HasActiveCommand || a.Combat.HasActiveGroundMovement || !ar.HasStandardAction()) return;
                actingPair = a; actingIndex = 0; BeginAttack(false, "rt-pair-a-rider-primary"); realtimeA = CaptureRealtimeAttack(ar);
                return;
            }
            var aFinished = RealtimeAttackFinished(realtimeA); var bFinished = RealtimeAttackFinished(realtimeB);
            if (!aFinished || !bFinished) return;
            var holds = ar.CombatState.Cooldown.StandardAction > RealtimeDebtMinimum && bm.CombatState.Cooldown.StandardAction > RealtimeDebtMinimum &&
                br.CombatState.Cooldown.StandardAction == 0f && am.CombatState.Cooldown.StandardAction == 0f;
            evidence["realtimeCheckpoint"] = new JObject { ["contract"] = RealtimeCheckpointContract,
                ["order"] = new JArray("rt-pair-b-mount-primary", "rt-pair-a-rider-primary"), ["minimumDebt"] = RealtimeDebtMinimum,
                ["debts"] = new JArray(actors.Select(x => new JObject { ["actorId"] = x.UniqueId, ["standard"] = x.CombatState.Cooldown.StandardAction })),
                ["snapshot"] = WorldSnapshot("rt-debt-checkpoint"), ["holds"] = holds };
            Require(holds, "Real native attack timings did not leave the declared distinct RT debt checkpoint.");
            MarkCheckpointReady();
        }
        private void MarkCheckpointReady()
        {
            Require(checkpointOnly && !checkpointReady && trace.Complete && !controls.HasUnsettledRelationshipTransition &&
                !world.UnifiedTurn.HasUnsettledPreparation && world.Pairs.All(p => !p.Combat.HasActiveCommand && !p.Combat.HasActiveGroundMovement),
                "Checkpoint still owns a command, preparation or incomplete observation.");
            Game.Instance.IsPaused = true;
            if (checkpointKind == Chunk7CombatCheckpointKind.ZeroMountedRetained)
                Require(world.Registry.ActiveCount == 0 && world.Registry.Owners.Length == 2 &&
                    new[] { a, b }.All(p => p.Relationship.State == RelationshipState.Unmounted && p.Relationship.PairOwner != null &&
                        p.Participant.CaptureParticipation()?.Activation?.Split == true &&
                        SplitReleaseRound(p) == Game.Instance.TurnBasedCombatController.RoundNumber),
                    "Zero-mounted checkpoint does not retain both original same-round split obligations.");
            checkpointSnapshot = WorldSnapshot("native-checkpoint-ready"); checkpointReady = true;
            Pass("C7-checkpoint-native-" + checkpointKind);
        }
        internal JObject CaptureCheckpoint() => new JObject { ["ready"] = checkpointReady, ["kind"] = checkpointKind.ToString(),
            ["actorIds"] = new JArray((actors ?? new UnitEntityData[0]).Select(x => x.UniqueId)),
            ["targetId"] = target?.UniqueId, ["targetGroupId"] = target?.Group?.Id, ["provenance"] = "source-native-setup",
            ["checkpoint"] = checkpointSnapshot?.DeepClone(), ["world"] = WorldSnapshot("checkpoint-capture"),
            ["trace"] = trace?.Capture() ?? evidence["allocationTrace"]?.DeepClone(),
            ["nativeCompletion"] = completion?.Capture() ?? evidence["nativeCompletion"]?.DeepClone(),
            ["nativeRounds"] = rounds?.Capture() ?? evidence["nativeRounds"]?.DeepClone(),
            ["source"] = evidence.DeepClone(), ["errors"] = new JArray(errors), ["cleanupComplete"] = completed };
        internal void RefreshCheckpointEncounter()
        {
            if (targetService != null && targetService.State == DiagnosticCombatTargetState.Active)
                Require(targetService.RefreshBidirectionalCombatMemoryLease(), "Checkpoint target memory/AI lease changed during continuation.");
        }
        internal void BeginCheckpointCleanup() => BeginCleanup();
        internal bool UpdateCheckpointCleanup()
        {
            if (!completed) PollCleanup();
            Require(!completed || cleanupProven, "Checkpoint source retained failed cleanup: " + string.Join(" | ", errors));
            return cleanupProven;
        }
    }
}
