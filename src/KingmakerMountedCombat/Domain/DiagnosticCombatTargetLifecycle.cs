using System;
using System.Collections.Generic;

namespace KingmakerMountedCombat.Domain
{
    public enum DiagnosticCombatTargetState
    {
        Absent,
        Created,
        Active,
        DestroyRequested,
        Removed,
        Faulted
    }

    public sealed class DiagnosticCombatTargetLifecycle
    {
        public DiagnosticCombatTargetState State { get; private set; }

        public string TargetId { get; private set; }

        public bool BeginCreate(string targetId, bool exactWorkingAuthorized)
        {
            if (!exactWorkingAuthorized ||
                string.IsNullOrWhiteSpace(targetId) ||
                (State != DiagnosticCombatTargetState.Absent && State != DiagnosticCombatTargetState.Removed))
            {
                return false;
            }

            TargetId = targetId;
            State = DiagnosticCombatTargetState.Created;
            return true;
        }

        public bool Activate(string exactTargetId, bool allSafetyGatesPassed)
        {
            if (State != DiagnosticCombatTargetState.Created ||
                !allSafetyGatesPassed ||
                !string.Equals(TargetId, exactTargetId, StringComparison.Ordinal))
            {
                return false;
            }

            State = DiagnosticCombatTargetState.Active;
            return true;
        }

        public bool RequestDestroy(string reason)
        {
            if (State == DiagnosticCombatTargetState.Absent ||
                State == DiagnosticCombatTargetState.Removed ||
                State == DiagnosticCombatTargetState.DestroyRequested)
            {
                return false;
            }

            State = DiagnosticCombatTargetState.DestroyRequested;
            return true;
        }

        public bool ConfirmRemoved(string exactTargetId, bool zeroResidue)
        {
            if (State != DiagnosticCombatTargetState.DestroyRequested ||
                !zeroResidue ||
                !string.Equals(TargetId, exactTargetId, StringComparison.Ordinal))
            {
                return false;
            }

            State = DiagnosticCombatTargetState.Removed;
            return true;
        }

        public void Fault()
        {
            State = DiagnosticCombatTargetState.Faulted;
        }
    }

    public sealed class DiagnosticCombatTargetSafetySnapshot
    {
        private readonly string[] failedGateNames;

        public DiagnosticCombatTargetSafetySnapshot(
            bool exactBlueprint,
            bool notDirectlyControllable,
            bool notPet,
            bool noMaster,
            bool exactRuntimeFaction,
            bool dedicatedRuntimeGroup,
            bool targetTreatsRiderAsEnemy,
            bool riderTreatsTargetAsEnemy,
            bool noExperienceOnDeath,
            bool aiBackingDisabled,
            bool commandsEmpty,
            bool inventoryHasNoLoot,
            bool nativePrimaryNaturalWeaponResolvedWithoutProvisioning,
            bool primaryNaturalWeaponPresent,
            bool primaryNaturalWeaponIsNatural,
            bool primaryNaturalWeaponIsMelee)
        {
            var failures = new List<string>();
            AddFailure(failures, exactBlueprint, "exact-blueprint");
            AddFailure(failures, notDirectlyControllable, "not-directly-controllable");
            AddFailure(failures, notPet, "not-pet");
            AddFailure(failures, noMaster, "no-master");
            AddFailure(failures, exactRuntimeFaction, "exact-runtime-faction");
            AddFailure(failures, dedicatedRuntimeGroup, "dedicated-runtime-group");
            AddFailure(failures, targetTreatsRiderAsEnemy, "target-treats-rider-as-enemy");
            AddFailure(failures, riderTreatsTargetAsEnemy, "rider-treats-target-as-enemy");
            AddFailure(failures, noExperienceOnDeath, "no-experience-on-death");
            AddFailure(failures, aiBackingDisabled, "ai-backing-disabled");
            AddFailure(failures, commandsEmpty, "commands-empty");
            AddFailure(failures, inventoryHasNoLoot, "inventory-has-no-loot");
            AddFailure(failures, nativePrimaryNaturalWeaponResolvedWithoutProvisioning, "native-primary-natural-weapon-resolved-without-provisioning");
            AddFailure(failures, primaryNaturalWeaponPresent, "primary-natural-weapon-present");
            AddFailure(failures, primaryNaturalWeaponIsNatural, "primary-natural-weapon-is-natural");
            AddFailure(failures, primaryNaturalWeaponIsMelee, "primary-natural-weapon-is-melee");
            failedGateNames = failures.ToArray();
        }

        public bool AllPassed => failedGateNames.Length == 0;

        public string[] FailedGateNames => (string[])failedGateNames.Clone();

        public string FailureSummary => string.Join(",", failedGateNames);

        private static void AddFailure(List<string> failures, bool passed, string name)
        {
            if (!passed)
            {
                failures.Add(name);
            }
        }
    }
}
