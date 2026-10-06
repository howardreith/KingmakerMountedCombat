using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Controllers;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Integration
{
    internal sealed partial class MountedCombatController
    {
        // One owner from native shell admission through final postconditions. These references
        // are never rediscovered through a replacement relationship or serialized.
        private sealed class ChargeOwner
        {
            internal UnitEntityData Rider, Mount, Target;
            internal UnitCommands RiderCommands, MountCommands;
            internal long Generation;
            internal UnitUseAbility Shell;
            internal object Process;
            internal AbilityExecutionContext Context;
            internal Kingmaker.Controllers.Combat.UnitCombatState ManualTargetState;
            internal bool ManualTargetOwned;
            internal Func<bool> ProcessEnded;
            internal MountedPairAttackCommand Command;
            internal MountedChargeOwnership Ownership;
            internal int AdmissionFrame;
            internal bool AdmissionCompensation;
            internal bool NativeActionInProgress;
            internal bool NativeActionFailed;
            internal RuleCastSpell NativeRule;
            internal AbilityExecutionController NativeExecutor;
            internal readonly List<AbilityExecutionProcess> Processes = new List<AbilityExecutionProcess>();
            internal bool ProcessObservationPending;
            internal string ProcessObservationError;
            internal bool Committed => Shell != null && Shell.IsActed;
        }

        private ChargeOwner chargeOwner;
        private object chargeAdmissionFence;
        internal bool HasChargeOwnership => chargeOwner != null;
        internal bool ChargeAdmissionFenced => chargeAdmissionFence != null || ChargeNativeBoundaryPending;
        internal string ChargeCleanupStatus => chargeOwner?.Ownership.LastAttempt?.Describe() ?? "settled";
        internal int ChargeCleanupAttemptCount { get; private set; }
        internal MountedPairAttackCommand OwnedChargeCommand => chargeOwner?.Command;
        internal UnitUseAbility OwnedChargeShell => chargeOwner?.Shell;
        internal JObject LastDrainedChargeOwnership { get; private set; }

        internal JObject CaptureChargeOwnership() => DescribeChargeOwner(chargeOwner);

        private JObject DescribeChargeOwner(ChargeOwner owner)
        {
            if (owner == null) return new JObject { ["owned"] = false, ["fenced"] = ChargeAdmissionFenced };
            return new JObject
            {
                ["owned"] = true, ["fenced"] = ChargeAdmissionFenced,
                ["identity"] = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(owner),
                ["shellIdentity"] = owner.Shell == null ? (int?)null : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(owner.Shell),
                ["ruleIdentity"] = owner.NativeRule == null ? (int?)null : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(owner.NativeRule),
                ["executorIdentity"] = owner.NativeExecutor == null ? (int?)null : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(owner.NativeExecutor),
                ["contextIdentity"] = owner.Context == null ? (int?)null : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(owner.Context),
                ["processes"] = new JArray(owner.Processes.Select(p => new JObject
                {
                    ["identity"] = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(p),
                    ["contextIdentity"] = p.Context == null ? (int?)null : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(p.Context),
                    ["ended"] = p.IsEnded
                })),
                ["rider"] = owner.Rider?.UniqueId, ["mount"] = owner.Mount?.UniqueId, ["target"] = owner.Target?.UniqueId,
                ["generation"] = owner.Generation, ["state"] = owner.Ownership.State.ToString(),
                ["cleanupRequested"] = owner.Ownership.CleanupRequested, ["attempts"] = owner.Ownership.AttemptCount,
                ["committed"] = owner.Committed, ["commandTerminal"] = owner.Command == null || owner.Command.IsFinished,
                ["riderSlotReleased"] = owner.Command == null || owner.RiderCommands.Standard != owner.Command,
                ["riderContainerReleased"] = Absent(owner.RiderCommands, owner.Command),
                ["schedulerAbsent"] = owner.Command == null || !pairedCommandScheduler.HasRegistration(owner.Command),
                ["carrierDrained"] = owner.Command == null || owner.Command.ChargeCarrierDrained,
                ["leaseDrained"] = owner.Command == null || owner.Command.ChargeCleanupComplete,
                ["lease"] = owner.Command?.CaptureChargeLeaseEvidence(),
                ["debt"] = owner.Command?.ChargeCleanupDebt,
                ["shellTerminal"] = owner.Shell == null || owner.Shell.IsFinished,
                ["shellContainerReleased"] = Absent(owner.RiderCommands, owner.Shell),
                ["processObserved"] = owner.Process != null, ["nativeActionInProgress"] = owner.NativeActionInProgress,
                ["nativeActionFailed"] = owner.NativeActionFailed,
                ["processCount"] = owner.Processes.Count,
                ["nativeRuleObserved"] = owner.NativeRule != null,
                ["nativeExecutorObserved"] = owner.NativeExecutor != null,
                ["shellProcessAssigned"] = owner.Shell?.ExecutionProcess != null,
                ["ruleProcessAssigned"] = owner.NativeRule?.ExecutionProcess != null,
                ["processObservationPending"] = owner.ProcessObservationPending,
                ["processObservationError"] = owner.ProcessObservationError,
                ["manualTargetOwned"] = owner.ManualTargetOwned,
                ["manualTargetReleased"] = !owner.ManualTargetOwned,
                ["processEnded"] = ChargeProcessesEnded(owner),
                ["attempt"] = owner.Ownership.LastAttempt?.Describe()
            };
        }

        private static bool IsMountedChargeShell(UnitCommand command)
        {
            var ability = command as UnitUseAbility;
            return ability?.Spell?.Blueprint?.AssetGuid == NativeMountedControlService.MountedChargeAbilityGuid;
        }

        // Called before any native queue mutation. Reentrant Run/AddToQueue for the same exact
        // shell is lawful; an independent request may not steal or overwrite this owner.
        internal bool AdmitChargeCommand(UnitCommands container, UnitCommand command)
        {
            var current = chargeOwner;
            if (current != null &&
                (ReferenceEquals(container, current.RiderCommands) || ReferenceEquals(container, current.MountCommands)))
            {
                var ownedRequest = ReferenceEquals(command, current.Shell) || ReferenceEquals(command, current.Command) ||
                    current.Command?.OwnsChargeCarrier(command) == true;
                // Native Run interrupts the old command before installing its replacement. Resolve
                // our ownership first so a failure cannot strand debt behind a newly admitted action.
                if (current.Ownership.CleanupRequested || !ownedRequest)
                {
                    if (!TryDrainChargeOwnership("pair-command-replacement")) return false;
                    if (ownedRequest) return false; // retired shells/carriers may never resume
                }
            }
            if (!IsMountedChargeShell(command)) return true;
            if (disposed || ChargeAdmissionFenced || !settings.EnableMountedCharge ||
                TurnBased.Controllers.CombatController.IsInTurnBasedCombat() || ChargeNativeWorldLoading) return false;
            var shell = (UnitUseAbility)command;
            if (chargeOwner != null)
                return ReferenceEquals(chargeOwner.Shell, shell) && ReferenceEquals(container, chargeOwner.RiderCommands) &&
                    chargeOwner.Generation == relationship.MountedPairGeneration &&
                    ReferenceEquals(chargeOwner.Rider, relationship.Rider) && ReferenceEquals(chargeOwner.Mount, relationship.Mount) &&
                    (shell.Executor == null || ReferenceEquals(shell.Executor, chargeOwner.Rider)) &&
                    ReferenceEquals(shell.Spell.Caster?.Unit, chargeOwner.Rider) && ReferenceEquals(shell.Target?.Unit, chargeOwner.Target) &&
                    !chargeOwner.Ownership.CleanupRequested;
            var rider = relationship.Rider;
            if (rider == null || relationship.Mount == null || container != rider.Commands || shell.Spell.Caster?.Unit != rider)
                return false;
            // Native admission still evaluates targeting/action availability; this observer only
            // takes custody of the exact shell, without starting it or spending anything.
            CaptureNativeChargeOwner(rider, container, shell);
            return true;
        }

        // Keep the Unity clock at the native admission boundary, outside the independently
        // testable exact-container admission/cleanup decisions.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void CaptureNativeChargeOwner(UnitEntityData rider, UnitCommands container, UnitUseAbility shell)
        {
            chargeOwner = CreateChargeOwner(rider, relationship.Mount, shell.Target?.Unit, container,
                relationship.Mount.Commands, shell, relationship.MountedPairGeneration, Time.frameCount);
        }

        internal Action<bool> BeginChargeNativeAction(UnitUseAbility shell)
        {
            var owner = chargeOwner;
            if (owner == null || !ReferenceEquals(owner.Shell, shell)) return null;
            if (owner.NativeActionInProgress) throw new InvalidOperationException("Reentered exact charge action.");
            Action<bool> complete = returned => CompleteChargeNativeAction(owner, returned);
            owner.NativeActionInProgress = true;
            owner.ProcessObservationPending = true;
            return complete;
        }

        // Runs from the native body's finally, including exceptions before the shell
        // receives ExecutionProcess. Never replace the original native exception.
        private void CompleteChargeNativeAction(ChargeOwner owner, bool returned)
        {
            if (!returned)
            {
                owner.NativeActionFailed = true;
                owner.Ownership.Retire();
            }
            try { ObserveChargeProcesses(owner); }
            catch (Exception error) { RetainChargeProcessObservation(owner, error); }
            finally { owner.NativeActionInProgress = false; }
        }

        internal bool CaptureChargeNativeRule(UnitUseAbility shell, RuleCastSpell rule, AbilityExecutionController executor)
        {
            var owner = chargeOwner;
            if (owner == null || !ReferenceEquals(owner.Shell, shell)) return false;
            if (!owner.NativeActionInProgress || owner.NativeRule != null || rule == null || rule.Context == null || executor == null)
                throw new InvalidOperationException("Charge native rule requires one exact live action scope.");
            owner.NativeRule = rule;
            owner.Context = rule.Context;
            owner.NativeExecutor = executor;
            owner.ProcessObservationPending = true;
            return true;
        }

        internal bool OwnsChargeNativeAction(UnitUseAbility shell) => chargeOwner != null &&
            ReferenceEquals(chargeOwner.Shell, shell) && chargeOwner.NativeActionInProgress;

        internal void ObserveChargeProcess(UnitUseAbility shell)
        {
            var owner = chargeOwner;
            if (owner == null || !ReferenceEquals(owner.Shell, shell)) return;
            try { ObserveChargeProcesses(owner); }
            catch (Exception error) { RetainChargeProcessObservation(owner, error); }
        }

        private static void RetainChargeProcessObservation(ChargeOwner owner, Exception error)
        {
            owner.ProcessObservationPending = true;
            owner.ProcessObservationError = error.GetType().Name + ": " + error.Message;
            owner.Ownership.Retire();
        }

        private static void ObserveChargeProcesses(ChargeOwner owner)
        {
            owner.ProcessObservationPending = true;
            MountedChargeAdmissionFault.FireCleanup("observe-process");
            RetainChargeProcess(owner, owner.Shell?.ExecutionProcess);
            RetainChargeProcess(owner, owner.NativeRule?.ExecutionProcess);
            if (owner.NativeExecutor != null)
                foreach (var process in NativeSaveEffectBoundary.CaptureAbilities(owner.NativeExecutor, owner.Context))
                    RetainChargeProcess(owner, process);
            owner.ProcessObservationError = null;
            owner.ProcessObservationPending = false;
        }

        private static void RetainChargeProcess(ChargeOwner owner, AbilityExecutionProcess process)
        {
            if (process == null) return;
            // Retain even an unexpected replacement before refusing further delivery.
            if (!owner.Processes.Contains(process)) owner.Processes.Add(process);
            owner.ProcessEnded = () => owner.Processes.All(p => p.IsEnded);
            if (owner.Process == null) owner.Process = process;
            if (owner.Context == null) owner.Context = process.Context;
            if (!ReferenceEquals(owner.Process, process) || !ReferenceEquals(owner.Context, process.Context))
                owner.Ownership.Retire();
        }

        private static bool ChargeProcessesEnded(ChargeOwner owner) => !owner.NativeActionInProgress &&
            !owner.ProcessObservationPending && (owner.ProcessEnded == null || owner.ProcessEnded());

        internal bool AllowChargeExecution(UnitCommand command)
        {
            if (!IsMountedChargeShell(command)) return true;
            return !disposed && !ChargeAdmissionFenced && settings.EnableMountedCharge &&
                !TurnBased.Controllers.CombatController.IsInTurnBasedCombat() && !ChargeNativeWorldLoading &&
                chargeOwner != null && ReferenceEquals(chargeOwner.Shell, command) &&
                !chargeOwner.Ownership.CleanupRequested && chargeOwner.Generation == relationship.MountedPairGeneration;
        }

        private bool OwnsChargeDelivery(AbilityExecutionContext context)
        {
            var owner = chargeOwner;
            if (owner != null) ObserveChargeProcess(owner.Shell);
            return owner != null && context != null && ReferenceEquals(owner.Context, context) &&
                !owner.Ownership.CleanupRequested && !ChargeAdmissionFenced && !ChargeNativeWorldLoading &&
                !owner.ProcessObservationPending && !owner.NativeActionFailed &&
                (owner.Shell == null || owner.Shell.Result != UnitCommand.ResultType.Interrupt) &&
                owner.Command == null && owner.Generation == relationship.MountedPairGeneration &&
                ReferenceEquals(owner.Rider, relationship.Rider) && ReferenceEquals(owner.Mount, relationship.Mount);
        }

        private ChargeOwner CreateChargeOwner(UnitEntityData rider, UnitEntityData mount, UnitEntityData target,
            UnitCommands container, UnitCommands mountContainer, UnitUseAbility shell, long generation, int admissionFrame)
        {
            var owner = new ChargeOwner { Rider = rider, Mount = mount, Target = target,
                RiderCommands = container, MountCommands = mountContainer, Shell = shell,
                Generation = generation, AdmissionFrame = admissionFrame };
            owner.Ownership = new MountedChargeOwnership(owner, new[]
            {
                new MountedChargeCompensationStep("retire-command", () => owner.Command?.RequestChargeCleanup()),
                new MountedChargeCompensationStep("release-manual-target", () => ReleaseChargeManualTarget(owner)),
                new MountedChargeCompensationStep("abandon-scheduler", () =>
                {
                    MountedChargeAdmissionFault.FireCleanup("abandon-scheduler");
                    pairedCommandScheduler.AbandonRegistration(owner.Command, "charge ownership barrier");
                }),
                new MountedChargeCompensationStep("interrupt-command", () => InterruptExact(owner.Command)),
                new MountedChargeCompensationStep("dequeue-command", () => RemoveExact(owner.RiderCommands, owner.Command)),
                new MountedChargeCompensationStep("stop-carrier", () => owner.Command?.TryDrainChargeCarrier()),
                new MountedChargeCompensationStep("restore-lease", () => owner.Command?.TryDischargeChargeCleanupDebt()),
                new MountedChargeCompensationStep("interrupt-shell", () => InterruptExact(owner.Shell)),
                new MountedChargeCompensationStep("dequeue-shell", () => RemoveExact(owner.RiderCommands, owner.Shell)),
                new MountedChargeCompensationStep("observe-process", () => ObserveChargeProcesses(owner))
            }, new[]
            {
                new MountedChargePostcondition("command-terminal", () => owner.Command == null || owner.Command.IsFinished),
                new MountedChargePostcondition("manual-target-released", () => !owner.ManualTargetOwned),
                new MountedChargePostcondition("standard-slot-released", () => owner.Command == null || owner.RiderCommands.Standard != owner.Command),
                new MountedChargePostcondition("container-released", () => Absent(owner.RiderCommands, owner.Command)),
                new MountedChargePostcondition("scheduler-registration-absent", () => owner.Command == null || !pairedCommandScheduler.HasRegistration(owner.Command)),
                new MountedChargePostcondition("carrier-terminal-and-removed", () => owner.Command == null || owner.Command.ChargeCarrierDrained),
                new MountedChargePostcondition("lease-restored-or-absent", () => owner.Command == null || owner.Command.ChargeCleanupComplete),
                new MountedChargePostcondition("no-lease-cleanup-debt", () => owner.Command == null || string.IsNullOrEmpty(owner.Command.ChargeCleanupDebt)),
                new MountedChargePostcondition("shell-terminal", () => owner.Shell == null || owner.Shell.IsFinished),
                new MountedChargePostcondition("shell-container-released", () => Absent(owner.RiderCommands, owner.Shell)),
                // No native process cancel API exists. Retired delivery refuses and the process
                // must finish through its ordinary native tick; no manual coroutine advancement.
                new MountedChargePostcondition("execution-process-ended", () => ChargeProcessesEnded(owner))
            });
            return owner;
        }

        private void OwnChargeManualTarget(UnitEntityData rider, UnitEntityData target)
        {
            var owner = chargeOwner;
            if (owner == null || !ReferenceEquals(owner.Rider, rider) || !ReferenceEquals(owner.Target, target) ||
                owner.Ownership.CleanupRequested || owner.ManualTargetOwned)
                throw new InvalidOperationException("Charge manual target requires its exact live owner.");
            owner.ManualTargetState = rider.CombatState;
            owner.ManualTargetOwned = true; // retain custody before the native mutation
            owner.ManualTargetState.ManualTarget = target;
        }

        private static void ReleaseChargeManualTarget(ChargeOwner owner)
        {
            if (!owner.ManualTargetOwned) return;
            MountedChargeAdmissionFault.FireCleanup("release-manual-target");
            // A later independent input is not ours. Never restore a previous attack intent:
            // this one-shot charge must not restart ordinary native AI after cancellation.
            if (ReferenceEquals(owner.ManualTargetState.ManualTarget, owner.Target))
                owner.ManualTargetState.ManualTarget = null;
            if (ReferenceEquals(owner.ManualTargetState.ManualTarget, owner.Target))
                throw new InvalidOperationException("Charge manual target remains owned.");
            owner.ManualTargetOwned = false;
        }

        private static void InterruptExact(UnitCommand command)
        {
            if (command != null && !command.IsFinished)
            {
                MountedChargeAdmissionFault.FireCleanup(command is MountedPairAttackCommand ? "rider-interrupt" : "shell-interrupt");
                command.Interrupt(false);
            }
        }

        private static bool Absent(UnitCommands commands, UnitCommand command) => command == null ||
            commands != null && !commands.Contains(command) && !commands.Queue.Contains(command);

        private static void RemoveExact(UnitCommands commands, UnitCommand command)
        {
            if (command == null || commands == null) return;
            MountedChargeAdmissionFault.FireCleanup(command is MountedPairAttackCommand ? "rider-remove" : "shell-remove");
            // Pinned native InterruptAll(predicate) removes only matching raw/queued entries.
            // Unlike RemoveFinishedAndUpdateQueue it never promotes unrelated queued work.
            commands.InterruptAll(candidate => ReferenceEquals(candidate, command));
        }

        internal bool TryDrainChargeOwnership(string trigger)
        {
            var owner = chargeOwner;
            if (owner == null) return true;
            var complete = owner.Ownership.TryDrain(trigger);
            ChargeCleanupAttemptCount++;
            if (owner.AdmissionCompensation)
            {
                LastChargeCompensation = owner.Ownership.LastAttempt?.Describe();
                LastChargeCompensationComplete = complete;
                LastChargeCompensationCommandResident = !Absent(owner.RiderCommands, owner.Command);
                LastChargeCompensationLeaseRestored = owner.Command == null || owner.Command.ChargeLeaseRestored;
                LastChargeCompensationUnmet = owner.Ownership.LastAttempt == null ? "not-attempted" :
                    string.Join("|", owner.Ownership.LastAttempt.UnmetPostconditions);
                LastChargeCompensationActiveCommandCleared = complete;
            }
            logger.Info("Charge ownership barrier: generation=" + owner.Generation + ";trigger=" + trigger +
                ";committed=" + owner.Committed + ";" + owner.Ownership.LastAttempt?.Describe());
            if (!complete) return false;
            LastDrainedChargeOwnership = DescribeChargeOwner(owner);
            if (ReferenceEquals(activeCommand, owner.Command)) activeCommand = null;
            if (ReferenceEquals(finishedCommandPendingSweep, owner.Command)) finishedCommandPendingSweep = null;
            chargeOwner = null;
            try { ChargeOwnershipDrained?.Invoke(); }
            catch (Exception exception) { logger.Exception("Charge drain observer", exception); }
            return true;
        }

        internal event Action ChargeOwnershipDrained;

        internal bool AcquireChargeSaveFence(object operation)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            if (chargeAdmissionFence != null && !ReferenceEquals(chargeAdmissionFence, operation))
                throw new InvalidOperationException("Overlapping charge serialization fences.");
            chargeAdmissionFence = operation;
            return TryDrainChargeOwnership("save-before-enumeration") && !ChargeNativeBoundaryPending;
        }

        internal bool ContinueChargeSaveFence(object operation) => operation != null &&
            ReferenceEquals(chargeAdmissionFence, operation) && chargeOwner == null;

        internal void ReleaseChargeSaveFence(object operation)
        {
            if (ReferenceEquals(chargeAdmissionFence, operation)) chargeAdmissionFence = null;
        }

        internal void UpdateChargeCleanup()
        {
            if (ChargeSerializationBusy) return;
            var owner = chargeOwner;
            if (owner == null) { ResumeChargeNativeBoundaries(); return; }
            ObserveChargeProcess(owner.Shell);
            if (owner.Ownership.CleanupRequested || !settings.EnableMountedCharge || !settings.EnableUnsafeMovementExperiment ||
                TurnBased.Controllers.CombatController.IsInTurnBasedCombat() ||
                owner.Generation != relationship.MountedPairGeneration || owner.Rider != relationship.Rider || owner.Mount != relationship.Mount ||
                owner.Command != null && owner.Command.IsFinished ||
                owner.Command == null && owner.ProcessEnded != null && owner.ProcessEnded() ||
                owner.Command == null && owner.Shell != null && owner.Shell.Result == UnitCommand.ResultType.Interrupt ||
                owner.Command == null && owner.Process == null && Time.frameCount > owner.AdmissionFrame && Absent(owner.RiderCommands, owner.Shell))
                TryDrainChargeOwnership("bounded-update");
            ResumeChargeNativeBoundaries();
        }
    }
}
