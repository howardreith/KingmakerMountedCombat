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
    internal enum RemovalPreparationState { Idle, Refused, Saving, Ready, Failed }

    internal sealed class RemovalAssessment
    {
        internal bool Safe => Reasons.Count == 0;
        internal readonly List<string> Reasons = new List<string>();
        // Permanent references: native units and character facts that name a
        // KMC-registered blueprint. A save holding one cannot be opened without
        // the mod, so removal is refused rather than the campaign stripped.
        internal readonly List<string> PermanentReferences = new List<string>();
        internal bool Mounted;
        internal string Summary => Safe ? "Safe to prepare: no permanent KMC reference in the loaded world." :
            string.Join(" ", Reasons);
    }

    // The user-visible "prepare to disable / remove" flow. Transient pair state
    // is dismounted through the same cleanup the registered disable runs, and
    // a NEW native save is written through the engine's own serialization so
    // the campaign has a clean archive before KMC is switched off or deleted.
    // Nothing here edits an existing archive, strips a campaign, or claims
    // more than the archive it verified.
    internal sealed class MountedRemovalPreparation
    {
        internal const string CleanupSaveName = "KMC_CLEANUP";
        private const double SaveTimeoutSeconds = 120;
        private readonly GameMountedRelationshipService relationship;
        private readonly MountedPersistenceService persistence;
        private readonly HorseCompanionBlueprintService horseCompanion;
        private readonly Func<bool> cleanup;
        private readonly IModLogger logger;
        private SaveInfo pendingSave;
        private bool saveCallback;
        private int failedSavesAtRequest;
        private DateTime saveRequestedAtUtc;

        internal RemovalPreparationState State { get; private set; }
        internal string Status { get; private set; } = "Not prepared. Prepare before disabling or removing KMC to write a clean save.";
        internal string CleanupSavePath { get; private set; }
        internal string CleanupSaveLeaf { get; private set; }
        internal string CleanupSaveSha256 { get; private set; }
        internal int AssessmentCount { get; private set; }
        internal int RefusalCount { get; private set; }
        internal int CleanupSaveCount { get; private set; }
        internal RemovalAssessment LastAssessment { get; private set; }

        internal MountedRemovalPreparation(GameMountedRelationshipService relationship, MountedPersistenceService persistence,
            HorseCompanionBlueprintService horseCompanion, Func<bool> cleanup, IModLogger logger)
        {
            this.relationship = relationship ?? throw new ArgumentNullException(nameof(relationship));
            this.persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
            this.horseCompanion = horseCompanion ?? throw new ArgumentNullException(nameof(horseCompanion));
            this.cleanup = cleanup ?? throw new ArgumentNullException(nameof(cleanup));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Inspection only: nothing is dismounted, saved or mutated here. The
        // scan covers the loaded world and the cross-scene party; units stashed
        // in areas that are not loaded are not inspected and are not claimed.
        internal RemovalAssessment Assess()
        {
            AssessmentCount++;
            var assessment = new RemovalAssessment { Mounted = relationship.State == RelationshipState.Mounted };
            var game = Game.Instance;
            if (game?.Player == null || game.CurrentlyLoadedArea == null || game.SaveManager == null)
            {
                assessment.Reasons.Add("No loaded world: load the campaign first.");
                LastAssessment = assessment;
                return assessment;
            }
            if (persistence.SaveSuspended || persistence.HasActiveSaveScope || persistence.SaveDraining)
                assessment.Reasons.Add("A mounted save is still being written; retry once it finishes.");
            if (persistence.LoadInFlight)
                assessment.Reasons.Add("A save is still being loaded; retry once the area has finished loading.");
            if (!game.SaveManager.IsSaveAllowed())
                assessment.Reasons.Add("Saving is not allowed right now (combat, dialog or cutscene); retry when the game allows a manual save.");
            foreach (var reference in FindPermanentReferences(game))
                assessment.PermanentReferences.Add(reference);
            if (assessment.PermanentReferences.Count != 0)
                assessment.Reasons.Add("This campaign still references KMC's Horse companion (" +
                    string.Join("; ", assessment.PermanentReferences) + "). A save with it cannot be opened without KMC: " +
                    "remove or respec that companion first, or keep KMC installed for this campaign.");
            LastAssessment = assessment;
            return assessment;
        }

        private IEnumerable<string> FindPermanentReferences(Game game)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var units = new List<UnitEntityData>();
            try { units.AddRange(game.State.Units.Where(u => u != null)); }
            catch (Exception exception) { logger.Exception("Removal assessment could not enumerate loaded units", exception); }
            try { units.AddRange(game.Player.AllCharacters.Where(u => u != null)); }
            catch (Exception exception) { logger.Exception("Removal assessment could not enumerate party characters", exception); }
            foreach (var unit in units)
            {
                if (!seen.Add(unit.UniqueId)) continue;
                var guid = unit.Blueprint?.AssetGuid;
                if (string.Equals(guid, HorseCompanionBlueprintService.UnitGuid, StringComparison.Ordinal))
                    yield return "unit " + unit.CharacterName + " [" + unit.UniqueId + "] is the KMC Horse";
                var descriptor = unit.Descriptor;
                if (descriptor == null) continue;
                var feature = horseCompanion.HorseFeature;
                var upgrade = horseCompanion.HorseUpgrade;
                if (feature != null && descriptor.GetFact(feature) != null)
                    yield return "character " + unit.CharacterName + " [" + unit.UniqueId + "] has the KMC Horse companion feature";
                if (upgrade != null && descriptor.GetFact(upgrade) != null)
                    yield return "character " + unit.CharacterName + " [" + unit.UniqueId + "] has the KMC Horse advancement feature";
            }
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
            saveCallback = false;
            failedSavesAtRequest = persistence.FailedSaveCount;
            saveRequestedAtUtc = DateTime.UtcNow;
            CleanupSavePath = null; CleanupSaveLeaf = null; CleanupSaveSha256 = null;
            State = RemovalPreparationState.Saving;
            Status = "Writing the cleanup save " + CleanupSaveName + "...";
            game.SaveGame(descriptor, () => saveCallback = true);
            return true;
        }

        // Polled every frame by the composition root. Only the archive on disk
        // decides readiness: complete, in the manager, and recording no pair.
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
            SaveInfo written = null;
            try
            {
                written = game?.SaveManager?.FirstOrDefault(s => ReferenceEquals(s, pendingSave)) ??
                    game?.SaveManager?.FirstOrDefault(s => s.Name == pendingSave.Name && s.HasFileOnDisk);
            }
            catch (Exception exception) { logger.Exception("Prepare-to-disable could not resolve the written save", exception); }
            if (written == null || !written.HasFileOnDisk || written.OperationState != SaveInfo.StateType.None)
            {
                if ((DateTime.UtcNow - saveRequestedAtUtc).TotalSeconds > SaveTimeoutSeconds)
                    Fail("the cleanup save reported completion but no complete archive was found.");
                return;
            }
            MountedSaveReadResult read;
            try { read = NativeMountedSaveStorage.Read(written.Saver); }
            catch (Exception exception)
            {
                logger.Exception("Prepare-to-disable could not read the cleanup archive", exception);
                Fail("the cleanup archive could not be read back.");
                return;
            }
            var clean = read.Kind == MountedSaveReadKind.Missing || (read.Kind == MountedSaveReadKind.Current &&
                read.Data != null && !read.Data.Mounted && read.Data.Rider == null && read.Data.Mount == null);
            if (!clean)
            {
                Fail("the cleanup archive still records a mounted pair (" + read.Kind + ").");
                return;
            }
            CleanupSavePath = written.FolderName;
            CleanupSaveLeaf = written.FileName;
            try { CleanupSaveSha256 = HashFile(written.FolderName); }
            catch (Exception exception) { logger.Exception("Prepare-to-disable could not hash the cleanup archive", exception); }
            CleanupSaveCount++;
            pendingSave = null;
            State = RemovalPreparationState.Ready;
            Status = "Prepared: cleanup save " + CleanupSaveLeaf + " records no mounted pair. KMC can now be disabled or removed; " +
                "load that save afterwards.";
            logger.Info(Status);
        }

        private void Fail(string reason)
        {
            pendingSave = null;
            State = RemovalPreparationState.Failed;
            Status = "Not prepared: " + reason;
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
