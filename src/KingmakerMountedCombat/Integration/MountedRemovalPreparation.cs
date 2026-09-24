using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.UnitLogic;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Logging;

namespace KingmakerMountedCombat.Integration
{
    internal enum RemovalPreparationState { Idle, Refused, Saving, Ready, Unconfirmed, Failed }

    internal sealed class RemovalAssessment
    {
        internal bool Safe => Reasons.Count == 0;
        internal readonly List<string> Reasons = new List<string>();
        // Permanent references: native units and character facts that name a
        // KMC-registered blueprint. A save holding one cannot be opened without
        // the mod, so removal is refused rather than the campaign stripped.
        internal readonly List<string> PermanentReferences = new List<string>();
        internal RemovalWorldFacts World;
        internal bool Mounted;
        internal string Summary => Safe ? "Safe to prepare: the world is settled and no permanent KMC reference is loaded." :
            string.Join(" ", Reasons);
    }

    // The user-visible "prepare to disable / remove" flow. Transient pair state
    // is dismounted through the same cleanup the registered disable runs, and
    // a NEW native save is written through the engine's own serialization so
    // the campaign has a clean archive before KMC is switched off or deleted.
    // Nothing here edits an existing archive, strips a campaign, or claims
    // more than the archive it verified: readiness is reported only after the
    // archive this operation requested is bound to the persistence service's
    // own completed-write record for exactly this request,
    // its KMC member records no pair, combat supplement or control binding,
    // and every JSON member of the archive is free of KMC-registered blueprint
    // identities (that scan covers stashed areas the live world does not show).
    internal sealed class MountedRemovalPreparation
    {
        internal const string CleanupSaveName = "KMC_CLEANUP";
        private const double SaveTimeoutSeconds = 120;
        private readonly GameMountedRelationshipService relationship;
        private readonly MountedPersistenceService persistence;
        private readonly MountedCombatController combat;
        private readonly HorseCompanionBlueprintService horseCompanion;
        private readonly Func<bool> cleanup;
        private readonly IModLogger logger;
        private SaveInfo pendingSave;
        private string pendingGameId;
        private int completedAtRequest;
        private bool saveCallback;
        private int failedSavesAtRequest;
        private DateTime saveRequestedAtUtc;

        internal RemovalPreparationState State { get; private set; }
        internal string Status { get; private set; } = "Not prepared. Prepare before disabling or removing KMC to write a clean save.";
        internal string CleanupSavePath { get; private set; }
        internal string CleanupSaveLeaf { get; private set; }
        internal string CleanupSaveSha256 { get; private set; }
        internal string CleanupCampaignId { get; private set; }
        internal int CleanupScannedMembers { get; private set; }
        internal long CleanupScannedBytes { get; private set; }
        internal IReadOnlyList<string> CleanupReferenceHits { get; private set; } = new string[0];
        internal string CleanupBinding { get; private set; }
        internal int AssessmentCount { get; private set; }
        internal int RefusalCount { get; private set; }
        internal int CleanupSaveCount { get; private set; }
        internal int UnconfirmedCount { get; private set; }
        internal RemovalAssessment LastAssessment { get; private set; }
        // What the poll is waiting on while Saving, for the scenario probe rows.
        internal string PendingDiagnostics { get; private set; }

        internal MountedRemovalPreparation(GameMountedRelationshipService relationship, MountedPersistenceService persistence,
            MountedCombatController combat, HorseCompanionBlueprintService horseCompanion, Func<bool> cleanup, IModLogger logger)
        {
            this.relationship = relationship ?? throw new ArgumentNullException(nameof(relationship));
            this.persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
            this.combat = combat ?? throw new ArgumentNullException(nameof(combat));
            this.horseCompanion = horseCompanion ?? throw new ArgumentNullException(nameof(horseCompanion));
            this.cleanup = cleanup ?? throw new ArgumentNullException(nameof(cleanup));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // The KMC-registered blueprint identities a save cannot carry without the
        // mod: the Horse companion trio and its portrait, and the four native
        // mounted-control abilities. Native blueprints KMC only references are
        // not in this list; they resolve without KMC.
        internal static string[] RegisteredBlueprintGuids => new[]
        {
            HorseCompanionBlueprintService.UnitGuid, HorseCompanionBlueprintService.FeatureGuid,
            HorseCompanionBlueprintService.UpgradeGuid, HorseCompanionBlueprintService.PortraitGuid,
            NativeMountedControlService.MountAbilityGuid, NativeMountedControlService.DismountAbilityGuid,
            NativeMountedControlService.RiderPrimaryAbilityGuid, NativeMountedControlService.MountPrimaryAbilityGuid
        };

        // Inspection only: nothing is dismounted, saved or mutated here. The
        // live scan covers the loaded world and the cross-scene party; units
        // stashed in areas that are not loaded are covered by the archive scan
        // after the cleanup save, never claimed from here.
        internal RemovalAssessment Assess()
        {
            AssessmentCount++;
            var assessment = new RemovalAssessment { Mounted = relationship.State == RelationshipState.Mounted };
            var game = Game.Instance;
            var facts = new RemovalWorldFacts();
            try
            {
                facts.WorldLoaded = game?.Player != null && game.CurrentlyLoadedArea != null && game.SaveManager != null;
                if (facts.WorldLoaded)
                {
                    facts.PartyInCombat = game.Player.IsInCombat;
                    facts.AnyPartyMemberInCombat = game.Player.Party.Any(u => u != null && u.IsInCombat);
                    facts.ActiveMountedCommand = combat.HasActiveCommand;
                    facts.StockAttackIntent = combat.HasStockAttackIntent;
                    facts.PairedActivation = !string.IsNullOrEmpty(combat.PairedActivationIdentity);
                    facts.CombatRestorationPending = persistence.CombatRestorationPending;
                    facts.LoadInFlight = persistence.LoadInFlight;
                    facts.LoadingWorld = persistence.LoadingWorld;
                    facts.LoadingProcess = LoadingProcess.Instance.IsLoadingInProcess;
                    facts.SaveSuspended = persistence.SaveSuspended;
                    facts.ActiveSaveScope = persistence.HasActiveSaveScope;
                    facts.SaveDraining = persistence.SaveDraining;
                    facts.WorldHoldReleasePending = persistence.WorldHoldReleasePending;
                    facts.ResetToMainMenuPending = persistence.ResetToMainMenuPending;
                    facts.DefaultMode = game.CurrentMode == Kingmaker.GameModes.GameModeType.Default;
                    facts.SaveAllowed = game.SaveManager.IsSaveAllowed();
                }
            }
            catch (Exception exception)
            {
                facts.InspectionFailed = true;
                facts.InspectionFailure = exception.GetType().Name + ": " + exception.Message;
                logger.Exception("Removal assessment could not inspect the world state", exception);
            }
            if (facts.WorldLoaded && !facts.InspectionFailed)
            {
                try
                {
                    foreach (var reference in FindPermanentReferences(game))
                        assessment.PermanentReferences.Add(reference);
                }
                catch (Exception exception)
                {
                    // Fail closed: a scan that could not finish proves nothing.
                    facts.InspectionFailed = true;
                    facts.InspectionFailure = exception.GetType().Name + ": " + exception.Message;
                    logger.Exception("Removal assessment could not enumerate the loaded world", exception);
                }
            }
            assessment.World = facts;
            assessment.Reasons.AddRange(RemovalReadinessPolicy.WorldReasons(facts));
            if (assessment.PermanentReferences.Count != 0)
                assessment.Reasons.Add("This campaign still references KMC's Horse companion (" +
                    string.Join("; ", assessment.PermanentReferences) + "). A save with it cannot be opened without KMC: " +
                    "remove or respec that companion first, or keep KMC installed for this campaign.");
            LastAssessment = assessment;
            return assessment;
        }

        private List<string> FindPermanentReferences(Game game)
        {
            var references = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var units = new List<UnitEntityData>();
            // No catch here: an enumeration failure surfaces to Assess, which
            // records it as a failed inspection rather than a clean world.
            units.AddRange(game.State.Units.Where(u => u != null));
            units.AddRange(game.Player.AllCharacters.Where(u => u != null));
            var feature = horseCompanion.HorseFeature;
            var upgrade = horseCompanion.HorseUpgrade;
            foreach (var unit in units)
            {
                if (!seen.Add(unit.UniqueId)) continue;
                var guid = unit.Blueprint?.AssetGuid;
                if (string.Equals(guid, HorseCompanionBlueprintService.UnitGuid, StringComparison.Ordinal))
                    references.Add("unit " + unit.CharacterName + " [" + unit.UniqueId + "] is the KMC Horse");
                var descriptor = unit.Descriptor;
                if (descriptor == null) continue;
                if (feature != null && descriptor.GetFact(feature) != null)
                    references.Add("character " + unit.CharacterName + " [" + unit.UniqueId + "] has the KMC Horse companion feature");
                if (upgrade != null && descriptor.GetFact(upgrade) != null)
                    references.Add("character " + unit.CharacterName + " [" + unit.UniqueId + "] has the KMC Horse advancement feature");
            }
            return references;
        }

        // The user's button. Refuses with the assessment's reasons; otherwise
        // dismounts through the registered-disable cleanup and requests one NEW
        // native save. The result is reported only from the written archive.
        internal bool Begin()
        {
            if (State == RemovalPreparationState.Saving)
            {
                Status = "A cleanup save is already being written.";
                return false;
            }
            var assessment = Assess();
            if (!assessment.Safe)
            {
                RefusalCount++;
                State = RemovalPreparationState.Refused;
                Status = "Not prepared: " + assessment.Summary;
                logger.Warning("Prepare-to-disable refused: " + assessment.Summary);
                return false;
            }
            if (assessment.Mounted && !cleanup())
            {
                State = RemovalPreparationState.Failed;
                Status = "Not prepared: mounted cleanup retained residue; nothing was saved.";
                logger.Error(Status);
                return false;
            }
            if (relationship.State == RelationshipState.Mounted)
            {
                State = RemovalPreparationState.Failed;
                Status = "Not prepared: the pair is still mounted after cleanup; nothing was saved.";
                logger.Error(Status);
                return false;
            }
            var game = Game.Instance;
            SaveInfo descriptor;
            try
            {
                descriptor = game.SaveManager.CreateNewSave(CleanupSaveName);
                if (descriptor == null) throw new InvalidOperationException("The engine returned no save descriptor.");
            }
            catch (Exception exception)
            {
                State = RemovalPreparationState.Failed;
                Status = "Not prepared: the cleanup save could not be requested (" + exception.GetType().Name + ").";
                logger.Exception("Prepare-to-disable cleanup save request", exception);
                return false;
            }
            pendingSave = descriptor;
            pendingGameId = game.Player.GameId;
            completedAtRequest = persistence.CompletedSaveCount;
            saveCallback = false;
            failedSavesAtRequest = persistence.FailedSaveCount;
            saveRequestedAtUtc = DateTime.UtcNow;
            PendingDiagnostics = null;
            CleanupSavePath = null; CleanupSaveLeaf = null; CleanupSaveSha256 = null; CleanupCampaignId = null;
            CleanupScannedMembers = 0; CleanupScannedBytes = 0; CleanupReferenceHits = new string[0]; CleanupBinding = null;
            State = RemovalPreparationState.Saving;
            Status = "Writing the cleanup save " + CleanupSaveName + "...";
            game.SaveGame(descriptor, () => saveCallback = true);
            return true;
        }

        // Polled every frame by the composition root. Only the archive on disk
        // decides readiness, and only the archive this operation requested.
        internal void Update()
        {
            if (State != RemovalPreparationState.Saving || pendingSave == null) return;
            if (persistence.FailedSaveCount != failedSavesAtRequest)
            {
                Fail("the cleanup save failed; " + persistence.Feedback);
                return;
            }
            if (!saveCallback)
            {
                if ((DateTime.UtcNow - saveRequestedAtUtc).TotalSeconds > SaveTimeoutSeconds)
                    Fail("the cleanup save did not complete within " + SaveTimeoutSeconds + " seconds.");
                return;
            }
            if (persistence.SaveSuspended || persistence.HasActiveSaveScope) return;
            var game = Game.Instance;
            RemovalWrittenArchiveFacts written;
            SaveInfo writtenSave;
            try
            {
                // Verified installed behaviour: SaveRoutine (0x0600BEF3 MoveNext)
                // keeps the requested descriptor only as originalSave (IL_01A0),
                // prepares and registers a COPY (IL_01DC..IL_022C), and the
                // worker (SerializeAndSaveThread 0x0600802A) writes that copy's
                // zip in place, reaching KMC's transpiled replacement site only
                // when an original archive is passed (IL_031C). A first-ever save
                // such as this one therefore records no replacement commit, and
                // the requested instance never learns its path. The archive this
                // operation wrote is the one the persistence service's own
                // completion record names for exactly this requested descriptor,
                // and that must be the only wrapped operation completed since the
                // request. Nothing here selects by name.
                var completedSince = persistence.CompletedSaveCount - completedAtRequest;
                var completion = persistence.LastCompletedSave;
                var forThisRequest = completedSince == 1 && completion != null && ReferenceEquals(completion.Requested, pendingSave);
                writtenSave = forThisRequest ? completion.Written : null;
                var registered = writtenSave != null && game?.SaveManager != null && game.SaveManager.Any(s => ReferenceEquals(s, writtenSave));
                var requestedRetired = game?.SaveManager != null && !game.SaveManager.Any(s => ReferenceEquals(s, pendingSave));
                PendingDiagnostics = "completedSince=" + completedSince + " forThisRequest=" + forThisRequest + " writtenRegistered=" + registered +
                    " requestedRetired=" + requestedRetired + " operation=" + (writtenSave == null ? "<none>" : writtenSave.OperationState.ToString()) +
                    " suspended=" + persistence.SaveSuspended + " scope=" + persistence.HasActiveSaveScope;
                if (completedSince == 0)
                {
                    // The native callback fired but no wrapped operation has
                    // completed yet: the worker may still be settling. Bounded.
                    if ((DateTime.UtcNow - saveRequestedAtUtc).TotalSeconds > SaveTimeoutSeconds)
                        Unconfirmed("the cleanup save reported completion but no KMC-wrapped save operation completed since the request (" + PendingDiagnostics + ")");
                    return;
                }
                written = new RemovalWrittenArchiveFacts
                {
                    CompletedSinceRequest = completedSince, CompletedForThisRequest = forThisRequest,
                    WrittenDescriptorRegistered = registered, RequestedDescriptorRetired = requestedRetired,
                    HasFileOnDisk = writtenSave != null && writtenSave.HasFileOnDisk && System.IO.File.Exists(writtenSave.FolderName),
                    OperationState = writtenSave?.OperationState.ToString(), Path = writtenSave?.FolderName,
                    Name = writtenSave?.Name, Type = writtenSave?.Type.ToString(), GameId = writtenSave?.GameId
                };
            }
            catch (Exception exception)
            {
                logger.Exception("Prepare-to-disable could not read the written descriptor", exception);
                Unconfirmed("the written save descriptor could not be read (" + exception.GetType().Name + ")");
                return;
            }
            // The operation has completed, so every binding fact is final: a
            // mismatch now is unconfirmed, never something to wait out.
            var binding = RemovalReadinessPolicy.BindingReason(written, CleanupSaveName, pendingGameId);
            if (binding != null) { Unconfirmed(binding); return; }
            MountedSaveReadResult read;
            try { read = NativeMountedSaveStorage.Read(writtenSave.Saver); }
            catch (Exception exception)
            {
                logger.Exception("Prepare-to-disable could not read the cleanup archive", exception);
                Unconfirmed("the cleanup archive could not be read back (" + exception.GetType().Name + ")");
                return;
            }
            var member = new RemovalMemberFacts
            {
                Kind = read.Kind.ToString(), Mounted = read.Data?.Mounted == true,
                RiderPresent = read.Data?.Rider != null, MountPresent = read.Data?.Mount != null,
                CombatPresent = read.Data?.Combat != null, SlotCount = read.Data?.Slots?.Length ?? 0,
                CampaignId = read.Data?.CampaignId
            };
            var memberReasons = RemovalReadinessPolicy.MemberReasons(member, pendingGameId);
            if (memberReasons.Count != 0) { Fail(string.Join("; ", memberReasons) + "."); return; }
            List<string> hits;
            int scanned; long bytes;
            try { hits = NativeMountedSaveStorage.FindReferences(writtenSave.Saver, RegisteredBlueprintGuids, out scanned, out bytes); }
            catch (Exception exception)
            {
                logger.Exception("Prepare-to-disable could not scan the cleanup archive", exception);
                Unconfirmed("the cleanup archive could not be scanned for KMC references (" + exception.GetType().Name + ")");
                return;
            }
            CleanupScannedMembers = scanned; CleanupScannedBytes = bytes; CleanupReferenceHits = hits.ToArray();
            if (scanned == 0) { Unconfirmed("the cleanup archive exposed no member to scan"); return; }
            if (hits.Count != 0)
            {
                Fail("the cleanup archive still carries KMC blueprint references (" + string.Join("; ", hits) +
                    "); a save with them cannot be opened without KMC, so this campaign must keep KMC installed.");
                return;
            }
            string hash;
            try { hash = HashFile(written.Path); }
            catch (Exception exception)
            {
                logger.Exception("Prepare-to-disable could not hash the cleanup archive", exception);
                Unconfirmed("the cleanup archive could not be hashed (" + exception.GetType().Name + ")");
                return;
            }
            CleanupSavePath = written.Path;
            CleanupSaveLeaf = writtenSave.FileName;
            CleanupSaveSha256 = hash;
            CleanupCampaignId = written.GameId;
            CleanupBinding = "bound";
            CleanupSaveCount++;
            pendingSave = null;
            State = RemovalPreparationState.Ready;
            Status = "Prepared: cleanup save " + CleanupSaveLeaf + " (campaign " + CleanupCampaignId + ") is the archive this operation wrote; " +
                "it records no mounted pair, no combat participation and no KMC control binding, and none of its " + scanned +
                " members names a KMC blueprint. KMC can now be disabled or removed; load that save afterwards.";
            logger.Info(Status);
        }

        private void Fail(string reason)
        {
            pendingSave = null;
            State = RemovalPreparationState.Failed;
            Status = "Not prepared: " + reason;
            logger.Error(Status);
        }

        // Written, perhaps, but not established: the user is told removal is
        // not confirmed rather than that it is safe.
        private void Unconfirmed(string reason)
        {
            pendingSave = null;
            UnconfirmedCount++;
            State = RemovalPreparationState.Unconfirmed;
            Status = "Preparation unconfirmed: " + reason + ". Do not remove KMC on this result; prepare again.";
            logger.Error(Status);
        }

        private static string HashFile(string path)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
            using (var stream = System.IO.File.OpenRead(path))
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
    }
}
