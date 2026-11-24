using System;
using System.Runtime.CompilerServices;

namespace KingmakerMountedCombat.Domain
{
    // MMR-06 saved-Horse repair admission (review R1-F2): the feature mutation runs only on a settled, enabled world:
    // the service is live, no native load is in progress or queued, and no save worker or restoration is pending
    // (an unknown persistence state blocks).
    public static class HorseInheritedFactRepairPolicy
    {
        public static bool MayRepair(bool serviceEnabled, bool worldSettled, bool persistenceBlocked) =>
            serviceEnabled && worldSettled && !persistenceBlocked;
    }

    // Completion per loaded object, never per stable ID: a reloaded unit (a new object with the same UniqueId) is examined
    // again, a refused or failed pass leaves its object pending for a later settled update, and a completed object is
    // never repaired twice. Weak keys: a discarded world's objects are not retained.
    public sealed class LoadedInstanceRepairLedger<T> where T : class
    {
        private readonly ConditionalWeakTable<T, object> completed = new ConditionalWeakTable<T, object>();

        public bool IsCompleted(T instance)
        {
            object ignored;
            return instance != null && completed.TryGetValue(instance, out ignored);
        }

        // Runs repair once when admitted; an exception propagates and leaves the object pending.
        public bool TryRun(T instance, bool admitted, Func<T, int> repair, out int changed)
        {
            changed = 0;
            if (instance == null || repair == null || !admitted || IsCompleted(instance)) return false;
            changed = repair(instance);
            completed.Add(instance, null);
            return true;
        }
    }
}
