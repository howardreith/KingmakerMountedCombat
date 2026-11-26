[CmdletBinding()]
param([string]$Configuration = 'Release')
# Mounted Combat alpha 2, MMR-07 (review R1-F13): the production Horse Bite handle binding and strike reader on genuine
# Kingmaker objects, and the compiled wiring that reaches them.
#  1. The adapter's real-type boundary (HorsePrimaryAttackAnimationAdapter.HandleBoundary) with the production decision
#     (HorseStrikeHandleBinding.Bind) on genuine UnitAnimationActionHandle / UnitAnimationActionSpecialAttack /
#     UnitAnimationManager objects created without Unity: stock adoption, exactly-once, same-action refresh, a refused
#     different-action refresh, a foreign or dead manager, hoof controls that never populate the Bite (PR-5 R1-F1), a missing
#     handle supplied once, a refused ambiguous fallback, and the once-per-command missing-handle report (PR-5 R1-F7). The
#     record/create/assign operations are the adapter's delegates, supplied here as script blocks. The Bite-limb proof
#     (IsBiteLimb) on genuine slots and weapon blueprints.
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

# One planned strike's state: the adapter's command record (with its missing-handle flag) and attack slot, as the delegates see
# them. Exact is the adapter's context, which holds only for the proven Bite limb (checked on genuine slots below).
function New-Strike($Current, $Manager, [bool]$Alive, $Actions, [bool]$Exact = $true) {
    @{ Current = $Current; Manager = $Manager; Alive = $Alive; Actions = $Actions; Exact = $Exact; MissingReported = $false;
       Recorded = $null; RecordedAction = $null; Source = $null; Records = 0; Refreshes = 0; Creates = 0; Assigns = 0 }
}
function Bind-Strike($Strike) {
    $script:s = $Strike
    $list = [Activator]::CreateInstance($listType); foreach ($a in @($Strike.Actions)) { $list.Add($a) }
    $arguments = [object[]]@(
        $Strike.Exact, $Strike.Current, $Strike.Manager, $Strike.Alive, $list,
        (As-Delegate { param($h) $null -ne $script:s.Recorded -and [object]::ReferenceEquals($script:s.Recorded, $h) } $funcHandleBool),
        (As-Delegate { $script:s.RecordedAction } $funcAction),
        (As-Delegate { $null -ne $script:s.Recorded } ([Func[bool]])),
        (As-Delegate { param($h, $a, $source) if ($null -ne $script:s.Recorded) { throw 'duplicate record' }; $script:s.Recorded = $h; $script:s.RecordedAction = $a; $script:s.Source = $source; $script:s.Records++ } $recordType),
        (As-Delegate { param($h, $a) if ($null -eq $script:s.Recorded -or [object]::ReferenceEquals($script:s.Recorded, $h) -or -not [object]::ReferenceEquals($script:s.RecordedAction, $a)) { throw 'inexact refresh' }; $script:s.Recorded = $h; $script:s.Refreshes++ } $refreshType),
        (As-Delegate { param($a) $script:s.Creates++; New-Handle $a $script:s.Manager } $createType),
        (As-Delegate { param($h) $script:s.Assigns++; $script:s.Current = $h } $assignType),
        (As-Delegate { $script:s.MissingReported } ([Func[bool]])),
        (As-Delegate { $script:s.MissingReported = $true } ([Action])))
    $boundary = $boundaryCtor.Invoke($arguments)
    $result = $bind.Invoke($null, [object[]]@($boundary))
    $Strike.LastResult = $result
    $result.Outcome.ToString()
}

$manager = New-Native $managerType
$bite = New-Action 'Bite'; $hoof = New-Action 'Hoof'; $secondBite = New-Action 'Bite'
$horseActions = @($bite, $hoof)
$stock = New-Handle $bite $manager
$strike = New-Strike $stock $manager $true @($bite, $hoof, $secondBite)
Check ((Bind-Strike $strike) -ceq 'Adopted' -and [object]::ReferenceEquals($strike.Recorded, $stock) -and $strike.Source -ceq 'stock-created' -and $strike.Assigns -eq 0 -and $strike.Creates -eq 0) 'genuine stock Bite handle of the Horse manager is adopted, not replaced'
Check ((Bind-Strike $strike) -ceq 'AlreadyRecorded' -and $strike.Records -eq 1) 'the same genuine handle is recorded exactly once'
$later = New-Handle $bite $manager; $strike.Current = $later
Check ((Bind-Strike $strike) -ceq 'Refreshed' -and [object]::ReferenceEquals($strike.Recorded, $later) -and $strike.Records -eq 1 -and $strike.Refreshes -eq 1) 'a later genuine stock handle of the same action refreshes the record'
$strike.Current = New-Handle $secondBite $manager
Check ((Bind-Strike $strike) -ceq 'RejectedRefreshAction' -and [object]::ReferenceEquals($strike.Recorded, $later)) 'a later handle playing a different Bite action is refused; the record keeps the first'
$strike.Current = New-Handle $hoof $manager
Check ((Bind-Strike $strike) -ceq 'RejectedStockMismatch' -and [object]::ReferenceEquals($strike.Recorded, $later) -and $strike.Refreshes -eq 1) 'PR-5 R1-F1: a later handle playing the Hoof action is never taken for the Bite'
$foreign = New-Strike (New-Handle $bite (New-Native $managerType)) $manager $true $horseActions
Check ((Bind-Strike $foreign) -ceq 'RejectedStockMismatch' -and $foreign.Records -eq 0) 'a genuine handle from another animation manager is refused'
$dead = New-Strike (New-Handle $bite $manager) $manager $false $horseActions
Check ((Bind-Strike $dead) -ceq 'RejectedStockMismatch' -and $dead.Records -eq 0) 'no adoption without a live Horse manager'
$unlisted = New-Strike (New-Handle (New-Action 'Bite') $manager) $manager $true $horseActions
Check ((Bind-Strike $unlisted) -ceq 'RejectedStockMismatch') 'a Bite action outside the Horse action set is refused'
# PR-5 review R1-F1, before any Bite registration: a hoof strike (not the Bite limb, so not exact) with and without its stock
# handle; a genuine Hoof handle even in an exact context; an untyped weapon whose only candidate is Hoof.
$hoofStock = New-Strike (New-Handle $hoof $manager) $manager $true $horseActions $false
$hoofMissing = New-Strike $null $manager $true $horseActions $false
$hoofInExact = New-Strike (New-Handle $hoof $manager) $manager $true $horseActions
$untypedOnlyHoof = New-Strike $null $manager $true @($hoof)
Check ((Bind-Strike $hoofStock) -ceq 'NotExact' -and (Bind-Strike $hoofMissing) -ceq 'NotExact' -and (Bind-Strike $hoofInExact) -ceq 'RejectedStockMismatch' -and
    (Bind-Strike $untypedOnlyHoof) -ceq 'RejectedCandidateCount' -and $untypedOnlyHoof.LastResult.CandidateCount -eq 0 -and
    @($hoofStock, $hoofMissing, $hoofInExact, $untypedOnlyHoof | Where-Object { $_.Records -ne 0 -or $_.Assigns -ne 0 -or $_.Creates -ne 0 }).Count -eq 0 -and
    $null -eq $hoofMissing.Current -and $null -eq $untypedOnlyHoof.Current) 'PR-5 R1-F1: a hoof with and without a stock handle, a Hoof handle and an untyped weapon with only a Hoof candidate leave zero Bite records, creations and assignments'
$missing = New-Strike $null $manager $true $horseActions
Check ((Bind-Strike $missing) -ceq 'Supplied' -and $missing.Creates -eq 1 -and $missing.Assigns -eq 1 -and $missing.Records -eq 1 -and $missing.Source -ceq 'kmc-supplied' -and [object]::ReferenceEquals($missing.Current.Action, $bite)) 'a missing stock handle gets exactly one handle of the single Bite action, assigned and recorded as kmc-supplied'
Check ((Bind-Strike $missing) -ceq 'AlreadyRecorded' -and $missing.Creates -eq 1 -and $missing.Assigns -eq 1) 'the supplied handle is not supplied again'
# Review R4-F7: after a recorded handle (adopted or supplied), a strike planned without a handle gets no fallback: no
# creation, no assignment, no second record (the record delegate throws on a duplicate, as MountedPairAttackCommand does).
$adoptedThenMissing = New-Strike (New-Handle $bite $manager) $manager $true $horseActions
[void](Bind-Strike $adoptedThenMissing); $adoptedThenMissing.Current = $null
Check ((Bind-Strike $adoptedThenMissing) -ceq 'MissingAfterRecorded' -and $adoptedThenMissing.Records -eq 1 -and $adoptedThenMissing.Creates -eq 0 -and $adoptedThenMissing.Assigns -eq 0 -and $null -eq $adoptedThenMissing.Current) 'R4-F7: adopted, then a strike without a handle: no fallback, one record'
$firstReport = $adoptedThenMissing.LastResult.FirstMissingReport; $secondOutcome = Bind-Strike $adoptedThenMissing
Check ($firstReport -and $secondOutcome -ceq 'MissingAfterRecorded' -and -not $adoptedThenMissing.LastResult.FirstMissingReport -and $adoptedThenMissing.MissingReported) 'PR-5 R1-F7: repeated missing-handle callbacks of one command report once (the flag is the command''s, set through the boundary)'
$missing.Current = $null
Check ((Bind-Strike $missing) -ceq 'MissingAfterRecorded' -and $missing.LastResult.FirstMissingReport -and $missing.Records -eq 1 -and $missing.Creates -eq 1 -and $missing.Assigns -eq 1 -and $null -eq $missing.Current) 'R4-F7: supplied, then a strike without a handle: no second fallback or record (a different command reports on its own)'
$laterHoof = New-Strike (New-Handle $bite $manager) $manager $true $horseActions
[void](Bind-Strike $laterHoof); $laterHoof.Exact = $false; $laterHoof.Current = $null
Check ((Bind-Strike $laterHoof) -ceq 'NotExact' -and $laterHoof.Records -eq 1 -and $laterHoof.Creates -eq 0 -and $laterHoof.Assigns -eq 0 -and [object]::ReferenceEquals($laterHoof.RecordedAction, $bite)) 'R4-F7: a later hoof strike without a handle after a recorded bite is left untouched'
$ambiguous = New-Strike $null $manager $true @($bite, (New-Action 'Bite'), $hoof)
Check ((Bind-Strike $ambiguous) -ceq 'RejectedCandidateCount' -and $ambiguous.LastResult.CandidateCount -eq 2 -and $ambiguous.Assigns -eq 0 -and $ambiguous.Creates -eq 0) 'two Bite actions: the fallback assigns nothing'

# PR-5 review R1-F1: the Bite-limb proof (HorsePrimaryAttackAnimationAdapter.IsBiteLimb, used by IsHorseBiteSlot for the
# binding context and the terminal observation) on genuine WeaponSlot, List<WeaponSlot> and BlueprintItemWeapon objects.
$slotType = GameType 'Kingmaker.Items.Slots.WeaponSlot'
$weaponType = GameType 'Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon'
$handBlueprintType = GameType 'Kingmaker.Blueprints.Items.Equipment.BlueprintItemEquipmentHand'
$visualType = GameType 'Kingmaker.Blueprints.Items.Weapons.WeaponVisualParameters'
function New-Weapon([string]$Special) {
    $w = New-Native $weaponType; $v = New-Native $visualType
    SetField $v $visualType 'm_SpecialAnimation' ([Enum]::Parse($attackKind, $Special))
    SetField $w $handBlueprintType 'm_VisualParameters' $v
    $w
}
$biteSlot = New-Native $slotType; $hoofSlotA = New-Native $slotType; $hoofSlotB = New-Native $slotType
$runtimeLimbs = [Activator]::CreateInstance([Collections.Generic.List`1].MakeGenericType($slotType)); foreach ($x in @($biteSlot, $hoofSlotA, $hoofSlotB)) { $runtimeLimbs.Add($x) }
$biteWeapon = New-Weapon 'Bite'; $hoofWeapon = New-Weapon 'Hoof'; $untypedWeapon = New-Weapon 'None'
$blueprintLimbs = [Array]::CreateInstance($weaponType, 3); $blueprintLimbs.SetValue($biteWeapon, 0); $blueprintLimbs.SetValue($hoofWeapon, 1); $blueprintLimbs.SetValue($hoofWeapon, 2)
$untypedLimbs = [Array]::CreateInstance($weaponType, 3); $untypedLimbs.SetValue($untypedWeapon, 0); $untypedLimbs.SetValue($hoofWeapon, 1); $untypedLimbs.SetValue($hoofWeapon, 2)
$isBiteLimb = (KmcType 'KingmakerMountedCombat.Integration.HorsePrimaryAttackAnimationAdapter').GetMethod('IsBiteLimb', $any)
function BiteLimb($Hand, $Weapon, $Limbs, $BlueprintLimbs) { [bool]$isBiteLimb.Invoke($null, [object[]]@($Hand, $Weapon, $Limbs, $BlueprintLimbs)) }
Check ((BiteLimb $biteSlot $biteWeapon $runtimeLimbs $blueprintLimbs) -and -not (BiteLimb $hoofSlotA $hoofWeapon $runtimeLimbs $blueprintLimbs) -and
    -not (BiteLimb $hoofSlotB $hoofWeapon $runtimeLimbs $blueprintLimbs) -and -not (BiteLimb $biteSlot $hoofWeapon $runtimeLimbs $blueprintLimbs) -and
    -not (BiteLimb $biteSlot $untypedWeapon $runtimeLimbs $untypedLimbs) -and -not (BiteLimb (New-Native $slotType) $biteWeapon $runtimeLimbs $blueprintLimbs) -and
    -not (BiteLimb $null $biteWeapon $runtimeLimbs $blueprintLimbs) -and -not (BiteLimb $biteSlot $biteWeapon $null $blueprintLimbs)) 'PR-5 R1-F1: only the Horse''s first limb slot carrying the blueprint''s first limb with a Bite special animation is the Bite (hooves in limb1/limb2, a hoof weapon in limb0, an untyped weapon, a foreign slot, a missing hand or body are not)'

# ---- 2. The strike reader's real-type source on a genuine handle ---------------------------------------------------
$sourceType = KmcType 'KingmakerMountedCombat.Integration.MountedPairAttackCommand+HorseStrikeSource'
$source = @($sourceType.GetConstructors($any))[0].Invoke([object[]]@($null, $null))
$readHandle = New-Handle $bite $manager
SetField $readHandle $baseHandleType '<IsStarted>k__BackingField' $true
SetField $readHandle $baseHandleType '<IsActed>k__BackingField' $true
SetField $readHandle $baseHandleType 'm_SpeedScale' ([single]2.4)
$handleType.GetProperty('Variant').SetValue($readHandle, 1, $null)
function SourceCall([string]$Name) { $sourceType.GetMethod($Name, $any).Invoke($source, [object[]]@($readHandle)) }
Check ((SourceCall 'IsStarted') -and (SourceCall 'IsActed') -and -not (SourceCall 'IsInterrupted') -and [Math]::Abs((SourceCall 'SpeedScale') - 2.4) -lt 0.0001 -and (SourceCall 'Variant') -eq 1) 'reader source states a genuine handle''s started/acted/interrupted, speedScale and variant'
Check ($null -eq (SourceCall 'ActiveState') -and (SourceCall 'ActiveWeight') -eq 0) 'reader source reports no active animation for a handle that never played'
Check ($null -eq $sourceType.GetProperty('Handle').GetValue($source, $null)) 'reader source without a planned attack reports no handle (handle=none)'
Check ($sourceType.GetProperty('Slot').GetValue($source, $null) -ceq 'none') 'PR-5 R1-F5: reader source without a planned hand reports slot=none (the slot is resolved by HorseStrikeDiagnostic.Slot against the Horse body)'

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
# PR-5 review R1-F2: SupplyExact is exactly one call of HorseStrikeHandleBinding.SupplyContained (besides closure and delegate
# construction): building the boundary, the decision and the outcome reporting all run inside it.
$supplyContained = $bindingType.GetMethod('SupplyContained')
$supplyCalls = New-Object 'System.Collections.Generic.List[string]'
$supplyIl = $supplyExact.GetMethodBody().GetILAsByteArray()
for ($i = 0; $i -le $supplyIl.Length - 5; $i++) {
    if ($supplyIl[$i] -ne 0x28 -and $supplyIl[$i] -ne 0x6F -and $supplyIl[$i] -ne 0x73) { continue }
    try { $member = $supplyExact.Module.ResolveMethod([BitConverter]::ToInt32($supplyIl, $i + 1)) } catch { continue }
    if ($member.IsGenericMethod) { $member = $member.GetGenericMethodDefinition() }
    if ($member -eq $supplyContained) { $supplyCalls.Add('SupplyContained') }
    elseif ($member.IsConstructor -and ([Delegate].IsAssignableFrom($member.DeclaringType) -or $member.DeclaringType.Name.StartsWith('<>c'))) { }
    else { $supplyCalls.Add($member.DeclaringType.Name + '.' + $member.Name) }
}
Check ($supplyCalls.Count -eq 1 -and $supplyCalls[0] -ceq 'SupplyContained' -and $supplyExact.GetMethodBody().ExceptionHandlingClauses.Count -eq 0) ('PR-5 R1-F2: SupplyExact only calls HorseStrikeHandleBinding.SupplyContained (' + ($supplyCalls -join ', ') + ')')
$containedBody = $supplyContained.GetMethodBody()
$containedCatches = @($containedBody.ExceptionHandlingClauses | Where-Object { $_.Flags -eq [Reflection.ExceptionHandlingClauseOptions]::Clause -and $_.CatchType -eq [Exception] })
$nested = @(foreach ($a in $containedCatches) { foreach ($b in $containedCatches) { if ($a -ne $b -and $b.TryOffset -ge $a.HandlerOffset -and $b.TryOffset -lt $a.HandlerOffset + $a.HandlerLength) { ,@($a, $b) } } })
$containedIl = $containedBody.GetILAsByteArray(); $bindAt = -1
for ($i = 0; $i -le $containedIl.Length - 5; $i++) {
    if ($containedIl[$i] -ne 0x28) { continue }
    try { $m = $supplyContained.Module.ResolveMethod([BitConverter]::ToInt32($containedIl, $i + 1), $null, $supplyContained.GetGenericArguments()) } catch { continue }
    if ($m.Name -ceq 'Bind' -and $m.DeclaringType -eq $bindingType) { $bindAt = $i }
}
Check ($containedCatches.Count -eq 2 -and $nested.Count -eq 1 -and $bindAt -ge $nested[0][0].TryOffset -and $bindAt -lt $nested[0][0].TryOffset + $nested[0][0].TryLength) 'PR-5 R1-F2: SupplyContained runs build, Bind and report in one catch (Exception) and the fault report in a second catch (Exception) inside its handler (a throwing logger is absorbed)'
$commandType = KmcType 'KingmakerMountedCombat.Integration.MountedPairAttackCommand'
$retained = @($adapter.GetFields([Reflection.BindingFlags]'Instance,Public,NonPublic') | Where-Object { $_.FieldType -eq $commandType -or $_.FieldType -eq [object] -or $commandType.IsAssignableFrom($_.FieldType) })
Check ($retained.Count -eq 0 -and $null -ne $commandType.GetProperty('HorsePrimaryMissingHandleReported', $any)) ('PR-5 R1-F7: the adapter holds no command reference; the report-once flag is the command''s (' + (($retained | ForEach-Object { $_.Name }) -join ', ') + ')')
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
# PR-5 review R1-F4: the strike observation uses the same Bite-limb proof, and the terminal fields come from it.
$isBiteSlot = $adapter.GetMethod('IsHorseBiteSlot', $any)
$observed = $false
$observeIl = $observe.GetMethodBody().GetILAsByteArray()
for ($i = 0; $i -le $observeIl.Length - 5; $i++) { if ($observeIl[$i] -eq 0x6F -or $observeIl[$i] -eq 0x28) { try { $m = $observe.Module.ResolveMethod([BitConverter]::ToInt32($observeIl, $i + 1)) } catch { continue }; if ($m.Name -ceq 'Observe' -and $m.DeclaringType.IsGenericType -and $m.DeclaringType.GetGenericTypeDefinition().Name -ceq 'HorseBiteObservation`1') { $observed = $true } } }
Check ((Calls $command 'ObserveHorseStrike' $isBiteSlot) -and $observed) 'PR-5 R1-F4: ObserveHorseStrike records a Bite observation only through IsHorseBiteSlot (a hoof never is one)'
Check (Calls $command 'DescribeHorseBiteTerminal' ($kmc.GetType('KingmakerMountedCombat.Domain.HorseBiteTerminal', $true).GetMethod('Describe'))) 'PR-5 R1-F4: the terminal horseBite fields come from HorseBiteTerminal.Describe over the observation'
# PR-5 review R1-F3: the command's record publishes nothing when a metadata read fails. Outside Unity the genuine action's
# name getter cannot run, which is exactly such a failure.
$recordCommand = New-Native $commandType
SetField $recordCommand $commandType 'action' ([Enum]::Parse((KmcType 'KingmakerMountedCombat.Domain.MountedCombatActionKind'), 'MountPrimaryNatural'))
$recordFault = $null
try { [void]$commandType.GetMethod('RecordHorsePrimaryAnimation', $any).Invoke($recordCommand, [object[]]@((New-Handle $bite $manager), $bite, 'kmc-supplied')) } catch { $recordFault = $_ }
$recordFields = @('horsePrimaryAnimationHandle', 'horsePrimaryAnimationAction', 'horsePrimaryAnimationHandleSource', 'horsePrimaryAnimationActionName') | ForEach-Object { $commandType.GetField($_, $any).GetValue($recordCommand) }
Check ($null -ne $recordFault -and @($recordFields | Where-Object { $null -ne $_ }).Count -eq 0) 'PR-5 R1-F3: a failing metadata read inside RecordHorsePrimaryAnimation leaves no handle, action, source or name recorded'
Write-Host ('Mounted alpha 2 bite contract PASS=' + $script:passed + ' FAIL=0 nativeProof=false')
