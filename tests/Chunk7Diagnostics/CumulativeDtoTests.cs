using KingmakerMountedCombat.Diagnostics;
using Newtonsoft.Json.Linq;
namespace KingmakerMountedCombat.Tests {
 public static class CumulativeDtoTests {
  public static string Project(string input) {
   var request=JObject.Parse(input).ToObject<RuntimeRequest>();
   object selection=request.CumulativeAcquisition != null ? (object)request.CumulativeAcquisition : request.AcquiredManual;
   if(selection==null)throw new System.InvalidOperationException("Selected DTO missing.");
   return new JObject { ["selection"]=Chunk7EvidenceDto.Request(selection), ["defaultSelection"]=JObject.FromObject(selection), ["requestFamily"]=request.Scenario }.ToString();
  }
 }
}