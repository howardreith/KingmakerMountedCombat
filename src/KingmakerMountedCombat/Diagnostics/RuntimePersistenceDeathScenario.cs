using System;
using System.IO;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UI.Selection;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class RuntimePersistenceScenario
    {
        // A harmful lifecycle boundary across persistence: the rider (or the
        // mount) is killed by real native enemy damage while mounted, the
        // lifecycle cleanup ends the pair, a NEW native save records no pair,
        // and both an in-process reload and a fresh-process cold load restore
        // nothing invented while the native death state persists.
        private bool DeathCase => request.Scenario == "persistence-p07-save" && RuntimeRequest.IsDeathCase(request.PersistenceCase);
        private bool DeathColdCase => request.Scenario == "persistence-p07-load" && RuntimeRequest.IsDeathCase(request.PersistenceCase);
        private bool DeathSubjectIsMount => request.PersistenceCase == "mount-death";
        private const string DeathSaveName = "KMC_DEATH";
        private const string DeathSaveLeaf = "Manual_301_KMC_DEATH.zks";
        private int deathStage;
        private int deathFrames;
        private string deathRiderId;
        private string deathMountId;
        private string deathFirstPath;
        private string deathFirstHash;
        private string deathArchivePath;
        private string deathArchiveHash;
        private int deathSemanticsBefore;
        private int deathPresentationBefore;
        private int deathRequestedDamage;
        private int deathNativeDamage;
        private int deathThreshold;
        private float deathDifficulty;
        private int deathSubjectDamageBefore;
        private int deathSurvivorDamageBefore;
        private string deathSourceId;
        private int deathSettleFrames;
        private JObject deathEncounterEnded;
        // Native frames the encounter's end may take before the engine admits a
        // manual save again; the admission is recorded at the first frame and
        // at the judged frame either way.
        private const int DeathAdmissionSettleFrames = 600;

        private UnitEntityData DeathSubject => DeathSubjectIsMount ? mount : rider;
        private UnitEntityData DeathSurvivor => DeathSubjectIsMount ? rider : mount;

        private void AdvanceDeath()
        {
            if (clock.Elapsed.TotalSeconds > 300)
                throw new InvalidOperationException("P07 death boundary timed out at " + deathStage + " frames=" + deathFrames + ": " + persistence.Feedback);
            var game = Game.Instance;
            if (game == null) return;
            stage = 1200 + deathStage;
            if (deathStage == 5)
            {
                deathFrames++;
                if (LoadingProcess.Instance.IsLoadingInProcess || NativePersistenceIsolation.HasPendingWrites ||
                    game.CurrentlyLoadedArea == null || game.CurrentMode != Kingmaker.GameModes.GameModeType.Default || !callback ||
                    deathFrames < 10)
                { if (deathFrames > 7200) throw new InvalidOperationException("P07 death reload never completed: " + persistence.Feedback); return; }
                CompleteDeathReload();
                return;
            }
            if (LoadingProcess.Instance.IsLoadingInProcess) return;
            if (deathStage >= 1 && deathStage <= 2)
            {
                if (!targetService.RefreshBidirectionalCombatMemoryLease())
                    throw new InvalidOperationException("P07 death native combat memory fixture lease was lost.");
                if (game.IsPaused) { Write("fixture-native-unpause"); game.IsPaused = false; return; }
            }
            if (deathStage == 0)
            {
                if (!callback || NativePersistenceIsolation.HasPendingWrites || game.CurrentlyLoadedArea == null ||
                    game.CurrentMode != Kingmaker.GameModes.GameModeType.Default) return;
                Check(relationship.State == RelationshipState.Mounted && persistence.SnapshotCount == 1,
                    "P07-death-opens-with-one-real-mounted-save");
                rider = relationship.Rider; mount = relationship.Mount;
                deathRiderId = rider.UniqueId; deathMountId = mount.UniqueId;
                beforeControls = controls.CaptureSnapshot();
                var first = RecoveryArchive();
                deathFirstPath = first.FolderName; deathFirstHash = Hash(deathFirstPath);
                var read = NativeMountedSaveStorage.Read(first.Saver);
                Check(read.Kind == MountedSaveReadKind.Current && read.Data.Mounted &&
                    read.Data.Rider.Id == deathRiderId && read.Data.Mount.Id == deathMountId, "P07-death-first-archive-carries-the-pair");
                Write("native-write-complete", new JObject {
                    ["ordinal"] = 1, ["path"] = deathFirstPath, ["sha256"] = deathFirstHash,
                    ["length"] = new FileInfo(deathFirstPath).Length, ["nativeType"] = first.Type.ToString(),
                    ["nativeCallback"] = callback, ["operation"] = first.OperationState.ToString(),
                    ["snapshot"] = JObject.FromObject(read.Data, MountedSaveCodec.CreateSerializer()) });
                // A real native enemy, through the same target fixture every
                // combat case uses; the damage is the enemy's, under the
                // installed difficulty, never a direct state edit.
                targetService = new DiagnosticCombatTargetService(logger);
                var target = targetService.Spawn(rider, mount, FindDestination(8f), request.RunId, true, true);
                Check(targetService.PrepareForPlayerClick(target), "P07-death-enemy-prepared");
                Check(targetService.QueueBidirectionalCombatMemory(rider, target), "P07-death-native-combat-requested");
                deathFrames = 0; deathStage = 1;
                return;
            }
            if (deathStage == 1)
            {
                if (!game.Player.IsInCombat || !rider.CombatState.CanActInCombat)
                { if (++deathFrames > 3600) throw new InvalidOperationException("P07 death fixture never entered native combat."); return; }
                var target = targetService.Target;
                var subject = DeathSubject;
                deathDifficulty = game.Player.Difficulty.DamageToParty;
                deathThreshold = subject.Stats.HitPoints.ModifiedValue + subject.Stats.Constitution.ModifiedValue;
                var needed = deathThreshold + 1 - subject.Damage + subject.Stats.TemporaryHitPoints.ModifiedValue;
                Check(target != null && target.IsPlayersEnemy && !target.IsPlayerFaction && deathDifficulty > 0 &&
                    !float.IsNaN(deathDifficulty) && !float.IsInfinity(deathDifficulty) && needed > 0,
                    "P07-death-stimulus-has-its-exact-native-enemy-and-finite-difficulty");
                deathRequestedDamage = checked((int)Math.Ceiling((needed + 1d) / deathDifficulty));
                deathSubjectDamageBefore = subject.Damage; deathSurvivorDamageBefore = DeathSurvivor.Damage;
                deathSourceId = target.UniqueId;
                Check(relationship.State == RelationshipState.Mounted, "P07-death-pair-is-mounted-at-the-stimulus");
                var damage = Rulebook.Trigger(new RuleDealDamage(target, subject,
                    new DamageBundle(new DirectDamage(new DiceFormula(0, DiceType.Zero), deathRequestedDamage))));
                deathNativeDamage = damage.Damage;
                Check(damage.DamageBeforeDifficulty == deathRequestedDamage && game.Player.Difficulty.DamageToParty == deathDifficulty,
                    "P07-death-native-damage-was-delivered-under-unchanged-difficulty");
                Write("death-dispatched", DeathDetail(null));
                deathFrames = 0; deathStage = 2;
                return;
            }
            if (deathStage == 2)
            {
                var subject = DeathSubject;
                if (!subject.Descriptor.State.IsDead || relationship.State != RelationshipState.Unmounted || persistence.SaveSuspended)
                { if (++deathFrames > 3600) throw new InvalidOperationException("P07 native death or its cleanup never settled: dead=" + subject.Descriptor.State.IsDead + " relationship=" + relationship.State); return; }
                var after = controls.CaptureSnapshot();
                Check(after.DuplicateFactCount == 0 && !after.SerializationSuspended && !persistence.HasActiveSaveScope,
                    "P07-death-cleanup-left-no-duplicate-control-or-lease");
                Check(DeathSurvivor.IsInState && DeathSurvivor.Descriptor.State.IsConscious && DeathSurvivor.Damage == deathSurvivorDamageBefore,
                    "P07-death-left-the-independent-partner-alive-and-unharmed");
                Write("death-cleanup", DeathDetail(null));
                deathFrames = 0; deathStage = 3;
                return;
            }
            if (deathStage == 3)
            {
                // End the encounter through the target fixture's own teardown.
                if (!targetService.DestroyAndVerify()) { if (++deathFrames > 3600) throw new InvalidOperationException("P07 death enemy never left the world."); return; }
                if (game.Player.IsInCombat || rider.IsInCombat || mount.IsInCombat) return;
                if (game.IsPaused) { Write("fixture-native-unpause"); game.IsPaused = false; return; }
                // The engine's own save admission after the encounter, recorded
                // component by component BEFORE it is judged: runs
                // final95-p07-rider-death and -mount-death failed the combined
                // check with no engine log naming the refusing condition. The
                // admission is given the native frames the encounter's end may
                // need to settle, then judged exactly as measured.
                var admission = DeathAdmission(game);
                if (deathEncounterEnded == null)
                {
                    deathEncounterEnded = admission;
                    Write("death-encounter-ended", DeathDetail(new JObject { ["admission"] = admission }));
                }
                var allowed = (bool)admission["saveAllowed"];
                if (!allowed && ++deathSettleFrames < DeathAdmissionSettleFrames) return;
                Write("death-save-admission", DeathDetail(new JObject { ["admission"] = admission,
                    ["settleFrames"] = deathSettleFrames, ["encounterEnded"] = deathEncounterEnded }));
                Check(allowed, "P07-death-save-is-admitted-after-the-encounter");
                Check(DeathSubject.Descriptor.State.IsDead, "P07-native-death-persists-after-the-encounter");
                deathSemanticsBefore = persistence.SemanticRestoreCount; deathPresentationBefore = persistence.PresentationRestoreCount;
                callback = false;
                game.SaveGame(game.SaveManager.CreateNewSave(DeathSaveName), () => callback = true);
                deathFrames = 0; deathStage = 4;
                return;
            }
            if (deathStage == 4)
            {
                if (!callback || NativePersistenceIsolation.HasPendingWrites)
                { if (++deathFrames > 2400) throw new InvalidOperationException("P07 death save never completed: " + persistence.Feedback); return; }
                var saved = game.SaveManager.Single(s => s.Name == DeathSaveName && s.HasFileOnDisk);
                var read = NativeMountedSaveStorage.Read(saved.Saver);
                Check(saved.FileName == DeathSaveLeaf && saved.OperationState == SaveInfo.StateType.None &&
                    saved.GameId == request.Fixture.Working.GameId && read.Kind == MountedSaveReadKind.Current &&
                    !read.Data.Mounted && read.Data.Rider == null && read.Data.Mount == null &&
                    read.Data.CampaignId == request.Fixture.Working.GameId && persistence.SnapshotCount == 2,
                    "P07-death-save-records-no-pair-in-the-fixture-campaign");
                deathArchivePath = saved.FolderName; deathArchiveHash = Hash(deathArchivePath);
                Check(Hash(deathFirstPath) == deathFirstHash, "P07-death-save-left-the-first-archive-untouched");
                Check(DeathSubject.Descriptor.State.IsDead && DeathSurvivor.Descriptor.State.IsConscious &&
                    relationship.State == RelationshipState.Unmounted, "P07-native-death-persists-through-the-save");
                Write("death-saved", DeathDetail(new JObject { ["archive"] = new JObject {
                    ["path"] = deathArchivePath, ["leaf"] = saved.FileName, ["sha256"] = deathArchiveHash,
                    ["length"] = new FileInfo(deathArchivePath).Length, ["nativeType"] = saved.Type.ToString(),
                    ["internalName"] = saved.Name, ["gameId"] = saved.GameId, ["area"] = saved.Area?.AssetGuidThreadSafe,
                    ["operation"] = saved.OperationState.ToString(), ["kmcMember"] = read.Kind.ToString(),
                    ["snapshot"] = JObject.FromObject(read.Data, MountedSaveCodec.CreateSerializer()) } }));
                RequestRecoveryLoad(saved);
                deathFrames = 0; deathStage = 5;
            }
        }

        private void CompleteDeathReload()
        {
            var game = Game.Instance;
            var subject = game.State.Units.SingleOrDefault(u => u.UniqueId == (DeathSubjectIsMount ? deathMountId : deathRiderId));
            var survivor = game.State.Units.SingleOrDefault(u => u.UniqueId == (DeathSubjectIsMount ? deathRiderId : deathMountId));
            Write("death-reloaded", DeathDetail(new JObject {
                ["subjectPresent"] = subject != null, ["subjectDead"] = subject?.Descriptor.State.IsDead,
                ["survivorPresent"] = survivor != null, ["survivorConscious"] = survivor?.Descriptor.State.IsConscious,
                ["loadedDataMounted"] = persistence.LoadedData?.Mounted, ["semanticsDelta"] = persistence.SemanticRestoreCount - deathSemanticsBefore,
                ["presentationDelta"] = persistence.PresentationRestoreCount - deathPresentationBefore }));
            Check(relationship.State == RelationshipState.Unmounted && persistence.LoadedData != null && !persistence.LoadedData.Mounted &&
                persistence.SemanticRestoreCount == deathSemanticsBefore && persistence.PresentationRestoreCount == deathPresentationBefore &&
                controls.NativeCastRequestCount == 0 && controls.CaptureSnapshot().DuplicateFactCount == 0,
                "P07-reloading-the-death-save-invents-no-pair-and-restores-nothing");
            Check(subject != null && subject.Descriptor.State.IsDead && survivor != null && survivor.Descriptor.State.IsConscious,
                "P07-native-death-state-persists-across-the-reload");
            Check(Hash(deathArchivePath) == deathArchiveHash && Hash(deathFirstPath) == deathFirstHash,
                "P07-death-archives-are-byte-identical-after-the-reload");
            Dispose();
            Result = new RuntimeSubscenarioResult { Name = request.Scenario, Status = "PASS",
                AssertionPassCount = passed, AssertionFailCount = 0, Errors = new string[0] };
            Completed = true;
        }

        // The fresh-process control: the death save opens, nothing is invented,
        // the native death state is what the archive carried.
        private void AdvanceDeathCold()
        {
            if (clock.Elapsed.TotalSeconds > 150) throw new InvalidOperationException("P07 death cold load timed out.");
            var game = Game.Instance;
            if (game == null || LoadingProcess.Instance.IsLoadingInProcess || game.CurrentlyLoadedArea == null ||
                game.CurrentMode != Kingmaker.GameModes.GameModeType.Default) return;
            if (++deathFrames < 10) return;
            var party = game.Player.Party.ToArray();
            var dead = party.Where(u => u.Descriptor.State.IsDead).ToArray();
            var mounts = party.Where(u => SupportedMountedProfiles.IsSupported(u)).ToArray();
            var archive = Path.Combine(game.SaveManager.SavePath, request.PersistenceLoad.FileName);
            Write("initial");
            Write("death-cold-complete", new JObject {
                ["case"] = request.PersistenceCase, ["party"] = party.Length, ["deadPartyMembers"] = dead.Length,
                ["deadIds"] = new JArray(dead.Select(u => u.UniqueId)), ["supportedMounts"] = mounts.Length,
                ["mountDead"] = mounts.Length == 1 ? (bool?)mounts[0].Descriptor.State.IsDead : null,
                ["loadedDataPresent"] = persistence.LoadedData != null, ["loadedDataMounted"] = persistence.LoadedData?.Mounted,
                ["semantics"] = persistence.SemanticRestoreCount, ["presentation"] = persistence.PresentationRestoreCount,
                ["nativeCastRequests"] = controls.NativeCastRequestCount, ["feedback"] = persistence.Feedback,
                ["gameId"] = game.Player.GameId, ["area"] = game.CurrentlyLoadedArea.AssetGuidThreadSafe,
                ["archivePath"] = archive, ["archiveSha256"] = Hash(archive), ["expectedSha256"] = request.PersistenceLoad.Sha256 });
            Check(relationship.State == RelationshipState.Unmounted && persistence.LoadedData != null && !persistence.LoadedData.Mounted &&
                persistence.SemanticRestoreCount == 0 && persistence.PresentationRestoreCount == 0 && controls.NativeCastRequestCount == 0,
                "P07-cold-death-save-invents-no-pair-and-restores-nothing");
            Check(dead.Length == 1 && mounts.Length == 1 && mounts[0].Descriptor.State.IsDead == DeathSubjectIsMount &&
                (DeathSubjectIsMount ? dead[0] == mounts[0] : dead[0] != mounts[0]),
                "P07-cold-death-save-carries-exactly-the-native-death-it-recorded");
            Check(game.Player.GameId == request.PersistenceLoad.GameId && game.CurrentlyLoadedArea.AssetGuidThreadSafe == request.PersistenceLoad.Area &&
                Hash(archive) == request.PersistenceLoad.Sha256, "P07-cold-death-save-opened-its-own-world-byte-identical");
            Dispose();
            Result = new RuntimeSubscenarioResult { Name = request.Scenario, Status = "PASS",
                AssertionPassCount = passed, AssertionFailCount = 0, Errors = new string[0] };
            Completed = true;
        }

        // Every condition SaveManager.IsSaveAllowed (0x06008028) tests, plus the
        // subject's native life state, so a refusal names its own cause.
        private JObject DeathAdmission(Game game)
        {
            var subject = DeathSubject; var survivor = DeathSurvivor;
            var controllable = game.Player.ControllableCharacters;
            return new JObject {
                ["saveAllowed"] = game.SaveManager.IsSaveAllowed(),
                ["areaLoaded"] = game.CurrentlyLoadedArea != null,
                ["partyCombat"] = game.Player.IsInCombat,
                ["gameOverReason"] = game.Player.GameOverReason?.ToString(),
                ["mode"] = game.CurrentMode.ToString(),
                ["dialog"] = game.IsModeActive(Kingmaker.GameModes.GameModeType.Dialog),
                ["cutscene"] = game.IsModeActive(Kingmaker.GameModes.GameModeType.Cutscene),
                ["globalMapEncounter"] = game.Player.GlobalMap?.CurrentEncounterData != null,
                ["paused"] = game.IsPaused, ["loading"] = LoadingProcess.Instance.IsLoadingInProcess,
                ["subjectLifeState"] = subject.Descriptor.State.LifeState.ToString(),
                ["subjectDead"] = subject.Descriptor.State.IsDead, ["subjectFinallyDead"] = subject.Descriptor.State.IsFinallyDead,
                ["subjectInGame"] = subject.IsInGame, ["subjectDestroyed"] = subject.Destroyed,
                ["subjectHpLeft"] = subject.HPLeft, ["subjectDamage"] = subject.Damage,
                ["survivorLifeState"] = survivor.Descriptor.State.LifeState.ToString(),
                ["controllable"] = controllable.Count,
                ["controllableConscious"] = controllable.Count(u => u.Descriptor.State.IsConscious) };
        }

        private JObject DeathDetail(JObject extra)
        {
            var subject = DeathSubject; var survivor = DeathSurvivor;
            var detail = new JObject {
                ["case"] = request.PersistenceCase, ["stage"] = deathStage, ["frames"] = deathFrames,
                ["riderId"] = deathRiderId, ["mountId"] = deathMountId, ["subjectIsMount"] = DeathSubjectIsMount,
                ["sourceId"] = deathSourceId, ["requestedDamage"] = deathRequestedDamage, ["nativeDamage"] = deathNativeDamage,
                ["deathThreshold"] = deathThreshold, ["damageToParty"] = deathDifficulty,
                ["subjectDamageBefore"] = deathSubjectDamageBefore, ["survivorDamageBefore"] = deathSurvivorDamageBefore,
                ["subjectDamage"] = subject?.Damage, ["subjectDead"] = subject?.Descriptor.State.IsDead,
                ["subjectFinallyDead"] = subject?.Descriptor.State.IsFinallyDead,
                ["survivorDamage"] = survivor?.Damage, ["survivorConscious"] = survivor?.Descriptor.State.IsConscious,
                ["partyCombat"] = Game.Instance?.Player?.IsInCombat, ["relationship"] = relationship.State.ToString(),
                ["snapshots"] = persistence.SnapshotCount, ["failedSaves"] = persistence.FailedSaveCount,
                ["saveSuspended"] = persistence.SaveSuspended, ["activeScope"] = persistence.HasActiveSaveScope,
                ["semantics"] = persistence.SemanticRestoreCount, ["presentation"] = persistence.PresentationRestoreCount,
                ["nativeCastRequests"] = controls.NativeCastRequestCount,
                ["firstPath"] = deathFirstPath, ["firstHash"] = deathFirstHash,
                ["archivePath"] = deathArchivePath, ["archiveHash"] = deathArchiveHash, ["feedback"] = persistence.Feedback };
            if (extra != null) foreach (var property in extra.Properties()) detail[property.Name] = property.Value;
            return detail;
        }
    }
}
