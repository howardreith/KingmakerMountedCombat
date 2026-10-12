using System;
using System.Linq;
using System.Collections.Generic;
public sealed class BlueprintFeature { public string AssetGuid; }
public static class RangerSelectionContractAdapter
{
    private const string DogFeatureGuid = "f894e003d31461f48a02f5caec4e3359";
    /*GUARD*/
    private static readonly string[] Stock = { "472091361cf118049a2b4339c4ea836a", DogFeatureGuid,
        "e992949eba096644784592dc7f51a5c7", "aa92fea676be33d4dafd176d699d7996", "2ee2ba60850dd064e8b98bf5c2c946ba",
        "ece6bde3dfc76ba4791376428e70621a", "67a9dc42b15d0954ca4689b13e8dedea" };
    private static int passed, failed;
    private static BlueprintFeature[] Valid() { return Stock.Select(id => new BlueprintFeature { AssetGuid = id }).ToArray(); }
    private static void Check(bool condition, string name) { Console.WriteLine((condition ? "PASS " : "FAIL ") + name); if(condition) passed++; else failed++; }
    private static string Refusal(BlueprintFeature[] values) {
        try { RequireExactRangerOptions(values, "AllFeatures"); return null; }
        catch(InvalidOperationException error) { return error.Message; }
    }
    public static int Verify() {
        var values = Valid(); Check(object.ReferenceEquals(values, RequireExactRangerOptions(values, "AllFeatures")), "exact legacy seven options retain the original array");
        var nullMessage = Refusal(null); Check(nullMessage != null, "null surface refuses");
        Check(nullMessage != null && nullMessage.Contains("observedCount=null") && nullMessage.Contains("observedFirst32=<null>"), "null failure is explicit");
        Check(Refusal(new BlueprintFeature[0]) != null, "empty surface refuses");
        Check(Refusal(Valid().Concat(new[]{new BlueprintFeature { AssetGuid = "foreign" }}).ToArray()) != null, "additional option never broadens admission");
        values = Valid(); values[0] = null; Check(Refusal(values) != null, "null entry refuses");
        values = Valid(); values[1] = values[0]; Check(Refusal(values) != null, "duplicate stock entry refuses");
        values = Valid(); Array.Reverse(values); Check(Refusal(values) != null, "reordered stock entries refuse");
        values = Valid(); values[0].AssetGuid = values[0].AssetGuid.ToUpperInvariant(); Check(Refusal(values) != null, "changed GUID casing refuses");
        var many = Enumerable.Range(0,1000).Select(index => new BlueprintFeature { AssetGuid = "observed-" + index.ToString("D4") }).ToArray();
        var message = Refusal(many); Check(message != null && message.Contains("observedCount=1000") && message.Contains("observed-0031") && !message.Contains("observed-0032"), "failure retains count and bounds emitted identities at32");
        Check(message != null && message.Length < 800, "large foreign surface does not produce an unbounded log");
        Console.WriteLine("TOTAL PASS="+passed+" FAIL="+failed); return failed;
    }
    public static int Main() { return Verify() == 0 ? 0 : 1; }
}
