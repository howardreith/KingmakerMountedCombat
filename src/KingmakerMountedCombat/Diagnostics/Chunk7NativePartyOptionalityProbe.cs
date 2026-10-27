using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.Group;
using Kingmaker.Blueprints;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    // One owned native selector, opened solely to ask the installed GroupManager
    // whether an exact field character is required. It never switches or commits
    // the party. The caller owns the isolated world and zero-write authority.
    internal sealed class Chunk7NativePartyOptionalityProbe
    {
        private readonly UnitEntityData target;
        private readonly string[] expectedCharacters;
        private readonly Action guard;
        private readonly Action cleanupGuard;
        private GroupManager group;
        private bool opened, owned, observed, closed;
        private readonly JObject evidence = new JObject();

        internal Chunk7NativePartyOptionalityProbe(UnitEntityData target, string[] expectedCharacters, Action guard, Action cleanupGuard)
        {
            this.target = target ?? throw new ArgumentNullException(nameof(target));
            this.expectedCharacters = expectedCharacters ?? throw new ArgumentNullException(nameof(expectedCharacters));
            this.guard = guard ?? throw new ArgumentNullException(nameof(guard));
            this.cleanupGuard = cleanupGuard ?? throw new ArgumentNullException(nameof(cleanupGuard));
        }

        internal JObject Capture() => (JObject)evidence.DeepClone();
        internal bool Closed => closed;

        internal void Open()
        {
            guard();
            Require(!opened && !owned && !closed && expectedCharacters.Length == 5 &&
                expectedCharacters.Distinct(StringComparer.Ordinal).Count() == 5 &&
                expectedCharacters.Contains(target.UniqueId) &&
                Game.Instance.Player.PartyCharacters.Select(value => value.Value.UniqueId).SequenceEqual(expectedCharacters) &&
                Game.Instance.Player.RemoteCompanions.Count() == 0,
                "Optionality probe requires the exact five-character field before opening.");
            group = Game.Instance.UI?.Canvas?.GroupManager;
            Require(group != null && !group.gameObject.activeSelf,
                "Optionality probe cannot borrow an existing party selector.");
            opened = true; owned = true; // A fallible native open still belongs to us.
            new ShowPartySelection().RunAction();
            Require(Current() && group.gameObject.activeSelf,
                "The owned native party selector did not open.");
            evidence["contract"] = "installed-native-open-selector-optionality-v1";
            evidence["actorId"] = target.UniqueId;
            evidence["action"] = "native-ShowPartySelection";
            evidence["openCount"] = 1;
            evidence["switchCount"] = 0;
            evidence["goCount"] = 0;
        }

        internal bool Tick()
        {
            guard();
            Require(opened && owned && Current() && group.gameObject.activeSelf,
                "Optionality probe lost its owned open selector.");
            if (!observed)
            {
                var alpha = Alpha();
                Require(!float.IsNaN(alpha) && !float.IsInfinity(alpha) && alpha >= 0 && alpha <= 1,
                    "Native selector alpha is invalid.");
                if (!group.gameObject.activeInHierarchy || alpha < 1) return false;
                var selected = Selection();
                Require(selected.Count == expectedCharacters.Length &&
                    selected.Select(value => value.Value?.UniqueId).SequenceEqual(expectedCharacters) &&
                    selected.Count(value => ReferenceEquals(value.Value, target)) == 1,
                    "Opened native selector does not contain the exact five-character field.");
                var reference = selected.Single(value => ReferenceEquals(value.Value, target));
                var main = ReferenceEquals(target, Game.Instance.Player.MainCharacter.Value);
                var locked = target.Blueprint.GetComponent<LockedCompanionComponent>() != null;
                var required = group.MustBeInParty(reference);
                evidence["observedFrame"] = UnityEngine.Time.frameCount;
                evidence["readyAlpha"] = alpha;
                evidence["activeInHierarchy"] = true;
                evidence["selectedActorIds"] = new JArray(expectedCharacters);
                evidence["mainCharacter"] = main;
                evidence["lockedCompanion"] = locked;
                evidence["mustBeInParty"] = required;
                evidence["requiredByOpenedSelector"] = required && !main && !locked;
                evidence["eligibleForNativeRemoval"] = !required;
                observed = true;
            }
            // Close, never Go: the actual Player party is not reinitialized.
            group.Close();
            if (group.gameObject.activeSelf) return false;
            Require(Current(), "Native selector instance changed during Close.");
            owned = false; closed = true;
            evidence["closeCount"] = 1;
            evidence["closed"] = true;
            guard();
            return true;
        }

        internal bool TryDrain()
        {
            if (!owned) return !opened || Current() && !group.gameObject.activeSelf;
            cleanupGuard();
            Require(Current(), "Optionality probe selector was replaced; cleanup custody remains retained.");
            if (group.gameObject.activeSelf) group.Close();
            if (group.gameObject.activeSelf) return false;
            owned = false;
            if (observed && !closed)
            {
                closed = true;
                evidence["closeCount"] = 1;
                evidence["closed"] = true;
            }
            return true;
        }

        private bool Current() => ReferenceEquals(Game.Instance.UI?.Canvas?.GroupManager, group);
        private List<UnitReference> Selection()
        {
            var field = typeof(GroupManagerBase).GetField("PartyCharacter", BindingFlags.Instance | BindingFlags.NonPublic);
            Require(field?.MetadataToken == 0x04002B1C && field.FieldType == typeof(List<UnitReference>),
                "Installed native selector party-list contract differs.");
            return (List<UnitReference>)field.GetValue(group);
        }
        private float Alpha()
        {
            var getter = typeof(GroupManagerBase).GetProperty("CanvasGroup", BindingFlags.Instance | BindingFlags.NonPublic)?.GetGetMethod(true);
            Require(getter?.MetadataToken == 0x06003F4C && getter.ReturnType.FullName == "UnityEngine.CanvasGroup",
                "Installed native selector presentation contract differs.");
            var canvas = getter.Invoke(group, null);
            var alpha = getter.ReturnType.GetProperty("alpha");
            Require(canvas != null && alpha?.PropertyType == typeof(float), "Native selector presentation unavailable.");
            return (float)alpha.GetValue(canvas, null);
        }
        private static void Require(bool ok, string reason) { if (!ok) throw new InvalidOperationException(reason); }
    }
}
