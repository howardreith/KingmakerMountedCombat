using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    /// <summary>
    /// Exhaustive proof of the two-binding resolution. The invariants come from the
    /// installed assembly: UnitUseAbility.OnAction stores the execution process and
    /// returns terminal unless the ability engages a unit, so a relationship command
    /// completes and leaves the native Move slot while its AbilityExecutionProcess goes
    /// on delivering. The execution context is therefore authoritative and the Move slot
    /// is admissible only for delivery that happens synchronously inside OnAction.
    /// </summary>
    internal static class NativeShellBindingPolicyTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("a poisoned execution refuses before any Move-slot consideration", PoisonedContextIsTerminalFirst);
            runner.Run("the execution-context binding is authoritative when present", ContextBindingIsAuthoritative);
            runner.Run("the Move slot is admitted only for synchronous delivery of this exact context", SlotAdmittedOnlySynchronously);
            runner.Run("two disagreeing bindings refuse and poison instead of preferring either", DisagreementRefusesAndPoisons);
            runner.Run("no binding at all is an explicit refusal, never a loose fallback", NoBindingRefuses);
            runner.Run("the binding decision is total and only disagreement poisons", DecisionIsTotalAndPoisonIsExact);
        }

        private static NativeShellBindingOutcome Resolve(
            bool contextPresent, bool contextPoisoned, bool contextShellPresent,
            bool slotPresent, bool slotOwnsThisContext, bool slotShellPresent,
            bool bindingsAgree, out string refusal)
        {
            return NativeShellBindingPolicy.Resolve(
                contextPresent, contextPoisoned, contextShellPresent,
                slotPresent, slotOwnsThisContext, slotShellPresent, bindingsAgree, out refusal);
        }

        private static void PoisonedContextIsTerminalFirst()
        {
            // Whatever else is true -- including a perfectly good slot that owns this very
            // context and carries its own shell -- a poisoned execution stays refused.
            foreach (var contextShell in new[] { false, true })
            {
                foreach (var slotShell in new[] { false, true })
                {
                    foreach (var slotOwns in new[] { false, true })
                    {
                        string refusal;
                        var outcome = Resolve(true, true, contextShell, true, slotOwns, slotShell, true, out refusal);
                        TestRunner.Equal(NativeShellBindingOutcome.PoisonedContext, outcome,
                            "A poisoned execution was salvaged (contextShell=" + contextShell +
                            ";slotOwns=" + slotOwns + ";slotShell=" + slotShell + ").");
                        TestRunner.Equal(NativeShellBindingPolicy.PoisonedContextRefusal, refusal,
                            "The poisoned refusal text changed.");
                    }
                }
            }
        }

        private static void ContextBindingIsAuthoritative()
        {
            string refusal;
            // Context alone.
            TestRunner.Equal(NativeShellBindingOutcome.ExecutionContext,
                Resolve(true, false, true, false, false, false, false, out refusal),
                "The authoritative context binding was not used.");
            TestRunner.Equal(null, refusal, "An accepted context binding produced a refusal.");
            // Context plus a slot that is not an owner: the context still wins and the
            // foreign slot contributes nothing at all.
            TestRunner.Equal(NativeShellBindingOutcome.ExecutionContext,
                Resolve(true, false, true, true, false, true, false, out refusal),
                "A foreign Move slot displaced the authoritative context binding.");
            // Context plus an agreeing owner slot: still resolved through the context.
            TestRunner.Equal(NativeShellBindingOutcome.ExecutionContext,
                Resolve(true, false, true, true, true, true, true, out refusal),
                "An agreeing pair of bindings was not resolved through the context.");
            TestRunner.Equal(null, refusal, "An agreeing pair of bindings produced a refusal.");
        }

        private static void SlotAdmittedOnlySynchronously()
        {
            string refusal;
            // The one admissible fallback: no context binding can exist yet because the
            // OnAction postfix has not run, and this Move slot own execution process
            // created this exact context.
            TestRunner.Equal(NativeShellBindingOutcome.SynchronousMoveSlot,
                Resolve(true, false, false, true, true, true, false, out refusal),
                "Synchronous delivery inside OnAction was refused its own Move slot.");
            TestRunner.Equal(null, refusal, "An admitted synchronous slot produced a refusal.");

            // Every way of failing that test refuses. A registered shell sitting in the
            // Move slot is a DIFFERENT command unless its process owns this context: this
            // is what forbids a recent-shell, last-shell or caster-only lookup.
            TestRunner.Equal(NativeShellBindingOutcome.NoExactOwnership,
                Resolve(true, false, false, true, false, true, false, out refusal),
                "A Move slot that does not own this context was admitted.");
            TestRunner.Equal(NativeShellBindingOutcome.NoExactOwnership,
                Resolve(true, false, false, true, true, false, false, out refusal),
                "An owning Move slot with no registered shell was admitted.");
            TestRunner.Equal(NativeShellBindingOutcome.NoExactOwnership,
                Resolve(true, false, false, false, true, true, false, out refusal),
                "An absent Move slot was admitted.");
            // Without a context there is nothing for a slot to own, so no slot is ever
            // admissible -- the comparison that makes the fallback safe cannot be made.
            foreach (var slotOwns in new[] { false, true })
            {
                TestRunner.Equal(NativeShellBindingOutcome.NoExactOwnership,
                    Resolve(false, false, false, true, slotOwns, true, false, out refusal),
                    "A Move slot was admitted with no execution context to own (slotOwns=" + slotOwns + ").");
            }
            TestRunner.True(!NativeShellBindingPolicy.AdmitsSynchronousMoveSlot(false, true, true, true),
                "The slot admission predicate ignored the missing context.");
            TestRunner.True(NativeShellBindingPolicy.AdmitsSynchronousMoveSlot(true, true, true, true),
                "The slot admission predicate refused the one lawful synchronous case.");
        }

        private static void DisagreementRefusesAndPoisons()
        {
            string refusal;
            var outcome = Resolve(true, false, true, true, true, true, false, out refusal);
            TestRunner.Equal(NativeShellBindingOutcome.Disagreement, outcome,
                "Two bindings naming different shells resolved to one of them.");
            TestRunner.Equal(NativeShellBindingPolicy.DisagreementRefusal, refusal,
                "The disagreement refusal text changed.");
            TestRunner.True(NativeShellBindingPolicy.PoisonsTheExecution(outcome),
                "A binding disagreement did not poison the execution.");
            // A disagreement is only possible when both bindings are genuinely present;
            // a non-owning slot cannot manufacture one.
            TestRunner.Equal(NativeShellBindingOutcome.ExecutionContext,
                Resolve(true, false, true, true, false, true, false, out refusal),
                "A non-owning slot manufactured a disagreement.");
        }

        private static void NoBindingRefuses()
        {
            string refusal;
            TestRunner.Equal(NativeShellBindingOutcome.NoExactOwnership,
                Resolve(true, false, false, false, false, false, false, out refusal),
                "A delivery with no binding at all was accepted.");
            TestRunner.Equal(NativeShellBindingPolicy.NoOwnershipRefusal, refusal,
                "The no-ownership refusal text changed.");
            TestRunner.Equal(NativeShellBindingOutcome.NoExactOwnership,
                Resolve(false, false, false, false, false, false, false, out refusal),
                "A delivery with neither context nor slot was accepted.");
        }

        private static void DecisionIsTotalAndPoisonIsExact()
        {
            var accepted = 0;
            var poisoned = 0;
            foreach (var contextPresent in new[] { false, true })
            foreach (var contextPoisoned in new[] { false, true })
            foreach (var contextShell in new[] { false, true })
            foreach (var slotPresent in new[] { false, true })
            foreach (var slotOwns in new[] { false, true })
            foreach (var slotShell in new[] { false, true })
            foreach (var agree in new[] { false, true })
            {
                string refusal;
                var outcome = Resolve(contextPresent, contextPoisoned, contextShell,
                    slotPresent, slotOwns, slotShell, agree, out refusal);
                var slotAdmitted = NativeShellBindingPolicy.AdmitsSynchronousMoveSlot(
                    contextPresent, slotPresent, slotOwns, slotShell);
                var state = "context=" + contextPresent + ";poisoned=" + contextPoisoned +
                    ";contextShell=" + contextShell + ";slot=" + slotPresent +
                    ";slotOwns=" + slotOwns + ";slotShell=" + slotShell + ";agree=" + agree;

                // Refusal text and outcome always agree.
                TestRunner.Equal(NativeShellBindingPolicy.IsRefusal(outcome), refusal != null,
                    "Refusal text disagreed with the outcome (" + state + ").");
                // Only a disagreement poisons the execution.
                TestRunner.Equal(outcome == NativeShellBindingOutcome.Disagreement,
                    NativeShellBindingPolicy.PoisonsTheExecution(outcome),
                    "Poisoning widened beyond a binding disagreement (" + state + ").");
                // Every outcome is describable, so the ledger can never record an unknown.
                TestRunner.True(!string.IsNullOrEmpty(NativeShellBindingPolicy.DescribeOwnership(outcome)),
                    "An outcome had no ownership description (" + state + ").");

                if (contextPresent && contextPoisoned)
                {
                    TestRunner.Equal(NativeShellBindingOutcome.PoisonedContext, outcome,
                        "A poisoned execution was not terminal (" + state + ").");
                    poisoned++;
                    continue;
                }
                if (outcome == NativeShellBindingOutcome.ExecutionContext)
                {
                    TestRunner.True(contextShell, "A context binding was used without one present (" + state + ").");
                    accepted++;
                }
                else if (outcome == NativeShellBindingOutcome.SynchronousMoveSlot)
                {
                    TestRunner.True(slotAdmitted && !contextShell,
                        "The slot fallback was used while the context binding existed (" + state + ").");
                    accepted++;
                }
                else if (outcome == NativeShellBindingOutcome.Disagreement)
                {
                    TestRunner.True(contextShell && slotAdmitted && !agree,
                        "A disagreement was declared without two disagreeing bindings (" + state + ").");
                }
                else
                {
                    TestRunner.True(!contextShell && !slotAdmitted,
                        "A present binding was refused as unowned (" + state + ").");
                }
            }

            TestRunner.True(accepted > 0 && poisoned > 0,
                "The exhaustive sweep never exercised an acceptance or a poisoned execution.");
        }
    }
}
