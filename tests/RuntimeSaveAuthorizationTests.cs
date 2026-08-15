using System;
using System.IO;
using KingmakerMountedCombat.Diagnostics;

namespace KingmakerMountedCombat.Tests
{
    internal static class RuntimeSaveAuthorizationTests
    {
        private const string SaveRoot = @"C:\KmcAuthorizationTests\Saved Games";
        private const string WorkingFileName = "Manual_299_KMC_AUTOMATION_WORKING.zks";
        private const string BaselineFileName = "Manual_298_KMC_AUTOMATION_BASELINE.zks";
        private const string GameId = "07fa1e4d-8618-41b3-9b8d-faa17d3b26f7";
        private const string GameName = "KMC Fixture";
        private const string Area = "0123456789abcdef0123456789abcdef";
        private const string Sha = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

        public static void Register(TestRunner runner)
        {
            runner.Run("save authorization is inert outside automation", InactiveAuthorizationPassesThrough);
            runner.Run("save authorization permits exact Working load", ExactWorkingLoadIsAllowed);
            runner.Run("save authorization requires express Working write permission", WorkingWriteRequiresPermission);
            runner.Run("save authorization permits expressly authorized Working write", AuthorizedWorkingWriteIsAllowed);
            runner.Run("save authorization rejects Baseline and foreign targets", BaselineAndForeignTargetsAreRejected);
            runner.Run("save authorization rejects auto quick and new targets", AutoQuickAndNewTargetsAreRejected);
            runner.Run("save authorization requires exact campaign identity", CampaignIdentityMustBeExact);
            runner.Run("save authorization requires direct SaveManager child path", TargetMustBeDirectSaveRootChild);
            runner.Run("save authorization reports fatal telemetry", RejectionReportsFatalTelemetry);
            runner.Run("save authorization counters reconcile disjoint outcomes", CountersReconcileDisjointOutcomes);
            runner.Run("save authorization activation is exclusive and leased", ActivationIsExclusiveAndLeased);
            runner.Run("save authorization suppresses one exact Working probe", ExactWorkingWriteProbeIsSuppressedOnce);
            runner.Run("save authorization probe rejects wrong target fatally", SuppressionProbeDoesNotBroadenAuthority);
            runner.Run("save authorization probe lease cancels unconsumed arm", SuppressionLeaseCancelsUnconsumedArm);
        }

        private static void InactiveAuthorizationPassesThrough()
        {
            var authorization = new RuntimeSaveAuthorization();
            var decision = authorization.Authorize(RuntimeSaveOperation.Write, null, null);
            TestRunner.True(decision.Allowed, "Inactive authorization blocked ordinary game save behavior.");
            TestRunner.Equal(false, decision.FatalViolation, "Inactive authorization reported a fatal violation.");
        }

        private static void ExactWorkingLoadIsAllowed()
        {
            var authorization = new RuntimeSaveAuthorization();
            using (authorization.Activate(ValidFixture(), SaveRoot, false))
            {
                var decision = authorization.Authorize(RuntimeSaveOperation.Load, WorkingTarget(), SaveRoot);
                TestRunner.True(decision.Allowed, "Exact Working load was rejected.");
                TestRunner.Equal(0, authorization.FatalViolationCount, "Allowed load produced fatal telemetry.");
            }
        }

        private static void WorkingWriteRequiresPermission()
        {
            var authorization = new RuntimeSaveAuthorization();
            using (authorization.Activate(ValidFixture(), SaveRoot, false))
            {
                var decision = authorization.Authorize(RuntimeSaveOperation.Write, WorkingTarget(), SaveRoot);
                TestRunner.Equal(false, decision.Allowed, "Working write was allowed without express permission.");
                TestRunner.True(decision.FatalViolation, "Unauthorized Working write was not fatal.");
            }
        }

        private static void AuthorizedWorkingWriteIsAllowed()
        {
            var authorization = new RuntimeSaveAuthorization();
            var fixture = ValidFixture();
            using (authorization.Activate(fixture, SaveRoot, true))
            {
                fixture.Working.GameName = "mutated request object";
                var decision = authorization.Authorize(RuntimeSaveOperation.Write, WorkingTarget(), SaveRoot);
                TestRunner.True(decision.Allowed, "Expressly authorized Working write was rejected.");
            }
        }

        private static void BaselineAndForeignTargetsAreRejected()
        {
            var authorization = new RuntimeSaveAuthorization();
            using (authorization.Activate(ValidFixture(), SaveRoot, true))
            {
                var baseline = WorkingTarget();
                baseline.InternalName = RuntimeRequest.BaselineSaveName;
                baseline.FileName = BaselineFileName;
                baseline.FullPath = Path.Combine(SaveRoot, BaselineFileName);
                AssertFatalRejection(authorization.Authorize(RuntimeSaveOperation.Load, baseline, SaveRoot), "Baseline load");

                var foreign = WorkingTarget();
                foreign.InternalName = "PERSONAL_CAMPAIGN";
                foreign.FileName = "Manual_17_PERSONAL_CAMPAIGN.zks";
                foreign.FullPath = Path.Combine(SaveRoot, foreign.FileName);
                AssertFatalRejection(authorization.Authorize(RuntimeSaveOperation.Write, foreign, SaveRoot), "foreign write");
            }
        }

        private static void AutoQuickAndNewTargetsAreRejected()
        {
            var authorization = new RuntimeSaveAuthorization();
            using (authorization.Activate(ValidFixture(), SaveRoot, true))
            {
                var automatic = WorkingTarget();
                automatic.SaveType = "Auto";
                AssertFatalRejection(authorization.Authorize(RuntimeSaveOperation.Write, automatic, SaveRoot), "autosave");

                var quick = WorkingTarget();
                quick.SaveType = "Quick";
                AssertFatalRejection(authorization.Authorize(RuntimeSaveOperation.Write, quick, SaveRoot), "quicksave");

                var unsaved = WorkingTarget();
                unsaved.FileName = null;
                unsaved.FullPath = null;
                AssertFatalRejection(authorization.Authorize(RuntimeSaveOperation.Write, unsaved, SaveRoot), "new save");
            }
        }

        private static void CampaignIdentityMustBeExact()
        {
            var authorization = new RuntimeSaveAuthorization();
            using (authorization.Activate(ValidFixture(), SaveRoot, true))
            {
                var wrongGameId = WorkingTarget();
                wrongGameId.GameId = GameId.ToUpperInvariant();
                AssertFatalRejection(authorization.Authorize(RuntimeSaveOperation.Load, wrongGameId, SaveRoot), "GameId mismatch");

                var wrongGameName = WorkingTarget();
                wrongGameName.GameName = GameName.ToLowerInvariant();
                AssertFatalRejection(authorization.Authorize(RuntimeSaveOperation.Load, wrongGameName, SaveRoot), "GameName mismatch");

                var wrongArea = WorkingTarget();
                wrongArea.Area = Area.ToUpperInvariant();
                AssertFatalRejection(authorization.Authorize(RuntimeSaveOperation.Load, wrongArea, SaveRoot), "Area mismatch");
            }
        }

        private static void TargetMustBeDirectSaveRootChild()
        {
            var authorization = new RuntimeSaveAuthorization();
            using (authorization.Activate(ValidFixture(), SaveRoot, true))
            {
                var nested = WorkingTarget();
                nested.FullPath = Path.Combine(SaveRoot, "nested", WorkingFileName);
                AssertFatalRejection(authorization.Authorize(RuntimeSaveOperation.Load, nested, SaveRoot), "nested target");

                AssertFatalRejection(
                    authorization.Authorize(RuntimeSaveOperation.Load, WorkingTarget(), @"C:\KmcAuthorizationTests\Different Saved Games"),
                    "SaveManager root mismatch");
            }
        }

        private static void RejectionReportsFatalTelemetry()
        {
            var authorization = new RuntimeSaveAuthorization();
            var callbackCount = 0;
            string observedReason = null;
            authorization.FatalViolation += reason =>
            {
                callbackCount++;
                observedReason = reason;
            };

            using (authorization.Activate(ValidFixture(), SaveRoot, true))
            {
                var decision = authorization.Authorize(RuntimeSaveOperation.Write, null, SaveRoot);
                TestRunner.True(decision.FatalViolation, "Rejected null SaveInfo was not fatal.");
                TestRunner.Equal(1, callbackCount, "Fatal telemetry callback count differs.");
                TestRunner.Equal(1, authorization.FatalViolationCount, "Fatal telemetry count differs.");
                TestRunner.Equal(decision.Reason, observedReason, "Fatal telemetry callback lost the rejection reason.");
                TestRunner.Equal(decision.Reason, authorization.LastFatalViolation, "Last fatal violation was not retained.");
            }
        }

        private static void ActivationIsExclusiveAndLeased()
        {
            var authorization = new RuntimeSaveAuthorization();
            var lease = authorization.Activate(ValidFixture(), SaveRoot, true);
            TestRunner.True(authorization.IsActive, "Authorization did not become active.");

            var doubleActivationRejected = false;
            try
            {
                authorization.Activate(ValidFixture(), SaveRoot, true);
            }
            catch (InvalidOperationException)
            {
                doubleActivationRejected = true;
            }

            TestRunner.True(doubleActivationRejected, "Second active authorization scope was accepted.");
            lease.Dispose();
            lease.Dispose();
            TestRunner.Equal(false, authorization.IsActive, "Authorization lease did not deactivate idempotently.");
            TestRunner.True(
                authorization.Authorize(RuntimeSaveOperation.Write, null, null).Allowed,
                "Deactivated authorization did not restore ordinary pass-through behavior.");
        }

        private static void CountersReconcileDisjointOutcomes()
        {
            var authorization = new RuntimeSaveAuthorization();
            using (authorization.Activate(ValidFixture(), SaveRoot, true))
            {
                authorization.Authorize(RuntimeSaveOperation.Load, WorkingTarget(), SaveRoot);

                var baseline = WorkingTarget();
                baseline.InternalName = RuntimeRequest.BaselineSaveName;
                baseline.FileName = BaselineFileName;
                baseline.FullPath = Path.Combine(SaveRoot, BaselineFileName);
                authorization.Authorize(RuntimeSaveOperation.Load, baseline, SaveRoot);

                var foreign = WorkingTarget();
                foreign.InternalName = "FOREIGN";
                authorization.Authorize(RuntimeSaveOperation.Load, foreign, SaveRoot);
                authorization.ReportFatalViolation(RuntimeSaveOperation.Load, "projection failed");
                authorization.ReportBoundaryFailure(RuntimeSaveOperation.Write, "cleanup residue");

                TestRunner.Equal(1, authorization.AuthorizedLoadCount, "Working load count differs.");
                TestRunner.Equal(1, authorization.BaselineLoadRequestCount, "Baseline load was not classified separately.");
                TestRunner.Equal(2, authorization.UnauthorizedLoadCount, "Generic unauthorized load count differs.");
                TestRunner.Equal(1, authorization.UnauthorizedWriteCount, "Boundary write rejection was not counted.");
            }
        }

        private static void ExactWorkingWriteProbeIsSuppressedOnce()
        {
            var authorization = new RuntimeSaveAuthorization();
            using (authorization.Activate(ValidFixture(), SaveRoot, false))
            {
                using (authorization.ArmOneShotWorkingWriteSuppression())
                {
                    TestRunner.True(authorization.IsOneShotWorkingWriteSuppressionArmed,
                        "Working-write suppression did not arm.");
                    var first = authorization.Authorize(RuntimeSaveOperation.Write, WorkingTarget(), SaveRoot);
                    TestRunner.Equal(false, first.Allowed, "Expected diagnostic write probe entered serialization.");
                    TestRunner.Equal(false, first.FatalViolation, "Expected diagnostic write suppression was marked fatal.");
                    TestRunner.Equal(false, authorization.IsOneShotWorkingWriteSuppressionArmed,
                        "One-shot suppression remained armed after consumption.");
                    TestRunner.Equal(1, authorization.SuppressedWorkingWriteCount,
                        "Suppressed Working-write count differs.");
                    TestRunner.Equal(0, authorization.UnauthorizedWriteCount,
                        "Expected suppression polluted the unauthorized-write counter.");
                    TestRunner.Equal(0, authorization.FatalViolationCount,
                        "Expected suppression produced fatal telemetry.");
                }

                var second = authorization.Authorize(RuntimeSaveOperation.Write, WorkingTarget(), SaveRoot);
                AssertFatalRejection(second, "second Working write after one-shot consumption");
                TestRunner.Equal(1, authorization.SuppressedWorkingWriteCount,
                    "A second write consumed nonexistent suppression authority.");
            }
        }

        private static void SuppressionProbeDoesNotBroadenAuthority()
        {
            var authorization = new RuntimeSaveAuthorization();
            using (authorization.Activate(ValidFixture(), SaveRoot, false))
            using (authorization.ArmOneShotWorkingWriteSuppression())
            {
                var foreign = WorkingTarget();
                foreign.InternalName = "FOREIGN";
                AssertFatalRejection(
                    authorization.Authorize(RuntimeSaveOperation.Write, foreign, SaveRoot),
                    "foreign write while suppression armed");
                TestRunner.True(authorization.IsOneShotWorkingWriteSuppressionArmed,
                    "Wrong-target write consumed the exact Working suppression.");

                var exact = authorization.Authorize(RuntimeSaveOperation.Write, WorkingTarget(), SaveRoot);
                TestRunner.Equal(false, exact.Allowed, "Exact probe entered serialization after wrong-target rejection.");
                TestRunner.Equal(false, exact.FatalViolation, "Exact probe was not classified as expected suppression.");
                TestRunner.Equal(1, authorization.SuppressedWorkingWriteCount,
                    "Exact probe was not counted once.");
            }
        }

        private static void SuppressionLeaseCancelsUnconsumedArm()
        {
            var authorization = new RuntimeSaveAuthorization();
            using (authorization.Activate(ValidFixture(), SaveRoot, false))
            {
                var lease = authorization.ArmOneShotWorkingWriteSuppression();
                var doubleArmRejected = false;
                try
                {
                    authorization.ArmOneShotWorkingWriteSuppression();
                }
                catch (InvalidOperationException)
                {
                    doubleArmRejected = true;
                }

                TestRunner.True(doubleArmRejected, "A second one-shot suppression was armed concurrently.");
                lease.Dispose();
                lease.Dispose();
                TestRunner.Equal(false, authorization.IsOneShotWorkingWriteSuppressionArmed,
                    "Unconsumed suppression remained armed after lease disposal.");
                AssertFatalRejection(
                    authorization.Authorize(RuntimeSaveOperation.Write, WorkingTarget(), SaveRoot),
                    "Working write after suppression lease cancellation");
            }
        }

        private static void AssertFatalRejection(RuntimeSaveAuthorizationDecision decision, string context)
        {
            TestRunner.Equal(false, decision.Allowed, context + " was allowed.");
            TestRunner.True(decision.FatalViolation, context + " was not reported as fatal.");
            TestRunner.True(!string.IsNullOrWhiteSpace(decision.Reason), context + " has no diagnostic reason.");
        }

        private static RuntimeSaveTarget WorkingTarget()
        {
            return new RuntimeSaveTarget
            {
                InternalName = RuntimeRequest.WorkingSaveName,
                FileName = WorkingFileName,
                FullPath = Path.Combine(SaveRoot, WorkingFileName),
                SaveType = "Manual",
                GameId = GameId,
                GameName = GameName,
                Area = Area
            };
        }

        private static RuntimeFixtureIdentity ValidFixture()
        {
            return new RuntimeFixtureIdentity
            {
                Baseline = new RuntimeSaveDescriptor
                {
                    InternalName = RuntimeRequest.BaselineSaveName,
                    FileName = BaselineFileName,
                    Sha256 = Sha,
                    Length = 686605,
                    LastWriteTimeUtcTicks = 638907120000000000L,
                    GameId = GameId,
                    GameName = GameName,
                    Area = Area
                },
                Working = new RuntimeSaveDescriptor
                {
                    InternalName = RuntimeRequest.WorkingSaveName,
                    FileName = WorkingFileName,
                    Sha256 = "abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789",
                    Length = 684085,
                    LastWriteTimeUtcTicks = 638907120010000000L,
                    GameId = GameId,
                    GameName = GameName,
                    Area = Area
                },
                WriteAuthorization = new RuntimeSaveWriteAuthorization
                {
                    Mode = "working-only",
                    AllowedInternalName = RuntimeRequest.WorkingSaveName,
                    AllowedFileName = WorkingFileName,
                    BaselineImmutable = true
                }
            };
        }
    }
}
