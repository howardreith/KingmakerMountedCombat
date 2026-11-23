[CmdletBinding()]
param([string]$Configuration = 'Release')
# Mounted Shield (docs/missions/mounted-shield/MISSION.md) installed-contract and production-path checks.
#  1. Reads the installed Kingmaker 2.1.7b data (pinned file identities, exact serialized object offsets): the stock
#     Shield Focus and Greater Shield Focus identities the product resolves, the stock Power Attack donor, and the four
#     stock shield armor types' native base bonus with independent cross-checks (IsShield, proficiency group, armor check
#     penalty, arcane spell failure).
#  2. Runs the compiled product on genuine Kingmaker objects created without Unity:
#     - the Mounted Shield registration construction and contract validation (stock donor, IsClassFeature retained);
#     - the production base-bonus read on genuine ItemEntityShield/ItemEntityArmor/BlueprintItemArmor/BlueprintArmorType
#       objects built from the installed values, with an enhanced item blueprint and a shield weapon component beside it;
#     - the production amount policy over those reads (+2/+2/+3/+5 with Shield Focus, +4 with both focus feats);
#     - the production mount-AC write on a genuine ModifiableValue, evaluated by the installed engine's own stacking,
#       display list and ModifiableValueArmorClass touch/flat-footed predicates through its own ApplyModifiersFiltered;
#     - the production check write on a genuine RuleSkillCheck, read through the native Bonus/RollResult/IsPassed and the
#       production strict comparison (equality fails, one higher succeeds);
#     - the production three-feat publication on genuine BlueprintFeatureSelection arrays, with independent withdrawal;
#  3. The built DLL embeds exactly one Mounted Shield resource, byte-identical to the packet's runtime PNG.
# Not covered (native game evidence, BLOCKED on this machine): Unity object creation and the live library, a unit's
# CharacterStats (its AC constructor JITs a Unity internal call outside the player), equipment slots and events, the
# rulebook, save/load, the level-up UI and native prerequisite evaluation, and the in-game icon.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$lab = [IO.Path]::GetFullPath((Join-Path $repoRoot '..\..'))
# The lab root: two levels above the repository, for the main checkout and for a worktree.
$intake = Join-Path $lab 'environment-intake.json'
$layout = (Get-Content -Raw -LiteralPath $intake | ConvertFrom-Json).requestedLayout
$data = Join-Path $layout.kingmakerInstallDir 'Kingmaker_Data'
$managed = Join-Path $data 'Managed'
$productDll = Join-Path $repoRoot ('bin\' + $Configuration + '\KingmakerMountedCombat.dll')
$packetIcon = Join-Path $repoRoot 'docs\missions\mounted-shield\assets\MountedShieldFeat.png'
$repoIcon = Join-Path $repoRoot 'src\KingmakerMountedCombat\Assets\MountedShieldFeat.png'
$script:passed = 0
function Check([bool]$Value, [string]$Label) { if (-not $Value) { throw ('Mounted Shield native contract: ' + $Label) }; $script:passed++; Write-Host ('PASS ' + $Label) }

# ---- 1. Installed data ---------------------------------------------------------------------------------------------
$assets = Join-Path $data 'sharedassets1.assets'
Check ((Get-FileHash -LiteralPath $assets).Hash.ToLowerInvariant() -ceq 'cc779caf2fef21d111856a57d40b510677b0c236ad2723d1849564626445f785') 'pinned sharedassets1 identity'
function UnityString($Reader, [int]$Max) {
    $length = $Reader.ReadInt32(); if ($length -lt 0 -or $length -gt $Max) { throw 'Unexpected bounded Unity string' }
    $text = [Text.Encoding]::UTF8.GetString($Reader.ReadBytes($length))
    [void]$Reader.BaseStream.Seek((4 - $Reader.BaseStream.Position % 4) % 4, [IO.SeekOrigin]::Current)
    $text
}
function Bool32($Reader, [string]$Field) { $value = $Reader.ReadInt32(); if ($value -ne 0 -and $value -ne 1) { throw ('Unaligned or invalid bool ' + $Field) }; $value -eq 1 }
# MonoBehaviour header (m_GameObject, m_Enabled), m_Script, m_Name; BlueprintScriptableObject PrototypeLink, Components,
# m_AssetGuid. Returns the reader positioned after the GUID.
function Read-BlueprintHeader($Reader, [long]$Offset, [long]$Script, [string]$Name, [string]$Guid) {
    [void]$Reader.BaseStream.Seek($Offset + 16, [IO.SeekOrigin]::Begin)
    Check (($Reader.ReadInt32() -eq 1) -and ($Reader.ReadInt64() -eq $Script)) ($Name + ' managed script pointer ' + $Script)
    Check ((UnityString $Reader 256) -ceq $Name) ($Name + ' authored name')
    [void]$Reader.ReadBytes(12)
    $components = $Reader.ReadInt32(); if ($components -lt 0 -or $components -gt 64) { throw 'Unexpected component count' }
    [void]$Reader.ReadBytes(12 * $components)
    Check ((UnityString $Reader 32) -ceq $Guid) ($Name + ' GUID ' + $Guid)
}
# BlueprintFeature after the header: m_AllowNonContextActions, m_DisplayName, m_Description, m_Icon; HideInUI,
# HideInCharacterSheetAndLevelUp, HideNotAvailibleInUI, DlcType; Groups, Ranks, ReapplyOnLevelUp, IsClassFeature
# (scripts/Test-MountedArcheryFeatContract.ps1 decodes the same layout).
function Read-StockFeature($Reader, [long]$Offset, [int]$Size, [string]$Name, [string]$Guid) {
    Read-BlueprintHeader $Reader $Offset 6001 $Name $Guid
    [void](Bool32 $Reader 'm_AllowNonContextActions')
    for ($i = 0; $i -lt 2; $i++) { [void](UnityString $Reader 256); [void]$Reader.ReadBytes(12) }
    $iconFile = $Reader.ReadInt32(); $iconPath = $Reader.ReadInt64()
    $feature = [ordered]@{ Name = $Name; HasIcon = ($iconFile -ne 0 -or $iconPath -ne 0)
        HideInUI = (Bool32 $Reader 'HideInUI'); HideInCharacterSheetAndLevelUp = (Bool32 $Reader 'HideInCharacterSheetAndLevelUp')
        HideNotAvailibleInUI = (Bool32 $Reader 'HideNotAvailibleInUI'); DlcType = $Reader.ReadInt32() }
    $count = $Reader.ReadInt32(); if ($count -lt 0 -or $count -gt 16) { throw 'Unexpected group count' }
    $feature.Groups = @(for ($i = 0; $i -lt $count; $i++) { $Reader.ReadInt32() })
    $feature.Ranks = $Reader.ReadInt32()
    $feature.ReapplyOnLevelUp = Bool32 $Reader 'ReapplyOnLevelUp'
    $feature.IsClassFeature = Bool32 $Reader 'IsClassFeature'
    Check ($Reader.BaseStream.Position -eq $Offset + $Size) ($Name + ' decoding ends exactly at the serialized object end')
    [pscustomobject]$feature
}
# BlueprintArmorType after the header: four LocalizedStrings (m_Key + Shared), m_Icon, m_VisualParameters (sound type,
# four sound strings, override miss effect), then m_ArmorBonus, m_ArmorChecksPenalty, m_HasDexterityBonusLimit,
# m_MaxDexterityBonus, m_ProficiencyGroup, m_ArcaneSpellFailureChance, m_Weight, m_IsArmor, m_IsShield.
function Read-ShieldType($Reader, [long]$Offset, [string]$Name, [string]$Guid) {
    Read-BlueprintHeader $Reader $Offset 1348 $Name $Guid
    for ($i = 0; $i -lt 4; $i++) { [void](UnityString $Reader 256); [void]$Reader.ReadBytes(12) }
    [void]$Reader.ReadBytes(12)
    [void]$Reader.ReadInt32(); for ($i = 0; $i -lt 4; $i++) { [void](UnityString $Reader 256) }; [void]$Reader.ReadBytes(12)
    $type = [ordered]@{ Name = $Name; Guid = $Guid; ArmorBonus = $Reader.ReadInt32(); ArmorCheckPenalty = $Reader.ReadInt32() }
    [void](Bool32 $Reader 'm_HasDexterityBonusLimit'); [void]$Reader.ReadInt32()
    $type.ProficiencyGroup = $Reader.ReadInt32(); $type.ArcaneSpellFailure = $Reader.ReadInt32(); [void]$Reader.ReadSingle()
    $type.IsArmor = Bool32 $Reader 'm_IsArmor'; $type.IsShield = Bool32 $Reader 'm_IsShield'
    [pscustomobject]$type
}
$reader = [IO.BinaryReader]::new([IO.File]::OpenRead($assets))
try {
    $power = Read-StockFeature $reader 154062912 252 'PowerAttackFeature' '9972f33f977fc724c838e59641b2fca5'
    # Identity only: name, BlueprintFeature script and GUID, as the product resolves them (RequireBlueprint).
    Read-BlueprintHeader $reader 154079712 6001 'ShieldFocus' 'ac57069b6bf8c904086171683992a92a'
    Read-BlueprintHeader $reader 154079992 6001 'ShieldFocusGreater' 'afd05ca5363036c44817c071189b67e1'
    $shieldTypes = @(
        (Read-ShieldType $reader 153733248 'BucklerType' '26fcc43f7d20374498d2e1643381d345'),
        (Read-ShieldType $reader 153734328 'LightShieldType' 'd38e8ea23ce653c4582eb3e002555483'),
        (Read-ShieldType $reader 153733784 'HeavyShieldType' 'd1b05b901bab9524388ebfa0435902a6'),
        (Read-ShieldType $reader 153734872 'TowerShieldType' '5f0f4b6e480e7054b8592b5a8b55854a'))
} finally { $reader.Dispose() }
# ArmorProficiencyGroup: Buckler 4, LightShield 5, HeavyShield 6, TowerShield 7. Spell failure 5/5/15/50 and armor check
# penalty -1/-1/-2/-10 are the published shield values, so a misaligned decode cannot pass by accident.
$expectedShields = @(@(1, 4, -1, 5), @(1, 5, -1, 5), @(2, 6, -2, 15), @(4, 7, -10, 50))
for ($i = 0; $i -lt 4; $i++) {
    $type = $shieldTypes[$i]; $expected = $expectedShields[$i]
    Write-Host ('  stock ' + $type.Name + ': base=' + $type.ArmorBonus + ' group=' + $type.ProficiencyGroup + ' acp=' + $type.ArmorCheckPenalty + ' asf=' + $type.ArcaneSpellFailure + ' shield=' + $type.IsShield)
    Check ($type.IsArmor -and $type.IsShield -and $type.ArmorBonus -eq $expected[0] -and $type.ProficiencyGroup -eq $expected[1] -and
        $type.ArmorCheckPenalty -eq $expected[2] -and $type.ArcaneSpellFailure -eq $expected[3]) ('installed ' + $type.Name + ' base shield bonus +' + $expected[0])
}
Check ((($power.Groups | Sort-Object) -join ',') -ceq '1,2' -and $power.Ranks -eq 1 -and $power.IsClassFeature -and $power.HasIcon -and
    -not $power.HideInUI -and -not $power.HideInCharacterSheetAndLevelUp -and -not $power.HideNotAvailibleInUI) 'stock Power Attack donor: visible single-rank Feat and CombatFeat, IsClassFeature=true'

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
$unity = [Reflection.Assembly]::LoadFrom((Join-Path $managed 'UnityEngine.CoreModule.dll'))
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
$service = KmcType 'KingmakerMountedCombat.Integration.HorseCompanionBlueprintService'
$shieldService = KmcType 'KingmakerMountedCombat.Integration.MountedShieldService'
function Const([Type]$Type, [string]$Name) { $Type.GetField($Name, $any).GetRawConstantValue() }
Check ((Const $service 'ShieldFocusGuid') -ceq 'ac57069b6bf8c904086171683992a92a' -and (Const $service 'ShieldFocusName') -ceq 'ShieldFocus' -and
    (Const $service 'GreaterShieldFocusGuid') -ceq 'afd05ca5363036c44817c071189b67e1' -and (Const $service 'GreaterShieldFocusName') -ceq 'ShieldFocusGreater') `
    'the product resolves exactly the installed Shield Focus and Greater Shield Focus identities'
$featGuid = Const $service 'MountedShieldFeatureGuid'
# Collision with an installed or another mod's blueprint is refused at registration (AssertReservedGuidAbsent).
Check ($featGuid -cmatch '^[0-9a-f]{32}$' -and $featGuid -cne (Const $service 'MountedArcheryFeatureGuid') -and $featGuid -cne (Const $service 'MountedCombatFeatureGuid')) 'the Mounted Shield GUID is a distinct KMC identity'
Check ((Const $service 'MountedShieldIconResource') -ceq 'KingmakerMountedCombat.Assets.MountedShieldFeat.png' -and (Const $service 'MountedShieldIconSprite') -ceq 'KMC_MountedShield_Feat_Icon') 'icon resource and sprite names'

# 2a. Registration contract on genuine blueprints (the same construction and validation registration runs).
$featureType = GameType 'Kingmaker.Blueprints.Classes.BlueprintFeature'
$selectionType = GameType 'Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection'
$groupType = GameType 'Kingmaker.Blueprints.Classes.FeatureGroup'
$spriteType = $unity.GetType('UnityEngine.Sprite', $true)
$iconField = (GameType 'Kingmaker.Blueprints.Facts.BlueprintUnitFact').GetField('m_Icon', [Reflection.BindingFlags]'Instance,NonPublic')
function Groups([int[]]$Values) { $array = [Array]::CreateInstance($groupType, $Values.Count); for ($i = 0; $i -lt $Values.Count; $i++) { $array.SetValue([Enum]::ToObject($groupType, $Values[$i]), $i) }; , $array }
function F($Value, [string]$Field) { $featureType.GetField($Field).GetValue($Value) }
$donor = New-Native $featureType
$featureType.GetField('Groups').SetValue($donor, (Groups $power.Groups))
foreach ($field in 'Ranks', 'ReapplyOnLevelUp', 'IsClassFeature', 'HideInUI', 'HideInCharacterSheetAndLevelUp', 'HideNotAvailibleInUI') { $featureType.GetField($field).SetValue($donor, $power.$field) }
$iconField.SetValue($donor, (New-Native $spriteType))
$icon = New-Native $spriteType
$shield = New-Native $featureType
[void]$service.GetMethod('BuildMountedShieldContract', $any).Invoke($null, @($donor, $shield, $null, $null, $icon))
$validate = $service.GetMethod('MountedShieldContractViolation', $any)
Check ($null -eq $validate.Invoke($null, @($shield, $donor, $icon))) 'the Mounted Shield feat built from stock Power Attack holds the contract'
Check ((F $shield 'IsClassFeature') -and (F $shield 'Ranks') -eq 1 -and -not (F $shield 'HideInUI') -and [string](F $shield 'DlcType') -ceq 'None' -and
    -not [object]::ReferenceEquals((F $shield 'Groups'), (F $donor 'Groups'))) 'built feat: stock IsClassFeature=true retained (never forced false), one rank, visible, base game, own group array'
$featureType.GetField('IsClassFeature').SetValue($shield, $false)
$forced = [string]$validate.Invoke($null, @($shield, $donor, $icon))
Check ($forced.StartsWith('Mounted Shield did not retain the single-rank combat-feat contract: IsClassFeature=False (expected the stock donor''s True). Feature ')) 'forcing IsClassFeature=false is refused with a Mounted Shield diagnostic'
$featureType.GetField('IsClassFeature').SetValue($shield, $true)
$iconField.SetValue($shield, (New-Native $spriteType))
Check (([string]$validate.Invoke($null, @($shield, $donor, $icon))).Contains('icon is not KMC''s Mounted Shield icon')) 'a substituted icon is refused, never accepted as the required one'
$iconField.SetValue($shield, $icon)

# The hidden availability prerequisite (pre-native review PN2-F3): the native selection decision asks it, and it answers
# exactly what the publication's full comparison says; unset, it refuses.
$prereqType = KmcType 'KingmakerMountedCombat.Integration.MountedShieldAvailabilityPrerequisite'
Check ($prereqType.BaseType.FullName -ceq 'Kingmaker.Blueprints.Classes.Prerequisites.Prerequisite') 'the availability check is a native Prerequisite (evaluated by BlueprintFeature.MeetsPrerequisites)'
$prereq = New-Native $prereqType
$availableField = $prereqType.GetField('Available', $any)
$prereqCheck = $prereqType.GetMethod('Check')
$availableField.SetValue($null, $null)
$unset = [bool]$prereqCheck.Invoke($prereq, @($null, $null, $null))
$availableField.SetValue($null, [Func[bool]]{ $true })
$open = [bool]$prereqCheck.Invoke($prereq, @($null, $null, $null))
$availableField.SetValue($null, [Func[bool]]{ $false })
$closed = [bool]$prereqCheck.Invoke($prereq, @($null, $null, $null))
$availableField.SetValue($null, $null)
Check (-not $unset -and $open -and -not $closed -and $prereq.GetUIText() -ceq '') 'the prerequisite passes only while the rule is available, refuses when unset, and adds no prerequisite text'

# 2b. Base bonus read and amount on genuine item objects built from the installed values.
$armorTypeType = GameType 'Kingmaker.Blueprints.Items.Armors.BlueprintArmorType'
$itemArmorType = GameType 'Kingmaker.Blueprints.Items.Armors.BlueprintItemArmor'
$entityArmorType = GameType 'Kingmaker.Items.ItemEntityArmor'
$entityShieldType = GameType 'Kingmaker.Items.ItemEntityShield'
$entityWeaponType = GameType 'Kingmaker.Items.ItemEntityWeapon'
$profType = GameType 'Kingmaker.Blueprints.Items.Armors.ArmorProficiencyGroup'
$enchantType = GameType 'Kingmaker.Blueprints.Items.Ecnchantments.BlueprintEquipmentEnchantment'
$enhancementType = GameType 'Kingmaker.Blueprints.Items.Ecnchantments.ArmorEnhancementBonus'
$componentArrayType = (GameType 'Kingmaker.Blueprints.BlueprintComponent').MakeArrayType()
$baseRead = $shieldService.GetMethod('ShieldBaseBonus', $any)
function New-Shield($Type, [int]$Enhancement) {
    $armorType = New-Native $armorTypeType
    SetField $armorType $armorTypeType 'm_ArmorBonus' $Type.ArmorBonus
    SetField $armorType $armorTypeType 'm_ProficiencyGroup' ([Enum]::ToObject($profType, $Type.ProficiencyGroup))
    SetField $armorType $armorTypeType 'm_IsShield' $true
    $item = New-Native $itemArmorType
    SetField $item $itemArmorType 'm_Type' $armorType
    $enchantments = [Array]::CreateInstance($enchantType, 0)
    if ($Enhancement -gt 0) {
        $bonus = New-Native $enhancementType; SetField $bonus $enhancementType 'EnhancementValue' $Enhancement
        $components = [Array]::CreateInstance((GameType 'Kingmaker.Blueprints.BlueprintComponent'), 1); $components.SetValue($bonus, 0)
        $enchant = New-Native $enchantType; SetField $enchant $enchantType 'Components' $components
        $enchantments = [Array]::CreateInstance($enchantType, 1); $enchantments.SetValue($enchant, 0)
    }
    SetField $item $itemArmorType 'm_Enchantments' $enchantments
    $armor = New-Native $entityArmorType; SetField $armor $entityArmorType 'm_Blueprint' $item
    $entity = New-Native $entityShieldType; SetField $entity $entityShieldType 'ArmorComponent' $armor
    if ($Enhancement -gt 0) { SetField $entity $entityShieldType 'WeaponComponent' (New-Native $entityWeaponType) }
    $entity
}
$policy = KmcType 'KingmakerMountedCombat.Domain.MountedShieldPolicy'
$factsType = KmcType 'KingmakerMountedCombat.Domain.MountedShieldFacts'
function Amount([int]$Base, [bool]$Focus, [bool]$Greater, [bool]$HasShield = $true) {
    $facts = [Activator]::CreateInstance($factsType)
    foreach ($pair in @(@('Available', $true), @('MountedPair', $true), @('OwnsFeat', $true), @('EffectiveShield', $HasShield), @('ShieldBaseBonus', $Base), @('ShieldFocus', $Focus), @('GreaterShieldFocus', $Greater))) { $factsType.GetField($pair[0]).SetValue($facts, $pair[1]) }
    [int]$policy.GetMethod('Amount').Invoke($null, @($facts))
}
$expectedAmounts = @(2, 2, 3, 5)
for ($i = 0; $i -lt 4; $i++) {
    $type = $shieldTypes[$i]
    $mundane = [int]$baseRead.Invoke($null, @((New-Shield $type 0)))
    $enhanced = [int]$baseRead.Invoke($null, @((New-Shield $type 5)))
    Check ($mundane -eq $type.ArmorBonus -and $enhanced -eq $type.ArmorBonus) ($type.Name + ': the production read returns the native base +' + $type.ArmorBonus + ' for a mundane and a +5 item alike')
    Check ((Amount $mundane $true $false) -eq $expectedAmounts[$i]) ($type.Name + ' with Shield Focus supplies +' + $expectedAmounts[$i])
}
Check ((Amount 2 $true $true) -eq 4 -and (Amount 2 $true $false) -eq 3) 'heavy shield: +3 with Shield Focus, +4 with Greater Shield Focus too (mission ruling)'
Check ((Amount 2 $true $true $false) -eq 0) 'no effective shield: zero, focus feats included'

# 2c. The production mount-AC write, evaluated by the installed engine's own stacking and AC predicates.
$mvType = GameType 'Kingmaker.EntitySystem.Stats.ModifiableValue'
$acType = GameType 'Kingmaker.EntitySystem.Stats.ModifiableValueArmorClass'
$statType = GameType 'Kingmaker.EntitySystem.Stats.StatType'
$descriptorType = GameType 'Kingmaker.Enums.ModifierDescriptor'
$factType = GameType 'Kingmaker.Blueprints.Facts.Fact'
$modifierType = $mvType.GetNestedType('Modifier')
function Descriptor([string]$Name) { [Enum]::Parse($descriptorType, $Name) }
$addOther = $mvType.GetMethod('AddModifier', [Type[]]@([int], $factType, [string], $descriptorType))
$addMount = $shieldService.GetMethod('AddMountShieldBonus', $any)
$filtered = $mvType.GetMethod('ApplyModifiersFiltered', [Reflection.BindingFlags]'Instance,NonPublic')
$predicateType = [Func``2].MakeGenericType($modifierType, [bool])
$touch = [Delegate]::CreateDelegate($predicateType, $acType.GetMethod('AllowedForTouch'))
$flatFooted = [Delegate]::CreateDelegate($predicateType, $acType.GetMethod('AllowedForFlatFooted'))
$riderFact = New-Native $factType
function New-Ac { $value = $mvType.GetConstructor(@($statType)).Invoke(@([Enum]::Parse($statType, 'AC'))); $value.BaseValue = 10; $value }
function Read-Ac($Ac) { [pscustomobject]@{ Normal = $Ac.ModifiedValue; Touch = [int]$filtered.Invoke($Ac, @(10, $touch)); FlatFooted = [int]$filtered.Invoke($Ac, @(10, $flatFooted)) } }
function Shown($Ac) { @($Ac.GetDisplayModifiers() | ForEach-Object { [string]$_.ModDescriptor + ':' + $_.ModValue + ':' + $_.SourceComponent }) -join ',' }
$ac = New-Ac
$mine = $addMount.Invoke($null, @($ac, 3, $riderFact))
$view = Read-Ac $ac
Check ([string]$mine.ModDescriptor -ceq 'Shield' -and $mine.SourceComponent -ceq 'KMC_MountedShield_MountShieldBonus' -and [object]::ReferenceEquals($mine.Source, $riderFact) -and -not $mine.Stacks) `
    'the production write is one non-stacking Shield modifier named by the rider''s fact'
Check ($view.Normal -eq 13 -and $view.FlatFooted -eq 13 -and $view.Touch -eq 10) 'a rider supplying +3: normal 13, flat-footed 13, touch 10 (shield excluded from touch)'
$stronger = $addOther.Invoke($ac, @(4, $null, 'stand-in Shield spell', (Descriptor 'Shield')))
$view = Read-Ac $ac
Check ($view.Normal -eq 14 -and $view.FlatFooted -eq 14 -and $view.Touch -eq 10 -and (Shown $ac) -ceq 'Shield:4:stand-in Shield spell') `
    'a stronger +4 shield source on the mount: AC 14, not 17; the sheet lists the +4 alone (no untyped stack)'
[void]$stronger.Remove()
$equal = $addOther.Invoke($ac, @(3, $null, 'equal shield', (Descriptor 'Shield'))); Check ((Read-Ac $ac).Normal -eq 13) 'an equal +3 shield source: AC 13'; [void]$equal.Remove()
$weaker = $addOther.Invoke($ac, @(1, $null, 'weaker shield', (Descriptor 'Shield'))); Check ((Read-Ac $ac).Normal -eq 13 -and (Shown $ac) -ceq 'Shield:3:KMC_MountedShield_MountShieldBonus') 'a weaker +1 shield source: AC 13 and the sheet names Mounted Shield'; [void]$weaker.Remove()
foreach ($other in @(@(2, 'NaturalArmor'), @(1, 'Deflection'), @(1, 'Dodge'), @(4, 'Armor'))) { [void]$addOther.Invoke($ac, @($other[0], $null, $other[1], (Descriptor $other[1]))) }
$view = Read-Ac $ac
Check ($view.Normal -eq 21 -and $view.FlatFooted -eq 20 -and $view.Touch -eq 12) 'independent armor, natural armor, deflection and dodge stay intact: normal 21, flat-footed 20 (no dodge), touch 12 (deflection and dodge only)'
$holds = $shieldService.GetMethod('Holds', $any)
$removeExact = $shieldService.GetMethod('RemoveExact', $any)
$identity = $shieldService.GetMethod('IdentityModifiers', $any)
[void]$removeExact.Invoke($null, @($ac, $mine, $null))
Check (-not [bool]$holds.Invoke($null, @($ac, $mine)) -and $null -eq $mine.AppliedTo) 'the production removal takes the contribution off through its stat, verified'
$view = Read-Ac $ac
Check ($view.Normal -eq 18 -and $view.FlatFooted -eq 17 -and $view.Touch -eq 12) 'after removal only the other sources remain (normal 18, flat-footed 17, touch 12)'

# 2c'. A half-done write (pre-native review finding 1): the installed ModifiableValue.AddModifier puts the modifier into the
# stat and runs UpdateValue (and its OnChanged callbacks) before it sets AppliedTo. A throwing callback therefore leaves an
# untracked modifier that Modifier.Remove() cannot remove. The production identity sweep and RemoveExact must still find
# and remove it, and verify the stat is clean.
$halfDone = New-Ac
$changedType = [Action``2].MakeGenericType($mvType, [int])
$throwing = [Management.Automation.LanguagePrimitives]::ConvertTo([scriptblock]{ param($stat, $previous) throw 'stat-change callback fault' }, $changedType)
$mvType.GetEvent('OnChanged').AddEventHandler($halfDone, $throwing)
$threw = $false
try { [void]$addMount.Invoke($null, @($halfDone, 3, $null)) } catch { $threw = $true }
$stray = @($identity.Invoke($null, @($halfDone, [string](Const $service 'MountedShieldFeatureGuid'))))
Check ($threw -and $stray.Count -eq 1 -and $null -eq $stray[0].AppliedTo -and $halfDone.ModifiedValue -eq 13) 'reproduced: a throwing callback leaves the +3 in the stat with no AppliedTo (AC 13)'
Check (-not $stray[0].Remove()) 'reproduced: the native Modifier.Remove() cannot remove it (returns false)'
$faults = New-Object 'System.Collections.Generic.List[string]'
$onFault = [Management.Automation.LanguagePrimitives]::ConvertTo([scriptblock]{ param($fault) $faults.Add($fault.GetType().Name) }.GetNewClosure(), [Action[Exception]])
[void]$removeExact.Invoke($null, @($halfDone, $stray[0], $onFault))
Check (-not [bool]$holds.Invoke($null, @($halfDone, $stray[0])) -and @($identity.Invoke($null, @($halfDone, [string](Const $service 'MountedShieldFeatureGuid')))).Count -eq 0 -and
    $halfDone.ModifiedValue -eq 10 -and $faults.Count -eq 1) 'the production removal removes the untracked modifier through the stat, verifies it, and reports the callback fault'
$withForeign = New-Ac
$kept = $addOther.Invoke($withForeign, @(4, $null, 'unrelated', (Descriptor 'Shield')))
$ours = $addMount.Invoke($null, @($withForeign, 2, $null))
Check (@($identity.Invoke($null, @($withForeign, [string](Const $service 'MountedShieldFeatureGuid')))).Count -eq 1 -and [bool]$holds.Invoke($null, @($withForeign, $kept))) 'the identity sweep selects only Mounted Shield''s own modifier, never another shield source'

# 2d. The production check write on a genuine RuleSkillCheck, as MountedCombatDefenseService builds it (DC = attack + 1).
$checkType = GameType 'Kingmaker.RuleSystem.Rules.RuleSkillCheck'
$rollType = (GameType 'Kingmaker.RuleSystem.RulebookEvent').GetNestedType('RollEntry')
$toRoll = $rollType.GetMethod('op_Implicit', [Type[]]@([int]))
$addCheck = $shieldService.GetMethod('AddCheckBonus', $any)
$negates = (KmcType 'KingmakerMountedCombat.Domain.MountedCombatDefensePolicy').GetMethod('Negates', $any)
function Roll([int]$Attack, [int]$Stat, [int]$D20, [int]$Shield, [int]$Other) {
    $check = $checkType.GetConstructors()[0].Invoke(@($null, [Enum]::Parse($statType, 'SkillMobility'), ($Attack + 1)))
    if ($Other -ne 0) { [void]$addOther.Invoke($check.Bonus, @($Other, $null, 'other check bonus', (Descriptor 'Competence'))) }
    if ($Shield -gt 0) { [void]$addCheck.Invoke($null, @($check, $Shield, $riderFact)) }
    $checkType.GetProperty('StatValue').GetSetMethod($true).Invoke($check, @($Stat)) | Out-Null
    $checkType.GetProperty('D20').GetSetMethod($true).Invoke($check, @($toRoll.Invoke($null, @($D20)))) | Out-Null
    [pscustomobject]@{ Result = $check.RollResult; Bonus = [int]$check.Bonus.ModifiedValue; Passed = $check.IsPassed; Negates = [bool]$negates.Invoke($null, @($check.RollResult, $Attack)) }
}
$equalRoll = Roll 22 10 9 3 0; $higherRoll = Roll 22 10 10 3 0; $withoutShield = Roll 22 10 10 0 0; $withOther = Roll 22 10 8 3 2
Check ($equalRoll.Result -eq 22 -and $equalRoll.Bonus -eq 3 -and -not $equalRoll.Passed -and -not $equalRoll.Negates) 'check equal to the attack after the +3 addition fails (native IsPassed and the strict rule agree)'
Check ($higherRoll.Result -eq 23 -and $higherRoll.Passed -and $higherRoll.Negates) 'one higher succeeds'
Check ($withoutShield.Result -eq 20 -and -not $withoutShield.Negates) 'the same roll without the feat stays 20 and fails'
Check ($withOther.Result -eq 23 -and $withOther.Bonus -eq 5 -and $withOther.Negates) 'the addition stacks with the check''s other bonuses (untyped), counted once'
# The gameplay driver's planned total (owner amendment 1): the production ControlledSkillCheck chooses its die inside the
# native roll after StatValue and every Bonus are fixed (native OnTrigger order), so "equal" is the shield-modified total.
$controlled = KmcType 'KingmakerMountedCombat.Integration.MountedCombatDefenseService+ControlledSkillCheck'
$rollD20 = $controlled.GetMethod('RollD20', $any)
function Planned([int]$Attack, [int]$Total, [int]$Stat, [int]$Shield, [int]$Other) {
    $check = @($controlled.GetConstructors($any))[0].Invoke(@($null, ($Attack + 1), $Total))
    if ($Other -ne 0) { [void]$addOther.Invoke($check.Bonus, @($Other, $null, 'other check bonus', (Descriptor 'Competence'))) }
    if ($Shield -gt 0) { [void]$addCheck.Invoke($null, @($check, $Shield, $riderFact)) }
    $checkType.GetProperty('StatValue').GetSetMethod($true).Invoke($check, @($Stat)) | Out-Null
    $checkType.GetProperty('D20').GetSetMethod($true).Invoke($check, @($rollD20.Invoke($check, @()))) | Out-Null
    [pscustomobject]@{ Result = $check.RollResult; D20 = $check.RollResult - $Stat - [int]$check.Bonus.ModifiedValue; Negates = [bool]$negates.Invoke($null, @($check.RollResult, $Attack)) }
}
$plannedEqual = Planned 22 22 10 3 0; $plannedSuccess = Planned 22 23 10 3 2; $mobilityOnly = Roll 22 10 12 3 0
Check ($plannedEqual.Result -eq 22 -and $plannedEqual.D20 -eq 9 -and -not $plannedEqual.Negates) 'the planned "equal" total is the +3 shield-modified result (die 9) and fails'
Check ($plannedSuccess.Result -eq 23 -and $plannedSuccess.D20 -eq 8 -and $plannedSuccess.Negates) 'the planned success total lands one above with the shield and another bonus in the roll'
Check ($mobilityOnly.Result -eq 25) 'a die derived from Mobility alone (12) would have tested 25, three above equality: the trap the planned total removes'

# 2e. Three-feat publication on genuine selection blueprints.
function Feature-Array([object[]]$Items) { $array = [Array]::CreateInstance($featureType, $Items.Count); for ($i = 0; $i -lt $Items.Count; $i++) { $array.SetValue($Items[$i], $i) }; , $array }
function Selection-Array([object[]]$Items) { $array = [Array]::CreateInstance($selectionType, $Items.Count); for ($i = 0; $i -lt $Items.Count; $i++) { $array.SetValue($Items[$i], $i) }; , $array }
$allFeatures = $selectionType.GetField('AllFeatures')
$combat = New-Native $featureType; $archery = New-Native $featureType; $foreign = New-Native $featureType
$ordinary = New-Native $selectionType; $fighter = New-Native $selectionType; $extra = New-Native $selectionType
foreach ($selection in @($ordinary, $fighter, $extra)) { $allFeatures.SetValue($selection, (Feature-Array @($donor, $foreign))) }
$featureArrayType = $featureType.MakeArrayType()
$read = [Management.Automation.LanguagePrimitives]::ConvertTo([scriptblock]{ param($selection) , $script:allFeatures.GetValue($selection) }, [Func``2].MakeGenericType($selectionType, $featureArrayType))
$write = [Management.Automation.LanguagePrimitives]::ConvertTo([scriptblock]{ param($selection, $value) $script:allFeatures.SetValue($selection, $value) }, [Action``2].MakeGenericType($selectionType, $featureArrayType))
$label = [Management.Automation.LanguagePrimitives]::ConvertTo([scriptblock]{ param($selection) 'selection' }, [Func``2].MakeGenericType($selectionType, [string]))
$setType = (KmcType 'KingmakerMountedCombat.Domain.KmcCombatFeatPublicationSet`2').MakeGenericType($selectionType, $featureType)
$ctor = @($setType.GetConstructors() | Where-Object { $_.GetParameters().Count -eq 7 })[0]
$set = $ctor.Invoke(@((Selection-Array @($ordinary, $fighter, $extra)), (Selection-Array @($ordinary, $fighter)), $combat, $archery, $shield, $read, $write))
Check ($null -eq $set.Apply($true, $label)) 'publication writes every genuine selection without drift'
foreach ($selection in @($ordinary, $fighter, $extra)) {
    $now = $allFeatures.GetValue($selection)
    Check ($now.Count -eq 5 -and [object]::ReferenceEquals($now[0], $donor) -and [object]::ReferenceEquals($now[1], $foreign) -and [object]::ReferenceEquals($now[2], $combat) -and
        [object]::ReferenceEquals($now[3], $archery) -and [object]::ReferenceEquals($now[4], $shield)) 'a selection keeps its entries and offers Mounted Combat, Mounted Archery, Mounted Shield once each'
}
Check ($set.MountedShieldAvailable() -and $set.MountedArcheryAvailable()) 'both optional rules are available while every required list offers them'
# Withdrawal names its lists through UnityEngine.Object.ToString, a Unity internal call outside the player; independent
# withdrawal is exercised by the component tests (tests/MountedShieldTests.cs) on the same production set.
Check ($null -eq $set.Apply($false, $label)) 'disable restores every genuine selection without drift'
$changed = @(@($ordinary, $fighter, $extra) | Where-Object { $now = $allFeatures.GetValue($_); $now.Count -ne 2 -or -not [object]::ReferenceEquals($now[0], $donor) -or -not [object]::ReferenceEquals($now[1], $foreign) })
Check ($changed.Count -eq 0) 'every selection returns to its exact original entries'

# 2f. Compiled lifecycle order in CompositionRoot (supplemental review PN4-F1, PN4-F3). The root needs the live game, so
# the order is read from the product's own IL: the first update-failure dispatch reads the owned-load state before any pair
# cleanup (the cleanup is latched instead), the terminal stop suspends Mounted Shield's offers only after its benefits stop,
# and only an explicit enable resumes them.
$rootType = KmcType 'KingmakerMountedCombat.CompositionRoot'
function Calls([Reflection.MethodBase]$Method) {
    $il = $Method.GetMethodBody().GetILAsByteArray(); $module = $Method.Module; $rows = New-Object 'System.Collections.Generic.List[string]'
    for ($i = 0; $i -lt $il.Length - 4; $i++) {
        if ($il[$i] -eq 0x28 -or $il[$i] -eq 0x6F -or $il[$i] -eq 0x7D) {
            $token = [BitConverter]::ToInt32($il, $i + 1); $table = $token -shr 24
            if ($table -ne 0x06 -and $table -ne 0x0A -and $table -ne 0x04) { continue }
            try { $member = $module.ResolveMember($token); $rows.Add($member.DeclaringType.Name + '.' + $member.Name); $i += 4 } catch { }
        }
    }
    , $rows.ToArray()
}
function At([string[]]$Rows, [string]$Name) { [Array]::IndexOf($Rows, $Name) }
$handle = Calls ($rootType.GetMethod('HandleUpdateFailure', $any))
$load = At $handle 'MountedPersistenceService.get_LoadInFlight'; $cleanup = At $handle 'CompositionRoot.PerformFailureCleanup'; $latch = At $handle 'CompositionRoot.failureCleanupPending'
Check ($load -ge 0 -and $latch -gt $load -and $cleanup -gt $latch) 'the first update-failure dispatch reads the owned load and latches before any pair cleanup (PN4-F1)'
$perform = Calls ($rootType.GetMethod('PerformFailureCleanup', $any))
$disable = At $perform 'MountedShieldService.Disable'; $suspend = At $perform 'HorseCompanionBlueprintService.SuspendMountedShieldOffers'; $stopped = At $perform 'CompositionRoot.set_IsEnabled'
Check ($disable -ge 0 -and $suspend -gt $disable -and $stopped -gt $suspend) 'the terminal stop suspends Mounted Shield''s offers after its benefits stop and before the root reports disabled (PN4-F3)'
$enable = Calls ($rootType.GetMethod('SetEnabled', $any))
$resume = At $enable 'HorseCompanionBlueprintService.ResumeMountedShieldOffers'; $selection = At $enable 'HorseCompanionBlueprintService.SetSelectionEnabled'
Check ($resume -gt $selection -and @($enable | Where-Object { $_ -ceq 'HorseCompanionBlueprintService.ResumeMountedShieldOffers' }).Count -eq 1) 'only the explicit enable resumes the offers, after its selection transaction succeeds'

# ---- 3. Embedded icon identity -------------------------------------------------------------------------------------
$expectedIcon = 'fd519874e9d231946aad68c46f93a4e0a3a2ea04d4037807e6c7a4d36629f9d0'
function Sha([byte[]]$Bytes) { $sha = [Security.Cryptography.SHA256]::Create(); try { ([BitConverter]::ToString($sha.ComputeHash($Bytes)) -replace '-', '').ToLowerInvariant() } finally { $sha.Dispose() } }
$names = @($kmc.GetManifestResourceNames() | Where-Object { $_ -match '(?i)MountedShield' })
Check ($names.Count -eq 1 -and $names[0] -ceq 'KingmakerMountedCombat.Assets.MountedShieldFeat.png') 'the DLL embeds exactly one Mounted Shield resource (no master, 512px, generated or review image)'
$stream = $kmc.GetManifestResourceStream($names[0]); $buffer = New-Object IO.MemoryStream
try { $stream.CopyTo($buffer) } finally { $stream.Dispose() }
$embedded = $buffer.ToArray()
$packet = [IO.File]::ReadAllBytes($packetIcon); $production = [IO.File]::ReadAllBytes($repoIcon)
Write-Host ('  icon sha256 packet=' + (Sha $packet) + ' production=' + (Sha $production) + ' embedded=' + (Sha $embedded))
Check ((Sha $packet) -ceq $expectedIcon -and (Sha $production) -ceq $expectedIcon -and (Sha $embedded) -ceq $expectedIcon) 'packet, production and embedded icon bytes are identical'
$header = [BitConverter]::ToString($embedded, 0, 8)
$width = ($embedded[16] -shl 24) -bor ($embedded[17] -shl 16) -bor ($embedded[18] -shl 8) -bor $embedded[19]
$height = ($embedded[20] -shl 24) -bor ($embedded[21] -shl 16) -bor ($embedded[22] -shl 8) -bor $embedded[23]
Check ($header -ceq '89-50-4E-47-0D-0A-1A-0A' -and $width -eq 64 -and $height -eq 64 -and $embedded[24] -eq 8 -and $embedded[25] -eq 6) 'the embedded icon is a 64x64 8-bit RGBA PNG'
Write-Host ('Mounted Shield native contract PASS=' + $script:passed + ' FAIL=0 (product ' + $productDll + ' sha256=' + (Get-FileHash -LiteralPath $productDll).Hash.ToLowerInvariant() + ')')
