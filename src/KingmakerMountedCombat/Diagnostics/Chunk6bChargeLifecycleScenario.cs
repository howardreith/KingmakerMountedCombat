using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints.Root;
using Kingmaker.UI.Selection;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        private NativeChargeSlowLease chargeSlow;
        private NativeChargeViewLease chargeView;
        private NativeRelationshipCommandProbe chargeDismount;
        private NativeModeTransitionProbe chargeModeChange;
        private JObject chargeBoundaryFacts, chargeSlowFacts;
        private bool chargeFeatureDisabled;
        private bool chargeOwnershipChanged;
        private JObject chargeLeaseFaultFacts;
        private JObject chargeNativeActionFaultFacts;
        private bool ChargeNativeActionFaultCase => Chunk6bChargeCaseId == "C6B-CHARGE-action-failed-before-rule" ||
            Chunk6bChargeCaseId == "C6B-CHARGE-action-failed-after-rule";
        private bool ChargeLeaseFaultCase => Chunk6bChargeCaseId == "C6B-CHARGE-lease-application-failed";

        private void ArmAdditionalChargeFault()
        {
            if (ChargeNativeActionFaultCase)
            {
                var expected = Chunk6bChargeCaseId == "C6B-CHARGE-action-failed-before-rule" ? "before-rule" : "after-rule";
                chargeNativeActionFaultFacts = new JObject { ["armed"] = true, ["fired"] = false, ["boundary"] = expected };
                MountedChargeAdmissionFault.NativeAction = boundary =>
                {
                    if (boundary != expected) return;
                    MountedChargeAdmissionFault.NativeAction = null;
                    chargeNativeActionFaultFacts["fired"] = true;
                    chargeNativeActionFaultFacts["frame"] = UnityEngine.Time.frameCount;
                    combat.ObserveChargeProcess(combat.OwnedChargeShell);
                    chargeNativeActionFaultFacts["ownerAtFault"] = combat.CaptureChargeOwnership();
                    chargeNativeActionFaultFacts["shellAtFault"] = CaptureNativeAbilityShell(combat.OwnedChargeShell);
                    chargeNativeActionFaultFacts["stateAtFault"] = CaptureChunk6bChargeActors("native-action-before-exception");
                    throw new InvalidOperationException("Diagnostic native charge action failure at " + boundary + ".");
                };
            }
            if (!ChargeLeaseFaultCase) return;
            chargeLeaseFaultFacts = new JObject { ["armed"] = true, ["fired"] = false };
            MountedChargeAdmissionFault.AfterLeaseAcquired = () =>
            {
                chargeLeaseFaultFacts["fired"] = true;
                chargeLeaseFaultFacts["ownerAtFault"] = combat.CaptureChargeOwnership();
                chargeLeaseFaultFacts["stateAtFault"] = CaptureChunk6bChargeActors("lease-acquired-before-fault");
                throw new InvalidOperationException("Diagnostic failure after all native charge lease mutations.");
            };
        }

        private string AdditionalChargeIntervention
        {
            get
            {
                switch (Chunk6bChargeCaseId)
                {
                    case "C6B-CHARGE-feature-disabled": return "feature-disabled";
                    case "C6B-CHARGE-child-cleanup": return "native-child-fact-cleanup";
                    case "C6B-CHARGE-dismounted": return "native-dismount-request";
                    case "C6B-CHARGE-mode-changed": return "native-mode-change";
                    case "C6B-CHARGE-duplicate": return "native-duplicate-request";
                    case "C6B-CHARGE-new-landing-blocker": return "native-new-landing-blocker";
                    case "C6B-CHARGE-relationship-invalidated": return "native-ownership-loss";
                    case "C6B-CHARGE-view-replaced": return "native-view-replacement";
                    case "C6B-CHARGE-mount-dead": return "native-mount-death";
                    case "C6B-CHARGE-rider-dead": return "native-rider-death";
                    default: return null;
                }
            }
        }

        private void PrepareChargeRangeCase()
        {
            if (Chunk6bChargeCaseId != "C6B-CHARGE-beyond-maximum" || chargeSlow != null) return;
            chargeSlow = new NativeChargeSlowLease(horse);
            chargeSlow.Apply();
            chargeSlowFacts = new JObject { ["applied"] = chargeSlow.Capture() };
            Chunk6bChargeMeasurement["beyondMaximumReachable"] = horse.CombatSpeedMps * 6f < Chunk6bChargeCaseDistance;
            Chunk6bChargeMeasurement["beyondMaximumFixture"] = "native-Slowed-fact-within-original-spawn-envelope";
        }

        private bool InterveneAdditionalCharge(string kind)
        {
            if (kind != AdditionalChargeIntervention || kind == null) return false;
            if (kind == "native-mount-death" || kind == "native-rider-death") return false;
            chargeBoundaryFacts = new JObject { ["kind"] = kind, ["ownerBefore"] = combat.CaptureChargeOwnership() };
            switch (kind)
            {
                case "native-child-fact-cleanup":
                    ApplyNativeChargeChildrenStimulus();
                    combat.LastMountedChargeCommand.Interrupt();
                    break;
                case "native-view-replacement":
                    chargeBoundaryFacts["nativeView"] = chargeView.Evidence;
                    chargeView.Apply();
                    break;
                case "feature-disabled":
                    settings.EnableMountedCharge = false;
                    chargeFeatureDisabled = true;
                    break;
                case "native-dismount-request":
                    SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                    if (allocationTrace == null) allocationTrace = new NativeActorAllocationTrace(rider, horse, combat);
                    allocationTrace.BeginEncounter(Chunk6bChargeCaseId);
                    chargeBoundaryFacts["beforeRequest"] = CaptureChunk6bChargeActors("before-dismount-request");
                    chargeDismount = new NativeRelationshipCommandProbe(nativeControls, allocationTrace, rider, horse,
                        nativeControls.DismountAbility.AssetGuid, CaptureChunk6aCausalState);
                    chargeBoundaryFacts["availability"] = nativeControls.Evaluate(NativeMountedControlKind.Dismount, rider).Reason;
                    var clicked = TryNativeAbilityTargetClick(nativeControls.DismountAbility, rider, "charge-pending-dismount");
                    chargeBoundaryFacts["clicked"] = clicked;
                    chargeDismount.ClickCompleted(clicked);
                    chargeBoundaryFacts["feedback"] = combat.LastFeedback;
                    break;
                case "native-mode-change":
                    chargeModeChange = new NativeModeTransitionProbe(true);
                    chargeModeChange.DispatchTemporaryValue();
                    chargeBoundaryFacts["nativeSettingAfter"] = chargeModeChange.CurrentValue;
                    break;
                case "native-duplicate-request":
                    chargeBoundaryFacts["admittedBefore"] = combat.MountedChargeAdmittedCount;
                    chargeBoundaryFacts["availableBefore"] = FindChunk6bChargeAbility()?.IsAvailableForCast;
                    chargeBoundaryFacts["clicked"] = TryNativeAbilityTargetClick(nativeControls.MountedChargeAbility, target, "charge-pending-duplicate");
                    chargeBoundaryFacts["input"] = observations["charge-pending-duplicate"]?.DeepClone();
                    chargeBoundaryFacts["admittedAfter"] = combat.MountedChargeAdmittedCount;
                    chargeBoundaryFacts["ownerAfterRequest"] = combat.CaptureChargeOwnership();
                    break;
                case "native-new-landing-blocker":
                    chargeBoundaryFacts["landingClearBefore"] = !MountedChargeGeometry.LandingBlocked(horse, rider, target);
                    chargeBoundaryFacts["spawnFrame"] = UnityEngine.Time.frameCount;
                    chargeBoundaryFacts["placed"] = PlaceChunk6bChargeLandingBlocker();
                    chargeBoundaryFacts["blocker"] = chunk6bChargeBlockerEvidence;
                    // Spawn returns before State admits the entity to AwakeUnits. Observe
                    // the engine's later admission; never insert it into that collection.
                    break;
                case "native-ownership-loss":
                    chunk6aOwnershipOriginal = CaptureChunk6aOwnership();
                    if (!Chunk6aOwnershipFixtureValid(chunk6aOwnershipOriginal))
                        throw new InvalidOperationException("Charge ownership fixture lacks reversible native reciprocal references.");
                    chunk6aOwnershipEvidence = new JObject { ["before"] = chunk6aOwnershipOriginal.DeepClone(), ["method"] = CaptureChunk6aSetMasterIdentity() };
                    chargeBoundaryFacts["nativeOwnership"] = chunk6aOwnershipEvidence;
                    chargeOwnershipChanged = true;
                    chunk6aOwnershipStimulusCount = 1;
                    try { horse.Descriptor.SetMaster(null); }
                    finally { chunk6aOwnershipDetached = rider.Descriptor.Pet == null || horse.Descriptor.Master.Value == null; }
                    chunk6aOwnershipEvidence["detached"] = CaptureChunk6aOwnership();
                    if (!Chunk6aOwnershipDetachedValid((JObject)chunk6aOwnershipEvidence["detached"]))
                        throw new InvalidOperationException("Native ownership loss changed unrelated fixture inputs.");
                    break;
            }
            return true;
        }

        private void ObserveChargeLandingBlocker()
        {
            if (chargeBoundaryFacts == null || (string)chargeBoundaryFacts["kind"] != "native-new-landing-blocker" ||
                chargeBoundaryFacts["blockersAfter"] != null || chunk6bChargeBlocker == null ||
                !Game.Instance.State.AwakeUnits.Any(actor => ReferenceEquals(actor, chunk6bChargeBlocker))) return;
            chargeBoundaryFacts["awakeFrame"] = UnityEngine.Time.frameCount;
            chargeBoundaryFacts["awakeBlocker"] = chunk6bChargeBlocker.UniqueId;
            chargeBoundaryFacts["blockersAfter"] = DescribeChunk6bChargeLandingBlockers(target.Position, target.View.Corpulence);
        }

        private void ApplyNativeChargeChildrenStimulus()
        {
            // Explicit native-lifetime stimulus, not a grant of COTW feats/rage,
            // and not evidence of their eligibility or attack consequences. It
            // exercises the exact loaded children through native AddBuff/StoreFact
            // while the ordinary player-requested charge owns the root context.
            var blueprint = BlueprintRoot.Instance.SystemMechanics.ChargeBuff;
            var root = rider.Buffs.Enumerable.Single(buff => ReferenceEquals(buff.Blueprint, blueprint));
            var surface = MountedChargeBuffSurface.Read(blueprint);
            if (surface.Children.Length != 3) throw new InvalidOperationException("Child cleanup fixture requires the inspected loaded COTW graph.");
            var facts = new JArray();
            var stimulus = new JObject
            {
                ["contract"] = "native-lifetime-stimulus-no-feat-or-action-grant", ["rider"] = rider.UniqueId,
                ["rootIdentity"] = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(root),
                ["children"] = facts, ["allAcquired"] = false
            };
            chargeBoundaryFacts["childStimulus"] = stimulus;
            foreach (var childBlueprint in surface.Children)
            {
                if (rider.Buffs.Enumerable.Any(buff => ReferenceEquals(buff.Blueprint, childBlueprint)))
                    throw new InvalidOperationException("Child stimulus cannot replace a preexisting native fact.");
                var child = rider.Buffs.AddBuff(childBlueprint, root.Context, null);
                // The final child deliberately models an interruption after its
                // native add returns but before the parent StoreFact call. The
                // production acquisition owner must still retain and remove it.
                var stored = facts.Count < 2;
                if (stored) root.StoreFact(child);
                facts.Add(new JObject { ["blueprint"] = childBlueprint.AssetGuid,
                    ["identity"] = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(child),
                    ["storedByParent"] = stored, ["active"] = child.Active,
                    ["sameContextParent"] = ReferenceEquals(child.Context.ParentContext, root.Context) });
            }
            stimulus["allAcquired"] = true;
            stimulus["ownerWithChildren"] = combat.CaptureChargeOwnership();
        }

        private void ObserveAdditionalChargeSettlement()
        {
            if (chargeBoundaryFacts != null)
            {
                chargeBoundaryFacts["ownerAfter"] = combat.CaptureChargeOwnership();
                chargeBoundaryFacts["lastDrained"] = combat.LastDrainedChargeOwnership?.DeepClone();
                chargeBoundaryFacts["relationshipAfter"] = relationship.State.ToString();
                chargeBoundaryFacts["controlAfter"] = CaptureChunk6bChargeAbilityIdentity();
                if (chargeOwnershipChanged && !RestoreChargeNativeOwnership(false))
                    throw new InvalidOperationException("Native ownership restoration remains unresolved.");
                if (chargeModeChange != null)
                {
                    chargeBoundaryFacts["tbInitialized"] = Game.Instance.TurnBasedCombatController.Initialized;
                    chargeModeChange.Dispose();
                    chargeBoundaryFacts["modeRestored"] = chargeModeChange.RestoreDeliveryCompleted && chargeModeChange.PersistedValueUnchanged;
                    chargeModeChange = null;
                }
            }
            if (chargeSlow != null)
            {
                chargeSlowFacts["beforeRestore"] = chargeSlow.Capture();
                chargeSlow.Dispose();
                chargeSlowFacts["afterRestore"] = chargeSlow.Capture();
                chargeSlow = null;
            }
            if (chargeLeaseFaultFacts != null)
            {
                chargeLeaseFaultFacts["seamCleared"] = MountedChargeAdmissionFault.AfterLeaseAcquired == null;
                MountedChargeAdmissionFault.AfterLeaseAcquired = null;
            }
            if (chargeNativeActionFaultFacts != null)
            {
                chargeNativeActionFaultFacts["seamCleared"] = MountedChargeAdmissionFault.NativeAction == null;
                MountedChargeAdmissionFault.NativeAction = null;
            }
        }

        private bool RestoreChargeViewWhenSettled()
        {
            if (chargeView == null) return true;
            if (combat.HasChargeOwnership || combat.HasActiveCommand || relationship.State != RelationshipState.Unmounted) return false;
            var settled = chargeView.RestoreWhenSettled();
            chargeBoundaryFacts["nativeView"] = chargeView.Evidence;
            chargeBoundaryFacts["presentationResidue"] = relationship.Runtime.HasPresentationAttachmentResidue;
            return settled;
        }

        private bool ObserveChargeDismountSettlement()
        {
            if (chargeDismount == null) return true;
            chargeBoundaryFacts["dismountProgress"] = chargeDismount.Capture();
            if (!chargeDismount.Terminal || combat.HasChargeOwnership || relationship.State != RelationshipState.Unmounted ||
                !rider.Commands.Empty || !horse.Commands.Empty) return false;
            chargeBoundaryFacts["dismountProof"] = chargeDismount.Finish(true, false, 0, false);
            chargeDismount.Dispose(); chargeDismount = null;
            return true;
        }

        private bool RestoreChargeNativeOwnership(bool cleanup)
        {
            if (!chargeOwnershipChanged) return true;
            if (JToken.DeepEquals(chunk6aOwnershipOriginal, CaptureChunk6aOwnership())) chunk6aOwnershipDetached = false;
            if (!RestoreChunk6aOwnership(cleanup)) return false;
            chargeOwnershipChanged = false;
            return true;
        }

        private void ResetAdditionalChargeCase()
        {
            var errors = new List<Exception>();
            MountedChargeAdmissionFault.AfterLeaseAcquired = null;
            MountedChargeAdmissionFault.NativeAction = null;
            try { chargeDismount?.Dispose(); chargeDismount = null; } catch (Exception e) { errors.Add(e); }
            try { chargeView?.Dispose(); chargeView = null; } catch (Exception e) { errors.Add(e); }
            try { CleanupChargeDeath(); } catch (Exception e) { errors.Add(e); }
            try { if (!RestoreChargeNativeOwnership(true)) throw new InvalidOperationException("Native reciprocal ownership remains unresolved."); } catch (Exception e) { errors.Add(e); }
            try { chargeModeChange?.Dispose(); chargeModeChange = null; } catch (Exception e) { errors.Add(e); }
            try { chargeSlow?.Dispose(); chargeSlow = null; } catch (Exception e) { errors.Add(e); }
            try
            {
                if (chargeFeatureDisabled) { settings.EnableMountedCharge = true; nativeControls.Update(); }
                chargeFeatureDisabled = false;
            }
            catch (Exception e) { errors.Add(e); }
            if (errors.Count != 0) throw new AggregateException("Additional charge fixture restoration remains owned.", errors);
            chargeBoundaryFacts = null; chargeSlowFacts = null;
            chargeLeaseFaultFacts = null;
            chargeNativeActionFaultFacts = null;
        }
    }
}
