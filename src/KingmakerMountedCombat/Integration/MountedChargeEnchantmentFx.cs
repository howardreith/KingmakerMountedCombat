using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony12;
using Harmony12.ILCopying;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.View;
using Kingmaker.Visual.Particles;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace KingmakerMountedCombat.Integration
{
    // The inspected Flaming effect only. Native spawning, fading and pooling stay
    // authoritative. Retain acquisitions before setup, and latch pool return before
    // another user can reuse the same Unity object. Never change fade/pool state.
    internal sealed class MountedChargeEnchantmentFx
    {
        private static readonly List<MountedChargeEnchantmentFx> owners = new List<MountedChargeEnchantmentFx>();
        [ThreadStatic] private static RespawnScope current;
        private readonly ItemEnchantment fact;
        private readonly GameObject prefab;
        private readonly int thread;
        private readonly MountedChargeVisualOwnership<GameObject> lifetime = new MountedChargeVisualOwnership<GameObject>();
        private readonly List<Root> roots = new List<Root>();
        private readonly List<MountedChargeVisualOwnership<GameObject>.Acquisition> copies =
            new List<MountedChargeVisualOwnership<GameObject>.Acquisition>();
        private int scopes;
        private int pendingRoots;
        private int pendingCopies;
        private bool retiring;
        private string fault;
        private readonly List<string> failures = new List<string>();

        private sealed class Root
        {
            internal MountedChargeVisualOwnership<GameObject>.Acquisition Acquisition;
            internal SnapControllerBase[] Controllers;
            internal bool ReleaseResidueObserved;
            internal bool CustodyUncertain;
        }

        private sealed class RespawnScope
        {
            internal RespawnScope Previous;
            internal MountedChargeEnchantmentFx Owner;
            internal bool Returned;
            internal string Kind;
        }

        internal MountedChargeEnchantmentFx(ItemEnchantment fact)
        {
            this.fact = fact ?? throw new ArgumentNullException(nameof(fact));
            prefab = ((BlueprintWeaponEnchantment)fact.Blueprint).WeaponFxPrefab;
            thread = System.Threading.Thread.CurrentThread.ManagedThreadId;
            owners.Add(this);
        }

        internal bool ScopeSettled => scopes == 0 && pendingRoots == 0 && pendingCopies == 0 &&
            thread == System.Threading.Thread.CurrentThread.ManagedThreadId;
        internal bool Drained => ScopeSettled && fault == null && lifetime.Drained &&
            roots.All(root => root.ReleaseResidueObserved) && !fact.FxObject;

        internal void Release()
        {
            if (!Drained) throw new InvalidOperationException("Live Charge enchantment visuals cannot be forgotten.");
            owners.Remove(this);
        }

        internal static void ValidatePrefab(BlueprintItemEnchantment blueprint)
        {
            var weapon = blueprint as BlueprintWeaponEnchantment;
            if (weapon == null) throw new InvalidOperationException("Charge enchantment is not a native weapon enchantment.");
            if (blueprint.AssetGuid == MountedChargeBuffSurface.BurstGuid)
            {
                Require(!weapon.WeaponFxPrefab, "unexpected Burst prefab");
                return;
            }
            var value = weapon.WeaponFxPrefab;
            Require(blueprint.AssetGuid == MountedChargeBuffSurface.FlamingGuid && value && value.name == "Flaming00_WeaponBuff",
                "Flaming prefab identity");
            var components = value.GetComponentsInChildren<Component>(true);
            var particleType = typeof(FxFadeOut).Module.ResolveField(0x04000A93).FieldType.GetGenericArguments()[0];
            var expected = new Dictionary<Type, int>
            {
                [typeof(Transform)] = 9, [particleType] = 8, [particleType.Assembly.GetType("UnityEngine.ParticleSystemRenderer", true)] = 8,
                [typeof(SnapToLocator)] = 1, [typeof(PooledFx)] = 1, [typeof(FxFadeOut)] = 1,
                [typeof(WeaponParticlesSnapController)] = 5, [typeof(ParticlesMaterialController)] = 5,
                [typeof(Kingmaker.Utility.ApplyPrefabAtRuntime)] = 5
            };
            Require(components.Length == 43 && components.All(component => component && expected.ContainsKey(component.GetType())) &&
                expected.All(pair => components.Count(component => component.GetType() == pair.Key) == pair.Value), "finite prefab component inventory");
            var locator = value.GetComponent<SnapToLocator>();
            Require(locator && string.IsNullOrEmpty(locator.BoneName) && locator.BoneNames != null && locator.BoneNames.Count == 1 &&
                locator.BoneNames[0] == "Locator_WeaponCenterFX_00", "single locator; no detached clone branch");
            var fade = value.GetComponent<FxFadeOut>();
            var stopped = fade == null ? new object[0] : ((IEnumerable)Field(0x04000A9E, fade)).Cast<Object>().ToArray();
            Require(fade && fade.Duration == 1.5f && stopped.Length == 1 && !(Object)stopped[0], "native finite fade");
            Require(value.GetComponent<PooledFx>() && value.GetComponent<PooledFx>().enabled, "native pooled root");
            var controllers = components.OfType<SnapControllerBase>().ToArray();
            Require(controllers.Count(controller => (int)controller.SnapType == 0) == 2 &&
                controllers.Count(controller => (int)controller.SnapType == 1) == 3 &&
                controllers.All(controller => !(bool)Field(0x04000B02, controller) &&
                    !(Object)Field(0x04000B01, controller) && !controller.SupressMapOverride &&
                    ReferenceEquals(controller.Map, null) && string.IsNullOrEmpty(controller.Offset.WorldRotationBone)),
                "two bounded runtime-copy branches without authored actor/map ownership");
        }

        private void CaptureRoot(GameObject value)
        {
            if (!value) { fault = "root-acquisition-returned-null"; return; }
            var acquisition = lifetime.Capture(value);
            if (roots.Any(root => ReferenceEquals(root.Acquisition, acquisition))) return;
            roots.Add(new Root { Acquisition = acquisition, Controllers = value.GetComponentsInChildren<SnapControllerBase>(true)
                .Where(controller => !(bool)Field(0x04000B02, controller)).ToArray() });
            if (roots.Count > 16) fault = "root-acquisition-bound-exceeded";
        }

        private static object BeginRespawn(ItemEnchantment enchantment)
        {
            var owner = owners.FirstOrDefault(candidate => ReferenceEquals(candidate.fact, enchantment));
            if (owner != null && (owner.fault != null || owner.retiring || owner.roots.Count + owner.pendingRoots >= 16 ||
                owner.thread != System.Threading.Thread.CurrentThread.ManagedThreadId))
            {
                owner.fault = owner.fault ?? "respawn-outside-owned-capacity-thread-or-lifetime";
                throw new InvalidOperationException("Charge enchantment cannot resume across its ownership barrier.");
            }
            return BeginScope(owner, "respawn");
        }

        private static object BeginController(SnapControllerBase controller) => BeginScope(owners.FirstOrDefault(candidate =>
            candidate.roots.Any(root => !root.Acquisition.Returned && !root.CustodyUncertain &&
                root.Controllers.Any(item => ReferenceEquals(item, controller)))), "controller-init");

        private static object BeginRelease(GameObject value) => BeginScope(owners.FirstOrDefault(candidate =>
            candidate.roots.Any(root => !root.Acquisition.Returned && !root.CustodyUncertain &&
                ReferenceEquals(root.Acquisition.Value, value))), "pool-release");

        private static object BeginScope(MountedChargeEnchantmentFx owner, string kind)
        {
            var scope = new RespawnScope { Previous = current, Owner = owner, Kind = kind };
            current = scope; // Also shadow unowned nested respawns.
            if (owner != null)
            {
                owner.scopes++;
                if (kind == "respawn") owner.pendingRoots++; // Reserve before any reentrant native callback.
            }
            return scope;
        }

        private static void Returned(object value) { ((RespawnScope)value).Returned = true; }
        private static void EndRespawn(object value)
        {
            var scope = (RespawnScope)value;
            current = scope.Previous;
            if (scope.Owner == null) return;
            scope.Owner.scopes--;
            if (scope.Kind == "respawn") scope.Owner.pendingRoots--;
            if (!scope.Returned && scope.Owner.failures.Count < 32) scope.Owner.failures.Add("native-" + scope.Kind + "-did-not-return");
        }

        internal static GameObject InstantiateRoot(GameObject original, Vector3 position, Quaternion rotation)
        {
            var owner = current?.Owner;
            if (owner == null || current.Kind != "respawn" || !ReferenceEquals(original, owner.prefab))
                return Object.Instantiate(original, position, rotation);
            try
            {
                var value = Object.Instantiate(original, position, rotation);
                owner.CaptureRoot(value);
                return value;
            }
            catch { owner.fault = "native-root-acquisition-unconfirmed"; throw; }
        }

        internal static PooledGameObject Dequeue(Queue<PooledGameObject> queue)
        {
            var value = queue.Dequeue();
            var owner = current?.Owner;
            if (owner != null && current.Kind == "respawn" && value && value.Prefab && ReferenceEquals(value.Prefab.gameObject, owner.prefab))
                owner.CaptureRoot(value.gameObject);
            return value;
        }

        internal static GameObject InstantiateCopy(GameObject original, Transform parent, SnapControllerBase creator)
        {
            var owner = owners.FirstOrDefault(candidate => candidate.roots.Any(root => !root.Acquisition.Returned && !root.CustodyUncertain &&
                root.Controllers.Any(controller => ReferenceEquals(controller, creator))));
            if (owner == null) return Object.Instantiate(original, parent);
            owner.RequireCopyCapacity();
            if (!ReferenceEquals(creator.gameObject, original) || (int)creator.SnapType != 0)
            {
                owner.fault = owner.fault ?? "unexpected-native-copy-acquisition";
                throw new InvalidOperationException("Charge FX copy escaped its inspected native scope.");
            }
            owner.pendingCopies++;
            try
            {
                var value = Object.Instantiate(original, parent);
                owner.copies.Add(owner.lifetime.Capture(value));
                if (owner.copies.Count > owner.roots.Count * 2) owner.fault = "native-copy-bound-exceeded";
                return value;
            }
            catch { owner.fault = owner.fault ?? "native-copy-acquisition-unconfirmed"; throw; }
            finally { owner.pendingCopies--; }
        }

        private void RequireCopyCapacity()
        {
            if (fault == null && !retiring && copies.Count + pendingCopies < roots.Count * 2) return;
            fault = fault ?? "native-copy-outside-owned-capacity-or-lifetime";
            throw new InvalidOperationException("Charge FX copy cannot exceed its retained native acquisition capacity.");
        }

        internal static void ObserveRelease(GameObject value)
        {
            if (owners.Count == 0 || !value) return;
            foreach (var owner in owners.ToArray())
                foreach (var root in owner.roots.Where(root => !root.Acquisition.Returned && !root.CustodyUncertain && ReferenceEquals(root.Acquisition.Value, value)))
                {
                    try
                    {
                        if (!InExistingPool(value)) continue;
                        // Custody transfer precedes every fallible residue read. An
                        // exception must never let cleanup touch the next pool user.
                        owner.lifetime.ObserveReturn(value, ignored => true);
                        root.ReleaseResidueObserved = ControllersSettled(root.Controllers) &&
                            ReferenceEquals(Field(0x04000B2D, value.GetComponent<SnapToLocator>()), null);
                        if (!root.ReleaseResidueObserved) owner.fault = "pool-return-with-controller-residue";
                    }
                    catch (Exception error)
                    {
                        root.CustodyUncertain = !root.Acquisition.Returned;
                        owner.fault = "pool-return-observation:" + error.GetType().Name;
                    }
                }
        }

        private static bool InExistingPool(GameObject value)
        {
            if (!value || value.activeSelf) return false;
            var poolRoot = (GameObject)Field(0x04000AA3, null);
            var pool = value.GetComponent<PooledGameObject>();
            if (!poolRoot || value.transform.parent != poolRoot.transform || !pool || !pool.Prefab) return false;
            var queues = (Dictionary<int, Queue<PooledGameObject>>)Field(0x04000AA4, null);
            return queues != null && queues.TryGetValue(pool.Prefab.GetInstanceID(), out var queue) &&
                queue.Any(item => ReferenceEquals(item, pool));
        }

        private static bool ControllersSettled(IEnumerable<SnapControllerBase> controllers)
        {
            var registered = ((IEnumerable)Field(0x04000B12, null)).Cast<object>().ToArray();
            return controllers.All(controller => !controller ||
                !registered.Any(item => ReferenceEquals(item, controller)) &&
                !((IEnumerable)Field(0x04000AF3, controller) ?? new object[0]).Cast<object>().Any() &&
                !(bool)Field(0x04000B02, controller) && !(Object)Field(0x04000B01, controller) &&
                ReferenceEquals(controller.Map, null) && ReferenceEquals(Field(0x04000AFE, controller), null) &&
                ReferenceEquals(Field(0x04000B35, controller), null));
        }

        // Guard the native boundary as well as our drain: ordinary deactivation
        // or Respawn can revisit a stale FxObject after a throwing destruction.
        internal static void DestroyOwnedFx(GameObject value, ItemEnchantment enchantment)
        {
            var owner = owners.FirstOrDefault(candidate => ReferenceEquals(candidate.fact, enchantment));
            if (owner == null) { FxHelper.Destroy(value); return; }
            var root = owner.roots.LastOrDefault(item => ReferenceEquals(item.Acquisition.Value, value));
            if (owner.thread != System.Threading.Thread.CurrentThread.ManagedThreadId || root == null || root.CustodyUncertain ||
                !ReferenceEquals(enchantment.FxObject, value))
            {
                owner.fault = "native-fx-reference-without-live-custody";
                throw new InvalidOperationException("Charge enchantment cannot destroy an unconfirmed visual generation.");
            }
            if (root.Acquisition.Returned && owner.failures.Count < 32)
                owner.failures.Add("native-destroy-reference-already-returned");
            owner.lifetime.RequestCompletion(value, FxHelper.Destroy);
            // Native DestroyFx continues to its own FxObject=null setter, even
            // when this exact generation already returned. Never touch the FX.
        }

        internal bool TryDrain()
        {
            retiring = true;
            if (!ScopeSettled) return false;
            // DestroyFx is valid after native enchantment disposal and also handles
            // the inventory-view branch where native Deactivate intentionally skipped it.
            var attached = fact.FxObject;
            if (attached)
            {
                try { fact.DestroyFx(); } // Same guard for native and barrier callers.
                catch (Exception error) { if (failures.Count < 32) failures.Add("native-destroy:" + error.GetType().Name); }
            }
            foreach (var root in roots)
            {
                if (root.Acquisition.Returned || root.CustodyUncertain) continue;
                lifetime.TryComplete(root.Acquisition, FxHelper.Destroy, value => !value);
                if (root.Acquisition.Returned && !root.Acquisition.Value) root.ReleaseResidueObserved = true;
            }
            // Native OnDisable normally requests these destructions. An exact copy
            // captured before a failed field assignment is still ours to terminate.
            foreach (var copy in copies) lifetime.TryComplete(copy, value => Object.Destroy(value), value => !value);
            return Drained;
        }

        private static object Field(int token, object instance) => typeof(ItemEnchantment).Module.ResolveField(token).GetValue(instance);
        private static void Require(bool condition, string detail)
        {
            if (!condition) throw new InvalidOperationException("Mounted Charge FX input differs: " + detail + ".");
        }

        // Full Respawn scope: its early destruction callback must not admit a save
        // before the same native method continues and acquires a replacement FX.
        internal static IEnumerable<CodeInstruction> WrapRespawn(IEnumerable<CodeInstruction> source, ILGenerator generator, MethodBase original)
        {
            var token = original.MetadataToken;
            var beginMethod = token == 0x060099AB ? nameof(BeginRespawn) : token == 0x0600111A ? nameof(BeginController) :
                token == 0x060010BE ? nameof(BeginRelease) : null;
            if (beginMethod == null) throw new InvalidOperationException("Unpinned native visual lifetime body.");
            var code = source.ToList();
            var scope = generator.DeclareLocal(typeof(object)); var done = generator.DefineLabel();
            var output = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Call, Method(beginMethod)),
                new CodeInstruction(OpCodes.Stloc, scope)
            };
            var begin = new CodeInstruction(OpCodes.Nop);
            begin.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock, null)); output.Add(begin);
            foreach (var instruction in code)
            {
                if (instruction.opcode != OpCodes.Ret) { output.Add(instruction); continue; }
                var load = new CodeInstruction(OpCodes.Ldloc, scope);
                load.labels.AddRange(instruction.labels); load.blocks.AddRange(instruction.blocks);
                output.Add(load); output.Add(new CodeInstruction(OpCodes.Call, Method(nameof(Returned))));
                output.Add(new CodeInstruction(OpCodes.Leave, done));
            }
            var finish = new CodeInstruction(OpCodes.Ldloc, scope);
            finish.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginFinallyBlock, null));
            output.Add(finish); output.Add(new CodeInstruction(OpCodes.Call, Method(nameof(EndRespawn))));
            var end = new CodeInstruction(OpCodes.Nop); end.blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock, null)); output.Add(end);
            var ret = new CodeInstruction(OpCodes.Ret); ret.labels.Add(done); output.Add(ret);
            return output;
        }

        internal static IEnumerable<CodeInstruction> WrapAcquisition(IEnumerable<CodeInstruction> source, MethodBase original)
        {
            var token = original.MetadataToken;
            if (token != 0x060010BD && token != 0x06001109 && token != 0x0600111A)
                throw new InvalidOperationException("Unpinned native FX acquisition body.");
            var code = source.ToList(); var replaced = 0;
            foreach (var call in code.ToArray())
            {
                if (!(call.operand is MethodInfo method)) continue;
                if (token == 0x060010BD && method.DeclaringType == typeof(Queue<PooledGameObject>) && method.Name == "Dequeue")
                { call.opcode = OpCodes.Call; call.operand = Method(nameof(Dequeue)); replaced++; }
                if (method.DeclaringType != typeof(Object) || method.Name != "Instantiate" || !method.IsGenericMethod ||
                    method.GetGenericArguments().Single() != typeof(GameObject)) continue;
                if (token == 0x0600111A)
                {
                    Require(method.GetParameters().Length == 2 && method.GetParameters()[1].ParameterType == typeof(Transform), "native copy signature");
                    var instance = new CodeInstruction(OpCodes.Ldarg_0); instance.labels.AddRange(call.labels); call.labels.Clear();
                    instance.blocks.AddRange(call.blocks); call.blocks.Clear(); code.Insert(code.IndexOf(call), instance);
                    call.operand = Method(nameof(InstantiateCopy));
                }
                else
                {
                    Require(method.GetParameters().Length == 3 && method.GetParameters()[1].ParameterType == typeof(Vector3) &&
                        method.GetParameters()[2].ParameterType == typeof(Quaternion), "native root signature");
                    call.operand = Method(nameof(InstantiateRoot));
                }
                call.opcode = OpCodes.Call; replaced++;
            }
            Require(replaced == (token == 0x060010BD ? 2 : 1), "native acquisition call count");
            return code;
        }

        internal static IEnumerable<CodeInstruction> WrapDestruction(IEnumerable<CodeInstruction> source, MethodBase original)
        {
            Require(original.MetadataToken == 0x060099AF && original.DeclaringType == typeof(ItemEnchantment), "native destruction body");
            var code = source.ToList(); var replaced = 0;
            foreach (var call in code.ToArray())
            {
                if (!(call.operand is MethodInfo method) || method.DeclaringType != typeof(FxHelper) || method.MetadataToken != 0x060010B4) continue;
                var instance = new CodeInstruction(OpCodes.Ldarg_0);
                instance.labels.AddRange(call.labels); call.labels.Clear();
                instance.blocks.AddRange(call.blocks); call.blocks.Clear(); code.Insert(code.IndexOf(call), instance);
                call.opcode = OpCodes.Call; call.operand = Method(nameof(DestroyOwnedFx)); replaced++;
            }
            Require(replaced == 1, "one native destruction call before reference cleanup");
            return code;
        }

        private static MethodInfo Method(string name) => typeof(MountedChargeEnchantmentFx).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic);
        internal JObject CaptureEvidence() => new JObject
        {
            ["scopes"] = scopes, ["retiring"] = retiring, ["fault"] = fault, ["drained"] = Drained,
            ["pendingRoots"] = pendingRoots, ["pendingCopies"] = pendingCopies,
            ["failures"] = new JArray(failures),
            ["attached"] = !!fact.FxObject,
            ["roots"] = roots.Count, ["copies"] = copies.Count,
            ["rootFacts"] = new JArray(roots.Select(root => new JObject
            {
                ["generation"] = root.Acquisition.Generation, ["returned"] = root.Acquisition.Returned,
                ["custodyUncertain"] = root.CustodyUncertain,
                ["controllerResidueAbsent"] = root.ReleaseResidueObserved
            })),
            ["acquisitions"] = new JArray(lifetime.Acquisitions.Select(item => new JObject
            {
                ["generation"] = item.Generation, ["returned"] = item.Returned,
                ["completionRequested"] = item.CompletionRequested, ["failure"] = item.Failure
            }))
        };
    }
}
