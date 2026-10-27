using System;
using System.Linq;
using System.Reflection;
using Harmony12;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Installed explicitly before the first native load. Read-only boundary
    // receipts distinguish engine world disposal from ordinary area suspension.
    internal sealed class Chunk7WorldRetentionProbe : IDisposable
    {
        private const string HarmonyId = "KingmakerMountedCombat.Diagnostics.Chunk7WorldRetention";
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private const int MaximumEvents = 24;
        private static Chunk7WorldRetentionProbe active;
        private readonly MountedPersistenceService persistence;
        private readonly MountedWorldService world;
        private readonly NativeMountedControlService controls;
        private readonly RetainedObjectIdentity identities;
        private readonly WeakObjectSerials subscriptionSerials;
        private readonly JArray events = new JArray(), hooks = new JArray();
        private UnitEntityData[] actors = new UnitEntityData[0];
        private HarmonyInstance harmony;
        private bool attempted, installed, disposed;
        private int errors, dropped;
        internal bool Complete => installed && errors == 0 && dropped == 0;
        internal Chunk7WorldRetentionProbe(MountedPersistenceService persistence, MountedWorldService world, NativeMountedControlService controls, RetainedObjectIdentity identities,
            WeakObjectSerials subscriptionSerials = null)
        { this.persistence = persistence; this.world = world; this.controls = controls; this.identities = identities; this.subscriptionSerials = subscriptionSerials; }
        internal void Install()
        {
            if (attempted || disposed || active != null || persistence == null || world == null || controls == null || identities == null || Game.Instance?.CurrentlyLoadedArea != null)
                throw new InvalidOperationException("Repeated-world observer must be uniquely owned before native load.");
            attempted = true; harmony = HarmonyInstance.Create(HarmonyId); active = this;
            Patch("PrepareForNativeWorldDisposal", "DisposalBefore", "DisposalAfter");
            Patch("SuspendAreaPair", "SuspendBefore", "SuspendAfter");
            Patch("RestoreAreaPair", "ResumeBefore", "ResumeAfter");
            installed = true;
        }
        internal void BindActors(UnitEntityData[] value)
        {
            if (!Complete || disposed || value == null || value.Length != 4 || value.Any(a => a == null) || value.Select(a => a.UniqueId).Distinct().Count() != 4)
                throw new InvalidOperationException("Repeated-world observation needs four exact actors.");
            actors = value.ToArray();
        }
        private void Patch(string name, string prefix, string postfix)
        {
            var method = typeof(MountedPersistenceService).GetMethod(name, Flags);
            if (method == null) throw new MissingMethodException(typeof(MountedPersistenceService).FullName, name);
            harmony.Patch(method, new HarmonyMethod(typeof(Hooks).GetMethod(prefix, Flags)) { prioritiy = Priority.First },
                new HarmonyMethod(typeof(Hooks).GetMethod(postfix, Flags)) { prioritiy = Priority.Last });
            hooks.Add(new JObject { ["method"] = name, ["declaringType"] = method.DeclaringType.FullName,
                ["token"] = method.MetadataToken.ToString("X8"), ["moduleMvid"] = method.Module.ModuleVersionId.ToString() });
        }
        private void Record(string kind, int before, int after)
        {
            try
            {
                if (events.Count >= MaximumEvents) { dropped++; return; }
                events.Add(new JObject { ["kind"] = kind, ["before"] = before, ["after"] = after,
                    ["state"] = CaptureWorld(world, controls, persistence, actors, identities, subscriptionSerials) });
            }
            catch { errors++; }
        }
        internal JObject Capture() => new JObject { ["contract"] = "chunk7-repeated-world-retention-v1", ["complete"] = Complete,
            ["installedBeforeWorldLoad"] = installed, ["maximumEvents"] = MaximumEvents, ["errors"] = errors,
            ["dropped"] = dropped, ["hooks"] = hooks.DeepClone(), ["events"] = events.DeepClone() };
        internal static JObject CaptureWorld(MountedWorldService world, NativeMountedControlService controls,
            MountedPersistenceService persistence, UnitEntityData[] actors, RetainedObjectIdentity identities,
            WeakObjectSerials subscriptionSerials = null)
        {
            var game = Game.Instance;
            return new JObject {
                ["frame"] = Time.frameCount, ["gameTicks"] = game?.TimeController?.GameTime.Ticks,
                ["gameObject"] = identities.Get(game), ["playerObject"] = identities.Get(game?.Player), ["stateObject"] = identities.Get(game?.State),
                ["areaStateObject"] = identities.Get(game?.State?.LoadedAreaState), ["mainStateObject"] = identities.Get(game?.State?.LoadedAreaState?.MainState),
                ["area"] = game?.CurrentlyLoadedArea?.AssetGuidThreadSafe, ["playerInCombat"] = game?.Player?.IsInCombat,
                ["boundaries"] = world.BoundaryCount, ["participants"] = world.UnifiedTurn.SnapshotParticipants.Length,
                ["failedConstructions"] = world.FailedConstructionCount, ["allocationActors"] = world.UnifiedTurn.TrackedAllocationActorCount,
                ["active"] = world.Registry.ActiveCount, ["owners"] = new JArray(world.Registry.Owners.Select(o => new JObject {
                    ["ownerId"] = o.Id.ToString("N"), ["generation"] = o.Generation, ["riderId"] = o.RiderId, ["mountId"] = o.MountId,
                    ["state"] = o.State.ToString(), ["retainsObligations"] = o.RetainsObligations() })),
                ["preparedOrViewResidue"] = world.Pairs.Count(p => !p.Relationship.Runtime.NoPreparedPairResidue),
                ["liveControlBoundaries"] = world.Pairs.Count(p => controls.HasUnsettledShellFor(p.Relationship)),
                ["commandOwners"] = world.Pairs.Count(p => p.Combat.HasActiveCommand || p.Combat.HasActiveGroundMovement || p.Combat.HasChargeOwnership),
                ["unsettledPreparation"] = world.UnifiedTurn.HasUnsettledPreparation, ["relationshipTransition"] = controls.HasUnsettledRelationshipTransition,
                ["serializationBusy"] = world.SerializationBusy, ["pendingWrites"] = NativePersistenceIsolation.HasPendingWrites,
                ["saveScope"] = persistence.HasActiveSaveScope, ["saveFence"] = persistence.HasSaveAdmissionFence,
                ["nativeDisposals"] = persistence.NativeWorldDisposalCount, ["areaSuspensions"] = persistence.AreaSuspensionCount,
                ["areaResumes"] = persistence.AreaResumeCount, ["semanticRestores"] = persistence.SemanticRestoreCount,
                ["presentationRestores"] = persistence.PresentationRestoreCount, ["nativePostLoads"] = persistence.ArchivePostLoadObservedCount,
                ["nativeCastRequests"] = controls.NativeCastRequestCount, ["snapshotCount"] = persistence.SnapshotCount,
                ["duplicateFacts"] = controls.CaptureSnapshot().DuplicateFactCount,
                ["nativeSlots"] = JArray.FromObject(controls.CapturePersistentSlots(), MountedSaveCodec.CreateSerializer()),
                ["actors"] = new JArray((actors ?? new UnitEntityData[0]).Select(a => new JObject {
                    ["id"] = a.UniqueId, ["object"] = identities.Get(a), ["descriptor"] = identities.Get(a.Descriptor),
                    ["view"] = a.View == null ? 0 : a.View.GetInstanceID(), ["viewObject"] = identities.Get(a.View), ["inState"] = a.IsInState,
                    ["commandsEmpty"] = a.Commands != null && a.Commands.Empty,
                    ["native"] = JObject.FromObject(MountedPersistenceService.CaptureActor(a), MountedSaveCodec.CreateSerializer()),
                    ["overrides"] = new JArray(a.View == null ? new int[0] : a.View.GetComponents<RiderMovementAgent>().Select(m => m.GetInstanceID()).ToArray()),
                    ["agentOverride"] = a.View?.AgentOverride == null ? 0 : a.View.AgentOverride.GetInstanceID() })),
                ["subscriptions"] = Chunk4SubscriptionSnapshot.Capture(subscriptionSerials)
            };
        }
        public void Dispose()
        {
            if (disposed) return;
            harmony?.UnpatchAll(HarmonyId);
            if (ReferenceEquals(active, this)) active = null;
            actors = new UnitEntityData[0]; disposed = true;
        }
        private static class Hooks
        {
            internal static void DisposalBefore(MountedPersistenceService __instance, out int __state) =>
                __state = ReferenceEquals(active?.persistence, __instance) ? __instance.NativeWorldDisposalCount : -1;
            internal static void DisposalAfter(MountedPersistenceService __instance, bool __result, int __state)
            { if (__result && __state >= 0 && ReferenceEquals(active?.persistence, __instance) && __instance.NativeWorldDisposalCount != __state)
                active.Record("native-world-cleanup-before-disposal", __state, __instance.NativeWorldDisposalCount); }
            internal static void SuspendBefore(MountedPersistenceService __instance, out int __state) =>
                __state = ReferenceEquals(active?.persistence, __instance) ? __instance.AreaSuspensionCount : -1;
            internal static void SuspendAfter(MountedPersistenceService __instance, bool __result, int __state)
            { if (__result && __state >= 0 && ReferenceEquals(active?.persistence, __instance) && __instance.AreaSuspensionCount != __state)
                active.Record("area-suspended", __state, __instance.AreaSuspensionCount); }
            internal static void ResumeBefore(MountedPersistenceService __instance, out int __state) =>
                __state = ReferenceEquals(active?.persistence, __instance) ? __instance.AreaResumeCount : -1;
            internal static void ResumeAfter(MountedPersistenceService __instance, int __state)
            { if (__state >= 0 && ReferenceEquals(active?.persistence, __instance) && __instance.AreaResumeCount != __state)
                active.Record("area-restored", __state, __instance.AreaResumeCount); }
        }
    }
}
