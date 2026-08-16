using System;
using System.Collections.Generic;

namespace KingmakerMountedCombat.Domain
{
    public enum DiagnosticCombatTargetState
    {
        Absent,
        Created,
        Active,
        DestroyRequested,
        Removed,
        Faulted
    }

    public sealed class DiagnosticCombatTargetLifecycle
    {
        public DiagnosticCombatTargetState State { get; private set; }

        public string TargetId { get; private set; }

        public bool BeginCreate(string targetId, bool exactWorkingAuthorized)
        {
            if (!exactWorkingAuthorized ||
                string.IsNullOrWhiteSpace(targetId) ||
                (State != DiagnosticCombatTargetState.Absent && State != DiagnosticCombatTargetState.Removed))
            {
                return false;
            }

            TargetId = targetId;
            State = DiagnosticCombatTargetState.Created;
            return true;
        }

        public bool Activate(string exactTargetId, bool allSafetyGatesPassed)
        {
            if (State != DiagnosticCombatTargetState.Created ||
                !allSafetyGatesPassed ||
                !string.Equals(TargetId, exactTargetId, StringComparison.Ordinal))
            {
                return false;
            }

            State = DiagnosticCombatTargetState.Active;
            return true;
        }

        public bool RequestDestroy(string reason)
        {
            if (State == DiagnosticCombatTargetState.Absent ||
                State == DiagnosticCombatTargetState.Removed ||
                State == DiagnosticCombatTargetState.DestroyRequested)
            {
                return false;
            }

            State = DiagnosticCombatTargetState.DestroyRequested;
            return true;
        }

        public bool ConfirmRemoved(string exactTargetId, bool zeroResidue)
        {
            if (State != DiagnosticCombatTargetState.DestroyRequested ||
                !zeroResidue ||
                !string.Equals(TargetId, exactTargetId, StringComparison.Ordinal))
            {
                return false;
            }

            State = DiagnosticCombatTargetState.Removed;
            return true;
        }

        public void Fault()
        {
            State = DiagnosticCombatTargetState.Faulted;
        }
    }

    public sealed class DiagnosticCombatTargetSafetySnapshot
    {
        private readonly string[] failedGateNames;

        public DiagnosticCombatTargetSafetySnapshot(
            bool exactBlueprint,
            bool notDirectlyControllable,
            bool notPet,
            bool noMaster,
            bool exactRuntimeFaction,
            bool dedicatedRuntimeGroup,
            bool targetTreatsRiderAsEnemy,
            bool riderTreatsTargetAsEnemy,
            bool noExperienceOnDeath,
            bool aiBackingDisabled,
            bool boundedSleeplessLease,
            bool commandsEmpty,
            bool inventoryHasNoLoot,
            bool nativePrimaryNaturalWeaponResolvedWithoutProvisioning,
            bool primaryNaturalWeaponPresent,
            bool primaryNaturalWeaponIsNatural,
            bool primaryNaturalWeaponIsMelee)
        {
            var failures = new List<string>();
            AddFailure(failures, exactBlueprint, "exact-blueprint");
            AddFailure(failures, notDirectlyControllable, "not-directly-controllable");
            AddFailure(failures, notPet, "not-pet");
            AddFailure(failures, noMaster, "no-master");
            AddFailure(failures, exactRuntimeFaction, "exact-runtime-faction");
            AddFailure(failures, dedicatedRuntimeGroup, "dedicated-runtime-group");
            AddFailure(failures, targetTreatsRiderAsEnemy, "target-treats-rider-as-enemy");
            AddFailure(failures, riderTreatsTargetAsEnemy, "rider-treats-target-as-enemy");
            AddFailure(failures, noExperienceOnDeath, "no-experience-on-death");
            AddFailure(failures, aiBackingDisabled, "ai-backing-disabled");
            AddFailure(failures, boundedSleeplessLease, "bounded-sleepless-lease");
            AddFailure(failures, commandsEmpty, "commands-empty");
            AddFailure(failures, inventoryHasNoLoot, "inventory-has-no-loot");
            AddFailure(failures, nativePrimaryNaturalWeaponResolvedWithoutProvisioning, "native-primary-natural-weapon-resolved-without-provisioning");
            AddFailure(failures, primaryNaturalWeaponPresent, "primary-natural-weapon-present");
            AddFailure(failures, primaryNaturalWeaponIsNatural, "primary-natural-weapon-is-natural");
            AddFailure(failures, primaryNaturalWeaponIsMelee, "primary-natural-weapon-is-melee");
            failedGateNames = failures.ToArray();
        }

        public bool AllPassed => failedGateNames.Length == 0;

        public string[] FailedGateNames => (string[])failedGateNames.Clone();

        public string FailureSummary => string.Join(",", failedGateNames);

        private static void AddFailure(List<string> failures, bool passed, string name)
        {
            if (!passed)
            {
                failures.Add(name);
            }
        }
    }

    public sealed class DiagnosticCombatClickSafetySnapshot
    {
        private readonly string[] failedGateNames;

        public DiagnosticCombatClickSafetySnapshot(
            bool exactTarget,
            bool fogOfWarCleared,
            bool targetViewVisible,
            bool targetVisibleForPlayer,
            bool clickObjectResolvesExactView,
            bool riderCanAttackTarget,
            bool riderWeaponIsSupportedMelee)
        {
            var failures = new List<string>();
            AddFailure(failures, exactTarget, "exact-click-target");
            AddFailure(failures, fogOfWarCleared, "fog-of-war-cleared");
            AddFailure(failures, targetViewVisible, "target-view-visible");
            AddFailure(failures, targetVisibleForPlayer, "target-visible-for-player");
            AddFailure(failures, clickObjectResolvesExactView, "click-object-resolves-exact-view");
            AddFailure(failures, riderCanAttackTarget, "rider-can-attack-target");
            AddFailure(failures, riderWeaponIsSupportedMelee, "rider-weapon-is-supported-melee");
            failedGateNames = failures.ToArray();
        }

        public bool AllPassed => failedGateNames.Length == 0;

        public string[] FailedGateNames => (string[])failedGateNames.Clone();

        public string FailureSummary => string.Join(",", failedGateNames);

        private static void AddFailure(List<string> failures, bool passed, string name)
        {
            if (!passed)
            {
                failures.Add(name);
            }
        }
    }

    public sealed class DiagnosticCombatDispatchReadinessSnapshot
    {
        private readonly string[] failedGateNames;

        public DiagnosticCombatDispatchReadinessSnapshot(
            bool gameUnpaused,
            bool riderCanActInCombat,
            bool riderHandsIdle,
            bool equipmentControllerAvailable,
            bool equipmentUpdateIdle)
        {
            GameUnpaused = gameUnpaused;
            RiderCanActInCombat = riderCanActInCombat;
            RiderHandsBusy = !riderHandsIdle;
            EquipmentControllerAvailable = equipmentControllerAvailable;
            EquipmentUpdateScheduled = !equipmentUpdateIdle;
            var failures = new List<string>();
            AddFailure(failures, gameUnpaused, "game-unpaused");
            AddFailure(failures, riderCanActInCombat, "rider-can-act-in-combat");
            AddFailure(failures, riderHandsIdle, "rider-hands-idle");
            AddFailure(failures, equipmentControllerAvailable, "equipment-controller-available");
            AddFailure(failures, equipmentUpdateIdle, "equipment-update-idle");
            failedGateNames = failures.ToArray();
        }

        public bool GameUnpaused { get; }

        public bool RiderCanActInCombat { get; }

        public bool RiderHandsBusy { get; }

        public bool EquipmentControllerAvailable { get; }

        public bool EquipmentUpdateScheduled { get; }

        public bool AllPassed => failedGateNames.Length == 0;

        public string[] FailedGateNames => (string[])failedGateNames.Clone();

        public string FailureSummary => string.Join(",", failedGateNames);

        private static void AddFailure(List<string> failures, bool passed, string name)
        {
            if (!passed)
            {
                failures.Add(name);
            }
        }
    }

    public sealed class DiagnosticCombatEntryReadinessSnapshot
    {
        private readonly string[] failedGateNames;

        public DiagnosticCombatEntryReadinessSnapshot(
            bool memoryQueued,
            bool playerGroupMemoryContainsTarget,
            bool targetGroupMemoryContainsRider,
            bool riderInCombat,
            bool mountInCombat,
            bool targetInCombat,
            bool playerInCombat,
            bool riderPrepared,
            bool riderAwake,
            bool targetAwake,
            bool defaultGameMode,
            float riderInitiative,
            float gameDeltaTime)
        {
            MemoryQueued = memoryQueued;
            PlayerGroupMemoryContainsTarget = playerGroupMemoryContainsTarget;
            TargetGroupMemoryContainsRider = targetGroupMemoryContainsRider;
            RiderInCombat = riderInCombat;
            MountInCombat = mountInCombat;
            TargetInCombat = targetInCombat;
            PlayerInCombat = playerInCombat;
            RiderPrepared = riderPrepared;
            RiderAwake = riderAwake;
            TargetAwake = targetAwake;
            DefaultGameMode = defaultGameMode;
            RiderInitiative = riderInitiative;
            GameDeltaTime = gameDeltaTime;
            var failures = new List<string>();
            AddFailure(failures, memoryQueued, "combat-memory-queued");
            AddFailure(failures, playerGroupMemoryContainsTarget, "player-memory-contains-target");
            AddFailure(failures, targetGroupMemoryContainsRider, "target-memory-contains-rider");
            AddFailure(failures, riderInCombat, "rider-in-combat");
            AddFailure(failures, mountInCombat, "mount-in-combat");
            AddFailure(failures, targetInCombat, "target-in-combat");
            AddFailure(failures, playerInCombat, "player-in-combat");
            AddFailure(failures, riderPrepared, "rider-initiative-prepared");
            AddFailure(failures, riderAwake, "rider-awake");
            AddFailure(failures, targetAwake, "target-awake");
            AddFailure(failures, defaultGameMode, "default-game-mode");
            AddFailure(failures, gameDeltaTime > 0f, "positive-game-delta");
            failedGateNames = failures.ToArray();
        }

        public bool MemoryQueued { get; }

        public bool PlayerGroupMemoryContainsTarget { get; }

        public bool TargetGroupMemoryContainsRider { get; }

        public bool RiderInCombat { get; }

        public bool MountInCombat { get; }

        public bool TargetInCombat { get; }

        public bool PlayerInCombat { get; }

        public bool RiderPrepared { get; }

        public bool RiderAwake { get; }

        public bool TargetAwake { get; }

        public bool DefaultGameMode { get; }

        public float RiderInitiative { get; }

        public float GameDeltaTime { get; }

        public bool AllPassed => failedGateNames.Length == 0;

        public string[] FailedGateNames => (string[])failedGateNames.Clone();

        public string FailureSummary => string.Join(",", failedGateNames);

        private static void AddFailure(List<string> failures, bool passed, string name)
        {
            if (!passed)
            {
                failures.Add(name);
            }
        }
    }

    public sealed class DiagnosticTurnBasedDispatchReadinessSnapshot
    {
        private readonly string[] failedGateNames;

        public DiagnosticTurnBasedDispatchReadinessSnapshot(
            bool modeEnabled,
            bool controllerInitialized,
            bool rosterContainsRider,
            bool rosterContainsMount,
            bool rosterContainsTarget,
            bool nativeRiderTurnStarted,
            bool currentTurnRider,
            bool currentTurnCommandReady)
        {
            ModeEnabled = modeEnabled;
            ControllerInitialized = controllerInitialized;
            RosterContainsRider = rosterContainsRider;
            RosterContainsMount = rosterContainsMount;
            RosterContainsTarget = rosterContainsTarget;
            NativeRiderTurnStarted = nativeRiderTurnStarted;
            CurrentTurnRider = currentTurnRider;
            CurrentTurnCommandReady = currentTurnCommandReady;
            var failures = new List<string>();
            AddFailure(failures, modeEnabled, "turn-based-mode-enabled");
            AddFailure(failures, controllerInitialized, "turn-based-controller-initialized");
            AddFailure(failures, rosterContainsRider, "turn-roster-contains-rider");
            AddFailure(failures, rosterContainsMount, "turn-roster-contains-mount");
            AddFailure(failures, rosterContainsTarget, "turn-roster-contains-target");
            AddFailure(failures, nativeRiderTurnStarted, "native-rider-turn-started");
            AddFailure(failures, currentTurnRider, "current-turn-rider");
            AddFailure(failures, currentTurnCommandReady, "current-turn-command-ready");
            failedGateNames = failures.ToArray();
        }

        public bool ModeEnabled { get; }

        public bool ControllerInitialized { get; }

        public bool RosterContainsRider { get; }

        public bool RosterContainsMount { get; }

        public bool RosterContainsTarget { get; }

        public bool NativeRiderTurnStarted { get; }

        public bool CurrentTurnRider { get; }

        public bool CurrentTurnCommandReady { get; }

        public bool AllPassed => failedGateNames.Length == 0;

        public string[] FailedGateNames => (string[])failedGateNames.Clone();

        public string FailureSummary => string.Join(",", failedGateNames);

        private static void AddFailure(List<string> failures, bool passed, string name)
        {
            if (!passed)
            {
                failures.Add(name);
            }
        }
    }
}
