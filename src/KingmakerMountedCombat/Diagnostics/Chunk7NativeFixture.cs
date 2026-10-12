using System;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Commands;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    // The same exact disposable fixture and selected-ability delivery contract
    // used by the two-pair world scenario. This never spends or resets resources.
    internal static class Chunk7NativeFixture
    {
        internal static UnitEntityData[] ValidateWorking(RuntimeRequest request, HorseCompanionBlueprintService horses)
        {
            Require(request.Fixture?.Working?.Sha256 == Chunk7WorldScenarioEngine.WorkingHash,
                "Chunk 7 requires the exact qualified disposable Working bytes.");
            var game = Game.Instance;
            Require(game?.Player != null && !game.Player.IsInCombat, "The exact fixture must be outside combat.");
            var first = game.Player.Party.SingleOrDefault(x => x?.UniqueId == Chunk7WorldScenarioEngine.MammothRiderId);
            Require(first?.Group != null && first.Group.IsPlayerParty, "Exact Mammoth owner party group is unavailable.");
            var party = Enumerable.Range(0, first.Group.Count).Select(i => first.Group[i]).ToArray();
            Require(party.Select(x => x.UniqueId).OrderBy(x => x, StringComparer.Ordinal).SequenceEqual(
                new[] { Chunk7WorldScenarioEngine.MammothRiderId, Chunk7WorldScenarioEngine.MammothId,
                    Chunk7WorldScenarioEngine.HorseRiderId }.OrderBy(x => x, StringComparer.Ordinal)),
                "Working party actors differ; no substitute fixture actors are selected.");
            var mammoth = party.Single(x => x.UniqueId == Chunk7WorldScenarioEngine.MammothId);
            var horseRider = party.Single(x => x.UniqueId == Chunk7WorldScenarioEngine.HorseRiderId);
            Require(party.All(Idle) && mammoth.Blueprint?.AssetGuid == SupportedMountedProfiles.MammothBlueprintGuid &&
                ReferenceEquals(first.Descriptor.Pet, mammoth) && ReferenceEquals(mammoth.Descriptor.Master.Value, first),
                "Original Mammoth ownership or idle party admission differs.");
            Require(horseRider.Descriptor.Pet == null && !horseRider.Descriptor.IsPet && horseRider.IsDirectlyControllable &&
                horseRider.Descriptor.State.Size == Size.Medium && horseRider.Descriptor.GetFact(horses.LevelRank) == null &&
                horseRider.GetActivePolymorph() == null, "Horse rider does not satisfy the declared Medium fixture admission.");
            Require(MountedRiderPoseAdapter.TryValidateSupportedSurface(horseRider.View,
                SupportedMountedProfiles.Horse.RiderPoseProfile, out var rigError), "Horse rider rig refused: " + rigError);
            return party;
        }

        internal static bool Idle(UnitEntityData actor) => actor != null && actor.IsInState && actor.IsInGame &&
            actor.View?.AgentASP != null && actor.Descriptor.State.IsConscious && !actor.IsInCombat &&
            actor.Commands != null && actor.Commands.Empty;

        internal static void Select(UnitEntityData actor)
        {
            SelectionManager.Instance.SelectUnit(actor.View, true, true, false);
            Require(SelectionManager.Instance.SelectedUnits.Count == 1 && ReferenceEquals(SelectionManager.Instance.SelectedUnits[0], actor),
                "Native exact single selection failed.");
        }

        internal static UnitUseAbility Issue(NativeMountedControlService controls, UnitEntityData caster, UnitEntityData target,
            BlueprintAbility ability, UnitEntityData changeSelectionTo, JObject observation)
        {
            Game.Instance.IsPaused = true; Select(caster); controls.Update();
            var fact = caster.Descriptor.Abilities.GetAbility(ability); var handler = Game.Instance.SelectedAbilityHandler;
            Require(fact?.Data != null && handler != null && target.View != null, "Selected native ability is unavailable.");
            var casts = controls.NativeCastRequestCount; var refusals = controls.NativeRefusalCount;
            handler.SetAbility(fact.Data);
            bool clicked;
            try { clicked = handler.OnClick(target.View.gameObject, target.Position, 0, false, false); }
            finally { handler.DropAbility(); }
            var shell = caster.Commands.Raw.OfType<UnitUseAbility>().SingleOrDefault(x => ReferenceEquals(x.Spell?.Blueprint, ability));
            var identity = controls.CaptureRelationshipCommandIdentity(shell);
            observation["casterId"] = caster.UniqueId; observation["targetId"] = target.UniqueId;
            observation["abilityGuid"] = ability.AssetGuid; observation["clicked"] = clicked;
            observation["nativeCastDelta"] = controls.NativeCastRequestCount - casts;
            observation["nativeRefusalDelta"] = controls.NativeRefusalCount - refusals;
            observation["generationAtInit"] = identity?.Generation; observation["control"] = identity?.Control;
            observation["selectionAfterInput"] = changeSelectionTo.UniqueId;
            Require(clicked && controls.NativeCastRequestCount == casts + 1 && controls.NativeRefusalCount == refusals &&
                shell != null && identity != null && identity.Caster == caster.UniqueId && identity.Target == target.UniqueId,
                "Native selected-ability input did not create one exact admitted shell.");
            Select(changeSelectionTo); Game.Instance.IsPaused = false;
            return shell;
        }

        private static void Require(bool condition, string error) { if (!condition) throw new InvalidOperationException(error); }
    }
}
