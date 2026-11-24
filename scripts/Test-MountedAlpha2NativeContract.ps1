[CmdletBinding()]
param([string]$Configuration = 'Release')
# Mounted Combat alpha 2 (docs/missions/mounted-alpha2-melee-pose/MISSION.md) installed-contract and production-path checks.
# MMR-06 (plain horse bite Trip):
#  1. Installed Kingmaker 2.1.7b data (pinned sharedassets1 identity, exact serialized object offsets): the stock Mammoth
#     companion's AddFacts are exactly [TrippingBite, NaturalArmor7, TripDefenseFourLegs]; TrippingBite's only component is
#     ManeuverOnAttack(Category=Bite, Maneuver=Trip); the stock Dog and Wolf companions carry the same TrippingBite.
#  2. The compiled product on genuine Kingmaker objects created without Unity: the Horse's inherited facts drop exactly the
#     intrinsic attack-maneuver trigger and keep the rest in order; on a saved horse nothing is removed automatically: a
#     sourceless exact stock grant on the KMC Horse is only a repair candidate (removed on the owner's written request),
#     never a sourced (class/progression/selection/item/cutscene) grant, another shape or GUID, or another unit's fact.
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
$tb = New-Fact 'f957b4444b6fb404e84ae2a5765797bb' @($trigger)
$na = New-Fact 'NaturalArmor7' @((New-Native $statBonusType))
$td = New-Fact 'TripDefenseFourLegs' @()
# Another mod's own bite-maneuver fact: same component, different identity (review R1-F1: not the stock grant).
$otherTrigger = New-Native $maneuverType
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
$stock = $service.GetMethod('IsStockTrippingBite', $any)
Check ([bool]$stock.Invoke($null, @($tb)) -and -not [bool]$stock.Invoke($null, @($other)) -and -not [bool]$stock.Invoke($null, @($sameIdNoTrigger)) -and -not [bool]$stock.Invoke($null, @($sameIdDisarm)) -and -not [bool]$stock.Invoke($null, @($sameIdClaw)) -and
    -not [bool]$stock.Invoke($null, @($na)) -and -not [bool]$stock.Invoke($null, @($td)) -and -not [bool]$stock.Invoke($null, @($null))) 'only the installed stock identity with its exact Bite/Trip trigger is the withheld grant (another GUID, no trigger, Disarm or Claw are not)'
$donor = [Array]::CreateInstance($unitFactType, 4); $donor.SetValue($tb, 0); $donor.SetValue($na, 1); $donor.SetValue($td, 2); $donor.SetValue($other, 3)
$inherited = $service.GetMethod('HorseInheritedFacts', $any).Invoke($null, @(, $donor))
Check ($inherited.Length -eq 3 -and [object]::ReferenceEquals($inherited[0], $na) -and [object]::ReferenceEquals($inherited[1], $td) -and [object]::ReferenceEquals($inherited[2], $other)) 'Horse inherits [NaturalArmor7, TripDefenseFourLegs, another mod''s maneuver fact] in order, without the stock TrippingBite'
Check ($donor.Length -eq 4 -and [object]::ReferenceEquals($donor[0], $tb)) 'the stock Mammoth array itself is not mutated'
Check ($service.GetMethod('HorseInheritedFacts', $any).Invoke($null, @(, $null)).Length -eq 0) 'absent donor facts give none'
# The Horse's bite/hoof limbs are not facts: CopyBody sets them independently of AddFacts (unchanged), so the bite and its damage path remain.
Check ($null -ne $service.GetMethod('CopyBody', $any)) 'the Horse body limbs (bite, hoof, hoof) come from CopyBody, not from the filtered facts'

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
Check ((CallsAny $root $userData) -and (CallsAny $root $folderOf)) 'the repair folder is the persistence data parent''s KingmakerMountedCombat folder (isolated parent under a guarded run)'
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
# ---- 3. MMR-02/04/05/07 installed contracts the production paths bind -------------------------------------------------
$module = $game.ManifestModule
function MemberAt([int]$Token) { $module.ResolveMember($Token) }
$ignore = MemberAt 0x06000C2F
Check ($ignore.Name -ceq 'IgnoreClick' -and $ignore.DeclaringType.FullName -ceq 'TurnBased.Controllers.TurnController') 'MMR-02: 06000C2F is TurnController.IgnoreClick (the patched method)'
$need = MemberAt 0x04000691; $highlight = MemberAt 0x0400068B
Check ($need.Name -ceq 'm_NeedNewPredictions' -and $need.FieldType -eq [bool] -and $need.DeclaringType -eq $ignore.DeclaringType) 'MMR-02: 04000691 is TurnController.m_NeedNewPredictions (bool)'
Check ($highlight.Name -ceq 'm_HighlightedUnit' -and $highlight.DeclaringType -eq $ignore.DeclaringType) 'MMR-02: 0400068B is TurnController.m_HighlightedUnit'
function IlTokens([Reflection.MethodBase]$Method) { $il = $Method.GetMethodBody().GetILAsByteArray(); $set = @{}; for ($i = 0; $i -le $il.Length - 4; $i++) { $set[[BitConverter]::ToInt32($il, $i)] = $true }; $set }
$prediction = MemberAt 0x0400066E
Check ($prediction.Name -ceq 'm_PredictionCommands' -and $prediction.FieldType.FullName -ceq 'Kingmaker.UnitLogic.Commands.UnitCommands' -and $prediction.DeclaringType -eq $ignore.DeclaringType) 'MMR-02: 0400066E is TurnController.m_PredictionCommands (the simulated click''s commands)'
$pathCheck = MemberAt 0x06007018
Check ($pathCheck.Name -ceq 'IsCurrentPathForPoint' -and $pathCheck.DeclaringType.Name -ceq 'PathVisualizer') 'MMR-02: 06007018 is PathVisualizer.IsCurrentPathForPoint'
$ignoreTokens = IlTokens $ignore
Check ($ignoreTokens.ContainsKey(0x06007018)) 'MMR-02: native IgnoreClick drops a click whose visualized path does not end within the predicted approach radius'
$combatState = GameType 'Kingmaker.Controllers.Combat.UnitCombatState'
$canAct = $combatState.GetProperty('CanActInCombat').GetGetMethod()
$waiting = $combatState.GetProperty('IsWaitingInitiative', [Reflection.BindingFlags]'Instance,Public,NonPublic').GetGetMethod($true)
$inCombat = $combatState.GetField('m_InCombat', [Reflection.BindingFlags]'Instance,NonPublic')
$canActTokens = IlTokens $canAct
Check ($canActTokens.ContainsKey($waiting.MetadataToken) -and $canActTokens.ContainsKey($inCombat.MetadataToken)) 'MMR-04/05: native CanActInCombat reads m_InCombat and IsWaitingInitiative (the initiative wait before a real-time strike can start)'
$patches = KmcType 'KingmakerMountedCombat.Integration.MountedPatchController+PatchMethods'
Check ($null -ne $patches.GetMethod('PairedIgnoreClickPostfix', $any) -and $null -ne $patches.GetMethod('PairedIgnoreClickPrefix', $any)) 'MMR-02: the product carries the IgnoreClick prefix and the read-only drop-diagnostic postfix'
$handle = GameType 'Kingmaker.Visual.Animation.Actions.AnimationActionHandle'
Check ($null -ne $handle.GetProperty('SpeedScale')) 'MMR-07: native handle SpeedScale (reported on the terminal line)'
# The native horse animation bundle the Horse view loads (identity only; its decoded contents are recorded in
# docs/mounted-melee/HANDOFF.md: HorseAnimationSet_Bite -> clip BiteAttack, HorseAnimationSet_Hoof -> Right/Left_Hoof_Attack).
$bundle = Join-Path $data 'StreamingAssets\Bundles\resource_2f17956095cd04b49a8f4c0bc88bb9ca'
Check ((Get-FileHash -LiteralPath $bundle).Hash.ToLowerInvariant() -ceq '3edb2116adf8857e11e466613c3bde6376d6201910d6a21a3d8774ee8bf2b476') 'MMR-07: pinned native horse animation bundle identity'

# MMR-04 native target choice (review R1-F11): the rider brain's automatic attack (Character_Brain -> AttackAiAction ->
# AttackTargetsPriority: StandStill, UnconsciousPenalty, NotImpatient) scores candidates from DecisionContext.Enemies, which
# reads the group's memory (perceived enemies), filters dead/untargetable/aggro, and sorts with TargetInfo.Sort; FillWeight
# ranks a player's manual target first and, in real time, subtracts 0.001 x distance, so the nearest eligible perceived enemy
# wins. KMC passes that proposal through unchanged (no target selection of its own).
$decision = GameType 'Kingmaker.Controllers.Brain.DecisionContext'; $targetInfo = GameType 'Kingmaker.Controllers.Brain.TargetInfo'
$enemiesTokens = IlTokens ($decision.GetProperty('Enemies', [Reflection.BindingFlags]'Instance,Public,NonPublic').GetGetMethod($true))
$memoryEnemies = (GameType 'Kingmaker.UnitLogic.UnitGroupMemory').GetProperty('Enemies').GetGetMethod()
$sortMethod = $targetInfo.GetMethod('Sort', [Reflection.BindingFlags]'Static,Public,NonPublic')
Check ($enemiesTokens.ContainsKey($memoryEnemies.MetadataToken) -and $enemiesTokens.ContainsKey($sortMethod.MetadataToken)) 'MMR-04: native brain candidates come from the group memory and are sorted by TargetInfo.Sort'
$fill = $targetInfo.GetMethod('FillWeight', [Reflection.BindingFlags]'Instance,Public,NonPublic')
$fillIl = $fill.GetMethodBody().GetILAsByteArray()
$fillTokens = IlTokens $fill
$manual = (GameType 'Kingmaker.Controllers.Combat.UnitCombatState').GetProperty('ManualTarget').GetGetMethod()
$distanceField = $targetInfo.GetField('Distance')
$minusMilli = [BitConverter]::GetBytes([single]-0.001); $hasMinusMilli = $false
for ($i = 0; $i -le $fillIl.Length - 5; $i++) { if ($fillIl[$i] -eq 0x22 -and $fillIl[$i + 1] -eq $minusMilli[0] -and $fillIl[$i + 2] -eq $minusMilli[1] -and $fillIl[$i + 3] -eq $minusMilli[2] -and $fillIl[$i + 4] -eq $minusMilli[3]) { $hasMinusMilli = $true } }
Check ($fillTokens.ContainsKey($manual.MetadataToken) -and $fillTokens.ContainsKey($distanceField.MetadataToken) -and $hasMinusMilli) 'MMR-04: native FillWeight keeps a manual target first and prefers the nearer enemy (-0.001 x distance)'
# ---- 4. MMR-01 seat wiring (review R1-F12): the compiled profile-to-adapter path, not a copy of it -------------------
$profiles = KmcType 'KingmakerMountedCombat.Integration.SupportedMountedProfiles'
$profileType = KmcType 'KingmakerMountedCombat.Integration.SupportedMountedProfile'
$settles = $profileType.GetProperty('SettlesSeatFromIdlePose')
$calibrationType = KmcType 'KingmakerMountedCombat.Domain.AnimatedSaddleCalibration'
$horseProfile = $profiles.GetField('Horse', $any).GetValue($null); $mammothProfile = $profiles.GetField('Mammoth', $any).GetValue($null)
Check ($null -eq $profileType.GetProperty('DeclaredSeatFromSource') -and @($calibrationType.GetFields($any) | Where-Object { $_.FieldType.Name -eq 'PoseVector3' }).Count -eq 0) 'MMR-01: no literal seat basis remains in the product (the Horse basis is the rider''s own settled pose, measured live)'
Check ([bool]$settles.GetValue($horseProfile)) 'MMR-01: the Horse profile settles its seat on the rider''s own still pose'
Check (-not [bool]$settles.GetValue($mammothProfile)) 'MMR-01: the Mammoth keeps its own calibration (no animated saddle)'
$small = @($profiles.GetField('small', $any).GetValue($null))
Check ($small.Count -gt 0 -and @($small | Where-Object { [bool]$settles.GetValue($_) }).Count -eq 0) ('MMR-01: all ' + $small.Count + ' Small profiles keep their own fixed-from-source seat')
function CallsMethod([Type]$Type, [Reflection.MethodBase]$Target) {
    foreach ($t in @($Type) + @($Type.GetNestedTypes($any))) {
        foreach ($m in $t.GetMethods($any + [Reflection.BindingFlags]::DeclaredOnly)) {
            $body = $m.GetMethodBody(); if ($null -eq $body) { continue }
            $il = $body.GetILAsByteArray()
            for ($i = 0; $i -le $il.Length - 5; $i++) {
                if (($il[$i] -eq 0x28 -or $il[$i] -eq 0x6F) -and [BitConverter]::ToInt32($il, $i + 1) -eq $Target.MetadataToken) {
                    try { if ($m.Module.ResolveMethod([BitConverter]::ToInt32($il, $i + 1)) -eq $Target) { return $true } } catch { }
                }
            }
        }
    }
    $false
}
$runtimeType = KmcType 'KingmakerMountedCombat.Integration.KingmakerMountedPairRuntime'
$adapterType = KmcType 'KingmakerMountedCombat.Integration.MountedRiderPoseAdapter'
$observe = (KmcType 'KingmakerMountedCombat.Domain.SettledSeatSampler').GetMethod('Observe')
$notice = $calibrationType.GetMethod('FormatNotice')
Check (CallsMethod $runtimeType $settles.GetGetMethod()) 'MMR-01: pair attachment passes the profile''s settled-seat flag to the pose adapter'
Check (CallsMethod $adapterType $observe) 'MMR-01: the adapter''s seat frames decide the settled sample through SettledSeatSampler.Observe'
Check (CallsMethod $runtimeType $notice) 'MMR-01: pair attachment logs the attachment and settled seat samples (seatCalibration lines) on the normal path'
Write-Host ('Mounted alpha 2 native contract PASS=' + $script:passed + ' FAIL=0')
