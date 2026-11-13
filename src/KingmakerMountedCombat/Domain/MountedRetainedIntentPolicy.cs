using System;

namespace KingmakerMountedCombat.Domain
{
    // Exploration repair (issues 3, 4 and 7): one bounded custody for "the pair is still meant to be mounted while
    // its physical attachment is suspended". Sources: native scene staging of a pair member (cutscene continuity
    // repair; ordinary Dialog/Cutscene modes keep the attachment), the camp workflow (assembly and Rest) and world-map
    // travel. Restoration re-attaches the same eligible pair without a Mount action, preparation, refund or turn;
    // anything that invalidates the pair cancels its restoration.
    public enum MountedRetainedIntentKind
    {
        // Cutscene continuity repair: a pair handed to native scene staging (a scripted operation on its rider, or a
        // pair member taken out of the game by a scene). Ordinary Dialog/Cutscene modes no longer suspend. The value
        // and its saved encoding are unchanged, so an older save's dialog suspension restores under these rules.
        ScriptedEvent,
        Camp,
        WorldMapTravel
    }

    public enum MountedRetainedIntentDecision
    {
        Hold,
        Restore,
        Cancel
    }

    public enum MountedRetainedPairDecision
    {
        Restore,
        Retry,
        Drop,
        // Cutscene continuity repair: the scene left the mount behind; it walks to its rider natively, once, bounded.
        Regroup
    }

    // Review finding 1: what one restoration attempt left behind.
    public enum MountedRetainedRestoreOutcome
    {
        Restored,
        // Refused or rolled back with every owned attachment returned: the pair is cleanly dismounted.
        CleanRefusal,
        // The attempt failed but movement authority, presentation or a prepared/faulted relationship remains owned.
        // Its cleanup ownership is kept and the all-pair failure latch blocks saving and new participation.
        RetainedResidue
    }

    // The uncertain case of the review: a carried arrival refusal that may be transient is retained for in-area retry.
    public enum MountedCarriedArrivalDecision
    {
        Restore,
        Retain,
        Drop,
        // Second owner review finding 2: the established native capital rule ends the relationship at arrival
        // (retired, cleanly separated, never pending or saved intent).
        Retire
    }

    public static class MountedRetainedIntentPolicy
    {
        // Rider and mount must finish the event close together. RestoreSavedPair seats the rider on the mount where
        // the mount stands; a larger separation means the event relocated one of them, so the pair stays dismounted
        // rather than snapping the rider across the scene. Native camp places every unit on its own campsite slot
        // (CampPositionsSelector slots are not tied to a pet), so a camp allows the extent of one campsite.
        public const float MaximumRestoreSeparationMetres = 6f;
        public const float MaximumCampRestoreSeparationMetres = 25f;

        public static float MaximumSeparationMetres(MountedRetainedIntentKind kind) =>
            kind == MountedRetainedIntentKind.Camp ? MaximumCampRestoreSeparationMetres : MaximumRestoreSeparationMetres;
        // A pair whose units are not yet attachable (an event still holds a stock agent disabled, a view not yet
        // settled) is retried for this long in ordinary play before its intent is dropped.
        public const float EligibilityPatienceSeconds = 10f;

        // The native mode whose start itself suspends the attachment: Rest (camp translocates every unit to its own
        // campsite slot). Cutscene continuity repair: Dialog and Cutscene no longer suspend at mode start; they keep
        // the attachment (MountedGameModeDisposition.PreserveScriptedScene) and only a scripted operation that stages
        // a pair member hands that one pair off. Every other non-retained mode keeps the existing clean dismount.
        public static bool SuspendsForEvent(string exactModeName)
        {
            return string.Equals(exactModeName, "Rest", StringComparison.Ordinal);
        }

        // The world map is the native area whose custom UI scene is the global map.
        public static bool IsWorldMapArea(bool hasCustomUi, string customUiSceneName, string globalMapSceneName)
        {
            return hasCustomUi && !string.IsNullOrEmpty(customUiSceneName) &&
                string.Equals(customUiSceneName, globalMapSceneName, StringComparison.Ordinal);
        }

        // Whether the retained intent may be restored inside the current area now.
        public static MountedRetainedIntentDecision Decide(
            MountedRetainedIntentKind kind,
            bool sameWorld,
            bool partyInCombat,
            bool onWorldMap,
            string currentModeName,
            bool eventModeStillActive,
            bool loading,
            bool saveOrAreaBoundaryOwned,
            bool campAssemblyPending)
        {
            if (!sameWorld) return MountedRetainedIntentDecision.Cancel;
            // Restoration never adopts a native encounter: re-pairing inside one would need a native participation
            // decision (preparation or a turn), which restoration must never manufacture. Cutscene continuity repair:
            // a pair still staged when the fight began keeps its intent through the encounter and remounts once it is
            // over (no Mount action, preparation, refund or replay). Camp and world-map custody keep the cancellation.
            // An ordinary hostile dialog never reaches here: its pair stayed attached and entered the fight mounted.
            if (partyInCombat)
                return kind == MountedRetainedIntentKind.ScriptedEvent ? MountedRetainedIntentDecision.Hold : MountedRetainedIntentDecision.Cancel;
            // World-map travel restores only through the next local area's arrival.
            if (onWorldMap) return MountedRetainedIntentDecision.Hold;
            if (!string.Equals(currentModeName, "Default", StringComparison.Ordinal) || eventModeStillActive || loading ||
                saveOrAreaBoundaryOwned || campAssemblyPending)
                return MountedRetainedIntentDecision.Hold;
            return MountedRetainedIntentDecision.Restore;
        }

        // Per pair, once restoration is admitted. A pair that was invalidated (removed, dead, re-paired, already
        // mounted again, separated by the event) is dropped; a transiently unattachable pair is retried.
        public static MountedRetainedPairDecision DecidePair(
            bool unitsResolved,
            bool sameReciprocalProfile,
            bool alreadyInARelationship,
            bool permanentlyIneligible,
            string eligibilityRefusal,
            float separationMetres,
            float maximumSeparationMetres,
            bool sameNavmeshArea,
            bool patienceExpired,
            bool stagingOwned = false,
            MountedRegroupState regroup = MountedRegroupState.Unavailable)
        {
            if (!unitsResolved || !sameReciprocalProfile || alreadyInARelationship || permanentlyIneligible)
                return MountedRetainedPairDecision.Drop;
            // Cutscene continuity repair: a native scene still controls one of the actors (a playing command, a
            // cutscene-made order still running, or a scene that took it out of the game). Its final placement is
            // not known yet, so nothing is judged; this pair waits and the others are decided on their own.
            if (stagingOwned) return MountedRetainedPairDecision.Retry;
            var finite = !float.IsNaN(separationMetres) && !float.IsInfinity(separationMetres);
            if (!finite || separationMetres > maximumSeparationMetres || !sameNavmeshArea)
            {
                // The scene's final placement is respected: the rider is never snapped back to the mount. A mount left
                // behind within reach on the same navigation area walks to its rider; anything else stays dismounted.
                if (regroup == MountedRegroupState.InProgress) return MountedRetainedPairDecision.Retry;
                if (regroup == MountedRegroupState.Available && finite && sameNavmeshArea &&
                    separationMetres <= MountedSceneContinuityPolicy.RegroupMaximumMetres)
                    return MountedRetainedPairDecision.Regroup;
                return MountedRetainedPairDecision.Drop;
            }
            if (eligibilityRefusal == null) return MountedRetainedPairDecision.Restore;
            return patienceExpired ? MountedRetainedPairDecision.Drop : MountedRetainedPairDecision.Retry;
        }

        // On arrival in a local area with carried intent (world-map travel, an earlier event): an invalidated pair
        // (missing actor, changed profile or pairing, dead) or an arrival in combat is dropped individually; any other
        // refusal (a mode or agent not yet settled at OnAreaLoaded) is retained for the in-area retry, which applies
        // the bounded patience. Neither blocks the other pairs, saving or later area changes.
        public static MountedCarriedArrivalDecision DecideCarriedArrival(bool partyInCombat, bool unitsResolved,
            bool sameReciprocalProfile, bool permanentlyIneligible, string eligibilityRefusal, bool capitalControlRetirement = false)
        {
            if (!unitsResolved || !sameReciprocalProfile || permanentlyIneligible || partyInCombat) return MountedCarriedArrivalDecision.Drop;
            if (eligibilityRefusal == null) return MountedCarriedArrivalDecision.Restore;
            // The exact capital predicate (MountedAreaControlRule) is terminal and is decided before any retry.
            return capitalControlRetirement ? MountedCarriedArrivalDecision.Retire : MountedCarriedArrivalDecision.Retain;
        }

        // Review finding 3: a camp that begins while a pair is already suspended for a scripted event adopts the camp
        // rules (its native slot separation); world-map travel keeps restoring only through the next arrival.
        public static MountedRetainedIntentKind Promote(MountedRetainedIntentKind current, MountedRetainedIntentKind incoming)
        {
            return current == MountedRetainedIntentKind.ScriptedEvent && incoming == MountedRetainedIntentKind.Camp
                ? MountedRetainedIntentKind.Camp : current;
        }

        // Review finding 1: only an attempt whose relationship is observed cleanly unmounted (no movement authority,
        // presentation, prepared pair or attachment residue) may be treated as a pair that simply stayed dismounted.
        public static MountedRetainedRestoreOutcome ClassifyRestore(bool succeeded, bool movementAuthorityResidual,
            bool presentationResidual, bool relationshipUnmounted, bool noPreparedPairResidue, bool noAttachmentResidue)
        {
            if (succeeded) return MountedRetainedRestoreOutcome.Restored;
            return !movementAuthorityResidual && !presentationResidual && relationshipUnmounted && noPreparedPairResidue && noAttachmentResidue
                ? MountedRetainedRestoreOutcome.CleanRefusal
                : MountedRetainedRestoreOutcome.RetainedResidue;
        }
    }
}
