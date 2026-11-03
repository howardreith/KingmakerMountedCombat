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
            runner.Run("the native merge's missing bones are reported once each, in first-seen order", () =>
            {
                var avatar = new System.Collections.Generic.HashSet<string> { "Pelvis", "R_leg" };
                var missing = NativeRigMergePolicy.MissingBones(new[] { "Pelvis", "slot_10", "R_leg", "slot_11", "slot_10" }, avatar);
                TestRunner.True(missing.SequenceEqual(new[] { "slot_10", "slot_11" }), "Missing bones differ.");
                TestRunner.True(NativeRigMergePolicy.MissingBones(new[] { "Pelvis", "R_leg" }, avatar).Count == 0, "A complete rig reported a missing bone.");
            });
            runner.Run("exact declared Small observation produces one named body match", () =>
            { string error; var match = Declaration().Match(Observation(), out error); TestRunner.True(match != null && error == null && match.ProfileId == Profile, "Exact body was rejected."); });
            Refuse(runner, "original Medium reduced to Small", value => value.OriginalSizeOrdinal = 4);
            Refuse(runner, "original Small enlarged to Medium", value => value.EffectiveSizeOrdinal = 4);
            Refuse(runner, "rider polymorph", value => value.RiderPolymorphed = true);
            Refuse(runner, "mount polymorph", value => value.MountPolymorphed = true);
            Refuse(runner, "Small body borrowed for Mammoth", value => value.MountBlueprintGuid = "e7aa96d15a45238438ae4cfb476f6bb9");
            Refuse(runner, "mount no larger than Small rider", value => value.MountSizeOrdinal = 3);
            Refuse(runner, "other Small race", value => value.RaceGuid = new string('e', 32));
            Refuse(runner, "other sex", value => value.Gender = "Male");
            Refuse(runner, "other skeleton name", value => value.SkeletonName = "OtherSmallSkeleton");
            Refuse(runner, "unavailable native rig", value => value.NativeRigAvailable = false);
            Refuse(runner, "changed avatar scale", value => value.AvatarLocalScale = new PoseVector3(1.1f, 1f, 1f));
            Refuse(runner, "nonfinite avatar scale", value => value.AvatarLocalScale = new PoseVector3(float.NaN, 1f, 1f));
            // POSE-01: gear, cosmetics and a mirrored native rig do not change the rig family.
            Admit(runner, "other race preset", value => value.PresetGuid = new string('e', 32));
            Admit(runner, "other handedness", value => value.LeftHanded = true);
            Admit(runner, "a mirrored left-handed avatar", value => { value.AvatarMirrored = true; value.AvatarLocalScale = new PoseVector3(-1f, 1f, 1f); });
            Admit(runner, "missing Doll equipment", value => value.DollEquipmentEntityIds = null);
            Admit(runner, "other Doll equipment", value => value.DollEquipmentEntityIds = new[] { new string('e', 32) });
            Admit(runner, "another active weapon", value => value.CurrentEquipmentBlueprints[0] = new string('e', 32));
            Admit(runner, "other worn armor", value => value.CurrentEquipmentBlueprints[2] = new string('e', 32));
            Admit(runner, "other visual equipment", value => value.ActiveVisualEquipmentNames = new[] { "skin", "clothes", "wings" });
            Admit(runner, "a natural limb", value => value.AdditionalLimbCount = 1);
            // SIZE-01: native categories decide; a larger or reduced mount stays legal while it is larger than the rider.
            Admit(runner, "a Medium mount", value => value.MountSizeOrdinal = 4);
            Admit(runner, "a Huge mount", value => value.MountSizeOrdinal = 6);
            runner.Run("mount scale compensation undoes only later growth", () =>
            {
                var attach = new PoseVector3(1.515f, 1.515f, 1.515f);
                var same = MountScaleCompensation.AnchorScale(attach, attach);
                TestRunner.True(same.X == 1f && same.Y == 1f && same.Z == 1f, "A rider already compensated at attach was scaled again.");
                var grown = MountScaleCompensation.AnchorScale(attach, new PoseVector3(3.03f, 3.03f, 3.03f));
                TestRunner.True(Math.Abs(grown.Y - 0.5f) < 0.0001f, "Native mount growth reached the rider.");
                Throws2(() => MountScaleCompensation.AnchorScale(attach, new PoseVector3(0f, 1f, 1f)));
            });
            runner.Run("size policy requires one whole native category", () =>
            {
                TestRunner.True(MountedSizePolicy.Refusal(3, 4) == null && MountedSizePolicy.Refusal(3, 5) == null && MountedSizePolicy.Refusal(4, 5) == null, "A legal size pair was refused.");
                TestRunner.True(MountedSizePolicy.Refusal(4, 4) == MountedSizePolicy.TooSmallRefusal && MountedSizePolicy.Refusal(3, 3) != null && MountedSizePolicy.Refusal(4, 3) != null, "An equal or smaller mount was allowed.");
            });
            runner.Run("the published Small matrix has one profile per rig and mount", () =>
            {
                var ids = MountedRiderProfileCatalog.Small.Select(profile => profile.Body.ProfileId).ToArray();
                TestRunner.True(ids.Distinct().Count() == ids.Length && ids.Contains(MountedRiderProfileCatalog.OriginalSmallHorseId), "The Small matrix lost a profile or the original ID.");
                foreach (var id in ids) TestRunner.True(MountedRiderProfileCatalog.IsSupportedArchiveProfile(id), "A declared rig is not archive-eligible: " + id);
                foreach (var profile in MountedRiderProfileCatalog.Small)
                {
                    var rig = Observation(); rig.RaceGuid = profile.Body.RaceGuid; rig.Gender = profile.Body.Gender; rig.SkeletonName = profile.Body.SkeletonName;
                    rig.MountBlueprintGuid = profile.Body.MountBlueprintGuid; rig.MountSizeOrdinal = 5;
                    var claims = MountedRiderProfileCatalog.Small.Count(other => other.Body.Refusal(rig) == null);
                    TestRunner.True(profile.Body.Refusal(rig) == null && claims == 1, "A rig is claimed by " + claims + " profiles: " + profile.Body.ProfileId);
                    rig.MountSizeOrdinal = 3;
                    TestRunner.True(profile.Body.Refusal(rig) == MountedSizePolicy.TooSmallRefusal, "A mount no larger than its rider was admitted: " + profile.Body.ProfileId);
                }
            });
            runner.Run("each Small rig has a Riding Dog profile and no Medium rig does", () =>
            {
                var dog = MountedRiderProfileCatalog.Small.Where(profile => profile.Body.MountBlueprintGuid == MountedRiderProfileCatalog.RidingDogBlueprintGuid).ToArray();
                TestRunner.True(dog.Length == 12 && dog.Select(profile => profile.Body.SkeletonName).Distinct().Count() == 12 && dog.All(profile => profile.MountAnchorName == "UpperTorso" && profile.MountDisplayName == "Riding Dog"), "The Riding Dog profiles differ from the published matrix.");
            });
            runner.Run("every known preset rig has exactly one profile on each supported mount", () =>
            {
                var races = new[] { "Halfling", "Gnome" }; var sexes = new[] { "Male", "Female" }; var builds = new[] { "", "_fat", "_thin" };
                var mounts = new[] { DeclaredSmallRiderBody.HorseBlueprintGuid, MountedRiderProfileCatalog.RidingDogBlueprintGuid };
                TestRunner.True(MountedRiderProfileCatalog.Small.Count == 24, "The Small matrix is not twelve rigs on two mounts.");
                foreach (var race in races) foreach (var sex in sexes) foreach (var build in builds) foreach (var mount in mounts)
                {
                    var skeleton = sex + race + build;
                    TestRunner.True(MountedRiderProfileCatalog.Small.Count(profile => profile.Body.SkeletonName == skeleton && profile.Body.MountBlueprintGuid == mount && profile.Body.Gender == sex) == 1, "A preset rig lost its profile: " + skeleton);
                }
            });
            runner.Run("an unknown rig or a build suffix is never matched by a nearby declaration", () =>
            {
                foreach (var skeleton in new[] { "MaleHalfling_huge", "MaleHalfling_", "malehalfling_fat", "MaleHalfling_fat2", "MaleElf_fat" })
                {
                    var rig = Observation(); rig.RaceGuid = "b0c3ef2729c498f47970bb50fa1acd30"; rig.Gender = "Male"; rig.SkeletonName = skeleton;
                    TestRunner.True(MountedRiderProfileCatalog.Small.All(profile => profile.Body.Refusal(rig) != null), "An undeclared rig was admitted: " + skeleton);
                }
                var fat = Observation(); fat.RaceGuid = "b0c3ef2729c498f47970bb50fa1acd30"; fat.Gender = "Male"; fat.SkeletonName = "MaleHalfling_fat";
                TestRunner.True(MountedRiderProfileCatalog.Small.Single(profile => profile.Body.Refusal(fat) == null).Body.ProfileId == "small-halfling-male-fat-horse-v1", "The fat rig resolved to another profile.");
                fat.CurrentEquipmentBlueprints[2] = new string('e', 32); fat.ActiveVisualEquipmentNames = new[] { "other" };
                TestRunner.True(MountedRiderProfileCatalog.Small.Single(profile => profile.Body.Refusal(fat) == null).Body.ProfileId == "small-halfling-male-fat-horse-v1", "Gear changed the fat rig's profile.");
            });
            runner.Run("the first eight Small profile IDs are unchanged and stay archive-eligible", () =>
            {
                foreach (var id in new[] { "small-default-halfling-horse-v1", "small-halfling-female-horse-v1", "small-gnome-male-horse-v1", "small-gnome-female-horse-v1",
                    "small-halfling-male-ridingdog-v1", "small-halfling-female-ridingdog-v1", "small-gnome-male-ridingdog-v1", "small-gnome-female-ridingdog-v1" })
                    TestRunner.True(MountedRiderProfileCatalog.IsSupportedArchiveProfile(id) && MountedRiderProfileCatalog.Small.Count(profile => profile.Body.ProfileId == id) == 1, "A published Small ID moved: " + id);
            });
            runner.Run("a refusal names the exact body and the supported builds", () =>
            {
                var text = MountedRiderProfileCatalog.NoMeasuredSeatRefusal("Mammoth", "MaleHalfling_fat");
                TestRunner.True(text.Contains("MaleHalfling_fat") && text.Contains("Mammoth") && text.Contains("fat or thin"), "The refusal does not describe what is supported: " + text);
                TestRunner.True(!MountedRiderProfileCatalog.NoMeasuredSeatRefusal("Horse", null).Contains("()"), "A missing skeleton name printed an empty name.");
            });
            runner.Run("the original Small profile ID still names the male Halfling on the Horse", () =>
            {
                var body = MountedRiderProfileCatalog.Small.Single(profile => profile.Body.ProfileId == MountedRiderProfileCatalog.OriginalSmallHorseId).Body;
                TestRunner.True(body.SkeletonName == "MaleHalfling" && body.MountBlueprintGuid == DeclaredSmallRiderBody.HorseBlueprintGuid && body.HasExactFixturePins, "The original Small identity moved.");
            });
            ExactFixture(runner, "other race preset", value => value.PresetGuid = new string('e', 32));
            ExactFixture(runner, "same-name replacement native race object", value => value.NativeRaceAndPresetIdentityExact = false);
            ExactFixture(runner, "same-name replacement skeleton object", value => value.NativeSkeletonIsExactPreset = false);
            ExactFixture(runner, "other handedness", value => value.LeftHanded = true);
            ExactFixture(runner, "other mirror state", value => value.AvatarMirrored = true);
            ExactFixture(runner, "missing Doll equipment", value => value.DollEquipmentEntityIds = null);
            ExactFixture(runner, "duplicate Doll equipment", value => value.DollEquipmentEntityIds = new[] { Equipment, Equipment });
            ExactFixture(runner, "other Doll equipment", value => value.DollEquipmentEntityIds = new[] { new string('e', 32) });
            ExactFixture(runner, "undeclared active weapon", value => value.CurrentEquipmentBlueprints[0] = new string('e', 32));
            ExactFixture(runner, "undeclared worn armor", value => value.CurrentEquipmentBlueprints[2] = new string('e', 32));
            ExactFixture(runner, "missing explicit equipment slots", value => value.CurrentEquipmentBlueprints = new string[11]);
            ExactFixture(runner, "extra visual equipment", value => value.ActiveVisualEquipmentNames = new[] { "skin", "clothes", "wings" });
            ExactFixture(runner, "visual duplicate hides missing garment", value => value.ActiveVisualEquipmentNames = new[] { "skin", "skin" });
            ExactFixture(runner, "unexpected natural limb", value => value.AdditionalLimbCount = 1);
            ExactFixture(runner, "Horse enlarged to Huge", value => value.MountSizeOrdinal = 6);
            runner.Run("a rig-family declaration carries no exact fixture pins", () =>
            {
                var family = new DeclaredSmallRiderBody(Profile, DeclaredSmallRiderBody.HorseBlueprintGuid, Race, "Female", "SyntheticSmallSkeleton");
                TestRunner.True(!family.HasExactFixturePins && family.ExactFixtureRefusal(Observation()) != null && family.Refusal(Observation()) == null, "Rig-family pins differ.");
            });
            runner.Run("visual equipment comparison is order independent with exact multiplicity", () =>
            { var body = Observation(); body.ActiveVisualEquipmentNames = new[] { "clothes", "skin" }; TestRunner.True(Declaration().ExactFixtureRefusal(body) == null, "Equivalent visual multiset was rejected."); });
            runner.Run("repeated visual names retain their exact multiplicity", () =>
            {
                var declaration = Declaration(visual: new[] { "skin", "clothes", "skin" });
                var body = Observation(); body.ActiveVisualEquipmentNames = new[] { "skin", "skin", "clothes" };
                TestRunner.True(declaration.ExactFixtureRefusal(body) == null, "Equivalent repeated visual names were rejected.");
                body.ActiveVisualEquipmentNames = new[] { "skin", "clothes", "clothes" };
                TestRunner.True(declaration.ExactFixtureRefusal(body) != null, "A missing visual name was hidden by another duplicate.");
            });
            runner.Run("declaration owns copied equipment values", () =>
            {
                var slots = new string[12]; var declaration = Declaration(slots); slots[0] = new string('e', 32);
                TestRunner.True(declaration.ExactFixtureRefusal(Observation()) == null, "External array mutation changed declared equipment.");
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
            { TestRunner.True(MountedRiderProfileCatalog.Small.Count != 0 && !MountedRiderProfileCatalog.IsSupportedArchiveProfile(Profile), "Unmeasured synthetic profile was globally enabled."); });
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
                TestRunner.True(!MountedRiderProfileCatalog.IsSupportedArchiveProfile(Profile), "Constructing policy data changed the qualified native catalogue.");
            });
            runner.Run("qualified Halfling declaration and private calibration share exact identity", () =>
            {
                var provisional = MountedRiderProfileCatalog.PrivateCalibrationSmall.Single();
                var id = provisional.Body.ProfileId;
                TestRunner.True(id == "small-default-halfling-horse-v1" &&
                    provisional.Body.NativeInventorySha256 == "dcdc6cdce0265a221b2f3d890d34229385098235d2546df4a9c6a399239e48a9" &&
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
                    MountedRiderProfileCatalog.IsSupportedArchiveProfile(id),
                    "Released private Small calibration changed ordinary profile admission.");
            });
        }

        private static void Refuse(TestRunner runner, string name, Action<SmallRiderBodyObservation> change) => runner.Run("Small refuses " + name, () =>
        { var observed = Observation(); change(observed); string error; TestRunner.True(Declaration().Match(observed, out error) == null && error != null, "Changed body was admitted."); });
        private static void Admit(TestRunner runner, string name, Action<SmallRiderBodyObservation> change) => runner.Run("Small admits " + name, () =>
        { var observed = Observation(); change(observed); string error; TestRunner.True(Declaration().Match(observed, out error) != null && error == null, "A supported rig was refused: " + error); });
        private static void ExactFixture(TestRunner runner, string name, Action<SmallRiderBodyObservation> change) => runner.Run("Exact fixture pin refuses " + name, () =>
        { var observed = Observation(); change(observed); TestRunner.True(Declaration().ExactFixtureRefusal(observed) != null, "The exact fixture accepted a changed body."); });
        private static SmallRiderBodyMatch Match() { string error; return Declaration().Match(Observation(), out error); }
        private static DeclaredSmallRiderBody Declaration(string[] slots = null, string[] visual = null) => new DeclaredSmallRiderBody(Profile, new string('a', 64), Race, Preset,
            "Female", "SyntheticSmallSkeleton", false, false, new PoseVector3(1f, 1f, 1f), new[] { Equipment }, slots ?? new string[12], visual ?? new[] { "skin", "clothes" });
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
        private static void Throws2(Action action)
        { var threw = false; try { action(); } catch (InvalidOperationException) { threw = true; } TestRunner.True(threw, "An invalid mount scale was accepted."); }
        private static void Throws(Action action)
        { var threw = false; try { action(); } catch (ArgumentException) { threw = true; } TestRunner.True(threw, "Invalid declaration was accepted."); }
    }
}
