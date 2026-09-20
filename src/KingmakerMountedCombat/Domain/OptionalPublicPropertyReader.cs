using System;
using System.Reflection;

namespace KingmakerMountedCombat.Domain
{
    internal static class OptionalPublicPropertyReader
    {
        public static object Read(object instance, string name)
        {
            if (instance == null || string.IsNullOrEmpty(name))
            {
                return null;
            }

            try
            {
                for (var type = instance.GetType(); type != null; type = type.BaseType)
                {
                    PropertyInfo[] properties;
                    try
                    {
                        properties = type.GetProperties(
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.DeclaredOnly);
                    }
                    catch
                    {
                        continue;
                    }

                    foreach (var property in properties)
                    {
                        if (!string.Equals(property.Name, name, StringComparison.Ordinal) ||
                            property.GetIndexParameters().Length != 0)
                        {
                            continue;
                        }

                        try
                        {
                            return property.GetValue(instance, null);
                        }
                        catch
                        {
                            // Optional telemetry may fall back to the next public
                            // declaration, but it must never fault gameplay.
                        }
                    }
                }
            }
            catch
            {
                // A foreign or dynamic type may reject reflection. This reader is
                // observation-only, so absence is the truthful fallback.
            }

            return null;
        }
    }
}
