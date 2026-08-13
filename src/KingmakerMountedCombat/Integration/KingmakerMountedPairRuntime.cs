using System;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.GameModes;
using Kingmaker.View;
using KingmakerMountedCombat.Diagnostics;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Logging;
using UnityEngine;

namespace KingmakerMountedCombat.Integration
{
    internal sealed class KingmakerMountedPairRuntime : IMountedPairRuntime
    {
        internal const string MammothBlueprintGuid = "e7aa96d15a45238438ae4cfb476f6bb9";

        private readonly IModLogger logger;
        private readonly DiagnosticSettings settings;
        private UnitEntityData rider;
        private UnitEntityData mount;
        private UnitEntityView riderView;
        private UnitEntityView mountView;
        private UnitMovementAgent riderStockAgent;
        private bool riderStockAgentWasEnabled;
        private bool avoidanceLeaseOwned;
        private RiderMovementAgent riderOverride;
        private bool overrideComponentOwned;
        private bool overrideInstalled;
        private bool movementAuthorityConfigured;
        private Transform anchor;
        private bool presentationConfigured;
        private Vector3 preMountRiderPosition;
        private float preMountRiderOrientation;

        public KingmakerMountedPairRuntime(IModLogger logger, DiagnosticSettings settings)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public UnitEntityData Rider => rider;

        public UnitEntityData Mount => mount;

        public RiderMovementAgent MovementAgent => riderOverride;

        public void Prepare(UnitEntityData riderUnit, UnitEntityData mountUnit)
        {
            if (rider != null || mount != null)
            {
                throw new InvalidOperationException("A game pair is already prepared.");
            }

            rider = riderUnit ?? throw new ArgumentNullException(nameof(riderUnit));
            mount = mountUnit ?? throw new ArgumentNullException(nameof(mountUnit));
        }

        public MountedPairCandidate CreateCandidate()
        {
            if (rider == null || mount == null)
            {
                return null;
            }

            return CreateCandidate(rider, mount);
        }

        internal static MountedPairCandidate CreateCandidate(UnitEntityData riderUnit, UnitEntityData mountUnit)
        {
            if (riderUnit == null || mountUnit == null)
            {
                return null;
            }

            var riderState = riderUnit.Descriptor?.State;
            var mountState = mountUnit.Descriptor?.State;
            var exactCompanion = riderUnit.Descriptor?.Pet == mountUnit && mountUnit.Descriptor?.Master.Value == riderUnit && mountUnit.Descriptor.IsPet;
            var exactMammoth = mountUnit.Blueprint != null && string.Equals(mountUnit.Blueprint.AssetGuid, MammothBlueprintGuid, StringComparison.Ordinal);
            return new MountedPairCandidate(riderUnit.UniqueId, mountUnit.UniqueId)
            {
                RiderIsDirectlyControllable = riderUnit.IsInGame && riderUnit.IsDirectlyControllable,
                MountIsDirectlyControllable = mountUnit.IsInGame && mountUnit.IsDirectlyControllable,
                RiderIsAliveAndConscious = riderState != null && riderState.IsConscious && !riderState.IsFinallyDead,
                MountIsAliveAndConscious = mountState != null && mountState.IsConscious && !mountState.IsFinallyDead,
                ExactReciprocalCompanionRelationship = exactCompanion && exactMammoth,
                RiderIsInCombat = riderUnit.IsInCombat,
                MountIsInCombat = mountUnit.IsInCombat,
                PartyIsInCombat = Game.Instance?.Player?.IsInCombat ?? false,
                RiderSizeOrdinal = riderState == null ? int.MaxValue : (int)riderState.Size,
                MountSizeOrdinal = mountState == null ? int.MinValue : (int)mountState.Size,
                RiderViewAndStockAgentAvailable = riderUnit.View != null && riderUnit.View.AgentASP != null,
                MountViewAndStockAgentAvailable = mountUnit.View != null && mountUnit.View.AgentASP != null,
                RiderStockAgentEnabled = riderUnit.View != null && riderUnit.View.AgentASP != null && riderUnit.View.AgentASP.enabled,
                MountStockAgentEnabled = mountUnit.View != null && mountUnit.View.AgentASP != null && mountUnit.View.AgentASP.enabled,
                RiderAgentOverrideAvailable = riderUnit.View != null && riderUnit.View.AgentOverride == null,
                MountAgentOverrideAvailable = mountUnit.View != null && mountUnit.View.AgentOverride == null,
                RiderIsExactlyMedium = riderState != null && (int)riderState.Size == 4,
                SafeMovementMode = Game.Instance != null && IsSafeMovementMode(Game.Instance.CurrentMode)
            };
        }

        public string ValidateMountedInvariants()
        {
            if (rider == null || mount == null || riderView == null || mountView == null)
            {
                return "Prepared pair or exact mounted views are missing.";
            }

            var riderState = rider.Descriptor?.State;
            var mountState = mount.Descriptor?.State;
            if (rider.View != riderView || mount.View != mountView || riderView.EntityData != rider || mountView.EntityData != mount)
            {
                return "A mounted unit view was detached or replaced.";
            }

            if (!riderView.gameObject.activeInHierarchy || !mountView.gameObject.activeInHierarchy)
            {
                return "A mounted unit view is no longer active.";
            }

            if (rider.Descriptor?.Pet != mount || mount.Descriptor?.Master.Value != rider || !mount.Descriptor.IsPet)
            {
                return "The reciprocal active-companion relationship changed.";
            }

            if (mount.Blueprint == null || !string.Equals(mount.Blueprint.AssetGuid, MammothBlueprintGuid, StringComparison.Ordinal))
            {
                return "The active companion is no longer the selected Mammoth blueprint.";
            }

            if (!rider.IsInGame || !mount.IsInGame || !rider.IsDirectlyControllable || !mount.IsDirectlyControllable ||
                riderState == null || mountState == null || !riderState.IsConscious || !mountState.IsConscious ||
                riderState.IsFinallyDead || mountState.IsFinallyDead)
            {
                return "Mounted controllability or life-state invariant changed.";
            }

            if ((int)riderState.Size != 4 || (int)mountState.Size <= (int)riderState.Size)
            {
                return "Mounted current-size invariant changed.";
            }

            if (rider.IsInCombat || mount.IsInCombat || (Game.Instance?.Player?.IsInCombat ?? false) ||
                Game.Instance == null || !IsSafeMovementMode(Game.Instance.CurrentMode))
            {
                return "Mounted combat or game-mode boundary was crossed.";
            }

            if (!movementAuthorityConfigured || riderView.AgentASP != riderStockAgent || riderStockAgent == null ||
                riderStockAgent.enabled || !avoidanceLeaseOwned || riderOverride == null || !overrideInstalled ||
                riderView.AgentOverride != riderOverride)
            {
                return "Owned rider movement-authority state no longer matches the mounted invariant.";
            }

            if (mountView.AgentASP == null || !mountView.AgentASP.enabled || mountView.AgentOverride != null)
            {
                return "The authoritative mount movement agent changed.";
            }

            if (!presentationConfigured || anchor == null)
            {
                return "The Mammoth position-anchor presentation is unavailable.";
            }

            return null;
        }

        public void AcquireMovementAuthority(MountedPair pair)
        {
            RequirePreparedPair(pair);
            riderView = rider.View;
            mountView = mount.View;
            riderStockAgent = riderView.AgentASP;
            riderStockAgentWasEnabled = riderStockAgent.enabled;
            preMountRiderPosition = rider.Position;
            preMountRiderOrientation = rider.Orientation;
            movementAuthorityConfigured = true;
            rider.Commands.InterruptMove();
            riderView.StopMoving();
            mount.Commands.InterruptMove();
            mountView.StopMoving();

            riderStockAgent.AvoidanceDisabled = true;
            avoidanceLeaseOwned = true;
            riderStockAgent.enabled = false;

            riderOverride = riderView.gameObject.AddComponent<RiderMovementAgent>();
            overrideComponentOwned = true;
            riderOverride.Init(riderView.gameObject);
            riderView.AgentOverride = riderOverride;
            overrideInstalled = true;
            logger.Info("Movement authority acquired: mount AgentASP authoritative; rider AgentASP stopped, avoidance-leased, and disabled.");
        }

        public void AttachPresentation(MountedPair pair)
        {
            RequirePreparedPair(pair);
            anchor = FindTransform(mountView.transform, "Spine");
            if (anchor == null)
            {
                throw new InvalidOperationException("Selected Mammoth view has no exact Spine transform.");
            }

            riderOverride.Configure(mount, anchor, new Vector3(settings.RiderOffsetX, settings.RiderOffsetY, settings.RiderOffsetZ), new Vector3(0f, settings.RiderYawDegrees, 0f));
            presentationConfigured = true;
            logger.Info("Rider presentation synchronized to Mammoth Spine diagnostic anchor.");
        }

        public void RestorePresentation(MountedPair pair)
        {
            if (riderOverride != null)
            {
                riderOverride.Deconfigure();
            }
            presentationConfigured = false;
            anchor = null;
            TryReleasePreparedReferences();
        }

        public void RestoreMovementAuthority(MountedPair pair, CleanupTrigger trigger)
        {
            Exception first = null;
            try
            {
                mount?.Commands?.InterruptMove();
                rider?.Commands?.InterruptMove();
                if (mountView != null) { mountView.StopMoving(); }
                if (riderView != null) { riderView.StopMoving(); }
            }
            catch (Exception exception)
            {
                first = exception;
            }

            try
            {
                if (riderOverride == null)
                {
                    overrideInstalled = false;
                    overrideComponentOwned = false;
                }
                else if (overrideInstalled && riderView != null && riderView.AgentOverride == riderOverride)
                {
                    riderView.AgentOverride = null;
                    if (riderView.AgentOverride == riderOverride)
                    {
                        throw new InvalidOperationException("Owned rider AgentOverride remained installed after clear.");
                    }
                    overrideInstalled = false;
                    overrideComponentOwned = false;
                }
                else if (overrideInstalled && (riderView == null || riderView.AgentOverride == null))
                {
                    overrideInstalled = false;
                    overrideComponentOwned = false;
                }
                else if (overrideInstalled)
                {
                    throw new InvalidOperationException("Rider AgentOverride ownership changed before cleanup.");
                }

                if (!overrideInstalled && overrideComponentOwned && riderOverride != null)
                {
                    UnityEngine.Object.Destroy(riderOverride);
                    overrideComponentOwned = false;
                }
            }
            catch (Exception exception)
            {
                first = first ?? exception;
            }

            try
            {
                if (riderStockAgent != null)
                {
                    riderStockAgent.enabled = riderStockAgentWasEnabled;
                }
            }
            catch (Exception exception)
            {
                first = first ?? exception;
            }

            try
            {
                if (avoidanceLeaseOwned)
                {
                    if (riderStockAgent != null)
                    {
                        riderStockAgent.AvoidanceDisabled = false;
                    }
                    avoidanceLeaseOwned = false;
                }
            }
            catch (Exception exception)
            {
                first = first ?? exception;
            }

            try
            {
                if (ShouldPlaceRiderAfterCleanup(trigger) && rider != null && mount != null && riderView != null && mountView != null)
                {
                    var radius = Math.Max(rider.Corpulence, mount.Corpulence) + 0.25f;
                    if (global::AstarPath.active != null)
                    {
                        FreePlaceSelector.PlaceSpawnPlaces(2, radius, mount.Position);
                        rider.Translocate(FreePlaceSelector.GetRelaxedPosition(1, true), mount.Orientation);
                    }
                    else
                    {
                        var side = Quaternion.Euler(0f, mount.Orientation, 0f) * Vector3.right * radius;
                        rider.Translocate(mount.Position + side, mount.Orientation);
                    }
                }
                else if (ShouldPlaceRiderAfterCleanup(trigger) && rider != null && riderView != null)
                {
                    rider.Translocate(preMountRiderPosition, preMountRiderOrientation);
                }
            }
            catch (Exception exception)
            {
                first = first ?? exception;
            }

            if (first != null)
            {
                throw new InvalidOperationException("Best-effort movement-authority restoration encountered residue.", first);
            }

            movementAuthorityConfigured = false;
            riderOverride = null;
            riderStockAgent = null;
            TryReleasePreparedReferences();
        }

        public void CancelMountMovement()
        {
            mount?.Commands?.InterruptMove();
            mountView?.StopMoving();
        }

        public void ClearPreparedPairWhenUnmounted()
        {
            if (!movementAuthorityConfigured && !presentationConfigured && !avoidanceLeaseOwned && !overrideInstalled && !overrideComponentOwned)
            {
                TryReleasePreparedReferences();
            }
        }

        private void TryReleasePreparedReferences()
        {
            if (movementAuthorityConfigured || presentationConfigured || avoidanceLeaseOwned || overrideInstalled || overrideComponentOwned)
            {
                return;
            }

            riderOverride = null;
            riderStockAgent = null;
            riderView = null;
            mountView = null;
            rider = null;
            mount = null;
        }

        private void RequirePreparedPair(MountedPair pair)
        {
            if (pair == null || rider == null || mount == null || !string.Equals(pair.RiderId, rider.UniqueId, StringComparison.Ordinal) || !string.Equals(pair.MountId, mount.UniqueId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Runtime pair does not match the validated domain pair.");
            }
        }

        private static Transform FindTransform(Transform current, string exactName)
        {
            if (current == null)
            {
                return null;
            }

            if (string.Equals(current.name, exactName, StringComparison.Ordinal))
            {
                return current;
            }

            for (var index = 0; index < current.childCount; index++)
            {
                var found = FindTransform(current.GetChild(index), exactName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static bool IsSafeMovementMode(GameModeType mode)
        {
            return mode == GameModeType.Default || mode == GameModeType.Pause;
        }

        private static bool ShouldPlaceRiderAfterCleanup(CleanupTrigger trigger)
        {
            return trigger != CleanupTrigger.AreaUnloading &&
                trigger != CleanupTrigger.ViewDetached &&
                trigger != CleanupTrigger.LoadRequested &&
                trigger != CleanupTrigger.ProcessTeardown;
        }
    }
}
