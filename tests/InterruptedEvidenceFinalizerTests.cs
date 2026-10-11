using System;
using System.Collections.Generic;
using KingmakerMountedCombat.Diagnostics;

namespace KingmakerMountedCombat.Tests
{
    internal static class InterruptedEvidenceFinalizerTests
    {
        internal static void Register(TestRunner runner)
        {
            runner.Run("interrupted evidence survives cleanup failure and publishes observed debt once", () => {
                var finalizer = new InterruptedEvidenceFinalizer();
                var events = new List<string>();
                bool actorLive = true, capturedLive = false, cleanupDebt = false;
                Action run = () => finalizer.Capture(
                    () => { capturedLive = actorLive; events.Add("FAIL"); },
                    () => { actorLive = false; throw new InvalidOperationException("native removal debt"); },
                    ex => { cleanupDebt = ex.Message == "native removal debt"; events.Add("cleanup failed"); },
                    () => { TestRunner.True(capturedLive && cleanupDebt, "Failure observations lost before publication."); events.Add("publish"); });
                run(); run();
                TestRunner.Equal("FAIL,cleanup failed,publish", string.Join(",", events), "Interrupted evidence lost or duplicated.");
                TestRunner.True(finalizer.Published, "Publication did not settle.");
            });
            runner.Run("failed evidence publication is retryable and never reported as published", () => {
                var finalizer = new InterruptedEvidenceFinalizer(); int rows = 0, attempts = 0;
                Action publish = () => { if (++attempts == 1) throw new System.IO.IOException("write interrupted"); };
                bool threw = false;
                try { finalizer.Capture(() => rows++, () => { }, ex => { throw ex; }, publish); }
                catch (System.IO.IOException) { threw = true; }
                TestRunner.True(threw && !finalizer.Published, "Unobserved publication became success.");
                finalizer.Capture(() => rows++, () => { }, ex => { throw ex; }, publish);
                TestRunner.True(finalizer.Published && rows == 1 && attempts == 2, "Retry replaced or duplicated failure evidence.");
            });
        }
    }
}
