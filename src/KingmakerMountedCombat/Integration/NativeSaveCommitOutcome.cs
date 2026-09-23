using System;

namespace KingmakerMountedCombat.Integration
{
    // What actually happened to an interrupted save's archive.
    //
    // A faulted archive worker does not establish that nothing was written. The
    // native commit is File.Replace (or, for a first-ever save, the rename)
    // returning; the descriptor rebinding, ownership completion, worker cleanup
    // and notification that follow can all throw without un-writing those bytes.
    // So the outcome is decided from the recorded commit boundary, never from
    // task state, and "the previous save is unchanged" is only ever said when a
    // previous save existed and is still there.
    internal enum NativeSaveCommitKind
    {
        // The bytes were written. Say so even if a later step failed.
        Committed,

        // No commit happened for this operation. Whether a previous archive
        // survives is a separate question the caller still has to check.
        NotWritten,

        // A commit landed, but not this operation's, so nothing about this save
        // is proven. Never promise unchanged bytes here.
        Unconfirmed
    }

    internal static class NativeSaveCommitOutcome
    {
        // commitsAtStart/commitsNow bracket the operation; landed is the
        // destination the last recorded commit wrote to. requestedPath is the
        // archive the save was asked to write, preparedPath the leaf the native
        // routine minted for it -- a commit replaces its target in place and
        // rebinds the path, so either may legitimately be where it landed.
        internal static NativeSaveCommitKind Decide(int commitsAtStart, int commitsNow, string landed,
            string requestedPath, string preparedPath, Func<string, bool> exists)
        {
            if (commitsNow <= commitsAtStart) return NativeSaveCommitKind.NotWritten;
            var mine = !string.IsNullOrEmpty(landed) &&
                (SamePath(landed, requestedPath) || SamePath(landed, preparedPath));
            if (!mine) return NativeSaveCommitKind.Unconfirmed;
            // A commit that was recorded but whose archive is not on disk is not
            // a save the player can rely on, and it is not a clean "nothing
            // happened" either.
            return exists != null && exists(landed)
                ? NativeSaveCommitKind.Committed
                : NativeSaveCommitKind.Unconfirmed;
        }

        internal static bool SamePath(string left, string right) =>
            !string.IsNullOrEmpty(left) && !string.IsNullOrEmpty(right) &&
            string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }
}
