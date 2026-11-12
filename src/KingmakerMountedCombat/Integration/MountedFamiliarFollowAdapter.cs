using System;
using System.Linq;
using System.Reflection;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.View;
using Kingmaker.Visual.Critters;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Logging;
using Pathfinding;
using UnityEngine;

namespace KingmakerMountedCombat.Integration
{
    // Exploration repair P1/P2: the item familiar of a mounted rider. Bounded to the exact installed
    // Kingmaker.Visual.Critters.Familiar members (2.1.7b tokens below); if any is missing the adapter stays inert
    // and the native behaviour runs unchanged. It never spawns, hides, destroys or re-owns a familiar, and never
    // writes the rider's or the mount's position, heading, agent or commands.
    internal static class MountedFamiliarFollowAdapter
    {
        private const int MoveAgentFieldToken = 0x04000E2D;
        private const int StateFieldToken = 0x04000E2F;
        private const int DestinationFieldToken = 0x04000E30;
        private const int MoveToMethodToken = 0x060013C1;
        private const int MovingToMasterValue = 2;

        private static readonly FieldInfo MoveAgentField = ExactField("m_MoveAgent", MoveAgentFieldToken);
        private static readonly FieldInfo StateField = ExactField("m_State", StateFieldToken);
        private static readonly FieldInfo DestinationField = ExactField("m_Destination", DestinationFieldToken);
        private static readonly MethodInfo MoveToMethod = typeof(Familiar)
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .FirstOrDefault(m => m.Name == "MoveTo" && m.MetadataToken == MoveToMethodToken);
        private static readonly object MovingToMaster = StateField != null && StateField.FieldType.IsEnum &&
            string.Equals(Enum.GetName(StateField.FieldType, MovingToMasterValue), "MovingToMaster", StringComparison.Ordinal)
                ? Enum.ToObject(StateField.FieldType, MovingToMasterValue) : null;

        internal static bool Available => MoveAgentField != null && StateField != null && DestinationField != null &&
            MoveToMethod != null && MovingToMaster != null;

        internal static long FollowSubstitutionCount { get; private set; }
        internal static long AnchorReleaseCount { get; private set; }
        private static bool failureReported;

        // The exact mount the master is currently riding, or null (not a rider, not mounted, no live view).
        internal static UnitEntityData RiddenMount(UnitEntityData master, Func<UnitEntityData, GameMountedRelationshipService> owner)
        {
            if (master == null || owner == null) return null;
            var relationship = owner(master);
            if (relationship == null || relationship.State != RelationshipState.Mounted || !ReferenceEquals(relationship.Rider, master)) return null;
            var mount = relationship.Mount;
            return mount != null && mount.View != null && master.View != null ? mount : null;
        }

        // Postfix of native TeleportToMaster: the familiar was just parented to the master view's parent. For a mounted
        // rider that is the pair's owned anchor; move it to the mount view's container, keeping the native world pose.
        internal static void AfterTeleportToMaster(Familiar familiar, UnitEntityData mount, IModLogger logger)
        {
            try
            {
                if (familiar == null || mount == null) return;
                var master = familiar.Master;
                var parent = familiar.transform.parent;
                if (!MountedFamiliarFollowPolicy.MustLeaveOwnedAnchor(true, master?.View != null && parent == master.View.transform.parent)) return;
                familiar.transform.SetParent(mount.View.transform.parent, true);
                AnchorReleaseCount++;
            }
            catch (Exception error) { ReportOnce(logger, error); }
        }

        // Prefix of native UpdateMovingToMaster. Returns true when it ran the native rule with the mount as the
        // physical mover (the native method is then skipped); false leaves the native method to run unchanged.
        internal static bool TryUpdateMovingToMaster(Familiar familiar, UnitEntityData mount, IModLogger logger)
        {
            if (!Available || familiar == null || mount == null) return false;
            try
            {
                var agent = mount.View.AgentASP;
                var path = agent != null ? agent.Path : null;
                var complete = path != null && path.CompleteState == PathCompleteState.Complete && path.vectorPath != null && path.vectorPath.Count > 0;
                var point = MountedFamiliarFollowPolicy.Source(true, complete) == MountedFamiliarFollowSource.MountPathEnd
                    ? path.vectorPath[path.vectorPath.Count - 1]
                    : mount.Position;
                var destination = (Vector3?)DestinationField.GetValue(familiar);
                var following = Equals(StateField.GetValue(familiar), MovingToMaster);
                if (MountedFamiliarFollowPolicy.ShouldFollow(following, (point - (destination ?? familiar.transform.position)).magnitude))
                {
                    StateField.SetValue(familiar, MovingToMaster);
                    following = true;
                    MoveToMethod.Invoke(familiar, new object[] { point });
                }
                var own = MoveAgentField.GetValue(familiar) as UnitMovementAgent;
                var ownPath = own != null ? own.Path : null;
                if (following && ownPath != null &&
                    (ownPath.CompleteState == PathCompleteState.Error || ownPath.CompleteState == PathCompleteState.Partial))
                    familiar.TeleportToMaster(true);
                FollowSubstitutionCount++;
                return true;
            }
            catch (Exception error)
            {
                ReportOnce(logger, error);
                return false;
            }
        }

        private static FieldInfo ExactField(string name, int token)
        {
            var field = typeof(Familiar).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            return field != null && field.MetadataToken == token ? field : null;
        }

        private static void ReportOnce(IModLogger logger, Exception error)
        {
            if (failureReported) return;
            failureReported = true;
            logger?.Exception("Mounted familiar follow adapter left the native familiar behaviour in place", error);
        }
    }
}
