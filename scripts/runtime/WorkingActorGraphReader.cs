using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace KingmakerMountedCombat.Harness
{
    // Inert logical-field diagnostic. This never deserializes native objects or
    // interprets $type/$ref as authority. Caps are diagnostic policy, not engine limits.
    public static class WorkingActorGraphReader
    {
        public const long ArchiveByteLimit = 256L * 1024 * 1024;
        public const int ArchiveMemberLimit = 1024;
        public const int MemberByteLimit = 16 * 1024 * 1024;
        public const int DepthLimit = 64;
        public const int TokenLimit = 250000;
        public const int StringLimit = 1024 * 1024;
        public const int EntityLimit = 4096;
        public const int IdentityLengthLimit = 256;

        public sealed class Observation
        {
            public string status = "FAIL";
            public bool shapeSupported;
            public string comparison;
            public string mainReference;
            public string playerRootKind;
            public string partyRootKind;
            public bool mainReferencePresent;
            public bool entityListPresent;
            public string mainReferenceIdKind;
            public string mainReferenceShape;
            public string entityListShape;
            public int entityCount;
            public int matchingRecordCount;
            public int duplicateEntityIdCount;
            public int[] matchingRecordIndices = new int[0];
            public int playerTokenCount;
            public int partyTokenCount;
            public string playerSha256;
            public string partySha256;
            public string refusal;
            public bool nativeDeserialization;
            public bool nativeLoadProven;
            public bool restorationAuthorized;
            public bool eligibilityAuthorized;
            public bool rawBytesRewritten;
            public string scope = "logical-fields-only; native-converter-wire-shape-not-certified";
        }

        public static void CheckArchiveLength(long length)
        {
            if (length <= 0 || length > ArchiveByteLimit) throw new InvalidDataException("archive-byte-limit");
        }

        public static byte[] ReadMember(Stream stream, long declaredLength)
        {
            if (declaredLength <= 0 || declaredLength > MemberByteLimit) throw new InvalidDataException("selected-member-byte-limit");
            using (var output = new MemoryStream())
            {
                var buffer = new byte[32768];
                int count;
                while ((count = stream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    if (output.Length + count > MemberByteLimit || output.Length + count > declaredLength)
                        throw new InvalidDataException("selected-member-expanded-length");
                    output.Write(buffer, 0, count);
                }
                if (output.Length != declaredLength) throw new InvalidDataException("selected-member-expanded-length");
                return output.ToArray();
            }
        }

        public static Observation Analyze(byte[] player, byte[] party)
        {
            var result = new Observation();
            try
            {
                CheckBytes(player); CheckBytes(party);
                result.playerSha256 = Hash(player); result.partySha256 = Hash(party);
                var playerDoc = Parse(player, out result.playerTokenCount);
                var partyDoc = Parse(party, out result.partyTokenCount);
                result.playerRootKind = Kind(playerDoc.DocumentElement); result.partyRootKind = Kind(partyDoc.DocumentElement);
                // Capture independent bounded structural facts before either logical shape can refuse.
                var main = result.playerRootKind == "object" ? Member(playerDoc.DocumentElement, "m_MainCharacter") : null;
                var entities = result.partyRootKind == "object" ? Member(partyDoc.DocumentElement, "m_EntityData") : null;
                result.mainReferencePresent = main != null;
                result.mainReferenceShape = main == null ? "missing" : Kind(main);
                result.entityListPresent = entities != null;
                result.entityListShape = entities == null ? "missing" : Kind(entities);
                var records = Kind(entities) == "array" ? Children(entities) : new List<XmlNode>();
                result.entityCount = records.Count;
                RequireType(playerDoc.DocumentElement, "object", "unsupported-player-root");
                RequireType(partyDoc.DocumentElement, "object", "unsupported-party-root");
                if (main == null) throw new InvalidDataException("unsupported-main-reference-field");
                if (Kind(main) == "string") result.mainReference = Identity(main);
                else if (Kind(main) == "object")
                {
                    var children = Children(main);
                    if (main.Attributes["__type"] != null || children.Count != 1 || Name(children[0]) != "m_UniqueId")
                        throw new InvalidDataException("unsupported-main-reference-object");
                    result.mainReferenceIdKind = Kind(children[0]);
                    result.mainReference = Identity(children[0]);
                    result.mainReferenceShape = "object:m_UniqueId";
                }
                else throw new InvalidDataException("unsupported-main-reference-kind");
                RequireType(entities, "array", "unsupported-entity-list");
                if (records.Count > EntityLimit) throw new InvalidDataException("entity-count-limit");
                result.entityCount = records.Count;
                var matches = new List<int>();
                var seen = new HashSet<string>(StringComparer.Ordinal);
                for (int index = 0; index < records.Count; index++)
                {
                    var record = records[index];
                    RequireType(record, "object", "unsupported-entity-record");
                    if (Member(record, "$ref") != null) throw new InvalidDataException("unsupported-entity-reference");
                    var id = Identity(Member(record, "UniqueId"));
                    if (!seen.Add(id)) result.duplicateEntityIdCount++;
                    if (id == result.mainReference) matches.Add(index);
                }
                result.entityCount = records.Count;
                result.matchingRecordIndices = matches.ToArray(); result.matchingRecordCount = matches.Count;
                result.comparison = matches.Count == 0 ? "missing" : matches.Count == 1 ? "unique" : "duplicate";
                result.shapeSupported = true; result.status = "PASS";
            }
            catch (InvalidDataException error) { result.refusal = error.Message; }
            catch (XmlException) { result.refusal = "malformed-or-quota-limited-json"; }
            catch (DecoderFallbackException) { result.refusal = "invalid-utf8"; }
            if (!result.shapeSupported) result.mainReference = null; // Unsupported input exposes structure/hashes only.
            return result;
        }

        private static void CheckBytes(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0 || bytes.Length > MemberByteLimit)
                throw new InvalidDataException("selected-member-byte-limit");
        }
        private static string Hash(byte[] bytes)
        {
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }
        private static XmlDictionaryReaderQuotas Quotas()
        {
            return new XmlDictionaryReaderQuotas { MaxDepth = DepthLimit, MaxArrayLength = TokenLimit,
                MaxStringContentLength = StringLimit, MaxBytesPerRead = 4096, MaxNameTableCharCount = StringLimit };
        }
        private static XmlDocument Parse(byte[] bytes, out int tokenCount)
        {
            // Require strict UTF-8. Preserve raw hashes, including an optional UTF-8 BOM.
            var text = new UTF8Encoding(false, true).GetString(bytes);
            if (text.Length > 0 && text[0] == '\uFEFF') text = text.Substring(1);
            RequireSingleDocument(text);
            var normalized = Encoding.UTF8.GetBytes(text);
            tokenCount = 0;
            var containers = new Stack<string>();
            using (var reader = JsonReaderWriterFactory.CreateJsonReader(normalized, Quotas()))
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        // One value/start token, plus its property name when inside an object.
                        tokenCount += 1 + (containers.Count > 0 && containers.Peek() == "object" ? 1 : 0);
                        var kind = reader.GetAttribute("type");
                        // Framework JSON maps a leading __type member to an inert XML attribute.
                        var typeMetadata = reader.GetAttribute("__type");
                        if (typeMetadata != null)
                        {
                            tokenCount += 2;
                            if (typeMetadata.Length > StringLimit) throw new InvalidDataException("json-string-limit");
                        }
                        if (!reader.IsEmptyElement) containers.Push(kind ?? "");
                    }
                    else if (reader.NodeType == XmlNodeType.EndElement)
                    {
                        var kind = containers.Pop();
                        if (kind == "object" || kind == "array") tokenCount++;
                    }
                    if (reader.Value != null && reader.Value.Length > StringLimit) throw new InvalidDataException("json-string-limit");
                    if (tokenCount > TokenLimit) throw new InvalidDataException("json-token-limit");
                }
            }
            var document = new XmlDocument { XmlResolver = null };
            using (var reader = JsonReaderWriterFactory.CreateJsonReader(normalized, Quotas())) document.Load(reader);
            foreach (XmlNode node in document.SelectNodes("//*[@type='object']"))
            {
                var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (node.Attributes["__type"] != null) names.Add("__type");
                foreach (var child in Children(node))
                    if (!names.Add(Name(child))) throw new InvalidDataException("duplicate-or-case-ambiguous-json-member");
            }
            return document;
        }
        // JsonReaderWriterFactory may stop after the first value. Delimit that value
        // lexically before parsing and require the remaining input to be JSON whitespace.
        // Interior grammar is still validated by the framework reader, with no type activation.
        private static void RequireSingleDocument(string text)
        {
            int index = 0;
            while (index < text.Length && White(text[index])) index++;
            if (index == text.Length) throw new InvalidDataException("malformed-json-document");
            char first = text[index];
            if (first == '{' || first == '[')
            {
                var closes = new Stack<char>();
                do
                {
                    char ch = text[index];
                    if (ch == '"') { index = StringEnd(text, index); continue; }
                    if (ch == '{' || ch == '[')
                    {
                        closes.Push(ch == '{' ? '}' : ']');
                        if (closes.Count > DepthLimit) throw new InvalidDataException("malformed-or-quota-limited-json");
                    }
                    else if (ch == '}' || ch == ']')
                    {
                        if (closes.Count == 0 || closes.Pop() != ch) throw new InvalidDataException("malformed-json-document");
                    }
                    index++;
                    if (closes.Count == 0) break;
                } while (index < text.Length);
                if (closes.Count != 0) throw new InvalidDataException("malformed-json-document");
            }
            else if (first == '"') index = StringEnd(text, index);
            else if (first == 't' || first == 'f' || first == 'n')
            {
                string literal = first == 't' ? "true" : first == 'f' ? "false" : "null";
                if (text.Length - index < literal.Length || String.CompareOrdinal(text, index, literal, 0, literal.Length) != 0)
                    throw new InvalidDataException("malformed-json-document");
                index += literal.Length;
            }
            else
            {
                if (text[index] == '-') index++;
                if (index == text.Length || text[index] < '0' || text[index] > '9') throw new InvalidDataException("malformed-json-document");
                if (text[index] == '0') index++;
                else while (index < text.Length && text[index] >= '0' && text[index] <= '9') index++;
                if (index < text.Length && text[index] == '.') { index++; index = DigitsEnd(text, index); }
                if (index < text.Length && (text[index] == 'e' || text[index] == 'E'))
                {
                    index++; if (index < text.Length && (text[index] == '+' || text[index] == '-')) index++;
                    index = DigitsEnd(text, index);
                }
            }
            while (index < text.Length && White(text[index])) index++;
            if (index != text.Length) throw new InvalidDataException("trailing-json-content");
        }
        private static bool White(char value) { return value == ' ' || value == '\t' || value == '\r' || value == '\n'; }
        private static int DigitsEnd(string text, int index)
        {
            int start = index;
            while (index < text.Length && text[index] >= '0' && text[index] <= '9') index++;
            if (index == start) throw new InvalidDataException("malformed-json-document");
            return index;
        }
        private static int StringEnd(string text, int index)
        {
            for (index++; index < text.Length; index++)
            {
                char value = text[index];
                if (value == '"') return index + 1;
                if (value < 32) throw new InvalidDataException("malformed-json-document");
                if (value != '\\') continue;
                if (++index == text.Length) break;
                char escape = text[index];
                if (escape == 'u')
                {
                    for (int count = 0; count < 4; count++)
                    {
                        if (++index == text.Length) throw new InvalidDataException("malformed-json-document");
                        char hex = text[index];
                        if (!(hex >= '0' && hex <= '9') && !(hex >= 'a' && hex <= 'f') && !(hex >= 'A' && hex <= 'F'))
                            throw new InvalidDataException("malformed-json-document");
                    }
                }
                else if (escape != '"' && escape != '\\' && escape != '/' && escape != 'b' && escape != 'f' && escape != 'n' && escape != 'r' && escape != 't')
                    throw new InvalidDataException("malformed-json-document");
            }
            throw new InvalidDataException("malformed-json-document");
        }
        private static string Kind(XmlNode node) { return node == null || node.Attributes["type"] == null ? null : node.Attributes["type"].Value; }
        private static string Name(XmlNode node) { return node.LocalName == "item" && node.Attributes["item"] != null ? node.Attributes["item"].Value : node.LocalName; }
        private static List<XmlNode> Children(XmlNode node)
        {
            var result = new List<XmlNode>();
            foreach (XmlNode child in node.ChildNodes) if (child.NodeType == XmlNodeType.Element) result.Add(child);
            return result;
        }
        private static XmlNode Member(XmlNode node, string name)
        {
            foreach (var child in Children(node)) if (Name(child) == name) return child;
            return null;
        }
        private static void RequireType(XmlNode node, string type, string refusal)
        {
            if (Kind(node) != type) throw new InvalidDataException(refusal);
        }
        private static string Identity(XmlNode node)
        {
            RequireType(node, "string", "unsupported-identity-scalar");
            var value = node.InnerText;
            if (value.Length == 0 || value.Length > IdentityLengthLimit || String.IsNullOrWhiteSpace(value))
                throw new InvalidDataException("unsupported-identity-length");
            foreach (char ch in value) if (Char.IsControl(ch)) throw new InvalidDataException("unsupported-identity-control");
            return value;
        }
    }
}
