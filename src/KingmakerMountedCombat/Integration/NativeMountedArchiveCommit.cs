using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony12;
using Kingmaker.EntitySystem.Persistence;

namespace KingmakerMountedCombat.Integration
{
    // Only called from the verified native worker's replacement branch. Native
    // Save has already committed the complete archive, including our member.
    internal static class NativeMountedArchiveCommit
    {
        private static readonly Type Zip = typeof(SaveManager).Assembly.GetType(
            "Kingmaker.EntitySystem.Persistence.ZipSaver", true);
        private static readonly FieldInfo PathField = NativeCombatActorPersistence.Field(
            Zip, "m_FolderName", 0x04005431, typeof(string));
        private static readonly MethodInfo Rename = ResolveRename();
        private static int replacementFailures;
        internal static int ReplacementFailureCount => System.Threading.Volatile.Read(ref replacementFailures);

        // The archive is committed the instant the native replacement returns.
        // Everything after that -- descriptor rebinding, ownership completion,
        // the worker's own cleanup, notification -- can still throw, and none of
        // those failures un-writes the bytes. Recording the commit here is what
        // lets an interrupted operation distinguish "never written" from
        // "written, then something later failed", instead of inferring the save
        // was lost from a faulted task.
        private static int commits;
        private static string lastCommitted;
        internal static int CommitCount => System.Threading.Volatile.Read(ref commits);
        internal static string LastCommittedDestination => System.Threading.Volatile.Read(ref lastCommitted);

        private static void RecordCommit(string destination)
        {
            System.Threading.Volatile.Write(ref lastCommitted, destination);
            System.Threading.Interlocked.Increment(ref commits);
        }

        private static MethodInfo ResolveRename()
        {
            var method = Zip.GetMethod("RenameFile", new[] { typeof(string) });
            if (method == null || method.MetadataToken != 0x0600806D)
                throw new InvalidOperationException("Native ZIP replacement signature changed.");
            return method;
        }

        private static bool IsZip(ISaver saver) => saver != null && saver.GetType() == Zip;
        private static string PathOf(ISaver saver) => (string)PathField.GetValue(saver);

        internal static void PreservePrevious(ISaver previous, SaveInfo replacement)
        {
            if (!IsZip(previous) || !IsZip(replacement?.Saver)) { previous.Clear(); return; }
            ValidatePaths(PathOf(replacement.Saver), PathOf(previous));
            // Release cached native read handles without deleting last-good bytes.
            previous.Dispose();
        }

        internal static void Replace(ISaver staged, string destination, SaveInfo original)
        {
            if (!IsZip(staged) || !IsZip(original?.Saver))
            {
                // A first-ever save has no original to replace; the rename is its
                // commit and counts exactly the same.
                Rename.Invoke(staged, new object[] { destination });
                RecordCommit(destination);
                return;
            }
            if (PathOf(original.Saver) != destination)
                throw new IOException("Native replacement destination differs from its original descriptor.");
            var source = PathOf(staged);
            ValidatePaths(source, destination);
            using (var ownership = NativePersistenceIsolation.BeginArchiveReplacement(source, destination))
            {
                // Same-directory replacement commits the already complete native
                // archive atomically. No original deletion and no content rewrite.
                try { File.Replace(source, destination, null); }
                catch (IOException) { System.Threading.Interlocked.Increment(ref replacementFailures); throw; }
                // Committed. Record it before the two steps below, either of
                // which can throw without un-writing these bytes.
                RecordCommit(destination);
                PathField.SetValue(staged, destination);
                ownership?.Complete();
            }
        }

        private static void ValidatePaths(string source, string destination)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(destination) ||
                !Path.IsPathRooted(source) || !Path.IsPathRooted(destination) ||
                Path.GetFullPath(source) != source || Path.GetFullPath(destination) != destination ||
                string.Equals(source, destination, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(Path.GetDirectoryName(source), Path.GetDirectoryName(destination), StringComparison.OrdinalIgnoreCase) ||
                !File.Exists(source) || !File.Exists(destination) ||
                (File.GetAttributes(source) & (FileAttributes.ReparsePoint | FileAttributes.Directory)) != 0 ||
                (File.GetAttributes(destination) & (FileAttributes.ReparsePoint | FileAttributes.Directory)) != 0)
                throw new IOException("Native archive replacement requires two existing regular sibling files.");
        }

        internal static IEnumerable<CodeInstruction> Transform(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
            var clear = Enumerable.Range(0, code.Count).Where(i => Token(code[i], 0x06007FB2)).ToArray();
            var rename = Enumerable.Range(0, code.Count).Where(i => Token(code[i], 0x0600806D)).ToArray();
            if (clear.Length != 1 || rename.Length != 1 || clear[0] >= rename[0] ||
                code[clear[0]].labels.Count != 0 || code[rename[0]].labels.Count != 0 ||
                code.Count(i => Token(i, 0x06007FB3)) != 1 ||
                code.Count(i => Token(i, 0x06007FA5)) != 1)
                throw new InvalidOperationException("Native save commit/replacement branch changed.");
            var flags = BindingFlags.Static | BindingFlags.NonPublic;
            code[rename[0]].opcode = OpCodes.Call;
            code[rename[0]].operand = typeof(NativeMountedArchiveCommit).GetMethod(nameof(Replace), flags);
            code.Insert(rename[0], new CodeInstruction(OpCodes.Ldarg_3));
            code[clear[0]].opcode = OpCodes.Call;
            code[clear[0]].operand = typeof(NativeMountedArchiveCommit).GetMethod(nameof(PreservePrevious), flags);
            code.Insert(clear[0], new CodeInstruction(OpCodes.Ldarg_1));
            return code;
        }

        private static bool Token(CodeInstruction instruction, int token) =>
            instruction.operand is MemberInfo member && member.Module == typeof(SaveManager).Module &&
                member.MetadataToken == token;
    }
}
