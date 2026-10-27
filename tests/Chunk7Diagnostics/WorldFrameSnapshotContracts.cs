using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;

internal static class WorldFrameSnapshotContracts
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    private static int passed;

    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException(name);
        passed++;
        Console.WriteLine("PASS " + name);
    }

    private static void Exercise<T>(Type worldType)
    {
        var world = FormatterServices.GetUninitializedObject(worldType);
        var roster = new List<T> { (T)FormatterServices.GetUninitializedObject(typeof(T)) };
        var rosterField = worldType.GetField("pairs", Flags);
        var snapshotField = worldType.GetField("pairSnapshot", Flags);
        var visit = worldType.GetMethod("Visit", Flags);
        Check(rosterField != null && snapshotField != null && visit != null,
            "actual world exposes a closed frame roster and visit boundary");
        rosterField.SetValue(world, roster);

        var calls = 0;
        Action<T> ordinary = pair => { Check(Object.ReferenceEquals(pair, roster[0]), "visit keeps exact pair identity"); calls++; };
        var empty = (Exception[])visit.Invoke(world, new object[] { ordinary });
        var firstSnapshot = snapshotField.GetValue(world);
        Check(calls == 1 && empty.Length == 0 && firstSnapshot != null,
            "first successful visit publishes one complete frame snapshot");
        calls = 0;
        var repeated = (Exception[])visit.Invoke(world, new object[] { ordinary });
        Check(calls == 1 && Object.ReferenceEquals(firstSnapshot, snapshotField.GetValue(world)) &&
            Object.ReferenceEquals(empty, repeated),
            "repeated successful visits reuse roster and empty error result");

        calls = 0;
        var faulted = (Exception[])visit.Invoke(world, new object[] { (Action<T>)(pair => {
            calls++;
            throw new InvalidOperationException("one owner failed");
        }) });
        Check(calls == 1 && faulted.Length == 1 && faulted[0].Message == "one owner failed" &&
            Object.ReferenceEquals(firstSnapshot, snapshotField.GetValue(world)),
            "local owner failure is reported without replacing the roster");

        roster.Add((T)FormatterServices.GetUninitializedObject(typeof(T)));
        calls = 0;
        visit.Invoke(world, new object[] { (Action<T>)(pair => calls++) });
        Check(calls == 1, "a roster addition cannot enter an already published frame snapshot");
        snapshotField.SetValue(world, null);
        calls = 0;
        visit.Invoke(world, new object[] { (Action<T>)(pair => calls++) });
        Check(calls == 2 && !Object.ReferenceEquals(firstSnapshot, snapshotField.GetValue(world)),
            "invalidated roster admits the new pair on the next traversal");
    }

    public static int Main(string[] args)
    {
        try
        {
            var managed = args[0];
            AppDomain.CurrentDomain.AssemblyResolve += (sender, missing) => {
                var leaf = new AssemblyName(missing.Name).Name + ".dll";
                foreach (var directory in new[] { managed, Path.Combine(managed, "UnityModManager") })
                {
                    var path = Path.Combine(directory, leaf);
                    if (File.Exists(path)) return Assembly.LoadFrom(path);
                }
                return null;
            };
            var mod = Assembly.LoadFrom(args[1]);
            var worldType = mod.GetType("KingmakerMountedCombat.Integration.MountedWorldService", true);
            var pairType = mod.GetType("KingmakerMountedCombat.Integration.MountedPairServices", true);
            typeof(WorldFrameSnapshotContracts).GetMethod("Exercise", BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod(pairType).Invoke(null, new object[] { worldType });
            Console.WriteLine("WORLD FRAME SNAPSHOT PASS=" + passed);
            return 0;
        }
        catch (Exception error)
        {
            Console.WriteLine(error);
            return 1;
        }
    }
}
