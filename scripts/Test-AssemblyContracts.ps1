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
function Test-MethodIlContainsToken([Reflection.MethodBase]$Method,[int]$Token){
    if($null-eq$Method){return $false};$body=$Method.GetMethodBody();if($null-eq$body){return $false}
    [byte[]]$il=$body.GetILAsByteArray();[byte[]]$needle=[BitConverter]::GetBytes($Token)
    for($index=0;$index-le$il.Length-$needle.Length;$index++){
        if($il[$index]-eq$needle[0]-and$il[$index+1]-eq$needle[1]-and$il[$index+2]-eq$needle[2]-and$il[$index+3]-eq$needle[3]){return $true}
    }
    return $false
}
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
    $imageConversionPath=Join-Path $managed 'UnityEngine.ImageConversionModule.dll'
    $imageConversion=[Reflection.Assembly]::ReflectionOnlyLoadFrom($imageConversionPath)
    Assert-Contract ((Get-FileHash -Algorithm SHA256 -LiteralPath $imageConversionPath).Hash.ToLowerInvariant()-ceq'1b30743ab1830b9b45e79f88c1acefd7517eafcef4fd1a7a3eb853a07ca5bb17') 'UnityEngine.ImageConversionModule SHA-256'
    Assert-Contract ($imageConversion.ManifestModule.ModuleVersionId.ToString()-ceq'05cb8ac7-57d6-45bb-99c6-b21b00a9ccd7') 'UnityEngine.ImageConversionModule MVID'
    $loadImageMethods=@($imageConversion.GetType('UnityEngine.ImageConversion',$false).GetMethods([Reflection.BindingFlags]'Public,Static')|Where-Object Name -eq 'LoadImage')
    Assert-Contract ($loadImageMethods.Count-ge1 -and @($loadImageMethods|Where-Object {
        $_.ReturnType.FullName-ceq'System.Boolean' -and $_.GetParameters().Count-ge2 -and
        $_.GetParameters()[0].ParameterType.FullName-ceq'UnityEngine.Texture2D' -and
        $_.GetParameters()[1].ParameterType.FullName-ceq'System.Byte[]'
    }).Count-ge1) 'ImageConversion.LoadImage exact embedded-texture seam'
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
        @('Kingmaker.Blueprints.BlueprintScriptableObject',0x06009632,'get_ComponentsArray'),
        @('Kingmaker.Blueprints.BlueprintScriptableObject',0x06009633,'set_ComponentsArray'),
        @('Kingmaker.Blueprints.BlueprintScriptableObject',0x06009636,'set_AssetGuid'),
        @('Kingmaker.Blueprints.ResourcesLibrary',0x060096FE,'get_LibraryObject'),
        @('Kingmaker.Blueprints.LibraryScriptableObject',0x060096CA,'get_BlueprintsByAssetId'),
        @('Kingmaker.Blueprints.LibraryScriptableObject',0x060096D1,'GetAllBlueprints'),
        @('Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection',0x04006AE8,'Features'),
        @('Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection',0x04006AE9,'AllFeatures'),
        @('Kingmaker.Blueprints.Classes.BlueprintFeature',0x04006A1D,'DlcType'),
        @('Kingmaker.Blueprints.BlueprintUnit',0x04005FFD,'m_Portrait'),
        @('Kingmaker.Blueprints.Facts.BlueprintUnitFact',0x04006955,'m_DisplayName'),
        @('Kingmaker.Blueprints.Facts.BlueprintUnitFact',0x04006956,'m_Description'),
        @('Kingmaker.Blueprints.Facts.BlueprintUnitFact',0x04006957,'m_Icon'),
        @('Kingmaker.Localization.LocalizedString',0x04004C56,'m_Key'),
        @('Kingmaker.UnitLogic.FactLogic.AddPet',0x0400197B,'Pet'),
        @('Kingmaker.UnitLogic.FactLogic.AddPet',0x0400197C,'LevelRank'),
        @('Kingmaker.UnitLogic.FactLogic.AddPet',0x0400197D,'UpgradeFeature'),
        @('Kingmaker.UnitLogic.FactLogic.AddPet',0x0400197E,'UpgradeLevel'),
        @('Kingmaker.UnitLogic.FactLogic.AddPet',0x0600250B,'get_SpawnedPet'),
        @('Kingmaker.UnitLogic.FactLogic.AddPet',0x06002510,'TryUpdatePet'),
        @('Kingmaker.UnitLogic.FactLogic.AllowDyingCondition',0x06002562,'OnEntityCreated'),
        @('Kingmaker.UnitLogic.FactLogic.AllowDyingCondition',0x06002563,'OnEntityRemoved'),
        @('Kingmaker.UnitLogic.UnitState',0x04001600,'AllowDyingCondition'),
        @('Kingmaker.Blueprints.Classes.AddClassLevels',0x040069CE,'CharacterClass'),
        @('Kingmaker.Blueprints.Classes.AddClassLevels',0x040069D0,'Levels'),
        @('Kingmaker.Blueprints.Classes.AddClassLevels',0x06009B1B,'LevelUp'),
        @('Kingmaker.Blueprints.Classes.AddClassLevels',0x06009B1C,'LevelUp'),
        @('Kingmaker.Blueprints.Root.ProgressionRoot',0x0400621C,'XPTable'),
        @('Kingmaker.Blueprints.Classes.BlueprintStatProgression',0x06009B59,'GetBonus'),
        @('Kingmaker.UnitLogic.UnitProgressionData',0x06001F61,'get_CharacterLevel'),
        @('Kingmaker.UnitLogic.UnitProgressionData',0x06001F64,'get_Experience'),
        @('Kingmaker.UnitLogic.UnitProgressionData',0x06001F70,'AddSelection'),
        @('Kingmaker.UnitLogic.Feature',0x04001521,'<Source>k__BackingField'),
        @('Kingmaker.UnitLogic.Class.LevelUp.Actions.SelectFeature',0x060028E4,'Apply'),
        @('Kingmaker.ElementsSystem.ElementsContext',0x060083E5,'GetData'),
        @('Kingmaker.UnitLogic.UnitDescriptor',0x06001F17,'SetMaster'),
        @('Kingmaker.UnitLogic.UnitDescriptor',0x06001F16,'RemoveMaster'),
        @('Kingmaker.UnitLogic.UnitDescriptor',0x06001F12,'ResurrectAndFullRestore'),
        @('Kingmaker.UnitLogic.Feature',0x06001E30,'SetRankForce'),
        @('Kingmaker.UnitLogic.FeatureCollection',0x06001E3C,'AddFeature'),
        @('Kingmaker.UnitLogic.FeatureCollection',0x06001E3E,'RemoveFact'),
        @('Kingmaker.ResourceLinks.WeakResourceLink`1',0x06007478,'Load'),
        @('Kingmaker.Controllers.Units.UnitMoveController',0x06009183,'Tick'),@('Kingmaker.View.UnitEntityView',0x0600184D,'MoveTo'),
        @('Kingmaker.View.UnitEntityView',0x06001848,'ForcePlaceAboveGround'),
        @('Kingmaker.View.UnitEntityView',0x06001851,'StopMoving'),
        @('Kingmaker.View.UnitMovementAgent',0x060018B0,'CompleteMovement'),@('Kingmaker.View.UnitMovementAgent',0x060018BE,'get_WantsToMove'),@('Kingmaker.View.UnitMovementAgent',0x060018C0,'get_IsReallyMoving'),
        @('Kingmaker.View.UnitMovementAgent',0x060018C2,'Stop'),@('Kingmaker.View.UnitMovementAgentBase',0x060018E2,'get_Velocity'),
        @('Kingmaker.View.UnitMovementAgentBase',0x060018E6,'get_Speed'),@('Kingmaker.EntitySystem.Entities.UnitEntityData',0x06008345,'Translocate'),
        @('Kingmaker.Controllers.Clicks.Handlers.ClickGroundHandler',0x060093DA,'MoveSelectedUnitsToPoint'),
        @('Kingmaker.Controllers.Clicks.Handlers.ClickMapObjectHandler',0x060093E2,'OnClick'),
        @('Kingmaker.Controllers.Clicks.Handlers.ClickMapObjectHandler',0x060093E4,'Interact'),
        @('Kingmaker.Controllers.Clicks.Handlers.ClickMapObjectHandler',0x060093E5,'TryInteract'),
        @('Kingmaker.UnitLogic.Commands.UnitInteractWithObject',0x060026D6,'.ctor'),
        @('Kingmaker.UnitLogic.Commands.UnitInteractWithObject',0x060026DE,'OnAction'),
        @('Kingmaker.View.MapObjects.StandardDoor',0x060019ED,'Interact'),
        @('Kingmaker.View.MapObjects.StandardDoor',0x060019F0,'CanInteract'),
        @('Kingmaker.View.MapObjects.StandardDoor',0x06001AA5,'OnInteract'),
        @('Kingmaker.View.MapObjects.StandardDoor',0x06001AA7,'GetState'),
        @('Kingmaker.UnitLogic.Commands.UnitCommands',0x0600269F,'get_Move'),@('Kingmaker.UnitLogic.Commands.UnitCommands',0x060026A9,'GetCommand'),
        @('Kingmaker.UnitLogic.Commands.UnitUseAbility',0x06002728,'Init'),
        @('Kingmaker.UnitLogic.Commands.Base.UnitCommand',0x0600275E,'get_IsFinished'),@('Kingmaker.UnitLogic.Commands.Base.UnitCommand',0x060027AC,'Interrupt'),@('Kingmaker.UnitLogic.Commands.UnitMoveTo',0x060026F4,'get_Target'),
        @('Kingmaker.UnitLogic.Commands.Base.UnitCommand',0x0600276D,'set_NeedLoS'),
        @('Kingmaker.View.UnitMovementAgent',0x060018A3,'PathTo'),@('Kingmaker.View.UnitMovementAgent',0x060018A8,'FindPath'),@('Kingmaker.UI.Selection.SelectionManager',0x060034E2,'get_Instance'),
        @('Kingmaker.UI.Selection.SelectionManager',0x060034E4,'get_SelectedUnits'),@('Kingmaker.Game',0x06000C9A,'get_IsPaused'),
        @('Kingmaker.Game',0x06000C9B,'set_IsPaused'),@('Kingmaker.Game',0x06000CD6,'ReloadArea'),
        @('Kingmaker.Game',0x06000CE4,'SaveGame'),@('Kingmaker.Game',0x06000CE9,'GetCamera'),
        @('Kingmaker.View.UnitEntityView',0x06001826,'get_Animator'),
        @('Kingmaker.View.UnitEntityView',0x06001828,'get_CharacterAvatar'),
        @('Kingmaker.View.UnitEntityView',0x06001839,'get_IkController'),
        @('Kingmaker.Visual.Animation.IKController',0x06001565,'get_BipedIk'),
        @('Kingmaker.Visual.Animation.IKController',0x06001567,'get_GrounderIk'),
        @('Kingmaker.Visual.Animation.IKController',0x0600156C,'SetupIkSystem'),
        @('Kingmaker.Visual.Animation.IKController',0x0600156D,'SetupFbbik'),
        @('Kingmaker.Visual.Animation.Kingmaker.UnitAnimationManager',0x06001605,'Tick'),
        @('Kingmaker.UnitLogic.Commands.AttackHandInfo',0x0600265A,'CreateAnimationHandleForAttack'),
        @('Kingmaker.UI.ServiceWindow.DollRoom',0x06004683,'get_Unit'),
        @('Kingmaker.UI.ServiceWindow.DollRoom',0x06004684,'get_IsVisible'),
        @('Kingmaker.UI.ServiceWindow.DollRoom',0x06004688,'Show'),
        @('Kingmaker.UI.ServiceWindow.DollRoom',0x06004690,'SetupInfo'),
        @('Kingmaker.UI.ServiceWindow.DollRoom',0x04002F58,'m_SimpleAvatar'),
        @('Kingmaker.Controllers.Clicks.Handlers.ClickWithSelectedAbilityHandler',0x060093F4,'GetPriority'),
        @('Kingmaker.Controllers.Clicks.Handlers.ClickWithSelectedAbilityHandler',0x060093F5,'GetTarget'),
        @('Kingmaker.Controllers.Clicks.Handlers.ClickWithSelectedAbilityHandler',0x060093F6,'OnClick'),
        @('Kingmaker.Controllers.Clicks.Handlers.ClickWithSelectedAbilityHandler',0x060093F8,'SetAbility'),
        @('Kingmaker.Controllers.Clicks.Handlers.ClickWithSelectedAbilityHandler',0x060093F9,'DropAbility'),
        @('Kingmaker.Visual.CharacterSystem.Character',0x0600140B,'OnAnimatorUpdated'),
        @('Kingmaker.Visual.CharacterSystem.Character',0x0600140C,'LateUpdate'),
        @('Kingmaker.UI.ActionBar.ActionBarManager',0x04002E23,'m_Selected'),
        @('Kingmaker.UI.ActionBar.ActionBarManager',0x04002E2F,'Active'),
        @('Kingmaker.UI.ActionBar.ActionBarManager',0x04002E30,'CanUseAbilities'),
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
        @('Kingmaker.Game',0x06000C9E,'get_CurrentMode'),
        @('TurnBased.Controllers.CombatController',0x06000BC4,'get_Initialized'),
        @('TurnBased.Controllers.CombatController',0x06000BC7,'get_SortedUnits'),
        @('TurnBased.Controllers.CombatController',0x06000BC8,'get_RoundNumber'),
        @('TurnBased.Controllers.CombatController',0x06000BDA,'StartTurn'),
        @('TurnBased.Controllers.CombatController',0x06000BBE,'get_CurrentTurn'),
        @('TurnBased.Controllers.CombatController',0x06000BFF,'get_WaitingForUI'),
        @('TurnBased.Controllers.CombatController',0x04000652,'m_NextUnit'),
        @('TurnBased.Controllers.CombatController',0x06000BF6,'IsInTurnBasedCombat'),
        @('TurnBased.Controllers.CombatController',0x06000BEA,'Disable'),
        @('TurnBased.Controllers.CombatController',0x06000BE3,'HandleCombatEnd'),
        @('TurnBased.Controllers.TurnController',0x04000669,'Unit'),
        @('TurnBased.Controllers.TurnController',0x06000C24,'get_IsActing'),
        @('TurnBased.Controllers.TurnController',0x06000C25,'get_IsEnding'),
        @('TurnBased.Controllers.TurnController',0x06000C0E,'get_Status'),
        @('TurnBased.Controllers.TurnController',0x06000C47,'ForceToEnd'),
        @('TurnBased.Controllers.TurnController',0x06000C3A,'Start'),
        @('Kingmaker.Controllers.Rest.CameraController+CameraUnitFollower',0x0600C2FD,'Release'),
        @('Kingmaker.UnitLogic.Commands.UnitAttack',0x06002678,'.ctor'),
        @('Kingmaker.UnitLogic.Commands.UnitAttack',0x0600265D,'set_IsSingleAttack'),
        @('Kingmaker.UnitLogic.Commands.UnitAttack',0x06002666,'get_LastAttackRule'),
        @('Kingmaker.UnitLogic.Commands.UnitAttack',0x0600266F,'get_AllAttacks'),
        @('Kingmaker.UnitLogic.Commands.UnitAttack',0x06002670,'get_PlannedAttack'),
        @('Kingmaker.UnitLogic.Commands.UnitAttack',0x06002675,'GetAttackIndex'),
        @('Kingmaker.UnitLogic.Commands.UnitAttack',0x06002680,'OnTick'),
        @('Kingmaker.UnitLogic.Commands.UnitCommands',0x060026B2,'Run'),
        @('TurnBased.Controllers.CombatController',0x06000BE3,'HandleCombatEnd'),
        @('Kingmaker.UnitLogic.Commands.UnitUseAbility',0x06002725,'CreateCastCommand'),
        @('Kingmaker.UnitLogic.Commands.Base.UnitCommand',0x04001A72,'CreatedByPlayer'),
        @('Kingmaker.UnitLogic.Commands.Base.UnitCommand',0x06002775,'get_AiAction'),
        @('Kingmaker.UnitLogic.Buffs.Polymorph',0x06002A08,'TryReplaceView'),
        @('Kingmaker.UnitLogic.Buffs.Polymorph',0x06002A09,'RestoreView'),
        @('Kingmaker.EntitySystem.EntityDataBase',0x06007E9D,'AttachToViewOnLoad'),
        @('Kingmaker.EntitySystem.Entities.UnitEntityData',0x0600835C,'OnViewAttached'),
        @('Kingmaker.UnitLogic.Commands.Base.UnitCommand',0x060027BA,'IgnoreCooldown'),
        @('Kingmaker.EntitySystem.Entities.UnitEntityData',0x0600838F,'UpdateCooldowns'),
        @('Kingmaker.Controllers.Combat.UnitCombatState',0x06009390,'get_CanAttackOfOpportunity'),
        @('Kingmaker.Controllers.Combat.UnitCombatState',0x0600939B,'Disengage'),
        @('Kingmaker.Controllers.Combat.UnitCombatState',0x060093A1,'AttackOfOpportunity'),
        @('Kingmaker.Controllers.Combat.UnitCombatState',0x060093A2,'ShouldAttackOnDisengage'),
        @('Kingmaker.UnitLogic.Commands.UnitAttackOfOpportunity',0x06002696,'.ctor'),
        @('Kingmaker.UnitLogic.Commands.UnitAttackOfOpportunity',0x06002699,'OnAction'),
        @('Kingmaker.UnitLogic.Abilities.Components.AbilityCustomCharge',0x06002BB6,'Deliver'),
        @('Kingmaker.UnitLogic.Abilities.Components.AbilityCustomCharge',0x06002BB7,'TurnBasesRoutine'),
        @('Kingmaker.UnitLogic.Abilities.Components.AbilityCustomCharge',0x06002BB8,'RuntimeRoutine'),
        @('Kingmaker.UnitLogic.Abilities.Components.AbilityCustomCharge',0x06002BB9,'Cleanup'),
        @('Kingmaker.UnitLogic.Abilities.Components.AbilityCustomCharge',0x06002BBD,'CanTarget'),
        @('Kingmaker.UnitLogic.Commands.UnitAttack',0x06002663,'set_IsCharge'),
        @('Kingmaker.RuleSystem.Rules.RuleAttackWithWeapon',0x06007186,'set_IsCharge'),
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
        @('Kingmaker.UnitLogic.Groups.UnitGroup',0x06002412,'get_Count'),
        @('Kingmaker.UnitLogic.Groups.UnitGroup',0x06002413,'get_Item'),
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
        @('Kingmaker.UnitLogic.Commands.Base.UnitCommand',0x06002767,'set_ApproachRadius'),
        @('Kingmaker.Utility.GeometryUtils',0x06001C68,'MechanicsDistance'),
        @('Kingmaker.Controllers.Units.BaseUnitController',0x0600910B,'Tick'),
        @('TurnBased.Controllers.TurnController',0x06000C34,'Tick'),
        @('TurnBased.Controllers.TurnController',0x06000C3C,'Prepare'),
        @('TurnBased.Controllers.TurnController',0x06000C5E,'HandleUnitCommandDidEnd'),
        @('Kingmaker.UnitLogic.Commands.Base.UnitCommand',0x060027A6,'TickApproaching'),
        @('Kingmaker.UnitLogic.Commands.UnitCommands',0x060026BF,'InterruptAndRemoveCommand'),
        @('Kingmaker.UnitLogic.Abilities.AbilityData',0x06002B30,'get_IsSuitableForAutoUse'),
        @('TurnBased.Controllers.TurnController',0x06000C3D,'ContinueActing'),
        @('TurnBased.Controllers.TurnController',0x06000C37,'TickMovement'),
        @('Kingmaker.EntitySystem.Entities.UnitEntityData',0x0600838E,'IsCurrentUnit'),
        @('Kingmaker.Controllers.Units.UnitActionController',0x0600911C,'TickOnUnit'),
        @('Kingmaker.Controllers.Units.UnitActionController',0x0600911D,'TickCommandTurnBased'),
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
        @('Kingmaker.EntitySystem.Stats.ModifiableValue',0x06007F08,'AddModifier'),
        @('Kingmaker.EntitySystem.Stats.ModifiableValue+Modifier',0x0600BE68,'Remove'),
        @('Kingmaker.EntitySystem.Stats.ModifiableValueTemporaryHitPoints',0x06007F74,'HandleDamage'),
        @('Kingmaker.EntitySystem.Entities.UnitEntityData',0x06008330,'get_AreHandsBusyWithAnimation'),
        @('Kingmaker.EntitySystem.Entities.UnitEntityData',0x0600834E,'CanAttack'),
        @('Kingmaker.Controllers.Units.UnitHandEquipmentController',0x06009154,'IsUpdateScheduledFor'),
        @('Kingmaker.Controllers.Combat.UnitCombatState',0x0600938F,'get_CanActInCombat'),
        @('Kingmaker.Controllers.Combat.UnitCombatState+Cooldowns',0x0600C3B4,'get_Initiative'),
        @('Kingmaker.Controllers.Combat.UnitCombatState+Cooldowns',0x0600C3B5,'set_Initiative'),
        @('Kingmaker.Game',0x040006BE,'UnitMemoryController'),
        @('Kingmaker.Controllers.Units.UnitMemoryController',0x0600916F,'AddToMemory'),
        @('Kingmaker.EntitySystem.Entities.UnitEntityData',0x060082F8,'get_Memory'),
        @('Kingmaker.EntitySystem.Entities.UnitEntityData',0x060082F9,'get_IsAwake'),
        @('Kingmaker.Utility.CountingGuard',0x06001C13,'get_Value'),
        @('Kingmaker.Utility.CountingGuard',0x06001C15,'get_GuardCount'),
        @('Kingmaker.View.UnitEntityView',0x0600183B,'get_IsGetUp'),
        @('Kingmaker.Visual.CharactersRigidbody.RigidbodyCreatureController',0x060014B0,'get_IsControllingRigidbody'),
        @('Kingmaker.UnitLogic.UnitState',0x06001FBB,'HasCondition'),
        @('Kingmaker.UnitLogic.Abilities.AbilityData',0x06002B49,'get_IsAvailableForCast'),
        @('Kingmaker.EntitySystem.Entities.UnitEntityData',0x06008328,'get_IsDirectlyControllable'),
        @('Kingmaker.EntitySystem.Entities.UnitEntityData',0x06008329,'get_IsAIEnabled'),
        @('Kingmaker.EntitySystem.Entities.UnitEntityData',0x0600832A,'set_IsAIEnabled'),
        @('Kingmaker.EntitySystem.Entities.UnitEntityData',0x06008319,'get_IsBrainActive'),
        @('Kingmaker.EntitySystem.Entities.UnitEntityData',0x0600831A,'set_IsBrainActive'),
        @('Kingmaker.Controllers.Brain.AiBrainController',0x0600940A,'TickBrain'),
        @('Kingmaker.UnitLogic.Commands.UnitCommands',0x060026A6,'get_Empty'),
        @('Kingmaker.UnitLogic.Commands.UnitCommands',0x060026BD,'RemoveFinishedAndUpdateQueue'),
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
        @('Kingmaker.EntitySystem.Entities.UnitEntityData',0x060082E7,'set_Damage'),
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
    $weakResourceLoad=@(Find-Token 'Kingmaker.ResourceLinks.WeakResourceLink`1' 0x06007478)
    $weakResourceLoadParameters=@()
    if($weakResourceLoad.Count-eq1){$weakResourceLoadParameters=@($weakResourceLoad[0].GetParameters())}
    Assert-Contract ($weakResourceLoad.Count-eq1 -and $weakResourceLoad[0].IsPublic -and -not $weakResourceLoad[0].IsStatic -and
        $weakResourceLoadParameters.Count-eq1 -and $weakResourceLoadParameters[0].ParameterType.FullName-ceq'System.Boolean' -and
        $weakResourceLoadParameters[0].IsOptional -and $weakResourceLoadParameters[0].DefaultValue-eq$false) `
        'native WeakResourceLink<T>.Load(Boolean ignorePreloadWarning=false) view-observation seam'
    $allBlueprintsMethod=@(Find-Token 'Kingmaker.Blueprints.LibraryScriptableObject' 0x060096D1)
    $selectionFeaturesField=@(Find-Token 'Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection' 0x04006AE8)
    $selectionAllFeaturesField=@(Find-Token 'Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection' 0x04006AE9)
    $dlcTypeField=@(Find-Token 'Kingmaker.Blueprints.Classes.BlueprintFeature' 0x04006A1D)
    Assert-Contract ($allBlueprintsMethod.Count-eq1 -and $allBlueprintsMethod[0].IsPublic -and
        -not $allBlueprintsMethod[0].IsStatic -and $allBlueprintsMethod[0].GetParameters().Count-eq0 -and
        $allBlueprintsMethod[0].ReturnType.FullName-ceq'System.Collections.Generic.List`1[[Kingmaker.Blueprints.BlueprintScriptableObject, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]]' -and
        $selectionFeaturesField.Count-eq1 -and $selectionFeaturesField[0].IsPublic -and
        $selectionFeaturesField[0].FieldType.FullName-ceq'Kingmaker.Blueprints.Classes.BlueprintFeature[]' -and
        $selectionAllFeaturesField.Count-eq1 -and $selectionAllFeaturesField[0].IsPublic -and
        $selectionAllFeaturesField[0].FieldType.FullName-ceq'Kingmaker.Blueprints.Classes.BlueprintFeature[]' -and
        $dlcTypeField.Count-eq1 -and $dlcTypeField[0].IsPublic -and
        $dlcTypeField[0].FieldType.FullName-ceq'Kingmaker.Blueprints.Root.DlcType') `
        'native canonical blueprint list dual Ranger selection arrays and DLC entitlement surface'
    $selectFeatureApply=@(Find-Token 'Kingmaker.UnitLogic.Class.LevelUp.Actions.SelectFeature' 0x060028E4)
    Assert-Contract ($selectFeatureApply.Count-eq1 -and $selectFeatureApply[0].IsPublic -and
        -not $selectFeatureApply[0].IsStatic -and $selectFeatureApply[0].ReturnType.FullName-ceq'System.Void' -and
        $selectFeatureApply[0].GetParameters().Count-eq2 -and
        (Test-MethodIlContainsToken $selectFeatureApply[0] 0x06001F70) -and
        (Test-MethodIlContainsToken $selectFeatureApply[0] 0x06001E20)) `
        'native SelectFeature commit records progression-level selection and assigns feature source'
    $addPetUpdate=@(Find-Token 'Kingmaker.UnitLogic.FactLogic.AddPet' 0x06002510)
    $addPetDeactivate=@(Find-Token 'Kingmaker.UnitLogic.FactLogic.AddPet' 0x0600250D)
    $addPetLevel=@(Find-Token 'Kingmaker.UnitLogic.FactLogic.AddPet' 0x06002512)
    Assert-Contract ($addPetUpdate.Count-eq1 -and $addPetUpdate[0] -is [Reflection.MethodInfo] -and
        $addPetUpdate[0].IsPublic -and -not $addPetUpdate[0].IsStatic -and
        $addPetUpdate[0].ReturnType.FullName-ceq'System.Void' -and $addPetUpdate[0].GetParameters().Count-eq0 -and
        (Test-MethodIlContainsToken $addPetUpdate[0] 0x0600901F) -and
        (Test-MethodIlContainsToken $addPetUpdate[0] 0x06001F17) -and
        $addPetDeactivate.Count-eq1 -and (Test-MethodIlContainsToken $addPetDeactivate[0] 0x06001F16) -and
        $addPetLevel.Count-eq1 -and (Test-MethodIlContainsToken $addPetLevel[0] 0x06009B1B)) `
        'native AddPet owns spawn SetMaster level progression upgrade and RemoveMaster lifecycle'
    $allowDyingCreated=@(Find-Token 'Kingmaker.UnitLogic.FactLogic.AllowDyingCondition' 0x06002562)
    $allowDyingField=@(Find-Token 'Kingmaker.UnitLogic.UnitState' 0x04001600)
    Assert-Contract ($allowDyingCreated.Count-eq1 -and $allowDyingCreated[0] -is [Reflection.MethodInfo] -and
        $allowDyingCreated[0].IsPublic -and -not $allowDyingCreated[0].IsStatic -and
        $allowDyingCreated[0].ReturnType.FullName-ceq'System.Void' -and
        $allowDyingCreated[0].GetParameters().Count-eq1 -and
        $allowDyingCreated[0].GetParameters()[0].ParameterType.FullName-ceq'Kingmaker.EntitySystem.Entities.UnitEntityData' -and
        $allowDyingField.Count-eq1 -and $allowDyingField[0] -is [Reflection.FieldInfo] -and
        $allowDyingField[0].IsPublic -and $allowDyingField[0].IsInitOnly -and
        $allowDyingField[0].FieldType.FullName-ceq'Kingmaker.Utility.CountableFlag' -and
        (Test-MethodIlContainsToken $allowDyingCreated[0] 0x060082DB) -and
        (Test-MethodIlContainsToken $allowDyingCreated[0] 0x04001564) -and
        (Test-MethodIlContainsToken $allowDyingCreated[0] 0x04001600) -and
        (Test-MethodIlContainsToken $allowDyingCreated[0] 0x06001C0C)) `
        'native dying-condition component retains the exact UnitState CountableFlag on entity creation'
    $addClassLevelsPublic=@(Find-Token 'Kingmaker.Blueprints.Classes.AddClassLevels' 0x06009B1B)
    $addClassLevelsPrivate=@(Find-Token 'Kingmaker.Blueprints.Classes.AddClassLevels' 0x06009B1C)
    $defaultBuildData=$assembly.GetType('Kingmaker.Assets.UI.LevelUp.DefaultBuildData',$false)
    $elementsContextGetData=@(Find-Token 'Kingmaker.ElementsSystem.ElementsContext' 0x060083E5)
    Assert-Contract ($addClassLevelsPublic.Count-eq1 -and $addClassLevelsPublic[0].IsPublic -and
        -not $addClassLevelsPublic[0].IsStatic -and $addClassLevelsPublic[0].GetParameters().Count-eq2 -and
        $addClassLevelsPrivate.Count-eq1 -and $addClassLevelsPrivate[0].IsPrivate -and
        $addClassLevelsPrivate[0].GetParameters().Count-eq3 -and
        $null-ne$defaultBuildData -and $defaultBuildData.MetadataToken-eq0x02001761 -and
        $elementsContextGetData.Count-eq1 -and $elementsContextGetData[0].IsPublic -and
        $elementsContextGetData[0].IsStatic -and $elementsContextGetData[0].IsGenericMethodDefinition) `
        'native AddClassLevels public level update and DefaultBuildData context boundary'
    $xpTableField=@(Find-Token 'Kingmaker.Blueprints.Root.ProgressionRoot' 0x0400621C)
    $xpGetBonus=@(Find-Token 'Kingmaker.Blueprints.Classes.BlueprintStatProgression' 0x06009B59)
    $progressionCharacterLevel=@(Find-Token 'Kingmaker.UnitLogic.UnitProgressionData' 0x06001F61)
    $progressionExperience=@(Find-Token 'Kingmaker.UnitLogic.UnitProgressionData' 0x06001F64)
    Assert-Contract ($xpTableField.Count-eq1 -and $xpTableField[0] -is [Reflection.FieldInfo] -and
        $xpTableField[0].IsPublic -and $xpTableField[0].FieldType.FullName-ceq'Kingmaker.Blueprints.Classes.BlueprintStatProgression' -and
        $xpGetBonus.Count-eq1 -and $xpGetBonus[0].IsPublic -and -not $xpGetBonus[0].IsStatic -and
        $xpGetBonus[0].ReturnType.FullName-ceq'System.Int32' -and $xpGetBonus[0].GetParameters().Count-eq1 -and
        $xpGetBonus[0].GetParameters()[0].ParameterType.FullName-ceq'System.Int32' -and
        $progressionCharacterLevel.Count-eq1 -and $progressionCharacterLevel[0].IsPublic -and
        $progressionCharacterLevel[0].ReturnType.FullName-ceq'System.Int32' -and
        $progressionExperience.Count-eq1 -and $progressionExperience[0].IsPublic -and
        $progressionExperience[0].ReturnType.FullName-ceq'System.Int32') `
        'native companion class-level or exact XP progression settlement observation surface'
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
    $nativeAbilityClick=@(Find-Token 'Kingmaker.Controllers.Clicks.Handlers.ClickWithSelectedAbilityHandler' 0x060093F6)
    $createCastCommand=@(Find-Token 'Kingmaker.UnitLogic.Commands.UnitUseAbility' 0x06002725)
    $runCommand=@(Find-Token 'Kingmaker.UnitLogic.Commands.UnitCommands' 0x060026B2)
    $unitCommandConstructor=@(Find-Token 'Kingmaker.UnitLogic.Commands.Base.UnitCommand' 0x06002799)
    Assert-Contract ($nativeAbilityClick.Count-eq1 -and $nativeAbilityClick[0] -is [Reflection.MethodInfo] -and
        (Test-MethodIlContainsToken $nativeAbilityClick[0] 0x06002725) -and
        (Test-MethodIlContainsToken $nativeAbilityClick[0] 0x060026B2) -and
        -not (Test-MethodIlContainsToken $nativeAbilityClick[0] 0x04001A72) -and
        $createCastCommand.Count-eq1 -and $createCastCommand[0] -is [Reflection.MethodInfo] -and
        $createCastCommand[0].IsPublic -and $createCastCommand[0].IsStatic -and
        $createCastCommand[0].ReturnType.FullName-ceq'Kingmaker.UnitLogic.Commands.Base.UnitCommand' -and
        -not (Test-MethodIlContainsToken $createCastCommand[0] 0x04001A72) -and
        $runCommand.Count-eq1 -and $runCommand[0] -is [Reflection.MethodInfo] -and
        -not (Test-MethodIlContainsToken $runCommand[0] 0x04001A72) -and
        $unitCommandConstructor.Count-eq1 -and $unitCommandConstructor[0] -is [Reflection.ConstructorInfo] -and
        -not (Test-MethodIlContainsToken $unitCommandConstructor[0] 0x04001A72)) `
        'native ability click admits a stock command without assigning the CreatedByPlayer field'
    $attackOfOpportunity=@(Find-Token 'Kingmaker.Controllers.Combat.UnitCombatState' 0x060093A1)
    Assert-Contract ($attackOfOpportunity.Count-eq1 -and $attackOfOpportunity[0] -is [Reflection.MethodInfo] -and
        $attackOfOpportunity[0].IsPublic -and -not $attackOfOpportunity[0].IsStatic -and
        $attackOfOpportunity[0].ReturnType.FullName-ceq'System.Boolean' -and
        $attackOfOpportunity[0].GetParameters().Count-eq2 -and
        $attackOfOpportunity[0].GetParameters()[0].ParameterType.FullName-ceq'Kingmaker.EntitySystem.Entities.UnitEntityData' -and
        $attackOfOpportunity[0].GetParameters()[1].ParameterType.FullName-ceq'System.Boolean') `
        'UnitCombatState.AttackOfOpportunity exact public instance Boolean signature'
    $rangedOpportunityQueue=@(Find-Token 'Kingmaker.Controllers.Combat.UnitCombatEngagementController' 0x06009352)
    $provokeOpportunity=@(Find-Token 'Kingmaker.Controllers.Combat.UnitCombatEngagementController' 0x06009353)
    $engagementTick=@(Find-Token 'Kingmaker.Controllers.Combat.UnitCombatEngagementController' 0x0600934C)
    Assert-Contract ($rangedOpportunityQueue.Count-eq1 -and $rangedOpportunityQueue[0] -is [Reflection.MethodInfo] -and
        $rangedOpportunityQueue[0].GetParameters().Count-eq1 -and
        $rangedOpportunityQueue[0].GetParameters()[0].ParameterType.FullName-ceq'Kingmaker.RuleSystem.Rules.RuleAttackRoll' -and
        $provokeOpportunity.Count-eq1 -and $provokeOpportunity[0] -is [Reflection.MethodInfo] -and
        (Test-MethodIlContainsToken $rangedOpportunityQueue[0] 0x06009353) -and
        $engagementTick.Count-eq1 -and $engagementTick[0] -is [Reflection.MethodInfo] -and
        $engagementTick[0].ReturnType.FullName-ceq'System.Void' -and
        $engagementTick[0].GetParameters().Count-eq0) `
        'native ranged attack roll queues opportunity provocation for later engagement tick delivery'
    $combatCooldownTick=@(Find-Token 'Kingmaker.Controllers.Combat.UnitCombatCooldownsController' 0x0600934A)
    $combatCooldownTurnBasedGate=@(Find-Token 'Kingmaker.Controllers.Combat.UnitCombatCooldownsController' 0x06009349)
    Assert-Contract ($combatCooldownTick.Count-eq1 -and $combatCooldownTick[0] -is [Reflection.MethodInfo] -and
        -not $combatCooldownTick[0].IsStatic -and $combatCooldownTick[0].ReturnType.FullName-ceq'System.Void' -and
        $combatCooldownTick[0].GetParameters().Count-eq1 -and
        $combatCooldownTick[0].GetParameters()[0].ParameterType.FullName-ceq'Kingmaker.EntitySystem.Entities.UnitEntityData') `
        'UnitCombatCooldownsController.TickOnUnit exact instance Void(UnitEntityData) signature'
    Assert-Contract ($combatCooldownTurnBasedGate.Count-eq1 -and
        $combatCooldownTurnBasedGate[0] -is [Reflection.MethodInfo] -and
        -not $combatCooldownTurnBasedGate[0].IsStatic -and
        $combatCooldownTurnBasedGate[0].ReturnType.FullName-ceq'System.Boolean' -and
        $combatCooldownTurnBasedGate[0].GetParameters().Count-eq1 -and
        $combatCooldownTurnBasedGate[0].GetParameters()[0].ParameterType.FullName-ceq'Kingmaker.EntitySystem.Entities.UnitEntityData' -and
        (Test-MethodIlContainsToken $combatCooldownTurnBasedGate[0] 0x06000BF6) -and
        (Test-MethodIlContainsToken $combatCooldownTick[0] 0x06009349) -and
        (Test-MethodIlContainsToken $combatCooldownTick[0] 0x0600938E)) `
        'combat cooldown exact turn-based early-return gate precedes native initiative decrement'
    $disengage=@(Find-Token 'Kingmaker.Controllers.Combat.UnitCombatState' 0x0600939B)
    $shouldAttackOnDisengage=@(Find-Token 'Kingmaker.Controllers.Combat.UnitCombatState' 0x060093A2)
    $opportunityAction=@(Find-Token 'Kingmaker.UnitLogic.Commands.UnitAttackOfOpportunity' 0x06002699)
    Assert-Contract ($disengage.Count-eq1 -and $disengage[0] -is [Reflection.MethodInfo] -and
        (Test-MethodIlContainsToken $disengage[0] 0x060093A2) -and
        (Test-MethodIlContainsToken $disengage[0] 0x060093A1) -and
        $shouldAttackOnDisengage.Count-eq1 -and $shouldAttackOnDisengage[0] -is [Reflection.MethodInfo] -and
        $shouldAttackOnDisengage[0].IsPublic -and -not $shouldAttackOnDisengage[0].IsStatic -and
        $shouldAttackOnDisengage[0].ReturnType.FullName-ceq'System.Boolean' -and
        $shouldAttackOnDisengage[0].GetParameters().Count-eq2 -and
        $shouldAttackOnDisengage[0].GetParameters()[0].ParameterType.FullName-ceq'Kingmaker.EntitySystem.Entities.UnitEntityData' -and
        $shouldAttackOnDisengage[0].GetParameters()[1].ParameterType.FullName-ceq'System.Boolean' -and
        (Test-MethodIlContainsToken $attackOfOpportunity[0] 0x06002696) -and
        (Test-MethodIlContainsToken $attackOfOpportunity[0] 0x060026B2) -and
        $opportunityAction.Count-eq1 -and $opportunityAction[0] -is [Reflection.MethodInfo] -and
        (Test-MethodIlContainsToken $opportunityAction[0] 0x0600719C) -and
        (Test-MethodIlContainsToken $opportunityAction[0] 0x06007182)) `
        'stock disengage synchronously owns one per-unit opportunity command and marks its independent weapon rule'
    $chargeType=$assembly.GetType('Kingmaker.UnitLogic.Abilities.Components.AbilityCustomCharge',$false)
    $chargeDeliverState=if($null-eq$chargeType){$null}else{$chargeType.GetNestedType('<Deliver>d__2',[Reflection.BindingFlags]'NonPublic')}
    $chargeTurnState=if($null-eq$chargeType){$null}else{$chargeType.GetNestedType('<TurnBasesRoutine>d__3',[Reflection.BindingFlags]'NonPublic')}
    $chargeRuntimeState=if($null-eq$chargeType){$null}else{$chargeType.GetNestedType('<RuntimeRoutine>d__4',[Reflection.BindingFlags]'NonPublic')}
    $chargeDeliverMove=if($null-eq$chargeDeliverState){$null}else{@($chargeDeliverState.GetMethods([Reflection.BindingFlags]'Public,NonPublic,Instance')|Where-Object MetadataToken -eq 0x0600AB09|Select-Object -First 1)}
    $chargeTurnMove=if($null-eq$chargeTurnState){$null}else{@($chargeTurnState.GetMethods([Reflection.BindingFlags]'Public,NonPublic,Instance')|Where-Object MetadataToken -eq 0x0600AB0F|Select-Object -First 1)}
    $chargeRuntimeMove=if($null-eq$chargeRuntimeState){$null}else{@($chargeRuntimeState.GetMethods([Reflection.BindingFlags]'Public,NonPublic,Instance')|Where-Object MetadataToken -eq 0x0600AB15|Select-Object -First 1)}
    $chargeCleanup=@(Find-Token 'Kingmaker.UnitLogic.Abilities.Components.AbilityCustomCharge' 0x06002BB9)
    $chargeCanTarget=@(Find-Token 'Kingmaker.UnitLogic.Abilities.Components.AbilityCustomCharge' 0x06002BBD)
    Assert-Contract ($null-ne$chargeDeliverMove -and (Test-MethodIlContainsToken $chargeDeliverMove 0x06002678) -and
        (Test-MethodIlContainsToken $chargeDeliverMove 0x0600189E) -and
        (Test-MethodIlContainsToken $chargeDeliverMove 0x06001FA0) -and
        $null-ne$chargeTurnMove -and (Test-MethodIlContainsToken $chargeTurnMove 0x06002663) -and
        (Test-MethodIlContainsToken $chargeTurnMove 0x060026BB) -and
        (Test-MethodIlContainsToken $chargeTurnMove 0x060018E4) -and
        $null-ne$chargeRuntimeMove -and (Test-MethodIlContainsToken $chargeRuntimeMove 0x06002663) -and
        (Test-MethodIlContainsToken $chargeRuntimeMove 0x060026BB) -and
        (Test-MethodIlContainsToken $chargeRuntimeMove 0x060017AD) -and
        (Test-MethodIlContainsToken $chargeRuntimeMove 0x060018E4) -and
        $chargeCleanup.Count-eq1 -and (Test-MethodIlContainsToken $chargeCleanup[0] 0x0600189E) -and
        (Test-MethodIlContainsToken $chargeCleanup[0] 0x060018E4) -and
        (Test-MethodIlContainsToken $chargeCleanup[0] 0x06001FA0) -and
        $chargeCanTarget.Count-eq1 -and (Test-MethodIlContainsToken $chargeCanTarget[0] 0x060017AD)) `
        'stock charge owns caster movement speed state path tracing charge state and a caster-owned queued attack in both modes'
    $selectMissReason=@(Find-Token 'Kingmaker.EntitySystem.Stats.ModifiableValueArmorClass' 0x06007F2D)
    Assert-Contract ($selectMissReason.Count-eq1 -and $selectMissReason[0] -is [Reflection.MethodInfo] -and
        $selectMissReason[0].IsPublic -and -not $selectMissReason[0].IsStatic -and
        $selectMissReason[0].ReturnType.FullName-ceq'Kingmaker.RuleSystem.Rules.AttackResult' -and
        $selectMissReason[0].GetParameters().Count-eq2 -and
        @($selectMissReason[0].GetParameters()|Where-Object{$_.ParameterType.FullName-ceq'System.Boolean'}).Count-eq2) `
        'ModifiableValueArmorClass.SelectMissReason exact public instance signature'
    $addModifier=@(Find-Token 'Kingmaker.EntitySystem.Stats.ModifiableValue' 0x06007F08)
    $removeModifier=@(Find-Token 'Kingmaker.EntitySystem.Stats.ModifiableValue+Modifier' 0x0600BE68)
    $handleTemporaryHitPointDamage=@(Find-Token 'Kingmaker.EntitySystem.Stats.ModifiableValueTemporaryHitPoints' 0x06007F74)
    Assert-Contract ($addModifier.Count-eq1 -and $addModifier[0] -is [Reflection.MethodInfo] -and
        $addModifier[0].IsPublic -and -not $addModifier[0].IsStatic -and
        $addModifier[0].ReturnType.FullName-ceq'Kingmaker.EntitySystem.Stats.ModifiableValue+Modifier' -and
        $addModifier[0].GetParameters().Count-eq4 -and
        $addModifier[0].GetParameters()[0].ParameterType.FullName-ceq'System.Int32' -and
        $addModifier[0].GetParameters()[1].ParameterType.FullName-ceq'Kingmaker.Blueprints.Facts.Fact' -and
        $addModifier[0].GetParameters()[2].ParameterType.FullName-ceq'System.String' -and
        $addModifier[0].GetParameters()[3].ParameterType.FullName-ceq'Kingmaker.Enums.ModifierDescriptor' -and
        $removeModifier.Count-eq1 -and $removeModifier[0] -is [Reflection.MethodInfo] -and
        $removeModifier[0].IsPublic -and -not $removeModifier[0].IsStatic -and
        $removeModifier[0].ReturnType.FullName-ceq'System.Boolean' -and
        $removeModifier[0].GetParameters().Count-eq0 -and
        $handleTemporaryHitPointDamage.Count-eq1 -and $handleTemporaryHitPointDamage[0] -is [Reflection.MethodInfo] -and
        $handleTemporaryHitPointDamage[0].IsPublic -and -not $handleTemporaryHitPointDamage[0].IsStatic -and
        $handleTemporaryHitPointDamage[0].ReturnType.FullName-ceq'System.Int32' -and
        $handleTemporaryHitPointDamage[0].GetParameters().Count-eq1 -and
        $handleTemporaryHitPointDamage[0].GetParameters()[0].ParameterType.FullName-ceq'System.Int32') `
        'diagnostic target temporary-hit-point modifier acquire, damage absorption, and removal signatures'
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
    $brainActiveGet=@(Find-Token 'Kingmaker.EntitySystem.Entities.UnitEntityData' 0x06008319)
    $brainActiveSet=@(Find-Token 'Kingmaker.EntitySystem.Entities.UnitEntityData' 0x0600831A)
    $tickBrain=@(Find-Token 'Kingmaker.Controllers.Brain.AiBrainController' 0x0600940A)
    Assert-Contract ($brainActiveGet.Count-eq1 -and $brainActiveGet[0] -is [Reflection.MethodInfo] -and
        $brainActiveGet[0].IsPublic -and -not $brainActiveGet[0].IsStatic -and
        $brainActiveGet[0].ReturnType.FullName-ceq'System.Boolean' -and $brainActiveGet[0].GetParameters().Count-eq0 -and
        $brainActiveSet.Count-eq1 -and $brainActiveSet[0] -is [Reflection.MethodInfo] -and
        $brainActiveSet[0].IsPublic -and -not $brainActiveSet[0].IsStatic -and
        $brainActiveSet[0].ReturnType.FullName-ceq'System.Void' -and $brainActiveSet[0].GetParameters().Count-eq1 -and
        $brainActiveSet[0].GetParameters()[0].ParameterType.FullName-ceq'System.Boolean' -and
        $tickBrain.Count-eq1 -and $tickBrain[0] -is [Reflection.MethodInfo] -and
        $tickBrain[0].IsPrivate -and $tickBrain[0].IsStatic -and
        $tickBrain[0].ReturnType.FullName-ceq'System.Void' -and $tickBrain[0].GetParameters().Count-eq1 -and
        $tickBrain[0].GetParameters()[0].ParameterType.FullName-ceq'Kingmaker.EntitySystem.Entities.UnitEntityData' -and
        (Test-MethodIlContainsToken $tickBrain[0] 0x06008319) -and
        -not (Test-MethodIlContainsToken $tickBrain[0] 0x06008329)) `
        'AiBrainController exact per-target IsBrainActive command-selection gate signatures'
    $combatPrepareTick=@(Find-Token 'Kingmaker.Controllers.Combat.UnitCombatPrepareController' 0x0600936F)
    $combatCanAct=@(Find-Token 'Kingmaker.Controllers.Combat.UnitCombatState' 0x0600938F)
    Assert-Contract ($combatPrepareTick.Count-eq1 -and $combatPrepareTick[0] -is [Reflection.MethodInfo] -and
        (Test-MethodIlContainsToken $combatPrepareTick[0] 0x0600C3B5) -and
        $combatCanAct.Count-eq1 -and $combatCanAct[0] -is [Reflection.MethodInfo] -and
        (Test-MethodIlContainsToken $combatCanAct[0] 0x0600938E)) `
        'combat preparation writes per-unit initiative and actor readiness consumes its own waiting state'
    $unitActionTick=@(Find-Token 'Kingmaker.Controllers.Units.UnitActionController' 0x0600911C)
    Assert-Contract ($unitActionTick.Count-eq1 -and $unitActionTick[0] -is [Reflection.MethodInfo] -and
        (Test-MethodIlContainsToken $unitActionTick[0] 0x060026A6) -and
        (Test-MethodIlContainsToken $unitActionTick[0] 0x06001851)) `
        'UnitActionController exact empty-command container stops stock unit movement'
    $filteredMoveGetter=@(Find-Token 'Kingmaker.UnitLogic.Commands.UnitCommands' 0x0600269F)
    Assert-Contract ($filteredMoveGetter.Count-eq1 -and $filteredMoveGetter[0] -is [Reflection.MethodInfo] -and
        (Test-MethodIlContainsToken $filteredMoveGetter[0] 0x0600275E)) `
        'UnitCommands.Move filters a finished raw Move slot and cannot prove exact lifecycle ownership'
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
    $gameModeType=$assembly.GetType('Kingmaker.GameModes.GameModeType',$false)
    $polymorphReplace=@(Find-Token 'Kingmaker.UnitLogic.Buffs.Polymorph' 0x06002A08)
    $polymorphRestore=@(Find-Token 'Kingmaker.UnitLogic.Buffs.Polymorph' 0x06002A09)
    $attachView=@(Find-Token 'Kingmaker.EntitySystem.EntityDataBase' 0x06007E9D)
    Assert-Contract ($null-ne$gameModeType -and $gameModeType.IsEnum -and
        [int]$gameModeType.GetField('Default').GetRawConstantValue()-eq1 -and
        [int]$gameModeType.GetField('Pause').GetRawConstantValue()-eq4 -and
        [int]$gameModeType.GetField('FullScreenUi').GetRawConstantValue()-eq5 -and
        [int]$gameModeType.GetField('EscMode').GetRawConstantValue()-eq6 -and
        [int]$gameModeType.GetField('Cutscene').GetRawConstantValue()-eq7) `
        'GameModeType exact world non-world-UI and cutscene identities'
    $turnDisable=@(Find-Token 'TurnBased.Controllers.CombatController' 0x06000BEA)
    $turnCombatEnd=@(Find-Token 'TurnBased.Controllers.CombatController' 0x06000BE3)
    $nativeTurnStart=@(Find-Token 'TurnBased.Controllers.TurnController' 0x06000C3A)
    Assert-Contract ($turnDisable.Count-eq1 -and $turnDisable[0] -is [Reflection.MethodInfo] -and
        $turnDisable[0].IsPrivate -and $turnDisable[0].GetParameters().Count-eq0 -and
        (Test-MethodIlContainsToken $turnDisable[0] 0x06000C9A) -and
        (Test-MethodIlContainsToken $turnDisable[0] 0x06000C9B) -and
        $turnCombatEnd.Count-eq1 -and $turnCombatEnd[0] -is [Reflection.MethodInfo] -and
        $turnCombatEnd[0].IsPrivate -and $turnCombatEnd[0].GetParameters().Count-eq0 -and
        (Test-MethodIlContainsToken $turnCombatEnd[0] 0x06000BDC) -and
        (Test-MethodIlContainsToken $turnCombatEnd[0] 0x0600832A) -and
        $nativeTurnStart.Count-eq1 -and $nativeTurnStart[0] -is [Reflection.MethodInfo] -and
        $nativeTurnStart[0].IsPublic -and $nativeTurnStart[0].GetParameters().Count-eq1 -and
        (Test-MethodIlContainsToken $nativeTurnStart[0] 0x0600C2FD)) `
        'native TB disable owns the combat Pause boundary its combat-end reset writes controllable AI and native turn start releases the camera follower'
    Assert-Contract ($polymorphReplace.Count-eq1 -and $polymorphReplace[0] -is [Reflection.MethodInfo] -and
        $polymorphReplace[0].IsPrivate -and $polymorphReplace[0].GetParameters().Count-eq1 -and
        $polymorphReplace[0].GetParameters()[0].ParameterType.FullName-ceq'System.Boolean' -and
        $polymorphRestore.Count-eq1 -and $polymorphRestore[0] -is [Reflection.MethodInfo] -and
        $polymorphRestore[0].IsPrivate -and $polymorphRestore[0].GetParameters().Count-eq0 -and
        $attachView.Count-eq1 -and $attachView[0] -is [Reflection.MethodInfo] -and
        (Test-MethodIlContainsToken $polymorphReplace[0] 0x06007E9D) -and
        (Test-MethodIlContainsToken $polymorphRestore[0] 0x06007E9D)) `
        'Polymorph exact replacement and restoration paths attach stock views through EntityDataBase'
    $turnBasedCombatType=$assembly.GetType('TurnBased.Controllers.CombatController',$false)
    $turnControllerType=$assembly.GetType('TurnBased.Controllers.TurnController',$false)
    $gameTurnController=@(Find-Token 'Kingmaker.Game' 0x040006C2)
    $turnInitialized=@(Find-Token 'TurnBased.Controllers.CombatController' 0x06000BC4)
    $turnSortedUnits=@(Find-Token 'TurnBased.Controllers.CombatController' 0x06000BC7)
    $turnRoundNumber=@(Find-Token 'TurnBased.Controllers.CombatController' 0x06000BC8)
    $turnStart=@(Find-Token 'TurnBased.Controllers.CombatController' 0x06000BDA)
    $turnCurrent=@(Find-Token 'TurnBased.Controllers.CombatController' 0x06000BBE)
    $turnMode=@(Find-Token 'TurnBased.Controllers.CombatController' 0x06000BF6)
    $initiativeOverrideResult=@(Find-Token 'Kingmaker.RuleSystem.Rules.RuleInitiativeRoll' 0x04004B5B)
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
        [int]$turnStatus[0].ReturnType.GetField('Acting').GetRawConstantValue()-eq3 -and
        $initiativeOverrideResult.Count-eq1 -and
        $initiativeOverrideResult[0] -is [Reflection.FieldInfo] -and
        $initiativeOverrideResult[0].IsPrivate -and -not $initiativeOverrideResult[0].IsStatic -and
        $initiativeOverrideResult[0].FieldType.IsGenericType -and
        $initiativeOverrideResult[0].FieldType.GetGenericTypeDefinition().FullName-ceq'System.Nullable`1' -and
        @($initiativeOverrideResult[0].FieldType.GetGenericArguments()).Count-eq1 -and
        @($initiativeOverrideResult[0].FieldType.GetGenericArguments())[0].FullName-ceq'System.Int32') `
        'native turn-based controller roster, rider-turn, and mode signatures'
    $trackerType=$assembly.GetType('Kingmaker.UI._ConsoleUI.TurnBasedMode.InitiativeTrackerVM',$false)
    $trackerConstructor=if($null-eq$trackerType){$null}else{@($trackerType.GetConstructors([Reflection.BindingFlags]'Public,NonPublic,Instance')|Where-Object MetadataToken -eq 0x06004F00|Select-Object -First 1)}
    $trackerUpdate=@(Find-Token 'Kingmaker.UI._ConsoleUI.TurnBasedMode.InitiativeTrackerVM' 0x06004F0E)
    Assert-Contract ($null-ne$trackerType -and $null-ne$trackerConstructor -and @($trackerConstructor).Count-eq1 -and
        @($trackerConstructor)[0].IsPublic -and @($trackerConstructor)[0].GetParameters().Count-eq0 -and
        $trackerUpdate.Count-eq1 -and $trackerUpdate[0] -is [Reflection.MethodInfo] -and
        $trackerUpdate[0].IsPrivate -and -not $trackerUpdate[0].IsStatic -and
        $trackerUpdate[0].ReturnType.FullName-ceq'System.Void' -and $trackerUpdate[0].GetParameters().Count-eq0 -and
        (Test-MethodIlContainsToken $trackerUpdate[0] 0x06000BBE) -and
        (Test-MethodIlContainsToken $trackerUpdate[0] 0x06000BC7)) `
        'native tracker constructor and UpdateUnits current-turn then sorted-roster projection seams'
    $clickMapObject=@(Find-Token 'Kingmaker.Controllers.Clicks.Handlers.ClickMapObjectHandler' 0x060093E2)
    $interactCtor=@(Find-Token 'Kingmaker.UnitLogic.Commands.UnitInteractWithObject' 0x060026D6)
    $interactAction=@(Find-Token 'Kingmaker.UnitLogic.Commands.UnitInteractWithObject' 0x060026DE)
    $doorInteract=@(Find-Token 'Kingmaker.View.MapObjects.StandardDoor' 0x060019ED)
    $doorCanInteract=@(Find-Token 'Kingmaker.View.MapObjects.StandardDoor' 0x060019F0)
    $doorState=@(Find-Token 'Kingmaker.View.MapObjects.StandardDoor' 0x06001AA7)
    $navmeshCutType=$firstpass.GetType('Pathfinding.NavmeshCut',$false)
    $navmeshCutRequiresUpdate=if($null-eq$navmeshCutType){$null}else{$navmeshCutType.GetMethod('RequiresUpdate',[Reflection.BindingFlags]'Public,Instance')}
    $tileHandlerHelperType=$firstpass.GetType('Pathfinding.TileHandlerHelper',$false)
    $tileHandlerUpdate=if($null-eq$tileHandlerHelperType){$null}else{$tileHandlerHelperType.GetMethod('ForceUpdate',[Reflection.BindingFlags]'Public,Instance')}
    $tileHandlerType=$firstpass.GetType('Pathfinding.Util.TileHandler',$false)
    $tileHandlerLastUpdateFrame=if($null-eq$tileHandlerType){$null}else{$tileHandlerType.GetProperty('LastUpdateFrame',[Reflection.BindingFlags]'Public,Static')}
    $unitMovementPathTo=@(Find-Token 'Kingmaker.View.UnitMovementAgent' 0x060018A3)
    $astarPathType=$firstpass.GetType('AstarPath',$false)
    $astarPathActive=if($null-eq$astarPathType){$null}else{$astarPathType.GetField('active',[Reflection.BindingFlags]'Public,Static')}
    $astarGraphUpdatesQueued=if($null-eq$astarPathType){$null}else{$astarPathType.GetProperty('IsAnyGraphUpdatesQueued',[Reflection.BindingFlags]'Public,Instance')}
    Assert-Contract ($clickMapObject.Count-eq1 -and $clickMapObject[0] -is [Reflection.MethodInfo] -and
        $clickMapObject[0].IsPublic -and -not $clickMapObject[0].IsStatic -and
        $clickMapObject[0].ReturnType.FullName-ceq'System.Boolean' -and
        $clickMapObject[0].GetParameters().Count-eq5 -and
        $clickMapObject[0].GetParameters()[0].ParameterType.FullName-ceq'UnityEngine.GameObject' -and
        $interactCtor.Count-eq1 -and $interactCtor[0] -is [Reflection.ConstructorInfo] -and
        $interactCtor[0].IsPublic -and $interactCtor[0].GetParameters().Count-eq1 -and
        $interactCtor[0].GetParameters()[0].ParameterType.FullName-ceq'Kingmaker.View.MapObjects.InteractionComponent' -and
        $interactAction.Count-eq1 -and $interactAction[0] -is [Reflection.MethodInfo] -and
        $interactAction[0].IsFamily -and $interactAction[0].GetParameters().Count-eq0 -and
        $interactAction[0].ReturnType.FullName-ceq'Kingmaker.UnitLogic.Commands.Base.UnitCommand+ResultType' -and
        $doorInteract.Count-eq1 -and $doorInteract[0].IsPublic -and -not $doorInteract[0].IsStatic -and
        $doorInteract[0].GetParameters().Count-eq1 -and
        $doorInteract[0].GetParameters()[0].ParameterType.FullName-ceq'Kingmaker.EntitySystem.Entities.UnitEntityData' -and
        $doorCanInteract.Count-eq1 -and $doorCanInteract[0].IsPublic -and
        $doorCanInteract[0].ReturnType.FullName-ceq'System.Boolean' -and
        $doorState.Count-eq1 -and $doorState[0].IsPublic -and
        $doorState[0].ReturnType.FullName-ceq'System.Boolean' -and
        $null-ne$navmeshCutRequiresUpdate -and $navmeshCutRequiresUpdate.ReturnType.FullName-ceq'System.Boolean' -and
        $navmeshCutRequiresUpdate.GetParameters().Count-eq0 -and
        $null-ne$tileHandlerUpdate -and $tileHandlerUpdate.ReturnType.FullName-ceq'System.Void' -and
        $tileHandlerUpdate.GetParameters().Count-eq0) `
        'native map-object input, StandardDoor, and deferred navmesh-cut update seams'
    Assert-Contract ($null-ne$astarPathType -and $null-ne$astarPathActive -and
        $astarPathActive.FieldType.FullName-ceq'AstarPath' -and
        $null-ne$astarGraphUpdatesQueued -and
        $astarGraphUpdatesQueued.PropertyType.FullName-ceq'System.Boolean' -and
        $null-ne$astarGraphUpdatesQueued.GetGetMethod() -and
        $astarGraphUpdatesQueued.GetGetMethod().IsPublic -and
        -not $astarGraphUpdatesQueued.GetGetMethod().IsStatic -and
        $astarGraphUpdatesQueued.GetGetMethod().GetParameters().Count-eq0) `
        'native AstarPath graph-update queue observation seams'
    Assert-Contract ($null-ne$tileHandlerType -and $tileHandlerType.MetadataToken-eq0x020006C4 -and
        $null-ne$tileHandlerLastUpdateFrame -and $tileHandlerLastUpdateFrame.MetadataToken-eq0x170005E6 -and
        $tileHandlerLastUpdateFrame.PropertyType.FullName-ceq'System.Int32' -and
        $null-ne$tileHandlerLastUpdateFrame.GetGetMethod() -and
        $tileHandlerLastUpdateFrame.GetGetMethod().MetadataToken-eq0x060036E8 -and
        $tileHandlerLastUpdateFrame.GetGetMethod().IsPublic -and
        $tileHandlerLastUpdateFrame.GetGetMethod().IsStatic -and
        $unitMovementPathTo.Count-eq1 -and $unitMovementPathTo[0] -is [Reflection.MethodInfo] -and
        $unitMovementPathTo[0].IsPublic -and -not $unitMovementPathTo[0].IsStatic -and
        $unitMovementPathTo[0].ReturnType.FullName-ceq'System.Void' -and
        $unitMovementPathTo[0].GetParameters().Count-eq5 -and
        $unitMovementPathTo[0].GetParameters()[0].ParameterType.FullName-ceq'Kingmaker.UnitLogic.Commands.Base.UnitCommand' -and
        $unitMovementPathTo[0].GetParameters()[1].ParameterType.FullName-ceq'UnityEngine.Vector3' -and
        $unitMovementPathTo[0].GetParameters()[2].ParameterType.FullName-ceq'System.Single' -and
        $unitMovementPathTo[0].GetParameters()[3].ParameterType.FullName-ceq'System.Single' -and
        $unitMovementPathTo[0].GetParameters()[4].ParameterType.FullName-ceq'Kingmaker.View.UnitMovementAgentBase') `
        'native UnitMovementAgent.PathTo and TileHandler frame-suppression observation seams'
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
        @('TurnBased.Controllers.CombatController',0x06000E88,'ChooseNextUnit'),@('TurnBased.Controllers.CombatController',0x06000E91,'StartTurn'),
        @('TurnBased.Controllers.TurnController',0x06000F08,'Start'),@('TurnBased.Controllers.TurnController',0x06000F22,'IsAllActed'),
        @('TurnBased.Controllers.TurnController',0x06000F33,'HandleUnitCommandDidStart'),@('TurnBased.Controllers.TurnController',0x06000F34,'HandleUnitCommandDidEnd'),
        @('Kingmaker.Controllers.Units.SaddledUnitController',0x0600AB05,'TickDelegateMountToRider'),@('Kingmaker.Controllers.Units.SaddledUnitController',0x0600AB06,'TickDelegateRiderToMount'),
        @('Kingmaker.Controllers.Units.UnitMoveController',0x0600ABAA,'TickUnit'),
        @('Kingmaker.UnitLogic.Commands.Base.UnitCommand',0x0600CA6B,'AddRiderCommand'),@('Kingmaker.UnitLogic.Commands.Base.UnitCommand',0x0600CA6C,'AddMountCommand'),
        @('Kingmaker.UnitLogic.Commands.Base.UnitCommand',0x0600CA7E,'TickApproaching'),@('Kingmaker.UnitLogic.Commands.Base.UnitCommand',0x0600CA86,'Interrupt'),
        @('Kingmaker.UnitLogic.Commands.UnitCommands',0x0600C935,'FixTargetIfTargetOnMount'),
        @('Kingmaker.EntitySystem.Entities.UnitEntityData',0x0600A123,'IsCurrentUnit'),
        @('Kingmaker.UI.MVVM._VM.ActionBar.ActionBarVM',0x06005ADD,'UpdateSelection'))
    foreach($check in $checks){$member=@(Find-Token $check[0] $check[1]);$matches=$member.Count -eq 1;if($matches){$matches=[string]$member[0].Name -ceq [string]$check[2]};Assert-Contract $matches "token $($check[1].ToString('X8')) $($check[0]).$($check[2])"}
    $turnType=$assembly.GetType('TurnBased.Controllers.TurnController',$false)
    $turnFields=if($null-eq$turnType){@()}else{@($turnType.GetFields([Reflection.BindingFlags]'Public,NonPublic,Instance,DeclaredOnly'))}
    Assert-Contract (@('m_Rider','m_Mount','m_RiderCommands','m_MountCommands','m_RiderCombatState','m_MountCombatState','m_RiderCooldown','m_MountCooldown','m_RiderMovementStats','m_MountMovementStats','m_RiderActionState','m_MountActionState' | Where-Object {$name=$_;@($turnFields|Where-Object Name -ceq $name).Count-ne1}).Count-eq0) `
        'TurnController retains distinct rider and mount command combat cooldown movement and action ledgers'
}
Write-Host "TOTAL PASS=$passes FAIL=$($failures.Count)"
if($failures.Count-ne0){exit 1}
