using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Kingmaker;
using Kingmaker.Blueprints.Root;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI;
using Kingmaker.UI.CharSelect;
using Kingmaker.UI.Common.Animations;
using Kingmaker.UI.Group;
using Kingmaker.UI.LevelUp;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // The caller retains this object before Start, owns its isolated authority and
    // native builder, and retains it until Drain closes only these exact UI objects.
    // No companion/fact teardown, refund or direct native completion is performed.
    internal sealed class Chunk7NativeProgressionUi : IDisposable
    {
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        private readonly UnitEntityData target;
        private readonly Action assertScope;
        private readonly Func<bool> nativeBusy;
        private readonly Stopwatch elapsed = Stopwatch.StartNew();
        private readonly JArray events = new JArray();
        private GroupManager group;
        private GroupManagerCharacter entry;
        private DialogMessageBox dialog;
        private CharSelectWindow selector;
        private RespecCompanion respecAction;
        private NativeProgressionDismissProbe dismissal;
        private NativeProgressionRespecProbe respec;
        private bool started, respecSelected, groupOwned, selectorOwned, dialogOwned, confirmationInProgress;
        private bool confirmationSent, selectorChoiceSent, disposed;
        private int stage;
        internal Chunk7NativeProgressionUi(UnitEntityData target, Action assertScope, Func<bool> nativeBusy)
        {
            this.target = target ?? throw new ArgumentNullException(nameof(target));
            this.assertScope = assertScope ?? throw new ArgumentNullException(nameof(assertScope));
            this.nativeBusy = nativeBusy ?? throw new ArgumentNullException(nameof(nativeBusy));
        }
        internal NativeProgressionRespecProbe RespecProbe => respec;
        internal bool NativeDismissalObserved => dismissal?.Complete == true;
        internal bool NativeRespecPreviewObserved => respec != null && stage == 3;
        internal bool OwnsUi => groupOwned || selectorOwned || dialogOwned;
        internal JObject Capture() => new JObject { ["stage"] = stage, ["respec"] = respecSelected,
            ["groupOwned"] = groupOwned, ["selectorOwned"] = selectorOwned, ["dialogOwned"] = dialogOwned,
            ["confirmationSent"] = confirmationSent, ["selectorChoiceSent"] = selectorChoiceSent,
            ["events"] = events.DeepClone(), ["nativeDismissal"] = dismissal?.Capture(), ["nativeRespec"] = respec?.Capture() };
        internal void Start(bool retrain)
        {
            BeforeMutation();
            Require(!started && !disposed, "Native progression UI cannot reopen or reacquire a settled session.");
            Require(target.Blueprint == BlueprintRoot.Instance.CustomCompanion && !target.Descriptor.IsPet &&
                !ReferenceEquals(Game.Instance.Player.MainCharacter.Value, target) && target.Descriptor.State.CanRemoveFromParty &&
                !target.Descriptor.State.IsFinallyDead && target.IsInState && target.IsInGame,
                "Progression UI requires the exact disposable native custom companion in the field.");
            Require(DialogMessageBox.Instance == null || !DialogMessageBox.Instance.IsShown, "A pre-existing dialog blocks native progression input.");
            Require(Game.Instance.UI.CharacterBuildController?.LevelUpController == null && Game.Instance.UI.CharacterBuildController?.Unit == null,
                "A pre-existing native builder blocks progression input.");
            started = true; respecSelected = retrain;
            if (retrain)
            {
                Require(Game.Instance.Player.Difficulty.RespecAllowed && Game.Instance.Player.GetRespecCost() >= 0 &&
                    Game.Instance.Player.Money >= Game.Instance.Player.GetRespecCost(), "Native difficulty/funds refuse retraining.");
                selector = ExactSelector();
                Require(!selector.gameObject.activeSelf && !SelectorShown(), "A native character selector is already active.");
                respecAction = new RespecCompanion { ForFree = false, MatchPlayerXpExactly = false };
                respec = new NativeProgressionRespecProbe(target, respecAction, assertScope);
                respec.Install();
                selectorOwned = true; stage = 1;
                respecAction.RunAction();
                Require(ReferenceEquals(ExactSelector(), selector) && selector.gameObject.activeSelf && SelectorShown(),
                    "Native paid retrain action did not open the captured selector.");
                Record("native-respec-selector-opened");
            }
            else
            {
                group = Game.Instance.UI.Canvas.GroupManager;
                Require(group != null && !group.gameObject.activeSelf, "A native party selector is already active.");
                dismissal = new NativeProgressionDismissProbe(target, assertScope, () => confirmationInProgress);
                dismissal.Install();
                groupOwned = true; stage = 1;
                new ShowPartySelection().RunAction();
                Require(ReferenceEquals(Game.Instance.UI.Canvas.GroupManager, group) && group.gameObject.activeSelf,
                    "Native dismissal party selector did not open under exact ownership.");
                Record("native-dismiss-selector-opened");
            }
        }
        internal bool Tick()
        {
            assertScope();
            Require(started && !disposed && elapsed.Elapsed.TotalSeconds <= 60, "Native progression UI is inactive or exceeded its finite bound.");
            if (nativeBusy()) return false;
            if (respecSelected) return TickRespec();
            return TickDismissal();
        }
        private bool TickDismissal()
        {
            Require(groupOwned && ReferenceEquals(Game.Instance.UI.Canvas.GroupManager, group), "Owned dismissal selector was replaced.");
            if (stage == 1)
            {
                Require(group.gameObject.activeSelf, "Owned native party selector closed before dismissal.");
                if (!ReadyCanvas(GroupCanvas()) || !group.gameObject.activeInHierarchy) return false;
                Require(DialogMessageBox.Instance == null || !DialogMessageBox.Instance.IsShown, "Unexpected dialog blocks dismissal request.");
                var entries = Read<List<GroupManagerCharacter>>(group, typeof(GroupManagerBase), "ActiveCompanionsList", 0x04002B1A)
                    .Concat(Read<List<GroupManagerCharacter>>(group, typeof(GroupManagerBase), "RemoteCompanionsList", 0x04002B1B)).ToArray();
                Require(entries.Length <= 70, "Native party selector exceeds the bounded row count.");
                entry = entries.SingleOrDefault(value => value != null &&
                    ReferenceEquals(Read<UnitReference>(value, typeof(GroupManagerCharacter), "UnitReference", 0x04002B23).Value, target));
                Require(entry != null && entry.gameObject.activeInHierarchy &&
                    ReferenceEquals(Read<GroupManagerBase>(entry, typeof(GroupManagerCharacter), "GroupManager", 0x04002B24), group),
                    "The native selector has no exact owned target row.");
                var remove = ReadReference(entry, typeof(GroupManagerCharacter), "m_RemoveButton", 0x04002B1E, "UnityEngine.UI.Button");
                Require(ReadyButton(remove), "Native custom companion removal button is unavailable.");
                BeforeMutation(); dialog = DialogMessageBox.Instance;
                Require(dialog != null, "The native expected confirmation surface is unavailable.");
                dialogOwned = true; stage = 2;
                entry.OnClickRemove(); // Real native UI handler opens its own expected confirmation.
                RequireExpectedDialog(); Record("native-dismiss-confirmation-opened");
                return false;
            }
            if (stage == 2)
            {
                RequireExpectedDialog();
                var yes = ReadReference(dialog, typeof(DialogMessageBox), "m_ButtonYes", 0x04001DFB, "UnityEngine.UI.Button");
                if (!ReadyButton(yes)) return false;
                BeforeMutation(); Require(!confirmationSent, "Native dismissal confirmation cannot be repeated.");
                confirmationSent = true; confirmationInProgress = true; stage = 3;
                try { InvokeClick(yes); }
                finally { confirmationInProgress = false; }
                Require(dismissal.Complete, "Expected native Yes did not produce one actual native dismissal call.");
                Record("native-dismiss-confirmed");
            }
            Require(stage == 3 && confirmationSent && dismissal.Complete, "Native dismissal has no complete observed input.");
            if (dialog.IsShown) return false;
            Require(ReferenceEquals(DialogMessageBox.Instance, dialog), "Native dismissal dialog was replaced before settlement.");
            dialogOwned = false;
            return true;
        }
        private bool TickRespec()
        {
            Require(selectorOwned && ReferenceEquals(ExactSelector(), selector), "Owned respec selector was replaced.");
            if (stage == 1)
            {
                Require(selector.gameObject.activeSelf && SelectorShown(), "Owned respec selector closed before input.");
                var animator = Read<WindowAnimator>(selector, typeof(CharSelectWindow), "m_WindowAnimator", 0x04002D57);
                Require(animator != null, "Native selector window animator is unavailable.");
                var canvas = ReadReference(animator, typeof(WindowAnimator), "m_CanvasGroup", 0x04002D3A, "UnityEngine.CanvasGroup");
                if (!selector.gameObject.activeInHierarchy || !ReadyCanvas(canvas)) return false;
                Require(DialogMessageBox.Instance == null || !DialogMessageBox.Instance.IsShown, "Unexpected dialog blocks native respec selection.");
                var options = Read<List<UnitEntityData>>(selector, typeof(CharSelectWindow), "m_SelectCharacters", 0x04002D5C);
                var items = Read<List<HeroPortraitButton>>(selector, typeof(CharSelectWindow), "m_SelectorItems", 0x04002D5B);
                Require(options.Count <= 64 && items.Count <= 64 && options.Count(value => ReferenceEquals(value, target)) == 1,
                    "Native respec selector does not admit the exact target once.");
                var index = options.FindIndex(value => ReferenceEquals(value, target));
                var item = items.SingleOrDefault(value => value != null && value.gameObject.activeSelf && value.Index == index);
                var toggle = item == null ? null : ReadReference(item, typeof(HeroPortraitButton), "Toggle", 0x04002D62, "UnityEngine.UI.Toggle");
                Require(item != null && item.gameObject.activeInHierarchy && ReadyButton(toggle),
                    "Native respec target toggle is unavailable.");
                BeforeMutation(); Require(!selectorChoiceSent, "Native respec selector choice cannot repeat.");
                var isOn = toggle.GetType().GetProperty("isOn");
                Require(isOn?.PropertyType == typeof(bool) && isOn.CanWrite, "Native target toggle contract differs.");
                selectorChoiceSent = true; isOn.SetValue(toggle, true, null);
                Require(ReferenceEquals(selector.CurrentCharacter, target), "Native selector did not accept the exact target.");
                stage = 2; Record("native-respec-target-selected"); return false;
            }
            if (stage == 2)
            {
                Require(selector.gameObject.activeSelf && SelectorShown() && ReferenceEquals(selector.CurrentCharacter, target),
                    "Native respec target changed before acceptance.");
                var callback = Read<Action>(selector, typeof(CharSelectWindow), "m_SuccessAction", 0x04002D5D);
                Require(callback != null && callback.GetInvocationList().Length == 1 && ReferenceEquals(callback.Target, respecAction) &&
                    callback.Method.DeclaringType == typeof(RespecCompanion) && callback.Method.MetadataToken == 0x06008ED5,
                    "Native selector carries another action's success callback.");
                var ok = ReadReference(selector, typeof(CharSelectWindow), "m_ButtonOk", 0x04002D55, "UnityEngine.UI.Button");
                if (!ReadyButton(ok)) return false;
                BeforeMutation(); stage = 3; InvokeClick(ok);
                respec.RequireExactPreview(Game.Instance.UI.CharacterBuildController);
                Record("native-respec-preview-opened");
            }
            Require(stage == 3, "Native respec selector has no observed accepted preview."); return true;
        }
        internal bool DrainUi()
        {
            if (disposed) return true;
            assertScope();
            if (nativeBusy() || confirmationInProgress || dismissal?.InSynchronousCallback == true || respec?.InSynchronousCallback == true) return false;
            Require(DialogMessageBox.Instance == null || !DialogMessageBox.Instance.IsShown ||
                dialogOwned && ReferenceEquals(DialogMessageBox.Instance, dialog),
                "Unexpected native dialog retains progression UI custody without dismissal or further UI input.");
            if (dialogOwned)
            {
                Require(ReferenceEquals(DialogMessageBox.Instance, dialog), "Cleanup refuses a replaced dismissal dialog.");
                if (dialog.IsShown)
                {
                    if (confirmationSent) return false;
                    RequireExpectedDialog(); var no = ReadReference(dialog, typeof(DialogMessageBox), "m_ButtonNo", 0x04001DFD, "UnityEngine.UI.Button");
                    if (!ReadyButton(no)) return false;
                    BeforeMutation(); InvokeClick(no);
                    if (dialog.IsShown) return false;
                }
                dialogOwned = false;
            }
            if (groupOwned)
            {
                Require(ReferenceEquals(Game.Instance.UI.Canvas.GroupManager, group), "Cleanup refuses a replaced native party selector.");
                if (group.gameObject.activeSelf) { BeforeMutation(); group.Close(); }
                if (group.gameObject.activeSelf) return false;
                groupOwned = false;
            }
            if (selectorOwned)
            {
                Require(ReferenceEquals(ExactSelector(), selector), "Cleanup refuses a replaced native respec selector.");
                if (SelectorShown()) { BeforeMutation(); selector.OnButtonClose(); }
                if (selector.gameObject.activeSelf) return false; // Native disappearance callback owns deactivation.
                selectorOwned = false;
            }
            return true;
        }
        private void RequireExpectedDialog()
        {
            Require(dialogOwned && dialog != null && ReferenceEquals(DialogMessageBox.Instance, dialog) && dialog.IsShown &&
                ReferenceEquals(Game.Instance.UI.Canvas.GroupManager, group) && entry != null &&
                ReferenceEquals(Read<UnitReference>(entry, typeof(GroupManagerCharacter), "UnitReference", 0x04002B23).Value, target) &&
                ReferenceEquals(Read<GroupManagerBase>(entry, typeof(GroupManagerCharacter), "GroupManager", 0x04002B24), group),
                "Expected dismissal confirmation lost its exact actor/selector/dialog binding.");
            var callback = Read<Action<DialogMessageBoxBase.BoxButton>>(dialog, typeof(DialogMessageBox), "m_OnClose", 0x04001E05);
            Require(Read<DialogMessageBoxBase.BoxType>(dialog, typeof(DialogMessageBox), "m_BoxType", 0x04001E04) == DialogMessageBoxBase.BoxType.Dialog &&
                callback != null && callback.GetInvocationList().Length == 1 && ReferenceEquals(callback.Target, entry) &&
                callback.Method.DeclaringType == typeof(GroupManagerCharacter) && callback.Method.MetadataToken == 0x06003F60,
                "Unexpected confirmation remains untouched.");
        }
        private object GroupCanvas()
        {
            var getter = typeof(GroupManagerBase).GetProperty("CanvasGroup", Fields)?.GetGetMethod(true);
            Require(getter?.MetadataToken == 0x06003F4C, "Native group canvas contract differs.");
            return getter.Invoke(group, null);
        }
        private bool SelectorShown() => Read<bool>(selector, typeof(CharSelectWindow), "m_IsShowed", 0x04002D5E);
        private static CharSelectWindow ExactSelector()
        {
            var all = Game.Instance.UI.Canvas.GetComponentsInChildren<CharSelectWindow>(true);
            Require(all.Length == 1, "Native character-selector surface is absent or duplicated."); return all[0];
        }
        private static T Read<T>(object owner, Type declaring, string name, int token)
        {
            var field = declaring.GetField(name, Fields);
            Require(owner != null && field?.MetadataToken == token && field.FieldType == typeof(T), "Native read-only UI contract differs: " + name);
            return (T)field.GetValue(owner);
        }
        private static object ReadReference(object owner, Type declaring, string name, int token, string expectedType)
        {
            var field = declaring.GetField(name, Fields);
            Require(owner != null && field?.MetadataToken == token && field.FieldType.FullName == expectedType,
                "Native read-only UI reference contract differs: " + name);
            return field.GetValue(owner);
        }
        private static bool ReadyCanvas(object canvas)
        {
            var property = canvas?.GetType().GetProperty("alpha");
            Require(property?.PropertyType == typeof(float), "Native canvas alpha contract differs.");
            var alpha = (float)property.GetValue(canvas, null);
            Require(!float.IsNaN(alpha) && !float.IsInfinity(alpha) && alpha >= 0 && alpha <= 1,
                "Native UI fade is unavailable or invalid.");
            return alpha == 1;
        }
        private static bool ReadyButton(object button)
        {
            var property = button?.GetType().GetProperty("interactable");
            return button is Component component && component != null && component.gameObject.activeInHierarchy &&
                property?.PropertyType == typeof(bool) && (bool)property.GetValue(button, null);
        }
        private static void InvokeClick(object button)
        {
            Require(ReadyButton(button), "Native owned button is unavailable.");
            var onClick = button.GetType().GetProperty("onClick");
            var click = onClick?.GetValue(button, null); var invoke = click?.GetType().GetMethod("Invoke", Type.EmptyTypes);
            Require(invoke != null && invoke.ReturnType == typeof(void), "Native owned button event is unavailable.");
            invoke.Invoke(click, null);
        }
        private void BeforeMutation() { assertScope(); Require(!nativeBusy(), "Native loading or writer blocks progression UI input."); }
        private void Record(string name) { Require(events.Count < 32, "Native progression UI exceeded its bounded events."); events.Add(new JObject { ["kind"] = name, ["frame"] = Time.frameCount, ["actorId"] = target.UniqueId }); }
        private static void Require(bool value, string error) { if (!value) throw new InvalidOperationException(error); }
        public void Dispose()
        {
            if (disposed) return;
            if (!DrainUi()) throw new InvalidOperationException("Owned native progression UI still retains cleanup custody.");
            // The owner must cancel/drain its exact native builder before disposing
            // this lease; the selector does not own that separate UI lifetime.
            Require(!respecSelected || Game.Instance.UI.CharacterBuildController.LevelUpController == null,
                "Native respec builder still owns the actor; observer custody is retained.");
            // Keep the exact completed native observations after unpatch. The
            // outer owner may retry an independent observer release and still
            // needs this proof of the permitted actor rebind/removal. These
            // disposed probes cannot install again and own no live patches.
            dismissal?.Dispose();
            respec?.Dispose();
            disposed = true;
        }
    }
}
