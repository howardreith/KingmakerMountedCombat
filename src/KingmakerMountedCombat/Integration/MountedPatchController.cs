using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Harmony12;
using Kingmaker.Controllers;
using Kingmaker.Controllers.Clicks;
using Kingmaker.Controllers.Combat;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.Controllers.Units;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.EntitySystem.Persistence.SavesStorage;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Buffs;
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

        public MountedPatchController(GameMountedRelationshipService service, MountedPlayerActionController playerAction, MountedCombatController combat, UnifiedMountedTurnCoordinator unifiedTurn, NativeMountedControlService nativeControls, MountedPersistenceService persistence, MountedAnimationAdapter animation, MountedDollRoomIkAdapter dollRoomIk, RuntimeSaveAuthorization saveAuthorization, NativeLifecycleDeliveryLedger lifecycleLedger, IModLogger logger)
        {
            PatchBridge.Service = service ?? throw new ArgumentNullException(nameof(service));
            PatchBridge.PlayerAction = playerAction ?? throw new ArgumentNullException(nameof(playerAction));
            PatchBridge.Combat = combat ?? throw new ArgumentNullException(nameof(combat));
            PatchBridge.ChargeSafety = new MountedChargeSafetyService(service, combat.RejectPairedControl);
            PatchBridge.UnifiedTurn = unifiedTurn ?? throw new ArgumentNullException(nameof(unifiedTurn));
            PatchBridge.NativeControls = nativeControls ?? throw new ArgumentNullException(nameof(nativeControls));
            PatchBridge.Persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
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
                PatchExact(typeof(UnitCommands), "Run", 0x060026B3,
                    new[] { typeof(UnitCommand), typeof(bool), typeof(bool) }, nameof(PatchMethods.ChargeAdmissionPrefix));
                PatchExact(typeof(UnitCommands), "AddToQueueInternal", 0x060026B8,
                    new[] { typeof(UnitCommand), typeof(bool) }, nameof(PatchMethods.ChargeAdmissionPrefix));
                PatchExact(typeof(UnitCommand), "Start", 0x060027A5, Type.EmptyTypes, nameof(PatchMethods.SaveCommandStartPrefix));
                PatchExact(typeof(UnitCommand), "Tick", 0x060027A7, Type.EmptyTypes, nameof(PatchMethods.ChargeExecutionPrefix));
                PatchExact(typeof(ClickWithSelectedAbilityHandler), "OnClick", 0x060093F6,
                    new[] { typeof(UnityEngine.GameObject), typeof(UnityEngine.Vector3), typeof(int), typeof(bool), typeof(bool) },
                    nameof(PatchMethods.ChargeClickPrefix));
                PatchExact(typeof(Kingmaker.UnitLogic.Abilities.AbilityData), "get_IsAvailableForCast", 0x06002B49,
                    Type.EmptyTypes, nameof(PatchMethods.ChargeAvailabilityPrefix));
                PatchExact(typeof(Kingmaker.UnitLogic.Abilities.AbilityData), "CanTarget", 0x06002B63,
                    new[] { typeof(Kingmaker.Utility.TargetWrapper) }, null, nameof(PatchMethods.ChargeTargetPostfix));
                PatchExact(typeof(Kingmaker.UnitLogic.Abilities.AbilityData), "GetUnavailableReason", 0x06002B66,
                    Type.EmptyTypes, nameof(PatchMethods.ChargeReasonPrefix));
                PatchExact(typeof(UnitCommands), "InterruptAndRemoveCommand", 0x060026BF,
                    new[] { typeof(UnitCommand.CommandType), typeof(bool) }, nameof(PatchMethods.PairedCommandInterruptPrefix));
                PatchExact(typeof(UnitCommand), "TickApproaching", 0x060027A6, Type.EmptyTypes,
                    nameof(PatchMethods.MountedAttackApproachPrefix));
                PatchExact(typeof(UnitAttack), "UpdateTarget", 0x06002683, Type.EmptyTypes,
                    nameof(PatchMethods.MountedAttackTargetPrefix));
                PatchExact(typeof(CombatController), "HandleCombatEnd", 0x06000BE3, Type.EmptyTypes,
                    nameof(PatchMethods.NativeCombatEndPrefix), nameof(PatchMethods.NativeCombatEndPostfix));
                PatchExact(typeof(UnitUseAbility), "Init", 0x06002728, new[] { typeof(UnitEntityData) }, null, nameof(PatchMethods.NativeAbilityInitPostfix));
                PatchExact(typeof(Kingmaker.UnitLogic.Abilities.AbilityData), "get_IsSuitableForAutoUse", 0x06002B30,
                    Type.EmptyTypes, null, nameof(PatchMethods.RelationshipControlAutoUsePostfix));
                PatchExact(typeof(SelectionManager), "SelectUnit", 0x060034F0, new[] { typeof(UnitEntityView), typeof(bool), typeof(bool), typeof(bool) }, nameof(PatchMethods.SelectUnitPrefix));
                PatchExact(typeof(SelectionManager), "MultiSelect", 0x060034F5, new[] { typeof(IEnumerable<UnitEntityView>), typeof(bool) }, nameof(PatchMethods.MultiSelectPrefix));
                PatchExact(typeof(SelectionManagerBase), "Stop", 0x060000B9, Type.EmptyTypes, nameof(PatchMethods.StopOrHoldPrefix));
                PatchExact(typeof(SelectionManagerBase), "Hold", 0x060000BA, Type.EmptyTypes, nameof(PatchMethods.StopOrHoldPrefix));
                PatchExact(typeof(UnitMoveContiniously), "Init", 0x060026F0, new[] { typeof(UnitEntityData) }, nameof(PatchMethods.ContinuousMovePrefix));
                PatchExact(typeof(LoadingProcess), "StartLoadingProcessInternal", 0x06007FC5, null,
                    null, null, nameof(PatchMethods.DeferredSaveStartTranspiler));
                PatchExact(typeof(LoadingProcess), "TickLoading", 0x06007FC2, Type.EmptyTypes,
                    null, null, nameof(PatchMethods.DeferredSaveTickTranspiler));
                PatchExact(typeof(LoadingProcess), "get_IsLoadingInProcess", 0x06007FBC, Type.EmptyTypes,
                    null, nameof(PatchMethods.DeferredSaveLoadingPostfix));
                PatchExact(typeof(LoadingProcess), "StopAll", 0x06007FC3, Type.EmptyTypes,
                    nameof(PatchMethods.AbandonOwnedLoadingPrefix));
                PatchExact(typeof(SaveManager), "PrepareSave", 0x06008025, new[] { typeof(SaveInfo) }, null, nameof(PatchMethods.SavePreparedPostfix));
                PatchExact(typeof(SaveManager).Assembly.GetType("Kingmaker.EntitySystem.Persistence.ZipSaver", true),
                    "SaveJson", 0x06008063, new[] { typeof(string), typeof(string) }, nameof(PatchMethods.NativeSaveHeaderPrefix));
                PatchExact(typeof(UnitEntityData), "PostLoad", 0x0600835E, Type.EmptyTypes, null, nameof(PatchMethods.ActorPostLoadPostfix));
                PatchExact(typeof(SaveManager), "IsSaveAllowed", 0x06008028, Type.EmptyTypes, null, null, nameof(PatchMethods.CombatSaveAdmissionTranspiler));
                PatchExact(typeof(SaveManager), "SerializeAndSaveThread", 0x0600802A,
                    new[] { typeof(SaveInfo), typeof(SaveCreateDTO), typeof(SaveInfo) }, null, nameof(PatchMethods.SaveWorkerPostfix), nameof(PatchMethods.NativeArchiveCommitTranspiler));
                PatchExact(typeof(SaveManager), "SaveRoutine", 0x06008029, new[] { typeof(SaveInfo), typeof(bool) }, nameof(PatchMethods.SavePrefix), nameof(PatchMethods.SavePostfix));
                PatchExact(typeof(Kingmaker.Game), "LoadArea", 0x06000CD5,
                    new[] { typeof(Kingmaker.Blueprints.Area.BlueprintArea), typeof(Kingmaker.Blueprints.Area.BlueprintAreaEnterPoint),
                        typeof(AutoSaveMode), typeof(bool), typeof(SaveInfo) }, nameof(PatchMethods.AreaTransitionPrefix));
                PatchExact(typeof(Kingmaker.Game), "OnAreaLoaded", 0x06000CD7, Type.EmptyTypes,
                    null, nameof(PatchMethods.AreaEntitiesReadyPostfix));
                PatchExact(typeof(Kingmaker.Game), "LoadGame", 0x06000CE0, new[] { typeof(SaveInfo) }, nameof(PatchMethods.GameLoadAdmissionPrefix));
                PatchExact(typeof(Kingmaker.Game), "LoadGameFromMainMenu", 0x06000CE2, new[] { typeof(SaveInfo) }, nameof(PatchMethods.GameMainMenuLoadAdmissionPrefix));
                PatchExact(typeof(Kingmaker.Game), "LoadGameForSmokeTest", 0x06000CE1, new[] { typeof(SaveInfo) }, nameof(PatchMethods.GameSmokeLoadAdmissionPrefix));
                PatchExact(typeof(SaveManager), "LoadRoutine", 0x0600802C, new[] { typeof(SaveInfo), typeof(bool) }, nameof(PatchMethods.LoadPrefix), nameof(PatchMethods.LoadPostfix));
                PatchExact(typeof(UnitEntityView), "ForcePlaceAboveGround", 0x06001848, Type.EmptyTypes, nameof(PatchMethods.ForcePlaceAboveGroundPrefix));
                PatchExact(typeof(ClickUnitHandler), "OnClick", 0x060093ED, new[] { typeof(UnityEngine.GameObject), typeof(UnityEngine.Vector3), typeof(int), typeof(bool), typeof(bool) }, nameof(PatchMethods.UnitClickPrefix));
                PatchExact(typeof(UnitMovementAgent), "CanMoveInTurnBased", 0x060018A9, new[] { typeof(float).MakeByRefType() }, nameof(PatchMethods.MountMovementPrefix));
                PatchExact(typeof(UnitMoveController), "Tick", 0x06009183, Type.EmptyTypes, nameof(PatchMethods.NativeMovementUpdatePrefix));
                PatchExact(typeof(UnitMovementAgent), "CompleteMovement", 0x060018B0, Type.EmptyTypes, nameof(PatchMethods.CompleteMovementPrefix));
                PatchExact(typeof(UnitCommand), "get_IsUnitEnoughClose", 0x06002784, Type.EmptyTypes, null, nameof(PatchMethods.IsUnitEnoughClosePostfix));
                PatchExact(typeof(UnitAttack), "GetApproachRadius", 0x06002685, new[] { typeof(UnitEntityData) }, null, nameof(PatchMethods.AttackRangePostfix));
                PatchExact(typeof(UnitCombatState), "AttackOfOpportunity", 0x060093A1, new[] { typeof(UnitEntityData), typeof(bool) }, nameof(PatchMethods.AttackOfOpportunityPrefix));
                PatchExact(typeof(UnitCombatCooldownsController), "TickOnUnit", 0x0600934A, new[] { typeof(UnitEntityData) }, nameof(PatchMethods.CombatCooldownPrefix), nameof(PatchMethods.CombatCooldownPostfix));
                PatchExact(typeof(UnitCombatPrepareController), "Tick", 0x0600936F, Type.EmptyTypes, nameof(PatchMethods.CombatPreparePrefix));
                PatchExact(typeof(Kingmaker.Controllers.Projectiles.Projectile), "OnHit", 0x06009270,
                    Type.EmptyTypes, null, nameof(PatchMethods.ProjectileHitPostfix));
                PatchExact(typeof(UnitCommand), "Interrupt", 0x060027AC, new[] { typeof(bool) }, nameof(PatchMethods.CommandInterruptPrefix));
                PatchExact(typeof(UnitAnimationManager), "Tick", 0x06001605, Type.EmptyTypes, nameof(PatchMethods.AnimationTickPrefix));
                PatchExact(typeof(AttackHandInfo), "CreateAnimationHandleForAttack", 0x0600265A, new[] { typeof(IEnumerable<AttackHandInfo>) }, null, nameof(PatchMethods.AttackAnimationPostfix));
                PatchExact(typeof(IKController), "SetupIkSystem", 0x0600156C, new[] { typeof(Character) }, nameof(PatchMethods.DollRoomIkSetupPrefix));
                PatchExact(typeof(IKController), "SetupFbbik", 0x0600156D, Type.EmptyTypes, nameof(PatchMethods.DollRoomFbbikPrefix), nameof(PatchMethods.DollRoomFbbikPostfix));
                PatchExact(typeof(CombatController), "Tick", 0x06000BD1, Type.EmptyTypes, nameof(PatchMethods.CombatControllerTickPrefix), nameof(PatchMethods.CombatControllerTickPostfix));
                PatchExact(typeof(CombatController), "ChooseNextUnit", 0x06000BD2, Type.EmptyTypes, null, nameof(PatchMethods.ChooseNextUnitPostfix), nameof(PatchMethods.PairedSelectorTranspiler));
                PatchExact(typeof(CombatController), "HandleCombatStart", 0x06000BE2, new[] { typeof(bool) }, nameof(PatchMethods.PairedEncounterPrefix), nameof(PatchMethods.PairedEncounterPostfix));
                PatchExact(typeof(CombatController), "Disable", 0x06000BEA, Type.EmptyTypes, nameof(PatchMethods.PairedModeExitPrefix));
                PatchExact(typeof(CombatController), "RemoveUnit", 0x06000BE6, new[] { typeof(UnitEntityData) }, nameof(PatchMethods.PairedActorRemovalPrefix));
                PatchExact(typeof(BuffCollection), "Tick", 0x06002A02, Type.EmptyTypes, null, null, nameof(PatchMethods.PairedBuffTimerTranspiler));
                PatchExact(typeof(CombatController), "TickTime", 0x06000BD6, Type.EmptyTypes, null, null, nameof(PatchMethods.PairedReadinessTranspiler));
                PatchExact(typeof(CombatController).GetNestedType("<>c", BindingFlags.NonPublic), "<HandleCombatStart>b__79_2",
                    0x0600A2BE, null, null, null, nameof(PatchMethods.PairedReadinessTranspiler));
                PatchExact(typeof(TurnController), "Prepare", 0x06000C3C, Type.EmptyTypes, nameof(PatchMethods.TurnPreparePrefix), nameof(PatchMethods.TurnPreparePostfix), nameof(PatchMethods.PairedPreparationTranspiler));
                PatchExact(typeof(TurnController), "Tick", 0x06000C34, Type.EmptyTypes, nameof(PatchMethods.PairedTickPrefix), null, nameof(PatchMethods.PairedActivityTranspiler));
                PatchExact(typeof(TurnController), "UpdateActionPredictions", 0x06000C6E, Type.EmptyTypes, nameof(PatchMethods.PairedInputPredictionPrefix));
                PatchExact(typeof(TurnController), "IgnoreClick", 0x06000C2F, Type.EmptyTypes, nameof(PatchMethods.PairedIgnoreClickPrefix));
                PatchExact(typeof(UnitCombatState), "get_IsFullAttackRestrictedBecauseOfMoveAction", 0x06009391,
                    Type.EmptyTypes, null, null, nameof(PatchMethods.PairedFullAttackInputTranspiler));
                PatchExact(typeof(CombatController), "ModifyMovementLimitOn", 0x06000BDF, Type.EmptyTypes, null, null, nameof(PatchMethods.PairedControllerInputTranspiler));
                PatchExact(typeof(CombatController), "ModifyMovementLimitOff", 0x06000BE0, Type.EmptyTypes, null, null, nameof(PatchMethods.PairedControllerInputTranspiler));
                PatchExact(typeof(CombatController), "ChangeCursorAction", 0x06000BE1, Type.EmptyTypes, null, null, nameof(PatchMethods.PairedControllerInputTranspiler));
                PatchExact(typeof(Kingmaker.UI.TurnBasedMode.PredictionPanelPCView), "EnableFiveFoot", 0x06003086,
                    Type.EmptyTypes, null, null, nameof(PatchMethods.PairedControllerInputTranspiler));
                var predictionVm = typeof(Kingmaker.UI._ConsoleUI.TurnBasedMode.PredictionPanelVM);
                PatchExact(predictionVm, ".ctor", 0x06004F2F, null, null, null, nameof(PatchMethods.PairedVmConstructorTranspiler));
                PatchExact(predictionVm, "get_RemainingTime", 0x06004F29, Type.EmptyTypes, null, null, nameof(PatchMethods.PairedVmReaderTranspiler));
                PatchExact(predictionVm, "get_IsOutOfRange", 0x06004F2A, Type.EmptyTypes, null, null, nameof(PatchMethods.PairedVmReaderTranspiler));
                PatchExact(predictionVm, "get_CanSwitchAction", 0x06004F2B, Type.EmptyTypes, null, null, nameof(PatchMethods.PairedVmReaderTranspiler));
                PatchExact(predictionVm, "get_MovementLimit", 0x06004F2C, Type.EmptyTypes, null, null, nameof(PatchMethods.PairedVmReaderTranspiler));
                PatchExact(predictionVm, "get_IsOverTerrain", 0x06004F2D, Type.EmptyTypes, null, null, nameof(PatchMethods.PairedVmReaderTranspiler));
                PatchExact(predictionVm, "PredictionChanged", 0x06004F31, Type.EmptyTypes, null, null, nameof(PatchMethods.PairedVmReaderTranspiler));
                PatchExact(predictionVm, "<.ctor>b__26_0", 0x06004F33, null, null, null, nameof(PatchMethods.PairedVmReaderTranspiler));
                var pathPreview = typeof(Kingmaker.TurnBasedMode.PathVisualizer);
                PatchExact(pathPreview, "CalculatePathForCommand", 0x06007020, null, null, null, nameof(PatchMethods.PairedPathUnitTranspiler));
                PatchExact(pathPreview, "GetDefaultVisualPathSettings", 0x06007015, Type.EmptyTypes, null, null, nameof(PatchMethods.PairedPathSettingsTranspiler));
                PatchExact(pathPreview, "CurrentPathForUnit", 0x0600700F, null, null, null, nameof(PatchMethods.PairedControllerInputTranspiler));
                PatchExact(pathPreview, "UpdateActionStatesFromPath", 0x06007021, null, null, null, nameof(PatchMethods.PairedControllerInputTranspiler));
                PatchExact(typeof(ClickGroundHandler), "OnClick", 0x060093D5, null, null, null, nameof(PatchMethods.PairedPathUnitTranspiler));
                PatchExact(typeof(ClickGroundHandler), "MoveSelectedUnitsToPoint", 0x060093DB, null, null, null, nameof(PatchMethods.PairedControllerInputTranspiler));
                PatchExact(typeof(TurnController), "ContinueWaiting", 0x06000C3E, Type.EmptyTypes, null, nameof(PatchMethods.PairedWaitingPostfix));
                PatchExact(typeof(TurnController), "CanDelay", 0x06000C49, Type.EmptyTypes, null, nameof(PatchMethods.PairedCanDelayPostfix));
                PatchExact(typeof(TurnController), "DelayInitiaive", 0x06000C61, new[] { typeof(UnitEntityData) }, nameof(PatchMethods.PairedDelayPrefix));
                PatchExact(typeof(TurnController), "ForceToEnd", 0x06000C47, new[] { typeof(bool) }, nameof(PatchMethods.PairedForfeitPrefix), null, nameof(PatchMethods.PairedForfeitDebtTranspiler));
                PatchExact(typeof(TurnController), "End", 0x06000C46, Type.EmptyTypes, null, nameof(PatchMethods.PairedEndPostfix), nameof(PatchMethods.PairedEndDebtTranspiler));
                PatchExact(typeof(UnitDoNothing), "OnTick", 0x060026C9, Type.EmptyTypes, null, null, nameof(PatchMethods.PairedConditionActionTranspiler));
                PatchExact(typeof(UnitSelfHarm), "OnAction", 0x0600270C, Type.EmptyTypes, null, null, nameof(PatchMethods.PairedConditionActionTranspiler));
                PatchExact(typeof(UnitProneController), "Tick", 0x0600918C, new[] { typeof(UnitEntityData) }, null, null, nameof(PatchMethods.PairedProneTranspiler));
                PatchExact(typeof(UnitCombatState), "OnNewRound", 0x0600939D, Type.EmptyTypes, nameof(PatchMethods.NativeRoundStatePrefix));
                PatchExact(typeof(TurnController), "TickMovement", 0x06000C37,
                    new[] { typeof(float).MakeByRefType(), typeof(bool) }, null, nameof(PatchMethods.NativeMovementTickPostfix));
                PatchExact(typeof(TurnController), "HandleUnitCommandDidEnd", 0x06000C5E,
                    new[] { typeof(UnitCommand) }, null, nameof(PatchMethods.NativeMovementCommandEndPostfix));
                PatchExact(typeof(UnitActionController), "UpdateCooldowns", 0x06009120,
                    new[] { typeof(UnitCommand) }, null, nameof(PatchMethods.NativeActionCostPostfix));
                PatchExact(typeof(TurnController), "ContinueActing", 0x06000C3D, Type.EmptyTypes, null, nameof(PatchMethods.ContinueActingPostfix));
                PatchExact(typeof(CombatController), "HandleUnitRollsInitiative", 0x06000BEE, new[] { typeof(RuleInitiativeRoll) }, nameof(PatchMethods.InitiativePrefix));
                PatchExact(typeof(CombatController), "get_SortedUnits", 0x06000BC7, Type.EmptyTypes, null, nameof(PatchMethods.SortedUnitsPostfix));
                var trackerType = typeof(UnitEntityData).Assembly.GetType(
                    "Kingmaker.UI._ConsoleUI.TurnBasedMode.InitiativeTrackerVM",
                    true);
                PatchExact(trackerType, "UpdateUnits", 0x06004F0E, Type.EmptyTypes, nameof(PatchMethods.TrackerUpdatePrefix), nameof(PatchMethods.TrackerUpdatePostfix));
                PatchExact(typeof(UnitActionController), "TickCommandTurnBased", 0x0600911D, new[] { typeof(UnitCommand) }, null, nameof(PatchMethods.TickCommandTurnBasedPostfix), nameof(PatchMethods.PairedEligibilityTranspiler));
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
                    PatchBridge.Persistence = null;
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
            PatchBridge.ChargeSafety = null;
            PatchBridge.UnifiedTurn = null;
            PatchBridge.NativeControls = null;
                    PatchBridge.Persistence = null;
            PatchBridge.Animation = null;
            PatchBridge.DollRoomIk = null;
            PatchBridge.SaveAuthorization = null;
            PatchBridge.LifecycleLedger = null;
            PatchBridge.Logger = null;
            disposed = true;
        }

        private void PatchExact(Type type, string name, int expectedToken, Type[] parameters, string prefixName, string postfixName = null, string transpilerName = null)
        {
            MethodBase original;
            if (name == ".ctor")
            {
                original = Array.Find(type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance),
                    constructor => constructor.MetadataToken == expectedToken);
            }
            else if (parameters == null)
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
            var transpiler = transpilerName == null ? null : typeof(PatchMethods).GetMethod(transpilerName, BindingFlags.Static | BindingFlags.NonPublic);
            harmony.Patch(
                original,
                prefix == null ? null : new HarmonyMethod(prefix),
                postfix == null ? null : new HarmonyMethod(postfix),
                transpiler == null ? null : new HarmonyMethod(transpiler));
        }

        internal static void ReportFailedSave(Exception exception) => PatchBridge.Persistence?.ReportFailedSave(exception);

        // A native loading process that failed and is not an owned save being
        // retired. Observation only: the exception is rethrown unchanged.
        internal static void ReportFailedNativeLoading(Exception exception) =>
            PatchBridge.Persistence?.ObserveNativeLoadFailure(exception);

        private static class PatchBridge
        {
            internal static MountedChargeSafetyService ChargeSafety;
            internal static GameMountedRelationshipService Service;
            internal static MountedPlayerActionController PlayerAction;
            internal static MountedCombatController Combat;
            internal static UnifiedMountedTurnCoordinator UnifiedTurn;
            internal static NativeMountedControlService NativeControls;
            internal static MountedPersistenceService Persistence;
            internal static MountedAnimationAdapter Animation;
            internal static MountedDollRoomIkAdapter DollRoomIk;
            internal static RuntimeSaveAuthorization SaveAuthorization;
            internal static NativeLifecycleDeliveryLedger LifecycleLedger;
            internal static IModLogger Logger;
        }

        private static class PatchMethods
        {
            internal static void ChargeTargetPostfix(Kingmaker.UnitLogic.Abilities.AbilityData __instance, ref bool __result)
            {
                // The installed native-target extension replaces this method in
                // a prefix. Restrict only the affected Charge's final result.
                if (PatchBridge.ChargeSafety?.RejectionReason(__instance) != null) __result = false;
            }

            internal static bool ChargeAvailabilityPrefix(Kingmaker.UnitLogic.Abilities.AbilityData __instance, ref bool __result)
            {
                if (PatchBridge.ChargeSafety?.RejectionReason(__instance) == null) return true;
                __result = false;
                return false;
            }

            internal static bool ChargeReasonPrefix(Kingmaker.UnitLogic.Abilities.AbilityData __instance, ref string __result)
            {
                var reason = PatchBridge.ChargeSafety?.RejectionReason(__instance);
                if (reason == null) return true;
                __result = reason;
                return false;
            }

            internal static bool ChargeClickPrefix(ClickWithSelectedAbilityHandler __instance, int button,
                bool simulate, bool muteEvents, ref bool __result)
            {
                if (button != 0 || PatchBridge.ChargeSafety == null ||
                    PatchBridge.ChargeSafety.AllowClick(__instance.Ability, simulate, muteEvents)) return true;
                __result = false;
                return false;
            }

            internal static bool ChargeAdmissionPrefix(UnitCommand cmd) =>
                PatchBridge.Persistence?.CombatRestorationPending != true &&
                (PatchBridge.ChargeSafety == null || PatchBridge.ChargeSafety.AllowAdmission(cmd));

            internal static bool SaveCommandStartPrefix(UnitCommand __instance) =>
                PatchBridge.Persistence?.CombatRestorationPending != true &&
                (PatchBridge.Persistence?.Enabled != true || NativeSaveEffectBoundary.MayStartDuringWait(__instance) ||
                 PatchBridge.UnifiedTurn?.MayStartNativePreparationDuringSave(__instance) == true ||
                 !NativeDeferredSave.Waiting(LoadingProcess.Instance)) &&
                ChargeExecutionPrefix(__instance);

            internal static void DeferredSaveLoadingPostfix(LoadingProcess __instance, ref bool __result)
            {
                if (__result && NativeDeferredSave.Waiting(__instance)) __result = false;
            }

            internal static IEnumerable<CodeInstruction> DeferredSaveStartTranspiler(IEnumerable<CodeInstruction> instructions) =>
                NativeDeferredSave.TransformStart(instructions);

            internal static IEnumerable<CodeInstruction> DeferredSaveTickTranspiler(IEnumerable<CodeInstruction> instructions) =>
                NativeDeferredSave.TransformTick(instructions);

            internal static void AbandonOwnedLoadingPrefix(LoadingProcess __instance) =>
                NativeDeferredSave.AbandonOwned(__instance, error => PatchBridge.Persistence?.ReportAbandonedSave(error));

            internal static bool ChargeExecutionPrefix(UnitCommand __instance) =>
                PatchBridge.ChargeSafety == null || PatchBridge.ChargeSafety.AllowExecution(__instance);

            internal static void NativeMovementUpdatePrefix() => PatchBridge.Service?.BeginNativeMovementUpdate();

            internal static bool GroundCommandPrefix(ref UnitEntityData unit)
            {
                if (PointerController.SimulatingClick) { return true; }
                if (PatchBridge.Combat != null && !PatchBridge.Combat.TryAdmitGroundCommand(unit))
                {
                    return false;
                }
                return PatchBridge.Service == null || PatchBridge.Service.RouteGroundCommand(ref unit,
                    PatchBridge.UnifiedTurn?.MaySelectPairedPartner(unit) ?? false);
            }

            internal static void GroundCommandPostfix(UnitEntityData unit)
            {
                if (PointerController.SimulatingClick) { return; }
                PatchBridge.Combat?.CompleteGroundCommandAdmission(unit);
            }

            internal static bool UnitCommandRunPrefix(UnitCommands __instance, ref UnitCommand cmd)
            {
                if (PatchBridge.Persistence?.CombatRestorationPending == true) return false;
                // Native TB cursor prediction replaces Unit.Commands temporarily. Its fake orders
                // must stay entirely native; routing one can cancel a real pair order or move its mount.
                if (PointerController.SimulatingClick) { return true; }
                if (PatchBridge.UnifiedTurn?.AdmitNativePreparationCommand(__instance, cmd) == true) return true;
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
                PatchBridge.NativeControls?.PrepareNativeMountApproach(__instance);
                PatchBridge.NativeControls?.PrepareNativePrimaryIntentShell(__instance);
            }

            internal static void RelationshipControlAutoUsePostfix(Kingmaker.UnitLogic.Abilities.AbilityData __instance, ref bool __result)
            {
                if (PatchBridge.NativeControls != null && PatchBridge.NativeControls.IsPlayerOnlyRelationshipControl(__instance))
                    __result = false;
            }

            internal static void CommandInterruptPrefix(UnitCommand __instance)
            {
                PatchBridge.Combat?.ObserveCommandInterrupt(__instance);
            }

            internal static bool PairedCommandInterruptPrefix(UnitCommands __instance, UnitCommand.CommandType type, bool interruptPaired)
            {
                return PatchBridge.Combat == null ||
                    !PatchBridge.Combat.PreservesApproachParent(__instance, type, interruptPaired);
            }

            internal static bool MountedAttackApproachPrefix(UnitCommand __instance)
            {
                if (!ChargeExecutionPrefix(__instance)) return false;
                // The pair command owns its mount Move slot and drives it through
                // the existing native/off-executor movement paths. Running the
                // base approach too would stop that move or start rider navigation.
                return !(__instance is MountedPairAttackCommand);
            }

            internal static bool MountedAttackTargetPrefix(UnitAttack __instance, ref bool __result)
            {
                var mounted = __instance as MountedPairAttackCommand;
                if (mounted == null) return true;
                __result = mounted.ValidateNativeSequenceTarget();
                return false;
            }

            internal static void NativeMovementTickPostfix(TurnController __instance)
            {
                if (!PointerController.SimulatingClick) PatchBridge.UnifiedTurn?.ObserveNativeMovement(__instance);
            }

            internal static void NativeActionCostPostfix(UnitCommand command)
            {
                if (!PointerController.SimulatingClick) PatchBridge.UnifiedTurn?.ObserveNativeActionCost(command);
            }

            internal static void NativeMovementCommandEndPostfix(TurnController __instance, UnitCommand command)
            {
                if (!PointerController.SimulatingClick && command?.Executor == __instance.Unit)
                    PatchBridge.UnifiedTurn?.ObserveNativeMovement(__instance);
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

            internal static bool TurnPreparePrefix(TurnController __instance, out bool __state)
            {
                __state = PatchBridge.Persistence?.CombatRestorationPending != true &&
                    (PatchBridge.UnifiedTurn == null || PatchBridge.UnifiedTurn.HandleTurnPreparing(__instance));
                return __state;
            }

            internal static bool PairedEncounterPrefix(CombatController __instance, bool isPartyCombatStateChanged)
            {
                if (PatchBridge.Persistence?.BeforeCombatStart() == false) return false;
                if (PatchBridge.Persistence?.CombatRestorationPending != true)
                    PatchBridge.UnifiedTurn?.BeginNativeEncounter(__instance, isPartyCombatStateChanged);
                return true;
            }
            internal static void PairedEncounterPostfix() => PatchBridge.Persistence?.TryRestoreCombat();
            internal static bool CombatControllerTickPrefix() => PatchBridge.Persistence?.BeforeCombatTick() ?? true;
            internal static void PairedWaitingPostfix(TurnController __instance, ref bool __result) =>
                PatchBridge.UnifiedTurn?.ExtendPairedWaiting(__instance, ref __result);
            internal static void PairedForfeitPrefix(TurnController __instance, bool setCooldowns) => PatchBridge.UnifiedTurn?.ForfeitPairedActivation(__instance, setCooldowns);
            internal static void PairedModeExitPrefix(CombatController __instance)
            {
                if (PatchBridge.Persistence?.LoadingWorld != true) PatchBridge.UnifiedTurn?.BeforeNativeModeExit(__instance);
            }
            internal static void PairedActorRemovalPrefix(UnitEntityData unit) => PatchBridge.UnifiedTurn?.BeforePairedActorRemoval(unit);
            internal static void PairedTickPrefix(TurnController __instance) => PatchBridge.UnifiedTurn?.TickPairedNativeState(__instance);
            internal static bool PairedInputPredictionPrefix(TurnController __instance) =>
                PatchBridge.UnifiedTurn?.PrepareNativeInputPrediction(__instance) ?? true;
            internal static bool PairedIgnoreClickPrefix(TurnController __instance, ref bool __result) =>
                PatchBridge.UnifiedTurn == null || PatchBridge.UnifiedTurn.ShouldRunNativeIgnoreClick(__instance, ref __result);
            internal static bool PairedPreserveSelection(TurnController turn) => PatchBridge.UnifiedTurn?.PreservePartnerSelection(turn) ?? false;
            internal static TurnController PairedInputContext(TurnController turn) => PatchBridge.UnifiedTurn?.SelectedNativeInputContext(turn) ?? turn;
            internal static UnitEntityData PairedPathInputUnit() =>
                PairedInputContext(Kingmaker.Game.Instance.TurnBasedCombatController.CurrentTurn)?.Unit ?? CombatController.CurrentUnit;
            internal static TurnController PairedVmContext(object viewModel) => PairedInputContext(Kingmaker.Game.Instance.TurnBasedCombatController.CurrentTurn);
            internal static bool PairedActionContextActor(UnitEntityData actor) => PatchBridge.UnifiedTurn?.IsNativeActionContextActor(actor) ?? actor.IsCurrentUnit();
            internal static TurnController PairedActorActionContext(CombatController controller, UnitEntityData actor) =>
                PatchBridge.UnifiedTurn?.NativeActorActionContext(controller.CurrentTurn, actor) ?? controller.CurrentTurn;
            internal static void PairedCanDelayPostfix(TurnController __instance, ref bool __result)
            {
                if (__result && PatchBridge.UnifiedTurn != null) __result = PatchBridge.UnifiedTurn.CanDelayPaired(__instance);
            }
            internal static bool PairedDelayPrefix(TurnController __instance, UnitEntityData delayTarget) =>
                PatchBridge.UnifiedTurn == null || PatchBridge.UnifiedTurn.BeginPairedDelay(__instance, delayTarget);
            internal static bool IsPairedResume(TurnController turn) => PatchBridge.UnifiedTurn != null && PatchBridge.UnifiedTurn.IsPairedResume(turn);
            internal static void PairedEndPostfix(TurnController __instance) => PatchBridge.UnifiedTurn?.FinishPairedActivation(__instance);
            internal static void PairedPhaseChanged(TurnController turn) => PatchBridge.UnifiedTurn?.SynchronizePartnerPhase(turn);
            internal static bool SkipPairedCandidate(CombatController.TBUnitInfo candidate) =>
                PatchBridge.UnifiedTurn != null && PatchBridge.UnifiedTurn.SuppressPairedCandidate(candidate);
            internal static bool IsPartnerContext(TurnController turn) => PatchBridge.UnifiedTurn != null && PatchBridge.UnifiedTurn.IsPartnerContext(turn);
            internal static bool PairedActorEligible(UnitEntityData actor, UnitCommand command) =>
                PatchBridge.UnifiedTurn == null ? actor.IsCurrentUnit() : PatchBridge.UnifiedTurn.NativeActorEligibleForCommand(actor, command);
            internal static bool PairedActivity(TurnController turn) =>
                PatchBridge.UnifiedTurn == null ? turn.IsActed() : PatchBridge.UnifiedTurn.HasPairedActivity(turn);
            internal static bool PairedProneActor(UnitEntityData actor) =>
                PatchBridge.UnifiedTurn == null ? actor.IsCurrentUnit() : PatchBridge.UnifiedTurn.IsPairedProneActor(actor);
            internal static float PairedReadiness(UnitEntityData actor) =>
                PatchBridge.UnifiedTurn == null ? actor.GetTimeToNextTurn() : PatchBridge.UnifiedTurn.PairedNativeReadiness(actor);
            internal static void PairedStandardEndWrite(UnitCombatState.Cooldowns cooldown, float value) =>
                cooldown.StandardAction = PatchBridge.UnifiedTurn?.NativeCompletionValue(cooldown, cooldown.StandardAction, value) ?? value;
            internal static void PairedStandardFinalWrite(UnitCombatState.Cooldowns cooldown, float value) =>
                cooldown.StandardAction = PatchBridge.UnifiedTurn?.NativeFinalStandardValue(cooldown, cooldown.StandardAction, value) ?? value;
            internal static void PairedMoveEndWrite(UnitCombatState.Cooldowns cooldown, float value) =>
                cooldown.MoveAction = PatchBridge.UnifiedTurn?.NativeCompletionValue(cooldown, cooldown.MoveAction, value) ?? value;
            internal static void PairedSwiftEndWrite(UnitCombatState.Cooldowns cooldown, float value) =>
                cooldown.SwiftAction = PatchBridge.UnifiedTurn?.NativeCompletionValue(cooldown, cooldown.SwiftAction, value) ?? value;
            internal static void PairedPreparationConfusion(UnitConfusionController controller, TurnController turn)
            {
                if (PatchBridge.UnifiedTurn == null) controller.Tick();
                else PatchBridge.UnifiedTurn.TickPreparationConfusion(controller, turn);
            }
            internal static bool PairedBuffTimerActor(UnitEntityData current, UnitEntityData actor) =>
                ReferenceEquals(current, actor) ||
                (PatchBridge.Persistence?.CombatRestorationPending != true &&
                    PatchBridge.UnifiedTurn?.OwnsPartnerRoundEffects(current, actor) == true);
            internal static IEnumerable<CodeInstruction> PairedBuffTimerTranspiler(IEnumerable<CodeInstruction> instructions) =>
                PairedActivationTranspilers.BuffTimerEligibility(instructions, Hook(nameof(PairedBuffTimerActor)));
            private static MethodInfo Hook(string name) => typeof(PatchMethods).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic);
            internal static IEnumerable<CodeInstruction> PairedSelectorTranspiler(IEnumerable<CodeInstruction> instructions) =>
                PairedActivationTranspilers.Selector(instructions, Hook(nameof(SkipPairedCandidate)));
            internal static IEnumerable<CodeInstruction> PairedEligibilityTranspiler(IEnumerable<CodeInstruction> instructions) =>
                PairedActivationTranspilers.ActorEligibility(instructions, Hook(nameof(PairedActorEligible)), true);
            internal static IEnumerable<CodeInstruction> PairedProneTranspiler(IEnumerable<CodeInstruction> instructions) =>
                PairedActivationTranspilers.ActorEligibility(instructions, Hook(nameof(PairedProneActor)), false);
            internal static IEnumerable<CodeInstruction> PairedReadinessTranspiler(IEnumerable<CodeInstruction> instructions) =>
                PairedActivationTranspilers.Readiness(instructions, Hook(nameof(PairedReadiness)));
            internal static bool PairedConditionActor(UnitEntityData actor, UnitCommand command) =>
                PatchBridge.UnifiedTurn?.IsNativeConditionCommandActor(actor, command) ?? actor.IsCurrentUnit();
            internal static void PairedConditionForfeit(TurnController context, bool setCooldowns, UnitCommand command)
            {
                if (PatchBridge.UnifiedTurn == null) context.ForceToEnd(setCooldowns);
                else PatchBridge.UnifiedTurn.ForfeitNativeConditionActor(context, setCooldowns, command);
            }
            internal static void PairedForfeitPhase(TurnController context)
            {
                if (PatchBridge.UnifiedTurn?.CompleteNativeConditionForfeit(context) != true)
                    UnifiedMountedTurnCoordinator.CompleteNativeForfeitPhase(context);
            }
            internal static IEnumerable<CodeInstruction> PairedConditionActionTranspiler(IEnumerable<CodeInstruction> instructions) =>
                PairedActivationTranspilers.ActorConditionAction(instructions, Hook(nameof(PairedConditionActor)), Hook(nameof(PairedConditionForfeit)));
            internal static IEnumerable<CodeInstruction> PairedForfeitDebtTranspiler(IEnumerable<CodeInstruction> instructions) =>
                PairedActivationTranspilers.ActorForfeitPhase(
                    PairedActivationTranspilers.CompletionDebt(instructions, Hook(nameof(PairedStandardEndWrite)), Hook(nameof(PairedMoveEndWrite)), Hook(nameof(PairedSwiftEndWrite)), true),
                    Hook(nameof(PairedForfeitPhase)));
            internal static IEnumerable<CodeInstruction> PairedEndDebtTranspiler(IEnumerable<CodeInstruction> instructions) =>
                PairedActivationTranspilers.CompletionDebt(instructions, Hook(nameof(PairedStandardFinalWrite)), Hook(nameof(PairedMoveEndWrite)), Hook(nameof(PairedSwiftEndWrite)), false);
            internal static IEnumerable<CodeInstruction> PairedPreparationTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator) =>
                PairedActivationTranspilers.Preparation(instructions, generator, Hook(nameof(IsPartnerContext)), Hook(nameof(PairedPreparationConfusion)), Hook(nameof(IsPairedResume)));
            internal static IEnumerable<CodeInstruction> PairedActivityTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator) =>
                PairedActivationControlTranspilers.TickInput(
                    PairedActivationTranspilers.PreparingActivity(instructions, Hook(nameof(PairedActivity)), Hook(nameof(PairedPhaseChanged))),
                    generator, Hook(nameof(PairedPreserveSelection)), Hook(nameof(PairedInputContext)));
            internal static IEnumerable<CodeInstruction> PairedControllerInputTranspiler(IEnumerable<CodeInstruction> instructions) =>
                PairedActivationControlTranspilers.ControllerInput(instructions, Hook(nameof(PairedInputContext)));
            internal static IEnumerable<CodeInstruction> PairedVmConstructorTranspiler(IEnumerable<CodeInstruction> instructions) =>
                PairedActivationControlTranspilers.ViewModelContext(instructions, Hook(nameof(PairedVmContext)), 6);
            internal static IEnumerable<CodeInstruction> PairedVmReaderTranspiler(IEnumerable<CodeInstruction> instructions) =>
                PairedActivationControlTranspilers.ViewModelContext(instructions, Hook(nameof(PairedVmContext)), 1);
            internal static IEnumerable<CodeInstruction> PairedFullAttackInputTranspiler(IEnumerable<CodeInstruction> instructions) =>
                PairedActivationControlTranspilers.FullAttackRestriction(instructions, Hook(nameof(PairedActionContextActor)), Hook(nameof(PairedActorActionContext)));
            internal static IEnumerable<CodeInstruction> PairedPathUnitTranspiler(IEnumerable<CodeInstruction> instructions) =>
                PairedActivationControlTranspilers.PathUnitReads(instructions, Hook(nameof(PairedPathInputUnit)), 1);
            internal static IEnumerable<CodeInstruction> PairedPathSettingsTranspiler(IEnumerable<CodeInstruction> instructions) =>
                PairedActivationControlTranspilers.PathUnitReads(
                    PairedActivationControlTranspilers.ControllerInput(instructions, Hook(nameof(PairedInputContext))),
                    Hook(nameof(PairedPathInputUnit)), 2);

            internal static void NativeRoundStatePrefix(UnitCombatState __instance)
            {
                PatchBridge.UnifiedTurn?.HandleNativeRoundState(__instance);
            }

            internal static void TurnPreparePostfix(TurnController __instance, bool __state)
            {
                if (__state) PatchBridge.UnifiedTurn?.HandleTurnPrepared(__instance);
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
                return PatchBridge.Service == null || PatchBridge.Service.NormalizeSingleSelection(ref unit, single,
                    PatchBridge.UnifiedTurn?.MaySelectPairedPartner(unit?.EntityData) ?? false);
            }

            internal static void MultiSelectPrefix(ref IEnumerable<UnitEntityView> views)
            {
                PatchBridge.Service?.NormalizeMultiSelection(ref views,
                    PatchBridge.UnifiedTurn?.MaySelectPairedPartner(PatchBridge.Service?.Mount) ?? false);
            }

            internal static void StopOrHoldPrefix()
            {
                if (PointerController.SimulatingClick) { return; }
                PatchBridge.Combat?.CancelSelectedInput("stop or hold");
                PatchBridge.Service?.ForwardStopOrHold();
            }

            internal static bool ContinuousMovePrefix(ref UnitEntityData executor)
            {
                if (PointerController.SimulatingClick) { return true; }
                if (PatchBridge.Service != null && PatchBridge.Service.IsExactActivePairUnit(executor))
                {
                    PatchBridge.Combat?.Cancel("continuous movement replaced the active mounted combat intent");
                }
                return PatchBridge.Service == null || PatchBridge.Service.RouteContinuousMove(ref executor);
            }

            internal static void NativeCombatEndPrefix(out UnitEntityData __state)
            {
                __state = PatchBridge.Service?.CaptureNativeCombatEndMount();
            }

            internal static void NativeCombatEndPostfix(CombatController __instance, UnitEntityData __state)
            {
                PatchBridge.Service?.CompleteNativeCombatEndMountLease(__instance, __state);
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
                if (PatchBridge.UnifiedTurn != null &&
                    PatchBridge.UnifiedTurn.TryMoveNativePreparationActor(__instance, ref deltaTime, out result))
                { __result = result; return false; }
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

            internal static void ProjectileHitPostfix(Kingmaker.Controllers.Projectiles.Projectile __instance)
            {
                if (PatchBridge.Persistence?.Enabled == true) NativeSaveEffectBoundary.HitCompleted(__instance);
            }

            internal static bool CombatPreparePrefix() => PatchBridge.Persistence?.CombatRestorationPending != true;

            internal static bool CombatCooldownPrefix(UnitEntityData unit, out float __state)
            {
                __state = unit?.CombatState == null
                    ? float.NaN
                    : unit.CombatState.Cooldown.Initiative;
                return PatchBridge.Persistence?.CombatRestorationPending != true;
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

            internal static IEnumerable<CodeInstruction> NativeArchiveCommitTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod) =>
                NativeSaveWorkerBoundary.Transform(NativeMountedArchiveCommit.Transform(instructions), __originalMethod);

            internal static IEnumerable<CodeInstruction> CombatSaveAdmissionTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                var getter = typeof(Kingmaker.Player).GetProperty("IsInCombat").GetGetMethod();
                if (getter.MetadataToken != 0x06000DBF) throw new MissingMethodException("Native combat save predicate differs.");
                var replacement = typeof(PatchMethods).GetMethod(nameof(CombatBlocksSave), BindingFlags.Static | BindingFlags.NonPublic);
                var count = 0;
                var result = new List<CodeInstruction>();
                foreach (var instruction in instructions)
                {
                    if (Equals(instruction.operand, getter))
                    {
                        instruction.opcode = OpCodes.Call;
                        instruction.operand = replacement;
                        count++;
                    }
                    result.Add(instruction);
                }
                if (count != 1) throw new InvalidOperationException("Expected exactly one native combat save gate.");
                return result;
            }

            internal static bool CombatBlocksSave(Kingmaker.Player player) =>
                PatchBridge.Persistence?.NativeCombatBlocksSave(player) ?? player.IsInCombat;

            internal static bool SavePrefix(SaveManager __instance, SaveInfo saveInfo, bool forceAuto,
                ref IEnumerator<object> __result, out bool __state)
            {
                __state = false;
                RuntimeAutomationHost.ObserveSaveRequest();
                bool suppressed;
                var authorized = AuthorizeSaveBoundary(RuntimeSaveOperation.Write, __instance, saveInfo, ref __result, out suppressed);
                if (!authorized && !suppressed) return false;
                if (authorized && PatchBridge.Persistence != null) { __state = true; return true; }
                // Retain the explicitly armed historical suppression probe. An
                // unauthorized request must not dismount or change live controls.
                if (!GuardNativeBoundary(NativeLifecycleBoundary.SaveRequest, CleanupTrigger.SaveRequested, "SaveManager.SaveRoutine Harmony12 prefix"))
                {
                    PatchBridge.SaveAuthorization?.ReportBoundaryFailure(RuntimeSaveOperation.Write, "relationship service reported residue");
                    __result = EmptyRoutine();
                    return false;
                }
                __state = authorized;
                return authorized;
            }

            internal static void SavePostfix(SaveInfo saveInfo, ref IEnumerator<object> __result, bool __state)
            {
                if (__state && __result != null)
                    __result = PatchBridge.Persistence != null ? PatchBridge.Persistence.WrapSaveRoutine(__result, saveInfo) :
                        PatchBridge.NativeControls == null ? __result : PatchBridge.NativeControls.WrapSaveRoutine(__result);
            }

            internal static void AreaTransitionPrefix(Kingmaker.Blueprints.Area.BlueprintArea area, SaveInfo saveInfo) =>
                PatchBridge.Persistence?.BeginAreaTransition(area, saveInfo);
            internal static void AreaEntitiesReadyPostfix() => PatchBridge.Persistence?.RestoreAreaPair();

            internal static bool GameLoadAdmissionPrefix(SaveInfo saveInfo)
            {
                var authorization = PatchBridge.SaveAuthorization;
                if (authorization != null && authorization.IsActive)
                {
                    try
                    {
                        var reason = authorization.ValidateLoadBeforeWorldReplacement(
                            saveInfo == null ? null : NativePersistenceIsolation.Project(saveInfo),
                            Kingmaker.Game.Instance.SaveManager.SavePath);
                        if (reason != null)
                        {
                            authorization.ReportFatalViolation(RuntimeSaveOperation.Load, reason);
                            return false;
                        }
                    }
                    catch (Exception exception)
                    {
                        authorization.ReportFatalViolation(RuntimeSaveOperation.Load,
                            "Early native load identity inspection failed (" + exception.GetType().Name + ").");
                        return false;
                    }
                }
                return PatchBridge.Persistence?.CanLoadBeforeWorldReplacement(saveInfo) != false;
            }

            internal static bool GameMainMenuLoadAdmissionPrefix(SaveInfo saveInfo) =>
                GameLoadAdmissionPrefix(saveInfo) && PatchBridge.Persistence?.PrepareForNativeWorldDisposal() != false;

            internal static bool GameSmokeLoadAdmissionPrefix(SaveInfo save) => GameLoadAdmissionPrefix(save);

            internal static bool LoadPrefix(SaveManager __instance, SaveInfo saveInfo, bool isSmokeTest, ref IEnumerator<object> __result, out bool __state)
            {
                __state = false;
                RuntimeAutomationHost.ObserveLoadRequest();
                if (!AuthorizeSaveBoundary(RuntimeSaveOperation.Load, __instance, saveInfo, ref __result)) return false;
                if (PatchBridge.Persistence == null && !GuardNativeBoundary(NativeLifecycleBoundary.LoadStart, CleanupTrigger.LoadRequested, "SaveManager.LoadRoutine Harmony12 prefix"))
                {
                    PatchBridge.SaveAuthorization?.ReportBoundaryFailure(RuntimeSaveOperation.Load, "relationship service reported residue");
                    __result = EmptyRoutine();
                    return false;
                }

                __state = true;
                return true;
            }

            internal static void SaveWorkerPostfix(SaveInfo saveInfo) =>
                NativePersistenceIsolation.ObserveWorkerComplete(saveInfo);

            internal static void SavePreparedPostfix(SaveInfo save)
            {
                NativePersistenceIsolation.ObservePreparedWrite(save);
                PatchBridge.Persistence?.ObservePreparedSave(save);
            }

            internal static void NativeSaveHeaderPrefix(ISaver __instance, string name) =>
                PatchBridge.Persistence?.BeforeNativeHeader(__instance, name);

            internal static void ActorPostLoadPostfix(UnitEntityData __instance) =>
                PatchBridge.Persistence?.RestoreActorAfterPostLoad(__instance);

            internal static void LoadPostfix(SaveInfo saveInfo, ref IEnumerator<object> __result, bool __state)
            {
                if (__state)
                {
                    if (PatchBridge.Persistence != null)
                        __result = PatchBridge.Persistence.WrapLoadRoutine(__result, saveInfo);
                    __result = NativePersistenceIsolation.WrapReadOnlyLoad(__result, saveInfo.FolderName);
                }
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
                bool suppressed;
                return AuthorizeSaveBoundary(operation, saveManager, saveInfo, ref result, out suppressed);
            }

            private static bool AuthorizeSaveBoundary(RuntimeSaveOperation operation, SaveManager saveManager, SaveInfo saveInfo, ref IEnumerator<object> result, out bool suppressed)
            {
                suppressed = false;
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
                    if (authorization.IsPersistenceMode && operation == RuntimeSaveOperation.Write && saveInfo != null && !saveInfo.IsActuallySaved)
                        target = NativePersistenceIsolation.ProjectNewRequest(saveManager, saveInfo);
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
                    suppressed = true;
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
