using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        internal const string Chunk6aRiderIncapacitatedScenario = "chunk6a-rider-incapacitated";
        internal const string Chunk6aMountIncapacitatedScenario = "chunk6a-mount-incapacitated";
        private const string Chunk6aRiderIncapacitatedRow = "CM02-rider-incapacitated";
        private const string Chunk6aMountIncapacitatedRow = "CM02-mount-incapacitated";
        private const string Chunk6aLifeAssemblyMvid = "07fa1e4d-8618-41b3-9b8d-faa17d3b26f7";
        private const string Chunk6aSetLifeStateToken = "06009164";
        private const string Chunk6aTickLifeToken = "06009162";

        private bool Chunk6aRiderIncapacitatedOnly => request.Scenario == Chunk6aRiderIncapacitatedScenario;
        private bool Chunk6aMountIncapacitatedOnly => request.Scenario == Chunk6aMountIncapacitatedScenario;
        private bool Chunk6aPendingIncapacityOnly => Chunk6aRiderIncapacitatedOnly || Chunk6aMountIncapacitatedOnly;
        private UnitEntityData Chunk6aIncapacitySubject => Chunk6aMountIncapacitatedOnly ? horse : rider;
        private UnitEntityData Chunk6aIncapacityOther => Chunk6aMountIncapacitatedOnly ? rider : horse;
        private string Chunk6aIncapacityKind => Chunk6aMountIncapacitatedOnly ? "mount" : "rider";
        private string Chunk6aIncapacityRow => Chunk6aMountIncapacitatedOnly ? Chunk6aMountIncapacitatedRow : Chunk6aRiderIncapacitatedRow;
        private string Chunk6aIncapacityProofContract => Chunk6aMountIncapacitatedOnly
            ? "unacted-native-mount-incapacitated-no-cost-or-transition"
            : "unacted-native-rider-incapacitated-no-cost-or-transition";

        private JObject chunk6aIncapacityEvidence, chunk6aIncapacityOriginal;
        private UnitUseAbility chunk6aIncapacityCommand;
        private PairedConditionObserver chunk6aIncapacityObserver;
        private bool chunk6aIncapacityStimulated;
        private int chunk6aIncapacityDamageDispatchCount, chunk6aIncapacityCleanupInterruptCount;

        private void TickChunk6aPendingIncapacity()
        {
            if (!Chunk6aPendingIncapacityOnly || Chunk6aTurnBased)
                throw new InvalidOperationException("Pending-Mount incapacity requires its isolated RT transaction.");

            if (chunk6aStage == 43)
            {
                if (!Chunk6aIdle || !nativeControls.Evaluate(NativeMountedControlKind.MountCompanion, rider).IsEnabled) return;
                if (!EnsureChunk6aRiderSelection(Chunk6aIncapacityRow)) return;
                var start = CaptureChunk6aGeometry(Chunk6aIncapacityKind + "-incapacity-pre-click");
                if ((bool)start["isAdjacent"])
                    throw new InvalidOperationException("Pending-Mount incapacity must start outside transition reach.");
                chunk6aIncapacityOriginal = CaptureChunk6aPendingIncapacityState();
                var subjectBefore = (JObject)chunk6aIncapacityOriginal[Chunk6aIncapacityKind];
                var otherBefore = (JObject)chunk6aIncapacityOriginal[Chunk6aMountIncapacitatedOnly ? "rider" : "mount"];
                chunk6aIncapacityEvidence = new JObject {
                    ["contract"] = "native-life-state-invalidates-exact-mount-approach",
                    ["subjectKind"] = Chunk6aIncapacityKind,
                    ["subjectId"] = Chunk6aIncapacitySubject.UniqueId,
                    ["otherId"] = Chunk6aIncapacityOther.UniqueId,
                    ["start"] = start,
                    ["stateBefore"] = chunk6aIncapacityOriginal.DeepClone(),
                    ["stimulusCount"] = 0,
                    ["damageDispatchCount"] = 0,
                    ["diagnosticInterruptCount"] = 0,
                    ["externalRestorationRequired"] = true
                };
                observations["chunk6aPendingIncapacity"] = chunk6aIncapacityEvidence;
                if (!(bool)subjectBefore["conscious"] || (bool)subjectBefore["dead"] ||
                    !(bool)subjectBefore["allowDyingCondition"] || (bool)subjectBefore["immortal"] ||
                    (bool)subjectBefore["essential"] || (bool)subjectBefore["mainCharacter"] ||
                    !(bool)otherBefore["conscious"] || (bool)otherBefore["dead"] ||
                    target == null || !target.IsInState || !target.Descriptor.State.IsConscious ||
                    !target.IsPlayersEnemy || target.IsPlayerFaction || target == Chunk6aIncapacitySubject)
                {
                    FailCurrent(Chunk6aIncapacityRow,
                        "Pending-Mount incapacity fixture lacks the exact disposable conscious subject, independent conscious partner, or native enemy source: " + chunk6aIncapacityOriginal);
                    BeginCleanup(); return;
                }
                chunk6aCommandWindow = new NativeRelationshipCommandProbe(nativeControls, allocationTrace, rider, horse,
                    nativeControls.MountAbility.AssetGuid, CaptureChunk6aCausalState, true);
                var clicked = TryNativeAbilityTargetClick(nativeControls.MountAbility, horse,
                    "chunk6a-" + Chunk6aIncapacityKind + "-incapacitated-click");
                chunk6aCommandWindow.ClickCompleted(clicked);
                if (!clicked)
                {
                    FailCurrent(Chunk6aIncapacityRow,
                        "Exact rider native Mount click was refused before the native incapacity stimulus: " + playerAction.LastFeedback);
                    BeginCleanup(); return;
                }
                chunk6aIncapacityCommand = chunk6aCommandWindow.Command;
                chunk6aStage = 44; ResetLeafClock(); return;
            }

            var command = chunk6aIncapacityCommand;
            if (!chunk6aIncapacityStimulated)
            {
                if (command == null || !ReferenceEquals(command, chunk6aCommandWindow == null ? null : chunk6aCommandWindow.Command) ||
                    command.IsStarted || command.IsActed || command.IsFinished || command.ExecutionProcess != null)
                {
                    FailCurrent(Chunk6aIncapacityRow,
                        "The exact Mount left pending approach before native " + Chunk6aIncapacityKind + " incapacity.");
                    BeginCleanup(); return;
                }
                var moved = Chunk6aPlanarDistance((JObject)chunk6aIncapacityEvidence["start"]["riderPosition"], CapturePosition(rider.Position));
                if (!chunk6aCommandWindow.ApproachObserved || !rider.View.AgentASP.IsReallyMoving || moved <= 0.25f) return;
                var geometry = CaptureChunk6aGeometry(Chunk6aIncapacityKind + "-incapacity-trigger");
                var slot = rider.Commands.GetCommand(UnitCommand.CommandType.Move);
                var selected = SelectionManager.Instance.SelectedUnits;
                var before = CaptureChunk6aPendingIncapacityState();
                if ((bool)geometry["isAdjacent"] || !ReferenceEquals(slot, command) || selected.Count != 1 || selected[0] != rider ||
                    !JToken.DeepEquals(before, chunk6aIncapacityOriginal))
                {
                    FailCurrent(Chunk6aIncapacityRow,
                        "Native incapacity missed the exact selected rider's non-adjacent pending Mount or its life inputs changed first.");
                    BeginCleanup(); return;
                }
                var subject = Chunk6aIncapacitySubject;
                var difficulty = Game.Instance.Player.Difficulty.DamageToParty;
                var hitPoints = subject.Stats.HitPoints.ModifiedValue;
                var constitution = subject.Stats.Constitution.ModifiedValue;
                var temporaryHitPoints = subject.Stats.TemporaryHitPoints.ModifiedValue;
                var deathThreshold = hitPoints + constitution;
                var desiredDamage = hitPoints + 1;
                var needed = desiredDamage - subject.Damage + temporaryHitPoints;
                if (difficulty <= 0 || float.IsNaN(difficulty) || float.IsInfinity(difficulty) || needed <= 0)
                {
                    FailCurrent(Chunk6aIncapacityRow, "Native damage fixture lacks a finite difficulty or positive incapacitation window.");
                    BeginCleanup(); return;
                }
                var requested = checked((int)Math.Ceiling((needed + 1d) / difficulty));
                var projected = subject.Damage + requested * difficulty - temporaryHitPoints;
                if (projected >= deathThreshold)
                {
                    FailCurrent(Chunk6aIncapacityRow, "Native difficulty leaves no safe incapacity window below death.");
                    BeginCleanup(); return;
                }
                chunk6aIncapacityEvidence["trigger"] = CaptureChunk6aInvalidationTrigger(command, slot, geometry, moved);
                var stimulus = new JObject {
                    ["contract"] = "one-native-ruledeal-damage-to-incapacitation-window",
                    ["subjectKind"] = Chunk6aIncapacityKind,
                    ["subjectId"] = subject.UniqueId,
                    ["sourceId"] = target.UniqueId,
                    ["count"] = 1,
                    ["damageDispatchCount"] = 1,
                    ["difficulty"] = difficulty,
                    ["hitPoints"] = hitPoints,
                    ["constitution"] = constitution,
                    ["temporaryHitPoints"] = temporaryHitPoints,
                    ["damageBefore"] = subject.Damage,
                    ["desiredDamage"] = desiredDamage,
                    ["deathThreshold"] = deathThreshold,
                    ["requestedDamage"] = requested,
                    ["projectedDamage"] = projected,
                    ["before"] = before.DeepClone(),
                    ["commandBefore"] = CaptureOrdinaryCommand(command),
                    ["frameBefore"] = Time.frameCount,
                    ["gameTicksBefore"] = Game.Instance.TimeController.GameTime.Ticks
                };
                chunk6aIncapacityEvidence["stimulus"] = stimulus;
                chunk6aIncapacityObserver = new PairedConditionObserver(rider, horse, true);
                chunk6aIncapacityStimulated = true;
                chunk6aIncapacityDamageDispatchCount++;
                var damage = Rulebook.Trigger(new RuleDealDamage(target, subject,
                    new DamageBundle(new DirectDamage(new DiceFormula(0, DiceType.Zero), requested))));
                var after = CaptureChunk6aPendingIncapacityState();
                stimulus["ruleObject"] = RuntimeHelpers.GetHashCode(damage);
                stimulus["nativeDamage"] = damage.Damage;
                stimulus["nativeDamageBeforeDifficulty"] = damage.DamageBeforeDifficulty;
                stimulus["after"] = after.DeepClone();
                stimulus["frameAfter"] = Time.frameCount;
                stimulus["gameTicksAfter"] = Game.Instance.TimeController.GameTime.Ticks;
                chunk6aIncapacityEvidence["stateAfterDamage"] = after.DeepClone();
                chunk6aIncapacityEvidence["stimulusCount"] = 1;
                chunk6aIncapacityEvidence["damageDispatchCount"] = chunk6aIncapacityDamageDispatchCount;
                var subjectAfter = (JObject)after[Chunk6aIncapacityKind];
                if (Game.Instance.Player.Difficulty.DamageToParty != difficulty || damage.DamageBeforeDifficulty != requested ||
                    (int)subjectAfter["damage"] < desiredDamage || (int)subjectAfter["damage"] >= deathThreshold)
                {
                    FailCurrent(Chunk6aIncapacityRow,
                        "Native RuleDealDamage did not reach the exact nonlethal incapacity window under unchanged difficulty.");
                    BeginCleanup(); return;
                }
                ResetLeafClock(); return;
            }

            var subjectState = Chunk6aIncapacitySubject.Descriptor.State;
            if (command == null || command.IsActed || command.ExecutionProcess != null || relationship.State != RelationshipState.Unmounted)
            {
                FailCurrent(Chunk6aIncapacityRow,
                    "The incapacity-invalidated Mount acted, acquired a process, or transitioned instead of ending unacted.");
                BeginCleanup(); return;
            }
            if (subjectState.IsConscious || subjectState.IsDead || !chunk6aCommandWindow.UnactedTerminal) return;
            var lifeEvents = chunk6aIncapacityObserver.Capture();
            if (!Chunk6aExactIncapacityLifeEvent(lifeEvents, Chunk6aIncapacitySubject))
            {
                FailCurrent(Chunk6aIncapacityRow,
                    "The native life-state boundary did not contain exactly Conscious-to-Unconscious through SetLifeState and TickOnUnit.");
                BeginCleanup(); return;
            }
            var proof = chunk6aCommandWindow.FinishUnacted(Chunk6aIncapacityProofContract);
            var terminal = CaptureOrdinaryCommand(command);
            chunk6aIncapacityEvidence["commandProof"] = proof;
            chunk6aIncapacityEvidence["terminal"] = terminal;
            chunk6aIncapacityEvidence["nativeLifeEvents"] = lifeEvents;
            chunk6aIncapacityEvidence["stateAfterTerminal"] = CaptureChunk6aPendingIncapacityState();
            chunk6aIncapacityEvidence["subjectRemainsIncapacitated"] = !subjectState.IsConscious && !subjectState.IsDead;
            chunk6aIncapacityEvidence["diagnosticInterruptCount"] = chunk6aIncapacityCleanupInterruptCount;
            chunk6aCommandWindow.Dispose(); chunk6aCommandWindow = null;
            chunk6aIncapacityObserver.Dispose(); chunk6aIncapacityObserver = null;
            var otherBeforeFinal = (JObject)chunk6aIncapacityOriginal[Chunk6aMountIncapacitatedOnly ? "rider" : "mount"];
            var finalState = (JObject)chunk6aIncapacityEvidence["stateAfterTerminal"];
            var otherAfterFinal = (JObject)finalState[Chunk6aMountIncapacitatedOnly ? "rider" : "mount"];
            var otherUnchanged = JToken.DeepEquals(otherBeforeFinal, otherAfterFinal);
            var noResidue = relationship.State == RelationshipState.Unmounted && !playerAction.HasVoluntaryTransitionInFlight &&
                rider.Commands.Empty && horse.Commands.Empty;
            chunk6aIncapacityEvidence["otherActorUnchanged"] = otherUnchanged;
            chunk6aIncapacityEvidence["noResidue"] = noResidue;
            var pass = (bool)proof["pass"] && !(bool)terminal["started"] && otherUnchanged && noResidue &&
                chunk6aIncapacityDamageDispatchCount == 1 && chunk6aIncapacityCleanupInterruptCount == 0;
            AddRow(Chunk6aIncapacityRow, pass,
                pass ? "One real native RuleDealDamage placed the exact " + Chunk6aIncapacityKind +
                    " in the nonlethal Unconscious state through UnitLifeController; the same pending Mount ended unstarted/unacted with no process, cost, transition or residue while the other actor remained unchanged." :
                    "The exact native incapacity event, unacted terminal, resource proof, or independent-partner proof failed.",
                chunk6aIncapacityEvidence);
            chunk6aStage = 99; BeginCleanup();
        }

        private JObject CaptureChunk6aPendingIncapacityState()
        {
            return new JObject {
                ["rider"] = CaptureChunk6aPendingIncapacityActor(rider),
                ["mount"] = CaptureChunk6aPendingIncapacityActor(horse),
                ["relationship"] = relationship.State.ToString(),
                ["generation"] = relationship.MountedPairGeneration
            };
        }

        private JObject CaptureChunk6aPendingIncapacityActor(UnitEntityData actor)
        {
            return new JObject {
                ["id"] = actor.UniqueId,
                ["inState"] = actor.IsInState,
                ["inGame"] = actor.IsInGame,
                ["directlyControllable"] = actor.IsDirectlyControllable,
                ["lifeState"] = actor.Descriptor.State.LifeState.ToString(),
                ["conscious"] = actor.Descriptor.State.IsConscious,
                ["dead"] = actor.Descriptor.State.IsDead,
                ["finallyDead"] = actor.Descriptor.State.IsFinallyDead,
                ["damage"] = actor.Damage,
                ["hitPoints"] = actor.Stats.HitPoints.ModifiedValue,
                ["temporaryHitPoints"] = actor.Stats.TemporaryHitPoints.ModifiedValue,
                ["constitution"] = actor.Stats.Constitution.ModifiedValue,
                ["allowDyingCondition"] = (bool)actor.Descriptor.State.AllowDyingCondition,
                ["immortal"] = (bool)actor.Descriptor.State.Immortality,
                ["essential"] = actor.Descriptor.IsEssentialForGame,
                ["mainCharacter"] = actor == Game.Instance.Player.MainCharacter.Value
            };
        }

        private static bool Chunk6aExactIncapacityLifeEvent(JObject trace, UnitEntityData subject)
        {
            var events = (trace?["events"] as JArray)?.OfType<JObject>().ToArray() ?? new JObject[0];
            if (events.Length != 1) return false;
            var item = events[0];
            var source = (item["nativeSource"] as JArray)?.OfType<JObject>().ToArray() ?? new JObject[0];
            return (string)item["kind"] == "native-life-state" && (string)item["actor"] == subject.UniqueId &&
                (string)item["detail"] == "Conscious" && (string)item["lifeState"] == "Unconscious" &&
                source.Length == 2 && (string)source[0]["type"] == "Kingmaker.Controllers.Units.UnitLifeController" &&
                (string)source[0]["method"] == "SetLifeState" && (string)source[0]["token"] == Chunk6aSetLifeStateToken &&
                (string)source[0]["assemblyMvid"] == Chunk6aLifeAssemblyMvid &&
                (string)source[1]["type"] == "Kingmaker.Controllers.Units.UnitLifeController" &&
                (string)source[1]["method"] == "TickOnUnit" && (string)source[1]["token"] == Chunk6aTickLifeToken &&
                (string)source[1]["assemblyMvid"] == Chunk6aLifeAssemblyMvid;
        }

        private bool CaptureChunk6aPendingIncapacityDeadline()
        {
            if (!Chunk6aPendingIncapacityOnly || chunk6aStage < 43 || chunk6aStage > 44) return false;
            if (chunk6aIncapacityEvidence != null)
            {
                chunk6aIncapacityEvidence["deadlineState"] = CaptureChunk6aPendingIncapacityState();
                chunk6aIncapacityEvidence["deadlineCommand"] = CaptureOrdinaryCommand(chunk6aIncapacityCommand);
                if (chunk6aIncapacityObserver != null)
                    chunk6aIncapacityEvidence["deadlineLifeEvents"] = chunk6aIncapacityObserver.Capture();
            }
            FailCurrent(Chunk6aIncapacityRow, !chunk6aIncapacityStimulated
                ? "The exact Mount did not reach the measured approach boundary for native incapacity at the unchanged 30-second deadline."
                : "The exact incapacity-invalidated Mount did not reach its native unacted terminal at the unchanged 30-second deadline.");
            return true;
        }

        private void CleanupChunk6aPendingIncapacity()
        {
            if (!Chunk6aPendingIncapacityOnly) return;
            if (chunk6aIncapacityCommand != null && !chunk6aIncapacityCommand.IsFinished)
            {
                chunk6aIncapacityCommand.Interrupt();
                chunk6aIncapacityCleanupInterruptCount++;
            }
            if (chunk6aIncapacityObserver != null)
            {
                if (chunk6aIncapacityEvidence != null)
                    chunk6aIncapacityEvidence["cleanupLifeEvents"] = chunk6aIncapacityObserver.Capture();
                chunk6aIncapacityObserver.Dispose();
                chunk6aIncapacityObserver = null;
            }
        }
    }
}