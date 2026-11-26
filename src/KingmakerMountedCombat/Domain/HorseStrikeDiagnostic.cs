using System;
using System.Collections;
using System.Globalization;

namespace KingmakerMountedCombat.Domain
{
    // MMR-07 owner-log evidence (reviews R1-F13, R2-F4): one line per native strike of the exact KMC Horse, read at the
    // strike's own act event. It states handle facts literally (none, or its action, started/acted/interrupted, speedScale and
    // variant) and the installed playback boundary's facts for that handle (the active AnimationBase's clip, weight and state).
    // It does not claim what was rendered: whether the bite is visible is a footage comparison the owner makes.
    public static class HorseStrikeDiagnostic
    {
        public static string HandleState(bool hasHandle, bool started, bool acted, bool interrupted) =>
            !hasHandle ? "none" : (started ? "started" : "not-started") + "," + (acted ? "acted" : "not-acted") + (interrupted ? ",interrupted" : string.Empty);

        // PR-5 review R1-F5: the strike's actual native slot, resolved by reference against the Horse's body: primary or
        // secondary hand, or additional limb i (limb0 is the Bite, limb1 and limb2 the two hooves sharing one blueprint).
        // Independent of the routine ordinal: repeated strikes from one limb resolve to the same slot.
        public static string Slot(object hand, object primaryHand, object secondaryHand, IEnumerable additionalLimbs)
        {
            if (hand == null) return "none";
            if (primaryHand != null && ReferenceEquals(hand, primaryHand)) return "primary";
            if (secondaryHand != null && ReferenceEquals(hand, secondaryHand)) return "secondary";
            if (additionalLimbs != null)
            {
                var index = 0;
                foreach (var limb in additionalLimbs)
                {
                    if (limb != null && ReferenceEquals(limb, hand)) return "limb" + index.ToString(CultureInfo.InvariantCulture);
                    index++;
                }
            }
            return "unresolved";
        }

        // PR-5 review R1-F6: an unreadable group is stated as unreadable (with the failing exception type), never as absent.
        public static string Format(HorseStrikeRecord record) =>
            "Horse strike: horse=" + (record.Horse ?? "<none>") + "; index=" + record.Index.ToString(CultureInfo.InvariantCulture) + "/" +
            record.Count.ToString(CultureInfo.InvariantCulture) + "; slot=" + (record.Slot ?? "<none>") +
            (record.WeaponUnreadable != null
                ? "; weapon=unreadable(" + record.WeaponUnreadable + "); special=unreadable"
                : "; weapon=" + (record.Weapon ?? "<none>") + "; special=" + (record.Special ?? "<none>")) +
            "; handle=" + HandleText(record) + "; target=" + (record.Target ?? "<none>") + ".";

        private static string HandleText(HorseStrikeRecord record)
        {
            if (!record.HasHandle)
                return record.HandleFactsUnreadable != null ? "unreadable(" + record.HandleFactsUnreadable + ")" : "none";
            // PR-5 review R2-F4: each handle fact is read on its own; a failed fact is stated in place and the others, and the
            // playback group, are still shown.
            var failed = record.HandleFactsUnreadable;
            var state = Failed(failed, "started") || Failed(failed, "acted") || Failed(failed, "interrupted")
                ? "present" : HandleState(true, record.Started, record.Acted, record.Interrupted);
            return state + (failed != null ? ",facts-unreadable(" + failed + ")" : string.Empty) +
                "; action=" + (Failed(failed, "action") ? "unreadable" : record.Action ?? "<unnamed>") +
                "; speedScale=" + (Failed(failed, "speedScale") ? "unreadable" : record.SpeedScale.ToString("R", CultureInfo.InvariantCulture)) + "; variant=" +
                (Failed(failed, "variant") ? "unreadable" : record.Variant.ToString(CultureInfo.InvariantCulture)) + "/" +
                (Failed(failed, "variants") ? "unreadable" : record.Variants.ToString(CultureInfo.InvariantCulture)) +
                (record.PlaybackUnreadable != null
                    ? "; playback=unreadable(" + record.PlaybackUnreadable + ")"
                    : "; activeClip=" + (record.ActiveClip ?? "<none>") + "; clipWeight=" + record.ClipWeight.ToString("R", CultureInfo.InvariantCulture) +
                      "; animationState=" + (record.AnimationState ?? "<none>"));
        }

        // PR-5 review R1-F4/R1-F11: the pair command terminal line's horseBite fields, invariant culture throughout. observed is
        // the Bite as seen at its own strike: handle (with that handle's facts), no-handle (the Bite struck without one), or
        // not-reached (the exact Horse command ended before a Bite strike).
        public static string FormatTerminalBite(HorseBiteObservationKind kind, string action, string source, float speedScale, int routineAttacks,
            bool acted, bool finished, bool interrupted, string unreadable = null)
        {
            var routine = ",routineAttacks=" + routineAttacks.ToString(CultureInfo.InvariantCulture);
            if (kind == HorseBiteObservationKind.Unreadable) return "{observed=unreadable(" + (unreadable ?? "unknown") + ")" + routine + "}";
            if (kind == HorseBiteObservationKind.NoHandle) return "{observed=no-handle" + routine + "}";
            if (kind != HorseBiteObservationKind.Handle) return "{observed=not-reached" + routine + "}";
            return "{observed=handle,action=" + (action ?? "<unnamed>") + ",source=" + (source ?? HorseBiteTerminal.Unrecorded) +
                ",speedScale=" + speedScale.ToString("R", CultureInfo.InvariantCulture) + routine + ",acted=" + (acted ? "True" : "False") +
                ",finished=" + (finished ? "True" : "False") + ",interrupted=" + (interrupted ? "True" : "False") + "}";
        }

        internal static string Reason(Exception exception) => exception == null ? "unknown" : exception.GetType().Name;

        // Whether the comma-separated failure list ("name:Type,...") names this fact.
        private static bool Failed(string failures, string name) =>
            failures != null && ("," + failures).Contains("," + name + ":");
    }

    // PR-5 review R2-F2: Unreadable is a recognized Horse strike whose plan, limb or handle could not be read.
    public enum HorseBiteObservationKind { NotReached, Handle, NoHandle, Unreadable }

    // PR-5 review R1-F4: the Bite as actually observed at its strike, kept apart from the binding's registration (which stays
    // historical for idempotence). Only a strike of the proven Bite limb is an observation; a hoof never changes it. The
    // latest Bite strike of the command wins. PR-5 review R2-F2: a strike that could not be classified replaces any earlier
    // observation with an explicit unreadable one, so stale evidence never stands for the current strike.
    public sealed class HorseBiteObservation<THandle> where THandle : class
    {
        public HorseBiteObservationKind Kind { get; private set; }
        public THandle Handle { get; private set; }
        public int BiteStrikes { get; private set; }
        public string UnreadableReason { get; private set; }

        public void Observe(bool biteStrike, THandle handle)
        {
            if (!biteStrike) return;
            BiteStrikes++;
            Handle = handle;
            UnreadableReason = null;
            Kind = handle == null ? HorseBiteObservationKind.NoHandle : HorseBiteObservationKind.Handle;
        }

        public void ObserveUnreadable(string reason)
        {
            Handle = null;
            UnreadableReason = reason ?? "unknown";
            Kind = HorseBiteObservationKind.Unreadable;
        }
    }

    // PR-5 review R2-F2: the whole diagnostic observation of one exact-Horse strike, contained: plan acquisition, limb
    // classification, the handle read and the strike line. It never throws, so the command's single native OnAction
    // delegation always follows it. A failed read records an explicit unreadable observation and still emits exactly one
    // identifiable strike line for the Horse.
    public static class HorseStrikeObservationStep
    {
        public static void Observe<TPlan, THandle>(HorseBiteObservation<THandle> observation, Func<TPlan> readPlan, Func<TPlan, bool> isBiteLimb,
            Func<TPlan, THandle> handleOf, Func<TPlan, string> strikeLine, Func<string> horseId, Action<string> log)
            where TPlan : class where THandle : class
        {
            try
            {
                string unreadable = null;
                TPlan plan = null;
                var planRead = false;
                var bite = false;
                THandle handle = null;
                try { plan = readPlan(); planRead = true; }
                catch (Exception exception) { unreadable = "plan:" + HorseStrikeDiagnostic.Reason(exception); }
                if (planRead)
                {
                    try { bite = isBiteLimb(plan); }
                    catch (Exception exception) { unreadable = "limb:" + HorseStrikeDiagnostic.Reason(exception); }
                    if (unreadable == null && bite)
                    {
                        try { handle = handleOf(plan); }
                        catch (Exception exception) { unreadable = "handle:" + HorseStrikeDiagnostic.Reason(exception); }
                    }
                }
                try
                {
                    if (unreadable != null) observation.ObserveUnreadable(unreadable);
                    else observation.Observe(bite, handle);
                }
                catch (Exception) { }
                string line = null;
                if (planRead) { try { line = strikeLine(plan); } catch (Exception) { line = null; } }
                if (line == null)
                {
                    string id = null;
                    try { id = horseId(); } catch (Exception) { id = null; }
                    line = "Horse strike: horse=" + (id ?? "<unreadable>") + "; planned=" + (planRead ? "read; strike facts unreadable" : "unreadable") + ".";
                }
                if (unreadable != null) line = line.TrimEnd('.') + "; biteObservation=unreadable(" + unreadable + "); the native strike proceeds unchanged.";
                try { log(line); } catch (Exception) { }
            }
            catch (Exception)
            {
                // Diagnostics never affect the native strike.
            }
        }
    }

    // PR-5 review R1-F4: the structured playback evidence of the observed Bite (the handle seen at its own strike), with the
    // binding's registration kept as separate historical facts.
    public struct HorseBitePlayback
    {
        // handle, no-handle, not-reached or unreadable(<what>:<type>); null outside an exact KMC Horse primary command.
        public string Observation;
        public bool HandleObserved;
        public string Source, ActionName, ActionType, FactsUnreadable;
        public bool Acted, Finished, Interrupted;
        public bool RegisteredHandle;
        public string RegisteredSource;
    }

    // PR-5 review R2-F3: the terminal outcome is the detailed one or, when describing it throws, the minimal one; the Bite
    // disposition is attached on this common path, so both carry it. Attachment failures are contained.
    public static class HorseTerminalOutcome
    {
        public static T Compose<T>(Func<T> detailed, Func<Exception, T> minimal, Action<T> attachBite) where T : class
        {
            T outcome;
            try { outcome = detailed(); }
            catch (Exception exception) { outcome = minimal(exception); }
            if (outcome != null)
            {
                try { attachBite(outcome); } catch (Exception) { }
            }
            return outcome;
        }
    }

    public static class HorseBiteTerminal
    {
        public const string Unrecorded = "unrecorded";

        // PR-5 review R1-F4: the structured projection of the observed Bite. The playback fields describe the handle observed at
        // the Bite strike (its own facts; its source only when it is the registered handle); the registration stays separate.
        public static HorseBitePlayback Project<THandle>(HorseBiteObservation<THandle> observation, THandle recordedHandle, string recordedSource,
            Func<THandle, string> actionName, Func<THandle, string> actionType, Func<THandle, bool> acted, Func<THandle, bool> finished,
            Func<THandle, bool> interrupted) where THandle : class
        {
            var playback = new HorseBitePlayback { RegisteredHandle = recordedHandle != null, RegisteredSource = recordedHandle != null ? recordedSource : null };
            var kind = observation?.Kind ?? HorseBiteObservationKind.NotReached;
            switch (kind)
            {
                case HorseBiteObservationKind.NoHandle: playback.Observation = "no-handle"; return playback;
                case HorseBiteObservationKind.Unreadable: playback.Observation = "unreadable(" + (observation.UnreadableReason ?? "unknown") + ")"; return playback;
                case HorseBiteObservationKind.Handle: break;
                default: playback.Observation = "not-reached"; return playback;
            }
            var handle = observation.Handle;
            playback.Observation = "handle";
            playback.HandleObserved = true;
            playback.Source = recordedHandle != null && ReferenceEquals(handle, recordedHandle) ? recordedSource : Unrecorded;
            try
            {
                playback.ActionName = actionName(handle);
                playback.ActionType = actionType(handle);
                playback.Acted = acted(handle);
                playback.Finished = finished(handle);
                playback.Interrupted = interrupted(handle);
            }
            catch (Exception exception) { playback.FactsUnreadable = HorseStrikeDiagnostic.Reason(exception); }
            return playback;
        }

        // The playback judgement the scenario checks use: a Bite handle observed at the strike, bound by the exact stock adoption or
        // KMC supply, its facts readable and not interrupted. A handle-less or unreached Bite, or one whose handle is not the
        // registered one, is rejected whatever the registration says.
        public static bool PlaybackAccepted(HorseBitePlayback playback) =>
            playback.HandleObserved && playback.FactsUnreadable == null && PlaybackAccepted(true, playback.Source, playback.Interrupted);

        public static bool PlaybackAccepted(bool handleObserved, string source, bool interrupted) =>
            handleObserved && IsExactPlaybackSource(source) && !interrupted;

        public static bool IsExactPlaybackSource(string source) =>
            string.Equals(source, HorseStrikeHandleBinding.StockCreated, StringComparison.Ordinal) ||
            string.Equals(source, HorseStrikeHandleBinding.KmcSupplied, StringComparison.Ordinal);

        // The terminal horseBite fields from the observation: the observed handle's own facts, read at the terminal; its source is
        // the registration's only when the observed handle is the registered one, otherwise "unrecorded". A failing fact read is
        // stated, never replaced by another handle's facts.
        public static string Describe<THandle>(HorseBiteObservation<THandle> observation, THandle recordedHandle, string recordedSource,
            int routineAttacks, Func<THandle, string> actionName, Func<THandle, float> speedScale, Func<THandle, bool> acted,
            Func<THandle, bool> finished, Func<THandle, bool> interrupted) where THandle : class
        {
            if (observation == null || observation.Kind != HorseBiteObservationKind.Handle)
                return HorseStrikeDiagnostic.FormatTerminalBite(observation?.Kind ?? HorseBiteObservationKind.NotReached, null, null, float.NaN,
                    routineAttacks, false, false, false, observation?.UnreadableReason);
            var handle = observation.Handle;
            var source = recordedHandle != null && ReferenceEquals(handle, recordedHandle) ? recordedSource : Unrecorded;
            try
            {
                return HorseStrikeDiagnostic.FormatTerminalBite(HorseBiteObservationKind.Handle, actionName(handle), source, speedScale(handle),
                    routineAttacks, acted(handle), finished(handle), interrupted(handle));
            }
            catch (Exception exception)
            {
                return "{observed=handle,facts=unreadable(" + HorseStrikeDiagnostic.Reason(exception) + "),source=" + (source ?? Unrecorded) +
                    ",routineAttacks=" + routineAttacks.ToString(CultureInfo.InvariantCulture) + "}";
            }
        }
    }
}
