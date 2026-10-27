using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using KingmakerMountedCombat.Diagnostics;
public static class WorldResourcesAdapter
{
__METHOD__
    private static int passed;
    private static void Check(bool result, string name) { if (!result) throw new Exception(name); passed++; Console.WriteLine("PASS " + name); }
    private static JObject Window() => new JObject { ["gameTicks"] = 0L, ["actors"] = new JArray(new JObject {
        ["id"] = "exact-actor", ["move"] = 0.0, ["standard"] = 0.0, ["swift"] = 0.0,
        ["prepared"] = false, ["initiative"] = 0, ["reactionAllowance"] = 1 }) };
    private static JToken Actor(JObject window) => window["actors"][0];
    public static void Verify()
    {
        var before = Window(); var after = Window();
        Check(ResourcesConserved(before, after), "unchanged native resources are conserved");
        Actor(before)["move"] = 1.0; Actor(after)["move"] = 0.5; after["gameTicks"] = 5000000L;
        Check(ResourcesConserved(before, after), "ordinary real-time cooldown decay is conserved");
        before = Window(); after = Window(); Actor(after)["reactionAllowance"] = 2;
        Check(!ResourcesConserved(before, after), "reaction entitlement gain is refused");
        Actor(after)["reactionAllowance"] = 0;
        Check(!ResourcesConserved(before, after), "reaction expenditure change is refused");
        Actor(before)["reactionAllowance"] = -1; Actor(after)["reactionAllowance"] = -1;
        Check(ResourcesConserved(before, after), "unchanged native signed reaction observation is retained");
        before = Window(); after = Window(); Actor(after)["standard"] = 1.0;
        Check(!ResourcesConserved(before, after), "native action expenditure change is refused");
        after = Window(); Actor(after)["initiative"] = 1;
        Check(!ResourcesConserved(before, after), "native initiative change is refused");
        after = Window(); Actor(after)["prepared"] = true;
        Check(!ResourcesConserved(before, after), "native preparation grant is refused");
        after = Window(); after["actors"] = new JArray();
        Check(!ResourcesConserved(before, after), "missing native actor cannot prove conservation");
        Console.WriteLine("CHUNK7 WORLD RESOURCE OBSERVER PASS=" + passed + " FAIL=0; actual method and policy, primitive JSON observations only");
    }
}
