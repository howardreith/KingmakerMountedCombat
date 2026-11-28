namespace KingmakerMountedCombat.Domain
{
    // Known issues sprint 3 continuation (M05): gamepad stick movement of a mounted pair. Installed native contract
    // (Kingmaker 2.1.7b):
    //   - InGameInputLayer.UpdateMovement (06005D0C) is the only creator of UnitMoveContiniously. It reads the driven
    //     unit once (UnitSelectionManager.CurrentSelectUnit) and uses that one unit for every later step: the override
    //     reads, Commands.MoveContiniously, InterruptMove, Init, Commands.Run, Accelerate and Deaccelerate. It returns
    //     before reading the unit while the party is in combat.
    //   - UnitMoveContiniously.Init (060026F0) installs a UnitMovementAgentContinious as its executor's
    //     UnitEntityView.AgentOverride; OnTick ends the command when the stick direction is zero; OnEnded (060026F2)
    //     removes whatever override that executor's CURRENT view holds, not the one Init installed.
    //   - UnitCommand.Interrupt (060027AC) stamps the command's result before it calls OnEnded and does nothing on a
    //     command already stamped, so a failed OnEnded is never repeated.
    //   - UnitCommands.Run (060026B3) refuses a command whose executor is not the container's owner.
    //   - UnitMoveController.Tick moves a unit through UnitEntityView.MovementAgent: the override when one is installed.
    //   - In turn-based combat a command is ticked only for the unit whose turn it is, so a continuous command the
    //     stick no longer feeds would stay in its slot.
    //   - Native code takes a non-null override on the current unit to mean "this unit is being stick-driven"
    //     (UpdateMovement, ClickGroundHandler.MoveSelectedUnitsToPoint 060093DB). A mounted rider always carries the
    //     pair's own rider agent as its override, so that reading is always true for it.
    // The pair's physical mover is its mount, so the stick drives the mount: one unit for initialisation, execution,
    // lookup and cleanup. The mount's continuous agent is admitted only as the override of the exact bound command.
    public static class MountedContinuousDrivePolicy
    {
        // The unit native stick movement drives in place of the unit the player selected.
        public static bool DrivesPhysicalMover(bool pairMounted, bool unitIsRider, bool mountAvailable) =>
            pairMounted && unitIsRider && mountAvailable;

        // The companions' formation stays anchored on the unit the player selected (native MakeCompanionsFollow reads
        // that unit's party index; a pet mount has none).
        public static bool FormationAnchorIsRider(bool pairMounted, bool unitIsMount, bool riderIsCurrentSelection) =>
            pairMounted && unitIsMount && riderIsCurrentSelection;

        // The mounted invariant. Any override on the mount other than the owned live drive is still a fault.
        public static bool MountOverrideAdmissible(bool overridePresent, bool ownedLiveDrive) =>
            !overridePresent || ownedLiveDrive;

        // Native Run ends a unit's own stick movement when that unit is given a new Move or Standard order. The pair's
        // stick movement runs on the mount, so an order admitted on the rider's own container ends it here.
        public static bool RiderOrderReplacesDrive(bool driveBound, bool admittedOnRiderContainer, bool moveOrStandardOrder) =>
            driveBound && admittedOnRiderContainer && moveOrStandardOrder;

        // The two separate parts of retiring a bound drive. The command: an unfinished one is ended natively whether
        // or not its agent is still installed; one that never ran, or was displaced from its slot, raises no end
        // event. The captured agent: cleared only while the exact bound view still holds that exact agent.
        public static MountedContinuousDriveRetirement PlanRetirement(MountedContinuousDriveSettlement settlement,
            bool commandFinished, bool capturedAgentInstalledOnCapturedView)
        {
            var retires = settlement != MountedContinuousDriveSettlement.None && settlement != MountedContinuousDriveSettlement.Keep;
            var interrupts = retires && !commandFinished && settlement != MountedContinuousDriveSettlement.Ended;
            return new MountedContinuousDriveRetirement(interrupts, interrupts && settlement != MountedContinuousDriveSettlement.Orphaned,
                retires && capturedAgentInstalledOnCapturedView);
        }

        // Native UnitMoveContiniously.OnEnded clears the override of the executor's current view. For a command a pair
        // bound, that is allowed only when the view is the bound view and its override is the bound agent.
        public static bool NativeEndMayClearOverride(bool commandBoundByPair, bool viewIsBoundView, bool overrideIsBoundAgent) =>
            !commandBoundByPair || (viewIsBoundView && overrideIsBoundAgent);

        // When a retirement may be forgotten: the command can no longer be ended natively (it is terminal, or its
        // executor no longer holds it anywhere), and the captured view no longer holds the captured agent. A terminal
        // command alone is not enough.
        public static bool RetirementEstablished(bool commandFinished, bool commandHeldByItsExecutor, bool capturedAgentInstalledOnCapturedView) =>
            (commandFinished || !commandHeldByItsExecutor) && !capturedAgentInstalledOnCapturedView;
    }

    public struct MountedContinuousDriveRetirement
    {
        public MountedContinuousDriveRetirement(bool interruptCommand, bool raiseEndEvent, bool clearCapturedAgent)
        {
            InterruptCommand = interruptCommand;
            RaiseEndEvent = raiseEndEvent;
            ClearCapturedAgent = clearCapturedAgent;
        }

        public bool InterruptCommand { get; }

        public bool RaiseEndEvent { get; }

        public bool ClearCapturedAgent { get; }
    }

    public enum MountedContinuousDriveSettlement
    {
        None,
        // The bound command runs in the mount's Move slot, or its own Run has not been decided yet this frame.
        Keep,
        // The command ended; native OnEnded removed its override.
        Ended,
        // The command is unfinished and not in the mount's Move slot: never admitted, or displaced from the slot.
        Orphaned,
        // The pair is no longer the pair that bound it (dismounted, another pair generation, disposal).
        Unowned,
        // The command runs, but native stick input can no longer reach it (the party is in combat).
        InputGated,
        // A new order for the rider replaced the pair's stick movement.
        Replaced
    }

    // One pair's ownership record of native stick movement: the exact command, the exact agent instance its Init
    // installed, the exact view it was installed on, and the pair generation. Nothing else is ever owned.
    public sealed class MountedContinuousDriveWatch
    {
        private long generation;
        private int bindFrame;
        private bool admissionDecided;

        public object Command { get; private set; }

        public object Agent { get; private set; }

        public object View { get; private set; }

        // The pair generation the command was bound in.
        public long Generation => generation;

        public bool IsBound => Command != null;

        public void Bind(object command, object agent, object view, long pairGeneration, int frame)
        {
            Command = command;
            Agent = command == null ? null : agent;
            View = command == null ? null : view;
            generation = pairGeneration;
            bindFrame = frame;
            admissionDecided = false;
        }

        public void Clear()
        {
            Command = null;
            Agent = null;
            View = null;
            admissionDecided = false;
        }

        // Whether this exact override is the bound live drive. Native Init and Run are two consecutive calls, so an
        // override whose Run has not been observed is owned only within the frame that bound it.
        public bool Owns(object agent, long pairGeneration, bool pairMounted, bool commandFinished, bool commandInMountMoveSlot, int frame)
        {
            if (Command == null || agent == null || !ReferenceEquals(agent, Agent)) return false;
            if (!pairMounted || pairGeneration != generation || commandFinished) return false;
            return commandInMountMoveSlot || AwaitingAdmission(frame);
        }

        public MountedContinuousDriveSettlement Settle(long pairGeneration, bool pairMounted, bool commandFinished,
            bool commandInMountMoveSlot, bool admissionAttempted, bool stickInputGated, int frame)
        {
            if (Command == null) return MountedContinuousDriveSettlement.None;
            if (admissionAttempted) admissionDecided = true;
            if (commandFinished) return MountedContinuousDriveSettlement.Ended;
            if (!pairMounted || pairGeneration != generation) return MountedContinuousDriveSettlement.Unowned;
            if (commandInMountMoveSlot)
            {
                admissionDecided = true;
                return stickInputGated ? MountedContinuousDriveSettlement.InputGated : MountedContinuousDriveSettlement.Keep;
            }
            return AwaitingAdmission(frame) ? MountedContinuousDriveSettlement.Keep : MountedContinuousDriveSettlement.Orphaned;
        }

        private bool AwaitingAdmission(int frame) => !admissionDecided && frame == bindFrame;
    }

    // The live facts and the few native effects one pair's stick movement needs. The game adapter answers from the
    // pair's exact units; the tests answer from a model. A unit and its current view are separate identities: a view
    // captured when a command was bound can be retained after the unit received another view.
    public interface IMountedContinuousDriveHost
    {
        bool PairMounted { get; }

        long PairGeneration { get; }

        int Frame { get; }

        // Native UpdateMovement returns before it reads the driven unit: the party is in combat.
        bool StickInputGated { get; }

        object Mount { get; }

        // The unit's current view.
        object ViewOf(object unit);

        object ExecutorOf(object command);

        bool IsFinished(object command);

        bool InMountMoveSlot(object command);

        // Whether the command's executor still holds it in a slot or in its queue. A command nobody holds is never
        // ticked or ended by native code again.
        bool IsHeld(object command);

        // Whatever override that exact view holds, by reference.
        object OverrideOf(object view);

        // A live native continuous agent (not another agent type, not a destroyed component).
        bool IsContinuousAgent(object agent);

        // Native UnitCommand.Interrupt.
        void Interrupt(object command, bool raiseEndEvent);

        // Native UnitEntityView.AgentOverride = null, on that exact view.
        void ClearOverride(object view);

        // The native effect of a new order on the unit it was given to, which the rider no longer receives.
        void ReleaseRiderHold();

        // The retirement of this command is established: the owner no longer tracks it.
        void Forget(object command);

        void Report(MountedContinuousDriveSettlement settlement, string reason, bool warning);
    }

    // One pair's stick movement: bound after native Init, settled after native Run and before the mounted invariant
    // is read, released with the pair.
    //
    // Retirement has two separate parts, and neither is assumed from the other:
    //   - the command: native Interrupt, at most once per attempt. Native Interrupt stamps the command terminal before
    //     its OnEnded callback runs and never repeats that callback, so a terminal command proves nothing about its
    //     override.
    //   - the captured agent: cleared only on the exact view it was bound on, and only while that view still holds it.
    // Native UnitMoveContiniously.OnEnded clears the override of the executor's CURRENT view, whatever it is. For a
    // command of this owner that clearing is allowed only when that view and override are the bound ones
    // (NativeEndMayClearOverride, asked from inside that native method), so a foreign override on a replaced view, or
    // one that replaced the bound agent, is never cleared by retiring or ending the owned command.
    // A retirement that is not established stays as residue, with its exact command, view and agent, and is retried:
    // automatically up to AutomaticRetryLimit attempts, and deliberately by a release the lifecycle makes on purpose.
    public sealed class MountedContinuousDriveOwner
    {
        // Automatic attempts at one failing retirement: its first attempt, each settlement, the recovery that runs
        // while ordinary updates are stopped, and a new binding all stay within this limit. Only a deliberate release
        // retries beyond it.
        public const int AutomaticRetryLimit = 3;

        private sealed class Residue
        {
            public object Command, Agent, View;
            // The pair generation the command was bound in (review of 119088ed).
            public long Generation;
            public MountedContinuousDriveSettlement Settlement;
            public string Reason;
            public bool EffectApplied, FailureReported, InProgress;
            public int Failures;
        }

        private readonly IMountedContinuousDriveHost host;
        private readonly MountedContinuousDriveWatch watch = new MountedContinuousDriveWatch();
        private readonly System.Collections.Generic.List<Residue> residues = new System.Collections.Generic.List<Residue>();
        private bool orphanReported;

        public MountedContinuousDriveOwner(IMountedContinuousDriveHost host)
        {
            this.host = host ?? throw new System.ArgumentNullException(nameof(host));
        }

        public bool IsBound => watch.IsBound;

        // PR #70 R12-F4 (round 16): whether this exact command is the bound stick movement or one whose retirement is retained.
        public bool OwnsCommand(object command)
        {
            if (command == null) return false;
            if (ReferenceEquals(watch.Command, command)) return true;
            foreach (var residue in residues) if (ReferenceEquals(residue.Command, command)) return true;
            return false;
        }

        public object Command => watch.Command;

        public object Agent => watch.Agent;

        public object View => watch.View;

        public long BindCount { get; private set; }

        // Retirements in which a native effect was applied (a command interrupted, a captured agent cleared).
        public long RetiredCount { get; private set; }

        public long OrphanCount { get; private set; }

        // Retirements not yet established: retained, never discarded.
        public int ResidueCount => residues.Count;

        public long RetirementFailureCount { get; private set; }

        public string LastRetirementFailure { get; private set; }

        public string LastSettlement { get; private set; } = "not-observed";

        public long ReportFailureCount { get; private set; }

        // After native Init: a command whose executor is this pair's mount, with the continuous agent Init installed
        // on the mount's current view.
        public void Bind(object command)
        {
            if (command == null || !host.PairMounted) return;
            var mount = host.Mount;
            if (mount == null || !ReferenceEquals(host.ExecutorOf(command), mount) || ReferenceEquals(watch.Command, command)) return;
            var view = host.ViewOf(mount);
            var agent = view == null ? null : host.OverrideOf(view);
            if (agent == null || !host.IsContinuousAgent(agent)) return;
            // Native Init adopts the component an earlier command left on this view. That agent is this command's now:
            // an earlier record no longer clears it, and an earlier command's native end may no longer clear it.
            foreach (var residue in residues)
                if (ReferenceEquals(residue.View, view) && ReferenceEquals(residue.Agent, agent)) residue.Agent = null;
            // A new binding recovers earlier residue within the automatic limit.
            RecoverResidue(explicitRecovery: false);
            // A record that is still bound to an earlier command is retired, never overwritten.
            if (watch.IsBound)
                Retire(host.IsFinished(watch.Command) ? MountedContinuousDriveSettlement.Ended : MountedContinuousDriveSettlement.Replaced,
                    "superseded by a new command", ReferenceEquals(watch.View, view) && ReferenceEquals(watch.Agent, agent));
            watch.Bind(command, agent, view, host.PairGeneration, host.Frame);
            BindCount++;
        }

        // After native Run was called with the bound command, on whichever container.
        public void ObserveRun(object command)
        {
            if (command == null || !ReferenceEquals(watch.Command, command)) return;
            if (Settle(true) != MountedContinuousDriveSettlement.Keep || !host.InMountMoveSlot(command)) return;
            orphanReported = false;
            host.ReleaseRiderHold();
        }

        // After native Run admitted (or did not admit) any other command.
        public void ObserveRiderOrder(bool admittedOnRiderContainer, bool moveOrStandardOrder)
        {
            if (MountedContinuousDrivePolicy.RiderOrderReplacesDrive(watch.IsBound, admittedOnRiderContainer, moveOrStandardOrder))
                Retire(MountedContinuousDriveSettlement.Replaced, "rider order");
        }

        public MountedContinuousDriveSettlement Settle(bool admissionAttempted)
        {
            RecoverResidue(false);
            var command = watch.Command;
            if (command == null) return MountedContinuousDriveSettlement.None;
            var settlement = watch.Settle(host.PairGeneration, host.PairMounted, host.IsFinished(command),
                host.InMountMoveSlot(command), admissionAttempted, host.StickInputGated, host.Frame);
            if (settlement != MountedContinuousDriveSettlement.Keep) Retire(settlement, null);
            return settlement;
        }

        // Dismount and disposal: recovery of anything an earlier retirement left, then the pair stops owning its stick
        // movement before it stops being a pair. A deliberate release retries retained cleanup whatever its earlier
        // failures; one made from a path that can run every frame stays within the automatic limit.
        public void Release(string reason, bool explicitRecovery = true)
        {
            RecoverResidue(explicitRecovery);
            if (watch.IsBound) Retire(MountedContinuousDriveSettlement.Unowned, reason);
        }

        // Retained cleanup alone, automatically and within the limit. The lifecycle calls this when the pair's ordinary
        // update, which settles the drive, is not running.
        public void RecoverRetained() => RecoverResidue(false);

        // A deliberate retry of retained cleanup alone, for a caller that cannot go on to release (a readiness refusal).
        public void RecoverRetainedDeliberately() => RecoverResidue(true);

        // The mounted invariant's question: is this override of the mount the bound live drive of this pair? Residue
        // is never owned.
        public bool OwnsMountOverride(object agent)
        {
            var command = watch.Command;
            return command != null && agent != null && host.IsContinuousAgent(agent) &&
                watch.Owns(agent, host.PairGeneration, host.PairMounted, host.IsFinished(command), host.InMountMoveSlot(command), host.Frame);
        }

        // Known issues sprint 3 (review of 119088ed). A different question from OwnsMountOverride: given a view and the
        // override it currently carries (the caller reads both together), are they the captured view and agent of a
        // retirement this owner still owes, for a command bound in this pair generation? Only the captured view and agent
        // of an outstanding record answer yes; an adopted agent, a foreign override on the captured view, or any override
        // on another view does not.
        public bool RetainsCapturedAgent(object view, object agent, long pairGeneration)
        {
            if (view == null || agent == null) return false;
            foreach (var residue in residues)
                if (residue.Agent != null && ReferenceEquals(residue.View, view) && ReferenceEquals(residue.Agent, agent) &&
                    residue.Generation == pairGeneration) return true;
            return false;
        }

        // Asked from inside native UnitMoveContiniously.OnEnded, which is about to clear the override of the
        // executor's current view. A command this owner never bound, or whose retirement is established, keeps the
        // native rule.
        public bool NativeEndMayClearOverride(object command, object view, object currentOverride)
        {
            if (command == null) return true;
            if (ReferenceEquals(watch.Command, command))
                return MountedContinuousDrivePolicy.NativeEndMayClearOverride(true, ReferenceEquals(view, watch.View), ReferenceEquals(currentOverride, watch.Agent));
            foreach (var residue in residues)
                if (ReferenceEquals(residue.Command, command))
                    return MountedContinuousDrivePolicy.NativeEndMayClearOverride(true, ReferenceEquals(view, residue.View),
                        residue.Agent != null && ReferenceEquals(currentOverride, residue.Agent));
            return true;
        }

        // The record is transferred, never dropped: it becomes residue and stays until its retirement is established.
        // An agent a new command has adopted is no longer this record's to clear.
        private void Retire(MountedContinuousDriveSettlement settlement, string reason, bool agentAdoptedByNewCommand = false)
        {
            var residue = new Residue { Command = watch.Command, Agent = agentAdoptedByNewCommand ? null : watch.Agent, View = watch.View,
                Generation = watch.Generation, Settlement = settlement, Reason = reason };
            watch.Clear();
            residues.Add(residue);
            var established = Complete(residue);
            if (established) Drop(residue);
            LastSettlement = settlement + ";established=" + established + ";effect=" + residue.EffectApplied +
                (reason == null ? string.Empty : ";reason=" + reason);
            if (settlement == MountedContinuousDriveSettlement.Orphaned && residue.EffectApplied) OrphanCount++;
            // An attempt in which a step failed was reported by that attempt.
            if (!residue.EffectApplied || !established) return;
            if (settlement != MountedContinuousDriveSettlement.Orphaned)
            {
                Report(settlement, reason, false);
                return;
            }
            // A held stick whose command is refused every frame is reported once, not every frame.
            if (orphanReported) return;
            orphanReported = true;
            Report(settlement, reason, true);
        }

        // A native end raises its end event inside the attempt; a handler of that event may come back here. A record
        // whose attempt is running is left to that attempt.
        private void RecoverResidue(bool explicitRecovery)
        {
            if (residues.Count == 0) return;
            foreach (var residue in residues.ToArray())
            {
                if (residue.InProgress) continue;
                if (!explicitRecovery && residue.Failures >= AutomaticRetryLimit)
                {
                    // No further automatic attempt. The record still leaves once the facts establish its retirement
                    // without one: native ended the command itself, and the captured view lost the agent.
                    if (EstablishedWithoutAnAttempt(residue)) Drop(residue);
                    continue;
                }
                if (Complete(residue)) Drop(residue);
            }
        }

        private void Drop(Residue residue)
        {
            residues.Remove(residue);
            try { host.Forget(residue.Command); }
            catch (System.Exception) { ReportFailureCount++; }
        }

        private bool EstablishedWithoutAnAttempt(Residue residue)
        {
            try
            {
                return MountedContinuousDrivePolicy.RetirementEstablished(host.IsFinished(residue.Command), host.IsHeld(residue.Command), CapturedAgentInstalled(residue));
            }
            catch (System.Exception) { return false; }
        }

        private bool CapturedAgentInstalled(Residue residue) =>
            residue.View != null && residue.Agent != null && ReferenceEquals(host.OverrideOf(residue.View), residue.Agent);

        // One attempt. The two native effects are attempted independently, each failure is contained, and whether the
        // retirement is established is then read from the facts, never assumed from either effect having been called.
        private bool Complete(Residue residue)
        {
            residue.InProgress = true;
            try { return Attempt(residue); }
            finally { residue.InProgress = false; }
        }

        private bool Attempt(Residue residue)
        {
            System.Exception failure = null;
            try
            {
                var plan = MountedContinuousDrivePolicy.PlanRetirement(residue.Settlement, host.IsFinished(residue.Command), CapturedAgentInstalled(residue));
                if (plan.InterruptCommand)
                {
                    MarkEffect(residue);
                    host.Interrupt(residue.Command, plan.RaiseEndEvent);
                }
            }
            catch (System.Exception error) { failure = error; }
            try
            {
                // The command's native end may have removed the agent, or not (a replaced view, a failed callback, a
                // command that was already terminal): the captured view is read again and cleaned on its own.
                if (MountedContinuousDrivePolicy.PlanRetirement(residue.Settlement, host.IsFinished(residue.Command), CapturedAgentInstalled(residue)).ClearCapturedAgent)
                {
                    MarkEffect(residue);
                    host.ClearOverride(residue.View);
                }
            }
            catch (System.Exception error) { failure = failure ?? error; }
            var established = false;
            try
            {
                established = MountedContinuousDrivePolicy.RetirementEstablished(host.IsFinished(residue.Command), host.IsHeld(residue.Command), CapturedAgentInstalled(residue));
            }
            catch (System.Exception error) { failure = failure ?? error; }
            if (failure == null) return established;
            residue.Failures++;
            RetirementFailureCount++;
            LastRetirementFailure = failure.GetType().Name + ": " + failure.Message;
            if (!residue.FailureReported)
            {
                residue.FailureReported = true;
                Report(residue.Settlement, "a retirement step failed: " + LastRetirementFailure, true);
            }
            return established;
        }

        private void MarkEffect(Residue residue)
        {
            if (residue.EffectApplied) return;
            residue.EffectApplied = true;
            RetiredCount++;
        }

        // A report that fails must not undo a retirement or its retention.
        private void Report(MountedContinuousDriveSettlement settlement, string reason, bool warning)
        {
            try { host.Report(settlement, reason, warning); }
            catch (System.Exception) { ReportFailureCount++; }
        }
    }
}
