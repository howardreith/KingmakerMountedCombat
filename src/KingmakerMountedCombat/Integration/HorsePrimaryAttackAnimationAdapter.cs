using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items.Slots;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.View.Animation;
using Kingmaker.Visual.Animation.Actions;
using Kingmaker.Visual.Animation.Kingmaker;
using Kingmaker.Visual.Animation.Kingmaker.Actions;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Logging;
using UnityEngine;

namespace KingmakerMountedCombat.Integration
{
    /// <summary>
    /// Binds the exact native Horse Bite SpecialAttack handle for KMC telemetry. The
    /// stock UnitAttack normally creates that handle while initializing the
    /// Horse's Bite; a narrow fallback supplies it only when stock returns no
    /// handle. This adapter changes animation presentation only; the stock
    /// UnitAttack remains responsible for execution, rules, timing, and damage.
    /// </summary>
    internal sealed class HorsePrimaryAttackAnimationAdapter
    {
        private readonly GameMountedRelationshipService relationship;
        private readonly IModLogger logger;

        internal HorsePrimaryAttackAnimationAdapter(
            GameMountedRelationshipService relationship,
            IModLogger logger)
        {
            this.relationship = relationship ?? throw new ArgumentNullException(nameof(relationship));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        internal int HandleCreateCount { get; private set; }

        internal int HandleAdoptCount { get; private set; }

        internal int HandleRefreshCount { get; private set; }

        internal int HandleRejectCount { get; private set; }

        // Review R4-F7: strikes left without a fallback because a handle was already recorded, and binding faults contained.
        internal int HandleMissingAfterRecordedCount { get; private set; }

        internal int HandleFaultCount { get; private set; }

        // PR-5 review R1-F7: no command is retained here; the once-per-command missing-handle report is the command's own flag.
        private bool faultReported;

        internal string LastHandleSource { get; private set; } = "<none>";

        internal string LastActionName { get; private set; } = "<none>";

        internal string LastActionType { get; private set; } = "<none>";

        // MMR-07 (review R1-F13): the decision is the production HorseStrikeHandleBinding over the real-type HandleBoundary
        // below. PR-5 review R1-F2: building the boundary, the decision and the outcome reporting (metadata reads and logging)
        // all run inside HorseStrikeHandleBinding.SupplyContained, and a failing fault report is absorbed there as well, so
        // nothing reaches the native handle creation (the CreateAnimationHandleForAttack postfix and the command-start path).
        internal void SupplyExact(
            MountedPairAttackCommand command,
            AttackHandInfo attack,
            UnitEntityData horse)
        {
            HorseStrikeHandleBinding.SupplyContained<UnitAnimationActionHandle, UnitAnimationActionSpecialAttack>(
                () => CreateBoundary(command, attack, horse), result => Report(result), ReportFault);
        }

        private HandleBoundary CreateBoundary(MountedPairAttackCommand command, AttackHandInfo attack, UnitEntityData horse)
        {
            var exact = IsExactHorsePrimaryContext(command, attack, horse) && IsHorseBiteSlot(horse, attack);
            var manager = exact ? horse.View?.AnimationManager : null;
            return new HandleBoundary(
                exact,
                exact ? attack.AnimationHandle : null,
                manager,
                manager != null,
                manager?.ActionSet.OfType<UnitAnimationActionSpecialAttack>().Where(candidate => candidate != null),
                handle => command.HasRecordedHorsePrimaryAnimation(handle),
                () => command.RecordedHorsePrimaryAnimationAction,
                () => command.HasAnyRecordedHorsePrimaryAnimation,
                (handle, action, source) => command.RecordHorsePrimaryAnimation(handle, action, source),
                (handle, action) => command.RefreshStockCreatedHorsePrimaryAnimation(handle, action),
                action => CreateNativeHandle(manager, action),
                handle => attack.AnimationHandle = handle,
                () => command.HorsePrimaryMissingHandleReported,
                () => command.MarkHorsePrimaryMissingHandleReported());
        }

        private void Report(HorseStrikeHandleBindingResult<UnitAnimationActionHandle, UnitAnimationActionSpecialAttack> result)
        {
            switch (result.Outcome)
            {
                case HorseStrikeHandleOutcome.MissingAfterRecorded:
                    HandleMissingAfterRecordedCount++;
                    if (result.FirstMissingReport)
                    {
                        logger.Info("Horse strike planned without a native handle after an earlier recorded Bite handle: no fallback " +
                            "supplied and nothing recorded (native strike unchanged); recorded action=" + (result.Action?.name ?? "<none>") + ".");
                    }
                    return;
                case HorseStrikeHandleOutcome.Adopted:
                    HandleAdoptCount++;
                    Remember(HorseStrikeHandleBinding.StockCreated, result.Action);
                    logger.Info("Adopted the stock-created native Horse SpecialAttack handle for the exact KMC Bite: action=" +
                        LastActionName + "; variant=" + result.Handle.Variant + ".");
                    return;
                case HorseStrikeHandleOutcome.Refreshed:
                    HandleRefreshCount++;
                    Remember(HorseStrikeHandleBinding.StockCreated, result.Action);
                    logger.Info("Refreshed Horse primary telemetry to the later exact stock-created Bite handle: action=" +
                        LastActionName + "; variant=" + result.Handle.Variant + ".");
                    return;
                case HorseStrikeHandleOutcome.Supplied:
                    HandleCreateCount++;
                    Remember(HorseStrikeHandleBinding.KmcSupplied, result.Action);
                    logger.Info("Supplied one native Horse SpecialAttack handle for the exact KMC Bite: action=" +
                        LastActionName + "; variant=" + result.Handle.Variant + ".");
                    return;
                case HorseStrikeHandleOutcome.RejectedStockMismatch:
                    HandleRejectCount++;
                    logger.Error("Exact KMC Horse Bite animation rejected: the stock handle was not the exact native Horse Bite SpecialAttack action.");
                    return;
                case HorseStrikeHandleOutcome.RejectedRefreshAction:
                    HandleRejectCount++;
                    logger.Error("Exact KMC Horse Bite animation rejected: a later stock handle played a different SpecialAttack action than the recorded one.");
                    return;
                case HorseStrikeHandleOutcome.RejectedCandidateCount:
                    HandleRejectCount++;
                    logger.Error("Exact KMC Horse Bite animation rejected: expected one native Bite SpecialAttack action, observed " +
                        result.CandidateCount + ".");
                    return;
                case HorseStrikeHandleOutcome.RejectedCreateFailed:
                    HandleRejectCount++;
                    logger.Error("Exact KMC Horse Bite animation rejected: native SpecialAttack handle creation returned null.");
                    return;
            }
        }

        // Presentation telemetry never faults the native strike; the first fault is logged (a failing logger is absorbed by
        // SupplyContained).
        private void ReportFault(Exception exception)
        {
            HandleFaultCount++;
            if (faultReported) return;
            faultReported = true;
            logger.Exception("Exact KMC Horse Bite animation binding failed; the native strike is left as planned", exception);
        }

        private void Remember(string source, UnitAnimationActionSpecialAttack action)
        {
            LastHandleSource = source;
            LastActionName = action.name ?? "<unnamed>";
            LastActionType = action.Type.ToString();
        }

        private static UnitAnimationActionHandle CreateNativeHandle(UnitAnimationManager manager, UnitAnimationActionSpecialAttack action)
        {
            var handle = manager == null ? null : manager.CreateHandle(action) as UnitAnimationActionHandle;
            if (handle == null) return null;
            handle.SpecialAttackCount = 0;
            if (handle.VariantsCount > 0)
            {
                handle.Variant = Math.Abs(Time.frameCount) % handle.VariantsCount;
            }
            return handle;
        }

        // PR-5 reviews R1-F1/R2-F1: whether a planned strike is the KMC Horse's Bite, proven by the registered weapon's identity
        // and its actual slot, never by its presentation tag: the unit's blueprint is the exact KMC Horse blueprint, whose first
        // limb HorseCompanionBlueprintService registered as the stock Bite1d4 and verified by GUID (CopyBody builds bite, hoof,
        // hoof); the planned weapon is that registered blueprint and the planned hand is the Horse's runtime first additional
        // limb. A hoof (limb1, limb2), a hand, a foreign weapon or an unrelated untyped weapon is not. A registered Bite whose
        // visual SpecialAnimation is None is still the Bite.
        // PR-5 review R3-F5: the planned weapon must also carry the registration-time Bite identity (the GUID
        // HorseCompanionBlueprintService registered, RegisteredBiteGuid), so a foreign weapon substituted into both the blueprint's
        // first limb and the runtime slot is never the Bite. Binding stays fail-closed: only Bite binds.
        internal static bool IsHorseBiteSlot(UnitEntityData horse, AttackHandInfo attack) => ClassifyHorseStrike(horse, attack) == HorseBiteClass.Bite;

        // PR-5 review R2-F2 (round 4): Unavailable when the proof's inputs are missing (not a positive non-Bite).
        internal static HorseBiteClass ClassifyHorseStrike(UnitEntityData horse, AttackHandInfo attack)
        {
            var blueprint = horse?.Blueprint;
            if (ReferenceEquals(blueprint, null) || attack == null) return HorseBiteClass.Unavailable;
            if (!string.Equals(blueprint.AssetGuid, HorseCompanionBlueprintService.UnitGuid, StringComparison.Ordinal)) return HorseBiteClass.NotBite;
            return ClassifyBiteLimb(attack.Hand, attack.Weapon?.Blueprint, horse.Body?.AdditionalLimbs, blueprint.Body?.AdditionalLimbs,
                HorseCompanionBlueprintService.RegisteredBiteGuid);
        }

        // Reference tests only (a blueprint is never a destroyed Unity object), so genuine objects are judged the same inside and
        // outside Unity. blueprintLimbs is the registered Horse body's limbs; registeredBiteGuid the registration-time Bite identity.
        internal static HorseBiteClass ClassifyBiteLimb(WeaponSlot hand, BlueprintItemWeapon planned, IList<WeaponSlot> runtimeLimbs,
            BlueprintItemWeapon[] blueprintLimbs, string registeredBiteGuid)
        {
            // PR-5 review R2-F2 (round 5): a missing required first-slot reference (the runtime first limb or the registered body's
            // first limb) or a missing planned weapon identity is unavailable evidence, not a readable mismatch; NotBite is reserved for a
            // readable, confirmed non-Bite.
            if (hand == null || ReferenceEquals(planned, null) || runtimeLimbs == null || runtimeLimbs.Count == 0 || blueprintLimbs == null ||
                blueprintLimbs.Length == 0 || string.IsNullOrEmpty(registeredBiteGuid) || runtimeLimbs[0] == null ||
                ReferenceEquals(blueprintLimbs[0], null) || string.IsNullOrEmpty(planned.AssetGuid))
                return HorseBiteClass.Unavailable;
            return ReferenceEquals(runtimeLimbs[0], hand) && ReferenceEquals(blueprintLimbs[0], planned) &&
                string.Equals(planned.AssetGuid, registeredBiteGuid, StringComparison.Ordinal)
                ? HorseBiteClass.Bite : HorseBiteClass.NotBite;
        }

        internal static bool IsBiteLimb(WeaponSlot hand, BlueprintItemWeapon planned, IList<WeaponSlot> runtimeLimbs, BlueprintItemWeapon[] blueprintLimbs,
            string registeredBiteGuid) =>
            ClassifyBiteLimb(hand, planned, runtimeLimbs, blueprintLimbs, registeredBiteGuid) == HorseBiteClass.Bite;

        // The real-type boundary of HorseStrikeHandleBinding. Its classification members read only the native handle's Action
        // and Manager references and the action's AttackType (no Unity engine calls), so the contract
        // (scripts/Test-MountedAlpha2BiteContract.ps1) drives it with genuine Kingmaker objects. Manager liveness, the action
        // set, handle creation, the command record and the command's missing-handle flag are supplied by the adapter.
        internal sealed class HandleBoundary : IHorseStrikeHandleBoundary<UnitAnimationActionHandle, UnitAnimationActionSpecialAttack>
        {
            private readonly bool exact;
            private readonly UnitAnimationActionHandle current;
            private readonly object horseManager;
            private readonly bool managerAlive;
            private readonly List<UnitAnimationActionSpecialAttack> actions;
            private readonly Func<UnitAnimationActionHandle, bool> isRecorded;
            private readonly Func<UnitAnimationActionSpecialAttack> recordedAction;
            private readonly Func<bool> hasAnyRecorded;
            private readonly Action<UnitAnimationActionHandle, UnitAnimationActionSpecialAttack, string> record;
            private readonly Action<UnitAnimationActionHandle, UnitAnimationActionSpecialAttack> refresh;
            private readonly Func<UnitAnimationActionSpecialAttack, UnitAnimationActionHandle> create;
            private readonly Action<UnitAnimationActionHandle> assign;
            private readonly Func<bool> missingReported;
            private readonly Action markMissingReported;

            internal HandleBoundary(
                bool exact,
                UnitAnimationActionHandle current,
                object horseManager,
                bool managerAlive,
                IEnumerable<UnitAnimationActionSpecialAttack> actions,
                Func<UnitAnimationActionHandle, bool> isRecorded,
                Func<UnitAnimationActionSpecialAttack> recordedAction,
                Func<bool> hasAnyRecorded,
                Action<UnitAnimationActionHandle, UnitAnimationActionSpecialAttack, string> record,
                Action<UnitAnimationActionHandle, UnitAnimationActionSpecialAttack> refresh,
                Func<UnitAnimationActionSpecialAttack, UnitAnimationActionHandle> create,
                Action<UnitAnimationActionHandle> assign,
                Func<bool> missingReported,
                Action markMissingReported)
            {
                this.exact = exact;
                this.current = current;
                this.horseManager = horseManager;
                this.managerAlive = managerAlive;
                this.actions = actions == null ? new List<UnitAnimationActionSpecialAttack>() : actions.ToList();
                this.isRecorded = isRecorded ?? throw new ArgumentNullException(nameof(isRecorded));
                this.recordedAction = recordedAction ?? throw new ArgumentNullException(nameof(recordedAction));
                this.hasAnyRecorded = hasAnyRecorded ?? throw new ArgumentNullException(nameof(hasAnyRecorded));
                this.record = record ?? throw new ArgumentNullException(nameof(record));
                this.refresh = refresh ?? throw new ArgumentNullException(nameof(refresh));
                this.create = create ?? throw new ArgumentNullException(nameof(create));
                this.assign = assign ?? throw new ArgumentNullException(nameof(assign));
                this.missingReported = missingReported ?? throw new ArgumentNullException(nameof(missingReported));
                this.markMissingReported = markMissingReported ?? throw new ArgumentNullException(nameof(markMissingReported));
            }

            public bool ExactContext => exact;
            public UnitAnimationActionHandle CurrentHandle => current;
            public bool IsRecorded(UnitAnimationActionHandle handle) => isRecorded(handle);
            public UnitAnimationActionSpecialAttack RecordedAction => recordedAction();
            public bool HasAnyRecorded => hasAnyRecorded();
            public UnitAnimationActionSpecialAttack StockSpecialAction(UnitAnimationActionHandle handle) =>
                ((AnimationActionHandle)handle).Action as UnitAnimationActionSpecialAttack;
            public bool HandleUsesHorseManager(UnitAnimationActionHandle handle) =>
                managerAlive && !ReferenceEquals(horseManager, null) && ReferenceEquals(((AnimationActionHandle)handle).Manager, horseManager);
            public bool IsBiteAction(UnitAnimationActionSpecialAttack action) => action.AttackType == UnitAnimationSpecialAttackType.Bite;
            public IList<UnitAnimationActionSpecialAttack> SpecialAttackActions => actions;
            public UnitAnimationActionHandle CreateHandle(UnitAnimationActionSpecialAttack action) => create(action);
            public void AssignHandle(UnitAnimationActionHandle handle) => assign(handle);
            public void Record(UnitAnimationActionHandle handle, UnitAnimationActionSpecialAttack action, string source) => record(handle, action, source);
            public void Refresh(UnitAnimationActionHandle handle, UnitAnimationActionSpecialAttack action) => refresh(handle, action);
            public bool MissingReported => missingReported();
            public void MarkMissingReported() => markMissingReported();
        }

        private bool IsExactHorsePrimaryContext(
            MountedPairAttackCommand command,
            AttackHandInfo attack,
            UnitEntityData horse)
        {
            return relationship.State == RelationshipState.Mounted &&
                command != null && !command.IsFinished &&
                command.Action == MountedCombatActionKind.MountPrimaryNatural &&
                horse != null && ReferenceEquals(horse, relationship.Mount) &&
                string.Equals(horse.Blueprint?.AssetGuid,
                    HorseCompanionBlueprintService.UnitGuid, StringComparison.Ordinal) &&
                command.ActionActor == horse && command.ChildAttack != null &&
                ReferenceEquals(command.ChildAttack.PlannedAttack, attack) &&
                attack?.Hand?.Owner?.Unit == horse &&
                ReferenceEquals(attack.Weapon, command.ChildAttack.PlannedAttack.Weapon);
        }
    }
}
