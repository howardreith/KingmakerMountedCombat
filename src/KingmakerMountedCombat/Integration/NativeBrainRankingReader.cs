using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using Kingmaker.Controllers.Brain;
using Kingmaker.Controllers.Brain.Blueprints;
using Kingmaker.Designers;
using Kingmaker.EntitySystem.Entities;

namespace KingmakerMountedCombat.Integration
{
    // MMR-04 (review R1-F11), read only. An automatic attack proposal reaches KMC inside native AiBrainController.SelectAction
    // (its UnitCommands.Run), while the brain's static decision context still holds the candidate list it just walked:
    // DecisionContext.m_Enemies, already sorted by TargetInfo.Sort, with each candidate's distance and FillWeight, and the
    // winning score. This renders that native ranking for the exact proposal; it never calls a native getter that could
    // rebuild or re-sort the list, and it changes nothing.
    internal static class NativeBrainRankingReader
    {
        internal const int MaximumCandidates = 6;
        private static readonly FieldInfo ContextField =
            typeof(AiBrainController).GetField("s_Context", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly FieldInfo EnemiesField =
            typeof(DecisionContext).GetField("m_Enemies", BindingFlags.Instance | BindingFlags.NonPublic);

        internal struct Candidate
        {
            public UnitEntityData Unit;
            public float Weight;
            public float Distance;
            // PR-1 review R8-F1: the native facts BlueprintAiAction.IsTargetValid (0x0600942A) reads for this candidate under the proposal's
            // own action: DecisionContext.ActionRangeWithCorpulence (0x06009416: the action's GetRange plus both bodies' corpulence) and
            // TargetInfo.InLos (0x06009422, native's cached GameHelper.CheckLOS). InLos is read only where IsTargetValid reads it (the
            // unit's AiMovementForbidden, or the candidate in AIData.UnreachableUnits), so no line-of-sight probe native would not make.
            public float ActionRangeWithCorpulence;
            public bool InLos;
            // The proposal action's own range (BlueprintAiAction.GetRange), kept for the retained order's later recheck (R9-F1).
            public float ActionRange;
        }

        // PR-1 review R9-F1: native's line-of-sight test (GameHelper.CheckLOS(UnitEntityData, UnitEntityData), 0x06008555, the test
        // TargetInfo.InLos caches) for a retained automatic order's drive-time recheck of IsTargetValid's restricted rule.
        internal static bool NativeLos(UnitEntityData unit, UnitEntityData target) =>
            unit != null && target != null && GameHelper.CheckLOS(unit, target);

        // The same live list, as native ranked it, for the pair's equal-rank tie-break (review R1-F11), with the native admission facts for
        // the proposal's own AI action (BlueprintAiAction.GetRange reads only the deciding unit: BlueprintAiAttack 0x06009437). Null when
        // unavailable.
        internal static List<Candidate> Candidates(UnitEntityData unit, BlueprintAiAction action)
        {
            try
            {
                var context = ContextField?.GetValue(null) as DecisionContext;
                if (context == null || unit == null || !ReferenceEquals(context.Unit, unit)) return null;
                var enemies = EnemiesField?.GetValue(context) as List<TargetInfo>;
                if (enemies == null) return null;
                var range = action != null ? action.GetRange(context) : context.ActionRange;
                var unitBody = unit.View != null ? unit.View.Corpulence : 0f;
                var unreachable = unit.CombatState?.AIData?.UnreachableUnits;
                var result = new List<Candidate>(enemies.Count);
                foreach (var info in enemies)
                {
                    if (info?.Unit == null) continue;
                    var sightRead = unit.AiMovementForbidden || unreachable != null && unreachable.Contains(info.Unit);
                    var reach = range + unitBody + (info.Unit.View != null ? info.Unit.View.Corpulence : 0f);
                    // PR-1 review R9-F4: IsTargetValid tests range before sight, so a candidate already beyond its action range is not
                    // sight-tested.
                    result.Add(new Candidate
                    {
                        Unit = info.Unit, Weight = info.Weight, Distance = info.Distance, ActionRangeWithCorpulence = reach, ActionRange = range,
                        InLos = sightRead && info.Distance <= reach && info.InLos(context)
                    });
                }
                return result;
            }
            catch (Exception)
            {
                return null;
            }
        }

        internal static string Describe(UnitEntityData unit, UnitEntityData proposed)
        {
            try
            {
                var context = ContextField?.GetValue(null) as DecisionContext;
                if (context == null || unit == null || !ReferenceEquals(context.Unit, unit)) return "<unavailable>";
                var enemies = EnemiesField?.GetValue(context) as List<TargetInfo>;
                if (enemies == null) return "<unavailable>";
                var text = new StringBuilder("[");
                for (var index = 0; index < enemies.Count && index < MaximumCandidates; index++)
                {
                    var candidate = enemies[index];
                    if (index > 0) text.Append(',');
                    if (candidate?.Unit != null && ReferenceEquals(candidate.Unit, proposed)) text.Append('*');
                    text.Append(candidate?.Unit?.UniqueId ?? "<none>").Append(':')
                        .Append((candidate?.Distance ?? 0f).ToString("0.##", CultureInfo.InvariantCulture)).Append(':')
                        .Append((candidate?.Weight ?? 0f).ToString("0.####", CultureInfo.InvariantCulture));
                }
                if (enemies.Count > MaximumCandidates) text.Append(",+").Append(enemies.Count - MaximumCandidates);
                return text.Append("];bestScore=").Append(context.BestScore.ToString("0.####", CultureInfo.InvariantCulture)).ToString();
            }
            catch (Exception exception)
            {
                return "<unreadable:" + exception.GetType().Name + ">";
            }
        }
    }
}
