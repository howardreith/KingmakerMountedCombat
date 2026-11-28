using System;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using KingmakerMountedCombat.Domain;
using Kingmaker.TurnBasedMode;
using Kingmaker.TurnBasedMode.Controllers;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
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
        // One activation's ground choice: cleared with its partner context.
        private TurnController.MovementLimit mirroredGroundLimit;
        private bool hasMirroredGroundLimit;
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
            var riderSelected = activation != null && selected == activation.Principal;
            if (partnerContext == null || !PairedMovementInputPolicy.MountOwnsLocomotion(
                    CanAddressActor(activation?.Partner, native), riderSelected, selected != null && selected == activation?.Partner))
                return SelectedNativeInputContext(native);
            if (PairedMovementInputPolicy.MirrorsRiderInput(riderSelected, partnerContext.Unit.IsDirectlyControllable,
                    partnerContext.Unit.View?.IsMoving() ?? false))
            {
                // The step key and cycled ground limit stay on the native input context, whose click
                // admission and path refresh read them; the mount mirrors those choices. A limit the
                // rider's own spent budget or a hovered target's smart variant imposes is not a ground
                // choice: the mount keeps its last one, which native validates against its allocation.
                var input = SelectedNativeInputContext(native);
                NativeModifyRequested.SetValue(partnerContext, NativeModifyRequested.GetValue(input));
                NativeModifyCurrent.SetValue(partnerContext, NativeModifyCurrent.GetValue(input));
                var choice = PairedMovementInputPolicy.IsGroundLimitChoice(input.HasNormalMovement(),
                    NativeHighlightedUnit.GetValue(input) != null || NativeHighlightedObject.GetValue(input) != null);
                NativeMovementLimit.SetValue(partnerContext, PairedMovementInputPolicy.MountLimit(
                    (TurnController.MovementLimit)NativeMovementLimit.GetValue(input), choice,
                    ref mirroredGroundLimit, ref hasMirroredGroundLimit, TurnController.MovementLimit.TwoActions));
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

        private static readonly System.Reflection.MethodInfo NativeCurrentActionStates =
            ResolveMethod(typeof(TurnController), "ComputeCurrentActionStates", 0x06000C32, Type.EmptyTypes);
        private static readonly System.Reflection.MethodInfo NativeRestrictNormalMovement =
            ResolveMethod(typeof(TurnController), "ShouldRestrictNormalMovement", 0x06000C4F, Type.EmptyTypes);
        private bool mountStatesFilled;
        private bool fillingMountStates;

        // A native ActionsState reads CurrentTurn for its owner's movement range; the private context's own
        // states read the mount. Side-effect free.
        internal TurnController ActionsStateContext(ActionsState states) =>
            partnerContext != null && ReferenceEquals(states, partnerContext.ActionsStates) ? partnerContext : null;

        internal void BeginPairedPrediction(TurnController context)
        {
            if (activation != null && ReferenceEquals(context, activation.Boundary)) mountStatesFilled = false;
        }

        private bool RiderGroundPreview(bool requireFilled)
        {
            var boundary = activation?.Boundary;
            return partnerContext != null && boundary != null && PairedMovementInputPolicy.SlotsDescribeMount(
                SelectionManager.Instance?.SingleSelectedUnit == activation.Principal, CanAddressActor(activation.Partner, boundary),
                boundary.IsOverTerrain, !requireFilled || mountStatesFilled);
        }

        // After the native path writes its predictions into the rider's states during a rider-selected ground preview,
        // the same path writes them into the mount's: its states are first recomputed by native from the mount's own
        // cooldowns and restrictions (the turn-start ranges kept for the slider maximum), then its remaining ranges.
        internal void FillPairedMountStates(PathVisualizer path, ActionsState states)
        {
            if (fillingMountStates || activation?.Boundary == null || !ReferenceEquals(states, activation.Boundary.ActionsStates) ||
                !RiderGroundPreview(false) || !partnerContext.Unit.Commands.Empty || !activation.Principal.Commands.Empty) return;
            fillingMountStates = true;
            try
            {
                var mountStates = partnerContext.ActionsStates;
                var mount = partnerContext.Unit;
                var fresh = (ActionsState.Actions)NativeCurrentActionStates.Invoke(partnerContext, null);
                var moveCooldown = mount.CombatState.Cooldown.MoveAction;
                var restricted = (bool)NativeRestrictNormalMovement.Invoke(partnerContext, null);
                var moveFeet = (float)partnerContext.GetRemainingActionMovementRangeFeet(UnitCommand.CommandType.Move).Value;
                var standardFeet = (float)partnerContext.GetRemainingActionMovementRangeFeet(UnitCommand.CommandType.Standard).Value;
                // That computation is native's turn-start snapshot: it treats any started Move (UsedOneMoveAction) and any
                // started conversion as gone, and keeps a used step available (native's state machine settles those at the
                // command's own end, which a rider-selected pair move never delivers to the private context). Mid-turn the
                // remaining travel of each window stays, with that action's other uses lost; the live native
                // HasFiveFootStep is the mount's remaining step, a partial step's residual included.
                if (PairedMovementInputPolicy.PartialMoveRemains(moveCooldown, mount.IsMoveActionRestricted(), restricted, moveFeet))
                    Rebase(mountStates.Move, CombatAction.ActivityState.Available, CombatAction.ActivityState.Lost, CombatAction.ActivityState.Lost);
                else Rebase(mountStates.Move, fresh.Move);
                if (PairedMovementInputPolicy.ConversionTravelRemains(moveCooldown, mount.UsedStandardAction(), restricted, standardFeet))
                    Rebase(mountStates.Standard, CombatAction.ActivityState.Available, CombatAction.ActivityState.Lost, CombatAction.ActivityState.Lost);
                else Rebase(mountStates.Standard, fresh.Standard);
                // Known issues Sprint 7 (U01): a step is never promised once forced travel closed the activation's locomotion.
                if (MountedMovementDisplayPolicy.PromisesFiveFootStep(LocomotionClosedForActivation(), partnerContext.HasFiveFootStep()))
                    Rebase(mountStates.FiveFootStep, fresh.FiveFootStep);
                else Rebase(mountStates.FiveFootStep, CombatAction.ActivityState.Unable, null, null);
                mountStates.Clear();
                mountStates.UpdateMoveDistance();
                path.UpdateActionStatesFromPath(mountStates);
                mountStatesFilled = true;
            }
            finally { fillingMountStates = false; }
        }

        // Known issues Sprint 7 (U01): the mount's Move and Standard prediction actions show no usable distance once the
        // mount's allocation for this exact activation has recorded forced travel (a delivered Charge, or one that travelled
        // and was cancelled). A presentation projection on the distance native is about to write, before native announces
        // it: native's Unable state, cooldowns, TimeMoved, the allocation and the path are untouched. Returns whether the
        // action is this pair's mount's.
        internal bool ProjectUsableMoveDistance(CombatAction action, ref float? remaining)
        {
            if (action == null || partnerContext == null || activation == null) return false;
            var states = partnerContext.ActionsStates;
            if (states == null || !ReferenceEquals(action, states.Move) && !ReferenceEquals(action, states.Standard)) return false;
            remaining = MountedMovementDisplayPolicy.UsableDistanceFeet(LocomotionClosedForActivation(), remaining);
            return true;
        }

        private bool LocomotionClosedForActivation() => PairedLifecycleEnabled && partnerContext != null && activation != null &&
            movementState.ForcedTravelClosesActivation(partnerContext.Unit, activation.Identity);

        // Native's own action objects are kept (their subscriptions, turn-start slider ranges, absent activities and
        // identity). Native current-state setters only advance (Available is the lowest state), as the mount's entitlement
        // only shrinks within an activation; the native lock on the action's usage type is kept.
        private static void Rebase(CombatAction target, CombatAction source) =>
            Rebase(target, source.MovementActivityStateCurrent, source.AttackActivityStateCurrent, source.AbilityActivityStateCurrent);

        private static void Rebase(CombatAction target, CombatAction.ActivityState? move, CombatAction.ActivityState? attack, CombatAction.ActivityState? ability)
        {
            var locked = target.LockType;
            target.ChangeDefaultStates(move, attack, ability);
            target.LockType = locked;
        }

        // The movement-facing slot reads the mount's matching action during a filled rider-selected ground preview.
        internal CombatAction PairedSlotAction(CombatAction native)
        {
            if (native == null || !RiderGroundPreview(true)) return native;
            var rider = activation.Boundary.ActionsStates; var mount = partnerContext.ActionsStates;
            if (ReferenceEquals(native, rider.Move)) return mount.Move;
            if (ReferenceEquals(native, rider.FiveFootStep)) return mount.FiveFootStep;
            if (ReferenceEquals(native, rider.Standard)) return mount.Standard;
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

        private static readonly System.Reflection.FieldInfo NativeNeedNewPredictions =
            ResolveField(typeof(TurnController), "m_NeedNewPredictions", 0x04000691);
        // The commands native IgnoreClick's simulated click ran (swapped back into this field when its Temporary disposed).
        private static readonly System.Reflection.FieldInfo NativePredictionCommands =
            ResolveField(typeof(TurnController), "m_PredictionCommands", 0x0400066E);

        // MMR-02 (reviews R1-F3, R3-F2). After native IgnoreClick on the rider's own paired context: when native dropped the
        // exact predicted plain melee UnitAttack of the selected, addressable rider on the highlighted hostile, KMC admits the
        // click only if it can prove the order legal by its own rule (PairedAttackClickAdmissionPolicy): the strike already
        // admissible from where the pair stands, or a complete native route of the MOUNT's own agent whose cost is within the
        // mount's granted movement for the player's current step/single-move mode and whose endpoint admits the strike
        // (PairedAttackClickProbe). Every other drop keeps native's refusal, so the previous order is untouched. The step and
        // single-move flags are the player's input mode (mirrored onto the mount's context); the allowance is the mount's.
        internal void AdmitPairedAttackClick(TurnController context, ref bool result)
        {
            if (activation == null || context == null || !ReferenceEquals(context, activation.Boundary)) return;
            var combat = relationship.CombatOwner;
            // PR #70 R1-F7: a click native accepts carries native's own route; no earlier override proof may pass to it.
            if (!result) { combat?.DiscardPendingClickProof("native-accepted click"); return; }
            // PR #70 R4-F2 (round 5): an override is applied only inside an input dispatch that carries its proof to that dispatch's handler
            // (native PointerController.Tick, or the admission-aware direct input's own scoped dispatch). An unscoped IgnoreClick read keeps
            // native's result, so no handler can run an unbound order from it.
            if (combat == null || !combat.InPointerDispatch) { LastAttackClickDecision = "NativeDecision:outside-input-dispatch"; return; }
            var rider = activation.Principal;
            var mount = activation.Partner;
            var target = NativeHighlightedUnit.GetValue(context) as UnitEntityData;
            // PR #70 R5-F2 (round 6): a scoped direct input dispatches its own target in its own turn; an override proven for another
            // highlighted/predicted hostile (or in another turn) keeps native's refusal, so its handler never runs on that order.
            if (!combat.DispatchCarriesOverrideFor(target, Game.Instance?.TurnBasedCombatController?.CurrentTurn))
            {
                LastAttackClickDecision = "NativeDecision:dispatch-target-mismatch";
                return;
            }
            var predicted = (NativePredictionCommands.GetValue(context) as UnitCommands)?.GetCommand(UnitCommand.CommandType.Standard) as UnitAttack;
            var weapon = rider?.GetFirstWeapon();
            var meleeOnTarget = rider != null && mount != null && predicted != null && predicted.GetType() == typeof(UnitAttack) && target != null &&
                predicted.TargetUnit == target && !target.Descriptor.State.IsDead && rider.IsEnemy(target) &&
                weapon?.Blueprint != null && !weapon.Blueprint.IsRanged;
            var riderSelected = SelectionManager.Instance?.SingleSelectedUnit == rider;
            var riderAddressable = rider != null && CanAddressActor(rider, context);
            var predictionsCurrent = !(bool)NativeNeedNewPredictions.GetValue(context);
            var abilitySelected = Game.Instance?.SelectedAbilityHandler?.Ability != null;
            var riderCommandsEmpty = rider != null && rider.Commands.Empty;
            var inputCandidate = meleeOnTarget && riderSelected && riderAddressable && predictionsCurrent && !abilitySelected &&
                riderCommandsEmpty && !context.IsOverTerrain;
            string reason = "no-combat-owner", trace = "<none>";
            var decision = combat == null ? PairedAttackClickAdmission.NativeDecision :
                combat.AdmitTurnBasedAttackClick(target, context, inputCandidate, riderAddressable, riderSelected, riderCommandsEmpty,
                    predictionsCurrent, context.IsOverTerrain, abilitySelected, meleeOnTarget,
                    (out bool step, out bool singleMove, out string grantReason, out bool stepCounterBinds) =>
                        PairedMountGrantedMetres(context, out step, out singleMove, out grantReason, out stepCounterBinds),
                    PairedAttackClickProbe.PlanIdentity.Of(predicted), out reason, out trace);
            if (decision != PairedAttackClickAdmission.NativeDecision)
            {
                result = false;
                AdmittedAttackClickCount++;
            }
            if (!meleeOnTarget) return;
            LastAttackClickDecision = decision + ":" + reason;
            logger.Info("Turn-based rider melee attack click dropped by native's path check: decision=" + decision + "; reason=" + reason +
                "; rider=" + rider.UniqueId + "; target=" + target.UniqueId + "; " + trace +
                "; nativeApproachRadius=" + predicted.ApproachRadius.ToString("R") + ".");
        }

        internal int AdmittedAttackClickCount { get; private set; }
        internal string LastAttackClickDecision { get; private set; }

        internal bool PrepareNativeInputPrediction(TurnController context)
        {
            if (!PairedLifecycleEnabled || activation == null ||
                !ReferenceEquals(context, activation.Boundary) && !ReferenceEquals(context, partnerContext)) return true;
            BeginPairedPrediction(context);
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
