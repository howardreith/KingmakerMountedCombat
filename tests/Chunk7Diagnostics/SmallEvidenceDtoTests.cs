using System;using System.IO;using KingmakerMountedCombat.Diagnostics;using Newtonsoft.Json.Linq;
namespace KingmakerMountedCombat.Tests {
 public static class SmallEvidenceDtoTests {
  public static int Run(string inputPath,string outputPath){var input=JObject.Parse(File.ReadAllText(inputPath));var value=input["small"].ToObject<Chunk7SmallCalibrationSelection>();var ordinary=input["ordinary"].ToObject<Chunk7OrdinaryAcquisitionSelection>();var old=JObject.FromObject(value);var source=JObject.FromObject(ordinary);var projection=Chunk7EvidenceDto.Request(value);var normalized=Chunk7EvidenceDto.ReadDeployedOrdinarySelection(source,(string)input["sourceRunId"]);
   Need(old["Source"]!=null&&old["source"]==null,"default actual Small DTO exposes old casing mismatch");
   Need(JToken.DeepEquals(projection,input["small"]),"new Small request projection exactly matches actual nested request");
   Need(projection["source"]["sourceRunId"]!=null&&projection["source"]["SourceRunId"]==null,"nested source projection is explicit");
   Need(source["SmallRaceGuid"]!=null&&source["smallRaceGuid"]==null,"deployed ordinary source stays PascalCase");
   Need(JToken.DeepEquals(normalized,input["ordinary"]),"deployed ordinary source explicitly normalizes without aliases");
   Need(JToken.DeepEquals(Chunk7EvidenceDto.Request(value),JObject.Parse(projection.ToString())),"actual projected JSON preserves token shapes");
   File.WriteAllText(outputPath,new JObject{["deployedSmall"]=old,["small"]=projection,["deployedOrdinary"]=source,["ordinary"]=normalized,["nativeProof"]=false}.ToString());return 6;
  }
  static void Need(bool ok,string name){if(!ok)throw new Exception("FAIL "+name);Console.WriteLine("PASS "+name);}
 }
}