using System;
using System.Linq;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    internal static class SmallProfilePolicyTests
    {
        // Synthetic policy data only; these are not admitted native race/rig IDs.
        private const string Profile = "small-fixture-female-horse-v1";
        private static readonly string Race = new string('b', 32), Preset = new string('c', 32), Equipment = new string('d', 32);

        internal static void Register(TestRunner runner)
        {
            runner.Run("exact declared Small observation produces one named body match", () =>
            { string error; var match = Declaration().Match(Observation(), out error); TestRunner.True(match != null && error == null && match.ProfileId == Profile, "Exact body was rejected."); });
            Refuse(runner, "original Medium reduced to Small", value => value.OriginalSizeOrdinal = 4);
            Refuse(runner, "original Small enlarged to Medium", value => value.EffectiveSizeOrdinal = 4);
            Refuse(runner, "rider polymorph", value => value.RiderPolymorphed = true);
            Refuse(runner, "mount polymorph", value => value.MountPolymorphed = true);
            Refuse(runner, "Small body borrowed for Mammoth", value => value.MountBlueprintGuid = "e7aa96d15a45238438ae4cfb476f6bb9");
            Refuse(runner, "mount no larger than Small rider", value => value.MountSizeOrdinal = 3);
            Refuse(runner, "Horse reduced to Medium outside calibrated pose", value => value.MountSizeOrdinal = 4);
            Refuse(runner, "Horse enlarged to Huge outside calibrated pose", value => value.MountSizeOrdinal = 6);
            Refuse(runner, "other Small race", value => value.RaceGuid = new string('e', 32));
            Refuse(runner, "other race preset", value => value.PresetGuid = new string('e', 32));
            Refuse(runner, "other sex", value => value.Gender = "Male");
            Refuse(runner, "same-name replacement native race object", value => value.NativeRaceAndPresetIdentityExact = false);
            Refuse(runner, "other skeleton name", value => value.SkeletonName = "OtherSmallSkeleton");
            Refuse(runner, "same-name replacement skeleton object", value => value.NativeSkeletonIsExactPreset = false);
            Refuse(runner, "unavailable native rig", value => value.NativeRigAvailable = false);
            Refuse(runner, "other handedness", value => value.LeftHanded = true);
            Refuse(runner, "other mirror state", value => value.AvatarMirrored = true);
            Refuse(runner, "changed avatar scale", value => value.AvatarLocalScale = new PoseVector3(1.1f, 1f, 1f));
            Refuse(runner, "nonfinite avatar scale", value => value.AvatarLocalScale = new PoseVector3(float.NaN, 1f, 1f));
            Refuse(runner, "missing Doll equipment", value => value.DollEquipmentEntityIds = null);
            Refuse(runner, "duplicate Doll equipment", value => value.DollEquipmentEntityIds = new[] { Equipment, Equipment });
            Refuse(runner, "other Doll equipment", value => value.DollEquipmentEntityIds = new[] { new string('e', 32) });
            Refuse(runner, "undeclared active weapon", value => value.CurrentEquipmentBlueprints[0] = new string('e', 32));
            Refuse(runner, "undeclared worn armor", value => value.CurrentEquipmentBlueprints[2] = new string('e', 32));
            Refuse(runner, "missing explicit equipment slots", value => value.CurrentEquipmentBlueprints = new string[11]);
            Refuse(runner, "extra visual equipment", value => value.ActiveVisualEquipmentNames = new[] { "skin", "clothes", "wings" });
            Refuse(runner, "visual duplicate hides missing garment", value => value.ActiveVisualEquipmentNames = new[] { "skin", "skin" });
            Refuse(runner, "unexpected natural limb", value => value.AdditionalLimbCount = 1);
            runner.Run("visual equipment comparison is order independent with exact multiplicity", () =>
            { var body = Observation(); body.ActiveVisualEquipmentNames = new[] { "clothes", "skin" }; TestRunner.True(Declaration().Refusal(body) == null, "Equivalent visual multiset was rejected."); });
            runner.Run("declaration owns copied equipment values", () =>
            {
                var slots = new string[12]; var declaration = Declaration(slots); slots[0] = new string('e', 32);
                TestRunner.True(declaration.Refusal(Observation()) == null, "External array mutation changed declared equipment.");
            });
            runner.Run("Small size alone cannot enter pair admission", () =>
            { var candidate = Candidate(false, null); TestRunner.True(candidate.Validate() != null, "Undeclared Small pair was admitted."); });
            runner.Run("declared Small exploration candidate uses connected admission", () =>
            { TestRunner.True(Candidate(false, Match()).Validate() == null, "Exact Small candidate was refused."); });
            runner.Run("Small does not bypass exact reciprocal companion rule", () =>
            { var candidate = Candidate(false, Match()); candidate.ExactReciprocalCompanionRelationship = false; TestRunner.True(candidate.Validate() != null, "Unowned Small pair admitted."); });
            runner.Run("Small does not bypass exploration versus combat admission", () =>
            {
                var candidate = Candidate(false, Match()); candidate.PartyIsInCombat = true;
                TestRunner.True(candidate.Validate() != null && candidate.Validate(MountedRelationshipAdmission.VoluntaryCombat) == null,
                    "Small changed the explicit exploration/combat contract.");
            });
            runner.Run("Small saved candidate still refuses unavailable stock agent", () =>
            { var candidate = Candidate(false, Match()); candidate.RiderStockAgentEnabled = false; TestRunner.True(candidate.ValidateSavedRelationship() != null, "Restore bypassed native stock-agent gate."); });
            runner.Run("ordinary Medium candidate retains legacy admission", () =>
            { TestRunner.True(Candidate(true, null).Validate() == null, "Medium candidate changed."); });
            runner.Run("simultaneous Medium and Small profile claims refuse", () =>
            { TestRunner.True(Candidate(true, Match()).Validate() != null && !MountedPlayerActionEvaluator.Evaluate(Context(true, Match())).IsEnabled, "Ambiguous body profile admitted."); });
            runner.Run("Small profile control retains native Move and turn gates", () =>
            {
                var context = Context(false, Match()); context.InCombat = true; context.RiderHasMoveAction = false;
                TestRunner.True(!MountedPlayerActionEvaluator.Evaluate(context).IsEnabled, "Small fabricated Move entitlement.");
                context.NativeMoveActionShellAdmitted = true;
                TestRunner.True(MountedPlayerActionEvaluator.Evaluate(context).IsEnabled, "Native committed shell was dropped.");
                context.CombatTurnEligible = false;
                TestRunner.True(!MountedPlayerActionEvaluator.Evaluate(context).IsEnabled, "Native shell bypassed exact turn eligibility.");
            });
            runner.Run("Small profile control requires its independently valid pose surface", () =>
            { var context = Context(false, Match()); context.RiderBodyProfileSupported = false; TestRunner.True(!MountedPlayerActionEvaluator.Evaluate(context).IsEnabled, "Small bypassed live pose surface."); });
            runner.Run("qualified catalogue cannot admit synthetic Small profiles", () =>
            { TestRunner.True(MountedRiderProfileCatalog.Small.Count == 1 && !MountedRiderProfileCatalog.IsSupportedArchiveProfile(Profile), "Unmeasured synthetic profile was globally enabled."); });
            runner.Run("schema3 profile policy preserves both Medium IDs and rejects guessed/unknown IDs", () =>
            {
                TestRunner.True(MountedRiderProfileCatalog.IsSupportedArchiveProfile("medium-humanoid-horse-v1") &&
                    MountedRiderProfileCatalog.IsSupportedArchiveProfile("medium-humanoid-mammoth-v1") &&
                    !MountedRiderProfileCatalog.IsSupportedArchiveProfile("small-humanoid-horse-v1") &&
                    !MountedRiderProfileCatalog.IsSupportedArchiveProfile("MEDIUM-HUMANOID-HORSE-V1") &&
                    !MountedRiderProfileCatalog.IsSupportedArchiveProfile(null), "Versioned profile policy widened.");
            });
            runner.Run("Small declaration cannot silently reuse the Medium Horse pose", () =>
            { Throws(() => new DeclaredSmallMountedProfile(Declaration(), MountedRiderPoseProfiles.MediumHumanoidOnHorse, new string('a', 64), Vector(), Vector(), Vector())); });
            runner.Run("Small declaration cannot silently reuse the Medium Mammoth pose", () =>
            { Throws(() => new DeclaredSmallMountedProfile(Declaration(), MountedRiderPoseProfiles.MediumHumanoidOnMammoth, new string('a', 64), Vector(), Vector(), Vector())); });
            runner.Run("independent synthetic Small pose requires explicit native calibration identity", () =>
            {
                var pose = new MountedRiderPoseProfile(Profile, "Pelvis", Vector(), Vector(),
                    new MountedRiderLegPoseProfile("L_Up_leg", "L_leg", "L_foot", new PoseVector3(-0.1f, -0.3f, 0.1f), Vector(), Vector()),
                    new MountedRiderLegPoseProfile("R_Up_leg", "R_leg", "R_foot", new PoseVector3(0.1f, -0.3f, 0.1f), Vector(), Vector()));
                Throws(() => new DeclaredSmallMountedProfile(Declaration(), pose, null, Vector(), Vector(), Vector()));
                var declaration = new DeclaredSmallMountedProfile(Declaration(), pose, new string('a', 64), Vector(), Vector(), Vector());
                TestRunner.Equal(Profile, declaration.Pose.Id, "Exact independent pose identity changed.");
                TestRunner.True(MountedRiderProfileCatalog.Small.Count == 1, "Constructing policy data changed the qualified native catalogue.");
            });
            runner.Run("qualified Halfling declaration and private calibration share exact identity", () =>
            {
                var provisional = MountedRiderProfileCatalog.PrivateCalibrationSmall.Single();
                var id = provisional.Body.ProfileId;
                TestRunner.True(id == "small-default-halfling-horse-v1" &&
                    provisional.Body.NativeInventorySha256 == "dcdc6cdce0265a221b2f3d890d34229385098235d2546df4a9c6a399239e48a9" &&
                    provisional.NativeCalibrationSha256 == "7d2385144838cccc27082cc9792f338c716961ed25016e6fff986599196411db" &&
                    MountedRiderProfileCatalog.Small.Count == 1 &&
                    ReferenceEquals(MountedRiderProfileCatalog.Small[0], provisional) &&
                    MountedRiderProfileCatalog.IsSupportedArchiveProfile(id),
                    "Native-qualified Small declaration differs from its private calibration identity.");
                var gate = new PrivateSmallCalibrationGate(MountedRiderProfileCatalog.PrivateCalibrationSmall);
                const string rider = "c4e2c853-4b72-4904-ad84-d0b7bfba7470";
                const string horse = "0e0c7d2d-06b4-4510-9f81-d847340d1424";
                var riderObject = new object(); var horseObject = new object();
                var current = true;
                Func<bool> currentScope = () => current;
                TestRunner.True(gate.Resolve(rider, horse, riderObject, horseObject) == null && gate.ResolveProfileId(id) == null,
                    "Private Small profile resolved without owned calibration.");
                var refused = false; try { gate.Acquire(provisional, rider.ToUpperInvariant(), horse, riderObject, horseObject, currentScope); }
                catch (InvalidOperationException) { refused = true; }
                TestRunner.True(refused, "Noncanonical rider acquired private Small admission.");
                var lease = gate.Acquire(provisional, rider, horse, riderObject, horseObject, currentScope);
                TestRunner.True(ReferenceEquals(gate.Resolve(rider, horse, riderObject, horseObject), provisional) &&
                    ReferenceEquals(gate.ResolveProfileId(id), provisional) &&
                    gate.Resolve(horse, rider, horseObject, riderObject) == null &&
                    gate.Resolve(rider, horse, new object(), horseObject) == null &&
                    gate.Resolve(rider, horse, riderObject, new object()) == null &&
                    gate.Resolve("11111111-1111-4111-8111-111111111111", horse, riderObject, horseObject) == null,
                    "Private Small admission escaped its exact rider/Horse pair.");
                current = false;
                TestRunner.True(gate.Resolve(rider, horse, riderObject, horseObject) == null && gate.ResolveProfileId(id) == null,
                    "Changed native scope retained provisional Small admission.");
                current = true;
                var throwScope = false;
                Func<bool> guardedScope = () => { if (throwScope) throw new InvalidOperationException("Native scope changed."); return true; };
                lease.Dispose();
                lease = gate.Acquire(provisional, rider, horse, riderObject, horseObject, guardedScope);
                throwScope = true;
                TestRunner.True(gate.Resolve(rider, horse, riderObject, horseObject) == null && gate.ResolveProfileId(id) == null,
                    "Throwing native scope retained provisional Small admission.");
                throwScope = false;
                refused = false; try { gate.Acquire(provisional, rider, horse, riderObject, horseObject, currentScope); }
                catch (InvalidOperationException) { refused = true; }
                TestRunner.True(refused, "Duplicate private Small admission was accepted.");
                lease.Dispose();
                TestRunner.True(gate.Resolve(rider, horse, riderObject, horseObject) == null && gate.ResolveProfileId(id) == null &&
                    MountedRiderProfileCatalog.Small.Count == 1 &&
                    MountedRiderProfileCatalog.IsSupportedArchiveProfile(id),
                    "Released private Small calibration changed ordinary profile admission.");
            });
        }

        private static void Refuse(TestRunner runner, string name, Action<SmallRiderBodyObservation> change) => runner.Run("Small refuses " + name, () =>
        { var observed = Observation(); change(observed); string error; TestRunner.True(Declaration().Match(observed, out error) == null && error != null, "Changed body was admitted."); });
        private static SmallRiderBodyMatch Match() { string error; return Declaration().Match(Observation(), out error); }
        private static DeclaredSmallRiderBody Declaration(string[] slots = null) => new DeclaredSmallRiderBody(Profile, new string('a', 64), Race, Preset,
            "Female", "SyntheticSmallSkeleton", false, false, new PoseVector3(1f, 1f, 1f), new[] { Equipment }, slots ?? new string[12], new[] { "skin", "clothes" });
        private static SmallRiderBodyObservation Observation() => new SmallRiderBodyObservation
        {
            OriginalSizeOrdinal = 3, EffectiveSizeOrdinal = 3, MountBlueprintGuid = DeclaredSmallRiderBody.HorseBlueprintGuid, MountSizeOrdinal = 5,
            RaceGuid = Race, PresetGuid = Preset, Gender = "Female", SkeletonName = "SyntheticSmallSkeleton",
            NativeRaceAndPresetIdentityExact = true, NativeSkeletonIsExactPreset = true, NativeRigAvailable = true,
            AvatarLocalScale = new PoseVector3(1f, 1f, 1f), DollEquipmentEntityIds = new[] { Equipment }, CurrentEquipmentBlueprints = new string[12],
            ActiveVisualEquipmentNames = new[] { "skin", "clothes" }
        };
        private static MountedPairCandidate Candidate(bool medium, SmallRiderBodyMatch small) => new MountedPairCandidate("rider", "mount")
        {
            RiderIsDirectlyControllable = true, MountIsDirectlyControllable = true, RiderIsAliveAndConscious = true, MountIsAliveAndConscious = true,
            ExactReciprocalCompanionRelationship = true, RiderSizeOrdinal = medium ? 4 : 3, MountSizeOrdinal = 5,
            RiderViewAndStockAgentAvailable = true, MountViewAndStockAgentAvailable = true, RiderStockAgentEnabled = true,
            MountStockAgentEnabled = true, RiderAgentOverrideAvailable = true, MountAgentOverrideAvailable = true,
            RiderIsExactlyMedium = medium, SmallRiderBodyMatch = small, SafeMovementMode = true
        };
        private static MountedPlayerActionContext Context(bool medium, SmallRiderBodyMatch small) => new MountedPlayerActionContext
        {
            RelationshipState = RelationshipState.Unmounted, GameAvailable = true, FeatureEnabled = true, ExactlyOneRiderSelected = true,
            RiderIsExactlyMedium = medium, SmallRiderBodyMatch = small, RiderBodyProfileSupported = true, ExactActiveOwnedSupportedMount = true,
            MountIsStrictlyLarger = true, RiderIsAliveAndConscious = true, MountIsAliveAndConscious = true,
            RiderIsDirectlyControllableAndInGame = true, MountIsDirectlyControllableAndInGame = true, SafeGameMode = true,
            ViewsAndStockAgentsAvailable = true, StockAgentsReady = true, AgentOverridesAvailable = true,
            CombatTurnEligible = true, RiderHasMoveAction = true, PairAdjacent = true
        };
        private static PoseVector3 Vector() => new PoseVector3(0f, 0f, 0f);
        private static void Throws(Action action)
        { var threw = false; try { action(); } catch (ArgumentException) { threw = true; } TestRunner.True(threw, "Invalid declaration was accepted."); }
    }
}
