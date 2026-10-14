using System;
using System.Collections.Generic;
using System.Globalization;

namespace KingmakerMountedCombat.Diagnostics
{
    // A separately selected disposable setup. This DTO conveys no earned XP,
    // ordinary acquisition, mounted action or cold-load qualification.
    public sealed class Chunk7AssistedFixtureSelection
    {
        public const string Scenario = "chunk7-assisted-fixture-preparation";
        public const string CaseName = "authored-main-ranger4-manual";
        public const string AuthorizationSha256 = "052eb32338bba397e852cba8605ebc6878deff1dd907675041d5b1d2f802e28e";
        public const string StoryPolicyName = "first-cached-enabled-common-answer";
        public const string AssistancePolicyName = "native-main-ranger4-and-one-hire-shortfalls";
        public const string InitialAutoLeaf = "Auto_1.zks";
        public const string LevelupStagingLeaf = "Auto_2.zks";
        public const string OutputName = "KMC_C7_ASSISTED_FIXTURE";
        public string Case { get; set; }
        public string OwnerAuthorizationSha256 { get; set; }
        public bool BaseGameOnly { get; set; }
        public bool AssistedSetupOnly { get; set; }
        public string StoryPolicy { get; set; }
        public string AssistancePolicy { get; set; }
        public int ExpectedNativeLoadCount { get; set; }
        public int MaximumNativeWriteCount { get; set; }
        public int TargetRangerLevel { get; set; }
        public int MaximumTargetLevel { get; set; }
        public int MaximumStoryChoices { get; set; }
        public int MaximumStorySeconds { get; set; }
        public int MaximumExperienceShortfall { get; set; }
        public long MaximumMoneyShortfall { get; set; }
        public string NativeInitialAutoLeaf { get; set; }
        public string NativeManualName { get; set; }
        public IReadOnlyList<string> Validate(string scenario)
        {
            var errors = new List<string>();
            if (scenario != Scenario || Case != CaseName || !BaseGameOnly || !AssistedSetupOnly ||
                OwnerAuthorizationSha256 != AuthorizationSha256 || StoryPolicy != StoryPolicyName || AssistancePolicy != AssistancePolicyName)
                errors.Add("Assisted fixture preparation requires its exact owner-authorized isolated setup case and policies.");
            if (ExpectedNativeLoadCount != 0 || MaximumNativeWriteCount != 3 || NativeInitialAutoLeaf != InitialAutoLeaf || NativeManualName != OutputName)
                errors.Add("Assisted setup declares zero loads, at most three phase-owned writes and its exact native output identities.");
            if (TargetRangerLevel != 4 || MaximumTargetLevel != 8 || MaximumStoryChoices != 16 || MaximumStorySeconds != 300 ||
                MaximumExperienceShortfall != 200000 || MaximumMoneyShortfall != 100000)
                errors.Add("Assisted setup ceilings differ from the selected bounded native shortfall and story plan.");
            return errors;
        }
        internal static string ManualLeaf(int number)
        {
            if (number <= 0) throw new ArgumentOutOfRangeException(nameof(number));
            return "Manual_" + number.ToString(CultureInfo.InvariantCulture) + "_" + OutputName + ".zks";
        }
        internal string Key => string.Join("\n", Case, OwnerAuthorizationSha256, BaseGameOnly.ToString(), AssistedSetupOnly.ToString(),
            StoryPolicy, AssistancePolicy, ExpectedNativeLoadCount.ToString(CultureInfo.InvariantCulture),
            MaximumNativeWriteCount.ToString(CultureInfo.InvariantCulture), TargetRangerLevel.ToString(CultureInfo.InvariantCulture),
            MaximumTargetLevel.ToString(CultureInfo.InvariantCulture), MaximumStoryChoices.ToString(CultureInfo.InvariantCulture),
            MaximumStorySeconds.ToString(CultureInfo.InvariantCulture), MaximumExperienceShortfall.ToString(CultureInfo.InvariantCulture),
            MaximumMoneyShortfall.ToString(CultureInfo.InvariantCulture), NativeInitialAutoLeaf, NativeManualName);
    }
}
