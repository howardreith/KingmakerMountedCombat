using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using KingmakerMountedCombat.Integration;
using KingmakerMountedCombat.Diagnostics;

internal static class Schema2ResaveContractTests
{
    private static int passed, failed;
    private static void Check(string name, Action action)
    { try { action(); passed++; Console.WriteLine("PASS " + name); } catch(Exception e) { failed++; Console.WriteLine("FAIL " + name + ": " + e); } }
    private static void Reject(string name, Action<MountedArchiveData> mutate)
    {
        Check(name, () => { var before = Fixture(); var after = Copy(before); mutate(after); bool refused = false;
            try { Chunk7PersistenceScenario.ValidateSchema2Continuity(before, after); } catch { refused = true; }
            if (!refused) throw new Exception("Changed native graph was accepted."); });
    }
    private static MountedArchiveData Fixture()
    {
        var old = (MountedSaveData)typeof(Chunk7PersistenceTests).GetMethod("Historical", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
        old.Combat.Actors[0].InCombat = false; old.Combat.Roster = new[] { old.Combat.Roster[1] };
        old.Combat.Current = (SavedTurnContext)typeof(Chunk7PersistenceTests).GetMethod("Turn", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { old.Mount.Id });
        old.Combat.Paired = new SavedPairedState { PendingSplitId = old.Mount.Id, PendingSplitRound = old.Combat.Round, SplitReleaseRound = -1 };
        return MountedArchiveCodec.Decode(MountedSaveCodec.Encode(old)).Data;
    }
    private static MountedArchiveData Copy(MountedArchiveData value) => MountedArchiveCodec.Decode(MountedArchiveCodec.Encode(value)).Data;
    private static void Debt(MountedArchiveData value, Action<SavedNativeActor> mutation)
    { foreach(var actor in value.Pairs.SelectMany(p => new[] { p.Rider, p.Mount }).Concat(value.Combat.Actors.Select(a => a.Native))) mutation(actor); }
    private static void Validate(MountedArchiveData before, MountedArchiveData after) => Chunk7PersistenceScenario.ValidateSchema2Continuity(before, after);
    public static int Main(string[] arguments)
    {
        if(arguments.Length != 1) throw new ArgumentException("Exact installed Unity dependency path required.");
        AppDomain.CurrentDomain.AssemblyResolve += (sender, request) =>
            new AssemblyName(request.Name).Name == "UnityEngine" ? Assembly.LoadFrom(arguments[0]) : null;
        Check("unchanged pending-only native graph is accepted without mutating input", () => { var before = Fixture(); var text = MountedArchiveCodec.Encode(before); Validate(before, Copy(before)); if (MountedArchiveCodec.Encode(before) != text) throw new Exception("Input changed."); });
        Check("native elapsed cooldowns preserve debt within the observation window", () => { var a = Fixture(); var b = Copy(a); b.GameTimeTicks += 1000000; Debt(b, x => { x.Standard -= .1f; x.Move -= .1f; x.Swift -= .1f; x.Reaction -= .1f; }); Validate(a,b); });
        Check("collection enumeration order does not change native actor identity", () => { var a=Fixture(); var b=Copy(a); b.Combat.Actors=b.Combat.Actors.Reverse().ToArray(); Validate(a,b); });
        Check("unrelated actor is preserved with its own exact expenditure", () => { var a=Fixture(); var native=(SavedNativeActor)typeof(Chunk7PersistenceTests).GetMethod("Actor",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null); var row=(SavedCombatActor)typeof(Chunk7PersistenceTests).GetMethod("CombatActor",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{native}); a.Combat.Actors=a.Combat.Actors.Concat(new[]{row}).ToArray(); Validate(a,Copy(a)); });
        Reject("resave refuses an extra activation", x => x.Participations[0].State.Activation = new SavedActivation());
        Reject("resave refuses a restarted pending round", x => x.Participations[0].State.PendingSplitRound++);
        Reject("resave refuses a restarted split-release round", x => x.Participations[0].State.SplitReleaseRound++);
        Reject("resave refuses a dropped pending mount", x => x.Participations = new SavedPairParticipation[0]);
        Reject("resave refuses a detached replacement owner", x => { x.Participations[0].MountedPairId=null; x.Participations[0].OwnerId=Guid.NewGuid().ToString("N"); });
        Reject("resave refuses changed stable owner identity", x => { var id=Guid.NewGuid().ToString("N"); x.Pairs[0].PairId=id; x.Participations[0].OwnerId=id; x.Participations[0].MountedPairId=id; });
        Reject("resave refuses changed stable generation", x => { x.Pairs[0].Generation++; x.Participations[0].Generation++; });
        Reject("resave refuses changed profile", x => x.Pairs[0].ProfileId="medium-humanoid-mammoth-v1");
        Reject("resave refuses removed actor expenditure", x => Debt(x, a => { a.Standard=0; a.Move=0; }));
        Reject("resave refuses extra native spending", x => Debt(x, a => a.Standard++));
        Reject("resave refuses free reactions", x => Debt(x, a => a.ReactionsRemaining++));
        Reject("resave refuses a hidden native preparation", x => x.Combat.Actors[0].Prepared=!x.Combat.Actors[0].Prepared);
        Reject("resave refuses changed current turn status", x => x.Combat.Current.Status=2);
        Reject("resave refuses changed movement grant", x => x.Combat.Allocations[0].GrantId="other-grant");
        Reject("resave refuses changed movement debt", x => x.Combat.Allocations[0].Movement.TimeMoved+=.25f);
        Reject("resave refuses removed native controls", x => x.Slots=new SavedMountedSlot[0]);
        Reject("resave refuses changed native round", x => x.Combat.Round++);
        Reject("resave refuses another campaign", x => x.CampaignId=Guid.NewGuid().ToString("N"));
        Reject("resave refuses another area", x => x.AreaId=new string('b',32));
        Reject("resave refuses a backward native clock", x => x.GameTimeTicks--);
        Reject("resave refuses an observation beyond thirty seconds", x => x.GameTimeTicks+=310000000L);
        Reject("resave refuses duplicate actor ownership", x => x.Pairs=x.Pairs.Concat(x.Pairs).ToArray());
        Check("faithful current pending projection uses unchanged historical codec",()=>{var source=Fixture();var before=MountedArchiveCodec.Encode(source);var view=Chunk7PersistenceScenario.ProjectOwnedPendingSchema2(source);var decoded=MountedArchiveCodec.Decode(MountedSaveCodec.Encode(view));if(decoded.Kind!=MountedSaveReadKind.Current||decoded.SourceSchema!=2)throw new Exception(decoded.Feedback);Validate(source,decoded.Data);if(before!=MountedArchiveCodec.Encode(source))throw new Exception("Projection mutated native source.");});
        Check("receipted schema2 accepts float wire precision while refusing changed metadata or restoration",()=>{
            var source=Fixture();source.Combat.Actors[0].ReturnPoint=new[]{-19.916172f,.992301166f,-9.76273f};
            var view=Chunk7PersistenceScenario.ProjectOwnedPendingSchema2(source);
            var raw=JObject.Parse(MountedSaveCodec.Encode(view));((JArray)raw["Combat"]["Actors"][0]["ReturnPoint"])[0]=new JValue(-19.916172d);
            var native=MountedArchiveCodec.Decode(raw.ToString(Newtonsoft.Json.Formatting.None));
            if(native.Kind!=MountedSaveReadKind.Current||native.SourceSchema!=2||native.HistoricalData==null||
                JToken.DeepEquals(raw,JObject.FromObject(view,MountedSaveCodec.CreateSerializer()))||
                !Chunk7PersistenceScenario.MatchesOwnedPendingSchema2(native,native.Data,view,raw))
                throw new Exception("Faithful historical input with float wire precision was refused.");
            var changed=(JObject)raw.DeepClone();((JArray)changed["Combat"]["Actors"][0]["ReturnPoint"])[0]=new JValue(-100d);
            if(Chunk7PersistenceScenario.MatchesOwnedPendingSchema2(native,native.Data,view,changed))
                throw new Exception("Changed receipt was accepted.");
            var restored=Copy(native.Data);restored.Combat.Round++;
            if(Chunk7PersistenceScenario.MatchesOwnedPendingSchema2(native,restored,view,raw))
                throw new Exception("Changed restoration was accepted.");
            view.Combat.Actors[0].Native.Standard++;
            if(Chunk7PersistenceScenario.MatchesOwnedPendingSchema2(native,native.Data,view,raw))
                throw new Exception("Changed projected debt was accepted.");
        });
        Check("historical writer preserves loaded schema3 instead of stale recapture",()=>{var source=Fixture();var original=MountedArchiveCodec.Encode(source);var historical=MountedSaveCodec.Decode(original);bool captured=false;var output=NativeMountedSaveFormat.CaptureOrPreserve(historical,()=>{captured=true;throw new Exception("Must preserve newer metadata.");});if(captured||output!=original)throw new Exception("Future preservation changed.");});
        Check("pending projection clones shared state and native debts",()=>{var source=Fixture();var view=Chunk7PersistenceScenario.ProjectOwnedPendingSchema2(source);if(ReferenceEquals(view.Combat,source.Combat)||ReferenceEquals(view.Combat.Paired,source.Participations[0].State)||ReferenceEquals(view.Rider,source.Pairs[0].Rider)||ReferenceEquals(view.Slots,source.Slots))throw new Exception("Projection aliases source.");view.Combat.Paired.PendingSplitRound++;if(view.Combat.Paired.PendingSplitRound==source.Participations[0].State.PendingSplitRound)throw new Exception("Pending state changed source.");});
        Check("existing migration codec remains sole source of deterministic owner identity",()=>{var source=Fixture();var id="2639ced4ae8a40d9bfec862dc1f25622";source.Pairs[0].PairId=id;source.Pairs[0].Generation=7;source.Participations[0].OwnerId=id;source.Participations[0].MountedPairId=id;source.Participations[0].Generation=7;var view=Chunk7PersistenceScenario.ProjectOwnedPendingSchema2(source);var decoded=MountedArchiveCodec.Decode(MountedSaveCodec.Encode(view));if(decoded.Kind!=MountedSaveReadKind.Current||decoded.Data.Pairs[0].PairId==id||decoded.Data.Pairs[0].Generation!=1||!MountedSaveData.SameActorDebt(view.Rider,decoded.Data.Pairs[0].Rider))throw new Exception("Wrong native or deterministic custody.");});
        foreach(var fault in new[]{"pair2","participation0","wrongpending","activation","kind5"})Check("owned projection refuses unrepresentable "+fault,()=>{var source=Fixture();switch(fault){case "pair2":source.Pairs=source.Pairs.Concat(new[]{source.Pairs[0]}).ToArray();break;case "participation0":source.Participations=new SavedPairParticipation[0];break;case "wrongpending":source.Participations[0].State.PendingSplitId=source.Pairs[0].Rider.Id;break;case "activation":source.Participations[0].State.RiderId=source.Pairs[0].Rider.Id;source.Participations[0].State.MountId=source.Pairs[0].Mount.Id;break;case "kind5":source.Slots[0].Kind=5;break;}bool denied=false;try{Chunk7PersistenceScenario.ProjectOwnedPendingSchema2(source);}catch{denied=true;}if(!denied)throw new Exception("Unrepresentable source accepted.");});
        Console.WriteLine("CHUNK7 SCHEMA2 RESAVE CONTRACT PASS="+passed+" FAIL="+failed+"; actual production continuity method, DTO fixtures only.");
        return failed==0?0:1;
    }
}
