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
        internal bool CanConfigurePairedActivation => !disposed &&
            relationship.State == RelationshipState.Unmounted &&
            Game.Instance?.Player?.IsInCombat != true &&
            Game.Instance?.TurnBasedCombatController?.Initialized != true &&
            activation == null && partnerContext == null && pendingSplitMount == null;
        internal string PairedConfigurationFeedback { get; private set; }

        internal bool TryConfigurePairedActivation(bool enabled)
        {
            if (enabled == settings.EnablePairedActivation) return true;
            if (!CanConfigurePairedActivation)
            {
                PairedConfigurationFeedback = "Configure paired activation outside combat while dismounted.";
                return false;
            }
            if (enabled && (settings.EnableUnifiedMountedTurn || settings.EnablePairedCommandScheduler))
            {
                PairedConfigurationFeedback = "Disable both legacy turn experiments before enabling paired activation.";
                return false;
            }
            // Configuration selects the next pre-combat relationship policy. It
            // never retires a live allocation, prepares an actor or changes debt.
            settings.EnablePairedActivation = enabled;
            PairedConfigurationFeedback = enabled ? "Paired activation enabled for pre-combat mounting." : "Paired activation disabled.";
            logger.Info(PairedConfigurationFeedback);
            return true;
        }
        internal string ActivationIdentity => activation?.Identity;
        internal bool ActivationSplit => activation?.Split == true;
        internal bool ActivationFinalized => activation?.Finalized == true;
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

        private static readonly MethodInfo NativeFindUnitInfo = ResolveMethod(typeof(CombatController),
            "FindUnitInfo", 0x06000BBB, new[] { typeof(UnitEntityData) });
        private static readonly FieldInfo NativeSurprised =
            ResolveField(typeof(CombatController.TBUnitInfo), "Surprised", 0x04007069);
        private static readonly FieldInfo NativeActingInSurpriseRound =
            ResolveField(typeof(CombatController.TBUnitInfo), "ActingInSurpriseRound", 0x0400706F);

        internal long MidEncounterAdoptionCount { get; private set; }

        internal long AdoptionPlanCount { get; private set; }

        internal long AdoptionRevalidationFailureCount { get; private set; }

        internal long AdoptionRollbackCount { get; private set; }

        internal string LastAdoptionObservation { get; private set; } = "not-requested";

        internal string LastAdoptionPlanObservation { get; private set; } = "not-planned";

        // Side-effect-free: usable from availability and from admission.
        internal MidEncounterAdoption ResolveMidEncounterAdoption(
            UnitEntityData rider, UnitEntityData mount, out string refusal)
        {
            MidEncounterAdoptionPlan ignored;
            return ObserveMidEncounterAdoption(rider, mount, out ignored, out refusal);
        }

        // Relationship attachment and encounter adoption must be one transaction,
        // so the admission path takes an immutable generation-bound plan here,
        // revalidates it immediately before the relationship commit, and carries it
        // into the commit. Returns false only when a required adoption cannot be
        // planned; when the paired lifecycle is not enabled there is nothing to
        // adopt and the plan is null.
        internal bool TryPlanMidEncounterAdoption(
            UnitEntityData rider, UnitEntityData mount, out MidEncounterAdoptionPlan plan, out string refusal)
        {
            plan = null;
            refusal = null;
            if (!PairedLifecycleEnabled) return true;
            var disposition = ObserveMidEncounterAdoption(rider, mount, out plan, out refusal);
            if (disposition == MidEncounterAdoption.Unavailable || plan == null)
            {
                if (string.IsNullOrWhiteSpace(refusal))
                {
                    refusal = "The mounted pair cannot take over this encounter's activation yet.";
                }
                plan = null;
                return false;
            }
            AdoptionPlanCount++;
            LastAdoptionPlanObservation = "planned;" + plan.Describe();
            return true;
        }

        // Exact revalidation: the live encounter is observed again and compared
        // field by field. Any difference refuses the transition before the
        // relationship is committed, so nothing has to be compensated.
        internal bool RevalidateMidEncounterAdoptionPlan(
            MidEncounterAdoptionPlan plan, UnitEntityData rider, UnitEntityData mount, out string refusal)
        {
            refusal = null;
            if (plan == null) return true;
            MidEncounterAdoptionPlan current;
            string observationRefusal;
            ObserveMidEncounterAdoption(rider, mount, out current, out observationRefusal);
            if (current == null || !plan.Matches(current))
            {
                AdoptionRevalidationFailureCount++;
                var difference = plan.DescribeDifference(current) ?? observationRefusal ?? "the encounter changed";
                refusal = "The encounter changed before this mount could be completed: " + difference + ".";
                LastAdoptionPlanObservation = "revalidation-failed;" + refusal + ";planned={" + plan.Describe() +
                    "};observed={" + (current == null ? "<none>" : current.Describe()) + "}";
                return false;
            }
            if (!plan.EncounterStillLive)
            {
                AdoptionRevalidationFailureCount++;
                refusal = "The encounter ended before this mount could be completed.";
                LastAdoptionPlanObservation = "revalidation-failed;" + refusal;
                return false;
            }
            LastAdoptionPlanObservation = "revalidated;" + plan.Describe();
            return true;
        }

        private MidEncounterAdoption ObserveMidEncounterAdoption(
            UnitEntityData rider, UnitEntityData mount, out MidEncounterAdoptionPlan plan, out string refusal)
        {
            plan = null;
            refusal = null;
            if (!PairedLifecycleEnabled)
            {
                refusal = "Paired activation is disabled.";
                return MidEncounterAdoption.Unavailable;
            }
            if (rider == null || mount == null || ReferenceEquals(rider, mount))
            {
                refusal = "Adoption requires two exact distinct actors.";
                return MidEncounterAdoption.Unavailable;
            }
            if (!CanReplaceActivationForAdoption())
            {
                refusal = "The pair's previous mounted participation is still resolving this round.";
                return MidEncounterAdoption.Unavailable;
            }

            var turnBased = CombatController.IsInTurnBasedCombat();
            var controller = Game.Instance?.TurnBasedCombatController;
            var turn = controller?.CurrentTurn;
            var riderIndex = -1;
            var mountIndex = -1;
            if (turnBased && controller != null)
            {
                var index = 0;
                foreach (var unit in controller.SortedUnits)
                {
                    if (ReferenceEquals(unit, rider)) riderIndex = index;
                    if (ReferenceEquals(unit, mount)) mountIndex = index;
                    index++;
                }
            }

            bool surprised = false;
            bool actingInSurpriseRound = false;
            if (turnBased && controller != null && mountIndex >= 0)
            {
                var info = NativeFindUnitInfo.Invoke(controller, new object[] { mount });
                if (info == null)
                {
                    refusal = "Rider and mount must both be in this encounter's initiative order to mount during it.";
                    return MidEncounterAdoption.Unavailable;
                }
                surprised = (bool)NativeSurprised.GetValue(info);
                actingInSurpriseRound = (bool)NativeActingInSurpriseRound.GetValue(info);
            }

            var currentTurnIsExactRider = turn != null && ReferenceEquals(turn.Unit, rider);
            var disposition = MidEncounterAdoptionPolicy.Resolve(
                turnBased,
                currentTurnIsExactRider,
                riderIndex,
                mountIndex,
                surprised,
                actingInSurpriseRound,
                mount.IsVisibleForPlayer);
            if (disposition == MidEncounterAdoption.Unavailable)
            {
                refusal = MidEncounterAdoptionPolicy.DescribeUnavailable(
                              turnBased, currentTurnIsExactRider, riderIndex, mountIndex) ??
                          "The mounted pair cannot take over this encounter's activation yet.";
                return disposition;
            }

            // Every observation above is recorded so a change between admission
            // and delivery is detected by exact comparison. Building the plan
            // writes nothing and reserves nothing.
            var game = Game.Instance;
            plan = new MidEncounterAdoptionPlan(
                disposition,
                relationship.MountedPairGeneration,
                game?.CurrentlyLoadedArea?.AssetGuidThreadSafe,
                rider.UniqueId,
                mount.UniqueId,
                turnBased,
                controller?.RoundNumber ?? -1,
                turn?.Unit?.UniqueId,
                riderIndex,
                mountIndex,
                surprised,
                actingInSurpriseRound,
                mount.IsVisibleForPlayer,
                rider.IsInCombat,
                mount.IsInCombat,
                game?.Player?.IsInCombat ?? false,
                rider.IsAbleToAct(),
                mount.IsAbleToAct(),
                rider.Descriptor?.State?.IsConscious ?? false,
                mount.Descriptor?.State?.IsConscious ?? false);
            return disposition;
        }

        // A split activation still governs the partner's participation until its
        // release round has passed. Until then a fresh voluntary combat Mount is
        // refused rather than layered on top of it.
        private bool CanReplaceActivationForAdoption()
        {
            if (activation == null) return partnerContext == null;
            if (!activation.Split) return false;
            if (!CombatController.IsInTurnBasedCombat()) return true;
            var round = Game.Instance?.TurnBasedCombatController?.RoundNumber ?? -1;
            return splitReleaseRound < 0 || round > splitReleaseRound;
        }

        // One explicit typed adoption for a pair created during a running
        // encounter. It calls no encounter-start code, no candidate selection, no
        // JoinCombat, and no preparation for the principal. The partner's single
        // native preparation runs only for the PreparePartnerThisRound
        // disposition, whose own later slot is then suppressed.
        // The commit half of the one combat-mount transaction. It runs only with
        // the immutable plan taken when the transition was admitted and already
        // revalidated field by field immediately before the relationship commit.
        //
        // Ordering contract: every failure path in this method precedes the single
        // native partner preparation. Nothing that can be refused happens after
        // partnerContext.Prepare(), because that call clears the partner's native
        // cooldowns and cannot be undone. A rollback therefore never has to
        // un-prepare an actor.
        internal string AdoptRunningEncounter(
            MidEncounterAdoptionPlan plan, UnitEntityData rider, UnitEntityData mount)
        {
            if (plan == null)
            {
                return "Adoption requires the plan taken when this transition was admitted.";
            }
            if (rider == null || mount == null ||
                !string.Equals(plan.RiderId, rider.UniqueId, StringComparison.Ordinal) ||
                !string.Equals(plan.MountId, mount.UniqueId, StringComparison.Ordinal))
            {
                return "Adoption requires the exact planned pair.";
            }
            var injected = ConsumeAdoptionFault(rider, mount);
            if (injected != null)
            {
                return injected;
            }
            if (plan.RelationshipGeneration != relationship.MountedPairGeneration)
            {
                return "The mounted relationship generation changed after this transition was planned.";
            }
            if (!string.Equals(plan.EncounterSessionId,
                    Game.Instance?.CurrentlyLoadedArea?.AssetGuidThreadSafe, StringComparison.Ordinal))
            {
                return "The area changed after this transition was planned.";
            }
            if (relationship.State != RelationshipState.Mounted ||
                !ReferenceEquals(relationship.Rider, rider) || !ReferenceEquals(relationship.Mount, mount))
            {
                return "Adoption requires the exact live mounted pair.";
            }
            if (!(Game.Instance?.Player?.IsInCombat ?? false) && !rider.IsInCombat && !mount.IsInCombat)
            {
                return "Adoption requires a live encounter.";
            }

            // The disposition is the one decision this commit depends on, and it is
            // resolved from scheduling state alone, so re-resolving it here cannot
            // be perturbed by the presentation the relationship commit just
            // attached. A change since the pre-commit revalidation is refused.
            string refusal;
            var disposition = ResolveMidEncounterAdoption(rider, mount, out refusal);
            if (disposition == MidEncounterAdoption.Unavailable)
            {
                return refusal;
            }
            if (disposition != plan.Disposition)
            {
                return "The companion's participation in this round changed from " + plan.Disposition +
                    " to " + disposition + " before the mount could be completed.";
            }

            if (activation != null)
            {
                // Its release round has passed; retire the detached record only.
                // Native cooldowns keep every observed cost; movement allocations
                // are left untouched so no real expenditure is discarded.
                DisposePartnerContext();
                nativePreparationCommands.Clear();
                activation = null;
                splitReleaseRound = -1;
                resumingContext = null;
            }

            armedRider = rider;
            armedMount = mount;
            activationSession = Game.Instance.Player;
            var adopted = new PairedActivation<UnitEntityData, TurnController>(rider, mount);
            var controller = Game.Instance.TurnBasedCombatController;
            var turn = controller?.CurrentTurn;
            if (disposition == MidEncounterAdoption.RealTimeOwnership)
            {
                activation = adopted;
                // This encounter is already running, exactly as when turn-based
                // mode is enabled mid-combat: a whole native resource period must
                // pass before the pair may renew.
                pairedRenewalNotBefore = Game.Instance.TimeController.GameTime.Ticks + TimeSpan.TicksPerSecond * 6;
               
                MidEncounterAdoptionCount++;
                LastAdoptionObservation = "adopted-real-time-ownership;identity=" + adopted.EncounterId +
                    ";principal=" + rider.UniqueId + ";partner=" + mount.UniqueId + ";no-grant;no-prepare";
                LastInitiativeObservation = LastAdoptionObservation;
                logger.Info("Paired activation adopted a running real-time encounter: " + LastAdoptionObservation + ".");
                return null;
            }

            if (!adopted.AdoptRunningBoundary(turn, disposition))
            {
                return "The rider's running native turn could not be adopted.";
            }
            activation = adopted;
            nativePreparationCommands.Clear();
            // The principal's native Prepare already ran at its own slot; the
            // observation records the debt it stands at, it does not reset it.
            ObservePairedCosts(rider);
           
            if (disposition == MidEncounterAdoption.RetainPartnerParticipation)
            {
                // The partner's slot in this round is already behind the running
                // turn, so its allocation is recorded as ended and it gets no
                // private native context at all. The observation records the debt
                // it genuinely stands at; nothing is cleared, refreshed or
                // replayed, and no native preparation or callback runs for it.
                if (partnerContext != null)
                {
                    activation = null;
                    return "A retained partner cannot own a private native turn context.";
                }
                ObservePairedCosts(mount);
                if (!activation.State(mount).Ended)
                {
                    activation = null;
                    return "A retained partner's spent allocation was not closed.";
                }
            }
            else
            {
                if (!activation.BeginActorPreparation(mount, turn))
                {
                    activation = null;
                    return "The partner's native preparation could not be reserved.";
                }
                DisposePartnerContext();
                // Identical to the accepted pre-combat path: this private native
                // context supplies the partner's ONE preparation and its actor
                // command callbacks. It is never Start()ed, Tick()ed or selected.
                partnerContext = new TurnController(mount);
                SurpriseContext.SetValue(partnerContext, controller.IsActingSurpriseCommands(rider));
                RefreshPartnerNativeState();
                partnerContext.Prepare();
                SynchronizePartnerPhase(turn);
            }
            MidEncounterAdoptionCount++;
            LastAdoptionObservation = "adopted-running-turn;identity=" + activation.Identity +
                ";principal=" + rider.UniqueId + ";partner=" + mount.UniqueId +
                ";disposition=" + disposition + ";round=" + (controller?.RoundNumber ?? -1) +
                ";principalPrepareCalls=0;partnerPrepareCalls=" +
                (disposition == MidEncounterAdoption.PreparePartnerThisRound ? 1 : 0) +
                ";partnerEnded=" + (activation.State(mount)?.Ended == true) +
                ";partnerAddressable=" + CanAddressActor(mount, turn) +
                ";partnerContext=" + (partnerContext != null) +
                ";partnerStandardObserved=" + (activation.State(mount)?.StandardSpent ?? -1f) +
                ";partnerMoveObserved=" + (activation.State(mount)?.MoveSpent ?? -1f) +
                ";partnerSwiftObserved=" + (activation.State(mount)?.SwiftSpent ?? -1f);
            LastInitiativeObservation = LastAdoptionObservation;
            logger.Info("Paired activation adopted a running turn-based encounter: " + LastAdoptionObservation + ".");
            return null;
        }

        // Exact compensating cleanup for a combat Mount whose adoption did not
        // complete. It removes KMC bookkeeping only: the partial activation, the
        // private partner context, any preparation reservation, the armed pair and
        // the renewal floor. It writes no native cooldown, calls no native
        // preparation or end, and refunds nothing, so a native Move that Kingmaker
        // has already committed stays spent and pre-existing actor debt is
        // untouched. The mounted-pair generation is deliberately left advanced:
        // that is what retires every native shell created against the old
        // relationship, so a repeated delivery of the failed control is refused.
        internal void RollbackMidEncounterAdoption(string reason)
        {
            DisposePartnerContext();
            nativePreparationCommands.Clear();
            activation = null;
            armedRider = null;
            armedMount = null;
            activationSession = null;
            splitReleaseRound = -1;
            resumingContext = null;
            pairedRenewalNotBefore = 0;
            preparedRiderTurn = null;
            pendingSplitMount = null;
            pendingSplitRound = -1;
            AdoptionRollbackCount++;
            LastAdoptionObservation = "adoption-rolled-back;reason=" + (reason ?? "<none>") +
                ";activation=none;partnerContext=false;preparationCommands=0";
            LastInitiativeObservation = LastAdoptionObservation;
            logger.Error("Paired activation rolled back an incomplete mid-encounter adoption: " +
                (reason ?? "<none>"));
        }

        // A bounded diagnostic adoption fault. It exists so the combat-mount
        // transaction's compensating path is observable in the running game rather
        // than argued from source: it makes the adoption commit refuse at its first
        // check, before any state change and before the single native partner
        // preparation, exactly as a late invalidation would.
        //
        // It writes nothing and cannot widen anything: it arms only for two exact
        // distinct actors while the relationship is unmounted and the paired
        // lifecycle is enabled, only one may be armed, it is bound to those exact
        // actor identities, it is consumed by the first matching adoption commit,
        // and disposing it disarms it.
        private AdoptionFault adoptionFault;

        internal long AdoptionFaultConsumedCount { get; private set; }

        internal IDisposable ArmMidEncounterAdoptionFault(UnitEntityData rider, UnitEntityData mount)
        {
            if (!PairedLifecycleEnabled)
            {
                throw new InvalidOperationException("A diagnostic adoption fault requires the paired lifecycle.");
            }
            if (relationship.State != RelationshipState.Unmounted || activation != null || partnerContext != null)
            {
                throw new InvalidOperationException("A diagnostic adoption fault arms only on an idle unmounted pair.");
            }
            if (rider == null || mount == null || ReferenceEquals(rider, mount))
            {
                throw new InvalidOperationException("A diagnostic adoption fault requires two exact distinct actors.");
            }
            if (adoptionFault != null)
            {
                throw new InvalidOperationException("A diagnostic adoption fault is already armed.");
            }
            adoptionFault = new AdoptionFault(this, rider.UniqueId, mount.UniqueId);
            return adoptionFault;
        }

        private sealed class AdoptionFault : IDisposable
        {
            private readonly UnifiedMountedTurnCoordinator owner;
            internal readonly string RiderId;
            internal readonly string MountId;
            internal bool Consumed;

            internal AdoptionFault(UnifiedMountedTurnCoordinator owner, string riderId, string mountId)
            {
                this.owner = owner;
                RiderId = riderId;
                MountId = mountId;
            }

            public void Dispose()
            {
                if (ReferenceEquals(owner.adoptionFault, this)) { owner.adoptionFault = null; }
            }
        }

        private string ConsumeAdoptionFault(UnitEntityData rider, UnitEntityData mount)
        {
            var fault = adoptionFault;
            if (fault == null || fault.Consumed ||
                !string.Equals(fault.RiderId, rider?.UniqueId, StringComparison.Ordinal) ||
                !string.Equals(fault.MountId, mount?.UniqueId, StringComparison.Ordinal))
            {
                return null;
            }
            fault.Consumed = true;
            AdoptionFaultConsumedCount++;
            return "Injected diagnostic adoption fault: the planned encounter adoption was refused.";
        }

        // The adoption authority the relationship service binds. These are thin
        // explicit forwarders so the four operations above keep their internal
        // surface and their exact pinned signatures.
        bool IMidEncounterAdoptionAuthority.TryPlanMidEncounterAdoption(
            UnitEntityData rider, UnitEntityData mount,
            out MidEncounterAdoptionPlan plan, out string refusal) =>
            TryPlanMidEncounterAdoption(rider, mount, out plan, out refusal);

        bool IMidEncounterAdoptionAuthority.RevalidateMidEncounterAdoptionPlan(
            MidEncounterAdoptionPlan plan, UnitEntityData rider, UnitEntityData mount, out string refusal) =>
            RevalidateMidEncounterAdoptionPlan(plan, rider, mount, out refusal);

        string IMidEncounterAdoptionAuthority.AdoptRunningEncounter(
            MidEncounterAdoptionPlan plan, UnitEntityData rider, UnitEntityData mount) =>
            AdoptRunningEncounter(plan, rider, mount);

        void IMidEncounterAdoptionAuthority.RollbackMidEncounterAdoption(string reason) =>
            RollbackMidEncounterAdoption(reason);

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

        internal bool OwnsPartnerRoundEffects(UnitEntityData current, UnitEntityData actor)
        {
            var turn = Game.Instance?.TurnBasedCombatController?.CurrentTurn;
            return PairedLifecycleEnabled && activation != null && turn != null &&
                current == turn.Unit && current == activation.Principal && actor == activation.Partner &&
                partnerContext?.Unit == actor && relationship.State == RelationshipState.Mounted &&
                relationship.Rider == current && relationship.Mount == actor &&
                activation.OwnsRoundEffects(actor, turn);
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
