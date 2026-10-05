using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.Utility;
using Kingmaker.View;
using KingmakerMountedCombat.Domain;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Integration
{
    // Chunk 6B increment 6B.2: the admission surface of the pair-owned Mounted Charge, kept in one removable
    // partial so the whole feature is one policy, one lease, one ability component and this file.
    //
    // The rider owns the native full-round Standard ability shell and therefore the cost; the mount owns the
    // forced path. The charge is admitted as an ordinary rider melee pair transaction carrying a charge lease,
    // which is why no new action kind, evaluator branch or admission path is introduced. The command is queued
    // first on the rider with IgnoreCooldown, exactly as the stock charge queues its attack, because the shell
    // has already paid and Commands.Run would interrupt the shell that is paying.
    internal sealed partial class MountedCombatController
    {
        internal const string MountedChargeDisabledReason = "Enable Mounted Charge in the KMC mod settings.";

        // The stock minimum charge distance, read from the mount because the mount is the mover, and from the
        // turn-based mode setting exactly as AbilityCustomCharge.GetMinRangeMeters reads it.
        internal float GetMountedChargeMinimumRange(UnitEntityData caster)
        {
            var mount = relationship.Mount;
            if (disposed || mount?.View == null || caster == null || caster != relationship.Rider)
            {
                return 0f;
            }

            return MountedChargeMinimumRange(mount, null);
        }

        private static float MountedChargeMinimumRange(UnitEntityData mount, UnitEntityData target)
        {
            var targetCorpulence = target?.View == null ? 0.5f : target.View.Corpulence;
            if (Kingmaker.UI.SettingsUI.SettingsRoot.Instance.EnableTurnBasedMode.CurrentValue)
            {
                return TurnController.MetersOfFiveFootStep + GameConsts.MinWeaponRange.Meters +
                    mount.View.Corpulence + targetCorpulence;
            }

            return 10.Feet().Meters + mount.View.Corpulence + targetCorpulence;
        }

        private static float MountedChargeMaximumRange(UnitEntityData mount)
        {
            return mount.CombatSpeedMps * 6f;
        }

        // The stock clearance check, read from the mount. The carried rider is never an obstacle to its own
        // mount, and the landing point is one rider weapon reach short of the target, as the stock check is.
        private bool MountedChargeLandingBlocked(UnitEntityData mount, UnitEntityData target)
        {
            var rider = relationship.Rider;
            var weapon = rider?.GetFirstWeapon();
            var separation = mount.View.Corpulence + target.View.Corpulence + (weapon == null ? 0f : weapon.AttackRange.Meters);
            var direction = (target.Position - mount.Position).To2D().normalized;
            var landing = target.Position.To2D() - direction * separation;
            var state = Game.Instance?.State;
            if (state?.AwakeUnits == null)
            {
                return false;
            }

            return state.AwakeUnits.Any(actor => actor != mount && actor != rider && actor != target &&
                actor.View != null && actor.View.MovementAgent != null && !actor.View.MovementAgent.AvoidanceDisabled &&
                (landing - actor.Position.To2D()).magnitude < (mount.View.Corpulence + actor.View.Corpulence) * 0.8f);
        }

        // Delivery revalidation reaches this controller only from the Deliver of this mod's own charge
        // component, which the engine has already charged for. The fact is proven from the live execution
        // context rather than assumed from the call site: the executing ability must carry this mod's charge
        // component and its caster must be this pair's rider. Anything else is not a delivery of our shell.
        private bool DeliveringOwnChargeShell(AbilityExecutionContext context, UnitEntityData caster)
        {
            var blueprint = context == null || context.Ability == null ? null : context.Ability.Blueprint;
            return blueprint != null && caster != null && caster == relationship.Rider &&
                context.Caster == caster && blueprint.GetComponent<MountedChargeAbilityLogic>() != null;
        }

        private MountedChargeRequest CaptureMountedChargeRequest(UnitEntityData caster, UnitEntityData target,
            AbilityExecutionContext context = null)
        {
            var rider = relationship.Rider;
            var mount = relationship.Mount;
            var turnBased = CombatController.IsInTurnBasedCombat();
            var turn = Game.Instance?.TurnBasedCombatController?.CurrentTurn;
            var weapon = rider?.GetFirstWeapon();
            var targetState = target?.Descriptor?.State;
            var request = new MountedChargeRequest
            {
                FeatureEnabled = settings.EnableMountedCharge && settings.EnableUnsafeMovementExperiment,
                RelationshipMounted = relationship.State == RelationshipState.Mounted,
                ExactPair = rider != null && mount != null && caster == rider &&
                    target != null && target != rider && target != mount,
                RiderDirectlyControllable = rider != null && rider.IsDirectlyControllable,
                LifecycleBoundary = relationship.State == RelationshipState.Faulted,
                InCombat = rider != null && rider.IsInCombat,
                TurnBased = turnBased,
                RiderTurn = turn != null && turn.Unit == rider,
                TurnActingOrPreparing = turn != null &&
                    (turn.Status == TurnController.TurnStatus.Preparing || turn.IsActing),
                TurnActing = turn != null && turn.IsActing,
                TurnTimeMoved = turn == null ? 0f : turn.TimeMoved,
                RiderCanActInCombat = rider != null && rider.CombatState.CanActInCombat && rider.IsAbleToAct(),
                RiderStandardCooldown = rider == null ? 0f : rider.CombatState.Cooldown.StandardAction,
                RiderMoveCooldown = rider == null ? 0f : rider.CombatState.Cooldown.MoveAction,
                WeaponPresent = weapon != null,
                WeaponIsRanged = weapon?.Blueprint != null && weapon.Blueprint.IsRanged,
                TargetValid = target != null && target.IsInState && targetState != null &&
                    targetState.IsConscious && !targetState.IsFinallyDead,
                TargetVisible = target != null && target.IsVisibleForPlayer,
                TargetHostile = rider != null && target != null && rider.IsEnemy(target),
                TargetAttackable = rider != null && target != null && rider.CanAttack(target),
                AlreadyActiveCommand = HasActiveCommand || HasActiveGroundMovement || HasActiveDoorInteraction,
                MountCommandsIdle = mount?.Commands != null && mount.Commands.Empty && mount.Commands.Queue.Count == 0,
                DeliveringOwnShell = DeliveringOwnChargeShell(context, caster)
            };

            if (mount?.View != null && target?.View != null)
            {
                var endpoint = ObstacleAnalyzer.TraceAlongNavmesh(mount.Position, target.Position);
                request.Distance = (target.Position - mount.Position).magnitude;
                request.MinimumRange = MountedChargeMinimumRange(mount, target);
                request.MaximumRange = MountedChargeMaximumRange(mount);
                request.StraightRoute = endpoint == target.Position;
                request.MountAvoidanceDisabled = mount.View.MovementAgent != null && mount.View.MovementAgent.AvoidanceDisabled;
                request.LandingBlocked = MountedChargeLandingBlocked(mount, target);
            }
            else
            {
                request.StraightRoute = false;
            }

            return request;
        }

        // Availability has no target, so the geometry checks are deferred to targeting. Nothing is written.
        internal NativeMountedControlAvailability EvaluateMountedCharge(UnitEntityData caster)
        {
            if (disposed)
            {
                return new NativeMountedControlAvailability(false, false, "Mounted combat services are not active.");
            }

            if (!settings.EnableMountedCharge)
            {
                return new NativeMountedControlAvailability(false, false, MountedChargeDisabledReason);
            }

            var request = CaptureMountedChargeRequest(caster, null);
            request.TargetValid = true;
            request.TargetVisible = true;
            request.TargetHostile = true;
            request.TargetAttackable = true;
            request.ExactPair = relationship.Rider != null && relationship.Mount != null && caster == relationship.Rider;
            request.Distance = request.MinimumRange;
            request.StraightRoute = true;
            request.LandingBlocked = false;
            var availability = MountedChargePolicy.Evaluate(request);
            return new NativeMountedControlAvailability(true, availability.IsAllowed, availability.Reason);
        }

        internal bool CanMountedChargeTarget(UnitEntityData caster, UnitEntityData target)
        {
            if (disposed || target == null)
            {
                return false;
            }

            return MountedChargePolicy.Evaluate(CaptureMountedChargeRequest(caster, target)).IsAllowed;
        }

        internal string LastMountedChargeRefusal { get; private set; }

        internal int MountedChargeAdmittedCount { get; private set; }

        internal int MountedChargeRefusedCount { get; private set; }

        // The last admitted charge transaction, kept so a diagnostic observer can read its lease facts.
        internal MountedPairAttackCommand LastMountedChargeCommand { get; private set; }

        // Delivery from the rider's native full-round ability shell. The shell has already spent the rider's
        // action through Spell.Spend, so the pair transaction is queued first with IgnoreCooldown rather than
        // run, which is exactly what AbilityCustomCharge does with its own attack.
        internal MountedCombatClickResult TryExecuteMountedCharge(UnitEntityData caster, UnitEntityData target,
            AbilityExecutionContext context)
        {
            if (disposed)
            {
                return MountedCombatClickResult.NotHandled;
            }

            var request = CaptureMountedChargeRequest(caster, target, context);
            var availability = MountedChargePolicy.Evaluate(request);
            logger.Info("Mounted charge delivery observed: casterId=" + (caster?.UniqueId ?? "<none>") +
                "; targetId=" + (target?.UniqueId ?? "<none>") +
                "; turnBased=" + request.TurnBased + "; ownShell=" + request.DeliveringOwnShell +
                "; riderStandardCooldown=" + request.RiderStandardCooldown.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                "; distance=" + request.Distance.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                "; minimum=" + request.MinimumRange.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                "; maximum=" + request.MaximumRange.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                "; straight=" + request.StraightRoute + "; landingBlocked=" + request.LandingBlocked +
                "; allowed=" + availability.IsAllowed + "; reason=" + availability.Reason);
            if (!availability.IsAllowed)
            {
                return RefuseMountedCharge(availability.Reason, availability.RejectionCode ?? MountedCombatRejectionCode.WrongActionState);
            }

            if (HasStockAttackIntent)
            {
                Cancel("mounted charge replaced stock attack intent");
            }

            NativeSingleAttackWeaponSelection mountPrimary;
            CaptureContext(MountedCombatActionKind.RiderMelee, target, out mountPrimary, false, false);
            MountedPairAttackCommand command = null;
            var queuedOnRider = false;
            try
            {
                command = new MountedPairAttackCommand(
                    relationship,
                    relationship.Rider,
                    relationship.Mount,
                    target,
                    MountedCombatActionKind.RiderMelee,
                    mountPrimary,
                    horsePrimaryAttackAnimation,
                    logger,
                    HandleCommandTerminal,
                    true,
                    true,
                    true);
                command.NativePartnerMovement = settings.EnablePairedActivation;
                // The enclosing full-round shell owns the cost; the queued transaction must not be refused for
                // the action the shell has already spent, and must never be given an action of its own.
                command.IgnoreCooldown();
                var schedulerRequired = pairedCommandScheduler.RequiresLease(MountedCombatActionKind.RiderMelee);
                string schedulerReason;
                if (schedulerRequired && !pairedCommandScheduler.TryRegister(command, out schedulerReason))
                {
                    return RefuseMountedCharge(
                        "Mounted pair scheduler rejected the charge registration: " + schedulerReason + ".",
                        MountedCombatRejectionCode.CommandAdmissionFailure);
                }

                activeCommand = command;
                unifiedTurn.RememberPairedMovementInput();
                LastOutcome = null;
                var rider = relationship.Rider;
                rider.Commands.AddToQueueFirst(command);
                queuedOnRider = true;
                // The one diagnostics-only seam, for proving compensation from exactly here. Null in
                // production; an armed hook can only throw, which drives the refusal path below.
                MountedChargeAdmissionFault.FireAfterQueue();
                if (command.Executor != rider || command.IsFinished ||
                    (!rider.Commands.Contains(command) && !rider.Commands.Queue.Contains(command)))
                {
                    CompensateMountedChargeAdmission(command, rider, "native charge queue admission failed");
                    return RefuseMountedCharge(
                        "Mounted charge failed to enter the rider command queue.",
                        MountedCombatRejectionCode.CommandAdmissionFailure);
                }

                if (schedulerRequired && !pairedCommandScheduler.ConfirmAdmission(command, out schedulerReason))
                {
                    CompensateMountedChargeAdmission(command, rider,
                        "mounted pair scheduler rejected the charge admission: " + schedulerReason);
                    return RefuseMountedCharge(
                        "Mounted pair scheduler rejected the charge admission: " + schedulerReason + ".",
                        MountedCombatRejectionCode.CommandAdmissionFailure);
                }

                rider.CombatState.ManualTarget = target;
                LastMountedChargeCommand = command;
                MountedChargeAdmittedCount++;
                LastMountedChargeRefusal = null;
                LastRejectionCodes = new MountedCombatRejectionCode[0];
                LastFeedback = "Mounted charge accepted: the " + MountDisplayName + " carries the charge.";
                logger.Info("Mounted charge accepted: riderId=" + rider.UniqueId +
                    "; targetId=" + target.UniqueId + "; queuedFirst=true; ignoreCooldown=true.");
                return MountedCombatClickResult.HandledAccepted;
            }
            catch (Exception exception)
            {
                logger.Exception("Mounted charge admission", exception);
                if (command != null)
                {
                    // Whether or not the queue admission completed, every owner the attempt may have
                    // acquired is resolved here before the field is cleared.
                    CompensateMountedChargeAdmission(command, relationship.Rider,
                        "mounted charge admission threw " + exception.GetType().Name +
                        (queuedOnRider ? " after queue admission" : " before queue admission"));
                }
                else
                {
                    activeCommand = null;
                }

                return RefuseMountedCharge(
                    "Mounted charge admission failed: " + exception.GetType().Name + ".",
                    MountedCombatRejectionCode.CommandAdmissionFailure);
            }
        }

        // One helper for every charge failure after the command entered the rider queue. Each step resolves
        // a different native owner and every step runs even if an earlier one fails, because skipping one
        // strands it. activeCommand is cleared only as the final step, so native ownership is always
        // resolved first. Nothing is refunded: no cooldown, resource, preparation or turn is written here,
        // so a native shell cost that was already committed stands exactly as the engine took it.
        private MountedChargeCompensation CompensateMountedChargeAdmission(
            MountedPairAttackCommand command, UnitEntityData rider, string reason)
        {
            var compensation = new MountedChargeCompensation(reason, new[]
            {
                new MountedChargeCompensationStep("abandon-scheduler",
                    () => pairedCommandScheduler.AbandonRegistration(command, reason)),
                new MountedChargeCompensationStep("interrupt-command", () =>
                {
                    if (!command.IsFinished)
                    {
                        command.Interrupt(false);
                    }
                }),
                new MountedChargeCompensationStep("dequeue-command", () =>
                {
                    var commands = rider == null ? null : rider.Commands;
                    if (commands == null)
                    {
                        return;
                    }

                    if (commands.Contains(command) || commands.Queue.Contains(command))
                    {
                        commands.RemoveFinishedAndUpdateQueue();
                    }
                }),
                new MountedChargeCompensationStep("restore-lease", command.CompensateChargeLease),
                new MountedChargeCompensationStep("clear-active-command", () => { activeCommand = null; })
            });
            compensation.Run();

            var commandsAfter = rider == null ? null : rider.Commands;
            LastChargeCompensation = compensation.Describe();
            LastChargeCompensationComplete = compensation.Complete;
            LastChargeCompensationCommandResident = commandsAfter != null &&
                (commandsAfter.Contains(command) || commandsAfter.Queue.Contains(command));
            LastChargeCompensationLeaseRestored = command.ChargeLeaseRestored;
            LastChargeCompensationActiveCommandCleared = activeCommand == null;
            ChargeCompensationCount++;
            logger.Info("Mounted charge admission compensated: " + compensation.Describe() +
                "; commandResident=" + LastChargeCompensationCommandResident +
                "; leaseRestored=" + LastChargeCompensationLeaseRestored +
                "; activeCommandCleared=" + LastChargeCompensationActiveCommandCleared +
                "; commandFinished=" + command.IsFinished + ".");
            return compensation;
        }

        internal int ChargeCompensationCount { get; private set; }

        internal string LastChargeCompensation { get; private set; }

        internal bool LastChargeCompensationComplete { get; private set; }

        internal bool LastChargeCompensationCommandResident { get; private set; }

        internal bool LastChargeCompensationLeaseRestored { get; private set; }

        internal bool LastChargeCompensationActiveCommandCleared { get; private set; }

        private MountedCombatClickResult RefuseMountedCharge(string reason, MountedCombatRejectionCode code)
        {
            MountedChargeRefusedCount++;
            LastMountedChargeRefusal = reason;
            LastFeedback = reason;
            LastRejectionCodes = new[] { code };
            logger.Info("Rejected mounted charge: code=" + code + "; reason=" + reason);
            return MountedCombatClickResult.HandledRejected;
        }
    }
}
