using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.Group;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    // One ordinary native selector removal. The scenario supplies exact isolated
    // world/save custody, including a cleanup guard that tolerates this declared
    // transition. No party list, callback, entity state or archive is edited.
    internal sealed class Chunk7NativePartyFieldDeparture
    {
        private readonly UnitEntityData actor;
        private readonly string[] originalParty;
        private readonly Action guard, cleanupGuard;
        private GroupManager group;
        private bool started, owned, switchAttempted, goRequested, completed;
        private readonly JObject evidence = new JObject();
        internal bool Completed => completed;
        internal bool CommitRequested => goRequested;
        internal JObject Capture() => (JObject)evidence.DeepClone();

        internal Chunk7NativePartyFieldDeparture(UnitEntityData actor, string[] originalParty, Action guard, Action cleanupGuard)
        {
            this.actor = actor ?? throw new ArgumentNullException(nameof(actor));
            this.originalParty = originalParty == null ? throw new ArgumentNullException(nameof(originalParty)) : (string[])originalParty.Clone();
            this.guard = guard ?? throw new ArgumentNullException(nameof(guard));
            this.cleanupGuard = cleanupGuard ?? throw new ArgumentNullException(nameof(cleanupGuard));
        }

        internal void Open()
        {
            guard();
            Require(!started && !owned && !completed, "The exact departure selector may be opened only once.");
            Require(originalParty.Length == 6 && originalParty.All(id => !string.IsNullOrEmpty(id)) &&
                originalParty.Distinct(StringComparer.Ordinal).Count() == 6 && originalParty.Contains(actor.UniqueId) &&
                SameIds(Game.Instance.Player.PartyCharacters) &&
                Game.Instance.Player.PartyCharacters.Count(value => ReferenceEquals(value.Value, actor)) == 1 &&
                !Game.Instance.Player.RemoteCompanions.Any(value => ReferenceEquals(value.Value, actor)) &&
                !ReferenceEquals(Game.Instance.Player.MainCharacter.Value, actor) &&
                actor.Blueprint.GetComponent<LockedCompanionComponent>() == null &&
                actor.Descriptor.Pet == null && actor.Descriptor.Master.Value == null,
                "Departure requires the exact six-character field and one optional petless non-main character.");
            group = Game.Instance.UI.Canvas.GroupManager;
            Require(group != null && !Active(), "A pre-existing party selector cannot be borrowed for departure.");
            started = true; owned = true; // Retain a native partial-open failure.
            new ShowPartySelection().RunAction();
            Require(Current() && Active(), "Native departure selector did not open as the owned instance.");
            evidence["contract"] = "installed-native-single-field-departure-v1";
            evidence["action"] = "native-ShowPartySelection";
            evidence["actorId"] = actor.UniqueId;
            evidence["originalPartyCharacterIds"] = new JArray(originalParty);
            evidence["openCount"] = 1;
            evidence["switchCount"] = 0;
            evidence["goCount"] = 0;
            evidence["manualPartyMutation"] = false;
        }

        internal bool Tick()
        {
            guard();
            Require(started && owned && Current(), "Native departure selector custody was replaced.");
            if (!goRequested)
            {
                Require(!switchAttempted && Active(), "A failed/partial departure input cannot be retried.");
                var alpha = Alpha();
                Require(!float.IsNaN(alpha) && !float.IsInfinity(alpha) && alpha >= 0 && alpha <= 1,
                    "Native departure selector alpha is invalid.");
                if (!group.gameObject.activeInHierarchy || alpha < 1) return false;
                var selected = Selection();
                Require(SameIds(selected) && selected.Count(value => ReferenceEquals(value.Value, actor)) == 1 &&
                    SameIds(Game.Instance.Player.PartyCharacters), "Native departure selection or live party changed before input.");
                var reference = selected.Single(value => ReferenceEquals(value.Value, actor));
                var required = group.MustBeInParty(reference);
                evidence["mustBeInParty"] = required;
                Require(!required, "The opened native selector requires this character; departure is refused.");
                evidence["readyAlpha"] = alpha;
                evidence["readyActiveInHierarchy"] = true;
                switchAttempted = true; evidence["switchCount"] = 1;
                group.SwitchCharacter(reference);
                selected = Selection();
                Require(selected.Count == 5 && selected.All(value => value.Value != null && !ReferenceEquals(value.Value, actor)) &&
                    originalParty.Where(id => id != actor.UniqueId).All(id => selected.Count(value => value.Value.UniqueId == id) == 1),
                    "Native selector did not accept exactly the declared removal.");
                evidence["selectedPartyCharacterIds"] = new JArray(selected.Select(value => value.Value.UniqueId));
                guard(); goRequested = true; evidence["goCount"] = 1;
                group.Go();
            }
            if (Active()) return false;
            Require(Current() && owned, "Native departure selector changed at closure.");
            var party = Game.Instance.Player.PartyCharacters;
            Require(party.Count == 5 && !party.Any(value => ReferenceEquals(value.Value, actor)) &&
                originalParty.Where(id => id != actor.UniqueId).All(id => party.Count(value => value.Value?.UniqueId == id) == 1) &&
                Game.Instance.Player.RemoteCompanions.Count(value => ReferenceEquals(value.Value, actor)) == 1,
                "Native departure did not settle to five retained characters and the exact remote actor.");
            evidence["closedExactInstance"] = true;
            evidence["closedActiveSelf"] = false;
            evidence["remoteActorId"] = actor.UniqueId;
            evidence["settledPartyCharacterIds"] = new JArray(party.Select(value => value.Value.UniqueId));
            evidence["nativeDepartureProven"] = true;
            owned = false; completed = true; return true;
        }

        internal bool TryDrain()
        {
            if (!started) return true;
            cleanupGuard();
            Require(Current(), "Departure selector was replaced; cleanup custody remains retained.");
            if (!owned)
            {
                Require(!Active(), "Settled departure selector was reopened; cleanup custody remains retained.");
                return true;
            }
            if (Active()) group.Close();
            if (Active()) return false;
            owned = false; return true;
        }

        private bool SameIds(IEnumerable<UnitReference> values)
        {
            var rows = values.ToArray();
            return rows.Length == originalParty.Length &&
                originalParty.All(id => rows.Count(value => value.Value?.UniqueId == id) == 1);
        }
        private bool Current() => ReferenceEquals(Game.Instance.UI.Canvas.GroupManager, group);
        private bool Active() => group != null && group.gameObject.activeSelf;
        private List<UnitReference> Selection()
        {
            var field = typeof(GroupManagerBase).GetField("PartyCharacter", BindingFlags.Instance | BindingFlags.NonPublic);
            Require(field?.MetadataToken == 0x04002B1C && field.FieldType == typeof(List<UnitReference>), "Native party list contract differs.");
            return (List<UnitReference>)field.GetValue(group);
        }
        private float Alpha()
        {
            var getter = typeof(GroupManagerBase).GetProperty("CanvasGroup", BindingFlags.Instance | BindingFlags.NonPublic)?.GetGetMethod(true);
            Require(getter?.MetadataToken == 0x06003F4C && getter.ReturnType.FullName == "UnityEngine.CanvasGroup", "Native selector presentation contract differs.");
            var canvas = getter.Invoke(group, null); var alpha = getter.ReturnType.GetProperty("alpha");
            Require(canvas != null && alpha?.PropertyType == typeof(float), "Native selector presentation unavailable.");
            return (float)alpha.GetValue(canvas, null);
        }
        private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
    }
}
