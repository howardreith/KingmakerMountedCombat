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
    // travel owns the actors, and the same eligible pair is re-attached afterwards through RestoreSavedPair: no
    // public Mount action, no preparation, no refund, no turn and no movement grant. The custody is bounded to one
    // world (Player instance and campaign) and is cancelled by loading, combat, mod teardown or invalidation.
    internal sealed partial class MountedPersistenceService
    {
        private AreaTransfer retainedIntent;
        private MountedRetainedIntentKind retainedIntentKind;
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

        // Called at a native event boundary (Dialog, locked Cutscene, Rest, or the camp interaction before its approach
        // orders are issued). Returns true when every mounted pair is now suspended with its intent retained (or when
        // nothing is mounted and an earlier suspension of this world already holds the intent). False leaves the
        // caller's existing clean-dismount path in charge.
        internal bool TrySuspendForEvent(MountedRetainedIntentKind kind, string source)
        {
            var world = Game.Instance?.Player;
            if (!Enabled || world == null) return false;
            var mounted = WorldRelationships.Where(r => r.State == RelationshipState.Mounted && r.Rider != null && r.Mount != null).ToArray();
            if (mounted.Length == 0) return retainedIntent != null && ReferenceEquals(retainedIntent.World.Target, world);
            if (world.IsInCombat || mounted.Any(r => r.Rider.IsInCombat || r.Mount.IsInCombat)) return false;
            if (areaTransfer != null || SaveDraining || SaveSuspended || HasSaveAdmissionFence || HasActiveSaveScope ||
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
                var pair = new AreaPair { RiderId = owner.Rider.UniqueId, MountId = owner.Mount.UniqueId, ProfileId = owner.Runtime.MountProfileId,
                    OwnerId = owner.PairOwner?.Id.ToString("N"), Generation = owner.PairOwner?.Generation ?? 0 };
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
                else if (kind == MountedRetainedIntentKind.Camp) retainedIntentKind = kind;
                var ids = new HashSet<string>(captured.SelectMany(p => new[] { p.RiderId, p.MountId }), StringComparer.Ordinal);
                retainedIntent.Pairs = retainedIntent.Pairs.Where(p => !ids.Contains(p.RiderId) && !ids.Contains(p.MountId)).Concat(captured).ToArray();
                retainedIntent.Slots = retainedIntent.Slots.Where(s => !ids.Contains(s.ActorId))
                    .Concat(slots.Where(s => ids.Contains(s.ActorId))).ToArray();
                ResetRetainedSettle();
                RetainedSuspensionCount++;
                Report(captured.Count + " mounted relationship" + (captured.Count == 1 ? "" : "s") + " suspended for " + Describe(kind) +
                    " (" + source + "); the pair remounts automatically when ordinary play resumes.", true);
            }
            return !failed;
        }

        // Ordinary-play restoration inside the current area. Polled from Update; the decision itself is pure policy.
        internal void TryRestoreRetainedIntent()
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
                areaTransfer != null || HasActiveSaveScope || SaveDraining || SaveSuspended || HasSaveAdmissionFence || presentationPending ||
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
                var permanent = candidate != null && (!candidate.ExactReciprocalCompanionRelationship ||
                    !candidate.RiderIsAliveAndConscious || !candidate.MountIsAliveAndConscious);
                var separation = rider != null && mount != null
                    ? new UnityEngine.Vector2(rider.Position.x - mount.Position.x, rider.Position.z - mount.Position.z).magnitude
                    : float.PositiveInfinity;
                var pairDecision = MountedRetainedIntentPolicy.DecidePair(
                    candidate != null,
                    candidate != null && SupportedMountedProfiles.Resolve(rider, mount)?.Id == pair.ProfileId,
                    candidate != null && InAnyRelationship(rider, mount),
                    permanent,
                    refusal,
                    separation,
                    MountedRetainedIntentPolicy.MaximumSeparationMetres(retainedIntentKind),
                    rider != null && mount != null && SameNavmeshArea(rider, mount),
                    patienceExpired);
                if (pairDecision == MountedRetainedPairDecision.Retry) { remaining.Add(pair); continue; }
                if (pairDecision == MountedRetainedPairDecision.Drop)
                {
                    dropped.Add(DescribePair(rider, mount, pair) + " (" + (refusal ?? (separation > MountedRetainedIntentPolicy.MaximumSeparationMetres(retainedIntentKind) ||
                        (rider != null && mount != null && !SameNavmeshArea(rider, mount))
                        ? "rider and mount were separated by the event" : "the pair is no longer eligible")) + ")");
                    continue;
                }
                try
                {
                    var owner = mountedWorld != null ? mountedWorld.GetOrCreateAdmission(rider, mount) : relationship;
                    var result = owner?.RestoreSavedPair(rider, mount, pair.ProfileId);
                    if (result == null || !result.Succeeded)
                    {
                        dropped.Add(DescribePair(rider, mount, pair) + " (" + (result == null ? "no pair boundary is available" : string.Join("; ", result.Errors)) + ")");
                        continue;
                    }
                    restored.Add(pair);
                }
                catch (Exception error)
                {
                    logger.Exception("Mounted retained-intent restoration left a pair dismounted", error);
                    dropped.Add(DescribePair(rider, mount, pair) + " (" + error.Message + ")");
                }
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
            if (remaining.Count == 0) { retainedIntent = null; ResetRetainedSettle(); retainedEligibleSince = -1f; }
            if (restored.Count != 0 || dropped.Count != 0)
                Report((restored.Count != 0 ? restored.Count + " mounted relationship" + (restored.Count == 1 ? "" : "s") +
                        " restored after " + Describe(retainedIntentKind) + " without Mount or preparation." : "") +
                    (dropped.Count != 0 ? (restored.Count != 0 ? " " : "") + "Stayed dismounted: " + string.Join(", ", dropped.ToArray()) +
                        ". Mount again when ready." : ""), dropped.Count == 0);
        }

        // Cancels the retained intent; the affected pairs simply stay dismounted. Never touches native state.
        internal void DropRetainedIntent(string reason)
        {
            var custody = retainedIntent;
            if (custody == null) return;
            retainedIntent = null;
            ResetRetainedSettle();
            retainedEligibleSince = -1f;
            RetainedDropCount += custody.Pairs.Length;
            if (custody.Pairs.Length != 0)
                Report("Automatic remount cancelled because " + reason + "; " + custody.Pairs.Length + " pair" +
                    (custody.Pairs.Length == 1 ? " stays" : "s stay") + " dismounted. Mount again when ready.");
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

        private void ResetRetainedSettle() { retainedSettledFrames = 0; retainedSettledSince = -1f; }

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
