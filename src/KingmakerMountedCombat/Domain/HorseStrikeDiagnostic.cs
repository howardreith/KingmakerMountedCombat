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
            if (record.HandleFactsUnreadable != null) return "present,facts-unreadable(" + record.HandleFactsUnreadable + ")";
            return HandleState(true, record.Started, record.Acted, record.Interrupted) + "; action=" + (record.Action ?? "<unnamed>") +
                "; speedScale=" + record.SpeedScale.ToString("R", CultureInfo.InvariantCulture) + "; variant=" +
                record.Variant.ToString(CultureInfo.InvariantCulture) + "/" + record.Variants.ToString(CultureInfo.InvariantCulture) +
                (record.PlaybackUnreadable != null
                    ? "; playback=unreadable(" + record.PlaybackUnreadable + ")"
                    : "; activeClip=" + (record.ActiveClip ?? "<none>") + "; clipWeight=" + record.ClipWeight.ToString("R", CultureInfo.InvariantCulture) +
                      "; animationState=" + (record.AnimationState ?? "<none>"));
        }

        // PR-5 review R1-F4/R1-F11: the pair command terminal line's horseBite fields, invariant culture throughout. observed is
        // the Bite as seen at its own strike: handle (with that handle's facts), no-handle (the Bite struck without one), or
        // not-reached (the exact Horse command ended before a Bite strike).
        public static string FormatTerminalBite(HorseBiteObservationKind kind, string action, string source, float speedScale, int routineAttacks,
            bool acted, bool finished, bool interrupted)
        {
            var routine = ",routineAttacks=" + routineAttacks.ToString(CultureInfo.InvariantCulture);
            if (kind == HorseBiteObservationKind.NoHandle) return "{observed=no-handle" + routine + "}";
            if (kind != HorseBiteObservationKind.Handle) return "{observed=not-reached" + routine + "}";
            return "{observed=handle,action=" + (action ?? "<unnamed>") + ",source=" + (source ?? HorseBiteTerminal.Unrecorded) +
                ",speedScale=" + speedScale.ToString("R", CultureInfo.InvariantCulture) + routine + ",acted=" + (acted ? "True" : "False") +
                ",finished=" + (finished ? "True" : "False") + ",interrupted=" + (interrupted ? "True" : "False") + "}";
        }

        internal static string Reason(Exception exception) => exception == null ? "unknown" : exception.GetType().Name;
    }

    public enum HorseBiteObservationKind { NotReached, Handle, NoHandle }

    // PR-5 review R1-F4: the Bite as actually observed at its strike, kept apart from the binding's registration (which stays
    // historical for idempotence). Only a strike of the proven Bite limb is an observation; a hoof never changes it. The
    // latest Bite strike of the command wins.
    public sealed class HorseBiteObservation<THandle> where THandle : class
    {
        public HorseBiteObservationKind Kind { get; private set; }
        public THandle Handle { get; private set; }
        public int BiteStrikes { get; private set; }

        public void Observe(bool biteStrike, THandle handle)
        {
            if (!biteStrike) return;
            BiteStrikes++;
            Handle = handle;
            Kind = handle == null ? HorseBiteObservationKind.NoHandle : HorseBiteObservationKind.Handle;
        }
    }

    public static class HorseBiteTerminal
    {
        public const string Unrecorded = "unrecorded";

        // The terminal horseBite fields from the observation: the observed handle's own facts, read at the terminal; its source is
        // the registration's only when the observed handle is the registered one, otherwise "unrecorded". A failing fact read is
        // stated, never replaced by another handle's facts.
        public static string Describe<THandle>(HorseBiteObservation<THandle> observation, THandle recordedHandle, string recordedSource,
            int routineAttacks, Func<THandle, string> actionName, Func<THandle, float> speedScale, Func<THandle, bool> acted,
            Func<THandle, bool> finished, Func<THandle, bool> interrupted) where THandle : class
        {
            if (observation == null || observation.Kind != HorseBiteObservationKind.Handle)
                return HorseStrikeDiagnostic.FormatTerminalBite(observation?.Kind ?? HorseBiteObservationKind.NotReached, null, null, float.NaN,
                    routineAttacks, false, false, false);
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
