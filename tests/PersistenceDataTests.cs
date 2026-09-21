using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

public static class PersistenceDataTests
{
    private sealed class OptInGameResolver : DefaultContractResolver
    {
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            => base.CreateProperties(type, MemberSerialization.OptIn);
    }
    private static int count;
    private static void Check(bool value, string label)
    { if (!value) throw new Exception(label); count++; Console.WriteLine("PASS " + label); }
    private static void LoadAdmission(string json)
    {
        var read = MountedSaveCodec.Decode(json);
        var campaign = read.Data.CampaignId; var area = read.Data.AreaId;
        Check(MountedLoadAdmissionPolicy.Rejection(read, campaign, area, true, true, false, false, true) == null,
            "compatible current metadata is admitted without changing its saved debt");
        Check(MountedLoadAdmissionPolicy.Rejection(MountedSaveCodec.Decode(null), campaign, area, false, false, true, true, false) == null,
            "native legacy no-metadata load does not depend on mounted settings");
        var future = MountedSaveCodec.Decode(json.Replace("\"SchemaVersion\":2", "\"SchemaVersion\":99"));
        Check(MountedLoadAdmissionPolicy.Rejection(future, campaign, area, true, true, false, false, true) != null &&
            future.Data == null && future.OriginalJson.Contains("\"SchemaVersion\":99"),
            "future schema refuses before world replacement and preserves original data");
        Check(MountedLoadAdmissionPolicy.Rejection(MountedSaveCodec.Decode("{"), campaign, area, true, true, false, false, true) != null,
            "damaged metadata is not admitted as native fresh actions");
        Check(MountedLoadAdmissionPolicy.Rejection(read, Guid.NewGuid().ToString(), area, true, true, false, false, true) != null &&
            MountedLoadAdmissionPolicy.Rejection(read, campaign, new string('f',32), true, true, false, false, true) != null,
            "native campaign or area mismatch refuses without actor construction");
        Check(MountedLoadAdmissionPolicy.Rejection(read, campaign, area, false, true, false, false, true) != null,
            "disabled operation cannot silently restore mounted participation");
        foreach (var flags in new[] { new[] { false, false, false }, new[] { true, true, false }, new[] { true, false, true } })
            Check(MountedLoadAdmissionPolicy.Rejection(read, campaign, area, true, flags[0], flags[1], flags[2], true) != null,
                "incompatible active authority is reported rather than flipped");
        Check(MountedSaveCodec.Encode(read.Data) == json && read.OriginalJson == json,
            "load admission leaves every saved field and original JSON unchanged");
        var empty = new MountedSaveData { SchemaVersion = 2, CampaignId = campaign, AreaId = area,
            Policy = MountedSaveData.PairedPolicy, RulesId = MountedSaveData.Rules, Slots = new SavedMountedSlot[0] };
        Check(MountedLoadAdmissionPolicy.Rejection(MountedSaveCodec.Decode(MountedSaveCodec.Encode(empty)),
            campaign, area, false, false, false, false, false) == null,
            "empty current metadata supports disabled native loading without inventing a pair");
    }

    private static void CombatRoundTrip(string mountedJson)
    {
        var legacy = JObject.Parse(mountedJson);
        legacy["SchemaVersion"] = 1;
        legacy.Remove("Combat");
        var legacyJson = legacy.ToString(Formatting.None);
        var migrated = MountedSaveCodec.Decode(legacyJson);
        Check(migrated.Kind == MountedSaveReadKind.Current && migrated.Data.SchemaVersion == 2 &&
            migrated.Data.Combat == null && migrated.Data.Rider.Standard == 6 && migrated.OriginalJson == legacyJson,
            "schema1 migrates in memory without inventing combat or changing source data");

        var data = MountedSaveCodec.Decode(mountedJson).Data;
        var rider = data.Rider; var mount = data.Mount;
        Func<SavedNativeActor, SavedCombatActor> actor = native => new SavedCombatActor
        {
            Native = native, InCombat = true, Prepared = true, InitiativeRoll = 12, InitiativeRandom = 42,
            ExecutedAttacks = 1, LastMoveTicks = 200000, LastDeflectTicks = 0,
            DisengageTargets = new string[0],
            AiActions = new[]{new SavedAiAction { BlueprintId = new string('b',32), Cooldown = 3, Count = 1 }},
            AiDelayTicks = 1000000
        };
        data.Combat = new SavedCombatData
        {
            TurnBased = true, Round = 3, StartTicks = 0, RoundStartTicks = 100000, TurnStartTicks = 200000,
            TimeSinceStart = 0.025f, TimeToNextRound = 6f, HasEnemy = true, HadEnemy = true,
            Actors = new[]{ actor(rider), actor(mount) },
            Roster = new[]{new SavedRosterActor { ActorId = rider.Id, InitiativeProcessed = true },
                new SavedRosterActor { ActorId = mount.Id, InitiativeProcessed = true, Sequence = 1 }},
            Engagements = new SavedEngagement[0],
            Current = new SavedTurnContext { ActorId = rider.Id, Status = 3, Movement = new SavedMovementValues(),
                AttackMode = 1, MovementLimit = 1, GroundLimit = 0, SmartIndex = 3 },
            Allocations = new[]{new SavedMovementAllocation { ActorId = mount.Id, Round = 3, RoundStartTicks = 100000,
                Prepared = true, MoveObserved = mount.Move, StandardCommitted = true,
                GrantId = "d6727a02c73449b7ae3e7bcfb459288a:3",
                Movement = new SavedMovementValues { TimeMoved = 3.125f }}},
            Paired = new SavedPairedState
            {
                RiderId = rider.Id, MountId = mount.Id, BoundaryIsCurrent = true, SplitReleaseRound = -1, PendingSplitRound = -1,
                Partner = new SavedTurnContext { ActorId = mount.Id, Status = 3, Movement = new SavedMovementValues(),
                    AttackMode = 0, MovementLimit = 0, GroundLimit = 0, SmartIndex = -1 },
                Activation = new SavedActivation { EncounterId = "d6727a02c73449b7ae3e7bcfb459288a", Sequence = 3,
                    Rider = new SavedParticipation { Granted = true, Prepared = true, StandardObserved = 6f },
                    Mount = new SavedParticipation { Granted = true, Prepared = true, MoveObserved = 3.125f } }
            }
        };
        var json = MountedSaveCodec.Encode(data);
        var read = MountedSaveCodec.Decode(json);
        Check(MountedLoadAdmissionPolicy.Rejection(read, data.CampaignId, data.AreaId, true, true, false, false, true) == null &&
            MountedLoadAdmissionPolicy.Rejection(read, data.CampaignId, data.AreaId, true, true, false, false, false) != null,
            "combat mode mismatch refuses before native preparation without changing settings");
        Check(read.Kind == MountedSaveReadKind.Current && read.Data.Combat.Current.Status == 3 &&
            read.Data.Combat.Paired.Activation.ToSnapshot().Sequence == 3 &&
            read.Data.Combat.Actors[0].Native.ReactionsRemaining == 0 &&
            read.Data.Combat.Actors[0].AiActions[0].Count == 1 && read.Data.Combat.Actors[0].AiActions[0].Cooldown == 3,
            "combat turn, consumed reaction and native AI obligations round trip as primitives");
        Check(read.Data.Combat.Allocations[0].StandardCommitted && read.Data.Combat.Allocations[0].MoveObserved == 3.125f &&
            read.Data.Combat.Actors[1].Native.Standard == 0 && read.Data.Combat.Actors[0].AiDelayTicks == 1000000,
            "movement commitment stays separate from current debt and AI uses a remaining duration");
        var mutations = new Action<JObject>[]
        {
            root => root["Combat"]["Current"]["Status"] = 99,
            root => root["Combat"]["Current"]["Status"] = "3",
            root => root["Combat"]["Paired"]["Activation"]["Rider"]["Granted"] = false,
            root => root["Combat"]["Paired"]["Activation"]["Finalized"] = true,
            root => root["Combat"]["Actors"][0]["Native"]["Standard"] = 0,
            root => root["Combat"]["NextActor"] = "3faf445e-613a-4f58-a317-77bda58151e3",
            root => root["Combat"]["Actors"][0]["AiDelayTicks"] = -1,
            root => root["Combat"]["Current"]["UnknownControl"] = true,
            root => root["Combat"]["Actors"][0]["ReturnPoint"] = new JArray(1, 2),
            root => root["Combat"]["Actors"][0]["CachedInitiative"] = 1000001,
            root => { var clone = root["Combat"]["Actors"][0].DeepClone();
                clone["Native"]["Id"] = Guid.Parse((string)clone["Native"]["Id"]).ToString("D");
                ((JArray)root["Combat"]["Actors"]).Add(clone); }
        };
        foreach (var mutate in mutations)
        {
            var bad = JObject.Parse(json); mutate(bad);
            Check(MountedSaveCodec.Decode(bad.ToString()).Kind == MountedSaveReadKind.Invalid,
                "damaged combat participation or clock cannot create a safe-looking fallback");
        }
        var realtime = MountedSaveCodec.Decode(json).Data;
        var oldTurn = realtime.Combat.Current;
        realtime.Combat.TurnBased = false;
        realtime.Combat.Current = null; realtime.Combat.NextActor = null;
        realtime.Combat.Roster = new SavedRosterActor[0];
        realtime.Combat.Paired = null;
        realtime.Combat.Allocations = new SavedMovementAllocation[0];
        var rt = MountedSaveCodec.Decode(MountedSaveCodec.Encode(realtime));
        Check(rt.Kind == MountedSaveReadKind.Current && !rt.Data.Combat.TurnBased &&
            rt.Data.Combat.Current == null && rt.Data.Combat.Actors[0].Native.Standard == realtime.Rider.Standard &&
            rt.Data.Combat.Actors[0].Native.ReactionsRemaining == realtime.Rider.ReactionsRemaining,
            "real-time primitive combat snapshot retains debt without a turn grant");
        var invalidRt = JObject.Parse(MountedSaveCodec.Encode(realtime));
        invalidRt["Combat"]["Current"] = JObject.FromObject(oldTurn, MountedSaveCodec.CreateSerializer());
        Check(MountedSaveCodec.Decode(invalidRt.ToString()).Kind == MountedSaveReadKind.Invalid,
            "real-time snapshot cannot smuggle a native turn context");
    }

    public static void Run()
    {
        var rider = new SavedNativeActor { Id = "cfc488ed278f4549a497a17e8d40f08b", Standard = 6, Move = 1.25f, ReactionsRemaining = 0 };
        var data = new MountedSaveData { SchemaVersion = MountedSaveData.CurrentSchema, CampaignId = "89c49a86-a171-4ea9-871a-f6b3b53d19b9",
            AreaId = new string('a',32), GameTimeTicks = 250000, Policy = MountedSaveData.PairedPolicy, RulesId = MountedSaveData.Rules,
            Mounted = true, ProfileId = "medium-humanoid-mammoth-v1", Rider = rider,
            Mount = new SavedNativeActor { Id = "97b3ae99e9f3420caf64e5ab8b49f992", Move = 3.125f },
            Slots = new[]{ new SavedMountedSlot { ActorId = rider.Id, Index = 17, Kind = 3 } } };
        var frozen = MountedSaveCodec.Encode(data);
        var previousDefaults = JsonConvert.DefaultSettings;
        var defaultsConsulted = 0;
        try
        {
            JsonConvert.DefaultSettings = () =>
            {
                defaultsConsulted++;
                return new JsonSerializerSettings { ContractResolver = new OptInGameResolver(),
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects };
            };
            Check(JsonConvert.SerializeObject(data) == "{\"$id\":\"1\"}",
                "regression reproduces native global opt-in serializer erasing pair data");
            defaultsConsulted = 0;
            Check(MountedSaveCodec.Encode(data) == frozen && MountedSaveCodec.Decode(frozen).Data.Rider.Standard == 6,
                "archive codec round trips with native-style global JSON defaults installed");
            var evidence = JObject.FromObject(data.Rider, MountedSaveCodec.CreateSerializer());
            Check(evidence["Id"].Value<string>() == rider.Id && evidence["Standard"].Value<float>() == 6 &&
                defaultsConsulted == 0, "archive and native observations never consult global JSON defaults");
        }
        finally { JsonConvert.DefaultSettings = previousDefaults; }
        rider.Standard = 0;
        var decoded = MountedSaveCodec.Decode(frozen);
        Check(decoded.Kind == MountedSaveReadKind.Current && decoded.Data.Rider.Standard == 6 &&
            decoded.Data.Rider.Move == 1.25f && decoded.Data.Mount.Standard == 0 && decoded.Data.Mount.Move == 3.125f,
            "immutable snapshot preserves spent and available distinct actor debt");
        Check(decoded.Data.Slots[0].Index == 17 && decoded.Data.Slots[0].Kind == 3 &&
            decoded.Data.Rider.ReactionsRemaining == 0, "semantic controls and consumed reaction count round trip");
        Check(MountedSaveCodec.Decode(null).Kind == MountedSaveReadKind.Missing, "legacy save creates no pair");
        var future = frozen.Replace("\"SchemaVersion\":2", "\"SchemaVersion\":99");
        var futureRead = MountedSaveCodec.Decode(future);
        Check(futureRead.Kind == MountedSaveReadKind.Future && futureRead.OriginalJson == future, "future schema retained verbatim");
        foreach (var bad in new[]{
            frozen.Replace("\"Standard\":6","\"Standard\":NaN"),
            frozen.Replace("\"Standard\":6","\"Standard\":-1"),
            frozen.Replace("\"Standard\":6","\"Standard\":90000"),
            frozen.Replace("\"Standard\":6","\"Standard\":\"6\""),
            frozen.Replace("\"SchemaVersion\":2","\"SchemaVersion\":0"),
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
        LoadAdmission(frozen);
        CombatRoundTrip(frozen);
        Console.WriteLine("PERSISTENCE DATA PASS="+count+" FAIL=0; native cold qualification remains separate.");
    }
}
