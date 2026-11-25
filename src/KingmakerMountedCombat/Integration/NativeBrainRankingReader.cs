using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using Kingmaker.Controllers.Brain;
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
        }

        // The same live list, as native ranked it, for the pair's equal-rank tie-break (review R1-F11). Null when unavailable.
        internal static List<Candidate> Candidates(UnitEntityData unit)
        {
            try
            {
                var context = ContextField?.GetValue(null) as DecisionContext;
                if (context == null || unit == null || !ReferenceEquals(context.Unit, unit)) return null;
                var enemies = EnemiesField?.GetValue(context) as List<TargetInfo>;
                if (enemies == null) return null;
                var result = new List<Candidate>(enemies.Count);
                foreach (var info in enemies)
                    if (info?.Unit != null) result.Add(new Candidate { Unit = info.Unit, Weight = info.Weight, Distance = info.Distance });
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
