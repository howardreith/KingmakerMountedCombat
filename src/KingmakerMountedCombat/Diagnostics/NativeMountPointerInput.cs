using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Kingmaker;
using Kingmaker.Controllers.Clicks;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.TurnBasedMode;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Abilities;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Native selected-ability input with the same hover/prediction/admission gates as
    // the TB pointer. Owns only reversible pointer fields; never assigns a path.
    internal sealed class NativeMountPointerInput : IDisposable
    {
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        private static readonly PropertyInfo PointerOn = typeof(PointerController).GetProperty("PointerOn", Flags);
        private static readonly PropertyInfo WorldPosition = typeof(PointerController).GetProperty("WorldPosition", Flags);
        private static readonly FieldInfo SimulatedPosition = typeof(PointerController).GetField("m_WorldPositionForSimulation", Flags);
        private static readonly FieldInfo SimulatedHandler = typeof(PointerController).GetField("m_SimulateClickHandler", Flags);
        private static readonly FieldInfo MouseHandler = typeof(PointerController).GetField("m_MouseDownHandler", Flags);
        private static readonly MethodInfo Predictions = typeof(TurnController).GetMethod("UpdateActionPredictions", Flags);
        private readonly PointerController pointer;
        private readonly ClickWithSelectedAbilityHandler handler;
        private readonly AbilityData ability;
        private readonly UnitEntityData rider, target;
        private readonly TurnController turn;
        private readonly object[] saved;
        private readonly JArray samples = new JArray();
        private bool ready, clicked, disposed, restored;

        internal NativeMountPointerInput(UnitEntityData rider, UnitEntityData target, AbilityData ability)
        {
            if (typeof(TurnController).Assembly.ManifestModule.ModuleVersionId != new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7") ||
                Predictions?.MetadataToken != 0x06000C6E || PointerOn?.GetSetMethod(true)?.MetadataToken != 0x060093B2 ||
                WorldPosition?.GetSetMethod(true)?.MetadataToken != 0x060093B4 || SimulatedPosition?.MetadataToken != 0x04005EAD ||
                SimulatedHandler?.MetadataToken != 0x04005EAC || MouseHandler?.MetadataToken != 0x04005EAB ||
                typeof(TurnController).GetMethod("IgnoreClick", Flags)?.MetadataToken != 0x06000C2F)
                throw new MissingMemberException("Pinned native selected-ability pointer contract differs.");
            this.rider = rider; this.target = target; this.ability = ability;
            pointer = Game.Instance.DefaultPointerController; handler = Game.Instance.SelectedAbilityHandler;
            turn = Game.Instance.TurnBasedCombatController.CurrentTurn;
            if (pointer == null || handler == null || handler.Ability != null || ability?.Caster?.Unit != rider ||
                ability.Blueprint.AssetGuid != "f053faad986631688defa003cd7bda0e" || target?.View == null)
                throw new InvalidOperationException("Native Mount pointer requires an idle selector and exact ability/target.");
            AssertContext();
            saved = new[] { PointerOn.GetValue(pointer, null), WorldPosition.GetValue(pointer, null),
                SimulatedPosition.GetValue(pointer), SimulatedHandler.GetValue(pointer), MouseHandler.GetValue(pointer) };
            try
            {
                Record("before-set-ability", null);
                handler.SetAbility(ability);
                ApplyPointer();
                Record("after-set-ability", null);
            }
            catch { Dispose(); throw; }
        }
        private static int Id(object value) => value == null ? 0 : RuntimeHelpers.GetHashCode(value);
        private void AssertContext()
        {
            var selected = SelectionManager.Instance?.SelectedUnits;
            if (!CombatController.IsInTurnBasedCombat() || turn == null || !ReferenceEquals(turn, Game.Instance.TurnBasedCombatController.CurrentTurn) ||
                turn.Unit != rider || !turn.IsActing || selected == null || selected.Count != 1 || selected[0] != rider ||
                !rider.IsInCombat || !target.IsInCombat || PointerController.SimulatingClick)
                throw new InvalidOperationException("Native Mount pointer lost its exact selected rider/Acting allocation.");
        }
        private void ApplyPointer()
        {
            AssertContext();
            PointerOn.SetValue(pointer, target.View.gameObject, null);
            WorldPosition.SetValue(pointer, target.Position, null);
            SimulatedPosition.SetValue(pointer, target.Position);
            pointer.UpdateSelectedClickHandler();
            if (!ReferenceEquals(SimulatedHandler.GetValue(pointer), handler) || !ReferenceEquals(handler.Ability, ability))
                throw new InvalidOperationException("Native pointer priority did not select this exact active Mount ability.");
        }
        internal bool PollReady()
        {
            if (disposed || clicked) throw new InvalidOperationException("Native Mount pointer already closed or committed.");
            ApplyPointer();
            // Native SetHighlighted invalidates only when the hovered object changes.
            turn.OnHoverObjectChanged(null, target.View.gameObject);
            Predictions.Invoke(turn, null);
            var ignored = turn.IgnoreClick();
            ready = !ignored;
            Record("native-admission", ignored);
            return ready;
        }
        internal bool Click()
        {
            if (disposed || clicked || !ready) throw new InvalidOperationException("Native Mount click preceded its exact admitted preview.");
            AssertContext();
            if (!ReferenceEquals(SimulatedHandler.GetValue(pointer), handler) || !ReferenceEquals(handler.Ability, ability))
                throw new InvalidOperationException("Native Mount handler changed after prediction admission.");
            Record("before-real-click", false);
            clicked = true;
            var result = handler.OnClick(target.View.gameObject, target.Position, 0, false, false);
            Record("after-real-click", false);
            return result;
        }
        private void Record(string boundary, bool? ignored)
        {
            if (samples.Count >= 128) throw new InvalidOperationException("Native pointer observation bound exceeded.");
            var selected = SelectionManager.Instance.SelectedUnits;
            samples.Add(new JObject { ["boundary"] = boundary, ["frame"] = Time.frameCount,
                ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks, ["turnObject"] = Id(turn),
                ["turnActor"] = turn.Unit.UniqueId, ["turnStatus"] = turn.Status.ToString(),
                ["selectedIds"] = new JArray(selected.Select(x => x.UniqueId)),
                ["casterId"] = rider.UniqueId, ["targetId"] = target.UniqueId,
                ["pointerObject"] = Id(pointer), ["handlerObject"] = Id(handler),
                ["selectedHandlerObject"] = Id(SimulatedHandler.GetValue(pointer)),
                ["abilityObject"] = Id(ability), ["selectedAbilityObject"] = Id(handler.Ability),
                ["abilityGuid"] = ability.Blueprint.AssetGuid, ["simulatingClick"] = PointerController.SimulatingClick,
                ["ignored"] = ignored, ["riderCommandsEmpty"] = rider.Commands.Empty,
                ["moveSlotObject"] = Id(rider.Commands.GetCommand(Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Move)),
                ["riderPosition"] = new JArray(rider.Position.x, rider.Position.y, rider.Position.z),
                ["targetPosition"] = new JArray(target.Position.x, target.Position.y, target.Position.z),
                ["preview"] = NativeMountApproachPathProbe.SnapshotConsumedPath(PathVisualizer.Instance?.CurrentPathForUnit(rider.View)) });
        }
        internal JObject Capture() => new JObject { ["contract"] = "native-selected-ability-hover-prediction-and-ignore-click-before-one-commit",
            ["casterId"] = rider.UniqueId, ["targetId"] = target.UniqueId, ["clicked"] = clicked,
            ["ready"] = ready, ["restored"] = restored, ["samples"] = samples.DeepClone() };
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            try { if (ReferenceEquals(handler.Ability, ability)) handler.DropAbility(); }
            finally
            {
                PointerOn.SetValue(pointer, saved[0], null); WorldPosition.SetValue(pointer, saved[1], null);
                SimulatedPosition.SetValue(pointer, saved[2]); SimulatedHandler.SetValue(pointer, saved[3]); MouseHandler.SetValue(pointer, saved[4]);
                restored = ReferenceEquals(PointerOn.GetValue(pointer, null), saved[0]) && Equals(WorldPosition.GetValue(pointer, null), saved[1]) &&
                    Equals(SimulatedPosition.GetValue(pointer), saved[2]) && ReferenceEquals(SimulatedHandler.GetValue(pointer), saved[3]) && ReferenceEquals(MouseHandler.GetValue(pointer), saved[4]);
                if (!restored) throw new InvalidOperationException("Native Mount pointer lease did not restore its exact fields.");
            }
        }
    }
}
