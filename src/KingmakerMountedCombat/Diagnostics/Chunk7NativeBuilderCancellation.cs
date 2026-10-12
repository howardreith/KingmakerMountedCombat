using System;
using System.Reflection;
using Kingmaker;
using Kingmaker.UI;
using Kingmaker.UI.LevelUp;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // The caller retains this lease before TryDrain. Only the real builder's
    // native Esc and its exact installed cancellation Yes event are selected.
    // No completion callback is supplied, and acquired native state is retained.
    internal sealed class Chunk7NativeBuilderCancellation
    {
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
        private readonly CharacterBuildController builder;
        private readonly Action assertScope;
        private readonly Func<bool> nativeBusy;
        private DialogMessageBox dialog;
        private bool ownsDialog, requested, confirmed, settled;
        internal Chunk7NativeBuilderCancellation(CharacterBuildController builder, Action assertScope, Func<bool> nativeBusy)
        {
            this.builder = builder ?? throw new ArgumentNullException(nameof(builder));
            this.assertScope = assertScope ?? throw new ArgumentNullException(nameof(assertScope));
            this.nativeBusy = nativeBusy ?? throw new ArgumentNullException(nameof(nativeBusy));
        }
        internal bool TryDrain()
        {
            assertScope();
            if (nativeBusy()) return false;
            Require(ReferenceEquals(Game.Instance.UI.CharacterBuildController, builder), "Owned native builder was replaced during cancellation.");
            if (settled) return true;
            if (!requested)
            {
                Require(DialogMessageBox.Instance == null || !DialogMessageBox.Instance.IsShown,
                    "An unexpected dialog prevents owned native builder cancellation.");
                if (builder.LevelUpController == null) { settled = true; return true; }
                dialog = DialogMessageBox.Instance;
                Require(dialog != null, "Native builder cancellation dialog is unavailable.");
                ownsDialog = true; requested = true;
                builder.OnHotKeyEscPressed();
            }
            Require(ownsDialog && ReferenceEquals(DialogMessageBox.Instance, dialog), "Owned builder cancellation dialog was replaced.");
            if (!confirmed)
            {
                var button = ReadOwnedButton();
                if (!ReadyButton(button)) return false;
                assertScope(); Require(!nativeBusy(), "Native worker became busy before the owned cancellation choice.");
                confirmed = true; InvokeClick(button);
            }
            if (builder.LevelUpController != null || dialog.IsShown) return false;
            ownsDialog = false; settled = true; return true;
        }
        private object ReadOwnedButton()
        {
            var callback = typeof(DialogMessageBox).GetField("m_OnClose", Fields);
            var box = typeof(DialogMessageBox).GetField("m_BoxType", Fields);
            Require(callback?.MetadataToken == 0x04001E05 && box?.MetadataToken == 0x04001E04 && dialog.IsShown,
                "Native cancellation dialog contract differs.");
            var action = callback.GetValue(dialog) as Action<DialogMessageBoxBase.BoxButton>;
            Require((DialogMessageBoxBase.BoxType)box.GetValue(dialog) == DialogMessageBoxBase.BoxType.Dialog &&
                action != null && action.GetInvocationList().Length == 1 && ReferenceEquals(action.Target, builder) &&
                action.Method.DeclaringType == typeof(CharacterBuildController) && action.Method.MetadataToken == 0x060039BF,
                "Native cancellation belongs to another action; its dialog remains untouched.");
            var field = typeof(DialogMessageBox).GetField("m_ButtonYes", Fields);
            Require(field?.MetadataToken == 0x04001DFB && field.FieldType.FullName == "UnityEngine.UI.Button",
                "Native owned cancellation button contract differs.");
            return field.GetValue(dialog);
        }
        private static bool ReadyButton(object button)
        {
            var interactable = button?.GetType().GetProperty("interactable");
            Require(button is Component && interactable?.PropertyType == typeof(bool), "Native cancellation button is unavailable.");
            return ((Component)button).gameObject.activeInHierarchy && (bool)interactable.GetValue(button, null);
        }
        private static void InvokeClick(object button)
        {
            var click = button.GetType().GetProperty("onClick")?.GetValue(button, null);
            var invoke = click?.GetType().GetMethod("Invoke", Type.EmptyTypes);
            Require(invoke != null && invoke.ReturnType == typeof(void), "Native cancellation click event is unavailable.");
            invoke.Invoke(click, null);
        }
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    }
}
