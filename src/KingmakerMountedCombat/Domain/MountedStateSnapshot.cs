using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace KingmakerMountedCombat.Domain
{
    // PR #70 R6-F7 (round 9): an exact copy of KMC-owned order state (the intent, the retained-order ledger, the automatic-run ledger and
    // the manual-target ownership records), taken before a replacement transfers that state and restored if the transfer fails before the
    // new order owns its command and carrier. Every instance field of each owner is copied (a list field keeps its own instance and has its
    // contents restored); nothing outside these KMC objects is touched.
    public sealed class MountedStateSnapshot
    {
        private sealed class Entry { public object Owner; public FieldInfo Field; public object Value; public object[] Items; }
        private readonly List<Entry> entries = new List<Entry>();

        public static MountedStateSnapshot Capture(params object[] owners)
        {
            var snapshot = new MountedStateSnapshot();
            foreach (var owner in owners)
            {
                if (owner == null) continue;
                for (var type = owner.GetType(); type != null && type != typeof(object); type = type.BaseType)
                    foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    {
                        var value = field.GetValue(owner);
                        var list = value as IList;
                        object[] items = null;
                        if (list != null && !(value is System.Array)) { items = new object[list.Count]; list.CopyTo(items, 0); }
                        snapshot.entries.Add(new Entry { Owner = owner, Field = field, Value = value, Items = items });
                    }
            }
            return snapshot;
        }

        public void Restore()
        {
            foreach (var entry in entries)
            {
                if (entry.Items != null)
                {
                    var list = (IList)entry.Field.GetValue(entry.Owner) ?? (IList)entry.Value;
                    if (!ReferenceEquals(list, entry.Value) && !entry.Field.IsInitOnly) entry.Field.SetValue(entry.Owner, entry.Value);
                    list = (IList)entry.Value;
                    list.Clear();
                    foreach (var item in entry.Items) list.Add(item);
                }
                else if (!entry.Field.IsInitOnly) entry.Field.SetValue(entry.Owner, entry.Value);
            }
        }
    }
}
