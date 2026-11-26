[CmdletBinding()]
param([string]$Configuration = 'Release')
# Mounted Combat alpha 2 (docs/missions/mounted-alpha2-melee-pose/MISSION.md), MMR-06 Horse Trip: installed-contract and
# production-path checks, split by area from scripts/Test-MountedAlpha2NativeContract.ps1 at 46444b30 (PR #64, not accepted);
# every check line and label below is that script's, verbatim, with the shared preamble.
# MMR-06 (plain horse bite Trip):
#  1. Installed Kingmaker 2.1.7b data (pinned sharedassets1 identity, exact serialized object offsets): the stock Mammoth
#     companion's AddFacts are exactly [TrippingBite, NaturalArmor7, TripDefenseFourLegs]; TrippingBite's only component is
#     ManeuverOnAttack(Category=Bite, Maneuver=Trip); the stock Dog and Wolf companions carry the same TrippingBite.
#  2. The compiled product on genuine Kingmaker objects created without Unity: the Horse's inherited facts drop exactly the
#     intrinsic attack-maneuver trigger and keep the rest in order; on a saved horse nothing is removed automatically: a
#     sourceless exact stock grant on the KMC Horse is only a repair candidate (removed on the owner's written request),
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
$module = $game.ManifestModule
function CallsAnyMethod([Type]$Type, [Reflection.MethodBase]$Target) {
    foreach ($t in @($Type) + @($Type.GetNestedTypes($any))) {
        foreach ($m in @($t.GetMethods($any + [Reflection.BindingFlags]::DeclaredOnly)) + @($t.GetConstructors($any + [Reflection.BindingFlags]::DeclaredOnly))) {
            $body = $m.GetMethodBody(); if ($null -eq $body) { continue }; $il = $body.GetILAsByteArray()
            for ($i = 0; $i -le $il.Length - 5; $i++) { if (($il[$i] -eq 0x28 -or $il[$i] -eq 0x6F) -and [BitConverter]::ToInt32($il, $i + 1) -eq $Target.MetadataToken) { try { if ($m.Module.ResolveMethod([BitConverter]::ToInt32($il, $i + 1)) -eq $Target) { return $true } } catch { } } }
        }
    }
    $false
}
function MemberAt([int]$Token) { $module.ResolveMember($Token) }
function IlTokens([Reflection.MethodBase]$Method) { $il = $Method.GetMethodBody().GetILAsByteArray(); $set = @{}; for ($i = 0; $i -le $il.Length - 4; $i++) { $set[[BitConverter]::ToInt32($il, $i)] = $true }; $set }
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
$tb = New-Fact 'f957b4444b6fb404e84ae2a5765797bb' @($trigger)
$na = New-Fact 'NaturalArmor7' @((New-Native $statBonusType))
$td = New-Fact 'TripDefenseFourLegs' @()
# Another mod's own bite-maneuver fact: same component, different identity (review R1-F1: not the stock grant).
$otherTrigger = New-Native $maneuverType
# PR-4 review R1-F6: the same installed Category and Maneuver as the stock control, so the GUID alone distinguishes it.
$maneuverType.GetField('Category').SetValue($otherTrigger, [Enum]::ToObject($categoryType, $category))
$maneuverType.GetField('Maneuver').SetValue($otherTrigger, [Enum]::ToObject($combatManeuverType, $maneuver))
$other = New-Fact '0123456789abcdef0123456789abcdef' @($otherTrigger)
$sameIdNoTrigger = New-Fact 'f957b4444b6fb404e84ae2a5765797bb' @()
$disarm = New-Native $maneuverType
$maneuverType.GetField('Category').SetValue($disarm, [Enum]::ToObject($categoryType, $category))
$maneuverType.GetField('Maneuver').SetValue($disarm, [Enum]::Parse($combatManeuverType, 'Disarm'))
$sameIdDisarm = New-Fact 'f957b4444b6fb404e84ae2a5765797bb' @($disarm)
$claw = New-Native $maneuverType
$maneuverType.GetField('Category').SetValue($claw, [Enum]::Parse($categoryType, 'Claw'))
$maneuverType.GetField('Maneuver').SetValue($claw, [Enum]::ToObject($combatManeuverType, $maneuver))
$sameIdClaw = New-Fact 'f957b4444b6fb404e84ae2a5765797bb' @($claw)
# PR-4 review R1-F3: the stock GUID carrying the Bite/Trip component and a second effect (another mod's addition).
$mixedTrigger = New-Native $maneuverType
$maneuverType.GetField('Category').SetValue($mixedTrigger, [Enum]::ToObject($categoryType, $category))
$maneuverType.GetField('Maneuver').SetValue($mixedTrigger, [Enum]::ToObject($combatManeuverType, $maneuver))
$mixed = New-Fact 'f957b4444b6fb404e84ae2a5765797bb' @($mixedTrigger, (New-Native $statBonusType))
$stock = $service.GetMethod('IsStockTrippingBite', $any)
Check ([object]::ReferenceEquals($otherTrigger.GetType(), $trigger.GetType()) -and $otherTrigger.Category -eq $trigger.Category -and $otherTrigger.Maneuver -eq $trigger.Maneuver) 'the foreign-GUID control carries the same installed Category (Bite) and Maneuver (Trip) as the stock control'
Check (-not [bool]$stock.Invoke($null, @($mixed))) 'a stock-GUID feature carrying the Bite/Trip component and an additional effect is not the withheld stock grant (whole-fact treatment needs the verified one-component list)'
$mixedDonor = [Array]::CreateInstance($unitFactType, 3); $mixedDonor.SetValue($mixed, 0); $mixedDonor.SetValue($na, 1); $mixedDonor.SetValue($td, 2)
$mixedInherited = $service.GetMethod('HorseInheritedFacts', $any).Invoke($null, @(, $mixedDonor))
Check ($mixedInherited.Length -eq 3 -and [object]::ReferenceEquals($mixedInherited[0], $mixed)) 'fresh construction preserves the mixed stock-GUID feature whole'
$mixedTb = New-Fact 'f957b4444b6fb404e84ae2a5765797bb' @($trigger)
Check ([bool]$stock.Invoke($null, @($tb)) -and -not [bool]$stock.Invoke($null, @($other)) -and -not [bool]$stock.Invoke($null, @($sameIdNoTrigger)) -and -not [bool]$stock.Invoke($null, @($sameIdDisarm)) -and -not [bool]$stock.Invoke($null, @($sameIdClaw)) -and
    -not [bool]$stock.Invoke($null, @($na)) -and -not [bool]$stock.Invoke($null, @($td)) -and -not [bool]$stock.Invoke($null, @($null))) 'only the installed stock identity with its exact Bite/Trip trigger is the withheld grant (another GUID, no trigger, Disarm or Claw are not)'
$donor = [Array]::CreateInstance($unitFactType, 4); $donor.SetValue($tb, 0); $donor.SetValue($na, 1); $donor.SetValue($td, 2); $donor.SetValue($other, 3)
$inherited = $service.GetMethod('HorseInheritedFacts', $any).Invoke($null, @(, $donor))
Check ($inherited.Length -eq 3 -and [object]::ReferenceEquals($inherited[0], $na) -and [object]::ReferenceEquals($inherited[1], $td) -and [object]::ReferenceEquals($inherited[2], $other)) 'Horse inherits [NaturalArmor7, TripDefenseFourLegs, another mod''s maneuver fact] in order, without the stock TrippingBite'
Check ($donor.Length -eq 4 -and [object]::ReferenceEquals($donor[0], $tb)) 'the stock Mammoth array itself is not mutated'
Check ($service.GetMethod('HorseInheritedFacts', $any).Invoke($null, @(, $null)).Length -eq 0) 'absent donor facts give none'
# The Horse's bite/hoof limbs are not facts: CopyBody sets them independently of AddFacts (unchanged), so the bite and its damage path remain.
$copyBody = $service.GetMethod('CopyBody', $any)
$weaponType = GameType 'Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon'
$bodyType = $copyBody.GetParameters()[0].ParameterType
$biteWeapon = New-Native $weaponType; $hoofWeapon = New-Native $weaponType
$horseBody = $copyBody.Invoke($null, @((New-Native $bodyType), $biteWeapon, $hoofWeapon))
$limbs = @($horseBody.AdditionalLimbs)
Check ($limbs.Count -eq 3 -and [object]::ReferenceEquals($limbs[0], $biteWeapon) -and [object]::ReferenceEquals($limbs[1], $hoofWeapon) -and [object]::ReferenceEquals($limbs[2], $hoofWeapon) -and
    @($horseBody.AdditionalSecondaryLimbs).Count -eq 0 -and $horseBody.DisableHands -and $null -eq $horseBody.PrimaryHand -and $null -eq $horseBody.SecondaryHand) 'the compiled CopyBody builds one bite followed by two hooves (distinct genuine weapons; hands disabled), independently of the filtered facts'
Check (CallsAnyMethod $service $copyBody) 'the Horse construction uses that CopyBody'

$liveFeatureType = GameType 'Kingmaker.UnitLogic.Feature'
$factType = GameType 'Kingmaker.Blueprints.Facts.Fact'
$progressionType = GameType 'Kingmaker.Blueprints.Classes.BlueprintProgression'
$itemType = GameType 'Kingmaker.Items.ItemEntityWeapon'
$cutsceneType = GameType 'Kingmaker.Blueprints.Facts.FactSourceCutscene'
function New-LiveFeature($Blueprint, $Source, $Item, $Cutscene) {
    $feature = New-Native $liveFeatureType
    SetField $feature $liveFeatureType 'Blueprint' $Blueprint
    if ($null -ne $Source) { SetField $feature $liveFeatureType '<Source>k__BackingField' $Source }
    if ($null -ne $Item) { SetField $feature $factType '<SourceItem>k__BackingField' $Item }
    if ($null -ne $Cutscene) { SetField $feature $factType '<SourceCutscene>k__BackingField' $Cutscene }
    $feature
}
# Existing horses (review R3 R1-F1): nothing is removed automatically. The saved state cannot tell the alpha-1 inherited copy
# from a sourceless copy granted another way, so the product only reports candidates and removes one fact from one horse on
# the owner's written request (HorseTripRepairPolicy; tests/HorseTripRepairPolicyTests.cs covers the request lifecycle).
$candidate = $service.GetMethod('IsTripRepairCandidate', $any)
$horseGuid = $service.GetField('UnitGuid', $any).GetRawConstantValue()
$mammothGuid = $service.GetField('MammothUnitGuid', $any).GetRawConstantValue()
function IsCandidate([string]$UnitGuid, $Feature) { [bool]$candidate.Invoke($null, [object[]]@($UnitGuid, $Feature)) }
$inheritedTrip = New-LiveFeature $tb $null $null $null
$sourcedTrip = New-LiveFeature $tb (New-Native $progressionType) $null $null
$itemTrip = New-LiveFeature $tb $null (New-Native $itemType) $null
$cutsceneTrip = New-LiveFeature $tb $null $null ([Runtime.Serialization.FormatterServices]::GetUninitializedObject($cutsceneType))
$otherSourceless = New-LiveFeature $other $null $null $null
$armor = New-LiveFeature $na $null $null $null
Check (IsCandidate $horseGuid $inheritedTrip) 'a sourceless exact stock TrippingBite on the KMC Horse is a repair candidate (reported, removed only on request)'
Check (-not (IsCandidate $horseGuid (New-LiveFeature $mixed $null $null $null)) -and (IsCandidate $horseGuid (New-LiveFeature $mixedTb $null $null $null))) 'saved-horse repair excludes the sourceless mixed stock-GUID feature; the single-component stock control stays a candidate'
Check (-not (IsCandidate $horseGuid $sourcedTrip) -and -not (IsCandidate $horseGuid $itemTrip) -and -not (IsCandidate $horseGuid $cutsceneTrip)) 'an independently granted copy that records a source fact, item or cutscene is never a candidate'
Check (-not (IsCandidate $horseGuid $otherSourceless) -and -not (IsCandidate $horseGuid (New-LiveFeature $sameIdDisarm $null $null $null)) -and
    -not (IsCandidate $horseGuid (New-LiveFeature $sameIdClaw $null $null $null)) -and -not (IsCandidate $horseGuid (New-LiveFeature $sameIdNoTrigger $null $null $null)) -and
    -not (IsCandidate $horseGuid $armor) -and -not (IsCandidate $horseGuid $null)) 'another GUID with the same trigger, the stock GUID with Disarm, Claw or no trigger, and unrelated facts are not candidates'
Check (-not (IsCandidate $mammothGuid $inheritedTrip) -and -not (IsCandidate '0123456789abcdef0123456789abcdef' $inheritedTrip) -and -not (IsCandidate $null $inheritedTrip)) 'a TrippingBite on any unit other than the exact KMC Horse (stock Dog, Wolf, Mammoth, Riding Dog) is not a candidate'
Check ($null -eq $service.GetMethod('StaleInheritedAttackManeuvers', $any) -and $null -eq $service.GetMethod('DonorSuccessorFeature', $any)) 'the positional saved-fact deletion rule is gone from the product'
# Structural: the only product code in the service that removes a fact is the owner-request host's Remove.
$removers = New-Object 'System.Collections.Generic.List[string]'
foreach ($t in @($service) + @($service.GetNestedTypes($any))) {
    foreach ($m in @($t.GetMethods($any + [Reflection.BindingFlags]::DeclaredOnly)) + @($t.GetConstructors($any + [Reflection.BindingFlags]::DeclaredOnly))) {
        $body = $m.GetMethodBody(); if ($null -eq $body) { continue }
        $il = $body.GetILAsByteArray()
        for ($i = 0; $i -le $il.Length - 5; $i++) {
            if ($il[$i] -ne 0x28 -and $il[$i] -ne 0x6F) { continue }
            try { $callee = $m.Module.ResolveMethod([BitConverter]::ToInt32($il, $i + 1)) } catch { continue }
            if ($callee.Name -ceq 'RemoveFact' -and $callee.DeclaringType.Assembly -eq $game) { $removers.Add($t.Name + '.' + $m.Name); break }
        }
    }
}
Check ($removers.Count -eq 1 -and $removers[0] -ceq 'TripRepairFileHost.Remove') ('the only fact removal in the Horse service is the owner-request host (' + ($removers -join ', ') + ')')
$policy = KmcType 'KingmakerMountedCombat.Domain.HorseTripRepairPolicy'
Check ($policy.GetField('RequestFileName').GetRawConstantValue() -ceq 'horse-trip-repair-request.txt' -and $policy.GetField('FolderName').GetRawConstantValue() -ceq 'KingmakerMountedCombat' -and $null -eq $policy.GetField('CandidateReportFileName')) 'the owner-facing request file and user-data folder names; no candidate report file (candidates are logged only)'
# Review R4-F6: no file is written into the Mods tree. The request lives in <persistence data parent>/KingmakerMountedCombat (the composition
# root resolves it through NativePersistenceIsolation.UserDataParent and HorseTripRepairPaths.Folder), the mod folder is no longer recorded, and the
# Horse service itself calls no System.IO.File method (every file operation is HorseTripRepairFiles, run only for an owner request).
$mainType = KmcType 'KingmakerMountedCombat.Main'
Check ($null -eq $mainType.GetProperty('ModDirectory', $any)) 'the mod folder (UMM ModEntry.Path) is not recorded for the repair'
$rootCtor = @((KmcType 'KingmakerMountedCombat.CompositionRoot').GetConstructors($any))
$userData = (KmcType 'KingmakerMountedCombat.Integration.NativePersistenceIsolation').GetProperty('UserDataParent', $any).GetGetMethod($true)
$folderOf = (KmcType 'KingmakerMountedCombat.Domain.HorseTripRepairPaths').GetMethod('Folder')
$selectFolder = (KmcType 'KingmakerMountedCombat.Domain.HorseTripRepairPaths').GetMethod('SelectFolder')
$isIsolated = (KmcType 'KingmakerMountedCombat.Integration.NativePersistenceIsolation').GetProperty('IsIsolated', $any).GetGetMethod($true)
function CallsAny([Type]$Type, [Reflection.MethodBase]$Target) {
    foreach ($t in @($Type) + @($Type.GetNestedTypes($any))) {
        foreach ($m in @($t.GetMethods($any + [Reflection.BindingFlags]::DeclaredOnly)) + @($t.GetConstructors($any + [Reflection.BindingFlags]::DeclaredOnly))) {
            $body = $m.GetMethodBody(); if ($null -eq $body) { continue }; $il = $body.GetILAsByteArray()
            for ($i = 0; $i -le $il.Length - 5; $i++) { if (($il[$i] -eq 0x28 -or $il[$i] -eq 0x6F) -and [BitConverter]::ToInt32($il, $i + 1) -eq $Target.MetadataToken) { try { if ($m.Module.ResolveMethod([BitConverter]::ToInt32($il, $i + 1)) -eq $Target) { return $true } } catch { } } }
        }
    }
    $false
}
$root = KmcType 'KingmakerMountedCombat.CompositionRoot'
# PR-4 review R1-F1: one compiled method of the composition root selects the folder from the automation host, the isolation
# binding and the data parent; SelectFolder refuses a guarded run without a bound isolated root.
$automationField = $root.GetField('runtimeAutomation', $any)
$selectors = New-Object 'System.Collections.Generic.List[string]'
foreach ($t in @($root) + @($root.GetNestedTypes($any))) {
    foreach ($m in @($t.GetMethods($any + [Reflection.BindingFlags]::DeclaredOnly))) {
        $body = $m.GetMethodBody(); if ($null -eq $body) { continue }; $il = $body.GetILAsByteArray(); $seen = @{}
        for ($i = 0; $i -le $il.Length - 5; $i++) {
            if ($il[$i] -ne 0x28 -and $il[$i] -ne 0x6F -and $il[$i] -ne 0x7B) { continue }
            try { $member = $m.Module.ResolveMember([BitConverter]::ToInt32($il, $i + 1)) } catch { continue }
            if ($member -eq $selectFolder) { $seen.select = $true } elseif ($member -eq $isIsolated) { $seen.isolated = $true }
            elseif ($member -eq $userData) { $seen.parent = $true } elseif ($member -eq $automationField) { $seen.automation = $true }
        }
        if ($seen.Count -eq 4) { $selectors.Add($t.Name + '.' + $m.Name) }
    }
}
Check ($selectors.Count -eq 1 -and -not (CallsAny $root $folderOf)) ('the composition root selects the repair folder in one method from runtimeAutomation, NativePersistenceIsolation.IsIsolated and UserDataParent through SelectFolder (' + ($selectors -join ', ') + ')')
Check ($null -eq $selectFolder.Invoke($null, @($true, $false, 'C:\player')) -and $selectFolder.Invoke($null, @($true, $true, 'C:\run')) -ceq 'C:\run\KingmakerMountedCombat' -and
    $selectFolder.Invoke($null, @($false, $false, 'C:\player')) -ceq 'C:\player\KingmakerMountedCombat') 'a guarded run without a bound isolated root has no repair folder; an isolated run uses its own data parent; ordinary play the player''s'
$fileCalls = New-Object 'System.Collections.Generic.List[string]'
foreach ($t in @($service) + @($service.GetNestedTypes($any))) {
    foreach ($m in @($t.GetMethods($any + [Reflection.BindingFlags]::DeclaredOnly))) {
        $body = $m.GetMethodBody(); if ($null -eq $body) { continue }; $il = $body.GetILAsByteArray()
        for ($i = 0; $i -le $il.Length - 5; $i++) {
            if ($il[$i] -ne 0x28) { continue }
            try { $callee = $m.Module.ResolveMethod([BitConverter]::ToInt32($il, $i + 1)) } catch { continue }
            if ($callee.DeclaringType -eq [IO.File] -or $callee.DeclaringType -eq [IO.Directory]) { $fileCalls.Add($t.Name + '.' + $m.Name + '->' + $callee.Name) }
        }
    }
}
Check ($fileCalls.Count -eq 0) ('the Horse service performs no file operation of its own (' + ($fileCalls -join ', ') + ')')
Write-Host ('Mounted alpha 2 native contract (trip) PASS=' + $script:passed + ' FAIL=0 nativeProof=false')
