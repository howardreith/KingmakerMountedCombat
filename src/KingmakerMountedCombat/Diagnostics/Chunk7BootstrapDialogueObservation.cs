using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Controllers.Dialog;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.DialogSystem.State;
using Kingmaker.GameModes;
using Kingmaker.UI;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Raw, bounded snapshots only. The native engine is never driven by these
    // observations. The host retains this recorder through its terminal sample.
    internal sealed class Chunk7BootstrapDialogueObservation : IDisposable
    {
        internal const string Contract = "chunk7-bootstrap-dialogue-raw-v3";
        internal const int MaximumSnapshots = 4;
        // Four phases retain ten existing and at most 101 PC-choice references each.
        internal const int IdentityCapacity = MaximumSnapshots * (10 + Chunk7DialogueChoiceObservation.AdditionalIdentitiesPerSnapshot);
        private static readonly string[] Phases = { "bootstrap-readiness", "cleanup-entry", "cleanup-settlement", "host-terminal" };
        private readonly RetainedObjectIdentity identities;
        private readonly JArray rows = new JArray();
        private readonly List<string> errors = new List<string>();
        private int lastPhase = -1;
        private JObject terminal;

        internal Chunk7BootstrapDialogueObservation(Func<object, int> hash = null)
        { identities = new RetainedObjectIdentity(IdentityCapacity, hash); }
        internal bool Released => identities.Released;

        internal void Observe(string phase, Game game)
        { Observe(phase, Time.frameCount, () => ReadNative(game)); }

        internal void Observe(string phase, int frame, Func<RawState> read)
        {
            try
            {
                var ordinal = Array.IndexOf(Phases, phase);
                if (ordinal < 0 || frame < 0) throw new InvalidOperationException("Unknown passive phase or negative frame.");
                if (rows.Any(existing => (string)existing["phase"] == phase)) return;
                if (identities.Released || ordinal <= lastPhase || rows.Count == MaximumSnapshots)
                    throw new InvalidOperationException("Passive observation is released, reordered or full.");
                var row = EmptyRow(phase, frame);
                try
                {
                    var value = read();
                    if (value != null && value.TopMode != null && value.TopMode.Length > 64) throw new InvalidOperationException("Native mode label exceeded its passive bound.");
                    if (value == null) throw new InvalidOperationException("Native passive read returned no state.");
                    row["game"] = identities.Get(value.Game); row["controller"] = identities.Get(value.Controller);
                    row["topMode"] = value.TopMode; row["dialogActive"] = value.DialogActive;
                    row["cutsceneLock"] = value.CutsceneLock;
                    row["dialog"] = Blueprint(value.Dialog, value.DialogGuid);
                    row["currentCue"] = Blueprint(value.CurrentCue, value.CurrentCueGuid);
                    row["queuedCue"] = Blueprint(value.QueuedCue, value.QueuedCueGuid);
                    row["continueCue"] = Blueprint(value.ContinueCue, value.ContinueCueGuid);
                    row["cuePlayScheduled"] = value.CuePlayScheduled; row["dialogStopScheduled"] = value.DialogStopScheduled;
                    row["dialogState"] = identities.Get(value.DialogState); row["scheduledStart"] = identities.Get(value.ScheduledStart);
                    row["scheduledStartIsScheduled"] = value.ScheduledStartIsScheduled;
                    row["scheduledDialog"] = Blueprint(value.ScheduledDialog, value.ScheduledDialogGuid);
                    row["messageBox"] = identities.Get(value.MessageBox); row["messageBoxShown"] = value.MessageBoxShown;
                    row["messageBoxType"] = value.MessageBoxType;
                    row["choices"] = Chunk7DialogueChoiceObservation.Capture(value.Choices, identities.Get);
                    row["readSucceeded"] = true;
                }
                catch (Exception error) { row = EmptyRow(phase, frame); row["readError"] = ErrorText(error); }
                rows.Add(row); lastPhase = ordinal;
            }
            catch (Exception error) { if (errors.Count < MaximumSnapshots) errors.Add(ErrorText(error)); }
        }

        private JObject Blueprint(object value, string guid)
        {
            if (guid != null && (guid.Length > 64 || ReferenceEquals(value, null)))
                throw new InvalidOperationException("Native blueprint label exceeded its passive bound or lost its object.");
            return new JObject { ["object"] = identities.Get(value), ["guid"] = guid };
        }
        private static JObject EmptyRow(string phase, int frame)
        {
            return new JObject { ["phase"] = phase, ["frame"] = frame, ["readSucceeded"] = false, ["readError"] = null,
                ["game"] = null, ["controller"] = null, ["topMode"] = null, ["dialogActive"] = null, ["cutsceneLock"] = null,
                ["dialog"] = null, ["currentCue"] = null, ["queuedCue"] = null, ["continueCue"] = null,
                ["cuePlayScheduled"] = null, ["dialogStopScheduled"] = null,
                ["dialogState"] = null, ["scheduledStart"] = null, ["scheduledStartIsScheduled"] = null, ["scheduledDialog"] = null,
                ["messageBox"] = null, ["messageBoxShown"] = null, ["messageBoxType"] = null, ["choices"] = null };
        }
        private static string ErrorText(Exception error)
        {
            var value = error.GetType().Name + ": " + error.Message;
            return value.Length <= 256 ? value : value.Substring(0, 256);
        }
        internal JObject Capture()
        {
            return new JObject { ["contract"] = Contract, ["rawSnapshotsOnly"] = true,
                ["maximumSnapshots"] = MaximumSnapshots, ["identityCapacity"] = IdentityCapacity,
                ["identityReleased"] = identities.Released, ["identityFaultCount"] = identities.FaultCount,
                ["identityIds"] = new JArray(identities.Ids), ["snapshots"] = rows.DeepClone(), ["errors"] = new JArray(errors) };
        }
        internal JObject CaptureTerminal(Game game)
        {
            if (terminal != null) return (JObject)terminal.DeepClone();
            try { Observe("host-terminal", game); }
            finally { Dispose(); }
            terminal = Capture(); return (JObject)terminal.DeepClone();
        }
        public void Dispose() { identities.Dispose(); }

        // The only native boundary: field reads, existing observational getters
        // and blueprint identifiers, plus bounded stored choices. No predicates,
        // dialog text, condition evaluation, UI input or actions are invoked.
        private static RawState ReadNative(Game game)
        {
            RequireModule(typeof(Game));
            var controller = game == null ? null : ReadField<DialogController>(typeof(Game), game, "DialogController", 0x040006C7);
            var dialog = controller == null ? null : ReadField<BlueprintDialog>(typeof(DialogController), controller, "Dialog", 0x04005E5C);
            var current = controller == null ? null : ReadField<BlueprintCue>(typeof(DialogController), controller, "m_CurrentCue", 0x04005E5A);
            var queued = controller == null ? null : ReadField<BlueprintCueBase>(typeof(DialogController), controller, "m_CueToPlay", 0x04005E6E);
            var continued = controller == null ? null : ReadField<BlueprintCueBase>(typeof(DialogController), controller, "m_ContinueCue", 0x04005E68);
            var nativePlayer = game == null ? null : game.Player;
            var dialogState = nativePlayer == null ? null : nativePlayer.Dialog;
            var scheduled = dialogState == null ? null : ReadField<StartDialogData>(typeof(DialogState), dialogState, "<Scheduled>k__BackingField", 0x04005611);
            var scheduledDialog = scheduled == null ? null : ReadField<BlueprintDialog>(typeof(StartDialogData), scheduled, "Dialog", 0x04005E54);
            var popup = ReadField<DialogMessageBox>(typeof(DialogMessageBox), null, "Instance", 0x04001DF6);
            return new RawState { Game = game, Controller = controller,
                TopMode = game == null ? null : game.CurrentMode.ToString(),
                DialogActive = game == null ? (bool?)null : game.IsModeActive(GameModeType.Dialog),
                CutsceneLock = game == null ? (bool?)null : game.CutsceneLock,
                Dialog = dialog, DialogGuid = GuidOf(dialog), CurrentCue = current, CurrentCueGuid = GuidOf(current),
                QueuedCue = queued, QueuedCueGuid = GuidOf(queued), ContinueCue = continued, ContinueCueGuid = GuidOf(continued),
                DialogState = dialogState, ScheduledStart = scheduled, ScheduledDialog = scheduledDialog, ScheduledDialogGuid = GuidOf(scheduledDialog),
                ScheduledStartIsScheduled = scheduled == null ? (bool?)null : ReadField<bool>(typeof(StartDialogData), scheduled, "<IsScheduled>k__BackingField", 0x04005E59),
                CuePlayScheduled = controller == null ? (bool?)null : ReadField<bool>(typeof(DialogController), controller, "m_CuePlayScheduled", 0x04005E6F),
                DialogStopScheduled = controller == null ? (bool?)null : ReadField<bool>(typeof(DialogController), controller, "m_DialogStopScheduled", 0x04005E70),
                Choices = Chunk7DialogueChoiceObservation.ReadNative(game, controller, dialog, current),
                MessageBox = popup, MessageBoxShown = popup == null ? (bool?)null : popup.IsShown,
                MessageBoxType = popup == null ? (int?)null : (int)ReadField<DialogMessageBoxBase.BoxType>(typeof(DialogMessageBox), popup, "m_BoxType", 0x04001E04) };
        }
        private static string GuidOf(BlueprintScriptableObject value) { return value == null ? null : value.AssetGuidThreadSafe; }
        private static void RequireModule(Type type)
        {
            if (type.Module.ModuleVersionId != new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7"))
                throw new InvalidOperationException("Installed passive dialogue metadata changed.");
        }
        private static T ReadField<T>(Type type, object instance, string name, int token)
        {
            RequireModule(type);
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (field == null || field.MetadataToken != token || field.FieldType != typeof(T) || field.IsStatic != (instance == null))
                throw new InvalidOperationException("Installed passive dialogue field changed: " + name);
            return (T)field.GetValue(instance);
        }
        internal sealed class RawState
        {
            internal object Game, Controller, Dialog, CurrentCue, QueuedCue, ContinueCue, MessageBox;
            internal object DialogState, ScheduledStart, ScheduledDialog;
            internal string ScheduledDialogGuid;
            internal bool? ScheduledStartIsScheduled;
            internal string TopMode, DialogGuid, CurrentCueGuid, QueuedCueGuid, ContinueCueGuid;
            internal bool? DialogActive, CutsceneLock, CuePlayScheduled, DialogStopScheduled, MessageBoxShown;
            internal int? MessageBoxType;
            internal Chunk7DialogueChoiceObservation.RawState Choices;
        }
    }
}
