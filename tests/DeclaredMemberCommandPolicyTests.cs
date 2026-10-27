using KingmakerMountedCombat.Diagnostics;

namespace KingmakerMountedCombat.Tests
{
    // b206 row 57: the prone fold's healer is a leased non-pair member; each of its at most two native
    // scroll attempts must be a declared lease command, sequential and for the same healer only.
    internal static class DeclaredMemberCommandPolicyTests
    {
        internal static void Register(TestRunner runner)
        {
            runner.Run("declared member command: the first declaration is admitted", () =>
            {
                var healer = new object();
                TestRunner.True(DeclaredMemberCommandPolicy.Admits(null, null, 0, healer, 1), "First single declaration refused.");
                TestRunner.True(DeclaredMemberCommandPolicy.Admits(null, null, 0, healer, 2), "First bounded declaration refused.");
            });
            runner.Run("declared member command: the default single-declaration lease refuses a second", () =>
            {
                var healer = new object();
                TestRunner.True(!DeclaredMemberCommandPolicy.Admits(null, healer, 1, healer, 1), "A second declaration passed the single bound.");
            });
            runner.Run("declared member command: a second sequential declaration of the same member is admitted within two", () =>
            {
                var healer = new object();
                TestRunner.True(DeclaredMemberCommandPolicy.Admits(null, healer, 1, healer, 2), "The bounded second attempt was refused.");
            });
            runner.Run("declared member command: overlap, another member, a third declaration or a wider bound are refused", () =>
            {
                var healer = new object(); var other = new object();
                TestRunner.True(!DeclaredMemberCommandPolicy.Admits(healer, healer, 1, healer, 2), "An overlapping declaration was admitted.");
                TestRunner.True(!DeclaredMemberCommandPolicy.Admits(null, healer, 1, other, 2), "Another member's second declaration was admitted.");
                TestRunner.True(!DeclaredMemberCommandPolicy.Admits(null, healer, 2, healer, 2), "A third declaration was admitted.");
                TestRunner.True(!DeclaredMemberCommandPolicy.Admits(null, null, 0, healer, 3), "A bound above two was admitted.");
                TestRunner.True(!DeclaredMemberCommandPolicy.Admits(null, null, 0, healer, 0), "A zero bound was admitted.");
                TestRunner.True(!DeclaredMemberCommandPolicy.Admits(null, null, 0, null, 1), "A null member was admitted.");
                TestRunner.True(!DeclaredMemberCommandPolicy.Admits(null, null, -1, healer, 1), "A negative count was admitted.");
            });
        }
    }
}
