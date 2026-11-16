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

    // Known issues S02: a pair that was mounted at departure (not carried intent) at its arrival placement.
    public enum MountedDepartingArrivalDecision
    {
        Restore,
        // Eligible, but its exact views are not active yet: it joins the retained custody and restores in ordinary play.
        Defer,
        Retire,
        // The established all-pair failure (any refusal other than the native capital rule).
        Fail
    }

    public static class MountedRetainedIntentPolicy
    {
        // Rider and mount must finish the event close together. RestoreSavedPair seats the rider on the mount where
        // the mount stands; a larger separation means the event relocated one of them, so the pair stays dismounted
        // rather than snapping the rider across the scene. Native camp places every unit on its own campsite slot
        // (CampPositionsSelector slots are not tied to a pet), so a camp allows the extent of one campsite.
        public const float MaximumRestoreSeparationMetres = 6f;
        public const float MaximumCampRestoreSeparationMetres = 25f;

        // PR #37 review P2: restoration seats the rider on the mount where the mount stands, so the separation it accepts is
        // exactly how far it may move the rider. Scene staging promises the scene's final placement: it accepts only the
        // scene arrival distance, and a mount farther away walks to the rider first (MountedSceneContinuityPolicy).
        public static float MaximumSeparationMetres(MountedRetainedIntentKind kind) =>
            kind == MountedRetainedIntentKind.Camp ? MaximumCampRestoreSeparationMetres
            : kind == MountedRetainedIntentKind.ScriptedEvent ? MountedSceneContinuityPolicy.SceneRestoreSeparationMetres
            : MaximumRestoreSeparationMetres;
        // A pair whose units are not yet attachable (an event still holds a stock agent disabled, a view not yet
        // settled) is retried for this long in ordinary play before its intent is dropped.
        public const float EligibilityPatienceSeconds = 10f;

        // The native settle bound: restoration needs ordinary play to hold for several frames, never a single
        // intermediate Default notification between two queued native mode switches.
        public const int SettleFrames = 5;
        public const float SettleSeconds = 0.3f;

        public static bool Settled(int settledFrames, float settledSeconds)
        {
            return settledFrames >= SettleFrames && settledSeconds >= SettleSeconds;
        }

        // Patience counts from the first settled evaluation in which the pair was neither staged nor regrouping
        // (a negative start means it is not counting).
        public static bool PatienceExpired(float eligibleSince, float now)
        {
            return eligibleSince >= 0f && now - eligibleSince > EligibilityPatienceSeconds;
        }

        // Known issues S02: a unit counts as still standing where native arrival placed it within this distance.
        public const float ArrivalPlacementToleranceMetres = 0.1f;

        public static bool ArrivalPlacementHeld(bool deferredAtArrival, float riderDisplacementMetres, float mountDisplacementMetres)
        {
            return deferredAtArrival && riderDisplacementMetres <= ArrivalPlacementToleranceMetres &&
                mountDisplacementMetres <= ArrivalPlacementToleranceMetres;
        }

        // Known issues S02: an explicit player dismount ends that pair's retained intent at once, so a restoration that
        // is still pending for it (not yet settled, or held through an encounter) can never re-attach a pair the
        // player dismounted.
        public static bool ReleasedByDismount(CleanupTrigger trigger)
        {
            return trigger == CleanupTrigger.Manual;
        }

        public static bool SharesActor(string retainedRiderId, string retainedMountId, string riderId, string mountId)
        {
            return Same(retainedRiderId, riderId) || Same(retainedRiderId, mountId) ||
                Same(retainedMountId, riderId) || Same(retainedMountId, mountId);
        }

        private static bool Same(string left, string right)
        {
            return !string.IsNullOrEmpty(left) && string.Equals(left, right, StringComparison.Ordinal);
        }

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
            MountedRegroupState regroup = MountedRegroupState.Unavailable,
            bool stagingPatienceExpired = false,
            string viewReadinessRefusal = null,
            bool arrivalPlacementHeld = false)
        {
            if (!unitsResolved || !sameReciprocalProfile || alreadyInARelationship || permanentlyIneligible)
                return MountedRetainedPairDecision.Drop;
            // Cutscene continuity repair: a native scene still controls one of the actors (a playing command, a
            // cutscene-made order still running, or a scene that took it out of the game). Its final placement is
            // not known yet, so nothing is judged; this pair waits and the others are decided on their own. A scene
            // that keeps control in ordinary play past its bounded patience releases the intent instead.
            if (stagingOwned) return stagingPatienceExpired ? MountedRetainedPairDecision.Drop : MountedRetainedPairDecision.Retry;
            var finite = !float.IsNaN(separationMetres) && !float.IsInfinity(separationMetres);
            // Known issues S02: a pair deferred at its arrival placement whose rider and mount still stand exactly where
            // native arrival placed them is the arrival restoration, only later. The arrival restoration never had a
            // separation rule (the native formation gives every unit its own slot, not tied to its pet), and nothing
            // relocated the pair. Once either unit has moved, the rule below applies as for any other retained pair.
            if (!finite || !arrivalPlacementHeld && (separationMetres > maximumSeparationMetres || !sameNavmeshArea))
            {
                // The scene's final placement is respected: the rider is never snapped back to the mount. A mount left
                // behind within reach on the same navigation area walks to its rider; anything else stays dismounted.
                if (regroup == MountedRegroupState.InProgress) return MountedRetainedPairDecision.Retry;
                if (regroup == MountedRegroupState.Available && finite && sameNavmeshArea &&
                    separationMetres <= MountedSceneContinuityPolicy.RegroupMaximumMetres)
                    return MountedRetainedPairDecision.Regroup;
                return MountedRetainedPairDecision.Drop;
            }
            // Known issues S02: views that are not active yet are a transient refusal like any other, under the same
            // bounded patience; nothing is attached to them.
            if (eligibilityRefusal == null && viewReadinessRefusal == null) return MountedRetainedPairDecision.Restore;
            return patienceExpired ? MountedRetainedPairDecision.Drop : MountedRetainedPairDecision.Retry;
        }

        // On arrival in a local area with carried intent (world-map travel, an earlier event): an invalidated pair
        // (missing actor, changed profile or pairing, dead) or an arrival in combat is dropped individually; any other
        // refusal (a mode or agent not yet settled at OnAreaLoaded) is retained for the in-area retry, which applies
        // the bounded patience. Neither blocks the other pairs, saving or later area changes.
        public static MountedCarriedArrivalDecision DecideCarriedArrival(bool partyInCombat, bool unitsResolved,
            bool sameReciprocalProfile, bool permanentlyIneligible, string eligibilityRefusal, bool capitalControlRetirement = false,
            string viewReadinessRefusal = null)
        {
            if (!unitsResolved || !sameReciprocalProfile || permanentlyIneligible || partyInCombat) return MountedCarriedArrivalDecision.Drop;
            // Known issues S02: an otherwise eligible pair whose exact views are not active yet (the party is still
            // "sleeping" from world-map mode until the first Default tick) is retained for the in-area retry too.
            if (eligibilityRefusal == null)
                return viewReadinessRefusal == null ? MountedCarriedArrivalDecision.Restore : MountedCarriedArrivalDecision.Retain;
            // The exact capital predicate (MountedAreaControlRule) is terminal and is decided before any retry.
            return capitalControlRetirement ? MountedCarriedArrivalDecision.Retire : MountedCarriedArrivalDecision.Retain;
        }

        // A pair mounted at departure, at its arrival placement. Every refusal keeps its established outcome (the native
        // capital rule retires, anything else fails the all-pair restoration); only an eligible pair whose exact views
        // are not active yet is deferred to the retained custody instead of being attached to inactive views.
        public static MountedDepartingArrivalDecision DecideDepartingArrival(string eligibilityRefusal, bool capitalControlRetirement,
            string viewReadinessRefusal)
        {
            if (eligibilityRefusal != null)
                return capitalControlRetirement ? MountedDepartingArrivalDecision.Retire : MountedDepartingArrivalDecision.Fail;
            return viewReadinessRefusal == null ? MountedDepartingArrivalDecision.Restore : MountedDepartingArrivalDecision.Defer;
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
