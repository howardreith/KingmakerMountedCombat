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
            UISettings.Owner = Descriptor;
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
        // UnitUISettings.Owner: the descriptor whose bar this is (installed contract: CleanupBadSlots binds through it).
        public UnitDescriptor Owner { get; set; }
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
        // The older Mount / Dismount controls (supported aliases of the public control).
        internal static readonly BlueprintAbility MountBlueprint = new BlueprintAbility { name = "KMC_Mount", AssetGuid = "mount" };
        internal static readonly BlueprintAbility DismountBlueprint = new BlueprintAbility { name = "KMC_Dismount", AssetGuid = "dismount" };
        // A rider that gets the Mounted Charge command (Charge enabled on a mounted rider).
        internal bool LeaseCharge;

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

        // Enabled without a first update (a native operation can precede the first scan).
        internal void FixtureEnableWithoutUpdate() { enabled = true; toolbar.Enable(); }

        // The production ToolbarHost's public-binding predicate, reached directly (this substitute is part of the service).
        internal bool FixtureHostHoldsLiveBinding(UnitEntityData unit, int index) => new ToolbarHost(this).HoldsLiveBinding(unit, index);

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
            MountedControlSlotPolicy.ResolveExactKind(ability, MountBlueprint, DismountBlueprint, null, MountPrimaryBlueprint, MountedChargeBlueprint, mountStateAbility);

        private IEnumerable<BlueprintAbility> EnumerateBlueprints()
        {
            yield return MountPrimaryBlueprint;
            yield return MountedChargeBlueprint;
            yield return mountStateAbility;
        }

        // The public control (and, with LeaseCharge, the Mounted Charge command) is leased to an owner of a pet that names it
        // as master.
        private bool ShouldLease(UnitEntityData unit, NativeMountedControlKind kind) =>
            (kind == NativeMountedControlKind.MountState || LeaseCharge && kind == NativeMountedControlKind.MountedCharge) &&
            unit?.Descriptor?.Pet != null && ReferenceEquals(unit.Descriptor.Pet.Descriptor.Master.Value, unit);

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

        // Native IsBad for an ability slot, as the fixture models it: the bar owner holds no active fact with this data.
        private static bool IsBad(UnitEntityData owner, MechanicActionBarSlot slot)
        {
            var ability = (slot as MechanicActionBarSlotAbility)?.Ability;
            return ability != null && !owner.Descriptor.Abilities.Enumerable.Any(fact => fact.Active && ReferenceEquals(fact.Data, ability));
        }

        // Native GetSlot(index, unit) through the service's prefix and postfix (the Harmony forwarders pass the unit argument
        // and the prefix's __state): a bad slot becomes an empty slot bound to the unit argument. throws: no postfix.
        private static void NativeGetSlot(NativeMountedControlService service, UnitUISettings settings, int index, UnitEntityData unit, bool throws = false)
        {
            var operation = service.BeginNativeBadSlotCleanup(settings, index, unit);
            if (IsBad(settings.Owner?.Unit ?? unit, settings.Slots[index])) settings.SetSlot(new MechanicActionBarSlotEmpty { Unit = unit }, index);
            if (!throws) service.EndNativeBadSlotCleanup(operation);
        }

        // Native CleanupBadSlots through the service's prefix and postfix: each bad ability slot is rebound to the owner's
        // active fact of its blueprint (a new slot with that fact's data), or emptied, both bound to Owner.Unit. Each write
        // is reported to the service as MountedPatchController's transpiled SetSlot call sites report it (a labeled mirror of
        // NativeCleanupSetSlot). throwAfter: native threw after that many writes (no postfix).
        private static void NativeCleanup(NativeMountedControlService service, UnitUISettings settings, int throwAfter = -1)
        {
            var operation = service.BeginNativeBadSlotCleanup(settings);
            var owner = settings.Owner.Unit;
            var writes = 0;
            for (var index = 0; index < settings.Slots.Length; index++)
            {
                var slot = settings.Slots[index];
                if (!IsBad(owner, slot)) continue;
                if (writes == throwAfter) return;
                var blueprint = ((MechanicActionBarSlotAbility)slot).Ability.Blueprint;
                var fact = owner.Descriptor.Abilities.Enumerable.FirstOrDefault(item => ReferenceEquals(item.Blueprint, blueprint) && item.Active);
                settings.SetSlot(fact == null ? (MechanicActionBarSlot)new MechanicActionBarSlotEmpty { Unit = owner }
                    : new MechanicActionBarSlotAbility { Unit = owner, Ability = fact.Data }, index);
                service.RecordNativeCleanupWrite(settings, index);
                writes++;
            }
            if (throwAfter < 0) service.EndNativeBadSlotCleanup(operation);
        }

        private static int[] Rows(SavedMountedSlot[] rows, string actorId, int kind) =>
            rows.Where(row => row.ActorId == actorId && row.Kind == kind).Select(row => row.Index).OrderBy(index => index).ToArray();

        // The owned archive's row validation (MountedArchiveData.ValidateSlots: the row bound, valid rows, and unique keys from
        // the production MountedControlSlotPolicy.SavedRowKey); the fixture's actor ids stand for the Guid keys.
        private static void ValidArchive(SavedMountedSlot[] rows, string message)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            True(MountedControlSlotPolicy.FitsArchive(rows.Length) && rows.All(row => MountedControlSlotPolicy.IsValidSavedRow(row.Index, row.Kind) &&
                keys.Add(MountedControlSlotPolicy.SavedRowKey(row.ActorId, row.Index, row.Kind))),
                message + ": the owned archive would reject these rows: " + string.Join(",", rows.Select(row => row.ActorId + ":" + row.Index + "/" + row.Kind)));
        }

        private static void Autofill(bool on) => Kingmaker.UI.SettingsUI.SettingsRoot.Instance.AutofillActionbarSlots.CurrentValue = on;

        // A rider arranged at slot (the automatic placement at 0 moved there, then one update).
        private static UnitEntityData ArrangedRider(Player player, NativeMountedControlService service, string id, int slot)
        {
            Autofill(true);
            var rider = Owner(id);
            player.PartyCharacters.Add(new UnitReference(rider));
            service.FixtureEnable();
            var slots = rider.UISettings.Slots;
            True(Controls(rider).SequenceEqual(new[] { 0 }), id + ": the automatic placement did not run.");
            slots[slot] = slots[0]; slots[0] = null;
            service.Update();
            True(Controls(rider).SequenceEqual(new[] { slot }), id + ": not arranged at " + slot + ".");
            return rider;
        }

        private static Ability Lease(UnitEntityData unit, Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility blueprint) =>
            (Ability)unit.Descriptor.Abilities.AddFact(blueprint, null);

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
                var operation = service.BeginNativeBadSlotCleanup(rider.UISettings, 9, rider);
                rider.UISettings.SetSlot(new MechanicActionBarSlotEmpty { Unit = rider }, 9);
                service.EndNativeBadSlotCleanup(operation);
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
                    service.BeginNativeBadSlotCleanup(rider.UISettings, 0, rider);
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
            var chargeKind = (int)NativeMountedControlKind.MountedCharge;
            Check("review R1-F10 a former rider object kept by pair custody authors no row in in-world or owned captures; its managed control never collides", () =>
            {
                var player = NewWorld();
                var service = new NativeMountedControlService();
                var former = ArrangedRider(player, service, "A", 9);
                // The former object stays in pair custody; its replacement (same actor id, same companion id) is in the party
                // and places the live control at 11. Captures run before any update.
                player.PartyCharacters.Clear();
                service.relationship.Ownership.Owners.Add(new PairOwner { Rider = former });
                var current = Owner("A");
                player.PartyCharacters.Add(new UnitReference(current));
                var fact = Lease(current, NativeMountedControlService.MountStateBlueprint);
                current.UISettings.SetSlot(Binding(current, NativeMountedControlService.MountStateBlueprint, fact.Data), 11);
                // The former object also holds a callable managed control at the replacement's index 11.
                service.LeaseCharge = true;
                var charge = Lease(former, NativeMountedControlService.MountedChargeBlueprint);
                former.UISettings.SetSlot(Binding(former, NativeMountedControlService.MountedChargeBlueprint, charge.Data), 11);
                foreach (var owned in new[] { false, true })
                {
                    var label = owned ? "owned save" : "in-world capture (area transfer, retained remount)";
                    var rows = service.CapturePersistentSlots(false, owned);
                    True(Rows(rows, "A", MountedControlSlotPolicy.SavedMountState).SequenceEqual(new[] { 11 }) &&
                        Rows(rows, "A", MountedControlSlotPolicy.SavedPlacementRecord).Length == 1,
                        label + ": not 11 once with one marker: " + string.Join(",", rows.Select(row => row.ActorId + ":" + row.Index + "/" + row.Kind)));
                    True(Rows(rows, "A", chargeKind).Length == 0, label + ": the former object's managed control was written.");
                    ValidArchive(rows, label);
                }
            });
            Check("review R3-F1 an in-world capture before reconciliation records a callable Mounted Charge at the former public slot as Charge only", () =>
            {
                foreach (var moved in new[] { false, true })
                {
                    var player = NewWorld();
                    var service = new NativeMountedControlService();
                    var rider = ArrangedRider(player, service, "R", 9);
                    service.LeaseCharge = true;
                    var charge = Lease(rider, NativeMountedControlService.MountedChargeBlueprint);
                    var publicSlot = rider.UISettings.Slots[9];
                    rider.UISettings.SetSlot(Binding(rider, NativeMountedControlService.MountedChargeBlueprint, charge.Data), 9);
                    if (moved) rider.UISettings.SetSlot(publicSlot, 11);
                    var label = moved ? "moved to 11" : "removed";
                    var rows = service.CapturePersistentSlots();
                    True(Rows(rows, "R", chargeKind).SequenceEqual(new[] { 9 }), label + ": Charge at 9 was not recorded once.");
                    True(Rows(rows, "R", MountedControlSlotPolicy.SavedMountState).SequenceEqual(moved ? new[] { 11 } : new int[0]),
                        label + ": wrong Mount / Dismount rows: " + string.Join(",", Rows(rows, "R", MountedControlSlotPolicy.SavedMountState)));
                    True(Rows(rows, "R", MountedControlSlotPolicy.SavedPlacementRecord).Length == 1, label + ": not one marker.");
                    ValidArchive(rows, label);
                    True(!service.FixtureHostHoldsLiveBinding(rider, 9), label + ": the host's public-binding predicate accepted a callable Mounted Charge.");
                    True(!moved || service.FixtureHostHoldsLiveBinding(rider, 11), label + ": the host's public-binding predicate refused the live control.");
                }
            });
            Check("review R2-F4 a thrown native write survives an unrelated complete GetSlot during the open load's missing player", () =>
            {
                var player = NewWorld();
                var service = new NativeMountedControlService();
                var rider = ArrangedRider(player, service, "R", 9);
                var stock = Lease(rider, NativeMountedControlService.StockChargeBlueprint);
                rider.UISettings.SetSlot(Binding(rider, NativeMountedControlService.StockChargeBlueprint, stock.Data), 2);
                var fact = rider.Descriptor.Abilities.GetAbility(NativeMountedControlService.MountStateBlueprint);
                fact.Active = false;
                NativeGetSlot(service, rider.UISettings, 9, rider, throws: true);
                True(rider.UISettings.Slots[9] is MechanicActionBarSlotEmpty, "The fixture's native GetSlot did not empty 9.");
                var scope = new object();
                service.BeginSelectedLoad(scope);
                Game.Instance.Player = null;
                // Complete GetSlot calls on an ordinary slot and on an empty slot: prefix and postfix both run.
                NativeGetSlot(service, rider.UISettings, 2, rider);
                NativeGetSlot(service, rider.UISettings, 5, rider);
                service.Update();
                True(service.HoldsNativeOperationUnit, "The thrown operation was dropped during the missing-player interval.");
                Game.Instance.Player = player;
                service.AbandonLoadedWorld(scope);
                fact.Active = true;
                var rows = service.CapturePersistentSlots(false, true);
                True(Rows(rows, "R", MountedControlSlotPolicy.SavedMountState).SequenceEqual(new[] { 9 }) &&
                    Rows(rows, "R", MountedControlSlotPolicy.SavedPlacementRecord).Length == 1, "The outgoing world's native emptying was not saved as 9 with one marker.");
                service.Update();
                True(Controls(rider).SequenceEqual(new[] { 9 }), "The shortcut did not return once at 9: " + string.Join(",", Controls(rider)));
                True(IsBlueprint(rider, 2, NativeMountedControlService.StockChargeBlueprint), "The ordinary slot changed.");
            });
            Check("review R3-F2 a stale slot naming a discarded rider object never hides native invalidation of the current rider's bar", () =>
            {
                // GetSlot of a stale public slot naming the discarded object, before the first scan: identified by native's
                // unit argument (also with no owner attached to the settings yet) or the settings' owner.
                foreach (var attach in new[] { "unit argument and settings owner", "unit argument only" })
                {
                    var player = NewWorld();
                    var service = new NativeMountedControlService();
                    Autofill(false);
                    var current = Owner("A");
                    var discarded = Owner("A");
                    player.PartyCharacters.Add(new UnitReference(current));
                    current.UISettings.SetSlot(Binding(discarded, NativeMountedControlService.MountStateBlueprint), 9);
                    if (attach == "unit argument only") current.UISettings.Owner = null;
                    service.FixtureEnableWithoutUpdate();
                    NativeGetSlot(service, current.UISettings, 9, current);
                    True(current.UISettings.Slots[9] is MechanicActionBarSlotEmpty, attach + ": the fixture's native GetSlot did not empty the stale slot.");
                    current.UISettings.Owner = current.Descriptor;
                    service.Update();
                    True(Controls(current).SequenceEqual(new[] { 9 }), attach + ": the current rider did not recover 9: " + string.Join(",", Controls(current)));
                    True(Controls(discarded).Length == 0, attach + ": the discarded object received a binding.");
                    var rows = service.CapturePersistentSlots(false, true);
                    True(Rows(rows, "A", MountedControlSlotPolicy.SavedMountState).SequenceEqual(new[] { 9 }), attach + ": not saved at 9.");
                }
                // A whole-bar cleanup meets a stale-owner alias at 4 before the current rider's chosen binding at 9.
                {
                    var player = NewWorld();
                    var service = new NativeMountedControlService();
                    var current = ArrangedRider(player, service, "A", 9);
                    var discarded = Owner("A");
                    current.UISettings.SetSlot(Binding(discarded, NativeMountedControlService.MountBlueprint), 4);
                    var fact = current.Descriptor.Abilities.GetAbility(NativeMountedControlService.MountStateBlueprint);
                    fact.Active = false;
                    NativeCleanup(service, current.UISettings);
                    True(current.UISettings.Slots[4] is MechanicActionBarSlotEmpty && current.UISettings.Slots[9] is MechanicActionBarSlotEmpty,
                        "The fixture's native cleanup did not empty 4 and 9.");
                    fact.Active = true;
                    service.Update();
                    True(Controls(current).SequenceEqual(new[] { 9 }), "whole bar: the chosen 9 did not return once: " + string.Join(",", Controls(current)));
                    True(!IsBlueprint(current, 4, NativeMountedControlService.MountBlueprint), "whole bar: the stale alias came back.");
                }
            });
            Check("review R2-F5 (Pro review of 74fa9931) a thrown cleanup is settled from the writes its own SetSlot calls reported", () =>
            {
                foreach (var variant in new[] { "rebind then fact deactivated", "no write then the player's placement" })
                {
                    // 3 pending with a known stale copy at 9 (GetSlot emptied 3 before any scan).
                    var player = NewWorld();
                    var service = new NativeMountedControlService();
                    Autofill(false);
                    var rider = Owner("A");
                    player.PartyCharacters.Add(new UnitReference(rider));
                    var fact = Lease(rider, NativeMountedControlService.MountStateBlueprint);
                    rider.UISettings.SetSlot(Binding(rider, NativeMountedControlService.MountStateBlueprint), 3);
                    rider.UISettings.SetSlot(Binding(rider, NativeMountedControlService.MountStateBlueprint), 9);
                    service.FixtureEnableWithoutUpdate();
                    NativeGetSlot(service, rider.UISettings, 3, rider);
                    var expected = 3;
                    if (variant.StartsWith("rebind", StringComparison.Ordinal))
                    {
                        NativeCleanup(service, rider.UISettings, throwAfter: 1);
                        True(ReferenceEquals((rider.UISettings.Slots[9] as MechanicActionBarSlotAbility)?.Ability, fact.Data), variant + ": the fixture's cleanup did not rebind 9.");
                        fact.Active = false;
                    }
                    else
                    {
                        NativeCleanup(service, rider.UISettings, throwAfter: 0);
                        rider.UISettings.SetSlot(Binding(rider, NativeMountedControlService.MountStateBlueprint, fact.Data), 9);
                        NativeGetSlot(service, rider.UISettings, 9, rider);
                        expected = 9;
                    }
                    var rows = service.CapturePersistentSlots(false, true);
                    True(Rows(rows, "A", MountedControlSlotPolicy.SavedMountState).SequenceEqual(new[] { expected }) &&
                        Rows(rows, "A", MountedControlSlotPolicy.SavedPlacementRecord).Length == 1,
                        variant + ": not saved as " + expected + " with one marker: " + string.Join(",", Rows(rows, "A", MountedControlSlotPolicy.SavedMountState)));
                    fact.Active = true;
                    service.Update(); service.Update();
                    True(Controls(rider).SequenceEqual(new[] { expected }), variant + ": not one shortcut at " + expected + ": " + string.Join(",", Controls(rider)));
                }
            });
            Check("review R2-F3 (second Pro review) a fresh placement on a never-read loaded bar survives the companion's retirement through the SetSlot boundary", () =>
            {
                var player = NewWorld();
                var service = new NativeMountedControlService();
                Autofill(false);
                var rider = Owner("A");
                player.PartyCharacters.Add(new UnitReference(rider));
                var bar = rider.UISettings.Slots;
                rider.UISettings.Slots = null;
                service.FixtureEnableWithoutUpdate();
                service.RestorePersistentSlots(new[]
                {
                    new SavedMountedSlot { ActorId = "A", Index = 9, Kind = MountedControlSlotPolicy.SavedMountState },
                    new SavedMountedSlot { ActorId = "A", Index = 0, Kind = MountedControlSlotPolicy.SavedPlacementRecord }
                }, true);
                // A is replaced by B while the bar is unreadable; the update records the retirement.
                var petB = new UnitEntityData("pet-B");
                petB.Descriptor.IsPet = true;
                petB.Descriptor.Master = new UnitReference(rider);
                rider.Descriptor.Pet = petB;
                rider.Descriptor.PetUniqueId = petB.UniqueId;
                service.Update();
                rider.UISettings.Slots = bar;
                // The player's placement of B's control at 11: the action bar UI writes it through SetSlot, whose prefix
                // forwards to the service (the Harmony forwarder is mirrored here; its shape is a contract row).
                var fact = rider.Descriptor.Abilities.GetAbility(NativeMountedControlService.MountStateBlueprint);
                True(fact != null && fact.Active, "The update did not lease B's rider a live control.");
                service.BeforeNativeSlotWrite(rider.UISettings);
                rider.UISettings.SetSlot(Binding(rider, NativeMountedControlService.MountStateBlueprint, fact.Data), 11);
                service.Update(); service.Update();
                True(Controls(rider).SequenceEqual(new[] { 11 }), "B's fresh placement was not kept once at 11: " + string.Join(",", Controls(rider)));
                var rows = service.CapturePersistentSlots(false, true);
                True(Rows(rows, "A", MountedControlSlotPolicy.SavedMountState).SequenceEqual(new[] { 11 }) &&
                    Rows(rows, "A", MountedControlSlotPolicy.SavedPlacementRecord).Length == 1, "Not saved as 11 with one marker.");
            });
            Console.WriteLine("TOTAL PASS=" + passed + " FAIL=" + failed);
            return failed == 0 ? 0 : 1;
        }
    }
}
