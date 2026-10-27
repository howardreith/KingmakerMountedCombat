using System;
using System.IO;
using System.Linq;
using KingmakerMountedCombat.Diagnostics;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Tests
{
    public static class EvidenceDtoTests
    {
        private static int passes;
        public static int Run(string inputPath, string outputPath)
        {
            var input = JObject.Parse(File.ReadAllText(inputPath));
            var ordinary = input["ordinary"].ToObject<Chunk7OrdinaryAcquisitionSelection>();
            var field = input["field"].ToObject<Chunk7OrdinaryFieldSelection>();
            var run = (string)input["sourceRunId"];
            var deployed = JObject.FromObject(ordinary);
            var fieldDeployed = JObject.FromObject(field);
            var immutable = deployed.ToString();
            Good("actual deployed acquisition DTO is PascalCase", deployed["SmallRaceGuid"] != null && deployed["smallRaceGuid"] == null);
            Good("actual old field DTO has the reproduced canonical mismatch", !JToken.DeepEquals(fieldDeployed, input["field"]));
            var projected = Chunk7EvidenceDto.ReadDeployedOrdinarySelection(deployed, run);
            Good("exact actual deployed DTO projects all request pins", JToken.DeepEquals(projected, input["ordinary"]));
            Good("reader projection does not mutate the deployed artifact", deployed.ToString() == immutable);
            Good("new field evidence has exact request shape", JToken.DeepEquals(Chunk7EvidenceDto.Request(field), input["field"]));
            Good("serialized wire source round-trips through exact projection", JToken.DeepEquals(
                Chunk7EvidenceDto.ReadDeployedOrdinarySelection(JObject.Parse(deployed.ToString()), run), projected));
            Bad("camel aliases do not replace the deployed source contract", () => Chunk7EvidenceDto.ReadDeployedOrdinarySelection(projected, run));
            Bad("null source", () => Chunk7EvidenceDto.ReadDeployedOrdinarySelection(null, run));
            Change("one lower-case alias", deployed, run, value => { value["case"] = value["Case"]; value.Remove("Case"); });
            Change("mixed casing with duplicate semantic field", deployed, run, value => value["smallRaceGuid"] = value["SmallRaceGuid"]);
            Change("missing deployed property", deployed, run, value => value.Remove("ExistingActorId"));
            Change("unknown deployed property", deployed, run, value => value["unknown"] = true);
            Change("text boolean cannot be coerced", deployed, run, value => value["LeftHanded"] = "false");
            Change("integer boolean cannot be coerced", deployed, run, value => value["LeftHanded"] = 0);
            Change("text level cannot be coerced", deployed, run, value => value["MinimumRangerLevel"] = "4");
            Change("integral floating level cannot be coerced", deployed, run, value => value["MinimumRangerLevel"] = 4.0);
            Change("fractional level cannot be rounded", deployed, run, value => value["MinimumRangerLevel"] = 4.2);
            Change("level exceeds declared native range", deployed, run, value => value["MinimumRangerLevel"] = 21);
            Change("write count is exact", deployed, run, value => value["ExpectedNativeWriteCount"] = 2);
            Change("native save leaf is exact", deployed, run, value => value["NativeWriteLeaf"] = "Auto_2.zks");
            Change("source cannot reuse current run", deployed, run, value => value["InventoryRunId"] = run);
            Change("declared boolean remains required", deployed, run, value => value["LeftHanded"] = null);
            File.WriteAllText(outputPath, new JObject { ["deployedOrdinary"] = deployed, ["deployedField"] = fieldDeployed,
                ["ordinary"] = projected, ["field"] = Chunk7EvidenceDto.Request(field), ["passes"] = passes,
                ["nativeProof"] = false }.ToString());
            return passes;
        }
        private static void Change(string label, JObject original, string run, Action<JObject> edit)
        {
            Bad(label, () => { var changed = (JObject)original.DeepClone(); edit(changed); Chunk7EvidenceDto.ReadDeployedOrdinarySelection(changed, run); });
        }
        private static void Good(string name, bool value)
        {
            if (!value) throw new InvalidOperationException("FAIL " + name);
            Console.WriteLine("PASS " + name); passes++;
        }
        private static void Bad(string name, Action action)
        {
            try { action(); } catch (Exception) { Good(name, true); return; }
            throw new InvalidOperationException("FAIL accepted " + name);
        }
    }
}
