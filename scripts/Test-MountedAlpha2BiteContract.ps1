[CmdletBinding()]
param([string]$Configuration = 'Release')
# Mounted Combat alpha 2, MMR-07 (review R1-F13): the production Horse Bite handle binding and strike reader on genuine
# Kingmaker objects, and the compiled wiring that reaches them.
#  1. The adapter's real-type boundary (HorsePrimaryAttackAnimationAdapter.HandleBoundary) with the production decision
#     (HorseStrikeHandleBinding.Bind) on genuine UnitAnimationActionHandle / UnitAnimationActionSpecialAttack /
#     UnitAnimationManager objects created without Unity: stock adoption, exactly-once, same-action refresh, a refused
#     different-action refresh, a foreign or dead manager, hoof control, a missing handle supplied once, and a refused
#     ambiguous fallback. The record/create/assign operations are the adapter's delegates, supplied here as script blocks.
#  2. The strike reader's real-type source (MountedPairAttackCommand+HorseStrikeSource) on a genuine handle for the handle
#     facts that need no Unity engine call.
#  3. Compiled wiring: the native AttackHandInfo.CreateAnimationHandleForAttack postfix reaches SupplyExact and the binding;
#     MountedPairAttackCommand.OnAction calls ObserveHorseStrike once and its base OnAction exactly once (native attack event
#     and damage count unchanged); ObserveHorseStrike reads through HorseStrikeReader.
# Not covered (Unity/native game only): the animation manager's real CreateHandle, the action's name and variant table, the
# active AnimationBase's clip/weight/state during playback, and what was drawn on screen (owner footage observation).
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$lab = [IO.Path]::GetFullPath((Join-Path $repoRoot '..\..'))
$layout = (Get-Content -Raw -LiteralPath (Join-Path $lab 'environment-intake.json') | ConvertFrom-Json).requestedLayout
$managed = Join-Path (Join-Path $layout.kingmakerInstallDir 'Kingmaker_Data') 'Managed'
$productDll = Join-Path $repoRoot ('bin\' + $Configuration + '\KingmakerMountedCombat.dll')
$script:passed = 0
function Check([bool]$Value, [string]$Label) { if (-not $Value) { throw ('Mounted alpha 2 bite contract: ' + $Label) }; $script:passed++; Write-Host ('PASS ' + $Label) }

Check (Test-Path -LiteralPath $productDll -PathType Leaf) ('built product present: ' + $productDll)
$script:resolving = @{}
[AppDomain]::CurrentDomain.add_AssemblyResolve({ param($sender, $eventArgs)
    $short = ($eventArgs.Name -split ',')[0]
    if ($short.EndsWith('.resources') -or $script:resolving.ContainsKey($short)) { return $null }
    $loaded = @([AppDomain]::CurrentDomain.GetAssemblies() | Where-Object { $_.GetName().Name -eq $short }) | Select-Object -First 1
    if ($loaded) { return $loaded }
    $script:resolving[$short] = $true
    try {
        foreach ($dir in @($managed, (Join-Path $managed 'UnityModManager'))) { $path = Join-Path $dir ($short + '.dll'); if (Test-Path -LiteralPath $path) { return [Reflection.Assembly]::LoadFile($path) } }
        return $null
    } finally { $script:resolving.Remove($short) } })
$game = [Reflection.Assembly]::LoadFrom((Join-Path $managed 'Assembly-CSharp.dll'))
$kmc = [Reflection.Assembly]::LoadFile($productDll)
function GameType([string]$Name) { $game.GetType($Name, $true) }
function KmcType([string]$Name) { $kmc.GetType($Name, $true) }
function New-Native([Type]$Type) { [Runtime.Serialization.FormatterServices]::GetUninitializedObject($Type) }
$any = [Reflection.BindingFlags]'Instance,Static,Public,NonPublic'
function SetField($Target, [Type]$Type, [string]$Field, $Value) {
    $info = $null; $t = $Type
    while ($null -ne $t -and $null -eq $info) { $info = $t.GetField($Field, [Reflection.BindingFlags]'Instance,Public,NonPublic,DeclaredOnly'); $t = $t.BaseType }
    if ($null -eq $info) { throw ('Missing native field ' + $Type.FullName + '.' + $Field) }
    $info.SetValue($Target, $Value)
}
function As-Delegate([scriptblock]$Block, [Type]$DelegateType) { [Management.Automation.LanguagePrimitives]::ConvertTo($Block, $DelegateType) }

# ---- 1. The adapter's real-type boundary and the production binding ----------------------------------------------
$handleType = GameType 'Kingmaker.Visual.Animation.Kingmaker.UnitAnimationActionHandle'
$baseHandleType = GameType 'Kingmaker.Visual.Animation.Actions.AnimationActionHandle'
$actionType = GameType 'Kingmaker.Visual.Animation.Kingmaker.Actions.UnitAnimationActionSpecialAttack'
$managerType = GameType 'Kingmaker.Visual.Animation.Kingmaker.UnitAnimationManager'
$attackKind = GameType 'Kingmaker.Visual.Animation.Kingmaker.UnitAnimationSpecialAttackType'
function New-Action([string]$Kind) { $a = New-Native $actionType; SetField $a $actionType 'm_AttackType' ([Enum]::Parse($attackKind, $Kind)); $a }
function New-Handle($Action, $Manager) {
    $h = New-Native $handleType
    SetField $h $baseHandleType '<Action>k__BackingField' $Action
    SetField $h $baseHandleType '<Manager>k__BackingField' $Manager
    $h
}
$boundaryType = KmcType 'KingmakerMountedCombat.Integration.HorsePrimaryAttackAnimationAdapter+HandleBoundary'
$boundaryCtor = @($boundaryType.GetConstructors($any))[0]
$bindingType = KmcType 'KingmakerMountedCombat.Domain.HorseStrikeHandleBinding'
$bind = $bindingType.GetMethod('Bind').MakeGenericMethod($handleType, $actionType)
$listType = [Collections.Generic.List`1].MakeGenericType($actionType)
$funcHandleBool = [Func`2].MakeGenericType($handleType, [bool])
$funcAction = [Func`1].MakeGenericType($actionType)
$recordType = [Action`3].MakeGenericType($handleType, $actionType, [string])
$refreshType = [Action`2].MakeGenericType($handleType, $actionType)
$createType = [Func`2].MakeGenericType($actionType, $handleType)
$assignType = [Action`1].MakeGenericType($handleType)

# One planned strike's state: the adapter's command record and attack slot, as the delegates see them.
function New-Strike($Current, $Manager, [bool]$Alive, [string]$Expected, $Actions) {
    @{ Current = $Current; Manager = $Manager; Alive = $Alive; Expected = $Expected; Actions = $Actions;
       Recorded = $null; RecordedAction = $null; Source = $null; Records = 0; Refreshes = 0; Creates = 0; Assigns = 0 }
}
function Bind-Strike($Strike) {
    $script:s = $Strike
    $list = [Activator]::CreateInstance($listType); foreach ($a in @($Strike.Actions)) { $list.Add($a) }
    $arguments = [object[]]@(
        $true, $Strike.Current, $Strike.Manager, $Strike.Alive, [Enum]::Parse($attackKind, $Strike.Expected), $list,
        (As-Delegate { param($h) $null -ne $script:s.Recorded -and [object]::ReferenceEquals($script:s.Recorded, $h) } $funcHandleBool),
        (As-Delegate { $script:s.RecordedAction } $funcAction),
        (As-Delegate { $null -ne $script:s.Recorded } ([Func[bool]])),
        (As-Delegate { param($h, $a, $source) if ($null -ne $script:s.Recorded) { throw 'duplicate record' }; $script:s.Recorded = $h; $script:s.RecordedAction = $a; $script:s.Source = $source; $script:s.Records++ } $recordType),
        (As-Delegate { param($h, $a) if ($null -eq $script:s.Recorded -or [object]::ReferenceEquals($script:s.Recorded, $h) -or -not [object]::ReferenceEquals($script:s.RecordedAction, $a)) { throw 'inexact refresh' }; $script:s.Recorded = $h; $script:s.Refreshes++ } $refreshType),
        (As-Delegate { param($a) $script:s.Creates++; New-Handle $a $script:s.Manager } $createType),
        (As-Delegate { param($h) $script:s.Assigns++; $script:s.Current = $h } $assignType))
    $boundary = $boundaryCtor.Invoke($arguments)
    $result = $bind.Invoke($null, [object[]]@($boundary))
    $Strike.LastResult = $result
    $result.Outcome.ToString()
}

$manager = New-Native $managerType
$bite = New-Action 'Bite'; $hoof = New-Action 'Hoof'
$horseActions = @($bite, $hoof)
$stock = New-Handle $bite $manager
$strike = New-Strike $stock $manager $true 'Bite' $horseActions
Check ((Bind-Strike $strike) -ceq 'Adopted' -and [object]::ReferenceEquals($strike.Recorded, $stock) -and $strike.Source -ceq 'stock-created' -and $strike.Assigns -eq 0 -and $strike.Creates -eq 0) 'genuine stock Bite handle of the Horse manager is adopted, not replaced'
Check ((Bind-Strike $strike) -ceq 'AlreadyRecorded' -and $strike.Records -eq 1) 'the same genuine handle is recorded exactly once'
$later = New-Handle $bite $manager; $strike.Current = $later
Check ((Bind-Strike $strike) -ceq 'Refreshed' -and [object]::ReferenceEquals($strike.Recorded, $later) -and $strike.Records -eq 1 -and $strike.Refreshes -eq 1) 'a later genuine stock handle of the same action refreshes the record'
$strike.Expected = 'Hoof'; $strike.Current = New-Handle $hoof $manager
Check ((Bind-Strike $strike) -ceq 'RejectedRefreshAction' -and [object]::ReferenceEquals($strike.Recorded, $later)) 'a later handle playing a different SpecialAttack action is refused; the record keeps the bite'
$foreign = New-Strike (New-Handle $bite (New-Native $managerType)) $manager $true 'Bite' $horseActions
Check ((Bind-Strike $foreign) -ceq 'RejectedStockMismatch' -and $foreign.Records -eq 0) 'a genuine handle from another animation manager is refused'
$dead = New-Strike (New-Handle $bite $manager) $manager $false 'Bite' $horseActions
Check ((Bind-Strike $dead) -ceq 'RejectedStockMismatch' -and $dead.Records -eq 0) 'no adoption without a live Horse manager'
$unlisted = New-Strike (New-Handle (New-Action 'Bite') $manager) $manager $true 'Bite' $horseActions
Check ((Bind-Strike $unlisted) -ceq 'RejectedStockMismatch') 'a Bite action outside the Horse action set is refused'
$stockHoof = New-Strike (New-Handle $hoof $manager) $manager $true 'Hoof' $horseActions
Check ((Bind-Strike $stockHoof) -ceq 'Adopted' -and [object]::ReferenceEquals($stockHoof.RecordedAction, $hoof)) 'hoof control: a genuine stock hoof handle adopts the hoof action'
$biteForHoof = New-Strike (New-Handle $bite $manager) $manager $true 'Hoof' $horseActions
Check ((Bind-Strike $biteForHoof) -ceq 'RejectedStockMismatch' -and $biteForHoof.Records -eq 0) 'hoof control: a bite handle is never adopted for a hoof strike'
$missing = New-Strike $null $manager $true 'Bite' $horseActions
Check ((Bind-Strike $missing) -ceq 'Supplied' -and $missing.Creates -eq 1 -and $missing.Assigns -eq 1 -and $missing.Records -eq 1 -and $missing.Source -ceq 'kmc-supplied' -and [object]::ReferenceEquals($missing.Current.Action, $bite)) 'a missing stock handle gets exactly one handle of the single Bite action'
Check ((Bind-Strike $missing) -ceq 'AlreadyRecorded' -and $missing.Creates -eq 1 -and $missing.Assigns -eq 1) 'the supplied handle is not supplied again'
# Review R4-F7: after a recorded handle (adopted or supplied), a strike planned without a handle gets no fallback: no
# creation, no assignment, no second record (the record delegate throws on a duplicate, as MountedPairAttackCommand does).
$adoptedThenMissing = New-Strike (New-Handle $bite $manager) $manager $true 'Bite' $horseActions
[void](Bind-Strike $adoptedThenMissing); $adoptedThenMissing.Current = $null
Check ((Bind-Strike $adoptedThenMissing) -ceq 'MissingAfterRecorded' -and $adoptedThenMissing.Records -eq 1 -and $adoptedThenMissing.Creates -eq 0 -and $adoptedThenMissing.Assigns -eq 0 -and $null -eq $adoptedThenMissing.Current) 'R4-F7: adopted, then a strike without a handle: no fallback, one record, nothing assigned'
$missing.Current = $null
Check ((Bind-Strike $missing) -ceq 'MissingAfterRecorded' -and $missing.Records -eq 1 -and $missing.Creates -eq 1 -and $missing.Assigns -eq 1 -and $null -eq $missing.Current) 'R4-F7: supplied, then a strike without a handle: no second fallback or record'
$laterHoof = New-Strike (New-Handle $bite $manager) $manager $true 'Bite' $horseActions
[void](Bind-Strike $laterHoof); $laterHoof.Expected = 'Hoof'; $laterHoof.Current = $null
Check ((Bind-Strike $laterHoof) -ceq 'MissingAfterRecorded' -and $laterHoof.Records -eq 1 -and $laterHoof.Creates -eq 0 -and $laterHoof.Assigns -eq 0 -and [object]::ReferenceEquals($laterHoof.RecordedAction, $bite)) 'R4-F7: a later hoof strike without a handle after a recorded bite is left untouched'
$ambiguous = New-Strike $null $manager $true 'Bite' @($bite, (New-Action 'Bite'), $hoof)
Check ((Bind-Strike $ambiguous) -ceq 'RejectedCandidateCount' -and $ambiguous.LastResult.CandidateCount -eq 2 -and $ambiguous.Assigns -eq 0 -and $ambiguous.Creates -eq 0) 'two matching actions: the fallback assigns nothing'

# ---- 2. The strike reader's real-type source on a genuine handle ---------------------------------------------------
$sourceType = KmcType 'KingmakerMountedCombat.Integration.MountedPairAttackCommand+HorseStrikeSource'
$source = @($sourceType.GetConstructors($any))[0].Invoke([object[]]@($null))
$readHandle = New-Handle $bite $manager
SetField $readHandle $baseHandleType '<IsStarted>k__BackingField' $true
SetField $readHandle $baseHandleType '<IsActed>k__BackingField' $true
SetField $readHandle $baseHandleType 'm_SpeedScale' ([single]2.4)
$handleType.GetProperty('Variant').SetValue($readHandle, 1, $null)
function SourceCall([string]$Name) { $sourceType.GetMethod($Name, $any).Invoke($source, [object[]]@($readHandle)) }
Check ((SourceCall 'IsStarted') -and (SourceCall 'IsActed') -and -not (SourceCall 'IsInterrupted') -and [Math]::Abs((SourceCall 'SpeedScale') - 2.4) -lt 0.0001 -and (SourceCall 'Variant') -eq 1) 'reader source states a genuine handle''s started/acted/interrupted, speedScale and variant'
Check ($null -eq (SourceCall 'ActiveState') -and (SourceCall 'ActiveWeight') -eq 0) 'reader source reports no active animation for a handle that never played'
Check ($null -eq $sourceType.GetProperty('Handle').GetValue($source, $null)) 'reader source without a planned attack reports no handle (handle=none)'

# ---- 3. Compiled wiring -----------------------------------------------------------------------------------------
function Calls([Type]$Type, [string]$Method, [Reflection.MethodBase]$Target, [switch]$Count) {
    $total = 0
    $types = New-Object 'System.Collections.Generic.List[Type]'; $types.Add($Type)
    for ($i = 0; $i -lt $types.Count; $i++) { foreach ($n in $types[$i].GetNestedTypes($any)) { $types.Add($n) } }
    foreach ($t in $types) {
        foreach ($m in $t.GetMethods($any + [Reflection.BindingFlags]::DeclaredOnly)) {
            if ($Method -and $t -eq $Type -and $m.Name -cne $Method) { continue }
            if ($Method -and $t -ne $Type -and -not $m.Name.Contains('<' + $Method + '>')) { continue }
            $body = $m.GetMethodBody(); if ($null -eq $body) { continue }
            $il = $body.GetILAsByteArray()
            for ($i = 0; $i -le $il.Length - 5; $i++) {
                if ($il[$i] -ne 0x28 -and $il[$i] -ne 0x6F) { continue }
                try { $resolved = $m.Module.ResolveMethod([BitConverter]::ToInt32($il, $i + 1), $t.GetGenericArguments(), $null) } catch { continue }
                if ($resolved -is [Reflection.MethodInfo] -and $resolved.IsGenericMethod) { $resolved = $resolved.GetGenericMethodDefinition() }
                if ($resolved -eq $Target) { $total++ }
            }
        }
    }
    if ($Count) { $total } else { $total -gt 0 }
}
$patches = KmcType 'KingmakerMountedCombat.Integration.MountedPatchController+PatchMethods'
$animation = KmcType 'KingmakerMountedCombat.Integration.MountedAnimationAdapter'
$adapter = KmcType 'KingmakerMountedCombat.Integration.HorsePrimaryAttackAnimationAdapter'
$supplyHorse = $animation.GetMethod('SupplyExactHorsePrimaryAnimation', $any)
$supplyExact = $adapter.GetMethod('SupplyExact', $any)
Check (Calls $patches 'AttackAnimationPostfix' $supplyHorse) 'the CreateAnimationHandleForAttack postfix reaches MountedAnimationAdapter.SupplyExactHorsePrimaryAnimation'
Check (Calls $animation 'SupplyExactHorsePrimaryAnimation' $supplyExact) 'SupplyExactHorsePrimaryAnimation calls the Horse adapter''s SupplyExact'
Check (Calls $adapter 'SupplyExact' $bindingType.GetMethod('Bind')) 'SupplyExact decides through HorseStrikeHandleBinding.Bind'
$supplyClauses = @($supplyExact.GetMethodBody().ExceptionHandlingClauses | Where-Object { $_.Flags -eq [Reflection.ExceptionHandlingClauseOptions]::Clause -and $_.CatchType -eq [Exception] })
Check ($supplyClauses.Count -ge 1) 'R4-F7: SupplyExact contains the binding in a catch-all, so no binding exception reaches the native handle-creation postfix'
$controller = KmcType 'KingmakerMountedCombat.Integration.MountedPatchController'
$registered = $false
$controllerTypes = New-Object 'System.Collections.Generic.List[Type]'; $controllerTypes.Add($controller)
for ($i = 0; $i -lt $controllerTypes.Count; $i++) { foreach ($n in $controllerTypes[$i].GetNestedTypes($any)) { $controllerTypes.Add($n) } }
# The registrations are made in the controller's constructor.
foreach ($m in @($controllerTypes | ForEach-Object { $_.GetMethods($any + [Reflection.BindingFlags]::DeclaredOnly); $_.GetConstructors($any + [Reflection.BindingFlags]::DeclaredOnly) })) {
    $body = $m.GetMethodBody(); if ($null -eq $body) { continue }
    $il = $body.GetILAsByteArray(); $strings = @{}
    for ($i = 0; $i -le $il.Length - 5; $i++) { if ($il[$i] -eq 0x72) { try { $strings[$m.Module.ResolveString([BitConverter]::ToInt32($il, $i + 1))] = $true } catch { } } }
    if ($strings.ContainsKey('CreateAnimationHandleForAttack') -and $strings.ContainsKey('AttackAnimationPostfix')) { $registered = $true }
}
Check $registered 'AttackHandInfo.CreateAnimationHandleForAttack is patched with AttackAnimationPostfix'
$command = KmcType 'KingmakerMountedCombat.Integration.MountedPairAttackCommand'
$onAction = $command.GetMethod('OnAction', $any + [Reflection.BindingFlags]::DeclaredOnly)
$observe = $command.GetMethod('ObserveHorseStrike', $any)
$baseOnAction = $command.BaseType.GetMethod('OnAction', $any)
if ($null -ne $baseOnAction) { $baseOnAction = $baseOnAction.DeclaringType.GetMethod('OnAction', $any + [Reflection.BindingFlags]::DeclaredOnly) }
$unitAttack = GameType 'Kingmaker.UnitLogic.Commands.UnitAttack'
Check ($null -ne $onAction -and $null -ne $baseOnAction -and $unitAttack.IsAssignableFrom($command.BaseType)) 'MountedPairAttackCommand overrides OnAction above the native UnitAttack chain'
Check ((Calls $command 'OnAction' $observe -Count) -eq 1) 'OnAction calls ObserveHorseStrike exactly once'
Check ((Calls $command 'OnAction' $baseOnAction -Count) -eq 1) 'OnAction calls its base OnAction exactly once (native attack event and damage count unchanged)'
Check (Calls $command 'ObserveHorseStrike' ($kmc.GetType('KingmakerMountedCombat.Domain.HorseStrikeReader', $true).GetMethod('Read'))) 'ObserveHorseStrike reads through HorseStrikeReader'
Write-Host ('Mounted alpha 2 bite contract PASS=' + $script:passed + ' FAIL=0')
