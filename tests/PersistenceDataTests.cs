using System;
using System.IO;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

public static class PersistenceDataTests
{
    private static int count;
    private static void Check(bool value, string label)
    { if (!value) throw new Exception(label); count++; Console.WriteLine("PASS " + label); }
    public static void Run()
    {
        var rider = new SavedNativeActor { Id = "cfc488ed278f4549a497a17e8d40f08b", Standard = 6, Move = 1.25f, ReactionsRemaining = 0 };
        var data = new MountedSaveData { SchemaVersion = 1, CampaignId = "89c49a86-a171-4ea9-871a-f6b3b53d19b9",
            AreaId = new string('a',32), GameTimeTicks = 250000, Policy = MountedSaveData.PairedPolicy, RulesId = MountedSaveData.Rules,
            Mounted = true, ProfileId = "medium-humanoid-mammoth-v1", Rider = rider,
            Mount = new SavedNativeActor { Id = "97b3ae99e9f3420caf64e5ab8b49f992", Move = 3.125f },
            Slots = new[]{ new SavedMountedSlot { ActorId = rider.Id, Index = 17, Kind = 3 } } };
        var frozen = MountedSaveCodec.Encode(data);
        rider.Standard = 0;
        var decoded = MountedSaveCodec.Decode(frozen);
        Check(decoded.Kind == MountedSaveReadKind.Current && decoded.Data.Rider.Standard == 6 &&
            decoded.Data.Rider.Move == 1.25f && decoded.Data.Mount.Standard == 0 && decoded.Data.Mount.Move == 3.125f,
            "immutable snapshot preserves spent and available distinct actor debt");
        Check(decoded.Data.Slots[0].Index == 17 && decoded.Data.Slots[0].Kind == 3 &&
            decoded.Data.Rider.ReactionsRemaining == 0, "semantic controls and consumed reaction count round trip");
        Check(MountedSaveCodec.Decode(null).Kind == MountedSaveReadKind.Missing, "legacy save creates no pair");
        var future = frozen.Replace("\"SchemaVersion\":1", "\"SchemaVersion\":99");
        var futureRead = MountedSaveCodec.Decode(future);
        Check(futureRead.Kind == MountedSaveReadKind.Future && futureRead.OriginalJson == future, "future schema retained verbatim");
        foreach (var bad in new[]{
            frozen.Replace("\"Standard\":6","\"Standard\":NaN"),
            frozen.Replace("\"Standard\":6","\"Standard\":-1"),
            frozen.Replace("\"Standard\":6","\"Standard\":90000"),
            frozen.Replace("\"Standard\":6","\"Standard\":\"6\""),
            frozen.Replace("\"SchemaVersion\":1","\"SchemaVersion\":0"),
            frozen.Replace("\"Mounted\":true","\"Mounted\":true,\"mounted\":false"),
            frozen.Replace("\"Mounted\":true","\"Mounted\":1"),
            frozen.Replace("\"RulesId\":","\"$type\":\"System.IO.FileInfo\",\"RulesId\":"),
            frozen.Replace("medium-humanoid-mammoth-v1","unknown-profile"),
            frozen.Replace("97b3ae99e9f3420caf64e5ab8b49f992","cfc488ed278f4549a497a17e8d40f08b"),
            frozen.Replace("\"Index\":17","\"Index\":128"),
            frozen.Replace("\"Kind\":3","\"Kind\":19"),
            frozen.Replace("\"GameTimeTicks\":250000","\"GameTimeTicks\":-1"),
            frozen.Replace("\"Move\":1.25,",""),
            new string('x',32769), "[]", "{", "{\"SchemaVersion\":1}","/*comment*/"+frozen
        }) Check(MountedSaveCodec.Decode(bad).Kind == MountedSaveReadKind.Invalid, "malformed metadata fails closed");
        var duplicate = JObject.Parse(frozen);
        ((JArray)duplicate["Slots"]).Add(duplicate["Slots"][0].DeepClone());
        Check(MountedSaveCodec.Decode(duplicate.ToString()).Kind == MountedSaveReadKind.Invalid, "duplicate hotbar ownership rejected");
        var oversized = JObject.Parse(frozen);
        var slots = (JArray)oversized["Slots"];
        for (var i=0;i<129;i++) slots.Add(new JObject {["ActorId"]=rider.Id,["Index"]=i,["Kind"]=3});
        Check(MountedSaveCodec.Decode(oversized.ToString()).Kind == MountedSaveReadKind.Invalid, "unbounded slot data rejected");
        Console.WriteLine("PERSISTENCE DATA PASS="+count+" FAIL=0; native cold qualification remains separate.");
    }
}
