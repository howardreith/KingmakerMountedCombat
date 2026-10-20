using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Harmony12;
using Kingmaker;
using Kingmaker.Controllers.Rest;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.LevelUp;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.FactLogic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Observes only real native respec calls. The first committed rebuilt level
    // performs native refund/payment/rest/time/rebind; terminal autosave is later.
    // No hook changes an argument/result or invokes a native callback itself.
    internal sealed class NativeProgressionRespecProbe : IDisposable
    {
        private const string HarmonyId = "KingmakerMountedCombat.Diagnostics.Chunk7ProgressionRespec";
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private static NativeProgressionRespecProbe active;
        private readonly UnitEntityData original;
        private readonly RespecCompanion action;
        private readonly object player;
        private readonly Action assertScope;
        private readonly List<string> errors = new List<string>();
        private readonly JArray events = new JArray(), hooks = new JArray(), boundaryObservations = new JArray();
        private readonly HashSet<UnitDescriptor> expectedRest = new HashSet<UnitDescriptor>();
        private readonly HashSet<UnitDescriptor> restEntered = new HashSet<UnitDescriptor>(), restExited = new HashSet<UnitDescriptor>();
        private HarmonyInstance harmony;
        private bool disposed, installed, installationStarted, insideRespec, insideFinish, creditPending;
        private int callbackFrame = -1, entries, exits, previews, finishes, finishExits, advances, rebinds, moneyCredits;
        private int initialXp, initialUses, initialCost, expectedRefund;
        private long initialMoney, finishMoneyBefore, finishTicksBefore, creditAmount, creditBefore;
        private JObject creditBoundary;
        private UnitDescriptor previewDescriptor, reboundDescriptor;
        private UnitEntityData preview;
        private CharacterBuildController nativeBuilder;
        private object reboundView;
        private readonly UnitEntityData originalPet;
        private readonly UnitDescriptor originalPetDescriptor;
        private readonly object originalPetView;
        private readonly string originalPetId;
        private UnitEntityData observedPet, reboundPet;
        private UnitDescriptor observedPetDescriptor, reboundPetDescriptor;
        private object observedPetView, reboundPetView;
        private string observedPetId, reboundPetId;
        private AddPet observedPetSource, petUpdateSource;
        private UnitDescriptor petUpdateOwner;
        private bool originalPetRemoved, originalPetViewReleased;
        private int petUpdates, outsideFinishRestEntries, outsideFinishRestExits;


        internal NativeProgressionRespecProbe(UnitEntityData original, RespecCompanion action, Action assertScope)
        {
            this.original = original ?? throw new ArgumentNullException(nameof(original));
            this.action = action ?? throw new ArgumentNullException(nameof(action));
            this.assertScope = assertScope ?? throw new ArgumentNullException(nameof(assertScope));
            player = Game.Instance?.Player;
            Require(player != null, "Native respec observer requires its exact player.");
            originalPet = original.Descriptor.Pet;
            Require(originalPet != null && ReferenceEquals(originalPet.Descriptor.Master.Value, original),
                "Native respec observer requires its exact reciprocal original pet.");
            originalPetDescriptor = originalPet.Descriptor; originalPetView = originalPet.View; originalPetId = originalPet.UniqueId;
        }
        internal void Install()
        {
            if (disposed) throw new ObjectDisposedException(nameof(NativeProgressionRespecProbe));
            if (installed) return;
            if (installationStarted || active != null) throw new InvalidOperationException("A native respec observer already owns or attempted installation.");
            installationStarted = true;
            // Native contract validation; no patch ownership exists before the Harmony boundary.
            if (typeof(Game).Module.ModuleVersionId != new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7"))
                throw new InvalidOperationException("Installed native respec contracts changed.");
            harmony = HarmonyInstance.Create(HarmonyId); active = this;
            Patch(typeof(Player), "RespecCompanion", 0x06000DE7, new[] { typeof(UnitEntityData), typeof(Action) }, "RespecBefore", "RespecAfter");
            Patch(typeof(CharacterBuildController), "HandleLevelUpStart", 0x060039BC,
                new[] { typeof(UnitDescriptor), typeof(JToken), typeof(Action), typeof(LevelUpState.CharBuildMode) }, "BuildBefore", "BuildAfter");
            Patch(typeof(RespecCompanion), "FinishRespecialization", 0x06008ED5, Type.EmptyTypes, "FinishBefore", "FinishAfter");
            Patch(typeof(Player), "GainMoney", 0x06000DD0, new[] { typeof(long) }, "GainBefore", "GainAfter");
            Patch(typeof(Game), "AdvanceGameTime", 0x06000CC6, new[] { typeof(TimeSpan) }, "AdvanceBefore", null);
            Patch(typeof(RestController), "ApplyRest", 0x060091EC, new[] { typeof(UnitDescriptor) }, "RestBefore", "RestAfter");
            Patch(typeof(CharacterBuildController), "HandleUnitChangedAfterRespec", 0x060039D7,
                new[] { typeof(UnitEntityData) }, null, "RebindAfter");
            Patch(typeof(AddPet), "TryUpdatePet", 0x06002510, Type.EmptyTypes, "PetBefore", "PetAfter");
            installed = true;
        }
        internal bool InSynchronousCallback => callbackFrame == Time.frameCount &&
            (insideRespec || insideFinish || creditPending || petUpdateSource != null);
        internal bool Complete => installed && errors.Count == 0 && entries == 1 && exits == 1 && previews == 1 &&
            moneyCredits == 1 && !creditPending && finishes == 1 && finishExits == 1 && advances == 1 && rebinds == 1 &&
            petUpdateSource == null && outsideFinishRestEntries == outsideFinishRestExits &&
            restEntered.SetEquals(expectedRest) && restExited.SetEquals(expectedRest);
        internal UnitEntityData Preview => preview;
        internal UnitEntityData Original => original;
        internal long ExpectedMoneyAfter => checked(initialMoney + creditAmount - initialCost);
        internal int InitialExperience => initialXp;

        internal void RequireExactPreview(CharacterBuildController builder)
        {
            assertScope();
            Require(installed && errors.Count == 0 && entries == 1 && exits == 1 && previews == 1 && rebinds == 0 &&
                ReferenceEquals(Game.Instance.Player, player) && ReferenceEquals(builder, nativeBuilder) &&
                ReferenceEquals(builder.Unit, previewDescriptor) && ReferenceEquals(builder.LevelUpController?.Unit, previewDescriptor) &&
                ReferenceEquals(previewDescriptor.Unit, preview) && !ReferenceEquals(preview, original) && preview.UniqueId == original.UniqueId &&
                builder.LevelUpController.State.Mode == LevelUpState.CharBuildMode.Respec &&
                previewDescriptor.Progression.Experience == initialXp,
                "The real native respec call did not provide this exact preview and builder.");
        }
        internal bool TryRequireNativeRebind(CharacterBuildController builder)
        {
            assertScope();
            if (rebinds == 0) return false;
            Require(Complete && ReferenceEquals(builder, nativeBuilder) && ReferenceEquals(Game.Instance.Player, player) &&
                ReferenceEquals(original.Descriptor, reboundDescriptor) && ReferenceEquals(original.View, reboundView) &&
                original.Descriptor.Progression.Experience == initialXp && Game.Instance.Player.Money == ExpectedMoneyAfter,
                "Native respec rebind, payment, rest or actor continuity is incomplete.");
            RequireCurrentPetBinding();
            return true;
        }
        internal void RequireObservedPetBinding(UnitEntityData pet, object descriptor, object view)
        {
            Require(ReferenceEquals(pet, originalPet) && ReferenceEquals(descriptor, originalPetDescriptor) &&
                ReferenceEquals(view, originalPetView), "Respec observer belongs to another original pet binding.");
            RequireCurrentPetBinding();
        }
        private void RequireOriginalPet(bool removalObserved)
        {
            Require(originalPet.UniqueId == originalPetId && ReferenceEquals(originalPet.Descriptor, originalPetDescriptor),
                "Native respec replaced the original pet identity or descriptor.");
            if (!removalObserved)
            {
                Require(ReferenceEquals(originalPet.View, originalPetView), "Original pet view changed before observed native teardown.");
                return;
            }
            Require(originalPet.Descriptor.Master.Value == null && (originalPet.ShouldBeDestroyed || originalPet.Destroyed),
                "Observed old-pet removal lost its native detached destruction state.");
            if (ReferenceEquals(originalPet.View, null)) originalPetViewReleased = true;
            else Require(!originalPetViewReleased && ReferenceEquals(originalPet.View, originalPetView),
                "Native old-pet destruction acquired another view or revived a released view.");
        }
        private void RequireSourcedPet(UnitEntityData pet)
        {
            Require(ReferenceEquals(pet, observedPet) && pet != null && pet.UniqueId == observedPetId &&
                ReferenceEquals(pet.Descriptor, observedPetDescriptor) && ReferenceEquals(pet.View, observedPetView) &&
                ReferenceEquals(pet.Descriptor.Master.Value, original) && observedPetSource != null &&
                ReferenceEquals(observedPetSource.Owner, original.Descriptor) &&
                original.Descriptor.Progression.Features.SelectFactComponents<AddPet>().Any(value => ReferenceEquals(value, observedPetSource)) &&
                ReferenceEquals(observedPetSource.SpawnedPet, pet),
                "Native respec pet lacks its exact observed AddPet source and actor/descriptor/view binding.");
        }
        private void PinReboundPet()
        {
            var pet = original.Descriptor.Pet;
            var removed = !ReferenceEquals(pet, originalPet);
            RequireOriginalPet(removed);
            if (pet != null && removed) RequireSourcedPet(pet);
            if (!removed) Require(ReferenceEquals(originalPet.Descriptor.Master.Value, original),
                "Native respec kept its original pet without reciprocal ownership.");
            originalPetRemoved = removed;
            reboundPet = pet; reboundPetDescriptor = pet?.Descriptor; reboundPetView = pet?.View; reboundPetId = pet?.UniqueId;
        }
        private void RequireCurrentPetBinding()
        {
            Require(rebinds == 1 && errors.Count == 0, "Native respec has no successful observed pet rebind.");
            RequireOriginalPet(originalPetRemoved);
            Require(ReferenceEquals(original.Descriptor.Pet, reboundPet), "Native respec pet changed outside its observed native source.");
            if (reboundPet == null) return;
            Require(reboundPet.UniqueId == reboundPetId && ReferenceEquals(reboundPet.Descriptor, reboundPetDescriptor) &&
                ReferenceEquals(reboundPet.View, reboundPetView) && ReferenceEquals(reboundPet.Descriptor.Master.Value, original),
                "Observed respec pet identity, descriptor, view or reciprocal ownership changed.");
            if (originalPetRemoved) RequireSourcedPet(reboundPet);
        }
        private void BeginPetUpdate(AddPet source)
        {
            Require(entries == 1 && exits == 1 && previews == 1 && rebinds <= 1 && petUpdateSource == null && petUpdates < 32 &&
                ReferenceEquals(source.Owner, original.Descriptor) &&
                original.Descriptor.Progression.Features.SelectFactComponents<AddPet>().Any(value => ReferenceEquals(value, source)),
                "Native pet update is outside the exact observed respec actor and fact source.");
            if (rebinds == 1) RequireCurrentPetBinding();
            else RequireOriginalPet(false);
            var pet = original.Descriptor.Pet;
            Require(pet == null || ReferenceEquals(pet, originalPet) || ReferenceEquals(pet, observedPet),
                "An unobserved pet was present before native AddPet execution.");
            petUpdateSource = source; petUpdateOwner = source.Owner; callbackFrame = Time.frameCount;
        }
        private void CompletePetUpdate(AddPet source)
        {
            Require(ReferenceEquals(source, petUpdateSource) && ReferenceEquals(source.Owner, petUpdateOwner) &&
                ReferenceEquals(source.Owner, original.Descriptor) && callbackFrame == Time.frameCount &&
                original.Descriptor.Progression.Features.SelectFactComponents<AddPet>().Any(value => ReferenceEquals(value, source)),
                "Native AddPet exit differs from its exact entered source and owner.");
            var pet = source.SpawnedPet;
            Require(pet == null ? original.Descriptor.Pet == null || ReferenceEquals(original.Descriptor.Pet, originalPet) ||
                ReferenceEquals(original.Descriptor.Pet, observedPet) : ReferenceEquals(original.Descriptor.Pet, pet),
                "Native AddPet result is not the actor's reciprocal or previously pinned pet.");
            if (pet != null)
            {
                Require(ReferenceEquals(pet.Descriptor.Master.Value, original) &&
                    (observedPet == null || ReferenceEquals(pet, observedPet)),
                    "Native AddPet changed an already observed pet or lost reciprocal ownership.");
                if (observedPet != null) Require(pet.UniqueId == observedPetId &&
                    ReferenceEquals(pet.Descriptor, observedPetDescriptor) && ReferenceEquals(pet.View, observedPetView),
                    "Repeated native AddPet replaced its observed pet identity, descriptor or view.");
                observedPet = pet; observedPetDescriptor = pet.Descriptor; observedPetView = pet.View;
                observedPetId = pet.UniqueId; observedPetSource = source;
            }
            if (rebinds == 1 && pet != null)
            {
                Require(reboundPet == null || ReferenceEquals(reboundPet, pet), "Native AddPet replaced an already rebound pet.");
                reboundPet = pet; reboundPetDescriptor = pet?.Descriptor; reboundPetView = pet?.View; reboundPetId = pet?.UniqueId;
            }
            if (rebinds == 1) RequireCurrentPetBinding();
            petUpdates++; petUpdateSource = null; petUpdateOwner = null;
            Record("native-respec-pet-update", new JObject { ["sourceObject"] = Id(source), ["ownerDescriptorObject"] = Id(source.Owner),
                ["petId"] = pet?.UniqueId, ["petObject"] = Id(pet), ["petDescriptorObject"] = Id(pet?.Descriptor),
                ["petViewObject"] = Id(pet?.View), ["afterRebind"] = rebinds == 1 });
        }
        internal JObject Capture() => new JObject { ["installed"] = installed, ["installationAttempted"] = installationStarted,
            ["complete"] = Complete, ["errors"] = new JArray(errors), ["hooks"] = hooks.DeepClone(), ["events"] = events.DeepClone(),
            ["boundaryObservations"] = boundaryObservations.DeepClone(),
            ["nativeRespecEntries"] = entries, ["nativeRespecExits"] = exits, ["nativePreviewEntries"] = previews,
            ["nativeFinishEntries"] = finishes, ["nativeFinishExits"] = finishExits, ["nativeAdvanceCalls"] = advances,
            ["nativeRebinds"] = rebinds, ["nativePetUpdates"] = petUpdates, ["petUpdatePending"] = petUpdateSource != null,
            ["originalPetId"] = originalPetId, ["originalPetRemoved"] = originalPetRemoved,
            ["reboundPetId"] = reboundPetId, ["expectedRestCount"] = expectedRest.Count,
            ["observedRestEntries"] = restEntered.Count, ["observedRestExits"] = restExited.Count,
            ["outsideFinishRestEntries"] = outsideFinishRestEntries, ["outsideFinishRestExits"] = outsideFinishRestExits,
            ["initialMoney"] = initialMoney, ["nativeCost"] = initialCost,
            ["initialScrollRefund"] = expectedRefund, ["nativeScrollRefund"] = creditAmount,
            ["nativeMoneyCredits"] = moneyCredits, ["moneyCreditPending"] = creditPending,
            ["creditBoundary"] = creditBoundary?.DeepClone(),
            ["diagnosticMoneyWrites"] = 0, ["diagnosticXpWrites"] = 0, ["diagnosticRestCalls"] = 0, ["diagnosticTimeAdvances"] = 0 };
        private void Patch(Type type, string name, int token, Type[] parameters, string before, string after)
        {
            var method = type.GetMethod(name, Flags, null, parameters, null);
            Require(method != null && method.MetadataToken == token, "Native respec observation contract changed: " + type.FullName + "." + name);
            harmony.Patch(method, before == null ? null : new HarmonyMethod(typeof(Hooks).GetMethod(before, Flags)) { prioritiy = Priority.First },
                after == null ? null : new HarmonyMethod(typeof(Hooks).GetMethod(after, Flags)) { prioritiy = Priority.Last });
            hooks.Add(new JObject { ["type"] = type.FullName, ["method"] = name, ["token"] = token.ToString("X8"),
                ["moduleMvid"] = method.Module.ModuleVersionId.ToString("D") });
        }
        private void Safe(Action operation)
        {
            try { assertScope(); Require(ReferenceEquals(Game.Instance.Player, player), "Native respec player was replaced."); operation(); }
            catch (Exception exception) { var error = exception.ToString(); if (!errors.Contains(error)) errors.Add(error); }
        }
        private void Record(string kind, JObject detail)
        {
            Require(events.Count < 192, "Native respec observations exceeded their bound.");
            detail["kind"] = kind; detail["frame"] = Time.frameCount; detail["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks;
            detail["actorId"] = original.UniqueId; detail["actorObject"] = Id(original); events.Add(detail);
        }
        private void RecordBoundary(string kind, JObject detail)
        {
            Require(boundaryObservations.Count < 12, "Native respec boundary observations exceeded their bound.");
            detail["kind"] = kind; detail["frame"] = Time.frameCount;
            detail["actorId"] = original.UniqueId; boundaryObservations.Add(detail);
        }
        private static JObject Resources(UnitDescriptor unit) => new JObject { ["id"] = unit.Unit.UniqueId,
            ["descriptorObject"] = Id(unit), ["move"] = unit.Unit.CombatState.Cooldown.MoveAction,
            ["standard"] = unit.Unit.CombatState.Cooldown.StandardAction, ["swift"] = unit.Unit.CombatState.Cooldown.SwiftAction,
            ["prepared"] = unit.Unit.CombatState.Prepared, ["reactions"] = unit.Unit.CombatState.AttackOfOpportunityCount,
            ["reactionAllowance"] = unit.Unit.CombatState.AttackOfOpportunityPerRound,
            ["initiative"] = unit.Unit.CombatState.Initiative };
        private static int Id(object value) => value == null ? 0 : RuntimeHelpers.GetHashCode(value);
        private static void Require(bool value, string error) { if (!value) throw new InvalidOperationException(error); }

        private static class Hooks
        {
            internal static void RespecBefore(Player __instance, UnitEntityData unit, Action successCallback)
            {
                var p = active; if (p == null) return;
                p.Safe(() => {
                    Require(p.entries == 0 && ReferenceEquals(__instance, p.player) && ReferenceEquals(unit, p.original) &&
                        !p.action.ForFree && !p.action.MatchPlayerXpExactly && __instance.Difficulty.RespecAllowed &&
                        successCallback != null && successCallback.GetInvocationList().Length == 1 &&
                        ReferenceEquals(successCallback.Target, p.action) && successCallback.Method.DeclaringType == typeof(RespecCompanion) &&
                        successCallback.Method.MetadataToken == 0x06008ED5,
                        "Native respec entry is not the exact selected paid action and original actor.");
                    p.entries++; p.insideRespec = true; p.callbackFrame = Time.frameCount;
                    p.initialXp = unit.Descriptor.Progression.Experience; p.initialMoney = __instance.Money;
                    p.initialUses = __instance.RespecsUsed; p.initialCost = __instance.GetRespecCost();
                    p.expectedRefund = __instance.GetScrollRefundForRespec(unit.Descriptor);
                    Require(p.initialCost >= 0 && p.expectedRefund >= 0 && p.initialMoney >= p.initialCost,
                        "Native actual funds or refund range cannot support the selected respec.");
                    p.Record("native-respec-enter", new JObject { ["xp"] = p.initialXp, ["money"] = p.initialMoney,
                        ["uses"] = p.initialUses, ["cost"] = p.initialCost, ["scrollRefund"] = p.expectedRefund,
                        ["descriptorObject"] = Id(unit.Descriptor), ["viewObject"] = Id(unit.View), ["petId"] = unit.Descriptor.Pet?.UniqueId });
                });
            }
            internal static void RespecAfter(Player __instance, UnitEntityData unit)
            {
                var p = active; if (p == null) return;
                p.Safe(() => {
                    Require(p.insideRespec && p.callbackFrame == Time.frameCount && ReferenceEquals(__instance, p.player) &&
                        ReferenceEquals(unit, p.original) && p.previews == 1, "Native respec entry exited without its exact preview.");
                    p.exits++; p.insideRespec = false; p.Record("native-respec-ui-open", new JObject());
                });
            }
            internal static void BuildBefore(CharacterBuildController __instance, UnitDescriptor unit, JToken unitJson,
                Action onSuccess, LevelUpState.CharBuildMode mode)
            {
                var p = active; if (p == null || mode != LevelUpState.CharBuildMode.Respec) return;
                p.Safe(() => {
                    Require(p.insideRespec && p.callbackFrame == Time.frameCount && p.previews == 0 &&
                        ReferenceEquals(__instance, Game.Instance.UI.CharacterBuildController) && unitJson == null &&
                        unit != null && !ReferenceEquals(unit.Unit, p.original) && unit.Unit.UniqueId == p.original.UniqueId &&
                        unit.Progression.Experience == p.initialXp && onSuccess != null && onSuccess.GetInvocationList().Length == 1 &&
                        onSuccess.Method.Module.ModuleVersionId == typeof(Player).Module.ModuleVersionId &&
                        onSuccess.Method.DeclaringType?.DeclaringType == typeof(Player),
                        "Native respec builder did not originate from the observed native Player callback.");
                    p.previews++; p.preview = unit.Unit; p.previewDescriptor = unit; p.nativeBuilder = __instance;
                    p.Record("native-respec-preview", new JObject { ["previewObject"] = Id(p.preview), ["descriptorObject"] = Id(unit),
                        ["nativeCallbackToken"] = onSuccess.Method.MetadataToken.ToString("X8") });
                });
            }
            internal static void BuildAfter(CharacterBuildController __instance, UnitDescriptor unit, LevelUpState.CharBuildMode mode)
            {
                var p = active; if (p == null || mode != LevelUpState.CharBuildMode.Respec) return;
                p.Safe(() => Require(ReferenceEquals(__instance, p.nativeBuilder) && ReferenceEquals(unit, p.previewDescriptor) &&
                    ReferenceEquals(__instance.Unit, unit) && ReferenceEquals(__instance.LevelUpController?.Unit, unit),
                    "Native builder did not retain its exact observed preview."));
            }
            internal static void FinishBefore(RespecCompanion __instance)
            {
                var p = active; if (p == null) return;
                p.Safe(() => {
                    var player = Game.Instance.Player;
                    p.RecordBoundary("native-respec-finish-boundary", new JObject {
                        ["exactAction"] = ReferenceEquals(__instance, p.action), ["entries"] = p.entries,
                        ["previews"] = p.previews, ["priorFinishEntries"] = p.finishes,
                        ["forFree"] = __instance.ForFree, ["initialUses"] = p.initialUses,
                        ["currentUses"] = player.RespecsUsed, ["initialCost"] = p.initialCost,
                        ["currentCost"] = player.GetRespecCost(), ["initialMoney"] = p.initialMoney,
                        ["initialRefundEstimate"] = p.expectedRefund, ["observedRefund"] = p.creditAmount,
                        ["nativeMoneyCredits"] = p.moneyCredits, ["creditPending"] = p.creditPending,
                        ["currentMoney"] = player.Money,
                        ["outsideFinishRestEntries"] = p.outsideFinishRestEntries,
                        ["outsideFinishRestExits"] = p.outsideFinishRestExits });
                    Require(ReferenceEquals(__instance, p.action) && p.entries == 1 && p.previews == 1 &&
                        p.moneyCredits == 1 && !p.creditPending && p.finishes == 0 &&
                        !__instance.ForFree && player.RespecsUsed == p.initialUses && player.GetRespecCost() == p.initialCost &&
                        player.Money == checked(p.initialMoney + p.creditAmount),
                        "Native respec finish lost its exact action or observed native scroll refund.");
                    p.finishes++; p.insideFinish = true; p.callbackFrame = Time.frameCount;
                    p.finishMoneyBefore = player.Money; p.finishTicksBefore = Game.Instance.TimeController.GameTime.Ticks;
                    foreach (var actor in player.ControllableCharacters.Where(x => !x.Descriptor.State.IsFinallyDead))
                        Require(p.expectedRest.Add(actor.Descriptor), "Native respec rest roster contains a duplicate descriptor.");
                    Require(p.expectedRest.Count > 0 && p.expectedRest.Count <= 24, "Native rest roster is outside the bounded scope.");
                    p.Record("native-respec-finish-enter", new JObject { ["money"] = p.finishMoneyBefore,
                        ["nativeRestTargets"] = new JArray(p.expectedRest.Select(Resources)) });
                });
            }
            internal static void GainBefore(Player __instance, long amount)
            {
                var p = active; if (p == null || p.previews == 0) return;
                p.Safe(() => {
                    var playerExact = ReferenceEquals(__instance, p.player);
                    var builderExact = ReferenceEquals(Game.Instance.UI.CharacterBuildController, p.nativeBuilder);
                    var respecMode = p.nativeBuilder.LevelUpController?.State.Mode == LevelUpState.CharBuildMode.Respec;
                    var callbackRefund = __instance.GetScrollRefundForRespec(p.original.Descriptor);
                    if (p.creditBoundary == null)
                        p.creditBoundary = new JObject { ["frame"] = Time.frameCount,
                            ["playerExact"] = playerExact, ["builderExact"] = builderExact,
                            ["respecMode"] = respecMode, ["entries"] = p.entries, ["exits"] = p.exits,
                            ["finishes"] = p.finishes, ["priorMoneyCredits"] = p.moneyCredits,
                            ["creditPending"] = p.creditPending, ["initialMoney"] = p.initialMoney,
                            ["moneyBefore"] = __instance.Money, ["amount"] = amount,
                            ["callbackRefund"] = callbackRefund };
                    Require(playerExact && p.entries == 1 && p.exits == 1 &&
                        p.finishes == 0 && p.moneyCredits == 0 && !p.creditPending &&
                        builderExact && respecMode && __instance.Money == p.initialMoney &&
                        amount >= 0 && amount == callbackRefund,
                        "Native respec money credit is not the exact callback-time scroll refund.");
                    p.moneyCredits++; p.creditPending = true; p.creditAmount = amount;
                    p.creditBefore = __instance.Money; p.callbackFrame = Time.frameCount;
                });
            }
            internal static void GainAfter(Player __instance, long amount)
            {
                var p = active; if (p == null || p.previews == 0) return;
                p.Safe(() => {
                    Require(p.moneyCredits == 1 && p.creditPending && p.callbackFrame == Time.frameCount &&
                        ReferenceEquals(__instance, p.player) && amount == p.creditAmount &&
                        __instance.Money == checked(p.creditBefore + amount),
                        "Native respec money credit did not settle once on the exact player.");
                    p.creditPending = false;
                    p.Record("native-respec-refund-credit", new JObject {
                        ["amount"] = amount, ["moneyBefore"] = p.creditBefore,
                        ["moneyAfter"] = __instance.Money });
                });
            }
            internal static void AdvanceBefore(Game __instance, TimeSpan delta)
            {
                var p = active; if (p == null) return;
                p.Safe(() => {
                    Require(p.insideFinish && p.callbackFrame == Time.frameCount && ReferenceEquals(__instance, Game.Instance) &&
                        p.advances == 0 && delta == TimeSpan.FromDays(1), "Time advance is outside the one real native respec finish.");
                    p.advances++; p.Record("native-respec-time-advance", new JObject { ["deltaTicks"] = delta.Ticks });
                });
            }
            internal static void RestBefore(UnitDescriptor unit)
            {
                var p = active; if (p == null) return;
                p.Safe(() => {
                    if (!p.insideFinish) {
                        Require(++p.outsideFinishRestEntries <= 64, "Unowned native rest observations exceeded their bound.");
                        if (p.outsideFinishRestEntries <= 4)
                            p.RecordBoundary("native-rest-outside-finish-before", new JObject { ["unitId"] = unit?.Unit?.UniqueId,
                                ["descriptorObject"] = Id(unit), ["finishEntries"] = p.finishes });
                        return;
                    }
                    Require(p.callbackFrame == Time.frameCount && p.expectedRest.Contains(unit) && p.restEntered.Add(unit),
                        "Rest call is outside the exact once-only native respec roster.");
                    p.Record("native-respec-rest-before", Resources(unit));
                });
            }
            internal static void RestAfter(UnitDescriptor unit)
            {
                var p = active; if (p == null) return;
                p.Safe(() => {
                    if (!p.insideFinish) {
                        Require(++p.outsideFinishRestExits <= p.outsideFinishRestEntries,
                            "Unowned native rest exited without an observed entry.");
                        if (p.outsideFinishRestExits <= 4)
                            p.RecordBoundary("native-rest-outside-finish-after", new JObject { ["unitId"] = unit?.Unit?.UniqueId,
                                ["descriptorObject"] = Id(unit), ["finishEntries"] = p.finishes });
                        return;
                    }
                    Require(p.callbackFrame == Time.frameCount && p.restEntered.Contains(unit) && p.restExited.Add(unit),
                        "Native rest exit is missing its exact entered descriptor.");
                    p.Record("native-respec-rest-after", Resources(unit));
                });
            }
            internal static void FinishAfter(RespecCompanion __instance)
            {
                var p = active; if (p == null) return;
                p.Safe(() => {
                    Require(ReferenceEquals(__instance, p.action) && p.insideFinish && p.callbackFrame == Time.frameCount &&
                        p.finishes == 1 && p.finishExits == 0 && p.advances == 1 &&
                        p.restEntered.SetEquals(p.expectedRest) && p.restExited.SetEquals(p.expectedRest) &&
                        Game.Instance.Player.Money == checked(p.finishMoneyBefore - p.initialCost) &&
                        Game.Instance.Player.RespecsUsed == p.initialUses + 1 &&
                        Game.Instance.TimeController.GameTime.Ticks - p.finishTicksBefore == TimeSpan.TicksPerDay,
                        "Native respec finish has incomplete or repeated money, time or rest effects.");
                    p.finishExits++; p.insideFinish = false;
                    p.Record("native-respec-finish-exit", new JObject { ["money"] = Game.Instance.Player.Money,
                        ["uses"] = Game.Instance.Player.RespecsUsed, ["nativeElapsedTicks"] = Game.Instance.TimeController.GameTime.Ticks - p.finishTicksBefore });
                });
            }
            internal static void PetBefore(AddPet __instance)
            {
                var p = active; if (p == null || !ReferenceEquals(__instance?.Owner?.Unit, p.original)) return;
                p.Safe(() => p.BeginPetUpdate(__instance));
            }
            internal static void PetAfter(AddPet __instance)
            {
                var p = active; if (p == null || !ReferenceEquals(__instance?.Owner?.Unit, p.original)) return;
                p.Safe(() => p.CompletePetUpdate(__instance));
            }
            internal static void RebindAfter(CharacterBuildController __instance, UnitEntityData unit)
            {
                var p = active; if (p == null) return;
                p.Safe(() => {
                    Require(p.finishExits == 1 && p.rebinds == 0 && ReferenceEquals(unit, p.original) &&
                        ReferenceEquals(__instance, p.nativeBuilder) && ReferenceEquals(__instance.Unit, unit.Descriptor) &&
                        unit.Descriptor.Progression.Experience == p.initialXp && Game.Instance.Player.Money == p.ExpectedMoneyAfter,
                        "Native actor rebind did not follow the exact completed native respec callback.");
                    p.PinReboundPet();
                    p.rebinds++; p.reboundDescriptor = unit.Descriptor; p.reboundView = unit.View;
                    p.Record("native-respec-original-rebound", new JObject { ["descriptorObject"] = Id(unit.Descriptor),
                        ["viewObject"] = Id(unit.View), ["level"] = unit.Descriptor.Progression.CharacterLevel, ["petId"] = unit.Descriptor.Pet?.UniqueId,
                        ["petObject"] = Id(unit.Descriptor.Pet), ["petDescriptorObject"] = Id(unit.Descriptor.Pet?.Descriptor),
                        ["petViewObject"] = Id(unit.Descriptor.Pet?.View), ["oldPetId"] = p.originalPetId,
                        ["oldPetRemoved"] = p.originalPetRemoved, ["oldPetShouldBeDestroyed"] = p.originalPet.ShouldBeDestroyed,
                        ["oldPetDestroyed"] = p.originalPet.Destroyed, ["oldPetMasterId"] = p.originalPet.Descriptor.Master.Value?.UniqueId });
                });
            }
        }
        public void Dispose()
        {
            if (disposed) return;
            if (InSynchronousCallback) throw new InvalidOperationException("A native respec callback still owns its observer.");
            harmony?.UnpatchAll(HarmonyId); disposed = true;
            if (ReferenceEquals(active, this)) active = null;
        }
    }
}
