[CmdletBinding()]
param([ValidateSet('Both','Kingmaker','Wrath')][string]$Target='Both')

$ErrorActionPreference='Stop'; Set-StrictMode -Version Latest
$repoRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..')); $labRoot=[IO.Path]::GetFullPath((Join-Path $repoRoot '..\..'))
$intake=Get-Content -Raw -LiteralPath (Join-Path $labRoot 'environment-intake.json')|ConvertFrom-Json
if($Target -eq 'Both'){
    $totalPass=0
    foreach($childTarget in @('Kingmaker','Wrath')){
        $output=@(& powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File $PSCommandPath -Target $childTarget)
        if($LASTEXITCODE-ne0){$output|ForEach-Object{Write-Host $_};throw "$childTarget assembly-contract child failed."}
        $output|ForEach-Object{Write-Host $_}
        $line=@($output|Where-Object{$_ -match '^TOTAL PASS=(\d+) FAIL=0$'}|Select-Object -Last 1)
        if($line.Count-ne1){throw "$childTarget child did not return exact totals."}
        $totalPass += [int]([regex]::Match([string]$line[0],'^TOTAL PASS=(\d+)').Groups[1].Value)
    }
    Write-Host "ASSEMBLY-BACKED TOTAL PASS=$totalPass FAIL=0"
    return
}
$managed=if($Target-eq'Kingmaker'){Join-Path ([string]$intake.requestedLayout.kingmakerInstallDir) 'Kingmaker_Data\Managed'}else{Join-Path ([string]$intake.requestedLayout.wrathInstallDir) 'Wrath_Data\Managed'}
$assemblyPath=Join-Path $managed 'Assembly-CSharp.dll'; $passes=0; $failures=New-Object 'System.Collections.Generic.List[string]'
[AppDomain]::CurrentDomain.add_ReflectionOnlyAssemblyResolve({param($sender,$eventArgs)$name=($eventArgs.Name-split',')[0]+'.dll';$path=Join-Path $managed $name;if(Test-Path -LiteralPath $path){return [Reflection.Assembly]::ReflectionOnlyLoadFrom($path)};return $null})
$assembly=[Reflection.Assembly]::ReflectionOnlyLoadFrom($assemblyPath)
function Assert-Contract([bool]$Condition,[string]$Message){if($Condition){$script:passes++;Write-Host "PASS $Target $Message"}else{$script:failures.Add($Message);Write-Host "FAIL $Target $Message"}}
function Find-Token([string]$TypeName,[int]$Token){$type=$assembly.GetType($TypeName,$false);if($null-eq$type){return $null};return @($type.GetMembers([Reflection.BindingFlags]'Public,NonPublic,Instance,Static')|Where-Object MetadataToken -eq $Token|Select-Object -First 1)}
if($Target-eq'Kingmaker'){
    Assert-Contract ((Get-FileHash -Algorithm SHA256 -LiteralPath $assemblyPath).Hash.ToLowerInvariant()-ceq'3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb') 'Assembly-CSharp SHA-256'
    Assert-Contract ($assembly.ManifestModule.ModuleVersionId.ToString()-ceq'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7') 'Assembly-CSharp MVID'
    $firstpassPath=Join-Path $managed 'Assembly-CSharp-firstpass.dll'
    $firstpass=[Reflection.Assembly]::ReflectionOnlyLoadFrom($firstpassPath)
    Assert-Contract ((Get-FileHash -Algorithm SHA256 -LiteralPath $firstpassPath).Hash.ToLowerInvariant()-ceq'069a7362ce5e3ccd597206174aec13743c2db5a1bfbc2a42f15a5fbd1ea30d30') 'Assembly-CSharp-firstpass SHA-256'
    Assert-Contract ($firstpass.ManifestModule.ModuleVersionId.ToString()-ceq'57f03756-55de-42f5-8bb3-e983306082b2') 'Assembly-CSharp-firstpass MVID'
    Assert-Contract ($null-ne$firstpass.GetType('RootMotion.FinalIK.FullBodyBipedIK',$false) -and $null-ne$firstpass.GetType('RootMotion.SolverManager',$false)) 'native FullBodyBipedIK and post-animator solver surfaces present'
    $unityCorePath=Join-Path $managed 'UnityEngine.CoreModule.dll'
    $unityCore=[Reflection.Assembly]::ReflectionOnlyLoadFrom($unityCorePath)
    Assert-Contract ((Get-FileHash -Algorithm SHA256 -LiteralPath $unityCorePath).Hash.ToLowerInvariant()-ceq'3a76df7f709d465e3273502e08edbffb536b1c2f78c3a132b8668e59fddd2803') 'UnityEngine.CoreModule SHA-256'
    Assert-Contract ($unityCore.ManifestModule.ModuleVersionId.ToString()-ceq'bd5ffe06-494e-4588-a068-c8443cc48c47') 'UnityEngine.CoreModule MVID'
    $ummPath=Join-Path $managed 'UnityModManager\UnityModManager.dll'
    $ummAssembly=[Reflection.Assembly]::ReflectionOnlyLoadFrom($ummPath)
    Assert-Contract ((Get-FileHash -Algorithm SHA256 -LiteralPath $ummPath).Hash.ToLowerInvariant()-ceq'75b96e25a3a9fbadb47dd14a4ab490cb8c98143a6242aff3bba6145cd3047f39') 'UnityModManager SHA-256'
    Assert-Contract ($ummAssembly.ManifestModule.ModuleVersionId.ToString()-ceq'edf4be29-0c88-4482-876d-e946a0f9e363') 'UnityModManager MVID'
    Assert-Contract ($ummAssembly.GetName().Version.ToString()-ceq'0.28.2.0') 'UnityModManager exact version'
    foreach($name in @('Kingmaker.UnitLogic.Parts.UnitPartRider','Kingmaker.UnitLogic.Parts.UnitPartSaddled','Kingmaker.Controllers.Units.SaddledUnitController')){Assert-Contract ($null-eq$assembly.GetType($name,$false)) "mounted type absent: $name"}
    $gameVersionType=$assembly.GetType('Kingmaker.GameVersion',$false)
    $gameVersionMethod=if($null-eq$gameVersionType){$null}else{$gameVersionType.GetMethod('GetVersion',[Reflection.BindingFlags]'Public,Static')}
    Assert-Contract ($null-ne$gameVersionMethod -and $gameVersionMethod.ReturnType.FullName -ceq 'System.String' -and $gameVersionMethod.GetParameters().Count -eq 0) 'GameVersion.GetVersion exact runtime version seam'
    $checks=@(
        @('Kingmaker.Controllers.Clicks.Handlers.ClickGroundHandler',0x060093DC,'RunCommand'),@('Kingmaker.UI.Selection.SelectionManager',0x060034F0,'SelectUnit'),
        @('Kingmaker.Game',0x06000C86,'get_IsControllerMouse'),@('Kingmaker.Game',0x06000C98,'get_CurrentlyLoadedArea'),
        @('Kingmaker.Game',0x040006C6,'UI'),@('Kingmaker.UI.UIAccess',0x04001E96,'MainMenu'),
        @('Kingmaker.MainMenu',0x06000D91,'LoadGame'),@('Kingmaker.UI.LoadingScreen.LoadingScreen',0x04002652,'Instance'),
        @('Kingmaker.EntitySystem.Persistence.LoadingProcess',0x06007FB6,'get_Instance'),@('Kingmaker.SceneName',0x06000E0C,'get_MainMenu'),
        @('Kingmaker.UI.Selection.SelectionManager',0x060034F5,'MultiSelect'),@('SelectionManagerBase',0x060000B9,'Stop'),
        @('SelectionManagerBase',0x060000BA,'Hold'),@('Kingmaker.UnitLogic.Commands.UnitMoveContiniously',0x060026F0,'Init'),
        @('Kingmaker.EntitySystem.Persistence.SaveManager',0x06008012,'LoadZipSave'),@('Kingmaker.EntitySystem.Persistence.SaveManager',0x06008029,'SaveRoutine'),
        @('Kingmaker.EntitySystem.Persistence.SaveManager',0x0600802C,'LoadRoutine'),@('Kingmaker.EntitySystem.Persistence.SaveManager',0x06008030,'AddCallbackAfterLoad'),
        @('Kingmaker.Game',0x06000CE0,'LoadGame'),@('Kingmaker.Blueprints.BlueprintScriptableObject',0x06009637,'get_AssetGuidThreadSafe'),
        @('Kingmaker.Controllers.Units.UnitMoveController',0x06009183,'Tick'),@('Kingmaker.View.UnitEntityView',0x0600184D,'MoveTo'),
        @('Kingmaker.View.UnitEntityView',0x06001848,'ForcePlaceAboveGround'),
        @('Kingmaker.View.UnitMovementAgent',0x060018C2,'Stop'),@('Kingmaker.EntitySystem.Entities.UnitEntityData',0x06008345,'Translocate'),
        @('Kingmaker.Controllers.Clicks.Handlers.ClickGroundHandler',0x060093DA,'MoveSelectedUnitsToPoint'),
        @('Kingmaker.UnitLogic.Commands.UnitCommands',0x0600269F,'get_Move'),@('Kingmaker.UnitLogic.Commands.UnitMoveTo',0x060026F4,'get_Target'),
        @('Kingmaker.View.UnitMovementAgent',0x060018A8,'FindPath'),@('Kingmaker.UI.Selection.SelectionManager',0x060034E2,'get_Instance'),
        @('Kingmaker.UI.Selection.SelectionManager',0x060034E4,'get_SelectedUnits'),@('Kingmaker.Game',0x06000C9A,'get_IsPaused'),
        @('Kingmaker.Game',0x06000C9B,'set_IsPaused'),@('Kingmaker.Game',0x06000CD6,'ReloadArea'),
        @('Kingmaker.Game',0x06000CE4,'SaveGame'),@('Kingmaker.Game',0x06000CE9,'GetCamera'),
        @('Kingmaker.View.UnitEntityView',0x06001826,'get_Animator'),
        @('Kingmaker.View.UnitEntityView',0x06001828,'get_CharacterAvatar'),
        @('Kingmaker.View.UnitEntityView',0x06001839,'get_IkController'),
        @('Kingmaker.Visual.Animation.IKController',0x06001565,'get_BipedIk'),
        @('Kingmaker.Visual.Animation.IKController',0x06001567,'get_GrounderIk'),
        @('Kingmaker.Visual.CharacterSystem.Character',0x0600140B,'OnAnimatorUpdated'),
        @('Kingmaker.Visual.CharacterSystem.Character',0x0600140C,'LateUpdate'),
        @('Kingmaker.UI.ActionBar.ActionBarManager',0x04002E23,'m_Selected'),
        @('Kingmaker.UI.Group.GroupCharacterPortraitController',0x04002AF6,'m_Unit'),
        @('Kingmaker.UI.Group.GroupCharacterPortraitController',0x04002AEB,'m_SelectionSprite'),
        @('Kingmaker.UI.Group.GroupCharacterPortraitController',0x04002AEA,'Frame'),
        @('Kingmaker.UI.Selection.UIDecalBase',0x06003527,'get_Unit'),
        @('Kingmaker.UI.Selection.CharacterUIDecal',0x04002366,'Select'),
        @('Kingmaker.Controllers.Rest.CameraController+CameraUnitFollower',0x0400908A,'m_IsOn'),
        @('Kingmaker.Controllers.Rest.CameraController+CameraUnitFollower',0x0400908B,'m_Unit'),
        @('Kingmaker.Controllers.Rest.CameraController+CameraUnitFollower',0x0600C2FB,'Follow'),
        @('Kingmaker.View.CameraRig',0x0600173B,'GetPosition'),
        @('Kingmaker.Items.UnitBody',0x06007C07,'get_CurrentHandEquipmentSetIndex'),
        @('Kingmaker.Items.UnitBody',0x06007C08,'set_CurrentHandEquipmentSetIndex'),
        @('Kingmaker.Items.UnitBody',0x06007C09,'get_HandsEquipmentSets'),
        @('Kingmaker.View.UnitMovementAgentBase',0x060018E3,'get_MaxSpeedOverride'),
        @('Kingmaker.View.UnitMovementAgentBase',0x060018E4,'set_MaxSpeedOverride'),
        @('Kingmaker.Controllers.Clicks.Handlers.ClickUnitHandler',0x060093ED,'OnClick'),
        @('Kingmaker.View.UnitMovementAgent',0x060018A9,'CanMoveInTurnBased'),
        @('Kingmaker.Game',0x040006C2,'TurnBasedCombatController'),
        @('TurnBased.Controllers.CombatController',0x06000BC4,'get_Initialized'),
        @('TurnBased.Controllers.CombatController',0x06000BC7,'get_SortedUnits'),
        @('TurnBased.Controllers.CombatController',0x06000BC8,'get_RoundNumber'),
        @('TurnBased.Controllers.CombatController',0x06000BDA,'StartTurn'),
        @('TurnBased.Controllers.CombatController',0x06000BBE,'get_CurrentTurn'),
        @('TurnBased.Controllers.CombatController',0x06000BF6,'IsInTurnBasedCombat'),
        @('TurnBased.Controllers.TurnController',0x04000669,'Unit'),
        @('TurnBased.Controllers.TurnController',0x06000C24,'get_IsActing'),
        @('TurnBased.Controllers.TurnController',0x06000C0E,'get_Status'),
        @('TurnBased.Controllers.TurnController',0x06000C47,'ForceToEnd'),
        @('Kingmaker.UnitLogic.Commands.UnitAttack',0x06002678,'.ctor'),
        @('Kingmaker.UnitLogic.Commands.UnitAttack',0x0600265D,'set_IsSingleAttack'),
        @('Kingmaker.UnitLogic.Commands.UnitAttack',0x06002666,'get_LastAttackRule'),
        @('Kingmaker.UnitLogic.Commands.UnitAttack',0x0600266F,'get_AllAttacks'),
        @('Kingmaker.UnitLogic.Commands.UnitAttack',0x06002670,'get_PlannedAttack'),
        @('Kingmaker.UnitLogic.Commands.UnitAttack',0x06002675,'GetAttackIndex'),
        @('Kingmaker.UnitLogic.Commands.UnitAttack',0x06002680,'OnTick'),
        @('Kingmaker.UnitLogic.Commands.UnitCommands',0x060026B2,'Run'),
        @('Kingmaker.UnitLogic.Commands.Base.UnitCommand',0x060027BA,'IgnoreCooldown'),
        @('Kingmaker.EntitySystem.Entities.UnitEntityData',0x0600838F,'UpdateCooldowns'),
        @('Kingmaker.Controllers.Combat.UnitCombatState',0x060093A1,'AttackOfOpportunity'),
        @('Kingmaker.Controllers.EntityCreationController',0x0600901F,'SpawnUnit'),
        @('Kingmaker.EntitySystem.EntityDataBase',0x06007EA6,'Destroy'),
        @('Kingmaker.EntitySystem.EntityDataBase',0x06007E97,'get_IsVisibleForPlayer'),
        @('Kingmaker.EntitySystem.EntityDataBase',0x06007E98,'get_IsInFogOfWar'),
        @('Kingmaker.EntitySystem.EntityDataBase',0x06007E99,'set_IsInFogOfWar'),
        @('Kingmaker.View.EntityViewBase',0x0600175D,'get_IsVisible'),
        @('Kingmaker.View.EntityViewBase',0x06001770,'SetVisible'),
        @('Kingmaker.EntitySystem.Entities.UnitEntityData',0x06008338,'get_GiveExperienceOnDeath'),
        @('Kingmaker.EntitySystem.Entities.UnitEntityData',0x06008339,'set_GiveExperienceOnDeath'),
        @('Kingmaker.EntitySystem.Entities.UnitEntityData',0x040054BA,'m_AiEnabled'),
        @('Kingmaker.UnitLogic.UnitDescriptor',0x06001EFE,'SwitchFactions'),
        @('Kingmaker.EntitySystem.Entities.UnitEntityData',0x060082F6,'set_GroupId'),
        @('Kingmaker.EntitySystem.Entities.UnitEntityData',0x060082F7,'get_Group'),
        @('Kingmaker.EntitySystem.Entities.UnitEntityData',0x0600831D,'get_Inventory'),
        @('Kingmaker.Controllers.Units.UnitGroupsController',0x04005D8A,'Groups'),
        @('Kingmaker.UnitLogic.Groups.UnitGroup',0x040018E6,'Id'),
        @('Kingmaker.UnitLogic.Groups.UnitGroup',0x040018EB,'IsPlayerParty'),
        @('Kingmaker.UnitLogic.Groups.UnitGroup',0x06002416,'Empty'),
        @('Kingmaker.UnitLogic.Groups.UnitGroup',0x06002422,'Dispose'),
        @('Kingmaker.Items.ItemsCollection',0x06007BC8,'get_HasLoot'),
        @('Kingmaker.Items.UnitBody',0x06007BFD,'get_HandsAreEnabled'),
        @('Kingmaker.Items.UnitBody',0x06007C05,'get_EmptyHandWeapon'),
        @('Kingmaker.Items.UnitBody',0x06007C0C,'get_AdditionalLimbs'),
        @('Kingmaker.Items.UnitBody',0x06007C0D,'get_PrimaryHand'),
        @('Kingmaker.Items.UnitBody',0x06007C0E,'get_SecondaryHand'),
        @('Kingmaker.Items.Slots.WeaponSlot',0x06007C8B,'get_HasWeapon'),
        @('Kingmaker.Items.Slots.WeaponSlot',0x06007C8D,'get_MaybeWeapon'),
        @('Kingmaker.RuleSystem.Rules.RuleCalculateAttacksCount',0x04004AC6,'PrimaryHand'),
        @('Kingmaker.RuleSystem.Rules.RuleCalculateAttacksCount',0x04004AC7,'SecondaryHand'),
        @('Kingmaker.RuleSystem.Rules.RuleCalculateAttacksCount',0x060071E4,'.ctor'),
        @('Kingmaker.RuleSystem.Rules.RuleCalculateAttacksCount',0x060071E5,'OnTrigger'),
        @('Kingmaker.RuleSystem.Rules.RuleCalculateAttacksCount+AttacksCount',0x0600BA93,'get_MainAttacks'),
        @('Kingmaker.UnitLogic.Commands.UnitAttack',0x0600268C,'CreateSingleAttack'),
        @('Kingmaker.UnitLogic.Commands.UnitAttack',0x06002685,'GetApproachRadius'),
        @('Kingmaker.UnitLogic.Commands.UnitAttack',0x06002683,'UpdateTarget'),
        @('Kingmaker.UnitLogic.Commands.AttackHandInfo',0x04001A32,'WeaponRange'),
        @('Kingmaker.UnitLogic.Commands.Base.UnitCommand',0x06002784,'get_IsUnitEnoughClose'),
        @('Kingmaker.Utility.GeometryUtils',0x06001C68,'MechanicsDistance'),
        @('TurnBased.Controllers.TurnController',0x06000C37,'TickMovement'),
        @('Kingmaker.Controllers.Units.UnitActionController',0x0600911F,'ShouldStartCommand'),
        @('Kingmaker.Controllers.Units.UnitActionController',0x0600911E,'TickCommand'),
        @('Kingmaker.Controllers.Units.UnitActionController',0x06009120,'UpdateCooldowns'),
        @('Kingmaker.UnitLogic.Commands.Base.UnitCommand',0x060027A7,'Tick'),
        @('Kingmaker.PubSubSystem.EventBus',0x060074BB,'Subscribe'),
        @('Kingmaker.PubSubSystem.RulebookEventBus',0x06007540,'Subscribe'),
        @('Kingmaker.RuleSystem.Rules.RuleAttackWithWeapon',0x04004A8F,'Weapon'),
        @('Kingmaker.RuleSystem.Rules.RuleAttackWithWeapon',0x06007181,'get_IsAttackOfOpportunity'),
        @('Kingmaker.RuleSystem.Rules.RuleAttackWithWeapon',0x06007185,'get_IsCharge'),
        @('Kingmaker.RuleSystem.Rules.RuleAttackRoll',0x06007131,'get_Result'),
        @('Kingmaker.RuleSystem.Rules.RuleAttackRoll',0x0600716B,'get_IsHit'),
        @('Kingmaker.RuleSystem.Rules.Damage.RuleDealDamage',0x060073D5,'get_AttackRoll'),
        @('Kingmaker.RuleSystem.Rules.Damage.RuleDealDamage',0x060073E5,'get_Damage'),
        @('Kingmaker.RuleSystem.Rules.Damage.RuleDealDamage',0x060073ED,'get_IsFake'),
        @('Kingmaker.RuleSystem.Rules.Damage.RuleDealDamage',0x060073EF,'get_IsDot'),
        @('Kingmaker.RuleSystem.Rules.Damage.RuleDealDamage',0x060073F9,'get_SourceAbility'),
        @('Kingmaker.RuleSystem.Rules.Damage.RuleDealDamage',0x060073FB,'get_SourceArea'),
        @('Kingmaker.EntitySystem.Stats.ModifiableValueArmorClass',0x06007F2D,'SelectMissReason'),
        @('Kingmaker.EntitySystem.Entities.UnitEntityData',0x06008330,'get_AreHandsBusyWithAnimation'),
        @('Kingmaker.EntitySystem.Entities.UnitEntityData',0x0600834E,'CanAttack'),
        @('Kingmaker.Controllers.Units.UnitHandEquipmentController',0x06009154,'IsUpdateScheduledFor'),
        @('Kingmaker.Controllers.Combat.UnitCombatState',0x0600938F,'get_CanActInCombat'),
        @('Kingmaker.Controllers.Combat.UnitCombatState+Cooldowns',0x0600C3B4,'get_Initiative'),
        @('Kingmaker.Game',0x040006BE,'UnitMemoryController'),
        @('Kingmaker.Controllers.Units.UnitMemoryController',0x0600916F,'AddToMemory'),
        @('Kingmaker.EntitySystem.Entities.UnitEntityData',0x060082F8,'get_Memory'),
        @('Kingmaker.EntitySystem.Entities.UnitEntityData',0x060082F9,'get_IsAwake'),
        @('Kingmaker.EntitySystem.Entities.UnitEntityData',0x06008328,'get_IsDirectlyControllable'),
        @('Kingmaker.EntitySystem.Entities.UnitEntityData',0x06008329,'get_IsAIEnabled'),
        @('Kingmaker.EntitySystem.Entities.UnitEntityData',0x0600832A,'set_IsAIEnabled'),
        @('Kingmaker.UnitLogic.Commands.UnitCommands',0x060026A6,'get_Empty'),
        @('Kingmaker.EntitySystem.Entities.UnitEntityData',0x0600836C,'Wake'),
        @('Kingmaker.EntitySystem.Entities.UnitEntityData',0x040054C8,'Sleepless'),
        @('Kingmaker.Controllers.SleepingUnitsController',0x060090B1,'Tick'),
        @('Kingmaker.Controllers.SleepingUnitsController',0x060090B2,'ShouldBeSleeping'),
        @('Kingmaker.Controllers.Combat.UnitCombatJoinController',0x06009360,'Tick'),
        @('Kingmaker.Controllers.Combat.UnitCombatJoinController',0x06009361,'TickUnit'),
        @('Kingmaker.Controllers.Combat.UnitCombatJoinController',0x06009362,'ShouldEngageEnemy'),
        @('Kingmaker.UnitLogic.UnitGroupMemory',0x06001F2B,'get_Enemies'),
        @('Kingmaker.UnitLogic.UnitState',0x04001604,'IsIgnoredByCombat'),
        @('Kingmaker.UnitLogic.UnitState',0x06001F95,'get_LifeState'),
        @('Kingmaker.UnitLogic.UnitState',0x06001F9B,'get_IsFinallyDead'),
        @('Kingmaker.UnitLogic.UnitState',0x06001FA1,'get_MarkedForDeath'),
        @('Kingmaker.UnitLogic.UnitState',0x06001FA3,'get_ForceKill'),
        @('Kingmaker.UnitLogic.UnitState',0x06001FA7,'get_IsConscious'),
        @('Kingmaker.UnitLogic.UnitState',0x06001FA9,'get_IsDead'),
        @('Kingmaker.EntitySystem.Entities.UnitEntityData',0x060082E6,'get_Damage'),
        @('Kingmaker.EntitySystem.Entities.UnitEntityData',0x060082E8,'get_DamageNonLethal'),
        @('Kingmaker.EntitySystem.Entities.UnitEntityData',0x060082EC,'get_Stats'),
        @('Kingmaker.PubSubSystem.IUnitLifeStateChanged',0x06007620,'HandleUnitLifeStateChanged'),
        @('Kingmaker.Controllers.Units.UnitLifeController',0x06009162,'TickOnUnit'),
        @('Kingmaker.UnitLogic.UnitGroupMemory',0x06001F2C,'Add'),
        @('Kingmaker.UnitLogic.UnitGroupMemory',0x06001F2D,'Remove'),
        @('Kingmaker.UnitLogic.UnitGroupMemory',0x06001F31,'Contains'),
        @('Kingmaker.Controllers.Combat.UnitCombatPrepareController',0x0600936F,'Tick'),
        @('Kingmaker.Controllers.Combat.UnitCombatCooldownsController',0x0600934A,'TickOnUnit'),
        @('Kingmaker.Controllers.Combat.UnitCombatLeaveController',0x06009368,'TickGroup'),
        @('Kingmaker.UI.SettingsUI.SettingsEntityBool',0x04002275,'m_Cached'),
        @('Kingmaker.UI.SettingsUI.SettingsEntityBase',0x04002269,'OnOptionUpdatedCallback'),
        @('Kingmaker.UI.SettingsUI.SettingsEntityBase',0x06003359,'OnInvokeUpdateCallback'),
        @('Kingmaker.UI.SettingsUI.SettingsEntityBase',0x0600335C,'GetSavedValueString'),
        @('Kingmaker.UI.SettingsUI.SettingsRoot+SettingsListScreen',0x04007C9F,'EnableTurnBasedMode'),
        @('Kingmaker.UI.SettingsUI.SettingsRoot+SettingsListScreen',0x04007CBC,'OnlyOneSave'),
        @('Kingmaker.EntitySystem.Persistence.LoadingProcess',0x06007FBC,'get_IsLoadingInProcess'),
        @('Kingmaker.Utility.Screenshot',0x06001D41,'CapturePNG'),@('Kingmaker.View.MapObjects.StandardDoor',0x06001AA0,'get_IsOpen'))
    foreach($check in $checks){$member=@(Find-Token $check[0] $check[1]);$matches=$member.Count -eq 1;if($matches){$matches=[string]$member[0].Name -ceq [string]$check[2]};Assert-Contract $matches "token $($check[1].ToString('X8')) $($check[0]).$($check[2])"}
    $globalRulebookHandler=$assembly.GetType('Kingmaker.PubSubSystem.IGlobalRulebookHandler`1',$false)
    $globalRulebookSubscriber=$assembly.GetType('Kingmaker.PubSubSystem.IGlobalRulebookSubscriber',$false)
    Assert-Contract ($null-ne$globalRulebookHandler -and $globalRulebookHandler.MetadataToken-eq0x02000DC3 -and
        $null-ne$globalRulebookSubscriber -and $globalRulebookSubscriber.MetadataToken-eq0x02000DFA -and
        @($globalRulebookHandler.GetInterfaces()|Where-Object MetadataToken -eq 0x02000DC2).Count-eq1 -and
        @($globalRulebookHandler.GetInterfaces()|Where-Object MetadataToken -eq 0x02000DFA).Count-eq1) `
        'global Rulebook handler exact subscription-marker inheritance'
    $ruleAttackRollIsHit=@(Find-Token 'Kingmaker.RuleSystem.Rules.RuleAttackRoll' 0x0600716B)
    Assert-Contract ($ruleAttackRollIsHit.Count-eq1 -and $ruleAttackRollIsHit[0] -is [Reflection.MethodInfo] -and
        $ruleAttackRollIsHit[0].IsPublic -and -not $ruleAttackRollIsHit[0].IsStatic -and
        $ruleAttackRollIsHit[0].ReturnType.FullName-ceq'System.Boolean' -and $ruleAttackRollIsHit[0].GetParameters().Count-eq0) `
        'RuleAttackRoll.IsHit exact public instance Boolean signature'
    $selectMissReason=@(Find-Token 'Kingmaker.EntitySystem.Stats.ModifiableValueArmorClass' 0x06007F2D)
    Assert-Contract ($selectMissReason.Count-eq1 -and $selectMissReason[0] -is [Reflection.MethodInfo] -and
        $selectMissReason[0].IsPublic -and -not $selectMissReason[0].IsStatic -and
        $selectMissReason[0].ReturnType.FullName-ceq'Kingmaker.RuleSystem.Rules.AttackResult' -and
        $selectMissReason[0].GetParameters().Count-eq2 -and
        @($selectMissReason[0].GetParameters()|Where-Object{$_.ParameterType.FullName-ceq'System.Boolean'}).Count-eq2) `
        'ModifiableValueArmorClass.SelectMissReason exact public instance signature'
    $sleepless=@(Find-Token 'Kingmaker.EntitySystem.Entities.UnitEntityData' 0x040054C8)
    $shouldBeSleeping=@(Find-Token 'Kingmaker.Controllers.SleepingUnitsController' 0x060090B2)
    Assert-Contract ($sleepless.Count-eq1 -and $sleepless[0] -is [Reflection.FieldInfo] -and
        $sleepless[0].IsPublic -and -not $sleepless[0].IsStatic -and -not $sleepless[0].IsInitOnly -and
        $sleepless[0].FieldType.FullName-ceq'System.Boolean' -and
        $shouldBeSleeping.Count-eq1 -and $shouldBeSleeping[0] -is [Reflection.MethodInfo] -and
        $shouldBeSleeping[0].IsPrivate -and $shouldBeSleeping[0].IsStatic -and
        $shouldBeSleeping[0].ReturnType.FullName-ceq'System.Boolean' -and
        $shouldBeSleeping[0].GetParameters().Count-eq1 -and
        $shouldBeSleeping[0].GetParameters()[0].ParameterType.FullName-ceq'Kingmaker.EntitySystem.Entities.UnitEntityData') `
        'SleepingUnitsController exact per-unit sleepless gate signature'
    $combatJoinTickUnit=@(Find-Token 'Kingmaker.Controllers.Combat.UnitCombatJoinController' 0x06009361)
    $combatShouldEngage=@(Find-Token 'Kingmaker.Controllers.Combat.UnitCombatJoinController' 0x06009362)
    Assert-Contract ($combatJoinTickUnit.Count-eq1 -and $combatJoinTickUnit[0] -is [Reflection.MethodInfo] -and
        $combatJoinTickUnit[0].IsPrivate -and $combatJoinTickUnit[0].IsStatic -and
        $combatJoinTickUnit[0].ReturnType.FullName-ceq'System.Void' -and
        $combatJoinTickUnit[0].GetParameters().Count-eq1 -and
        $combatJoinTickUnit[0].GetParameters()[0].ParameterType.FullName-ceq'Kingmaker.EntitySystem.Entities.UnitEntityData' -and
        $combatShouldEngage.Count-eq1 -and $combatShouldEngage[0] -is [Reflection.MethodInfo] -and
        $combatShouldEngage[0].IsPrivate -and $combatShouldEngage[0].IsStatic -and
        $combatShouldEngage[0].ReturnType.FullName-ceq'System.Boolean' -and
        $combatShouldEngage[0].GetParameters().Count-eq2 -and
        @($combatShouldEngage[0].GetParameters()|Where-Object{$_.ParameterType.FullName-ceq'Kingmaker.EntitySystem.Entities.UnitEntityData'}).Count-eq2) `
        'UnitCombatJoinController exact per-unit and enemy-gate signatures'
    $forcePlaceAboveGround=@(Find-Token 'Kingmaker.View.UnitEntityView' 0x06001848)
    Assert-Contract ($forcePlaceAboveGround.Count-eq1 -and $forcePlaceAboveGround[0] -is [Reflection.MethodInfo] -and
        $forcePlaceAboveGround[0].IsPublic -and -not $forcePlaceAboveGround[0].IsStatic -and
        $forcePlaceAboveGround[0].ReturnType.FullName-ceq'System.Void' -and $forcePlaceAboveGround[0].GetParameters().Count-eq0) `
        'UnitEntityView.ForcePlaceAboveGround exact public instance void signature'
    $createSingleAttack=@(Find-Token 'Kingmaker.UnitLogic.Commands.UnitAttack' 0x0600268C)
    Assert-Contract ($createSingleAttack.Count-eq1 -and $createSingleAttack[0] -is [Reflection.MethodInfo] -and
        $createSingleAttack[0].IsPublic -and -not $createSingleAttack[0].IsStatic -and
        $createSingleAttack[0].GetParameters().Count-eq0 -and $createSingleAttack[0].ReturnType.IsGenericType -and
        $createSingleAttack[0].ReturnType.GetGenericTypeDefinition().FullName-ceq'System.Collections.Generic.List`1' -and
        @($createSingleAttack[0].ReturnType.GetGenericArguments()).Count-eq1 -and
        @($createSingleAttack[0].ReturnType.GetGenericArguments())[0].FullName-ceq'Kingmaker.UnitLogic.Commands.AttackHandInfo') `
        'UnitAttack.CreateSingleAttack exact public instance native-order signature'
    $turnBasedCombatType=$assembly.GetType('TurnBased.Controllers.CombatController',$false)
    $turnControllerType=$assembly.GetType('TurnBased.Controllers.TurnController',$false)
    $gameTurnController=@(Find-Token 'Kingmaker.Game' 0x040006C2)
    $turnInitialized=@(Find-Token 'TurnBased.Controllers.CombatController' 0x06000BC4)
    $turnSortedUnits=@(Find-Token 'TurnBased.Controllers.CombatController' 0x06000BC7)
    $turnRoundNumber=@(Find-Token 'TurnBased.Controllers.CombatController' 0x06000BC8)
    $turnStart=@(Find-Token 'TurnBased.Controllers.CombatController' 0x06000BDA)
    $turnCurrent=@(Find-Token 'TurnBased.Controllers.CombatController' 0x06000BBE)
    $turnMode=@(Find-Token 'TurnBased.Controllers.CombatController' 0x06000BF6)
    $turnUnit=@(Find-Token 'TurnBased.Controllers.TurnController' 0x04000669)
    $turnActing=@(Find-Token 'TurnBased.Controllers.TurnController' 0x06000C24)
    $turnStatus=@(Find-Token 'TurnBased.Controllers.TurnController' 0x06000C0E)
    Assert-Contract ($null-ne$turnBasedCombatType -and $null-ne$turnControllerType -and
        $gameTurnController.Count-eq1 -and $gameTurnController[0] -is [Reflection.FieldInfo] -and
        $gameTurnController[0].IsPublic -and -not $gameTurnController[0].IsStatic -and
        $gameTurnController[0].FieldType.FullName-ceq'TurnBased.Controllers.CombatController' -and
        $turnInitialized.Count-eq1 -and $turnInitialized[0] -is [Reflection.MethodInfo] -and
        $turnInitialized[0].IsPublic -and -not $turnInitialized[0].IsStatic -and
        $turnInitialized[0].ReturnType.FullName-ceq'System.Boolean' -and $turnInitialized[0].GetParameters().Count-eq0 -and
        $turnSortedUnits.Count-eq1 -and $turnSortedUnits[0] -is [Reflection.MethodInfo] -and
        $turnSortedUnits[0].IsPublic -and -not $turnSortedUnits[0].IsStatic -and
        $turnSortedUnits[0].ReturnType.IsGenericType -and
        $turnSortedUnits[0].ReturnType.GetGenericTypeDefinition().FullName-ceq'System.Collections.Generic.IEnumerable`1' -and
        @($turnSortedUnits[0].ReturnType.GetGenericArguments()).Count-eq1 -and
        @($turnSortedUnits[0].ReturnType.GetGenericArguments())[0].FullName-ceq'Kingmaker.EntitySystem.Entities.UnitEntityData' -and
        $turnRoundNumber.Count-eq1 -and $turnRoundNumber[0].ReturnType.FullName-ceq'System.Int32' -and
        $turnRoundNumber[0].GetParameters().Count-eq0 -and
        $turnStart.Count-eq1 -and $turnStart[0].IsPublic -and -not $turnStart[0].IsStatic -and
        $turnStart[0].ReturnType.FullName-ceq'System.Void' -and $turnStart[0].GetParameters().Count-eq1 -and
        $turnStart[0].GetParameters()[0].ParameterType.FullName-ceq'Kingmaker.EntitySystem.Entities.UnitEntityData' -and
        $turnCurrent.Count-eq1 -and $turnCurrent[0].ReturnType.FullName-ceq'TurnBased.Controllers.TurnController' -and
        $turnMode.Count-eq1 -and $turnMode[0].IsPublic -and $turnMode[0].IsStatic -and
        $turnMode[0].ReturnType.FullName-ceq'System.Boolean' -and $turnMode[0].GetParameters().Count-eq0 -and
        $turnUnit.Count-eq1 -and $turnUnit[0] -is [Reflection.FieldInfo] -and $turnUnit[0].IsPublic -and
        $turnUnit[0].FieldType.FullName-ceq'Kingmaker.EntitySystem.Entities.UnitEntityData' -and
        $turnActing.Count-eq1 -and $turnActing[0].IsPublic -and -not $turnActing[0].IsStatic -and
        $turnActing[0].ReturnType.FullName-ceq'System.Boolean' -and $turnActing[0].GetParameters().Count-eq0 -and
        $turnStatus.Count-eq1 -and $turnStatus[0].IsPublic -and -not $turnStatus[0].IsStatic -and
        $turnStatus[0].ReturnType.FullName-ceq'TurnBased.Controllers.TurnController+TurnStatus' -and
        $turnStatus[0].GetParameters().Count-eq0 -and
        [int]$turnStatus[0].ReturnType.GetField('Preparing').GetRawConstantValue()-eq2 -and
        [int]$turnStatus[0].ReturnType.GetField('Acting').GetRawConstantValue()-eq3) `
        'native turn-based controller roster, rider-turn, and mode signatures'
    $unityChecks=@(
        @('UnityEngine.SceneManagement.SceneManager',0x060019D5,'GetSceneByName'),
        @('UnityEngine.SceneManagement.Scene',0x060019C4,'get_isLoaded'))
    foreach($check in $unityChecks){$type=$unityCore.GetType($check[0],$false);$member=if($null-eq$type){$null}else{@($type.GetMembers([Reflection.BindingFlags]'Public,NonPublic,Instance,Static')|Where-Object MetadataToken -eq $check[1]|Select-Object -First 1)};$matches=$null-ne$member -and @($member).Count-eq1;if($matches){$matches=[string]@($member)[0].Name -ceq [string]$check[2]};Assert-Contract $matches "Unity token $($check[1].ToString('X8')) $($check[0]).$($check[2])"}
    $ummUi=$ummAssembly.GetType('UnityModManagerNet.UnityModManager+UI',$false)
    Assert-Contract ($null-ne$ummUi -and $ummUi.BaseType.FullName-ceq'UnityEngine.MonoBehaviour') 'UMM UI exact MonoBehaviour capture host'
    $ummUiChecks=@(
        @(0x060000CA,'get_Instance'),
        @(0x060000CB,'get_Opened'),
        @(0x060000E3,'ToggleWindow'))
    foreach($check in $ummUiChecks){$member=if($null-eq$ummUi){$null}else{@($ummUi.GetMembers([Reflection.BindingFlags]'Public,NonPublic,Instance,Static')|Where-Object MetadataToken -eq $check[0]|Select-Object -First 1)};$matches=$null-ne$member -and @($member).Count-eq1;if($matches){$matches=[string]@($member)[0].Name-ceq[string]$check[1]};if($matches -and [int]$check[0]-eq 0x060000CA){$matches=@($member)[0].IsPublic -and @($member)[0].IsStatic -and @($member)[0].ReturnType.FullName-ceq'UnityModManagerNet.UnityModManager+UI'};if($matches -and [int]$check[0]-eq 0x060000CB){$matches=@($member)[0].IsPublic -and -not @($member)[0].IsStatic -and @($member)[0].ReturnType.FullName-ceq'System.Boolean'};if($matches -and [int]$check[0]-eq 0x060000E3){$parameters=@(@($member)[0].GetParameters());$matches=@($member)[0].IsPublic -and -not @($member)[0].IsStatic -and @($member)[0].ReturnType.FullName-ceq'System.Void' -and $parameters.Count-eq1 -and $parameters[0].ParameterType.FullName-ceq'System.Boolean'};Assert-Contract $matches "UMM UI token $($check[0].ToString('X8')) $($check[1]) exact screenshot surface"}
    $monoBehaviour=$unityCore.GetType('UnityEngine.MonoBehaviour',$false)
    $waitForEndOfFrame=$unityCore.GetType('UnityEngine.WaitForEndOfFrame',$false)
    $defaultExecutionOrder=$unityCore.GetType('UnityEngine.DefaultExecutionOrder',$false)
    $startCoroutine=if($null-eq$monoBehaviour){$null}else{@($monoBehaviour.GetMethods([Reflection.BindingFlags]'Public,Instance')|Where-Object MetadataToken -eq 0x06000E39|Select-Object -First 1)}
    $stopCoroutine=if($null-eq$monoBehaviour){$null}else{@($monoBehaviour.GetMethods([Reflection.BindingFlags]'Public,Instance')|Where-Object MetadataToken -eq 0x06000E3C|Select-Object -First 1)}
    $waitConstructor=if($null-eq$waitForEndOfFrame){$null}else{@($waitForEndOfFrame.GetConstructors([Reflection.BindingFlags]'Public,Instance')|Where-Object MetadataToken -eq 0x0600156D|Select-Object -First 1)}
    $executionOrderConstructor=if($null-eq$defaultExecutionOrder){$null}else{@($defaultExecutionOrder.GetConstructors([Reflection.BindingFlags]'Public,Instance')|Where-Object MetadataToken -eq 0x060001F4|Select-Object -First 1)}
    Assert-Contract ($null-ne$startCoroutine -and @($startCoroutine).Count-eq1 -and @($startCoroutine)[0].GetParameters().Count-eq1 -and @($startCoroutine)[0].GetParameters()[0].ParameterType.FullName-ceq'System.Collections.IEnumerator' -and @($startCoroutine)[0].ReturnType.FullName-ceq'UnityEngine.Coroutine') 'Unity token 06000E39 MonoBehaviour.StartCoroutine(IEnumerator)'
    Assert-Contract ($null-ne$stopCoroutine -and @($stopCoroutine).Count-eq1 -and @($stopCoroutine)[0].GetParameters().Count-eq1 -and @($stopCoroutine)[0].GetParameters()[0].ParameterType.FullName-ceq'UnityEngine.Coroutine' -and @($stopCoroutine)[0].ReturnType.FullName-ceq'System.Void') 'Unity token 06000E3C MonoBehaviour.StopCoroutine(Coroutine)'
    Assert-Contract ($null-ne$waitConstructor -and @($waitConstructor).Count-eq1 -and @($waitConstructor)[0].GetParameters().Count-eq0) 'Unity token 0600156D WaitForEndOfFrame constructor'
    Assert-Contract ($null-ne$executionOrderConstructor -and @($executionOrderConstructor).Count-eq1 -and @($executionOrderConstructor)[0].GetParameters().Count-eq1 -and @($executionOrderConstructor)[0].GetParameters()[0].ParameterType.FullName-ceq'System.Int32') 'Unity token 060001F4 DefaultExecutionOrder(Int32) constructor'
}else{
    Assert-Contract ((Get-FileHash -Algorithm SHA256 -LiteralPath $assemblyPath).Hash.ToLowerInvariant()-ceq'2cb7160b7154d4ffacc77b9c51b1eb26199e1294300f04fdfc073367b2ef8953') 'Assembly-CSharp SHA-256'
    Assert-Contract ($assembly.ManifestModule.ModuleVersionId.ToString()-ceq'90a9869c-2792-4c7b-bfb7-5a8b33da7c82') 'Assembly-CSharp MVID'
    foreach($name in @('Kingmaker.UnitLogic.Parts.UnitPartRider','Kingmaker.UnitLogic.Parts.UnitPartSaddled','Kingmaker.Controllers.Units.SaddledUnitController')){Assert-Contract ($null-ne$assembly.GetType($name,$false)) "mounted type present: $name"}
    $checks=@(
        @('Kingmaker.UnitLogic.Parts.UnitPartRider',0x0600C263,'Mount'),@('Kingmaker.UnitLogic.Parts.UnitPartRider',0x0600C264,'Dismount'),
        @('Kingmaker.Controllers.Units.SaddledUnitController',0x0600AB06,'TickDelegateRiderToMount'),@('Kingmaker.Controllers.Units.UnitMoveController',0x0600ABAA,'TickUnit'),
        @('Kingmaker.UnitLogic.Commands.Base.UnitCommand',0x0600CA7E,'TickApproaching'),@('Kingmaker.UnitLogic.Commands.UnitCommands',0x0600C935,'FixTargetIfTargetOnMount'))
    foreach($check in $checks){$member=@(Find-Token $check[0] $check[1]);$matches=$member.Count -eq 1;if($matches){$matches=[string]$member[0].Name -ceq [string]$check[2]};Assert-Contract $matches "token $($check[1].ToString('X8')) $($check[0]).$($check[2])"}
}
Write-Host "TOTAL PASS=$passes FAIL=$($failures.Count)"
if($failures.Count-ne0){exit 1}
