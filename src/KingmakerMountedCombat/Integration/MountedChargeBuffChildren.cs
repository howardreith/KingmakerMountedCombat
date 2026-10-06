using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony12;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Items;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Integration
{
    // The existing lease's finite native lifetime graph. This is not a second
    // cleanup service: the controller still owns the one lease/barrier/debt.
    // Different-owner attack consequences are deliberately outside this graph.
    internal sealed class MountedChargeBuffChildren
    {
        private static readonly List<MountedChargeBuffChildren> owners = new List<MountedChargeBuffChildren>();
        [ThreadStatic] private static Node acquiring;
        private readonly MountedChargeBuffSurface surface;
        private readonly UnitEntityData rider;
        private readonly BuffCollection collection;
        private readonly Func<Buff> root;
        private readonly Func<bool> rootAcquiring;
        private readonly Action observeRootRemoval;
        private readonly Action captureRootResidue;
        private readonly ModifiableValue[] stats;
        private readonly int thread;
        private readonly List<Node> nodes = new List<Node>();
        private bool retiring;
        private string fault;
        private readonly List<string> failures = new List<string>();

        private sealed class Node
        {
            internal MountedChargeBuffChildren Graph;
            internal int Id;
            internal FactCollection Collection;
            internal BlueprintFact Blueprint;
            internal MechanicsContext ParentContext;
            internal Node Parent;
            internal ItemEntity Item;
            internal readonly MountedChargeFactOwnership<Fact> Owner = new MountedChargeFactOwnership<Fact>();
            internal readonly List<GameLogicComponent> Components = new List<GameLogicComponent>();
            internal readonly List<ModifiableValue.Modifier> Modifiers = new List<ModifiableValue.Modifier>();
            internal MountedChargeEnchantmentFx Visuals;
            internal Fact Fact => Owner.Fact;

            internal void Capture()
            {
                if (Fact == null) return;
                foreach (var component in ListField(Fact, 0x0400695A).Cast<GameLogicComponent>())
                    if (!Components.Any(item => ReferenceEquals(item, component))) Components.Add(component);
                foreach (var modifier in Graph.stats.SelectMany(stat => stat.Modifiers).Where(value => ReferenceEquals(value.Source, Fact)))
                    if (!Modifiers.Any(item => ReferenceEquals(item, modifier))) Modifiers.Add(modifier);
            }

            internal bool Settled()
            {
                if (Fact == null || Owner.Acquiring || !LifecycleSettled(Fact) ||
                    Collection.RawFacts.Any(item => ReferenceEquals(item, Fact)) || Fact.Active ||
                    (bool)Field(Fact, 0x0400695E) ||
                    Components.Any(component => ReferenceEquals(component, null) || component.IsListeningEvents) ||
                    Graph.stats.Any(stat => stat.Modifiers.Any(modifier => ReferenceEquals(modifier.Source, Fact))) ||
                    Modifiers.Any(modifier => !ReferenceEquals(modifier.AppliedTo, null) ||
                        Graph.stats.Any(stat => stat.Modifiers.Any(value => ReferenceEquals(value, modifier))))) return false;
                if (Fact is Buff)
                    return Fact.IsDisposed && !ListField(Fact, 0x0400695A).Any() && Field(Fact, 0x0400695B) == null &&
                        !ListField(Fact, 0x04001B6D).Any() && !ListField(Fact, 0x04001B6E).Any();
                // Native ItemEnchantment.Dispose does not call base Fact.Dispose.
                // Components and IsDisposed are deliberately not buff-shaped here.
                return Fact is ItemEnchantment && Field(Fact, 0x040068FE) == null &&
                    Field(Fact, 0x040068FF) == null && Visuals != null && Visuals.Drained;
            }

            internal bool Remove()
            {
                // The root's deactivation may lawfully remove a settled child, but
                // it may not unlink a child whose own callback can still resume.
                if (Owner.Acquiring || Fact == null || !LifecycleSettled(Fact)) return false;
                Capture();
                Action<Fact> remove = fact =>
                {
                    if (fact is ItemEnchantment enchantment) Item.RemoveEnchantment(enchantment);
                    else Collection.RemoveFact(fact);
                };
                try { Owner.TryRemove(remove, fact => Settled()); }
                finally { Visuals?.TryDrain(); }
                return Owner.TryRemove(remove, fact => Settled());
            }
        }

        internal MountedChargeBuffChildren(MountedChargeBuffSurface surface, UnitEntityData rider, BuffCollection collection,
            Func<Buff> root, Func<bool> rootAcquiring, Action observeRootRemoval, Action captureRootResidue)
        {
            this.surface = surface ?? throw new ArgumentNullException(nameof(surface));
            this.rider = rider ?? throw new ArgumentNullException(nameof(rider));
            this.collection = collection ?? throw new ArgumentNullException(nameof(collection));
            this.root = root ?? throw new ArgumentNullException(nameof(root));
            this.rootAcquiring = rootAcquiring ?? throw new ArgumentNullException(nameof(rootAcquiring));
            this.observeRootRemoval = observeRootRemoval ?? throw new ArgumentNullException(nameof(observeRootRemoval));
            this.captureRootResidue = captureRootResidue ?? throw new ArgumentNullException(nameof(captureRootResidue));
            stats = rider.Descriptor.Stats.GetList().ToArray();
            thread = System.Threading.Thread.CurrentThread.ManagedThreadId;
            owners.Add(this);
        }

        internal bool ScopeSettled => System.Threading.Thread.CurrentThread.ManagedThreadId == thread && !rootAcquiring() &&
            (root() == null || LifecycleSettled(root())) && nodes.All(node => !node.Owner.Acquiring &&
                (node.Fact == null || LifecycleSettled(node.Fact)) && (node.Visuals == null || node.Visuals.ScopeSettled));
        internal bool Drained => fault == null && ScopeSettled && nodes.All(node => node.Owner.Drained);

        internal bool TryDrain()
        {
            retiring = true;
            if (!ScopeSettled) return false;
            // Independent child/enchantment cleanup still runs when a parent's native
            // callback failed before StoreFact or before its component fields were set.
            foreach (var node in nodes.AsEnumerable().Reverse())
            {
                try { node.Remove(); }
                catch (Exception error)
                {
                    if (failures.Count < 32) failures.Add("child-removal:" + node.Id + ":" + error.GetType().Name);
                }
            }
            return Drained;
        }

        internal void Release()
        {
            if (!Drained) throw new InvalidOperationException("Live Charge child ownership cannot be released.");
            foreach (var node in nodes) node.Visuals?.Release();
            owners.Remove(this);
        }

        // AddBuff clones its input before AddBuffInternal: at these two hook sites
        // the exact clone's parent is our root context. Buff stores the clone itself.
        private bool RootContext(MechanicsContext context) => root() != null && context != null &&
            ReferenceEquals(root().Context, context.ParentContext);

        private Node Begin(FactCollection source, BlueprintFact blueprint, MechanicsContext context)
        {
            Node parent = null;
            ItemEntity item = null;
            if (ReferenceEquals(source, collection) && RootContext(context))
            {
                if (!surface.Children.Any(child => ReferenceEquals(child, blueprint)))
                    return Reject("unknown same-rider root child");
            }
            else if (source is ItemEnchantmentCollection enchantments)
            {
                parent = nodes.FirstOrDefault(node => node.Fact is Buff buff &&
                    node.Blueprint.AssetGuid == MountedChargeBuffSurface.HellfireGuid && ReferenceEquals(buff.Context, context));
                if (parent == null) return null;
                item = enchantments.Owner;
                if (!ReferenceEquals(item?.Wielder?.Unit, rider) ||
                    !surface.Enchantments.Any(enchantment => ReferenceEquals(enchantment, blueprint)))
                    return Reject("unexpected Hellfire weapon or enchantment");
                if (source.RawFacts.Any(fact => ReferenceEquals(fact.Blueprint, blueprint)))
                    return Reject("existing foreign enchantment cannot be acquired");
            }
            else return null; // Includes the opponent's lawful Frightful Shaken.
            if (retiring || nodes.Count >= 16 || System.Threading.Thread.CurrentThread.ManagedThreadId != thread)
                return Reject("late, unbounded or foreign-thread child acquisition");
            var created = new Node { Graph = this, Id = nodes.Count + 1, Collection = source,
                Blueprint = blueprint, ParentContext = context, Parent = parent, Item = item };
            nodes.Add(created); // Custody precedes every possible native acquisition.
            return created;
        }

        private Node Reject(string reason)
        {
            fault = reason;
            throw new InvalidOperationException("Mounted Charge child ownership: " + reason + ".");
        }

        // Wrappers replace only two token-pinned native calls. Every unowned call
        // invokes the original method unchanged; the finally scope survives throws.
        internal static Fact AddBuffFact(BuffCollection source, BlueprintFact blueprint, MechanicsContext context) =>
            AddFact(source, blueprint, context, () => source.AddFact(blueprint, context));

        internal static Fact AddEnchantmentFact(ItemEnchantmentCollection source, BlueprintFact blueprint, MechanicsContext context) =>
            AddFact(source, blueprint, context, () => source.AddFact(blueprint, context));

        private static Fact AddFact(FactCollection source, BlueprintFact blueprint, MechanicsContext context, Func<Fact> native)
        {
            Node node = null;
            foreach (var owner in owners.ToArray())
            {
                var candidate = owner.Begin(source, blueprint, context);
                if (candidate == null) continue;
                if (node != null) throw new InvalidOperationException("Ambiguous Charge child owner.");
                node = candidate;
            }
            var previous = acquiring;
            acquiring = node;
            try
            {
                // Shadow even an unowned reentrant call. It must not inherit its
                // caller's pending ownership merely because it uses that collection.
                if (node == null) return native();
                node.Owner.Acquire(native);
                return node.Fact;
            }
            finally { acquiring = previous; node?.Capture(); }
        }

        internal static void Created(FactCollection source, Fact fact)
        {
            var node = acquiring;
            if (node == null || !ReferenceEquals(source, node.Collection)) return;
            var context = fact is Buff buff ? buff.Context :
                fact is ItemEnchantment ? Field(fact, 0x040068FE) : null;
            if (fact == null || !ReferenceEquals(fact.Blueprint, node.Blueprint) || !ReferenceEquals(context, node.ParentContext))
                node.Graph.Reject("created child identity differs");
            node.Owner.CaptureCreated(fact);
            if (fact is ItemEnchantment enchantment) node.Visuals = new MountedChargeEnchantmentFx(enchantment);
        }

        // This prefix precedes AddBuffInternal's destructive Replace lookup. The
        // availability preflight is repeated here to cover reentrant foreign adds.
        internal static void BeforeAddBuff(BuffCollection source, BlueprintBuff blueprint, MechanicsContext context)
        {
            foreach (var owner in owners.ToArray())
                if (ReferenceEquals(source, owner.collection) && owner.RootContext(context) &&
                    source.RawFacts.Any(fact => ReferenceEquals(fact.Blueprint, blueprint)))
                    owner.Reject("native Replace would remove a preexisting child");
        }

        internal static void BeforeFactLifecycle(Fact fact)
        {
            foreach (var owner in owners.ToArray())
            {
                if (ReferenceEquals(owner.root(), fact)) owner.captureRootResidue();
                foreach (var node in owner.nodes.Where(node => ReferenceEquals(node.Fact, fact))) node.Capture();
            }
        }

        internal static void BeforeRemove(FactCollection source, Fact fact)
        {
            foreach (var owner in owners.ToArray())
            {
                if (ReferenceEquals(source, owner.collection) && ReferenceEquals(fact, owner.root()))
                {
                    owner.retiring = true;
                    owner.captureRootResidue();
                    owner.observeRootRemoval();
                }
                foreach (var node in owner.nodes.Where(node => ReferenceEquals(node.Collection, source) && ReferenceEquals(node.Fact, fact)))
                {
                    node.Capture();
                    node.Owner.ObserveNativeRemoval(fact);
                }
            }
        }

        internal static void BeforeRemoveBlueprint(FactCollection source, BlueprintFact blueprint)
        {
            if (owners.Count == 0) return;
            foreach (var fact in source.RawFacts.Where(fact => ReferenceEquals(fact.Blueprint, blueprint)).ToArray())
                BeforeRemove(source, fact);
        }

        internal static bool RemoveAction(ContextActionRemoveBuff action)
        {
            if (owners.Count == 0) return true;
            var active = ElementsContext.GetData<Buff.Data>()?.Buff;
            if (active == null || !owners.Any(owner => ReferenceEquals(owner.root(), active))) return true;
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var context = typeof(ContextAction).GetProperty("Context", flags).GetValue(action, null);
            var target = (Kingmaker.Utility.TargetWrapper)typeof(ContextAction).GetProperty("Target", flags).GetValue(action, null);
            foreach (var owner in owners.ToArray())
            {
                if (!ReferenceEquals(owner.root(), active) || !ReferenceEquals(context, active?.Context)) continue;
                if (!owner.surface.RemovalActions.Any(candidate => ReferenceEquals(candidate, action))) continue;
                if (!ReferenceEquals(target?.Unit, owner.rider) || action.ToCaster)
                    owner.Reject("native child removal target changed");
                // Preserve the two known actions' intent using retained instances.
                // Never call their native blueprint-wide removal on foreign facts.
                foreach (var node in owner.nodes.Where(node => ReferenceEquals(node.Blueprint, action.Buff)).ToArray())
                    node.Remove();
                return false;
            }
            return true;
        }

        internal static IEnumerable<CodeInstruction> WrapAddFact(IEnumerable<CodeInstruction> source, MethodBase original)
        {
            if (original.MetadataToken != 0x060029F7 && original.MetadataToken != 0x060099B8)
                throw new InvalidOperationException("Unpinned Charge child acquisition site.");
            var code = source.ToList();
            // Preserve OwnedFactCollection<T>.AddFact, including its post-add gain
            // event. Calling the base FactCollection implementation would drop it.
            var calls = code.Where(instruction => instruction.operand is MethodInfo method &&
                method.DeclaringType.IsGenericType && method.DeclaringType.GetGenericTypeDefinition() == typeof(OwnedFactCollection<>) &&
                method.MetadataToken == 0x060096AE).ToArray();
            if (calls.Length != 1) throw new InvalidOperationException("Native child acquisition call count differs.");
            calls[0].opcode = OpCodes.Call;
            calls[0].operand = typeof(MountedChargeBuffChildren).GetMethod(original.MetadataToken == 0x060029F7 ?
                nameof(AddBuffFact) : nameof(AddEnchantmentFact), BindingFlags.Static | BindingFlags.NonPublic);
            return code;
        }

        private static object Field(Fact fact, int token) => typeof(Fact).Module.ResolveField(token).GetValue(fact);
        private static IEnumerable<object> ListField(Fact fact, int token) =>
            (Field(fact, token) as IEnumerable)?.Cast<object>() ?? Enumerable.Empty<object>();
        internal static bool LifecycleSettled(Fact fact) => fact != null &&
            !(bool)Field(fact, 0x04006962) && !(bool)Field(fact, 0x04006963) && !(bool)Field(fact, 0x04006964);

        internal JObject CaptureEvidence() => new JObject
        {
            ["schema"] = 1, ["rider"] = rider.UniqueId,
            ["rootIdentity"] = root() == null ? 0 : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(root()),
            ["rootCollection"] = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(collection),
            ["surface"] = surface.Id, ["retiring"] = retiring, ["fault"] = fault, ["scopeSettled"] = ScopeSettled,
            ["failures"] = new JArray(failures),
            ["drained"] = Drained, ["facts"] = new JArray(nodes.Select(node => new JObject
            {
                ["id"] = node.Id, ["parent"] = node.Parent?.Id ?? 0, ["blueprint"] = node.Blueprint.AssetGuid,
                ["kind"] = node.Item == null ? "buff" : "enchantment", ["created"] = node.Fact != null,
                ["acquiring"] = node.Owner.Acquiring, ["acquired"] = node.Owner.Acquired,
                ["identityUncertain"] = node.Owner.IdentityUncertain,
                ["removalAttempted"] = node.Owner.RemovalAttempted, ["fault"] = node.Owner.Fault,
                ["settled"] = node.Settled(), ["drained"] = node.Owner.Drained,
                ["native"] = CaptureNativeFact(node.Fact, node.Collection, node.Components, stats, node.Modifiers),
                ["visuals"] = node.Visuals?.CaptureEvidence()
            }))
        };

        internal static JObject CaptureNativeFact(Fact fact, FactCollection source, IEnumerable<GameLogicComponent> components,
            IEnumerable<ModifiableValue> stats, IEnumerable<ModifiableValue.Modifier> modifiers)
        {
            if (fact == null) return null;
            var nativeStats = stats.ToArray();
            return new JObject
            {
                ["identity"] = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(fact),
                ["collection"] = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(source),
                ["inCollection"] = source.RawFacts.Any(item => ReferenceEquals(item, fact)),
                ["active"] = fact.Active, ["disposed"] = fact.IsDisposed, ["turnedOn"] = (bool)Field(fact, 0x0400695E),
                ["activating"] = (bool)Field(fact, 0x04006962), ["deactivating"] = (bool)Field(fact, 0x04006963),
                ["recalculating"] = (bool)Field(fact, 0x04006964),
                ["listening"] = components.Count(component => !ReferenceEquals(component, null) && component.IsListeningEvents),
                ["statModifiers"] = nativeStats.Sum(stat => stat.Modifiers.Count(modifier => ReferenceEquals(modifier.Source, fact))),
                ["attachedModifiers"] = modifiers.Count(modifier => !ReferenceEquals(modifier.AppliedTo, null) ||
                    nativeStats.Any(stat => stat.Modifiers.Any(item => ReferenceEquals(item, modifier)))),
                ["componentCount"] = ListField(fact, 0x0400695A).Count(),
                ["componentData"] = Field(fact, 0x0400695B) != null,
                ["storedFacts"] = fact is Buff ? ListField(fact, 0x04001B6E).Count() : 0,
                ["storedModifiers"] = fact is Buff ? ListField(fact, 0x04001B6D).Count() : 0,
                ["parentContext"] = fact is ItemEnchantment && Field(fact, 0x040068FE) != null,
                ["currentContext"] = fact is ItemEnchantment && Field(fact, 0x040068FF) != null
            };
        }
    }
}
