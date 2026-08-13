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
    foreach($name in @('Kingmaker.UnitLogic.Parts.UnitPartRider','Kingmaker.UnitLogic.Parts.UnitPartSaddled','Kingmaker.Controllers.Units.SaddledUnitController')){Assert-Contract ($null-eq$assembly.GetType($name,$false)) "mounted type absent: $name"}
    $gameVersionType=$assembly.GetType('Kingmaker.GameVersion',$false)
    $gameVersionMethod=if($null-eq$gameVersionType){$null}else{$gameVersionType.GetMethod('GetVersion',[Reflection.BindingFlags]'Public,Static')}
    Assert-Contract ($null-ne$gameVersionMethod -and $gameVersionMethod.ReturnType.FullName -ceq 'System.String' -and $gameVersionMethod.GetParameters().Count -eq 0) 'GameVersion.GetVersion exact runtime version seam'
    $checks=@(
        @('Kingmaker.Controllers.Clicks.Handlers.ClickGroundHandler',0x060093DC,'RunCommand'),@('Kingmaker.UI.Selection.SelectionManager',0x060034F0,'SelectUnit'),
        @('Kingmaker.UI.Selection.SelectionManager',0x060034F5,'MultiSelect'),@('SelectionManagerBase',0x060000B9,'Stop'),
        @('SelectionManagerBase',0x060000BA,'Hold'),@('Kingmaker.UnitLogic.Commands.UnitMoveContiniously',0x060026F0,'Init'),
        @('Kingmaker.EntitySystem.Persistence.SaveManager',0x06008012,'LoadZipSave'),@('Kingmaker.EntitySystem.Persistence.SaveManager',0x06008029,'SaveRoutine'),
        @('Kingmaker.EntitySystem.Persistence.SaveManager',0x0600802C,'LoadRoutine'),@('Kingmaker.EntitySystem.Persistence.SaveManager',0x06008030,'AddCallbackAfterLoad'),
        @('Kingmaker.Game',0x06000CE0,'LoadGame'),@('Kingmaker.Blueprints.BlueprintScriptableObject',0x06009637,'get_AssetGuidThreadSafe'),
        @('Kingmaker.Controllers.Units.UnitMoveController',0x06009183,'Tick'),@('Kingmaker.View.UnitEntityView',0x0600184D,'MoveTo'),
        @('Kingmaker.View.UnitMovementAgent',0x060018C2,'Stop'),@('Kingmaker.EntitySystem.Entities.UnitEntityData',0x06008345,'Translocate'))
    foreach($check in $checks){$member=@(Find-Token $check[0] $check[1]);$matches=$member.Count -eq 1;if($matches){$matches=[string]$member[0].Name -ceq [string]$check[2]};Assert-Contract $matches "token $($check[1].ToString('X8')) $($check[0]).$($check[2])"}
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
