using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace KingmakerMountedCombat.Diagnostics
{
    // Artifact writers do not inherit the host request serializer. Keep new
    // request-shaped evidence deliberate, and admit the deployed acquisition
    // artifact through its exact original DTO shape before projecting it.
    internal static class Chunk7EvidenceDto
    {
        internal static JObject Request(object value) => JObject.FromObject(value,
            new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() });

        internal static JObject ReadDeployedOrdinarySelection(JObject source, string sourceRunId)
        {
            var names = typeof(Chunk7OrdinaryAcquisitionSelection).GetProperties().Select(value => value.Name).ToArray();
            if (source == null || source.Properties().Count() != names.Length || names.Any(name => source.Property(name) == null))
                throw new InvalidOperationException("Paid acquisition source selection does not have the exact deployed DTO property names.");
            var plan = source.ToObject<Chunk7OrdinaryAcquisitionSelection>();
            if (plan == null || plan.Validate(Chunk7OrdinaryAcquisitionSelection.Scenario, sourceRunId).Count != 0 ||
                !JToken.DeepEquals(source, JObject.FromObject(plan)))
                throw new InvalidOperationException("Paid acquisition source selection differs from its exact native DTO types or pins.");
            return Request(plan);
        }
    }
}
