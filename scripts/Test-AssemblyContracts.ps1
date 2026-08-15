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
        @('Kingmaker.View.UnitMovementAgent',0x060018C2,'Stop'),@('Kingmaker.EntitySystem.Entities.UnitEntityData',0x06008345,'Translocate'),
        @('Kingmaker.Controllers.Clicks.Handlers.ClickGroundHandler',0x060093DA,'MoveSelectedUnitsToPoint'),
        @('Kingmaker.UnitLogic.Commands.UnitCommands',0x0600269F,'get_Move'),@('Kingmaker.UnitLogic.Commands.UnitMoveTo',0x060026F4,'get_Target'),
        @('Kingmaker.View.UnitMovementAgent',0x060018A8,'FindPath'),@('Kingmaker.UI.Selection.SelectionManager',0x060034E2,'get_Instance'),
        @('Kingmaker.UI.Selection.SelectionManager',0x060034E4,'get_SelectedUnits'),@('Kingmaker.Game',0x06000C9A,'get_IsPaused'),
        @('Kingmaker.Game',0x06000C9B,'set_IsPaused'),@('Kingmaker.Game',0x06000CD6,'ReloadArea'),
        @('Kingmaker.Game',0x06000CE4,'SaveGame'),@('Kingmaker.Game',0x06000CE9,'GetCamera'),
        @('Kingmaker.UI.SettingsUI.SettingsEntityBool',0x04002275,'m_Cached'),
        @('Kingmaker.UI.SettingsUI.SettingsEntityBase',0x04002269,'OnOptionUpdatedCallback'),
        @('Kingmaker.UI.SettingsUI.SettingsEntityBase',0x06003359,'OnInvokeUpdateCallback'),
        @('Kingmaker.UI.SettingsUI.SettingsEntityBase',0x0600335C,'GetSavedValueString'),
        @('Kingmaker.UI.SettingsUI.SettingsRoot+SettingsListScreen',0x04007C9F,'EnableTurnBasedMode'),
        @('Kingmaker.UI.SettingsUI.SettingsRoot+SettingsListScreen',0x04007CBC,'OnlyOneSave'),
        @('Kingmaker.EntitySystem.Persistence.LoadingProcess',0x06007FBC,'get_IsLoadingInProcess'),
        @('Kingmaker.Utility.Screenshot',0x06001D41,'CapturePNG'),@('Kingmaker.View.MapObjects.StandardDoor',0x06001AA0,'get_IsOpen'))
    foreach($check in $checks){$member=@(Find-Token $check[0] $check[1]);$matches=$member.Count -eq 1;if($matches){$matches=[string]$member[0].Name -ceq [string]$check[2]};Assert-Contract $matches "token $($check[1].ToString('X8')) $($check[0]).$($check[2])"}
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
    $startCoroutine=if($null-eq$monoBehaviour){$null}else{@($monoBehaviour.GetMethods([Reflection.BindingFlags]'Public,Instance')|Where-Object MetadataToken -eq 0x06000E39|Select-Object -First 1)}
    $stopCoroutine=if($null-eq$monoBehaviour){$null}else{@($monoBehaviour.GetMethods([Reflection.BindingFlags]'Public,Instance')|Where-Object MetadataToken -eq 0x06000E3C|Select-Object -First 1)}
    $waitConstructor=if($null-eq$waitForEndOfFrame){$null}else{@($waitForEndOfFrame.GetConstructors([Reflection.BindingFlags]'Public,Instance')|Where-Object MetadataToken -eq 0x0600156D|Select-Object -First 1)}
    Assert-Contract ($null-ne$startCoroutine -and @($startCoroutine).Count-eq1 -and @($startCoroutine)[0].GetParameters().Count-eq1 -and @($startCoroutine)[0].GetParameters()[0].ParameterType.FullName-ceq'System.Collections.IEnumerator' -and @($startCoroutine)[0].ReturnType.FullName-ceq'UnityEngine.Coroutine') 'Unity token 06000E39 MonoBehaviour.StartCoroutine(IEnumerator)'
    Assert-Contract ($null-ne$stopCoroutine -and @($stopCoroutine).Count-eq1 -and @($stopCoroutine)[0].GetParameters().Count-eq1 -and @($stopCoroutine)[0].GetParameters()[0].ParameterType.FullName-ceq'UnityEngine.Coroutine' -and @($stopCoroutine)[0].ReturnType.FullName-ceq'System.Void') 'Unity token 06000E3C MonoBehaviour.StopCoroutine(Coroutine)'
    Assert-Contract ($null-ne$waitConstructor -and @($waitConstructor).Count-eq1 -and @($waitConstructor)[0].GetParameters().Count-eq0) 'Unity token 0600156D WaitForEndOfFrame constructor'
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
