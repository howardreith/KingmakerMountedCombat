using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Integration
{
    internal static class NativeCombatTurnPersistence
    {
        private static FieldInfo F(string name, int token, Type type) =>
            NativeCombatActorPersistence.Field(typeof(CombatController), name, token, type);
        private static readonly FieldInfo Current = F("m_CurrentTurn", 0x0400064A, typeof(TurnController));
        private static readonly FieldInfo HasEnemy = F("m_HasEnemyInCombat", 0x0400064B, typeof(bool));
        private static readonly FieldInfo Surprise = F("m_HasSurpriseRound", 0x0400064C, typeof(bool));
        private static readonly FieldInfo UnitsChanged = F("m_IsUnitsChanged", 0x0400064D, typeof(bool));
        private static readonly FieldInfo Start = F("m_StartTime", 0x0400064E, typeof(TimeSpan));
        private static readonly FieldInfo Units = F("m_Units", 0x0400064F, typeof(List<CombatController.TBUnitInfo>));
        private static readonly FieldInfo Known = F("m_KnownInitiative", 0x04000650, typeof(Dictionary<string, int>));
        private static readonly FieldInfo Next = F("m_NextUnit", 0x04000652, typeof(UnitEntityData));
        private static readonly FieldInfo Selection = F("m_BeforeCombatSelectedUnit", 0x04000655, typeof(UnitEntityData));
        private static readonly FieldInfo HadEnemy = F("<HadEnemyAtSomePoint>k__BackingField", 0x0400065A, typeof(bool));
        private static readonly FieldInfo Initialized = F("<Initialized>k__BackingField", 0x0400065B, typeof(bool));
        private static readonly FieldInfo Round = F("<RoundNumber>k__BackingField", 0x0400065C, typeof(int));
        private static readonly FieldInfo Since = F("<TimeSinceStart>k__BackingField", 0x0400065E, typeof(float));
        private static readonly FieldInfo Until = F("<TimeToNextRound>k__BackingField", 0x0400065F, typeof(float));
        private static readonly FieldInfo RoundStart = F("<RoundStartTime>k__BackingField", 0x04000662, typeof(TimeSpan));
        private static readonly FieldInfo TurnStart = F("<TurnStartTime>k__BackingField", 0x04000663, typeof(TimeSpan));
        private static readonly FieldInfo ActingSurprise = NativeCombatActorPersistence.Field(
            typeof(CombatController.TBUnitInfo), "ActingInSurpriseRound", 0x0400706F, typeof(bool));
        private static readonly MethodInfo Delay = Method("HandleDelayTurn", 0x06000BE8, typeof(UnitEntityData), typeof(UnitEntityData));
        private static readonly MethodInfo Revert = Method("RevertNavmeshForUnit", 0x06000BB9, typeof(CombatController.TBUnitInfo));
        private static readonly MethodInfo GridTags = Method("UpdateNavigationGridTags", 0x06000BBA);

        internal static IEnumerable<UnitEntityData> ReferencedActors(CombatController controller)
        {
            var known = (Dictionary<string, int>)Known.GetValue(controller);
            return Game.Instance.State.Units.Where(u => u.IsInCombat || known.ContainsKey(u.UniqueId))
                .Concat(((List<CombatController.TBUnitInfo>)Units.GetValue(controller)).Select(u => u.Unit))
                .Concat(new[] { (UnitEntityData)Selection.GetValue(controller) }).Where(u => u != null).Distinct();
        }

        internal static SavedCombatData Capture(CombatController controller, UnitEntityData[] actors)
        {
            var tb = CombatController.IsInTurnBasedCombat();
            if (!tb) return new SavedCombatData
            {
                TurnBased = false, Actors = actors.Select(NativeCombatActorPersistence.Capture).ToArray(),
                Engagements = NativeCombatActorPersistence.CaptureEngagements(actors),
                Roster = new SavedRosterActor[0]
            };
            var roster = (List<CombatController.TBUnitInfo>)Units.GetValue(controller);
            return new SavedCombatData
            {
                TurnBased = true, Round = controller.RoundNumber, StartTicks = ((TimeSpan)Start.GetValue(controller)).Ticks,
                RoundStartTicks = controller.RoundStartTime.Ticks, TurnStartTicks = controller.TurnStartTime.Ticks,
                TimeSinceStart = controller.TimeSinceStart, TimeToNextRound = controller.TimeToNextRound,
                HasSurpriseRound = (bool)Surprise.GetValue(controller), HasEnemy = (bool)HasEnemy.GetValue(controller),
                HadEnemy = controller.HadEnemyAtSomePoint, NextActor = ((UnitEntityData)Next.GetValue(controller))?.UniqueId,
                BeforeCombatSelected = ((UnitEntityData)Selection.GetValue(controller))?.UniqueId,
                Actors = actors.Select(NativeCombatActorPersistence.Capture).ToArray(),
                Engagements = NativeCombatActorPersistence.CaptureEngagements(actors),
                Roster = roster.Select(u => new SavedRosterActor {
                    ActorId = u.Unit.UniqueId, InitiativeProcessed = u.InitiativeProcessed,
                    Surprising = u.Surprising, Surprised = u.Surprised, ActingSurprise = (bool)ActingSurprise.GetValue(u),
                    InitiativeOverride = u.InitiativeOverride, Sequence = u.InitiativeSequence }).ToArray(),
                Current = NativeTurnPersistence.Capture(controller.CurrentTurn)
            };
        }

        internal static void Restore(CombatController controller, SavedCombatData saved,
            IDictionary<string, UnitEntityData> actors)
        {
            if (!saved.TurnBased)
            {
                if (CombatController.IsInTurnBasedCombat() || controller.Initialized)
                    throw new InvalidOperationException("Real-time restoration cannot replace active turn-based input.");
                return; // RT has no TurnController, roster or paired activation to prepare.
            }
            if (!controller.Initialized)
                throw new InvalidOperationException("Native turn input must be initialized before saved combat rebind.");
            var restoredTurn = NativeTurnPersistence.Restore(saved.Current, actors);
            try
            {
                if (restoredTurn != null)
                    restoredTurn.OnDelay += (Action<UnitEntityData, UnitEntityData>)Delegate.CreateDelegate(
                        typeof(Action<UnitEntityData, UnitEntityData>), controller, Delay);
                foreach (var old in (List<CombatController.TBUnitInfo>)Units.GetValue(controller))
                    Revert.Invoke(controller, new object[] { old });
                controller.CurrentTurn?.Dispose();
                Current.SetValue(controller, restoredTurn);
                var roster = saved.Roster.Select(row => {
                    var info = new CombatController.TBUnitInfo {
                        Unit = actors[row.ActorId], InitiativeProcessed = row.InitiativeProcessed,
                        Surprising = row.Surprising, Surprised = row.Surprised,
                        InitiativeOverride = row.InitiativeOverride, InitiativeSequence = row.Sequence };
                    ActingSurprise.SetValue(info, row.ActingSurprise);
                    return info;
                }).ToList();
                Units.SetValue(controller, roster);
                var known = (Dictionary<string, int>)Known.GetValue(controller);
                known.Clear();
                foreach (var actor in saved.Actors)
                    if (actor.CachedInitiative.HasValue) known.Add(actor.Native.Id, actor.CachedInitiative.Value);
                Next.SetValue(controller, saved.NextActor == null ? null : actors[saved.NextActor]);
                Selection.SetValue(controller, saved.BeforeCombatSelected == null ? null : actors[saved.BeforeCombatSelected]);
                HasEnemy.SetValue(controller, saved.HasEnemy); HadEnemy.SetValue(controller, saved.HadEnemy);
                Surprise.SetValue(controller, saved.HasSurpriseRound); Initialized.SetValue(controller, true);
                Start.SetValue(controller, TimeSpan.FromTicks(saved.StartTicks));
                RoundStart.SetValue(controller, TimeSpan.FromTicks(saved.RoundStartTicks));
                TurnStart.SetValue(controller, TimeSpan.FromTicks(saved.TurnStartTicks));
                Round.SetValue(controller, saved.Round); Since.SetValue(controller, saved.TimeSinceStart);
                Until.SetValue(controller, saved.TimeToNextRound); UnitsChanged.SetValue(controller, false);
                // These are disposable pathfinding caches, rebuilt from the loaded
                // native positions. Do not call StartTurn: it ticks buffs and Prepare.
                if (restoredTurn != null && !restoredTurn.Unit.View.AgentASP.AvoidanceDisabled)
                    foreach (var info in roster.Where(i => i.Unit != restoredTurn.Unit))
                    {
                        var width = info.Unit.View.AgentASP.AvoidanceDisabled ? 0f : info.Unit.Corpulence * 2f;
                        info.NavmeshUpdateObjectOn.bounds = new Bounds(info.Unit.Position,
                            width == 0f ? Vector3.zero : new Vector3(width, 10f, width));
                        AstarPath.active.UpdateGraphs(info.NavmeshUpdateObjectOn);
                        info.NavMeshBlocked = true;
                    }
                GridTags.Invoke(controller, null);
                restoredTurn = null; // Native controller now owns disposal.
            }
            finally
            {
                if (restoredTurn != null)
                {
                    if (ReferenceEquals(controller.CurrentTurn, restoredTurn)) Current.SetValue(controller, null);
                    restoredTurn.Dispose();
                }
            }
        }

        private static MethodInfo Method(string name, int token, params Type[] args)
        {
            var method = typeof(CombatController).GetMethod(name,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, args, null);
            if (method == null || method.MetadataToken != token)
                throw new MissingMethodException("Native combat restoration method differs: " + name);
            return method;
        }
    }
}
