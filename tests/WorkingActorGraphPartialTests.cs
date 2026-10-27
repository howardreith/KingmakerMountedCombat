using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using KingmakerMountedCombat.Harness;

internal static class WorkingActorGraphPartialTests
{
    private const string Player = "{\"m_MainCharacter\":{\"m_UniqueId\":\"main-id\"}}";
    private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);
    private static int passed, failed;
    private static WorkingActorGraphReader.Observation Graph(string party, string player = Player)
    { return WorkingActorGraphReader.Analyze(Utf8.GetBytes(player), Utf8.GetBytes(party)); }
    private static string Party(params string[] records) { return "{\"m_EntityData\":[" + String.Join(",", records) + "]}"; }
    private static object Field(object value, string name)
    {
        if (value == null) throw new Exception("Missing object for " + name);
        var field = value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public);
        if (field == null) throw new Exception("Missing observation field: " + name);
        return field.GetValue(value);
    }
    private static object[] Items(object value, string name)
    { var result = new List<object>(); foreach (object item in (IEnumerable)Field(value, name)) result.Add(item); return result.ToArray(); }
    private static int Number(object value, string name) { return (int)Field(value, name); }
    private static bool Flag(object value, string name) { return (bool)Field(value, name); }
    private static string Text(object value, string name) { return (string)Field(value, name); }
    private static void Need(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Check(string name, Action action)
    { try { action(); passed++; Console.WriteLine("PASS " + name); } catch (Exception e) { failed++; Console.WriteLine("FAIL " + name + ": " + e.Message); } }
    private static string Json(object value)
    { using (var stream = new MemoryStream()) { new DataContractJsonSerializer(value.GetType()).WriteObject(stream, value); return Utf8.GetString(stream.ToArray()); } }
    private static void Refused(WorkingActorGraphReader.Observation r, string reason = "unsupported-entity-reference")
    {
        Need(r.status == "FAIL" && !r.shapeSupported && r.refusal == reason && r.mainReference == null && r.comparison == null, "Existing refusal changed");
        Need(r.matchingRecordCount == 0 && r.matchingRecordIndices.Length == 0, "Incomplete scan changed completed comparison fields");
        Need(!r.nativeDeserialization && !r.nativeLoadProven && !r.restorationAuthorized && !r.eligibilityAuthorized && !r.rawBytesRewritten, "Authority changed");
    }
    private static object Lexical(WorkingActorGraphReader.Observation r) { return Field(r, "lexicalAliases"); }
    private static object FirstRef(WorkingActorGraphReader.Observation r) { return Items(Lexical(r), "entityReferences")[0]; }
    private static object Partial(WorkingActorGraphReader.Observation r)
    {
        var partial = Field(r, "partialInline");
        Need(!Flag(partial, "comparisonComplete") && Text(partial, "scope").Contains("incomplete"), "Partial observation implies completion");
        return partial;
    }
    public static int Main()
    {
        Check("old unique logical comparison", () => { var r = Graph(Party("{\"UniqueId\":\"main-id\"}")); Need(r.status == "PASS" && r.comparison == "unique" && r.matchingRecordCount == 1 && !r.nativeLoadProven, "Unique changed"); });
        Check("old missing logical comparison", () => { var r = Graph(Party("{\"UniqueId\":\"other\"}")); Need(r.status == "PASS" && r.comparison == "missing", "Missing changed"); });
        Check("old duplicate logical comparison", () => { var r = Graph(Party("{\"UniqueId\":\"main-id\"}", "{\"UniqueId\":\"main-id\"}")); Need(r.status == "PASS" && r.comparison == "duplicate" && r.duplicateEntityIdCount == 1, "Duplicate changed"); });
        Check("old unsupported reference remains FAIL", () => Refused(Graph(Party("{\"$ref\":\"HIDDEN-ALIAS\"}"))));
        Check("input bytes unchanged", () => {
            byte[] player = Utf8.GetBytes(Player), party = Utf8.GetBytes(Party("{\"UniqueId\":\"main-id\"}", "{\"$ref\":\"HIDDEN-ALIAS\"}"));
            string p = Convert.ToBase64String(player), q = Convert.ToBase64String(party);
            Refused(WorkingActorGraphReader.Analyze(player, party)); Need(p == Convert.ToBase64String(player) && q == Convert.ToBase64String(party), "Source bytes changed");
        });
        Check("partial main equality survives later refusal", () => {
            var r = Graph(Party("{\"UniqueId\":\"main-id\"}", "{\"UniqueId\":\"other\"}", "{\"$ref\":\"HIDDEN-ALIAS\"}")); Refused(r);
            var p = Partial(r); Need(Flag(p, "available") && Number(p, "validatedPrefixCount") == 2 && Number(p, "matchingPrefixRecordCount") == 1 && Items(p, "matchingPrefixRecordIndices").Length == 1 && (int)Items(p, "matchingPrefixRecordIndices")[0] == 0, "Partial literal facts missing");
        });
        Check("partial zero is explicitly incomplete", () => {
            var r = Graph(Party("{\"UniqueId\":\"other\"}", "{\"$ref\":\"HIDDEN-ALIAS\"}")); Refused(r);
            var p = Partial(r); Need(Number(p, "validatedPrefixCount") == 1 && Number(p, "matchingPrefixRecordCount") == 0, "Partial zero differs");
        });
        Check("partial stops before reference and later inline record", () => {
            var r = Graph(Party("{\"UniqueId\":\"other\"}", "{\"$ref\":\"HIDDEN-ALIAS\"}", "{\"UniqueId\":\"main-id\"}")); Refused(r);
            Need(Number(Partial(r), "validatedPrefixCount") == 1 && Number(Partial(r), "matchingPrefixRecordCount") == 0, "Unvalidated suffix was counted");
        });
        Check("partial stops at malformed inline identity", () => {
            var r = Graph(Party("{\"UniqueId\":\"main-id\"}", "{\"UniqueId\":[\"HIDDEN-ID\"]}")); Refused(r, "unsupported-identity-scalar");
            Need(Number(Partial(r), "validatedPrefixCount") == 1 && Number(Partial(r), "matchingPrefixRecordCount") == 1, "Validated prefix was lost");
        });
        Check("unsupported main provides no partial comparison", () => {
            var r = Graph(Party("{\"UniqueId\":\"main-id\"}"), "{\"m_MainCharacter\":{\"$ref\":\"HIDDEN-MAIN\"}}"); Refused(r, "unsupported-main-reference-object");
            Need(!Flag(Partial(r), "available") && Number(Partial(r), "validatedPrefixCount") == 0, "Unsupported main was compared");
        });
        Check("partial index cap retains total without values", () => {
            var records = new List<string>(); for (int i = 0; i < 20; i++) records.Add("{\"UniqueId\":\"main-id\"}"); records.Add("{\"$ref\":\"HIDDEN-ALIAS\"}");
            var r = Graph(Party(records.ToArray())); Refused(r); var p = Partial(r);
            Need(Number(p, "matchingPrefixRecordCount") == 20 && Items(p, "matchingPrefixRecordIndices").Length == 16 && Flag(p, "matchingIndicesTruncated"), "Partial index bound differs");
            Need(!Json(r).Contains("HIDDEN-") && !Json(p).Contains("main-id"), "Partial observation leaked identity");
        });
        Check("complete old comparison remains separate", () => {
            var r = Graph(Party("{\"UniqueId\":\"main-id\"}")); Need(r.status == "PASS" && r.matchingRecordCount == 1, "Old completion changed");
            Need(Number(Partial(r), "matchingPrefixRecordCount") == 1, "Separate literal observation missing");
        });
        Check("backward lexical match reports target keys only", () => {
            var r = Graph(Party("{\"$id\":\"HIDDEN-ALIAS\",\"UniqueId\":\"main-id\",\"secret\":{\"nested\":\"HIDDEN-VALUE\"}}", "{\"$ref\":\"HIDDEN-ALIAS\"}")); Refused(r);
            var reference = FirstRef(r); Need(Number(reference, "definitionCount") == 1 && Text(reference, "uncertainty") == "single-backward-lexical-match-not-native", "Backward lexical classification differs");
            var target = Items(reference, "targets")[0]; Need(Text(target, "relativePosition") == "before-reference" && Number(target, "memberCount") == 3, "Target shape differs");
            Need(!Json(Lexical(r)).Contains("HIDDEN-") && !Json(Lexical(r)).Contains("main-id") && !Json(Lexical(r)).Contains("nested"), "Target scalar or nested keys leaked");
        });
        Check("forward lexical match stays uncertain", () => {
            var r = Graph(Party("{\"$ref\":\"HIDDEN-ALIAS\"}", "{\"$id\":\"HIDDEN-ALIAS\",\"UniqueId\":\"main-id\"}")); Refused(r);
            var reference = FirstRef(r); Need(Number(reference, "definitionCount") == 1 && Text(reference, "uncertainty") == "forward-definition-not-native" && Text(Items(reference, "targets")[0], "relativePosition") == "after-reference", "Forward match promoted");
        });
        Check("duplicate alias definitions remain ambiguous", () => {
            var r = Graph(Party("{\"$id\":\"HIDDEN-ALIAS\",\"UniqueId\":\"one\"}", "{\"$id\":\"HIDDEN-ALIAS\",\"UniqueId\":\"two\"}", "{\"$ref\":\"HIDDEN-ALIAS\"}")); Refused(r);
            Need(Number(FirstRef(r), "definitionCount") == 2 && Text(FirstRef(r), "uncertainty") == "multiple-definitions-not-native" && Number(Field(Lexical(r), "party"), "duplicateIdOccurrenceCount") == 1, "Duplicate alias uncertainty differs");
        });
        Check("missing alias definition stays uncertain", () => {
            var r = Graph(Party("{\"$ref\":\"HIDDEN-MISSING\"}")); Refused(r);
            Need(Number(FirstRef(r), "definitionCount") == 0 && Text(FirstRef(r), "uncertainty") == "missing-lexical-definition-not-native", "Missing alias promoted");
        });
        Check("player definition cannot satisfy party reference", () => {
            var r = Graph(Party("{\"$ref\":\"HIDDEN-ALIAS\"}"), "{\"$id\":\"HIDDEN-ALIAS\",\"m_MainCharacter\":{\"m_UniqueId\":\"main-id\"}}"); Refused(r);
            Need(Number(Field(Lexical(r), "player"), "idMemberCount") == 1 && Number(Field(Lexical(r), "party"), "idMemberCount") == 0 && Number(FirstRef(r), "definitionCount") == 0, "Cross-document match was used");
        });
        Check("lexical alias equality is exact case-sensitive", () => {
            var r = Graph(Party("{\"$id\":\"Alias\",\"UniqueId\":\"main-id\"}", "{\"$ref\":\"alias\"}")); Refused(r);
            Need(Number(FirstRef(r), "definitionCount") == 0, "Aliases were normalized");
        });
        foreach (string value in new[] { "null", "true", "42", "[]", "{}" })
        {
            string current = value;
            Check("nonstring alias remains uncertain " + current, () => {
                var r = Graph(Party("{\"$ref\":" + current + "}")); Refused(r);
                Need(!Flag(FirstRef(r), "aliasWellFormed") && Text(FirstRef(r), "uncertainty") == "malformed-alias-not-native" && Number(Field(Lexical(r), "party"), "invalidReferenceCount") == 1, "Nonstring alias was interpreted");
            });
        }
        foreach (string value in new[] { "\"\"", "\" \"", "\"\\u0001\"", "\"" + new string('x', 257) + "\"" })
        {
            string current = value;
            Check("bounded alias refusal " + value.Length, () => {
                var r = Graph(Party("{\"$ref\":" + current + "}")); Refused(r);
                Need(!Flag(FirstRef(r), "aliasWellFormed") && Items(FirstRef(r), "targets").Length == 0, "Invalid alias was matched");
            });
        }
        Check("target count and target-key output are capped", () => {
            var records = new List<string>(); for (int i = 0; i < 20; i++) records.Add("{\"$id\":\"HIDDEN-ALIAS\",\"UniqueId\":\"id" + i + "\"}"); records.Add("{\"$ref\":\"HIDDEN-ALIAS\"}");
            var r = Graph(Party(records.ToArray())); Refused(r); var reference = FirstRef(r);
            Need(Number(reference, "entityIndex") == 20 && Number(reference, "definitionCount") == 20 && Items(reference, "targets").Length == 16 && Flag(reference, "targetsTruncated"), "Refused extra record or target cap differs");
        });
        Check("entity reference observation window is capped", () => {
            var records = new List<string>(); for (int i = 0; i < 20; i++) records.Add("{\"$ref\":\"HIDDEN-ALIAS\"}");
            var r = Graph(Party(records.ToArray())); Refused(r);
            Need(Items(Lexical(r), "entityReferences").Length == 16 && Flag(Lexical(r), "entityRecordWindowTruncated"), "Reference observation cap differs");
        });
        Check("full bounded census marks retained alias cap uncertainty", () => {
            var defs = new List<string>(); for (int i = 0; i < 4097; i++) defs.Add("{\"$id\":\"alias" + i + "\"}");
            var r = Graph("{\"defs\":[" + String.Join(",", defs.ToArray()) + "],\"m_EntityData\":[{\"$ref\":\"alias4096\"}]}"); Refused(r);
            var party = Field(Lexical(r), "party"); Need(Number(party, "idMemberCount") == 4097 && Number(party, "retainedAliasCount") == 4096 && Flag(party, "duplicateCensusTruncated") && Number(FirstRef(r), "definitionCount") == 1 && Flag(FirstRef(r), "censusTruncated") && Text(FirstRef(r), "uncertainty") == "census-truncated-not-native", "Census cap concealed uncertainty or prevented bounded targeted observation");
        });
        Check("nested same-document target is still only lexical", () => {
            var r = Graph("{\"pool\":{\"$id\":\"HIDDEN-ALIAS\",\"Nested\":{\"secret\":\"HIDDEN-VALUE\"}},\"m_EntityData\":[{\"$ref\":\"HIDDEN-ALIAS\"}]}"); Refused(r);
            Need(Number(FirstRef(r), "definitionCount") == 1 && Number(Items(FirstRef(r), "targets")[0], "memberCount") == 2 && !Json(Lexical(r)).Contains("HIDDEN-") && !Json(Lexical(r)).Contains("secret"), "Nested target was resolved or exposed");
        });
        Check("target keys keep existing caps and surrogate boundary", () => {
            string key = new string('k', 127) + "\uD83D\uDC0E" + "tail"; var fields = new List<string> { "\"$id\":\"HIDDEN-ALIAS\"", "\"" + key + "\":\"HIDDEN-VALUE\"" };
            for (int i = 0; i < 20; i++) fields.Add("\"key" + i + "\":0");
            var r = Graph("{\"pool\":{" + String.Join(",", fields.ToArray()) + "},\"m_EntityData\":[{\"$ref\":\"HIDDEN-ALIAS\"}]}"); Refused(r);
            var target = Items(FirstRef(r), "targets")[0]; var members = Items(target, "members");
            Need(Number(target, "memberCount") == 22 && members.Length == 16 && Flag(target, "membersTruncated") && Text(members[1], "name").Length == 127 && Flag(members[1], "nameTruncated"), "Target key bound differs");
        });
        Check("malformed id kind is counted without matching", () => {
            var r = Graph("{\"pool\":{\"$id\":42},\"m_EntityData\":[{\"$ref\":\"42\"}]}"); Refused(r);
            Need(Number(Field(Lexical(r), "party"), "invalidIdCount") == 1 && Number(FirstRef(r), "definitionCount") == 0, "Invalid definition coerced");
        });
        Check("duplicate property still refuses before new observations", () => {
            var r = Graph(Party("{\"$id\":\"one\",\"$id\":\"two\",\"$ref\":\"one\"}")); Refused(r, "duplicate-or-case-ambiguous-json-member");
            Need(!Flag(Lexical(r), "available") && !Flag(Partial(r), "available"), "Malformed input supplied observations");
        });
        Check("trailing document still refuses before new observations", () => {
            var r = Graph(Party("{\"$ref\":\"HIDDEN-ALIAS\"}") + "{}"); Refused(r, "trailing-json-content");
            Need(!Flag(Lexical(r), "available") && !Flag(Partial(r), "available"), "Trailing input supplied observations");
        });
        Check("observation explicitly denies native reference authority", () => {
            var r = Graph(Party("{\"$ref\":\"HIDDEN-ALIAS\"}")); Refused(r); var lexical = Lexical(r);
            Need(Text(lexical, "scope").Contains("no-reference-resolution") && !Flag(lexical, "resolutionPerformed") && Number(lexical, "recordLimit") == 16 && Number(lexical, "targetLimit") == 16 && Number(lexical, "aliasLengthLimit") == 256, "Limits or denial missing");
        });
        Console.WriteLine("WORKING ACTOR GRAPH PARTIAL: " + passed + " PASS / " + failed + " FAIL");
        return failed == 0 ? 0 : 1;
    }
}
