[CmdletBinding()]
param([string]$Configuration = 'Release')
# Mounted Combat alpha 2 (docs/missions/mounted-alpha2-melee-pose/MISSION.md) installed-contract and production-path checks.
# MMR-06 (plain horse bite Trip):
#  1. Installed Kingmaker 2.1.7b data (pinned sharedassets1 identity, exact serialized object offsets): the stock Mammoth
#     companion's AddFacts are exactly [TrippingBite, NaturalArmor7, TripDefenseFourLegs]; TrippingBite's only component is
#     ManeuverOnAttack(Category=Bite, Maneuver=Trip); the stock Dog and Wolf companions carry the same TrippingBite.
#  2. The compiled product on genuine Kingmaker objects created without Unity: the Horse's inherited facts drop exactly the
#     intrinsic attack-maneuver trigger and keep the rest in order; a horse saved before the repair loses only a
#     sourceless inherited trigger, never a sourced (class/progression/selection) grant or another fact.
# MMR-02/04/05/07: the installed members the click admission, the real-time readiness gate and the bite telemetry bind, and
# the native horse animation bundle identity.
# Not covered (native game evidence): the live registration, the stock Dog/Wolf/Riding Dog in play, a bite hit's rulebook
# chain, and a saved horse's actual facts on load.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$lab = [IO.Path]::GetFullPath((Join-Path $repoRoot '..\..'))
$layout = (Get-Content -Raw -LiteralPath (Join-Path $lab 'environment-intake.json') | ConvertFrom-Json).requestedLayout
$data = Join-Path $layout.kingmakerInstallDir 'Kingmaker_Data'
$managed = Join-Path $data 'Managed'
$productDll = Join-Path $repoRoot ('bin\' + $Configuration + '\KingmakerMountedCombat.dll')
$script:passed = 0
function Check([bool]$Value, [string]$Label) { if (-not $Value) { throw ('Mounted alpha 2 native contract: ' + $Label) }; $script:passed++; Write-Host ('PASS ' + $Label) }

# ---- 1. Installed data ---------------------------------------------------------------------------------------------
$assets = Join-Path $data 'sharedassets1.assets'
Check ((Get-FileHash -LiteralPath $assets).Hash.ToLowerInvariant() -ceq 'cc779caf2fef21d111856a57d40b510677b0c236ad2723d1849564626445f785') 'pinned sharedassets1 identity'
function UnityString($Reader, [int]$Max) {
    $length = $Reader.ReadInt32(); if ($length -lt 0 -or $length -gt $Max) { throw 'Unexpected bounded Unity string' }
    $text = [Text.Encoding]::UTF8.GetString($Reader.ReadBytes($length))
    [void]$Reader.BaseStream.Seek((4 - $Reader.BaseStream.Position % 4) % 4, [IO.SeekOrigin]::Current)
    $text
}
# MonoBehaviour header (m_GameObject, m_Enabled), m_Script, m_Name; BlueprintScriptableObject PrototypeLink, Components,
# m_AssetGuid. Returns the component path IDs; the reader is left after the GUID.
function Read-BlueprintHeader($Reader, [long]$Offset, [long]$Script, [string]$Name, [string]$Guid) {
    [void]$Reader.BaseStream.Seek($Offset + 16, [IO.SeekOrigin]::Begin)
    Check (($Reader.ReadInt32() -eq 1) -and ($Reader.ReadInt64() -eq $Script)) ($Name + ' managed script pointer ' + $Script)
    Check ((UnityString $Reader 256) -ceq $Name) ($Name + ' authored name')
    [void]$Reader.ReadBytes(12)
    $count = $Reader.ReadInt32(); if ($count -lt 0 -or $count -gt 64) { throw 'Unexpected component count' }
    $components = @(for ($i = 0; $i -lt $count; $i++) { [void]$Reader.ReadInt32(); $Reader.ReadInt64() })
    if ($Guid) { Check ((UnityString $Reader 32) -ceq $Guid) ($Name + ' GUID ' + $Guid) }
    , $components
}
# A BlueprintUnit's AddFacts is its final serialized PPtr array, followed by one 4-byte field (the stock layout shared by
# the three companion units below): count, then (fileID 0, pathID) pairs ending 4 bytes before the object end.
function Read-AddFacts($Reader, [long]$Offset, [int]$Size, [int]$Count) {
    [void]$Reader.BaseStream.Seek($Offset + $Size - 4 - 12 * $Count - 4, [IO.SeekOrigin]::Begin)
    Check ($Reader.ReadInt32() -eq $Count) ('AddFacts count ' + $Count + ' at the serialized end')
    @(for ($i = 0; $i -lt $Count; $i++) { $file = $Reader.ReadInt32(); $path = $Reader.ReadInt64(); if ($file -ne 0) { throw 'Foreign AddFacts reference' }; $path })
}
$tripping = 78172; $naturalArmor = 78131; $tripDefense = 82113
$reader = New-Object IO.BinaryReader ([IO.File]::OpenRead($assets))
try {
    [void](Read-BlueprintHeader $reader 152094752 7324 'AnimalCompanionUnitMammoth' 'e7aa96d15a45238438ae4cfb476f6bb9')
    $mammothFacts = Read-AddFacts $reader 152094752 804 3
    Check ((($mammothFacts) -join ',') -ceq "$tripping,$naturalArmor,$tripDefense") 'stock Mammoth AddFacts = [TrippingBite, NaturalArmor7, TripDefenseFourLegs]'
    foreach ($unit in @(@(152092320, 'AnimalCompanionUnitDog'), @(152097200, 'AnimalCompanionUnitWolf'))) {
        [void](Read-BlueprintHeader $reader $unit[0] 7324 $unit[1] $null)
        $facts = Read-AddFacts $reader $unit[0] 800 3
        Check ($facts[0] -eq $tripping -and $facts[2] -eq $tripDefense) ('stock ' + $unit[1] + ' carries the same TrippingBite (legitimate; untouched)')
    }
    $trippingComponents = Read-BlueprintHeader $reader 152635120 6001 'TrippingBite' 'f957b4444b6fb404e84ae2a5765797bb'
    Check ($trippingComponents.Count -eq 1 -and $trippingComponents[0] -eq 223427) 'TrippingBite has exactly one component'
    [void](Read-BlueprintHeader $reader 186451712 7074 '$ManeuverOnAttack$8f1ebfd9-eb4e-49a3-9228-5b7b82bc8d16' $null)
    [void]$reader.BaseStream.Seek(186451712 + 108 - 8, [IO.SeekOrigin]::Begin)
    $category = $reader.ReadInt32(); $maneuver = $reader.ReadInt32()
    [void](Read-BlueprintHeader $reader 152625680 3769 'NaturalArmor7' $null)
    [void](Read-BlueprintHeader $reader 154054896 6001 'TripDefenseFourLegs' '13c87ac5985cc85498ef9d1ac8b78923')
} finally { $reader.Dispose() }

# ---- 2. Production paths on genuine objects ------------------------------------------------------------------------
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
$maneuverType = GameType 'Kingmaker.Designers.Mechanics.Facts.ManeuverOnAttack'
$categoryType = GameType 'Kingmaker.Enums.WeaponCategory'; $combatManeuverType = GameType 'Kingmaker.RuleSystem.Rules.CombatManeuver'
Check ([Enum]::GetName($categoryType, $category) -ceq 'Bite' -and [Enum]::GetName($combatManeuverType, $maneuver) -ceq 'Trip') ('installed TrippingBite component: ManeuverOnAttack(Category=' + [Enum]::GetName($categoryType, $category) + ', Maneuver=' + [Enum]::GetName($combatManeuverType, $maneuver) + ')')
$service = KmcType 'KingmakerMountedCombat.Integration.HorseCompanionBlueprintService'
Check ($service.GetField('StockTrippingBiteGuid', $any).GetRawConstantValue() -ceq 'f957b4444b6fb404e84ae2a5765797bb') 'the product records the installed TrippingBite identity'
$featureType = GameType 'Kingmaker.Blueprints.Classes.BlueprintFeature'
$unitFactType = GameType 'Kingmaker.Blueprints.Facts.BlueprintUnitFact'
$componentType = GameType 'Kingmaker.Blueprints.BlueprintComponent'
$statBonusType = GameType 'Kingmaker.UnitLogic.FactLogic.AddStatBonus'
function New-Fact([string]$Name, $Components) {
    $fact = New-Native $featureType
    $array = [Array]::CreateInstance($componentType, @($Components).Count)
    for ($i = 0; $i -lt @($Components).Count; $i++) { $array.SetValue(@($Components)[$i], $i) }
    SetField $fact $featureType 'm_AssetGuid' $Name
    $featureType.GetProperty('ComponentsArray').SetValue($fact, $array, $null)
    $fact
}
$trigger = New-Native $maneuverType
$maneuverType.GetField('Category').SetValue($trigger, [Enum]::ToObject($categoryType, $category))
$maneuverType.GetField('Maneuver').SetValue($trigger, [Enum]::ToObject($combatManeuverType, $maneuver))
$tb = New-Fact 'TrippingBite' @($trigger)
$na = New-Fact 'NaturalArmor7' @((New-Native $statBonusType))
$td = New-Fact 'TripDefenseFourLegs' @()
$grants = $service.GetMethod('GrantsIntrinsicAttackManeuver', $any)
Check ([bool]$grants.Invoke($null, @($tb)) -and -not [bool]$grants.Invoke($null, @($na)) -and -not [bool]$grants.Invoke($null, @($td)) -and -not [bool]$grants.Invoke($null, @($null))) 'only the bite-Trip trigger is an intrinsic attack maneuver'
$donor = [Array]::CreateInstance($unitFactType, 3); $donor.SetValue($tb, 0); $donor.SetValue($na, 1); $donor.SetValue($td, 2)
$inherited = $service.GetMethod('HorseInheritedFacts', $any).Invoke($null, @(, $donor))
Check ($inherited.Length -eq 2 -and [object]::ReferenceEquals($inherited[0], $na) -and [object]::ReferenceEquals($inherited[1], $td)) 'Horse inherits [NaturalArmor7, TripDefenseFourLegs] in order, without TrippingBite'
Check ($donor.Length -eq 3 -and [object]::ReferenceEquals($donor[0], $tb)) 'the stock Mammoth array itself is not mutated'
Check ($service.GetMethod('HorseInheritedFacts', $any).Invoke($null, @(, $null)).Length -eq 0) 'absent donor facts give none'

$liveFeatureType = GameType 'Kingmaker.UnitLogic.Feature'
$progressionType = GameType 'Kingmaker.Blueprints.Classes.BlueprintProgression'
function New-LiveFeature($Blueprint, $Source) {
    $feature = New-Native $liveFeatureType
    SetField $feature $liveFeatureType 'Blueprint' $Blueprint
    if ($null -ne $Source) { SetField $feature $liveFeatureType '<Source>k__BackingField' $Source }
    $feature
}
$listType = [Collections.Generic.List`1].MakeGenericType($liveFeatureType)
$stale = $service.GetMethod('StaleInheritedAttackManeuvers', $any)
$inheritedTrip = New-LiveFeature $tb $null
$sourcedTrip = New-LiveFeature $tb (New-Native $progressionType)
$armor = New-LiveFeature $na $null; $defense = New-LiveFeature $td $null
$saved = [Activator]::CreateInstance($listType); foreach ($f in @($armor, $inheritedTrip, $defense, $sourcedTrip)) { $saved.Add($f) }
$found = $stale.Invoke($null, [object[]]@(, $saved))
Check ($found.Count -eq 1 -and [object]::ReferenceEquals($found[0], $inheritedTrip)) 'a saved horse: only the sourceless inherited TrippingBite is stale; a progression-granted copy, natural armor and trip defense stay'
$clean = [Activator]::CreateInstance($listType); foreach ($f in @($armor, $defense)) { $clean.Add($f) }
Check ($stale.Invoke($null, [object[]]@(, $clean)).Count -eq 0 -and $stale.Invoke($null, [object[]]@(, $null)).Count -eq 0) 'a repaired or new horse has nothing to remove (idempotent)'
# ---- 3. MMR-02/04/05/07 installed contracts the production paths bind -------------------------------------------------
$module = $game.ManifestModule
function MemberAt([int]$Token) { $module.ResolveMember($Token) }
$ignore = MemberAt 0x06000C2F
Check ($ignore.Name -ceq 'IgnoreClick' -and $ignore.DeclaringType.FullName -ceq 'TurnBased.Controllers.TurnController') 'MMR-02: 06000C2F is TurnController.IgnoreClick (the patched method)'
$need = MemberAt 0x04000691; $highlight = MemberAt 0x0400068B
Check ($need.Name -ceq 'm_NeedNewPredictions' -and $need.FieldType -eq [bool] -and $need.DeclaringType -eq $ignore.DeclaringType) 'MMR-02: 04000691 is TurnController.m_NeedNewPredictions (bool)'
Check ($highlight.Name -ceq 'm_HighlightedUnit' -and $highlight.DeclaringType -eq $ignore.DeclaringType) 'MMR-02: 0400068B is TurnController.m_HighlightedUnit'
function IlTokens([Reflection.MethodBase]$Method) { $il = $Method.GetMethodBody().GetILAsByteArray(); $set = @{}; for ($i = 0; $i -le $il.Length - 4; $i++) { $set[[BitConverter]::ToInt32($il, $i)] = $true }; $set }
$pathCheck = MemberAt 0x06007018
Check ($pathCheck.Name -ceq 'IsCurrentPathForPoint' -and $pathCheck.DeclaringType.Name -ceq 'PathVisualizer') 'MMR-02: 06007018 is PathVisualizer.IsCurrentPathForPoint'
$ignoreTokens = IlTokens $ignore
Check ($ignoreTokens.ContainsKey(0x06007018)) 'MMR-02: native IgnoreClick drops a click whose visualized path does not end within the predicted approach radius'
$combatState = GameType 'Kingmaker.Controllers.Combat.UnitCombatState'
$canAct = $combatState.GetProperty('CanActInCombat').GetGetMethod()
$waiting = $combatState.GetProperty('IsWaitingInitiative', [Reflection.BindingFlags]'Instance,Public,NonPublic').GetGetMethod($true)
$inCombat = $combatState.GetField('m_InCombat', [Reflection.BindingFlags]'Instance,NonPublic')
$canActTokens = IlTokens $canAct
Check ($canActTokens.ContainsKey($waiting.MetadataToken) -and $canActTokens.ContainsKey($inCombat.MetadataToken)) 'MMR-04/05: CanActInCombat reads m_InCombat and IsWaitingInitiative (false outside combat; the product gates only in combat)'
$patches = KmcType 'KingmakerMountedCombat.Integration.MountedPatchController+PatchMethods'
Check ($null -ne $patches.GetMethod('PairedIgnoreClickPostfix', $any) -and $null -ne $patches.GetMethod('PairedIgnoreClickPrefix', $any)) 'MMR-02: the product carries the IgnoreClick prefix and the admission postfix'
$handle = GameType 'Kingmaker.Visual.Animation.Actions.AnimationActionHandle'
Check ($null -ne $handle.GetProperty('SpeedScale')) 'MMR-07: native handle SpeedScale (reported on the terminal line)'
# The native horse animation bundle the Horse view loads (identity only; its decoded contents are recorded in
# docs/mounted-melee/HANDOFF.md: HorseAnimationSet_Bite -> clip BiteAttack, HorseAnimationSet_Hoof -> Right/Left_Hoof_Attack).
$bundle = Join-Path $data 'StreamingAssets\Bundles\resource_2f17956095cd04b49a8f4c0bc88bb9ca'
Check ((Get-FileHash -LiteralPath $bundle).Hash.ToLowerInvariant() -ceq '3edb2116adf8857e11e466613c3bde6376d6201910d6a21a3d8774ee8bf2b476') 'MMR-07: pinned native horse animation bundle identity'

Write-Host ('Mounted alpha 2 native contract PASS=' + $script:passed + ' FAIL=0')
