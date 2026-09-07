using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Controllers.Combat;
using Kingmaker.Controllers.Units;
using Kingmaker.EntitySystem.Entities;
using TurnBased.Controllers;

namespace KingmakerMountedCombat.Integration
{
    internal sealed partial class UnifiedMountedTurnCoordinator
    {
        private static readonly System.Reflection.FieldInfo NativeCanGetUp =
            ResolveField(typeof(TurnController), "UnitCanGetUpOnCommand", 0x04000670);
        private static readonly System.Reflection.MethodInfo NativeCanGetUpValue =
            ResolveMethod(NativeCanGetUp.FieldType, "set_Value", 0x06000446, new[] { typeof(bool) });
        private static readonly System.Reflection.MethodInfo NativeCanGetUpReader =
            ResolveMethod(NativeCanGetUp.FieldType, "get_Value", 0x06000445, Type.EmptyTypes);
        internal bool PartnerCanGetUp => partnerContext != null &&
            (bool)NativeCanGetUpReader.Invoke(NativeCanGetUp.GetValue(partnerContext), null);

        internal bool OwnsCompletionDebt(UnitCombatState.Cooldowns cooldown)
        {
            return PairedLifecycleEnabled && activation != null && activation.Sequence > 0 &&
                (ReferenceEquals(cooldown, activation.Principal.CombatState.Cooldown) ||
                 ReferenceEquals(cooldown, activation.Partner.CombatState.Cooldown));
        }

        internal float NativeCompletionValue(UnitCombatState.Cooldowns cooldown, float current, float native) =>
            OwnsCompletionDebt(cooldown) ? Math.Max(current, native) : native;

        internal bool IsPairedResume(TurnController turn) => PairedLifecycleEnabled && ReferenceEquals(resumingContext, turn);

        internal bool CanDelayPaired(TurnController turn)
        {
            if (!PairedLifecycleEnabled || activation == null || !ReferenceEquals(turn, activation.Boundary)) return true;
            if (!CanAddressActor(activation.Partner, turn) || turn.IsActed() || partnerContext == null ||
                partnerContext.IsActed() || !turn.Unit.Commands.Empty || !activation.Partner.Commands.Empty ||
                combat.HasActiveCommand || combat.HasActiveGroundMovement) return false;
            // A step spends participation despite having no Standard/Move debit.
            return turn.TimeMoved <= 0f && partnerContext.TimeMoved <= 0f;
        }

        internal bool BeginPairedDelay(TurnController turn, UnitEntityData target)
        {
            if (!PairedLifecycleEnabled || activation == null || !ReferenceEquals(turn, activation.Boundary)) return true;
            var controller = Game.Instance.TurnBasedCombatController;
            var units = controller.SortedUnits.ToList();
            var wait = target?.GetTimeToNextTurn() ?? float.PositiveInfinity;
            if (!CanDelayPaired(turn) || target == activation.Partner || target == activation.Principal ||
                units.IndexOf(target) <= units.IndexOf(activation.Principal) || wait >= controller.TimeToNextRound)
            {
                combat.RejectPairedControl("Paired Delay requires both actors to be unused and a later actor in this native round.");
                return false;
            }
            ObservePairedCosts(activation.Principal); ObservePairedCosts(activation.Partner);
            if (!activation.Suspend(turn))
            {
                combat.RejectPairedControl("This paired allocation has already been used and cannot be delayed.");
                return false;
            }
            LastInitiativeObservation = "native-delay-suspended-existing-grant;identity=" + activation.Identity;
            return true;
        }

        internal float PairedNativeReadiness(UnitEntityData actor)
        {
            var native = actor.GetTimeToNextTurn();
            if (!PairedLifecycleEnabled || activation == null || activation.Split || actor != activation.Principal)
                return native;
            // The initial pre-combat policy follows rider initiative. Once either
            // paired grant or RT participation exists, both native debts matter.
            if (activation.Sequence > 0 || pairedRenewalNotBefore != 0)
                native = Math.Max(native, activation.Partner.GetTimeToNextTurn());
            return Math.Max(native, (float)((pairedRenewalNotBefore -
                Game.Instance.TimeController.GameTime.Ticks) / (double)TimeSpan.TicksPerSecond));
        }

        internal void BeforeNativeModeExit(CombatController controller)
        {
            if (!PairedLifecycleEnabled || !controller.Initialized) return;
            pendingSplitRound = -1;
            if (activation == null) return;
            var boundary = activation.Boundary;
            if (boundary != null && !activation.Finalized)
            {
                // Native Disable disposes CurrentTurn without End. Complete the
                // ONE paired allocation before that disposal; callbacks still see
                // its authoritative boundary. This forfeits remaining actions.
                boundary.ForceToEnd();
                NativeEnd.Invoke(boundary, null);
            }
            pairedRenewalNotBefore = Game.Instance.TimeController.GameTime.Ticks + TimeSpan.TicksPerSecond * 6;
            // Native RT readiness now carries split participation; a TB round
            // number reset must not keep a detached actor suppressed indefinitely.
            if (activation.Split) splitReleaseRound = -1;
            LastSplitObservation = "native-mode-exit-forfeits-pair;identity=" + activation.Identity;
            logger.Info(LastSplitObservation);
        }

        private void RefreshPartnerNativeState()
        {
            if (partnerContext == null) return;
            var actor = partnerContext.Unit;
            NativeCanGetUpValue.Invoke(NativeCanGetUp.GetValue(partnerContext), new object[] {
                actor.IsDirectlyControllable && actor.Descriptor.State.Prone.Active && !UnitProneController.ShouldBeProne(actor) });
        }

        internal void TickPairedNativeState(TurnController turn)
        {
            if (!PairedLifecycleEnabled || activation == null || !ReferenceEquals(turn, activation.Boundary)) return;
            RefreshPartnerNativeState();
            movementState.ResetTransientStepImmunity(partnerContext);
            RefreshPairedInputPresentation(turn);
        }

        internal bool IsPairedProneActor(UnitEntityData actor) => actor.IsCurrentUnit() ||
            CanAddressActor(actor, Game.Instance?.TurnBasedCombatController?.CurrentTurn);

        internal void BeforePairedActorRemoval(UnitEntityData actor)
        {
            if (!PairedLifecycleEnabled || activation == null ||
                actor != activation.Principal && actor != activation.Partner) return;
            var pair = activation;
            var boundary = pair.Boundary;
            if (boundary != null && !pair.Finalized)
            {
                boundary.ForceToEnd();
                NativeEnd.Invoke(boundary, null);
            }
            // Removal may choose/dispose without End. Finish before that selector
            // boundary, then retain only the survivor's participation exclusion.
            if (actor == pair.Principal && pair.Partner.IsInCombat)
            {
                pendingSplitMount = pair.Partner;
                pendingSplitRound = Game.Instance?.TurnBasedCombatController?.RoundNumber ?? -1;
            }
            DisposePartnerContext();
            nativePreparationCommands.Clear();
            activation = null; armedRider = null; armedMount = null; resumingContext = null;
            LastSplitObservation = "native-actor-removal-ended-pair;actor=" + actor.UniqueId + ";identity=" + pair.Identity;
            logger.Info(LastSplitObservation);
        }
    }
}
