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
    //     removes whatever override that executor's view holds.
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

        // What may be retired when a bound drive is settled. Nothing is touched unless the exact bound agent is still
        // the override of the exact bound view. A command that is still unfinished is ended natively (its own OnEnded
        // removes the override); one that never ran, or was displaced from its slot, raises no end event.
        public static MountedContinuousDriveRetirement PlanRetirement(MountedContinuousDriveSettlement settlement,
            bool boundAgentStillInstalled, bool commandFinished)
        {
            var retires = boundAgentStillInstalled && settlement != MountedContinuousDriveSettlement.None &&
                settlement != MountedContinuousDriveSettlement.Keep;
            var interrupts = retires && !commandFinished && settlement != MountedContinuousDriveSettlement.Ended;
            return new MountedContinuousDriveRetirement(interrupts, interrupts && settlement != MountedContinuousDriveSettlement.Orphaned, retires);
        }
    }

    public struct MountedContinuousDriveRetirement
    {
        public MountedContinuousDriveRetirement(bool interruptCommand, bool raiseEndEvent, bool clearOverride)
        {
            InterruptCommand = interruptCommand;
            RaiseEndEvent = raiseEndEvent;
            ClearOverride = clearOverride;
        }

        public bool InterruptCommand { get; }

        public bool RaiseEndEvent { get; }

        public bool ClearOverride { get; }
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
    // pair's exact units; the tests answer from a model.
    public interface IMountedContinuousDriveHost
    {
        bool PairMounted { get; }

        long PairGeneration { get; }

        int Frame { get; }

        // Native UpdateMovement returns before it reads the driven unit: the party is in combat.
        bool StickInputGated { get; }

        object Mount { get; }

        object MountView { get; }

        object ExecutorOf(object command);

        bool IsFinished(object command);

        bool InMountMoveSlot(object command);

        // Whatever override the view holds, by reference.
        object OverrideOf(object view);

        // A live native continuous agent (not another agent type, not a destroyed component).
        bool IsContinuousAgent(object agent);

        // Native UnitCommand.Interrupt.
        void Interrupt(object command, bool raiseEndEvent);

        // Native UnitEntityView.AgentOverride = null.
        void ClearOverride(object view);

        // The native effect of a new order on the unit it was given to, which the rider no longer receives.
        void ReleaseRiderHold();

        void Report(MountedContinuousDriveSettlement settlement, string reason, bool warning);
    }

    // One pair's stick movement: bound after native Init, settled after native Run and before the mounted invariant
    // is read, released with the pair. It retires only the exact agent its bound command installed.
    public sealed class MountedContinuousDriveOwner
    {
        private readonly IMountedContinuousDriveHost host;
        private readonly MountedContinuousDriveWatch watch = new MountedContinuousDriveWatch();
        private bool orphanReported;

        public MountedContinuousDriveOwner(IMountedContinuousDriveHost host)
        {
            this.host = host ?? throw new System.ArgumentNullException(nameof(host));
        }

        public bool IsBound => watch.IsBound;

        public object Command => watch.Command;

        public object Agent => watch.Agent;

        public long BindCount { get; private set; }

        public long RetiredCount { get; private set; }

        public long OrphanCount { get; private set; }

        public string LastSettlement { get; private set; } = "not-observed";

        // After native Init: a command whose executor is this pair's mount, with the continuous agent Init installed
        // on the mount's view. A new command adopts the component an earlier one left; that record is replaced.
        public void Bind(object command)
        {
            if (command == null || !host.PairMounted) return;
            var mount = host.Mount;
            if (mount == null || !ReferenceEquals(host.ExecutorOf(command), mount) || ReferenceEquals(watch.Command, command)) return;
            var view = host.MountView;
            var agent = view == null ? null : host.OverrideOf(view);
            if (agent == null || !host.IsContinuousAgent(agent)) return;
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

        // After native Run was called with any other command.
        public void ObserveRiderOrder(bool admittedOnRiderContainer, bool moveOrStandardOrder)
        {
            if (MountedContinuousDrivePolicy.RiderOrderReplacesDrive(watch.IsBound, admittedOnRiderContainer, moveOrStandardOrder))
                Retire(MountedContinuousDriveSettlement.Replaced, "rider order");
        }

        public MountedContinuousDriveSettlement Settle(bool admissionAttempted)
        {
            var command = watch.Command;
            if (command == null) return MountedContinuousDriveSettlement.None;
            var settlement = watch.Settle(host.PairGeneration, host.PairMounted, host.IsFinished(command),
                host.InMountMoveSlot(command), admissionAttempted, host.StickInputGated, host.Frame);
            if (settlement != MountedContinuousDriveSettlement.Keep) Retire(settlement, null);
            return settlement;
        }

        // Dismount and disposal: the pair stops owning its stick movement before it stops being a pair.
        public void Release(string reason)
        {
            if (watch.IsBound) Retire(MountedContinuousDriveSettlement.Unowned, reason);
        }

        // The mounted invariant's question: is this override of the mount the bound live drive of this pair?
        public bool OwnsMountOverride(object agent)
        {
            var command = watch.Command;
            return command != null && agent != null && host.IsContinuousAgent(agent) &&
                watch.Owns(agent, host.PairGeneration, host.PairMounted, host.IsFinished(command), host.InMountMoveSlot(command), host.Frame);
        }

        private void Retire(MountedContinuousDriveSettlement settlement, string reason)
        {
            var command = watch.Command;
            var agent = watch.Agent;
            var view = watch.View;
            watch.Clear();
            var installed = view != null && agent != null && ReferenceEquals(host.OverrideOf(view), agent);
            var plan = MountedContinuousDrivePolicy.PlanRetirement(settlement, installed, command == null || host.IsFinished(command));
            LastSettlement = settlement + ";overrideInstalled=" + installed + (reason == null ? string.Empty : ";reason=" + reason);
            if (!plan.ClearOverride) return;
            if (plan.InterruptCommand) host.Interrupt(command, plan.RaiseEndEvent);
            if (ReferenceEquals(host.OverrideOf(view), agent)) host.ClearOverride(view);
            RetiredCount++;
            if (settlement != MountedContinuousDriveSettlement.Orphaned)
            {
                host.Report(settlement, reason, false);
                return;
            }
            // A held stick whose command is refused every frame is reported once, not every frame.
            OrphanCount++;
            if (orphanReported) return;
            orphanReported = true;
            host.Report(settlement, reason, true);
        }
    }
}
