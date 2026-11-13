using System;

namespace KingmakerMountedCombat.Domain
{
    // Cutscene continuity repair (owner order 2026-11-12). Installed Kingmaker 2.1.7b contracts this policy relies on:
    // - Game.SetCutsceneLock(true) starts GameModeType.Cutscene; DialogController owns GameModeType.Dialog. Neither mode
    //   itself moves, hides or replaces a unit: both only take the camera and player input.
    // - A scene stages a unit only through its own operations: cutscene commands run UnitCommands.Run on the unit
    //   (CommandMoveUnit, CommandMoveParty, CommandMoveByPath, animation, attack and cast commands), and every
    //   UnitCommand constructed inside a cutscene's CutsceneParametersContext carries the native Cutscene flag; dialog
    //   and script actions (RunCommand, AttackCommand) run UnitCommands.Run inside Dialog/Cutscene mode; HideUnit-style
    //   actions clear IsInGame. While a command plays, CutscenePlayerData marks its controlled unit
    //   (CutsceneControlledUnit.MarkUnit/ReleaseUnit) and the unit's GetControllingPlayer is the playing cutscene.
    // Ordinary scenes therefore keep the attachment; only a scripted operation on the rider hands the one pair to native
    // staging, and restoration waits until no native scene owns either actor any longer.
    public enum MountedSceneCommandDisposition
    {
        // Not a scripted scene operation on a mounted pair actor: every existing gate decides.
        NotScripted,
        // A scripted command on the mount: the mount carries its seated rider (the native movement supports it).
        KeepAttached,
        // A scripted command on the rider out of combat: the pair is handed to native staging before native Run.
        HandOffRider,
        // A scripted command on the rider during an encounter: staging is unsupported there (restoring would need a
        // native participation decision), so the attachment and every turn resource stay as they are.
        KeepAttachedInCombat
    }

    public enum MountedSceneInGameDisposition
    {
        // The existing invariant validation decides (a removal outside a scene stays an invalidation).
        Validate,
        // A scene took a living, still reciprocal pair member out of the game: temporary staging, intent retained.
        HandOff
    }

    public enum MountedRegroupState
    {
        // Regrouping is not offered (combat, camp or world-map custody, or no navigable separation).
        Unavailable,
        Available,
        InProgress,
        Exhausted
    }

    public enum MountedRegroupStep
    {
        Wait,
        Arrived,
        Abandon
    }

    public static class MountedSceneContinuityPolicy
    {
        // A mount left behind by a scene may walk to its rider along the native navigation graph for this far, once,
        // for this long. A larger or cross-area separation stays dismounted (never a teleport across the scene).
        public const float RegroupMaximumMetres = 30f;
        public const float RegroupDeadlineSeconds = 20f;
        public const float RegroupApproachRadiusMetres = 1.5f;
        public const int RegroupMaximumAttempts = 2;

        public static bool IsSceneMode(string exactModeName)
        {
            return string.Equals(exactModeName, "Dialog", StringComparison.Ordinal) ||
                string.Equals(exactModeName, "Cutscene", StringComparison.Ordinal);
        }

        // A native scene is in charge of the party: the cutscene lock, or Dialog/Cutscene anywhere on the mode stack.
        public static bool SceneActive(bool cutsceneLock, bool dialogModeActive, bool cutsceneModeActive)
        {
            return cutsceneLock || dialogModeActive || cutsceneModeActive;
        }

        // A scripted operation: the native cutscene flag, or a command nobody but the scene can have issued while a
        // scene is in charge (not the player, not the unit's own AI, not this mod).
        public static bool IsScriptedCommand(bool nativeCutsceneCommand, bool sceneActive, bool riderControlledByCutscene,
            bool createdByPlayer, bool aiAction, bool modOwned)
        {
            if (modOwned || createdByPlayer || aiAction) return false;
            return nativeCutsceneCommand || sceneActive || riderControlledByCutscene;
        }

        public static MountedSceneCommandDisposition DecideCommand(bool relationshipMounted, bool executorIsExactRider,
            bool executorIsExactMount, bool scripted, bool encounterActive, bool simulatingClick)
        {
            if (!relationshipMounted || simulatingClick || !scripted || executorIsExactRider == executorIsExactMount)
                return MountedSceneCommandDisposition.NotScripted;
            if (executorIsExactMount) return MountedSceneCommandDisposition.KeepAttached;
            return encounterActive ? MountedSceneCommandDisposition.KeepAttachedInCombat : MountedSceneCommandDisposition.HandOffRider;
        }

        public static MountedSceneInGameDisposition DecideInGameChange(bool relationshipMounted, bool pairMemberLeftGame,
            bool sceneActive, bool bothAliveAndConscious, bool exactReciprocalRelationship, bool encounterActive)
        {
            return relationshipMounted && pairMemberLeftGame && sceneActive && bothAliveAndConscious &&
                exactReciprocalRelationship && !encounterActive
                ? MountedSceneInGameDisposition.HandOff
                : MountedSceneInGameDisposition.Validate;
        }

        // Whether a native scene still owns one actor's staging. Read from native state every time, so a skipped or
        // aborted scene needs no paired end callback: control is released, cutscene commands finish, the lock clears.
        public static bool ActorStagingOwned(bool controlledByLiveCutscene, bool hasUnfinishedCutsceneCommand,
            bool inGame, bool sceneActive)
        {
            return controlledByLiveCutscene || hasUnfinishedCutsceneCommand || (sceneActive && !inGame);
        }

        public static MountedRegroupState RegroupAvailability(MountedRetainedIntentKind kind, bool encounterActive,
            int attempts, bool commandLive)
        {
            if (commandLive) return MountedRegroupState.InProgress;
            if (kind != MountedRetainedIntentKind.ScriptedEvent || encounterActive) return MountedRegroupState.Unavailable;
            return attempts >= RegroupMaximumAttempts ? MountedRegroupState.Exhausted : MountedRegroupState.Available;
        }

        // One step of a running regroup move: the native move ended (arrived, failed or replaced) or ran out of time.
        public static MountedRegroupStep StepRegroup(bool commandLive, float elapsedSeconds, float separationMetres,
            float maximumSeparationMetres)
        {
            // A non-finite separation means an actor can no longer be measured (gone, or not resolved): nothing to wait on.
            if (float.IsNaN(separationMetres) || float.IsInfinity(separationMetres)) return MountedRegroupStep.Abandon;
            if (separationMetres <= maximumSeparationMetres) return MountedRegroupStep.Arrived;
            if (!commandLive || elapsedSeconds > RegroupDeadlineSeconds) return MountedRegroupStep.Abandon;
            return MountedRegroupStep.Wait;
        }
    }
}
