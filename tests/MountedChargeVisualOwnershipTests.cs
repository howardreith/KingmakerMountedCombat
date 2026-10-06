using System;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    internal static class MountedChargeVisualOwnershipTests
    {
        private sealed class Effect { internal bool InPool; internal bool Destroyed; }
        internal static void Register(TestRunner runner)
        {
            runner.Run("charge FX waits for observed native fade completion", () =>
            {
                var owner = new MountedChargeVisualOwnership<Effect>(); var effect = new Effect(); var calls = 0;
                var acquisition = owner.Capture(effect);
                Action<Effect> request = item => calls++;
                Func<Effect, bool> settled = item => item.Destroyed;
                TestRunner.True(!owner.TryComplete(acquisition, request, settled) && !owner.Drained && calls == 1,
                    "A returned fade request fabricated completion.");
                TestRunner.True(!owner.TryComplete(acquisition, request, settled) && calls == 1,
                    "A later barrier replayed an outstanding fade request.");
                effect.Destroyed = true;
                TestRunner.True(owner.TryComplete(acquisition, request, settled) && owner.Drained && calls == 1,
                    "Observed Unity completion did not drain its retained acquisition.");
            });
            runner.Run("charge FX pool return latches before unrelated reuse", () =>
            {
                var owner = new MountedChargeVisualOwnership<Effect>(); var effect = new Effect(); var calls = 0;
                var old = owner.Capture(effect); effect.InPool = true;
                TestRunner.True(owner.ObserveReturn(effect, item => item.InPool), "Exact observed pool return was lost.");
                effect.InPool = false; // Another native actor claimed the same object.
                TestRunner.True(owner.TryComplete(old, item => calls++, item => { calls++; return false; }) && calls == 0,
                    "Retired generation inspected or destroyed another pool user's object.");
                var next = owner.Capture(effect);
                TestRunner.True(next.Generation != old.Generation && !next.Returned && !owner.Drained,
                    "A later owned acquisition inherited an earlier return verdict.");
            });
            runner.Run("charge FX foreign release and incomplete pool return cannot settle ownership", () =>
            {
                var owner = new MountedChargeVisualOwnership<Effect>(); var effect = new Effect(); var foreign = new Effect { InPool = true };
                owner.Capture(effect);
                TestRunner.True(!owner.ObserveReturn(foreign, item => item.InPool) && !owner.ObserveReturn(effect, item => item.InPool) && !owner.Drained,
                    "A foreign or unproven release erased native visual debt.");
            });
            runner.Run("charge FX residue observation failure cannot revive returned custody", () =>
            {
                var owner = new MountedChargeVisualOwnership<Effect>(); var effect = new Effect { InPool = true };
                var acquisition = owner.Capture(effect); var debt = false; var calls = 0;
                try
                {
                    owner.ObserveReturn(effect, item => item.InPool);
                    throw new InvalidOperationException("native residue reader failed after queue proof");
                }
                catch (InvalidOperationException) { debt = true; }
                effect.InPool = false; // A different native owner reuses this object.
                TestRunner.True(debt && owner.TryComplete(acquisition, item => calls++, item => { calls++; return false; }) && calls == 0,
                    "Fallible post-transfer observations restored authority over a later pool user.");
            });
            runner.Run("charge FX thrown request remains retained until postconditions pass", () =>
            {
                var owner = new MountedChargeVisualOwnership<Effect>(); var effect = new Effect(); var calls = 0;
                var acquisition = owner.Capture(effect);
                Action<Effect> request = item => { calls++; throw new InvalidOperationException(); };
                TestRunner.True(!owner.TryComplete(acquisition, request, item => item.Destroyed) && acquisition.Failure != null,
                    "Throwing native effect removal lost its exact acquisition.");
                effect.Destroyed = true;
                TestRunner.True(owner.TryComplete(acquisition, request, item => item.Destroyed) && calls == 1,
                    "Native settlement replayed a failed cleanup callback.");
            });
            runner.Run("charge FX duplicate observations retain one live generation", () =>
            {
                var owner = new MountedChargeVisualOwnership<Effect>(); var effect = new Effect();
                TestRunner.True(ReferenceEquals(owner.Capture(effect), owner.Capture(effect)) && owner.Acquisitions.Count == 1,
                    "Creation and later setter observations duplicated a single native acquisition.");
            });
            runner.Run("charge native FX cleanup cannot destroy a returned generation through a stale field", () =>
            {
                var owner = new MountedChargeVisualOwnership<Effect>(); var effect = new Effect();
                var reference = effect; var calls = 0; var acquisition = owner.Capture(effect);
                try { owner.RequestCompletion(reference, item => { calls++; throw new InvalidOperationException("fade started before setter"); }); }
                catch (InvalidOperationException) { }
                TestRunner.True(ReferenceEquals(reference, effect) && acquisition.Failure != null && !owner.Drained,
                    "Throwing native destruction failed to preserve its unresolved reference and debt.");
                effect.InPool = true; owner.ObserveReturn(effect, item => item.InPool);
                effect.InPool = false; // Foreign actor now owns this object.
                owner.RequestCompletion(reference, item => calls++);
                reference = null; // Native DestroyFx's remaining setter is still executed.
                TestRunner.True(reference == null && calls == 1 && owner.Drained && acquisition.Failure != null,
                    "Stale native field destroyed a reused generation or lost failure history.");
                var next = owner.Capture(effect);
                TestRunner.True(owner.RequestCompletion(effect, item => calls++) && calls == 2 && !next.Returned,
                    "An earlier return masked this owner's later live acquisition.");
            });
            runner.Run("charge native FX cleanup refuses unknown objects before any mutation", () =>
            {
                var owner = new MountedChargeVisualOwnership<Effect>(); var foreign = new Effect(); var calls = 0; var refused = false;
                try { owner.RequestCompletion(foreign, item => calls++); }
                catch (InvalidOperationException) { refused = true; }
                TestRunner.True(refused && calls == 0, "An unknown native reference gained cleanup authority.");
            });
        }
    }
}
