using System;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // Mounted Archery and the mounted ranged-movement rule. Travel is produced by the production MountedMovementState.Advance
    // with native-shaped ticks (the delegated mount path mirrors TurnController.TickMovement), not restated constants.
    internal static class MountedRangedMovementTests
    {
        private const float HorseSpeedMps = 50f * 0.3048f / 3f;
        private const float StepRange = 1.524f;
        private const float TbTick = 3f / 12f;

        public static void Register(TestRunner runner)
        {
            runner.Run("mounted ranged movement: stationary and one move contribute nothing; a double move gives -4, or -2 with the feat", Contributions);
            runner.Run("mounted ranged movement: the baseline needs neither feat and leaves other modifiers to the native sum", BaselineAndIsolation);
            runner.Run("mounted ranged movement: native endpoint borrowed time at the first-move boundary is still one move", BorrowedTimeBoundary);
            runner.Run("mounted ranged movement: short legs accumulate; speed changes never manufacture a second move", LegsAndSpeed);
            runner.Run("mounted ranged movement: stand-up, a spent Standard, steps, charges and cancellations are not double moves", NonQualifyingTravel);
            runner.Run("mounted ranged movement: auto-stop and a second click are classified by actual travel", AutoStopAndSecondClick);
            runner.Run("mounted ranged movement: each roll reads travel already made; earlier shots are not revised", PerRollTiming);
            runner.Run("mounted ranged movement: rider ownership, active pair, mode and attack kind gate the contribution", Gates);
            runner.Run("mounted ranged movement: only a wielded ranged weapon qualifies; firearms' touch AC is not an exclusion", WeaponAttacks);
            runner.Run("mounted ranged movement: Dead Shot probes and its prefix confirmation are committed weapon rolls", DeadShotShapes);
            runner.Run("mounted ranged movement: the allocation must be current for the pair's activation", AllocationCurrency);
            runner.Run("mounted ranged movement: a mid-encounter mount (PreparePartnerThisRound) charges the mounted travel, not earlier riderless travel", MidEncounterAdoption);
            runner.Run("mounted ranged movement: retained travel survives persistence and dismount; a fresh grant resets it", Lifetime);
            runner.Run("mounted archery publication: both KMC combat feats share one exact lease per selection array", CoordinatedLease);
            runner.Run("mounted archery publication: independent leases of one array would conflict", IndependentLeasesConflict);
            runner.Run("mounted archery publication: a drifted extra list stops offering it while the rule stays on", ExtraListDrift);
            runner.Run("mounted archery publication: a drifted required list withdraws the feat everywhere and turns the rule off", RequiredListDrift);
            runner.Run("mounted archery publication: disable, re-enable and a missing feat keep the lists and the rule coherent", PublicationLifecycle);
        }

        // One native-shaped path: full ticks of `tick` (the first may differ), the last tick near the endpoint.
        private sealed class Mount
        {
            internal readonly MountedMovementState State = new MountedMovementState();
            internal float MoveAction;
            internal bool StandardUsed;
            internal float Speed = HorseSpeedMps;

            internal float Travel(float pathSeconds, float tick = TbTick, float firstTick = 0f, bool step = false, bool forced = false,
                bool singleMove = false, bool autoStop = false)
            {
                var left = pathSeconds;
                var moved = 0f;
                var request = firstTick > 0f ? firstTick : tick;
                while (left > 1e-6f)
                {
                    float debit;
                    var allowed = State.Advance(request, Speed, StepRange, MoveAction, StandardUsed, false, step, singleMove, forced,
                        left <= request, autoStop, out debit);
                    if (forced) debit = allowed;
                    MoveAction += debit;
                    if (allowed <= 0f) break;
                    left -= allowed;
                    moved += allowed;
                    request = tick;
                }
                return moved;
            }

            internal float Normal => MountedRangedMovementPolicy.NormalTravelSeconds(State.TimeMoved, State.TimeForced, State.TimeStepped);
        }

        private static MountedRangedMovementFacts Rider(float travel, bool owns = false) => new MountedRangedMovementFacts
        {
            Available = true, TurnBased = true, ActiveRider = true, QualifyingAttack = true, CurrentAllocation = true,
            NormalTravelSeconds = travel, OwnsMountedArchery = owns
        };

        private static int Contribution(MountedRangedMovementFacts facts)
        {
            int value;
            MountedRangedMovementPolicy.Evaluate(facts, out value);
            return value;
        }

        private static void Contributions()
        {
            var stationary = new Mount();
            TestRunner.Equal(0, Contribution(Rider(stationary.Normal)), "A stationary mount contributes nothing.");
            var single = new Mount();
            single.Travel(3f);
            TestRunner.Equal(0, Contribution(Rider(single.Normal)), "One full move contributes nothing.");
            TestRunner.Equal(0, Contribution(Rider(single.Normal, true)), "One full move contributes nothing with the feat.");
            var twice = new Mount();
            twice.Travel(6f);
            TestRunner.True(twice.MoveAction > 3f && twice.Normal > 5.9f, "The double move reached the second allowance natively.");
            TestRunner.Equal(-4, Contribution(Rider(twice.Normal)), "A double move gives -4.");
            TestRunner.Equal(-2, Contribution(Rider(twice.Normal, true)), "Mounted Archery reduces it to -2.");
            int value;
            TestRunner.Equal(MountedRangedMovementOutcome.Applied, MountedRangedMovementPolicy.Evaluate(Rider(twice.Normal), out value), "Applied outcome.");
            TestRunner.Equal(MountedRangedMovementOutcome.WithinFirstMove, MountedRangedMovementPolicy.Evaluate(Rider(single.Normal), out value), "One-move outcome.");
        }

        private static void BaselineAndIsolation()
        {
            // The facts carry no Mounted Combat ownership at all: a rider owning neither feat receives the baseline.
            TestRunner.Equal(-4, Contribution(Rider(4f, false)), "A rider with neither feat takes the baseline.");
            // The rule is a fixed contribution of its own; it takes no other modifier as input, so it cannot scale or
            // replace one. (The native additive sum is the seam's contract, pinned in Test-AssemblyContracts.ps1.)
            foreach (var travel in new[] { 0f, 1f, 3f, 3.25f, 3.26f, 6f, 12f })
                foreach (var owns in new[] { false, true })
                {
                    var value = Contribution(Rider(travel, owns));
                    TestRunner.True(value == 0 || value == -4 || value == -2, "Only 0, -4 or -2 is ever produced.");
                    TestRunner.True(value <= 0, "The rule is never a bonus.");
                }
        }

        private static void BorrowedTimeBoundary()
        {
            // A single move whose last tick (inside UnitMovementAgent.NearTheEnd) crosses the 3 s boundary is not clamped.
            var oneMove = new Mount();
            oneMove.Travel(3f, TbTick, 0.15f);
            TestRunner.True(oneMove.State.TimeMoved > 3f && oneMove.MoveAction > 3f, "Native borrowed time overshoots the first allowance.");
            TestRunner.Equal(0, Contribution(Rider(oneMove.Normal)), "Borrowed endpoint time is still one move.");
            // The worst case: the largest native turn-based tick, with almost nothing left of the first allowance.
            var worst = new Mount();
            worst.Travel(3f, TbTick, TbTick - 1e-4f);
            TestRunner.True(worst.State.TimeMoved > 3.24f && worst.State.TimeMoved <= MountedRangedMovementPolicy.SecondMoveThresholdSeconds, "Worst borrowed tick stays at the threshold.");
            TestRunner.Equal(0, Contribution(Rider(worst.Normal)), "The worst native borrowed tick is still one move.");
            TestRunner.True(!MountedRangedMovementPolicy.EnteredSecondMove(MountedRangedMovementPolicy.SecondMoveThresholdSeconds), "The threshold itself is one move.");
            TestRunner.True(MountedRangedMovementPolicy.EnteredSecondMove(MountedRangedMovementPolicy.SecondMoveThresholdSeconds + 0.001f), "Beyond the threshold is a double move.");
            TestRunner.True(Math.Abs(MountedRangedMovementPolicy.NativeMaximumTurnBasedTickSeconds - 0.25f) < 1e-6f, "TimeController caps a TB tick at 3/12 s.");
            // Genuine travel past the tolerance counts, at a normal frame rate.
            var genuine = new Mount();
            genuine.Travel(3.5f, 1f / 60f);
            TestRunner.Equal(-4, Contribution(Rider(genuine.Normal)), "Half a second into the second move is a double move.");
        }

        private static void LegsAndSpeed()
        {
            var legs = new Mount();
            for (var index = 0; index < 4; index++) legs.Travel(0.7f, 1f / 30f);
            TestRunner.Equal(0, Contribution(Rider(legs.Normal)), "Four short legs inside one move contribute nothing.");
            legs.Travel(0.7f, 1f / 30f);
            TestRunner.Equal(-4, Contribution(Rider(legs.Normal)), "A fifth leg past the first allowance is a double move.");
            // Haste or a speed change covers more ground in the same movement time; it never adds an allowance.
            var hasted = new Mount();
            hasted.Travel(2f);
            var metresBefore = 2f * hasted.Speed;
            hasted.Speed *= 2f;
            hasted.Travel(0.9f);
            TestRunner.True(metresBefore + 0.9f * hasted.Speed > 3f * HorseSpeedMps && hasted.MoveAction <= 3f,
                "Doubled speed covers more than one base move's distance inside the first allowance.");
            TestRunner.Equal(0, Contribution(Rider(hasted.Normal)), "One move's worth of time at any speed is one move.");
        }

        private static void NonQualifyingTravel()
        {
            var stoodUp = new Mount { MoveAction = 3f };
            stoodUp.Travel(6f);
            TestRunner.True(stoodUp.MoveAction >= 6f - 1e-4f, "Standing up spent the first Move without locomotion.");
            TestRunner.Equal(0, Contribution(Rider(stoodUp.Normal)), "Stand-up plus one move is one move of travel.");
            var attacked = new Mount { StandardUsed = true };
            attacked.Travel(6f);
            TestRunner.Equal(0, Contribution(Rider(attacked.Normal)), "With its Standard spent the mount can only make one move.");
            var stepped = new Mount();
            stepped.Travel(0.3f, TbTick, 0f, step: true);
            stepped.Travel(5f);
            TestRunner.True(stepped.State.TimeStepped > 0f && stepped.Normal <= 0f, "A five-foot step is step time and forbids normal travel.");
            TestRunner.Equal(0, Contribution(Rider(stepped.Normal)), "A five-foot step is not a double move.");
            var charged = new Mount();
            charged.Travel(5f, TbTick, 0f, forced: true);
            charged.Travel(2f);
            TestRunner.True(charged.State.TimeForced >= 5f - 1e-4f && charged.Normal <= 0f, "Charge travel is forced time and ends normal travel.");
            TestRunner.Equal(0, Contribution(Rider(charged.Normal)), "A charge is not a double move.");
            var cancelled = new Mount();
            cancelled.Travel(0f);
            TestRunner.Equal(0, Contribution(Rider(cancelled.Normal)), "A cancelled command with no travel contributes nothing.");
            TestRunner.Equal(0f, MountedRangedMovementPolicy.NormalTravelSeconds(float.NaN, 0f, 0f), "Invalid native state reads as no travel.");
        }

        private static void AutoStopAndSecondClick()
        {
            var stopped = new Mount();
            stopped.Travel(5f, TbTick, 0f, autoStop: true);
            TestRunner.True(stopped.State.TimeMoved <= 3f + TbTick && stopped.State.AutoStopPending == false, "Native auto-stop ended the move at the first allowance.");
            TestRunner.Equal(0, Contribution(Rider(stopped.Normal)), "An auto-stopped move is one move.");
            stopped.Travel(1f, TbTick, 0f, autoStop: true);
            TestRunner.Equal(-4, Contribution(Rider(stopped.Normal)), "Travel ordered after the auto-stop is the second move.");
            var clicks = new Mount();
            clicks.Travel(1f);
            clicks.Travel(1f);
            TestRunner.Equal(0, Contribution(Rider(clicks.Normal)), "A second movement click is not inherently a double move.");
        }

        private static void PerRollTiming()
        {
            // move -> shoot -> move -> shoot: each roll reads the travel already committed when it is made.
            var mount = new Mount();
            mount.Travel(2.5f);
            var early = Contribution(Rider(mount.Normal, false));
            mount.Travel(2.5f);
            var late = Contribution(Rider(mount.Normal, false));
            TestRunner.Equal(0, early, "The shot before the threshold is unpenalized.");
            TestRunner.Equal(-4, late, "The shot after the threshold is penalized.");
            // shoot -> double move: the shot resolved at zero travel.
            var before = new Mount();
            var shot = Contribution(Rider(before.Normal));
            before.Travel(6f);
            TestRunner.Equal(0, shot, "A shot before a double move stays at zero.");
        }

        private static void Gates()
        {
            int value;
            var facts = Rider(5f, true);
            facts.Available = false;
            TestRunner.Equal(MountedRangedMovementOutcome.Unavailable, MountedRangedMovementPolicy.Evaluate(facts, out value), "No penalty while the feat is unavailable.");
            TestRunner.Equal(0, value, "Unavailable contributes nothing.");
            facts = Rider(5f);
            facts.TurnBased = false;
            TestRunner.Equal(MountedRangedMovementOutcome.NotTurnBased, MountedRangedMovementPolicy.Evaluate(facts, out value), "Real time is unchanged.");
            TestRunner.Equal(0, value, "Real time contributes nothing.");
            facts = Rider(5f, true);
            facts.ActiveRider = false;
            TestRunner.Equal(MountedRangedMovementOutcome.NotActiveRider, MountedRangedMovementPolicy.Evaluate(facts, out value),
                "The mount, an unmounted actor or another pair's actor is not this rider.");
            TestRunner.Equal(0, value, "A non-rider contributes nothing.");
            facts = Rider(5f);
            facts.QualifyingAttack = false;
            TestRunner.Equal(MountedRangedMovementOutcome.NotQualifyingAttack, MountedRangedMovementPolicy.Evaluate(facts, out value), "Melee is unaffected.");
            facts = Rider(5f);
            facts.CurrentAllocation = false;
            TestRunner.Equal(MountedRangedMovementOutcome.NoCurrentAllocation, MountedRangedMovementPolicy.Evaluate(facts, out value), "A stale allocation is unaffected.");
            TestRunner.Equal(0, value, "No current allocation contributes nothing.");
        }

        private static void WeaponAttacks()
        {
            TestRunner.True(MountedRangedMovementPolicy.QualifyingRangedWeaponAttack(true, true, false, false), "A wielded bow, crossbow, thrown weapon or firearm qualifies.");
            TestRunner.True(!MountedRangedMovementPolicy.QualifyingRangedWeaponAttack(true, false, false, false), "A melee weapon does not.");
            TestRunner.True(!MountedRangedMovementPolicy.QualifyingRangedWeaponAttack(true, true, true, false), "A natural weapon does not.");
            TestRunner.True(!MountedRangedMovementPolicy.QualifyingRangedWeaponAttack(false, true, false, false), "A spell's proxy weapon, never equipped, does not.");
            TestRunner.True(!MountedRangedMovementPolicy.QualifyingRangedWeaponAttack(true, true, false, true), "A ray or kinetic proxy category does not.");
        }

        private static void DeadShotShapes()
        {
            // Review finding 2. Dead Shot triggers its own RuleAttackRolls on the equipped firearm (no RuleAttackWithWeapon
            // parent): the weapon qualifies because it is wielded, not because of the delivering rule.
            TestRunner.True(MountedRangedMovementPolicy.QualifyingRangedWeaponAttack(true, true, false, false), "A Dead Shot probe on the equipped firearm qualifies.");
            // After qualifying travel a Dead Shot probe receives the ordinary firearm attack's contribution.
            var mount = new Mount();
            mount.Travel(4f);
            TestRunner.Equal(-4, Contribution(Rider(mount.Normal)), "Dead Shot without the feat.");
            TestRunner.Equal(-2, Contribution(Rider(mount.Normal, true)), "Dead Shot with the feat.");
            // The probe's own calculation runs inside RuleAttackRoll.OnTrigger; the delivery's one confirmation runs in an
            // OnTrigger prefix of the auto-hit delivery roll, before its body (no AC rule yet). Both are committed.
            TestRunner.True(MountedRangedMovementPolicy.CommittedRollCalculation(true, true, false),
                "The roll's own calculation, or a confirmation made for the still-unresolved roll, is committed.");
            TestRunner.True(!MountedRangedMovementPolicy.CommittedRollCalculation(false, false, false), "A character-sheet or tooltip preview has no enclosing roll.");
            TestRunner.True(!MountedRangedMovementPolicy.CommittedRollCalculation(true, false, false), "A parry or another actor's calculation is not this roll's.");
            TestRunner.True(!MountedRangedMovementPolicy.CommittedRollCalculation(true, true, true), "Nothing after the roll stored its result.");
        }

        private static void AllocationCurrency()
        {
            var rider = new object();
            var horse = new object();
            var activation = new PairedActivation<object, object>(rider, horse);
            TestRunner.True(MountedRangedMovementPolicy.CurrentPairedGrant(activation, rider, horse) == null, "No grant before the activation has a boundary.");
            var boundary = new object();
            TestRunner.True(activation.Begin(boundary) && activation.BeginActorPreparation(rider, boundary) && activation.BeginActorPreparation(horse, boundary), "Ordinary paired preparation.");
            var grant = MountedRangedMovementPolicy.CurrentPairedGrant(activation, rider, horse);
            TestRunner.Equal(activation.Identity, grant, "The pair's activation identity is the authority.");
            TestRunner.True(MountedRangedMovementPolicy.IsAllocationCurrent(true, grant, grant, true, true), "The mount's record of this activation is current.");
            TestRunner.True(MountedRangedMovementPolicy.IsAllocationCurrent(true, grant, grant, true, false), "One paired activation spans a Delay into a later round.");
            TestRunner.True(!MountedRangedMovementPolicy.IsAllocationCurrent(false, grant, grant, true, true), "Another combat controller is never current.");
            TestRunner.True(!MountedRangedMovementPolicy.IsAllocationCurrent(true, activation.EncounterId.ToString("N") + ":0", grant, true, true), "An earlier activation is stale.");
            TestRunner.True(!MountedRangedMovementPolicy.IsAllocationCurrent(true, "native:" + new string('a', 32), grant, true, true),
                "The mount's own turn this round (riderless, or under another rider) is not the pair's.");
            TestRunner.True(!MountedRangedMovementPolicy.IsAllocationCurrent(true, grant, null, true, true), "No current activation, nothing is current.");
            TestRunner.True(MountedRangedMovementPolicy.CurrentPairedGrant(activation, new object(), horse) == null, "Another rider does not own this pair's grant.");
            TestRunner.True(MountedRangedMovementPolicy.CurrentPairedGrant(activation, rider, new object()) == null, "Another mount does not own this pair's grant.");
            TestRunner.True(MountedRangedMovementPolicy.IsAllocationCurrent(true, null, null, false, true), "Legacy turn modes: an ungranted record of this round is current.");
            TestRunner.True(!MountedRangedMovementPolicy.IsAllocationCurrent(true, null, null, false, false), "Legacy turn modes: an earlier round is stale.");
            TestRunner.True(!MountedRangedMovementPolicy.IsAllocationCurrent(true, "native:" + new string('a', 32), null, false, true), "Legacy turn modes never read a granted record.");
        }

        private static void MidEncounterAdoption()
        {
            // Review finding 1. The horse moved riderless on its own turn earlier this round (its own native grant).
            var horseRecord = new Mount();
            horseRecord.Travel(6f);
            var riderlessGrant = "native:" + new string('c', 32);
            var rider = new object();
            var horse = new object();
            var riderTurn = new object();
            // The rider mounts with its Move on its running turn. AdoptRunningEncounter adopts that boundary without
            // preparing the rider again, then prepares the horse alone: Prepare -> BeginActorPreparation(horse) ->
            // BeginGrantedPreparation(partner, activation.Identity), which replaces the horse's record with a fresh one.
            var activation = new PairedActivation<object, object>(rider, horse);
            TestRunner.True(activation.AdoptRunningBoundary(riderTurn, Domain.MidEncounterAdoption.PreparePartnerThisRound), "The running turn is adopted.");
            TestRunner.True(MountedRangedMovementPolicy.CurrentPairedGrant(activation, rider, horse) == null, "Before the horse's preparation nothing is granted to it.");
            TestRunner.True(activation.BeginActorPreparation(horse, riderTurn), "The horse's one native preparation.");
            var paired = MountedRangedMovementPolicy.CurrentPairedGrant(activation, rider, horse);
            TestRunner.True(paired != null, "The adopted pair has a current activation.");
            TestRunner.True(!MountedRangedMovementPolicy.IsAllocationCurrent(true, riderlessGrant, paired, true, true), "The riderless record is not the pair's.");
            var mounted = new Mount();   // the record BeginGrantedPreparation created, stamped with the activation identity
            var mountedGrant = activation.Identity;
            mounted.Travel(3.5f);
            // The rider's own record is absent or still carries its earlier native grant; neither is an input any more.
            foreach (var owns in new[] { false, true })
            {
                var facts = Rider(mounted.Normal, owns);
                facts.CurrentAllocation = MountedRangedMovementPolicy.IsAllocationCurrent(true, mountedGrant, paired, true, true);
                TestRunner.Equal(owns ? -2 : -4, Contribution(facts), "Qualifying mounted travel after a mid-encounter mount is penalized.");
            }
            var shortMount = new Mount();
            shortMount.Travel(2f);
            var shortFacts = Rider(shortMount.Normal);
            shortFacts.CurrentAllocation = MountedRangedMovementPolicy.IsAllocationCurrent(true, mountedGrant, paired, true, true);
            TestRunner.Equal(0, Contribution(shortFacts), "Earlier riderless travel is never added to the mounted travel.");

            // RetainPartnerParticipation: the horse's slot is already behind the rider, so it is not prepared and keeps its
            // own record; its riderless travel stays its own.
            var retained = new PairedActivation<object, object>(rider, horse);
            TestRunner.True(retained.AdoptRunningBoundary(new object(), Domain.MidEncounterAdoption.RetainPartnerParticipation), "Retained adoption.");
            var retainedGrant = MountedRangedMovementPolicy.CurrentPairedGrant(retained, rider, horse);
            TestRunner.True(!MountedRangedMovementPolicy.IsAllocationCurrent(true, riderlessGrant, retainedGrant, true, true), "A retained horse's own turn is not the pair's.");
        }

        private static void Lifetime()
        {
            var traveled = new Mount();
            traveled.Travel(4.5f);
            // SavedMovementValues persists exactly TimeMoved, TimeForced and TimeStepped with the grant and round, and
            // MountedMovementPersistence restores them into the same record: the classification is derived, not saved.
            var restored = new MountedMovementState { TimeMoved = traveled.State.TimeMoved, TimeForced = traveled.State.TimeForced,
                TimeStepped = traveled.State.TimeStepped, MetresStepped = traveled.State.MetresStepped };
            TestRunner.Equal(Contribution(Rider(traveled.Normal)), Contribution(Rider(MountedRangedMovementPolicy.NormalTravelSeconds(
                restored.TimeMoved, restored.TimeForced, restored.TimeStepped))), "A restored allocation keeps its classification.");
            // Dismount suppresses applicability (no active rider) without touching the retained travel.
            var dismounted = Rider(traveled.Normal);
            dismounted.ActiveRider = false;
            TestRunner.Equal(0, Contribution(dismounted), "Dismounted, nothing applies.");
            TestRunner.Equal(-4, Contribution(Rider(traveled.Normal)), "A legal reattachment to the same allocation still reads its travel.");
            // BeginGrantedPreparation replaces the allocation with a new record, whose default state is no travel.
            TestRunner.Equal(0, Contribution(Rider(new MountedMovementState().TimeMoved)), "A fresh record starts at no travel.");
        }

        private static void CoordinatedLease()
        {
            var power = new object();
            var dodge = new object();
            var foreign = new object();
            var mountedCombat = new object();
            var mountedArchery = new object();
            var items = KmcCombatFeatPublication.Items(mountedCombat, mountedArchery);
            TestRunner.True(items.Length == 2 && ReferenceEquals(items[0], mountedCombat) && ReferenceEquals(items[1], mountedArchery), "Mounted Combat then Mounted Archery.");
            var onlyMountedCombat = KmcCombatFeatPublication.Items<object>(mountedCombat, null);
            TestRunner.True(onlyMountedCombat.Length == 1 && ReferenceEquals(onlyMountedCombat[0], mountedCombat), "A Mounted Archery fault leaves Mounted Combat offered alone.");
            var threw = false;
            try { KmcCombatFeatPublication.Items<object>(null, mountedArchery); } catch (ArgumentNullException) { threw = true; }
            TestRunner.True(threw, "Mounted Archery is never offered without Mounted Combat.");
            threw = false;
            try { KmcCombatFeatPublication.Items(mountedCombat, mountedCombat); } catch (ArgumentException) { threw = true; }
            TestRunner.True(threw, "A feature is not appended twice.");

            var source = new[] { power, foreign, dodge };
            var lease = new ExactAppendOnlyArrayLease<object>(source, items);
            var applied = lease.CreateAppendedValue();
            TestRunner.True(applied.Length == 5 && ReferenceEquals(applied[0], power) && ReferenceEquals(applied[1], foreign) &&
                ReferenceEquals(applied[2], dodge) && ReferenceEquals(applied[3], mountedCombat) && ReferenceEquals(applied[4], mountedArchery),
                "Foreign entries and original order are kept; both feats appear once at the end.");
            object[] restored;
            string error;
            TestRunner.True(lease.TryRestore(applied, out restored, out error) && lease.MatchesOriginal(restored), "Disable restores the exact original.");
            TestRunner.True(lease.MatchesAppended(lease.CreateAppendedValue()), "Re-enable reapplies the same exact value.");
            var drifted = new[] { power, foreign, dodge, mountedCombat, mountedArchery, new object() };
            TestRunner.True(!lease.TryRestore(drifted, out restored, out error) && !lease.MatchesOriginal(drifted) && !lease.MatchesAppended(drifted),
                "Foreign drift after publication is refused, never overwritten.");
            threw = false;
            try { new ExactAppendOnlyArrayLease<object>(applied, items); } catch (ArgumentException) { threw = true; }
            TestRunner.True(threw, "Duplicate initialization over an already-published array is refused.");
        }

        private static void IndependentLeasesConflict()
        {
            var original = new[] { new object(), new object() };
            var mountedCombat = new object();
            var mountedArchery = new object();
            var first = new ExactAppendOnlyArrayLease<object>(original, mountedCombat);
            var second = new ExactAppendOnlyArrayLease<object>(original, mountedArchery);
            var afterFirst = first.CreateAppendedValue();
            TestRunner.True(!second.MatchesOriginal(afterFirst), "A second lease captured from the same array refuses the first's append.");
            object[] restored;
            string error;
            TestRunner.True(!second.TryRestore(afterFirst, out restored, out error), "And it could not restore over it either.");
        }

        private sealed class Lists
        {
            internal readonly object Power = new object(), Dodge = new object(), MountedCombat = new object(), MountedArchery = new object();
            internal readonly System.Collections.Generic.Dictionary<string, object[]> Arrays = new System.Collections.Generic.Dictionary<string, object[]>();
            internal KmcCombatFeatPublicationSet<string, object> Set;

            internal Lists(bool archery = true)
            {
                Arrays["ordinary"] = new[] { Power, Dodge };
                Arrays["fighter"] = new[] { Power, Dodge, new object() };
                Arrays["extra"] = new[] { Power, Dodge };
                Set = new KmcCombatFeatPublicationSet<string, object>(new[] { "ordinary", "fighter", "extra" }, new[] { "ordinary", "fighter" },
                    MountedCombat, archery ? MountedArchery : null, key => Arrays[key], (key, value) => Arrays[key] = value);
            }

            internal bool Has(string key, object item) => Array.IndexOf(Arrays[key], item) >= 0;

            // The invariant the review asked for: the rule is on exactly when no list offers a feat whose mechanic is off.
            internal void AssertCoherent(string when)
            {
                var available = Set.MountedArcheryAvailable();
                foreach (var key in Arrays.Keys)
                    if (Has(key, MountedArchery)) TestRunner.True(available, when + ": " + key + " offers Mounted Archery only while its rule is on.");
                if (available) TestRunner.True(Has("ordinary", MountedArchery) && Has("fighter", MountedArchery), when + ": an active rule is obtainable from both required lists.");
            }
        }

        private static void ExtraListDrift()
        {
            var lists = new Lists();
            TestRunner.True(lists.Set.Apply(true, key => key) == null, "Publication succeeds.");
            TestRunner.True(lists.Set.MountedArcheryAvailable(), "Published and available.");
            lists.AssertCoherent("published");
            // Another mod replaces the extra list after publication and omits Mounted Archery.
            var foreign = new[] { lists.Power, lists.Dodge, lists.MountedCombat, new object() };
            lists.Arrays["extra"] = foreign;
            TestRunner.True(lists.Set.MountedArcheryAvailable(), "Both required lists still offer it: the rule stays on.");
            TestRunner.True(ReferenceEquals(lists.Arrays["extra"], foreign), "The foreign list is never rewritten.");
            TestRunner.True(lists.Has("ordinary", lists.MountedArchery) && lists.Has("fighter", lists.MountedArchery), "The required lists keep offering it.");
            lists.AssertCoherent("extra drift");
        }

        private static void RequiredListDrift()
        {
            var lists = new Lists();
            lists.Set.Apply(true, key => key);
            // Another mod replaces the fighter list after publication and omits Mounted Archery.
            var foreign = new[] { lists.Power, lists.Dodge, lists.MountedCombat, new object() };
            lists.Arrays["fighter"] = foreign;
            // The per-frame refresh sees the replaced required array by reference and withdraws at once.
            TestRunner.True(lists.Set.Refresh(false), "The per-frame refresh withdraws on a replaced required list.");
            TestRunner.True(lists.Set.MountedArcheryWithdrawn && lists.Set.WithdrawalReason == "fighter", "Withdrawn, naming the list.");
            TestRunner.True(!lists.Set.MountedArcheryAvailable(), "The rule is off.");
            TestRunner.True(!lists.Has("ordinary", lists.MountedArchery) && !lists.Has("extra", lists.MountedArchery), "No list still offers Mounted Archery.");
            TestRunner.True(lists.Has("ordinary", lists.MountedCombat) && lists.Has("extra", lists.MountedCombat), "Mounted Combat stays offered.");
            TestRunner.True(ReferenceEquals(lists.Arrays["fighter"], foreign), "The foreign list is never rewritten.");
            lists.AssertCoherent("required drift");
            TestRunner.True(!lists.Set.Refresh(true), "Withdrawal happens once.");

            // An in-place edit keeps the array reference; the per-frame check misses it, the attack-time check does not.
            var inPlace = new Lists();
            inPlace.Set.Apply(true, key => key);
            var ordinary = inPlace.Arrays["ordinary"];
            ordinary[Array.IndexOf(ordinary, inPlace.MountedArchery)] = new object();
            TestRunner.True(!inPlace.Set.Refresh(false), "The cheap check sees the same reference.");
            TestRunner.True(!inPlace.Set.MountedArcheryAvailable(), "The full comparison at attack time withdraws.");
            inPlace.AssertCoherent("in-place drift");
        }

        private static void PublicationLifecycle()
        {
            var lists = new Lists();
            var originals = new System.Collections.Generic.Dictionary<string, object[]>();
            foreach (var key in lists.Arrays.Keys) originals[key] = (object[])lists.Arrays[key].Clone();
            TestRunner.True(!lists.Set.MountedArcheryAvailable(), "Nothing is available before publication.");
            lists.Set.Apply(true, key => key);
            TestRunner.True(lists.Set.Apply(false, key => key) == null, "Disable restores every list.");
            foreach (var key in originals.Keys)
                TestRunner.True(lists.Arrays[key].Length == originals[key].Length && !lists.Has(key, lists.MountedCombat) && !lists.Has(key, lists.MountedArchery), "Restored " + key + ".");
            TestRunner.True(!lists.Set.MountedArcheryAvailable() && !lists.Set.MountedArcheryWithdrawn, "Disabled is not a withdrawal.");
            lists.Set.Apply(true, key => key);
            lists.AssertCoherent("re-enabled");
            lists.Arrays["ordinary"] = new[] { lists.Power, lists.Dodge, lists.MountedCombat, new object() };
            TestRunner.True(!lists.Set.MountedArcheryAvailable(), "Withdrawn after required drift.");
            TestRunner.True(lists.Set.Apply(false, key => key) != null, "Disable reports the foreign ordinary list and leaves it.");
            TestRunner.True(lists.Set.Apply(true, key => key) != null && !lists.Has("fighter", lists.MountedArchery) && lists.Has("fighter", lists.MountedCombat),
                "Re-enabling after a withdrawal publishes Mounted Combat only.");
            lists.AssertCoherent("re-enabled after withdrawal");

            var noArchery = new Lists(false);
            TestRunner.True(noArchery.Set.Apply(true, key => key) == null && noArchery.Has("ordinary", noArchery.MountedCombat), "Mounted Combat alone is published.");
            TestRunner.True(!noArchery.Set.MountedArcheryAvailable(), "Without the feat there is no rule.");
            noArchery.AssertCoherent("no Mounted Archery");
        }
    }
}
