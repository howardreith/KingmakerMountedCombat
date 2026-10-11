using System;

namespace KingmakerMountedCombat.Domain
{
    public enum NativeShellBindingOutcome
    {
        /// <summary>This execution was already claimed by two transitions. Permanently terminal.</summary>
        PoisonedContext,

        /// <summary>Two present bindings name different shells. Refused, never resolved to either.</summary>
        Disagreement,

        /// <summary>The authoritative binding: the command's own execution context.</summary>
        ExecutionContext,

        /// <summary>The narrow synchronous fallback: the Move slot whose process created THIS context.</summary>
        SynchronousMoveSlot,

        /// <summary>No exact ownership could be established. Refused.</summary>
        NoExactOwnership
    }

    /// <summary>
    /// Decides which binding, if any, owns a relationship delivery.
    ///
    /// A relationship command completes and leaves the native Move slot while its
    /// AbilityExecutionProcess goes on delivering on later frames, so rediscovering the
    /// shell from the Move slot at Deliver time is unsound in general. The execution
    /// context is therefore the authoritative binding. The Move slot is admitted only in
    /// the one case where no context binding can yet exist: delivery that happens
    /// synchronously inside OnAction, before the postfix has had a chance to bind. That
    /// case is recognised by the slot's own process owning THIS very context — never by
    /// recency, by the caster alone, or by a matching generation.
    /// </summary>
    public static class NativeShellBindingPolicy
    {
        public const string PoisonedContextRefusal =
            "This native execution was claimed by two mounted transitions and is permanently refused.";

        public const string DisagreementRefusal =
            "Two different mounted transitions claim this native execution.";

        public const string NoOwnershipRefusal =
            "This mounted transition owns neither its native execution process nor its native Move command.";

        /// <summary>
        /// The Move slot is an owner only when its own execution process created this exact
        /// context. A slot that merely happens to hold a registered shell is a different
        /// command and is never admitted.
        /// </summary>
        public static bool AdmitsSynchronousMoveSlot(
            bool contextPresent,
            bool slotPresent,
            bool slotOwnsThisContext,
            bool slotShellPresent)
        {
            return contextPresent && slotPresent && slotOwnsThisContext && slotShellPresent;
        }

        public static NativeShellBindingOutcome Resolve(
            bool contextPresent,
            bool contextPoisoned,
            bool contextShellPresent,
            bool slotPresent,
            bool slotOwnsThisContext,
            bool slotShellPresent,
            bool bindingsAgree,
            out string refusal)
        {
            // A poisoned context refuses first, before any slot is considered, so an
            // unresolvable conflict can never be salvaged by whatever occupies the Move
            // slot afterwards.
            if (contextPresent && contextPoisoned)
            {
                refusal = PoisonedContextRefusal;
                return NativeShellBindingOutcome.PoisonedContext;
            }

            var slotAdmitted = AdmitsSynchronousMoveSlot(
                contextPresent, slotPresent, slotOwnsThisContext, slotShellPresent);

            // Two present bindings must name the same shell. Disagreement is an explicit
            // refusal and poisons the execution, never a preference for either side.
            if (contextShellPresent && slotAdmitted && !bindingsAgree)
            {
                refusal = DisagreementRefusal;
                return NativeShellBindingOutcome.Disagreement;
            }

            if (contextShellPresent)
            {
                refusal = null;
                return NativeShellBindingOutcome.ExecutionContext;
            }

            if (slotAdmitted)
            {
                refusal = null;
                return NativeShellBindingOutcome.SynchronousMoveSlot;
            }

            refusal = NoOwnershipRefusal;
            return NativeShellBindingOutcome.NoExactOwnership;
        }

        public static bool IsRefusal(NativeShellBindingOutcome outcome)
        {
            return outcome == NativeShellBindingOutcome.PoisonedContext ||
                outcome == NativeShellBindingOutcome.Disagreement ||
                outcome == NativeShellBindingOutcome.NoExactOwnership;
        }

        /// <summary>
        /// A disagreement poisons the execution so the conflict survives the Move slot's
        /// disappearance; every other refusal is carried by the shell's own retirement.
        /// </summary>
        public static bool PoisonsTheExecution(NativeShellBindingOutcome outcome)
        {
            return outcome == NativeShellBindingOutcome.Disagreement;
        }

        public static string DescribeOwnership(NativeShellBindingOutcome outcome)
        {
            switch (outcome)
            {
                case NativeShellBindingOutcome.ExecutionContext: return "execution-context";
                case NativeShellBindingOutcome.SynchronousMoveSlot: return "synchronous-move-slot";
                case NativeShellBindingOutcome.PoisonedContext: return "poisoned-context";
                case NativeShellBindingOutcome.Disagreement: return "binding-disagreement";
                case NativeShellBindingOutcome.NoExactOwnership: return "no-exact-ownership";
                default: throw new ArgumentOutOfRangeException(nameof(outcome));
            }
        }
    }
}
