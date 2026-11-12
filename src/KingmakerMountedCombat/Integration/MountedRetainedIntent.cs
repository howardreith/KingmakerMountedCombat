using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.GameModes;
using Kingmaker.UnitLogic.Commands;
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
        private float retainedEligibleSince = -1f;
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
            // An area arrival that already restored (an arrival cutscene follows OnAreaLoaded) no longer owns the pairs.
            if ((areaTransfer != null && !areaTransfer.Lifecycle.ResumeAttempted) || SaveDraining || SaveSuspended || HasSaveAdmissionFence || HasActiveSaveScope ||
                presentationPending || CombatSemanticRestorationPending || HasAreaParticipationObligations() ||
                !DrainWorldCharge("scripted event suspension"))
                return false;
            if (retainedIntent != null && (!ReferenceEquals(retainedIntent.World.Target, world) || retainedIntent.CampaignId != world.GameId))
                DropRetainedIntent("a different world");
            var slots = controls.CapturePersistentSlots();
            var captured = new List<AreaPair>();
            var failed = false;
            foreach (var owner in mounted)
            {
                // The exact owner identity and the settled participation are captured as the area transfer captures them.
                var pair = new AreaPair { RiderId = owner.Rider.UniqueId, MountId = owner.Mount.UniqueId, ProfileId = owner.Runtime.MountProfileId,
                    OwnerId = owner.PairOwner?.Id.ToString("N"), Generation = owner.PairOwner?.Generation ?? 0, Carried = true,
                    Participation = unifiedTurn.SnapshotParticipants.SingleOrDefault(p => ReferenceEquals(p.Relationship, owner))?.CaptureParticipation() };
                try
                {
                    if (owner.GuardBoundary(CleanupTrigger.EventSuspension)) captured.Add(pair);
                    else failed = true;
                }
                catch (Exception error) { failed = true; logger.Exception("Mounted event suspension retained a pair", error); }
            }
            if (captured.Count != 0)
            {
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
                Report(captured.Count + " mounted relationship" + (captured.Count == 1 ? "" : "s") + " suspended for " + Describe(retainedIntentKind) +
                    " (" + source + "); the pair remounts automatically when ordinary play resumes.", true);
            }
            return !failed;
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
            if (decision == MountedRetainedIntentDecision.Hold) { ResetRetainedSettle(); return; }
            var now = UnityEngine.Time.unscaledTime;
            if (retainedSettledSince < 0f) retainedSettledSince = now;
            if (++retainedSettledFrames < RetainedSettleFrames || now - retainedSettledSince < RetainedSettleSeconds) return;
            if (retainedEligibleSince < 0f) retainedEligibleSince = now;
            var patienceExpired = now - retainedEligibleSince > MountedRetainedIntentPolicy.EligibilityPatienceSeconds;

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
                var pairDecision = MountedRetainedIntentPolicy.DecidePair(
                    candidate != null,
                    candidate != null && SupportedMountedProfiles.Resolve(rider, mount)?.Id == pair.ProfileId,
                    candidate != null && InAnyRelationship(rider, mount),
                    PermanentlyIneligible(candidate),
                    refusal,
                    separation,
                    MountedRetainedIntentPolicy.MaximumSeparationMetres(retainedIntentKind),
                    sameArea,
                    patienceExpired);
                if (pairDecision == MountedRetainedPairDecision.Retry) { remaining.Add(pair); continue; }
                if (pairDecision == MountedRetainedPairDecision.Drop)
                {
                    dropped.Add(DescribePair(rider, mount, pair) + " (" + (refusal ?? (candidate != null &&
                        (separation > MountedRetainedIntentPolicy.MaximumSeparationMetres(retainedIntentKind) || !sameArea)
                        ? "rider and mount were separated by the event" : "the pair is no longer eligible")) + ")");
                    continue;
                }
                var resolved = new Dictionary<string, UnitEntityData>(StringComparer.Ordinal) { { pair.RiderId, rider }, { pair.MountId, mount } };
                string detail;
                var outcome = RestoreRetainedPair(pair, rider, mount, resolved, false, out detail);
                if (outcome == MountedRetainedRestoreOutcome.Restored) { restored.Add(pair); continue; }
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

        private void LatchRetainedResidue(AreaTransfer custody, string pair, string detail)
        {
            failedAreaTransfer = custody;
            areaRestoreFailure = "Retained mounted restoration left owned state for " + pair + " (" + detail + ").";
            Report(areaRestoreFailure + " Its cleanup ownership is retained; saving and new native participation remain blocked; load a valid save to resume.");
        }

        private static bool PermanentlyIneligible(MountedPairCandidate candidate) =>
            candidate != null && (!candidate.ExactReciprocalCompanionRelationship ||
                !candidate.RiderIsAliveAndConscious || !candidate.MountIsAliveAndConscious);

        // Cancels the retained intent; the affected pairs simply stay dismounted. Never touches native state.
        internal void DropRetainedIntent(string reason)
        {
            var custody = retainedIntent;
            if (custody == null) return;
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
        private void ResetRetainedSettle() { retainedSettledFrames = 0; retainedSettledSince = -1f; retainedEligibleSince = -1f; }

        private static string DescribePair(UnitEntityData rider, UnitEntityData mount, AreaPair pair) =>
            (rider?.CharacterName ?? pair.RiderId) + " and " + (mount?.CharacterName ?? pair.MountId);

        private static string Describe(MountedRetainedIntentKind kind)
        {
            switch (kind)
            {
                case MountedRetainedIntentKind.Camp: return "camping";
                case MountedRetainedIntentKind.WorldMapTravel: return "world-map travel";
                default: return "a scripted event";
            }
        }
    }
}
