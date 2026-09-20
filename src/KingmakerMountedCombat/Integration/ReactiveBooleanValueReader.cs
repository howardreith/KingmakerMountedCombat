using System;
using System.Reflection;

namespace KingmakerMountedCombat.Integration
{
    internal static class ReactiveBooleanValueReader
    {
        internal const string Unavailable = "<unavailable>";

        internal static string Read(object owner, string memberName)
        {
            if (owner == null || string.IsNullOrEmpty(memberName))
            {
                return Unavailable;
            }

            var ownerType = owner.GetType();
            object reactive = null;
            var property = ownerType.GetProperty(
                memberName,
                BindingFlags.Instance | BindingFlags.Public);
            if (property != null)
            {
                reactive = property.GetValue(owner, null);
            }
            else
            {
                var field = ownerType.GetField(
                    memberName,
                    BindingFlags.Instance | BindingFlags.Public);
                if (field != null)
                {
                    reactive = field.GetValue(owner);
                }
            }

            var value = reactive?.GetType().GetProperty(
                "Value",
                BindingFlags.Instance | BindingFlags.Public)?.GetValue(reactive, null);
            return value is bool boolean ? boolean.ToString() : Unavailable;
        }
    }
}
