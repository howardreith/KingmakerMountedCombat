using KingmakerMountedCombat.Diagnostics;
using Newtonsoft.Json.Linq;
namespace KingmakerMountedCombat.Tests {
 public static class OwnedPairsDtoTests {
  public static string Project(string input) {
   var request=JObject.Parse(input).ToObject<RuntimeRequest>();
   if(request.OwnedPairs==null || request.OwnedPairs.Validate(request.Scenario,request.RunId,request.PersistenceCase).Count!=0) throw new System.InvalidOperationException("Typed owned selection refused.");
   return new JObject { ["selection"]=Chunk7EvidenceDto.Request(request.OwnedPairs),["scenario"]=request.SelectedChunk7Scenario,["expectedLoadType"]=request.ExpectedNativeLoadType,["expectedWriteCount"]=request.OwnedPairs.WriteCount(request.PersistenceCase),["fixtureArea"]=request.SelectedChunk7Fixture.Area,["requiresCombatColdBind"]=request.OwnedPairs.RequiresCombatColdBind(request.PersistenceCase) }.ToString();
  }
 }
}
