using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.UI;
using Kingmaker.UI.Group;
using Kingmaker.UI.Overtip;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.Visual.Animation.Kingmaker;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace KingmakerMountedCombat.Diagnostics
{
    // F5 (PET-01, PET-02): the exploration Manual route with the Riding Dog pair, plus the companion naming and tack
    // observations. The producer names the stock Mammoth and the Riding Dog through the real sheet button and native dialog
    // (typing reaches the dialog's own text field through its key handler), inspects the tack unmounted and mounted, and
    // writes the ordinary Manual. The separate-process consumer reads the names, identities and tack after the native load.
    // Setup disclosed here: visual-only attack and cast animations with no rule, ability or resource.
    internal sealed partial class Chunk7PersistenceScenario
    {
        internal const string CompanionNamesCaseName = "companion-names-tack-manual";
        internal const string SharedName = "Ébène l'Ancien", DogDistinctName = "Biscuit", CancelledName = "Nobody", MarkupName = "<b>Rex</b>",
            BlankName = "   ", LongName = "Wolfgang Maximilian Wynn";
        private static readonly MethodInfo InputFieldSelected = typeof(KeyboardAccess).GetMethod("IsInputFieldSelected", BindingFlags.Static | BindingFlags.NonPublic) is MethodInfo m && m.MetadataToken == 0x06002DFB ? m : null;
        private bool CompanionNamesCase => request.PersistenceCase == CompanionNamesCaseName || RiderFallCase;
        private readonly JArray f5Shots = new JArray();
        private readonly JObject f5 = new JObject();
        private List<KeyValuePair<string, Func<bool>>> f5Steps;
        private int f5Index, f5Frames;
        private float f5At;
        private bool f5ShotPending, f5NamesDone, f5Started;
        private Action f5Then;

        private CompanionNameService Names => CompanionNameService.Current;
        private CompanionRenameSheetControl Sheet => Names.Sheet;

        // ---- Producer, before the mounts: tack unmounted, then naming through the sheet and dialog. ----
        private void BeginCompanionNamesUnmounted()
        {
            Require(Names != null && RidingDogTackService.Current != null && CompanionRenameSheetControl.ContractAvailable && Names.Dialog.Available && InputFieldSelected != null,
                "The F5 naming or tack service, or a native UI contract, is unavailable.");
            evidence["companionNames"] = f5;
            f5["dog"] = Identity(secondMount); f5["mammoth"] = Identity(firstMount);
            f5["shots"] = f5Shots;
            var steps = new List<KeyValuePair<string, Func<bool>>>();
            Step(steps, "tack-unmounted", () => TackAdopted(secondMount) && Record("tackUnmounted", Tack()));
            Step(steps, "specimen-unmounted", () => Specimen("unmounted-idle"));
            Step(steps, "shots-unmounted", () => World("f5-dog-unmounted-side", 90f) && World("f5-dog-unmounted-other", 270f));
            Step(steps, "bite-unmounted", () => Bite("f5-dog-bite-unmounted"));
            Step(steps, "sheet-mammoth", () => OpenSheet(firstMount));
            Step(steps, "sheet-mammoth-shot", () => Ui("f5-sheet-mammoth") && Record("sheetMammoth", SheetState()));
            Step(steps, "rename-open", () => PressRename() && Record("dialogOpen", DialogState()) && Ui("f5-dialog-open"));
            Step(steps, "rename-type", () => TypeText(SharedName) && Ui("f5-dialog-typed"));
            Step(steps, "rename-commit", () => Commit(firstMount, SharedName, "mammothRenamed"));
            Step(steps, "sheet-renamed-shot", () => Ui("f5-sheet-mammoth-renamed") && Record("sheetMammothRenamed", SheetState()));
            Step(steps, "cancel-open", () => PressRename());
            Step(steps, "cancel-type", () => TypeText(CancelledName));
            Step(steps, "cancel-esc", () => EscField() && Unchanged(firstMount, SharedName, "cancelled") && Record("cancelReleasedKeyboard", !(bool)InputFieldSelected.Invoke(null, null)));
            Step(steps, "cancel-button-open", () => PressRename());
            Step(steps, "cancel-button-type", () => TypeText(CancelledName));
            Step(steps, "cancel-button", () => PressDialog(false) && Unchanged(firstMount, SharedName, "cancelButton"));
            Step(steps, "markup-open", () => PressRename());
            Step(steps, "markup-type", () => TypeText(MarkupName));
            Step(steps, "markup-commit", () => CommitOnly() && Ui("f5-invalid-message") && Record("markupMessageShown", DialogMessageBox.Instance.IsShown));
            Step(steps, "markup-dismiss", () => EscBox());
            Step(steps, "markup-reopened", () => NativeTextDialog.IsShown && Settled(10) && Record("markupReopenedWith", Typed()));
            Step(steps, "markup-reopened-esc", () => EscField() && Unchanged(firstMount, SharedName, "markupRefused"));
            // A blank entry is refused like any invalid one; it never restores the default name.
            Step(steps, "blank-open", () => PressRename());
            Step(steps, "blank-type", () => TypeText(BlankName));
            Step(steps, "blank-commit", () => CommitOnly() && Ui("f5-blank-message") && Record("blankMessageShown", DialogMessageBox.Instance.IsShown));
            Step(steps, "blank-dismiss", () => EscBox());
            Step(steps, "blank-reopened", () => NativeTextDialog.IsShown && Settled(10) && Record("blankReopenedWith", Typed()));
            Step(steps, "blank-reopened-esc", () => EscField() && Unchanged(firstMount, SharedName, "blankRefused"));
            Step(steps, "long-open", () => PressRename());
            Step(steps, "long-type", () => TypeText(LongName));
            Step(steps, "long-commit", () => Commit(firstMount, LongName, "mammothLong"));
            Step(steps, "sheet-long-shot", () => Ui("f5-sheet-mammoth-long") && Record("sheetMammothLong", SheetState()));
            Step(steps, "rename-again-open", () => PressRename());
            Step(steps, "rename-again-type", () => TypeText(SharedName));
            Step(steps, "rename-again-commit", () => Commit(firstMount, SharedName, "mammothRenamedAgain"));
            Step(steps, "sheet-rider", () => SwitchSheet(secondRider, null) && Record("nonPetHidden", Sheet.RenameButton == null || !Sheet.RenameButton.activeSelf) &&
                Record("riderNameUnstyled", Sheet.ReservedMargin == null));
            Step(steps, "sheet-dog", () => SwitchSheet(secondMount, secondMount));
            Step(steps, "dog-distinct-open", () => PressRename());
            Step(steps, "dog-distinct-type", () => TypeText(DogDistinctName));
            Step(steps, "dog-distinct-commit", () => Commit(secondMount, DogDistinctName, "dogDistinct") && Record("distinctLabels", Labels()));
            Step(steps, "dog-distinct-shot", () => Ui("f5-sheet-dog-distinct") && Record("sheetDog", SheetState()));
            Step(steps, "dog-duplicate-open", () => PressRename());
            Step(steps, "dog-duplicate-type", () => TypeText(SharedName));
            Step(steps, "dog-duplicate-commit", () => Commit(secondMount, SharedName, "dogDuplicate") && Record("duplicateLabels", Labels()));
            Step(steps, "dog-duplicate-shot", () => Ui("f5-sheet-dog-duplicate"));
            Step(steps, "sheet-close", () => CloseSheet());
            StartSteps(steps, () => { f5NamesDone = true; Next(1); });
        }

        // ---- Producer, after both mounts and before the save: the tack under the rider, in motion and in the action poses. ----
        private void BeginCompanionNamesMounted()
        {
            var steps = new List<KeyValuePair<string, Func<bool>>>();
            Step(steps, "tack-mounted", () => Mounted(secondRider, secondMount) && Record("tackMounted", Tack()));
            Step(steps, "specimen-mounted", () => Specimen("mounted-idle"));
            Step(steps, "shots-mounted", () => World("f5-dog-mounted-side", 90f) && World("f5-dog-mounted-other", 270f));
            Step(steps, "cast", () => CastPose());
            Step(steps, "cast-shot", () => Time.time >= f5At && World("f5-dog-mounted-cast", 90f));
            Step(steps, "cast-settle", () => Time.time >= f5At + 1.5f);
            Step(steps, "bite-mounted", () => Bite("f5-dog-mounted-bite"));
            Step(steps, "walk", () => Walk());
            Step(steps, "specimen-walk", () => Time.time >= f5At && Specimen("mounted-walk"));
            Step(steps, "walk-shot", () => Time.time >= f5At && World("f5-dog-mounted-turn", 60f));
            Step(steps, "walk-settle", () => secondMount.View?.AgentASP?.IsReallyMoving != true && secondMount.Commands.Empty && secondRider.Commands.Empty);
            Step(steps, "at-save", () => Mounted(secondRider, secondMount) && Record("tackAfterMotion", Tack()) && Record("atSave", Labels()));
            Step(steps, "overtips-at-save", () => OvertipsReady() && Record("overtipsAtSave", Overtips()));
            StartSteps(steps, AfterNativeMounts);
        }

        // Since F2 a rider can also hold the one public Mount / Dismount control (kind 6) and its one-placement record (kind 7,
        // index 0, not a hotbar slot), and a saved hidden Dismount binding (kind 2) is collapsed into that public control (r02).
        // So this case binds only Rider Primary (kind 3), exactly once per rider; nothing else may appear (rows recorded).
        private void RequirePostF2Slots()
        {
            f5["slotsAtSave"] = new JArray(slots.Select(s => new JObject { ["actor"] = s.ActorId, ["index"] = s.Index, ["kind"] = s.Kind }));
            f5["managedHotbarSlotsAtSave"] = controls.CaptureSnapshot().ManagedHotbarSlotCount;
            foreach (var rider in new[] { firstRider, secondRider })
            {
                var own = slots.Where(s => s.ActorId == rider.UniqueId).ToArray();
                Require(own.Count(s => s.Kind == 2) == 0 && own.Count(s => s.Kind == 3) == 1 && own.Count(s => s.Kind == 6) <= 1 &&
                    own.Count(s => s.Kind == 7) <= 1 && own.All(s => s.Kind == 3 || s.Kind == 6 || s.Kind == 7 && s.Index == 0),
                    "All-pair native hotbar binding differs.");
            }
            Require(slots.All(s => s.ActorId == firstRider.UniqueId || s.ActorId == secondRider.UniqueId), "All-pair native hotbar binding differs.");
        }

        // ---- Consumer: names, identities and the tack after the separate-process native load. ----
        private void RecordCompanionNamesCold()
        {
            evidence["companionNamesCold"] = f5;
            f5["dog"] = Identity(secondMount); f5["mammoth"] = Identity(firstMount);
            f5["labels"] = Labels();
            f5["tackCold"] = Tack();
            f5["shots"] = f5Shots;
        }

        private void BeginCompanionNamesColdSheet()
        {
            var steps = new List<KeyValuePair<string, Func<bool>>>();
            Step(steps, "overtips-cold", () => OvertipsReady() && Record("overtipsCold", Overtips()));
            Step(steps, "specimen-cold", () => Specimen("cold-idle"));
            Step(steps, "tack-remounted", () => Mounted(secondRider, secondMount) && Record("tackRemounted", Tack()));
            Step(steps, "sheet-dog", () => OpenSheet(secondMount));
            Step(steps, "sheet-dog-shot", () => Ui("f5-cold-sheet-dog") && Record("sheetDogCold", SheetState()));
            Step(steps, "sheet-mammoth", () => SwitchSheet(firstMount, firstMount));
            Step(steps, "sheet-mammoth-shot", () => Ui("f5-cold-sheet-mammoth") && Record("sheetMammothCold", SheetState()));
            Step(steps, "sheet-close", () => CloseSheet());
            StartSteps(steps, BeginCleanup);
        }

        private void UpdateCompanionNames()
        {
            if (f5ShotPending) return;
            if (f5Index >= f5Steps.Count) return;
            var step = f5Steps[f5Index];
            f5Frames++;
            if (!step.Value()) return;
            f5Index++; f5Frames = 0; Next(stage);
            if (f5Index < f5Steps.Count) return;
            var then = f5Then; f5Then = null; f5Steps = null;
            then();
        }

        private void StartSteps(List<KeyValuePair<string, Func<bool>>> steps, Action then)
        {
            f5Steps = steps; f5Index = 0; f5Frames = 0; f5Then = then; f5Started = true;
        }

        private static void Step(List<KeyValuePair<string, Func<bool>>> steps, string name, Func<bool> body) => steps.Add(new KeyValuePair<string, Func<bool>>(name, body));
        private string CurrentStep => f5Steps != null && f5Index < f5Steps.Count ? f5Steps[f5Index].Key : null;

        private bool Record(string key, JToken value) { f5[key] = value; return true; }

        // A few frames between actions, as a player's input would leave; a step that waits on the game holds its own frames.
        private bool Settled(int frames = 8) => f5Frames >= frames;

        private static JObject Identity(UnitEntityData unit) => new JObject
        {
            ["id"] = unit.UniqueId, ["blueprint"] = unit.Blueprint?.AssetGuid, ["master"] = unit.Descriptor.Master.Value?.UniqueId,
            ["masterOwnsPet"] = ReferenceEquals(unit.Descriptor.Master.Value?.Descriptor.Pet, unit),
            ["customName"] = unit.Descriptor.CustomName, ["characterName"] = unit.CharacterName, ["defaultName"] = CompanionNameService.DefaultName(unit),
            ["eligible"] = CompanionNameService.Refusal(unit) == null, ["size"] = unit.Descriptor.State.Size.ToString(),
            ["armorClass"] = unit.Stats.AC.ModifiedValue, ["speed"] = unit.Stats.Speed.ModifiedValue, ["corpulence"] = unit.Corpulence
        };

        private JObject Labels() => new JObject
        {
            ["dog"] = new JObject { ["id"] = secondMount.UniqueId, ["characterName"] = secondMount.CharacterName, ["customName"] = secondMount.Descriptor.CustomName },
            ["mammoth"] = new JObject { ["id"] = firstMount.UniqueId, ["characterName"] = firstMount.CharacterName, ["customName"] = firstMount.Descriptor.CustomName },
            ["writes"] = Names.Writes, ["lastOutcome"] = Names.LastOutcome
        };

        // The ordinary in-world name label: each companion's native overtip, which sets its text from CharacterName on spawn, view
        // attach and IUnitNameHandler (with a decorative first letter in rich text). Read, never written; visibility is not forced.
        private static readonly FieldInfo OvertipControllers = typeof(OvertipManager).GetField("m_UnitControllers", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo OvertipName = typeof(OvertipControllerBase).GetField("Name", BindingFlags.Instance | BindingFlags.Public);

        private static OvertipController OvertipOf(UnitEntityData unit)
        {
            var manager = Game.Instance.UI.BarkManager as OvertipManager;
            if (manager == null) return null;
            var controllers = OvertipControllers?.GetValue(manager) as IDictionary;
            return controllers != null && controllers.Contains(unit) ? controllers[unit] as OvertipController : null;
        }

        // This TMP's SetText(string) fills the character buffer it renders from and never stores the string in m_text (the `text`
        // getter), so the drawn label is read back from that buffer up to its terminator (TMP_Text.m_char_buffer, exact token).
        private static string RenderedText(object label)
        {
            for (var type = label?.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField("m_char_buffer", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field == null) continue;
                if (field.MetadataToken != 0x040014C2 || !(field.GetValue(label) is int[] buffer)) return null;
                var text = new System.Text.StringBuilder();
                foreach (var code in buffer)
                {
                    if (code == 0) break;
                    if (code > 0xFFFF && code <= 0x10FFFF) text.Append(char.ConvertFromUtf32(code)); else text.Append((char)code);
                }
                return text.ToString();
            }
            return null;
        }

        private bool OvertipsReady() => (OvertipOf(secondMount) != null && OvertipOf(firstMount) != null && Settled(4)) || f5Frames > 300;

        private JObject Overtips()
        {
            JObject Row(UnitEntityData unit)
            {
                var controller = OvertipOf(unit);
                var label = controller == null ? null : OvertipName?.GetValue(controller);
                return new JObject
                {
                    ["id"] = unit.UniqueId, ["controllerUnit"] = controller?.Unit?.UniqueId, ["characterName"] = unit.CharacterName,
                    ["text"] = RenderedText(label), ["serializedText"] = label?.GetType().GetProperty("text")?.GetValue(label, null) as string
                };
            }
            return new JObject { ["dog"] = Row(secondMount), ["mammoth"] = Row(firstMount), ["writes"] = Names.Writes, ["lastOutcome"] = Names.LastOutcome };
        }

        private bool TackAdopted(UnitEntityData unit)
        {
            var bindings = RidingDogTackService.Bindings(unit.View);
            return bindings.Length == 1 && bindings[0].AdoptedByView && Settled(15);
        }

        private static JToken Finite(float? value) => value == null || float.IsNaN(value.Value) || float.IsInfinity(value.Value) ? JValue.CreateNull() : new JValue(value.Value);

        private JObject Tack()
        {
            var dog = RidingDogTackService.Bindings(secondMount.View);
            var mammoth = RidingDogTackService.Bindings(firstMount.View);
            var binding = dog.FirstOrDefault();
            var fit = binding?.Tack?.Fit;
            return new JObject
            {
                ["dogBindings"] = dog.Length, ["mammothBindings"] = mammoth.Length, ["unitBound"] = binding?.UnitId,
                ["adopted"] = binding?.AdoptedByView ?? false, ["rendererEnabled"] = binding?.Renderer != null && binding.Renderer.enabled,
                ["visible"] = binding?.Renderer != null && binding.Renderer.isVisible,
                ["sameBones"] = binding?.Renderer != null && binding.Source != null && binding.Renderer.bones.SequenceEqual(binding.Source.bones),
                ["source"] = binding?.Source?.name, ["shader"] = binding?.Tack?.Shader,
                ["replacedTextures"] = new JArray(binding?.Tack?.ReplacedTextures ?? new string[0]), ["keptTextures"] = new JArray(binding?.Tack?.KeptTextures ?? new string[0]),
                ["vertices"] = fit?.VertexCount ?? 0, ["triangles"] = (fit?.Triangles.Length ?? 0) / 3, ["casts"] = fit?.Casts ?? 0, ["misses"] = fit?.Misses ?? 0,
                ["backAtSeat"] = fit?.BackHeightAtSeat ?? 0f, ["seatTop"] = fit?.SeatTopAtSeat ?? 0f, ["cantleTop"] = fit?.CantleTop ?? 0f, ["pommelTop"] = fit?.PommelTop ?? 0f,
                ["maxRise"] = fit?.MaxRiseAboveBack ?? 0f, ["minClearance"] = Finite(fit?.MinClearance), ["maxClearance"] = Finite(fit?.MaxClearance), ["unsupported"] = fit?.Unsupported ?? -1,
                ["failure"] = binding?.Tack?.Failure, ["fitMilliseconds"] = binding?.Tack?.FitMilliseconds ?? 0.0,
                ["attachments"] = RidingDogTackService.Current.Attachments, ["meshBuilds"] = RidingDogTackService.Current.MeshBuilds,
                ["dogStats"] = Identity(secondMount)
            };
        }

        // ---- Sheet and dialog, through their own native paths. ----
        private bool OpenSheet(UnitEntityData unit)
        {
            if (!f5Started) return false;
            if (f5Frames == 1)
            {
                SelectionManager.Instance.SelectUnit(unit.View, true, true, false);
                return false;
            }
            if (f5Frames == 3) EventBus.RaiseEvent(delegate (IServiceWindowUIHandler handler) { handler.HandleOpenCharScreen(); });
            if (f5Frames < 12) return false;
            if (!ReferenceEquals(Sheet.Shown, unit)) { if (f5Frames == 12 && GroupController.Instance != null) GroupController.Instance.SelectUnit(unit); return false; }
            return Settled(20) && Sheet.RenameButton != null && Sheet.RenameButton.activeInHierarchy;
        }

        private bool SwitchSheet(UnitEntityData unit, UnitEntityData expectedShown)
        {
            if (f5Frames == 1) GroupController.Instance.SelectUnit(unit);
            return Settled(12) && ReferenceEquals(Sheet.Shown, expectedShown);
        }

        private bool CloseSheet()
        {
            if (f5Frames == 1) EventBus.RaiseEvent(delegate (IServiceWindowUIHandler handler) { handler.HandleOpenCharScreen(); });
            if (!Settled(20)) return false;
            var closed = Sheet.RenameButton == null || !Sheet.RenameButton.activeInHierarchy;
            Require(closed || f5Frames < 200, "The character sheet did not close.");
            return closed && Record("sheetClosed", true);
        }

        // The one control and where it sits, in the sheet canvas's own space: the line it belongs to, the name's rendered text,
        // and every visible level/experience text. Overlaps are measured on rendered text, not on the native rects.
        private JObject SheetState()
        {
            var label = Sheet.NameLabel;
            var button = Sheet.RenameButton;
            var state = new JObject
            {
                ["shown"] = Sheet.Shown?.UniqueId, ["renameActive"] = button != null && button.activeInHierarchy,
                ["renameLabel"] = Sheet.RenameLabel, ["layout"] = Sheet.Layout,
                ["buttons"] = label == null ? 0 : label.GetComponentsInChildren<Transform>(true).Count(t => t.name == "KMC_RenameCompanion"),
                ["reservedMargin"] = Sheet.ReservedMargin is Vector4 margin ? new JArray(margin.x, margin.y, margin.z, margin.w) : null
            };
            var level = Sheet.Screen?.Level;
            if (label == null || button == null || level == null) return state;
            var textType = label.GetType();
            while (textType != null && textType.FullName != "TMPro.TMP_Text") textType = textType.BaseType;
            var line = WorldRect((RectTransform)label.transform);
            var control = WorldRect((RectTransform)button.transform);
            var name = TextRect(label, textType);
            var levelTexts = level.GetComponentsInChildren(textType, false)
                .Where(text => !string.IsNullOrEmpty(textType.GetProperty("text").GetValue(text, null) as string))
                .Select(text => TextRect(text, textType)).ToArray();
            const float tolerance = 0.5f;
            state["line"] = RectJson(line); state["control"] = RectJson(control); state["nameText"] = RectJson(name);
            state["levelTexts"] = new JArray(levelTexts.Select(RectJson));
            state["controlInsideLineHorizontally"] = control.xMin >= line.xMin - tolerance && control.xMax <= line.xMax + tolerance;
            state["controlCentredOnLine"] = Math.Abs(control.center.y - line.center.y) <= tolerance;
            state["textClearOfControl"] = name.xMax <= control.xMin + tolerance;
            state["controlClearOfLevel"] = levelTexts.Length > 0 && levelTexts.All(text => !text.Overlaps(control));
            state["textClearOfLevel"] = levelTexts.Length > 0 && levelTexts.All(text => !text.Overlaps(name));
            state["textOverflowing"] = Convert.ToBoolean(textType.GetProperty("isTextOverflowing").GetValue(label, null));
            state["fontSize"] = Convert.ToSingle(textType.GetProperty("fontSize").GetValue(label, null));
            return state;
        }

        private static Rect WorldRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners.Min(c => c.x), corners.Min(c => c.y), corners.Max(c => c.x), corners.Max(c => c.y));
        }

        private static Rect TextRect(Component text, Type textType)
        {
            var bounds = (Bounds)textType.GetProperty("textBounds").GetValue(text, null);
            var min = text.transform.TransformPoint(bounds.min);
            var max = text.transform.TransformPoint(bounds.max);
            return Rect.MinMaxRect(Math.Min(min.x, max.x), Math.Min(min.y, max.y), Math.Max(min.x, max.x), Math.Max(min.y, max.y));
        }

        private static JObject RectJson(Rect rect) => new JObject { ["xMin"] = rect.xMin, ["yMin"] = rect.yMin, ["xMax"] = rect.xMax, ["yMax"] = rect.yMax };

        private bool PressRename() => Press(Sheet.RenameButton) && WaitDialog();
        private bool Press(GameObject button)
        {
            if (f5Frames == 1 && !NativeTextDialog.IsShown) Require(CompanionRenameSheetControl.Press(button), "A sheet button could not be pressed.");
            return Settled(6) && NativeTextDialog.IsShown;
        }
        private bool WaitDialog() => NativeTextDialog.IsShown && Settled(10);

        private JObject DialogState()
        {
            var input = Names.Dialog.LiveTextInput();
            return new JObject
            {
                ["shown"] = NativeTextDialog.IsShown, ["prefilled"] = Typed(), ["hotkeysSuspended"] = (bool)InputFieldSelected.Invoke(null, null),
                ["inputFocused"] = input != null && Convert.ToBoolean(input.GetType().GetProperty("isFocused").GetValue(input, null)),
                ["actorCommandsIdle"] = ActorsIdle()
            };
        }

        private bool ActorsIdle() => new[] { firstRider, firstMount, secondRider, secondMount }.All(unit => unit.Commands.Empty);

        private string Typed()
        {
            var input = Names.Dialog.LiveTextInput();
            return input?.GetType().GetProperty("text").GetValue(input, null) as string;
        }

        // Keystrokes through the field's own key handler: End, enough Backspaces to clear it, then each character.
        private bool TypeText(string text)
        {
            if (!Settled(4)) return false;
            if (f5Frames > 4) return Typed() != null && Settled(10);
            var input = Names.Dialog.LiveTextInput();
            Require(input != null, "The native text field is not live.");
            var process = input.GetType().GetMethod("ProcessEvent", new[] { typeof(Event) });
            var before = ActorsIdle();
            // Select all, then one Backspace: a selection delete does not depend on the caret or the field's editing options.
            process.Invoke(input, new object[] { Event.KeyboardEvent("^a") });
            process.Invoke(input, new object[] { Event.KeyboardEvent("backspace") });
            foreach (var character in text)
                process.Invoke(input, new object[] { new Event { type = EventType.KeyDown, character = character, keyCode = KeyCode.None } });
            // The field's own update loop refreshes its label once it has consumed keys; this is that native step.
            input.GetType().GetMethod("ForceLabelUpdate", Type.EmptyTypes).Invoke(input, null);
            var typed = f5["typing"] as JArray ?? new JArray();
            typed.Add(new JObject { ["intended"] = text, ["field"] = Typed(), ["hotkeysSuspended"] = (bool)InputFieldSelected.Invoke(null, null), ["actorsIdleBefore"] = before, ["actorsIdleAfter"] = ActorsIdle() });
            f5["typing"] = typed;
            return false;
        }

        // OK: the field ends its edit as the button takes focus, then the button's own click.
        private bool CommitOnly()
        {
            if (f5Frames == 1)
            {
                var input = Names.Dialog.LiveTextInput();
                input.GetType().GetMethod("DeactivateInputField", Type.EmptyTypes).Invoke(input, null);
                return false;
            }
            if (f5Frames == 3) Require(PressNative(Names.Dialog.Button(true)), "The dialog's OK button could not be pressed.");
            return Settled(10);
        }

        private bool Commit(UnitEntityData unit, string expected, string key)
        {
            if (!CommitOnly()) return false;
            Require(!NativeTextDialog.IsShown, "The dialog stayed open after OK.");
            Require(unit.Descriptor.CustomName == expected && unit.CharacterName == expected, "The companion's name is " + unit.Descriptor.CustomName + ", not " + expected + ".");
            return Record(key, Identity(unit));
        }

        private bool PressDialog(bool yes)
        {
            if (f5Frames == 1) Require(PressNative(Names.Dialog.Button(yes)), "A dialog button could not be pressed.");
            return Settled(10) && !NativeTextDialog.IsShown;
        }

        private static bool PressNative(Component button)
        {
            if (button == null) return false;
            var click = button.GetType().GetProperty("onClick").GetValue(button, null) as UnityEngine.Events.UnityEvent;
            if (click == null) return false;
            click.Invoke();
            return true;
        }

        // Esc in the focused name field: the field's own key handler takes it as a cancel and ends the edit (what its
        // OnUpdateSelected does with that key); the product then closes the box through the box's Esc handler.
        private bool EscField()
        {
            if (f5Frames == 1)
            {
                var input = Names.Dialog.LiveTextInput();
                Require(input != null && Convert.ToBoolean(input.GetType().GetProperty("isFocused").GetValue(input, null)), "The name field is not focused for Esc.");
                input.GetType().GetMethod("ProcessEvent", new[] { typeof(Event) }).Invoke(input, new object[] { Event.KeyboardEvent("escape") });
                input.GetType().GetMethod("DeactivateInputField", Type.EmptyTypes).Invoke(input, null);
            }
            return Settled(10);
        }

        // Esc on the message box, which holds no field: native KeyboardAccess runs the escape binding, whose handler is this.
        private bool EscBox()
        {
            if (f5Frames == 1) DialogMessageBox.Instance.OnEscPressed();
            return Settled(10);
        }

        private bool Unchanged(UnitEntityData unit, string expected, string key)
        {
            Require(!NativeTextDialog.IsShown, "The dialog stayed open after Esc.");
            Require(unit.Descriptor.CustomName == expected, "A cancelled or refused entry changed the name to " + unit.Descriptor.CustomName + ".");
            return Record(key, Identity(unit));
        }

        // ---- Visual-only poses and motion. ----
        private bool Bite(string shot)
        {
            if (f5Frames == 1)
            {
                var manager = secondMount.View?.AnimationManager;
                var handle = manager?.CreateHandle(UnitAnimationType.MainHandAttack, false) ?? manager?.CreateHandle(UnitAnimationType.SpecialAttack, false);
                f5[shot + "-animation"] = handle == null ? "none" : handle.Action?.Type.ToString();
                if (handle != null) manager.Execute(handle);
                f5At = Time.time + 0.3f;
                return false;
            }
            return Time.time >= f5At && World(shot, 90f);
        }

        // The rider's own cast animation only: no ability, rule, spell slot or command.
        private bool CastPose()
        {
            var manager = secondRider.View?.AnimationManager;
            var handle = manager?.CreateHandle(UnitAnimationType.CastSpell, false);
            f5["castAnimation"] = handle == null ? "none" : handle.Action?.Type.ToString();
            if (handle != null) manager.Execute(handle);
            f5At = Time.time + 0.5f;
            return true;
        }

        // An ordinary selected-party ground order, three metres to the pair's right: the pair turns and walks.
        private bool Walk()
        {
            SelectionManager.Instance.SelectUnit(secondRider.View, true, true, false);
            var goal = secondMount.Position + secondMount.View.transform.right * 3f + secondMount.View.transform.forward * 1f;
            ClickGroundHandler.MoveSelectedUnitsToPoint(goal, false);
            f5At = Time.time + 0.4f;
            return true;
        }

        // ---- Captures: the gameplay camera for the world, the whole screen for the sheet and dialog. ----
        private bool World(string name, float yaw)
        {
            if (f5Shots.Any(row => row.Value<string>("name") == name)) return true;
            if (f5ShotPending) return false;
            f5ShotPending = true;
            UnityModManagerNet.UnityModManager.UI.Instance.StartCoroutine(Sprint5Fixture.Capture(request.EvidenceRoot, f5Shots, name, secondMount, secondRider, secondMount,
                yaw, 1.3f, 6.3f, 24f, () => f5ShotPending = false));
            return false;
        }

        private bool Ui(string name)
        {
            if (f5Shots.Any(row => row.Value<string>("name") == name)) return true;
            if (f5ShotPending || !Settled(6)) return false;
            f5ShotPending = true;
            UnityModManagerNet.UnityModManager.UI.Instance.StartCoroutine(ScreenCapture(name));
            return false;
        }

        private IEnumerator ScreenCapture(string name)
        {
            yield return new WaitForEndOfFrame();
            var row = new JObject { ["name"] = name, ["frame"] = Time.frameCount, ["screen"] = true };
            try
            {
                var texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
                byte[] bytes;
                try
                {
                    texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
                    texture.Apply(false);
                    bytes = texture.EncodeToPNG();
                }
                finally { Object.Destroy(texture); }
                var folder = Path.Combine(request.EvidenceRoot, "movement-visuals"); Directory.CreateDirectory(folder);
                var file = "finalize5-" + name + ".png";
                File.WriteAllBytes(Path.Combine(folder, file), bytes);
                using (var algorithm = SHA256.Create())
                    row["sha256"] = BitConverter.ToString(algorithm.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
                row["file"] = "movement-visuals/" + file; row["bytes"] = bytes.Length; row["width"] = Screen.width; row["height"] = Screen.height;
            }
            catch (Exception exception) { row["error"] = exception.GetType().Name + ": " + exception.Message; }
            f5Shots.Add(row);
            f5ShotPending = false;
        }
    }
}
