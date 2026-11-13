using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints.Area;
using Kingmaker.EntitySystem.Persistence;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Integration
{
    internal sealed partial class MountedPersistenceService
    {
        private AreaTransfer areaTransfer;
        private AreaTransfer failedAreaTransfer;
        internal int FailedAreaPairCount => failedAreaTransfer?.Pairs?.Length ?? 0;
        private string areaRestoreFailure;
        internal bool AreaTransitionPending => areaTransfer != null;
        internal int AreaSuspensionCount { get; private set; }
        internal int AreaResumeCount { get; private set; }
        internal int AreaRetiredPairCount { get; private set; }
        internal int RefusedAreaTransferCount { get; private set; }

        // Area TRAVEL in the same world (a door, a world-map arrival, a scripted area change): the 0.2.4 terms in their
        // exact order and with the same lazy evaluation; the first refusing term is now named in the log.
        internal bool AdmitChargeAreaTransition()
        {
            string participant = null;
            var refusal = MountedWorldBoundaryAdmissionPolicy.DecideTravel(areaRestoreFailure != null,
                () => HasAreaParticipationObligations(out participant), HasActiveSaveScope, HasSaveAdmissionFence, AnyChargeFence,
                () => DrainWorldCharge("area transition admission"));
            if (refusal == MountedWorldBoundaryRefusal.None) return true;
            RefusedAreaTransferCount++;
            Report("Area change refused: a mounted activation, pending survivor, command, cleanup or save still owns this world boundary.");
            TraceBoundary("Mounted world boundary refused: operation=area-travel; entry=Game.LoadArea(06000CD5, saveInfo=null); reason=" +
                refusal + " (" + MountedWorldBoundaryAdmissionPolicy.Describe(refusal) + ")" + (participant != null ? "; " + participant : "") +
                "; " + BoundaryStateDetail() + ".");
            return false;
        }

        // Save/load repair: Game.LoadGame (06000CE0) admitted this exact descriptor in its prefix and calls the full LoadArea
        // synchronously with it, so the binding is the same object within the same frame. Anything else re-runs the
        // selected-load admission here.
        private WeakReference admittedLoadSave;
        private int admittedLoadFrame = -1;
        internal int SavedWorldReplacementCount { get; private set; }
        internal int RefusedSavedWorldReplacementCount { get; private set; }

        internal void BindAdmittedLoad(SaveInfo save)
        {
            admittedLoadSave = save == null ? null : new WeakReference(save);
            admittedLoadFrame = UnityEngine.Time.frameCount;
        }

        // A saved-world REPLACEMENT through the full LoadArea (saveInfo != null). It never applies the exploration travel
        // guard, and it releases nothing itself (independent review F1/F2): the established load path releases the
        // outgoing world (OnAreaBeginUnloading cleanup, then WrapLoadRoutine, which retires the previous load scope before
        // BeginLoadHousekeeping). Archive workers, a queued save, a charge fence, retained charge cleanup on any pair and
        // the selected archive's own admission still refuse.
        internal bool AdmitSavedWorldReplacement(BlueprintArea area, SaveInfo save)
        {
            var bound = admittedLoadSave != null && ReferenceEquals(admittedLoadSave.Target, save) &&
                admittedLoadFrame == UnityEngine.Time.frameCount;
            admittedLoadSave = null; admittedLoadFrame = -1;
            var game = Game.Instance;
            var detail = "operation=saved-world-replacement; entry=Game.LoadArea(06000CD5, saveInfo); save=" + (save?.Name ?? "none") +
                "; selectedCampaign=" + (save?.GameId ?? "none") + "; selectedArea=" + (save?.Area?.AssetGuidThreadSafe ?? "none") +
                "; outgoingCampaign=" + (game?.Player?.GameId ?? "none") + "; outgoingArea=" +
                (game?.CurrentlyLoadedArea?.AssetGuidThreadSafe ?? "none") + "; admittedByLoadGame=" + bound +
                "; inCombat=" + (game?.Player?.IsInCombat ?? false) + "; turnBased=" + TurnBased.Controllers.CombatController.IsInTurnBasedCombat() +
                "; activation=" + (unifiedTurn.ActivationIdentity ?? "none");
            var before = BoundaryStateDetail();
            var refusal = DecideSavedWorldReplacement(save, bound);
            if (refusal != MountedWorldBoundaryRefusal.None)
            {
                RefusedSavedWorldReplacementCount++;
                TraceBoundary("Mounted world boundary refused: " + detail + "; reason=" + refusal + " (" +
                    MountedWorldBoundaryAdmissionPolicy.Describe(refusal) + "); " + before + "; chargeOwnersAfterDrain=" + RetainedChargeOwnerCount + ".");
                // The selected-load admission reports its own refusal; every other refusal is reported here once.
                if (refusal != MountedWorldBoundaryRefusal.SelectedLoadRefused)
                {
                    var message = "Load canceled: " + MountedWorldBoundaryAdmissionPolicy.Describe(refusal) + "; try again once it finishes.";
                    Report(message);
                    try { Kingmaker.PubSubSystem.EventBus.RaiseEvent<Kingmaker.PubSubSystem.IWarningNotificationUIHandler>(
                        handler => handler.HandleWarning(message, true)); }
                    catch (Exception error) { logger.Exception("Mounted load warning display failed", error); }
                }
                return false;
            }
            SavedWorldReplacementCount++;
            BeginAreaTransition(area, save, true);
            TraceBoundary("Mounted world boundary admitted: " + detail + "; " + before +
                "; the outgoing mounted world is released by the native unload and load housekeeping.");
            return true;
        }

        // The admission decision itself, free of frame and game-instance reads so the contract probe can drive it over real
        // pair controllers. Every pair's charge custody is drained (not only the primary's, and not only a fenced one);
        // a drain that throws refuses the load rather than letting the native unload begin over unresolved custody.
        private MountedWorldBoundaryRefusal DecideSavedWorldReplacement(SaveInfo save, bool bound) =>
            MountedWorldBoundaryAdmissionPolicy.DecideSavedReplacement(() => bound || CanLoadBeforeWorldReplacement(save),
                HasActiveSaveScope, SaveDraining, HasSaveAdmissionFence, AnyChargeFence,
                () => TryDrainWorldChargeForLoad("saved-world replacement admission"));

        private bool TryDrainWorldChargeForLoad(string reason)
        {
            try { return DrainWorldCharge(reason); }
            catch (Exception error)
            {
                try { logger.Exception("Mounted charge cleanup failed during load admission", error); } catch { }
                return false;
            }
        }

        // Boundary diagnostics never change the boundary decision: the admitted line is written after the transition
        // began, and the logger itself has no exception guard (review of PR #31).
        private void TraceBoundary(string message)
        {
            try { logger.Info(message); } catch { }
        }

        private int RetainedChargeOwnerCount => WorldCombats.Count(c => c.HasChargeOwnership);

        private string BoundaryStateDetail() =>
            "transfer=" + (areaTransfer?.Lifecycle.Describe() ?? "none") + "; restoreFailure=" + (areaRestoreFailure != null) +
            "; saveScope=" + HasActiveSaveScope + "; saveFence=" + HasSaveAdmissionFence + "; saveDraining=" + SaveDraining +
            "; saveSuspended=" + SaveSuspended + "; chargeFence=" + AnyChargeFence + "; chargeOwners=" + RetainedChargeOwnerCount + "; mode=" + (Game.Instance?.CurrentMode.ToString() ?? "none");

        private bool HasAreaParticipationObligations() { string ignored; return HasAreaParticipationObligations(out ignored); }

        // Every participant is still visited (the settlement's side effects are unchanged); the first unsettled one is named.
        private bool HasAreaParticipationObligations(out string first)
        {
            first = null;
            var unsettled = false;
            foreach (var participant in unifiedTurn.SnapshotParticipants)
            {
                var participation = participant.TrySettleExplorationParticipation();
                var shell = controls.HasUnsettledShellFor(participant.Relationship);
                if (participation && !shell) continue;
                unsettled = true;
                if (first != null) continue;
                var relationship = participant.Relationship;
                first = "participant=" + (relationship?.Rider?.UniqueId ?? "none") + "/" + (relationship?.Mount?.UniqueId ?? "none") +
                    "; owner=" + (relationship?.PairOwner?.Id.ToString("N") ?? "none") + ":" + (relationship?.PairOwner?.Generation ?? 0) +
                    "; participationSettled=" + participation + "; controlShellUnsettled=" + shell +
                    "; riderInCombat=" + (relationship?.Rider?.IsInCombat ?? false) + "; mountInCombat=" + (relationship?.Mount?.IsInCombat ?? false) +
                    "; activation=" + (unifiedTurn.ActivationIdentity ?? "none");
            }
            return unsettled;
        }

        // nativeDispatch: true from the admitted full Game.LoadArea (06000CD5) prefix, false from the party-leave event
        // that precedes the native dispatch (Ratnook door repair: the dispatch may instead be a same-area Game.Teleport).
        internal void BeginAreaTransition(BlueprintArea area, SaveInfo saveInfo, bool nativeDispatch, BlueprintAreaEnterPoint enterPoint = null)
        {
            var world = Game.Instance?.Player;
            if (saveInfo != null) { CancelAreaTransition(); DropRetainedIntent("a save is being loaded"); return; }
            // The same unfinished transfer: a full-load dispatch only records its route. A transfer that already attempted
            // restoration is finished, so a newer load of the same area starts its own transfer instead of inheriting it.
            if (areaTransfer != null && areaTransfer.World.Target == world && areaTransfer.AreaId == area?.AssetGuidThreadSafe &&
                !areaTransfer.Lifecycle.ResumeAttempted)
            {
                if (nativeDispatch && areaTransfer.Lifecycle.ObserveFullLoadDispatch()) TraceAreaTransfer(areaTransfer, "full area load dispatched");
                return;
            }
            CancelAreaTransition();
            if (restoreLoad != null && (!restoreLoad.World.NativeCompleted || presentationPending))
            {
                FenceAbandonedCombat(); restoreLoad.World.Close(); restoreLoad = null;
                presentationPending = false; restoredActors.Clear(); loaded = null; ClearArchiveRestoration(); loadSequence++;
            }
            if (!Enabled || world == null || area == null) return;
            if (HasAreaParticipationObligations())
                throw new InvalidOperationException("Area transfer cannot discard retained native participation.");
            var mounted = WorldRelationships.Where(r => r.State == RelationshipState.Mounted).ToArray();
            // Exploration repair (issue 7): intent retained by world-map travel or by a scripted event in this world
            // is carried by the transfer like the pairs mounted at departure; an area change never discards it.
            var carried = retainedIntent != null && ReferenceEquals(retainedIntent.World.Target, world) &&
                retainedIntent.CampaignId == world.GameId ? retainedIntent : null;
            if (retainedIntent != null && carried == null) DropRetainedIntent("the world changed");
            if (mounted.Length == 0 && carried == null) return;
            if (world.IsInCombat || mounted.Any(r => r.Rider.IsInCombat || r.Mount.IsInCombat))
            {
                if (carried != null) DropRetainedIntent("combat began");
                return;
            }
            if (SaveDraining || SaveSuspended || HasSaveAdmissionFence)
            {
                RefusedAreaTransferCount++;
                if (carried != null) DropRetainedIntent("a mounted save is still owned");
                Report("A mounted save is still owned; pairs are not carried across this area change.");
                return;
            }
            var departing = mounted.Select(r => new AreaPair { RiderId = r.Rider.UniqueId, MountId = r.Mount.UniqueId,
                ProfileId = r.Runtime.MountProfileId, OwnerId = r.PairOwner?.Id.ToString("N"),
                Generation = r.PairOwner?.Generation ?? 0, Participation = unifiedTurn.SnapshotParticipants
                    .Single(p => ReferenceEquals(p.Relationship, r)).CaptureParticipation() }).ToArray();
            var departingIds = new HashSet<string>(departing.SelectMany(p => new[] { p.RiderId, p.MountId }), StringComparer.Ordinal);
            var carriedPairs = (carried?.Pairs ?? new AreaPair[0])
                .Where(p => !departingIds.Contains(p.RiderId) && !departingIds.Contains(p.MountId))
                .Select(p => new AreaPair { RiderId = p.RiderId, MountId = p.MountId, ProfileId = p.ProfileId, OwnerId = p.OwnerId,
                    Generation = p.Generation, Participation = p.Participation, Carried = true }).ToArray();
            var slots = controls.CapturePersistentSlots();
            // Third owner review: merge live and retained control rows per logical row, never per actor.
            var mergedSlots = MountedRetainedIntentRecord.MergeSlotRows(
                slots.Select(s => new MountedRetainedIntentSlotRecord { ActorId = s.ActorId, Index = s.Index, Kind = s.Kind }),
                (carried?.Slots ?? new SavedMountedSlot[0]).Select(s => new MountedRetainedIntentSlotRecord { ActorId = s.ActorId, Index = s.Index, Kind = s.Kind }))
                .Select(s => new SavedMountedSlot { ActorId = s.ActorId, Index = s.Index, Kind = s.Kind }).ToArray();
            areaTransfer = new AreaTransfer {
                Lifecycle = new MountedAreaTransferLifecycle(nativeDispatch ? MountedAreaTransferRoute.FullLoad : MountedAreaTransferRoute.Requested),
                Sequence = ++areaTransferSequence, DepartureAreaId = Game.Instance.CurrentlyLoadedArea?.AssetGuidThreadSafe,
                EnterPointId = enterPoint?.AssetGuidThreadSafe,
                World = new WeakReference(world), CampaignId = world.GameId, AreaId = area.AssetGuidThreadSafe,
                Pairs = departing.Concat(carriedPairs).ToArray(),
                TurnBased = TurnBased.Controllers.CombatController.IsInTurnBasedCombat(),
                Slots = mergedSlots,
                WorldMap = IsWorldMapArea(area),
                CarriedKind = carried != null ? retainedIntentKind : MountedRetainedIntentKind.WorldMapTravel
            };
            if (carried != null) { retainedIntent = null; ResetRetainedSettle(); }
            TraceAreaTransfer(areaTransfer, nativeDispatch ? "began at full area load" : "began at party leave");
        }

        internal bool SuspendAreaPair()
        {
            var transfer = areaTransfer;
            if (transfer == null || !Enabled || transfer.World.Target != Game.Instance?.Player) return false;
            if (transfer.Lifecycle.Suspended) return true;
            try
            {
                if (!DrainWorldCharge("area suspension"))
                    throw new InvalidOperationException("Area suspension retained charge cleanup ownership.");
                if (HasAreaParticipationObligations())
                    throw new InvalidOperationException("Area suspension encountered retained native participation.");
                // Ordinary exploration area transfer does NOT clear the shared
                // actor allocation ledger. Native encounter/actor lifetime owns
                // its retirement; attachment cleanup cannot erase expenditure.
                controls.SuspendAreaControls();
                var errors = new List<Exception>();
                foreach (var owner in WorldRelationships)
                    try
                    {
                        if (!owner.GuardBoundary(CleanupTrigger.AreaSuspension))
                            errors.Add(new InvalidOperationException("Area suspension retained mounted references."));
                    }
                    catch (Exception error) { errors.Add(error); }
                if (errors.Count != 0) throw new AggregateException(errors);
                mountedWorld?.DiscardAdmissionBindings();
                transfer.Lifecycle.ObserveSuspended(); AreaSuspensionCount++;
                Report("All " + transfer.Pairs.Length + " mounted relationships suspended for native area placement.");
                return true;
            }
            catch (Exception error)
            {
                failedAreaTransfer = transfer;
                areaRestoreFailure = "Mounted area suspension failed: " + error.Message;
                Report(areaRestoreFailure + " Original transfer custody is retained; new participation and saving remain blocked.");
                CancelAreaTransition();
                throw;
            }
        }

        internal void RestoreAreaPair()
        {
            var transfer = areaTransfer;
            if (transfer == null || !transfer.Lifecycle.BeginRestore()) return;
            var game = Game.Instance;
            try
            {
                if (!Enabled || transfer.World.Target != game?.Player || game.Player.GameId != transfer.CampaignId ||
                    game.CurrentlyLoadedArea?.AssetGuidThreadSafe != transfer.AreaId)
                    throw new InvalidOperationException("Area world changed before mounted restoration.");
                // Exploration repair (issue 7): the world map has no physical rider-and-mount placement (native
                // GlobalMapRules enters GlobalMap mode during scene init, so attachment is never admitted there).
                // The suspended pairs travel as retained intent and restore at the next local area's arrival;
                // nothing is latched, so saving and area entry stay admitted.
                if (transfer.WorldMap)
                {
                    // Review finding 2: only the carried pairs' own control rows travel; another unit's row that later
                    // fails to resolve must never be able to latch the all-pair failure at the next arrival.
                    var pairActors = new HashSet<string>(transfer.Pairs.SelectMany(p => new[] { p.RiderId, p.MountId }), StringComparer.Ordinal);
                    retainedIntent = new AreaTransfer { World = transfer.World, CampaignId = transfer.CampaignId, AreaId = transfer.AreaId,
                        Pairs = transfer.Pairs.Select(p => new AreaPair { RiderId = p.RiderId, MountId = p.MountId, ProfileId = p.ProfileId,
                            OwnerId = p.OwnerId, Generation = p.Generation, Participation = p.Participation, Carried = true }).ToArray(),
                        Slots = transfer.Slots.Where(s => pairActors.Contains(s.ActorId)).ToArray() };
                    retainedIntentKind = MountedRetainedIntentKind.WorldMapTravel;
                    ResetRetainedSettle();
                    areaTransfer = null;
                    RetainedSuspensionCount++;
                    Report(transfer.Pairs.Length + " mounted relationship" + (transfer.Pairs.Length == 1 ? "" : "s") +
                        " carried across the world map; the pair remounts on arrival in the next area.", true);
                    return;
                }
                var carriedDropped = new List<string>();
                var carriedDroppedPairs = new List<AreaPair>();
                // The uncertain case of the review: a carried refusal that may be transient (a mode or agent not settled
                // at OnAreaLoaded, an earlier owner identity not yet retired) is retained for the in-area retry.
                var carriedRetained = new List<AreaPair>();
                var arrivalInCombat = game.Player.IsInCombat;
                // Resolve every pair and slot before changing the first attachment.
                var resolved = new Dictionary<string, Kingmaker.EntitySystem.Entities.UnitEntityData>(StringComparer.Ordinal);
                foreach (var id in transfer.Pairs.SelectMany(p => new[] { p.RiderId, p.MountId })
                    .Concat(transfer.Slots.Select(s => s.ActorId)).Distinct(StringComparer.Ordinal))
                {
                    var matches = game.State.Units.Concat(game.Player.AllCharacters).Where(u => u != null && u.UniqueId == id).Distinct().ToArray();
                    if (matches.Length == 1) { resolved.Add(id, matches[0]); continue; }
                    // A carried pair (intent retained over the world map or an earlier event) whose actor is gone stays
                    // dismounted on its own; it never blocks the other pairs or later area changes. A control row of
                    // such an actor is skipped the same way (RestorePersistentSlots ignores unresolved actors).
                    if (transfer.Pairs.Any(p => p.Carried && (p.RiderId == id || p.MountId == id)) &&
                        !transfer.Pairs.Any(p => !p.Carried && (p.RiderId == id || p.MountId == id))) continue;
                    throw new InvalidOperationException("An area actor did not resolve uniquely: " + id);
                }
                // Admission is all-pair: a destination that cannot directly
                // control the second pair must not attach the first pair. The one
                // exception is the native capital rule (owner decision): that pair is
                // retired, dismounted, after its separation is observed, and the rest restore.
                var area = game.CurrentlyLoadedArea;
                var restore = new List<AreaPair>(); var retire = new List<AreaPair>();
                foreach (var pair in transfer.Pairs)
                {
                    if (pair.Carried)
                    {
                        Kingmaker.EntitySystem.Entities.UnitEntityData carriedRider, carriedMount;
                        resolved.TryGetValue(pair.RiderId, out carriedRider); resolved.TryGetValue(pair.MountId, out carriedMount);
                        var present = carriedRider != null && carriedMount != null;
                        string carriedRefusal = null;
                        var sameProfile = false;
                        var permanent = false;
                        var capitalRetirement = false;
                        try
                        {
                            if (present)
                            {
                                sameProfile = SupportedMountedProfiles.Resolve(carriedRider, carriedMount)?.Id == pair.ProfileId;
                                var carriedCandidate = KingmakerMountedPairRuntime.CreateCandidate(carriedRider, carriedMount);
                                permanent = PermanentlyIneligible(carriedCandidate);
                                carriedRefusal = carriedCandidate?.ValidateSavedRelationship();
                                capitalRetirement = carriedRefusal != null &&
                                    IsCapitalControlRetirement(area, carriedRider, carriedMount, carriedCandidate, carriedRefusal);
                            }
                        }
                        catch (Exception classification) { carriedRefusal = classification.Message; }
                        var arrival = MountedRetainedIntentPolicy.DecideCarriedArrival(arrivalInCombat, present, sameProfile, permanent,
                            carriedRefusal, capitalRetirement);
                        if (arrival == MountedCarriedArrivalDecision.Restore) restore.Add(pair);
                        // The established capital retirement, with its clean-separation checks below; never pending intent.
                        else if (arrival == MountedCarriedArrivalDecision.Retire) retire.Add(pair);
                        else if (arrival == MountedCarriedArrivalDecision.Retain) carriedRetained.Add(pair);
                        else
                        {
                            carriedDroppedPairs.Add(pair);
                            carriedDropped.Add((carriedRider?.CharacterName ?? pair.RiderId) + " and " + (carriedMount?.CharacterName ?? pair.MountId) +
                                " (" + (!present ? "an actor is not present" : !sameProfile ? "the mount profile changed" :
                                    arrivalInCombat ? "the party arrived in combat" : carriedRefusal ?? "the pair is no longer eligible") + ")");
                        }
                        continue;
                    }
                    var rider = resolved[pair.RiderId]; var mount = resolved[pair.MountId];
                    var profile = SupportedMountedProfiles.Resolve(rider, mount);
                    var candidate = KingmakerMountedPairRuntime.CreateCandidate(rider, mount);
                    var refusal = candidate?.ValidateSavedRelationship();
                    if (profile?.Id != pair.ProfileId)
                        throw new InvalidOperationException("Area relationship was not eligible: The saved mount profile changed.");
                    if (refusal == null) { restore.Add(pair); continue; }
                    if (!IsCapitalControlRetirement(area, rider, mount, candidate, refusal))
                        throw new InvalidOperationException("Area relationship was not eligible: " + refusal);
                    retire.Add(pair);
                }
                foreach (var pair in retire)
                {
                    var residue = RetiredAreaPairResidue(pair, resolved[pair.RiderId], resolved[pair.MountId]);
                    if (residue != null) throw new InvalidOperationException("Capital-arrival retirement was not clean: " + residue);
                }
                foreach (var pair in restore)
                {
                    if (pair.Carried)
                    {
                        // Retained intent (world-map travel, an earlier event or a loaded record): the established import
                        // lifecycle with the captured owner identity (review findings 1 and 4). A clean refusal is retried
                        // in ordinary play; an attempt that left owned state is never reported as a clean dismount.
                        string carriedDetail;
                        var carriedOutcome = RestoreRetainedPair(pair, resolved[pair.RiderId], resolved[pair.MountId], resolved,
                            transfer.TurnBased, out carriedDetail);
                        if (carriedOutcome == MountedRetainedRestoreOutcome.RetainedResidue)
                            throw new InvalidOperationException("Carried mounted restoration left owned state for " +
                                resolved[pair.RiderId].CharacterName + " and " + resolved[pair.MountId].CharacterName + ": " + carriedDetail);
                        if (carriedOutcome == MountedRetainedRestoreOutcome.CleanRefusal) { carriedRetained.Add(pair); continue; }
                        AreaResumeCount++; RetainedRestoreCount++;
                        continue;
                    }
                    var owner = ArchiveNativeEnabled ? mountedWorld.CreateRestoredBoundary().Relationship : relationship;
                    if (ArchiveNativeEnabled)
                        owner.ImportSavedOwnership(resolved[pair.RiderId], resolved[pair.MountId], pair.OwnerId, pair.Generation, true);
                    // Native OnAreaLoaded completed placement; this hook still
                    // precedes after-entry autosaving for every attachment.
                    var result = owner.RestoreSavedPair(resolved[pair.RiderId], resolved[pair.MountId], pair.ProfileId);
                    if (!result.Succeeded) throw new InvalidOperationException("Area relationship was not eligible: " + string.Join("; ", result.Errors));
                    var participant = unifiedTurn.SnapshotParticipants.Single(p => ReferenceEquals(p.Relationship, owner));
                    participant.RestoreParticipation(pair.Participation, resolved, transfer.TurnBased);
                    owner.CompleteSavedParticipationImport();
                    AreaResumeCount++;
                }
                controls.ResumeAreaControls();
                // A carried pair that stayed dismounted (dropped or retained for retry) keeps its current control bindings.
                var droppedActors = new HashSet<string>(carriedDroppedPairs.Concat(carriedRetained)
                    .SelectMany(p => new[] { p.RiderId, p.MountId }), StringComparer.Ordinal);
                if (carriedRetained.Count != 0)
                {
                    var retainedActors = new HashSet<string>(carriedRetained.SelectMany(p => new[] { p.RiderId, p.MountId }), StringComparer.Ordinal);
                    retainedIntent = new AreaTransfer { World = transfer.World, CampaignId = transfer.CampaignId, AreaId = transfer.AreaId,
                        Pairs = carriedRetained.ToArray(), Slots = transfer.Slots.Where(s => retainedActors.Contains(s.ActorId)).ToArray() };
                    retainedIntentKind = transfer.CarriedKind;
                    ResetRetainedSettle();
                }
                controls.RestorePersistentSlots(transfer.Slots.Where(s => !droppedActors.Contains(s.ActorId)).ToArray());
                AreaRetiredPairCount += retire.Count;
                RetainedDropCount += carriedDropped.Count;
                if (carriedDropped.Count != 0)
                {
                    Report("Stayed dismounted on arrival: " + string.Join(", ", carriedDropped.ToArray()) + ". Mount again when ready. " +
                        (transfer.Pairs.Length - carriedDropped.Count - carriedRetained.Count - retire.Count) + " of " + transfer.Pairs.Length +
                        " mounted relationships restored without Mount or preparation.");
                    return;
                }
                if (carriedRetained.Count != 0 && retire.Count == 0)
                {
                    Report((transfer.Pairs.Length - carriedRetained.Count) + " of " + transfer.Pairs.Length +
                        " mounted relationships restored on arrival without Mount or preparation; " + carriedRetained.Count +
                        " will remount when ordinary play settles.", true);
                    return;
                }
                // Report keeps one feedback line, so the dismounted pairs are named in the summary itself.
                Report(retire.Count == 0
                    ? "All " + transfer.Pairs.Length + " mounted relationships restored before area autosave without Mount or preparation."
                    : "Dismounted on arrival because the companion is not directly controllable in this area: " +
                        string.Join(", ", retire.Select(pair => resolved[pair.RiderId].CharacterName + " and " + resolved[pair.MountId].CharacterName).ToArray()) +
                        ". Mount again after leaving. " + restore.Count + " of " + transfer.Pairs.Length +
                        " mounted relationships restored without Mount or preparation.");
            }
            catch (Exception error)
            {
                failedAreaTransfer = transfer;
                areaRestoreFailure = "All-pair area restoration failed: " + error.Message;
                Report(areaRestoreFailure + " Saving and new native participation remain blocked; load a valid save to resume.");
            }
            finally { controls.ResumeAreaControls(); }
        }

        // Narrow to the native capital rule: every other refusal keeps the all-pair failure.
        // The hypothetical re-validation only asks whether anything else would refuse; it grants nothing.
        private static bool IsCapitalControlRetirement(BlueprintArea area, Kingmaker.EntitySystem.Entities.UnitEntityData rider,
            Kingmaker.EntitySystem.Entities.UnitEntityData mount, MountedPairCandidate candidate, string refusal)
        {
            if (area == null || candidate == null) return false;
            var main = Game.Instance?.Player?.MainCharacter.Value;
            bool MainOrMainPet(Kingmaker.EntitySystem.Entities.UnitEntityData unit) =>
                main != null && (unit == main || unit.Descriptor?.Master.Value == main);
            var assumed = KingmakerMountedPairRuntime.CreateCandidate(rider, mount);
            if (assumed == null) return false;
            assumed.RiderIsDirectlyControllable = true; assumed.MountIsDirectlyControllable = true;
            return MountedAreaControlRule.IsCapitalControlRetirement(area.IsCapital, refusal, assumed.ValidateSavedRelationship(),
                candidate.RiderIsDirectlyControllable, MainOrMainPet(rider), candidate.MountIsDirectlyControllable, MainOrMainPet(mount));
        }

        // Observed separation for a retired pair: no active owner, no world relationship holding
        // either unit or any owned residue, settled saved participation, distinct live views at
        // finite positions and no installed agent override. Any residue fails closed.
        private string RetiredAreaPairResidue(AreaPair pair, Kingmaker.EntitySystem.Entities.UnitEntityData rider,
            Kingmaker.EntitySystem.Entities.UnitEntityData mount)
        {
            var p = pair.Participation;
            if (p != null && (p.Activation != null || p.Boundary != null || p.Partner != null || p.PendingSplitId != null))
                return "saved participation still carries obligations";
            if (mountedWorld != null && (mountedWorld.Registry.FindActive(rider) != null || mountedWorld.Registry.FindActive(mount) != null))
                return "a pair owner is still active";
            foreach (var r in WorldRelationships)
                if ((ReferenceEquals(r.Rider, rider) || ReferenceEquals(r.Mount, mount) || ReferenceEquals(r.Rider, mount) || ReferenceEquals(r.Mount, rider)) &&
                    (r.State != RelationshipState.Unmounted || !r.Runtime.NoPreparedPairResidue || r.Runtime.HasPresentationAttachmentResidue))
                    return "a world relationship still holds the pair or owned residue";
            if (rider.View == null || mount.View == null || rider.View.transform.IsChildOf(mount.View.transform) ||
                mount.View.transform.IsChildOf(rider.View.transform))
                return "rider and mount views are not separate";
            if (!Finite(rider.Position) || !Finite(mount.Position)) return "a unit position is not finite";
            if (rider.View.AgentOverride != null || mount.View.AgentOverride != null) return "an agent override remains installed";
            return null;
        }
        private static bool Finite(UnityEngine.Vector3 v) =>
            !float.IsNaN(v.x) && !float.IsInfinity(v.x) && !float.IsNaN(v.y) && !float.IsInfinity(v.y) && !float.IsNaN(v.z) && !float.IsInfinity(v.z);

        // Ratnook door repair: the exact installed TeleportParty loading process (Game.Teleport 06000CCB queues it).
        internal const string NativeTeleportProcessName = "<TeleportParty>d__161";

        // Harmony prefix of Game.Teleport (06000CCB). Only an enter point of the transfer's own destination, which must
        // be the loaded area, is this transfer's dispatch; any other teleport (a cutscene, the capital exit) is foreign.
        internal void ObserveLocalTeleportDispatch(BlueprintAreaEnterPoint enterPoint)
        {
            var transfer = areaTransfer;
            var game = Game.Instance;
            if (transfer == null || enterPoint == null || transfer.World.Target != game?.Player) return;
            var areaId = enterPoint.Area?.AssetGuidThreadSafe;
            if (areaId == null || areaId != transfer.AreaId || game.CurrentlyLoadedArea?.AssetGuidThreadSafe != areaId) return;
            if (!transfer.Lifecycle.ObserveLocalTeleportDispatch()) return;
            transfer.EnterPointId = transfer.EnterPointId ?? enterPoint.AssetGuidThreadSafe;
            TraceAreaTransfer(transfer, "local teleport dispatched");
        }

        // Native ITeleportHandler.HandlePartyTeleport: Player.MoveCharacters has placed the party for this transfer's
        // own teleport, inside the native loading process exactly as OnAreaLoaded is for a full load.
        internal void CompleteLocalTeleportPlacement()
        {
            var transfer = areaTransfer;
            if (transfer == null || transfer.World.Target != Game.Instance?.Player) return;
            // Review P2: a TeleportParty still queued places the party later; restore only at the last one.
            var anotherQueued = LoadingProcess.Instance != null &&
                LoadingProcess.Instance.QueuedNames.Any(name => string.Equals(name, NativeTeleportProcessName, StringComparison.Ordinal));
            if (!transfer.Lifecycle.ObserveLocalTeleportPlacement(anotherQueued))
            {
                if (anotherQueued && transfer.Lifecycle.Route == MountedAreaTransferRoute.LocalTeleport && !transfer.Lifecycle.Placed)
                    TraceAreaTransfer(transfer, "teleport placement deferred to a later queued teleport");
                return;
            }
            TraceAreaTransfer(transfer, "native local placement observed");
            RestoreAreaPair();
        }

        // Native Game.OnAreaLoaded postfix: only a transfer whose full load was dispatched completes here.
        internal void CompleteFullAreaPlacement()
        {
            var transfer = areaTransfer;
            if (transfer == null || !transfer.Lifecycle.ObserveFullLoadPlacement()) return;
            RestoreAreaPair();
        }

        private void CompleteAreaTransitionIfReady()
        {
            var transfer = areaTransfer;
            if (transfer == null) return;
            if (transfer.World.Target != Game.Instance?.Player) { CancelAreaTransition(); return; }
            if (LoadingProcess.Instance.IsLoadingInProcess || LoadingProcess.Instance.QueuedNames.Any() ||
                NativeDeferredSave.Waiting(LoadingProcess.Instance)) return;
            var decision = transfer.Lifecycle.DecideAtIdleLoader();
            // Review P2: the refusal that left the transfer undispatched may be an owned save or charge boundary. Wait
            // (state-based, never a timer) until that boundary settles before re-attaching anything; an earlier latched
            // failure keeps its own custody record and is never replaced by this transfer.
            if (decision == MountedAreaTransferIdleDecision.RestoreInPlace)
            {
                if (HasActiveSaveScope || HasSaveAdmissionFence || SaveDraining || SaveSuspended || AnyChargeFence) return;
                if (areaRestoreFailure != null)
                {
                    TraceAreaTransfer(transfer, "no native dispatch under an existing restore failure; left dismounted");
                    CancelAreaTransition();
                    return;
                }
            }
            // The native dispatch is synchronous with the party-leave event (ExecuteTransition 0600263E), so an idle queue
            // with no observed dispatch is a refused transition: the party was never moved. Restore in place only when the
            // same world still holds the departure area; anything else stays an unknown placement.
            if (decision == MountedAreaTransferIdleDecision.RestoreInPlace && transfer.DepartureAreaId != null &&
                Game.Instance.CurrentlyLoadedArea?.AssetGuidThreadSafe == transfer.DepartureAreaId &&
                transfer.Lifecycle.ObserveRefusedBeforeDispatch())
            {
                TraceAreaTransfer(transfer, "no native dispatch; restoring in the departure area");
                transfer.AreaId = transfer.DepartureAreaId; transfer.WorldMap = false;
                RestoreAreaPair();
            }
            else if (decision != MountedAreaTransferIdleDecision.Finish)
            {
                TraceAreaTransfer(transfer, "idle without native placement");
                failedAreaTransfer = transfer;
                areaRestoreFailure = "Native area placement did not complete before the mounted restore boundary.";
                Report(areaRestoreFailure);
            }
            CancelAreaTransition();
        }

        private long areaTransferSequence;

        // Bounded correlation for one transfer: begin, route, placement and failure only (never per frame).
        private void TraceAreaTransfer(AreaTransfer transfer, string phase)
        {
            if (transfer == null) return;
            logger.Info("Mounted area transfer " + transfer.Sequence + ": " + phase + "; from=" + (transfer.DepartureAreaId ?? "none") +
                "; to=" + (transfer.AreaId ?? "none") + "; enter=" + (transfer.EnterPointId ?? "none") + "; pairs=" + (transfer.Pairs?.Length ?? 0) +
                "; " + transfer.Lifecycle.Describe() + "; sameWorld=" + ReferenceEquals(transfer.World.Target, Game.Instance?.Player) + ".");
        }

        private void CancelAreaTransition()
        {
            if (areaTransfer == null) return;
            areaTransfer = null; controls.ResumeAreaControls();
        }

        private sealed class AreaPair
        {
            internal string RiderId, MountId, ProfileId, OwnerId; internal long Generation; internal SavedPairedState Participation; internal bool Carried;
            // Cutscene continuity repair, transient (never saved): this pair's own eligibility patience start, its bounded
            // regroup move and attempts, and the boundary that handed it to scene staging (diagnostics only).
            internal float EligibleSince = -1f;
            internal Kingmaker.UnitLogic.Commands.UnitMoveTo RegroupMove;
            internal float RegroupStartedAt = -1f;
            internal int RegroupAttempts;
            internal string StagingBoundary;
        }
        private sealed class AreaTransfer
        {
            internal WeakReference World;
            internal string CampaignId, AreaId;
            internal AreaPair[] Pairs;
            internal SavedMountedSlot[] Slots;
            internal bool TurnBased;
            // Ratnook door repair: the native route and phase of this transfer (retained-intent custody never uses it).
            internal MountedAreaTransferLifecycle Lifecycle = new MountedAreaTransferLifecycle(MountedAreaTransferRoute.Requested);
            internal long Sequence;
            internal string DepartureAreaId, EnterPointId;
            // Exploration repair (issue 7): the destination is the world map, so arrival keeps retained intent.
            internal bool WorldMap;
            // The kind of the retained intent this transfer carries (its carried pairs restore under its rules).
            internal MountedRetainedIntentKind CarriedKind;
        }
    }
}
