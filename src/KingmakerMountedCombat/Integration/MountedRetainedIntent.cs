using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.AreaLogic.Cutscenes;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.GameModes;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Integration
{
    // Exploration repair (issues 3, 4 and 7). Retained mounted intent: the physical attachment of a mounted pair is
    // suspended (an ordinary GuardBoundary dismount) while a native scripted event, the camp workflow or world-map
    // travel owns the actors, and the same eligible pair is re-attached afterwards through the established
    // saved-ownership import and RestoreSavedPair: no public Mount action, no preparation, no refund, no turn and no
    // movement grant. The custody is bounded to one world (Player instance and campaign), is saved in its own archive
    // member, and is cancelled by combat, mod teardown or invalidation.
    internal sealed partial class MountedPersistenceService
    {
        private AreaTransfer retainedIntent;
        private MountedRetainedIntentKind retainedIntentKind;
        private MountedRetainedIntentRecord pendingLoadedRetainedIntent;
        // The selecting load's sequence: a record is adopted only by the load that read it (never by a replacement).
        private long pendingLoadedRetainedSequence = -1;
        private int retainedSettledFrames;
        private float retainedSettledSince = -1f;
        // The native settle bound: restoration needs ordinary play to hold for several frames, never a single
        // intermediate Default notification between two queued native mode switches.
        private const int RetainedSettleFrames = 5;
        private const float RetainedSettleSeconds = 0.3f;

        internal bool RetainedIntentPending => retainedIntent != null;
        internal int RetainedIntentPairCount => retainedIntent?.Pairs?.Length ?? 0;
        internal MountedRetainedIntentKind? RetainedIntentKind => retainedIntent == null ? (MountedRetainedIntentKind?)null : retainedIntentKind;
        internal int RetainedSuspensionCount { get; private set; }
        internal int RetainedRestoreCount { get; private set; }
        internal int RetainedDropCount { get; private set; }
        internal int RetainedSavedCount { get; private set; }
        internal int RetainedAdoptedCount { get; private set; }

        // Called at a native event boundary (Dialog, locked Cutscene, Rest, or the camp interaction before its approach
        // orders are issued). Returns true when every mounted pair is now suspended with its intent retained (or when
        // nothing is mounted and an earlier suspension of this world already holds the intent). False leaves the
        // caller's existing clean-dismount path in charge.
        internal bool TrySuspendForEvent(MountedRetainedIntentKind kind, string source)
        {
            var world = Game.Instance?.Player;
            if (!Enabled || world == null) return false;
            // Review finding 3: a pair already suspended in this world adopts the stronger rules of the new event first,
            // before any early return (a camp that follows a dialog keeps the camp separation).
            PromoteRetainedIntent(kind);
            var mounted = WorldRelationships.Where(r => r.State == RelationshipState.Mounted && r.Rider != null && r.Mount != null).ToArray();
            if (mounted.Length == 0) return retainedIntent != null && ReferenceEquals(retainedIntent.World.Target, world);
            if (world.IsInCombat || mounted.Any(r => r.Rider.IsInCombat || r.Mount.IsInCombat)) return false;
            if (SuspensionRefusal("event suspension") != null) return false;
            var captured = SuspendPairs(world, kind, mounted, out var failed);
            if (captured.Count != 0)
                Report(captured.Count + " mounted relationship" + (captured.Count == 1 ? "" : "s") + " suspended for " + Describe(retainedIntentKind) +
                    " (" + source + "); the pair remounts automatically when ordinary play resumes.", true);
            return !failed;
        }

        // The world-wide owned boundaries no suspension may cross. An area arrival that already restored (an arrival
        // cutscene follows OnAreaLoaded) no longer owns the pairs.
        private string SuspensionRefusal(string purpose)
        {
            if (areaTransfer != null && !areaTransfer.Lifecycle.ResumeAttempted) return "an area transfer owns the pairs";
            if (SaveDraining || SaveSuspended || HasSaveAdmissionFence || HasActiveSaveScope) return "a save owns mounted state";
            if (presentationPending || CombatSemanticRestorationPending) return "a load restoration is pending";
            if (HasAreaParticipationObligations()) return "paired participation is unsettled";
            if (!DrainWorldCharge(purpose)) return "mounted charge cleanup is incomplete";
            return null;
        }

        // Suspends exactly the given mounted relationships into this world's retained custody. The exact owner identity
        // and the settled participation are captured as the area transfer captures them; a pair already in custody is
        // replaced only by its own newer capture (a pair is captured only while physically mounted, so a nested callback
        // for an already suspended pair captures nothing and never replaces its provenance).
        private List<AreaPair> SuspendPairs(Player world, MountedRetainedIntentKind kind, IEnumerable<GameMountedRelationshipService> owners,
            out bool failed, string stagingBoundary = null)
        {
            if (retainedIntent != null && (!ReferenceEquals(retainedIntent.World.Target, world) || retainedIntent.CampaignId != world.GameId))
                DropRetainedIntent("a different world");
            var slots = controls.CapturePersistentSlots();
            var captured = new List<AreaPair>();
            failed = false;
            foreach (var owner in owners)
            {
                var pair = new AreaPair { RiderId = owner.Rider.UniqueId, MountId = owner.Mount.UniqueId, ProfileId = owner.Runtime.MountProfileId,
                    OwnerId = owner.PairOwner?.Id.ToString("N"), Generation = owner.PairOwner?.Generation ?? 0, Carried = true,
                    Participation = unifiedTurn.SnapshotParticipants.SingleOrDefault(p => ReferenceEquals(p.Relationship, owner))?.CaptureParticipation(),
                    StagingBoundary = stagingBoundary };
                try
                {
                    if (owner.GuardBoundary(CleanupTrigger.EventSuspension)) captured.Add(pair);
                    else failed = true;
                }
                catch (Exception error) { failed = true; logger.Exception("Mounted event suspension retained a pair", error); }
            }
            if (captured.Count == 0) return captured;
            if (retainedIntent == null)
            {
                retainedIntent = new AreaTransfer { World = new WeakReference(world), CampaignId = world.GameId,
                    AreaId = Game.Instance.CurrentlyLoadedArea?.AssetGuidThreadSafe, Pairs = new AreaPair[0], Slots = new SavedMountedSlot[0] };
                retainedIntentKind = kind;
            }
            var ids = new HashSet<string>(captured.SelectMany(p => new[] { p.RiderId, p.MountId }), StringComparer.Ordinal);
            retainedIntent.Pairs = retainedIntent.Pairs.Where(p => !ids.Contains(p.RiderId) && !ids.Contains(p.MountId)).Concat(captured).ToArray();
            retainedIntent.Slots = retainedIntent.Slots.Where(s => !ids.Contains(s.ActorId))
                .Concat(slots.Where(s => ids.Contains(s.ActorId))).ToArray();
            ResetRetainedSettle();
            RetainedSuspensionCount++;
            return captured;
        }

        internal int SceneStagingHandOffCount { get; private set; }
        internal int SceneStagingRefusedCount { get; private set; }
        internal int ScenePreservedObservationCount { get; private set; }
        internal int SceneRegroupCount { get; private set; }

        // Cutscene continuity repair. Native Dialog or Cutscene mode began or ended with pairs attached: they stay
        // attached (one bounded line per mode transition, never per frame).
        internal void ObserveScenePreserved(string mode, bool started)
        {
            var mounted = WorldRelationships.Where(r => r.State == RelationshipState.Mounted && r.Rider != null && r.Mount != null).ToArray();
            if (mounted.Length == 0) return;
            ScenePreservedObservationCount++;
            logger.Info("Mounted attachment preserved through native " + mode + (started ? " start" : " end") + ": " +
                string.Join(", ", mounted.Select(r => "rider=" + r.Rider.UniqueId + ";mount=" + r.Mount.UniqueId).ToArray()) +
                "; gameplay input stays refused until Default.");
        }

        // The native UnitCommands.Run(UnitCommand) prefix, before every routing gate: a scripted scene operation on a
        // mounted rider hands that one pair to native staging before native Run executes it on the rider; one on the
        // mount runs natively with the rider seated. Returns true when the command is a scripted scene operation on a pair
        // actor: the caller then lets native Run execute it as the scene authored it, never through the pair's
        // player-order routing (which would refuse or reroute, e.g., a scripted stock attack of the mount).
        internal bool ObserveSceneCommand(UnitCommands container, UnitCommand command, bool simulatingClick)
        {
            if (!Enabled || container == null || command == null || simulatingClick) return false;
            // Every unit's Run passes here: a player, AI or mod-owned order is never scripted (IsScriptedCommand), so it
            // leaves before the relationship scan.
            if (command.CreatedByPlayer || command.AiAction != null) return false;
            var game = Game.Instance;
            var owner = WorldRelationships.FirstOrDefault(r => r.State == RelationshipState.Mounted &&
                (ReferenceEquals(r.Rider?.Commands, container) || ReferenceEquals(r.Mount?.Commands, container)));
            if (owner == null || game?.Player == null) return false;
            var rider = owner.Rider; var mount = owner.Mount;
            var sceneActive = SceneActive(game);
            var scripted = MountedSceneContinuityPolicy.IsScriptedCommand(command.Cutscene, sceneActive,
                NativeSceneControl.IsUnderLiveSceneControl(rider),
                command.CreatedByPlayer, command.AiAction != null, command.GetType().Assembly == typeof(MountedSceneContinuityPolicy).Assembly);
            var encounter = game.Player.IsInCombat || (rider?.IsInCombat ?? false) || (mount?.IsInCombat ?? false);
            var disposition = MountedSceneContinuityPolicy.DecideCommand(true, ReferenceEquals(rider?.Commands, container),
                ReferenceEquals(mount?.Commands, container), scripted, encounter, false);
            var detail = command.GetType().Name + (command.Cutscene ? " (cutscene)" : "") + " in " + game.CurrentMode;
            switch (disposition)
            {
                case MountedSceneCommandDisposition.KeepAttached:
                    logger.Info("Scripted scene command on the mount runs natively with its rider seated: rider=" + rider?.UniqueId +
                        ";mount=" + mount?.UniqueId + ";command=" + detail + ".");
                    return true;
                case MountedSceneCommandDisposition.KeepAttachedInCombat:
                    SceneStagingRefusedCount++;
                    logger.Warning("Scripted scene command on a mounted rider during an encounter keeps the attachment (staging is not " +
                        "supported in combat; the scene's command runs natively): rider=" + rider?.UniqueId + ";mount=" + mount?.UniqueId +
                        ";command=" + detail + ".");
                    return true;
                case MountedSceneCommandDisposition.HandOffRider:
                    // Handed off: the rider is free and native Run executes the command. Refused: the pair stays attached and
                    // the scene's command still runs natively rather than through the pair's player-order gates.
                    TryHandOffForStaging(owner, "UnitCommands.Run(" + detail + ")");
                    return true;
            }
            return false;
        }

        // Polled from Update: a mounted rider's synchronizer saw a scene place the rider away from its saddle (a direct
        // Position write, e.g. TranslocateUnit or CommandTeleportParty) and held. The pair is handed off at that placement;
        // a refused handoff releases the synchronizer, which returns the rider to the saddle (the in-combat fallback).
        internal void ObserveScenePlacements()
        {
            if (!Enabled) return;
            foreach (var owner in WorldRelationships)
            {
                var agent = owner.State == RelationshipState.Mounted ? owner.Runtime?.MovementAgent : null;
                var placement = agent?.PendingScenePlacement;
                if (placement == null) continue;
                var orientation = agent.PendingSceneOrientation;
                var handedOff = false;
                try { handedOff = TryHandOffForStaging(owner, "scene placement of the rider", placement.Value, orientation); }
                catch (Exception error) { logger.Exception("Scene placement handoff was not evaluated", error); }
                if (!handedOff) agent.ReleaseScenePlacement();
            }
        }

        // IInGameHandler.HandleObjectInGameChaged for a mounted pair actor, before invariant validation. True when the
        // scene's removal was taken as temporary staging (the caller then skips the invalidation path).
        internal bool ObserveSceneInGameChange(GameMountedRelationshipService owner, UnitEntityData unit)
        {
            var game = Game.Instance;
            if (!Enabled || owner == null || unit == null || game?.Player == null || owner.State != RelationshipState.Mounted) return false;
            var rider = owner.Rider; var mount = owner.Mount;
            if (rider == null || mount == null) return false;
            var candidate = KingmakerMountedPairRuntime.CreateCandidate(rider, mount);
            var disposition = MountedSceneContinuityPolicy.DecideInGameChange(true, !unit.IsInGame, SceneActive(game),
                candidate.RiderIsAliveAndConscious && candidate.MountIsAliveAndConscious, candidate.ExactReciprocalCompanionRelationship,
                game.Player.IsInCombat || rider.IsInCombat || mount.IsInCombat);
            return disposition == MountedSceneInGameDisposition.HandOff &&
                TryHandOffForStaging(owner, "IInGameHandler.HandleObjectInGameChaged(" + unit.UniqueId + " left the game during a scene)");
        }

        // Pair-local handoff to native scene staging: only this pair is suspended (its exact owner identity, generation and
        // world retained); every other pair keeps its attachment. A refusal leaves the pair attached and says why.
        // A scene placement (the rider's position written by the scene) is applied after the suspension, so the scene's
        // final placement stands instead of the ordinary placement beside the mount.
        internal bool TryHandOffForStaging(GameMountedRelationshipService owner, string boundary,
            UnityEngine.Vector3? scenePlacement = null, float sceneOrientation = 0f)
        {
            var world = Game.Instance?.Player;
            if (!Enabled || world == null || owner?.Rider == null || owner.Mount == null || owner.State != RelationshipState.Mounted) return false;
            // Review P1: a completed suspension clears the relationship's rider and mount, so every later use reads these.
            var rider = owner.Rider;
            var mount = owner.Mount;
            var pendingKind = retainedIntent != null && ReferenceEquals(retainedIntent.World.Target, world) && retainedIntent.CampaignId == world.GameId
                ? retainedIntentKind : (MountedRetainedIntentKind?)null;
            var refusal = world.IsInCombat || rider.IsInCombat || mount.IsInCombat ? "an encounter is active"
                : MountedSceneContinuityPolicy.HandOffCustodyRefusal(pendingKind) ?? SuspensionRefusal("scene staging");
            if (refusal == null)
            {
                List<AreaPair> captured = null;
                var failed = false;
                var target = scenePlacement ?? UnityEngine.Vector3.zero;
                var outcome = MountedSceneHandOffSequence.Run(rider,
                    () =>
                    {
                        captured = SuspendPairs(world, MountedRetainedIntentKind.ScriptedEvent, new[] { owner }, out var suspendFailed, boundary);
                        failed = suspendFailed;
                        return captured.Count == 1 && !failed;
                    },
                    scenePlacement.HasValue,
                    r => r.Translocate(target, sceneOrientation),
                    r => new UnityEngine.Vector2(r.Position.x - target.x, r.Position.z - target.z).magnitude <= ScenePlacementToleranceMetres,
                    out var placementError);
                if (MountedSceneHandOffSequence.IsHandedOff(outcome))
                {
                    SceneStagingHandOffCount++;
                    var pair = captured[0];
                    var identity = " (rider=" + pair.RiderId + ";mount=" + pair.MountId + ";owner=" + pair.OwnerId + "/" + pair.Generation + ")";
                    if (outcome == MountedSceneHandOffOutcome.HandedOffPlacementLost)
                    {
                        ScenePlacementLostCount++;
                        if (placementError != null) logger.Exception("The scene's placement of a handed-off rider was not reapplied", placementError);
                        logger.Warning("Scene placement not preserved for rider=" + pair.RiderId + ";mount=" + pair.MountId + ": target=" + target +
                            ";actual=" + rider.Position + ".");
                        Report(DescribePair(rider, mount, pair) + " handed to native scene staging at " + boundary + identity +
                            ", but the scene's placement of the rider could not be kept; the pair remounts automatically when the scene releases them.");
                        return true;
                    }
                    Report(DescribePair(rider, mount, pair) + " handed to native scene staging at " + boundary + identity +
                        (outcome == MountedSceneHandOffOutcome.HandedOffPlacementPreserved ? " at the scene's placement" : "") +
                        "; the pair remounts automatically when the scene releases them.", true);
                    return true;
                }
                refusal = "the attachment could not be suspended cleanly";
            }
            SceneStagingRefusedCount++;
            logger.Warning("Scene staging handoff refused for rider=" + rider.UniqueId + ";mount=" + mount.UniqueId +
                " at " + boundary + ": " + refusal + ".");
            return false;
        }

        // A reapplied scene placement holds when the rider stands where the scene put it (native Translocate is exact).
        private const float ScenePlacementToleranceMetres = 0.1f;
        internal int ScenePlacementLostCount { get; private set; }

        private static bool SceneActive(Game game) =>
            game != null && MountedSceneContinuityPolicy.SceneActive(game.CutsceneLock, game.IsModeActive(GameModeType.Dialog),
                game.IsModeActive(GameModeType.Cutscene));

        // Read from native state on every evaluation: a cutscene still playing a command that controls the unit, or a
        // cutscene-made order still in one of its slots, or a scene that took it out of the game.
        private static bool ActorStagingOwned(UnitEntityData unit, bool sceneActive)
        {
            if (unit == null) return false;
            var controlled = NativeSceneControl.IsUnderLiveSceneControl(unit);
            var cutsceneCommand = unit.Commands != null && unit.Commands.Contains(c => c.Cutscene && !c.IsFinished);
            return MountedSceneContinuityPolicy.ActorStagingOwned(controlled, cutsceneCommand, unit.IsInGame, sceneActive);
        }

        // Review finding 3: an event mode that begins while the pairs are already physically dismounted (a Rest after a
        // dialog) updates only the retained pairs of this world; no unmounted actor is captured.
        internal void PromoteRetainedIntent(MountedRetainedIntentKind kind)
        {
            var world = Game.Instance?.Player;
            if (retainedIntent == null || world == null || !ReferenceEquals(retainedIntent.World.Target, world)) return;
            retainedIntentKind = MountedRetainedIntentPolicy.Promote(retainedIntentKind, kind);
        }

        // Ordinary-play restoration inside the current area. Polled from Update; the decision itself is pure policy.
        internal void TryRestoreRetainedIntent()
        {
            if (retainedIntent == null) return;
            try { RestoreRetainedIntentCore(); }
            catch (Exception error)
            {
                // Review finding 5: a fault while deciding restoration never becomes a per-frame mod update failure.
                // The restore helper itself classifies any attempt that touched native state; this catch only sees
                // failures before an attempt, so the retained pairs are still cleanly dismounted.
                logger.Exception("Retained mounted intent restoration failed", error);
                DropRetainedIntent("restoration could not be evaluated");
            }
        }

        private void RestoreRetainedIntentCore()
        {
            var custody = retainedIntent;
            if (custody == null) return;
            var game = Game.Instance;
            var player = game?.Player;
            var sameWorld = Enabled && player != null && ReferenceEquals(custody.World.Target, player) && player.GameId == custody.CampaignId;
            var area = game?.CurrentlyLoadedArea;
            var decision = MountedRetainedIntentPolicy.Decide(
                retainedIntentKind,
                sameWorld,
                player?.IsInCombat ?? false,
                IsWorldMapArea(area),
                game == null ? "None" : game.CurrentMode.ToString(),
                game != null && (game.IsModeActive(GameModeType.Dialog) || game.IsModeActive(GameModeType.Cutscene) || game.IsModeActive(GameModeType.Rest)),
                LoadingProcess.Instance != null && LoadingProcess.Instance.IsLoadingInProcess,
                (areaTransfer != null) || HasActiveSaveScope || SaveDraining || SaveSuspended || HasSaveAdmissionFence || presentationPending ||
                    CombatSemanticRestorationPending,
                CampAssemblyPending(custody, game));
            if (decision == MountedRetainedIntentDecision.Cancel)
            {
                DropRetainedIntent(!sameWorld ? "the world changed" : "combat began");
                return;
            }
            if (decision == MountedRetainedIntentDecision.Hold)
            {
                // A fight that began while a pair was still staged: the intent is kept through it (never adopted into
                // the encounter), and a regroup walk in progress stops so it spends nothing of the mount's turn.
                if (player?.IsInCombat == true)
                {
                    InterruptRegroups(custody, game, "an encounter began");
                    if (!retainedCombatHoldLogged)
                    {
                        retainedCombatHoldLogged = true;
                        logger.Info("Retained mounted intent held through the encounter for " + custody.Pairs.Length + " staged pair" +
                            (custody.Pairs.Length == 1 ? "" : "s") + "; the pair" + (custody.Pairs.Length == 1 ? " remounts" : "s remount") +
                            " after combat without Mount, preparation or refund.");
                    }
                }
                ResetRetainedSettle();
                return;
            }
            retainedCombatHoldLogged = false;
            var now = UnityEngine.Time.unscaledTime;
            if (retainedSettledSince < 0f) retainedSettledSince = now;
            if (++retainedSettledFrames < RetainedSettleFrames || now - retainedSettledSince < RetainedSettleSeconds) return;
            var sceneActive = SceneActive(game);
            var maximumSeparation = MountedRetainedIntentPolicy.MaximumSeparationMetres(retainedIntentKind);

            var remaining = new List<AreaPair>();
            var restored = new List<AreaPair>();
            var dropped = new List<string>();
            foreach (var pair in custody.Pairs)
            {
                var rider = ResolveUnique(game, pair.RiderId);
                var mount = ResolveUnique(game, pair.MountId);
                var candidate = rider != null && mount != null ? KingmakerMountedPairRuntime.CreateCandidate(rider, mount) : null;
                var refusal = candidate?.ValidateSavedRelationship();
                var separation = rider != null && mount != null
                    ? new UnityEngine.Vector2(rider.Position.x - mount.Position.x, rider.Position.z - mount.Position.z).magnitude
                    : float.PositiveInfinity;
                var sameArea = rider != null && mount != null && SameNavmeshArea(rider, mount);
                // Cutscene continuity repair: staging ownership and the regroup walk are per pair, read from native state.
                var stagingOwned = ActorStagingOwned(rider, sceneActive) || ActorStagingOwned(mount, sceneActive);
                var regroupLive = pair.RegroupMove != null && !pair.RegroupMove.IsFinished && mount != null && mount.Commands.Contains(pair.RegroupMove);
                var regroup = MountedSceneContinuityPolicy.RegroupAvailability(retainedIntentKind, player.IsInCombat, pair.RegroupAttempts, regroupLive);
                if (regroupLive)
                {
                    // The deadline is measured in native game time, so a pause or a scene that holds the walk spends none of it.
                    var step = MountedSceneContinuityPolicy.StepRegroup(true,
                        (float)(game.TimeController.GameTime.TotalSeconds - pair.RegroupStartedAt), separation, maximumSeparation);
                    if (step != MountedRegroupStep.Wait)
                    {
                        // Arrived: the walk ends here, so restoration never re-attaches a pair whose mount is still walking.
                        // Abandoned (deadline, replaced, unmeasurable): the attempt is spent; a remaining attempt may walk again.
                        pair.RegroupMove.Interrupt();
                        regroupLive = false;
                        regroup = MountedSceneContinuityPolicy.RegroupAvailability(retainedIntentKind, player.IsInCombat, pair.RegroupAttempts, false);
                    }
                }
                if (!regroupLive) pair.RegroupMove = null;
                // Patience counts only this pair's own settled, unowned, non-regrouping play.
                if (stagingOwned || regroup == MountedRegroupState.InProgress) pair.EligibleSince = -1f;
                else if (pair.EligibleSince < 0f) pair.EligibleSince = now;
                var patienceExpired = pair.EligibleSince >= 0f && now - pair.EligibleSince > MountedRetainedIntentPolicy.EligibilityPatienceSeconds;
                // A scene that keeps control in settled ordinary play is bounded too (native game time).
                var gameSeconds = game.TimeController.GameTime.TotalSeconds;
                if (!stagingOwned) pair.StagingOwnedSince = -1d;
                else if (pair.StagingOwnedSince < 0d) pair.StagingOwnedSince = gameSeconds;
                var stagingExpired = stagingOwned && gameSeconds - pair.StagingOwnedSince > MountedSceneContinuityPolicy.StagingPatienceSeconds;
                var pairDecision = MountedRetainedIntentPolicy.DecidePair(
                    candidate != null,
                    candidate != null && SupportedMountedProfiles.Resolve(rider, mount)?.Id == pair.ProfileId,
                    candidate != null && InAnyRelationship(rider, mount),
                    PermanentlyIneligible(candidate),
                    refusal,
                    separation,
                    maximumSeparation,
                    sameArea,
                    patienceExpired,
                    stagingOwned,
                    regroup,
                    stagingExpired);
                if (pairDecision == MountedRetainedPairDecision.Retry) { remaining.Add(pair); continue; }
                if (pairDecision == MountedRetainedPairDecision.Regroup)
                {
                    StartRegroup(pair, rider, mount, separation, game.TimeController.GameTime.TotalSeconds);
                    remaining.Add(pair);
                    continue;
                }
                if (pairDecision == MountedRetainedPairDecision.Drop)
                {
                    var separated = candidate != null && (separation > maximumSeparation || !sameArea);
                    var reason = stagingExpired ? "a scene kept control of the pair" : refusal ?? (separated
                        ? (pair.RegroupAttempts != 0 ? "the mount could not regroup with its rider" : "rider and mount were separated by the event")
                        : "the pair is no longer eligible");
                    if (pair.RegroupMove != null && !pair.RegroupMove.IsFinished) pair.RegroupMove.Interrupt();
                    logger.Info("Retained mounted restoration refused: rider=" + pair.RiderId + ";mount=" + pair.MountId +
                        ";boundary=" + (pair.StagingBoundary ?? Describe(retainedIntentKind)) + ";separation=" + separation.ToString("0.0") +
                        ";reason=" + reason + ".");
                    dropped.Add(DescribePair(rider, mount, pair) + " (" + reason + ")");
                    continue;
                }
                var resolved = new Dictionary<string, UnitEntityData>(StringComparer.Ordinal) { { pair.RiderId, rider }, { pair.MountId, mount } };
                string detail;
                var outcome = RestoreRetainedPair(pair, rider, mount, resolved, false, out detail);
                if (outcome == MountedRetainedRestoreOutcome.Restored)
                {
                    restored.Add(pair);
                    logger.Info("Retained mounted pair restored: rider=" + pair.RiderId + ";mount=" + pair.MountId + ";owner=" + pair.OwnerId +
                        "/" + pair.Generation + ";boundary=" + (pair.StagingBoundary ?? Describe(retainedIntentKind)) +
                        ";regroups=" + pair.RegroupAttempts + ".");
                    continue;
                }
                if (outcome == MountedRetainedRestoreOutcome.RetainedResidue)
                {
                    // Review finding 1: never reported as a clean dismount. The faulted relationship keeps its exact
                    // cleanup ownership (and its own retry); the latch blocks saving and new native participation.
                    retainedIntent = null;
                    ResetRetainedSettle();
                    LatchRetainedResidue(custody, DescribePair(rider, mount, pair), detail);
                    return;
                }
                // A clean refusal (for example the previous owner identity has not yet retired) is retried while the
                // bounded patience lasts; afterwards the cleanly dismounted pair is released.
                if (patienceExpired) dropped.Add(DescribePair(rider, mount, pair) + " (" + detail + ")");
                else remaining.Add(pair);
            }
            if (restored.Count != 0)
            {
                var ids = new HashSet<string>(restored.SelectMany(p => new[] { p.RiderId, p.MountId }), StringComparer.Ordinal);
                try { controls.RestorePersistentSlots(custody.Slots.Where(s => ids.Contains(s.ActorId)).ToArray()); }
                catch (Exception error) { logger.Exception("Mounted control slots were not restored after retained remount", error); }
            }
            RetainedRestoreCount += restored.Count;
            RetainedDropCount += dropped.Count;
            custody.Pairs = remaining.ToArray();
            if (remaining.Count == 0) { retainedIntent = null; ResetRetainedSettle(); }
            if (restored.Count != 0 || dropped.Count != 0)
                Report((restored.Count != 0 ? restored.Count + " mounted relationship" + (restored.Count == 1 ? "" : "s") +
                        " restored after " + Describe(retainedIntentKind) + " without Mount or preparation." : "") +
                    (dropped.Count != 0 ? (restored.Count != 0 ? " " : "") + "Stayed dismounted: " + string.Join(", ", dropped.ToArray()) +
                        ". Mount again when ready." : ""), dropped.Count == 0);
        }

        // Review findings 1 and 4: one restoration attempt for a retained pair through the established lifecycle. The
        // captured owner identity is imported (archive-native worlds), RestoreSavedPair re-attaches without Mount or
        // preparation, captured settled participation is restored, and the import is completed. A failed attempt is
        // classified from the relationship it left: only a fully clean dismount is a CleanRefusal.
        private MountedRetainedRestoreOutcome RestoreRetainedPair(AreaPair pair, UnitEntityData rider, UnitEntityData mount,
            Dictionary<string, UnitEntityData> resolved, bool turnBased, out string detail)
        {
            detail = null;
            GameMountedRelationshipService owner;
            var imported = false;
            try
            {
                if (ArchiveNativeEnabled)
                {
                    // Settled detached owners (this pair's own previous owner included) retire first, so the exact
                    // identity can be imported; an owner that still holds obligations refuses the import cleanly.
                    mountedWorld.ReleaseSettledAdmissions();
                    owner = mountedWorld.CreateRestoredBoundary().Relationship;
                    if (pair.OwnerId != null && pair.Generation > 0)
                    {
                        owner.ImportSavedOwnership(rider, mount, pair.OwnerId, pair.Generation, true);
                        imported = true;
                    }
                }
                else owner = relationship;
            }
            catch (Exception error)
            {
                // Nothing was attached: an import refusal leaves no owner, and the acquired empty boundary is released
                // by the ordinary settled-admission sweep.
                detail = error.Message;
                return MountedRetainedRestoreOutcome.CleanRefusal;
            }
            TransitionResult result = null;
            try
            {
                result = owner.RestoreSavedPair(rider, mount, pair.ProfileId);
                if (result.Succeeded)
                {
                    try
                    {
                        if (pair.Participation != null)
                            unifiedTurn.SnapshotParticipants.Single(p => ReferenceEquals(p.Relationship, owner))
                                .RestoreParticipation(pair.Participation, resolved, turnBased);
                        owner.CompleteSavedParticipationImport();
                        return MountedRetainedRestoreOutcome.Restored;
                    }
                    catch (Exception participation)
                    {
                        // The pair is attached but its participation import did not complete: never a clean state.
                        detail = "participation import failed after attachment: " + participation.Message;
                        return MountedRetainedRestoreOutcome.RetainedResidue;
                    }
                }
                detail = string.Join("; ", result.Errors);
            }
            catch (Exception error) { detail = error.Message; }
            if (imported) owner.CancelSavedOwnershipImport();
            var runtime = owner.Runtime;
            return MountedRetainedIntentPolicy.ClassifyRestore(false,
                result?.MovementAuthorityResidual ?? false, result?.PresentationResidual ?? false,
                owner.State == RelationshipState.Unmounted,
                runtime != null && runtime.NoPreparedPairResidue,
                runtime != null && !runtime.HasPresentationAttachmentResidue);
        }

        private bool retainedCombatHoldLogged;

        // Cutscene continuity repair: the scene left the mount behind within reach. The mount walks to its rider with an
        // ordinary native move (native pathfinding; never a teleport, never a player order). Out of combat only, at most
        // RegroupMaximumAttempts times, each bounded by RegroupDeadlineSeconds.
        private void StartRegroup(AreaPair pair, UnitEntityData rider, UnitEntityData mount, float separation, double gameSeconds)
        {
            pair.RegroupAttempts++;
            pair.RegroupStartedAt = gameSeconds;
            var move = new UnitMoveTo(rider.Position, MountedSceneContinuityPolicy.RegroupApproachRadiusMetres);
            try { mount.Commands.Run(move); }
            catch (Exception error) { logger.Exception("Mounted regroup move was not issued", error); }
            pair.RegroupMove = mount.Commands.Contains(move) ? move : null;
            SceneRegroupCount++;
            logger.Info("Retained mounted pair regroups natively: rider=" + pair.RiderId + ";mount=" + pair.MountId +
                ";separation=" + separation.ToString("0.0") + ";attempt=" + pair.RegroupAttempts + ";issued=" + (pair.RegroupMove != null) + ".");
        }

        private void InterruptRegroups(AreaTransfer custody, Game game, string reason)
        {
            foreach (var pair in custody.Pairs)
            {
                var move = pair.RegroupMove;
                pair.RegroupMove = null;
                if (move == null || move.IsFinished) continue;
                var mount = ResolveUnique(game, pair.MountId);
                if (mount != null && mount.Commands.Contains(move)) move.Interrupt();
                logger.Info("Retained mounted regroup stopped because " + reason + ": rider=" + pair.RiderId + ";mount=" + pair.MountId + ".");
            }
        }

        private void LatchRetainedResidue(AreaTransfer custody, string pair, string detail)
        {
            failedAreaTransfer = custody;
            areaRestoreFailure = "Retained mounted restoration left owned state for " + pair + " (" + detail + ").";
            Report(areaRestoreFailure + " Its cleanup ownership is retained; saving and new native participation remain blocked; load a valid save to resume.");
        }

        private static bool PermanentlyIneligible(MountedPairCandidate candidate) =>
            candidate != null && (!candidate.ExactReciprocalCompanionRelationship ||
                !candidate.RiderIsAliveAndConscious || !candidate.MountIsAliveAndConscious);

        // Cancels the retained intent; the affected pairs simply stay dismounted. Touches no native state except stopping
        // this custody's own regroup walk, if one is still running.
        internal void DropRetainedIntent(string reason)
        {
            var custody = retainedIntent;
            if (custody == null) return;
            try { InterruptRegroups(custody, Game.Instance, "the retained intent was cancelled"); }
            catch (Exception error) { logger.Exception("Mounted regroup was not stopped", error); }
            retainedIntent = null;
            ResetRetainedSettle();
            RetainedDropCount += custody.Pairs.Length;
            if (custody.Pairs.Length != 0)
                Report("Automatic remount cancelled because " + reason + "; " + custody.Pairs.Length + " pair" +
                    (custody.Pairs.Length == 1 ? " stays" : "s stay") + " dismounted. Mount again when ready.");
        }

        // Review finding 2: the retained (physically suspended) pairs of this world for the save header barrier, as
        // their own record. Pairs physically mounted at the barrier belong to the kmc-mounted-state member alone.
        // A record that cannot be encoded is left out (the pair then reloads dismounted) rather than failing the save.
        private string CaptureRetainedIntentRecord()
        {
            try
            {
                var game = Game.Instance;
                var player = game?.Player;
                var area = game?.CurrentlyLoadedArea;
                if (!Enabled || player == null || area == null) return null;
                // Second owner review finding 1: an in-flight transfer contributes its suspended rows only before its
                // arrival is attempted. Afterwards retainedIntent is the authoritative unresolved set: restored pairs are
                // physically mounted (kmc-mounted-state), dropped and retired pairs are not pending.
                var retained = retainedIntent != null && ReferenceEquals(retainedIntent.World.Target, player) &&
                    retainedIntent.CampaignId == player.GameId ? retainedIntent : null;
                var transfer = areaTransfer != null && ReferenceEquals(areaTransfer.World.Target, player) ? areaTransfer : null;
                var transferPending = transfer != null && !transfer.Lifecycle.ResumeAttempted;
                var rows = MountedRetainedIntentCapture.PendingRows(ToRecords(retained?.Pairs), ToRecords(transfer?.Pairs), !transferPending);
                var kind = MountedRetainedIntentCapture.Kind(retainedIntentKind, transferPending && transfer.Pairs.Length != 0,
                    transfer?.WorldMap ?? false, transfer?.CarriedKind ?? MountedRetainedIntentKind.WorldMapTravel);
                var mounted = new HashSet<string>(WorldRelationships.Where(r => r.State == RelationshipState.Mounted)
                    .SelectMany(r => new[] { r.Rider?.UniqueId, r.Mount?.UniqueId }).Where(id => id != null), StringComparer.Ordinal);
                var records = MountedRetainedIntentRecord.Recordable(rows, mounted);
                if (records.Length == 0) return null;
                // Second owner review finding 3: the retained pairs' own control rows travel with their intent.
                var slotRows = (retained?.Slots ?? new SavedMountedSlot[0]).Concat(transferPending ? transfer.Slots : new SavedMountedSlot[0])
                    .Select(s => new MountedRetainedIntentSlotRecord { ActorId = s.ActorId, Index = s.Index, Kind = s.Kind });
                var text = new MountedRetainedIntentRecord { CampaignId = player.GameId, AreaId = area.AssetGuidThreadSafe,
                    GameTimeTicks = game.TimeController.GameTime.Ticks, Kind = kind, Pairs = records,
                    Slots = MountedRetainedIntentRecord.RecordableSlots(slotRows, records) }.Encode();
                RetainedSavedCount++;
                return text;
            }
            catch (Exception error)
            {
                logger.Exception("Retained mounted intent was not written into this save", error);
                return null;
            }
        }

        private static IEnumerable<MountedRetainedIntentPairRecord> ToRecords(IEnumerable<AreaPair> pairs) =>
            (pairs ?? Enumerable.Empty<AreaPair>()).Select(p => new MountedRetainedIntentPairRecord {
                RiderId = p.RiderId, MountId = p.MountId, ProfileId = p.ProfileId, OwnerId = p.OwnerId, Generation = p.Generation });

        private void ClearPendingLoadedRetainedIntent() { pendingLoadedRetainedIntent = null; pendingLoadedRetainedSequence = -1; }

        // Read at load selection: accepted only for the selected campaign and area; adopted at native arrival.
        private void SelectRetainedIntentRecord(SaveInfo save)
        {
            ClearPendingLoadedRetainedIntent();
            try
            {
                var text = NativeMountedSaveStorage.ReadRetainedIntent(save.Saver);
                if (text == null) return;
                var record = MountedRetainedIntentRecord.Decode(text);
                if (record.CampaignId != save.GameId || record.AreaId != save.Area?.AssetGuidThreadSafe)
                {
                    logger.Warning("Saved retained mounted intent belongs to a different campaign or area; it is ignored.");
                    return;
                }
                pendingLoadedRetainedIntent = record;
                pendingLoadedRetainedSequence = loadSequence;
            }
            catch (Exception error) { logger.Exception("Saved retained mounted intent could not be read; the pairs load dismounted", error); }
        }

        // Native OnAreaLoaded of the selected load: the exact saved world (campaign, area and native game time) adopts
        // the record as retained intent. A world-map save keeps it until the next local arrival; a local save restores
        // it in ordinary play. A pair also present in the mounted-state member is never adopted twice.
        internal void AdoptLoadedRetainedIntent()
        {
            var record = pendingLoadedRetainedIntent;
            var sequence = pendingLoadedRetainedSequence;
            if (record == null) return;
            ClearPendingLoadedRetainedIntent();
            // Only the exact load that selected the record, still current, may adopt it.
            if (restoreLoad == null || restoreLoad.Sequence != sequence || loadSequence != sequence)
            {
                logger.Warning("Saved retained mounted intent belongs to a replaced or cancelled load; it is ignored.");
                return;
            }
            try
            {
                var game = Game.Instance;
                var player = game?.Player;
                if (!Enabled || player == null ||
                    !record.MatchesLoadedWorld(player.GameId, game.CurrentlyLoadedArea?.AssetGuidThreadSafe, game.TimeController.GameTime.Ticks))
                {
                    logger.Warning("Saved retained mounted intent does not match the loaded world; it is ignored.");
                    return;
                }
                var archived = new HashSet<string>((LoadedArchiveData?.Pairs ?? new SavedMountedPair[0])
                    .SelectMany(p => new[] { p?.Rider?.Id, p?.Mount?.Id }).Where(id => id != null), StringComparer.Ordinal);
                var pairs = record.Pairs.Where(p => !archived.Contains(p.RiderId) && !archived.Contains(p.MountId))
                    .Select(p => new AreaPair { RiderId = p.RiderId, MountId = p.MountId, ProfileId = p.ProfileId,
                        OwnerId = p.OwnerId, Generation = p.Generation, Carried = true }).ToArray();
                if (pairs.Length == 0) return;
                var adoptedActors = new HashSet<string>(pairs.SelectMany(p => new[] { p.RiderId, p.MountId }), StringComparer.Ordinal);
                retainedIntent = new AreaTransfer { World = new WeakReference(player), CampaignId = player.GameId, AreaId = record.AreaId,
                    Pairs = pairs, Slots = record.Slots.Where(s => adoptedActors.Contains(s.ActorId))
                        .Select(s => new SavedMountedSlot { ActorId = s.ActorId, Index = s.Index, Kind = s.Kind }).ToArray() };
                retainedIntentKind = record.Kind;
                ResetRetainedSettle();
                RetainedAdoptedCount++;
                Report(pairs.Length + " mounted relationship" + (pairs.Length == 1 ? "" : "s") + " saved during " + Describe(record.Kind) +
                    " will remount " + (record.Kind == MountedRetainedIntentKind.WorldMapTravel || IsWorldMapArea(game.CurrentlyLoadedArea)
                        ? "on arrival in the next area." : "when ordinary play resumes."), true);
            }
            catch (Exception error) { logger.Exception("Saved retained mounted intent was not adopted; the pairs stay dismounted", error); }
        }

        // A camp assembly (the native RestGroupCommand of the camp interaction) is still gathering the party.
        private static bool CampAssemblyPending(AreaTransfer custody, Game game)
        {
            if (custody == null || game?.State?.Units == null) return false;
            foreach (var pair in custody.Pairs)
                foreach (var id in new[] { pair.RiderId, pair.MountId })
                {
                    var unit = ResolveUnique(game, id);
                    var group = unit?.Commands?.GroupCommand;
                    if (group is RestGroupCommand && !group.IsFinished) return true;
                }
            return false;
        }

        private bool InAnyRelationship(UnitEntityData rider, UnitEntityData mount)
        {
            foreach (var r in WorldRelationships)
                if (r.State != RelationshipState.Unmounted &&
                    (ReferenceEquals(r.Rider, rider) || ReferenceEquals(r.Mount, mount) || ReferenceEquals(r.Rider, mount) || ReferenceEquals(r.Mount, rider)))
                    return true;
            return false;
        }

        private static UnitEntityData ResolveUnique(Game game, string id)
        {
            if (game?.State?.Units == null || game.Player == null || string.IsNullOrEmpty(id)) return null;
            var matches = game.State.Units.Concat(game.Player.AllCharacters).Where(u => u != null && u.UniqueId == id).Distinct().Take(2).ToArray();
            return matches.Length == 1 ? matches[0] : null;
        }

        internal static bool IsWorldMapArea(Kingmaker.Blueprints.Area.BlueprintArea area)
        {
            return area != null && MountedRetainedIntentPolicy.IsWorldMapArea(area.HasCustomUI,
                area.CustomUIScene != null ? area.CustomUIScene.SceneName : null, SceneName.CustomUIGlobalMap);
        }

        private static bool SameNavmeshArea(UnitEntityData rider, UnitEntityData mount)
        {
            try { return Kingmaker.View.ObstacleAnalyzer.GetArea(rider.Position) == Kingmaker.View.ObstacleAnalyzer.GetArea(mount.Position); }
            catch (Exception) { return false; }
        }

        // Review finding 4 (first review): patience counts only continuous settled ordinary play of the current custody.
        private void ResetRetainedSettle()
        {
            retainedSettledFrames = 0; retainedSettledSince = -1f;
            if (retainedIntent == null) return;
            foreach (var pair in retainedIntent.Pairs) { pair.EligibleSince = -1f; pair.StagingOwnedSince = -1d; }
        }

        private static string DescribePair(UnitEntityData rider, UnitEntityData mount, AreaPair pair) =>
            (rider?.CharacterName ?? pair.RiderId) + " and " + (mount?.CharacterName ?? pair.MountId);

        private static string Describe(MountedRetainedIntentKind kind)
        {
            switch (kind)
            {
                case MountedRetainedIntentKind.Camp: return "camping";
                case MountedRetainedIntentKind.WorldMapTravel: return "world-map travel";
                default: return "scene staging";
            }
        }
    }
}
