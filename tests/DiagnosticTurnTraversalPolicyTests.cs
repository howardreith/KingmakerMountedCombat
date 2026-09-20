using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    internal static class DiagnosticTurnTraversalPolicyTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("diagnostic traversal admits exact leased idle fixture turn", ExactLeasedFixtureTurnIsAdmitted);
            runner.Run("diagnostic traversal rejects foreign or unleased turn", ForeignOrUnleasedTurnIsRejected);
            runner.Run("diagnostic traversal requires every native idle gate", EveryNativeIdleGateIsRequired);
            runner.Run("diagnostic traversal requires two stable frames and one call", StabilityAndCardinalityAreRequired);
            runner.Run("diagnostic traversal pair actor needs explicit authorization", PairActorNeedsExplicitAuthorization);
            runner.Run("diagnostic traversal rejects mounted mount turn", MountedMountTurnIsProhibited);
        }

        private static void ExactLeasedFixtureTurnIsAdmitted()
        {
            TestRunner.Equal(true, Evaluate(), "Exact leased fixture turn was rejected.");
        }

        private static void ForeignOrUnleasedTurnIsRejected()
        {
            TestRunner.Equal(false, Evaluate(rosterReferenceExact: false), "Foreign roster reference was admitted.");
            TestRunner.Equal(false, Evaluate(nonPairLeaseReferenceExact: false), "Unleased non-pair member was admitted.");
            TestRunner.Equal(false, Evaluate(directlyControllable: false), "AI/hostile turn was admitted.");
            TestRunner.Equal(false, Evaluate(samePlayerParty: false), "Another group was admitted.");
            TestRunner.Equal(false, Evaluate(currentIsExpected: true), "Desired test turn was ended by traversal.");
        }

        private static void EveryNativeIdleGateIsRequired()
        {
            TestRunner.Equal(false, Evaluate(actionableTurn: false), "Non-actionable turn was admitted.");
            TestRunner.Equal(false, Evaluate(commandsIdle: false), "Command-bearing turn was admitted.");
            TestRunner.Equal(false, Evaluate(handsIdle: false), "Hands-busy turn was admitted.");
            TestRunner.Equal(false, Evaluate(equipmentIdle: false), "Equipment-busy turn was admitted.");
            TestRunner.Equal(false, Evaluate(pairWorkIdle: false), "Active KMC pair work was admitted.");
            TestRunner.Equal(false, Evaluate(pendingNextUnitClear: false), "Pending next-unit transition was admitted.");
            TestRunner.Equal(false, Evaluate(waitingForUiClear: false), "UI-guarded transition was admitted.");
            TestRunner.Equal(false, Evaluate(defaultUnpausedTurnBasedMode: false), "Wrong mode/pause state was admitted.");
        }

        private static void StabilityAndCardinalityAreRequired()
        {
            TestRunner.Equal(false, Evaluate(stableFrames: 1), "One-frame candidate was admitted.");
            TestRunner.Equal(false, Evaluate(alreadyEnded: true), "Previously ended turn reference was admitted twice.");
            TestRunner.Equal(false, Evaluate(currentPresent: false), "Missing current turn was admitted.");
        }

        private static void PairActorNeedsExplicitAuthorization()
        {
            TestRunner.Equal(
                false,
                Evaluate(currentIsPairActor: true, pairActorPassAuthorized: false,
                    nonPairLeaseReferenceExact: false),
                "Pair actor was admitted without an explicit unmounted control transition.");
            TestRunner.Equal(
                true,
                Evaluate(currentIsPairActor: true, pairActorPassAuthorized: true,
                    nonPairLeaseReferenceExact: false),
                "Explicitly authorized pair actor was rejected.");
        }

        private static void MountedMountTurnIsProhibited()
        {
            TestRunner.Equal(
                true,
                DiagnosticTurnTraversalPolicy.IsProhibitedMountedMountTurn(true, false, true),
                "Mounted non-expected mount turn was not rejected.");
            TestRunner.Equal(
                false,
                DiagnosticTurnTraversalPolicy.IsProhibitedMountedMountTurn(true, true, true),
                "Expected mount control turn was rejected.");
            TestRunner.Equal(
                false,
                DiagnosticTurnTraversalPolicy.IsProhibitedMountedMountTurn(true, false, false),
                "Unmounted mount control turn was treated as a unified-turn violation.");
        }

        private static bool Evaluate(
            bool currentPresent = true,
            bool currentIsExpected = false,
            bool currentIsPairActor = false,
            bool pairActorPassAuthorized = false,
            bool rosterReferenceExact = true,
            bool nonPairLeaseReferenceExact = true,
            bool directlyControllable = true,
            bool samePlayerParty = true,
            bool actionableTurn = true,
            bool commandsIdle = true,
            bool handsIdle = true,
            bool equipmentIdle = true,
            bool pairWorkIdle = true,
            bool pendingNextUnitClear = true,
            bool waitingForUiClear = true,
            bool defaultUnpausedTurnBasedMode = true,
            bool alreadyEnded = false,
            int stableFrames = DiagnosticTurnTraversalPolicy.RequiredStableFrames)
        {
            return DiagnosticTurnTraversalPolicy.CanForceEndExactFixtureTurn(
                currentPresent,
                currentIsExpected,
                currentIsPairActor,
                pairActorPassAuthorized,
                rosterReferenceExact,
                nonPairLeaseReferenceExact,
                directlyControllable,
                samePlayerParty,
                actionableTurn,
                commandsIdle,
                handsIdle,
                equipmentIdle,
                pairWorkIdle,
                pendingNextUnitClear,
                waitingForUiClear,
                defaultUnpausedTurnBasedMode,
                alreadyEnded,
                stableFrames);
        }
    }
}
