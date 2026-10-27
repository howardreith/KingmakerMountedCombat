using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityModManagerNet;

namespace KingmakerMountedCombat.Diagnostics
{
    // Read-only live loader observation. Disk staging receipts alone do not prove
    // which UMM entries or assemblies actually reached the selected native run.
    internal static class RuntimeLoaderInventory
    {
        internal const string EvidenceFileName = "runtime-loader-inventory.json";
        internal const string EvidenceKind = "runtime-loader-inventory";
        internal static void EnsureWritten(RuntimeRequest request)
        {
            if (File.Exists(Path.Combine(request.EvidenceRoot, EvidenceFileName))) return;
            var errors = new List<string>(); var mods = new JArray(); var assemblies = new JArray();
            try
            {
                var entries = ReadMember(typeof(UnityModManager), null, "modEntries") as IEnumerable;
                if (entries == null) throw new InvalidOperationException("Exact UMM modEntries collection is unavailable.");
                foreach (var entry in entries)
                {
                    if (entry == null) throw new InvalidOperationException("UMM modEntries contains null.");
                    var info = ReadMember(entry.GetType(), entry, "Info");
                    var id = info == null ? null : ReadMember(info.GetType(), info, "Id") as string;
                    var active = ReadMember(entry.GetType(), entry, "Active");
                    var enabled = ReadMember(entry.GetType(), entry, "Enabled");
                    if (string.IsNullOrEmpty(id) || !(active is bool) || !(enabled is bool))
                        throw new InvalidOperationException("UMM entry identity/Active/Enabled contract is incomplete.");
                    mods.Add(new JObject { ["id"] = id, ["active"] = (bool)active, ["enabled"] = (bool)enabled });
                }
            }
            catch (Exception error) { errors.Add("Loaded UMM entry observation: " + error); }
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().OrderBy(x => x.FullName, StringComparer.Ordinal))
            {
                var item = new JObject { ["name"] = assembly.GetName().Name, ["fullName"] = assembly.FullName, ["dynamic"] = assembly.IsDynamic };
                try
                {
                    var location = assembly.IsDynamic ? null : assembly.Location;
                    item["location"] = location;
                    item["mvid"] = assembly.IsDynamic ? null : assembly.ManifestModule.ModuleVersionId.ToString();
                    item["sha256"] = string.IsNullOrEmpty(location) ? null : Hash(location);
                }
                catch (Exception error) { item["observationError"] = error.Message; errors.Add("Loaded assembly " + assembly.FullName + ": " + error.Message); }
                assemblies.Add(item);
            }
            WriteArtifact(request, EvidenceFileName, EvidenceKind, errors.Count == 0 ? "PASS" : "FAIL",
                new JObject { ["modEntries"] = mods, ["loadedAssemblies"] = assemblies,
                    ["capturedInNativeProcess"] = true, ["processId"] = System.Diagnostics.Process.GetCurrentProcess().Id }, errors);
        }
        private static object ReadMember(Type type, object instance, string name)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            var field = type.GetField(name, flags);
            if (field != null) return field.GetValue(instance);
            var property = type.GetProperty(name, flags);
            if (property == null || property.GetIndexParameters().Length != 0) throw new MissingMemberException(type.FullName, name);
            return property.GetValue(instance, null);
        }
        internal static void WriteArtifact(RuntimeRequest request, string leaf, string kind, string status, JObject observations, IEnumerable<string> errors)
        {
            var path = Path.Combine(request.EvidenceRoot, leaf);
            if (File.Exists(path)) throw new InvalidOperationException("Native artifact already exists: " + leaf);
            var assembly = typeof(Main).Assembly;
            var artifact = new JObject { ["schemaVersion"] = 1, ["evidenceKind"] = kind, ["runId"] = request.RunId,
                ["scenario"] = request.Scenario, ["branch"] = request.Branch, ["commit"] = request.Commit,
                ["productVersion"] = request.ProductVersion, ["dllSha256"] = Hash(assembly.Location),
                ["dllMvid"] = assembly.ManifestModule.ModuleVersionId.ToString(), ["createdAtUtc"] = DateTimeOffset.UtcNow.ToString("o"),
                ["status"] = status, ["observations"] = observations, ["errors"] = new JArray(errors) };
            var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try { File.WriteAllText(temporary, artifact.ToString(Formatting.None), new UTF8Encoding(false)); File.Move(temporary, path); }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
        private static string Hash(string path)
        { using (var hash = SHA256.Create()) using (var stream = File.OpenRead(path)) return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "").ToLowerInvariant(); }
    }
}
