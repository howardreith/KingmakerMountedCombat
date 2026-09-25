using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using Kingmaker.EntitySystem.Persistence;

namespace KingmakerMountedCombat.Integration
{
    internal static class NativeMountedSaveStorage
    {
        private const int MaximumBytes = 131072;
        private static readonly UTF8Encoding Encoding = new UTF8Encoding(false, true);
        private static readonly Type ZipSaver = typeof(SaveManager).Assembly.GetType(
            "Kingmaker.EntitySystem.Persistence.ZipSaver", true);
        private static readonly MethodInfo NativeZip = ResolveZip();
        private static MethodInfo ResolveZip()
        {
            if (typeof(SaveManager).Assembly.ManifestModule.ModuleVersionId !=
                new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7"))
                throw new InvalidOperationException("Native mounted storage assembly changed.");
            var method = ZipSaver.GetMethod("get_ZipFile", BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null || method.MetadataToken != 0x0600805C)
                throw new InvalidOperationException("Native archive accessor changed.");
            return method;
        }

        internal static MountedSaveReadResult Read(ISaver saver)
        {
            if (saver == null) throw new ArgumentNullException(nameof(saver));
            if (saver.GetType() != ZipSaver)
            {
                if (saver.GetAllFiles().Contains(MountedSaveData.ArchiveMember))
                    return new MountedSaveReadResult(MountedSaveReadKind.Invalid, null, null,
                        "Mounted metadata requires the qualified native archive format.");
                return MountedSaveCodec.Decode(null);
            }
            var zip = NativeZip.Invoke(saver, null);
            if (zip == null) throw new InvalidDataException("The selected native archive could not be read.");
            var entries = (IEnumerable)zip.GetType().GetProperty("Entries").GetValue(zip, null);
            var matches = 0;
            long length = 0;
            foreach (var entry in entries)
            {
                var type = entry.GetType();
                var name = (string)type.GetProperty("FileName").GetValue(entry, null);
                if (name != MountedSaveData.ArchiveMember) continue;
                matches++;
                length = (long)type.GetProperty("UncompressedSize").GetValue(entry, null);
            }
            if (matches == 0) return MountedSaveCodec.Decode(null);
            if (matches != 1 || length <= 0 || length > MaximumBytes)
                return new MountedSaveReadResult(MountedSaveReadKind.Invalid, null, null,
                    "Mounted archive member is duplicated or exceeds its bounded size.");
            var bytes = saver.ReadBytes(MountedSaveData.ArchiveMember);
            if (bytes == null || bytes.LongLength != length)
                throw new InvalidDataException("Mounted archive member changed while reading.");
            try { return MountedSaveCodec.Decode(Encoding.GetString(bytes)); }
            catch (DecoderFallbackException)
            {
                return new MountedSaveReadResult(MountedSaveReadKind.Invalid, null, null,
                    "Mounted archive metadata is not valid UTF-8.");
            }
        }

        // Every JSON member of the archive (areas, party, player, header) and the
        // KMC member, read through the engine's own saver and searched for the
        // given identities. Bounded, and any member that cannot be read exactly
        // aborts the scan rather than being skipped.
        private const int MaximumScanMembers = 1024;
        private const long MaximumScanMemberBytes = 64L * 1024 * 1024;
        private const long MaximumScanTotalBytes = 512L * 1024 * 1024;
        internal static System.Collections.Generic.List<string> FindReferences(ISaver saver,
            System.Collections.Generic.IEnumerable<string> tokens, out int scannedMembers, out long scannedBytes)
        {
            if (saver == null) throw new ArgumentNullException(nameof(saver));
            if (saver.GetType() != ZipSaver)
                throw new InvalidOperationException("Reference scanning requires the qualified native archive format.");
            var zip = NativeZip.Invoke(saver, null);
            if (zip == null) throw new InvalidDataException("The selected native archive could not be read.");
            var entries = (IEnumerable)zip.GetType().GetProperty("Entries").GetValue(zip, null);
            var lenient = new UTF8Encoding(false, false);
            var members = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, string>>();
            scannedMembers = 0; scannedBytes = 0;
            foreach (var entry in entries)
            {
                var type = entry.GetType();
                var name = (string)type.GetProperty("FileName").GetValue(entry, null);
                if (string.IsNullOrEmpty(name)) throw new InvalidDataException("An archive member has no name.");
                if (!name.EndsWith(".json", StringComparison.OrdinalIgnoreCase) && name != MountedSaveData.ArchiveMember) continue;
                var length = (long)type.GetProperty("UncompressedSize").GetValue(entry, null);
                if (length < 0 || length > MaximumScanMemberBytes)
                    throw new InvalidDataException("Archive member exceeds the scan bound: " + name);
                if (++scannedMembers > MaximumScanMembers)
                    throw new InvalidDataException("Archive has more members than the scan bound.");
                scannedBytes += length;
                if (scannedBytes > MaximumScanTotalBytes)
                    throw new InvalidDataException("Archive exceeds the total scan bound.");
                var bytes = saver.ReadBytes(name);
                if (bytes == null || bytes.LongLength != length)
                    throw new InvalidDataException("Archive member changed while scanning: " + name);
                members.Add(new System.Collections.Generic.KeyValuePair<string, string>(name, lenient.GetString(bytes)));
            }
            return Domain.RemovalReadinessPolicy.ReferenceHits(members, tokens);
        }

        internal static void ConstrainIsolatedCommit(ISaver saver, string root)
        {
            if (saver == null || saver.GetType() != ZipSaver)
                throw new InvalidOperationException("Isolated commit requires the exact native archive writer.");
            var zip = NativeZip.Invoke(saver, null);
            var type = zip.GetType();
            if (type.Assembly.ManifestModule.ModuleVersionId != new Guid("115b0c21-1e45-4f4b-81d7-eca9951b7383"))
                throw new InvalidOperationException("Installed native ZIP writer changed.");
            var name = type.GetProperty("Name");
            var temporary = type.GetProperty("TempFileFolder");
            if (name?.GetGetMethod()?.MetadataToken != 0x060001CF ||
                temporary?.GetSetMethod()?.MetadataToken != 0x060001F2 ||
                Path.GetDirectoryName((string)name.GetValue(zip, null)) != root)
                throw new InvalidOperationException("Native archive destination is outside its exact isolated root.");
            // Exact get_WriteStream (0600021E) chooses this folder or Name's
            // parent. Both are now the same authorized run-owned directory.
            temporary.SetValue(zip, root, null);
        }

        internal static void Stage(ISaver saver, string immutableJson)
        {
            if (saver == null || saver.GetType() != ZipSaver)
                throw new InvalidOperationException("Mounted metadata requires the qualified native archive writer.");
            if (immutableJson == null) throw new ArgumentNullException(nameof(immutableJson));
            var bytes = Encoding.GetBytes(immutableJson);
            if (bytes.Length == 0 || bytes.Length > MaximumBytes)
                throw new InvalidDataException("Mounted archive metadata exceeds the bounded size.");
            // SaveBytes updates the native in-memory archive. Native Save alone
            // commits it with the entity snapshot, screenshot and header.
            saver.SaveBytes(MountedSaveData.ArchiveMember, bytes);
        }
    }
}
