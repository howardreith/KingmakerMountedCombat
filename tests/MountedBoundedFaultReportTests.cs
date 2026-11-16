using System;
using System.Collections.Generic;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // Known issues sprint 3 continuation: the group-pace hooks' fault report (MountedPatchController.PatchMethods
    // .ReportGroupPaceFault) goes through MountedBoundedFaultReport. The compiled hooks themselves are executed with an
    // operational failure and a throwing logger by scripts/Test-ContinuousDriveContracts.ps1.
    internal static class MountedBoundedFaultReportTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("known issues sprint 3: a fault report whose logger throws is contained, made once, and leaves the caller's native fallback intact", FaultReportContainsAThrowingLogger);
        }

        private static void FaultReportContainsAThrowingLogger()
        {
            // The shape of every hook that uses the report: an operational failure, a report, then the native answer.
            var report = new MountedBoundedFaultReport();
            var calls = 0;
            Action<string, Exception> throwing = (message, error) => { calls++; throw new InvalidOperationException("the logger failed"); };
            Func<List<string>, List<string>> hook = selected =>
            {
                try { throw new NullReferenceException("operational failure"); }
                catch (Exception error) { report.Report(throwing, "left native", error); return selected; }
            };
            var input = new List<string> { "a", "b" };
            List<string> output = null;
            Exception escaped = null;
            try { output = hook(input); } catch (Exception error) { escaped = error; }
            TestRunner.True(escaped == null, "A logger failure escaped the fault report: " + escaped);
            TestRunner.True(ReferenceEquals(output, input), "The native inputs were not returned after the report.");
            TestRunner.True(report.Reported && report.SinkFailed, "The report did not record that it was made and that its sink failed.");
            hook(input);
            hook(input);
            TestRunner.Equal(1, calls, "A failing logger was called again: the report is not bounded.");

            // A working logger receives the one report; a missing logger is harmless.
            var working = new MountedBoundedFaultReport();
            var received = new List<string>();
            var fault = new InvalidOperationException("x");
            working.Report((message, error) => received.Add(message + ":" + error.Message), "first", fault);
            working.Report((message, error) => received.Add(message + ":" + error.Message), "second", fault);
            TestRunner.True(received.Count == 1 && received[0] == "first:x" && !working.SinkFailed, "A working logger did not receive exactly the first report.");
            var silent = new MountedBoundedFaultReport();
            silent.Report(null, "none", fault);
            TestRunner.True(silent.Reported && !silent.SinkFailed, "A missing logger was not tolerated.");
        }
    }
}
