using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.View;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Authored Kingmaker Beast Shape I supplies both view changes. This fixture
    // never assigns View, invokes a lifecycle subscriber, or fabricates a turn.
    internal sealed class NativeChargeViewLease : IUnitViewAttachedUIHandler, IDisposable
    {
        internal const string BlueprintId = "00d8fbe9cf61dc24298be8d95500c84b";
        private const string PrefabId = "0dc0f602a83a2034ba5842f73c0012c1";
        private static readonly string[] ComponentTypes = {
            "Kingmaker.UnitLogic.Buffs.Polymorph", "Kingmaker.Blueprints.Classes.Spells.SpellDescriptorComponent",
            "Kingmaker.Designers.Mechanics.Buffs.BuffMovementSpeed", "Kingmaker.Designers.Mechanics.Buffs.ReplaceAsksList",
            "Kingmaker.Designers.Mechanics.Facts.ReplaceSourceBone" };
        // Installed COTW CleanUp.fixPolymorphSizeChangesStacking adds one native
        // rule listener for each of these exact eight size buffs. No foreign
        // assembly reference or mutation is needed to observe their loaded form.
        private static readonly string[] SizeImmunityIds = {
            "4f139d125bb602f48bfaec3d3e1937cb", "b0793973c61a19744a8630468e8f4174",
            "c84fbb4414925f344b894e9511626296", "17206974f2a2c164db26d1af7fac57d5",
            "3fca5d38053677044a7ffd9a872d3a0a", "4ce640f9800d444418779a214598d0a3",
            "6ba82f2c8a7146e6b4880cbe7f8534e8", "c5d35ba066ae4a079a7d86a316d3ef38" };

        private static bool MatchesLoadedComponents(BlueprintComponent[] values)
        {
            if (values == null || values.Any(item => ReferenceEquals(item, null))) return false;
            return values.Select(item => item.GetType().FullName).OrderBy(item => item, StringComparer.Ordinal)
                .SequenceEqual(ComponentTypes.Concat(Enumerable.Repeat(typeof(SpecificBuffImmunity).FullName, 8))
                    .OrderBy(item => item, StringComparer.Ordinal)) &&
                values.OfType<SpecificBuffImmunity>().Select(item => ReferenceEquals(item.Buff, null) ? null : item.Buff.AssetGuid)
                    .OrderBy(item => item, StringComparer.Ordinal)
                    .SequenceEqual(SizeImmunityIds.OrderBy(item => item, StringComparer.Ordinal));
        }
        private readonly UnitEntityData actor;
        private readonly BlueprintBuff blueprint;
        private readonly UnitEntityView originalView;
        private readonly JObject baseline;
        private readonly IDisposable subscription;
        private readonly List<GameLogicComponent> components = new List<GameLogicComponent>();
        private readonly JArray attachments = new JArray();
        private readonly JObject evidence;
        private UnitEntityView replacementView;
        private Buff buff;
        private bool attempted, removalAttempted, removalReturned, restored;

        internal NativeChargeViewLease(UnitEntityData actor)
        {
            this.actor = actor;
            blueprint = ResourcesLibrary.TryGetBlueprint<BlueprintBuff>(BlueprintId);
            if (actor?.View == null || actor.GetActivePolymorph() != null || actor.Body.IsPolymorphed ||
                actor.Descriptor.OverrideAsks != null || actor.Descriptor.ReplaceBlueprintForInspection != null ||
                blueprint == null || blueprint.name != "BeastShapeIBuff" ||
                !MatchesLoadedComponents(blueprint.ComponentsArray) ||
                blueprint.GetComponent<Polymorph>().Prefab.AssetId != PrefabId ||
                actor.Buffs.Enumerable.Any(item => item.Blueprint == blueprint))
                throw new InvalidOperationException("Charge view fixture requires the exact unowned loaded Beast Shape I and stock rider: " +
                    new JObject { ["view"] = actor?.View != null, ["polymorph"] = actor?.GetActivePolymorph() != null,
                        ["bodyPolymorphed"] = actor?.Body?.IsPolymorphed, ["asksOverride"] = actor?.Descriptor?.OverrideAsks != null,
                        ["inspectionOverride"] = actor?.Descriptor?.ReplaceBlueprintForInspection != null,
                        ["name"] = blueprint == null ? null : blueprint.name,
                        ["prefab"] = blueprint?.GetComponent<Polymorph>()?.Prefab?.AssetId,
                        ["components"] = blueprint == null ? null : new JArray(blueprint.ComponentsArray.Select(item => item?.GetType().FullName))
                    }.ToString(Newtonsoft.Json.Formatting.None));
            originalView = actor.View;
            baseline = CaptureEffects();
            evidence = new JObject { ["actor"] = actor.UniqueId, ["blueprint"] = BlueprintId,
                ["name"] = blueprint.name, ["prefab"] = PrefabId,
                ["components"] = new JArray(blueprint.ComponentsArray.Select(item => item.GetType().FullName)),
                ["sizeImmunities"] = new JArray(blueprint.GetComponents<SpecificBuffImmunity>().Select(item => item.Buff.AssetGuid)),
                ["before"] = Capture(), ["effectsBefore"] = baseline.DeepClone() };
            subscription = EventBus.Subscribe(this);
        }

        internal void Apply()
        {
            if (attempted) throw new InvalidOperationException("Charge view fixture cannot repeat its native effect.");
            attempted = true;
            evidence["applyCalls"] = 1;
            try { buff = actor.Buffs.AddBuff(blueprint, actor, TimeSpan.FromMinutes(10)); }
            finally
            {
                // AddFact may insert before a callback throws. Preserve the exact
                // new fact so root cleanup can still remove this owned stimulus.
                if (buff == null) buff = actor.Buffs.Enumerable.SingleOrDefault(item => item.Blueprint == blueprint);
                CaptureComponents();
                replacementView = actor.View;
                evidence["afterApply"] = Capture();
            }
        }

        public void HandleUnitViewAttached(UnitEntityData unit)
        {
            if (unit != actor) return;
            var source = new JArray();
            foreach (var frame in new System.Diagnostics.StackTrace(false).GetFrames() ?? new System.Diagnostics.StackFrame[0])
            {
                var method = frame.GetMethod();
                if (method?.Module != typeof(UnitEntityData).Module) continue;
                var token = method.MetadataToken;
                if (token != 0x06002A08 && token != 0x06002A09 && token != 0x06007E9D && token != 0x0600835C) continue;
                source.Add(new JObject { ["token"] = token.ToString("x8"), ["method"] = method.Name,
                    ["assemblyMvid"] = method.Module.ModuleVersionId.ToString("D") });
            }
            if (attachments.Count >= 8) throw new InvalidOperationException("Unexpected repeated native view attachments.");
            attachments.Add(new JObject { ["frame"] = Time.frameCount, ["actor"] = actor.UniqueId,
                ["view"] = actor.View == null ? 0 : actor.View.GetInstanceID(), ["nativeSource"] = source });
        }

        private void CaptureComponents()
        {
            if (buff == null) return;
            var native = typeof(Buff).Module.ResolveField(0x0400695A).GetValue(buff) as IEnumerable;
            if (native != null) foreach (GameLogicComponent component in native)
                if (!components.Any(item => ReferenceEquals(item, component))) components.Add(component);
        }

        private JObject CaptureEffects()
        {
            var visual = actor.Get<UnitPartVisualChanges>();
            return new JObject { ["size"] = (int)actor.Descriptor.State.Size, ["speed"] = actor.CombatSpeedMps,
                ["stats"] = new JArray(actor.Stats.GetList().Select(item => item.ModifiedValue)),
                ["facts"] = new JArray(actor.Logic.Enumerable.Select(item => item.Blueprint.AssetGuid).OrderBy(item => item, StringComparer.Ordinal)),
                ["bodyPolymorphed"] = actor.Body.IsPolymorphed,
                ["inspectionOverride"] = actor.Descriptor.ReplaceBlueprintForInspection != null,
                ["asksOverride"] = actor.Descriptor.OverrideAsks != null,
                ["sourceBones"] = new JArray(visual == null ? new string[0] : visual.SourceBone.ToArray()),
                ["boneReplaced"] = visual?.BoneReplaced ?? false, ["boneDefault"] = visual?.BoneDefault ?? false,
                ["handSet"] = actor.Body.CurrentHandEquipmentSetIndex,
                ["equipment"] = new JArray(actor.Body.HandsEquipmentSets.Select(set => new JObject {
                    ["primary"] = CaptureItem(set.PrimaryHand.MaybeItem), ["secondary"] = CaptureItem(set.SecondaryHand.MaybeItem) })),
                ["limbs"] = new JArray(actor.Body.AdditionalLimbs.Select(slot => CaptureItem(slot.MaybeItem))),
                ["stockLimbs"] = new JArray(actor.Body.NotPolymorphedAdditionalLimbs.Select(slot => CaptureItem(slot.MaybeItem))),
                ["polymorphHands"] = typeof(UnitEntityData).Module.ResolveField(0x04005014).GetValue(actor.Body) != null,
                ["polymorphLimbs"] = typeof(UnitEntityData).Module.ResolveField(0x04005015).GetValue(actor.Body) != null };
        }

        private static JObject CaptureItem(ItemEntity item) => item == null ? null : new JObject {
            ["identity"] = RuntimeHelpers.GetHashCode(item), ["blueprint"] = item.Blueprint.AssetGuid };

        private JObject Capture() => new JObject { ["frame"] = Time.frameCount,
            ["view"] = actor.View == null ? 0 : actor.View.GetInstanceID(),
            ["bound"] = actor.View != null && actor.View.Data == actor,
            ["polymorph"] = actor.GetActivePolymorph() != null,
            ["buffCount"] = actor.Buffs.Enumerable.Count(item => item.Blueprint == blueprint),
            ["effects"] = CaptureEffects() };

        internal bool RestoreWhenSettled()
        {
            if (restored) return true;
            if (attempted && !removalAttempted)
            {
                CaptureComponents(); removalAttempted = true; evidence["removeCalls"] = 1;
                // Never replay a throwing native removal after it unlinks the fact.
                buff?.Remove(); removalReturned = true;
            }
            evidence["restorationProgress"] = Capture();
            if (attempted && (!removalReturned || buff == null || !buff.IsDisposed || buff.Active ||
                components.Any(item => item.IsListeningEvents) || originalView != null || replacementView != null)) return false;
            if (actor.View == null || actor.View.Data != actor || actor.GetActivePolymorph() != null ||
                actor.AreHandsBusyWithAnimation || !actor.Commands.Empty || !JToken.DeepEquals(baseline, CaptureEffects())) return false;
            restored = true;
            evidence["afterRestore"] = Capture(); evidence["restored"] = true;
            evidence["originalRetired"] = originalView == null; evidence["replacementRetired"] = replacementView == null;
            evidence["factDisposed"] = buff?.IsDisposed ?? !attempted;
            evidence["listenersRemaining"] = components.Count(item => item.IsListeningEvents);
            subscription.Dispose();
            return true;
        }

        internal JObject Evidence { get { evidence["attachments"] = attachments.DeepClone(); return evidence; } }
        public void Dispose()
        {
            if (!RestoreWhenSettled()) throw new InvalidOperationException("Native charge view fixture restoration remains owned.");
        }
    }
}
