using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Harmony12;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Ecnchantments;
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
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.View;
using Kingmaker.View.MapObjects;
using Kingmaker.Visual.Animation;
using Kingmaker.Visual.Animation.Kingmaker;
using Kingmaker.Visual.CharacterSystem;
using Kingmaker.Visual.Particles;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Diagnostics;
using KingmakerMountedCombat.Logging;
using TurnBased.Controllers;

namespace KingmakerMountedCombat.Integration
{
    internal sealed class MountedPatchController : IDisposable
    {
        private const string HarmonyId = "KingmakerMountedCombat.Feasibility";
        // Kingmaker 2.1.7b UnitUISettings.CleanupBadSlots and GetSlot(int, UnitEntityData), pinned in Test-AssemblyContracts.ps1.
        internal const int NativeCleanupBadSlotsToken = 0x06003004;
        internal const int NativeGetSlotToken = 0x06002FFE;
        // CleanupBadSlots writes only through its own calls of the non-virtual UnitUISettings.SetSlot, three of them
        // (rebind, activatable rebind, empty), pinned in Test-AssemblyContracts.ps1.
        internal const int NativeCleanupSetSlotCalls = 3;
        // UnitUISettings.SetSlot(MechanicActionBarSlot, int), the write every action bar edit and every cleanup write uses.
        internal const int NativeSetSlotToken = 0x06002FFF;
        private static readonly Guid ExpectedKingmakerMvid = new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7");
        private readonly HarmonyInstance harmony;
        private bool disposed;

        public MountedPatchController(GameMountedRelationshipService service, MountedPlayerActionController playerAction, MountedCombatController combat, UnifiedMountedTurnCoordinator unifiedTurn, NativeMountedControlService nativeControls, MountedPersistenceService persistence, MountedAnimationAdapter animation, MountedDollRoomIkAdapter dollRoomIk, RuntimeSaveAuthorization saveAuthorization, NativeLifecycleDeliveryLedger lifecycleLedger, IModLogger logger, MountedCombatDefenseService defense, HorseCompanionBlueprintService horseCompanion, MountedPairPresentation presentation)
        {
            PatchBridge.Service = service ?? throw new ArgumentNullException(nameof(service));
            PatchBridge.PlayerAction = playerAction ?? throw new ArgumentNullException(nameof(playerAction));
            PatchBridge.Combat = combat ?? throw new ArgumentNullException(nameof(combat));
            PatchBridge.ChargeSafety = new MountedChargeSafetyService(service, nativeControls, combat.RejectPairedControl);
            PatchBridge.UnifiedTurn = unifiedTurn ?? throw new ArgumentNullException(nameof(unifiedTurn));
            PatchBridge.NativeControls = nativeControls ?? throw new ArgumentNullException(nameof(nativeControls));
            PatchBridge.Persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
            PatchBridge.Animation = animation ?? throw new ArgumentNullException(nameof(animation));
            PatchBridge.DollRoomIk = dollRoomIk ?? throw new ArgumentNullException(nameof(dollRoomIk));
            PatchBridge.SaveAuthorization = saveAuthorization ?? throw new ArgumentNullException(nameof(saveAuthorization));
            PatchBridge.LifecycleLedger = lifecycleLedger ?? throw new ArgumentNullException(nameof(lifecycleLedger));
            PatchBridge.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            PatchBridge.Defense = defense ?? throw new ArgumentNullException(nameof(defense));
            PatchBridge.HorseCompanion = horseCompanion ?? throw new ArgumentNullException(nameof(horseCompanion));
            PatchBridge.Presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
            harmony = HarmonyInstance.Create(HarmonyId);
            try
            {
                var observedMvid = typeof(UnitEntityData).Assembly.ManifestModule.ModuleVersionId;
                if (observedMvid != ExpectedKingmakerMvid)
                {
                    throw new InvalidOperationException("Exact Kingmaker Assembly-CSharp MVID mismatch: " + observedMvid + ".");
                }

                PatchExact(typeof(Kingmaker.Assets.UI.LevelUp.CharBSelectorLayer), "FillFeatureDataLight", 0x06009F79,
                    new[] { typeof(Kingmaker.UnitLogic.Class.LevelUp.FeatureSelectionState), typeof(bool) },
                    nameof(PatchMethods.FeatSelectorRefreshPrefix), nameof(PatchMethods.FeatSelectorRefreshPostfix));
                PatchExact(typeof(Kingmaker.Assets.UI.LevelUp.CharBuildSelectorItem), "ReInit", 0x06009FA8,
                    new[] { typeof(Kingmaker.UnitLogic.Class.LevelUp.FeatureSelectionState), typeof(Kingmaker.Blueprints.Classes.Selection.IFeatureSelectionItem),
                        typeof(Kingmaker.Assets.UI.LevelUp.CharBSelectorLayer), typeof(UnityEngine.Events.UnityAction) },
                    nameof(PatchMethods.FeatRowPreviewRebindPrefix));
                // PET-01: the sheet's name line offers Rename Companion for the companion it is showing.
                PatchExact(typeof(Kingmaker.UI.ServiceWindow.CharacterScreen.CharacterScreenController), "SetupInfo", 0x060049BE,
                    new[] { typeof(bool) }, null, nameof(PatchMethods.CharacterSheetNamePostfix));
                // City companion inspection: the capital's full-screen Inventory/Character/Spellbook roster lists the
                // reserve owners' own animal companions (a UI projection only; see CityCompanionRosterProjection).
                PatchExact(typeof(Kingmaker.UI.Common.UIUtility), "GetGroup", CityCompanionRosterAdapter.NativeGetGroupToken,
                    new[] { typeof(bool), typeof(bool) }, null, nameof(PatchMethods.CityCompanionRosterPostfix));
                PatchExact(typeof(Kingmaker.RuleSystem.Rules.RuleAttackRoll), "OnTrigger", 0x06007173, new[] { typeof(Kingmaker.RuleSystem.RulebookEventContext) }, null, null, nameof(PatchMethods.MountedCombatDefenseTranspiler));
                PatchExact(typeof(Kingmaker.UnitLogic.Class.LevelUp.Actions.LevelUpHelper), "AddFeatures", 0x060028C6,
                    new[] { typeof(Kingmaker.UnitLogic.Class.LevelUp.LevelUpState), typeof(Kingmaker.UnitLogic.UnitDescriptor), typeof(System.Collections.Generic.IList<Kingmaker.Blueprints.Classes.BlueprintFeatureBase>), typeof(Kingmaker.Blueprints.BlueprintScriptableObject), typeof(int) },
                    nameof(PatchMethods.DivineBondLevelEntryFeatures));
                PatchExact(typeof(ClickGroundHandler), "RunCommand", 0x060093DC, new[] { typeof(UnitEntityData), typeof(UnityEngine.Vector3), typeof(float?), typeof(float), typeof(float), typeof(bool) }, nameof(PatchMethods.GroundCommandPrefix), nameof(PatchMethods.GroundCommandPostfix));
                PatchExact(typeof(Kingmaker.Player), "DismissCompanion", 0x06000DD7,
                    new[] { typeof(UnitEntityData) }, null, null,
                    nameof(PatchMethods.MountedDismissInventoryTranspiler));
                // The only gameplay caller of LoadingProcess.StopAll. Deferred while an
                // owned archive worker can still commit, so the save is never
                // abandoned mid-write on the way to the main menu.
                PatchExact(typeof(Kingmaker.Game), "ResetToMainMenu", 0x06000CDD,
                    new[] { typeof(string), typeof(Kingmaker.Blueprints.Area.BlueprintAreaPreset) },
                    nameof(PatchMethods.ResetToMainMenuPrefix));
                // Known issues Sprint 6 (review of 2ae4cc8b, finding 4): its one internal Run call is scoped exception-safely.
                PatchExact(typeof(UnitCommands), "Run", 0x060026B2, new[] { typeof(UnitCommand) }, nameof(PatchMethods.UnitCommandRunPrefix),
                    nameof(PatchMethods.UnitCommandRunPostfix), nameof(PatchMethods.NativeRunScopeTranspiler));
                // Known issues Sprint 6, review of fa533e97 (R18): native's capability terminals are captured where native
                // takes them. Each transpiler keeps native's own calls and values; it only records which branch ended a
                // pair command.
                PatchExact(typeof(UnitActionController), "TickCommandTurnBased", NativeCommandTerminalBranches.TickCommandTurnBasedToken,
                    new[] { typeof(UnitCommand) }, null, null, nameof(PatchMethods.NativeTurnBasedTerminalTranspiler));
                PatchExact(typeof(UnitActionController), "TickCommand", NativeCommandTerminalBranches.TickCommandToken,
                    new[] { typeof(UnitCommand), typeof(bool) }, null, null, nameof(PatchMethods.NativeTickTerminalTranspiler));
                PatchExact(typeof(UnitAttack), "get_ShouldBeInterrupted", NativeCommandTerminalBranches.AttackShouldBeInterruptedToken,
                    Type.EmptyTypes, null, null, nameof(PatchMethods.NativeAttackBanTranspiler));
                // Known issues sprint 3 continuation (M05): this overload is the one native admission both the public
                // Run and queue promotion (RemoveFinishedAndUpdateQueue) reach; its postfix observes what was admitted.
                PatchExact(typeof(UnitCommands), "Run", 0x060026B3,
                    new[] { typeof(UnitCommand), typeof(bool), typeof(bool) }, nameof(PatchMethods.InternalRunPrefix),
                    nameof(PatchMethods.InternalRunPostfix), nameof(PatchMethods.NativeRunHoldTranspiler));
                // PR-1 review R1-F11: ClickUnitHandler.OnClick assigns the clicked unit as the manual target after its Run; the click is
                // bracketed so that assignment is attributed (observation only; native behavior unchanged).
                PatchExact(typeof(Kingmaker.Controllers.Clicks.Handlers.ClickUnitHandler), "OnClick", 0x060093ED,
                    new[] { typeof(UnityEngine.GameObject), typeof(UnityEngine.Vector3), typeof(int), typeof(bool), typeof(bool) },
                    nameof(PatchMethods.ClickUnitPrefix), nameof(PatchMethods.ClickUnitPostfix), nameof(PatchMethods.ClickAssignmentTranspiler));
                // PR #70 R1-F7 (round 4): the input dispatch that evaluates IgnoreClick and runs the click's handler (observation only).
                PatchExact(typeof(PointerController), "Tick", 0x060093BA, Type.EmptyTypes, null, null, nameof(PatchMethods.PointerTickTranspiler));
                PatchExact(typeof(UnitCommands), "AddToQueueInternal", 0x060026B8,
                    new[] { typeof(UnitCommand), typeof(bool) }, nameof(PatchMethods.ChargeEnqueuePrefix), nameof(PatchMethods.QueueAcceptedPostfix));
                // Review R1-F8: the one native Hold write of a completed move (player-faction executor, real-time combat).
                PatchExact(typeof(UnitMoveTo), "OnAction", 0x06002702, Type.EmptyTypes, nameof(PatchMethods.MoveActionHoldPrefix),
                    nameof(PatchMethods.MoveActionHoldPostfix));
                PatchExact(typeof(UnitCommands), "AddToQueueOrRun", 0x060026B9,
                    new[] { typeof(UnitCommand), typeof(bool) }, nameof(PatchMethods.ChargeEnqueueOrRunPrefix));
                PatchExact(typeof(UnitCommand), "Start", 0x060027A5, Type.EmptyTypes, nameof(PatchMethods.SaveCommandStartPrefix));
                PatchExact(typeof(UnitCommand), "Tick", 0x060027A7, Type.EmptyTypes, nameof(PatchMethods.ChargeExecutionPrefix));
                // Horse speed repair: the native accelerated group move is offered to selected units only.
                PatchExact(typeof(UnitCommand), "AccelerateIfPathIsLong", 0x060027B4, null,
                    nameof(PatchMethods.NativeGroupAccelerationPrefix), nameof(PatchMethods.NativeGroupAccelerationPostfix));
                // Known issues sprint 3 (M04): the whole body of this method places the unit's destination pointer.
                PatchExact(typeof(UnitEntityView), "OnMovementStarted", 0x0600184E, new[] { typeof(UnityEngine.Vector3), typeof(bool) },
                    nameof(PatchMethods.CarriedRiderPointerPrefix));
                // Known issues M04 closeout (review of b1a88b4e, R3): a pointer placement is recorded where it happens.
                PatchExact(typeof(Kingmaker.UI.ClickPointerManager), "AddPointer", 0x06002D62, new[] { typeof(UnityEngine.Vector3), typeof(UnitEntityData) },
                    null, nameof(PatchMethods.PointerPlacedPostfix));
                // Known issues sprint 3 (M02): the gamepad's follower acceleration pass walks selected units only.
                // Continuation (M05): the same method's one driven-unit read is answered with the pair's physical mover.
                PatchExact(typeof(Kingmaker.UI._ConsoleUI.InputLayers.InGameLayer.InGameInputLayer), "UpdateMovement", 0x06005D0C,
                    Type.EmptyTypes, null, null, nameof(PatchMethods.GamepadMovementTranspiler));
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
                PatchExact(typeof(UnitUseAbility), "OnAction", 0x06002737, Type.EmptyTypes, null,
                    nameof(PatchMethods.NativeAbilityActionPostfix), nameof(PatchMethods.NativeAbilityActionTranspiler));
                PatchExact(typeof(UnitEntityView), "OnMovementInterrupted", 0x0600184F, new[] { typeof(UnityEngine.Vector3) },
                    nameof(PatchMethods.NativeMountMovementInterruptedPrefix), nameof(PatchMethods.NativeMountMovementInterruptedPostfix));
                PatchExact(typeof(Kingmaker.UnitLogic.Abilities.AbilityData), "get_IsSuitableForAutoUse", 0x06002B30,
                    Type.EmptyTypes, null, nameof(PatchMethods.RelationshipControlAutoUsePostfix));
                PatchExact(typeof(Kingmaker.UI.UnitSettings.MechanicActionBarSlotAbility), "OnClick", 0x06002F5D,
                    Type.EmptyTypes, nameof(PatchMethods.PublicSlotClickPrefix), nameof(PatchMethods.PublicSlotClickPostfix));
                // Toolbar persistence (observation only): native CleanupBadSlots replaces every slot whose ability fact is gone
                // or inactive (empty, or rebound to a live fact of the same blueprint); its bracket tells KMC which public
                // slots native emptied, so that emptying is never read as the player's removal.
                // Its SetSlot call sites are redirected to record each write as it completes, so a cleanup that throws is
                // settled from what it actually wrote (MountedToolbarLifecycle.RecordNativeSlotWrite).
                PatchExact(typeof(Kingmaker.UI.UnitSettings.UnitUISettings), "CleanupBadSlots", NativeCleanupBadSlotsToken,
                    Type.EmptyTypes, nameof(PatchMethods.BadSlotCleanupPrefix), nameof(PatchMethods.BadSlotCleanupPostfix),
                    nameof(PatchMethods.BadSlotCleanupWritesTranspiler));
                // Before any write reaches a bar (a player's edit, a cleanup write), a companion retirement made while that bar
                // was never read is pinned to what the bar held, so the write itself is never taken for an older binding.
                PatchExact(typeof(Kingmaker.UI.UnitSettings.UnitUISettings), "SetSlot", NativeSetSlotToken,
                    new[] { typeof(Kingmaker.UI.UnitSettings.MechanicActionBarSlot), typeof(int) }, nameof(PatchMethods.SlotWritePrefix));
                // GetSlot replaces a bad slot too (that one slot only).
                PatchExact(typeof(Kingmaker.UI.UnitSettings.UnitUISettings), "GetSlot", NativeGetSlotToken,
                    new[] { typeof(int), typeof(UnitEntityData) }, nameof(PatchMethods.SlotReadPrefix), nameof(PatchMethods.BadSlotCleanupPostfix));
                // Known issues Sprint 7 (U03): the class panel's rows for the shown character's bond.
                PatchExact(typeof(Kingmaker.UI.ServiceWindow.CharacterScreen.CharSComponentClassBlock), "GetUIProgressionLayout", 0x06004A32,
                    Type.EmptyTypes, null, null, nameof(PatchMethods.DivineBondClassRowsTranspiler));
                // Known issues Sprint 7 (U02): the selected mark of the exact public slot whose delivery is targeting.
                PatchExact(typeof(Kingmaker.UI.UnitSettings.MechanicActionBarSlotAbility), "UpdateSlotInternal", 0x06002F68,
                    new[] { typeof(Kingmaker.UI.ActionBar.ActionBarSlot) }, null, nameof(PatchMethods.PublicSlotSelectionPostfix));
                PatchExact(typeof(SelectionManager), "SelectUnit", 0x060034F0, new[] { typeof(UnitEntityView), typeof(bool), typeof(bool), typeof(bool) }, nameof(PatchMethods.SelectUnitPrefix));
                PatchExact(typeof(SelectionManager), "MultiSelect", 0x060034F5, new[] { typeof(IEnumerable<UnitEntityView>), typeof(bool) }, nameof(PatchMethods.MultiSelectPrefix));
                PatchExact(typeof(ClickMapObjectHandler), "TryInteract", 0x060093E5,
                    new[] { typeof(InteractionComponent), typeof(List<UnitEntityData>), typeof(bool) },
                    nameof(PatchMethods.TrapClickScopePrefix), nameof(PatchMethods.TrapClickScopePostfix));
                PatchExact(typeof(UnitCommands), "InterruptMove", 0x060026C2, Type.EmptyTypes, null, nameof(PatchMethods.InterruptMovePostfix));
                PatchExact(typeof(Kingmaker.Player), "ReInitPartyCharacters", 0x06000DED,
                    new[] { typeof(List<Kingmaker.EntitySystem.Entities.UnitReference>) },
                    nameof(PatchMethods.PartyReinitPrefix), nameof(PatchMethods.PartyReinitPostfix));
                PatchExact(typeof(SelectionManagerBase), "Stop", 0x060000B9, Type.EmptyTypes, nameof(PatchMethods.StopPrefix));
                PatchExact(typeof(SelectionManagerBase), "Hold", 0x060000BA, Type.EmptyTypes, nameof(PatchMethods.HoldPrefix));
                PatchExact(typeof(UnitMoveContiniously), "Init", 0x060026F0, new[] { typeof(UnitEntityData) },
                    nameof(PatchMethods.ContinuousMovePrefix), nameof(PatchMethods.ContinuousMovePostfix));
                // The native end of a continuous command clears the override of its executor's current view; a command
                // a pair bound clears only the agent it was bound with.
                PatchExact(typeof(UnitMoveContiniously), "OnEnded", 0x060026F2, new[] { typeof(bool) }, null, null,
                    nameof(PatchMethods.ContinuousMoveEndTranspiler));
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
                PatchExact(typeof(UnitEntityData), "JoinCombat", 0x06008355, Type.EmptyTypes,
                    nameof(PatchMethods.RetainedNativeJoinPrefix));
                var join = typeof(UnitEntityData).GetMethod("JoinCombat", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                    null, Type.EmptyTypes, null);
                var joinPrefix = typeof(PatchMethods).GetMethod(nameof(PatchMethods.RetainedNativeJoinPrefix), BindingFlags.NonPublic | BindingFlags.Static);
                var joinPatches = harmony.GetPatchInfo(join);
                if (joinPrefix?.ReturnType != typeof(bool) || joinPatches == null ||
                    joinPatches.Prefixes.Count(p => p.owner == HarmonyId && p.patch == joinPrefix) != 1)
                    throw new InvalidOperationException("Exact retained native JoinCombat bool guard did not install once.");
                PatchExact(typeof(SaveManager), "IsSaveAllowed", 0x06008028, Type.EmptyTypes, null, null, nameof(PatchMethods.CombatSaveAdmissionTranspiler));
                PatchExact(typeof(SaveManager), "SerializeAndSaveThread", 0x0600802A,
                    new[] { typeof(SaveInfo), typeof(SaveCreateDTO), typeof(SaveInfo) },
                    nameof(PatchMethods.SaveWorkerPrefix), null, nameof(PatchMethods.NativeArchiveCommitTranspiler));
                PatchExact(typeof(SaveManager), "SaveRoutine", 0x06008029, new[] { typeof(SaveInfo), typeof(bool) }, nameof(PatchMethods.SavePrefix), nameof(PatchMethods.SavePostfix));
                // Save/load repair: observation only. Game.SaveGame refuses through IsSaveAllowed with a UI warning and no
                // log; this records each request and the refusing term before that native check.
                PatchExact(typeof(Kingmaker.Game), "SaveGame", 0x06000CE4, new[] { typeof(SaveInfo), typeof(Action) },
                    nameof(PatchMethods.SaveRequestObservationPrefix));
                PatchExact(typeof(Kingmaker.Game), "LoadArea", 0x06000CD5,
                    new[] { typeof(Kingmaker.Blueprints.Area.BlueprintArea), typeof(Kingmaker.Blueprints.Area.BlueprintAreaEnterPoint),
                        typeof(AutoSaveMode), typeof(bool), typeof(SaveInfo) }, nameof(PatchMethods.AreaTransitionPrefix));
                PatchExact(typeof(Kingmaker.Game), "OnAreaLoaded", 0x06000CD7, Type.EmptyTypes,
                    null, nameof(PatchMethods.AreaEntitiesReadyPostfix));
                // Ratnook door repair: Game.LoadArea(enterPoint, mode) 06000CC9 sends an enter point of the loaded area to
                // Game.Teleport, which never reaches the full LoadArea or OnAreaLoaded. Its dispatch binds the transfer's
                // route; ITeleportHandler.HandlePartyTeleport (MountedLifecycleSubscriber) is that route's placement.
                PatchExact(typeof(Kingmaker.Game), "Teleport", 0x06000CCB, new[] { typeof(Kingmaker.Blueprints.Area.BlueprintAreaEnterPoint) },
                    nameof(PatchMethods.LocalTeleportDispatchPrefix));
                // Known issues C04: native ScriptZoneController.Tick (internal) rechecks only units with native motion this
                // tick; a seated rider's carried motion is checked after it.
                PatchExact(typeof(Kingmaker.Game).Assembly.GetType("Kingmaker.Controllers.MapObjects.ScriptZoneController", true), "Tick",
                    0x060092A0, Type.EmptyTypes, null, nameof(PatchMethods.ScriptZoneTickPostfix));
                // Known issues C02: the native scene placement operations, around which a seated rider's own placement is
                // attributed (they write Position directly).
                PatchExact(typeof(Kingmaker.Designers.EventConditionActionSystem.Actions.TranslocateUnit), "RunAction", 0x06008FA2, Type.EmptyTypes,
                    nameof(PatchMethods.TranslocateUnitCapturePrefix), nameof(PatchMethods.TranslocateUnitPostfix));
                PatchExact(typeof(Kingmaker.AreaLogic.Cutscenes.Commands.CommandMoveUnit), "IsFinished", 0x06009E4E,
                    new[] { typeof(Kingmaker.AreaLogic.Cutscenes.CutscenePlayerData) },
                    nameof(PatchMethods.CommandMoveUnitCapturePrefix), nameof(PatchMethods.CommandMoveUnitPostfix));
                PatchExact(typeof(Kingmaker.AreaLogic.Cutscenes.Commands.CommandTeleportParty), "OnRun", 0x06009E90,
                    new[] { typeof(Kingmaker.AreaLogic.Cutscenes.CutscenePlayerData) },
                    nameof(PatchMethods.CommandTeleportPartyCapturePrefix), nameof(PatchMethods.CommandTeleportPartyPostfix));
                PatchExact(typeof(Kingmaker.Game), "LoadGame", 0x06000CE0, new[] { typeof(SaveInfo) }, nameof(PatchMethods.GameLoadAdmissionPrefix));
                PatchExact(typeof(Kingmaker.Game), "LoadGameFromMainMenu", 0x06000CE2, new[] { typeof(SaveInfo) }, nameof(PatchMethods.GameMainMenuLoadAdmissionPrefix));
                PatchExact(typeof(Kingmaker.Game), "LoadGameForSmokeTest", 0x06000CE1, new[] { typeof(SaveInfo) }, nameof(PatchMethods.GameSmokeLoadAdmissionPrefix));
                PatchExact(typeof(SaveManager), "LoadRoutine", 0x0600802C, new[] { typeof(SaveInfo), typeof(bool) }, nameof(PatchMethods.LoadPrefix), nameof(PatchMethods.LoadPostfix));
                PatchExact(typeof(UnitEntityView), "ForcePlaceAboveGround", 0x06001848, Type.EmptyTypes, nameof(PatchMethods.ForcePlaceAboveGroundPrefix));
                // Exploration repair C1: the native camp interaction issues one UnitApproachCamp per party unit, each to
                // its own camp slot. A mounted rider's stock agent is disabled, so its approach never succeeds and the
                // RestGroupCommand waits forever. The party's mounted pairs are suspended with retained intent first.
                PatchExact(typeof(Kingmaker.View.MapObjects.RestInteraction), "OnInteract", 0x06001A7B, new[] { typeof(UnitEntityData) },
                    nameof(PatchMethods.CampInteractionPrefix));
                // Exploration repair P1/P2: an item familiar of a mounted rider follows the physical mover and never
                // adopts the pair's owned attachment anchor (exact installed Kingmaker.Visual.Critters.Familiar).
                PatchExact(typeof(Kingmaker.Visual.Critters.Familiar), "UpdateMovingToMaster", 0x060013BF, Type.EmptyTypes,
                    nameof(PatchMethods.FamiliarFollowPrefix));
                PatchExact(typeof(Kingmaker.Visual.Critters.Familiar), "TeleportToMaster", 0x060013C5, new[] { typeof(bool) },
                    null, nameof(PatchMethods.FamiliarTeleportPostfix));
                PatchUnitClick();
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
                if (MountedRiderProfileCatalog.Small.Count != 0 || MountedRiderProfileCatalog.PrivateCalibrationSmall.Count != 0)
                    PatchExact(typeof(Character), "LateUpdate", 0x0600140C, Type.EmptyTypes, null, nameof(PatchMethods.PresentationRigLateUpdatePostfix));
                PatchExact(typeof(IKController), "SetupIkSystem", 0x0600156C, new[] { typeof(Character) }, nameof(PatchMethods.DollRoomIkSetupPrefix));
                PatchExact(typeof(IKController), "SetupFbbik", 0x0600156D, Type.EmptyTypes, nameof(PatchMethods.DollRoomFbbikPrefix), nameof(PatchMethods.DollRoomFbbikPostfix));
                PatchExact(typeof(CombatController), "Tick", 0x06000BD1, Type.EmptyTypes, nameof(PatchMethods.CombatControllerTickPrefix), nameof(PatchMethods.CombatControllerTickPostfix));
                PatchExact(typeof(CombatController), "ChooseNextUnit", 0x06000BD2, Type.EmptyTypes, null, nameof(PatchMethods.ChooseNextUnitPostfix), nameof(PatchMethods.PairedSelectorTranspiler));
                PatchExact(typeof(CombatController), "HandleCombatStart", 0x06000BE2, new[] { typeof(bool) }, nameof(PatchMethods.PairedEncounterPrefix), nameof(PatchMethods.PairedEncounterPostfix));
                PatchExact(typeof(CombatController), "HandleTurnBasedModeStateChanged", 0x06000BF5, new[] { typeof(bool) }, nameof(PatchMethods.ChargeModeBoundaryPrefix), nameof(PatchMethods.ChargeModeBoundaryPostfix));
                PatchExact(typeof(CombatController), "HandlePartyCombatStateChanged", 0x06000BED, new[] { typeof(bool) }, nameof(PatchMethods.ChargeCombatBoundaryPrefix));
                PatchExact(typeof(CombatController), "Disable", 0x06000BEA, Type.EmptyTypes, nameof(PatchMethods.PairedModeExitPrefix));
                PatchExact(typeof(CombatController), "RemoveUnit", 0x06000BE6, new[] { typeof(UnitEntityData) }, nameof(PatchMethods.PairedActorRemovalPrefix));
                PatchExact(typeof(BuffCollection), "Tick", 0x06002A02, Type.EmptyTypes, null, null, nameof(PatchMethods.PairedBuffTimerTranspiler));
                PatchExact(typeof(BuffCollection), "CreateFact", 0x060029F9, null,
                    null, nameof(PatchMethods.ChargeBuffCreatedPostfix));
                PatchExact(typeof(BuffCollection), "AddBuffInternal", 0x060029F7, null,
                    nameof(PatchMethods.ChargeChildAdmissionPrefix), null, nameof(PatchMethods.ChargeChildAcquisitionTranspiler));
                PatchExact(typeof(ItemEnchantmentCollection), "AddEnchantment", 0x060099B8, null,
                    null, null, nameof(PatchMethods.ChargeChildAcquisitionTranspiler));
                PatchExact(typeof(ItemEnchantmentCollection), "CreateFact", 0x060099B7, null,
                    null, nameof(PatchMethods.ChargeEnchantmentCreatedPostfix));
                PatchExact(typeof(FactCollection), "RemoveFact", 0x06009699, new[] { typeof(Fact) }, nameof(PatchMethods.ChargeFactRemovalPrefix));
                PatchExact(typeof(FactCollection), "RemoveFact", 0x06009698, new[] { typeof(BlueprintFact) }, nameof(PatchMethods.ChargeBlueprintRemovalPrefix));
                PatchExact(typeof(Fact), "Activate", 0x06009A50, Type.EmptyTypes, nameof(PatchMethods.ChargeFactLifecyclePrefix));
                PatchExact(typeof(Fact), "Deactivate", 0x06009A52, Type.EmptyTypes, nameof(PatchMethods.ChargeFactLifecyclePrefix));
                PatchExact(typeof(Fact), "Dispose", 0x06009A5A, Type.EmptyTypes, nameof(PatchMethods.ChargeFactLifecyclePrefix));
                PatchExact(typeof(ContextActionRemoveBuff), "RunAction", 0x06002394, Type.EmptyTypes, nameof(PatchMethods.ChargeChildRemovalPrefix));
                PatchExact(typeof(ItemEnchantment), "RespawnFx", 0x060099AB, Type.EmptyTypes,
                    null, null, nameof(PatchMethods.ChargeFxRespawnTranspiler));
                PatchExact(typeof(GameObjectsPool), "Claim", 0x060010BD, null, null, null, nameof(PatchMethods.ChargeFxAcquisitionTranspiler));
                PatchExact(typeof(PooledGameObject), "CreateInstance", 0x06001109, null, null, null, nameof(PatchMethods.ChargeFxAcquisitionTranspiler));
                PatchExact(typeof(SnapControllerBase), "Init", 0x0600111A, Type.EmptyTypes, null, null, nameof(PatchMethods.ChargeFxAcquisitionTranspiler));
                PatchExact(typeof(GameObjectsPool), "Release", 0x060010BE, null, null, nameof(PatchMethods.ChargeFxReleasePostfix), nameof(PatchMethods.ChargeFxRespawnTranspiler));
                PatchExact(typeof(Kingmaker.Blueprints.Items.Ecnchantments.ItemEnchantment), "DestroyFx", 0x060099AF,
                    Type.EmptyTypes, null, null, nameof(PatchMethods.ChargeFxDestructionTranspiler));
                PatchExact(typeof(Kingmaker.UnitLogic.FactLogic.AddCondition), "OnTurnOn", 0x06002448, Type.EmptyTypes,
                    null, null, nameof(PatchMethods.ChargeConditionTranspiler));
                PatchExact(typeof(Kingmaker.UnitLogic.FactLogic.AddCondition), "OnTurnOff", 0x06002449, Type.EmptyTypes,
                    null, null, nameof(PatchMethods.ChargeConditionTranspiler));
                PatchExact(typeof(Kingmaker.UnitLogic.FactLogic.AddCondition), "OnEntityCreated", 0x0600244A, new[] { typeof(UnitEntityData) },
                    null, null, nameof(PatchMethods.ChargeConditionTranspiler));
                PatchExact(typeof(Kingmaker.UnitLogic.UnitState), "UpdateStatusEffect", 0x06001FBF,
                    new[] { typeof(Kingmaker.UnitLogic.UnitCondition), typeof(Buff) }, nameof(PatchMethods.ChargeConditionMutationPrefix));
                PatchExact(typeof(CombatController), "TickTime", 0x06000BD6, Type.EmptyTypes, nameof(PatchMethods.ChargeTransitionTickPrefix), null, nameof(PatchMethods.PairedReadinessTranspiler));
                PatchExact(typeof(CombatController).GetNestedType("<>c", BindingFlags.NonPublic), "<HandleCombatStart>b__79_2",
                    0x0600A2BE, null, null, null, nameof(PatchMethods.PairedReadinessTranspiler));
                PatchExact(typeof(TurnController), "Prepare", 0x06000C3C, Type.EmptyTypes, nameof(PatchMethods.TurnPreparePrefix), nameof(PatchMethods.TurnPreparePostfix), nameof(PatchMethods.PairedPreparationTranspiler));
                PatchExact(typeof(TurnController), "Tick", 0x06000C34, Type.EmptyTypes, nameof(PatchMethods.PairedTickPrefix), null, nameof(PatchMethods.PairedActivityTranspiler));
                PatchExact(typeof(TurnController), "UpdateActionPredictions", 0x06000C6E, Type.EmptyTypes, nameof(PatchMethods.PairedInputPredictionPrefix));
                PatchExact(typeof(TurnController), "IgnoreClick", 0x06000C2F, Type.EmptyTypes, nameof(PatchMethods.PairedIgnoreClickPrefix),
                    nameof(PatchMethods.PairedIgnoreClickPostfix));
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
                PatchExact(pathPreview, "UpdateActionStatesFromPath", 0x06007021, null, null, nameof(PatchMethods.PairedPathStatesPostfix), nameof(PatchMethods.PairedPathStatesTranspiler));
                PatchExact(typeof(Kingmaker.TurnBasedMode.ActionsState), "UpdateMoveDistance", 0x06007009, Type.EmptyTypes, null, null, nameof(PatchMethods.PairedActionsStateOwnerTranspiler));
                // Known issues Sprint 7 (U01): every write of a prediction action's remaining distance (native's
                // UpdateMoveDistance and the path's UpdateActionStatesFromPath) passes through this one native method.
                PatchExact(typeof(Kingmaker.TurnBasedMode.Controllers.CombatAction), "UpdateRemainingDistance", 0x0600704F,
                    new[] { typeof(float?), typeof(bool) }, nameof(PatchMethods.PairedMoveDistancePrefix));
                // Movement-facing prediction slots: the mount's actions during a rider-selected ground preview.
                PatchExact(typeof(Kingmaker.UI._ConsoleUI.TurnBasedMode.PredictionPanelMoveActionVM), "get_CombatAction", 0x06004E0B, Type.EmptyTypes, null, nameof(PatchMethods.PairedSlotActionPostfix));
                PatchExact(typeof(Kingmaker.UI._ConsoleUI.TurnBasedMode.PredictionPanelFiveFeetActionVM), "get_CombatAction", 0x06004DE4, Type.EmptyTypes, null, nameof(PatchMethods.PairedSlotActionPostfix));
                PatchExact(typeof(Kingmaker.UI._ConsoleUI.TurnBasedMode.PredictionPanelStandardActionVM), "get_CombatAction", 0x06004E0F, Type.EmptyTypes, null, nameof(PatchMethods.PairedSlotActionPostfix));
                var fullRoundSlot = typeof(Kingmaker.UI._ConsoleUI.TurnBasedMode.PredictionPanelFullRoundActionVM);
                PatchExact(fullRoundSlot, "get_CombatAction", 0x06004E00, Type.EmptyTypes, null, nameof(PatchMethods.PairedSlotActionPostfix));
                PatchExact(fullRoundSlot, "UpdateStates", 0x06004E04, Type.EmptyTypes, null, null, nameof(PatchMethods.PairedFullRoundStatesTranspiler));
                PatchExact(fullRoundSlot, "UpdateDistance", 0x06004E08, Type.EmptyTypes, null, null, nameof(PatchMethods.PairedFullRoundDistanceTranspiler));
                PatchExact(fullRoundSlot, "GetActivityText", 0x06004E07, Type.EmptyTypes, null, null, nameof(PatchMethods.PairedFullRoundTextTranspiler));
                PatchExact(typeof(ClickGroundHandler), "OnClick", 0x060093D5, null, null, null, nameof(PatchMethods.PairedPathUnitTranspiler));
                // Known issues sprint 3 (M03): the same method also reads the group speed limit from the physical movers.
                // Continuation (M05): and its one stick-driven test of the current unit reads the pair's physical mover.
                PatchExact(typeof(ClickGroundHandler), "MoveSelectedUnitsToPoint", 0x060093DB, null, null, null, nameof(PatchMethods.GroundOrderTranspiler));
                PatchExact(typeof(Kingmaker.UI.ClickPointerManager), "Update", 0x06002D64, Type.EmptyTypes, null, null, nameof(PatchMethods.PairedPrimaryMarkerTranspiler));
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
                // Known issues Sprint 7 (U04): the folded mount's hover and the rider's mounted badge.
                // Review of 292cd23c (R1): the tracker's IUnitDirectHoverUIHandler, which the world hover of a unit view raises
                // (false to the old view, then true to the new); KMC's owned redirect is released before native's pass.
                PatchExact(trackerType, "HandleHoverChange", 0x06004F16, new[] { typeof(UnitEntityView), typeof(bool) },
                    nameof(PatchMethods.FoldedMountHoverPrefix), nameof(PatchMethods.FoldedMountHoverPostfix));
                // (R2): the portrait's per-frame refresh (CharacterBase.Update calls UpdateCrippled for every bound portrait
                // outside loading; HealthUpdate runs only on a hit-point change) and its bind or rebind to a unit.
                PatchExact(typeof(Kingmaker.UI.Group.GroupCharacterPortraitController), "UpdateCrippled", 0x06003F02, Type.EmptyTypes, null, nameof(PatchMethods.MountedBadgePostfix));
                PatchExact(typeof(Kingmaker.UI.Group.GroupCharacterPortraitController), "Initialize", 0x06003EF6, new[] { typeof(UnitEntityData) }, null, nameof(PatchMethods.MountedBadgePostfix));
                PatchExact(typeof(UnitActionController), "TickCommandTurnBased", 0x0600911D, new[] { typeof(UnitCommand) }, null, nameof(PatchMethods.TickCommandTurnBasedPostfix), nameof(PatchMethods.PairedEligibilityTranspiler));
                logger.Info("Installed exact-token Harmony12 active-pair guards, unified-turn adapters, and bounded probes.");
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
                    PatchBridge.ChargeSafety = null;
                    PatchBridge.UnifiedTurn = null;
                    PatchBridge.NativeControls = null;
                    PatchBridge.Persistence = null;
                    PatchBridge.Animation = null;
                    PatchBridge.DollRoomIk = null;
                    PatchBridge.SaveAuthorization = null;
                    PatchBridge.LifecycleLedger = null;
                    PatchBridge.Logger = null;
                    PatchBridge.Defense = null;
            PatchBridge.HorseCompanion = null;
            PatchBridge.Presentation = null;
            DivineMountCallLogic.IsMountedPairMember = null;
            DivineMountSmiteLogic.IsMountedMount = null;
            DivineMountSmiteLogic.ReportLateRefusal = null;
                }
                throw;
            }
        }

        public void Dispose()
        {
            if (disposed) { return; }
            if (!PatchMethods.GuardNativeBoundary(NativeLifecycleBoundary.ModDisable, CleanupTrigger.ModDisabled, "patch-disposal"))
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
            PatchBridge.Defense = null;
            PatchBridge.Presentation = null;
            disposed = true;
        }

        // PR #70 R4-F2 (round 5): the admission-aware direct input's scoped dispatch (the same bracket as native PointerController.Tick).
        // PR #70 R5-F2 (round 6): the scope is bound to the target its handler will receive and to the turn it was opened in, so an override
        // proven for the highlighted/predicted hostile can only reach a handler call on that same hostile in that same turn.
        internal static void BeginScopedInputDispatch(UnitEntityData target, object turn)
        {
            PatchMethods.PointerTickPrefix();
            PatchMethods.BindScopedDispatchTarget(target, turn);
        }
        internal static void EndScopedInputDispatch() => PatchMethods.PointerTickPostfix();

        // Known issues M04 closeout (review of b1a88b4e, R1): only the cannot-attack approach's own submission is guarded.
        // Its own method so the composition regression (Test-GunslingerClickComposition.ps1) runs this exact registration.
        private void PatchUnitClick() =>
            PatchExact(typeof(ClickUnitHandler), "OnClick", 0x060093ED, new[] { typeof(UnityEngine.GameObject), typeof(UnityEngine.Vector3), typeof(int), typeof(bool), typeof(bool) }, nameof(PatchMethods.UnitClickPrefix),
                null, nameof(PatchMethods.CannotAttackApproachTranspiler));

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
            try
            {
                harmony.Patch(
                    original,
                    prefix == null ? null : new HarmonyMethod(prefix),
                    postfix == null ? null : new HarmonyMethod(postfix),
                    transpiler == null ? null : new HarmonyMethod(transpiler));
            }
            catch (Exception error)
            {
                var cause = error;
                while (cause is TargetInvocationException && cause.InnerException != null) cause = cause.InnerException;
                throw new InvalidOperationException("Harmony could not patch " + type.FullName + "." + name + " (token " + expectedToken.ToString("X8") +
                    ", " + HarmonyId + " prefix " + (prefixName ?? "none") + ", postfix " + (postfixName ?? "none") + ", transpiler " + (transpilerName ?? "none") +
                    "): " + cause.Message + " Patches already on the method: " +
                    DescribePatches(original) + ".", error);
            }
        }

        // Diagnostic only: the owners Harmony already records on the method (the failed patch is not recorded) and, for
        // transpilers, their order inputs, so a structural mismatch names the other mods that changed the method first.
        private string DescribePatches(MethodBase original)
        {
            try
            {
                var info = harmony.GetPatchInfo(original);
                if (info == null || info.Owners.Count == 0) return "none";
                return "owners " + string.Join(", ", info.Owners.ToArray()) + "; transpilers " + (info.Transpilers.Count == 0 ? "none" :
                    string.Join(", ", info.Transpilers.Select(patch => patch.owner + " " + patch.patch?.DeclaringType?.FullName + "." + patch.patch?.Name +
                        " (priority " + patch.priority + ", index " + patch.index + ")").ToArray()));
            }
            catch (Exception error) { return "unavailable (" + error.GetType().Name + ")"; }
        }

        internal static void ReportFailedSave(Exception exception, object operation) =>
            PatchBridge.Persistence?.ReportFailedSave(exception, operation);

        // Observation for the unload-refusal probe: the bridged services are
        // still installed. This only ever turns false once Dispose has run.
        internal static bool BridgeInstalled =>
            PatchBridge.Persistence != null && PatchBridge.NativeControls != null && PatchBridge.Service != null;

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
            internal static MountedWorldService World => Service?.World;
            internal static GameMountedRelationshipService Owner(UnitEntityData actor) =>
                World != null ? World.ForActor(actor)?.Relationship : Service?.ResolveActorOwner(actor);
            internal static GameMountedRelationshipService Owner(UnitCommands commands) =>
                World != null ? World.Pairs.FirstOrDefault(pair => commands != null &&
                    (ReferenceEquals(pair.Relationship.PairOwner?.Rider?.Commands, commands) ||
                     ReferenceEquals(pair.Relationship.PairOwner?.Mount?.Commands, commands)))?.Relationship : Service;
            internal static MountedCombatController CombatFor(UnitEntityData actor) => Owner(actor)?.CombatOwner;
            internal static MountedCombatController CombatFor(UnitCommands commands) => Owner(commands)?.CombatOwner;
            internal static MountedPairServices[] Pairs => World?.Pairs;
            internal static MountedCombatDefenseService Defense;
            internal static HorseCompanionBlueprintService HorseCompanion;
            internal static MountedPairPresentation Presentation;
        }

        private static class PatchMethods
        {
            internal static bool FeatRowPreviewRebindPrefix(Kingmaker.Assets.UI.LevelUp.CharBuildSelectorItem __instance,
                Kingmaker.UnitLogic.Class.LevelUp.FeatureSelectionState featureSelection,
                Kingmaker.Blueprints.Classes.Selection.IFeatureSelectionItem feature,
                Kingmaker.Assets.UI.LevelUp.CharBSelectorLayer selector, UnityEngine.Events.UnityAction action)
            {
                return !MountedFeatSelectorRefreshService.RebindEqualPreviewRow(PatchBridge.HorseCompanion,
                    __instance, featureSelection, feature, selector, action);
            }
            internal static void FeatSelectorRefreshPrefix(Kingmaker.Assets.UI.LevelUp.CharBSelectorLayer __instance,
                Kingmaker.UnitLogic.Class.LevelUp.FeatureSelectionState selectionState, ref bool refreshSort, out bool __state)
            {
                __state = MountedFeatSelectorRefreshService.BeforeRefresh(PatchBridge.HorseCompanion, __instance, selectionState, ref refreshSort);
            }

            internal static void CharacterSheetNamePostfix(Kingmaker.UI.ServiceWindow.CharacterScreen.CharacterScreenController __instance)
            {
                CompanionNameService.Current?.Sheet.Refresh(__instance);
            }

            // Each prefix hands its own operation (or null) to its own postfix through Harmony's per-invocation __state, so a
            // postfix completes only its own invocation; a native method that throws runs no postfix, and the lifecycle settles
            // its operation at the next entry.
            internal static void BadSlotCleanupPrefix(Kingmaker.UI.UnitSettings.UnitUISettings __instance, out object __state) =>
                __state = PatchBridge.NativeControls?.BeginNativeBadSlotCleanup(__instance);

            // GetSlot(int index, UnitEntityData unit): only that slot belongs to the operation; native's own unit argument
            // identifies the bar's owner (parameter names pinned in Test-AssemblyContracts.ps1).
            internal static void SlotReadPrefix(Kingmaker.UI.UnitSettings.UnitUISettings __instance, int index, UnitEntityData unit, out object __state) =>
                __state = PatchBridge.NativeControls?.BeginNativeBadSlotCleanup(__instance, index, unit);

            internal static void BadSlotCleanupPostfix(object __state) =>
                PatchBridge.NativeControls?.EndNativeBadSlotCleanup(__state);

            // Observation only, before the native write; it never throws, so the write always happens.
            internal static void SlotWritePrefix(Kingmaker.UI.UnitSettings.UnitUISettings __instance)
            {
                try { PatchBridge.NativeControls?.BeforeNativeSlotWrite(__instance); }
                catch (Exception error) { PatchBridge.Logger?.Exception("Mount / Dismount toolbar: a native slot write was not observed", error); }
            }

            // CleanupBadSlots's own SetSlot(slot, index) calls become NativeCleanupSetSlot(this, slot, index): the same write
            // (a native exception propagates unchanged), then KMC records it. Exactly the pinned call sites, or patching fails.
            internal static IEnumerable<CodeInstruction> BadSlotCleanupWritesTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                var native = typeof(Kingmaker.UI.UnitSettings.UnitUISettings).GetMethod("SetSlot",
                    new[] { typeof(Kingmaker.UI.UnitSettings.MechanicActionBarSlot), typeof(int) });
                var hook = typeof(PatchMethods).GetMethod(nameof(NativeCleanupSetSlot), BindingFlags.Static | BindingFlags.NonPublic);
                var list = instructions.ToList();
                var replaced = 0;
                foreach (var instruction in list)
                {
                    if (instruction.opcode != OpCodes.Call || !Equals(instruction.operand, native)) continue;
                    instruction.operand = hook;
                    replaced++;
                }
                if (native == null || hook == null || replaced != NativeCleanupSetSlotCalls)
                    throw new InvalidOperationException("UnitUISettings.CleanupBadSlots no longer writes through exactly " +
                        NativeCleanupSetSlotCalls + " SetSlot calls (found " + replaced + ").");
                return list;
            }

            private static void NativeCleanupSetSlot(Kingmaker.UI.UnitSettings.UnitUISettings settings,
                Kingmaker.UI.UnitSettings.MechanicActionBarSlot slot, int index)
            {
                settings.SetSlot(slot, index);
                try { PatchBridge.NativeControls?.RecordNativeCleanupWrite(settings, index); }
                catch (Exception error) { PatchBridge.Logger?.Exception("Mount / Dismount toolbar: a native cleanup write was not recorded", error); }
            }

            internal static void CityCompanionRosterPostfix(ref List<UnitEntityData> __result, bool withRemote, bool withPet)
            {
                __result = CityCompanionRosterAdapter.Project(__result, withRemote, withPet,
                    PatchBridge.NativeControls?.IsActive == true, PatchBridge.Logger);
            }

            internal static void FeatSelectorRefreshPostfix(Kingmaker.Assets.UI.LevelUp.CharBSelectorLayer __instance, bool __state)
            {
                MountedFeatSelectorRefreshService.AfterRefresh(__instance, __state);
                MountedFeatSelectorRefreshService.ClearStaleRowSelection(PatchBridge.HorseCompanion, __instance);
            }

            // PAL-01: a class level entry reaches a mount-bonded paladin without the later weapon-bond upgrades.
            internal static void DivineBondLevelEntryFeatures(Kingmaker.UnitLogic.UnitDescriptor unit, ref System.Collections.Generic.IList<Kingmaker.Blueprints.Classes.BlueprintFeatureBase> features)
            {
                if (PatchBridge.HorseCompanion != null) features = PatchBridge.HorseCompanion.FilterLevelEntryFeatures(unit, features);
            }

            internal static bool MountedDismissAutoMerge(Kingmaker.Player player, UnitEntityData actor) =>
                NativeMountedDismissInventorySafety.AutoMerge(PatchBridge.Service, player, actor);
            internal static IEnumerable<CodeInstruction> MountedDismissInventoryTranspiler(
                IEnumerable<CodeInstruction> instructions, ILGenerator generator) =>
                NativeMountedDismissInventorySafety.Rewrite(instructions, generator,
                    typeof(PatchMethods).GetMethod(nameof(MountedDismissAutoMerge),
                        BindingFlags.Static | BindingFlags.NonPublic));
            internal static void ChargeBuffCreatedPostfix(BuffCollection __instance, Fact __result)
            {
                MountedChargeLease.ObserveChargeBuffCreated(__instance, __result as Buff);
                MountedChargeBuffChildren.Created(__instance, __result);
            }

            internal static void ChargeEnchantmentCreatedPostfix(ItemEnchantmentCollection __instance, Fact __result) =>
                MountedChargeBuffChildren.Created(__instance, __result);
            internal static void ChargeChildAdmissionPrefix(BuffCollection __instance, BlueprintBuff blueprint, MechanicsContext context) =>
                MountedChargeBuffChildren.BeforeAddBuff(__instance, blueprint, context);
            internal static void ChargeFactRemovalPrefix(FactCollection __instance, Fact fact) => MountedChargeBuffChildren.BeforeRemove(__instance, fact);
            internal static void ChargeBlueprintRemovalPrefix(FactCollection __instance, BlueprintFact blueprint) => MountedChargeBuffChildren.BeforeRemoveBlueprint(__instance, blueprint);
            internal static void ChargeFactLifecyclePrefix(Fact __instance) => MountedChargeBuffChildren.BeforeFactLifecycle(__instance);
            internal static bool ChargeChildRemovalPrefix(ContextActionRemoveBuff __instance) => MountedChargeBuffChildren.RemoveAction(__instance);
            internal static IEnumerable<CodeInstruction> ChargeChildAcquisitionTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod) =>
                MountedChargeBuffChildren.WrapAddFact(instructions, __originalMethod);
            internal static IEnumerable<CodeInstruction> ChargeFxAcquisitionTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase __originalMethod)
            {
                var output = MountedChargeEnchantmentFx.WrapAcquisition(instructions, __originalMethod);
                return __originalMethod.MetadataToken == 0x0600111A ?
                    MountedChargeEnchantmentFx.WrapRespawn(output, generator, __originalMethod) : output;
            }
            internal static IEnumerable<CodeInstruction> ChargeFxRespawnTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase __originalMethod) =>
                MountedChargeEnchantmentFx.WrapRespawn(instructions, generator, __originalMethod);
            internal static void ChargeFxReleasePostfix(UnityEngine.GameObject instance) => MountedChargeEnchantmentFx.ObserveRelease(instance);
            internal static IEnumerable<CodeInstruction> ChargeFxDestructionTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod) =>
                MountedChargeEnchantmentFx.WrapDestruction(instructions, __originalMethod);

            internal static IEnumerable<CodeInstruction> ChargeConditionTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod) =>
                MountedChargeConditionObserver.Wrap(instructions, __originalMethod);

            internal static void ChargeConditionMutationPrefix(Kingmaker.UnitLogic.UnitState __instance,
                Kingmaker.UnitLogic.UnitCondition condition, Buff sourceBuff) =>
                MountedChargeConditionObserver.ObserveMutation(__instance, condition, sourceBuff);

            internal static void ChargeTargetPostfix(Kingmaker.UnitLogic.Abilities.AbilityData __instance,
                Kingmaker.Utility.TargetWrapper target, ref bool __result)
            {
                // The installed native-target extension replaces this method in
                // a prefix. Decide only the affected Charge's final result: the
                // mounted rider's ordinary Charge targets what the mounted Charge can.
                var decision = PatchBridge.ChargeSafety?.PublicCanTarget(__instance, target?.Unit);
                if (decision.HasValue) __result = decision.Value;
            }

            internal static bool ChargeAvailabilityPrefix(Kingmaker.UnitLogic.Abilities.AbilityData __instance, ref bool __result)
            {
                var availability = PatchBridge.ChargeSafety?.PublicAvailability(__instance);
                if (availability == null) return true;
                __result = availability.IsEnabled;
                return false;
            }

            internal static bool ChargeReasonPrefix(Kingmaker.UnitLogic.Abilities.AbilityData __instance, ref string __result)
            {
                var availability = PatchBridge.ChargeSafety?.PublicAvailability(__instance);
                if (availability == null || availability.IsEnabled) return true;
                __result = availability.Reason;
                return false;
            }

            // F2: the public slot runs the delivery identity it stands for right now. The native body runs unchanged on
            // that AbilityData (availability, warning, prediction, target selection or self cast), then the slot gets its
            // public identity back. Harmony 1 has no finalizer; the control service repairs a slot an exception left behind.
            internal static void PublicSlotClickPrefix(Kingmaker.UI.UnitSettings.MechanicActionBarSlotAbility __instance,
                out Kingmaker.UnitLogic.Abilities.AbilityData __state)
            {
                __state = null;
                PatchBridge.NativeControls?.BeginPublicSlotClick();
                var delivery = PatchBridge.NativeControls?.ResolvePublicSlotDelivery(__instance,
                    PatchBridge.ChargeSafety?.Route(__instance.Ability) == MountedChargeRoute.Mounted);
                if (delivery == null) return;
                __state = __instance.Ability;
                __instance.Ability = delivery;
            }

            internal static void PublicSlotClickPostfix(Kingmaker.UI.UnitSettings.MechanicActionBarSlotAbility __instance,
                Kingmaker.UnitLogic.Abilities.AbilityData __state)
            {
                if (__state == null) return;
                var delivery = __instance.Ability;
                __instance.Ability = __state;
                // Known issues Sprint 7 (U02): the exact slot that handed native selection to its delivery is remembered
                // for drawing only. A presentation failure never reaches the click.
                try { PatchBridge.NativeControls?.RecordPublicSlotTargeting(__instance, __state, delivery); }
                catch (Exception exception) { ReportPresentationFault("public-slot targeting record", exception); }
            }

            // Known issues Sprint 7 (U02): after native draws the slot, the exact public slot whose delivery owns native
            // target selection gets native's own selected mark. Every other slot keeps native's drawing.
            internal static void PublicSlotSelectionPostfix(Kingmaker.UI.UnitSettings.MechanicActionBarSlotAbility __instance,
                Kingmaker.UI.ActionBar.ActionBarSlot slot)
            {
                var controls = PatchBridge.NativeControls;
                if (controls == null || slot == null) return;
                try { if (controls.PresentsTargetingSelection(__instance)) NativeMountedControlService.ShowTargetingSelection(__instance, slot); }
                catch (Exception exception) { ReportPresentationFault("public-slot selection mark", exception); }
            }

            // Known issues Sprint 7 (U01): the distance a mount's Move or Standard prediction action is about to show reads
            // zero once forced travel has closed the pair's activation locomotion. Before native stores and announces it.
            internal static void PairedMoveDistancePrefix(Kingmaker.TurnBasedMode.Controllers.CombatAction __instance, ref float? remainingDistance)
            {
                try { PatchBridge.UnifiedTurn?.ProjectUsableMoveDistance(__instance, ref remainingDistance); }
                catch (Exception exception) { ReportPresentationFault("paired move distance", exception); }
            }

            // Known issues Sprint 7 (U03): the class panel's one progression read, replaced by the projected copy.
            internal static Kingmaker.UnitLogic.ProgressionData[] DivineBondClassRows(Kingmaker.UnitLogic.UnitProgressionData progression,
                Kingmaker.Blueprints.Classes.BlueprintCharacterClass characterClass)
            {
                var service = PatchBridge.HorseCompanion;
                return service != null ? service.ProjectClassProgressions(progression, characterClass) : progression.GetClassProgressions(characterClass);
            }

            internal static IEnumerable<CodeInstruction> DivineBondClassRowsTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                var native = typeof(Kingmaker.UnitLogic.UnitProgressionData).GetMethod("GetClassProgressions",
                    new[] { typeof(Kingmaker.Blueprints.Classes.BlueprintCharacterClass) });
                var hook = typeof(PatchMethods).GetMethod(nameof(DivineBondClassRows), BindingFlags.Static | BindingFlags.NonPublic);
                var list = instructions.ToList();
                var replaced = 0;
                foreach (var instruction in list)
                {
                    if (instruction.opcode != OpCodes.Callvirt || !Equals(instruction.operand, native)) continue;
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = hook;
                    replaced++;
                }
                if (native == null || hook == null || replaced != 1)
                    throw new InvalidOperationException("The class progression layout no longer reads GetClassProgressions exactly once.");
                return list;
            }

            // Review of 292cd23c (R1): before native's hover pass, KMC's owned redirect is released exactly.
            internal static void FoldedMountHoverPrefix(object __instance, UnitEntityView unitEntityView, bool isHover)
            {
                try { PatchBridge.Presentation?.BeforeTrackerHover(__instance, unitEntityView, isHover); }
                catch (Exception exception) { ReportPresentationFault("folded mount hover release", exception); }
            }

            // Known issues Sprint 7 (U04): after native's hover pass, a folded mount's hover highlights its rider's entry.
            internal static void FoldedMountHoverPostfix(object __instance, UnitEntityView unitEntityView, bool isHover)
            {
                try { PatchBridge.Presentation?.RedirectFoldedMountHover(__instance, unitEntityView, isHover); }
                catch (Exception exception) { ReportPresentationFault("folded mount hover", exception); }
            }

            // Known issues Sprint 7 (U04): every native portrait refresh and bind shows or hides the rider's mounted badge.
            internal static void MountedBadgePostfix(Kingmaker.UI.Group.GroupCharacterPortraitController __instance)
            {
                try { PatchBridge.Presentation?.RefreshPortrait(__instance); }
                catch (Exception exception) { ReportPresentationFault("mounted badge", exception); }
            }

            // Presentation is never allowed to fail a native UI call. Each seam (a fixed set of names) reports its first
            // failure only, since some run every frame; logging itself is contained.
            private static readonly HashSet<string> ReportedPresentationSeams = new HashSet<string>(StringComparer.Ordinal);
            private static void ReportPresentationFault(string seam, Exception exception)
            {
                try
                {
                    if (ReportedPresentationSeams.Add(seam))
                        PatchBridge.Logger?.Error("Mounted presentation " + seam + " failed and was skipped (reported once): " +
                            exception.GetType().Name + ": " + exception.Message);
                }
                catch { }
            }

            internal static bool ChargeClickPrefix(ClickWithSelectedAbilityHandler __instance, UnityEngine.GameObject gameObject, int button,
                bool simulate, bool muteEvents, ref bool __result)
            {
                if (button == 0 && !simulate && !muteEvents) ObserveMountedSmiteClick(__instance.Ability, gameObject);
                if (button != 0 || PatchBridge.ChargeSafety == null ||
                    PatchBridge.ChargeSafety.AllowClick(__instance.Ability, simulate, muteEvents)) return true;
                __result = false;
                return false;
            }

            private static void ObserveMountedSmiteClick(Kingmaker.UnitLogic.Abilities.AbilityData ability, UnityEngine.GameObject target)
            {
                var logic = ability?.Blueprint.GetComponent<DivineMountSmiteLogic>();
                var victim = target == null ? null : target.GetComponentInParent<Kingmaker.View.UnitEntityView>()?.EntityData;
                if (logic == null || victim == null || !logic.IsLegalTarget(ability.Caster.Unit, victim) || !logic.NeedsApproach(ability.Caster.Unit, victim)) return;
                PatchBridge.World?.ForActor(ability.Caster.Unit)?.Combat.ReportMountedSmiteRefusal(null, true);
            }

            internal static bool ChargeEnqueueOrRunPrefix(UnitCommands __instance, UnitCommand cmd, bool first) =>
                PatchBridge.NativeControls?.AdmitPublicControlCommand(cmd) != false &&
                (PointerController.SimulatingClick || PatchBridge.CombatFor(__instance)?.AdmitChargeEnqueueOrRun(__instance, cmd, first) != false);

            internal static bool ChargeEnqueuePrefix(UnitCommands __instance, UnitCommand cmd) =>
                ChargeAdmissionPrefix(__instance, cmd, MountedChargeRequestRoute.Enqueue);

            private static bool ChargeAdmissionPrefix(UnitCommands __instance, UnitCommand cmd, MountedChargeRequestRoute route) =>
                PatchBridge.NativeControls?.AdmitPublicControlCommand(cmd) != false &&
                PatchBridge.Persistence?.CombatRestorationPending != true &&
                PatchBridge.Persistence?.ActiveSaveWorkerRunning != true &&
                (PointerController.SimulatingClick || PatchBridge.CombatFor(__instance)?.AdmitChargeCommand(__instance, cmd, route) != false) &&
                (PatchBridge.ChargeSafety == null || PatchBridge.ChargeSafety.AllowAdmission(cmd));

            // Queue advancement reaches only this overload, so a queued automatic pair command is re-admitted here.
            internal static bool InternalRunPrefix(UnitCommands __instance, UnitCommand cmd, bool fromQueue)
            {
                if (!ChargeAdmissionPrefix(__instance, cmd, fromQueue ? MountedChargeRequestRoute.QueuePromotion : MountedChargeRequestRoute.Run)) return false;
                var automatic = fromQueue ? cmd as MountedPairSingleAttack : null;
                if (automatic == null) return true;
                if (!automatic.IsAutomaticOrigin) return AdmitExplicitPromotion(automatic, PatchBridge.CombatFor(__instance));
                var combat = PatchBridge.CombatFor(__instance);
                if (combat != null) return combat.AdmitQueuedAutomaticRun(automatic);
                (automatic as MountedPairAttackCommand)?.RecordEndCause(MountedAttackEndCause.Cancelled, "queued automatic command without its pair");
                automatic.Interrupt();
                return false;
            }

            // PR-1 reviews R1-F2, R3-F1: a retired explicit pair wrapper never promotes, also when its pair can no longer be resolved
            // (a late callback after detachment); otherwise the pair's controller decides.
            internal static bool AdmitExplicitPromotion(MountedPairSingleAttack command, MountedCombatController owner)
            {
                if (command.OrderRetired)
                {
                    if (!command.IsFinished) command.Interrupt();
                    return false;
                }
                return owner == null || owner.AdmitQueuedExplicitRun(command);
            }

            // Known issues sprint 3 continuation (M05). After the native overload that admits commands, reached by the
            // public Run and by queue promotion alike: an order that is now in a slot of the rider's own container ends
            // the pair's stick movement, as native Run ends a unit's own. An enqueued or refused command is in no slot.
            internal static void InternalRunPostfix(UnitCommands __instance, UnitCommand cmd, bool fromQueue)
            {
                if (PointerController.SimulatingClick || cmd == null) return;
                // Reviews R1-F5/R3-F3/R2-F5: what native actually accepted (a slot, or the queue via Run's queue-instead path)
                // is classified against a retained ordinary order. A refused or merged command is in neither place.
                try
                {
                    // Review R5-F5: native's own fromQueue marks promotion of an already accepted (queued) command.
                    // PR-1 review R1-F11: a promotion counts only if it actually installed the command (a refused promotion did not).
                    var route = MountedAcceptedWorkRoutes.AfterRun(fromQueue, __instance.Contains(cmd), __instance.Queue.Contains(cmd));
                    if (route.HasValue) PatchBridge.CombatFor(__instance)?.ObserveAcceptedWork(__instance, cmd, route.Value);
                }
                catch (Exception error) { PatchBridge.Logger?.Exception("Accepted pair work was not observed", error); }
                if (cmd is UnitMoveContiniously) return;
                try { PatchBridge.Owner(__instance)?.ObserveOrderDuringContinuousDrive(__instance, cmd); }
                catch (Exception error) { ReportGroupPaceFault("Mounted stick movement: an admitted order was not observed", error); }
            }

            // PR #70 R1-F7 (rounds 4-5): the pointer dispatch bracket. Every pair's controller is told. The transpiler makes the native body
            // `PointerTickPrefix(); try { <body, each ret -> leave> } finally { PointerTickPostfix(); }` (the WrapRespawn pattern), so the
            // dispatch ends on a normal return AND when the body throws (Harmony 1.2 has no finalizers; an ordinary postfix would be skipped).
            internal static void PointerTickPrefix()
            {
                try { foreach (var combat in ClickControllers()) combat.BeginPointerDispatch(); }
                catch (Exception error) { PatchBridge.Logger?.Exception("Pointer dispatch was not observed", error); }
            }

            // PR #70 R5-F2 (round 6): the direct input's dispatch target and turn (native PointerController.Tick binds none).
            internal static void BindScopedDispatchTarget(UnitEntityData target, object turn)
            {
                try { foreach (var combat in ClickControllers()) combat.BindDispatchTarget(target, turn); }
                catch (Exception error) { PatchBridge.Logger?.Exception("Scoped input dispatch target was not bound", error); }
            }

            internal static int PointerDispatchClosures;
            internal static void PointerTickPostfix()
            {
                PointerDispatchClosures++;
                try { foreach (var combat in ClickControllers()) combat.EndPointerDispatch(); }
                catch (Exception error) { PatchBridge.Logger?.Exception("Pointer dispatch end was not observed", error); }
            }

            internal static IEnumerable<CodeInstruction> PointerTickTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                var done = generator.DefineLabel();
                var output = new List<CodeInstruction> { new CodeInstruction(OpCodes.Call, typeof(PatchMethods).GetMethod(nameof(PointerTickPrefix), BindingFlags.Static | BindingFlags.NonPublic)) };
                var begin = new CodeInstruction(OpCodes.Nop);
                begin.blocks.Add(new Harmony12.ILCopying.ExceptionBlock(Harmony12.ILCopying.ExceptionBlockType.BeginExceptionBlock, null));
                output.Add(begin);
                foreach (var instruction in instructions)
                {
                    if (instruction.opcode != OpCodes.Ret) { output.Add(instruction); continue; }
                    var leave = new CodeInstruction(OpCodes.Leave, done);
                    leave.labels.AddRange(instruction.labels); leave.blocks.AddRange(instruction.blocks);
                    output.Add(leave);
                }
                var finish = new CodeInstruction(OpCodes.Call, typeof(PatchMethods).GetMethod(nameof(PointerTickPostfix), BindingFlags.Static | BindingFlags.NonPublic));
                finish.blocks.Add(new Harmony12.ILCopying.ExceptionBlock(Harmony12.ILCopying.ExceptionBlockType.BeginFinallyBlock, null));
                output.Add(finish);
                var end = new CodeInstruction(OpCodes.Nop);
                end.blocks.Add(new Harmony12.ILCopying.ExceptionBlock(Harmony12.ILCopying.ExceptionBlockType.EndExceptionBlock, null));
                output.Add(end);
                var ret = new CodeInstruction(OpCodes.Ret); ret.labels.Add(done); output.Add(ret);
                return output;
            }

            // PR-1 review R1-F11: the click bracket. Every pair's controller is told; each attributes only its own actors' selection.
            internal static void ClickUnitPrefix(bool simulate)
            {
                try
                {
                    var selected = SelectionManager.Instance?.SelectedUnits;
                    var simulated = simulate || PointerController.SimulatingClick;
                    foreach (var combat in ClickControllers()) combat.BeginClickObservation(selected, simulated);
                }
                catch (Exception error) { PatchBridge.Logger?.Exception("Manual-target click was not observed", error); }
            }

            internal static void ClickUnitPostfix(UnityEngine.GameObject gameObject, bool __result)
            {
                try
                {
                    var clicked = gameObject == null ? null : gameObject.GetComponent<Kingmaker.View.UnitEntityView>()?.EntityData;
                    foreach (var combat in ClickControllers()) combat.EndClickObservation(clicked, __result);
                }
                catch (Exception error) { PatchBridge.Logger?.Exception("Manual-target click was not observed", error); }
            }

            // PR-1 review R1-F11 (round 8): installed ClickUnitHandler.OnClick has exactly one call to UnitCombatState.set_ManualTarget (IL_0363,
            // after each non-skipped unit's Run). It is replaced by ClickManualTargetAssignment (same stack: the unit's combat state, the clicked
            // unit), which performs the identical native assignment and reports it. Any other count leaves OnClick unchanged (no assignment is
            // then observed, so no ownership transfers) and is reported.
            internal static IEnumerable<CodeInstruction> ClickAssignmentTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                var code = instructions.ToList();
                var setter = typeof(UnitCombatState).GetProperty(nameof(UnitCombatState.ManualTarget))?.GetSetMethod(true);
                var hook = AccessTools.Method(typeof(PatchMethods), nameof(ClickManualTargetAssignment));
                var sites = code.Where(instruction => (instruction.opcode == OpCodes.Callvirt || instruction.opcode == OpCodes.Call) &&
                    Equals(instruction.operand, setter)).ToList();
                if (sites.Count != 1 || hook == null)
                {
                    PatchBridge.Logger?.Error("Manual-target click assignment site not found exactly once (" + sites.Count + "); click ownership is not transferred.");
                    return code;
                }
                sites[0].opcode = OpCodes.Call;
                sites[0].operand = hook;
                return code;
            }

            internal static void ClickManualTargetAssignment(UnitCombatState combatState, UnitEntityData target)
            {
                combatState.ManualTarget = target;   // the native assignment, unchanged
                try { foreach (var combat in ClickControllers()) combat.ObserveClickTargetAssignment(combatState, target); }
                catch (Exception error) { PatchBridge.Logger?.Exception("Manual-target click assignment was not observed", error); }
            }

            private static IEnumerable<MountedCombatController> ClickControllers()
            {
                var pairs = PatchBridge.Pairs;
                if (pairs != null) { foreach (var pair in pairs) if (pair?.Combat != null) yield return pair.Combat; yield break; }
                if (PatchBridge.Combat != null) yield return PatchBridge.Combat;
            }

            // Every native queue insertion (AddToQueue/AddToQueueFirst, Run's queue-instead path, custom handlers). The prefix
            // may have refused it, so only a command now in the queue is observed.
            internal static void QueueAcceptedPostfix(UnitCommands __instance, UnitCommand cmd)
            {
                if (PointerController.SimulatingClick || cmd == null || __instance == null) return;
                try
                {
                    if (__instance.Queue.Contains(cmd))
                        PatchBridge.CombatFor(__instance)?.ObserveAcceptedWork(__instance, cmd, MountedAcceptedWorkRoute.Queue);
                }
                catch (Exception error) { PatchBridge.Logger?.Exception("Queued pair work was not observed", error); }
            }

            internal static void MoveActionHoldPrefix(UnitMoveTo __instance, out bool __state)
            {
                __state = __instance?.Executor != null && __instance.Executor.HoldState;
            }

            internal static void MoveActionHoldPostfix(UnitMoveTo __instance, bool __state)
            {
                if (PointerController.SimulatingClick || __instance?.Executor == null) return;
                try { PatchBridge.CombatFor(__instance.Executor)?.RestoreHoldAfterMoveAction(__instance, __state); }
                catch (Exception error) { PatchBridge.Logger?.Exception("Own carrier Hold write was not restored", error); }
            }

            internal static IEnumerable<CodeInstruction> NativeRunHoldTranspiler(IEnumerable<CodeInstruction> instructions) =>
                NativeRunHoldBoundary.Rewrite(instructions,
                    typeof(PatchMethods).GetMethod(nameof(NativeRunHoldWrite), BindingFlags.Static | BindingFlags.NonPublic));

            private static void NativeRunHoldWrite(UnitEntityData owner, UnitCommand cmd, bool fromQueue, bool doNotClearQueue)
            {
                if (PatchBridge.CombatFor(owner)?.NativeRunKeepsHold(owner, cmd, fromQueue, doNotClearQueue) != true)
                    owner.HoldState = false;
            }

            // Game.ResetToMainMenu, the only gameplay caller of StopAll. Deferred
            // while an owned archive worker can still commit; the service replays
            // it with the same arguments once the save has finished.
            internal static bool ResetToMainMenuPrefix(string message, Kingmaker.Blueprints.Area.BlueprintAreaPreset preset) =>
                PatchBridge.Persistence == null || !PatchBridge.Persistence.DeferResetToMainMenu(message, preset);

            // While a deferred save waits, new actions are held; the registered Mount/Dismount shell the save itself waits for
            // (SaveEffectsReady: OwnsUnsettledRelationshipShell) must still start, or the hold and the readiness term deadlock
            // until the bounded wait refuses the save (preview.208 combat-mount-unsettled-save).
            internal static bool SaveCommandStartPrefix(UnitCommand __instance) =>
                PatchBridge.Persistence?.CombatRestorationPending != true &&
                (PatchBridge.Persistence?.Enabled != true || NativeSaveEffectBoundary.MayStartDuringWait(__instance) ||
                 PatchBridge.NativeControls?.OwnsUnsettledRelationshipShell(__instance) == true ||
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
                (PatchBridge.CombatFor(__instance?.Executor)?.AllowChargeExecution(__instance) != false) &&
                (PatchBridge.ChargeSafety == null || PatchBridge.ChargeSafety.AllowExecution(__instance));

            // Known issues C04: a seated rider whose carried position moved without native motion is checked against the
            // active script zones with the native ScriptZone.CheckUnit. A fault leaves native membership as it was.
            internal static void ScriptZoneTickPostfix()
            {
                try
                {
                    List<Kingmaker.View.MapObjects.SriptZones.ScriptZone> zones = null;
                    if (PatchBridge.World == null) MountedScriptZoneAdapter.Recheck(PatchBridge.Service, ref zones, PatchBridge.Logger);
                    else
                    {
                        var errors = PatchBridge.World.Visit(pair => MountedScriptZoneAdapter.Recheck(pair.Relationship, ref zones, PatchBridge.Logger));
                        if (errors.Length != 0) throw new AggregateException(errors);
                    }
                }
                catch (Exception error) { ReportScriptZoneFault("Mounted script zones: a seated rider's membership was left native", error); }
            }

            // Known issues C02: around each native scene placement operation, every mounted pair's positions are captured and
            // a rider the operation alone wrote is attributed to the scene. A fault leaves the placement unattributed.
            // Review R8 of e1c0c7ad: each operation's prefix names it exactly; CommandTeleportParty's also reads its proven
            // target set as native will (single-unit operations carry none). Review R10 of 813d00a1: the commands' prefixes
            // take the original CutscenePlayerData argument, and who owns the operation is captured here, at entry.
            internal static void TranslocateUnitCapturePrefix(out object __state) =>
                __state = CaptureScenePlacement(MountedScenePlacementSource.TranslocateUnit, null, null);

            internal static void CommandMoveUnitCapturePrefix(Kingmaker.AreaLogic.Cutscenes.Commands.CommandMoveUnit __instance,
                Kingmaker.AreaLogic.Cutscenes.CutscenePlayerData player, out object __state) =>
                __state = CaptureScenePlacement(MountedScenePlacementSource.CommandMoveUnitTimedOut, __instance, player);

            internal static void CommandTeleportPartyCapturePrefix(Kingmaker.AreaLogic.Cutscenes.Commands.CommandTeleportParty __instance,
                Kingmaker.AreaLogic.Cutscenes.CutscenePlayerData player, out object __state) =>
                __state = CaptureScenePlacement(MountedScenePlacementSource.CommandTeleportParty, __instance, player);

            internal static object CaptureScenePlacement(MountedScenePlacementSource source, Kingmaker.AreaLogic.Cutscenes.CommandBase command,
                Kingmaker.AreaLogic.Cutscenes.CutscenePlayerData player)
            {
                try
                {
                    var owners = PatchBridge.World == null ? new[] { PatchBridge.Service } : PatchBridge.World.Pairs.Select(pair => pair.Relationship);
                    var targets = source == MountedScenePlacementSource.CommandTeleportParty
                        ? MountedScenePlacementAttribution.TeleportPartyTargets(command as Kingmaker.AreaLogic.Cutscenes.Commands.CommandTeleportParty) : null;
                    return MountedScenePlacementAttribution.Take(source, () =>
                    {
                        var game = Kingmaker.Game.Instance;
                        var foreground = game != null && MountedSceneContinuityPolicy.SceneActive(game.CutsceneLock,
                            game.IsModeActive(Kingmaker.GameModes.GameModeType.Dialog), game.IsModeActive(Kingmaker.GameModes.GameModeType.Cutscene));
                        return NativeSceneOperationOwnership.Decide(source, foreground, command, player);
                    }, owners, targets);
                }
                catch (Exception error) { ReportScenePlacementFault("Mounted scene placement: the positions were not captured", error); return null; }
            }

            internal static void TranslocateUnitPostfix(object __state) => AttributeScenePlacement(__state);

            internal static void CommandMoveUnitPostfix(object __state) => AttributeScenePlacement(__state);

            internal static void CommandTeleportPartyPostfix(object __state) => AttributeScenePlacement(__state);

            private static void AttributeScenePlacement(object state)
            {
                try { MountedScenePlacementAttribution.Attribute(state as MountedScenePlacementAttribution.Capture, PatchBridge.Logger); }
                catch (Exception error) { ReportScenePlacementFault("Mounted scene placement: the placement was left unattributed", error); }
            }

            // Review R6 of 263ac3ea: C04's script-zone hook and C02's scene-placement hooks each have their own bounded
            // report, so a fault in one never silences the other's first diagnostic.
            private static readonly MountedBoundedFaultReport scriptZoneFault = new MountedBoundedFaultReport();
            private static void ReportScriptZoneFault(string message, Exception error) =>
                scriptZoneFault.Report((text, fault) => PatchBridge.Logger?.Exception(text, fault), message, error);

            private static readonly MountedBoundedFaultReport scenePlacementFault = new MountedBoundedFaultReport();
            private static void ReportScenePlacementFault(string message, Exception error) =>
                scenePlacementFault.Report((text, fault) => PatchBridge.Logger?.Exception(text, fault), message, error);

            internal static void NativeMovementUpdatePrefix()
            {
                if (PatchBridge.World == null) PatchBridge.Service?.BeginNativeMovementUpdate();
                else PatchBridge.World.Visit(pair => pair.Relationship.BeginNativeMovementUpdate());
            }

            internal static void NativeGroupAccelerationPrefix(UnitCommand __instance, out bool __state)
            {
                __state = __instance != null && __instance.IsAccelerated;
            }

            // After native AccelerateIfPathIsLong offered Accelerate() to every selected unit's commands, offer it to
            // each pair's mount that executes its selected rider's routed ground order (MountedGroupPacePolicy).
            internal static void NativeGroupAccelerationPostfix(UnitCommand __instance, bool __state)
            {
                if (__instance == null || !MountedGroupPacePolicy.NativeGroupOfferRan(__state, __instance.IsAccelerated)) return;
                try
                {
                    if (PatchBridge.World == null) PatchBridge.Combat?.CarryNativeGroupAcceleration();
                    else foreach (var error in PatchBridge.World.Visit(pair => pair.Combat?.CarryNativeGroupAcceleration()))
                        PatchBridge.Logger?.Exception("Mounted group pace was not carried to a mount", error);
                }
                catch (Exception error) { PatchBridge.Logger?.Exception("Mounted group pace was not carried to a mount", error); }
            }

            // Known issues sprint 3 (M03). Same shape as the Enumerable.Select it replaces in the native group speed
            // limit: each selected unit is answered by its physical mover, each mover once, and the native selector
            // still reads the speed. A fault leaves the native inputs exactly as they were.
            internal static IEnumerable<float> GroupPaceSpeedInputs(IEnumerable<UnitEntityData> selected, Func<UnitEntityData, float> nativeSpeed)
            {
                var actors = selected;
                try { actors = MountedPaceActors.Project(selected, unit => PatchBridge.Owner(unit)?.GroupPaceSpeedMover(unit)); }
                catch (Exception error) { ReportGroupPaceFault("Mounted group pace: the speed inputs were left native", error); actors = selected; }
                return actors.Select(nativeSpeed);
            }

            // Known issues sprint 3 (M02). The list the native gamepad follower pass walks: a selected rider whose
            // routed ground order runs on its unselected mount is answered by that mount. A fault leaves the selection.
            internal static List<UnitEntityData> GamepadFollowerOfferUnits(List<UnitEntityData> selected)
            {
                try { return MountedPaceActors.Project(selected, unit => PatchBridge.CombatFor(unit)?.GamepadFollowerOfferMover(unit, selected)); }
                catch (Exception error) { ReportGroupPaceFault("Mounted group pace: the gamepad follower pass was left native", error); return selected; }
            }

            // Known issues sprint 3 (M04). A carried rider's own plan for its entrance member command places no second
            // marker beside the mount's. A fault leaves the native placement.
            internal static bool CarriedRiderPointerPrefix(UnitEntityView __instance)
            {
                try
                {
                    var unit = __instance != null ? __instance.EntityData : null;
                    return PatchBridge.CombatFor(unit)?.SkipCarriedRiderPointer(unit) != true;
                }
                catch (Exception error) { ReportGroupPaceFault("Mounted entrance approach: the rider's pointer placement was left native", error); return true; }
            }

            // Known issues M04 closeout (review of b1a88b4e, R3): native AddPointer ran for this unit; the placement is
            // recorded for the command it was made for (diagnostics). A fault leaves the native placement unrecorded.
            internal static void PointerPlacedPostfix(UnitEntityData unit)
            {
                try { MountedDestinationMarkers.RecordPlacement(unit, UnityEngine.Time.frameCount); }
                catch (Exception error) { ReportMarkerFault("Mounted marker observation: a pointer placement was not recorded", error); }
            }

            // These hooks run many times a second; one fault is reported once. A logger that itself fails is contained
            // (MountedBoundedFaultReport), so the caller's fallback to native behaviour always completes.
            private static readonly MountedBoundedFaultReport groupPaceFault = new MountedBoundedFaultReport();
            private static void ReportGroupPaceFault(string message, Exception error) =>
                groupPaceFault.Report((text, fault) => PatchBridge.Logger?.Exception(text, fault), message, error);

            // Known issues sprint 3 continuation (M05). The unit native stick movement drives for the selected unit: a
            // mounted pair's rider is answered by its mount. A fault leaves the selected unit.
            internal static UnitEntityData GamepadDrivenUnit(UnitEntityData selected)
            {
                try { return PatchBridge.Owner(selected)?.ContinuousDriveMover(selected) ?? selected; }
                catch (Exception error) { ReportGroupPaceFault("Mounted stick movement: the driven unit was left native", error); return selected; }
            }

            // The unit the native companion formation is anchored on: the selected rider of a stick-driven mount.
            internal static UnitEntityData GamepadFormationUnit(UnitEntityData unit)
            {
                try
                {
                    var owner = PatchBridge.Owner(unit);
                    if (owner == null) return unit;
                    return owner.ContinuousDriveFormationUnit(unit, Kingmaker.UI._ConsoleUI.Models.UnitSelectionManager.Instance?.CurrentSelectUnitValue) ?? unit;
                }
                catch (Exception error) { ReportGroupPaceFault("Mounted stick movement: the formation anchor was left native", error); return unit; }
            }

            // The override native reads to learn whether the current unit is being stick-driven: for a mounted pair's
            // rider, its mount's. A fault leaves the native read.
            internal static UnitMovementAgentBase GamepadDrivenOverride(UnitEntityView view)
            {
                if (ReferenceEquals(view, null)) return null;
                try
                {
                    var unit = view.EntityData;
                    var mover = PatchBridge.Owner(unit)?.ContinuousDriveMover(unit);
                    var moverView = mover != null ? mover.View : null;
                    return (!ReferenceEquals(moverView, null) ? moverView : view).AgentOverride;
                }
                catch (Exception error) { ReportGroupPaceFault("Mounted stick movement: the stick-driven test was left native", error); return view.AgentOverride; }
            }

            // The one write native OnEnded makes to the override of the ending command's executor view. A fault leaves
            // the native write.
            internal static void EndedContinuousCommandOverride(UnitEntityView view, UnitMovementAgentBase value, UnitMoveContiniously command)
            {
                var mayClear = true;
                try { mayClear = GameMountedRelationshipService.ContinuousDriveEndMayClearOverride(command, view); }
                catch (Exception error) { ReportGroupPaceFault("Mounted stick movement: the command's native end was left native", error); }
                if (mayClear) view.AgentOverride = value;
            }

            internal static IEnumerable<CodeInstruction> ContinuousMoveEndTranspiler(IEnumerable<CodeInstruction> instructions) =>
                ContinuousDriveTranspilers.EndedCommandOverrideClear(instructions, Hook(nameof(EndedContinuousCommandOverride)));

            internal static IEnumerable<CodeInstruction> GamepadMovementTranspiler(IEnumerable<CodeInstruction> instructions) =>
                GamepadFollowerOfferTranspiler(ContinuousDriveTranspilers.DrivenUnit(instructions,
                    Hook(nameof(GamepadDrivenUnit)), Hook(nameof(GamepadFormationUnit))));
            internal static IEnumerable<CodeInstruction> GroundOrderTranspiler(IEnumerable<CodeInstruction> instructions) =>
                ContinuousDriveTranspilers.DrivenOverrideRead(GroundGroupMoveTranspiler(instructions), Hook(nameof(GamepadDrivenOverride)));

            internal static IEnumerable<CodeInstruction> GroundGroupMoveTranspiler(IEnumerable<CodeInstruction> instructions) =>
                GroupPaceTranspilers.SpeedInputs(PairedControllerInputTranspiler(instructions), Hook(nameof(GroupPaceSpeedInputs)));
            internal static IEnumerable<CodeInstruction> GamepadFollowerOfferTranspiler(IEnumerable<CodeInstruction> instructions) =>
                GroupPaceTranspilers.GamepadFollowerOffer(instructions, Hook(nameof(GamepadFollowerOfferUnits)));

            internal static bool GroundCommandPrefix(ref UnitEntityData unit)
            {
                if (PointerController.SimulatingClick) { return true; }
                var combat = PatchBridge.CombatFor(unit);
                if (combat != null && !combat.TryAdmitGroundCommand(unit))
                {
                    return false;
                }
                var owner = PatchBridge.Owner(unit);
                var runsNatively = owner == null || owner.RouteGroundCommand(ref unit,
                    PatchBridge.UnifiedTurn?.MaySelectPairedPartner(unit) ?? false);
                if (runsNatively) combat?.OpenGroundOrder();
                return runsNatively;
            }

            internal static void GroundCommandPostfix(UnitEntityData unit)
            {
                if (PointerController.SimulatingClick) { return; }
                PatchBridge.CombatFor(unit)?.CompleteGroundCommandAdmission(unit);
            }

            internal static bool UnitCommandRunPrefix(UnitCommands __instance, ref UnitCommand cmd, out MountedCarriedRequestTicket __state)
            {
                // PR #8 review correction A: no ticket on any refusal or native-only path, so nothing a refused,
                // simulated or preparation request carries can release the pair's carried approach.
                __state = null;
                // Bind a same-frame native hostile click to the exact command its own Run carries,
                // before any refusal below, so no later command can borrow or erase that click.
                PatchBridge.CombatFor(__instance)?.BindObservedStockRequest(__instance, cmd);
                if (PatchBridge.Persistence?.CombatRestorationPending == true) return false;
                // While an owned archive worker can still commit, no unit -- owned pair
                // or not -- may run or queue a command: the worker is serializing the
                // live graphs those queues live in. Refused, not deferred: the world is
                // held until the worker settles.
                if (PatchBridge.Persistence?.ActiveSaveWorkerRunning == true) return false;
                // Native TB cursor prediction replaces Unit.Commands temporarily. Its fake orders
                // must stay entirely native; routing one can cancel a real pair order or move its mount.
                if (PointerController.SimulatingClick) { return true; }
                // Cutscene continuity repair: a scripted scene operation on a mounted rider hands that one pair to native
                // staging before any pair routing, so native Run executes the scene's command on the free rider. A
                // fault here never blocks the scene's command: the pair then keeps its attachment.
                var sceneOperation = false;
                try { sceneOperation = PatchBridge.Persistence?.ObserveSceneCommand(__instance, cmd, PointerController.SimulatingClick) == true; }
                catch (Exception error) { PatchBridge.Logger?.Exception("Scene staging handoff was not evaluated", error); }
                if (PatchBridge.CombatFor(__instance)?.AdmitChargeCommand(__instance, cmd) == false) return false;
                if (PatchBridge.UnifiedTurn?.AdmitNativePreparationCommand(__instance, cmd) == true) return true;
                // A scripted scene operation runs as the scene authored it: the door, stock-attack and carried-approach
                // gates below exist for the player's own orders and would refuse or reroute it, stalling the scene.
                if (sceneOperation) return true;
                var combat = PatchBridge.CombatFor(__instance);
                if (combat == null) return true;
                // The door, stock-attack and stock-command gates in their existing order. The carried approach is
                // released only after native Run executed an admitted request, from what Run displaced (postfix).
                return combat.AdmitPairCommandRun(__instance, ref cmd, out __state);
            }

            internal static void UnitCommandRunPostfix(UnitCommands __instance, UnitCommand cmd, MountedCarriedRequestTicket __state)
            {
                PatchBridge.CombatFor(__instance)?.ObserveGroundOrderRun(__instance, cmd);
                if (__state != null) PatchBridge.CombatFor(__instance)?.CompletePairCommandRun(__state, cmd);
                // Entrance repair: after native Run executed the rider's own UnitAreaTransition (and its own request ticket
                // settled), its horse, a native member of the same group that selection projected onto the rider,
                // receives its member command.
                try { PatchBridge.CombatFor(__instance)?.CarryRiderEntranceTransition(__instance, cmd); }
                catch (Exception error) { PatchBridge.Logger?.Exception("Mounted entrance approach was not carried", error); }
                // Known issues sprint 3 continuation (M05): native Run was called with a pair's bound stick movement; a
                // command it did not admit to the mount's Move slot leaves no override behind.
                // This postfix also runs when the prefix above refused the command, which is an admission failure too.
                try { if (cmd is UnitMoveContiniously) PatchBridge.Owner(cmd.Executor)?.ObserveContinuousDriveRun(cmd); }
                catch (Exception error) { ReportGroupPaceFault("Mounted stick movement: a command run was not settled", error); }
            }

            internal static void NativeAbilityInitPostfix(UnitUseAbility __instance)
            {
                PatchBridge.NativeControls?.PrepareNativeMountApproach(__instance);
                PatchBridge.NativeControls?.PrepareNativePrimaryIntentShell(__instance);
            }

            // The OnAction boundary is where UnitUseAbility sets its ExecutionProcess.
            // Binding the relationship shell to that exact execution context here is
            // what lets Deliver prove ownership on a later frame, after the command has
            // completed and left the Move slot.
            internal static void NativeAbilityActionPostfix(UnitUseAbility __instance)
            {
                PatchBridge.NativeControls?.BindNativeRelationshipProcess(__instance);
            }

            internal static Action<bool> BeginNativeChargeAction(UnitUseAbility __instance) =>
                PatchBridge.CombatFor(__instance?.Executor)?.BeginChargeNativeAction(__instance);

            internal static IEnumerable<CodeInstruction> NativeAbilityActionTranspiler(IEnumerable<CodeInstruction> instructions,
                ILGenerator generator, MethodBase original) => NativeChargeActionBoundary.Wrap(instructions, generator, original,
                    typeof(PatchMethods).GetMethod(nameof(BeginNativeChargeAction), BindingFlags.Static | BindingFlags.NonPublic),
                    typeof(PatchMethods).GetMethod(nameof(TriggerNativeChargeRule), BindingFlags.Static | BindingFlags.NonPublic));

            internal static Kingmaker.RuleSystem.Rules.Abilities.RuleCastSpell TriggerNativeChargeRule(
                Kingmaker.RuleSystem.Rules.Abilities.RuleCastSpell rule, UnitUseAbility shell)
            {
                var combat = PatchBridge.CombatFor(shell?.Executor);
                var owned = combat?.OwnsChargeNativeAction(shell) == true;
                if (owned) combat.CaptureChargeNativeRule(shell, rule, Kingmaker.Game.Instance?.AbilityExecutor);
                if (owned) MountedChargeAdmissionFault.FireNativeAction("before-rule");
                var result = Kingmaker.RuleSystem.Rulebook.Trigger(rule);
                if (owned) MountedChargeAdmissionFault.FireNativeAction("after-rule");
                return result;
            }

            internal static void NativeMountMovementInterruptedPrefix(UnitEntityView __instance, out UnitUseAbility __state)
            {
                __state = PatchBridge.NativeControls?.CaptureNativeMountApproachInterruption(__instance);
            }

            internal static void NativeMountMovementInterruptedPostfix(UnitEntityView __instance, UnitUseAbility __state)
            {
                PatchBridge.NativeControls?.CompleteNativeMountApproachInterruption(__instance, __state);
            }

            internal static void RelationshipControlAutoUsePostfix(Kingmaker.UnitLogic.Abilities.AbilityData __instance, ref bool __result)
            {
                if (PatchBridge.NativeControls != null && PatchBridge.NativeControls.IsPlayerOnlyRelationshipControl(__instance))
                    __result = false;
            }

            internal static void CommandInterruptPrefix(UnitCommand __instance)
            {
                PatchBridge.CombatFor(__instance?.Executor)?.ObserveCommandInterrupt(__instance);
            }

            internal static bool PairedCommandInterruptPrefix(UnitCommands __instance, UnitCommand.CommandType type, bool interruptPaired)
            {
                return PatchBridge.CombatFor(__instance)?.PreservesApproachParent(__instance, type, interruptPaired) != true;
            }

            internal static bool MountedAttackApproachPrefix(UnitCommand __instance)
            {
                if (!ChargeExecutionPrefix(__instance)) return false;
                // The pair command owns its mount Move slot and drives it through
                // the existing native/off-executor movement paths. Running the
                // base approach too would stop that move or start rider navigation.
                // The mounted door command approaches the same way while unstarted (Sprint 2): the
                // mount's delegated move replaces the rider's native approach, and the native action
                // controller starts the command only once CanStart reports the pair in range.
                var door = __instance as MountedDoorInteractionCommand;
                if (door != null)
                {
                    door.TickMountedApproach();
                    return false;
                }
                var pairAttack = __instance as MountedPairAttackCommand;
                if (pairAttack != null)
                {
                    // MMR-05 (review R3-F1): native ticks an unstarted command's approach while its start waits; the pair
                    // command's approach is its own delegated mount move.
                    // The horse striker's own native approach runs only when the command asks for it.
                    return !pairAttack.IsStarted && pairAttack.TickPreStartApproach();
                }
                // Sprint 3 (TRAP-01, SPELL-01): the rider's own unstarted native ability or discovered-trap interaction is
                // approached by the mount. The driver runs the engine's own OnTickApproaching first and skips only the
                // native rider approach; in range it returns false and the native code starts the command as usual.
                var carrier = PatchBridge.CombatFor(__instance?.Executor);
                if (carrier != null && carrier.TryTickCarriedApproach(__instance, IsRelationshipControlShell(__instance))) return false;
                return true;
            }

            private static bool IsRelationshipControlShell(UnitCommand command)
            {
                var controls = PatchBridge.NativeControls;
                return controls != null &&
                    (controls.OwnsUnsettledRelationshipShell(command) ||
                     controls.IsPlayerOnlyRelationshipControl((command as UnitUseAbility)?.Spell));
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
                MountedAnimationAdapter.ApplyNaturalHorseGait(__instance, PatchBridge.HorseCompanion?.HorseUnit);
                (PatchBridge.World?.ForActor(__instance?.View?.EntityData)?.Animation ??
                    (PatchBridge.World == null ? PatchBridge.Animation : null))?.RestoreExactDelegatedMountLocomotion(__instance);
            }

            internal static void AttackAnimationPostfix(AttackHandInfo __instance)
            {
                if (PatchBridge.World == null) PatchBridge.Animation?.SupplyExactHorsePrimaryAnimation(__instance);
                else PatchBridge.World.Visit(pair => pair.Animation.SupplyExactHorsePrimaryAnimation(__instance));
            }

            internal static void DollRoomIkSetupPrefix(IKController __instance)
            {
                if (PatchBridge.World == null) PatchBridge.DollRoomIk?.BindExactMountedRiderIfRequired(__instance);
                else PatchBridge.World.Visit(pair => pair.DollRoomIk.BindExactMountedRiderIfRequired(__instance));
            }

            internal static void DollRoomFbbikPrefix(IKController __instance, out MountedDollRoomIkAdapter __state)
            {
                __state = null;
                var candidates = PatchBridge.Pairs?.Select(pair => pair.DollRoomIk) ?? new[] { PatchBridge.DollRoomIk };
                foreach (var adapter in candidates)
                    if (adapter?.BeginExactFbbikObservation(__instance) == true) { __state = adapter; break; }
            }

            internal static void DollRoomFbbikPostfix(MountedDollRoomIkAdapter __state)
            {
                __state?.CompleteExactFbbikObservation(true);
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
            internal static bool ChargeModeBoundaryPrefix(CombatController __instance, bool enabled, out bool __state)
            {
                __state = false;
                if (!MountedCombatController.NativeModeChanges(__instance, enabled)) return true;
                __state = PatchBridge.Combat?.AdmitChargeNativeBoundary(__instance, MountedChargeBoundaryKind.Mode, enabled) ?? true;
                return __state;
            }
            internal static void ChargeModeBoundaryPostfix(bool enabled, bool __state)
            {
                if (__state) PatchBridge.Combat?.CompleteChargeNativeMode(enabled);
            }
            internal static bool ChargeCombatBoundaryPrefix(CombatController __instance, bool inCombat) =>
                PatchBridge.Combat?.AdmitChargeNativeBoundary(__instance, MountedChargeBoundaryKind.PartyCombat, inCombat) ?? true;
            internal static bool ChargeTransitionTickPrefix() => PatchBridge.Combat?.ChargeNativeBoundaryPending != true;
            internal static bool CombatControllerTickPrefix() => ChargeTransitionTickPrefix() && (PatchBridge.Persistence?.BeforeCombatTick() ?? true);
            internal static void PairedWaitingPostfix(TurnController __instance, ref bool __result) =>
                PatchBridge.UnifiedTurn?.ExtendPairedWaiting(__instance, ref __result);
            internal static void PairedForfeitPrefix(TurnController __instance, bool setCooldowns) => PatchBridge.UnifiedTurn?.ForfeitPairedActivation(__instance, setCooldowns);
            internal static void PairedModeExitPrefix(CombatController __instance)
            {
                if (PatchBridge.Persistence?.LoadingWorld != true) PatchBridge.UnifiedTurn?.BeforeNativeModeExit(__instance);
            }
            internal static bool PairedActorRemovalPrefix(CombatController __instance, UnitEntityData unit)
            {
                if (PatchBridge.Combat?.AdmitChargeActorRemoval(__instance, unit) == false) return false;
                PatchBridge.UnifiedTurn?.BeforePairedActorRemoval(unit);
                return true;
            }
            internal static void PairedTickPrefix(TurnController __instance) => PatchBridge.UnifiedTurn?.TickPairedNativeState(__instance);
            internal static bool PairedInputPredictionPrefix(TurnController __instance) =>
                PatchBridge.UnifiedTurn?.PrepareNativeInputPrediction(__instance) ?? true;
            internal static bool PairedIgnoreClickPrefix(TurnController __instance, ref bool __result) =>
                PatchBridge.UnifiedTurn == null || PatchBridge.UnifiedTurn.ShouldRunNativeIgnoreClick(__instance, ref __result);
            internal static void PairedIgnoreClickPostfix(TurnController __instance, ref bool __result) =>
                PatchBridge.UnifiedTurn?.AdmitPairedAttackClick(__instance, ref __result);
            internal static bool PairedPreserveSelection(TurnController turn) => PatchBridge.UnifiedTurn?.PreservePartnerSelection(turn) ?? false;
            internal static TurnController PairedInputContext(TurnController turn) => PatchBridge.UnifiedTurn?.SelectedNativeInputContext(turn) ?? turn;
            internal static UnitEntityData PairedPathInputUnit() =>
                PairedInputContext(Kingmaker.Game.Instance.TurnBasedCombatController.CurrentTurn)?.Unit ?? CombatController.CurrentUnit;
            internal static TurnController PairedVmContext(object viewModel) => PairedInputContext(Kingmaker.Game.Instance.TurnBasedCombatController.CurrentTurn);
            internal static TurnController PairedMovementContext(TurnController turn) => PatchBridge.UnifiedTurn?.PairedMovementContext(turn) ?? turn;
            internal static UnitEntityData PairedPathMovementUnit() =>
                PairedMovementContext(Kingmaker.Game.Instance.TurnBasedCombatController.CurrentTurn)?.Unit ?? CombatController.CurrentUnit;
            internal static Kingmaker.TurnBasedMode.Controllers.CombatAction.ActionState PairedPathPredictionState(
                Kingmaker.TurnBasedMode.Controllers.CombatAction action) =>
                PatchBridge.UnifiedTurn?.PairedMovementPredictionState(action, action.PredictionState) ?? action.PredictionState;
            // The pair's travel runs on the mount while the rider is selected; its destination is the selected command's.
            internal static bool PairedPrimaryMarker(List<UnitEntityData> selected, UnitEntityData key)
            {
                if (selected.Contains(key)) return true;
                var owner = PatchBridge.Owner(key);
                return owner != null && owner.State == RelationshipState.Mounted && owner.PairOwner?.Mount == key &&
                    selected.Contains(owner.PairOwner.Rider);
            }
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
            internal static void MountedCombatDefense(Kingmaker.RuleSystem.Rules.RuleAttackRoll roll) => PatchBridge.Defense?.ResolveAttack(roll);
            internal static IEnumerable<CodeInstruction> MountedCombatDefenseTranspiler(IEnumerable<CodeInstruction> instructions) =>
                PairedActivationTranspilers.AttackRollResolved(instructions, Hook(nameof(MountedCombatDefense)));
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
                PairedActivationControlTranspilers.PredictionStateReads(PairedActivationControlTranspilers.PathUnitReads(
                    PairedActivationControlTranspilers.ControllerInput(instructions, Hook(nameof(PairedMovementContext))),
                    Hook(nameof(PairedPathMovementUnit)), 2), Hook(nameof(PairedPathPredictionState)), 2);
            internal static IEnumerable<CodeInstruction> PairedPrimaryMarkerTranspiler(IEnumerable<CodeInstruction> instructions) =>
                PairedActivationControlTranspilers.SelectedMarker(instructions, Hook(nameof(PairedPrimaryMarker)));
            // The mount's private ActionsState reads the mount's own movement range; every other state keeps its reader.
            internal static TurnController PairedActionsStateOwner(TurnController current, Kingmaker.TurnBasedMode.ActionsState states) =>
                PatchBridge.UnifiedTurn?.ActionsStateContext(states) ?? current;
            internal static TurnController PairedPathStatesOwner(TurnController current, Kingmaker.TurnBasedMode.ActionsState states) =>
                PatchBridge.UnifiedTurn?.ActionsStateContext(states) ?? PairedInputContext(current);
            internal static IEnumerable<CodeInstruction> PairedActionsStateOwnerTranspiler(IEnumerable<CodeInstruction> instructions) =>
                PairedActivationControlTranspilers.ActionsStateOwnerReads(instructions, Hook(nameof(PairedActionsStateOwner)), 0, 5);
            internal static IEnumerable<CodeInstruction> PairedPathStatesTranspiler(IEnumerable<CodeInstruction> instructions) =>
                PairedActivationControlTranspilers.ActionsStateOwnerReads(instructions, Hook(nameof(PairedPathStatesOwner)), 1, 1);
            internal static void PairedPathStatesPostfix(Kingmaker.TurnBasedMode.PathVisualizer __instance, Kingmaker.TurnBasedMode.ActionsState actionsState) =>
                PatchBridge.UnifiedTurn?.FillPairedMountStates(__instance, actionsState);
            internal static void PairedSlotActionPostfix(ref Kingmaker.TurnBasedMode.Controllers.CombatAction __result)
            {
                if (PatchBridge.UnifiedTurn != null) __result = PatchBridge.UnifiedTurn.PairedSlotAction(__result);
            }
            private static readonly FieldInfo SlotController = ExactSlotController();
            private static FieldInfo ExactSlotController()
            {
                var field = typeof(Kingmaker.UI._ConsoleUI.TurnBasedMode.PredictionPanelActionVM).GetField("Controller", BindingFlags.Instance | BindingFlags.NonPublic);
                if (field == null || field.MetadataToken != 0x04003463) throw new MissingFieldException("PredictionPanelActionVM", "Controller exact token 04003463");
                return field;
            }
            private static Kingmaker.TurnBasedMode.ActionsState SlotStates(Kingmaker.UI._ConsoleUI.TurnBasedMode.PredictionPanelFullRoundActionVM slot) =>
                ((TurnController)SlotController.GetValue(slot)).ActionsStates;
            internal static Kingmaker.TurnBasedMode.Controllers.CombatAction PairedFullRoundMove(Kingmaker.UI._ConsoleUI.TurnBasedMode.PredictionPanelFullRoundActionVM slot)
            {
                var native = SlotStates(slot).Move;
                return PatchBridge.UnifiedTurn?.PairedSlotAction(native) ?? native;
            }
            internal static Kingmaker.TurnBasedMode.Controllers.CombatAction PairedFullRoundStandard(Kingmaker.UI._ConsoleUI.TurnBasedMode.PredictionPanelFullRoundActionVM slot)
            {
                var native = SlotStates(slot).Standard;
                return PatchBridge.UnifiedTurn?.PairedSlotAction(native) ?? native;
            }
            internal static IEnumerable<CodeInstruction> PairedFullRoundStatesTranspiler(IEnumerable<CodeInstruction> instructions) =>
                PairedActivationControlTranspilers.FullRoundSlotReads(instructions, Hook(nameof(PairedFullRoundMove)), Hook(nameof(PairedFullRoundStandard)), 9, 9);
            internal static IEnumerable<CodeInstruction> PairedFullRoundDistanceTranspiler(IEnumerable<CodeInstruction> instructions) =>
                PairedActivationControlTranspilers.FullRoundSlotReads(instructions, Hook(nameof(PairedFullRoundMove)), Hook(nameof(PairedFullRoundStandard)), 3, 3);
            internal static IEnumerable<CodeInstruction> PairedFullRoundTextTranspiler(IEnumerable<CodeInstruction> instructions) =>
                PairedActivationControlTranspilers.FullRoundSlotReads(instructions, Hook(nameof(PairedFullRoundMove)), Hook(nameof(PairedFullRoundStandard)), 4, 5);

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
                // Review of 292cd23c (R1): a rebuilt tracker keeps KMC's redirect only while its exact entry and pair hold.
                try { PatchBridge.Presentation?.ValidateHoverLease("tracker-rebuilt"); }
                catch (Exception exception) { ReportPresentationFault("folded mount hover validation", exception); }
            }

            internal static void TickCommandTurnBasedPostfix(UnitCommand command, ref bool __result)
            {
                RuntimeAutomationHost.ObserveNativeTurnBasedCommandEligibility(command, __result);
                PatchBridge.UnifiedTurn?.AdmitExactMountCommand(command, ref __result);
            }

            internal static bool SelectUnitPrefix(ref UnitEntityView unit, bool single)
            {
                var owner = PatchBridge.Owner(unit?.EntityData);
                return owner == null || owner.NormalizeSingleSelection(ref unit, single,
                    PatchBridge.UnifiedTurn?.MaySelectPairedPartner(unit?.EntityData) ?? false);
            }

            internal static void MultiSelectPrefix(ref IEnumerable<UnitEntityView> views)
            {
                if (PatchBridge.World == null)
                    PatchBridge.Service?.NormalizeMultiSelection(ref views,
                        PatchBridge.UnifiedTurn?.MaySelectPairedPartner(PatchBridge.Service?.Mount) ?? false);
                else foreach (var pair in PatchBridge.World.Pairs)
                    pair.Relationship.NormalizeMultiSelection(ref views,
                        PatchBridge.UnifiedTurn.MaySelectPairedPartner(pair.Relationship.Mount));
            }

            // Native Hold: the Stop/Hold cancellation, then the mount's Hold recorded as the player's when it is selected.
            internal static void HoldPrefix()
            {
                StopOrHoldPrefix();
                if (PointerController.SimulatingClick) return;
                if (PatchBridge.World == null) PatchBridge.Combat?.ObserveHoldInput();
                else PatchBridge.World.Visit(pair => pair.Combat.ObserveHoldInput());
            }

            internal static void StopOrHoldPrefix()
            {
                if (PointerController.SimulatingClick) { return; }
                if (PatchBridge.World == null)
                { PatchBridge.Combat?.CancelSelectedInput("stop or hold"); PatchBridge.Service?.ForwardStopOrHold(); }
                else PatchBridge.World.Visit(pair =>
                { pair.Combat.CancelSelectedInput("stop or hold"); pair.Relationship.ForwardStopOrHold(); });
            }

            // F4 (WORLD-02): the scope of one real (non-simulated) trap click. Native TryInteract runs the selected units'
            // InterruptMove loop synchronously inside it, after SelectUnit accepted an interactor.
            private static InteractionComponent trapClickInteraction;
            private static int trapClickFrame = -1;

            internal static void TrapClickScopePrefix(InteractionComponent interaction)
            {
                trapClickInteraction = interaction != null && MountedTrapGroupStopPolicy.OpensTrapClickScope(
                    interaction is DisableTrapInteractionComponent, interaction.HasVisibleTrap(), PointerController.SimulatingClick) ? interaction : null;
                trapClickFrame = UnityEngine.Time.frameCount;
            }

            internal static void TrapClickScopePostfix()
            {
                trapClickInteraction = null;
                trapClickFrame = -1;
            }

            internal static void InterruptMovePostfix(UnitCommands __instance)
            {
                if (trapClickInteraction == null) return;
                var inside = MountedTrapGroupStopPolicy.IsAcceptedTrapClickScope(trapClickFrame >= 0, trapClickFrame, UnityEngine.Time.frameCount, true);
                var owner = PatchBridge.Owner(__instance);
                var rider = owner?.Rider;
                if (rider != null && ReferenceEquals(rider.Commands, __instance))
                    owner.ForwardTrapGroupStop(rider, inside, PointerController.SimulatingClick);
            }

            // F4 (WORLD-01): native GroupManager.Go re-initialises the party (every member removed, then re-added).
            internal static void PartyReinitPrefix() => NativePartyReinitScope.Enter();

            internal static void PartyReinitPostfix() => NativePartyReinitScope.Exit();

            // Stop additionally holds back the selected pair's automatic proposals (native Stop arms nothing itself).
            internal static void StopPrefix()
            {
                StopOrHoldPrefix();
                if (PointerController.SimulatingClick) { return; }
                if (PatchBridge.World == null) PatchBridge.Combat?.ObserveStopInput();
                else PatchBridge.World.Visit(pair => pair.Combat.ObserveStopInput());
            }

            internal static bool ContinuousMovePrefix(ref UnitEntityData executor)
            {
                if (PointerController.SimulatingClick) { return true; }
                var owner = PatchBridge.Owner(executor);
                if (owner?.IsExactActivePairUnit(executor) == true)
                {
                    try { owner.EndRiderOrdersForContinuousDrive(); }
                    catch (Exception error) { ReportGroupPaceFault("Mounted stick movement: the rider's orders were not ended", error); }
                    owner.CombatOwner.Cancel("continuous movement replaced the active mounted combat intent");
                }
                return owner == null || owner.RouteContinuousMove(ref executor);
            }

            // Known issues sprint 3 continuation (M05): native Init installed its continuous agent on the executor's
            // view. For a pair's mount that agent is bound to this exact command and pair generation.
            internal static void ContinuousMovePostfix(UnitMoveContiniously __instance)
            {
                if (PointerController.SimulatingClick || __instance == null) { return; }
                try { PatchBridge.Owner(__instance.Executor)?.BindContinuousDrive(__instance); }
                catch (Exception error) { ReportGroupPaceFault("Mounted stick movement: the continuous agent was not bound", error); }
            }

            internal static void NativeCombatEndPrefix(out Dictionary<GameMountedRelationshipService, UnitEntityData> __state)
            {
                __state = new Dictionary<GameMountedRelationshipService, UnitEntityData>();
                var owners = PatchBridge.Pairs?.Select(pair => pair.Relationship) ?? new[] { PatchBridge.Service };
                foreach (var owner in owners)
                    if (owner != null) __state.Add(owner, owner.CaptureNativeCombatEndMount());
            }

            internal static void NativeCombatEndPostfix(CombatController __instance, Dictionary<GameMountedRelationshipService, UnitEntityData> __state)
            {
                var errors = new List<Exception>();
                foreach (var pair in __state)
                    try { pair.Key.CompleteNativeCombatEndMountLease(__instance, pair.Value); }
                    catch (Exception error) { errors.Add(error); }
                if (errors.Count != 0) throw new AggregateException("Native combat-end lease restoration failed.", errors);
            }

            internal static bool ForcePlaceAboveGroundPrefix(UnitEntityView __instance)
            {
                return PatchBridge.Owner(__instance?.EntityData)?.TrySuppressRiderGroundPlacement(__instance) != true;
            }

            internal static bool UnitClickPrefix(
                UnityEngine.GameObject gameObject,
                int button,
                bool simulate,
                ref bool __result)
            {
                var selected = PatchBridge.World?.Selected;
                var input = PatchBridge.World == null ? PatchBridge.PlayerAction : selected?.PlayerActions;
                var combat = PatchBridge.World == null ? PatchBridge.Combat : selected?.Combat;
                var result = input?.TryHandleMountTargetClick(gameObject, button, simulate) ??
                    MountedCombatClickResult.NotHandled;
                if (result == MountedCombatClickResult.NotHandled)
                {
                    result = combat?.TryHandleUnitClick(gameObject, button, simulate) ??
                        MountedCombatClickResult.NotHandled;
                }
                if (result == MountedCombatClickResult.NotHandled)
                {
                    return true;
                }
                __result = result == MountedCombatClickResult.HandledAccepted;
                return false;
            }

            private static MethodInfo TerminalHook(string name) =>
                typeof(NativeCommandTerminalBranches).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic);

            internal static IEnumerable<CodeInstruction> NativeTurnBasedTerminalTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator) =>
                NativeCommandTerminalBranches.TurnBased(instructions, generator, TerminalHook(nameof(NativeCommandTerminalBranches.CaptureCanAct)),
                    TerminalHook(nameof(NativeCommandTerminalBranches.TurnBasedForceFinish)));

            internal static IEnumerable<CodeInstruction> NativeTickTerminalTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator) =>
                NativeCommandTerminalBranches.Tick(instructions, generator, TerminalHook(nameof(NativeCommandTerminalBranches.ObserveShouldBeInterrupted)),
                    TerminalHook(nameof(NativeCommandTerminalBranches.InterruptAtStateSite)),
                    TerminalHook(nameof(NativeCommandTerminalBranches.InterruptAsSoonAsPossible)),
                    TerminalHook(nameof(NativeCommandTerminalBranches.CaptureCanAct)),
                    TerminalHook(nameof(NativeCommandTerminalBranches.InterruptAtRunningSite)));

            internal static IEnumerable<CodeInstruction> NativeAttackBanTranspiler(IEnumerable<CodeInstruction> instructions) =>
                NativeCommandTerminalBranches.AttackBan(instructions, TerminalHook(nameof(NativeCommandTerminalBranches.AttackBanTerm)));

            internal static IEnumerable<CodeInstruction> NativeRunScopeTranspiler(IEnumerable<CodeInstruction> instructions) =>
                MountedNativeRunScope.Transpile(instructions, typeof(MountedNativeRunScope).GetMethod(nameof(MountedNativeRunScope.ScopedRun),
                    BindingFlags.Static | BindingFlags.NonPublic));

            internal static IEnumerable<CodeInstruction> CannotAttackApproachTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator) =>
                MountedClickApproachTranspiler.CannotAttackApproachRun(instructions, generator, Hook(nameof(ClickApproachRouted)));

            // Known issues M04 closeout, review of b1a88b4e (R1, R2); Tabletop Rules Expansion startup repair. Guards the one
            // submission of the click command's local in native ClickUnitHandler.OnClick (MountedClickApproachTranspiler).
            // The cannot-attack approach a mounted rider was given is run, as the original command object, by the mount
            // through the ground-order lifecycle, and true is returned so the method's own submission is skipped. False
            // leaves that submission to run unchanged on the same container and command: native Run, or the Tabletop Rules
            // Expansion's firearm-order Submit with its input invocation when that mod has rewritten it. A fault leaves the
            // submission, unless it came after the command was already given to another unit (initialised once).
            // Routed commands are only an exact UnitMoveTo outside simulation; for one of those that mod's Submit carries no
            // firearm order and runs native Run on the container it is given, so the router's own native Run on the rider or
            // mount is the same submission that mod would have made there.
            internal static bool ClickApproachRouted(UnitCommands commands, UnitCommand cmd)
            {
                bool handled;
                try { handled = RouteRiderClickApproach(commands, cmd); }
                catch (Exception error) { ReportMarkerFault("Mounted click approach: the rider's order was left native", error); handled = false; }
                return handled || !(cmd == null || cmd.Executor == null || ReferenceEquals(cmd.Executor.Commands, commands));
            }

            // True: handled here (routed, or refused by the ground-order admission); the rider's Run of it never happens.
            private static bool RouteRiderClickApproach(UnitCommands commands, UnitCommand cmd)
            {
                var owner = PatchBridge.Owner(commands);
                var rider = owner?.Rider;
                var game = Kingmaker.Game.Instance;
                var sceneMayOwnRider = rider != null && (NativeSceneControl.IsUnderLiveSceneControl(rider) || game != null &&
                    MountedSceneContinuityPolicy.SceneActive(game.CutsceneLock, game.IsModeActive(Kingmaker.GameModes.GameModeType.Dialog),
                        game.IsModeActive(Kingmaker.GameModes.GameModeType.Cutscene)));
                if (!MountedDestinationMarkerPolicy.RoutesRiderClickApproach(owner != null && owner.State == RelationshipState.Mounted,
                        rider != null && ReferenceEquals(commands, rider.Commands), cmd != null && cmd.GetType() == typeof(UnitMoveTo),
                        cmd?.AiAction != null, true, PointerController.SimulatingClick, sceneMayOwnRider)) return false;
                var generation = owner.MountedPairGeneration;
                var mount = owner.Mount;
                // The hostile-click observation binds to the command the clicked unit's own Run carries (native order:
                // the click's request, then that Run). The rider's Run is not reached, so it is bound here, to this command.
                PatchBridge.CombatFor(commands)?.BindObservedStockRequest(commands, cmd);
                var unit = rider;
                var admitted = GroundCommandPrefix(ref unit);
                try
                {
                    if (!admitted) { }
                    else if (ReferenceEquals(unit, rider)) commands.Run(cmd);
                    else if (unit != null && ReferenceEquals(unit, mount) && owner.State == RelationshipState.Mounted &&
                        owner.MountedPairGeneration == generation)
                    {
                        // The original command: the open ground order recognises it by identity (native leaves its
                        // CreatedByPlayer false), so the watched move and the turn-based admission are this command.
                        PatchBridge.CombatFor(unit)?.BindGroundOrderClickApproach(cmd);
                        unit.Commands.Run(cmd);
                    }
                }
                finally { GroundCommandPostfix(unit); }
                RiderClickApproachRoutedCount++;
                try
                {
                    PatchBridge.Logger?.Info("Mounted cannot-attack click approach given to the pair: rider=" + rider.UniqueId + "; executor=" +
                        (cmd.Executor?.UniqueId ?? "none") + "; admitted=" + admitted + "; live=" + (cmd.Executor != null && !cmd.IsFinished) + ".");
                }
                catch (Exception) { }
                return true;
            }

            internal static long RiderClickApproachRoutedCount;

            // A fault in the closeout's marker hooks leaves native behaviour; reported once.
            private static readonly MountedBoundedFaultReport markerFault = new MountedBoundedFaultReport();
            private static void ReportMarkerFault(string message, Exception error) =>
                markerFault.Report((text, fault) => PatchBridge.Logger?.Exception(text, fault), message, error);

            internal static bool MountMovementPrefix(
                UnitMovementAgent __instance,
                ref float deltaTime,
                ref bool __result)
            {
                bool result;
                if (PatchBridge.UnifiedTurn != null &&
                    PatchBridge.UnifiedTurn.TryMoveNativePreparationActor(__instance, ref deltaTime, out result))
                { __result = result; return false; }
                var combat = PatchBridge.CombatFor(__instance?.Unit?.EntityData);
                if (combat != null &&
                    combat.TryOverrideMountTurnMovement(__instance, ref deltaTime, out result))
                {
                    __result = result;
                    return false;
                }
                return true;
            }

            internal static bool CompleteMovementPrefix(UnitMovementAgent __instance)
            {
                return PatchBridge.CombatFor(__instance?.Unit?.EntityData)?.TryCompleteNativeMountTurnMoveAtReachedPathEnd(__instance) != true;
            }

            internal static void IsUnitEnoughClosePostfix(UnitCommand __instance, ref bool __result)
            {
                if (!__result && PatchBridge.CombatFor(__instance?.Executor)?.ShouldTreatNativeMountTurnMoveAsEnoughClose(__instance) == true)
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
                if (!suppressStep && PatchBridge.CombatFor(__instance?.Unit)?.ShouldSuppressStockOpportunityAttack(__instance?.Unit, target) != true)
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

            internal static void PresentationRigLateUpdatePostfix(Character __instance) =>
                PatchBridge.Persistence?.ObserveNativePresentationLateUpdate(__instance);

            internal static bool CombatPreparePrefix() => PatchBridge.Persistence?.CombatRestorationPending != true;

            internal static bool RetainedNativeJoinPrefix(UnitEntityData __instance) =>
                PatchBridge.Persistence?.AllowNativeJoinCombat(__instance) != false;

            internal readonly struct CombatCooldownObservation
            {
                internal readonly RuntimeCombatScenarioEngine Consumer;
                internal readonly UnitEntityData Unit;
                internal readonly float Initiative;

                internal CombatCooldownObservation(RuntimeCombatScenarioEngine consumer, UnitEntityData unit, float initiative)
                {
                    Consumer = consumer;
                    Unit = unit;
                    Initiative = initiative;
                }
            }

            internal static bool CombatCooldownPrefix(UnitEntityData unit, out CombatCooldownObservation __state)
            {
                var consumer = RuntimeAutomationHost.GetCombatCooldownConsumer(unit);
                var initiative = consumer == null || unit?.CombatState == null
                    ? float.NaN
                    : unit.CombatState.Cooldown.Initiative;
                __state = new CombatCooldownObservation(consumer, consumer == null ? null : unit, initiative);
                return PatchBridge.Persistence?.CombatRestorationPending != true;
            }

            internal static void CombatCooldownPostfix(UnitEntityData unit, CombatCooldownObservation __state)
            {
                // A native callback may outlive its consumer. Never assign its prefix
                // sample to a newly created scenario, a different actor, or a disposed row.
                if (!ReferenceEquals(unit, __state.Unit) ||
                    !RuntimeAutomationHost.IsCurrentCombatCooldownConsumer(__state.Consumer, unit)) return;
                var combatState = unit?.CombatState;
                var game = Kingmaker.Game.Instance;
                RuntimeAutomationHost.ObserveCombatCooldownTick(
                    __state.Consumer,
                    unit,
                    __state.Initiative,
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

            internal static void SaveRequestObservationPrefix(SaveInfo saveInfo)
            {
                try { PatchBridge.Persistence?.ObserveSaveRequest(saveInfo); }
                catch (Exception error) { PatchBridge.Logger?.Exception("Mounted save request was not observed", error); }
            }

            internal static void SavePostfix(SaveInfo saveInfo, ref IEnumerator<object> __result, bool __state)
            {
                if (__state && __result != null)
                    __result = PatchBridge.Persistence != null ? PatchBridge.Persistence.WrapSaveRoutine(__result, saveInfo) :
                        PatchBridge.NativeControls == null ? __result : PatchBridge.NativeControls.WrapSaveRoutine(__result);
            }

            internal static bool AreaTransitionPrefix(Kingmaker.Blueprints.Area.BlueprintArea area, SaveInfo saveInfo)
            {
                // Save/load repair: Game.LoadGame (06000CE0) reaches this full LoadArea with the selected SaveInfo. That is a
                // saved-world replacement, admitted by its own load policy and released through load housekeeping; only
                // area travel (saveInfo == null) is subject to the exploration travel guard.
                if (MountedWorldBoundaryAdmissionPolicy.Classify(saveInfo != null) == MountedWorldBoundaryKind.SavedWorldReplacement)
                    return PatchBridge.Persistence?.AdmitSavedWorldReplacement(area, saveInfo) != false;
                if (PatchBridge.Persistence?.AdmitChargeAreaTransition() == false) return false;
                PatchBridge.Persistence?.BeginAreaTransition(area, saveInfo, true);
                return true;
            }
            internal static void LocalTeleportDispatchPrefix(Kingmaker.Blueprints.Area.BlueprintAreaEnterPoint areaEnterPoint)
            {
                // Observation only: the native teleport always runs. A fault here must never stop the party's move.
                try { PatchBridge.Persistence?.ObserveLocalTeleportDispatch(areaEnterPoint); }
                catch (Exception error) { PatchBridge.Logger?.Exception("Mounted local teleport dispatch was not observed", error); }
            }
            internal static void AreaEntitiesReadyPostfix()
            {
                PatchBridge.Persistence?.CompleteFullAreaPlacement();
                // Review finding 2: the exact loaded world adopts its saved retained mounted intent at native arrival.
                PatchBridge.Persistence?.AdoptLoadedRetainedIntent();
            }

            internal static void CampInteractionPrefix(Kingmaker.View.MapObjects.RestInteraction __instance)
            {
                // Native refusals (combat, no camp view) run unchanged afterwards; nothing is suspended for them.
                if (__instance == null || Kingmaker.Game.Instance?.Player?.IsInCombat != false ||
                    __instance.GetComponent<Kingmaker.View.MapObjects.CampPlaceView>() == null) return;
                try { PatchBridge.Persistence?.TrySuspendForEvent(MountedRetainedIntentKind.Camp, "RestInteraction.OnInteract"); }
                catch (Exception error) { PatchBridge.Logger?.Exception("Mounted camp suspension left the native camp interaction unchanged", error); }
            }

            internal static bool FamiliarFollowPrefix(Kingmaker.Visual.Critters.Familiar __instance)
            {
                var mount = MountedFamiliarFollowAdapter.RiddenMount(__instance?.Master, PatchBridge.Owner);
                return mount == null || !MountedFamiliarFollowAdapter.TryUpdateMovingToMaster(__instance, mount, PatchBridge.Logger);
            }

            internal static void FamiliarTeleportPostfix(Kingmaker.Visual.Critters.Familiar __instance)
            {
                var mount = MountedFamiliarFollowAdapter.RiddenMount(__instance?.Master, PatchBridge.Owner);
                if (mount != null) MountedFamiliarFollowAdapter.AfterTeleportToMaster(__instance, mount, PatchBridge.Logger);
            }

            internal static bool GameLoadAdmissionPrefix(SaveInfo saveInfo)
                => GameLoadAdmissionCore(saveInfo, "Game.LoadGame");

            private static bool GameLoadAdmissionCore(SaveInfo saveInfo, string nativeEntry)
            {
                var authorization = PatchBridge.SaveAuthorization;
                if (authorization != null && authorization.IsActive)
                {
                    try
                    {
                        var reason = authorization.ValidateLoadBeforeWorldReplacement(
                            saveInfo == null ? null : NativePersistenceIsolation.Project(saveInfo),
                            Kingmaker.Game.Instance.SaveManager.SavePath, nativeEntry);
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
                var accepted = PatchBridge.Persistence?.CanLoadBeforeWorldReplacement(saveInfo) != false;
                if (authorization?.HasArmedOwnerSeedLoadRefusal == true)
                    accepted = authorization.FenceOwnerSeedLoadRefusalDecision(accepted,
                        saveInfo == null ? null : NativePersistenceIsolation.Project(saveInfo),
                        Kingmaker.Game.Instance.SaveManager.SavePath, nativeEntry);
                // The full LoadArea this entry calls synchronously recognises the exact admitted descriptor.
                if (accepted) PatchBridge.Persistence?.BindAdmittedLoad(saveInfo);
                return accepted;
            }

            internal static bool GameMainMenuLoadAdmissionPrefix(SaveInfo saveInfo) =>
                GameLoadAdmissionCore(saveInfo, "Game.LoadGameFromMainMenu") && PatchBridge.Persistence?.PrepareForNativeWorldDisposal() != false;

            internal static bool GameSmokeLoadAdmissionPrefix(SaveInfo save) => GameLoadAdmissionCore(save, "Game.LoadGameForSmokeTest");

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

            internal static void SaveWorkerPrefix(SaveInfo saveInfo) => NativeSaveWorkerBoundary.ObserveWorkerEntry(saveInfo);

            // The isolation's write leases are observed and released through the
            // isolation's own seams on PrepareSave and the worker, so they survive
            // the integration-absent detach of this controller (final103-p07-absent).
            internal static void SavePreparedPostfix(SaveInfo save) => PatchBridge.Persistence?.ObservePreparedSave(save);

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

            internal static bool GuardNativeBoundary(NativeLifecycleBoundary boundary, CleanupTrigger trigger, string source)
            {
                var owners = PatchBridge.Pairs?.Select(pair => pair.Relationship) ?? new[] { PatchBridge.Service };
                var allSucceeded = true;
                foreach (var service in owners)
                {
                    if (service == null) continue;
                    var before = service.State;
                    var succeeded = false;
                    try { succeeded = service.GuardBoundary(trigger); }
                    catch (Exception error) { PatchBridge.Logger?.Exception("Native world boundary retained pair cleanup", error); }
                    finally
                    {
                        PatchBridge.LifecycleLedger?.Record(boundary, source, before, service.State,
                            trigger, true, succeeded && service.State == RelationshipState.Unmounted);
                    }
                    if (!succeeded) allSucceeded = false;
                }
                return allSucceeded;
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
