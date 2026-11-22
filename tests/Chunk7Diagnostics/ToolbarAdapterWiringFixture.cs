// Offline wiring fixture of the toolbar adapter (review R1-F14). scripts/Test-ToolbarLifecycleMutations.ps1 compiles the
// actual production file src/KingmakerMountedCombat/Integration/NativeMountedControlService.Toolbar.cs (its ToolbarHost,
// roster adapter, save-scope and area forwarding, native slot brackets, archive capture and restore) together with the
// production lifecycle and this file. Everything below is a labeled substitute:
// - the Kingmaker types the adapter touches (Game, Player, UnitEntityData, UnitUISettings, slots, facts, AbilityData), as
//   minimal in-memory facades;
// - the members of NativeMountedControlService that live in its main file (Update, blueprints, ShouldLease, ResolveKind,
//   leases, logger), reduced to what the adapter calls. Update mirrors the main file's order (Housekeeping, the
//   disabled/suspended gates, Reconcile).
// The checks drive the service's own entry points; a wiring mutation in the adapter file must make a named check fail.
// Not native evidence.
using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;

namespace UnityEngine
{
    public static class Time { public static int frameCount; }
}

namespace Kingmaker.UnitLogic.Abilities.Blueprints
{
    public sealed class BlueprintAbility
    {
        public string name;
        public string AssetGuid;
    }
}

namespace Kingmaker.EntitySystem.Entities
{
    using Kingmaker.UI.UnitSettings;
    using Kingmaker.UnitLogic.Abilities;

    public struct UnitReference
    {
        public UnitEntityData Value;
        public UnitReference(UnitEntityData value) { Value = value; }
    }

    public sealed class UnitDescriptor
    {
        public UnitEntityData Unit;
        public UnitEntityData Pet;
        public bool IsPet;
        public UnitReference Master;
        public string PetUniqueId;
        public AbilityCollection Abilities;
    }

    public sealed class UnitEntityData
    {
        public string UniqueId;
        public UnitUISettings UISettings;
        public UnitDescriptor Descriptor;
        public AbilityData AutoUseAbility;

        public UnitEntityData(string id)
        {
            UniqueId = id;
            UISettings = new UnitUISettings();
            Descriptor = new UnitDescriptor { Unit = this };
            Descriptor.Abilities = new AbilityCollection(Descriptor);
        }
    }
}

namespace Kingmaker.UnitLogic.Abilities
{
    using Kingmaker.EntitySystem.Entities;
    using Kingmaker.UnitLogic.Abilities.Blueprints;

    public sealed class AbilityData
    {
        public BlueprintAbility Blueprint;
        public UnitDescriptor Caster;
    }

    public sealed class Ability
    {
        public BlueprintAbility Blueprint;
        public bool Active;
        public AbilityData Data;
    }

    public sealed class AbilityCollection
    {
        private readonly UnitDescriptor owner;
        private readonly List<Ability> facts = new List<Ability>();
        public AbilityCollection(UnitDescriptor owner) { this.owner = owner; }
        public IEnumerable<Ability> Enumerable => facts.ToArray();
        public Ability GetAbility(BlueprintAbility blueprint) => facts.FirstOrDefault(fact => ReferenceEquals(fact.Blueprint, blueprint));
        public void RemoveFact(Ability fact) => facts.Remove(fact);
        public object AddFact(BlueprintAbility blueprint, object context)
        {
            var fact = new Ability { Blueprint = blueprint, Active = true, Data = new AbilityData { Blueprint = blueprint, Caster = owner } };
            facts.Add(fact);
            return fact;
        }
    }
}

namespace Kingmaker.UI.UnitSettings
{
    using Kingmaker.EntitySystem.Entities;
    using Kingmaker.UnitLogic.Abilities;

    public class MechanicActionBarSlot { public UnitEntityData Unit; }
    public sealed class MechanicActionBarSlotEmpty : MechanicActionBarSlot { }
    public sealed class MechanicActionBarSlotAbility : MechanicActionBarSlot { public AbilityData Ability; }

    public sealed class UnitUISettings
    {
        public MechanicActionBarSlot[] Slots = new MechanicActionBarSlot[140];
        public int DirtyCount;
        public void SetSlot(MechanicActionBarSlot slot, int index) { Slots[index] = slot; }
        public MechanicActionBarSlot GetSlot(int index, UnitEntityData unit) =>
            Slots[index] ?? (Slots[index] = new MechanicActionBarSlotEmpty { Unit = unit });
        public void SetDirty() { DirtyCount++; }
    }
}

namespace Kingmaker.UI.SettingsUI
{
    public sealed class SettingsValue { public bool CurrentValue = true; }
    public sealed class SettingsRoot
    {
        public static SettingsRoot Instance = new SettingsRoot();
        public SettingsValue AutofillActionbarSlots = new SettingsValue();
    }
}

namespace Kingmaker
{
    using Kingmaker.EntitySystem.Entities;

    public sealed class Player
    {
        public List<UnitReference> PartyCharacters = new List<UnitReference>();
        public List<UnitReference> RemoteCompanions = new List<UnitReference>();
        public List<UnitReference> DetachedPartyCharacters = new List<UnitReference>();
        public List<UnitReference> ExCompanions = new List<UnitReference>();
        // Player.AllCharacters: the roster characters and their pets.
        public IEnumerable<UnitEntityData> AllCharacters
        {
            get
            {
                var owners = PartyCharacters.Concat(RemoteCompanions).Concat(DetachedPartyCharacters).Select(reference => reference.Value).ToList();
                return owners.Concat(owners.Where(unit => unit.Descriptor.Pet != null).Select(unit => unit.Descriptor.Pet)).Distinct().ToList();
            }
        }
    }

    public sealed class Game
    {
        public static Game Instance = new Game();
        public Player Player;
    }
}

namespace KingmakerMountedCombat.Integration
{
    using Kingmaker.EntitySystem.Entities;
    using Kingmaker.UI.UnitSettings;
    using Kingmaker.UnitLogic.Abilities;
    using Kingmaker.UnitLogic.Abilities.Blueprints;

    // Substitute: the archive row DTO and actor-id check (MountedSaveData.cs).
    internal sealed class SavedMountedSlot { public string ActorId; public int Index; public int Kind; }
    internal static class MountedSaveData { internal static bool ActorId(string id) => !string.IsNullOrEmpty(id); }

    // Substitute: the pair registry's owners (relationship.Ownership.Owners).
    internal sealed class PairOwner { public UnitEntityData Rider = null, Mount = null; }
    internal sealed class PairOwnership { public List<PairOwner> Owners = new List<PairOwner>(); }
    internal sealed class GameMountedRelationshipService { public PairOwnership Ownership = new PairOwnership(); }

    internal sealed class FixtureLogger { internal readonly List<string> Lines = new List<string>(); internal void Info(string line) => Lines.Add(line); }

    // Substitute of the members NativeMountedControlService keeps in its main file.
    internal sealed partial class NativeMountedControlService
    {
        internal static readonly BlueprintAbility MountStateBlueprint = new BlueprintAbility { name = "KMC_MountState", AssetGuid = "state" };
        internal static readonly BlueprintAbility MountPrimaryBlueprint = new BlueprintAbility { name = "KMC_MountPrimary", AssetGuid = "mp" };
        internal static readonly BlueprintAbility MountedChargeBlueprint = new BlueprintAbility { name = "KMC_MountedCharge", AssetGuid = "mc" };
        internal static readonly BlueprintAbility StockChargeBlueprint = new BlueprintAbility { name = "Charge", AssetGuid = "charge" };

        private readonly BlueprintAbility mountStateAbility = MountStateBlueprint;
        private readonly FixtureLogger logger = new FixtureLogger();
        internal readonly GameMountedRelationshipService relationship = new GameMountedRelationshipService();
        private readonly List<HotbarSerializationLease> hotbarSerializationLeases = new List<HotbarSerializationLease>();
        private readonly List<InitialPlacementObservation> initialPlacements = new List<InitialPlacementObservation>();
        private bool disposed = false, enabled, serializationSuspended, restoringSlots;
        internal Func<bool> PlacementSettled { get; set; }
        internal Func<bool> PersistentRowsSuppressed { get; set; }

        internal NativeMountedControlService() { CreateToolbarLifecycle(); }

        internal sealed class InitialPlacementObservation { internal string UnitId; internal int Index; internal int Frame; internal string[] MainBarBefore; }

        private sealed class HotbarSerializationLease
        {
            public HotbarSerializationLease(UnitEntityData unit, int index, BlueprintAbility blueprint, MechanicActionBarSlotEmpty placeholder)
            { Unit = unit; Index = index; Blueprint = blueprint; Placeholder = placeholder; }
            public UnitEntityData Unit { get; }
            public int Index { get; }
            public BlueprintAbility Blueprint { get; }
            public MechanicActionBarSlotEmpty Placeholder { get; }
        }

        internal void FixtureEnable() { enabled = true; toolbar.Enable(); Update(); }

        internal void FixtureDisable() { enabled = false; toolbar.Disable(); ClearSerializationHotbarLeases(false); }

        // The main file's Update order: housekeeping before the gates, then the lifecycle's reconciliation.
        internal void Update()
        {
            toolbar.Housekeeping();
            if (disposed || !enabled || serializationSuspended || areaSuspended) return;
            toolbar.Reconcile();
        }

        private void ClearTargetingPresentation(string reason) { }
        private void CancelPendingNativeControls(CleanupTrigger trigger) { }
        private void ReconcileRetiredPrimaryReferences(UnitEntityData unit) { }
        private NativeMountedControlKind ResolveMountStateIntent(UnitEntityData unit) => NativeMountedControlKind.MountCompanion;

        private NativeMountedControlKind ResolveKind(BlueprintAbility ability) =>
            MountedControlSlotPolicy.ResolveExactKind(ability, null, null, null, MountPrimaryBlueprint, MountedChargeBlueprint, mountStateAbility);

        private IEnumerable<BlueprintAbility> EnumerateBlueprints()
        {
            yield return MountPrimaryBlueprint;
            yield return MountedChargeBlueprint;
            yield return mountStateAbility;
        }

        // The public control is leased to an owner of a pet that names it as master.
        private bool ShouldLease(UnitEntityData unit, NativeMountedControlKind kind) =>
            kind == NativeMountedControlKind.MountState && unit?.Descriptor?.Pet != null && ReferenceEquals(unit.Descriptor.Pet.Descriptor.Master.Value, unit);

        private static void MarkBarChanged(UnitEntityData unit) => unit.UISettings?.SetDirty();

        private static void AddUnique(List<UnitEntityData> units, UnitEntityData unit)
        {
            if (unit != null && !units.Contains(unit)) units.Add(unit);
        }

        private static AbilityData ResolveStockChargeForSlot(UnitEntityData unit, MechanicActionBarSlot[] slots)
        {
            var charge = unit.Descriptor.Abilities.GetAbility(StockChargeBlueprint);
            if (charge == null || !charge.Active) return null;
            return slots.Any(slot => ReferenceEquals((slot as MechanicActionBarSlotAbility)?.Ability?.Blueprint, StockChargeBlueprint)) ? null : charge.Data;
        }
    }
}

namespace KingmakerMountedCombat.Tests
{
    using Kingmaker;
    using Kingmaker.EntitySystem.Entities;
    using Kingmaker.UI.UnitSettings;
    using Kingmaker.UnitLogic.Abilities;

    internal static class ToolbarAdapterWiringFixture
    {
        private static int passed, failed;

        private static void True(bool value, string message) { if (!value) throw new Exception(message); }

        private static void Check(string name, Action body)
        {
            try { body(); passed++; Console.WriteLine("PASS wiring: " + name); }
            catch (Exception error) { failed++; Console.WriteLine("FAIL wiring: " + name + ": " + error.Message); }
        }

        private static Player NewWorld()
        {
            var player = new Player();
            Game.Instance.Player = player;
            return player;
        }

        // A character with its persistent pet (IsPet, master = owner, pet link set).
        private static UnitEntityData Owner(string id)
        {
            var owner = new UnitEntityData(id);
            var pet = new UnitEntityData("pet-" + id);
            pet.Descriptor.IsPet = true;
            pet.Descriptor.Master = new UnitReference(owner);
            owner.Descriptor.Pet = pet;
            owner.Descriptor.PetUniqueId = pet.UniqueId;
            return owner;
        }

        private static MechanicActionBarSlotAbility Binding(UnitEntityData unit, Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility blueprint, AbilityData data = null) =>
            new MechanicActionBarSlotAbility { Unit = unit, Ability = data ?? new AbilityData { Blueprint = blueprint, Caster = unit.Descriptor } };

        private static bool IsBlueprint(UnitEntityData unit, int index, Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility blueprint) =>
            ReferenceEquals((unit.UISettings.Slots[index] as MechanicActionBarSlotAbility)?.Ability?.Blueprint, blueprint);

        private static int[] Controls(UnitEntityData unit) => Enumerable.Range(0, unit.UISettings.Slots.Length)
            .Where(index => IsBlueprint(unit, index, NativeMountedControlService.MountStateBlueprint)).ToArray();

        public static int Main()
        {
            Check("a remote owner in pair custody and a detached owner: save and disable carry their slots and sanitize their pets", () =>
            {
                foreach (var boundary in new[] { "save", "disable" })
                {
                    var player = NewWorld();
                    var service = new NativeMountedControlService();
                    var remote = Owner("Remote");
                    var detached = Owner("Detached");
                    player.RemoteCompanions.Add(new UnitReference(remote));
                    player.DetachedPartyCharacters.Add(new UnitReference(detached));
                    service.relationship.Ownership.Owners.Add(new PairOwner { Rider = remote });
                    foreach (var owner in new[] { remote, detached })
                    {
                        owner.UISettings.SetSlot(Binding(owner, NativeMountedControlService.MountStateBlueprint), 5);
                        var pet = owner.Descriptor.Pet;
                        pet.UISettings.SetSlot(Binding(pet, NativeMountedControlService.MountPrimaryBlueprint), 2);
                        pet.AutoUseAbility = new AbilityData { Blueprint = NativeMountedControlService.MountPrimaryBlueprint };
                    }
                    service.FixtureEnable();
                    if (boundary == "save")
                    {
                        var rows = service.CapturePersistentSlots(false, true);
                        foreach (var owner in new[] { remote, detached })
                            True(rows.Count(row => row.ActorId == owner.UniqueId && row.Kind == MountedControlSlotPolicy.SavedMountState && row.Index == 5) == 1 &&
                                rows.Count(row => row.ActorId == owner.UniqueId && row.Kind == MountedControlSlotPolicy.SavedPlacementRecord) == 1,
                                owner.UniqueId + ": the archive does not carry slot 5 and one record.");
                        True(service.BeginSaveSerializationScope(), "The save scope did not begin.");
                    }
                    else service.FixtureDisable();
                    foreach (var owner in new[] { remote, detached })
                    {
                        var pet = owner.Descriptor.Pet;
                        True(!IsBlueprint(owner, 5, NativeMountedControlService.MountStateBlueprint), boundary + ": " + owner.UniqueId + " kept its KMC binding.");
                        True(!IsBlueprint(pet, 2, NativeMountedControlService.MountPrimaryBlueprint) && pet.AutoUseAbility == null,
                            boundary + ": " + pet.UniqueId + " kept a managed slot or auto-use.");
                    }
                    if (boundary == "save") service.EndSaveSerializationScope();
                }
            });
            Check("native GetSlot through the service brackets: an emptied chosen slot returns once; a removal stays", () =>
            {
                var player = NewWorld();
                var service = new NativeMountedControlService();
                var rider = Owner("Rider");
                player.PartyCharacters.Add(new UnitReference(rider));
                service.FixtureEnable();
                True(Controls(rider).SequenceEqual(new[] { 0 }), "The automatic placement did not run.");
                var slots = rider.UISettings.Slots;
                slots[9] = slots[0]; slots[0] = null;
                service.Update();
                var fact = rider.Descriptor.Abilities.GetAbility(NativeMountedControlService.MountStateBlueprint);
                fact.Active = false;
                // The Harmony prefix and postfix around native GetSlot(9), which empties the bad slot with an empty slot bound
                // to the unit.
                service.BeginNativeBadSlotCleanup(rider.UISettings, 9);
                rider.UISettings.SetSlot(new MechanicActionBarSlotEmpty { Unit = rider }, 9);
                service.EndNativeBadSlotCleanup(rider.UISettings);
                fact.Active = true;
                service.Update();
                True(Controls(rider).SequenceEqual(new[] { 9 }), "The emptied slot did not return once: " + string.Join(",", Controls(rider)));
                slots[9] = null;
                service.Update(); service.Update();
                True(Controls(rider).Length == 0, "A removal came back.");
            });
            Check("a native slot operation that threw is released by the main menu and by disable, in service and lifecycle", () =>
            {
                foreach (var exit in new[] { "main menu", "disable" })
                {
                    var player = NewWorld();
                    var service = new NativeMountedControlService();
                    var rider = Owner("Rider");
                    player.PartyCharacters.Add(new UnitReference(rider));
                    service.FixtureEnable();
                    service.BeginNativeBadSlotCleanup(rider.UISettings, 0);
                    True(service.HoldsNativeOperationUnit, "The prefix did not open an operation.");
                    if (exit == "main menu") { Game.Instance.Player = null; service.Update(); }
                    else service.FixtureDisable();
                    True(!service.HoldsNativeOperationUnit, exit + ": the thrown operation's unit is still held.");
                }
            });
            Check("save first on an older reserve bar migrates its Mounted Charge to the stock Charge at its index", () =>
            {
                var player = NewWorld();
                var service = new NativeMountedControlService();
                var rider = Owner("Reserve");
                player.RemoteCompanions.Add(new UnitReference(rider));
                rider.Descriptor.Abilities.AddFact(NativeMountedControlService.StockChargeBlueprint, null);
                rider.UISettings.SetSlot(Binding(rider, NativeMountedControlService.MountedChargeBlueprint), 8);
                service.FixtureEnable();
                // Enabling runs one update; the older bar is restored again as a cold load would leave it.
                rider.UISettings.SetSlot(Binding(rider, NativeMountedControlService.MountedChargeBlueprint), 8);
                service.CapturePersistentSlots(false, true);
                True(service.BeginSaveSerializationScope(), "The save scope did not begin.");
                True(IsBlueprint(rider, 8, NativeMountedControlService.StockChargeBlueprint), "Serialization lost the Charge shortcut at 8.");
                service.EndSaveSerializationScope();
                True(IsBlueprint(rider, 8, NativeMountedControlService.StockChargeBlueprint), "The release lost the Charge shortcut at 8.");
            });
            Check("archive rows naming a party pet own no arrangement and are not saved again; the rider's rows are", () =>
            {
                var player = NewWorld();
                var service = new NativeMountedControlService();
                var rider = Owner("Rider");
                player.PartyCharacters.Add(new UnitReference(rider));
                // A loaded world restores its rows before any automatic placement (the setting off keeps it out of the way).
                Kingmaker.UI.SettingsUI.SettingsRoot.Instance.AutofillActionbarSlots.CurrentValue = false;
                service.FixtureEnable();
                var pet = rider.Descriptor.Pet;
                service.RestorePersistentSlots(new[]
                {
                    new SavedMountedSlot { ActorId = rider.UniqueId, Index = 9, Kind = MountedControlSlotPolicy.SavedMountState },
                    new SavedMountedSlot { ActorId = rider.UniqueId, Index = 0, Kind = MountedControlSlotPolicy.SavedPlacementRecord },
                    new SavedMountedSlot { ActorId = pet.UniqueId, Index = 5, Kind = MountedControlSlotPolicy.SavedMountState },
                    new SavedMountedSlot { ActorId = pet.UniqueId, Index = 0, Kind = MountedControlSlotPolicy.SavedPlacementRecord }
                }, true);
                var rows = service.CapturePersistentSlots(false, true);
                True(rows.All(row => row.ActorId != pet.UniqueId), "A pet row was saved again.");
                True(rows.Any(row => row.ActorId == rider.UniqueId && row.Index == 9 && row.Kind == MountedControlSlotPolicy.SavedMountState), "The rider's row was lost.");
            });
            Console.WriteLine("TOTAL PASS=" + passed + " FAIL=" + failed);
            return failed == 0 ? 0 : 1;
        }
    }
}
