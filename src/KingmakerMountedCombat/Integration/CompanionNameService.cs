using System;
using System.Linq;
using System.Reflection;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.UI;
using Kingmaker.UnitLogic.Parts;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Logging;
using UnityEngine;

namespace KingmakerMountedCombat.Integration
{
    // PET-01: a player-owned permanent animal companion's name is the native per-unit UnitDescriptor.CustomName, the field the
    // native ChangeUnitName action writes and every native label reads through CharacterName. It is saved with that unit and
    // nowhere else: no blueprint, id, ownership or parallel table changes, and a replacement companion starts with its own default.
    internal sealed class CompanionNameService : IDisposable
    {
        internal static CompanionNameService Current { get; private set; }
        private readonly Func<string, string> text;
        private readonly IModLogger logger;
        private bool disposed;

        internal CompanionNameService(Func<string, string> text, IModLogger logger)
        {
            this.text = text ?? throw new ArgumentNullException(nameof(text));
            this.logger = logger;
            Dialog = new NativeTextDialog();
            Sheet = new CompanionRenameSheetControl(this, logger);
            Current = this;
        }

        internal NativeTextDialog Dialog { get; }
        internal CompanionRenameSheetControl Sheet { get; }
        // The last confirmed or refused outcome, for the log and the runtime evidence.
        internal string LastOutcome { get; private set; }
        internal int Writes { get; private set; }

        internal string Text(string key) => text(key);

        internal static CompanionRenameSubject Subject(UnitEntityData unit)
        {
            if (unit == null) return null;
            var descriptor = unit.Descriptor;
            var master = descriptor?.Master.Value;
            var player = Game.Instance?.Player;
            return new CompanionRenameSubject
            {
                UnitId = unit.UniqueId,
                MasterId = master?.UniqueId,
                IsPet = descriptor != null && descriptor.IsPet,
                MasterOwnsThisPet = master != null && ReferenceEquals(master.Descriptor.Pet, unit),
                MasterIsPlayerCharacter = master != null && player != null && player.AllCharacters.Contains(master),
                IsPlayerFaction = unit.IsPlayerFaction,
                IsSummoned = unit.Get<UnitPartSummonedMonster>() != null
            };
        }

        internal static string Refusal(UnitEntityData unit) => CompanionRenameEligibility.Refusal(Subject(unit));
        internal static bool IsEligible(UnitEntityData unit) => Refusal(unit) == null;
        internal static string DefaultName(UnitEntityData unit) => unit?.Blueprint?.CharacterName ?? string.Empty;
        internal static bool HasCustomName(UnitEntityData unit) => !string.IsNullOrEmpty(unit?.Descriptor?.CustomName);

        // Confirmation re-reads ownership on the exact unit the dialog opened for; the current selection plays no part.
        internal CompanionNameVerdict Rename(UnitEntityData unit, CompanionRenameSubject opened, string typed, out string refusal)
        {
            refusal = CompanionRenameEligibility.ConfirmationRefusal(opened, Subject(unit));
            if (refusal != null) { Record("rename refused: " + refusal); return CompanionNameVerdict.Empty; }
            var verdict = CompanionNamePolicy.Normalize(typed, out var name);
            if (verdict != CompanionNameVerdict.Accepted) { Record("rename rejected: " + verdict); return verdict; }
            Write(unit, name);
            Record("renamed " + unit.UniqueId);
            return verdict;
        }

        internal bool ResetToDefault(UnitEntityData unit, CompanionRenameSubject opened, out string refusal)
        {
            refusal = CompanionRenameEligibility.ConfirmationRefusal(opened, Subject(unit));
            if (refusal != null) { Record("reset refused: " + refusal); return false; }
            Write(unit, string.Empty);
            Record("reset " + unit.UniqueId);
            return true;
        }

        private void Write(UnitEntityData unit, string value)
        {
            unit.Descriptor.CustomName = value;
            Writes++;
            // The native rename event: overtips and dialog portraits refresh from it.
            EventBus.RaiseEvent(delegate (IUnitNameHandler handler) { handler.OnUnitNameChanged(unit); });
            Sheet.RefreshShown();
        }

        private void Record(string outcome)
        {
            LastOutcome = outcome;
            logger?.Info("Companion name: " + outcome + ".");
        }

        // Rename Companion: the native text dialog, prefilled with the current name. Cancel and Esc change nothing.
        internal bool OpenRename(UnitEntityData unit)
        {
            var opened = Subject(unit);
            if (disposed || CompanionRenameEligibility.Refusal(opened) != null) return false;
            var prompt = string.Format(Text("KMC.Companion.Rename.Prompt"), unit.CharacterName, DefaultName(unit));
            return Dialog.OpenText(prompt, unit.CharacterName, Text("KMC.Companion.Rename.Confirm"), result => OnRenameResult(unit, opened, result));
        }

        private void OnRenameResult(UnitEntityData unit, CompanionRenameSubject opened, string result)
        {
            if (disposed) return;
            if (result == null) { Record("rename cancelled"); return; }
            var verdict = Rename(unit, opened, result, out var refusal);
            if (verdict == CompanionNameVerdict.Accepted || refusal != null) return;
            // A rejected entry explains itself in the native message box and reopens with what was typed. Both open from
            // Update, never inside the box's own close callbacks, which the native box follows with a second result callback.
            pending = () => Dialog.OpenMessage(Text("KMC.Companion.Rename.Invalid." + verdict), () => pending = () => Dialog.OpenText(
                string.Format(Text("KMC.Companion.Rename.Prompt"), unit.CharacterName, DefaultName(unit)), result,
                Text("KMC.Companion.Rename.Confirm"), retry => OnRenameResult(unit, opened, retry)));
        }

        private Action pending;

        internal void Update()
        {
            if (disposed) return;
            Dialog.Poll();
            if (pending == null || NativeTextDialog.IsShown) return;
            var next = pending;
            pending = null;
            next();
        }

        internal bool OpenReset(UnitEntityData unit)
        {
            var opened = Subject(unit);
            if (disposed || CompanionRenameEligibility.Refusal(opened) != null || !HasCustomName(unit)) return false;
            var prompt = string.Format(Text("KMC.Companion.Reset.Prompt"), unit.CharacterName, DefaultName(unit));
            return Dialog.OpenConfirm(prompt, yes =>
            {
                if (disposed) return;
                if (!yes) { Record("reset cancelled"); return; }
                ResetToDefault(unit, opened, out _);
            });
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            Sheet.Dispose();
            if (ReferenceEquals(Current, this)) Current = null;
        }
    }

    // The native PC message box, reached through its own event interface. Its text field is the one the native save
    // overwrite uses; the only addition is starting the edit at once, so typing reaches the field and not the game.
    internal sealed class NativeTextDialog
    {
        private static readonly FieldInfo InputField = Field("m_InputField", 0x04001E02);
        private static readonly FieldInfo YesButton = Field("m_ButtonYes", 0x04001DFB);
        private static readonly FieldInfo NoButton = Field("m_ButtonNo", 0x04001DFD);
        private static readonly MethodInfo BeginEdit = InputField == null ? null : Method(InputField.FieldType, "BeingEdit", 0x06004134);
        private static readonly PropertyInfo TextInput = InputField == null ? null : InputField.FieldType.GetProperty("TextInput", BindingFlags.Instance | BindingFlags.Public);

        internal bool Available => InputField != null && YesButton != null && NoButton != null && BeginEdit != null && TextInput != null;
        internal static bool IsShown => DialogMessageBox.Instance != null && DialogMessageBox.Instance.IsShown;

        internal bool OpenText(string prompt, string initial, string yesLabel, Action<string> onResult)
        {
            if (!Available || IsShown) return false;
            EventBus.RaiseEvent(delegate (IDialogMessageBoxUIHandler handler)
            {
                handler.HandleOpen(prompt, onResult, initial, null, yesLabel, null, true, false, (uint)CompanionNamePolicy.MaxUtf16Length);
            });
            if (!IsShown) return false;
            var field = InputField.GetValue(DialogMessageBox.Instance);
            if (field != null) BeginEdit.Invoke(field, null);
            owned = true; focusSeen = false;
            return true;
        }

        private static readonly Type EventSystemType = YesButton?.FieldType.Assembly.GetType("UnityEngine.EventSystems.EventSystem", false);
        private static readonly PropertyInfo CurrentEventSystem = EventSystemType?.GetProperty("current", BindingFlags.Static | BindingFlags.Public);
        private static readonly PropertyInfo SelectedObject = EventSystemType?.GetProperty("currentSelectedGameObject", BindingFlags.Instance | BindingFlags.Public);
        private static readonly MethodInfo SelectObject = EventSystemType?.GetMethod("SetSelectedGameObject", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(GameObject) }, null);
        private bool owned, focusSeen;

        // While its field is selected the native KeyboardAccess runs no binding, Esc included, and the field takes Esc as its own
        // cancel. So for a text dialog this service opened, the field's cancel closes the box through the box's own Esc handler,
        // and a closed box gives its field's selection back, so game hotkeys return. Called every frame.
        internal void Poll()
        {
            if (!owned) return;
            var input = LiveTextInput();
            if (IsShown && input != null)
            {
                var type = input.GetType();
                var focused = Convert.ToBoolean(type.GetProperty("isFocused").GetValue(input, null));
                focusSeen |= focused;
                if (focusSeen && !focused && Convert.ToBoolean(type.GetProperty("wasCanceled").GetValue(input, null)))
                    DialogMessageBox.Instance.OnEscPressed();
                return;
            }
            if (IsShown) return;
            owned = false;
            ReleaseSelection();
        }

        private static void ReleaseSelection()
        {
            var system = CurrentEventSystem?.GetValue(null, null);
            var field = DialogMessageBox.Instance == null ? null : InputField?.GetValue(DialogMessageBox.Instance) as Component;
            if (system == null || field == null || SelectObject == null) return;
            var selected = SelectedObject.GetValue(system, null) as GameObject;
            if (selected != null && selected.transform.IsChildOf(field.transform)) SelectObject.Invoke(system, new object[] { null });
        }

        // Whether any selected UI field still holds the keyboard, the same test KeyboardAccess applies before every binding.
        internal static GameObject SelectedUi()
        {
            var system = CurrentEventSystem?.GetValue(null, null);
            return system == null ? null : SelectedObject.GetValue(system, null) as GameObject;
        }

        internal bool OpenConfirm(string prompt, Action<bool> onResult)
        {
            if (!Available || IsShown) return false;
            EventBus.RaiseEvent(delegate (IDialogMessageBoxUIHandler handler)
            {
                handler.HandleOpen(prompt, DialogMessageBoxBase.BoxType.Dialog, button => onResult(button == DialogMessageBoxBase.BoxButton.Yes), null, null, null, null);
            });
            return IsShown;
        }

        internal void OpenMessage(string message, Action then)
        {
            if (IsShown) return;
            EventBus.RaiseEvent(delegate (IDialogMessageBoxUIHandler handler)
            {
                handler.HandleOpen(message, DialogMessageBoxBase.BoxType.Message, button => then?.Invoke(), null, null, null, null);
            });
        }

        // The live text input (TMPro.TMP_InputField) of the shown dialog, for the runtime evidence.
        internal object LiveTextInput()
        {
            if (!Available || !IsShown) return null;
            var field = InputField.GetValue(DialogMessageBox.Instance);
            return field == null ? null : TextInput.GetValue(field, null);
        }

        internal UnityEngine.Component Button(bool yes) =>
            !Available || DialogMessageBox.Instance == null ? null : (yes ? YesButton : NoButton).GetValue(DialogMessageBox.Instance) as UnityEngine.Component;

        private static FieldInfo Field(string name, int token)
        {
            var field = typeof(DialogMessageBox).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            return field != null && field.MetadataToken == token ? field : null;
        }

        private static MethodInfo Method(Type type, string name, int token)
        {
            var method = type.GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, Type.EmptyTypes, null);
            return method != null && method.MetadataToken == token ? method : null;
        }
    }
}
