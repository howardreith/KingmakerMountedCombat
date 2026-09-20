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
        internal static bool Supports(string value) => value == Scenario || value == "persistence-p01-save" || value == "persistence-p01-load";
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
                        SaveType = "Manual", Area = fixture.Area, InitialSha256 = fixture.Sha256, Writable = false }
                };
                if (request.Scenario == "persistence-p01-save") entries.Add(new PersistenceSaveEntry
                {
                    FileName = "Manual_300_KMC_P01.zks", InternalName = "KMC_P01", SaveType = "Manual", Area = fixture.Area,
                    Writable = true
                });
                authority = new PersistenceSaveAuthorization(runRoot, fixture.GameId, fixture.GameName,
                    request.Fixture.Baseline.Sha256, entries);
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
            if (saves.Length != 1 || authority.Validate(RuntimeSaveOperation.Load,
                Project(saves[0]), game.SaveManager.SavePath) != null)
                throw new InvalidOperationException("Isolated native enumeration differs from the single copied fixture.");
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
                return authority.Validate(RuntimeSaveOperation.Load, Project(descriptor), SaveRoot) == null;
        }

        private static RuntimeSaveTarget Project(SaveInfo save) => save == null ? null : new RuntimeSaveTarget
        {
            InternalName = save.Name, FileName = save.FileName, FullPath = save.FolderName,
            SaveType = save.Type.ToString(), GameId = save.GameId, GameName = save.GameName,
            Area = save.Area?.AssetGuidThreadSafe
        };
    }
}
