// Review of 0b4b8fa4 (equivalence answer): the production readiness evaluation
// against the baseline v0.2.14 predicate it replaced. scripts/Test-DeferredSaveWait.ps1
// splices the production SaveReadiness, SaveEffectsReady and EvaluateSaveEffects
// from this worktree and the baseline SaveEffectsReady from commit 61519031,
// renamed BaselineSaveEffectsReady. Both read the same fake world state, built
// for every combination of the eight readiness terms (with raw/queued command and
// active/pending projectile variants). Only the world state is fake.
using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Integration
{
    class Player { }
    class UnitCommand { }
    class UnitCommands
    {
        internal UnitCommand[] Raw = new UnitCommand[4];
        internal LinkedList<UnitCommand> Queue = new LinkedList<UnitCommand>();
    }
    class UnitEntityData { internal UnitCommands Commands = new UnitCommands(); }
    class WorldState { internal List<UnitEntityData> Units = new List<UnitEntityData>(); }
    class Game
    {
        internal static Game Instance = new Game();
        internal Player Player = new Player();
        internal WorldState State = new WorldState();
    }
    class GameMountedRelationshipService { internal string PairSaveRefusal; }
    class MountedPairOwner<TActor, TBoundary> where TActor : class where TBoundary : class { internal TBoundary Boundary; }
    class Relationship
    {
        internal MountedPairOwner<UnitEntityData, GameMountedRelationshipService> Unready;
        internal bool AllSaveOwnersReady => Unready == null;
        internal MountedPairOwner<UnitEntityData, GameMountedRelationshipService> FirstUnreadySaveOwner => Unready;
    }
    class UnifiedTurn { internal bool HasUnsettledPreparation; }
    class Controls
    {
        internal bool HasUnsettledRelationshipTransition;
        internal GameMountedRelationshipService UnsettledShellOwner;
        internal UnitCommand ShellCommand;
        internal bool HasUnsettledShellFor(GameMountedRelationshipService boundary) => ReferenceEquals(boundary, UnsettledShellOwner);
        internal bool OwnsUnsettledRelationshipShell(UnitCommand command) => command != null && ReferenceEquals(command, ShellCommand);
    }
    static class NativeSaveEffectBoundary
    {
        internal static UnitCommand Settling;
        internal static object Active, Pending, Process;
        internal static bool CommandNeedsSettlement(UnitCommand command) => command != null && ReferenceEquals(command, Settling);
        internal static object FirstUnresolvedProjectile(out bool pending)
        {
            pending = Active == null && Pending != null;
            return Active ?? Pending;
        }
        internal static object FirstUnresolvedAbility() => Process;
        internal static bool HasUnresolvedProjectiles() => Active != null || Pending != null;
        internal static bool HasUnresolvedAbilities() => Process != null;
    }

    class Persistence
    {
        internal Relationship relationship = new Relationship();
        internal UnifiedTurn unifiedTurn = new UnifiedTurn();
        internal Controls controls = new Controls();
        internal GameMountedRelationshipService[] WorldRelationships = { new GameMountedRelationshipService(), new GameMountedRelationshipService() };

__READINESS_TYPE__
__READY_METHOD__
__EVALUATE_METHOD__
__BASELINE_METHOD__
    }

    static class Program
    {
        static int Main()
        {
            var terms = new[] { "owner", "preparation", "transition", "shell", "command", "shellCommand", "projectile", "ability" };
            var expectedOrder = new[]
            {
                SaveWaitBlockerKind.PairOwnership, SaveWaitBlockerKind.NativePreparation, SaveWaitBlockerKind.RelationshipTransition,
                SaveWaitBlockerKind.RelationshipShell, SaveWaitBlockerKind.Command, SaveWaitBlockerKind.RelationshipShellCommand,
                SaveWaitBlockerKind.Projectile, SaveWaitBlockerKind.Ability
            };
            int combinations = 0, failures = 0;
            for (var mask = 0; mask < 1 << terms.Length; mask++)
            for (var variant = 0; variant < 4; variant++)
            {
                var queuedCommand = (variant & 1) != 0;
                var pendingProjectile = (variant & 2) != 0;
                Game.Instance = new Game();
                var persistence = new Persistence();
                var units = new[] { new UnitEntityData(), new UnitEntityData(), new UnitEntityData() };
                Game.Instance.State.Units.AddRange(units);
                NativeSaveEffectBoundary.Settling = null; NativeSaveEffectBoundary.Active = null;
                NativeSaveEffectBoundary.Pending = null; NativeSaveEffectBoundary.Process = null;
                Func<int, bool> on = i => (mask & (1 << i)) != 0;
                var owner = new MountedPairOwner<UnitEntityData, GameMountedRelationshipService>
                    { Boundary = new GameMountedRelationshipService { PairSaveRefusal = "a voluntary Mount/Dismount is in flight" } };
                if (on(0)) persistence.relationship.Unready = owner;
                persistence.unifiedTurn.HasUnsettledPreparation = on(1);
                persistence.controls.HasUnsettledRelationshipTransition = on(2);
                if (on(3)) persistence.controls.UnsettledShellOwner = persistence.WorldRelationships[1];
                var settling = new UnitCommand();
                if (on(4))
                {
                    NativeSaveEffectBoundary.Settling = settling;
                    if (queuedCommand) units[1].Commands.Queue.AddLast(settling); else units[1].Commands.Raw[2] = settling;
                }
                var shell = new UnitCommand();
                if (on(5)) { persistence.controls.ShellCommand = shell; units[2].Commands.Queue.AddLast(shell); }
                var projectile = new object();
                if (on(6)) { if (pendingProjectile) NativeSaveEffectBoundary.Pending = projectile; else NativeSaveEffectBoundary.Active = projectile; }
                var process = new object();
                if (on(7)) NativeSaveEffectBoundary.Process = process;

                combinations++;
                var baseline = persistence.BaselineSaveEffectsReady();
                var result = persistence.EvaluateSaveEffects();
                var first = Enumerable.Range(0, terms.Length).FirstOrDefault(i => on(i));
                var expected = mask == 0 ? SaveWaitBlockerKind.None : expectedOrder[first];
                var problems = new List<string>();
                if (baseline != result.Ready) problems.Add("ready baseline=" + baseline + " candidate=" + result.Ready);
                if (persistence.SaveEffectsReady() != baseline) problems.Add("SaveEffectsReady differs from baseline");
                if (result.Kind != expected) problems.Add("kind " + result.Kind + " expected " + expected);
                switch (result.Kind)
                {
                    case SaveWaitBlockerKind.PairOwnership:
                        if (!ReferenceEquals(result.Subject, owner) || result.Reason != "a voluntary Mount/Dismount is in flight") problems.Add("owner identity");
                        break;
                    case SaveWaitBlockerKind.RelationshipShell:
                        if (!ReferenceEquals(result.Subject, persistence.WorldRelationships[1])) problems.Add("shell owner identity");
                        break;
                    case SaveWaitBlockerKind.Command:
                        if (!ReferenceEquals(result.Subject, settling) || !ReferenceEquals(result.Unit, units[1]) || result.Queued != queuedCommand)
                            problems.Add("command identity/ownership");
                        break;
                    case SaveWaitBlockerKind.RelationshipShellCommand:
                        if (!ReferenceEquals(result.Subject, shell) || !ReferenceEquals(result.Unit, units[2]) || !result.Queued) problems.Add("shell command identity");
                        break;
                    case SaveWaitBlockerKind.Projectile:
                        if (!ReferenceEquals(result.Subject, projectile) || result.Queued != pendingProjectile) problems.Add("projectile identity/membership");
                        break;
                    case SaveWaitBlockerKind.Ability:
                        if (!ReferenceEquals(result.Subject, process)) problems.Add("ability identity");
                        break;
                }
                if (problems.Count != 0)
                {
                    failures++;
                    if (failures <= 10) Console.WriteLine("FAIL mask=" + Convert.ToString(mask, 2).PadLeft(8, '0') + " variant=" + variant + " :: " + string.Join("; ", problems));
                }
            }
            // No world: both refuse in the same way before reading any term.
            Game.Instance = new Game { Player = null };
            var noWorld = new Persistence();
            bool baselineThrew = false, candidateThrew = false;
            try { noWorld.BaselineSaveEffectsReady(); } catch (InvalidOperationException) { baselineThrew = true; }
            try { noWorld.EvaluateSaveEffects(); } catch (InvalidOperationException) { candidateThrew = true; }
            if (!baselineThrew || !candidateThrew) { failures++; Console.WriteLine("FAIL no-world refusal baseline=" + baselineThrew + " candidate=" + candidateThrew); }
            Console.WriteLine((failures == 0 ? "PASS" : "FAIL") + " readiness equivalence against baseline 61519031 over " + combinations +
                " term combinations plus the no-world refusal; failures=" + failures);
            return failures == 0 ? 0 : 1;
        }
    }
}
