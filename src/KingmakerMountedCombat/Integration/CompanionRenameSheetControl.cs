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
    // PET-01's entry point: one copy of the native message box's own button, fixed at the right end of the character sheet's
    // name line and shown only while the sheet displays a renameable companion. It opens the native text dialog. The name
    // keeps a slot clear for it: while the control shows, the name text gets an equal margin on both sides (so it stays
    // centred where the sheet puts it), shrinks to fit, and ends in an ellipsis rather than wrapping into the level line.
    // Every other unit gets the label's own settings back. The product does not reference UnityEngine.UI, so the button and
    // label members are reached by exact token.
    internal sealed class CompanionRenameSheetControl : IDisposable
    {
        private const float Scale = 0.75f, Gap = 8f, MinimumFontRatio = 0.7f;
        private static readonly FieldInfo CharNameField = Field(typeof(CharacterScreenController), "CharName", 0x0400313D, BindingFlags.Public);
        private static readonly FieldInfo CurrentField = Field(typeof(CharacterScreenController), "m_CurrentCharacter", 0x04003140, BindingFlags.NonPublic);
        private static readonly FieldInfo YesButtonField = Field(typeof(Kingmaker.UI.DialogMessageBox), "m_ButtonYes", 0x04001DFB, BindingFlags.NonPublic);
        private static readonly Type ButtonType = YesButtonField?.FieldType.FullName == "UnityEngine.UI.Button" ? YesButtonField.FieldType : null;
        private static readonly MethodInfo GetOnClick = Exact(ButtonType?.GetProperty("onClick", BindingFlags.Instance | BindingFlags.Public)?.GetGetMethod(), 0x06000186);
        private static readonly MethodInfo SetInteractable = Exact(ButtonType?.GetProperty("interactable", BindingFlags.Instance | BindingFlags.Public)?.GetSetMethod(), 0x0600043D);
        private static readonly Type LabelType = CharNameField?.FieldType.BaseType?.FullName == "TMPro.TMP_Text" ? CharNameField.FieldType.BaseType : null;
        private static readonly PropertyInfo LabelText = ExactProperty(LabelType?.GetProperty("text", BindingFlags.Instance | BindingFlags.Public), 0x17000117);
        private static readonly PropertyInfo Margin = ExactProperty(LabelType?.GetProperty("margin", BindingFlags.Instance | BindingFlags.Public), 0x17000155);
        private static readonly PropertyInfo WordWrapping = ExactProperty(LabelType?.GetProperty("enableWordWrapping", BindingFlags.Instance | BindingFlags.Public), 0x17000139);
        private static readonly PropertyInfo Overflow = ExactProperty(LabelType?.GetProperty("overflowMode", BindingFlags.Instance | BindingFlags.Public), 0x1700013B);
        private static readonly PropertyInfo AutoSizing = ExactProperty(LabelType?.GetProperty("enableAutoSizing", BindingFlags.Instance | BindingFlags.Public), 0x1700012D);
        private static readonly PropertyInfo FontSizeMin = ExactProperty(LabelType?.GetProperty("fontSizeMin", BindingFlags.Instance | BindingFlags.Public), 0x1700012E);
        private static readonly PropertyInfo FontSizeMax = ExactProperty(LabelType?.GetProperty("fontSizeMax", BindingFlags.Instance | BindingFlags.Public), 0x1700012F);
        private static readonly PropertyInfo FontSize = ExactProperty(LabelType?.GetProperty("fontSize", BindingFlags.Instance | BindingFlags.Public), 0x17000129);
        private static readonly object Ellipsis = Overflow != null && Enum.IsDefined(Overflow.PropertyType, "Ellipsis") ? Enum.Parse(Overflow.PropertyType, "Ellipsis") : null;

        private readonly CompanionNameService names;
        private readonly IModLogger logger;
        private CharacterScreenController screen;
        private GameObject rename;
        private UnitEntityData shown;
        private Component styled, originalOf;
        private LabelStyle original;
        private bool failed, disposed;

        internal CompanionRenameSheetControl(CompanionNameService names, IModLogger logger)
        {
            this.names = names;
            this.logger = logger;
        }

        internal static bool ContractAvailable => CharNameField != null && CurrentField != null && ButtonType != null && GetOnClick != null &&
            SetInteractable != null && LabelType != null && LabelText != null && Margin != null && WordWrapping != null && Overflow != null &&
            AutoSizing != null && FontSizeMin != null && FontSizeMax != null && FontSize != null && Ellipsis != null;
        internal UnitEntityData Shown => shown;
        internal GameObject RenameButton => rename;
        internal string Layout { get; private set; }
        internal string RenameLabel => Label(rename);
        // The name label's margin (left, top, right, bottom) while the control shows; null when the label has its own settings.
        internal Vector4? ReservedMargin => styled == null ? (Vector4?)null : (Vector4)Margin.GetValue(styled, null);
        internal Component NameLabel => screen == null ? null : CharNameField.GetValue(screen) as Component;
        internal CharacterScreenController Screen => screen;

        // Evidence: whether the live name label carries exactly the settings it had before this control first styled it.
        // Null when the control has never styled the label now on the sheet. An auto-sized label sets its own font size.
        internal bool? LabelHasOwnStyle()
        {
            var label = NameLabel;
            if (label == null || !ReferenceEquals(label, originalOf)) return null;
            var now = LabelStyle.Read(label);
            return now.Margin == original.Margin && now.WordWrapping == original.WordWrapping && Equals(now.Overflow, original.Overflow) &&
                now.AutoSizing == original.AutoSizing && now.FontSizeMin == original.FontSizeMin && now.FontSizeMax == original.FontSizeMax &&
                (original.AutoSizing || now.FontSize == original.FontSize);
        }

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
                if (eligible) Arrange(); else Unstyle();
            }
            catch (Exception exception)
            {
                // Presentation only: the sheet keeps working without the control, and its name gets its own settings back.
                failed = true;
                try { if (rename != null) rename.SetActive(false); Unstyle(); } catch (Exception) { }
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
            if (screen == controller && rename != null) return true;
            if (!ContractAvailable) { failed = true; logger?.Warning("Companion rename control unavailable: a native UI contract differs."); return false; }
            var source = YesButtonField.GetValue(Kingmaker.UI.DialogMessageBox.Instance) as Component;
            var name = CharNameField.GetValue(controller) as Component;
            if (source == null || name == null) return false;
            Destroy();
            screen = controller;
            rename = Clone(source.gameObject, name.transform, "KMC_RenameCompanion", names.Text("KMC.Companion.Rename.Button"), OnRename);
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

        // Always the right end of the name line, vertically centred on it, in the line's own units, so the sheet's canvas
        // scaling moves both together. The name gives up the button's width plus a gap on each side.
        private void Arrange()
        {
            var name = CharNameField.GetValue(screen) as Component;
            var renameRect = (RectTransform)rename.transform;
            renameRect.anchorMin = renameRect.anchorMax = renameRect.pivot = new Vector2(1f, 0.5f);
            renameRect.anchoredPosition = Vector2.zero;
            var reserve = renameRect.rect.width * Scale + Gap;
            Style(name, reserve);
            Layout = "name-line-right";
        }

        private void Style(Component label, float reserve)
        {
            if (!ReferenceEquals(styled, label))
            {
                Unstyle();
                original = LabelStyle.Read(label);
                originalOf = label;
                styled = label;
            }
            var reserved = original.Margin;
            reserved.x = Math.Max(reserved.x, reserve);
            reserved.z = Math.Max(reserved.z, reserve);
            var largest = original.AutoSizing ? original.FontSizeMax : original.FontSize;
            Margin.SetValue(label, reserved, null);
            WordWrapping.SetValue(label, false, null);
            Overflow.SetValue(label, Ellipsis, null);
            FontSizeMax.SetValue(label, largest, null);
            FontSizeMin.SetValue(label, original.AutoSizing ? Math.Min(original.FontSizeMin, largest * MinimumFontRatio) : largest * MinimumFontRatio, null);
            AutoSizing.SetValue(label, true, null);
        }

        // The label is the sheet's one name field for every character, so anything not a companion gets it back unchanged.
        private void Unstyle()
        {
            var label = styled;
            styled = null;
            if (label == null) return;
            original.Write(label);
        }

        private void OnRename()
        {
            if (shown != null && CompanionNameService.IsEligible(shown)) names.OpenRename(shown);
        }

        private static string Label(GameObject button)
        {
            if (button == null || LabelType == null) return null;
            var text = button.GetComponentInChildren(LabelType, true);
            return text == null ? null : LabelText.GetValue(text, null) as string;
        }

        private void Destroy()
        {
            Unstyle();
            if (rename != null) Object.Destroy(rename);
            rename = null;
            shown = null;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            Destroy();
            screen = null;
        }

        private struct LabelStyle
        {
            internal Vector4 Margin;
            internal bool WordWrapping, AutoSizing;
            internal object Overflow;
            internal float FontSizeMin, FontSizeMax, FontSize;

            internal static LabelStyle Read(Component label) => new LabelStyle
            {
                Margin = (Vector4)CompanionRenameSheetControl.Margin.GetValue(label, null),
                WordWrapping = (bool)CompanionRenameSheetControl.WordWrapping.GetValue(label, null),
                Overflow = CompanionRenameSheetControl.Overflow.GetValue(label, null),
                AutoSizing = (bool)CompanionRenameSheetControl.AutoSizing.GetValue(label, null),
                FontSizeMin = (float)CompanionRenameSheetControl.FontSizeMin.GetValue(label, null),
                FontSizeMax = (float)CompanionRenameSheetControl.FontSizeMax.GetValue(label, null),
                FontSize = (float)CompanionRenameSheetControl.FontSize.GetValue(label, null)
            };

            // A destroyed label (the sheet was rebuilt) needs nothing back.
            internal void Write(Component label)
            {
                if (label == null) return;
                CompanionRenameSheetControl.AutoSizing.SetValue(label, AutoSizing, null);
                CompanionRenameSheetControl.FontSizeMin.SetValue(label, FontSizeMin, null);
                CompanionRenameSheetControl.FontSizeMax.SetValue(label, FontSizeMax, null);
                CompanionRenameSheetControl.FontSize.SetValue(label, FontSize, null);
                CompanionRenameSheetControl.Overflow.SetValue(label, Overflow, null);
                CompanionRenameSheetControl.WordWrapping.SetValue(label, WordWrapping, null);
                CompanionRenameSheetControl.Margin.SetValue(label, Margin, null);
            }
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
