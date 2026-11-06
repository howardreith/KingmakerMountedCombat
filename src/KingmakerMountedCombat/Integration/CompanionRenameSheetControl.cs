using System;
using System.Reflection;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.ServiceWindow.CharacterScreen;
using Kingmaker.UnitLogic;
using KingmakerMountedCombat.Logging;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace KingmakerMountedCombat.Integration
{
    // PET-01's entry point: two copies of the native message box's own button on the character sheet's name line, shown only
    // while the sheet displays a renameable companion. Rename opens the native text dialog; Default Name (shown once a custom
    // name exists) asks before restoring the blueprint name. The product does not reference UnityEngine.UI, so the button and
    // label members are reached by exact token.
    internal sealed class CompanionRenameSheetControl : IDisposable
    {
        private const float Scale = 0.75f, Gap = 8f;
        private static readonly FieldInfo CharNameField = Field(typeof(CharacterScreenController), "CharName", 0x0400313D, BindingFlags.Public);
        private static readonly FieldInfo CurrentField = Field(typeof(CharacterScreenController), "m_CurrentCharacter", 0x04003140, BindingFlags.NonPublic);
        private static readonly FieldInfo YesButtonField = Field(typeof(Kingmaker.UI.DialogMessageBox), "m_ButtonYes", 0x04001DFB, BindingFlags.NonPublic);
        private static readonly Type ButtonType = YesButtonField?.FieldType.FullName == "UnityEngine.UI.Button" ? YesButtonField.FieldType : null;
        private static readonly MethodInfo GetOnClick = Exact(ButtonType?.GetProperty("onClick", BindingFlags.Instance | BindingFlags.Public)?.GetGetMethod(), 0x06000186);
        private static readonly MethodInfo SetInteractable = Exact(ButtonType?.GetProperty("interactable", BindingFlags.Instance | BindingFlags.Public)?.GetSetMethod(), 0x0600043D);
        private static readonly Type LabelType = CharNameField?.FieldType.BaseType?.FullName == "TMPro.TMP_Text" ? CharNameField.FieldType.BaseType : null;
        private static readonly PropertyInfo LabelText = ExactProperty(LabelType?.GetProperty("text", BindingFlags.Instance | BindingFlags.Public), 0x17000117);
        private static readonly PropertyInfo PreferredWidth = LabelType?.GetProperty("preferredWidth", BindingFlags.Instance | BindingFlags.Public);

        private readonly CompanionNameService names;
        private readonly IModLogger logger;
        private CharacterScreenController screen;
        private GameObject rename, reset;
        private UnitEntityData shown;
        private bool failed, disposed;

        internal CompanionRenameSheetControl(CompanionNameService names, IModLogger logger)
        {
            this.names = names;
            this.logger = logger;
        }

        internal static bool ContractAvailable => CharNameField != null && CurrentField != null && ButtonType != null && GetOnClick != null &&
            SetInteractable != null && LabelType != null && LabelText != null && PreferredWidth != null;
        internal UnitEntityData Shown => shown;
        internal GameObject RenameButton => rename;
        internal GameObject ResetButton => reset;
        internal string Layout { get; private set; }
        internal string RenameLabel => Label(rename);
        internal string ResetLabel => Label(reset);

        // CharacterScreenController.SetupInfo postfix: the sheet has just filled its name for the displayed unit.
        internal void Refresh(CharacterScreenController controller)
        {
            if (disposed || failed || controller == null) return;
            try
            {
                var unit = (CurrentField?.GetValue(controller) as UnitDescriptor)?.Unit;
                var eligible = unit != null && CompanionNameService.IsEligible(unit);
                if (!eligible && rename == null) return;
                if (!Ensure(controller)) return;
                shown = eligible ? unit : null;
                rename.SetActive(eligible);
                reset.SetActive(eligible && CompanionNameService.HasCustomName(unit));
                if (eligible) Arrange();
            }
            catch (Exception exception)
            {
                // Presentation only: the sheet keeps working without the control.
                failed = true;
                logger?.Warning("Companion rename control failed: " + exception.GetType().Name + ": " + exception.Message);
            }
        }

        // After a rename the native sheet refills its name, and this control follows.
        internal void RefreshShown()
        {
            if (screen != null && screen.isActiveAndEnabled) screen.SetupInfo();
        }

        // Runtime evidence and the ordinary click both press the button through its own native click event.
        internal static bool Press(GameObject button)
        {
            if (button == null || !button.activeInHierarchy || ButtonType == null) return false;
            var component = button.GetComponent(ButtonType);
            if (component == null) return false;
            ((UnityEvent)GetOnClick.Invoke(component, null)).Invoke();
            return true;
        }

        private bool Ensure(CharacterScreenController controller)
        {
            if (screen == controller && rename != null && reset != null) return true;
            if (!ContractAvailable) { failed = true; logger?.Warning("Companion rename control unavailable: a native UI contract differs."); return false; }
            var source = YesButtonField.GetValue(Kingmaker.UI.DialogMessageBox.Instance) as Component;
            var name = CharNameField.GetValue(controller) as Component;
            if (source == null || name == null) return false;
            Destroy();
            screen = controller;
            rename = Clone(source.gameObject, name.transform, "KMC_RenameCompanion", names.Text("KMC.Companion.Rename.Button"), OnRename);
            reset = Clone(source.gameObject, name.transform, "KMC_ResetCompanionName", names.Text("KMC.Companion.Reset.Button"), OnReset);
            return true;
        }

        private static GameObject Clone(GameObject source, Transform parent, string objectName, string label, UnityAction action)
        {
            var clone = Object.Instantiate(source, parent, false);
            clone.name = objectName;
            clone.SetActive(false);
            var button = clone.GetComponent(ButtonType);
            var click = (UnityEvent)GetOnClick.Invoke(button, null);
            // Only the dialog's runtime listeners exist, and a copy carries none; any serialized ones are switched off too.
            for (var index = 0; index < click.GetPersistentEventCount(); index++) click.SetPersistentListenerState(index, UnityEventCallState.Off);
            click.RemoveAllListeners();
            click.AddListener(action);
            SetInteractable.Invoke(button, new object[] { true });
            foreach (var text in clone.GetComponentsInChildren(LabelType, true)) LabelText.SetValue(text, label, null);
            var rect = (RectTransform)clone.transform;
            rect.localScale = new Vector3(Scale, Scale, 1f);
            return clone;
        }

        // Right end of the name line when the centred name leaves room; otherwise centred just under it.
        private void Arrange()
        {
            var name = CharNameField.GetValue(screen) as Component;
            var line = (RectTransform)name.transform;
            var renameRect = (RectTransform)rename.transform;
            var resetRect = (RectTransform)reset.transform;
            var renameWidth = renameRect.rect.width * Scale;
            var resetWidth = reset.activeSelf ? resetRect.rect.width * Scale + Gap : 0f;
            var textWidth = Convert.ToSingle(PreferredWidth.GetValue(name, null));
            var room = (line.rect.width - textWidth) * 0.5f;
            if (room >= renameWidth + resetWidth + Gap)
            {
                Place(renameRect, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero);
                Place(resetRect, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-(renameWidth + Gap), 0f));
                Layout = "name-line-right";
            }
            else
            {
                var total = renameWidth + resetWidth;
                Place(renameRect, new Vector2(0.5f, 0f), new Vector2(1f, 1f), new Vector2(total * 0.5f, -Gap * 0.5f));
                Place(resetRect, new Vector2(0.5f, 0f), new Vector2(1f, 1f), new Vector2(total * 0.5f - renameWidth - Gap, -Gap * 0.5f));
                Layout = "under-name";
            }
        }

        private static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position)
        {
            rect.anchorMin = anchor; rect.anchorMax = anchor; rect.pivot = pivot; rect.anchoredPosition = position;
        }

        private void OnRename()
        {
            if (shown != null && CompanionNameService.IsEligible(shown)) names.OpenRename(shown);
        }

        private void OnReset()
        {
            if (shown != null && CompanionNameService.IsEligible(shown)) names.OpenReset(shown);
        }

        private static string Label(GameObject button)
        {
            if (button == null || LabelType == null) return null;
            var text = button.GetComponentInChildren(LabelType, true);
            return text == null ? null : LabelText.GetValue(text, null) as string;
        }

        private void Destroy()
        {
            if (rename != null) Object.Destroy(rename);
            if (reset != null) Object.Destroy(reset);
            rename = reset = null;
            shown = null;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            Destroy();
            screen = null;
        }

        private static FieldInfo Field(Type type, string name, int token, BindingFlags visibility)
        {
            var field = type.GetField(name, BindingFlags.Instance | visibility);
            return field != null && field.MetadataToken == token ? field : null;
        }

        private static MethodInfo Exact(MethodInfo method, int token) => method != null && method.MetadataToken == token ? method : null;
        private static PropertyInfo ExactProperty(PropertyInfo property, int token) => property != null && property.MetadataToken == token ? property : null;
    }
}
