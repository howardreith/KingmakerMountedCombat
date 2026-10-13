using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Harmony12;
using Kingmaker;
using Kingmaker.EntitySystem.Persistence;
using KingmakerMountedCombat.Integration;

namespace KingmakerMountedCombat.Diagnostics
{
    // One bootstrap checkpoint, not persistence qualification. It loads only a
    // copied, request-hashed Working fixture with the original strict write ban.
    internal sealed class PersistenceIsolationBootstrap
    {
        internal const string Scenario = "persistence-isolation";
        internal static bool Supports(string value) => value == Chunk7CumulativeAcquisitionSelection.Scenario || value == Chunk7AcquiredManualSelection.Scenario || value == Chunk7ManualDerivedSelection.Scenario || value == Chunk7ManualSeedSelection.Scenario || value == Chunk7BootstrapInventorySelection.Scenario || value == Chunk7NativeBootstrapSelection.Scenario || value == Chunk7ProgressionSelection.Scenario || value == Chunk7SmallCalibrationSelection.Scenario || value == Chunk7OrdinaryFieldSelection.Scenario || value == Chunk7OrdinaryAcquisitionSelection.Scenario || RuntimeRequest.IsChunk7PersistenceScenario(value) || value == Scenario || value == "persistence-p01-save" || value == "persistence-p01-load" ||
            value == "persistence-p02-save" || value == "persistence-p02-load" || value == "persistence-p03-save" || value == "persistence-p03-load" || value == "persistence-p04-save" || value == "persistence-p04-load" ||
            value == "persistence-p07-save" || value == "persistence-p07-load" || value == "persistence-p05-save" || value == "persistence-p05-load" || value == "persistence-p06-load";
        internal PersistenceSaveAuthorization Authority => authority;
        private const string HarmonyId = "KingmakerMountedCombat.PersistenceIsolation";
        private readonly RuntimeRequest request;
        private static readonly FieldInfo UpdateTask = typeof(SaveManager).GetField("m_UpdateTask", BindingFlags.Instance | BindingFlags.NonPublic);
        private PersistenceSaveAuthorization authority;
        private bool redirected;
        internal bool Ready { get; private set; }
        internal string SaveRoot => authority?.Root;

        internal PersistenceIsolationBootstrap(RuntimeRequest request)
        {
            if (request == null || !Supports(request.Scenario) || request.Validate().Count != 0)
                throw new ArgumentException("An exact persistence bootstrap request is required.");
            this.request = request;
        }

        internal bool TryPrepare()
        {
            if (Ready) return true;
            if (request.Scenario == Chunk7SmallCalibrationSelection.Scenario) request.SmallCalibration.RequireCompiledDeclaration();
            var game = Game.Instance;
            if (game?.SaveManager == null || game.CurrentlyLoadedArea != null)
                throw new InvalidOperationException("Save isolation must be installed before a world is loaded.");
            // Let the old read-only menu enumeration finish before changing roots.
            if (!redirected)
            {
                if (UpdateTask == null || UpdateTask.MetadataToken != 0x04005410)
                    throw new InvalidOperationException("Native save enumeration task contract changed.");
                var priorTask = (Task)UpdateTask.GetValue(game.SaveManager);
                if (priorTask != null && !priorTask.IsCompleted) return false;
                var lab = Path.GetDirectoryName(Path.GetDirectoryName(request.EvidenceRoot));
                var runRoot = Path.Combine(lab, "runtime-staging", "persistence-" + request.RunId);
                var fixture = request.PersistenceLoad ?? request.Fixture.Working;
                var entries = new System.Collections.Generic.List<PersistenceSaveEntry>
                {
                    new PersistenceSaveEntry { FileName = fixture.FileName, InternalName = fixture.InternalName,
                        SaveType = request.Scenario == Chunk7AcquiredManualSelection.Scenario || request.Scenario == Chunk7ManualSeedSelection.Scenario || request.Scenario == Chunk7BootstrapInventorySelection.Scenario || request.Scenario == Chunk7SmallCalibrationSelection.Scenario || request.Scenario == Chunk7OrdinaryFieldSelection.Scenario || request.Scenario == Chunk7PersistenceScenario.LoadScenario ? request.ExpectedNativeLoadType :
                            request.Scenario == "persistence-p05-load" ? RuntimePersistenceScenario.SlotType(request.PersistenceCase).ToString() :
                            RuntimeRequest.IsTransitionAutoCase(request.PersistenceCase) ? "Auto" : "Manual",
                        Area = fixture.Area, InitialSha256 = fixture.Sha256, Writable = false }
                };
                if (request.Scenario == Chunk7ManualDerivedSelection.Scenario || request.Scenario == Chunk7CumulativeAcquisitionSelection.Scenario)
                    entries.Add(new PersistenceSaveEntry { FileName = Chunk7ManualDerivedSelection.OutputLeaf,
                        InternalName = RuntimePersistenceScenario.SlotName(SaveInfo.SaveType.Auto), SaveType = "Auto", Area = fixture.Area, Writable = true });
                if (request.Scenario == Chunk7ManualSeedSelection.Scenario)
                    entries.Add(new PersistenceSaveEntry { FileName = request.ManualSeed.OutputLeaf,
                        InternalName = Chunk7ManualSeedSelection.OutputName, SaveType = "Manual", Area = fixture.Area, Writable = true });
                if (request.Scenario == Chunk7ProgressionSelection.Scenario && request.Progression.WritesAutosave)
                    entries.Add(new PersistenceSaveEntry { FileName = Chunk7OrdinaryAcquisitionSelection.AutoLeaf,
                        InternalName = RuntimePersistenceScenario.SlotName(SaveInfo.SaveType.Auto), SaveType = "Auto", Area = fixture.Area, Writable = true });
                if (request.Scenario == Chunk7NativeBootstrapSelection.Scenario)
                {
                    var main = Chunk7NativeBootstrapScenario.ResolveMainPreset();
                    entries.Add(new PersistenceSaveEntry { FileName = Chunk7NativeBootstrapSelection.AutoLeaf,
                        InternalName = RuntimePersistenceScenario.SlotName(SaveInfo.SaveType.Auto), SaveType = "Auto",
                        Area = main.Area.AssetGuidThreadSafe, Writable = true, Campaign = "B", AdmitsBeforeArea = true });
                }
                if (request.Scenario == Chunk7OrdinaryAcquisitionSelection.Scenario)
                    entries.Add(new PersistenceSaveEntry { FileName = Chunk7OrdinaryAcquisitionSelection.AutoLeaf,
                        InternalName = RuntimePersistenceScenario.SlotName(SaveInfo.SaveType.Auto), SaveType = "Auto",
                        Area = fixture.Area, Writable = true });
                if (request.Scenario == Chunk7PersistenceScenario.SaveScenario)
                {
                    var type = Chunk7PersistenceScenario.NativeType(request.PersistenceCase);
                    entries.Add(new PersistenceSaveEntry { FileName = Chunk7PersistenceScenario.NativeLeaf(type),
                        InternalName = Chunk7PersistenceScenario.NativeName(type), SaveType = type.ToString(), Area = request.ExpectedChunk7ArchiveArea, Writable = true });
                }
                var schema1Resave = request.Scenario == Chunk7PersistenceScenario.LoadScenario &&
                    request.PersistenceCase == Chunk7PersistenceScenario.Schema1Case;
                var schema2Resave = request.Scenario == Chunk7PersistenceScenario.LoadScenario &&
                    request.PersistenceCase == "legacy-mounted-pending-tb-manual";
                if (schema2Resave)
                    entries.Add(new PersistenceSaveEntry { FileName = Chunk7PersistenceScenario.Schema2OutputLeaf,
                        InternalName = Chunk7PersistenceScenario.Schema2OutputName, SaveType = "Manual", Area = fixture.Area, Writable = true });
                if (schema1Resave)
                    entries.Add(new PersistenceSaveEntry { FileName = Chunk7PersistenceScenario.Schema1OutputLeaf,
                        InternalName = Chunk7PersistenceScenario.Schema1OutputName, SaveType = "Manual", Area = fixture.Area, Writable = true });
                if (request.Scenario == Chunk7PersistenceScenario.LoadScenario &&
                    (schema1Resave ? fixture.InternalName != "KMC_P01" || fixture.FileName != "Manual_812_KMC_P06.zks" :
                    fixture.InternalName != Chunk7PersistenceScenario.NativeName(Chunk7PersistenceScenario.NativeType(request.PersistenceCase))))
                    throw new InvalidOperationException("Chunk 7 cold source differs from its exact localized native category.");
                // A transition autosave is loaded read-only; the subsequent
                // ordinary write it must support lands in that same loaded area.
                // SaveManager.FindUnusedSaveNumber 06008024 takes the maximum
                // only over saves of the matching type, and this isolated root
                // holds just the Auto source, so native Manual numbering starts
                // at 1 here rather than at the 300 a staged Working fixture
                // produces. The declared leaf has to be the one the engine will
                // actually choose, or PrepareSave rejects its own descriptor.
                if (RuntimeRequest.IsTransitionAutoCase(request.PersistenceCase))
                    entries.Add(new PersistenceSaveEntry {
                        FileName = "Manual_1_KMC_P01.zks", InternalName = "KMC_P01",
                        SaveType = "Manual", Area = fixture.Area, Writable = true });
                if (request.Scenario == "persistence-p01-save" || request.Scenario == "persistence-p02-save" || request.Scenario == "persistence-p03-save" || request.Scenario == "persistence-p04-save") entries.Add(new PersistenceSaveEntry
                {
                    FileName = "Manual_300_KMC_P01.zks", InternalName = "KMC_P01", SaveType = "Manual", Area = fixture.Area,
                    Writable = true
                });
                // The drain case makes three real saves: the opening write, the
                // interrupted one, and the subsequent one. A committed
                // interruption consumes its own leaf, so the third is declared
                // rather than left to be refused mid-gate.
                if (request.Scenario == "persistence-p07-save" && request.PersistenceCase == "serialization-cancel")
                    entries.Add(new PersistenceSaveEntry {
                        FileName = "Manual_302_KMC_P01.zks", InternalName = "KMC_P01",
                        SaveType = "Manual", Area = fixture.Area, Writable = true });
                // Campaign B: the engine's own new game, declared before it starts.
                // Its identity is not known here and is never assigned here; the
                // authority freezes what the engine mints on B's first write. The
                // area is the authored preset's, resolved the same way the
                // scenario resolves the preset it starts.
                if (request.Scenario == "persistence-p07-save" && request.PersistenceCase == "campaign-b")
                {
                    var bootstrap = NativeCampaignBootstrap.Resolve();
                    entries.Add(new PersistenceSaveEntry {
                        FileName = NativeCampaignBootstrap.AutosaveLeaf,
                        InternalName = RuntimePersistenceScenario.SlotName(SaveInfo.SaveType.Auto),
                        SaveType = "Auto", Area = bootstrap.Area, Writable = true, Campaign = "B",
                        AdmitsBeforeArea = true });
                    entries.Add(new PersistenceSaveEntry {
                        FileName = NativeCampaignBootstrap.ManualLeaf, InternalName = NativeCampaignBootstrap.ManualName,
                        SaveType = "Manual", Area = bootstrap.Area, Writable = true, Campaign = "B" });
                }
                // The integration-absent process writes one NEW archive without
                // KMC; campaign B's cold process writes one NEW archive in B. Both
                // roots hold exactly one source, so native Manual numbering
                // continues from that source's own number.
                if (request.Scenario == "persistence-p07-load" && request.PersistenceCase == "absent-kmc")
                    entries.Add(new PersistenceSaveEntry { FileName = "Manual_302_KMC_ABSENT2.zks", InternalName = "KMC_ABSENT2",
                        SaveType = "Manual", Area = fixture.Area, Writable = true });
                if (request.Scenario == "persistence-p07-load" && request.PersistenceCase == "campaign-b")
                    entries.Add(new PersistenceSaveEntry { FileName = "Manual_303_KMC_B2.zks", InternalName = "KMC_B2",
                        SaveType = "Manual", Area = fixture.Area, Writable = true });
                if (request.Scenario == "persistence-p07-save" &&
                    (!RuntimeRequest.IsChargeLifecycleCase(request.PersistenceCase) || request.PersistenceCase == "mounted-charge-removal"))
                {
                    // A cross-area case makes no pre-transfer manual write: the
                    // native autosave is its departure evidence, and every manual
                    // leaf commits in the declared destination area. The save
                    // admitted across the transfer is still carrying the previous
                    // save's area, so exactly that leaf declares both endpoints.
                    var cross = RuntimeRequest.IsCrossAreaCase(request.PersistenceCase);
                    var afterEntry = cross && request.PersistenceAreaTarget.AutoSaveMode == "AfterEntry";
                    // Manual writes are requested after arrival, so both of their
                    // boundaries already observe the destination.
                    // SaveManager.CreateNewSave 06008015 passes the requested name
                    // through MakeNameUnique, so a second NEW archive under a name
                    // the list already holds would be renamed and refused. Campaign
                    // B's second A archive therefore carries its own exact name.
                    for (var n = 0; n < 2; n++)
                    {
                        var name = n == 1 && request.PersistenceCase == "campaign-b" ? NativeCampaignBootstrap.SecondFixtureName :
                            n == 1 && (request.PersistenceCase == "prepare-removal" || request.PersistenceCase == "mounted-charge-removal") ? MountedRemovalPreparation.CleanupSaveName :
                            n == 1 && RuntimeRequest.IsDeathCase(request.PersistenceCase) ? "KMC_DEATH" :
                            n == 1 && RuntimeRequest.IsEligibilityCase(request.PersistenceCase) ? "KMC_SIZE" : "KMC_P01";
                        entries.Add(new PersistenceSaveEntry {
                            FileName = "Manual_" + (300 + n) + "_" + name + ".zks", InternalName = name,
                            SaveType = "Manual",
                            Area = cross ? request.PersistenceAreaTarget.Area : fixture.Area,
                            Writable = true });
                    }
                    if (cross) entries.Add(new PersistenceSaveEntry {
                        FileName = "Auto_1.zks", InternalName = RuntimePersistenceScenario.SlotName(SaveInfo.SaveType.Auto),
                        SaveType = "Auto",
                        // BeforeExit commits in the departure area; AfterEntry commits
                        // in the destination but is admitted before PrepareSave.
                        Area = afterEntry ? request.PersistenceAreaTarget.Area : fixture.Area,
                        AdmissionArea = afterEntry ? fixture.Area : null,
                        Writable = true });
                }
                // The combat-mounted save-route checkpoint writes the same settled pair through the native quick
                // and auto routes after its manual leaf; both native slots are declared exactly once each.
                if (request.Scenario == "persistence-p04-save" && request.PersistenceCase == "combat-mount-routes")
                    foreach (var slot in new[] { SaveInfo.SaveType.Quick, SaveInfo.SaveType.Auto })
                        entries.Add(new PersistenceSaveEntry { FileName = slot + "_1.zks",
                            InternalName = RuntimePersistenceScenario.SlotName(slot), SaveType = slot.ToString(), Area = fixture.Area, Writable = true });
                if (request.Scenario == "persistence-p05-save" && request.PersistenceCase != "alternating" && request.PersistenceCase != "queued")
                {
                    var type = RuntimePersistenceScenario.SlotType(request.PersistenceCase);
                    var name = RuntimePersistenceScenario.SlotName(type);
                    for (var n = 0; n < 2; n++) entries.Add(new PersistenceSaveEntry {
                        FileName = type == SaveInfo.SaveType.Manual ? "Manual_" + (300 + n) + "_KMC_P01.zks" : type + "_" + (1 + n) + ".zks",
                        InternalName = name, SaveType = type.ToString(), Area = fixture.Area, Writable = true });
                }
                if (request.Scenario == "persistence-p05-save" && request.PersistenceCase == "queued")
                {
                    for (var n = 0; n < 3; n++)
                    {
                        var name = n == 2 ? "KMC_P01" : "KMC_P05_QUEUE_" + (n + 1);
                        entries.Add(new PersistenceSaveEntry { FileName = "Manual_" + (300 + n) + "_" + name + ".zks",
                            InternalName = name, SaveType = "Manual", Area = fixture.Area, Writable = true });
                    }
                }
                if (request.PersistenceCase == "alternating")
                {
                    if (request.Scenario == "persistence-p05-save")
                    {
                        entries.Add(new PersistenceSaveEntry { FileName = "Manual_300_KMC_P01.zks",
                            InternalName = "KMC_P01", SaveType = "Manual", Area = fixture.Area, Writable = true });
                        entries.Add(new PersistenceSaveEntry { FileName = "Manual_301_KMC_P05_UNMOUNTED.zks",
                            InternalName = "KMC_P05_UNMOUNTED", SaveType = "Manual", Area = fixture.Area, Writable = true });
                    }
                    else
                    {
                        var alternate = request.PersistenceAlternate;
                        entries.Add(new PersistenceSaveEntry { FileName = alternate.FileName, InternalName = alternate.InternalName,
                            SaveType = "Manual", Area = alternate.Area, InitialSha256 = alternate.Sha256, Writable = false });
                        entries.Add(new PersistenceSaveEntry { FileName = "Manual_302_KMC_P05_POST.zks",
                            InternalName = "KMC_P05_POST", SaveType = "Manual", Area = fixture.Area, Writable = true });
                    }
                }
                if (request.Scenario == "persistence-p06-load")
                {
                    var variant = request.PersistenceAlternate;
                    // The foreign-header derivative is campaign B's own archive: its
                    // native header carries B's validated identity, declared here
                    // read-only so the engine may enumerate and attempt it.
                    var foreignHeader = request.PersistenceCase == "foreign-header-campaign";
                    entries.Add(new PersistenceSaveEntry { FileName = variant.FileName, InternalName = variant.InternalName,
                        SaveType = "Manual", Area = variant.Area, InitialSha256 = variant.Sha256, Writable = false,
                        ForeignGameId = foreignHeader ? variant.GameId : null, ForeignGameName = foreignHeader ? variant.GameName : null });
                }
                if (request.Scenario == "persistence-p05-load" && fixture.InternalName !=
                    RuntimePersistenceScenario.SlotName(RuntimePersistenceScenario.SlotType(request.PersistenceCase)))
                    throw new InvalidOperationException("Cold native slot name differs from the exact localized category.");
                // Only a writing cross-area run declares a transition; the cold
                // process already opens in the destination and writes nothing.
                var declaresTransfer = request.Scenario == "persistence-p07-save" &&
                    RuntimeRequest.IsCrossAreaCase(request.PersistenceCase);
                authority = new PersistenceSaveAuthorization(runRoot, fixture.GameId, fixture.GameName,
                    request.Fixture.Baseline.Sha256, entries,
                    declaresTransfer ? fixture.Area : null,
                    declaresTransfer ? request.PersistenceAreaTarget.Area : null, (schema2Resave || request.Scenario == Chunk7CumulativeAcquisitionSelection.Scenario || request.Scenario == Chunk7AcquiredManualSelection.Scenario || request.Scenario == Chunk7ManualDerivedSelection.Scenario || request.Scenario == Chunk7ManualSeedSelection.Scenario || request.Scenario == Chunk7NativeBootstrapSelection.Scenario || request.Scenario == Chunk7OrdinaryAcquisitionSelection.Scenario ||
                        request.Scenario == Chunk7ProgressionSelection.Scenario && request.Progression.WritesAutosave ||
                        request.Scenario == Chunk7PersistenceScenario.SaveScenario && Chunk7PersistenceScenario.IsSaveBoundaryCase(request.PersistenceCase)) ? (int?)1 : null,
                    request.Scenario == Chunk7NativeBootstrapSelection.Scenario ? (int?)0 : schema2Resave ? (int?)1 : request.Scenario == Chunk7CumulativeAcquisitionSelection.Scenario || request.Scenario == Chunk7AcquiredManualSelection.Scenario || request.Scenario == Chunk7ManualDerivedSelection.Scenario || request.Scenario == Chunk7ManualSeedSelection.Scenario || request.Scenario == Chunk7BootstrapInventorySelection.Scenario || request.Scenario == Chunk7ProgressionSelection.Scenario || request.Scenario == Chunk7SmallCalibrationSelection.Scenario || request.Scenario == Chunk7OrdinaryFieldSelection.Scenario ? (int?)1 : request.PersistenceCase == Chunk7PersistenceScenario.ThreeLoadsCase ? (int?)3 :
                    request.PersistenceCase == Chunk7PersistenceScenario.ThreeAreasCase ? (int?)1 : null,
                    request.Scenario == Chunk7AcquiredManualSelection.Scenario ? request : null,
                    request.Scenario == Chunk7AcquiredManualSelection.Scenario ? game : null,
                    request.Scenario == Chunk7AcquiredManualSelection.Scenario ? game.SaveManager : null);
                var areas = Path.Combine(runRoot, "Areas");
                if (!Directory.Exists(areas) || Directory.EnumerateFileSystemEntries(areas).Any() ||
                    (File.GetAttributes(areas) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidOperationException("Bootstrap area stash must be an empty owned directory.");
                var harmony = HarmonyInstance.Create(HarmonyId);
                try
                {
                    NativePersistenceIsolation.Install(harmony);
                    NativePersistenceIsolation.Bind(authority);
                }
                catch { harmony.UnpatchAll(HarmonyId); throw; }
                redirected = true;
                game.SaveManager.UpdateSaveListAsync();
                return false;
            }
            if (!game.SaveManager.AreSavesUpToDate) return false;
            var saves = game.SaveManager.ToArray();
            var expectedCount = request.Scenario == "persistence-p06-load" || request.Scenario == "persistence-p05-load" && request.PersistenceCase == "alternating" ? 2 : 1;
            if (saves.Length != expectedCount || saves.Any(save => (request.Scenario == Chunk7AcquiredManualSelection.Scenario || request.Scenario == Chunk7NativeBootstrapSelection.Scenario
                ? authority.ValidateArchiveIdentity(Project(save), game.SaveManager.SavePath)
                : authority.Validate(RuntimeSaveOperation.Load, Project(save), game.SaveManager.SavePath)) != null))
                throw new InvalidOperationException("Isolated native enumeration differs from the single copied fixture.");
            if (request.Scenario == Chunk7AcquiredManualSelection.Scenario)
            {
                var source = saves.Single();
                if (source.Type != SaveInfo.SaveType.Auto || source.OperationState != SaveInfo.StateType.None ||
                    !source.IsActuallySaved || !source.HasFileOnDisk || !source.IsAutoLevelupSave ||
                    NativePersistenceIsolation.NativeSlotRotationEnabled || Game.Instance != game)
                    throw new InvalidOperationException("Acquired Auto native inventory is not settled before Manual reservation.");
                var number = game.SaveManager.FindUnusedSaveNumber(SaveInfo.SaveType.Manual);
                authority.ReserveAcquiredManualOutput(request, game, game.SaveManager, source, Project(source), number);
            }
            Ready = true;
            return true;
        }

        internal bool VerifyLoaded(string actualLoadedPath)
        {
            if (!Ready || actualLoadedPath != Path.Combine(SaveRoot, (request.PersistenceLoad ?? request.Fixture.Working).FileName) ||
                Game.Instance.SaveManager.SavePath != SaveRoot ||
                NativePersistenceIsolation.ObservedStashFolder != Path.Combine(Path.GetDirectoryName(SaveRoot), "Areas"))
                return false;
            using (var descriptor = Game.Instance.SaveManager.LoadZipSave(actualLoadedPath))
                return authority.ValidateArchiveIdentity(Project(descriptor), SaveRoot) == null;
        }

        private static RuntimeSaveTarget Project(SaveInfo save) => save == null ? null : new RuntimeSaveTarget
        {
            InternalName = save.Name, FileName = save.FileName, FullPath = save.FolderName,
            SaveType = save.Type.ToString(), GameId = save.GameId, GameName = save.GameName,
            Area = save.Area?.AssetGuidThreadSafe
        };
    }
}
