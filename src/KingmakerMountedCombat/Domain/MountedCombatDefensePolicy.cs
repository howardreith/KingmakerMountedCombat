namespace KingmakerMountedCombat.Domain
{
    // FEAT-01: the Mounted Combat feat's reaction. Once per round when the mount is hit, the rider attempts one Mobility
    // check as an immediate action and negates the hit when the check exceeds the attack result. The once-per-round use
    // and the Swift opportunity are separate constraints.
    internal enum MountedCombatDefenseRefusal
    {
        None,
        NotAHit,
        NoNumericAttackResult,
        NoFeat,
        PreferenceOff,
        RiderCannotAct,
        RiderFlatFooted,
        UsageSpent,
        SwiftOwed,
        SwiftUnavailable
    }

    internal enum MountedCombatDefenseCommitment
    {
        None,
        // Turn-based, on the rider's own turn: an immediate action is that turn's Swift action.
        SpendCurrentSwift,
        // Turn-based, off the rider's turn: it borrows the next turn's Swift action.
        BorrowNextSwift,
        // Real time has no turns: the native six-second Swift cooldown is the opportunity, and the use lasts as long.
        RealTimeWindow
    }

    internal struct MountedCombatDefenseFacts
    {
        internal bool WouldHit, HasNumericAttackResult, OwnsFeat, PreferenceOn, RiderCanAct, RiderFlatFooted;
        internal bool TurnBased, RiderTurn, SwiftAvailable;
        internal MountedCombatDefenseState State;
    }

    // The rider's reaction state, independent of any mount: a remount, a new mount or a target change cannot touch it.
    internal struct MountedCombatDefenseState
    {
        // Used since the rider's last turn-based preparation.
        internal bool Spent;
        // A Swift action borrowed off-turn and not yet paid.
        internal bool Owed;
        // A running real-time use. Its end is the native duration, not a value kept here.
        internal bool Window;
        internal bool UsageSpent => Spent || Window;
        internal bool Any => Spent || Owed || Window;
    }

    internal struct MountedCombatDefenseTransition
    {
        internal MountedCombatDefenseState State;
        // Charge one Swift action through the native writer now.
        internal bool ChargeSwift;
    }

    internal static class MountedCombatDefensePolicy
    {
        internal const int MinimumTrainedRanks = 1;

        internal static MountedCombatDefenseRefusal Evaluate(MountedCombatDefenseFacts facts, out MountedCombatDefenseCommitment commitment)
        {
            commitment = MountedCombatDefenseCommitment.None;
            if (!facts.WouldHit) return MountedCombatDefenseRefusal.NotAHit;
            if (!facts.HasNumericAttackResult) return MountedCombatDefenseRefusal.NoNumericAttackResult;
            if (!facts.OwnsFeat) return MountedCombatDefenseRefusal.NoFeat;
            if (!facts.PreferenceOn) return MountedCombatDefenseRefusal.PreferenceOff;
            if (!facts.RiderCanAct) return MountedCombatDefenseRefusal.RiderCannotAct;
            if (facts.RiderFlatFooted) return MountedCombatDefenseRefusal.RiderFlatFooted;
            if (facts.State.UsageSpent) return MountedCombatDefenseRefusal.UsageSpent;
            if (facts.State.Owed) return MountedCombatDefenseRefusal.SwiftOwed;
            if (facts.TurnBased && !facts.RiderTurn)
            {
                // A Swift action spent on the rider's last turn is history, not debt against the next one.
                commitment = MountedCombatDefenseCommitment.BorrowNextSwift;
                return MountedCombatDefenseRefusal.None;
            }
            if (!facts.SwiftAvailable) return MountedCombatDefenseRefusal.SwiftUnavailable;
            commitment = facts.TurnBased ? MountedCombatDefenseCommitment.SpendCurrentSwift : MountedCombatDefenseCommitment.RealTimeWindow;
            return MountedCombatDefenseRefusal.None;
        }

        // Strict: equality fails.
        internal static bool Negates(int checkResult, int attackResult) => checkResult > attackResult;

        internal static MountedCombatDefenseTransition Commit(MountedCombatDefenseState state, MountedCombatDefenseCommitment commitment)
        {
            switch (commitment)
            {
                case MountedCombatDefenseCommitment.SpendCurrentSwift: state.Spent = true; return new MountedCombatDefenseTransition { State = state, ChargeSwift = true };
                case MountedCombatDefenseCommitment.BorrowNextSwift: state.Spent = true; state.Owed = true; return new MountedCombatDefenseTransition { State = state };
                case MountedCombatDefenseCommitment.RealTimeWindow: state.Window = true; return new MountedCombatDefenseTransition { State = state, ChargeSwift = true };
                default: return new MountedCombatDefenseTransition { State = state };
            }
        }

        // The rider's native turn preparation is the turn-based renewal: it just cleared the Swift cooldown, so the
        // borrowed action is charged here against the turn it was borrowed from. A real-time use still running when the
        // mode changed spent the same Swift opportunity, so it is carried the same way.
        internal static MountedCombatDefenseTransition RiderTurnPrepared(MountedCombatDefenseState state) =>
            new MountedCombatDefenseTransition { State = default(MountedCombatDefenseState), ChargeSwift = state.Owed || state.Window };

        // Turn-based state observed in real time: the mode changed, or a save made in the other mode was loaded. Nothing
        // is refunded: the debt is charged and the use runs a whole real-time window.
        internal static MountedCombatDefenseTransition RealTimeObserved(MountedCombatDefenseState state)
        {
            if (!state.Spent && !state.Owed) return new MountedCombatDefenseTransition { State = state };
            return new MountedCombatDefenseTransition { State = new MountedCombatDefenseState { Window = true }, ChargeSwift = state.Owed };
        }
    }
}
