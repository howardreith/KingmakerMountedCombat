namespace KingmakerMountedCombat.Diagnostics
{
    // Pure admission rule for a leased non-pair member's declared ordinary command
    // (DiagnosticNonPairPartyAiLease). The first declaration needs no history. A later
    // one is admitted only sequentially (the previous declaration closed), only for the
    // same exact member, and only within an explicit bound of at most two declarations
    // per lease (the row-57 prone fold: a healer's at most two native scroll attempts).
    internal static class DeclaredMemberCommandPolicy
    {
        internal const int MaximumSequentialSameMember = 2;

        internal static bool Admits(object openMember, object lastMember, int declaredCount, object unit, int maximumDeclarations)
        {
            if (unit == null || openMember != null || declaredCount < 0) return false;
            if (maximumDeclarations < 1 || maximumDeclarations > MaximumSequentialSameMember) return false;
            if (declaredCount == 0) return true;
            return declaredCount < maximumDeclarations && ReferenceEquals(unit, lastMember);
        }
    }
}
