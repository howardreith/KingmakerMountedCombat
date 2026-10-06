using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker;
using Kingmaker.Controllers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Parts;
using Newtonsoft.Json.Linq;

namespace KmcRemovalObserver
{
    // Raw native observations only. The external reader owns charge acceptance.
    // This assembly has no reference to the KMC product and installs no gameplay patch.
    internal sealed class ChargeAbsenceProbe : IGlobalRulebookHandler<RuleAttackWithWeapon>, IDisposable
    {
        private readonly ObserverChargeSource source;
        private readonly string[] blueprints;
        private readonly IDisposable subscription;
        private readonly JArray attacks = new JArray();
        private int dropped;
        private readonly FieldInfo processes;
        internal ChargeAbsenceProbe(ObserverChargeSource source, string[] blueprints)
        {
            this.source = source; this.blueprints = blueprints;
            processes = typeof(AbilityExecutionController).GetField("m_Abilities", BindingFlags.Instance | BindingFlags.NonPublic);
            if (processes == null || processes.MetadataToken != 0x04005D50 ||
                processes.FieldType != typeof(List<AbilityExecutionProcess>) ||
                processes.Module.ModuleVersionId != new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7"))
                throw new InvalidOperationException("Exact native ability-process observation contract changed.");
            subscription = EventBus.Subscribe(this);
        }
        private bool Pair(string id) => id == source.RiderId || id == source.MountId;
        internal bool PairCommandsSettled => new[] { source.RiderId, source.MountId }.All(id =>
        {
            var actors = Game.Instance.State.Units.Where(unit => unit.UniqueId == id).ToArray();
            return actors.Length == 1 && actors[0].Commands.Empty && actors[0].View?.AgentASP != null &&
                actors[0].View.AgentASP.Path == null && !actors[0].View.AgentASP.IsReallyMoving;
        });
        public void OnEventAboutToTrigger(RuleAttackWithWeapon evt)
        {
            if (!Pair(evt.Initiator?.UniqueId)) return;
            if (attacks.Count >= 32) { dropped++; return; }
            attacks.Add(new JObject { ["actor"] = evt.Initiator.UniqueId, ["target"] = evt.Target?.UniqueId,
                ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks, ["opportunity"] = evt.IsAttackOfOpportunity });
        }
        public void OnEventDidTrigger(RuleAttackWithWeapon evt) { }
        private static JObject Command(UnitCommand command) => command == null ? null : new JObject
        {
            ["type"] = command.GetType().FullName, ["executor"] = command.Executor?.UniqueId,
            ["finished"] = command.IsFinished, ["acted"] = command.IsActed
        };
        private JObject Actor(string id)
        {
            var matches = Game.Instance.State.Units.Where(candidate => candidate.UniqueId == id).ToArray();
            var unit = matches.Length == 1 ? matches[0] : null;
            var agent = unit?.View?.AgentASP;
            return new JObject
            {
                ["id"] = id, ["matches"] = matches.Length, ["blueprint"] = unit?.Blueprint?.AssetGuid,
                ["viewPresent"] = unit?.View != null, ["agentPresent"] = agent != null,
                ["raw"] = unit == null ? null : new JArray(unit.Commands.Raw.Where(c => c != null).Select(Command)),
                ["queue"] = unit == null ? null : new JArray(unit.Commands.Queue.Select(Command)),
                ["standard"] = Command(unit?.Commands.Standard), ["move"] = Command(unit?.Commands.Move),
                ["pathPresent"] = agent?.Path != null, ["moving"] = agent?.IsReallyMoving,
                ["mountCharging"] = agent?.IsCharging, ["speedOverride"] = agent?.MaxSpeedOverride,
                ["riderCharging"] = unit?.Descriptor?.State.IsCharging,
                ["chargeBuffs"] = unit == null ? -1 : unit.Buffs.Enumerable.Count(buff => buff.Blueprint.AssetGuid == source.ChargeBuffGuid),
                ["kmcAbilities"] = unit == null ? null : new JArray(unit.Descriptor.Abilities.Enumerable
                    .Where(ability => blueprints.Contains(ability.Blueprint.AssetGuid)).Select(ability => ability.Blueprint.AssetGuid))
            };
        }
        internal JObject Capture() => new JObject
        {
            // The game's global serializer preserves references and injects $id
            // into FromObject. Emit the exact five source facts independently of
            // those settings; the external reader still requires exact identity.
            ["source"] = source.CaptureEvidence(), ["rider"] = Actor(source.RiderId), ["mount"] = Actor(source.MountId),
            ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
            ["processes"] = new JArray(((List<AbilityExecutionProcess>)processes.GetValue(Game.Instance.AbilityExecutor))
                .Where(process => Pair(process.Context?.Caster?.UniqueId)).Select(process => new JObject
                { ["caster"] = process.Context?.Caster?.UniqueId, ["ended"] = process.IsEnded,
                    ["ability"] = process.Context?.Ability?.Blueprint?.AssetGuid })),
            ["attacks"] = attacks.DeepClone(), ["dropped"] = dropped
        };
        public void Dispose() => subscription.Dispose();
    }
}
