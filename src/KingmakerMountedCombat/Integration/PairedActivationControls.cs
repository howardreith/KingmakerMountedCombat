using System;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.Selection;
using Kingmaker.UI.TurnBasedMode;
using TurnBased.Controllers;

namespace KingmakerMountedCombat.Integration
{
    // Selection addresses an actor; it never changes activation ownership.
    internal sealed partial class UnifiedMountedTurnCoordinator
    {
        private TurnController lastPresentedInputContext;
        private TurnController pairedMovementInputContext;
        private static readonly System.Reflection.MethodInfo NativeShowTurnPanel =
            ResolveMethod(typeof(TurnBasedModeUIController), "ShowTurnPanel", 0x06003090, Type.EmptyTypes);
        private static readonly System.Reflection.MethodInfo NativeInputPredictions =
            ResolveMethod(typeof(TurnController), "UpdateActionPredictions", 0x06000C6E, Type.EmptyTypes);

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

        internal TurnController NativeActorActionContext(TurnController native, UnitEntityData actor) =>
            actor != null && actor == activation?.Partner && CanAddressActor(actor, native) && partnerContext != null
                ? partnerContext : native;

        internal bool IsNativeActionContextActor(UnitEntityData actor) => actor.IsCurrentUnit() ||
            CanAddressActor(actor, Game.Instance?.TurnBasedCombatController?.CurrentTurn);

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
            pairedMovementInputContext = CanAddressActor(activation?.Partner, turn) ? SelectedNativeInputContext(turn) : null;
        }

        private TurnController MovementInputContext(TurnController turn) => pairedMovementInputContext != null &&
            (ReferenceEquals(pairedMovementInputContext, turn) || ReferenceEquals(pairedMovementInputContext, partnerContext))
                ? pairedMovementInputContext : SelectedNativeInputContext(turn);

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
