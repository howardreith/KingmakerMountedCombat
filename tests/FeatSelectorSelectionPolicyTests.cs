using System.Collections.Generic;
using System.Linq;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // The installed Toggle.Set, NotifyToggleOn and SetAllTogglesOff contracts plus the native
    // light refresh, modelled closely enough to reach the production decision. Rendering
    // stays a native-screen claim; this proves only the state transitions.
    internal static class FeatSelectorSelectionPolicyTests
    {
        private sealed class Group
        {
            public bool AllowSwitchOff;
            public readonly List<Row> Rows = new List<Row>();
            public bool AnyOn => Rows.Any(row => row.Active && row.IsOn);
            public void ClearAll()
            {
                var previous = AllowSwitchOff;
                AllowSwitchOff = true;
                foreach (var row in Rows.Where(row => row.Active)) row.SetOn(false);
                AllowSwitchOff = previous;
            }
        }

        private sealed class Row
        {
            public string Feature;
            public bool Active = true;
            public bool IsOn;
            public int Actions;
            public int UnguardedChanges;
            public Selector Owner;
            public void SetOn(bool value)
            {
                if (IsOn == value) return;
                IsOn = value;
                var group = Owner.Group;
                if (Active && (IsOn || (!group.AnyOn && !group.AllowSwitchOff)))
                {
                    IsOn = true;
                    foreach (var other in group.Rows.Where(other => other != this && other.Active)) other.SetOn(false);
                }
                if (!Owner.IgnoreAction) UnguardedChanges++;
                if (IsOn && !Owner.IgnoreAction) { Actions++; Owner.Selected = Feature; }
            }
        }

        private sealed class Selector
        {
            public readonly Group Group = new Group();
            public bool IgnoreAction;
            public bool Offers = true;
            public string Selected;
            public int Clears;
            public Selector(params string[] features)
            {
                foreach (var feature in features) Group.Rows.Add(new Row { Feature = feature, Owner = this });
            }
            public Row this[string feature] => Group.Rows.Single(row => row.Feature == feature);
            public void LightRefresh(bool withProductionSync)
            {
                IgnoreAction = true;
                foreach (var row in Group.Rows) row.SetOn(row.Feature == Selected);
                IgnoreAction = false;
                if (!withProductionSync) return;
                if (!FeatSelectorSelectionPolicy.MustClearStaleRows(Offers, Selected != null, Group.AnyOn)) return;
                Clears++;
                IgnoreAction = true;
                Group.ClearAll();
                IgnoreAction = false;
            }
        }

        internal static void BeforeTheFixALastSelectedRowStaysOnWithNothingSelected()
        {
            var selector = Selected("Mounted Combat");
            selector.Selected = null;
            selector.LightRefresh(withProductionSync: false);
            TestRunner.Equal(true, selector["Mounted Combat"].IsOn, "The native light refresh should leave the F1c stale row on.");
        }

        internal static void RemovingThePrerequisiteClearsTheStaleRowWithoutACallback()
        {
            var selector = Selected("Mounted Combat");
            var actions = selector["Mounted Combat"].Actions;
            var unguarded = selector["Mounted Combat"].UnguardedChanges;
            selector.Selected = null;
            selector.LightRefresh(withProductionSync: true);
            TestRunner.Equal(false, selector["Mounted Combat"].IsOn, "A row with no current selection stayed selected.");
            TestRunner.Equal(1, selector.Clears, "The stale row was not cleared exactly once.");
            TestRunner.Equal(actions, selector["Mounted Combat"].Actions, "Clearing fired a selection callback.");
            TestRunner.Equal(unguarded, selector["Mounted Combat"].UnguardedChanges, "Clearing ran outside the native action guard.");
            TestRunner.Equal(false, selector.IgnoreAction, "The native action guard was not restored.");
            selector.LightRefresh(withProductionSync: true);
            TestRunner.Equal(1, selector.Clears, "An already clear selector was cleared again.");
        }

        internal static void RegainingThePrerequisiteAllowsReselection()
        {
            var selector = Selected("Mounted Combat");
            selector.Selected = null;
            selector.LightRefresh(withProductionSync: true);
            selector.LightRefresh(withProductionSync: true);
            selector["Mounted Combat"].SetOn(true);
            TestRunner.Equal("Mounted Combat", selector.Selected, "Reselection did not reach the native action.");
            selector.LightRefresh(withProductionSync: true);
            TestRunner.Equal(true, selector["Mounted Combat"].IsOn, "A reselected row lost its state.");
        }

        internal static void AValidSelectionSurvivesAnEqualPreviewRebind()
        {
            var selector = Selected("Mounted Combat");
            selector.LightRefresh(withProductionSync: true);
            TestRunner.Equal(true, selector["Mounted Combat"].IsOn, "A valid selected row was cleared.");
            TestRunner.Equal(0, selector.Clears, "A valid selection reached the clear.");
        }

        internal static void ANativeComparisonRowKeepsItsNativeExclusivity()
        {
            var selector = Selected("Alertness");
            selector["Blind Fight"].SetOn(true);
            selector.LightRefresh(withProductionSync: true);
            TestRunner.Equal("Blind Fight", selector.Selected, "The native comparison selection changed.");
            TestRunner.Equal(false, selector["Alertness"].IsOn, "The previous native row stayed on.");
            TestRunner.Equal(true, selector["Blind Fight"].IsOn, "The native comparison row lost its state.");
            TestRunner.Equal(0, selector.Clears, "A native successor needed the clear.");
        }

        internal static void ANativeRowLosingItsPrerequisiteClearsToo()
        {
            var selector = Selected("Alertness");
            selector.Selected = null;
            selector.LightRefresh(withProductionSync: true);
            TestRunner.Equal(false, selector["Alertness"].IsOn, "The same fault stayed on a native row in the offering selection.");
        }

        internal static void ASelectionNotOfferingTheFeatIsLeftNative()
        {
            var selector = Selected("Alertness");
            selector.Offers = false;
            selector.Selected = null;
            selector.LightRefresh(withProductionSync: true);
            TestRunner.Equal(true, selector["Alertness"].IsOn, "A selection outside the seam changed.");
            TestRunner.Equal(0, selector.Clears, "A selection outside the seam was cleared.");
        }

        internal static void AHiddenRowIsNotTheGroupsToClear()
        {
            var selector = Selected("Mounted Combat");
            selector["Mounted Combat"].Active = false;
            selector.Selected = null;
            selector.LightRefresh(withProductionSync: true);
            TestRunner.Equal(false, selector["Mounted Combat"].IsOn, "Native should switch a hidden row off directly.");
            TestRunner.Equal(0, selector.Clears, "A hidden row reached the clear.");
        }

        internal static void ANewPreviewInheritsNothing()
        {
            var selector = Selected("Mounted Combat");
            var fresh = new Selector("Mounted Combat", "Alertness", "Blind Fight");
            fresh.LightRefresh(withProductionSync: true);
            TestRunner.Equal(false, fresh["Mounted Combat"].IsOn, "A fresh preview inherited the row state.");
            TestRunner.Equal(0, fresh["Mounted Combat"].Actions, "A fresh preview inherited a callback.");
            TestRunner.Equal(true, selector["Mounted Combat"].IsOn, "A fresh preview changed the old preview.");
        }

        private static Selector Selected(string feature)
        {
            var selector = new Selector("Mounted Combat", "Alertness", "Blind Fight");
            selector[feature].SetOn(true);
            return selector;
        }
    }
}
