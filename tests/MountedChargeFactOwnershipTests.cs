using System;
using System.Collections.Generic;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    internal static class MountedChargeFactOwnershipTests
    {
        private sealed class Fact { internal bool Disposed; internal int Residue = 1; }
        internal static void Register(TestRunner runner)
        {
            runner.Run("charge fact refuses reentrant cleanup until acquisition returns", () =>
            {
                var owner = new MountedChargeFactOwnership<Fact>(); var fact = new Fact();
                var native = new List<Fact>(); var removals = 0; var captures = 0;
                Action<Fact> remove = item => { removals++; native.Remove(item); item.Residue = 0; item.Disposed = true; };
                Func<Fact, bool> settled = item => !native.Contains(item) && item.Disposed && item.Residue == 0;
                owner.Acquire(() =>
                {
                    owner.CaptureCreated(fact);
                    if (owner.TryRemove(remove, settled)) captures++;
                    TestRunner.True(owner.Acquiring && owner.Outstanding && removals == 0,
                        "Save cleanup touched a created fact before native insertion.");
                    native.Add(fact);
                    if (owner.TryRemove(remove, settled)) captures++;
                    TestRunner.True(removals == 0 && native.Contains(fact),
                        "Reentrant save removed a fact while activation could still resume.");
                    return fact;
                });
                TestRunner.True(captures == 0 && !owner.Acquiring && owner.Acquired,
                    "Acquisition scope did not end exactly at the native return.");
                TestRunner.True(owner.TryRemove(remove, settled) && removals == 1 && !owner.Outstanding,
                    "A later settled save could not drain the retained exact fact.");
            });
            runner.Run("charge fact acquisition exception ends scope but retains exact debt", () =>
            {
                var owner = new MountedChargeFactOwnership<Fact>(); var fact = new Fact();
                try { owner.Acquire(() => { owner.CaptureCreated(fact); throw new InvalidOperationException(); }); }
                catch (InvalidOperationException) { }
                TestRunner.True(!owner.Acquiring && owner.Outstanding && !owner.Acquired && ReferenceEquals(owner.Fact, fact),
                    "Failed acquisition left a fabricated live scope or dropped its exact owner.");
            });
            runner.Run("automatic child removal is observed before callbacks and never replayed", () =>
            {
                var owner = new MountedChargeFactOwnership<Fact>(); var fact = new Fact(); var calls = 0;
                owner.Acquire(() => { owner.CaptureCreated(fact); return fact; });
                owner.ObserveNativeRemoval(fact);
                fact.Disposed = true; // native parent caught a failure before residue drained
                Func<Fact, bool> settled = item => item.Disposed && item.Residue == 0;
                TestRunner.True(!owner.TryRemove(item => calls++, settled) && calls == 0 && owner.Outstanding,
                    "Absent child caused replay of an automatic native callback.");
                fact.Residue = 0;
                TestRunner.True(owner.TryRemove(item => calls++, settled) && calls == 0 && !owner.Outstanding,
                    "Independent native settlement did not drain retained child ownership.");
            });
            runner.Run("foreign child cannot mark the owned fact removal", () =>
            {
                var owner = new MountedChargeFactOwnership<Fact>(); var owned = new Fact(); var foreign = new Fact();
                owner.Acquire(() => { owner.CaptureCreated(owned); return owned; });
                var refused = false;
                try { owner.ObserveNativeRemoval(foreign); } catch (InvalidOperationException) { refused = true; }
                TestRunner.True(refused && !owner.RemovalAttempted && owner.Outstanding,
                    "A foreign same-blueprint fact changed exact cleanup ownership.");
            });
            runner.Run("charge fact captures exact ownership before throwing activation", () =>
            {
                var owner = new MountedChargeFactOwnership<Fact>(); var fact = new Fact();
                var native = new List<Fact>();
                try { owner.Acquire(() => { owner.CaptureCreated(fact); native.Add(fact); throw new InvalidOperationException(); }); }
                catch (InvalidOperationException) { }
                var removed = owner.TryRemove(item => { native.Remove(item); item.Residue = 0; item.Disposed = true; },
                    item => !native.Contains(item) && item.Disposed && item.Residue == 0);
                TestRunner.True(removed && owner.Drained && ReferenceEquals(owner.Fact, fact), "Complete native postconditions did not drain a retained partial acquisition.");
                TestRunner.True(native.Count == 0 && fact.Residue == 0 && owner.Fault != null, "Independent safe removal was skipped or callback debt erased.");
            });
            runner.Run("charge fact never adopts an unrelated native return", () =>
            {
                var owner = new MountedChargeFactOwnership<Fact>(); var owned = new Fact(); var foreign = new Fact();
                try { owner.Acquire(() => { owner.CaptureCreated(owned); return foreign; }); } catch (InvalidOperationException) { }
                TestRunner.True(owner.Outstanding && !owner.Acquired && ReferenceEquals(owner.Fact, owned), "Foreign returned fact replaced exact acquisition identity.");
                TestRunner.True(!owner.TryRemove(item => { }, item => true) && owner.IdentityUncertain,
                    "Even clean-looking state discharged an ambiguous acquisition identity.");
            });
            runner.Run("charge fact removal exception after unlink retains debt without callback replay", () =>
            {
                var owner = new MountedChargeFactOwnership<Fact>(); var fact = new Fact(); var native = new List<Fact>();
                owner.Acquire(() => { owner.CaptureCreated(fact); native.Add(fact); return fact; }); var calls = 0;
                Action<Fact> remove = item => { calls++; native.Remove(item); item.Disposed = true; throw new InvalidOperationException(); };
                Func<Fact, bool> absent = item => !native.Contains(item) && item.Disposed && item.Residue == 0;
                try { owner.TryRemove(remove, absent); } catch (InvalidOperationException) { }
                TestRunner.True(!owner.TryRemove(remove, absent) && calls == 1 && owner.Outstanding,
                    "Absent/disposed fact erased remaining callback residue or replayed removal.");
                fact.Residue = 0;
                TestRunner.True(owner.TryRemove(remove, absent) && calls == 1 && owner.Drained && owner.Fault != null,
                    "Second exact postcondition proof failed to drain retained debt, replayed native removal, or erased failure history.");
            });
            runner.Run("charge fact waits for native residue and preserves foreign fact", () =>
            {
                var owner = new MountedChargeFactOwnership<Fact>(); var fact = new Fact(); var foreign = new Fact();
                var native = new List<Fact> { foreign }; var calls = 0;
                owner.Acquire(() => { owner.CaptureCreated(fact); native.Add(fact); return fact; });
                Action<Fact> remove = item => { calls++; native.Remove(item); item.Disposed = true; };
                Func<Fact, bool> settled = item => !native.Contains(item) && item.Disposed && item.Residue == 0;
                TestRunner.True(!owner.TryRemove(remove, settled) && owner.Outstanding, "Swallowed native residue was considered complete.");
                TestRunner.True(!owner.TryRemove(remove, settled) && calls == 1 && owner.Outstanding,
                    "Waiting for in-flight native callbacks replayed fact removal.");
                fact.Residue = 0;
                TestRunner.True(owner.TryRemove(remove, settled) && !owner.Outstanding && calls == 1 && native.Contains(foreign), "Observed settlement replayed removal or touched a foreign fact.");
            });
        }
    }
}
