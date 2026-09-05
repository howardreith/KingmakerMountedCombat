using System;
using System.Collections.Generic;
using System.Reflection;
using Harmony12;
using Kingmaker.Controllers.Combat;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.Controllers.Units;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.View;
using Kingmaker.Visual.Animation;
using Kingmaker.Visual.Animation.Kingmaker;
using Kingmaker.Visual.CharacterSystem;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Diagnostics;
using KingmakerMountedCombat.Logging;
using TurnBased.Controllers;

namespace KingmakerMountedCombat.Integration
{
    internal sealed class MountedPatchController : IDisposable
    {
        private const string HarmonyId = "KingmakerMountedCombat.Feasibility";
        private static readonly Guid ExpectedKingmakerMvid = new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7");
        private readonly HarmonyInstance harmony;
        private bool disposed;

        public MountedPatchController(GameMountedRelationshipService service, MountedPlayerActionController playerAction, MountedCombatController combat, UnifiedMountedTurnCoordinator unifiedTurn, NativeMountedControlService nativeControls, MountedAnimationAdapter animation, MountedDollRoomIkAdapter dollRoomIk, RuntimeSaveAuthorization saveAuthorization, NativeLifecycleDeliveryLedger lifecycleLedger, IModLogger logger)
        {
            PatchBridge.Service = service ?? throw new ArgumentNullException(nameof(service));
            PatchBridge.PlayerAction = playerAction ?? throw new ArgumentNullException(nameof(playerAction));
            PatchBridge.Combat = combat ?? throw new ArgumentNullException(nameof(combat));
            PatchBridge.UnifiedTurn = unifiedTurn ?? throw new ArgumentNullException(nameof(unifiedTurn));
            PatchBridge.NativeControls = nativeControls ?? throw new ArgumentNullException(nameof(nativeControls));
            PatchBridge.Animation = animation ?? throw new ArgumentNullException(nameof(animation));
            PatchBridge.DollRoomIk = dollRoomIk ?? throw new ArgumentNullException(nameof(dollRoomIk));
            PatchBridge.SaveAuthorization = saveAuthorization ?? throw new ArgumentNullException(nameof(saveAuthorization));
            PatchBridge.LifecycleLedger = lifecycleLedger ?? throw new ArgumentNullException(nameof(lifecycleLedger));
            PatchBridge.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            harmony = HarmonyInstance.Create(HarmonyId);
            try
            {
                var observedMvid = typeof(UnitEntityData).Assembly.ManifestModule.ModuleVersionId;
                if (observedMvid != ExpectedKingmakerMvid)
                {
                    throw new InvalidOperationException("Exact Kingmaker Assembly-CSharp MVID mismatch: " + observedMvid + ".");
                }

                PatchExact(typeof(ClickGroundHandler), "RunCommand", 0x060093DC, new[] { typeof(UnitEntityData), typeof(UnityEngine.Vector3), typeof(float?), typeof(float), typeof(float), typeof(bool) }, nameof(PatchMethods.GroundCommandPrefix), nameof(PatchMethods.GroundCommandPostfix));
                PatchExact(typeof(UnitCommands), "Run", 0x060026B2, new[] { typeof(UnitCommand) }, nameof(PatchMethods.UnitCommandRunPrefix));
                PatchExact(typeof(UnitUseAbility), "Init", 0x06002728, new[] { typeof(UnitEntityData) }, null, nameof(PatchMethods.NativeAbilityInitPostfix));
                PatchExact(typeof(SelectionManager), "SelectUnit", 0x060034F0, new[] { typeof(UnitEntityView), typeof(bool), typeof(bool), typeof(bool) }, nameof(PatchMethods.SelectUnitPrefix));
                PatchExact(typeof(SelectionManager), "MultiSelect", 0x060034F5, new[] { typeof(IEnumerable<UnitEntityView>), typeof(bool) }, nameof(PatchMethods.MultiSelectPrefix));
                PatchExact(typeof(SelectionManagerBase), "Stop", 0x060000B9, Type.EmptyTypes, nameof(PatchMethods.StopOrHoldPrefix));
                PatchExact(typeof(SelectionManagerBase), "Hold", 0x060000BA, Type.EmptyTypes, nameof(PatchMethods.StopOrHoldPrefix));
                PatchExact(typeof(UnitMoveContiniously), "Init", 0x060026F0, new[] { typeof(UnitEntityData) }, nameof(PatchMethods.ContinuousMovePrefix));
                PatchExact(typeof(SaveManager), "SaveRoutine", 0x06008029, new[] { typeof(SaveInfo), typeof(bool) }, nameof(PatchMethods.SavePrefix), nameof(PatchMethods.SavePostfix));
                PatchExact(typeof(SaveManager), "LoadRoutine", 0x0600802C, new[] { typeof(SaveInfo), typeof(bool) }, nameof(PatchMethods.LoadPrefix));
                PatchExact(typeof(UnitEntityView), "ForcePlaceAboveGround", 0x06001848, Type.EmptyTypes, nameof(PatchMethods.ForcePlaceAboveGroundPrefix));
                PatchExact(typeof(ClickUnitHandler), "OnClick", 0x060093ED, new[] { typeof(UnityEngine.GameObject), typeof(UnityEngine.Vector3), typeof(int), typeof(bool), typeof(bool) }, nameof(PatchMethods.UnitClickPrefix));
                PatchExact(typeof(UnitMovementAgent), "CanMoveInTurnBased", 0x060018A9, new[] { typeof(float).MakeByRefType() }, nameof(PatchMethods.MountMovementPrefix));
                PatchExact(typeof(UnitMovementAgent), "CompleteMovement", 0x060018B0, Type.EmptyTypes, nameof(PatchMethods.CompleteMovementPrefix));
                PatchExact(typeof(UnitCommand), "get_IsUnitEnoughClose", 0x06002784, Type.EmptyTypes, null, nameof(PatchMethods.IsUnitEnoughClosePostfix));
                PatchExact(typeof(UnitAttack), "GetApproachRadius", 0x06002685, new[] { typeof(UnitEntityData) }, null, nameof(PatchMethods.AttackRangePostfix));
                PatchExact(typeof(UnitCombatState), "AttackOfOpportunity", 0x060093A1, new[] { typeof(UnitEntityData), typeof(bool) }, nameof(PatchMethods.AttackOfOpportunityPrefix));
                PatchExact(typeof(UnitCombatCooldownsController), "TickOnUnit", 0x0600934A, new[] { typeof(UnitEntityData) }, nameof(PatchMethods.CombatCooldownPrefix), nameof(PatchMethods.CombatCooldownPostfix));
                PatchExact(typeof(UnitCommand), "Interrupt", 0x060027AC, new[] { typeof(bool) }, nameof(PatchMethods.CommandInterruptPrefix));
                PatchExact(typeof(UnitAnimationManager), "Tick", 0x06001605, Type.EmptyTypes, nameof(PatchMethods.AnimationTickPrefix));
                PatchExact(typeof(AttackHandInfo), "CreateAnimationHandleForAttack", 0x0600265A, new[] { typeof(IEnumerable<AttackHandInfo>) }, null, nameof(PatchMethods.AttackAnimationPostfix));
                PatchExact(typeof(IKController), "SetupIkSystem", 0x0600156C, new[] { typeof(Character) }, nameof(PatchMethods.DollRoomIkSetupPrefix));
                PatchExact(typeof(IKController), "SetupFbbik", 0x0600156D, Type.EmptyTypes, nameof(PatchMethods.DollRoomFbbikPrefix), nameof(PatchMethods.DollRoomFbbikPostfix));
                PatchExact(typeof(CombatController), "Tick", 0x06000BD1, Type.EmptyTypes, null, nameof(PatchMethods.CombatControllerTickPostfix));
                PatchExact(typeof(CombatController), "ChooseNextUnit", 0x06000BD2, Type.EmptyTypes, null, nameof(PatchMethods.ChooseNextUnitPostfix));
                PatchExact(typeof(TurnController), "Prepare", 0x06000C3C, Type.EmptyTypes, null, nameof(PatchMethods.TurnPreparePostfix));
                PatchExact(typeof(TurnController), "ContinueActing", 0x06000C3D, Type.EmptyTypes, null, nameof(PatchMethods.ContinueActingPostfix));
                PatchExact(typeof(CombatController), "HandleUnitRollsInitiative", 0x06000BEE, new[] { typeof(RuleInitiativeRoll) }, nameof(PatchMethods.InitiativePrefix));
                PatchExact(typeof(CombatController), "get_SortedUnits", 0x06000BC7, Type.EmptyTypes, null, nameof(PatchMethods.SortedUnitsPostfix));
                var trackerType = typeof(UnitEntityData).Assembly.GetType(
                    "Kingmaker.UI._ConsoleUI.TurnBasedMode.InitiativeTrackerVM",
                    true);
                PatchExact(trackerType, "UpdateUnits", 0x06004F0E, Type.EmptyTypes, nameof(PatchMethods.TrackerUpdatePrefix), nameof(PatchMethods.TrackerUpdatePostfix));
                PatchExact(typeof(UnitActionController), "TickCommandTurnBased", 0x0600911D, new[] { typeof(UnitCommand) }, null, nameof(PatchMethods.TickCommandTurnBasedPostfix));
                logger.Info("Installed thirty-one exact-token Harmony12 active-pair guards, unified-turn adapters, and bounded probes.");
            }
            catch
            {
                try
                {
                    harmony.UnpatchAll(HarmonyId);
                }
                finally
                {
                    PatchBridge.Service = null;
                    PatchBridge.PlayerAction = null;
                    PatchBridge.Combat = null;
                    PatchBridge.UnifiedTurn = null;
                    PatchBridge.NativeControls = null;
                    PatchBridge.Animation = null;
                    PatchBridge.DollRoomIk = null;
                    PatchBridge.SaveAuthorization = null;
                    PatchBridge.LifecycleLedger = null;
                    PatchBridge.Logger = null;
                }
                throw;
            }
        }

        public void Dispose()
        {
            if (disposed) { return; }
            if (PatchBridge.Service != null && !PatchBridge.Service.GuardBoundary(CleanupTrigger.ModDisabled))
            {
                throw new InvalidOperationException("Harmony guards cannot be removed while mounted cleanup residue remains.");
            }
            harmony.UnpatchAll(HarmonyId);
            PatchBridge.Service = null;
            PatchBridge.PlayerAction = null;
            PatchBridge.Combat = null;
            PatchBridge.UnifiedTurn = null;
            PatchBridge.NativeControls = null;
            PatchBridge.Animation = null;
            PatchBridge.DollRoomIk = null;
            PatchBridge.SaveAuthorization = null;
            PatchBridge.LifecycleLedger = null;
            PatchBridge.Logger = null;
            disposed = true;
        }

        private void PatchExact(Type type, string name, int expectedToken, Type[] parameters, string prefixName, string postfixName = null)
        {
            MethodInfo original;
            if (parameters == null)
            {
                var candidates = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                original = Array.Find(candidates, method => string.Equals(method.Name, name, StringComparison.Ordinal) && method.MetadataToken == expectedToken);
            }
            else
            {
                original = type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static, null, parameters, null);
            }

            if (original == null || original.MetadataToken != expectedToken)
            {
                throw new MissingMethodException(type.FullName, name + " exact token " + expectedToken.ToString("X8"));
            }

            var prefix = prefixName == null ? null : typeof(PatchMethods).GetMethod(prefixName, BindingFlags.Static | BindingFlags.NonPublic);
            var postfix = postfixName == null ? null : typeof(PatchMethods).GetMethod(postfixName, BindingFlags.Static | BindingFlags.NonPublic);
            harmony.Patch(
                original,
                prefix == null ? null : new HarmonyMethod(prefix),
                postfix == null ? null : new HarmonyMethod(postfix));
        }

        private static class PatchBridge
        {
            internal static GameMountedRelationshipService Service;
            internal static MountedPlayerActionController PlayerAction;
            internal static MountedCombatController Combat;
            internal static UnifiedMountedTurnCoordinator UnifiedTurn;
            internal static NativeMountedControlService NativeControls;
            internal static MountedAnimationAdapter Animation;
            internal static MountedDollRoomIkAdapter DollRoomIk;
            internal static RuntimeSaveAuthorization SaveAuthorization;
            internal static NativeLifecycleDeliveryLedger LifecycleLedger;
            internal static IModLogger Logger;
        }

        private static class PatchMethods
        {
            internal static bool GroundCommandPrefix(ref UnitEntityData unit)
            {
                if (PatchBridge.Combat != null && !PatchBridge.Combat.TryAdmitGroundCommand(unit))
                {
                    return false;
                }
                return PatchBridge.Service == null || PatchBridge.Service.RouteGroundCommand(ref unit);
            }

            internal static void GroundCommandPostfix(UnitEntityData unit)
            {
                PatchBridge.Combat?.CompleteGroundCommandAdmission(unit);
            }

            internal static bool UnitCommandRunPrefix(UnitCommands __instance, ref UnitCommand cmd)
            {
                if (PatchBridge.Combat != null && !PatchBridge.Combat.TryRouteMountedDoorInteraction(__instance, ref cmd))
                {
                    return false;
                }
                if (PatchBridge.Combat != null && !PatchBridge.Combat.TryRouteMountedStockAttack(__instance, cmd))
                {
                    return false;
                }
                return PatchBridge.Combat == null || PatchBridge.Combat.ShouldAllowStockCommand(__instance, cmd);
            }

            internal static void NativeAbilityInitPostfix(UnitUseAbility __instance)
            {
                PatchBridge.NativeControls?.PrepareNativePrimaryIntentShell(__instance);
            }

            internal static void CommandInterruptPrefix(UnitCommand __instance)
            {
                PatchBridge.Combat?.ObserveCommandInterrupt(__instance);
            }

            internal static void AnimationTickPrefix(UnitAnimationManager __instance)
            {
                PatchBridge.Animation?.RestoreExactDelegatedMountLocomotion(__instance);
            }

            internal static void AttackAnimationPostfix(AttackHandInfo __instance)
            {
                PatchBridge.Animation?.SupplyExactHorsePrimaryAnimation(__instance);
            }

            internal static void DollRoomIkSetupPrefix(IKController __instance)
            {
                PatchBridge.DollRoomIk?.BindExactMountedRiderIfRequired(__instance);
            }

            internal static void DollRoomFbbikPrefix(IKController __instance, out bool __state)
            {
                __state = PatchBridge.DollRoomIk != null &&
                    PatchBridge.DollRoomIk.BeginExactFbbikObservation(__instance);
            }

            internal static void DollRoomFbbikPostfix(bool __state)
            {
                PatchBridge.DollRoomIk?.CompleteExactFbbikObservation(__state);
            }

            internal static void ChooseNextUnitPostfix(CombatController __instance)
            {
                PatchBridge.UnifiedTurn?.HandleChooseNextUnit(__instance);
            }

            internal static void CombatControllerTickPostfix(CombatController __instance)
            {
                PatchBridge.UnifiedTurn?.HandleCombatControllerTickCompleted(__instance);
            }

            internal static void TurnPreparePostfix(TurnController __instance)
            {
                PatchBridge.UnifiedTurn?.HandleTurnPrepared(__instance);
            }

            internal static void ContinueActingPostfix(TurnController __instance, ref bool __result)
            {
                PatchBridge.UnifiedTurn?.ExtendTurnIfMountActionable(__instance, ref __result);
            }

            internal static void InitiativePrefix(RuleInitiativeRoll rule)
            {
                PatchBridge.UnifiedTurn?.MirrorInitiativeEvent(rule);
            }

            internal static void SortedUnitsPostfix(ref IEnumerable<UnitEntityData> __result)
            {
                PatchBridge.UnifiedTurn?.FilterTrackerSortedUnits(ref __result);
            }

            internal static void TrackerUpdatePrefix(out bool __state)
            {
                __state = PatchBridge.UnifiedTurn != null &&
                    PatchBridge.UnifiedTurn.BeginTrackerProjection();
            }

            internal static void TrackerUpdatePostfix(bool __state)
            {
                PatchBridge.UnifiedTurn?.EndTrackerProjection(__state);
            }

            internal static void TickCommandTurnBasedPostfix(UnitCommand command, ref bool __result)
            {
                RuntimeAutomationHost.ObserveNativeTurnBasedCommandEligibility(command, __result);
                PatchBridge.UnifiedTurn?.AdmitExactMountCommand(command, ref __result);
            }

            internal static bool SelectUnitPrefix(ref UnitEntityView unit, bool single)
            {
                return PatchBridge.Service == null || PatchBridge.Service.NormalizeSingleSelection(ref unit, single);
            }

            internal static void MultiSelectPrefix(ref IEnumerable<UnitEntityView> views)
            {
                PatchBridge.Service?.NormalizeMultiSelection(ref views);
            }

            internal static void StopOrHoldPrefix()
            {
                PatchBridge.Combat?.CancelSelectedInput("stop or hold");
                PatchBridge.Service?.ForwardStopOrHold();
            }

            internal static bool ContinuousMovePrefix(ref UnitEntityData executor)
            {
                if (PatchBridge.Service != null && PatchBridge.Service.IsExactActivePairUnit(executor))
                {
                    PatchBridge.Combat?.Cancel("continuous movement replaced the active mounted combat intent");
                }
                return PatchBridge.Service == null || PatchBridge.Service.RouteContinuousMove(ref executor);
            }

            internal static bool ForcePlaceAboveGroundPrefix(UnitEntityView __instance)
            {
                return PatchBridge.Service == null || !PatchBridge.Service.TrySuppressRiderGroundPlacement(__instance);
            }

            internal static bool UnitClickPrefix(
                UnityEngine.GameObject gameObject,
                int button,
                bool simulate,
                ref bool __result)
            {
                var result = PatchBridge.PlayerAction?.TryHandleMountTargetClick(gameObject, button, simulate) ??
                    MountedCombatClickResult.NotHandled;
                if (result == MountedCombatClickResult.NotHandled)
                {
                    result = PatchBridge.Combat?.TryHandleUnitClick(gameObject, button, simulate) ??
                        MountedCombatClickResult.NotHandled;
                }
                if (result == MountedCombatClickResult.NotHandled)
                {
                    return true;
                }
                __result = result == MountedCombatClickResult.HandledAccepted;
                return false;
            }

            internal static bool MountMovementPrefix(
                UnitMovementAgent __instance,
                ref float deltaTime,
                ref bool __result)
            {
                bool result;
                if (PatchBridge.Combat != null &&
                    PatchBridge.Combat.TryOverrideMountTurnMovement(__instance, ref deltaTime, out result))
                {
                    __result = result;
                    return false;
                }
                return true;
            }

            internal static bool CompleteMovementPrefix(UnitMovementAgent __instance)
            {
                return PatchBridge.Combat == null ||
                    !PatchBridge.Combat.TryCompleteNativeMountTurnMoveAtReachedPathEnd(__instance);
            }

            internal static void IsUnitEnoughClosePostfix(UnitCommand __instance, ref bool __result)
            {
                if (!__result && PatchBridge.Combat != null &&
                    PatchBridge.Combat.ShouldTreatNativeMountTurnMoveAsEnoughClose(__instance))
                {
                    __result = true;
                }
            }

            internal static void AttackRangePostfix(
                UnitAttack __instance,
                UnitEntityData unit,
                ref float __result)
            {
                var attack = __instance as MountedPairSingleAttack;
                float radius;
                if (attack != null && attack.TryCalculateNativeApproachRadius(unit, out radius))
                {
                    __result = radius;
                }
            }

            internal static bool AttackOfOpportunityPrefix(
                UnitCombatState __instance,
                UnitEntityData target,
                ref bool __result)
            {
                var suppressStep = PatchBridge.UnifiedTurn != null &&
                    PatchBridge.UnifiedTurn.ShouldSuppressStepOpportunity(target);
                if (!suppressStep && (PatchBridge.Combat == null ||
                    !PatchBridge.Combat.ShouldSuppressStockOpportunityAttack(__instance?.Unit, target)))
                {
                    return true;
                }

                __result = false;
                return false;
            }

            internal static void CombatCooldownPrefix(UnitEntityData unit, out float __state)
            {
                __state = unit?.CombatState == null
                    ? float.NaN
                    : unit.CombatState.Cooldown.Initiative;
            }

            internal static void CombatCooldownPostfix(UnitEntityData unit, float __state)
            {
                var combatState = unit?.CombatState;
                var game = Kingmaker.Game.Instance;
                RuntimeAutomationHost.ObserveCombatCooldownTick(
                    unit,
                    __state,
                    combatState == null ? float.NaN : combatState.Cooldown.Initiative,
                    game?.TimeController == null ? 0f : game.TimeController.GameDeltaTime,
                    combatState != null && combatState.Prepared,
                    unit != null && unit.IsInCombat,
                    unit != null && game?.State?.AwakeUnits != null && game.State.AwakeUnits.Contains(unit));
            }

            internal static bool SavePrefix(SaveManager __instance, SaveInfo saveInfo, bool forceAuto, ref IEnumerator<object> __result)
            {
                RuntimeAutomationHost.ObserveSaveRequest();
                if (!GuardNativeBoundary(NativeLifecycleBoundary.SaveRequest, CleanupTrigger.SaveRequested, "SaveManager.SaveRoutine Harmony12 prefix"))
                {
                    PatchBridge.SaveAuthorization?.ReportBoundaryFailure(RuntimeSaveOperation.Write, "relationship service reported residue");
                    __result = EmptyRoutine();
                    return false;
                }

                if (!AuthorizeSaveBoundary(RuntimeSaveOperation.Write, __instance, saveInfo, ref __result))
                {
                    return false;
                }
                if (PatchBridge.NativeControls != null &&
                    !PatchBridge.NativeControls.BeginSaveSerializationScope())
                {
                    PatchBridge.SaveAuthorization?.ReportBoundaryFailure(
                        RuntimeSaveOperation.Write,
                        "native mounted-control serialization scope could not start");
                    __result = EmptyRoutine();
                    return false;
                }
                return true;
            }

            internal static void SavePostfix(ref IEnumerator<object> __result)
            {
                if (PatchBridge.NativeControls != null &&
                    PatchBridge.NativeControls.SerializationSuspended)
                {
                    __result = PatchBridge.NativeControls.WrapSaveRoutine(__result);
                }
            }

            internal static bool LoadPrefix(SaveManager __instance, SaveInfo saveInfo, bool isSmokeTest, ref IEnumerator<object> __result)
            {
                RuntimeAutomationHost.ObserveLoadRequest();
                if (!GuardNativeBoundary(NativeLifecycleBoundary.LoadStart, CleanupTrigger.LoadRequested, "SaveManager.LoadRoutine Harmony12 prefix"))
                {
                    PatchBridge.SaveAuthorization?.ReportBoundaryFailure(RuntimeSaveOperation.Load, "relationship service reported residue");
                    __result = EmptyRoutine();
                    return false;
                }

                return AuthorizeSaveBoundary(RuntimeSaveOperation.Load, __instance, saveInfo, ref __result);
            }

            private static bool GuardNativeBoundary(NativeLifecycleBoundary boundary, CleanupTrigger trigger, string source)
            {
                var service = PatchBridge.Service;
                if (service == null)
                {
                    return true;
                }

                var before = service.State;
                var succeeded = false;
                try
                {
                    succeeded = service.GuardBoundary(trigger);
                    return succeeded;
                }
                finally
                {
                    PatchBridge.LifecycleLedger?.Record(
                        boundary,
                        source,
                        before,
                        service.State,
                        trigger,
                        true,
                        succeeded && service.State == RelationshipState.Unmounted);
                }
            }

            private static bool AuthorizeSaveBoundary(RuntimeSaveOperation operation, SaveManager saveManager, SaveInfo saveInfo, ref IEnumerator<object> result)
            {
                var authorization = PatchBridge.SaveAuthorization;
                if (authorization == null || !authorization.IsActive)
                {
                    return true;
                }

                RuntimeSaveAuthorizationDecision decision;
                try
                {
                    var target = saveInfo == null
                        ? null
                        : new RuntimeSaveTarget
                        {
                            InternalName = saveInfo.Name,
                            FileName = saveInfo.FileName,
                            FullPath = saveInfo.FolderName,
                            SaveType = saveInfo.Type.ToString(),
                            GameId = saveInfo.GameId,
                            GameName = saveInfo.GameName,
                            Area = saveInfo.Area == null ? null : saveInfo.Area.AssetGuidThreadSafe
                        };
                    var saveRoot = saveManager == null ? null : saveManager.SavePath;
                    decision = authorization.Authorize(operation, target, saveRoot);
                }
                catch (Exception exception)
                {
                    authorization.ReportFatalViolation(operation, "SaveInfo projection failed (" + exception.GetType().Name + ").");
                    PatchBridge.Logger?.Exception("FATAL runtime save authorization projection", exception);
                    result = EmptyRoutine();
                    return false;
                }

                if (decision.Allowed)
                {
                    return true;
                }

                if (decision.FatalViolation)
                {
                    PatchBridge.Logger?.Error("FATAL runtime save authorization violation: " + decision.Reason);
                }
                else
                {
                    PatchBridge.Logger?.Warning("Expected runtime save serialization suppression: " + decision.Reason);
                }
                result = EmptyRoutine();
                return false;
            }

            private static IEnumerator<object> EmptyRoutine()
            {
                yield break;
            }
        }
    }
}
