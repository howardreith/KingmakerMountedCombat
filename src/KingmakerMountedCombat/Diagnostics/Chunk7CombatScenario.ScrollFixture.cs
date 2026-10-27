using System;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Commands;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    // 7E folded measurements (row 57 prone stand-up, row 69 defensive casting): one disposable native Cure Light
    // Wounds scroll stack built on the caster's own equipped quick-slot entity through the existing
    // NativeCastingItemLease (four factory units, so every measured spend starts at two or more units), its cast
    // observer (NativeCastingItemTrace over the caster and one partner) and the original quick slots. The scroll
    // ability is used only through the native selected-ability click; the native engine owns the cast, its checks,
    // the spend and every cost. The stack is released and the original quick slots are observed restored.
    internal sealed partial class Chunk7CombatScenario
    {
        private sealed class FoldedScrollFixture
        {
            internal UnitEntityData Caster;
            internal NativeCastingItemTrace Casts;
            internal NativeCastingOriginalSlots Slots;
            internal NativeCastingItemLease Scroll;
            internal JObject Record;
            internal bool Released;
        }
        internal const int FoldedScrollUnits = 4;
        // Out of combat and settled: the fixture fields are assigned before each native step, so an early failure
        // still reaches the cleanup release.
        private FoldedScrollFixture AcquireFoldedScroll(UnitEntityData caster, UnitEntityData partner, JObject record, string uncovered)
        {
            var fixture = new FoldedScrollFixture { Caster = caster, Record = record };
            foldedScrolls.Add(fixture);
            record["casterId"] = caster.UniqueId; record["scrollUnits"] = FoldedScrollUnits;
            record["scrollBlueprint"] = NativeCastingItemLease.CureScroll;
            fixture.Casts = new NativeCastingItemTrace(caster, partner);
            fixture.Slots = new NativeCastingOriginalSlots(caster, fixture.Casts);
            fixture.Scroll = new NativeCastingItemLease(caster, fixture.Casts, NativeCastingItemLease.CureScroll, fixture.Slots);
            fixture.Scroll.Acquire(FoldedScrollUnits);
            var ability = fixture.Scroll.Item.Ability?.Data;
            record["scrollAbility"] = fixture.Casts.Ability(ability);
            Require(ability != null && ReferenceEquals(ability.SourceItem, fixture.Scroll.Item) && ability.Blueprint.AssetGuid == StagedClwGuid &&
                ability.Caster?.Unit == caster && ability.IsAvailableForCast, uncovered);
            return fixture;
        }
        private readonly System.Collections.Generic.List<FoldedScrollFixture> foldedScrolls = new System.Collections.Generic.List<FoldedScrollFixture>();
        // One ordinary native ability click of the exact equipped scroll on a unit target; one shell at most.
        private UnitUseAbility IssueFoldedScrollCast(FoldedScrollFixture fixture, UnitEntityData spellTarget, JObject record)
        {
            fixture.Scroll.RequireExactlyEquipped("Folded scroll cast", 2);
            var ability = fixture.Scroll.Item.Ability?.Data;
            Require(ability != null && ReferenceEquals(ability.SourceItem, fixture.Scroll.Item) && ability.Caster.Unit == fixture.Caster,
                "Native scroll ability does not source the exact equipped fixture stack on its caster.");
            record["itemsBefore"] = fixture.Scroll.Snapshot(); record["castEventsBefore"] = fixture.Casts.EventCount;
            Chunk7NativeFixture.Select(fixture.Caster); Game.Instance.IsPaused = true;
            var handler = Game.Instance.SelectedAbilityHandler;
            handler.SetAbility(ability);
            record["ability"] = fixture.Casts.Ability(ability); record["target"] = spellTarget.UniqueId;
            record["selected"] = ReferenceEquals(handler.Ability, ability);
            var resolved = handler.GetTarget(spellTarget.View.gameObject, spellTarget.Position, ability);
            record["canTarget"] = resolved != null && ability.CanTarget(resolved);
            try { record["clicked"] = handler.OnClick(spellTarget.View.gameObject, spellTarget.Position, 0, false, false); }
            finally { if (handler.Ability != null) handler.SetAbility(null); Game.Instance.IsPaused = false; }
            var shells = fixture.Caster.Commands.Raw.Concat(fixture.Caster.Commands.Queue).OfType<UnitUseAbility>()
                .Where(c => c.Spell.Blueprint == ability.Blueprint).Distinct().ToArray();
            record["admittedShellCount"] = shells.Length;
            Require(shells.Length == 1 && (bool)record["clicked"], "One normal scroll input did not admit exactly one native cast shell.");
            record["shell"] = fixture.Casts.Shell(shells[0]); record["shellIdentity"] = fixture.Casts.Identity(shells[0]);
            issuedCommands.Add(shells[0]);
            return shells[0];
        }
        private bool FoldedCastSettled(FoldedScrollFixture fixture, UnitUseAbility shell, UnitEntityData spellTarget, ref int frames)
        {
            var settled = (shell == null || shell.IsFinished) && fixture.Casts.ProcessesSettled && fixture.Caster.Commands.Empty &&
                spellTarget.Commands.Empty && !NativeSaveEffectBoundary.HasUnresolvedAbilities() && !NativeSaveEffectBoundary.HasUnresolvedProjectiles() &&
                !fixture.Caster.AreHandsBusyWithAnimation && !spellTarget.AreHandsBusyWithAnimation && target.Commands.Empty &&
                !target.AreHandsBusyWithAnimation && (a == null || !a.Combat.HasActiveCommand && !a.Combat.HasActiveGroundMovement);
            if (!settled) { frames = 0; return false; }
            return ++frames >= 10;
        }
        private void FinishFoldedCast(FoldedScrollFixture fixture, UnitUseAbility shell, JObject record)
        {
            record["itemsAfter"] = fixture.Scroll.Snapshot();
            record["castEvents"] = fixture.Casts.EventsSince((int)record["castEventsBefore"]);
            record["terminal"] = fixture.Casts.Shell(shell);
        }
        // Release order as the staged stack: the owned item leaves its slot and the inventory, the original quick
        // slots are restored and observed, the lease disposes, then the cast observer retires.
        private void ReleaseFoldedScrollFixtures()
        {
            foreach (var fixture in foldedScrolls)
            {
                if (fixture.Released) continue;
                Require(fixture.Casts == null || fixture.Casts.ProcessesSettled && fixture.Caster.Commands.Empty,
                    "A folded scroll stack is released only after its caster's casts and commands have settled.");
                if (fixture.Scroll != null)
                {
                    fixture.Scroll.ReleaseOwnedItem(); fixture.Slots.Restore(); fixture.Scroll.Dispose();
                    fixture.Record["scroll"] = fixture.Scroll.Evidence; fixture.Record["originalSlots"] = fixture.Slots.Evidence;
                }
                if (fixture.Casts != null) { fixture.Casts.Dispose(); fixture.Record["castTrace"] = fixture.Casts.Capture(); }
                fixture.Released = true;
            }
        }
        // The native check events of one cast window (the observer's own records), by the caster.
        private static JObject[] CastEvents(JObject record, string kind, string actor) =>
            ((JArray)record["castEvents"]).OfType<JObject>().Where(e => (string)e["kind"] == kind && (string)e["actor"] == actor).ToArray();
    }
}
