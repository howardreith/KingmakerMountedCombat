using System;
using Kingmaker.PubSubSystem;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Logging;

namespace KingmakerMountedCombat.Integration
{
    internal enum MountedCombatHoldNoticeOutcome
    {
        None,
        // Owed but not attempted: a load is in progress.
        Waiting,
        // Attempted, and no warning receiver is subscribed: nothing was spent; a later evaluation tries again.
        NoReceiver,
        // At least one subscribed receiver took the notice without failing.
        Delivered,
        // Receivers were reached but none took it, or the dispatch failed: spent, never retried in this encounter.
        Failed
    }

    // Known issues C03, review R3 of b30c24f8 and R4 of 263ac3ea: the player is told, once per custody and encounter
    // (MountedRetainedIntentPolicy.CombatHoldNotice), that pairs still staged by a scene when a fight began stay
    // dismounted through it, through the native warning notification (IWarningNotificationUIHandler, added to the log).
    //
    // Native EventBus.RaiseEvent returns nothing, does nothing when no receiver is subscribed, and invokes the given
    // action once per subscribed receiver, catching and logging each receiver's exception (SubscriptionManager.RaiseEvent).
    // The receivers (WarningsText, BattleLogManager) subscribe only when the area's UI is initialized (StaticCanvas on
    // OnAreaDidLoad, raised by the last loading step) and unsubscribe when the area begins unloading. So delivery is
    // known from the action itself: it is counted as reached when native invokes it for a receiver, and as delivered when
    // that receiver's HandleWarning returned. A notice is spent only once a receiver was reached (or the dispatch failed),
    // never by a dispatch nobody received, and never attempted during a load. Delivery owns no retained, combat or
    // cleanup state and never throws.
    internal sealed class MountedCombatHoldNotifier
    {
        private readonly MountedRetainedIntentPolicy.CombatHoldNotice notice = new MountedRetainedIntentPolicy.CombatHoldNotice();
        private readonly IModLogger logger;

        internal MountedCombatHoldNotifier(IModLogger logger)
        {
            this.logger = logger;
        }

        // Notices taken by at least one receiver.
        internal long DeliveredCount { get; private set; }
        // Notices spent without any receiver taking them (every reached receiver failed, or the dispatch failed).
        internal long FailedCount { get; private set; }
        // Attempts that reached no receiver (nothing spent).
        internal long NoReceiverCount { get; private set; }
        internal string LastNotice { get; private set; }

        // Asked at every evaluation of the retained intent. pairs and diagnostic are read only when a receiver is reached.
        internal MountedCombatHoldNoticeOutcome Evaluate(object custody, bool heldThroughEncounter, bool loading, Func<string[]> pairs,
            Func<string> diagnostic)
        {
            bool attempt;
            try
            {
                attempt = notice.ShouldAttempt(custody, heldThroughEncounter, loading);
                if (!attempt) return loading && notice.IsOwed(custody, heldThroughEncounter) ? MountedCombatHoldNoticeOutcome.Waiting : MountedCombatHoldNoticeOutcome.None;
            }
            catch (Exception error) { Log(error); return MountedCombatHoldNoticeOutcome.None; }
            string text = null;
            var reached = 0;
            var accepted = 0;
            try
            {
                EventBus.RaiseEvent<IWarningNotificationUIHandler>(handler =>
                {
                    reached++;
                    text = text ?? Compose(pairs?.Invoke());
                    handler.HandleWarning(text, true);
                    accepted++;
                });
            }
            catch (Exception error)
            {
                notice.Spend(custody);
                FailedCount++;
                LastNotice = text;
                Log(error);
                return MountedCombatHoldNoticeOutcome.Failed;
            }
            if (reached == 0)
            {
                NoReceiverCount++;
                return MountedCombatHoldNoticeOutcome.NoReceiver;
            }
            notice.Spend(custody);
            LastNotice = text;
            try
            {
                logger?.Info("Combat-hold notice for retained scene staging: " + (diagnostic?.Invoke() ?? "") + "; receivers reached=" + reached +
                    ", took it=" + accepted + "; notice=\"" + text + "\".");
            }
            catch (Exception error) { Log(error); }
            if (accepted == 0)
            {
                FailedCount++;
                return MountedCombatHoldNoticeOutcome.Failed;
            }
            DeliveredCount++;
            return MountedCombatHoldNoticeOutcome.Delivered;
        }

        internal static string Compose(string[] pairs)
        {
            var names = pairs == null || pairs.Length == 0 ? "Your mounted pairs" : string.Join("; ", pairs);
            return names + " stay dismounted for this fight, because they were still part of a scene when it began. " +
                "After the fight they remount automatically if they still can.";
        }

        private void Log(Exception error)
        {
            try { logger?.Exception("The combat-hold notice was not delivered", error); }
            catch { }
        }
    }
}
