using System;
using System.Collections.Generic;
using KingmakerMountedCombat.Domain;


namespace KingmakerMountedCombat.Tests
{
    // Chunk 6B increment 6B.2: the order of a charge transaction, which is load-bearing rather than cosmetic.
    //
    // The transition revalidation asks whether the carrier still owns the mount's Move slot. Releasing the
    // carrier first therefore makes a lawful charge reject itself - that was a real defect in the first form
    // of this work, and the first test here is the one that fails if it comes back. The attack-start
    // revalidation asks the opposite question, so it must run after the release is proven. And the lease may
    // not be applied before exact carrier ownership exists, because its forced path lives only as long as
    // that carrier.
    internal static class MountedChargeTransactionSequenceTests
    {
        private static string Join(IEnumerable<string> values)
        {
            return string.Join("|", new List<string>(values).ToArray());
        }

        private static string Join(IEnumerable<MountedChargeTransactionStep> steps)
        {
            var names = new List<string>();
            foreach (var step in steps)
            {
                names.Add(step.ToString());
            }

            return string.Join("|", names.ToArray());
        }

        // The lease's five native mutations, in the exact order MountedChargeLease applies them, undone in
        // reverse. The complete positive test below needs two things from this: that nothing is applied
        // before exact carrier ownership exists, and that a lawful charge ends with every one of them
        // observably gone rather than merely "the undo loop ran".
        private sealed class ChargeLeaseMutations
        {
            private static readonly string[] Order =
                { "charge-buff", "mount-charging", "mount-speed-override", "rider-charging-state", "forced-path" };

            private readonly Dictionary<string, bool> applied = new Dictionary<string, bool>();

            internal ChargeLeaseMutations()
            {
                foreach (var name in Order)
                {
                    applied[name] = false;
                }
            }

            internal bool AnyApplied
            {
                get
                {
                    foreach (var name in Order)
                    {
                        if (applied[name])
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }

            internal bool AllApplied
            {
                get
                {
                    foreach (var name in Order)
                    {
                        if (!applied[name])
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }

            internal MountedChargeApplicationTransaction Transaction()
            {
                var steps = new List<MountedChargeApplicationStep>();
                foreach (var name in Order)
                {
                    var captured = name;
                    steps.Add(new MountedChargeApplicationStep(captured,
                        () => applied[captured] = true,
                        () => applied[captured] = false));
                }

                return new MountedChargeApplicationTransaction(steps);
            }

            internal MountedChargeCleanupLedger Cleanup()
            {
                var steps = new List<MountedChargeCleanupStep>();
                for (var index = Order.Length - 1; index >= 0; index--)
                {
                    var captured = Order[index];
                    steps.Add(new MountedChargeCleanupStep(captured,
                        () => applied[captured],
                        () =>
                        {
                            applied[captured] = false;
                            return true;
                        }));
                }

                return new MountedChargeCleanupLedger(steps);
            }

            internal string Describe()
            {
                var parts = new List<string>();
                foreach (var name in Order)
                {
                    parts.Add(name + "=" + applied[name]);
                }

                return string.Join(";", parts.ToArray());
            }
        }

        // A charge whose conditions all still hold, for the phase given. The carrier-ownership input is the
        // live sequence state, so the test cannot answer the revalidation's own question for it.
        private static MountedChargeRevalidationRequest Request(MountedChargeRevalidationPhase phase, bool carrierOwned)
        {
            return new MountedChargeRevalidationRequest
            {
                Phase = phase,
                TargetValid = true,
                TargetVisible = true,
                TargetHostile = true,
                TargetAttackable = true,
                StraightRoute = true,
                LandingBlocked = false,
                MountAvoidanceDisabled = false,
                Distance = 6f,
                MaximumRange = 30.48f,
                RelationshipMounted = true,
                ExactPair = true,
                LifecycleBoundary = false,
                RiderOwnsAttackSlot = true,
                CarrierOwnsMountMoveSlot = carrierOwned,
                MountQueueEmpty = true,
                FinalAttackAdmitted = true
            };
        }

        // The complete, lawful real-time charge, in the exact order the command takes it.
        private static readonly MountedChargeTransactionStep[] PositiveOrder =
        {
            MountedChargeTransactionStep.CarrierAdmitted,
            MountedChargeTransactionStep.CarrierOwnershipProven,
            MountedChargeTransactionStep.InitialRevalidation,
            MountedChargeTransactionStep.LeaseApplied,
            MountedChargeTransactionStep.TransitionRevalidation,
            MountedChargeTransactionStep.CarrierReleasedForAttack,
            MountedChargeTransactionStep.CarrierReleaseProven,
            MountedChargeTransactionStep.Arrived,
            MountedChargeTransactionStep.AttackStartRevalidation,
            MountedChargeTransactionStep.AttackStarted
        };

        internal static void Register(TestRunner runner)
        {
            runner.Run("the complete positive real-time charge order is lawful end to end", () =>
            {
                var sequence = new MountedChargeTransactionSequence();
                foreach (var step in PositiveOrder)
                {
                    TestRunner.True(sequence.Observe(step),
                        "The lawful positive order was rejected at " + step + ": " + sequence.Describe());
                }

                TestRunner.True(sequence.Lawful, "The lawful positive order reported a violation: " + sequence.Describe());
                TestRunner.True(Join(sequence.Observed) == Join(PositiveOrder),
                    "The observed order differs: " + Join(sequence.Observed));
                TestRunner.True(!sequence.CarrierOwned, "The carrier was still owned after the attack started.");
            });

            runner.Run("releasing the carrier before the transition revalidation is a violation", () =>
            {
                // The exact defect this type exists to prevent: StopDelegatedMove before the transition
                // revalidation, which left the revalidation asking about a carrier that was already gone.
                var sequence = new MountedChargeTransactionSequence();
                foreach (var step in new[]
                {
                    MountedChargeTransactionStep.CarrierAdmitted,
                    MountedChargeTransactionStep.CarrierOwnershipProven,
                    MountedChargeTransactionStep.InitialRevalidation,
                    MountedChargeTransactionStep.LeaseApplied
                })
                {
                    TestRunner.True(sequence.Observe(step), "The lawful prefix was rejected at " + step + ".");
                }

                TestRunner.True(!sequence.Observe(MountedChargeTransactionStep.CarrierReleasedForAttack),
                    "The carrier was allowed to be released before the transition revalidation.");
                TestRunner.True(!sequence.Lawful, "An early carrier release was not recorded as a violation.");
                TestRunner.True(sequence.Violations.Count == 1 &&
                    sequence.Violations[0].Contains("released before the transition revalidation"),
                    "The violation was not the expected one: " + sequence.Describe());
            });

            runner.Run("the transition revalidation after a release is a violation", () =>
            {
                // The same defect seen from the other side: if the release did happen first, the
                // revalidation itself is the illegal step.
                var sequence = new MountedChargeTransactionSequence();
                sequence.Observe(MountedChargeTransactionStep.CarrierAdmitted);
                sequence.Observe(MountedChargeTransactionStep.CarrierOwnershipProven);
                sequence.Observe(MountedChargeTransactionStep.InitialRevalidation);
                sequence.Observe(MountedChargeTransactionStep.LeaseApplied);
                sequence.ObserveCarrierReleasedForRepath();
                TestRunner.True(!sequence.Observe(MountedChargeTransactionStep.TransitionRevalidation),
                    "The transition revalidation was allowed without a carrier.");
                TestRunner.True(sequence.Violations[0].Contains("after the carrier was released"),
                    "The violation was not the expected one: " + sequence.Describe());
            });

            runner.Run("the lease may not be applied before exact carrier ownership", () =>
            {
                var noCarrier = new MountedChargeTransactionSequence();
                TestRunner.True(!noCarrier.Observe(MountedChargeTransactionStep.InitialRevalidation),
                    "The initial revalidation was allowed without a proven carrier.");

                var admittedOnly = new MountedChargeTransactionSequence();
                admittedOnly.Observe(MountedChargeTransactionStep.CarrierAdmitted);
                TestRunner.True(!admittedOnly.Observe(MountedChargeTransactionStep.LeaseApplied),
                    "The lease was applied on a carrier whose ownership was never proven.");

                var unrevalidated = new MountedChargeTransactionSequence();
                unrevalidated.Observe(MountedChargeTransactionStep.CarrierAdmitted);
                unrevalidated.Observe(MountedChargeTransactionStep.CarrierOwnershipProven);
                TestRunner.True(!unrevalidated.Observe(MountedChargeTransactionStep.LeaseApplied),
                    "The lease was applied without an immediately preceding revalidation.");
            });

            runner.Run("carrier ownership must be proven immediately after admission", () =>
            {
                var sequence = new MountedChargeTransactionSequence();
                TestRunner.True(!sequence.Observe(MountedChargeTransactionStep.CarrierOwnershipProven),
                    "Ownership was proven with no admission at all.");
            });

            // Item 2: a failed revalidation must never be followed in the same tick by another carrier, a
            // second repath, a lease application or an attack transition. Each is an ordering violation.
            runner.Run("a second carrier may not be admitted while one is owned", () =>
            {
                var sequence = new MountedChargeTransactionSequence();
                sequence.Observe(MountedChargeTransactionStep.CarrierAdmitted);
                sequence.Observe(MountedChargeTransactionStep.CarrierOwnershipProven);
                TestRunner.True(!sequence.Observe(MountedChargeTransactionStep.CarrierAdmitted),
                    "A second carrier was admitted while the first was still owned.");
                TestRunner.True(sequence.Violations[0].Contains("while one was still owned"),
                    "The violation was not the expected one: " + sequence.Describe());
            });

            runner.Run("nothing may follow the attack release except the attack itself", () =>
            {
                MountedChargeTransactionSequence Released()
                {
                    var sequence = new MountedChargeTransactionSequence();
                    foreach (var step in new[]
                    {
                        MountedChargeTransactionStep.CarrierAdmitted,
                        MountedChargeTransactionStep.CarrierOwnershipProven,
                        MountedChargeTransactionStep.InitialRevalidation,
                        MountedChargeTransactionStep.LeaseApplied,
                        MountedChargeTransactionStep.TransitionRevalidation,
                        MountedChargeTransactionStep.CarrierReleasedForAttack
                    })
                    {
                        sequence.Observe(step);
                    }

                    return sequence;
                }

                TestRunner.True(!Released().Observe(MountedChargeTransactionStep.CarrierAdmitted),
                    "A new carrier was admitted after the attack release.");
                TestRunner.True(!Released().Observe(MountedChargeTransactionStep.RepathRevalidation),
                    "A repath revalidation ran after the attack release.");
            });

            runner.Run("the lease may be applied only once", () =>
            {
                var sequence = new MountedChargeTransactionSequence();
                sequence.Observe(MountedChargeTransactionStep.CarrierAdmitted);
                sequence.Observe(MountedChargeTransactionStep.CarrierOwnershipProven);
                sequence.Observe(MountedChargeTransactionStep.InitialRevalidation);
                sequence.Observe(MountedChargeTransactionStep.LeaseApplied);
                sequence.ObserveCarrierReleasedForRepath();
                sequence.Observe(MountedChargeTransactionStep.CarrierAdmitted);
                sequence.Observe(MountedChargeTransactionStep.CarrierOwnershipProven);
                // The guard is layered, and both layers matter. A second initial revalidation is itself
                // illegal, because the initial revalidation exists only to precede the single lease
                // application - a repath re-forces the existing lease, it does not re-apply one.
                TestRunner.True(!sequence.Observe(MountedChargeTransactionStep.InitialRevalidation),
                    "A second initial revalidation was allowed after the lease was applied.");
                TestRunner.True(!sequence.Observe(MountedChargeTransactionStep.LeaseApplied),
                    "The lease was applied a second time.");
                TestRunner.True(sequence.Violations.Count == 2,
                    "The violations were not the expected pair: " + sequence.Describe());
                TestRunner.True(sequence.Violations[0].Contains("initial revalidation ran after the lease was applied"),
                    "The first violation was not the expected one: " + sequence.Describe());
                TestRunner.True(sequence.Violations[1].Contains("applied twice"),
                    "The second violation was not the expected one: " + sequence.Describe());
            });

            runner.Run("the attack needs a proven release, arrival and a final revalidation", () =>
            {
                var noRelease = new MountedChargeTransactionSequence();
                noRelease.Observe(MountedChargeTransactionStep.CarrierAdmitted);
                noRelease.Observe(MountedChargeTransactionStep.CarrierOwnershipProven);
                TestRunner.True(!noRelease.Observe(MountedChargeTransactionStep.Arrived),
                    "Arrival was recorded before the carrier release was proven.");

                var owned = new MountedChargeTransactionSequence();
                owned.Observe(MountedChargeTransactionStep.CarrierAdmitted);
                owned.Observe(MountedChargeTransactionStep.CarrierOwnershipProven);
                TestRunner.True(!owned.Observe(MountedChargeTransactionStep.AttackStartRevalidation),
                    "The final revalidation ran while the carrier was still owned.");

                var unchecked_ = new MountedChargeTransactionSequence();
                foreach (var step in new[]
                {
                    MountedChargeTransactionStep.CarrierAdmitted,
                    MountedChargeTransactionStep.CarrierOwnershipProven,
                    MountedChargeTransactionStep.InitialRevalidation,
                    MountedChargeTransactionStep.LeaseApplied,
                    MountedChargeTransactionStep.TransitionRevalidation,
                    MountedChargeTransactionStep.CarrierReleasedForAttack,
                    MountedChargeTransactionStep.CarrierReleaseProven,
                    MountedChargeTransactionStep.Arrived
                })
                {
                    unchecked_.Observe(step);
                }

                TestRunner.True(!unchecked_.Observe(MountedChargeTransactionStep.AttackStarted),
                    "The native attack started without a final revalidation.");
            });

            runner.Run("a repath cycle may legally re-admit a carrier and re-force the path", () =>
            {
                var sequence = new MountedChargeTransactionSequence();
                foreach (var step in new[]
                {
                    MountedChargeTransactionStep.CarrierAdmitted,
                    MountedChargeTransactionStep.CarrierOwnershipProven,
                    MountedChargeTransactionStep.InitialRevalidation,
                    MountedChargeTransactionStep.LeaseApplied
                })
                {
                    sequence.Observe(step);
                }

                // A repath: the carrier is released for a repath, not for the attack.
                TestRunner.True(sequence.Observe(MountedChargeTransactionStep.RepathRevalidation),
                    "A repath revalidation was rejected mid-approach.");
                sequence.ObserveCarrierReleasedForRepath();
                TestRunner.True(sequence.Observe(MountedChargeTransactionStep.CarrierAdmitted),
                    "A repath could not admit a new carrier.");
                TestRunner.True(sequence.Observe(MountedChargeTransactionStep.CarrierOwnershipProven),
                    "A repath carrier could not prove ownership.");
                TestRunner.True(sequence.Observe(MountedChargeTransactionStep.RepathRevalidation),
                    "A re-force revalidation was rejected.");
                TestRunner.True(sequence.Observe(MountedChargeTransactionStep.TransitionRevalidation),
                    "The transition revalidation was rejected after a lawful repath cycle.");
                TestRunner.True(sequence.Lawful, "A lawful repath cycle reported a violation: " + sequence.Describe());
            });

            // Item 7: the one complete positive real-time charge, driving the four charge policies in the
            // exact order MountedPairAttackCommand drives them - admission, proven carrier ownership, the
            // initial revalidation, the lease, the approach, the transition revalidation WHILE the carrier
            // is owned, the release, a proven release, arrival, the final revalidation, exactly one native
            // attack, and exact cleanup.
            runner.Run("one complete positive real-time charge drives order, lease, revalidation and cleanup", () =>
            {
                var sequence = new MountedChargeTransactionSequence();
                var lease = new ChargeLeaseMutations();
                var transaction = lease.Transaction();

                void Revalidate(MountedChargeRevalidationPhase phase, string what)
                {
                    var outcome = MountedChargeRevalidation.Evaluate(Request(phase, sequence.CarrierOwned));
                    TestRunner.True(outcome.IsValid, what + " invalidated a lawful charge: " + outcome.Reason);
                }

                void Step(MountedChargeTransactionStep step)
                {
                    TestRunner.True(sequence.Observe(step),
                        "The lawful charge was rejected at " + step + ": " + sequence.Describe());
                }

                // The native UnitMoveTo is created and run on the mount, then proven to be the exact
                // carrier: the exact mount's, in the exact Move slot, unqueued, with the rider wrapper
                // still its parent.
                Step(MountedChargeTransactionStep.CarrierAdmitted);
                Step(MountedChargeTransactionStep.CarrierOwnershipProven);
                TestRunner.True(!lease.AnyApplied,
                    "A lease mutation existed before exact carrier ownership: " + lease.Describe());

                // The initial in-transaction revalidation runs before any mutation exists, so a charge that
                // has already become unlawful never acquires one.
                Revalidate(MountedChargeRevalidationPhase.BeforeRepath, "the initial revalidation");
                Step(MountedChargeTransactionStep.InitialRevalidation);
                TestRunner.True(!lease.AnyApplied,
                    "A lease mutation existed before the lease was applied: " + lease.Describe());

                // Only now the lease, as one ordered transaction.
                transaction.Apply();
                TestRunner.True(transaction.Applied && lease.AllApplied,
                    "The lease did not apply completely: " + transaction.Describe() + ";" + lease.Describe());
                Step(MountedChargeTransactionStep.LeaseApplied);

                // The approach re-asserts the forced path behind its own revalidation each tick.
                Revalidate(MountedChargeRevalidationPhase.BeforeRepath, "the approach revalidation");
                Step(MountedChargeTransactionStep.RepathRevalidation);

                // The approach-to-attack boundary, asked while the carrier still owns the Move slot. This
                // is the ordering the first form of this work got wrong.
                TestRunner.True(sequence.CarrierOwned,
                    "The carrier was already released at the transition boundary: " + sequence.Describe());
                Revalidate(MountedChargeRevalidationPhase.BeforeAttackTransition, "the transition revalidation");
                Step(MountedChargeTransactionStep.TransitionRevalidation);

                // Then, and only then, the release - and the release is proven before arrival.
                Step(MountedChargeTransactionStep.CarrierReleasedForAttack);
                TestRunner.True(!sequence.CarrierOwned, "The carrier was still owned after its release.");
                Step(MountedChargeTransactionStep.CarrierReleaseProven);
                Step(MountedChargeTransactionStep.Arrived);

                // The final revalidation asks the opposite question of the transition one, and passes only
                // because the carrier is gone. Then exactly one native rider attack.
                Revalidate(MountedChargeRevalidationPhase.BeforeAttackStart, "the final revalidation");
                Step(MountedChargeTransactionStep.AttackStartRevalidation);
                Step(MountedChargeTransactionStep.AttackStarted);
                TestRunner.True(sequence.Lawful,
                    "The complete positive charge reported a violation: " + sequence.Describe());
                TestRunner.True(!transaction.RolledBack && transaction.UndoFailures.Count == 0,
                    "A lawful charge rolled its own lease back: " + transaction.Describe());

                // Exact cleanup: every mutation undone in reverse, nothing left owned, no debt.
                var cleanup = lease.Cleanup();
                cleanup.Attempt();
                TestRunner.True(cleanup.Complete,
                    "The lawful charge did not clean up exactly: " + cleanup.Describe());
                TestRunner.True(Join(cleanup.Resolved) ==
                    "forced-path|rider-charging-state|mount-speed-override|mount-charging|charge-buff",
                    "The cleanup order was not the reverse of the apply order: " + Join(cleanup.Resolved));
                TestRunner.True(!lease.AnyApplied,
                    "A lease mutation survived the charge: " + lease.Describe());
            });
        }
    }
}
