using System;
using System.Reflection;
using Kingmaker;
using Kingmaker.Controllers.Units;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Parts;

namespace KingmakerMountedCombat.Integration
{
    // Original adapter for the installed Kingmaker condition preparation contract.
    // A replacement TickOnUnit prefix in the human mod set rejects non-current
    // actors before the native body. Use native parts, rules and command factories
    // at the reserved partner boundary without changing that prefix or CurrentTurn.
    internal static class PairedConfusionPreparation
    {
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        private static readonly MethodInfo[] Factories = {
            Factory("DoNothing", 0x06009132), Factory("SelfHarm", 0x06009133), Factory("AttackNearest", 0x06009134)
        };
        private static readonly FieldInfo Duration = typeof(UnitConfusionController).GetField("RoundDuration", Flags);

        static PairedConfusionPreparation()
        {
            if (typeof(UnitConfusionController).Module.ModuleVersionId != new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7") ||
                Duration == null || Duration.MetadataToken != 0x04005D88 || Duration.FieldType != typeof(TimeSpan))
                throw new InvalidOperationException("Paired condition preparation requires the installed native contract.");
        }

        private static MethodInfo Factory(string name, int token)
        {
            var method = typeof(UnitConfusionController).GetMethod(name, Flags, null, new[] { typeof(UnitPartConfusion) }, null);
            if (method == null || method.MetadataToken != token || method.ReturnType != typeof(UnitCommand))
                throw new MissingMethodException("Native condition command factory changed: " + name);
            return method;
        }

        internal static void Prepare(UnitEntityData actor)
        {
            var now = Game.Instance.TimeController.GameTime;
            var part = actor.Get<UnitPartConfusion>();
            if (part != null && part.RoundStartTime < now)
            {
                part.Cmd?.Interrupt();
                part.RoundStartTime = TimeSpan.Zero;
            }
            var state = actor.Descriptor.State;
            var nearest = state.HasCondition(UnitCondition.AttackNearest);
            if (!state.HasCondition(UnitCondition.Confusion) && !nearest)
            {
                actor.Remove<UnitPartConfusion>();
                return;
            }
            part = actor.Ensure<UnitPartConfusion>();
            var standardAvailable = !actor.CombatState.HasCooldownForCommand(UnitCommand.CommandType.Standard);
            if (now - part.RoundStartTime > (TimeSpan)Duration.GetValue(null) && standardAvailable)
            {
                var roll = Rulebook.Trigger(new RuleRollDice(actor, new DiceFormula(1, DiceType.D100))).Result.Value;
                var choice = nearest ? 100 : roll;
                part.State = (ConfusionState)(choice < 26 ? 0 : choice < 51 ? 1 : choice < 76 ? 2 : 3);
                if ((int)part.State == 0) part.ReleaseControl(); else part.RetainControl();
                part.RoundStartTime = now;
                part.Cmd?.Interrupt();
                part.Cmd = null;
            }
            if (part.Cmd != null || !state.CanAct || (int)part.State == 0) return;
            var factory = standardAvailable ? (int)part.State - 1 : 0;
            if (factory < 0 || factory >= Factories.Length) throw new ArgumentOutOfRangeException("Native confusion state");
            part.Cmd = (UnitCommand)Factories[factory].Invoke(null, new object[] { part });
            if (part.Cmd != null) actor.Commands.Run(part.Cmd);
        }
    }
}
