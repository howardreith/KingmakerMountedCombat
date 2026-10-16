using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker;
using Kingmaker.Blueprints.Root;
using Kingmaker.Controllers.Dialog;
using Kingmaker.DialogSystem;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.ElementsSystem;
using Kingmaker.GameModes;
using Kingmaker.UI;
using Kingmaker.UI.Dialog;
using Newtonsoft.Json.Linq;
using NativeDialogController = Kingmaker.Controllers.Dialog.DialogController;
using PcDialogController = Kingmaker.UI.Dialog.DialogController;

namespace KingmakerMountedCombat.Diagnostics
{
    // Reads existing controller/PC-panel storage only. No predicate, UI constructor,
    // callback, condition, action, text/localization or scene traversal is invoked.
    internal static class Chunk7DialogueChoiceObservation
    {
        internal const int MaximumAnswers = 32;
        internal const int MaximumRows = 32;
        internal const int MaximumDirectTypes = 256;
        internal const int MaximumTypesPerList = 32;
        internal const int MaximumTypeName = 256;
        // Per phase: five UI owners, 32 offered blueprints, 32 PC rows and
        // 32 possibly foreign row blueprints. Other context is already retained.
        internal const int AdditionalIdentitiesPerSnapshot = 101;

        internal static JObject Capture(RawState state, Func<object, int> identity)
        {
            try
            {
                if (state == null) throw new InvalidOperationException("Passive choice native boundary was not captured.");
                if (state.Error != null) return Failure(state.Error);
                if (state.Offered == null || state.Offered.Length > MaximumAnswers ||
                    state.Rows == null || state.Rows.Length > MaximumRows)
                    throw new InvalidOperationException("Passive choice collection is missing or exceeds its bound.");
                if (Array.IndexOf(Statuses, state.UiStatus) < 0) throw new InvalidOperationException("Passive choice UI status changed.");
                var remaining = MaximumDirectTypes;
                var offered = new JArray();
                foreach (var item in state.Offered)
                {
                    if (item == null || ReferenceEquals(item.Answer, null)) throw new InvalidOperationException("Passive offered answer is null.");
                    offered.Add(new JObject { ["answer"] = Blueprint(item.Answer, item.Guid, identity),
                        ["selectionType"] = item.SelectionType, ["requireValidCue"] = item.RequireValidCue,
                        ["showConditionTypes"] = Types(item.ShowTypes, ref remaining),
                        ["selectConditionTypes"] = Types(item.SelectTypes, ref remaining),
                        ["onSelectActionTypes"] = Types(item.ActionTypes, ref remaining) });
                }
                var rows = new JArray();
                foreach (var row in state.Rows)
                {
                    if (row == null || ReferenceEquals(row.View, null)) throw new InvalidOperationException("Passive PC row is null.");
                    var matches = new JArray();
                    for (var i = 0; i < state.Offered.Length; ++i)
                        if (!ReferenceEquals(row.Answer, null) && ReferenceEquals(row.Answer, state.Offered[i].Answer)) matches.Add(i);
                    rows.Add(new JObject { ["row"] = identity(row.View), ["answer"] = Blueprint(row.Answer, row.Guid, identity),
                        ["nativeIndex"] = row.Index, ["initialized"] = row.Initialized, ["cachedDisabled"] = row.Disabled,
                        ["choosingCharacter"] = row.ChoosingCharacter, ["interchapter"] = row.Interchapter,
                        ["matchingOfferIndices"] = matches,
                        ["sameRowReferences"] = state.Rows.Count(other => other != null && ReferenceEquals(other.View, row.View)),
                        ["sameAnswerReferences"] = ReferenceEquals(row.Answer, null) ? 0 : state.Rows.Count(other => other != null && ReferenceEquals(other.Answer, row.Answer)) });
                }
                return new JObject { ["readSucceeded"] = true, ["readError"] = null,
                    ["scope"] = "controller-offers-and-pc-cached-rows", ["cachedStateIsAuthorization"] = false,
                    ["directTypesOnly"] = true, ["systemButtonsIncluded"] = false,
                    ["game"] = identity(state.Game), ["controller"] = identity(state.Controller),
                    ["dialog"] = identity(state.Dialog), ["currentCue"] = identity(state.CurrentCue),
                    ["ui"] = identity(state.Ui), ["common"] = identity(state.Common), ["manager"] = identity(state.Manager),
                    ["pcDialog"] = identity(state.PcDialog), ["currentPart"] = identity(state.CurrentPart),
                    ["uiStatus"] = state.UiStatus, ["managerKind"] = state.ManagerKind, ["managerShown"] = state.ManagerShown,
                    ["pcDialogActive"] = state.PcDialogActive, ["offered"] = offered, ["rows"] = rows,
                    ["directTypeCount"] = MaximumDirectTypes - remaining };
            }
            catch (Exception error) { return Failure(ErrorText(error)); }
        }
        private static readonly string[] Statuses = { "missing-game", "missing-controller", "missing-ui", "missing-common",
            "missing-manager", "unsupported-dialog-kind", "missing-pc-dialog", "missing-current-part", "inactive-dialog", "pc-cached-rows" };
        private static JObject Failure(string error)
        {
            if (string.IsNullOrEmpty(error)) error = "Passive choice read failed without a message.";
            if (error.Length > 256) error = error.Substring(0, 256);
            return new JObject { ["readSucceeded"] = false, ["readError"] = error,
                ["scope"] = "controller-offers-and-pc-cached-rows", ["cachedStateIsAuthorization"] = false,
                ["directTypesOnly"] = true, ["systemButtonsIncluded"] = false,
                ["game"] = null, ["controller"] = null, ["dialog"] = null, ["currentCue"] = null,
                ["ui"] = null, ["common"] = null, ["manager"] = null, ["pcDialog"] = null, ["currentPart"] = null,
                ["uiStatus"] = null, ["managerKind"] = null, ["managerShown"] = null, ["pcDialogActive"] = null,
                ["offered"] = null, ["rows"] = null, ["directTypeCount"] = null };
        }
        private static JObject Blueprint(object value, string guid, Func<object, int> identity)
        {
            if (guid != null && (guid.Length != 32 || guid.Any(c => !(c >= '0' && c <= '9') && !(c >= 'a' && c <= 'f')) || ReferenceEquals(value, null)))
                throw new InvalidOperationException("Passive answer GUID is malformed or has no object.");
            return new JObject { ["object"] = identity(value), ["guid"] = guid };
        }
        private static JToken Types(string[] values, ref int remaining)
        {
            if (values == null) return JValue.CreateNull();
            if (values.Length > MaximumTypesPerList || values.Length > remaining) throw new InvalidOperationException("Passive direct metadata exceeds its bound.");
            remaining -= values.Length;
            foreach (var value in values)
                if (value != null && (value.Length == 0 || value.Length > MaximumTypeName)) throw new InvalidOperationException("Passive type name exceeds its bound.");
            return new JArray(values);
        }
        private static string ErrorText(Exception error)
        { var text = error.GetType().Name + ": " + error.Message; return text.Length <= 256 ? text : text.Substring(0, 256); }

        internal static RawState ReadNative(Game game, NativeDialogController controller, BlueprintDialog dialog, BlueprintCue cue)
        {
            try
            {
                var state = new RawState { Game = game, Controller = controller, Dialog = dialog, CurrentCue = cue,
                    Offered = new Offered[0], Rows = new Row[0], UiStatus = "missing-game" };
                if (game == null) return state;
                state.UiStatus = "missing-controller"; if (controller == null) return state;
                var offers = ReadField<List<BlueprintAnswer>>(typeof(NativeDialogController), controller, "m_Answers", 0x04005E67);
                if (offers == null || offers.Count > MaximumAnswers) throw new InvalidOperationException("Native offered list is absent or over bound.");
                var answers = offers.ToArray(); var remaining = MaximumDirectTypes;
                state.Offered = answers.Select(answer => ReadAnswer(answer, ref remaining)).ToArray();
                var ui = ReadField<UIAccess>(typeof(Game), game, "UI", 0x040006C6); state.Ui = ui;
                state.UiStatus = "missing-ui";
                if (ui != null)
                {
                    var common = ReadField<UICommon>(typeof(UIAccess), ui, "Common", 0x04001E97); state.Common = common;
                    state.UiStatus = "missing-common";
                    if (common != null)
                    {
                        var manager = ReadField<DialogBookEventUIManager>(typeof(UICommon), common, "DialogUIManager", 0x04001E9E); state.Manager = manager;
                        state.UiStatus = "missing-manager";
                        if (manager != null)
                        {
                            var kind = ReadField<DialogType>(typeof(DialogBookEventUIManager), manager, "m_State", 0x04002EBC);
                            state.ManagerKind = (int)kind;
                            state.ManagerShown = ReadField<bool>(typeof(DialogBookEventUIManager), manager, "m_IsShow", 0x04002EBD);
                            state.UiStatus = "unsupported-dialog-kind";
                            if (kind == DialogType.Common)
                            {
                                var view = ReadField<PcDialogController>(typeof(DialogBookEventUIManager), manager, "Dialog", 0x04002EB9); state.PcDialog = view;
                                state.UiStatus = "missing-pc-dialog";
                                if (view != null)
                                {
                                    state.PcDialogActive = ReadField<bool>(typeof(PcDialogController), view, "m_IsActive", 0x04002ECA);
                                    var part = ReadField<DialogCurrentPart>(typeof(PcDialogController), view, "CurrentPart", 0x04002EBE); state.CurrentPart = part;
                                    state.UiStatus = "missing-current-part";
                                    if (part != null)
                                    {
                                        var nativeRows = ReadField<List<TempOptionUI>>(typeof(DialogCurrentPart), part, "m_AnswersList", 0x04002ED8);
                                        if (nativeRows == null || nativeRows.Count > MaximumRows) throw new InvalidOperationException("Native PC answer rows are absent or over bound.");
                                        var rowRefs = nativeRows.ToArray(); state.Rows = rowRefs.Select(ReadRow).ToArray();
                                        RequireSameReferences(nativeRows, rowRefs, "PC rows");
                                        state.UiStatus = game.CurrentMode == GameModeType.Dialog && game.IsModeActive(GameModeType.Dialog) &&
                                            state.ManagerShown == true && state.PcDialogActive == true ? "pc-cached-rows" : "inactive-dialog";
                                    }
                                    // Common dialogs render a sole root system answer on the DialogueSystemButton, not a
                                    // TempOptionUI row (Fill skips IsSystem answers). Observe the button affordance passively.
                                    var systemButton = ReadField<DialogueSystemButton>(typeof(PcDialogController), view, "SystemButton", 0x04002EC0);
                                    state.SystemButton = systemButton;
                                    if (systemButton != null)
                                    {
                                        state.SystemButtonEnabled = ReadField<bool>(typeof(DialogueSystemButton), systemButton, "m_IsEnable", 0x04002EDA);
                                        state.SystemButtonAnswer = ReadField<BlueprintAnswer>(typeof(DialogueSystemButton), systemButton, "m_Answer", 0x04002EDC);
                                    }
                                    state.ContinueCue = ReadField<BlueprintCueBase>(typeof(NativeDialogController), controller, "m_ContinueCue", 0x04005E68);
                                    // Classify the offered answers against the four DialogRoot system answers by reference
                                    // identity only (no DialogExtensions.IsSystem predicate). BlueprintRoot.Instance is the
                                    // native single root; Game.Instance.BlueprintRoot resolves to the same instance.
                                    var root = BlueprintRoot.Instance;
                                    var dialogRoot = root == null ? null : ReadField<DialogRoot>(typeof(BlueprintRoot), root, "Dialog", 0x040060F5);
                                    if (dialogRoot != null)
                                    {
                                        var continueAnswer = ReadField<BlueprintAnswer>(typeof(DialogRoot), dialogRoot, "ContinueAnswer", 0x0400618E);
                                        var exitAnswer = ReadField<BlueprintAnswer>(typeof(DialogRoot), dialogRoot, "ExitAnswer", 0x0400618D);
                                        var interchapterContinueAnswer = ReadField<BlueprintAnswer>(typeof(DialogRoot), dialogRoot, "InterchapterContinueAnswer", 0x04006190);
                                        var interchapterExitAnswer = ReadField<BlueprintAnswer>(typeof(DialogRoot), dialogRoot, "InterchapterExitAnswer", 0x0400618F);
                                        foreach (var offered in state.Offered)
                                            if (offered != null)
                                                offered.IsSystemAnswer = ReferenceEquals(offered.Answer, continueAnswer) || ReferenceEquals(offered.Answer, exitAnswer) ||
                                                    ReferenceEquals(offered.Answer, interchapterContinueAnswer) || ReferenceEquals(offered.Answer, interchapterExitAnswer);
                                    }
                                }
                            }
                        }
                    }
                }
                RequireSameReferences(offers, answers, "offered answers");
                if (!ReferenceEquals(ReadField<NativeDialogController>(typeof(Game), game, "DialogController", 0x040006C7), controller) ||
                    !ReferenceEquals(ReadField<BlueprintDialog>(typeof(NativeDialogController), controller, "Dialog", 0x04005E5C), dialog) ||
                    !ReferenceEquals(ReadField<BlueprintCue>(typeof(NativeDialogController), controller, "m_CurrentCue", 0x04005E5A), cue))
                    throw new InvalidOperationException("Native dialogue context changed during passive choice read.");
                return state;
            }
            catch (Exception error) { return new RawState { Error = ErrorText(error) }; }
        }
        private static Offered ReadAnswer(BlueprintAnswer answer, ref int remaining)
        {
            if (answer == null) throw new InvalidOperationException("Native offered answer is null or destroyed.");
            var selection = ReadField<CharacterSelection>(typeof(BlueprintAnswer), answer, "CharacterSelection", 0x0400561C);
            return new Offered { Answer = answer, Guid = answer.AssetGuidThreadSafe,
                SelectionType = selection == null ? (int?)null : (int)ReadField<CharacterSelection.Type>(typeof(CharacterSelection), selection, "SelectionType", 0x040055FD),
                RequireValidCue = ReadField<bool>(typeof(BlueprintAnswer), answer, "RequireValidCue", 0x0400561F),
                ShowTypes = ReadConditions(ReadField<ConditionsChecker>(typeof(BlueprintAnswer), answer, "ShowConditions", 0x0400561D), ref remaining),
                SelectTypes = ReadConditions(ReadField<ConditionsChecker>(typeof(BlueprintAnswer), answer, "SelectConditions", 0x0400561E), ref remaining),
                ActionTypes = ReadActions(ReadField<ActionList>(typeof(BlueprintAnswer), answer, "OnSelect", 0x04005621), ref remaining) };
        }
        private static Row ReadRow(TempOptionUI row)
        {
            if (row == null) throw new InvalidOperationException("Native PC answer row is null or destroyed.");
            var answer = ReadField<BlueprintAnswer>(typeof(TempOptionUI), row, "m_Answer", 0x04002EE0);
            return new Row { View = row, Answer = answer, Guid = answer == null ? null : answer.AssetGuidThreadSafe,
                Index = ReadField<int>(typeof(TempOptionUI), row, "m_Index", 0x04002EE1),
                ChoosingCharacter = ReadField<bool>(typeof(TempOptionUI), row, "m_ChooseCharacterInit", 0x04002EE2),
                Initialized = ReadField<bool>(typeof(TempOptionUI), row, "m_IsInit", 0x04002EE3),
                Disabled = ReadField<bool>(typeof(TempOptionUI), row, "m_Disabled", 0x04002EE4),
                Interchapter = ReadField<bool>(typeof(TempOptionUI), row, "m_Interchapter", 0x04002EE8) };
        }
        private static string[] ReadConditions(ConditionsChecker value, ref int remaining)
        { return value == null ? null : ReadTypes(ReadField<Condition[]>(typeof(ConditionsChecker), value, "Conditions", 0x040054E0), ref remaining); }
        private static string[] ReadActions(ActionList value, ref int remaining)
        { return value == null ? null : ReadTypes(ReadField<GameAction[]>(typeof(ActionList), value, "Actions", 0x040054DD), ref remaining); }
        private static string[] ReadTypes(Array values, ref int remaining)
        {
            if (values == null) return null;
            if (values.Length > MaximumTypesPerList || values.Length > remaining) throw new InvalidOperationException("Native direct metadata exceeds its bound.");
            remaining -= values.Length; var result = new string[values.Length];
            for (var i = 0; i < values.Length; ++i)
            {
                var value = values.GetValue(i); result[i] = value == null ? null : value.GetType().FullName;
                if (result[i] != null && result[i].Length > MaximumTypeName) throw new InvalidOperationException("Native metadata type name exceeds its bound.");
            }
            return result;
        }
        private static void RequireSameReferences<T>(List<T> current, T[] captured, string label) where T : class
        {
            if (current.Count != captured.Length) throw new InvalidOperationException("Native " + label + " changed during capture.");
            for (var i = 0; i < captured.Length; ++i) if (!ReferenceEquals(current[i], captured[i])) throw new InvalidOperationException("Native " + label + " reordered during capture.");
        }
        private static T ReadField<T>(Type type, object instance, string name, int token)
        {
            if (type.Module.ModuleVersionId != new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7")) throw new InvalidOperationException("Installed passive choice module changed.");
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (field == null || field.MetadataToken != token || field.FieldType != typeof(T) || field.IsStatic)
                throw new InvalidOperationException("Installed passive choice field changed: " + name);
            return (T)field.GetValue(instance);
        }
        internal sealed class RawState
        {
            internal string Error, UiStatus;
            internal object Game, Controller, Dialog, CurrentCue, Ui, Common, Manager, PcDialog, CurrentPart;
            internal object SystemButton, SystemButtonAnswer, ContinueCue;
            internal int? ManagerKind;
            internal bool? ManagerShown, PcDialogActive, SystemButtonEnabled;
            internal Offered[] Offered;
            internal Row[] Rows;
        }
        internal sealed class Offered
        {
            internal object Answer; internal string Guid; internal int? SelectionType; internal bool RequireValidCue, IsSystemAnswer;
            internal string[] ShowTypes, SelectTypes, ActionTypes;
        }
        internal sealed class Row
        {
            internal object View, Answer; internal string Guid; internal int Index;
            internal bool Initialized, Disabled, ChoosingCharacter, Interchapter;
        }
    }
}
