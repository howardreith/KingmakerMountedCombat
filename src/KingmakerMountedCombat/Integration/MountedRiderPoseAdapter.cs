using System;
using System.Collections.Generic;
using System.Diagnostics;
using Kingmaker.View;
using Kingmaker.Visual.Animation.Kingmaker;
using KingmakerMountedCombat.Domain;
using UnityEngine;

namespace KingmakerMountedCombat.Integration
{
    [DefaultExecutionOrder(32000)]
    internal sealed class MountedRiderPoseAdapter : MonoBehaviour
    {
        private ScopedPoseBaselineLease<Transform, Vector3, Quaternion, Vector3> baselineLease;
        private UnitEntityView riderView;
        private MountedRiderPoseProfile profile;
        private Transform pelvis;
        private LegBinding leftLeg;
        private LegBinding rightLeg;
        private bool configured;
        private bool baselineCaptured;
        private long totalApplyTicks;
        private readonly AnimatedSaddlePosition animatedSaddle = new AnimatedSaddlePosition();
        private Transform saddleMountRoot;
        private Transform saddleSource;
        private bool animatedSaddleRequired;
        private Transform scaleCompensatedAnchor;
        private PoseVector3 mountScaleAtAttach;

        public bool IsConfigured => configured;

        public bool IsHealthy => configured && string.IsNullOrEmpty(LastFailure) && baselineLease != null && baselineLease.IsAcquired &&
            (!animatedSaddleRequired || animatedSaddle.IsAcquired && saddleMountRoot != null && saddleSource != null &&
                saddleSource.IsChildOf(saddleMountRoot));

        public long AnimatedSaddleSampleCount { get; private set; }
        // MMR-01 (review R1-F12): the rider pose's own pelvis-to-source vector at attachment, the basis applied now (the
        // attachment sample until the rider's own pose settles, then the settled sample), and the settle state.
        public PoseVector3 CalibrationObservedSeatFromSource { get; private set; }
        public PoseVector3 CalibrationAppliedSeatFromSource { get; private set; }
        public bool SeatCalibrationSettled => seatSampler != null && seatSampler.Settled;
        public bool SeatCalibrationSettles => seatSampler != null;

        // PR-3 review R1-F4: which seat basis this adapter actually applies: none (no animated saddle, e.g. the Mammoth), fixed (a Small
        // fixed-from-source seat; CalibrationAppliedSeatFromSource is its fixed basis, nothing is measured), attachment (the posed pelvis
        // sampled once), or settling (the Horse's provisional-then-settled sample).
        public string SeatCalibrationKind { get; private set; } = "none";

        // The presentation observation's seat-calibration fields, scoped to what this adapter measured or applied.
        internal static string FormatSeatCalibration(MountedRiderPoseAdapter adapter)
        {
            if (adapter == null) return "<none>";
            switch (adapter.SeatCalibrationKind)
            {
                case "fixed":
                    return "fixed;observedSeatFromSource=<not-measured>;appliedSeatFromSource=" + AnimatedSaddleCalibration.Format(adapter.CalibrationAppliedSeatFromSource);
                case "attachment":
                    return "attachment;observedSeatFromSource=" + AnimatedSaddleCalibration.Format(adapter.CalibrationObservedSeatFromSource) +
                        ";appliedSeatFromSource=" + AnimatedSaddleCalibration.Format(adapter.CalibrationAppliedSeatFromSource);
                case "settling":
                    return (adapter.SeatCalibrationSettled ? "settled" : "provisional") +
                        ";observedSeatFromSource=" + AnimatedSaddleCalibration.Format(adapter.CalibrationObservedSeatFromSource) +
                        ";appliedSeatFromSource=" + AnimatedSaddleCalibration.Format(adapter.CalibrationAppliedSeatFromSource) +
                        ";settleFrames=" + adapter.SeatSettleFrames + ";seatIneligibleFrames=" + adapter.SeatIneligibleFrames;
                default:
                    return "none(no animated saddle)";
            }
        }
        public int SeatSettleFrames => seatSampler == null ? 0 : seatSampler.FramesToSettle;
        public float SeatSettleAnimationSeconds => seatSampler == null ? 0f : seatSampler.AnimationSecondsToSettle;
        public int SeatIgnoredFrames => seatSampler == null ? 0 : seatSampler.IgnoredFrames;
        public int SeatIneligibleFrames => seatSampler == null ? 0 : seatSampler.IneligibleFrames;
        // Review R5-F3: the attachment-frame anchor relation (rider position anchor minus Chest, mount root frame).
        public PoseVector3 CalibrationAnchorFromSource { get; private set; }
        // Set by the pair runtime before ConfigureAnimatedSaddle: (phase, observed, applied, frames, animation seconds, anchor
        // relation) for its seatCalibration line.
        internal Action<string, PoseVector3, PoseVector3, int, float, PoseVector3> SeatCalibrationNotice;
        private SettledSeatSampler seatSampler;
        private Transform seatRiderAnchor;
        private PoseVector3 seatCorrection;
        public double MaximumAnimatedSeatError { get; private set; }
        public Vector3 LastAnimatedSeatPosition { get; private set; }

        internal double? ObserveCurrentAnimatedSeatResidual()
        {
            if (!animatedSaddleRequired || !IsHealthy || pelvis == null) { return null; }
            var seat = animatedSaddle.Project(ToPose(saddleMountRoot.InverseTransformPoint(saddleSource.position)));
            return Vector3.Distance(pelvis.position, saddleMountRoot.TransformPoint(ToUnity(seat)));
        }

        public bool HasPoseResidue => baselineLease != null && (baselineLease.IsAcquired || baselineLease.IsFrameActive);

        internal string ObserveSeatGeometry()
        {
            if (!animatedSaddleRequired || !IsHealthy || pelvis == null) { return "unavailable"; }
            // Cached joints only: Chest supplies the anatomical centerline.
            return "pelvisMountLocal=" + saddleMountRoot.InverseTransformPoint(pelvis.position).ToString("F4") +
                ";chestMountLocal=" + saddleMountRoot.InverseTransformPoint(saddleSource.position).ToString("F4") +
                ";anatomicalRiderRightMountLocal=" + saddleMountRoot.InverseTransformDirection(
                    (rightLeg.Thigh.position - leftLeg.Thigh.position).normalized).ToString("F4") +
                ";mountRightWorld=" + saddleMountRoot.right.ToString("F4") +
                ";chestWorld=" + saddleSource.position.ToString("F4") +
                ";chestLocalRotation=" + saddleSource.localRotation.ToString("F4");
        }

        public bool BaselineRestoreVerified => !baselineCaptured || (baselineLease != null && baselineLease.LastRestoreVerified);

        public bool FramePoseApplied => baselineLease != null && baselineLease.IsFrameActive;

        public int BoneCount => baselineLease == null ? 0 : baselineLease.NodeCount;

        public long PoseApplicationFrameCount { get; private set; }

        public long FootTargetClampCount { get; private set; }

        public double MaximumFootTargetResidualWorldUnits { get; private set; }

        public double MaximumKneeTargetResidualWorldUnits { get; private set; }

        public double MaximumSegmentLengthResidualWorldUnits { get; private set; }

        public double MaximumApplyMicroseconds { get; private set; }

        public double AverageApplyMicroseconds => PoseApplicationFrameCount == 0
            ? 0d
            : TicksToMicroseconds(totalApplyTicks) / PoseApplicationFrameCount;

        public string ProfileId => profile == null ? null : profile.Id;

        public string LastFailure { get; private set; }

        public string BoneInventory => pelvis == null || leftLeg == null || rightLeg == null
            ? null
            : string.Join(",", new[]
            {
                pelvis.name,
                leftLeg.Thigh.name,
                leftLeg.LowerLeg.name,
                leftLeg.Foot.name,
                rightLeg.Thigh.name,
                rightLeg.LowerLeg.name,
                rightLeg.Foot.name
            });

        public void Configure(UnitEntityView exactRiderView, MountedRiderPoseProfile exactProfile)
        {
            if (configured || (baselineLease != null && baselineLease.IsAcquired))
            {
                throw new InvalidOperationException("A mounted rider pose adapter is already configured.");
            }
            riderView = exactRiderView ?? throw new ArgumentNullException(nameof(exactRiderView));
            profile = exactProfile ?? throw new ArgumentNullException(nameof(exactProfile));
            string surfaceError;
            if (!TryResolveSupportedRig(riderView, profile, out pelvis, out leftLeg, out rightLeg, out surfaceError))
            {
                throw new InvalidOperationException(surfaceError);
            }

            baselineLease = CreateBaselineLease();
            baselineLease.Acquire(new[]
            {
                pelvis,
                leftLeg.Thigh,
                leftLeg.LowerLeg,
                leftLeg.Foot,
                rightLeg.Thigh,
                rightLeg.LowerLeg,
                rightLeg.Foot
            });
            baselineCaptured = true;
            PrimeTimedPosePath();
            LastFailure = null;
            PoseApplicationFrameCount = 0;
            FootTargetClampCount = 0;
            MaximumFootTargetResidualWorldUnits = 0d;
            MaximumKneeTargetResidualWorldUnits = 0d;
            MaximumSegmentLengthResidualWorldUnits = 0d;
            MaximumApplyMicroseconds = 0d;
            totalApplyTicks = 0;
            configured = true;
            enabled = true;
        }

        private void PrimeTimedPosePath()
        {
            // Keep one-time CLR/Unity call-site initialization outside the
            // per-frame budget while exercising the exact same pose mutation
            // and exact restoration path. Normal evidence counters start only
            // after this reversible prime succeeds.
            var started = Stopwatch.GetTimestamp();
            baselineLease.PrimeFrame(ApplyPose);
            var elapsedMicroseconds = TicksToMicroseconds(Stopwatch.GetTimestamp() - started);
            if (elapsedMicroseconds < 0d || double.IsNaN(elapsedMicroseconds) || double.IsInfinity(elapsedMicroseconds))
            {
                throw new InvalidOperationException("Mounted rider pose priming produced an invalid elapsed time.");
            }
        }

        public void ConfigureAnimatedSaddle(Transform exactMountRoot, Transform exactSource, Vector3 anatomicalCorrection, bool seatIsFixedFromSource = false,
            bool settleSeatFromIdlePose = false, Transform exactRiderAnchor = null)
        {
            if (!configured || exactMountRoot == null || exactSource == null ||
                !exactSource.IsChildOf(exactMountRoot) ||
                settleSeatFromIdlePose && (exactRiderAnchor == null || !exactRiderAnchor.IsChildOf(exactMountRoot)))
            {
                throw new InvalidOperationException("Horse saddle requires the exact live mount rig.");
            }
            saddleMountRoot = exactMountRoot;
            saddleSource = exactSource;
            seatRiderAnchor = settleSeatFromIdlePose ? exactRiderAnchor : null;
            baselineLease.PrimeFrame(() =>
            {
                ApplyPose();
                AcquireSaddleCalibration(anatomicalCorrection, seatIsFixedFromSource, settleSeatFromIdlePose);
            });
            animatedSaddleRequired = true;
        }

        // MMR-01 (review R1-F12): the attachment calibration of the animated saddle, inside the primed frame after the profile
        // pose. Small profiles keep their fixed-from-source seat. Otherwise the posed pelvis is sampled; the Horse treats that
        // sample as provisional and settles on the rider's own still pose later (ObserveSettlingSeat, AnimatedSaddleCalibration).
        private void AcquireSaddleCalibration(Vector3 anatomicalCorrection, bool seatIsFixedFromSource, bool settleSeatFromIdlePose)
        {
            var source = ToPose(saddleMountRoot.InverseTransformPoint(saddleSource.position));
            seatSampler = null;
            if (seatIsFixedFromSource)
            {
                animatedSaddle.Acquire(source, source + ToPose(anatomicalCorrection), new PoseVector3(0f, 0f, 0f));
                SeatCalibrationKind = "fixed";
                CalibrationObservedSeatFromSource = new PoseVector3(0f, 0f, 0f);
                CalibrationAppliedSeatFromSource = ToPose(anatomicalCorrection);
            }
            else if (settleSeatFromIdlePose)
            {
                // Review R5-F3: the normalized sample (AnimatedSaddleCalibration.NormalizedSeatFromSource): the anchor relation
                // taken in this attachment frame, plus the rider's own rider-local pelvis.
                seatCorrection = ToPose(anatomicalCorrection);
                CalibrationAnchorFromSource = ToPose(saddleMountRoot.InverseTransformPoint(seatRiderAnchor.position)) - source;
                var normalized = NormalizedSeatSample();
                CalibrationObservedSeatFromSource = normalized;
                CalibrationAppliedSeatFromSource = normalized;
                animatedSaddle.Acquire(source, source + normalized, seatCorrection);
                seatSampler = new SettledSeatSampler();
                SeatCalibrationKind = "settling";
                NotifySeatCalibration("attach", CalibrationObservedSeatFromSource, CalibrationAppliedSeatFromSource, 0, 0f);
            }
            else
            {
                var observed = ToPose(saddleMountRoot.InverseTransformPoint(pelvis.position));
                seatCorrection = ToPose(anatomicalCorrection);
                CalibrationObservedSeatFromSource = observed - source;
                CalibrationAppliedSeatFromSource = observed - source;
                animatedSaddle.Acquire(source, observed, seatCorrection);
                SeatCalibrationKind = "attachment";
            }
        }

        // MMR-01 (review R1-F12): every seat frame until the rider's resting pose is confirmed: the normalized sample (after native
        // animation and the profile pelvis offset, before this frame's seat write; the frame lease restored the previous write
        // before the animator pass), the frame's fresh-evaluation advance and its rest eligibility (reviews R5-F4, R7-F2, R7-F3).
        // On the frame a resting window settles, the saddle is re-acquired once from it; the confirmed reference is never
        // replaced afterwards. A pose not settled after the notice interval is logged once (phase=unsettled) and keeps the
        // attachment sample while sampling continues.
        private void ObserveSettlingSeat()
        {
            if (seatSampler == null || !seatSampler.Observing) return;
            var source = ToPose(saddleMountRoot.InverseTransformPoint(saddleSource.position));
            var observed = NormalizedSeatSample();
            var advance = SeatAnimationAdvanceSeconds();
            var outcome = seatSampler.Observe(observed, advance, advance > 0f && SeatRestEligible());
            if (outcome == SeatSampleOutcome.Unsettled)
            {
                NotifySeatCalibration("unsettled", observed, CalibrationAppliedSeatFromSource, seatSampler.Frames, seatSampler.AnimationSeconds);
                return;
            }
            if (outcome != SeatSampleOutcome.Settled) return;
            animatedSaddle.Release();
            animatedSaddle.Acquire(source, source + seatSampler.SettledSample, seatCorrection);
            CalibrationAppliedSeatFromSource = seatSampler.SettledSample;
            NotifySeatCalibration("settled", seatSampler.SettledSample, CalibrationAppliedSeatFromSource, seatSampler.FramesToSettle,
                seatSampler.AnimationSecondsToSettle);
        }

        // Review R5-F3: the attachment-frame anchor relation plus the rider-local pelvis (pelvis minus the rider root, which
        // RiderMovementAgent keeps on the anchor), in the mount root frame; independent of where the Chest stood at attachment.
        private PoseVector3 NormalizedSeatSample() => AnimatedSaddleCalibration.NormalizedSeatFromSource(CalibrationAnchorFromSource,
            ToPose(saddleMountRoot.InverseTransformPoint(pelvis.position)), ToPose(saddleMountRoot.InverseTransformPoint(riderView.transform.position)));

        // Review R5-F4: how far the rider's freshly evaluated animation advanced this frame (0 = not evaluated or not advancing).
        // Installed contracts (Test-MmrHorseSeatNativeContract.ps1): UnitEntityView.OnDataAttached sets every unit Animator's
        // cullingMode to AlwaysAnimate, so off-screen culling does not stop the skeleton's transform writes; the native
        // AnimationManager's PlayableGraph (created in OnEnable, output bound to that Animator) is evaluated by the Animator's
        // update while it plays. Not evaluated: the manager or Animator inactive or disabled, the graph invalid or stopped, or a
        // culling mode other than AlwaysAnimate (Kingmaker never sets one for a unit; treated as unverified). Not advancing: the
        // manager Disabled (its graph speed 0: StandUp, resurrection) or a paused game (TimeController.Suspend: Time.timeScale 0).
        private float SeatAnimationAdvanceSeconds()
        {
            var manager = riderView == null ? null : riderView.AnimationManager;
            if (manager == null || !manager.isActiveAndEnabled || manager.Disabled) return 0f;
            var animator = manager.Animator;
            if (animator == null || !animator.isActiveAndEnabled || animator.cullingMode != AnimatorCullingMode.AlwaysAnimate) return 0f;
            var graph = manager.PlayableGraph;
            if (!graph.IsValid() || !graph.IsPlaying()) return 0f;
            var delta = Time.deltaTime;
            return delta > 0f ? delta : 0f;
        }

        // Reviews R7-F2, R7-F3: whether the rider's current pose is a resting reference, by the native idle stage itself:
        // UnitAnimationManager.CanRunIdleAction (installed: animation Speed <= 0, and every active action is the locomotion
        // handle, additive or released). The carried rider's Speed follows the mount's movement (RiderMovementAgent reports the
        // mount agent's speed; UnitAnimationController.TickOnUnit sets the animation Speed from it), so the pair moving is not
        // rest. A released action still blending out (an active animation of a released, non-additive handle with weight > 0)
        // is a transition, not rest. A command that waits (queued, or not yet started and not moving) plays no action and stays
        // eligible; an approach, attack, cast, idle variant or transition does not.
        private bool SeatRestEligible()
        {
            var manager = riderView == null ? null : riderView.AnimationManager;
            if (manager == null || !manager.CanRunIdleAction()) return false;
            // PR-3 review R1-F1: rest requires (1) native idle eligibility (CanRunIdleAction above), (2) a positively weighted current
            // locomotion composition: an animation whose unreleased handle's action is a UnitAnimationAction of Type LocoMotion and whose
            // handle.ActiveAnimation is that animation, with GetWeight() > 0, and (3) no outgoing released non-additive contribution.
            // It does not require the transition-in to have finished. Installed 2.1.7b trace (docs/missions/mmr-split/evidence/
            // PR-3-idle-weight-trace.md, pinned by Test-MmrHorseSeatNativeContract.ps1): the stationary humanoid idle is registered by
            // UnitAnimationActionLocoMotionHuman.UpdateCurrentClips through AnimationManager.AddAnimationClip, which stores the one
            // PlayableInfo (or off-hand AnimationComposition) as handle.ActiveAnimation and in ActiveAnimations; its GetWeight is
            // PlayableInfo.m_Weight (Reset to 0, ramped by UpdateInternal over TransitionIn), and MixerInfo.NormalizeWeights gives the
            // clip a positive mixer input exactly when m_Weight * m_WeightMultiplier (always 1) is above zero.
            var animations = manager.ActiveAnimations;
            if (animations == null) return false;
            var restingContributes = false;
            foreach (var animation in animations)
            {
                var handle = animation == null ? null : animation.Handle;
                if (handle == null) continue;
                var additive = handle.Action != null && handle.Action.IsAdditive;
                if (handle.IsReleased)
                {
                    if (!additive && animation.GetWeight() > 0f) return false;
                    continue;
                }
                var locomotion = handle.Action as UnitAnimationAction;
                if (locomotion != null && locomotion.Type == UnitAnimationType.LocoMotion && ReferenceEquals(handle.ActiveAnimation, animation) &&
                    animation.GetWeight() > 0f)
                    restingContributes = true;
            }
            return restingContributes;
        }

        private void NotifySeatCalibration(string phase, PoseVector3 observed, PoseVector3 applied, int frames, float animationSeconds)
        {
            try { SeatCalibrationNotice?.Invoke(phase, observed, applied, frames, animationSeconds, CalibrationAnchorFromSource); }
            catch (Exception) { }
        }

        internal void CompensateMountScale(Transform exactAnchor)
        {
            if (!animatedSaddleRequired || exactAnchor == null || exactAnchor.parent != saddleMountRoot)
                throw new InvalidOperationException("Mount scale compensation requires the owned anchor under the exact mount root.");
            mountScaleAtAttach = ToPose(saddleMountRoot.lossyScale);
            scaleCompensatedAnchor = exactAnchor;
        }

        internal static bool TryValidateSupportedSurface(
            UnitEntityView exactRiderView,
            MountedRiderPoseProfile exactProfile,
            out string error)
        {
            Transform ignoredPelvis;
            LegBinding ignoredLeft;
            LegBinding ignoredRight;
            return TryResolveSupportedRig(
                exactRiderView,
                exactProfile,
                out ignoredPelvis,
                out ignoredLeft,
                out ignoredRight,
                out error);
        }

        public void Deconfigure()
        {
            configured = false;
            enabled = false;
            baselineLease?.Restore();
            animatedSaddle.Release();
            animatedSaddleRequired = false;
            seatSampler = null;
            SeatCalibrationKind = "none";
            seatRiderAnchor = null;
            scaleCompensatedAnchor = null;
            saddleMountRoot = null;
            saddleSource = null;
            riderView = null;
            profile = null;
            pelvis = null;
            leftLeg = null;
            rightLeg = null;
        }

        private void Update()
        {
            if (!configured)
            {
                return;
            }

            try
            {
                baselineLease.RestoreFrame();
            }
            catch (Exception exception)
            {
                LatchFailure("Could not restore the previous evaluated animation baseline before the next animator pass.", exception);
            }
        }

        private void LateUpdate()
        {
            if (!configured)
            {
                return;
            }

            try
            {
                ValidateLiveRig();
                var started = Stopwatch.GetTimestamp();
                baselineLease.BeginFrame();
                ApplyPose();
                var elapsed = Stopwatch.GetTimestamp() - started;
                totalApplyTicks += elapsed;
                var microseconds = TicksToMicroseconds(elapsed);
                MaximumApplyMicroseconds = Math.Max(MaximumApplyMicroseconds, microseconds);
                PoseApplicationFrameCount++;
            }
            catch (Exception exception)
            {
                LatchFailure("Mounted rider pose application failed.", exception);
            }
        }

        private void ApplyPose()
        {
            // A mount's later native growth is the mount's own. The rider keeps its own body size.
            if (scaleCompensatedAnchor != null && saddleMountRoot != null)
            {
                scaleCompensatedAnchor.localScale = ToUnity(MountScaleCompensation.AnchorScale(mountScaleAtAttach, ToPose(saddleMountRoot.lossyScale)));
            }
            var pelvisPosition = pelvis.localPosition;
            var pelvisRotation = pelvis.localRotation;
            pelvis.localPosition = pelvisPosition + ToUnity(profile.PelvisPositionOffset);
            pelvis.localRotation = pelvisRotation * Quaternion.Euler(ToUnity(profile.PelvisEulerOffset));

            if (animatedSaddleRequired)
            {
                ApplyAnimatedSeat();
            }

            ApplyLeg(leftLeg, profile.LeftLeg);
            ApplyLeg(rightLeg, profile.RightLeg);
        }

        // This executes after native animation and the mechanics-only RiderMovementAgent.
        // Project position through the root basis; never inherit the Chest rest quaternion.
        // The seven-bone frame lease restores this visual-only pelvis write before animation.
        private void ApplyAnimatedSeat()
        {
            if (!IsHealthy) { throw new InvalidOperationException("Owned Horse saddle frame changed."); }
            ObserveSettlingSeat();
            var localSeat = animatedSaddle.Project(ToPose(saddleMountRoot.InverseTransformPoint(saddleSource.position)));
            LastAnimatedSeatPosition = saddleMountRoot.TransformPoint(ToUnity(localSeat));
            pelvis.position = LastAnimatedSeatPosition;
            MaximumAnimatedSeatError = Math.Max(MaximumAnimatedSeatError,
                Vector3.Distance(pelvis.position, LastAnimatedSeatPosition));
            AnimatedSaddleSampleCount++;
        }

        private void ApplyLeg(LegBinding binding, MountedRiderLegPoseProfile legProfile)
        {
            var firstLengthBefore = Vector3.Distance(binding.Thigh.position, binding.LowerLeg.position);
            var secondLengthBefore = Vector3.Distance(binding.LowerLeg.position, binding.Foot.position);
            if (firstLengthBefore <= 0.0001f || secondLengthBefore <= 0.0001f)
            {
                throw new InvalidOperationException("A supported rider leg contains a zero-length segment.");
            }

            var evaluatedFootRotation = binding.Foot.rotation;
            var root = binding.Thigh.position;
            // A left-handed native avatar is mirrored, which swaps the anatomical side of each named bone.
            // Scale-compensated (Small) seats follow the side the bone is really on; the Medium path is unchanged.
            var side = scaleCompensatedAnchor != null && riderView.CharacterAvatar.Mirror ? -1f : 1f;
            var footTarget = ToUnity(legProfile.FootTargetFromThigh); footTarget.x *= side;
            var kneeHint = ToUnity(legProfile.KneeHintFromThigh); kneeHint.x *= side;
            var requestedTarget = root + riderView.transform.TransformVector(footTarget);
            var bendHint = root + riderView.transform.TransformVector(kneeHint);
            var solution = TwoBonePoseSolver.Solve(
                ToPose(root),
                ToPose(requestedTarget),
                ToPose(bendHint),
                firstLengthBefore,
                secondLengthBefore);
            if (solution.TargetClamped)
            {
                FootTargetClampCount++;
            }

            var desiredJoint = ToUnity(solution.Joint);
            var desiredTarget = ToUnity(solution.Target);
            var currentThighDirection = binding.LowerLeg.position - binding.Thigh.position;
            var desiredThighDirection = desiredJoint - binding.Thigh.position;
            binding.Thigh.rotation = Quaternion.FromToRotation(currentThighDirection, desiredThighDirection) * binding.Thigh.rotation;

            var currentLowerDirection = binding.Foot.position - binding.LowerLeg.position;
            var desiredLowerDirection = desiredTarget - binding.LowerLeg.position;
            binding.LowerLeg.rotation = Quaternion.FromToRotation(currentLowerDirection, desiredLowerDirection) * binding.LowerLeg.rotation;
            binding.Foot.rotation = evaluatedFootRotation * Quaternion.Euler(ToUnity(legProfile.FootEulerOffset));

            var footResidual = Vector3.Distance(binding.Foot.position, desiredTarget);
            var kneeResidual = Vector3.Distance(binding.LowerLeg.position, desiredJoint);
            var firstLengthAfter = Vector3.Distance(binding.Thigh.position, binding.LowerLeg.position);
            var secondLengthAfter = Vector3.Distance(binding.LowerLeg.position, binding.Foot.position);
            var segmentResidual = Math.Max(
                Math.Abs(firstLengthAfter - firstLengthBefore),
                Math.Abs(secondLengthAfter - secondLengthBefore));
            MaximumFootTargetResidualWorldUnits = Math.Max(MaximumFootTargetResidualWorldUnits, footResidual);
            MaximumKneeTargetResidualWorldUnits = Math.Max(MaximumKneeTargetResidualWorldUnits, kneeResidual);
            MaximumSegmentLengthResidualWorldUnits = Math.Max(MaximumSegmentLengthResidualWorldUnits, segmentResidual);
        }

        private void ValidateLiveRig()
        {
            if (riderView == null || riderView.gameObject != gameObject || riderView.Animator == null || riderView.CharacterAvatar == null ||
                pelvis == null || leftLeg == null || rightLeg == null || baselineLease == null || !baselineLease.IsAcquired)
            {
                throw new InvalidOperationException("The exact rider view or pose baseline was detached/replaced.");
            }
            if (!leftLeg.IsHierarchyValid(pelvis) || !rightLeg.IsHierarchyValid(pelvis))
            {
                throw new InvalidOperationException("The supported rider pelvis/leg hierarchy changed while mounted.");
            }
        }

        private void LatchFailure(string message, Exception exception)
        {
            LastFailure = message + " " + exception.GetType().Name + ": " + exception.Message;
            enabled = false;
        }

        private void OnDisable()
        {
            BestEffortRestore();
        }

        private void OnDestroy()
        {
            configured = false;
            BestEffortRestore();
        }

        private void BestEffortRestore()
        {
            try
            {
                baselineLease?.Restore();
            }
            catch (Exception exception)
            {
                if (string.IsNullOrEmpty(LastFailure))
                {
                    LastFailure = "Pose teardown restoration failed. " + exception.GetType().Name + ": " + exception.Message;
                }
            }
        }

        private static bool TryResolveSupportedRig(
            UnitEntityView exactRiderView,
            MountedRiderPoseProfile exactProfile,
            out Transform exactPelvis,
            out LegBinding exactLeftLeg,
            out LegBinding exactRightLeg,
            out string error)
        {
            exactPelvis = null;
            exactLeftLeg = null;
            exactRightLeg = null;
            error = null;
            if (exactRiderView == null)
            {
                error = "The exact rider view is required for pose-surface validation.";
                return false;
            }
            if (exactProfile == null)
            {
                error = "The exact rider pose profile is required.";
                return false;
            }
            var profileError = exactProfile.Validate();
            if (profileError != null)
            {
                error = "Mounted rider pose profile is invalid: " + profileError;
                return false;
            }
            if (exactRiderView.Animator == null || exactRiderView.CharacterAvatar == null)
            {
                error = "The exact rider view has no active native humanoid animator/character surface.";
                return false;
            }

            var searchRoot = exactRiderView.CharacterAvatar.transform;
            exactPelvis = FindUniqueTransform(searchRoot, exactProfile.PelvisBoneName);
            exactLeftLeg = ResolveLeg(searchRoot, exactPelvis, exactProfile.LeftLeg);
            exactRightLeg = ResolveLeg(searchRoot, exactPelvis, exactProfile.RightLeg);
            if (exactPelvis == null || exactLeftLeg == null || exactRightLeg == null)
            {
                error = "The exact rider does not expose one unambiguous supported humanoid pelvis/leg rig.";
                return false;
            }
            return true;
        }

        private static LegBinding ResolveLeg(Transform searchRoot, Transform exactPelvis, MountedRiderLegPoseProfile leg)
        {
            if (searchRoot == null || exactPelvis == null || leg == null)
            {
                return null;
            }
            var thigh = FindUniqueTransform(searchRoot, leg.ThighBoneName);
            var lowerLeg = FindUniqueTransform(searchRoot, leg.LowerLegBoneName);
            var foot = FindUniqueTransform(searchRoot, leg.FootBoneName);
            var binding = new LegBinding(thigh, lowerLeg, foot);
            return binding.IsHierarchyValid(exactPelvis) ? binding : null;
        }

        private static Transform FindUniqueTransform(Transform root, string exactName)
        {
            Transform found = null;
            var count = 0;
            FindTransforms(root, exactName, ref found, ref count);
            return count == 1 ? found : null;
        }

        private static void FindTransforms(Transform current, string exactName, ref Transform found, ref int count)
        {
            if (current == null || count > 1)
            {
                return;
            }
            if (string.Equals(current.name, exactName, StringComparison.Ordinal))
            {
                found = current;
                count++;
            }
            for (var index = 0; index < current.childCount; index++)
            {
                FindTransforms(current.GetChild(index), exactName, ref found, ref count);
            }
        }

        private static PoseVector3 ToPose(Vector3 value)
        {
            return new PoseVector3(value.x, value.y, value.z);
        }

        private static Vector3 ToUnity(PoseVector3 value)
        {
            return new Vector3(value.X, value.Y, value.Z);
        }

        private static double TicksToMicroseconds(long ticks)
        {
            return ticks * (1000000d / Stopwatch.Frequency);
        }

        private static ScopedPoseBaselineLease<Transform, Vector3, Quaternion, Vector3> CreateBaselineLease()
        {
            return new ScopedPoseBaselineLease<Transform, Vector3, Quaternion, Vector3>(
                transform => transform.localPosition,
                transform => transform.localRotation,
                transform => transform.localScale,
                (transform, position) => transform.localPosition = position,
                (transform, rotation) => transform.localRotation = rotation,
                (transform, scale) => transform.localScale = scale,
                null,
                new BoundedVector3Comparer(0.0001f),
                new BoundedQuaternionComparer(0.01f),
                new BoundedVector3Comparer(0.0001f));
        }

        private sealed class LegBinding
        {
            public LegBinding(Transform thigh, Transform lowerLeg, Transform foot)
            {
                Thigh = thigh;
                LowerLeg = lowerLeg;
                Foot = foot;
            }

            public Transform Thigh { get; }

            public Transform LowerLeg { get; }

            public Transform Foot { get; }

            public bool IsHierarchyValid(Transform exactPelvis)
            {
                return exactPelvis != null && Thigh != null && LowerLeg != null && Foot != null &&
                    Thigh.IsChildOf(exactPelvis) && LowerLeg.IsChildOf(Thigh) && Foot.IsChildOf(LowerLeg);
            }
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
