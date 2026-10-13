using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace KingmakerMountedCombat.RuntimeTooling
{
    // Read-only tooling: hashes every byte, with bounded concurrency and no cache.
    public static class InventoryHasher
    {
        public static string[] HashFiles(string[] paths)
        {
            if (paths == null) { throw new ArgumentNullException("paths"); }
            var hashes = new string[paths.Length];
            var options = new ParallelOptions { MaxDegreeOfParallelism = Math.Min(4, Environment.ProcessorCount) };
            Parallel.For(0, paths.Length, options, index =>
            {
                if (string.IsNullOrEmpty(paths[index]) || !Path.IsPathRooted(paths[index]))
                {
                    throw new ArgumentException("Inventory paths must be exact absolute file paths.");
                }
                using (var stream = new FileStream(paths[index], FileMode.Open, FileAccess.Read,
                    FileShare.Read, 128 * 1024, FileOptions.SequentialScan))
                using (var algorithm = SHA256.Create())
                {
                    hashes[index] = BitConverter.ToString(algorithm.ComputeHash(stream))
                        .Replace("-", string.Empty).ToLowerInvariant();
                }
            });
            return hashes;
        }
    }
}

namespace KingmakerMountedCombat.RuntimeTooling
{
    // Bounded canonical UTF-8 hashing; caller retains the exact formatting and ordering.
    public sealed class ManifestCanonicalHasher : IDisposable
    {
        private readonly SHA256 algorithm = SHA256.Create();
        private readonly CryptoStream stream;
        private readonly StreamWriter writer;
        private bool first = true;
        private bool complete;
        public ManifestCanonicalHasher()
        {
            stream = new CryptoStream(Stream.Null, algorithm, CryptoStreamMode.Write);
            writer = new StreamWriter(stream, new System.Text.UTF8Encoding(false), 4096, true);
        }
        public void Append(string row)
        {
            if (complete) throw new InvalidOperationException("Canonical manifest is complete.");
            if (!first) writer.Write('\n');
            writer.Write(row);
            first = false;
        }
        public string Complete()
        {
            if (complete) throw new InvalidOperationException("Canonical manifest is complete.");
            writer.Flush();
            stream.FlushFinalBlock();
            complete = true;
            return BitConverter.ToString(algorithm.Hash).Replace("-", string.Empty).ToLowerInvariant();
        }
        public void Dispose()
        {
            try { writer.Dispose(); }
            finally { try { stream.Dispose(); } finally { algorithm.Dispose(); } }
        }
    }
}
