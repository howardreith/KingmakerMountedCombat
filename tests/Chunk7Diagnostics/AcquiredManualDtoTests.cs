using KingmakerMountedCombat.Diagnostics;
using Newtonsoft.Json.Linq;
namespace KingmakerMountedCombat.Tests {
 public static class AcquiredManualDtoTests {
  public static string Project(string input) {
   var request=JObject.Parse(input).ToObject<RuntimeRequest>();var plan=request.AcquiredManual;
   return new JObject { ["selection"]=Chunk7EvidenceDto.Request(plan), ["defaultSelection"]=JObject.FromObject(plan), ["requestFamily"]=request.Scenario, ["requestSelectionActor"]=plan.ActorId }.ToString();
  }
 }
}
