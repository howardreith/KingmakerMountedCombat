using System;
using System.Reflection;
using Kingmaker;
using Kingmaker.Controllers.Units;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Domain;
using TurnBased.Controllers;

namespace KingmakerMountedCombat.Integration
{
    internal sealed partial class UnifiedMountedTurnCoordinator
    {
        private PairedActivation<UnitEntityData, TurnController> activation;
        private UnitEntityData armedRider;
        private UnitEntityData armedMount;
        private object activationSession;
        private TurnController partnerContext;
        private UnitEntityData preparingConfusionActor;
        private int splitReleaseRound = -1;
        private long pairedRenewalNotBefore;
        private TurnController resumingContext;
        private static readonly MethodInfo NativeToEnd = ResolveMethod(typeof(TurnController), "ToEnd", 0x06000C45, Type.EmptyTypes);
        internal static void CompleteNativeForfeitPhase(TurnController context) => NativeToEnd.Invoke(context, null);
        private static readonly MethodInfo NativeEnd = ResolveMethod(typeof(TurnController), "End", 0x06000C46, Type.EmptyTypes);
        private static readonly MethodInfo NativeContinueActing = ResolveMethod(typeof(TurnController), "ContinueActing", 0x06000C3D, Type.EmptyTypes);
        private static readonly MethodInfo NativeStatus = ResolveMethod(typeof(TurnController), "set_Status", 0x06000C0F, new[] { typeof(TurnController.TurnStatus) });
        private static readonly MethodInfo NativeConfusionTick = ResolveMethod(typeof(UnitConfusionController), "TickOnUnit", 0x06009131, new[] { typeof(UnitEntityData) });
        private static readonly FieldInfo SurpriseContext = ResolveField(typeof(TurnController), "m_ActingInSurpriseRound", 0x0400066D);

        internal bool PairedLifecycleEnabled => !disposed && settings.EnablePairedActivation;
        internal string ActivationIdentity => activation?.Identity;
        internal long ActivationSequence => activation?.Sequence ?? 0;
        internal TurnController PartnerContext => partnerContext;
        internal bool IsPartnerContext(TurnController turn) => turn != null && ReferenceEquals(partnerContext, turn);
        internal bool IsPreparingPairedActor(UnitEntityData actor) => PairedLifecycleEnabled &&
            activation?.IsPreparingActor(actor, Game.Instance?.TurnBasedCombatController?.CurrentTurn) == true;
        internal bool PairedActorEnded(UnitEntityData actor) => activation?.State(actor)?.Ended == true;

        internal bool CanAddressActor(UnitEntityData actor, TurnController turn)
        {
            return PairedLifecycleEnabled && activation != null && activation.CanAddress(actor, turn) &&
                (turn.Status == TurnController.TurnStatus.Preparing || turn.IsActing) &&
                relationship.State == RelationshipState.Mounted && relationship.Rider == activation.Principal &&
                relationship.Mount == activation.Partner;
        }

        internal bool CanMovePairedMount(TurnController turn)
        {
            var mount = activation?.Partner;
            if (!CanAddressActor(mount, turn)) return false;
            var getUp = PartnerCanGetUp &&
                mount.HasMoveAction() && mount.Descriptor.State.CanStandUp;
            // Native ContinueActing accepts its get-up state while the prone
            // animation still prevents ordinary action. CanStandUp retains the
            // native helpless/dazed/stunned restriction; standing costs Move.
            if (!mount.IsAbleToAct() && !getUp) return false;
            if (!mount.Descriptor.State.CanMove && !getUp) return false;
            var input = SelectedNativeInputContext(turn);
            // Native prone processing requires a real Move action. A remaining
            // free step cannot pay for standing up.
            return movementState.HasGrantedMovement(mount, !getUp && input.EnabledFiveFootStep,
                !getUp && input.EnabledSingleActionMove);
        }

        private void ArmPairedEncounter(UnitEntityData rider, UnitEntityData mount)
        {
            if (!PairedLifecycleEnabled || rider == null || mount == null || rider.IsInCombat || mount.IsInCombat ||
                (Game.Instance?.Player?.IsInCombat ?? true)) return;
            armedRider = rider;
            armedMount = mount;
            activationSession = Game.Instance.Player;
            LastInitiativeObservation = "armed-before-encounter;rider-principal=" + rider.UniqueId;
        }

        internal void BeginNativeEncounter(CombatController controller, bool isPartyCombatStateChanged)
        {
            if (!PairedLifecycleEnabled || activation != null || armedRider == null || armedMount == null ||
                activationSession != Game.Instance?.Player || relationship.State != RelationshipState.Mounted ||
                relationship.Rider != armedRider || relationship.Mount != armedMount) return;
            // The relationship was armed outside combat. This hook precedes the
            // controller roster and first native selection, including fixture TB entry.
            if (controller.CurrentTurn != null) throw new InvalidOperationException("Paired ownership cannot replace a running native turn.");
            activation = new PairedActivation<UnitEntityData, TurnController>(armedRider, armedMount);
            // Adopting an encounter that has already run in RT cannot manufacture
            // an immediate round/reaction grant. Native time must first advance a
            // whole resource period, and both actors' real debt must recover.
            pairedRenewalNotBefore = isPartyCombatStateChanged ? 0 :
                Game.Instance.TimeController.GameTime.Ticks + TimeSpan.TicksPerSecond * 6;
            LastInitiativeObservation = "encounter-owned;identity=" + activation.EncounterId +
                ";principal=" + armedRider.UniqueId + ";nativeCombatStart=" + isPartyCombatStateChanged;
        }

        internal bool SuppressPairedCandidate(CombatController.TBUnitInfo candidate)
        {
            if (PairedLifecycleEnabled && pendingSplitMount != null)
            {
                if (Game.Instance.TurnBasedCombatController.RoundNumber > pendingSplitRound || !pendingSplitMount.IsInCombat)
                    pendingSplitMount = null;
                else if (candidate?.Unit == pendingSplitMount) return true;
            }
            if (!PairedLifecycleEnabled || activation == null || candidate?.Unit != activation.Partner) return false;
            if (activation.Split && Game.Instance.TurnBasedCombatController.RoundNumber > splitReleaseRound)
            {
                // A new native round only releases participation. The actor still
                // waits for native readiness and receives its grant in Prepare.
                return false;
            }
            RedundantMountTurnSkipCount++;
            LastTurnCandidateObservation = "candidate-loop-excluded;activation=" + activation.Identity +
                ";mount=" + candidate.Unit.UniqueId;
            return true;
        }

        private bool BeginPairedPreparation(TurnController turn)
        {
            if (activation == null || turn == null) return true;
            if (IsPartnerContext(turn))
            {
                if (!activation.BeginActorPreparation(turn.Unit, activation.Boundary)) return false;
                movementState.BeginGrantedPreparation(turn, activation.Identity);
                return true;
            }
            if (activation.Split || turn.Unit != activation.Principal ||
                !ReferenceEquals(Game.Instance.TurnBasedCombatController.CurrentTurn, turn)) return true;
            if (activation.Suspended)
            {
                if (!activation.Resume(turn)) throw new InvalidOperationException("Invalid paired native delay resume.");
                resumingContext = turn;
                LastInitiativeObservation = "native-delay-resumed-existing-grant;identity=" + activation.Identity;
                return true;
            }
            if (!activation.Begin(turn)) return false;
            nativePreparationCommands.Clear();
            if (!activation.BeginActorPreparation(turn.Unit, turn)) return false;
            movementState.BeginGrantedPreparation(turn, activation.Identity);
            return true;
        }

        private void CompletePairedPreparation(TurnController turn)
        {
            if (activation == null || activation.Split || turn == null) return;
            if (ReferenceEquals(resumingContext, turn))
            {
                resumingContext = null;
                SynchronizePartnerPhase(turn);
                return;
            }
            var actor = activation.State(turn.Unit);
            if (actor == null || !actor.Granted || actor.Prepared) return;
            activation.FinishActorPreparation(turn.Unit);
            ObservePairedCosts(turn.Unit);
            if (IsPartnerContext(turn)) { MountLedgerPrepareCount++; return; }
            if (!ReferenceEquals(activation.Boundary, turn)) return;
            DisposePartnerContext();
            // This native context supplies complete preparation and actor command
            // callbacks. It is never Start()ed, Tick()ed, selected, or CurrentTurn.
            partnerContext = new TurnController(activation.Partner);
            SurpriseContext.SetValue(partnerContext, Game.Instance.TurnBasedCombatController.IsActingSurpriseCommands(turn.Unit));
            RefreshPartnerNativeState();
            partnerContext.Prepare();
            SynchronizePartnerPhase(turn);
            logger.Info("Paired activation prepared: " + activation.Identity + ";principal=" + turn.Unit.UniqueId +
                ";partner=" + activation.Partner.UniqueId + ";native-preparations=2.");
        }

        internal bool NativeActorEligibleForCommand(UnitEntityData actor, UnitCommand command)
        {
            if (actor.IsCurrentUnit()) return true;
            var turn = Game.Instance?.TurnBasedCombatController?.CurrentTurn;
            return OwnsNativePreparationCommand(actor, command, turn) ||
                CanAddressActor(actor, turn) && actor == activation.Partner &&
                command != null && command.Executor == actor &&
                (!command.IsIgnoreCooldown || command.GetType() == typeof(UnitMoveTo)) &&
                combat != null && combat.OwnsExactPairedNativeCommand(command);
        }

        internal void SynchronizePartnerPhase(TurnController turn)
        {
            if (!PairedLifecycleEnabled || activation == null || partnerContext == null ||
                !ReferenceEquals(turn, activation.Boundary) || activation.Split && !HasNativePreparationActivity(turn)) return;
            // A private native context has no Tick driver. Its phase follows the
            // actual principal transition so native command-end processing does
            // not return early in Preparing and omit movement cost finalization.
            if ((turn.IsActing || turn.IsEnding) && partnerContext.Status != turn.Status)
                NativeStatus.Invoke(partnerContext, new object[] { turn.Status });
        }

        internal bool HasPairedActivity(TurnController turn)
        {
            return turn.IsActed() || HasNativePreparationActivity(turn) || CanAddressActor(activation?.Partner, turn) &&
                (combat.HasActiveCommand || combat.HasActiveGroundMovement);
        }

        internal void TickPreparationConfusion(UnitConfusionController controller, TurnController turn)
        {
            if (!PairedLifecycleEnabled || activation == null || activation.State(turn.Unit) == null)
            { controller.Tick(); return; }
            if (preparingConfusionActor != null) throw new InvalidOperationException("Nested native confusion preparation.");
            preparingConfusionActor = turn.Unit;
            try
            {
                if (IsPartnerContext(turn))
                {
                    if (!IsPreparingPairedActor(turn.Unit))
                        throw new InvalidOperationException("Partner condition preparation has no pending native grant.");
                    PairedConfusionPreparation.Prepare(turn.Unit);
                }
                else NativeConfusionTick.Invoke(controller, new object[] { turn.Unit });
            }
            finally { preparingConfusionActor = null; }
        }

        private void ObservePairedCosts(UnitEntityData actor)
        {
            var state = activation?.State(actor);
            if (state == null || !state.Granted) return;
            var cooldown = actor.CombatState.Cooldown;
            state.Observe(cooldown.StandardAction, cooldown.MoveAction, cooldown.SwiftAction);
        }

        private bool PartnerHasAction(TurnController turn)
        {
            if (HasNativePreparationActivity(turn)) return true;
            if (!CanAddressActor(activation?.Partner, turn) || partnerContext == null) return false;
            // Native continuation includes its Auto End preference, remaining
            // action time, get-up exception and condition/AI completion policy.
            // An unused step alone does not override native automatic completion.
            // This runs only when the principal's native predicate would end;
            // the private context never ticks or becomes an activation driver.
            return (bool)NativeContinueActing.Invoke(partnerContext, null);
        }

        internal void ExtendPairedWaiting(TurnController turn, ref bool result)
        {
            if (!PairedLifecycleEnabled || activation == null || !ReferenceEquals(turn, activation.Boundary)) return;
            SynchronizePartnerPhase(turn);
            if (HasNativePreparationActivity(turn) || activation.Partner.IsInState && activation.Partner.IsAbleToAct() &&
                (activation.Partner.Commands.IsRunning() || combat.HasActiveCommand || combat.HasActiveGroundMovement)) result = true;
        }

        internal void ForfeitPairedActivation(TurnController turn, bool setCooldowns)
        {
            if (!PairedLifecycleEnabled || activation == null || !ReferenceEquals(turn, activation.Boundary)) return;
            if (ReferenceEquals(turn, nativeConditionForfeitContext)) return;
            if (!setCooldowns) return; // Native delay's false path is not an allocation forfeiture.
            activation.BeginEnding();
            ObservePairedCosts(turn.Unit); ObservePairedCosts(activation.Partner);
            InterruptNativePreparationCommands();
            combat.Cancel("native paired End Turn");
            if (partnerContext != null)
            {
                partnerContext.ForceToEnd();
            }
        }

        internal void EndPairedServiceParticipation()
        {
            if (!PairedLifecycleEnabled || activation == null) return;
            var boundary = activation.Boundary;
            if (boundary != null && !activation.Finalized)
            {
                // Disable/unload must finish the granted resources while the
                // native actor contexts and scoped adapters are still available.
                boundary.ForceToEnd();
                NativeEnd.Invoke(boundary, null);
            }
            nativePreparationCommands.Clear();
        }
        internal void FinishPairedActivation(TurnController turn)
        {
            if (!PairedLifecycleEnabled || activation == null || !ReferenceEquals(turn, activation.Boundary)) return;
            if (activation.Finalized) return;
            activation.BeginEnding();
            if (partnerContext != null) NativeEnd.Invoke(partnerContext, null);
            ObservePairedCosts(activation.Principal); ObservePairedCosts(activation.Partner);
            activation.EndActor(activation.Principal); activation.EndActor(activation.Partner);
            activation.FinalizeActivation();
            nativePreparationCommands.Clear();
            logger.Info("Paired activation ended: " + activation.Identity);
            DisposePartnerContext();
        }

        private void SplitPairedActivation(CleanupTrigger trigger)
        {
            if (activation == null) { armedRider = null; armedMount = null; return; }
            ObservePairedCosts(activation.Principal); ObservePairedCosts(activation.Partner);
            activation.Detach();
            splitReleaseRound = Game.Instance?.TurnBasedCombatController?.RoundNumber ?? -1;
            LastSplitObservation = "split-retains-current-grant;activation=" + activation.Identity + ";trigger=" + trigger;
            logger.Info("Paired split retains mount participation until the next native round: " + LastSplitObservation);
        }

        private void MaintainPairedLifetime()
        {
            if (!(Game.Instance?.Player?.IsInCombat ?? false)) pendingSplitMount = null;
            if (activationSession != null && activationSession != Game.Instance?.Player ||
                activation != null && !(Game.Instance?.Player?.IsInCombat ?? false))
            {
                // Encounter participation is over. Native cooldowns continue to
                // represent any real-time recovery; removing these supplemental
                // references never clears or refunds those native costs.
                if (activation != null)
                {
                    movementState.RetireCompletedEncounterActor(activation.Principal);
                    movementState.RetireCompletedEncounterActor(activation.Partner);
                }
                nativePreparationCommands.Clear();
                DisposePartnerContext(); activation = null; activationSession = null;
                armedRider = null; armedMount = null; splitReleaseRound = -1; pairedRenewalNotBefore = 0; resumingContext = null;
            }
            // Native removal can already have retired activation ownership while
            // the relationship survives combat exit. Arm the next encounter even
            // then; ArmPairedEncounter requires both actors and the party outside
            // combat and never prepares actors or writes native resources.
            if (activation == null && relationship.State == RelationshipState.Mounted)
                ArmPairedEncounter(relationship.Rider, relationship.Mount);
        }

        private void DisposePartnerContext()
        {
            var context = partnerContext;
            partnerContext = null;
            pairedMovementInputContext = null;
            lastPresentedInputContext = null;
            context?.Dispose();
        }
    }
}
