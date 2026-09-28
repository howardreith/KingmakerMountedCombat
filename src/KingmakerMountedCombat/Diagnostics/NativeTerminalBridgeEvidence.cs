using System;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace KingmakerMountedCombat.Diagnostics
{
    // Pure binding of a passive native window to one exact relationship terminal.
    // Command execution and resource replay retain their separate validators.
    internal static class NativeTerminalBridgeEvidence
    {
        private static void Require(bool ok,string reason){if(!ok)throw new InvalidOperationException("Terminal resource bridge: "+reason);}
        private static string Text(JToken v)=>v?.Type==JTokenType.String?(string)v:null;
        private static long Int(JToken v){Require(v?.Type==JTokenType.Integer,"integer missing");return (long)v;}
        private static bool Bool(JToken v){Require(v?.Type==JTokenType.Boolean,"boolean missing");return (bool)v;}
        private static double Number(JToken v){Require(v!=null&&(v.Type==JTokenType.Float||v.Type==JTokenType.Integer),"number missing");var n=(double)v;Require(!double.IsNaN(n)&&!double.IsInfinity(n),"nonfinite number");return n;}
        internal static void AssertComplete(JObject proof,JObject boundary,JObject bridge,bool turnBased,string relationship)
        {
            Require(proof?["identity"] is JObject&&proof["samples"] is JArray,"command observations missing");
            var identity=proof["identity"];var rider=Text(identity["casterId"]);var mount=Text(proof["mountId"]);
            Require(!string.IsNullOrEmpty(rider)&&!string.IsNullOrEmpty(mount)&&rider!=mount,"pair identity missing");
            Require(Int(identity["generationAtInit"])>=0,"command generation missing");
            foreach(var field in new[]{"commandObject","processObject","contextObject"})Require(Int(identity[field])!=0,"command identity incomplete");
            foreach(var field in new[]{"controlIdentity","casterId","targetId","abilityGuid","commandType"})Require(!string.IsNullOrEmpty(Text(identity[field])),"command text identity missing");
            Require(JToken.DeepEquals(identity,bridge?["commandIdentity"]),"bridge command differs");
            var terminals=proof["samples"].Where(s=>Text(s["boundary"])=="terminal").ToArray();Require(terminals.Length==1,"terminal count differs");
            var terminal=terminals[0];Require(JToken.DeepEquals(identity,terminal["identity"])&&Bool(terminal["acted"])&&Bool(terminal["finished"])&&Bool(terminal["processEnded"])&&Text(terminal["result"])=="Success"&&!Bool(terminal["simulatingClick"]),"terminal command/process differs");
            NativePassiveResourceEvidence.AssertComplete(bridge);
            Require(Text(bridge["riderId"])==rider&&Text(bridge["mountId"])==mount&&Bool(bridge["turnBased"])==turnBased&&Bool(boundary?["turnBased"])==turnBased,"pair/mode binding differs");
            foreach(var field in new[]{"frame","gameTicks","allocationSequence"})Require(Int(bridge["before"][field])==Int(terminal[field])&&Int(bridge["after"][field])==Int(boundary[field]),"clock/sequence binding differs");
            Require(terminal["state"]?["ledger"] is JObject&&JToken.DeepEquals(terminal["state"]["ledger"],boundary["state"]?["ledger"])&&Int(terminal["state"]["generation"])==Int(boundary["state"]?["generation"]),"relationship ledger/generation changed");
            Require(Text(terminal["state"]["relationshipState"])==relationship&&Text(boundary["state"]["relationshipState"])==relationship,"relationship boundary differs");
            foreach(var role in new[]{"rider","mount"}){
                var native=terminal["nativeAllocation"]?[role];var final=boundary[role+"Resources"];var before=terminal["state"][role];var after=boundary["state"][role];
                Require(native is JObject&&JToken.DeepEquals(bridge["before"][role],native)&&JToken.DeepEquals(bridge["after"][role],final),"allocation snapshots differ");
                foreach(var pair in new[]{new[]{native,before},new[]{final,after}}){
                    Require(Text(pair[0]["actor"])==Text(pair[1]["actor"])&&Int(pair[0]["grantSequence"])==Int(pair[1]["nativePrepareCount"]),"allocation identity/preparation differs");
                    foreach(var field in new[]{"standard","move","swift","reactions","reactionCooldown","initiativeCooldown","initiativeOrder","reactionsPerRound"})
                        Require(Math.Abs(Number(pair[0][field])-Number(pair[1][field]))<=0.0001,"command allocation resource differs: "+field);
                }
            }
        }
    }
}
