using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.GameModes;
using Kingmaker.UI.ActionBar;
using Kingmaker.UI.Group;
using Kingmaker.UI.Selection;
using Kingmaker.View;
using KingmakerMountedCombat.Diagnostics;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Logging;
using UnityEngine;

namespace KingmakerMountedCombat.Integration
{
    internal sealed class KingmakerMountedPairRuntime : IMountedPairRuntime
    {
        internal const string MammothBlueprintGuid = SupportedMountedProfiles.MammothBlueprintGuid;
        internal const string HorseBlueprintGuid = SupportedMountedProfiles.HorseBlueprintGuid;
        private const int MammothAiBackingFieldToken = 0x040054BA;
        private static readonly FieldInfo MammothAiBackingField = ResolveMammothAiBackingField();

        private readonly IModLogger logger;
        private readonly DiagnosticSettings settings;
        private readonly ScopedTransformAttachmentLease<Transform, Vector3, Quaternion, Vector3> riderAttachmentLease;
        private UnitEntityData rider;
        private UnitEntityData mount;
        private SupportedMountedProfile supportedProfile;
        private UnitEntityView riderView;
        private UnitEntityView mountView;
        private UnitMovementAgent riderStockAgent;
        private bool riderStockAgentWasEnabled;
        private bool riderAvoidanceWasDisabled;
        private bool avoidanceLeaseOwned;
        private RiderMovementAgent riderOverride;
        private bool overrideComponentOwned;
        private bool overrideInstalled;
        private bool movementAuthorityConfigured;
        private bool mountAiBackingWasEnabled;
        private bool mountAiLeaseOwned;
        private bool riderForbidRotationWasEnabled;
        private bool riderForbidRotationLeaseOwned;
        private Transform sourceAnchor;
        private GameObject positionAnchorObject;
        private Transform positionAnchor;
        private MountedRiderPoseAdapter poseAdapter;
        private bool poseComponentOwned;
        private bool poseBaselineRestoreVerified;
        private bool replacementRiderViewReleaseVerified;
        private bool presentationConfigured;
        private Vector3 preMountRiderPosition;
        private float preMountRiderOrientation;

        public KingmakerMountedPairRuntime(IModLogger logger, DiagnosticSettings settings)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            riderAttachmentLease = new ScopedTransformAttachmentLease<Transform, Vector3, Quaternion, Vector3>(
                transform => transform.parent,
                transform => transform.GetSiblingIndex(),
                transform => transform.position,
                transform => transform.rotation,
                transform => transform.localScale,
                (transform, parent, worldPositionStays) => transform.SetParent(parent, worldPositionStays),
                (transform, siblingIndex) => transform.SetSiblingIndex(siblingIndex),
                (transform, position) => transform.position = position,
                (transform, rotation) => transform.rotation = rotation,
                (transform, scale) => transform.localScale = scale,
                null,
                new BoundedVector3Comparer(0.0001f),
                new BoundedQuaternionComparer(0.01f),
                new BoundedVector3Comparer(0.0001f));
        }

        public UnitEntityData Rider => rider;

        public UnitEntityData Mount => mount;

        public RiderMovementAgent MovementAgent => riderOverride;

        public string MountProfileId => supportedProfile?.Id;

        public string MountDisplayName => supportedProfile?.DisplayName;

        public bool PresentationAttachmentLeaseActive => riderAttachmentLease.IsAcquired;

        public bool PresentationAttachmentRestoreVerified => riderAttachmentLease.LastRestoreVerified;

        public string PresentationAttachmentParentName => positionAnchor == null ? null : positionAnchor.name;

        public string PresentationSourceAnchorName => sourceAnchor == null ? null : sourceAnchor.name;

        public bool RiderParentMatchesAttachment => riderView != null && positionAnchor != null && riderView.transform.parent == positionAnchor;

        public bool HasPresentationAttachmentResidue => riderAttachmentLease.IsAcquired || positionAnchorObject != null || positionAnchor != null ||
            poseAdapter != null || poseComponentOwned;

        public bool PoseConfigured => poseAdapter != null && poseAdapter.IsConfigured;

        public bool PoseHealthy => poseAdapter != null && poseAdapter.IsHealthy;

        public bool PoseFrameApplied => poseAdapter != null && poseAdapter.FramePoseApplied;

        internal bool MountAiLeaseOwned => mountAiLeaseOwned;

        internal bool MountRawAiEnabled => mount != null && (bool)MammothAiBackingField.GetValue(mount);

        public bool PoseBaselineRestoreVerified => poseAdapter != null ? poseAdapter.BaselineRestoreVerified : poseBaselineRestoreVerified;

        public bool ReplacementRiderViewReleaseVerified => replacementRiderViewReleaseVerified;

        public int PoseBoneCount => poseAdapter == null ? 0 : poseAdapter.BoneCount;

        public int PoseComponentCount => riderView == null ? 0 : riderView.GetComponents<MountedRiderPoseAdapter>().Length;

        public string PoseProfileId => poseAdapter == null ? null : poseAdapter.ProfileId;

        public string PoseBoneInventory => poseAdapter == null ? null : poseAdapter.BoneInventory;

        public string PoseFailure => poseAdapter == null ? null : poseAdapter.LastFailure;

        public long PoseApplicationFrameCount => poseAdapter == null ? 0 : poseAdapter.PoseApplicationFrameCount;

        public long PoseFootTargetClampCount => poseAdapter == null ? 0 : poseAdapter.FootTargetClampCount;

        public double PoseMaximumFootTargetResidualWorldUnits => poseAdapter == null ? 0d : poseAdapter.MaximumFootTargetResidualWorldUnits;

        public double PoseMaximumKneeTargetResidualWorldUnits => poseAdapter == null ? 0d : poseAdapter.MaximumKneeTargetResidualWorldUnits;

        public double PoseMaximumSegmentLengthResidualWorldUnits => poseAdapter == null ? 0d : poseAdapter.MaximumSegmentLengthResidualWorldUnits;

        public double PoseMaximumApplyMicroseconds => poseAdapter == null ? 0d : poseAdapter.MaximumApplyMicroseconds;

        public double PoseAverageApplyMicroseconds => poseAdapter == null ? 0d : poseAdapter.AverageApplyMicroseconds;

        public string PresentationAttachmentRiskState
        {
            get
            {
                if (!riderAttachmentLease.IsAcquired)
                {
                    return HasPresentationAttachmentResidue ? "owned anchor exists without an active lease" : "none";
                }
                if (riderView == null)
                {
                    return "rider view was destroyed or detached while its parent lease remained active";
                }
                if (mountView == null)
                {
                    return "mount view was destroyed or detached while the rider parent lease remained active";
                }
                if (positionAnchor == null || positionAnchorObject == null)
                {
                    return "owned attachment anchor was destroyed while its rider lease remained active";
                }
                if (!poseComponentOwned || poseAdapter == null)
                {
                    return "owned rider pose adapter was destroyed or detached while mounted";
                }
                if (!poseAdapter.IsHealthy)
                {
                    return "owned rider pose adapter is unhealthy: " + (poseAdapter.LastFailure ?? "unknown failure");
                }
                if (riderView.transform.parent != positionAnchor)
                {
                    return "rider parent changed outside the owned attachment lease";
                }
                return "active and internally consistent";
            }
        }

        public void Prepare(UnitEntityData riderUnit, UnitEntityData mountUnit)
        {
            if (rider != null || mount != null)
            {
                throw new InvalidOperationException("A game pair is already prepared.");
            }

            rider = riderUnit ?? throw new ArgumentNullException(nameof(riderUnit));
            mount = mountUnit ?? throw new ArgumentNullException(nameof(mountUnit));
            supportedProfile = SupportedMountedProfiles.Resolve(mount);
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
            var exactSupportedMount = SupportedMountedProfiles.Resolve(mountUnit) != null;
            return new MountedPairCandidate(riderUnit.UniqueId, mountUnit.UniqueId)
            {
                RiderIsDirectlyControllable = riderUnit.IsInGame && riderUnit.IsDirectlyControllable,
                MountIsDirectlyControllable = mountUnit.IsInGame && mountUnit.IsDirectlyControllable,
                RiderIsAliveAndConscious = riderState != null && riderState.IsConscious && !riderState.IsFinallyDead,
                MountIsAliveAndConscious = mountState != null && mountState.IsConscious && !mountState.IsFinallyDead,
                ExactReciprocalCompanionRelationship = exactCompanion && exactSupportedMount,
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

            var modeDisposition = MountedGameModePolicy.Classify(Game.Instance?.CurrentMode.ToString());
            if (!MountedViewActivityPolicy.IsAdmissible(
                modeDisposition,
                riderView.gameObject.activeSelf,
                mountView.gameObject.activeSelf,
                riderView.gameObject.activeInHierarchy,
                mountView.gameObject.activeInHierarchy))
            {
                return "Mounted rider/mount view activity is incoherent for the exact current game mode.";
            }

            if (rider.Descriptor?.Pet != mount || mount.Descriptor?.Master.Value != rider || !mount.Descriptor.IsPet)
            {
                return "The reciprocal active-companion relationship changed.";
            }

            if (supportedProfile == null || mount.Blueprint == null ||
                !string.Equals(mount.Blueprint.AssetGuid, supportedProfile.BlueprintGuid, StringComparison.Ordinal))
            {
                return "The active companion is no longer the selected supported mount blueprint.";
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

            if (Game.Instance == null || !MountedGameModePolicy.CanRetainMountedRelationship(Game.Instance.CurrentMode.ToString()))
            {
                return "An unsupported mounted game-mode boundary was crossed.";
            }

            if (!movementAuthorityConfigured || riderView.AgentASP != riderStockAgent || riderStockAgent == null ||
                riderStockAgent.enabled || !avoidanceLeaseOwned || riderOverride == null || !overrideInstalled ||
                riderView.AgentOverride != riderOverride || !riderForbidRotationLeaseOwned || !riderView.ForbidRotation)
            {
                return "Owned rider movement-authority state no longer matches the mounted invariant.";
            }

            if (mountView.AgentASP == null || !mountView.AgentASP.enabled || mountView.AgentOverride != null)
            {
                return "The authoritative mount movement agent changed.";
            }

            if (!mountAiLeaseOwned || (bool)MammothAiBackingField.GetValue(mount))
            {
                return "The scoped mount AI lease changed.";
            }

            if (!presentationConfigured || sourceAnchor == null || positionAnchor == null ||
                positionAnchor.parent != mountView.transform || !riderAttachmentLease.IsAcquired ||
                riderView.transform.parent != positionAnchor || !poseComponentOwned || poseAdapter == null ||
                !poseAdapter.IsHealthy || supportedProfile == null ||
                poseAdapter.ProfileId != supportedProfile.RiderPoseProfile.Id ||
                poseAdapter.BoneCount != 7 || riderView.GetComponents<MountedRiderPoseAdapter>().Length != 1)
            {
                return "The scoped mount position attachment or exact supported rider pose is unavailable or changed.";
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
            riderAvoidanceWasDisabled = riderStockAgent.AvoidanceDisabled;
            riderForbidRotationWasEnabled = riderView.ForbidRotation;
            replacementRiderViewReleaseVerified = false;
            preMountRiderPosition = rider.Position;
            preMountRiderOrientation = rider.Orientation;
            movementAuthorityConfigured = true;
            rider.Commands.InterruptMove();
            riderView.StopMoving();
            mount.Commands.InterruptMove();
            mountView.StopMoving();

            mountAiBackingWasEnabled = (bool)MammothAiBackingField.GetValue(mount);
            mount.IsAIEnabled = false;
            if ((bool)MammothAiBackingField.GetValue(mount))
            {
                throw new InvalidOperationException("Mount AI backing state did not enter the scoped disabled lease.");
            }
            mountAiLeaseOwned = true;

            riderStockAgent.AvoidanceDisabled = true;
            avoidanceLeaseOwned = true;
            riderStockAgent.enabled = false;
            riderView.ForbidRotation = true;
            riderForbidRotationLeaseOwned = true;

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
            if (supportedProfile == null)
            {
                throw new InvalidOperationException("The prepared mount has no exact supported mounted profile.");
            }

            sourceAnchor = FindTransform(mountView.transform, supportedProfile.SourceAnchorName);
            if (sourceAnchor == null)
            {
                throw new InvalidOperationException("Selected " + supportedProfile.DisplayName +
                    " view has no exact " + supportedProfile.SourceAnchorName + " transform.");
            }

            // Pilot evidence showed the animated Spine moved 0.113 world units
            // between the engine Update and LateUpdate phases, and inheriting its
            // full quaternion rolled the humanoid root by about 161 degrees every
            // frame. Project the observed Spine point into a fixed child of the
            // authoritative mount root instead. Parenting makes mount translation
            // and yaw continuous without another nav agent; the static root-local
            // point intentionally does not inherit gait-driven bone rotation.
            var sourceOffset = supportedProfile.UsesDiagnosticMammothOffsets
                ? new Vector3(settings.RiderOffsetX, settings.RiderOffsetY, settings.RiderOffsetZ)
                : ToUnity(supportedProfile.SourceAnchorOffset);
            var riderEuler = supportedProfile.UsesDiagnosticMammothOffsets
                ? new Vector3(0f, settings.RiderYawDegrees, 0f)
                : ToUnity(supportedProfile.RiderEulerOffset);
            var desiredWorldPosition = sourceAnchor.TransformPoint(sourceOffset);
            positionAnchorObject = new GameObject("KMC_RiderPositionAnchor");
            positionAnchorObject.hideFlags = HideFlags.HideAndDontSave;
            positionAnchor = positionAnchorObject.transform;
            positionAnchor.SetParent(mountView.transform, false);
            positionAnchor.localPosition = mountView.transform.InverseTransformPoint(desiredWorldPosition);
            positionAnchor.localRotation = Quaternion.identity;
            positionAnchor.localScale = Vector3.one;

            riderAttachmentLease.Acquire(riderView.transform, positionAnchor);
            riderOverride.Configure(
                mount,
                positionAnchor,
                Vector3.zero,
                riderEuler);
            poseAdapter = riderView.gameObject.AddComponent<MountedRiderPoseAdapter>();
            poseComponentOwned = true;
            poseBaselineRestoreVerified = false;
            poseAdapter.Configure(riderView, supportedProfile.RiderPoseProfile);
            presentationConfigured = true;
            logger.Info("Rider presentation attached through an owned " + supportedProfile.DisplayName +
                "-root position lease and exact " + supportedProfile.RiderPoseProfile.Id + " procedural pose profile.");
        }

        internal bool ReassertMountAiLeaseAfterNativeTurnBasedExit()
        {
            if (!mountAiLeaseOwned || mount == null)
            {
                return false;
            }

            mount.IsAIEnabled = false;
            return !(bool)MammothAiBackingField.GetValue(mount);
        }

        public void RestorePresentation(MountedPair pair)
        {
            Exception first = null;
            try
            {
                ReleaseReplacementRiderViewFromOwnedAnchor();
            }
            catch (Exception exception)
            {
                first = first ?? exception;
            }
            try
            {
                if (poseComponentOwned)
                {
                    if (poseAdapter == null)
                    {
                        throw new InvalidOperationException("Owned rider pose adapter was destroyed before baseline restoration could be verified.");
                    }
                    poseAdapter.Deconfigure();
                    if (poseAdapter.HasPoseResidue || !poseAdapter.BaselineRestoreVerified)
                    {
                        throw new InvalidOperationException("Owned rider pose adapter retained bone state after deconfiguration.");
                    }
                    poseBaselineRestoreVerified = true;
                    UnityEngine.Object.Destroy(poseAdapter);
                    poseComponentOwned = false;
                    poseAdapter = null;
                }
            }
            catch (Exception exception)
            {
                first = first ?? exception;
            }

            if (!poseComponentOwned && poseAdapter == null)
            {
                try
                {
                    if (riderOverride != null)
                    {
                        riderOverride.Deconfigure();
                    }
                }
                catch (Exception exception)
                {
                    first = first ?? exception;
                }

                try
                {
                    riderAttachmentLease.Restore();
                }
                catch (Exception exception)
                {
                    first = first ?? exception;
                }
            }

            // Never destroy an attachment parent while a failed lease may still
            // own a rider beneath it. A coordinator retry will repeat restoration.
            if (!poseComponentOwned && poseAdapter == null && !riderAttachmentLease.IsAcquired)
            {
                try
                {
                    if (positionAnchorObject != null)
                    {
                        if (riderView != null && riderView.transform.parent == positionAnchor)
                        {
                            throw new InvalidOperationException("Refusing to destroy an owned attachment anchor while the rider remains parented beneath it.");
                        }
                        positionAnchor.SetParent(null, true);
                        UnityEngine.Object.Destroy(positionAnchorObject);
                    }
                    positionAnchor = null;
                    positionAnchorObject = null;
                    sourceAnchor = null;
                }
                catch (Exception exception)
                {
                    first = first ?? exception;
                }
            }

            if (first != null || riderAttachmentLease.IsAcquired || positionAnchorObject != null || positionAnchor != null ||
                poseAdapter != null || poseComponentOwned)
            {
                throw new InvalidOperationException("Best-effort rider presentation restoration retained attachment residue.", first);
            }

            presentationConfigured = false;
            // MountedRelationshipCoordinator always invokes this presentation
            // restoration before RestoreMovementAuthority. Therefore the rider is
            // detached and its captured world pose is verified before the stock
            // agent/avoidance lease is restored and nav-safe dismount placement is
            // attempted.
            TryReleasePreparedReferences();
        }

        public void RestoreMovementAuthority(MountedPair pair, CleanupTrigger trigger)
        {
            if (riderAttachmentLease.IsAcquired || positionAnchorObject != null || positionAnchor != null || poseAdapter != null || poseComponentOwned)
            {
                // The coordinator performs best-effort cleanup even after a
                // presentation failure. Do not re-enable the rider nav agent or
                // place the logical rider while its view may still be parented
                // under the owned carrier. Both coordinator residue flags remain
                // set, and its ordinary retry must first restore presentation.
                throw new InvalidOperationException("Movement authority cannot be restored while the rider presentation attachment retains residue: " + PresentationAttachmentRiskState + ".");
            }

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
                if (mountAiLeaseOwned)
                {
                    if (mount == null)
                    {
                        throw new InvalidOperationException("Mount disappeared before its AI lease could be restored.");
                    }
                    mount.IsAIEnabled = mountAiBackingWasEnabled;
                    if ((bool)MammothAiBackingField.GetValue(mount) != mountAiBackingWasEnabled)
                    {
                        throw new InvalidOperationException("Mount AI backing state did not return to its captured value.");
                    }
                    mountAiLeaseOwned = false;
                }
            }
            catch (Exception exception)
            {
                first = first ?? exception;
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
                    // UnitEntityView's setter destroys the prior component. Unity's
                    // destroyed-object equality makes that retained reference compare
                    // equal to null, so the postcondition must inspect the property,
                    // not compare it with the now-destroyed owned reference.
                    if (riderView.AgentOverride != null)
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
                if (riderForbidRotationLeaseOwned)
                {
                    if (riderView == null)
                    {
                        throw new InvalidOperationException("Rider view was destroyed before its rotation lease could be restored.");
                    }
                    riderView.ForbidRotation = riderForbidRotationWasEnabled;
                    if (riderView.ForbidRotation != riderForbidRotationWasEnabled)
                    {
                        throw new InvalidOperationException("Rider ForbidRotation did not return to its captured value.");
                    }
                    riderForbidRotationLeaseOwned = false;
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
                        // AvoidanceDisabled is backed by a CountingGuard. Release
                        // exactly the single KMC lease; assigning a captured true
                        // value would acquire a second lease instead of restoring.
                        riderStockAgent.AvoidanceDisabled = false;
                        // A successful setter call is the exact KMC lease release.
                        // The effective getter also reports true whenever Owlcat's
                        // unit state is unconscious, independently of CountingGuard.
                        // Clear KMC ownership before validating that combined stock
                        // contract so a later cleanup retry can never release twice.
                        avoidanceLeaseOwned = false;
                        var riderIsConscious = rider == null || rider.Descriptor.State.IsConscious;
                        if (!AvoidanceRestorationExpectation.Matches(
                            riderAvoidanceWasDisabled,
                            riderIsConscious,
                            riderStockAgent.AvoidanceDisabled))
                        {
                            throw new InvalidOperationException("Rider avoidance state did not match the captured lease state plus native consciousness after releasing the KMC lease.");
                        }
                    }
                    else
                    {
                        avoidanceLeaseOwned = false;
                    }
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
            if (!movementAuthorityConfigured && !presentationConfigured && !mountAiLeaseOwned && !avoidanceLeaseOwned && !riderForbidRotationLeaseOwned && !overrideInstalled && !overrideComponentOwned &&
                !riderAttachmentLease.IsAcquired && positionAnchorObject == null && positionAnchor == null && poseAdapter == null && !poseComponentOwned)
            {
                TryReleasePreparedReferences();
            }
        }

        private void TryReleasePreparedReferences()
        {
            if (movementAuthorityConfigured || presentationConfigured || mountAiLeaseOwned || avoidanceLeaseOwned || riderForbidRotationLeaseOwned || overrideInstalled || overrideComponentOwned ||
                riderAttachmentLease.IsAcquired || positionAnchorObject != null || positionAnchor != null || poseAdapter != null || poseComponentOwned)
            {
                return;
            }

            riderOverride = null;
            riderStockAgent = null;
            riderView = null;
            mountView = null;
            rider = null;
            mount = null;
            supportedProfile = null;
        }

        internal bool IsExactCapturedView(UnitEntityData unit)
        {
            if (unit == null)
            {
                return false;
            }
            if (unit == rider)
            {
                return unit.View == riderView;
            }
            return unit == mount && unit.View == mountView;
        }

        internal bool HasRiderViewReplacement => rider != null && riderView != null && rider.View != null && rider.View != riderView;

        internal bool IsChangedViewChildOfOwnedAnchor(UnitEntityData unit)
        {
            return unit != null && unit == rider && unit.View != null &&
                unit.View != riderView && positionAnchor != null &&
                unit.View.transform.parent == positionAnchor;
        }

        internal string CapturePresentationObservation(bool includeUiOwnership = true)
        {
            var currentRiderView = rider?.View;
            var currentMountView = mount?.View;
            var selection = Kingmaker.UI.Selection.SelectionManager.Instance?.SelectedUnits;
            var riderSelected = rider != null && selection != null && selection.Count == 1 && selection[0] == rider;
            return "mode=" + (Game.Instance == null ? "unavailable" : Game.Instance.CurrentMode.ToString()) +
                ";turnBased=" + TurnBased.Controllers.CombatController.IsInTurnBasedCombat() +
                ";riderViewExact=" + (currentRiderView != null && currentRiderView == riderView) +
                ";riderViewActiveSelf=" + (currentRiderView != null && currentRiderView.gameObject.activeSelf) +
                ";riderViewActiveInHierarchy=" + (currentRiderView != null && currentRiderView.gameObject.activeInHierarchy) +
                ";riderParent=" + (currentRiderView?.transform.parent == null ? "<root>" : currentRiderView.transform.parent.name) +
                ";riderSibling=" + (currentRiderView == null ? -1 : currentRiderView.transform.GetSiblingIndex()) +
                ";riderRendererCount=" + CountRenderers(currentRiderView) +
                ";riderEnabledRendererCount=" + CountEnabledRenderers(currentRiderView) +
                ";mountViewExact=" + (currentMountView != null && currentMountView == mountView) +
                ";mountViewActiveSelf=" + (currentMountView != null && currentMountView.gameObject.activeSelf) +
                ";mountViewActiveInHierarchy=" + (currentMountView != null && currentMountView.gameObject.activeInHierarchy) +
                ";poseLease=" + (poseAdapter != null && poseAdapter.IsConfigured) +
                ";attachmentLease=" + riderAttachmentLease.IsAcquired +
                ";replacementReleased=" + replacementRiderViewReleaseVerified +
                ";riderSelected=" + riderSelected +
                ";observationScope=" + (includeUiOwnership ? "full-ui" : "mode-lightweight") +
                (includeUiOwnership ? ";" + CaptureUiOwnershipObservation() : string.Empty);
        }

        private string CaptureUiOwnershipObservation()
        {
            try
            {
                var actionBar = ActionBarManager.Instance;
                var actionBarOwnerField = typeof(ActionBarManager).GetField(
                    "m_Selected",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var actionBarOwner = actionBar == null || actionBarOwnerField == null
                    ? null
                    : actionBarOwnerField.GetValue(actionBar) as UnitEntityData;

                var portraitOwnerField = typeof(GroupCharacterPortraitController).GetField(
                    "m_Unit",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var portraitSelectionField = typeof(GroupCharacterPortraitController).GetField(
                    "m_SelectionSprite",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var portraitFrameField = typeof(GroupCharacterPortraitController).GetField(
                    "Frame",
                    BindingFlags.Instance | BindingFlags.Public);
                var portraitCount = 0;
                var portraitActiveOwnerCount = 0;
                var portraitActive = false;
                var portraitSelected = false;
                foreach (var candidate in Resources.FindObjectsOfTypeAll<GroupCharacterPortraitController>())
                {
                    if (candidate == null || portraitOwnerField == null || portraitOwnerField.GetValue(candidate) != rider)
                    {
                        continue;
                    }
                    portraitCount++;
                    if (candidate.gameObject.activeInHierarchy)
                    {
                        portraitActiveOwnerCount++;
                        portraitActive = true;
                    }
                    var selectedSprite = portraitSelectionField?.GetValue(candidate) as Sprite;
                    var frame = portraitFrameField?.GetValue(candidate) as Component;
                    var currentSprite = frame?.GetType().GetProperty(
                        "sprite",
                        BindingFlags.Instance | BindingFlags.Public)?.GetValue(frame, null) as Sprite;
                    portraitSelected |= candidate.gameObject.activeInHierarchy &&
                        selectedSprite != null && currentSprite == selectedSprite;
                }

                var follower = Game.Instance?.CameraController?.Follower;
                var followerType = follower?.GetType();
                var cameraOnField = followerType?.GetField("m_IsOn", BindingFlags.Instance | BindingFlags.NonPublic);
                var cameraUnitField = followerType?.GetField("m_Unit", BindingFlags.Instance | BindingFlags.NonPublic);
                var cameraOn = cameraOnField != null && (bool)cameraOnField.GetValue(follower);
                var cameraUnit = cameraUnitField?.GetValue(follower) as UnitEntityData;
                var turn = Game.Instance?.TurnBasedCombatController?.CurrentTurn;
                var pointer = Game.Instance?.DefaultPointerController;
                var selected = SelectionManager.Instance?.SelectedUnits;
                var selectedUnit = selected != null && selected.Count == 1 ? selected[0] : null;

                return "actionBarOwner=" + (actionBarOwner?.UniqueId ?? "<none>") +
                    ";actionBarActive=" + (actionBar != null && actionBar.isActiveAndEnabled && actionBar.gameObject.activeInHierarchy) +
                    ";actionBarEnabled=" + (actionBar != null && actionBar.enabled) +
                    ";actionBarActiveSelf=" + (actionBar != null && actionBar.gameObject.activeSelf) +
                    ";actionBarActiveInHierarchy=" + (actionBar != null && actionBar.gameObject.activeInHierarchy) +
                    ";actionBarReactiveActive=" + ReactiveBooleanValueReader.Read(actionBar, "Active") +
                    ";actionBarCanUseAbilities=" + ReactiveBooleanValueReader.Read(actionBar, "CanUseAbilities") +
                    ";actionBarSectionShown=" + (actionBar != null && actionBar.UISection != null && actionBar.UISection.IsShowed) +
                    ";portraitOwnerCount=" + portraitCount +
                    ";portraitActiveOwnerCount=" + portraitActiveOwnerCount +
                    ";portraitActive=" + portraitActive +
                    ";portraitSelected=" + portraitSelected +
                    ";cameraOn=" + cameraOn +
                    ";cameraOwner=" + (cameraUnit?.UniqueId ?? "<none>") +
                    ";selectedUnit=" + (selectedUnit?.UniqueId ?? "<none>") +
                    ";turnUnit=" + (turn?.Unit?.UniqueId ?? "<none>") +
                    ";turnStatus=" + (turn == null ? "<none>" : turn.Status.ToString()) +
                    ";turnUnitDirectlyControllable=" + (turn?.Unit != null && turn.Unit.IsDirectlyControllable) +
                    ";turnCanMove=" + (turn != null && turn.CanMove) +
                    ";turnCanEndNoActing=" + (turn != null && turn.CanEndTurnAndNoActing()) +
                    ";pointerInGui=" + Kingmaker.Controllers.Clicks.PointerController.InGui +
                    ";pointerControllerAvailable=" + (Game.Instance?.ClickEventsController != null) +
                    ";pointerMode=" + (pointer == null ? "<none>" : pointer.Mode.ToString()) +
                    ";riderCommands=" + (rider?.Commands == null ? -1 : rider.Commands.Raw.Count(command => command != null)) +
                    ";mountCommands=" + (mount?.Commands == null ? -1 : mount.Commands.Raw.Count(command => command != null)) +
                    ";riderAiEnabled=" + (rider != null && rider.IsAIEnabled) +
                    ";mountAiEnabled=" + (mount != null && mount.IsAIEnabled);
            }
            catch (Exception exception)
            {
                return "uiOwnershipObservationError=" + exception.GetType().Name;
            }
        }

        private void ReleaseReplacementRiderViewFromOwnedAnchor()
        {
            var replacement = rider?.View;
            if (replacement == null || replacement == riderView || positionAnchor == null ||
                replacement.transform.parent != positionAnchor)
            {
                return;
            }

            var transform = replacement.transform;
            var activeSelf = replacement.gameObject.activeSelf;
            var worldPosition = transform.position;
            var worldRotation = transform.rotation;
            var worldScale = transform.lossyScale;
            if (!riderAttachmentLease.ReleaseInheritedReplacement(transform) ||
                transform.parent != riderAttachmentLease.OriginalParent ||
                Vector3.Distance(transform.position, worldPosition) > 0.0001f ||
                Quaternion.Angle(transform.rotation, worldRotation) > 0.01f ||
                Vector3.Distance(transform.lossyScale, worldScale) > 0.0001f ||
                replacement.gameObject.activeSelf != activeSelf)
            {
                throw new InvalidOperationException("Replacement rider view did not leave the owned attachment anchor without transform or visibility mutation.");
            }
            replacementRiderViewReleaseVerified = true;
            logger.Info("Released the stock replacement rider view from the owned mounted anchor before presentation cleanup.");
        }

        private static int CountRenderers(UnitEntityView view)
        {
            return view == null ? 0 : view.GetComponentsInChildren<Renderer>(true).Length;
        }

        private static int CountEnabledRenderers(UnitEntityView view)
        {
            if (view == null)
            {
                return 0;
            }
            var count = 0;
            foreach (var renderer in view.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer != null && renderer.enabled)
                {
                    count++;
                }
            }
            return count;
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
            return MountedGameModePolicy.CanAdmitMountedAction(mode.ToString());
        }

        private static Vector3 ToUnity(PoseVector3 value)
        {
            return new Vector3(value.X, value.Y, value.Z);
        }

        private static FieldInfo ResolveMammothAiBackingField()
        {
            var field = typeof(UnitEntityData).GetField("m_AiEnabled", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null || field.MetadataToken != MammothAiBackingFieldToken || field.FieldType != typeof(bool))
            {
                throw new MissingFieldException(typeof(UnitEntityData).FullName, "m_AiEnabled exact token " + MammothAiBackingFieldToken.ToString("X8"));
            }
            return field;
        }

        private static bool ShouldPlaceRiderAfterCleanup(CleanupTrigger trigger)
        {
            return trigger != CleanupTrigger.AreaUnloading &&
                trigger != CleanupTrigger.ViewDetached &&
                trigger != CleanupTrigger.ViewReplaced &&
                trigger != CleanupTrigger.LoadRequested &&
                trigger != CleanupTrigger.ProcessTeardown;
        }

        private sealed class BoundedVector3Comparer : IEqualityComparer<Vector3>
        {
            private readonly float maximumDistance;

            public BoundedVector3Comparer(float maximumDistance)
            {
                this.maximumDistance = maximumDistance;
            }

            public bool Equals(Vector3 first, Vector3 second)
            {
                return Vector3.Distance(first, second) <= maximumDistance;
            }

            public int GetHashCode(Vector3 value)
            {
                return 0;
            }
        }

        private sealed class BoundedQuaternionComparer : IEqualityComparer<Quaternion>
        {
            private readonly float maximumAngleDegrees;

            public BoundedQuaternionComparer(float maximumAngleDegrees)
            {
                this.maximumAngleDegrees = maximumAngleDegrees;
            }

            public bool Equals(Quaternion first, Quaternion second)
            {
                return Quaternion.Angle(first, second) <= maximumAngleDegrees;
            }

            public int GetHashCode(Quaternion value)
            {
                return 0;
            }
        }
    }
}
