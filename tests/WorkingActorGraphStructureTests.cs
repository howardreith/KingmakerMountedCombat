using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using KingmakerMountedCombat.Harness;

internal static class WorkingActorGraphStructureTests
{
    private const string Player = "{\"m_MainCharacter\":{\"m_UniqueId\":\"main-id\"}}";
    private static int passed, failed;
    private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);
    private static WorkingActorGraphReader.Observation Graph(string party, string player = Player)
    { return WorkingActorGraphReader.Analyze(Utf8.GetBytes(player), Utf8.GetBytes(party)); }
    private static string Party(params string[] records) { return "{\"m_EntityData\":[" + String.Join(",", records) + "]}"; }
    private static object Field(object value, string name)
    {
        if (value == null) throw new Exception("Missing structural object for " + name);
        var field = value.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance);
        if (field == null) throw new Exception("Missing structural field: " + name);
        return field.GetValue(value);
    }
    private static object[] ArrayField(object value, string name)
    {
        var list = new List<object>();
        foreach (object item in (IEnumerable)Field(value, name)) list.Add(item);
        return list.ToArray();
    }
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Check(string name, Action action)
    {
        try { action(); passed++; Console.WriteLine("PASS " + name); }
        catch (Exception error) { failed++; Console.WriteLine("FAIL " + name + ": " + error.Message); }
    }
    private static string Json(object value)
    {
        using (var stream = new MemoryStream())
        {
            new DataContractJsonSerializer(value.GetType()).WriteObject(stream, value);
            return Utf8.GetString(stream.ToArray());
        }
    }
    private static void Refused(WorkingActorGraphReader.Observation value, string reason)
    {
        Require(value.status == "FAIL" && !value.shapeSupported && value.refusal == reason, "Refusal changed");
        Require(value.mainReference == null && value.comparison == null && !value.nativeDeserialization && !value.nativeLoadProven &&
            !value.restorationAuthorized && !value.eligibilityAuthorized && !value.rawBytesRewritten, "Refusal exposed identity or authority");
    }
    private static object Member(object record, string name)
    {
        foreach (var member in ArrayField(record, "members")) if ((string)Field(member, "name") == name) return member;
        throw new Exception("Missing reported key: " + name);
    }
    private static string ManyMembers(int count, bool reference)
    {
        var fields = new List<string>();
        for (int index = 0; index < count; index++) fields.Add("\"key" + index + "\":\"HIDDEN-SCALAR-VALUE\"");
        if (reference) fields.Add("\"$ref\":\"HIDDEN-REFERENCE-VALUE\"");
        else fields.Add("\"UniqueId\":\"main-id\"");
        return "{" + String.Join(",", fields.ToArray()) + "}";
    }
    public static int Main()
    {
        Check("historical unique comparison unchanged", () => { var r = Graph(Party("{\"UniqueId\":\"main-id\"}")); Require(r.status == "PASS" && r.comparison == "unique" && r.matchingRecordCount == 1 && !r.nativeLoadProven, "Unique comparison changed"); });
        Check("historical missing comparison unchanged", () => { var r = Graph(Party("{\"UniqueId\":\"other\"}")); Require(r.status == "PASS" && r.comparison == "missing", "Missing comparison changed"); });
        Check("historical duplicate comparison unchanged", () => { var r = Graph(Party("{\"UniqueId\":\"main-id\"}", "{\"UniqueId\":\"main-id\"}")); Require(r.status == "PASS" && r.comparison == "duplicate" && r.duplicateEntityIdCount == 1, "Duplicate comparison changed"); });
        Check("reference remains unsupported without value disclosure", () => { var r = Graph(Party("{\"$ref\":\"HIDDEN-REFERENCE-VALUE\",\"UniqueId\":\"HIDDEN-ID-VALUE\"}")); Refused(r, "unsupported-entity-reference"); Require(!Json(r).Contains("HIDDEN-"), "Scalar value leaked"); });
        Check("exact refused index and member kinds observed", () => {
            var r = Graph(Party("{\"UniqueId\":\"other\"}", "{\"$ref\":\"HIDDEN-REFERENCE-VALUE\",\"Other\":{\"nested\":\"HIDDEN-NESTED-VALUE\"}}"));
            Refused(r, "unsupported-entity-reference"); Require((int)Field(r, "refusedEntityIndex") == 1, "Refused index differs");
            var shape = Field(r, "refusedEntityShape"); Require((string)Field(shape, "kind") == "object" && (int)Field(shape, "memberCount") == 2, "Record shape differs");
            Require((string)Field(Member(shape, "$ref"), "kind") == "string" && (string)Field(Member(shape, "Other"), "kind") == "object", "Key kinds differ");
            Require(!Json(r).Contains("HIDDEN-") && !Json(shape).Contains("nested"), "Scalar or nested members leaked");
        });
        Check("all three bounded record shapes survive first refusal", () => {
            var r = Graph(Party("{\"$ref\":\"secret-one\"}", "{\"UniqueId\":\"secret-two\"}", "{\"$ref\":12}"));
            var shapes = ArrayField(r, "entityRecordShapes"); Require(shapes.Length == 3 && (int)Field(shapes[2], "index") == 2 && (string)Field(shapes[2], "referenceKind") == "number", "Later structural facts absent");
            Require(!Json(r).Contains("secret-"), "Scalar identity leaked");
        });
        Check("refusing record beyond prefix remains bounded and visible", () => {
            var records = new List<string>(); for (int i = 0; i < 20; i++) records.Add("{\"UniqueId\":\"id" + i + "\"}"); records.Add("{\"$ref\":false}");
            var r = Graph(Party(records.ToArray())); Refused(r, "unsupported-entity-reference");
            Require(ArrayField(r, "entityRecordShapes").Length == 16 && (bool)Field(r, "entityRecordShapesTruncated"), "Prefix cap lost");
            Require((int)Field(r, "refusedEntityIndex") == 20 && (int)Field(Field(r, "refusedEntityShape"), "index") == 20 && (string)Field(Field(r, "refusedEntityShape"), "referenceKind") == "boolean", "Out-of-prefix refusal lost");
        });
        Check("record cap boundary does not falsely report truncation", () => {
            var records = new List<string>(); for (int i = 0; i < 16; i++) records.Add("{\"UniqueId\":\"id" + i + "\"}");
            var r = Graph(Party(records.ToArray())); Require(ArrayField(r, "entityRecordShapes").Length == 16 && !(bool)Field(r, "entityRecordShapesTruncated") && Field(r, "refusedEntityIndex") == null && Field(r, "refusedEntityShape") == null, "Exact record boundary differs");
        });
        Check("member cap boundary is exact", () => { var r = Graph(Party(ManyMembers(15, false))); var shape = ArrayField(r, "entityRecordShapes")[0]; Require((int)Field(shape, "memberCount") == 16 && ArrayField(shape, "members").Length == 16 && !(bool)Field(shape, "membersTruncated"), "Exact member boundary differs"); });
        Check("member cap does not hide the refused reference kind", () => {
            var r = Graph(Party(ManyMembers(20, true))); Refused(r, "unsupported-entity-reference"); var shape = Field(r, "refusedEntityShape");
            Require((int)Field(shape, "memberCount") == 21 && ArrayField(shape, "members").Length == 16 && (bool)Field(shape, "membersTruncated") && (string)Field(shape, "referenceKind") == "string", "Member cap or exact reference kind differs");
            Require(!Json(r).Contains("HIDDEN-"), "Truncated observation leaked a scalar");
        });
        Check("key cap flags truncation without splitting surrogate pair", () => {
            string key = new string('k', 127) + "\uD83D\uDC0E" + "tail";
            var r = Graph(Party("{\"$ref\":\"secret\",\"" + key + "\":\"HIDDEN-VALUE\"}")); var member = ArrayField(Field(r, "refusedEntityShape"), "members")[1];
            Require(((string)Field(member, "name")).Length == 127 && (bool)Field(member, "nameTruncated") && !Json(r).Contains("HIDDEN-"), "Capped UTF16 key differs");
        });
        Check("exact key cap is not marked truncated", () => {
            string key = new string('k', 128); var r = Graph(Party("{\"$ref\":null,\"" + key + "\":0}")); var member = Member(Field(r, "refusedEntityShape"), key);
            Require(!(bool)Field(member, "nameTruncated") && (string)Field(Field(r, "refusedEntityShape"), "referenceKind") == "null", "Exact key or null reference kind differs");
        });
        foreach (var row in new[] { new[] { "\"HIDDEN-STRING\"", "string" }, new[] { "12345", "number" }, new[] { "true", "boolean" }, new[] { "null", "null" }, new[] { "[\"HIDDEN-ARRAY\"]", "array" } })
        {
            var current = row;
            Check("nonobject " + row[1] + " refuses and records kind only", () => { var r = Graph(Party(current[0])); Refused(r, "unsupported-entity-record"); var shape = Field(r, "refusedEntityShape"); Require((string)Field(shape, "kind") == current[1] && (int)Field(shape, "memberCount") == 0 && ArrayField(shape, "members").Length == 0 && !Json(r).Contains("HIDDEN-"), "Nonobject shape or value differs"); });
        }
        Check("missing identity reports absence without relaxing refusal", () => { var r = Graph(Party("{}")); Refused(r, "unsupported-identity-scalar"); var shape = Field(r, "refusedEntityShape"); Require(Field(shape, "identityKind") == null && (int)Field(shape, "memberCount") == 0, "Missing identity shape differs"); });
        Check("array identity reports kind and remains unsupported", () => { var r = Graph(Party("{\"UniqueId\":[\"HIDDEN-ID\"]}")); Refused(r, "unsupported-identity-scalar"); Require((string)Field(Field(r, "refusedEntityShape"), "identityKind") == "array" && !Json(r).Contains("HIDDEN-ID"), "Array identity leaked or changed"); });
        Check("framework type metadata reports key but never its value", () => {
            var r = Graph(Party("{\"__type\":\"HIDDEN-TYPE-VALUE\",\"$ref\":\"HIDDEN-REF\"}")); Refused(r, "unsupported-entity-reference"); var shape = Field(r, "refusedEntityShape");
            Require((int)Field(shape, "memberCount") == 2 && (string)Field(Member(shape, "__type"), "kind") == "string" && !Json(r).Contains("HIDDEN-"), "Framework metadata summary differs");
        });
        Check("native type string remains inert", () => { var r = Graph(Party("{\"$type\":\"HIDDEN-NATIVE-TYPE\",\"UniqueId\":\"main-id\"}")); Require(r.status == "PASS" && !r.nativeDeserialization && (string)Field(Member(ArrayField(r, "entityRecordShapes")[0], "$type"), "kind") == "string" && !Json(Field(r, "entityRecordShapes")).Contains("HIDDEN-"), "Native type observation differs"); });
        Check("unsupported main retains independent record structure", () => { var r = Graph(Party("{\"$ref\":\"HIDDEN-REF\"}"), "{\"m_MainCharacter\":{\"$ref\":\"HIDDEN-MAIN\"}}"); Refused(r, "unsupported-main-reference-object"); Require(ArrayField(r, "entityRecordShapes").Length == 1 && Field(r, "refusedEntityIndex") == null && !Json(r).Contains("HIDDEN-"), "Uninspected identity treated as evaluated"); });
        Check("malformed trailing input still refuses before structural capture", () => { var r = Graph(Party("{\"UniqueId\":\"main-id\"}") + " {}"); Refused(r, "trailing-json-content"); Require(ArrayField(r, "entityRecordShapes").Length == 0, "Malformed document supplied observations"); });
        Check("duplicate key still refuses before structural capture", () => { var r = Graph(Party("{\"$ref\":\"one\",\"$ref\":\"two\"}")); Refused(r, "duplicate-or-case-ambiguous-json-member"); Require(ArrayField(r, "entityRecordShapes").Length == 0, "Ambiguous keys supplied observations"); });
        Check("invalid UTF8 still refuses", () => { var r = WorkingActorGraphReader.Analyze(new byte[] { 255 }, Utf8.GetBytes(Party("{}"))); Refused(r, "invalid-utf8"); Require(ArrayField(r, "entityRecordShapes").Length == 0, "Invalid bytes supplied observations"); });
        Check("source bytes remain byte-identical", () => { var player = Utf8.GetBytes(Player); var party = Utf8.GetBytes(Party("{\"$ref\":\"unresolved\"}")); string beforePlayer = Convert.ToBase64String(player), beforeParty = Convert.ToBase64String(party); var r = WorkingActorGraphReader.Analyze(player, party); Refused(r, "unsupported-entity-reference"); Require(Convert.ToBase64String(player) == beforePlayer && Convert.ToBase64String(party) == beforeParty, "Input bytes changed"); });
        Check("observation declares its fixed structural caps and limits", () => { var r = Graph(Party("{}")); Require((int)Field(r, "structuralRecordLimit") == 16 && (int)Field(r, "structuralMemberLimit") == 16 && (int)Field(r, "structuralKeyLengthLimit") == 128 && ((string)Field(r, "structuralScope")).Contains("no-reference-resolution"), "Structural limits missing"); });
        Console.WriteLine("WORKING ACTOR GRAPH STRUCTURE: " + passed + " PASS / " + failed + " FAIL");
        return failed == 0 ? 0 : 1;
    }
}
