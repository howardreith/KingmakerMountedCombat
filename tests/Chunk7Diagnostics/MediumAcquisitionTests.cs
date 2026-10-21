using System;
using System.Linq;
using KingmakerMountedCombat.Diagnostics;
using Newtonsoft.Json.Linq;
namespace KingmakerMountedCombat.Tests
{
    public static class MediumAcquisitionTests
    {
        private static int passes;
        private static void Check(string name, bool value)
        { if (!value) throw new InvalidOperationException(name); passes++; Console.WriteLine("PASS " + name); }
        public static string Project(string raw)
        {
            var selection = JObject.Parse(raw)["request"]["manualDerived"].ToObject<Chunk7ManualDerivedSelection>();
            return Chunk7EvidenceDto.Request(selection).ToString();
        }
        public static int Run(string raw, string legacySmall, string legacyExisting)
        {
            passes = 0;
            var fixture = JObject.Parse(raw); var selection = fixture["request"]["manualDerived"].ToObject<Chunk7ManualDerivedSelection>();
            var inventory = (JObject)fixture["source"]["observations"]["nativeProfileInventory"];
            var before = inventory.ToString(); var pins = selection.MediumBody;
            Check("actual Medium selection validates", selection.Validate(Chunk7ManualDerivedSelection.Scenario, "current-run").Count == 0);
            Check("actual source body matches exact Medium template", Chunk7MediumHireBodySource.Matches(inventory, pins));
            Check("source body inspection has no mutation", inventory.ToString() == before);
            Action<string, Action<JObject>> reject = (name, mutate) => {
                var copy = (JObject)inventory.DeepClone(); mutate(copy);
                Check(name, !Chunk7MediumHireBodySource.Matches(copy,pins));
            };
            reject("source original Small refuses", value => value["party"][0]["originalSize"]="Small");
            reject("source effective Large refuses", value => value["party"][0]["effectiveSize"]="Large");
            reject("polymorphed source refuses", value => value["party"][0]["polymorphed"]=true);
            reject("numeric polymorph token refuses", value => value["party"][0]["polymorphed"]=0);
            reject("missing doll refuses", value => value["party"][0]["dollPresent"]=false);
            reject("string handedness refuses", value => value["party"][0]["dollLeftHanded"]="false");
            reject("other handedness refuses", value => value["party"][0]["dollLeftHanded"]=true);
            reject("other native race refuses", value => value["party"][0]["raceGuid"]=new string('c',32));
            reject("other native preset refuses", value => value["party"][0]["dollPresetGuid"]=new string('d',32));
            reject("doll race category mismatch refuses", value => value["party"][0]["dollRaceId"]="Elf");
            reject("other native sex refuses", value => value["party"][0]["dollGender"]="Female");
            reject("absent character refuses", value => value["party"][0]["character"]["present"]=false);
            reject("absent skeleton refuses", value => value["party"][0]["character"]["skeleton"]["present"]=false);
            reject("other native skeleton refuses", value => value["party"][0]["character"]["skeleton"]["name"]="DifferentRig");
            reject("array character refuses without coercion", value => value["party"][0]["character"]=new JArray());
            reject("array skeleton refuses without coercion", value => value["party"][0]["character"]["skeleton"]=new JArray());
            reject("ambiguous template refuses", value => ((JArray)value["party"]).Add(value["party"][0].DeepClone()));
            reject("non-object actor refuses", value => ((JArray)value["party"]).Add("actor"));
            reject("missing template refuses", value => value["party"][0]["id"]="99999999-1111-4111-8111-111111111111");
            reject("unbounded party refuses", value => {while(((JArray)value["party"]).Count<25)((JArray)value["party"]).Add(value["party"][0].DeepClone());});
            Check("null body pins refuse",!Chunk7MediumHireBodySource.Matches(inventory,null));
            Check("non-object inventory refuses",!Chunk7MediumHireBodySource.Matches(new JArray(),pins));
            Check("actual evidence serializer preserves Medium body tokens",JToken.DeepEquals(Chunk7EvidenceDto.Request(selection),fixture["request"]["manualDerived"]));
            foreach(var old in new[]{legacySmall,legacyExisting})
            {
                var request=JObject.Parse(old)["request"]["manualDerived"];var plan=request.ToObject<Chunk7ManualDerivedSelection>();
                Check("legacy JSON shape remains exact without additive null",JToken.DeepEquals(Chunk7EvidenceDto.Request(plan),request));
            }
            var cumulative=Chunk7EvidenceDto.Request(selection).ToObject<Chunk7CumulativeAcquisitionSelection>();
            cumulative.NativeWriteLeaf=Chunk7CumulativeAcquisitionSelection.OutputLeaf;
            cumulative.PriorOwnerId=selection.Readiness.MainActorId;cumulative.PriorPetId="99999999-1111-4111-8111-111111111111";
            Check("cumulative Medium validates in its own family",cumulative.Validate(Chunk7CumulativeAcquisitionSelection.Scenario,"current-run").Count==0);
            var expectedProjection=Chunk7EvidenceDto.Request(selection);expectedProjection["nativeWriteLeaf"]=Chunk7CumulativeAcquisitionSelection.OutputLeaf;
            Check("cumulative choice projection retains exact typed Medium pins and its separate Auto output",JToken.DeepEquals(Chunk7EvidenceDto.Request(cumulative.NativeChoicePins()),expectedProjection));
            cumulative.PriorPetId=cumulative.PriorOwnerId;Check("cumulative conflicting owner refuses",cumulative.Validate(Chunk7CumulativeAcquisitionSelection.Scenario,"current-run").Count!=0);
            return passes;
        }
    }
}
