using KingmakerMountedCombat.Diagnostics;
using Newtonsoft.Json.Linq;
namespace KingmakerMountedCombat.Tests
{
    public static class ProgressionDtoTests
    {
        public static string Project(string input)
        {
            var request = JObject.Parse(input);
            var selection = request["progression"].ToObject<Chunk7ProgressionSelection>();
            var descriptor = request["persistenceLoad"].ToObject<RuntimeSaveDescriptor>();
            return new JObject { ["selection"] = Chunk7EvidenceDto.Request(selection),
                ["descriptor"] = Chunk7EvidenceDto.Request(descriptor),
                ["defaultSelection"] = JObject.FromObject(selection),
                ["defaultDescriptor"] = JObject.FromObject(descriptor) }.ToString();
        }
    }
}
