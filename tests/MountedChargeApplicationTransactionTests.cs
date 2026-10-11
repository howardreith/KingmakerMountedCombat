using System;
using System.Collections.Generic;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // Chunk 6B increment 6B.2: the charge lease's application must be reversible at every boundary.
    //
    // These fault-inject after each of the five mutations the lease takes, in the order the lease takes them:
    // charge-buff, mount-charging, mount-speed-override, rider-charging-state, forced-path. The three failure
    // shapes the owner named for the mandatory buff are covered explicitly: the ChargeBuff blueprint being
    // unavailable, AddBuff returning null, and AddBuff throwing. In every case the completed prefix must be
    // undone in reverse order, nothing beyond it may be touched, and the original exception must survive.
    internal static class MountedChargeApplicationTransactionTests
    {
        // A recorder standing in for the live native fields the lease mutates.
        private sealed class Fixture
        {
            // The mutations that actually happened.
            internal readonly List<string> Applied = new List<string>();

            // Undos that actually reverted a mutation. An undo invoked for a step whose mutation never
            // happened must appear here as nothing at all, which is what makes the rollback safe to run
            // over every entered step.
            internal readonly List<string> Reverted = new List<string>();

            // Every undo invocation, reverting or not.
            internal readonly List<string> UndoCalls = new List<string>();
            internal string FaultAfter;
            internal string FaultAt;
            internal Exception FaultWith;
            internal string UndoFaultAt;

            internal MountedChargeApplicationStep Step(string name)
            {
                return new MountedChargeApplicationStep(name,
                    () =>
                    {
                        if (FaultAt == name)
                        {
                            throw FaultWith ?? new InvalidOperationException("injected at " + name);
                        }

                        Applied.Add(name);
                        if (FaultAfter == name)
                        {
                            throw FaultWith ?? new InvalidOperationException("injected after " + name);
                        }
                    },
                    () =>
                    {
                        if (UndoFaultAt == name)
                        {
                            throw new InvalidOperationException("undo fault at " + name);
                        }

                        UndoCalls.Add(name);
                        if (Applied.Contains(name))
                        {
                            Reverted.Add(name);
                        }
                    });
            }

            internal MountedChargeApplicationTransaction Build()
            {
                return new MountedChargeApplicationTransaction(new[]
                {
                    Step("charge-buff"),
                    Step("mount-charging"),
                    Step("mount-speed-override"),
                    Step("rider-charging-state"),
                    Step("forced-path")
                });
            }
        }

        private static readonly string[] Order =
        {
            "charge-buff", "mount-charging", "mount-speed-override", "rider-charging-state", "forced-path"
        };

        private static string Join(IEnumerable<string> values)
        {
            return string.Join("|", new List<string>(values).ToArray());
        }

        internal static void Register(TestRunner runner)
        {
            runner.Run("a charge application that completes takes every mutation and undoes none", () =>
            {
                var fixture = new Fixture();
                var transaction = fixture.Build();
                transaction.Apply();
                TestRunner.True(transaction.Applied, "The complete application did not report Applied.");
                TestRunner.True(!transaction.RolledBack, "A complete application reported a rollback.");
                TestRunner.True(Join(fixture.Applied) == Join(Order),
                    "The mutations were not taken in the lease's order: " + Join(fixture.Applied));
                TestRunner.True(fixture.UndoCalls.Count == 0,
                    "A complete application undid something: " + Join(fixture.UndoCalls));
                TestRunner.True(transaction.FailedStep == null && transaction.FailureReason == null,
                    "A complete application recorded a failure.");
            });

            // One case per mutation boundary: the fault lands immediately after that mutation completed, so
            // the completed prefix is exactly that step and everything before it.
            for (var index = 0; index < Order.Length; index++)
            {
                var boundary = Order[index];
                var expectedApplied = new List<string>();
                for (var before = 0; before <= index; before++)
                {
                    expectedApplied.Add(Order[before]);
                }

                var expectedUndone = new List<string>(expectedApplied);
                expectedUndone.Reverse();

                runner.Run("a charge application faulted after " + boundary + " undoes exactly that prefix in reverse", () =>
                {
                    var fixture = new Fixture { FaultAfter = boundary };
                    var transaction = fixture.Build();
                    var threw = false;
                    try
                    {
                        transaction.Apply();
                    }
                    catch (InvalidOperationException exception)
                    {
                        threw = true;
                        TestRunner.True(exception.Message == "injected after " + boundary,
                            "The original exception did not survive the rollback: " + exception.Message);
                    }

                    TestRunner.True(threw, "A faulted application did not rethrow.");
                    TestRunner.True(!transaction.Applied, "A faulted application reported Applied.");
                    TestRunner.True(transaction.RolledBack, "A faulted application did not report a rollback.");
                    TestRunner.True(transaction.FailedStep == boundary,
                        "The failed step was recorded as " + transaction.FailedStep + " rather than " + boundary + ".");
                    TestRunner.True(Join(fixture.Applied) == Join(expectedApplied),
                        "The applied prefix was " + Join(fixture.Applied) + " rather than " + Join(expectedApplied) + ".");
                    TestRunner.True(Join(fixture.Reverted) == Join(expectedUndone),
                        "The revert order was " + Join(fixture.Reverted) + " rather than " + Join(expectedUndone) + ".");
                    TestRunner.True(Join(fixture.UndoCalls) == Join(expectedUndone),
                        "The undo walk was " + Join(fixture.UndoCalls) + " rather than " + Join(expectedUndone) + ".");
                    TestRunner.True(Join(transaction.AttemptedSteps) == Join(expectedApplied),
                        "The entered steps were " + Join(transaction.AttemptedSteps) + ".");
                    TestRunner.True(transaction.UndoFailures.Count == 0,
                        "A clean rollback reported undo failures: " + Join(transaction.UndoFailures));
                });
            }

            runner.Run("the mandatory charge buff failing leaves nothing to undo", () =>
            {
                // All three shapes the owner named resolve to the first step throwing: an unavailable
                // blueprint, AddBuff returning null, and AddBuff throwing. None may mutate anything.
                foreach (var shape in new[]
                {
                    new { Name = "the blueprint is unavailable", Error = (Exception)new InvalidOperationException("The stock charge buff blueprint is unavailable.") },
                    new { Name = "AddBuff returned null", Error = (Exception)new InvalidOperationException("The stock charge buff could not be installed on the rider.") },
                    new { Name = "AddBuff threw", Error = (Exception)new NullReferenceException("native AddBuff threw") }
                })
                {
                    var fixture = new Fixture { FaultAt = "charge-buff", FaultWith = shape.Error };
                    var transaction = fixture.Build();
                    var threw = false;
                    try
                    {
                        transaction.Apply();
                    }
                    catch (Exception exception)
                    {
                        threw = true;
                        TestRunner.True(ReferenceEquals(exception, shape.Error),
                            "The exact native exception did not survive when " + shape.Name + ".");
                    }

                    TestRunner.True(threw, "No exception when " + shape.Name + ".");
                    TestRunner.True(!transaction.Applied, "Applied was reported when " + shape.Name + ".");
                    TestRunner.True(transaction.RolledBack, "No rollback was reported when " + shape.Name + ".");
                    TestRunner.True(fixture.Applied.Count == 0,
                        "A mutation was taken when " + shape.Name + ": " + Join(fixture.Applied));
                    TestRunner.True(fixture.Reverted.Count == 0,
                        "Something was reverted when nothing had been applied: " + Join(fixture.Reverted));
                    TestRunner.True(Join(fixture.UndoCalls) == "charge-buff",
                        "The rollback walk was " + Join(fixture.UndoCalls) + " rather than the entered step alone.");
                    TestRunner.True(transaction.FailedStep == "charge-buff",
                        "The failed step was not the charge buff when " + shape.Name + ".");
                }
            });

            runner.Run("an undo that itself fails is recorded and does not strand the other mutations", () =>
            {
                // The forced path is applied last, so a fault after it undoes all five. If the rider-state
                // undo throws, the remaining undos must still run: a second fault must not strand the first
                // mutation, which is the whole point of the reverse walk.
                var fixture = new Fixture { FaultAfter = "forced-path", UndoFaultAt = "rider-charging-state" };
                var transaction = fixture.Build();
                try
                {
                    transaction.Apply();
                }
                catch (InvalidOperationException)
                {
                }

                TestRunner.True(transaction.RolledBack, "The rollback was not reported.");
                TestRunner.True(Join(fixture.Reverted) == "forced-path|mount-speed-override|mount-charging|charge-buff",
                    "The surviving reverts were " + Join(fixture.Reverted) + ".");
                TestRunner.True(transaction.UndoFailures.Count == 1 &&
                    transaction.UndoFailures[0] == "rider-charging-state:InvalidOperationException",
                    "The undo failure was not recorded exactly: " + Join(transaction.UndoFailures));
            });

            runner.Run("a charge application transaction runs exactly once", () =>
            {
                var fixture = new Fixture();
                var transaction = fixture.Build();
                transaction.Apply();
                var threw = false;
                try
                {
                    transaction.Apply();
                }
                catch (InvalidOperationException)
                {
                    threw = true;
                }

                TestRunner.True(threw, "A second application was accepted.");
                TestRunner.True(Join(fixture.Applied) == Join(Order),
                    "The second application mutated something: " + Join(fixture.Applied));
            });

            runner.Run("a charge application needs at least one step and no null step", () =>
            {
                var empty = false;
                try
                {
                    new MountedChargeApplicationTransaction(new MountedChargeApplicationStep[0]);
                }
                catch (ArgumentException)
                {
                    empty = true;
                }

                TestRunner.True(empty, "An empty charge application was accepted.");

                var nulled = false;
                try
                {
                    new MountedChargeApplicationTransaction(new MountedChargeApplicationStep[] { null });
                }
                catch (ArgumentException)
                {
                    nulled = true;
                }

                TestRunner.True(nulled, "A null charge application step was accepted.");

                var unnamed = false;
                try
                {
                    new MountedChargeApplicationStep(string.Empty, () => { }, () => { });
                }
                catch (ArgumentException)
                {
                    unnamed = true;
                }

                TestRunner.True(unnamed, "An unnamed charge application step was accepted.");
            });
        }
    }
}
