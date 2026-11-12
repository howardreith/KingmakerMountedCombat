using System;

namespace KingmakerMountedCombat.Domain
{
    // Save/load repair (2026-11-12): the full Game.LoadArea (06000CD5) is reached by two different native operations.
    // Exact installed contract: Game.LoadGame (06000CE0) calls it synchronously with the selected SaveInfo after its
    // corruption precheck (a saved-world REPLACEMENT); every door, world-map arrival and scripted area change calls it
    // with a null SaveInfo (area TRAVEL in the same world). The 0.2.4 prefix applied the travel admission to both, so an
    // in-session load during an ordinary paired combat turn was refused as if the party were leaving the area.
    public enum MountedWorldBoundaryKind
    {
        AreaTravel,
        SavedWorldReplacement
    }

    public enum MountedWorldBoundaryRefusal
    {
        None,
        RestoreFailureLatched,
        ParticipationUnsettled,
        SaveScopeActive,
        SaveDraining,
        SaveAdmissionFence,
        ChargeFence,
        ChargeCleanupUnresolved,
        SelectedLoadRefused
    }

    public static class MountedWorldBoundaryAdmissionPolicy
    {
        public static MountedWorldBoundaryKind Classify(bool hasSaveInfo) =>
            hasSaveInfo ? MountedWorldBoundaryKind.SavedWorldReplacement : MountedWorldBoundaryKind.AreaTravel;

        // Travel keeps the 0.2.4 terms in their exact short-circuit order: the participation settlement and the charge
        // drain are evaluated lazily, only after every earlier term admitted, exactly as before. The first refusing
        // term is returned so the refusal can be named without evaluating anything a second time.
        public static MountedWorldBoundaryRefusal DecideTravel(bool restoreFailureLatched, Func<bool> participationUnsettled,
            bool saveScopeActive, bool saveAdmissionFence, bool chargeFence, Func<bool> chargeDrained)
        {
            if (participationUnsettled == null) throw new ArgumentNullException(nameof(participationUnsettled));
            if (chargeDrained == null) throw new ArgumentNullException(nameof(chargeDrained));
            if (restoreFailureLatched) return MountedWorldBoundaryRefusal.RestoreFailureLatched;
            if (participationUnsettled()) return MountedWorldBoundaryRefusal.ParticipationUnsettled;
            if (saveScopeActive) return MountedWorldBoundaryRefusal.SaveScopeActive;
            if (saveAdmissionFence) return MountedWorldBoundaryRefusal.SaveAdmissionFence;
            if (chargeFence) return MountedWorldBoundaryRefusal.ChargeFence;
            if (!chargeDrained()) return MountedWorldBoundaryRefusal.ChargeCleanupUnresolved;
            return MountedWorldBoundaryRefusal.None;
        }

        // A saved-world replacement discards the outgoing world, so ordinary live combat participation (a prepared paired
        // turn, queued actions) and an earlier restore-failure latch never refuse it. The outgoing world is released by the
        // established load path, not here: the native unload's OnAreaBeginUnloading cleanup, then WrapLoadRoutine, which
        // retires the previous load scope before BeginLoadHousekeeping (releasing participation without End or forfeit and
        // clearing the latch only after cleanup). Nothing is released at admission, so a refused load changes nothing.
        // What still refuses: an archive worker that can still commit, an active save, a queued save admission, a charge
        // fence, retained charge cleanup on ANY pair, and the selected archive's own admission (compatibility, availability,
        // metadata). Review of PR #31 (P2): a pair can retain charge ownership without holding a charge fence (its native
        // ability process has not ended), and the area-unloading cleanup cannot veto an unload, so every pair's charge
        // custody is drained here, once the save guards have admitted and before the native unload can begin. Ordinary
        // participation is still never consulted.
        public static MountedWorldBoundaryRefusal DecideSavedReplacement(Func<bool> selectedLoadAdmitted, bool saveScopeActive,
            bool saveDraining, bool saveAdmissionFence, bool chargeFence, Func<bool> worldChargeDrained)
        {
            if (selectedLoadAdmitted == null) throw new ArgumentNullException(nameof(selectedLoadAdmitted));
            if (worldChargeDrained == null) throw new ArgumentNullException(nameof(worldChargeDrained));
            if (saveDraining) return MountedWorldBoundaryRefusal.SaveDraining;
            if (saveScopeActive) return MountedWorldBoundaryRefusal.SaveScopeActive;
            if (saveAdmissionFence) return MountedWorldBoundaryRefusal.SaveAdmissionFence;
            if (chargeFence) return MountedWorldBoundaryRefusal.ChargeFence;
            if (!worldChargeDrained()) return MountedWorldBoundaryRefusal.ChargeCleanupUnresolved;
            if (!selectedLoadAdmitted()) return MountedWorldBoundaryRefusal.SelectedLoadRefused;
            return MountedWorldBoundaryRefusal.None;
        }

        public static string Describe(MountedWorldBoundaryRefusal refusal)
        {
            switch (refusal)
            {
                case MountedWorldBoundaryRefusal.None: return "admitted";
                case MountedWorldBoundaryRefusal.RestoreFailureLatched: return "an earlier mounted restoration failure is latched";
                case MountedWorldBoundaryRefusal.ParticipationUnsettled: return "a mounted pair's participation, command or control shell is unsettled";
                case MountedWorldBoundaryRefusal.SaveScopeActive: return "a save is being written";
                case MountedWorldBoundaryRefusal.SaveDraining: return "an archive worker is still finishing";
                case MountedWorldBoundaryRefusal.SaveAdmissionFence: return "a save request still owns its admission";
                case MountedWorldBoundaryRefusal.ChargeFence: return "a mounted charge still owns its admission";
                case MountedWorldBoundaryRefusal.ChargeCleanupUnresolved: return "mounted charge cleanup is unresolved";
                case MountedWorldBoundaryRefusal.SelectedLoadRefused: return "the selected save was refused";
                default: return refusal.ToString();
            }
        }
    }

    // Entrance repair (2026-11-12). Exact installed contract: AreaTransitionController.StartAreaTransition builds one
    // AreaTransitionGroupCommand over Player.GetPartyCharactersForGroupCommand (directly controllable, able to move, not
    // at death's door, same navmesh area: a mounted horse is a member), calls SelectionManager.SelectAll (whose
    // MultiSelect KMC normalizes so a mounted horse is projected onto its rider) and then gives each SELECTED unit its
    // own UnitAreaTransition through RunUnitTransitionCommand. The horse, a group member, therefore receives no command,
    // the rider's own approach cannot move (its agent is disabled while mounted), and the group (which starts only once
    // every member acted or stands within 6 m of the transition) never starts. The repair issues the horse its own
    // native member command in the SAME group when the rider's command runs, exactly what native would have issued had
    // the horse been selected; the horse carries the rider, and native arrival, cancellation and completion are unchanged.
    public enum MountedEntranceCarryDecision
    {
        NotApplicable,
        Carry,
        MountAlreadyOrdered,
        MountNotGroupMember,
        GroupNotPending,
        RiderCommandNotLive
    }

    public static class MountedEntranceCarryPolicy
    {
        public static MountedEntranceCarryDecision Decide(bool mounted, bool riderContainer, bool exactAreaTransition, bool partyInCombat,
            bool riderCommandLive, bool groupPending, bool mountIsGroupMember, bool mountHasLiveCommandForGroup)
        {
            if (!mounted || !riderContainer || !exactAreaTransition || partyInCombat) return MountedEntranceCarryDecision.NotApplicable;
            // A refused or immediately replaced rider command carries nothing (the Harmony postfix also runs after a refusal).
            if (!riderCommandLive) return MountedEntranceCarryDecision.RiderCommandNotLive;
            if (!groupPending) return MountedEntranceCarryDecision.GroupNotPending;
            // A horse that is not a native member would be counted as an extra arrival: never ordered.
            if (!mountIsGroupMember) return MountedEntranceCarryDecision.MountNotGroupMember;
            // Native already ordered the horse (it was selected), or a repeated run of the same group: nothing duplicated.
            if (mountHasLiveCommandForGroup) return MountedEntranceCarryDecision.MountAlreadyOrdered;
            return MountedEntranceCarryDecision.Carry;
        }
    }
}
