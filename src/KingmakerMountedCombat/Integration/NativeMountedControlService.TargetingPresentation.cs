using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Kingmaker;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.UnitSettings;
using Kingmaker.UnitLogic.Abilities;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Integration
{
    // Known issues Sprint 7 (U02): while a hidden delivery ability owns native target selection, the exact visible public
    // slot whose click handed it over is drawn selected. Presentation only: the slot keeps its public AbilityData (the
    // click restores it), the saved row is untouched, and native target selection, its delivery, cancellation and every
    // action cost stay native. One mapping at most; it holds only weak references and value identities.
    internal sealed partial class NativeMountedControlService
    {
        private sealed class SerialBox { internal long Value; }
        private readonly ConditionalWeakTable<object, SerialBox> presentationSerials = new ConditionalWeakTable<object, SerialBox>();
        private long nextPresentationSerial;
        // Every native target-selection start or end (any ability) is a new operation.
        private long targetingOperation;
        private MountedTargetingSlotIdentity targetingMapping;
        // The exact slot object of the mapping, weakly, so a destroyed slot is never kept alive.
        private WeakReference targetingSlot;
        internal int TargetingPresentationClearCount { get; private set; }
        internal string LastTargetingPresentationClear { get; private set; }
        internal bool HasTargetingPresentation => targetingMapping != null;
        // The engine boundaries this presentation reads. A detached contract replaces them; production reads the game.
        private Func<ClickWithSelectedAbilityHandler> selectedAbilityHandler = () => Game.Instance?.SelectedAbilityHandler;
        private Func<UnitEntityData, AbilityData, bool> abilityFactLive = (unit, data) =>
        {
            var fact = data?.Blueprint == null ? null : unit?.Descriptor?.Abilities?.GetAbility(data.Blueprint);
            return fact != null && fact.Active && ReferenceEquals(fact.Data, data);
        };

        private long PresentationSerial(object value)
        {
            if (value == null) return 0;
            return presentationSerials.GetValue(value, key => new SerialBox { Value = ++nextPresentationSerial }).Value;
        }

        // Called from the target-selection start and end handlers before anything else: whatever ability it concerns,
        // the previous selection operation is over.
        private void NoteTargetSelectionOperation(string reason)
        {
            targetingOperation++;
            ClearTargetingPresentation(reason);
        }

        internal void ClearTargetingPresentation(string reason)
        {
            if (targetingMapping == null && targetingSlot == null) return;
            targetingMapping = null;
            targetingSlot = null;
            TargetingPresentationClearCount++;
            LastTargetingPresentationClear = reason;
        }

        // The click on a public slot begins: any earlier mapping belongs to a selection this click replaces.
        internal void BeginPublicSlotClick() => ClearTargetingPresentation("public-slot-click");

        // The click returned normally (Harmony 1 runs no postfix after an exception, so a throwing click records nothing):
        // the slot has its public identity back, and native selection may now hold exactly the delivery it swapped in.
        internal void RecordPublicSlotTargeting(MechanicActionBarSlotAbility slot, AbilityData publicAbility, AbilityData delivery)
        {
            var selected = selectedAbilityHandler()?.Ability;
            var record = MountedTargetingGlowPolicy.MayRecord(delivery != null, slot != null && ReferenceEquals(slot.Ability, publicAbility),
                delivery != null && ReferenceEquals(selected, delivery), !disposed && enabled && !serializationSuspended && !areaSuspended);
            if (!record) return;
            targetingMapping = LiveTargetingIdentity(slot, delivery);
            targetingSlot = new WeakReference(slot);
        }

        // Whether this exact slot is drawn selected now. Reads every identity afresh; a changed identity of the mapped
        // slot itself, an ended operation or a disabled service removes the mapping.
        internal bool PresentsTargetingSelection(MechanicActionBarSlotAbility slot)
        {
            var recorded = targetingMapping;
            if (recorded == null || slot == null) return false;
            if (disposed || !enabled || serializationSuspended || areaSuspended)
            {
                ClearTargetingPresentation("service-inactive");
                return false;
            }
            var selected = selectedAbilityHandler()?.Ability;
            if (!MountedTargetingGlowPolicy.Retains(recorded, targetingOperation, PresentationSerial(selected)))
            {
                ClearTargetingPresentation("selection-ended");
                return false;
            }
            var mappedSlot = targetingSlot?.Target;
            if (mappedSlot == null)
            {
                ClearTargetingPresentation("slot-destroyed");
                return false;
            }
            if (!ReferenceEquals(mappedSlot, slot)) return false;
            if (MountedTargetingGlowPolicy.Glows(recorded, LiveTargetingIdentity(slot, selected))) return true;
            ClearTargetingPresentation("identity-changed");
            return false;
        }

        // Every identity as it is now. A public or delivery ability whose own fact is gone reads as no ability.
        private MountedTargetingSlotIdentity LiveTargetingIdentity(MechanicActionBarSlotAbility slot, AbilityData delivery)
        {
            var unit = slot?.Unit;
            var publicAbility = slot?.Ability;
            var owner = unit == null ? null : ResolveControlBoundary(unit, false);
            var publicLive = publicAbility != null && abilityFactLive(unit, publicAbility);
            var deliveryLive = delivery != null && abilityFactLive(unit, delivery);
            return new MountedTargetingSlotIdentity(
                PresentationSerial(slot),
                publicLive ? PresentationSerial(publicAbility) : 0,
                owner?.PairOwner?.Id ?? Guid.Empty,
                owner?.MountedPairGeneration ?? 0,
                owner == null ? -1 : (int)owner.State,
                unit?.UniqueId,
                publicAbility?.Blueprint?.AssetGuid,
                delivery?.Blueprint?.AssetGuid,
                deliveryLive ? PresentationSerial(delivery) : 0,
                targetingOperation);
        }

        private static readonly FieldInfo SlotActiveMark = ResolveSlotField("ActiveMark", 0x04002E37);
        private static readonly FieldInfo SlotDefaultColor = ResolveSlotField("DefaultColor", 0x04002E39);

        private static FieldInfo ResolveSlotField(string name, int token)
        {
            var field = typeof(Kingmaker.UI.ActionBar.ActionBarSlot).GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null || field.MetadataToken != token) throw new MissingFieldException(typeof(Kingmaker.UI.ActionBar.ActionBarSlot).FullName, name);
            return field;
        }

        // Native's own selected branch of UpdateSlotInternal for this slot view: the active mark in its default colour,
        // shown where the slot is possible. Nothing else of the slot changes.
        internal static void ShowTargetingSelection(MechanicActionBarSlotAbility slot, Kingmaker.UI.ActionBar.ActionBarSlot view)
        {
            var mark = view == null ? null : SlotActiveMark.GetValue(view) as UnityEngine.Component;
            if (mark == null) return;
            var color = mark.GetType().GetProperty("color", BindingFlags.Instance | BindingFlags.Public);
            if (color != null && color.CanWrite) color.SetValue(mark, SlotDefaultColor.GetValue(view), null);
            mark.gameObject.SetActive(slot.IsPossibleActive);
        }
    }
}
