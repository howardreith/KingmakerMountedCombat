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
function New-Weapon([string]$Special, [string]$Guid = $null) {
    $w = New-Native $weaponType; $v = New-Native $visualType
    if ($Guid) { SetField $w (GameType 'Kingmaker.Blueprints.BlueprintScriptableObject') 'm_AssetGuid' $Guid }
    SetField $v $visualType 'm_SpecialAnimation' ([Enum]::Parse($attackKind, $Special))
    SetField $w $handBlueprintType 'm_VisualParameters' $v
    $w
}
$biteSlot = New-Native $slotType; $hoofSlotA = New-Native $slotType; $hoofSlotB = New-Native $slotType
$runtimeLimbs = [Activator]::CreateInstance([Collections.Generic.List`1].MakeGenericType($slotType)); foreach ($x in @($biteSlot, $hoofSlotA, $hoofSlotB)) { $runtimeLimbs.Add($x) }
$registeredBiteGuid = 'registered-bite-guid'
$biteWeapon = New-Weapon 'Bite' $registeredBiteGuid; $hoofWeapon = New-Weapon 'Hoof' 'hoof-guid'; $untypedWeapon = New-Weapon 'None' 'unrelated-guid'
$blueprintLimbs = [Array]::CreateInstance($weaponType, 3); $blueprintLimbs.SetValue($biteWeapon, 0); $blueprintLimbs.SetValue($hoofWeapon, 1); $blueprintLimbs.SetValue($hoofWeapon, 2)
$untypedLimbs = [Array]::CreateInstance($weaponType, 3); $untypedLimbs.SetValue($untypedWeapon, 0); $untypedLimbs.SetValue($hoofWeapon, 1); $untypedLimbs.SetValue($hoofWeapon, 2)
$isBiteLimb = (KmcType 'KingmakerMountedCombat.Integration.HorsePrimaryAttackAnimationAdapter').GetMethod('IsBiteLimb', $any)
function BiteLimb($Hand, $Weapon, $Limbs, $BlueprintLimbs, [string]$Registered = $registeredBiteGuid) { [bool]$isBiteLimb.Invoke($null, [object[]]@($Hand, $Weapon, $Limbs, $BlueprintLimbs, $Registered)) }
# PR-5 review R2-F1: the registered Bite weapon (the registered Horse body's first limb) is the Bite in its actual first-limb
# slot whatever its visual tag; an unrelated untyped weapon is not.
$untaggedBite = New-Weapon 'None' $registeredBiteGuid
$registeredUntagged = [Array]::CreateInstance($weaponType, 3); $registeredUntagged.SetValue($untaggedBite, 0); $registeredUntagged.SetValue($hoofWeapon, 1); $registeredUntagged.SetValue($hoofWeapon, 2)
Check ((BiteLimb $biteSlot $biteWeapon $runtimeLimbs $blueprintLimbs) -and (BiteLimb $biteSlot $untaggedBite $runtimeLimbs $registeredUntagged) -and
    -not (BiteLimb $hoofSlotA $hoofWeapon $runtimeLimbs $blueprintLimbs) -and
    -not (BiteLimb $hoofSlotB $hoofWeapon $runtimeLimbs $blueprintLimbs) -and -not (BiteLimb $biteSlot $hoofWeapon $runtimeLimbs $blueprintLimbs) -and
    -not (BiteLimb $biteSlot $untypedWeapon $runtimeLimbs $blueprintLimbs) -and -not (BiteLimb (New-Native $slotType) $biteWeapon $runtimeLimbs $blueprintLimbs) -and
    -not (BiteLimb $null $biteWeapon $runtimeLimbs $blueprintLimbs) -and -not (BiteLimb $biteSlot $biteWeapon $null $blueprintLimbs)) 'PR-5 R1-F1/R2-F1: the Bite is the registered Horse body''s first-limb weapon in the Horse''s first runtime limb slot, also with SpecialAnimation None; hooves in limb1/limb2, a hoof weapon in limb0, an unrelated untyped weapon, a foreign slot and missing parts are not'
# The fallback chain for the untagged registered Bite: exact from the production proof, no stock handle, one genuine Bite action:
# one supply and a Bite observation. Hoof-only candidates and an unrelated untyped weapon: zero writes.
$untaggedStrike = New-Strike $null $manager $true $horseActions (BiteLimb $biteSlot $untaggedBite $runtimeLimbs $registeredUntagged)
$hoofOnly = New-Strike $null $manager $true @($hoof) (BiteLimb $biteSlot $untaggedBite $runtimeLimbs $registeredUntagged)
$unrelated = New-Strike $null $manager $true $horseActions (BiteLimb $biteSlot $untypedWeapon $runtimeLimbs $registeredUntagged)
$observationType = (KmcType 'KingmakerMountedCombat.Domain.HorseBiteObservation`1').MakeGenericType($handleType)
$untaggedObservation = [Activator]::CreateInstance($observationType)
$untaggedOutcome = Bind-Strike $untaggedStrike
[void]$observationType.GetMethod('Observe', [Type[]]@([bool], $handleType)).Invoke($untaggedObservation, [object[]]@((BiteLimb $biteSlot $untaggedBite $runtimeLimbs $registeredUntagged), $untaggedStrike.Current))
Check ($untaggedOutcome -ceq 'Supplied' -and $untaggedStrike.Creates -eq 1 -and $untaggedStrike.Assigns -eq 1 -and $untaggedStrike.Records -eq 1 -and
    [object]::ReferenceEquals($untaggedStrike.Current.Action, $bite) -and $untaggedObservation.Kind.ToString() -ceq 'Handle' -and
    [object]::ReferenceEquals($untaggedObservation.Handle, $untaggedStrike.Current)) 'PR-5 R2-F1: the registered Bite with SpecialAnimation None, no stock handle and one genuine Bite action gets exactly one supplied handle and a Bite observation'
# PR-5 review R3-F5: a foreign weapon substituted into the blueprint's first limb and the runtime first slot is never the Bite:
# zero adoption, creation and assignment; the classification of missing inputs is Unavailable (never a positive non-Bite).
$substituted = [Array]::CreateInstance($weaponType, 3); $substituted.SetValue($hoofWeapon, 0); $substituted.SetValue($hoofWeapon, 1); $substituted.SetValue($hoofWeapon, 2)
$classify = (KmcType 'KingmakerMountedCombat.Integration.HorsePrimaryAttackAnimationAdapter').GetMethod('ClassifyBiteLimb', $any)
$substitutedStrike = New-Strike (New-Handle $bite $manager) $manager $true $horseActions (BiteLimb $biteSlot $hoofWeapon $runtimeLimbs $substituted)
Check ((Bind-Strike $substitutedStrike) -ceq 'NotExact' -and $substitutedStrike.Records -eq 0 -and $substitutedStrike.Creates -eq 0 -and $substitutedStrike.Assigns -eq 0 -and
    $classify.Invoke($null, [object[]]@($biteSlot, $hoofWeapon, $runtimeLimbs, $substituted, $registeredBiteGuid)).ToString() -ceq 'NotBite' -and
    $classify.Invoke($null, [object[]]@($biteSlot, $untaggedBite, $runtimeLimbs, $registeredUntagged, $null)).ToString() -ceq 'Unavailable' -and
    $classify.Invoke($null, [object[]]@($null, $untaggedBite, $runtimeLimbs, $registeredUntagged, $registeredBiteGuid)).ToString() -ceq 'Unavailable' -and
    $untaggedOutcome -ceq 'Supplied') 'PR-5 R3-F5: a foreign weapon substituted into the blueprint''s first limb and the runtime slot is not the Bite (no adoption, creation or assignment); missing inputs or registration are Unavailable; the original untagged Bite still gets its one fallback'
Check ((Bind-Strike $hoofOnly) -ceq 'RejectedCandidateCount' -and $hoofOnly.LastResult.CandidateCount -eq 0 -and (Bind-Strike $unrelated) -ceq 'NotExact' -and
    @($hoofOnly, $unrelated | Where-Object { $_.Records -ne 0 -or $_.Assigns -ne 0 -or $_.Creates -ne 0 }).Count -eq 0) 'PR-5 R2-F1: only a Hoof candidate, or an unrelated untyped weapon in the Bite slot, writes nothing'

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
            if ($Method -and $t -eq $Type -and $m.Name -cne $Method -and -not $m.Name.Contains('<' + $Method + '>')) { continue }
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
# PR-5 review R2-F2: the plan read, the limb proof and the strike line run only inside the contained HorseStrikeObservationStep
# (lambdas it invokes inside its own guards); ObserveHorseStrike itself never reads PlannedAttack, and OnAction has no handler that
# could swallow a native failure: its base.OnAction call is unguarded and single.
$step = (KmcType 'KingmakerMountedCombat.Domain.HorseStrikeObservationStep').GetMethod('Observe')
$plannedGetter = $unitAttack.GetProperty('PlannedAttack', $any).GetGetMethod($true)
$exactOnly = { param($Name, $Target) $n = 0; $il = $command.GetMethod($Name, $any + [Reflection.BindingFlags]::DeclaredOnly).GetMethodBody().GetILAsByteArray()
    for ($i = 0; $i -le $il.Length - 5; $i++) { if ($il[$i] -eq 0x28 -or $il[$i] -eq 0x6F) { try { $r = $command.Module.ResolveMethod([BitConverter]::ToInt32($il, $i + 1)) } catch { continue }; if ($r -is [Reflection.MethodInfo] -and $r.IsGenericMethod) { $r = $r.GetGenericMethodDefinition() }; if ($r -eq $Target) { $n++ } } }; $n }
Check ((& $exactOnly 'ObserveHorseStrike' $step) -eq 1 -and (& $exactOnly 'ObserveHorseStrike' $plannedGetter) -eq 0 -and (Calls $command 'ObserveHorseStrike' $plannedGetter) -and
    $onAction.GetMethodBody().ExceptionHandlingClauses.Count -eq 0) 'PR-5 R2-F2: ObserveHorseStrike hands the plan read, limb proof and strike line to HorseStrikeObservationStep.Observe (one call) and never reads PlannedAttack itself; OnAction swallows nothing'
# PR-5 review R1-F4: the strike observation uses the same Bite-limb proof, and the terminal fields come from it.
$isBiteSlot = $adapter.GetMethod('IsHorseBiteSlot', $any)
$observed = $false; $observedUnreadable = $false
$stepIl = $step.GetMethodBody().GetILAsByteArray()
for ($i = 0; $i -le $stepIl.Length - 5; $i++) { if ($stepIl[$i] -eq 0x6F -or $stepIl[$i] -eq 0x28) { try { $m = $step.Module.ResolveMethod([BitConverter]::ToInt32($stepIl, $i + 1), $null, $step.GetGenericArguments()) } catch { continue }
    if ($m.DeclaringType.IsGenericType -and $m.DeclaringType.GetGenericTypeDefinition().Name -ceq 'HorseBiteObservation`1') { if ($m.Name -ceq 'Observe') { $observed = $true } elseif ($m.Name -ceq 'ObserveUnreadable') { $observedUnreadable = $true } } } }
$observed = $observed -and $observedUnreadable
Check ((Calls $command 'ObserveHorseStrike' ($adapter.GetMethod('ClassifyHorseStrike', $any))) -and -not (Calls $command 'ObserveHorseStrike' $isBiteSlot) -and (Calls $adapter 'IsHorseBiteSlot' ($adapter.GetMethod('ClassifyHorseStrike', $any))) -and $observed) 'PR-5 R1-F4/R2-F2: ObserveHorseStrike classifies through ClassifyHorseStrike (Bite, not-Bite or unavailable; binding uses its Bite result through IsHorseBiteSlot) and its observation step records Observe or ObserveUnreadable'
Check (Calls $command 'DescribeHorseBiteTerminal' ($kmc.GetType('KingmakerMountedCombat.Domain.HorseBiteTerminal', $true).GetMethod('Describe'))) 'PR-5 R1-F4: the terminal horseBite fields come from HorseBiteTerminal.Describe over the observation'
# PR-5 review R1-F3: the command's record publishes nothing when a metadata read fails. Outside Unity the genuine action's
# name getter cannot run, which is exactly such a failure.
# PR-5 review R2-F5: the action gets a nonzero cached native pointer, so Unity's equality reads it as live and the production
# precondition (animationAction == null) passes; the first metadata read (UnityEngine.Object.name) then fails outside the engine.
# The check asserts that the fault came from that read, not from the precondition, and that nothing was published.
$recordCommand = New-Native $commandType
SetField $recordCommand $commandType 'action' ([Enum]::Parse((KmcType 'KingmakerMountedCombat.Domain.MountedCombatActionKind'), 'MountPrimaryNatural'))
$liveBite = New-Action 'Bite'
$unityBase = $actionType; while ($unityBase.FullName -ne 'UnityEngine.Object') { $unityBase = $unityBase.BaseType }
$unityBase.GetField('m_CachedPtr', [Reflection.BindingFlags]'Instance,NonPublic').SetValue($liveBite, [IntPtr]4242)
$unityEquality = $unityBase.GetMethod('op_Equality', [Reflection.BindingFlags]'Static,Public')
$passesPrecondition = -not [bool]$unityEquality.Invoke($null, [object[]]@($liveBite, $null))
$recordFault = $null
try { [void]$commandType.GetMethod('RecordHorsePrimaryAnimation', $any).Invoke($recordCommand, [object[]]@((New-Handle $liveBite $manager), $liveBite, 'kmc-supplied')) } catch { $recordFault = $_.Exception }
$inner = $recordFault; while ($null -ne $inner -and $null -ne $inner.InnerException -and ($inner -is [Reflection.TargetInvocationException] -or $inner -is [Management.Automation.MethodInvocationException])) { $inner = $inner.InnerException }
# The fault is Unity's unavailable internal call (SecurityException, ECall), raised at the first engine call after the
# precondition; in the compiled method that call is UnityEngine.Object.get_name and it precedes every record write, so moving a
# write ahead of it publishes a field before the fault (the field check below then fails).
$recordMethod = $commandType.GetMethod('RecordHorsePrimaryAnimation', $any)
$recordIl = $recordMethod.GetMethodBody().GetILAsByteArray(); $nameAt = -1; $firstWrite = [int]::MaxValue
$recordFieldNames = @('horsePrimaryAnimationHandle', 'horsePrimaryAnimationAction', 'horsePrimaryAnimationHandleSource', 'horsePrimaryAnimationActionName', 'horsePrimaryAnimationActionType')
for ($i = 0; $i -le $recordIl.Length - 5; $i++) {
    if ($recordIl[$i] -eq 0x28 -or $recordIl[$i] -eq 0x6F) { try { $m = $recordMethod.Module.ResolveMethod([BitConverter]::ToInt32($recordIl, $i + 1)) } catch { continue }
        if ($nameAt -lt 0 -and $m.DeclaringType.FullName -ceq 'UnityEngine.Object' -and $m.Name -notmatch '^op_') { if ($m.Name -ceq 'get_name') { $nameAt = $i } else { $nameAt = -2 } } }
    elseif ($recordIl[$i] -eq 0x7D) { try { $f = $recordMethod.Module.ResolveField([BitConverter]::ToInt32($recordIl, $i + 1)) } catch { continue }; if ($recordFieldNames -contains $f.Name -and $i -lt $firstWrite) { $firstWrite = $i } }
}
$atNameRead = $null -ne $inner -and $inner -is [Security.SecurityException] -and $inner.Message -match 'ECall' -and $nameAt -ge 0 -and $nameAt -lt $firstWrite
$recordFields = $recordFieldNames | ForEach-Object { $commandType.GetField($_, $any).GetValue($recordCommand) }
Write-Host ('  R2-F5 metadata fault: precondition passed=' + $passesPrecondition + '; fault=' + $(if ($inner) { $inner.GetType().FullName + ': ' + $inner.Message } else { '<none>' }) + '; first engine call=get_name at IL ' + $nameAt + '; first record write at IL ' + $firstWrite)
Check ($passesPrecondition -and $atNameRead -and @($recordFields | Where-Object { $null -ne $_ }).Count -eq 0) 'PR-5 R1-F3/R2-F5: past the production precondition (a live-pointer action), the metadata read (UnityEngine.Object.name) fails and RecordHorsePrimaryAnimation publishes no handle, action, source, name or type'
# PR-5 reviews R2-F3/R1-F4: both terminal outcomes come from HorseTerminalOutcome.Compose, which attaches the Bite disposition and
# the observed playback projection (AttachHorseBite); DescribeOutcome no longer sets them from the registration; the scenario's
# playback checks judge the observed projection through HorseBiteTerminal.PlaybackAccepted.
$compose = (KmcType 'KingmakerMountedCombat.Domain.HorseTerminalOutcome').GetMethod('Compose')
$project = (KmcType 'KingmakerMountedCombat.Domain.HorseBiteTerminal').GetMethod('Project')
$outcomeType = KmcType 'KingmakerMountedCombat.Integration.MountedPairAttackOutcome'
$setBite = $outcomeType.GetProperty('HorseBiteTerminal').GetSetMethod(); $setCreated = $outcomeType.GetProperty('AttackAnimationHandleCreated').GetSetMethod()
$engine = KmcType 'KingmakerMountedCombat.Diagnostics.HorseCompanionUnmountedScenarioEngine'
$accepted = (KmcType 'KingmakerMountedCombat.Domain.HorseBiteTerminal').GetMethod('PlaybackAccepted', [Type[]]@([bool], [string], [bool], [string]))
Check ((& $exactOnly 'ReportTerminalOnce' $compose) -eq 1 -and (Calls $command 'AttachHorseBite' $setBite) -and (Calls $command 'AttachHorseBite' $setCreated) -and
    (Calls $command 'DescribeHorseBitePlayback' $project) -and -not (Calls $command 'DescribeOutcome' $setBite) -and -not (Calls $command 'DescribeOutcome' $setCreated) -and
    (Calls $engine $null $accepted -Count) -ge 2 -and $null -eq $engine.GetMethod('IsExactHorsePrimaryAnimationSource', $any)) 'PR-5 R2-F3/R1-F4: one composer attaches the Bite disposition and observed playback to the detailed and the minimal outcome; the scenario playback checks use HorseBiteTerminal.PlaybackAccepted on it'
# PR-5 reviews R2-F3/R3-F3 (round 4): the command's own terminal delivery. Genuine, detached MountedPairAttackCommand objects (an
# exact KMC Horse by its unit blueprint GUID, or another unit) run the production ReportTerminalOnce; the terminal callback captures
# the actual MountedPairAttackOutcome. Without a transaction the detailed description throws (an unrelated read), so the minimal
# outcome is delivered; with one, the detailed outcome is. The scenario decision is the production four-argument PlaybackAccepted.
$entityType = GameType 'Kingmaker.EntitySystem.Entities.UnitEntityData'
$entityBase = GameType 'Kingmaker.EntitySystem.EntityDataBase'
$descriptorType = GameType 'Kingmaker.UnitLogic.UnitDescriptor'
$unitBlueprintType = GameType 'Kingmaker.Blueprints.BlueprintUnit'
$scriptableType = GameType 'Kingmaker.Blueprints.BlueprintScriptableObject'
$horseUnitGuid = (KmcType 'KingmakerMountedCombat.Integration.HorseCompanionBlueprintService').GetField('UnitGuid', $any).GetValue($null)
function New-Unit([string]$Guid, [string]$Id) {
    $bp = New-Native $unitBlueprintType; SetField $bp $scriptableType 'm_AssetGuid' $Guid
    $d = New-Native $descriptorType; SetField $d $descriptorType '<Blueprint>k__BackingField' $bp
    $u = New-Native $entityType; SetField $u $entityType '<Descriptor>k__BackingField' $d; SetField $u $entityBase '<UniqueId>k__BackingField' $Id; $u
}
$terminalType = [Action`2].MakeGenericType($commandType, $outcomeType)
$transactionType = KmcType 'KingmakerMountedCombat.Domain.MountedCombatTransaction'
$reportTerminal = $commandType.GetMethod('ReportTerminalOnce', $any)
function Deliver([string]$Guid, $Observed, [string]$Source, $Registered, [bool]$Detailed, $Observation = $null) {
    $c = New-Native $commandType
    SetField $c $commandType 'action' ([Enum]::Parse((KmcType 'KingmakerMountedCombat.Domain.MountedCombatActionKind'), 'MountPrimaryNatural'))
    $u = New-Unit $Guid 'horse-1'; SetField $c $commandType 'mount' $u; SetField $c $commandType 'actionActor' $u
    $obs = [Activator]::CreateInstance($observationType)
    if ($null -ne $Observed) { [void]$observationType.GetMethod('Observe', [Type[]]@([bool], $handleType, [string])).Invoke($obs, [object[]]@($true, $Observed, $Source)) }
    if ($null -ne $Observation) { $obs = $Observation }
    SetField $c $commandType 'horseBiteObservation' $obs
    if ($null -ne $Registered) {
        SetField $c $commandType 'horsePrimaryAnimationHandle' $Registered; SetField $c $commandType 'horsePrimaryAnimationHandleSource' 'stock-created'
        SetField $c $commandType 'horsePrimaryAnimationActionName' 'HorseAnimationSet_Bite'; SetField $c $commandType 'horsePrimaryAnimationActionType' 'SpecialAttack'
    }
    if ($Detailed) {
        SetField $c $commandType 'transaction' (New-Native $transactionType); SetField $c $commandType 'attackTarget' (New-Unit 'target-guid' 'kobold-1')
        SetField $c $commandType 'continuationTargetIds' (New-Object 'System.Collections.Generic.List[string]'); SetField $c $commandType 'repathObservations' (New-Object 'System.Collections.Generic.List[string]')
        SetField $c $commandType 'm_AllAttacks' ([Activator]::CreateInstance([Collections.Generic.List`1].MakeGenericType((GameType 'Kingmaker.UnitLogic.Commands.AttackHandInfo'))))
    }
    $script:delivered = New-Object System.Collections.ArrayList
    SetField $c $commandType 'terminal' (As-Delegate { param($cmd, $o) [void]$script:delivered.Add($o) } $terminalType)
    [void]$reportTerminal.Invoke($c, $null); [void]$reportTerminal.Invoke($c, $null)
    , @($script:delivered.ToArray())
}
$acceptedFour = (KmcType 'KingmakerMountedCombat.Domain.HorseBiteTerminal').GetMethod('PlaybackAccepted', [Type[]]@([bool], [string], [bool], [string]))
function Accepted($o) { [bool]$acceptedFour.Invoke($null, [object[]]@($o.AttackAnimationHandleCreated, $o.AttackAnimationHandleSource, $o.AttackAnimationInterrupted, $o.AttackAnimationFactsUnreadable)) }
$registeredHandle = New-Handle $bite $manager
$minimal = Deliver $horseUnitGuid $registeredHandle 'stock-created' $registeredHandle $false
$detailed = Deliver $horseUnitGuid $registeredHandle 'stock-created' $registeredHandle $true
$nonHorse = Deliver 'not-the-horse' $registeredHandle 'stock-created' $registeredHandle $false
Write-Host ('  R2-F3 delivery: minimal=' + $minimal.Count + ' actor=' + $minimal[0].ActorId + ' bite=' + $minimal[0].HorseBiteTerminal + ' | detailed=' + $detailed.Count + ' actor=' + $detailed[0].ActorId + ' bite=' + $detailed[0].HorseBiteTerminal + ' | nonHorse bite=' + [string]$nonHorse[0].HorseBiteTerminal)
Check ($minimal.Count -eq 1 -and $null -eq $minimal[0].ActorId -and [string]$minimal[0].HorseBiteTerminal -like '{observed=handle,action=HorseAnimationSet_Bite,source=stock-created*' -and
    $minimal[0].AttackAnimationObservation -ceq 'handle' -and $minimal[0].AttackAnimationHandleCreated -and $minimal[0].AttackAnimationHandleSource -ceq 'stock-created' -and (Accepted $minimal[0]) -and
    $detailed.Count -eq 1 -and $detailed[0].ActorId -ceq 'horse-1' -and [string]$detailed[0].HorseBiteTerminal -ceq [string]$minimal[0].HorseBiteTerminal -and (Accepted $detailed[0]) -and
    $nonHorse.Count -eq 1 -and $null -eq $nonHorse[0].HorseBiteTerminal -and $null -eq $nonHorse[0].AttackAnimationObservation -and -not $nonHorse[0].AttackAnimationHandleCreated -and $nonHorse[0].AttackAnimationHandleRegistered) 'PR-5 R2-F3: the command''s ReportTerminalOnce delivers one outcome carrying the observed Bite on the minimal path (detailed read failed) and on the detailed path; the non-Horse control carries none'
# R3-F3: an observed handle whose action-name read fails at the terminal (outside the engine the genuine action's name is an
# unavailable internal call): the delivered outcome states it and the scenario decision rejects it.
$strayHandle = New-Handle $bite $manager
$unreadable = Deliver $horseUnitGuid $strayHandle 'stock-created' $registeredHandle $false
Write-Host ('  R3-F3 delivery: factsUnreadable=' + $unreadable[0].AttackAnimationFactsUnreadable + ' source=' + $unreadable[0].AttackAnimationHandleSource + ' accepted=' + (Accepted $unreadable[0]))
Check ($unreadable.Count -eq 1 -and $unreadable[0].AttackAnimationHandleCreated -and -not [string]::IsNullOrEmpty($unreadable[0].AttackAnimationFactsUnreadable) -and
    $unreadable[0].AttackAnimationHandleSource -ceq 'stock-created' -and -not (Accepted $unreadable[0]) -and (Accepted $minimal[0])) 'PR-5 R3-F3: a delivered outcome whose observed handle facts could not be read carries the failure and the scenario playback decision rejects it; the readable positive passes'
# PR-5 review R3-F1 (round 4): the schema-8 wire format. The production strict reader's property assertion
# (Assert-KmcExactProperties, taken from scripts/runtime/RuntimeHarness.Common.ps1 by the PowerShell parser) and its schema-8 Horse
# outcome name list judge the keys the compiled writer (HorseCompanionUnmountedScenarioEngine.CaptureMountedOutcome) emits.
$commonPath = Join-Path $repoRoot 'scripts\runtime\RuntimeHarness.Common.ps1'
$commonAst = [Management.Automation.Language.Parser]::ParseFile($commonPath, [ref]$null, [ref]$null)
$exactFn = @($commonAst.FindAll({ param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -ceq 'Assert-KmcExactProperties' }, $true))[0]
. ([scriptblock]::Create($exactFn.Extent.Text))
$commonText = [IO.File]::ReadAllText($commonPath)
$namesMatch = [regex]::Match($commonText, '\$outcomeNames = @\(([^)]*)\)')
$readerNames = @([regex]::Matches($namesMatch.Groups[1].Value, "'([A-Za-z]+)'") | ForEach-Object { $_.Groups[1].Value }) + @('attackAnimationHandleSource')
$capture = $engine.GetMethod('CaptureMountedOutcome', $any)
$captureIl = $capture.GetMethodBody().GetILAsByteArray(); $writerNames = New-Object 'System.Collections.Generic.List[string]'
for ($i = 0; $i -le $captureIl.Length - 5; $i++) { if ($captureIl[$i] -eq 0x72) { try { $lit = $capture.Module.ResolveString([BitConverter]::ToInt32($captureIl, $i + 1)) } catch { continue }; if ($lit -cmatch '^[a-z][A-Za-z]+$') { $writerNames.Add($lit) } } }
function Shaped([string[]]$Names) { $o = [ordered]@{}; foreach ($n in $Names) { $o[$n] = $null }; [pscustomobject]$o }
$candidateAccepted = $true; try { Assert-KmcExactProperties (Shaped @($writerNames)) $readerNames 'candidate Horse outcome' } catch { $candidateAccepted = $false }
$historical = @('action','actorId','commandOwnerId','resourceOwnerId','targetId','result','childAttackStartCount','repathCount','attackWeaponBlueprintId',
    'attackWeaponIsNatural','attackWeaponIsRanged','attackWeaponSlot','delegatedMoveExecutorId','delegatedMoveExecutorIsExactMount','riderStandardCharged',
    'actionStandardCharged','terminalReason','attackAnimationHandleCreated','attackAnimationActionName','attackAnimationActionType','attackAnimationActed',
    'attackAnimationFinished','attackAnimationInterrupted','attackAnimationHandleSource')
$historicalAccepted = $true; try { Assert-KmcExactProperties (Shaped $historical) $readerNames 'historical schema-8 Horse outcome' } catch { $historicalAccepted = $false }
$extraRejected = $false; try { Assert-KmcExactProperties (Shaped (@($writerNames) + 'attackAnimationObservation')) $readerNames 'expanded outcome' } catch { $extraRejected = $true }
Write-Host ('  R3-F1 wire: writer=' + $writerNames.Count + ' reader=' + $readerNames.Count + ' candidate=' + $candidateAccepted + ' historical=' + $historicalAccepted + ' extraRejected=' + $extraRejected)
Check ($writerNames.Count -eq 24 -and $readerNames.Count -eq 24 -and $candidateAccepted -and $historicalAccepted -and $extraRejected) 'PR-5 R3-F1: the compiled Horse outcome writer emits exactly the schema-8 keys the production strict reader requires; a historical schema-8 outcome still passes and an unexpected field still fails'
# PR-5 review R3-F2 (round 4): the explicit Mount Primary row and its reader. The reader's playback clauses are taken verbatim from
# scripts/runtime/RuntimeHarness.Common.ps1 (Phase 3E TB Horse mount-primary) and evaluated on outcomes; the C# row uses the
# production four-argument decision (with Acted and Finished). An acted, finished, uninterrupted handle that binding rejected (its
# observed source is unrecorded) fails both; the registered Bite with the same flags passes; a historical artifact (registration
# source, no readability field) passes; an unreadable fact fails.
$clauses = [regex]::Match($commonText, '(?s)(\$outcome\.attackAnimationHandleCreated -ne \$true -or \$outcome\.attackAnimationActed -ne \$true -or.*?attackAnimationFactsUnreadable\)\) -or)')
$readerRejects = [scriptblock]::Create('param($outcome) (' + $clauses.Groups[1].Value + ' $false)')
function P3([string]$Source, $Unreadable = $null, [switch]$Historical) {
    $o = [ordered]@{ attackAnimationHandleCreated = $true; attackAnimationActed = $true; attackAnimationFinished = $true; attackAnimationInterrupted = $false; attackAnimationHandleSource = $Source }
    if (-not $Historical) { $o.attackAnimationFactsUnreadable = $Unreadable }
    [pscustomobject]$o
}
$phase3d = KmcType 'KingmakerMountedCombat.Diagnostics.Phase3dHorseScenarioTranche'
function RowAccepts($o) { [bool]$acceptedFour.Invoke($null, [object[]]@($o.attackAnimationHandleCreated, $o.attackAnimationHandleSource, $o.attackAnimationInterrupted, $o.attackAnimationFactsUnreadable)) -and $o.attackAnimationActed -and $o.attackAnimationFinished }
$rejectedHandle = P3 'unrecorded'; $registered = P3 'stock-created'; $historicalRow = P3 'kmc-supplied' -Historical; $oldestRow = [pscustomobject][ordered]@{ attackAnimationHandleCreated = $true; attackAnimationActed = $true; attackAnimationFinished = $true; attackAnimationInterrupted = $false }; $unreadableRow = P3 'stock-created' 'SecurityException'
Write-Host ('  R3-F2 row/reader: rejected=' + (RowAccepts $rejectedHandle) + '/' + (& $readerRejects $rejectedHandle) + ' registered=' + (RowAccepts $registered) + '/' + (& $readerRejects $registered) + ' historical=' + (& $readerRejects $historicalRow) + ' unreadable=' + (& $readerRejects $unreadableRow))
Check ($clauses.Success -and (Calls $phase3d $null $acceptedFour) -and -not (RowAccepts $rejectedHandle) -and (& $readerRejects $rejectedHandle) -and
    (RowAccepts $registered) -and -not (& $readerRejects $registered) -and -not (& $readerRejects $historicalRow) -and -not (& $readerRejects $oldestRow) -and (& $readerRejects $unreadableRow) -and
    -not (RowAccepts $unreadableRow)) 'PR-5 R3-F2: the explicit Mount Primary row (production four-argument decision) and its reader reject an acted, finished, uninterrupted handle binding rejected (unrecorded) and an unreadable fact; the registered Bite and historical artifacts (without the readability field, or without any source) pass under strict mode'
# ---- 5. PR-5 round 5 ---------------------------------------------------------------------------------------------------
# R2-F2: after a known Bite observation, the production classifier (ClassifyBiteLimb) on nonempty collections whose required first
# entry is missing, and on a planned weapon without identity, is Unavailable; through the production observation step
# (HorseStrikeObservationStep.Observe, the command's contained observation) that is one identifiable unreadable strike line, the
# earlier handle and source cleared, and terminal playback rejected by the production decision. The confirmed hoof stays NotBite
# and leaves the earlier Bite standing.
$nullFirstRuntime = [Activator]::CreateInstance([Collections.Generic.List`1].MakeGenericType($slotType)); foreach ($x in @($null, $hoofSlotA, $hoofSlotB)) { $nullFirstRuntime.Add($x) }
$nullFirstBlueprint = [Array]::CreateInstance($weaponType, 3); $nullFirstBlueprint.SetValue($hoofWeapon, 1); $nullFirstBlueprint.SetValue($hoofWeapon, 2)
$noIdentityBite = New-Weapon 'Bite'
$noIdentityLimbs = [Array]::CreateInstance($weaponType, 3); $noIdentityLimbs.SetValue($noIdentityBite, 0); $noIdentityLimbs.SetValue($hoofWeapon, 1); $noIdentityLimbs.SetValue($hoofWeapon, 2)
function Classify($Hand, $Weapon, $Limbs, $BlueprintLimbs) { $classify.Invoke($null, [object[]]@($Hand, $Weapon, $Limbs, $BlueprintLimbs, $registeredBiteGuid)).ToString() }
$classes = @((Classify $biteSlot $biteWeapon $nullFirstRuntime $blueprintLimbs), (Classify $biteSlot $biteWeapon $runtimeLimbs $nullFirstBlueprint),
    (Classify $biteSlot $noIdentityBite $runtimeLimbs $noIdentityLimbs), (Classify $hoofSlotA $hoofWeapon $runtimeLimbs $blueprintLimbs), (Classify $biteSlot $biteWeapon $runtimeLimbs $blueprintLimbs))
$biteClassType = KmcType 'KingmakerMountedCombat.Domain.HorseBiteClass'
$step = (KmcType 'KingmakerMountedCombat.Domain.HorseStrikeObservationStep').GetMethod('Observe').MakeGenericMethod([object], $handleType)
function Step($Observation, [scriptblock]$ClassifyPlan) {
    $script:stepLines = New-Object System.Collections.ArrayList
    [void]$step.Invoke($null, [object[]]@($Observation,
        (As-Delegate { [object]'plan' } ([Func`1].MakeGenericType([object]))),
        (As-Delegate $ClassifyPlan ([Func`2].MakeGenericType([object], $biteClassType))),
        (As-Delegate { param($p) $null } ([Func`2].MakeGenericType([object], $handleType))),
        (As-Delegate { param($h) 'stock-created' } ([Func`2].MakeGenericType($handleType, [string]))),
        (As-Delegate { param($p) 'Horse strike: horse=horse-1; slot=limb0.' } ([Func`2].MakeGenericType([object], [string]))),
        (As-Delegate { 'horse-1' } ([Func`1].MakeGenericType([string]))),
        (As-Delegate { param($l) [void]$script:stepLines.Add($l) } ([Action`1].MakeGenericType([string])))))
}
$knownHandle = New-Handle $bite $manager
$observeKnown = $observationType.GetMethod('Observe', [Type[]]@([bool], $handleType, [string]))
$missingLimb = [Activator]::CreateInstance($observationType); [void]$observeKnown.Invoke($missingLimb, [object[]]@($true, $knownHandle, 'stock-created'))
Step $missingLimb { param($p) [Enum]::Parse($biteClassType, (Classify $biteSlot $biteWeapon $nullFirstRuntime $blueprintLimbs)) }
$missingLines = @($script:stepLines.ToArray())
$confirmedHoof = [Activator]::CreateInstance($observationType); [void]$observeKnown.Invoke($confirmedHoof, [object[]]@($true, $knownHandle, 'stock-created'))
Step $confirmedHoof { param($p) [Enum]::Parse($biteClassType, (Classify $hoofSlotA $hoofWeapon $runtimeLimbs $blueprintLimbs)) }
$hoofLines = @($script:stepLines.ToArray())
$missingTerminal = Deliver $horseUnitGuid $null $null $knownHandle $false $missingLimb
Write-Host ('  R2-F2 r5: classes=' + ($classes -join ',') + ' missing=' + $missingLimb.Kind + '/' + $missingLimb.UnreadableReason + ' line=' + ($missingLines -join ' | ') + ' terminal=' + $missingTerminal[0].HorseBiteTerminal + ' accepted=' + (Accepted $missingTerminal[0]) + ' hoof=' + $confirmedHoof.Kind)
Check (($classes -join ',') -ceq 'Unavailable,Unavailable,Unavailable,NotBite,Bite' -and
    $missingLimb.Kind.ToString() -ceq 'Unreadable' -and $missingLimb.UnreadableReason -ceq 'limb:unavailable' -and $null -eq $missingLimb.Handle -and $null -eq $missingLimb.Source -and
    $missingLines.Count -eq 1 -and $missingLines[0] -like 'Horse strike: horse=horse-1*biteObservation=unreadable(limb:unavailable)*' -and
    $missingTerminal.Count -eq 1 -and [string]$missingTerminal[0].HorseBiteTerminal -like '{observed=unreadable(limb:unavailable)*' -and -not (Accepted $missingTerminal[0]) -and
    $confirmedHoof.Kind.ToString() -ceq 'Handle' -and [object]::ReferenceEquals($confirmedHoof.Handle, $knownHandle) -and $hoofLines.Count -eq 1 -and -not ($hoofLines[0] -like '*unreadable*')) 'PR-5 R2-F2: after a known Bite, a nonempty limb collection with a null required first entry (runtime or registered) or a planned weapon without identity is Unavailable: one identifiable unreadable strike line, the earlier handle and source cleared, terminal playback rejected; a confirmed hoof is NotBite and keeps the earlier Bite'

# R4-F3: the registered Bite identity follows successful registration. The compiled Register publishes it only after the selection
# transaction and the registered-contract check (the property setter's only call in Register follows ValidateRegisteredContract),
# and the production rollback withdraws it before any removal; invoked on a service holding a published identity, the rollback
# leaves the classifier without one (Unavailable), while the published identity admits the original untagged Bite.
$serviceType = KmcType 'KingmakerMountedCombat.Integration.HorseCompanionBlueprintService'
$biteProperty = $serviceType.GetProperty('RegisteredBiteGuid', $any); $biteSetter = $biteProperty.GetSetMethod($true)
function Offsets([Reflection.MethodBase]$Method, [Reflection.MethodBase]$Target) {
    $il = $Method.GetMethodBody().GetILAsByteArray(); $at = @()
    for ($i = 0; $i -le $il.Length - 5; $i++) { if ($il[$i] -eq 0x28 -or $il[$i] -eq 0x6F) { try { if ($Method.Module.ResolveMethod([BitConverter]::ToInt32($il, $i + 1)) -eq $Target) { $at += $i } } catch { } } }
    , $at
}
$register = $serviceType.GetMethod('Register', $any); $validate = $serviceType.GetMethod('ValidateRegisteredContract', $any); $applyDesired = $serviceType.GetMethod('ApplySelectionDesired', $any)
$rollback = $serviceType.GetMethod('RollBackPartialRegistration', $any)
$publishAt = Offsets $register $biteSetter; $validateAt = Offsets $register $validate; $desiredAt = Offsets $register $applyDesired; $withdrawAt = Offsets $rollback $biteSetter
$untaggedGuid = [string](GameType 'Kingmaker.Blueprints.BlueprintScriptableObject').GetProperty('AssetGuid').GetValue($untaggedBite, $null)
$untaggedPublished = $classify.Invoke($null, [object[]]@($biteSlot, $untaggedBite, $runtimeLimbs, $registeredUntagged, $untaggedGuid)).ToString()
$previousIdentity = $biteProperty.GetValue($null, $null)
$rolledBack = $null; $rollbackFault = $null
try {
    [void]$biteSetter.Invoke($null, [object[]]@($untaggedGuid))
    $service = New-Native $serviceType
    try { [void]$rollback.Invoke($service, [object[]]@($null)) } catch { $rollbackFault = $_.Exception.GetBaseException().GetType().Name }
    $rolledBack = $biteProperty.GetValue($null, $null)
}
finally { [void]$biteSetter.Invoke($null, [object[]]@($previousIdentity)) }
$afterRollback = $classify.Invoke($null, [object[]]@($biteSlot, $untaggedBite, $runtimeLimbs, $registeredUntagged, $rolledBack)).ToString()
Write-Host ('  R4-F3: publish@' + ($publishAt -join ',') + ' desired@' + ($desiredAt -join ',') + ' validate@' + ($validateAt -join ',') + ' withdraw@' + ($withdrawAt -join ',') + ' rolledBack=' + [string]$rolledBack + ' (rollback remainder ' + [string]$rollbackFault + ') classify after rollback=' + $afterRollback + ' published=' + $untaggedPublished)
Check ($publishAt.Count -eq 1 -and $validateAt.Count -eq 1 -and $desiredAt.Count -ge 1 -and $publishAt[0] -gt $validateAt[0] -and $publishAt[0] -gt ($desiredAt | Measure-Object -Maximum).Maximum -and
    $withdrawAt.Count -eq 1 -and $null -eq $rolledBack -and $afterRollback -ceq 'Unavailable' -and $untaggedPublished -ceq 'Bite') 'PR-5 R4-F3: Register publishes the Bite identity only after the selection transaction and the registered-contract check; the production rollback withdraws it, leaving the classifier Unavailable; a published identity admits the original untagged Bite'

# R4-F2: a KMC-supplied Bite is observed at its strike, the registration then refreshes to a different stock handle of the same
# action, and the command terminates before another Bite acts. The production ReportTerminalOnce output passes through the compiled
# schema-8 writer (CaptureMountedOutcome) and, with the animation snapshot of that registration, through the production schema-8
# reader clause (taken from scripts/runtime/RuntimeHarness.Common.ps1); a historical positive (equal sources) still passes and an
# unregistered observation (unrecorded) is still rejected.
$capture = $engine.GetMethod('CaptureMountedOutcome', $any)
$suppliedHandle = New-Handle $bite $manager; $refreshedStock = New-Handle $bite $manager
$refreshed = Deliver $horseUnitGuid $suppliedHandle 'kmc-supplied' $refreshedStock $false
$historicalOutcome = Deliver $horseUnitGuid $registeredHandle 'stock-created' $registeredHandle $false
$unrecordedOutcome = Deliver $horseUnitGuid $suppliedHandle 'unrecorded' $refreshedStock $false
function Wire($o) { $j = $capture.Invoke($null, [object[]]@($o, $true)); $j.ToString() | ConvertFrom-Json }
function Snapshot([string]$Last, [int]$Created, [int]$Adopted) { [pscustomobject][ordered]@{ delegatedLocomotionRestoreCount = 0; lastDelegatedLocomotionSource = 'none'; lastDelegatedLocomotionSpeed = 0;
    horsePrimaryHandleCreateCount = $Created; horsePrimaryHandleAdoptCount = $Adopted; horsePrimaryHandleRejectCount = 0; lastHorsePrimaryHandleSource = $Last;
    lastHorsePrimaryActionName = 'HorseAnimationSet_Bite'; lastHorsePrimaryActionType = 'SpecialAttack' } }
$schema8 = [regex]::Match($commonText, "(?s)if \(\[string\]\`$o\.mountedTurnHorseOutcome\.attackAnimationHandleSource -cnotin .*?\) \{\s*throw 'PASS Horse native animation did not prove")
$schema8Clause = $schema8.Value.Substring(4, $schema8.Value.LastIndexOf(') {') - 4)
$schema8Rejects = [scriptblock]::Create('param($o) (' + $schema8Clause + ')')
function Schema8($Outcome, $Snapshot) { [pscustomobject]@{ mountedTurnHorseOutcome = $Outcome; mountedHorseOutcome = $Outcome; mountedTurnHorseAnimation = $Snapshot[0]; mountedHorseAnimation = $Snapshot[1] } }
$refreshedWire = Wire $refreshed[0]
$refreshedVerdict = & $schema8Rejects (Schema8 $refreshedWire @((Snapshot 'stock-created' 1 0), (Snapshot 'stock-created' 1 1)))
$historicalVerdict = & $schema8Rejects (Schema8 (Wire $historicalOutcome[0]) @((Snapshot 'stock-created' 0 1), (Snapshot 'stock-created' 0 2)))
$unrecordedVerdict = & $schema8Rejects (Schema8 (Wire $unrecordedOutcome[0]) @((Snapshot 'stock-created' 1 0), (Snapshot 'stock-created' 1 1)))
Write-Host ('  R4-F2: refreshed outcome source=' + $refreshedWire.attackAnimationHandleSource + ' registration=' + $refreshed[0].AttackAnimationRegisteredSource + ' rejected=' + $refreshedVerdict + ' | historical rejected=' + $historicalVerdict + ' | unrecorded rejected=' + $unrecordedVerdict)
Check ($schema8.Success -and $refreshedWire.attackAnimationHandleSource -ceq 'kmc-supplied' -and $refreshed[0].AttackAnimationRegisteredSource -ceq 'stock-created' -and
    -not $refreshedVerdict -and -not $historicalVerdict -and $unrecordedVerdict) 'PR-5 R4-F2: the schema-8 reader credits the KMC-supplied Bite observed at its strike after the registration refreshed to a stock handle (writer output through the production reader); a historical positive passes and an unrecorded observation is rejected'

# R3-F2: a serialized current outcome (the production ReportTerminalOnce outcome through the tranche's own JSON settings) passes the
# Mount Primary reader; removing the observed source, or the readability field, separately makes it fail; the explicit unrecorded
# negative still fails and the supported historical positives still pass.
$jsonSettings = $phase3d.GetField('JsonSettings', $any).GetValue($null)
$jObject = $jsonSettings.GetType().Assembly.GetType('Newtonsoft.Json.Linq.JObject', $true)
$serializer = $jObject.Assembly.GetType('Newtonsoft.Json.JsonSerializer').GetMethod('Create', [Type[]]@($jsonSettings.GetType())).Invoke($null, [object[]]@($jsonSettings))
function Serialized($o) { ($jObject.GetMethod('FromObject', [Type[]]@([object], $serializer.GetType())).Invoke($null, [object[]]@($o, $serializer))).ToString() | ConvertFrom-Json }
function Without($o, [string]$Name) { $copy = $o | ConvertTo-Json -Depth 6 | ConvertFrom-Json; $copy.PSObject.Properties.Remove($Name); $copy }
$currentRow = Serialized $detailed[0]; $currentRow.attackAnimationActed = $true; $currentRow.attackAnimationFinished = $true
$noSource = Without $currentRow 'attackAnimationHandleSource'; $noReadability = Without $currentRow 'attackAnimationFactsUnreadable'
$unrecordedRow = $currentRow | ConvertTo-Json -Depth 6 | ConvertFrom-Json; $unrecordedRow.attackAnimationHandleSource = 'unrecorded'
Write-Host ('  R3-F2 r5: current=' + (& $readerRejects $currentRow) + ' noSource=' + (& $readerRejects $noSource) + ' noReadability=' + (& $readerRejects $noReadability) + ' unrecorded=' + (& $readerRejects $unrecordedRow) + ' historical=' + (& $readerRejects $historicalRow) + ' oldest=' + (& $readerRejects $oldestRow))
Check ($null -ne $currentRow.PSObject.Properties['attackAnimationObservation'] -and -not (& $readerRejects $currentRow) -and (& $readerRejects $noSource) -and (& $readerRejects $noReadability) -and
    (& $readerRejects $unrecordedRow) -and -not (& $readerRejects $historicalRow) -and -not (& $readerRejects $oldestRow)) 'PR-5 R3-F2: a serialized current Mount Primary outcome passes; removing its observed source or its readability field separately fails; the unrecorded negative fails and the supported historical positives pass'
Write-Host ('Mounted alpha 2 bite contract PASS=' + $script:passed + ' FAIL=0 nativeProof=false')
