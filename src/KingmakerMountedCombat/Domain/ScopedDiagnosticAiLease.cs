using System;
using System.Collections.Generic;

namespace KingmakerMountedCombat.Domain
{
    /// <summary>
    /// Reversibly suppresses AI for an exact, prevalidated diagnostic unit set.
    /// The lease never owns commands: every unit must have an empty command queue
    /// before acquisition, throughout the lease, and after restoration.
    /// </summary>
    public sealed class ScopedDiagnosticAiLease<TUnit>
        where TUnit : class
    {
        private readonly Func<TUnit, string> getId;
        private readonly Func<TUnit, bool> contextIsExact;
        private readonly Func<TUnit, bool> commandsAreEmpty;
        private readonly Func<TUnit, bool> getRawAiEnabled;
        private readonly Func<TUnit, bool> getEffectiveAiEnabled;
        private readonly Action<TUnit, bool> setRawAiEnabled;
        private readonly List<State> states = new List<State>();

        public ScopedDiagnosticAiLease(
            Func<TUnit, string> getId,
            Func<TUnit, bool> contextIsExact,
            Func<TUnit, bool> commandsAreEmpty,
            Func<TUnit, bool> getRawAiEnabled,
            Func<TUnit, bool> getEffectiveAiEnabled,
            Action<TUnit, bool> setRawAiEnabled)
        {
            this.getId = getId ?? throw new ArgumentNullException(nameof(getId));
            this.contextIsExact = contextIsExact ?? throw new ArgumentNullException(nameof(contextIsExact));
            this.commandsAreEmpty = commandsAreEmpty ?? throw new ArgumentNullException(nameof(commandsAreEmpty));
            this.getRawAiEnabled = getRawAiEnabled ?? throw new ArgumentNullException(nameof(getRawAiEnabled));
            this.getEffectiveAiEnabled = getEffectiveAiEnabled ?? throw new ArgumentNullException(nameof(getEffectiveAiEnabled));
            this.setRawAiEnabled = setRawAiEnabled ?? throw new ArgumentNullException(nameof(setRawAiEnabled));
        }

        public bool IsAcquired { get; private set; }

        public bool LastActiveValidationPassed { get; private set; }

        public bool LastRestoreVerified { get; private set; }

        public IReadOnlyList<State> States => states;

        public void Acquire(IEnumerable<TUnit> units)
        {
            if (IsAcquired)
            {
                throw new InvalidOperationException("A diagnostic AI lease is already active.");
            }

            var candidates = Materialize(units);
            ValidateExactCandidates(candidates, null, true);
            states.Clear();
            foreach (var unit in candidates)
            {
                states.Add(new State(
                    unit,
                    getId(unit),
                    commandsAreEmpty(unit),
                    getRawAiEnabled(unit),
                    getEffectiveAiEnabled(unit)));
            }

            IsAcquired = true;
            LastActiveValidationPassed = false;
            LastRestoreVerified = false;
            try
            {
                foreach (var state in states)
                {
                    setRawAiEnabled(state.Unit, false);
                }
                ValidateActive(candidates);
            }
            catch (Exception acquisitionFailure)
            {
                try
                {
                    Restore(candidates);
                }
                catch (Exception restorationFailure)
                {
                    throw new AggregateException(
                        "Diagnostic AI acquisition failed and exact rollback also failed.",
                        acquisitionFailure,
                        restorationFailure);
                }
                throw new InvalidOperationException(
                    "Diagnostic AI acquisition failed and was rolled back exactly.",
                    acquisitionFailure);
            }
        }

        public void ValidateActive(IEnumerable<TUnit> currentUnits)
        {
            RequireAcquired();
            var current = Materialize(currentUnits);
            ValidateExactCandidates(current, states, true);

            for (var index = 0; index < states.Count; index++)
            {
                var state = states[index];
                state.CommandsEmptyDuring = commandsAreEmpty(state.Unit);
                state.RawAiDuring = getRawAiEnabled(state.Unit);
                state.EffectiveAiDuring = getEffectiveAiEnabled(state.Unit);
                if (!state.CommandsEmptyDuring || state.RawAiDuring || state.EffectiveAiDuring)
                {
                    LastActiveValidationPassed = false;
                    throw new InvalidOperationException(
                        "Diagnostic AI lease lost command or disabled-AI invariants for " + state.UnitId + ".");
                }
            }
            LastActiveValidationPassed = true;
        }

        public void Restore(IEnumerable<TUnit> currentUnits)
        {
            if (!IsAcquired)
            {
                return;
            }

            var failures = new List<Exception>();
            try
            {
                var current = Materialize(currentUnits);
                ValidateExactCandidates(current, states, true);
            }
            catch (Exception exception)
            {
                failures.Add(new InvalidOperationException(
                    "Diagnostic AI lease membership or command state changed before restoration.",
                    exception));
            }

            foreach (var state in states)
            {
                try { setRawAiEnabled(state.Unit, state.RawAiBefore); }
                catch (Exception exception)
                {
                    failures.Add(new InvalidOperationException(
                        "Could not restore raw AI state for " + state.UnitId + ".",
                        exception));
                }
            }

            foreach (var state in states)
            {
                try
                {
                    state.CommandsEmptyAfter = commandsAreEmpty(state.Unit);
                    state.RawAiAfter = getRawAiEnabled(state.Unit);
                    state.EffectiveAiAfter = getEffectiveAiEnabled(state.Unit);
                    if (!contextIsExact(state.Unit) || !state.CommandsEmptyAfter ||
                        state.RawAiAfter != state.RawAiBefore ||
                        state.EffectiveAiAfter != state.EffectiveAiBefore)
                    {
                        failures.Add(new InvalidOperationException(
                            "Diagnostic AI restoration did not reproduce exact context, command, raw, and effective state for " +
                            state.UnitId + "."));
                    }
                }
                catch (Exception exception)
                {
                    failures.Add(new InvalidOperationException(
                        "Could not verify restored AI state for " + state.UnitId + ".",
                        exception));
                }
            }

            if (failures.Count != 0)
            {
                LastRestoreVerified = false;
                throw new AggregateException("Diagnostic AI lease restoration retained residue.", failures);
            }

            IsAcquired = false;
            LastRestoreVerified = true;
        }

        private List<TUnit> Materialize(IEnumerable<TUnit> units)
        {
            if (units == null)
            {
                throw new ArgumentNullException(nameof(units));
            }
            return new List<TUnit>(units);
        }

        private void ValidateExactCandidates(
            IReadOnlyList<TUnit> candidates,
            IReadOnlyList<State> expected,
            bool requireEmptyCommands)
        {
            if (candidates.Count == 0)
            {
                throw new InvalidOperationException("A diagnostic AI lease requires at least one non-pair unit.");
            }
            if (expected != null && candidates.Count != expected.Count)
            {
                throw new InvalidOperationException("Diagnostic AI lease membership count changed.");
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < candidates.Count; index++)
            {
                var unit = candidates[index];
                var id = unit == null ? null : getId(unit);
                if (unit == null || string.IsNullOrWhiteSpace(id) || !ids.Add(id) || !contextIsExact(unit))
                {
                    throw new InvalidOperationException("Diagnostic AI lease candidate identity or context is not exact.");
                }
                for (var prior = 0; prior < index; prior++)
                {
                    if (ReferenceEquals(candidates[prior], unit))
                    {
                        throw new InvalidOperationException("Diagnostic AI lease candidate is duplicated.");
                    }
                }
                if (expected != null &&
                    (!ReferenceEquals(unit, expected[index].Unit) ||
                     !string.Equals(id, expected[index].UnitId, StringComparison.Ordinal)))
                {
                    throw new InvalidOperationException("Diagnostic AI lease membership identity or order changed.");
                }
                if (requireEmptyCommands && !commandsAreEmpty(unit))
                {
                    throw new InvalidOperationException("Diagnostic AI lease refuses a non-empty command queue.");
                }
            }
        }

        private void RequireAcquired()
        {
            if (!IsAcquired)
            {
                throw new InvalidOperationException("A diagnostic AI lease is not active.");
            }
        }

        public sealed class State
        {
            internal State(
                TUnit unit,
                string unitId,
                bool commandsEmptyBefore,
                bool rawAiBefore,
                bool effectiveAiBefore)
            {
                Unit = unit;
                UnitId = unitId;
                CommandsEmptyBefore = commandsEmptyBefore;
                RawAiBefore = rawAiBefore;
                EffectiveAiBefore = effectiveAiBefore;
            }

            public TUnit Unit { get; }

            public string UnitId { get; }

            public bool CommandsEmptyBefore { get; }

            public bool RawAiBefore { get; }

            public bool EffectiveAiBefore { get; }

            public bool CommandsEmptyDuring { get; internal set; }

            public bool RawAiDuring { get; internal set; }

            public bool EffectiveAiDuring { get; internal set; }

            public bool CommandsEmptyAfter { get; internal set; }

            public bool RawAiAfter { get; internal set; }

            public bool EffectiveAiAfter { get; internal set; }
        }
    }
}
