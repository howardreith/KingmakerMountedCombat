using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.Selection;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;
namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        internal static bool IsChunk6aRefusedScenario(string scenario) =>
            scenario == "chunk6a-refused-policy-disabled" || scenario == "chunk6a-refused-foreign-companion" || scenario == "chunk6a-refused-wrong-creature-target" || scenario == "chunk6a-refused-mount-selected" ||
            scenario == "chunk6a-refused-multiple-selection" || scenario == "chunk6a-refused-foreign-selection";
        private bool Chunk6aRefusedOnly => IsChunk6aRefusedScenario(request.Scenario);
        private NativeRefusedMountInputProbe chunk6aRefusalProbe;
        private bool chunk6aRefusalInvoked;
        private string Chunk6aRefusalCase => request.Scenario.Substring("chunk6a-refused-".Length);
        private string Chunk6aRefusalRow => Chunk6aRefusalCase == "policy-disabled" ? "CM06-combat-mount-requires-qualified-paired-policy" : Chunk6aRefusalCase == "foreign-companion" ? "CM02-foreign-companion" : Chunk6aRefusalCase == "wrong-creature-target" ? "CM02-wrong-creature-target" : "CM06-" + Chunk6aRefusalCase;
        private void TickChunk6aRefusedMount()
        {
            if (!Chunk6aRefusedOnly || Chunk6aTurnBased || chunk6aRefusalInvoked)
                throw new InvalidOperationException("Refused Mount requires one input in its own RT transaction.");
            if (!Chunk6aIdle) return;
            var row = Chunk6aRefusalRow;
            if (!EnsureChunk6aRiderSelection(row)) return;
            var kind = NativeMountedControlKind.MountCompanion;
            var legalAvailability = nativeControls.Evaluate(kind, rider);
            var ownedTargetLegal = nativeControls.CanTarget(kind, rider, horse);
            if (!legalAvailability.IsVisible || !legalAvailability.IsEnabled || !ownedTargetLegal ||
                !rider.IsInCombat || !horse.IsInCombat || CombatController.IsInTurnBasedCombat())
                throw new InvalidOperationException("Refusal fixture lacks a legal exact rider Mount immediately before its negative input: " + legalAvailability.Reason);
            UnitEntityData foreignOwner = null;
            UnitEntityData other;
            var foreignCompanion = Chunk6aRefusalCase == "foreign-companion";
            if (foreignCompanion)
            {
                string refusal;
                if (!relationship.TryResolveAutomationPair(SupportedMountedProfiles.MammothBlueprintGuid, out foreignOwner, out other, out refusal) ||
                    foreignOwner == rider || foreignOwner == horse || other == rider || other == horse ||
                    !foreignOwner.IsInGame || !foreignOwner.Commands.Empty || !other.IsInGame || !other.IsDirectlyControllable || other.View == null || !other.Commands.Empty)
                    throw new InvalidOperationException("Foreign companion refusal requires the unchanged native Mammoth and its distinct exact owner: " + refusal);
            }
            else
            {
                other = Game.Instance.Player.Party.FirstOrDefault(u => u != null && u != rider && u != horse &&
                    u.IsInGame && u.IsDirectlyControllable && u.View != null && u.Commands.Empty &&
                    u.Descriptor.Master.Value == null && !SupportedMountedProfiles.IsSupported(u));
                if (other == null) throw new InvalidOperationException("Refusal fixture lacks a live native unrelated non-pet party creature.");
            }
            var manager = SelectionManager.Instance;
            var legal = new JObject {
                ["frame"] = Time.frameCount, ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
                ["inCombat"] = rider.IsInCombat && horse.IsInCombat, ["turnBased"] = CombatController.IsInTurnBasedCombat(),
                ["visible"] = legalAvailability.IsVisible, ["enabled"] = legalAvailability.IsEnabled,
                ["canTargetOwnedMount"] = ownedTargetLegal,
                ["exactRiderSelected"] = manager.SelectedUnits.Count == 1 && manager.SelectedUnits[0] == rider,
                ["state"] = CaptureChunk6aCausalState()
            };
            var name = Chunk6aRefusalCase; var target = horse; UnitEntityData[] expected;
            if (name == "policy-disabled") { expected = new[] { rider }; BeginChunk6aRefusalPolicy(); }
            else if (name == "wrong-creature-target" || foreignCompanion) { expected = new[] { rider }; target = other; }
            else if (name == "mount-selected") { manager.SelectUnit(horse.View, true, true, false); expected = new[] { horse }; }
            else if (name == "multiple-selection") { manager.SelectUnit(horse.View, false, true, false); expected = new[] { rider, horse }; }
            else { manager.SelectUnit(other.View, true, true, false); expected = new[] { other }; }
            var selected = manager.SelectedUnits.ToArray();
            var exactSelection = selected.Length == expected.Length && selected.Zip(expected, ReferenceEquals).All(x => x);
            var availability = nativeControls.Evaluate(kind, rider);
            var condition = new JObject {
                ["frame"] = Time.frameCount, ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
                ["selectionVerified"] = exactSelection,
                ["inCombat"] = rider.IsInCombat && horse.IsInCombat, ["turnBased"] = CombatController.IsInTurnBasedCombat(),
                ["visible"] = availability.IsVisible, ["enabled"] = availability.IsEnabled,
                ["canTargetOwnedMount"] = nativeControls.CanTarget(kind, rider, horse),
                ["canTargetRequested"] = nativeControls.CanTarget(kind, rider, target),
                ["selectedIds"] = new JArray(selected.Select(u => u.UniqueId)),
                ["selectedObjects"] = new JArray(selected.Select(RuntimeHelpers.GetHashCode)),
                ["targetId"] = target.UniqueId, ["targetObject"] = RuntimeHelpers.GetHashCode(target),
                ["expectedReason"] = playerAction.DescribeNativeMountTargetRejection(rider, target)
            };
            var evidence = new JObject {
                ["contract"] = "one-native-refused-mount-in-fresh-rt-allocation", ["case"] = name, ["scenario"] = request.Scenario, ["row"] = row,
                ["identity"] = new JObject { ["riderId"] = rider.UniqueId, ["mountId"] = horse.UniqueId, ["unrelatedId"] = other.UniqueId,
                    ["riderObject"] = RuntimeHelpers.GetHashCode(rider), ["mountObject"] = RuntimeHelpers.GetHashCode(horse), ["unrelatedObject"] = RuntimeHelpers.GetHashCode(other),
                    ["reciprocalPair"] = rider.Descriptor.Pet == horse && horse.Descriptor.Master.Value == rider,
                    ["mountProfile"] = SupportedMountedProfiles.Resolve(horse)?.DisplayName,
                    ["unrelatedLivePartyActor"] = Game.Instance.Player.Party.Contains(other) && other.IsInGame && other.IsDirectlyControllable && other.View != null,
                    ["unrelatedIsPet"] = other.Descriptor.Master.Value != null, ["unrelatedSupportedMount"] = SupportedMountedProfiles.IsSupported(other) },
                ["legal"] = legal, ["condition"] = condition
            };
            if (foreignCompanion) evidence["foreignCompanionBefore"] = CaptureChunk6aForeignCompanion(foreignOwner, other);
            observations["chunk6aRefusedMount"] = evidence;
            if (!exactSelection)
            { FailCurrent(row, "Exact negative selection failed before SetAbility or OnClick: " + condition.ToString(Formatting.None)); BeginCleanup(); return; }
            chunk6aRefusalProbe = new NativeRefusedMountInputProbe(nativeControls, allocationTrace, rider, horse, target, CaptureChunk6aCausalState);
            chunk6aRefusalInvoked = true;
            var accepted = chunk6aRefusalProbe.Invoke();
            var input = chunk6aRefusalProbe.Capture(); evidence["input"] = input;
            condition["state"] = input["before"]["state"].DeepClone();
            chunk6aRefusalProbe.Dispose(); chunk6aRefusalProbe = null;
            if (name == "policy-disabled") CompleteChunk6aRefusalPolicy(evidence);
            if (foreignCompanion) evidence["foreignCompanionAfter"] = CaptureChunk6aForeignCompanion(foreignOwner, other);
            string failure = null;
            try { NativeRefusedMountCaseEvidence.AssertComplete(evidence); }
            catch (Exception exception) { failure = exception.Message; }
            AddRow(row, !accepted && failure == null,
                failure ?? "One exact native target click was refused for the declared cause; no command, shell, movement, action/reaction event or relationship delta occurred.", evidence);
            chunk6aStage = 99; BeginCleanup();
        }
        private JObject CaptureChunk6aForeignCompanion(UnitEntityData owner, UnitEntityData pet) => new JObject {
            ["ownerId"] = owner.UniqueId, ["ownerObject"] = RuntimeHelpers.GetHashCode(owner),
            ["targetId"] = pet.UniqueId, ["targetObject"] = RuntimeHelpers.GetHashCode(pet),
            ["masterId"] = pet.Descriptor.Master.Value?.UniqueId,
            ["masterObject"] = pet.Descriptor.Master.Value == null ? 0 : RuntimeHelpers.GetHashCode(pet.Descriptor.Master.Value),
            ["ownerPetId"] = owner.Descriptor.Pet?.UniqueId,
            ["ownerPetObject"] = owner.Descriptor.Pet == null ? 0 : RuntimeHelpers.GetHashCode(owner.Descriptor.Pet),
            ["ownerLiveParty"] = Game.Instance.Player.Party.Contains(owner) && owner.IsInGame,
            ["targetLiveParty"] = Game.Instance.Player.Party.Contains(pet) && pet.IsInGame && pet.IsDirectlyControllable && pet.View != null,
            ["targetBlueprint"] = pet.Blueprint.AssetGuid, ["targetProfile"] = SupportedMountedProfiles.Resolve(pet)?.DisplayName,
            ["ownerCommandsEmpty"] = owner.Commands.Empty, ["targetCommandsEmpty"] = pet.Commands.Empty,
            ["ownerResources"] = Chunk6aCooldowns(owner), ["targetResources"] = Chunk6aCooldowns(pet)
        };
        private void CleanupChunk6aRefusalInput()
        {
            if (chunk6aRefusalProbe != null) observations["chunk6aRefusedMountInputAtCleanup"] = chunk6aRefusalProbe.Capture();
            chunk6aRefusalProbe?.Dispose(); chunk6aRefusalProbe = null;
            CleanupChunk6aRefusalPolicy();
        }
    }
}
