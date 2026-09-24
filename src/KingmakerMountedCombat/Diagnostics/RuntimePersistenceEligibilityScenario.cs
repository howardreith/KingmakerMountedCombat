using System;
using System.IO;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class RuntimePersistenceScenario
    {
        // A live eligibility change across persistence: a real native size
        // effect (the engine's own Enlarge Person buff, applied through the
        // native buff system) makes the mounted rider Large, KMC's mounted
        // invariant ends the pair by its own rule, a NEW native save records no
        // pair while the effect persists natively, and both an in-process
        // reload and a fresh-process cold load restore nothing invented while
        // the enlarged rider and its effect are exactly what the archive
        // carried.
        private bool EligibilityCase => request.Scenario == "persistence-p07-save" && RuntimeRequest.IsEligibilityCase(request.PersistenceCase);
        private bool EligibilityColdCase => request.Scenario == "persistence-p07-load" && RuntimeRequest.IsEligibilityCase(request.PersistenceCase);
        private const string EligibilitySaveName = "KMC_SIZE";
        private const string EligibilitySaveLeaf = "Manual_301_KMC_SIZE.zks";
        internal const string EligibilityBuffName = "EnlargePersonBuff";
        private const int MediumSizeOrdinal = 4;
        private int eligibilityStage;
        private int eligibilityFrames;
        private string eligibilityRiderId;
        private string eligibilityMountId;
        private string eligibilityFirstPath;
        private string eligibilityFirstHash;
        private string eligibilityArchivePath;
        private string eligibilityArchiveHash;
        private string eligibilityBuffGuid;
        private int eligibilitySizeBefore;
        private int eligibilitySizeAfter;
        private int eligibilitySemanticsBefore;
        private int eligibilityPresentationBefore;
        private int eligibilityMountDamageBefore;

        internal static BlueprintBuff FindEligibilityBuff()
        {
            var library = ResourcesLibrary.LibraryObject;
            if (library?.BlueprintsByAssetId == null) return null;
            var matches = library.BlueprintsByAssetId.Values.OfType<BlueprintBuff>()
                .Where(b => b != null && b.name == EligibilityBuffName).ToArray();
            return matches.Length == 1 ? matches[0] : null;
        }

        private static int SizeOrdinal(UnitEntityData unit) => unit?.Descriptor?.State == null ? -1 : (int)unit.Descriptor.State.Size;

        private static bool CarriesBuff(UnitEntityData unit, string guid) =>
            unit != null && !string.IsNullOrEmpty(guid) && unit.Buffs.Enumerable.Any(b => b.Blueprint?.AssetGuid == guid);

        private void AdvanceEligibility()
        {
            if (clock.Elapsed.TotalSeconds > 300)
                throw new InvalidOperationException("P07 eligibility boundary timed out at " + eligibilityStage + " frames=" + eligibilityFrames + ": " + persistence.Feedback);
            var game = Game.Instance;
            if (game == null) return;
            stage = 1300 + eligibilityStage;
            if (eligibilityStage == 4)
            {
                eligibilityFrames++;
                if (LoadingProcess.Instance.IsLoadingInProcess || NativePersistenceIsolation.HasPendingWrites ||
                    game.CurrentlyLoadedArea == null || game.CurrentMode != Kingmaker.GameModes.GameModeType.Default || !callback ||
                    eligibilityFrames < 10)
                { if (eligibilityFrames > 7200) throw new InvalidOperationException("P07 eligibility reload never completed: " + persistence.Feedback); return; }
                CompleteEligibilityReload();
                return;
            }
            if (LoadingProcess.Instance.IsLoadingInProcess || game.CurrentlyLoadedArea == null ||
                game.CurrentMode != Kingmaker.GameModes.GameModeType.Default) return;
            if (eligibilityStage == 0)
            {
                if (!callback || NativePersistenceIsolation.HasPendingWrites) return;
                Check(relationship.State == RelationshipState.Mounted && persistence.SnapshotCount == 1,
                    "P07-eligibility-opens-with-one-real-mounted-save");
                rider = relationship.Rider; mount = relationship.Mount;
                eligibilityRiderId = rider.UniqueId; eligibilityMountId = mount.UniqueId;
                beforeControls = controls.CaptureSnapshot();
                var first = RecoveryArchive();
                eligibilityFirstPath = first.FolderName; eligibilityFirstHash = Hash(eligibilityFirstPath);
                var read = NativeMountedSaveStorage.Read(first.Saver);
                Check(read.Kind == MountedSaveReadKind.Current && read.Data.Mounted &&
                    read.Data.Rider.Id == eligibilityRiderId && read.Data.Mount.Id == eligibilityMountId, "P07-eligibility-first-archive-carries-the-pair");
                Write("native-write-complete", new JObject {
                    ["ordinal"] = 1, ["path"] = eligibilityFirstPath, ["sha256"] = eligibilityFirstHash,
                    ["length"] = new FileInfo(eligibilityFirstPath).Length, ["nativeType"] = first.Type.ToString(),
                    ["nativeCallback"] = callback, ["operation"] = first.OperationState.ToString(),
                    ["snapshot"] = JObject.FromObject(read.Data, MountedSaveCodec.CreateSerializer()) });
                // The engine's own size effect, resolved by its authored name and
                // applied through the native buff system; never a state edit.
                var blueprint = FindEligibilityBuff();
                Check(blueprint != null && blueprint.ComponentsArray.Any(c => c != null && c.GetType().Name == "ChangeUnitSize"),
                    "P07-eligibility-stimulus-is-the-engine-own-size-effect");
                eligibilityBuffGuid = blueprint.AssetGuid;
                eligibilitySizeBefore = SizeOrdinal(rider);
                eligibilityMountDamageBefore = mount.Damage;
                Check(eligibilitySizeBefore == MediumSizeOrdinal && !CarriesBuff(rider, eligibilityBuffGuid) && !game.Player.IsInCombat,
                    "P07-eligibility-rider-is-medium-and-unaffected-before-the-stimulus");
                var buff = rider.Buffs.AddBuff(blueprint, rider, TimeSpan.FromMinutes(10));
                Check(buff != null && CarriesBuff(rider, eligibilityBuffGuid), "P07-eligibility-native-size-effect-applied");
                Write("eligibility-dispatched", EligibilityDetail(null));
                eligibilityFrames = 0; eligibilityStage = 1;
                return;
            }
            if (eligibilityStage == 1)
            {
                // KMC's own mounted invariant (rider size exactly Medium) ends the
                // pair; the fixture only waits for that native consequence.
                if (relationship.State != RelationshipState.Unmounted || SizeOrdinal(rider) <= MediumSizeOrdinal || persistence.SaveSuspended)
                { if (++eligibilityFrames > 600) throw new InvalidOperationException("P07 eligibility change never ended the pair: size=" + SizeOrdinal(rider) + " relationship=" + relationship.State); return; }
                eligibilitySizeAfter = SizeOrdinal(rider);
                var after = controls.CaptureSnapshot();
                Check(after.DuplicateFactCount == 0 && !after.SerializationSuspended && !persistence.HasActiveSaveScope,
                    "P07-eligibility-cleanup-left-no-duplicate-control-or-lease");
                Check(mount.IsInState && mount.Descriptor.State.IsConscious && mount.Damage == eligibilityMountDamageBefore &&
                    rider.Descriptor.State.IsConscious && CarriesBuff(rider, eligibilityBuffGuid),
                    "P07-eligibility-left-both-actors-alive-with-the-effect-in-place");
                Write("eligibility-cleanup", EligibilityDetail(null));
                eligibilityFrames = 0; eligibilityStage = 2;
                return;
            }
            if (eligibilityStage == 2)
            {
                if (game.IsPaused) { Write("fixture-native-unpause"); game.IsPaused = false; return; }
                var allowed = game.SaveManager.IsSaveAllowed();
                if (!allowed && ++eligibilityFrames < 600) return;
                Write("eligibility-save-admission", EligibilityDetail(new JObject { ["saveAllowed"] = allowed, ["settleFrames"] = eligibilityFrames,
                    ["partyCombat"] = game.Player.IsInCombat, ["mode"] = game.CurrentMode.ToString() }));
                Check(allowed && !game.Player.IsInCombat, "P07-eligibility-save-is-admitted-after-the-change");
                Check(relationship.State == RelationshipState.Unmounted && SizeOrdinal(rider) == eligibilitySizeAfter && CarriesBuff(rider, eligibilityBuffGuid),
                    "P07-eligibility-native-effect-persists-to-the-save");
                eligibilitySemanticsBefore = persistence.SemanticRestoreCount; eligibilityPresentationBefore = persistence.PresentationRestoreCount;
                callback = false;
                game.SaveGame(game.SaveManager.CreateNewSave(EligibilitySaveName), () => callback = true);
                eligibilityFrames = 0; eligibilityStage = 3;
                return;
            }
            if (eligibilityStage == 3)
            {
                if (!callback || NativePersistenceIsolation.HasPendingWrites)
                { if (++eligibilityFrames > 2400) throw new InvalidOperationException("P07 eligibility save never completed: " + persistence.Feedback); return; }
                var saved = game.SaveManager.Single(s => s.Name == EligibilitySaveName && s.HasFileOnDisk);
                var read = NativeMountedSaveStorage.Read(saved.Saver);
                Check(saved.FileName == EligibilitySaveLeaf && saved.OperationState == SaveInfo.StateType.None &&
                    saved.GameId == request.Fixture.Working.GameId && read.Kind == MountedSaveReadKind.Current &&
                    !read.Data.Mounted && read.Data.Rider == null && read.Data.Mount == null &&
                    read.Data.CampaignId == request.Fixture.Working.GameId && persistence.SnapshotCount == 2,
                    "P07-eligibility-save-records-no-pair-in-the-fixture-campaign");
                eligibilityArchivePath = saved.FolderName; eligibilityArchiveHash = Hash(eligibilityArchivePath);
                Check(Hash(eligibilityFirstPath) == eligibilityFirstHash, "P07-eligibility-save-left-the-first-archive-untouched");
                Check(SizeOrdinal(rider) == eligibilitySizeAfter && CarriesBuff(rider, eligibilityBuffGuid) &&
                    relationship.State == RelationshipState.Unmounted, "P07-eligibility-native-effect-persists-through-the-save");
                Write("eligibility-saved", EligibilityDetail(new JObject { ["archive"] = new JObject {
                    ["path"] = eligibilityArchivePath, ["leaf"] = saved.FileName, ["sha256"] = eligibilityArchiveHash,
                    ["length"] = new FileInfo(eligibilityArchivePath).Length, ["nativeType"] = saved.Type.ToString(),
                    ["internalName"] = saved.Name, ["gameId"] = saved.GameId, ["area"] = saved.Area?.AssetGuidThreadSafe,
                    ["operation"] = saved.OperationState.ToString(), ["kmcMember"] = read.Kind.ToString(),
                    ["snapshot"] = JObject.FromObject(read.Data, MountedSaveCodec.CreateSerializer()) } }));
                RequestRecoveryLoad(saved);
                eligibilityFrames = 0; eligibilityStage = 4;
            }
        }

        private void CompleteEligibilityReload()
        {
            var game = Game.Instance;
            var subject = game.State.Units.SingleOrDefault(u => u.UniqueId == eligibilityRiderId);
            var partner = game.State.Units.SingleOrDefault(u => u.UniqueId == eligibilityMountId);
            Write("eligibility-reloaded", EligibilityDetail(new JObject {
                ["riderPresent"] = subject != null, ["riderSize"] = SizeOrdinal(subject), ["riderCarriesEffect"] = CarriesBuff(subject, eligibilityBuffGuid),
                ["mountPresent"] = partner != null, ["mountConscious"] = partner?.Descriptor.State.IsConscious,
                ["loadedDataMounted"] = persistence.LoadedData?.Mounted, ["semanticsDelta"] = persistence.SemanticRestoreCount - eligibilitySemanticsBefore,
                ["presentationDelta"] = persistence.PresentationRestoreCount - eligibilityPresentationBefore }));
            Check(relationship.State == RelationshipState.Unmounted && persistence.LoadedData != null && !persistence.LoadedData.Mounted &&
                persistence.SemanticRestoreCount == eligibilitySemanticsBefore && persistence.PresentationRestoreCount == eligibilityPresentationBefore &&
                controls.NativeCastRequestCount == 0 && controls.CaptureSnapshot().DuplicateFactCount == 0,
                "P07-reloading-the-eligibility-save-invents-no-pair-and-restores-nothing");
            Check(subject != null && SizeOrdinal(subject) == eligibilitySizeAfter && CarriesBuff(subject, eligibilityBuffGuid) &&
                partner != null && partner.Descriptor.State.IsConscious, "P07-native-size-effect-persists-across-the-reload");
            Check(Hash(eligibilityArchivePath) == eligibilityArchiveHash && Hash(eligibilityFirstPath) == eligibilityFirstHash,
                "P07-eligibility-archives-are-byte-identical-after-the-reload");
            Dispose();
            Result = new RuntimeSubscenarioResult { Name = request.Scenario, Status = "PASS",
                AssertionPassCount = passed, AssertionFailCount = 0, Errors = new string[0] };
            Completed = true;
        }

        // The fresh-process control: the archive opens, nothing is invented, and
        // exactly one party member carries the engine's size effect and is Large.
        private void AdvanceEligibilityCold()
        {
            if (clock.Elapsed.TotalSeconds > 150) throw new InvalidOperationException("P07 eligibility cold load timed out.");
            var game = Game.Instance;
            if (game == null || LoadingProcess.Instance.IsLoadingInProcess || game.CurrentlyLoadedArea == null ||
                game.CurrentMode != Kingmaker.GameModes.GameModeType.Default) return;
            if (++eligibilityFrames < 10) return;
            var blueprint = FindEligibilityBuff();
            var guid = blueprint?.AssetGuid;
            var party = game.Player.Party.ToArray();
            var mounts = game.State.Units.Where(u => SupportedMountedProfiles.IsSupported(u)).ToArray();
            var affected = party.Where(u => CarriesBuff(u, guid)).ToArray();
            // The supported mount is natively larger than Medium; the enlarged
            // party members are counted among the others only.
            var large = party.Where(u => SizeOrdinal(u) > MediumSizeOrdinal && !mounts.Contains(u)).ToArray();
            var states = new JObject();
            foreach (var unit in party.Concat(mounts).Distinct())
                states[unit.UniqueId] = new JObject { ["size"] = SizeOrdinal(unit), ["carriesEffect"] = CarriesBuff(unit, guid),
                    ["inParty"] = party.Contains(unit), ["supportedMount"] = mounts.Contains(unit), ["conscious"] = unit.Descriptor.State.IsConscious };
            var archive = Path.Combine(game.SaveManager.SavePath, request.PersistenceLoad.FileName);
            Write("initial");
            Write("eligibility-cold-complete", new JObject {
                ["case"] = request.PersistenceCase, ["buffName"] = EligibilityBuffName, ["buffGuid"] = guid,
                ["party"] = party.Length, ["affected"] = affected.Length, ["affectedIds"] = new JArray(affected.Select(u => u.UniqueId)),
                ["large"] = large.Length, ["supportedMounts"] = mounts.Length,
                ["mountId"] = mounts.Length == 1 ? mounts[0].UniqueId : null, ["states"] = states,
                ["loadedDataPresent"] = persistence.LoadedData != null, ["loadedDataMounted"] = persistence.LoadedData?.Mounted,
                ["semantics"] = persistence.SemanticRestoreCount, ["presentation"] = persistence.PresentationRestoreCount,
                ["nativeCastRequests"] = controls.NativeCastRequestCount, ["feedback"] = persistence.Feedback,
                ["gameId"] = game.Player.GameId, ["area"] = game.CurrentlyLoadedArea.AssetGuidThreadSafe,
                ["archivePath"] = archive, ["archiveSha256"] = Hash(archive), ["expectedSha256"] = request.PersistenceLoad.Sha256 });
            Check(relationship.State == RelationshipState.Unmounted && persistence.LoadedData != null && !persistence.LoadedData.Mounted &&
                persistence.SemanticRestoreCount == 0 && persistence.PresentationRestoreCount == 0 && controls.NativeCastRequestCount == 0,
                "P07-cold-eligibility-save-invents-no-pair-and-restores-nothing");
            Check(guid != null && affected.Length == 1 && large.Length == 1 && affected[0] == large[0] && mounts.Length == 1 &&
                !mounts.Contains(affected[0]) && mounts[0].Descriptor.State.IsConscious,
                "P07-cold-eligibility-save-carries-exactly-the-enlarged-rider-it-recorded");
            Check(game.Player.GameId == request.PersistenceLoad.GameId && game.CurrentlyLoadedArea.AssetGuidThreadSafe == request.PersistenceLoad.Area &&
                Hash(archive) == request.PersistenceLoad.Sha256, "P07-cold-eligibility-save-opened-its-own-world-byte-identical");
            Dispose();
            Result = new RuntimeSubscenarioResult { Name = request.Scenario, Status = "PASS",
                AssertionPassCount = passed, AssertionFailCount = 0, Errors = new string[0] };
            Completed = true;
        }

        private JObject EligibilityDetail(JObject extra)
        {
            var detail = new JObject {
                ["case"] = request.PersistenceCase, ["stage"] = eligibilityStage, ["frames"] = eligibilityFrames,
                ["riderId"] = eligibilityRiderId, ["mountId"] = eligibilityMountId,
                ["buffName"] = EligibilityBuffName, ["buffGuid"] = eligibilityBuffGuid,
                ["sizeBefore"] = eligibilitySizeBefore, ["sizeAfter"] = eligibilitySizeAfter, ["riderSize"] = SizeOrdinal(rider),
                ["riderCarriesEffect"] = CarriesBuff(rider, eligibilityBuffGuid), ["mountSize"] = SizeOrdinal(mount),
                ["mountConscious"] = mount?.Descriptor.State.IsConscious, ["mountDamage"] = mount?.Damage,
                ["relationship"] = relationship.State.ToString(), ["partyCombat"] = Game.Instance?.Player?.IsInCombat,
                ["snapshots"] = persistence.SnapshotCount, ["failedSaves"] = persistence.FailedSaveCount,
                ["saveSuspended"] = persistence.SaveSuspended, ["activeScope"] = persistence.HasActiveSaveScope,
                ["semantics"] = persistence.SemanticRestoreCount, ["presentation"] = persistence.PresentationRestoreCount,
                ["nativeCastRequests"] = controls.NativeCastRequestCount,
                ["firstPath"] = eligibilityFirstPath, ["firstHash"] = eligibilityFirstHash,
                ["archivePath"] = eligibilityArchivePath, ["archiveHash"] = eligibilityArchiveHash, ["feedback"] = persistence.Feedback };
            if (extra != null) foreach (var property in extra.Properties()) detail[property.Name] = property.Value;
            return detail;
        }
    }
}
