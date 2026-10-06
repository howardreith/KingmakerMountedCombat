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
            runner.Run("charge fact captures exact ownership before throwing activation", () =>
            {
                var owner = new MountedChargeFactOwnership<Fact>(); var fact = new Fact();
                var native = new List<Fact>();
                try { owner.Acquire(() => { owner.CaptureCreated(fact); native.Add(fact); throw new InvalidOperationException(); }); }
                catch (InvalidOperationException) { }
                var removed = owner.TryRemove(item => { native.Remove(item); item.Residue = 0; item.Disposed = true; },
                    item => !native.Contains(item) && item.Disposed && item.Residue == 0);
                TestRunner.True(!removed && owner.Outstanding && ReferenceEquals(owner.Fact, fact), "Partial activation lost its only owner.");
                TestRunner.True(native.Count == 0 && fact.Residue == 0 && owner.Fault != null, "Independent safe removal was skipped or callback debt erased.");
            });
            runner.Run("charge fact never adopts an unrelated native return", () =>
            {
                var owner = new MountedChargeFactOwnership<Fact>(); var owned = new Fact(); var foreign = new Fact();
                try { owner.Acquire(() => { owner.CaptureCreated(owned); return foreign; }); } catch (InvalidOperationException) { }
                TestRunner.True(owner.Outstanding && !owner.Acquired && ReferenceEquals(owner.Fact, owned), "Foreign returned fact replaced exact acquisition identity.");
            });
            runner.Run("charge fact removal exception after unlink retains debt without callback replay", () =>
            {
                var owner = new MountedChargeFactOwnership<Fact>(); var fact = new Fact(); var native = new List<Fact>();
                owner.Acquire(() => { owner.CaptureCreated(fact); native.Add(fact); return fact; }); var calls = 0;
                Action<Fact> remove = item => { calls++; native.Remove(item); item.Disposed = true; throw new InvalidOperationException(); };
                Func<Fact, bool> absent = item => !native.Contains(item) && item.Disposed && item.Residue == 0;
                try { owner.TryRemove(remove, absent); } catch (InvalidOperationException) { }
                fact.Residue = 0;
                TestRunner.True(!owner.TryRemove(remove, absent) && calls == 1 && owner.Outstanding, "Absent/disposed fact erased unconfirmed callback debt or replayed it.");
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
