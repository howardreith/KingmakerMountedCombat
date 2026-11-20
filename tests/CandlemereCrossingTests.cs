using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // Known issues C04: Candlemere Tower, "wade through" while mounted.
    //
    // Production code executed here: MountedScriptZonePolicy (whether a seated rider is checked against the script zones
    // after the native zone tick), MountedScriptZoneObservations (where each rider was last checked, per mounting
    // instance) and the real MountedRelationshipCoordinator, whose Attach creates the mounting instance and whose cleanup
    // runs a runtime that clears the rider and mount references as the game runtime does. The adapter's own few lines
    // (read the relationship's rider and instance, ask, check, record) are restated in AdapterTick. Everything else is a
    // model, labelled as such:
    // - the native tick, as read from the installed IL: the product's synchronization of the rider before
    //   UnitMoveController stamps PreviousPosition (06009183), the awake-unit loop (a moving unit's Position follows its
    //   view; an attached rider's view follows the mount's), ScriptZoneController.Tick (060092A0) checking only units
    //   with HasMotionThisTick (0600830F), and the rider's LateUpdate synchronization;
    // - the authored crossing, as read from the installed Candlemere scene data and pinned by
    //   Test-CandlemereCrossingContracts.ps1: on a passed check, UnitInScriptZone(InteractingUnit, "SortcutBefore")
    //   chooses TeleportParty to CandlemereTowerShortcut (true) or CandlemereTowerShortcutBack (false). The "Wade
    //   through" object and Shortcut_Back are inside SortcutBefore; Shortcut is outside, 8.26 m away. The model keeps
    //   only those facts (the zone is the band 7 < z < 20 here, which holds the authored positions of the interaction and
    //   Shortcut_Back and not Shortcut; the pair starts outside it, beyond the near side);
    // - the native placement of the party at an enter point (each unit written directly, outside the controller tick).
    //
    // The order of the rider and its mount in native AwakeUnits is not established offline, so both orders run.
    internal static class CandlemereCrossingTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("known issues C04: the zone-recheck decision checks a seated rider only when mounted, without native motion, and moved since its last check", PolicyDecisions);
            runner.Run("known issues C04 (native tick and crossing modelled; the recheck decision is production): a pair ridden to the near side crosses with the check passed; without the recheck the carried rider is not in the zone and the party is sent back to the near side", MountedNearSideCrossing);
            runner.Run("known issues C04 (modelled native tick): the crossing works in both directions while mounted, the endpoint is kept through the mounted updates that follow, and a repeated activation decides its direction again", BothDirectionsAndRepeat);
            runner.Run("known issues C04 (modelled native tick): when the mount precedes its rider natively the rider already moves natively; no extra check is made and the crossing works either way", MountFirstOrderNeedsNothing);
            runner.Run("known issues C04 (modelled native tick): unmounted and unrelated units, a failed check, a manual dismount and a remount keep their native outcomes", Controls);
            runner.Run("C04 review R1 (real coordinator and reference clearing; observations production): a remount at the place the rider was last checked, after it left the zone dismounted, is checked again on the same and on another relationship; the reviewed rider-keyed cache was not", RemountAfterLeavingTheZone);
            runner.Run("C04 review R1 (observations production): an observation belongs to its mounting instance only; a stationary rider within it is not checked again; another instance or another world's unit is", ObservationsBelongToTheirInstance);
            runner.Run("C04 review R9 (zone loop production): a rider checked against two zones with a logger that throws on the first change still has the second zone checked and its membership right; a healthy logger receives both; a failing check still ends the pass", ThrowingSuccessLoggerZones);
        }

        private static void PolicyDecisions()
        {
            TestRunner.True(MountedScriptZonePolicy.RiderNeedsZoneRecheck(true, false, false, 0, 0, 0), "A seated rider never checked while mounted is not checked.");
            TestRunner.True(MountedScriptZonePolicy.RiderNeedsZoneRecheck(true, false, true, 0.5f, 0, 0), "A seated rider that moved is not checked.");
            TestRunner.True(!MountedScriptZonePolicy.RiderNeedsZoneRecheck(true, false, true, 0, 0, 0), "A seated rider that did not move is checked again.");
            TestRunner.True(!MountedScriptZonePolicy.RiderNeedsZoneRecheck(true, true, true, 3, 0, 0), "A rider native already checked this tick is checked twice.");
            TestRunner.True(!MountedScriptZonePolicy.RiderNeedsZoneRecheck(false, false, false, 3, 0, 0), "An unmounted rider is checked by the pair.");
            TestRunner.True(!MountedScriptZonePolicy.PositionChanged(0.000001f, 0, 0) && MountedScriptZonePolicy.PositionChanged(0.0001f, 0, 0),
                "Position equality does not follow Unity's Vector3 equality.");
        }

        private static void MountedNearSideCrossing()
        {
            foreach (var adapter in new[] { true, false })
            {
                var world = new World { Adapter = adapter };
                world.RideTo(World.NearSideApproach);
                var destination = world.WadeThrough(checkPassed: true);
                if (adapter)
                {
                    TestRunner.Equal("CandlemereTowerShortcut", destination, "The mounted pair was not sent across.");
                    world.Frames(5);
                    TestRunner.True(world.FarSide(world.Rider) && world.FarSide(world.Mount) && world.Seated, "The pair did not keep the far-side endpoint while mounted.");
                }
                else
                {
                    // The reviewed base: the carried rider never entered the zone, so the false branch ran.
                    TestRunner.Equal("CandlemereTowerShortcutBack", destination, "Setup (base without the recheck): the party should have been sent back.");
                    world.Frames(5);
                    TestRunner.True(!world.FarSide(world.Rider) && !world.FarSide(world.Mount), "Setup (base without the recheck): the pair should still be on the near side.");
                }
            }
        }

        private static void BothDirectionsAndRepeat()
        {
            var world = new World { Adapter = true };
            world.RideTo(World.NearSideApproach);
            TestRunner.Equal("CandlemereTowerShortcut", world.WadeThrough(true), "First crossing.");
            world.Frames(3);
            // The far-side approach: the rider left the zone with the pair.
            world.RideTo(World.FarSideApproach);
            TestRunner.True(!world.Zone.Contains(world.Rider), "The carried rider is still a member of the near-side zone on the far side.");
            TestRunner.Equal("CandlemereTowerShortcutBack", world.WadeThrough(true), "The way back.");
            world.Frames(3);
            TestRunner.True(!world.FarSide(world.Rider) && world.Seated, "The pair did not keep the near-side endpoint.");
            // A repeated activation (native: the stored outcome's actions run on every use) decides again from membership.
            world.RideTo(World.NearSideApproach);
            TestRunner.Equal("CandlemereTowerShortcut", world.WadeThrough(true), "A repeated crossing went the wrong way.");
        }

        private static void MountFirstOrderNeedsNothing()
        {
            foreach (var adapter in new[] { true, false })
            {
                var world = new World { Adapter = adapter, MountFirst = true };
                world.RideTo(World.NearSideApproach);
                TestRunner.Equal("CandlemereTowerShortcut", world.WadeThrough(true), "With the mount first, the native recheck alone should cross.");
                TestRunner.True(world.AdapterChecksWhileMoving == 0, "The pair checked a rider native had already checked.");
            }
        }

        private static void Controls()
        {
            // A dismounted rider walks natively; the pair checks nothing; the crossing works with or without the recheck.
            foreach (var adapter in new[] { true, false })
            {
                var world = new World { Adapter = adapter };
                world.Dismount();
                world.WalkRiderTo(World.NearSideApproach);
                TestRunner.Equal("CandlemereTowerShortcut", world.WadeThrough(true), "A dismounted crossing changed.");
                TestRunner.True(world.AdapterChecks == 0, "The pair checked an unmounted rider.");
            }
            // An unrelated walking unit and an unrelated parked pair: only the moving seated rider is checked.
            var other = new World { Adapter = true };
            var bystander = other.AddUnit("bystander", 0, 20);
            other.RideTo(World.NearSideApproach);
            TestRunner.True(other.CheckCountFor(bystander) == 0, "An unrelated idle unit was checked.");
            TestRunner.True(other.AdapterChecksFor(other.Mount) == 0 && other.AdapterChecksFor(bystander) == 0, "The pair checked a unit other than its seated rider.");
            // A failed check runs its own actions (the native interaction is disabled) and moves nobody.
            var failed = new World { Adapter = true };
            failed.RideTo(World.NearSideApproach);
            TestRunner.Equal(null, failed.WadeThrough(false), "A failed check moved the party.");
            TestRunner.True(failed.InteractionDisabled && !failed.FarSide(failed.Rider), "A failed check did not keep its native outcome.");
            // A manual dismount (real cleanup, references cleared): nothing is checked while dismounted; a remount checks the
            // rider again on its first frame.
            var remount = new World { Adapter = true };
            remount.RideTo(World.NearSideApproach);
            remount.Dismount();
            var before = remount.AdapterChecks;
            remount.Frames(3);
            TestRunner.True(remount.AdapterChecks == before && remount.RuntimeRider == null, "A dismounted rider was checked, or cleanup kept the reference.");
            remount.Remount();
            remount.Frames(1);
            TestRunner.True(remount.AdapterChecks == before + 1 && remount.Zone.Contains(remount.Rider), "A remounted rider was not checked on its first frame.");
            remount.Frames(5);
            TestRunner.True(remount.AdapterChecks == before + 1, "A stationary seated rider was checked again within one mounting.");
        }

        // Review R1 of b30c24f8: the rider was checked at P inside the zone; a normal dismount cleared the relationship's
        // references; the rider walked out (native updates its membership); it remounts the stationary mount at P, which
        // writes it to the saddle without native motion. The first mounted tick must check it again.
        private static void RemountAfterLeavingTheZone()
        {
            foreach (var mode in new[] { CacheMode.Production, CacheMode.ReviewedRiderKeyed })
                foreach (var anotherBoundary in new[] { false, true })
                {
                    var world = new World { Adapter = true, Cache = mode };
                    world.RideTo(World.EdgeInside);
                    TestRunner.True(world.Zone.Contains(world.Rider), "Setup: the mounted rider was not checked inside the zone at P.");
                    world.Dismount();
                    TestRunner.True(world.RuntimeRider == null, "Setup: cleanup did not clear the relationship's rider reference.");
                    world.WalkRiderTo(World.EdgeOutside);
                    TestRunner.True(!world.Zone.Contains(world.Rider), "Setup: native did not update the walking rider's membership.");
                    if (anotherBoundary) world.RemountOnAnotherRelationship(); else world.Remount();
                    world.Frames(1);
                    var inside = world.Zone.Contains(world.Rider);
                    var destination = world.WadeThrough(true);
                    if (mode == CacheMode.Production)
                        TestRunner.True(inside && destination == "CandlemereTowerShortcut",
                            "The remounted rider kept its stale membership (" + (anotherBoundary ? "another" : "the same") + " relationship).");
                    else
                        TestRunner.True(!inside && destination == "CandlemereTowerShortcutBack",
                            "Setup (reviewed cache): the stale observation should have suppressed the first mounted check.");
                }
        }

        private static void ThrowingSuccessLoggerZones()
        {
            // Two active zones the rider is now inside; native has it outside both (it was carried in).
            var rider = new Unit { Name = "rider", X = 4f, Z = 10f };
            var first = new Zone(); var second = new Zone();
            var zones = new List<Zone> { first, second };
            var reports = 0;
            var changes = MountedScriptZoneChecks.CheckEach(zones, zone => zone.Contains(rider), zone => zone.CheckUnit(rider),
                (zone, inside) => { reports++; throw new InvalidOperationException("the logger failed"); });
            TestRunner.True(changes == 2 && reports == 2 && first.Contains(rider) && second.Contains(rider),
                "A throwing success logger stopped the rider's remaining zones.");
            var lines = new List<string>();
            var healthy = new Unit { Name = "rider", X = 4f, Z = 10f };
            MountedScriptZoneChecks.CheckEach(new List<Zone> { new Zone(), new Zone() }, zone => zone.Contains(healthy), zone => zone.CheckUnit(healthy),
                (zone, inside) => lines.Add("inside=" + inside));
            TestRunner.True(lines.Count == 2 && lines[0] == "inside=True", "A healthy logger did not receive both membership changes.");
            // An operational failure (the native check itself throws) still ends the pass, for the hook's bounded report.
            var thrown = false;
            try { MountedScriptZoneChecks.CheckEach(zones, zone => false, zone => { throw new InvalidOperationException("check failed"); }, null); }
            catch (InvalidOperationException) { thrown = true; }
            TestRunner.True(thrown, "A failing zone check was swallowed by the loop instead of reaching the hook's fault report.");
        }

        private static void ObservationsBelongToTheirInstance()
        {
            var observations = new MountedScriptZoneObservations<Unit>();
            var rider = new Unit { Name = "rider" };
            var first = new MountedPair("rider-a", "horse-a");
            TestRunner.True(observations.NeedsRecheck(rider, first, false, 1, 0, 1), "A rider never checked in this mounting was not checked.");
            observations.Record(rider, first, 1, 0, 1);
            TestRunner.True(!observations.NeedsRecheck(rider, first, false, 1, 0, 1), "A stationary rider was checked again within its mounting.");
            TestRunner.True(observations.NeedsRecheck(rider, first, false, 1.5f, 0, 1), "A carried rider that moved was not checked.");
            TestRunner.True(!observations.NeedsRecheck(rider, first, true, 2, 0, 1) && !observations.NeedsRecheck(rider, first, false, 2, 0, 1),
                "A tick with native motion was not taken as native's own check.");
            // The same rider, the same place, a later mounting (same or another relationship): not the instance asked about.
            var later = new MountedPair("rider-a", "horse-a");
            TestRunner.True(observations.NeedsRecheck(rider, later, false, 2, 0, 1), "A later mounting inherited an earlier observation.");
            // Another world's unit with the same identity and place is another object.
            TestRunner.True(observations.NeedsRecheck(new Unit { Name = "rider" }, first, false, 2, 0, 1), "Another world's unit inherited an observation.");
            TestRunner.True(!observations.NeedsRecheck(null, first, false, 0, 0, 0) && !observations.NeedsRecheck(rider, null, false, 0, 0, 0),
                "An absent rider or mounting was checked.");
        }

        // ---- the model ----
        private sealed class Unit
        {
            public string Name;
            public float X, Z, PrevX, PrevZ;
            public bool HasMotionThisTick => !(Sq(X - PrevX, Z - PrevZ) < MountedScriptZonePolicy.SamePositionSquaredDistance);
        }

        private static float Sq(float dx, float dz) => dx * dx + dz * dz;

        private enum CacheMode { Production, ReviewedRiderKeyed }

        // The game runtime's reference handling, as the coordinator drives it: attachment seats the rider; cleanup puts it
        // beside the mount and clears both references (a later tick reads a null rider from the relationship).
        private sealed class PairRuntime : IMountedPairRuntime
        {
            public Unit Rider, Mount;
            private readonly Unit rider, mount;
            public PairRuntime(Unit rider, Unit mount) { this.rider = rider; this.mount = mount; }
            public void AcquireMovementAuthority(MountedPair pair) { Rider = rider; Mount = mount; }
            public void AttachPresentation(MountedPair pair) { }
            public void RestorePresentation(MountedPair pair) { }
            public void RestoreMovementAuthority(MountedPair pair, CleanupTrigger trigger)
            {
                rider.X = mount.X + 1.0f; rider.Z = mount.Z;
                Rider = null; Mount = null;
            }
        }

        // The reviewed b30c24f8 adapter's cache, transcribed only to show the sequence the correction changes (not
        // production): keyed by the rider alone; its one removal is reachable only while the relationship still names a rider.
        private sealed class ReviewedRiderKeyedCache
        {
            private readonly Dictionary<Unit, float[]> checkedAt = new Dictionary<Unit, float[]>();
            public void Tick(Unit relationshipRider, bool mounted, Zone zone, Action counted)
            {
                if (relationshipRider == null) return;
                float[] last; var known = checkedAt.TryGetValue(relationshipRider, out last);
                if (!mounted) { if (known) checkedAt.Remove(relationshipRider); return; }
                var native = relationshipRider.HasMotionThisTick;
                var dx = known ? relationshipRider.X - last[0] : 0f; var dz = known ? relationshipRider.Z - last[1] : 0f;
                if (!MountedScriptZonePolicy.RiderNeedsZoneRecheck(true, native, known, dx, 0, dz))
                {
                    if (native) checkedAt[relationshipRider] = new[] { relationshipRider.X, relationshipRider.Z };
                    return;
                }
                zone.CheckUnit(relationshipRider);
                counted();
                checkedAt[relationshipRider] = new[] { relationshipRider.X, relationshipRider.Z };
            }
        }

        private static MountedPairCandidate PairCandidate() =>
            new MountedPairCandidate("rider-a", "horse-a")
            {
                RiderIsDirectlyControllable = true, MountIsDirectlyControllable = true, RiderIsAliveAndConscious = true, MountIsAliveAndConscious = true,
                ExactReciprocalCompanionRelationship = true, RiderSizeOrdinal = 4, MountSizeOrdinal = 5, RiderViewAndStockAgentAvailable = true,
                MountViewAndStockAgentAvailable = true, RiderStockAgentEnabled = true, MountStockAgentEnabled = true, RiderAgentOverrideAvailable = true,
                MountAgentOverrideAvailable = true, RiderIsExactlyMedium = true, SafeMovementMode = true
            };

        private sealed class Zone
        {
            private readonly HashSet<Unit> inside = new HashSet<Unit>();
            public readonly Dictionary<Unit, int> Checks = new Dictionary<Unit, int>();
            public static bool InShape(float z) => z > 7f && z < 20f;   // SortcutBefore, as far as the authored positions go
            public bool Contains(Unit unit) => inside.Contains(unit);
            // Native ScriptZone.CheckUnit (06001AEB): add or remove by the unit's position.
            public void CheckUnit(Unit unit)
            {
                int n; Checks.TryGetValue(unit, out n); Checks[unit] = n + 1;
                if (InShape(unit.Z)) inside.Add(unit); else inside.Remove(unit);
            }
        }

        private sealed class World
        {
            // Authored world positions (x, z) from the installed scene: the interaction and the two enter points.
            public static readonly float[] FindWay = { 4.44f, 9.14f }, Shortcut = { 4.20f, 5.20f }, ShortcutBack = { 4.05f, 13.46f };
            public static readonly float[] NearSideApproach = { 4.4f, 11.5f }, FarSideApproach = { 4.3f, 4.0f };
            // Review R1: a place just inside the zone's edge, and one just outside it.
            public static readonly float[] EdgeInside = { 4.4f, 7.6f }, EdgeOutside = { 4.4f, 6.4f };

            public bool Adapter, MountFirst, InteractionDisabled;
            public CacheMode Cache = CacheMode.Production;
            public readonly Unit Rider = new Unit { Name = "rider", X = 4f, Z = 25f, PrevX = 4f, PrevZ = 25f };
            public readonly Unit Mount = new Unit { Name = "mount", X = 4f, Z = 25f, PrevX = 4f, PrevZ = 25f };
            public readonly List<Unit> Others = new List<Unit>();
            public readonly Zone Zone = new Zone();
            public int AdapterChecks, AdapterChecksWhileMoving;
            private readonly Dictionary<Unit, int> adapterChecks = new Dictionary<Unit, int>();
            private readonly MountedScriptZoneObservations<Unit> observations = new MountedScriptZoneObservations<Unit>();
            private readonly ReviewedRiderKeyedCache reviewed = new ReviewedRiderKeyedCache();
            private PairRuntime runtime;
            private MountedRelationshipCoordinator coordinator;
            private bool mountMovingThisFrame;

            public World()
            {
                // The pair starts outside the zone, beyond the near side, and rides into it.
                TestRunner.True(!Zone.InShape(Rider.Z), "Setup: the pair should start outside the zone.");
                Attach(newRelationship: true);
            }

            public bool Seated => coordinator.State == RelationshipState.Mounted;
            public Unit RuntimeRider => runtime.Rider;

            private void Attach(bool newRelationship)
            {
                if (newRelationship)
                {
                    runtime = new PairRuntime(Rider, Mount);
                    coordinator = new MountedRelationshipCoordinator(runtime);
                }
                TestRunner.True(coordinator.Mount(PairCandidate()).Succeeded, "Setup: the pair did not mount.");
                // The product's configuration writes the rider to the saddle (no native motion).
                Rider.X = Mount.X; Rider.Z = Mount.Z;
            }
            public bool FarSide(Unit unit) => unit.Z < 7f;
            public int CheckCountFor(Unit unit) { int n; return Zone.Checks.TryGetValue(unit, out n) ? n : 0; }
            public int AdapterChecksFor(Unit unit) { int n; return adapterChecks.TryGetValue(unit, out n) ? n : 0; }

            public Unit AddUnit(string name, float x, float z)
            {
                var unit = new Unit { Name = name, X = x, Z = z, PrevX = x, PrevZ = z };
                Others.Add(unit);
                return unit;
            }

            // The mount walks natively toward a point, one native step per frame; a seated rider is carried.
            public void RideTo(float[] point)
            {
                for (var guard = 0; guard < 400 && Sq(Mount.X - point[0], Mount.Z - point[1]) > 0.0025f; guard++)
                    Frame(() => Step(Mount, point));
                Frames(2);
            }

            public void WalkRiderTo(float[] point)
            {
                for (var guard = 0; guard < 400 && Sq(Rider.X - point[0], Rider.Z - point[1]) > 0.0025f; guard++)
                    Frame(() => Step(Rider, point));
                Frames(2);
            }

            private static void Step(Unit unit, float[] point)
            {
                var dx = point[0] - unit.X; var dz = point[1] - unit.Z;
                var d = (float)Math.Sqrt(Sq(dx, dz));
                var s = Math.Min(0.25f, d);
                unit.X += dx / d * s; unit.Z += dz / d * s;
            }

            public void Frames(int n) { for (var i = 0; i < n; i++) Frame(null); }

            // One frame of the native controllers with the product's hooks, as read from the installed IL.
            private void Frame(Action nativeMove)
            {
                // The product's UnitMoveController prefix: the seated rider is synchronized to the saddle before the stamp.
                if (Seated) { Rider.X = Mount.X; Rider.Z = Mount.Z; }
                // UnitMoveController.Tick: PreviousPosition is stamped for every awake unit, then each unit moves.
                foreach (var unit in AllUnits()) { unit.PrevX = unit.X; unit.PrevZ = unit.Z; }
                mountMovingThisFrame = nativeMove != null;
                var order = MountFirst ? new[] { Mount, Rider } : new[] { Rider, Mount };
                foreach (var unit in order)
                {
                    if (unit == Rider && Seated)
                    {
                        // A seated rider's view is attached to the mount's: when the mount already moved this tick, the
                        // rider's Position follows the view; otherwise it stays where the prefix put it.
                        if (MountFirst && mountMovingThisFrame) { Rider.X = Mount.X; Rider.Z = Mount.Z; }
                        continue;
                    }
                    if (nativeMove != null) nativeMove.Invoke();
                    nativeMove = null;
                }
                // ScriptZoneController.Tick: only units with native motion this tick are checked.
                foreach (var unit in AllUnits()) if (unit.HasMotionThisTick) Zone.CheckUnit(unit);
                // The product's postfix: the production decision for the seated rider.
                if (Adapter) AdapterTick();
                // LateUpdate: the seated rider is written to the saddle.
                if (Seated) { Rider.X = Mount.X; Rider.Z = Mount.Z; }
            }

            // MountedScriptZoneAdapter.Recheck for this relationship: the rider and mounting instance are read from the
            // relationship (null after cleanup), the decision and the record are the production observations.
            private void AdapterTick()
            {
                var rider = runtime.Rider;
                if (Cache == CacheMode.ReviewedRiderKeyed) { reviewed.Tick(rider, Seated, Zone, () => Count(rider)); return; }
                var instance = coordinator.ActivePair;
                if (rider == null || instance == null || !Seated) return;
                if (!observations.NeedsRecheck(rider, instance, rider.HasMotionThisTick, rider.X, 0, rider.Z)) return;
                Zone.CheckUnit(rider);
                Count(rider);
                observations.Record(rider, instance, rider.X, 0, rider.Z);
            }

            private void Count(Unit rider)
            {
                AdapterChecks++;
                if (mountMovingThisFrame && MountFirst) AdapterChecksWhileMoving++;
                int n; adapterChecks.TryGetValue(rider, out n); adapterChecks[rider] = n + 1;
            }

            private IEnumerable<Unit> AllUnits() => new[] { Rider, Mount }.Concat(Others);

            // InteractionSkillCheck "FindWay" used by the rider (the interacting unit), within its 4 m approach radius.
            public string WadeThrough(bool checkPassed)
            {
                TestRunner.True(Math.Sqrt(Sq(Rider.X - FindWay[0], Rider.Z - FindWay[1])) <= 4.0 || FarSide(Rider),
                    "Setup: the interacting unit is not within the interaction's approach radius.");
                if (!checkPassed) { InteractionDisabled = true; return null; }
                // Conditional "which side of the passage we are on": UnitInScriptZone(InteractingUnit, SortcutBefore).
                var target = Zone.Contains(Rider) ? Shortcut : ShortcutBack;
                var name = target == Shortcut ? "CandlemereTowerShortcut" : "CandlemereTowerShortcutBack";
                // Game.Teleport, inside its loading process: every controllable unit written directly (the pet beside).
                Rider.X = target[0]; Rider.Z = target[1];
                Mount.X = target[0] + 1.2f; Mount.Z = target[1];
                return name;
            }

            // A normal successful dismount through the real coordinator: the runtime puts the rider beside the mount and
            // clears the relationship's references.
            public void Dismount()
            {
                var result = coordinator.Dismount(CleanupTrigger.Manual);
                TestRunner.True(result.Succeeded && coordinator.State == RelationshipState.Unmounted && coordinator.ActivePair == null,
                    "Setup: the dismount did not complete cleanly.");
            }

            public void Remount() => Attach(newRelationship: false);

            public void RemountOnAnotherRelationship() => Attach(newRelationship: true);
        }
    }
}
