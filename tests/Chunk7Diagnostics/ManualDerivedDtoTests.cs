using KingmakerMountedCombat.Diagnostics;
using Newtonsoft.Json.Linq;
namespace KingmakerMountedCombat.Tests {
 public static class ManualDerivedDtoTests {
  public static string Project(string input) {
   var plan=JObject.Parse(input)["manualDerived"].ToObject<Chunk7ManualDerivedSelection>();
   return new JObject { ["selection"]=Chunk7EvidenceDto.Request(plan), ["defaultSelection"]=JObject.FromObject(plan), ["defaultReadiness"]=JObject.FromObject(plan.Readiness) }.ToString();
  }
 }
}
