using System;
using System.Collections.Generic;

namespace KingmakerMountedCombat.Domain
{
    public enum HorseStrikeHandleOutcome
    {
        NotExact,
        AlreadyRecorded,
        Adopted,
        Refreshed,
        Supplied,
        RejectedStockMismatch,
        RejectedRefreshAction,
        RejectedCandidateCount,
        RejectedCreateFailed,
        // Native planned this strike without a handle after the command already recorded one (review R4-F7): a fallback
        // would be a second registration, so the native strike is left exactly as native planned it.
        MissingAfterRecorded
    }

    // The Unity/Kingmaker operations the exact Horse Bite handle binding needs (MMR-07, review R1-F13). The production
    // implementation (Integration/HorsePrimaryAttackAnimationAdapter.HandleBoundary) reads the native handle, its action and
    // manager, the Horse's SpecialAttack actions and the pair command's telemetry record; tests supply fakes.
    public interface IHorseStrikeHandleBoundary<THandle, TAction>
        where THandle : class
        where TAction : class
    {
        // The exact KMC Horse primary context (pair mounted, live MountPrimaryNatural command on the exact Horse, the planned
        // attack and weapon of that command). Decided by the adapter before any handle is read.
        bool ExactContext { get; }

        // The planned attack's current native handle (null when native created none).
        THandle CurrentHandle { get; }

        // The pair command's record: whether this exact handle is the recorded one, and the recorded action (null if none).
        bool IsRecorded(THandle handle);
        TAction RecordedAction { get; }
        bool HasAnyRecorded { get; }

        // The handle's action when it is a SpecialAttack action, else null.
        TAction StockSpecialAction(THandle handle);

        // The handle was created by the Horse's own live animation manager.
        bool HandleUsesHorseManager(THandle handle);

        // The weapon's expected special animation type, and whether an action plays it.
        bool ExpectedTypeIsNone { get; }
        bool MatchesExpectedType(TAction action);

        // The Horse animation manager's SpecialAttack actions (empty without a live manager).
        IList<TAction> SpecialAttackActions { get; }

        THandle CreateHandle(TAction action);
        void AssignHandle(THandle handle);
        void Record(THandle handle, TAction action, string source);
        void Refresh(THandle handle, TAction action);
    }

    public struct HorseStrikeHandleBindingResult<THandle, TAction>
        where THandle : class
        where TAction : class
    {
        public HorseStrikeHandleOutcome Outcome;
        public THandle Handle;
        public TAction Action;
        public int CandidateCount;
    }

    // Adopt the stock-created native SpecialAttack handle of the exact KMC Horse primary strike, refresh the record to a later
    // stock handle of the same action, or (only when native created none) supply exactly one handle of the single matching
    // SpecialAttack action. A handle is recorded at most once; anything inexact is rejected and nothing is assigned. Once a
    // handle is recorded for the command, a later strike without a handle is never given a fallback (no creation, assignment
    // or second record): the binding cannot attribute a new handle to that record, so native's strike stays intact. This
    // decides presentation telemetry only: the native UnitAttack keeps execution, rules, timing and damage.
    public static class HorseStrikeHandleBinding
    {
        public const string StockCreated = "stock-created";
        public const string KmcSupplied = "kmc-supplied";

        public static HorseStrikeHandleBindingResult<THandle, TAction> Bind<THandle, TAction>(IHorseStrikeHandleBoundary<THandle, TAction> boundary)
            where THandle : class
            where TAction : class
        {
            if (boundary == null) throw new ArgumentNullException(nameof(boundary));
            var result = new HorseStrikeHandleBindingResult<THandle, TAction> { Outcome = HorseStrikeHandleOutcome.NotExact };
            if (!boundary.ExactContext) return result;
            var current = boundary.CurrentHandle;
            if (current != null)
            {
                result.Handle = current;
                if (boundary.IsRecorded(current))
                {
                    result.Outcome = HorseStrikeHandleOutcome.AlreadyRecorded;
                    return result;
                }
                var stock = boundary.StockSpecialAction(current);
                result.Action = stock;
                if (!boundary.HandleUsesHorseManager(current) || stock == null || boundary.ExpectedTypeIsNone ||
                    !boundary.MatchesExpectedType(stock) || !Contains(boundary.SpecialAttackActions, stock))
                {
                    result.Outcome = HorseStrikeHandleOutcome.RejectedStockMismatch;
                    return result;
                }
                if (boundary.HasAnyRecorded)
                {
                    if (!ReferenceEquals(boundary.RecordedAction, stock))
                    {
                        result.Outcome = HorseStrikeHandleOutcome.RejectedRefreshAction;
                        return result;
                    }
                    boundary.Refresh(current, stock);
                    result.Outcome = HorseStrikeHandleOutcome.Refreshed;
                    return result;
                }
                boundary.Record(current, stock, StockCreated);
                result.Outcome = HorseStrikeHandleOutcome.Adopted;
                return result;
            }

            // Review R4-F7: checked before any creation or assignment.
            if (boundary.HasAnyRecorded)
            {
                result.Outcome = HorseStrikeHandleOutcome.MissingAfterRecorded;
                result.Action = boundary.RecordedAction;
                return result;
            }
            TAction only = null;
            var count = 0;
            var actions = boundary.SpecialAttackActions;
            if (actions != null)
            {
                foreach (var candidate in actions)
                {
                    if (candidate == null || !boundary.ExpectedTypeIsNone && !boundary.MatchesExpectedType(candidate)) continue;
                    count++;
                    only = candidate;
                }
            }
            result.CandidateCount = count;
            if (count != 1)
            {
                result.Outcome = HorseStrikeHandleOutcome.RejectedCandidateCount;
                return result;
            }
            result.Action = only;
            var handle = boundary.CreateHandle(only);
            if (handle == null)
            {
                result.Outcome = HorseStrikeHandleOutcome.RejectedCreateFailed;
                return result;
            }
            result.Handle = handle;
            boundary.AssignHandle(handle);
            boundary.Record(handle, only, KmcSupplied);
            result.Outcome = HorseStrikeHandleOutcome.Supplied;
            return result;
        }

        private static bool Contains<TAction>(IList<TAction> actions, TAction action) where TAction : class
        {
            if (actions == null) return false;
            foreach (var candidate in actions)
                if (ReferenceEquals(candidate, action)) return true;
            return false;
        }
    }

    // The planned-strike facts the Horse strike line reports, read through a boundary over the native planned attack and its
    // handle (production: MountedPairAttackCommand.HorseStrikeSource). Read only; null-safe at every step.
    public interface IHorseStrikeSource<THandle> where THandle : class
    {
        string WeaponName { get; }
        string SpecialType { get; }
        THandle Handle { get; }
        string ActionName(THandle handle);
        bool IsStarted(THandle handle);
        bool IsActed(THandle handle);
        bool IsInterrupted(THandle handle);
        float SpeedScale(THandle handle);
        int Variant(THandle handle);
        int VariantsCount(THandle handle);
        string ActiveClip(THandle handle);
        float ActiveWeight(THandle handle);
        string ActiveState(THandle handle);
    }

    public struct HorseStrikeRecord
    {
        public string Horse, Weapon, Special, Action, ActiveClip, AnimationState, Target;
        public int Index, Count, Variant, Variants;
        public bool HasHandle, Started, Acted, Interrupted;
        public float SpeedScale, ClipWeight;
        public object HandleIdentity;

        public string Format() => HorseStrikeDiagnostic.Format(Horse, Index, Count, Weapon, Special, HasHandle, Action, Started, Acted,
            Interrupted, SpeedScale, Variant, Variants, ActiveClip, ClipWeight, AnimationState, Target);
    }

    public static class HorseStrikeReader
    {
        public static HorseStrikeRecord Read<THandle>(IHorseStrikeSource<THandle> source, string horse, int index, int count, string target)
            where THandle : class
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var record = new HorseStrikeRecord
            {
                Horse = horse,
                Index = index,
                Count = count,
                Target = target,
                Weapon = source.WeaponName,
                Special = source.SpecialType
            };
            var handle = source.Handle;
            if (handle == null) return record;
            record.HasHandle = true;
            record.HandleIdentity = handle;
            record.Action = source.ActionName(handle);
            record.Started = source.IsStarted(handle);
            record.Acted = source.IsActed(handle);
            record.Interrupted = source.IsInterrupted(handle);
            record.SpeedScale = source.SpeedScale(handle);
            record.Variant = source.Variant(handle);
            record.Variants = source.VariantsCount(handle);
            record.ActiveClip = source.ActiveClip(handle);
            record.ClipWeight = source.ActiveWeight(handle);
            record.AnimationState = source.ActiveState(handle);
            return record;
        }
    }
}
