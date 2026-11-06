using System;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.TurnBasedMode.Controllers;
using Kingmaker.UI.Selection;
using Kingmaker.UI.TurnBasedMode;
using TurnBased.Controllers;

namespace KingmakerMountedCombat.Integration
{
    // Selection addresses an actor; it never changes activation ownership.
    internal sealed partial class MountedPairTurnParticipant
    {
        private TurnController lastPresentedInputContext;
        private TurnController pairedMovementInputContext;
        private static readonly System.Reflection.MethodInfo NativeShowTurnPanel =
            ResolveMethod(typeof(TurnBasedModeUIController), "ShowTurnPanel", 0x06003090, Type.EmptyTypes);
        private static readonly System.Reflection.MethodInfo NativeInputPredictions =
            ResolveMethod(typeof(TurnController), "UpdateActionPredictions", 0x06000C6E, Type.EmptyTypes);
        private static readonly System.Reflection.MethodInfo NativeSelectMovementLimit =
            ResolveMethod(typeof(TurnController), "TrySelectMovementLimit", 0x06000C42, Type.EmptyTypes);
        private static readonly System.Reflection.FieldInfo NativeMovementLimit = ResolveField(typeof(TurnController), "m_MovementLimit", 0x0400067E);
        private static readonly System.Reflection.FieldInfo NativeHighlightedUnit = ResolveField(typeof(TurnController), "m_HighlightedUnit", 0x0400068B);
        private static readonly System.Reflection.FieldInfo NativeHighlightedObject = ResolveField(typeof(TurnController), "m_HighlightedObject", 0x0400068D);
        private object mirroredGroundLimit;
        private static readonly System.Reflection.FieldInfo NativeModifyCurrent = ResolveField(typeof(TurnController), "m_ModifyMovementCurrentValue", 0x04000680);
        private static readonly System.Reflection.FieldInfo NativeModifyRequested = ResolveField(typeof(TurnController), "m_ModifyMovementRequestValue", 0x04000681);

        internal bool MaySelectPairedPartner(UnitEntityData actor) => actor != null &&
            actor == activation?.Partner && CanAddressActor(actor, Game.Instance?.TurnBasedCombatController?.CurrentTurn);

        internal bool PreservePartnerSelection(TurnController turn) =>
            CanAddressActor(activation?.Partner, turn) &&
            SelectionManager.Instance?.SingleSelectedUnit == activation.Partner;

        internal TurnController SelectedNativeInputContext(TurnController native)
        {
            if (!PreservePartnerSelection(native) || partnerContext == null) return native;
            return partnerContext;
        }

        // Locomotion belongs to the mount whichever pair actor is selected: the path
        // and execution read the mount's own allocation, never the carried rider's
        // untouched movement fields.
        internal TurnController PairedMovementContext(TurnController native)
        {
            var selected = SelectionManager.Instance?.SingleSelectedUnit;
            if (partnerContext == null || !CanAddressActor(activation?.Partner, native) ||
                selected != activation.Principal && selected != activation.Partner)
                return SelectedNativeInputContext(native);
            if (selected == activation.Principal && partnerContext.Unit.IsDirectlyControllable &&
                !(partnerContext.Unit.View?.IsMoving() ?? false))
            {
                // The step key and cycled ground limit stay on the native input context, whose click
                // admission and path refresh read them; the mount mirrors those choices. A limit the
                // rider's own spent budget or a hovered target's smart variant imposes is not a ground
                // choice: the mount keeps its last one, which native validates against its allocation.
                var input = SelectedNativeInputContext(native);
                NativeModifyRequested.SetValue(partnerContext, NativeModifyRequested.GetValue(input));
                NativeModifyCurrent.SetValue(partnerContext, NativeModifyCurrent.GetValue(input));
                if (input.HasNormalMovement() && NativeHighlightedUnit.GetValue(input) == null && NativeHighlightedObject.GetValue(input) == null)
                    mirroredGroundLimit = NativeMovementLimit.GetValue(input);
                NativeMovementLimit.SetValue(partnerContext, mirroredGroundLimit ?? TurnController.MovementLimit.TwoActions);
                NativeSelectMovementLimit.Invoke(partnerContext, null);
            }
            return partnerContext;
        }

        // The private context's prediction slots are refreshed only while the mount is
        // selected. For a rider-selected path, read the mount's real budget instead.
        internal CombatAction.ActionState PairedMovementPredictionState(CombatAction action, CombatAction.ActionState native)
        {
            if (partnerContext == null || action == null || activation == null ||
                SelectionManager.Instance?.SingleSelectedUnit != activation.Principal) return native;
            var mount = partnerContext.Unit;
            if (ReferenceEquals(action, partnerContext.ActionsStates.Move))
                return mount.CombatState.Cooldown.MoveAction < 3f ? CombatAction.ActionState.Available : CombatAction.ActionState.Unable;
            if (ReferenceEquals(action, partnerContext.ActionsStates.Standard))
                return mount.UsedStandardAction() ? CombatAction.ActionState.Unable : CombatAction.ActionState.Available;
            return native;
        }

        internal TurnController NativeActorActionContext(TurnController native, UnitEntityData actor) =>
            actor != null && actor == activation?.Partner && (CanAddressActor(actor, native) || OwnsNativeConditionActorContext(actor, native)) && partnerContext != null
                ? partnerContext : native;

        internal bool IsNativeActionContextActor(UnitEntityData actor) => actor.IsCurrentUnit() ||
            CanAddressActor(actor, Game.Instance?.TurnBasedCombatController?.CurrentTurn) ||
            OwnsNativeConditionActorContext(actor, Game.Instance?.TurnBasedCombatController?.CurrentTurn);

        internal bool ShouldRunNativeIgnoreClick(TurnController context, ref bool result)
        {
            var addressed = SelectedNativeInputContext(context);
            if (ReferenceEquals(context, addressed)) return true;
            result = addressed.IgnoreClick();
            return false;
        }

        internal bool PrepareNativeInputPrediction(TurnController context)
        {
            if (!PairedLifecycleEnabled || activation == null ||
                !ReferenceEquals(context, activation.Boundary) && !ReferenceEquals(context, partnerContext)) return true;
            var addressed = SelectedNativeInputContext(activation.Boundary);
            if (ReferenceEquals(context, addressed)) return true;
            if (ReferenceEquals(context, activation.Boundary)) NativeInputPredictions.Invoke(addressed, null);
            return false;
        }

        internal void RememberPairedMovementInput()
        {
            var turn = Game.Instance?.TurnBasedCombatController?.CurrentTurn;
            pairedMovementInputContext = CanAddressActor(activation?.Partner, turn) ? PairedMovementContext(turn) : null;
        }

        private TurnController MovementInputContext(TurnController turn) => pairedMovementInputContext != null &&
            (ReferenceEquals(pairedMovementInputContext, turn) || ReferenceEquals(pairedMovementInputContext, partnerContext))
                ? pairedMovementInputContext : PairedMovementContext(turn);

        private void RefreshPairedInputPresentation(TurnController turn)
        {
            if (!CanAddressActor(activation?.Partner, turn)) { lastPresentedInputContext = null; return; }
            var context = SelectedNativeInputContext(turn);
            if (ReferenceEquals(context, lastPresentedInputContext)) return;
            var ui = Game.Instance?.UI?.TurnBasedUI;
            if (ui == null) return;
            // Native ShowTurnPanel disposes the old prediction view model and
            // binds the existing PC controls. No turn/fact/command callbacks.
            NativeShowTurnPanel.Invoke(ui, null);
            lastPresentedInputContext = context;
        }
    }
}
