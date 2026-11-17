using System;
using Kingmaker.PubSubSystem;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Logging;

namespace KingmakerMountedCombat.Integration
{
    // Known issues C03, review R3 of b30c24f8: the player is told, once per custody and encounter
    // (MountedRetainedIntentPolicy.CombatHoldNotice), that pairs still staged by a scene when a fight began stay
    // dismounted through it. The notice goes through the native warning notification (IWarningNotificationUIHandler,
    // added to the log), the path this project's other player warnings use; identities and the mechanism stay in the
    // diagnostic log. Delivery can fail without consequence: the notifier owns no retained or combat state and never throws.
    internal sealed class MountedCombatHoldNotifier
    {
        private readonly MountedRetainedIntentPolicy.CombatHoldNotice notice = new MountedRetainedIntentPolicy.CombatHoldNotice();
        private readonly IModLogger logger;

        internal MountedCombatHoldNotifier(IModLogger logger)
        {
            this.logger = logger;
        }

        internal long DeliveredCount { get; private set; }
        internal long FailedCount { get; private set; }
        internal string LastNotice { get; private set; }

        // Asked at every evaluation of the retained intent. pairs and diagnostic are read only when a notice is due.
        internal bool Evaluate(object custody, bool heldThroughEncounter, Func<string[]> pairs, Func<string> diagnostic)
        {
            bool due;
            try { due = notice.ShouldNotify(custody, heldThroughEncounter); }
            catch (Exception error) { Log(error); return false; }
            if (!due) return false;
            try
            {
                var text = Compose(pairs?.Invoke());
                LastNotice = text;
                try { logger?.Info("Combat-hold notice for retained scene staging: " + (diagnostic?.Invoke() ?? "") + "; notice=\"" + text + "\"."); }
                catch (Exception error) { Log(error); }
                EventBus.RaiseEvent<IWarningNotificationUIHandler>(handler => handler.HandleWarning(text, true));
                DeliveredCount++;
                return true;
            }
            catch (Exception error)
            {
                FailedCount++;
                Log(error);
                return false;
            }
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
