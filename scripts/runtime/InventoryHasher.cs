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
