using System;
using System.Collections.Generic;
using KingmakerMountedCombat.Diagnostics;

// Behavioral checks for the repeated-world KMC Horse owner count. The mount map mirrors the
// product profiles: Medium Horse and the declared Small Halfling Horse mount the KMC Horse
// blueprint, Mammoth mounts its own, and an unknown profile resolves to nothing.
internal static class ReplayHorseOwnersTests
{
    private const string Horse = "4016c7db400ab721ff125aef9e65e202";
    private const string Mammoth = "e7aa96d15a45238438ae4cfb476f6bb9";
    private static readonly Dictionary<string, string> Mounts = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["medium-humanoid-horse-v1"] = Horse,
        ["small-default-halfling-horse-v1"] = Horse,
        ["medium-humanoid-mammoth-v1"] = Mammoth,
    };
    private static int passed;

    private static string Mount(string id) => id != null && Mounts.TryGetValue(id, out var value) ? value : null;

    private static void Expect(string name, int expected, params string[] profiles)
    {
        var actual = Chunk7ReplayHorseOwners.Count(profiles, Mount, Horse);
        if (actual != expected) throw new InvalidOperationException(name + ": expected " + expected + ", observed " + actual + ".");
        passed++; Console.WriteLine("PASS " + name);
    }

    private static void Refuse(string name, Action action)
    {
        try { action(); }
        catch (ArgumentException) { passed++; Console.WriteLine("PASS " + name); return; }
        throw new InvalidOperationException(name + ": invalid input was admitted.");
    }

    private static int Main()
    {
        try
        {
            Expect("Small Halfling Horse plus Medium Horse is two", 2, "small-default-halfling-horse-v1", "medium-humanoid-horse-v1");
            Expect("two Medium Horses is two", 2, "medium-humanoid-horse-v1", "medium-humanoid-horse-v1");
            Expect("Mammoth plus Horse is one", 1, "medium-humanoid-mammoth-v1", "medium-humanoid-horse-v1");
            Expect("unknown and null profiles do not count", 0, "small-other-horse-v9", null);
            Expect("Mammoth only is zero", 0, "medium-humanoid-mammoth-v1");
            Refuse("null profile list is refused", () => Chunk7ReplayHorseOwners.Count(null, Mount, Horse));
            Refuse("null mount map is refused", () => Chunk7ReplayHorseOwners.Count(new string[0], null, Horse));
            Refuse("empty Horse blueprint is refused", () => Chunk7ReplayHorseOwners.Count(new string[0], Mount, ""));
            Console.WriteLine("REPLAY HORSE OWNERS PASS=" + passed + " FAIL=0");
            return 0;
        }
        catch (Exception error) { Console.WriteLine("FAIL " + error.Message); return 1; }
    }
}
