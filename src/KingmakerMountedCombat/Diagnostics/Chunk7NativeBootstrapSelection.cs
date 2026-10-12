using System;
using System.Collections.Generic;

namespace KingmakerMountedCombat.Diagnostics
{
    // This is an explicit isolated native campaign creation, not a saved-game
    // loader fallback. Every older request still takes WorkingFixtureLoader.
    public sealed class Chunk7NativeBootstrapSelection
    {
        public const string Scenario = "chunk7-native-main-bootstrap";
        public const string MainCase = "authored-main-campaign";
        public const string AutoLeaf = "Auto_1.zks";
        public string Case { get; set; }
        public bool BaseGameOnly { get; set; }
        public int ExpectedNativeLoadCount { get; set; }
        public int ExpectedNativeWriteCount { get; set; }
        public string NativeWriteLeaf { get; set; }

        public IReadOnlyList<string> Validate(string scenario)
        {
            var errors = new List<string>();
            if (scenario != Scenario || Case != MainCase || !BaseGameOnly)
                errors.Add("Native bootstrap requires its exact authored main-campaign selection.");
            if (ExpectedNativeLoadCount != 0 || ExpectedNativeWriteCount != 1 || NativeWriteLeaf != AutoLeaf)
                errors.Add("Native bootstrap permits no saved-game loads and one native Auto_1 write.");
            return errors;
        }
    }
}
