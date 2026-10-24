using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Visual.CharacterSystem;
using KingmakerMountedCombat.Domain;
using UnityEngine;

namespace KingmakerMountedCombat.Integration
{
    // One pending schema-3 restoration, never an actor/rig cache used after it.
    // LateUpdate notifications observe existing native visual work only.
    internal sealed class NativeMountedPresentationReadiness
    {
        private sealed class Pair
        {
            internal string Id;
            internal UnitEntityData Rider, Mount;
            internal SupportedMountedProfile Profile;
            internal Character ObservedAvatar;
            internal object RiderView, MountView, Skeleton, RiderAnimator, AvatarAnimator;
            internal Transform Anchor;
            internal string SurfaceIdentity;
            internal MountedPresentationReadinessResult Observed;
        }
        private readonly Pair[] pairs;
        private readonly MountedPresentationReadiness policy;
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private string observationFailure;

        internal NativeMountedPresentationReadiness(MountedArchiveData source,
            IReadOnlyDictionary<string, UnitEntityData> actors, PresentationRestoreCustody custody)
        {
            pairs = source.Pairs.Select(row => new Pair { Id = row.PairId, Rider = actors[row.Rider.Id],
                Mount = actors[row.Mount.Id], Profile = SupportedMountedProfiles.ResolveProfileId(row.ProfileId) }).ToArray();
            if (pairs.Any(p => p.Profile == null)) throw new InvalidOperationException("Saved native rig profile is not declared.");
            policy = new MountedPresentationReadiness(custody, 0d);
        }

        internal void ObserveLateUpdate(Character avatar)
        {
            if (avatar == null || observationFailure != null) return;
            try
            {
                foreach (var pair in pairs)
                {
                    if (!ReferenceEquals(pair.Rider.View?.CharacterAvatar, avatar)) continue;
                    pair.Observed = Probe(pair, out var anchor, out var identity);
                    pair.ObservedAvatar = avatar; pair.RiderView = pair.Rider.View; pair.MountView = pair.Mount.View;
                    pair.Skeleton = avatar.Skeleton; pair.RiderAnimator = pair.Rider.View.Animator;
                    pair.AvatarAnimator = avatar.Animator; pair.Anchor = anchor; pair.SurfaceIdentity = identity;
                }
            }
            catch (Exception error) { observationFailure = error.GetType().FullName + ": " + error.Message; }
        }

        internal MountedPresentationReadinessResult Check(PresentationRestoreCustody custody)
        {
            var rows = new Dictionary<string, MountedPresentationReadinessResult>(StringComparer.Ordinal);
            foreach (var pair in pairs)
            {
                if (observationFailure != null) { rows.Add(pair.Id, MountedPresentationReadinessResult.Incompatible(observationFailure)); continue; }
                var current = Probe(pair, out var anchor, out var identity);
                // Missing or dirty surfaces are retryable. A new view/avatar must
                // complete its own native LateUpdate; an old observation cannot
                // admit a replacement whose native actor has the same stable ID.
                if (current.Kind == MountedPresentationReadinessKind.Waiting) { pair.Observed = null; rows.Add(pair.Id, current); continue; }
                var avatar = pair.Rider.View?.CharacterAvatar;
                if (pair.Observed == null || !ReferenceEquals(pair.ObservedAvatar, avatar) ||
                    !ReferenceEquals(pair.RiderView, pair.Rider.View) || !ReferenceEquals(pair.MountView, pair.Mount.View) ||
                    !ReferenceEquals(pair.Skeleton, avatar?.Skeleton) || !ReferenceEquals(pair.RiderAnimator, pair.Rider.View?.Animator) ||
                    !ReferenceEquals(pair.AvatarAnimator, avatar?.Animator) || !ReferenceEquals(pair.Anchor, anchor) || pair.SurfaceIdentity != identity)
                { rows.Add(pair.Id, MountedPresentationReadinessResult.Waiting("exact clean native Character.LateUpdate has not completed for these views")); continue; }
                // A body/equipment change after the observed LateUpdate must be
                // checked again, never admitted by the prior sample alone.
                rows.Add(pair.Id, current.Kind == MountedPresentationReadinessKind.Incompatible ? current : pair.Observed);
            }
            return policy.Check(clock.Elapsed.TotalSeconds, custody, rows);
        }

        private static MountedPresentationReadinessResult Probe(Pair pair, out Transform anchor, out string identity)
        {
            anchor = null; identity = null;
            var rider = pair.Rider; var mount = pair.Mount;
            if (rider?.View == null || mount?.View == null || rider.View.AgentASP == null || mount.View.AgentASP == null)
                return MountedPresentationReadinessResult.Waiting("native entity views and movement agents are incomplete");
            var avatar = rider.View.CharacterAvatar;
            if (avatar == null || avatar.Skeleton == null || avatar.Animator == null || rider.View.Animator == null || mount.View.Animator == null)
                return MountedPresentationReadinessResult.Waiting("native character, skeleton or animator is incomplete");
            var visual = avatar.EquipmentEntities?.Take(129).ToArray();
            if (visual == null || visual.Any(x => x == null) || avatar.IsDirty || avatar.Skeleton.IsDirty() || visual.Any(x => x.IsDirty()))
                return MountedPresentationReadinessResult.Waiting("native skeleton/equipment rebuild remains dirty");
            if (visual.Length > 128) return MountedPresentationReadinessResult.Incompatible("native equipment inventory exceeds the declared bound");
            // Capture visual identities before classifying a finished mismatch.
            // A rebuild can clear IsDirty in Update before this frame's native
            // LateUpdate. Changed equipment/bone objects must first pass that
            // boundary; an old sample cannot turn their transient absence into
            // permanent incompatibility.
            identity = SmallRiderBodyFingerprint.Capture(NativeSmallRiderBody.Capture(rider, mount)) +
                "|equipment=" + string.Join(",", visual.Select(x => x.GetInstanceID())) +
                "|rider=" + HierarchyIdentity(avatar.transform) + "|mount=" + HierarchyIdentity(mount.View.transform);
            // The callback has already returned from native Character.LateUpdate.
            // This branch only reads a complete surface; it never invokes Update,
            // LateUpdate, asset loading, skeleton setters or a rebuild itself.
            if (!MountedRiderPoseAdapter.TryValidateSupportedSurface(rider.View, pair.Profile.RiderPoseProfile, out var error))
                return MountedPresentationReadinessResult.Incompatible(error);
            var pending = new Stack<Transform>(); pending.Push(mount.View.transform);
            var visited = 0;
            while (pending.Count != 0)
            {
                var node = pending.Pop();
                if (++visited > 2048) return MountedPresentationReadinessResult.Incompatible("native mount view exceeds the bounded rig surface");
                if (string.Equals(node.name, pair.Profile.SourceAnchorName, StringComparison.Ordinal))
                {
                    if (anchor != null) return MountedPresentationReadinessResult.Incompatible("native mount seat anchor is ambiguous");
                    anchor = node;
                }
                for (var i = 0; i < node.childCount; i++)
                {
                    if (pending.Count + visited >= 2048) return MountedPresentationReadinessResult.Incompatible("native mount view exceeds the bounded rig surface");
                    pending.Push(node.GetChild(i));
                }
            }
            if (anchor == null) return MountedPresentationReadinessResult.Incompatible("native mount seat anchor is absent");
            if (SupportedMountedProfiles.Resolve(rider, mount)?.Id != pair.Profile.Id)
                return MountedPresentationReadinessResult.Incompatible("finished native body, equipment or profile differs from the saved declaration");
            if (!ReferenceEquals(rider.Descriptor.Pet, mount) || !ReferenceEquals(mount.Descriptor.Master.Value, rider))
                return MountedPresentationReadinessResult.Incompatible("native reciprocal companion ownership changed");
            return MountedPresentationReadinessResult.Ready();
        }

        private static string HierarchyIdentity(Transform root)
        {
            var text = new StringBuilder(); var pending = new Stack<Transform>(); pending.Push(root); var visited = 0;
            while (pending.Count != 0)
            {
                var node = pending.Pop();
                if (++visited > 2048 || node.name.Length > 256) throw new InvalidOperationException("Native visual hierarchy exceeds the bounded identity surface.");
                text.Append(node.GetInstanceID()).Append(':').Append(node.parent == null ? 0 : node.parent.GetInstanceID())
                    .Append(':').Append(node.name.Length).Append(':').Append(node.name).Append(';');
                for (var i = 0; i < node.childCount; i++)
                {
                    if (pending.Count + visited >= 2048) throw new InvalidOperationException("Native visual hierarchy exceeds the bounded identity surface.");
                    pending.Push(node.GetChild(i));
                }
            }
            return text.ToString();
        }
    }
}
