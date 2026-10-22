using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
namespace Kingmaker.EntitySystem.Entities
{
    internal sealed class UnitEntityData { internal string UniqueId; internal Blueprint Blueprint; internal Descriptor Descriptor = new Descriptor(); }
    internal sealed class Blueprint { internal string AssetGuid; }
    internal sealed class Ref { internal UnitEntityData Value; }
    internal sealed class Descriptor { internal UnitEntityData Pet; internal Ref Master = new Ref(); internal Progression Progression = new Progression(); }
    internal sealed class Progression { internal int Experience, CharacterLevel; internal List<ClassLevel> Classes = new List<ClassLevel>(); }
    internal sealed class ClassLevel { internal Blueprint CharacterClass; internal int LevelValue; internal int Level => LevelValue; }
}
namespace Kingmaker
{
    internal sealed class Player { internal int Money = 357290, ExperienceRatePercent = 100; internal List<Ref> RemoteCompanions = new List<Ref>(), PartyCharacters = new List<Ref>(); }
    internal sealed class Game { internal static Game Instance; internal Player Player = new Player(); }
}
namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed class Chunk7OwnedPairsSelection
    {
        /*ACTUAL_CONSTANTS*/
        /*ACTUAL_ROLES*/
        internal bool IsSixPairStress = true;
        internal string SourceArchiveSha256 = SixPairSourceArchive;
        internal string[] PairIds => SixPairRoles;
    }
    internal static class Chunk7AssistedColdInventorySelection { /*ACTUAL_COLD_SCENARIO*/ }
    internal sealed class Fixture
    {
        private readonly Chunk7OwnedPairsSelection selection = new Chunk7OwnedPairsSelection();
        internal readonly Kingmaker.Game game = Kingmaker.Game.Instance = new Kingmaker.Game();
        private JObject observations, sourceWorld, sourceArrangement, sixRemoteArrangement;
        private JObject[] ownerRows; private UnitEntityData sixRemote; private object sixRemoteDescriptor;
        private readonly RetainedObjectIdentity identities = new RetainedObjectIdentity(128);
        private static void Require(bool value, string text) { if (!value) throw new InvalidOperationException(text); }
        internal Fixture()
        {
            var remote = new UnitEntityData { UniqueId = "8265f8cf-0969-4ec7-a266-2448de91c8c4", Blueprint = new Blueprint { AssetGuid = "77c11edb92ce0fd408ad96b40fd27121" } };
            remote.Descriptor.Progression.Experience = 9000; remote.Descriptor.Progression.CharacterLevel = 4;
            remote.Descriptor.Progression.Classes.Add(new ClassLevel { CharacterClass = new Blueprint { AssetGuid = "772c83a25e2268e448e841dcd548235f" }, LevelValue = 4 });
            game.Player.RemoteCompanions.Add(new Ref { Value = remote });
            for (var i = 0; i < 6; i++) game.Player.PartyCharacters.Add(new Ref { Value = new UnitEntityData { UniqueId = selection.PairIds[i * 2] } });
        }
        internal UnitEntityData Remote => game.Player.RemoteCompanions[0].Value;
        internal JObject Arrangement => sourceArrangement;
        internal int Owners => ownerRows.Length;
        internal void Read(JObject artifact) => ReadSixPairSource(artifact);
        internal void Preserve() => RequireSixPairRemote();
        /*ACTUAL_READ*/
        /*ACTUAL_REMOTE*/
        /*ACTUAL_CAPTURE_REMOTE*/
        /*ACTUAL_ARRANGEMENT*/
    }
}
internal static class Program
{
    private static int count;
    private static void Check(bool value, string text) { if (!value) throw new Exception(text); count++; Console.WriteLine("PASS " + text); }
    private static void Refuse(Action action, string text) { var refused = false; try { action(); } catch (InvalidOperationException) { refused = true; } Check(refused, text); }
    public static void Main(string[] args)
    {
        var managed = Path.GetFullPath(Path.GetDirectoryName(args[1]));
        AppDomain.CurrentDomain.AssemblyResolve += (sender, request) =>
        {
            var name = new System.Reflection.AssemblyName(request.Name).Name;
            if (name != "UnityEngine" && !name.StartsWith("UnityEngine.", StringComparison.Ordinal)) return null;
            var path = Path.GetFullPath(Path.Combine(managed, name + ".dll"));
            if (!path.StartsWith(managed + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) return null;
            return File.Exists(path) ? System.Reflection.Assembly.LoadFrom(path) : null;
        };
        var source = JObject.Parse(File.ReadAllText(args[0]));
        var f = new KingmakerMountedCombat.Diagnostics.Fixture(); f.Read(source);
        Check(f.Owners == 6 && ((JArray)f.Arrangement["actors"]).Count == 12 && (int)f.Arrangement["money"] == 357290,
            "actual six-pair decoder projects all twelve retained native progression rows and funds");
        var before = f.CaptureSixPairRemote(); f.Preserve();
        Check(JToken.DeepEquals(before, f.CaptureSixPairRemote()), "actual remote custody retains its object/descriptor/progression without setup");
        foreach (var fault in new[] { "status", "run", "kind", "errors", "missing party", "changed settled party", "missing Horse", "role", "rank", "reciprocal", "wrong producer", "wrong archive", "metadata pair", "metadata participation", "metadata slot", "metadata combat" })
        {
            var v = (JObject)source.DeepClone(); var o = v["observations"];
            switch (fault)
            {
                case "status": v["status"] = "FAIL"; break; case "run": v["runId"] = "other"; break;
                case "kind": v["evidenceKind"] = "other"; break; case "errors": ((JArray)v["errors"]).Add("failed"); break;
                case "missing party": ((JArray)o["settledWorld"]["party"]).RemoveAt(0); break;
                case "changed settled party": o["settledWorld"]["party"][0]["experience"] = 9001; break;
                case "missing Horse": ((JArray)o["sixthHorseFacts"]).RemoveAt(5); break;
                case "role": o["sixthHorseFacts"][5]["mountId"] = o["sixthHorseFacts"][0]["mountId"].DeepClone(); break;
                case "rank": o["sixthHorseFacts"][5]["rank"] = 2; break;
                case "reciprocal": o["sixthHorseFacts"][5]["reciprocal"] = false; break;
                case "wrong producer": o["source"]["runId"] = "other"; break;
                case "wrong archive": o["source"]["archiveSha256"] = new string('f', 64); break;
                case "metadata pair": o["archive"]["pairs"] = 1; break;
                case "metadata participation": o["archive"]["participations"] = 1; break;
                case "metadata slot": o["archive"]["slots"] = 1; break;
                case "metadata combat": o["archive"]["combatPresent"] = true; break;
            }
            Refuse(() => new KingmakerMountedCombat.Diagnostics.Fixture().Read(v), "actual six-pair source decoder refuses " + fault);
        }
        foreach (var fault in new[] { "XP", "level", "class", "pet", "master", "remote replacement", "descriptor replacement", "removed remote", "extra remote", "money" })
        {
            f = new KingmakerMountedCombat.Diagnostics.Fixture(); f.Read(source); var remote = f.Remote;
            switch (fault)
            {
                case "XP": remote.Descriptor.Progression.Experience++; break; case "level": remote.Descriptor.Progression.CharacterLevel++; break;
                case "class": remote.Descriptor.Progression.Classes[0].LevelValue++; break;
                case "pet": remote.Descriptor.Pet = new UnitEntityData(); break; case "master": remote.Descriptor.Master.Value = new UnitEntityData(); break;
                case "remote replacement": f.game.Player.RemoteCompanions[0].Value = new UnitEntityData { UniqueId = remote.UniqueId }; break;
                case "descriptor replacement": remote.Descriptor = new Descriptor(); break;
                case "removed remote": f.game.Player.RemoteCompanions.Clear(); break;
                case "extra remote": f.game.Player.RemoteCompanions.Add(new Ref { Value = new UnitEntityData() }); break;
                case "money": f.game.Player.Money++; break;
            }
            Refuse(f.Preserve, "actual six-pair remote custody refuses " + fault);
        }
        foreach (var fault in new[] { "missing character", "extra Horse character", "Horse replaces rider", "duplicate character", "null character", "changed source characters" })
        {
            f = new KingmakerMountedCombat.Diagnostics.Fixture(); var v = (JObject)source.DeepClone(); var characters = f.game.Player.PartyCharacters;
            switch (fault)
            {
                case "missing character": characters.RemoveAt(5); break;
                case "extra Horse character": characters.Add(new Ref { Value = new UnitEntityData { UniqueId = KingmakerMountedCombat.Diagnostics.Chunk7OwnedPairsSelection.SixPairRoles[11] } }); break;
                case "Horse replaces rider": characters[5].Value.UniqueId = KingmakerMountedCombat.Diagnostics.Chunk7OwnedPairsSelection.SixPairRoles[11]; break;
                case "duplicate character": characters[5] = characters[0]; break;
                case "null character": characters[5].Value = null; break;
                case "changed source characters": v["observations"]["settledWorld"]["partyCharacterIds"][5] = KingmakerMountedCombat.Diagnostics.Chunk7OwnedPairsSelection.SixPairRoles[11]; break;
            }
            Refuse(() => f.Read(v), "actual source admission refuses " + fault);
        }
        Console.WriteLine("SIX-PAIR FIXTURE SOURCE/REMOTE PASS=" + count + " FAIL=0; actual methods on pinned b185 observation, external native objects only");
    }
}
