using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.Group;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    // Owns only one native selector. The caller supplies the exact world/save/
    // worker guard before every operation; no party list or callback is edited.
    internal sealed class Chunk7NativePartyFieldAdmission
    {
        private readonly UnitEntityData actor;
        private readonly string[] originalParty;
        private readonly Action guard;
        private GroupManager group;
        private bool started, owned, goRequested, completed;
        private readonly JObject evidence = new JObject();
        internal bool Completed => completed;
        internal bool Owned => owned;
        internal JObject Capture() => (JObject)evidence.DeepClone();
        internal Chunk7NativePartyFieldAdmission(UnitEntityData actor, string[] originalParty, Action guard)
        { this.actor = actor ?? throw new ArgumentNullException(nameof(actor)); this.originalParty = originalParty ?? throw new ArgumentNullException(nameof(originalParty)); this.guard = guard ?? throw new ArgumentNullException(nameof(guard)); }

        internal void Open()
        {
            guard();
            if (started) throw new InvalidOperationException("The exact native selector may be opened only once.");
            Require(Game.Instance.Player.RemoteCompanions.Count(value => ReferenceEquals(value.Value, actor)) == 1 &&
                !Game.Instance.Player.PartyCharacters.Any(value => ReferenceEquals(value.Value, actor)), "Exact actor is not remote before field admission.");
            group = Game.Instance.UI.Canvas.GroupManager;
            Require(group != null && !Active(), "A pre-existing party selector cannot be borrowed.");
            started = true; owned = true; // ownership precedes a fallible native open
            new ShowPartySelection().RunAction();
            Require(Current() && Active(), "Native selector did not open as the owned instance.");
            evidence["action"] = "native-ShowPartySelection"; evidence["actorId"] = actor.UniqueId;
            evidence["nativeCap"] = 6; evidence["openCount"] = 1; evidence["openActiveSelf"] = true;
            evidence["manualPartyMutation"] = false;
        }
        internal bool Tick()
        {
            guard();
            Require(started && owned && Current(), "Native selector custody was replaced.");
            if (!goRequested)
            {
                Require(Active(), "Native selector closed before its declared Go.");
                var alpha = Alpha();
                Require(!float.IsNaN(alpha) && !float.IsInfinity(alpha) && alpha >= 0 && alpha <= 1, "Native selector alpha is invalid.");
                if (!group.gameObject.activeInHierarchy || alpha < 1) return false;
                evidence["readyAlpha"] = alpha; evidence["readyActiveInHierarchy"] = true;
                var remote = Game.Instance.Player.RemoteCompanions.SingleOrDefault(value => ReferenceEquals(value.Value, actor));
                var selected = Selection();
                Require(remote != null && selected.Count < 6 && selected.All(value => !ReferenceEquals(value.Value, actor)) &&
                    selected.Count == originalParty.Length && originalParty.All(id => selected.Count(value => value.Value?.UniqueId == id) == 1),
                    "Native party capacity/membership changed before field selection.");
                // A thrown native click ends progression; callers enter cleanup.
                group.SwitchCharacter(remote);
                selected = Selection();
                Require(selected.Count == originalParty.Length + 1 && selected.Count <= 6 && selected.Count(value => ReferenceEquals(value.Value, actor)) == 1 &&
                    originalParty.All(id => selected.Count(value => value.Value?.UniqueId == id) == 1), "Native selector did not accept the one exact addition.");
                guard(); goRequested = true; evidence["switchCount"] = 1; evidence["goCount"] = 1;
                group.Go();
            }
            if (Active()) return false;
            Require(Current() && owned, "Native selector changed at closure.");
            evidence["closedActiveSelf"] = false; evidence["closedExactInstance"] = true;
            owned = false; completed = true; return true;
        }
        internal bool TryDrain()
        {
            if (!owned)
            {
                if (started) { guard(); Require(Current() && !Active(), "Completed selector was replaced or reopened; cleanup custody remains retained."); }
                return true;
            }
            guard(); Require(Current(), "Owned selector was replaced; cleanup custody remains retained.");
            if (Active()) group.Close();
            if (Active()) return false;
            owned = false; return true;
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
