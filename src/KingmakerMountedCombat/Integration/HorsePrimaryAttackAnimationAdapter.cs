using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.EntitySystem.Entities;
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
    /// Binds the exact native Horse SpecialAttack handle for KMC telemetry. The
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

        private object lastMissingCommand;
        private bool faultReported;

        internal string LastHandleSource { get; private set; } = "<none>";

        internal string LastActionName { get; private set; } = "<none>";

        internal string LastActionType { get; private set; } = "<none>";

        // MMR-07 (review R1-F13): the decision is the production HorseStrikeHandleBinding over the real-type HandleBoundary
        // below; this method only reports its outcome. Counters and log lines are unchanged; a later stock handle of a
        // different action is now a reported rejection instead of an exception thrown out of the native postfix.
        internal void SupplyExact(
            MountedPairAttackCommand command,
            AttackHandInfo attack,
            UnitEntityData horse)
        {
            // The context test and the boundary's construction (weapon visual parameters, the manager's action set) are inside
            // the same containment as the decision: this runs inside native handle creation, and nothing here may fault it.
            HorseStrikeHandleBindingResult<UnitAnimationActionHandle, UnitAnimationActionSpecialAttack> result;
            try
            {
                var exact = IsExactHorsePrimaryContext(command, attack, horse);
                var manager = exact ? horse.View?.AnimationManager : null;
                var boundary = new HandleBoundary(
                    exact,
                    exact ? attack.AnimationHandle : null,
                    manager,
                    manager != null,
                    exact ? attack.Weapon.Blueprint.VisualParameters.SpecialAnimation : UnitAnimationSpecialAttackType.None,
                    manager?.ActionSet.OfType<UnitAnimationActionSpecialAttack>().Where(candidate => candidate != null),
                    handle => command.HasRecordedHorsePrimaryAnimation(handle),
                    () => command.RecordedHorsePrimaryAnimationAction,
                    () => command.HasAnyRecordedHorsePrimaryAnimation,
                    (handle, action, source) => command.RecordHorsePrimaryAnimation(handle, action, source),
                    (handle, action) => command.RefreshStockCreatedHorsePrimaryAnimation(handle, action),
                    action => CreateNativeHandle(manager, action),
                    handle => attack.AnimationHandle = handle);
                result = HorseStrikeHandleBinding.Bind(boundary);
            }
            catch (Exception exception)
            {
                // Presentation telemetry never faults the native strike (this runs inside native handle creation).
                HandleFaultCount++;
                if (!faultReported) logger.Exception("Exact KMC Horse primary animation binding failed; the native strike is left as planned", exception);
                faultReported = true;
                return;
            }
            switch (result.Outcome)
            {
                case HorseStrikeHandleOutcome.MissingAfterRecorded:
                    HandleMissingAfterRecordedCount++;
                    if (!ReferenceEquals(lastMissingCommand, command))
                    {
                        lastMissingCommand = command;
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
                    logger.Error("Exact KMC Horse primary animation rejected: the stock Bite handle was not the exact native Horse SpecialAttack action.");
                    return;
                case HorseStrikeHandleOutcome.RejectedRefreshAction:
                    HandleRejectCount++;
                    logger.Error("Exact KMC Horse primary animation rejected: a later stock handle played a different SpecialAttack action than the recorded one.");
                    return;
                case HorseStrikeHandleOutcome.RejectedCandidateCount:
                    HandleRejectCount++;
                    logger.Error("Exact KMC Horse primary animation rejected: expected one native SpecialAttack action, observed " +
                        result.CandidateCount + ".");
                    return;
                case HorseStrikeHandleOutcome.RejectedCreateFailed:
                    HandleRejectCount++;
                    logger.Error("Exact KMC Horse primary animation rejected: native SpecialAttack handle creation returned null.");
                    return;
            }
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

        // The real-type boundary of HorseStrikeHandleBinding. Its classification members read only the native handle's Action
        // and Manager references and the action's AttackType (no Unity engine calls), so the contract
        // (scripts/Test-MountedAlpha2BiteContract.ps1) drives it with genuine Kingmaker objects. Manager liveness, the action
        // set, handle creation and the command record are supplied by the adapter.
        internal sealed class HandleBoundary : IHorseStrikeHandleBoundary<UnitAnimationActionHandle, UnitAnimationActionSpecialAttack>
        {
            private readonly bool exact;
            private readonly UnitAnimationActionHandle current;
            private readonly object horseManager;
            private readonly bool managerAlive;
            private readonly UnitAnimationSpecialAttackType expected;
            private readonly List<UnitAnimationActionSpecialAttack> actions;
            private readonly Func<UnitAnimationActionHandle, bool> isRecorded;
            private readonly Func<UnitAnimationActionSpecialAttack> recordedAction;
            private readonly Func<bool> hasAnyRecorded;
            private readonly Action<UnitAnimationActionHandle, UnitAnimationActionSpecialAttack, string> record;
            private readonly Action<UnitAnimationActionHandle, UnitAnimationActionSpecialAttack> refresh;
            private readonly Func<UnitAnimationActionSpecialAttack, UnitAnimationActionHandle> create;
            private readonly Action<UnitAnimationActionHandle> assign;

            internal HandleBoundary(
                bool exact,
                UnitAnimationActionHandle current,
                object horseManager,
                bool managerAlive,
                UnitAnimationSpecialAttackType expected,
                IEnumerable<UnitAnimationActionSpecialAttack> actions,
                Func<UnitAnimationActionHandle, bool> isRecorded,
                Func<UnitAnimationActionSpecialAttack> recordedAction,
                Func<bool> hasAnyRecorded,
                Action<UnitAnimationActionHandle, UnitAnimationActionSpecialAttack, string> record,
                Action<UnitAnimationActionHandle, UnitAnimationActionSpecialAttack> refresh,
                Func<UnitAnimationActionSpecialAttack, UnitAnimationActionHandle> create,
                Action<UnitAnimationActionHandle> assign)
            {
                this.exact = exact;
                this.current = current;
                this.horseManager = horseManager;
                this.managerAlive = managerAlive;
                this.expected = expected;
                this.actions = actions == null ? new List<UnitAnimationActionSpecialAttack>() : actions.ToList();
                this.isRecorded = isRecorded ?? throw new ArgumentNullException(nameof(isRecorded));
                this.recordedAction = recordedAction ?? throw new ArgumentNullException(nameof(recordedAction));
                this.hasAnyRecorded = hasAnyRecorded ?? throw new ArgumentNullException(nameof(hasAnyRecorded));
                this.record = record ?? throw new ArgumentNullException(nameof(record));
                this.refresh = refresh ?? throw new ArgumentNullException(nameof(refresh));
                this.create = create ?? throw new ArgumentNullException(nameof(create));
                this.assign = assign ?? throw new ArgumentNullException(nameof(assign));
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
            public bool ExpectedTypeIsNone => expected == UnitAnimationSpecialAttackType.None;
            public bool MatchesExpectedType(UnitAnimationActionSpecialAttack action) => action.AttackType == expected;
            public IList<UnitAnimationActionSpecialAttack> SpecialAttackActions => actions;
            public UnitAnimationActionHandle CreateHandle(UnitAnimationActionSpecialAttack action) => create(action);
            public void AssignHandle(UnitAnimationActionHandle handle) => assign(handle);
            public void Record(UnitAnimationActionHandle handle, UnitAnimationActionSpecialAttack action, string source) => record(handle, action, source);
            public void Refresh(UnitAnimationActionHandle handle, UnitAnimationActionSpecialAttack action) => refresh(handle, action);
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
