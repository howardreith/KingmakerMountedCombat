using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using KingmakerMountedCombat.Integration;

public static class Chunk7PersistenceTests
{
    private static int count;
    private static void Check(bool condition, string text)
    { if (!condition) throw new Exception(text); count++; Console.WriteLine("PASS " + text); }
    private static void Reject(string json, string text)
    {
        var read = MountedArchiveCodec.Decode(json);
        Check(read.Kind == MountedSaveReadKind.Invalid && read.Data == null && read.OriginalJson == json, text);
    }
    private static SavedNativeActor Actor() => new SavedNativeActor { Id = Guid.NewGuid().ToString("N"),
        Standard = 6, Move = 3.125f, Swift = 1, Reaction = 6, ReactionsRemaining = 0, LastSurpriseTicks = 20 };
    private static SavedCombatActor CombatActor(SavedNativeActor actor) => new SavedCombatActor { Native = actor,
        InCombat = true, Prepared = true, AiActions = new SavedAiAction[0], DisengageTargets = new string[0] };
    private static SavedTurnContext Turn(string id) => new SavedTurnContext { ActorId = id, Status = 3,
        Movement = new SavedMovementValues { TimeMoved = 1f }, SmartIndex = -1 };
    private static MountedSaveData Historical()
    {
        var rider = Actor(); var mount = Actor();
        return new MountedSaveData { SchemaVersion = 2, CampaignId = Guid.NewGuid().ToString("N"),
            AreaId = new string('a',32), GameTimeTicks = 1000000,
            Policy = MountedSaveData.PairedPolicy, RulesId = MountedSaveData.Rules,
            Mounted = true, ProfileId = "medium-humanoid-horse-v1", Rider = rider, Mount = mount,
            Slots = new[] { new SavedMountedSlot { ActorId = rider.Id, Index = 5, Kind = 1 } },
            Combat = new SavedCombatData { TurnBased = true, Round = 2,
                Actors = new[] { CombatActor(rider), CombatActor(mount) },
                Roster = new[] { new SavedRosterActor { ActorId = rider.Id }, new SavedRosterActor { ActorId = mount.Id, Sequence = 1 } },
                Engagements = new SavedEngagement[0], Current = Turn(rider.Id),
                Allocations = new[] { new SavedMovementAllocation { ActorId = mount.Id, Round = 2,
                    Prepared = true, MoveObserved = 3.125f, StandardCommitted = true,
                    GrantId = "native:" + Guid.NewGuid().ToString("N"),
                    Movement = new SavedMovementValues { TimeMoved = 3.125f, TimeForced = 1f } } },
                Paired = new SavedPairedState { RiderId = rider.Id, MountId = mount.Id,
                    BoundaryIsCurrent = true, Partner = Turn(mount.Id), SplitReleaseRound = 2,
                    PendingSplitId = mount.Id, PendingSplitRound = 2, RenewalNotBeforeTicks = 2000000,
                    Activation = new SavedActivation { EncounterId = Guid.NewGuid().ToString("N"), Sequence = 2,
                        Split = true, Rider = new SavedParticipation { Granted = true, Prepared = true, StandardObserved = 6 },
                        Mount = new SavedParticipation { Granted = true, Prepared = true, MoveObserved = 3.125f } } }
            } };
    }
    private static MountedArchiveData Archive(int pairs)
    {
        var seed = Historical();
        var all = Enumerable.Range(0,pairs).Select(i => Historical()).ToArray();
        var data = MountedArchiveCodec.Decode(MountedSaveCodec.Encode(seed)).Data;
        data.Pairs = new SavedMountedPair[pairs]; data.Participations = new SavedPairParticipation[pairs];
        data.Slots = new SavedMountedSlot[0];
        data.Combat.Actors = all.SelectMany(x => x.Combat.Actors).ToArray();
        data.Combat.Roster = all.SelectMany(x => x.Combat.Roster).ToArray();
        data.Combat.Allocations = all.SelectMany(x => x.Combat.Allocations).ToArray();
        data.Combat.Current = all[0].Combat.Current;
        for (var i = 0; i < pairs; i++)
        {
            var old = all[i]; var id = Guid.NewGuid().ToString("N");
            data.Pairs[i] = new SavedMountedPair { PairId = id, Generation = i+1, ProfileId = old.ProfileId, Rider = old.Rider, Mount = old.Mount };
            var state = old.Combat.Paired;
            if (i != 0) { state.BoundaryIsCurrent = false; state.Boundary = old.Combat.Current; }
            data.Participations[i] = new SavedPairParticipation { OwnerId = id, Generation = i+1, MountedPairId = id, State = state };
        }
        return data;
    }
    public static void Run()
    {
        var old = Historical(); var json = MountedSaveCodec.Encode(old);
        var current = MountedArchiveCodec.Decode(json);
        Check(current.Kind == MountedSaveReadKind.Current && current.SourceSchema == 2 && current.OriginalJson == json &&
            current.Data.Pairs.Length == 1 && current.Data.Participations.Length == 1 && current.Data.Combat.Paired == null,
            "schema2 migrates one relationship and retained participation into one shared combat envelope");
        Check(current.Data.Combat.Actors[0].Native.Standard == 6 && current.Data.Combat.Allocations[0].MoveObserved == 3.125f &&
            current.Data.Participations[0].State.Activation.Split && current.Data.Participations[0].State.PendingSplitRound == 2 &&
            current.Data.Participations[0].State.RenewalNotBeforeTicks == 2000000,
            "migration retains expenditure, split, pending participation and renewal floor");
        Check(MountedSaveCodec.Encode(current.HistoricalData) == json && MountedSaveCodec.Encode(old) == json,
            "migration leaves original JSON and historical native adapter data untouched");
        Check(MountedArchiveCodec.Decode(json).Data.Pairs[0].PairId == current.Data.Pairs[0].PairId,
            "historical migration produces a stable custody identity");
        var version1 = JObject.Parse(json); version1["SchemaVersion"] = 1; version1.Remove("Combat");
        var v1json = version1.ToString(Formatting.None); var v1 = MountedArchiveCodec.Decode(v1json);
        Check(v1.Kind == MountedSaveReadKind.Current && v1.Data.Combat == null && v1.Data.Participations.Length == 0 &&
            v1.Data.Pairs[0].Rider.Standard == 6 && v1.OriginalJson == v1json,
            "schema1 migration preserves debt without inventing a combat authority");
        foreach (var source in new[] { v1json, json })
        {
            var bad = JObject.Parse(source); bad["Rider"]["Move"] = "3";
            Reject(bad.ToString(Formatting.None), "historical malformed primitive refuses before migration");
            Reject(source + new string(' ',32769), "historical character bound remains unchanged");
        }
        var detached = MountedSaveCodec.Decode(json).Data;
        detached.Mounted = false; detached.ProfileId = null; detached.Rider = null; detached.Mount = null;
        var detachedJson = MountedSaveCodec.Encode(detached); var migrated = MountedArchiveCodec.Decode(detachedJson);
        Check(migrated.Kind == MountedSaveReadKind.Current && migrated.Data.Pairs.Length == 0 &&
            migrated.Data.Participations.Length == 1 && migrated.Data.Participations[0].MountedPairId == null &&
            migrated.Data.Participations[0].State.BoundaryIsCurrent,
            "unmounted schema2 retains detached activation and pending split through migration");
        var pendingOnly = MountedSaveCodec.Decode(detachedJson).Data;
        var pendingId = pendingOnly.Combat.Paired.PendingSplitId;
        pendingOnly.Combat.Paired = new SavedPairedState { PendingSplitId = pendingId, PendingSplitRound = 2, SplitReleaseRound = -1 };
        var pendingJson = MountedSaveCodec.Encode(pendingOnly); var pending = MountedArchiveCodec.Decode(pendingJson);
        Check(pending.Kind == MountedSaveReadKind.Current && pending.Data.Pairs.Length == 0 &&
            pending.Data.Participations[0].State.PendingSplitId == pendingId && pending.Data.Participations[0].State.RiderId == null &&
            pending.Data.Participations[0].State.Activation == null && pending.OriginalJson == pendingJson,
            "legacy pending-only survivor migrates without inventing pair identities or an activation");
        // Historical removal can leave an attached rider outside combat while
        // only its mount retains a native participation exclusion.
        var pendingMounted = Historical();
        pendingMounted.Combat.Actors[0].InCombat = false;
        pendingMounted.Combat.Roster = new[] { pendingMounted.Combat.Roster[1] };
        pendingMounted.Combat.Current = Turn(pendingMounted.Mount.Id);
        pendingMounted.Combat.Paired = new SavedPairedState { PendingSplitId = pendingMounted.Mount.Id,
            PendingSplitRound = pendingMounted.Combat.Round, SplitReleaseRound = -1 };
        var pendingMountedJson = MountedSaveCodec.Encode(pendingMounted);
        Check(MountedSaveCodec.Decode(pendingMountedJson).Kind == MountedSaveReadKind.Current,
            "historical validator accepts attached rider outside combat with pending-only mount");
        var migratedPendingMounted = MountedArchiveCodec.Decode(pendingMountedJson);
        Check(migratedPendingMounted.Kind == MountedSaveReadKind.Current &&
            migratedPendingMounted.Data.Pairs.Length == 1 && migratedPendingMounted.Data.Participations.Length == 1 &&
            migratedPendingMounted.Data.Participations[0].MountedPairId == migratedPendingMounted.Data.Pairs[0].PairId &&
            migratedPendingMounted.Data.Participations[0].OwnerId == migratedPendingMounted.Data.Pairs[0].PairId,
            "mounted-plus-pending-only schema2 migration associates the retained obligation with its attachment");
        var pendingMountedState = migratedPendingMounted.Data.Participations[0].State;
        Check(pendingMountedState.RiderId == null && pendingMountedState.MountId == null &&
            pendingMountedState.Activation == null && !pendingMountedState.BoundaryIsCurrent &&
            pendingMountedState.Boundary == null && pendingMountedState.Partner == null &&
            pendingMountedState.PendingSplitId == pendingMounted.Mount.Id &&
            pendingMountedState.PendingSplitRound == pendingMounted.Combat.Round && pendingMountedState.SplitReleaseRound == -1 &&
            migratedPendingMounted.OriginalJson == pendingMountedJson &&
            MountedSaveCodec.Encode(migratedPendingMounted.HistoricalData) == pendingMountedJson,
            "mounted pending-only migration preserves exclusion rounds, original data and absent native activation");
        var preservedCombat = JObject.FromObject(pendingMounted.Combat, MountedSaveCodec.CreateSerializer());
        preservedCombat["Paired"] = null;
        Check(JToken.DeepEquals(preservedCombat, JObject.FromObject(migratedPendingMounted.Data.Combat,
            MountedSaveCodec.CreateSerializer())), "mounted pending-only migration preserves every shared native actor and allocation debt");
        var pendingMountedArchive = MountedArchiveCodec.Encode(migratedPendingMounted.Data);
        Check(MountedArchiveCodec.Decode(pendingMountedArchive).Kind == MountedSaveReadKind.Current,
            "linked pending-only ownership remains valid through schema3 encode and decode");
        var retained = Archive(2);
        var spent = retained.Pairs[0];
        retained.Participations[0].State = new SavedPairedState { PendingSplitId = spent.Mount.Id,
            PendingSplitRound = retained.Combat.Round, SplitReleaseRound = -1 };
        var spentActor = retained.Combat.Actors.Single(row => row.Native.Id == spent.Rider.Id);
        spentActor.InCombat = false; spentActor.Prepared = false;
        retained.Combat.Roster = retained.Combat.Roster.Where(row => row.ActorId != spent.Rider.Id).ToArray();
        retained.Combat.Current = Turn(retained.Pairs[1].Rider.Id);
        var mountGrant = retained.Combat.Allocations.Single(row => row.ActorId == spent.Mount.Id);
        retained.Combat.Allocations = retained.Combat.Allocations.Concat(new[] { new SavedMovementAllocation {
            ActorId = spent.Rider.Id, Round = retained.Combat.Round,
            RoundStartTicks = retained.Combat.RoundStartTicks, GrantId = mountGrant.GrantId,
            Prepared = true, Movement = new SavedMovementValues() } }).ToArray();
        Check(RetainedNativeReentryPolicy.SpentPendingRiders(retained).SequenceEqual(new[] { spent.Rider.Id }) &&
            !RetainedNativeReentryPolicy.SpentPendingRiders(retained).Contains(retained.Pairs[1].Rider.Id),
            "retained pending split protects only the exact spent rider, not its mount or neighboring pair");
        Check(RetainedNativeReentryPolicy.SameRetainedRound(retained.Combat, retained.Combat.Round,
                retained.Combat.StartTicks, retained.Combat.RoundStartTicks) &&
            !RetainedNativeReentryPolicy.SameRetainedRound(retained.Combat, retained.Combat.Round + 1,
                retained.Combat.StartTicks, retained.Combat.RoundStartTicks) &&
            !RetainedNativeReentryPolicy.SameRetainedRound(retained.Combat, retained.Combat.Round,
                retained.Combat.StartTicks + 1, retained.Combat.RoundStartTicks),
            "retained native join custody ends at a new round or new encounter without writing a resource");
        mountGrant.GrantId = "native:" + Guid.NewGuid().ToString("N");
        Check(RetainedNativeReentryPolicy.SpentPendingRiders(retained).Length == 0,
            "different native grants cannot borrow pending rider re-entry custody");
        var pendingNegative = JObject.Parse(pendingMountedArchive);
        pendingNegative["Participations"][0]["State"]["PendingSplitId"] = pendingMounted.Rider.Id;
        Reject(pendingNegative.ToString(Formatting.None), "pending-only linked ownership cannot claim the attached rider as its pending mount");
        pendingNegative = JObject.Parse(pendingMountedArchive);
        pendingNegative["Participations"][0]["MountedPairId"] = null;
        pendingNegative["Participations"][0]["OwnerId"] = Guid.NewGuid().ToString("N");
        Reject(pendingNegative.ToString(Formatting.None), "detached pending-only owner cannot claim an actively mounted actor");
        pendingNegative = JObject.Parse(pendingMountedArchive);
        var conflictingPending = pendingNegative["Participations"][0].DeepClone();
        conflictingPending["OwnerId"] = Guid.NewGuid().ToString("N"); conflictingPending["MountedPairId"] = null;
        ((JArray)pendingNegative["Participations"]).Add(conflictingPending);
        Reject(pendingNegative.ToString(Formatting.None), "a second retained owner cannot duplicate a mounted pending-only obligation");
        var crossPairPending = Archive(2);
        crossPairPending.Participations[0].State = new SavedPairedState {
            PendingSplitId = crossPairPending.Pairs[1].Mount.Id, PendingSplitRound = 2, SplitReleaseRound = -1 };
        Reject(JObject.FromObject(crossPairPending, MountedSaveCodec.CreateSerializer()).ToString(Formatting.None),
            "pending-only participation cannot link another pair's mount to its own owner");
        var unrelatedPending = Historical(); var unrelatedActor = Actor();
        unrelatedPending.Combat.Actors = unrelatedPending.Combat.Actors.Concat(new[] { CombatActor(unrelatedActor) }).ToArray();
        unrelatedPending.Combat.Paired = new SavedPairedState {
            PendingSplitId = unrelatedActor.Id, PendingSplitRound = 2, SplitReleaseRound = -1 };
        var unrelatedRead = MountedArchiveCodec.Decode(MountedSaveCodec.Encode(unrelatedPending));
        Check(unrelatedRead.Kind == MountedSaveReadKind.Current && unrelatedRead.Data.Participations[0].MountedPairId == null &&
            unrelatedRead.Data.Participations[0].State.PendingSplitId == unrelatedActor.Id,
            "an unrelated historical pending survivor remains detached instead of borrowing the active pair's custody");
        var emptyHistorical = Historical();
        emptyHistorical.Mounted = false; emptyHistorical.Rider = null; emptyHistorical.Mount = null; emptyHistorical.ProfileId = null;
        emptyHistorical.Combat.Paired = new SavedPairedState { SplitReleaseRound = -1, PendingSplitRound = -1 };
        var emptyHistoricalJson = MountedSaveCodec.Encode(emptyHistorical);
        var emptyMigrated = MountedArchiveCodec.Decode(emptyHistoricalJson);
        Check(emptyMigrated.Kind == MountedSaveReadKind.Current && emptyMigrated.Data.Participations.Length == 0 &&
            emptyMigrated.Data.Combat.Actors.Length == 2 && emptyMigrated.Data.Combat.Allocations[0].MoveObserved == 3.125f &&
            MountedSaveCodec.Encode(emptyMigrated.HistoricalData) == emptyHistoricalJson,
            "historical empty participation migrates without inventing an actorless owner or dropping shared debt");
        var two = Archive(2); var encoded = MountedArchiveCodec.Encode(two); var round = MountedArchiveCodec.Decode(encoded);
        Check(round.Kind == MountedSaveReadKind.Current && MountedArchiveCodec.Encode(round.Data) == encoded &&
            round.Data.Pairs.Length == 2 && round.Data.Participations.Length == 2,
            "two independent Medium pairs round-trip under schema3 with one native turn");
        var chargeSlots = JObject.Parse(encoded);
        chargeSlots["Slots"] = new JArray(new JObject { ["ActorId"] = two.Pairs[0].Rider.Id, ["Index"] = 127, ["Kind"] = 5 });
        Check(MountedArchiveCodec.Decode(chargeSlots.ToString(Formatting.None)).Kind == MountedSaveReadKind.Current,
            "schema3 retains the native Mounted Charge hotbar binding alongside the four historical controls");
        var legacyChargeSlot = JObject.Parse(json); legacyChargeSlot["Slots"][0]["Kind"] = 5;
        Reject(legacyChargeSlot.ToString(Formatting.None), "schema3 control breadth does not broaden historical schema2 acceptance");
        var mutations = new Action<JObject>[] {
            root => root["Pairs"][1]["Rider"] = root["Pairs"][0]["Mount"].DeepClone(),
            root => root["Pairs"][1]["PairId"] = root["Pairs"][0]["PairId"],
            root => root["Participations"][1]["MountedPairId"] = Guid.NewGuid().ToString("N"),
            root => root["Participations"][1]["Generation"] = 99,
            root => root["Pairs"][0]["Rider"]["Standard"] = 0,
            root => root["Participations"][1]["State"]["RiderId"] = Guid.NewGuid().ToString("N"),
            root => { root["Participations"][1]["State"]["BoundaryIsCurrent"] = true; root["Participations"][1]["State"]["Boundary"] = null; },
            root => root["Combat"]["Paired"] = root["Participations"][0]["State"].DeepClone(),
            root => root["Participations"][0]["MountedPairId"] = null,
            root => root["Pairs"][0]["Rider"]["Id"] = Guid.Parse((string)root["Pairs"][1]["Rider"]["Id"]).ToString("D")
        };
        for(var i=0;i<mutations.Length;i++)
        { var bad=JObject.Parse(encoded); mutations[i](bad); Reject(bad.ToString(Formatting.None), "schema3 cross-record mutation refuses " + i); }
        two.Pairs = new SavedMountedPair[0]; foreach(var p in two.Participations) p.MountedPairId = null;
        var noPairs = MountedArchiveCodec.Encode(two);
        Check(MountedArchiveCodec.Decode(noPairs).Data.Participations.Length == 2,
            "zero mounted records can retain two independent native participation obligations");
        var six = MountedArchiveCodec.Encode(Archive(6));
        Check(MountedArchiveCodec.Decode(six).Data.Pairs.Length == 6 && System.Text.Encoding.UTF8.GetByteCount(six) <= MountedArchiveCodec.MaximumBytes,
            "six representative pairs fit character, token, depth and archive-byte bounds together");
        var maximum = Archive(12);
        maximum.Pairs = maximum.Pairs.Take(6).ToArray();
        foreach (var participation in maximum.Participations.Skip(6)) participation.MountedPairId = null;
        maximum.Combat.Actors = maximum.Combat.Actors.Concat(Enumerable.Range(0,40).Select(i => CombatActor(Actor()))).ToArray();
        foreach (var actor in maximum.Combat.Actors)
            actor.AiActions = new[] { new SavedAiAction { BlueprintId = Guid.NewGuid().ToString("N"), Cooldown = 3, Count = 2 } };
        maximum.Combat.Roster = maximum.Combat.Actors.Select((a,i) => new SavedRosterActor { ActorId = a.Native.Id, Sequence = i }).ToArray();
        maximum.Combat.Allocations = maximum.Combat.Actors.Select(a => new SavedMovementAllocation {
            ActorId = a.Native.Id, Round = 2, Prepared = true, MoveObserved = 3.125f, StandardCommitted = true,
            GrantId = Guid.NewGuid().ToString("N") + ":" + long.MaxValue,
            Movement = new SavedMovementValues { TimeMoved = 3.125f, TimeForced = 1f, TimeStepped = 0.75f, MetresStepped = 1.25f }
        }).ToArray();
        maximum.Combat.Engagements = maximum.Combat.Actors.Select((a,i) => new SavedEngagement {
            From = a.Native.Id, To = maximum.Combat.Actors[(i+1)%maximum.Combat.Actors.Length].Native.Id, SinceTicks = 20 }).ToArray();
        maximum.Slots = maximum.Combat.Actors.SelectMany(a => new[] {
            new SavedMountedSlot { ActorId = a.Native.Id, Index = 0, Kind = 1 },
            new SavedMountedSlot { ActorId = a.Native.Id, Index = 127, Kind = 4 } }).ToArray();
        var maximumJson = MountedArchiveCodec.Encode(maximum);
        var maximumTokens = 0; var maximumDepth = 0;
        using (var stringReader = new StringReader(maximumJson))
        using (var tokenReader = new JsonTextReader(stringReader))
            while (tokenReader.Read()) { maximumTokens++; maximumDepth = Math.Max(maximumDepth, tokenReader.Depth); }
        var maximumBytes = System.Text.Encoding.UTF8.GetByteCount(maximumJson);
        Check(MountedArchiveCodec.Decode(maximumJson).Kind == MountedSaveReadKind.Current && maximum.Pairs.Length == 6 &&
            maximum.Participations.Length == 12 && maximum.Combat.Actors.Length == 64 && maximum.Combat.Allocations.Length == 64 &&
            maximum.Slots.Length == 128 && maximumJson.Length <= MountedArchiveCodec.MaximumCharacters &&
            maximumBytes <= MountedArchiveCodec.MaximumBytes && maximumTokens <= MountedArchiveCodec.MaximumTokens && maximumDepth <= 12,
            "representative six mounted plus six detached owners fit shared 64 actors and 128 slots within every wire bound");
        Console.WriteLine("CHUNK7 MAXIMUM REPRESENTATIVE chars=" + maximumJson.Length + "/" + MountedArchiveCodec.MaximumCharacters +
            " utf8Bytes=" + maximumBytes + "/" + MountedArchiveCodec.MaximumBytes + " tokens=" + maximumTokens + "/" +
            MountedArchiveCodec.MaximumTokens + " depth=" + maximumDepth + "/12; private primitive fixture, not a native maximum-save qualification");
        var bounds = JObject.Parse(six); ((JArray)bounds["Pairs"]).Add(bounds["Pairs"][0].DeepClone());
        Reject(bounds.ToString(Formatting.None), "seventh mounted record exceeds selected capacity");
        bounds=JObject.Parse(encoded); bounds["Slots"]=new JArray(Enumerable.Range(0,129).Select(i=>new JObject {
            ["ActorId"]=old.Combat.Actors[0].Native.Id,["Index"]=i,["Kind"]=1 }));
        Reject(bounds.ToString(Formatting.None), "owned slots remain bounded together with the world envelope");
        bounds=JObject.Parse(encoded); bounds["Combat"]["Actors"]=new JArray(Enumerable.Range(0,65).Select(i=>JObject.FromObject(CombatActor(Actor()),MountedSaveCodec.CreateSerializer())));
        Reject(bounds.ToString(Formatting.None), "combat actor bound remains shared across pairs");
        bounds=JObject.Parse(encoded); bounds["Participations"]=new JArray(Enumerable.Range(0,13).Select(i=>bounds["Participations"][0].DeepClone()));
        Reject(bounds.ToString(Formatting.None), "retained participation is independently bounded");
        var tooManyOwners = Archive(13); tooManyOwners.Pairs = tooManyOwners.Pairs.Take(6).ToArray();
        tooManyOwners.Participations = tooManyOwners.Participations.Skip(6).ToArray();
        foreach(var p in tooManyOwners.Participations) p.MountedPairId = null;
        var ownersJson = JObject.FromObject(tooManyOwners, MountedSaveCodec.CreateSerializer()).ToString(Formatting.None);
        Reject(ownersJson, "active and detached owners together respect the twelve-owner registry bound");
        Reject(encoded+new string(' ',MountedArchiveCodec.MaximumCharacters), "schema3 character overflow refuses");
        Reject("{\"SchemaVersion\":99,\"opaque\":\""+new string('馬',50000)+"\"}", "UTF8 byte overflow refuses even within the character bound");
        Reject("{\"SchemaVersion\":99,\"opaque\":["+string.Join(",",Enumerable.Repeat("0",MountedArchiveCodec.MaximumTokens))+"]}", "token overflow refuses within byte and character bounds");
        Reject("{\"SchemaVersion\":99,\"opaque\":"+new string('[',13)+"0"+new string(']',13)+"}", "depth overflow refuses");
        foreach(var source in new[]{v1json,json})
            Check(NativeMountedSaveFormat.CanRestore(NativeMountedSaveFormat.Decode(source)), "native historical adapter still selects schema1/2 restoration");
        foreach(var source in new[]{encoded,noPairs})
        {
            Check(MountedArchiveCodec.Decode(source).Kind == MountedSaveReadKind.Current,
                "foundation successfully decodes archive independently of native support");
            var native=NativeMountedSaveFormat.Decode(source);
            var captures=0;
            var kept=NativeMountedSaveFormat.CaptureOrPreserve(native,()=>{captures++;return old;});
            Check(!NativeMountedSaveFormat.CanRestore(native) && native.Data==null && kept==source && captures==0,
                "native adapter preserves schema3 without any one-pair capture or partial restore");
        }
        var future=encoded.Replace("\"SchemaVersion\":3","\"SchemaVersion\":99");
        Check(MountedArchiveCodec.Decode(future).Kind==MountedSaveReadKind.Future &&
            NativeMountedSaveFormat.CaptureOrPreserve(NativeMountedSaveFormat.Decode(future),()=>old)==future,
            "future payload remains opaque and unchanged");
        var unavailable=new MountedSaveReadResult(MountedSaveReadKind.Invalid,null,null,"unreadable");
        var refused=false;try{NativeMountedSaveFormat.CaptureOrPreserve(unavailable,()=>old);}catch(InvalidOperationException){refused=true;}
        Check(refused,"unreadable original metadata refuses writing instead of inventing empty state");
        Check(NativeMountedSaveFormat.WriterSchema==2 && NativeMountedSaveFormat.Encode(old)==json,
            "explicit historical adapter continues emitting unchanged schema2");
        var misleading=new MountedSaveReadResult(MountedSaveReadKind.Current,old,encoded,null,3);
        Check(!NativeMountedSaveFormat.CanRestore(misleading) && NativeMountedSaveFormat.CaptureOrPreserve(misleading,()=>old)==encoded,
            "Current status alone cannot authorize a newer native format");
        var selectedNativeSupport = NativeMountedSaveFormat.ArchiveNativeSupportEnabled;
        Check(selectedNativeSupport && NativeMountedSaveFormat.NativePairCapacity == 2 && NativeMountedSaveFormat.SelectedWriterSchema == 3,
            "reviewed world integration deliberately selects two-pair schema3 native support");
        NativeMountedSaveFormat.ArchiveNativeSupportEnabled = false;
        Check(!NativeMountedSaveFormat.CanRestore(round),
            "successful archive decoding does not select native support by itself");
        var gateRefused = false;
        try { NativeMountedSaveFormat.CaptureOrPreserve((MountedArchiveReadResult)null, () => round.Data); }
        catch (InvalidOperationException) { gateRefused = true; }
        Check(gateRefused, "schema3 native capture refuses without the explicit world capability selection");
        NativeMountedSaveFormat.ArchiveNativeSupportEnabled = true;
        try
        {
            var selectedJson = MountedArchiveCodec.Encode(Archive(NativeMountedSaveFormat.NativePairCapacity));
            var selected = MountedArchiveCodec.Decode(selectedJson);
            Check(NativeMountedSaveFormat.SelectedWriterSchema == 3 && NativeMountedSaveFormat.CanRestore(selected) &&
                NativeMountedSaveFormat.CaptureOrPreserve((MountedArchiveReadResult)null, () => selected.Data) == selectedJson,
                "selected all-pair native adapter accepts and writes the entire selected-capacity schema3 graph");
            var overCapacityJson = MountedArchiveCodec.Encode(Archive(6));
            var overCapacity = MountedArchiveCodec.Decode(overCapacityJson); var overCapacityCaptures = 0;
            Check(overCapacity.Kind == MountedSaveReadKind.Current && !NativeMountedSaveFormat.CanRestore(overCapacity) &&
                NativeMountedSaveFormat.CaptureOrPreserve(overCapacity, () => { overCapacityCaptures++; return round.Data; }) == overCapacityJson &&
                overCapacityCaptures == 0 && MountedLoadAdmissionPolicy.Rejection(overCapacity, overCapacity.Data.CampaignId,
                    overCapacity.Data.AreaId, true, true, false, false, true).Contains("capacity"),
                "offline six-pair decoding does not authorize restoration beyond the selected native capacity or reinterpret its original");
            var oversizedWriteRefused = false;
            try { NativeMountedSaveFormat.CaptureOrPreserve((MountedArchiveReadResult)null, () => overCapacity.Data); }
            catch (InvalidOperationException) { oversizedWriteRefused = true; }
            Check(oversizedWriteRefused, "selected native writing refuses to emit a graph beyond its admission capacity");
            foreach (var source in new[] { v1json, json, pendingMountedJson, emptyHistoricalJson })
            {
                var migratedNative = MountedArchiveCodec.Decode(source);
                Check(NativeMountedSaveFormat.CanRestore(migratedNative) &&
                    MountedLoadAdmissionPolicy.Rejection(migratedNative, migratedNative.Data.CampaignId,
                        migratedNative.Data.AreaId, true, true, false, false, true) == null,
                    "selected native adapter explicitly admits a valid historical migration");
            }
            var opaque = MountedArchiveCodec.Decode(future); var captures = 0;
            Check(!NativeMountedSaveFormat.CanRestore(opaque) && NativeMountedSaveFormat.CaptureOrPreserve(opaque,
                () => { captures++; return round.Data; }) == future && captures == 0,
                "selected new native adapter still preserves future metadata without capture");
            var fakeCurrent = new MountedArchiveReadResult { Kind = MountedSaveReadKind.Current, SourceSchema = 99,
                Data = round.Data, OriginalJson = future };
            Check(!NativeMountedSaveFormat.CanRestore(fakeCurrent), "a Current verdict cannot impersonate a supported native source schema");
            var invalid = new MountedArchiveReadResult { Kind = MountedSaveReadKind.Invalid };
            var denied = false;
            try { NativeMountedSaveFormat.CaptureOrPreserve(invalid, () => round.Data); }
            catch (InvalidOperationException) { denied = true; }
            Check(denied, "selected all-pair native adapter refuses to replace an unreadable original");
            var actorless = JObject.Parse(encoded);
            actorless["Participations"][0]["State"] = JObject.FromObject(new SavedPairedState {
                SplitReleaseRound = -1, PendingSplitRound = -1 }, MountedSaveCodec.CreateSerializer());
            Reject(actorless.ToString(Formatting.None), "schema3 cannot invent a retained owner with no native actor obligation");
        }
        finally { NativeMountedSaveFormat.ArchiveNativeSupportEnabled = selectedNativeSupport; }
        Console.WriteLine("CHUNK7 PERSISTENCE PASS="+count+" FAIL=0 (primitive production codec/native format boundary; no game saves)");
    }
}
