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
            bool actionActorCanAttackTarget,
            bool actionWeaponIsSupportedMelee)
        {
            var failures = new List<string>();
            AddFailure(failures, exactTarget, "exact-click-target");
            AddFailure(failures, fogOfWarCleared, "fog-of-war-cleared");
            AddFailure(failures, targetViewVisible, "target-view-visible");
            AddFailure(failures, targetVisibleForPlayer, "target-visible-for-player");
            AddFailure(failures, clickObjectResolvesExactView, "click-object-resolves-exact-view");
            AddFailure(failures, actionActorCanAttackTarget, "action-actor-can-attack-target");
            AddFailure(failures, actionWeaponIsSupportedMelee, "action-weapon-is-supported-melee");
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
            bool actionActorCanActInCombat,
            bool actionActorHandsIdle,
            bool equipmentControllerAvailable,
            bool equipmentUpdateIdle)
        {
            GameUnpaused = gameUnpaused;
            ActionActorCanActInCombat = actionActorCanActInCombat;
            ActionActorHandsBusy = !actionActorHandsIdle;
            EquipmentControllerAvailable = equipmentControllerAvailable;
            EquipmentUpdateScheduled = !equipmentUpdateIdle;
            var failures = new List<string>();
            AddFailure(failures, gameUnpaused, "game-unpaused");
            AddFailure(failures, actionActorCanActInCombat, "action-actor-can-act-in-combat");
            AddFailure(failures, actionActorHandsIdle, "action-actor-hands-idle");
            AddFailure(failures, equipmentControllerAvailable, "equipment-controller-available");
            AddFailure(failures, equipmentUpdateIdle, "equipment-update-idle");
            failedGateNames = failures.ToArray();
        }

        public bool GameUnpaused { get; }

        public bool ActionActorCanActInCombat { get; }

        public bool ActionActorHandsBusy { get; }

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

    public sealed class DiagnosticNativeCombatJoinReadinessSnapshot
    {
        private readonly string[] failedGateNames;

        public DiagnosticNativeCombatJoinReadinessSnapshot(
            bool riderInGame,
            bool mountInGame,
            bool targetInGame,
            bool riderConscious,
            bool mountConscious,
            bool targetConscious,
            bool riderIgnoredByCombat,
            bool mountIgnoredByCombat,
            bool targetIgnoredByCombat,
            bool playerGroupContainsRider,
            bool playerGroupContainsMount,
            bool targetGroupContainsTarget,
            bool playerGroupEnemiesContainsTarget,
            bool targetGroupEnemiesContainsRider,
            bool riderNotInFogOfWar,
            bool targetNotInFogOfWar,
            bool riderNotInStealthAmbush,
            bool targetNotInStealthAmbush)
        {
            RiderInGame = riderInGame;
            MountInGame = mountInGame;
            TargetInGame = targetInGame;
            RiderConscious = riderConscious;
            MountConscious = mountConscious;
            TargetConscious = targetConscious;
            RiderIgnoredByCombat = riderIgnoredByCombat;
            MountIgnoredByCombat = mountIgnoredByCombat;
            TargetIgnoredByCombat = targetIgnoredByCombat;
            PlayerGroupContainsRider = playerGroupContainsRider;
            PlayerGroupContainsMount = playerGroupContainsMount;
            TargetGroupContainsTarget = targetGroupContainsTarget;
            PlayerGroupEnemiesContainsTarget = playerGroupEnemiesContainsTarget;
            TargetGroupEnemiesContainsRider = targetGroupEnemiesContainsRider;
            RiderNotInFogOfWar = riderNotInFogOfWar;
            TargetNotInFogOfWar = targetNotInFogOfWar;
            RiderNotInStealthAmbush = riderNotInStealthAmbush;
            TargetNotInStealthAmbush = targetNotInStealthAmbush;

            var failures = new List<string>();
            AddFailure(failures, riderInGame, "rider-in-game");
            AddFailure(failures, mountInGame, "mount-in-game");
            AddFailure(failures, targetInGame, "target-in-game");
            AddFailure(failures, riderConscious, "rider-conscious");
            AddFailure(failures, mountConscious, "mount-conscious");
            AddFailure(failures, targetConscious, "target-conscious");
            AddFailure(failures, !riderIgnoredByCombat, "rider-not-ignored-by-combat");
            AddFailure(failures, !mountIgnoredByCombat, "mount-not-ignored-by-combat");
            AddFailure(failures, !targetIgnoredByCombat, "target-not-ignored-by-combat");
            AddFailure(failures, playerGroupContainsRider, "player-group-contains-rider");
            AddFailure(failures, playerGroupContainsMount, "player-group-contains-mount");
            AddFailure(failures, targetGroupContainsTarget, "target-group-contains-target");
            AddFailure(failures, playerGroupEnemiesContainsTarget, "player-enemies-contain-target");
            AddFailure(failures, targetGroupEnemiesContainsRider, "target-enemies-contain-rider");
            AddFailure(failures, riderNotInFogOfWar, "rider-not-in-fog");
            AddFailure(failures, targetNotInFogOfWar, "target-not-in-fog");
            AddFailure(failures, riderNotInStealthAmbush, "rider-not-in-stealth-ambush");
            AddFailure(failures, targetNotInStealthAmbush, "target-not-in-stealth-ambush");
            failedGateNames = failures.ToArray();
        }

        public bool RiderInGame { get; }
        public bool MountInGame { get; }
        public bool TargetInGame { get; }
        public bool RiderConscious { get; }
        public bool MountConscious { get; }
        public bool TargetConscious { get; }
        public bool RiderIgnoredByCombat { get; }
        public bool MountIgnoredByCombat { get; }
        public bool TargetIgnoredByCombat { get; }
        public bool PlayerGroupContainsRider { get; }
        public bool PlayerGroupContainsMount { get; }
        public bool TargetGroupContainsTarget { get; }
        public bool PlayerGroupEnemiesContainsTarget { get; }
        public bool TargetGroupEnemiesContainsRider { get; }
        public bool RiderNotInFogOfWar { get; }
        public bool TargetNotInFogOfWar { get; }
        public bool RiderNotInStealthAmbush { get; }
        public bool TargetNotInStealthAmbush { get; }
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
            bool nativeActionActorTurnStarted,
            bool currentTurnActionActor,
            bool currentTurnCommandReady)
        {
            ModeEnabled = modeEnabled;
            ControllerInitialized = controllerInitialized;
            RosterContainsRider = rosterContainsRider;
            RosterContainsMount = rosterContainsMount;
            RosterContainsTarget = rosterContainsTarget;
            NativeActionActorTurnStarted = nativeActionActorTurnStarted;
            CurrentTurnActionActor = currentTurnActionActor;
            CurrentTurnCommandReady = currentTurnCommandReady;
            var failures = new List<string>();
            AddFailure(failures, modeEnabled, "turn-based-mode-enabled");
            AddFailure(failures, controllerInitialized, "turn-based-controller-initialized");
            AddFailure(failures, rosterContainsRider, "turn-roster-contains-rider");
            AddFailure(failures, rosterContainsMount, "turn-roster-contains-mount");
            AddFailure(failures, rosterContainsTarget, "turn-roster-contains-target");
            AddFailure(failures, nativeActionActorTurnStarted, "native-action-actor-turn-started");
            AddFailure(failures, currentTurnActionActor, "current-turn-action-actor");
            AddFailure(failures, currentTurnCommandReady, "current-turn-command-ready");
            failedGateNames = failures.ToArray();
        }

        public bool ModeEnabled { get; }

        public bool ControllerInitialized { get; }

        public bool RosterContainsRider { get; }

        public bool RosterContainsMount { get; }

        public bool RosterContainsTarget { get; }

        public bool NativeActionActorTurnStarted { get; }

        public bool CurrentTurnActionActor { get; }

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
